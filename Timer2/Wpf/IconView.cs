using System;
using System.Collections.Generic;
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
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using FontStyle = System.Windows.FontStyle;
using Application = System.Windows.Application;
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

namespace TimerSys.Wpf
{

    internal sealed class IconView : FrameworkElement
    {
        public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
            nameof(Kind), typeof(Ico), typeof(IconView),
            new FrameworkPropertyMetadata(Ico.Info, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(
            nameof(Brush), typeof(Brush), typeof(IconView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
            nameof(Thickness), typeof(double), typeof(IconView),
            new FrameworkPropertyMetadata(7.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public Ico Kind
        {
            get => (Ico)GetValue(KindProperty);
            set => SetValue(KindProperty, value);
        }

        public Brush? Brush
        {
            get => (Brush?)GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }

        public double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        public IconView()
        {
            Width = 18;
            Height = 18;
            IsHitTestVisible = false;
            SnapsToDevicePixels = true;
        }

        protected override void OnRender(DrawingContext dc)
        {
            var brush = Brush ?? ThemeManager.Brush("TextBrush");
            double w = ActualWidth, h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            double scale = Math.Min(w, h) / 100.0;
            dc.PushTransform(new TranslateTransform((w - 100 * scale) / 2, (h - 100 * scale) / 2));
            dc.PushTransform(new ScaleTransform(scale, scale));

            var pen = new Pen(brush, Thickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap   = PenLineCap.Round,
                LineJoin     = PenLineJoin.Round
            };
            pen.Freeze();

            Draw(dc, Kind, pen, brush);

            dc.Pop();
            dc.Pop();
        }

        private static void Line(DrawingContext dc, Pen p, double x1, double y1, double x2, double y2) =>
            dc.DrawLine(p, new Point(x1, y1), new Point(x2, y2));

        private static void Poly(DrawingContext dc, Pen p, bool close, params double[] xy)
        {
            var figure = new PathFigure { StartPoint = new Point(xy[0], xy[1]), IsClosed = close, IsFilled = false };
            for (int i = 2; i + 1 < xy.Length; i += 2)
                figure.Segments.Add(new LineSegment(new Point(xy[i], xy[i + 1]), true));
            var geo = new PathGeometry();
            geo.Figures.Add(figure);
            geo.Freeze();
            dc.DrawGeometry(null, p, geo);
        }

        private static void FillPoly(DrawingContext dc, Brush b, params double[] xy)
        {
            var figure = new PathFigure { StartPoint = new Point(xy[0], xy[1]), IsClosed = true, IsFilled = true };
            for (int i = 2; i + 1 < xy.Length; i += 2)
                figure.Segments.Add(new LineSegment(new Point(xy[i], xy[i + 1]), true));
            var geo = new PathGeometry();
            geo.Figures.Add(figure);
            geo.Freeze();
            dc.DrawGeometry(b, null, geo);
        }

        private static void Rect(DrawingContext dc, Pen p, double x, double y, double w, double h, double r = 0) =>
            dc.DrawRoundedRectangle(null, p, new Rect(x, y, w, h), r, r);

        private static void FillRect(DrawingContext dc, Brush b, double x, double y, double w, double h) =>
            dc.DrawRectangle(b, null, new Rect(x, y, w, h));

        private static void Ellipse(DrawingContext dc, Pen p, double cx, double cy, double rx, double ry) =>
            dc.DrawEllipse(null, p, new Point(cx, cy), rx, ry);

        private static void FillEllipse(DrawingContext dc, Brush b, double cx, double cy, double rx, double ry) =>
            dc.DrawEllipse(b, null, new Point(cx, cy), rx, ry);

        private static void Arc(DrawingContext dc, Pen p, double cx, double cy, double r, double startDeg, double sweepDeg)
        {
            var start = new Point(cx + r * Math.Cos(startDeg * Math.PI / 180), cy + r * Math.Sin(startDeg * Math.PI / 180));
            double endDeg = startDeg + sweepDeg;
            var end = new Point(cx + r * Math.Cos(endDeg * Math.PI / 180), cy + r * Math.Sin(endDeg * Math.PI / 180));

            var figure = new PathFigure { StartPoint = start, IsClosed = false, IsFilled = false };
            figure.Segments.Add(new ArcSegment(end, new Size(r, r), 0,
                Math.Abs(sweepDeg) > 180, sweepDeg >= 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, true));
            var geo = new PathGeometry();
            geo.Figures.Add(figure);
            geo.Freeze();
            dc.DrawGeometry(null, p, geo);
        }

        private static void Draw(DrawingContext dc, Ico kind, Pen p, Brush b)
        {
            switch (kind)
            {
                case Ico.Idle:

                    Poly(dc, Thin(p, 7), false, 8, 62, 34, 62, 8, 88, 34, 88);
                    Poly(dc, Thin(p, 8), false, 40, 36, 70, 36, 40, 66, 70, 66);
                    Poly(dc, Thin(p, 6), false, 76, 6, 94, 6, 76, 24, 94, 24);
                    break;

                case Ico.Stopwatch:
                    Ellipse(dc, p, 50, 58, 32, 32);
                    Line(dc, p, 50, 12, 50, 24);
                    Line(dc, p, 38, 10, 62, 10);
                    Line(dc, Thin(p, 7), 50, 58, 50, 38);
                    Line(dc, Thin(p, 7), 50, 58, 66, 66);
                    break;

                case Ico.Calendar:
                    Rect(dc, p, 14, 22, 72, 66, 6);
                    Line(dc, p, 14, 42, 86, 42);
                    Line(dc, p, 32, 10, 32, 28);
                    Line(dc, p, 68, 10, 68, 28);
                    FillRect(dc, b, 28, 54, 14, 12);
                    FillRect(dc, b, 58, 54, 14, 12);
                    FillRect(dc, b, 28, 72, 14, 12);
                    break;

                case Ico.TaskList:
                    Poly(dc, Thin(p, 7), false, 12, 26, 20, 34, 34, 18);
                    Poly(dc, Thin(p, 7), false, 12, 54, 20, 62, 34, 46);
                    Line(dc, Thin(p, 7), 48, 28, 90, 28);
                    Line(dc, Thin(p, 7), 48, 56, 90, 56);
                    Line(dc, Thin(p, 7), 12, 84, 90, 84);
                    break;

                case Ico.Settings:
                case Ico.Gear:
                    Gear(dc, Thin(p, 6));
                    break;

                case Ico.Info:
                    Ellipse(dc, p, 50, 50, 38, 38);
                    FillEllipse(dc, b, 50, 32, 6, 6);
                    Line(dc, Thin(p, 11), 50, 46, 50, 72);
                    break;

                case Ico.Help:
                    Ellipse(dc, p, 50, 50, 38, 38);
                    Arc(dc, Thin(p, 8), 50, 40, 16, 170, 230);
                    Line(dc, Thin(p, 8), 50, 54, 50, 64);
                    FillEllipse(dc, b, 50, 79, 5.5, 5.5);
                    break;

                case Ico.Folder:
                    Poly(dc, Thin(p, 7), true, 10, 80, 10, 24, 40, 24, 48, 36, 90, 36, 90, 80);
                    break;

                case Ico.FolderOpen:
                    Poly(dc, Thin(p, 7), true, 10, 80, 10, 24, 40, 24, 48, 36, 90, 36, 90, 80);
                    Poly(dc, Thin(p, 7), false, 10, 80, 26, 52, 100, 52);
                    break;

                case Ico.ChevronDown:
                    Poly(dc, Thin(p, 10), false, 24, 38, 50, 64, 76, 38);
                    break;

                case Ico.Play:
                    FillPoly(dc, b, 26, 16, 84, 50, 26, 84);
                    break;

                case Ico.Stop:
                    FillRect(dc, b, 24, 24, 52, 52);
                    break;

                case Ico.Pause:
                    FillRect(dc, b, 26, 18, 16, 64);
                    FillRect(dc, b, 58, 18, 16, 64);
                    break;

                case Ico.Key:
                    Ellipse(dc, Thin(p, 8), 29, 51, 17, 17);
                    Line(dc, Thin(p, 8), 46, 51, 90, 51);
                    Line(dc, Thin(p, 8), 72, 51, 72, 68);
                    Line(dc, Thin(p, 8), 86, 51, 86, 64);
                    break;

                case Ico.Save:
                    Poly(dc, Thin(p, 7), true, 14, 14, 72, 14, 86, 28, 86, 86, 14, 86);
                    FillRect(dc, b, 32, 14, 34, 24);
                    Rect(dc, Thin(p, 7), 30, 54, 40, 32);
                    break;

                case Ico.Close:
                case Ico.Cross:
                    Line(dc, Thin(p, 10), 26, 26, 74, 74);
                    Line(dc, Thin(p, 10), 74, 26, 26, 74);
                    break;

                case Ico.Check:
                    Poly(dc, Thin(p, 12), false, 18, 52, 40, 74, 84, 24);
                    break;

                case Ico.Shield:
                    Poly(dc, Thin(p, 7), true, 50, 10, 86, 24, 86, 56, 50, 92, 14, 56, 14, 24);
                    break;

                case Ico.Bell:
                    Poly(dc, Thin(p, 7), true, 20, 70, 26, 60, 26, 42, 36, 20, 64, 20, 74, 42, 74, 60, 80, 70);
                    Arc(dc, Thin(p, 7), 50, 74, 10, 0, 180);
                    break;

                case Ico.Trash:
                    Line(dc, Thin(p, 7), 14, 26, 86, 26);
                    Poly(dc, Thin(p, 7), false, 24, 26, 30, 88, 70, 88, 76, 26);
                    Poly(dc, Thin(p, 7), false, 38, 26, 40, 14, 60, 14, 62, 26);
                    Line(dc, Thin(p, 7), 42, 42, 44, 72);
                    Line(dc, Thin(p, 7), 58, 42, 56, 72);
                    break;

                case Ico.Pencil:
                    Poly(dc, Thin(p, 7), true, 12, 88, 18, 64, 68, 14, 88, 34, 38, 84);
                    Line(dc, Thin(p, 7), 60, 22, 80, 42);
                    break;

                case Ico.Power:
                    Arc(dc, Thin(p, 9), 50, 54, 32, -60, 300);
                    Line(dc, Thin(p, 9), 50, 8, 50, 44);
                    break;

                case Ico.Plus:
                    Line(dc, Thin(p, 11), 50, 20, 50, 80);
                    Line(dc, Thin(p, 11), 20, 50, 80, 50);
                    break;

                case Ico.Camera:
                    Poly(dc, Thin(p, 7), true, 8, 84, 8, 30, 30, 30, 38, 16, 62, 16, 70, 30, 92, 30, 92, 84);
                    Ellipse(dc, Thin(p, 7), 50, 54, 14, 14);
                    break;

                case Ico.Globe:
                    Ellipse(dc, Thin(p, 7), 50, 50, 38, 38);
                    Line(dc, Thin(p, 7), 12, 50, 88, 50);
                    Ellipse(dc, Thin(p, 7), 50, 50, 18, 38);
                    break;

                case Ico.Speaker:
                    Poly(dc, Thin(p, 7), true, 10, 38, 30, 38, 52, 18, 52, 82, 30, 62, 10, 62);
                    Arc(dc, Thin(p, 7), 64, 50, 14, -60, 120);
                    Arc(dc, Thin(p, 7), 64, 50, 26, -60, 120);
                    break;

                case Ico.Sparkle:
                    FillPoly(dc, b, 44, 10, 54, 34, 78, 44, 54, 54, 44, 78, 34, 54, 10, 44, 34, 34);
                    FillPoly(dc, b, 78, 60, 83, 73, 96, 78, 83, 83, 78, 96, 73, 83, 60, 78, 73, 73);
                    break;

                case Ico.Note:
                    Poly(dc, Thin(p, 7), true, 20, 10, 62, 10, 80, 28, 80, 90, 20, 90);
                    Poly(dc, Thin(p, 7), false, 60, 10, 60, 30, 80, 30);
                    Line(dc, Thin(p, 7), 34, 48, 66, 48);
                    Line(dc, Thin(p, 7), 34, 64, 66, 64);
                    break;

                case Ico.Monitor:
                    Rect(dc, Thin(p, 7), 10, 18, 80, 54, 5);
                    Line(dc, Thin(p, 7), 36, 86, 64, 86);
                    Line(dc, Thin(p, 7), 50, 72, 50, 86);
                    break;

                case Ico.Minimize:
                    Line(dc, Thin(p, 6), 16, 50, 84, 50);
                    break;

                case Ico.Maximize:
                    Rect(dc, Thin(p, 6), 18, 18, 64, 64, 3);
                    break;

                case Ico.Restore:
                    Rect(dc, Thin(p, 6), 16, 30, 54, 54, 3);
                    Poly(dc, Thin(p, 6), false, 32, 30, 32, 16, 84, 16, 84, 68, 70, 68);
                    break;

                case Ico.Lock:
                    Rect(dc, Thin(p, 7), 20, 44, 60, 46, 6);
                    Arc(dc, Thin(p, 7), 50, 44, 18, 180, 180);
                    FillEllipse(dc, b, 50, 62, 5, 5);
                    Line(dc, Thin(p, 7), 50, 64, 50, 74);
                    break;

                case Ico.Download:
                    Line(dc, Thin(p, 9), 50, 12, 50, 60);
                    Poly(dc, Thin(p, 9), false, 28, 42, 50, 64, 72, 42);
                    Poly(dc, Thin(p, 9), false, 16, 82, 16, 90, 84, 90, 84, 82);
                    break;

                case Ico.Refresh:
                    Arc(dc, Thin(p, 9), 50, 50, 34, 60, 250);
                    Poly(dc, Thin(p, 9), false, 84, 24, 84, 48, 60, 48);
                    break;

                case Ico.Toggle:
                    Rect(dc, Thin(p, 7), 10, 30, 80, 40, 20);
                    FillEllipse(dc, b, 70, 50, 12, 12);
                    break;

                case Ico.Mail:
                    Rect(dc, Thin(p, 7), 10, 22, 80, 56, 6);
                    Poly(dc, Thin(p, 7), false, 12, 26, 50, 56, 88, 26);
                    break;

                case Ico.Moon:

                    MoonShape(dc, b);
                    break;

                case Ico.Sun:

                    Ellipse(dc, Thin(p, 8), 50, 50, 20, 20);
                    for (int i = 0; i < 8; i++)
                    {
                        double a = i * Math.PI / 4;
                        Line(dc, Thin(p, 8),
                            50 + 30 * Math.Cos(a), 50 + 30 * Math.Sin(a),
                            50 + 42 * Math.Cos(a), 50 + 42 * Math.Sin(a));
                    }
                    break;

                case Ico.Warning:

                    Poly(dc, Thin(p, 8), true, 50, 10, 94, 86, 6, 86);
                    Line(dc, Thin(p, 9), 50, 38, 50, 62);
                    FillEllipse(dc, b, 50, 76, 5.5, 5.5);
                    break;

                case Ico.Error:

                    Ellipse(dc, Thin(p, 8), 50, 50, 38, 38);
                    Line(dc, Thin(p, 10), 50, 26, 50, 56);
                    FillEllipse(dc, b, 50, 72, 6, 6);
                    break;

                case Ico.Menu:
                    Line(dc, Thin(p, 8), 16, 28, 84, 28);
                    Line(dc, Thin(p, 8), 16, 50, 84, 50);
                    Line(dc, Thin(p, 8), 16, 72, 84, 72);
                    break;

                case Ico.FlagPl:
                    FlagHalves(dc, Brushes.White, new SolidColorBrush(Color.FromRgb(0xDC, 0x14, 0x3C)));
                    break;

                case Ico.FlagEn:
                    FlagUs(dc);
                    break;
            }
        }

        private static Pen Thin(Pen source, double thickness)
        {
            var pen = new Pen(source.Brush, thickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap   = PenLineCap.Round,
                LineJoin     = PenLineJoin.Round
            };
            pen.Freeze();
            return pen;
        }

        private static void Gear(DrawingContext dc, Pen p)
        {
            const double cx = 50, cy = 50, rOut = 42, rIn = 30;
            const int teeth = 8;
            var points = new List<Point>();
            for (int i = 0; i < teeth; i++)
            {
                double a0 = i * 2 * Math.PI / teeth;
                double wdt = 2 * Math.PI / teeth * 0.26;
                points.Add(At(cx, cy, rIn, a0 - wdt * 1.9));
                points.Add(At(cx, cy, rOut, a0 - wdt));
                points.Add(At(cx, cy, rOut, a0 + wdt));
                points.Add(At(cx, cy, rIn, a0 + wdt * 1.9));
            }

            var figure = new PathFigure { StartPoint = points[0], IsClosed = true, IsFilled = false };
            for (int i = 1; i < points.Count; i++)
                figure.Segments.Add(new LineSegment(points[i], true));
            var geo = new PathGeometry();
            geo.Figures.Add(figure);
            geo.Freeze();
            dc.DrawGeometry(null, p, geo);
            dc.DrawEllipse(null, p, new Point(cx, cy), 13, 13);
        }

        private static void MoonShape(DrawingContext dc, Brush b)
        {
            var full = new EllipseGeometry(new Point(50, 50), 38, 38);
            var bite = new EllipseGeometry(new Point(70, 34), 34, 34);
            var moon = new CombinedGeometry(GeometryCombineMode.Exclude, full, bite);
            moon.Freeze();
            dc.DrawGeometry(b, null, moon);
        }

        private static void FlagHalves(DrawingContext dc, Brush top, Brush bottom)
        {
            var frame = new Rect(8, 22, 84, 56);
            dc.DrawRectangle(top, null, new Rect(8, 22, 84, 28));
            dc.DrawRectangle(bottom, null, new Rect(8, 50, 84, 28));
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(90, 0, 0, 0)), 4);
            pen.Freeze();
            dc.DrawRectangle(null, pen, frame);
        }

        private static void FlagUs(DrawingContext dc)
        {
            var red = new SolidColorBrush(Color.FromRgb(0xB2, 0x22, 0x34));
            var blue = new SolidColorBrush(Color.FromRgb(0x3C, 0x3B, 0x6E));
            red.Freeze();
            blue.Freeze();

            dc.DrawRectangle(Brushes.White, null, new Rect(8, 22, 84, 56));
            for (int i = 0; i < 7; i++)
                dc.DrawRectangle(red, null, new Rect(8, 22 + i * 8, 84, 4));
            dc.DrawRectangle(blue, null, new Rect(8, 22, 38, 28));

            var pen = new Pen(new SolidColorBrush(Color.FromArgb(90, 0, 0, 0)), 4);
            pen.Freeze();
            dc.DrawRectangle(null, pen, new Rect(8, 22, 84, 56));
        }

        private static Point At(double cx, double cy, double r, double a) =>
            new Point(cx + r * Math.Cos(a), cy + r * Math.Sin(a));
    }
}
