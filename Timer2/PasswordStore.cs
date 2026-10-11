using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TimerSys
{

    internal static class PasswordStore
    {
        private const int SaltSize   = 16;
        private const int HashSize   = 32;
        private const int Iterations = 120_000;

        public static string FilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "psd.dat");

        public static bool Exists()
        {
            try { return File.Exists(FilePath); }
            catch { return false; }
        }

        public static bool Save(string password)
        {
            try
            {
                byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
                byte[] hash = Derive(password, salt);
                string line = "v2:" + Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);

                string? dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(FilePath, line, Encoding.ASCII);
                return true;
            }
            catch { return false; }
        }

        public static bool Verify(string password)
        {
            string stored;
            try
            {
                if (!File.Exists(FilePath)) return false;
                stored = File.ReadAllText(FilePath).Trim();
            }
            catch { return false; }

            if (stored.Length == 0) return false;

            if (stored.StartsWith("v2:", StringComparison.Ordinal))
            {
                var parts = stored.Split(':');
                if (parts.Length != 3) return false;
                try
                {
                    byte[] salt     = Convert.FromBase64String(parts[1]);
                    byte[] expected = Convert.FromBase64String(parts[2]);
                    byte[] actual   = Derive(password, salt);
                    return CryptographicOperations.FixedTimeEquals(actual, expected);
                }
                catch { return false; }
            }

            if (string.Equals(stored, LegacyMd5(password), StringComparison.OrdinalIgnoreCase))
            {
                Save(password);
                return true;
            }
            return false;
        }

        private static byte[] Derive(string password, byte[] salt) =>
            Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password ?? ""), salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        private static string LegacyMd5(string input)
        {
            byte[] hash = MD5.HashData(Encoding.ASCII.GetBytes(input ?? ""));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }
}
