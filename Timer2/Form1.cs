namespace Timer2
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using static System.Windows.Forms.DataFormats;
    using static System.Windows.Forms.VisualStyles.VisualStyleElement;
    using TextBox = TextBox;
    using System.IO;
    using System.IO.Compression;
    using System.Collections.Specialized;
    using Microsoft.Win32;
    using System.Security.Principal;
    using Microsoft.VisualBasic.ApplicationServices;
    using System.Threading;
    using TimerSys;
    using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;
    using System.Reflection.Emit;
    using System.Net;
    using System.Globalization;
    using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
    using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
    using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer timer1 = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer timer2 = new System.Windows.Forms.Timer();
        private DateTime timer1EndTime;
        private DateTime timer2EndTime;
        private DateTime _timer2LocalTarget;
        private int _lastSecond1 = -1;
        private int _lastSecond2 = -1;
        private string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "config.json");
        private NameValueCollection appSettings = new NameValueCollection();
        private System.Windows.Forms.Timer timer3 = new System.Windows.Forms.Timer();
        private int _lastFiredScheduleMinute = -1;
        private DateTime _lastFiredDate = DateTime.MinValue;
        private System.Windows.Forms.Timer _timerSnooze = new System.Windows.Forms.Timer { Interval = 500 };
        private DateTime _snoozeEndTime;
        private System.Windows.Forms.Label? _lblScheduleStatus;
        private System.Windows.Forms.Label? _lblSnoozeStatus;
        private System.Windows.Forms.Button? _btnActSettings1;
        private System.Windows.Forms.Button? _btnActSettings2;
        private System.Windows.Forms.Button? _btnActSettings3;
        private System.Windows.Forms.ToolTip _actionTip = new System.Windows.Forms.ToolTip();
        private System.Windows.Forms.Timer _timerStatusUI = new System.Windows.Forms.Timer { Interval = 1000 };
        private ToolStripMenuItem? _menuScheduleToggle;
        private List<TaskEntry> _taskList = new();
        private int _taskListFilter = 0;
        private TaskEntry? _currentlyFiringTask = null;
        private readonly System.Windows.Forms.Timer _configSaveTimer =
            new System.Windows.Forms.Timer { Interval = 600 };

        private bool _initializing = true;

        private bool _forceExit;

        private readonly List<IDisposable> _disposables = new();

        private readonly List<Form> _activeToasts = new();

        private const int SCHEDULE_INTERVAL_IDX = 16;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern uint ExtractIconEx(string szFileName, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLinkCoClass { }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        [System.Runtime.InteropServices.Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cch);
            void SetDescription([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cch);
            void SetWorkingDirectory([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cch);
            void SetArguments([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath, int cch, out int piIcon);
            void SetIconLocation([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszFile);
        }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        [System.Runtime.InteropServices.Guid("0000010b-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            [System.Runtime.InteropServices.PreserveSig] int IsDirty();
            void Load([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszFileName, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] out string ppszFileName);
        }

        private static void CreateShortcutNative(string shortcutPath, string targetPath, string arguments)
        {
            var link = (IShellLinkW)new ShellLinkCoClass();
            link.SetPath(targetPath);
            link.SetArguments(arguments ?? "");
            link.SetWorkingDirectory(Path.GetDirectoryName(targetPath) ?? "");
            ((IPersistFile)link).Save(shortcutPath, false);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static Icon? ExtractIconFromExe(string file, int number, bool largeIcon)
        {
            IntPtr[] large = new IntPtr[1];
            IntPtr[] small = new IntPtr[1];
            try
            {
                if (ExtractIconEx(file, number, large, small, 1) == 0) return null;
                IntPtr handle = largeIcon ? large[0] : small[0];
                if (handle == IntPtr.Zero) return null;
                using var tmp = Icon.FromHandle(handle);
                return (Icon)tmp.Clone();
            }
            catch { return null; }
            finally
            {
                if (large[0] != IntPtr.Zero) DestroyIcon(large[0]);
                if (small[0] != IntPtr.Zero) DestroyIcon(small[0]);
            }
        }

        private static bool TryParseTime(string? text, out TimeSpan value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            return TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out value)
                || TimeSpan.TryParse(text + ":00", CultureInfo.InvariantCulture, out value);
        }

        private static string FormatRemaining(TimeSpan ts)
        {
            if (ts.TotalSeconds <= 0) return "00:00:00";
            return ts.TotalDays >= 1
                ? $"{(int)ts.TotalDays}d {ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        private static long GetIdleMilliseconds()
        {
            var info = new LASTINPUTINFO { cbSize = LASTINPUTINFO.SizeOf };
            if (!GetLastInputInfo(ref info)) return 0;

            return unchecked((uint)Environment.TickCount - info.dwTime);
        }

        private static Image? LoadImageSafe(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                using var src = Image.FromFile(path);
                return new Bitmap(src);
            }
            catch { return null; }
        }

        private bool TryStartProcess(ProcessStartInfo psi, bool waitForExit = false)
        {
            try
            {
                using var p = Process.Start(psi);
                if (waitForExit) p?.WaitForExit();
                return true;
            }
            catch (Exception ex)
            {
                ShowActionError(psi.FileName, ex);
                return false;
            }
        }

        private bool TryStartProcess(string fileName, string arguments = "")
            => TryStartProcess(new ProcessStartInfo
            {
                FileName        = fileName,
                Arguments       = arguments,
                UseShellExecute = false,
                CreateNoWindow  = true
            });

        private void ShowActionError(string what, Exception ex)
        {
            void Show() => MessageBox.Show(
                Lang.T("msg.processError", what, ex.Message),
                Lang.T("msg.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            try
            {
                if (InvokeRequired) BeginInvoke(new Action(Show));
                else Show();
            }
            catch { }
        }

        public static string systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
        public static string shell32Path = Path.Combine(systemDirectory, "shell32.dll");
        public static string appDirectory =
            Path.GetDirectoryName(Application.ExecutablePath) ?? AppDomain.CurrentDomain.BaseDirectory;

        public Form1()
        {
            InitializeComponent();

            comboBox1.Items.AddRange(ActionIds);
            comboBox2.Items.AddRange(ActionIds);
            comboBox3.Items.AddRange(ActionIds);
            timer1.Interval = 100;
            timer1.Tick += Timer1_Tick;
            timer2.Interval = 100;
            timer2.Tick += Timer2_Tick;
            timer3.Interval = 1000;
            timer3.Tick += Timer3_Tick;
            _configSaveTimer.Tick += (s, ev) => { _configSaveTimer.Stop(); SaveConfigFile(); };

            checkBox8.CheckedChanged += checkBox8_CheckedChanged;
            checkBox9.CheckedChanged += checkBox9_CheckedChanged;
            checkBox10.CheckedChanged += checkBox10_CheckedChanged;
            checkBox11.CheckedChanged += checkBox11_CheckedChanged;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            EnsureDataFiles();
            LoadConfigFile();
            notifyIcon1.Icon = SystemIcons.Application;
            notifyIcon1.ContextMenuStrip = contextMenuStrip1;

            const int clockIconIndex = 112;
            try
            {
                string icoPath = Path.Combine(appDirectory, "icons", "clock.ico");
                notifyIcon1.Icon = File.Exists(icoPath)
                    ? new Icon(icoPath)
                    : ExtractIconFromExe(shell32Path, clockIconIndex, true) ?? SystemIcons.Application;
            }
            catch
            {
                notifyIcon1.Icon = SystemIcons.Application;
            }

            textBoxInfo.ScrollBars = ScrollBars.Vertical;

            bool isFirstLaunch = !PasswordStore.Exists();
            if (isFirstLaunch)
            {
                using var form4Welcome = new Form4();
                form4Welcome.ShowDialog();

                checkBox1.Checked = true;
                checkBox3.Checked = true;
                SaveConfigFile();
            }

        }

        public class GitHubTag
        {
            [JsonProperty("name")]
            public string Name { get; set; } = "";
        }

        public void UnlockScreen() => SetUiLocked(false);

        private void SetUiLocked(bool locked)
        {
            foreach (string name in new[] { "tabActivator", "tabPage5", "tabPage3" })
            {
                var page = tabControl1.TabPages[name];
                if (page != null) page.Enabled = !locked;
            }
            button6.Enabled   = locked;
            label19.Visible   = locked;
            label19.Enabled   = locked;
            if (locked) label19.BringToFront();
        }

        private void checkBox6_CheckedChanged(object? sender, EventArgs e)
        {
            if (_initializing) return;
            appSettings["protect"] = checkBox6.Checked ? "1" : "0";
            SaveConfigFile();
            SetUiLocked(checkBox6.Checked);
        }

        private Form3? form3 = null;

        private void button6_Click(object? sender, EventArgs e)
        {
            using var dlgPwd = new Form2();
            dlgPwd.ShowDialog(this);
        }

        private void label19_Click(object? sender, EventArgs e)
        {

            if (form3 != null && !form3.IsDisposed) { form3.Activate(); return; }
            using (form3 = new Form3 { MainForm = this })
            {
                form3.ShowDialog(this);
            }
            form3 = null;
        }

        private Dictionary<string, string> GetDefaultSettings() => new Dictionary<string, string>
        {
            ["defaultAction1"] = "0",
            ["defaultAction2"] = "0",
            ["defaultAction3"] = "0",
            ["timer1ActivateBy"] = "00:05:00",
            ["timer1EnabledAuto"] = "0",
            ["timer2HourActivateBy"] = "10:00",
            ["timer2DateActivateBy"] = DateTime.Now.ToString("dd.MM.yyyy"),
            ["timer3HourActivateBy"] = "10:00",
            ["timer3CheckedDays"] = "",
            ["timer3Enabled"] = "0",
            ["timer3LastFired"] = "",
            ["runWindowsAuto"] = "0",
            ["autoStart"] = "0",
            ["noMinimalize"] = "0",
            ["RunHost"] = "1",
            ["powerPlanSetID"] = "",
            ["powerPlanID"] = "",
            ["protect"] = "0",
            ["userSet"] = "msedge.exe",
            ["userCommandBehaviour"] = "0",
            ["timezoneCheck"] = "1",
            ["noTray"] = "0",
            ["noTimerShow"] = "0",
            ["darkModeEnabled"] = "0",
            ["soundFile"] = "",
            ["act1_url"] = "https://google.com",
            ["act2_url"] = "https://google.com",
            ["act3_url"] = "https://google.com",
            ["act1_cmd"] = "",
            ["act2_cmd"] = "",
            ["act3_cmd"] = "",
            ["act1_note"] = "",
            ["act2_note"] = "",
            ["act3_note"] = "",

            ["cleanTemp"] = "1",
            ["cleanWinTemp"] = "1",
            ["cleanDownloads"] = "0",
            ["cleanWU"] = "1",
            ["cleanDriverCache"] = "0",
            ["cleanThumbs"] = "1",
            ["cleanPrefetch"] = "0",
            ["cleanRecycleBin"] = "0",
            ["cleanEventLog"] = "0",
            ["cleanCustomFolders"] = "",
            ["noBalloon"] = "0",
            ["taskList"] = "[]",
            ["autoStartQueue"] = "1",
            ["timer3UserPaused"] = "0",

            ["ss_format"]   = "PNG",
            ["ss_count"]    = "1",
            ["ss_delay"]    = "0",
            ["ss_interval"] = "1000",
            ["ss_compress"] = "0",
            ["ss_folder"]   = "",

            ["language"]      = "",
            ["checkUpdates"]  = "1",
            ["skipVersion"]   = "",
            ["activatorMode"]   = "1",
            ["timer2InputMode"] = "duration",
            ["timer2Duration"]  = "00:30:00",
            ["hideTips"]        = "0",
            ["soundEveryTask"]  = "0",
            ["act1_sound"]    = "",
            ["act2_sound"]    = "",
            ["act3_sound"]    = "",

            ["smtp_host"]     = "",
            ["smtp_port"]     = "587",
            ["smtp_security"] = "Auto",
            ["smtp_auth"]     = "Password",
            ["smtp_oauth_client"]  = "",
            ["smtp_oauth_secret"]  = "",
            ["smtp_oauth_refresh"] = "",
            ["smtp_oauth_url"]     = "",
            ["smtp_user"]     = "",
            ["smtp_pass"]     = "",
            ["smtp_from"]     = "",
            ["smtp_fromName"] = "",

            ["act1_mail_to"] = "", ["act2_mail_to"] = "", ["act3_mail_to"] = "",
            ["act1_mail_subject"] = "", ["act2_mail_subject"] = "", ["act3_mail_subject"] = "",
            ["act1_mail_template"] = "ShortText", ["act2_mail_template"] = "ShortText", ["act3_mail_template"] = "ShortText",
            ["act1_mail_body"] = "", ["act2_mail_body"] = "", ["act3_mail_body"] = "",
            ["act1_mail_attachment"] = "", ["act2_mail_attachment"] = "", ["act3_mail_attachment"] = "",
            ["act1_mail_command"] = "", ["act2_mail_command"] = "", ["act3_mail_command"] = "",
        };

        private void LoadConfigFile()
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            Directory.CreateDirectory(dataDir);
            configFilePath = Path.Combine(dataDir, "config.json");

            var defaults = GetDefaultSettings();
            var loaded = new Dictionary<string, string>();

            if (File.Exists(configFilePath))
            {
                try { loaded = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(configFilePath)) ?? loaded; }
                catch {  }
            }

            appSettings = new NameValueCollection();

            Lang.SetQuiet(string.IsNullOrEmpty(loaded.TryGetValue("language", out var lc) ? lc : "")
                ? Lang.SystemDefault : lc);
            foreach (var kv in defaults)
                appSettings[kv.Key] = loaded.TryGetValue(kv.Key, out var v) ? v : kv.Value;
            foreach (var kv in loaded)
                if (appSettings[kv.Key] == null) appSettings[kv.Key] = kv.Value;

            if (!File.Exists(configFilePath)) SaveConfigFile();

            if (appSettings["autoStart"] == "1")
            {
                if (!TryParseTime(appSettings["timer1ActivateBy"], out var autoStartSpan))
                {
                    autoStartSpan = TimeSpan.FromMinutes(5);
                    appSettings["timer1ActivateBy"] = "00:05:00";
                }
                timer1EndTime = DateTime.Now.Add(autoStartSpan);
                timer1.Start();
            }

            comboBox1.SelectedIndex = int.TryParse(appSettings["defaultAction1"], out int a1) ? a1 : 0;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            comboBox2.SelectedIndex = int.TryParse(appSettings["defaultAction2"], out int a2) ? a2 : 0;
            comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;
            comboBox3.SelectedIndex = int.TryParse(appSettings["defaultAction3"], out int a3) ? a3 : 0;
            comboBox3.SelectedIndexChanged += comboBox3_SelectedIndexChanged;

            textBox4.Text = appSettings["timer1ActivateBy"];
            textBox4.TextChanged += textBox4_TextChanged;
            textBox7.Text = appSettings["timer2HourActivateBy"];
            textBox7.TextChanged += textBox7_TextChanged;
            textBox1.Text = appSettings["timer3HourActivateBy"];
            textBoxUser.Text = appSettings["userSet"];
            textBoxUser.TextChanged += textBoxUser_TextChanged;

            string checkedDays = appSettings["timer3CheckedDays"] ?? "";
            if (!string.IsNullOrEmpty(checkedDays))
            {
                foreach (var part in checkedDays.Split(','))
                    if (int.TryParse(part.Trim(), out int idx) && idx >= 0 && idx < checkedListBox1.Items.Count)
                        checkedListBox1.SetItemChecked(idx, true);
            }

            if (!DateTime.TryParseExact(appSettings["timer2DateActivateBy"], "dd.MM.yyyy", CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime settingsDate))
                settingsDate = DateTime.Now;
            DateTime currentDate = DateTime.Now;
            dateTimePicker1.MinDate = settingsDate < currentDate ? settingsDate : currentDate;
            dateTimePicker1.Value = settingsDate;
            dateTimePicker1.ValueChanged += dateTimePicker1_ValueChanged;

            checkBox1.Checked = appSettings["runWindowsAuto"] == "1";
            checkBox2.Checked = appSettings["autoStart"] == "1";
            checkBox3.Checked = appSettings["noMinimalize"] == "1";
            checkBox4.Checked = appSettings["RunHost"] == "1";
            checkBox5.Checked = appSettings["powerPlanSetID"] != "";
            checkBox6.Checked = appSettings["protect"] == "1";
            checkBox7.Checked = appSettings["userCommandBehaviour"] == "1";
            checkBox8.Checked = appSettings["timezoneCheck"] == "1";
            checkBox9.Checked = appSettings["noTray"] == "1";
            checkBox10.Checked = appSettings["noTimerShow"] == "1";
            checkBox11.Checked = appSettings["darkModeEnabled"] == "1";
            checkBox12.Checked = appSettings["autoStartQueue"] != "0";

            try { _taskList = JsonConvert.DeserializeObject<List<TaskEntry>>(appSettings["taskList"] ?? "[]") ?? new(); }
            catch { _taskList = new(); }

            if (appSettings["timer3Enabled"] == "1" && !string.IsNullOrEmpty(appSettings["timer3CheckedDays"]))
                timer3.Start();

            if (appSettings["autoStartQueue"] != "0")
            {
                if (!timer3.Enabled && appSettings["timer3UserPaused"] != "1" && _taskList.Any(t => t.ActivatorType == 3 && t.Enabled))
                    timer3.Start();
                if (!timer2.Enabled && _taskList.Any(t => t.ActivatorType == 2 && t.Enabled && t.EndTimeUtc > DateTime.UtcNow))
                    timer2.Start();
            }

            _initializing = false;
            ApplyLoadedSettings();
        }

        private void ApplyLoadedSettings()
        {

            this.MinimizeBox = !checkBox3.Checked;
            this.FormClosing -= Form1_FormClosing;
            this.Resize -= Form1_Resize;
            notifyIcon1.DoubleClick -= NotifyIcon1_DoubleClick;
            if (checkBox3.Checked)
            {
                this.FormClosing += Form1_FormClosing;
                this.Resize += Form1_Resize;
                notifyIcon1.DoubleClick += NotifyIcon1_DoubleClick;
            }

            timer1.Tick -= Timer1_Tick;
            timer1.Tick -= Timer1_Tick_Reset;
            if (checkBox4.Checked) timer1.Tick += Timer1_Tick_Reset;
            else                   timer1.Tick += Timer1_Tick;

            if (checkBox1.Checked) SetAutoStart(true);

            if (checkBox5.Checked && !string.IsNullOrEmpty(appSettings["powerPlanSetID"]))
            {
                importedPowerPlanGuid = appSettings["powerPlanSetID"] ?? "";
                StartPowerPlanMonitor();
            }
        }

        private void SaveConfigFile()
        {
            try
            {
                var dict = new Dictionary<string, string>();
                foreach (string? k in appSettings.AllKeys)
                {
                    if (k == null) continue;
                    dict[k] = appSettings[k] ?? "";
                }

                dict["taskList"] = JsonConvert.SerializeObject(_taskList ?? new List<TaskEntry>());

                string? dir = Path.GetDirectoryName(configFilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                string tmp = configFilePath + ".tmp";
                File.WriteAllText(tmp, JsonConvert.SerializeObject(dict, Formatting.Indented));
                File.Move(tmp, configFilePath, true);
            }
            catch {  }
        }

        private void ScheduleConfigSave()
        {
            _configSaveTimer.Stop();
            _configSaveTimer.Start();
        }

        private System.Threading.Timer? powerPlanMonitor;
        private string importedPowerPlanGuid = "";

        private volatile bool _powerPlanGuardActive;

        private static readonly Regex _guidRegex = new Regex(
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
            RegexOptions.Compiled);

        private static string RunPowerCfg(string arguments, bool captureOutput)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "powercfg",
                    Arguments              = arguments,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    WindowStyle            = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = captureOutput,
                    RedirectStandardError  = captureOutput
                };
                using var proc = Process.Start(psi);
                if (proc == null) return "";
                string output = captureOutput ? proc.StandardOutput.ReadToEnd() : "";
                proc.WaitForExit(15000);
                return output;
            }
            catch { return ""; }
        }

        private static string ExtractGuid(string powercfgOutput)
        {
            var m = _guidRegex.Match(powercfgOutput ?? "");
            return m.Success ? m.Value : "";
        }

        private string ImportAndSetActivePowerPlan()
        {
            var inputPath  = GetDataFilePath("plan.pow");
            var guidString = Guid.NewGuid().ToString("D");

            RunPowerCfg($"-import \"{inputPath}\" {guidString}", captureOutput: false);
            RunPowerCfg($"/setactive {guidString}", captureOutput: false);

            SavePowerPlanImportedGuidToConfigFile(guidString);
            return guidString;
        }

        private void StartPowerPlanMonitor()
        {
            StopPowerPlanMonitor();
            _powerPlanGuardActive = true;
            powerPlanMonitor = new System.Threading.Timer(CheckPowerPlan, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        private void StopPowerPlanMonitor()
        {
            _powerPlanGuardActive = false;
            powerPlanMonitor?.Dispose();
            powerPlanMonitor = null;
        }

        private void CheckPowerPlan(object? state)
        {

            try
            {
                if (!_powerPlanGuardActive || string.IsNullOrEmpty(importedPowerPlanGuid)) return;

                string active = ExtractGuid(RunPowerCfg("/GETACTIVESCHEME", captureOutput: true));
                if (string.IsNullOrEmpty(active)) return;

                if (!string.Equals(active, importedPowerPlanGuid, StringComparison.OrdinalIgnoreCase))
                    RunPowerCfg($"/setactive {importedPowerPlanGuid}", captureOutput: false);
            }
            catch { }
        }

        private string GetCurrentPowerPlanGuid()
            => ExtractGuid(RunPowerCfg("/GETACTIVESCHEME", captureOutput: true));

        private void SavePowerPlanGuidToConfigFile(string guid)
        {
            appSettings["powerPlanID"] = guid ?? "";
            SaveConfigFile();
        }

        private void SavePowerPlanImportedGuidToConfigFile(string guid)
        {
            appSettings["powerPlanSetID"] = guid;
            SaveConfigFile();
        }

        private void SavePowerPlanRestoreGuidToConfigFile()
        {
            appSettings["powerPlanSetID"] = "";
            SaveConfigFile();
        }

        private void RemovePowerPlan()
        {
            string scriptPath = GetDataFilePath("w7restorePlan.ps1");
            if (File.Exists(scriptPath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(scriptPath)
                };
                TryStartProcess(psi, waitForExit: true);
            }
            SavePowerPlanRestoreGuidToConfigFile();
        }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern int mciSendString(string command, System.Text.StringBuilder? returnValue, int returnLength, IntPtr winHandle);

        private static readonly string[] _soundExtensions = { "*.wav", "*.mp3", "*.aac", "*.ogg", "*.wma", "*.flac" };
        private System.Media.SoundPlayer? _wavPlayer;

        private string ResolveSoundFor(int activator)
        {
            string value = activator > 0 ? (appSettings[$"act{activator}_sound"] ?? "").Trim() : "";
            if (string.IsNullOrEmpty(value)) value = (appSettings["soundFile"] ?? "").Trim();
            if (string.IsNullOrEmpty(value)) value = SoundCatalog.DefaultFile;
            return SoundCatalog.ResolvePath(value);
        }

        private void PlaySoundFile(string filePath)
        {

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {

                string builtIn = SoundCatalog.ResolvePath(SoundCatalog.DefaultFile);
                if (File.Exists(builtIn)) filePath = builtIn;

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    try
                    {
                        string soundsPath = Path.Combine(appDirectory, "sounds", "sound.wma");
                        if (!File.Exists(soundsPath))
                        {
                            var asm = System.Reflection.Assembly.GetExecutingAssembly();
                            using var resStream = asm.GetManifestResourceStream("TimerSys.data.sound.wma");
                            if (resStream != null)
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(soundsPath)!);
                                using var fs = File.Create(soundsPath);
                                resStream.CopyTo(fs);
                            }
                        }
                        if (File.Exists(soundsPath)) filePath = soundsPath;
                    }
                    catch { }
                }
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                string mediaDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "..", "Media");
                foreach (var fb in new[] { "tada.wav", "Alarm01.wav", "chimes.wav", "ding.wav" })
                {
                    string fp = Path.Combine(mediaDir, fb);
                    if (File.Exists(fp)) { filePath = fp; break; }
                }
            }
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            string extLow = Path.GetExtension(filePath).ToLowerInvariant();
            if (extLow == ".wav")
            {
                try
                {
                    _wavPlayer?.Dispose();
                    _wavPlayer = new System.Media.SoundPlayer(filePath);
                    _wavPlayer.Play();
                }
                catch { }
            }
            else
            {

                try
                {
                    mciSendString("close mci_sound", null, 0, IntPtr.Zero);
                    mciSendString($"open \"{filePath}\" alias mci_sound", null, 0, IntPtr.Zero);
                    mciSendString("play mci_sound", null, 0, IntPtr.Zero);
                }
                catch { }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

            [MarshalAs(UnmanagedType.U4)]
            public int cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwTime;
        }

        private TimeSpan? IdleThresholdFor(TaskEntry t)
            => TryParseTime(string.IsNullOrEmpty(t.IdleTimeout) ? textBox4.Text : t.IdleTimeout, out var ts)
                ? ts
                : (TimeSpan?)null;

        private void Timer1_Tick(object? sender, EventArgs e)
        {
            long idleMs = GetIdleMilliseconds();

            bool anyFired = false;
            var tasks = _taskList.Where(t => t.ActivatorType == 1 && t.Enabled).ToList();
            foreach (var t in tasks)
            {
                var threshold = IdleThresholdFor(t);
                if (threshold == null) continue;
                if (idleMs >= (long)threshold.Value.TotalMilliseconds)
                {
                    ExecuteCommand(t.Action, 1);
                    t.Enabled = false;
                    t.Completed = true;
                    anyFired = true;
                }
            }
            if (anyFired) { SaveConfigFile(); RefreshTaskGrid(); }

            if (!_taskList.Any(t => t.ActivatorType == 1 && t.Enabled))
            {
                timer1.Stop();
                _lastSecond1 = -1;

                textBox5.Text = anyFired && !checkBox10.Checked ? Lang.T("msg.actionDone") : "";
                return;
            }

            if (!checkBox10.Checked)
            {

                TimeSpan? pending = _taskList
                    .Where(t => t.ActivatorType == 1 && t.Enabled)
                    .Select(IdleThresholdFor)
                    .Where(ts => ts != null)
                    .OrderBy(ts => ts!.Value)
                    .FirstOrDefault();

                if (pending != null)
                {
                    var remaining = pending.Value - TimeSpan.FromMilliseconds(idleMs);
                    int sec1 = (int)remaining.TotalSeconds;
                    if (sec1 != _lastSecond1)
                    {
                        _lastSecond1 = sec1;
                        textBox5.Text = remaining.TotalSeconds > 0
                            ? Lang.T("msg.remaining", FormatRemaining(remaining))
                            : Lang.T("msg.ready");
                    }
                }
            }
        }

        public void StartTimer1()
        {
            timer1EndTime = DateTime.Now.Add(
                TryParseTime(textBox4.Text, out var ts) ? ts : TimeSpan.FromMinutes(5));
            timer1.Start();
        }

        private void Timer2_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.UtcNow;
            bool anyFired = false;

            var stopers = _taskList.Where(t => t.ActivatorType == 2 && t.Enabled && t.EndTimeUtc != default).ToList();
            foreach (var t in stopers)
            {
                if (t.EndTimeUtc <= now)
                {
                    ExecuteCommand(t.Action, 2);
                    t.Enabled = false;
                    t.Completed = true;
                    anyFired = true;
                }
            }
            if (anyFired) { SaveConfigFile(); RefreshTaskGrid(); }

            var remaining = _taskList.Where(t => t.ActivatorType == 2 && t.Enabled && t.EndTimeUtc > now).ToList();
            if (!remaining.Any())
            {
                timer2.Stop();
                _lastSecond2 = -1;
                textBox9.Text = anyFired && !checkBox10.Checked ? Lang.T("msg.actionDone") : "";
                return;
            }

            var nearest = remaining.OrderBy(t => t.EndTimeUtc).First();
            var rem = nearest.EndTimeUtc - now;
            int sec2 = (int)rem.TotalSeconds;
            if (sec2 != _lastSecond2)
            {
                _lastSecond2 = sec2;
                if (!checkBox10.Checked)
                    textBox9.Text = Lang.T("msg.nextIn", FormatRemaining(rem));
            }
        }

        private void Timer1_Tick_Reset(object? sender, EventArgs e)
        {
            long idleTime = GetIdleMilliseconds();

            if (idleTime < 1000 && TryParseTime(textBox4.Text, out var ts4r))
                timer1EndTime = DateTime.Now.Add(ts4r);

            var remainingTime = timer1EndTime - DateTime.Now;
            if (remainingTime.TotalSeconds <= 0)
            {

                var tasks = _taskList.Where(t => t.ActivatorType == 1 && t.Enabled).ToList();
                foreach (var t in tasks)
                    ExecuteCommand(t.Action, 1);
                if (!tasks.Any() && comboBox1.SelectedIndex >= 0)
                    ExecuteCommand(ActionIdFromIndex(comboBox1.SelectedIndex), 1);

                if (TryParseTime(textBox4.Text, out var ts4Reset))
                    timer1EndTime = DateTime.Now.Add(ts4Reset);
                else
                    timer1EndTime = DateTime.Now.AddMinutes(5);
            }
            else if (!checkBox10.Checked)
            {
                textBox5.Text = Lang.T("msg.remaining", FormatRemaining(remainingTime));
            }
        }

        private void ExecuteCommand(string command, int activator = 0)
        {

            ShowActivationBalloon(command, activator);

            string? actKey = activator > 0 ? $"act{activator}" : null;
            string globalCmd = System.Net.WebUtility.HtmlDecode(appSettings["userSet"] ?? "");
            string userCommand = (actKey != null && !string.IsNullOrEmpty(appSettings[$"{actKey}_cmd"]))
                ? appSettings[$"{actKey}_cmd"]!
                : globalCmd;

            switch (command)
            {
                case "Odtwórz dźwięk":
                    PlaySoundFile(ResolveSoundFor(activator));
                    break;
                case "Uruchom polecenie":
                    if (string.IsNullOrWhiteSpace(userCommand)) break;
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c " + userCommand,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    TryStartProcess(psi, waitForExit: appSettings["userCommandBehaviour"] == "1");
                    break;
                case "Wyłącz monitor":

                    var _monHandle = this.Handle;
                    Task.Run(() => {
                        System.Threading.Thread.Sleep(300);
                        SendMessage(_monHandle, 0x0112, new IntPtr(0xF170), new IntPtr(2));
                    });
                    break;

                case "Uśpienie":
                    TryStartProcess("Rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0");
                    break;
                case "Wylogowanie":
                    TryStartProcess("shutdown", "/l");
                    break;
                case "Restart":
                    TryStartProcess("shutdown", "/r /t 0");
                    break;
                case "Wyłączenie komputera":
                    TryStartProcess("shutdown", "/s /t 0");
                    break;
                case "Hibernacja":
                    TryStartProcess("shutdown", "/h");
                    break;
                case "Blokada użytkownika":
                    TryStartProcess("Rundll32.exe", "user32.dll,LockWorkStation");
                    break;

                case "Oczyść pliki tymczasowe":

                    var cleanSettings = new System.Collections.Generic.Dictionary<string, string>();
                    foreach (string ck in new[] { "cleanTemp","cleanWinTemp","cleanDownloads","cleanWU",
                                                   "cleanDriverCache","cleanThumbs","cleanPrefetch",
                                                   "cleanRecycleBin","cleanEventLog","cleanCustomFolders" })
                        cleanSettings[ck] = appSettings[ck] ?? "0";

                    Task.Run(() =>
                    {
                        int deleted = 0;

                        void SafeDeleteDir(string dir)
                        {
                            if (!Directory.Exists(dir)) return;
                            try
                            {
                                foreach (var f in Directory.EnumerateFiles(dir))
                                    try { File.Delete(f); deleted++; } catch { }
                            }
                            catch { }
                            try
                            {
                                foreach (var sub in Directory.EnumerateDirectories(dir))
                                    try { Directory.Delete(sub, true); deleted++; } catch { }
                            }
                            catch { }
                        }

                        void SafeDeleteFiles(string dir, string pattern)
                        {
                            if (!Directory.Exists(dir)) return;
                            try
                            {
                                foreach (var f in Directory.EnumerateFiles(dir, pattern))
                                    try { File.Delete(f); deleted++; } catch { }
                            }
                            catch { }
                        }

                        void SafeDeleteRecursive(string dir)
                        {
                            if (!Directory.Exists(dir)) return;
                            try
                            {
                                foreach (var f in Directory.EnumerateFiles(dir))
                                    try { File.Delete(f); deleted++; } catch { }
                            }
                            catch { }
                            try
                            {
                                foreach (var sub in Directory.EnumerateDirectories(dir))
                                    try { SafeDeleteRecursive(sub); } catch { }
                            }
                            catch { }
                        }

                        try
                        {
                            if (cleanSettings["cleanTemp"] != "0")
                                SafeDeleteDir(Path.GetTempPath());

                            if (cleanSettings["cleanWinTemp"] != "0")
                                SafeDeleteDir(Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"));

                            if (cleanSettings["cleanDownloads"] == "1")
                                SafeDeleteFiles(Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"), "*");

                            if (cleanSettings["cleanWU"] == "1")
                                SafeDeleteRecursive(Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                                    "SoftwareDistribution", "Download"));

                            if (cleanSettings["cleanDriverCache"] == "1")
                                SafeDeleteDir(Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                                    "System32", "DriverStore", "Temp"));

                            if (cleanSettings["cleanThumbs"] == "1")
                                SafeDeleteFiles(Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                    "Microsoft", "Windows", "Explorer"), "thumbcache_*.db");

                            if (cleanSettings["cleanPrefetch"] == "1")
                                SafeDeleteFiles(Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"), "*.pf");

                            if (cleanSettings["cleanRecycleBin"] == "1")
                                try
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = "powershell.exe",
                                        Arguments = "-NoProfile -Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue\"",
                                        CreateNoWindow = true,
                                        UseShellExecute = false
                                    });
                                }
                                catch { }

                            if (cleanSettings["cleanEventLog"] == "1")
                                try
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = "wevtutil.exe",
                                        Arguments = "cl System",
                                        CreateNoWindow = true,
                                        UseShellExecute = false
                                    })?.WaitForExit();
                                }
                                catch { }
                        }
                        catch { }

                        if (!string.IsNullOrEmpty(cleanSettings["cleanCustomFolders"]))
                            foreach (var customDir in cleanSettings["cleanCustomFolders"]
                                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                                .Where(d => !string.IsNullOrWhiteSpace(d)))
                                SafeDeleteDir(customDir);

                        BeginInvoke(new Action(() =>
                            MessageBox.Show(Lang.T("msg.cleanupDone", deleted),
                                Lang.T("msg.cleanupTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information)));
                    });
                    break;

                case "Zapisz notatkę na pulpicie":
                    try
                    {
                        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string noteFile = Path.Combine(desktop,
                            $"{Lang.T("note.file")}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                        string noteContent = actKey != null ? (appSettings[$"{actKey}_note"] ?? "") : "";
                        string content = Lang.T("note.header") + "\r\n" +
                                         Lang.T("note.when", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")) + "\r\n" +
                                         Lang.T("note.who", Environment.MachineName, Environment.UserName) + "\r\n";
                        if (!string.IsNullOrEmpty(noteContent))
                            content += "\r\n" + Lang.T("note.body") + "\r\n" + noteContent + "\r\n";
                        File.WriteAllText(noteFile, content, System.Text.Encoding.UTF8);
                    }
                    catch { }
                    break;

                case "Otwórz stronę WWW":
                    try
                    {
                        string urlVal = actKey != null ? (appSettings[$"{actKey}_url"] ?? "") : "";
                        if (string.IsNullOrEmpty(urlVal)) urlVal = userCommand.Trim();
                        if (!urlVal.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                            !urlVal.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                            urlVal = "https://" + urlVal;
                        if (!string.IsNullOrEmpty(urlVal))
                            Process.Start(new ProcessStartInfo { FileName = urlVal, UseShellExecute = true });
                    }
                    catch { }
                    break;

                case "Opróżnij kosz":
                    try
                    {
                        var psiRb = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = "-NoProfile -Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue\"",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        Process.Start(psiRb);
                    }
                    catch { }
                    break;

                case "Wykonaj zrzut ekranu":
                    Task.Run(() => TakeScreenshots());
                    break;

                case ActionEmail:
                    SendConfiguredEmail(activator);
                    break;
            }

            if (appSettings["soundEveryTask"] == "1" && command != "Odtwórz dźwięk")
                PlaySoundFile(ResolveSoundFor(activator));
        }

        private void SetAutoStart(bool enabled)
        {

            try
            {
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcutPath = Path.Combine(startupFolder, "Wyłącznik czasowy.lnk");

                if (enabled)
                {
                    if (!File.Exists(shortcutPath))
                        CreateShortcutNative(shortcutPath, Application.ExecutablePath, "--minimized");
                }
                else if (File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                }
            }
            catch (Exception ex)
            {
                if (!_initializing)
                    MessageBox.Show(Lang.T("msg.autostartError", ex.Message),
                        Lang.T("msg.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void StopSound()
        {
            try
            {
                mciSendString("stop mci_sound", null, 0, IntPtr.Zero);
                mciSendString("close mci_sound", null, 0, IntPtr.Zero);
            }
            catch { }

            try
            {
                _wavPlayer?.Stop();
                _wavPlayer?.Dispose();
                _wavPlayer = null;
            }
            catch { }
        }

        private void ShowActivationBalloon(string actionName, int activator = 0)
        {
            if (appSettings["noBalloon"] == "1") return;
            try
            {
                string now   = DateTime.Now.ToString("HH:mm");
                string today = DateTime.Now.ToString("dd.MM.yyyy");

                string shown = ActionDisplay(actionName);
                string body = activator switch
                {
                    1 => Lang.T("toast.idle", shown, now, today,
                            TryParseTime(appSettings["timer1ActivateBy"], out var ts1) ? (int)ts1.TotalMinutes : 0),
                    2 => Lang.T("toast.timer", shown, now, today),
                    3 => Lang.T("toast.schedule", shown, now, today, GetScheduleDescription()),
                    _ => Lang.T("toast.generic", shown, now, today)
                };

                if (InvokeRequired)
                    BeginInvoke(new Action(() => ShowCustomToast(body, actionName, activator)));
                else
                    ShowCustomToast(body, actionName, activator);
            }
            catch { }
        }

        private void TakeScreenshots()
        {
            try
            {

                string fmt     = appSettings["ss_format"] ?? "PNG";
                int count      = int.TryParse(appSettings["ss_count"],    out int sc) ? Math.Max(1, sc) : 1;
                int delayMs    = int.TryParse(appSettings["ss_delay"],    out int sd) ? sd : 0;
                int intervalMs = int.TryParse(appSettings["ss_interval"], out int si) ? si : 1000;
                bool compress  = appSettings["ss_compress"] == "1";
                string folder  = appSettings["ss_folder"] ?? "";
                if (string.IsNullOrWhiteSpace(folder))
                    folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                Directory.CreateDirectory(folder);

                System.Drawing.Imaging.ImageFormat imgFmt = fmt.ToUpperInvariant() switch
                {
                    "JPG" or "JPEG" => System.Drawing.Imaging.ImageFormat.Jpeg,
                    "BMP"           => System.Drawing.Imaging.ImageFormat.Bmp,
                    "GIF"           => System.Drawing.Imaging.ImageFormat.Gif,
                    _               => System.Drawing.Imaging.ImageFormat.Png
                };
                string ext = fmt.ToLowerInvariant();
                if (ext == "jpeg") ext = "jpg";

                if (delayMs > 0) System.Threading.Thread.Sleep(delayMs);

                var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
                var files  = new System.Collections.Generic.List<string>();

                for (int i = 0; i < count; i++)
                {
                    if (i > 0) System.Threading.Thread.Sleep(intervalMs);
                    using var bmp = new Bitmap(bounds.Width, bounds.Height);
                    using var g   = Graphics.FromImage(bmp);
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    string ts   = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                    string path = Path.Combine(folder, $"screenshot_{ts}.{ext}");
                    bmp.Save(path, imgFmt);
                    files.Add(path);
                }

                if (compress && files.Count > 0)
                {
                    string zipPath = Path.Combine(folder, $"screenshots_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                    using var zip = new ZipArchive(File.Create(zipPath), ZipArchiveMode.Create);
                    foreach (var f in files)
                    { zip.CreateEntryFromFile(f, Path.GetFileName(f)); File.Delete(f); }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    BeginInvoke(new Action(() =>
                        MessageBox.Show(Lang.T("msg.screenshotError", ex.Message), Lang.T("msg.error"),
                            MessageBoxButtons.OK, MessageBoxIcon.Error)));
                }
                catch { }
            }
        }

        private void ShowCustomToast(string message, string actionName = "", int activator = 0)
        {
            bool dark      = appSettings["darkModeEnabled"] == "1";
            Color bgColor  = dark ? Color.FromArgb(45, 45, 48)    : Color.FromArgb(245, 245, 248);
            Color fgColor  = dark ? Color.FromArgb(230, 230, 230)  : Color.FromArgb(30, 30, 30);
            Color subColor = dark ? Color.FromArgb(160, 160, 160)  : Color.FromArgb(100, 100, 100);
            Color barColor = activator switch
            {
                1 => Color.FromArgb(0, 150, 136),
                2 => Color.FromArgb(33, 150, 243),
                3 => Color.FromArgb(156, 39, 176),
                _ => Color.FromArgb(0, 122, 204)
            };
            Ico icon = activator switch
            { 1 => Ico.Idle, 2 => Ico.Stopwatch, 3 => Ico.Calendar, _ => Ico.Bell };
            string title = string.IsNullOrEmpty(actionName)
                ? Lang.T("toast.titleDefault")
                : Lang.T("toast.titleAction", ActionDisplay(actionName));

            int toastW = 420, toastH = 140;
            var wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);

            var toast = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition   = FormStartPosition.Manual,
                TopMost         = true,
                ShowInTaskbar   = false,
                ClientSize      = new Size(toastW, toastH),
                BackColor       = bgColor,
                Opacity         = 0
            };

            var bar = new System.Windows.Forms.Label
            { BackColor = barColor, Location = new Point(0, 0), Size = new Size(5, toastH) };
            toast.Controls.Add(bar);

            var picIcon = new PictureBox
            {
                Image     = VectorIcons.Get(icon, 28, barColor),
                BackColor = Color.Transparent,
                Location  = new Point(14, 16), Size = new Size(32, 32),
                SizeMode  = PictureBoxSizeMode.CenterImage
            };
            toast.Controls.Add(picIcon);

            var lblTitle = new System.Windows.Forms.Label
            {
                Text = title, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = fgColor, BackColor = Color.Transparent,
                Location = new Point(54, 10), Size = new Size(toastW - 54 - 28, 20), AutoSize = false
            };
            toast.Controls.Add(lblTitle);

            var lblMsg = new System.Windows.Forms.Label
            {
                Text = message, Font = new Font("Segoe UI", 8.5f),
                ForeColor = subColor, BackColor = Color.Transparent,
                Location = new Point(54, 34), Size = new Size(toastW - 60, 52), AutoSize = false
            };
            toast.Controls.Add(lblMsg);

            var btnX = new System.Windows.Forms.Button
            {
                Text = "", FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Image = VectorIcons.Get(Ico.Close, 10, subColor),
                ImageAlign = ContentAlignment.MiddleCenter,
                Location = new Point(toastW - 26, 4), Size = new Size(22, 22), TabStop = false
            };
            btnX.FlatAppearance.BorderSize = 0;
            btnX.Click += (s2, ev2) => { try { toast.Close(); } catch { } };
            toast.Controls.Add(btnX);

            var lblCountdown = new System.Windows.Forms.Label
            {
                Text = Lang.T("toast.closeIn", 15),
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = subColor, BackColor = Color.Transparent,
                Location = new Point(54, 90), Size = new Size(toastW - 80, 16), AutoSize = false
            };
            toast.Controls.Add(lblCountdown);

            var progressBg = new System.Windows.Forms.Panel
            {
                BackColor = dark ? Color.FromArgb(70, 70, 73) : Color.FromArgb(215, 215, 218),
                Location  = new Point(0, toastH - 4), Size = new Size(toastW, 4)
            };
            toast.Controls.Add(progressBg);

            var progressFg = new System.Windows.Forms.Panel
            {
                BackColor = barColor,
                Location  = new Point(0, toastH - 4), Size = new Size(toastW, 4)
            };
            toast.Controls.Add(progressFg);

            var toastFonts = new List<Font> { lblTitle.Font, lblMsg.Font, lblCountdown.Font };

            int finalX = wa.Right - toastW - 12;

            _activeToasts.RemoveAll(f => f.IsDisposed);
            int finalY = wa.Bottom - toastH - 12 - _activeToasts.Count * (toastH + 8);
            if (finalY < wa.Top + 8) finalY = wa.Top + 8;
            _activeToasts.Add(toast);
            toast.FormClosed += (s2, ev2) =>
            {
                _activeToasts.Remove(toast);
                foreach (var f in toastFonts) { try { f.Dispose(); } catch { } }
            };
            toast.Location = new Point(finalX, wa.Bottom + 5);

            int totalMs = 15000, remainMs = totalMs;
            bool paused = false;

            void OnEnter(object? s2, EventArgs ev2) { paused = true; }
            void OnLeave(object? s2, EventArgs ev2)
            {
                if (!toast.IsDisposed)
                    paused = toast.Bounds.Contains(Cursor.Position);
            }

            foreach (Control c in toast.Controls)
            { c.MouseEnter += OnEnter; c.MouseLeave += OnLeave; }
            toast.MouseEnter += OnEnter;
            toast.MouseLeave += OnLeave;

            toast.Show();

            int slideSteps = 12, slideStep = 0;
            double totalSlide = wa.Bottom + 5 - finalY;
            int slideFrom = wa.Bottom + 5;
            var slideTimer = new System.Windows.Forms.Timer { Interval = 16 };
            slideTimer.Tick += (s2, ev2) =>
            {
                if (slideStep >= slideSteps || toast.IsDisposed) { slideTimer.Stop(); slideTimer.Dispose(); return; }
                slideStep++;
                double t    = (double)slideStep / slideSteps;
                double ease = 1 - Math.Pow(1 - t, 3);
                int newY    = (int)(slideFrom - totalSlide * ease);
                toast.Location = new Point(finalX, newY);
                toast.Opacity  = 0.96 * ease;
            };
            slideTimer.Start();

            var ct = new System.Windows.Forms.Timer { Interval = 100 };
            ct.Tick += (s2, ev2) =>
            {
                if (toast.IsDisposed) { ct.Stop(); ct.Dispose(); return; }
                if (!paused) remainMs -= 100;
                if (remainMs <= 0)
                {
                    ct.Stop(); ct.Dispose();
                    try { if (!toast.IsDisposed) toast.Close(); } catch { }
                    return;
                }
                int secs = (remainMs + 999) / 1000;
                lblCountdown.Text = Lang.T("toast.closeIn", secs);
                progressFg.Width  = Math.Max(0, (int)(toastW * (double)remainMs / totalMs));
            };
            ct.Start();
        }

        private string GetScheduleDescription()
        {

            if (_currentlyFiringTask != null && !string.IsNullOrEmpty(_currentlyFiringTask.RepeatIntervalType))
            {
                string unit = Lang.T(_currentlyFiringTask.RepeatIntervalType switch
                { "h" => "unit.hours", "d" => "unit.days", _ => "unit.minutes" });
                return Lang.T("val.every", _currentlyFiringTask.RepeatIntervalValue, unit);
            }

            var labels = Lang.A("scheduleDays");
            var days = (appSettings["timer3CheckedDays"] ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => int.TryParse(p.Trim(), out int i) && i >= 0 && i < labels.Length
                    ? labels[i] : null)
                .Where(s => s != null);
            string time = appSettings["timer3HourActivateBy"] ?? "?";
            return string.Join(", ", days) + Lang.T("sched.descAt") + time;
        }

        private string BuildCleanupSummary()
        {
            var map = new (string Key, string LabelKey)[]
            {
                ("cleanTemp",        "clean.userTemp"),
                ("cleanWinTemp",     "clean.winTemp"),
                ("cleanDownloads",   "clean.downloads"),
                ("cleanWU",          "clean.wu"),
                ("cleanDriverCache", "clean.driver"),
                ("cleanThumbs",      "clean.thumbs"),
                ("cleanPrefetch",    "clean.prefetch"),
                ("cleanRecycleBin",  "clean.bin"),
                ("cleanEventLog",    "clean.log"),
            };
            var enabled = map.Where(x => appSettings[x.Key] != "0").Select(x => Lang.T(x.LabelKey));
            var result = string.Join(", ", enabled);
            return result.Length > 0 ? result : Lang.T("clean.none");
        }

        private void UpdateComboHints(System.Windows.Forms.ComboBox combo,
            System.Windows.Forms.Label? tipLabel, System.Windows.Forms.Button? btnSettings)
        {
            int idx = combo.SelectedIndex;
            var descriptions = Lang.A("actionDesc");
            string desc = (idx >= 0 && idx < descriptions.Length)
                ? descriptions[idx]
                : Lang.T("tip.pickAction");

            if (tipLabel != null)
                tipLabel.Text = Lang.T("tip.header") + "\r\n\r\n" + desc;

            if (btnSettings != null)
            {
                btnSettings.Enabled = ActionHasParams(idx);
                _actionTip.SetToolTip(btnSettings, desc);
            }
        }

        private void comboBox1_SelectedIndexChanged(object? sender, EventArgs e)
        {
            appSettings["defaultAction1"] = comboBox1.SelectedIndex.ToString();
            SaveConfigFile();
            UpdateComboHints(comboBox1, tip01, _btnActSettings1);
        }

        private void comboBox2_SelectedIndexChanged(object? sender, EventArgs e)
        {
            appSettings["defaultAction2"] = comboBox2.SelectedIndex.ToString();
            SaveConfigFile();
            UpdateComboHints(comboBox2, tip02, _btnActSettings2);
        }

        private void comboBox3_SelectedIndexChanged(object? sender, EventArgs e)
        {
            appSettings["defaultAction3"] = comboBox3.SelectedIndex.ToString();
            SaveConfigFile();
            UpdateComboHints(comboBox3, tip03, _btnActSettings3);
        }

        private static bool ActionHasParams(int idx) =>
            idx == 0 || idx == 1 || idx == 9 || idx == 10 || idx == 11 || idx == 13 || idx == 14;

        private void ShowActionSettingsDialog(System.Windows.Forms.ComboBox combo, int activatorNum)
        {
            int idx = combo.SelectedIndex;
            var descriptions = Lang.A("actionDesc");
            string desc = (idx >= 0 && idx < descriptions.Length) ? descriptions[idx] : "";

            if (idx == 0) { ShowSoundSettingsDialog(activatorNum); return; }

            if (idx == 9) { ShowCleanupSettingsDialog(); return; }

            if (idx == 13) { ShowScreenshotSettingsDialog(); return; }

            if (idx == 14) { ShowEmailActionDialog(activatorNum); return; }

            string actKey = $"act{activatorNum}";
            string paramLabel = idx == 1  ? Lang.T("dlg.commandParam") :
                                idx == 10 ? Lang.T("dlg.noteParam") :
                                idx == 11 ? Lang.T("dlg.urlParam") : "";
            string configKey = idx == 1 ? $"{actKey}_cmd" :
                               idx == 10 ? $"{actKey}_note" :
                               idx == 11 ? $"{actKey}_url" : "";

            bool isNote = idx == 10;
            bool hasParams = ActionHasParams(idx);

            using var dlg = new Form
            {
                Text = Lang.T("dlg.actionSettings", combo.SelectedItem?.ToString() ?? ""),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = this.BackColor
            };

            var lblDesc = new System.Windows.Forms.Label
            { Text = desc, Location = new Point(12, 10), Size = new Size(456, 50), AutoSize = false };
            dlg.Controls.Add(lblDesc);

            if (!hasParams)
            {
                dlg.ClientSize = new Size(480, 140);
                var btnOk0 = new System.Windows.Forms.Button
                { Text = Lang.T("dlg.close"), DialogResult = DialogResult.OK, Location = new Point(185, 98), Size = new Size(90, 28) };
                dlg.Controls.Add(btnOk0); dlg.AcceptButton = btnOk0;
                if (checkBox11.Checked) ApplyThemeToControls(dlg.Controls, Color.FromArgb(30, 30, 30), Color.FromArgb(220, 220, 220), Color.FromArgb(55, 55, 55), true);
                dlg.ShowDialog(this); return;
            }

            var lblParam = new System.Windows.Forms.Label { Text = paramLabel, Location = new Point(12, 68), AutoSize = true };
            dlg.Controls.Add(lblParam);

            int txtW = idx == 1 ? 364 : 456;
            var txtValue = new TextBox
            {
                Text = configKey.Length > 0 ? (appSettings[configKey] ?? "") : "",
                Location = new Point(12, 88),
                Size = new Size(txtW, isNote ? 80 : 25),
                Multiline = isNote,
                ScrollBars = isNote ? ScrollBars.Vertical : ScrollBars.None
            };
            dlg.Controls.Add(txtValue);

            if (idx == 1)
            {
                var btnBrw = new System.Windows.Forms.Button
                { Text = "", Location = new Point(txtValue.Right + 4, 88), Size = new Size(30, 25), FlatStyle = FlatStyle.Flat };
                SetIcon(btnBrw, Ico.Folder, 16);
                btnBrw.Click += (s, ev) =>
                {
                    using var ofd = new OpenFileDialog { Filter = Lang.T("dlg.filterPrograms"), Title = Lang.T("dlg.pickProgram") };
                    if (ofd.ShowDialog() == DialogResult.OK) txtValue.Text = ofd.FileName;
                };
                dlg.Controls.Add(btnBrw);
            }

            int btnY = isNote ? (88 + 80 + 12) : 124;
            dlg.ClientSize = new Size(480, btnY + 48);

            var btnSave = new System.Windows.Forms.Button { Text = Lang.T("dlg.save"), Location = new Point(12, btnY), Size = new Size(90, 28), FlatStyle = FlatStyle.Flat };
            var btnCancel2 = new System.Windows.Forms.Button { Text = Lang.T("dlg.cancel"), Location = new Point(108, btnY), Size = new Size(90, 28), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            btnSave.Click += (s, ev) => { appSettings[configKey] = txtValue.Text.Trim(); SaveConfigFile(); dlg.DialogResult = DialogResult.OK; dlg.Close(); };
            dlg.Controls.Add(btnSave); dlg.Controls.Add(btnCancel2);
            dlg.AcceptButton = btnSave; dlg.CancelButton = btnCancel2;
            if (checkBox11.Checked) ApplyThemeToControls(dlg.Controls, Color.FromArgb(30, 30, 30), Color.FromArgb(220, 220, 220), Color.FromArgb(55, 55, 55), true);
            dlg.ShowDialog(this);
        }

        private void ShowScreenshotSettingsDialog()
        {

            string fmtKey = "ss_format";
            string cntKey = "ss_count";
            string dlyKey = "ss_delay";
            string intKey = "ss_interval";
            string zipKey = "ss_compress";
            string dirKey = "ss_folder";

            using var dlg = new Form
            {
                Text = Lang.T("dlg.ssTitle"),
                ClientSize = new Size(420, 290),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false, MinimizeBox = false,
                BackColor = this.BackColor
            };

            int row = 14;
            void AddRow(string lbl, Control ctrl)
            {
                var lbCtrl = new System.Windows.Forms.Label { Text = lbl, Location = new Point(12, row + 3), AutoSize = true };
                dlg.Controls.Add(lbCtrl);
                ctrl.Location = new Point(150, row);
                ctrl.Size = new Size(248, 23);
                dlg.Controls.Add(ctrl);
                row += 32;
            }

            var cboFmt = new System.Windows.Forms.ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cboFmt.Items.AddRange(new object[] { "PNG", "JPG", "BMP", "GIF" });
            string curFmt = appSettings[fmtKey] ?? "PNG";
            cboFmt.SelectedIndex = Math.Max(0, cboFmt.Items.IndexOf(curFmt));
            AddRow(Lang.T("dlg.ssFormat"), cboFmt);

            var nudCount = new System.Windows.Forms.NumericUpDown
            { Minimum = 1, Maximum = 99, Value = int.TryParse(appSettings[cntKey], out int cv) ? Math.Max(1, cv) : 1 };
            AddRow(Lang.T("dlg.ssCount"), nudCount);

            var nudDelay = new System.Windows.Forms.NumericUpDown
            { Minimum = 0, Maximum = 60000, Increment = 500, Value = int.TryParse(appSettings[dlyKey], out int dv) ? dv : 0 };
            AddRow(Lang.T("dlg.ssDelay"), nudDelay);

            var nudInterval = new System.Windows.Forms.NumericUpDown
            { Minimum = 100, Maximum = 60000, Increment = 500, Value = int.TryParse(appSettings[intKey], out int iv) ? iv : 1000 };
            AddRow(Lang.T("dlg.ssInterval"), nudInterval);

            var chkZip = new System.Windows.Forms.CheckBox
            { Text = Lang.T("dlg.ssZip"), Checked = appSettings[zipKey] == "1", Location = new Point(150, row), AutoSize = true };
            dlg.Controls.Add(chkZip);
            row += 32;

            var txtFolder = new System.Windows.Forms.TextBox { Text = appSettings[dirKey] ?? "" };
            txtFolder.Location = new Point(150, row);
            txtFolder.Size = new Size(215, 23);
            var lbFolder = new System.Windows.Forms.Label { Text = Lang.T("dlg.ssFolder"), Location = new Point(12, row + 3), AutoSize = true };
            dlg.Controls.Add(lbFolder);
            dlg.Controls.Add(txtFolder);
            var btnBrowse = new System.Windows.Forms.Button
            { Text = "", Location = new Point(368, row), Size = new Size(28, 23), FlatStyle = FlatStyle.Flat };
            SetIcon(btnBrowse, Ico.Folder, 14);
            dlg.Controls.Add(btnBrowse);
            btnBrowse.Click += (s2, ev2) =>
            {
                using var fbd = new FolderBrowserDialog { SelectedPath = txtFolder.Text };
                if (fbd.ShowDialog() == DialogResult.OK) txtFolder.Text = fbd.SelectedPath;
            };
            row += 36;

            var btnOk2 = new System.Windows.Forms.Button
            { Text = Lang.T("dlg.save"), Location = new Point(150, row), Size = new Size(88, 28), FlatStyle = FlatStyle.Flat };
            var btnCancel3 = new System.Windows.Forms.Button
            { Text = Lang.T("dlg.cancel"), Location = new Point(244, row), Size = new Size(88, 28), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            btnOk2.Click += (s2, ev2) =>
            {
                appSettings[fmtKey] = cboFmt.Text;
                appSettings[cntKey] = ((int)nudCount.Value).ToString();
                appSettings[dlyKey] = ((int)nudDelay.Value).ToString();
                appSettings[intKey] = ((int)nudInterval.Value).ToString();
                appSettings[zipKey] = chkZip.Checked ? "1" : "0";
                appSettings[dirKey] = txtFolder.Text.Trim();
                SaveConfigFile();
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };
            dlg.Controls.Add(btnOk2);
            dlg.Controls.Add(btnCancel3);
            dlg.AcceptButton = btnOk2;
            dlg.CancelButton = btnCancel3;
            dlg.ClientSize = new Size(420, row + 48);

            if (checkBox11.Checked)
                ApplyThemeToControls(dlg.Controls, Color.FromArgb(30, 30, 30), Color.FromArgb(220, 220, 220), Color.FromArgb(55, 55, 55), true);
            dlg.ShowDialog(this);
        }

        private (string type, int value) ShowRepeatIntervalDialog()
        {
            bool dark    = appSettings["darkModeEnabled"] == "1";
            string sType = appSettings["timer3_repeat_type"]  ?? "m";
            int    sVal  = int.TryParse(appSettings["timer3_repeat_value"], out int sv) ? sv : 30;

            using var dlg = new Form
            {
                Text            = Lang.T("dlg.repeatTitle"),
                ClientSize      = new Size(320, 160),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition   = FormStartPosition.CenterParent,
                MaximizeBox     = false, MinimizeBox = false,
                BackColor       = this.BackColor
            };

            dlg.Controls.Add(new System.Windows.Forms.Label
            { Text = Lang.T("dlg.repeatLabel"), Location = new Point(12, 14), AutoSize = true });

            var nud = new System.Windows.Forms.NumericUpDown
            { Minimum = 1, Maximum = 99999, Value = Math.Max(1, sVal),
              Location = new Point(12, 40), Size = new Size(80, 23) };
            dlg.Controls.Add(nud);

            var repUnits = Lang.A("repeatUnits");
            var rbMin  = new System.Windows.Forms.RadioButton { Text = repUnits[0], Location = new Point(102, 41), AutoSize = true, Checked = sType == "m" };
            var rbHour = new System.Windows.Forms.RadioButton { Text = repUnits[1], Location = new Point(176, 41), AutoSize = true, Checked = sType == "h" };
            var rbDay  = new System.Windows.Forms.RadioButton { Text = repUnits[2], Location = new Point(250, 41), AutoSize = true, Checked = sType == "d" };
            dlg.Controls.Add(rbMin); dlg.Controls.Add(rbHour); dlg.Controls.Add(rbDay);

            if (!rbMin.Checked && !rbHour.Checked && !rbDay.Checked) rbMin.Checked = true;

            string resultType = ""; int resultValue = 0;
            var btnOk = new System.Windows.Forms.Button
            { Text = Lang.T("dlg.save"), Location = new Point(70, 110), Size = new Size(80, 28), FlatStyle = FlatStyle.Flat };
            var btnCancel = new System.Windows.Forms.Button
            { Text = Lang.T("dlg.cancel"), Location = new Point(162, 110), Size = new Size(80, 28),
              FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            btnOk.Click += (s2, ev2) =>
            {
                resultType  = rbMin.Checked ? "m" : rbHour.Checked ? "h" : "d";
                resultValue = (int)nud.Value;
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };
            dlg.Controls.Add(btnOk);
            dlg.Controls.Add(btnCancel);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            if (dark) ApplyThemeToControls(dlg.Controls,
                Color.FromArgb(30, 30, 30), Color.FromArgb(220, 220, 220), Color.FromArgb(55, 55, 55), true);
            dlg.ShowDialog(this);
            return (resultType, resultValue);
        }

        private void ShowSoundSettingsDialog(int activatorNum)
        {
            string modeName = Lang.T(activatorNum switch
            {
                2 => "mode.timer",
                3 => "mode.schedule",
                _ => "mode.idle"
            });

            using var dlg = new Form
            {
                Text            = Lang.T("dlg.soundTitle"),
                ClientSize      = new Size(500, 272),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition   = FormStartPosition.CenterParent,
                MaximizeBox     = false,
                MinimizeBox     = false,
                ShowInTaskbar   = false,
                Icon            = this.Icon,
                BackColor       = this.BackColor
            };

            dlg.Controls.Add(new System.Windows.Forms.Label
            {
                Text     = Lang.T("snd.hint"),
                Location = new Point(14, 12),
                Size     = new Size(472, 36),
                AutoSize = false
            });

            var chkShared = new System.Windows.Forms.CheckBox
            {
                Text     = Lang.T("snd.useShared"),
                Location = new Point(14, 54),
                Size     = new Size(472, 21),
                Checked  = activatorNum <= 0 ||
                           string.IsNullOrWhiteSpace(appSettings[$"act{activatorNum}_sound"]),
                Enabled  = activatorNum > 0
            };
            dlg.Controls.Add(chkShared);

            var lblTarget = new System.Windows.Forms.Label
            {
                Location = new Point(14, 86),
                Size     = new Size(472, 17),
                AutoSize = false,
                Font     = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            dlg.Controls.Add(lblTarget);

            dlg.Controls.Add(new System.Windows.Forms.Label
            { Text = Lang.T("snd.list"), Location = new Point(14, 112), AutoSize = true });

            var cbo = new System.Windows.Forms.ComboBox
            {
                Location      = new Point(14, 132),
                Size          = new Size(472, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var f in SoundCatalog.Files) cbo.Items.Add(SoundCatalog.DisplayName(f));
            cbo.Items.Add(Lang.T("snd.custom"));
            int customIndex = cbo.Items.Count - 1;
            dlg.Controls.Add(cbo);

            var lblPath = new System.Windows.Forms.Label
            { Text = Lang.T("snd.customPath"), Location = new Point(14, 166), AutoSize = true };
            dlg.Controls.Add(lblPath);

            var txtFile = new TextBox { Location = new Point(14, 186), Size = new Size(434, 25) };
            dlg.Controls.Add(txtFile);

            var btnBrw = new System.Windows.Forms.Button
            { Text = "", Location = new Point(454, 186), Size = new Size(32, 25), FlatStyle = FlatStyle.Flat };
            SetIcon(btnBrw, Ico.Folder, 16);
            btnBrw.Click += (s, ev) =>
            {
                using var ofd = new OpenFileDialog { Filter = Lang.T("dlg.soundFilter"), Title = Lang.T("dlg.soundPick") };
                if (ofd.ShowDialog(dlg) == DialogResult.OK)
                {
                    txtFile.Text = ofd.FileName;
                    cbo.SelectedIndex = customIndex;
                }
            };
            dlg.Controls.Add(btnBrw);

            void LoadValue()
            {
                string value = chkShared.Checked
                    ? (appSettings["soundFile"] ?? "").Trim()
                    : (appSettings[$"act{activatorNum}_sound"] ?? "").Trim();
                if (string.IsNullOrEmpty(value)) value = SoundCatalog.DefaultFile;

                int idx = SoundCatalog.Files.ToList()
                    .FindIndex(f => string.Equals(f, value, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    cbo.SelectedIndex = idx;
                    txtFile.Text = "";
                }
                else
                {
                    cbo.SelectedIndex = customIndex;
                    txtFile.Text = value;
                }

                lblTarget.Text = chkShared.Checked
                    ? Lang.T("snd.shared")
                    : Lang.T("snd.forMode", modeName);
            }

            void UpdateCustomState()
            {
                bool custom = cbo.SelectedIndex == customIndex;
                lblPath.Enabled = custom;
                txtFile.Enabled = custom;
                btnBrw.Enabled  = custom;
            }

            string CurrentValue() => cbo.SelectedIndex == customIndex
                ? txtFile.Text.Trim()
                : SoundCatalog.Files[Math.Max(0, cbo.SelectedIndex)];

            cbo.SelectedIndexChanged += (s, ev) => UpdateCustomState();
            chkShared.CheckedChanged += (s, ev) => { LoadValue(); UpdateCustomState(); };
            LoadValue();
            UpdateCustomState();

            var btnPlay = new System.Windows.Forms.Button
            { Text = Lang.T("dlg.soundTest"), Location = new Point(14, 224), Size = new Size(110, 30), FlatStyle = FlatStyle.Flat };
            SetIcon(btnPlay, Ico.Play, 14);
            btnPlay.Click += (s, ev) => PlaySoundFile(SoundCatalog.ResolvePath(CurrentValue()));
            dlg.Controls.Add(btnPlay);

            var btnStop = new System.Windows.Forms.Button
            { Text = Lang.T("dlg.soundStop"), Location = new Point(132, 224), Size = new Size(110, 30), FlatStyle = FlatStyle.Flat };
            SetIcon(btnStop, Ico.Stop, 14);
            btnStop.Click += (s, ev) => StopSound();
            dlg.Controls.Add(btnStop);

            var btnSave = new System.Windows.Forms.Button
            { Text = Lang.T("dlg.save"), Location = new Point(286, 224), Size = new Size(96, 30), FlatStyle = FlatStyle.Flat };
            var btnCancel = new System.Windows.Forms.Button
            { Text = Lang.T("dlg.cancel"), Location = new Point(390, 224), Size = new Size(96, 30), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            btnSave.Click += (s, ev) =>
            {
                string value = CurrentValue();
                if (chkShared.Checked)
                {
                    appSettings["soundFile"] = value;
                    if (activatorNum > 0) appSettings[$"act{activatorNum}_sound"] = "";
                }
                else
                {
                    appSettings[$"act{activatorNum}_sound"] = value;
                }
                SaveConfigFile();
                StopSound();
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };
            dlg.Controls.Add(btnSave);
            dlg.Controls.Add(btnCancel);
            dlg.AcceptButton = btnSave;
            dlg.CancelButton = btnCancel;

            if (checkBox11.Checked) ApplyThemeToControls(dlg.Controls,
                Color.FromArgb(30, 30, 30), Color.FromArgb(220, 220, 220), Color.FromArgb(55, 55, 55), true);
            dlg.ShowDialog(this);
            StopSound();
        }

        private static bool IsAdmin()
        {
            using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
            return new System.Security.Principal.WindowsPrincipal(id)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        private void ShowCleanupSettingsDialog()
        {
            var options = new (string Key, string LabelKey, string TipKey, bool NeedsAdmin)[]
            {
                ("cleanTemp",        "clean.userTemp",  "clean.userTempTip",  false),
                ("cleanWinTemp",     "clean.winTemp",   "clean.winTempTip",   true),
                ("cleanDownloads",   "clean.downloads", "clean.downloadsTip", false),
                ("cleanWU",          "clean.wu",        "clean.wuTip",        true),
                ("cleanDriverCache", "clean.driver",    "clean.driverTip",    true),
                ("cleanThumbs",      "clean.thumbs",    "clean.thumbsTip",    false),
                ("cleanPrefetch",    "clean.prefetch",  "clean.prefetchTip",  false),
                ("cleanRecycleBin",  "clean.bin",       "clean.binTip",       false),
                ("cleanEventLog",    "clean.log",       "clean.logTip",       true),
            };

            bool adminNow = IsAdmin();

            using var dlg = new Form
            {
                Text = Lang.T("dlg.cleanTitle"),
                ClientSize = new Size(520, options.Length * 28 + 210),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = this.BackColor
            };
            using var tip = new System.Windows.Forms.ToolTip();
            var checkboxes = new List<System.Windows.Forms.CheckBox>();
            int y = 10;
            foreach (var opt in options)
            {
                bool disabled = opt.NeedsAdmin && !adminNow;
                var cb = new System.Windows.Forms.CheckBox
                {
                    Text = Lang.T(opt.LabelKey) + (disabled ? Lang.T("dlg.needsAdmin") : ""),
                    Checked = !disabled && appSettings[opt.Key] != "0",
                    Enabled = !disabled,
                    Location = new Point(12, y),
                    AutoSize = true,
                    Tag = opt.Key,
                    ForeColor = disabled ? Color.Gray : SystemColors.ControlText
                };
                tip.SetToolTip(cb, Lang.T(opt.TipKey) + (disabled ? Lang.T("dlg.needsAdminTip") : ""));
                dlg.Controls.Add(cb);
                checkboxes.Add(cb);
                y += 28;
            }

            y += 6;
            dlg.Controls.Add(new System.Windows.Forms.Label
            {
                Text = Lang.T("dlg.cleanExtra"),
                Location = new Point(12, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });
            y += 22;
            var customFolderList = new System.Windows.Forms.ListBox
            {
                Location = new Point(12, y),
                Size = new Size(392, 70),
                BorderStyle = BorderStyle.FixedSingle
            };

            var savedCustom = (appSettings["cleanCustomFolders"] ?? "")
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !string.IsNullOrWhiteSpace(p));
            foreach (var p in savedCustom) customFolderList.Items.Add(p);
            dlg.Controls.Add(customFolderList);

            var btnAddFolder = new System.Windows.Forms.Button
            {
                Text = Lang.T("dlg.cleanAdd"),
                Location = new Point(412, y),
                Size = new Size(96, 32),
                FlatStyle = FlatStyle.Flat
            };
            btnAddFolder.Click += (s, ev) =>
            {
                using var fbd = new FolderBrowserDialog { Description = Lang.T("dlg.cleanPickDir"), UseDescriptionForTitle = true };
                if (fbd.ShowDialog(dlg) == DialogResult.OK && !customFolderList.Items.Contains(fbd.SelectedPath))
                    customFolderList.Items.Add(fbd.SelectedPath);
            };
            SetIcon(btnAddFolder, Ico.Plus, 14);
            dlg.Controls.Add(btnAddFolder);

            var btnRemoveFolder = new System.Windows.Forms.Button
            {
                Text = Lang.T("dlg.cleanRemove"),
                Location = new Point(412, y + 36),
                Size = new Size(96, 32),
                FlatStyle = FlatStyle.Flat
            };
            btnRemoveFolder.Click += (s, ev) =>
            {
                if (customFolderList.SelectedIndex >= 0)
                    customFolderList.Items.RemoveAt(customFolderList.SelectedIndex);
            };
            SetIcon(btnRemoveFolder, Ico.Close, 14);
            dlg.Controls.Add(btnRemoveFolder);
            y += 78;
            var btnSave = new System.Windows.Forms.Button { Text = Lang.T("dlg.save"), Location = new Point(12, y + 8), Size = new Size(90, 28), FlatStyle = FlatStyle.Flat };
            var btnCancel = new System.Windows.Forms.Button { Text = Lang.T("dlg.cancel"), Location = new Point(108, y + 8), Size = new Size(90, 28), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            btnSave.Click += (s, ev) =>
            {
                foreach (var cb in checkboxes)
                    appSettings[cb.Tag as string ?? ""] = cb.Checked ? "1" : "0";

                appSettings["cleanCustomFolders"] = string.Join(";", customFolderList.Items.Cast<string>());
                SaveConfigFile();
                dlg.DialogResult = DialogResult.OK; dlg.Close();
            };

            var btnAdmin = new System.Windows.Forms.Button
            {
                Text = Lang.T(adminNow ? "dlg.runningAsAdmin" : "dlg.runAsAdminNow"),
                Enabled = !adminNow,
                Location = new Point(210, y + 8),
                Size = new Size(288, 28),
                FlatStyle = FlatStyle.Flat
            };
            SetIcon(btnAdmin, adminNow ? Ico.Check : Ico.Shield);
            tip.SetToolTip(btnAdmin, Lang.T(adminNow ? "tip.runAsAdminOn" : "dlg.runAsAdminTip"));
            btnAdmin.Click += (s, ev) =>
            {

                foreach (var cb in checkboxes)
                    appSettings[cb.Tag as string ?? ""] = cb.Checked ? "1" : "0";
                SaveConfigFile();

                try
                {
                    int pid  = Environment.ProcessId;
                    string exe = Application.ExecutablePath.Replace("'", "''");
                    string cmdArgs = $"/c taskkill /F /PID {pid} & timeout /t 2 /nobreak > nul & powershell -WindowStyle Hidden -NonInteractive -Command \"Start-Process -FilePath '{exe}' -Verb RunAs\"";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName        = "cmd.exe",
                        Arguments       = cmdArgs,
                        UseShellExecute = false,
                        CreateNoWindow  = true,
                        WindowStyle     = ProcessWindowStyle.Hidden
                    });
                    Environment.Exit(0);
                }
                catch {  }
            };

            dlg.Controls.Add(btnSave);
            dlg.Controls.Add(btnCancel);
            dlg.Controls.Add(btnAdmin);
            dlg.AcceptButton = btnSave;
            dlg.CancelButton = btnCancel;
            if (checkBox11.Checked) ApplyThemeToControls(dlg.Controls, Color.FromArgb(30, 30, 30), Color.FromArgb(220, 220, 220), Color.FromArgb(55, 55, 55), true);
            dlg.ShowDialog(this);
        }

        private void ShowBackupRestoreDialog()
        {
            bool dark = appSettings["darkModeEnabled"] == "1";
            using var dlg = new Form
            {
                Text = Lang.T("dlg.backupTitle"),
                ClientSize = new Size(440, 195),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false, MinimizeBox = false,
                BackColor = this.BackColor
            };

            dlg.Controls.Add(new System.Windows.Forms.Label
            {
                Text = Lang.T("dlg.backupInfo"),
                Location = new Point(12, 12), Size = new Size(416, 36), AutoSize = false
            });

            var btnBackup = new System.Windows.Forms.Button
            {
                Text = Lang.T("dlg.backupCreate"),
                Location = new Point(12, 58), Size = new Size(200, 36), FlatStyle = FlatStyle.Flat
            };
            btnBackup.Click += (s, ev) =>
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = Lang.T("dlg.backupFilter"),
                    FileName = $"TimerSys_backup_{DateTime.Now:yyyyMMdd_HHmm}.dat",
                    Title = Lang.T("dlg.backupSaveTitle")
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;
                try
                {
                    string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
                    using var zip = new ZipArchive(File.Create(sfd.FileName), ZipArchiveMode.Create);
                    foreach (var fn in new[] { "config.json", "psd.dat" })
                    {
                        string src = Path.Combine(dataDir, fn);
                        if (File.Exists(src)) zip.CreateEntryFromFile(src, fn);
                    }
                    MessageBox.Show(Lang.T("dlg.backupOk"), Lang.T("dlg.success"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Lang.T("dlg.backupFail", ex.Message), Lang.T("msg.error"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            SetIcon(btnBackup, Ico.Save);
            dlg.Controls.Add(btnBackup);

            var btnRestore = new System.Windows.Forms.Button
            {
                Text = Lang.T("dlg.backupRestore"),
                Location = new Point(224, 58), Size = new Size(200, 36), FlatStyle = FlatStyle.Flat
            };
            btnRestore.Click += (s, ev) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Filter = Lang.T("dlg.backupFilterOpen"),
                    Title = Lang.T("dlg.backupOpenTitle")
                };
                if (ofd.ShowDialog() != DialogResult.OK) return;
                if (MessageBox.Show(Lang.T("dlg.restoreWarn"),
                    Lang.T("dlg.confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                try
                {
                    string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
                    Directory.CreateDirectory(dataDir);
                    string dataRoot = Path.GetFullPath(dataDir + Path.DirectorySeparatorChar);
                    using var zip = new ZipArchive(File.OpenRead(ofd.FileName), ZipArchiveMode.Read);
                    foreach (var entry in zip.Entries)
                    {
                        if (string.IsNullOrEmpty(Path.GetFileName(entry.FullName))) continue;

                        string dest = Path.GetFullPath(Path.Combine(dataRoot, entry.FullName));
                        if (!dest.StartsWith(dataRoot, StringComparison.OrdinalIgnoreCase)) continue;

                        string? destDir = Path.GetDirectoryName(dest);
                        if (!string.IsNullOrEmpty(destDir)) Directory.CreateDirectory(destDir);
                        entry.ExtractToFile(dest, true);
                    }
                    MessageBox.Show(Lang.T("dlg.restoreOk"),
                        Lang.T("dlg.success"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                    int pid2 = Environment.ProcessId;
                    string exePath = Application.ExecutablePath;
                    Process.Start(new ProcessStartInfo
                    {
                        FileName        = "cmd.exe",
                        Arguments       = $"/c taskkill /F /PID {pid2} & timeout /t 2 /nobreak > nul & start \"\" \"{exePath}\"",
                        UseShellExecute = false,
                        CreateNoWindow  = true,
                        WindowStyle     = ProcessWindowStyle.Hidden
                    });
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Lang.T("dlg.restoreFail", ex.Message), Lang.T("msg.error"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            SetIcon(btnRestore, Ico.FolderOpen);
            dlg.Controls.Add(btnRestore);

            var btnClose = new System.Windows.Forms.Button
            {
                Text = Lang.T("dlg.close"), Location = new Point(175, 150), Size = new Size(90, 28),
                FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel
            };
            dlg.Controls.Add(btnClose);
            dlg.CancelButton = btnClose;

            if (dark) ApplyThemeToControls(dlg.Controls,
                Color.FromArgb(30, 30, 30), Color.FromArgb(220, 220, 220), Color.FromArgb(55, 55, 55), true);
            dlg.ShowDialog(this);
        }

        private void ShowHelpDialog()
        {
            if (HelpFile.Open()) return;
            MessageBox.Show(Lang.T("help.missing"), Lang.T("msg.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            _menuExit = new ToolStripMenuItem(Lang.T("tray.exit"), null, (s, ev) =>
            {

                _forceExit = true;
                Close();
            });
            _menuStopIdle = new ToolStripMenuItem(Lang.T("tray.stopIdle"), null, (s, ev) =>
            {
                timer1.Stop();
                _lastSecond1 = -1;
                textBox5.Text = "";
            });
            _menuStopTimer = new ToolStripMenuItem(Lang.T("tray.stopTimer"), null, (s, ev) =>
            {
                timer2.Stop();
                _lastSecond2 = -1;
                textBox9.Text = "";
            });
            _menuResumeIdle = new ToolStripMenuItem(Lang.T("tray.resumeIdle"), null, (s, ev) =>
            {
                if (TryParseTime(textBox4.Text, out var ts4m))
                    timer1EndTime = DateTime.Now.Add(ts4m);
                timer1.Start();
            });
            _menuResumeTimer = new ToolStripMenuItem(Lang.T("tray.resumeTimer"), null, (s, ev) =>
            {

                if (!TryGetTimerTarget(out DateTime fullSelectedDateTime, out _)) return;

                TimeSpan timeToWait = fullSelectedDateTime - DateTime.Now;
                if (timeToWait.TotalSeconds > 0)
                {
                    _timer2LocalTarget = fullSelectedDateTime;
                    timer2EndTime = DateTime.UtcNow.Add(timeToWait);
                    timer2.Start();
                }
            });
            menu.Items.AddRange(new ToolStripItem[]
            {
                _menuExit, _menuStopIdle, _menuStopTimer, _menuResumeIdle, _menuResumeTimer
            });

            var menuIconColor = Color.FromArgb(70, 70, 74);
            _menuExit.Image        = VectorIcons.Get(Ico.Close, 16, menuIconColor);
            _menuStopIdle.Image    = VectorIcons.Get(Ico.Stop,  16, menuIconColor);
            _menuStopTimer.Image   = VectorIcons.Get(Ico.Stop,  16, menuIconColor);
            _menuResumeIdle.Image  = VectorIcons.Get(Ico.Play,  16, menuIconColor);
            _menuResumeTimer.Image = VectorIcons.Get(Ico.Play,  16, menuIconColor);
            notifyIcon1.ContextMenuStrip = menu;

            label19.Location = new Point(0, 0);
            label19.Size = new Size(this.ClientSize.Width, this.ClientSize.Height);
            label19.BringToFront();

            label19.Click -= label19_Click;
            label19.Click += label19_Click;
            label19.Cursor = Cursors.Hand;

            SetUiLocked(checkBox6.Checked);

            textBoxUser.Width = 511;
            var btnBrowse = new System.Windows.Forms.Button
            {
                Text = "",
                Width = 30,
                Height = textBoxUser.Height,
                Location = new Point(textBoxUser.Right + 4, textBoxUser.Top),
                FlatStyle = FlatStyle.Flat,
                TabIndex = 100
            };
            var ttip = new System.Windows.Forms.ToolTip();
            _disposables.Add(ttip);
            RegTip(ttip, btnBrowse, "tip.browseFile");
            btnBrowse.Click += (s, ev) =>
            {
                using var dlg = new OpenFileDialog
                {
                    Title = "Wybierz plik do uruchomienia",
                    Filter = "Programy i skrypty|*.exe;*.bat;*.cmd;*.ps1;*.vbs|Wszystkie pliki|*.*"
                };
                if (dlg.ShowDialog() == DialogResult.OK) textBoxUser.Text = dlg.FileName;
            };
            groupBox1.Controls.Add(btnBrowse);

            var presetMenu = new ContextMenuStrip();
            var presetCommands = new[] { "notepad.exe", "calc.exe", "taskmgr.exe", "explorer.exe", "cmd.exe /k", "powershell.exe" };
            presetMenu.Opening += (s, ev) =>
            {
                presetMenu.Items.Clear();
                var names = Lang.A("presets");
                for (int i = 0; i < presetCommands.Length && i < names.Length; i++)
                {
                    string cmd = presetCommands[i];
                    presetMenu.Items.Add(names[i], null, (s2, ev2) => textBoxUser.Text = cmd);
                }
            };
            var btnPreset = new System.Windows.Forms.Button
            {
                Text = "",
                Width = 28,
                Height = textBoxUser.Height,
                Location = new Point(btnBrowse.Right + 2, textBoxUser.Top),
                FlatStyle = FlatStyle.Flat,
                TabIndex = 101
            };
            RegTip(ttip, btnPreset, "tip.presets");
            btnPreset.Click += (s, ev) => presetMenu.Show(btnPreset, new Point(0, btnPreset.Height));
            groupBox1.Controls.Add(btnPreset);
            RegIcon(btnBrowse, Ico.Folder, 16);
            RegIcon(btnPreset, Ico.ChevronDown, 14);

            _lblScheduleStatus = new System.Windows.Forms.Label
            {
                Name = "lblScheduleStatus",
                Text = "Harmonogram nieaktywny",
                Location = new Point(20, 336),
                Size = new Size(620, 18),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.DimGray,
                AutoSize = false
            };
            tabPage6.Controls.Add(_lblScheduleStatus);
            _lblScheduleStatus.BringToFront();

            var sepLine = _sepSnooze = new System.Windows.Forms.Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(20, 360),
                Size = new Size(620, 2),
                AutoSize = false
            };
            tabPage6.Controls.Add(sepLine);

            var lblSnoozeHeader = _lblSnoozeHeader = new System.Windows.Forms.Label
            {
                Text = Lang.T("snooze.header"),
                Location = new Point(20, 368),
                Size = new Size(300, 17),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = false
            };
            tabPage6.Controls.Add(lblSnoozeHeader);

            var cboSnooze = _cboSnooze = new System.Windows.Forms.ComboBox
            {
                Location = new Point(20, 390),
                Size = new Size(130, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            cboSnooze.Items.AddRange(Lang.A("snoozeItems"));
            cboSnooze.SelectedIndex = 0;
            tabPage6.Controls.Add(cboSnooze);

            var btnSnooze = _btnSnooze = new System.Windows.Forms.Button
            {
                Text = Lang.T("snooze.run"),
                Location = new Point(156, 389),
                Size = new Size(95, 26),
                FlatStyle = FlatStyle.Flat
            };
            tabPage6.Controls.Add(btnSnooze);

            _lblSnoozeStatus = new System.Windows.Forms.Label
            {
                Text = "",
                Location = new Point(262, 394),
                Size = new Size(300, 18),
                ForeColor = Color.Coral,
                AutoSize = false
            };
            tabPage6.Controls.Add(_lblSnoozeStatus);

            _timerSnooze.Tick += (s, ev) =>
            {
                var rem = _snoozeEndTime - DateTime.Now;
                if (rem.TotalSeconds <= 0)
                {
                    _timerSnooze.Stop();
                    btnSnooze.Enabled = true;
                    _lblSnoozeStatus.Text = "";
                    if (comboBox3.SelectedIndex >= 0)
                        ExecuteCommand(ActionIdFromIndex(comboBox3.SelectedIndex), 3);
                }
                else
                {
                    _lblSnoozeStatus.Text = Lang.T("snooze.counting", rem.ToString(@"mm\:ss"));
                }
            };
            btnSnooze.Click += (s, ev) =>
            {
                int[] minutes = { 5, 10, 30, 60, 180, 300, 600 };
                int idx = cboSnooze.SelectedIndex;
                if (idx < 0 || idx >= minutes.Length) return;
                if (comboBox3.SelectedIndex < 0)
                {
                    MessageBox.Show(Lang.T("snooze.pickAction"),
                        Lang.T("msg.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                _snoozeEndTime = DateTime.Now.AddMinutes(minutes[idx]);
                _timerSnooze.Start();
                btnSnooze.Enabled = false;
            };

            int settingsBtnX = comboBox1.Right + 5;
            int settingsBtnY = comboBox1.Top;

            _btnActSettings1 = new System.Windows.Forms.Button
            {
                Text = "⚙",
                Size = new Size(30, comboBox1.Height),
                Location = new Point(settingsBtnX, settingsBtnY),
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
            RegTip(_actionTip, _btnActSettings1, "tip.actionSettings");
            _btnActSettings1.Click += (s, ev) => ShowActionSettingsDialog(comboBox1, 1);
            _btnActSettings1.Enabled = ActionHasParams(comboBox1.SelectedIndex);
            tabPage1.Controls.Add(_btnActSettings1);

            _btnActSettings2 = new System.Windows.Forms.Button
            {
                Text = "⚙",
                Size = new Size(30, comboBox2.Height),
                Location = new Point(comboBox2.Right + 5, comboBox2.Top),
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
            RegTip(_actionTip, _btnActSettings2, "tip.actionSettings");
            _btnActSettings2.Click += (s, ev) => ShowActionSettingsDialog(comboBox2, 2);
            _btnActSettings2.Enabled = ActionHasParams(comboBox2.SelectedIndex);
            tabPage2.Controls.Add(_btnActSettings2);

            _btnActSettings3 = new System.Windows.Forms.Button
            {
                Text = "⚙",
                Size = new Size(30, comboBox3.Height),
                Location = new Point(comboBox3.Right + 5, comboBox3.Top),
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
            RegTip(_actionTip, _btnActSettings3, "tip.actionSettings");
            _btnActSettings3.Click += (s, ev) => ShowActionSettingsDialog(comboBox3, 3);
            _btnActSettings3.Enabled = ActionHasParams(comboBox3.SelectedIndex);
            tabPage6.Controls.Add(_btnActSettings3);

            var settTip = new System.Windows.Forms.ToolTip();
            _disposables.Add(settTip);
            RegTip(settTip, checkBox1,  "tip.autoStartWindows");
            RegTip(settTip, checkBox2,  "tip.autoCountdown");
            RegTip(settTip, checkBox3,  "tip.minimizeToTray");
            RegTip(settTip, checkBox4,  "tip.resetCounter");
            RegTip(settTip, checkBox5,  "tip.noSleep");
            RegTip(settTip, checkBox6,  "tip.passwordLock");
            RegTip(settTip, checkBox7,  "tip.waitForProcess");
            RegTip(settTip, checkBox8,  "tip.timezone");
            RegTip(settTip, checkBox9,  "tip.noTray");
            RegTip(settTip, checkBox10, "tip.noCountdown");
            RegTip(settTip, checkBox11, "tip.darkMode");

            _timerStatusUI.Tick += (s, ev) =>
            {
                if (_lblScheduleStatus != null)
                {

                    bool darkUi    = checkBox11.Checked;
                    Color idleCol   = darkUi ? Color.FromArgb(150, 150, 150) : Color.DimGray;
                    Color activeCol = darkUi ? Color.FromArgb(110, 215, 130) : Color.DarkGreen;

                    if (!timer3.Enabled)
                    {
                        _lblScheduleStatus.Text = Lang.T("sched.inactive");
                        _lblScheduleStatus.ForeColor = idleCol;
                    }
                    else
                    {
                        var nextFire = GetNextScheduleFireTime();
                        if (nextFire.HasValue)
                        {
                            var diff = nextFire.Value - DateTime.Now;
                            string ds = diff.TotalHours >= 24
                                ? $"{(int)diff.TotalDays}d {diff.Hours:D2}h {diff.Minutes:D2}m"
                                : $"{(int)diff.TotalHours:D2}h {diff.Minutes:D2}m {diff.Seconds:D2}s";
                            _lblScheduleStatus.Text = Lang.T("sched.next", nextFire.Value.ToString("HH:mm"), ds);
                            _lblScheduleStatus.ForeColor = activeCol;
                        }
                        else
                        {
                            _lblScheduleStatus.Text = Lang.T("sched.noDate");
                            _lblScheduleStatus.ForeColor = idleCol;
                        }
                    }
                }

                {
                    var utcNow = DateTime.UtcNow;
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        if (row.Tag is string rowId)
                        {
                            var t = _taskList.FirstOrDefault(x => x.Id == rowId);
                            if (t != null && t.ActivatorType == 2 && t.Enabled && t.EndTimeUtc != default)
                            {
                                var rem = t.EndTimeUtc - utcNow;
                                row.Cells[1].Value = rem.TotalSeconds > 0
                                    ? FormatRemaining(rem)
                                    : Lang.T("val.expired");
                            }
                            else if (t != null && t.ActivatorType == 1 && t.Enabled)
                            {
                                var th = IdleThresholdFor(t);
                                if (th == null || !timer1.Enabled)
                                {
                                    row.Cells[1].Value = Lang.T("val.waiting");
                                }
                                else
                                {
                                    var left = th.Value - TimeSpan.FromMilliseconds(GetIdleMilliseconds());
                                    row.Cells[1].Value = left.TotalSeconds > 0
                                        ? FormatRemaining(left)
                                        : Lang.T("val.soon");
                                }
                            }
                            else if (t != null && t.ActivatorType == 3 && t.Enabled
                                     && !string.IsNullOrEmpty(t.RepeatIntervalType))
                            {
                                double iMin = t.RepeatIntervalType switch
                                { "h" => t.RepeatIntervalValue * 60.0, "d" => t.RepeatIntervalValue * 1440.0, _ => t.RepeatIntervalValue };
                                var nextFire = t.LastFired.AddMinutes(iMin);
                                var rem = nextFire - DateTime.Now;
                                string unitLabel = Lang.T(t.RepeatIntervalType switch { "h" => "unit.hour", "d" => "unit.day", _ => "unit.min" });
                                row.Cells[1].Value = Lang.T("val.every", t.RepeatIntervalValue, unitLabel) + ": "
                                    + (rem.TotalSeconds > 0 ? FormatRemaining(rem) : Lang.T("val.soon"));
                            }
                        }
                    }
                }
            };
            _timerStatusUI.Start();

            UpdateComboHints(comboBox1, tip01, _btnActSettings1);
            UpdateComboHints(comboBox2, tip02, _btnActSettings2);
            UpdateComboHints(comboBox3, tip03, _btnActSettings3);

            stopStartHarmonogram.Click += (s, ev) => SetHarmonogramRunning(appSettings["timer3UserPaused"] == "1");
            ApplyHarmonogramUI(appSettings["timer3UserPaused"] != "1");

            this.FormClosed += (s, ev) => CleanupResources();

            if (checkBox8.Checked)
                SystemEvents.TimeChanged += SystemEvents_TimeChanged;
            if (checkBox9.Checked)
                notifyIcon1.Visible = false;
            if (checkBox10.Checked)
            {
                textBox5.Visible = false;
                textBox9.Visible = false;
            }
            if (checkBox11.Checked)
                ApplyTheme(true);

            checkBox12.CheckedChanged += (s, ev) =>
            {
                appSettings["autoStartQueue"] = checkBox12.Checked ? "1" : "0";
                SaveConfigFile();
            };
            RegTip(settTip, checkBox12, "tip.autoQueue");

            checkBox16.Checked = appSettings["noBalloon"] == "1";
            checkBox16.CheckedChanged += (s, ev) =>
            {
                appSettings["noBalloon"] = checkBox16.Checked ? "1" : "0";
                SaveConfigFile();
            };
            RegTip(settTip, checkBox16, "tip.noBalloon");

            string adminFlagPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "runasadmin.flag");
            checkBox15.Checked = File.Exists(adminFlagPath);

            if (IsAdmin()) checkBox15.ForeColor = Color.FromArgb(0, 128, 0);
            checkBox15.CheckedChanged += (s, ev) =>
            {
                if (checkBox15.Checked)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(adminFlagPath)!);
                    File.WriteAllText(adminFlagPath, "1");
                    if (!IsAdmin())
                    {
                        var res = MessageBox.Show(
                            Lang.T("dlg.runAsAdminTip"),
                            Lang.T("dlg.runAsAdminNow"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (res == DialogResult.Yes)
                        {
                            try
                            {
                                int pid15 = Environment.ProcessId;
                                string exe15 = Application.ExecutablePath.Replace("'", "''");
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName        = "cmd.exe",
                                    Arguments       = $"/c taskkill /F /PID {pid15} & timeout /t 2 /nobreak > nul & powershell -WindowStyle Hidden -NonInteractive -Command \"Start-Process -FilePath '{exe15}' -Verb RunAs\"",
                                    UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
                                });
                                Environment.Exit(0);
                            }
                            catch { checkBox15.Checked = false; File.Delete(adminFlagPath); }
                        }
                    }
                }
                else { if (File.Exists(adminFlagPath)) File.Delete(adminFlagPath); }
            };
            RegTip(settTip, checkBox15, IsAdmin() ? "tip.runAsAdminOn" : "tip.runAsAdminOff");

            menu.Items.Add(new ToolStripSeparator());
            bool schedPaused = appSettings["timer3UserPaused"] == "1";
            _menuScheduleToggle = new ToolStripMenuItem(
                Lang.T(schedPaused ? "sched.start" : "sched.stop"), null, ToggleScheduleFromTray)
            {
                Image = VectorIcons.Get(schedPaused ? Ico.Play : Ico.Stop, 16, menuIconColor)
            };
            _menuScheduleToggle.Enabled = _taskList.Any(t => t.ActivatorType == 3);
            menu.Items.Add(_menuScheduleToggle);
            _menuSettings = new ToolStripMenuItem(Lang.T("tray.settings"), null, (s, ev) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                notifyIcon1.Visible = false;
                var page = tabControl1.TabPages["tabPage3"];
                if (page != null) tabControl1.SelectedTab = page;
            })
            {
                Image = VectorIcons.Get(Ico.Settings, 16, menuIconColor)
            };
            menu.Items.Add(_menuSettings);

            menu.Opening += (s, ev) =>
            {

                menu.Items[1].Enabled = timer1.Enabled;
                menu.Items[2].Enabled = timer2.Enabled;
                menu.Items[3].Enabled = !timer1.Enabled;
                menu.Items[4].Enabled = !timer2.Enabled;

                if (_menuScheduleToggle != null)
                {
                    _menuScheduleToggle.Enabled = _taskList.Any(t => t.ActivatorType == 3);
                    bool paused = appSettings["timer3UserPaused"] == "1";
                    _menuScheduleToggle.Text  = Lang.T(paused ? "sched.start" : "sched.stop");
                    _menuScheduleToggle.Image = VectorIcons.Get(paused ? Ico.Play : Ico.Stop, 16,
                                                                Color.FromArgb(70, 70, 74));
                }
            };

            button2.Click += (s, ev) => AddTaskAndRun(1);
            button4.Click += (s, ev) => AddTaskAndRun(2);
            button8.Click += (s, ev) => AddTaskAndRun(3);

            helpLink.LinkClicked += (s, ev) => ShowHelpDialog();

            button13.Click += (s, ev) => ShowBackupRestoreDialog();

            dataGridView1.ReadOnly = true;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;

            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            button10.Click += (s, ev) =>
            {
                if (dataGridView1.SelectedRows.Count == 0) return;
                string id = dataGridView1.SelectedRows[0].Tag as string ?? "";
                var removed = _taskList.FirstOrDefault(t => t.Id == id);
                if (removed == null) return;
                int removedType = removed.ActivatorType;
                _taskList.Remove(removed);

                switch (removedType)
                {
                    case 1:
                        if (!_taskList.Any(t => t.ActivatorType == 1 && t.Enabled))
                        { timer1.Stop(); _lastSecond1 = -1; textBox5.Text = ""; }
                        break;
                    case 2:
                        if (!_taskList.Any(t => t.ActivatorType == 2 && t.Enabled && t.EndTimeUtc > DateTime.UtcNow))
                        { timer2.Stop(); _lastSecond2 = -1; textBox9.Text = ""; }
                        break;
                    case 3:
                        if (!_taskList.Any(t => t.ActivatorType == 3 && t.Enabled))
                            timer3.Stop();
                        break;
                }
                SaveConfigFile();
                RefreshTaskGrid();
            };

            button12.Click += (s, ev) =>
            {
                if (dataGridView1.SelectedRows.Count == 0) return;
                string id = dataGridView1.SelectedRows[0].Tag as string ?? "";
                var task = _taskList.FirstOrDefault(t => t.Id == id);
                if (task == null) return;
                task.Enabled = !task.Enabled;

                if (task.Enabled) task.Completed = false;

                if (!task.Enabled)
                {

                    switch (task.ActivatorType)
                    {
                        case 1:
                            if (!_taskList.Any(t => t.ActivatorType == 1 && t.Enabled && t.Id != task.Id))
                            { timer1.Stop(); _lastSecond1 = -1; textBox5.Text = ""; }
                            break;
                        case 2:
                            if (!_taskList.Any(t => t.ActivatorType == 2 && t.Enabled && t.Id != task.Id && t.EndTimeUtc > DateTime.UtcNow))
                            { timer2.Stop(); _lastSecond2 = -1; textBox9.Text = ""; }
                            break;
                        case 3:
                            if (!_taskList.Any(t => t.ActivatorType == 3 && t.Enabled && t.Id != task.Id))
                            { timer3.Stop(); appSettings["timer3Enabled"] = "0"; }
                            break;
                    }
                }
                else
                {

                    switch (task.ActivatorType)
                    {
                        case 1:
                            if (!timer1.Enabled && TryParseTime(textBox4.Text, out var ts4e))
                            { timer1EndTime = DateTime.Now.Add(ts4e); timer1.Start(); }
                            break;
                        case 2:
                            if (!timer2.Enabled && task.EndTimeUtc > DateTime.UtcNow)
                                timer2.Start();
                            break;
                        case 3:
                            if (!timer3.Enabled && appSettings["timer3UserPaused"] != "1"
                                && _taskList.Any(t => t.ActivatorType == 3 && t.Enabled))
                            { timer3.Start(); appSettings["timer3Enabled"] = "1"; }
                            break;
                    }
                }
                SaveConfigFile();
                RefreshTaskGrid();
            };

            button11.Click += (s, ev) =>
            {
                if (dataGridView1.SelectedRows.Count == 0) return;
                string id = dataGridView1.SelectedRows[0].Tag as string ?? "";
                var task = _taskList.FirstOrDefault(t => t.Id == id);
                if (task == null) return;

                bool darkDlg = appSettings["darkModeEnabled"] == "1";
                int dlgH = task.ActivatorType == 3 ? 310 : task.ActivatorType == 2 ? 220 : 195;
                using var editDlg = new Form
                {
                    Text = Lang.T("dlg.editTask"),
                    ClientSize = new Size(500, dlgH),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false, MinimizeBox = false,
                    BackColor = this.BackColor
                };

                int row = 12;

                void AddEditRow(string lbl, Control ctrl, int h = 23)
                {
                    editDlg.Controls.Add(new System.Windows.Forms.Label
                    {
                        Text     = lbl,
                        Location = new Point(12, row + 4),
                        Size     = new Size(178, 17),
                        AutoSize = false
                    });
                    ctrl.Location = new Point(196, row); ctrl.Size = new Size(286, h);
                    editDlg.Controls.Add(ctrl);
                    row += h + 10;
                }

                var cboEditAction = new System.Windows.Forms.ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cboEditAction.Items.AddRange(Lang.A("actions"));
                int curIdx = ActionIndexOf(task.Action);
                cboEditAction.SelectedIndex = curIdx >= 0 ? curIdx : 0;
                AddEditRow(Lang.T("dlg.editAction"), cboEditAction);
                var txtNotes = new TextBox { Text = task.Notes };
                AddEditRow(Lang.T("dlg.editNote"), txtNotes);

                TextBox? txtIdle = null;
                DateTimePicker? dtpDate = null;
                TextBox? txtTime2 = null;
                TextBox? txtHour3 = null;
                CheckedListBox? clbDays3 = null;
                System.Windows.Forms.NumericUpDown? nudRepN = null;
                System.Windows.Forms.RadioButton? rbRepMin = null, rbRepHour = null, rbRepDay = null;

                if (task.ActivatorType == 1)
                {
                    txtIdle = new TextBox { Text = string.IsNullOrEmpty(task.IdleTimeout) ? textBox4.Text : task.IdleTimeout };
                        AddEditRow(Lang.T("dlg.editIdle"), txtIdle);
                }
                else if (task.ActivatorType == 2)
                {
                    dtpDate = new DateTimePicker { Value = task.LocalTarget != default ? task.LocalTarget.Date : DateTime.Today, Format = DateTimePickerFormat.Short };
                    AddEditRow(Lang.T("dlg.editDate"), dtpDate);
                    string timeStr = task.LocalTarget != default ? task.LocalTarget.ToString("HH:mm") : "";
                    txtTime2 = new TextBox { Text = timeStr };
                    AddEditRow(Lang.T("dlg.editHour"), txtTime2);
                }
                else if (task.ActivatorType == 3)
                {
                    bool isIntervalEdit = !string.IsNullOrEmpty(task.RepeatIntervalType);
                    if (isIntervalEdit)
                    {

                        int repVal = task.RepeatIntervalValue > 0 ? task.RepeatIntervalValue : 30;
                        nudRepN = new System.Windows.Forms.NumericUpDown
                        { Minimum = 1, Maximum = 99999, Value = repVal };
                        AddEditRow(Lang.T("dlg.editEveryN"), nudRepN);

                        var panelRB = new System.Windows.Forms.Panel { Location = new Point(196, row), Size = new Size(286, 26) };
                        var repUnits = Lang.A("repeatUnits");
                        rbRepMin  = new System.Windows.Forms.RadioButton { Text = repUnits[0], Location = new Point(0,  4), AutoSize = true, Checked = task.RepeatIntervalType == "m" };
                        rbRepHour = new System.Windows.Forms.RadioButton { Text = repUnits[1], Location = new Point(85, 4), AutoSize = true, Checked = task.RepeatIntervalType == "h" };
                        rbRepDay  = new System.Windows.Forms.RadioButton { Text = repUnits[2], Location = new Point(175,4), AutoSize = true, Checked = task.RepeatIntervalType == "d" };
                        if (!rbRepMin.Checked && !rbRepHour.Checked && !rbRepDay.Checked) rbRepMin.Checked = true;
                        panelRB.Controls.Add(rbRepMin); panelRB.Controls.Add(rbRepHour); panelRB.Controls.Add(rbRepDay);
                        editDlg.Controls.Add(panelRB);
                        editDlg.Controls.Add(new System.Windows.Forms.Label { Text = Lang.T("dlg.editUnit"), Location = new Point(12, row + 4), Size = new Size(178, 17), AutoSize = false });
                        row += 36;
                    }
                    else
                    {

                        txtHour3 = new TextBox { Text = string.IsNullOrEmpty(task.ScheduleHour) ? textBox1.Text : task.ScheduleHour };
                        AddEditRow(Lang.T("dlg.editHour"), txtHour3);
                        clbDays3 = new CheckedListBox { CheckOnClick = true };

                        for (int i = 0; i < SCHEDULE_INTERVAL_IDX; i++)
                            clbDays3.Items.Add(checkedListBox1.Items[i]);
                        var savedDays = (string.IsNullOrEmpty(task.ScheduleDays)
                            ? string.Join(",", checkedListBox1.CheckedIndices.Cast<int>())
                            : task.ScheduleDays)
                            .Split(',')
                            .Where(x => int.TryParse(x.Trim(), out _))
                            .Select(x => int.Parse(x.Trim()))
                            .ToHashSet();
                        for (int i = 0; i < clbDays3.Items.Count; i++)
                            clbDays3.SetItemChecked(i, savedDays.Contains(i));
                        int clbH = Math.Min(120, clbDays3.Items.Count * 18 + 4);
                        AddEditRow(Lang.T("dlg.editDays"), clbDays3, clbH);
                    }
                }

                row += 4;
                var btnSave2 = new System.Windows.Forms.Button
                { Text = Lang.T("dlg.save"), Location = new Point(196, row), Size = new Size(96, 28), FlatStyle = FlatStyle.Flat };
                btnSave2.Click += (s2, ev2) =>
                {
                    task.Action = ActionIdFromIndex(cboEditAction.SelectedIndex);
                    task.Notes  = txtNotes.Text.Trim();
                    if (task.ActivatorType == 1 && txtIdle != null)
                    {
                        if (!TryParseTime(txtIdle.Text, out _))
                        { MessageBox.Show(Lang.T("msg.badIdleFormat")); return; }
                        task.IdleTimeout = txtIdle.Text.Trim();
                    }
                    else if (task.ActivatorType == 2 && dtpDate != null && txtTime2 != null)
                    {
                        if (!TryParseTime(txtTime2.Text, out var newTime))
                        { MessageBox.Show(Lang.T("msg.badHourFormat")); return; }
                        DateTime newTarget = dtpDate.Value.Date + newTime;
                        if (newTarget <= DateTime.Now)
                        { MessageBox.Show(Lang.T("msg.futureRequired")); return; }
                        task.LocalTarget = newTarget;
                        task.EndTimeUtc  = newTarget.ToUniversalTime();
                        task.Enabled = true;
                        if (!timer2.Enabled) timer2.Start();
                    }
                    else if (task.ActivatorType == 3)
                    {
                        bool wasInterval = !string.IsNullOrEmpty(task.RepeatIntervalType);
                        if (wasInterval && nudRepN != null && rbRepMin != null)
                        {

                            task.RepeatIntervalType  = rbRepMin.Checked ? "m" : rbRepHour!.Checked ? "h" : "d";
                            task.RepeatIntervalValue = (int)nudRepN.Value;

                        }
                        else if (!wasInterval && txtHour3 != null && clbDays3 != null)
                        {
                            if (!TryParseTime(txtHour3.Text, out _))
                            { MessageBox.Show(Lang.T("msg.badHourFormat")); return; }
                            if (clbDays3.CheckedIndices.Count == 0)
                            { MessageBox.Show(Lang.T("msg.pickOneDay")); return; }
                            task.ScheduleHour = txtHour3.Text.Trim();
                            task.ScheduleDays = string.Join(",", clbDays3.CheckedIndices.Cast<int>());
                            task.LastFired    = default;
                        }
                    }
                    SaveConfigFile();
                    RefreshTaskGrid();
                    editDlg.Close();
                };
                editDlg.Controls.Add(btnSave2);
                var btnCancel3 = new System.Windows.Forms.Button
                { Text = Lang.T("dlg.cancel"), Location = new Point(300, row), Size = new Size(96, 28), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
                editDlg.Controls.Add(btnCancel3);
                editDlg.CancelButton = btnCancel3;
                editDlg.ClientSize = new Size(500, row + 48);

                if (darkDlg) ApplyThemeToControls(editDlg.Controls,
                    Color.FromArgb(30, 30, 30), Color.FromArgb(220, 220, 220), Color.FromArgb(55, 55, 55), true);
                editDlg.ShowDialog(this);
            };

            RefreshTaskGrid();

            checkedListBox1.ItemCheck += (s, ev) =>
            {
                if (ev.Index == SCHEDULE_INTERVAL_IDX && ev.NewValue == CheckState.Checked)
                {

                    var (repType, repValue) = ShowRepeatIntervalDialog();
                    if (string.IsNullOrEmpty(repType))
                    {
                        ev.NewValue = CheckState.Unchecked;
                        return;
                    }
                    appSettings["timer3_repeat_type"]  = repType;
                    appSettings["timer3_repeat_value"] = repValue.ToString();

                    BeginInvoke(new Action(() =>
                    {
                        for (int i = 0; i < SCHEDULE_INTERVAL_IDX; i++)
                            checkedListBox1.SetItemChecked(i, false);
                    }));
                }
                else if (ev.Index != SCHEDULE_INTERVAL_IDX && ev.NewValue == CheckState.Checked)
                {

                    BeginInvoke(new Action(() =>
                        checkedListBox1.SetItemChecked(SCHEDULE_INTERVAL_IDX, false)));
                }
            };

            BuildActivatorTab();
            BuildTaskFilters();
            BuildSettingsExtras(settTip);

            RegTip(settTip, button6,  "tip.passwordLock");
            Reg(label9,  "tasks.header");
            Reg(button12, "tasks.toggle");
            Reg(button11, "tasks.edit");
            Reg(button10, "tasks.delete");
            Reg(label2,  "lbl.action");
            Reg(label5,  "lbl.action");
            Reg(label22, "lbl.action");
            Reg(label3,  "lbl.idleTime");
            Reg(label6,  "lbl.moment");
            Reg(label25, "lbl.moment");
            Reg(label4,  "lbl.formatHMS");
            Reg(label7,  "lbl.formatHM");
            Reg(label24, "lbl.formatHM");
            Reg(label26, "lbl.repeat");
            Reg(button2, "btn.addTask");
            Reg(button4, "btn.addTask");
            Reg(button8, "btn.addTask");
            Reg(label27, "set.groupBehaviour");
            Reg(label28, "set.groupAppearance");
            Reg(label29, "set.groupSecurity");
            Reg(label1,  "set.commandLabel");
            Reg(checkBox1,  "set.autoStartWindows");
            Reg(checkBox2,  "set.autoCountdown");
            Reg(checkBox12, "set.autoQueue");
            Reg(checkBox4,  "set.resetCounter");
            Reg(checkBox8,  "set.timezone");
            Reg(checkBox5,  "set.noSleep");
            Reg(checkBox7,  "set.waitForProcess");
            Reg(checkBox9,  "set.noTray");
            Reg(checkBox10, "set.noCountdown");
            Reg(checkBox3,  "set.minimizeToTray");
            Reg(checkBox11, "set.darkMode");
            Reg(checkBox16, "set.noBalloon");
            Reg(checkBox6,  "set.passwordLock");
            Reg(checkBox15, "set.runAsAdmin");
            Reg(button6,  "set.setPassword");
            Reg(button5,  "set.editConfig");
            Reg(button7,  "set.resetSettings");
            Reg(button13, "set.backup");
            Reg(label20,  "upd.available");
            Reg(label23,  "upd.upToDate");
            if (_lblSnoozeHeader != null) Reg(_lblSnoozeHeader, "snooze.header");
            if (_btnSnooze       != null) Reg(_btnSnooze, "snooze.run");

            Lang.Changed += ApplyLanguage;
            ApplyIcons();
            ApplyLanguage();
            if (checkBox11.Checked) ApplyTheme(true);

            label20.Visible = label20.Enabled = false;
            label23.Visible = label23.Enabled = false;
            CheckForUpdate(announceAtStartup: true);
        }

        private void textBox4_TextChanged(object? sender, EventArgs e)
        {
            appSettings["timer1ActivateBy"] = textBox4.Text;

            TimeSpan maxTime = new TimeSpan(24, 0, 0);
            TimeSpan inputTime;

            if (TimeSpan.TryParse(textBox4.Text, out inputTime) && inputTime > maxTime)
            {
                textBox5.Text = Lang.T("msg.maxOneDay");
                timer1.Stop();
                textBox4.Text = "24:00:00";
            }

            ScheduleConfigSave();
        }

        private void textBox7_TextChanged(object? sender, EventArgs e)
        {
            appSettings["timer2HourActivateBy"] = textBox7.Text;
            ScheduleConfigSave();
        }

        private void dateTimePicker1_ValueChanged(object? sender, EventArgs e)
        {
            appSettings["timer2DateActivateBy"] = dateTimePicker1.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            SaveConfigFile();
        }
        private void checkBox1_CheckedChanged(object? sender, EventArgs e)
        {
            if (_initializing) return;
            appSettings["runWindowsAuto"] = checkBox1.Checked ? "1" : "0";
            SetAutoStart(checkBox1.Checked);
            SaveConfigFile();
        }

        private void checkBox2_CheckedChanged(object? sender, EventArgs e)
        {
            if (_initializing) return;
            appSettings["autoStart"] = checkBox2.Checked ? "1" : "0";
            if (checkBox2.Checked && TryParseTime(textBox4.Text, out var ts2))
            {
                timer1EndTime = DateTime.Now.Add(ts2);
                timer1.Start();
            }
            SaveConfigFile();
        }

        private NotifyIcon notifyIcon1 = new NotifyIcon();

        private void checkBox3_CheckedChanged(object? sender, EventArgs e)
        {
            if (_initializing) return;
            appSettings["noMinimalize"] = checkBox3.Checked ? "1" : "0";
            this.MinimizeBox = !checkBox3.Checked;
            this.FormClosing -= Form1_FormClosing;
            this.Resize -= Form1_Resize;
            notifyIcon1.DoubleClick -= NotifyIcon1_DoubleClick;
            if (checkBox3.Checked)
            {
                this.FormClosing += Form1_FormClosing;
                this.Resize += Form1_Resize;
                notifyIcon1.DoubleClick += NotifyIcon1_DoubleClick;
            }

            SaveConfigFile();
        }

        private void checkBox4_CheckedChanged(object? sender, EventArgs e)
        {
            if (_initializing) return;
            appSettings["RunHost"] = checkBox4.Checked ? "1" : "0";
            if (checkBox4.Checked)
            {
                timer1.Tick -= Timer1_Tick;
                timer1.Tick += Timer1_Tick_Reset;
            }
            else
            {
                timer1.Tick -= Timer1_Tick_Reset;
                timer1.Tick += Timer1_Tick;
            }
            SaveConfigFile();
        }

        private void checkBox5_CheckedChanged(object? sender, EventArgs e)
        {
            if (_initializing) return;

            if (checkBox5.Checked)
            {

                if (string.IsNullOrEmpty(appSettings["powerPlanSetID"]))
                {
                    var currentPowerPlanGuid = GetCurrentPowerPlanGuid();
                    if (!string.IsNullOrEmpty(currentPowerPlanGuid))
                        SavePowerPlanGuidToConfigFile(currentPowerPlanGuid);

                    importedPowerPlanGuid = ImportAndSetActivePowerPlan();
                }
                else
                {
                    importedPowerPlanGuid = appSettings["powerPlanSetID"] ?? "";
                }
                StartPowerPlanMonitor();
            }
            else
            {
                StopPowerPlanMonitor();

                string restore = appSettings["powerPlanID"] ?? "";
                if (!string.IsNullOrEmpty(restore))
                    RunPowerCfg($"/setactive {restore}", captureOutput: false);

                RemovePowerPlan();
                checkBox5.Enabled = false;
                checkBox5.Text = "Uruchom ponownie aby zastosować zmiany";
            }
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {

            try
            {
                if (_configSaveTimer.Enabled) { _configSaveTimer.Stop(); SaveConfigFile(); }
            }
            catch { }

            if (_forceExit ||
                e.CloseReason == CloseReason.WindowsShutDown ||
                e.CloseReason == CloseReason.TaskManagerClosing ||
                e.CloseReason == CloseReason.ApplicationExitCall)
                return;

            if (checkBox3.Checked)
            {
                e.Cancel = true;
                this.Hide();
                notifyIcon1.Visible = true;
            }
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized && checkBox3.Checked)
            {
                this.Hide();
                notifyIcon1.Visible = true;
            }
        }

        private void NotifyIcon1_DoubleClick(object? sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;

            if (appSettings["protect"] == "1")
                SetUiLocked(true);
        }

        private void textBoxUser_TextChanged(object? sender, EventArgs e)
        {
            appSettings["userSet"] = textBoxUser.Text;
            ScheduleConfigSave();
        }

        private void button5_Click(object? sender, EventArgs e)
        {
            if (!File.Exists(configFilePath)) SaveConfigFile();
            TryStartProcess(new ProcessStartInfo
            {
                FileName = configFilePath,
                UseShellExecute = true
            });
        }

        private void label20_Click(object? sender, EventArgs e)
        {
            TryStartProcess(new ProcessStartInfo
            {
                FileName = "https://github.com/Mattronix7200/TimerSysApp/",
                UseShellExecute = true
            });
        }

        private void button7_Click(object? sender, EventArgs e)
        {
            string scriptPath = GetDataFilePath("fixSettings.ps1");
            if (File.Exists(scriptPath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(scriptPath)
                };
                TryStartProcess(psi, waitForExit: true);
            }
            else
            {
                MessageBox.Show(Lang.T("msg.noFixScript"),
                    Lang.T("msg.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void checkBox7_CheckedChanged(object? sender, EventArgs e)
        {
            if (_initializing) return;
            appSettings["userCommandBehaviour"] = checkBox7.Checked ? "1" : "0";
            SaveConfigFile();
        }

        private void checkBox8_CheckedChanged(object? sender, EventArgs e)
        {
            if (_initializing) return;
            appSettings["timezoneCheck"] = checkBox8.Checked ? "1" : "0";
            SaveConfigFile();
            if (checkBox8.Checked)
                SystemEvents.TimeChanged += SystemEvents_TimeChanged;
            else
                SystemEvents.TimeChanged -= SystemEvents_TimeChanged;
        }

        private void SystemEvents_TimeChanged(object? sender, EventArgs e)
        {
            BeginInvoke(new Action(() =>
            {
                if (timer2.Enabled)
                {
                    foreach (var t in _taskList.Where(t => t.ActivatorType == 2 && t.Enabled && t.LocalTarget != default))
                        t.EndTimeUtc = t.LocalTarget.ToUniversalTime();
                    if (_timer2LocalTarget != default)
                        timer2EndTime = _timer2LocalTarget.ToUniversalTime();
                }
                if (timer3.Enabled)
                {
                    foreach (var t in _taskList.Where(t => t.ActivatorType == 3 && t.Enabled && string.IsNullOrEmpty(t.RepeatIntervalType)))
                        t.LastFired = t.LastFired.Date < DateTime.Now.Date ? t.LastFired : DateTime.MinValue;
                }
            }));
        }

        private void checkBox9_CheckedChanged(object? sender, EventArgs e)
        {
            if (_initializing) return;
            appSettings["noTray"] = checkBox9.Checked ? "1" : "0";
            SaveConfigFile();
            notifyIcon1.Visible = !checkBox9.Checked;
        }

        private void checkBox10_CheckedChanged(object? sender, EventArgs e)
        {
            if (_initializing) return;
            appSettings["noTimerShow"] = checkBox10.Checked ? "1" : "0";
            SaveConfigFile();
            bool show = !checkBox10.Checked;
            textBox5.Visible = show;
            textBox9.Visible = show;
        }

        private void checkBox11_CheckedChanged(object? sender, EventArgs e)
        {
            if (_initializing) return;
            appSettings["darkModeEnabled"] = checkBox11.Checked ? "1" : "0";
            SaveConfigFile();
            ApplyTheme(checkBox11.Checked);
        }

        private void ApplyTheme(bool dark)
        {
            Color bg = dark ? Color.FromArgb(30, 30, 30) : SystemColors.ButtonHighlight;
            Color fg = dark ? Color.FromArgb(220, 220, 220) : SystemColors.ControlText;
            Color input = dark ? Color.FromArgb(55, 55, 55) : SystemColors.Window;
            this.BackColor = dark ? Color.FromArgb(25, 25, 25) : SystemColors.Control;
            ApplyThemeToControls(this.Controls, bg, fg, input, dark);

            if (_lblModeIntro != null)
                _lblModeIntro.ForeColor = dark ? Color.FromArgb(165, 165, 170) : Color.FromArgb(90, 90, 95);
            ApplyIcons();
        }

        private void ApplyThemeToControls(Control.ControlCollection controls, Color bg, Color fg, Color input, bool dark)
        {
            foreach (Control ctrl in controls)
            {
                string typeName = ctrl.GetType().Name;
                if (typeName == "TabPage" || typeName == "GroupBox" || typeName == "Panel")
                {
                    ctrl.BackColor = bg;
                    if (typeName == "GroupBox") ctrl.ForeColor = fg;
                }
                else if (typeName == "TextBox" || typeName == "ComboBox" || typeName == "DateTimePicker" || typeName == "CheckedListBox")
                {

                    if (ctrl is TextBox { ReadOnly: true, BorderStyle: BorderStyle.None })
                    {
                        ctrl.BackColor = bg;
                        ctrl.ForeColor = dark ? Color.FromArgb(110, 175, 255) : SystemColors.MenuHighlight;
                    }
                    else
                    {
                        ctrl.BackColor = input;
                        ctrl.ForeColor = fg;
                    }
                }
                else if (typeName == "Label")
                {

                    if (ctrl.BackColor != Color.SlateGray)
                    { ctrl.BackColor = Color.Transparent; ctrl.ForeColor = fg; }
                }

                else if (typeName == "LinkLabel")
                {
                    var link = (LinkLabel)ctrl;
                    link.BackColor = Color.Transparent;
                    link.ForeColor = fg;
                    link.LinkColor       = dark ? Color.FromArgb(110, 180, 255) : Color.FromArgb(0, 102, 204);
                    link.ActiveLinkColor = dark ? Color.FromArgb(150, 205, 255) : Color.FromArgb(0, 80, 160);
                    link.VisitedLinkColor = link.LinkColor;
                }
                else if (typeName == "NumericUpDown" || typeName == "ListBox" || typeName == "RichTextBox")
                {
                    ctrl.BackColor = input;
                    ctrl.ForeColor = fg;
                }
                else if (typeName == "RadioButton")
                {
                    ctrl.BackColor = bg;
                    ctrl.ForeColor = fg;
                }
                else if (typeName == "DataGridView")
                {
                    var dgv = (DataGridView)ctrl;
                    dgv.BackgroundColor = dark ? Color.FromArgb(42, 42, 45) : SystemColors.AppWorkspace;
                    dgv.DefaultCellStyle.BackColor = dark ? Color.FromArgb(42, 42, 45) : SystemColors.Window;
                    dgv.DefaultCellStyle.ForeColor = fg;
                    dgv.DefaultCellStyle.SelectionBackColor = dark ? Color.FromArgb(60, 80, 115) : SystemColors.Highlight;
                    dgv.DefaultCellStyle.SelectionForeColor = dark ? Color.White : SystemColors.HighlightText;
                    dgv.ColumnHeadersDefaultCellStyle.BackColor = dark ? Color.FromArgb(50, 50, 55) : SystemColors.ButtonFace;
                    dgv.ColumnHeadersDefaultCellStyle.ForeColor = fg;
                    dgv.RowHeadersDefaultCellStyle.BackColor = dark ? Color.FromArgb(45, 45, 48) : SystemColors.ButtonFace;
                    dgv.GridColor = dark ? Color.FromArgb(65, 65, 68) : SystemColors.ActiveBorder;
                    dgv.EnableHeadersVisualStyles = false;
                }
                else if (typeName == "CheckBox")
                {
                    var cb = ctrl as System.Windows.Forms.CheckBox;
                    if (cb != null) { cb.BackColor = bg; cb.ForeColor = fg; }
                }
                else if (typeName == "Button")
                {
                    var btn = ctrl as System.Windows.Forms.Button;
                    if (btn != null)
                    {
                        if (dark)
                        {

                            btn.FlatStyle = FlatStyle.Flat;
                            btn.BackColor = Color.FromArgb(64, 64, 70);
                            btn.FlatAppearance.BorderSize          = 1;
                            btn.FlatAppearance.BorderColor         = Color.FromArgb(108, 108, 116);
                            btn.FlatAppearance.MouseOverBackColor  = Color.FromArgb(82, 82, 90);
                            btn.FlatAppearance.MouseDownBackColor  = Color.FromArgb(96, 96, 104);
                        }
                        else { btn.FlatStyle = FlatStyle.Standard; btn.BackColor = SystemColors.ButtonFace; }
                        btn.ForeColor = fg;
                    }
                }
                else if (typeName == "TabControl")
                { ctrl.BackColor = bg; ctrl.ForeColor = fg; }
                if (ctrl.HasChildren && typeName != "DataGridView")
                    ApplyThemeToControls(ctrl.Controls, bg, fg, input, dark);
            }
        }

        private void CleanupResources()
        {
            StopSound();
            try { SystemEvents.TimeChanged -= SystemEvents_TimeChanged; } catch { }
            StopPowerPlanMonitor();

            try
            {
                if (_configSaveTimer.Enabled)
                {
                    _configSaveTimer.Stop();
                    SaveConfigFile();
                }
            }
            catch { }

            foreach (var t in new[] { timer1, timer2, timer3, _timerSnooze, _timerStatusUI, _configSaveTimer })
            {
                try { t.Stop(); t.Dispose(); } catch { }
            }

            foreach (var toast in _activeToasts.ToList())
            {
                try { if (!toast.IsDisposed) toast.Close(); } catch { }
            }
            _activeToasts.Clear();

            try { notifyIcon1.Visible = false; notifyIcon1.Dispose(); } catch { }
            try { _actionTip.Dispose(); } catch { }
            try { _logoScaled?.Dispose(); _logoScaled = null; } catch { }

            foreach (var d in _disposables)
            {
                try { d.Dispose(); } catch { }
            }
            _disposables.Clear();
        }

        private static string GetDataFilePath(string filename) =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", filename);

        private static void EnsureDataFiles()
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            Directory.CreateDirectory(dataDir);
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            string[] files = { "plan.pow", "w7restorePlan.ps1", "fixSettings.ps1" };
            foreach (var fn in files)
            {
                string dest = Path.Combine(dataDir, fn);
                if (File.Exists(dest)) continue;

                string[] candidates = { $"TimerSys.data.{fn}", $"Timer2.data.{fn}", $"Wyłącznik czasowy.data.{fn}" };
                foreach (var resName in candidates)
                {
                    using var stream = asm.GetManifestResourceStream(resName);
                    if (stream == null) continue;
                    using var fs = File.Create(dest);
                    stream.CopyTo(fs);
                    break;
                }
            }

            SoundCatalog.EnsureExtracted();

            string soundsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds");
            Directory.CreateDirectory(soundsDir);
            string soundDest = Path.Combine(soundsDir, "sound.wma");
            if (!File.Exists(soundDest))
            {
                using var sndStream = asm.GetManifestResourceStream("TimerSys.data.sound.wma");
                if (sndStream != null)
                {
                    using var fs = File.Create(soundDest);
                    sndStream.CopyTo(fs);
                }
            }

            string versionFile = Path.Combine(dataDir, "version.txt");
            try
            {
                string current = AppVersion;
                if (!File.Exists(versionFile) || File.ReadAllText(versionFile).Trim() != current)
                    File.WriteAllText(versionFile, current);
            }
            catch { }

            string iconsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons");
            Directory.CreateDirectory(iconsDir);
            var asmIcons = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (string resName in asmIcons.GetManifestResourceNames())
            {
                if (!resName.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)) continue;
                string[] parts  = resName.Split('.');
                string fileName = parts.Length >= 2 ? $"{parts[^2]}.{parts[^1]}" : resName;
                string destPath = Path.Combine(iconsDir, fileName);
                if (!File.Exists(destPath))
                {
                    using var srcIco  = asmIcons.GetManifestResourceStream(resName);
                    if (srcIco == null) continue;
                    using var dstIco = File.Create(destPath);
                    srcIco.CopyTo(dstIco);
                }
            }
        }

        private void StartSchedule()
        {
            if (!TryParseTime(textBox1.Text, out _))
            {
                MessageBox.Show(Lang.T("msg.badHourFormat"), Lang.T("msg.error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (checkedListBox1.CheckedItems.Count == 0)
            {
                MessageBox.Show(Lang.T("msg.pickDay"), Lang.T("msg.error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            appSettings["timer3HourActivateBy"] = textBox1.Text;
            appSettings["timer3CheckedDays"] = string.Join(",", checkedListBox1.CheckedIndices.Cast<int>());
            appSettings["timer3Enabled"] = "1";
            SaveConfigFile();
            timer3.Start();
        }

        private void ToggleScheduleFromTray(object? sender, EventArgs e) =>
            SetHarmonogramRunning(appSettings["timer3UserPaused"] == "1");

        private void ApplyHarmonogramUI(bool running)
        {
            comboBox3.Enabled = running;
            button8.Enabled = running;
            checkedListBox1.Enabled = running;
            textBox1.Enabled = running;
            if (_btnActSettings3 != null)
                _btnActSettings3.Enabled = running && ActionHasParams(comboBox3.SelectedIndex);
            stopStartHarmonogram.Text = Lang.T(running ? "sched.stop" : "sched.start");
            SetIcon(stopStartHarmonogram, running ? Ico.Stop : Ico.Play);
            if (_menuScheduleToggle != null)
            {
                _menuScheduleToggle.Text  = Lang.T(running ? "sched.stop" : "sched.start");
                _menuScheduleToggle.Image = VectorIcons.Get(running ? Ico.Stop : Ico.Play, 16,
                                                            Color.FromArgb(70, 70, 74));
            }
        }

        private void SetHarmonogramRunning(bool running)
        {
            if (running)
            {
                if (_taskList.Any(t => t.ActivatorType == 3 && t.Enabled))
                    timer3.Start();
                appSettings["timer3Enabled"] = timer3.Enabled ? "1" : "0";
                appSettings["timer3UserPaused"] = "0";
            }
            else
            {
                timer3.Stop();
                appSettings["timer3Enabled"] = "0";
                appSettings["timer3UserPaused"] = "1";
            }
            SaveConfigFile();
            ApplyHarmonogramUI(running);
        }

        private void AddTaskAndRun(int activatorType)
        {

            string action = activatorType switch
            {
                1 => ActionIdFromIndex(comboBox1.SelectedIndex),
                2 => ActionIdFromIndex(comboBox2.SelectedIndex),
                3 => ActionIdFromIndex(comboBox3.SelectedIndex),
                _ => ""
            };
            if (string.IsNullOrEmpty(action)) return;

            var task = new TaskEntry { ActivatorType = activatorType, Action = action, Enabled = true };
            _taskList.Add(task);
            SaveConfigFile();
            RefreshTaskGrid();

            switch (activatorType)
            {
                case 1:
                    if (!TryParseTime(textBox4.Text, out var ts1))
                    {
                        MessageBox.Show(Lang.T("msg.badIdleFormat"), Lang.T("msg.error"),
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        _taskList.Remove(task); SaveConfigFile(); RefreshTaskGrid();
                        return;
                    }
                    task.IdleTimeout = textBox4.Text;
                    if (!timer1.Enabled)
                    {
                        timer1EndTime = DateTime.Now.Add(ts1);
                        timer1.Start();
                    }
                    break;

                case 2:
                    if (!TryGetTimerTarget(out DateTime target, out string targetError))
                    {
                        textBox9.Text = targetError;
                        MessageBox.Show(targetError, Lang.T("msg.error"),
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        _taskList.Remove(task); SaveConfigFile(); RefreshTaskGrid();
                        return;
                    }
                    TimeSpan toWait = target - DateTime.Now;

                    task.EndTimeUtc  = DateTime.UtcNow.Add(toWait);
                    task.LocalTarget = target;
                    _timer2LocalTarget = target;
                    timer2EndTime = task.EndTimeUtc;
                    SaveConfigFile();
                    RefreshTaskGrid();
                    timer2.Start();
                    break;

                case 3:
                    task.ScheduleHour = textBox1.Text;
                    task.ScheduleDays = string.Join(",", checkedListBox1.CheckedIndices.Cast<int>());

                    bool isInterval = checkedListBox1.GetItemChecked(SCHEDULE_INTERVAL_IDX);
                    if (isInterval)
                    {

                        string repType  = appSettings["timer3_repeat_type"]  ?? "m";
                        int    repValue = int.TryParse(appSettings["timer3_repeat_value"], out int rv) ? rv : 30;
                        task.RepeatIntervalType  = repType;
                        task.RepeatIntervalValue = repValue;
                        task.LastFired = DateTime.Now;
                        appSettings["timer3Enabled"] = "1";
                        SaveConfigFile();
                        timer3.Start();
                    }
                    else
                    {
                        SaveConfigFile();
                        StartSchedule();
                    }
                    break;
            }

            tabControl1.SelectedTab = tabControl1.TabPages["tabPage5"];
        }

        private void Timer3_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            var current = new TimeSpan(now.Hour, now.Minute, 0);
            bool anyFired = false;

            var schTasks = _taskList.Where(t => t.ActivatorType == 3 && t.Enabled).ToList();
            foreach (var t in schTasks)
            {

                if (!string.IsNullOrEmpty(t.RepeatIntervalType))
                {
                    double intervalMinutes = t.RepeatIntervalType switch
                    {
                        "h" => t.RepeatIntervalValue * 60.0,
                        "d" => t.RepeatIntervalValue * 1440.0,
                        _   => t.RepeatIntervalValue
                    };
                    if ((now - t.LastFired).TotalMinutes >= intervalMinutes)
                    {
                        _currentlyFiringTask = t;
                        ExecuteCommand(t.Action, 3);
                        _currentlyFiringTask = null;
                        t.LastFired = now;
                        anyFired = true;
                    }
                    continue;
                }

                string hourStr = string.IsNullOrEmpty(t.ScheduleHour) ? textBox1.Text : t.ScheduleHour;
                if (!TryParseTime(hourStr, out var targetTime)) continue;

                if (Math.Abs((current - targetTime).TotalMinutes) >= 0.5) continue;
                if (t.LastFired.Date == now.Date) continue;

                string daysStr = string.IsNullOrEmpty(t.ScheduleDays)
                    ? string.Join(",", checkedListBox1.CheckedIndices.Cast<int>())
                    : t.ScheduleDays;
                if (!IsTaskScheduledToday(daysStr, t.LastFired, now)) continue;

                _currentlyFiringTask = t;
                ExecuteCommand(t.Action, 3);
                _currentlyFiringTask = null;
                t.LastFired = now;
                anyFired = true;
            }

            if (anyFired)
            {
                _lastFiredScheduleMinute = now.Minute;
                _lastFiredDate = now.Date;
                appSettings["timer3LastFired"] = now.ToString("yyyy-MM-dd");
                SaveConfigFile();
                RefreshTaskGrid();
            }
        }

        private DateTime? GetNextScheduleFireTime()
        {
            if (!TryParseTime(textBox1.Text, out var targetTime)) return null;
            if (checkedListBox1.CheckedItems.Count == 0) return null;

            for (int daysAhead = 0; daysAhead <= 8; daysAhead++)
            {
                var candidate = DateTime.Today.AddDays(daysAhead).Add(targetTime);
                if (candidate <= DateTime.Now) continue;
                if (IsDateScheduledForDisplay(candidate.Date)) return candidate;
            }
            return null;
        }

        private bool IsDateScheduledForDisplay(DateTime date)
        {
            var dow = date.DayOfWeek;
            DateTime.TryParse(appSettings["timer3LastFired"] ?? "", out DateTime lastFired);

            foreach (int idx in checkedListBox1.CheckedIndices.Cast<int>())
            {
                switch (idx)
                {
                    case 0: return true;
                    case 1: if ((date.Date - lastFired.Date).TotalDays >= 2) return true; break;
                    case 2: if ((date.Date - lastFired.Date).TotalDays >= 3) return true; break;
                    case 3: if ((date.Date - lastFired.Date).TotalDays >= 4) return true; break;
                    case 4: if ((date.Date - lastFired.Date).TotalDays >= 5) return true; break;
                    case 5: if ((date.Date - lastFired.Date).TotalDays >= 6) return true; break;
                    case 6: if ((date.Date - lastFired.Date).TotalDays >= 7) return true; break;
                    case 7: if (dow == DayOfWeek.Friday || dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday) return true; break;
                    case 8: if (dow == DayOfWeek.Monday) return true; break;
                    case 9: if (dow == DayOfWeek.Tuesday) return true; break;
                    case 10: if (dow == DayOfWeek.Wednesday) return true; break;
                    case 11: if (dow == DayOfWeek.Thursday) return true; break;
                    case 12: if (dow == DayOfWeek.Friday) return true; break;
                    case 13: if (dow == DayOfWeek.Saturday) return true; break;
                    case 14: if (dow == DayOfWeek.Sunday) return true; break;
                    case 15: if (dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday) return true; break;
                }
            }
            return false;
        }

        private bool IsTodayScheduled()
        {
            var daysStr = string.Join(",", checkedListBox1.CheckedIndices.Cast<int>());
            DateTime.TryParse(appSettings["timer3LastFired"] ?? "", out DateTime lastFired);
            return IsTaskScheduledToday(daysStr, lastFired, DateTime.Now);
        }

        private static bool IsTaskScheduledToday(string daysStr, DateTime lastFired, DateTime today)
        {
            if (string.IsNullOrEmpty(daysStr)) return false;
            var dow = today.DayOfWeek;
            foreach (var part in daysStr.Split(','))
            {
                if (!int.TryParse(part.Trim(), out int idx)) continue;
                switch (idx)
                {
                    case 0: return true;
                    case 1: if ((today.Date - lastFired.Date).TotalDays >= 2) return true; break;
                    case 2: if ((today.Date - lastFired.Date).TotalDays >= 3) return true; break;
                    case 3: if ((today.Date - lastFired.Date).TotalDays >= 4) return true; break;
                    case 4: if ((today.Date - lastFired.Date).TotalDays >= 5) return true; break;
                    case 5: if ((today.Date - lastFired.Date).TotalDays >= 6) return true; break;
                    case 6: if ((today.Date - lastFired.Date).TotalDays >= 7) return true; break;
                    case 7: if (dow == DayOfWeek.Friday || dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday) return true; break;
                    case 8: if (dow == DayOfWeek.Monday) return true; break;
                    case 9: if (dow == DayOfWeek.Tuesday) return true; break;
                    case 10: if (dow == DayOfWeek.Wednesday) return true; break;
                    case 11: if (dow == DayOfWeek.Thursday) return true; break;
                    case 12: if (dow == DayOfWeek.Friday) return true; break;
                    case 13: if (dow == DayOfWeek.Saturday) return true; break;
                    case 14: if (dow == DayOfWeek.Sunday) return true; break;
                    case 15: if (dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday) return true; break;
                }
            }
            return false;
        }

        private void RefreshTaskGrid()
        {
            if (dataGridView1 == null) return;
            dataGridView1.Rows.Clear();
            var items = _taskListFilter == 0
                ? _taskList
                : _taskList.Where(t => t.ActivatorType == _taskListFilter).ToList();
            foreach (var t in items)
            {
                string actName = Lang.T(t.ActivatorType switch
                {
                    1 => "mode.idle",
                    2 => "mode.timer",
                    3 => "mode.schedule",
                    _ => "?"
                });
                int rowIdx = dataGridView1.Rows.Add(
                    ActionDisplay(t.Action),
                    GetTaskNextActivation(t),
                    t.Enabled ? "✓" : "✗",
                    actName
                );
                dataGridView1.Rows[rowIdx].Tag = t.Id;

                if (!t.Enabled)
                    dataGridView1.Rows[rowIdx].DefaultCellStyle.ForeColor =
                        checkBox11.Checked ? Color.FromArgb(140, 140, 140) : Color.Gray;
            }
        }

        private string GetTaskNextActivation(TaskEntry t)
        {
            if (!t.Enabled) return Lang.T(t.Completed ? "val.done" : "val.paused");
            switch (t.ActivatorType)
            {
                case 2:
                    if (t.EndTimeUtc != default)
                    {
                        var rem = t.EndTimeUtc - DateTime.UtcNow;
                        return rem.TotalSeconds > 0 ? FormatRemaining(rem) : Lang.T("val.expired");
                    }
                    return "-";
                case 3:
                    if (!string.IsNullOrEmpty(t.RepeatIntervalType))
                    {

                        double iMin = t.RepeatIntervalType switch
                        {
                            "h" => t.RepeatIntervalValue * 60.0,
                            "d" => t.RepeatIntervalValue * 1440.0,
                            _   => t.RepeatIntervalValue
                        };
                        var nextFire = t.LastFired.AddMinutes(iMin);
                        var rem = nextFire - DateTime.Now;
                        string unitLabel = Lang.T(t.RepeatIntervalType switch { "h" => "unit.hour", "d" => "unit.day", _ => "unit.min" });
                        string prefix = Lang.T("val.every", t.RepeatIntervalValue, unitLabel) + ": ";
                        return prefix + (rem.TotalSeconds > 0 ? FormatRemaining(rem) : Lang.T("val.soon"));
                    }
                    string hourStr = string.IsNullOrEmpty(t.ScheduleHour) ? textBox1.Text : t.ScheduleHour;
                    if (!TryParseTime(hourStr, out var targetTime)) return "-";
                    string daysStr = string.IsNullOrEmpty(t.ScheduleDays)
                        ? string.Join(",", checkedListBox1.CheckedIndices.Cast<int>())
                        : t.ScheduleDays;
                    for (int d = 0; d <= 8; d++)
                    {
                        var candidate = DateTime.Today.AddDays(d).Add(targetTime);
                        if (candidate <= DateTime.Now) continue;
                        if (IsTaskScheduledToday(daysStr, t.LastFired, candidate.Date.Add(TimeSpan.Zero)))
                            return candidate.ToString("dd.MM HH:mm");
                    }
                    return "-";
                case 1:
                {
                    var threshold = IdleThresholdFor(t);
                    if (!timer1.Enabled || threshold == null) return Lang.T("val.waiting");
                    var left = threshold.Value - TimeSpan.FromMilliseconds(GetIdleMilliseconds());
                    return left.TotalSeconds > 0 ? FormatRemaining(left) : Lang.T("val.soon");
                }
                default:
                    return "-";
            }
        }

    }

    public class TaskEntry
    {
        public string   Id            { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public int      ActivatorType { get; set; }
        public string   Action        { get; set; } = "";
        public bool     Enabled       { get; set; } = true;

        public bool     Completed     { get; set; }
        public string   Notes         { get; set; } = "";

        public DateTime EndTimeUtc    { get; set; }
        public DateTime LocalTarget   { get; set; }

        public string   ScheduleHour  { get; set; } = "";
        public string   ScheduleDays  { get; set; } = "";
        public DateTime LastFired     { get; set; }

        public string   IdleTimeout   { get; set; } = "";

        public string   RepeatIntervalType  { get; set; } = "";
        public int      RepeatIntervalValue { get; set; }
    }
}
