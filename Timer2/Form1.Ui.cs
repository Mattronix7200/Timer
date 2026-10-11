namespace Timer2
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using TimerSys;

    public partial class Form1
    {

        private const int ModeIdle = 1, ModeTimer = 2, ModeSchedule = 3;

        internal static readonly string[] ActionIds =
        {
            "Odtwórz dźwięk", "Uruchom polecenie", "Wyłącz monitor", "Uśpienie",
            "Wylogowanie", "Restart", "Wyłączenie komputera", "Hibernacja",
            "Blokada użytkownika", "Oczyść pliki tymczasowe", "Zapisz notatkę na pulpicie",
            "Otwórz stronę WWW", "Opróżnij kosz", "Wykonaj zrzut ekranu",
            ActionEmail
        };

        private static readonly Ico[] ActionIcons =
        {
            Ico.Speaker, Ico.Play, Ico.Monitor, Ico.Idle, Ico.Power, Ico.Refresh, Ico.Power,
            Ico.Idle, Ico.Lock, Ico.Sparkle, Ico.Note, Ico.Globe, Ico.Trash, Ico.Camera,
            Ico.Mail
        };

        private TabPage? _tabActivator;
        private Panel? _panelIdle, _panelTimer, _panelSched;
        private Button? _btnMode1, _btnMode2, _btnMode3;
        private Label? _lblModeHeader, _lblModeIntro;
        private int _activatorMode = ModeIdle;

        private Button[] _filterButtons = Array.Empty<Button>();
        private Label? _lblSnoozeHeader;
        private Button? _btnTimerInputMode;
        private TextBox? _txtDuration;
        private Label? _lblDurationHint, _lblDurationCaption;
        private bool _timerDurationMode = true;
        private CheckBox? _chkHideTips, _chkSoundEveryTask;
        private Label? _sepSnooze;
        private Button? _btnSnooze;
        private ComboBox? _cboSnooze;
        private Label? _lblLanguage;
        private ComboBox? _cboLanguage;
        private CheckBox? _chkCheckUpdates;
        private ImageList? _tabIcons;
        private ToolStripMenuItem? _menuExit, _menuStopIdle, _menuStopTimer, _menuResumeIdle,
                                   _menuResumeTimer, _menuSettings;

        private readonly List<(Control c, string key)> _i18n = new();

        private readonly List<(Control c, ToolTip tip, string key)> _i18nTips = new();

        private void Reg(Control c, string key) => _i18n.Add((c, key));
        private void RegTip(ToolTip tip, Control c, string key)
        {
            _i18nTips.Add((c, tip, key));
            tip.SetToolTip(c, Lang.T(key));
        }

        private bool DarkUi => checkBox11.Checked;
        private Color IconColor => DarkUi ? Color.FromArgb(222, 222, 222) : Color.FromArgb(55, 55, 58);
        private static Color AccentColor => Color.FromArgb(0, 120, 215);

        private void SetIcon(ButtonBase btn, Ico kind, int size = 18, Color? color = null)
        {
            btn.Image = VectorIcons.Get(kind, size, color ?? IconColor);
            btn.ImageAlign = ContentAlignment.MiddleLeft;
            if (!string.IsNullOrEmpty(btn.Text))
            {
                btn.TextImageRelation = TextImageRelation.ImageBeforeText;
                btn.TextAlign = ContentAlignment.MiddleCenter;
            }
            else
            {
                btn.ImageAlign = ContentAlignment.MiddleCenter;
            }
        }

        internal static int ActionIndexOf(string? id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            int i = Array.IndexOf(ActionIds, id);
            if (i >= 0) return i;
            return Array.IndexOf(Lang.A("actions"), id);
        }

        internal static string ActionDisplay(string? id)
        {
            int i = ActionIndexOf(id);
            var names = Lang.A("actions");
            return (i >= 0 && i < names.Length) ? names[i] : (id ?? "");
        }

        internal static string ActionIdFromIndex(int idx) =>
            (idx >= 0 && idx < ActionIds.Length) ? ActionIds[idx] : "";

        private void BuildActivatorTab()
        {
            var panelBounds = new Rectangle(0, 82, 666, 324);

            _tabActivator = new TabPage
            {
                Name      = "tabActivator",
                Text      = Lang.T("tab.activator"),
                BackColor = tabPage1.BackColor,
                UseVisualStyleBackColor = false
            };

            _lblModeHeader = new Label
            {
                Text     = Lang.T("mode.header"),
                Location = new Point(20, 14),
                AutoSize = true,
                Font     = new Font("Segoe UI", 9.75f, FontStyle.Bold)
            };
            _tabActivator.Controls.Add(_lblModeHeader);
            Reg(_lblModeHeader, "mode.header");

            _btnMode1 = MakeModeButton(ModeIdle,     "mode.idle",     150);
            _btnMode2 = MakeModeButton(ModeTimer,    "mode.timer",    306);
            _btnMode3 = MakeModeButton(ModeSchedule, "mode.schedule", 462);

            var sep = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location    = new Point(20, 42),
                Size        = new Size(613, 2),
                AutoSize    = false
            };
            _tabActivator.Controls.Add(sep);

            _lblModeIntro = new Label
            {
                Location  = new Point(20, 48),
                Size      = new Size(613, 32),
                AutoSize  = false,
                Font      = new Font("Segoe UI", 8.75f, FontStyle.Italic),
                ForeColor = Color.FromArgb(90, 90, 95)
            };
            _tabActivator.Controls.Add(_lblModeIntro);

            _panelIdle  = MovePageToPanel(tabPage1, panelBounds);
            _panelTimer = MovePageToPanel(tabPage2, panelBounds);
            _panelSched = MovePageToPanel(tabPage6, panelBounds);

            LayoutIdlePanel();
            LayoutTimerPanel();
            LayoutSchedulePanel();

            _tabActivator.Controls.Add(_panelIdle);
            _tabActivator.Controls.Add(_panelTimer);
            _tabActivator.Controls.Add(_panelSched);

            int insertAt = tabControl1.TabPages.IndexOf(tabPage1);
            if (insertAt < 0) insertAt = 0;
            tabControl1.TabPages.Remove(tabPage1);
            tabControl1.TabPages.Remove(tabPage2);
            tabControl1.TabPages.Remove(tabPage6);
            tabControl1.TabPages.Insert(insertAt, _tabActivator);
            tabControl1.SelectedIndex = 0;

            SetActivatorMode(int.TryParse(appSettings["activatorMode"], out int m) && m >= 1 && m <= 3
                ? m : ModeIdle, save: false);
        }

        private static Panel MovePageToPanel(TabPage page, Rectangle bounds)
        {
            var panel = new Panel
            {
                Location  = bounds.Location,
                Size      = bounds.Size,
                Visible   = false,
                BackColor = Color.Transparent
            };
            var kids = page.Controls.Cast<Control>().ToArray();
            page.Controls.Clear();
            panel.Controls.AddRange(kids);
            return panel;
        }

        private Button MakeModeButton(int mode, string key, int x)
        {
            var btn = new Button
            {
                Text      = Lang.T(key),
                Location  = new Point(x, 6),
                Size      = new Size(150, 30),
                FlatStyle = FlatStyle.Flat,
                TabStop   = true,
                Tag       = mode
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.Click += (s, e) => SetActivatorMode(mode);
            _tabActivator!.Controls.Add(btn);
            Reg(btn, key);
            return btn;
        }

        private void SetActivatorMode(int mode, bool save = true)
        {
            _activatorMode = mode;

            if (_panelIdle  != null) _panelIdle.Visible  = mode == ModeIdle;
            if (_panelTimer != null) _panelTimer.Visible = mode == ModeTimer;
            if (_panelSched != null) _panelSched.Visible = mode == ModeSchedule;

            if (_lblModeIntro != null)
                _lblModeIntro.Text = Lang.T(mode switch
                {
                    ModeTimer    => "mode.intro.timer",
                    ModeSchedule => "mode.intro.schedule",
                    _            => "mode.intro.idle"
                });

            StyleModeButtons();

            if (save)
            {
                appSettings["activatorMode"] = mode.ToString();
                ScheduleConfigSave();
            }
        }

        private void StyleModeButtons()
        {
            var icons = new[] { Ico.Idle, Ico.Stopwatch, Ico.Calendar };
            var buttons = new[] { _btnMode1, _btnMode2, _btnMode3 };

            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                if (btn == null) continue;
                bool active = (int)(btn.Tag ?? 0) == _activatorMode;

                btn.BackColor = active
                    ? AccentColor
                    : (DarkUi ? Color.FromArgb(58, 58, 58) : Color.FromArgb(240, 240, 242));
                btn.ForeColor = active
                    ? Color.White
                    : (DarkUi ? Color.FromArgb(220, 220, 220) : Color.FromArgb(40, 40, 40));
                btn.FlatAppearance.BorderColor = active
                    ? AccentColor
                    : (DarkUi ? Color.FromArgb(90, 90, 90) : Color.FromArgb(190, 190, 195));
                btn.Font = new Font("Segoe UI", 9.75f, active ? FontStyle.Bold : FontStyle.Regular);
                SetIcon(btn, icons[i], 18, active ? Color.White : IconColor);
            }
        }

        private static void Place(Control? c, int x, int y, int w = 0, int h = 0)
        {
            if (c == null) return;
            c.Location = new Point(x, y);
            if (w > 0 && h > 0) c.Size = new Size(w, h);
        }

        private void LayoutIdlePanel()
        {
            label11.Visible = false;
            Place(label2, 20, 8);
            Place(comboBox1, 20, 32, 343, 25);
            Place(_btnActSettings1, 368, 32, 30, 25);
            Place(label3, 20, 76);
            Place(textBox4, 20, 100, 343, 25);
            Place(label4, 20, 133);
            Place(button2, 20, 159, 343, 28);
            Place(textBox5, 20, 205, 627, 60);
            Place(tip01, 418, 8, 230, 180);
        }

        private void LayoutTimerPanel()
        {
            label12.Visible = false;
            Place(label5, 20, 8);
            Place(comboBox2, 20, 32, 343, 25);
            Place(_btnActSettings2, 368, 32, 30, 25);
            Place(label6, 20, 76);
            Place(dateTimePicker1, 20, 100, 343, 25);
            Place(textBox7, 20, 135, 79, 25);
            Place(label7, 106, 138);
            Place(button4, 20, 172, 343, 28);
            Place(textBox9, 20, 216, 627, 60);
            Place(tip02, 418, 8, 230, 180);

            _btnTimerInputMode = new Button
            {
                Location  = new Point(238, 71),
                Size      = new Size(160, 26),
                FlatStyle = FlatStyle.Flat,
                TabStop   = true
            };
            _btnTimerInputMode.Click += (s, e) => SetTimerInputMode(!_timerDurationMode);
            _panelTimer!.Controls.Add(_btnTimerInputMode);

            _lblDurationCaption = new Label
            {
                Text     = Lang.T("timer.duration"),
                Location = new Point(20, 76),
                Size     = new Size(210, 17),
                AutoSize = false,
                Font     = label6.Font
            };
            _panelTimer.Controls.Add(_lblDurationCaption);

            _txtDuration = new TextBox
            {
                Text     = appSettings["timer2Duration"] ?? "00:30:00",
                Location = new Point(20, 100),
                Size     = new Size(120, 25)
            };
            _txtDuration.TextChanged += (s, e) =>
            {
                appSettings["timer2Duration"] = _txtDuration.Text;
                ScheduleConfigSave();
            };
            _panelTimer.Controls.Add(_txtDuration);

            _lblDurationHint = new Label
            {
                Text     = Lang.T("timer.maxDuration"),
                Location = new Point(150, 103),
                Size     = new Size(240, 17),
                AutoSize = false
            };
            _panelTimer.Controls.Add(_lblDurationHint);

            SetTimerInputMode(appSettings["timer2InputMode"] != "datetime", save: false);
        }

        private void SetTimerInputMode(bool duration, bool save = true)
        {
            _timerDurationMode = duration;

            if (_lblDurationCaption != null) _lblDurationCaption.Visible = duration;
            if (_txtDuration != null)        _txtDuration.Visible        = duration;
            if (_lblDurationHint != null)    _lblDurationHint.Visible    = duration;

            label6.Visible          = !duration;
            dateTimePicker1.Visible = !duration;
            textBox7.Visible        = !duration;
            label7.Visible          = !duration;

            if (_btnTimerInputMode != null)
            {
                _btnTimerInputMode.Text = Lang.T(duration ? "timer.toDateTime" : "timer.toDuration");
                SetIcon(_btnTimerInputMode, duration ? Ico.Calendar : Ico.Stopwatch, 16);
            }

            if (save)
            {
                appSettings["timer2InputMode"] = duration ? "duration" : "datetime";
                ScheduleConfigSave();
            }
        }

        private bool TryGetTimerTarget(out DateTime target, out string error)
        {
            target = default;
            error  = "";

            if (_timerDurationMode)
            {
                if (!TryParseTime(_txtDuration?.Text, out var dur) ||
                    dur <= TimeSpan.Zero || dur >= TimeSpan.FromDays(1))
                {
                    error = Lang.T("msg.badDuration");
                    return false;
                }
                target = DateTime.Now.Add(dur);
                return true;
            }

            if (!TryParseTime(textBox7.Text, out var hour))
            {
                error = Lang.T("msg.badHourFormat");
                return false;
            }
            target = dateTimePicker1.Value.Date + hour;
            if (target <= DateTime.Now)
            {
                error = Lang.T("msg.pastDate");
                return false;
            }
            return true;
        }

        private void LayoutSchedulePanel()
        {
            label21.Visible = false;
            Place(label22, 20, 8);
            Place(comboBox3, 20, 32, 343, 25);
            Place(_btnActSettings3, 368, 32, 30, 25);
            Place(label25, 20, 67);
            Place(textBox1, 20, 91, 79, 25);
            Place(label24, 106, 94);
            Place(label26, 20, 126);
            Place(checkedListBox1, 20, 150, 343, 70);
            Place(button8, 20, 230, 343, 28);

            Place(tip03, 418, 8, 217, 170);
            Place(stopStartHarmonogram, 418, 186, 217, 28);
            Place(_lblScheduleStatus, 418, 222, 217, 34);

            Place(_sepSnooze, 20, 264, 613, 2);
            Place(_lblSnoozeHeader, 20, 270, 320, 17);
            Place(_cboSnooze, 20, 290, 150, 25);
            Place(_btnSnooze, 178, 289, 110, 28);
            Place(_lblSnoozeStatus, 296, 294, 300, 18);
        }

        private void BuildTaskFilters()
        {
            label13.Visible = false;
            label14.Visible = false;
            label15.Visible = false;

            var defs = new (string key, Ico ico, int filter)[]
            {
                ("tasks.all",     Ico.TaskList,  0),
                ("mode.idle",     Ico.Idle,      1),
                ("mode.timer",    Ico.Stopwatch, 2),
                ("mode.schedule", Ico.Calendar,  3),
            };

            var list = new List<Button>();
            int y = 46;
            foreach (var d in defs)
            {
                var btn = new Button
                {
                    Text      = Lang.T(d.key),
                    Location  = new Point(16, y),
                    Size      = new Size(140, 30),
                    FlatStyle = FlatStyle.Flat,
                    TabStop   = true,
                    Tag       = d.filter
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.Click += (s, e) => SetTaskFilter(d.filter);
                tabPage5.Controls.Add(btn);
                Reg(btn, d.key);
                list.Add(btn);
                y += 34;
            }
            _filterButtons = list.ToArray();
            SetTaskFilter(_taskListFilter);
        }

        private void SetTaskFilter(int filter)
        {
            _taskListFilter = filter;
            StyleFilterButtons();
            RefreshTaskGrid();
        }

        private void StyleFilterButtons()
        {
            var icons = new[] { Ico.TaskList, Ico.Idle, Ico.Stopwatch, Ico.Calendar };
            for (int i = 0; i < _filterButtons.Length; i++)
            {
                var btn = _filterButtons[i];
                bool active = (int)(btn.Tag ?? 0) == _taskListFilter;
                btn.BackColor = active
                    ? AccentColor
                    : (DarkUi ? Color.FromArgb(58, 58, 58) : Color.FromArgb(240, 240, 242));
                btn.ForeColor = active
                    ? Color.White
                    : (DarkUi ? Color.FromArgb(220, 220, 220) : Color.FromArgb(40, 40, 40));
                btn.FlatAppearance.BorderColor = active
                    ? AccentColor
                    : (DarkUi ? Color.FromArgb(90, 90, 90) : Color.FromArgb(190, 190, 195));
                btn.Font = new Font("Segoe UI", 9f, active ? FontStyle.Bold : FontStyle.Regular);
                SetIcon(btn, icons[i], 16, active ? Color.White : IconColor);
            }
        }

        private void BuildSettingsExtras(ToolTip tip)
        {

            AddSettingsSeparator(38);
            AddSettingsSeparator(366);
            AddSettingsSeparator(598);

            _chkCheckUpdates = new CheckBox
            {
                Text     = Lang.T("set.checkUpdates"),
                Location = new Point(24, 202),
                Size     = new Size(560, 21),
                Checked  = appSettings["checkUpdates"] != "0"
            };
            _chkCheckUpdates.CheckedChanged += (s, e) =>
            {
                if (_initializing) return;
                appSettings["checkUpdates"] = _chkCheckUpdates.Checked ? "1" : "0";
                SaveConfigFile();
            };
            groupBox1.Controls.Add(_chkCheckUpdates);
            Reg(_chkCheckUpdates, "set.checkUpdates");
            RegTip(tip, _chkCheckUpdates, "tip.checkUpdates");

            _lblLanguage = new Label
            {
                Text     = Lang.T("set.language"),
                Location = new Point(24, 382),
                AutoSize = true
            };
            groupBox1.Controls.Add(_lblLanguage);
            Reg(_lblLanguage, "set.language");

            _cboLanguage = new ComboBox
            {
                Location      = new Point(190, 379),
                Size          = new Size(220, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboLanguage.Items.AddRange(new object[] { "Polski", "English" });
            _cboLanguage.SelectedIndex = Lang.Current == Lang.EN ? 1 : 0;
            _cboLanguage.SelectedIndexChanged += (s, e) =>
            {
                string code = _cboLanguage.SelectedIndex == 1 ? Lang.EN : Lang.PL;
                if (code == Lang.Current) return;
                appSettings["language"] = code;
                SaveConfigFile();
                Lang.Set(code);
            };
            groupBox1.Controls.Add(_cboLanguage);
            RegTip(tip, _cboLanguage, "tip.language");

            var btnMail = new Button
            {
                Text      = Lang.T("mail.setupButton"),
                Location  = new Point(309, 94),
                Size      = new Size(285, 30),
                FlatStyle = FlatStyle.Flat
            };
            btnMail.Click += (s, e) => ShowMailSetupDialog();
            groupBox3.Controls.Add(btnMail);
            Reg(btnMail, "mail.setupButton");
            RegIcon(btnMail, Ico.Mail);

            _chkSoundEveryTask = new CheckBox
            {
                Text     = Lang.T("set.soundEveryTask"),
                Location = new Point(24, 228),
                Size     = new Size(560, 21),
                Checked  = appSettings["soundEveryTask"] == "1"
            };
            _chkSoundEveryTask.CheckedChanged += (s, e) =>
            {
                if (_initializing) return;
                appSettings["soundEveryTask"] = _chkSoundEveryTask.Checked ? "1" : "0";
                SaveConfigFile();
            };
            groupBox1.Controls.Add(_chkSoundEveryTask);
            Reg(_chkSoundEveryTask, "set.soundEveryTask");
            RegTip(tip, _chkSoundEveryTask, "tip.soundEveryTask");

            _chkHideTips = new CheckBox
            {
                Text     = Lang.T("set.hideTips"),
                Location = new Point(24, 440),
                Size     = new Size(560, 21),
                Checked  = appSettings["hideTips"] == "1"
            };
            _chkHideTips.CheckedChanged += (s, e) =>
            {
                if (_initializing) return;
                appSettings["hideTips"] = _chkHideTips.Checked ? "1" : "0";
                SaveConfigFile();
                ApplyTipVisibility();
            };
            groupBox1.Controls.Add(_chkHideTips);
            Reg(_chkHideTips, "set.hideTips");
            RegTip(tip, _chkHideTips, "tip.hideTips");

            groupBox1.Size     = new Size(groupBox1.Width, 668);
            groupBox3.Location = new Point(groupBox3.Left, 688);
        }

        private void ApplyTipVisibility()
        {
            bool show = appSettings["hideTips"] != "1";
            foreach (var tipLabel in new[] { tip01, tip02, tip03 })
            {
                if (tipLabel != null) tipLabel.Visible = show;
            }
        }

        private void AddSettingsSeparator(int y)
        {
            var line = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location    = new Point(16, y),
                Size        = new Size(578, 2),
                AutoSize    = false
            };
            groupBox1.Controls.Add(line);
        }

        private void ApplyLanguage()
        {
            foreach (var (c, key) in _i18n)
            {
                if (c is { IsDisposed: false }) c.Text = Lang.T(key);
            }
            foreach (var (c, tip, key) in _i18nTips)
            {
                if (c is { IsDisposed: false }) tip.SetToolTip(c, Lang.T(key));
            }

            this.Text        = Lang.T("msg.title");
            notifyIcon1.Text = Lang.T("msg.title");
            ApplyBranding();

            if (_tabActivator != null) _tabActivator.Text = Lang.T("tab.activator");
            tabPage5.Text = Lang.T("tab.tasks");
            tabPage3.Text = Lang.T("tab.settings");
            tabPage4.Text = Lang.T("tab.info");
            helpLink.Text = Lang.T("help.link");

            foreach (var combo in new[] { comboBox1, comboBox2, comboBox3 })
            {
                int sel = combo.SelectedIndex;
                combo.BeginUpdate();
                combo.Items.Clear();
                combo.Items.AddRange(Lang.A("actions").Cast<object>().ToArray());
                combo.EndUpdate();
                if (sel >= 0 && sel < combo.Items.Count) combo.SelectedIndex = sel;
                else if (combo.Items.Count > 0) combo.SelectedIndex = 0;
            }

            var checkedDays = checkedListBox1.CheckedIndices.Cast<int>().ToArray();
            checkedListBox1.BeginUpdate();
            checkedListBox1.Items.Clear();
            checkedListBox1.Items.AddRange(Lang.A("scheduleDays").Cast<object>().ToArray());
            foreach (int i in checkedDays)
                if (i < checkedListBox1.Items.Count) checkedListBox1.SetItemChecked(i, true);
            checkedListBox1.EndUpdate();

            if (_cboSnooze != null)
            {
                int sel = _cboSnooze.SelectedIndex;
                _cboSnooze.Items.Clear();
                _cboSnooze.Items.AddRange(Lang.A("snoozeItems").Cast<object>().ToArray());
                _cboSnooze.SelectedIndex = sel >= 0 && sel < _cboSnooze.Items.Count ? sel : 0;
            }

            if (dataGridView1.Columns.Count >= 4)
            {
                dataGridView1.Columns[0].HeaderText = Lang.T("col.task");
                dataGridView1.Columns[1].HeaderText = Lang.T("col.remaining");
                dataGridView1.Columns[2].HeaderText = Lang.T("col.state");
                dataGridView1.Columns[3].HeaderText = Lang.T("col.queue");
            }

            label8.Text      = Lang.T("info.by") + "\r\n\r\n" + Lang.T("info.support")
                             + "\r\n\r\nhttps://suppi.pl/jmsoft\r\n\r\n" + Lang.T("info.version", AppVersion);
            label10.Text     = HelpContent.License();

            textBoxInfo.Text = HelpContent.Changelog()
                .Replace("\r\n", "\n").Replace("\n", "\r\n");
            textBoxInfo.SelectionStart = 0;

            if (_menuExit        != null) _menuExit.Text        = Lang.T("tray.exit");
            if (_menuStopIdle    != null) _menuStopIdle.Text    = Lang.T("tray.stopIdle");
            if (_menuStopTimer   != null) _menuStopTimer.Text   = Lang.T("tray.stopTimer");
            if (_menuResumeIdle  != null) _menuResumeIdle.Text  = Lang.T("tray.resumeIdle");
            if (_menuResumeTimer != null) _menuResumeTimer.Text = Lang.T("tray.resumeTimer");
            if (_menuSettings    != null) _menuSettings.Text    = Lang.T("tray.settings");
            notifyIcon1.Text = Lang.T("msg.title");

            SetActivatorMode(_activatorMode, save: false);
            SetTimerInputMode(_timerDurationMode, save: false);
            if (_lblDurationCaption != null) _lblDurationCaption.Text = Lang.T("timer.duration");
            if (_lblDurationHint != null)    _lblDurationHint.Text    = Lang.T("timer.maxDuration");
            ApplyTipVisibility();
            ApplyHarmonogramUI(appSettings["timer3UserPaused"] != "1");
            UpdateComboHints(comboBox1, tip01, _btnActSettings1);
            UpdateComboHints(comboBox2, tip02, _btnActSettings2);
            UpdateComboHints(comboBox3, tip03, _btnActSettings3);
            RefreshTaskGrid();
            if (_cboLanguage != null)
                _cboLanguage.SelectedIndex = Lang.Current == Lang.EN ? 1 : 0;
        }

        private Image? _logoScaled;

        private void ApplyBranding()
        {

            try
            {
                using var src = Lang.Current == Lang.EN
                    ? TimerSys.Properties.Resources.logo_en
                    : TimerSys.Properties.Resources.logo;

                int h = pictureBox5.Height;
                int w = Math.Max(1, (int)Math.Round(src.Width * (h / (double)src.Height)));

                var scaled = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode   = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);
                    g.DrawImage(src, new Rectangle(0, 0, w, h));
                }

                pictureBox5.BackColor = Color.Transparent;
                pictureBox5.Image     = scaled;
                _logoScaled?.Dispose();
                _logoScaled = scaled;
            }
            catch { }

            try
            {
                label19.Image = Lang.Current == Lang.EN
                    ? TimerSys.Properties.Resources.block_en
                    : TimerSys.Properties.Resources.block;
            }
            catch { }
        }

        private string SoundDisplayFor(int activator)
        {
            string v = activator > 0 ? (appSettings[$"act{activator}_sound"] ?? "").Trim() : "";
            if (string.IsNullOrEmpty(v)) v = (appSettings["soundFile"] ?? "").Trim();
            if (string.IsNullOrEmpty(v)) v = SoundCatalog.DefaultFile;
            string name = SoundCatalog.DisplayName(v);
            return string.IsNullOrEmpty(name) ? System.IO.Path.GetFileName(v) : name;
        }

        private void ApplyIcons()
        {
            Color c = IconColor;

            _tabIcons ??= new ImageList { ImageSize = new Size(18, 18), ColorDepth = ColorDepth.Depth32Bit };
            _tabIcons.Images.Clear();
            _tabIcons.Images.Add("act",      VectorIcons.Get(Ico.Stopwatch, 18, c));
            _tabIcons.Images.Add("tasks",    VectorIcons.Get(Ico.TaskList,  18, c));
            _tabIcons.Images.Add("settings", VectorIcons.Get(Ico.Settings,  18, c));
            _tabIcons.Images.Add("info",     VectorIcons.Get(Ico.Info,      18, c));
            tabControl1.ImageList = _tabIcons;
            if (_tabActivator != null) _tabActivator.ImageIndex = 0;
            tabPage5.ImageIndex = 1;
            tabPage3.ImageIndex = 2;
            tabPage4.ImageIndex = 3;

            SetIcon(button2, Ico.Plus);
            SetIcon(button4, Ico.Plus);
            SetIcon(button8, Ico.Plus);
            SetIcon(button12, Ico.Toggle);
            SetIcon(button11, Ico.Pencil);
            SetIcon(button10, Ico.Trash);
            SetIcon(button5, Ico.Pencil);
            SetIcon(button7, Ico.Refresh);
            SetIcon(button13, Ico.Save);
            SetIcon(button6, Ico.Key);
            if (_btnSnooze != null) SetIcon(_btnSnooze, Ico.Play);
            SetIcon(stopStartHarmonogram,
                appSettings["timer3UserPaused"] == "1" ? Ico.Play : Ico.Stop);

            foreach (var b in new[] { _btnActSettings1, _btnActSettings2, _btnActSettings3 })
                if (b != null) { b.Text = ""; SetIcon(b, Ico.Gear, 16); }

            foreach (var b in _extraIconButtons)
                if (b.btn is { IsDisposed: false }) SetIcon(b.btn, b.ico, b.size);

            StyleModeButtons();
            StyleFilterButtons();
            StyleTabControl(DarkUi);

        }

        private void StyleTabControl(bool dark)
        {
            tabControl1.DrawItem -= TabControl1_DrawItem;
            if (dark)
            {
                tabControl1.Appearance = TabAppearance.FlatButtons;
                tabControl1.DrawMode   = TabDrawMode.OwnerDrawFixed;

                tabControl1.SizeMode   = TabSizeMode.Fixed;
                tabControl1.ItemSize   = new Size(166, 30);
                tabControl1.DrawItem  += TabControl1_DrawItem;
            }
            else
            {
                tabControl1.DrawMode   = TabDrawMode.Normal;
                tabControl1.Appearance = TabAppearance.Normal;
                tabControl1.SizeMode   = TabSizeMode.Normal;
                tabControl1.ItemSize   = new Size(0, 26);
            }
            tabControl1.Invalidate();
        }

        private void TabControl1_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= tabControl1.TabPages.Count) return;
            var page     = tabControl1.TabPages[e.Index];
            bool active  = e.Index == tabControl1.SelectedIndex;
            var bounds   = e.Bounds;

            using (var back = new SolidBrush(active ? Color.FromArgb(52, 52, 56) : Color.FromArgb(34, 34, 36)))
                e.Graphics.FillRectangle(back, bounds);
            if (active)
            {
                using var accent = new SolidBrush(AccentColor);
                e.Graphics.FillRectangle(accent, bounds.Left, bounds.Bottom - 3, bounds.Width, 3);
            }

            int x = bounds.Left + 12;
            if (_tabIcons != null && page.ImageIndex >= 0 && page.ImageIndex < _tabIcons.Images.Count)
            {
                var img = _tabIcons.Images[page.ImageIndex];
                e.Graphics.DrawImage(img, x, bounds.Top + (bounds.Height - img.Height) / 2, img.Width, img.Height);
                x += img.Width + 6;
            }

            TextRenderer.DrawText(e.Graphics, page.Text, tabControl1.Font,
                new Rectangle(x, bounds.Top, bounds.Right - x - 8, bounds.Height),
                active ? Color.White : Color.FromArgb(185, 185, 190),
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }

        private readonly List<(Button btn, Ico ico, int size)> _extraIconButtons = new();

        private void RegIcon(Button btn, Ico ico, int size = 18)
        {
            _extraIconButtons.Add((btn, ico, size));
            SetIcon(btn, ico, size);
        }

        internal static string AppVersion
        {
            get
            {
                try
                {
                    var asm = System.Reflection.Assembly.GetExecutingAssembly();
                    var v = asm.GetName().Version;
                    return v == null ? "1.0.7" : $"{v.Major}.{v.Minor}.{v.Build}";
                }
                catch { return "1.0.7"; }
            }
        }

        private void CheckForUpdate(bool announceAtStartup)
        {
            if (announceAtStartup && appSettings["checkUpdates"] == "0") return;

            System.Threading.Tasks.Task.Run(async () =>
            {
                string? latestText = null;
                try
                {
                    using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    client.DefaultRequestHeaders.Add("User-Agent", "TimerSys");
                    var response = await client
                        .GetAsync("https://api.github.com/repos/Mattronix7200/TimerSysApp/tags")
                        .ConfigureAwait(false);
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var tags = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GitHubTag>>(body);

                    var latest = (tags ?? new List<GitHubTag>())
                        .Select(t => t.Name ?? "")
                        .Where(n => System.Text.RegularExpressions.Regex.IsMatch(n, @"^release-\d+\.\d+\.\d+$"))
                        .Select(n => Version.TryParse(n.Replace("release-", ""), out var v) ? v : null)
                        .Where(v => v != null)
                        .OrderByDescending(v => v)
                        .FirstOrDefault();

                    if (latest != null) latestText = latest.ToString(3);
                }
                catch {  }

                try
                {
                    BeginInvoke(new Action(() => OnUpdateChecked(latestText, announceAtStartup)));
                }
                catch { }
            });
        }

        private void OnUpdateChecked(string? latestText, bool announceAtStartup)
        {
            var current = Version.TryParse(AppVersion, out var cv) ? cv : new Version(0, 0, 0);
            bool isNewer = Version.TryParse(latestText ?? "", out var lv) && lv > current;

            label20.Visible = label20.Enabled = isNewer;
            label23.Visible = label23.Enabled = !isNewer;

            if (!isNewer || !announceAtStartup) return;
            if (string.Equals(appSettings["skipVersion"], latestText, StringComparison.Ordinal)) return;

            ShowUpdateDialog(latestText!);
        }

        private void ShowUpdateDialog(string latest)
        {
            bool dark = DarkUi;
            using var dlg = new Form
            {
                Text            = Lang.T("upd.title"),
                ClientSize      = new Size(470, 210),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition   = StartPosition == FormStartPosition.CenterScreen && !Visible
                                    ? FormStartPosition.CenterScreen
                                    : FormStartPosition.CenterParent,
                MaximizeBox     = false,
                MinimizeBox     = false,
                ShowInTaskbar   = false,
                Icon            = this.Icon,
                BackColor       = this.BackColor
            };

            var pic = new PictureBox
            {
                Image    = VectorIcons.Get(Ico.Download, 48, AccentColor),
                Location = new Point(20, 24),
                Size     = new Size(48, 48),
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            dlg.Controls.Add(pic);

            dlg.Controls.Add(new Label
            {
                Text     = Lang.T("upd.question", latest, AppVersion),
                Location = new Point(84, 20),
                Size     = new Size(366, 90),
                AutoSize = false
            });

            var chkSkip = new CheckBox
            {
                Text     = Lang.T("upd.skip"),
                Location = new Point(84, 112),
                Size     = new Size(360, 21)
            };
            dlg.Controls.Add(chkSkip);

            var btnOpen = new Button
            {
                Text         = Lang.T("upd.open"),
                Location     = new Point(190, 152),
                Size         = new Size(130, 32),
                FlatStyle    = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            SetIcon(btnOpen, Ico.Globe);
            var btnLater = new Button
            {
                Text         = Lang.T("upd.later"),
                Location     = new Point(330, 152),
                Size         = new Size(120, 32),
                FlatStyle    = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            dlg.Controls.Add(btnOpen);
            dlg.Controls.Add(btnLater);
            dlg.AcceptButton = btnOpen;
            dlg.CancelButton = btnLater;

            if (dark) ApplyThemeToControls(dlg.Controls,
                Color.FromArgb(30, 30, 30), Color.FromArgb(220, 220, 220), Color.FromArgb(55, 55, 55), true);

            var result = dlg.ShowDialog(Visible ? this : null);

            if (chkSkip.Checked)
            {
                appSettings["skipVersion"] = latest;
                SaveConfigFile();
            }
            if (result == DialogResult.OK)
                TryStartProcess(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = "https://github.com/Mattronix7200/TimerSysApp/releases",
                    UseShellExecute = true
                });
        }
    }
}
