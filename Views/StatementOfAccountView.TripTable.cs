using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 10: the trips table follows the printed statement (the trip's cells are one tall cell across its store rows) and the
    /// Amount and Fuel surcharge are typed here, once per trip; No. of drops is read-only and is the trip's store count.
    /// </summary>
    public partial class StatementOfAccountView
    {
        private static readonly HashSet<string> MergedCols = new HashSet<string> { "colTripNo", "colDate", "colZone", "colType", "colPlate", "colTons", "colDrops", "colAmount", "colFuel", "colDriver" };
        private static readonly HashSet<string> TripFigureCols = new HashSet<string> { "colDrops", "colAmount", "colFuel" };
        private bool propagatingTripFigure;
        private SoaSheetTotalsPanel? sheetTotals;

        private string TripKeyAt(int i)
        {
            if (i < 0 || i >= gridItems.Rows.Count) return "";
            var r = gridItems.Rows[i];
            return r.IsNewRow ? "" : (r.Cells["colTripNo"].Value?.ToString() ?? "").Trim();
        }

        /// <summary>First and last row of the trip that row <paramref name="i"/> belongs to (the same row twice for a trip of one row).</summary>
        private void TripBounds(int i, out int first, out int last)
        {
            first = last = i;
            string k = TripKeyAt(i);
            if (k.Length == 0) return;
            while (first > 0 && TripKeyAt(first - 1) == k) first--;
            while (last < gridItems.Rows.Count - 1 && TripKeyAt(last + 1) == k) last++;
        }

        private bool IsFirstRowOfTrip(int rowIndex) { TripBounds(rowIndex, out int f, out _); return f == rowIndex; }

        private static decimal CellDecimal(DataGridViewRow r, string col)
            => decimal.TryParse((r.Cells[col].Value?.ToString() ?? "").Replace(",", "").Replace("₱", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;

        /// <summary>True when a trip's Amount and Fuel surcharge come from its service invoice and can't be typed here (same rule as the cell editing).</summary>
        private bool IsMoneyLocked(DataGridViewRow row)
        {
            int d0 = RowDeliveryId(row), s0 = RowSpecialTripId(row);
            if (d0 <= 0 && s0 <= 0) return false;
            try { return !(!IsLegacySoa() && (MJdatabase.GetLiveInvoiceForTrip(d0 > 0 ? d0 : 0, d0 > 0 ? 0 : s0) == null || MJdatabase.IsPreStatementInvoice(d0 > 0 ? d0 : 0, d0 > 0 ? 0 : s0))); }
            catch (Exception ex) { ErrLog.Swallowed(ex); return true; }
        }

        // ------------------------------------------------------------------ the merged trip cells

        private void SetupTripMerge()
        {
            // The client is locked while trips are on the statement and free again when the table is empty.
            gridItems.RowsAdded += (s, e) => UpdateClientLock();
            gridItems.RowsRemoved += (s, e) => UpdateClientLock();
            gridItems.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics == null) return;
                string col = gridItems.Columns[e.ColumnIndex].Name;
                if (!MergedCols.Contains(col) || TripKeyAt(e.RowIndex).Length == 0) return;
                TripBounds(e.RowIndex, out int first, out int last);
                if (first == last)
                {
                    // Nov 17: an empty Amount / Fuel surcharge that can be typed shows the "Type amount" hint.
                    if (col is "colAmount" or "colFuel" && ShowsMoneyHint(first, last, col))
                    {
                        e.PaintBackground(e.CellBounds, true);
                        DrawMoneyHint(e.Graphics, e.CellBounds, e.CellStyle ?? gridItems.DefaultCellStyle, (e.State & DataGridViewElementStates.Selected) != 0);
                        e.Handled = true;
                    }
                    return;   // otherwise a trip of one row paints normally
                }
                var st = e.CellStyle ?? gridItems.DefaultCellStyle;
                bool sel = (e.State & DataGridViewElementStates.Selected) != 0;
                using (var b = new SolidBrush(sel ? st.SelectionBackColor : st.BackColor)) e.Graphics.FillRectangle(b, e.CellBounds);
                using (var pen = new Pen(gridItems.GridColor))
                {
                    var rc = e.CellBounds;
                    e.Graphics.DrawLine(pen, rc.Right - 1, rc.Top, rc.Right - 1, rc.Bottom - 1);
                    if (e.RowIndex == last) e.Graphics.DrawLine(pen, rc.Left, rc.Bottom - 1, rc.Right - 1, rc.Bottom - 1);
                }
                int mid = first + (last - first) / 2;   // the value sits in the middle of the trip's rows
                if (e.RowIndex == mid)
                {
                    string text;
                    if (col is "colAmount" or "colFuel")
                    {
                        decimal sum = 0m;
                        for (int i = first; i <= last; i++) sum += CellDecimal(gridItems.Rows[i], col);
                        text = sum > 0 ? sum.ToString("N2", CultureInfo.InvariantCulture) : "";
                        if (text.Length == 0 && ShowsMoneyHint(first, last, col))   // Nov 17: "Type amount" hint in the empty trip cell
                        {
                            DrawMoneyHint(e.Graphics, e.CellBounds, st, sel);
                            e.Handled = true;
                            return;
                        }
                    }
                    else text = gridItems.Rows[first].Cells[col].Value?.ToString() ?? "";
                    var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix
                        | (st.Alignment == DataGridViewContentAlignment.MiddleRight ? TextFormatFlags.Right : TextFormatFlags.Left);
                    var rect = Rectangle.Inflate(e.CellBounds, -Dpi(4), 0);
                    TextRenderer.DrawText(e.Graphics, text, st.Font ?? gridItems.Font, rect, sel ? st.SelectionForeColor : st.ForeColor, flags);
                }
                e.Handled = true;
            };
            // Drops, Amount and Fuel surcharge are typed once per trip, on the trip's first row: clicking any row of the trip goes there.
            gridItems.CellEnter += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string col = gridItems.Columns[e.ColumnIndex].Name;
                if (!TripFigureCols.Contains(col) || TripKeyAt(e.RowIndex).Length == 0) return;
                TripBounds(e.RowIndex, out int first, out _);
                if (first == e.RowIndex) return;
                BeginInvoke(new Action(() =>
                {
                    try { if (first < gridItems.Rows.Count) gridItems.CurrentCell = gridItems.Rows[first].Cells[col]; }
                    catch (Exception ex) { ErrLog.Swallowed(ex); }
                }));
            };
            // Nov 17: a date typed on a line with no dispatch puts its trip in its place, oldest to latest (only once it reads as a date).
            gridItems.CellEndEdit += (s, e) =>
            {
                if (isBindingForm || e.RowIndex < 0 || e.ColumnIndex < 0 || gridItems.Columns[e.ColumnIndex].Name != "colDate") return;
                if (!Fmt.TryParseDate(gridItems.Rows[e.RowIndex].Cells["colDate"].Value?.ToString(), out _)) return;
                BeginInvoke(new Action(SortTripsByDateStoreReceived));
            };
            // What is typed on the trip's first row is the trip's figure: the other rows of the trip follow (drops) or stay empty (money).
            gridItems.CellValueChanged += (s, e) =>
            {
                if (isBindingForm || propagatingTripFigure || e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string col = gridItems.Columns[e.ColumnIndex].Name;
                if (!TripFigureCols.Contains(col) || TripKeyAt(e.RowIndex).Length == 0) return;
                TripBounds(e.RowIndex, out int first, out int last);
                if (first == last) return;
                propagatingTripFigure = true;
                try
                {
                    var v = gridItems.Rows[e.RowIndex].Cells[col].Value;
                    for (int i = first; i <= last; i++)
                        if (i != e.RowIndex) gridItems.Rows[i].Cells[col].Value = col == "colDrops" ? v : (object)"";
                }
                finally { propagatingTripFigure = false; }
            };
        }

        // ------------------------------------------------------------------ Nov 17: "Date store received" = the dispatch order's date; trips listed oldest to latest

        private bool sortingTrips;

        /// <summary>Nov 17: the dispatch order's date (Delivery.DispatchDate; a special trip's own dispatch date) of the trip a line bills, or null for a line with no dispatch.</summary>
        private static DateTime? DispatchDateOfRow(DataGridViewRow r, Dictionary<int, DateTime?> byDelivery, Dictionary<int, DateTime?> bySpecial)
        {
            try
            {
                int d = RowDeliveryId(r);
                if (d > 0)
                {
                    if (!byDelivery.TryGetValue(d, out var dv)) byDelivery[d] = dv = MJdatabase.GetDeliveryById(d)?.DispatchDate.Date;
                    return dv;
                }
                int s = RowSpecialTripId(r);
                if (s > 0)
                {
                    if (!bySpecial.TryGetValue(s, out var sv)) bySpecial[s] = sv = MJdatabase.GetSpecialTripById(s)?.DispatchDate.Date;
                    return sv;
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return null;
        }

        /// <summary>
        /// Nov 17: every line that bills a dispatch (or special trip) shows the DISPATCH ORDER's date as its "Date store received". Used when a saved statement
        /// is opened (older statements stored the trip report's date): only what is shown changes, nothing is written until the person saves. Lines with no dispatch stay as they are.
        /// </summary>
        private void ApplyDispatchDatesToRows()
        {
            if (gridItems == null || !gridItems.Columns.Contains("colDate")) return;
            var byDelivery = new Dictionary<int, DateTime?>();
            var bySpecial = new Dictionary<int, DateTime?>();
            bool was = isBindingForm;
            isBindingForm = true;
            try
            {
                foreach (DataGridViewRow r in gridItems.Rows)
                {
                    if (r.IsNewRow) continue;
                    var d = DispatchDateOfRow(r, byDelivery, bySpecial);
                    if (!d.HasValue) continue;
                    string want = Fmt.Date(d.Value);
                    if (!string.Equals(r.Cells["colDate"].Value?.ToString() ?? "", want, StringComparison.Ordinal)) r.Cells["colDate"].Value = want;
                }
            }
            finally { isBindingForm = was; }
        }

        /// <summary>
        /// Nov 17: lists the trips oldest to latest by "Date store received". A trip is the rows that share a Trip no.; its rows stay together and in their store
        /// order, the first row keeps the Amount and Fuel surcharge (whole trips only move), and Trip is renumbered 1..n in the new order. Same date: the order they
        /// already had. A trip with no readable date goes last. Only the cell values move between the existing rows, so no money changes.
        /// </summary>
        private void SortTripsByDateStoreReceived()
        {
            if (gridItems == null || sortingTrips || !gridItems.Columns.Contains("colDate") || !gridItems.Columns.Contains("colTripNo")) return;
            sortingTrips = true;
            bool was = isBindingForm;
            try
            {
                try { gridItems.EndEdit(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
                var rows = gridItems.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
                if (rows.Count == 0) return;
                // the trips: runs of consecutive rows with the same (non-blank) Trip no.
                var groups = new List<List<int>>();
                string lastKey = "";
                for (int i = 0; i < rows.Count; i++)
                {
                    string k = (rows[i].Cells["colTripNo"].Value?.ToString() ?? "").Trim();
                    if (k.Length > 0 && groups.Count > 0 && k == lastKey) groups[groups.Count - 1].Add(i);
                    else groups.Add(new List<int> { i });
                    lastKey = k;
                }
                DateTime? DateOf(List<int> g)
                {
                    foreach (int i in g)
                    {
                        string t = rows[i].Cells["colDate"].Value?.ToString() ?? "";
                        if (Fmt.TryParseDate(t, out var d) || DateTime.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return d.Date;
                    }
                    return null;
                }
                var ordered = groups.Select((g, idx) => (g, idx, d: DateOf(g)))
                    .OrderBy(x => x.d.HasValue ? 0 : 1).ThenBy(x => x.d ?? DateTime.MaxValue).ThenBy(x => x.idx).ToList();
                // the new Trip no. of each trip (blank-numbered lines stay blank)
                var newNo = new Dictionary<int, object?>();
                var order = new List<int>();
                int no = 0;
                foreach (var x in ordered)
                {
                    bool numbered = (rows[x.g[0]].Cells["colTripNo"].Value?.ToString() ?? "").Trim().Length > 0;
                    int tripNo = numbered ? ++no : 0;
                    foreach (int i in x.g) { order.Add(i); newNo[i] = numbered ? tripNo : rows[i].Cells["colTripNo"].Value; }
                }
                var cols = gridItems.Columns.Cast<DataGridViewColumn>().Where(c => c is not DataGridViewButtonColumn).Select(c => c.Index).ToList();
                var snapshot = rows.Select(r => cols.Select(ci => r.Cells[ci].Value).ToArray()).ToList();
                int tripCol = gridItems.Columns["colTripNo"]!.Index;
                bool changed = false;
                for (int p = 0; p < rows.Count && !changed; p++)
                {
                    if (order[p] != p) { changed = true; break; }
                    if (!Equals(rows[p].Cells[tripCol].Value?.ToString(), newNo[p]?.ToString())) changed = true;
                }
                if (!changed) return;
                isBindingForm = true;   // writing values must not re-run the totals or the trip-figure propagation
                for (int p = 0; p < rows.Count; p++)
                {
                    int src = order[p];
                    for (int k = 0; k < cols.Count; k++)
                    {
                        int ci = cols[k];
                        object? v = ci == tripCol ? newNo[src] : snapshot[src][k];
                        var cell = rows[p].Cells[ci];
                        if (!Equals(cell.Value, v)) cell.Value = v;
                    }
                }
                gridItems.Invalidate();
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            finally { isBindingForm = was; sortingTrips = false; }
            SyncDropsToStoreCount();
        }

        /// <summary>Nov 17: the hint shown in an empty Amount / Fuel surcharge cell of a trip.</summary>
        private const string MoneyHintText = "Type amount";

        /// <summary>
        /// Nov 17: true when the trip's Amount / Fuel surcharge (column <paramref name="col"/>) is empty and can be typed: the table and the trip's first
        /// cell are not read-only. A trip whose money comes from its service invoice already has an amount, so it shows no hint.
        /// </summary>
        private bool ShowsMoneyHint(int first, int last, string col)
        {
            if (gridItems.ReadOnly || gridItems.Columns[col] is not DataGridViewColumn c || c.ReadOnly || first < 0 || first >= gridItems.Rows.Count) return false;
            if (gridItems.Rows[first].Cells[col].ReadOnly) return false;
            for (int i = first; i <= last && i < gridItems.Rows.Count; i++)
                if (CellDecimal(gridItems.Rows[i], col) > 0 || !string.IsNullOrWhiteSpace(gridItems.Rows[i].Cells[col].Value?.ToString())) return false;
            return true;
        }

        /// <summary>Draws the grey, italic "Type amount" hint, right-aligned like the amounts.</summary>
        private void DrawMoneyHint(Graphics g, Rectangle bounds, DataGridViewCellStyle st, bool selected)
        {
            var baseFont = st.Font ?? gridItems.Font;
            var rect = Rectangle.Inflate(bounds, -Dpi(4), 0);
            using var f = new Font(baseFont, FontStyle.Italic);
            TextRenderer.DrawText(g, MoneyHintText, f, rect, selected ? st.SelectionForeColor : AppTheme.TextMuted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        /// <summary>
        /// Nov 14: "No. of drops" is the number of stores of the trip (the rows that carry a store name or DR no.), the same on every row of the trip.
        /// It is never typed; it is refreshed whenever the table changes and before the statement is saved or printed.
        /// </summary>
        private void SyncDropsToStoreCount()
        {
            if (gridItems == null || !gridItems.Columns.Contains("colDrops")) return;
            int i = 0;
            bool was = isBindingForm;
            isBindingForm = true;
            try
            {
                while (i < gridItems.Rows.Count)
                {
                    if (TripKeyAt(i).Length == 0) { i++; continue; }
                    TripBounds(i, out int first, out int last);
                    int stores = 0;
                    for (int k = first; k <= last; k++)
                    {
                        var r = gridItems.Rows[k];
                        if (!string.IsNullOrWhiteSpace(r.Cells["colDest"].Value?.ToString()) || !string.IsNullOrWhiteSpace(r.Cells["colDr"].Value?.ToString())) stores++;
                    }
                    if (stores == 0) stores = 1;
                    for (int k = first; k <= last; k++)
                        if (!int.TryParse(gridItems.Rows[k].Cells["colDrops"].Value?.ToString(), out int cur) || cur != stores) gridItems.Rows[k].Cells["colDrops"].Value = stores;
                    i = last + 1;
                }
            }
            finally { isBindingForm = was; }
        }

        /// <summary>Once trips are on the statement the client can't be changed (the trips, amounts and TIN belong to it); Clear form frees it again.</summary>
        private void UpdateClientLock()
        {
            if (cbClient == null || gridItems == null) return;
            bool hasTrips = gridItems.Rows.Cast<DataGridViewRow>().Any(r => !r.IsNewRow);
            if (!hasTrips || cbClient.Enabled) cbClient.Enabled = !hasTrips;
        }
    }
}
