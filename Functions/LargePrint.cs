using System;
using System.Drawing;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Rule 16 large-print look (Settings → Appearance → "Large print", on by default): bigger bold text, taller rows
    /// and thicker borders on the redesigned screens. Screens ask for sizes here instead of hard-coding them.
    /// </summary>
    public static class LargePrint
    {
        public const string SettingKey = "UI_LargePrint";
        private static bool? cached;
        public static bool On
        {
            get
            {
                if (cached.HasValue) return cached.Value;
                try { cached = MJdatabase.GetSystemSetting(SettingKey, "1") != "0"; } catch { cached = true; }
                return cached.Value;
            }
        }
        public static void Set(bool on)
        {
            MJdatabase.SetSystemSetting(SettingKey, on ? "1" : "0", "Large print on the redesigned screens (rule 16)");
            cached = on;
        }
        /// <summary>Text size: the large value when large print is on, else the normal one.</summary>
        public static float Pt(float large, float normal) => On ? large : normal;
        /// <summary>Row / control height in 96-dpi pixels.</summary>
        public static int Px(int large, int normal) => On ? large : normal;
        public static int Border => On ? 2 : 1;
        public static Font Bold(float large, float normal) => FontCache.Get("Segoe UI", Pt(large, normal), FontStyle.Bold);
        public static Font Semibold(float large, float normal) => FontCache.Get("Segoe UI Semibold", Pt(large, normal), FontStyle.Regular);
    }
}
