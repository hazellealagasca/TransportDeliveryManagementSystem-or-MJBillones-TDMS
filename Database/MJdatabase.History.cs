using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>One line of Finance History: a completed (paid / issued) or a voided (cancelled) statement, service invoice or collection receipt.</summary>
    public sealed class FinanceHistoryRow
    {
        public const string Soa = "Statement of Account", Invoice = "Service invoice", Receipt = "Collection receipt";
        public string Kind = "", Number = "", Client = "", Related = "", Reason = "", By = "";
        public decimal Amount;
        /// <summary>Completed list: the payment (completion) date. Voided list: the date it was cancelled / voided.</summary>
        public DateTime Date;
        public int Id;
        /// <summary>True when the date is only the document's own date (no payment / void time was recorded).</summary>
        public bool DateIsEstimate;
        /// <summary>The record itself, for opening it read-only (Invoice, StatementOfAccount or the first CollectionReceipt row).</summary>
        public object? Record;
    }

    public static partial class MJdatabase
    {
        private static HashSet<string> ColumnsOf(SqliteConnection conn, string table)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var c = conn.CreateCommand();
            c.CommandText = $"PRAGMA table_info({table})";
            using var r = c.ExecuteReader();
            while (r.Read()) set.Add(r.GetString(1));
            return set;
        }

        private static DateTime ParseHistoryDate(string? s, DateTime fallback) => DateTime.TryParse(s, out var d) ? d : fallback;

        /// <summary>
        /// Finance History, Completed: every Paid statement of account, every Paid service invoice and every issued (not voided) collection receipt
        /// whose completion date (the payment date of its receipt) falls from..to, newest first (date, then id).
        /// </summary>
        public static List<FinanceHistoryRow> GetCompletedFinanceHistory(DateTime from, DateTime to)
        {
            var rows = new List<FinanceHistoryRow>();
            var invoices = GetInvoices();
            var receipts = GetCollectionReceipts(); // issued only (voided receipts are not read)
            var soas = GetStatementsOfAccount();
            var soaNo = soas.ToDictionary(s => s.Id, s => s.SoaNumber);
            var invById = invoices.ToDictionary(i => i.Id);
            bool InRange(DateTime d) => d.Date >= from.Date && d.Date <= to.Date;

            foreach (var soa in soas.Where(s => string.Equals(s.Status, PayStatus.Paid, StringComparison.OrdinalIgnoreCase)))
            {
                var mine = receipts.Where(r => r.SoaId == soa.Id || (invById.TryGetValue(r.InvoiceId, out var iv) && iv.SoaId == soa.Id)).ToList();
                DateTime when = mine.Count > 0 ? mine.Max(r => r.PaymentDate) : soa.IssueDate;
                if (!InRange(when)) continue;
                var own = invoices.Where(i => i.SoaId == soa.Id).Select(i => i.InvoiceNumber).ToList();
                rows.Add(new FinanceHistoryRow
                {
                    Kind = FinanceHistoryRow.Soa, Number = soa.SoaNumber, Client = soa.ClientName, Amount = mine.Count > 0 ? mine.Sum(r => r.AmountPaid) : soa.TotalAmount,
                    Date = when, Id = soa.Id, DateIsEstimate = mine.Count == 0, Record = soa,
                    Related = string.Join(", ", mine.Select(r => r.ReceiptNumber).Distinct().Concat(own).Take(6))
                });
            }
            foreach (var inv in invoices.Where(i => string.Equals(i.Status, PayStatus.Paid, StringComparison.OrdinalIgnoreCase)))
            {
                var mine = receipts.Where(r => r.InvoiceId == inv.Id).ToList();
                DateTime when = mine.Count > 0 ? mine.Max(r => r.PaymentDate) : inv.IssueDate;
                if (!InRange(when)) continue;
                var related = new List<string>();
                if (inv.SoaId > 0 && soaNo.TryGetValue(inv.SoaId, out var sn)) related.Add(sn);
                related.AddRange(mine.Select(r => r.ReceiptNumber).Distinct());
                rows.Add(new FinanceHistoryRow
                {
                    Kind = FinanceHistoryRow.Invoice, Number = inv.InvoiceNumber, Client = inv.ClientName, Amount = inv.NetAmount, Date = when, Id = inv.Id,
                    DateIsEstimate = mine.Count == 0, Record = inv, Related = string.Join(", ", related)
                });
            }
            foreach (var grp in receipts.GroupBy(r => (r.ReceiptNumber ?? "").Trim().ToUpperInvariant()))
            {
                var first = grp.OrderByDescending(r => r.Id).First();
                DateTime when = grp.Max(r => r.PaymentDate);
                if (!InRange(when)) continue;
                string client = grp.Select(r => invById.TryGetValue(r.InvoiceId, out var iv) ? iv.ClientName : "").FirstOrDefault(c => c.Length > 0) ?? "";
                var related = new List<string>();
                foreach (int sid in grp.Select(r => r.SoaId).Where(x => x > 0).Distinct()) if (soaNo.TryGetValue(sid, out var sn)) related.Add(sn);
                related.AddRange(grp.Select(r => r.InvoiceNumber).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct());
                rows.Add(new FinanceHistoryRow
                {
                    Kind = FinanceHistoryRow.Receipt, Number = first.ReceiptNumber, Client = client, Amount = grp.Sum(r => r.AmountPaid), Date = when, Id = grp.Max(r => r.Id),
                    Record = first, Related = string.Join(", ", related.Take(6))
                });
            }
            return rows.OrderByDescending(r => r.Date.Date).ThenByDescending(r => r.Id).ToList();
        }

        /// <summary>
        /// Finance History, Voided: cancelled statements of account, cancelled service invoices and voided collection receipts, newest first
        /// by the date they were cancelled / voided (the document's own date when that was not recorded), with the reason when one was kept.
        /// </summary>
        public static List<FinanceHistoryRow> GetVoidedFinanceHistory()
        {
            var rows = new List<FinanceHistoryRow>();
            try { EnsureNumberSchema(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            foreach (var soa in GetStatementsOfAccount().Where(s => string.Equals(s.Status, PayStatus.Cancelled, StringComparison.OrdinalIgnoreCase)))
            {
                var info = GetSoaCancelInfo(soa.Id);
                rows.Add(new FinanceHistoryRow
                {
                    Kind = FinanceHistoryRow.Soa, Number = soa.SoaNumber, Client = soa.ClientName, Amount = soa.TotalAmount, Id = soa.Id, Record = soa,
                    Date = info.At ?? soa.IssueDate, DateIsEstimate = !info.At.HasValue, Reason = info.Reason, By = info.By
                });
            }
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cols = ColumnsOf(conn, "Invoices");
                string C(string n) => cols.Contains(n) ? $"COALESCE({n},'')" : "''";
                using (var c = conn.CreateCommand())
                {
                    c.CommandText = $"SELECT Id, COALESCE(InvoiceNumber,''), COALESCE(ClientName,''), COALESCE(NetAmount,0), COALESCE(IssueDate,''), {C("CancelledAt")}, {C("CancelReason")}, {C("CancelledBy")} FROM Invoices WHERE COALESCE(Status,'') = 'Cancelled' COLLATE NOCASE";
                    using var r = c.ExecuteReader();
                    var byId = GetInvoices().ToDictionary(i => i.Id);
                    while (r.Read())
                    {
                        int id = r.GetInt32(0);
                        DateTime issued = ParseHistoryDate(r.GetString(4), DateTime.MinValue);
                        bool has = DateTime.TryParse(r.GetString(5), out var at);
                        rows.Add(new FinanceHistoryRow
                        {
                            Kind = FinanceHistoryRow.Invoice, Number = r.GetString(1), Client = r.GetString(2), Amount = Convert.ToDecimal(r.GetValue(3)), Id = id,
                            Date = has ? at : issued, DateIsEstimate = !has, Reason = r.GetString(6), By = r.GetString(7), Record = byId.TryGetValue(id, out var inv) ? inv : null
                        });
                    }
                }
                var rc = ColumnsOf(conn, "CollectionReceipts");
                string R(string n) => rc.Contains(n) ? $"COALESCE(r.{n},'')" : "''";
                using (var c = conn.CreateCommand())
                {
                    c.CommandText = $@"SELECT r.Id, COALESCE(r.ReceiptNumber,''), COALESCE(r.InvoiceNumber,''), COALESCE(r.AmountPaid,0), COALESCE(r.PaymentDate,''), {R("VoidedAt")}, {R("VoidReason")}, {R("VoidedBy")},
                                              COALESCE((SELECT v.ClientName FROM Invoices v WHERE v.Id = r.InvoiceId),'')
                                       FROM CollectionReceipts r WHERE COALESCE(r.IsVoid,0) = 1";
                    using var r = c.ExecuteReader();
                    var groups = new Dictionary<string, FinanceHistoryRow>(StringComparer.OrdinalIgnoreCase);
                    var invNos = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    while (r.Read())
                    {
                        string no = r.GetString(1).Trim();
                        decimal amt = Convert.ToDecimal(r.GetValue(3));
                        DateTime paid = ParseHistoryDate(r.GetString(4), DateTime.MinValue);
                        bool has = DateTime.TryParse(r.GetString(5), out var at);
                        if (!invNos.TryGetValue(no, out var list)) invNos[no] = list = new List<string>();
                        if (r.GetString(2).Length > 0 && !list.Contains(r.GetString(2))) list.Add(r.GetString(2));
                        if (groups.TryGetValue(no, out var g)) { g.Amount += amt; g.Id = Math.Max(g.Id, r.GetInt32(0)); if (g.Reason.Length == 0) g.Reason = r.GetString(6); continue; }
                        groups[no] = new FinanceHistoryRow
                        {
                            Kind = FinanceHistoryRow.Receipt, Number = no, Client = r.GetString(8), Amount = amt, Id = r.GetInt32(0), Date = has ? at : paid, DateIsEstimate = !has,
                            Reason = r.GetString(6), By = r.GetString(7),
                            Record = new CollectionReceipt { Id = r.GetInt32(0), ReceiptNumber = no, InvoiceNumber = r.GetString(2), AmountPaid = amt, PaymentDate = paid == DateTime.MinValue ? DateTime.Today : paid }
                        };
                    }
                    foreach (var g in groups.Values) { g.Related = string.Join(", ", invNos[g.Number].Take(6)); rows.Add(g); }
                }
            }
            return rows.OrderByDescending(r => r.Date.Date).ThenByDescending(r => r.Id).ToList();
        }
    }
}
