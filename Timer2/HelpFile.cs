using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace TimerSys
{
    internal static class HelpFile
    {
        public const string FileName = "Pomoc.pdf";
        private const string ResourceName = "TimerSys.data.Pomoc.pdf";

        public static string Path =>
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", FileName);

        public static bool Ensure()
        {
            try
            {
                string target = Path;
                var asm = Assembly.GetExecutingAssembly();

                using var stream = asm.GetManifestResourceStream(ResourceName);
                if (stream == null) return File.Exists(target);

                if (File.Exists(target) && new FileInfo(target).Length == stream.Length) return true;

                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);

                string temporary = target + ".tmp";
                using (var file = File.Create(temporary)) stream.CopyTo(file);
                File.Move(temporary, target, true);
                return true;
            }
            catch
            {
                return File.Exists(Path);
            }
        }

        public static bool Open()
        {
            if (!Ensure()) return false;
            try
            {
                Process.Start(new ProcessStartInfo { FileName = Path, UseShellExecute = true });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
