using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
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
namespace TimerSys.Wpf.Dialogs
{

    public sealed class ActionParamDialog : ThemedWindow
    {
        public ActionParamDialog(AppEngine engine, int activator, int actionIndex)
        {
            var names = Lang.A("actions");
            Title = Lang.T("dlg.actionSettings", actionIndex < names.Length ? names[actionIndex] : "");
            Width = 560;

            var descriptions = Lang.A("actionDesc");
            AddHint(actionIndex < descriptions.Length ? descriptions[actionIndex] : "");

            string name = actionIndex switch
            {
                1 => "cmd",
                10 => "note",
                11 => "url",
                _ => ""
            };

            if (name.Length == 0)
            {
                AddButton(Lang.T("dlg.close"), accent: true, onClick: () => { DialogResult = true; Close(); });
                return;
            }

            var shared = AddCheck(Lang.T("par.useShared"), engine.ParamShared(activator, "param"));
            shared.IsEnabled = activator > 0;

            AddLabel(Lang.T(actionIndex switch
            {
                1 => "dlg.commandParam",
                10 => "dlg.noteParam",
                _ => "dlg.urlParam"
            }), bold: true);

            var box = AddTextBox(engine.Param(activator, "param", name), height: actionIndex == 10 ? 120 : 0);

            void Reload()
            {
                bool before = engine.ParamShared(activator, "param");
                engine.SetParamShared(activator, "param", shared.IsChecked == true);
                box.Text = engine.Param(activator, "param", name);
                engine.SetParamShared(activator, "param", before);
            }
            shared.Checked += (s, e) => Reload();
            shared.Unchecked += (s, e) => Reload();

            if (actionIndex == 1)
            {
                AddButtonRow(Lang.T("dlg.pickProgram"), Ico.Folder, () =>
                {
                    var ofd = new OpenFileDialog { Filter = Lang.T("dlg.filterPrograms"), Title = Lang.T("dlg.pickProgram") };
                    if (ofd.ShowDialog(this) == true) box.Text = ofd.FileName;
                });
            }

            AddOkCancel(() =>
            {
                engine.SetParamShared(activator, "param", shared.IsChecked == true);
                engine.SetParam(activator, "param", name, box.Text.Trim());
                engine.SaveSettings();
                DialogResult = true;
                Close();
            });
        }

        private void AddButtonRow(string text, Ico icon, Action onClick)
        {
            var button = new Button
            {
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Content = MainWindow.BuildIconLabel(icon, text, 15)
            };
            button.SetResourceReference(StyleProperty, "FlatButton");
            button.Click += (s, e) => onClick();
            Body.Children.Add(button);
        }
    }

    public sealed class RepeatIntervalDialog : ThemedWindow
    {
        public RepeatIntervalDialog(AppEngine engine)
        {
            Title = Lang.T("dlg.repeatTitle");
            Width = 420;

            AddLabel(Lang.T("dlg.repeatLabel"), bold: true, topMargin: 0);

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            var value = new TextBox
            {
                Text = engine.Get("timer3_repeat_value"),
                Width = 80,
                Margin = new Thickness(0, 0, 14, 0)
            };
            row.Children.Add(value);

            var units = Lang.A("repeatUnits");
            string current = engine.Get("timer3_repeat_type");
            var radios = new RadioButton[3];
            for (int i = 0; i < 3; i++)
            {
                radios[i] = new RadioButton
                {
                    Content = units[i],
                    Margin = new Thickness(0, 0, 14, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    IsChecked = current == new[] { "m", "h", "d" }[i]
                };
                row.Children.Add(radios[i]);
            }
            if (!radios.Any(r => r.IsChecked == true)) radios[0].IsChecked = true;
            Body.Children.Add(row);

            AddOkCancel(() =>
            {
                engine.Set("timer3_repeat_type", radios[1].IsChecked == true ? "h" : radios[2].IsChecked == true ? "d" : "m");
                engine.Set("timer3_repeat_value",
                    int.TryParse(value.Text.Trim(), out int n) && n > 0 ? n.ToString() : "30", immediate: true);
                DialogResult = true;
                Close();
            });
        }
    }

