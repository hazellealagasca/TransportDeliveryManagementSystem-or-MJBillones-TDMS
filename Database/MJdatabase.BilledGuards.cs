using System;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// A finished trip report whose trip has a live invoice is part of what was billed: it is neither edited nor voided until that
    /// invoice is cancelled (the invoice's own rules then ask for the statement or receipt to be dealt with first).
    /// One place answers the question for every path (the Trip Reports screen, SaveTripReport, the void in SoftDelete).
    /// A Draft is not locked: an invoice can't be made from a Draft, so it was never what was billed.
    /// </summary>
    public static partial class MJdatabase
    {
        /// <summary>
        /// The live invoice that bills the trip of this finished trip report, or null (not billed, a Draft, void, or unknown).
        /// Read from the stored row, so an old copy of the report on a screen can't hide the link.
        /// </summary>
        private static LiveTripInvoice? LiveInvoiceOfTripReport(int reportId, out string reportNumber)
        {
            reportNumber = "";
            if (reportId <= 0) return null;
            EnsureSpecialTripColumns();
            int deliveryId, specialTripId;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var c = conn.CreateCommand();
                c.CommandText = "SELECT COALESCE(TripReportNumber,''), COALESCE(DeliveryId,0), COALESCE(SpecialTripId,0), COALESCE(Status,'Completed'), COALESCE(IsVoid,0) FROM TripReports WHERE Id = @id";
                c.Parameters.AddWithValue("@id", reportId);
                using var r = c.ExecuteReader();
                if (!r.Read()) return null;
                reportNumber = r.GetString(0);
                deliveryId = Convert.ToInt32(r.GetValue(1));
                specialTripId = Convert.ToInt32(r.GetValue(2));
                bool isDraft = string.Equals(r.GetString(3), TripReportStatus.Draft, StringComparison.OrdinalIgnoreCase);
                bool isVoid = Convert.ToInt32(r.GetValue(4)) != 0;
                if (isDraft || isVoid) return null;
            }
            return GetLiveInvoiceForTrip(deliveryId, specialTripId);
        }

        private static string BilledReportMessage(string reportNumber, LiveTripInvoice live, string doing)
            => $"Trip report {reportNumber} is already billed on {live.Describe()}, so it can't be {doing}. " +
               "Cancel that invoice first (if it is on a statement or has a receipt, take its trip off the statement or void the receipt first). The report can then be changed again.";

        /// <summary>Empty when the trip report may be changed; otherwise why it may not (its trip is billed on a live invoice).</summary>
        public static string WhyTripReportIsLocked(TripReport tr)
        {
            if (tr == null || tr.Id <= 0) return "";
            var live = LiveInvoiceOfTripReport(tr.Id, out string no);
            return live == null ? "" : BilledReportMessage(string.IsNullOrWhiteSpace(no) ? tr.TripReportNumber : no, live, "changed");
        }
    }
}
