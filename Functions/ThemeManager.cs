using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    public enum ThemeMode { Light, Dark, System }
    public enum SidebarStyle { Charcoal, Light, Accent }
    public sealed class AccentPreset
    {
        public string Name { get; }
        public Color Color { get; }
        public AccentPreset(string name, Color color) { Name = name; Color = color; }
    }
    /// <summary>Holds the user's appearance choices and persists them in SystemSettings.</summary>
    public static class ThemeManager
    {
        public static readonly Color DefaultAccent = Color.FromArgb(180, 24, 44);
        public static readonly IReadOnlyList<AccentPreset> Presets = new List<AccentPreset>
        {
            new AccentPreset("MJ Crimson", Color.FromArgb(180, 24, 44)),
            new AccentPreset("Ruby", Color.FromArgb(214, 40, 87)),
            new AccentPreset("Sunset", Color.FromArgb(234, 88, 12)),
            new AccentPreset("Gold", Color.FromArgb(202, 138, 4)),
            new AccentPreset("Emerald", Color.FromArgb(5, 150, 105)),
            new AccentPreset("Teal", Color.FromArgb(13, 148, 136)),
            new AccentPreset("Royal Blue", Color.FromArgb(37, 99, 235)),
            new AccentPreset("Indigo", Color.FromArgb(79, 70, 229)),
            new AccentPreset("Violet", Color.FromArgb(124, 58, 237)),
            new AccentPreset("Graphite", Color.FromArgb(68, 64, 60)),
        };
        public static ThemeMode Mode { get; private set; } = ThemeMode.Light;
        public static Color AccentColor { get; private set; } = DefaultAccent;
        public static SidebarStyle Sidebar { get; private set; } = SidebarStyle.Charcoal;
        public static bool IsDark { get; private set; }
        public static event EventHandler? ThemeChanged;
        public static void Load()
        {
            try
            {
                string mode = MJdatabase.GetSystemSetting("Theme.Mode", "Light");
                string accent = MJdatabase.GetSystemSetting("Theme.Accent", ToHex(DefaultAccent));
                string sidebar = MJdatabase.GetSystemSetting("Theme.Sidebar", "Charcoal");
                Mode = Enum.TryParse(mode, true, out ThemeMode m) ? m : ThemeMode.Light;
                AccentColor = TryParseHex(accent, out Color c) ? c : DefaultAccent;
                Sidebar = Enum.TryParse(sidebar, true, out SidebarStyle s) ? s : SidebarStyle.Charcoal;
            }
            catch
            {
                Mode = ThemeMode.Light; AccentColor = DefaultAccent; Sidebar = SidebarStyle.Charcoal;
            }
            Resolve();
        }
        public static void Apply(ThemeMode mode, Color accent, SidebarStyle sidebar, bool persist = true, bool quiet = false)
        {
            Mode = mode;
            AccentColor = Color.FromArgb(255, accent.R, accent.G, accent.B);
            Sidebar = sidebar;
            if (persist)
            {
                try
                {
                    MJdatabase.SetSystemSetting("Theme.Mode", Mode.ToString(), "Appearance: Light, Dark or System");
                    MJdatabase.SetSystemSetting("Theme.Accent", ToHex(AccentColor), "Appearance: accent colour");
                    MJdatabase.SetSystemSetting("Theme.Sidebar", Sidebar.ToString(), "Appearance: sidebar style");
                }
                catch { }
            }
            Resolve(quiet);
            if (!quiet) ThemeChanged?.Invoke(null, EventArgs.Empty);
        }
        public static void ResetToDefaults() => Apply(ThemeMode.Light, DefaultAccent, SidebarStyle.Charcoal);
        private static void Resolve(bool quiet = false)
        {
            IsDark = Mode == ThemeMode.Dark || (Mode == ThemeMode.System && SystemPrefersDark());
            Palette.Invalidate();
            AppTheme.Invalidate();
            if (!quiet) ApplyNativeColorMode();
        }
        public static bool SystemPrefersDark()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                object? v = key?.GetValue("AppsUseLightTheme");
                return v is int i && i == 0;
            }
            catch { return false; }
        }
        public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        public static bool TryParseHex(string? text, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.Trim().TrimStart('#');
            if (t.Length == 3) t = string.Concat(t[0], t[0], t[1], t[1], t[2], t[2]);
            if (t.Length != 6) return false;
            if (!int.TryParse(t, System.Globalization.NumberStyles.HexNumber, null, out int v)) return false;
            color = Color.FromArgb((v >> 16) & 255, (v >> 8) & 255, v & 255);
            return true;
        }
        public static Color Mix(Color a, Color b, double t)
        {
            t = Math.Clamp(t, 0, 1);
            return Color.FromArgb(
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }
        // ---------- Native window chrome ----------
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = false)]
        private static extern int SetPreferredAppMode(int mode);
        [DllImport("uxtheme.dll", EntryPoint = "#136", SetLastError = false)]
        private static extern void FlushMenuThemes();
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);
        public static void ApplyNativeColorMode()
        {
            try
            {
                if (Environment.OSVersion.Version.Build < 17763) return;
                SetPreferredAppMode(IsDark ? 2 : 3);
                FlushMenuThemes();
            }
            catch { }
        }
        /// <summary>Makes the Windows title bar follow the theme (dark title bar, caption colour on Windows 11).</summary>
        public static void StyleWindowChrome(Form form, Color? caption = null)
        {
            if (form == null) return;
            void Do()
            {
                try
                {
                    int dark = IsDark ? 1 : 0;
                    if (DwmSetWindowAttribute(form.Handle, 20, ref dark, sizeof(int)) != 0)
                        DwmSetWindowAttribute(form.Handle, 19, ref dark, sizeof(int));
                    Color cap = caption ?? AppTheme.Surface;
                    int colorRef = cap.R | (cap.G << 8) | (cap.B << 16);
                    DwmSetWindowAttribute(form.Handle, 35, ref colorRef, sizeof(int));
                    Color txt = AppTheme.TextPrimary;
                    int textRef = txt.R | (txt.G << 8) | (txt.B << 16);
                    DwmSetWindowAttribute(form.Handle, 36, ref textRef, sizeof(int));
                }
                catch { }
            }
            if (form.IsHandleCreated) Do();
            else form.HandleCreated += (s, e) => Do();
        }
        /// <summary>Dark scrollbars / native parts for a control when dark mode is on.</summary>
        public static void StyleNativeControl(Control c)
        {
            if (c == null) return;
            void Do()
            {
                try
                {
                    string theme = c is ComboBox ? (IsDark ? "DarkMode_CFD" : "CFD") : (IsDark ? "DarkMode_Explorer" : "Explorer");
                    SetWindowTheme(c.Handle, theme, null);
                }
                catch { }
            }
            if (c.IsHandleCreated) Do();
            else c.HandleCreated += (s, e) => Do();
        }
    }
    /// <summary>Semantic colour tokens for the active theme.</summary>
    public static class AppTheme
    {
        private static readonly Dictionary<string, Color> cache = new Dictionary<string, Color>();
        internal static void Invalidate() { lock (cache) cache.Clear(); }
        private static Color Get(string key, Func<Color> make)
        {
            lock (cache)
            {
                if (cache.TryGetValue(key, out Color c)) return c;
                c = make();
                cache[key] = c;
                return c;
            }
        }
        public static bool IsDark => ThemeManager.IsDark;
        public static Color Canvas => Get(nameof(Canvas), () => Palette.C(248, 250, 252));
        public static Color Surface => Get(nameof(Surface), () => Palette.White);
        public static Color SurfaceAlt => Get(nameof(SurfaceAlt), () => Palette.C(248, 250, 252));
        public static Color SurfaceHover => Get(nameof(SurfaceHover), () => Palette.C(241, 245, 249));
        public static Color InputBack => Get(nameof(InputBack), () => IsDark ? Palette.C(248, 250, 252) : Palette.White);
        public static Color Border => Get(nameof(Border), () => Palette.C(226, 232, 240));
        public static Color BorderStrong => Get(nameof(BorderStrong), () => Palette.C(203, 213, 225));
        public static Color TextPrimary => Get(nameof(TextPrimary), () => Palette.C(15, 23, 42));
        public static Color TextSecondary => Get(nameof(TextSecondary), () => Palette.C(51, 65, 85));
        public static Color TextMuted => Get(nameof(TextMuted), () => Palette.C(100, 116, 139));
        public static Color Accent => Get(nameof(Accent), () =>
        {
            Color a = ThemeManager.AccentColor;
            if (!IsDark) return a;
            var (L, C, h) = Oklch.FromColor(a);
            return Oklch.ToColor(Math.Max(L, 0.62), C * 0.95, h);
        });
        public static Color AccentHover => Get(nameof(AccentHover), () => Oklch.WithLightness(Accent, Oklch.FromColor(Accent).L + (IsDark ? 0.05 : -0.06)));
        public static Color AccentPressed => Get(nameof(AccentPressed), () => Oklch.WithLightness(Accent, Oklch.FromColor(Accent).L + (IsDark ? -0.04 : -0.11)));
        public static Color OnAccent => Get(nameof(OnAccent), () => BestTextOn(Accent));
        public static Color AccentSoft => Get(nameof(AccentSoft), () => ThemeManager.Mix(Surface, Accent, IsDark ? 0.20 : 0.09));
        public static Color AccentSoftStrong => Get(nameof(AccentSoftStrong), () => ThemeManager.Mix(Surface, Accent, IsDark ? 0.32 : 0.18));
        public static Color AccentText => Get(nameof(AccentText), () =>
        {
            var (L, C, h) = Oklch.FromColor(ThemeManager.AccentColor);
            return IsDark ? Oklch.ToColor(Math.Max(0.76, L), C * 0.85, h) : Oklch.ToColor(Math.Min(L, 0.50), C, h);
        });
        // Status colours
        public static Color Success => Get(nameof(Success), () => Palette.C(5, 150, 105));
        public static Color SuccessSoft => Get(nameof(SuccessSoft), () => Palette.C(220, 252, 231));
        public static Color SuccessText => Get(nameof(SuccessText), () => Palette.C(22, 101, 52));
        public static Color Warning => Get(nameof(Warning), () => Palette.C(217, 119, 6));
        public static Color WarningSoft => Get(nameof(WarningSoft), () => Palette.C(254, 243, 199));
        public static Color WarningText => Get(nameof(WarningText), () => Palette.C(180, 83, 9));
        public static Color Danger => Get(nameof(Danger), () => Palette.C(220, 38, 38));
        public static Color DangerHover => Get(nameof(DangerHover), () => Palette.C(185, 28, 28));
        public static Color DangerSoft => Get(nameof(DangerSoft), () => Palette.C(254, 226, 226));
        public static Color DangerText => Get(nameof(DangerText), () => Palette.C(185, 28, 28));
        public static Color Info => Get(nameof(Info), () => Palette.C(2, 132, 199));
        public static Color InfoSoft => Get(nameof(InfoSoft), () => Palette.C(224, 242, 254));
        public static Color InfoText => Get(nameof(InfoText), () => Palette.C(3, 105, 161));
        // Sidebar
        public static Color Sidebar => Get(nameof(Sidebar), () =>
        {
            switch (ThemeManager.Sidebar)
            {
                case SidebarStyle.Light: return Surface;
                case SidebarStyle.Accent:
                    {
                        var (L, C, h) = Oklch.FromColor(ThemeManager.AccentColor);
                        return Oklch.ToColor(IsDark ? 0.24 : 0.30, Math.Min(C, 0.11), h);
                    }
                default: return IsDark ? Oklch.ToColor(0.165, 0.005, 285) : Color.FromArgb(28, 25, 23);
            }
        });
        public static bool SidebarIsDark => Oklch.FromColor(Sidebar).L < 0.6;
        public static Color SidebarText => Get(nameof(SidebarText), () => SidebarIsDark ? Color.FromArgb(250, 250, 249) : TextPrimary);
        public static Color SidebarTextMuted => Get(nameof(SidebarTextMuted), () => SidebarIsDark ? ThemeManager.Mix(Sidebar, Color.White, 0.62) : TextMuted);
        public static Color SidebarHover => Get(nameof(SidebarHover), () => SidebarIsDark ? ThemeManager.Mix(Sidebar, Color.White, 0.07) : SurfaceHover);
        public static Color SidebarActive => Get(nameof(SidebarActive), () => SidebarIsDark ? ThemeManager.Mix(Sidebar, Color.White, 0.12) : AccentSoft);
        public static Color SidebarActiveText => Get(nameof(SidebarActiveText), () => SidebarIsDark ? Color.White : AccentText);
        public static Color SidebarIndicator => Get(nameof(SidebarIndicator), () =>
        {
            if (ThemeManager.Sidebar == SidebarStyle.Accent) return Color.White;
            var (L, C, h) = Oklch.FromColor(ThemeManager.AccentColor);
            return SidebarIsDark ? Oklch.ToColor(Math.Max(L, 0.60), C, h) : Accent;
        });
        public static Color SidebarDivider => Get(nameof(SidebarDivider), () => SidebarIsDark ? ThemeManager.Mix(Sidebar, Color.White, 0.10) : Border);
        // Grid
        public static Color GridHeaderBack => Get(nameof(GridHeaderBack), () => SurfaceAlt);
        public static Color GridHeaderText => Get(nameof(GridHeaderText), () => TextMuted);
        public static Color GridLine => Get(nameof(GridLine), () => IsDark ? Oklch.ToColor(0.29, 0.006, 285) : Color.FromArgb(240, 238, 236));
        public static Color GridSelection => Get(nameof(GridSelection), () => ThemeManager.Mix(Surface, Accent, IsDark ? 0.22 : 0.10));
        public static Color GridHover => Get(nameof(GridHover), () => IsDark ? Oklch.ToColor(0.26, 0.006, 285) : Color.FromArgb(250, 249, 248));
        public static Color FocusRing => Get(nameof(FocusRing), () => Accent);
        public static Color Shadow => Get(nameof(Shadow), () => IsDark ? Color.FromArgb(0, 0, 0) : Color.FromArgb(28, 25, 23));
        public static Color BestTextOn(Color bg)
        {
            Color light = Color.White;
            Color dark = Color.FromArgb(24, 22, 20);
            double cl = Oklch.Contrast(light, bg), cd = Oklch.Contrast(dark, bg);
            return cl >= 3.2 || cl >= cd ? light : dark;
        }
    }
    /// <summary>Brand images prepared for the current theme.</summary>
    public static class BrandAssets
    {
        private static readonly Dictionary<string, Image?> cache = new Dictionary<string, Image?>();
        private static string AssetPath(string name) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", name);
        private static Bitmap? LoadBitmap(string name)
        {
            try
            {
                string p = AssetPath(name);
                if (!File.Exists(p)) return null;
                using var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var img = Image.FromStream(fs);
                return new Bitmap(img);
            }
            catch { return null; }
        }
        /// <summary>Full logo (eagle + wordmark). On dark backgrounds the black lettering is turned white.</summary>
        public static Image? LogoFor(Color background)
        {
            bool dark = Oklch.FromColor(background).L < 0.6;
            string key = "logo-" + (dark ? "dark" : "light");
            lock (cache)
            {
                if (cache.TryGetValue(key, out Image? hit)) return hit;
                Bitmap? src = LoadBitmap("logo_transparent.png");
                if (src == null)
                {
                    src = LoadBitmap("logo.png");
                }
                Image? result = src;
                if (src != null && dark) result = RecolorDarkInk(src);
                if (src != null) result = Trim(result!);
                cache[key] = result;
                return result;
            }
        }
        /// <summary>Just the eagle head (right-hand part of the logo), used when the sidebar is collapsed.</summary>
        public static Image? EagleMark(Color background)
        {
            bool dark = Oklch.FromColor(background).L < 0.6;
            string key = "eagle-" + (dark ? "dark" : "light");
            lock (cache)
            {
                if (cache.TryGetValue(key, out Image? hit)) return hit;
                Image? full = null;
                Bitmap? src = LoadBitmap("logo_transparent.png");
                if (src != null)
                {
                    int x0 = (int)(src.Width * 0.70), y0 = 0, w = src.Width - x0, h = (int)(src.Height * 0.52);
                    var crop = src.Clone(new Rectangle(x0, y0, w, h), PixelFormat.Format32bppArgb);
                    full = Trim(crop);
                    src.Dispose();
                }
                cache[key] = full;
                return full;
            }
        }
        private static Bitmap RecolorDarkInk(Bitmap src)
        {
            var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp)) g.DrawImage(src, 0, 0, src.Width, src.Height);
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int bytes = Math.Abs(data.Stride) * bmp.Height;
            byte[] px = new byte[bytes];
            Marshal.Copy(data.Scan0, px, 0, bytes);
            for (int i = 0; i + 3 < bytes; i += 4)
            {
                byte b = px[i], g = px[i + 1], r = px[i + 2], a = px[i + 3];
                if (a == 0) continue;
                int max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
                bool neutral = max - min < 40;
                if (neutral && max < 120)
                {
                    int v = 250 - max / 3;
                    px[i] = (byte)v; px[i + 1] = (byte)v; px[i + 2] = (byte)(v - 1);
                }
                else if (!neutral && r > g + 40 && max < 150)
                {
                    // deep shadows of the red swoosh: lift them slightly so they don't disappear on charcoal
                    px[i + 2] = (byte)Math.Min(255, r + 35);
                }
            }
            Marshal.Copy(px, 0, data.Scan0, bytes);
            bmp.UnlockBits(data);
            return bmp;
        }
        private static Image Trim(Image img)
        {
            try
            {
                var bmp = img as Bitmap ?? new Bitmap(img);
                int minX = bmp.Width, minY = bmp.Height, maxX = -1, maxY = -1;
                var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int stride = Math.Abs(data.Stride);
                byte[] px = new byte[stride * bmp.Height];
                Marshal.Copy(data.Scan0, px, 0, px.Length);
                bmp.UnlockBits(data);
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        int i = y * stride + x * 4;
                        byte a = px[i + 3];
                        bool opaqueWhite = a > 0 && px[i] > 245 && px[i + 1] > 245 && px[i + 2] > 245;
                        if (a > 16 && !opaqueWhite)
                        {
                            if (x < minX) minX = x; if (x > maxX) maxX = x;
                            if (y < minY) minY = y; if (y > maxY) maxY = y;
                        }
                    }
                }
                if (maxX < 0) return bmp;
                var r = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
                return bmp.Clone(r, PixelFormat.Format32bppArgb);
            }
            catch { return img; }
        }
    }
    /// <summary>Small drawing helpers shared by the custom-painted controls.</summary>
    public static class Gfx
    {
        public static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = Math.Max(0.1f, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
            if (radius <= 0.5f)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
        public static GraphicsPath RoundedRect(Rectangle r, int radius) => RoundedRect(new RectangleF(r.X, r.Y, r.Width, r.Height), radius);
        public static void HighQuality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }
    }
}
