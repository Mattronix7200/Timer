using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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

    public sealed class SoundDialog : ThemedWindow
    {
        public SoundDialog(AppEngine engine, int activator)
        {
            Title = Lang.T("dlg.soundTitle");
            Width = 540;

            string modeName = engine.ModeName(activator);
            AddHint(Lang.T("snd.hint"));

            var shared = AddCheck(Lang.T("snd.useShared"),
                activator <= 0 || engine.Get($"act{activator}_sound").Trim().Length == 0);
            shared.IsEnabled = activator > 0;

            var target = AddLabel("", bold: true);

            AddLabel(Lang.T("snd.list"));
            var files = SoundCatalog.Files.ToList();
            var combo = AddCombo(files.Select(SoundCatalog.DisplayName).Append(Lang.T("snd.custom")));
            int customIndex = combo.Items.Count - 1;

            var pathLabel = AddLabel(Lang.T("snd.customPath"));
            var pathRow = new Grid();
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var pathBox = new TextBox();
            var browse = new Button { Width = 40, Margin = new Thickness(8, 0, 0, 0), Content = new IconView { Kind = Ico.Folder, Width = 15, Height = 15 } };
            browse.SetResourceReference(StyleProperty, "SquareIconButton");
            Grid.SetColumn(browse, 1);
            pathRow.Children.Add(pathBox);
            pathRow.Children.Add(browse);
            Body.Children.Add(pathRow);

            void Load()
            {
                string value = shared.IsChecked == true
                    ? engine.Get("soundFile").Trim()
                    : engine.Get($"act{activator}_sound").Trim();
                if (value.Length == 0) value = SoundCatalog.DefaultFile;

                int idx = files.FindIndex(f => string.Equals(f, value, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) { combo.SelectedIndex = idx; pathBox.Text = ""; }
                else { combo.SelectedIndex = customIndex; pathBox.Text = value; }

                target.Text = shared.IsChecked == true
                    ? Lang.T("snd.shared")
                    : Lang.T("snd.forMode", modeName);
            }

            void SyncCustom()
            {

                var visibility = combo.SelectedIndex == customIndex ? Visibility.Visible : Visibility.Collapsed;
                pathLabel.Visibility = pathRow.Visibility = visibility;
            }

            string Current() => combo.SelectedIndex == customIndex
                ? pathBox.Text.Trim()
                : files[Math.Max(0, Math.Min(combo.SelectedIndex, files.Count - 1))];

            browse.Click += (s, e) =>
            {
                var ofd = new OpenFileDialog { Filter = Lang.T("dlg.soundFilter"), Title = Lang.T("dlg.soundPick") };
                if (ofd.ShowDialog(this) != true) return;
                pathBox.Text = ofd.FileName;
                combo.SelectedIndex = customIndex;
            };
            combo.SelectionChanged += (s, e) => SyncCustom();
            shared.Checked += (s, e) => { Load(); SyncCustom(); };
            shared.Unchecked += (s, e) => { Load(); SyncCustom(); };
            Load();
            SyncCustom();

            AddButton(Lang.T("dlg.soundTest"), icon: Ico.Play,
                onClick: () => engine.PlaySound(SoundCatalog.ResolvePath(Current())));
            AddButton(Lang.T("dlg.soundStop"), icon: Ico.Stop, onClick: engine.StopSound);
            AddOkCancel(() =>
            {
                string value = Current();
                if (shared.IsChecked == true)
                {
                    engine.Set("soundFile", value);
                    if (activator > 0) engine.Set($"act{activator}_sound", "");
                }
                else engine.Set($"act{activator}_sound", value);
                engine.SaveSettings();
                engine.StopSound();
                DialogResult = true;
                Close();
            });

            Closed += (s, e) => engine.StopSound();
        }
    }

    public sealed class CleanupDialog : ThemedWindow
    {
        private static readonly (string Key, string LabelKey, string TipKey, bool Admin)[] Options =
        {
            ("cleanTemp",        "clean.userTemp",  "clean.userTempTip",  false),
            ("cleanWinTemp",     "clean.winTemp",   "clean.winTempTip",   true),
            ("cleanDownloads",   "clean.downloads", "clean.downloadsTip", false),
            ("cleanWU",          "clean.wu",        "clean.wuTip",        true),
            ("cleanDriverCache", "clean.driver",    "clean.driverTip",    true),
            ("cleanThumbs",      "clean.thumbs",    "clean.thumbsTip",    false),
            ("cleanPrefetch",    "clean.prefetch",  "clean.prefetchTip",  false),
            ("cleanRecycleBin",  "clean.bin",       "clean.binTip",       false),
            ("cleanEventLog",    "clean.log",       "clean.logTip",       true),
        };

        public CleanupDialog(AppEngine engine)
        {
            Title = Lang.T("dlg.cleanTitle");
            Width = 600;

            bool admin = IsAdmin();
            var boxes = new List<(CheckBox Box, string Key)>();
            foreach (var opt in Options)
            {
                bool disabled = opt.Admin && !admin;
                var cb = AddCheck(Lang.T(opt.LabelKey) + (disabled ? Lang.T("dlg.needsAdmin") : ""),
                    !disabled && engine.Get(opt.Key) != "0");
                cb.IsEnabled = !disabled;
                cb.ToolTip = Lang.T(opt.TipKey) + (disabled ? Lang.T("dlg.needsAdminTip") : "");
                cb.Margin = new Thickness(0, 3, 0, 3);
                boxes.Add((cb, opt.Key));
            }

            AddLabel(Lang.T("dlg.cleanExtra"), bold: true, topMargin: 16);
            var list = new ListBox { Height = 110, Margin = new Thickness(0, 0, 0, 8) };
            list.SetResourceReference(BackgroundProperty, "InputBrush");
            list.SetResourceReference(ForegroundProperty, "TextBrush");
            list.SetResourceReference(BorderBrushProperty, "BorderBrush");
            foreach (var p in engine.Get("cleanCustomFolders").Split(';', StringSplitOptions.RemoveEmptyEntries))
                list.Items.Add(p.Trim());
            Body.Children.Add(list);

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            var add = new Button { Content = MainWindow.BuildIconLabel(Ico.Plus, Lang.T("dlg.cleanAdd"), 15), Margin = new Thickness(0, 0, 8, 0) };
            var remove = new Button { Content = MainWindow.BuildIconLabel(Ico.Close, Lang.T("dlg.cleanRemove"), 15) };
            add.SetResourceReference(StyleProperty, "FlatButton");
            remove.SetResourceReference(StyleProperty, "FlatButton");
            add.Click += (s, e) =>
            {
                var dialog = new OpenFolderDialog { Title = Lang.T("dlg.cleanPickDir") };
                if (dialog.ShowDialog(this) == true && !list.Items.Contains(dialog.FolderName))
                    list.Items.Add(dialog.FolderName);
            };
            remove.Click += (s, e) => { if (list.SelectedIndex >= 0) list.Items.RemoveAt(list.SelectedIndex); };
            row.Children.Add(add);
            row.Children.Add(remove);
            Body.Children.Add(row);

            AddOkCancel(() =>
            {
                foreach (var (box, key) in boxes) engine.Set(key, box.IsChecked == true ? "1" : "0");
                engine.Set("cleanCustomFolders",
                    string.Join(";", list.Items.Cast<string>()), immediate: true);
                DialogResult = true;
                Close();
            });
        }

        private static bool IsAdmin()
        {
            try
            {
                using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
                return new System.Security.Principal.WindowsPrincipal(id)
                    .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }
    }

    public sealed class BackupDialog : ThemedWindow
    {
        public BackupDialog(AppEngine engine)
        {
            Title = Lang.T("dlg.backupTitle");
            Width = 520;

            AddHint(Lang.T("dlg.backupInfo"));

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 4) };
            var create = new Button { Content = MainWindow.BuildIconLabel(Ico.Save, Lang.T("dlg.backupCreate"), 15), Margin = new Thickness(0, 0, 10, 0), MinWidth = 220 };
            var restore = new Button { Content = MainWindow.BuildIconLabel(Ico.FolderOpen, Lang.T("dlg.backupRestore"), 15), MinWidth = 220 };
            create.SetResourceReference(StyleProperty, "FlatButton");
            restore.SetResourceReference(StyleProperty, "FlatButton");
            row.Children.Add(create);
            row.Children.Add(restore);
            Body.Children.Add(row);

            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

            create.Click += (s, e) =>
            {
                var sfd = new SaveFileDialog
                {
                    Filter = Lang.T("dlg.backupFilter"),
                    FileName = $"TimerSys_backup_{DateTime.Now:yyyyMMdd_HHmm}.dat",
                    Title = Lang.T("dlg.backupSaveTitle")
                };
                if (sfd.ShowDialog(this) != true) return;
                try
                {
                    engine.SaveSettings();
                    using var zip = new ZipArchive(File.Create(sfd.FileName), ZipArchiveMode.Create);
                    foreach (var fn in new[] { "config.json", "psd.dat" })
                    {
                        string src = Path.Combine(dataDir, fn);
                        if (File.Exists(src)) zip.CreateEntryFromFile(src, fn);
                    }
                    MessageDialog.Show(this, Lang.T("dlg.backupOk"), Lang.T("dlg.success"), MessageKind.Success);
                }
                catch (Exception ex)
                {
                    MessageDialog.Show(this, Lang.T("dlg.backupFail", ex.Message), Lang.T("msg.error"), MessageKind.Error);
                }
            };

            restore.Click += (s, e) =>
            {
                var ofd = new OpenFileDialog { Filter = Lang.T("dlg.backupFilterOpen"), Title = Lang.T("dlg.backupOpenTitle") };
                if (ofd.ShowDialog(this) != true) return;
                if (!MessageDialog.Confirm(this, Lang.T("dlg.restoreWarn"), Lang.T("dlg.confirm"))) return;
                try
                {
                    Directory.CreateDirectory(dataDir);
                    string root = Path.GetFullPath(dataDir + Path.DirectorySeparatorChar);
                    using (var zip = new ZipArchive(File.OpenRead(ofd.FileName), ZipArchiveMode.Read))
                    {
                        foreach (var entry in zip.Entries)
                        {
                            if (string.IsNullOrEmpty(Path.GetFileName(entry.FullName))) continue;

                            string dest = Path.GetFullPath(Path.Combine(root, entry.FullName));
                            if (!dest.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;
                            entry.ExtractToFile(dest, true);
                        }
                    }
                    MessageDialog.Show(this, Lang.T("dlg.restoreOk"), Lang.T("dlg.success"), MessageKind.Success);
                    RestartApp();
                }
                catch (Exception ex)
                {
                    MessageDialog.Show(this, Lang.T("dlg.restoreFail", ex.Message), Lang.T("msg.error"), MessageKind.Error);
                }
            };

            AddButton(Lang.T("dlg.close"), accent: true, onClick: () => { DialogResult = true; Close(); });
        }

        private static void RestartApp()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c timeout /t 1 /nobreak > nul & start \"\" \"{Environment.ProcessPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch { }
            Application.Current.Shutdown();
        }
    }

}