    public sealed class ScreenshotDialog : ThemedWindow
    {
        public ScreenshotDialog(AppEngine engine)
        {
            Title = Lang.T("dlg.ssTitle");
            Width = 520;

            AddLabel(Lang.T("dlg.ssFormat"), topMargin: 0);
            var format = AddCombo(new[] { "PNG", "JPG", "BMP", "GIF" },
                Array.IndexOf(new[] { "PNG", "JPG", "BMP", "GIF" }, engine.Get("ss_format").ToUpperInvariant()));

            AddLabel(Lang.T("dlg.ssCount"));
            var count = AddTextBox(engine.Get("ss_count"));
            AddLabel(Lang.T("dlg.ssDelay"));
            var delay = AddTextBox(engine.Get("ss_delay"));
            AddLabel(Lang.T("dlg.ssInterval"));
            var interval = AddTextBox(engine.Get("ss_interval"));
            var zip = AddCheck(Lang.T("dlg.ssZip"), engine.Get("ss_compress") == "1");

            AddLabel(Lang.T("dlg.ssFolder"));
            var folderRow = new Grid();
            folderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            folderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var folder = new TextBox { Text = engine.Get("ss_folder") };
            var browse = new Button { Width = 40, Margin = new Thickness(8, 0, 0, 0), Content = new IconView { Kind = Ico.Folder, Width = 15, Height = 15 } };
            browse.SetResourceReference(StyleProperty, "SquareIconButton");
            browse.Click += (s, e) =>
            {
                var dialog = new OpenFolderDialog { Title = Lang.T("dlg.ssFolder") };
                if (dialog.ShowDialog(this) == true) folder.Text = dialog.FolderName;
            };
            Grid.SetColumn(browse, 1);
            folderRow.Children.Add(folder);
            folderRow.Children.Add(browse);
            Body.Children.Add(folderRow);

            AddOkCancel(() =>
            {
                engine.Set("ss_format", format.SelectedItem?.ToString() ?? "PNG");
                engine.Set("ss_count", count.Text.Trim());
                engine.Set("ss_delay", delay.Text.Trim());
                engine.Set("ss_interval", interval.Text.Trim());
                engine.Set("ss_compress", zip.IsChecked == true ? "1" : "0");
                engine.Set("ss_folder", folder.Text.Trim(), immediate: true);
                DialogResult = true;
                Close();
            });
        }
    }

    public sealed class PasswordDialog : ThemedWindow
    {
        private int _attempts;

