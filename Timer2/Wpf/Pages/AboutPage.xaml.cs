using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
    public partial class AboutPage : UserControl, IRefreshablePage
    {
        private const string WebsiteUrl = "https://jmsoft.pl";
        private const string SupportUrl = "https://suppi.pl/jmsoft";

        private readonly AppEngine _engine;
        private readonly MainWindow _owner;
        private readonly List<ToggleButton> _tabs = new();
        private int _tab;

        public AboutPage(AppEngine engine, MainWindow owner)
        {
            _engine = engine;
            _owner = owner;
            InitializeComponent();
            RefreshTexts();
        }

        private void BuildTabs()
        {
            TabBar.Children.Clear();
            _tabs.Clear();

            var defs = new (int Value, string Key, Ico Icon)[]
            {
                (0, "info.tabChangelog", Ico.Note),
                (1, "info.tabLicense", Ico.Shield),
            };

            foreach (var d in defs)
            {
                var button = new ToggleButton
                {
                    Style = (Style)FindResource("SegmentToggle"),
                    Margin = new Thickness(3),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Tag = d.Value,
                    IsChecked = d.Value == _tab
                };
                button.Content = MainWindow.BuildIconLabel(d.Icon, Lang.T(d.Key), 16,
                    d.Value == _tab ? Brushes.White : ThemeManager.Brush("TextMutedBrush"));
                button.Click += (s, e) => SetTab(d.Value);
                TabBar.Children.Add(button);
                _tabs.Add(button);
            }
        }

        private void SetTab(int value)
        {
            _tab = value;
            foreach (var b in _tabs)
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
            LblContent.Text = value == 1 ? HelpContent.License() : HelpContent.Changelog();
        }

        private void BuildButtons()
        {
            ButtonRow.Children.Clear();
            Add("help.link", Ico.Help, () => MainWindow.OpenHelp(_owner));
            Add("info.website", Ico.Globe, () => SmtpSetupDialog.OpenUrl(WebsiteUrl));
            Add("info.checkUpdates", Ico.Download, CheckUpdates);

            void Add(string key, Ico icon, Action onClick)
            {
                var button = new Button
                {
                    Content = MainWindow.BuildIconLabel(icon, Lang.T(key), 15, ThemeManager.Brush("TextBrush")),
                    Margin = new Thickness(0, 0, 10, 0),
                    MinWidth = 190,
                    MinHeight = 38
                };
                button.SetResourceReference(StyleProperty, "FlatButton");
                button.Click += (s, e) => onClick();
                ButtonRow.Children.Add(button);
            }
        }

        private async void CheckUpdates()
        {
            string? latest = await UpdateChecker.LatestVersionAsync();
            if (!UpdateChecker.IsNewer(latest))
            {
                MessageDialog.Show(_owner, Lang.T("upd.upToDate"), Lang.T("msg.title"), MessageKind.Success);
                return;
            }

            var dialog = new UpdateDialog(latest!, AppEngine.AppVersion) { Owner = _owner };
            dialog.ShowDialog();
        }

        private void BtnSupport_Click(object sender, RoutedEventArgs e) =>
            SmtpSetupDialog.OpenUrl(SupportUrl);

        private void ApplyBranding()
        {
            try
            {
                string file = Lang.Current == Lang.EN ? "logo_en.png" : "logo.png";
                LogoImage.Source = new BitmapImage(
                    new Uri($"pack://application:,,,/Resources/{file}", UriKind.Absolute));
            }
            catch { }
        }

        public void RefreshTexts()
        {
            LblVersion.Text = Lang.T("info.version", AppEngine.AppVersion);
            LblAuthor.Text = Lang.T("info.by");
            LblSupport.Text = Lang.T("info.support");
            BtnSupport.Content = MainWindow.BuildIconLabel(
                Ico.Sparkle, Lang.T("info.supportButton"), 15, Brushes.White);
            ApplyBranding();
            BuildTabs();
            BuildButtons();
            SetTab(_tab);
        }

        public void RefreshTheme()
        {
            BuildTabs();
            BuildButtons();
            SetTab(_tab);
        }
    }
}
