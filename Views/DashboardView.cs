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
        private Label lblTruckVal = null!;
        private Label lblTruckSub = null!;
        private Panel cardDeliveries = null!;
        private Panel cardCrew = null!;
        private Panel cardTrucks = null!;
        private string activeTimeframe = "Today";
        private string activeTiming = "All";
        private string activeTab = "Dispatches";
        private ComboBox cmbTimeframe = null!;
        private DateTimePicker dtpSpecificDate = null!;
        private ComboBox cmbTiming = null!;
        private bool isUpdatingFilterInternally = false;
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
            this.Resize += DashboardView_Resize;
        }

        private void SetupDateFilterStrip()
        {
            flowFilterControls.Controls.Clear();

            Label lblDate = new Label
            {
                Text = "Date:",
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Padding = new Padding(0, 5, 4, 0),
                Margin = new Padding(0, 0, 4, 4)
            };

            cmbTimeframe = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(15, 23, 42),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width = 120,
                Margin = new Padding(0, 1, 8, 4),
                Cursor = Cursors.Hand
            };
            cmbTimeframe.Items.AddRange(new object[] { "Today", "Specific Date", "This Week", "This Month", "All-Time" });
            cmbTimeframe.SelectedItem = activeTimeframe;

            dtpSpecificDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 8.5F),
                Width = 110,
                Margin = new Padding(0, 1, 12, 4),
                Value = DateTime.Today,
                Cursor = Cursors.Hand
            };

            Label lblTiming = new Label
            {
                Text = "Dispatches:",
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Padding = new Padding(0, 5, 4, 0),
                Margin = new Padding(0, 0, 4, 4)
            };

            cmbTiming = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(15, 23, 42),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width = 90,
                Margin = new Padding(0, 1, 0, 4),
                Cursor = Cursors.Hand
            };
            cmbTiming.Items.AddRange(new object[] { "All", "Past", "Present", "Future" });
            cmbTiming.SelectedItem = activeTiming;

            cmbTimeframe.SelectedIndexChanged += (s, e) =>
            {
                if (isUpdatingFilterInternally) return;
                activeTimeframe = cmbTimeframe.SelectedItem?.ToString() ?? "Today";
                if (activeTimeframe == "Today")
                {
                    isUpdatingFilterInternally = true;
                    dtpSpecificDate.Value = DateTime.Today;
                    isUpdatingFilterInternally = false;
                }
                LoadDashboardData();
            };

            dtpSpecificDate.ValueChanged += (s, e) =>
            {
                if (isUpdatingFilterInternally) return;
                isUpdatingFilterInternally = true;
                activeTimeframe = "Specific Date";
                cmbTimeframe.SelectedItem = "Specific Date";
                isUpdatingFilterInternally = false;
                LoadDashboardData();
            };

            cmbTiming.SelectedIndexChanged += (s, e) =>
            {
                activeTiming = cmbTiming.SelectedItem?.ToString() ?? "All";
                LoadDashboardData();
            };

            flowFilterControls.Controls.Add(lblDate);
            flowFilterControls.Controls.Add(cmbTimeframe);
            flowFilterControls.Controls.Add(dtpSpecificDate);
            flowFilterControls.Controls.Add(lblTiming);
            flowFilterControls.Controls.Add(cmbTiming);
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

            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("📊 Export Table to CSV...", null, (s, e) =>
            {
                try
                {
                    string reportName = activeTab == "Dispatches" ? "Active_Dispatches" : "Loading_Manifest";
                    string title = activeTab == "Dispatches" ? "Active Dispatch Operations" : "Daily Loading Manifest";
                    DataExportHelper.ExportToCsv(gridOperations, reportName, title);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export error: {ex.Message}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            menuActions.Items.Add("🖨️ Print Report", null, (s, e) =>
            {
                try
                {
                    if (activeTab == "Dispatches")
                        PrintHelper.PrintGridReport("Active Dispatch Operations Summary", gridOperations, "Real-time delivery dispatches and truck status summary.");
                    else
                        PrintHelper.PrintGridReport("Daily Loading Manifest Summary", gridOperations, "Daily loading schedule and crate manifest summary.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Print error: {ex.Message}", "Print Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            var btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);
            flowSearchPrint.Controls.Add(btnActions);

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
            cardDeliveries = CreateExecutiveKpiCard("Today's Deliveries", "0 Planned", "0 In Transit", Color.FromArgb(37, 99, 235), "Dispatch", out lblDeliveriesTitle, out lblDeliveriesVal, out lblDeliveriesSub);
            cardCrew = CreateExecutiveKpiCard("Crew Available", "0 Ready", "0 Active Drivers", Color.FromArgb(5, 150, 105), "Crew", out _, out lblCrewVal, out lblCrewSub);
            cardTrucks = CreateExecutiveKpiCard("Truck Status", "0 Ready", "0 In Transit • 0 Total Trucks", Color.FromArgb(217, 119, 6), "Trucks", out _, out lblTruckVal, out lblTruckSub);

            pnlKpis.Controls.Add(cardDeliveries, 0, 0);
            pnlKpis.Controls.Add(cardCrew, 1, 0);
            pnlKpis.Controls.Add(cardTrucks, 2, 0);
        }

        private void DashboardView_Resize(object? sender, EventArgs e)
        {
            if (cardDeliveries == null || cardCrew == null || cardTrucks == null) return;
            int currentWidth = this.Width;

            if (currentWidth < 1050)
            {
                if (pnlKpis.ColumnCount != 2)
                {
                    pnlKpiContainer.Height = 190;
                    pnlKpis.SuspendLayout();
                    pnlKpis.ColumnCount = 2;
                    pnlKpis.RowCount = 2;
                    pnlKpis.ColumnStyles.Clear();
                    pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    pnlKpis.RowStyles.Clear();
                    pnlKpis.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                    pnlKpis.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

                    pnlKpis.SetCellPosition(cardDeliveries, new TableLayoutPanelCellPosition(0, 0));
                    pnlKpis.SetCellPosition(cardCrew, new TableLayoutPanelCellPosition(1, 0));
                    pnlKpis.SetCellPosition(cardTrucks, new TableLayoutPanelCellPosition(0, 1));
                    pnlKpis.SetColumnSpan(cardTrucks, 2);
                    pnlKpis.ResumeLayout(true);
                }
            }
            else
            {
                if (pnlKpis.ColumnCount != 3)
                {
                    pnlKpiContainer.Height = 118;
                    pnlKpis.SuspendLayout();
                    pnlKpis.ColumnCount = 3;
                    pnlKpis.RowCount = 1;
                    pnlKpis.ColumnStyles.Clear();
                    pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
                    pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
                    pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
                    pnlKpis.RowStyles.Clear();
                    pnlKpis.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                    pnlKpis.SetColumnSpan(cardTrucks, 1);
                    pnlKpis.SetCellPosition(cardDeliveries, new TableLayoutPanelCellPosition(0, 0));
                    pnlKpis.SetCellPosition(cardCrew, new TableLayoutPanelCellPosition(1, 0));
                    pnlKpis.SetCellPosition(cardTrucks, new TableLayoutPanelCellPosition(2, 0));
                    pnlKpis.ResumeLayout(true);
                }
            }

            if (currentWidth < 950)
            {
                if (tblTableToolbar.ColumnCount != 1)
                {
                    tblTableToolbar.SuspendLayout();
                    tblTableToolbar.ColumnCount = 1;
                    tblTableToolbar.RowCount = 2;
                    tblTableToolbar.ColumnStyles.Clear();
                    tblTableToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                    tblTableToolbar.RowStyles.Clear();
                    tblTableToolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    tblTableToolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    tblTableToolbar.SetCellPosition(flowTabs, new TableLayoutPanelCellPosition(0, 0));
                    tblTableToolbar.SetCellPosition(flowSearchPrint, new TableLayoutPanelCellPosition(0, 1));
                    flowSearchPrint.Dock = DockStyle.Left;
                    tblTableToolbar.ResumeLayout(true);
                }
            }
            else
            {
                if (tblTableToolbar.ColumnCount != 2)
                {
                    tblTableToolbar.SuspendLayout();
                    tblTableToolbar.ColumnCount = 2;
                    tblTableToolbar.RowCount = 1;
                    tblTableToolbar.ColumnStyles.Clear();
                    tblTableToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    tblTableToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    tblTableToolbar.RowStyles.Clear();
                    tblTableToolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    tblTableToolbar.SetCellPosition(flowTabs, new TableLayoutPanelCellPosition(0, 0));
                    tblTableToolbar.SetCellPosition(flowSearchPrint, new TableLayoutPanelCellPosition(1, 0));
                    flowSearchPrint.Dock = DockStyle.Right;
                    tblTableToolbar.ResumeLayout(true);
                }
            }
        }

        private Panel CreateExecutiveKpiCard(string title, string defaultVal, string defaultSub, Color accentColor, string targetModule, out Label titleLabel, out Label valLabel, out Label subLabel)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(6),
                Cursor = Cursors.Hand,
                Padding = new Padding(14, 6, 14, 6),
                MinimumSize = new Size(180, 84)
            };

            Panel accentBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 3,
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
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 1)
            };

            valLabel = new Label
            {
                Text = defaultVal,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 0, 1),
                TextAlign = ContentAlignment.MiddleLeft
            };

            subLabel = new Label
            {
                Text = defaultSub,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 0, 0)
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
                if (this.IsDisposed || !this.IsHandleCreated) return;

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
                else if (activeTimeframe == "Specific Date")
                {
                    DateTime selectedDate = dtpSpecificDate != null ? dtpSpecificDate.Value.Date : now.Date;
                    filteredDeliveries = deliveries.Where(d => d.DispatchDate.Date == selectedDate);
                    filteredInvoices = invoices.Where(i => i.IssueDate.Date == selectedDate);
                    filteredSchedules = loadingSchedules.Where(s => s.ScheduleDate.Date == selectedDate);
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

                if (activeTiming == "Past")
                {
                    filteredDeliveries = filteredDeliveries.Where(d => d.DispatchDate.Date < now.Date);
                    filteredSchedules = filteredSchedules.Where(s => s.ScheduleDate.Date < now.Date);
                }
                else if (activeTiming == "Present")
                {
                    filteredDeliveries = filteredDeliveries.Where(d => d.DispatchDate.Date == now.Date);
                    filteredSchedules = filteredSchedules.Where(s => s.ScheduleDate.Date == now.Date);
                }
                else if (activeTiming == "Future")
                {
                    filteredDeliveries = filteredDeliveries.Where(d => d.DispatchDate.Date > now.Date);
                    filteredSchedules = filteredSchedules.Where(s => s.ScheduleDate.Date > now.Date);
                }

                currentDispatches = filteredDeliveries.OrderByDescending(d => d.DispatchDate).ToList();
                currentSchedules = filteredSchedules.OrderByDescending(s => s.ScheduleDate).ToList();

                RenderGridData();

                if (lblDeliveriesTitle != null)
                {
                    string tfLabel = activeTimeframe switch
                    {
                        "Today" => "Today's Deliveries",
                        "Specific Date" => dtpSpecificDate != null ? $"Deliveries ({dtpSpecificDate.Value:MMM dd, yyyy})" : "Deliveries (Specific Date)",
                        "This Week" => "This Week's Deliveries",
                        "This Month" => "This Month's Deliveries",
                        _ => "Total Deliveries (All-Time)"
                    };
                    if (activeTiming != "All")
                    {
                        tfLabel += $" [{activeTiming}]";
                    }
                    lblDeliveriesTitle.Text = tfLabel;
                }

                int todayPlanned = filteredDeliveries.Count(d => d.Status != "Cancelled");
                int inTransit = filteredDeliveries.Count(d => d.Status == "In Transit" || d.Status == "Delayed");
                int delayedDeliveries = filteredDeliveries.Count(d => d.Status == "Delayed");
                int deliveredCount = filteredDeliveries.Count(d => d.Status == "Completed" || d.Status == "Delivered");

                lblDeliveriesVal.Text = $"{todayPlanned}";
                lblDeliveriesSub.Text = delayedDeliveries > 0
                    ? $"{inTransit} In Transit ({delayedDeliveries} Delayed) • {deliveredCount} Delivered"
                    : $"{inTransit} In Transit • {deliveredCount} Delivered";

                int activeDrivers = crews.Count(c => c.Role.Equals("Driver", StringComparison.OrdinalIgnoreCase) && c.IsActive && c.IsAvailable);
                int totalAvailableCrew = crews.Count(c => c.IsActive && c.IsAvailable);
                int onTripCrew = crews.Count(c => c.IsActive && !c.IsAvailable);

                lblCrewVal.Text = $"{totalAvailableCrew} Ready";
                lblCrewSub.Text = $"{activeDrivers} Drivers • {onTripCrew} On Route";

                int totalTrucks = trucks.Count;
                int availableTrucks = trucks.Count(t => t.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) || t.Status.Equals("Available", StringComparison.OrdinalIgnoreCase));
                int inUseTrucks = trucks.Count(t => t.Status.Equals("In Use", StringComparison.OrdinalIgnoreCase) || t.Status.Equals("On Trip", StringComparison.OrdinalIgnoreCase));

                lblTruckVal.Text = $"{availableTrucks} Ready";
                lblTruckSub.Text = $"{inUseTrucks} In Transit • {totalTrucks} Total Trucks";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] LoadDashboardData: {ex.Message}");
            }
        }

        private void RenderGridData()
        {
            string q = txtQuickSearch.Text.Trim().ToLowerInvariant();

            gridOperations.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            gridOperations.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            gridOperations.ColumnHeadersDefaultCellStyle.Font = UITheme.GridHeaderFont;
            gridOperations.ColumnHeadersHeight = UITheme.CompactHeaderHeight;
            gridOperations.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            gridOperations.DefaultCellStyle.Font = UITheme.GridCellFont;
            gridOperations.DefaultCellStyle.SelectionBackColor = UITheme.SelectionHighlight;
            gridOperations.DefaultCellStyle.SelectionForeColor = UITheme.SelectionText;
            gridOperations.RowTemplate.Height = UITheme.CompactRowHeight;
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
