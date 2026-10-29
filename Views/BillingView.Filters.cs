using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>Date and sort chips for the invoice list.</summary>
    public partial class BillingView
    {
        private DateFilterChip? dateChip;
        private ChoiceChip? sortChip;
        private static readonly string[] InvoiceSorts = { "Newest first", "Oldest first", "Due soonest", "Unpaid first", "Highest amount", "Client A–Z" };
        private void InitListChips()
        {
            dateChip = new DateFilterChip(DatePresets.History, "All dates") { Margin = new Padding(6, 4, 0, 0) };
            sortChip = new ChoiceChip("Sort", InvoiceSorts, "⇅") { Margin = new Padding(6, 4, 0, 0) };
            dateChip.RangeChanged += (s, e) => FilterGridData();
            sortChip.SelectionChanged += (s, e) => FilterGridData();
            var tips = new ToolTip();
            tips.SetToolTip(dateChip, "Show invoices issued on a day, week, month or dates you pick. ‹ › step back or forward.");
            tips.SetToolTip(sortChip, "Order of the list");
            FlowFit.Grow(pnlGridActionsFlow);
            int at = pnlGridActionsFlow.Controls.IndexOf(btnClearSearch);
            pnlGridActionsFlow.Controls.Add(dateChip);
            pnlGridActionsFlow.Controls.Add(sortChip);
            if (at >= 0) { pnlGridActionsFlow.Controls.SetChildIndex(dateChip, at + 1); pnlGridActionsFlow.Controls.SetChildIndex(sortChip, at + 2); }
        }
        private List<Invoice> ApplyListChips(IEnumerable<Invoice> rows)
        {
            var r = dateChip?.Range ?? DateRange.All;
            if (!r.IsAll) rows = rows.Where(i => r.Contains(i.IssueDate));
            rows = (sortChip?.SelectedIndex ?? 0) switch
            {
                1 => rows.OrderBy(i => i.IssueDate).ThenBy(i => i.Id),
                2 => rows.OrderBy(i => i.DueDate),
                3 => rows.OrderBy(i => RowMenu.Is(i.Status, "Paid") ? 1 : 0).ThenBy(i => i.DueDate),
                4 => rows.OrderByDescending(i => i.NetAmount),
                5 => rows.OrderBy(i => i.ClientName).ThenByDescending(i => i.IssueDate),
                _ => rows.OrderByDescending(i => i.IssueDate).ThenByDescending(i => i.Id)
            };
            return rows.ToList();
        }
    }
}
