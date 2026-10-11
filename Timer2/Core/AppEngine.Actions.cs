using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TimerSys.Core
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("kernel32.dll")]
        public static extern uint SetThreadExecutionState(uint flags);

        public const uint EsContinuous      = 0x80000000;
        public const uint EsSystemRequired  = 0x00000001;
        public const uint EsDisplayRequired = 0x00000002;

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        public static extern int mciSendString(string command, System.Text.StringBuilder? ret, int len, IntPtr hwnd);

        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));
            [MarshalAs(UnmanagedType.U4)] public int cbSize;
            [MarshalAs(UnmanagedType.U4)] public uint dwTime;
        }
    }

    public sealed partial class AppEngine
    {

        public static readonly string[] ActionIds =
        {
            "Odtwórz dźwięk", "Uruchom polecenie", "Wyłącz monitor", "Uśpienie",
            "Wylogowanie", "Restart", "Wyłączenie komputera", "Hibernacja",
            "Blokada użytkownika", "Oczyść pliki tymczasowe", "Zapisz notatkę na pulpicie",
            "Otwórz stronę WWW", "Opróżnij kosz", "Wykonaj zrzut ekranu",
            "Wyślij wiadomość e-mail"
        };

        internal static readonly Ico[] ActionIcons =
        {
            Ico.Speaker, Ico.Play, Ico.Monitor, Ico.Idle, Ico.Power, Ico.Refresh, Ico.Power,
            Ico.Idle, Ico.Lock, Ico.Sparkle, Ico.Note, Ico.Globe, Ico.Trash, Ico.Camera, Ico.Mail
        };

        public const string ActionEmail = "Wyślij wiadomość e-mail";

        public static int ActionIndexOf(string? id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            int i = Array.IndexOf(ActionIds, id);
            return i >= 0 ? i : Array.IndexOf(Lang.A("actions"), id);
        }

        public static string ActionDisplay(string? id)
        {
            int i = ActionIndexOf(id);
            var names = Lang.A("actions");
            return i >= 0 && i < names.Length ? names[i] : id ?? "";
        }

        public static string ActionIdFromIndex(int idx) =>
            idx >= 0 && idx < ActionIds.Length ? ActionIds[idx] : "";

        public static bool ActionHasSettings(int idx) =>
            idx is 0 or 1 or 9 or 10 or 11 or 13 or 14;

        public void StartIdleCountdown()
        {
            _idleEndTime = DateTime.Now.Add(
                TryParseTime(Get("timer1ActivateBy"), out var ts) ? ts : TimeSpan.FromMinutes(5));
            _idleTimer.Start();
        }

        public void StopIdleCountdown()
        {
            _idleTimer.Stop();
            IdleStatusChanged?.Invoke("");
        }

        public void StopTimerCountdown()
        {
            _stopTimer.Stop();
            TimerStatusChanged?.Invoke("");
        }

        private TimeSpan? IdleThresholdFor(TaskItem t) =>
            TryParseTime(string.IsNullOrEmpty(t.IdleTimeout) ? Get("timer1ActivateBy") : t.IdleTimeout, out var ts)
                ? ts : null;

        private TimeSpan? ConfiguredIdleTime()
        {
            if (TryParseTime(Get("timer1ActivateBy"), out var fromSettings) && fromSettings > TimeSpan.Zero)
                return fromSettings;

            foreach (var task in Tasks.Where(t => t.ActivatorType == ModeIdle && t.Enabled))
            {
                var own = IdleThresholdFor(task);
                if (own != null && own.Value > TimeSpan.Zero) return own;
            }
            return null;
        }

        private void OnIdleTick()
        {
            long idleMs = IdleMilliseconds();
            bool resetMode = Flag("RunHost", true);
            bool anyFired = false;

            if (resetMode)
            {
                var configured = ConfiguredIdleTime();
                if (configured == null)
                {
                    _idleTimer.Stop();
                    IdleStatusChanged?.Invoke("");
                    return;
                }

                if (idleMs < 1000) _idleEndTime = DateTime.Now.Add(configured.Value);

                var ready = Tasks.Where(t => t.ActivatorType == ModeIdle && t.Enabled && !IsEditing(t)).ToList();
                if (ready.Count == 0)
                {
                    IdleStatusChanged?.Invoke("");
                    return;
                }

                var left = _idleEndTime - DateTime.Now;
                if (left.TotalSeconds <= 0)
                {
                    _idleEndTime = DateTime.Now.Add(configured.Value);
                    foreach (var t in ready) ExecuteAction(t.Action, ModeIdle);
                    IdleStatusChanged?.Invoke(Lang.T("msg.actionDone"));
                    return;
                }
                if (!Flag("noTimerShow")) IdleStatusChanged?.Invoke(Lang.T("msg.remaining", FormatRemaining(left)));
                return;
            }

            foreach (var t in Tasks.Where(t => t.ActivatorType == ModeIdle && t.Enabled && !IsEditing(t)).ToList())
            {
                var threshold = IdleThresholdFor(t);
                if (threshold == null || threshold.Value <= TimeSpan.Zero) continue;
                if (idleMs >= (long)threshold.Value.TotalMilliseconds)
                {
                    ExecuteAction(t.Action, ModeIdle);
                    t.Enabled = false;
                    t.Completed = true;
                    anyFired = true;
                }
            }
            if (anyFired) { SaveSettings(); TasksChanged?.Invoke(); }

            if (!Tasks.Any(t => t.ActivatorType == ModeIdle && t.Enabled))
            {
                _idleTimer.Stop();
                IdleStatusChanged?.Invoke(anyFired && !Flag("noTimerShow") ? Lang.T("msg.actionDone") : "");
                return;
            }

            if (Flag("noTimerShow")) return;
            TimeSpan? pending = Tasks
                .Where(t => t.ActivatorType == ModeIdle && t.Enabled)
                .Select(IdleThresholdFor)
                .Where(ts => ts != null)
                .OrderBy(ts => ts!.Value)
                .FirstOrDefault();
            if (pending != null)
            {
                var left = pending.Value - TimeSpan.FromMilliseconds(idleMs);
                IdleStatusChanged?.Invoke(left.TotalSeconds > 0
                    ? Lang.T("msg.remaining", FormatRemaining(left))
                    : Lang.T("msg.ready"));
            }
        }

        private void OnTimerTick()
        {
            var now = DateTime.UtcNow;
            bool anyFired = false;

            foreach (var t in Tasks.Where(t => t.ActivatorType == ModeTimer && t.Enabled && t.EndTimeUtc != default && !IsEditing(t)).ToList())
            {
                if (t.EndTimeUtc > now) continue;
                ExecuteAction(t.Action, ModeTimer);
                t.Enabled = false;
                t.Completed = true;
                anyFired = true;
            }
            if (anyFired) { SaveSettings(); TasksChanged?.Invoke(); }

            var remaining = Tasks.Where(t => t.ActivatorType == ModeTimer && t.Enabled && t.EndTimeUtc > now).ToList();
            if (!remaining.Any())
            {
                _stopTimer.Stop();
                TimerStatusChanged?.Invoke(anyFired && !Flag("noTimerShow") ? Lang.T("msg.actionDone") : "");
                return;
            }

            if (Flag("noTimerShow")) return;
            var nearest = remaining.OrderBy(t => t.EndTimeUtc).First();
            TimerStatusChanged?.Invoke(Lang.T("msg.nextIn", FormatRemaining(nearest.EndTimeUtc - now)));
        }

        private void OnScheduleTick()
        {
            var now = DateTime.Now;
            var current = new TimeSpan(now.Hour, now.Minute, 0);
            bool anyFired = false;

            foreach (var t in Tasks.Where(t => t.ActivatorType == ModeSchedule && t.Enabled && !IsEditing(t)).ToList())
            {
                if (!string.IsNullOrEmpty(t.RepeatIntervalType))
                {
                    double minutes = t.RepeatIntervalType switch
                    {
                        "h" => t.RepeatIntervalValue * 60.0,
                        "d" => t.RepeatIntervalValue * 1440.0,
                        _ => t.RepeatIntervalValue
                    };
                    if ((now - t.LastFired).TotalMinutes >= minutes)
                    {
                        _firingTask = t;
                        ExecuteAction(t.Action, ModeSchedule);
                        _firingTask = null;
                        t.LastFired = now;
                        anyFired = true;
                    }
                    continue;
                }

                string hourStr = string.IsNullOrEmpty(t.ScheduleHour) ? Get("timer3HourActivateBy") : t.ScheduleHour;
                if (!TryParseTime(hourStr, out var target)) continue;
                if (Math.Abs((current - target).TotalMinutes) >= 0.5) continue;
                if (t.LastFired.Date == now.Date) continue;

                string days = string.IsNullOrEmpty(t.ScheduleDays) ? Get("timer3CheckedDays") : t.ScheduleDays;
                if (!IsScheduledOn(days, t.LastFired, now)) continue;

                _firingTask = t;
                ExecuteAction(t.Action, ModeSchedule);
                _firingTask = null;
                t.LastFired = now;
                anyFired = true;
            }

            if (anyFired)
            {
                Settings["timer3LastFired"] = now.ToString("yyyy-MM-dd");
                SaveSettings();
                TasksChanged?.Invoke();
            }

            ScheduleStatusChanged?.Invoke(ScheduleStatusText());
        }

        private void OnSnoozeTick()
        {
            var left = _snoozeEnd - DateTime.Now;
            if (left.TotalSeconds > 0)
            {
                SnoozeStatusChanged?.Invoke(Lang.T("snooze.counting", left.ToString(@"mm\:ss")));
                return;
            }
            _snoozeTimer.Stop();
            SnoozeStatusChanged?.Invoke("");

            if (Tasks.RemoveAll(t => t.OneOff) > 0)
            {
                SaveSettings();
                TasksChanged?.Invoke();
            }

            ExecuteAction(ActionIdFromIndex(int.TryParse(Get("defaultAction3"), out int a) ? a : 0), ModeSchedule);
        }

        public void StartSnooze(int minutes)
        {
            _snoozeEnd = DateTime.Now.AddMinutes(minutes);
            _snoozeTimer.Start();

            string action = ActionIdFromIndex(int.TryParse(Get("defaultAction3"), out int a) ? a : 0);
            Tasks.RemoveAll(t => t.OneOff);
            Tasks.Add(new TaskItem
            {
                Id = SnoozeTaskId,
                ActivatorType = ModeTimer,
                Action = action,
                Enabled = true,
                OneOff = true,
                Duration = TimeSpan.FromMinutes(minutes),
                LocalTarget = _snoozeEnd,
                EndTimeUtc = _snoozeEnd.ToUniversalTime()
            });
            SaveSettings();
            TasksChanged?.Invoke();
        }

        private const string SnoozeTaskId = "oneoff00";

        public string ScheduleStatusText()
        {
            if (!_schedTimer.IsEnabled) return Lang.T("sched.inactive");
            var next = NextScheduleFireTime();
            if (next == null) return Lang.T("sched.noDate");
            var diff = next.Value - DateTime.Now;
            string ds = diff.TotalHours >= 24
                ? $"{(int)diff.TotalDays}d {diff.Hours:D2}h {diff.Minutes:D2}m"
                : $"{(int)diff.TotalHours:D2}h {diff.Minutes:D2}m {diff.Seconds:D2}s";
            return Lang.T("sched.next", next.Value.ToString("HH:mm"), ds);
        }

        public DateTime? NextScheduleFireTime()
        {
            if (!TryParseTime(Get("timer3HourActivateBy"), out var target)) return null;
            string days = Get("timer3CheckedDays");
            if (days.Length == 0) return null;
            DateTime.TryParse(Get("timer3LastFired"), out DateTime lastFired);

            for (int d = 0; d <= 8; d++)
            {
                var candidate = DateTime.Today.AddDays(d).Add(target);
                if (candidate <= DateTime.Now) continue;
                if (IsScheduledOn(days, lastFired, candidate)) return candidate;
            }
            return null;
        }

        public static bool IsScheduledOn(string daysStr, DateTime lastFired, DateTime day)
        {
            if (string.IsNullOrEmpty(daysStr)) return false;
            var dow = day.DayOfWeek;
            foreach (var part in daysStr.Split(','))
            {
                if (!int.TryParse(part.Trim(), out int idx)) continue;
                double gap = (day.Date - lastFired.Date).TotalDays;
                switch (idx)
                {
                    case 0: return true;
                    case 1: if (gap >= 2) return true; break;
                    case 2: if (gap >= 3) return true; break;
                    case 3: if (gap >= 4) return true; break;
                    case 4: if (gap >= 5) return true; break;
                    case 5: if (gap >= 6) return true; break;
                    case 6: if (gap >= 7) return true; break;
                    case 7: if (dow is DayOfWeek.Friday or DayOfWeek.Saturday or DayOfWeek.Sunday) return true; break;
                    case 8: if (dow == DayOfWeek.Monday) return true; break;
                    case 9: if (dow == DayOfWeek.Tuesday) return true; break;
                    case 10: if (dow == DayOfWeek.Wednesday) return true; break;
                    case 11: if (dow == DayOfWeek.Thursday) return true; break;
                    case 12: if (dow == DayOfWeek.Friday) return true; break;
                    case 13: if (dow == DayOfWeek.Saturday) return true; break;
                    case 14: if (dow == DayOfWeek.Sunday) return true; break;
                    case 15: if (dow is not DayOfWeek.Saturday and not DayOfWeek.Sunday) return true; break;
                }
            }
            return false;
        }

        public string ScheduleDescription()
        {
            if (_firingTask != null && !string.IsNullOrEmpty(_firingTask.RepeatIntervalType))
            {
                string unit = Lang.T(_firingTask.RepeatIntervalType switch
                { "h" => "unit.hours", "d" => "unit.days", _ => "unit.minutes" });
                return Lang.T("val.every", _firingTask.RepeatIntervalValue, unit);
            }
            var labels = Lang.A("scheduleDays");
            var days = Get("timer3CheckedDays")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => int.TryParse(p.Trim(), out int i) && i >= 0 && i < labels.Length ? labels[i] : null)
                .Where(s => s != null);
            return string.Join(", ", days) + Lang.T("sched.descAt") + Get("timer3HourActivateBy");
        }

        public void AddTask(TaskItem task)
        {
            Tasks.Add(task);
            SaveSettings();
            TasksChanged?.Invoke();

            switch (task.ActivatorType)
            {
                case ModeIdle:
                    if (!_idleTimer.IsEnabled) StartIdleCountdown();
                    break;
                case ModeTimer:
                    _stopTimer.Start();
                    break;
                case ModeSchedule:
                    if (Get("timer3UserPaused") != "1") _schedTimer.Start();
                    break;
            }
        }

        public void RemoveTask(string id)
        {
            var task = Tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return;
            Tasks.Remove(task);
            SyncTimersAfterChange(task.ActivatorType);
            SaveSettings();
            TasksChanged?.Invoke();
        }

        public void ToggleTask(string id)
        {
            var task = Tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return;
            task.Enabled = !task.Enabled;
            if (task.Enabled) task.Completed = false;

            if (task.Enabled)
            {
                switch (task.ActivatorType)
                {
                    case ModeIdle: if (!_idleTimer.IsEnabled) StartIdleCountdown(); break;
                    case ModeTimer:

                        if (task.Paused > TimeSpan.Zero)
                        {
                            task.EndTimeUtc = DateTime.UtcNow.Add(task.Paused);
                            task.LocalTarget = task.EndTimeUtc.ToLocalTime();
                            task.Paused = TimeSpan.Zero;
                        }
                        if (task.EndTimeUtc > DateTime.UtcNow) _stopTimer.Start();
                        break;
                    case ModeSchedule:
                        if (task.Paused > TimeSpan.Zero && task.RepeatIntervalType.Length > 0)
                        {
                            task.LastFired = DateTime.Now.Add(task.Paused) - IntervalOf(task);
                            task.Paused = TimeSpan.Zero;
                        }
                        if (Get("timer3UserPaused") != "1") _schedTimer.Start();
                        break;
                }
            }
            else
            {

                if (!task.Completed)
                {
                    TimeSpan left = task.ActivatorType switch
                    {
                        ModeTimer => task.EndTimeUtc - DateTime.UtcNow,
                        ModeSchedule when task.RepeatIntervalType.Length > 0 =>
                            task.LastFired.Add(IntervalOf(task)) - DateTime.Now,
                        _ => TimeSpan.Zero
                    };
                    task.Paused = left > TimeSpan.Zero ? left : TimeSpan.Zero;
                }
                SyncTimersAfterChange(task.ActivatorType);
            }

            SaveSettings();
            TasksChanged?.Invoke();
        }

        private static TimeSpan IntervalOf(TaskItem task) => task.RepeatIntervalType switch
        {
            "h" => TimeSpan.FromHours(task.RepeatIntervalValue),
            "d" => TimeSpan.FromDays(task.RepeatIntervalValue),
            _ => TimeSpan.FromMinutes(task.RepeatIntervalValue)
        };

        public bool RestartTask(string id)
        {
            var task = Tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return false;

            task.Completed = false;
            task.Enabled = true;

            switch (task.ActivatorType)
            {
                case ModeTimer:
                {

                    if (task.Duration <= TimeSpan.Zero) return false;
                    task.LocalTarget = DateTime.Now.Add(task.Duration);
                    task.EndTimeUtc = task.LocalTarget.ToUniversalTime();
                    _stopTimer.Start();
                    break;
                }
                case ModeIdle:
                    StartIdleCountdown();
                    break;
                case ModeSchedule:
                    task.LastFired = default;
                    if (Get("timer3UserPaused") != "1") _schedTimer.Start();
                    break;
            }

            SaveSettings();
            TasksChanged?.Invoke();
            return true;
        }

        private void SyncTimersAfterChange(int activatorType)
        {
            switch (activatorType)
            {
                case ModeIdle:
                    if (!Tasks.Any(t => t.ActivatorType == ModeIdle && t.Enabled)) StopIdleCountdown();
                    break;
                case ModeTimer:
                    if (!Tasks.Any(t => t.ActivatorType == ModeTimer && t.Enabled && t.EndTimeUtc > DateTime.UtcNow))
                        StopTimerCountdown();
                    break;
                case ModeSchedule:
                    if (!Tasks.Any(t => t.ActivatorType == ModeSchedule && t.Enabled))
                    {
                        _schedTimer.Stop();
                        ScheduleStatusChanged?.Invoke(Lang.T("sched.inactive"));
                    }
                    break;
            }
        }

        public void SetScheduleRunning(bool running)
        {
            if (running)
            {
                if (Tasks.Any(t => t.ActivatorType == ModeSchedule && t.Enabled)) _schedTimer.Start();
                Settings["timer3UserPaused"] = "0";
            }
            else
            {
                _schedTimer.Stop();
                Settings["timer3UserPaused"] = "1";
            }
            Settings["timer3Enabled"] = _schedTimer.IsEnabled ? "1" : "0";
            SaveSettings();
            ScheduleStatusChanged?.Invoke(ScheduleStatusText());
        }

        public string TaskStatusText(TaskItem t)
        {
            if (!t.Enabled)
            {

                if (!t.Completed && t.Paused > TimeSpan.Zero &&
                    (t.ActivatorType == ModeTimer ||
                     (t.ActivatorType == ModeSchedule && t.RepeatIntervalType.Length > 0)))
                    return FormatRemaining(t.Paused);
                return Lang.T(t.Completed ? "val.done" : "val.paused");
            }

            switch (t.ActivatorType)
            {
                case ModeIdle:
                {
                    var threshold = IdleThresholdFor(t);
                    if (!_idleTimer.IsEnabled || threshold == null) return Lang.T("val.waiting");
                    var left = threshold.Value - TimeSpan.FromMilliseconds(IdleMilliseconds());
                    return left.TotalSeconds > 0 ? FormatRemaining(left) : Lang.T("val.soon");
                }
                case ModeTimer:
                {
                    if (t.EndTimeUtc == default) return "-";
                    var left = t.EndTimeUtc - DateTime.UtcNow;
                    return left.TotalSeconds > 0 ? FormatRemaining(left) : Lang.T("val.expired");
                }
                case ModeSchedule:
                {
                    if (!string.IsNullOrEmpty(t.RepeatIntervalType))
                    {
                        double minutes = t.RepeatIntervalType switch
                        { "h" => t.RepeatIntervalValue * 60.0, "d" => t.RepeatIntervalValue * 1440.0, _ => t.RepeatIntervalValue };
                        var left = t.LastFired.AddMinutes(minutes) - DateTime.Now;
                        string unit = Lang.T(t.RepeatIntervalType switch
                        { "h" => "unit.hour", "d" => "unit.day", _ => "unit.min" });
                        return Lang.T("val.every", t.RepeatIntervalValue, unit) + ": " +
                               (left.TotalSeconds > 0 ? FormatRemaining(left) : Lang.T("val.soon"));
                    }
                    string hourStr = string.IsNullOrEmpty(t.ScheduleHour) ? Get("timer3HourActivateBy") : t.ScheduleHour;
                    if (!TryParseTime(hourStr, out var target)) return "-";
                    string days = string.IsNullOrEmpty(t.ScheduleDays) ? Get("timer3CheckedDays") : t.ScheduleDays;
                    for (int d = 0; d <= 8; d++)
                    {
                        var candidate = DateTime.Today.AddDays(d).Add(target);
                        if (candidate <= DateTime.Now) continue;
                        if (IsScheduledOn(days, t.LastFired, candidate)) return candidate.ToString("dd.MM HH:mm");
                    }
                    return "-";
                }
                default: return "-";
            }
        }

        public string ModeName(int activatorType) => Lang.T(activatorType switch
        {
            ModeTimer => "mode.timer",
            ModeSchedule => "mode.schedule",
            _ => "mode.idle"
        });

        public void ExecuteAction(string actionId, int activator)
        {
            NotifyAction(actionId, activator);

            string userCommand = Param(activator, "param", "cmd");
            if (userCommand.Length == 0)
                userCommand = System.Net.WebUtility.HtmlDecode(Get("userSet"));

            switch (actionId)
            {
                case "Odtwórz dźwięk":
                    PlaySound(ResolveSoundFor(activator));
                    break;

                case "Uruchom polecenie":
                    if (string.IsNullOrWhiteSpace(userCommand)) break;
                    RunProcess(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c " + userCommand,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }, Get("userCommandBehaviour") == "1");
                    break;

                case "Wyłącz monitor":
                    TurnOffMonitor();
                    break;

                case "Uśpienie":           RunProcess("Rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0"); break;
                case "Wylogowanie":        RunProcess("shutdown", "/l"); break;
                case "Restart":            RunProcess("shutdown", "/r /t 0"); break;
                case "Wyłączenie komputera": RunProcess("shutdown", "/s /t 0"); break;
                case "Hibernacja":         RunProcess("shutdown", "/h"); break;
                case "Blokada użytkownika": RunProcess("Rundll32.exe", "user32.dll,LockWorkStation"); break;

                case "Oczyść pliki tymczasowe":
                    Task.Run(CleanTemporaryFiles);
                    break;

                case "Zapisz notatkę na pulpicie":
                    SaveDesktopNote(Param(activator, "param", "note"));
                    break;

                case "Otwórz stronę WWW":
                {
                    string url = Param(activator, "param", "url");
                    if (string.IsNullOrEmpty(url)) url = userCommand.Trim();
                    if (url.Length == 0) break;
                    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        url = "https://" + url;
                    RunProcess(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                    break;
                }

                case "Opróżnij kosz":
                    RunProcess(new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                    break;

                case "Wykonaj zrzut ekranu":
                    Task.Run(TakeScreenshots);
                    break;

                case ActionEmail:
                    SendConfiguredEmail(activator);
                    break;
            }

            if (Flag("soundEveryTask") && actionId != "Odtwórz dźwięk")
                PlaySound(ResolveSoundFor(activator));
        }

        private void NotifyAction(string actionId, int activator)
        {
            if (Flag("noBalloon")) return;
            string now = DateTime.Now.ToString("HH:mm");
            string today = DateTime.Now.ToString("dd.MM.yyyy");
            string shown = ActionDisplay(actionId);

            string body = activator switch
            {
                ModeIdle => Lang.T("toast.idle", shown, now, today,
                    TryParseTime(Get("timer1ActivateBy"), out var ts) ? (int)ts.TotalMinutes : 0),
                ModeTimer => Lang.T("toast.timer", shown, now, today),
                ModeSchedule => Lang.T("toast.schedule", shown, now, today, ScheduleDescription()),
                _ => Lang.T("toast.generic", shown, now, today)
            };

            ActionPerformed?.Invoke(new ActionNotification
            {
                ActionId = actionId,
                Activator = activator,
                Body = body
            });
        }

        private void RunProcess(string file, string args) => RunProcess(new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        private void RunProcess(ProcessStartInfo psi, bool wait = false)
        {
            try
            {
                using var p = Process.Start(psi);
                if (wait) p?.WaitForExit();
            }
            catch (Exception ex)
            {
                ErrorReported?.Invoke(Lang.T("msg.processError", psi.FileName, ex.Message));
            }
        }

        private static void TurnOffMonitor()
        {
            Task.Run(() =>
            {
                System.Threading.Thread.Sleep(300);
                try
                {
                    IntPtr broadcast = new IntPtr(0xFFFF);
                    SendMessage(broadcast, 0x0112, new IntPtr(0xF170), new IntPtr(2));
                }
                catch { }
            });
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private System.Media.SoundPlayer? _wavPlayer;

        public string ResolveSoundFor(int activator)
        {
            string value = activator > 0 ? Get($"act{activator}_sound").Trim() : "";
            if (value.Length == 0) value = Get("soundFile").Trim();
            if (value.Length == 0) value = SoundCatalog.DefaultFile;
            return SoundCatalog.ResolvePath(value);
        }

        public void PlaySound(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                string builtIn = SoundCatalog.ResolvePath(SoundCatalog.DefaultFile);
                if (File.Exists(builtIn)) path = builtIn;
            }
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                string mediaDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "..", "Media");
                foreach (var fb in new[] { "tada.wav", "Alarm01.wav", "chimes.wav", "ding.wav" })
                {
                    string candidate = Path.Combine(mediaDir, fb);
                    if (File.Exists(candidate)) { path = candidate; break; }
                }
            }
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            if (Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _wavPlayer?.Dispose();
                    _wavPlayer = new System.Media.SoundPlayer(path);
                    _wavPlayer.Play();
                }
                catch { }
            }
            else
            {
                try
                {
                    NativeMethods.mciSendString("close mci_sound", null, 0, IntPtr.Zero);
                    NativeMethods.mciSendString($"open \"{path}\" alias mci_sound", null, 0, IntPtr.Zero);
                    NativeMethods.mciSendString("play mci_sound", null, 0, IntPtr.Zero);
                }
                catch { }
            }
        }

        public void StopSound()
        {
            try
            {
                NativeMethods.mciSendString("stop mci_sound", null, 0, IntPtr.Zero);
                NativeMethods.mciSendString("close mci_sound", null, 0, IntPtr.Zero);
            }
            catch { }
            try { _wavPlayer?.Stop(); _wavPlayer?.Dispose(); _wavPlayer = null; } catch { }
        }

        public string SoundDisplayFor(int activator)
        {
            string v = activator > 0 ? Get($"act{activator}_sound").Trim() : "";
            if (v.Length == 0) v = Get("soundFile").Trim();
            if (v.Length == 0) v = SoundCatalog.DefaultFile;
            string name = SoundCatalog.DisplayName(v);
            return name.Length > 0 ? name : Path.GetFileName(v);
        }

        private void SaveDesktopNote(string note)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string file = Path.Combine(desktop, $"{Lang.T("note.file")}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                string content = Lang.T("note.header") + "\r\n" +
                                 Lang.T("note.when", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")) + "\r\n" +
                                 Lang.T("note.who", Environment.MachineName, Environment.UserName) + "\r\n";
                if (note.Length > 0) content += "\r\n" + Lang.T("note.body") + "\r\n" + note + "\r\n";
                File.WriteAllText(file, content, System.Text.Encoding.UTF8);
            }
            catch { }
        }

        private void TakeScreenshots()
        {
            try
            {
                string fmt = Get("ss_format").Length > 0 ? Get("ss_format") : "PNG";
                int count = int.TryParse(Get("ss_count"), out int c) ? Math.Max(1, c) : 1;
                int delay = int.TryParse(Get("ss_delay"), out int d) ? d : 0;
                int interval = int.TryParse(Get("ss_interval"), out int i) ? i : 1000;
                bool compress = Get("ss_compress") == "1";
                string folder = Get("ss_folder");
                if (folder.Length == 0) folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                Directory.CreateDirectory(folder);

                var imgFmt = fmt.ToUpperInvariant() switch
                {
                    "JPG" or "JPEG" => System.Drawing.Imaging.ImageFormat.Jpeg,
                    "BMP" => System.Drawing.Imaging.ImageFormat.Bmp,
                    "GIF" => System.Drawing.Imaging.ImageFormat.Gif,
                    _ => System.Drawing.Imaging.ImageFormat.Png
                };
                string ext = fmt.ToLowerInvariant() == "jpeg" ? "jpg" : fmt.ToLowerInvariant();

                if (delay > 0) System.Threading.Thread.Sleep(delay);

                var bounds = System.Windows.Forms.Screen.PrimaryScreen?.Bounds
                             ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);
                var files = new List<string>();

                for (int n = 0; n < count; n++)
                {
                    if (n > 0) System.Threading.Thread.Sleep(interval);
                    using var bmp = new System.Drawing.Bitmap(bounds.Width, bounds.Height);
                    using var g = System.Drawing.Graphics.FromImage(bmp);
                    g.CopyFromScreen(bounds.Location, System.Drawing.Point.Empty, bounds.Size);
                    string path = Path.Combine(folder, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.{ext}");
                    bmp.Save(path, imgFmt);
                    files.Add(path);
                }

                if (compress && files.Count > 0)
                {
                    string zip = Path.Combine(folder, $"screenshots_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                    using var archive = new ZipArchive(File.Create(zip), ZipArchiveMode.Create);
                    foreach (var f in files) { archive.CreateEntryFromFile(f, Path.GetFileName(f)); File.Delete(f); }
                }
            }
            catch (Exception ex)
            {
                ErrorReported?.Invoke(Lang.T("msg.screenshotError", ex.Message));
            }
        }

        private void CleanTemporaryFiles()
        {
            int deleted = 0;

            void DeleteFiles(string dir, string pattern = "*")
            {
                if (!Directory.Exists(dir)) return;
                try
                {
                    foreach (var f in Directory.EnumerateFiles(dir, pattern))
                        try { File.Delete(f); deleted++; } catch { }
                }
                catch { }
            }

            void DeleteDir(string dir)
            {
                DeleteFiles(dir);
                if (!Directory.Exists(dir)) return;
                try
                {
                    foreach (var sub in Directory.EnumerateDirectories(dir))
                        try { Directory.Delete(sub, true); deleted++; } catch { }
                }
                catch { }
            }

            try
            {
                string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                if (Get("cleanTemp") != "0") DeleteDir(Path.GetTempPath());
                if (Get("cleanWinTemp") != "0") DeleteDir(Path.Combine(win, "Temp"));
                if (Get("cleanDownloads") == "1")
                    DeleteFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
                if (Get("cleanWU") == "1") DeleteDir(Path.Combine(win, "SoftwareDistribution", "Download"));
                if (Get("cleanDriverCache") == "1") DeleteDir(Path.Combine(win, "System32", "DriverStore", "Temp"));
                if (Get("cleanThumbs") == "1")
                    DeleteFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Microsoft", "Windows", "Explorer"), "thumbcache_*.db");
                if (Get("cleanPrefetch") == "1") DeleteFiles(Path.Combine(win, "Prefetch"), "*.pf");
                if (Get("cleanRecycleBin") == "1")
                    RunProcess(new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue\"",
                        CreateNoWindow = true, UseShellExecute = false
                    });
                if (Get("cleanEventLog") == "1")
                    RunProcess(new ProcessStartInfo
                    {
                        FileName = "wevtutil.exe", Arguments = "cl System",
                        CreateNoWindow = true, UseShellExecute = false
                    }, wait: true);

                foreach (var dir in Get("cleanCustomFolders").Split(';', StringSplitOptions.RemoveEmptyEntries))
                    DeleteDir(dir.Trim());
            }
            catch { }

            ErrorReported?.Invoke(Lang.T("msg.cleanupDone", deleted));
        }

        public string CleanupSummary()
        {
            var map = new (string Key, string LabelKey)[]
            {
                ("cleanTemp", "clean.userTemp"), ("cleanWinTemp", "clean.winTemp"),
                ("cleanDownloads", "clean.downloads"), ("cleanWU", "clean.wu"),
                ("cleanDriverCache", "clean.driver"), ("cleanThumbs", "clean.thumbs"),
                ("cleanPrefetch", "clean.prefetch"), ("cleanRecycleBin", "clean.bin"),
                ("cleanEventLog", "clean.log"),
            };
            var enabled = map.Where(x => Get(x.Key) != "0").Select(x => Lang.T(x.LabelKey));
            string result = string.Join(", ", enabled);
            return result.Length > 0 ? result : Lang.T("clean.none");
        }

        private void SendConfiguredEmail(int activator)
        {
            int n = activator > 0 ? activator : 1;

            var cfg = SmtpSettings.Load(Settings);
            var template = Enum.TryParse<MailTemplate>(Param(n, "mail", "mail_template"), out var t)
                ? t : MailTemplate.ShortText;
            string to = Param(n, "mail", "mail_to").Trim();
            string subject = Param(n, "mail", "mail_subject").Trim();
            string body = Param(n, "mail", "mail_body");
            string attachment = Param(n, "mail", "mail_attachment").Trim();
            string command = Param(n, "mail", "mail_command").Trim();
            string name = ActionDisplay(ActionEmail);

            Task.Run(() =>
            {
                string? error;
                try
                {
                    var mail = EmailSender.Compose(template, subject, body, attachment, command, name);
                    error = EmailSender.Send(cfg, to, mail);
                }
                catch (Exception ex) { error = ex.Message; }

                if (error != null)
                    ErrorReported?.Invoke(Lang.T("mail.sendFailed", MailProviders.Describe(error, cfg.Host)));
            });
        }

        public void SetAutoStart(bool enabled)
        {
            try
            {
                string startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcut = Path.Combine(startup, "Wyłącznik czasowy.lnk");
                if (enabled)
                {
                    if (!File.Exists(shortcut))
                        ShellLink.Create(shortcut, Environment.ProcessPath ?? "", "--minimized");
                }
                else if (File.Exists(shortcut)) File.Delete(shortcut);
            }
            catch (Exception ex)
            {
                ErrorReported?.Invoke(Lang.T("msg.autostartError", ex.Message));
            }
        }

        private System.Threading.Timer? _powerMonitor;
        private string _powerPlanGuid = "";
        private static readonly Regex GuidRegex = new(
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled);

        public void SetKeepAwake(bool enabled, bool keepScreenOn)
        {
            _keepAwake = enabled;
            _keepScreenOn = keepScreenOn;
            ApplyKeepAwake();

            if (enabled)
            {
                _keepAwakeTimer ??= new System.Threading.Timer(
                    _ => ApplyKeepAwake(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            }
            else
            {
                _keepAwakeTimer?.Dispose();
                _keepAwakeTimer = null;
            }
        }

        private bool _keepAwake;
        private bool _keepScreenOn;
        private System.Threading.Timer? _keepAwakeTimer;

        private void ApplyKeepAwake()
        {
            try
            {
                uint flags = NativeMethods.EsContinuous;
                if (_keepAwake)
                {
                    flags |= NativeMethods.EsSystemRequired;
                    if (_keepScreenOn) flags |= NativeMethods.EsDisplayRequired;
                }
                NativeMethods.SetThreadExecutionState(flags);
            }
            catch { }
        }

        public static string RunPowerCfg(string arguments, bool captureOutput)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg", Arguments = arguments,
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = captureOutput, RedirectStandardError = captureOutput
                };
                using var proc = Process.Start(psi);
                if (proc == null) return "";
                string output = captureOutput ? proc.StandardOutput.ReadToEnd() : "";
                proc.WaitForExit(15000);
                return output;
            }
            catch { return ""; }
        }

        public static string CurrentPowerPlanGuid()
        {
            var m = GuidRegex.Match(RunPowerCfg("/GETACTIVESCHEME", true));
            return m.Success ? m.Value : "";
        }

        public void StartPowerPlanMonitor(string planGuid)
        {
            StopPowerPlanMonitor();
            _powerPlanGuid = planGuid;
            _powerMonitor = new System.Threading.Timer(_ =>
            {
                try
                {
                    if (_powerPlanGuid.Length == 0) return;
                    string active = CurrentPowerPlanGuid();
                    if (active.Length > 0 && !string.Equals(active, _powerPlanGuid, StringComparison.OrdinalIgnoreCase))
                        RunPowerCfg($"/setactive {_powerPlanGuid}", false);
                }
                catch { }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        public void StopPowerPlanMonitor()
        {
            _powerPlanGuid = "";
            _powerMonitor?.Dispose();
            _powerMonitor = null;
        }

        public static void EnsureDataFiles()
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            Directory.CreateDirectory(dataDir);
            var asm = System.Reflection.Assembly.GetExecutingAssembly();

            foreach (var fn in new[] { "plan.pow", "w7restorePlan.ps1", "fixSettings.ps1" })
            {
                string dest = Path.Combine(dataDir, fn);
                if (File.Exists(dest)) continue;
                using var stream = asm.GetManifestResourceStream($"TimerSys.data.{fn}");
                if (stream == null) continue;
                using var fs = File.Create(dest);
                stream.CopyTo(fs);
            }

            HelpFile.Ensure();

            SoundCatalog.EnsureExtracted();

            string soundsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds");
            Directory.CreateDirectory(soundsDir);
            string soundDest = Path.Combine(soundsDir, "sound.wma");
            if (!File.Exists(soundDest))
            {
                using var snd = asm.GetManifestResourceStream("TimerSys.data.sound.wma");
                if (snd != null)
                {
                    using var fs = File.Create(soundDest);
                    snd.CopyTo(fs);
                }
            }

            try
            {
                string versionFile = Path.Combine(dataDir, "version.txt");
                string current = AppVersion;
                if (!File.Exists(versionFile) || File.ReadAllText(versionFile).Trim() != current)
                    File.WriteAllText(versionFile, current);
            }
            catch { }
        }

        public static string AppVersion
        {
            get
            {
                try
                {
                    var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                    return v == null ? "1.0.7" : $"{v.Major}.{v.Minor}.{v.Build}";
                }
                catch { return "1.0.7"; }
            }
        }
    }
}
