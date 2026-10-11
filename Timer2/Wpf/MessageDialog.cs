using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
    public enum MessageKind { Info, Warning, Error, Question, Success }

    public sealed class MessageDialog : ThemedWindow
    {
        private MessageDialog(string text, string title, MessageKind kind, bool withCancel,
                              string? okText, string? cancelText)
        {
            Title = title;
            Width = 480;

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(new IconView
            {
                Kind = kind switch
                {
                    MessageKind.Warning => Ico.Warning,
                    MessageKind.Error => Ico.Error,
                    MessageKind.Question => Ico.Help,
                    MessageKind.Success => Ico.Check,
                    _ => Ico.Info
                },
                Width = 30,
                Height = 30,
                Margin = new Thickness(0, 2, 16, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Brush = kind switch
                {
                    MessageKind.Warning => ThemeManager.Brush("WarningBrush"),
                    MessageKind.Error => ThemeManager.Brush("DangerBrush"),
                    MessageKind.Success => ThemeManager.Brush("SuccessBrush"),
                    _ => ThemeManager.Brush("AccentBrush")
                }
            });
            row.Children.Add(new TextBlock
            {
                Text = text,
                MaxWidth = 380,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            });
            Body.Children.Add(row);

            AddButton(okText ?? "OK", accent: true, onClick: () => { DialogResult = true; Close(); });
            if (withCancel)
                AddButton(cancelText ?? Lang.T("dlg.cancel"), onClick: () => { DialogResult = false; Close(); });
        }

        public static void Show(Window? owner, string text, string title, MessageKind kind = MessageKind.Info)
        {
            var dlg = new MessageDialog(text, title, kind, false, null, null);
            if (owner != null && owner.IsVisible) dlg.Owner = owner;
            else dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dlg.ShowDialog();
        }

        public static void ShowTimed(Window? owner, string text, string title, MessageKind kind, int seconds)
        {
            var dialog = new MessageDialog(text, title, kind, false, null, null);
            if (owner != null && owner.IsVisible) dialog.Owner = owner;
            else dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var countdown = new System.Windows.Threading.DispatcherTimer
            { Interval = TimeSpan.FromSeconds(Math.Max(1, seconds)) };
            countdown.Tick += (s, e) =>
            {
                countdown.Stop();
                if (dialog.IsLoaded) dialog.Close();
            };
            dialog.Loaded += (s, e) => countdown.Start();
            dialog.Closed += (s, e) => countdown.Stop();
            dialog.ShowDialog();
        }

        public static bool Confirm(Window? owner, string text, string title,
                                   string? okText = null, string? cancelText = null)
        {
            var dlg = new MessageDialog(text, title, MessageKind.Question, true, okText, cancelText);
            if (owner != null && owner.IsVisible) dlg.Owner = owner;
            else dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return dlg.ShowDialog() == true;
        }
    }
}
