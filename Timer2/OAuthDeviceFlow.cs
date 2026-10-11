using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TimerSys
{

    internal sealed class DeviceCodePrompt
    {
        public string UserCode { get; init; } = "";
        public string VerificationUrl { get; init; } = "";
        public string Message { get; init; } = "";
    }

    internal sealed class DeviceFlowResult
    {
        public string RefreshToken { get; init; } = "";
        public string AccessToken { get; init; } = "";

        public string Account { get; init; } = "";
        public string? Error { get; init; }
        public bool Success => Error == null && RefreshToken.Length > 0;
    }

    internal static class OAuthTokens
    {

        public static string AccountFrom(string? idToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idToken)) return "";
                string[] parts = idToken.Split('.');
                if (parts.Length < 2) return "";

                string payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                var json = JObject.Parse(
                    System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload)));

                foreach (string claim in new[] { "email", "preferred_username", "upn" })
                {
                    string value = json.Value<string>(claim) ?? "";
                    if (value.Contains('@')) return value;
                }
            }
            catch { }
            return "";
        }
    }

    internal static class OAuthDeviceFlow
    {

        public const string MicrosoftDeviceCodeUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/devicecode";
        public const string MicrosoftTokenUrl      = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
        public const string MicrosoftSmtpScope     = "offline_access openid email https://outlook.office.com/SMTP.Send";

        public static async Task<DeviceFlowResult> AuthorizeAsync(
            string deviceCodeUrl,
            string tokenUrl,
            string clientId,
            string scope,
            Action<DeviceCodePrompt> onPrompt,
            CancellationToken cancel)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return new DeviceFlowResult { Error = Lang.T("oauth.noClientId") };

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

                using var startContent = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["scope"]     = scope
                });
                using var startResponse = await client.PostAsync(deviceCodeUrl, startContent, cancel)
                                                      .ConfigureAwait(false);
                string startBody = await startResponse.Content.ReadAsStringAsync(cancel).ConfigureAwait(false);
                if (!startResponse.IsSuccessStatusCode)
                    return new DeviceFlowResult { Error = Lang.T("oauth.startFailed", startBody) };

                var start = JObject.Parse(startBody);
                string deviceCode  = start.Value<string>("device_code") ?? "";
                string userCode    = start.Value<string>("user_code") ?? "";
                string verifyUrl   = start.Value<string>("verification_uri")
                                     ?? start.Value<string>("verification_url") ?? "";
                int interval       = start.Value<int?>("interval") ?? 5;
                int expiresIn      = start.Value<int?>("expires_in") ?? 900;
                string message     = start.Value<string>("message") ?? "";

                if (deviceCode.Length == 0 || userCode.Length == 0)
                    return new DeviceFlowResult { Error = Lang.T("oauth.startFailed", startBody) };

                onPrompt(new DeviceCodePrompt
                {
                    UserCode = userCode,
                    VerificationUrl = verifyUrl,
                    Message = message
                });

                DateTime deadline = DateTime.UtcNow.AddSeconds(expiresIn);
                while (DateTime.UtcNow < deadline)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, interval)), cancel).ConfigureAwait(false);

                    using var pollContent = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"]  = "urn:ietf:params:oauth:grant-type:device_code",
                        ["client_id"]   = clientId,
                        ["device_code"] = deviceCode
                    });
                    using var pollResponse = await client.PostAsync(tokenUrl, pollContent, cancel)
                                                         .ConfigureAwait(false);
                    string pollBody = await pollResponse.Content.ReadAsStringAsync(cancel).ConfigureAwait(false);
                    var poll = JObject.Parse(pollBody);

                    if (pollResponse.IsSuccessStatusCode)
                    {
                        return new DeviceFlowResult
                        {
                            RefreshToken = poll.Value<string>("refresh_token") ?? "",
                            AccessToken  = poll.Value<string>("access_token") ?? "",
                            Account      = OAuthTokens.AccountFrom(poll.Value<string>("id_token"))
                        };
                    }

                    string error = poll.Value<string>("error") ?? "";
                    switch (error)
                    {
                        case "authorization_pending":
                            continue;
                        case "slow_down":
                            interval += 5;
                            continue;
                        case "authorization_declined":
                            return new DeviceFlowResult { Error = Lang.T("oauth.declined") };
                        case "expired_token":
                            return new DeviceFlowResult { Error = Lang.T("oauth.expired") };
                        default:
                            return new DeviceFlowResult
                            {
                                Error = Lang.T("oauth.startFailed",
                                    poll.Value<string>("error_description") ?? pollBody)
                            };
                    }
                }

                return new DeviceFlowResult { Error = Lang.T("oauth.expired") };
            }
            catch (OperationCanceledException)
            {
                return new DeviceFlowResult { Error = Lang.T("oauth.cancelled") };
            }
            catch (Exception ex)
            {
                return new DeviceFlowResult { Error = Lang.T("oauth.startFailed", ex.Message) };
            }
        }
    }
}
