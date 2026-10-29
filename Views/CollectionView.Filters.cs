using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>Date and sort chips for the payments list.</summary>
    public partial class CollectionView
    {
        private DateFilterChip? dateChip;
        private ChoiceChip? sortChip;
        private static readonly string[] ReceiptSorts = { "Newest first", "Oldest first", "Highest amount", "Receipt # (A–Z)" };
        private void InitListChips()
        {
            dateChip = new DateFilterChip(DatePresets.History, "All dates") { Margin = new Padding(6, 4, 0, 0) };
            sortChip = new ChoiceChip("Sort", ReceiptSorts, "⇅") { Margin = new Padding(6, 4, 0, 0) };
            dateChip.RangeChanged += (s, e) => FilterGridData();
            sortChip.SelectionChanged += (s, e) => FilterGridData();
            var tips = new ToolTip();
            tips.SetToolTip(dateChip, "Show payments received on a day, week, month or dates you pick. ‹ › step back or forward.");
            tips.SetToolTip(sortChip, "Order of the list");
            FlowFit.Grow(pnlGridActionsFlow);
            int at = pnlGridActionsFlow.Controls.IndexOf(btnClearSearch);
            pnlGridActionsFlow.Controls.Add(dateChip);
            pnlGridActionsFlow.Controls.Add(sortChip);
            if (at >= 0) { pnlGridActionsFlow.Controls.SetChildIndex(dateChip, at + 1); pnlGridActionsFlow.Controls.SetChildIndex(sortChip, at + 2); }
        }
        private List<CollectionReceipt> ApplyListChips(IEnumerable<CollectionReceipt> rows)
        {
            var r = dateChip?.Range ?? DateRange.All;
            if (!r.IsAll) rows = rows.Where(x => r.Contains(x.PaymentDate));
            rows = (sortChip?.SelectedIndex ?? 0) switch
            {
                1 => rows.OrderBy(x => x.PaymentDate).ThenBy(x => x.Id),
                2 => rows.OrderByDescending(x => x.AmountPaid),
                3 => rows.OrderBy(x => x.ReceiptNumber),
                _ => rows.OrderByDescending(x => x.PaymentDate).ThenByDescending(x => x.Id)
            };
            return rows.ToList();
        }
    }
}
