using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
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

    public partial class SettingsPage : UserControl, IRefreshablePage
    {
        private readonly AppEngine _engine;
        private readonly MainWindow _owner;
        private bool _loading;

        private static readonly string AdminFlagPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "runasadmin.flag");

        public SettingsPage(AppEngine engine, MainWindow owner)
        {
            _engine = engine;
            _owner = owner;
            InitializeComponent();
            Build();
        }

        private void Build()
        {
            _loading = true;
            Root.Children.Clear();

            BuildLanguageCard();

            var behaviour = Section("set.groupBehaviour", Ico.Toggle);
            Option(behaviour, "set.autoStartWindows", "tip.autoStartWindows",
                _engine.Flag("runWindowsAuto"), v =>
                {
                    _engine.SetFlag("runWindowsAuto", v);
                    try { _engine.SetAutoStart(v); }
                    catch (Exception ex) { Warn(Lang.T("msg.autostartError", ex.Message)); }
                });
            Option(behaviour, "set.autoCountdown", "tip.autoCountdown",
                _engine.Flag("autoStart"), v => _engine.SetFlag("autoStart", v));
            Option(behaviour, "set.autoQueue", "tip.autoQueue",
                _engine.Get("autoStartQueue") != "0", v => _engine.SetFlag("autoStartQueue", v));
            Option(behaviour, "set.resetCounter", "tip.resetCounter",
                _engine.Get("RunHost") != "0", v => _engine.SetFlag("RunHost", v));
            Option(behaviour, "set.timezone", "tip.timezone",
                _engine.Get("timezoneCheck") != "0", v => _engine.SetFlag("timezoneCheck", v));
            Option(behaviour, "set.noSleep", "tip.noSleep",
                _engine.Flag("keepAwake"), v =>
                {
                    _engine.SetFlag("keepAwake", v);
                    _engine.SetKeepAwake(v, _engine.Flag("keepScreenOn"));
                    Build();
                });
            if (_engine.Flag("keepAwake"))
                Option(behaviour, "set.keepScreenOn", "tip.keepScreenOn",
                    _engine.Flag("keepScreenOn"), v =>
                    {
                        _engine.SetFlag("keepScreenOn", v);
                        _engine.SetKeepAwake(true, v);
                    });
            Option(behaviour, "set.waitForProcess", "tip.waitForProcess",
                _engine.Flag("userCommandBehaviour"), v => _engine.SetFlag("userCommandBehaviour", v));
            Option(behaviour, "set.checkUpdates", "tip.checkUpdates",
                _engine.Get("checkUpdates") != "0", v => _engine.SetFlag("checkUpdates", v));

            var appearance = Section("set.groupAppearance", Ico.Sparkle);
            Option(appearance, "set.darkMode", "tip.darkMode",
                _engine.Flag("darkModeEnabled"), v =>
                {
                    _engine.SetFlag("darkModeEnabled", v);
                    _owner.ApplyTheme(v);
                });
            Option(appearance, "set.roundedCorners", "tip.roundedCorners",
                _engine.Flag("roundedCorners"), v =>
                {
                    _engine.SetFlag("roundedCorners", v);
                    _owner.ApplyTheme(ThemeManager.IsDark);
                });
            BuildAccentRow(appearance);
            Option(appearance, "set.hideTips", "tip.hideTips",
                _engine.Flag("hideTips"), v =>
                {
                    _engine.SetFlag("hideTips", v);
                    _owner.RefreshOtherPages(this);
                });
            Option(appearance, "set.minimizeToTray", "tip.minimizeToTray",
                _engine.Flag("noMinimalize"), v => _engine.SetFlag("noMinimalize", v));
            Option(appearance, "set.noTray", "tip.noTray",
                _engine.Flag("noTray"), v =>
                {
                    _engine.SetFlag("noTray", v);
                    App.Instance?.RefreshTray();
                });
            Option(appearance, "set.noCountdown", "tip.noCountdown",
                _engine.Flag("noTimerShow"), v =>
                {
                    _engine.SetFlag("noTimerShow", v);
                    _owner.RefreshOtherPages(this);
                });
            Option(appearance, "set.noBalloon", "tip.noBalloon",
                _engine.Flag("noBalloon"), v => _engine.SetFlag("noBalloon", v));
            Option(appearance, "set.soundEveryTask", "tip.soundEveryTask",
                _engine.Flag("soundEveryTask"), v => _engine.SetFlag("soundEveryTask", v));

            var security = Section("set.groupSecurity", Ico.Shield);
            Option(security, "set.passwordLock", "tip.passwordLock",
                _engine.Flag("protect"), v =>
                {
                    _engine.SetFlag("protect", v);
                    _owner.SetLocked(v);
                });
            bool admin = IsAdmin();
            Option(security, "set.runAsAdmin", admin ? "tip.runAsAdminOn" : "tip.runAsAdminOff",
                File.Exists(AdminFlagPath), SetRunAsAdmin, accentText: admin);

            var tools = Section("set.groupTools", Ico.Gear);
            ToolButton(tools, "set.setPassword", Ico.Key,
                () => new SetPasswordDialog { Owner = _owner }.ShowDialog());
            ToolButton(tools, "set.backup", Ico.Save,
                () => new BackupDialog(_engine) { Owner = _owner }.ShowDialog());
            ToolButton(tools, "set.editConfig", Ico.Note, EditConfig);
            ToolButton(tools, "set.resetSettings", Ico.Refresh, ResetSettings);

            _loading = false;
        }

        private void BuildLanguageCard()
        {
            var card = new Border
            {
                Style = (Style)FindResource("Card"),
                Margin = new Thickness(0, 0, 0, 16)
            };
            var panel = new StackPanel();

            var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            header.Children.Add(new IconView
            {
                Kind = Ico.Globe,
                Width = 18,
                Height = 18,
                Margin = new Thickness(0, 0, 10, 0),
                Brush = ThemeManager.Brush("AccentBrush")
            });
            header.Children.Add(new TextBlock
            {
                Text = Lang.T("set.langSection"),
                FontWeight = FontWeights.SemiBold,
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Center
            });
            panel.Children.Add(header);

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(new TextBlock
            {
                Text = Lang.T("set.language"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            });

            var combo = new ComboBox { Width = 220 };
            combo.Items.Add("Polski");
            combo.Items.Add("English");
            combo.SelectedIndex = Lang.Current == Lang.EN ? 1 : 0;
            combo.SelectionChanged += (s, e) =>
            {
                if (_loading) return;
                string code = combo.SelectedIndex == 1 ? Lang.EN : Lang.PL;
                if (code == Lang.Current) return;
                _engine.Set("language", code, immediate: true);
                Lang.Set(code);
            };
            row.Children.Add(combo);
            panel.Children.Add(row);

            panel.Children.Add(new TextBlock
            {
                Text = Lang.T("tip.language"),
                Margin = new Thickness(0, 10, 0, 0),
                Style = (Style)FindResource("Hint")
            });

            card.Child = panel;
            Root.Children.Add(card);
        }

        private StackPanel Section(string titleKey, Ico icon)
        {
            var panel = new StackPanel();

            var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            header.Children.Add(new IconView
            {
                Kind = icon,
                Width = 18,
                Height = 18,
                Margin = new Thickness(0, 0, 10, 0),
                Brush = ThemeManager.Brush("AccentBrush")
            });
            header.Children.Add(new TextBlock
            {
                Text = Lang.T(titleKey),
                FontWeight = FontWeights.SemiBold,
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Center
            });
            panel.Children.Add(header);

            Root.Children.Add(new Border
            {
                Style = (Style)FindResource("Card"),
                Margin = new Thickness(0, 0, 0, 16),
                Child = panel
            });
            return panel;
        }

        private void Option(StackPanel parent, string textKey, string? tipKey, bool value,
                            Action<bool> onChange, bool accentText = false)
        {
            var box = new CheckBox
            {
                Content = new TextBlock { Text = Lang.T(textKey), TextWrapping = TextWrapping.Wrap },
                IsChecked = value,
                Margin = new Thickness(0, 10, 0, 0)
            };
            if (accentText) box.Foreground = ThemeManager.Brush("SuccessBrush");
            box.Checked += (s, e) => { if (!_loading) onChange(true); };
            box.Unchecked += (s, e) => { if (!_loading) onChange(false); };
            parent.Children.Add(box);

            if (tipKey == null) return;

            string tip = Lang.T(tipKey).Replace("\n", " ");
            parent.Children.Add(new TextBlock
            {
                Text = tip,
                ToolTip = tip,
                Margin = new Thickness(30, 3, 0, 4),
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Style = (Style)FindResource("Hint")
            });
        }

        private static readonly (string Name, string Hex)[] AccentPalette =
        {
            ("col.pink",   ""),
            ("col.red",    "#D2353B"),
            ("col.orange", "#E07A1F"),
            ("col.green",  "#2E9E5B"),
            ("col.teal",   "#13897F"),
            ("col.blue",   "#2D6FD4"),
            ("col.violet", "#7A4AD6"),
            ("col.gray",   "#5A6472"),
        };

        private void BuildAccentRow(StackPanel parent)
        {
            parent.Children.Add(new TextBlock
            {
                Text = Lang.T("set.accent"),
                Margin = new Thickness(0, 16, 0, 2),
                FontWeight = FontWeights.SemiBold
            });
            parent.Children.Add(new TextBlock
            {
                Text = Lang.T("tip.accent"),
                Margin = new Thickness(0, 0, 0, 8),
                Style = (Style)FindResource("Hint")
            });

            string current = _engine.Get("accentColor").Trim();
            var row = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };

            foreach (var entry in AccentPalette)
            {
                string hex = entry.Hex;
                string shown = hex.Length > 0
                    ? hex
                    : (ThemeManager.IsDark ? ThemeManager.DefaultAccentDark : ThemeManager.DefaultAccentLight);

                bool selected = string.Equals(current, hex, StringComparison.OrdinalIgnoreCase);
                row.Children.Add(Swatch(shown, Lang.T(entry.Name), selected, () => SetAccent(hex)));
            }

            bool custom = current.Length > 0 && !AccentPalette.Any(
                e => string.Equals(e.Hex, current, StringComparison.OrdinalIgnoreCase));
            row.Children.Add(Swatch(custom ? current : "#808080", Lang.T("col.custom"), custom, PickAccent));

            parent.Children.Add(row);
        }

        private Button Swatch(string hex, string tip, bool selected, Action onClick)
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex)!;
            var button = new Button
            {
                Width = 34,
                Height = 30,
                Margin = new Thickness(0, 0, 8, 8),
                ToolTip = tip,
                Background = new System.Windows.Media.SolidColorBrush(color),
                BorderThickness = new Thickness(selected ? 3 : 1),
                BorderBrush = selected
                    ? ThemeManager.Brush("TextBrush")
                    : ThemeManager.Brush("BorderStrongBrush"),
                Content = selected ? new IconView
                {
                    Kind = Ico.Check,
                    Width = 13,
                    Height = 13,
                    Brush = System.Windows.Media.Brushes.White
                } : null
            };
            button.SetResourceReference(StyleProperty, "SquareIconButton");
            button.Background = new System.Windows.Media.SolidColorBrush(color);
            button.Click += (s, e) => onClick();
            return button;
        }

        private void SetAccent(string hex)
        {
            _engine.Set("accentColor", hex, immediate: true);
            _owner.ApplyTheme(ThemeManager.IsDark);
        }

        private void PickAccent()
        {
            using var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true };
            string current = _engine.Get("accentColor").Trim();
            if (current.Length > 0)
            {
                try
                {
                    var now = (System.Windows.Media.Color)
                        System.Windows.Media.ColorConverter.ConvertFromString(current)!;
                    dialog.Color = System.Drawing.Color.FromArgb(now.R, now.G, now.B);
                }
                catch { }
            }

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            SetAccent($"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}");
        }

        private void ToolButton(StackPanel parent, string textKey, Ico icon, Action onClick)
        {
            var button = new Button
            {
                Content = MainWindow.BuildIconLabel(icon, Lang.T(textKey), 15,
                    ThemeManager.Brush("TextBrush")),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 8, 0, 0),
                MinHeight = 38
            };
            button.SetResourceReference(StyleProperty, "FlatButton");
            button.Click += (s, e) => onClick();
            parent.Children.Add(button);
        }

        private void SetRunAsAdmin(bool enabled)
        {
            try
            {
                if (!enabled)
                {
                    if (File.Exists(AdminFlagPath)) File.Delete(AdminFlagPath);
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(AdminFlagPath)!);
                File.WriteAllText(AdminFlagPath, "1");
                if (IsAdmin()) return;

                if (!MessageDialog.Confirm(_owner, Lang.T("dlg.runAsAdminTip"), Lang.T("dlg.runAsAdminNow")))
                    return;

                _engine.FlushPendingSave();
                string exe = Environment.ProcessPath ?? "";
                if (exe.Length == 0) return;
                Process.Start(new ProcessStartInfo { FileName = exe, UseShellExecute = true, Verb = "runas" });
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Warn(ex.Message);
            }
        }

        private void EditConfig()
        {
            try
            {
                _engine.FlushPendingSave();
                Process.Start(new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = $"\"{_engine.ConfigPath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex) { Warn(ex.Message); }
        }

        private void ResetSettings()
        {
            if (!MessageDialog.Confirm(_owner, Lang.T("msg.resetAsk"), Lang.T("dlg.confirm"))) return;

            try
            {
                RestoreUserPowerPlan();

                string data = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
                foreach (string name in new[] { "config.json", "psd.dat", "password.dat" })
                {
                    string file = Path.Combine(data, name);
                    if (File.Exists(file)) File.Delete(file);
                }

                _engine.SkipNextSave();
                MessageDialog.ShowTimed(_owner, Lang.T("msg.resetDone"), Lang.T("dlg.success"), MessageKind.Success, 4);
                RestartProgram();
            }
            catch (Exception ex) { Warn(ex.Message); }
        }

        private void RestoreUserPowerPlan()
        {
            _engine.SetKeepAwake(false, false);
            _engine.StopPowerPlanMonitor();

            string restore = _engine.Get("powerPlanID");
            if (restore.Length > 0) AppEngine.RunPowerCfg($"/setactive {restore}", false);

            string list = AppEngine.RunPowerCfg("/list", true);
            foreach (string line in list.Split('\n'))
            {
                if (line.IndexOf("czasowy - kontrola Windows", StringComparison.OrdinalIgnoreCase) < 0) continue;
                var found = System.Text.RegularExpressions.Regex.Match(line,
                    @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
                if (found.Success) AppEngine.RunPowerCfg($"/delete {found.Value}", false);
            }
        }

        private void RestartProgram()
        {
            string exe = Environment.ProcessPath ?? "";
            if (exe.Length == 0) return;

            try
            {
                _engine.FlushPendingSave();
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c ping -n 4 127.0.0.1 > nul & start \"\" \"{exe}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                Application.Current.Shutdown();
            }
            catch (Exception ex) { Warn(ex.Message); }
        }

        private static bool IsAdmin()
        {
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                return new System.Security.Principal.WindowsPrincipal(identity)
                    .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        private void Warn(string message) =>
            MessageDialog.Show(_owner, message, Lang.T("msg.error"), MessageKind.Error);

        public void RefreshTexts() => Build();

        public void RefreshTheme() => Build();
    }
}
