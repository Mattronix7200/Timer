using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TimerSys
{

    internal static class SoundCatalog
    {

        private static readonly (string File, string pl, string en)[] _items =
        {
            ("sound.wma",     "Domyślny",        "Default"),
            ("bells.mp3",     "Dzwonki",         "Bells"),
            ("calm.mp3",      "Spokojny",        "Calm"),
            ("classic.mp3",   "Klasyczny",       "Classic"),
            ("drums.mp3",     "Bębny",           "Drums"),
            ("electric.mp3",  "Elektroniczny",   "Electronic"),
            ("generic01.mp3", "Uniwersalny 1",   "Generic 1"),
            ("generic02.mp3", "Uniwersalny 2",   "Generic 2"),
            ("generic03.mp3", "Uniwersalny 3",   "Generic 3"),
            ("guitar.mp3",    "Gitara",          "Guitar"),
            ("piano.mp3",     "Pianino",         "Piano"),
            ("raindrop.mp3",  "Krople deszczu",  "Raindrop"),
            ("sunny.mp3",     "Słoneczny",       "Sunny"),
            ("warning.mp3",   "Ostrzeżenie",     "Warning"),
            ("whistle.mp3",   "Gwizdek",         "Whistle"),
        };

        public const string DefaultFile = "sound.wma";

        public static string SoundsDirectory =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds");

        public static IReadOnlyList<string> Files => _items.Select(i => i.File).ToList();

        public static string DisplayName(string file)
        {
            foreach (var i in _items)
            {
                if (string.Equals(i.File, file, StringComparison.OrdinalIgnoreCase))
                    return Lang.Current == Lang.EN ? i.en : i.pl;
            }
            return "";
        }

        public static bool IsBuiltIn(string? value) =>
            !string.IsNullOrEmpty(value) &&
            _items.Any(i => string.Equals(i.File, value, StringComparison.OrdinalIgnoreCase));

        public static string ResolvePath(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            value = value.Trim();
            if (Path.IsPathRooted(value)) return value;
            return Path.Combine(SoundsDirectory, value);
        }

        public static void EnsureExtracted()
        {
            try
            {
                string dir = SoundsDirectory;
                Directory.CreateDirectory(dir);
                var asm = Assembly.GetExecutingAssembly();

                foreach (var item in _items)
                {

                    if (!item.File.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) continue;

                    string dest = Path.Combine(dir, item.File);
                    if (File.Exists(dest)) continue;

                    using var src = asm.GetManifestResourceStream("TimerSys.Resources." + item.File);
                    if (src == null) continue;
                    using var fs = File.Create(dest);
                    src.CopyTo(fs);
                }
            }
            catch {  }
        }
    }
}
