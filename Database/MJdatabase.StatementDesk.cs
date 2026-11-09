using System;
using System.Collections.Generic;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>One statement of account that still has trips without their service invoice (a special trip waiting for its invoice is listed the same way).</summary>
    public sealed class WaitingSoaRow
    {
        public int SoaId;
        public string SoaNumber = "";
        public string ClientName = "";
        public DateTime IssueDate;
        public string Period = "";
        public int TripsToInvoice;
        public decimal Total;
        public int DaysWaiting;
        /// <summary>Set for a special trip that waits for its invoice (old route); null for a statement.</summary>
        public WaitingTripRow? Special;
    }

    /// <summary>One line of the Finance History > Statement history ledger.</summary>
    public sealed class StatementHistoryRow
    {
        public StatementOfAccount Soa = new();
        public int Trips;
        public int TripsWithoutInvoice;
        public string Invoices = "";
        /// <summary>"Waiting for invoice", "Invoiced", "Partly collected", "Collected" or "Cancelled".</summary>
        public string Stage = "";
    }

    public static partial class MJdatabase
    {
        /// <summary>
        /// Statements (not cancelled) with trips that still have no service invoice, oldest first, plus special trips that wait for their invoice.
        /// This is the worklist at the top of Billing (statement first: the invoice is made from the statement).
        /// </summary>
        public static List<WaitingSoaRow> GetStatementsWaitingForInvoice()
        {
            var rows = new List<WaitingSoaRow>();
            var missing = GetSoaTripsWithoutInvoice();
            DateTime today = DateTime.Today;
            foreach (var s in GetStatementsOfAccount())
            {
                if (!missing.TryGetValue(s.Id, out int n) || n <= 0) continue;
                if (string.Equals(s.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)) continue;
                rows.Add(new WaitingSoaRow { SoaId = s.Id, SoaNumber = s.SoaNumber, ClientName = s.ClientName, IssueDate = s.IssueDate, Period = s.PeriodCovered, TripsToInvoice = n, Total = s.TotalAmount,
                                             DaysWaiting = Math.Max(0, (int)(today - s.IssueDate.Date).TotalDays) });
            }
            foreach (var t in GetTripsWaitingForInvoice().Where(w => w.SpecialTripId > 0))
                rows.Add(new WaitingSoaRow { SoaNumber = t.Number, ClientName = t.ClientName, IssueDate = t.Date, Period = "Special trip", TripsToInvoice = 1, Total = t.Expected, DaysWaiting = t.DaysWaiting, Special = t });
            return rows.OrderByDescending(r => r.DaysWaiting).ThenBy(r => r.SoaNumber, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>The dispatches of this statement that have no service invoice yet (to invoice them from the statement).</summary>
        public static List<int> GetSoaDeliveryIdsWithoutInvoice(int soaId)
        {
            var live = GetLiveInvoicesByDelivery();
            return GetStatementOfAccountItems(soaId).Where(i => i.DeliveryId > 0).Select(i => i.DeliveryId).Distinct().Where(id => !live.ContainsKey(id)).ToList();
        }

        /// <summary>Every statement ever saved, newest first, with where it stands. Saved automatically when the statement is saved (nothing to do).</summary>
        public static List<StatementHistoryRow> GetStatementHistory()
        {
            var result = new List<StatementHistoryRow>();
            var missing = GetSoaTripsWithoutInvoice();
            var invoices = GetTripInvoices();
            foreach (var s in GetStatementsOfAccount().OrderByDescending(x => x.Id))
            {
                var mine = invoices.Where(i => i.SoaId == s.Id).ToList();
                var items = GetStatementOfAccountItems(s.Id);
                int trips = items.Where(i => i.DeliveryId > 0 || i.SpecialTripId > 0).Select(i => (i.DeliveryId, i.SpecialTripId)).Distinct().Count();
                missing.TryGetValue(s.Id, out int miss);
                string stage;
                if (string.Equals(s.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)) stage = "Cancelled";
                else if (string.Equals(s.Status, PayStatus.Paid, StringComparison.OrdinalIgnoreCase)) stage = "Collected";
                else if (miss > 0) stage = "Waiting for invoice";
                else if (mine.Any(i => i.Collected > 0)) stage = "Partly collected";
                else stage = "Invoiced";
                result.Add(new StatementHistoryRow { Soa = s, Trips = trips, TripsWithoutInvoice = miss, Invoices = string.Join(", ", mine.Select(i => i.Number)), Stage = stage });
            }
            return result;
        }
    }
}
