using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>One service invoice that was cancelled as a typing mistake (the "Cancelled as typing mistakes" report, plan 17.6).</summary>
    public sealed class TypingMistakeRow
    {
        public int InvoiceId;
        public string Number = "";
        public string Client = "";
        public decimal Net;
        public DateTime? CancelledAt;
        public string CancelledBy = "";
        public string Reason = "";
        public DateTime? Until;
        public DateTime? ReinstatedAt;
        /// <summary>Waiting (number kept for the corrected record), Reinstated, or Expired (number burned).</summary>
        public string State = "";
    }

    public static partial class MJdatabase
    {
        /// <summary>The kind and reason of the cancel in progress, read by the soft-delete of an invoice (single UI thread).</summary>
        [ThreadStatic] private static (string Kind, string Reason)? cancelContext;

        /// <summary>
        /// Cancels a service invoice and says WHY (plan 17.6). A <b>typing mistake</b> keeps the number for 7 days so the same record can be
        /// reinstated and corrected; a <b>real void</b> (the paper invoice is cancelled) burns the number for good. All the usual guards still apply
        /// (payments, statements). Returns false when the invoice could not be cancelled.
        /// </summary>
        public static bool CancelInvoice(int invoiceId, bool typingMistake, string reason)
        {
            reason = (reason ?? "").Trim();
            if (reason.Length < 3) throw new InvalidOperationException("Write the reason for cancelling this invoice.");
            EnsureNumberSchema();
            cancelContext = (typingMistake ? "TypingMistake" : "Void", reason);
            try { return DeleteRecord("Invoices", invoiceId); }
            finally { cancelContext = null; }
        }

        /// <summary>
        /// "Re-enter" for an invoice cancelled as a typing mistake: the same record is reopened so it can be corrected, keeping its number.
        /// Refused for a real void, after the 7 days, or when the trip has since been billed by another invoice.
        /// </summary>
        public static void ReinstateInvoice(int invoiceId)
        {
            EnsureNumberSchema();
            string number; int deliveryId, specialTripId; decimal net;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction(deferred: false);
            using (var c = conn.CreateCommand())
            {
                c.Transaction = tx;
                c.CommandText = "SELECT COALESCE(InvoiceNumber,''), COALESCE(Status,''), COALESCE(CancelKind,''), COALESCE(ReenterUntil,''), COALESCE(DeliveryId,0), COALESCE(SpecialTripId,0), COALESCE(NetAmount,0) FROM Invoices WHERE Id = @id";
                c.Parameters.AddWithValue("@id", invoiceId);
                using var r = c.ExecuteReader();
                if (!r.Read()) throw new InvalidOperationException("That invoice no longer exists.");
                number = r.GetString(0);
                if (!r.GetString(1).Equals("Cancelled", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Invoice {number} is not cancelled.");
                if (r.GetString(2) != "TypingMistake")
                    throw new InvalidOperationException($"Invoice {number} was cancelled because the paper invoice is void, so its number can never be used again and it can't be reinstated. Enter a new invoice with a new number.");
                DateTime? until = DateTime.TryParse(r.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.None, out var u) ? u : null;
                if (!until.HasValue || until.Value < DateTime.Now)
                    throw new InvalidOperationException($"Invoice {number} was cancelled as a typing mistake, but the {PendingReenterDays} days to correct it ended on {until:MMM d, yyyy}. Its number is now used for good; enter a new invoice with a new number.");
                deliveryId = r.GetInt32(4); specialTripId = r.GetInt32(5); net = Centavos(Convert.ToDecimal(r.GetValue(6)));
            }
            var live = GetLiveInvoiceForTrip(deliveryId, specialTripId, invoiceId);
            if (live != null) throw new InvalidOperationException($"The trip is already billed by {live.Describe()}, so invoice {number} can't be reinstated.");
            string status = net > 0 ? "Unpaid" : "Draft";
            using (var u = conn.CreateCommand())
            {
                u.Transaction = tx;
                u.CommandText = "UPDATE Invoices SET Status = @s, ReinstatedAt = @now, ReenterUntil = NULL WHERE Id = @id";
                u.Parameters.AddWithValue("@s", status);
                u.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                u.Parameters.AddWithValue("@id", invoiceId);
                u.ExecuteNonQuery();
            }
            RecalculateInvoiceStatus(conn, tx, invoiceId);
            SyncTripReportForInvoice(conn, tx, invoiceId);
            tx.Commit();
            try { LogAuditTrail("INVOICE_REINSTATED", "Invoices", invoiceId, number, "Status", "Cancelled", status, $"Invoice {number} reopened to correct a typing mistake; its number was kept"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        /// <summary>True when the invoice was cancelled as a typing mistake and can still be reinstated (7 days).</summary>
        public static bool CanReinstateInvoice(Models.Invoice inv)
        {
            if (inv == null || inv.Id <= 0 || !string.Equals(inv.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)) return false;
            try
            {
                var e = LoadNumberIndex().Entries.Find(x => x.Kind == NumberKind.SI && x.OwnerId == inv.Id);
                return e != null && e.State == "Pending";
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); return false; }
        }

        /// <summary>The "Cancelled as typing mistakes" report: who cancelled what, why, and whether it was reinstated, is still waiting, or expired.</summary>
        public static List<TypingMistakeRow> GetTypingMistakeCancellations()
        {
            EnsureNumberSchema();
            var list = new List<TypingMistakeRow>();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var c = conn.CreateCommand();
            c.CommandText = @"SELECT Id, COALESCE(InvoiceNumber,''), COALESCE(ClientName,''), COALESCE(NetAmount,0), COALESCE(CancelledAt,''), COALESCE(CancelledBy,''),
                                     COALESCE(CancelReason,''), COALESCE(ReenterUntil,''), COALESCE(ReinstatedAt,''), COALESCE(Status,'')
                              FROM Invoices WHERE COALESCE(CancelKind,'') = 'TypingMistake' ORDER BY CancelledAt DESC, Id DESC";
            using var r = c.ExecuteReader();
            while (r.Read())
            {
                var row = new TypingMistakeRow
                {
                    InvoiceId = r.GetInt32(0), Number = r.GetString(1), Client = r.GetString(2), Net = Centavos(Convert.ToDecimal(r.GetValue(3))),
                    CancelledAt = ParseWhen(r.GetString(4)), CancelledBy = r.GetString(5), Reason = r.GetString(6), Until = ParseWhen(r.GetString(7)), ReinstatedAt = ParseWhen(r.GetString(8))
                };
                bool cancelled = r.GetString(9).Equals("Cancelled", StringComparison.OrdinalIgnoreCase);
                row.State = !cancelled ? "Reinstated" : row.Until.HasValue && row.Until.Value >= DateTime.Now ? "Waiting" : "Expired";
                list.Add(row);
            }
            return list;
        }
    }
}
