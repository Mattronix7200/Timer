using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TimerSys.Core;

using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Orientation = System.Windows.Controls.Orientation;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace TimerSys.Wpf
{

    internal sealed class ToastWindow : Window
    {
        private static readonly List<ToastWindow> Open = new();
        private const double ToastWidth = 380;
        private const double Gap = 12;

        private readonly DispatcherTimer _life = new() { Interval = TimeSpan.FromSeconds(9) };

        private ToastWindow(string title, string body, Ico icon)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            Width = ToastWidth;
            SizeToContent = SizeToContent.Height;
            UseLayoutRounding = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

            var card = new Border
            {
                Background = ThemeManager.Brush("CardBrush"),
                BorderBrush = ThemeManager.Brush("BorderStrongBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = ThemeManager.Corner(),
                Padding = new Thickness(16, 14, 12, 14),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 18,
                    ShadowDepth = 2,
                    Opacity = 0.35,
                    Color = Colors.Black
                }
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var glyph = new IconView
            {
                Kind = icon,
                Width = 24,
                Height = 24,
                Margin = new Thickness(0, 1, 14, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Brush = ThemeManager.Brush("AccentBrush")
            };
            Grid.SetColumn(glyph, 0);

            var text = new StackPanel();
            text.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                Foreground = ThemeManager.Brush("TextBrush"),
                TextWrapping = TextWrapping.Wrap
            });
            text.Children.Add(new TextBlock
            {
                Text = body,
                Margin = new Thickness(0, 5, 0, 0),
                FontSize = 12.5,
                Foreground = ThemeManager.Brush("TextMutedBrush"),
                TextWrapping = TextWrapping.Wrap
            });
            Grid.SetColumn(text, 1);

            var close = new Button
            {
                Width = 26,
                Height = 26,
                VerticalAlignment = VerticalAlignment.Top,
                Content = new IconView
                {
                    Kind = Ico.Close,
                    Width = 10,
                    Height = 10,
                    Brush = ThemeManager.Brush("TextMutedBrush")
                }
            };
            close.SetResourceReference(StyleProperty, "IconButton");
            close.Click += (s, e) => Dismiss();
            Grid.SetColumn(close, 2);

            grid.Children.Add(glyph);
            grid.Children.Add(text);
            grid.Children.Add(close);
            card.Child = grid;
            Content = card;

            MouseLeftButtonUp += (s, e) => Dismiss();
            _life.Tick += (s, e) => Dismiss();
        }

        public static void Show(string title, string body, Ico icon = Ico.Bell)
        {
            var toast = new ToastWindow(title, body, icon);
            toast.Show();

            var area = SystemParameters.WorkArea;
            toast.Left = area.Right - ToastWidth - Gap;
            toast.Top = area.Bottom - toast.ActualHeight - Gap;
            foreach (var other in Open)
                toast.Top -= other.ActualHeight + Gap;

            Open.Add(toast);
            toast._life.Start();

            toast.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
        }

        private void Dismiss()
        {
            _life.Stop();
            Open.Remove(this);

            var fade = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(160));
            fade.Completed += (s, e) => { try { Close(); } catch { } };
            BeginAnimation(OpacityProperty, fade);
        }
    }
}
