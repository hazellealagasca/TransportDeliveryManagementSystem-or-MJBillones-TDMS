using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Translates the app's original hard-coded colours into the active theme.
    /// Every literal colour in the views goes through Pal.C(...), so one switch here re-skins the whole system:
    /// slate greys become the soft neutral ramp, blues become the chosen accent, and dark mode inverts lightness
    /// while keeping status colours (green/amber/red) recognisable.
    /// </summary>
    public static class Palette
    {
        private static readonly Dictionary<int, Color> cache = new Dictionary<int, Color>();
        private static readonly object gate = new object();
        public static Color White => C(255, 255, 255);
        public static Color Black => C(0, 0, 0);
        public static Color C(int r, int g, int b) => Map(Color.FromArgb(r, g, b));
        public static Color C(int a, int r, int g, int b) => Color.FromArgb(a, Map(Color.FromArgb(r, g, b)));
        public static Color C(int argb) => Map(Color.FromArgb(argb));
        public static Color C(int a, Color baseColor) => Color.FromArgb(a, baseColor);
        public static void Invalidate()
        {
            lock (gate) cache.Clear();
        }
        public static Color Map(Color c)
        {
            if (c.A == 0) return c;
            int key = c.ToArgb();
            lock (gate)
            {
                if (cache.TryGetValue(key, out Color hit)) return hit;
            }
            Color mapped = Color.FromArgb(c.A, Compute(c));
            lock (gate) cache[key] = mapped;
            return mapped;
        }
        private static readonly double[,] DarkNeutral =
        {
            { 0.0, 1.0 }, { 0.13, 0.985 }, { 0.208, 0.965 }, { 0.279, 0.92 }, { 0.372, 0.86 }, { 0.446, 0.78 },
            { 0.554, 0.68 }, { 0.711, 0.55 }, { 0.869, 0.37 }, { 0.929, 0.31 }, { 0.968, 0.27 }, { 0.984, 0.205 }, { 0.9999, 0.205 }
        };
        private static double Interp(double[,] pts, double x)
        {
            int n = pts.GetLength(0);
            if (x <= pts[0, 0]) return pts[0, 1];
            for (int i = 1; i < n; i++)
            {
                if (x <= pts[i, 0])
                {
                    double x0 = pts[i - 1, 0], y0 = pts[i - 1, 1], x1 = pts[i, 0], y1 = pts[i, 1];
                    return y0 + (y1 - y0) * (x - x0) / (x1 - x0);
                }
            }
            return pts[n - 1, 1];
        }
        public static double DarkLightness(double l) => l >= 0.9999 ? 0.235 : Interp(DarkNeutral, l);
        private static Color Compute(Color c)
        {
            var (L, C, h) = Oklch.FromColor(c);
            var (La, Ca, ha) = Oklch.FromColor(ThemeManager.AccentColor);
            bool neutral = C < 0.012 || (C < 0.062 && h >= 195 && h <= 295 && (L < 0.95 || C < 0.022));
            double L2, C2, h2;
            if (neutral)
            {
                if (L > 0.999) { L2 = 1; C2 = 0; h2 = 0; }
                else
                {
                    L2 = (L > 0.975 && L < 0.995) ? L - 0.014 : L;
                    C2 = 0.0035 + 0.004 * (1 - L2);
                    h2 = 70;
                }
            }
            else if (h >= 250 && h <= 285)
            {
                const double refL = 0.546, refC = 0.215;
                L2 = L <= refL ? L / refL * La : La + (L - refL) / (1 - refL) * (1 - La);
                C2 = C * Ca / refC;
                h2 = ha;
            }
            else
            {
                L2 = L; C2 = C; h2 = h;
            }
            if (!ThemeManager.IsDark) return Oklch.ToColor(L2, C2, h2);
            if (neutral || C2 < 0.03)
            {
                double ld = DarkLightness(neutral ? L : L2);
                return Oklch.ToColor(ld, ld < 0.5 ? 0.006 : 0.004, 285);
            }
            if (L2 > 0.8) return Oklch.ToColor(DarkLightness(L2), C2 * 0.5, h2);
            if (L2 >= 0.42) return Oklch.ToColor(Math.Max(L2, 0.62) + (L2 - 0.42) * 0.1, C2 * 0.92, h2);
            return Oklch.ToColor(Math.Min(0.86, DarkLightness(L2)), C2 * 0.7, h2);
        }
        // ---- Designer colours: remap a freshly built control tree once ----
        private static readonly ConditionalWeakTable<Control, object> remapped = new ConditionalWeakTable<Control, object>();
        public static void RemapTree(Control root)
        {
            if (root == null) return;
            // Pass 1: record which colours are explicitly different from the parent (ambient colours follow the parent).
            var plan = new List<(Control c, bool back, bool fore)>();
            void Collect(Control c, bool isRoot)
            {
                if (remapped.TryGetValue(c, out _)) return;
                remapped.Add(c, true);
                bool back = isRoot || c.Parent == null || c.BackColor != c.Parent.BackColor;
                bool fore = isRoot || c.Parent == null || c.ForeColor != c.Parent.ForeColor;
                plan.Add((c, back, fore));
                foreach (Control child in c.Controls) Collect(child, false);
            }
            Collect(root, true);
            // Pass 2: translate.
            foreach (var (c, back, fore) in plan)
            {
                try
                {
                    Color bc = c.BackColor, fc = c.ForeColor;
                    if (back && bc.A != 0) c.BackColor = FromDesigner(bc);
                    if (fore) c.ForeColor = FromDesigner(fc);
                    if (c is ButtonBase bb && bb.FlatStyle == FlatStyle.Flat)
                    {
                        var fa = bb.FlatAppearance;
                        if (!fa.BorderColor.IsEmpty) fa.BorderColor = FromDesigner(fa.BorderColor);
                        if (!fa.MouseOverBackColor.IsEmpty) fa.MouseOverBackColor = FromDesigner(fa.MouseOverBackColor);
                        if (!fa.MouseDownBackColor.IsEmpty) fa.MouseDownBackColor = FromDesigner(fa.MouseDownBackColor);
                    }
                    if (c is LinkLabel ll)
                    {
                        ll.LinkColor = AppTheme.AccentText;
                        ll.ActiveLinkColor = AppTheme.AccentHover;
                        ll.VisitedLinkColor = ll.LinkColor;
                    }
                    if (c is DataGridView grid)
                    {
                        grid.BackgroundColor = FromDesigner(grid.BackgroundColor);
                        grid.GridColor = FromDesigner(grid.GridColor);
                        RemapStyle(grid.DefaultCellStyle);
                        RemapStyle(grid.ColumnHeadersDefaultCellStyle);
                        RemapStyle(grid.RowHeadersDefaultCellStyle);
                        RemapStyle(grid.AlternatingRowsDefaultCellStyle);
                        RemapStyle(grid.RowsDefaultCellStyle);
                        foreach (DataGridViewColumn col in grid.Columns)
                        {
                            if (col.HasDefaultCellStyle) RemapStyle(col.DefaultCellStyle);
                        }
                    }
                }
                catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
            }
        }
        private static void RemapStyle(DataGridViewCellStyle? s)
        {
            if (s == null) return;
            if (!s.BackColor.IsEmpty) s.BackColor = FromDesigner(s.BackColor);
            if (!s.ForeColor.IsEmpty) s.ForeColor = FromDesigner(s.ForeColor);
            if (!s.SelectionBackColor.IsEmpty) s.SelectionBackColor = FromDesigner(s.SelectionBackColor);
            if (!s.SelectionForeColor.IsEmpty) s.SelectionForeColor = FromDesigner(s.SelectionForeColor);
        }
        private static Color FromDesigner(Color c)
        {
            if (c.IsEmpty || c.A == 0) return c;
            if (c.IsSystemColor)
            {
                if (c == SystemColors.Control || c == SystemColors.ButtonFace) return AppTheme.Canvas;
                if (c == SystemColors.Window) return AppTheme.Surface;
                if (c == SystemColors.ControlText || c == SystemColors.WindowText) return AppTheme.TextPrimary;
                return c;
            }
            return Map(Color.FromArgb(c.A, c.R, c.G, c.B));
        }
    }
    /// <summary>OKLab / OKLCh conversions (perceptual colour space used for theme maths).</summary>
    public static class Oklch
    {
        private static double S2L(double c) { c /= 255.0; return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4); }
        private static double L2S(double c) => c <= 0.0031308 ? 12.92 * c : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;
        public static (double L, double C, double h) FromColor(Color col)
        {
            double r = S2L(col.R), g = S2L(col.G), b = S2L(col.B);
            double l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
            double m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
            double s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);
            double L = 0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s;
            double A = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s;
            double B = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;
            double h = Math.Atan2(B, A) * 180 / Math.PI;
            if (h < 0) h += 360;
            return (L, Math.Sqrt(A * A + B * B), h);
        }
        private static (double r, double g, double b) Lin(double L, double C, double h)
        {
            double A = C * Math.Cos(h * Math.PI / 180), B = C * Math.Sin(h * Math.PI / 180);
            double l = L + 0.3963377774 * A + 0.2158037573 * B;
            double m = L - 0.1055613458 * A - 0.0638541728 * B;
            double s = L - 0.0894841775 * A - 1.2914855480 * B;
            l = l * l * l; m = m * m * m; s = s * s * s;
            return (4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
                    -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
                    -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s);
        }
        public static Color ToColor(double L, double C, double h)
        {
            L = Math.Clamp(L, 0, 1);
            var rgb = Lin(L, C, h);
            int guard = 0;
            while (guard++ < 60 && C > 0.0005 && (rgb.r < -0.0005 || rgb.r > 1.0005 || rgb.g < -0.0005 || rgb.g > 1.0005 || rgb.b < -0.0005 || rgb.b > 1.0005))
            {
                C *= 0.94;
                rgb = Lin(L, C, h);
            }
            int To(double v) => (int)Math.Round(Math.Clamp(L2S(Math.Clamp(v, 0, 1)), 0, 1) * 255);
            return Color.FromArgb(To(rgb.r), To(rgb.g), To(rgb.b));
        }
        public static double Luminance(Color c)
        {
            return 0.2126 * S2L(c.R) + 0.7152 * S2L(c.G) + 0.0722 * S2L(c.B);
        }
        public static double Contrast(Color a, Color b)
        {
            double la = Luminance(a), lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }
        public static Color WithLightness(Color c, double L)
        {
            var (_, C, h) = FromColor(c);
            return ToColor(L, C, h);
        }
    }
}
