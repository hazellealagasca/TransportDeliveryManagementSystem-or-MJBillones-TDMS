using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class DashboardView : UserControl, IRefreshable
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action<string>? OnNavigateRequested { get; set; }

        public void RefreshData()
        {
            LoadDashboardData();
        }

        private Label lblDeliveriesTitle = null!;
        private Label lblDeliveriesVal = null!;
        private Label lblDeliveriesSub = null!;
        private Label lblCrewVal = null!;
        private Label lblCrewSub = null!;
        private Label lblFleetVal = null!;
        private Label lblFleetSub = null!;
        private Label lblFinanceVal = null!;
        private Label lblFinanceSub = null!;
        private string activeTimeframe = "Today";
        private string activeTab = "Dispatches";
        private List<Button> filterButtons = new List<Button>();
        private List<Delivery> currentDispatches = new List<Delivery>();
        private List<LoadingSchedule> currentSchedules = new List<LoadingSchedule>();

        public DashboardView()
        {
            InitializeComponent();
            gridOperations.BringToFront();
            UIThemeHelper.EnableDoubleBuffering(this);
            SetupDateFilterStrip();
            SetupKpiCards();
            SetupTabSwitching();
            SetupContextMenu();
            LoadDashboardData();
        }

        private void SetupDateFilterStrip()
        {
            string[] timeframes = { "All-Time", "Today", "This Week", "This Month" };
            filterButtons.Clear();
            flowFilterControls.Controls.Clear();

            for (int i = 0; i < timeframes.Length; i++)
            {
                string tf = timeframes[i];
                Button btn = new Button
                {
                    Text = tf,
                    Size = new Size(100, 32),
                    Margin = new Padding(0, 0, 8, 0),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };

                btn.Click += (s, e) =>
                {
                    activeTimeframe = tf;
                    UpdateFilterButtonStyles();
                    LoadDashboardData();
                };

                filterButtons.Add(btn);
                flowFilterControls.Controls.Add(btn);
            }

            flowFilterControls.Controls.Add(lblSystemBadge);
            UpdateFilterButtonStyles();
        }

        private void UpdateFilterButtonStyles()
        {
            foreach (var btn in filterButtons)
            {
                if (btn.Text == activeTimeframe)
                {
                    btn.BackColor = Color.FromArgb(15, 23, 42);
                    btn.ForeColor = Color.White;
                    btn.FlatAppearance.BorderSize = 0;
                }
                else
                {
                    btn.BackColor = Color.FromArgb(241, 245, 249);
                    btn.ForeColor = Color.FromArgb(71, 85, 105);
                    btn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
                }
            }
        }

        private void SetupTabSwitching()
        {
            btnTabDispatches.Click += (s, e) =>
            {
                activeTab = "Dispatches";
                UpdateTabButtonStyles();
                RenderGridData();
            };

            btnTabLoading.Click += (s, e) =>
            {
                activeTab = "Loading";
                UpdateTabButtonStyles();
                RenderGridData();
            };

            txtQuickSearch.TextChanged += (s, e) => RenderGridData();
            btnPrintTable.Click += (s, e) =>
            {
                if (activeTab == "Dispatches")
                    PrintHelper.PrintGridReport("Active Dispatch Operations Summary", gridOperations, "Real-time delivery dispatches and truck status summary.");
                else
                    PrintHelper.PrintGridReport("Daily Loading Manifest Summary", gridOperations, "Daily loading schedule and crate manifest summary.");
            };

            gridOperations.CellPainting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && gridOperations.Columns[e.ColumnIndex].HeaderText == "Status" && e.Graphics != null)
                {
                    e.PaintBackground(e.CellBounds, true);
                    string status = e.Value?.ToString() ?? "";
                    Color bg = Color.FromArgb(241, 245, 249);
                    Color fg = Color.FromArgb(51, 65, 85);

                    if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) || status.Equals("Delivered", StringComparison.OrdinalIgnoreCase) || status.Equals("Dispatched", StringComparison.OrdinalIgnoreCase))
                    {
                        bg = Color.FromArgb(240, 253, 244);
                        fg = Color.FromArgb(22, 101, 52);
                    }
                    else if (status.Equals("In Transit", StringComparison.OrdinalIgnoreCase) || status.Equals("Assigned", StringComparison.OrdinalIgnoreCase))
                    {
                        bg = Color.FromArgb(239, 246, 255);
                        fg = Color.FromArgb(30, 64, 175);
                    }
                    else
                    {
                        bg = Color.FromArgb(254, 243, 199);
                        fg = Color.FromArgb(180, 83, 9);
                    }

                    Rectangle pillRect = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y + 6, e.CellBounds.Width - 16, e.CellBounds.Height - 12);
                    using (SolidBrush brush = new SolidBrush(bg))
                    {
                        e.Graphics.FillRectangle(brush, pillRect);
                    }
                    using (Pen pen = new Pen(fg, 1))
                    {
                        e.Graphics.DrawRectangle(pen, pillRect);
                    }
                    TextRenderer.DrawText(e.Graphics, status, new Font("Segoe UI Semibold", 9F, FontStyle.Bold), pillRect, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    e.Handled = true;
                }
            };
        }

        private void UpdateTabButtonStyles()
        {
            if (activeTab == "Dispatches")
            {
                btnTabDispatches.BackColor = Color.FromArgb(15, 23, 42);
                btnTabDispatches.ForeColor = Color.White;
                btnTabDispatches.FlatAppearance.BorderSize = 0;

                btnTabLoading.BackColor = Color.FromArgb(241, 245, 249);
                btnTabLoading.ForeColor = Color.FromArgb(71, 85, 105);
                btnTabLoading.FlatAppearance.BorderSize = 1;
            }
            else
            {
                btnTabLoading.BackColor = Color.FromArgb(15, 23, 42);
                btnTabLoading.ForeColor = Color.White;
                btnTabLoading.FlatAppearance.BorderSize = 0;

                btnTabDispatches.BackColor = Color.FromArgb(241, 245, 249);
                btnTabDispatches.ForeColor = Color.FromArgb(71, 85, 105);
                btnTabDispatches.FlatAppearance.BorderSize = 1;
            }
        }

        private void SetupKpiCards()
        {
            pnlKpis.Controls.Clear();
            pnlKpis.Controls.Add(CreateExecutiveKpiCard("Today's Deliveries", "0 Planned", "0 In Transit", Color.FromArgb(37, 99, 235), "Dispatch", out lblDeliveriesTitle, out lblDeliveriesVal, out lblDeliveriesSub), 0, 0);
            pnlKpis.Controls.Add(CreateExecutiveKpiCard("Crew Available", "0 Ready", "0 Active Drivers", Color.FromArgb(5, 150, 105), "Crew", out _, out lblCrewVal, out lblCrewSub), 1, 0);
            pnlKpis.Controls.Add(CreateExecutiveKpiCard("Truck Utilization", "0%", "0 / 0 Trucks Ready", Color.FromArgb(217, 119, 6), "Trucks", out _, out lblFleetVal, out lblFleetSub), 2, 0);
            pnlKpis.Controls.Add(CreateExecutiveKpiCard("Receivables to Collect", "₱0.00", "Outstanding Balance", Color.FromArgb(220, 38, 38), "Billing", out _, out lblFinanceVal, out lblFinanceSub), 3, 0);
        }

        private Panel CreateExecutiveKpiCard(string title, string defaultVal, string defaultSub, Color accentColor, string targetModule, out Label titleLabel, out Label valLabel, out Label subLabel)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(6),
                Cursor = Cursors.Hand,
                Padding = new Padding(16, 10, 16, 10)
            };

            Panel accentBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 4,
                BackColor = accentColor,
                Cursor = Cursors.Hand
            };

            TableLayoutPanel tblCardText = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            tblCardText.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblCardText.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tblCardText.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblCardText.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 0, 2)
            };

            valLabel = new Label
            {
                Text = defaultVal,
                Font = new Font("Segoe UI", 19F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Fill,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 2),
                TextAlign = ContentAlignment.MiddleLeft
            };

            subLabel = new Label
            {
                Text = defaultSub,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Bottom,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 0)
            };

            tblCardText.Controls.Add(titleLabel, 0, 0);
            tblCardText.Controls.Add(valLabel, 0, 1);
            tblCardText.Controls.Add(subLabel, 0, 2);

            EventHandler clickHandler = (s, e) => OnNavigateRequested?.Invoke(targetModule);

            card.Click += clickHandler;
            accentBar.Click += clickHandler;
            tblCardText.Click += clickHandler;
            titleLabel.Click += clickHandler;
            valLabel.Click += clickHandler;
            subLabel.Click += clickHandler;

            EventHandler enterHandler = (s, e) => card.BackColor = Color.FromArgb(248, 250, 252);
            EventHandler leaveHandler = (s, e) => card.BackColor = Color.White;

            card.MouseEnter += enterHandler;
            tblCardText.MouseEnter += enterHandler;
            titleLabel.MouseEnter += enterHandler;
            valLabel.MouseEnter += enterHandler;
            subLabel.MouseEnter += enterHandler;

            card.MouseLeave += leaveHandler;
            tblCardText.MouseLeave += leaveHandler;
            titleLabel.MouseLeave += leaveHandler;
            valLabel.MouseLeave += leaveHandler;
            subLabel.MouseLeave += leaveHandler;

            card.Controls.Add(tblCardText);
            card.Controls.Add(accentBar);

            return card;
        }

        private async void LoadDashboardData()
        {
            try
            {
                var deliveries = await MJdatabase.GetDeliveriesAsync();
                var trucks = await MJdatabase.GetTrucksAsync();
                var crews = await MJdatabase.GetCrewsAsync();
                var invoices = await MJdatabase.GetInvoicesAsync();
                var loadingSchedules = await MJdatabase.GetLoadingSchedulesAsync();

                DateTime now = DateTime.Now;
                IEnumerable<Delivery> filteredDeliveries = deliveries;
                IEnumerable<Invoice> filteredInvoices = invoices;
                IEnumerable<LoadingSchedule> filteredSchedules = loadingSchedules;

                if (activeTimeframe == "Today")
                {
                    filteredDeliveries = deliveries.Where(d => d.DispatchDate.Date == now.Date);
                    filteredInvoices = invoices.Where(i => i.IssueDate.Date == now.Date);
                    filteredSchedules = loadingSchedules.Where(s => s.ScheduleDate.Date == now.Date);
                }
                else if (activeTimeframe == "This Week")
                {
                    DateTime startOfWeek = now.AddDays(-(int)now.DayOfWeek);
                    filteredDeliveries = deliveries.Where(d => d.DispatchDate.Date >= startOfWeek.Date);
                    filteredInvoices = invoices.Where(i => i.IssueDate.Date >= startOfWeek.Date);
                    filteredSchedules = loadingSchedules.Where(s => s.ScheduleDate.Date >= startOfWeek.Date);
                }
                else if (activeTimeframe == "This Month")
                {
                    filteredDeliveries = deliveries.Where(d => d.DispatchDate.Month == now.Month && d.DispatchDate.Year == now.Year);
                    filteredInvoices = invoices.Where(i => i.IssueDate.Month == now.Month && i.IssueDate.Year == now.Year);
                    filteredSchedules = loadingSchedules.Where(s => s.ScheduleDate.Month == now.Month && s.ScheduleDate.Year == now.Year);
                }

                currentDispatches = filteredDeliveries.OrderByDescending(d => d.DispatchDate).ToList();
                currentSchedules = filteredSchedules.OrderByDescending(s => s.ScheduleDate).ToList();

                RenderGridData();

                if (lblDeliveriesTitle != null)
                {
                    lblDeliveriesTitle.Text = activeTimeframe switch
                    {
                        "Today" => "Today's Deliveries",
                        "This Week" => "This Week's Deliveries",
                        "This Month" => "This Month's Deliveries",
                        _ => "Total Deliveries (All-Time)"
                    };
                }

                int todayPlanned = filteredDeliveries.Count(d => d.Status != "Cancelled");
                int inTransit = filteredDeliveries.Count(d => d.Status == "In Transit");
                int deliveredCount = filteredDeliveries.Count(d => d.Status == "Completed" || d.Status == "Delivered");

                lblDeliveriesVal.Text = $"{todayPlanned}";
                lblDeliveriesSub.Text = $"{inTransit} In Transit • {deliveredCount} Delivered";

                int activeDrivers = crews.Count(c => c.Role.Equals("Driver", StringComparison.OrdinalIgnoreCase) && c.IsActive && c.IsAvailable);
                int totalAvailableCrew = crews.Count(c => c.IsActive && c.IsAvailable);
                int onTripCrew = crews.Count(c => c.IsActive && !c.IsAvailable);

                lblCrewVal.Text = $"{totalAvailableCrew} Ready";
                lblCrewSub.Text = $"{activeDrivers} Drivers • {onTripCrew} On Route";

                int totalTrucks = trucks.Count;
                int availableTrucks = trucks.Count(t => t.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) || t.Status.Equals("Available", StringComparison.OrdinalIgnoreCase));
                int inUseTrucks = trucks.Count(t => t.Status.Equals("In Use", StringComparison.OrdinalIgnoreCase) || t.Status.Equals("On Trip", StringComparison.OrdinalIgnoreCase));
                double utilRate = totalTrucks > 0 ? (double)inUseTrucks / totalTrucks * 100 : 0;

                lblFleetVal.Text = $"{utilRate:F0}%";
                lblFleetSub.Text = $"{availableTrucks} / {totalTrucks} Trucks Ready";

                decimal outstandingBal = filteredInvoices.Where(i => i.Status != "Paid").Sum(i => i.NetAmount);
                lblFinanceVal.Text = $"₱{outstandingBal:N2}";
                lblFinanceSub.Text = "Outstanding Receivables";
            }
            catch { }
        }

        private void RenderGridData()
        {
            string q = txtQuickSearch.Text.Trim().ToLowerInvariant();

            gridOperations.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            gridOperations.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            gridOperations.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            gridOperations.ColumnHeadersHeight = 44;
            gridOperations.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            gridOperations.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            gridOperations.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 242, 255);
            gridOperations.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 27, 75);
            gridOperations.RowTemplate.Height = 42;
            gridOperations.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            gridOperations.GridColor = Color.FromArgb(241, 245, 249);
            gridOperations.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            gridOperations.AutoGenerateColumns = false;
            gridOperations.Columns.Clear();

            if (activeTab == "Dispatches")
            {
                var list = currentDispatches.Where(d =>
                    string.IsNullOrEmpty(q) ||
                    d.DispatchNumber.ToLowerInvariant().Contains(q) ||
                    d.LoadingScheduleInfo.ToLowerInvariant().Contains(q) ||
                    d.TruckInfo.ToLowerInvariant().Contains(q) ||
                    d.DriverName.ToLowerInvariant().Contains(q) ||
                    d.Status.ToLowerInvariant().Contains(q)
                ).ToList();

                gridOperations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DispatchNumber", HeaderText = "Dispatch #", FillWeight = 20, MinimumWidth = 160 });
                gridOperations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LoadingScheduleInfo", HeaderText = "Client & Destination", FillWeight = 30, MinimumWidth = 180 });
                gridOperations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TruckInfo", HeaderText = "Truck", FillWeight = 16, MinimumWidth = 110 });
                gridOperations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DriverName", HeaderText = "Driver", FillWeight = 18, MinimumWidth = 120 });
                gridOperations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalCrates", HeaderText = "Crates", FillWeight = 10, MinimumWidth = 75 });
                gridOperations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", FillWeight = 12, MinimumWidth = 85 });

                gridOperations.DataSource = null;
                gridOperations.DataSource = list;
            }
            else
            {
                var list = currentSchedules.Where(s =>
                    string.IsNullOrEmpty(q) ||
                    s.ScheduleNumber.ToLowerInvariant().Contains(q) ||
                    s.ClientName.ToLowerInvariant().Contains(q) ||
                    s.Destination.ToLowerInvariant().Contains(q) ||
                    s.Status.ToLowerInvariant().Contains(q)
                ).ToList();

                gridOperations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ScheduleNumber", HeaderText = "Schedule #", FillWeight = 20, MinimumWidth = 160 });
                gridOperations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = "Client", FillWeight = 24, MinimumWidth = 150 });
                gridOperations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Destination", HeaderText = "Destination", FillWeight = 26, MinimumWidth = 160 });
                gridOperations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CargoType", HeaderText = "Cargo Type", FillWeight = 14, MinimumWidth = 100 });
                gridOperations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalCrates", HeaderText = "Crates", FillWeight = 10, MinimumWidth = 75 });
                gridOperations.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", FillWeight = 12, MinimumWidth = 85 });

                gridOperations.DataSource = null;
                gridOperations.DataSource = list;
            }
        }

        private void SetupContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Font = new Font("Segoe UI Semibold", 9F);

            var itemDetails = new ToolStripMenuItem("🔍 View Full Details & History", null, (s, e) => {
                if (gridOperations.SelectedRows.Count > 0)
                {
                    var item = gridOperations.SelectedRows[0].DataBoundItem;
                    if (item != null) RecordDetailsDialog.ShowDetails(this, item);
                }
            });

            var itemGoToModule = new ToolStripMenuItem("🚀 Jump to Operational Module", null, (s, e) => {
                if (activeTab == "Dispatches") OnNavigateRequested?.Invoke("Dispatch");
                else OnNavigateRequested?.Invoke("Loading");
            });

            var itemCopyId = new ToolStripMenuItem("📋 Copy Identifier", null, (s, e) => {
                if (gridOperations.SelectedRows.Count > 0)
                {
                    var item = gridOperations.SelectedRows[0].DataBoundItem;
                    if (item is Delivery d) { Clipboard.SetText(d.DispatchNumber); ToastHelper.ShowToast(this, "Dispatch # copied ✓"); }
                    else if (item is LoadingSchedule ls) { Clipboard.SetText(ls.ScheduleNumber); ToastHelper.ShowToast(this, "Schedule # copied ✓"); }
                }
            });

            var itemPrint = new ToolStripMenuItem("🖨️ Print / Inspect Record", null, (s, e) => {
                if (gridOperations.SelectedRows.Count > 0)
                {
                    var item = gridOperations.SelectedRows[0].DataBoundItem;
                    if (item != null) RecordDetailsDialog.ShowDetails(this, item);
                }
            });

            menu.Items.AddRange(new ToolStripItem[] { itemDetails, itemGoToModule, new ToolStripSeparator(), itemCopyId, itemPrint });

            gridOperations.CellMouseDown += (s, e) => {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0) {
                    gridOperations.ClearSelection();
                    gridOperations.Rows[e.RowIndex].Selected = true;
                    if (e.ColumnIndex >= 0) gridOperations.CurrentCell = gridOperations.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    menu.Show(gridOperations, gridOperations.PointToClient(Cursor.Position));
                }
            };

            gridOperations.CellDoubleClick += (s, e) => {
                if (e.RowIndex >= 0 && gridOperations.Rows[e.RowIndex].DataBoundItem is object item) {
                    RecordDetailsDialog.ShowDetails(this, item);
                }
            };
        }
}
}
