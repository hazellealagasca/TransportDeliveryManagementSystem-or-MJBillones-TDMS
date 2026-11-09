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
    /// "Trip payments" window sets the number of drops, Amount and Fuel surcharge of each trip in one place, with the statement's
    /// computation beside it. The window writes into the same table cells, so Save, the checks and the print are unchanged.
    /// </summary>
    public partial class StatementOfAccountView
    {
        private static readonly HashSet<string> MergedCols = new HashSet<string> { "colTripNo", "colDate", "colZone", "colType", "colPlate", "colTons", "colDrops", "colAmount", "colFuel", "colDriver" };
        private static readonly HashSet<string> TripFigureCols = new HashSet<string> { "colDrops", "colAmount", "colFuel" };
        private bool propagatingTripFigure, tripPaymentsOpen;
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
            gridItems.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics == null) return;
                string col = gridItems.Columns[e.ColumnIndex].Name;
                if (!MergedCols.Contains(col) || TripKeyAt(e.RowIndex).Length == 0) return;
                TripBounds(e.RowIndex, out int first, out int last);
                if (first == last) return;   // a trip of one row paints normally
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

        // ------------------------------------------------------------------ the Trip payments window

        private sealed class TripPayLine
        {
            public int First, Last;
            public string TripNo = "", TripReport = "", Date = "", Stores = "";
            public bool Locked;
        }

        private List<TripPayLine> CollectTripPayLines()
        {
            var lines = new List<TripPayLine>();
            int i = 0;
            while (i < gridItems.Rows.Count)
            {
                if (TripKeyAt(i).Length == 0) { i++; continue; }
                TripBounds(i, out int first, out int last);
                var r0 = gridItems.Rows[first];
                lines.Add(new TripPayLine
                {
                    First = first, Last = last,
                    TripNo = TripKeyAt(first),
                    TripReport = (r0.Cells["colTripReport"].Value?.ToString() ?? "").Trim(),
                    Date = (r0.Cells["colDate"].Value?.ToString() ?? "").Trim(),
                    Stores = string.Join(", ", Enumerable.Range(first, last - first + 1).Select(k => (gridItems.Rows[k].Cells["colDest"].Value?.ToString() ?? "").Trim()).Where(x => x.Length > 0)),
                    Locked = IsMoneyLocked(r0)
                });
                i = last + 1;
            }
            return lines;
        }

        /// <summary>Opens the Trip payments window: one row per trip of the statement; Apply writes the figures into the table.</summary>
        private void ShowTripPayments(bool auto)
        {
            if (tripPaymentsOpen || gridItems == null) return;
            gridItems.EndEdit();
            var lines = CollectTripPayLines();
            if (lines.Count == 0)
            {
                if (!auto) QuietBox.Show("Add trip reports to the statement first (tick them in Ready service invoices).", "Trip payments", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            tripPaymentsOpen = true;
            try
            {
                using var f = new Form
                {
                    Text = "Trip payments",
                    StartPosition = FormStartPosition.CenterParent,
                    ShowInTaskbar = false,
                    MinimizeBox = false,
                    ClientSize = new Size(Dpi(980), Dpi(600)),
                    MinimumSize = new Size(Dpi(860), Dpi(520)),
                    BackColor = AppTheme.Surface,
                    Font = new Font("Segoe UI", 9.5F)
                };
                try { ThemeManager.StyleWindowChrome(f); } catch (Exception swx) { ErrLog.Swallowed(swx); }
                string client = cbClient.Text.Trim(), soaType = cbSoaType.Text.Trim();
                var head = new Label
                {
                    Dock = DockStyle.Top, Height = Dpi(64), Padding = new Padding(Dpi(16), Dpi(10), Dpi(16), 0),
                    Font = new Font("Segoe UI Semibold", 11F), ForeColor = AppTheme.TextPrimary,
                    Text = $"Trip payments  ·  {lines.Count} trip report{(lines.Count == 1 ? "" : "s")}\r\n{(client.Length > 0 ? client : "(no client)")}  ·  {(soaType.Length > 0 ? soaType : "SOA type not chosen")}  ·  type only the drops, Amount and Fuel surcharge"
                };
                var grid = new DataGridView
                {
                    Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                    AutoGenerateColumns = false, BackgroundColor = AppTheme.Surface, BorderStyle = BorderStyle.None,
                    SelectionMode = DataGridViewSelectionMode.CellSelect, MultiSelect = false, EditMode = DataGridViewEditMode.EditOnEnter,
                    ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing, ColumnHeadersHeight = Dpi(34),
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, EnableHeadersVisualStyles = false
                };
                grid.RowTemplate.Height = Dpi(34);
                grid.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(219, 234, 254);
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(30, 58, 138);
                grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.75F);
                var ro = new DataGridViewCellStyle { BackColor = Pal.C(248, 250, 252) };
                var edit = new DataGridViewCellStyle { BackColor = Pal.C(254, 252, 232), Alignment = DataGridViewContentAlignment.MiddleRight };
                var money = new DataGridViewCellStyle(edit) { Format = "N2" };
                DataGridViewTextBoxColumn C(string name, string header, int w, bool readOnly, DataGridViewCellStyle st, int min = 60)
                    => new DataGridViewTextBoxColumn { Name = name, HeaderText = header, FillWeight = w, MinimumWidth = Dpi(min), ReadOnly = readOnly, DefaultCellStyle = new DataGridViewCellStyle(st), SortMode = DataGridViewColumnSortMode.NotSortable };
                grid.Columns.Add(C("pTrip", "Trip", 36, true, ro, 40));
                grid.Columns.Add(C("pReport", "Trip report #", 110, true, ro, 100));
                grid.Columns.Add(C("pDate", "Date", 80, true, ro, 80));
                grid.Columns.Add(C("pStores", "Stores", 200, true, ro, 140));
                grid.Columns.Add(C("pDrops", "No. of drops", 60, false, edit, 70));
                grid.Columns.Add(C("pAmount", "Amount (VAT incl.)", 90, false, money, 100));
                grid.Columns.Add(C("pFuel", "Fuel surcharge", 80, false, money, 90));
                grid.Columns.Add(C("pTotal", "Row total", 80, true, new DataGridViewCellStyle(ro) { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }, 90));
                foreach (var ln in lines)
                {
                    var r0 = gridItems.Rows[ln.First];
                    decimal amt = 0m, fuel = 0m;
                    for (int k = ln.First; k <= ln.Last; k++) { amt += CellDecimal(gridItems.Rows[k], "colAmount"); fuel += CellDecimal(gridItems.Rows[k], "colFuel"); }
                    int idx = grid.Rows.Add(ln.TripNo, ln.TripReport, ln.Date, ln.Stores, (r0.Cells["colDrops"].Value?.ToString() ?? "").Trim(), amt > 0 ? amt : (object)"", fuel > 0 ? fuel : (object)"", amt + fuel);
                    grid.Rows[idx].Tag = ln;
                    if (ln.Locked)
                    {
                        foreach (var cn in new[] { "pAmount", "pFuel" }) { grid.Rows[idx].Cells[cn].ReadOnly = true; grid.Rows[idx].Cells[cn].Style.BackColor = Pal.C(248, 250, 252); }
                        grid.Rows[idx].Cells["pStores"].ToolTipText = "This trip's Amount and Fuel surcharge come from its service invoice.";
                    }
                }
                // computation panel
                var comp = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = Dpi(190), ColumnCount = 2, Padding = new Padding(Dpi(16), Dpi(8), Dpi(16), Dpi(4)), BackColor = AppTheme.Canvas };
                comp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                comp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi(140)));
                var valLabels = new List<Label>();
                string[] names = { "Total Sales (VAT Inclusive)", "Less: VAT (12%)", "Amount: Net of VAT", "Fuel surcharge (added, no VAT)", "Less: Withholding Tax (2%)", "TOTAL AMOUNT DUE" };
                for (int i = 0; i < names.Length; i++)
                {
                    bool last = i == names.Length - 1;
                    var fnt = new Font(last ? "Segoe UI Semibold" : "Segoe UI", last ? 11F : 9.5F);
                    comp.Controls.Add(new Label { Text = names[i], Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = fnt, ForeColor = last ? AppTheme.AccentText : AppTheme.TextSecondary }, 0, i);
                    var v = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = fnt, ForeColor = last ? AppTheme.AccentText : AppTheme.TextPrimary };
                    valLabels.Add(v);
                    comp.Controls.Add(v, 1, i);
                }
                var note = new Label { Dock = DockStyle.Bottom, Height = Dpi(26), Padding = new Padding(Dpi(16), 0, Dpi(16), 0), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.WarningText };
                var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = Dpi(54), FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(Dpi(12), Dpi(8), Dpi(12), Dpi(8)), BackColor = AppTheme.Surface };
                var ok = new Button { Text = "Apply to statement", Size = new Size(Dpi(170), Dpi(38)), DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat };
                var cancel = new Button { Text = "Cancel", Size = new Size(Dpi(100), Dpi(38)), DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
                UIThemeHelper.SetButtonRole(ok, "primary");
                UIThemeHelper.SetButtonRole(cancel, "secondary");
                buttons.Controls.Add(ok);
                buttons.Controls.Add(cancel);
                f.AcceptButton = ok; f.CancelButton = cancel;

                bool refreshing = false;
                void Recompute()
                {
                    if (refreshing) return;
                    refreshing = true;
                    try
                    {
                        decimal a = 0m, fu = 0m; int missing = 0;
                        foreach (DataGridViewRow r in grid.Rows)
                        {
                            decimal ra = CellDecimal(r, "pAmount"), rf = CellDecimal(r, "pFuel");
                            a += ra; fu += rf;
                            if (ra <= 0) missing++;
                            r.Cells["pTotal"].Value = ra + rf;
                        }
                        var t = SoaTotals.From(a, fu);
                        decimal[] vals = { t.TotalSales, t.Vat, t.NetOfVat, t.Fuel, t.Withholding, t.Due };
                        for (int i = 0; i < vals.Length; i++) valLabels[i].Text = "₱" + vals[i].ToString("N2", CultureInfo.InvariantCulture);
                        note.Text = missing > 0 ? $"⚠ {missing} trip{(missing == 1 ? "" : "s")} without an amount yet. The statement can't be saved until every trip has one." : "";
                    }
                    finally { refreshing = false; }
                }
                grid.CellValueChanged += (s, e) => { if (e.RowIndex >= 0) Recompute(); };
                grid.EditingControlShowing += (s, e) =>
                {
                    if (e.Control is TextBox tb)
                    {
                        tb.TextChanged -= LivePayCommit; tb.TextChanged += LivePayCommit;
                        void LivePayCommit(object? o, EventArgs a) { try { if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); } catch (Exception ex) { ErrLog.Swallowed(ex); } }
                    }
                };
                grid.CellValidating += (s, e) =>
                {
                    if (e.RowIndex < 0) return;
                    string cn = grid.Columns[e.ColumnIndex].Name;
                    if (cn is not ("pDrops" or "pAmount" or "pFuel")) return;
                    string txt = (e.FormattedValue?.ToString() ?? "").Replace(",", "").Replace("₱", "").Trim();
                    if (txt.Length == 0) return;
                    bool good = cn == "pDrops" ? int.TryParse(txt, out int n) && n >= 0 : decimal.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) && d >= 0;
                    if (!good) { grid.Rows[e.RowIndex].ErrorText = cn == "pDrops" ? "Drops must be a whole number of 0 or more." : "Enter an amount of 0 or more, e.g. 8500 or 8500.50"; e.Cancel = true; }
                    else grid.Rows[e.RowIndex].ErrorText = "";
                };
                grid.CellEndEdit += (s, e) => { if (e.RowIndex >= 0) grid.Rows[e.RowIndex].ErrorText = ""; };
                f.Controls.Add(grid);
                f.Controls.Add(head);
                f.Controls.Add(note);
                f.Controls.Add(comp);
                f.Controls.Add(buttons);
                grid.BringToFront();
                Recompute();
                if (f.ShowDialog(FindForm()) != DialogResult.OK) return;
                grid.EndEdit();

                bool was = isBindingForm;
                isBindingForm = true;
                try
                {
                    foreach (DataGridViewRow r in grid.Rows)
                    {
                        if (r.Tag is not TripPayLine ln) continue;
                        string drops = (r.Cells["pDrops"].Value?.ToString() ?? "").Trim();
                        for (int k = ln.First; k <= ln.Last; k++) gridItems.Rows[k].Cells["colDrops"].Value = drops.Length > 0 && int.TryParse(drops.Replace(",", ""), out int dn) ? dn : (object)drops;
                        if (ln.Locked) continue;
                        decimal amt = CellDecimal(r, "pAmount"), fuel = CellDecimal(r, "pFuel");
                        for (int k = ln.First; k <= ln.Last; k++)
                        {
                            gridItems.Rows[k].Cells["colAmount"].Value = k == ln.First && amt > 0 ? amt : (object)"";
                            gridItems.Rows[k].Cells["colFuel"].Value = k == ln.First && fuel > 0 ? fuel : (object)"";
                        }
                    }
                }
                finally { isBindingForm = was; }
                RecalculateTaxes(manualOverride: false);
                SizeItemsGrid();
                gridItems.Invalidate();
                UpdateSoaGuide();
            }
            finally { tripPaymentsOpen = false; }
        }

        /// <summary>Opens the payment window by itself after trips were added, but never in an unattended (hidden) host.</summary>
        private void OfferTripPayments()
        {
            if (!IsHandleCreated || FindForm() is not Form host || !host.Visible || host.Opacity < 1.0 || !host.ShowInTaskbar) return;
            BeginInvoke(new Action(() => { try { ShowTripPayments(auto: true); } catch (Exception ex) { ErrLog.Error("StatementOfAccountView.TripPayments", ex); } }));
        }
    }
}
