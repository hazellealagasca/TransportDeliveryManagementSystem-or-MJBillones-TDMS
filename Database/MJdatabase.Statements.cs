using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>One service invoice that is issued, unpaid and on no statement: it is ready to be put on a statement of account.</summary>
    public sealed class ReadyInvoiceRow
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public string ClientName { get; set; } = "";
        public int DeliveryId { get; set; }
        public int SpecialTripId { get; set; }
        /// <summary>Dispatch number or special-trip order reference.</summary>
        public string TripNumber { get; set; } = "";
        public DateTime TripDate { get; set; }
        public DateTime IssueDate { get; set; }
        public decimal Net { get; set; }
        /// <summary>"Normal" (a dispatch) or "Special" (a special trip): the two kinds go on separate statements.</summary>
        public string Kind { get; set; } = SoaTypes.Normal;
        public int DaysWaiting { get; set; }
    }

    /// <summary>Who cancelled a statement, when and why (empty fields when it is not cancelled or was cancelled before this was recorded).</summary>
    public sealed class SoaCancelInfo
    {
        public string By = "", Reason = "";
        public DateTime? At;
        public bool Any => By.Length > 0 || Reason.Length > 0 || At.HasValue;
    }

    public static partial class MJdatabase
    {
        public const int MinCancelReasonLength = 5;
        private static bool soaCancelColumnsReady;

        private static void EnsureSoaCancelColumns()
        {
            if (soaCancelColumnsReady) return;
            try
            {
                EnsureSpecialTripColumns();
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                foreach (var (col, ddl) in new[] { ("CancelReason", "TEXT"), ("CancelledAt", "TEXT"), ("CancelledBy", "TEXT") })
                    AddColumnIfMissing(conn, "StatementsOfAccount", col, ddl);
                soaCancelColumnsReady = true;
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        /// <summary>
        /// Every service invoice that can go on a statement now: issued with an amount, nothing paid, on no live statement, billing a dispatch or a
        /// special trip. (Old statements' own whole-statement invoices are never listed.) Oldest first. This is the one list the Statement of
        /// Account screen uses for "ready for a statement".
        /// </summary>
        public static List<ReadyInvoiceRow> GetInvoicesReadyForStatement()
        {
            var rows = new List<ReadyInvoiceRow>();
            var deliveries = GetDeliveries().GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.First());
            var specials = GetSpecialTrips().GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.First());
            foreach (var i in GetTripInvoices().Where(i => i.Open && i.SoaId == 0 && i.Kind.Length > 0))
            {
                string number = "", client = i.Client ?? "";
                DateTime tripDate = i.IssueDate;
                if (i.SpecialTripId > 0 && specials.TryGetValue(i.SpecialTripId, out var st)) { number = st.OrderReference; tripDate = st.DispatchDate; if (client.Length == 0) client = st.Company ?? ""; }
                else if (i.DeliveryId > 0 && deliveries.TryGetValue(i.DeliveryId, out var d)) { number = d.DispatchNumber; tripDate = d.DispatchDate; if (client.Length == 0) client = d.ClientName ?? ""; }
                rows.Add(new ReadyInvoiceRow
                {
                    InvoiceId = i.Id, InvoiceNumber = i.Number, ClientName = client.Trim(), DeliveryId = i.DeliveryId, SpecialTripId = i.SpecialTripId,
                    TripNumber = number, TripDate = tripDate, IssueDate = i.IssueDate, Net = i.Net, Kind = i.Kind,
                    DaysWaiting = Math.Max(0, (int)(DateTime.Today - i.IssueDate.Date).TotalDays)
                });
            }
            return rows.OrderBy(r => r.ClientName, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.IssueDate).ThenBy(r => r.InvoiceNumber, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>Empty when the statement can be cancelled; otherwise the reason (already cancelled, or a receipt must be voided first).</summary>
        public static string WhySoaCannotBeCancelled(StatementOfAccount soa)
        {
            if (soa == null || soa.Id <= 0) return "Select a statement first.";
            string st = (soa.Status ?? "").Trim();
            if (st.StartsWith("Cancelled", StringComparison.OrdinalIgnoreCase) || st.StartsWith("Void", StringComparison.OrdinalIgnoreCase))
                return $"Statement {soa.SoaNumber} is already cancelled.";
            int receipts = LiveReceiptCountForSoa(soa.Id);
            if (receipts > 0)
                return $"Statement {soa.SoaNumber} has been paid ({receipts} collection receipt{(receipts == 1 ? "" : "s")}). Void the receipt first in Collections & Receipts, then cancel the statement.";
            return "";
        }

        /// <summary>Number of collection receipts that are not voided on the invoices of this statement.</summary>
        public static int LiveReceiptCountForSoa(int soaId)
        {
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var c = conn.CreateCommand();
                c.CommandText = "SELECT COUNT(*) FROM CollectionReceipts r JOIN Invoices v ON v.Id = r.InvoiceId WHERE v.SoaId = @id AND COALESCE(r.IsVoid,0) = 0";
                c.Parameters.AddWithValue("@id", soaId);
                return Convert.ToInt32(c.ExecuteScalar() ?? 0);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); return 0; }
        }

        /// <summary>
        /// Cancels a statement for a stated reason. Its service invoices go back to "ready for a statement", its trips stay billed by those invoices,
        /// and the reason, user and time are kept (list, audit log). Refuses a statement that is already cancelled or has a live receipt.
        /// </summary>
        public static void CancelStatementOfAccount(int id, string reason)
        {
            reason = (reason ?? "").Trim();
            if (reason.Length < MinCancelReasonLength)
                throw new InvalidOperationException($"Write why the statement is cancelled (at least {MinCancelReasonLength} characters).");
            var soa = GetStatementOfAccountById(id) ?? throw new InvalidOperationException("That statement no longer exists.");
            string why = WhySoaCannotBeCancelled(soa);
            if (why.Length > 0) throw new InvalidOperationException(why);
            EnsureSoaCancelColumns();
            RestoreInvoiceAmountsOfSoa(id); // invoices whose amounts this statement replaced get their earlier amounts back
            DeleteStatementOfAccount(id); // soft delete: status Cancelled, invoices freed, trip reports resynced, one audit entry
            string user = SessionContext.ActiveUser?.Username ?? SessionContext.CurrentUsername;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var c = conn.CreateCommand();
                c.CommandText = "UPDATE StatementsOfAccount SET CancelReason = @r, CancelledAt = @at, CancelledBy = @by WHERE Id = @id";
                c.Parameters.AddWithValue("@r", reason);
                c.Parameters.AddWithValue("@at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                c.Parameters.AddWithValue("@by", user ?? "");
                c.Parameters.AddWithValue("@id", id);
                c.ExecuteNonQuery();
            }
            LogAuditTrail("CANCEL", "StatementsOfAccount", id, soa.SoaNumber, "Reason", "", reason, "Statement cancelled: " + reason);
        }

        /// <summary>Who cancelled the statement and why.</summary>
        public static SoaCancelInfo GetSoaCancelInfo(int id)
        {
            var info = new SoaCancelInfo();
            try
            {
                EnsureSoaCancelColumns();
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var c = conn.CreateCommand();
                c.CommandText = "SELECT COALESCE(CancelReason,''), COALESCE(CancelledBy,''), COALESCE(CancelledAt,'') FROM StatementsOfAccount WHERE Id = @id";
                c.Parameters.AddWithValue("@id", id);
                using var r = c.ExecuteReader();
                if (r.Read())
                {
                    info.Reason = r.GetString(0); info.By = r.GetString(1);
                    if (DateTime.TryParse(r.GetString(2), out var at)) info.At = at;
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return info;
        }
    }
}
