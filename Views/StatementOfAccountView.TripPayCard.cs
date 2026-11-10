using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 10: "2 · Trip payments" sits right under Client &amp; SOA type inside the statement form (not in a pop-up). One row per trip report:
    /// type its No. of drops, Amount and Fuel surcharge; every change goes straight into the trips table and the statement's totals,
    /// and the computation shows underneath. The trips table and this card always show the same figures.
    /// </summary>
    public partial class StatementOfAccountView
    {
        private Panel? soaCardPay;
        private DataGridView? gridPay;
        private Label? lblPayHint, lblPayNote;
        private readonly List<Label> payVals = new List<Label>();
        private System.Windows.Forms.Timer? payTimer;
        private bool payBuilding, payWriting;

        private Panel BuildTripPayCard()
        {
            gridPay = new DataGridView
            {
                Height = Dpi(80), AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoGenerateColumns = false,
                BackgroundColor = AppTheme.Surface, BorderStyle = BorderStyle.FixedSingle, SelectionMode = DataGridViewSelectionMode.CellSelect, MultiSelect = false,
                EditMode = DataGridViewEditMode.EditOnEnter, ScrollBars = ScrollBars.None, EnableHeadersVisualStyles = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing, ColumnHeadersHeight = Dpi(46),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9.5F)
            };
            gridPay.RowTemplate.Height = Dpi(34);
            gridPay.RowTemplate.MinimumHeight = Dpi(34);
            gridPay.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(219, 234, 254);
            gridPay.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(30, 58, 138);
            gridPay.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.75F);
            gridPay.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;   // long headers wrap onto two lines instead of being cut off
            gridPay.AllowUserToResizeRows = false;
            var ro = new DataGridViewCellStyle { BackColor = Pal.C(248, 250, 252) };
            var edit = new DataGridViewCellStyle { BackColor = Pal.C(254, 252, 232), Alignment = DataGridViewContentAlignment.MiddleRight };
            var money = new DataGridViewCellStyle(edit) { Format = "N2" };
            DataGridViewTextBoxColumn C(string name, string header, int w, bool readOnly, DataGridViewCellStyle st, int min)
                => new DataGridViewTextBoxColumn { Name = name, HeaderText = header, FillWeight = w, MinimumWidth = Dpi(min), ReadOnly = readOnly, DefaultCellStyle = new DataGridViewCellStyle(st), SortMode = DataGridViewColumnSortMode.NotSortable };
            gridPay.Columns.Add(C("pTrip", "Trip", 30, true, ro, 34));
            gridPay.Columns.Add(C("pReport", "Trip report #", 100, true, ro, 70));
            gridPay.Columns.Add(C("pDate", "Date", 70, true, ro, 66));
            gridPay.Columns.Add(C("pStores", "Stores", 150, true, ro, 70));
            gridPay.Columns.Add(C("pDrops", "No. of drops", 60, false, edit, 56));
            gridPay.Columns.Add(C("pAmount", "Amount (VAT incl.)", 80, false, money, 80));
            gridPay.Columns.Add(C("pFuel", "Fuel surcharge", 70, false, money, 76));
            gridPay.Columns.Add(C("pTotal", "Row total", 70, true, new DataGridViewCellStyle(ro) { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }, 76));
            gridPay.EditingControlShowing += (s, e) =>
            {
                if (e.Control is not TextBox tb) return;
                tb.TextChanged -= LivePayCommit; tb.TextChanged += LivePayCommit;
                void LivePayCommit(object? o, EventArgs a) { try { if (gridPay.IsCurrentCellDirty) gridPay.CommitEdit(DataGridViewDataErrorContexts.Commit); } catch (Exception ex) { ErrLog.Swallowed(ex); } }
            };
            gridPay.CellValidating += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                string cn = gridPay.Columns[e.ColumnIndex].Name;
                if (cn is not ("pDrops" or "pAmount" or "pFuel")) return;
                string txt = (e.FormattedValue?.ToString() ?? "").Replace(",", "").Replace("₱", "").Trim();
                if (txt.Length == 0) return;
                bool good = cn == "pDrops" ? int.TryParse(txt, out int n) && n >= 0 : decimal.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) && d >= 0;
                if (!good) { gridPay.Rows[e.RowIndex].ErrorText = cn == "pDrops" ? "Drops must be a whole number of 0 or more." : "Enter an amount of 0 or more, e.g. 8500 or 8500.50"; e.Cancel = true; }
                else gridPay.Rows[e.RowIndex].ErrorText = "";
            };
            gridPay.CellEndEdit += (s, e) => { if (e.RowIndex >= 0) gridPay.Rows[e.RowIndex].ErrorText = ""; };
            gridPay.CellValueChanged += (s, e) =>
            {
                if (payBuilding || e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (gridPay.Columns[e.ColumnIndex].Name is not ("pDrops" or "pAmount" or "pFuel")) return;
                WritePayRow(gridPay.Rows[e.RowIndex]);
            };

            lblPayHint = new Label { Dock = DockStyle.Top, Height = Dpi(24), Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, TextAlign = ContentAlignment.MiddleLeft, Text = "Type only the No. of drops, Amount and Fuel surcharge of each trip report. The statement's totals follow as you type." };
            lblPayNote = new Label { Dock = DockStyle.Top, Height = Dpi(24), Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.WarningText, TextAlign = ContentAlignment.MiddleLeft };

            var comp = new TableLayoutPanel { Height = Dpi(6 * 24 + 8), ColumnCount = 2, BackColor = AppTheme.Canvas, Padding = new Padding(Dpi(12), Dpi(4), Dpi(12), Dpi(4)) };
            comp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            comp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi(150)));
            string[] names = { "Total Sales (VAT Inclusive)", "Less: VAT (12%)", "Amount: Net of VAT", "Fuel surcharge (added, no VAT)", "Less: Withholding Tax (2%)", "TOTAL AMOUNT DUE" };
            for (int i = 0; i < names.Length; i++)
            {
                comp.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi(24)));
                bool last = i == names.Length - 1;
                var fnt = new Font(last ? "Segoe UI Semibold" : "Segoe UI", last ? 10.5F : 9.5F);
                comp.Controls.Add(new Label { Text = names[i], Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = fnt, ForeColor = last ? AppTheme.AccentText : AppTheme.TextSecondary }, 0, i);
                var v = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = fnt, ForeColor = last ? AppTheme.AccentText : AppTheme.TextPrimary };
                payVals.Add(v);
                comp.Controls.Add(v, 1, i);
            }
            payTimer = new System.Windows.Forms.Timer { Interval = 120 };
            payTimer.Tick += (s, e) => { payTimer!.Stop(); RebuildTripPay(); };
            gridItems.RowsAdded += (s, e) => SchedulePayRefresh();
            gridItems.RowsRemoved += (s, e) => SchedulePayRefresh();
            gridItems.CellValueChanged += (s, e) => SchedulePayRefresh();
            soaCardPay = CreateSectionCard(2, "Trip payments", "The trip reports on this statement: type their No. of drops, Amount and Fuel surcharge here.", lblPayHint, gridPay, lblPayNote, comp);
            RebuildTripPay();
            return soaCardPay;
        }

        /// <summary>Once trips are on the statement the client can't be changed (the trips, amounts and TIN belong to it); Clear form frees it again.</summary>
        private void UpdateClientLock()
        {
            if (cbClient == null || gridItems == null) return;
            bool hasTrips = gridItems.Rows.Cast<DataGridViewRow>().Any(r => !r.IsNewRow);
            if (!hasTrips || cbClient.Enabled) cbClient.Enabled = !hasTrips;
        }

        private void SchedulePayRefresh()
        {
            if (payWriting || payBuilding || payTimer == null) return;
            payTimer.Stop(); payTimer.Start();
        }

        /// <summary>Rebuilds the card's rows from the trips table (one row per trip) and shows the computation.</summary>
        private void RebuildTripPay()
        {
            if (gridPay == null || payWriting || gridPay.IsCurrentCellInEditMode) return;
            payBuilding = true;
            try
            {
                gridPay.Rows.Clear();
                var lines = CollectTripPayLines();
                foreach (var ln in lines)
                {
                    var r0 = gridItems.Rows[ln.First];
                    decimal amt = 0m, fuel = 0m;
                    for (int k = ln.First; k <= ln.Last; k++) { amt += CellDecimal(gridItems.Rows[k], "colAmount"); fuel += CellDecimal(gridItems.Rows[k], "colFuel"); }
                    int idx = gridPay.Rows.Add(ln.TripNo, ln.TripReport, ln.Date, ln.Stores, (r0.Cells["colDrops"].Value?.ToString() ?? "").Trim(), amt > 0 ? amt : (object)"", fuel > 0 ? fuel : (object)"", amt + fuel);
                    gridPay.Rows[idx].Tag = ln;
                    if (ln.Locked)
                    {
                        foreach (var cn in new[] { "pAmount", "pFuel" }) { gridPay.Rows[idx].Cells[cn].ReadOnly = true; gridPay.Rows[idx].Cells[cn].Style.BackColor = Pal.C(248, 250, 252); }
                        gridPay.Rows[idx].Cells["pStores"].ToolTipText = "This trip's Amount and Fuel surcharge come from its service invoice.";
                    }
                }
                gridPay.Height = gridPay.ColumnHeadersHeight + Math.Max(1, gridPay.Rows.Count) * gridPay.RowTemplate.Height + SystemInformation.HorizontalScrollBarHeight + Dpi(8);
                if (lblPayHint != null) lblPayHint.Text = lines.Count == 0
                    ? "No trip reports on this statement yet. Tick them in the Ready service invoices tab and add them; they appear here."
                    : "Type only the No. of drops, Amount and Fuel surcharge of each trip report. The statement's totals follow as you type.";
            }
            finally { payBuilding = false; }
            RefreshPayComputation();
            UpdateClientLock();
        }

        private void RefreshPayComputation()
        {
            if (gridPay == null) return;
            decimal a = 0m, fu = 0m; int missing = 0;
            payBuilding = true;
            try
            {
                foreach (DataGridViewRow r in gridPay.Rows)
                {
                    decimal ra = CellDecimal(r, "pAmount"), rf = CellDecimal(r, "pFuel");
                    a += ra; fu += rf;
                    if (ra <= 0) missing++;
                    r.Cells["pTotal"].Value = ra + rf;
                }
            }
            finally { payBuilding = false; }
            var t = SoaTotals.From(a, fu);
            decimal[] vals = { t.TotalSales, t.Vat, t.NetOfVat, t.Fuel, t.Withholding, t.Due };
            for (int i = 0; i < vals.Length && i < payVals.Count; i++) payVals[i].Text = "₱" + vals[i].ToString("N2", CultureInfo.InvariantCulture);
            if (lblPayNote != null) lblPayNote.Text = missing > 0 ? $"⚠ {missing} trip{(missing == 1 ? "" : "s")} without an amount yet. The statement can't be saved until every trip has one." : "";
        }

        /// <summary>A figure was typed in the card: write it into the trips table (drops on every store row of the trip; money on the trip's first row).</summary>
        private void WritePayRow(DataGridViewRow r)
        {
            if (r.Tag is not TripPayLine ln || ln.First >= gridItems.Rows.Count || ln.Last >= gridItems.Rows.Count) return;
            payWriting = true;
            bool was = isBindingForm;
            isBindingForm = true;
            try
            {
                string drops = (r.Cells["pDrops"].Value?.ToString() ?? "").Trim();
                for (int k = ln.First; k <= ln.Last; k++) gridItems.Rows[k].Cells["colDrops"].Value = drops.Length > 0 && int.TryParse(drops.Replace(",", ""), out int dn) ? dn : (object)drops;
                if (!ln.Locked)
                {
                    decimal amt = CellDecimal(r, "pAmount"), fuel = CellDecimal(r, "pFuel");
                    for (int k = ln.First; k <= ln.Last; k++)
                    {
                        gridItems.Rows[k].Cells["colAmount"].Value = k == ln.First && amt > 0 ? amt : (object)"";
                        gridItems.Rows[k].Cells["colFuel"].Value = k == ln.First && fuel > 0 ? fuel : (object)"";
                    }
                }
            }
            finally { isBindingForm = was; payWriting = false; }
            RecalculateTaxes(manualOverride: false);
            gridItems.Invalidate();
            RefreshPayComputation();
        }
    }
}
