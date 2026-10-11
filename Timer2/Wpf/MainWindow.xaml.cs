using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TimerSys.Core;
using TimerSys.Wpf.Pages;

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

namespace TimerSys.Wpf
{
    public partial class MainWindow : Window
    {
        private const double NavWidthWide = 212;
        private const double NavWidthNarrow = 64;

        private readonly AppEngine _engine;
        private readonly Dictionary<string, UserControl> _pages = new();
        private readonly List<ToggleButton> _navButtons = new();
        private string _currentPage = "activator";
        private bool _navCollapsed;

        public AppEngine Engine => _engine;

        public MainWindow(AppEngine engine)
        {
            _engine = engine;
            InitializeComponent();

            _navCollapsed = _engine.Flag("navCollapsed");

            BuildNavigation();
            ApplyBranding();
            ApplyTexts();
            UpdateChromeIcons();
            ApplyNavWidth();

            _engine.ErrorReported += OnEngineError;
            Lang.Changed += () => { ApplyTexts(); ApplyBranding(); RefreshPages(); };

            Navigate("activator");
            SetLocked(_engine.Flag("protect"));
            WindowStyling.UseCorners(this, _engine.Flag("roundedCorners"));
        }

        private readonly (string Key, string LangKey, Ico Icon)[] _navItems =
        {
            ("activator", "tab.activator", Ico.Stopwatch),
            ("tasks",     "tab.tasks",     Ico.TaskList),
            ("settings",  "tab.settings",  Ico.Settings),
            ("about",     "tab.info",      Ico.Info),
        };

        private void BuildNavigation()
        {
            NavPanel.Children.Clear();
            _navButtons.Clear();

            foreach (var item in _navItems)
            {
                var button = new ToggleButton
                {
                    Style = (Style)FindResource("SegmentToggle"),
                    Margin = new Thickness(0, 0, 0, 4),
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Tag = item.Key,
                    ToolTip = Lang.T(item.LangKey)
                };
                button.Content = BuildNavContent(item.Icon, Lang.T(item.LangKey), false);
                button.Checked += (s, e) => Navigate(item.Key);
                button.Click += (s, e) => { if (button.IsChecked != true) button.IsChecked = true; };

                NavPanel.Children.Add(button);
                _navButtons.Add(button);
            }
        }

        private StackPanel BuildNavContent(Ico icon, string text, bool active)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = _navCollapsed ? HorizontalAlignment.Center : HorizontalAlignment.Left
            };
            panel.Children.Add(new IconView
            {
                Kind = icon,
                Width = 18,
                Height = 18,
                Margin = new Thickness(0, 0, _navCollapsed ? 0 : 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Brush = active ? Brushes.White : ThemeManager.Brush("TextMutedBrush")
            });
            if (!_navCollapsed)
                panel.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
            return panel;
        }

        public void Navigate(string key)
        {
            _currentPage = key;
            foreach (var b in _navButtons)
                b.IsChecked = (string?)b.Tag == key;

            if (!_pages.TryGetValue(key, out var page))
            {
                page = key switch
                {
                    "tasks"    => new TasksPage(_engine),
                    "settings" => new SettingsPage(_engine, this),
                    "about"    => new AboutPage(_engine, this),
                    _          => new ActivatorPage(_engine, this)
                };
                _pages[key] = page;
            }
            PageHost.Content = page;
            UpdateNavContent();
        }

        private void UpdateNavContent()
        {
            for (int i = 0; i < _navButtons.Count && i < _navItems.Length; i++)
            {
                bool active = _navButtons[i].IsChecked == true;
                _navButtons[i].Content = BuildNavContent(_navItems[i].Icon, Lang.T(_navItems[i].LangKey), active);
                _navButtons[i].ToolTip = Lang.T(_navItems[i].LangKey);
                _navButtons[i].Padding = _navCollapsed ? new Thickness(6, 8, 6, 8) : new Thickness(14, 8, 14, 8);
            }
        }

        private void BtnNavToggle_Click(object sender, RoutedEventArgs e)
        {
            _navCollapsed = !_navCollapsed;
            _engine.SetFlag("navCollapsed", _navCollapsed);
            ApplyNavWidth();
        }

        private void ApplyNavWidth()
        {
            NavColumn.Width = new GridLength(_navCollapsed ? NavWidthNarrow : NavWidthWide);
            LblStatus.Visibility = _navCollapsed ? Visibility.Collapsed : Visibility.Visible;
            ToolRow.Columns = _navCollapsed ? 1 : 2;
            BtnTheme.Margin = _navCollapsed ? new Thickness(0, 0, 0, 6) : new Thickness(0, 0, 4, 0);
            BtnLanguage.Margin = _navCollapsed ? new Thickness(0) : new Thickness(4, 0, 0, 0);

            BtnNavToggle.Content = new IconView
            {
                Kind = Ico.Menu,
                Width = 16,
                Height = 16,
                Brush = ThemeManager.Brush("TextMutedBrush")
            };
            BtnNavToggle.ToolTip = Lang.T(_navCollapsed ? "nav.expand" : "nav.collapse");

            UpdateNavContent();
        }

        public void EditTask(TaskItem task)
        {
            Navigate("activator");
            if (_pages.TryGetValue("activator", out var page) && page is ActivatorPage activator)
                activator.BeginEdit(task);
        }

        public void RefreshOtherPages(UserControl? except)
        {
            foreach (var page in _pages.Values)
                if (!ReferenceEquals(page, except))
                    (page as IRefreshablePage)?.RefreshTexts();
        }

