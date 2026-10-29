using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Company logo chosen in Settings. Kept in SystemSettings (PNG as base64) so it travels with database backups and
    /// survives rebuilds; when none is set, the built-in logo in the Assets folder is used.
    /// </summary>
    public static partial class BrandAssets
    {
        private const string KeyLogo = "CustomLogoPng", KeyLighten = "CustomLogoLightenOnDark";
        public const int MaxSourceBytes = 10 * 1024 * 1024;
        private const int MaxSide = 1000;
        private static byte[]? customBytes;
        private static Bitmap? customBitmap;
        private static bool customLoaded;
        private static bool? lighten;
        /// <summary>Raised on the UI thread after the logo is changed or reset, so open screens can redraw it.</summary>
        public static event Action? LogoChanged;
        private static void EnsureCustomLoaded()
        {
            if (customLoaded) return;
            customLoaded = true;
            try
            {
                string b64 = MJdatabase.GetSystemSetting(KeyLogo, "");
                if (string.IsNullOrWhiteSpace(b64)) return;
                byte[] bytes = Convert.FromBase64String(b64);
                using var ms = new MemoryStream(bytes);
                using var img = Image.FromStream(ms);
                customBitmap = new Bitmap(img);
                customBytes = bytes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Logo] " + ex.Message);
                customBitmap = null; customBytes = null;
            }
        }
        public static bool HasCustomLogo { get { lock (cache) { EnsureCustomLoaded(); return customBitmap != null; } } }
        /// <summary>A fresh copy of the custom logo (the caller owns it), or null when the built-in logo is in use.</summary>
        public static Bitmap? CustomLogoCopy()
        {
            lock (cache)
            {
                EnsureCustomLoaded();
                return customBitmap == null ? null : new Bitmap(customBitmap);
            }
        }
        /// <summary>PNG bytes of the custom logo for printed forms, or null when the built-in logo is in use.</summary>
        public static byte[]? CustomLogoPngBytes()
        {
            lock (cache) { EnsureCustomLoaded(); return customBytes; }
        }
        /// <summary>On a dark sidebar, turn black or dark-grey lettering white so it stays readable.</summary>
        public static bool LightenCustomLogoOnDark
        {
            get
            {
                if (lighten == null)
                {
                    string v = "True";
                    try { v = MJdatabase.GetSystemSetting(KeyLighten, "True"); } catch { }
                    lighten = !v.Equals("False", StringComparison.OrdinalIgnoreCase) && v != "0";
                }
                return lighten.Value;
            }
            set
            {
                lighten = value;
                MJdatabase.SetSystemSetting(KeyLighten, value ? "True" : "False", "Company logo: lighten dark lettering on dark backgrounds");
                Changed();
            }
        }
        /// <summary>
        /// Checks and stores a new logo from an image file (PNG, JPG, BMP or GIF). Large images are scaled down to at most
        /// 1000 px on the longest side. Returns an error message, or null when the logo was saved.
        /// </summary>
        public static string? SetCustomLogo(string filePath)
        {
            try
            {
                var info = new FileInfo(filePath);
                if (!info.Exists) return "The file could not be found.";
                if (info.Length > MaxSourceBytes) return "The image is larger than 10 MB. Please choose a smaller file.";
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var img = Image.FromStream(fs);
                if (img.Width < 16 || img.Height < 16) return "The image is too small. Use a logo at least 16 × 16 pixels.";
                double scale = Math.Min(1.0, (double)MaxSide / Math.Max(img.Width, img.Height));
                int w = Math.Max(1, (int)Math.Round(img.Width * scale)), h = Math.Max(1, (int)Math.Round(img.Height * scale));
                using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.DrawImage(img, new Rectangle(0, 0, w, h));
                }
                using var ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Png);
                MJdatabase.SetSystemSetting(KeyLogo, Convert.ToBase64String(ms.ToArray()), "Company logo (PNG)");
                if (MJdatabase.GetSystemSetting(KeyLogo, "").Length == 0) return "The logo could not be saved to the database.";
                Changed();
                return null;
            }
            catch (OutOfMemoryException) { return "That file is not a picture the system can read. Use a PNG, JPG, BMP or GIF image."; }
            catch (ArgumentException) { return "That file is not a picture the system can read. Use a PNG, JPG, BMP or GIF image."; }
            catch (Exception ex) { return "The logo could not be changed: " + ex.Message; }
        }
        /// <summary>Goes back to the built-in M.J. Billones logo.</summary>
        public static void ResetToDefaultLogo()
        {
            MJdatabase.SetSystemSetting(KeyLogo, "", "Company logo (PNG)");
            Changed();
        }
        private static void Changed()
        {
            lock (cache)
            {
                // Old images may still be on screen until LogoChanged handlers swap them, so they are left to the GC.
                cache.Clear();
                customBitmap = null; customBytes = null; customLoaded = false;
            }
            LogoChanged?.Invoke();
        }
    }
}
