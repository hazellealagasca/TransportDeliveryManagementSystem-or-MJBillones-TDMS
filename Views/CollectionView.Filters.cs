using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>Date, client, method and sort chips for the receipts list (Nov 25: the list shows the issued receipts, read-only), plus click-a-header sorting.</summary>
    public partial class CollectionView
    {
        private DateFilterChip? dateChip;
        private ChoiceChip? clientChip;
        private ChoiceChip? methodChip;
        private ChoiceChip? sortChip;
        private readonly ToolTip chipTips = new ToolTip();
        private static readonly string[] ReceiptSorts = { "Newest first", "Oldest first", "Highest amount", "Lowest amount", "Client (A–Z)", "Receipt # (A–Z)", "Invoice # (A–Z)" };
        private static readonly string[] ReceiptMethods = { "All methods", "Check", "Cash", "Bank Transfer", "GCash" };
        private void InitListChips()
        {
            dateChip = new DateFilterChip(DatePresets.History, "All dates") { Margin = new Padding(6, 4, 0, 0) };
            methodChip = new ChoiceChip("", ReceiptMethods, "💳") { Margin = new Padding(6, 4, 0, 0) };
            sortChip = new ChoiceChip("Sort", ReceiptSorts, "⇅") { Margin = new Padding(6, 4, 0, 0) };
            dateChip.RangeChanged += (s, e) => FilterGridData();
            methodChip.SelectionChanged += (s, e) => FilterGridData();
            sortChip.SelectionChanged += (s, e) => { currentSortColumn = ""; isSortAscending = true; FilterGridData(); }; // Nov 25: picking a Sort option clears the header sort
            chipTips.SetToolTip(dateChip, "Show payments received on a day, week, month or dates you pick. ‹ › step back or forward.");
            chipTips.SetToolTip(methodChip, "Show receipts paid by one method");
            chipTips.SetToolTip(sortChip, "Order of the list (you can also click a column heading)");
            FlowFit.Grow(pnlGridActionsFlow);
            int at = pnlGridActionsFlow.Controls.IndexOf(btnClearSearch);
            pnlGridActionsFlow.Controls.Add(dateChip);
            pnlGridActionsFlow.Controls.Add(methodChip);
            pnlGridActionsFlow.Controls.Add(sortChip);
            if (at >= 0) { pnlGridActionsFlow.Controls.SetChildIndex(dateChip, at + 1); pnlGridActionsFlow.Controls.SetChildIndex(methodChip, at + 2); pnlGridActionsFlow.Controls.SetChildIndex(sortChip, at + 3); }
            RebuildClientChip();
        }

        /// <summary>Nov 25: the client chip lists "All clients" and the clients on the receipts. A ChoiceChip can't change its choices, so it is made again on each load (the picked client stays when it is still there).</summary>
        private void RebuildClientChip()
        {
            if (methodChip == null) return;
            string keep = clientChip != null && clientChip.SelectedIndex > 0 ? clientChip.SelectedText : "";
            var names = allReceipts.Select(r => (r.ClientName ?? "").Trim()).Where(n => n.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            var options = new List<string> { "All clients" }; options.AddRange(names);
            var chip = new ChoiceChip("", options, "👤") { Margin = new Padding(6, 4, 0, 0) };
            int idx = keep.Length == 0 ? 0 : options.FindIndex(o => string.Equals(o, keep, StringComparison.OrdinalIgnoreCase));
            chip.SetQuietly(Math.Max(0, idx));
            chip.SelectionChanged += (s, e) => FilterGridData();
            chipTips.SetToolTip(chip, "Show the receipts of one client");
            int pos = pnlGridActionsFlow.Controls.IndexOf(methodChip);
            var old = clientChip;
            pnlGridActionsFlow.Controls.Add(chip);
            if (pos >= 0) pnlGridActionsFlow.Controls.SetChildIndex(chip, pos);
            clientChip = chip;
            if (old != null) { pnlGridActionsFlow.Controls.Remove(old); old.Dispose(); }
        }

        /// <summary>Nov 25: the client of each receipt: its statement's client, or the invoice's client (display only; not stored).</summary>
        private static void FillReceiptClients(List<CollectionReceipt> rows)
        {
            Dictionary<int, string> soaClients, invClients;
            try { soaClients = MJdatabase.GetStatementsOfAccount().GroupBy(s => s.Id).ToDictionary(g => g.Key, g => g.First().ClientName ?? ""); }
            catch (Exception ex) { ErrLog.Swallowed(ex); soaClients = new Dictionary<int, string>(); }
            try { invClients = MJdatabase.GetInvoices().GroupBy(i => i.Id).ToDictionary(g => g.Key, g => g.First().ClientName ?? ""); }
            catch (Exception ex) { ErrLog.Swallowed(ex); invClients = new Dictionary<int, string>(); }
            foreach (var r in rows)
            {
                string? name = null;
                if (r.SoaId > 0) soaClients.TryGetValue(r.SoaId, out name);
                if (string.IsNullOrWhiteSpace(name)) invClients.TryGetValue(r.InvoiceId, out name);
                r.ClientName = name ?? "";
            }
        }

        private List<CollectionReceipt> ApplyListChips(IEnumerable<CollectionReceipt> rows)
        {
            var r = dateChip?.Range ?? DateRange.All;
            if (!r.IsAll) rows = rows.Where(x => r.Contains(x.PaymentDate));
            if (clientChip != null && clientChip.SelectedIndex > 0) { string c = clientChip.SelectedText; rows = rows.Where(x => string.Equals((x.ClientName ?? "").Trim(), c, StringComparison.OrdinalIgnoreCase)); }
            if (methodChip != null && methodChip.SelectedIndex > 0) { string m = methodChip.SelectedText; rows = rows.Where(x => string.Equals((x.PaymentMethod ?? "").Trim(), m, StringComparison.OrdinalIgnoreCase)); }
            var cmp = StringComparer.OrdinalIgnoreCase;
            if (!string.IsNullOrEmpty(currentSortColumn))
            {
                // Nov 25: a clicked column heading decides the order (click again to flip it)
                bool asc = isSortAscending;
                var src = rows.ToList();
                IOrderedEnumerable<CollectionReceipt> By<T>(Func<CollectionReceipt, T> key, IComparer<T> c) =>
                    asc ? src.OrderBy(key, c).ThenBy(x => x.Id) : src.OrderByDescending(key, c).ThenByDescending(x => x.Id);
                var ordered = currentSortColumn switch
                {
                    "ReceiptNumber" => By(x => x.ReceiptNumber ?? "", cmp),
                    "InvoiceNumber" => By(x => x.InvoiceNumber ?? "", cmp),
                    "ClientName" => By(x => x.ClientName ?? "", cmp),
                    "PaymentDate" => By(x => x.PaymentDate, Comparer<DateTime>.Default),
                    "AmountPaid" => By(x => x.AmountPaid, Comparer<decimal>.Default),
                    "PaymentMethod" => By(x => x.PaymentMethod ?? "", cmp),
                    "ReferenceNumber" => By(x => x.ReferenceNumber ?? "", cmp),
                    "Notes" => By(x => x.Notes ?? "", cmp),
                    _ => By(x => x.Id, Comparer<int>.Default)
                };
                return ordered.ToList();
            }
            rows = (sortChip?.SelectedIndex ?? 0) switch
            {
                1 => rows.OrderBy(x => x.PaymentDate).ThenBy(x => x.Id),
                2 => rows.OrderByDescending(x => x.AmountPaid).ThenByDescending(x => x.Id),
                3 => rows.OrderBy(x => x.AmountPaid).ThenBy(x => x.Id),
                4 => rows.OrderBy(x => x.ClientName ?? "", cmp).ThenByDescending(x => x.PaymentDate).ThenByDescending(x => x.Id),
                5 => rows.OrderBy(x => x.ReceiptNumber ?? "", cmp),
                6 => rows.OrderBy(x => x.InvoiceNumber ?? "", cmp).ThenBy(x => x.ReceiptNumber ?? "", cmp),
                _ => rows.OrderByDescending(x => x.PaymentDate).ThenByDescending(x => x.Id) // Newest first: latest to oldest
            };
            return rows.ToList();
        }

        /// <summary>Shows the sort arrow on the clicked column heading (none when the Sort chip decides the order).</summary>
        private void ShowHeaderSortGlyph()
        {
            foreach (DataGridViewColumn col in gridCollections.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.Programmatic;
                col.HeaderCell.SortGlyphDirection = !string.IsNullOrEmpty(currentSortColumn) && col.DataPropertyName == currentSortColumn
                    ? (isSortAscending ? SortOrder.Ascending : SortOrder.Descending) : SortOrder.None;
            }
        }
    }
}
