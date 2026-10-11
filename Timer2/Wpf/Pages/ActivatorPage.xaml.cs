using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using TimerSys.Core;
using TimerSys.Wpf.Dialogs;

using UserControl = System.Windows.Controls.UserControl;
using Button = System.Windows.Controls.Button;
using ListBox = System.Windows.Controls.ListBox;
using ListBoxItem = System.Windows.Controls.ListBoxItem;
using TextBox = System.Windows.Controls.TextBox;
using ComboBox = System.Windows.Controls.ComboBox;
using CheckBox = System.Windows.Controls.CheckBox;
using RadioButton = System.Windows.Controls.RadioButton;
using Label = System.Windows.Controls.Label;
using Image = System.Windows.Controls.Image;
using Orientation = System.Windows.Controls.Orientation;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using FontFamily = System.Windows.Media.FontFamily;
using FontStyle = System.Windows.FontStyle;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using Rect = System.Windows.Rect;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Clipboard = System.Windows.Clipboard;
using Cursors = System.Windows.Input.Cursors;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
namespace TimerSys.Wpf.Pages
{
    public partial class ActivatorPage : UserControl, IRefreshablePage
    {
        private readonly AppEngine _engine;
        private readonly MainWindow _owner;
        private readonly List<ToggleButton> _modeButtons = new();
        private readonly List<CheckBox> _dayBoxes = new();
        private int _mode;
        private bool _durationMode = true;
        private bool _loading = true;
        private string? _editingId;

        public ActivatorPage(AppEngine engine, MainWindow owner)
        {
            _engine = engine;
            _owner = owner;
            InitializeComponent();

            BuildModeBar();
            BuildDays();

            CboAction.SelectionChanged += (s, e) =>
            {
                if (_loading) return;
                _engine.Set($"defaultAction{_mode}", CboAction.SelectedIndex.ToString());
                UpdateTip();
            };
            TxtIdleTime.TextChanged  += (s, e) => StoreTime("timer1ActivateBy", TxtIdleTime.Text);
            TxtDuration.TextChanged  += (s, e) => { if (!_loading) _engine.Set("timer2Duration", TxtDuration.Text); };
            TxtHour.TextChanged      += (s, e) => { if (!_loading) _engine.Set("timer2HourActivateBy", TxtHour.Text); };
            TxtSchedHour.TextChanged += (s, e) => StoreTime("timer3HourActivateBy", TxtSchedHour.Text);
            DatePick.SelectedDateChanged += (s, e) =>
            {
                if (_loading || DatePick.SelectedDate == null) return;
                _engine.Set("timer2DateActivateBy", DatePick.SelectedDate.Value.ToString("dd.MM.yyyy"));
            };

            _engine.IdleStatusChanged     += t => Dispatcher.Invoke(() =>
            {
                if (_editingId == null && _mode == AppEngine.ModeIdle) LblCountdown.Text = t;
            });
            _engine.TimerStatusChanged    += t => Dispatcher.Invoke(() =>
            {
                if (_editingId == null && _mode == AppEngine.ModeTimer) LblCountdown.Text = t;
            });
            _engine.ScheduleStatusChanged += t => Dispatcher.Invoke(() => LblSchedStatus.Text = t);
            _engine.SnoozeStatusChanged   += t => Dispatcher.Invoke(() =>
            {
                LblSnoozeStatus.Text = t;
                BtnSnooze.IsEnabled = t.Length == 0;
            });

            LoadValues();
            SetMode(int.TryParse(_engine.Get("activatorMode"), out int m) && m is >= 1 and <= 3 ? m : 1, save: false);
            RefreshTexts();
            _loading = false;
        }

        private void StoreTime(string key, string text)
        {
            if (_loading) return;
            if (!AppEngine.TryParseTime(text, out var value) || value <= TimeSpan.Zero) return;
            _engine.Set(key, text.Trim());
        }

