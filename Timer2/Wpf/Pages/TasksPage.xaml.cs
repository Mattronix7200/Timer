using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using TimerSys.Core;

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
    public partial class TasksPage : UserControl, IRefreshablePage
    {
        private readonly AppEngine _engine;
        private readonly List<ToggleButton> _filters = new();
        private int _filter;
        private readonly System.Windows.Threading.DispatcherTimer _refresh;

        private readonly List<(Border Row, TaskItem Task, TextBlock Remaining, TextBlock State)> _rows = new();

        public TasksPage(AppEngine engine)
        {
            _engine = engine;
            InitializeComponent();

            BuildFilters();
            RefreshTexts();

            _engine.TasksChanged += () => Dispatcher.Invoke(Rebuild);

            _refresh = new System.Windows.Threading.DispatcherTimer
            { Interval = TimeSpan.FromSeconds(1) };
            _refresh.Tick += (s, e) => { if (IsVisible) UpdateRowTexts(); };
            _refresh.Start();
        }

        private void BuildFilters()
        {
            FilterBar.Children.Clear();
            _filters.Clear();

            var defs = new (int Value, string Key, Ico Icon)[]
            {
                (0, "tasks.all", Ico.TaskList),
                (AppEngine.ModeIdle, "mode.idle", Ico.Idle),
                (AppEngine.ModeTimer, "mode.timer", Ico.Stopwatch),
                (AppEngine.ModeSchedule, "mode.schedule", Ico.Calendar),
            };

            foreach (var d in defs)
            {
                var button = new ToggleButton
                {
                    Style = (Style)FindResource("SegmentToggle"),
                    Margin = new Thickness(3),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Tag = d.Value,
                    IsChecked = d.Value == _filter
                };
                button.Content = MainWindow.BuildIconLabel(d.Icon, Lang.T(d.Key), 16,
                    d.Value == _filter ? Brushes.White : ThemeManager.Brush("TextMutedBrush"));

                button.Click += (s, e) => SetFilter(d.Value);
                FilterBar.Children.Add(button);
                _filters.Add(button);
            }
        }

        private void SetFilter(int value)
        {
            _filter = value;
            foreach (var b in _filters)
            {
                bool active = (int)(b.Tag ?? 0) == value;
                b.IsChecked = active;

                var brush = active ? Brushes.White : ThemeManager.Brush("TextMutedBrush");
                if (b.Content is StackPanel sp)
                {
                    foreach (var child in sp.Children)
                    {
                        if (child is IconView icon) icon.Brush = brush;
                        else if (child is TextBlock label) label.Foreground = brush;
                    }
                }
            }
            Rebuild();
        }

        private void Rebuild()
        {
            RowsPanel.Children.Clear();
            _rows.Clear();

            var tasks = _engine.Tasks
                .Where(t => _filter == 0 || t.ActivatorType == _filter)
                .ToList();

            foreach (var task in tasks)
                RowsPanel.Children.Add(BuildRow(task));

            LblEmpty.Visibility = tasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            LblCount.Text = Lang.T("tasks.count", _engine.Tasks.Count);
            UpdateRowTexts();
        }

        private Border BuildRow(TaskItem task)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(174) });

            var name = new Grid();
            name.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            name.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            name.Children.Add(new IconView
            {
                Kind = task.OneOff ? Ico.Calendar : task.ActivatorType switch
                {
                    AppEngine.ModeTimer => Ico.Stopwatch,
                    AppEngine.ModeSchedule => Ico.Calendar,
                    _ => Ico.Idle
                },
                Width = 17,
                Height = 17,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Brush = ThemeManager.Brush(task.Enabled ? "AccentBrush" : "TextMutedBrush")
            });
            var nameText = new TextBlock
            {
                Text = task.OneOff
                    ? Lang.T("tasks.snoozeName", AppEngine.ActionDisplay(task.Action))
                    : AppEngine.ActionDisplay(task.Action),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap,
                Margin = new Thickness(0, 0, 12, 0),
                Opacity = task.Enabled ? 1 : 0.55
            };
            nameText.ToolTip = nameText.Text;
            Grid.SetColumn(nameText, 1);
            name.Children.Add(nameText);
            Grid.SetColumn(name, 0);

            var remaining = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetColumn(remaining, 1);

            var state = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(state, 2);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            buttons.Children.Add(RowButton(Ico.Refresh, Lang.T("tasks.rerun"), () => Restart(task)));
            buttons.Children.Add(RowButton(task.Enabled ? Ico.Pause : Ico.Play, Lang.T("tasks.toggle"),
                () => { _engine.ToggleTask(task.Id); Rebuild(); }));
            buttons.Children.Add(RowButton(Ico.Pencil, Lang.T("tasks.edit"), () => EditTask(task)));
            buttons.Children.Add(RowButton(Ico.Trash, Lang.T("tasks.delete"), () =>
            {
                if (MessageDialog.Confirm(Window.GetWindow(this),
                        Lang.T("tasks.deleteAsk", AppEngine.ActionDisplay(task.Action)), Lang.T("msg.title")))
                    _engine.RemoveTask(task.Id);
            }));
            Grid.SetColumn(buttons, 4);

            var queue = new TextBlock
            {
                Text = _engine.ModeName(task.OneOff ? AppEngine.ModeSchedule : task.ActivatorType),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = ThemeManager.Brush("TextMutedBrush")
            };
            Grid.SetColumn(queue, 3);

            grid.Children.Add(name);
            grid.Children.Add(remaining);
            grid.Children.Add(state);
            grid.Children.Add(queue);
            grid.Children.Add(buttons);

            var row = new Border
            {
                Style = (Style)FindResource("Card"),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(14, 10, 10, 10),
                Child = grid
            };
            _rows.Add((row, task, remaining, state));
            return row;
        }

        private Button RowButton(Ico icon, string tip, Action onClick)
        {
            var button = new Button
            {
                Width = 36,
                Height = 32,
                Margin = new Thickness(4, 0, 0, 0),
                ToolTip = tip,
                Content = new IconView
                {
                    Kind = icon,
                    Width = 15,
                    Height = 15,
                    Brush = ThemeManager.Brush("TextBrush")
                }
            };
            button.SetResourceReference(StyleProperty, "SquareIconButton");
            System.Windows.Automation.AutomationProperties.SetName(button, tip);
            button.Click += (s, e) => onClick();
            return button;
        }

        private void UpdateRowTexts()
        {
            foreach (var r in _rows)
            {
                r.Remaining.Text = _engine.TaskStatusText(r.Task);
                r.Remaining.Foreground = ThemeManager.Brush(r.Task.Enabled ? "TextBrush" : "TextMutedBrush");
                r.State.Text = !r.Task.Enabled
                    ? Lang.T(r.Task.Completed ? "val.done" : "val.paused")
                    : Lang.T("val.active");
                r.State.Foreground = ThemeManager.Brush(r.Task.Enabled ? "SuccessBrush" : "TextMutedBrush");
            }
        }

        private void Restart(TaskItem task)
        {
            if (_engine.RestartTask(task.Id))
            {
                Rebuild();
                return;
            }
            MessageDialog.Show(Window.GetWindow(this), Lang.T("tasks.rerunUnknown"),
                Lang.T("msg.title"), MessageKind.Info);
        }

        private void EditTask(TaskItem task)
        {
            if (Window.GetWindow(this) is MainWindow owner) owner.EditTask(task);
        }

        public void RefreshTexts()
        {
            LblHeader.Text = Lang.T("tasks.header");
            ColTask.Text = Lang.T("col.task");
            ColRemaining.Text = Lang.T("col.remaining");
            ColState.Text = Lang.T("col.state");
            ColQueue.Text = Lang.T("col.queue");
            LblEmpty.Text = Lang.T("tasks.empty");
            BuildFilters();
            Rebuild();
        }

        public void RefreshTheme()
        {
            BuildFilters();
            Rebuild();
        }
    }
}
