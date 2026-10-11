using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TimerSys
{

    internal static class OAuthBrowserFlow
    {
        public static async Task<DeviceFlowResult> AuthorizeAsync(
            OAuthProvider provider,
            Action<string> onBrowserOpened,
            CancellationToken cancel)
        {
            if (string.IsNullOrWhiteSpace(provider.ClientId))
                return new DeviceFlowResult { Error = Lang.T("oauth.noClientId") };

            TcpListener? listener = null;
            try
            {

                listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                string redirectUri = $"http://localhost:{port}/";

                string verifier = RandomUrlSafe(32);
                string challenge = Challenge(verifier);
                string state = RandomUrlSafe(16);

                string url = provider.AuthorizeUrl
                    + "?client_id=" + Uri.EscapeDataString(provider.ClientId)
                    + "&response_type=code"
                    + "&redirect_uri=" + Uri.EscapeDataString(redirectUri)
                    + "&scope=" + Uri.EscapeDataString(provider.Scope)
                    + "&state=" + state
                    + "&code_challenge=" + challenge
                    + "&code_challenge_method=S256";
                if (provider.ExtraAuthParams.Length > 0) url += "&" + provider.ExtraAuthParams;

                onBrowserOpened(url);

                var query = await ReadCallbackAsync(listener, cancel).ConfigureAwait(false);
                if (query == null) return new DeviceFlowResult { Error = Lang.T("oauth.cancelled") };

                if (query.TryGetValue("error", out string? error))
                {
                    query.TryGetValue("error_description", out string? description);
                    return new DeviceFlowResult { Error = description ?? error };
                }
                if (!query.TryGetValue("code", out string? code) || code.Length == 0)
                    return new DeviceFlowResult { Error = Lang.T("oauth.noCode") };
                if (!query.TryGetValue("state", out string? returned) || returned != state)
                    return new DeviceFlowResult { Error = Lang.T("oauth.badState") };

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                var fields = new Dictionary<string, string>
                {
                    ["grant_type"]    = "authorization_code",
                    ["client_id"]     = provider.ClientId,
                    ["code"]          = code,
                    ["redirect_uri"]  = redirectUri,
                    ["code_verifier"] = verifier
                };

                if (provider.ClientSecret.Length > 0) fields["client_secret"] = provider.ClientSecret;

                using var content = new FormUrlEncodedContent(fields);
                using var response = await client.PostAsync(provider.TokenUrl, content, cancel).ConfigureAwait(false);
                string body = await response.Content.ReadAsStringAsync(cancel).ConfigureAwait(false);
                var json = JObject.Parse(body);

                if (!response.IsSuccessStatusCode)
                    return new DeviceFlowResult
                    {
                        Error = Lang.T("oauth.startFailed",
                            json.Value<string>("error_description") ?? body)
                    };

                return new DeviceFlowResult
                {
                    RefreshToken = json.Value<string>("refresh_token") ?? "",
                    AccessToken  = json.Value<string>("access_token") ?? "",
                    Account      = OAuthTokens.AccountFrom(json.Value<string>("id_token"))
                };
            }
            catch (OperationCanceledException)
            {
                return new DeviceFlowResult { Error = Lang.T("oauth.cancelled") };
            }
            catch (Exception ex)
            {
                return new DeviceFlowResult { Error = Lang.T("oauth.startFailed", ex.Message) };
            }
            finally
            {
                try { listener?.Stop(); } catch { }
            }
        }

        private static async Task<Dictionary<string, string>?> ReadCallbackAsync(
            TcpListener listener, CancellationToken cancel)
        {
            using var registration = cancel.Register(() => { try { listener.Stop(); } catch { } });

            using var socket = await listener.AcceptTcpClientAsync(cancel).ConfigureAwait(false);
            using var stream = socket.GetStream();

            var buffer = new byte[8192];
            int read = await stream.ReadAsync(buffer, cancel).ConfigureAwait(false);
            string request = Encoding.UTF8.GetString(buffer, 0, read);

            string page = "<!doctype html><html lang=\"pl\"><meta charset=\"utf-8\">"
                        + "<title>" + Lang.T("msg.title") + "</title>"
                        + "<body style=\"font-family:Segoe UI,Arial,sans-serif;background:#17181c;color:#e8eaf0;"
                        + "display:flex;align-items:center;justify-content:center;height:100vh;margin:0\">"
                        + "<p style=\"font-size:18px\">" + Lang.T("oauth.browserDone") + "</p></body></html>";
            byte[] pageBytes = Encoding.UTF8.GetBytes(page);
            byte[] header = Encoding.ASCII.GetBytes(
                "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\n"
                + "Content-Length: " + pageBytes.Length + "\r\nConnection: close\r\n\r\n");

            await stream.WriteAsync(header, cancel).ConfigureAwait(false);
            await stream.WriteAsync(pageBytes, cancel).ConfigureAwait(false);
            await stream.FlushAsync(cancel).ConfigureAwait(false);

            int lineEnd = request.IndexOf('\r');
            string line = lineEnd > 0 ? request[..lineEnd] : request;
            string[] parts = line.Split(' ');
            if (parts.Length < 2) return null;

            int mark = parts[1].IndexOf('?');
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (mark < 0) return result;

            foreach (string pair in parts[1][(mark + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = pair.IndexOf('=');
                if (eq <= 0) continue;
                result[Uri.UnescapeDataString(pair[..eq])] = Uri.UnescapeDataString(pair[(eq + 1)..]);
            }
            return result;
        }

        private static string RandomUrlSafe(int bytes)
        {
            var data = RandomNumberGenerator.GetBytes(bytes);
            return Base64Url(data);
        }

        private static string Challenge(string verifier) =>
            Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        private static string Base64Url(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
