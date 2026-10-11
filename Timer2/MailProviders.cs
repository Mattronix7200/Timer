using System;
using System.Collections.Generic;
using System.Linq;

namespace TimerSys
{

    internal sealed class MailProvider
    {
        public string Name { get; init; } = "";
        public string Host { get; init; } = "";
        public int Port { get; init; } = 587;
        public SmtpSecurity Security { get; init; } = SmtpSecurity.StartTls;

        public int AttachmentLimitMB { get; init; } = 25;

        public string[] HostMatches { get; init; } = Array.Empty<string>();

        public string AppPasswordUrl { get; init; } = "";
    }

    internal static class MailProviders
    {

        public const int DefaultLimitMB = 25;

        public static readonly IReadOnlyList<MailProvider> All = new[]
        {
            new MailProvider { Name = "OVH",                     Host = "ssl0.ovh.net",            Port = 465, Security = SmtpSecurity.ImplicitSsl, AttachmentLimitMB = 100, HostMatches = new[] { "ovh." } },
            new MailProvider { Name = "OVH (STARTTLS)",          Host = "ssl0.ovh.net",            Port = 587, Security = SmtpSecurity.StartTls,    AttachmentLimitMB = 100, HostMatches = Array.Empty<string>() },
            new MailProvider { Name = "Gmail",                   Host = "smtp.gmail.com",          Port = 587, Security = SmtpSecurity.StartTls,    AttachmentLimitMB = 25,  HostMatches = new[] { "gmail", "googlemail" }, AppPasswordUrl = "https://myaccount.google.com/apppasswords" },
            new MailProvider { Name = "Outlook.com / Hotmail",   Host = "smtp-mail.outlook.com",   Port = 587, Security = SmtpSecurity.StartTls,    AttachmentLimitMB = 25,  HostMatches = new[] { "smtp-mail.outlook" }, AppPasswordUrl = "https://account.live.com/proofs/AppPassword" },
            new MailProvider { Name = "Microsoft 365",           Host = "smtp.office365.com",      Port = 587, Security = SmtpSecurity.StartTls,    AttachmentLimitMB = 25,  HostMatches = new[] { "office365", "outlook.office" } },
            new MailProvider { Name = "WP.pl",                   Host = "smtp.wp.pl",              Port = 465, Security = SmtpSecurity.ImplicitSsl, AttachmentLimitMB = 100, HostMatches = new[] { "wp.pl" } },
            new MailProvider { Name = "o2.pl",                   Host = "poczta.o2.pl",            Port = 465, Security = SmtpSecurity.ImplicitSsl, AttachmentLimitMB = 100, HostMatches = new[] { "o2.pl" } },
            new MailProvider { Name = "Interia.pl",              Host = "poczta.interia.pl",       Port = 465, Security = SmtpSecurity.ImplicitSsl, AttachmentLimitMB = 100, HostMatches = new[] { "interia" } },
            new MailProvider { Name = "Onet.pl",                 Host = "smtp.poczta.onet.pl",     Port = 465, Security = SmtpSecurity.ImplicitSsl, AttachmentLimitMB = 50,  HostMatches = new[] { "onet" } },
            new MailProvider { Name = "nazwa.pl",                Host = "smtp.nazwa.pl",           Port = 465, Security = SmtpSecurity.ImplicitSsl, AttachmentLimitMB = 100, HostMatches = new[] { "nazwa.pl" } },
            new MailProvider { Name = "home.pl",                 Host = "poczta.home.pl",          Port = 465, Security = SmtpSecurity.ImplicitSsl, AttachmentLimitMB = 100, HostMatches = new[] { "home.pl" } },
        };

        public static MailProvider? Match(string? host)
        {
            if (string.IsNullOrWhiteSpace(host)) return null;
            string h = host.Trim().ToLowerInvariant();
            return All.FirstOrDefault(p => p.HostMatches.Any(m => h.Contains(m)));
        }

        public static bool IsMicrosoft(string? host)
        {
            if (string.IsNullOrWhiteSpace(host)) return false;
            string h = host.Trim().ToLowerInvariant();
            return h.Contains("outlook") || h.Contains("office365") || h.Contains("hotmail") || h.Contains("live.com");
        }

