using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>All-in-One Summary under the dispatch orders table: the orders shown (follows search and filters) and the clicked order.</summary>
    public partial class DispatchManagementView
    {
        private AllInOneSummaryBar? dispatchSummary;
        private List<Delivery> dispatchSummaryShown = new List<Delivery>();

        private void UpdateDispatchSummary(List<Delivery> shown)
        {
            dispatchSummaryShown = shown ?? new List<Delivery>();
            RenderDispatchSummary();
        }

        private void RenderDispatchSummary()
        {
            if (gridDispatch?.Parent == null) return;
            try
            {
                if (dispatchSummary == null)
                {
                    dispatchSummary = AllInOneSummaryBar.AttachBelow(gridDispatch, "Dispatch");
                    gridDispatch.SelectionChanged += (s, e) => RenderDispatchSummary();
                }
                var list = dispatchSummaryShown;
                bool Cancelled(Delivery d) => RowMenu.Is(d.Status, "Cancelled");
                int CratesOf(Delivery d) => d.TotalCrates > 0 ? d.TotalCrates : Math.Max(0, d.GCrate) + Math.Max(0, d.ECrate);
                int waiting = list.Count(d => RowMenu.Is(d.Status, "Pending", "Scheduled", "Assigned", "Loading"));
                int road = list.Count(d => RowMenu.Is(d.Status, "In Transit", "Dispatched"));
                int delivered = list.Count(d => RowMenu.Is(d.Status, "Delivered"));
                int completed = list.Count(d => RowMenu.Is(d.Status, "Completed"));
                int cancelled = list.Count(Cancelled);
                int crates = list.Where(d => !Cancelled(d)).Sum(CratesOf);
                int trucks = list.Where(d => !Cancelled(d) && d.TruckId > 0).Select(d => d.TruckId).Distinct().Count();
                var m = new List<SummaryMetric>
                {
                    new("Orders shown", list.Count.ToString("N0"), SummaryTone.Blue, $"of {allDispatches.Count:N0} orders", Strong: true),
                    new("Waiting to leave", waiting.ToString("N0"), SummaryTone.Purple, "pending / scheduled"),
                    new("On the road", road.ToString("N0"), SummaryTone.Amber, "in transit now", Strong: road > 0),
                    new("Delivered", delivered.ToString("N0"), SummaryTone.Teal, "waiting to be completed"),
                    new("Completed", completed.ToString("N0"), SummaryTone.Green, "closed orders"),
                    new("Cancelled", cancelled.ToString("N0"), SummaryTone.Red, "not counted in crates"),
                    new("Total crates", crates.ToString("N0"), SummaryTone.Blue, "on the orders shown"),
                    new("Trucks used", trucks.ToString("N0"), SummaryTone.Neutral, "different trucks"),
                };
                string context = $"Orders shown: {list.Count:N0} (follows your search and filters)";
                string preview = $"{AllInOneSummaryBar.Count(list.Count, "order", "orders")}  ·  {road:N0} on the road  ·  {crates:N0} crates";
                var sel = gridDispatch.SelectedRows.Count == 1 ? gridDispatch.SelectedRows[0].DataBoundItem as Delivery : null;
                if (sel != null)
                {
                    int sc = CratesOf(sel);
                    int stops = Math.Max(1, sel.TotalStopsCount);
                    m.Add(new SummaryMetric("Selected order", sel.DispatchNumber, SummaryTone.Purple, $"{sel.Status} · {sel.ClientName}", Strong: true));
                    m.Add(new SummaryMetric("Its crates", sc.ToString("N0"), SummaryTone.Teal, $"G {sel.GCrate:N0} · E {sel.ECrate:N0}"));
                    m.Add(new SummaryMetric("Its stores", stops.ToString("N0"), SummaryTone.Neutral, sel.CleanRouteDisplay));
                    m.Add(new SummaryMetric("Truck & driver", string.IsNullOrWhiteSpace(sel.TruckInfo) ? "—" : sel.TruckInfo, SummaryTone.Neutral, string.IsNullOrWhiteSpace(sel.DriverName) ? "no driver yet" : sel.DriverName));
                    context = $"Selected order {sel.DispatchNumber} — {sel.ClientName} · {Fmt.Date(sel.DispatchDate)} · {sel.Status}   ·   Orders shown: {list.Count:N0}";
                    preview += $"  ·  selected {sel.DispatchNumber}: {sc:N0} crates";
                }
                dispatchSummary.SetMetrics(m, preview, context);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
}
