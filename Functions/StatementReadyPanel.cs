using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// The worklist at the top of Statement of Account: every service invoice that is ready to go on a statement. Tick several (or "Select all")
    /// to make one statement; a statement is for ONE client and one kind of trip, so ticking an invoice of another client starts a new selection.
    /// </summary>
    public sealed class StatementReadyPanel : Panel
    {
        private readonly Label lblHeader = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 10F), UseMnemonic = false };
        private readonly Label lblHint = new() { AutoSize = true, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9F), UseMnemonic = false };
        private readonly ComboBox cbClient = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox txtSearch = new() { Width = 240 };
        private readonly CheckBox chkAll = new() { AutoSize = true, Text = "Select all", Font = new Font("Segoe UI Semibold", 9F), Cursor = Cursors.Hand };
        private readonly Button btnMake = new() { AutoSize = true, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI Semibold", 9F), UseMnemonic = false };
        private readonly DataGridView grid = new();
        private readonly Label lblEmpty = new() { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 10F), UseMnemonic = false };
        private readonly System.Windows.Forms.Timer searchTimer = new() { Interval = 200 };
        private List<ReadyInvoiceRow> all = new();
        private readonly HashSet<int> picked = new();
        private bool rendering;
        private string? note; // shown once under the buttons after the next redraw

        /// <summary>The person asked for a statement of these invoices (all of one client and kind).</summary>
        public event Action<string, List<ReadyInvoiceRow>>? StatementRequested;
        public event Action<int>? CountChanged;
        public int Count => all.Count;

        public StatementReadyPanel()
        {
            Dock = DockStyle.Top;
            Height = (int)Math.Round(190 * DeviceDpi / 96f); // H3: a queue card never takes more than ~190 px, so the ledger below keeps its room
            Padding = new Padding(0, 0, 0, 10);
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(0, 0, 0, 6) };
            PageKit.StyleCombo(cbClient);
            PageKit.StyleSearch(txtSearch, "Search SI, trip, client…");
            cbClient.SelectedIndexChanged += (s, e) => { if (!rendering) { picked.Clear(); Render(); } };
            txtSearch.TextChanged += (s, e) => { searchTimer.Stop(); searchTimer.Start(); };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) { txtSearch.Clear(); e.SuppressKeyPress = true; } };
            searchTimer.Tick += (s, e) => { searchTimer.Stop(); Render(); };
            chkAll.CheckedChanged += (s, e) => { if (!rendering) ToggleAll(chkAll.Checked); };
            btnMake.Click += (s, e) => Request(null);
            UIThemeHelper.SetButtonRole(btnMake, "primary");
            lblHeader.Margin = new Padding(0, 6, 14, 0);
            cbClient.Margin = new Padding(0, 2, 8, 0);
            txtSearch.Margin = new Padding(0, 2, 12, 0);
            chkAll.Margin = new Padding(0, 6, 14, 0);
            btnMake.Margin = new Padding(0, 0, 10, 0);
            lblHint.Margin = new Padding(0, 7, 0, 0);
            top.Controls.AddRange(new Control[] { lblHeader, cbClient, txtSearch, chkAll, btnMake, lblHint });

            grid.Dock = DockStyle.Fill;
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
            grid.CurrentCellDirtyStateChanged += (s, e) => { if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            grid.CellValueChanged += Grid_CellValueChanged;
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && e.ColumnIndex > 0 && grid.Rows[e.RowIndex].Tag is ReadyInvoiceRow r) Request(r); };
            grid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { Request(grid.CurrentRow?.Tag as ReadyInvoiceRow); e.SuppressKeyPress = true; } };
            grid.CellFormatting += Grid_CellFormatting;
            grid.CurrentCellChanged += (s, e) => UpdateButton();

            var host = new Panel { Dock = DockStyle.Fill };
            host.Controls.Add(grid);
            host.Controls.Add(lblEmpty);
            Controls.Add(host);
            Controls.Add(top);
            Disposed += (s, e) => searchTimer.Dispose();
        }

        private static string Group(ReadyInvoiceRow r) => r.ClientName.ToUpperInvariant() + "|" + r.Kind;

        /// <summary>Reads the list again from the database and redraws (keeps the ticks that are still valid).</summary>
        public void RefreshData()
        {
            // Nov 9 (statement first): the queue is the COMPLETED TRIP REPORTS that have no statement yet (not invoices). Row key = dispatch id.
            // Special trips keep coming from their service invoices.
            try
            {
                var today = DateTime.Today;
                all = MJdatabase.GetTripReportsWaitingForStatement().Select(c => new ReadyInvoiceRow
                {
                    InvoiceId = c.DeliveryId, InvoiceNumber = c.TripReportNumber, TripNumber = c.DispatchNumber, ClientName = c.ClientName, Kind = "Normal",
                    TripDate = c.ReportDate, IssueDate = c.ReportDate, Net = 0m, DaysWaiting = Math.Max(0, (int)(today - c.ReportDate.Date).TotalDays)
                }).ToList();
                all.AddRange(MJdatabase.GetInvoicesReadyForStatement().Where(r => r.Kind == "Special"));
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); all = new(); }
            picked.IntersectWith(all.Select(r => r.InvoiceId));
            rendering = true;
            try
            {
                string keep = cbClient.SelectedItem as string ?? "All clients";
                cbClient.Items.Clear();
                cbClient.Items.Add("All clients");
                foreach (var c in all.Select(r => r.ClientName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) cbClient.Items.Add(c);
                int idx = cbClient.Items.IndexOf(keep);
                cbClient.SelectedIndex = idx >= 0 ? idx : 0;
            }
            finally { rendering = false; }
            Render();
            CountChanged?.Invoke(all.Count);
        }

        private List<ReadyInvoiceRow> VisibleRows()
        {
            string q = txtSearch.Text.Trim();
            string client = cbClient.SelectedIndex > 0 ? cbClient.SelectedItem as string ?? "" : "";
            return all.Where(r => (client.Length == 0 || string.Equals(r.ClientName, client, StringComparison.OrdinalIgnoreCase))
                               && (q.Length == 0 || new[] { r.InvoiceNumber, r.TripNumber, r.ClientName, r.Kind }.Any(f => (f ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)))).ToList();
        }

        private void Render()
        {
            rendering = true;
            try
            {
                var rows = VisibleRows();
                grid.SuspendLayout();
                grid.Columns.Clear(); grid.Rows.Clear();
                grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "", FillWeight = 4, MinimumWidth = 34, SortMode = DataGridViewColumnSortMode.NotSortable });
                AddCol("Trip report / SI", 12); AddCol("Dispatch", 12); AddCol("Client", 24); AddCol("Completed", 10); AddCol("Invoiced", 10); AddCol("Amount", 12); AddCol("Waiting", 9);
                foreach (var r in rows)
                {
                    int i = grid.Rows.Add(picked.Contains(r.InvoiceId), r.InvoiceNumber, r.TripNumber + (r.Kind == "Special" ? " (special)" : ""), r.ClientName, r.TripDate.ToString("MMM d"), r.Kind == "Special" ? r.IssueDate.ToString("MMM d") : "no invoice yet", r.Net > 0 ? $"₱{r.Net:N2}" : "typed on the statement", r.DaysWaiting == 0 ? "today" : $"{r.DaysWaiting} d");
                    grid.Rows[i].Tag = r;
                }
                grid.ResumeLayout();
                int clients = all.Select(r => r.ClientName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                lblHeader.Text = $"{all.Count} completed trip report{(all.Count == 1 ? "" : "s")} waiting for a statement" + (all.Count > 0 ? $" · {clients} client{(clients == 1 ? "" : "s")}" : "");
                lblEmpty.Text = all.Count == 0 ? "Nothing is waiting. When a trip report is completed and has no statement yet, it appears here." : "No trip report matches your search.";
                lblEmpty.Visible = rows.Count == 0; lblEmpty.BringToFront(); grid.Visible = rows.Count > 0;
                chkAll.Enabled = rows.Count > 0;
                chkAll.Checked = rows.Count > 0 && picked.Count > 0 && rows.Where(r => Group(r) == SelectedGroup()).All(r => picked.Contains(r.InvoiceId)) && rows.All(r => Group(r) == SelectedGroup());
            }
            finally { rendering = false; }
            UpdateButton();
        }

        private void AddCol(string header, float weight)
        {
            var col = new DataGridViewTextBoxColumn { HeaderText = header, FillWeight = weight, MinimumWidth = 60, SortMode = DataGridViewColumnSortMode.NotSortable, ReadOnly = true };
            if (header == "Amount") col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns.Add(col);
        }

        private string? SelectedGroup() => all.Where(r => picked.Contains(r.InvoiceId)).Select(Group).FirstOrDefault();

        /// <summary>The invoices a statement would be made from: the ticked ones, or (nothing ticked) all of the highlighted row's client.</summary>
        private List<ReadyInvoiceRow> Target(ReadyInvoiceRow? focus)
        {
            if (picked.Count > 0) return all.Where(r => picked.Contains(r.InvoiceId)).ToList();
            var row = focus ?? grid.CurrentRow?.Tag as ReadyInvoiceRow;
            return row == null ? new() : all.Where(r => Group(r) == Group(row)).ToList();
        }

        private void UpdateButton()
        {
            var t = Target(null);
            if (t.Count == 0) { btnMake.Text = "Make statement ▸"; btnMake.Enabled = false; lblHint.Text = note ?? (all.Count > 0 ? "Tick invoices of one client, or press Select all." : ""); note = null; return; }
            btnMake.Enabled = true;
            string label = picked.Count > 0 ? $"{t.Count} invoice{(t.Count == 1 ? "" : "s")}" : $"all {t.Count} of this client";
            decimal known = t.Sum(r => r.Net);
            btnMake.Text = $"＋ New Statement ▸ {t[0].ClientName} · {label.Replace("invoice", "trip report")}" + (known > 0 ? $" · ₱{known:N2}" : "");
            lblHint.Text = note ?? (picked.Count > 0 ? "One statement is for one client." : "Nothing ticked: this uses every ready invoice of the highlighted client.");
            note = null;
        }

        private void Grid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (rendering || e.RowIndex < 0 || e.ColumnIndex != 0 || grid.Rows[e.RowIndex].Tag is not ReadyInvoiceRow r) return;
            bool on = grid.Rows[e.RowIndex].Cells[0].Value is true;
            if (on)
            {
                string? current = SelectedGroup();
                if (current != null && current != Group(r)) // another client or kind: a statement can't mix them, so start a new selection
                {
                    picked.Clear();
                    note = $"A statement is for one client, so the selection moved to {r.ClientName}.";
                }
                picked.Add(r.InvoiceId);
            }
            else picked.Remove(r.InvoiceId);
            BeginInvoke(new Action(Render)); // not inside the grid's own change event
        }

        private void ToggleAll(bool on)
        {
            var rows = VisibleRows();
            picked.Clear();
            if (on && rows.Count > 0)
            {
                // The group of the highlighted row (or the first row) - a statement is for one client.
                var focus = grid.CurrentRow?.Tag as ReadyInvoiceRow ?? rows[0];
                foreach (var r in rows.Where(r => Group(r) == Group(focus))) picked.Add(r.InvoiceId);
                if (rows.Any(r => Group(r) != Group(focus))) note = $"Selected all of {focus.ClientName}. Other clients need their own statement.";
                Render();
            }
            else Render();
        }

        private void Request(ReadyInvoiceRow? focus)
        {
            var t = Target(focus);
            if (t.Count == 0) return;
            StatementRequested?.Invoke(t[0].ClientName, t);
        }

        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].HeaderText != "Waiting") return;
            int days = grid.Rows[e.RowIndex].Tag is ReadyInvoiceRow r ? r.DaysWaiting : 0;
            string age = MJdatabase.WaitingAge(days);
            e.CellStyle.Font = new Font("Segoe UI Semibold", 9F);
            e.CellStyle.ForeColor = age == "Red" ? AppTheme.DangerText : age == "Amber" ? AppTheme.WarningText : AppTheme.TextMuted;
            if (age != "Normal") e.Value = (age == "Red" ? "● " : "◐ ") + e.Value;
        }
    }
}