        public static bool IsGoogle(string? host)
        {
            if (string.IsNullOrWhiteSpace(host)) return false;
            string h = host.Trim().ToLowerInvariant();
            return h.Contains("gmail") || h.Contains("googlemail");
        }

        public static int LimitMB(string? host) => Match(host)?.AttachmentLimitMB ?? DefaultLimitMB;

        public static string? AppPasswordUrlFor(string? host) =>
            string.IsNullOrEmpty(Match(host)?.AppPasswordUrl) ? null : Match(host)!.AppPasswordUrl;

        public static string Describe(string? serverReply) => Describe(serverReply, null);

        public static string Describe(string? serverReply, string? host)
        {
            string raw = (serverReply ?? "").Trim();
            if (raw.Length == 0) return Lang.T("smtp.unknown");

            raw = Shorten(raw);
            string lower = raw.ToLowerInvariant();
            string? friendly = null;

            if (lower.Contains("sendasdenied") || lower.Contains("5.2.252") ||
                lower.Contains("not allowed to send as"))
                friendly = Lang.T("smtp.sendAsDenied");
            else if (lower.Contains("5.3.4") || lower.Contains("message size") || lower.Contains("size limit") ||
                lower.Contains("too large") || lower.Contains("exceeded"))
                friendly = Lang.T("smtp.tooBig");
            else if (lower.Contains("virus") || lower.Contains("malware") ||
                     lower.Contains("security issue") || lower.Contains("blocked because"))
                friendly = Lang.T("smtp.blockedContent");
            else if (raw.StartsWith("535") || lower.Contains("authentication unsuccessful") ||
                     lower.Contains("authentication failed") || lower.Contains("invalid credentials"))
                friendly = Lang.T("smtp.authFailed");
            else if (raw.StartsWith("530") || lower.Contains("must issue a starttls") ||
                     lower.Contains("authentication required"))
                friendly = Lang.T("smtp.authRequired");
            else if (raw.StartsWith("550") || raw.StartsWith("553"))
                friendly = Lang.T("smtp.rejectedRecipient");
            else if (raw.StartsWith("554"))
                friendly = Lang.T("smtp.rejectedMessage");
            else if (raw.StartsWith("421") || raw.StartsWith("451") || raw.StartsWith("452"))
                friendly = Lang.T("smtp.temporary");
            else if (lower.Contains("spam"))
                friendly = Lang.T("smtp.spam");

            string hint = ProviderHint(host, raw);
            if (hint.Length > 0) friendly = (friendly ?? "") + (friendly == null ? "" : "\n\n") + hint;

            return friendly == null
                ? raw
                : friendly + "\n\n" + Lang.T("smtp.serverSaid") + "\n" + raw;
        }

        private static string Shorten(string raw)
        {
            int begin = raw.IndexOf("[BeginDiagnosticData]", StringComparison.OrdinalIgnoreCase);
            if (begin >= 0)
            {
                int end = raw.IndexOf("[EndDiagnosticData]", StringComparison.OrdinalIgnoreCase);
                raw = end > begin
                    ? raw[..begin] + raw[(end + "[EndDiagnosticData]".Length)..]
                    : raw[..begin];
            }

            raw = raw.Trim();
            const int limit = 600;
            return raw.Length > limit ? raw[..limit].TrimEnd() + " (...)" : raw;
        }

        private static string ProviderHint(string? host, string raw)
        {
            if (string.IsNullOrWhiteSpace(host)) return "";
            bool authProblem = raw.StartsWith("535") ||
                               raw.ToLowerInvariant().Contains("authentication unsuccessful");
            if (!authProblem) return "";

            string h = host.ToLowerInvariant();
            if (h.Contains("outlook") || h.Contains("hotmail") || h.Contains("live.com") || h.Contains("office365"))
                return Lang.T("smtp.hintMicrosoft");
            if (h.Contains("gmail") || h.Contains("googlemail"))
                return Lang.T("smtp.hintGoogle");
            return "";
        }
    }
}
