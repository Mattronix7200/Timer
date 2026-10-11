using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

    public sealed class SmtpSetupDialog : ThemedWindow
    {
        private readonly AppEngine _engine;

        public SmtpSetupDialog(AppEngine engine)
        {
            _engine = engine;
            Title = Lang.T("mail.setupTitle");
            Width = 620;

            var cfg = SmtpSettings.Load(engine.Settings);
            AddHint(Lang.T("mail.setupHint"));

            AddLabel(Lang.T("mail.host"), bold: true);
            var host = AddTextBox(cfg.Host);

            var serverRow = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            serverRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
            serverRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            serverRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            serverRow.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            serverRow.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            void Caption(string text, int column)
            {
                var label = new TextBlock { Text = text, Margin = new Thickness(0, 0, 10, 4) };
                Grid.SetColumn(label, column);
                serverRow.Children.Add(label);
            }
            Caption(Lang.T("mail.port"), 0);
            Caption(Lang.T("mail.security"), 1);

            var port = new TextBox { Text = cfg.Port.ToString(), Margin = new Thickness(0, 0, 10, 0) };
            Grid.SetRow(port, 1);
            serverRow.Children.Add(port);

            var security = new ComboBox { Margin = new Thickness(0, 0, 10, 0) };
            foreach (var name in new[]
                     { Lang.T("mail.secAuto"), Lang.T("mail.secSsl"), Lang.T("mail.secStartTls"), Lang.T("mail.secNone") })
                security.Items.Add(name);
            security.SelectedIndex = Math.Max(0, Math.Min((int)cfg.Security, security.Items.Count - 1));
            Grid.SetRow(security, 1);
            Grid.SetColumn(security, 1);
            serverRow.Children.Add(security);

            var presets = new Button
            {
                Content = MainWindow.BuildIconLabel(Ico.ChevronDown, Lang.T("mail.presets"), 14),
                MinWidth = 170,
                ToolTip = Lang.T("mail.presetsTip")
            };
            presets.SetResourceReference(StyleProperty, "FlatButton");
            Grid.SetRow(presets, 1);
            Grid.SetColumn(presets, 2);
            serverRow.Children.Add(presets);
            Body.Children.Add(serverRow);

            var menu = new ContextMenu();
            foreach (var provider in MailProviders.All)
            {
                var pv = provider;
                var item = new MenuItem { Header = $"{pv.Name}  ({pv.AttachmentLimitMB} MB)" };
                item.Click += (s, e) =>
                {
                    host.Text = pv.Host;
                    port.Text = pv.Port.ToString();
                    security.SelectedIndex = (int)pv.Security;
                };
                menu.Items.Add(item);
            }
            presets.Click += (s, e) => { menu.PlacementTarget = presets; menu.IsOpen = true; };

            var appPassword = new Button
            {
                Content = MainWindow.BuildIconLabel(Ico.Key, Lang.T("mail.appPassword"), 14),
                MinWidth = 190,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Visibility = Visibility.Collapsed,
                ToolTip = Lang.T("mail.appPasswordTip")
            };
            appPassword.SetResourceReference(StyleProperty, "FlatButton");
            appPassword.Click += (s, e) =>
            {
                string? url = MailProviders.AppPasswordUrlFor(host.Text);
                if (!string.IsNullOrEmpty(url)) OpenUrl(url);
            };
            Body.Children.Add(appPassword);

            AddLabel(Lang.T("mail.authMode"));
            var auth = AddCombo(new[] { Lang.T("mail.authPassword"), Lang.T("mail.authOAuth") }, (int)cfg.Auth);

            AddLabel(Lang.T("mail.user"));
            var user = AddTextBox(cfg.User);

            var passLabel = AddLabel(Lang.T("mail.password"));
            var password = new PasswordBox { Password = cfg.Password, MinHeight = 32, Padding = new Thickness(9, 6, 9, 6) };
            password.SetResourceReference(BackgroundProperty, "InputBrush");
            password.SetResourceReference(ForegroundProperty, "TextBrush");
            Body.Children.Add(password);

            var oauth = new StackPanel { Visibility = Visibility.Collapsed };
            Body.Children.Add(oauth);

            var manual = new StackPanel();
            oauth.Children.Add(manual);
            TextBox ManualBox(string labelKey, string value)
            {
                manual.Children.Add(new TextBlock { Text = Lang.T(labelKey), Margin = new Thickness(0, 10, 0, 4) });
                var box = new TextBox { Text = value };
                manual.Children.Add(box);
                return box;
            }
            var clientId = ManualBox("mail.oauthClient", cfg.OAuthClientId);
            var clientSecret = ManualBox("mail.oauthSecret", cfg.OAuthClientSecret);
            var tokenUrl = ManualBox("mail.oauthUrl", cfg.OAuthTokenUrl);

            string refreshValue = cfg.OAuthRefreshToken;
            TextBox? fromBox = null;

            var signInRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 14, 0, 0) };
            var signIn = new Button
            {
                Content = MainWindow.BuildIconLabel(Ico.Key, Lang.T("oauth.signIn"), 15),
                MinWidth = 240,
                Margin = new Thickness(0, 0, 14, 0)
            };
            signIn.SetResourceReference(StyleProperty, "FlatButton");
            var signInState = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            signInRow.Children.Add(signIn);
            signInRow.Children.Add(signInState);
            oauth.Children.Add(signInRow);

            var oauthNote = new TextBlock
            {
                Margin = new Thickness(0, 10, 0, 0),
                Foreground = ThemeManager.Brush("TextMutedBrush"),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12
            };
            oauth.Children.Add(oauthNote);

            void SyncSignInState()
            {
                bool signedIn = refreshValue.Trim().Length > 0;
                signInState.Text = Lang.T(signedIn ? "oauth.signedIn" : "oauth.notSignedIn");
                signInState.Foreground = ThemeManager.Brush(signedIn ? "SuccessBrush" : "TextMutedBrush");
                signIn.Content = MainWindow.BuildIconLabel(
                    Ico.Key, Lang.T(signedIn ? "oauth.signInAgain" : "oauth.signIn"), 15);
            }

            signIn.Click += (s, e) =>
            {
                var provider = OAuthClients.For(host.Text) ?? new OAuthProvider
                {
                    AuthorizeUrl = OAuthClients.MicrosoftProvider.AuthorizeUrl,
                    TokenUrl = tokenUrl.Text.Trim().Length > 0
                        ? tokenUrl.Text.Trim() : OAuthDeviceFlow.MicrosoftTokenUrl,
                    Scope = OAuthDeviceFlow.MicrosoftSmtpScope,
                    ClientId = clientId.Text.Trim(),
                    ClientSecret = clientSecret.Text,
                    ExtraAuthParams = "prompt=select_account"
                };

                var dialog = new OAuthSignInDialog(provider) { Owner = this };
                string? token = dialog.Run();
                if (token == null) return;

                refreshValue = token;
                if (tokenUrl.Text.Trim().Length == 0) tokenUrl.Text = provider.TokenUrl;

                if (dialog.Account.Length > 0)
                {
                    user.Text = dialog.Account;
                    if (fromBox != null) fromBox.Text = dialog.Account;
                }

                SyncSignInState();
                MessageDialog.Show(this,
                    dialog.Account.Length > 0
                        ? Lang.T("oauth.successAs", dialog.Account)
                        : Lang.T("oauth.success"),
                    Lang.T("msg.title"), MessageKind.Success);
            };

            AddLabel(Lang.T("mail.from"));
            var from = AddTextBox(cfg.From);
            fromBox = from;
            AddLabel(Lang.T("mail.fromName"));
            var fromName = AddTextBox(cfg.FromName);

            void SyncAuth()
            {
                bool useOauth = auth.SelectedIndex == 1;
                passLabel.Visibility = password.Visibility = useOauth ? Visibility.Collapsed : Visibility.Visible;
                oauth.Visibility = useOauth ? Visibility.Visible : Visibility.Collapsed;

                bool builtIn = OAuthClients.For(host.Text) != null;
                manual.Visibility = builtIn ? Visibility.Collapsed : Visibility.Visible;

                if (!builtIn) oauthNote.Text = Lang.T("oauth.noteManual");
                else
                {
                    bool google = MailProviders.IsGoogle(host.Text);
                    oauthNote.Text = Lang.T("oauth.noteBuiltIn", google ? "Google" : "Microsoft")
                        + (google ? "\n" + Lang.T("oauth.noteGoogle") : "");
                }
                SyncSignInState();
            }
            void SyncProvider()
            {

                bool appPasswordWorks = MailProviders.AppPasswordUrlFor(host.Text) != null
                                        && !MailProviders.IsMicrosoft(host.Text)
                                        && auth.SelectedIndex != 1;
                appPassword.Visibility = appPasswordWorks ? Visibility.Visible : Visibility.Collapsed;
                SyncAuth();
            }

            auth.SelectionChanged += (s, e) => SyncAuth();
            host.TextChanged += (s, e) => SyncProvider();
            SyncProvider();

            SmtpSettings Current() => new()
            {
                Host = host.Text.Trim(),
                Port = int.TryParse(port.Text.Trim(), out int p) && p > 0 ? p : 587,
                Security = (SmtpSecurity)Math.Max(0, security.SelectedIndex),
                Auth = (SmtpAuth)Math.Max(0, auth.SelectedIndex),
                User = user.Text.Trim(),
                Password = password.Password,
                From = from.Text.Trim(),
                FromName = fromName.Text.Trim(),
                OAuthClientId = OAuthClients.For(host.Text)?.ClientId ?? clientId.Text.Trim(),
                OAuthClientSecret = OAuthClients.For(host.Text)?.ClientSecret ?? clientSecret.Text,
                OAuthRefreshToken = refreshValue.Trim(),
                OAuthTokenUrl = OAuthClients.For(host.Text)?.TokenUrl ?? tokenUrl.Text.Trim()
            };

            AddButton(Lang.T("mail.test"), icon: Ico.Mail, onClick: () =>
            {
                var cfgNow = Current();
                var mail = EmailSender.Compose(MailTemplate.Html, Lang.T("mail.test"), "", "", "", Lang.T("mail.test"));
                string? error = EmailSender.Send(cfgNow, cfgNow.From, mail);
                if (error == null)
                    MessageDialog.Show(this, Lang.T("mail.testOk"), Lang.T("msg.title"), MessageKind.Success);
                else
                    MessageDialog.Show(this, Lang.T("mail.testFail", MailProviders.Describe(error, cfgNow.Host)),
                        Lang.T("msg.error"), MessageKind.Error);
            });
            AddOkCancel(() =>
            {
                Current().Save(engine.Settings);
                engine.SaveSettings();
                DialogResult = true;
                Close();
            });
        }

        internal static void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                { FileName = url, UseShellExecute = true });
            }
            catch { }
        }
    }

    internal sealed class OAuthSignInDialog : ThemedWindow
    {
        private readonly OAuthProvider _provider;
        private readonly TextBlock _info = new() { TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _code = new()
        {
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0),
            Visibility = Visibility.Collapsed
        };
        private readonly CancellationTokenSource _cancel = new();
        private string? _result;
        private bool _deviceMode;

        public string Account { get; private set; } = "";

        public OAuthSignInDialog(OAuthProvider provider)
        {
            _provider = provider;
            Title = Lang.T("oauth.signInTitle");
            Width = 480;

            _info.Text = Lang.T("oauth.browserWait");
            Body.Children.Add(_info);
            _code.SetResourceReference(ForegroundProperty, "AccentBrush");
            Body.Children.Add(_code);

            if (provider.TokenUrl.Contains("microsoftonline", StringComparison.OrdinalIgnoreCase))
                AddButton(Lang.T("oauth.useDeviceCode"), icon: Ico.Key, onClick: SwitchToDeviceCode);
            AddButton(Lang.T("dlg.cancel"), onClick: () => { _cancel.Cancel(); DialogResult = false; Close(); });
            Closed += (s, e) => _cancel.Cancel();
        }

        public string? Run()
        {
            Loaded += (s, e) => Start(OAuthBrowserFlow.AuthorizeAsync(
                _provider,
                url => Dispatcher.Invoke(() => SmtpSetupDialog.OpenUrl(url)),
                _cancel.Token), browser: true);

            ShowDialog();
            return _result;
        }

        private void SwitchToDeviceCode()
        {
            if (_deviceMode) return;
            _deviceMode = true;
            _cancel.Cancel();

            _info.Text = Lang.T("oauth.waiting");
            if (Buttons.Children.Count > 1) Buttons.Children.RemoveAt(0);

            var cancel = new CancellationTokenSource();
            Closed += (s, e) => cancel.Cancel();

            Start(OAuthDeviceFlow.AuthorizeAsync(
                OAuthDeviceFlow.MicrosoftDeviceCodeUrl,
                _provider.TokenUrl,
                _provider.ClientId,
                _provider.Scope,
                prompt => Dispatcher.Invoke(() =>
                {
                    _info.Text = Lang.T("oauth.codePrompt", prompt.VerificationUrl);
                    _code.Text = prompt.UserCode;
                    _code.Visibility = Visibility.Visible;
                    SmtpSetupDialog.OpenUrl(prompt.VerificationUrl);
                }),
                cancel.Token), browser: false);
        }

        private void Start(System.Threading.Tasks.Task<DeviceFlowResult> task, bool browser) =>
            task.ContinueWith(t => Dispatcher.Invoke(() =>
            {

                if (!IsLoaded || (browser && _deviceMode)) return;

                if (t.IsCompletedSuccessfully && t.Result.Success)
                {
                    _result = t.Result.RefreshToken;
                    Account = t.Result.Account;
                    DialogResult = true;
                    Close();
                    return;
                }

                string message = t.IsCompletedSuccessfully
                    ? t.Result.Error ?? Lang.T("oauth.failed")
                    : t.Exception?.GetBaseException().Message ?? Lang.T("oauth.failed");
                MessageDialog.Show(this, message, Lang.T("msg.error"), MessageKind.Error);
                DialogResult = false;
                Close();
            }), System.Threading.Tasks.TaskScheduler.Default);
    }

    public sealed class EmailActionDialog : ThemedWindow
    {
        public EmailActionDialog(AppEngine engine, int activator)
        {
            int n = activator > 0 ? activator : 1;
            Title = Lang.T("dlg.actionSettings", AppEngine.ActionDisplay(AppEngine.ActionEmail));
            Width = 620;

            string Value(string name) => engine.Param(n, "mail", name);

            var setup = new Button
            {
                Content = MainWindow.BuildIconLabel(Ico.Settings, Lang.T("mail.setupButton"), 15),
                HorizontalAlignment = HorizontalAlignment.Left,
                MinWidth = 240,
                Margin = new Thickness(0, 0, 0, 6)
            };
            setup.SetResourceReference(StyleProperty, "FlatButton");
            setup.Click += (s, e) => new SmtpSetupDialog(engine) { Owner = this }.ShowDialog();
            Body.Children.Add(setup);

            var shared = AddCheck(Lang.T("par.useShared"), engine.ParamShared(n, "mail"));
            shared.IsEnabled = activator > 0;

            AddLabel(Lang.T("mail.to"), bold: true);
            var to = AddTextBox(Value("mail_to"));
            AddLabel(Lang.T("mail.subject"), bold: true);
            var subject = AddTextBox(Value("mail_subject"));

            AddLabel(Lang.T("mail.template"), bold: true);
            var template = AddCombo(Lang.A("mailTemplates"),
                Enum.TryParse<MailTemplate>(Value("mail_template"), out var t) ? (int)t : 0);

            var detailLabel = AddLabel("", bold: true);
            var detailRow = new Grid();
            detailRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            detailRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var detail = new TextBox { TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var browse = new Button { Width = 40, Margin = new Thickness(8, 0, 0, 0), Content = new IconView { Kind = Ico.Folder, Width = 15, Height = 15 } };
            browse.SetResourceReference(StyleProperty, "SquareIconButton");
            browse.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(browse, 1);
            detailRow.Children.Add(detail);
            detailRow.Children.Add(browse);
            Body.Children.Add(detailRow);

            var htmlNote = new TextBlock
            {
                Text = Lang.T("mail.htmlNote"),
                Margin = new Thickness(0, 6, 0, 0),
                Foreground = ThemeManager.Brush("TextMutedBrush"),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Visibility = Visibility.Collapsed
            };
            Body.Children.Add(htmlNote);

            void SyncDetail()
            {
                var kind = (MailTemplate)Math.Max(0, template.SelectedIndex);
                htmlNote.Visibility = kind == MailTemplate.Html ? Visibility.Visible : Visibility.Collapsed;
                switch (kind)
                {
                    case MailTemplate.Attachment:
                        detailLabel.Text = Lang.T("mail.attachment");
                        detail.AcceptsReturn = false; detail.Height = double.NaN; detail.MinHeight = 32;
                        detail.Text = Value("mail_attachment");
                        detail.IsEnabled = true; browse.Visibility = Visibility.Visible;
                        break;
                    case MailTemplate.Command:
                        detailLabel.Text = Lang.T("mail.command");
                        detail.AcceptsReturn = false; detail.Height = double.NaN; detail.MinHeight = 32;
                        detail.Text = Value("mail_command");
                        detail.IsEnabled = true; browse.Visibility = Visibility.Collapsed;
                        break;
                    case MailTemplate.Html:
                    case MailTemplate.Custom:
                        detailLabel.Text = Lang.T("mail.text");
                        detail.AcceptsReturn = true; detail.Height = 120;
                        detail.Text = Value("mail_body");
                        detail.IsEnabled = true; browse.Visibility = Visibility.Collapsed;
                        break;
                    default:

                        detailLabel.Visibility = Visibility.Collapsed;
                        detailRow.Visibility = Visibility.Collapsed;
                        browse.Visibility = Visibility.Collapsed;
                        return;
                }

                detailLabel.Visibility = Visibility.Visible;
                detailRow.Visibility = Visibility.Visible;
            }

            void Store()
            {
                switch ((MailTemplate)Math.Max(0, template.SelectedIndex))
                {
                    case MailTemplate.Attachment:
                        engine.SetParam(n, "mail", "mail_attachment", detail.Text.Trim()); break;
                    case MailTemplate.Command:
                        engine.SetParam(n, "mail", "mail_command", detail.Text.Trim()); break;
                    case MailTemplate.Html:
                    case MailTemplate.Custom:
                        engine.SetParam(n, "mail", "mail_body", detail.Text); break;
                }
            }

            browse.Click += (s, e) =>
            {
                var ofd = new OpenFileDialog { Title = Lang.T("mail.attachment") };
                if (ofd.ShowDialog(this) != true) return;
                detail.Text = ofd.FileName;
                WarnIfTooBig(engine, ofd.FileName);
            };
            template.SelectionChanged += (s, e) => SyncDetail();
            SyncDetail();

            AddButton(Lang.T("mail.test"), icon: Ico.Mail, onClick: () =>
            {
                Store();
                var cfg = SmtpSettings.Load(engine.Settings);
                var mail = EmailSender.Compose((MailTemplate)Math.Max(0, template.SelectedIndex),
                    subject.Text.Trim(), Value("mail_body"),
                    Value("mail_attachment"), Value("mail_command"),
                    AppEngine.ActionDisplay(AppEngine.ActionEmail));
                string? error = EmailSender.Send(cfg, to.Text.Trim(), mail);
                if (error == null)
                    MessageDialog.Show(this, Lang.T("mail.testOk"), Lang.T("msg.title"), MessageKind.Success);
                else
                    MessageDialog.Show(this, Lang.T("mail.testFail", MailProviders.Describe(error, cfg.Host)),
                        Lang.T("msg.error"), MessageKind.Error);
            });
            AddOkCancel(() =>
            {
                engine.SetParamShared(n, "mail", shared.IsChecked == true);
                engine.SetParam(n, "mail", "mail_to", to.Text.Trim());
                engine.SetParam(n, "mail", "mail_subject", subject.Text.Trim());
                engine.SetParam(n, "mail", "mail_template",
                    ((MailTemplate)Math.Max(0, template.SelectedIndex)).ToString());
                Store();
                engine.SaveSettings();
                if ((MailTemplate)Math.Max(0, template.SelectedIndex) is MailTemplate.Attachment or MailTemplate.Html)
                    WarnIfTooBig(engine, Value("mail_attachment"));
                DialogResult = true;
                Close();
            });
        }

        private void WarnIfTooBig(AppEngine engine, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                var info = new System.IO.FileInfo(path);
                if (!info.Exists) return;

                string host = engine.Get("smtp_host");
                var provider = MailProviders.Match(host);
                int limitMB = MailProviders.LimitMB(host);
                if (info.Length <= (long)limitMB * 1024 * 1024) return;

                double mb = info.Length / 1024d / 1024d;
                MessageDialog.Show(this,
                    Lang.T("mail.sizeWarn",
                        provider?.Name ?? Lang.T("mail.unknownProvider"),
                        limitMB + " MB",
                        mb >= 1 ? $"{mb:0.0} MB" : $"{info.Length / 1024d:0} KB"),
                    Lang.T("mail.sizeWarnTitle"), MessageKind.Warning);
            }
            catch { }
        }
    }
}
