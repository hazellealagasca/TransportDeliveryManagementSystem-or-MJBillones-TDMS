using System;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// The order a dispatch moves through: nothing after the trip (trip report, invoice, statement of account)
    /// can be started until the dispatch is Completed (a trip report can also be filed while it is on the road: finishing it completes the dispatch).
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
            if (HasDeparted(d.Status))
                return $"Dispatch {d.DispatchNumber} is still on the road (\"{d.Status}\").\n\n" +
                       $"It is completed by its trip report: file the trip report in Trip Reports and finish it. Then you can {nextStep}.";
            return $"Dispatch {d.DispatchNumber} is still \"{(string.IsNullOrWhiteSpace(d.Status) ? "Pending" : d.Status)}\" and hasn't left yet.\n\n" +
                   $"Once the truck has left, file its trip report (that completes the dispatch). Then you can {nextStep}.";
        }
        /// <summary>A trip report can be filed now: Completed (old data), or on the road and already left. A special trip (negative id) still needs Completed.</summary>
        public static bool CanFileTripReport(Delivery d) => d.Id < 0 ? IsCompleted(d.Status) : Database.MJdatabase.CanFileTripReport(d);
        /// <summary>
        /// The trip report step only: a trip report can be filed for a Completed dispatch (old data) or one that is on the road and has left.
        /// A special trip (negative id) still needs Completed. Null when the report can be filed.
        /// </summary>
        public static string? WhyTripReportBlocked(Delivery d)
        {
            if (CanFileTripReport(d)) return null;
            if (IsCancelled(d.Status))
                return $"Dispatch {d.DispatchNumber} was cancelled, so you can't file its trip report for it.";
            if (d.Id < 0)
                return $"Special trip {d.DispatchNumber} is still \"{d.Status}\". Mark it completed first, then file its trip report.";
            return $"Dispatch {d.DispatchNumber} is still \"{(string.IsNullOrWhiteSpace(d.Status) ? "Pending" : d.Status)}\" and hasn't left yet.\n\n" +
                   $"Its trip report can be filed once the truck has left (set departure {Database.MJdatabase.DepartureDateTime(d):MMM d, hh:mm tt}).";
        }
        /// <summary>True when a trip report can be filed now; otherwise explains why and returns false. Finishing the report completes a dispatch that is on the road.</summary>
        public static bool RequireTripReportReady(IWin32Window? owner, Delivery? d)
        {
            if (d == null) return false;
            string? why = WhyTripReportBlocked(d);
            if (why == null) return true;
            QuietBox.Show(owner, why, "Trip report not available yet", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        /// <summary>True when the dispatch is completed; otherwise explains why and returns false.</summary>
        public static bool RequireCompleted(IWin32Window? owner, Delivery? d, string nextStep)
        {
            if (d == null) return false;
            string? why = WhyBlocked(d, nextStep);
            if (why == null) return true;
            QuietBox.Show(owner, why, "Dispatch not completed yet", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        /// <summary>Incidents: always allowed once completed; on the road only after confirming; never before it leaves or once cancelled.</summary>
        public static bool AllowIncident(IWin32Window? owner, Delivery? d)
        {
            if (d == null) return false;
            if (IsCompleted(d.Status)) return true;
            if (IsCancelled(d.Status))
            {
                QuietBox.Show(owner, $"Dispatch {d.DispatchNumber} was cancelled, so no incident can be logged for it.", "Dispatch cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (HasDeparted(d.Status))
            {
                return MessageBox.Show(owner, $"Dispatch {d.DispatchNumber} is still \"{d.Status}\" — it isn't completed yet.\n\nLog an incident that happened on the road (accident, breakdown, damage in transit)?",
                    "Dispatch not completed yet", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
            }
            QuietBox.Show(owner, $"Dispatch {d.DispatchNumber} is still \"{d.Status}\" and hasn't left yet, so there is nothing to report.\n\nIncidents can be logged once the truck is on the road, or after the dispatch is completed.",
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
