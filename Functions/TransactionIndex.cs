using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>One row in Order History: a loading order (not dispatched yet), a dispatch or a special trip.</summary>
    public sealed class TxRecord
    {
        public const string KLoading = "Loading order", KDispatch = "Dispatch", KSpecial = "Special trip";
        public string Kind { get; set; } = "";
        public string Number { get; set; } = "";
        public DateTime Date { get; set; }
        public string Time { get; set; } = "";
        public string Client { get; set; } = "";
        public string Route { get; set; } = "";
        public string Truck { get; set; } = "";
        public string Driver { get; set; } = "";
        public string Aides { get; set; } = "";
        /// <summary>Number of aides (names contain commas, so this is counted from the crew ids).</summary>
        public int AideCount { get; set; }
        public int Crates { get; set; }
        public string Status { get; set; } = "";
        public string Cargo { get; set; } = "";
        public string Remarks { get; set; } = "";
        public decimal? Amount { get; set; }
        public string InvoiceNo { get; set; } = "";
        public object? Source { get; set; }
        public string Key => Kind + "|" + Number;
        public string Crew => string.Join(" · ", new[] { Driver, Aides }.Where(x => !string.IsNullOrWhiteSpace(x)));
        /// <summary>Waiting, On the road, Completed or Cancelled.</summary>
        public string StatusGroup
        {
            get
            {
                string s = (Status ?? "").Trim();
                if (DispatchGate.IsCancelled(s)) return "Cancelled";
                if (DispatchGate.IsCompleted(s)) return "Completed";
                if (RowMenu.Is(s, "In Transit", "In-Transit", "Delayed", "Dispatched", "On Trip")) return "On the road";
                return "Waiting";
            }
        }
        private string? search;
        /// <summary>Everything a person might remember about the record, lower-case, including the date written several ways.</summary>
        public string SearchText => search ??= string.Join("  ", new[]
        {
            Kind, Number, Client, Route, Truck, Driver, Aides, Status, StatusGroup, Cargo, Remarks, InvoiceNo,
            Crates > 0 ? Crates + " crates" : "",
            Amount.HasValue ? Amount.Value.ToString("0.##", CultureInfo.InvariantCulture) + " " + Amount.Value.ToString("N2", CultureInfo.InvariantCulture) : "",
            Date.ToString("MMM d yyyy", CultureInfo.InvariantCulture), Date.ToString("MMMM d yyyy", CultureInfo.InvariantCulture),
            Date.ToString("yyyy-MM-dd"), Date.ToString("MM/dd/yyyy"), Date.ToString("dd/MM/yyyy"), Date.ToString("dddd", CultureInfo.InvariantCulture), Time
        }).ToLowerInvariant();
    }
    public static class TransactionIndex
    {
        /// <summary>Loads every transaction. Safe to call off the UI thread (data only, no controls).</summary>
        public static List<TxRecord> LoadAll(bool withAmounts)
        {
            var list = new List<TxRecord>();
            List<Delivery> dels; List<LoadingSchedule> sch; List<SpecialTrip> sts; List<Invoice> inv;
            try { dels = MJdatabase.GetDeliveries(); } catch { dels = new List<Delivery>(); }
            try { sch = MJdatabase.GetLoadingSchedules(); } catch { sch = new List<LoadingSchedule>(); }
            try { sts = MJdatabase.GetSpecialTrips(); } catch { sts = new List<SpecialTrip>(); }
            try { inv = withAmounts ? MJdatabase.GetInvoices() : new List<Invoice>(); } catch { inv = new List<Invoice>(); }
            var invByDelivery = inv.Where(i => i.DeliveryId > 0).GroupBy(i => i.DeliveryId).ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.Id).First());
            var dispatchedSchedules = new HashSet<int>(dels.Where(d => d.LoadingScheduleId > 0).Select(d => d.LoadingScheduleId));
            foreach (var d in dels)
            {
                invByDelivery.TryGetValue(d.Id, out var iv);
                list.Add(new TxRecord
                {
                    Kind = TxRecord.KDispatch, Number = d.DispatchNumber ?? "", Date = d.DispatchDate, Time = d.DepartureTime ?? "",
                    Client = d.ClientName ?? "", Route = d.Destination ?? "", Truck = d.TruckInfo ?? "", Driver = d.DriverName ?? "",
                    Aides = !string.IsNullOrWhiteSpace(d.AideNames) ? d.AideNames : d.AideName ?? "",
                    Crates = d.TotalCrates > 0 ? d.TotalCrates : d.GCrate + d.ECrate, Status = d.Status ?? "", Cargo = d.CargoType ?? "",
                    Remarks = d.Remarks ?? "", Amount = iv?.NetAmount, InvoiceNo = iv?.InvoiceNumber ?? "", Source = d,
                    AideCount = Math.Max(0, MJdatabase.GetDeliveryCrewIds(d).Count() - (d.DriverId > 0 ? 1 : 0))
                });
            }
            foreach (var s in sch.Where(s => !dispatchedSchedules.Contains(s.Id)))
            {
                list.Add(new TxRecord
                {
                    Kind = TxRecord.KLoading, Number = s.ScheduleNumber ?? "", Date = s.ScheduleDate, Time = s.DepartureTime ?? "",
                    Client = s.ClientName ?? "", Route = s.Destination ?? "", Crates = s.TotalCrates > 0 ? s.TotalCrates : s.GCrate + s.ECrate,
                    Status = s.Status ?? "", Cargo = s.CargoType ?? "", Remarks = s.Remarks ?? "", Source = s
                });
            }
            foreach (var t in sts)
            {
                list.Add(new TxRecord
                {
                    Kind = TxRecord.KSpecial, Number = t.OrderReference ?? "", Date = t.DispatchDate, Time = t.DepartureTime ?? "",
                    Client = t.Company ?? "", Route = t.Address ?? "", Truck = t.TruckPlate ?? "", Driver = t.DriverName ?? "", Aides = t.AideName ?? "",
                    Status = t.Status ?? "", Cargo = "Special trip", Remarks = t.Remarks ?? "", Source = t, AideCount = t.AllAideIds.Count
                });
            }
            return list;
        }
        private static readonly string[] DateFormats = { "MMM d", "MMMM d", "MMM d yyyy", "MMMM d yyyy", "MMM d, yyyy", "MMMM d, yyyy", "M/d", "M/d/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "d MMM", "d MMMM", "d MMM yyyy" };
        /// <summary>"oct 5", "October 5 2026", "10/05/2026" or "2026-10-05" → that date (this year if no year is typed).</summary>
        public static DateTime? ParseDate(string text)
        {
            string t = (text ?? "").Trim();
            if (t.Length < 3 || !t.Any(char.IsDigit) || t.Count(char.IsLetter) > 9) return null;
            if (DateTime.TryParseExact(t, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var d)) return d.Date;
            return null;
        }
        /// <summary>Every word must appear somewhere in the record; a typed date matches that day.</summary>
        public static bool Matches(TxRecord r, string query)
        {
            string q = (query ?? "").Trim().ToLowerInvariant();
            if (q.Length == 0) return true;
            var day = ParseDate(q);
            if (day.HasValue) return r.Date.Date == day.Value;
            foreach (var w in q.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                if (!r.SearchText.Contains(w)) return false;
            return true;
        }
    }
}
