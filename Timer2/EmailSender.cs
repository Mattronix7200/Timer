using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace TimerSys
{

    internal enum MailTemplate
    {
        ShortText,
        DateTime,
        Attachment,
        Html,
        Command,
        Custom
    }

    internal static class SecretStore
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("TimerSys.Smtp.v1");

        public static string Protect(string? plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            try
            {
                byte[] data = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(plain), Entropy, DataProtectionScope.CurrentUser);
                return "dpapi:" + Convert.ToBase64String(data);
            }
            catch { return ""; }
        }

        public static string Unprotect(string? stored)
        {
            if (string.IsNullOrEmpty(stored)) return "";
            if (!stored.StartsWith("dpapi:", StringComparison.Ordinal)) return "";
            try
            {
                byte[] data = Convert.FromBase64String(stored.Substring(6));
                return Encoding.UTF8.GetString(
                    ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser));
            }
            catch { return ""; }
        }
    }

    internal sealed class SmtpSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public SmtpSecurity Security { get; set; } = SmtpSecurity.Auto;
        public SmtpAuth Auth { get; set; } = SmtpAuth.Password;
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public string From { get; set; } = "";
        public string FromName { get; set; } = "";

        public string OAuthClientId { get; set; } = "";
        public string OAuthClientSecret { get; set; } = "";
        public string OAuthRefreshToken { get; set; } = "";
        public string OAuthTokenUrl { get; set; } = "";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);

        public static SmtpSettings Load(NameValueCollection s) => new SmtpSettings
        {
            Host     = (s["smtp_host"] ?? "").Trim(),
            Port     = int.TryParse(s["smtp_port"], out int p) && p > 0 ? p : 587,
            Security = Enum.TryParse<SmtpSecurity>(s["smtp_security"], out var sec) ? sec : SmtpSecurity.Auto,
            Auth     = Enum.TryParse<SmtpAuth>(s["smtp_auth"], out var au) ? au : SmtpAuth.Password,
            User     = (s["smtp_user"] ?? "").Trim(),
            Password = SecretStore.Unprotect(s["smtp_pass"]),
            From     = (s["smtp_from"] ?? "").Trim(),
            FromName = (s["smtp_fromName"] ?? "").Trim(),

            OAuthClientId     = (s["smtp_oauth_client"] ?? "").Trim(),
            OAuthClientSecret = SecretStore.Unprotect(s["smtp_oauth_secret"]),
            OAuthRefreshToken = SecretStore.Unprotect(s["smtp_oauth_refresh"]),
            OAuthTokenUrl     = (s["smtp_oauth_url"] ?? "").Trim()
        }.WithBuiltInOAuth();

        public SmtpSettings WithBuiltInOAuth()
        {
            var provider = OAuthClients.For(Host);
            if (provider == null) return this;

            if (OAuthClientId.Length == 0) OAuthClientId = provider.ClientId;
            if (OAuthClientSecret.Length == 0) OAuthClientSecret = provider.ClientSecret;
            if (OAuthTokenUrl.Length == 0) OAuthTokenUrl = provider.TokenUrl;
            return this;
        }

        public void Save(NameValueCollection s)
        {
            s["smtp_host"]     = Host;
            s["smtp_port"]     = Port.ToString();
            s["smtp_security"] = Security.ToString();
            s["smtp_auth"]     = Auth.ToString();
            s["smtp_user"]     = User;
            s["smtp_pass"]     = SecretStore.Protect(Password);
            s["smtp_from"]     = From;
            s["smtp_fromName"] = FromName;

            s["smtp_oauth_client"]  = OAuthClientId;
            s["smtp_oauth_secret"]  = SecretStore.Protect(OAuthClientSecret);
            s["smtp_oauth_refresh"] = SecretStore.Protect(OAuthRefreshToken);
            s["smtp_oauth_url"]     = OAuthTokenUrl;
        }
    }

    internal sealed class ComposedMail
    {
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";
        public bool IsHtml { get; set; }
        public List<string> Attachments { get; } = new();
    }

    internal static class EmailSender
    {
        public const string DownloadUrl = "https://github.com/Mattronix7200/TimerSysApp/releases";

        public static ComposedMail Compose(
            MailTemplate template,
            string subject,
            string customText,
            string attachmentPath,
            string command,
            string actionName)
        {
            var mail = new ComposedMail();
            string now  = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            string host = Environment.MachineName;
            string user = Environment.UserName;

            mail.Subject = string.IsNullOrWhiteSpace(subject)
                ? Lang.T("mail.defaultSubject", actionName)
                : subject;

            switch (template)
            {
                case MailTemplate.ShortText:
                    mail.Body = Lang.T("mail.bodyShort", actionName, now);
                    break;

                case MailTemplate.DateTime:
                    mail.Body = Lang.T("mail.bodyShort", actionName, now) + "\r\n\r\n" +
                                Lang.T("note.when", now) + "\r\n" +
                                Lang.T("note.who", host, user);
                    break;

                case MailTemplate.Attachment:
                    mail.Body = Lang.T("mail.bodyAttachment", actionName, now);
                    if (!string.IsNullOrWhiteSpace(attachmentPath) && File.Exists(attachmentPath))
                        mail.Attachments.Add(attachmentPath);
                    else
                        mail.Body += "\r\n\r\n" + Lang.T("mail.attachmentMissing", attachmentPath);
                    break;

                case MailTemplate.Command:
                {
                    string output = RunCommand(command, out string outFile);
                    mail.Body = Lang.T("mail.bodyCommand", command, now) + "\r\n\r\n" +
                                "----------------------------------------\r\n" + output;
                    if (!string.IsNullOrEmpty(outFile)) mail.Attachments.Add(outFile);
                    break;
                }

                case MailTemplate.Html:
                {
                    string inner = string.IsNullOrWhiteSpace(customText)
                        ? Lang.T("mail.bodyShort", actionName, now)
                        : customText;
                    mail.Body   = BuildHtml(mail.Subject, inner, now, host, user);
                    mail.IsHtml = true;
                    if (!string.IsNullOrWhiteSpace(attachmentPath) && File.Exists(attachmentPath))
                        mail.Attachments.Add(attachmentPath);
                    break;
                }

                default:
                    mail.Body = string.IsNullOrWhiteSpace(customText)
                        ? Lang.T("mail.bodyShort", actionName, now)
                        : customText;
                    break;
            }

            if (!mail.IsHtml)
                mail.Body += "\r\n\r\n-- \r\n" + Lang.T("mail.footerPlain");

            return mail;
        }

        private static string RunCommand(string command, out string outputFile)
        {
            outputFile = "";
            if (string.IsNullOrWhiteSpace(command)) return Lang.T("mail.noCommand");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "cmd.exe",
                    Arguments              = "/c " + command,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding  = Encoding.UTF8
                };
                using var proc = Process.Start(psi);
                if (proc == null) return Lang.T("mail.noCommand");

                string output = proc.StandardOutput.ReadToEnd();
                string errors = proc.StandardError.ReadToEnd();
                proc.WaitForExit(120000);

                if (!string.IsNullOrWhiteSpace(errors)) output += "\r\n[stderr]\r\n" + errors;
                if (string.IsNullOrWhiteSpace(output)) output = Lang.T("mail.commandNoOutput");

                try
                {
                    string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
                    Directory.CreateDirectory(dir);
                    string file = Path.Combine(dir, $"command_output_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                    File.WriteAllText(file, output, Encoding.UTF8);
                    outputFile = file;
                }
                catch { }

                return output;
            }
            catch (Exception ex)
            {
                return Lang.T("mail.commandFailed", ex.Message);
            }
        }

        public static string BuildHtml(string title, string body, string now, string host, string user)
        {
            string safeTitle = WebUtility.HtmlEncode(title);
            string footer = Lang.T("mail.footerHtml")
                .Replace("{LINK}", $"<a href=\"{DownloadUrl}\" style=\"color:#f2566f;text-decoration:none;\">"
                                   + WebUtility.HtmlEncode(Lang.T("msg.title")) + "</a>");

            return $@"<!DOCTYPE html>
<html><head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1""></head>
<body style=""margin:0;padding:0;background:#f4f5f7;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#f4f5f7;padding:24px 0;"">
  <tr><td align=""center"">
    <table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""width:600px;max-width:600px;background:#ffffff;border:1px solid #e3e5e8;border-radius:8px;font-family:'Segoe UI',Arial,Helvetica,sans-serif;"">
      <tr><td style=""padding:20px 28px 16px 28px;border-bottom:3px solid #f2566f;"">
        <span style=""font-size:20px;font-weight:bold;color:#f2566f;"">{safeTitle}</span>
      </td></tr>
      <tr><td style=""padding:24px 28px;font-size:14px;line-height:1.6;color:#2f3338;"">
        {body}
      </td></tr>
      <tr><td style=""padding:0 28px 20px 28px;"">
        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""font-size:12px;color:#5b6168;background:#fafbfc;border:1px solid #eceef0;border-radius:6px;"">
          <tr><td style=""padding:10px 14px;"">{WebUtility.HtmlEncode(Lang.T("note.when", now))}</td></tr>
          <tr><td style=""padding:0 14px 10px 14px;"">{WebUtility.HtmlEncode(Lang.T("note.who", host, user))}</td></tr>
        </table>
      </td></tr>
      <tr><td style=""padding:14px 28px;background:#fafbfc;border-top:1px solid #e3e5e8;border-radius:0 0 8px 8px;font-size:12px;color:#7a8089;"">
        {footer}
      </td></tr>
    </table>
  </td></tr>
</table>
</body></html>";
        }

        public static string? Send(SmtpSettings cfg, string recipients, ComposedMail mail)
        {
            if (!cfg.IsConfigured) return Lang.T("mail.notConfigured");

            var to = recipients
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Where(r => r.Length > 0)
                .ToList();
            if (to.Count == 0) return Lang.T("mail.noRecipient");

            string secret = cfg.Password;
            if (cfg.Auth == SmtpAuth.OAuth2)
            {
                string? tokenError = AcquireAccessToken(cfg, out secret);
                if (tokenError != null) return tokenError;
            }

            byte[] mime;
            try { mime = BuildMime(cfg, to, mail); }
            catch (Exception ex) { return ex.Message; }
            if (mime.Length == 0) return Lang.T("mail.buildFailed");

            return SmtpTransport.Send(
                cfg.Host, cfg.Port, cfg.Security, cfg.Auth,
                cfg.User, secret, cfg.From, to, mime);
        }

        private static byte[] BuildMime(SmtpSettings cfg, List<string> recipients, ComposedMail mail)
        {
            string tmp = Path.Combine(Path.GetTempPath(), "TimerSysMail_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            var streams = new List<FileStream>();
            try
            {
                using (var message = new MailMessage
                {
                    From            = string.IsNullOrWhiteSpace(cfg.FromName)
                                        ? new MailAddress(cfg.From)
                                        : new MailAddress(cfg.From, cfg.FromName, Encoding.UTF8),
                    Subject         = mail.Subject,
                    Body            = mail.Body,
                    IsBodyHtml      = mail.IsHtml,
                    BodyEncoding    = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                })
                {
                    foreach (var r in recipients) message.To.Add(r);

                    foreach (var path in mail.Attachments)
                    {
                        if (!File.Exists(path)) continue;
                        var fs = File.OpenRead(path);
                        streams.Add(fs);
                        message.Attachments.Add(new Attachment(fs, Path.GetFileName(path)));
                    }

                    using var writer = new System.Net.Mail.SmtpClient
                    {
                        DeliveryMethod          = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                        PickupDirectoryLocation = tmp
                    };
                    writer.Send(message);
                }

                string? file = Directory.GetFiles(tmp, "*.eml").FirstOrDefault();
                return file == null ? Array.Empty<byte>() : File.ReadAllBytes(file);
            }
            finally
            {
                foreach (var fs in streams)
                {
                    try { fs.Dispose(); } catch { }
                }
                try { Directory.Delete(tmp, true); } catch { }
            }
        }

        public static string? AcquireAccessToken(SmtpSettings cfg, out string accessToken)
        {
            accessToken = "";
            if (string.IsNullOrWhiteSpace(cfg.OAuthTokenUrl) ||
                string.IsNullOrWhiteSpace(cfg.OAuthRefreshToken) ||
                string.IsNullOrWhiteSpace(cfg.OAuthClientId))
                return Lang.T("mail.oauthIncomplete");

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                var fields = new Dictionary<string, string>
                {
                    ["grant_type"]    = "refresh_token",
                    ["refresh_token"] = cfg.OAuthRefreshToken,
                    ["client_id"]     = cfg.OAuthClientId
                };
                if (!string.IsNullOrWhiteSpace(cfg.OAuthClientSecret))
                    fields["client_secret"] = cfg.OAuthClientSecret;

                using var content  = new FormUrlEncodedContent(fields);
                using var response = client.PostAsync(cfg.OAuthTokenUrl, content).GetAwaiter().GetResult();
                string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                    return Lang.T("mail.oauthFailed", (int)response.StatusCode + " " + json);

                var parsed = Newtonsoft.Json.Linq.JObject.Parse(json);
                accessToken = parsed.Value<string>("access_token") ?? "";
                return string.IsNullOrEmpty(accessToken)
                    ? Lang.T("mail.oauthFailed", json)
                    : null;
            }
            catch (Exception ex)
            {
                return Lang.T("mail.oauthFailed", ex.Message);
            }
        }
    }
}
