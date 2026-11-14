using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>All-in-One Summary under the trip reports table (follows search and filters, and the clicked report).</summary>
    public partial class TripReportView
    {
        private AllInOneSummaryBar? tripListSummary;
        private List<TripReport> tripListSummaryShown = new List<TripReport>();

        private void UpdateTripListSummary(List<TripReport> shown)
        {
            tripListSummaryShown = shown ?? new List<TripReport>();
            RenderTripListSummary();
        }

        private void RenderTripListSummary()
        {
            if (gridTripReports?.Parent == null) return;
            try
            {
                if (tripListSummary == null)
                {
                    tripListSummary = AllInOneSummaryBar.AttachBelow(gridTripReports, "TripReports");
                    gridTripReports.SelectionChanged += (s, e) => RenderTripListSummary();
                }
                var list = tripListSummaryShown;
                // Nov 6 workflow: proof of delivery and billing progress, not delivered crates or return rates.
                int drafts = list.Count(t => RowMenu.Is(t.Status, TripReportStatus.Draft));
                int completed = list.Count(t => RowMenu.Is(t.Status, TripReportStatus.Completed));
                int invoiced = list.Count(t => RowMenu.Is(t.Status, TripReportStatus.Invoiced));
                int noGate = list.Count(t => string.IsNullOrWhiteSpace(t.TruckTimeIn) || string.IsNullOrWhiteSpace(t.TruckTimeOut));
                var m = new List<SummaryMetric>
                {
                    new("Reports shown", list.Count.ToString("N0"), SummaryTone.Blue, $"of {allTripReports.Count:N0} trip reports", Strong: true),
                    new("Drafts", drafts.ToString("N0"), SummaryTone.Amber, "steps still to finish", Strong: drafts > 0),
                    new("Ready to invoice", completed.ToString("N0"), SummaryTone.Teal, "completed, no invoice yet", Strong: completed > 0),
                    new("Invoiced", invoiced.ToString("N0"), SummaryTone.Green, "in History"),
                    new("No gate times", noGate.ToString("N0"), SummaryTone.Red, "time in / out missing", Strong: noGate > 0),
                };
                string context = $"Trip reports shown: {list.Count:N0} (follows your search and filters)";
                string preview = $"{AllInOneSummaryBar.Count(list.Count, "report", "reports")}  ·  {completed:N0} ready to invoice  ·  {drafts:N0} draft";
                var sel = gridTripReports.SelectedRows.Count == 1 ? gridTripReports.SelectedRows[0].DataBoundItem as TripReport : null;
                if (sel != null)
                {
                    m.Add(new SummaryMetric("Selected report", sel.TripReportNumber, SummaryTone.Purple, $"{sel.DispatchNumber} · {Fmt.Date(sel.ReportDate)}", Strong: true));
                    m.Add(new SummaryMetric("Its status", sel.Status, SummaryTone.Teal, $"gate {(string.IsNullOrWhiteSpace(sel.TruckTimeIn) ? "—" : sel.TruckTimeIn)} – {(string.IsNullOrWhiteSpace(sel.TruckTimeOut) ? "—" : sel.TruckTimeOut)}"));
                    context = $"Selected report {sel.TripReportNumber} — dispatch {sel.DispatchNumber}   ·   Trip reports shown: {list.Count:N0}";
                }
                tripListSummary.SetMetrics(m, preview, context);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
}
