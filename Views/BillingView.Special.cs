using System;
using System.Collections.Generic;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 6, 2026: a completed special trip with its trip report gets its service invoice here, like a dispatch: pick it and type
    /// the SI No. In the trip list it stands in as a dispatch with Id = −(special trip id); its amount is the trip charge.
    /// Opened from the special trip form, the invoice is locked to that trip.
    /// </summary>
    public partial class BillingView
    {
        private readonly Dictionary<int, SpecialTrip> billSpecials = new();

        /// <summary>A special trip as an entry of the trip list (Id = −trip id).</summary>
        public static Delivery SpecialTripStandIn(SpecialTrip t)
        {
            string dest = string.IsNullOrWhiteSpace(t.DestinationAddress) ? t.Address : t.DestinationAddress;
            return new Delivery
            {
                Id = -t.Id,
                DispatchNumber = t.OrderReference,
                ClientName = t.Company,
                DispatchDate = t.DispatchDate,
                Status = RowMenu.Is(t.Status, "Completed") ? "Completed" : t.Status,
                LoadingScheduleInfo = "Special trip · " + dest,
                TruckInfo = t.TruckPlate,
                DriverName = t.DriverName
            };
        }

        /// <summary>Completed special trips with a trip report and no service invoice yet.</summary>
        private List<Delivery> SpecialTripsToInvoice()
        {
            billSpecials.Clear();
            var list = new List<Delivery>();
            try
            {
                var links = MJdatabase.GetSpecialTripLinks();
                foreach (var t in MJdatabase.GetSpecialTrips().Where(t => RowMenu.Is(t.Status, "Completed")))
                {
                    if (!links.TryGetValue(t.Id, out var l) || !l.HasReport || l.InvoiceId > 0) continue;
                    billSpecials[t.Id] = t;
                    list.Add(SpecialTripStandIn(t));
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return list;
        }

        private decimal SpecialCharge(Delivery d)
        {
            if (d.Id >= 0) return 0;
            if (billSpecials.TryGetValue(-d.Id, out var t)) return t.TripCharge;
            try { return MJdatabase.GetSpecialTripById(-d.Id)?.TripCharge ?? 0; } catch (Exception ex) { ErrLog.Swallowed(ex); return 0; }
        }

        private static bool SpecialTripHasReport(int tripId)
        {
            try { return MJdatabase.GetSpecialTripLinks().TryGetValue(tripId, out var l) && l.HasReport; } catch (Exception ex) { ErrLog.Swallowed(ex); return false; }
        }
    }
}
