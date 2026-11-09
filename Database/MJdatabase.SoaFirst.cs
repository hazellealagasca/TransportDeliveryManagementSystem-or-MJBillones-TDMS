using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>A trip's line(s) on a live statement: the statement it is on and the amounts typed there (the invoice copies them, locked).</summary>
    public sealed class StatementTripLine
    {
        public int SoaId;
        public string SoaNumber = "";
        public string ClientName = "";
        public decimal Freight;
        public decimal Fuel;
    }

    /// <summary>One completed trip report that waits to be put on a statement (Nov 9: statement first).</summary>
    public sealed class StatementCandidate
    {
        public int DeliveryId;
        public string TripReportNumber = "";
        public string DispatchNumber = "";
        public string ClientName = "";
        public DateTime ReportDate;
        public string Route = "";
        public int Crates;
    }

    public static partial class MJdatabase
    {
        /// <summary>
        /// Completed trip reports (finished, not void) whose dispatch has no live statement and no live invoice: the queue in the
        /// Statements of Account tab. Same rules as the "waiting" list, so counts agree. Oldest first, grouped by client by the caller.
        /// </summary>
        public static List<StatementCandidate> GetTripReportsWaitingForStatement()
        {
            var list = new List<StatementCandidate>();
            var reportedIds = GetTripReportDeliveryIds();
            var reports = GetTripReportsByDelivery();
            var live = GetLiveInvoicesByDelivery();
            var onStatement = GetBilledDeliveryIds(0);
            foreach (var d in GetDeliveries())
            {
                if (!string.Equals(d.Status, "Completed", StringComparison.OrdinalIgnoreCase) || !reportedIds.Contains(d.Id)) continue;
                if (onStatement.ContainsKey(d.Id)) continue;                         // already on a live statement (HasSoa)
                // A trip that has an UNPAID service invoice of its own that is on no statement (made before this flow) still belongs in the queue:
                // its statement line takes the invoice's amounts. Paid invoices and old statements' invoices do not.
                if (live.TryGetValue(d.Id, out var li) && (li.OnOldStatement || li.SoaId > 0 || !string.Equals(li.Status, PayStatus.Unpaid, StringComparison.OrdinalIgnoreCase))) continue;
                reports.TryGetValue(d.Id, out var tr);
                list.Add(new StatementCandidate
                {
                    DeliveryId = d.Id,
                    TripReportNumber = tr?.TripReportNumber ?? "",
                    DispatchNumber = d.DispatchNumber ?? "",
                    ClientName = (d.ClientName ?? "").Trim(),
                    ReportDate = tr != null && tr.ReportDate > DateTime.MinValue ? tr.ReportDate : d.DispatchDate,
                    Route = d.CleanRouteDisplay,
                    Crates = d.TotalCrates
                });
            }
            return list.OrderBy(c => c.ClientName, StringComparer.OrdinalIgnoreCase).ThenBy(c => c.ReportDate).ThenBy(c => c.TripReportNumber, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Why these trip reports can't start one statement (null = they can): at least one, and all of ONE client.
        /// The same rule the + New Statement button uses and the save re-checks.
        /// </summary>
        public static string? WhySelectionNotOneClient(IReadOnlyList<StatementCandidate> picked)
        {
            if (picked == null || picked.Count == 0) return "Tick at least one completed trip report.";
            var clients = picked.Select(p => p.ClientName.Trim()).Where(c => c.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (clients.Count == 0) return "The ticked trip reports have no client. Fix the dispatch's client first.";
            if (clients.Count > 1) return "Select one client at a time. You ticked trip reports of: " + string.Join(", ", clients) + ".";
            return null;
        }

        /// <summary>
        /// True when the trip has an UNPAID service invoice of its own that is on no statement (made before the statement-first flow). For the
        /// statement such a trip counts as "not billed yet": its Amount and Fuel surcharge are typed on the statement line, and saving the
        /// statement puts those amounts on that invoice.
        /// </summary>
        public static bool IsPreStatementInvoice(int deliveryId, int specialTripId = 0)
        {
            var li = GetLiveInvoiceForTrip(deliveryId, specialTripId);
            return li != null && !li.OnOldStatement && li.SoaId <= 0 && string.Equals(li.Status, PayStatus.Unpaid, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The live statement that holds this trip and the amounts typed on its line(s), or null when the trip is on no live statement.</summary>
        public static StatementTripLine? GetStatementLineForTrip(int deliveryId, int specialTripId = 0)
        {
            if (deliveryId <= 0 && specialTripId <= 0) return null;
            EnsureSpecialTripColumns();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var c = conn.CreateCommand();
            c.CommandText = @"SELECT s.Id, COALESCE(s.SoaNumber,''), COALESCE(s.ClientName,''), COALESCE(SUM(x.Amount),0), COALESCE(SUM(x.FuelSurcharge),0)
                              FROM StatementOfAccountItems x JOIN StatementsOfAccount s ON s.Id = x.SoaId AND LOWER(COALESCE(s.Status,'')) <> 'cancelled'
                              WHERE " + (deliveryId > 0 ? "COALESCE(x.DeliveryId,0) = @k" : "COALESCE(x.SpecialTripId,0) = @k") + @"
                              GROUP BY s.Id ORDER BY s.Id DESC LIMIT 1";
            c.Parameters.AddWithValue("@k", deliveryId > 0 ? deliveryId : specialTripId);
            using var r = c.ExecuteReader();
            if (!r.Read()) return null;
            return new StatementTripLine
            {
                SoaId = r.GetInt32(0), SoaNumber = r.GetString(1), ClientName = r.GetString(2),
                Freight = Centavos(Convert.ToDecimal(r.GetValue(3))), Fuel = Centavos(Convert.ToDecimal(r.GetValue(4)))
            };
        }

        /// <summary>
        /// A service invoice was just saved for a trip that sits on a statement: put it on the statement and give it its share of the
        /// statement's withholding, so the invoices' net amounts add up to the statement's Total Amount Due to the centavo (SoaTotals.Share).
        /// The amounts must be the statement line's: any other amount is refused (the lock on the invoice form is not the only guard).
        /// </summary>
        public static void ApplyStatementToNewInvoice(int invoiceId, int deliveryId, int specialTripId = 0)
        {
            var line = GetStatementLineForTrip(deliveryId, specialTripId);
            if (line == null || invoiceId <= 0) return;
            EnsureSpecialTripColumns();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                decimal total, fuel;
                using (var c = conn.CreateCommand())
                {
                    c.Transaction = tx;
                    c.CommandText = "SELECT COALESCE(TotalAmount,0), COALESCE(FuelSurcharge,0) FROM Invoices WHERE Id = @id";
                    c.Parameters.AddWithValue("@id", invoiceId);
                    using var r = c.ExecuteReader();
                    if (!r.Read()) throw new InvalidOperationException("The invoice was not found.");
                    total = Centavos(Convert.ToDecimal(r.GetValue(0))); fuel = Centavos(Convert.ToDecimal(r.GetValue(1)));
                }
                if (total != line.Freight + line.Fuel || fuel != line.Fuel)
                    throw new InvalidOperationException($"The invoice amount (PHP {total:N2}) must match the statement {line.SoaNumber} (PHP {line.Freight + line.Fuel:N2}). It is locked to the statement.");

                // Every trip of the statement: its freight, to share the withholding in proportion.
                var trips = new List<(int Del, int Sp, decimal Freight, decimal Fuel)>();
                using (var c = conn.CreateCommand())
                {
                    c.Transaction = tx;
                    c.CommandText = @"SELECT COALESCE(DeliveryId,0), COALESCE(SpecialTripId,0), COALESCE(SUM(Amount),0), COALESCE(SUM(FuelSurcharge),0)
                                      FROM StatementOfAccountItems WHERE SoaId = @s AND (COALESCE(DeliveryId,0) > 0 OR COALESCE(SpecialTripId,0) > 0)
                                      GROUP BY COALESCE(DeliveryId,0), COALESCE(SpecialTripId,0) ORDER BY MIN(Id)";
                    c.Parameters.AddWithValue("@s", line.SoaId);
                    using var r = c.ExecuteReader();
                    while (r.Read()) trips.Add((r.GetInt32(0), r.GetInt32(1), Centavos(Convert.ToDecimal(r.GetValue(2))), Centavos(Convert.ToDecimal(r.GetValue(3)))));
                }
                var t = SoaTotals.From(trips.Sum(x => x.Freight), trips.Sum(x => x.Fuel));
                var shares = SoaTotals.Share(t.Withholding, trips.Select(x => x.Freight).ToArray());
                int at = trips.FindIndex(x => deliveryId > 0 ? x.Del == deliveryId : x.Sp == specialTripId);
                decimal share = at >= 0 ? shares[at] : 0m;
                using (var u = conn.CreateCommand())
                {
                    u.Transaction = tx;
                    u.CommandText = "UPDATE Invoices SET SoaId = @s, Deductions = @w, NetAmount = TotalAmount - @w WHERE Id = @id";
                    u.Parameters.AddWithValue("@s", line.SoaId);
                    u.Parameters.AddWithValue("@w", share);
                    u.Parameters.AddWithValue("@id", invoiceId);
                    u.ExecuteNonQuery();
                }
                // The statement's list of invoice numbers follows.
                using (var u = conn.CreateCommand())
                {
                    u.Transaction = tx;
                    u.CommandText = @"UPDATE StatementsOfAccount SET SalesInvoiceNo = COALESCE((SELECT GROUP_CONCAT(InvoiceNumber, ', ') FROM Invoices
                                          WHERE SoaId = @s AND LOWER(COALESCE(Status,'')) NOT IN ('cancelled','void','voided')), '') WHERE Id = @s";
                    u.Parameters.AddWithValue("@s", line.SoaId);
                    u.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        /// <summary>Statements (id → number of trips) that still have trips without a service invoice. Such a statement can't be collected yet.</summary>
        public static Dictionary<int, int> GetSoaTripsWithoutInvoice()
        {
            var map = new Dictionary<int, int>();
            EnsureSpecialTripColumns();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var c = conn.CreateCommand();
            c.CommandText = @"SELECT x.SoaId, COUNT(*) FROM (
                                  SELECT SoaId, COALESCE(DeliveryId,0) AS D, COALESCE(SpecialTripId,0) AS S FROM StatementOfAccountItems
                                  WHERE COALESCE(DeliveryId,0) > 0 OR COALESCE(SpecialTripId,0) > 0 GROUP BY SoaId, D, S) x
                              JOIN StatementsOfAccount s ON s.Id = x.SoaId AND LOWER(COALESCE(s.Status,'')) <> 'cancelled'
                              WHERE NOT EXISTS (SELECT 1 FROM Invoices v WHERE LOWER(COALESCE(v.Status,'')) NOT IN ('cancelled','void','voided')
                                                AND ((x.D > 0 AND COALESCE(v.DeliveryId,0) = x.D) OR (x.S > 0 AND COALESCE(v.SpecialTripId,0) = x.S)))
                                AND NOT EXISTS (SELECT 1 FROM Invoices o WHERE o.SoaId = x.SoaId AND COALESCE(o.DeliveryId,0) = 0 AND COALESCE(o.SpecialTripId,0) = 0
                                                AND LOWER(COALESCE(o.Status,'')) NOT IN ('cancelled','void','voided'))
                              GROUP BY x.SoaId";
            using var r = c.ExecuteReader();
            while (r.Read()) map[r.GetInt32(0)] = r.GetInt32(1);
            return map;
        }
    }
}
