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

        /// <summary>
        /// True when a time out (12-hour "08:42 PM", or an older stored 24-hour HHMM) is earlier than the time in. Equal times, blanks and
        /// unreadable text are not "earlier". Used for the plant gate and for each store: it only raises the overnight question, never refuses.
        /// </summary>
        /// <summary>
        /// Nov 26: the truck reaches the plant gate after it leaves on the dispatch order, so its time in can't be earlier than the order's departure time.
        /// Earlier by less than 12 hours = wrong; earlier by 12 hours or more is the next day (left late in the evening, at the plant after midnight) and is fine.
        /// Blank or unreadable times are not judged here (the required / "must be a time" checks report those).
        /// </summary>
        public static bool TruckInBeforeDeparture(string? departure, string? truckIn)
        {
            if (!Fmt.TryParseTime(departure, out var dep) || !Fmt.TryParseTime(truckIn, out var tin)) return false;
            if (tin >= dep) return false;
            return (dep - tin) < TimeSpan.FromHours(12);
        }
        public static bool GateOutBeforeIn(string? timeIn, string? timeOut)
        {
            if (!Fmt.TryParseTime(timeIn, out var a) || !Fmt.TryParseTime(timeOut, out var b)) return false;
            return b < a;
        }
        /// <summary>
        /// Nov 17: which plant gate time a store's time (in or out) is earlier than: "in" (earlier than the truck time in), "out" (earlier than the truck time out,
        /// the truck leaves the gate before it reaches the stores), or null when it is fine. Blank or unreadable times are ignored (the required / "must be a time"
        /// checks report those); when only one gate time is readable, the store time is checked against that one.
        /// Timeline: a normal gate (out not earlier than in) compares the store time as it is, so it must be at or after the truck time out.
        /// An overnight gate (out earlier than in) puts the truck time out after midnight: truckOut' = out + 24h, and a store time earlier than the truck time in
        /// counts as after midnight too (store + 24h); it is wrong when store' &lt; truckOut'. So with in 10:00 PM, out 12:30 AM: 01:00 AM is fine, 11:00 PM and 12:15 AM are not.
        /// </summary>
        public static string? StoreTimeBeforePlantGate(string? truckIn, string? truckOut, string? storeTime)
        {
            if (!Fmt.TryParseTime(storeTime, out var s)) return null;
            bool hasIn = Fmt.TryParseTime(truckIn, out var a), hasOut = Fmt.TryParseTime(truckOut, out var b);
            if (hasIn && hasOut && b < a)
            {
                var day = TimeSpan.FromHours(24);
                var store = s < a ? s + day : s;
                return store < b + day ? "out" : null;
            }
            if (hasIn && s < a) return "in";
            if (hasOut && s < b) return "out";
            return null;
        }

        /// <summary>The short hint shown on a store time cell that is earlier than a plant gate time.</summary>
        public static string StoreBeforeGateHint(string which, string truckIn, string truckOut)
            => which == "in" ? $"Earlier than the truck time in at the plant gate ({Fmt.Time(truckIn)})" : $"Earlier than the truck time out at the plant gate ({Fmt.Time(truckOut)})";
        /// <summary>The question asked when a time out is earlier than the time in (plant gate and each store).</summary>
        public static string OvernightQuestion(string timeIn, string timeOut) => $"Time out ({Fmt.Time(timeOut)}) is earlier than time in ({Fmt.Time(timeIn)}). Is this an overnight trip?";
        public static string GateOvernightQuestion(string timeIn, string timeOut) => OvernightQuestion(timeIn, timeOut);
    }
}
