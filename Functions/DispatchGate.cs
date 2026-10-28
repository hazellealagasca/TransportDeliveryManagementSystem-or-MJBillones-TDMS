using System;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// The order a dispatch moves through: nothing after the trip (trip report, invoice, statement of account)
    /// can be started until a person has marked the dispatch Completed in Dispatch.
    /// Incidents are the one exception: they can be logged while the truck is on the road (an accident, a breakdown),
    /// but not before it has left.
    /// </summary>
    public static class DispatchGate
    {
        public static bool IsCompleted(string? status) => Is(status, "Completed") || Is(status, "Delivered");
        public static bool IsCancelled(string? status) => Is(status, "Cancelled");
        public static bool HasDeparted(string? status) => Is(status, "In Transit") || Is(status, "Delayed");
        public static bool IsCompleted(Delivery d) => IsCompleted(d.Status);
        /// <summary>The message to show when a dispatch isn't completed yet, or null when it is.</summary>
        public static string? WhyBlocked(Delivery d, string nextStep)
        {
            if (IsCompleted(d.Status)) return null;
            if (IsCancelled(d.Status))
                return $"Dispatch {d.DispatchNumber} was cancelled, so you can't {nextStep} for it.";
            return $"Dispatch {d.DispatchNumber} is still \"{(string.IsNullOrWhiteSpace(d.Status) ? "Pending" : d.Status)}\".\n\n" +
                   $"Mark it completed first: go to Dispatch, right-click the order and choose \"Mark order complete\" (or open it and click ✔ Complete). " +
                   $"Then you can {nextStep}.";
        }
        /// <summary>True when the dispatch is completed; otherwise explains why and returns false.</summary>
        public static bool RequireCompleted(IWin32Window? owner, Delivery? d, string nextStep)
        {
            if (d == null) return false;
            string? why = WhyBlocked(d, nextStep);
            if (why == null) return true;
            MessageBox.Show(owner, why, "Complete the dispatch first", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        /// <summary>Incidents: always allowed once completed; on the road only after confirming; never before it leaves or once cancelled.</summary>
        public static bool AllowIncident(IWin32Window? owner, Delivery? d)
        {
            if (d == null) return false;
            if (IsCompleted(d.Status)) return true;
            if (IsCancelled(d.Status))
            {
                MessageBox.Show(owner, $"Dispatch {d.DispatchNumber} was cancelled, so no incident can be logged for it.", "Dispatch cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (HasDeparted(d.Status))
            {
                return MessageBox.Show(owner, $"Dispatch {d.DispatchNumber} is still \"{d.Status}\" — it hasn't been marked completed.\n\nLog an incident that happened on the road (accident, breakdown, damage in transit)?",
                    "Dispatch not completed yet", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
            }
            MessageBox.Show(owner, $"Dispatch {d.DispatchNumber} is still \"{d.Status}\" and hasn't left yet, so there is nothing to report.\n\nIncidents can be logged once the truck is on the road, or after the dispatch is marked completed.",
                "Dispatch hasn't left", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        /// <summary>The latest copy of a dispatch from the database (a list can be minutes old).</summary>
        public static Delivery Fresh(Delivery d)
        {
            try { return (d.Id > 0 ? Database.MJdatabase.GetDeliveryById(d.Id) : null) ?? d; } catch { return d; }
        }
        private static bool Is(string? a, string b) => string.Equals((a ?? "").Trim(), b, StringComparison.OrdinalIgnoreCase);
    }
}
