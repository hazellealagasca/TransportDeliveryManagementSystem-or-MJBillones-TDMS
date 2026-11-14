using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Input rules shared by several screens (client, company details): TIN formatting and check, email, contact person, phone shape, tidy text.
    /// Pure functions, so they can be checked without opening a screen.
    /// </summary>
    public static class InputRules
    {
        private static readonly Regex PersonRx = new(@"^[\p{L} .,'\-’]+$", RegexOptions.Compiled);

        /// <summary>Trims and turns every run of spaces / line breaks into one space.</summary>
        public static string Tidy(string? text) => Regex.Replace((text ?? "").Trim(), @"\s+", " ");

        /// <summary>Digits only (at most 12), shown as 000-000-000-000 while typing.</summary>
        public static string FormatTin(string? text)
        {
            string raw = new string((text ?? "").Where(char.IsDigit).ToArray());
            if (raw.Length > 12) raw = raw.Substring(0, 12);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && i % 3 == 0 && i <= 9) sb.Append('-');
                sb.Append(raw[i]);
            }
            return sb.ToString();
        }

        /// <summary>Re-formats a TIN text box as you type and keeps the caret sensible.</summary>
        public static void FormatTinBox(System.Windows.Forms.TextBox box)
        {
            string formatted = FormatTin(box.Text);
            if (box.Text != formatted)
            {
                int cursor = box.SelectionStart;
                box.Text = formatted;
                box.SelectionStart = Math.Min(cursor + 1, box.Text.Length);
            }
        }

        /// <summary>Problem with a TIN, or null when fine. A blank TIN is a problem only when required.</summary>
        public static string? CheckTin(string? tin, bool required, string who = "The TIN")
        {
            int n = (tin ?? "").Count(char.IsDigit);
            if (n == 0) return required ? $"{who} is required (e.g. 006-896-371-000)." : null;
            if (n != 9 && n != 12) return $"{who} should have 9 digits (or 12 with the branch code), e.g. 006-896-371-000.";
            return null;
        }

        /// <summary>Phone shape used by the client screen: digits with spaces, dashes, brackets, plus or slash; 7 to 13 digits. Blank is not checked here.</summary>
        public static bool PhoneShapeOk(string? phone)
        {
            string p = (phone ?? "").Trim();
            string digits = new string(p.Where(char.IsDigit).ToArray());
            return p.All(ch => char.IsDigit(ch) || " -()+/".IndexOf(ch) >= 0) && digits.Length >= 7 && digits.Length <= 13;
        }

        /// <summary>name@domain.tld: no spaces, one @, a dot-separated domain ending in at least two letters.</summary>
        public static bool IsValidEmail(string? email)
        {
            return Fmt.IsValidEmail(email);
        }

        /// <summary>A person's name as typed in a contact-person box: letters, spaces, period, comma (for "Last, First"), hyphen and apostrophe only (no digits).</summary>
        public static bool IsValidPersonName(string? name)
        {
            string n = (name ?? "").Trim();
            return n.Length > 0 && PersonRx.IsMatch(n) && n.Any(char.IsLetter);
        }
    }
}
