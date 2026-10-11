using System;
using System.Security.Cryptography;
using System.Text;

namespace TimerSys
{

    internal sealed class OAuthProvider
    {
        public string AuthorizeUrl { get; init; } = "";
        public string TokenUrl { get; init; } = "";
        public string Scope { get; init; } = "";
        public string ClientId { get; init; } = "";

        public string ClientSecret { get; init; } = "";

        public string ExtraAuthParams { get; init; } = "";
    }

    internal static class OAuthClients
    {
        private const string MicrosoftIdPacked =
            "AkxlQ9XpZ/kdWj5WTOC4CPCUqslzfOHvi0a/pWe3E65XHzFD";
        private const string GoogleIdPacked =
            "DE4wENjhZvAGUj5fUa/6D+Kd9ZIhJrKyiFL48XC0AfEHRW1H3Lo2v1UHZAYN4+0Z4oOplyQn5K7dVf+kduYd9kEZb1XCszuk";
        private const string GoogleSecretPacked =
            "cjNCcryIeYZkCmkFGp30C/nD5r8II8iA52/Ul1PBMdxDRHI=";

        private static string? _microsoftId, _googleId, _googleSecret;

        public static string MicrosoftId => _microsoftId ??= Unpack(MicrosoftIdPacked);
        public static string GoogleId => _googleId ??= Unpack(GoogleIdPacked);
        public static string GoogleSecret => _googleSecret ??= Unpack(GoogleSecretPacked);

        public static string Microsoft => MicrosoftId;

        public static readonly OAuthProvider MicrosoftProvider = new()
        {
            AuthorizeUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
            TokenUrl     = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
            Scope        = "offline_access openid email https://outlook.office.com/SMTP.Send",
            ExtraAuthParams = "prompt=select_account"
        };

        public static readonly OAuthProvider GoogleProvider = new()
        {
            AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth",
            TokenUrl     = "https://oauth2.googleapis.com/token",
            Scope        = "openid email https://mail.google.com/",

            ExtraAuthParams = "access_type=offline&prompt=consent"
        };

        public static OAuthProvider? For(string? host)
        {
            if (MailProviders.IsMicrosoft(host))
                return With(MicrosoftProvider, MicrosoftId, "");

            if (MailProviders.IsGoogle(host))
                return With(GoogleProvider, GoogleId, GoogleSecret);

            return null;
        }

        private static OAuthProvider With(OAuthProvider source, string id, string secret) => new()
        {
            AuthorizeUrl = source.AuthorizeUrl,
            TokenUrl = source.TokenUrl,
            Scope = source.Scope,
            ExtraAuthParams = source.ExtraAuthParams,
            ClientId = id,
            ClientSecret = secret
        };

        private static string Unpack(string packed)
        {
            try
            {
                byte[] key = SHA256.HashData(Encoding.ASCII.GetBytes("TimerSys.OAuth.v1"));
                byte[] data = Convert.FromBase64String(packed);
                var result = new byte[data.Length];
                for (int i = 0; i < data.Length; i++)
                    result[i] = (byte)(data[i] ^ key[i % key.Length]);
                return Encoding.ASCII.GetString(result);
            }
            catch
            {
                return "";
            }
        }
    }
}
