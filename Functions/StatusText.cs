using System;
using System.Collections.Generic;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// The words shown on screen for a stored status. The stored value never changes (comparisons, SQL and old data keep working);
    /// only the text a person reads does. To rename another status on screen, add one line to <see cref="Shown"/>.
    /// </summary>
    public static class StatusText
    {
        private static readonly Dictionary<string, string> Shown = new(StringComparer.OrdinalIgnoreCase)
        {
            ["In Transit"] = "Current",
            ["In-Transit"] = "Current",
            ["Unpaid"] = "For Collection"
        };

        /// <summary>The text to show for a stored status. Anything without a mapping is returned as it is.</summary>
        public static string Display(string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return stored ?? "";
            return Shown.TryGetValue(stored.Trim(), out var shown) ? shown : stored;
        }

        /// <summary>The words for an invoice or statement status. A leftover Draft row reads "For Collection" when it has an amount, otherwise "Incomplete".</summary>
        public static string Billing(string? stored, decimal amount)
        {
            if (string.Equals(stored?.Trim(), "Draft", StringComparison.OrdinalIgnoreCase)) return amount > 0 ? "For Collection" : "Incomplete";
            return Display(stored);
        }

        /// <summary>Same as <see cref="Billing"/> for a status with a note after it ("Unpaid · overdue"): only the first word group is renamed.</summary>
        public static string BillingText(string? text, decimal amount)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            int dot = text.IndexOf(" · ", StringComparison.Ordinal);
            if (dot < 0) return Billing(text, amount);
            return Billing(text.Substring(0, dot), amount) + text.Substring(dot);
        }
    }
}
