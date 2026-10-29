using System;
using System.Collections.Generic;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Who is free on the dispatch date being planned (not just right now). A truck, driver or aide can be booked on
    /// any date where they have no other dispatch or special trip; for today, anyone still on the road from an earlier
    /// trip counts as busy too. Trucks in maintenance / inactive and inactive crew are never offered.
    /// </summary>
    public partial class DispatchManagementView
    {
        private HashSet<int> bookedTrucks = new HashSet<int>();
        private HashSet<int> bookedCrew = new HashSet<int>();
        private readonly Dictionary<int, string> truckBookedBy = new Dictionary<int, string>();
        private readonly Dictionary<int, string> crewBookedBy = new Dictionary<int, string>();
        private DateTime bookingsDate = DateTime.MinValue;
        private static bool IsActiveTrip(string? s) => RowMenu.Is(s, "Scheduled", "Pending", "In Transit", "Rescheduled", "Dispatched", "Delayed");
        private void ComputeBookingsFor(DateTime date)
        {
            date = date.Date;
            bookingsDate = date;
            bookedTrucks = new HashSet<int>();
            bookedCrew = new HashSet<int>();
            truckBookedBy.Clear();
            crewBookedBy.Clear();
            int editingId = selectedDelivery?.Id ?? 0;
            void Book(int truckId, IEnumerable<int> crew, string why)
            {
                if (truckId > 0 && bookedTrucks.Add(truckId)) truckBookedBy[truckId] = why;
                foreach (var c in crew.Where(c => c > 0)) if (bookedCrew.Add(c)) crewBookedBy[c] = why;
            }
            foreach (var d in allDispatches)
            {
                if (d.Id == editingId || !IsActiveTrip(d.Status)) continue;
                bool sameDay = d.DispatchDate.Date == date;
                bool stillOut = date == DateTime.Today && d.DispatchDate.Date < date && RowMenu.Is(d.Status, "In Transit", "Delayed");
                if (!sameDay && !stillOut) continue;
                Book(d.TruckId, MJdatabase.GetDeliveryCrewIds(d).Select(x => x.CrewId), stillOut ? $"still on the road ({d.DispatchNumber})" : $"booked on {d.DispatchNumber}");
            }
            try
            {
                foreach (var st in MJdatabase.GetSpecialTrips().Where(s => s.DispatchDate.Date == date && RowMenu.Is(s.Status, "Pending", "Scheduled", "In Transit", "Rescheduled")))
                    Book(st.TruckId, new[] { st.DriverId }.Concat(st.AllAideIds), $"special trip {st.OrderReference}");
            }
            catch { }
        }
        private void EnsureBookings()
        {
            var date = dtpDispatchDate.Value.Date;
            if (bookingsDate != date) ComputeBookingsFor(date);
        }
        /// <summary>Active and not on another trip on the planned date.</summary>
        private bool CrewFree(Crew c)
        {
            EnsureBookings();
            // A driver whose license has expired by the dispatch date isn't offered for driving.
            if (c.IsDriverRole && c.LicenseExpiredOn(dtpDispatchDate.Value.Date)) return false;
            return c.IsActive && !bookedCrew.Contains(c.Id);
        }
        private bool TruckFree(Truck t)
        {
            EnsureBookings();
            return !MJdatabase.IsTruckBlockedStatus(t.Status) && !bookedTrucks.Contains(t.Id);
        }
        /// <summary>The planned date changed: rebuild the truck / driver / aide lists for that day, keeping picks that are still free.</summary>
        private void RefreshResourcesForDate()
        {
            int keepTruck = cbTruck.SelectedIndex >= 0 && cbTruck.SelectedIndex < trucks.Count ? trucks[cbTruck.SelectedIndex].Id : 0;
            int keepDriver = cbDriver.SelectedIndex >= 0 && cbDriver.SelectedIndex < drivers.Count ? drivers[cbDriver.SelectedIndex].Id : 0;
            ComputeBookingsFor(dtpDispatchDate.Value.Date);
            var dropped = new List<string>();
            if (keepTruck > 0 && truckBookedBy.TryGetValue(keepTruck, out var tw) && (selectedDelivery == null || selectedDelivery.TruckId != keepTruck))
                dropped.Add($"truck {allTrucks.FirstOrDefault(t => t.Id == keepTruck)?.PlateNumber} is {tw}");
            if (keepDriver > 0 && crewBookedBy.TryGetValue(keepDriver, out var dw) && (selectedDelivery == null || selectedDelivery.DriverId != keepDriver))
                dropped.Add($"driver {allDrivers.FirstOrDefault(c => c.Id == keepDriver)?.FullName} is {dw}");
            foreach (var a in selectedAideIds.Where(id => crewBookedBy.ContainsKey(id)).ToList())
            {
                dropped.Add($"aide {allAides.FirstOrDefault(c => c.Id == a)?.FullName} is {crewBookedBy[a]}");
                selectedAideIds.Remove(a);
            }
            bool was = isBindingForm;
            isBindingForm = true;
            try
            {
                PopulateTrucksDriversAndAides();
                int ti = trucks.FindIndex(t => t.Id == keepTruck);
                if (ti >= 0) cbTruck.SelectedIndex = ti;
                int di = drivers.FindIndex(c => c.Id == keepDriver);
                if (di >= 0) cbDriver.SelectedIndex = di;
            }
            finally { isBindingForm = was; }
            if (dropped.Count > 0)
                ToastHelper.ShowToast(this, $"On {dtpDispatchDate.Value:MMM d}: " + string.Join("; ", dropped) + ". Pick someone else.", ToastType.Warning);
        }
    }
}
