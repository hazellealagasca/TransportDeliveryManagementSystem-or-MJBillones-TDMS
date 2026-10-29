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
    public class CompletedDispatchesView : UserControl, IRefreshable
    {
        private readonly List<Delivery> allCompleted = new List<Delivery>();
        private List<Delivery> filteredDeliveries = new List<Delivery>();

        private DataGridView gridCompleted = null!;
        private TextBox txtSearch = null!;
        private ComboBox cbDateFilter = null!;
        private int Dz(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private DateFilterChip dateChip = null!;
        private ChoiceChip sortChip = null!;
        private static readonly string[] CompletedSorts = { "Newest first", "Oldest first", "Client A–Z", "Most crates first" };
        private Label lblTotalTrips = null!;
        private Label lblGridCount = null!;
        public Action<int, string>? OnNavigateToIncidentRequested;
        public Action<int, string>? OnNavigateToTripReportRequested;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action<Delivery>? OnNavigateToCreateInvoiceRequested { get; set; }

        public CompletedDispatchesView()
        {
            InitializeCustomComponents();
            UIThemeHelper.ApplyTheme(this);
            SetupContextMenu();
            _ = LoadCompletedDeliveriesAsync();
        }

        private void InitializeCustomComponents()
        {
            this.SuspendLayout();
            this.Dock = DockStyle.Fill;
            this.BackColor = Pal.C(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            var pnlMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Pal.C(248, 250, 252),
                Padding = new Padding(16)
            };
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, Dz(58)));
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, Dz(72)));
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, Dz(104)));
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var pnlHeader = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8)
            };

            var lblTitle = new Label
            {
                Text = "✅ Completed Dispatches & Loading Schedules",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Pal.C(15, 23, 42),
                AutoSize = true,
                Location = new Point(0, 2)
            };

            var lblSubtitle = new Label
            {
                Text = "Every delivered trip. Select a row, then bill it, print it or open its trip report.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Pal.C(100, 116, 139),
                AutoSize = true,
                Location = new Point(2, 28)
            };

            var btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                BackColor = Pal.White,
                ForeColor = Pal.C(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(95, 32),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRefresh.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnRefresh.Location = new Point(pnlHeader.Width - 95, 6);
            btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRefresh.Click += (s, e) => RefreshData();

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(btnRefresh);

            void PlaceSub() { lblSubtitle.Top = lblTitle.Bottom + 1; }
            lblTitle.SizeChanged += (s, e) => PlaceSub();
            lblTitle.HandleCreated += (s, e) => PlaceSub();
            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 8)
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            pnlKpis.Controls.Add(CreateKpiCard("TOTAL COMPLETED DISPATCHES", "0", Pal.C(16, 185, 129), out lblTotalTrips), 0, 0);
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Pal.White,
                Padding = new Padding(10, 8, 10, 8)
            };

            var pnlSearchRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = Dz(42)
            };

            var flowFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            txtSearch = new TextBox
            {
                PlaceholderText = "🔍 Search dispatch #, client, destination, plate, driver...",
                Size = new Size(320, 28),
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 1, 8, 0)
            };
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            cbDateFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(130, 28),
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 1, 8, 0)
            };
            cbDateFilter.Items.AddRange(new object[] { "All Dates", "Today", "Last 7 Days", "This Month", "Last 30 Days" });
            cbDateFilter.SelectedIndex = 0;
            cbDateFilter.SelectedIndexChanged += (s, e) => ApplyFilter();

            var btnClearFilter = new Button
            {
                Text = "Reset",
                Font = new Font("Segoe UI", 8.5F),
                Size = new Size(68, 28),
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 8, 0)
            };
            btnClearFilter.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            dateChip = new DateFilterChip(DatePresets.History, "All dates") { Margin = new Padding(0, 0, 8, 0) };
            dateChip.RangeChanged += (s, e) => ApplyFilter();
            sortChip = new ChoiceChip("Sort", CompletedSorts, "⇅") { Margin = new Padding(0, 0, 8, 0) };
            sortChip.SelectionChanged += (s, e) => ApplyFilter();
            btnClearFilter.Click += (s, e) =>
            {
                txtSearch.Clear();
                cbDateFilter.SelectedIndex = 0;
                dateChip.Range = DateRange.All;
                sortChip.SelectedIndex = 0;
            };

            cbDateFilter.Visible = false;
            flowFilter.Controls.Add(txtSearch);
            flowFilter.Controls.Add(dateChip);
            flowFilter.Controls.Add(sortChip);
            flowFilter.Controls.Add(btnClearFilter);

            pnlSearchRow.Controls.Add(flowFilter);

            var flowActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = Dz(38),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 4, 0, 0)
            };

            var btnCreateInvoice = new Button
            {
                Text = "🧾 Create Billing Invoice",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(175, 29),
                BackColor = Pal.C(16, 185, 129),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnCreateInvoice.FlatAppearance.BorderSize = 0;
            btnCreateInvoice.Click += (s, e) =>
            {
                if (gridCompleted.SelectedRows.Count > 0 && gridCompleted.SelectedRows[0].DataBoundItem is Delivery del)
                {
                    OnNavigateToCreateInvoiceRequested?.Invoke(del);
                }
                else
                {
                    QuietBox.Show("Please select a completed delivery from the list to create its billing invoice.", "Select Delivery", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            var btnViewDetails = new Button
            {
                Text = "🔍 View Schedule",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(130, 29),
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnViewDetails.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnViewDetails.Click += (s, e) => ShowSelectedDetails();

            var btnPrintTripReport = new Button
            {
                Text = "📋 Print Trip Report",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(145, 29),
                BackColor = Pal.C(30, 58, 138),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnPrintTripReport.FlatAppearance.BorderSize = 0;
            btnPrintTripReport.Click += (s, e) => PrintSelectedTripReport();

            var btnPrintDossier = new Button
            {
                Text = "🖨️ Print Dossier",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(130, 29),
                BackColor = Pal.C(15, 23, 42),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnPrintDossier.FlatAppearance.BorderSize = 0;
            btnPrintDossier.Click += (s, e) => PrintSelectedDossier();

            var btnIncidents = new Button
            {
                Text = "⚠️ Incident Log",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(125, 29),
                BackColor = Pal.C(254, 242, 242),
                ForeColor = Pal.C(185, 28, 28),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnIncidents.FlatAppearance.BorderColor = Pal.C(254, 202, 202);
            btnIncidents.Click += (s, e) => NavigateToIncident();

            var btnTripReport = new Button
            {
                Text = "📝 View Trip Report",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(140, 29),
                BackColor = Pal.C(238, 242, 255),
                ForeColor = Pal.C(67, 56, 202),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 0)
            };
            btnTripReport.FlatAppearance.BorderColor = Pal.C(199, 210, 254);
            btnTripReport.Click += (s, e) => NavigateToTripReport();

            flowActions.Controls.Add(btnCreateInvoice);
            flowActions.Controls.Add(btnViewDetails);
            flowActions.Controls.Add(btnPrintTripReport);
            flowActions.Controls.Add(btnPrintDossier);
            flowActions.Controls.Add(btnIncidents);
            flowActions.Controls.Add(btnTripReport);

            pnlToolbar.Controls.Add(flowActions);
            pnlToolbar.Controls.Add(pnlSearchRow);

            var pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Pal.White,
                Padding = new Padding(12)
            };

            gridCompleted = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Pal.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Pal.C(241, 245, 249),
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                RowTemplate = { Height = 36 },
                EnableHeadersVisualStyles = false
            };
            gridCompleted.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(241, 245, 249);
            gridCompleted.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(71, 85, 105);
            gridCompleted.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            gridCompleted.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
            gridCompleted.ColumnHeadersHeight = 36;
            gridCompleted.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            gridCompleted.DefaultCellStyle.SelectionBackColor = Pal.C(224, 231, 255);
            gridCompleted.DefaultCellStyle.SelectionForeColor = Pal.C(15, 23, 42);
            SetupGridColumns();

            gridCompleted.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) ShowSelectedDetails();
            };

            lblGridCount = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                Text = "Showing 0 completed dispatches",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Pal.C(100, 116, 139),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };

            pnlGridCard.Controls.Add(gridCompleted);
            pnlGridCard.Controls.Add(lblGridCount);

            pnlMain.Controls.Add(pnlHeader, 0, 0);
            pnlMain.Controls.Add(pnlKpis, 0, 1);
            pnlMain.Controls.Add(pnlToolbar, 0, 2);
            pnlMain.Controls.Add(pnlGridCard, 0, 3);

            this.Controls.Add(pnlMain);
            this.ResumeLayout(false);
        }

        private Panel CreateKpiCard(string label, string val, Color accentColor, out Label valLabel)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Pal.White,
                Padding = new Padding(12, 10, 12, 10),
                Margin = new Padding(4, 0, 4, 0)
            };

            var topStrip = new Panel
            {
                Dock = DockStyle.Top,
                Height = 3,
                BackColor = accentColor
            };

            var lblT = new Label
            {
                Text = label,
                Font = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold),
                ForeColor = Pal.C(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 18,
                Padding = new Padding(0, 4, 0, 0)
            };

            valLabel = new Label
            {
                Text = val,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Pal.C(15, 23, 42),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnl.Controls.Add(valLabel);
            pnl.Controls.Add(lblT);
            pnl.Controls.Add(topStrip);
            return pnl;
        }

        private void SetupGridColumns()
        {
            gridCompleted.Columns.Clear();

            gridCompleted.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DispatchNumber",
                HeaderText = "Dispatch #",
                Width = 110,
                DefaultCellStyle = { Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) }
            });

            gridCompleted.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DispatchDate",
                HeaderText = "Date",
                Width = 95,
                DefaultCellStyle = { Format = "MM/dd/yyyy" }
            });

            gridCompleted.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ClientName",
                HeaderText = "Client",
                Width = 140
            });

            gridCompleted.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Destination",
                HeaderText = "Destination",
                Width = 150
            });

            gridCompleted.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TruckInfo",
                HeaderText = "Truck Assigned",
                Width = 130
            });

            gridCompleted.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DriverName",
                HeaderText = "Driver",
                Width = 120
            });

            gridCompleted.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "AideDisplay",
                HeaderText = "Aides / Helpers",
                Width = 130
            });

            gridCompleted.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "GCrate",
                HeaderText = "G-Crates",
                Width = 80,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            gridCompleted.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ECrate",
                HeaderText = "E-Crates",
                Width = 80,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            gridCompleted.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TotalCrates",
                HeaderText = "Total Crates",
                Width = 95,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) }
            });

            gridCompleted.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Status",
                HeaderText = "Status",
                Width = 95,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            gridCompleted.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridCompleted.Rows[e.RowIndex].DataBoundItem is Delivery del)
                {
                    OnNavigateToCreateInvoiceRequested?.Invoke(del);
                }
            };

            gridCompleted.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridCompleted.Columns[e.ColumnIndex].DataPropertyName == "Status")
                {
                    e.CellStyle.ForeColor = Pal.C(22, 101, 52);
                    e.CellStyle.BackColor = Pal.C(240, 253, 244);
                    e.CellStyle.Font = FontCache.Get("Segoe UI Semibold", 8.5F, FontStyle.Bold);
                }
            };
        }

        private void SetupContextMenu()
        {
            RowMenu.Attach(gridCompleted, (m, rows) =>
            {
                var dels = rows.Select(r => r.DataBoundItem).OfType<Delivery>().ToList();
                if (dels.Count == 0) return;
                if (dels.Count > 1)
                {
                    m.Items.AddRange(RowMenu.Header($"✅  {dels.Count} completed dispatches", $"{dels.Sum(d => d.TotalCrates):N0} crates · {dels.Select(d => d.ClientName).Distinct().Count()} client(s)"));
                    m.Items.Add(RowMenu.Item("Copy dispatch numbers", () => RowMenu.Copy(this, string.Join(", ", dels.Select(d => d.DispatchNumber)), "Dispatch numbers copied")));
                    m.Items.Add(RowMenu.Item("Print the selected rows", () => PrintHelper.PrintSelectedOrAllGridReport("Completed Dispatches", gridCompleted, "Selected completed dispatches.")));
                    m.Items.Add(RowMenu.Item("Bill them on a statement of account", () => MainForm.Instance?.NavigateToFinance("StatementOfAccount")));
                    m.Items.Add(RowMenu.Sep());
                    m.Items.Add(RowMenu.ExportSub(gridCompleted, "Completed_Dispatches", "Completed Dispatches"));
                    return;
                }
                var del = dels[0];
                Invoice? invoice = null;
                try { invoice = MJdatabase.GetInvoices().FirstOrDefault(i => i.DeliveryId == del.Id); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
                m.Items.AddRange(RowMenu.Header($"✅  {del.DispatchNumber}", $"{RowMenu.Short(del.ClientName, 26)} · {del.DispatchDate:MMM d, yyyy} · {del.TotalCrates} crates · {del.TruckInfo}"));
                m.Items.Add(RowMenu.Item("Open dispatch details", () => ShowSelectedDetails()));
                m.Items.Add(RowMenu.Item("Trip report", () => NavigateToTripReport()));
                m.Items.Add(RowMenu.Item("Incidents / file an incident", () => NavigateToIncident()));
                m.Items.Add(RowMenu.Sep());
                if (invoice == null) m.Items.Add(RowMenu.Good("Create billing invoice", () => OnNavigateToCreateInvoiceRequested?.Invoke(del)));
                else m.Items.Add(RowMenu.Item($"Show invoice {invoice.InvoiceNumber} ({invoice.Status})", () => MainForm.Instance?.ShowFinanceRecords("Billing", invoice.InvoiceNumber)));
                m.Items.Add(RowMenu.Item("Open client profile", () => MainForm.Instance?.OpenClientProfile(del.ClientName), "", !string.IsNullOrWhiteSpace(del.ClientName)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Sub("🖨️  Print",
                    RowMenu.Item("Official trip report", () => PrintSelectedTripReport()),
                    RowMenu.Item("End-to-end delivery dossier", () => PrintSelectedDossier())));
                m.Items.Add(RowMenu.CopySub(this, ("Dispatch number", del.DispatchNumber), ("Client", del.ClientName), ("Destination", del.Destination),
                    ("Summary", Forms.DispatchDetailsDialog.BuildSummaryText(del))));
                m.Items.Add(RowMenu.Sub("🔎  Find",
                    RowMenu.Item("Other trips for this client", () => RowMenu.Search(txtSearch, del.ClientName)),
                    RowMenu.Item("Other trips with this truck", () => RowMenu.Search(txtSearch, del.TruckInfo)),
                    RowMenu.Item("Other trips with this driver", () => RowMenu.Search(txtSearch, del.DriverName))));
                m.Items.Add(RowMenu.ExportSub(gridCompleted, "Completed_Dispatches", "Completed Dispatches"));
            }, allowMultiSelect: true);
        }

        public async void RefreshData()
        {
            await LoadCompletedDeliveriesAsync();
        }

        public async System.Threading.Tasks.Task LoadCompletedDeliveriesAsync()
        {
            try
            {
                var deliveries = await MJdatabase.GetDeliveriesAsync();
                allCompleted.Clear();
                allCompleted.AddRange(deliveries.Where(d => string.Equals(d.Status, "Completed", StringComparison.OrdinalIgnoreCase)).OrderByDescending(d => d.DispatchDate).ThenByDescending(d => d.Id));
                ApplyFilter();
            }
            catch (Exception ex)
            { ErrLog.Error("CompletedDispatchesView", ex); 
                MessageBox.Show("Failed to load completed dispatches: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilter()
        {
            string q = txtSearch?.Text?.Trim().ToLowerInvariant() ?? "";
            string dateFilter = cbDateFilter?.SelectedItem?.ToString() ?? "All Dates";
            DateTime now = DateTime.Today;

            var filtered = allCompleted.AsEnumerable();

            if (!string.IsNullOrEmpty(q))
            {
                filtered = filtered.Where(d =>
                    d.DispatchNumber.ToLowerInvariant().Contains(q) ||
                    d.ClientName.ToLowerInvariant().Contains(q) ||
                    d.Destination.ToLowerInvariant().Contains(q) ||
                    d.TruckInfo.ToLowerInvariant().Contains(q) ||
                    d.DriverName.ToLowerInvariant().Contains(q) ||
                    (!string.IsNullOrEmpty(d.AideNames) && d.AideNames.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(d.LoadingScheduleInfo) && d.LoadingScheduleInfo.ToLowerInvariant().Contains(q)));
            }

            var range = dateChip?.Range ?? DateRange.All;
            if (!range.IsAll) filtered = filtered.Where(d => range.Contains(d.DispatchDate));
            filtered = (sortChip?.SelectedIndex ?? 0) switch
            {
                1 => filtered.OrderBy(d => d.DispatchDate).ThenBy(d => d.Id),
                2 => filtered.OrderBy(d => d.ClientName).ThenByDescending(d => d.DispatchDate),
                3 => filtered.OrderByDescending(d => d.TotalCrates).ThenByDescending(d => d.DispatchDate),
                _ => filtered.OrderByDescending(d => d.DispatchDate).ThenByDescending(d => d.Id)
            };
            filteredDeliveries = filtered.ToList();
            gridCompleted.DataSource = filteredDeliveries;

            lblTotalTrips.Text = filteredDeliveries.Count.ToString("N0");
            lblGridCount.Text = $"Showing {filteredDeliveries.Count:N0} of {allCompleted.Count:N0} completed dispatches";
        }

        private Delivery? GetSelectedDelivery()
        {
            if (gridCompleted.CurrentRow != null && gridCompleted.CurrentRow.DataBoundItem is Delivery del)
            {
                return del;
            }
            return null;
        }

        private void ShowSelectedDetails()
        {
            var del = GetSelectedDelivery();
            if (del == null)
            {
                QuietBox.Show("Please select a completed dispatch first.", "Select Delivery", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            RecordDetailsDialog.ShowDetails(this, del);
        }

        private void PrintSelectedDossier()
        {
            var del = GetSelectedDelivery();
            if (del == null)
            {
                QuietBox.Show("Please select a completed dispatch from the grid first.", "Select Delivery", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            PrintHelper.PrintEndToEndDeliveryDossier(del.Id, del.DispatchNumber);
        }

        private void PrintSelectedTripReport()
        {
            var del = GetSelectedDelivery();
            if (del == null)
            {
                QuietBox.Show("Please select a completed dispatch from the grid first.", "Select Delivery", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            PrintHelper.PrintOfficialTripReport(del.Id, del.DispatchNumber);
        }

        private void NavigateToIncident()
        {
            var del = GetSelectedDelivery();
            if (del == null)
            {
                QuietBox.Show("Please select a completed dispatch first.", "Select Delivery", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            OnNavigateToIncidentRequested?.Invoke(del.Id, del.DispatchNumber);
        }

        private void NavigateToTripReport()
        {
            var del = GetSelectedDelivery();
            if (del == null)
            {
                QuietBox.Show("Please select a completed dispatch first.", "Select Delivery", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            OnNavigateToTripReportRequested?.Invoke(del.Id, del.DispatchNumber);
        }
    }
}
