using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using TimerSys.Core;

using WinForms = System.Windows.Forms;
using Application = System.Windows.Application;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;
using Brushes = System.Drawing.Brushes;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;
using Rectangle = System.Drawing.Rectangle;

namespace TimerSys.Wpf
{

    internal sealed class TrayIcon : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr handle);

        private readonly AppEngine _engine;
        private readonly MainWindow _window;
        private readonly WinForms.NotifyIcon _icon;
        private readonly Icon _base;
        private readonly System.Windows.Threading.DispatcherTimer _refresh;

        private Icon? _badge;
        private string _badgeKey = "-";
        private WinForms.ContextMenuStrip? _menu;
        private readonly System.Collections.Generic.List<(WinForms.ToolStripMenuItem Item, TaskItem Task)> _menuTasks = new();

        public TrayIcon(AppEngine engine, MainWindow window)
        {
            _engine = engine;
            _window = window;
            _base = LoadIcon();

            _icon = new WinForms.NotifyIcon
            {
                Icon = _base,
                Text = Lang.T("msg.title"),
                Visible = !engine.Flag("noTray")
            };
            _icon.DoubleClick += (s, e) => ShowWindow();
            _icon.MouseUp += (s, e) =>
            {
                if (e.Button == WinForms.MouseButtons.Right) BuildMenu();
            };

            Lang.Changed += () => { _badgeKey = "-"; UpdateBadge(); };
            _engine.TasksChanged += () => _window.Dispatcher.Invoke(UpdateBadge);

            _refresh = new System.Windows.Threading.DispatcherTimer
            { Interval = TimeSpan.FromSeconds(1) };
            _refresh.Tick += (s, e) => { UpdateBadge(); RefreshMenuTimes(); };
            _refresh.Start();

            UpdateBadge();
        }

        private static Icon LoadIcon()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clock.ico");
                if (File.Exists(path)) return new Icon(path, 32, 32);

                string? exe = Environment.ProcessPath;
                if (exe != null)
                {
                    var extracted = Icon.ExtractAssociatedIcon(exe);
                    if (extracted != null) return extracted;
                }
            }
            catch { }
            return SystemIcons.Application;
        }

        private int ActiveCount => _engine.Tasks.Count(t => t.Enabled && !t.Completed);

        private int PausedCount => _engine.Tasks.Count(t => !t.Enabled && !t.Completed);

        private void UpdateBadge()
        {
            int active = ActiveCount;
            int paused = PausedCount;
            int count = active > 0 ? active : paused;
            bool green = active > 0;

            string key = count == 0 ? "" : count + (green ? "z" : "c");
            if (key == _badgeKey) return;
            _badgeKey = key;

            var previous = _badge;
            _badge = count == 0 ? null : DrawBadge(count, green);
            _icon.Icon = _badge ?? _base;
            _icon.Text = BuildTooltip(active, paused);

            if (previous != null)
            {
                IntPtr handle = previous.Handle;
                previous.Dispose();
                DestroyIcon(handle);
            }
        }

        private static string BuildTooltip(int active, int paused)
        {
            string text = Lang.T("msg.title");
            if (active > 0) text += Environment.NewLine + Lang.T("tray.activeCount", active);
            if (paused > 0) text += Environment.NewLine + Lang.T("tray.pausedCount", paused);
            return text.Length > 127 ? text[..127] : text;
        }

        private Icon DrawBadge(int count, bool green)
        {
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.DrawIcon(_base, new Rectangle(0, 0, 32, 32));

                var area = new Rectangle(9, 9, 23, 23);
                using var fill = new SolidBrush(green
                    ? Color.FromArgb(32, 160, 72)
                    : Color.FromArgb(205, 48, 48));
                using var edge = new Pen(Color.White, 2.5f);
                g.FillEllipse(fill, area);
                g.DrawEllipse(edge, area);

                string text = count > 9 ? "9+" : count.ToString();
                using var font = new Font("Segoe UI", count > 9 ? 12f : 15f, FontStyle.Bold, GraphicsUnit.Pixel);
                var size = g.MeasureString(text, font);
                g.DrawString(text, font, Brushes.White,
                    area.Left + (area.Width - size.Width) / 2,
                    area.Top + (area.Height - size.Height) / 2);
            }

            IntPtr handle = bitmap.GetHicon();
            var icon = (Icon)Icon.FromHandle(handle).Clone();
            DestroyIcon(handle);
            bitmap.Dispose();
            return icon;
        }

        private void BuildMenu()
        {
            var menu = new WinForms.ContextMenuStrip { ShowImageMargin = false };
            _menuTasks.Clear();
            _menu = menu;
            menu.Closed += (s, e) => { _menu = null; _menuTasks.Clear(); };

            void Add(string text, Action onClick, bool enabled = true)
            {
                var item = new WinForms.ToolStripMenuItem(text) { Enabled = enabled };
                item.Click += (s, e) => onClick();
                menu.Items.Add(item);
            }

            Add(Lang.T("tray.show"), ShowWindow);
            menu.Items.Add(new WinForms.ToolStripSeparator());

            var pending = _engine.Tasks
                .Where(t => !t.Completed)
                .OrderBy(t => t.Enabled ? 0 : 1)
                .Take(8)
                .ToList();

            if (pending.Count == 0)
            {
                Add(Lang.T("tray.noTasks"), () => { }, enabled: false);
            }
            else
            {
                foreach (var task in pending)
                {
                    string name = task.OneOff
                        ? Lang.T("tasks.snoozeName", AppEngine.ActionDisplay(task.Action))
                        : AppEngine.ActionDisplay(task.Action);
                    string state = task.Enabled
                        ? _engine.TaskStatusText(task)
                        : Lang.T("val.paused") + " " + _engine.TaskStatusText(task);
                    var captured = task;
                    Add(MenuLine(captured), () => { ShowWindow(); _window.Navigate("tasks"); });
                    _menuTasks.Add(((WinForms.ToolStripMenuItem)menu.Items[menu.Items.Count - 1], captured));
                }
            }

            bool hasIdle = _engine.Tasks.Any(t => t.ActivatorType == AppEngine.ModeIdle && t.Enabled && !t.Completed);
            bool hasTimer = _engine.Tasks.Any(t => t.ActivatorType == AppEngine.ModeTimer && t.Enabled && !t.Completed);
            bool hasSchedule = _engine.Tasks.Any(t => t.ActivatorType == AppEngine.ModeSchedule && t.Enabled && !t.Completed);

            menu.Items.Add(new WinForms.ToolStripSeparator());

            Add(Lang.T(_engine.IdleRunning ? "tray.stopIdle" : "tray.resumeIdle"), () =>
            {
                if (_engine.IdleRunning) _engine.StopIdleCountdown();
                else _engine.StartIdleCountdown();
            }, enabled: hasIdle);

            Add(Lang.T(_engine.TimerRunning ? "tray.stopTimer" : "tray.resumeTimer"), () =>
            {
                if (_engine.TimerRunning) _engine.StopTimerCountdown();
            }, enabled: hasTimer && _engine.TimerRunning);

            Add(Lang.T(_engine.ScheduleRunning ? "tray.stopSched" : "tray.resumeSched"),
                () => _engine.SetScheduleRunning(!_engine.ScheduleRunning), enabled: hasSchedule);

            menu.Items.Add(new WinForms.ToolStripSeparator());
            Add(Lang.T("tray.settings"), () => { ShowWindow(); _window.Navigate("settings"); });
            Add(Lang.T("tray.exit"), () => Application.Current.Shutdown());

            _icon.ContextMenuStrip = menu;
            menu.Show(WinForms.Control.MousePosition);
        }

        private string MenuLine(TaskItem task)
        {
            string name = task.OneOff
                ? Lang.T("tasks.snoozeName", AppEngine.ActionDisplay(task.Action))
                : AppEngine.ActionDisplay(task.Action);
            string state = task.Enabled
                ? _engine.TaskStatusText(task)
                : Lang.T("val.paused") + " " + _engine.TaskStatusText(task);
            return name + "   " + state;
        }

        private void RefreshMenuTimes()
        {
            if (_menu == null || !_menu.Visible) return;
            foreach (var entry in _menuTasks)
            {
                string line = MenuLine(entry.Task);
                if (entry.Item.Text != line) entry.Item.Text = line;
            }
        }

        private void ShowWindow()
        {
            _window.Show();
            if (_window.WindowState == WindowState.Minimized)
                _window.WindowState = WindowState.Normal;
            _window.Activate();
        }

        public void Refresh()
        {
            _icon.Visible = !_engine.Flag("noTray");
            _badgeKey = "-";
            UpdateBadge();
        }

        public void Dispose()
        {
            _refresh.Stop();
            _icon.Visible = false;
            _icon.Dispose();
            _badge?.Dispose();
        }
    }
}
