using System.Text.RegularExpressions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>Nov 12 validation audit (daily screens): small, testable rules for the dispatch number, plant gate times, sign-off names and payment reference.</summary>
    public static class DailyRules
    {
        public const int DispatchNumberMax = 30, ReferenceMax = 30, SignOffNameMax = 100;
        private static readonly Regex DispatchRx = new(@"^[A-Za-z0-9\-/]{1,30}$", RegexOptions.Compiled);
        private static readonly Regex RefRx = new(@"^[A-Za-z0-9\-]{1,30}$", RegexOptions.Compiled);
        private static readonly Regex NameRx = new(@"^\p{L}[\p{L} .'\-]{0,99}$", RegexOptions.Compiled);

        /// <summary>Trim and turn runs of spaces / line breaks into one space.</summary>
        public static string Tidy(string? s) => Regex.Replace((s ?? "").Trim(), @"\s+", " ");

        /// <summary>Trim and turn runs of spaces / tabs into one space (line breaks stay).</summary>
        public static string CollapseSpaces(string? s) => Regex.Replace((s ?? "").Trim(), @"[ 	]{2,}", " ");

        /// <summary>Dispatch number: letters, digits, "-" and "/", up to 30 characters.</summary>
        public static bool DispatchNumberOk(string? s) => DispatchRx.IsMatch((s ?? "").Trim());
        public const string DispatchNumberMessage = "Dispatch # can only have letters, digits, - and / (up to 30).";

        /// <summary>Payment reference: letters, digits and "-", up to 30 characters.</summary>
        public static bool PaymentReferenceOk(string? s) => RefRx.IsMatch((s ?? "").Trim());
        public const string PaymentReferenceMessage = "Payment reference can only have letters, digits and - (up to 30).";

        /// <summary>A person's name on a printed form: letters, spaces, period, hyphen, apostrophe; up to 100 characters.</summary>
        public static bool SignOffNameOk(string? s) => NameRx.IsMatch(Tidy(s));

        /// <summary>True when the plant gate time out (HHMM) is earlier than the time in. Equal times, blanks and bad formats are not "earlier".</summary>
        public static bool GateOutBeforeIn(string? timeIn, string? timeOut)
        {
            if (!TryHhmm(timeIn, out int a) || !TryHhmm(timeOut, out int b)) return false;
            return b < a;
        }
        private static bool TryHhmm(string? t, out int minutes)
        {
            minutes = 0;
            var m = Regex.Match((t ?? "").Trim(), @"^([01]\d|2[0-3])([0-5]\d)$");
            if (!m.Success) return false;
            minutes = int.Parse(m.Groups[1].Value) * 60 + int.Parse(m.Groups[2].Value);
            return true;
        }
        public static string GateOvernightQuestion(string timeIn, string timeOut) => $"Truck left the plant at {timeOut.Trim()}, before it came in at {timeIn.Trim()}. Is this an overnight trip?";
    }
}
