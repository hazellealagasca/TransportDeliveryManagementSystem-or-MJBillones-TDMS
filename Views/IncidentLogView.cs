using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 17: Resources → Incident log. Every damaged / missing crates incident recorded on a trip report (step 3), newest first, with the
    /// trip's client, truck and crew and who is charged. Read-only (incidents are added and removed on the trip report); double-click opens
    /// the dispatch details. Nothing in pay, statements, invoices or receipts reads it.
    /// </summary>
    public sealed class IncidentLogView : UserControl, IRefreshable
    {
        private readonly DateTimePicker dtFrom, dtTo;
        private readonly TextBox txtSearch;
        private readonly CheckBox chkChargedOnly;
        private readonly DataGridView grid;
        private readonly Label lblSummary, lblTitle, lblSub;
        private List<IncidentLogRow> all = new();
        private bool loading;

        public IncidentLogView()
        {
            BackColor = AppTheme.Canvas;
            Padding = new Padding(D(16), D(10), D(16), D(10));
            lblTitle = new Label { Dock = DockStyle.Top, AutoSize = true, Text = "Incident log", Font = new Font("Segoe UI Semibold", 14F), ForeColor = AppTheme.TextPrimary, UseMnemonic = false };
            lblSub = new Label { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, D(2), 0, D(8)), Font = new Font("Segoe UI", 9.5F), ForeColor = AppTheme.TextMuted, UseMnemonic = false,
                Text = "Damaged or missing crates recorded on trip reports (step 3). Add or remove an incident on the trip report; double-click a line to open its dispatch." };

            var filters = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, BackColor = AppTheme.Canvas, Padding = new Padding(0, 0, 0, D(8)) };
            Label L(string t) => new Label { Text = t, AutoSize = true, Margin = new Padding(0, D(7), D(6), 0), ForeColor = AppTheme.TextSecondary, UseMnemonic = false };
            dtFrom = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = Fmt.DateFormat, Width = D(120), Value = DateTime.Today.AddMonths(-3), Margin = new Padding(0, D(2), D(12), 0) };
            dtTo = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = Fmt.DateFormat, Width = D(120), Value = DateTime.Today, Margin = new Padding(0, D(2), D(12), 0) };
            txtSearch = new TextBox { Width = D(320), Margin = new Padding(0, D(2), D(12), 0) };
            PageKit.StyleSearch(txtSearch, "Search dispatch #, client, truck, crew, cause…");
            chkChargedOnly = new CheckBox { Text = "Charged to crew only", AutoSize = true, Margin = new Padding(0, D(5), 0, 0), ForeColor = AppTheme.TextPrimary, UseMnemonic = false };
            filters.Controls.AddRange(new Control[] { L("From"), dtFrom, L("to"), dtTo, txtSearch, chkChargedOnly });

            grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoGenerateColumns = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = AppTheme.Surface, BorderStyle = BorderStyle.None, Name = "gridIncidentLog"
            };
            UIThemeHelper.ApplyGridTheme(grid);
            DataGridViewTextBoxColumn C(string name, string header, float w, bool right = false)
            {
                var c = new DataGridViewTextBoxColumn { Name = name, HeaderText = header, FillWeight = w, SortMode = DataGridViewColumnSortMode.Automatic };
                if (right) c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                return c;
            }
            grid.Columns.AddRange(
                C("colDate", "Date", 60), C("colDispatch", "Dispatch #", 65), C("colClient", "Client", 90), C("colTruck", "Truck", 70),
                C("colDriver", "Driver", 95), C("colAides", "Aides", 110), C("colCause", "Cause", 110), C("colQty", "Damaged crates", 60, true),
                C("colCost", "Est. cost (₱)", 65, true), C("colCharged", "Charged to", 130), C("colNotes", "Notes", 120));
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenDispatch(e.RowIndex); };
            grid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter && grid.CurrentRow != null) { OpenDispatch(grid.CurrentRow.Index); e.Handled = e.SuppressKeyPress = true; } };
            RowMenu.Attach(grid, (m, rows) =>
            {
                if (rows[0].Tag is not IncidentLogRow x) return;
                m.Items.AddRange(RowMenu.Header($"⚠  Incident · {x.Cause}", $"{Fmt.Date(x.IncidentDate)} · Dispatch {x.DispatchNumber} · {x.DamagedQuantity:N0} crates · PHP {x.EstimatedCost:N2}"));
                m.Items.Add(RowMenu.Item("Open dispatch details", () => OpenDispatch(rows[0].Index), "Enter", x.DeliveryId > 0));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.CopySub(this, ("Dispatch #", x.DispatchNumber), ("Cause", x.Cause), ("Charged to", x.ChargedTo),
                    ("Summary", $"{Fmt.Date(x.IncidentDate)} · Dispatch {x.DispatchNumber} · {x.ClientName}: {x.Cause}, {x.DamagedQuantity:N0} crates, PHP {x.EstimatedCost:N2}, charged to {x.ChargedTo}")));
                m.Items.Add(RowMenu.ExportSub(grid, "Incident_Log", "Incident log", () => PrintHelper.PrintGridReport("Incident log", grid, "Damaged or missing crates recorded on trip reports.")));
            });

            lblSummary = new Label { Dock = DockStyle.Bottom, AutoSize = false, Height = D(32), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = AppTheme.TextPrimary, UseMnemonic = false, Name = "lblIncidentLogSummary" };

            Controls.Add(grid);
            Controls.Add(lblSummary);
            Controls.Add(filters);
            Controls.Add(lblSub);
            Controls.Add(lblTitle);

            dtFrom.ValueChanged += (s, e) => Render();
            dtTo.ValueChanged += (s, e) => Render();
            txtSearch.TextChanged += (s, e) => Render();
            chkChargedOnly.CheckedChanged += (s, e) => Render();
        }

        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);

        /// <summary>Nov 18: shows the incidents matching <paramref name="search"/> (e.g. a dispatch number) over all dates.</summary>
        public void ShowFor(string search)
        {
            RefreshData();
            var first = all.Count > 0 ? all.Min(x => x.IncidentDate.Date) : DateTime.Today;
            if (first < dtFrom.MinDate) first = dtFrom.MinDate;
            if (dtFrom.Value.Date > first) dtFrom.Value = first;
            if (dtTo.Value.Date < DateTime.Today) dtTo.Value = DateTime.Today;
            chkChargedOnly.Checked = false;
            txtSearch.Text = search ?? "";
        }

        public void RefreshData()
        {
            if (loading) return;
            loading = true;
            try { all = MJdatabase.GetIncidentLog(); }
            catch (Exception ex) { ErrLog.Error("IncidentLogView", ex); all = new List<IncidentLogRow>(); }
            finally { loading = false; }
            Render();
        }

        /// <summary>The lines that pass the date range, the search and the "charged to crew only" tick.</summary>
        public List<IncidentLogRow> VisibleRows()
        {
            DateTime from = dtFrom.Value.Date, to = dtTo.Value.Date;
            string q = txtSearch.Text.Trim();
            return all.Where(x => x.IncidentDate.Date >= from && x.IncidentDate.Date <= to
                                  && (!chkChargedOnly.Checked || x.ChargeableToCrew)
                                  && (q.Length == 0 || x.SearchText.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        private void Render()
        {
            var rows = VisibleRows();
            grid.SuspendLayout();
            try
            {
                grid.Rows.Clear();
                foreach (var x in rows)
                {
                    int i = grid.Rows.Add(Fmt.Date(x.IncidentDate), x.DispatchNumber, x.ClientName, x.TruckInfo, x.DriverName, x.AideNames, x.Cause,
                        x.DamagedQuantity.ToString("N0"), x.EstimatedCost.ToString("N2"), x.ChargedTo, x.Notes);
                    grid.Rows[i].Tag = x;
                }
            }
            finally { grid.ResumeLayout(); }
            int crates = rows.Sum(x => x.DamagedQuantity);
            decimal cost = rows.Sum(x => x.EstimatedCost), charged = rows.Where(x => x.ChargeableToCrew).Sum(x => x.EstimatedCost);
            lblSummary.Text = rows.Count == 0
                ? (all.Count == 0 ? "No incidents recorded yet. They come from step 3 of the trip report (damaged or missing crates)." : "No incident matches the dates or the search.")
                : $"{rows.Count:N0} incident{(rows.Count == 1 ? "" : "s")} · {crates:N0} damaged crate{(crates == 1 ? "" : "s")} · Est. cost PHP {cost:N2} · Charged to crew PHP {charged:N2}";
        }

        private void OpenDispatch(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count || grid.Rows[rowIndex].Tag is not IncidentLogRow x || x.DeliveryId <= 0) return;
            try
            {
                var d = MJdatabase.GetDeliveryById(x.DeliveryId);
                if (d == null) { QuietBox.Show($"Dispatch {x.DispatchNumber} was not found.", "Incident log", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                Forms.DispatchDetailsDialog.ShowFor(this, d);
            }
            catch (Exception ex) { AppLog.ShowFailure(FindForm(), "Incident log", "The dispatch could not be opened.", ex); }
        }
    }
}
