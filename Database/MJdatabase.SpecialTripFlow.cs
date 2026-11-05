using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>What a special trip is linked to (for the list badges, the summary and the checklist actions).</summary>
    public sealed class SpecialTripLinks
    {
        public string TripReportNumber = "";
        public int InvoiceId;
        public string InvoiceNumber = "";
        public string InvoiceStatus = "";
        public decimal InvoiceNet, Collected;
        public string SoaNumber = "";
        public string SoaInvoiceStatus = "";
        public bool HasReport => TripReportNumber.Length > 0;
        public bool OnSoa => SoaNumber.Length > 0;
        /// <summary>Money received: its service invoice is paid, or the SOA it is on has a paid invoice.</summary>
        public bool IsCollected => (InvoiceId > 0 && InvoiceNet > 0 && Collected >= InvoiceNet) || SoaInvoiceStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Special trips: Destination Address, Trip charge, Mode of Payment, and the checklist actions (With Trip Report,
    /// With SOA, With Service Invoice, With Collection) that run when a trip is saved.
    /// </summary>
    public static partial class MJdatabase
    {
        /// <summary>The only Mode of Payment choices for a special trip.</summary>
        public static readonly string[] SpecialTripPaymentModes = { "Cash", "Bank Transfer", "Cheque", "Online Payment" };

        /// <summary>Adds the special-trip columns (and their links on trip reports, invoices and SOA lines) when missing. Checked every time: cheap, and safe after a restore.</summary>
        public static void EnsureSpecialTripColumns()
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            void Add(string table, string column, string type)
            {
                bool exists = false, tableExists = false;
                using (var c = conn.CreateCommand())
                {
                    c.CommandText = $"PRAGMA table_info({table})";
                    using var r = c.ExecuteReader();
                    while (r.Read()) { tableExists = true; if (string.Equals(r.GetString(1), column, StringComparison.OrdinalIgnoreCase)) exists = true; }
                }
                if (!tableExists || exists) return;
                using var a = conn.CreateCommand();
                a.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {type}";
                try { a.ExecuteNonQuery(); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
            }
            Add("SpecialTrips", "DestinationAddress", "TEXT DEFAULT ''");
            Add("SpecialTrips", "TripCharge", "REAL DEFAULT 0");
            Add("SpecialTrips", "ModeOfPayment", "TEXT DEFAULT ''");
            Add("TripReports", "SpecialTripId", "INTEGER DEFAULT 0");
            Add("Invoices", "SpecialTripId", "INTEGER DEFAULT 0");
            Add("StatementOfAccountItems", "SpecialTripId", "INTEGER DEFAULT 0");
        }

        /// <summary>Makes sure "Online Payment" is one of the payment methods in Collections &amp; Receipts.</summary>
        public static void EnsureOnlinePaymentMethod()
        {
            try
            {
                var methods = GetLookupItems("Payment Method");
                if (methods.Count > 0 && !methods.Any(m => string.Equals(m?.Trim(), "Online Payment", StringComparison.OrdinalIgnoreCase)))
                    SaveLookupItem("Payment Method", "Online Payment");
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        /// <summary>Reads Destination Address, Trip charge and Mode of Payment for these trips.</summary>
        public static void FillSpecialTripExtras(IEnumerable<SpecialTrip> trips)
        {
            var list = trips.Where(t => t != null && t.Id > 0).ToList();
            if (list.Count == 0) return;
            EnsureSpecialTripColumns();
            var byId = list.GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.ToList());
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, COALESCE(DestinationAddress,''), COALESCE(TripCharge,0), COALESCE(ModeOfPayment,'') FROM SpecialTrips" + (list.Count == 1 ? " WHERE Id = @id" : "");
            if (list.Count == 1) cmd.Parameters.AddWithValue("@id", list[0].Id);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                if (!byId.TryGetValue(r.GetInt32(0), out var ts)) continue;
                foreach (var t in ts)
                {
                    t.DestinationAddress = r.GetString(1);
                    t.TripCharge = Convert.ToDecimal(r.GetDouble(2));
                    t.ModeOfPayment = r.GetString(3);
                }
            }
        }

        /// <summary>Saves Destination Address, Trip charge and Mode of Payment inside the trip's own save.</summary>
        private static void SaveSpecialTripExtras(SqliteConnection conn, SqliteTransaction trans, SpecialTrip trip)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = "UPDATE SpecialTrips SET DestinationAddress = @dest, TripCharge = @charge, ModeOfPayment = @mop WHERE Id = @id";
            cmd.Parameters.AddWithValue("@dest", (trip.DestinationAddress ?? "").Trim());
            cmd.Parameters.AddWithValue("@charge", (double)Math.Max(0m, trip.TripCharge));
            cmd.Parameters.AddWithValue("@mop", (trip.ModeOfPayment ?? "").Trim());
            cmd.Parameters.AddWithValue("@id", trip.Id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>For every special trip: its trip report, service invoice (with what was collected) and the SOA it is on.</summary>
        public static Dictionary<int, SpecialTripLinks> GetSpecialTripLinks()
        {
            EnsureSpecialTripColumns();
            var map = new Dictionary<int, SpecialTripLinks>();
            SpecialTripLinks L(int id) { if (!map.TryGetValue(id, out var l)) { l = new SpecialTripLinks(); map[id] = l; } return l; }
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using (var c = conn.CreateCommand())
            {
                c.CommandText = "SELECT SpecialTripId, COALESCE(TripReportNumber,'') FROM TripReports WHERE COALESCE(SpecialTripId,0) > 0 ORDER BY Id";
                using var r = c.ExecuteReader();
                while (r.Read()) L(r.GetInt32(0)).TripReportNumber = r.GetString(1);
            }
            using (var c = conn.CreateCommand())
            {
                c.CommandText = @"SELECT i.SpecialTripId, i.Id, i.InvoiceNumber, COALESCE(i.Status,''), COALESCE(i.NetAmount,0),
                                         (SELECT COALESCE(SUM(AmountPaid),0) FROM CollectionReceipts cr WHERE cr.InvoiceId = i.Id AND COALESCE(cr.IsVoid,0) = 0)
                                  FROM Invoices i WHERE COALESCE(i.SpecialTripId,0) > 0 AND COALESCE(i.Status,'') <> 'Cancelled' ORDER BY i.Id";
                using var r = c.ExecuteReader();
                while (r.Read())
                {
                    var l = L(r.GetInt32(0));
                    l.InvoiceId = r.GetInt32(1); l.InvoiceNumber = r.GetString(2); l.InvoiceStatus = r.GetString(3);
                    l.InvoiceNet = Convert.ToDecimal(r.GetDouble(4)); l.Collected = Convert.ToDecimal(r.GetDouble(5));
                }
            }
            using (var c = conn.CreateCommand())
            {
                c.CommandText = @"SELECT it.SpecialTripId, s.SoaNumber,
                                         COALESCE((SELECT i.Status FROM Invoices i WHERE i.SoaId = s.Id AND COALESCE(i.Status,'') <> 'Cancelled' ORDER BY i.Id DESC LIMIT 1), '')
                                  FROM StatementOfAccountItems it JOIN StatementsOfAccount s ON s.Id = it.SoaId
                                  WHERE COALESCE(it.SpecialTripId,0) > 0 AND COALESCE(s.Status,'') NOT IN ('Cancelled','Void','Voided')";
                using var r = c.ExecuteReader();
                while (r.Read()) { var l = L(r.GetInt32(0)); l.SoaNumber = r.GetString(1); l.SoaInvoiceStatus = r.GetString(2); }
            }
            return map;
        }

        /// <summary>
        /// Special-trip service invoices show the trip's order reference as their dispatch # and "Special trip · destination"
        /// as their loading schedule, so Billing, receipts and prints carry the Destination Address.
        /// </summary>
        public static void FillSpecialTripInvoiceInfo(List<Invoice> invoices)
        {
            try
            {
                if (invoices == null || invoices.Count == 0) return;
                EnsureSpecialTripColumns();
                var info = new Dictionary<int, (string Ref, string Dest)>();
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using var c = conn.CreateCommand();
                    c.CommandText = @"SELECT i.Id, COALESCE(t.OrderReference,''), COALESCE(t.DestinationAddress,'') FROM Invoices i JOIN SpecialTrips t ON t.Id = i.SpecialTripId WHERE COALESCE(i.SpecialTripId,0) > 0";
                    using var r = c.ExecuteReader();
                    while (r.Read()) info[r.GetInt32(0)] = (r.GetString(1), r.GetString(2));
                }
                foreach (var inv in invoices)
                {
                    if (!info.TryGetValue(inv.Id, out var x)) continue;
                    if (string.IsNullOrWhiteSpace(inv.DispatchNumber)) inv.DispatchNumber = x.Ref;
                    inv.LoadingScheduleNumber = x.Dest.Length > 0 ? "Special trip · " + x.Dest : "Special trip";
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        /// <summary>The special trip an invoice is for: its own service invoice, or the SOA invoice that bills it.</summary>
        public static SpecialTrip? GetSpecialTripForInvoice(int invoiceId)
        {
            if (invoiceId <= 0) return null;
            EnsureSpecialTripColumns();
            int tripId;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var c = conn.CreateCommand();
                c.CommandText = @"SELECT COALESCE(SpecialTripId,0) FROM Invoices WHERE Id = @id";
                c.Parameters.AddWithValue("@id", invoiceId);
                tripId = Convert.ToInt32(c.ExecuteScalar() ?? 0);
                if (tripId == 0)
                {
                    c.CommandText = @"SELECT COALESCE(it.SpecialTripId,0) FROM StatementOfAccountItems it JOIN Invoices i ON i.SoaId = it.SoaId AND i.SoaId > 0
                                      WHERE i.Id = @id AND COALESCE(it.SpecialTripId,0) > 0 ORDER BY it.Id LIMIT 1";
                    tripId = Convert.ToInt32(c.ExecuteScalar() ?? 0);
                }
            }
            return tripId > 0 ? GetSpecialTripById(tripId) : null;
        }

        /// <summary>
        /// The trip report rows for a special trip: rows saved with an earlier report (kept under −trip Id), or one row
        /// per stop of the trip (its code and details), or a single row for the Destination Address.
        /// </summary>
        public static List<DispatchStop> GetOrBuildSpecialTripReportStops(int specialTripId, string clientName)
        {
            var saved = GetDispatchStops(-specialTripId);
            if (saved.Count > 0) return saved;
            var trip = GetSpecialTripById(specialTripId);
            var list = new List<DispatchStop>();
            if (trip == null) return list;
            int seq = 1;
            foreach (var st in trip.Stops.OrderBy(x => x.SequenceNo))
            {
                string name = (st.DeliveryDetails ?? "").Trim();
                if (name.Length == 0 && string.IsNullOrWhiteSpace(st.StandardId)) continue;
                list.Add(new DispatchStop { DeliveryId = -specialTripId, StopSequence = seq++, ClientName = trip.Company, StoreCode = (st.StandardId ?? "").Trim(), Destination = name.Length > 0 ? name : trip.DestinationAddress, DepartureTime = st.DepartureTime ?? "" });
            }
            if (list.Count == 0)
                list.Add(new DispatchStop { DeliveryId = -specialTripId, StopSequence = 1, ClientName = trip.Company, Destination = string.IsNullOrWhiteSpace(trip.DestinationAddress) ? trip.Address : trip.DestinationAddress });
            return list;
        }

        /// <summary>
        /// Special trips ticked "With Trip Report" shown in the Trip Report form as dispatch-like entries (Id = −trip Id,
        /// dispatch # = order reference, route = Destination Address), with the same truck, crew and date.
        /// </summary>
        public static List<Delivery> GetSpecialTripsForTripReports(ISet<string> reportedNumbers)
        {
            var list = new List<Delivery>();
            foreach (var t in GetSpecialTrips().Where(t => !string.Equals(t.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                                                         && (t.WithTripReport || reportedNumbers.Contains(t.OrderReference ?? ""))))
            {
                string dest = string.IsNullOrWhiteSpace(t.DestinationAddress) ? t.Address : t.DestinationAddress;
                list.Add(new Delivery
                {
                    Id = -t.Id,
                    DispatchNumber = t.OrderReference,
                    LoadingScheduleInfo = "Special trip · " + dest,
                    ClientName = t.Company,
                    Destination = dest,
                    TruckId = t.TruckId,
                    TruckInfo = t.TruckPlate,
                    DriverId = t.DriverId,
                    DriverName = t.DriverName,
                    AideId = t.AideId,
                    AideName = t.AideName,
                    AideIds = t.AideIds,
                    DispatchDate = t.DispatchDate,
                    DepartureTime = t.DepartureTime,
                    Status = string.Equals(t.Status, "In-Transit", StringComparison.OrdinalIgnoreCase) ? "In Transit" : t.Status,
                    CargoType = "Special trip",
                    TotalStopsCount = Math.Max(1, t.TotalStopsCount)
                });
            }
            return list;
        }

        /// <summary>The trip's own service invoice (not cancelled), or null.</summary>
        private static Invoice? FindSpecialTripInvoice(SpecialTrip trip, bool includeCancelled)
        {
            int id = 0;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var c = conn.CreateCommand();
                c.CommandText = "SELECT Id FROM Invoices WHERE COALESCE(SpecialTripId,0) = @st" + (includeCancelled ? "" : " AND COALESCE(Status,'') <> 'Cancelled'") + " ORDER BY (Status = 'Cancelled'), Id DESC LIMIT 1";
                c.Parameters.AddWithValue("@st", trip.Id);
                id = Convert.ToInt32(c.ExecuteScalar() ?? 0);
                if (id == 0)
                {
                    // Invoices made before the link existed: "INV-<order reference>".
                    c.CommandText = "SELECT Id FROM Invoices WHERE UPPER(InvoiceNumber) = UPPER(@n) AND COALESCE(SpecialTripId,0) = 0" + (includeCancelled ? "" : " AND COALESCE(Status,'') <> 'Cancelled'") + " LIMIT 1";
                    c.Parameters.AddWithValue("@n", "INV-" + trip.OrderReference);
                    id = Convert.ToInt32(c.ExecuteScalar() ?? 0);
                }
            }
            return id > 0 ? GetInvoices().FirstOrDefault(i => i.Id == id) : null;
        }

        private static void LinkInvoiceToSpecialTrip(int invoiceId, int tripId)
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var c = conn.CreateCommand();
            c.CommandText = "UPDATE Invoices SET SpecialTripId = @st WHERE Id = @id";
            c.Parameters.AddWithValue("@st", tripId);
            c.Parameters.AddWithValue("@id", invoiceId);
            c.ExecuteNonQuery();
        }

        private static void SetInvoiceStatusDirect(int invoiceId, string status, string number, string reason)
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var c = conn.CreateCommand();
                c.CommandText = "UPDATE Invoices SET Status = @s WHERE Id = @id";
                c.Parameters.AddWithValue("@s", status);
                c.Parameters.AddWithValue("@id", invoiceId);
                c.ExecuteNonQuery();
            }
            try { LogAuditTrail("UPDATE", "Invoices", invoiceId, number, "Status", "", status, reason); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        /// <summary>
        /// Runs the special trip's checklist after it is saved, using the trip's own information:
        /// <list type="bullet">
        /// <item>With Trip Report: the trip is listed in Trip Reports as needing a report (nothing to create here).</item>
        /// <item>With SOA: the trip is offered in the client's Statement of Account (nothing to create here).</item>
        /// <item>With Service Invoice: its service invoice is created, or kept in step with the trip charge while nothing is paid on it; unticked, an unpaid one is cancelled.</item>
        /// <item>With Collection: a reminder that a payment is due (when newly ticked).</item>
        /// </list>
        /// Returns what was done, in plain words, for the save message.
        /// </summary>
        public static List<string> SyncSpecialTripChecklist(SpecialTrip trip, SpecialTrip? before, string? siNumber = null)
        {
            var notes = new List<string>();
            if (trip == null || trip.Id <= 0) return notes;
            try
            {
                EnsureSpecialTripColumns();
                bool cancelled = string.Equals(trip.Status?.Trim(), "Cancelled", StringComparison.OrdinalIgnoreCase);
                var inv = FindSpecialTripInvoice(trip, includeCancelled: true);
                decimal collected = inv != null ? GetTotalCollectedForInvoice(inv.Id) : 0m;
                bool invActive = inv != null && !string.Equals(inv.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);

                if (trip.WithServiceInvoice && !cancelled)
                {
                    if (inv == null && string.IsNullOrWhiteSpace(siNumber))
                    {
                        notes.Add("Service invoice not made yet: no Service Invoice No. was typed. Make it in Billing and type its SI No."); // Nov 6
                    }
                    else if (inv == null)
                    {
                        string number = siNumber!.Trim();
                        var siWhy = CheckSiNumber(number);
                        if (siWhy != null) throw new InvalidOperationException(siWhy);
                        var created = new Invoice
                        {
                            InvoiceNumber = number,
                            DispatchNumber = trip.OrderReference,
                            LoadingScheduleNumber = "SPECIAL-TRIP",
                            ClientName = trip.Company,
                            IssueDate = DateTime.Now,
                            DueDate = DateTime.Now.AddDays(15),
                            TotalAmount = trip.TripCharge,
                            Deductions = 0,
                            NetAmount = trip.TripCharge,
                            Status = trip.TripCharge > 0 ? "Unpaid" : "Draft"
                        };
                        SaveInvoice(created);
                        if (created.Id > 0) LinkInvoiceToSpecialTrip(created.Id, trip.Id);
                        notes.Add($"Service invoice {created.InvoiceNumber} created for ₱{trip.TripCharge:N2} (Billing Invoices).");
                    }
                    else if (collected <= 0 && !string.Equals(inv.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                    {
                        bool changed = inv.TotalAmount != trip.TripCharge || inv.Deductions != 0 || !string.Equals(inv.ClientName, trip.Company, StringComparison.Ordinal) || !invActive;
                        if (changed)
                        {
                            inv.ClientName = trip.Company;
                            inv.TotalAmount = trip.TripCharge;
                            inv.Deductions = 0;
                            inv.NetAmount = trip.TripCharge;
                            inv.Status = trip.TripCharge > 0 ? "Unpaid" : "Draft";
                            SaveInvoice(inv);
                            notes.Add($"Service invoice {inv.InvoiceNumber} updated to ₱{trip.TripCharge:N2}.");
                        }
                        if (inv.Id > 0) LinkInvoiceToSpecialTrip(inv.Id, trip.Id);
                    }
                    else if (inv.NetAmount != trip.TripCharge)
                    {
                        notes.Add($"Service invoice {inv.InvoiceNumber} already has payments, so its amount (₱{inv.NetAmount:N2}) was not changed. Adjust it in Billing if needed.");
                    }
                }
                else if (invActive && inv != null)
                {
                    if (collected <= 0 && !string.Equals(inv.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                    {
                        SetInvoiceStatusDirect(inv.Id, "Cancelled", inv.InvoiceNumber, cancelled ? $"Special trip {trip.OrderReference} was cancelled" : $"\"With Service Invoice\" was unticked on special trip {trip.OrderReference}");
                        notes.Add($"Service invoice {inv.InvoiceNumber} was cancelled (nothing had been paid on it).");
                    }
                    else notes.Add($"Service invoice {inv.InvoiceNumber} has payments, so it was kept.");
                }

                var links = GetSpecialTripLinks().TryGetValue(trip.Id, out var l) ? l : new SpecialTripLinks();
                if (!cancelled)
                {
                    if (trip.ModeOfPaymentWithReport)
                        notes.Add(links.HasReport ? $"Trip report {links.TripReportNumber} is done." : "Listed in Trip Reports as needing a trip report.");
                    else if (links.HasReport)
                        notes.Add($"Trip report {links.TripReportNumber} already exists (kept).");
                    if (trip.WithSOA)
                        notes.Add(links.OnSoa ? $"On statement {links.SoaNumber}." : $"Ready to add to {trip.Company}'s Statement of Account.");
                    else if (links.OnSoa)
                        notes.Add($"It is already on statement {links.SoaNumber}; remove its line there if it shouldn't be billed.");
                    if (trip.WithCollection)
                    {
                        notes.Add(links.IsCollected ? "Collection: paid." : $"Collection pending ({trip.ModeOfPayment}).");
                        if (before == null || !before.WithCollection)
                            EventNotificationService.Publish("Collection Pending", $"Special trip {trip.OrderReference} for {trip.Company} ({trip.DestinationAddress}) needs its {trip.ModeOfPayment} payment recorded in Collections & Receipts.", ToastType.Info, null, "Finance", "Collections", trip.OrderReference);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrLog.Error("SyncSpecialTripChecklist", ex);
                notes.Add("Some checklist actions could not be completed: " + ex.Message);
            }
            return notes;
        }
    }
}
