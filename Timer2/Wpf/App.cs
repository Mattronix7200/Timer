using System;
using System.Linq;
using System.Windows;
using TimerSys.Core;
using TimerSys.Wpf.Dialogs;

using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace TimerSys.Wpf
{

    internal sealed class App : Application
    {

        public static App? Instance { get; private set; }

        private readonly string[] _args;
        private AppEngine? _engine;
        private MainWindow? _window;
        private TrayIcon? _tray;

        public App(string[] args)
        {
            Instance = this;
            _args = args;
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/Wpf/Theme.xaml", UriKind.Relative)
            });
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _engine = new AppEngine();
            _engine.Initialize();

            ThemeManager.Apply(_engine.Flag("darkModeEnabled"),
                _engine.Get("accentColor"), _engine.Flag("roundedCorners"));

            bool firstRun = !PasswordStore.Exists();

            _window = new MainWindow(_engine);
            _tray = new TrayIcon(_engine, _window);

            _engine.ActionPerformed += OnActionPerformed;
            _window.Closed += (s, ev) => Shutdown();

            bool minimized = _args.Any(a =>
                string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));

            _window.Show();

            if (minimized)
            {
                _window.ShowInTaskbar = false;
                if (_engine.Flag("noTray")) _window.WindowState = WindowState.Minimized;
                else _window.Hide();
                _engine.StartIdleCountdown();
            }

            if (firstRun)
            {
                _engine.SetFlag("runWindowsAuto", true);
                _engine.SetFlag("noMinimalize", true);
                try { _engine.SetAutoStart(true); } catch { }

                if (_window.IsVisible)
                {
                    var welcome = new WelcomeDialog();
                    welcome.Owner = _window;
                    welcome.ShowDialog();
                }
            }

            if (_engine.Get("checkUpdates") != "0") CheckForUpdate();
        }

        public void RefreshTray() => _tray?.Refresh();

        private void OnActionPerformed(ActionNotification notification) => Dispatcher.Invoke(() =>
        {
            if (_engine == null || _engine.Flag("noBalloon")) return;
            ToastWindow.Show(AppEngine.ActionDisplay(notification.ActionId), notification.Body);
        });

        private async void CheckForUpdate()
        {
            if (_engine == null) return;

            string? latest = await UpdateChecker.LatestVersionAsync();
            if (!UpdateChecker.IsNewer(latest)) return;
            if (string.Equals(_engine.Get("skipVersion"), latest, StringComparison.Ordinal)) return;
            if (_window == null || !_window.IsVisible) return;

            var dialog = new UpdateDialog(latest!, AppEngine.AppVersion) { Owner = _window };
            dialog.ShowDialog();
            if (dialog.SkipVersion) _engine.Set("skipVersion", latest!, immediate: true);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _engine?.FlushPendingSave();
                _engine?.Dispose();
                _tray?.Dispose();
            }
            catch { }
            base.OnExit(e);
        }
    }
}
