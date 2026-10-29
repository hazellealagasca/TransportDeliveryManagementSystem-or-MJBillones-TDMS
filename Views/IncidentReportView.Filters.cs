using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>Date and sort chips for the incident list.</summary>
    public partial class IncidentReportView
    {
        private DateFilterChip? dateChip;
        private ChoiceChip? sortChip;
        private static readonly (string Label, string Column, bool Asc)[] IncidentSorts =
        {
            ("Newest first", "IncidentDate", false), ("Oldest first", "IncidentDate", true),
            ("Highest cost", "EstimatedCost", false), ("Most crates damaged", "DamagedQuantity", false), ("Status", "Status", true)
        };
        private void InitListChips()
        {
            currentSortColumn = "IncidentDate"; isSortAscending = false;
            dateChip = new DateFilterChip(DatePresets.History, "All dates") { Margin = new Padding(6, 4, 0, 0) };
            sortChip = new ChoiceChip("Sort", IncidentSorts.Select(o => o.Label), "⇅") { Margin = new Padding(6, 4, 0, 0) };
            dateChip.RangeChanged += (s, e) => FilterGridData();
            sortChip.SelectionChanged += (s, e) =>
            {
                var o = IncidentSorts[sortChip.SelectedIndex];
                currentSortColumn = o.Column; isSortAscending = o.Asc;
                FilterGridData();
            };
            var tips = new ToolTip();
            tips.SetToolTip(dateChip, "Show incidents on a day, week, month or dates you pick. ‹ › step back or forward.");
            tips.SetToolTip(sortChip, "Order of the list (you can also click a column header)");
            FlowFit.Grow(pnlGridActionsFlow);
            int at = pnlGridActionsFlow.Controls.IndexOf(btnClearSearch);
            pnlGridActionsFlow.Controls.Add(dateChip);
            pnlGridActionsFlow.Controls.Add(sortChip);
            if (at >= 0) { pnlGridActionsFlow.Controls.SetChildIndex(dateChip, at + 1); pnlGridActionsFlow.Controls.SetChildIndex(sortChip, at + 2); }
        }
        private List<IncidentReport> ApplyDateChip(List<IncidentReport> rows)
        {
            var r = dateChip?.Range ?? DateRange.All;
            int si = Array.FindIndex(IncidentSorts, o => o.Column == currentSortColumn && o.Asc == isSortAscending);
            sortChip?.SetQuietly(si >= 0 ? si : 0);
            return r.IsAll ? rows : rows.Where(x => r.Contains(x.IncidentDate)).ToList();
        }
    }
}
