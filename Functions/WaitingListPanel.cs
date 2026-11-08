using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// The worklist at the top of Billing (plan section 4): every trip waiting for its service invoice (scrollable, live search, days waiting
    /// with an amber / red chip). It comes from the shared database function, so the number matches the Dashboard and the tab badge.
    /// Trips blocked by a missing trip report are not shown here; they are in Trip Reports.
    /// </summary>
    public sealed class WaitingListPanel : Panel
    {
        private readonly Label lblHeader = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 10F), UseMnemonic = false };
        private readonly Button btnAction = new() { AutoSize = true, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI Semibold", 9F), Text = "Invoice ▸", UseMnemonic = false };
        private readonly TextBox txtSearch = new() { Width = 300 };
        private readonly DataGridView grid = new();
        private readonly Label lblEmpty = new() { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 10F), UseMnemonic = false };
        private readonly System.Windows.Forms.Timer searchTimer = new() { Interval = 200 };
        private List<WaitingTripRow> invoiceRows = new();

        /// <summary>The person chose a trip to invoice (double-click, Enter or the Invoice button).</summary>
        public event Action<WaitingTripRow>? InvoiceRequested;
        /// <summary>Raised after a refresh with the number of trips waiting for an invoice.</summary>
        public event Action<int>? CountChanged;

        public int WaitingForInvoice => invoiceRows.Count;

        public WaitingListPanel()
        {
            Dock = DockStyle.Top;
            Height = (int)Math.Round(190 * DeviceDpi / 96f); // H3: a queue card never takes more than ~190 px, so the ledger below keeps its room
            Padding = new Padding(0, 0, 0, 10);
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(0, 0, 0, 6) };
            btnAction.Click += (s, e) => RequestSelected();
            PageKit.StyleSearch(txtSearch, "Search dispatch, client, route, trip report…");
            txtSearch.TextChanged += (s, e) => { searchTimer.Stop(); searchTimer.Start(); };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) { txtSearch.Clear(); e.SuppressKeyPress = true; } };
            searchTimer.Tick += (s, e) => { searchTimer.Stop(); Render(); };
            btnAction.Margin = new Padding(0, 0, 6, 0);
            UIThemeHelper.SetButtonRole(btnAction, "primary");
            lblHeader.Margin = new Padding(0, 6, 14, 0);
            txtSearch.Margin = new Padding(0, 2, 10, 0);
            top.Controls.AddRange(new Control[] { lblHeader, txtSearch, btnAction });

            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = AppTheme.Surface;
            grid.GridColor = AppTheme.Border;
            grid.RowTemplate.Height = 32;
            grid.ColumnHeadersHeight = 32;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Canvas;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextMuted;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
            grid.DefaultCellStyle.BackColor = AppTheme.Surface;
            grid.DefaultCellStyle.ForeColor = AppTheme.TextPrimary;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) RequestSelected(); };
            grid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { RequestSelected(); e.SuppressKeyPress = true; } };
            grid.CellFormatting += Grid_CellFormatting;

            var host = new Panel { Dock = DockStyle.Fill };
            host.Controls.Add(grid);
            host.Controls.Add(lblEmpty);
            Controls.Add(host);
            Controls.Add(top);
            Disposed += (s, e) => searchTimer.Dispose();
        }

        /// <summary>Reads the list again from the database and redraws.</summary>
        public void RefreshData()
        {
            try { invoiceRows = MJdatabase.GetTripsWaitingForInvoice(); }
            catch (Exception ex) { ErrLog.Swallowed(ex); invoiceRows = new(); }
            Render();
            CountChanged?.Invoke(invoiceRows.Count);
        }

        private static bool Hit(string q, params string[] fields) => fields.Any(f => (f ?? "").Contains(q, StringComparison.OrdinalIgnoreCase));

        private void Render()
        {
            string q = txtSearch.Text.Trim();
            grid.SuspendLayout();
            grid.Columns.Clear(); grid.Rows.Clear();
            var rows = invoiceRows.Where(r => q.Length == 0 || Hit(q, r.Number, r.ClientName, r.Route, r.TripReportNumber, r.Kind, r.Date.ToString("MMM d yyyy"))).ToList();
            decimal expected = rows.Sum(r => r.Expected);
            lblHeader.Text = $"{invoiceRows.Count} trip{(invoiceRows.Count == 1 ? "" : "s")} waiting for their service invoice" + (expected > 0 ? $" · ₱{expected:N2} known so far" : "");
            AddCol("Trip", 11); AddCol("Client", 22); AddCol("Date", 10); AddCol("Route", 22); AddCol("Trip report", 17); AddCol("Crates", 7); AddCol("Expected", 11); AddCol("Waiting", 10);
            foreach (var r in rows)
            {
                int i = grid.Rows.Add(r.Number, r.ClientName, r.Date.ToString("MMM d"), r.Route, r.TripReportNumber, r.Crates > 0 ? r.Crates.ToString() : "", r.Expected > 0 ? $"₱{r.Expected:N2}" : "", r.DaysWaiting == 0 ? "today" : $"{r.DaysWaiting} d");
                grid.Rows[i].Tag = r;
            }
            lblEmpty.Text = invoiceRows.Count == 0 ? "All caught up — no completed trips are waiting for their service invoice." : "No trip matches your search.";
            grid.ResumeLayout();
            lblEmpty.Visible = rows.Count == 0;
            lblEmpty.BringToFront();
            grid.Visible = rows.Count > 0;
            btnAction.Enabled = rows.Count > 0;
        }

        private void AddCol(string header, float weight) =>
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, FillWeight = weight, MinimumWidth = 60, SortMode = DataGridViewColumnSortMode.NotSortable });

        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].HeaderText != "Waiting") return;
            int days = grid.Rows[e.RowIndex].Tag is WaitingTripRow t ? t.DaysWaiting : 0;
            string age = MJdatabase.WaitingAge(days);
            e.CellStyle.Font = new Font("Segoe UI Semibold", 9F);
            if (age == "Red") e.CellStyle.ForeColor = AppTheme.DangerText;
            else if (age == "Amber") e.CellStyle.ForeColor = AppTheme.WarningText;
            else e.CellStyle.ForeColor = AppTheme.TextMuted;
            if (age != "Normal") e.Value = (age == "Red" ? "● " : "◐ ") + e.Value;
        }

        private void RequestSelected()
        {
            if (grid.CurrentRow?.Tag is not WaitingTripRow row) return;
            InvoiceRequested?.Invoke(row);
        }
    }
}
