using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>All-in-One Summary under the incident & damage table (follows search and filters, and the clicked report).</summary>
    public partial class IncidentReportView
    {
        private AllInOneSummaryBar? incidentSummary;
        private List<IncidentReport> incidentSummaryShown = new List<IncidentReport>();

        private void UpdateIncidentSummary(List<IncidentReport> shown)
        {
            incidentSummaryShown = shown ?? new List<IncidentReport>();
            RenderIncidentSummary();
        }

        private void RenderIncidentSummary()
        {
            if (gridIncidents?.Parent == null) return;
            try
            {
                if (incidentSummary == null)
                {
                    incidentSummary = AllInOneSummaryBar.AttachBelow(gridIncidents, "Incidents");
                    gridIncidents.SelectionChanged += (s, e) => RenderIncidentSummary();
                }
                var list = incidentSummaryShown;
                bool Closed(IncidentReport i) => RowMenu.Is(i.Status, "Resolved", "Closed", "Settled", "Cancelled");
                int damaged = list.Sum(i => Math.Max(0, i.DamagedQuantity));
                decimal cost = list.Sum(i => Math.Max(0m, i.EstimatedCost));
                int open = list.Count(i => !Closed(i));
                int resolved = list.Count(Closed);
                int crew = list.Count(i => i.ChargeableToCrew);
                var m = new List<SummaryMetric>
                {
                    new("Reports shown", list.Count.ToString("N0"), SummaryTone.Blue, "incident & damage logs", Strong: true),
                    new("Damaged crates", damaged.ToString("N0"), SummaryTone.Red, "on the reports shown", Strong: damaged > 0),
                    new("Estimated cost", AllInOneSummaryBar.Money(cost), SummaryTone.Amber, "of the damage"),
                    new("Still open", open.ToString("N0"), SummaryTone.Amber, "not resolved yet", Strong: open > 0),
                    new("Resolved", resolved.ToString("N0"), SummaryTone.Green, "closed reports"),
                    new("Chargeable to crew", crew.ToString("N0"), SummaryTone.Purple, "reports"),
                };
                string context = $"Reports shown: {list.Count:N0} (follows your search and filters)";
                string preview = $"{AllInOneSummaryBar.Count(list.Count, "report", "reports")}  ·  {damaged:N0} damaged crates  ·  {AllInOneSummaryBar.Money(cost)}";
                var sel = gridIncidents.SelectedRows.Count == 1 ? gridIncidents.SelectedRows[0].DataBoundItem as IncidentReport : null;
                if (sel != null)
                {
                    m.Add(new SummaryMetric("Selected report", string.IsNullOrWhiteSpace(sel.DispatchNumber) ? $"#{sel.Id}" : sel.DispatchNumber, SummaryTone.Purple, $"{sel.Status} · {Fmt.Date(sel.IncidentDate)}", Strong: true));
                    m.Add(new SummaryMetric("Its damaged crates", sel.DamagedQuantity.ToString("N0"), SummaryTone.Red, sel.Cause));
                    m.Add(new SummaryMetric("Its cost", AllInOneSummaryBar.Money(sel.EstimatedCost), SummaryTone.Amber, sel.ChargeableToCrew ? "chargeable to crew" : "not charged to crew"));
                    context = $"Selected report for {sel.DispatchNumber} — {sel.Cause} · {sel.Status}   ·   Reports shown: {list.Count:N0}";
                }
                incidentSummary.SetMetrics(m, preview, context);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
}
