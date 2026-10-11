using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace TimerSys.Core
{

    public class TaskItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

        public int ActivatorType { get; set; }
        public string Action { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public bool Completed { get; set; }
        public string Notes { get; set; } = "";

        public DateTime EndTimeUtc { get; set; }
        public DateTime LocalTarget { get; set; }

        public TimeSpan Duration { get; set; }

        public TimeSpan Paused { get; set; }

        public bool OneOff { get; set; }

        public string ScheduleHour { get; set; } = "";
        public string ScheduleDays { get; set; } = "";
        public DateTime LastFired { get; set; }

        public string IdleTimeout { get; set; } = "";

        public string RepeatIntervalType { get; set; } = "";
        public int RepeatIntervalValue { get; set; }
    }

    public sealed class ActionNotification
    {
        public string ActionId { get; init; } = "";
        public int Activator { get; init; }
        public string Body { get; init; } = "";
    }

    public sealed partial class AppEngine : IDisposable
    {
        public const int ModeIdle = 1, ModeTimer = 2, ModeSchedule = 3;
        public const int ScheduleIntervalIndex = 16;

        private readonly System.Windows.Threading.DispatcherTimer _idleTimer   = new() { Interval = TimeSpan.FromMilliseconds(200) };
        private readonly System.Windows.Threading.DispatcherTimer _stopTimer   = new() { Interval = TimeSpan.FromMilliseconds(200) };
        private readonly System.Windows.Threading.DispatcherTimer _schedTimer  = new() { Interval = TimeSpan.FromSeconds(1) };
        private readonly System.Windows.Threading.DispatcherTimer _snoozeTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
        private readonly System.Windows.Threading.DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private readonly System.Windows.Threading.DispatcherTimer _saveTimer   = new() { Interval = TimeSpan.FromMilliseconds(600) };

        private DateTime _idleEndTime;
        private DateTime _snoozeEnd;
        private TaskItem? _firingTask;
        private string _configPath = "";

        public string ConfigPath => _configPath;

        public NameValueCollection Settings { get; private set; } = new();
        public List<TaskItem> Tasks { get; private set; } = new();

        public event Action? TasksChanged;

        public event Action<string>? IdleStatusChanged;
        public event Action<string>? TimerStatusChanged;
        public event Action<string>? ScheduleStatusChanged;
        public event Action<string>? SnoozeStatusChanged;

        public event Action<ActionNotification>? ActionPerformed;

        public event Action<string>? ErrorReported;

        public string? EditingTaskId { get; set; }

        public bool IsEditing(TaskItem task) =>
            EditingTaskId != null && task.Id == EditingTaskId;

        public bool IdleRunning => _idleTimer.IsEnabled;
        public bool TimerRunning => _stopTimer.IsEnabled;
        public bool ScheduleRunning => _schedTimer.IsEnabled;
        public bool SnoozeRunning => _snoozeTimer.IsEnabled;

        public void Initialize()
        {
            EnsureDataFiles();
            LoadSettings();

            _idleTimer.Tick   += (s, e) => OnIdleTick();
            _stopTimer.Tick   += (s, e) => OnTimerTick();
            _schedTimer.Tick  += (s, e) => OnScheduleTick();
            _snoozeTimer.Tick += (s, e) => OnSnoozeTick();
            _statusTimer.Tick += (s, e) => TasksChanged?.Invoke();
            _saveTimer.Tick   += (s, e) => { _saveTimer.Stop(); SaveSettings(); };
            _statusTimer.Start();

            if (Settings["autoStartQueue"] != "0")
            {
                if (Settings["timer3UserPaused"] != "1" && Tasks.Any(t => t.ActivatorType == ModeSchedule && t.Enabled))
                    _schedTimer.Start();
                if (Tasks.Any(t => t.ActivatorType == ModeTimer && t.Enabled && t.EndTimeUtc > DateTime.UtcNow))
                    _stopTimer.Start();
                if (Tasks.Any(t => t.ActivatorType == ModeIdle && t.Enabled))
                    _idleTimer.Start();
            }

            if (Settings["autoStart"] == "1") StartIdleCountdown();
            if (Settings["runWindowsAuto"] == "1") SetAutoStart(true);

            if (!string.IsNullOrEmpty(Settings["powerPlanSetID"]))
            {
                string restore = Settings["powerPlanID"] ?? "";
                if (restore.Length > 0) RunPowerCfg($"/setactive {restore}", false);
                Settings["powerPlanSetID"] = "";
                Settings["keepAwake"] = "1";
                SaveSettings();
            }

            if (Settings["keepAwake"] == "1") SetKeepAwake(true, Settings["keepScreenOn"] == "1");

            Microsoft.Win32.SystemEvents.TimeChanged += OnSystemTimeChanged;
        }

        private void OnSystemTimeChanged(object? sender, EventArgs e)
        {
            if (Get("timezoneCheck") == "0") return;

            TimeZoneInfo.ClearCachedData();

            bool shifted = false;
            foreach (var task in Tasks)
            {
                if (task.ActivatorType != ModeTimer || task.Completed) continue;
                if (task.LocalTarget == default) continue;

                var expected = task.LocalTarget.ToUniversalTime();
                if (Math.Abs((expected - task.EndTimeUtc).TotalSeconds) < 1) continue;

                task.EndTimeUtc = expected;
                shifted = true;
            }

            if (!shifted) return;

            SaveSettings();
            TasksChanged?.Invoke();
            ErrorReported?.Invoke(Lang.T("msg.timezoneShift"));
        }

        public void Dispose()
        {
            try { Microsoft.Win32.SystemEvents.TimeChanged -= OnSystemTimeChanged; } catch { }

            SetKeepAwake(false, false);
            FlushPendingSave();
            foreach (var t in new[] { _idleTimer, _stopTimer, _schedTimer, _snoozeTimer, _statusTimer, _saveTimer })
                t.Stop();
            StopPowerPlanMonitor();
            StopSound();
        }

        public string Get(string key) => Settings[key] ?? "";
        public bool Flag(string key, bool fallback = false)
        {
            string v = Settings[key] ?? "";
            return v.Length == 0 ? fallback : v == "1";
        }

        public void Set(string key, string value, bool immediate = false)
        {
            Settings[key] = value;
            if (immediate) SaveSettings();
            else { _saveTimer.Stop(); _saveTimer.Start(); }
        }

        public bool ParamShared(int activator, string group) =>
            activator <= 0 || Get($"act{activator}_shared_{group}") != "0";

        public void SetParamShared(int activator, string group, bool shared)
        {
            if (activator > 0) Set($"act{activator}_shared_{group}", shared ? "1" : "0");
        }

        public string Param(int activator, string group, string name)
        {
            if (!ParamShared(activator, group)) return Get($"act{activator}_{name}");

            string common = Get($"shared_{name}");

            return common.Length > 0 || activator <= 0 ? common : Get($"act{activator}_{name}");
        }

        public void SetParam(int activator, string group, string name, string value)
        {
            if (ParamShared(activator, group)) Set($"shared_{name}", value);
            else Set($"act{activator}_{name}", value);
        }

        public void SetFlag(string key, bool value, bool immediate = true) =>
            Set(key, value ? "1" : "0", immediate);

        public void FlushPendingSave()
        {
            if (!_saveTimer.IsEnabled) return;
            _saveTimer.Stop();
            SaveSettings();
        }

        private void LoadSettings()
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            Directory.CreateDirectory(dataDir);
            _configPath = Path.Combine(dataDir, "config.json");

            var defaults = Defaults();
            var loaded = new Dictionary<string, string>();
            if (File.Exists(_configPath))
            {
                try
                {
                    loaded = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        File.ReadAllText(_configPath)) ?? loaded;
                }
                catch { }
            }

            Lang.SetQuiet(loaded.TryGetValue("language", out var lc) && lc.Length > 0 ? lc : Lang.SystemDefault);

            Settings = new NameValueCollection();
            foreach (var kv in defaults)
                Settings[kv.Key] = loaded.TryGetValue(kv.Key, out var v) ? v : kv.Value;
            foreach (var kv in loaded)
                if (Settings[kv.Key] == null) Settings[kv.Key] = kv.Value;

            try { Tasks = JsonConvert.DeserializeObject<List<TaskItem>>(Settings["taskList"] ?? "[]") ?? new(); }
            catch { Tasks = new(); }

            if (!File.Exists(_configPath)) SaveSettings();
        }

        private bool _skipSave;

        public void SkipNextSave()
        {
            _skipSave = true;
            _saveTimer.Stop();
        }

        public void SaveSettings()
        {
            if (_skipSave) return;

            try
            {
                var dict = new Dictionary<string, string>();
                foreach (string? k in Settings.AllKeys)
                {
                    if (k == null) continue;
                    dict[k] = Settings[k] ?? "";
                }
                dict["taskList"] = JsonConvert.SerializeObject(Tasks);

                string? dir = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                string tmp = _configPath + ".tmp";
                File.WriteAllText(tmp, JsonConvert.SerializeObject(dict, Formatting.Indented));
                File.Move(tmp, _configPath, true);
            }
            catch { }
        }

        private static Dictionary<string, string> Defaults() => new()
        {
            ["defaultAction1"] = "0",
            ["defaultAction2"] = "0",
            ["defaultAction3"] = "0",
            ["timer1ActivateBy"] = "00:05:00",
            ["timer2HourActivateBy"] = "10:00",
            ["timer2DateActivateBy"] = DateTime.Now.ToString("dd.MM.yyyy"),
            ["timer2InputMode"] = "duration",
            ["timer2Duration"] = "00:30:00",
            ["timer3HourActivateBy"] = "10:00",
            ["timer3CheckedDays"] = "",
            ["timer3Enabled"] = "0",
            ["timer3LastFired"] = "",
            ["timer3UserPaused"] = "0",
            ["timer3_repeat_type"] = "m",
            ["timer3_repeat_value"] = "30",
            ["runWindowsAuto"] = "0",
            ["autoStart"] = "0",
            ["noMinimalize"] = "0",
            ["RunHost"] = "1",
            ["powerPlanSetID"] = "",
            ["keepAwake"] = "1",
            ["keepScreenOn"] = "1",
            ["powerPlanID"] = "",
            ["protect"] = "0",
            ["userSet"] = "msedge.exe",
            ["userCommandBehaviour"] = "0",
            ["timezoneCheck"] = "1",
            ["noTray"] = "0",
            ["noTimerShow"] = "0",
            ["darkModeEnabled"] = "0",
            ["roundedCorners"] = "0",
            ["accentColor"] = "",
            ["hideTips"] = "0",
            ["soundEveryTask"] = "0",
            ["soundFile"] = "",
            ["noBalloon"] = "0",
            ["taskList"] = "[]",
            ["autoStartQueue"] = "1",
            ["activatorMode"] = "1",
            ["language"] = "",
            ["checkUpdates"] = "1",
            ["skipVersion"] = "",

            ["act1_url"] = "https://google.com",
            ["act2_url"] = "https://google.com",
            ["act3_url"] = "https://google.com",
            ["act1_cmd"] = "", ["act2_cmd"] = "", ["act3_cmd"] = "",
            ["act1_note"] = "", ["act2_note"] = "", ["act3_note"] = "",
            ["act1_sound"] = "", ["act2_sound"] = "", ["act3_sound"] = "",

            ["shared_url"] = "https://google.com", ["shared_cmd"] = "", ["shared_note"] = "",
            ["shared_mail_to"] = "", ["shared_mail_subject"] = "", ["shared_mail_template"] = "ShortText",
            ["shared_mail_body"] = "", ["shared_mail_attachment"] = "", ["shared_mail_command"] = "",

            ["cleanTemp"] = "1", ["cleanWinTemp"] = "1", ["cleanDownloads"] = "0",
            ["cleanWU"] = "1", ["cleanDriverCache"] = "0", ["cleanThumbs"] = "1",
            ["cleanPrefetch"] = "0", ["cleanRecycleBin"] = "0", ["cleanEventLog"] = "0",
            ["cleanCustomFolders"] = "",

            ["ss_format"] = "PNG", ["ss_count"] = "1", ["ss_delay"] = "0",
            ["ss_interval"] = "1000", ["ss_compress"] = "0", ["ss_folder"] = "",

            ["smtp_host"] = "", ["smtp_port"] = "587", ["smtp_security"] = "Auto",
            ["smtp_auth"] = "Password", ["smtp_user"] = "", ["smtp_pass"] = "",
            ["smtp_from"] = "", ["smtp_fromName"] = "",
            ["smtp_oauth_client"] = "", ["smtp_oauth_secret"] = "",
            ["smtp_oauth_refresh"] = "", ["smtp_oauth_url"] = "",

            ["act1_mail_to"] = "", ["act2_mail_to"] = "", ["act3_mail_to"] = "",
            ["act1_mail_subject"] = "", ["act2_mail_subject"] = "", ["act3_mail_subject"] = "",
            ["act1_mail_template"] = "ShortText", ["act2_mail_template"] = "ShortText", ["act3_mail_template"] = "ShortText",
            ["act1_mail_body"] = "", ["act2_mail_body"] = "", ["act3_mail_body"] = "",
            ["act1_mail_attachment"] = "", ["act2_mail_attachment"] = "", ["act3_mail_attachment"] = "",
            ["act1_mail_command"] = "", ["act2_mail_command"] = "", ["act3_mail_command"] = "",
        };

        public static bool TryParseTime(string? text, out TimeSpan value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            return TimeSpan.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out value)
                || TimeSpan.TryParse(text + ":00", System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        public static string FormatRemaining(TimeSpan ts)
        {
            if (ts.TotalSeconds <= 0) return "00:00:00";

            ts = TimeSpan.FromSeconds(Math.Ceiling(ts.TotalSeconds));

            return ts.TotalDays >= 1
                ? $"{(int)ts.TotalDays}d {ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        public static long IdleMilliseconds()
        {
            var info = new NativeMethods.LASTINPUTINFO { cbSize = NativeMethods.LASTINPUTINFO.SizeOf };
            if (!NativeMethods.GetLastInputInfo(ref info)) return 0;
            return unchecked((uint)Environment.TickCount - info.dwTime);
        }
    }
}
