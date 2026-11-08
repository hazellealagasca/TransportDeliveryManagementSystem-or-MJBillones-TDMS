using System;
using System.Text.RegularExpressions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>The kinds of typed numbers that must be unique (plan section 17).</summary>
    public enum NumberKind { SI, CR, DR, DSP }

    /// <summary>
    /// The one place that decides when two typed numbers are "the same number" (plan 17.3).
    /// <see cref="Of"/> is the strict key inside one kind; <see cref="Core"/> is the looser key used to spot the same digits on a different kind of document.
    /// </summary>
    public static class NumberKey
    {
        private static readonly Regex Separators = new(@"[\s\-/\.]", RegexOptions.Compiled);
        private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex TrailingDigits = new(@"^(.*?)(\d+)$", RegexOptions.Compiled);

        public static string Short(NumberKind k) => k.ToString();
        public static string Label(NumberKind k) => k switch
        {
            NumberKind.SI => "Service Invoice No.",
            NumberKind.CR => "Collection Receipt No.",
            NumberKind.DR => "Delivery receipt (DR) no.",
            _ => "Dispatch No."
        };

        /// <summary>
        /// SI: upper-case, drop spaces - / . , drop a leading SI followed by digits, drop leading zeros of the last digit run.
        /// CR: the same without the SI prefix rule. DR and DSP: exact (upper-case, outer spaces trimmed, inner spaces collapsed, nothing else).
        /// </summary>
        public static string Of(NumberKind kind, string? text)
        {
            string t = (text ?? "").Trim();
            switch (kind)
            {
                case NumberKind.SI:
                {
                    string k = Separators.Replace(t.ToUpperInvariant(), "");
                    if (k.Length > 2 && k.StartsWith("SI", StringComparison.Ordinal) && char.IsDigit(k[2])) k = k.Substring(2);
                    return StripZeros(k);
                }
                case NumberKind.CR:
                    return StripZeros(Separators.Replace(t.ToUpperInvariant(), ""));
                default:
                    return Spaces.Replace(t.ToUpperInvariant(), " ");
            }
        }

        /// <summary>Digits of a number without prefix, separators and leading zeros: SI-1001, 1001, DR-1001 and 01001 all give 1001.</summary>
        public static string Core(string? text)
        {
            string k = Regex.Replace((text ?? "").ToUpperInvariant(), "[^A-Z0-9]", "");
            foreach (var p in new[] { "DSP", "SI", "CR", "DR" })
                if (k.Length > p.Length && k.StartsWith(p, StringComparison.Ordinal) && char.IsDigit(k[p.Length])) { k = k.Substring(p.Length); break; }
            return StripZeros(k);
        }

        private static string StripZeros(string k)
        {
            var m = TrailingDigits.Match(k);
            if (!m.Success) return k;
            string d = m.Groups[2].Value.TrimStart('0');
            if (d.Length == 0) d = "0";
            return m.Groups[1].Value + d;
        }
    }
}
