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
                int Del(TripReport t) => t.DeliveredCrates > 0 ? t.DeliveredCrates : Math.Max(0, t.DeliveredGCrates) + Math.Max(0, t.DeliveredECrates);
                int Ret(TripReport t) => t.ReturnedCrates > 0 ? t.ReturnedCrates : Math.Max(0, t.ReturnedGCrates) + Math.Max(0, t.ReturnedECrates);
                int Short(TripReport t) => Math.Max(0, t.DeliveredGCrates - t.ReturnedGCrates) + Math.Max(0, t.DeliveredECrates - t.ReturnedECrates);
                int delivered = list.Sum(Del), returned = list.Sum(Ret), missing = list.Sum(Short);
                int withShort = list.Count(t => Short(t) > 0);
                var m = new List<SummaryMetric>
                {
                    new("Reports shown", list.Count.ToString("N0"), SummaryTone.Blue, $"of {allTripReports.Count:N0} trip reports", Strong: true),
                    new("Crates delivered", delivered.ToString("N0"), SummaryTone.Teal, "on the reports shown"),
                    new("Crates returned", returned.ToString("N0"), SummaryTone.Green, "empties brought back"),
                    new("Missing crates", missing.ToString("N0"), SummaryTone.Amber, "delivered but not returned", Strong: missing > 0),
                    new("Trips with shortages", withShort.ToString("N0"), SummaryTone.Red, "reports", Strong: withShort > 0),
                    new("Complete trips", (list.Count - withShort).ToString("N0"), SummaryTone.Green, "every crate returned"),
                };
                string context = $"Trip reports shown: {list.Count:N0} (follows your search and filters)";
                string preview = $"{AllInOneSummaryBar.Count(list.Count, "report", "reports")}  ·  {delivered:N0} delivered  ·  {missing:N0} missing";
                var sel = gridTripReports.SelectedRows.Count == 1 ? gridTripReports.SelectedRows[0].DataBoundItem as TripReport : null;
                if (sel != null)
                {
                    m.Add(new SummaryMetric("Selected report", sel.TripReportNumber, SummaryTone.Purple, $"{sel.DispatchNumber} · {sel.ReportDate:MMM d, yyyy}", Strong: true));
                    m.Add(new SummaryMetric("Its crates", $"{Del(sel):N0} / {Ret(sel):N0}", SummaryTone.Teal, "delivered / returned"));
                    m.Add(new SummaryMetric("Its missing crates", Short(sel).ToString("N0"), SummaryTone.Amber, Short(sel) > 0 ? "not returned" : "all returned", Strong: Short(sel) > 0));
                    context = $"Selected report {sel.TripReportNumber} — dispatch {sel.DispatchNumber}   ·   Trip reports shown: {list.Count:N0}";
                }
                tripListSummary.SetMetrics(m, preview, context);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
}
