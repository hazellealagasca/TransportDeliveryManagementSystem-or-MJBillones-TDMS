using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// The one place for how dates, times and emails are shown and read. Display is always MM/dd/yyyy and hh:mm AM/PM.
    /// Stored values (SQL parameters, database text, file names) stay yyyy-MM-dd and are NOT written through here.
    /// </summary>
    public static class Fmt
    {
        public const string DateFormat = "MM/dd/yyyy";
        public const string TimeFormat = "hh:mm tt";

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ---- dates ----
        public static string Date(DateTime d) => d.ToString(DateFormat, Inv);
        public static string Date(DateTime? d) => d.HasValue ? Date(d.Value) : "";
        /// <summary>Date and time of day, e.g. 11/14/2026 08:42 PM.</summary>
        public static string DateTimeText(DateTime d) => d.ToString(DateFormat + " " + TimeFormat, Inv);
        public static string DateTimeText(DateTime? d) => d.HasValue ? DateTimeText(d.Value) : "";
        /// <summary>Short form for chips and headers, e.g. 11/14.</summary>
        public static string Short(DateTime d) => d.ToString("MM/dd", Inv);
        public static string Short(DateTime? d) => d.HasValue ? Short(d.Value) : "";
        public static string ShortDateTime(DateTime d) => d.ToString("MM/dd " + TimeFormat, Inv);
        public static string ShortDateTime(DateTime? d) => d.HasValue ? ShortDateTime(d.Value) : "";
        /// <summary>Weekday and date, e.g. Sat, 11/14/2026.</summary>
        public static string DayDate(DateTime d) => d.ToString("ddd", Inv) + ", " + Date(d);
        public static string DayDate(DateTime? d) => d.HasValue ? DayDate(d.Value) : "";
        public static string DayShort(DateTime d) => d.ToString("ddd", Inv) + ", " + Short(d);
        public static string DayShort(DateTime? d) => d.HasValue ? DayShort(d.Value) : "";

        private static readonly string[] DateInputs = { "MM/dd/yyyy", "M/d/yyyy", "MM/d/yyyy", "M/dd/yyyy", "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-ddTHH:mm:ss" };

        /// <summary>Reads MM/dd/yyyy, M/d/yyyy and the old stored yyyy-MM-dd.</summary>
        public static bool TryParseDate(string? s, out DateTime d)
        {
            d = default;
            string t = (s ?? "").Trim();
            if (t.Length == 0) return false;
            return DateTime.TryParseExact(t, DateInputs, Inv, DateTimeStyles.None, out d);
        }

        /// <summary>A stored date text (yyyy-MM-dd or older) shown as MM/dd/yyyy. Text that is not a date is returned as it was.</summary>
        public static string DateText(string? stored) => TryParseDate(stored, out var d) ? Date(d) : (stored ?? "").Trim();

        /// <summary>A date typed or shown as MM/dd/yyyy turned back into the stored yyyy-MM-dd. Text that is not a date is returned as it was.</summary>
        public static string StoredDate(string? shown) => TryParseDate(shown, out var d) ? d.ToString("yyyy-MM-dd", Inv) : (shown ?? "").Trim();

        private static readonly Regex IsoDateRx = new(@"\b(\d{4})-(\d{2})-(\d{2})\b", RegexOptions.Compiled);

        /// <summary>Every stored yyyy-MM-dd inside a line of text shown as MM/dd/yyyy ("2026-11-01 to 2026-11-09" becomes "11/01/2026 to 11/09/2026").</summary>
        public static string DatesIn(string? text) => IsoDateRx.Replace(text ?? "", m => $"{m.Groups[2].Value}/{m.Groups[3].Value}/{m.Groups[1].Value}");

        // ---- times ----
        private static readonly Regex Twelve = new(@"^(\d{1,2}):?(\d{2})?\s*([AP])\.?M?\.?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex Colon24 = new(@"^(\d{1,2})[:.](\d{2})$", RegexOptions.Compiled);
        private static readonly Regex Plain24 = new(@"^(\d{2})(\d{2})$", RegexOptions.Compiled);

        /// <summary>
        /// Reads h:mm AM/PM, hh:mm AM/PM, "842p" (hmm a/p), and the older stored 24-hour HH:mm and HHmm ("2042", "0139").
        /// Hours 1-12 with AM/PM, minutes 00-59.
        /// </summary>
        public static bool TryParseTime(string? s, out TimeSpan t)
        {
            t = default;
            string x = Regex.Replace((s ?? "").Trim(), @"\s+", " ");
            if (x.Length == 0) return false;
            var m = Twelve.Match(x);
            if (m.Success)
            {
                int h = int.Parse(m.Groups[1].Value, Inv);
                int min = m.Groups[2].Success ? int.Parse(m.Groups[2].Value, Inv) : 0;
                if (h < 1 || h > 12 || min > 59) return false;
                bool pm = char.ToUpperInvariant(m.Groups[3].Value[0]) == 'P';
                int h24 = h % 12 + (pm ? 12 : 0);
                t = new TimeSpan(h24, min, 0);
                return true;
            }
            m = Colon24.Match(x);
            if (!m.Success) m = Plain24.Match(x);
            if (m.Success)
            {
                int h = int.Parse(m.Groups[1].Value, Inv);
                int min = int.Parse(m.Groups[2].Value, Inv);
                if (h > 23 || min > 59) return false;
                t = new TimeSpan(h, min, 0);
                return true;
            }
            return false;
        }

        /// <summary>Stored HHmm, HH:mm or h:mm tt shown as hh:mm AM/PM. Blank stays blank; text that is not a time is returned as it was.</summary>
        public static string Time(string? stored)
        {
            string x = (stored ?? "").Trim();
            if (x.Length == 0) return "";
            return TryParseTime(x, out var t) ? Time(t) : x;
        }

        public static string Time(TimeSpan t) => new DateTime(2000, 1, 1).Add(t).ToString(TimeFormat, Inv);

        /// <summary>True when both times read and out is not before in, or when either is blank. Unreadable text is false.</summary>
        public static bool TimeOutOnOrAfter(string? timeIn, string? timeOut)
        {
            if (string.IsNullOrWhiteSpace(timeIn) || string.IsNullOrWhiteSpace(timeOut)) return true;
            return TryParseTime(timeIn, out var a) && TryParseTime(timeOut, out var b) && b >= a;
        }

        // ---- email ----
        private static readonly Regex EmailRx = new(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9](?:[A-Za-z0-9\-]*[A-Za-z0-9])?(?:\.[A-Za-z0-9](?:[A-Za-z0-9\-]*[A-Za-z0-9])?)*\.[A-Za-z]{2,}$", RegexOptions.Compiled);

        /// <summary>name@domain.tld: no spaces, one @, a dot-separated domain ending in at least two letters.</summary>
        public static bool IsValidEmail(string? s)
        {
            string e = (s ?? "").Trim();
            return e.Length <= 254 && EmailRx.IsMatch(e) && !e.Contains("..");
        }

        // ---- controls ----
        public static void ApplyDatePicker(DateTimePicker p)
        {
            p.Format = DateTimePickerFormat.Custom;
            p.CustomFormat = DateFormat;
        }

        /// <summary>
        /// 12-hour time box: digits and a/p are typed, the box shows hh:mm AM when you leave it; text that is not a time turns red
        /// (the ValidationHelper state) and an empty box is left alone.
        /// </summary>
        public static void ApplyTimeBox(TextBox tb)
        {
            tb.MaxLength = 8;
            tb.PlaceholderText = "e.g. 08:42 PM";
            tb.KeyPress += (s, e) =>
            {
                if (char.IsControl(e.KeyChar)) return;
                char c = char.ToUpperInvariant(e.KeyChar);
                if (char.IsDigit(c) || c == ':' || c == ' ' || c == 'A' || c == 'P' || c == 'M') e.KeyChar = c;
                else e.Handled = true;
            };
            tb.Leave += (s, e) =>
            {
                string text = tb.Text.Trim();
                if (text.Length == 0) { ValidationHelper.ApplyValidationState(tb, true, ""); return; }
                if (TryParseTime(text, out var t)) { tb.Text = Time(t); ValidationHelper.ApplyValidationState(tb, true, ""); }
                else ValidationHelper.ApplyValidationState(tb, false, "Enter a time like 08:42 PM.");
            };
            tb.TextChanged += (s, e) =>
            {
                if (tb.Tag as string == "InvalidInput" && TryParseTime(tb.Text, out _)) ValidationHelper.ApplyValidationState(tb, true, "");
            };
        }
    }
}
