using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>A saved statement of account that can be billed with ONE service invoice, with the figures the statement works out.</summary>
    public sealed class SoaToInvoice
    {
        public int SoaId;
        public string SoaNumber = "";
        public string ClientName = "";
        public int Trips;
        public string TripReports = "";
        public SoaTotalsResult Totals;
        public string Label => $"{SoaNumber} · {ClientName} · ₱{Totals.Due:N2} · {Trips} trip{(Trips == 1 ? "" : "s")}";
    }

    public static partial class MJdatabase
    {
        /// <summary>The figures of a statement, worked out from its lines by the one formula (SoaTotals): freight, fuel, VAT, withholding and total due.</summary>
        public static SoaTotalsResult GetSoaTotalsFor(int soaId)
        {
            var items = GetStatementOfAccountItems(soaId);
            return SoaTotals.From(items.Sum(i => i.Amount), items.Sum(i => i.FuelSurcharge));
        }

        /// <summary>
        /// Why this statement can't get its own whole-statement invoice (null = it can): it must be saved, live, unpaid, have trips and a
        /// positive amount, and neither it nor any of its trips may already have a live invoice (a trip is billed once).
        /// </summary>
        public static string? WhySoaCannotBeInvoiced(int soaId, Dictionary<int, LiveTripInvoice>? liveByDelivery = null, StatementOfAccount? known = null)
        {
            var soa = known ?? GetStatementOfAccountById(soaId);
            if (soa == null) return "That statement was not found.";
            string st = (soa.Status ?? "").Trim();
            if (st.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) || st.Equals("Void", StringComparison.OrdinalIgnoreCase)) return $"{soa.SoaNumber} is {st.ToLowerInvariant()}.";
            if (st.Equals(PayStatus.Paid, StringComparison.OrdinalIgnoreCase)) return $"{soa.SoaNumber} is already paid in full.";
            var items = GetStatementOfAccountItems(soaId);
            var tripItems = items.Where(i => i.DeliveryId > 0 || i.SpecialTripId > 0).ToList();
            if (tripItems.Count == 0) return $"{soa.SoaNumber} has no trips on it.";
            var t = SoaTotals.From(items.Sum(i => i.Amount), items.Sum(i => i.FuelSurcharge));
            if (t.TotalSales <= 0m) return $"{soa.SoaNumber} has no amount yet. Type the amount of its trips on the statement first.";
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var c = conn.CreateCommand();
            c.CommandText = "SELECT InvoiceNumber FROM Invoices WHERE COALESCE(SoaId,0) = @s AND LOWER(COALESCE(Status,'')) NOT IN ('cancelled','void','voided') LIMIT 1";
            c.Parameters.AddWithValue("@s", soaId);
            if (c.ExecuteScalar() is string no && no.Length > 0) return $"{soa.SoaNumber} already has invoice {no}. A statement is billed once.";
            var live = liveByDelivery ?? GetLiveInvoicesByDelivery();
            foreach (var i in tripItems.Where(x => x.DeliveryId > 0).Select(x => x.DeliveryId).Distinct())
                if (live.TryGetValue(i, out var li)) return $"A trip on {soa.SoaNumber} is already invoiced one by one ({li.Describe()}). A trip is billed once.";
            foreach (var sp in tripItems.Where(x => x.SpecialTripId > 0).Select(x => x.SpecialTripId).Distinct())
                if (SpecialTripHasActiveInvoice(sp)) return $"A special trip on {soa.SoaNumber} is already invoiced. A trip is billed once.";
            return null;
        }

        /// <summary>Re-checks the trip reports of every trip on a statement: Invoiced while the statement is billed (by its own invoice or its trips' invoices), back to Completed otherwise.</summary>
        private static void ResyncTripReportsOfSoa(SqliteConnection conn, SqliteTransaction tx, int soaId)
        {
            var keys = new List<(int D, int S)>();
            using (var c = conn.CreateCommand())
            {
                c.Transaction = tx;
                c.CommandText = "SELECT DISTINCT COALESCE(DeliveryId,0), COALESCE(SpecialTripId,0) FROM StatementOfAccountItems WHERE SoaId = @s AND (COALESCE(DeliveryId,0) > 0 OR COALESCE(SpecialTripId,0) > 0)";
                c.Parameters.AddWithValue("@s", soaId);
                using var r = c.ExecuteReader();
                while (r.Read()) keys.Add((r.GetInt32(0), r.GetInt32(1)));
            }
            foreach (var (d, s) in keys) SyncTripReportStatus(conn, tx, d, s);
        }

        /// <summary>Statements that can be billed with one invoice now, newest first (the list behind New invoice → Statement of Account).</summary>
        public static List<SoaToInvoice> GetSoasWaitingForOwnInvoice()
        {
            var list = new List<SoaToInvoice>();
            var liveNow = GetLiveInvoicesByDelivery();
            foreach (var s in GetStatementsOfAccount().OrderByDescending(x => x.Id))
            {
                if (WhySoaCannotBeInvoiced(s.Id, liveNow, s) != null) continue;
                var items = GetStatementOfAccountItems(s.Id);
                int trips = items.Where(i => i.DeliveryId > 0 || i.SpecialTripId > 0).Select(i => (i.DeliveryId, i.SpecialTripId)).Distinct().Count();
                var nos = items.Select(i => i.TripReportNumber).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Take(6).ToList();
                list.Add(new SoaToInvoice
                {
                    SoaId = s.Id, SoaNumber = s.SoaNumber, ClientName = (s.ClientName ?? "").Trim(), Trips = trips,
                    TripReports = string.Join(", ", nos), Totals = SoaTotals.From(items.Sum(i => i.Amount), items.Sum(i => i.FuelSurcharge))
                });
            }
            return list;
        }

        /// <summary>The statement's SAVED total amount due (read, never recalculated); null when the statement is not found.</summary>
        public static decimal? GetSavedStatementTotalDue(int soaId) => soaId <= 0 ? null : GetStatementOfAccountById(soaId)?.TotalAmount;

        /// <summary>
        /// Issues ONE service invoice for the whole statement. The amounts are the statement's, worked out here from its lines (never typed):
        /// total = freight + fuel, deductions = the statement's withholding, net = its Total Amount Due. The typed SI No. is the only input.
        /// </summary>
        public static Invoice SaveInvoiceForSoa(int soaId, string invoiceNumber, bool crossTypeConfirmed = false)
        {
            var why = WhySoaCannotBeInvoiced(soaId);
            if (why != null) throw new InvalidOperationException(why + " Nothing was saved.");
            var soa = GetStatementOfAccountById(soaId)!;
            var items = GetStatementOfAccountItems(soaId);
            var t = SoaTotals.From(items.Sum(i => i.Amount), items.Sum(i => i.FuelSurcharge));
            var inv = new Invoice
            {
                InvoiceNumber = (invoiceNumber ?? "").Trim(), SoaId = soaId, DeliveryId = 0, ClientName = (soa.ClientName ?? "").Trim(),
                IssueDate = DateTime.Now, DueDate = DateTime.Now.AddDays(15),
                TotalAmount = InvoiceTotal.Of(t.TotalSales, t.Fuel), FuelSurcharge = t.Fuel, Deductions = 0, NetAmount = InvoiceTotal.Of(t.TotalSales, t.Fuel), Status = PayStatus.Unpaid
            };
            SaveInvoice(inv, crossTypeConfirmed: crossTypeConfirmed);
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();
            using (var u = conn.CreateCommand())
            {
                u.Transaction = tx;
                // The statement's single withholding figure goes on the invoice: its net is exactly the statement's Total Amount Due.
                u.CommandText = "UPDATE Invoices SET Deductions = @w, NetAmount = TotalAmount - @w WHERE Id = @id";
                u.Parameters.AddWithValue("@w", t.Withholding);
                u.Parameters.AddWithValue("@id", inv.Id);
                u.ExecuteNonQuery();
            }
            using (var u = conn.CreateCommand())
            {
                u.Transaction = tx;
                u.CommandText = "UPDATE StatementsOfAccount SET SalesInvoiceNo = @n WHERE Id = @s";
                u.Parameters.AddWithValue("@n", inv.InvoiceNumber);
                u.Parameters.AddWithValue("@s", soaId);
                u.ExecuteNonQuery();
            }
            ResyncTripReportsOfSoa(conn, tx, soaId); // the statement's trip reports turn Invoiced with its invoice
            tx.Commit();
            inv.Deductions = t.Withholding; inv.NetAmount = inv.TotalAmount - t.Withholding;
            return inv;
        }
    }
}
