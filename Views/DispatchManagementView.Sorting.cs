using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>Sort and status pickers for the dispatch list. Newest first by default.</summary>
    public partial class DispatchManagementView
    {
        private ComboBox? cbSortBy, cbStatusPick;
        private bool syncingPickers;
        private static readonly (string Label, string Column, bool Asc)[] SortOptions =
        {
            ("Newest first", "Date", false),
            ("Oldest first", "Date", true),
            ("Status · active first", "Status", true),
            ("Status · finished first", "Status", false),
            ("Dispatch # (A–Z)", "DispatchNumber", true),
            ("Most crates first", "TotalCrates", false)
        };
        private static readonly (string Label, string Filter)[] StatusOptions =
        {
            ("All statuses", ""),
            ("Awaiting completion", AwaitingCompletionFilter),
            ("Scheduled", "Scheduled"),
            ("In Transit", "In Transit"),
            ("Delayed", "Delayed"),
            ("Completed", "Completed"),
            ("Cancelled", "Cancelled")
        };
        private void InitSortAndStatusPickers()
        {
            currentSortColumn = "Date";
            isSortAscending = false;
            int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            ComboBox Make(IEnumerable<string> items, int width)
            {
                var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = D(width), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F), Margin = new Padding(D(6), D(4), 0, 0) };
                cb.Items.AddRange(items.Cast<object>().ToArray());
                cb.SelectedIndex = 0;
                return cb;
            }
            cbStatusPick = Make(StatusOptions.Select(s => s.Label), 160);
            cbSortBy = Make(SortOptions.Select(s => "Sort: " + s.Label), 200);
            cbStatusPick.SelectedIndexChanged += (s, e) =>
            {
                if (syncingPickers) return;
                currentStatusFilter = StatusOptions[Math.Max(0, cbStatusPick.SelectedIndex)].Filter;
                FilterGridData();
                UpdateSummaryButtonStyles();
            };
            cbSortBy.SelectedIndexChanged += (s, e) =>
            {
                if (syncingPickers) return;
                var opt = SortOptions[Math.Max(0, cbSortBy.SelectedIndex)];
                currentSortColumn = opt.Column;
                isSortAscending = opt.Asc;
                FilterGridData();
            };
            var tips = new ToolTip();
            tips.SetToolTip(cbStatusPick, "Show only orders with this status");
            tips.SetToolTip(cbSortBy, "Order of the list (you can also click a column header)");
            int at = pnlGridActionsFlow.Controls.IndexOf(btnClearSearch);
            pnlGridActionsFlow.Controls.Add(cbStatusPick);
            pnlGridActionsFlow.Controls.Add(cbSortBy);
            if (at >= 0)
            {
                pnlGridActionsFlow.Controls.SetChildIndex(cbStatusPick, at + 1);
                pnlGridActionsFlow.Controls.SetChildIndex(cbSortBy, at + 2);
            }
        }
        /// <summary>Header clicks: date and crates start newest/largest first; others A–Z.</summary>
        private void SortByHeader(string dataProperty)
        {
            string col = dataProperty switch
            {
                "DispatchDate" or "DepartureTime" => "Date",
                "Status" => "Status",
                "TotalCrates" => "TotalCrates",
                _ => dataProperty
            };
            if (currentSortColumn == col) isSortAscending = !isSortAscending;
            else { currentSortColumn = col; isSortAscending = !(col == "Date" || col == "TotalCrates"); }
            FilterGridData();
        }
        private static DateTime DepartureOf(Delivery d)
        {
            if (DateTime.TryParse(d.DepartureTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) || DateTime.TryParse(d.DepartureTime, out t))
                return d.DispatchDate.Date + t.TimeOfDay;
            return d.DispatchDate.Date;
        }
        private IEnumerable<Delivery> ApplySort(IEnumerable<Delivery> rows)
        {
            switch (currentSortColumn)
            {
                case "Status":
                    var byStatus = isSortAscending
                        ? rows.OrderBy(d => MJdatabase.IsAwaitingCompletion(d) ? -1 : GetDispatchStatusSortWeight(d.Status))
                        : rows.OrderByDescending(d => MJdatabase.IsAwaitingCompletion(d) ? -1 : GetDispatchStatusSortWeight(d.Status));
                    return byStatus.ThenByDescending(DepartureOf).ThenByDescending(d => d.Id);
                case "DispatchNumber":
                    return isSortAscending ? rows.OrderBy(d => d.DispatchNumber) : rows.OrderByDescending(d => d.DispatchNumber);
                case "TotalCrates":
                    return (isSortAscending ? rows.OrderBy(d => d.TotalCrates) : rows.OrderByDescending(d => d.TotalCrates)).ThenByDescending(DepartureOf);
                case "Date":
                case "DispatchDate":
                    return isSortAscending ? rows.OrderBy(DepartureOf).ThenBy(d => d.Id) : rows.OrderByDescending(DepartureOf).ThenByDescending(d => d.Id);
                default:
                    var prop = typeof(Delivery).GetProperty(currentSortColumn);
                    if (prop == null) return rows.OrderByDescending(DepartureOf);
                    return isSortAscending ? rows.OrderBy(d => Convert.ToString(prop.GetValue(d), CultureInfo.CurrentCulture)) : rows.OrderByDescending(d => Convert.ToString(prop.GetValue(d), CultureInfo.CurrentCulture));
            }
        }
        /// <summary>Keeps the two pickers showing what the list is actually doing (pills and header clicks change it too).</summary>
        private void SyncPickers()
        {
            if (cbSortBy == null || cbStatusPick == null) return;
            syncingPickers = true;
            try
            {
                int si = Array.FindIndex(SortOptions, o => o.Column == currentSortColumn && o.Asc == isSortAscending);
                if (si >= 0) cbSortBy.SelectedIndex = si;
                string f = currentStatusFilter == "Pending" ? "Scheduled" : currentStatusFilter;
                int fi = Array.FindIndex(StatusOptions, o => o.Filter == f);
                cbStatusPick.SelectedIndex = fi >= 0 ? fi : 0;
            }
            finally { syncingPickers = false; }
        }
    }
}
