using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>All-in-One Summary under the statements table (follows search and filters, and the clicked statement).</summary>
    public partial class StatementOfAccountView
    {
        private AllInOneSummaryBar? soaSummary;
        private List<StatementOfAccount> soaSummaryShown = new List<StatementOfAccount>();

        private void UpdateSoaSummary(List<StatementOfAccount> shown)
        {
            soaSummaryShown = shown ?? new List<StatementOfAccount>();
            RenderSoaSummary();
        }

        private void RenderSoaSummary()
        {
            if (gridSoa?.Parent == null) return;
            try
            {
                if (soaSummary == null)
                {
                    soaSummary = AllInOneSummaryBar.AttachBelow(gridSoa, "Soa");
                    gridSoa.SelectionChanged += (s, e) => RenderSoaSummary();
                }
                var list = soaSummaryShown.Where(s => !RowMenu.Is(s.Status, "Cancelled", "Void", "Voided")).ToList();
                int cancelled = soaSummaryShown.Count - list.Count;
                decimal billed = list.Sum(s => s.TotalAmount);
                decimal vat = list.Sum(s => s.VatAmount);
                decimal wht = list.Sum(s => s.WithholdingTax);
                int paid = list.Count(s => RowMenu.Is(s.Status, "Paid"));
                int unpaid = list.Count(s => RowMenu.Is(s.Status, PayStatus.Unpaid));
                int noInvoice = list.Count(s => RowMenu.Is(s.Status, "No invoice") || (string.IsNullOrWhiteSpace(s.SalesInvoiceNo) && !RowMenu.Is(s.Status, "Paid", "Unpaid", "Draft")));
                int drafts = list.Count(s => RowMenu.Is(s.Status, "Draft"));
                var m = new List<SummaryMetric>
                {
                    new("Statements shown", soaSummaryShown.Count.ToString("N0"), SummaryTone.Blue, $"of {allSoas.Count:N0} statements", Strong: true),
                    new("Total billed", AllInOneSummaryBar.Money(billed), SummaryTone.Teal, "amount due on the statements"),
                    new("VAT", AllInOneSummaryBar.Money(vat), SummaryTone.Neutral, "included above"),
                    new("Withholding tax", AllInOneSummaryBar.Money(wht), SummaryTone.Neutral, "2% EWT"),
                    new("Paid", paid.ToString("N0"), SummaryTone.Green, "statements fully paid"),
                    new("Unpaid", unpaid.ToString("N0"), SummaryTone.Amber, "awaiting the collection receipt"),
                    new("No invoice yet", noInvoice.ToString("N0"), SummaryTone.Red, "need a sales invoice", Strong: noInvoice > 0),
                    new("Drafts / cancelled", $"{drafts:N0} / {cancelled:N0}", SummaryTone.Purple, "not billed"),
                };
                string context = $"Statements shown: {soaSummaryShown.Count:N0} (follows your search and filters)";
                string preview = $"{AllInOneSummaryBar.Count(soaSummaryShown.Count, "statement", "statements")}  ·  {AllInOneSummaryBar.Money(billed)} billed  ·  {noInvoice:N0} without invoice";
                var sel = gridSoa.SelectedRows.Count == 1 ? gridSoa.SelectedRows[0].DataBoundItem as StatementOfAccount : null;
                if (sel != null)
                {
                    m.Add(new SummaryMetric("Selected statement", sel.SoaNumber, SummaryTone.Purple, $"{sel.Status} · {sel.ClientName}", Strong: true));
                    m.Add(new SummaryMetric("Its amount", AllInOneSummaryBar.Money(sel.TotalAmount), SummaryTone.Teal, string.IsNullOrWhiteSpace(sel.PeriodCovered) ? Fmt.Date(sel.IssueDate) : sel.PeriodCovered));
                    m.Add(new SummaryMetric("Its sales invoice", string.IsNullOrWhiteSpace(sel.SalesInvoiceNo) ? "—" : sel.SalesInvoiceNo, SummaryTone.Neutral, string.IsNullOrWhiteSpace(sel.SalesInvoiceNo) ? "no invoice yet" : "linked invoice"));
                    context = $"Selected statement {sel.SoaNumber} — {sel.ClientName} · {sel.Status}   ·   Statements shown: {soaSummaryShown.Count:N0}";
                }
                soaSummary.SetMetrics(m, preview, context);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
}
