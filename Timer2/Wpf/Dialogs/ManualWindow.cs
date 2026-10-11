using System;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;

using Button = System.Windows.Controls.Button;
using Image = System.Windows.Controls.Image;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;
using Point = System.Windows.Point;
using Orientation = System.Windows.Controls.Orientation;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace TimerSys.Wpf.Dialogs
{
    [SupportedOSPlatform("windows10.0.19041.0")]
    internal sealed class ManualWindow : Window
    {
        private PdfBook? _book;
        private readonly Image _page = new()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 16, 0, 16)
        };
        private readonly ScrollViewer _scroll = new()
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        private readonly TextBlock _status = new()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 460,
            TextAlignment = TextAlignment.Center
        };
        private readonly TextBlock _counter = new()
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 14, 0),
            FontWeight = FontWeights.SemiBold
        };

        private Button _previous = null!;
        private Button _next = null!;
        private int _index;
        private double _zoom = 1.0;
        private bool _busy;

        public ManualWindow()
        {
            Title = Lang.T("help.link") + " – " + Lang.T("msg.title");
            Width = 1000;
            Height = 780;
            MinWidth = 620;
            MinHeight = 460;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;
            UseLayoutRounding = true;
            WindowStyle = WindowStyle.None;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            SetResourceReference(BackgroundProperty, "WindowBrush");

            System.Windows.Shell.WindowChrome.SetWindowChrome(this, new System.Windows.Shell.WindowChrome
            {
                CaptionHeight = 0,
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = new Thickness(6)
            });

            var frame = new Border { BorderThickness = new Thickness(1) };
            frame.SetResourceReference(Border.BorderBrushProperty, "BorderStrongBrush");
            frame.SetResourceReference(Border.BackgroundProperty, "WindowBrush");
            frame.SetResourceReference(TextElement.ForegroundProperty, "TextBrush");

            var layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            layout.Children.Add(BuildTitleBar());
            layout.Children.Add(BuildToolBar());

            var host = new Grid();
            _status.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
            _status.Text = Lang.T("help.loading");
            _scroll.Content = _page;
            _scroll.Visibility = Visibility.Collapsed;
            host.Children.Add(_status);
            host.Children.Add(_scroll);
            Grid.SetRow(host, 2);
            layout.Children.Add(host);

            frame.Child = layout;
            Content = frame;

            PreviewKeyDown += OnKey;
            PreviewMouseWheel += OnWheel;
            SizeChanged += async (s, e) => { if ((_book?.IsOpen ?? false) && e.WidthChanged) await ShowPageAsync(); };
            Loaded += async (s, e) => await LoadAsync();
            Closed += (s, e) => _book?.Dispose();

            WindowStyling.UseSquareCorners(this);
            OwnerDim.Attach(this);
        }

        private Border BuildTitleBar()
        {
            var bar = new Border { BorderThickness = new Thickness(0, 0, 0, 1) };
            bar.SetResourceReference(Border.BackgroundProperty, "ChromeBrush");
            bar.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
            bar.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2) ToggleMaximize();
                else if (e.ButtonState == MouseButtonState.Pressed) DragMove();
            };

            var grid = new Grid();
            var caption = new TextBlock
            {
                Text = Title,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(18, 0, 0, 0),
                FontWeight = FontWeights.SemiBold
            };
            caption.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            grid.Children.Add(caption);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            buttons.Children.Add(ChromeButton(Ico.FolderOpen, Lang.T("help.openOutside"), () => HelpFile.Open()));
            buttons.Children.Add(ChromeButton(Ico.Maximize, Lang.T("win.maximize"), ToggleMaximize));
            buttons.Children.Add(ChromeButton(Ico.Close, Lang.T("win.close"), Close));
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(buttons, true);
            grid.Children.Add(buttons);

            bar.Child = grid;
            Grid.SetRow(bar, 0);
            return bar;
        }

        private Border BuildToolBar()
        {
            var bar = new Border { BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(12, 0, 12, 0) };
            bar.SetResourceReference(Border.BackgroundProperty, "NavBrush");
            bar.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _previous = ToolButton(Ico.ChevronDown, Lang.T("help.previous"), 90, () => Turn(-1));
            _next = ToolButton(Ico.ChevronDown, Lang.T("help.next"), -90, () => Turn(1));

            _counter.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

            row.Children.Add(_previous);
            row.Children.Add(_counter);
            row.Children.Add(_next);
            row.Children.Add(new Border { Width = 1, Margin = new Thickness(16, 10, 16, 10), Background = ThemeManager.Brush("BorderBrush") });
            row.Children.Add(ToolButton(Ico.Minimize, Lang.T("help.zoomOut"), 0, () => Zoom(-0.2)));
            row.Children.Add(ToolButton(Ico.Plus, Lang.T("help.zoomIn"), 0, () => Zoom(0.2)));
            row.Children.Add(ToolButton(Ico.Monitor, Lang.T("help.fitWidth"), 0, () => Zoom(0)));

            bar.Child = row;
            Grid.SetRow(bar, 1);
            return bar;
        }

        private Button ChromeButton(Ico icon, string tip, Action onClick)
        {
            var button = new Button
            {
                Width = 46,
                Height = 42,
                ToolTip = tip,
                Content = new IconView
                {
                    Kind = icon,
                    Width = 14,
                    Height = 14,
                    Brush = ThemeManager.Brush("TextMutedBrush")
                }
            };
            button.SetResourceReference(StyleProperty, "IconButton");
            System.Windows.Automation.AutomationProperties.SetName(button, tip);
            button.Click += (s, e) => onClick();
            return button;
        }

        private Button ToolButton(Ico icon, string tip, double rotation, Action onClick)
        {
            var glyph = new IconView
            {
                Kind = icon,
                Width = 14,
                Height = 14,
                Brush = ThemeManager.Brush("TextBrush")
            };
            if (rotation != 0)
            {
                glyph.RenderTransformOrigin = new Point(0.5, 0.5);
                glyph.RenderTransform = new RotateTransform(rotation);
            }

            var button = new Button
            {
                Width = 38,
                Height = 32,
                Margin = new Thickness(3, 0, 3, 0),
                ToolTip = tip,
                Content = glyph
            };
            button.SetResourceReference(StyleProperty, "SquareIconButton");
            System.Windows.Automation.AutomationProperties.SetName(button, tip);
            button.Click += (s, e) => onClick();
            return button;
        }

        private void ToggleMaximize() =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private async System.Threading.Tasks.Task LoadAsync()
        {
            if (!HelpFile.Ensure())
            {
                _status.Text = Lang.T("help.missing");
                return;
            }

            if (!PdfBook.Available)
            {
                CanShow = false;
                _status.Text = Lang.T("help.noViewer");
                if (HelpFile.Open()) Close();
                return;
            }

            _book = new PdfBook();
            if (!await _book.OpenAsync(HelpFile.Path))
            {
                _status.Text = Lang.T("help.noViewer");
                if (HelpFile.Open()) Close();
                return;
            }

            _status.Visibility = Visibility.Collapsed;
            _scroll.Visibility = Visibility.Visible;
            await ShowPageAsync();
        }

        private async System.Threading.Tasks.Task ShowPageAsync()
        {
            if (_busy || !(_book?.IsOpen ?? false)) return;
            _busy = true;
            try
            {
                double surface = _scroll.ActualWidth > 10 ? _scroll.ActualWidth : ActualWidth - 60;
                double available = Math.Max(420, surface - 40);
                var image = _book == null ? null : await _book.RenderAsync(_index, available * _zoom);
                if (image != null)
                {
                    _page.Source = image;
                    _page.Width = image.PixelWidth;
                    _page.Height = image.PixelHeight;
                }
                _counter.Text = Lang.T("help.pageOf", _index + 1, (_book?.PageCount ?? 0));
                _previous.IsEnabled = _index > 0;
                _next.IsEnabled = _index < (_book?.PageCount ?? 0) - 1;
            }
            finally
            {
                _busy = false;
            }
        }

        private async void Turn(int step)
        {
            int target = _index + step;
            if (target < 0 || target >= (_book?.PageCount ?? 0)) return;
            _index = target;
            _scroll.ScrollToTop();
            await ShowPageAsync();
        }

        private async void Zoom(double change)
        {
            _zoom = change == 0 ? 1.0 : Math.Max(0.5, Math.Min(3.0, _zoom + change));
            await ShowPageAsync();
        }

        private void OnKey(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape: Close(); break;
                case Key.PageDown: Turn(1); e.Handled = true; break;
                case Key.PageUp: Turn(-1); e.Handled = true; break;
                case Key.Home: _index = 0; _scroll.ScrollToTop(); _ = ShowPageAsync(); e.Handled = true; break;
                case Key.End: _index = Math.Max(0, (_book?.PageCount ?? 0) - 1); _scroll.ScrollToTop(); _ = ShowPageAsync(); e.Handled = true; break;
                case Key.Add or Key.OemPlus: Zoom(0.2); e.Handled = true; break;
                case Key.Subtract or Key.OemMinus: Zoom(-0.2); e.Handled = true; break;
            }
        }

        private void OnWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                Zoom(e.Delta > 0 ? 0.2 : -0.2);
                e.Handled = true;
                return;
            }

            bool atBottom = _scroll.VerticalOffset >= _scroll.ScrollableHeight - 1;
            bool atTop = _scroll.VerticalOffset <= 1;
            if (e.Delta < 0 && atBottom) { Turn(1); e.Handled = true; }
            else if (e.Delta > 0 && atTop) { Turn(-1); e.Handled = true; }
        }

        public static bool CanShow { get; private set; } = true;

        public static void MarkUnavailable() => CanShow = false;
    }
}
