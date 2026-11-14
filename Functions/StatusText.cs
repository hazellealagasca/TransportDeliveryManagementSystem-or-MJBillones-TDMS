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
            ["In-Transit"] = "Current"
        };

        /// <summary>The text to show for a stored status. Anything without a mapping is returned as it is.</summary>
        public static string Display(string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return stored ?? "";
            return Shown.TryGetValue(stored.Trim(), out var shown) ? shown : stored;
        }
    }
}
