using System;
using System.Windows;
using System.Windows.Media;

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
using FontFamily = System.Windows.Media.FontFamily;
using FontStyle = System.Windows.FontStyle;
using MessageBox = System.Windows.MessageBox;
using Clipboard = System.Windows.Clipboard;
using Cursors = System.Windows.Input.Cursors;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;
using Size = System.Windows.Size;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace TimerSys.Wpf
{

    internal static class ThemeManager
    {
        public static bool IsDark { get; private set; }

        public static bool Rounded { get; private set; }

        public const string DefaultAccentDark = "#F2566F";
        public const string DefaultAccentLight = "#E8415C";

        public static CornerRadius Corner() => new(Rounded ? 6 : 0);


        public static void Apply(bool dark) => Apply(dark, "", Rounded);

        public static void Apply(bool dark, string accent, bool rounded)
        {
            IsDark = dark;
            Rounded = rounded;
            var r = Application.Current?.Resources;
            if (r == null) return;

            void Set(string key, string hex) => r[key] = new SolidColorBrush(Parse(hex));

            r["ControlCorner"] = new CornerRadius(rounded ? 6 : 0);
            r["CardCorner"] = new CornerRadius(rounded ? 10 : 0);

            if (dark)
            {
                Set("WindowBrush",       "#17181C");
                Set("ChromeBrush",       "#1E2026");
                Set("CardBrush",         "#22242B");
                Set("SurfaceBrush",      "#2B2E36");
                Set("SurfaceHoverBrush", "#353945");
                Set("SurfacePressBrush", "#3E4250");
                Set("InputBrush",        "#1B1D23");
                Set("PopupBrush",        "#24262E");
                Set("BorderBrush",       "#343845");
                Set("BorderStrongBrush", "#454A59");
                Set("TextBrush",         "#E8EAF0");
                Set("TextMutedBrush",    "#9AA0AE");
                Set("AccentBrush",       "#F2566F");
                Set("AccentHoverBrush",  "#F56B81");
                Set("AccentPressBrush",  "#D8445C");
                Set("AccentSoftBrush",   "#38242C");
                Set("ScrollThumbBrush",  "#4A4F5E");
                Set("SuccessBrush",      "#5BD69A");
                Set("WarningBrush",      "#F0B45E");
                Set("DangerBrush",       "#FF6B6B");
                Set("NavBrush",          "#1B1D23");
            }
            else
            {
                Set("WindowBrush",       "#F4F5F8");
                Set("ChromeBrush",       "#FFFFFF");
                Set("CardBrush",         "#FFFFFF");
                Set("SurfaceBrush",      "#F5F6FA");
                Set("SurfaceHoverBrush", "#E8EAF1");
                Set("SurfacePressBrush", "#DADDE6");
                Set("InputBrush",        "#FFFFFF");
                Set("PopupBrush",        "#FFFFFF");
                Set("BorderBrush",       "#D3D7E0");
                Set("BorderStrongBrush", "#C3C7D2");
                Set("TextBrush",         "#1F2228");
                Set("TextMutedBrush",    "#6B7280");
                Set("AccentBrush",       "#E8415C");
                Set("AccentHoverBrush",  "#F2566F");
                Set("AccentPressBrush",  "#CC3450");
                Set("AccentSoftBrush",   "#FDEDF0");
                Set("ScrollThumbBrush",  "#C3C7D2");
                Set("SuccessBrush",      "#1F9254");
                Set("WarningBrush",      "#B9770E");
                Set("DangerBrush",       "#C42B1C");
                Set("NavBrush",          "#FFFFFF");
            }

            ApplyAccent(r, accent, dark);
        }

        private static void ApplyAccent(ResourceDictionary r, string accent, bool dark)
        {
            string hex = accent.Trim();
            if (hex.Length == 0) hex = dark ? DefaultAccentDark : DefaultAccentLight;

            Color main;
            try { main = Parse(hex); }
            catch { main = Parse(dark ? DefaultAccentDark : DefaultAccentLight); }

            r["AccentBrush"] = new SolidColorBrush(main);
            r["AccentHoverBrush"] = new SolidColorBrush(Shift(main, dark ? 0.12 : 0.10));
            r["AccentPressBrush"] = new SolidColorBrush(Shift(main, -0.14));
            r["AccentSoftBrush"] = new SolidColorBrush(dark ? Blend(main, Parse("#17181C"), 0.78)
                                                            : Blend(main, Colors.White, 0.88));
        }

        private static Color Shift(Color color, double amount)
        {
            double factor = 1 + amount;
            byte Channel(byte value) => (byte)Math.Max(0, Math.Min(255, value * factor));
            return Color.FromRgb(Channel(color.R), Channel(color.G), Channel(color.B));
        }

        private static Color Blend(Color color, Color onto, double weight)
        {
            byte Mix(byte a, byte b) => (byte)(a * (1 - weight) + b * weight);
            return Color.FromRgb(Mix(color.R, onto.R), Mix(color.G, onto.G), Mix(color.B, onto.B));
        }

        private static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex)!;

        public static Brush Brush(string key) =>
            Application.Current?.Resources[key] as Brush ?? Brushes.Gray;

        public static Color ColorOf(string key) =>
            (Application.Current?.Resources[key] as SolidColorBrush)?.Color ?? Colors.Gray;
    }
}
