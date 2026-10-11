using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace TimerSys.Wpf
{
    internal static class OwnerDim
    {
        private const double BlurRadius = 9;
        private const double DimOpacity = 0.5;

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Window, Brush> Backgrounds = new();

        public static void Attach(Window dialog)
        {
            dialog.Loaded += (s, e) => Dim(dialog.Owner);
            dialog.Closed += (s, e) => Restore(dialog.Owner);
        }

        private static void Dim(Window? owner)
        {
            if (owner?.Content is not UIElement root) return;
            if (root.Effect is BlurEffect) return;

            var blur = new BlurEffect
            {
                Radius = 0,
                KernelType = KernelType.Gaussian,
                RenderingBias = RenderingBias.Performance
            };
            root.Effect = blur;
            root.IsHitTestVisible = false;

            Backgrounds.Remove(owner);
            Backgrounds.Add(owner, owner.Background);
            owner.Background = new SolidColorBrush(ThemeManager.IsDark
                ? Color.FromRgb(0x0E, 0x0F, 0x12)
                : Color.FromRgb(0x8E, 0x93, 0x9B));

            blur.BeginAnimation(BlurEffect.RadiusProperty,
                new DoubleAnimation(0, BlurRadius, TimeSpan.FromMilliseconds(140)));
            root.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(1, DimOpacity, TimeSpan.FromMilliseconds(140)));
        }

        private static void Restore(Window? owner)
        {
            if (owner?.Content is not UIElement root) return;
            if (root.Effect is not BlurEffect blur) return;

            var fade = new DoubleAnimation(blur.Radius, 0, TimeSpan.FromMilliseconds(120));
            fade.Completed += (s, e) =>
            {
                root.Effect = null;
                root.IsHitTestVisible = true;
                if (Backgrounds.TryGetValue(owner, out var original))
                {
                    owner.Background = original;
                    Backgrounds.Remove(owner);
                }
            };
            blur.BeginAnimation(BlurEffect.RadiusProperty, fade);
            root.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(root.Opacity, 1, TimeSpan.FromMilliseconds(120)));
        }
    }
}
