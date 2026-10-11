using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;

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

    public class ThemedWindow : Window
    {
        protected StackPanel Body { get; } = new() { Margin = new Thickness(22, 18, 22, 6) };
        protected StackPanel Buttons { get; } = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(22, 10, 22, 18)
        };

        private readonly TextBlock _title = new()
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(18, 0, 0, 0),
            FontWeight = FontWeights.SemiBold
        };

        public ThemedWindow()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.Height;
            Width = 520;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            UseLayoutRounding = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            SetResourceReference(BackgroundProperty, "WindowBrush");

            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 0,
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = new Thickness(0)
            });

            var root = new Border { BorderThickness = new Thickness(1) };
            root.SetResourceReference(Border.BorderBrushProperty, "BorderStrongBrush");
            root.SetResourceReference(Border.BackgroundProperty, "WindowBrush");
            root.SetResourceReference(TextElement.ForegroundProperty, "TextBrush");

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var bar = new Border { BorderThickness = new Thickness(0, 0, 0, 1) };
            bar.SetResourceReference(Border.BackgroundProperty, "ChromeBrush");
            bar.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
            bar.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };

            var barGrid = new Grid();
            barGrid.Children.Add(_title);

            var close = new Button
            {
                Width = 42,
                Height = 42,
                HorizontalAlignment = HorizontalAlignment.Right,
                Content = new IconView { Kind = Ico.Close, Width = 13, Height = 13 }
            };
            close.SetResourceReference(StyleProperty, "IconButton");
            close.Click += (s, e) => { DialogResult ??= false; Close(); };
            WindowChrome.SetIsHitTestVisibleInChrome(close, true);
            barGrid.Children.Add(close);
            bar.Child = barGrid;

            var bodyScroll = new ScrollViewer
            {
                Content = Body,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = Math.Max(240, Math.Min(560, SystemParameters.WorkArea.Height - 190))
            };

            Grid.SetRow(bar, 0);
            Grid.SetRow(bodyScroll, 1);
            Grid.SetRow(Buttons, 2);
            grid.Children.Add(bar);
            grid.Children.Add(bodyScroll);
            grid.Children.Add(Buttons);

            root.Child = grid;
            Content = root;

            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) { DialogResult ??= false; Close(); }
            };

            WindowStyling.UseSquareCorners(this);
            OwnerDim.Attach(this);
        }

        public new string Title
        {
            get => _title.Text;
            set { base.Title = value; _title.Text = value; }
        }

        protected TextBlock AddLabel(string text, bool bold = false, double topMargin = 10)
        {
            var tb = new TextBlock
            {
                Text = text,
                Margin = new Thickness(0, topMargin, 0, 4),
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal
            };
            Body.Children.Add(tb);
            return tb;
        }

        protected TextBlock AddHint(string text)
        {
            var tb = new TextBlock { Text = text, Margin = new Thickness(0, 0, 0, 8) };
            tb.SetResourceReference(StyleProperty, "Hint");
            Body.Children.Add(tb);
            return tb;
        }

        protected TextBox AddTextBox(string value = "", bool password = false, int height = 0)
        {
            var box = new TextBox { Text = value, Margin = new Thickness(0, 0, 0, 2) };
            if (height > 0)
            {
                box.Height = height;
                box.AcceptsReturn = true;
                box.TextWrapping = TextWrapping.Wrap;
                box.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                box.VerticalContentAlignment = VerticalAlignment.Top;
            }
            Body.Children.Add(box);
            return box;
        }

        protected ComboBox AddCombo(System.Collections.Generic.IEnumerable<string> items, int selected = 0)
        {
            var combo = new ComboBox { Margin = new Thickness(0, 0, 0, 2) };
            foreach (var i in items) combo.Items.Add(i);
            if (combo.Items.Count > 0)
                combo.SelectedIndex = Math.Max(0, Math.Min(selected, combo.Items.Count - 1));
            Body.Children.Add(combo);
            return combo;
        }

        protected CheckBox AddCheck(string text, bool value)
        {
            var cb = new CheckBox { Content = text, IsChecked = value, Margin = new Thickness(0, 8, 0, 2) };
            Body.Children.Add(cb);
            return cb;
        }

        protected Button AddButton(string text, bool accent = false, Ico? icon = null, Action? onClick = null)
        {
            var button = new Button { MinWidth = 104, Margin = new Thickness(8, 0, 0, 0) };
            button.SetResourceReference(StyleProperty, accent ? "AccentButton" : "FlatButton");
            button.Content = icon == null
                ? text
                : MainWindow.BuildIconLabel(icon.Value, text, 15, accent ? Brushes.White : null);
            if (onClick != null) button.Click += (s, e) => onClick();
            Buttons.Children.Add(button);
            return button;
        }

        protected void AddOkCancel(Action onOk, string? okText = null)
        {
            AddButton(okText ?? Lang.T("dlg.save"), accent: true, onClick: () => onOk());
            AddButton(Lang.T("dlg.cancel"), onClick: () => { DialogResult = false; Close(); });
        }
    }
}
