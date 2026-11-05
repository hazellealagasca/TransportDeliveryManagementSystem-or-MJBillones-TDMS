using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>All-in-One Summary under the collections & receipts table (follows search and filters, and the clicked receipt).</summary>
    public partial class CollectionView
    {
        private AllInOneSummaryBar? collectionSummary;
        private List<CollectionReceipt> collectionSummaryShown = new List<CollectionReceipt>();

        private void UpdateCollectionSummary(List<CollectionReceipt> shown)
        {
            collectionSummaryShown = shown ?? new List<CollectionReceipt>();
            RenderCollectionSummary();
        }

        private void RenderCollectionSummary()
        {
            if (gridCollections?.Parent == null) return;
            try
            {
                if (collectionSummary == null)
                {
                    collectionSummary = AllInOneSummaryBar.AttachBelow(gridCollections, "Collections");
                    gridCollections.SelectionChanged += (s, e) => RenderCollectionSummary();
                }
                var list = collectionSummaryShown;
                decimal total = list.Sum(r => r.AmountPaid);
                bool Method(CollectionReceipt r, params string[] words) => words.Any(w => (r.PaymentMethod ?? "").IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);
                decimal cash = list.Where(r => Method(r, "cash")).Sum(r => r.AmountPaid);
                decimal check = list.Where(r => Method(r, "check", "cheque")).Sum(r => r.AmountPaid);
                decimal bank = list.Where(r => !Method(r, "cash", "check", "cheque")).Sum(r => r.AmountPaid);
                var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                decimal thisMonth = list.Where(r => r.PaymentDate >= monthStart).Sum(r => r.AmountPaid);
                int invoices = list.Where(r => r.InvoiceId > 0).Select(r => r.InvoiceId).Distinct().Count();
                var last = list.OrderByDescending(r => r.PaymentDate).FirstOrDefault();
                var m = new List<SummaryMetric>
                {
                    new("Receipts shown", list.Count.ToString("N0"), SummaryTone.Blue, $"of {allReceipts.Count:N0} receipts", Strong: true),
                    new("Amount collected", AllInOneSummaryBar.Money(total), SummaryTone.Green, "on the receipts shown", Strong: true),
                    new("This month", AllInOneSummaryBar.Money(thisMonth), SummaryTone.Teal, monthStart.ToString("MMMM yyyy")),
                    new("Cash", AllInOneSummaryBar.Money(cash), SummaryTone.Neutral, "paid in cash"),
                    new("Check", AllInOneSummaryBar.Money(check), SummaryTone.Purple, "paid by check"),
                    new("Bank / online", AllInOneSummaryBar.Money(bank), SummaryTone.Amber, "transfer, deposit, e-wallet"),
                    new("Invoices paid", invoices.ToString("N0"), SummaryTone.Green, "different invoices"),
                    new("Last payment", last == null ? "—" : last.PaymentDate.ToString("MMM d"), SummaryTone.Neutral, last == null ? "no receipts" : $"{last.ReceiptNumber} · {AllInOneSummaryBar.Money(last.AmountPaid)}"),
                };
                string context = $"Receipts shown: {list.Count:N0} (follows your search and filters)";
                string preview = $"{AllInOneSummaryBar.Count(list.Count, "receipt", "receipts")}  ·  {AllInOneSummaryBar.Money(total)} collected";
                var sel = gridCollections.SelectedRows.Count == 1 ? gridCollections.SelectedRows[0].DataBoundItem as CollectionReceipt : null;
                if (sel != null)
                {
                    m.Add(new SummaryMetric("Selected receipt", sel.ReceiptNumber, SummaryTone.Purple, $"{sel.PaymentDate:MMM d, yyyy} · {sel.PaymentMethod}", Strong: true));
                    m.Add(new SummaryMetric("Its amount", AllInOneSummaryBar.Money(sel.AmountPaid), SummaryTone.Green, $"for invoice {sel.InvoiceNumber}"));
                    context = $"Selected receipt {sel.ReceiptNumber} — invoice {sel.InvoiceNumber}   ·   Receipts shown: {list.Count:N0}";
                }
                collectionSummary.SetMetrics(m, preview, context);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
}
