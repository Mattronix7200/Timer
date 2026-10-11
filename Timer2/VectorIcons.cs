using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace TimerSys
{

    public enum Ico
    {
        Idle, Stopwatch, Calendar, TaskList, Settings, Info,
        Gear, Folder, FolderOpen, ChevronDown, Play, Stop, Pause,
        Key, Save, Close, Check, Cross, Shield, Bell, Trash, Pencil,
        Power, Plus, Camera, Globe, Speaker, Sparkle, Note, Monitor,
        Lock, Download, Refresh, Help, Toggle, Mail,
        Minimize, Maximize, Restore,
        Moon, Sun, Warning, Error, Menu, FlagPl, FlagEn
    }

    internal static class VectorIcons
    {
        private static readonly Dictionary<(Ico, int, int), Image> _cache = new();

        public static Image Get(Ico kind, int size, Color color)
        {
            var key = (kind, size, color.ToArgb());
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode   = PixelOffsetMode.HighQuality;
                g.ScaleTransform(size / 100f, size / 100f);

                using var pen = new Pen(color, 8f)
                {
                    StartCap    = LineCap.Round,
                    EndCap      = LineCap.Round,
                    LineJoin    = LineJoin.Round,
                    MiterLimit  = 2f
                };
                using var brush = new SolidBrush(color);
                Draw(kind, g, pen, brush, color);
            }

            _cache[key] = bmp;
            return bmp;
        }

        public static Icon GetIcon(Ico kind, int size, Color color)
        {
            using var bmp = new Bitmap(Get(kind, size, color));
            return Icon.FromHandle(bmp.GetHicon());
        }

        public static void Clear()
        {
            foreach (var img in _cache.Values)
            {
                try { img.Dispose(); } catch { }
            }
            _cache.Clear();
        }

        private static void Draw(Ico kind, Graphics g, Pen p, Brush b, Color c)
        {
            switch (kind)
            {
                case Ico.Idle:       Idle(g, p, b); break;
                case Ico.Stopwatch:  Stopwatch(g, p, b); break;
                case Ico.Calendar:   Calendar(g, p, b); break;
                case Ico.TaskList:   TaskList(g, p, b); break;
                case Ico.Settings:   Gear(g, p, b); break;
                case Ico.Gear:       Gear(g, p, b); break;
                case Ico.Info:       Info(g, p, b); break;
                case Ico.Folder:     Folder(g, p, b, false); break;
                case Ico.FolderOpen: Folder(g, p, b, true); break;
                case Ico.ChevronDown: Chevron(g, p); break;
                case Ico.Play:       Play(g, b); break;
                case Ico.Stop:       Stop(g, b); break;
                case Ico.Pause:      Pause(g, b); break;
                case Ico.Key:        Key(g, p, b); break;
                case Ico.Save:       Save(g, p, b); break;
                case Ico.Close:      Close(g, p); break;
                case Ico.Check:      Check(g, p); break;
                case Ico.Cross:      Close(g, p); break;
                case Ico.Shield:     Shield(g, p, b); break;
                case Ico.Bell:       Bell(g, p, b); break;
                case Ico.Trash:      Trash(g, p); break;
                case Ico.Pencil:     Pencil(g, p); break;
                case Ico.Power:      Power(g, p); break;
                case Ico.Plus:       Plus(g, p); break;
                case Ico.Camera:     Camera(g, p, b); break;
                case Ico.Globe:      Globe(g, p); break;
                case Ico.Speaker:    Speaker(g, p, b); break;
                case Ico.Sparkle:    Sparkle(g, p, b); break;
                case Ico.Note:       Note(g, p); break;
                case Ico.Monitor:    Monitor(g, p); break;
                case Ico.Lock:       Lock(g, p, b); break;
                case Ico.Download:   Download(g, p); break;
                case Ico.Refresh:    Refresh(g, p); break;
                case Ico.Help:       Help(g, p, b); break;
                case Ico.Toggle:     Toggle(g, p, b); break;
                case Ico.Mail:       Mail(g, p); break;
            }
        }

        private static void Idle(Graphics g, Pen p, Brush b)
        {

            void Z(float x, float y, float size, float width)
            {
                using var pen = new Pen(p.Color, width)
                { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                g.DrawLines(pen, new[]
                {
                    new PointF(x, y),
                    new PointF(x + size, y),
                    new PointF(x, y + size),
                    new PointF(x + size, y + size)
                });
            }

            Z(8,  62, 26, 7f);
            Z(40, 36, 30, 8f);
            Z(76, 6,  18, 6f);
        }

        private static void Stopwatch(Graphics g, Pen p, Brush b)
        {
            g.DrawEllipse(p, 18, 26, 64, 64);
            g.DrawLine(p, 50, 12, 50, 24);
            g.DrawLine(p, 38, 10, 62, 10);
            using var thin = new Pen(p.Color, 7f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(thin, 50, 58, 50, 38);
            g.DrawLine(thin, 50, 58, 66, 66);
        }

        private static void Calendar(Graphics g, Pen p, Brush b)
        {
            g.DrawRectangle(p, 14, 22, 72, 66);
            g.DrawLine(p, 14, 42, 86, 42);
            g.DrawLine(p, 32, 10, 32, 28);
            g.DrawLine(p, 68, 10, 68, 28);
            g.FillRectangle(b, 28, 54, 14, 12);
            g.FillRectangle(b, 58, 54, 14, 12);
            g.FillRectangle(b, 28, 72, 14, 12);
        }

        private static void TaskList(Graphics g, Pen p, Brush b)
        {
            using var thin = new Pen(p.Color, 7f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.DrawLines(thin, new[] { new PointF(12, 26), new PointF(20, 34), new PointF(34, 18) });
            g.DrawLines(thin, new[] { new PointF(12, 54), new PointF(20, 62), new PointF(34, 46) });
            g.DrawLine(thin, 48, 28, 90, 28);
            g.DrawLine(thin, 48, 56, 90, 56);
            g.DrawLine(thin, 12, 84, 90, 84);
        }

        private static void Gear(Graphics g, Pen p, Brush b)
        {
            const float cx = 50, cy = 50, rOut = 42, rIn = 30;
            using var path = new GraphicsPath();
            const int teeth = 8;
            var pts = new List<PointF>();
            for (int i = 0; i < teeth; i++)
            {
                double a0 = i * 2 * Math.PI / teeth;
                double w = 2 * Math.PI / teeth * 0.26;
                pts.Add(Pt(cx, cy, rIn, a0 - w * 1.9));
                pts.Add(Pt(cx, cy, rOut, a0 - w));
                pts.Add(Pt(cx, cy, rOut, a0 + w));
                pts.Add(Pt(cx, cy, rIn, a0 + w * 1.9));
            }
            path.AddPolygon(pts.ToArray());
            using var thin = new Pen(p.Color, 6f) { LineJoin = LineJoin.Round };
            g.DrawPath(thin, path);
            g.DrawEllipse(thin, cx - 13, cy - 13, 26, 26);
        }

        private static PointF Pt(float cx, float cy, float r, double a) =>
            new PointF(cx + (float)(r * Math.Cos(a)), cy + (float)(r * Math.Sin(a)));

        private static void Info(Graphics g, Pen p, Brush b)
        {
            g.DrawEllipse(p, 12, 12, 76, 76);
            g.FillEllipse(b, 44, 26, 12, 12);
            using var thick = new Pen(p.Color, 11f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(thick, 50, 46, 50, 72);
        }

        private static void Help(Graphics g, Pen p, Brush b)
        {
            g.DrawEllipse(p, 12, 12, 76, 76);
            using var thin = new Pen(p.Color, 8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var path = new GraphicsPath();
            path.AddArc(34, 26, 32, 28, 170, 230);
            path.AddLine(50, 54, 50, 64);
            g.DrawPath(thin, path);
            g.FillEllipse(b, 44, 74, 11, 11);
        }

        private static void Folder(Graphics g, Pen p, Brush b, bool open)
        {
            using var thin = new Pen(p.Color, 7f) { LineJoin = LineJoin.Round };
            using var path = new GraphicsPath();
            path.AddLines(new[]
            {
                new PointF(10, 80), new PointF(10, 24), new PointF(40, 24),
                new PointF(48, 36), new PointF(90, 36), new PointF(90, 80)
            });
            path.CloseFigure();
            g.DrawPath(thin, path);
            if (open)
                g.DrawLines(thin, new[] { new PointF(10, 80), new PointF(26, 52), new PointF(100, 52) });
        }

        private static void Chevron(Graphics g, Pen p)
        {
            using var thin = new Pen(p.Color, 10f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.DrawLines(thin, new[] { new PointF(24, 38), new PointF(50, 64), new PointF(76, 38) });
        }

        private static void Play(Graphics g, Brush b)
        {
            using var path = new GraphicsPath();
            path.AddPolygon(new[] { new PointF(26, 16), new PointF(84, 50), new PointF(26, 84) });
            g.FillPath(b, path);
        }

        private static void Stop(Graphics g, Brush b)
        {
            using var path = new GraphicsPath();
            path.AddRectangle(new RectangleF(22, 22, 56, 56));
            g.FillPath(b, path);
        }

        private static void Pause(Graphics g, Brush b)
        {
            g.FillRectangle(b, 26, 18, 16, 64);
            g.FillRectangle(b, 58, 18, 16, 64);
        }

        private static void Key(Graphics g, Pen p, Brush b)
        {
            using var thin = new Pen(p.Color, 8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawEllipse(thin, 12, 34, 34, 34);
            g.DrawLine(thin, 44, 51, 90, 51);
            g.DrawLine(thin, 72, 51, 72, 68);
            g.DrawLine(thin, 86, 51, 86, 64);
        }

        private static void Save(Graphics g, Pen p, Brush b)
        {
            using var thin = new Pen(p.Color, 7f) { LineJoin = LineJoin.Round };
            using var path = new GraphicsPath();
            path.AddLines(new[]
            {
                new PointF(14, 14), new PointF(72, 14), new PointF(86, 28),
                new PointF(86, 86), new PointF(14, 86)
            });
            path.CloseFigure();
            g.DrawPath(thin, path);
            g.FillRectangle(b, 32, 14, 34, 24);
            g.DrawRectangle(thin, 30, 54, 40, 32);
        }

        private static void Close(Graphics g, Pen p)
        {
            using var thin = new Pen(p.Color, 10f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(thin, 24, 24, 76, 76);
            g.DrawLine(thin, 76, 24, 24, 76);
        }

        private static void Check(Graphics g, Pen p)
        {
            using var thin = new Pen(p.Color, 12f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.DrawLines(thin, new[] { new PointF(18, 52), new PointF(40, 74), new PointF(84, 24) });
        }

        private static void Shield(Graphics g, Pen p, Brush b)
        {
            using var thin = new Pen(p.Color, 7f) { LineJoin = LineJoin.Round };
            using var path = new GraphicsPath();
            path.AddLines(new[]
            {
                new PointF(50, 10), new PointF(86, 24), new PointF(86, 52)
            });
            path.AddBezier(86, 52, 86, 74, 68, 86, 50, 92);
            path.AddBezier(50, 92, 32, 86, 14, 74, 14, 52);
            path.AddLine(14, 52, 14, 24);
            path.CloseFigure();
            g.DrawPath(thin, path);
        }

        private static void Bell(Graphics g, Pen p, Brush b)
        {
            using var thin = new Pen(p.Color, 7f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var path = new GraphicsPath();
            path.AddLines(new[] { new PointF(20, 70), new PointF(26, 60) });
            path.AddLine(26, 60, 26, 44);
            path.AddBezier(26, 44, 26, 24, 36, 16, 50, 16);
            path.AddBezier(50, 16, 64, 16, 74, 24, 74, 44);
            path.AddLine(74, 44, 74, 60);
            path.AddLine(74, 60, 80, 70);
            path.CloseFigure();
            g.DrawPath(thin, path);
            g.DrawArc(thin, 40, 74, 20, 14, 0, 180);
        }

        private static void Trash(Graphics g, Pen p)
        {
            using var thin = new Pen(p.Color, 7f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(thin, 14, 26, 86, 26);
            g.DrawLines(thin, new[] { new PointF(24, 26), new PointF(30, 88), new PointF(70, 88), new PointF(76, 26) });
            g.DrawLines(thin, new[] { new PointF(38, 26), new PointF(40, 14), new PointF(60, 14), new PointF(62, 26) });
            g.DrawLine(thin, 42, 42, 44, 72);
            g.DrawLine(thin, 58, 42, 56, 72);
        }

        private static void Pencil(Graphics g, Pen p)
        {
            using var thin = new Pen(p.Color, 7f) { LineJoin = LineJoin.Round };
            using var path = new GraphicsPath();
            path.AddPolygon(new[] { new PointF(12, 88), new PointF(18, 64), new PointF(68, 14), new PointF(88, 34), new PointF(38, 84) });
            g.DrawPath(thin, path);
            g.DrawLine(thin, 60, 22, 80, 42);
        }

        private static void Power(Graphics g, Pen p)
        {
            using var thin = new Pen(p.Color, 9f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(thin, 18, 22, 64, 64, -60, 300);
            g.DrawLine(thin, 50, 8, 50, 44);
        }

        private static void Plus(Graphics g, Pen p)
        {
            using var thin = new Pen(p.Color, 11f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(thin, 50, 20, 50, 80);
            g.DrawLine(thin, 20, 50, 80, 50);
        }

        private static void Camera(Graphics g, Pen p, Brush b)
        {
            using var thin = new Pen(p.Color, 7f) { LineJoin = LineJoin.Round };
            using var path = new GraphicsPath();
            path.AddLines(new[]
            {
                new PointF(8, 84), new PointF(8, 30), new PointF(30, 30),
                new PointF(38, 16), new PointF(62, 16), new PointF(70, 30),
                new PointF(92, 30), new PointF(92, 84)
            });
            path.CloseFigure();
            g.DrawPath(thin, path);
            g.DrawEllipse(thin, 36, 40, 28, 28);
        }

        private static void Globe(Graphics g, Pen p)
        {
            using var thin = new Pen(p.Color, 7f);
            g.DrawEllipse(thin, 12, 12, 76, 76);
            g.DrawLine(thin, 12, 50, 88, 50);
            g.DrawArc(thin, 32, 12, 36, 76, 90, 180);
            g.DrawArc(thin, 32, 12, 36, 76, 270, 180);
        }

        private static void Speaker(Graphics g, Pen p, Brush b)
        {
            using var thin = new Pen(p.Color, 7f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var path = new GraphicsPath();
            path.AddPolygon(new[]
            {
                new PointF(10, 38), new PointF(30, 38), new PointF(52, 18),
                new PointF(52, 82), new PointF(30, 62), new PointF(10, 62)
            });
            g.DrawPath(thin, path);
            g.DrawArc(thin, 50, 32, 28, 36, -60, 120);
            g.DrawArc(thin, 56, 20, 40, 60, -60, 120);
        }

        private static void Sparkle(Graphics g, Pen p, Brush b)
        {
            using var path = new GraphicsPath();
            path.AddPolygon(new[]
            {
                new PointF(44, 10), new PointF(54, 34), new PointF(78, 44),
                new PointF(54, 54), new PointF(44, 78), new PointF(34, 54),
                new PointF(10, 44), new PointF(34, 34)
            });
            g.FillPath(b, path);
            using var small = new GraphicsPath();
            small.AddPolygon(new[]
            {
                new PointF(78, 60), new PointF(83, 73), new PointF(96, 78),
                new PointF(83, 83), new PointF(78, 96), new PointF(73, 83),
                new PointF(60, 78), new PointF(73, 73)
            });
            g.FillPath(b, small);
        }

        private static void Note(Graphics g, Pen p)
        {
            using var thin = new Pen(p.Color, 7f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var path = new GraphicsPath();
            path.AddLines(new[]
            {
                new PointF(20, 10), new PointF(62, 10), new PointF(80, 28),
                new PointF(80, 90), new PointF(20, 90)
            });
            path.CloseFigure();
            g.DrawPath(thin, path);
            g.DrawLines(thin, new[] { new PointF(60, 10), new PointF(60, 30), new PointF(80, 30) });
            g.DrawLine(thin, 34, 48, 66, 48);
            g.DrawLine(thin, 34, 64, 66, 64);
        }

        private static void Monitor(Graphics g, Pen p)
        {
            using var thin = new Pen(p.Color, 7f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawRectangle(thin, 10, 18, 80, 54);
            g.DrawLine(thin, 36, 86, 64, 86);
            g.DrawLine(thin, 50, 72, 50, 86);
        }

        private static void Lock(Graphics g, Pen p, Brush b)
        {
            using var thin = new Pen(p.Color, 7f) { LineJoin = LineJoin.Round };
            g.DrawRectangle(thin, 20, 44, 60, 46);
            g.DrawArc(thin, 32, 12, 36, 44, 180, 180);
            g.FillEllipse(b, 45, 60, 10, 10);
            g.DrawLine(thin, 50, 66, 50, 76);
        }

        private static void Download(Graphics g, Pen p)
        {
            using var thin = new Pen(p.Color, 9f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.DrawLine(thin, 50, 12, 50, 60);
            g.DrawLines(thin, new[] { new PointF(28, 42), new PointF(50, 64), new PointF(72, 42) });
            g.DrawLines(thin, new[] { new PointF(16, 82), new PointF(16, 90), new PointF(84, 90), new PointF(84, 82) });
        }

        private static void Refresh(Graphics g, Pen p)
        {
            using var thin = new Pen(p.Color, 9f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.DrawArc(thin, 16, 16, 68, 68, 60, 250);
            g.DrawLines(thin, new[] { new PointF(84, 24), new PointF(84, 48), new PointF(60, 48) });
        }

        private static void Mail(Graphics g, Pen p)
        {
            using var thin = new Pen(p.Color, 7f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawRectangle(thin, 10, 22, 80, 56);
            g.DrawLines(thin, new[] { new PointF(10, 26), new PointF(50, 56), new PointF(90, 26) });
        }

        private static void Toggle(Graphics g, Pen p, Brush b)
        {
            using var thin = new Pen(p.Color, 7f);
            using var path = new GraphicsPath();
            path.AddArc(8, 28, 44, 44, 90, 180);
            path.AddArc(48, 28, 44, 44, 270, 180);
            path.CloseFigure();
            g.DrawPath(thin, path);
            g.FillEllipse(b, 58, 38, 24, 24);
        }
    }
}
