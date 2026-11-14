using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 6 workflow: Finance History. "Settled records" lists what is fully paid (paid statements and paid lone invoices),
    /// so the working Billing and SOA lists can stay on open work. "Change log" is the old history view.
    /// </summary>
    public sealed class FinanceHistoryView : UserControl, IRefreshable
    {
        private readonly Panel host;
        private readonly Button btnSettled, btnLog, btnStatements;
        private FinanceStatementHistoryView? statements;
        private FinanceSettledView? settled;
        private ModuleHistoryView? log;

        public FinanceHistoryView()
        {
            BackColor = AppTheme.Canvas;
            int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = D(44), Padding = new Padding(D(12), D(8), D(12), 0), BackColor = AppTheme.Canvas, WrapContents = false };
            Button Tab(string text) => new Button { Text = text, AutoSize = true, MinimumSize = new Size(D(130), D(30)), Margin = new Padding(0, 0, D(6), 0), Cursor = Cursors.Hand };
            btnSettled = Tab("✅  Settled records");
            btnLog = Tab("📜  Change log");
            btnStatements = Tab("📑  Statement history");
            btnSettled.Click += (s, e) => Show(true);
            btnLog.Click += (s, e) => Show(false);
            btnStatements.Click += (s, e) => Show(false, true);
            bar.Controls.Add(btnStatements); bar.Controls.Add(btnSettled); bar.Controls.Add(btnLog);
            host = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas };
            Controls.Add(host); Controls.Add(bar);
            Show(true);
        }

        private void Show(bool showSettled, bool showStatements = false)
        {
            UIThemeHelper.SetButtonRole(btnSettled, showSettled ? "primary" : "secondary");
            UIThemeHelper.SetButtonRole(btnLog, !showSettled && !showStatements ? "primary" : "secondary");
            UIThemeHelper.SetButtonRole(btnStatements, showStatements ? "primary" : "secondary");
            statements?.RefreshData();
            Control view = showStatements
                ? statements ??= new FinanceStatementHistoryView { Dock = DockStyle.Fill }
                : showSettled
                ? settled ??= new FinanceSettledView { Dock = DockStyle.Fill }
                : log ??= new ModuleHistoryView("Billing & collections history", new[] { "Invoices", "CollectionReceipts", "StatementsOfAccount" }, isAdminOnly: true, paymentsSummary: true) { Dock = DockStyle.Fill };
            if (host.Controls.Count == 1 && host.Controls[0] == view) return;
            host.Controls.Clear();
            host.Controls.Add(view);
        }

        public void RefreshData()
        {
            settled?.RefreshData();
            statements?.RefreshData();
            log?.RefreshData();
        }
    }

    /// <summary>Paid statements and paid lone invoices, newest payment first.</summary>
    public sealed class FinanceSettledView : UserControl, IRefreshable
    {
        private readonly DateTimePicker dtFrom, dtTo;
        private readonly TextBox txtSearch;
        private readonly DataGridView grid;
        private readonly Label lblSummary;
        private List<(StatementOfAccount? Soa, List<TripInvoice> Invoices, string ReceiptNo, DateTime PaidOn)> records = new();
        /// <summary>The trips (dispatch and trip report) behind each record, in the same order as <see cref="records"/>.</summary>
        private List<List<SettledTrip>> recordTrips = new();
        private List<DuplicateBilling> billedTwice = new();
        private int loadId;

        public FinanceSettledView()
        {
            BackColor = AppTheme.Canvas;
            int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            var filters = new FlowLayoutPanel { Dock = DockStyle.Top, Height = D(46), Padding = new Padding(D(12), D(8), D(12), 0), BackColor = AppTheme.Canvas, WrapContents = false };
            Label L(string t) => new Label { Text = t, AutoSize = true, Margin = new Padding(0, D(6), D(6), 0), ForeColor = AppTheme.TextSecondary };
            dtFrom = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = Fmt.DateFormat, Width = D(120), Value = DateTime.Today.AddMonths(-3), Margin = new Padding(0, 0, D(12), 0) };
            dtTo = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = Fmt.DateFormat, Width = D(120), Value = DateTime.Today, Margin = new Padding(0, 0, D(12), 0) };
            txtSearch = new TextBox { Width = D(300), PlaceholderText = "Search client, dispatch, trip report, SOA, SI or receipt no.", Margin = new Padding(0, D(2), 0, 0) };
            filters.Controls.Add(L("Paid from")); filters.Controls.Add(dtFrom); filters.Controls.Add(L("to")); filters.Controls.Add(dtTo); filters.Controls.Add(txtSearch);
            dtFrom.ValueChanged += (s, e) => RefreshData();
            dtTo.ValueChanged += (s, e) => RefreshData();
            txtSearch.TextChanged += (s, e) => Bind();

            grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                BackgroundColor = AppTheme.Surface, BorderStyle = BorderStyle.None
            };
            UIThemeHelper.ApplyGridTheme(grid);
            void Col(string name, string header, int weight, bool money = false) => grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name, HeaderText = header, FillWeight = weight,
                DefaultCellStyle = money ? new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } : new DataGridViewCellStyle()
            });
            Col("colPaid", "Paid on", 70); Col("colReceipt", "Receipt No.", 70); Col("colClient", "Client", 120); Col("colSoa", "SOA No.", 80);
            Col("colType", "Type", 55); Col("colInvoices", "Service invoices", 120); Col("colTrips", "Dispatch · trip report", 190); Col("colAmount", "Amount paid", 80, true);
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenRecord(e.RowIndex); };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Open the statement", null, (s, e) => { if (grid.CurrentRow != null) OpenRecord(grid.CurrentRow.Index); });
            menu.Items.Add("Open the receipt", null, (s, e) => { if (grid.CurrentRow?.Tag is int i) FinanceHubView.Of(this)?.ShowCollections(records[i].ReceiptNo); });
            // One "Open order" item per trip of the selected record: opens that dispatch in Order History / Operations.
            menu.Opening += (s, e) =>
            {
                for (int k = menu.Items.Count - 1; k >= 0; k--) if (menu.Items[k].Tag as string == "order") menu.Items.RemoveAt(k);
                if (grid.CurrentRow?.Tag is not int i || i >= recordTrips.Count) return;
                int at = 2;
                foreach (var t in recordTrips[i])
                {
                    var trip = t;
                    var item = new ToolStripMenuItem($"Open order {trip.Number}" + (trip.TripReportNo.Length > 0 ? $" · {trip.TripReportNo}" : "")) { Tag = "order", Enabled = trip.DeliveryId > 0 };
                    item.Click += (a, b) => RowMenu.OpenDispatch(this, trip.DeliveryId, trip.Number);
                    menu.Items.Insert(at++, item);
                }
            };
            menu.Items.Add("📊 Export table to CSV...", null, (s, e) => DataExportHelper.ExportToCsv(grid, "Settled_Records", "Settled records"));
            grid.ContextMenuStrip = menu;

            lblSummary = new Label { Dock = DockStyle.Bottom, Height = D(32), Padding = new Padding(D(12), 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft, ForeColor = AppTheme.TextSecondary, BackColor = AppTheme.Canvas };
            Controls.Add(grid); Controls.Add(lblSummary); Controls.Add(filters);
            Load += (s, e) => RefreshData();
        }

        public async void RefreshData()
        {
            int my = ++loadId;
            DateTime from = dtFrom.Value.Date, to = dtTo.Value.Date;
            try
            {
                var (list, trips, twice) = await Task.Run(() =>
                {
                    var l = MJdatabase.GetSettledRecords(from, to);
                    List<DuplicateBilling> dup = new();
                    try { dup = MJdatabase.GetTripsBilledMoreThanOnce(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
                    return (l, MJdatabase.ResolveSettledTrips(l), dup);
                });
                if (my != loadId || IsDisposed) return;
                records = list; recordTrips = trips; billedTwice = twice;
                Bind();
            }
            catch (Exception ex)
            {
                ErrLog.Error("FinanceSettledView", ex);
                lblSummary.Text = "Settled records could not be loaded: " + ex.Message;
            }
        }

        private static string TypeOf((StatementOfAccount? Soa, List<TripInvoice> Invoices, string ReceiptNo, DateTime PaidOn) r)
        {
            if (r.Soa != null && !string.IsNullOrWhiteSpace(r.Soa.SoaType)) return r.Soa.SoaType;
            string kind = r.Invoices.FirstOrDefault()?.Kind ?? "";
            return kind.Length > 0 ? kind : SoaTypes.Normal;
        }

        private void Bind()
        {
            string q = txtSearch.Text.Trim();
            bool Match(string s) => s.Contains(q, StringComparison.OrdinalIgnoreCase);
            grid.Rows.Clear();
            decimal total = 0; int shown = 0;
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                string client = r.Soa?.ClientName ?? r.Invoices.FirstOrDefault()?.Client ?? "";
                string soaNo = r.Soa?.SoaNumber ?? "—";
                string sis = string.Join(", ", r.Invoices.Select(x => x.Number));
                var tripList = i < recordTrips.Count ? recordTrips[i] : new List<SettledTrip>();
                string trips = string.Join("; ", tripList.Select(t => t.TripReportNo.Length > 0 ? $"{t.Number} · {t.TripReportNo}" : t.Number));
                if (q.Length > 0 && !(Match(client) || Match(soaNo) || Match(sis) || Match(r.ReceiptNo) || Match(trips))) continue;
                decimal amount = r.Invoices.Sum(x => x.Net);
                int row = grid.Rows.Add(Fmt.Date(r.PaidOn), r.ReceiptNo, client, soaNo, TypeOf(r), sis, trips, amount);
                grid.Rows[row].Tag = i;
                total += amount; shown++;
            }
            string twiceNote = billedTwice.Count == 0 ? "" : $"   ·   ⚠ {billedTwice.Count} trip{(billedTwice.Count == 1 ? " is" : "s are")} billed more than once: {string.Join("; ", billedTwice.Take(3).Select(d => $"{d.TripLabel} ({string.Join(" + ", d.Invoices)})"))}{(billedTwice.Count > 3 ? "…" : "")}";
            lblSummary.Text = (shown == 0 ? $"Nothing was fully paid between {Fmt.Date(dtFrom.Value)} and {Fmt.Date(dtTo.Value)}." : $"{shown:N0} settled record{(shown == 1 ? "" : "s")} · PHP {total:N2} collected") + twiceNote;
            lblSummary.ForeColor = billedTwice.Count > 0 ? AppTheme.DangerText : AppTheme.TextSecondary;
        }

        private void OpenRecord(int rowIndex)
        {
            if (grid.Rows[rowIndex].Tag is not int i) return;
            var r = records[i];
            var hub = FinanceHubView.Of(this);
            if (r.Soa != null) hub?.ShowStatements(r.Soa.SoaNumber);
            else hub?.ShowInvoices(r.Invoices[0].Number);
        }
    }
}
