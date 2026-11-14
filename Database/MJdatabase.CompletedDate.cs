using System;
using System.Collections.Generic;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// The date a trip was completed, as the printables show it: the date of its (finished) trip report.
    /// A special trip with no report falls back to the date it was marked completed. A trip with neither has no
    /// completed date yet, so the caller keeps whatever date it already holds (the dispatch date).
    /// </summary>
    public sealed class TripCompletedDates
    {
        private readonly Dictionary<int, DateTime> byDelivery = new();
        private readonly Dictionary<int, DateTime> bySpecial = new();

        internal void AddReport(int deliveryId, int specialTripId, DateTime date)
        {
            if (specialTripId > 0) bySpecial[specialTripId] = date;
            else if (deliveryId > 0) byDelivery[deliveryId] = date;
        }
        internal void AddSpecialCompleted(int specialTripId, DateTime date)
        {
            if (specialTripId > 0 && !bySpecial.ContainsKey(specialTripId)) bySpecial[specialTripId] = date;
        }

        /// <summary>The completed date of a dispatch or of a special trip (give one id, the other 0), or null when it has none yet.</summary>
        public DateTime? Of(int deliveryId, int specialTripId)
        {
            if (specialTripId > 0) return bySpecial.TryGetValue(specialTripId, out var s) ? s : null;
            if (deliveryId > 0) return byDelivery.TryGetValue(deliveryId, out var d) ? d : null;
            return null;
        }

        /// <summary>The printed text (03/17/2026, like the statement sheet): the completed date, else <paramref name="fallback"/> shown the same way.</summary>
        public string Text(int deliveryId, int specialTripId, string? fallback)
        {
            var d = Of(deliveryId, specialTripId);
            if (d.HasValue) return Fmt.Date(d.Value);
            string fb = (fallback ?? "").Trim();
            return DateTime.TryParse(fb, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var p) ? Fmt.Date(p) : fb;
        }

        /// <summary>The stored form (yyyy-MM-dd) of the completed date, else <paramref name="fallback"/> as it is.</summary>
        public string Iso(int deliveryId, int specialTripId, string? fallback)
        {
            var d = Of(deliveryId, specialTripId);
            return d.HasValue ? d.Value.ToString("yyyy-MM-dd") : (fallback ?? "").Trim();
        }
    }

    public static partial class MJdatabase
    {
        /// <summary>Completed dates of every trip that has a finished trip report (and of completed special trips).</summary>
        public static TripCompletedDates GetTripCompletedDates()
        {
            var map = new TripCompletedDates();
            try
            {
                bool Finished(TripReport t) => !string.Equals(t.Status, TripReportStatus.Draft, StringComparison.OrdinalIgnoreCase);
                foreach (var kv in GetTripReportsByDelivery())
                    if (kv.Value.SpecialTripId <= 0 && Finished(kv.Value)) map.AddReport(kv.Key, 0, kv.Value.ReportDate.Date);
                foreach (var tr in GetTripReports().Where(t => t.SpecialTripId > 0 && Finished(t)).OrderBy(t => t.Id)) // newest report of a special trip wins
                    map.AddReport(0, tr.SpecialTripId, tr.ReportDate.Date);
                foreach (var t in GetSpecialTrips().Where(t => t.CompletedAt.HasValue))
                    map.AddSpecialCompleted(t.Id, t.CompletedAt!.Value.Date);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return map;
        }
    }
}
