using System;
using System.Globalization;
using System.Text.RegularExpressions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>Departure times are stored as text ("08:00 AM"). This reads the ways people type them and saves one format.</summary>
    public static class TimeText
    {
        private static readonly string[] Formats = { "hh:mm tt", "h:mm tt", "h:mmtt", "hh:mmtt", "h tt", "htt", "H:mm", "HH:mm", "HHmm", "Hmm", "h.mm tt", "H.mm" };
        /// <summary>"8am", "8:00 am", "0800", "20:30", "8.30 PM" → time of day. False when it isn't a time.</summary>
        public static bool TryParse(string? text, out TimeSpan time)
        {
            time = default;
            string t = (text ?? "").Trim().ToUpperInvariant().Replace("A.M.", "AM").Replace("P.M.", "PM");
            if (t.Length == 0) return false;
            t = Regex.Replace(t, @"\s+", " ");
            t = Regex.Replace(t, @"(\d)\s*(AM|PM)$", "$1 $2");
            if (DateTime.TryParseExact(t, Formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var d) ||
                DateTime.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out d))
            {
                time = d.TimeOfDay;
                return true;
            }
            return false;
        }
        /// <summary>Saves times as "08:00 AM". Text that isn't a time is kept as typed, so nothing is lost.</summary>
        public static string Normalize(string? text, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            return TryParse(text, out var t) ? DateTime.Today.Add(t).ToString("hh:mm tt", CultureInfo.InvariantCulture) : text.Trim();
        }
    }
}
