using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace TimerSys
{
    internal enum SmtpSecurity { Auto, ImplicitSsl, StartTls, None }

    internal enum SmtpAuth { Password, OAuth2 }

    internal static class SmtpTransport
    {

        public static string? Send(
            string host, int port, SmtpSecurity security,
            SmtpAuth auth, string user, string secret,
            string envelopeFrom, IEnumerable<string> recipients, byte[] message)
        {
            var rcpts = new List<string>();
            foreach (var r in recipients)
            {
                string t = r.Trim();
                if (t.Length > 0) rcpts.Add(t);
            }
            if (rcpts.Count == 0) return Lang.T("mail.noRecipient");

            bool implicitSsl = security switch
            {
                SmtpSecurity.ImplicitSsl => true,
                SmtpSecurity.StartTls    => false,
                SmtpSecurity.None        => false,
                _                        => port == 465
            };
            bool wantStartTls = security == SmtpSecurity.StartTls ||
                                (security == SmtpSecurity.Auto && !implicitSsl);

            TcpClient? tcp = null;
            Stream? stream = null;
            try
            {
                tcp = new TcpClient { SendTimeout = 30000, ReceiveTimeout = 30000 };
                if (!tcp.ConnectAsync(host, port).Wait(20000))
                    return Lang.T("mail.timeout", host, port);

                stream = tcp.GetStream();
                if (implicitSsl) stream = Secure(stream, host);

                var io = new Session(stream);

                string greeting = io.Read();
                if (!greeting.StartsWith("220")) return greeting;

                string ehlo = io.Command("EHLO " + LocalName());
                if (!ehlo.StartsWith("250"))
                {
                    ehlo = io.Command("HELO " + LocalName());
                    if (!ehlo.StartsWith("250")) return ehlo;
                }

                if (!implicitSsl && wantStartTls && ehlo.IndexOf("STARTTLS", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string tls = io.Command("STARTTLS");
                    if (!tls.StartsWith("220")) return tls;
                    stream = Secure(stream, host);
                    io = new Session(stream);
                    ehlo = io.Command("EHLO " + LocalName());
                    if (!ehlo.StartsWith("250")) return ehlo;
                }

                if (!string.IsNullOrWhiteSpace(user))
                {
                    string? authError = Authenticate(io, ehlo, auth, user, secret);
                    if (authError != null) return authError;
                }

                string from = io.Command($"MAIL FROM:<{envelopeFrom}>");
                if (!from.StartsWith("250")) return from;

                foreach (var rcpt in rcpts)
                {
                    string r = io.Command($"RCPT TO:<{rcpt}>");
                    if (!r.StartsWith("250") && !r.StartsWith("251")) return r;
                }

                string data = io.Command("DATA");
                if (!data.StartsWith("354")) return data;

                io.WriteRaw(DotStuff(message));
                io.WriteRaw(Encoding.ASCII.GetBytes("\r\n.\r\n"));
                string done = io.Read();
                if (!done.StartsWith("250")) return done;

                try { io.Command("QUIT"); } catch { }
                return null;
            }
            catch (Exception ex)
            {
                return ex.InnerException?.Message ?? ex.Message;
            }
            finally
            {
                try { stream?.Dispose(); } catch { }
                try { tcp?.Dispose(); } catch { }
            }
        }

        private static string? Authenticate(Session io, string ehlo, SmtpAuth auth, string user, string secret)
        {
            if (auth == SmtpAuth.OAuth2)
            {
                string token = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                    $"user={user}\u0001auth=Bearer {secret}\u0001\u0001"));
                string r = io.Command("AUTH XOAUTH2 " + token);
                if (r.StartsWith("235")) return null;
                if (r.StartsWith("334")) { io.Command(""); }
                return Lang.T("mail.authFailed", r);
            }

            bool supportsLogin = ehlo.IndexOf("LOGIN", StringComparison.OrdinalIgnoreCase) >= 0;
            bool supportsPlain = ehlo.IndexOf("PLAIN", StringComparison.OrdinalIgnoreCase) >= 0;

            if (supportsLogin || !supportsPlain)
            {
                string r = io.Command("AUTH LOGIN");
                if (r.StartsWith("334"))
                {
                    r = io.Command(Convert.ToBase64String(Encoding.UTF8.GetBytes(user)));
                    if (r.StartsWith("334"))
                    {
                        r = io.Command(Convert.ToBase64String(Encoding.UTF8.GetBytes(secret)));
                        if (r.StartsWith("235")) return null;
                    }
                }
                if (!supportsPlain) return Lang.T("mail.authFailed", r);
            }

            string plain = Convert.ToBase64String(Encoding.UTF8.GetBytes("\0" + user + "\0" + secret));
            string p = io.Command("AUTH PLAIN " + plain);
            return p.StartsWith("235") ? null : Lang.T("mail.authFailed", p);
        }

        private static Stream Secure(Stream inner, string host)
        {
            var ssl = new SslStream(inner, leaveInnerStreamOpen: false);
            ssl.AuthenticateAsClient(host);
            return ssl;
        }

        private static string LocalName()
        {
            string n = Environment.MachineName;
            foreach (char c in n)
            {
                if (!char.IsLetterOrDigit(c) && c != '-') return "localhost";
            }
            return n.Length > 0 ? n : "localhost";
        }

        private static byte[] DotStuff(byte[] data)
        {
            var outBuf = new List<byte>(data.Length + 64);
            bool lineStart = true;
            foreach (byte b in data)
            {
                if (lineStart && b == (byte)'.') outBuf.Add((byte)'.');
                outBuf.Add(b);
                lineStart = b == (byte)'\n';
            }
            return outBuf.ToArray();
        }

        private sealed class Session
        {
            private readonly Stream _stream;
            private readonly byte[] _buffer = new byte[8192];
            private readonly StringBuilder _pending = new();

            public Session(Stream stream) => _stream = stream;

            public void WriteRaw(byte[] data)
            {
                _stream.Write(data, 0, data.Length);
                _stream.Flush();
            }

            public string Command(string line)
            {
                WriteRaw(Encoding.UTF8.GetBytes(line + "\r\n"));
                return Read();
            }

            public string Read()
            {
                var result = new StringBuilder();
                while (true)
                {
                    string line = ReadLine();
                    if (line.Length == 0) break;
                    result.AppendLine(line);
                    if (line.Length < 4 || line[3] != '-') break;
                }
                return result.ToString().TrimEnd();
            }

            private string ReadLine()
            {
                while (true)
                {
                    string buffered = _pending.ToString();
                    int idx = buffered.IndexOf('\n');
                    if (idx >= 0)
                    {
                        string line = buffered.Substring(0, idx).TrimEnd('\r');
                        _pending.Clear();
                        _pending.Append(buffered.Substring(idx + 1));
                        return line;
                    }

                    int read = _stream.Read(_buffer, 0, _buffer.Length);
                    if (read <= 0) return buffered;
                    _pending.Append(Encoding.UTF8.GetString(_buffer, 0, read));
                }
            }
        }
    }
}
