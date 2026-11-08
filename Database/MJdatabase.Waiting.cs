using System;
using System.Collections.Generic;
using System.Linq;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>One trip that is finished and waits for its service invoice (plan section 4).</summary>
    public sealed class WaitingTripRow
    {
        /// <summary>"Dispatch", "Special trip" or "Cancelled (chargeable)".</summary>
        public string Kind { get; set; } = "Dispatch";
        public int DeliveryId { get; set; }
        public int SpecialTripId { get; set; }
        public string Number { get; set; } = "";
        public string ClientName { get; set; } = "";
        public DateTime Date { get; set; }
        public string Route { get; set; } = "";
        public int Crates { get; set; }
        public string TripReportNumber { get; set; } = "";
        public DateTime? TripReportDate { get; set; }
        /// <summary>The amount known up front (cancellation fee or special-trip charge); 0 when it comes from the rate matrix at invoice time.</summary>
        public decimal Expected { get; set; }
        /// <summary>Whole days since the trip report (or the trip date when there is no report).</summary>
        public int DaysWaiting { get; set; }
        public string Age => MJdatabase.WaitingAge(DaysWaiting);
    }

    /// <summary>One completed trip that is blocked because it has no finished trip report.</summary>
    public sealed class WaitingReportRow
    {
        public int DeliveryId { get; set; }
        public string Number { get; set; } = "";
        public string ClientName { get; set; } = "";
        public DateTime Date { get; set; }
        public string Route { get; set; } = "";
        public string ReportState { get; set; } = "";
        public string ReportNumber { get; set; } = "";
        public int DaysWaiting { get; set; }
    }

    public static partial class MJdatabase
    {
        /// <summary>A trip that has waited this many days turns amber (plan 17.12 phase 1).</summary>
        public const int WaitingAmberDays = 2;
        /// <summary>... and red from this many days.</summary>
        public const int WaitingRedDays = 3;

        /// <summary>"Normal", "Amber" or "Red" for a number of days waiting.</summary>
        public static string WaitingAge(int days) => days >= WaitingRedDays ? "Red" : days >= WaitingAmberDays ? "Amber" : "Normal";

        /// <summary>
        /// Every trip that can be given a service invoice now and has none: a completed dispatch with a finished trip report, a chargeable cancelled
        /// dispatch, or a completed special trip with its service invoice switched on (and its trip report when it needs one). A trip that has a
        /// live invoice (its own, or an old statement's), or sits on a live statement, is not listed. Oldest first.
        /// This is the one list every screen and count uses.
        /// </summary>
        public static List<WaitingTripRow> GetTripsWaitingForInvoice()
        {
            var rows = new List<WaitingTripRow>();
            var reportedIds = GetTripReportDeliveryIds();
            var reports = GetTripReportsByDelivery();
            var live = GetLiveInvoicesByDelivery();
            var onStatement = GetBilledDeliveryIds(0);
            var fees = GetChargeableCancelledFees();
            DateTime today = DateTime.Today;

            foreach (var d in GetDeliveries())
            {
                bool completed = string.Equals(d.Status, "Completed", StringComparison.OrdinalIgnoreCase) && reportedIds.Contains(d.Id);
                bool chargeable = string.Equals(d.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) && fees.ContainsKey(d.Id);
                if (!completed && !chargeable) continue;
                if (live.ContainsKey(d.Id) || onStatement.ContainsKey(d.Id)) continue;
                reports.TryGetValue(d.Id, out var tr);
                DateTime since = tr != null && tr.ReportDate > DateTime.MinValue ? tr.ReportDate : d.DispatchDate;
                rows.Add(new WaitingTripRow
                {
                    Kind = chargeable ? "Cancelled (chargeable)" : "Dispatch",
                    DeliveryId = d.Id,
                    Number = d.DispatchNumber,
                    ClientName = d.ClientName ?? "",
                    Date = d.DispatchDate,
                    Route = d.CleanRouteDisplay,
                    Crates = d.TotalCrates,
                    TripReportNumber = tr?.TripReportNumber ?? "",
                    TripReportDate = tr?.ReportDate,
                    Expected = chargeable ? fees[d.Id] : 0m,
                    DaysWaiting = Math.Max(0, (int)(today - since.Date).TotalDays)
                });
            }

            var links = GetSpecialTripLinks();
            var liveSpecial = GetLiveInvoicesBySpecialTrip();
            foreach (var t in GetSpecialTrips().Where(t => string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase) && t.WithServiceInvoice))
            {
                var l = links.TryGetValue(t.Id, out var lk) ? lk : new SpecialTripLinks();
                if (t.WithTripReport && !l.HasReport) continue;
                if (l.InvoiceId > 0 || liveSpecial.ContainsKey(t.Id)) continue;
                rows.Add(new WaitingTripRow
                {
                    Kind = "Special trip",
                    SpecialTripId = t.Id,
                    Number = t.OrderReference,
                    ClientName = t.Company ?? "",
                    Date = t.DispatchDate,
                    Route = t.DestinationAddress ?? "",
                    TripReportNumber = l.TripReportNumber,
                    Expected = t.TripCharge,
                    DaysWaiting = Math.Max(0, (int)(today - t.DispatchDate.Date).TotalDays)
                });
            }
            return rows.OrderByDescending(r => r.DaysWaiting).ThenBy(r => r.Number, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>Completed dispatches that cannot be billed yet because their trip report is missing or only a draft.</summary>
        public static List<WaitingReportRow> GetTripsWaitingForTripReport()
        {
            DateTime today = DateTime.Today;
            return GetDispatchesAwaitingReport().Select(a => new WaitingReportRow
            {
                DeliveryId = a.DeliveryId,
                Number = a.DispatchNumber,
                ClientName = a.ClientName,
                Date = a.DispatchDate,
                Route = a.Destination,
                ReportState = a.ReportState,
                ReportNumber = a.ReportNumber,
                DaysWaiting = Math.Max(0, (int)(today - a.DispatchDate.Date).TotalDays)
            }).OrderByDescending(r => r.DaysWaiting).ToList();
        }

        /// <summary>The number every screen shows for "trips waiting for their service invoice".</summary>
        public static int CountTripsWaitingForInvoice() => GetTripsWaitingForInvoice().Count;
    }
}
