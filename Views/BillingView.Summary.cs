using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>All-in-One Summary under the billing invoices table (follows search and filters, and the clicked invoice).</summary>
    public partial class BillingView
    {
        private AllInOneSummaryBar? billingSummary;
        private List<Invoice> billingSummaryShown = new List<Invoice>();
        private Dictionary<int, decimal> billingPaid = new Dictionary<int, decimal>();

        private void UpdateBillingSummary(List<Invoice> shown, bool reloadPayments)
        {
            billingSummaryShown = shown ?? new List<Invoice>();
            if (reloadPayments || billingPaid.Count == 0)
            {
                try { billingPaid = MJdatabase.GetCollectionReceipts().Where(r => r.InvoiceId > 0).GroupBy(r => r.InvoiceId).ToDictionary(g => g.Key, g => g.Sum(r => r.AmountPaid)); }
                catch (Exception ex) { ErrLog.Swallowed(ex); billingPaid = new Dictionary<int, decimal>(); }
            }
            RenderBillingSummary();
        }

        private void RenderBillingSummary()
        {
            if (gridInvoices?.Parent == null) return;
            try
            {
                if (billingSummary == null)
                {
                    billingSummary = AllInOneSummaryBar.AttachBelow(gridInvoices, "Billing");
                    gridInvoices.SelectionChanged += (s, e) => RenderBillingSummary();
                }
                var list = billingSummaryShown.Where(i => !RowMenu.Is(i.Status, "Cancelled", "Void", "Voided")).ToList();
                decimal Paid(Invoice i) => billingPaid.TryGetValue(i.Id, out var p) ? p : 0m;
                decimal Balance(Invoice i) => Math.Max(0m, i.NetAmount - Paid(i));
                decimal billed = list.Sum(i => i.NetAmount);
                decimal collected = list.Sum(i => Math.Min(i.NetAmount, Paid(i)));
                decimal outstanding = list.Sum(Balance);
                var overdue = list.Where(i => Balance(i) > 0 && i.DueDate.Date < DateTime.Today).ToList();
                int paidCount = list.Count(i => Balance(i) == 0 && i.NetAmount > 0);
                int unpaid = list.Count(i => Balance(i) > 0 && Paid(i) == 0);
                var m = new List<SummaryMetric>
                {
                    new("Invoices shown", billingSummaryShown.Count.ToString("N0"), SummaryTone.Blue, $"of {allInvoices.Count:N0} invoices", Strong: true),
                    new("Total billed", AllInOneSummaryBar.Money(billed), SummaryTone.Teal, "net amount"),
                    new("Collected", AllInOneSummaryBar.Money(collected), SummaryTone.Green, "paid so far"),
                    new("Outstanding", AllInOneSummaryBar.Money(outstanding), SummaryTone.Amber, "still to collect", Strong: outstanding > 0),
                    new("Overdue", overdue.Count.ToString("N0"), SummaryTone.Red, overdue.Count > 0 ? AllInOneSummaryBar.Money(overdue.Sum(Balance)) + " past due" : "none past due", Strong: overdue.Count > 0),
                    new("Paid in full", paidCount.ToString("N0"), SummaryTone.Green, "invoices"),
                    new("Unpaid", unpaid.ToString("N0"), SummaryTone.Purple, "invoices"),
                };
                string context = $"Invoices shown: {billingSummaryShown.Count:N0} (follows your search and filters)";
                string preview = $"{AllInOneSummaryBar.Count(billingSummaryShown.Count, "invoice", "invoices")}  ·  {AllInOneSummaryBar.Money(outstanding)} outstanding  ·  {overdue.Count:N0} overdue";
                var sel = gridInvoices.SelectedRows.Count == 1 ? gridInvoices.SelectedRows[0].DataBoundItem as Invoice : null;
                if (sel != null)
                {
                    m.Add(new SummaryMetric("Selected invoice", sel.InvoiceNumber, SummaryTone.Purple, $"{sel.Status} · {sel.ClientName}", Strong: true));
                    m.Add(new SummaryMetric("Its amount", AllInOneSummaryBar.Money(sel.NetAmount), SummaryTone.Teal, $"due {sel.DueDate:MMM d, yyyy}"));
                    m.Add(new SummaryMetric(Paid(sel) > 0 ? "Paid in full" : "To collect in full", AllInOneSummaryBar.Money(Paid(sel) > 0 ? Paid(sel) : Balance(sel)), Paid(sel) > 0 ? SummaryTone.Green : SummaryTone.Amber, Paid(sel) > 0 ? "collected" : "one receipt for the full amount", Strong: Balance(sel) > 0));
                    context = $"Selected invoice {sel.InvoiceNumber} — {sel.ClientName} · {sel.Status}   ·   Invoices shown: {billingSummaryShown.Count:N0}";
                }
                billingSummary.SetMetrics(m, preview, context);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
}
