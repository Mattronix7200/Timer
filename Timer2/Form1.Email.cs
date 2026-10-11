namespace Timer2
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using TimerSys;

    public partial class Form1
    {

        internal const string ActionEmail = "Wyślij wiadomość e-mail";

        private void ShowMailSetupDialog(IWin32Window? owner = null)
        {
            var cfg = SmtpSettings.Load(appSettings);
            bool dark = DarkUi;

            using var dlg = new Form
            {
                Text            = Lang.T("mail.setupTitle"),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition   = FormStartPosition.CenterParent,
                MaximizeBox     = false,
                MinimizeBox     = false,
                ShowInTaskbar   = false,
                Icon            = this.Icon,
                BackColor       = this.BackColor
            };

            const int labelX = 14, fieldX = 170, fieldW = 356, dlgW = 540;
            int y = 12;

            void Hint(string key, int height)
            {
                dlg.Controls.Add(new Label
                { Text = Lang.T(key), Location = new Point(labelX, y), Size = new Size(dlgW - 28, height), AutoSize = false });
                y += height + 6;
            }

            Control Row(string labelKey, Control ctrl, int height = 25)
            {
                dlg.Controls.Add(new Label
                { Text = Lang.T(labelKey), Location = new Point(labelX, y + 4), Size = new Size(fieldX - labelX - 6, 17), AutoSize = false });
                ctrl.Location = new Point(fieldX, y);
                if (ctrl.Width <= 0 || ctrl is TextBox or ComboBox) ctrl.Size = new Size(fieldW, height);
                dlg.Controls.Add(ctrl);
                y += height + 9;
                return ctrl;
            }

            Hint("mail.setupHint", 34);

            var txtHost = new TextBox { Text = cfg.Host };
            Row("mail.host", txtHost);

            var nudPort = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = cfg.Port, Width = 100 };
            dlg.Controls.Add(new Label
            { Text = Lang.T("mail.port"), Location = new Point(labelX, y + 4), Size = new Size(fieldX - labelX - 6, 17), AutoSize = false });
            nudPort.Location = new Point(fieldX, y);
            nudPort.Size = new Size(100, 25);
            dlg.Controls.Add(nudPort);
            y += 34;

            var cboSecurity = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cboSecurity.Items.AddRange(new object[]
            { Lang.T("mail.secAuto"), Lang.T("mail.secSsl"), Lang.T("mail.secStartTls"), Lang.T("mail.secNone") });
            cboSecurity.SelectedIndex = (int)cfg.Security;
            Row("mail.security", cboSecurity);

            var presetMenu = new ContextMenuStrip();
            foreach (var preset in MailProviders.All)
            {
                var item = preset;
                presetMenu.Items.Add($"{item.Name}  ({item.AttachmentLimitMB} MB)", null, (s, ev) =>
                {
                    txtHost.Text = item.Host;
                    nudPort.Value = item.Port;
                    cboSecurity.SelectedIndex = (int)item.Security;
                });
            }
            var btnPresets = new Button
            {
                Text      = Lang.T("mail.presets"),
                Location  = new Point(fieldX, y),
                Size      = new Size(200, 26),
                FlatStyle = FlatStyle.Flat
            };
            SetIcon(btnPresets, Ico.ChevronDown, 14);
            btnPresets.Click += (s, ev) => presetMenu.Show(btnPresets, new Point(0, btnPresets.Height));
            dlg.Controls.Add(btnPresets);

            var btnAppPassword = new Button
            {
                Text      = Lang.T("mail.appPassword"),
                Location  = new Point(fieldX + 208, y),
                Size      = new Size(148, 26),
                FlatStyle = FlatStyle.Flat,
                Visible   = false
            };
            SetIcon(btnAppPassword, Ico.Key, 14);
            btnAppPassword.Click += (s, ev) =>
            {
                string? url = MailProviders.AppPasswordUrlFor(txtHost.Text);
                if (!string.IsNullOrEmpty(url))
                    TryStartProcess(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true });
            };
            dlg.Controls.Add(btnAppPassword);

            var presetTip = new ToolTip();
            presetTip.SetToolTip(btnPresets, Lang.T("mail.presetsTip"));
            presetTip.SetToolTip(btnAppPassword, Lang.T("mail.appPasswordTip"));

            void SyncProvider() =>
                btnAppPassword.Visible = MailProviders.AppPasswordUrlFor(txtHost.Text) != null;
            txtHost.TextChanged += (s, ev) => SyncProvider();
            SyncProvider();
            y += 36;

            var cboAuth = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cboAuth.Items.AddRange(new object[] { Lang.T("mail.authPassword"), Lang.T("mail.authOAuth") });
            cboAuth.SelectedIndex = (int)cfg.Auth;
            Row("mail.authMode", cboAuth);

            var txtUser = new TextBox { Text = cfg.User };
            Row("mail.user", txtUser);

            int pwdTop = y;
            var lblPass = new Label
            { Text = Lang.T("mail.password"), Location = new Point(labelX, y + 4), Size = new Size(fieldX - labelX - 6, 17), AutoSize = false };
            var txtPass = new TextBox
            { Text = cfg.Password, Location = new Point(fieldX, y), Size = new Size(fieldW, 25), UseSystemPasswordChar = true };
            dlg.Controls.Add(lblPass);
            dlg.Controls.Add(txtPass);

            var oauthPanel = new Panel
            { Location = new Point(0, pwdTop), Size = new Size(dlgW, 170), Visible = false };
            dlg.Controls.Add(oauthPanel);

            int oy = 0;
            TextBox OauthRow(string labelKey, string value, bool secret = false)
            {
                oauthPanel.Controls.Add(new Label
                { Text = Lang.T(labelKey), Location = new Point(labelX, oy + 4), Size = new Size(fieldX - labelX - 6, 17), AutoSize = false });
                var box = new TextBox
                { Text = value, Location = new Point(fieldX, oy), Size = new Size(fieldW, 25), UseSystemPasswordChar = secret };
                oauthPanel.Controls.Add(box);
                oy += 34;
                return box;
            }

            var txtClient  = OauthRow("mail.oauthClient", cfg.OAuthClientId);
            var txtSecret  = OauthRow("mail.oauthSecret", cfg.OAuthClientSecret, secret: true);
            var txtRefresh = OauthRow("mail.oauthRefresh", cfg.OAuthRefreshToken, secret: true);
            var txtTokenUrl = OauthRow("mail.oauthUrl", cfg.OAuthTokenUrl);
            oauthPanel.Controls.Add(new Label
            { Text = Lang.T("mail.oauthHint"), Location = new Point(labelX, oy), Size = new Size(dlgW - 28, 32), AutoSize = false });
            oauthPanel.Size = new Size(dlgW, oy + 36);

            y = pwdTop + 34;
            int afterAuthY = y;

            var txtFrom = new TextBox { Text = cfg.From };
            var txtFromName = new TextBox { Text = cfg.FromName };
            var lblFrom = new Label
            { Text = Lang.T("mail.from"), Size = new Size(fieldX - labelX - 6, 17), AutoSize = false };
            var lblFromName = new Label
            { Text = Lang.T("mail.fromName"), Size = new Size(fieldX - labelX - 6, 17), AutoSize = false };
            foreach (var c in new Control[] { lblFrom, txtFrom, lblFromName, txtFromName }) dlg.Controls.Add(c);

            var btnTest = new Button { Text = Lang.T("mail.test"), Size = new Size(220, 30), FlatStyle = FlatStyle.Flat };
            SetIcon(btnTest, Ico.Mail, 16);
            var btnSave = new Button { Text = Lang.T("dlg.save"), Size = new Size(96, 30), FlatStyle = FlatStyle.Flat };
            var btnCancel = new Button
            { Text = Lang.T("dlg.cancel"), Size = new Size(96, 30), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            foreach (var c in new Control[] { btnTest, btnSave, btnCancel }) dlg.Controls.Add(c);

            void LayoutAuth()
            {
                bool oauth = cboAuth.SelectedIndex == 1;
                lblPass.Visible = txtPass.Visible = !oauth;
                oauthPanel.Visible = oauth;

                int baseY = oauth ? pwdTop + oauthPanel.Height + 6 : afterAuthY;
                lblFrom.Location     = new Point(labelX, baseY + 4);
                txtFrom.Location     = new Point(fieldX, baseY);
                txtFrom.Size         = new Size(fieldW, 25);
                lblFromName.Location = new Point(labelX, baseY + 38);
                txtFromName.Location = new Point(fieldX, baseY + 34);
                txtFromName.Size     = new Size(fieldW, 25);

                int btnY = baseY + 76;
                btnTest.Location   = new Point(labelX, btnY);
                btnSave.Location   = new Point(dlgW - 212, btnY);
                btnCancel.Location = new Point(dlgW - 110, btnY);
                dlg.ClientSize     = new Size(dlgW, btnY + 44);
            }

            cboAuth.SelectedIndexChanged += (s, ev) => LayoutAuth();
            LayoutAuth();

            SmtpSettings Current() => new()
            {
                Host     = txtHost.Text.Trim(),
                Port     = (int)nudPort.Value,
                Security = (SmtpSecurity)cboSecurity.SelectedIndex,
                Auth     = (SmtpAuth)cboAuth.SelectedIndex,
                User     = txtUser.Text.Trim(),
                Password = txtPass.Text,
                From     = txtFrom.Text.Trim(),
                FromName = txtFromName.Text.Trim(),
                OAuthClientId     = txtClient.Text.Trim(),
                OAuthClientSecret = txtSecret.Text,
                OAuthRefreshToken = txtRefresh.Text.Trim(),
                OAuthTokenUrl     = txtTokenUrl.Text.Trim()
            };

            btnTest.Click += (s, ev) => SendTestMail(dlg, Current(), txtFrom.Text.Trim());
            btnSave.Click += (s, ev) =>
            {
                Current().Save(appSettings);
                SaveConfigFile();
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };
            dlg.AcceptButton = btnSave;
            dlg.CancelButton = btnCancel;

            if (dark) ApplyThemeToControls(dlg.Controls,
                Color.FromArgb(30, 30, 30), Color.FromArgb(220, 220, 220), Color.FromArgb(55, 55, 55), true);
            dlg.ShowDialog(owner ?? this);
        }

        private void SendTestMail(IWin32Window owner, SmtpSettings cfg, string to)
        {
            var mail = EmailSender.Compose(MailTemplate.Html, Lang.T("mail.test"), "", "", "", Lang.T("mail.test"));
            string? error = EmailSender.Send(cfg, to, mail);
            if (error == null)
                MessageBox.Show(owner, Lang.T("mail.testOk"), Lang.T("msg.title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show(owner, Lang.T("mail.testFail", MailProviders.Describe(error, cfg.Host)), Lang.T("msg.error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ShowEmailActionDialog(int activatorNum)
        {
            if (activatorNum <= 0) activatorNum = 1;
            string key = $"act{activatorNum}";
            bool dark = DarkUi;

            using var dlg = new Form
            {
                Text            = Lang.T("dlg.actionSettings", Lang.A("actions").LastOrDefault() ?? ActionEmail),
                ClientSize      = new Size(540, 430),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition   = FormStartPosition.CenterParent,
                MaximizeBox     = false,
                MinimizeBox     = false,
                ShowInTaskbar   = false,
                Icon            = this.Icon,
                BackColor       = this.BackColor
            };

            var btnSetup = new Button
            {
                Text      = Lang.T("mail.setupButton"),
                Location  = new Point(14, 12),
                Size      = new Size(240, 30),
                FlatStyle = FlatStyle.Flat
            };
            SetIcon(btnSetup, Ico.Settings, 16);
            btnSetup.Click += (s, ev) => ShowMailSetupDialog(dlg);
            dlg.Controls.Add(btnSetup);

            int y = 54;
            void Lab(string textKey)
            {
                dlg.Controls.Add(new Label
                { Text = Lang.T(textKey), Location = new Point(14, y), Size = new Size(512, 17), AutoSize = false });
                y += 20;
            }

            Lab("mail.to");
            var txtTo = new TextBox { Text = appSettings[$"{key}_mail_to"] ?? "", Location = new Point(14, y), Size = new Size(512, 25) };
            dlg.Controls.Add(txtTo);
            y += 34;

            Lab("mail.subject");
            var txtSubject = new TextBox { Text = appSettings[$"{key}_mail_subject"] ?? "", Location = new Point(14, y), Size = new Size(512, 25) };
            dlg.Controls.Add(txtSubject);
            y += 34;

            Lab("mail.template");
            var cboTemplate = new ComboBox
            { Location = new Point(14, y), Size = new Size(512, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboTemplate.Items.AddRange(Lang.A("mailTemplates"));
            cboTemplate.SelectedIndex = TemplateIndex(appSettings[$"{key}_mail_template"]);
            dlg.Controls.Add(cboTemplate);
            y += 36;

            var lblDetail = new Label { Location = new Point(14, y), Size = new Size(512, 17), AutoSize = false };
            dlg.Controls.Add(lblDetail);
            y += 20;

            var txtDetail = new TextBox
            {
                Location   = new Point(14, y),
                Size       = new Size(470, 120),
                Multiline  = true,
                ScrollBars = ScrollBars.Vertical
            };
            dlg.Controls.Add(txtDetail);

            var btnBrowse = new Button
            { Text = "", Location = new Point(492, y), Size = new Size(34, 26), FlatStyle = FlatStyle.Flat };
            SetIcon(btnBrowse, Ico.Folder, 16);
            btnBrowse.Click += (s, ev) =>
            {
                using var ofd = new OpenFileDialog { Title = Lang.T("mail.attachment") };
                if (ofd.ShowDialog(dlg) != DialogResult.OK) return;
                txtDetail.Text = ofd.FileName;
                WarnIfAttachmentTooBig(dlg, ofd.FileName);
            };
            dlg.Controls.Add(btnBrowse);
            y += 130;

            void SyncDetail()
            {
                var t = (MailTemplate)cboTemplate.SelectedIndex;
                switch (t)
                {
                    case MailTemplate.Attachment:
                        lblDetail.Text = Lang.T("mail.attachment");
                        txtDetail.Multiline = false;
                        txtDetail.Size = new Size(470, 25);
                        txtDetail.Text = appSettings[$"{key}_mail_attachment"] ?? "";
                        btnBrowse.Visible = true;
                        txtDetail.Enabled = true;
                        break;
                    case MailTemplate.Command:
                        lblDetail.Text = Lang.T("mail.command");
                        txtDetail.Multiline = false;
                        txtDetail.Size = new Size(512, 25);
                        txtDetail.Text = appSettings[$"{key}_mail_command"] ?? "";
                        btnBrowse.Visible = false;
                        txtDetail.Enabled = true;
                        break;
                    case MailTemplate.Html:
                    case MailTemplate.Custom:
                        lblDetail.Text = t == MailTemplate.Html
                            ? Lang.T("mail.text") + "   " + Lang.T("mail.htmlNote")
                            : Lang.T("mail.text");
                        txtDetail.Multiline = true;
                        txtDetail.Size = new Size(512, 120);
                        txtDetail.Text = appSettings[$"{key}_mail_body"] ?? "";
                        btnBrowse.Visible = false;
                        txtDetail.Enabled = true;
                        break;
                    default:
                        lblDetail.Text = Lang.T("mail.text");
                        txtDetail.Multiline = true;
                        txtDetail.Size = new Size(512, 120);
                        txtDetail.Text = "";
                        txtDetail.Enabled = false;
                        btnBrowse.Visible = false;
                        break;
                }
            }

            void StoreDetail()
            {
                var t = (MailTemplate)cboTemplate.SelectedIndex;
                switch (t)
                {
                    case MailTemplate.Attachment: appSettings[$"{key}_mail_attachment"] = txtDetail.Text.Trim(); break;
                    case MailTemplate.Command:    appSettings[$"{key}_mail_command"]    = txtDetail.Text.Trim(); break;
                    case MailTemplate.Html:
                    case MailTemplate.Custom:     appSettings[$"{key}_mail_body"]       = txtDetail.Text; break;
                }
            }

            cboTemplate.SelectedIndexChanged += (s, ev) => SyncDetail();
            SyncDetail();

            var btnTest = new Button
            { Text = Lang.T("mail.test"), Location = new Point(14, y), Size = new Size(210, 30), FlatStyle = FlatStyle.Flat };
            SetIcon(btnTest, Ico.Mail, 16);
            btnTest.Click += (s, ev) =>
            {
                StoreDetail();
                var mail = EmailSender.Compose(
                    (MailTemplate)cboTemplate.SelectedIndex,
                    txtSubject.Text.Trim(),
                    appSettings[$"{key}_mail_body"] ?? "",
                    appSettings[$"{key}_mail_attachment"] ?? "",
                    appSettings[$"{key}_mail_command"] ?? "",
                    ActionDisplay(ActionEmail));
                var sendCfg = SmtpSettings.Load(appSettings);
                string? error = EmailSender.Send(sendCfg, txtTo.Text.Trim(), mail);
                if (error == null)
                    MessageBox.Show(dlg, Lang.T("mail.testOk"), Lang.T("msg.title"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show(dlg, Lang.T("mail.testFail", MailProviders.Describe(error, sendCfg.Host)), Lang.T("msg.error"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            dlg.Controls.Add(btnTest);

            var btnSave = new Button
            { Text = Lang.T("dlg.save"), Location = new Point(326, y), Size = new Size(96, 30), FlatStyle = FlatStyle.Flat };
            var btnCancel = new Button
            { Text = Lang.T("dlg.cancel"), Location = new Point(430, y), Size = new Size(96, 30), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            btnSave.Click += (s, ev) =>
            {
                appSettings[$"{key}_mail_to"]       = txtTo.Text.Trim();
                appSettings[$"{key}_mail_subject"]  = txtSubject.Text.Trim();
                appSettings[$"{key}_mail_template"] = ((MailTemplate)cboTemplate.SelectedIndex).ToString();
                StoreDetail();
                if ((MailTemplate)cboTemplate.SelectedIndex is MailTemplate.Attachment or MailTemplate.Html)
                    WarnIfAttachmentTooBig(dlg, appSettings[$"{key}_mail_attachment"] ?? "");
                SaveConfigFile();
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };
            dlg.Controls.Add(btnSave);
            dlg.Controls.Add(btnCancel);
            dlg.AcceptButton = btnSave;
            dlg.CancelButton = btnCancel;
            dlg.ClientSize = new Size(540, y + 46);

            if (dark) ApplyThemeToControls(dlg.Controls,
                Color.FromArgb(30, 30, 30), Color.FromArgb(220, 220, 220), Color.FromArgb(55, 55, 55), true);
            dlg.ShowDialog(this);
        }

        private void WarnIfAttachmentTooBig(IWin32Window owner, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                var info = new System.IO.FileInfo(path);
                if (!info.Exists) return;

                string host     = appSettings["smtp_host"] ?? "";
                var provider    = MailProviders.Match(host);
                int limitMB     = MailProviders.LimitMB(host);
                long limitBytes = (long)limitMB * 1024 * 1024;
                if (info.Length <= limitBytes) return;

                MessageBox.Show(owner,
                    Lang.T("mail.sizeWarn",
                        provider?.Name ?? Lang.T("mail.unknownProvider"),
                        limitMB + " MB",
                        FormatBytes(info.Length)),
                    Lang.T("mail.sizeWarnTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch { }
        }

        private static string FormatBytes(long bytes)
        {
            double mb = bytes / 1024d / 1024d;
            return mb >= 1 ? $"{mb:0.0} MB" : $"{bytes / 1024d:0} KB";
        }

        private static int TemplateIndex(string? stored) =>
            Enum.TryParse<MailTemplate>(stored, out var t) ? (int)t : (int)MailTemplate.ShortText;

        private void SendConfiguredEmail(int activator)
        {
            int n = activator > 0 ? activator : 1;
            string key = $"act{n}";

            var cfg      = SmtpSettings.Load(appSettings);
            var template = (MailTemplate)TemplateIndex(appSettings[$"{key}_mail_template"]);
            string to    = (appSettings[$"{key}_mail_to"] ?? "").Trim();
            string subj  = (appSettings[$"{key}_mail_subject"] ?? "").Trim();
            string body  = appSettings[$"{key}_mail_body"] ?? "";
            string att   = (appSettings[$"{key}_mail_attachment"] ?? "").Trim();
            string cmd   = (appSettings[$"{key}_mail_command"] ?? "").Trim();
            string name  = ActionDisplay(ActionEmail);

            System.Threading.Tasks.Task.Run(() =>
            {
                string? error;
                try
                {
                    var mail = EmailSender.Compose(template, subj, body, att, cmd, name);
                    error = EmailSender.Send(cfg, to, mail);
                }
                catch (Exception ex) { error = ex.Message; }

                if (error == null) return;
                try
                {
                    BeginInvoke(new Action(() => MessageBox.Show(
                        Lang.T("mail.sendFailed", MailProviders.Describe(error, cfg.Host)), Lang.T("msg.title"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning)));
                }
                catch { }
            });
        }
    }
}
