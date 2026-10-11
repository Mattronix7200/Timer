using System.Collections.Generic;
using System.IO;
namespace Timer2
{
    internal static class Program
    {

        private static System.Threading.Mutex? _appMutex;
        private static bool _mutexOwned;

        [STAThread]
        static void Main(string[] args)
        {

            _appMutex = new System.Threading.Mutex(true, "TimerSys_SingleInstance_v1", out bool createdNew);
            _mutexOwned = createdNew;
            if (!createdNew)
            {
                LoadLanguage();
                MessageBox.Show(TimerSys.Lang.T("msg.alreadyRunning"), TimerSys.Lang.T("msg.title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                _appMutex.Dispose();
                return;
            }

            try
            {

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => HandleCrash(e.Exception, fatal: false);
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                    HandleCrash(e.ExceptionObject as Exception, fatal: true);
                System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
                {
                    LogError(e.Exception);
                    e.SetObserved();
                };

                string adminFlagFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "runasadmin.flag");
                if (File.Exists(adminFlagFile) && !IsAdmin())
                {
                    try
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName        = Application.ExecutablePath,
                            UseShellExecute = true,
                            Verb            = "runas"
                        };

                        foreach (var a in args) psi.ArgumentList.Add(a);

                        ReleaseMutex();
                        System.Diagnostics.Process.Start(psi);
                        return;
                    }
                    catch
                    {

                        try
                        {
                            _appMutex = new System.Threading.Mutex(true, "TimerSys_SingleInstance_v1", out bool again);
                            _mutexOwned = again;
                        }
                        catch { }
                    }
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                bool useWinForms = false;
                foreach (var a in args)
                    if (string.Equals(a, "--winforms", StringComparison.OrdinalIgnoreCase)) useWinForms = true;

                if (useWinForms)
                {
                    var form1 = new Form1();
                    if (args.Length > 0 && args[0] == "--minimized")
                    {
                        form1.WindowState = FormWindowState.Minimized;
                        form1.ShowInTaskbar = false;
                        form1.StartTimer1();
                    }
                    Application.Run(form1);
                }
                else
                {
                    var app = new TimerSys.Wpf.App(args);
                    app.DispatcherUnhandledException += (s, e) =>
                    {
                        HandleCrash(e.Exception, fatal: false);
                        e.Handled = true;
                    };
                    app.Run();
                }
            }
            finally
            {
                ReleaseMutex();
            }
        }

        private static void LoadLanguage()
        {
            try
            {
                string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "config.json");
                string? code = null;
                if (File.Exists(file))
                {
                    var saved = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        File.ReadAllText(file));
                    if (saved != null && saved.TryGetValue("language", out var value) && value.Length > 0)
                        code = value;
                }
                TimerSys.Lang.SetQuiet(code ?? TimerSys.Lang.SystemDefault);
            }
            catch
            {
                TimerSys.Lang.SetQuiet(TimerSys.Lang.SystemDefault);
            }
        }

        private static void ReleaseMutex()
        {
            try
            {
                if (_appMutex != null && _mutexOwned)
                {
                    _appMutex.ReleaseMutex();
                    _mutexOwned = false;
                }
                _appMutex?.Dispose();
                _appMutex = null;
            }
            catch { }
        }

        private static void HandleCrash(Exception? ex, bool fatal)
        {
            LogError(ex);
            try
            {
                string msg = "Wystąpił nieoczekiwany błąd:\n\n" + (ex?.Message ?? "nieznany błąd") +
                             "\n\nSzczegóły zapisano w pliku data\\error.log.";
                if (!fatal) msg += "\n\nProgram spróbuje kontynuować pracę.";
                MessageBox.Show(msg, "Wyłącznik czasowy – błąd",
                    MessageBoxButtons.OK, fatal ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
            }
            catch { }
        }

        private static void LogError(Exception? ex)
        {
            if (ex == null) return;
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "error.log");

                try
                {
                    var fi = new FileInfo(file);
                    if (fi.Exists && fi.Length > 512 * 1024)
                        File.Move(file, file + ".old", true);
                }
                catch { }

                File.AppendAllText(file,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\r\n{ex.StackTrace}\r\n\r\n");
            }
            catch { }
        }

        private static bool IsAdmin()
        {
            using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
            return new System.Security.Principal.WindowsPrincipal(id)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }
}