        private void RefreshPages()
        {
            foreach (var page in _pages.Values)
                (page as IRefreshablePage)?.RefreshTexts();
            BuildNavigation();
            ApplyNavWidth();
            Navigate(_currentPage);
        }

        public void ApplyTheme(bool dark)
        {
            ThemeManager.Apply(dark, _engine.Get("accentColor"), _engine.Flag("roundedCorners"));
            WindowStyling.UseCorners(this, ThemeManager.Rounded);
            UpdateChromeIcons();
            ApplyNavWidth();
            foreach (var page in _pages.Values)
                (page as IRefreshablePage)?.RefreshTheme();
        }

        private void UpdateChromeIcons()
        {
            SetChromeIcon(BtnHelp, Ico.Help);
            SetChromeIcon(BtnMinimize, Ico.Minimize);
            SetChromeIcon(BtnClose, Ico.Close);

            BtnTheme.Content = new IconView
            {
                Kind = ThemeManager.IsDark ? Ico.Sun : Ico.Moon,
                Width = 17,
                Height = 17,
                Brush = ThemeManager.Brush("TextBrush")
            };
            BtnTheme.ToolTip = Lang.T(ThemeManager.IsDark ? "theme.toLight" : "theme.toDark");

            BtnLanguage.Content = new IconView
            {
                Kind = Lang.Current == Lang.EN ? Ico.FlagEn : Ico.FlagPl,
                Width = 22,
                Height = 22
            };
            BtnLanguage.ToolTip = Lang.T("lang.switch");
        }

        private void SetChromeIcon(Button button, Ico kind)
        {
            if (button.ToolTip is string tip)
                System.Windows.Automation.AutomationProperties.SetName(button, tip);
            button.Content = new IconView
            {
                Kind = kind,
                Width = 14,
                Height = 14,
                Brush = ThemeManager.Brush("TextMutedBrush")
            };
        }

        public static StackPanel BuildIconLabel(Ico kind, string text, double size = 16, Brush? brush = null)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new IconView
            {
                Kind = kind,
                Width = size,
                Height = size,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Brush = brush
            });
            var label = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center };
            if (brush != null) label.Foreground = brush;
            panel.Children.Add(label);
            return panel;
        }

        private void ApplyBranding()
        {
            try
            {

                var icon = new BitmapImage(new Uri("pack://application:,,,/clock.ico", UriKind.Absolute));
                AppIcon.Source = icon;
                Icon = icon;
            }
            catch { }

            try
            {
                string lockFile = Lang.Current == Lang.EN ? "block_en.png" : "block.jpg";
                var bitmap = new BitmapImage(
                    new Uri($"pack://application:,,,/Resources/{lockFile}", UriKind.Absolute));
                LockImage.Source = bitmap;

                LockOverlay.Background = new SolidColorBrush(EdgeColorOf(bitmap));
            }
            catch { }
        }

        private static Color EdgeColorOf(BitmapSource source)
        {
            try
            {
                var one = new CroppedBitmap(source, new Int32Rect(0, 0, 1, 1));
                var converted = new FormatConvertedBitmap(one, PixelFormats.Bgra32, null, 0);
                var pixel = new byte[4];
                converted.CopyPixels(pixel, 4, 0);
                return Color.FromRgb(pixel[2], pixel[1], pixel[0]);
            }
            catch
            {
                return Color.FromRgb(0x3A, 0x3A, 0x3A);
            }
        }

        private void ApplyTexts()
        {
            Title = Lang.T("msg.title");
            LblTitle.Text = Lang.T("msg.title");
            BtnHelp.ToolTip = Lang.T("help.link");
            BtnMinimize.ToolTip = Lang.T("win.minimize");
            BtnClose.ToolTip = Lang.T("win.close");
            LblLockHint.Text = Lang.T("pwd.unlockHint");
            UpdateNavContent();
        }

        public void SetStatus(string text) => LblStatus.Text = text;

        public void SetLocked(bool locked)
        {
            LockOverlay.Visibility = locked ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LockOverlay_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new Dialogs.PasswordDialog { Owner = this };
            if (dialog.ShowDialog() != true) return;

            LockOverlay.Visibility = Visibility.Collapsed;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) ToggleMaximize();
            else if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void ToggleMaximize() =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_engine.Flag("noMinimalize"))
            {

                if (_engine.Flag("protect")) SetLocked(true);
                Hide();
                return;
            }
            Application.Current.Shutdown();
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e) => OpenHelp(this);

        public static void OpenHelp(Window? owner)
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041) && Dialogs.ManualWindow.CanShow)
            {
                var manual = new Dialogs.ManualWindow();
                if (owner != null && owner.IsVisible) manual.Owner = owner;
                manual.ShowDialog();
                return;
            }

            if (HelpFile.Open()) return;
            MessageDialog.Show(owner, Lang.T("help.missing"), Lang.T("msg.error"), MessageKind.Warning);
        }

        private void BtnTheme_Click(object sender, RoutedEventArgs e)
        {
            bool dark = !ThemeManager.IsDark;
            _engine.SetFlag("darkModeEnabled", dark);
            ApplyTheme(dark);
        }

        private void BtnLanguage_Click(object sender, RoutedEventArgs e)
        {
            string code = Lang.Current == Lang.EN ? Lang.PL : Lang.EN;
            _engine.Set("language", code, immediate: true);
            Lang.Set(code);
            UpdateChromeIcons();
        }

        private void OnEngineError(string message) => Dispatcher.Invoke(() =>
            MessageDialog.Show(this, message, Lang.T("msg.error"), MessageKind.Error));
    }

    public interface IRefreshablePage
    {
        void RefreshTexts();
        void RefreshTheme();
    }
}
