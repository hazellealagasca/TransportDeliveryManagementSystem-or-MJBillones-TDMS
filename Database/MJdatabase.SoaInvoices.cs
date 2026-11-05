using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Nov 6, 2026: a statement of account groups the service invoices of its trips (one invoice per trip, made in Billing with
    /// the typed SI No.). The statement no longer makes its own invoice; its total is the sum of its invoices.
    /// Statements saved before this (with their own SI-invoice) keep working the old way.
    /// </summary>
    public static partial class MJdatabase
    {
        /// <summary>The trip's service invoice (newest that is not cancelled), from a list made by <see cref="GetTripInvoices"/>.</summary>
        public static TripInvoice? TripInvoiceFor(IEnumerable<TripInvoice> all, int deliveryId, int specialTripId)
        {
            if (deliveryId > 0) return all.Where(i => i.DeliveryId == deliveryId).OrderByDescending(i => i.Id).FirstOrDefault();
            if (specialTripId > 0) return all.Where(i => i.SpecialTripId == specialTripId).OrderByDescending(i => i.Id).FirstOrDefault();
            return null;
        }

        /// <summary>True when the statement still has the invoice it made itself before Nov 6 (an "SI no." invoice with no trip).</summary>
        public static bool SoaHasOwnInvoice(int soaId)
        {
            if (soaId <= 0) return false;
            EnsureSpecialTripColumns();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            return SoaHasOwnInvoice(conn, null, soaId);
        }
        private static bool SoaHasOwnInvoice(SqliteConnection conn, SqliteTransaction? tx, int soaId)
        {
            using var c = conn.CreateCommand();
            c.Transaction = tx;
            c.CommandText = "SELECT COUNT(*) FROM Invoices WHERE COALESCE(SoaId,0) = @sid AND COALESCE(DeliveryId,0) = 0 AND COALESCE(SpecialTripId,0) = 0 AND COALESCE(Status,'') <> 'Cancelled' COLLATE NOCASE";
            c.Parameters.AddWithValue("@sid", soaId);
            return Convert.ToInt32(c.ExecuteScalar() ?? 0) > 0;
        }

        /// <summary>
        /// Puts the service invoice of every trip on the statement onto it (and takes off the ones whose trip was removed).
        /// Throws with the reasons when a trip has no invoice that can go on a statement, or when the total is not the invoices' total.
        /// </summary>
        private static void LinkTripInvoicesToSoa(SqliteConnection conn, SqliteTransaction tx, StatementOfAccount soa, int soaId, List<StatementOfAccountItem> items)
        {
            var problems = new List<string>();
            var found = new List<(int Id, string Number, decimal Net)>();
            int unlinked = (items ?? new()).Count(i => i.DeliveryId <= 0 && i.SpecialTripId <= 0);
            if (unlinked > 0) problems.Add($"{unlinked} line(s) are not linked to a trip, so they have no service invoice. Add trips from the list instead of typing lines.");
            var trips = (items ?? new()).Where(i => i.DeliveryId > 0 || i.SpecialTripId > 0)
                .GroupBy(i => (i.DeliveryId > 0 ? i.DeliveryId : 0, i.DeliveryId > 0 ? 0 : i.SpecialTripId)).ToList();
            foreach (var g in trips)
            {
                var (did, sid) = g.Key;
                var first = g.First();
                string label = !string.IsNullOrWhiteSpace(first.TripReportNumber) ? first.TripReportNumber : $"trip {first.TripNumber}";
                using var c = conn.CreateCommand();
                c.Transaction = tx;
                c.CommandText = @"SELECT i.Id, i.InvoiceNumber, i.NetAmount, COALESCE(i.Status,''), COALESCE(i.ClientName,''),
                                         CASE WHEN s.Id IS NULL OR COALESCE(s.Status,'') = 'Cancelled' COLLATE NOCASE THEN 0 ELSE s.Id END, COALESCE(s.SoaNumber,''),
                                         (SELECT COALESCE(SUM(r.AmountPaid),0) FROM CollectionReceipts r WHERE r.InvoiceId = i.Id AND COALESCE(r.IsVoid,0) = 0)
                                  FROM Invoices i LEFT JOIN StatementsOfAccount s ON s.Id = COALESCE(i.SoaId,0)
                                  WHERE " + (did > 0 ? "COALESCE(i.DeliveryId,0) = @k" : "COALESCE(i.SpecialTripId,0) = @k") + @"
                                    AND COALESCE(i.Status,'') <> 'Cancelled' COLLATE NOCASE
                                  ORDER BY i.Id DESC LIMIT 1";
                c.Parameters.AddWithValue("@k", did > 0 ? did : sid);
                using var r = c.ExecuteReader();
                if (!r.Read()) { problems.Add($"{label}: no service invoice yet. Make it in Billing (type its SI No.), then add the trip here."); continue; }
                int id = r.GetInt32(0);
                string no = r.IsDBNull(1) ? "" : r.GetString(1);
                decimal net = Centavos(Convert.ToDecimal(r.GetValue(2)));
                string st = r.GetString(3), client = r.GetString(4);
                int onSoa = Convert.ToInt32(r.GetValue(5));
                string onSoaNo = r.GetString(6);
                decimal paid = Centavos(Convert.ToDecimal(r.GetValue(7)));
                if (onSoa > 0 && onSoa != soaId) problems.Add($"{label}: SI {no} is already on {onSoaNo}.");
                else if (paid > 0 || st.Equals("Paid", StringComparison.OrdinalIgnoreCase)) problems.Add($"{label}: SI {no} is already paid, so it is collected on its own receipt.");
                else if (st.Equals("Draft", StringComparison.OrdinalIgnoreCase) || net <= 0) problems.Add($"{label}: SI {no} has no amount yet. Finish it in Billing.");
                else if (!string.Equals(client.Trim(), (soa.ClientName ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) problems.Add($"{label}: SI {no} is for {client}, not {soa.ClientName}.");
                else found.Add((id, no, net));
            }
            if (found.Count == 0 && problems.Count == 0) problems.Add("Add at least one trip that has a service invoice.");
            if (problems.Count > 0) throw new InvalidOperationException("This statement can't be saved yet:\n\n• " + string.Join("\n• ", problems));

            decimal total = found.Sum(f => f.Net);
            if (Math.Abs(total - soa.TotalAmount) > 0.05m)
                throw new InvalidOperationException($"The statement's total (PHP {soa.TotalAmount:N2}) must be the total of its service invoices (PHP {total:N2}). Nothing was saved.");

            // Invoices of trips taken off the statement go back to being lone invoices (only while nothing is paid on them).
            var keep = new HashSet<int>(found.Select(f => f.Id));
            var drop = new List<(int Id, string No, decimal Paid)>();
            using (var c = conn.CreateCommand())
            {
                c.Transaction = tx;
                c.CommandText = @"SELECT Id, InvoiceNumber, (SELECT COALESCE(SUM(AmountPaid),0) FROM CollectionReceipts r WHERE r.InvoiceId = Invoices.Id AND COALESCE(r.IsVoid,0) = 0)
                                  FROM Invoices WHERE COALESCE(SoaId,0) = @sid AND COALESCE(Status,'') <> 'Cancelled' COLLATE NOCASE";
                c.Parameters.AddWithValue("@sid", soaId);
                using var r = c.ExecuteReader();
                while (r.Read()) if (!keep.Contains(r.GetInt32(0))) drop.Add((r.GetInt32(0), r.IsDBNull(1) ? "" : r.GetString(1), Centavos(Convert.ToDecimal(r.GetValue(2)))));
            }
            var paidDrop = drop.Where(d => d.Paid > 0).ToList();
            if (paidDrop.Count > 0) throw new InvalidOperationException($"SI {string.Join(", ", paidDrop.Select(d => d.No))} on this statement is already paid, so its trip can't be taken off.");
            foreach (var d in drop)
            {
                using var u = conn.CreateCommand();
                u.Transaction = tx;
                u.CommandText = "UPDATE Invoices SET SoaId = 0 WHERE Id = @id";
                u.Parameters.AddWithValue("@id", d.Id);
                u.ExecuteNonQuery();
            }
            foreach (var f in found)
            {
                using var u = conn.CreateCommand();
                u.Transaction = tx;
                u.CommandText = "UPDATE Invoices SET SoaId = @sid WHERE Id = @id";
                u.Parameters.AddWithValue("@sid", soaId);
                u.Parameters.AddWithValue("@id", f.Id);
                u.ExecuteNonQuery();
            }
            soa.SalesInvoiceNo = string.Join(", ", found.Select(f => f.Number));
            using (var u = conn.CreateCommand())
            {
                u.Transaction = tx;
                u.CommandText = "UPDATE StatementsOfAccount SET SalesInvoiceNo = @si WHERE Id = @sid";
                u.Parameters.AddWithValue("@si", soa.SalesInvoiceNo);
                u.Parameters.AddWithValue("@sid", soaId);
                u.ExecuteNonQuery();
            }
        }
    }
}
