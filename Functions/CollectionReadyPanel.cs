using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// The worklist at the top of Collections &amp; Receipts: every statement of account (and every service invoice on no statement) that is
    /// waiting for its payment, with the amount due, the due date and whether it is overdue. One receipt pays one row, in full.
    /// Same look as the "ready for a statement" list of the Statement of Account screen.
    /// </summary>
    public sealed class CollectionReadyPanel : Panel
    {
        private readonly Label lblHeader = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 10F), UseMnemonic = false };
        private readonly Label lblHint = new() { AutoSize = true, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9F), UseMnemonic = false };
        private readonly ComboBox cbClient = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox txtSearch = new() { Width = 240 };
        private readonly Button btnRecord = new() { AutoSize = true, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI Semibold", 9F), UseMnemonic = false };
        private readonly DataGridView grid = new();
        private readonly Label lblEmpty = new() { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 10F), UseMnemonic = false };
        private readonly System.Windows.Forms.Timer searchTimer = new() { Interval = 200 };
        private List<PayTarget> all = new();
        private bool rendering;

        /// <summary>The person chose a statement / invoice to record the payment of.</summary>
        public event Action<PayTarget>? PaymentRequested;
        public event Action<int>? CountChanged;
        public int Count => all.Count;

        public CollectionReadyPanel()
        {
            Dock = DockStyle.Top;
            Height = (int)Math.Round(190 * DeviceDpi / 96f); // H3: a queue card never takes more than ~190 px, so the ledger below keeps its room
            Padding = new Padding(0, 0, 0, 10);
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(0, 0, 0, 6) };
            PageKit.StyleCombo(cbClient);
            PageKit.StyleSearch(txtSearch, "Search statement, SI, client…");
            cbClient.SelectedIndexChanged += (s, e) => { if (!rendering) Render(); };
            txtSearch.TextChanged += (s, e) => { searchTimer.Stop(); searchTimer.Start(); };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) { txtSearch.Clear(); e.SuppressKeyPress = true; } };
            searchTimer.Tick += (s, e) => { searchTimer.Stop(); Render(); };
            btnRecord.Click += (s, e) => Request();
            UIThemeHelper.SetButtonRole(btnRecord, "primary");
            lblHeader.Margin = new Padding(0, 6, 14, 0);
            cbClient.Margin = new Padding(0, 2, 8, 0);
            txtSearch.Margin = new Padding(0, 2, 12, 0);
            btnRecord.Margin = new Padding(0, 0, 10, 0);
            lblHint.Margin = new Padding(0, 7, 0, 0);
            top.Controls.AddRange(new Control[] { lblHeader, cbClient, txtSearch, btnRecord, lblHint });

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
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Request(); };
            grid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { Request(); e.SuppressKeyPress = true; } };
            grid.CellFormatting += Grid_CellFormatting;
            grid.CurrentCellChanged += (s, e) => UpdateButton();

            var host = new Panel { Dock = DockStyle.Fill };
            host.Controls.Add(grid);
            host.Controls.Add(lblEmpty);
            Controls.Add(host);
            Controls.Add(top);
            Disposed += (s, e) => searchTimer.Dispose();
        }

        /// <summary>Reads the list again from the database (oldest due date first) and redraws.</summary>
        public void RefreshData()
        {
            try { all = MJdatabase.GetPayTargets(); }
            catch (Exception ex) { ErrLog.Swallowed(ex); all = new(); }
            rendering = true;
            try
            {
                string keep = cbClient.SelectedItem as string ?? "All clients";
                cbClient.Items.Clear();
                cbClient.Items.Add("All clients");
                foreach (var c in all.Select(t => t.Client).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) cbClient.Items.Add(c);
                int idx = cbClient.Items.IndexOf(keep);
                cbClient.SelectedIndex = idx >= 0 ? idx : 0;
            }
            finally { rendering = false; }
            Render();
            CountChanged?.Invoke(all.Count);
        }

        private List<PayTarget> VisibleRows()
        {
            string q = txtSearch.Text.Trim();
            string client = cbClient.SelectedIndex > 0 ? cbClient.SelectedItem as string ?? "" : "";
            return all.Where(t => (client.Length == 0 || string.Equals(t.Client, client, StringComparison.OrdinalIgnoreCase))
                               && (q.Length == 0 || t.SoaNumber.Contains(q, StringComparison.OrdinalIgnoreCase) || t.Client.Contains(q, StringComparison.OrdinalIgnoreCase)
                                   || t.Invoices.Any(i => i.Number.Contains(q, StringComparison.OrdinalIgnoreCase))))
                      .OrderBy(t => t.DueDate).ThenBy(t => t.Title, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string Due(DateTime due)
        {
            int days = (int)(due.Date - DateTime.Today).TotalDays;
            return days < 0 ? $"{-days} d overdue" : days == 0 ? "due today" : $"due in {days} d";
        }

        private void Render()
        {
            rendering = true;
            try
            {
                var rows = VisibleRows();
                grid.SuspendLayout();
                grid.Columns.Clear(); grid.Rows.Clear();
                AddCol("Statement of Account", 15); AddCol("Service invoices", 20); AddCol("Client", 22); AddCol("Invoiced", 9); AddCol("Due date", 10, false); AddCol("Amount due", 12, true); AddCol("Status", 12);
                foreach (var t in rows)
                {
                    int i = grid.Rows.Add(t.IsSoa ? t.SoaNumber : "(no statement)", string.Join(", ", t.Invoices.Select(x => x.Number)), t.Client, Fmt.Short(t.IssueDate), Fmt.Date(t.DueDate), $"₱{t.Net:N2}", Due(t.DueDate));
                    grid.Rows[i].Tag = t;
                }
                grid.ResumeLayout();
                int overdue = all.Count(t => t.DueDate.Date < DateTime.Today);
                lblHeader.Text = $"{all.Count} statement{(all.Count == 1 ? "" : "s")} / invoice{(all.Count == 1 ? "" : "s")} waiting for payment" + (all.Count > 0 ? $" · ₱{all.Sum(t => t.Net):N2}" + (overdue > 0 ? $" · {overdue} overdue" : "") : "");
                lblEmpty.Text = all.Count == 0 ? "Nothing is waiting for payment. Statements of account with their service invoices and nothing paid yet are listed here." : "No statement matches your search.";
                lblEmpty.Visible = rows.Count == 0; lblEmpty.BringToFront(); grid.Visible = rows.Count > 0;
            }
            finally { rendering = false; }
            UpdateButton();
        }

        private void AddCol(string header, float weight, bool? right = null)
        {
            var col = new DataGridViewTextBoxColumn { HeaderText = header, FillWeight = weight, MinimumWidth = 60, SortMode = DataGridViewColumnSortMode.NotSortable, ReadOnly = true };
            if (right == true) col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns.Add(col);
        }

        private PayTarget? Current => grid.CurrentRow?.Tag as PayTarget;

        private void UpdateButton()
        {
            var t = Current;
            if (t == null) { btnRecord.Text = "Record payment ▸"; btnRecord.Enabled = false; lblHint.Text = all.Count > 0 ? "Pick a row, then Record payment (or double-click it)." : ""; return; }
            btnRecord.Enabled = true;
            btnRecord.Text = $"Record payment ▸ {(t.IsSoa ? t.SoaNumber : "SI " + t.Invoices[0].Number)} · ₱{t.Net:N2}";
            lblHint.Text = "One receipt pays the full amount.";
        }

        private void Request()
        {
            if (Current is PayTarget t) PaymentRequested?.Invoke(t);
        }

        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].HeaderText != "Status") return;
            if (grid.Rows[e.RowIndex].Tag is not PayTarget t) return;
            int days = (int)(t.DueDate.Date - DateTime.Today).TotalDays;
            e.CellStyle.Font = new Font("Segoe UI Semibold", 9F);
            e.CellStyle.ForeColor = days < 0 ? AppTheme.DangerText : days <= 3 ? AppTheme.WarningText : AppTheme.TextMuted;
            if (days < 0) e.Value = "● " + e.Value;
        }
    }
}
