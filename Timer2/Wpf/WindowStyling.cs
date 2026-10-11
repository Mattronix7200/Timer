using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TimerSys.Wpf
{

    internal static class WindowStyling
    {
        private const int DwmwaWindowCornerPreference = 33;

        private enum CornerPreference
        {
            Default = 0,
            DoNotRound = 1,
            Round = 2,
            RoundSmall = 3
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public static void UseSquareCorners(Window window) => UseCorners(window, ThemeManager.Rounded);

        public static void UseCorners(Window window, bool rounded)
        {
            void Apply()
            {
                try
                {
                    var handle = new WindowInteropHelper(window).Handle;
                    if (handle == IntPtr.Zero) return;
                    int value = (int)(rounded ? CornerPreference.Round : CornerPreference.DoNotRound);
                    DwmSetWindowAttribute(handle, DwmwaWindowCornerPreference, ref value, sizeof(int));
                }
                catch
                {

                }
            }

            if (window.IsLoaded) Apply();
            else window.SourceInitialized += (s, e) => Apply();
        }
    }
}