        private void BuildModeBar()
        {
            ModeBar.Children.Clear();
            _modeButtons.Clear();
            var defs = new (int Mode, string Key, Ico Icon)[]
            {
                (AppEngine.ModeIdle,     "mode.idle",     Ico.Idle),
                (AppEngine.ModeTimer,    "mode.timer",    Ico.Stopwatch),
                (AppEngine.ModeSchedule, "mode.schedule", Ico.Calendar),
            };
            foreach (var d in defs)
            {
                var button = new ToggleButton
                {
                    Style = (Style)FindResource("SegmentToggle"),
                    Margin = new Thickness(3),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Tag = d.Mode,
                    Content = BuildModeContent(d.Icon, Lang.T(d.Key), false)
                };
                button.Click += (s, e) => SetMode(d.Mode);
                ModeBar.Children.Add(button);
                _modeButtons.Add(button);
            }
        }

        private static StackPanel BuildModeContent(Ico icon, string text, bool active)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            panel.Children.Add(new IconView
            {
                Kind = icon, Width = 17, Height = 17,
                Margin = new Thickness(0, 0, 8, 0),
                Brush = active ? Brushes.White : ThemeManager.Brush("TextMutedBrush")
            });
            panel.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
            return panel;
        }

        private void BuildDays()
        {
            DaysPanel.Children.Clear();
            _dayBoxes.Clear();
            var labels = Lang.A("scheduleDays");
            var selected = _engine.Get("timer3CheckedDays")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => int.TryParse(p.Trim(), out int i) ? i : -1).ToHashSet();

            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                var cb = new CheckBox { Content = labels[i], IsChecked = selected.Contains(i), Margin = new Thickness(0, 3, 0, 3) };
                cb.Checked += (s, e) => OnDayChecked(index);
                cb.Unchecked += (s, e) => StoreDays();
                DaysPanel.Children.Add(cb);
                _dayBoxes.Add(cb);
            }
        }

        private void OnDayChecked(int index)
        {
            if (_loading) return;

            if (index == AppEngine.ScheduleIntervalIndex)
            {
                var dialog = new RepeatIntervalDialog(_engine) { Owner = _owner };
                if (dialog.ShowDialog() != true)
                {
                    _dayBoxes[index].IsChecked = false;
                    return;
                }
                for (int i = 0; i < AppEngine.ScheduleIntervalIndex; i++) _dayBoxes[i].IsChecked = false;
            }
            else
            {
                _dayBoxes[AppEngine.ScheduleIntervalIndex].IsChecked = false;
            }
            StoreDays();
        }

        private void StoreDays()
        {
            if (_loading) return;
            var indexes = _dayBoxes.Select((cb, i) => (cb, i)).Where(x => x.cb.IsChecked == true).Select(x => x.i);
            _engine.Set("timer3CheckedDays", string.Join(",", indexes));
        }

        public void BeginEdit(TaskItem task)
        {
            _loading = true;
            _editingId = task.Id;
            _engine.EditingTaskId = task.Id;

            switch (task.ActivatorType)
            {
                case AppEngine.ModeIdle:
                    TxtIdleTime.Text = task.IdleTimeout;
                    break;
                case AppEngine.ModeTimer:
                    if (task.Duration > TimeSpan.Zero)
                    {
                        _durationMode = true;
                        TxtDuration.Text = task.Duration.ToString(@"hh\:mm\:ss");
                    }
                    else
                    {
                        _durationMode = false;
                        DatePick.SelectedDate = task.LocalTarget.Date;
                        TxtHour.Text = task.LocalTarget.ToString("HH:mm");
                    }
                    break;
                case AppEngine.ModeSchedule:
                    TxtSchedHour.Text = task.ScheduleHour;
                    var days = task.ScheduleDays
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => int.TryParse(x.Trim(), out int i) ? i : -1)
                        .ToHashSet();
                    for (int i = 0; i < _dayBoxes.Count; i++) _dayBoxes[i].IsChecked = days.Contains(i);
                    break;
            }

            _loading = false;
            SetMode(task.ActivatorType, save: false);

            _loading = true;
            CboAction.SelectedIndex = AppEngine.ActionIndexOf(task.Action);
            _loading = false;

            UpdateEditState();
        }

        private void CancelEdit()
        {
            _editingId = null;
            _engine.EditingTaskId = null;
            UpdateEditState();
        }

        private void UpdateEditState()
        {
            bool editing = _editingId != null;
            if (editing) LblCountdown.Text = Lang.T("tasks.editPaused");
            EditBanner.Visibility = editing ? Visibility.Visible : Visibility.Collapsed;
            BtnCancelEdit.Visibility = editing ? Visibility.Visible : Visibility.Collapsed;
            LblEditBanner.Text = Lang.T("tasks.editing");
            BtnCancelEdit.Content = MainWindow.BuildIconLabel(Ico.Cross, Lang.T("tasks.editCancel"), 14);
            BtnAdd.Content = MainWindow.BuildIconLabel(
                editing ? Ico.Save : Ico.Plus,
                Lang.T(editing ? "tasks.editSave" : "btn.addTask"), 16, Brushes.White);
        }

        private void BtnCancelEdit_Click(object sender, RoutedEventArgs e) => CancelEdit();

        private void LoadValues()
        {
            _loading = true;
            CboAction.Items.Clear();
            foreach (var name in Lang.A("actions")) CboAction.Items.Add(name);

            TxtIdleTime.Text  = _engine.Get("timer1ActivateBy");
            TxtDuration.Text  = _engine.Get("timer2Duration");
            TxtHour.Text      = _engine.Get("timer2HourActivateBy");
            TxtSchedHour.Text = _engine.Get("timer3HourActivateBy");
            DatePick.SelectedDate = DateTime.TryParseExact(_engine.Get("timer2DateActivateBy"), "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d) ? d : DateTime.Today;

            CboSnooze.Items.Clear();
            foreach (var s in Lang.A("snoozeItems")) CboSnooze.Items.Add(s);
            CboSnooze.SelectedIndex = 0;

            _durationMode = _engine.Get("timer2InputMode") != "datetime";
            _loading = false;
        }

        public void SetMode(int mode, bool save = true)
        {
            _mode = mode;
            foreach (var b in _modeButtons)
            {
                bool active = (int)(b.Tag ?? 0) == mode;
                b.IsChecked = active;
                var defs = new Dictionary<int, (Ico, string)>
                {
                    [AppEngine.ModeIdle] = (Ico.Idle, "mode.idle"),
                    [AppEngine.ModeTimer] = (Ico.Stopwatch, "mode.timer"),
                    [AppEngine.ModeSchedule] = (Ico.Calendar, "mode.schedule"),
                };
                var def = defs[(int)(b.Tag ?? 1)];
                b.Content = BuildModeContent(def.Item1, Lang.T(def.Item2), active);
            }

            PanelIdle.Visibility     = mode == AppEngine.ModeIdle ? Visibility.Visible : Visibility.Collapsed;
            PanelTimer.Visibility    = mode == AppEngine.ModeTimer ? Visibility.Visible : Visibility.Collapsed;
            PanelSchedule.Visibility = mode == AppEngine.ModeSchedule ? Visibility.Visible : Visibility.Collapsed;
            SchedExtras.Visibility   = mode == AppEngine.ModeSchedule ? Visibility.Visible : Visibility.Collapsed;

            LblIntro.Text = Lang.T(mode switch
            {
                AppEngine.ModeTimer => "mode.intro.timer",
                AppEngine.ModeSchedule => "mode.intro.schedule",
                _ => "mode.intro.idle"
            });

            bool loading = _loading;
            _loading = true;
            CboAction.SelectedIndex = int.TryParse(_engine.Get($"defaultAction{mode}"), out int a)
                && a >= 0 && a < CboAction.Items.Count ? a : 0;
            _loading = loading;

            SetDurationMode(_durationMode, save: false);
            UpdateTip();
            UpdateScheduleButton();
            LblCountdown.Text = "";

            if (save) _engine.Set("activatorMode", mode.ToString());
        }

        private void SetDurationMode(bool duration, bool save = true)
        {
            _durationMode = duration;
            PanelDuration.Visibility = duration ? Visibility.Visible : Visibility.Collapsed;
            PanelDateTime.Visibility = duration ? Visibility.Collapsed : Visibility.Visible;
            LblTimerCaption.Text = Lang.T(duration ? "timer.duration" : "lbl.moment");
            BtnTimerMode.Content = MainWindow.BuildIconLabel(
                duration ? Ico.Calendar : Ico.Stopwatch,
                Lang.T(duration ? "timer.toDateTime" : "timer.toDuration"), 15);
            if (save) _engine.Set("timer2InputMode", duration ? "duration" : "datetime");
        }

        private void UpdateTip()
        {
            bool show = !_engine.Flag("hideTips");
            TipCard.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

            int idx = CboAction.SelectedIndex;
            var descriptions = Lang.A("actionDesc");
            LblTipTitle.Text = Lang.T("tip.header");
            LblTipText.Text = idx >= 0 && idx < descriptions.Length ? descriptions[idx] : Lang.T("tip.pickAction");

            BtnActionSettings.IsEnabled = AppEngine.ActionHasSettings(idx);
            BtnActionSettings.Content = new IconView
            {
                Kind = Ico.Gear, Width = 16, Height = 16,
                Brush = ThemeManager.Brush("TextBrush")
            };
            BtnActionSettings.ToolTip = Lang.T("tip.actionSettings");
        }

        private void UpdateScheduleButton()
        {
            bool running = _engine.ScheduleRunning;
            BtnSchedToggle.Content = MainWindow.BuildIconLabel(
                running ? Ico.Stop : Ico.Play, Lang.T(running ? "sched.stop" : "sched.start"), 15);
            LblSchedStatus.Text = _engine.ScheduleStatusText();
        }

        private void BtnActionSettings_Click(object sender, RoutedEventArgs e)
        {
            int idx = CboAction.SelectedIndex;
            if (idx < 0) return;

            Window dialog = idx switch
            {
                0  => new SoundDialog(_engine, _mode),
                9  => new CleanupDialog(_engine),
                13 => new ScreenshotDialog(_engine),
                14 => new EmailActionDialog(_engine, _mode),
                _  => new ActionParamDialog(_engine, _mode, idx)
            };
            dialog.Owner = _owner;
            dialog.ShowDialog();
            UpdateTip();
        }

        private void BtnTimerMode_Click(object sender, RoutedEventArgs e) => SetDurationMode(!_durationMode);

        private void BtnSchedToggle_Click(object sender, RoutedEventArgs e)
        {
            _engine.SetScheduleRunning(!_engine.ScheduleRunning);
            UpdateScheduleButton();
        }

        private void BtnSnooze_Click(object sender, RoutedEventArgs e)
        {
            int[] minutes = { 5, 10, 30, 60, 180, 300, 600 };
            int idx = CboSnooze.SelectedIndex;
            if (idx < 0 || idx >= minutes.Length) return;
            if (CboAction.SelectedIndex < 0)
            {
                MessageDialog.Show(_owner, Lang.T("snooze.pickAction"), Lang.T("msg.title"));
                return;
            }
            _engine.StartSnooze(minutes[idx]);
            BtnSnooze.IsEnabled = false;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string action = AppEngine.ActionIdFromIndex(CboAction.SelectedIndex);
            if (action.Length == 0) return;

            var task = new TaskItem { ActivatorType = _mode, Action = action, Enabled = true };

            switch (_mode)
            {
                case AppEngine.ModeIdle:
                    if (!AppEngine.TryParseTime(TxtIdleTime.Text, out _))
                    {
                        MessageDialog.Show(_owner, Lang.T("msg.badIdleFormat"), Lang.T("msg.error"), MessageKind.Error);
                        return;
                    }
                    task.IdleTimeout = TxtIdleTime.Text.Trim();
                    break;

                case AppEngine.ModeTimer:
                {
                    if (!TryGetTarget(out DateTime target, out TimeSpan duration, out string error))
                    {
                        MessageDialog.Show(_owner, error, Lang.T("msg.error"), MessageKind.Error);
                        return;
                    }
                    task.LocalTarget = target;
                    task.EndTimeUtc = target.ToUniversalTime();

                    task.Duration = duration;
                    break;
                }

                case AppEngine.ModeSchedule:
                {
                    if (!AppEngine.TryParseTime(TxtSchedHour.Text, out _))
                    {
                        MessageDialog.Show(_owner, Lang.T("msg.badHourFormat"), Lang.T("msg.error"), MessageKind.Error);
                        return;
                    }
                    bool interval = _dayBoxes[AppEngine.ScheduleIntervalIndex].IsChecked == true;
                    if (!interval && !_dayBoxes.Take(AppEngine.ScheduleIntervalIndex).Any(c => c.IsChecked == true))
                    {
                        MessageDialog.Show(_owner, Lang.T("msg.pickDay"), Lang.T("msg.error"), MessageKind.Error);
                        return;
                    }
                    task.ScheduleHour = TxtSchedHour.Text.Trim();
                    task.ScheduleDays = string.Join(",", _dayBoxes
                        .Select((cb, i) => (cb, i)).Where(x => x.cb.IsChecked == true).Select(x => x.i));
                    if (interval)
                    {
                        task.RepeatIntervalType = _engine.Get("timer3_repeat_type");
                        task.RepeatIntervalValue = int.TryParse(_engine.Get("timer3_repeat_value"), out int rv) ? rv : 30;
                        task.LastFired = DateTime.Now;
                    }
                    break;
                }
            }

            if (_editingId != null)
            {
                string previous = _editingId;
                CancelEdit();
                _engine.RemoveTask(previous);
            }

            _engine.AddTask(task);
            UpdateScheduleButton();
            _owner.Navigate("tasks");
        }

        private bool TryGetTarget(out DateTime target, out TimeSpan duration, out string error)
        {
            target = default;
            duration = TimeSpan.Zero;
            error = "";
            if (_durationMode)
            {
                if (!AppEngine.TryParseTime(TxtDuration.Text, out var dur) ||
                    dur <= TimeSpan.Zero || dur >= TimeSpan.FromDays(1))
                {
                    error = Lang.T("msg.badDuration");
                    return false;
                }
                duration = dur;
                target = DateTime.Now.Add(dur);
                return true;
            }

            if (!AppEngine.TryParseTime(TxtHour.Text, out var hour))
            {
                error = Lang.T("msg.badHourFormat");
                return false;
            }
            target = (DatePick.SelectedDate ?? DateTime.Today).Date + hour;
            if (target <= DateTime.Now)
            {
                error = Lang.T("msg.pastDate");
                return false;
            }

            duration = TimeSpan.FromSeconds(Math.Ceiling((target - DateTime.Now).TotalSeconds));
            return true;
        }

        public void RefreshTexts()
        {
            _loading = true;
            LblMode.Text         = Lang.T("mode.header");
            LblAction.Text       = Lang.T("lbl.action");
            LblIdleTime.Text     = Lang.T("lbl.idleTime");
            LblIdleHint.Text     = Lang.T("lbl.formatHMS");
            LblDurationHint.Text = Lang.T("timer.maxDuration");
            LblHourHint.Text     = Lang.T("lbl.formatHM");
            LblSchedHour.Text    = Lang.T("lbl.moment");
            LblSchedHourHint.Text = Lang.T("lbl.formatHM");
            LblRepeat.Text       = Lang.T("lbl.repeat");
            LblSnooze.Text       = Lang.T("snooze.header");
            BtnSnooze.Content    = MainWindow.BuildIconLabel(Ico.Play, Lang.T("snooze.run"), 15);

            int action = CboAction.SelectedIndex;
            CboAction.Items.Clear();
            foreach (var name in Lang.A("actions")) CboAction.Items.Add(name);
            CboAction.SelectedIndex = action >= 0 && action < CboAction.Items.Count ? action : 0;

            int snooze = CboSnooze.SelectedIndex;
            CboSnooze.Items.Clear();
            foreach (var s in Lang.A("snoozeItems")) CboSnooze.Items.Add(s);
            CboSnooze.SelectedIndex = snooze >= 0 && snooze < CboSnooze.Items.Count ? snooze : 0;

            BuildModeBar();
            BuildDays();
            _loading = false;
            SetMode(_mode, save: false);
            UpdateEditState();
        }

        public void RefreshTheme()
        {
            SetMode(_mode, save: false);
            UpdateScheduleButton();
        }
    }
}
