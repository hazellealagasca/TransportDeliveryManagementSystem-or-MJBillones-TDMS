using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class LoadingManagementView : UserControl, IRefreshable
    {
        private LoadingSchedule? selectedSchedule = null;
        private bool isBindingForm = false;
        private List<LoadingSchedule> allSchedules = new List<LoadingSchedule>();
        private string currentSortColumn = "Default";
        private bool isSortAscending = true;
        private System.Windows.Forms.Timer? tmrAutoSaveDraft;
        private Panel? pnlDraftBanner;
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;
        private Panel pnlRouteStoresBreakdown = null!;
        private DataGridView gridRouteStores = null!;
        private Label lblRouteStoresHeader = null!;
        private bool isUpdatingRouteStores = false;

        public LoadingManagementView()
        {
            InitializeComponent();
            Pal.RemapTree(this);
            if (pnlSummary != null)
            {
                pnlSummary.Visible = false;
                pnlGridCard.Controls.Remove(pnlSummary);
            }
            gridSchedules.BringToFront();
            InitializeRouteStoresBreakdownPanel();
            SetupGridColumns();
            SetupContextMenu();
            gridSchedules.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridSchedules.Rows[e.RowIndex].DataBoundItem is LoadingSchedule item) RecordDetailsDialog.ShowDetails(this, item); };
            gridSchedules.ColumnHeaderMouseClick += (s, e) => {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridSchedules.Columns.Count) return;
                string col = gridSchedules.Columns[e.ColumnIndex].DataPropertyName;
                if (string.IsNullOrEmpty(col)) return;
                if (currentSortColumn == col) isSortAscending = !isSortAscending;
                else { currentSortColumn = col; isSortAscending = true; }
                FilterGridData();
            };
            gridSchedules.CurrentCellDirtyStateChanged += (s, e) => { if (gridSchedules.IsCurrentCellDirty && gridSchedules.CurrentCell is DataGridViewCheckBoxCell) gridSchedules.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            gridSchedules.CellValueChanged += (s, e) => {
                if (e.RowIndex >= 0 && e.ColumnIndex == 0 && gridSchedules.Rows[e.RowIndex].DataBoundItem is LoadingSchedule ls) {
                    MJdatabase.ToggleLoadingSchedulePeak(ls.Id, ls.IsPeak);
                    if (selectedSchedule != null && selectedSchedule.Id == ls.Id) {
                        chkIsPeak.Checked = ls.IsPeak;
                        CalculateCratesAndRecommendation();
                    }
                }
            };
            SetupEvents();
            LoadSchedules();
            Button btnExport = new Button
            {
                Text = "📊 Export",
                Size = new Size(88, 30),
                BackColor = Pal.C(15, 23, 42),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += (s, e) => DataExportHelper.ExportToCsv(gridSchedules, "Loading_Schedules", "Cargo Loading Schedules");
            pnlGridActionsFlow.Controls.Add(btnExport);

            Button btnReschedule = new Button
            {
                Text = "📅 Reschedule",
                Size = new Size(118, 30),
                BackColor = Pal.C(234, 88, 12),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnReschedule.FlatAppearance.BorderSize = 0;
            btnReschedule.Click += (s, e) => OpenRescheduleDialog();
            pnlGridActionsFlow.Controls.Add(btnReschedule);

            Button btnDispatchOrder = new Button
            {
                Text = "🚚 Dispatch Now",
                Size = new Size(130, 30),
                BackColor = Pal.C(5, 150, 105),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDispatchOrder.FlatAppearance.BorderSize = 0;
            btnDispatchOrder.Click += (s, e) => DispatchSelectedSchedule();
            pnlGridActionsFlow.Controls.Add(btnDispatchOrder);

            Button btnPrintLoadingSheet = new Button
            {
                Text = "🖨️ Loading Sheet",
                Size = new Size(136, 30),
                BackColor = Pal.C(30, 58, 138),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPrintLoadingSheet.FlatAppearance.BorderSize = 0;
            btnPrintLoadingSheet.Click += (s, e) => PrintOfficialLoadingSheetAction();
            pnlGridActionsFlow.Controls.Add(btnPrintLoadingSheet);

            InitializeFormToggle();
            UIThemeHelper.ApplyTheme(this);
            SetButtonStates(false);
            ClearFields();
        }

        private void InitializeFormToggle()
        {
            btnToggleForm = new Button
            {
                Text = "📝 Input Form ▾",
                Size = new Size(130, 30),
                BackColor = Pal.C(30, 41, 59),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 4)
            };
            btnToggleForm.FlatAppearance.BorderSize = 0;
            btnToggleForm.Click += (s, e) => SetFormVisibility(!pnlForm.Visible);
            pnlGridActionsFlow.Controls.Add(btnToggleForm);
            pnlGridActionsFlow.Controls.SetChildIndex(btnToggleForm, 0);

            btnCloseForm = new Button
            {
                Name = "btnCloseForm",
                Text = "✕ Close",
                Dock = DockStyle.Right,
                Size = new Size(84, 28),
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 0, 4)
            };
            btnCloseForm.FlatAppearance.BorderSize = 0;
            btnCloseForm.Click += (s, e) => SetFormVisibility(true);
            pnlFormHeaderBar.Controls.Add(btnCloseForm);

            SetFormVisibility(true);
        }

        private void SetFormVisibility(bool visible)
        {
            pnlForm.Visible = visible;
            if (tblMasterContent.ColumnStyles.Count > 0)
            {
                tblMasterContent.ColumnStyles[0].Width = visible ? UIThemeHelper.GetAdaptiveDrawerWidth(this, 400F, 460F) : 0F;
                tblMasterContent.ColumnStyles[0].SizeType = SizeType.Absolute;
            }
            if (tblMasterContent.ColumnStyles.Count > 1)
            {
                tblMasterContent.ColumnStyles[1].Width = 100F;
                tblMasterContent.ColumnStyles[1].SizeType = SizeType.Percent;
            }
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = visible ? "📝 Hide Form ▴" : "📝 Input Form ▾";
                btnToggleForm.BackColor = visible ? Pal.C(226, 232, 240) : Pal.C(30, 41, 59);
                btnToggleForm.ForeColor = visible ? Pal.C(15, 23, 42) : Pal.White;
            }
        }

        public void RefreshData()
        {
            LoadSchedules();
        }

        private void InitializeRouteStoresBreakdownPanel()
        {
            pnlRouteStoresBreakdown = new Panel
            {
                Dock = DockStyle.Top,
                Height = 200,
                Visible = false,
                BackColor = Pal.C(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8, 6, 8, 6),
                Margin = new Padding(0, 4, 0, 8)
            };

            var pnlRouteStoreHeaderBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 30,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 4)
            };
            pnlRouteStoreHeaderBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlRouteStoreHeaderBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            lblRouteStoresHeader = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = Pal.C(30, 58, 138),
                Text = "🏪 Stores & crates for this schedule"
            };

            var btnAddNonRouteStore = new Button
            {
                Text = "✎ Edit route stores",
                Height = 26,
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                BackColor = Pal.C(238, 242, 255),
                ForeColor = Pal.C(67, 56, 202),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 4, 2)
            };
            btnAddNonRouteStore.FlatAppearance.BorderColor = Pal.C(199, 210, 254);
            btnAddNonRouteStore.Click += (s, e) => OpenDeliveryRoutesForCurrentStore();

            routeTips.SetToolTip(btnAddNonRouteStore, "Open this route in Master Data → Delivery Routes to add, remove or reorder its stores.");
            pnlRouteStoreHeaderBar.Controls.Add(lblRouteStoresHeader, 0, 0);
            pnlRouteStoreHeaderBar.Controls.Add(btnAddNonRouteStore, 1, 0);
            var lblRouteStoresHint = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Pal.C(100, 116, 139),
                Text = "Crates here apply to this schedule only (whole numbers). To change the route's stores, use \"Edit route stores\".",
                AutoEllipsis = true
            };
            gridRouteStores = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Pal.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                EditMode = DataGridViewEditMode.EditOnEnter,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 26,
                RowTemplate = { Height = 24 },
                Font = new Font("Segoe UI", 8.5F),
                EnableHeadersVisualStyles = false
            };
            gridRouteStores.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(219, 234, 254);
            gridRouteStores.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(30, 58, 138);
            gridRouteStores.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);

            gridRouteStores.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStoreCode", HeaderText = "Store Code", FillWeight = 70, ReadOnly = true });
            gridRouteStores.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStoreName", HeaderText = "Store Name", FillWeight = 160, ReadOnly = true });
            gridRouteStores.Columns.Add(new DataGridViewTextBoxColumn { Name = "colECrate", HeaderText = "E-Crates", FillWeight = 60, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            gridRouteStores.Columns.Add(new DataGridViewTextBoxColumn { Name = "colGCrate", HeaderText = "G-Crates", FillWeight = 60, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            gridRouteStores.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStoreTotal", HeaderText = "Total", FillWeight = 55, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold) } });
            // DR / invoice numbers are not entered in the loading store table; the hidden column only carries old values through.
            gridRouteStores.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDrNumbers", HeaderText = "DR / Doc #", FillWeight = 95, Visible = false });
            InputGuards.DigitsOnlyColumns(gridRouteStores, "colECrate", "colGCrate");
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

            pnlRouteStoresBreakdown.Controls.Add(gridRouteStores);
            pnlRouteStoresBreakdown.Controls.Add(lblRouteStoresHint);
            pnlRouteStoresBreakdown.Controls.Add(pnlRouteStoreHeaderBar);
            pnlForm.Controls.Add(pnlRouteStoresBreakdown);
            ApplyTwoColumnFormLayout();
            int destIdx = pnlForm.Controls.IndexOf(cbDestination);
            if (destIdx >= 0)
            {
                pnlForm.Controls.SetChildIndex(pnlRouteStoresBreakdown, destIdx);
            }
        }


        private Button btnAddRoute = null!;

        private ClientInfoStrip? clientStrip;
        private void OnClientRegisteredFromStrip(string name)
        {
            if (!cbClient.Items.Cast<object>().Any(o => string.Equals(o?.ToString(), name, StringComparison.OrdinalIgnoreCase))) cbClient.Items.Add(name);
            ReloadRoutesList();
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
            UpdateClientContactHint();
            clientStrip?.ShowNow(name);
        }
        private readonly ToolTip routeTips = new ToolTip { InitialDelay = 400, ShowAlways = true };
        private void ApplyTwoColumnFormLayout()
        {
            if (pnlForm == null) return;
            pnlForm.SuspendLayout();
            pnlForm.Controls.Clear();

            var pnlRowSchedClient = CreateTwoColumnRow(
                lblScheduleNumber, txtScheduleNumber,
                lblClient, cbClient
            );

            btnAddRoute = new Button
            {
                Text = "+ New route",
                Dock = DockStyle.Right,
                Width = (int)(104 * DeviceDpi / 96f),
                BackColor = Pal.C(37, 99, 235),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F),
                Cursor = Cursors.Hand
            };
            btnAddRoute.FlatAppearance.BorderSize = 0;
            btnAddRoute.Click += BtnAddRoute_Click;

            routeTips.SetToolTip(btnAddRoute, "Create a brand-new delivery route (code, name and its stores).");
            btnAddStop.Text = "Custom stops ▾";
            routeTips.SetToolTip(btnAddStop, "Only for a one-time trip that isn't a saved route: type addresses or pick stops by hand.");
            Panel pnlDestContainer = new Panel { Dock = DockStyle.Fill };
            cbDestination.Dock = DockStyle.Fill;
            btnAddStop.Dock = DockStyle.Right;
            btnAddStop.Width = 140;
            pnlDestContainer.Controls.Add(cbDestination);
            pnlDestContainer.Controls.Add(btnAddRoute);
            pnlDestContainer.Controls.Add(btnAddStop);

            var pnlRowRouteCargo = CreateTwoColumnRow(
                lblDest, pnlDestContainer,
                lblCargoType, tblCargoPeak
            );

            var pnlRowDateTime = CreateTwoColumnRow(
                lblDate, dtpScheduleDate,
                lblTime, pnlTimeRow
            );

            pnlForm.Controls.Add(btnClone);
            pnlForm.Controls.Add(btnClear);
            pnlForm.Controls.Add(tblUpdateDelete);
            pnlForm.Controls.Add(btnSave);
            pnlForm.Controls.Add(pnlRowDateTime);
            pnlForm.Controls.Add(pnlRec);
            pnlForm.Controls.Add(txtTotalCrates);
            pnlForm.Controls.Add(lblTotalCrates);
            pnlForm.Controls.Add(tblCrates);
            pnlForm.Controls.Add(pnlRouteStoresBreakdown);
            pnlForm.Controls.Add(pnlRowRouteCargo);
            clientStrip ??= new ClientInfoStrip();
            clientStrip.ClientRegistered -= OnClientRegisteredFromStrip;
            clientStrip.ClientRegistered += OnClientRegisteredFromStrip;
            pnlForm.Controls.Add(clientStrip);
            pnlForm.Controls.Add(pnlRowSchedClient);
            pnlForm.Controls.Add(pnlFormHeaderBar);

            pnlForm.ResumeLayout(true);
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
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

            lbl1.Dock = DockStyle.Fill;
            if (lbl1 is Label l1)
            {
                l1.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
                l1.ForeColor = Pal.C(30, 41, 59);
                l1.TextAlign = ContentAlignment.BottomLeft;
                l1.Padding = new Padding(0, 0, 4, 2);
            }
            ctrl1.Dock = DockStyle.Fill;
            tbl.Controls.Add(lbl1, 0, 0);
            tbl.Controls.Add(ctrl1, 0, 1);

            lbl2.Dock = DockStyle.Fill;
            if (lbl2 is Label l2)
            {
                l2.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
                l2.ForeColor = Pal.C(30, 41, 59);
                l2.TextAlign = ContentAlignment.BottomLeft;
                l2.Padding = new Padding(4, 0, 0, 2);
            }
            ctrl2.Dock = DockStyle.Fill;
            tbl.Controls.Add(lbl2, 1, 0);
            tbl.Controls.Add(ctrl2, 1, 1);

            return tbl;
        }

        private void PopulateRouteStoresGrid(DeliveryRoute? route, List<DispatchStop>? savedStops = null)
        {
            if (gridRouteStores == null || pnlRouteStoresBreakdown == null) return;
            isUpdatingRouteStores = true;
            try
            {
                gridRouteStores.Rows.Clear();
                if (savedStops != null && savedStops.Count > 0)
                {
                    lblRouteStoresHeader.Text = route != null
                        ? $"🏪 Stores & crates for this schedule  ·  {route.RouteCode} - {route.RouteName} ({savedStops.Count} stores)"
                        : $"🏪 Stores & crates for this schedule  ·  {savedStops.Count} stores";
                    foreach (var st in savedStops)
                    {
                        gridRouteStores.Rows.Add(st.StoreCode, st.Destination, st.ECrate, st.GCrate, st.ECrate + st.GCrate, st.DrNumbers);
                    }
                    pnlRouteStoresBreakdown.Height = Math.Clamp(58 + savedStops.Count * 25, 108, 195);
                    pnlRouteStoresBreakdown.Visible = true;
                }
                else if (route != null && route.PlacesList.Count > 0)
                {
                    lblRouteStoresHeader.Text = $"🏪 Stores & crates for this schedule  ·  {route.RouteCode} - {route.RouteName} ({route.PlacesList.Count} stores)";
                    int cnt = route.PlacesList.Count;
                    int curE = (int)numECrate.Value;
                    int curG = (int)numGCrate.Value;
                    int bE = cnt > 0 ? curE / cnt : 0;
                    int rE = cnt > 0 ? curE % cnt : 0;
                    int bG = cnt > 0 ? curG / cnt : 0;
                    int rG = cnt > 0 ? curG % cnt : 0;
                    for (int i = 0; i < cnt; i++)
                    {
                        var (sc, sn) = DeliveryRoute.SplitStoreCodeAndName(route.PlacesList[i]);
                        int ec = bE + (i < rE ? 1 : 0);
                        int gc = bG + (i < rG ? 1 : 0);
                        gridRouteStores.Rows.Add(sc, sn, ec, gc, ec + gc, "");
                    }
                    pnlRouteStoresBreakdown.Height = Math.Clamp(58 + cnt * 25, 108, 195);
                    pnlRouteStoresBreakdown.Visible = true;
                }
                else
                {
                    pnlRouteStoresBreakdown.Visible = false;
                }
            }
            finally
            {
                isUpdatingRouteStores = false;
            }
        }

        private void RecalculateTotalsFromRouteStoresGrid()
        {
            if (isUpdatingRouteStores || gridRouteStores == null || !pnlRouteStoresBreakdown.Visible) return;
            isUpdatingRouteStores = true;
            try
            {
                int sumE = 0;
                int sumG = 0;
                foreach (DataGridViewRow row in gridRouteStores.Rows)
                {
                    if (row.IsNewRow) continue;
                    int ec = int.TryParse(row.Cells["colECrate"].Value?.ToString(), out int pe) ? Math.Max(0, pe) : 0;
                    int gc = int.TryParse(row.Cells["colGCrate"].Value?.ToString(), out int pg) ? Math.Max(0, pg) : 0;
                    row.Cells["colECrate"].Value = ec;
                    row.Cells["colGCrate"].Value = gc;
                    row.Cells["colStoreTotal"].Value = ec + gc;
                    sumE += ec;
                    sumG += gc;
                }
                numECrate.Value = Math.Min(numECrate.Maximum, sumE);
                numGCrate.Value = Math.Min(numGCrate.Maximum, sumG);
                CalculateCratesAndRecommendation();
            }
            finally
            {
                isUpdatingRouteStores = false;
            }
        }

        private List<DispatchStop> CollectRouteStoresFromGrid()
        {
            var list = new List<DispatchStop>();
            if (gridRouteStores == null || !pnlRouteStoresBreakdown.Visible) return list;
            int seq = 1;
            foreach (DataGridViewRow row in gridRouteStores.Rows)
            {
                if (row.IsNewRow) continue;
                int ec = int.TryParse(row.Cells["colECrate"].Value?.ToString(), out int pe) ? Math.Max(0, pe) : 0;
                int gc = int.TryParse(row.Cells["colGCrate"].Value?.ToString(), out int pg) ? Math.Max(0, pg) : 0;
                list.Add(new DispatchStop
                {
                    StopSequence = seq++,
                    StoreCode = row.Cells["colStoreCode"].Value?.ToString()?.Trim() ?? "",
                    Destination = row.Cells["colStoreName"].Value?.ToString()?.Trim() ?? "",
                    ECrate = ec,
                    GCrate = gc,
                    TotalCrates = ec + gc,
                    DrNumbers = row.Cells["colDrNumbers"].Value?.ToString()?.Trim() ?? ""
                });
            }
            return list;
        }

        private void PrintOfficialLoadingSheetAction()
        {
            var targetDate = selectedSchedule?.ScheduleDate.Date ?? dtpScheduleDate.Value.Date;
            var sameDaySchedules = allSchedules.Where(s => s.ScheduleDate.Date == targetDate && s.Status != "Cancelled").OrderBy(s => s.Id).ToList();
            if (sameDaySchedules.Count == 0 && selectedSchedule != null)
            {
                sameDaySchedules.Add(selectedSchedule);
            }
            else if (sameDaySchedules.Count == 0 && allSchedules.Count > 0)
            {
                sameDaySchedules = allSchedules.Take(5).ToList();
            }
            PrintHelper.PrintOfficialLoadingScheduleSheet(sameDaySchedules);
        }

        private void SetupEvents()
        {
            txtScheduleNumber.TextChanged += (s, e) => { if (!isBindingForm) HandleScheduleNumberInput(); };
            CargoRouting.FillCargoCombo(cbCargoType);
            InputGuards.DigitsOnly(numGCrate, numECrate);
            cbCargoType.SelectedIndexChanged += (s, e) =>
            {
                if (isBindingForm || !CargoRouting.IsSpecial(cbCargoType.Text)) return;
                string company = cbClient.Text.Trim();
                DateTime when = dtpScheduleDate.Value;
                isBindingForm = true;
                try { cbCargoType.SelectedIndex = -1; } finally { isBindingForm = false; }
                BeginInvoke(new Action(() => CargoRouting.OfferSpecialTrip(FindForm(), company, when)));
            };
            numGCrate.ValueChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            numECrate.ValueChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            dtpScheduleDate.ValueChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            cbClient.TextChanged += (s, e) => { clientStrip?.ShowFor(cbClient.Text); if (!isBindingForm) { UpdateClientContactHint(); ReloadRoutesList(); } };
            cbClient.SelectedIndexChanged += (s, e) => { if (!isBindingForm) { UpdateClientContactHint(); ReloadRoutesList(); } };
            btnAddStop.Click += BtnAddStop_Click;
            cbDestination.TextChanged += (s, e) =>
            {
                if (!isBindingForm)
                {
                    CalculateCratesAndRecommendation();
                    if (cbDestination.Focused)
                    {
                        string txt = cbDestination.Text.Trim();
                        var matched = MJdatabase.FindRouteByNameOrCode(txt);
                        if (matched != null && (txt.Equals(matched.RouteCode, StringComparison.OrdinalIgnoreCase) || txt.Equals(matched.RouteName, StringComparison.OrdinalIgnoreCase)))
                        {
                            ShowRouteStoresPopup(matched);
                            PopulateRouteStoresGrid(matched);
                        }
                        else if (matched == null)
                        {
                            PopulateRouteStoresGrid(null);
                        }
                    }
                }
            };
            cbDestination.SelectedIndexChanged += (s, e) =>
            {
                if (!isBindingForm)
                {
                    string selectedText = cbDestination.Text.Trim();
                    var matchedRoute = MJdatabase.FindRouteByNameOrCode(selectedText);
                    if (matchedRoute != null && (selectedText.Equals(matchedRoute.RouteName, StringComparison.OrdinalIgnoreCase) || selectedText.Equals(matchedRoute.RouteCode, StringComparison.OrdinalIgnoreCase)))
                    {
                        cbDestination.Text = $"{matchedRoute.RouteCode} - {matchedRoute.RouteName}";
                        ShowRouteStoresPopup(matchedRoute);
                    }
                    PopulateRouteStoresGrid(matchedRoute);
                    FillClientFromRoute(matchedRoute);
                    CalculateCratesAndRecommendation();
                }
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
                        ShowRouteStoresPopup(matched);
                        PopulateRouteStoresGrid(matched);
                        cbDestination.SelectionStart = cbDestination.Text.Length;
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                }
            };
            chkIsPeak.CheckedChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            btnTimePicker.Click += (s, e) =>
            {
                string? pickedTime = AnalogClockTimePickerDialog.ShowTimePicker(this.FindForm() ?? this.ParentForm!, txtDepartureTime.Text);
                if (!string.IsNullOrEmpty(pickedTime))
                {
                    txtDepartureTime.Text = pickedTime;
                }
            };
            SetupDraftAutoSave();
        }

        private void UpdateClientContactHint()
        {
            string cname = cbClient.Text.Trim();
            if (!string.IsNullOrWhiteSpace(cname))
            {
                var cl = MJdatabase.GetClientByCompanyName(cname);
                if (cl != null && !string.IsNullOrEmpty(cl.ContactNumber))
                {
                    lblClient.Text = $"Client: (📞 {cl.ContactPerson} {cl.ContactNumber})";
                    return;
                }
            }
            lblClient.Text = "Client Name:";
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

        private void CalculateCratesAndRecommendation()
        {
            int g = (int)numGCrate.Value;
            int ec = (int)numECrate.Value;
            int total = g + ec;
            txtTotalCrates.Text = $"{total} Crates ({g} G-Crates + {ec} E-Crates)";

            var rec = MJdatabase.GetTruckRecommendation(total, dtpScheduleDate.Value, cbCargoType.Text);
            string dest = cbDestination.Text.Trim();
            var matchedRoute = MJdatabase.FindRouteByNameOrCode(dest);
            var matchedRate = MJdatabase.FindMatchingRate(dest, chkIsPeak.Checked);
            string routeNotice = matchedRoute != null ? $"  |  🛣️ {matchedRoute.RouteName} ({matchedRoute.TotalStops} stops)" : "";



            string rateNotice = !Features.On(Features.RateMatrix) ? "" : matchedRate != null ? $"  |  🏷️ Rate: PHP {matchedRate.RateAmount:N2} ({(matchedRate.IsPeakSeason ? "Peak Season" : "Regular")})" : "  |  ⚠️ No rate configured for this destination (set it in Rate Matrix)";
            lblRec.Text = rec.RecommendationMessage + routeNotice + rateNotice;




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

        private void SetupGridColumns()
        {
            gridSchedules.ReadOnly = false;
            gridSchedules.EditMode = DataGridViewEditMode.EditOnEnter;
            gridSchedules.AutoGenerateColumns = false;
            gridSchedules.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridSchedules.ScrollBars = ScrollBars.Both;
            gridSchedules.AllowUserToResizeColumns = true;
            gridSchedules.ShowCellToolTips = false;
            gridSchedules.Columns.Clear();
            gridSchedules.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "IsPeak", HeaderText = "Peak", FillWeight = 45, MinimumWidth = 60, ReadOnly = false });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ScheduleNumber", HeaderText = "Schedule #", FillWeight = 115, MinimumWidth = 145, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = "Client Name", FillWeight = 135, MinimumWidth = 150, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Destination", HeaderText = "Destination", FillWeight = 120, MinimumWidth = 140, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CargoType", HeaderText = "Cargo Type", FillWeight = 95, MinimumWidth = 110, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ScheduleDate", HeaderText = "Date", DefaultCellStyle = { Format = "MM/dd/yyyy" }, FillWeight = 80, MinimumWidth = 90, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DepartureTime", HeaderText = "Departure", FillWeight = 85, MinimumWidth = 95, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "GCrate", HeaderText = "G-Crates", FillWeight = 65, MinimumWidth = 70, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ECrate", HeaderText = "E-Crates", FillWeight = 65, MinimumWidth = 70, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalCrates", HeaderText = "Total", FillWeight = 70, MinimumWidth = 75, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", FillWeight = 80, MinimumWidth = 90, ReadOnly = true });
            gridSchedules.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RescheduleCount", HeaderText = "Resched", FillWeight = 60, MinimumWidth = 65, ReadOnly = true });
            gridSchedules.CellFormatting -= GridSchedules_CellFormatting;
            gridSchedules.CellFormatting += GridSchedules_CellFormatting;
        }

        private void GridSchedules_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (gridSchedules.Columns[e.ColumnIndex].DataPropertyName == "Status" && e.Value != null)
            {
                string status = e.Value.ToString() ?? "";
                if (status.Equals("Dispatched", StringComparison.OrdinalIgnoreCase) || status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Pal.C(220, 252, 231);
                    e.CellStyle.ForeColor = Pal.C(22, 101, 52);
                    e.CellStyle.Font = FontCache.Get(gridSchedules.Font, FontStyle.Bold);
                }
                else if (status.Equals("Past Due", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Pal.C(254, 226, 226);
                    e.CellStyle.ForeColor = Pal.C(185, 28, 28);
                    e.CellStyle.Font = FontCache.Get(gridSchedules.Font, FontStyle.Bold);
                }
                else if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Pal.C(254, 243, 199);
                    e.CellStyle.ForeColor = Pal.C(180, 83, 9);
                }
                else if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Pal.C(241, 245, 249);
                    e.CellStyle.ForeColor = Pal.C(100, 116, 139);
                }
                else if (status.Equals("Rescheduled", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Pal.C(238, 242, 255);
                    e.CellStyle.ForeColor = Pal.C(67, 56, 202);
                }
            }
        }

        private void GridSchedules_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridSchedules.SelectedRows.Count > 0 && gridSchedules.SelectedRows[0].DataBoundItem is LoadingSchedule ls)
            {
                BindScheduleToForm(ls);
            }
        }

        private void SetButtonStates(bool isEditing)
        {
            string sNum = txtScheduleNumber.Text.Trim();
            var matched = !string.IsNullOrWhiteSpace(sNum)
                ? allSchedules.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.ScheduleNumber) && s.ScheduleNumber.Equals(sNum, StringComparison.OrdinalIgnoreCase))
                : null;

            bool exists = (selectedSchedule != null || matched != null) && !string.IsNullOrWhiteSpace(sNum);

            btnSave.Visible = !exists;
            btnSave.Enabled = !exists;
            tblUpdateDelete.Visible = exists;
            tblUpdateDelete.Enabled = exists;
            btnClone.Visible = exists;
            btnClone.Enabled = exists;

            btnClear.Text = exists ? "➕ New Schedule Order" : "🧹 Clear Form";
            lblFormHeader.Text = exists
                ? $"Loading Order Form: {sNum}"
                : (string.IsNullOrWhiteSpace(sNum) ? "Loading Order Form: [New]" : $"Loading Order Form: {sNum} (New)");
        }

        private void HandleScheduleNumberInput()
        {
            string sNum = txtScheduleNumber.Text.Trim();
            if (string.IsNullOrWhiteSpace(sNum))
            {
                selectedSchedule = null;
                SetButtonStates(false);
                return;
            }

            var matched = allSchedules.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.ScheduleNumber) && s.ScheduleNumber.Equals(sNum, StringComparison.OrdinalIgnoreCase));
            if (matched != null)
            {
                BindScheduleToForm(matched);
                for (int i = 0; i < gridSchedules.Rows.Count; i++)
                {
                    if (gridSchedules.Rows[i].DataBoundItem is LoadingSchedule rowS && rowS.Id == matched.Id)
                    {
                        gridSchedules.ClearSelection();
                        gridSchedules.Rows[i].Selected = true;
                        break;
                    }
                }
            }
            else
            {
                selectedSchedule = null;
                SetButtonStates(false);
            }
        }

        private void BindScheduleToForm(LoadingSchedule ls)
        {
            isBindingForm = true;
            try
            {
                selectedSchedule = ls;
                txtScheduleNumber.Text = selectedSchedule.ScheduleNumber ?? "";
                cbClient.Text = selectedSchedule.ClientName;
                var matchedSchedRoute = MJdatabase.FindRouteByNameOrCode(selectedSchedule.Destination);
                cbDestination.Text = matchedSchedRoute != null ? $"{matchedSchedRoute.RouteCode} - {matchedSchedRoute.RouteName}" : selectedSchedule.Destination;
                if (selectedSchedule.ScheduleDate >= dtpScheduleDate.MinDate && selectedSchedule.ScheduleDate <= dtpScheduleDate.MaxDate)
                {
                    dtpScheduleDate.Value = selectedSchedule.ScheduleDate;
                }
                txtDepartureTime.Text = string.IsNullOrEmpty(selectedSchedule.DepartureTime) ? "08:00 AM" : selectedSchedule.DepartureTime;
                numGCrate.Value = Math.Max(0, selectedSchedule.GCrate);
                numECrate.Value = Math.Max(0, selectedSchedule.ECrate);
                chkIsPeak.Checked = selectedSchedule.IsPeak;
                cbCargoType.SelectedItem = string.IsNullOrEmpty(selectedSchedule.CargoType) ? "Cold / Chilled" : selectedSchedule.CargoType;
                var savedStops = MJdatabase.GetLoadingScheduleStops(selectedSchedule.Id);
                PopulateRouteStoresGrid(matchedSchedRoute, savedStops.Count > 0 ? savedStops : null);
                CalculateCratesAndRecommendation();
            }
            finally
            {
                isBindingForm = false;
            }
            SetButtonStates(true);
        }

        private async void LoadSchedules()
        {
            try
            {
                var clients = MJdatabase.GetClientNames();
                cbClient.BeginUpdate();
                cbClient.Items.Clear();
                foreach (var c in clients.Distinct(StringComparer.OrdinalIgnoreCase)) cbClient.Items.Add(c);
                UIThemeHelper.SafeEndUpdate(cbClient);
                var activeRoutes = MJdatabase.GetDeliveryRoutes(activeOnly: true);
                cbDestination.BeginUpdate();
                cbDestination.Items.Clear();
                foreach (var r in activeRoutes.GroupBy(x => x.RouteCode).Select(g => g.First())) cbDestination.Items.Add($"{r.RouteCode} - {r.RouteName}");
                UIThemeHelper.SafeEndUpdate(cbDestination);
                allSchedules = await MJdatabase.GetLoadingSchedulesAsync();
                if (this.IsDisposed || !this.IsHandleCreated) return;

                FilterGridData();
                var schedules = allSchedules;
                CalculateCratesAndRecommendation();

                int pendingCount = schedules.Count(s => s.Status == "Pending");
                int pastDueCount = schedules.Count(s => s.Status == "Past Due");
                int transitCount = schedules.Count(s => s.Status == "In Transit");
                int completedCount = schedules.Count(s => s.Status == "Completed");
                int totalCratesSum = schedules.Sum(s => s.TotalCrates);

                lblSummary.Text = $"📊 Total Schedules: {schedules.Count:N0}  |  ⏳ Pending: {pendingCount}  |  🚨 Past Due: {pastDueCount}  |  🚚 In Transit: {transitCount}  |  ✅ Completed: {completedCount}  |  📦 Total Volume: {totalCratesSum:N0} Crates";
                CheckAndPromptDraft();
            }
            catch (Exception ex)
            { ErrLog.Error("LoadingManagementView", ex); 
                MessageBox.Show($"Failed to load loading schedules: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                var valErrors = new List<string>();
                Control? firstInvalid = null;
                ValidationHelper.ClearErrors(pnlForm);

                if (!ValidationHelper.ValidateRequiredText(txtScheduleNumber, "Schedule Reference Number", valErrors)) firstInvalid ??= txtScheduleNumber;
                if (!ValidationHelper.ValidateRequiredCombo(cbClient, "Client Company", valErrors)) firstInvalid ??= cbClient;
                if (!ValidationHelper.ValidateRequiredCombo(cbDestination, "Destination Route", valErrors)) firstInvalid ??= cbDestination;
                if (!ValidationHelper.ValidateRequiredText(txtDepartureTime, "Scheduled Departure Time", valErrors)) firstInvalid ??= txtDepartureTime;

                int gTotal = (int)numGCrate.Value + (int)numECrate.Value;
                if (gTotal <= 0)
                {
                    ValidationHelper.ApplyValidationState(numGCrate, false, "Planned Crates must be greater than zero.");
                    valErrors.Add("Total planned crates must be greater than 0." + CargoRouting.SpecialHint);
                    firstInvalid ??= numGCrate;
                }

                if (!ValidationHelper.ShowValidationSummaryIfAny(this, valErrors, firstInvalid))
                {
                    return;
                }
                string schedNum = txtScheduleNumber.Text.Trim();
                if (string.IsNullOrWhiteSpace(schedNum))
                {
                    MessageBox.Show("Please enter a Loading Schedule Reference Number.", "Reference Number Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtScheduleNumber.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(cbClient.Text) || string.IsNullOrWhiteSpace(cbDestination.Text))
                {
                    MessageBox.Show("Please enter both Client Name and Destination Branch.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (dtpScheduleDate.Value.Date < DateTime.Today.AddDays(-365))
                {
                    MessageBox.Show("Please enter a valid schedule date within the past year or a future date.", "Invalid Date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dtpScheduleDate.Focus();
                    return;
                }

                int g = (int)numGCrate.Value;
                int ec = (int)numECrate.Value;
                if (g == 0 && ec == 0)
                {
                    MessageBox.Show("Please enter at least 1 crate in either G-Crates or E-Crates.", "Crate Quantity Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numGCrate.Focus();
                    return;
                }

                string client = cbClient.Text.Trim();
                string dest = cbDestination.Text.Trim();
                var matchedRoute = MJdatabase.FindRouteByNameOrCode(dest);
                if (matchedRoute != null && matchedRoute.PlacesList.Count > 0)
                {
                    dest = $"{matchedRoute.RouteCode} - {matchedRoute.RouteName}: {matchedRoute.FormattedPlaces}";
                }
                string depTime = txtDepartureTime.Text.Trim();

                if (string.IsNullOrWhiteSpace(depTime) || !System.Text.RegularExpressions.Regex.IsMatch(depTime, @"^(0?[1-9]|1[0-2]):[0-5][0-9]\s*(AM|PM)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    MessageBox.Show("Please select or enter a valid departure time (e.g. 08:00 AM).", "Invalid Departure Time", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (selectedSchedule != null)
                {
                    var ask = MessageBox.Show($"You currently have schedule '{selectedSchedule.ScheduleNumber}' selected.\n\n• Click YES to UPDATE the selected schedule.\n• Click NO to create a NEW additional order.", "Save or Update?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (ask == DialogResult.Cancel) return;
                    if (ask == DialogResult.Yes)
                    {
                        BtnUpdate_Click(sender, e);
                        return;
                    }
                }

                string? duplicateNotice = MJdatabase.ValidateLoadingScheduleConflict(0, client, dest, dtpScheduleDate.Value, depTime);
                if (!string.IsNullOrEmpty(duplicateNotice))
                {
                    var conf = MessageBox.Show(duplicateNotice + "\n\nDo you want to proceed and create an ADDITIONAL batch/trip order for this client?", "Additional Batch Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (conf != DialogResult.Yes) return;
                }

                MJdatabase.SaveLookupItem("Client Company", client);
                SaveNewDestinationIfNeeded(dest);

                LoadingSchedule ls = new LoadingSchedule
                {
                    ScheduleNumber = schedNum,
                    CargoType = cbCargoType.SelectedItem?.ToString() ?? "Cold / Chilled",
                    ClientName = client,
                    Destination = dest,
                    ScheduleDate = dtpScheduleDate.Value,
                    DepartureTime = depTime,
                    GCrate = g,
                    ECrate = ec,
                    TotalCrates = g + ec,
                    IsPeak = chkIsPeak.Checked,
                    Status = "Pending",
                    Remarks = "Scheduled Delivery Order"
                };

                MJdatabase.SaveLoadingSchedule(ls);
                var storeStops = CollectRouteStoresFromGrid();
                if (storeStops.Count > 0)
                {
                    MJdatabase.SaveLoadingScheduleStops(ls.Id, storeStops);
                }
                MJdatabase.ClearFormDraft("LoadingSchedule", SessionContext.CurrentUsername);
                if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;
                QuietBox.Show("Loading Schedule created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadSchedules();
            }
            catch (Exception ex)
            { ErrLog.Error("LoadingManagementView", ex); 
                MessageBox.Show($"Failed to save loading schedule: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedSchedule == null)
                {
                    MessageBox.Show("Please select a loading schedule to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (RowMenu.Is(selectedSchedule.Status, "Completed", "Cancelled"))
                {
                    MessageBox.Show($"Loading order {selectedSchedule.ScheduleNumber} is {selectedSchedule.Status.ToLowerInvariant()} and can no longer be changed.", "Order closed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(cbClient.Text) || string.IsNullOrWhiteSpace(cbDestination.Text))
                {
                    MessageBox.Show("Please enter both the client and the destination route.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    (string.IsNullOrWhiteSpace(cbClient.Text) ? (Control)cbClient : cbDestination).Focus();
                    return;
                }
                var liveDispatch = FindDispatchForSchedule(selectedSchedule);
                if (liveDispatch != null && ((int)numGCrate.Value != selectedSchedule.GCrate || (int)numECrate.Value != selectedSchedule.ECrate || dtpScheduleDate.Value.Date != selectedSchedule.ScheduleDate.Date))
                {
                    var go = MessageBox.Show($"This order already has dispatch {liveDispatch.DispatchNumber} ({liveDispatch.Status}). Changing the crates or date here does not change the dispatch — update the dispatch too.\n\nSave the loading order anyway?", "Already dispatched", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                    if (go != DialogResult.Yes) return;
                }
                string schedNum = txtScheduleNumber.Text.Trim();
                if (string.IsNullOrWhiteSpace(schedNum))
                {
                    MessageBox.Show("Please enter a Loading Schedule Reference Number.", "Reference Number Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtScheduleNumber.Focus();
                    return;
                }

                string client = cbClient.Text.Trim();
                string dest = cbDestination.Text.Trim();
                var matchedRoute = MJdatabase.FindRouteByNameOrCode(dest);
                if (matchedRoute != null && matchedRoute.PlacesList.Count > 0)
                {
                    dest = $"{matchedRoute.RouteCode} - {matchedRoute.RouteName}: {matchedRoute.FormattedPlaces}";
                }
                string depTime = txtDepartureTime.Text.Trim();

                if (string.IsNullOrWhiteSpace(depTime) || !System.Text.RegularExpressions.Regex.IsMatch(depTime, @"^(0?[1-9]|1[0-2]):[0-5][0-9]\s*(AM|PM)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    MessageBox.Show("Please select or enter a valid departure time (e.g. 08:00 AM).", "Invalid Departure Time", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string? conflict = MJdatabase.ValidateLoadingScheduleConflict(selectedSchedule.Id, client, dest, dtpScheduleDate.Value, depTime);
                if (!string.IsNullOrEmpty(conflict))
                {
                    MessageBox.Show(conflict, "Loading Schedule Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int g = (int)numGCrate.Value;
                int ec = (int)numECrate.Value;
                if (g == 0 && ec == 0)
                {
                    MessageBox.Show("Please enter at least 1 crate in either G-Crates or E-Crates.", "Crate Quantity Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numGCrate.Focus();
                    return;
                }

                MJdatabase.SaveLookupItem("Client Company", client);
                SaveNewDestinationIfNeeded(dest);

                selectedSchedule.ScheduleNumber = schedNum;
                selectedSchedule.ClientName = client;
                selectedSchedule.Destination = dest;
                selectedSchedule.ScheduleDate = dtpScheduleDate.Value;
                selectedSchedule.DepartureTime = depTime;
                selectedSchedule.GCrate = g;
                selectedSchedule.ECrate = ec;
                selectedSchedule.TotalCrates = g + ec;
                selectedSchedule.IsPeak = chkIsPeak.Checked;
                selectedSchedule.CargoType = cbCargoType.SelectedItem?.ToString() ?? "Cold / Chilled";
                selectedSchedule.Remarks = "Scheduled Delivery Order";

                MJdatabase.SaveLoadingSchedule(selectedSchedule);
                var storeStops = CollectRouteStoresFromGrid();
                if (storeStops.Count > 0)
                {
                    MJdatabase.SaveLoadingScheduleStops(selectedSchedule.Id, storeStops);
                }
                QuietBox.Show("Loading Schedule updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadSchedules();
            }
            catch (Exception ex)
            { ErrLog.Error("LoadingManagementView", ex); 
                MessageBox.Show($"Failed to update loading schedule: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedSchedule == null)
                {
                    MessageBox.Show("Please select a loading schedule to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string? deleteBlock = MJdatabase.ValidateRecordDeletion("LoadingSchedules", selectedSchedule.Id);
                if (!string.IsNullOrEmpty(deleteBlock))
                {
                    MessageBox.Show(deleteBlock, "Deletion Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show($"Are you sure you want to delete Loading Schedule {selectedSchedule.ScheduleNumber}?\n\n" + MJdatabase.DeletePolicyNote("LoadingSchedules"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("LoadingSchedules", selectedSchedule.Id);
                    if (deleted)
                    {
                        QuietBox.Show(MJdatabase.LastDeleteSummary, "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearFields();
                        LoadSchedules();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete loading schedule. It may be locked or referenced by active operations.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            { ErrLog.Error("LoadingManagementView", ex); 
                MessageBox.Show($"Delete operation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClone_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedSchedule == null)
                {
                    QuietBox.Show("Please select an existing loading schedule from the table to repeat / clone.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var newSched = new LoadingSchedule
                {
                    ScheduleNumber = $"SCH-{DateTime.Now:yyyyMMddHHmmss}",
                    ClientName = selectedSchedule.ClientName,
                    Destination = selectedSchedule.Destination,
                    CargoType = selectedSchedule.CargoType,
                    ScheduleDate = DateTime.Today,
                    DepartureTime = selectedSchedule.DepartureTime,
                    GCrate = selectedSchedule.GCrate,
                    ECrate = selectedSchedule.ECrate,
                    TotalCrates = selectedSchedule.TotalCrates,
                    IsPeak = selectedSchedule.IsPeak,
                    Status = "Pending"
                };

                MJdatabase.SaveLoadingSchedule(newSched);
                var srcStops = MJdatabase.GetLoadingScheduleStops(selectedSchedule.Id);
                if (srcStops.Count > 0)
                {
                    MJdatabase.SaveLoadingScheduleStops(newSched.Id, srcStops);
                }
                LoadSchedules();
                QuietBox.Show($"New Loading Order '{newSched.ScheduleNumber}' for {newSched.ClientName} ({newSched.Destination}) cloned successfully for today!", "Order Cloned", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            { ErrLog.Error("LoadingManagementView", ex); 
                MessageBox.Show($"Failed to clone loading schedule: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            ClearFields();
        }

        private static int GetLoadingStatusSortWeight(string? status)
        {
            if (string.Equals(status, "Scheduled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (string.Equals(status, "Assigned", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Loading", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (string.Equals(status, "Dispatched", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
                return 2;
            if (string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                return 3;
            return 4;
        }

        private void FilterGridData()
        {
            string q = txtGridSearch.Text.Trim().ToLowerInvariant();
            var query = string.IsNullOrEmpty(q)
                ? allSchedules.AsEnumerable()
                : allSchedules.Where(s =>
                    (!string.IsNullOrEmpty(s.ScheduleNumber) && s.ScheduleNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(s.ClientName) && s.ClientName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(s.Destination) && s.Destination.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(s.CargoType) && s.CargoType.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(s.Status) && s.Status.ToLower().Contains(q))
                );

            IEnumerable<LoadingSchedule> sorted;
            if (currentSortColumn == "ScheduleNumber")
            {
                sorted = isSortAscending ? query.OrderBy(s => s.ScheduleNumber) : query.OrderByDescending(s => s.ScheduleNumber);
            }
            else if (currentSortColumn == "ScheduleDate")
            {
                sorted = isSortAscending ? query.OrderBy(s => s.ScheduleDate) : query.OrderByDescending(s => s.ScheduleDate);
            }
            else if (currentSortColumn == "TotalCrates")
            {
                sorted = isSortAscending ? query.OrderBy(s => s.TotalCrates) : query.OrderByDescending(s => s.TotalCrates);
            }
            else
            {
                sorted = query.OrderBy(s => GetLoadingStatusSortWeight(s.Status)).ThenByDescending(s => s.ScheduleDate).ThenByDescending(s => s.Id);
            }

            var filtered = sorted.ToList();
            gridSchedules.DataSource = null;
            gridSchedules.DataSource = filtered;
            lblGridHeader.Text = string.IsNullOrEmpty(q)
                ? $"Active Schedules List ({allSchedules.Count})"
                : $"Active Schedules List - Showing {filtered.Count} of {allSchedules.Count}";

            if (filtered.Count > 0)
            {
                gridSchedules.ClearSelection();
                gridSchedules.ClearSelection();
            }
            else
            {
                isBindingForm = true;
                selectedSchedule = null;
                txtScheduleNumber.Text = "";
                cbClient.Text = "";
                cbDestination.Text = "";
                numGCrate.Value = 0;
                numECrate.Value = 0;
                txtTotalCrates.Text = "";
                lblRec.Text = "No matching schedules found.";
                lblFormHeader.Text = "Loading Order Form";
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
            PrintHelper.PrintGridReport("Loading Schedules & Cargo Order Report", gridSchedules, "Official client loading schedule register with crate quantities and peak season status.");
        }

        public bool HasUnsavedChanges()
        {
            if (!pnlForm.Visible || selectedSchedule != null) return false;
            return !string.IsNullOrWhiteSpace(txtScheduleNumber.Text)
                || !string.IsNullOrWhiteSpace(cbClient.Text)
                || !string.IsNullOrWhiteSpace(cbDestination.Text)
                || (numGCrate.Value + numECrate.Value) > 0;
        }

        private void ClearFields()
        {
            isBindingForm = true;
            try
            {
                selectedSchedule = null;
                txtScheduleNumber.Text = "";
                cbClient.SelectedIndex = -1;
                cbClient.Text = "";
                cbCargoType.SelectedIndex = -1;
                cbCargoType.Text = "";
                cbDestination.SelectedIndex = -1;
                cbDestination.Text = "";
                dtpScheduleDate.Value = DateTime.Now;
                txtDepartureTime.Text = "08:00 AM";
                numGCrate.Value = 0;
                numECrate.Value = 0;
                chkIsPeak.Checked = false;
                PopulateRouteStoresGrid(null);
                CalculateCratesAndRecommendation();
            }
            finally
            {
                isBindingForm = false;
            }
            SetButtonStates(false);
            gridSchedules.ClearSelection();
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action<LoadingSchedule>? OnDispatchScheduleRequested { get; set; }

        private void DispatchSelectedSchedule()
        {
            if (selectedSchedule == null)
            {
                if (gridSchedules.SelectedRows.Count > 0 && gridSchedules.SelectedRows[0].DataBoundItem is LoadingSchedule rowSched)
                {
                    selectedSchedule = rowSched;
                }
                else
                {
                    QuietBox.Show("Please select a loading schedule to dispatch.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            if (selectedSchedule.Status == "Completed")
            {
                QuietBox.Show($"Loading Order '{selectedSchedule.ScheduleNumber}' is already completed and delivered.", "Already Delivered", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (selectedSchedule.Status == "Cancelled")
            {
                MessageBox.Show($"Loading Order '{selectedSchedule.ScheduleNumber}' is cancelled and cannot be dispatched.", "Order Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var existing = FindDispatchForSchedule(selectedSchedule);
            if (existing != null)
            {
                var ans = MessageBox.Show($"Loading Order '{selectedSchedule.ScheduleNumber}' already has dispatch {existing.DispatchNumber} ({existing.Status}).\n\nOpen that dispatch instead?", "Already dispatched", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (ans == DialogResult.Yes) Forms.DispatchDetailsDialog.ShowFor(this, existing);
                return;
            }
            OnDispatchScheduleRequested?.Invoke(selectedSchedule);
        }

        private void SetupContextMenu()
        {
            RowMenu.Attach(gridSchedules, (m, rows) =>
            {
                var list = rows.Select(r => r.DataBoundItem).OfType<LoadingSchedule>().ToList();
                if (list.Count == 0) return;
                if (list.Count > 1)
                {
                    m.Items.AddRange(RowMenu.Header($"📦  {list.Count} loading orders", $"{list.Sum(x => x.TotalCrates):N0} crates · {list.Select(x => x.ClientName).Distinct().Count()} client(s)"));
                    m.Items.Add(RowMenu.Item("Print one loading sheet for all", () => PrintHelper.PrintOfficialLoadingScheduleSheet(list.Where(x => !RowMenu.Is(x.Status, "Cancelled")).ToList())));
                    m.Items.Add(RowMenu.Item("Copy schedule numbers", () => RowMenu.Copy(this, string.Join(", ", list.Select(x => x.ScheduleNumber)), "Schedule numbers copied")));
                    m.Items.Add(RowMenu.Sep());
                    m.Items.Add(RowMenu.ExportSub(gridSchedules, "Loading_Schedules", "Cargo Loading Schedules", () => BtnPrint_Click(this, EventArgs.Empty)));
                    return;
                }
                var ls = list[0];
                selectedSchedule = ls;
                bool done = RowMenu.Is(ls.Status, "Completed");
                bool cancelled = RowMenu.Is(ls.Status, "Cancelled");
                var dispatch = FindDispatchForSchedule(ls);
                m.Items.AddRange(RowMenu.Header($"📦  {ls.ScheduleNumber}",
                    $"{RowMenu.Short(ls.ClientName, 26)} · {ls.ScheduleDate:ddd, MMM d} {ls.DepartureTime} · {ls.TotalCrates} crates · {ls.Status}{(ls.RescheduleCount > 0 ? $" · moved {ls.RescheduleCount}×" : "")}"));
                m.Items.Add(RowMenu.Item("Open details", () => RecordDetailsDialog.ShowDetails(this, ls), "Enter"));
                m.Items.Add(RowMenu.Item("Edit loading order", () => { SetFormVisibility(true); cbClient.Focus(); }, "", !done && !cancelled));
                m.Items.Add(RowMenu.Sep());
                if (dispatch != null)
                    m.Items.Add(RowMenu.Item($"Open dispatch {dispatch.DispatchNumber} ({dispatch.Status})", () => Forms.DispatchDetailsDialog.ShowFor(this, dispatch)));
                else
                    m.Items.Add(RowMenu.Good("Create dispatch for this order", () => DispatchSelectedSchedule(), !done && !cancelled));
                m.Items.Add(RowMenu.Item("Reschedule…", () => OpenRescheduleDialog(), "", !done && !cancelled && !RowMenu.Is(ls.Status, "In Transit")));
                m.Items.Add(RowMenu.Item("Open client profile", () => MainForm.Instance?.OpenClientProfile(ls.ClientName), "", !string.IsNullOrWhiteSpace(ls.ClientName)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Sub("🖨️  Print",
                    RowMenu.Item($"Official loading sheet — {ls.ScheduleDate:MMM d}", () => PrintOfficialLoadingSheetAction()),
                    RowMenu.Item("Only this order's loading sheet", () => PrintHelper.PrintOfficialLoadingScheduleSheet(new List<LoadingSchedule> { ls })),
                    RowMenu.Item("Loading register (table)", () => BtnPrint_Click(this, EventArgs.Empty))));
                m.Items.Add(RowMenu.CopySub(this, ("Schedule number", ls.ScheduleNumber), ("Client", ls.ClientName), ("Destination / route", ls.Destination),
                    ("Summary", $"{ls.ScheduleNumber} — {ls.ClientName}, {ls.ScheduleDate:dddd, MMMM d} at {ls.DepartureTime}: {ls.TotalCrates} crates ({ls.GCrate} G / {ls.ECrate} E), {ls.CargoType}, to {ls.Destination}")));
                m.Items.Add(RowMenu.Sub("🔎  Find",
                    RowMenu.Item("Other orders for this client", () => RowMenu.Search(txtGridSearch, ls.ClientName)),
                    RowMenu.Item($"Orders on {ls.ScheduleDate:MMM d}", () => RowMenu.Search(txtGridSearch, ls.ScheduleDate.ToString("MM/dd/yyyy")))));
                m.Items.Add(RowMenu.ExportSub(gridSchedules, "Loading_Schedules", "Cargo Loading Schedules", () => BtnPrint_Click(this, EventArgs.Empty)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Danger("Cancel loading order…", () => CancelSchedule(ls), !done && !cancelled));
                m.Items.Add(RowMenu.Danger("Delete loading order…", () => { selectedSchedule = ls; BtnDelete_Click(this, EventArgs.Empty); }));
            }, allowMultiSelect: true);
            gridSchedules.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && gridSchedules.CurrentRow?.DataBoundItem is LoadingSchedule ls) { RecordDetailsDialog.ShowDetails(this, ls); e.Handled = true; e.SuppressKeyPress = true; }
            };
        }
        /// <summary>The live (not cancelled) dispatch made from a loading order, if there is one.</summary>
        private static Delivery? FindDispatchForSchedule(LoadingSchedule ls)
        {
            try
            {
                return MJdatabase.GetDeliveries().Where(d => d.LoadingScheduleId == ls.Id && !RowMenu.Is(d.Status, "Cancelled"))
                    .OrderByDescending(d => d.Id).FirstOrDefault();
            }
            catch { return null; }
        }
        private void CancelSchedule(LoadingSchedule ls)
        {
            if (RowMenu.Is(ls.Status, "Cancelled")) { QuietBox.Show("This loading order is already cancelled.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (RowMenu.Is(ls.Status, "Completed")) { MessageBox.Show("This loading order has already been delivered and can't be cancelled.", "Already delivered", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var live = FindDispatchForSchedule(ls);
            if (live != null)
            {
                MessageBox.Show($"Loading order {ls.ScheduleNumber} already has dispatch {live.DispatchNumber} ({live.Status}).\n\nCancel the dispatch first (right-click it in Dispatch → Cancel order); the loading order is released with it.", "Dispatch exists", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                string? reason = CancellationReasonDialog.PromptCancellationReason(this.FindForm() ?? this.ParentForm!, "Loading Schedule", ls.ScheduleNumber);
                if (string.IsNullOrEmpty(reason)) return;
                ls.Status = "Cancelled";
                ls.CancellationReason = reason;
                MJdatabase.SaveLoadingSchedule(ls);
                ToastHelper.ShowToast(this, $"{ls.ScheduleNumber} cancelled", ToastType.Info);
                LoadSchedules();
            }
            catch (Exception ex)
            { ErrLog.Error("LoadingManagementView", ex); 
                MessageBox.Show($"Failed to cancel loading schedule: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenRescheduleDialog()
        {
            if (selectedSchedule == null)
            {
                if (gridSchedules.SelectedRows.Count > 0 && gridSchedules.SelectedRows[0].DataBoundItem is LoadingSchedule ls)
                {
                    selectedSchedule = ls;
                }
            }

            if (selectedSchedule == null)
            {
                QuietBox.Show("Please select a loading schedule order to reschedule.", "Select Order", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (selectedSchedule.Status.Equals("In Transit", StringComparison.OrdinalIgnoreCase) ||
                selectedSchedule.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Loading Order #{selectedSchedule.ScheduleNumber} is currently '{selectedSchedule.Status}' and cannot be rescheduled.", "Reschedule Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selectedSchedule.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Loading Order #{selectedSchedule.ScheduleNumber} is Cancelled and cannot be rescheduled.", "Reschedule Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dlg = new RescheduleScheduleDialog(selectedSchedule))
            {
                if (dlg.ShowDialog(this.FindForm() ?? this.ParentForm) == DialogResult.OK)
                {
                    LoadSchedules();
                }
            }
        }

        private void SetupDraftAutoSave()
        {
            tmrAutoSaveDraft = new System.Windows.Forms.Timer { Interval = 15000 };
            tmrAutoSaveDraft.Tick += (s, e) => SaveCurrentDraft();
            tmrAutoSaveDraft.Start();
        }

        private void CheckAndPromptDraft()
        {
            try
            {
                var draft = MJdatabase.GetFormDraft("LoadingSchedule", SessionContext.CurrentUsername);
                if (draft != null && !string.IsNullOrWhiteSpace(draft.Value.Data))
                {
                    if (pnlDraftBanner != null) pnlForm.Controls.Remove(pnlDraftBanner);
                    pnlDraftBanner = DraftAutoSaveService.CreateDraftBanner("Loading Schedule", draft.Value.LastSaved, RestoreDraft, DiscardDraft);
                    pnlForm.Controls.Add(pnlDraftBanner);
                    pnlDraftBanner.BringToFront();
                }
            }
            catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
        }

        private void RestoreDraft()
        {
            try
            {
                var draft = MJdatabase.GetFormDraft("LoadingSchedule", SessionContext.CurrentUsername);
                if (draft == null || string.IsNullOrWhiteSpace(draft.Value.Data)) return;
                var map = DraftAutoSaveService.DeserializeDraft(draft.Value.Data);
                isBindingForm = true;

                if (map.TryGetValue("ScheduleNumber", out var sn) && !string.IsNullOrWhiteSpace(sn))
                {
                    txtScheduleNumber.Text = sn;
                }
                if (map.TryGetValue("Client", out var cl) && !string.IsNullOrWhiteSpace(cl))
                {
                    cbClient.Text = cl;
                }
                if (map.TryGetValue("Destination", out var dest) && !string.IsNullOrWhiteSpace(dest))
                {
                    cbDestination.Text = dest;
                }
                if (map.TryGetValue("ScheduleDate", out var sd) && DateTime.TryParse(sd, out var sdate) && sdate >= dtpScheduleDate.MinDate && sdate <= dtpScheduleDate.MaxDate)
                {
                    dtpScheduleDate.Value = sdate;
                }
                if (map.TryGetValue("DepartureTime", out var dt) && !string.IsNullOrWhiteSpace(dt))
                {
                    txtDepartureTime.Text = dt;
                }
                if (map.TryGetValue("GCrate", out var gc) && decimal.TryParse(gc, out var gval))
                {
                    numGCrate.Value = Math.Min(numGCrate.Maximum, Math.Max(numGCrate.Minimum, gval));
                }
                if (map.TryGetValue("ECrate", out var ec) && decimal.TryParse(ec, out var eval))
                {
                    numECrate.Value = Math.Min(numECrate.Maximum, Math.Max(numECrate.Minimum, eval));
                }
                if (map.TryGetValue("CargoType", out var ct) && !string.IsNullOrWhiteSpace(ct))
                {
                    cbCargoType.SelectedItem = ct;
                }
                if (map.TryGetValue("IsPeak", out var ip) && bool.TryParse(ip, out var isPeakVal))
                {
                    chkIsPeak.Checked = isPeakVal;
                }

                isBindingForm = false;
                CalculateCratesAndRecommendation();
                ToastHelper.ShowToast(this, "Loading Schedule draft restored ✓", ToastType.Success);
            }
            catch (Exception ex)
            {
                isBindingForm = false;
                System.Diagnostics.Debug.WriteLine($"[LoadingSchedule] RestoreDraft: {ex.Message}");
                ToastHelper.ShowToast(this, "Could not restore draft.", ToastType.Warning);
            }
        }

        private void DiscardDraft()
        {
            MJdatabase.ClearFormDraft("LoadingSchedule", SessionContext.CurrentUsername);
            if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;
            ToastHelper.ShowToast(this, "Loading Schedule draft discarded ✓", ToastType.Info);
        }

        private void SaveNewDestinationIfNeeded(string fullDest)
        {
            if (string.IsNullOrWhiteSpace(fullDest)) return;
            var stops = fullDest.Split(new[] { "➔", "->", " / ", ", " }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
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

        private void BtnAddStop_Click(object? sender, EventArgs e)
        {
            var cms = new ContextMenuStrip();
            var allDestinations = cbDestination.Items.Cast<object>().Select(o => o.ToString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

            var titleItem = new ToolStripMenuItem("➕ Add Next Delivery Stop / Destination Address") { Enabled = false, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) };
            cms.Items.Add(titleItem);
            cms.Items.Add(new ToolStripSeparator());

            var customInputItem = new ToolStripMenuItem("✏️ Type Custom Stop Address...", null, (s, ev) =>
            {
                using var prompt = new Form
                {
                    Text = "Add Destination Drop / Stop Address",
                    Size = new Size(520, 220),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Pal.C(248, 250, 252),
                    Font = new Font("Segoe UI", 9.5F)
                };

                var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Pal.C(30, 41, 59), Padding = new Padding(16, 12, 16, 12) };
                var lblPrompt = new Label { Text = "Enter next destination address, branch, or landmark:", Dock = DockStyle.Fill, ForeColor = Pal.White, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold) };
                pnlHeader.Controls.Add(lblPrompt);

                var pnlContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 16) };
                var txtInput = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 10F), MaxLength = 200 };
                var btnAdd = new Button { Text = "Add Stop to Route ➔", Dock = DockStyle.Bottom, Height = 36, BackColor = Pal.C(22, 101, 52), ForeColor = Pal.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnAdd.FlatAppearance.BorderSize = 0;
                btnAdd.Click += (bs, be) =>
                {
                    string val = txtInput.Text.Trim();
                    if (!string.IsNullOrWhiteSpace(val) && val.Length <= 200)
                    {
                        AppendDestinationStop(val);
                        prompt.DialogResult = DialogResult.OK;
                        prompt.Close();
                    }
                };

                pnlContent.Controls.Add(btnAdd);
                pnlContent.Controls.Add(txtInput);
                prompt.Controls.Add(pnlContent);
                prompt.Controls.Add(pnlHeader);
                prompt.AcceptButton = btnAdd;
                prompt.ShowDialog(this.FindForm() ?? this.ParentForm);
            });
            cms.Items.Add(customInputItem);
            cms.Items.Add(new ToolStripSeparator());

            var configuredRoutesMenu = new ToolStripMenuItem("🛣️ Configured Delivery Routes (Master Data)");
            var activeDeliveryRoutes = MJdatabase.GetDeliveryRoutes(activeOnly: true);
            foreach (var r in activeDeliveryRoutes)
            {
                var rItem = new ToolStripMenuItem($"{r.RouteName} ({r.TotalStops} stops) - {r.Description}", null, (s, ev) =>
                {
                    cbDestination.Text = $"{r.RouteName}: {r.FormattedPlaces}";
                    cbDestination.Focus();
                    cbDestination.SelectionStart = cbDestination.Text.Length;
                });
                configuredRoutesMenu.DropDownItems.Add(rItem);
            }
            cms.Items.Add(configuredRoutesMenu);

            var presetMenu = new ToolStripMenuItem("📋 Select from Preset Routes / Destinations");
            foreach (var d in allDestinations.Take(30))
            {
                string destName = d;
                presetMenu.DropDownItems.Add(new ToolStripMenuItem(destName, null, (s, ev) => AppendDestinationStop(destName)));
            }
            cms.Items.Add(presetMenu);

            var multiBuilderItem = new ToolStripMenuItem("🗺️ Multi-Route Stops Checklist...", null, (s, ev) => ShowMultiRouteBuilderDialog());
            cms.Items.Add(multiBuilderItem);

            if (!string.IsNullOrWhiteSpace(cbDestination.Text))
            {
                cms.Items.Add(new ToolStripSeparator());
                cms.Items.Add(new ToolStripMenuItem("🧹 Clear Destination Address", null, (s, ev) => cbDestination.Text = ""));
            }

            cms.Show(btnAddStop, new Point(0, btnAddStop.Height));
        }

        private void AppendDestinationStop(string nextStop)
        {
            string current = cbDestination.Text.Trim();
            if (string.IsNullOrWhiteSpace(current))
            {
                cbDestination.Text = nextStop;
            }
            else
            {
                cbDestination.Text = $"{current} ➔ {nextStop}";
            }
            if (!cbDestination.Items.Contains(nextStop))
            {
                cbDestination.Items.Add(nextStop);
            }
            cbDestination.Focus();
            cbDestination.SelectionStart = cbDestination.Text.Length;
        }

        private void ShowMultiRouteBuilderDialog()
        {
            using var dlg = new Form
            {
                Text = "Multi-Stop Delivery Route Builder",
                Size = new Size(540, 520),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Pal.C(248, 250, 252),
                Font = new Font("Segoe UI", 9.5F)
            };

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Pal.C(30, 41, 59), Padding = new Padding(16, 12, 16, 12) };
            var lblTitle = new Label { Text = "Check delivery stops in sequence to build the route:", Dock = DockStyle.Fill, ForeColor = Pal.White, Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold) };
            pnlHeader.Controls.Add(lblTitle);

            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 54, Padding = new Padding(16, 8, 16, 8) };
            var btnApply = new Button { Text = "Apply Route to Order ✓", Dock = DockStyle.Right, Width = 180, BackColor = Pal.C(22, 101, 52), ForeColor = Pal.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnApply.FlatAppearance.BorderSize = 0;
            var btnCancel = new Button { Text = "Cancel", Dock = DockStyle.Left, Width = 100, BackColor = Pal.C(241, 245, 249), ForeColor = Pal.C(71, 85, 105), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => dlg.Close();
            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(btnApply);

            var chkList = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, Font = new Font("Segoe UI", 10F) };
            var routeStops = MJdatabase.GetDeliveryRoutes(activeOnly: true).SelectMany(r => r.PlacesList);
            var allDests = cbDestination.Items.Cast<object>().Select(o => o.ToString() ?? "").Concat(routeStops).Where(s => !string.IsNullOrWhiteSpace(s) && !s.StartsWith("🛣️")).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
            foreach (var d in allDests)
            {
                chkList.Items.Add(d);
            }

            btnApply.Click += (s, e) =>
            {
                var selected = chkList.CheckedItems.Cast<object>().Select(o => o.ToString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                if (selected.Count > 0)
                {
                    cbDestination.Text = string.Join(" ➔ ", selected);
                }
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            dlg.Controls.Add(chkList);
            dlg.Controls.Add(pnlBottom);
            dlg.Controls.Add(pnlHeader);
            dlg.ShowDialog(this.FindForm() ?? this.ParentForm);
        }

        private void SaveCurrentDraft()
        {
            if (isBindingForm || selectedSchedule != null) return;
            if (string.IsNullOrWhiteSpace(cbClient.Text) && string.IsNullOrWhiteSpace(cbDestination.Text) && numGCrate.Value == 0 && numECrate.Value == 0) return;

            try
            {
                var map = new Dictionary<string, string>
                {
                    ["ScheduleNumber"] = txtScheduleNumber.Text.Trim(),
                    ["Client"] = cbClient.Text.Trim(),
                    ["Destination"] = cbDestination.Text.Trim(),
                    ["ScheduleDate"] = dtpScheduleDate.Value.ToString("yyyy-MM-dd"),
                    ["DepartureTime"] = txtDepartureTime.Text.Trim(),
                    ["GCrate"] = numGCrate.Value.ToString(),
                    ["ECrate"] = numECrate.Value.ToString(),
                    ["CargoType"] = cbCargoType.SelectedItem?.ToString() ?? cbCargoType.Text,
                    ["IsPeak"] = chkIsPeak.Checked.ToString()
                };
                string json = DraftAutoSaveService.SerializeDraft(map);
                MJdatabase.SaveFormDraft("LoadingSchedule", SessionContext.CurrentUsername, json);
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[LoadingSchedule] SaveDraft: {ex.Message}"); ErrLog.Swallowed(ex); }
        }
    
        private void OpenDeliveryRoutesForCurrentStore()
        {
            string routeCode = ExtractCurrentRouteCode();
            MainForm.Instance?.NavigateToDeliveryRoutes(
                routeCode: routeCode,
                isNewRoute: false,
                returnTarget: "Loading",
                backAction: () => ReturnFromDeliveryRoutes("Loading"));
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
                string targetText = $"{route.RouteCode} - {route.RouteName}";
                if (cbDestination.Items.Contains(targetText))
                {
                    cbDestination.SelectedItem = targetText;
                }
                else
                {
                    cbDestination.Text = targetText;
                }
                PopulateRouteStoresGrid(route);
            }
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

        public void ReloadRoutesList()
        {
            string cl = cbClient.Text.Trim();
            var activeRoutes = MJdatabase.GetDeliveryRoutesByClient(cl, activeOnly: true);
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