        public PasswordDialog()
        {
            Title = Lang.T("pwd.dlgTitle");
            Width = 430;

            AddLabel(Lang.T("pwd.enter"), bold: true, topMargin: 0);
            var box = new PasswordBox { Margin = new Thickness(0, 4, 0, 2), MinHeight = 32, Padding = new Thickness(9, 6, 9, 6) };
            box.SetResourceReference(BackgroundProperty, "InputBrush");
            box.SetResourceReference(ForegroundProperty, "TextBrush");
            Body.Children.Add(box);

            var error = new TextBlock { Margin = new Thickness(0, 6, 0, 0), Visibility = Visibility.Collapsed };
            error.SetResourceReference(ForegroundProperty, "AccentBrush");
            Body.Children.Add(error);

            var ok = AddButton("OK", accent: true, onClick: () =>
            {
                if (!PasswordStore.Exists())
                {
                    MessageDialog.Show(this, Lang.T("pwd.missing"), Lang.T("msg.title"), MessageKind.Warning);
                    DialogResult = true;
                    Close();
                    return;
                }
                if (PasswordStore.Verify(box.Password))
                {
                    DialogResult = true;
                    Close();
                    return;
                }
                _attempts++;
                error.Text = Lang.T(_attempts >= 5 ? "pwd.wrongWait" : "pwd.wrong");
                error.Visibility = Visibility.Visible;
                box.SelectAll();
                box.Focus();
            });

            box.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Enter) ok.Command?.Execute(null); };
            box.KeyUp += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                    ok.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            };
            Loaded += (s, e) => box.Focus();
        }
    }

    public sealed class SetPasswordDialog : ThemedWindow
    {
        private const int MinLength = 4;

        public SetPasswordDialog()
        {
            Title = Lang.T("pwd.setTitle");
            Width = 450;

            AddLabel(Lang.T("pwd.first"), bold: true, topMargin: 0);
            var first = NewBox();
            AddLabel(Lang.T("pwd.repeat"), bold: true);
            var second = NewBox();

            var error = new TextBlock { Margin = new Thickness(0, 8, 0, 0) };
            error.SetResourceReference(ForegroundProperty, "AccentBrush");
            Body.Children.Add(error);

            AddOkCancel(() =>
            {
                if (first.Password.Length < MinLength) { error.Text = Lang.T("pwd.tooShort", MinLength); return; }
                if (first.Password != second.Password) { error.Text = Lang.T("pwd.mismatch"); return; }
                if (!PasswordStore.Save(first.Password))
                {
                    MessageDialog.Show(this, Lang.T("pwd.saveFailed"), Lang.T("msg.title"), MessageKind.Error);
                    return;
                }
                DialogResult = true;
                Close();
            }, okText: "OK");
        }

        private PasswordBox NewBox()
        {
            var box = new PasswordBox { Margin = new Thickness(0, 4, 0, 2), MinHeight = 32, Padding = new Thickness(9, 6, 9, 6) };
            box.SetResourceReference(BackgroundProperty, "InputBrush");
            box.SetResourceReference(ForegroundProperty, "TextBrush");
            Body.Children.Add(box);
            return box;
        }
    }

    public sealed class WelcomeDialog : ThemedWindow
    {
        public WelcomeDialog()
        {
            Title = Lang.T("welcome.title");
            Width = 520;

            AddHint(Lang.T("welcome.intro"));

            var box = new Border
            {
                Margin = new Thickness(0, 6, 0, 10),
                Padding = new Thickness(10, 14, 10, 14),
                CornerRadius = ThemeManager.Corner()
            };
            box.SetResourceReference(Border.BackgroundProperty, "AccentSoftBrush");
            box.Child = new TextBlock
            {
                Text = "123456",
                FontSize = 30,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = ThemeManager.Brush("AccentBrush")
            };
            Body.Children.Add(box);
            AddHint(Lang.T("welcome.note"));

            AddButton("OK", accent: true, onClick: () => { DialogResult = true; Close(); });

            if (!PasswordStore.Exists() && !PasswordStore.Save("123456"))
                Loaded += (s, e) => MessageDialog.Show(this, Lang.T("pwd.saveFailed"), Lang.T("msg.title"), MessageKind.Warning);
        }
    }

    public sealed class UpdateDialog : ThemedWindow
    {
        public bool SkipVersion { get; private set; }

        public UpdateDialog(string latest, string current)
        {
            Title = Lang.T("upd.title");
            Width = 500;

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(new IconView
            {
                Kind = Ico.Download, Width = 38, Height = 38,
                Margin = new Thickness(0, 2, 16, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Brush = ThemeManager.Brush("AccentBrush")
            });
            row.Children.Add(new TextBlock
            {
                Text = Lang.T("upd.question", latest, current),
                MaxWidth = 380,
                TextWrapping = TextWrapping.Wrap
            });
            Body.Children.Add(row);

            var skip = AddCheck(Lang.T("upd.skip"), false);

            AddButton(Lang.T("upd.open"), accent: true, icon: Ico.Globe, onClick: () =>
            {
                SkipVersion = skip.IsChecked == true;
                DialogResult = true;
                Close();
            });
            AddButton(Lang.T("upd.later"), onClick: () =>
            {
                SkipVersion = skip.IsChecked == true;
                DialogResult = false;
                Close();
            });
        }
    }
}
