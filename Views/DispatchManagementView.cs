using System;

using System.Collections.Generic;

using System.Drawing;

using System.Linq;

using System.Threading.Tasks;

using System.Windows.Forms;

using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;

using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;



namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views

{

    public partial class DispatchManagementView : UserControl, IRefreshable, IUnsavedForm

    {

        private Delivery? selectedDelivery = null;

        private bool isBindingForm = false;

        private List<Delivery> allDispatches = new List<Delivery>();

        private string currentSortColumn = "Default";

        private bool isSortAscending = true;



        private List<LoadingSchedule> loadingSchedules = new List<LoadingSchedule>();

        private List<Truck> allTrucks = new List<Truck>();

        private List<Crew> allDrivers = new List<Crew>();

        private List<Crew> allAides = new List<Crew>();

        private List<Truck> trucks = new List<Truck>();

        private List<Crew> drivers = new List<Crew>();

        private List<Crew> aides = new List<Crew>();

        private System.Windows.Forms.Timer? tmrAutoSaveDraft;


        private Button btnToggleForm = null!;

        private Button? btnCompleteOrder;

        private Button? btnAwaitingCompletion;

        private Button? btnShowFinished;

        /// <summary>When on, completed and cancelled orders are listed under the active ones (remembered per user).</summary>

        private bool showFinished;

        private static string ShowFinishedKey => "DispatchShowFinished_" + (SessionContext.CurrentUsername ?? "");

        private CheckBox chkMultiDrop = null!;

        private Panel pnlMultiDropBuilder = null!;

        private ListBox lstStops = null!;

        private Label lblMultiDropSummary = null!;

        private List<DispatchStop> currentStops = new List<DispatchStop>();

        public Action? OnNavigateToCompletedRequested;

        private string currentStatusFilter = "";

        private FlowLayoutPanel? flowSummaryBadges;

        private Panel? pnlDraftBanner = new Panel();
        private Button? btnFilterTotal;

        private Button? btnFilterPending;

        private Button? btnFilterTransit;

        
        private Button? btnFilterCompleted;

        private Label? lblSummaryVolume;



        public DispatchManagementView()

        {

            InitializeComponent();

            Pal.RemapTree(this);

            lblFormHeader.UseMnemonic = false;

            UIThemeHelper.FitColumnHeaders(gridDispatch); // "E-Crates", "G-Crates" etc. shown in full, not "E-Crat…"

            lblGridHeader.UseMnemonic = false;

            pnlForm.AutoScroll = false;

            pnlFormHeaderBar.AutoScroll = false;

            pnlFormActionsBar.AutoScroll = false;

            gridDispatch.BringToFront();
            if (pnlSummary != null)
            {
                pnlSummary.Visible = false;
                pnlGridCard.Controls.Remove(pnlSummary);
            }

            SetupAideModuleEvents();

            InitializeMultiDropControls();

            InitializeSummaryBadges();

            SetupGridColumns();

            SetupContextMenu();

            WireRowCompleteButtons();

            InitSortAndStatusPickers();

            gridDispatch.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridDispatch.Rows[e.RowIndex].DataBoundItem is Delivery item) RecordDetailsDialog.ShowDetails(this, item); };

            gridDispatch.ColumnHeaderMouseClick += (s, e) => {

                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridDispatch.Columns.Count) return;

                string col = gridDispatch.Columns[e.ColumnIndex].DataPropertyName;

                if (string.IsNullOrEmpty(col)) return;

                SortByHeader(col);

            };

            cbTruck.DrawMode = DrawMode.OwnerDrawFixed;

            cbTruck.ItemHeight = 28;

            cbTruck.DrawItem += (s, e) => DrawStatusComboBoxItem(cbTruck, e);



            cbDriver.DrawMode = DrawMode.OwnerDrawFixed;

            cbDriver.ItemHeight = 28;

            cbDriver.DrawItem += (s, e) => DrawStatusComboBoxItem(cbDriver, e);

            SetupEvents();

            LoadDropdownsAndGrid();



            Button btnPrintLoadingSheet = new Button
            {
                Text = "🖨️ Print loading sheet",

                Size = new Size(162, 30),
                BackColor = Pal.C(30, 58, 138),

                ForeColor = Pal.White,

                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 4)
            };
            btnPrintLoadingSheet.FlatAppearance.BorderSize = 0;
            btnPrintLoadingSheet.Click += (s, e) => PrintOfficialLoadingSheetAction();
            pnlGridActionsFlow.Controls.Add(btnPrintLoadingSheet);

            ContextMenuStrip menuActions = new ContextMenuStrip();

            ToolStripMenuItem itemPrintOfficialSheet = new ToolStripMenuItem("🖨️ Print official loading schedule sheet", null, (s, e) => PrintOfficialLoadingSheetAction());

            ToolStripMenuItem itemPrintTripReport = new ToolStripMenuItem("📋 Print official trip report", null, (s, e) => {

                if (selectedDelivery != null) PrintHelper.PrintOfficialTripReport(selectedDelivery.Id, selectedDelivery.DispatchNumber);
                else QuietBox.Show("Please select a dispatch order first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);

            });

            ToolStripMenuItem itemExport = new ToolStripMenuItem("📊 Export to CSV", null, (s, e) => DataExportHelper.ExportToCsv(gridDispatch, "Dispatch_Orders", "Active and Dispatched Trips"));

            ToolStripMenuItem itemPrint = new ToolStripMenuItem("🖨️ Print dispatch summary", null, (s, e) => BtnPrint_Click(this, EventArgs.Empty));

            menuActions.Items.Add(itemPrintOfficialSheet);
            menuActions.Items.Add(itemPrintTripReport);
            menuActions.Items.Add(new ToolStripSeparator());
            menuActions.Items.Add(itemExport);
            menuActions.Items.Add(itemPrint);

            Button btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);



            btnPrint.Visible = false;

            btnCloseForm.Click += (s, e) => { if (FormGuard.ConfirmDiscard(this)) SetFormVisibility(false); };



            InitializeHorizontalFormResizer();

            currentFormWidth = UIThemeHelper.GetAdaptiveDrawerWidth(this, 420F, 480F);
            InitializeFormToggle();

            UIThemeHelper.ApplyTheme(this);

            ApplyPostThemeOverrides();

            SetButtonStates(false);

        }



        private Panel? pnlFormResizeGrip;

        private bool isResizingFormWidth = false;

        private int resizeStartScreenX = 0;

        private float resizeStartWidth = 580F;

        private float currentFormWidth = 420F;



        private void InitializeHorizontalFormResizer()

        {

            pnlFormResizeGrip = new Panel

            {

                Dock = DockStyle.Right,

                Width = 6,

                BackColor = Pal.C(226, 232, 240),

                Cursor = Cursors.SizeWE

            };

            pnlFormResizeGrip.MouseEnter += (s, e) => pnlFormResizeGrip.BackColor = Pal.C(59, 130, 246);

            pnlFormResizeGrip.MouseLeave += (s, e) => { if (!isResizingFormWidth) pnlFormResizeGrip.BackColor = Pal.C(226, 232, 240); };

            pnlFormResizeGrip.Paint += (s, e) =>

            {

                int cx = pnlFormResizeGrip.Width / 2;

                int cy = pnlFormResizeGrip.Height / 2;

                using var brush = new SolidBrush(Pal.C(148, 163, 184));

                for (int dy = -12; dy <= 12; dy += 6)

                {

                    e.Graphics.FillRectangle(brush, cx - 1, cy + dy, 2, 3);

                }

            };

            pnlFormResizeGrip.MouseDown += (s, e) =>

            {

                if (e.Button == MouseButtons.Left)

                {

                    isResizingFormWidth = true;

                    resizeStartScreenX = Cursor.Position.X;

                    resizeStartWidth = tblMasterContent.ColumnStyles[0].Width;

                    pnlFormResizeGrip.BackColor = Pal.C(37, 99, 235);

                }

            };

            pnlFormResizeGrip.MouseMove += (s, e) =>

            {

                if (!isResizingFormWidth) return;

                int deltaX = Cursor.Position.X - resizeStartScreenX;

                float maxAllowed = Math.Max(480F, this.ClientSize.Width - 420F);

                currentFormWidth = Math.Clamp(resizeStartWidth + deltaX, 360F, maxAllowed);

                tblMasterContent.ColumnStyles[0].Width = currentFormWidth;

                cbTruck.Invalidate();

                cbDriver.Invalidate();

            };

            pnlFormResizeGrip.MouseUp += (s, e) =>

            {

                if (!isResizingFormWidth) return;

                isResizingFormWidth = false;

                pnlFormResizeGrip.BackColor = Pal.C(226, 232, 240);

                if (gridDispatch.Columns.Count > 0)

                {

                    gridDispatch.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

                    gridDispatch.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                }

            };

            pnlForm.Controls.Add(pnlFormResizeGrip);

            pnlForm.Controls.SetChildIndex(pnlFormResizeGrip, pnlForm.Controls.Count - 1);

        }



        protected override void OnLoad(EventArgs e)

        {

            base.OnLoad(e);

            ApplyPostThemeOverrides();

        }



        private void ApplyPostThemeOverrides()

        {

            if (pnlFormResizeGrip != null)

            {

                pnlFormResizeGrip.Dock = DockStyle.Right;

                pnlFormResizeGrip.Width = 6;

                pnlFormResizeGrip.Cursor = Cursors.SizeWE;

                pnlForm.Controls.SetChildIndex(pnlFormResizeGrip, pnlForm.Controls.Count - 1);

            }

            pnlFormBody.AutoScrollMargin = new Size(0, 6);

            pnlFormBody.Padding = new Padding(12, 2, 12, 2);

            pnlFormBody.Resize -= PnlFormBody_Resize;

            pnlFormBody.Resize += PnlFormBody_Resize;

            flowAidesModule.Height = 66;

            if (chkMultiDrop != null)

            {

                chkMultiDrop.Padding = new Padding(0, 1, 0, 1);

            }

            pnlAidesSearchRow.Height = 28;

            btnClearAides.Height = 28;

            btnPickAnalogTime.Height = 28;

            var compactLabelFont = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold);

            foreach (var lbl in new[] { lblDispatchNumber, lblClient, lblDest, lblCargoType, lblPeakHeader, lblGCrate, lblECrate, lblTotalCrates, lblTr, lblDr, lblAi, lblDd, lblDepartureTime, lblSt })

            {

                lbl.Font = compactLabelFont;

                lbl.Padding = new Padding(0, 0, 0, 0);

            }

            lblRec.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);

            lblRec.MaximumSize = new Size(Math.Max(200, pnlRec.ClientSize.Width - 12), 0);

            pnlRec.Resize -= PnlRec_Resize;

            pnlRec.Resize += PnlRec_Resize;

            lblAidesSummary.AutoSize = false;

            lblAidesSummary.Height = 19;

            lblAidesSummary.MaximumSize = Size.Empty;

            lblAidesSummary.AutoEllipsis = true;

            lblAidesSummary.TextAlign = ContentAlignment.MiddleLeft;

            lblAidesSummary.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);

            lblAidesSummary.Padding = new Padding(0, 0, 0, 1);

            StyleFullPageForm();

            btnSave.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);

            btnUpdate.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);

            btnClear.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);

            txtTotalCrates.BackColor = Pal.C(241, 245, 249);

            txtTotalCrates.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);

            txtTotalCrates.TextAlign = HorizontalAlignment.Center;

            gridDispatch.DefaultCellStyle.Padding = new Padding(2, 0, 2, 0);

            gridDispatch.AlternatingRowsDefaultCellStyle.Padding = new Padding(2, 0, 2, 0);

            gridDispatch.ColumnHeadersDefaultCellStyle.Padding = new Padding(3, 0, 3, 0);

            if (btnToggleForm != null)
            {
                btnToggleForm.BackColor = pnlForm.Visible ? Pal.C(226, 232, 240) : Pal.C(185, 28, 28);

                btnToggleForm.ForeColor = pnlForm.Visible ? Pal.C(15, 23, 42) : Pal.White;

            }
        }

        // ------------------------------------------------------------------ Full-page order form

        private Panel? pnlHeaderSpacer;

        private const int FormContentMaxWidth = 1100;

        private int Dpi(int v) => (int)Math.Round(v * DeviceDpi / 96f);

        private void StyleFullPageForm()

        {

            if (pnlFormResizeGrip != null) pnlFormResizeGrip.Visible = false;

            pnlForm.BackColor = AppTheme.Canvas;

            pnlFormBody.BackColor = AppTheme.Canvas;

            pnlFormActionsBar.BackColor = AppTheme.Surface;

            // Header: back button on the left, page title next to it

            pnlFormHeaderBar.BackColor = AppTheme.Canvas;

            pnlFormHeaderBar.Height = Dpi(58);

            pnlFormHeaderBar.Padding = new Padding(0, Dpi(6), 0, Dpi(12));

            btnCloseForm.Text = "←  Back to orders";

            btnCloseForm.AutoSize = false;

            btnCloseForm.MinimumSize = Size.Empty;

            btnCloseForm.Padding = new Padding(0);

            btnCloseForm.Dock = DockStyle.Left;

            btnCloseForm.Width = Dpi(150);

            btnCloseForm.Font = new Font("Segoe UI Semibold", 9.5F);

            UIThemeHelper.SetButtonRole(btnCloseForm, "secondary");

            btnCloseForm.ForeColor = AppTheme.TextPrimary;

            pnlHeaderSpacer ??= new Panel { Dock = DockStyle.Left, Width = Dpi(16), BackColor = AppTheme.Canvas };

            if (!pnlFormHeaderBar.Controls.Contains(pnlHeaderSpacer)) pnlFormHeaderBar.Controls.Add(pnlHeaderSpacer);

            lblFormHeader.AutoSize = false;

            lblFormHeader.AutoEllipsis = true;

            lblFormHeader.Dock = DockStyle.Fill;

            lblFormHeader.TextAlign = ContentAlignment.MiddleLeft;

            lblFormHeader.Font = new Font("Segoe UI Semibold", 15F);

            lblFormHeader.ForeColor = AppTheme.TextPrimary;

            lblFormHeader.BackColor = AppTheme.Canvas;

            pnlFormHeaderBar.Controls.SetChildIndex(lblFormHeader, 0);

            pnlFormHeaderBar.Controls.SetChildIndex(pnlHeaderSpacer, 1);

            pnlFormHeaderBar.Controls.SetChildIndex(btnCloseForm, 2);

            pnlFormHeaderBar.Invalidate();

            // Body: centred column of section cards

            pnlFormBody.Padding = new Padding(0, 0, 0, Dpi(16));

            pnlForm.Resize -= PnlForm_ResizeCenter;

            pnlForm.Resize += PnlForm_ResizeCenter;

            BuildFormActionBar();

            flowAidesModule.Height = Dpi(118);

            CenterFormContent();

        }

        private void PnlForm_ResizeCenter(object? sender, EventArgs e) => CenterFormContent();

        private void CenterFormContent()

        {

            if (pnlForm == null || pnlForm.Width <= 0) return;

            int side = Math.Max(Dpi(24), (pnlForm.ClientSize.Width - Dpi(FormContentMaxWidth)) / 2);

            pnlFormHeaderBar.Padding = new Padding(side, pnlFormHeaderBar.Padding.Top, side, pnlFormHeaderBar.Padding.Bottom);

            pnlFormBody.Padding = new Padding(side, 0, side + SystemInformation.VerticalScrollBarWidth / 2, Dpi(16));

            pnlFormActionsBar.Padding = new Padding(side, Dpi(12), side, Dpi(12));

        }

        private FlowLayoutPanel? flowFormActions;

        private void BuildFormActionBar()

        {

            if (flowFormActions != null) return;

            flowFormActions = new FlowLayoutPanel

            {

                Dock = DockStyle.Fill,

                FlowDirection = FlowDirection.RightToLeft,

                WrapContents = false,

                BackColor = AppTheme.Surface,

                Padding = new Padding(0),

                Margin = new Padding(0)

            };

            tblFormButtons.Controls.Remove(btnSave);

            tblFormButtons.Controls.Remove(btnUpdate);

            tblFormButtons.Controls.Remove(btnReschedule);

            tblFormButtons.Controls.Remove(btnClear);

            tblFormButtons.Visible = false;

            foreach (var b in new[] { btnSave, btnUpdate, btnReschedule, btnClear })

            {

                b.Dock = DockStyle.None;

                b.Height = Dpi(40);

                b.Margin = new Padding(Dpi(10), 0, 0, 0);

                b.Font = new Font("Segoe UI Semibold", 9.5F);

            }

            btnSave.Width = Dpi(190);

            btnUpdate.Width = Dpi(190);

            btnReschedule.Width = Dpi(170);

            btnClear.Width = Dpi(190);

            UIThemeHelper.SetButtonRole(btnClear, "secondary");

            // Right-to-left: primary action sits at the far right

            flowFormActions.Controls.Add(btnSave);

            flowFormActions.Controls.Add(btnUpdate);

            flowFormActions.Controls.Add(btnReschedule);

            flowFormActions.Controls.Add(btnClear);

            pnlFormActionsBar.Controls.Add(flowFormActions);

            flowFormActions.BringToFront();

            pnlFormActionsBar.Height = Dpi(66);

        }



        private void PnlFormBody_Resize(object? sender, EventArgs e)

        {

            cbTruck.Invalidate();

            cbDriver.Invalidate();

        }



        private void PnlRec_Resize(object? sender, EventArgs e)

        {

            lblRec.MaximumSize = new Size(Math.Max(200, pnlRec.ClientSize.Width - 12), 0);

        }



        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)

        {

            if (keyData == Keys.Escape && pnlForm.Visible && EscapeGuard.AllowClose(this))

            {

                SetFormVisibility(false);

                return true;

            }

            return base.ProcessCmdKey(ref msg, keyData);

        }

        /// <summary>Opens a blank New Dispatch Order form (used by the dashboard's "+ New loading order").</summary>

        public void OpenNewOrderForm()

        {

            if (pnlForm.Visible && selectedDelivery == null) return; // already on a new order

            gridDispatch.ClearSelection();

            ClearFields();

            SetFormVisibility(true);

        }



        private void InitializeFormToggle()

        {

            btnToggleForm = new Button

            {

                Text = "📝 + Add order ▾",

                Size = new Size(150, 30),

                BackColor = Pal.C(185, 28, 28),

                ForeColor = Pal.White,

                FlatStyle = FlatStyle.Flat,

                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),

                Cursor = Cursors.Hand,

                Margin = new Padding(12, 4, 8, 4)

            };

            btnToggleForm.FlatAppearance.BorderSize = 0;

            btnToggleForm.Click += (s, e) =>

            {

                // "+ Add order" always opens a blank order, never the row that happens to be selected in the list.

                if (!pnlForm.Visible) ClearFields(); // Nov 6: a new order always opens blank

                SetFormVisibility(!pnlForm.Visible);

            };

            pnlGridActionsFlow.Controls.Add(btnToggleForm);

            Button btnSpecialTrip = new Button
            {
                Text = "✨ + Special trip",

                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                MinimumSize = new Size(160, 30),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = Pal.C(14, 116, 144),

                ForeColor = Pal.White,

                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 4)
            };
            btnSpecialTrip.FlatAppearance.BorderSize = 0;
            btnSpecialTrip.Click += (s, e) =>
            {
                // Nov 6: the special trip form opens full screen in the Special Trip tab (no popup).
                var mf = MainForm.Instance;
                mf?.NavigateToOperations("SpecialTrips");
                mf?.GetOperationsView<SpecialTripHistoryView>("SpecialTrips")?.OpenSpecialTripDialog();
            };
            pnlGridActionsFlow.Controls.Add(btnSpecialTrip);



            Features.Bind(btnSpecialTrip, () => btnSpecialTrip.Visible = Features.On(Features.SpecialTrips), Features.SpecialTrips);

            btnCompleteOrder = new Button

            {

                Name = "btnCompleteOrder",

                Text = "✔ Complete order",

                AutoSize = true,

                AutoSizeMode = AutoSizeMode.GrowOnly,

                MinimumSize = new Size(150, 30),

                Padding = new Padding(10, 0, 10, 0),

                BackColor = Pal.C(5, 150, 105),

                ForeColor = Pal.White,

                FlatStyle = FlatStyle.Flat,

                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),

                Cursor = Cursors.Hand,

                Margin = new Padding(0, 4, 8, 4),

                Enabled = false

            };

            btnCompleteOrder.FlatAppearance.BorderSize = 0;

            btnCompleteOrder.FlatAppearance.MouseOverBackColor = Pal.C(4, 120, 87);

            btnCompleteOrder.Click += (s, e) => CompleteSelectedOrder();

            btnCompleteOrder.Visible = false; // completion now lives in each order's Status cell

            btnAwaitingCompletion = new Button

            {

                Name = "btnAwaitingCompletion",

                Text = "⏰ 0 awaiting completion",

                AutoSize = true,

                AutoSizeMode = AutoSizeMode.GrowOnly,

                MinimumSize = new Size(120, 30),

                Padding = new Padding(10, 0, 10, 0),

                BackColor = Pal.C(254, 243, 199),

                ForeColor = Pal.C(180, 83, 9),

                FlatStyle = FlatStyle.Flat,

                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),

                Cursor = Cursors.Hand,

                Margin = new Padding(0, 4, 8, 4),

                Visible = false

            };

            btnAwaitingCompletion.FlatAppearance.BorderSize = 0;

            btnAwaitingCompletion.FlatAppearance.MouseOverBackColor = Pal.C(253, 230, 138);

            btnAwaitingCompletion.Click += (s, e) =>

            {

                currentStatusFilter = currentStatusFilter == AwaitingCompletionFilter ? "" : AwaitingCompletionFilter;

                FilterGridData();

                UpdateSummaryButtonStyles();

            };

            pnlGridActionsFlow.Controls.Add(btnAwaitingCompletion);

            try { showFinished = string.Equals(MJdatabase.GetSystemSetting(ShowFinishedKey, "False").Trim(), "True", StringComparison.OrdinalIgnoreCase); } catch (System.Exception swxSf) { ErrLog.Swallowed(swxSf); }

            btnShowFinished = new Button

            {

                Name = "btnShowFinished",

                Text = "○ Show finished",

                AutoSize = true,

                AutoSizeMode = AutoSizeMode.GrowOnly,

                MinimumSize = new Size(120, 30),

                Padding = new Padding(10, 0, 10, 0),

                Cursor = Cursors.Hand,

                Margin = new Padding(0, 4, 8, 4)

            };

            UIThemeHelper.SetButtonRole(btnShowFinished, "secondary");

            btnShowFinished.Click += (s, e) => SetShowFinished(!showFinished);

            new ToolTip().SetToolTip(btnShowFinished, "Show or hide completed and cancelled orders. They also stay in Order History.");

            pnlGridActionsFlow.Controls.Add(btnShowFinished);

            SetFormVisibility(false);

        }



        /// <summary>Turns the app's full-screen form mode on or off (left menu, top bar and the Operations tabs).</summary>
        private void SetFullScreen(bool on)
        {
            try
            {
                MainForm.Instance?.SetFocusMode(on);
                Control? c = Parent;
                while (c != null && !(c is OperationsHubView)) c = c.Parent;
                (c as OperationsHubView)?.SetTabsVisible(!on);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
        /// <summary>Nov 6: changing the route clears the store table; ask first when stores were added or crates typed. False = keep the old route.</summary>
        private bool ConfirmRouteSwitch(DeliveryRoute? next)
        {
            if (gridRouteStores == null || next == null || isBindingForm) return true;
            if (storeListRoute != null && storeListRoute.Id == next.Id) return true;
            int added = 0, crates = 0;
            foreach (DataGridViewRow r in gridRouteStores.Rows)
            {
                string c0 = gridRouteStores.Columns.Contains("colStoreCode") ? (r.Cells["colStoreCode"].Value?.ToString() ?? "").Trim() : "";
                string n0 = gridRouteStores.Columns.Contains("colStoreName") ? (r.Cells["colStoreName"].Value?.ToString() ?? "").Trim() : "";
                if (!IsRouteStoreRow(r) && (c0.Length > 0 || n0.Length > 0)) added++;
                crates += CratesOf(r);
            }
            if (added == 0 && crates == 0) return true;
            string what = added > 0 && crates > 0 ? $"{added} added store(s) and the crates you typed" : added > 0 ? $"{added} added store(s)" : "the crates you typed";
            return MessageBox.Show(this.FindForm(), $"Changing the route to {next.RouteCode} - {next.RouteName} clears the store table, including {what}, and loads that route's stores.\n\nChange the route?", "Change delivery route", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }
        /// <summary>True when switching needs no question (no added stores and no crates typed).</summary>
        private bool ConfirmRouteSwitchQuiet(DeliveryRoute next)
        {
            if (gridRouteStores == null) return true;
            foreach (DataGridViewRow r in gridRouteStores.Rows)
            {
                if (CratesOf(r) > 0) return false;
                string c0 = gridRouteStores.Columns.Contains("colStoreCode") ? (r.Cells["colStoreCode"].Value?.ToString() ?? "").Trim() : "";
                string n0 = gridRouteStores.Columns.Contains("colStoreName") ? (r.Cells["colStoreName"].Value?.ToString() ?? "").Trim() : "";
                if (!IsRouteStoreRow(r) && (c0.Length > 0 || n0.Length > 0)) return false;
            }
            return true;
        }
        /// <summary>Puts the route dropdown back on the route the table was loaded from (after "No").</summary>
        private void RevertRouteDropdown()
        {
            if (storeListRoute == null) return;
            bool was = isBindingForm;
            isBindingForm = true;
            try { cbDestination.Text = $"{storeListRoute.RouteCode} - {storeListRoute.RouteName}"; }
            finally { isBindingForm = was; }
        }
        // Nov 6: unsaved-typing tracking for the add / edit form (see FormGuard).
        private FormDirtyTracker? formDirty;
        public bool FormIsOpenAndDirty => pnlForm.Visible && (formDirty?.IsDirty ?? false);
        public void DiscardAndCloseForm() => SetFormVisibility(false);
        private void SetFormVisibility(bool visible)
        {
            // Nov 6: a form is blank whenever it is opened or left.
            if (!visible && pnlForm.Visible && IsHandleCreated) ClearFields();
            if (visible) formDirty ??= new FormDirtyTracker(pnlForm);
            formDirty?.Reset();

            if (visible && !pnlForm.Visible && selectedDelivery == null && string.IsNullOrWhiteSpace(cbClient.Text)) BeginInvoke(new Action(CheckAndPromptDraft));

            tblMasterContent.SuspendLayout();

            // The order form is a full page of its own: it replaces the order list instead of sitting beside it.

            pnlForm.Visible = visible;

            pnlGridCard.Visible = !visible;

            if (visible && selectedDelivery == null) BeginInvoke(new Action(ApplyDispatchNumberMode));

            if (tblMasterContent.ColumnStyles.Count > 0)

            {

                tblMasterContent.ColumnStyles[0].SizeType = visible ? SizeType.Percent : SizeType.Absolute;

                tblMasterContent.ColumnStyles[0].Width = visible ? 100F : 0F;

            }

            if (tblMasterContent.ColumnStyles.Count > 1)

            {

                tblMasterContent.ColumnStyles[1].SizeType = visible ? SizeType.Absolute : SizeType.Percent;

                tblMasterContent.ColumnStyles[1].Width = visible ? 0F : 100F;

            }

            pnlForm.Margin = new Padding(0);
            tblMasterContent.ResumeLayout(true);
            if (visible) CenterFormContent();
            // Nov 6: the order form is full screen (no menu, top bar or Operations tabs) while it is open.
            if (IsHandleCreated) BeginInvoke(new Action(() => SetFullScreen(pnlForm.Visible && Visible)));

            pnlGridCard.PerformLayout();

            if (gridDispatch.Columns.Count > 0)

            {

                gridDispatch.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

                gridDispatch.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            }

            if (btnToggleForm != null)

            {

                btnToggleForm.Text = visible ? "✕ Hide form ▴" : "📝 + Add order ▾";

                btnToggleForm.BackColor = visible ? Pal.C(226, 232, 240) : Pal.C(185, 28, 28);

                btnToggleForm.ForeColor = visible ? Pal.C(15, 23, 42) : Pal.White;

            }

            MainForm.Instance?.NotifyDrawerToggled(visible);

        }



        public void PreSelectTruck(int truckId)

        {

            try

            {

                if (trucks == null || trucks.Count == 0)

                {

                    trucks = MJdatabase.GetTrucks();

                }

                int idx = trucks.FindIndex(t => t.Id == truckId);

                if (idx >= 0 && idx < cbTruck.Items.Count)

                {

                    cbTruck.SelectedIndex = idx;

                    CalculateCratesAndRecommendation();

                }

            }

            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[Dispatch] PreSelectTruck: {ex.Message}"); ErrLog.Swallowed(ex); }

        }



        public void EditDeliveryOrder(Delivery delivery)

        {

            if (delivery == null) return;

            try

            {

                SetFormVisibility(true);

                BindDeliveryToForm(delivery);

                for (int i = 0; i < gridDispatch.Rows.Count; i++)

                {

                    if (gridDispatch.Rows[i].DataBoundItem is Delivery rowD && rowD.Id == delivery.Id)

                    {

                        gridDispatch.ClearSelection();

                        gridDispatch.Rows[i].Selected = true;

                        break;

                    }

                }

                txtDispatchNumber.Focus();

            }

            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[Dispatch] EditDeliveryOrder: {ex.Message}"); ErrLog.Swallowed(ex); }

        }



        public void InitiateDispatchForSchedule(LoadingSchedule schedule)

        {

            if (schedule == null) return;



            try

            {

                SetFormVisibility(true);

                ClearFields();



                cbClient.Text = schedule.ClientName;

                lastAutoDispatchNumber = AutoNumbers ? MJdatabase.NextDispatchNumber(schedule.ClientName) : "";

                txtDispatchNumber.Text = lastAutoDispatchNumber;

                var matchedRoute = MJdatabase.FindRouteByNameOrCode(schedule.Destination);

                cbDestination.Text = matchedRoute != null ? $"{matchedRoute.RouteCode} - {matchedRoute.RouteName}" : schedule.Destination;

                cbCargoType.Text = !string.IsNullOrEmpty(schedule.CargoType) ? schedule.CargoType : "Cold / Chilled";

                chkIsPeak.Checked = schedule.IsPeak;



                numGCrate.Value = Math.Max(0, schedule.GCrate);

                numECrate.Value = Math.Max(0, schedule.ECrate);

                txtTotalCrates.Text = $"{schedule.TotalCrates:N0} Crates";



                if (schedule.ScheduleDate >= dtpDispatchDate.MinDate && schedule.ScheduleDate <= dtpDispatchDate.MaxDate)

                {

                    dtpDispatchDate.Value = schedule.ScheduleDate;

                }



                if (!string.IsNullOrEmpty(schedule.DepartureTime))

                {

                    cbDepartureTime.Text = schedule.DepartureTime;

                }



                if (trucks == null || trucks.Count == 0)

                {

                    trucks = MJdatabase.GetTrucks();

                }



                var rec = Features.On(Features.TruckSuggestion) ? MJdatabase.GetTruckRecommendation(schedule.TotalCrates, schedule.ScheduleDate, schedule.CargoType) : null;

                if (rec?.RecommendedTruck != null)

                {

                    int trkIdx = trucks.FindIndex(t => t.Id == rec.RecommendedTruck.Id);

                    if (trkIdx >= 0 && trkIdx < cbTruck.Items.Count)

                    {

                        cbTruck.SelectedIndex = trkIdx;

                    }

                }



                chkMultiDrop.Checked = false;

                CalculateCratesAndRecommendation();

                UpdateDestinationHint();

                lblFormHeader.Text = $"New Dispatch Order  ·  {schedule.ScheduleNumber} ({schedule.ClientName})";

                cbDriver.Focus();

                ToastHelper.ShowToast(this, $"Loaded Order #{schedule.ScheduleNumber} for {schedule.ClientName} ({schedule.TotalCrates} Crates) ✓", ToastType.Info);

            }

            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[Dispatch] InitiateDispatchForSchedule: {ex.Message}"); ErrLog.Swallowed(ex); }

        }



        public void RefreshData()

        {

            LoadDropdownsAndGrid();

        }



        private void SetupEvents()

        {

            btnPickAnalogTime.Click += (s, e) => OpenAnalogClockPicker();

            cbClient.SelectedIndexChanged += async (s, e) =>
            {
                if (!isBindingForm)
                {
                    string cl = cbClient.Text.Trim();
                    await ReloadRoutesForClientAsync(cl);

                    if (IsDisposed) return;

                    // Rows already in the table are checked against the newly selected client.

                    ShowStoreWarning(null);

                    storeGuard?.RevalidateAll(showToast: true);

                }
            };

            cbClient.TextChanged += (s, e) =>

            {

                clientStrip?.ShowFor(cbClient.Text); AutoFillDispatchNumber();

                // Client box emptied: there is nothing to check the store rows against, so their marks are cleared.

                if (!isBindingForm && cbClient.Text.Trim().Length == 0) storeGuard?.RevalidateAll(showToast: false);

            };

            Features.Bind(txtDispatchNumber, ApplyDispatchNumberMode, Features.AutoDispatchNumber);

            cbDestination.SelectedIndexChanged += (s, e) =>
            {
                if (!isBindingForm)
                {
                    string selected = cbDestination.Text.Trim();
                    var matched = MJdatabase.FindRouteByNameOrCode(selected);
                    if (matched != null && (selected.Equals(matched.RouteName, StringComparison.OrdinalIgnoreCase) || selected.Equals(matched.RouteCode, StringComparison.OrdinalIgnoreCase)))
                    {
                        cbDestination.Text = $"{matched.RouteCode} - {matched.RouteName}";
                        }
                        if (!ConfirmRouteSwitch(matched)) { BeginInvoke(new Action(RevertRouteDropdown)); UpdateDestinationHint(); return; }
                        PopulateRouteStoresGrid(matched);
                        FillClientFromRoute(matched);

                }
                UpdateDestinationHint();
            };

            cbDestination.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    string txt = cbDestination.Text.Trim();
                    var matched = MJdatabase.FindRouteByNameOrCode(txt);
                    if (matched != null && (txt.Equals(matched.RouteName, StringComparison.OrdinalIgnoreCase) || txt.Equals(matched.RouteCode, StringComparison.OrdinalIgnoreCase)))
                    {
                        cbDestination.Text = $"{matched.RouteCode} - {matched.RouteName}";
                        cbDestination.SelectionStart = cbDestination.Text.Length;
                        if (!ConfirmRouteSwitch(matched)) { RevertRouteDropdown(); e.Handled = true; e.SuppressKeyPress = true; return; }
                        PopulateRouteStoresGrid(matched);
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                }
            };

            cbDestination.TextChanged += (s, e) =>
            {
                UpdateDestinationHint();
                if (!isBindingForm && cbDestination.Focused)
                {
                    string txt = cbDestination.Text.Trim();
                    var matched = MJdatabase.FindRouteByNameOrCode(txt);
                    // While typing, only switch when it is safe (no added stores or typed crates); otherwise wait for Enter / a pick, which ask first.
                    if (matched != null && (storeListRoute == null || storeListRoute.Id != matched.Id) && ConfirmRouteSwitchQuiet(matched))
                    {
                    PopulateRouteStoresGrid(matched);
                    }
                }
            };

            cbCargoType.SelectedIndexChanged += (s, e) =>

            {

                if (isBindingForm) return;

                if (CargoRouting.IsSpecial(cbCargoType.Text))

                {

                    string company = cbClient.Text.Trim();

                    DateTime when = dtpDispatchDate.Value;

                    isBindingForm = true;

                    try { cbCargoType.SelectedIndex = -1; cbCargoType.Text = ""; } finally { isBindingForm = false; }

                    BeginInvoke(new Action(() => CargoRouting.OfferSpecialTrip(FindForm(), company, when)));

                    return;

                }

                CalculateCratesAndRecommendation();

            };

            InputGuards.DigitsOnly(numGCrate, numECrate);

            numGCrate.ValueChanged += (s, e) =>
            {
                if (!isBindingForm)
                {
                    SyncNumericCratesToStoreGrid();
                    CalculateCratesAndRecommendation();
                }
            };

            numECrate.ValueChanged += (s, e) =>
            {
                if (!isBindingForm)
                {
                    SyncNumericCratesToStoreGrid();
                    CalculateCratesAndRecommendation();
                }
            };

            dtpDispatchDate.ValueChanged += (s, e) => { if (!isBindingForm) { RefreshResourcesForDate(); CalculateCratesAndRecommendation(); UpdateDateAvailabilityHint(); } };

            cbTruck.SelectedIndexChanged += (s, e) =>
            {
                if (!isBindingForm)
                {
                    AutoSuggestDriverForSelectedTruck();
                    CalculateCratesAndRecommendation();
                }
            };

            txtDispatchNumber.TextChanged += (s, e) =>
            {
                if (!isBindingForm)
                {
                    HandleDispatchNumberInput();
                }
            };

            SetupDraftAutoSave();

        }



        /// <summary>Client initials + running number, e.g. Red Ribbon → RR0001. Multi-drop uses the first stop's client.</summary>

        private string GenerateNextDispatchNumber()

        {

            string client = cbClient.Text.Trim();

            if (string.IsNullOrWhiteSpace(client) && currentStops.Count > 0) client = currentStops[0].ClientName ?? "";

            return MJdatabase.NextDispatchNumber(client);

        }

        private string lastAutoDispatchNumber = "";

        /// <summary>Settings → Features &amp; Pop-ups → "Automatic dispatch numbers".</summary>

        private static bool AutoNumbers => Features.On(Features.AutoDispatchNumber);

        /// <summary>With automatic numbers the box is filled by the system (read-only for new orders); without, it is typed in.</summary>

        private void ApplyDispatchNumberMode()

        {

            bool auto = AutoNumbers;

            bool newOrder = selectedDelivery == null;

            txtDispatchNumber.ReadOnly = auto && newOrder;

            txtDispatchNumber.PlaceholderText = auto ? "Filled in automatically when you pick the client (e.g. Red Ribbon → RR0001)" : "Type the dispatch number, e.g. RR0001";

            if (!auto && newOrder && txtDispatchNumber.Text.Trim().Equals(lastAutoDispatchNumber, StringComparison.OrdinalIgnoreCase)) { txtDispatchNumber.Text = ""; lastAutoDispatchNumber = ""; }

            if (auto && newOrder) AutoFillDispatchNumber();

        }

        /// <summary>Fills the dispatch # for a new order when the client is chosen, unless the user typed their own number.</summary>

        private void AutoFillDispatchNumber()

        {

            if (!AutoNumbers || isBindingForm || selectedDelivery != null || !pnlForm.Visible) return;

            string cur = txtDispatchNumber.Text.Trim();

            if (cur.Length > 0 && !cur.Equals(lastAutoDispatchNumber, StringComparison.OrdinalIgnoreCase)) return;

            string client = cbClient.Text.Trim();

            if (string.IsNullOrWhiteSpace(client) && currentStops.Count == 0) return;

            string next = GenerateNextDispatchNumber();

            if (next.Equals(cur, StringComparison.OrdinalIgnoreCase)) return;

            lastAutoDispatchNumber = next;

            txtDispatchNumber.Text = next;

        }



        private ToolTip tipRouteStores = new ToolTip { IsBalloon = false, ShowAlways = true, AutoPopDelay = 8000, InitialDelay = 150, ReshowDelay = 100 };



        private void ShowRouteStoresPopup(DeliveryRoute route)

        {

            if (route == null || route.PlacesList.Count == 0 || this.Disposing || this.IsDisposed) return;

            try

            {

                var sb = new System.Text.StringBuilder();

                sb.AppendLine($"🛣️ {route.RouteCode} - {route.RouteName} ({route.TotalStops} Stores to Deliver):");

                for (int i = 0; i < route.PlacesList.Count; i++)

                {

                    sb.AppendLine($"  {i + 1}. {route.PlacesList[i]}");

                }

                string text = sb.ToString().TrimEnd();

                tipRouteStores.SetToolTip(cbDestination, text);

                tipRouteStores.Show(text, cbDestination, 0, cbDestination.Height + 4, 6000);

            }

            catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }

        }



        private void UpdateDestinationHint()

        {

            string dest = cbDestination.Text.Trim();

            if (string.IsNullOrEmpty(dest))

            {

                tipRouteStores.SetToolTip(cbDestination, "Type route code (e.g. R1, R2) to see list of stores.");

                return;

            }

            var matchedRoute = MJdatabase.FindRouteByNameOrCode(dest);

            if (matchedRoute != null)

            {

                var sb = new System.Text.StringBuilder();

                sb.AppendLine($"🛣️ {matchedRoute.RouteCode} - {matchedRoute.RouteName} ({matchedRoute.TotalStops} Stores to Deliver):");

                for (int i = 0; i < matchedRoute.PlacesList.Count; i++)

                {

                    sb.AppendLine($"  {i + 1}. {matchedRoute.PlacesList[i]}");

                }

                tipRouteStores.SetToolTip(cbDestination, sb.ToString().TrimEnd());

                return;

            }

            tipRouteStores.SetToolTip(cbDestination, $"📍 Destination: {dest} (Direct Delivery)");

        }



        private void OpenAnalogClockPicker()

        {

            string cur = cbDepartureTime.Text;

            if (string.IsNullOrWhiteSpace(cur)) cur = "08:00 AM";



            var dlg = new AnalogClockTimePickerDialog(cur);

            if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)

            {

                string sel = dlg.SelectedTimeString;

                if (!cbDepartureTime.Items.Contains(sel))

                {

                    cbDepartureTime.Items.Add(sel);

                }

                cbDepartureTime.SelectedItem = sel;

                cbDepartureTime.Text = sel;

            }

        }



        private void CalculateCratesAndRecommendation()

        {

            int g = (int)numGCrate.Value;

            int ec = (int)numECrate.Value;

            int total = g + ec;

            txtTotalCrates.Text = $"G-Crates {g:N0}   +   E-Crates {ec:N0}   =   {total:N0} crates";
            UpdateCombiSplitUi(total);

            if (lnkCapDetails != null) lnkCapDetails.Visible = false;

            if (total == 0)

            {

                pnlRec.BackColor = Pal.C(241, 245, 249);

                lblRec.ForeColor = Pal.C(71, 85, 105);

                lblRec.Text = "ℹ️ Enter crate quantity to verify vehicle load capacity.";

                return;

            }



            Truck? selTruck = (cbTruck.SelectedIndex >= 0 && cbTruck.SelectedIndex < trucks.Count)

                ? trucks[cbTruck.SelectedIndex]

                : null;



            if (selTruck != null)

            {

                // Same check as Save (CapacityValidator): temperature, Dry / Cold / Combi matrix and truck maximum.

                ApplyLiveCapacityStatus(selTruck, total);

            }

            else if (!Features.On(Features.TruckSuggestion))

            {

                pnlRec.BackColor = Pal.C(241, 245, 249);

                lblRec.ForeColor = Pal.C(71, 85, 105);

                lblRec.Text = $"ℹ️ {total:N0} crates. Choose a truck that can carry them.";

            }

            else

            {

                // Nov 6 workflow: one compact line instead of the recommendation card.
                var rec = MJdatabase.GetTruckRecommendation(total, dtpDispatchDate.Value, "");
                lblRec.Text = rec.RecommendedTruck != null
                    ? $"ℹ️ Total {total:N0} crates  ·  suggested truck {rec.RecommendedTruck.PlateNumber} ({CapacityValidator.MatrixFor(rec.RecommendedTruck).TruckMax:N0} crates)"
                    : $"ℹ️ Total {total:N0} crates  ·  {rec.RecommendationMessage}";



                if (rec.IsMultiTruckNeeded)

                {

                    pnlRec.BackColor = Pal.C(254, 242, 242);

                    lblRec.ForeColor = Pal.C(185, 28, 28);

                }

                else

                {

                    pnlRec.BackColor = Pal.C(240, 253, 244);

                    lblRec.ForeColor = Pal.C(22, 101, 52);

                }

            }

        }



        private Panel? pnlRouteStoresBreakdown;
        private Label? lblRouteStoresHeader;
        private string storeBreakdownDescription = "add or edit stores, crates & columns";
        private DataGridView? gridRouteStores;
        private StoreEntryGuard? storeGuard;

        private Label? lblRouteStoresHint;

        private List<SpecialTripColumnDef> storeGridColumns = new List<SpecialTripColumnDef>();
        private bool isUpdatingRouteStores = false;

        // The route the store table was last filled from. Used to tell whether the user changed the store list.

        private DeliveryRoute? storeListRoute;

        private Button? btnSaveStoresToRoute;

        private readonly ToolTip storeTips = new ToolTip { InitialDelay = 400, ShowAlways = true };

        private static string NormStoreKey(string code, string name) =>

            $"{(code ?? "").Trim().ToUpperInvariant()}|{System.Text.RegularExpressions.Regex.Replace((name ?? "").Trim(), @"\s+", " ").ToUpperInvariant()}";

        private List<(string Code, string Name)> GetGridStoreList()

        {

            var list = new List<(string, string)>();

            if (gridRouteStores == null) return list;

            bool hasCode = gridRouteStores.Columns.Contains("colStoreCode");

            bool hasName = gridRouteStores.Columns.Contains("colStoreName");

            if (!hasName) return list;

            foreach (DataGridViewRow row in gridRouteStores.Rows)

            {

                if (row.IsNewRow) continue;

                string code = hasCode ? (row.Cells["colStoreCode"].Value?.ToString() ?? "").Trim() : "";

                string name = (row.Cells["colStoreName"].Value?.ToString() ?? "").Trim();

                if (code.Length == 0 && name.Length == 0) continue;

                list.Add((code, name));

            }

            return list;

        }

        /// <summary>Shows "Save stores to route" only when this order's store list no longer matches the saved route.</summary>

        private void UpdateSaveStoresToRouteButton()

        {

            if (btnSaveStoresToRoute == null || isUpdatingRouteStores) return;

            bool show = false;

            var route = storeListRoute;

            if (route != null && route.Id > 0)

            {

                var gridKeys = GetGridStoreList().Select(x => NormStoreKey(x.Code, x.Name)).ToList();

                var routeKeys = route.PlacesList.Select(p => { var (c, n) = DeliveryRoute.SplitStoreCodeAndName(p); return NormStoreKey(c, n); }).ToList();

                show = gridKeys.Count > 0 && !gridKeys.SequenceEqual(routeKeys);

            }

            if (btnSaveStoresToRoute.Visible != show) btnSaveStoresToRoute.Visible = show;

        }

        private void SaveStoresToRoute()

        {

            var route = storeListRoute;

            if (route == null || route.Id <= 0) return;

            var stores = GetGridStoreList();

            if (stores.Count == 0) return;

            var answer = MessageBox.Show(this.FindForm(),

                $"Update the saved route \"{route.RouteCode} - {route.RouteName}\" so it has these {stores.Count} stores?\n\n" +

                "Future orders on this route will start with this store list. Orders already saved are not changed.",

                "Save stores to route", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (answer != DialogResult.Yes) return;

            try

            {

                var fresh = MJdatabase.GetDeliveryRoutes(activeOnly: false).FirstOrDefault(r => r.Id == route.Id) ?? route;

                MJdatabase.SetRoutePlaces(fresh, stores.Select(x => string.IsNullOrWhiteSpace(x.Code) ? x.Name : $"{x.Code} - {x.Name}"));

                MJdatabase.SaveDeliveryRoute(fresh);

                storeListRoute = fresh;

                UpdateSaveStoresToRouteButton();

                ToastHelper.ShowToast(this.FindForm() ?? (Control)this, $"Route {fresh.RouteCode} now has {stores.Count} stores.", ToastType.Success);

            }

            catch (Exception ex)

            { ErrLog.Error("DispatchManagementView", ex); 

                MessageBox.Show(this.FindForm(), "Couldn't update the route: " + ex.Message, "Save stores to route", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            }

        }

        private static List<SpecialTripColumnDef> GetDefaultStoreBreakdownColumns()
        {
            // Nov 6 workflow: Store Code, Store Name, DR No., G-Crates, E-Crates. The route and the per-store total stay in the
            // table (hidden): the store checks need the route, and the total is still worked out.
            return new List<SpecialTripColumnDef>
            {
                new SpecialTripColumnDef { Key = "colStoreCode", HeaderText = "Store Code", Width = 80, IsFill = false },
                new SpecialTripColumnDef { Key = "colStoreName", HeaderText = "Store Name", Width = 155, IsFill = true },
                new SpecialTripColumnDef { Key = "colStoreRoute", HeaderText = "Route", Width = 120, IsFill = false },
                new SpecialTripColumnDef { Key = "colDrNumbers", HeaderText = "DR No.", Width = 110, IsFill = false },
                new SpecialTripColumnDef { Key = "colGCrate", HeaderText = "G-Crates ✎", Width = 72, IsFill = false },
                new SpecialTripColumnDef { Key = "colECrate", HeaderText = "E-Crates ✎", Width = 72, IsFill = false },
                new SpecialTripColumnDef { Key = "colStoreTotal", HeaderText = "Total", Width = 58, IsFill = false }
            };
        }

        private Label CreateStepHeaderBanner(string title)
        {
            return new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 22,
                BackColor = Pal.C(239, 246, 255),

                ForeColor = Pal.C(30, 58, 138),

                Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                Margin = new Padding(0, 3, 0, 2)
            };
        }

        private void InitializeMultiDropControls()
        {
            chkMultiDrop = new CheckBox { Visible = false };
            pnlMultiDropBuilder = new Panel { Visible = false };
            lstStops = new ListBox { Visible = false };
            lblMultiDropSummary = new Label { Visible = false };

            lblDest.Text = "Delivery Route:";
            lblGCrate.Text = "Total G-Crates:";
            lblECrate.Text = "Total E-Crates:";

            storeGridColumns = GetDefaultStoreBreakdownColumns();

            pnlRouteStoresBreakdown = new Panel
            {
                Dock = DockStyle.Top,
                Height = Dpi(260),

                Visible = true,
                BackColor = Pal.C(248, 250, 252),

                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(Dpi(10), Dpi(8), Dpi(10), Dpi(10)),

                Margin = new Padding(0, 2, 0, 4)
            };

            var pnlRouteStoreHeaderBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = Dpi(46),

                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 4)
            };
            pnlRouteStoreHeaderBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlRouteStoreHeaderBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlRouteStoreHeaderBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlRouteStoreHeaderBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            lblRouteStoresHeader = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 10.5F),

                ForeColor = Pal.C(15, 23, 42),

                Text = "🏪 Stores & crates for this order",

                Cursor = Cursors.Hand
            };
            lblRouteStoresHeader.DoubleClick += (s, e) => OpenStoreBreakdownTableEditor();

            var btnAddStoreRow = new Button
            {
                Text = "＋ Add store",
                Height = Dpi(34),
                AutoSize = true,
                Padding = new Padding(Dpi(10), 0, Dpi(10), 0),
                Font = new Font("Segoe UI Semibold", 9F),
                BackColor = AppTheme.Surface,

                ForeColor = AppTheme.TextPrimary,

                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 8, 2)
            };
            UIThemeHelper.SetButtonRole(btnAddStoreRow, "secondary"); // same look as the other form buttons
            tipRouteStores.SetToolTip(btnAddStoreRow, "Add another store of the same client company to this order");
            btnAddStoreRow.Click += (s, e) => AddStoreBreakdownRow();

            btnEditStoreTable = new Button

            {
                Text = "✎ Edit",
                Height = Dpi(34),

                AutoSize = true,
                Padding = new Padding(Dpi(8), 0, Dpi(8), 0),

                Font = new Font("Segoe UI Semibold", 9F),

                BackColor = Pal.C(22, 163, 74),

                ForeColor = Pal.White,

                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 4, 2)
            };
            btnEditStoreTable.FlatAppearance.BorderSize = 0;
            btnEditStoreTable.Click += (s, e) => OpenStoreBreakdownTableEditor();

            storeTips.SetToolTip(btnEditStoreTable, "Change this table's columns (Settings → Features can hide this).");

            btnSaveStoresToRoute = new Button

            {
                Text = "💾 Save stores to route",

                Height = Dpi(34),

                AutoSize = true,
                Padding = new Padding(Dpi(8), 0, Dpi(8), 0),

                Font = new Font("Segoe UI Semibold", 9F),

                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 2),

                Visible = false

            };
            UIThemeHelper.SetButtonRole(btnSaveStoresToRoute, "secondary");

            btnSaveStoresToRoute.Click += (s, e) => SaveStoresToRoute();

            storeTips.SetToolTip(btnSaveStoresToRoute, "Copy this order's store list into the saved route, so future orders on this route start with the same stores.");

            pnlRouteStoreHeaderBar.Controls.Add(lblRouteStoresHeader, 0, 0);
            pnlRouteStoreHeaderBar.Controls.Add(btnAddStoreRow, 1, 0);
            pnlRouteStoreHeaderBar.Controls.Add(btnEditStoreTable, 2, 0);
            pnlRouteStoreHeaderBar.Controls.Add(btnSaveStoresToRoute, 3, 0);

            lblRouteStoresHint = new Label

            {

                Dock = DockStyle.Top,

                Height = Dpi(22),

                TextAlign = ContentAlignment.MiddleLeft,

                Font = new Font("Segoe UI", 8.5F),

                ForeColor = Pal.C(100, 116, 139),

                Text = StoreHintText,

                AutoEllipsis = true

            };

            gridRouteStores = new LookupGrid

            {
                Dock = DockStyle.Fill,
                BackgroundColor = Pal.White,

                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ScrollBars = ScrollBars.Horizontal,

                SelectionMode = DataGridViewSelectionMode.CellSelect,
                EditMode = DataGridViewEditMode.EditOnEnter,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = Dpi(LargePrint.Px(36, 30)),
                RowTemplate = { Height = Dpi(LargePrint.Px(40, 32)) },
                Font = LargePrint.On ? LargePrint.Bold(12F, 9F) : new Font("Segoe UI", 9F),
                GridColor = AppTheme.BorderStrong,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                EnableHeadersVisualStyles = false
            };
            gridRouteStores.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(219, 234, 254);

            gridRouteStores.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(30, 58, 138);

            gridRouteStores.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);

            RebuildStoreBreakdownGridColumns();

            InputGuards.DigitsOnlyColumns(gridRouteStores, "colECrate", "colGCrate");

            storeGuard = new StoreEntryGuard((LookupGrid)gridRouteStores, "colStoreCode", "colStoreName", "colStoreRoute",

                () => cbClient.Text, () => storeListRoute != null ? $"{storeListRoute.RouteCode} - {storeListRoute.RouteName}" : cbDestination.Text, ShowStoreWarning);

            storeGuard.RowHasData = row => CratesOf(row) > 0;
            storeGuard.UseOrderRoute = true;
            VisibleChanged += (s, e) => SetFullScreen(Visible && pnlForm.Visible); // leaving the page ends full screen // Nov 6: every row shows the route picked in Delivery Route; the store list has no Route column

            storeGuard.RowChanged += row => { RecalculateTotalsFromRouteStoresGrid(); UpdateSaveStoresToRouteButton(); };

            gridRouteStores.RowsAdded += (s, e) => SizeStoreBreakdown(gridRouteStores.Rows.Count);
            // Rule 16d: rows added by other paths (table editor, draft) get their type from the route once their values are in.
            gridRouteStores.RowsAdded += (s, e) => { if (IsHandleCreated) BeginInvoke(new Action(ClassifyUnsetStoreRows)); };
            gridRouteStores.CellFormatting += GridRouteStores_RowTypeFormatting;
            gridRouteStores.CellEnter += (s, e) =>
            {
                // Route rows: code and name are locked, so focus goes straight to the crate cells.
                if (e.RowIndex < 0 || !IsRouteStoreRow(gridRouteStores.Rows[e.RowIndex])) return;
                string cn = gridRouteStores.Columns[e.ColumnIndex].Name;
                if ((cn == "colStoreCode" || cn == "colStoreName") && gridRouteStores.Columns.Contains("colECrate"))
                    BeginInvoke(new Action(() => { try { gridRouteStores.CurrentCell = gridRouteStores.Rows[e.RowIndex].Cells["colECrate"]; } catch (Exception ex) { ErrLog.Swallowed(ex); } }));
            };
            gridRouteStores.EditingControlShowing += (s, e) =>
            {
                // Crate cells: the content is selected on focus, so typing replaces the 0.
                string cn = gridRouteStores.CurrentCell?.OwningColumn?.Name ?? "";
                if ((cn == "colECrate" || cn == "colGCrate") && e.Control is TextBox tb) BeginInvoke(new Action(() => { try { tb.SelectAll(); } catch (Exception ex) { ErrLog.Swallowed(ex); } }));
            };

            gridRouteStores.RowsRemoved += (s, e) => SizeStoreBreakdown(gridRouteStores.Rows.Count);

            gridRouteStores.CellValueChanged += (s, e) =>
            {
                if (isBindingForm || isUpdatingRouteStores || e.RowIndex < 0) return;
                RecalculateTotalsFromRouteStoresGrid();
            };
            gridRouteStores.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (gridRouteStores.IsCurrentCellDirty)
                {
                    gridRouteStores.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            gridRouteStores.CellContentClick += (s, e) =>
            {
                if (gridRouteStores == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (gridRouteStores.Columns[e.ColumnIndex].Name == "colDelStore")
                {
                string label = gridRouteStores.Columns.Contains("colStoreName") ? gridRouteStores.Rows[e.RowIndex].Cells["colStoreName"].Value?.ToString() ?? "" : "";
                if (IsRouteStoreRow(gridRouteStores.Rows[e.RowIndex]))
                {
                    // Rule 16d (approved Nov 6): a locked route store can be removed only with a reason (at least 10 characters), which is logged.
                    string code0 = gridRouteStores.Columns.Contains("colStoreCode") ? gridRouteStores.Rows[e.RowIndex].Cells["colStoreCode"].Value?.ToString() ?? "" : "";
                    string? why = PromptRouteRowRemovalReason(label.Length > 0 ? label : code0);
                    if (why == null) return;
                    try { MJdatabase.LogAuditTrail("UPDATE", "Deliveries", selectedDelivery?.Id ?? 0, selectedDelivery?.DispatchNumber ?? "(new order)", "RouteStoreRemoved", $"{code0} - {label}", "", why); } catch (Exception ax) { ErrLog.Swallowed(ax); }
                    gridRouteStores.Rows.RemoveAt(e.RowIndex);
                    RecalculateTotalsFromRouteStoresGrid();
                    return;
                }
                if (MessageBox.Show(this.FindForm(), $"Are you sure you want to remove {(label.Length > 0 ? "\"" + label + "\"" : "this store")} from this dispatch's store table?", "Remove store row", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

                    gridRouteStores.Rows.RemoveAt(e.RowIndex);
                    RecalculateTotalsFromRouteStoresGrid();
                }
            };
            gridRouteStores.ColumnHeaderMouseDoubleClick += (s, e) => OpenStoreBreakdownTableEditor();

            // Settings → "Show Column Edit Header in Edit Store Table": the Edit button and the ✎ marks come and go live.

            Features.Bind(gridRouteStores, ApplyStoreEditHeaderSetting, Features.StoreTableEditHeader);

            pnlRouteStoresBreakdown.Controls.Add(gridRouteStores);
            pnlRouteStoresBreakdown.Controls.Add(lblRouteStoresHint);

            pnlRouteStoresBreakdown.Controls.Add(pnlRouteStoreHeaderBar);
            gridRouteStores.RowsAdded += (s, e) => UpdateSaveStoresToRouteButton();

            gridRouteStores.RowsRemoved += (s, e) => UpdateSaveStoresToRouteButton();

            gridRouteStores.CellValueChanged += (s, e) => UpdateSaveStoresToRouteButton();

            pnlFormBody.Controls.Add(pnlRouteStoresBreakdown);

            int destIdx = pnlFormBody.Controls.IndexOf(cbDestination);
            if (destIdx >= 0)
            {
                pnlFormBody.Controls.SetChildIndex(pnlRouteStoresBreakdown, destIdx);
            }

            var lblStep1 = CreateStepHeaderBanner("① STEP 1: ROUTE & STORE CRATE LOADING");
            pnlFormBody.Controls.Add(lblStep1);
            pnlFormBody.Controls.SetChildIndex(lblStep1, pnlFormBody.Controls.Count - 1);

            var lblStep2 = CreateStepHeaderBanner("② STEP 2: TRUCK & CREW ASSIGNMENT");
            pnlFormBody.Controls.Add(lblStep2);
            int recIdx = pnlFormBody.Controls.IndexOf(pnlRec);
            if (recIdx >= 0)
            {
                pnlFormBody.Controls.SetChildIndex(lblStep2, recIdx + 1);
            }

            var lblStep3 = CreateStepHeaderBanner("③ STEP 3: SCHEDULE & DISPATCH STATUS");
            pnlFormBody.Controls.Add(lblStep3);
            int dtIdx = pnlFormBody.Controls.IndexOf(tblDateTime);
            if (dtIdx >= 0)
            {
                pnlFormBody.Controls.SetChildIndex(lblStep3, dtIdx + 1);
                ApplyTwoColumnFormLayout();
            }
        }

        private void RebuildStoreBreakdownGridColumns()
        {
            if (gridRouteStores == null) return;
            gridRouteStores.Columns.Clear();
            if (storeGridColumns == null || storeGridColumns.Count == 0)
            {
                storeGridColumns = GetDefaultStoreBreakdownColumns();
            }

            // Nov 6 workflow: the store table carries each store's DR number again (the trip report shows the same DR).
            // Only invoice-number columns are left out (they belong to billing).
            storeGridColumns = storeGridColumns.Where(c => c.Key == "colDrNumbers" || !IsDrOrInvoiceColumn(c)).ToList();

            if (storeGridColumns.Count == 0) storeGridColumns = GetDefaultStoreBreakdownColumns();

            // Store code, store name and route always stay in the table (the store checks need them).

            var defaults = GetDefaultStoreBreakdownColumns();

            foreach (var key in new[] { "colStoreCode", "colStoreName", "colStoreRoute", "colDrNumbers" })
            {
                if (storeGridColumns.Any(c => c.Key == key)) continue;
                string after = key switch { "colStoreName" => "colStoreCode", "colStoreRoute" => "colStoreName", _ => "colStoreRoute" };
                int at = key == "colStoreCode" ? 0 : Math.Min(storeGridColumns.Count, storeGridColumns.FindIndex(c => c.Key == after) + 1);

                storeGridColumns.Insert(Math.Max(0, at), defaults.First(c => c.Key == key).Clone());

            }

            // Rule 16d: row type badge first, and a hidden flag (true = store from the order's route, locked).
            var colType = new DataGridViewTextBoxColumn { Name = "colRowType", HeaderText = "Row Type", FillWeight = 150, MinimumWidth = Dpi(LargePrint.Px(170, 112)), ReadOnly = true };
            colType.DefaultCellStyle.Font = LargePrint.On ? LargePrint.Bold(10F, 8.25F) : new Font("Segoe UI Semibold", 8.25F);
            colType.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            gridRouteStores.Columns.Add(colType);
            gridRouteStores.Columns.Add(new DataGridViewCheckBoxColumn { Name = "colBaseline", Visible = false, ReadOnly = true });
            foreach (var colDef in storeGridColumns)
            {
            bool isNumeric = colDef.Key == "colECrate" || colDef.Key == "colGCrate" || colDef.Key == "colStoreTotal";
                // Code and name are typed (or picked) and checked against the client's stores; the route comes with the

                // store and the total is calculated, so those two can't be typed.

                bool isTotal = colDef.Key == "colStoreTotal" || colDef.Key == "colStoreRoute";

                var col = new DataGridViewTextBoxColumn
                {
                    Name = colDef.Key,
                    HeaderText = StoreHeaderText(colDef.HeaderText),

                    FillWeight = colDef.IsFill ? 140 : 75,
                    MinimumWidth = 50,
                    ReadOnly = isTotal,
                    DefaultCellStyle =
                    {
                        Alignment = isNumeric ? DataGridViewContentAlignment.MiddleRight : DataGridViewContentAlignment.MiddleLeft,
                        BackColor = isTotal ? Pal.C(241, 245, 249) : Pal.C(254, 252, 232),

                        Font = LargePrint.On ? (isNumeric ? LargePrint.Bold(13F, 9F) : LargePrint.Bold(12F, 9F)) : (isNumeric || isTotal ? new Font("Segoe UI Semibold", 9F) : new Font("Segoe UI", 9F))
                    }
                };
                gridRouteStores.Columns.Add(col);
            }

            var colDel = new DataGridViewButtonColumn
            {
                Name = "colDelStore",
                HeaderText = "Action",
                Text = "",
                UseColumnTextForButtonValue = false, // text per row: "🗑 Remove" on added rows, "Remove…" (with a reason) on route rows
                FillWeight = 60,
                MinimumWidth = Dpi(96),
                FlatStyle = FlatStyle.Flat
            };
            colDel.DefaultCellStyle.ForeColor = Color.White;
            colDel.DefaultCellStyle.BackColor = AppTheme.Danger;
            colDel.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5F);
            gridRouteStores.Columns.Add(colDel);
            foreach (var hidden in new[] { "colStoreRoute", "colStoreTotal" })
                if (gridRouteStores.Columns.Contains(hidden)) gridRouteStores.Columns[hidden]!.Visible = false;
        }

        private static bool IsDrOrInvoiceColumn(SpecialTripColumnDef c)

        {

            if (string.Equals(c.Key, "colDrNumbers", StringComparison.OrdinalIgnoreCase)) return true;

            string h = (c.HeaderText ?? "").Replace("✎", "").Trim();

            return System.Text.RegularExpressions.Regex.IsMatch(h, @"^(DR|D\.R\.)\b|\bInv(oice)?\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        }

        private void AddStoreBreakdownRow()
        {
            if (gridRouteStores == null || pnlRouteStoresBreakdown == null) return;
            if (cbClient.Text.Trim().Length == 0)

            {

                ShowStoreWarning(StoreDirectory.NeedClientMessage);

                ToastHelper.ShowToast(this.FindForm() ?? (Control)this, StoreDirectory.NeedClientMessage, ToastType.Warning);

                cbClient.Focus();

                return;

            }

            pnlRouteStoresBreakdown.Visible = true;
            gridRouteStores.EndEdit();

            // A new row is completely empty: type or pick the store code or name; the rest fills in from Master Data.

            int rIdx = gridRouteStores.Rows.Add();
            SetStoreRowType(gridRouteStores.Rows[rIdx], false); // rule 16d: an added company store (editable, removable)
            SizeStoreBreakdown(gridRouteStores.Rows.Count);

            if (rIdx >= 0 && gridRouteStores.Columns.Contains("colStoreName"))

            {

                try

                {

                    gridRouteStores.CurrentCell = gridRouteStores.Rows[rIdx].Cells["colStoreName"];

                    gridRouteStores.BeginEdit(true);

                }

                catch (Exception ex) { ErrLog.Swallowed(ex); }

            }

        }

        /// <summary>The client's stores from Master Data (shared with the store checks, so a store just registered is known here too).</summary>

        private List<ClientStore> ClientStoresForLookup()

        {

            if (storeGuard != null) return storeGuard.Stores();

            string client = cbClient.Text.Trim();

            try { return client.Length == 0 ? new List<ClientStore>() : MJdatabase.GetClientStores(client); }

            catch (Exception ex) { ErrLog.Swallowed(ex); return new List<ClientStore>(); }

        }

        /// <summary>

        /// The route of this order, worked out from the stores in the table (the route box is no longer shown).

        /// Picks the client's route that contains the most of these stores; otherwise describes the stops.

        /// </summary>

        private string ResolveDestinationFromStores(string current)

        {

            var stores = GetGridStoreList();

            if (stores.Count == 0) return current;

            string client = cbClient.Text.Trim();

            try

            {

                var routes = client.Length == 0 ? new List<DeliveryRoute>() : MJdatabase.GetDeliveryRoutesByClient(client, activeOnly: false);

                DeliveryRoute? best = null;

                int bestHits = 0;

                foreach (var r in routes)

                {

                    var keys = r.PlacesList.Select(p => { var (c, n) = DeliveryRoute.SplitStoreCodeAndName(p); return (C: (c ?? "").Trim(), N: (n ?? "").Trim()); }).ToList();

                    int hits = stores.Count(st => keys.Any(k => (k.C.Length > 0 && k.C.Equals(st.Code, StringComparison.OrdinalIgnoreCase)) || (k.N.Length > 0 && k.N.Equals(st.Name, StringComparison.OrdinalIgnoreCase))));

                    if (hits > bestHits) { best = r; bestHits = hits; }

                }

                if (best != null && bestHits * 2 >= stores.Count) return $"{best.RouteCode} - {best.RouteName}";

            }

            catch (Exception ex) { ErrLog.Swallowed(ex); }

            string first = stores[0].Name.Length > 0 ? stores[0].Name : stores[0].Code;

            return stores.Count == 1 ? first : $"{first} (+{stores.Count - 1} stop{(stores.Count - 1 == 1 ? "" : "s")})";

        }

        /// <summary>Every store row must be one of the selected client's registered stores, listed once (rows with crates need a store).</summary>

        private bool ValidateStoreRows()

        {

            if (gridRouteStores == null || storeGuard == null) return true;
            // Rule 16d: an added row can't be left blank.
            foreach (DataGridViewRow r in gridRouteStores.Rows)
            {
                if (IsRouteStoreRow(r)) continue;
                string c0 = gridRouteStores.Columns.Contains("colStoreCode") ? (r.Cells["colStoreCode"].Value?.ToString() ?? "").Trim() : "";
                string n0 = gridRouteStores.Columns.Contains("colStoreName") ? (r.Cells["colStoreName"].Value?.ToString() ?? "").Trim() : "";
                if (c0.Length == 0 && n0.Length == 0)
                {
                    try { gridRouteStores.CurrentCell = r.Cells["colStoreName"]; gridRouteStores.FirstDisplayedScrollingRowIndex = r.Index; } catch (Exception ex) { ErrLog.Swallowed(ex); }
                    MessageBox.Show(this.FindForm(), $"Row {r.Index + 1} is an added store with no store picked. Pick a store of this client, or remove the row, then save.", "Added store is blank", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            return storeGuard.ValidateForSave(this.FindForm());

        }

        /// <summary>Crates typed on a store row.</summary>

        private int CratesOf(DataGridViewRow row)

        {

            if (gridRouteStores == null) return 0;

            int Num(string col) => gridRouteStores.Columns.Contains(col) && int.TryParse(row.Cells[col].Value?.ToString(), out int n) ? Math.Max(0, n) : 0;

            return Num("colECrate") + Num("colGCrate");

        }

        private const string StoreHintText = "Type a store code or name and pick it from the list (Code | Name | Route). Stores that aren't registered can be added from the warning.";

        /// <summary>Shows a warning in place of the hint under the store table (null puts the hint back).</summary>

        private void ShowStoreWarning(string? warning)

        {

            if (lblRouteStoresHint == null) return;

            lblRouteStoresHint.Text = warning == null ? StoreHintText : "⚠ " + warning;

            lblRouteStoresHint.ForeColor = warning == null ? Pal.C(100, 116, 139) : AppTheme.DangerText;

            lblRouteStoresHint.Font = new Font(warning == null ? "Segoe UI" : "Segoe UI Semibold", 8.5F);

        }

        private void SizeStoreBreakdown(int rows)

        {

            if (pnlRouteStoresBreakdown == null || gridRouteStores == null) return;

            // The table grows with its rows and the whole form scrolls (no second scrollbar inside the table).

            int chrome = pnlRouteStoresBreakdown.Padding.Vertical + Dpi(46) + Dpi(22) + Dpi(12);

            int rowH = gridRouteStores.RowTemplate.Height;

            int want = chrome + gridRouteStores.ColumnHeadersHeight + Math.Max(2, rows) * rowH + rowH / 2;

            pnlRouteStoresBreakdown.Height = Math.Max(Dpi(180), want);

        }

        private Button? btnEditStoreTable;

        private static bool StoreEditHeaderOn => Features.On(Features.StoreTableEditHeader);

        /// <summary>A column header as shown: without the ✎ mark when the column edit header is switched off.</summary>

        private static string StoreHeaderText(string? header)

        {

            string h = header ?? "";

            return StoreEditHeaderOn ? h : h.Replace("✎", "").Trim();

        }

        /// <summary>Shows or hides the column edit header (Edit button, ✎ marks, double-click to edit) on this store table.</summary>

        private void ApplyStoreEditHeaderSetting()

        {

            bool on = StoreEditHeaderOn;

            if (btnEditStoreTable != null) btnEditStoreTable.Visible = on;

            if (lblRouteStoresHeader != null) lblRouteStoresHeader.Cursor = on ? Cursors.Hand : Cursors.Default;

            if (gridRouteStores == null) return;

            foreach (var def in storeGridColumns)

                if (gridRouteStores.Columns.Contains(def.Key)) gridRouteStores.Columns[def.Key]!.HeaderText = StoreHeaderText(def.HeaderText);

        }

        private void OpenStoreBreakdownTableEditor()
        {
            if (gridRouteStores == null || lblRouteStoresHeader == null || pnlRouteStoresBreakdown == null) return;
            if (!StoreEditHeaderOn) return; // switched off in Settings

            gridRouteStores.EndEdit();

            var currentRows = new List<Dictionary<string, string>>();
            foreach (DataGridViewRow r in gridRouteStores.Rows)
            {
                if (r.IsNewRow) continue;
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in storeGridColumns)
                {
                    if (gridRouteStores.Columns.Contains(c.Key))
                    {
                        dict[c.Key] = r.Cells[c.Key].Value?.ToString() ?? string.Empty;
                    }
                }
                currentRows.Add(dict);
            }

            using (var dlg = new SpecialTripTableEditorDialog(
                lblRouteStoresHeader.Text,
                storeBreakdownDescription,
                storeGridColumns,
                currentRows,
                cbDepartureTime.Text))
            {
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
                {
                    isUpdatingRouteStores = true;
                    try
                    {
                        lblRouteStoresHeader.Text = dlg.EditedTableTitle;
                        storeBreakdownDescription = dlg.EditedTableDescription;
                        storeGridColumns = dlg.EditedColumns.Select(c => c.Clone()).ToList();
                        RebuildStoreBreakdownGridColumns();

                        gridRouteStores.Rows.Clear();
                        foreach (var dict in dlg.EditedRows)
                        {
                            int rIdx = gridRouteStores.Rows.Add();
                            var row = gridRouteStores.Rows[rIdx];
                            foreach (var c in storeGridColumns)
                            {
                                if (gridRouteStores.Columns.Contains(c.Key))
                                {
                                    row.Cells[c.Key].Value = dict.TryGetValue(c.Key, out string? v) ? v : string.Empty;
                                }
                            }
                        }
                        pnlRouteStoresBreakdown.Visible = true;
                        SizeStoreBreakdown(gridRouteStores.Rows.Count);

                    }
                    finally
                    {
                        isUpdatingRouteStores = false;
                    }
                    // Rows typed in the table editor go through the same store checks.

                    storeGuard?.RevalidateAll(showToast: true);

                    RecalculateTotalsFromRouteStoresGrid();
                    UpdateSaveStoresToRouteButton();

                }
            }
        }



        private Button btnAddRoute = null!;

        /// <summary>The Combi cold / dry split row (shown only for Combi cargo).</summary>

        private TableLayoutPanel? pnlRowCombi;

        private ClientInfoStrip? clientStrip;

        private void ApplyTwoColumnFormLayout()
        {
            clientStrip ??= new ClientInfoStrip();

            clientStrip.ClientRegistered -= OnClientRegisteredFromStrip;

            clientStrip.ClientRegistered += OnClientRegisteredFromStrip;

            clientStrip.ReturnTo = () =>

            {

                if (!pnlForm.Visible) return null;

                SaveDraftQuietly();

                return (OrderFormName, ReturnToOrderForm);

            };

            if (pnlFormBody == null) return;
            pnlFormBody.SuspendLayout();
            pnlFormBody.Controls.Clear();

            btnAddRoute = new Button
            {
                Text = "+ New route",

                Dock = DockStyle.Right,
                Width = Dpi(112),

                BackColor = Pal.C(37, 99, 235),

                ForeColor = Pal.White,

                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F),

                Cursor = Cursors.Hand
            };
            btnAddRoute.FlatAppearance.BorderSize = 0;
            btnAddRoute.Click += BtnAddRoute_Click;
            btnAddRoute.Visible = false; // Nov 6: routes are made in Master Data → Clients → Delivery routes, not in the order form

            storeTips.SetToolTip(btnAddRoute, "Create a brand-new delivery route (code, name and its stores). Pick an existing route from the list instead if it already exists.");

            var pnlRowOrderClient = CreateTwoColumnRow(
                lblDispatchNumber, txtDispatchNumber,
                lblClient, cbClient
            );

            Panel pnlRouteContainer = new Panel { Dock = DockStyle.Fill };
            cbDestination.Dock = DockStyle.Fill;
            pnlRouteContainer.Controls.Add(cbDestination);
            pnlRouteContainer.Controls.Add(btnAddRoute);

            Panel pnlCargoContainer = new Panel { Dock = DockStyle.Fill };
            cbCargoType.Dock = DockStyle.Fill;
            // Nov 6 workflow: no cargo / temperature dropdown on the dispatch (the truck's own type is saved, and "Combi" is a
            // note in the remarks). The Peak Season tick keeps its place.
            cbCargoType.Visible = false;
            lblCargoType.Text = "Season:";
            chkIsPeak.Dock = DockStyle.Fill;
            chkIsPeak.Text = "Peak Season";

            pnlCargoContainer.Controls.Add(cbCargoType);
            pnlCargoContainer.Controls.Add(chkIsPeak);

            // Delivery Route (pick a route to fill the store table, or "+ New route") next to the cargo type. The Combi

            // cold / dry split has its own row, shown only for Combi cargo.

            InitCapacityUi();

            lblDest.Text = "Delivery Route:";

            lblDest.Visible = true;

            pnlRouteContainer.Visible = true;

            var pnlRowRouteCargo = CreateTwoColumnRow(
                lblDest, pnlRouteContainer,

                lblCargoType, pnlCargoContainer

            );

            pnlRowCombi = CreateTwoColumnRow(

                lblCombiSplit, pnlCombiSplit,

                new Label { Visible = false }, new Panel { Visible = false }

            );

            pnlRowCombi.Visible = IsCombiCargo && !CargoRouting.IsSpecial(cbCargoType.Text);

            var pnlRowTruckDriver = CreateTwoColumnRow(
                lblTr, cbTruck,
                lblDr, cbDriver
            );

            var pnlRowDateTime = CreateTwoColumnRow(
                lblDd, dtpDispatchDate,
                lblDepartureTime, pnlTimeRow
            );
            lblDd.Text = "Dispatch Date & Time:";
            lblDepartureTime.Text = "Departure Time:";

            var pnlRowStatus = CreateTwoColumnRow(
                lblSt, cbStatus,
                new Label { Visible = false }, new Panel { Visible = false }
            );
            lblSt.Text = "Dispatch Status:";

            Panel pnlAidesSection = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 4)
            };
            lblAidesSummary.Dock = DockStyle.Top;
            flowAidesModule.Dock = DockStyle.Top;
            pnlAidesSearchRow.Dock = DockStyle.Top;
            lblAi.Dock = DockStyle.Top;
            lblAi.Font = new Font("Segoe UI Semibold", 9F);

            lblAi.ForeColor = AppTheme.TextSecondary;

            lblAi.Padding = new Padding(0, Dpi(4), 0, Dpi(4));

            pnlAidesSection.Controls.Add(lblAidesSummary);
            pnlAidesSection.Controls.Add(flowAidesModule);
            pnlAidesSection.Controls.Add(pnlAidesSearchRow);
            pnlAidesSection.Controls.Add(lblAi);

            var card1 = CreateSectionCard(1, "Route & store crates", "Who the order is for, where it goes and how many crates are loaded.",

                pnlRowOrderClient, clientStrip, pnlRowRouteCargo, pnlRowCombi, pnlRouteStoresBreakdown, tblCrates, pnlRec);

            var card2 = CreateSectionCard(2, "Truck & crew", "Assign the truck, the driver and the helpers for this trip.",

                BuildAddResourceLinks(), pnlRowTruckDriver, pnlAidesSection);

            var card3 = CreateSectionCard(3, "Schedule & status", "When the truck leaves and where the order stands.",

                pnlRowDateTime, pnlRowStatus);

            pnlFormBody.Controls.Add(card3);

            pnlFormBody.Controls.Add(CreateSectionSpacer());

            pnlFormBody.Controls.Add(card2);

            pnlFormBody.Controls.Add(CreateSectionSpacer());

            pnlFormBody.Controls.Add(card1);

            pnlFormBody.ResumeLayout(true);
        }

        /// <summary>

        /// "+ Add a truck / driver / aide" links above the truck and crew pickers. They open the new-record form in

        /// Resources; saving it (or clicking its back button) comes straight back to this order, with everything kept.

        /// </summary>

        private Control BuildAddResourceLinks()

        {

            var row = new FlowLayoutPanel

            {

                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true,

                FlowDirection = FlowDirection.LeftToRight, BackColor = AppTheme.Surface,

                Padding = new Padding(0, 0, 0, Dpi(6)), Margin = new Padding(0)

            };

            var lead = new Label { Text = "Not in the list?", AutoSize = true, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9F), Margin = new Padding(0, Dpi(3), Dpi(8), 0) };

            row.Controls.Add(lead);

            LinkLabel Link(string text, string tip, Action open)

            {

                var l = new LinkLabel

                {

                    Text = text, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), LinkBehavior = LinkBehavior.HoverUnderline,

                    LinkColor = AppTheme.AccentText, ActiveLinkColor = AppTheme.AccentText, Margin = new Padding(0, Dpi(3), Dpi(18), 0), Cursor = Cursors.Hand

                };

                tipRouteStores.SetToolTip(l, tip);

                l.LinkClicked += (s, e) => open();

                row.Controls.Add(l);

                return l;

            }

            var lnkTruck = Link("＋ Add a truck", "Opens Resources → Trucks with a new truck form. Save it to come back here.", () => OpenAddResource("truck"));

            var lnkDriver = Link("＋ Add a driver", "Opens Crew Management with a new driver form. Save it to come back here.", () => OpenAddResource("driver"));

            var lnkAide = Link("＋ Add an aide", "Opens Crew Management with a new aide / helper form. Save it to come back here.", () => OpenAddResource("aide"));

            void Apply()

            {

                bool trucksOk = Permissions.Can("Resources", "Trucks"), crewOk = Permissions.Can("Resources", "Crew");

                lnkTruck.Visible = trucksOk;

                lnkDriver.Visible = lnkAide.Visible = crewOk;

                row.Visible = trucksOk || crewOk; // (reading .Visible back is false until the form is on screen)

            }

            Apply();

            VisibleChanged += (s, e) => { if (Visible) Apply(); };

            return row;

        }

        private string OrderFormName => selectedDelivery != null ? "Edit Dispatch Order" : "New Dispatch Order";

        /// <summary>Back to this order form after a trip to another screen (client profile, trucks, crew).</summary>

        private void ReturnToOrderForm()

        {

            skipDraftPromptOnce = true;

            MainForm.Instance?.NavigateToOperations("Dispatch");

        }

        private bool skipDraftPromptOnce;

        // What was being added on the side trip, and who already existed, so the new one can be picked on return.

        private string? pendingAdd;

        private HashSet<int> idsBeforeAdd = new HashSet<int>();

        private void OpenAddResource(string what)

        {

            if (!pnlForm.Visible) return;

            SaveDraftQuietly();

            pendingAdd = what;

            idsBeforeAdd = what == "truck" ? allTrucks.Select(t => t.Id).ToHashSet() : allDrivers.Select(c => c.Id).Concat(allAides.Select(c => c.Id)).ToHashSet();

            if (what == "truck") MainForm.Instance?.OpenNewTruckFrom(OrderFormName, ReturnToOrderForm);

            else MainForm.Instance?.OpenNewCrewFrom(what == "driver", OrderFormName, ReturnToOrderForm);

        }

        /// <summary>After "+ Add a truck / driver / aide": picks the one that was just saved (when it is free on this date).</summary>

        private void PickJustAdded()

        {

            string? what = pendingAdd;

            pendingAdd = null;

            if (what == null) return;

            if (what == "truck")

            {

                int i = trucks.FindIndex(t => !idsBeforeAdd.Contains(t.Id));

                if (i >= 0) cbTruck.SelectedIndex = i;

            }

            else if (what == "driver")

            {

                int i = drivers.FindIndex(c => !idsBeforeAdd.Contains(c.Id));

                if (i >= 0) cbDriver.SelectedIndex = i;

            }

            else

            {

                var added = aides.FirstOrDefault(c => !idsBeforeAdd.Contains(c.Id));

                if (added != null && !selectedAideIds.Contains(added.Id)) { selectedAideIds.Add(added.Id); RenderAidesModule(); }

            }

        }

        private void SaveDraftQuietly()

        {

            try { SaveCurrentDraft(); } catch (Exception ex) { ErrLog.Swallowed(ex); }

        }

        private Panel CreateSectionSpacer() => new Panel { Dock = DockStyle.Top, Height = Dpi(16), BackColor = AppTheme.Canvas };

        private Panel CreateSectionCard(int step, string title, string subtitle, params Control?[] contentTopToBottom)

        {

            var card = new Panel

            {

                Dock = DockStyle.Top,

                AutoSize = true,

                AutoSizeMode = AutoSizeMode.GrowAndShrink,

                BackColor = AppTheme.Surface,

                Padding = new Padding(Dpi(24), Dpi(18), Dpi(24), Dpi(22)),

                Margin = new Padding(0)

            };

            card.Paint += (s, e) =>

            {

                using var pen = new Pen(AppTheme.Border);

                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);

            };

            card.Resize += (s, e) => card.Invalidate();

            for (int i = contentTopToBottom.Length - 1; i >= 0; i--)

            {

                var c = contentTopToBottom[i];

                if (c == null) continue;

                c.Dock = DockStyle.Top;

                card.Controls.Add(c);

            }

            var header = new Panel { Dock = DockStyle.Top, Height = Dpi(56), BackColor = AppTheme.Surface };

            header.Paint += (s, e) =>

            {

                var g = e.Graphics;

                Gfx.HighQuality(g);

                int d = Dpi(30);

                var circle = new Rectangle(0, Dpi(2), d, d);

                using (var b = new SolidBrush(AppTheme.AccentSoft)) g.FillEllipse(b, circle);

                using (var nf = new Font("Segoe UI Semibold", 10F))

                    TextRenderer.DrawText(g, step.ToString(), nf, circle, AppTheme.AccentText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

                using (var tf = new Font("Segoe UI Semibold", 12F))

                    TextRenderer.DrawText(g, title, tf, new Point(d + Dpi(12), 0), AppTheme.TextPrimary, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

                using (var sf = new Font("Segoe UI", 9F))

                    TextRenderer.DrawText(g, subtitle, sf, new Rectangle(d + Dpi(12), Dpi(24), header.Width - d - Dpi(12), Dpi(22)), AppTheme.TextMuted, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

                using var pen = new Pen(AppTheme.Border);

                g.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);

            };

            header.Resize += (s, e) => header.Invalidate();

            var gap = new Panel { Dock = DockStyle.Top, Height = Dpi(10), BackColor = AppTheme.Surface };

            card.Controls.Add(gap);

            card.Controls.Add(header);

            return card;

        }

        private TableLayoutPanel CreateTwoColumnRow(Control lbl1, Control ctrl1, Control lbl2, Control ctrl2)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 3),
                Padding = new Padding(0)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi(36)));

            tbl.Padding = new Padding(0, 0, 0, Dpi(10));

            ctrl1.Margin = new Padding(0, 0, Dpi(10), 0);

            ctrl2.Margin = new Padding(Dpi(10), 0, 0, 0);

            lbl1.Dock = DockStyle.Fill;
            if (lbl1 is Label l1)
            {
                l1.Font = new Font("Segoe UI Semibold", 9F);

                l1.ForeColor = AppTheme.TextSecondary;

                l1.TextAlign = ContentAlignment.BottomLeft;
                l1.Padding = new Padding(0, Dpi(4), 4, Dpi(4));

            }
            ctrl1.Dock = DockStyle.Fill;
            tbl.Controls.Add(lbl1, 0, 0);
            tbl.Controls.Add(ctrl1, 0, 1);

            lbl2.Dock = DockStyle.Fill;
            if (lbl2 is Label l2)
            {
                l2.Font = new Font("Segoe UI Semibold", 9F);

                l2.ForeColor = AppTheme.TextSecondary;

                l2.TextAlign = ContentAlignment.BottomLeft;
                l2.Padding = new Padding(Dpi(10), Dpi(4), 0, Dpi(4));

            }
            ctrl2.Dock = DockStyle.Fill;
            tbl.Controls.Add(lbl2, 1, 0);
            tbl.Controls.Add(ctrl2, 1, 1);

            return tbl;
        }

        private void BtnAddRoute_Click(object? sender, EventArgs e)
        {
            string currentClient = cbClient.Text.Trim();
            using var dlg = new QuickAddRouteDialog(currentClient);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.CreatedRoute != null)
            {
                var route = dlg.CreatedRoute;
                if (!string.IsNullOrWhiteSpace(route.ClientCompanyName) && cbClient.Items.Contains(route.ClientCompanyName))
                {
                    cbClient.SelectedItem = route.ClientCompanyName;
                }
                ReloadRoutesList();
                string routeDisplay = $"{route.RouteCode} - {route.RouteName}";
                if (!cbDestination.Items.Contains(routeDisplay))
                {
                    cbDestination.Items.Add(routeDisplay);
                }
                cbDestination.SelectedItem = routeDisplay;
                cbDestination.Text = routeDisplay;
                PopulateRouteStoresGrid(route);
                numGCrate.Focus();
            }
        }



        /// <summary>The stores of the route's client (or of the picked client when the route has none) from Master Data.</summary>

        private List<ClientStore> StoresForRoute(DeliveryRoute? route)

        {

            string owner = (route?.ClientCompanyName ?? "").Trim();

            if (owner.Length == 0) owner = cbClient.Text.Trim();

            if (owner.Length == 0) return new List<ClientStore>();

            if (string.Equals(owner, cbClient.Text.Trim(), StringComparison.OrdinalIgnoreCase)) return ClientStoresForLookup();

            try { return MJdatabase.GetClientStores(owner); }

            catch (Exception ex) { ErrLog.Swallowed(ex); return new List<ClientStore>(); }

        }

        /// <summary>Finds a store by code first, then by name (spaces and case ignored).</summary>

        private static ClientStore? MatchStore(List<ClientStore> stores, string code, string name)

        {

            if (stores == null || stores.Count == 0) return null;

            string Norm(string? x) => System.Text.RegularExpressions.Regex.Replace((x ?? "").Trim(), @"\s+", " ");

            string c = Norm(code), n = Norm(name);

            ClientStore? hit = null;

            if (c.Length > 0) hit = stores.FirstOrDefault(st => Norm(st.StoreCode).Equals(c, StringComparison.OrdinalIgnoreCase));

            if (hit == null && n.Length > 0) hit = stores.FirstOrDefault(st => Norm(st.StoreName).Equals(n, StringComparison.OrdinalIgnoreCase));

            return hit;

        }

        private void PopulateRouteStoresGrid(DeliveryRoute? route, List<DispatchStop>? savedStops = null)
        {
            if (gridRouteStores == null || pnlRouteStoresBreakdown == null || lblRouteStoresHeader == null) return;
            storeListRoute = route;

            isUpdatingRouteStores = true;
            try
            {
                gridRouteStores.Rows.Clear();
                if (savedStops != null && savedStops.Count > 0)
                {
                    lblRouteStoresHeader.Text = route != null
                        ? $"🏪 Stores & crates for this order  ·  {route.RouteCode} - {route.RouteName} ({savedStops.Count} store{(savedStops.Count == 1 ? "" : "s")})"

                        : $"🏪 Stores & crates for this order  ·  {savedStops.Count} store{(savedStops.Count == 1 ? "" : "s")}";

                    var masterSaved = (savedStops.Any(x => string.IsNullOrWhiteSpace(x.StoreCode))) ? StoresForRoute(route) : new List<ClientStore>();
                    var savedTypes = MJdatabase.GetStopTypes(selectedDelivery?.Id ?? 0);

                    foreach (var st in savedStops)
                    {
                        int rIdx = gridRouteStores.Rows.Add();
                        var row = gridRouteStores.Rows[rIdx];
                        string savedCode = st.StoreCode ?? "";

                        // Older orders saved without a code: show the code from Master Data.

                        if (string.IsNullOrWhiteSpace(savedCode)) savedCode = MatchStore(masterSaved, "", st.Destination ?? "")?.StoreCode?.Trim() ?? "";

                        if (gridRouteStores.Columns.Contains("colStoreCode")) row.Cells["colStoreCode"].Value = savedCode;

                        if (gridRouteStores.Columns.Contains("colStoreName")) row.Cells["colStoreName"].Value = st.Destination;
                        if (gridRouteStores.Columns.Contains("colECrate")) row.Cells["colECrate"].Value = st.ECrate;
                        if (gridRouteStores.Columns.Contains("colGCrate")) row.Cells["colGCrate"].Value = st.GCrate;
                        if (gridRouteStores.Columns.Contains("colStoreTotal")) row.Cells["colStoreTotal"].Value = st.ECrate + st.GCrate;
                        if (gridRouteStores.Columns.Contains("colDrNumbers")) row.Cells["colDrNumbers"].Value = st.DrNumbers;
                        SetStoreRowType(row, MJdatabase.IsRouteStoreRow(savedTypes, route, savedCode, st.Destination));
                        }
                    SizeStoreBreakdown(savedStops.Count);

                    pnlRouteStoresBreakdown.Visible = true;
                }
                else if (route != null && route.PlacesList.Count > 0)
                {
                    lblRouteStoresHeader.Text = $"🏪 Stores & crates for this order  ·  {route.RouteCode} - {route.RouteName} ({route.PlacesList.Count} store{(route.PlacesList.Count == 1 ? "" : "s")})";

                    int cnt = route.PlacesList.Count;
                    int curE = (int)numECrate.Value;
                    int curG = (int)numGCrate.Value;
                    int bE = cnt > 0 ? curE / cnt : 0;
                    int rE = cnt > 0 ? curE % cnt : 0;
                    int bG = cnt > 0 ? curG / cnt : 0;
                    int rG = cnt > 0 ? curG % cnt : 0;
                    var master = StoresForRoute(route);

                    for (int i = 0; i < cnt; i++)
                    {
                        var (sc, sn) = DeliveryRoute.SplitStoreCodeAndName(route.PlacesList[i]);
                        // A route keeps only store names; the code comes from the client's stores in Master Data.

                        var known = MatchStore(master, sc, sn);

                        if (known != null)

                        {

                            if (!string.IsNullOrWhiteSpace(known.StoreCode)) sc = known.StoreCode.Trim();

                            if (!string.IsNullOrWhiteSpace(known.StoreName)) sn = known.StoreName.Trim();

                        }

                        int ec = bE + (i < rE ? 1 : 0);
                        int gc = bG + (i < rG ? 1 : 0);
                        int rIdx = gridRouteStores.Rows.Add();
                        var row = gridRouteStores.Rows[rIdx];
                        if (gridRouteStores.Columns.Contains("colStoreCode")) row.Cells["colStoreCode"].Value = sc;
                        if (gridRouteStores.Columns.Contains("colStoreName")) row.Cells["colStoreName"].Value = sn;
                        if (gridRouteStores.Columns.Contains("colECrate")) row.Cells["colECrate"].Value = ec;
                        if (gridRouteStores.Columns.Contains("colGCrate")) row.Cells["colGCrate"].Value = gc;
                        if (gridRouteStores.Columns.Contains("colStoreTotal")) row.Cells["colStoreTotal"].Value = ec + gc;
                        if (gridRouteStores.Columns.Contains("colDrNumbers")) row.Cells["colDrNumbers"].Value = "";
                        SetStoreRowType(row, true); // rule 16d: the route's own stores are locked
                        }
                    SizeStoreBreakdown(cnt);

                    pnlRouteStoresBreakdown.Visible = true;
                }
                else
                {
                    // No route: drop the old route's name and store count from the heading too.

                    lblRouteStoresHeader.Text = "🏪 Stores & crates for this order";

                    pnlRouteStoresBreakdown.Visible = true;
                    SizeStoreBreakdown(0);

                }
            }
            finally
            {
                isUpdatingRouteStores = false;
            }
            // Official code, name and route on every row; rows that aren't this client's stores are marked.

            storeGuard?.RevalidateAll(showToast: false);

            UpdateSaveStoresToRouteButton();

        }

        // ---- Rule 16d: 🔒 route store rows (locked) vs ➕ added company store rows
        private bool IsRouteStoreRow(DataGridViewRow r) =>
            gridRouteStores != null && gridRouteStores.Columns.Contains("colBaseline") && r.Cells["colBaseline"].Value is bool b && b;
        private void SetStoreRowType(DataGridViewRow row, bool routeStore)
        {
            if (gridRouteStores == null || !gridRouteStores.Columns.Contains("colBaseline")) return;
            row.Cells["colBaseline"].Value = routeStore;
            row.Cells["colRowType"].Value = routeStore ? "🔒 Route store" : "＋ Added store";
            foreach (var key in new[] { "colStoreCode", "colStoreName" })
                if (gridRouteStores.Columns.Contains(key)) row.Cells[key].ReadOnly = routeStore;
            row.Height = Dpi(LargePrint.Px(40, 32));
        }
        /// <summary>Rows that came in without a type (table editor, saved draft): on the route = route store, otherwise added.</summary>
        private void ClassifyUnsetStoreRows()
        {
            if (gridRouteStores == null || !gridRouteStores.Columns.Contains("colBaseline")) return;
            var none = new Dictionary<string, bool>();
            foreach (DataGridViewRow r in gridRouteStores.Rows)
            {
                if (r.Cells["colBaseline"].Value is bool) continue;
                string c = gridRouteStores.Columns.Contains("colStoreCode") ? r.Cells["colStoreCode"].Value?.ToString() ?? "" : "";
                string n = gridRouteStores.Columns.Contains("colStoreName") ? r.Cells["colStoreName"].Value?.ToString() ?? "" : "";
                SetStoreRowType(r, (c.Trim().Length > 0 || n.Trim().Length > 0) && MJdatabase.IsRouteStoreRow(none, storeListRoute, c, n));
            }
        }
        private List<(string Code, string Name, bool Baseline)> CollectStopTypes()
        {
            var list = new List<(string, string, bool)>();
            if (gridRouteStores == null) return list;
            foreach (DataGridViewRow r in gridRouteStores.Rows)
            {
                string c = gridRouteStores.Columns.Contains("colStoreCode") ? r.Cells["colStoreCode"].Value?.ToString() ?? "" : "";
                string n = gridRouteStores.Columns.Contains("colStoreName") ? r.Cells["colStoreName"].Value?.ToString() ?? "" : "";
                if (c.Trim().Length == 0 && n.Trim().Length == 0) continue;
                list.Add((c, n, IsRouteStoreRow(r)));
            }
            return list;
        }
        private void GridRouteStores_RowTypeFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (gridRouteStores == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var row = gridRouteStores.Rows[e.RowIndex];
            bool locked = IsRouteStoreRow(row);
            string cn = gridRouteStores.Columns[e.ColumnIndex].Name;
            if (cn == "colDelStore")
            {
                // Theme-soft: added rows show a red "Remove" link-style button; route rows a muted "Remove…" (asks for a reason).
                e.Value = locked ? "Remove…" : "Remove";
                e.CellStyle.BackColor = locked ? AppTheme.Surface : AppTheme.DangerSoft;
                e.CellStyle.ForeColor = locked ? AppTheme.TextMuted : AppTheme.DangerText;
                return;
            }
            if (cn == "colRowType")
            {
                e.CellStyle.ForeColor = locked ? AppTheme.TextSecondary : AppTheme.SuccessText;
                return;
            }
            if (cn == "colECrate" || cn == "colGCrate" || cn == "colStoreTotal") return; // keep the crate cells' own look
            // Route rows keep the grid's own colours (code and name are read-only); added rows get a faint green tint.
            if (!locked) e.CellStyle.BackColor = ThemeManager.Mix(AppTheme.Surface, AppTheme.Success, ThemeManager.IsDark ? 0.10 : 0.06);
            if (locked && (cn == "colStoreCode" || cn == "colStoreName")) e.CellStyle.ForeColor = AppTheme.TextSecondary;
        }
        /// <summary>Reason for taking a locked route store off this order (at least 10 characters), or null when kept.</summary>
        private string? PromptRouteRowRemovalReason(string store)
        {
            using var f = new Form { Text = "Remove route store", FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false, ClientSize = new Size(Dpi(520), Dpi(230)), BackColor = AppTheme.Surface };
            var lbl = new Label { Text = $"\"{store}\" is one of this route's stores (🔒). Why is it not getting a delivery on this order?", Dock = DockStyle.Top, Height = Dpi(56), Font = LargePrint.Semibold(11F, 9.5F), ForeColor = AppTheme.TextPrimary, Padding = new Padding(Dpi(12), Dpi(10), Dpi(12), 0), UseMnemonic = false };
            var txt = new TextBox { Multiline = true, Dock = DockStyle.Top, Height = Dpi(80), Font = LargePrint.Semibold(12F, 10F), MaxLength = 300, PlaceholderText = "Reason (at least 10 characters), e.g. store closed for inventory today" };
            var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = Dpi(56), FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(Dpi(10)), BackColor = AppTheme.SurfaceAlt };
            var ok = new Button { Text = "Remove from this order", AutoSize = true, Enabled = false, BackColor = AppTheme.Danger, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = LargePrint.Bold(11F, 9F) };
            var keep = new Button { Text = "Keep store", AutoSize = true, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat, Font = LargePrint.Bold(11F, 9F) };
            txt.TextChanged += (s, e) => ok.Enabled = txt.Text.Trim().Length >= 10;
            ok.Click += (s, e) => { if (txt.Text.Trim().Length >= 10) { f.DialogResult = DialogResult.OK; f.Close(); } };
            bar.Controls.Add(ok); bar.Controls.Add(keep);
            var pad = new Panel { Dock = DockStyle.Top, Height = Dpi(8) };
            f.Controls.Add(txt); f.Controls.Add(pad); f.Controls.Add(lbl); f.Controls.Add(bar);
            f.CancelButton = keep;
            f.Shown += (s, e) => txt.Focus();
            return f.ShowDialog(this.FindForm()) == DialogResult.OK ? txt.Text.Trim() : null;
        }
        private void RecalculateTotalsFromRouteStoresGrid()
        {
            if (isUpdatingRouteStores || gridRouteStores == null || pnlRouteStoresBreakdown == null || !pnlRouteStoresBreakdown.Visible) return;
            isUpdatingRouteStores = true;
            try
            {
                int sumE = 0;
                int sumG = 0;
                foreach (DataGridViewRow row in gridRouteStores.Rows)
                {
                    int ec = 0;
                    int gc = 0;
                    if (gridRouteStores.Columns.Contains("colECrate"))
                        int.TryParse(row.Cells["colECrate"].Value?.ToString(), out ec);
                    if (gridRouteStores.Columns.Contains("colGCrate"))
                        int.TryParse(row.Cells["colGCrate"].Value?.ToString(), out gc);
                    ec = Math.Max(0, ec);
                    gc = Math.Max(0, gc);
                    if (gridRouteStores.Columns.Contains("colStoreTotal"))
                        row.Cells["colStoreTotal"].Value = ec + gc;
                    sumE += ec;
                    sumG += gc;
                }
                // Crates come only from the store rows: no rows means 0 crates (never a leftover total).

                numECrate.Value = Math.Clamp(sumE, (int)numECrate.Minimum, (int)numECrate.Maximum);

                numGCrate.Value = Math.Clamp(sumG, (int)numGCrate.Minimum, (int)numGCrate.Maximum);

                CalculateCratesAndRecommendation();
            }
            finally
            {
                isUpdatingRouteStores = false;
            }
        }

        private void SyncNumericCratesToStoreGrid()
        {
            if (isUpdatingRouteStores || isBindingForm || gridRouteStores == null || gridRouteStores.Rows.Count == 0) return;
            isUpdatingRouteStores = true;
            try
            {
                int curE = (int)numECrate.Value;
                int curG = (int)numGCrate.Value;
                int gridE = 0;
                int gridG = 0;
                foreach (DataGridViewRow row in gridRouteStores.Rows)
                {
                    if (gridRouteStores.Columns.Contains("colECrate"))
                    {
                        int.TryParse(row.Cells["colECrate"].Value?.ToString(), out int ec);
                        gridE += Math.Max(0, ec);
                    }
                    if (gridRouteStores.Columns.Contains("colGCrate"))
                    {
                        int.TryParse(row.Cells["colGCrate"].Value?.ToString(), out int gc);
                        gridG += Math.Max(0, gc);
                    }
                }
                if (gridE == curE && gridG == curG) return;

                int cnt = gridRouteStores.Rows.Count;
                for (int i = 0; i < cnt; i++)
                {
                    int rowE = (curE / cnt) + (i < (curE % cnt) ? 1 : 0);
                    int rowG = (curG / cnt) + (i < (curG % cnt) ? 1 : 0);
                    var r = gridRouteStores.Rows[i];
                    if (gridRouteStores.Columns.Contains("colECrate")) r.Cells["colECrate"].Value = rowE;
                    if (gridRouteStores.Columns.Contains("colGCrate")) r.Cells["colGCrate"].Value = rowG;
                    if (gridRouteStores.Columns.Contains("colStoreTotal")) r.Cells["colStoreTotal"].Value = rowE + rowG;
                }
            }
            finally
            {
                isUpdatingRouteStores = false;
            }
        }

        private List<DispatchStop> CollectRouteStoresFromGrid(string clientName, string destination)
        {
            var list = new List<DispatchStop>();
            if (gridRouteStores != null && pnlRouteStoresBreakdown != null && pnlRouteStoresBreakdown.Visible && gridRouteStores.Rows.Count > 0)
            {
                int seq = 1;
                foreach (DataGridViewRow row in gridRouteStores.Rows)
                {
                    string sc = gridRouteStores.Columns.Contains("colStoreCode") ? (row.Cells["colStoreCode"].Value?.ToString()?.Trim() ?? "") : "";
                    string sn = gridRouteStores.Columns.Contains("colStoreName") ? (row.Cells["colStoreName"].Value?.ToString()?.Trim() ?? "") : "";
                    if (string.IsNullOrWhiteSpace(sn) && string.IsNullOrWhiteSpace(sc)) continue;
                    int ec = 0;
                    int gc = 0;
                    if (gridRouteStores.Columns.Contains("colECrate")) int.TryParse(row.Cells["colECrate"].Value?.ToString(), out ec);
                    if (gridRouteStores.Columns.Contains("colGCrate")) int.TryParse(row.Cells["colGCrate"].Value?.ToString(), out gc);
                    string dr = gridRouteStores.Columns.Contains("colDrNumbers") ? (row.Cells["colDrNumbers"].Value?.ToString()?.Trim() ?? "") : "";
                    ec = Math.Max(0, ec);
                    gc = Math.Max(0, gc);
                    list.Add(new DispatchStop
                    {
                        StopSequence = seq++,
                        ClientName = clientName,
                        StoreCode = sc,
                        Destination = sn,
                        ECrate = ec,
                        GCrate = gc,
                        TotalCrates = ec + gc,
                        DeliveredECrates = ec,
                        DeliveredGCrates = gc,
                        DeliveredCrates = ec + gc,
                        DrNumbers = dr,
                        Remarks = $"Stop {seq - 1}"
                    });
                }
                int sumStops = list.Sum(s => s.TotalCrates);
                int formTotal = (int)(numECrate.Value + numGCrate.Value);
                if (sumStops != formTotal && formTotal > 0 && list.Count > 0)
                {
                    int cnt = list.Count;
                    int curE = (int)numECrate.Value;
                    int curG = (int)numGCrate.Value;
                    for (int i = 0; i < cnt; i++)
                    {
                        list[i].ECrate = (curE / cnt) + (i < (curE % cnt) ? 1 : 0);
                        list[i].GCrate = (curG / cnt) + (i < (curG % cnt) ? 1 : 0);
                        list[i].TotalCrates = list[i].ECrate + list[i].GCrate;
                        list[i].DeliveredECrates = list[i].ECrate;
                        list[i].DeliveredGCrates = list[i].GCrate;
                        list[i].DeliveredCrates = list[i].TotalCrates;
                    }
                }
            }
            return list;
        }

        private void PrintOfficialLoadingSheetAction()
        {
            var toPrint = new List<Delivery>();
            if (gridDispatch.SelectedRows.Count > 0)
            {
                DateTime selDate = DateTime.Today;
                if (gridDispatch.SelectedRows[0].DataBoundItem is Delivery firstDel)
                {
                    selDate = firstDel.DispatchDate.Date;
                }
                toPrint = allDispatches.Where(d => d.DispatchDate.Date == selDate && d.Status != "Cancelled").OrderBy(d => d.DepartureTime).ToList();
            }
            if (toPrint.Count == 0)
            {
                toPrint = allDispatches.Where(d => d.Status != "Cancelled").OrderByDescending(d => d.DispatchDate).Take(10).ToList();
            }
            PrintHelper.PrintOfficialLoadingScheduleFromDeliveries(toPrint);
        }



        private void ToggleMultiDropMode(bool isMulti)

        {

            pnlMultiDropBuilder.Visible = isMulti;

            cbClient.Enabled = !isMulti;

            cbDestination.Enabled = !isMulti;


            lblClient.Text = isMulti ? "Primary Client (Multi-Drop):" : "Client / Company Name:";

            lblDest.Text = isMulti ? "First Stop Destination:" : "Delivery Destination / Branch:";

            numGCrate.Enabled = !isMulti;

            numECrate.Enabled = !isMulti;

            if (isMulti)

            {

                if (currentStops.Count == 0)

                {

                    var matchedRoute = MJdatabase.FindRouteByNameOrCode(cbDestination.Text);

                    if (matchedRoute != null && matchedRoute.PlacesList.Count > 0)

                    {

                        LoadStopsFromDeliveryRoute(matchedRoute);

                    }

                    else if (cbDestination.Text.Contains("➔") || cbDestination.Text.Contains("->"))

                    {

                        var arrowStops = cbDestination.Text.Split(new[] { "➔", "->", "=>" }, StringSplitOptions.RemoveEmptyEntries)

                                                           .Select(s => System.Text.RegularExpressions.Regex.Replace(s.Trim(), @"^🛣️\s*|^Route\s*\d+\s*:\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim())

                                                           .Where(s => !string.IsNullOrWhiteSpace(s))

                                                           .ToList();

                        string client = !string.IsNullOrWhiteSpace(cbClient.Text) ? cbClient.Text.Trim() : "Client Drop";

                        int totalG = (int)numGCrate.Value;

                        int totalE = (int)numECrate.Value;

                        for (int i = 0; i < arrowStops.Count; i++)

                        {

                            currentStops.Add(new DispatchStop

                            {

                                StopSequence = i + 1,

                                ClientName = client,

                                Destination = arrowStops[i],

                                GCrate = (i == 0) ? totalG : 0,

                                ECrate = (i == 0) ? totalE : 0,

                                TotalCrates = (i == 0) ? (totalG + totalE) : 0,

                                Remarks = $"Stop {i + 1}"

                            });

                        }

                    }

                    else if (!string.IsNullOrWhiteSpace(cbClient.Text))

                {

                    currentStops.Add(new DispatchStop

                    {

                        StopSequence = 1,

                        ClientName = cbClient.Text.Trim(),

                        Destination = cbDestination.Text.Trim(),

                        GCrate = (int)numGCrate.Value,

                        ECrate = (int)numECrate.Value,

                        TotalCrates = (int)(numGCrate.Value + numECrate.Value),

                        Remarks = "Stop 1"

                    });

                }

                }

                RefreshStopsList();

            }

            else

            {

                CalculateCratesAndRecommendation();

            }

        }



        private void RefreshStopsList()

        {

            if (currentStops.Count > 0 && string.IsNullOrWhiteSpace(cbClient.Text)) BeginInvoke(new Action(AutoFillDispatchNumber));

            lstStops.Items.Clear();

            for (int i = 0; i < currentStops.Count; i++)

            {

                var s = currentStops[i];

                s.StopSequence = i + 1;

                lstStops.Items.Add($"Stop {s.StopSequence}: {s.ClientName} ({s.Destination}) - {s.TotalCrates} Crates ({s.GCrate}G + {s.ECrate}E)");

            }

            int totalG = currentStops.Sum(s => s.GCrate);

            int totalE = currentStops.Sum(s => s.ECrate);

            int totalAll = totalG + totalE;



            isBindingForm = true;

            numGCrate.Value = Math.Min(numGCrate.Maximum, totalG);

            numECrate.Value = Math.Min(numECrate.Maximum, totalE);

            isBindingForm = false;



            lblMultiDropSummary.Text = $"✓ {currentStops.Count} Stop(s) Configured: Total {totalAll:N0} Crates";

            txtTotalCrates.Text = $"{totalAll:N0} Crates";

            CalculateCratesAndRecommendation();

        }



        private void BtnPickRoute_Click(object? sender, EventArgs e)

        {

            var cms = new ContextMenuStrip();

            var routes = MJdatabase.GetDeliveryRoutes(activeOnly: true);

            if (routes.Count == 0)

            {

                cms.Items.Add(new ToolStripMenuItem("No active delivery routes found in Master Data") { Enabled = false });

            }

            else

            {

                cms.Items.Add(new ToolStripMenuItem("🛣️ Select Delivery Route to Load Stops:") { Enabled = false, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) });

                cms.Items.Add(new ToolStripSeparator());

                foreach (var r in routes)

                {

                    var item = new ToolStripMenuItem($"{r.RouteName} ({r.TotalStops} stops) - {r.Description}", null, (s, ev) =>

                    {

                        LoadStopsFromDeliveryRoute(r);

                    });

                    cms.Items.Add(item);

                }

            }

            if (sender is Control ctrl)

            {

                cms.Show(ctrl, new Point(0, ctrl.Height));

            }

        }



        private void LoadStopsFromDeliveryRoute(DeliveryRoute r)

        {

            if (r == null || r.PlacesList.Count == 0) return;

            string client = !string.IsNullOrWhiteSpace(cbClient.Text) ? cbClient.Text.Trim() : "Client Drop";

            int totalG = (int)numGCrate.Value;

            int totalE = (int)numECrate.Value;



            currentStops.Clear();

            for (int i = 0; i < r.PlacesList.Count; i++)

            {

                string place = r.PlacesList[i];

                currentStops.Add(new DispatchStop

                {

                    StopSequence = i + 1,

                    ClientName = client,

                    Destination = place,

                    GCrate = (i == 0) ? totalG : 0,

                    ECrate = (i == 0) ? totalE : 0,

                    TotalCrates = (i == 0) ? (totalG + totalE) : 0,

                    Remarks = $"{r.RouteName} - Stop {i + 1}"

                });

            }

            cbDestination.Text = $"{r.RouteName}: {r.FormattedPlaces}";

            RefreshStopsList();

        }



        private void BtnRemoveStop_Click(object? sender, EventArgs e)

        {

            int idx = lstStops.SelectedIndex;

            if (idx >= 0 && idx < currentStops.Count)

            {

                currentStops.RemoveAt(idx);

                RefreshStopsList();

                if (currentStops.Count > 0)

                {

                    lstStops.SelectedIndex = Math.Min(idx, currentStops.Count - 1);

                }

            }

            else

            {

                QuietBox.Show("Please select a stop to remove from the route list.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }

        }



        private void MoveSelectedStop(int direction)

        {

            int idx = lstStops.SelectedIndex;

            if (idx < 0) return;

            int newIdx = idx + direction;

            if (newIdx < 0 || newIdx >= currentStops.Count) return;



            var item = currentStops[idx];

            currentStops.RemoveAt(idx);

            currentStops.Insert(newIdx, item);

            RefreshStopsList();

            lstStops.SelectedIndex = newIdx;

        }



        private void UpdateDateAvailabilityHint()

        {

            if (dtpDispatchDate.Value.Date > DateTime.Today)

            {

                lblRec.Text += $" | 📅 {dtpDispatchDate.Value:MMM dd}: showing trucks & crew free that day ({bookedTrucks.Count} truck(s), {bookedCrew.Count} crew already booked).";

            }

        }



        private void SetupGridColumns()

        {

            gridDispatch.AutoGenerateColumns = false;

            gridDispatch.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            gridDispatch.ScrollBars = ScrollBars.Both;

            gridDispatch.AllowUserToResizeColumns = true;

            gridDispatch.ShowCellToolTips = false;

            gridDispatch.Columns.Clear();



            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DispatchNumber", HeaderText = "Dispatch #", FillWeight = 88, MinimumWidth = 65, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DispatchDate", HeaderText = "Date", DefaultCellStyle = { Format = "MM/dd/yyyy", Alignment = DataGridViewContentAlignment.MiddleCenter }, FillWeight = 78, MinimumWidth = 62, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DepartureTime", HeaderText = "Time", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }, FillWeight = 66, MinimumWidth = 52, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CleanRouteDisplay", HeaderText = "Route", FillWeight = 115, MinimumWidth = 80, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = "Client", FillWeight = 95, MinimumWidth = 70, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TruckInfo", HeaderText = "Truck", FillWeight = 80, MinimumWidth = 58, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DriverName", HeaderText = "Driver", FillWeight = 80, MinimumWidth = 58, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "AideDisplay", HeaderText = "Helper / Aide", FillWeight = 80, MinimumWidth = 58, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "GCrate", HeaderText = "G-Crates", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }, FillWeight = 52, MinimumWidth = 42, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ECrate", HeaderText = "E-Crates", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }, FillWeight = 52, MinimumWidth = 42, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalCrates", HeaderText = "Total", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }, FillWeight = 52, MinimumWidth = 42, ReadOnly = true });
            gridDispatch.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }, FillWeight = 82, MinimumWidth = 60, ReadOnly = true });

            gridDispatch.CellFormatting -= GridDispatch_CellFormatting;

            gridDispatch.CellFormatting += GridDispatch_CellFormatting;

        }



        private void GridDispatch_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)

        {

            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string propName = gridDispatch.Columns[e.ColumnIndex].DataPropertyName;

            FormatOverCapacityBadge(e, propName);

            if (propName == "GCrate" || propName == "ECrate" || propName == "TotalCrates" || propName == "DispatchDate" || propName == "DepartureTime" || propName == "Status" || propName == "RouteTypeDisplay")

            {

                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            }

            if (propName != "Status" && gridDispatch.DataSource is IList<Delivery> mutedList && e.RowIndex < mutedList.Count && IsFinished(mutedList[e.RowIndex]))

                e.CellStyle.ForeColor = AppTheme.TextMuted; // finished orders shown under the active ones are greyed slightly

            if (propName == "Status" && e.Value != null)

            {

                string status = e.Value.ToString() ?? "";

                if (gridDispatch.DataSource is IList<Delivery> boundList && e.RowIndex < boundList.Count && boundList[e.RowIndex] is Delivery rowDel && MJdatabase.IsAwaitingCompletion(rowDel))

                {

                    e.Value = CompleteCellText;

                    e.FormattingApplied = true;

                    e.CellStyle.BackColor = Pal.C(254, 243, 199);

                    e.CellStyle.ForeColor = Pal.C(180, 83, 9);

                    e.CellStyle.Font = FontCache.Get(gridDispatch.Font, FontStyle.Bold);

                    return;

                }

                if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))

                {

                    e.CellStyle.BackColor = Pal.C(220, 252, 231);

                    e.CellStyle.ForeColor = Pal.C(22, 101, 52);

                    e.CellStyle.Font = FontCache.Get(gridDispatch.Font, FontStyle.Bold);

                }



                else if (status.Equals("In Transit", StringComparison.OrdinalIgnoreCase))

                {

                    e.CellStyle.BackColor = Pal.C(254, 243, 199);

                    e.CellStyle.ForeColor = Pal.C(180, 83, 9);

                    e.CellStyle.Font = FontCache.Get(gridDispatch.Font, FontStyle.Bold);

                }

                else if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase))

                {

                    e.CellStyle.BackColor = Pal.C(241, 245, 249);

                    e.CellStyle.ForeColor = Pal.C(51, 65, 85);

                }

                else if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))

                {

                    e.CellStyle.BackColor = Pal.C(241, 245, 249);

                    e.CellStyle.ForeColor = Pal.C(100, 116, 139);

                }

            }

        }



        private bool isLoadingData;

        private bool reloadRequested;

        /// <summary>

        /// Only one load runs at a time. A second request while loading just queues one more pass,

        /// so two loads never fight over the same lists and table (which caused errors and freezes).

        /// </summary>

        private async void LoadDropdownsAndGrid()

        {

            if (isLoadingData)

            {

                reloadRequested = true;

                return;

            }

            isLoadingData = true;

            try

            {

                await LoadDropdownsAndGridCore();

            }

            finally

            {

                isLoadingData = false;

            }

            if (reloadRequested && !this.IsDisposed)

            {

                reloadRequested = false;

                LoadDropdownsAndGrid();

            }

        }

        private static void LogViewError(string where, Exception ex)

        {

            AppLog.Error("DispatchView." + where, ex);

        }

        private void WhileBinding(Action a)

        {

            bool was = isBindingForm;

            isBindingForm = true;

            try { a(); } finally { isBindingForm = was; }

        }

        private static void SafeStep(string name, Action step)

        {

            try { step(); }

            catch (Exception ex) { LogViewError(name, ex); }

        }

        private async Task LoadDropdownsAndGridCore()

        {

            // An order form that is open (for example when coming back from adding a truck or opening the client)

            // keeps what was typed and picked: only the lists behind it are refreshed.

            bool keepForm = pnlForm.Visible;

            string keepClient = cbClient.Text, keepRoute = cbDestination.Text;

            int keepTruck = cbTruck.SelectedIndex >= 0 && cbTruck.SelectedIndex < trucks.Count ? trucks[cbTruck.SelectedIndex].Id : 0;

            int keepDriver = cbDriver.SelectedIndex >= 0 && cbDriver.SelectedIndex < drivers.Count ? drivers[cbDriver.SelectedIndex].Id : 0;

            try

            {

                // One client list for the whole system: the clients in Master Data → Clients.

                var clients = await Task.Run(() => MJdatabase.GetClientNames());

                cbClient.BeginUpdate();
                cbClient.Items.Clear();
                foreach (var c in clients.Distinct(StringComparer.OrdinalIgnoreCase)) cbClient.Items.Add(c);
                UIThemeHelper.SafeEndUpdate(cbClient);

                WhileBinding(() => { cbClient.SelectedIndex = -1; cbClient.Text = keepForm ? keepClient : ""; });

                var activeRoutes = await Task.Run(() => MJdatabase.GetDeliveryRoutes(activeOnly: true));

                cbDestination.BeginUpdate();
                cbDestination.Items.Clear();
                foreach (var r in activeRoutes.GroupBy(x => x.RouteCode).Select(g => g.First())) cbDestination.Items.Add($"{r.RouteCode} - {r.RouteName}");
                UIThemeHelper.SafeEndUpdate(cbDestination);

                WhileBinding(() => { cbDestination.SelectedIndex = -1; cbDestination.Text = keepForm ? keepRoute : ""; });

                if (cbCargoType.Items.Count == 0 || CargoRouting.Enabled != cbCargoType.Items.Contains(CargoRouting.SpecialOption))

                {

                    CargoRouting.FillCargoCombo(cbCargoType);

                    cbCargoType.SelectedIndex = -1;
                    cbCargoType.Text = "";

                }



                allTrucks = await MJdatabase.GetTrucksAsync();

                if (this.IsDisposed || !this.IsHandleCreated) return;



                var crews = await MJdatabase.GetCrewsAsync();

                if (this.IsDisposed || !this.IsHandleCreated) return;

                allDrivers = crews.FindAll(c => c.Role.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0 && c.IsActive);

                allAides = crews.FindAll(c => (c.Role.IndexOf("Aide", StringComparison.OrdinalIgnoreCase) >= 0 || c.Role.IndexOf("Helper", StringComparison.OrdinalIgnoreCase) >= 0 || c.Role.IndexOf("Crew", StringComparison.OrdinalIgnoreCase) >= 0) && c.IsActive);

                foreach (var licensedAide in allAides.Where(a => a.HasLicense))

                {
                    if (!allDrivers.Any(d => d.Id == licensedAide.Id))
                    {
                        allDrivers.Add(licensedAide);
                    }
                }

                allDispatches = await MJdatabase.GetDeliveriesAsync();

                RefreshOverCapacityMap();

                if (this.IsDisposed || !this.IsHandleCreated) return;

                SafeStep("PopulateTrucksDriversAndAides", () =>

                {

                    if (!keepForm) { PopulateTrucksDriversAndAides(); return; }

                    WhileBinding(() =>

                    {

                        PopulateTrucksDriversAndAides();

                        int ti = trucks.FindIndex(t => t.Id == keepTruck);

                        if (ti >= 0) cbTruck.SelectedIndex = ti;

                        int di = drivers.FindIndex(c => c.Id == keepDriver);

                        if (di >= 0) cbDriver.SelectedIndex = di;

                        PickJustAdded();

                    });

                });

                FilterGridData();

                var deliveries = allDispatches;



                int pendingCount = deliveries.Count(d => d.Status == "Scheduled" || d.Status == "Pending");

                int transitCount = deliveries.Count(d => d.Status == "In Transit");

                
                int completedCount = deliveries.Count(d => d.Status == "Completed");

                int totalCrates = deliveries.Sum(d => d.TotalCrates);



                SafeStep("UpdateSummaryBadges", () => UpdateSummaryBadges(deliveries.Count, pendingCount, transitCount, completedCount, totalCrates));

                // Only reset the form when nobody is editing an order (a refresh must not wipe what the user is typing).

                if (!pnlForm.Visible) SafeStep("ClearFields", ClearFields);

                SafeStep("CalculateCratesAndRecommendation", CalculateCratesAndRecommendation);

                SafeStep("UpdateDestinationHint", UpdateDestinationHint);

                // The draft is offered when the order form is opened, not every time the list loads.

                if (pnlForm.Visible && !skipDraftPromptOnce) SafeStep("CheckAndPromptDraft", CheckAndPromptDraft);

                skipDraftPromptOnce = false;

            }

            catch (Exception ex)

            { ErrLog.Error("DispatchManagementView", ex); 

                LogViewError("Load", ex);

                MessageBox.Show($"Failed to load dispatch data: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }



        private static void ApplyButtonVisualState(Button btn, bool isEnabled, Color activeBgColor, Color activeFgColor)

        {

            btn.Enabled = isEnabled;

            if (isEnabled)

            {

                btn.BackColor = activeBgColor;

                btn.ForeColor = activeFgColor;

                btn.Cursor = Cursors.Hand;

            }

            else

            {

                btn.BackColor = Pal.C(226, 232, 240);

                btn.ForeColor = Pal.C(148, 163, 184);

                btn.Cursor = Cursors.Default;

            }

        }



        private void SetButtonStates(bool isEditing)
        {
            string dNum = txtDispatchNumber.Text.Trim();
            var matched = !string.IsNullOrWhiteSpace(dNum)
                ? allDispatches.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.DispatchNumber) && d.DispatchNumber.Equals(dNum, StringComparison.OrdinalIgnoreCase))
                : null;

            bool exists = (selectedDelivery != null || matched != null) && !string.IsNullOrWhiteSpace(dNum);
            var activeDelivery = selectedDelivery ?? matched;

            if (!exists)
            {
                btnSave.Visible = true;
                btnSave.Enabled = true;
                ApplyButtonVisualState(btnSave, true, Pal.C(13, 110, 253), Pal.White);

                if (tblFormButtons.Controls.Contains(btnSave)) tblFormButtons.SetColumnSpan(btnSave, 2);

                btnUpdate.Visible = false;
                btnUpdate.Enabled = false;

                btnReschedule.Visible = false;
                btnReschedule.Enabled = false;

                tblFormButtons.RowStyles[0].Height = 34F;
                tblFormButtons.RowStyles[1].Height = 0F;
                tblFormButtons.RowStyles[2].Height = 30F;
                pnlFormActionsBar.Height = Dpi(66);

                btnClear.Text = "🧹 Reset form";

            }
            else
            {
                btnSave.Visible = false;
                btnSave.Enabled = false;

                btnUpdate.Visible = true;
                btnUpdate.Enabled = true;
                ApplyButtonVisualState(btnUpdate, true, Pal.C(25, 135, 84), Pal.White);

                if (tblFormButtons.Controls.Contains(btnUpdate)) tblFormButtons.SetColumnSpan(btnUpdate, 2);

                string st = activeDelivery?.Status ?? "";
                bool isOngoing = !st.Equals("Completed", StringComparison.OrdinalIgnoreCase) && !st.Equals("Cancelled", StringComparison.OrdinalIgnoreCase);

                if (isOngoing)
                {
                    btnReschedule.Visible = true;
                    btnReschedule.Enabled = true;
                    tblFormButtons.RowStyles[0].Height = 34F;
                    tblFormButtons.RowStyles[1].Height = 32F;
                    tblFormButtons.RowStyles[2].Height = 30F;
                    pnlFormActionsBar.Height = Dpi(66);

                }
                else
                {
                    btnReschedule.Visible = false;
                    btnReschedule.Enabled = false;
                    tblFormButtons.RowStyles[0].Height = 34F;
                    tblFormButtons.RowStyles[1].Height = 0F;
                    tblFormButtons.RowStyles[2].Height = 30F;
                    pnlFormActionsBar.Height = Dpi(66);

                }

                btnClear.Text = "➕ New dispatch form";

            }
        }



        private int GetCrewTripCount(int crewId, bool isDriver)
        {
            if (allDispatches == null || allDispatches.Count == 0) return 0;
            if (isDriver)
            {
                return allDispatches.Count(d => d.DriverId == crewId && !string.Equals(d.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));
            }
            return allDispatches.Count(d =>
                !string.Equals(d.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                (d.AideId == crewId || (!string.IsNullOrWhiteSpace(d.AideIds) && d.AideIds.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Any(id => int.TryParse(id.Trim(), out int parsedId) && parsedId == crewId))));
        }

        private void AutoSuggestDriverForSelectedTruck()
        {
            if (isBindingForm || selectedDelivery != null) return;
            if (cbTruck.SelectedIndex < 0 || cbTruck.SelectedIndex >= trucks.Count) return;

            var selTruck = trucks[cbTruck.SelectedIndex];
            if (selTruck == null || drivers.Count == 0) return;

            int suggestedDriverId = 0;
            if (allDispatches != null && allDispatches.Count > 0)
            {
                var regularDriverGroup = allDispatches
                    .Where(d => d.TruckId == selTruck.Id && d.DriverId > 0 && !string.Equals(d.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(d => d.DriverId)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                if (regularDriverGroup != null)
                {
                    suggestedDriverId = regularDriverGroup.Key;
                }
            }

            int idx = -1;
            if (suggestedDriverId > 0)
            {
                idx = drivers.FindIndex(d => d.Id == suggestedDriverId && CrewFree(d));

            }

            if (idx < 0 && cbDriver.SelectedIndex < 0)
            {
                idx = drivers.FindIndex(d => CrewFree(d) && d.Role.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0);

            }

            if (idx >= 0 && idx < cbDriver.Items.Count)
            {
                cbDriver.SelectedIndex = idx;
            }
        }

        private void PopulateTrucksDriversAndAides()
        {
            ComputeBookingsFor(dtpDispatchDate.Value.Date);

            trucks = allTrucks.Where(t =>
                TruckFree(t) ||

                (selectedDelivery != null && t.Id == selectedDelivery.TruckId)
            ).ToList();

            cbTruck.Items.Clear();

            foreach (var t in trucks)
            {
                string tBadge = TruckFree(t) ? "[ACTIVE]" : (MJdatabase.IsTruckBlockedStatus(t.Status) ? "[MAINTENANCE]" : "[ON TRIP]");

                cbTruck.Items.Add($"{tBadge} {t.PlateNumber} [{t.TruckType}]");
            }

            cbTruck.SelectedIndex = -1;

            drivers = allDrivers.Where(d =>
                CrewFree(d) ||

                (selectedDelivery != null && d.Id == selectedDelivery.DriverId)
            )
            .OrderByDescending(d => d.Role.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0)
            .ThenBy(d => GetCrewTripCount(d.Id, true))
            .ThenBy(d => d.FullName)
            .ToList();

            cbDriver.Items.Clear();

            foreach (var d in drivers)
            {
                string dBadge = CrewFree(d) ? "[ACTIVE]" : "[ON TRIP]";

                bool isBackupAide = d.Role.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) < 0;
                string roleNote = isBackupAide ? " [Licensed Aide]" : "";
                int tripCount = GetCrewTripCount(d.Id, true);
                cbDriver.Items.Add($"{dBadge} {d.FullName}{roleNote} ({tripCount} Trips)");
            }

            cbDriver.SelectedIndex = -1;

            aides = allAides.Where(a =>
                CrewFree(a) ||

                (selectedDelivery != null && selectedAideIds.Contains(a.Id))
            )
            .OrderBy(a => GetCrewTripCount(a.Id, false))
            .ThenBy(a => a.FullName)
            .ToList();

            if (aides.Count == 0)
            {
                lblAi.Text = allAides.Count == 0 ? "Assigned Helper/Aides: no aides yet. Use “＋ Add an aide” above." : "Assigned Helper/Aides: no aide is free on this date. Use “＋ Add an aide” above or pick another date.";

                lblAi.ForeColor = Pal.C(185, 28, 28);

            }
            else
            {
                lblAi.Text = "Assigned Helper/Aides (Sorted by fewest trips — Standard: 2 Aides):";
                lblAi.ForeColor = Pal.C(30, 41, 59);

            }

            RenderAidesModule();
        }



        private void GridDispatch_SelectionChanged(object? sender, EventArgs e)
        {
            // While the order form is open the list is hidden; a list refresh re-selecting its first row must not

            // replace the order being typed (orders are opened for editing explicitly, see EditDeliveryOrder).

            if (pnlForm.Visible) { UpdateCompleteOrderButton(); return; }

            if (gridDispatch.SelectedRows.Count > 0 && gridDispatch.SelectedRows[0].DataBoundItem is Delivery d)
            {
                BindDeliveryToForm(d);
            }
            UpdateCompleteOrderButton();

        }

        // ------------------------------------------------------------------ Order completion

        private const string AwaitingCompletionFilter = "__AwaitingCompletion";

        private const string AllOrdersFilter = "__AllOrders";

        /// <summary>Completed or cancelled: the order is finished and leaves the active list.</summary>

        private static bool IsFinished(Delivery d) => RowMenu.Is(d.Status, "Completed", "Cancelled");

        internal const string CompleteCellText = "✔ Complete";

        private void WireRowCompleteButtons()

        {

            gridDispatch.CellClick += (s, e) =>

            {

                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

                if (gridDispatch.Columns[e.ColumnIndex].DataPropertyName != "Status") return;

                if (gridDispatch.Rows[e.RowIndex].DataBoundItem is Delivery d && IsOnTheRoad(d))

                {

                    gridDispatch.ClearSelection();

                    gridDispatch.Rows[e.RowIndex].Selected = true;

                    CompleteSelectedOrder(d);

                }

            };

            gridDispatch.CellMouseEnter += (s, e) =>

            {

                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

                bool hot = gridDispatch.Columns[e.ColumnIndex].DataPropertyName == "Status" &&

                           gridDispatch.Rows[e.RowIndex].DataBoundItem is Delivery d && IsOnTheRoad(d);

                gridDispatch.Cursor = hot ? Cursors.Hand : Cursors.Default;

            };

            gridDispatch.MouseLeave += (s, e) => gridDispatch.Cursor = Cursors.Default;

        }

        /// <summary>On the road: in transit / delayed, or its departure time has passed (the auto check may not have run yet).</summary>

        internal static bool IsOnTheRoad(Delivery? d)

        {

            if (d == null) return false;

            string st = d.Status ?? "";

            if (st.Equals("In Transit", StringComparison.OrdinalIgnoreCase) || st.Equals("Delayed", StringComparison.OrdinalIgnoreCase)) return true;

            if (st.Equals("Completed", StringComparison.OrdinalIgnoreCase) || st.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)) return false;

            return MJdatabase.DepartureDateTime(d) <= DateTime.Now;

        }

        /// <summary>Complete is allowed once the truck is on the road — including when it arrives early.</summary>

        private static bool CanCompleteOrder(Delivery? d) => IsOnTheRoad(d);

        private void StartTripNow(Delivery d)

        {

            var leaves = MJdatabase.DepartureDateTime(d);

            if (MessageBox.Show($"Start dispatch {d.DispatchNumber} now?\n\nIt is set to leave at {leaves:MMM d, h:mm tt}. Use this when the truck left early — the order goes In Transit and can be completed when it arrives.",

                    "Start trip now", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var (ok, message) = MJdatabase.StartTripNow(d.Id, SessionContext.CurrentUsername);

            if (!ok) { MessageBox.Show(message, "Start trip now", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            ToastHelper.ShowToast(this, message, ToastType.Success);

            LoadDropdownsAndGrid();

            try { MainForm.Instance?.UpdateHeaderKpiBadges(); } catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }

        }

        private Delivery? GetGridSelectedDelivery()

        {

            if (gridDispatch.SelectedRows.Count > 0 && gridDispatch.SelectedRows[0].DataBoundItem is Delivery d) return d;

            return selectedDelivery;

        }

        private void UpdateCompleteOrderButton()

        {

            if (btnCompleteOrder == null) return;

            var d = GetGridSelectedDelivery();

            bool can = CanCompleteOrder(d);

            btnCompleteOrder.Enabled = can;

            if (can && d != null && MJdatabase.IsAwaitingCompletion(d))

                btnCompleteOrder.Text = $"✔ Complete {d.DispatchNumber}";

            else

                btnCompleteOrder.Text = "✔ Complete order";

            btnCompleteOrder.Invalidate();

        }

        private void SetShowFinished(bool on)

        {

            showFinished = on;

            try { MJdatabase.SetSystemSetting(ShowFinishedKey, on ? "True" : "False", "Loading Schedule & Dispatch: show finished orders"); } catch (System.Exception swxSf2) { ErrLog.Swallowed(swxSf2); }

            if (on && currentStatusFilter == AllOrdersFilter) currentStatusFilter = "";

            FilterGridData();

        }

        private void UpdateShowFinishedButton()

        {

            if (btnShowFinished == null) return;

            int n = allDispatches.Count(IsFinished);

            btnShowFinished.Text = showFinished ? $"● Showing finished ({n})" : $"○ Show finished ({n})";

            UIThemeHelper.SetButtonRole(btnShowFinished, showFinished ? "strong" : "secondary");

        }

        /// <summary>Text added to the "completed"/"cancelled" notice when the order is about to leave the list.</summary>

        private string FinishedNote => showFinished ? "" : " · moved to finished orders (Show finished or Order History)";

        private void UpdateAwaitingCompletionBadge()

        {

            if (btnAwaitingCompletion == null) return;

            int n = allDispatches.Count(MJdatabase.IsAwaitingCompletion);

            btnAwaitingCompletion.Visible = n > 0 || currentStatusFilter == AwaitingCompletionFilter;

            btnAwaitingCompletion.Text = currentStatusFilter == AwaitingCompletionFilter

                ? $"⏰ {n} awaiting completion ✕"

                : $"⏰ {n} awaiting completion";

        }

        private void CompleteSelectedOrder(Delivery? target = null)

        {

            var d = target ?? GetGridSelectedDelivery();

            if (d == null)

            {

                QuietBox.Show("Please select a dispatch order in the table first.", "Select an Order", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;

            }

            if (!CanCompleteOrder(d))

            {

                if (RowMenu.Is(d.Status, "Completed", "Cancelled"))

                    QuietBox.Show($"Dispatch #{d.DispatchNumber} is already {d.Status.ToLowerInvariant()}.", "Complete Order", MessageBoxButtons.OK, MessageBoxIcon.Information);

                else if (MessageBox.Show($"Dispatch #{d.DispatchNumber} hasn't left yet — it goes in transit automatically at {MJdatabase.DepartureDateTime(d):MMM d, h:mm tt}.\n\nDid the truck already leave? Click Yes to start the trip now.", "Not on the road yet", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)

                    StartTripNow(d);

                return;

            }

            int days = Math.Max(0, (DateTime.Today - d.DispatchDate.Date).Days);

            string age = days == 0 ? "today" : days == 1 ? "1 day ago" : $"{days} days ago";

            string notStarted = DateTime.Today == d.DispatchDate.Date ? "\n\nArrived early? That's fine — completing it now frees the truck and crew for the next trip." : "";

            var confirm = MessageBox.Show(

                $"Mark this order as COMPLETED?\n\n" +

                $"Dispatch #:  {d.DispatchNumber}\n" +

                $"Client:  {d.ClientName}\n" +

                $"Truck:  {d.TruckInfo}\n" +

                $"Driver:  {d.DriverName}\n" +

                $"Dispatched:  {d.DispatchDate:MMM dd, yyyy} ({age})" +

                notStarted +

                "\n\nThis frees the truck, driver and aides, and closes the linked loading schedule. " +

                "You can still file the trip report afterwards.",

                "Complete Order", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            var (ok, message) = MJdatabase.CompleteDelivery(d.Id, SessionContext.CurrentUsername);

            if (!ok)

            {

                MessageBox.Show(message, "Complete Order", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;

            }

            ToastHelper.ShowToast(this, message + FinishedNote, ToastType.Success);

            if (selectedDelivery != null && selectedDelivery.Id == d.Id)

            {

                ClearFields();

                SetFormVisibility(false);

            }

            LoadDropdownsAndGrid();

            try { MainForm.Instance?.UpdateHeaderKpiBadges(); } catch (System.Exception swx3) { ErrLog.Swallowed(swx3); }

        }

        private void HandleDispatchNumberInput()
        {
            string dNum = txtDispatchNumber.Text.Trim();
            if (string.IsNullOrWhiteSpace(dNum))
            {
                selectedDelivery = null;
                lblFormHeader.Text = "New Dispatch Order";

                SetButtonStates(false);
                return;
            }

            var matched = allDispatches.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.DispatchNumber) && d.DispatchNumber.Equals(dNum, StringComparison.OrdinalIgnoreCase));
            if (matched != null)
            {
                BindDeliveryToForm(matched);
                for (int i = 0; i < gridDispatch.Rows.Count; i++)
                {
                    if (gridDispatch.Rows[i].DataBoundItem is Delivery rowD && rowD.Id == matched.Id)
                    {
                        gridDispatch.ClearSelection();
                        gridDispatch.Rows[i].Selected = true;
                        break;
                    }
                }
            }
            else
            {
                selectedDelivery = null;
                lblFormHeader.Text = $"New Dispatch Order  ·  {dNum}";

                SetButtonStates(false);
            }
        }

        private void BindDeliveryToForm(Delivery d)
        {
            isBindingForm = true;
            try
            {
                selectedDelivery = d;
                txtDispatchNumber.ReadOnly = false;

                txtDispatchNumber.Text = selectedDelivery.DispatchNumber;
                cbClient.Text = selectedDelivery.ClientName;
                var matchedDspRoute = MJdatabase.FindRouteByNameOrCode(selectedDelivery.Destination);
                cbDestination.Text = matchedDspRoute != null ? $"{matchedDspRoute.RouteCode} - {matchedDspRoute.RouteName}" : selectedDelivery.Destination;
                cbCargoType.Text = string.IsNullOrEmpty(selectedDelivery.CargoType) ? "Cold / Chilled" : selectedDelivery.CargoType;
                chkIsPeak.Checked = selectedDelivery.IsPeak;

                selectedAideIds.Clear();
                if (!string.IsNullOrEmpty(selectedDelivery.AideIds))
                {
                    var idTokens = selectedDelivery.AideIds.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                    foreach (var idStr in idTokens)
                    {
                        if (int.TryParse(idStr, out int aId))
                            selectedAideIds.Add(aId);
                    }
                }

                PopulateTrucksDriversAndAides();

                int trkIdx = trucks.FindIndex(t => t.Id == selectedDelivery.TruckId);
                if (trkIdx >= 0 && trkIdx < cbTruck.Items.Count) cbTruck.SelectedIndex = trkIdx;

                int drvIdx = drivers.FindIndex(drv => drv.Id == selectedDelivery.DriverId);
                if (drvIdx >= 0 && drvIdx < cbDriver.Items.Count) cbDriver.SelectedIndex = drvIdx;

                if (selectedDelivery.DispatchDate >= dtpDispatchDate.MinDate && selectedDelivery.DispatchDate <= dtpDispatchDate.MaxDate)
                {
                    dtpDispatchDate.Value = selectedDelivery.DispatchDate;
                }
                cbDepartureTime.Text = string.IsNullOrEmpty(selectedDelivery.DepartureTime) ? "08:00 AM" : selectedDelivery.DepartureTime;
                cbStatus.Text = string.IsNullOrEmpty(selectedDelivery.Status) ? "Scheduled" : selectedDelivery.Status;
                numGCrate.Value = Math.Max(0, selectedDelivery.GCrate);
                numECrate.Value = Math.Max(0, selectedDelivery.ECrate);
                numCombiCold.Value = Math.Min(numCombiCold.Maximum, Math.Max(0, MJdatabase.GetDeliveryCapacityInfo(selectedDelivery.Id).Cold));

                var savedStops = MJdatabase.GetOrBuildTripStopsForDelivery(selectedDelivery);
                PopulateRouteStoresGrid(matchedDspRoute, savedStops);
                CalculateCratesAndRecommendation();
                UpdateDestinationHint();
                lblFormHeader.Text = $"Dispatch #{selectedDelivery.DispatchNumber}  ·  {(MJdatabase.IsAwaitingCompletion(selectedDelivery) ? "For Completion" : selectedDelivery.Status)}";

            }
            finally
            {
                isBindingForm = false;
            }
            SetButtonStates(true);
        }



        private (List<int> Ids, List<string> Names) GetSelectedAides()

        {

            var sel = aides.Where(a => selectedAideIds.Contains(a.Id)).ToList();

            var ids = sel.Select(a => a.Id).ToList();

            var names = sel.Select(a => a.FullName).ToList();

            return (ids, names);

        }



        private void BtnSave_Click(object? sender, EventArgs e)

        {

            try

            {
                var valErrors = new List<string>();
                Control? firstInvalid = null;
                ValidationHelper.ClearErrors(pnlForm);

                if (!ValidationHelper.ValidateRequiredText(txtDispatchNumber, "Order Reference / Dispatch #", valErrors)) firstInvalid ??= txtDispatchNumber;
                if (!chkMultiDrop.Checked)
                {
                    if (!ValidationHelper.ValidateRequiredCombo(cbClient, "Client Company", valErrors)) firstInvalid ??= cbClient;
                    // The route box is hidden: the destination comes from the stores entered in the table.

                    string autoDest = ResolveDestinationFromStores(cbDestination.Text.Trim());

                    if (GetGridStoreList().Count > 0 && autoDest.Length > 0 && !string.Equals(cbDestination.Text.Trim(), autoDest, StringComparison.Ordinal))

                        WhileBinding(() => cbDestination.Text = autoDest);

                    if (GetGridStoreList().Count == 0)

                    {

                        valErrors.Add("Please add at least one store to this order (Stores & crates → Add).");

                        firstInvalid ??= (Control?)gridRouteStores ?? cbClient;

                    }

                }
                else
                {
                    if (currentStops.Count == 0)
                    {
                        valErrors.Add("At least 1 destination stop must be added for multi-drop route.");
                        firstInvalid ??= cbDestination;
                    }
                }
                if (cbTruck.SelectedIndex < 0)
                {
                    ValidationHelper.ApplyValidationState(cbTruck, false, "Assigned Truck is required.");
                    valErrors.Add("Assigned Truck is required.");
                    firstInvalid ??= cbTruck;
                }
                if (cbDriver.SelectedIndex < 0)
                {
                    ValidationHelper.ApplyValidationState(cbDriver, false, "Assigned Driver is required.");
                    valErrors.Add("Assigned Driver is required.");
                    firstInvalid ??= cbDriver;
                }
                int plannedCrates = (int)numGCrate.Value + (int)numECrate.Value;
                if (CargoRouting.IsSpecial(cbCargoType.Text))

                {

                    ValidationHelper.ApplyValidationState(cbCargoType, false, "Non-crate / specialized cargo is booked as a Special Trip.");

                    valErrors.Add($"Cargo type '{cbCargoType.Text.Trim()}' is not a crate delivery. Book it as a Special Trip / Special Delivery instead.");

                    firstInvalid ??= cbCargoType;

                }

                if (plannedCrates <= 0 && (!chkMultiDrop.Checked || currentStops.Sum(s => s.TotalCrates) <= 0))
                {
                    ValidationHelper.ApplyValidationState(numGCrate, false, "Total Crates must be greater than zero.");
                    valErrors.Add("Total Crates to deliver must be greater than 0." + CargoRouting.SpecialHint);

                    firstInvalid ??= numGCrate;
                }

                if (!ValidationHelper.ShowValidationSummaryIfAny(this, valErrors, firstInvalid))
                {
                    return;
                }


                string dspNum = txtDispatchNumber.Text.Trim();

                if (AutoNumbers && (string.IsNullOrWhiteSpace(dspNum) || dspNum.Equals(lastAutoDispatchNumber, StringComparison.OrdinalIgnoreCase)))

                {

                    // Re-check at save time so two orders for the same client never get the same number.

                    dspNum = GenerateNextDispatchNumber();

                    lastAutoDispatchNumber = dspNum;

                    isBindingForm = true;

                    txtDispatchNumber.Text = dspNum;

                    isBindingForm = false;

                }



                var existingDeliveries = MJdatabase.GetDeliveries();

                if (existingDeliveries.Any(d => d.DispatchNumber.Equals(dspNum, StringComparison.OrdinalIgnoreCase)))

                {

                    MessageBox.Show($"Order Reference / Dispatch Number '{dspNum}' is already in use. Please provide a unique reference number.", "Reference Number Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    txtDispatchNumber.Focus();

                    return;

                }



                bool isMulti = chkMultiDrop.Checked;

                string clientName = cbClient.Text.Trim();

                if (!ValidateStoreRows()) return;

                string destination = ResolveDestinationFromStores(cbDestination.Text.Trim());

                if (destination.Length > 0 && !string.Equals(cbDestination.Text.Trim(), destination, StringComparison.Ordinal))

                    WhileBinding(() => cbDestination.Text = destination);

                var matchedRoute = MJdatabase.FindRouteByNameOrCode(destination);

                if (matchedRoute != null && matchedRoute.PlacesList.Count > 0)

                {

                    {

                        // List the stores actually on this order (not every store of the route).

                        var orderStores = GetGridStoreList().Select(x => x.Name.Length > 0 ? x.Name : x.Code).Where(x => x.Length > 0).ToList();

                        destination = $"{matchedRoute.RouteCode} - {matchedRoute.RouteName}: {(orderStores.Count > 0 ? string.Join(" ➔ ", orderStores) : matchedRoute.FormattedPlaces)}";

                    }

                }



                if (!isMulti)

                {

                    if (string.IsNullOrWhiteSpace(clientName))

                    {

                        MessageBox.Show("Please select or enter a Client Company name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        cbClient.Focus();

                        return;

                    }

                    if (string.IsNullOrWhiteSpace(destination))

                    {

                        MessageBox.Show("Please add at least one store to this order (Stores & crates → Add).", "Store required", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        cbDestination.Focus();

                        return;

                    }

                }

                else

                {

                    if (currentStops.Count == 0)

                    {

                        MessageBox.Show("Please add at least 1 destination stop to configure the multi-drop route.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        return;

                    }

                    clientName = currentStops[0].ClientName;

                    destination = currentStops[0].Destination;

                }



                if (clientName.Length > 150)

                {

                    MessageBox.Show("Client name cannot exceed 150 characters.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    cbClient.Focus();

                    return;

                }

                if (destination.Length > 500)

                {

                    MessageBox.Show("Destination cannot exceed 200 characters.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    cbDestination.Focus();

                    return;

                }



                if (cbTruck.SelectedIndex < 0 || cbDriver.SelectedIndex < 0)

                {

                    MessageBox.Show("STRICT FLEET ASSIGNMENT REQUIRED:\nPlease select both an assigned Truck Vehicle and Driver before saving this dispatch order.", "Fleet Assignment Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    if (cbTruck.SelectedIndex < 0) cbTruck.Focus();

                    else cbDriver.Focus();

                    return;

                }



                var selTr = trucks[cbTruck.SelectedIndex];

                var selDr = drivers[cbDriver.SelectedIndex];

                var (aideIdsForSave, aideNamesForSave) = GetSelectedAides();



                if (aideIdsForSave.Count == 0)

                {

                    MessageBox.Show("Please assign at least 1 aide / helper to this trip. Every driver needs an aide.", "Aide required", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    return;

                }

                int primaryAideId = aideIdsForSave.Count > 0 ? aideIdsForSave[0] : 0;

                string primaryAideName = aideNamesForSave.Count > 0 ? aideNamesForSave[0] : "None";



                bool isFutureDispatch = dtpDispatchDate.Value.Date > DateTime.Today;



                if (!isFutureDispatch)

                {

                    if (!selDr.IsAvailable)

                    {

                        MessageBox.Show($"DRIVER ON TRIP:\nDriver '{selDr.FullName}' is currently marked as 'On Trip' and cannot be dispatched today.", "Resource Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                        return;

                    }



                    if (!selTr.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) && !selTr.Status.Equals("Available", StringComparison.OrdinalIgnoreCase))

                    {

                        MessageBox.Show($"TRUCK UNAVAILABLE:\nVehicle '{selTr.PlateNumber}' is currently marked as '{selTr.Status}' and cannot be dispatched today.", "Resource Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                        return;

                    }



                    foreach (int aideId in aideIdsForSave)

                    {

                        var aide = aides.Find(a => a.Id == aideId);

                        if (aide != null && !aide.IsAvailable)

                        {

                            MessageBox.Show($"AIDE/HELPER ON TRIP:\nHelper '{aide.FullName}' is currently marked as 'On Trip' and cannot be dispatched today.", "Resource Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                            return;

                        }

                    }

                }

                else

                {

                    if (MJdatabase.IsTruckBlockedStatus(selTr.Status))

                    {

                        MessageBox.Show($"TRUCK NOT AVAILABLE:\nVehicle '{selTr.PlateNumber}' is marked as '{selTr.Status}' and cannot be scheduled.", "Resource Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                        return;

                    }

                }



                string? conflict = MJdatabase.ValidateDispatchConflict(0, selTr.Id, selDr.Id, aideIdsForSave, dtpDispatchDate.Value);

                if (!string.IsNullOrEmpty(conflict))

                {

                    MessageBox.Show(conflict, "Scheduling & Delivery Conflict", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                    return;

                }



                int g = (int)numGCrate.Value;

                int ec = (int)numECrate.Value;

                int total = g + ec;

                if (total <= 0)

                {

                    MessageBox.Show("Please enter at least 1 crate for dispatch.", "Crate Quantity Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    return;

                }

                int capCold = 0, capDry = 0, capOver = 0;

                string capReason = "";

                if (selTr != null && !PassCapacityGate(selTr, total, out capCold, out capDry, out capOver, out capReason)) return;

                bool isDuplicate = MJdatabase.GetDeliveries().Any(x => x.DispatchNumber.Equals(dspNum, StringComparison.OrdinalIgnoreCase));

                if (isDuplicate)

                {

                    MessageBox.Show($"DISPATCH NUMBER ALREADY EXISTS:\nDispatch Number '{dspNum}' is already assigned to an existing delivery order. Please enter a unique dispatch number.", "Order Number Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                    txtDispatchNumber.Focus();

                    return;

                }

                if (gridRouteStores != null && pnlRouteStoresBreakdown != null && pnlRouteStoresBreakdown.Visible && gridRouteStores.Rows.Count > 0)
                {
                    SyncNumericCratesToStoreGrid();
                }

                DateTime schedDeparture = MJdatabase.CombineDateAndTime(dtpDispatchDate.Value, cbDepartureTime.Text.Trim());

                if (schedDeparture < DateTime.Now.AddMinutes(-5))

                {

                    var pastConfirm = MessageBox.Show($"PAST DATE NOTICE:\nThe scheduled dispatch date/time ({schedDeparture:yyyy-MM-dd hh:mm tt}) is in the past.\n\nAre you sure you want to proceed with recording this past dispatch?", "Past Schedule Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (pastConfirm != DialogResult.Yes) return;

                }



                string status = cbStatus.SelectedItem?.ToString() ?? "Scheduled";

                string cancellationReason = "";

                if (status == "Cancelled")

                {

                    string? reason = CancellationReasonDialog.PromptCancellationReason(this.FindForm() ?? this.ParentForm!, "Dispatch Order");

                    if (string.IsNullOrEmpty(reason)) return;

                    cancellationReason = reason;

                }



                var routeStops = CollectRouteStoresFromGrid(clientName, destination);
                bool hasMultiStops = routeStops.Count > 1;
                string lsInfo = $"{clientName} ({destination})";

                Delivery d = new Delivery
                {
                    DispatchNumber = dspNum,
                    ClientName = clientName,
                    Destination = destination,
                    IsPeak = chkIsPeak.Checked,
                    CargoType = selTr!.CargoType, // Nov 6 workflow: the truck's own type (no cargo dropdown)
                    LoadingScheduleId = 0,
                    LoadingScheduleInfo = lsInfo,
                    TruckId = selTr!.Id,
                    TruckInfo = $"{selTr.PlateNumber} ({selTr.TruckType})",
                    DriverId = selDr.Id,
                    DriverName = selDr.FullName,
                    AideId = primaryAideId,
                    AideName = primaryAideName,
                    AideIds = string.Join(",", aideIdsForSave),
                    AideNames = aideNamesForSave.Count > 0 ? string.Join(", ", aideNamesForSave) : "None",
                    GCrate = g,
                    ECrate = ec,
                    TotalCrates = total,
                    DispatchDate = dtpDispatchDate.Value,
                    DepartureTime = cbDepartureTime.Text.Trim(),
                    Status = status,
                    Remarks = hasMultiStops ? $"Route Breakdown ({routeStops.Count} stores)" : "Dispatched Delivery Order",
                    CancellationReason = cancellationReason,
                    IsMultiDrop = hasMultiStops,
                    TotalStopsCount = Math.Max(1, routeStops.Count),
                    Stops = routeStops
                };



                MJdatabase.SaveLookupItem("Client Company", clientName);

                SaveNewDestinationIfNeeded(destination);

                if (isMulti && currentStops != null)

                {

                    foreach (var stop in currentStops)

                    {

                        if (!string.IsNullOrWhiteSpace(stop.ClientName)) MJdatabase.SaveLookupItem("Client Company", stop.ClientName);

                        if (!string.IsNullOrWhiteSpace(stop.Destination)) SaveNewDestinationIfNeeded(stop.Destination);

                    }

                }



                MJdatabase.SaveDelivery(d);
                try { MJdatabase.SetStopTypes(d.Id, CollectStopTypes()); } catch (Exception stx) { ErrLog.Swallowed(stx); }
                AppEvents.Raise(AppDataKind.Dispatch);
                try { MJdatabase.SetDeliveryCapacityInfo(d.Id, d.DispatchNumber, capCold, capDry, capOver, capReason); } catch (Exception cx) { ErrLog.Swallowed(cx); }

                MJdatabase.ClearFormDraft("Dispatch", SessionContext.CurrentUsername);

                if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;

                QuietBox.Show("Dispatch & Loading Order created and crew assigned successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearFields();

                LoadDropdownsAndGrid();

                SetFormVisibility(false);

            }

            catch (Exception ex)

            { ErrLog.Error("DispatchManagementView", ex); 

                MessageBox.Show($"Failed to create dispatch order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }



        private void BtnReschedule_Click(object? sender, EventArgs e)
        {
            string dNum = txtDispatchNumber.Text.Trim();
            var target = selectedDelivery ?? allDispatches.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.DispatchNumber) && d.DispatchNumber.Equals(dNum, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                QuietBox.Show("Please select an active dispatch order to reschedule.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;
            }

            if (target.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Dispatch #{target.DispatchNumber} has already been completed and cannot be rescheduled.", "Reschedule Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (target.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Dispatch #{target.DispatchNumber} is cancelled and cannot be rescheduled.", "Reschedule Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dtpDispatchDate.Value >= dtpDispatchDate.MinDate && dtpDispatchDate.Value <= dtpDispatchDate.MaxDate)
            {
                target.DispatchDate = dtpDispatchDate.Value.Date;
            }
            if (!string.IsNullOrWhiteSpace(cbDepartureTime.Text))
            {
                target.DepartureTime = cbDepartureTime.Text.Trim();
            }
            if (cbTruck.SelectedIndex >= 0 && cbTruck.SelectedIndex < trucks.Count)
            {
                target.TruckId = trucks[cbTruck.SelectedIndex].Id;
                target.TruckInfo = $"{trucks[cbTruck.SelectedIndex].PlateNumber} [{trucks[cbTruck.SelectedIndex].TruckType}]";
            }
            if (cbDriver.SelectedIndex >= 0 && cbDriver.SelectedIndex < drivers.Count)
            {
                target.DriverId = drivers[cbDriver.SelectedIndex].Id;
                target.DriverName = drivers[cbDriver.SelectedIndex].FullName;
            }
            var (activeAideIds, activeAideNames) = GetSelectedAides();
            if (activeAideIds.Count > 0)
            {
                target.AideId = activeAideIds[0];
                target.AideName = string.Join(", ", activeAideNames);
            }

            using (var dlg = new RescheduleDeliveryDialog(target))
            {
                if (dlg.ShowDialog(this.FindForm() ?? this.ParentForm) == DialogResult.OK)
                {
                    LoadDropdownsAndGrid();
                    var reloaded = MJdatabase.GetDeliveryById(target.Id);
                    if (reloaded != null)
                    {
                        BindDeliveryToForm(reloaded);
                        cbStatus.Text = "Rescheduled";
                    }
                    else
                    {
                        target.DispatchDate = dlg.RescheduledDate;
                        target.DepartureTime = dlg.RescheduledTime;
                        target.Status = "Rescheduled";
                        BindDeliveryToForm(target);
                    }
                }
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)

        {

            try

            {

                if (selectedDelivery == null)

                {

                    MessageBox.Show("Please select a dispatch order to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    return;

                }



                string dspNum = txtDispatchNumber.Text.Trim();

                if (string.IsNullOrWhiteSpace(dspNum))

                {

                    dspNum = selectedDelivery.DispatchNumber;

                }



                bool isMulti = chkMultiDrop.Checked;

                string clientName = cbClient.Text.Trim();

                string statusForStores = cbStatus.SelectedItem?.ToString() ?? "Scheduled";

                if (statusForStores != "Cancelled" && statusForStores != "Completed" && !ValidateStoreRows()) return;

                string destination = ResolveDestinationFromStores(cbDestination.Text.Trim());

                if (destination.Length > 0 && !string.Equals(cbDestination.Text.Trim(), destination, StringComparison.Ordinal))

                    WhileBinding(() => cbDestination.Text = destination);

                var matchedRoute = MJdatabase.FindRouteByNameOrCode(destination);

                if (matchedRoute != null && matchedRoute.PlacesList.Count > 0)

                {

                    {

                        // List the stores actually on this order (not every store of the route).

                        var orderStores = GetGridStoreList().Select(x => x.Name.Length > 0 ? x.Name : x.Code).Where(x => x.Length > 0).ToList();

                        destination = $"{matchedRoute.RouteCode} - {matchedRoute.RouteName}: {(orderStores.Count > 0 ? string.Join(" ➔ ", orderStores) : matchedRoute.FormattedPlaces)}";

                    }

                }



                if (!isMulti)

                {

                    if (string.IsNullOrWhiteSpace(clientName))

                    {

                        MessageBox.Show("Please select or enter a Client Company name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        cbClient.Focus();

                        return;

                    }

                    if (string.IsNullOrWhiteSpace(destination))

                    {

                        MessageBox.Show("Please add at least one store to this order (Stores & crates → Add).", "Store required", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        cbDestination.Focus();

                        return;

                    }

                }

                else

                {

                    if (currentStops.Count == 0)

                    {

                        MessageBox.Show("Please add at least 1 destination stop to configure the multi-drop route.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        return;

                    }

                    clientName = currentStops[0].ClientName;

                    destination = currentStops[0].Destination;

                }



                if (clientName.Length > 150)

                {

                    MessageBox.Show("Client name cannot exceed 150 characters.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    cbClient.Focus();

                    return;

                }

                if (destination.Length > 500)

                {

                    MessageBox.Show("Destination cannot exceed 200 characters.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    cbDestination.Focus();

                    return;

                }



                if (cbTruck.SelectedIndex < 0 || cbDriver.SelectedIndex < 0)

                {

                    MessageBox.Show("STRICT FLEET ASSIGNMENT REQUIRED:\nPlease select both an assigned Truck Vehicle and Driver.", "Fleet Assignment Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    return;

                }



                var selTr = trucks[cbTruck.SelectedIndex];

                var selDr = drivers[cbDriver.SelectedIndex];

                var (aideIdsForUpdate, aideNamesForUpdate) = GetSelectedAides();

                string statusForAide = cbStatus.SelectedItem?.ToString() ?? "Scheduled";

                if (aideIdsForUpdate.Count == 0 && statusForAide != "Cancelled" && statusForAide != "Completed")

                {

                    MessageBox.Show("Please assign at least 1 aide / helper to this trip. Every driver needs an aide.", "Aide required", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    return;

                }

                int primaryAideId = aideIdsForUpdate.Count > 0 ? aideIdsForUpdate[0] : 0;

                string primaryAideName = aideNamesForUpdate.Count > 0 ? aideNamesForUpdate[0] : "None";



                string newStatus = cbStatus.SelectedItem?.ToString() ?? "Scheduled";

                bool closingTrip = newStatus == "Cancelled" || newStatus == "Completed";

                string? conflict = closingTrip ? null : MJdatabase.ValidateDispatchConflict(selectedDelivery.Id, selTr.Id, selDr.Id, aideIdsForUpdate, dtpDispatchDate.Value);

                if (!string.IsNullOrEmpty(conflict))

                {

                    MessageBox.Show(conflict, "Scheduling & Delivery Conflict", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return;

                }



                string cancellationReason = selectedDelivery.CancellationReason;
                bool cancelChargeChosen = false, cancelChargeable = false;
                decimal cancelFee = 0m;



                if (newStatus == "Cancelled" && selectedDelivery.Status != "Cancelled")

                {

                    decimal rateFee = 0m;
                    try { rateFee = MJdatabase.CalculateDeliveryTariff(selectedDelivery, out _); } catch (Exception rex) { ErrLog.Swallowed(rex); }
                    string? reason = CancellationReasonDialog.PromptDispatchCancellation(this.FindForm() ?? this.ParentForm!, selectedDelivery.DispatchNumber, selectedDelivery.ClientName, rateFee, out cancelChargeable, out cancelFee);
                    if (string.IsNullOrEmpty(reason)) return;
                    cancellationReason = reason;
                    cancelChargeChosen = true;

                }



                int g = (int)numGCrate.Value;

                int ec = (int)numECrate.Value;

                int total = g + ec;

                if (total <= 0)

                {

                    MessageBox.Show("Please enter at least 1 crate for dispatch.", "Crate Quantity Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    return;

                }

                int capCold = 0, capDry = 0, capOver = 0;

                string capReason = "";

                // Closing an old trip (Cancelled / Completed) doesn't re-check the truck's capacity.

                if (!closingTrip && selTr != null && !PassCapacityGate(selTr, total, out capCold, out capDry, out capOver, out capReason)) return;

                if (closingTrip) { var keep = MJdatabase.GetDeliveryCapacityInfo(selectedDelivery.Id); (capCold, capDry, capOver, capReason) = (keep.Cold, keep.Dry, keep.Over, keep.Reason); }

                bool isDuplicateUpdate = MJdatabase.GetDeliveries().Any(x => x.DispatchNumber.Equals(dspNum, StringComparison.OrdinalIgnoreCase) && x.Id != selectedDelivery.Id);

                if (isDuplicateUpdate)

                {

                    MessageBox.Show($"DISPATCH NUMBER ALREADY EXISTS:\nDispatch Number '{dspNum}' is already assigned to another delivery order. Please enter a unique dispatch number.", "Order Number Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                    txtDispatchNumber.Focus();

                    return;

                }

                if (gridRouteStores != null && pnlRouteStoresBreakdown != null && pnlRouteStoresBreakdown.Visible && gridRouteStores.Rows.Count > 0)
                {
                    SyncNumericCratesToStoreGrid();
                }



                string lsInfo = isMulti

                    ? string.Join(" ➔ ", currentStops.Select(s => $"{s.ClientName} ({s.Destination})"))

                    : $"{clientName} ({destination})";



                selectedDelivery.DispatchNumber = dspNum;

                selectedDelivery.ClientName = clientName;

                selectedDelivery.Destination = destination;

                selectedDelivery.IsPeak = chkIsPeak.Checked;

                selectedDelivery.CargoType = !string.IsNullOrEmpty(cbCargoType.Text) ? cbCargoType.Text : selTr!.CargoType;

                selectedDelivery.LoadingScheduleInfo = lsInfo;

                selectedDelivery.TruckId = selTr!.Id;

                selectedDelivery.TruckInfo = $"{selTr.PlateNumber} ({selTr.TruckType})";

                selectedDelivery.DriverId = selDr.Id;

                selectedDelivery.DriverName = selDr.FullName;



                var routeStopsUpd = CollectRouteStoresFromGrid(clientName, destination);
                selectedDelivery.IsMultiDrop = routeStopsUpd.Count > 1;
                selectedDelivery.TotalStopsCount = Math.Max(1, routeStopsUpd.Count);
                selectedDelivery.Stops = routeStopsUpd;



                selectedDelivery.DispatchDate = dtpDispatchDate.Value;

                selectedDelivery.DepartureTime = cbDepartureTime.Text.Trim();

                selectedDelivery.Status = newStatus;

                selectedDelivery.GCrate = g;

                selectedDelivery.ECrate = ec;

                selectedDelivery.TotalCrates = total;

                selectedDelivery.AideId = primaryAideId;

                selectedDelivery.AideName = primaryAideName;

                selectedDelivery.AideIds = string.Join(",", aideIdsForUpdate);

                selectedDelivery.AideNames = aideNamesForUpdate.Count > 0 ? string.Join(", ", aideNamesForUpdate) : "None";

                selectedDelivery.CancellationReason = cancellationReason;



                MJdatabase.SaveLookupItem("Client Company", clientName);

                SaveNewDestinationIfNeeded(destination);

                if (isMulti && currentStops != null)

                {

                    foreach (var stop in currentStops)

                    {

                        if (!string.IsNullOrWhiteSpace(stop.ClientName)) MJdatabase.SaveLookupItem("Client Company", stop.ClientName);

                        if (!string.IsNullOrWhiteSpace(stop.Destination)) SaveNewDestinationIfNeeded(stop.Destination);

                    }

                }



                MJdatabase.SaveDelivery(selectedDelivery);
                try { MJdatabase.SetStopTypes(selectedDelivery.Id, CollectStopTypes()); } catch (Exception stx) { ErrLog.Swallowed(stx); }
                AppEvents.Raise(AppDataKind.Dispatch);
                if (cancelChargeChosen) MJdatabase.SetDeliveryCancelCharge(selectedDelivery.Id, cancelChargeable, cancelFee);
                try { MJdatabase.SetDeliveryCapacityInfo(selectedDelivery.Id, selectedDelivery.DispatchNumber, capCold, capDry, capOver, capReason); } catch (Exception cx) { ErrLog.Swallowed(cx); }

                QuietBox.Show("Dispatch & Loading Order updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearFields();

                LoadDropdownsAndGrid();

                SetFormVisibility(false);

            }

            catch (Exception ex)

            { ErrLog.Error("DispatchManagementView", ex); 

                MessageBox.Show($"Failed to update dispatch order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }



        private void BtnDelete_Click(object? sender, EventArgs e)

        {

            try

            {

                if (selectedDelivery == null)

                {

                    MessageBox.Show("Please select a dispatch order to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    return;

                }



                string? deleteBlock = MJdatabase.ValidateRecordDeletion("Deliveries", selectedDelivery.Id);

                if (!string.IsNullOrEmpty(deleteBlock))

                {

                    MessageBox.Show(deleteBlock, "Deletion Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    return;

                }



                var res = MessageBox.Show($"Are you sure you want to delete Dispatch Order {selectedDelivery.DispatchNumber}?\n\n" + MJdatabase.DeletePolicyNote("Deliveries"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

                if (res == DialogResult.Yes)

                {

                    bool deleted = MJdatabase.DeleteRecord("Deliveries", selectedDelivery.Id);

                    if (deleted)

                    {

                        QuietBox.Show(MJdatabase.LastDeleteSummary, "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        ClearFields();

                        LoadDropdownsAndGrid();

                    }

                    else

                    {

                        MessageBox.Show("Failed to delete dispatch order. The record may be referenced by other data or an error occurred.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    }

                }

            }

            catch (Exception ex)

            { ErrLog.Error("DispatchManagementView", ex); 

                MessageBox.Show($"Failed to delete dispatch order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }



        private void BtnClear_Click(object? sender, EventArgs e)

        {

            ClearFields();

            // A reset form must not come back as a "restored draft" next time.

            try { MJdatabase.ClearFormDraft("Dispatch", SessionContext.CurrentUsername); } catch (Exception ex) { ErrLog.Swallowed(ex); }

        }



        private void InitializeSummaryBadges()

        {

            if (pnlSummary == null) return;

            pnlSummary.Visible = false;

            lblSummary.Visible = false;



            flowSummaryBadges = new FlowLayoutPanel

            {

                Dock = DockStyle.Fill,

                FlowDirection = FlowDirection.LeftToRight,

                WrapContents = false,

                AutoScroll = true,

                BackColor = Pal.C(248, 250, 252),

                Padding = new Padding(0)

            };



            btnFilterTotal = CreateSummaryPill("📊 Total Orders: 0", () => { currentStatusFilter = ""; FilterGridData(); UpdateSummaryButtonStyles(); });

            btnFilterPending = CreateSummaryPill("📋 Scheduled: 0", () => { currentStatusFilter = (currentStatusFilter == "Scheduled" || currentStatusFilter == "Pending") ? "" : "Scheduled"; FilterGridData(); UpdateSummaryButtonStyles(); });

            btnFilterTransit = CreateSummaryPill("🚚 In Transit: 0", () => { currentStatusFilter = currentStatusFilter == "In Transit" ? "" : "In Transit"; FilterGridData(); UpdateSummaryButtonStyles(); });

            btnFilterCompleted = CreateSummaryPill("✅ Completed: 0 ➔", () => OnNavigateToCompletedRequested?.Invoke());



            lblSummaryVolume = new Label

            {

                Text = "📦 Total Volume: 0 Crates",

                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),

                ForeColor = Pal.C(71, 85, 105),

                AutoSize = true,

                Margin = new Padding(12, 5, 4, 4)

            };



            flowSummaryBadges.Controls.Add(btnFilterTotal);

            flowSummaryBadges.Controls.Add(btnFilterPending);

            flowSummaryBadges.Controls.Add(btnFilterTransit);

            
            flowSummaryBadges.Controls.Add(btnFilterCompleted);

            flowSummaryBadges.Controls.Add(lblSummaryVolume);



            pnlSummary.Controls.Add(flowSummaryBadges);

            flowSummaryBadges.BringToFront();

            UpdateSummaryButtonStyles();

        }



        private Button CreateSummaryPill(string text, Action onClick)

        {

            var btn = new Button

            {

                Text = text,

                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),

                AutoSize = true,

                AutoSizeMode = AutoSizeMode.GrowOnly,

                Height = 26,

                FlatStyle = FlatStyle.Flat,

                Cursor = Cursors.Hand,

                Margin = new Padding(2, 2, 6, 2),

                Padding = new Padding(8, 0, 8, 0)

            };

            btn.FlatAppearance.BorderSize = 1;

            btn.Click += (s, e) => onClick();

            return btn;

        }



        private void UpdateSummaryBadges(int total, int pending, int transit, int completed, int volume)

        {

            if (btnFilterTotal != null && btnFilterPending != null && btnFilterTransit != null && btnFilterCompleted != null && lblSummaryVolume != null)

            {

                btnFilterTotal.Text = $"📊 Total Orders: {total:N0}";

                btnFilterPending.Text = $"📋 Scheduled: {pending:N0}";

                btnFilterTransit.Text = $"🚚 In Transit: {transit:N0}";

                btnFilterCompleted.Text = $"✅ Completed: {completed:N0} ➔";

                lblSummaryVolume.Text = $"📦 Total Volume: {volume:N0} Crates";

                UpdateSummaryButtonStyles();

            }

            else if (lblSummary != null)

            {

                lblSummary.Text = $"📊 Total Orders: {total:N0}  |  ⏳ Pending: {pending:N0}  |  🚚 In Transit: {transit:N0}  |  ✅ Completed: {completed:N0}  |  📦 Total Volume: {volume:N0} Crates";

            }

        }



        private void UpdateSummaryButtonStyles()

        {

            if (btnFilterTotal == null || btnFilterPending == null || btnFilterTransit == null || btnFilterCompleted == null) return;



            ApplyPillStyle(btnFilterTotal, string.IsNullOrEmpty(currentStatusFilter), Pal.C(15, 23, 42), Pal.White, Pal.C(241, 245, 249), Pal.C(71, 85, 105));

            ApplyPillStyle(btnFilterPending, currentStatusFilter == "Scheduled" || currentStatusFilter == "Pending", Pal.C(217, 119, 6), Pal.White, Pal.C(254, 243, 199), Pal.C(180, 83, 9));

            ApplyPillStyle(btnFilterTransit, currentStatusFilter == "In Transit", Pal.C(37, 99, 235), Pal.White, Pal.C(219, 234, 254), Pal.C(29, 78, 216));

            btnFilterCompleted.BackColor = Pal.C(240, 253, 244);

            btnFilterCompleted.ForeColor = Pal.C(22, 101, 52);

            btnFilterCompleted.FlatAppearance.BorderColor = Pal.C(187, 247, 208);

        }



        private void ApplyPillStyle(Button btn, bool isActive, Color activeBg, Color activeFg, Color inactiveBg, Color inactiveFg)

        {

            if (isActive)

            {

                btn.BackColor = activeBg;

                btn.ForeColor = activeFg;

                btn.FlatAppearance.BorderColor = activeBg;

            }

            else

            {

                btn.BackColor = inactiveBg;

                btn.ForeColor = inactiveFg;

                btn.FlatAppearance.BorderColor = Pal.C(226, 232, 240);

            }

        }



        /// <summary>Active first: awaiting completion, in transit, delayed, scheduled; then completed, then cancelled.</summary>

        private static int StatusRank(Delivery d)

        {
            if (MJdatabase.IsAwaitingCompletion(d)) return 0;

            string status = d.Status ?? "";

            if (RowMenu.Is(status, "In Transit")) return 1;

            if (RowMenu.Is(status, "Delayed")) return 2;

            if (RowMenu.Is(status, "Scheduled", "Pending")) return 3;

            if (RowMenu.Is(status, "Completed")) return 5;

            if (RowMenu.Is(status, "Cancelled")) return 6;

            return 4;
        }

        private void FilterGridData()
        {
            string q = txtGridSearch.Text.Trim().ToLowerInvariant();
            var query = allDispatches.AsEnumerable();
            if (string.IsNullOrEmpty(currentStatusFilter))

            {

                if (!showFinished) query = query.Where(d => !IsFinished(d));

            }

            else if (currentStatusFilter != AllOrdersFilter)

            {
                if (currentStatusFilter == AwaitingCompletionFilter)

                {

                    query = query.Where(MJdatabase.IsAwaitingCompletion);

                }

                else if (currentStatusFilter == "Scheduled" || currentStatusFilter == "Pending")

                {
                    query = query.Where(d => string.Equals(d.Status, "Scheduled", StringComparison.OrdinalIgnoreCase) || string.Equals(d.Status, "Pending", StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    query = query.Where(d => string.Equals(d.Status, currentStatusFilter, StringComparison.OrdinalIgnoreCase));
                }
            }

            var dr = DateFilter;

            if (!dr.IsAll) query = query.Where(d => dr.Contains(d.DispatchDate));

            var matching = string.IsNullOrEmpty(q)
                ? query
                : query.Where(d =>
                    (!string.IsNullOrEmpty(d.DispatchNumber) && d.DispatchNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(d.ClientName) && d.ClientName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(d.Destination) && d.Destination.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(d.LoadingScheduleInfo) && d.LoadingScheduleInfo.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(d.TruckInfo) && d.TruckInfo.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(d.DriverName) && d.DriverName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(d.AideNames) && d.AideNames.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(d.CargoType) && d.CargoType.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(d.Status) && d.Status.ToLower().Contains(q))
                );

            IEnumerable<Delivery> sorted = ApplySort(matching);

            // Finished orders always sit under the active ones (stable re-order keeps the chosen sort inside each part).

            if (currentSortColumn != "Status") sorted = sorted.OrderBy(d => IsFinished(d) ? 1 : 0);

            var filtered = sorted.ToList();
            SyncPickers();

            gridDispatch.DataSource = null;
            gridDispatch.DataSource = filtered;

            UpdateDispatchSummary(filtered);

            string filterTag = currentStatusFilter == AwaitingCompletionFilter ? " [Awaiting completion]"

                : currentStatusFilter == AllOrdersFilter ? " [All orders]"

                : !string.IsNullOrEmpty(currentStatusFilter) ? $" [{currentStatusFilter}]" : "";

            UpdateAwaitingCompletionBadge();

            UpdateShowFinishedButton();

            if (!dr.IsAll) filterTag += $" [{dr.Describe(DatePresets.Orders)}]";

            int activeCount = allDispatches.Count(d => !IsFinished(d)), finishedCount = allDispatches.Count - activeCount;

            string baseCount = showFinished ? $"{activeCount} active · {finishedCount} finished" : $"{activeCount} active";

            lblGridHeader.Text = string.IsNullOrEmpty(q) && string.IsNullOrEmpty(currentStatusFilter) && dr.IsAll

                ? $"Delivery & Dispatch Orders ({baseCount})"

                : $"Delivery & Dispatch Orders{filterTag} - Showing {filtered.Count} of {allDispatches.Count}";



            if (filtered.Count > 0)
            {
                gridDispatch.ClearSelection();
            }

            else
            {
                isBindingForm = true;
                selectedDelivery = null;
                SetButtonStates(false);
                txtDispatchNumber.Text = "";
                numGCrate.Value = 0;
                numECrate.Value = 0;
                txtTotalCrates.Text = "";
                lblRec.Text = "No matching dispatch orders found.";
                lblFormHeader.Text = "New Dispatch Order";

                isBindingForm = false;
            }

        }



        private void TxtGridSearch_TextChanged(object? sender, EventArgs e)

        {

            FilterGridData();

        }



        private void BtnClearSearch_Click(object? sender, EventArgs e)

        {

            txtGridSearch.Text = "";

            txtGridSearch.Focus();

        }



        private void BtnPrint_Click(object? sender, EventArgs e)

        {

            try

            {

                PrintHelper.PrintGridReport("Delivery Dispatch Manifest Report", gridDispatch, "Official transport dispatch order register with complete vehicle, crew, and crate details.");

            }

            catch (Exception ex)

            { ErrLog.Error("DispatchManagementView", ex); 

                MessageBox.Show($"Failed to generate print report: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }



        private void ClearFields()
        {
            isBindingForm = true;
            try
            {
                selectedDelivery = null;
                if (numCombiCold != null) numCombiCold.Value = 0;

                txtDispatchNumber.Text = "";
                cbClient.SelectedIndex = -1;
                cbClient.Text = "";
                cbDestination.SelectedIndex = -1;
                cbDestination.Text = "";
                cbCargoType.SelectedIndex = -1;
                cbCargoType.Text = "";
                chkIsPeak.Checked = false;
                cbTruck.SelectedIndex = -1;
                cbTruck.Text = "";
                cbDriver.SelectedIndex = -1;
                cbDriver.Text = "";
                dtpDispatchDate.Value = DateTime.Today;
                cbDepartureTime.SelectedIndex = -1;
                cbDepartureTime.Text = "";
                if (cbStatus.Items.Count > 0) cbStatus.SelectedIndex = 0;
                numGCrate.Value = 0;
                numECrate.Value = 0;
                txtTotalCrates.Text = "0 Crates";
                selectedAideIds.Clear();
                txtSearchAides.Clear();
                chkMultiDrop.Checked = false;
                currentStops.Clear();
                storeListRoute = null;

                if (gridRouteStores != null) gridRouteStores.Rows.Clear();
                // The store table is always shown: the route box is gone, so stores are added here directly.

                if (pnlRouteStoresBreakdown != null) { pnlRouteStoresBreakdown.Visible = true; SizeStoreBreakdown(0); }

                if (lblRouteStoresHeader != null) lblRouteStoresHeader.Text = "🏪 Stores & crates for this order";

                UpdateSaveStoresToRouteButton();

                lblRec.Text = "Select route and crates to view recommendation...";
                lblRec.ForeColor = Pal.C(71, 85, 105);

                lblFormHeader.Text = "New Dispatch Order";

                txtDispatchNumber.Enabled = true;
                cbClient.Enabled = true;
                cbDestination.Enabled = true;
                cbCargoType.Enabled = true;
                chkIsPeak.Enabled = true;
                cbTruck.Enabled = true;
                cbDriver.Enabled = true;
                flowAidesModule.Enabled = true;

                PopulateTrucksDriversAndAides();
            }
            finally
            {
                isBindingForm = false;
            }

            SetButtonStates(false);
            gridDispatch.ClearSelection();

            txtSearchAides.Enabled = true;



            btnClearAides.Enabled = true;

            numGCrate.Enabled = true;

            numECrate.Enabled = true;

            dtpDispatchDate.Enabled = true;

            cbDepartureTime.Enabled = true;

            btnPickAnalogTime.Enabled = true;



            ApplyDispatchNumberMode();

            lblFormHeader.Text = string.IsNullOrWhiteSpace(txtDispatchNumber.Text) ? "New Dispatch Order" : $"New Dispatch Order  ·  {txtDispatchNumber.Text}";

            lblRec.Text = "💡 Select truck vehicle and driver to verify capacity match.";

            pnlRec.BackColor = Pal.C(248, 250, 252);

            lblRec.ForeColor = Pal.C(71, 85, 105);

            UpdateDestinationHint();

        }



        private HashSet<int> selectedAideIds = new HashSet<int>();



        private void SetupAideModuleEvents()
        {
            txtSearchAides.TextChanged += (s, e) => RenderAidesModule();

            btnClearAides.Click += (s, e) =>
            {
                selectedAideIds.Clear();
                txtSearchAides.Clear();
                RenderAidesModule();
            };

            if (btnClearAides.Parent != null)
            {
                var btnAutoPickTwoAides = new Button
                {
                    Text = "⚡ 2 Aides",
                    Dock = DockStyle.Right,
                    Width = 72,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Pal.C(220, 252, 231),

                    ForeColor = Pal.C(22, 101, 52),

                    Font = new Font("Segoe UI Semibold", 7.8F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnAutoPickTwoAides.FlatAppearance.BorderSize = 0;
                // Sized to its text so it isn't cut off on high-DPI screens.

                btnAutoPickTwoAides.Width = TextRenderer.MeasureText(btnAutoPickTwoAides.Text, btnAutoPickTwoAides.Font).Width + Dpi(22);

                tipRouteStores.SetToolTip(btnAutoPickTwoAides, "Picks the 2 free aides with the fewest trips");

                btnAutoPickTwoAides.Click += (s, e) =>
                {
                    selectedAideIds.Clear();
                    var bestTwo = aides
                        .Where(a => CrewFree(a))

                        .OrderBy(a => GetCrewTripCount(a.Id, false))
                        .ThenBy(a => a.FullName)
                        .Take(2)
                        .ToList();
                    foreach (var a in bestTwo)
                    {
                        selectedAideIds.Add(a.Id);
                    }
                    RenderAidesModule();
                };
                btnClearAides.Parent.Controls.Add(btnAutoPickTwoAides);
            }
        }

        // Rule 16b: two explicit aide slots (max 2), each a large searchable list that hides the other slot's pick.
        private ComboBox? cbAideSlot1, cbAideSlot2;
        private Label? lblAideCount;
        private Panel? pnlAideSlots;
        private bool renderingAides;
        private sealed class AideItem
        {
            public Crew Aide { get; }
            public string Label { get; }
            public AideItem(Crew a, string status) { Aide = a; Label = $"{a.FullName}  ·  {status}"; }
            public override string ToString() => Label;
        }
        private List<int> AideSlots() => selectedAideIds.Take(MJdatabase.MaxAidesPerOrder).ToList();
        private void SetAideSlots(int a1, int a2)
        {
            selectedAideIds.Clear();
            if (a1 > 0) selectedAideIds.Add(a1);
            if (a2 > 0 && a2 != a1) selectedAideIds.Add(a2);
        }
        private ComboBox MakeAideSlot()
        {
            var cb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                Font = LargePrint.On ? LargePrint.Bold(12F, 9.75F) : new Font("Segoe UI", 9.75F),
                Width = Dpi(LargePrint.Px(420, 320)),
                Margin = new Padding(0, 0, Dpi(6), 0),
                IntegralHeight = false,
                MaxDropDownItems = 12
            };
            // Typed text must match a listed aide; anything else snaps back to the slot's current aide.
            cb.Leave += (s, e) => { if (!renderingAides && cb.SelectedItem == null && cb.Text.Trim().Length > 0) BeginInvoke(new Action(RenderAidesModule)); };
            cb.SelectionChangeCommitted += (s, e) => BeginInvoke(new Action(() => OnAideSlotPicked(cb)));
            UIThemeHelper.StyleInput(cb);
            return cb;
        }
        private void OnAideSlotPicked(ComboBox cb)
        {
            if (renderingAides) return;
            var slots = AideSlots();
            int a1 = slots.Count > 0 ? slots[0] : 0, a2 = slots.Count > 1 ? slots[1] : 0;
            int pick = (cb.SelectedItem as AideItem)?.Aide.Id ?? 0;
            if (cb == cbAideSlot1) a1 = pick; else a2 = pick;
            if (a1 == 0 && a2 > 0) { a1 = a2; a2 = 0; } // keep Aide 1 filled first
            SetAideSlots(a1, a2);
            RenderAidesModule();
        }
        private void BuildAideSlotsPanel()
        {
            if (pnlAideSlots != null) return;
            pnlAideSlots = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 3, RowCount = 3, Margin = new Padding(0), Padding = new Padding(0, Dpi(2), 0, Dpi(4)) };
            var t = (TableLayoutPanel)pnlAideSlots;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Label Cap(string text) => new Label { Text = text, AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, Dpi(7), Dpi(10), 0), UseMnemonic = false };
            Button Clear(Func<ComboBox?> slot) {
                var b = new Button { Text = "Clear", AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), Cursor = Cursors.Hand, Margin = new Padding(Dpi(6), Dpi(1), 0, 0), MinimumSize = new Size(0, Dpi(30)) };
                UIThemeHelper.SetButtonRole(b, "secondary");
                b.Click += (s, e) => {
                    var slots = AideSlots();
                    int a1 = slots.Count > 0 ? slots[0] : 0, a2 = slots.Count > 1 ? slots[1] : 0;
                    if (slot() == cbAideSlot1) { a1 = a2; a2 = 0; } else a2 = 0;
                    SetAideSlots(a1, a2);
                    RenderAidesModule();
                };
                return b;
            }
            cbAideSlot1 = MakeAideSlot();
            cbAideSlot2 = MakeAideSlot();
            lblAideCount = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 8.75F), Padding = new Padding(Dpi(8), Dpi(3), Dpi(8), Dpi(3)), Margin = new Padding(0, Dpi(9), 0, 0), UseMnemonic = false };
            var auto = new Button { Text = "Pick 2 free aides", AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), Cursor = Cursors.Hand, Margin = new Padding(Dpi(10), Dpi(6), 0, 0), MinimumSize = new Size(0, Dpi(30)) };
            UIThemeHelper.SetButtonRole(auto, "secondary");
            tipRouteStores.SetToolTip(auto, "Picks the 2 free aides with the fewest trips");
            auto.Click += (s, e) =>
            {
                var best = aides.Where(a => CrewFree(a)).OrderBy(a => GetCrewTripCount(a.Id, false)).ThenBy(a => a.FullName).Take(MJdatabase.MaxAidesPerOrder).Select(a => a.Id).ToList();
                SetAideSlots(best.Count > 0 ? best[0] : 0, best.Count > 1 ? best[1] : 0);
                RenderAidesModule();
            };
            t.Controls.Add(Cap("Aide 1 (Primary Helper)"), 0, 0); t.Controls.Add(cbAideSlot1, 1, 0); t.Controls.Add(Clear(() => cbAideSlot1), 2, 0);
            t.Controls.Add(Cap("Aide 2 (Second Helper)"), 0, 1); t.Controls.Add(cbAideSlot2, 1, 1); t.Controls.Add(Clear(() => cbAideSlot2), 2, 1);
            var badgeRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            badgeRow.Controls.Add(lblAideCount); badgeRow.Controls.Add(auto);
            t.Controls.Add(badgeRow, 0, 2); t.SetColumnSpan(badgeRow, 3);
            // The old pill list and search row are replaced by the two slots.
            pnlAidesSearchRow.Visible = false;
            flowAidesModule.Visible = false;
            var host = flowAidesModule.Parent;
            if (host != null) { host.Controls.Add(pnlAideSlots); host.Controls.SetChildIndex(pnlAideSlots, host.Controls.GetChildIndex(flowAidesModule)); }
        }
        private void FillAideSlot(ComboBox cb, int current, int other)
        {
            cb.BeginUpdate();
            cb.Items.Clear();
            // Only active, available aides (or the one already on this order), never the other slot's aide.
            foreach (var a in aides.Where(a => a.Id != other && (CrewFree(a) || a.Id == current)).OrderBy(a => a.FullName))
                cb.Items.Add(new AideItem(a, a.Id == current && !CrewFree(a) ? "On this order" : "Available"));
            cb.SelectedItem = cb.Items.Cast<AideItem>().FirstOrDefault(i => i.Aide.Id == current);
            if (cb.SelectedItem == null) cb.Text = "";
            cb.EndUpdate();
        }
        private void RenderAidesModule()
        {
            if (renderingAides) return;
            renderingAides = true;
            try
            {
                BuildAideSlotsPanel();
                var slots = AideSlots();
                int a1 = slots.Count > 0 ? slots[0] : 0, a2 = slots.Count > 1 ? slots[1] : 0;
                if (selectedAideIds.Count > MJdatabase.MaxAidesPerOrder) SetAideSlots(a1, a2);
                FillAideSlot(cbAideSlot1!, a1, a2);
                FillAideSlot(cbAideSlot2!, a2, a1);
                cbAideSlot2!.Enabled = a1 > 0; // fill Aide 1 first
            }
            finally { renderingAides = false; }
            UpdateAidesSummary();
        }
        private void UpdateAidesSummary()
        {
            int n = Math.Min(selectedAideIds.Count, MJdatabase.MaxAidesPerOrder);
            var selNames = aides.Where(a => selectedAideIds.Contains(a.Id)).Select(a => a.FullName).ToList();
            if (lblAideCount != null)
            {
                bool max = n >= MJdatabase.MaxAidesPerOrder;
                lblAideCount.Text = max ? $"{n} of {MJdatabase.MaxAidesPerOrder} aides assigned · limit reached" : $"{n} of {MJdatabase.MaxAidesPerOrder} aides assigned";
                lblAideCount.BackColor = n == 0 ? AppTheme.SurfaceAlt : max ? AppTheme.SuccessSoft : AppTheme.InfoSoft;
                lblAideCount.ForeColor = n == 0 ? AppTheme.TextMuted : max ? AppTheme.SuccessText : AppTheme.InfoText;
            }
            lblAidesSummary.Text = selNames.Count == 0 ? "No aides assigned yet" : $"Aides: {string.Join(", ", selNames)}";
            lblAidesSummary.ForeColor = selNames.Count == 0 ? AppTheme.TextMuted : AppTheme.SuccessText;
        }
        private void DrawStatusComboBoxItem(ComboBox cb, DrawItemEventArgs e)

        {

            if (e.Index < 0 || e.Index >= cb.Items.Count) return;



            e.DrawBackground();

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            string text = cb.Items[e.Index]?.ToString() ?? "";



            using var g = e.Graphics;

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;



            bool isAssignedToCurrent = false;

            bool isCompleted = selectedDelivery != null && (selectedDelivery.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) || selectedDelivery.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase));

            if (selectedDelivery != null && !isCompleted)

            {

                if (cb == cbTruck && trucks != null && e.Index < trucks.Count && trucks[e.Index].Id == selectedDelivery.TruckId)

                {

                    isAssignedToCurrent = true;

                }

                else if (cb == cbDriver && drivers != null && e.Index < drivers.Count && drivers[e.Index].Id == selectedDelivery.DriverId)

                {

                    isAssignedToCurrent = true;

                }

            }



            bool isAvail = text.Contains("[ACTIVE]", StringComparison.OrdinalIgnoreCase) || text.Contains("[AVAILABLE]", StringComparison.OrdinalIgnoreCase);

            bool isOnTrip = text.Contains("[ON TRIP]", StringComparison.OrdinalIgnoreCase);



            Color dotColor;

            Color badgeBg;

            Color badgeFg;

            string statusTag;



            if (isAssignedToCurrent)

            {

                dotColor = Pal.C(37, 99, 235);

                badgeBg = Pal.C(219, 234, 254);

                badgeFg = Pal.C(30, 64, 175);

                statusTag = "ASSIGNED";

            }

            else if (isAvail)

            {

                dotColor = Pal.C(16, 185, 129);

                badgeBg = Pal.C(220, 252, 231);

                badgeFg = Pal.C(22, 101, 52);

                statusTag = "ACTIVE";

            }

            else if (isOnTrip)

            {

                dotColor = Pal.C(249, 115, 22);

                badgeBg = Pal.C(254, 243, 199);

                badgeFg = Pal.C(180, 83, 9);

                statusTag = "ON TRIP";

            }

            else

            {

                dotColor = Pal.C(239, 68, 68);

                badgeBg = Pal.C(254, 226, 226);

                badgeFg = Pal.C(185, 28, 28);

                statusTag = "BUSY";

            }



            int dotSize = 7;

            int dotY = e.Bounds.Y + (e.Bounds.Height - dotSize) / 2;

            using (var brush = new SolidBrush(dotColor))

            {

                g.FillEllipse(brush, e.Bounds.X + 6, dotY, dotSize, dotSize);

            }



            using var tagFont = new Font("Segoe UI", 7.25F, FontStyle.Bold);

            Size tagTextSize = TextRenderer.MeasureText(g, statusTag, tagFont, new Size(200, e.Bounds.Height), TextFormatFlags.NoPadding);

            int tagW = Math.Max(60, tagTextSize.Width + 12);

            Rectangle tagRect = new Rectangle(e.Bounds.X + 17, e.Bounds.Y + 4, tagW, e.Bounds.Height - 8);

            using (var tagBrush = new SolidBrush(isSelected ? Pal.White : badgeBg))

            {

                g.FillRectangle(tagBrush, tagRect);

            }

            TextRenderer.DrawText(g, statusTag, tagFont, tagRect, isSelected ? Pal.C(15, 23, 42) : badgeFg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

            string cleanText = text.Replace("[ACTIVE]", "").Replace("[AVAILABLE]", "").Replace("[ON TRIP]", "").Replace("[MAINTENANCE]", "").Trim();



            Color textColor = isSelected ? Pal.White : Pal.C(30, 41, 59);

            int textStartX = tagRect.Right + 6;

            Rectangle textRect = new Rectangle(textStartX, e.Bounds.Y, Math.Max(40, e.Bounds.Right - textStartX - 4), e.Bounds.Height);

            TextRenderer.DrawText(g, cleanText, FontCache.Get("Segoe UI Semibold", 8.75F, FontStyle.Bold), textRect, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            e.DrawFocusRectangle();

        }



        [System.ComponentModel.Browsable(false)]

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]

        public Action<Delivery>? OnRecordTripRequested { get; set; }



        [System.ComponentModel.Browsable(false)]

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]

        public Action<Delivery>? OnCreateInvoiceRequested { get; set; }



        private void CheckAndPromptDraft()

        {

            try

            {

                var draft = MJdatabase.GetFormDraft("Dispatch", SessionContext.CurrentUsername);

                if (draft != null && !string.IsNullOrWhiteSpace(draft.Value.Data))

                {

                    RestoreDraft();

                }

            }

            catch (System.Exception swx4) { ErrLog.Swallowed(swx4); }

        }



        private void RestoreDraft()

        {

            try

            {

                var draft = MJdatabase.GetFormDraft("Dispatch", SessionContext.CurrentUsername);

                if (draft == null || string.IsNullOrWhiteSpace(draft.Value.Data)) return;

                var map = DraftAutoSaveService.DeserializeDraft(draft.Value.Data);

                isBindingForm = true;



                if (map.TryGetValue("DispatchNumber", out var dnum) && !string.IsNullOrWhiteSpace(dnum))

                {

                    txtDispatchNumber.Text = dnum;

                }

                if (map.TryGetValue("Client", out var cl) && !string.IsNullOrWhiteSpace(cl))

                {

                    cbClient.Text = cl;

                }

                if (map.TryGetValue("Destination", out var dst) && !string.IsNullOrWhiteSpace(dst))

                {

                    cbDestination.Text = dst;

                }

                if (map.TryGetValue("CargoType", out var ctype) && !string.IsNullOrWhiteSpace(ctype))

                {

                    cbCargoType.Text = ctype;

                }

                if (map.TryGetValue("IsPeak", out var isPeakStr) && bool.TryParse(isPeakStr, out bool isPeakVal))

                {

                    chkIsPeak.Checked = isPeakVal;

                }

                // Truck and driver are found by their id (list positions change with the date and availability).

                if (map.TryGetValue("TruckId", out var tId) && int.TryParse(tId, out int tid) && tid > 0)

                {

                    int ti = trucks.FindIndex(t => t.Id == tid);

                    if (ti >= 0 && ti < cbTruck.Items.Count) cbTruck.SelectedIndex = ti;

                }

                if (map.TryGetValue("DriverId", out var dId) && int.TryParse(dId, out int did) && did > 0)

                {

                    int di = drivers.FindIndex(d => d.Id == did);

                    if (di >= 0 && di < cbDriver.Items.Count) cbDriver.SelectedIndex = di;

                }

                if (map.TryGetValue("CombiCold", out var cc) && decimal.TryParse(cc, out var ccv))

                    numCombiCold.Value = Math.Min(numCombiCold.Maximum, Math.Max(0, ccv));

                // Crates come from the store rows saved with the draft (older drafts without stores restore none).

                RestoreDraftStores(map.TryGetValue("Stores", out var st) ? st : "");

                if (map.TryGetValue("DepartureTime", out var dt) && !string.IsNullOrWhiteSpace(dt))

                {

                    cbDepartureTime.Text = dt;

                }

                if (map.TryGetValue("DispatchDate", out var dd) && DateTime.TryParse(dd, out var ddate) && ddate >= dtpDispatchDate.MinDate && ddate <= dtpDispatchDate.MaxDate)

                {

                    dtpDispatchDate.Value = ddate;

                }

                if (map.TryGetValue("Aides", out var aidesStr) && !string.IsNullOrEmpty(aidesStr))

                {

                    var ids = aidesStr.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    selectedAideIds.Clear();

                    foreach (var idStr in ids)

                    {

                        if (int.TryParse(idStr, out int aid)) selectedAideIds.Add(aid);

                    }

                    RenderAidesModule();

                }



                isBindingForm = false;

                RecalculateTotalsFromRouteStoresGrid();

                CalculateCratesAndRecommendation();

                UpdateDestinationHint();

                AutoFillDispatchNumber();

                ToastHelper.ShowToast(this, "Dispatch draft restored ✓", ToastType.Success);

            }

            catch (Exception ex)

            {

                isBindingForm = false;

                System.Diagnostics.Debug.WriteLine($"[Dispatch] RestoreDraft: {ex.Message}");

                ToastHelper.ShowToast(this, "Could not restore draft.", ToastType.Warning);

            }

        }

        /// <summary>The store rows of the form as one line of text for the draft: code|name|E|G per row, rows split by ";;".</summary>

        private string DraftStoresText()

        {

            if (gridRouteStores == null) return "";

            string V(DataGridViewRow r, string c) => gridRouteStores.Columns.Contains(c) ? (r.Cells[c].Value?.ToString() ?? "").Replace("|", "/").Replace(";;", ";").Trim() : "";

            var parts = new List<string>();

            foreach (DataGridViewRow r in gridRouteStores.Rows)

            {

                if (r.IsNewRow) continue;

                string code = V(r, "colStoreCode"), name = V(r, "colStoreName");

                if (code.Length == 0 && name.Length == 0) continue;

                parts.Add($"{code}|{name}|{V(r, "colECrate")}|{V(r, "colGCrate")}");

            }

            return string.Join(";;", parts);

        }

        private void RestoreDraftStores(string text)

        {

            if (gridRouteStores == null) return;

            gridRouteStores.Rows.Clear();

            foreach (var part in (text ?? "").Split(";;", StringSplitOptions.RemoveEmptyEntries))

            {

                var f = part.Split('|');

                if (f.Length < 4) continue;

                int.TryParse(f[2], out int e);

                int.TryParse(f[3], out int g);

                int i = gridRouteStores.Rows.Add();

                var row = gridRouteStores.Rows[i];

                string code = f[0], name = f[1];

                // Drafts saved before codes were looked up for route stores: fill the code from Master Data.

                if (string.IsNullOrWhiteSpace(code))

                {

                    var known = MatchStore(ClientStoresForLookup(), "", name);

                    if (known != null && !string.IsNullOrWhiteSpace(known.StoreCode)) code = known.StoreCode.Trim();

                }

                if (gridRouteStores.Columns.Contains("colStoreCode")) row.Cells["colStoreCode"].Value = code;

                if (gridRouteStores.Columns.Contains("colStoreName")) row.Cells["colStoreName"].Value = name;

                if (gridRouteStores.Columns.Contains("colECrate")) row.Cells["colECrate"].Value = Math.Max(0, e);

                if (gridRouteStores.Columns.Contains("colGCrate")) row.Cells["colGCrate"].Value = Math.Max(0, g);

                if (gridRouteStores.Columns.Contains("colStoreTotal")) row.Cells["colStoreTotal"].Value = Math.Max(0, e) + Math.Max(0, g);

            }

            if (pnlRouteStoresBreakdown != null) pnlRouteStoresBreakdown.Visible = true;

            SizeStoreBreakdown(gridRouteStores.Rows.Count);

            storeGuard?.RevalidateAll(showToast: false);

        }



        private void DiscardDraft()

        {

            MJdatabase.ClearFormDraft("Dispatch", SessionContext.CurrentUsername);

            if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;

            ToastHelper.ShowToast(this, "Dispatch draft discarded ✓", ToastType.Info);

        }



        private void SaveNewDestinationIfNeeded(string fullDest)

        {

            if (string.IsNullOrWhiteSpace(fullDest)) return;

            var stops = fullDest.Split(new[] { "➔", "->", " / ", ", " }, StringSplitOptions.RemoveEmptyEntries)

                                .Select(s => System.Text.RegularExpressions.Regex.Replace(s.Trim(), @"^🛣️\s*|^Route\s*\d+\s*:\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim())

                                .Where(s => !string.IsNullOrWhiteSpace(s))

                                .Distinct();



            var existing = MJdatabase.GetLookupItems("Destination Route");

            foreach (var stop in stops)

            {

                if (!existing.Any(e => e.Equals(stop, StringComparison.OrdinalIgnoreCase)))

                {

                    MJdatabase.SaveLookupItem("Destination Route", stop);

                    if (!cbDestination.Items.Contains(stop))

                    {

                        cbDestination.Items.Add(stop);

                    }

                }

            }

            if (!fullDest.Contains("➔") && !fullDest.Contains("->") && !existing.Any(e => e.Equals(fullDest, StringComparison.OrdinalIgnoreCase)))

            {

                MJdatabase.SaveLookupItem("Destination Route", fullDest);

                if (!cbDestination.Items.Contains(fullDest))

                {

                    cbDestination.Items.Add(fullDest);

                }

            }

        }



        private void SetupDraftAutoSave()

        {

            tmrAutoSaveDraft = new System.Windows.Forms.Timer { Interval = 15000 };

            tmrAutoSaveDraft.Tick += (s, e) => SaveCurrentDraft();

            tmrAutoSaveDraft.Start();

        }



        public bool HasUnsavedChanges()
        {
            // Only a form that is open counts. A draft sitting in the closed form is already saved.

            if (!pnlForm.Visible || selectedDelivery != null) return false;

            return !string.IsNullOrWhiteSpace(txtDispatchNumber.Text)
                || !string.IsNullOrWhiteSpace(cbClient.Text)
                || !string.IsNullOrWhiteSpace(cbDestination.Text)
                || cbTruck.SelectedIndex >= 0
                || cbDriver.SelectedIndex >= 0
                || (numGCrate.Value + numECrate.Value) > 0
                || selectedAideIds.Count > 0;
        }

        public void AutoSaveDraft()
        {
            SaveCurrentDraft();
        }

        private void SaveCurrentDraft()

        {

            if (isBindingForm || selectedDelivery != null) return;

            if (string.IsNullOrWhiteSpace(cbClient.Text) && cbTruck.SelectedIndex < 0 && cbDriver.SelectedIndex < 0 && numGCrate.Value == 0 && numECrate.Value == 0) return;



            try

            {

                var map = new Dictionary<string, string>

                {

                    ["DispatchNumber"] = txtDispatchNumber.Text,

                    ["Client"] = cbClient.Text,

                    ["Destination"] = cbDestination.Text,

                    ["CargoType"] = cbCargoType.Text,

                    ["IsPeak"] = chkIsPeak.Checked.ToString(),

                    ["TruckId"] = (cbTruck.SelectedIndex >= 0 && cbTruck.SelectedIndex < trucks.Count ? trucks[cbTruck.SelectedIndex].Id : 0).ToString(),

                    ["DriverId"] = (cbDriver.SelectedIndex >= 0 && cbDriver.SelectedIndex < drivers.Count ? drivers[cbDriver.SelectedIndex].Id : 0).ToString(),

                    ["CombiCold"] = numCombiCold.Value.ToString(),

                    ["Stores"] = DraftStoresText(),

                    ["GCrate"] = numGCrate.Value.ToString(),

                    ["ECrate"] = numECrate.Value.ToString(),

                    ["DepartureTime"] = cbDepartureTime.Text,

                    ["DispatchDate"] = dtpDispatchDate.Value.ToString("yyyy-MM-dd"),

                    ["Aides"] = string.Join(",", selectedAideIds)

                };

                string json = DraftAutoSaveService.SerializeDraft(map);

                MJdatabase.SaveFormDraft("Dispatch", SessionContext.CurrentUsername, json);

            }

            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[Dispatch] SaveDraft: {ex.Message}"); ErrLog.Swallowed(ex); }

        }

    
        private void OpenDeliveryRoutesForCurrentStore()
        {
            string routeCode = ExtractCurrentRouteCode();
            MainForm.Instance?.NavigateToDeliveryRoutes(
                routeCode: routeCode,
                isNewRoute: false,
                returnTarget: "Dispatch",
                backAction: () => ReturnFromDeliveryRoutes("Dispatch"));
        }

        private void OpenDeliveryRoutesForNewRoute()
        {
            BtnAddRoute_Click(this, EventArgs.Empty);
        }

        private void ReturnFromDeliveryRoutes(string target)
        {
            MainForm.Instance?.NavigateToOperations(target);
            ReloadRoutesList();
            string curr = cbDestination.Text.Trim();
            if (!string.IsNullOrWhiteSpace(curr))
            {
                var routes = MJdatabase.GetDeliveryRoutes(activeOnly: false);
                var matched = routes.FirstOrDefault(r =>
                    string.Equals(r.RouteCode, curr, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.RouteName, curr, StringComparison.OrdinalIgnoreCase) ||
                    curr.StartsWith(r.RouteCode + " -", StringComparison.OrdinalIgnoreCase));
                if (matched != null)
                {
                    PopulateRouteStoresGrid(matched);
                }
            }
        }

        private void OnClientRegisteredFromStrip(string name)

        {

            if (!cbClient.Items.Cast<object>().Any(o => string.Equals(o?.ToString(), name, StringComparison.OrdinalIgnoreCase))) cbClient.Items.Add(name);

        }

        /// <summary>Route list for the picked client: that client's routes (plus routes not tied to any client).</summary>

        private async Task ReloadRoutesForClientAsync(string client)

        {

            var routes = await Task.Run(() => string.IsNullOrWhiteSpace(client) ? MJdatabase.GetDeliveryRoutes(activeOnly: true) : MJdatabase.GetDeliveryRoutesByClient(client, activeOnly: true));

            if (IsDisposed) return;

            string curDest = cbDestination.Text.Trim();

            var labels = routes.GroupBy(x => x.RouteCode).Select(g => $"{g.First().RouteCode} - {g.First().RouteName}").ToList();

            cbDestination.BeginUpdate();

            cbDestination.Items.Clear();

            foreach (var l in labels) cbDestination.Items.Add(l);

            UIThemeHelper.SafeEndUpdate(cbDestination);

            if (curDest.Length == 0) return;

            var current = MJdatabase.FindRouteByNameOrCode(curDest);

            bool belongsElsewhere = current != null && !string.IsNullOrWhiteSpace(client) && !string.IsNullOrWhiteSpace(current.ClientCompanyName)

                && !string.Equals(current.ClientCompanyName.Trim(), client.Trim(), StringComparison.OrdinalIgnoreCase);

            if (belongsElsewhere)

            {

                // The route picked earlier is another client's: clear it so the wrong stores aren't loaded.

                bool was = isBindingForm;

                isBindingForm = true;

                cbDestination.SelectedIndex = -1;

                cbDestination.Text = "";

                isBindingForm = was;

                // The store rows stay: they are checked against the new client (rows that aren't its stores are marked).

                storeListRoute = null;

                if (lblRouteStoresHeader != null) lblRouteStoresHeader.Text = "🏪 Stores & crates for this order";

                UpdateSaveStoresToRouteButton();

                ToastHelper.ShowToast(this, $"Route {current!.RouteCode} belongs to {current.ClientCompanyName}; pick one of {client}'s routes. The stores in the table were checked against {client}.", ToastType.Info);

                return;

            }

            string? item = labels.FirstOrDefault(l => string.Equals(l, curDest, StringComparison.OrdinalIgnoreCase));

            if (item != null) { bool was = isBindingForm; isBindingForm = true; cbDestination.SelectedItem = item; isBindingForm = was; }

            else cbDestination.Text = curDest;

        }

        /// <summary>Picking a route fills in its client when no client has been chosen yet.</summary>

        private void FillClientFromRoute(DeliveryRoute? route)

        {

            if (route == null || string.IsNullOrWhiteSpace(route.ClientCompanyName) || !string.IsNullOrWhiteSpace(cbClient.Text)) return;

            string name = route.ClientCompanyName.Trim();

            var item = cbClient.Items.Cast<object>().FirstOrDefault(o => string.Equals(o?.ToString(), name, StringComparison.OrdinalIgnoreCase));

            bool was = isBindingForm;

            isBindingForm = true;

            if (item != null) cbClient.SelectedItem = item; else cbClient.Text = name;

            isBindingForm = was;

            clientStrip?.ShowNow(name);

            // The route's stores were loaded before the client was known: check them now.

            ShowStoreWarning(null);

            storeGuard?.RevalidateAll(showToast: false);

        }

        public void ReloadRoutesList()
        {
            if (!string.IsNullOrWhiteSpace(cbClient.Text))

            {

                _ = ReloadRoutesForClientAsync(cbClient.Text.Trim());

                return;

            }

            var activeRoutes = MJdatabase.GetDeliveryRoutes(activeOnly: true);
            string currentText = cbDestination.Text;
            cbDestination.BeginUpdate();
            cbDestination.Items.Clear();
            foreach (var r in activeRoutes.GroupBy(x => x.RouteCode).Select(g => g.First()))
            {
                cbDestination.Items.Add($"{r.RouteCode} - {r.RouteName}");
            }
            UIThemeHelper.SafeEndUpdate(cbDestination);

            if (!string.IsNullOrWhiteSpace(currentText))
            {
                cbDestination.Text = currentText;
            }
        }

        private string ExtractCurrentRouteCode()
        {
            string txt = cbDestination.Text.Trim();
            if (string.IsNullOrWhiteSpace(txt)) return "";
            int dash = txt.IndexOf('-');
            if (dash > 0)
            {
                return txt.Substring(0, dash).Trim();
            }
            return txt;
        }
    }
}
