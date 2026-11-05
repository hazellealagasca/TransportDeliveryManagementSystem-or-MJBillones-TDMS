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
    public partial class TripReportView : UserControl, IRefreshable, IUnsavedForm
    {
        public Action<int, string, int, DateTime>? OnLogIncidentRequested;
        private TripReport? selectedTripReport = null;
        private bool isBindingForm = false;
        private List<TripReport> allTripReports = new List<TripReport>();
        private string currentSortColumn = "TripReportNumber";
        private bool isSortAscending = true;
        private List<Delivery> allDeliveries = new List<Delivery>();
        private System.Windows.Forms.Timer? tmrAutoSaveDraft;
        private Panel? pnlDraftBanner;
        /// <summary>Restore or Discard was chosen for the waiting draft while this form is open (the banner is not shown again).</summary>
        private bool draftAnswered;
        /// <summary>The incident note last filled into Remarks automatically (so switching dispatch can replace it).</summary>
        private string autoIncidentRemark = "";
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;
        private Panel pnlMultiDropInfo = null!;
        private Label lblMultiDropDetails = null!;
        private DataGridView gridStores = null!;
        private Panel pnlTripFooter = null!;
        private TextBox txtTruckTimeIn = null!;
        private TextBox txtTruckTimeOut = null!;
        private TextBox txtDtrApprovedBy = null!;
        private TextBox txtReturnCheckedBy = null!;
        private TextBox txtDtrPreparedBy = null!;
        private TextBox txtDrPreparedBy = null!;
        private TextBox txtDrCheckedBy = null!;
        private TextBox txtGuardName = null!;
        private bool isUpdatingStoreGrid = false;
        private Label? lblDispatchInfoBanner;

        public TripReportView()
        {
            InitializeComponent();
            Pal.RemapTree(this);
            gridTripReports.BringToFront();
            InitializeMultiDropTripPanel();
            SetupGridColumns();
            SetupContextMenu();
            gridTripReports.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridTripReports.Rows[e.RowIndex].DataBoundItem is TripReport item) OpenEditTripReport(item); };
            gridTripReports.ColumnHeaderMouseClick += (s, e) => {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridTripReports.Columns.Count) return;
                string col = gridTripReports.Columns[e.ColumnIndex].DataPropertyName;
                if (string.IsNullOrEmpty(col)) return;
                if (currentSortColumn == col) isSortAscending = !isSortAscending;
                else { currentSortColumn = col; isSortAscending = true; }
                FilterGridData();
            };
            SetupEvents();
            LoadData();

            btnPrint.Visible = false;

            var btnPrintOfficialTrip = new Button
            {
                Text = "📋 Official trip report",
                Size = new Size(165, 30),
                BackColor = Pal.C(30, 58, 138),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 4)
            };
            btnPrintOfficialTrip.FlatAppearance.BorderSize = 0;
            btnPrintOfficialTrip.Click += (s, e) => PrintSelectedTripReport();
            pnlGridActionsFlow.Controls.Add(btnPrintOfficialTrip);

            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("📋 Print official trip report", null, (s, e) => PrintSelectedTripReport());
            menuActions.Items.Add("📊 Export trip documentation to CSV", null, (s, e) => DataExportHelper.ExportToCsv(gridTripReports, "Trip_Reports", "Completed Trip Reports"));
            menuActions.Items.Add("🖨️ Print trip reports register", null, (s, e) => BtnPrint_Click(this, EventArgs.Empty));
            var btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);
            pnlGridActionsFlow.Controls.Add(btnActions);

            currentFormWidth = UIThemeHelper.GetAdaptiveDrawerWidth(this, 420F, 480F);
            InitializeFormToggle();
            UIThemeHelper.ApplyTheme(this);
            StyleFullPageForm();
            InitReportCenter();
            FitStoreGridHeaders();
            ThemeManager.ThemeChanged += (s, e) => { try { if (!IsDisposed) BeginInvoke(new Action(FitStoreGridHeaders)); } catch (Exception ex) { ErrLog.Swallowed(ex); } };
        }

        /// <summary>Crate headers wrap onto two lines ("Delivered / E") so they are never cut off, at any window size.</summary>
        private void FitStoreGridHeaders()
        {
            if (gridStores == null) return;
            foreach (DataGridViewColumn c in gridStores.Columns)
                c.HeaderCell.Style.WrapMode = DataGridViewTriState.True;
            gridStores.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            if (!storeLinkColourHooked)
            {
                storeLinkColourHooked = true;
                // Every store name looks the same clickable link (alternating rows used to turn some of them a different colour).
                gridStores.CellFormatting += (s, e) =>
                {
                    if (e.RowIndex >= 0 && e.CellStyle != null && gridStores.Columns[e.ColumnIndex].Name == "colStore")
                    {
                        e.CellStyle.ForeColor = AppTheme.Accent;
                        e.CellStyle.SelectionForeColor = AppTheme.Accent;
                    }
                };
            }
        }
        private bool storeLinkColourHooked;
        private float currentFormWidth = 420F;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && pnlForm.Visible && EscapeGuard.AllowClose(this))
            {
                SetFormVisibility(false);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitializeFormToggle()
        {
            btnToggleForm = new Button
            {
                Text = "＋ New trip report",
                Size = new Size(Dpi(160), 30),
                BackColor = Pal.C(185, 28, 28),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 4)
            };
            btnToggleForm.FlatAppearance.BorderSize = 0;
            btnToggleForm.Click += (s, e) => { if (pnlForm.Visible) { if (FormGuard.ConfirmDiscard(this)) SetFormVisibility(false); } else OpenNewTripReport(); };
            UIThemeHelper.SetButtonRole(btnToggleForm, "primary");
            pnlGridActionsFlow.Controls.Add(btnToggleForm);
            pnlGridActionsFlow.Controls.SetChildIndex(btnToggleForm, 0);

            btnCloseForm = new Button
            {
                Text = "✕ Close",
                Dock = DockStyle.Right,
                Width = 72,
                Height = 30,
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCloseForm.FlatAppearance.BorderSize = 0;
            btnCloseForm.Click += (s, e) => { if (FormGuard.ConfirmDiscard(this)) SetFormVisibility(false); };
            pnlFormHeaderBar.Controls.Add(btnCloseForm);

            SetFormVisibility(false);
        }

        public void RefreshData()
        {
            LoadData();
        }

        private void InitializeMultiDropTripPanel()
        {
            tblTimes.Visible = false;
            tblManagerNames.Visible = false;

            lblDelGCrate.Text = "Delivered G-crates";
            lblDelECrate.Text = "Delivered E-crates";
            lblRetGCrate.Text = "Returned G-crates";
            lblRetECrate.Text = "Returned E-crates";
            lblDel.Text = "Dispatch order";
            lblRem.Text = "QA / store remarks";
            lblDispatchInfoBanner = new Label
            {
                Dock = DockStyle.Top,
                Height = Dpi(58),
                BackColor = Pal.C(240, 253, 244),
                ForeColor = Pal.C(22, 101, 52),
                Font = new Font("Segoe UI Semibold", 9F),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Dpi(12), Dpi(4), Dpi(12), Dpi(4)),
                Margin = new Padding(0, Dpi(8), 0, 0),
                AutoEllipsis = true,
                Text = "ℹ️ Pick a dispatch order above to load its route, truck, crew and stores."
            };

            pnlMultiDropInfo = new Panel
            {
                Dock = DockStyle.Top,
                Height = Dpi(300),
                Visible = true,
                BackColor = AppTheme.Surface,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            var pnlMultiDropHeaderBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = Dpi(46),
                ColumnCount = 3,
                RowCount = 1,
                BackColor = AppTheme.Surface,
                Margin = new Padding(0)
            };

            pnlMultiDropHeaderBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlMultiDropHeaderBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlMultiDropHeaderBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            lblMultiDropDetails = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 10.5F),
                ForeColor = AppTheme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Text = "🏪 Stores & crates for this trip"
            };

            Button MakeBarButton(string text, string role)
            {
                var b = new Button
                {
                    Text = text,
                    Height = Dpi(34),
                    AutoSize = true,
                    Padding = new Padding(Dpi(10), 0, Dpi(10), 0),
                    Font = new Font("Segoe UI Semibold", 9F),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(Dpi(6), Dpi(4), 0, Dpi(4))
                };
                b.FlatAppearance.BorderSize = 0;
                UIThemeHelper.SetButtonRole(b, role);
                return b;
            }
            var btnAddTripStoreRow = MakeBarButton("➕ Add store", "secondary");
            btnAddTripStoreRow.Click += (s, e) =>
            {
                if (gridStores == null) return;
                // The new row starts completely empty; its details are entered in the store window.
                int rIdx = gridStores.Rows.Add();
                SizeStoreTable(gridStores.Rows.Count);
                if (rIdx >= 0 && !OpenStoreDetails(rIdx))
                {
                    var r = gridStores.Rows[rIdx];
                    bool empty = r.Cells.Cast<DataGridViewCell>().Where(c => c is not DataGridViewButtonCell).All(c => string.IsNullOrWhiteSpace(c.Value?.ToString()));
                    if (empty) { gridStores.Rows.RemoveAt(rIdx); SizeStoreTable(gridStores.Rows.Count); }
                }
            };

            var btnEditTripStoreTable = MakeBarButton("✎ Edit table", "secondary");
            btnEditTripStoreTable.Click += (s, e) => OpenTripStoreTableEditor();
            pnlMultiDropHeaderBar.Controls.Add(lblMultiDropDetails, 0, 0);
            pnlMultiDropHeaderBar.Controls.Add(btnAddTripStoreRow, 1, 0);
            pnlMultiDropHeaderBar.Controls.Add(btnEditTripStoreTable, 2, 0);
            btnEditTripStoreTable.Visible = false; // the free table editor would bypass the locked crate cells
            var lblStoresHint = new Label
            {
                Dock = DockStyle.Top,
                Height = Dpi(24),
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 8.75F),
                ForeColor = AppTheme.TextMuted,
                AutoEllipsis = true,
                Text = "Click a store name for its returned crates. Type DR numbers in the DR #s cell (Enter or a comma between them) or click … beside it. Delivered crates can't be changed."
            };
            gridStores = new MultiValueGrid
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppTheme.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                EditMode = DataGridViewEditMode.EditOnEnter,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars = ScrollBars.Both,
                // Headers wrap onto two lines so "Delivered E" etc. are never cut off.
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = Dpi(46),
                RowTemplate = { Height = Dpi(30), MinimumHeight = Dpi(30) },
                Font = new Font("Segoe UI", 9F),
                EnableHeadersVisualStyles = false
            };
            gridStores.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(219, 234, 254);
            gridStores.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(30, 58, 138);
            gridStores.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5F);
            gridStores.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            var roStyle = new DataGridViewCellStyle { BackColor = Pal.C(248, 250, 252), ForeColor = Pal.C(15, 23, 42) };
            var editStyle = new DataGridViewCellStyle { BackColor = Pal.C(254, 252, 232), ForeColor = Pal.C(15, 23, 42) };
            var editNumStyle = new DataGridViewCellStyle { BackColor = Pal.C(254, 252, 232), ForeColor = Pal.C(15, 23, 42), Alignment = DataGridViewContentAlignment.MiddleRight };
            DataGridViewTextBoxColumn Col(string name, string header, int weight, DataGridViewCellStyle style, int min = 48) =>
                new DataGridViewTextBoxColumn { Name = name, HeaderText = header, FillWeight = weight, MinimumWidth = Dpi(min), ReadOnly = false, DefaultCellStyle = new DataGridViewCellStyle(style) };
            // Store, DR and crate cells are locked: click the store to open its details (several DRs, returned crates).
            var roNumStyle = new DataGridViewCellStyle(roStyle) { Alignment = DataGridViewContentAlignment.MiddleRight };
            var linkStyle = new DataGridViewCellStyle(roStyle) { ForeColor = AppTheme.Accent, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Underline) };
            DataGridViewTextBoxColumn Locked(string name, string header, int weight, DataGridViewCellStyle style, int min = 48)
            { var c = Col(name, header, weight, style, min); c.ReadOnly = true; return c; }
            gridStores.Columns.Add(Locked("colCode", "Code", 64, roStyle, 64));
            gridStores.Columns["colCode"]!.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
            gridStores.Columns.Add(Locked("colStore", "Store name (click)", 150, linkStyle, 110));
            gridStores.Columns.Add(Col("colDr", "DR #s", 110, editStyle, 96));
            gridStores.Columns.Add(Col("colIn", "Time in", 66, editStyle, 66));
            gridStores.Columns.Add(Col("colOut", "Time out", 66, editStyle, 70));
            gridStores.Columns.Add(Col("colRep", "Received by", 110, editStyle, 100));
            gridStores.Columns.Add(Locked("colLoadE", "Delivered E", 80, roNumStyle, 92));
            gridStores.Columns.Add(Locked("colLoadG", "Delivered G", 80, roNumStyle, 92));
            gridStores.Columns.Add(Locked("colRetE", "Returned E", 80, roNumStyle, 92));
            gridStores.Columns.Add(Locked("colRetG", "Returned G", 80, roNumStyle, 92));
            gridStores.Columns.Add(Col("colRem", "No. Drops", 66, editNumStyle, 80));
            InputGuards.DigitsOnlyColumns(gridStores, "colRem");
            // "…" beside the DR cell opens the DR list. Added last so the other columns keep their positions.
            gridStores.Columns.Add(DrCellSupport.MakeButtonColumn("colDrBtn", Dpi(34)));
            gridStores.Columns["colDrBtn"]!.DisplayIndex = gridStores.Columns["colDr"]!.DisplayIndex + 1;
            DrCellSupport.Attach((MultiValueGrid)gridStores, "colDr", "colDrBtn",
                row => (row.Cells["colStore"].Value?.ToString() ?? "").Trim(),
                row => { RejectDuplicateDrs(row); MarkDuplicateDrs(showToast: false); SizeStoreTable(gridStores.Rows.Count); });
            gridStores.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || isBindingForm) return;
                string cn = gridStores.Columns[e.ColumnIndex].Name;
                if (cn is "colStore" or "colCode" or "colLoadE" or "colLoadG" or "colRetE" or "colRetG") OpenStoreDetails(e.RowIndex);
            };
            gridStores.CellValueChanged += (s, e) =>
            {
                if (isBindingForm || isUpdatingStoreGrid || e.RowIndex < 0) return;
                RecalculateTotalsFromStoreGrid();
            };
            gridStores.CurrentCellDirtyStateChanged += (s, e) =>
            {
                // The DR cell is committed when the person leaves it (Enter adds a new line there).
                if (gridStores.IsCurrentCellDirty && gridStores.CurrentCell != null && gridStores.Columns[gridStores.CurrentCell.ColumnIndex].Name != "colDr")
                {
                    gridStores.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

            // The Trip & Crate Summary follows every added or removed store row.
            gridStores.RowsAdded += (s, e) => { if (!isUpdatingStoreGrid) UpdateTripCrateSummary(); };
            gridStores.RowsRemoved += (s, e) => { if (!isUpdatingStoreGrid) UpdateTripCrateSummary(); };
            pnlMultiDropInfo.Controls.Add(gridStores);
            pnlMultiDropInfo.Controls.Add(lblStoresHint);
            pnlMultiDropInfo.Controls.Add(pnlMultiDropHeaderBar);

            pnlTripFooter = new Panel
            {
                Dock = DockStyle.Top,
                Height = Dpi(150),
                BackColor = AppTheme.Surface,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            var tblFooter = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 4,
                BackColor = AppTheme.Surface
            };
            for (int i = 0; i < 4; i++) tblFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tblFooter.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi(24)));
            tblFooter.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi(44)));
            tblFooter.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi(24)));
            tblFooter.RowStyles.Add(new RowStyle(SizeType.Absolute, Dpi(44)));
            Label MakeLbl(string t) => new Label { Text = t, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, 0, Dpi(12), Dpi(2)) };
            TextBox MakeTxt(string def) => new TextBox { Text = def, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, Dpi(2), Dpi(12), Dpi(6)) };
            txtTruckTimeIn = MakeTxt("");
            txtTruckTimeOut = MakeTxt("");
            txtTruckTimeIn.PlaceholderText = "e.g. 2042 (24-hour)";
            txtTruckTimeOut.PlaceholderText = "e.g. 0139 (24-hour)";
            txtDtrApprovedBy = MakeTxt("RAMIL MENDOZA");
            txtReturnCheckedBy = MakeTxt("");
            txtDtrPreparedBy = MakeTxt("XERXES RYAN MANRIQUE");
            txtDrPreparedBy = MakeTxt("VINJOHN VILLAMOR");
            txtDrCheckedBy = MakeTxt("JOE-EM FLORES");
            txtGuardName = MakeTxt("");

            tblFooter.Controls.Add(MakeLbl("Truck time in"), 0, 0);
            tblFooter.Controls.Add(MakeLbl("Truck time out"), 1, 0);
            tblFooter.Controls.Add(MakeLbl("DTR approved by"), 2, 0);
            tblFooter.Controls.Add(MakeLbl("Return checked by"), 3, 0);
            tblFooter.Controls.Add(txtTruckTimeIn, 0, 1);
            tblFooter.Controls.Add(txtTruckTimeOut, 1, 1);
            tblFooter.Controls.Add(txtDtrApprovedBy, 2, 1);
            tblFooter.Controls.Add(txtReturnCheckedBy, 3, 1);

            tblFooter.Controls.Add(MakeLbl("DTR prepared by"), 0, 2);
            tblFooter.Controls.Add(MakeLbl("DR prepared by"), 1, 2);
            tblFooter.Controls.Add(MakeLbl("DR checked by"), 2, 2);
            tblFooter.Controls.Add(MakeLbl("Guard name & sign"), 3, 2);
            tblFooter.Controls.Add(txtDtrPreparedBy, 0, 3);
            tblFooter.Controls.Add(txtDrPreparedBy, 1, 3);
            tblFooter.Controls.Add(txtDrCheckedBy, 2, 3);
            tblFooter.Controls.Add(txtGuardName, 3, 3);

            pnlTripFooter.Controls.Add(tblFooter);
            InitializeCrewPaySection();
            BuildTripReportCards();
        }
        private void SizeStoreTable(int rowCount)
        {
            if (pnlMultiDropInfo == null || gridStores == null) return;
            // Keep "(N stores)" in the heading right after a store is added or removed.
            if (lblMultiDropDetails != null)
            {
                int n = gridStores.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow);
                lblMultiDropDetails.Text = System.Text.RegularExpressions.Regex.Replace(lblMultiDropDetails.Text, @"\(\d+ stores?\)$", $"({n} store{(n == 1 ? "" : "s")})");
            }
            // Rows grow with their DR lists, so the table is sized from the real row heights.
            try { gridStores.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            int chrome = Dpi(46) + Dpi(24) + Dpi(22);
            int rowsH = gridStores.Rows.Cast<DataGridViewRow>().Sum(r => r.Height);
            int h = chrome + Math.Max(gridStores.ColumnHeadersHeight, Dpi(34)) + Math.Max(rowsH, 3 * Dpi(30));
            pnlMultiDropInfo.Height = Math.Clamp(h, Dpi(240), Dpi(720));
        }

        private void UpdateMultiDropTripInfo(Delivery d)
        {
            if (d == null || gridStores == null) return;
            isUpdatingStoreGrid = true;
            try
            {
                gridStores.Rows.Clear();
                var stops = MJdatabase.GetOrBuildTripStopsForDelivery(d);
                var route = MJdatabase.FindRouteByNameOrCode(d.Destination) ?? MJdatabase.FindRouteByNameOrCode(d.LoadingScheduleInfo);
                string routeTitle = route != null ? $"{route.RouteCode} - {route.RouteName}" : d.CleanRouteDisplay;
                string refNum = $"RCS VILLASIS{d.DispatchDate:MMddyy}-{d.Id:D3}";
                if (lblDispatchInfoBanner != null)
                {
                    lblDispatchInfoBanner.Text = $"📄 Ref: {refNum}   ·   🛣️ Route: {routeTitle}   ·   📅 Date: {d.DispatchDate:dd/MM/yyyy} (leaves {d.DepartureTime})" + Environment.NewLine + $"🚚 Truck: {d.TruckInfo}   ·   👤 Driver: {d.DriverName}   ·   🤝 Aides: {d.AideDisplay}";
                }
                var incidents = MJdatabase.GetIncidentReports().FindAll(ir => ir.DeliveryId == d.Id);
                // An incident note filled in automatically for the previously picked dispatch belongs to that trip only.
                if (autoIncidentRemark.Length > 0 && txtRemarks.Text.Trim() == autoIncidentRemark.Trim()) txtRemarks.Text = "";
                autoIncidentRemark = "";
                if (incidents.Count > 0)
                {
                    string incSummary = string.Join(" | ", incidents.Select(i => $"{i.Cause}: {i.DamagedQuantity} damaged crates (PHP {i.EstimatedCost:N2})"));
                    if (lblDispatchInfoBanner != null)
                    {
                        lblDispatchInfoBanner.Text += Environment.NewLine + $"⚠️ Incident recorded: {incSummary}";
                        lblDispatchInfoBanner.BackColor = Pal.C(254, 242, 242);
                        lblDispatchInfoBanner.ForeColor = Pal.C(185, 28, 28);
                    }
                    if (string.IsNullOrWhiteSpace(txtRemarks.Text))
                    {
                        txtRemarks.Text = $"Incident Recorded: {incSummary}";
                        autoIncidentRemark = txtRemarks.Text;
                    }
                }
                else
                {
                    if (lblDispatchInfoBanner != null)
                    {
                        lblDispatchInfoBanner.BackColor = Pal.C(240, 253, 244);
                        lblDispatchInfoBanner.ForeColor = Pal.C(22, 101, 52);
                    }
                }

                if (lblDispatchInfoBanner != null)
                {
                    int lines = lblDispatchInfoBanner.Text.Split('\n').Length;
                    lblDispatchInfoBanner.Height = Dpi(16) + lines * Dpi(21);
                }
                lblMultiDropDetails.Text = $"🏪 Stores & crates for this trip  ·  {routeTitle} ({stops.Count} stores)";
                foreach (var s in stops)
                {
                    int loadE = (s.DeliveredECrates > 0 || s.DeliveredGCrates > 0) ? s.DeliveredECrates : s.ECrate;
                    int loadG = (s.DeliveredECrates > 0 || s.DeliveredGCrates > 0) ? s.DeliveredGCrates : s.GCrate;
                    int retE = (s.ReturnedECrates > 0 || s.ReturnedGCrates > 0) ? s.ReturnedECrates : loadE;
                    int retG = (s.ReturnedECrates > 0 || s.ReturnedGCrates > 0) ? s.ReturnedGCrates : loadG;

                    int rIdx = gridStores.Rows.Add(
                        s.StoreCode,
                        s.Destination,
                        s.DrNumbers,
                        s.ArrivalTime,
                        s.DepartureTime,
                        s.StoreManagerName,
                        loadE,
                        loadG,
                        retE,
                        retG,
                        DropsFromRemarks(s.Remarks)
                    );
                    gridStores.Rows[rIdx].Tag = s;
                }
                SizeStoreTable(stops.Count);
                MarkDuplicateDrs(showToast: false);
                pnlMultiDropInfo.Visible = true;
                if (tripCrateSummary != null) tripCrateSummary.Visible = true;
            }
            finally
            {
                isUpdatingStoreGrid = false;
            }
        }

        private void OpenTripStoreTableEditor()
        {
            if (gridStores == null) return;

            var colSpecs = new List<SpecialTripColumnDef>();
            foreach (DataGridViewColumn col in gridStores.Columns)
            {
                colSpecs.Add(new SpecialTripColumnDef
                {
                    Key = col.Name,
                    HeaderText = col.HeaderText,
                    Width = col.Width,
                    IsFill = false
                });
            }

            var rowDicts = new List<Dictionary<string, string>>();
            foreach (DataGridViewRow row in gridStores.Rows)
            {
                if (row.IsNewRow) continue;
                var dict = new Dictionary<string, string>();
                foreach (DataGridViewColumn col in gridStores.Columns)
                {
                    dict[col.Name] = row.Cells[col.Name].Value?.ToString() ?? "";
                }
                rowDicts.Add(dict);
            }

            using var dlg = new SpecialTripTableEditorDialog(
                lblMultiDropDetails.Text,
                "Customize store breakdown columns, headers, store times, representatives, and crate counts.",
                colSpecs,
                rowDicts);

            if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
            {
                lblMultiDropDetails.Text = dlg.EditedTableTitle;
                isUpdatingStoreGrid = true;
                try
                {
                    foreach (var spec in dlg.EditedColumns)
                    {
                        if (gridStores.Columns.Contains(spec.Key) && gridStores.Columns[spec.Key] is DataGridViewColumn existingCol)
                        {
                            existingCol.HeaderText = spec.HeaderText;
                        }
                        else
                        {
                            gridStores.Columns.Add(new DataGridViewTextBoxColumn
                            {
                                Name = spec.Key,
                                HeaderText = spec.HeaderText,
                                Width = Math.Max(70, spec.Width),
                                ReadOnly = false
                            });
                        }
                    }

                    gridStores.Rows.Clear();
                    foreach (var rDict in dlg.EditedRows)
                    {
                        int rIdx = gridStores.Rows.Add();
                        var gRow = gridStores.Rows[rIdx];
                        foreach (var kv in rDict)
                        {
                            if (gridStores.Columns.Contains(kv.Key))
                            {
                                gRow.Cells[kv.Key].Value = kv.Value;
                            }
                        }
                    }
                }
                finally
                {
                    isUpdatingStoreGrid = false;
                }
                RecalculateTotalsFromStoreGrid();
            }
        }
        /// <summary>Opens the store window for one row; returns true when the user applied changes.</summary>
        /// <summary>The store's Remarks now hold its number of drops. Older reports stored "Stop 1", "Stop 2"… there
        /// (the stop's position, not a count), so those show as blank instead of a wrong number.</summary>
        private static string DropsFromRemarks(string? remarks)
        {
            string r = (remarks ?? "").Trim();
            return System.Text.RegularExpressions.Regex.IsMatch(r, @"^\d{1,3}$") ? r : "";
        }
        private bool OpenStoreDetails(int rowIndex)
        {
            if (gridStores == null || rowIndex < 0 || rowIndex >= gridStores.Rows.Count) return false;
            gridStores.EndEdit();
            var row = gridStores.Rows[rowIndex];
            string V(string col) => gridStores.Columns.Contains(col) ? (row.Cells[col].Value?.ToString() ?? "").Trim() : "";
            int N(string col) => int.TryParse(V(col), out int n) ? Math.Max(0, n) : 0;
            bool fromDispatch = row.Tag is DispatchStop;
            bool specialTrip = (GetSelectedDelivery()?.Id ?? 0) < 0 || (isEditingExistingReport && (selectedTripReport?.SpecialTripId ?? 0) > 0);
            using var dlg = new TripStoreDialog(V("colCode"), V("colStore"), !fromDispatch, N("colLoadG"), N("colLoadE"), N("colRetG"), N("colRetE"),
                                                V("colDr"), V("colRem"), V("colIn"), V("colOut"), V("colRep"), canEditDelivered: specialTrip);
            var tripDel = GetSelectedDelivery();
            dlg.StoreClient = tripDel?.ClientName ?? "";
            dlg.StoreRoute = tripDel?.Destination;
            dlg.AlreadyListed = st =>
            {
                foreach (DataGridViewRow other in gridStores.Rows)
                {
                    if (other.Index == rowIndex || other.IsNewRow) continue;
                    string oc = (other.Cells["colCode"].Value?.ToString() ?? "").Trim(), on = (other.Cells["colStore"].Value?.ToString() ?? "").Trim();
                    if ((oc.Length > 0 && StoreDirectory.Same(oc, st.StoreCode)) || (oc.Length == 0 && StoreDirectory.Same(on, st.StoreName)))
                        return $"\"{StoreDirectory.Norm(st.StoreName)}\" ({StoreDirectory.Norm(st.StoreCode)}) is already on this trip (row {other.Index + 1}).";
                }
                return null;
            };
            if (dlg.ShowDialog(this.FindForm()) != DialogResult.OK) return false;
            row.Cells["colCode"].Value = dlg.StoreCode;
            row.Cells["colStore"].Value = dlg.StoreName;
            row.Cells["colDr"].Value = DrList.Normalize(dlg.DrNumbers);
            row.Cells["colRetG"].Value = dlg.ReturnedG;
            row.Cells["colRetE"].Value = dlg.ReturnedE;
            if (specialTrip) { row.Cells["colLoadG"].Value = dlg.DeliveredG; row.Cells["colLoadE"].Value = dlg.DeliveredE; }
            if (!fromDispatch)
            {
                if (string.IsNullOrWhiteSpace(V("colLoadE"))) row.Cells["colLoadE"].Value = 0;
                if (string.IsNullOrWhiteSpace(V("colLoadG"))) row.Cells["colLoadG"].Value = 0;
            }
            row.Cells["colRem"].Value = dlg.Drops;
            row.Cells["colIn"].Value = dlg.TimeIn;
            row.Cells["colOut"].Value = dlg.TimeOut;
            row.Cells["colRep"].Value = dlg.Representative;
            RejectDuplicateDrs(row);
            RecalculateTotalsFromStoreGrid();
            SizeStoreTable(gridStores.Rows.Count);
            MarkDuplicateDrs(showToast: false);
            return true;
        }
        /// <summary>The same DR number on two stores of this trip: the DR cells get a warning mark (and a short message).</summary>
        private List<string> MarkDuplicateDrs(bool showToast)
        {
            var msgs = new List<string>();
            if (gridStores == null || !gridStores.Columns.Contains("colDr")) return msgs;
            var where = new Dictionary<string, List<DataGridViewRow>>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in gridStores.Rows)
            {
                if (row.IsNewRow) continue;
                foreach (var dr in DrList.Parse(row.Cells["colDr"].Value?.ToString()))
                {
                    if (!where.TryGetValue(dr, out var l)) where[dr] = l = new List<DataGridViewRow>();
                    if (!l.Contains(row)) l.Add(row);
                }
            }
            string Store(DataGridViewRow r) { string n = (r.Cells["colStore"].Value?.ToString() ?? "").Trim(); return n.Length > 0 ? n : $"row {r.Index + 1}"; }
            var flagged = new Dictionary<DataGridViewRow, List<string>>();
            foreach (var kv in where.Where(k => k.Value.Count > 1))
            {
                msgs.Add($"{kv.Key} is on {string.Join(" and ", kv.Value.Select(Store))}");
                foreach (var r in kv.Value) { if (!flagged.TryGetValue(r, out var l)) flagged[r] = l = new List<string>(); l.Add(kv.Key); }
            }
            foreach (DataGridViewRow row in gridStores.Rows)
            {
                if (row.IsNewRow) continue;
                row.Cells["colDr"].ErrorText = flagged.TryGetValue(row, out var drs) ? $"Also on another store of this trip: {string.Join(", ", drs)}" : "";
            }
            if (showToast && msgs.Count > 0)
                ToastHelper.ShowToast(this, $"Same DR on two stores: {string.Join("; ", msgs.Take(3))}{(msgs.Count > 3 ? "…" : "")}", ToastType.Warning);
            return msgs;
        }
        /// <summary>
        /// A DR number can be used only once. DRs just entered for <paramref name="row"/> that are already on another
        /// store of this trip, or on another dispatch, are taken out again and the person is told where they are.
        /// </summary>
        private void RejectDuplicateDrs(DataGridViewRow row)
        {
            if (gridStores == null || row == null || row.Index < 0 || row.DataGridView != gridStores) return;
            var drs = DrList.Parse(row.Cells["colDr"].Value?.ToString());
            if (drs.Count == 0) return;
            string Store(DataGridViewRow r) { string n = (r.Cells["colStore"].Value?.ToString() ?? "").Trim(); return n.Length > 0 ? n : $"row {r.Index + 1}"; }
            var inTrip = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow other in gridStores.Rows)
            {
                if (other.IsNewRow || other == row) continue;
                foreach (var dr in DrList.Parse(other.Cells["colDr"].Value?.ToString()))
                    if (!inTrip.ContainsKey(dr)) inTrip[dr] = Store(other);
            }
            var del = selectedTripReport != null
                ? allDeliveries.FirstOrDefault(d => d.Id == selectedTripReport.DeliveryId || d.DispatchNumber == selectedTripReport.DispatchNumber)
                : GetSelectedDelivery();
            var elsewhere = MJdatabase.FindDrOwnersOnOtherTrips(del?.Id ?? 0, drs.Where(d => !inTrip.ContainsKey(d)));
            var keep = new List<string>();
            var refused = new List<string>();
            foreach (var dr in drs)
            {
                if (inTrip.TryGetValue(dr, out var st)) refused.Add($"{dr} is already on {st} (this trip)");
                else if (elsewhere.TryGetValue(dr, out var where)) refused.Add($"{dr} is already on {where}");
                else keep.Add(dr);
            }
            if (refused.Count == 0) return;
            bool was = isUpdatingStoreGrid;
            isUpdatingStoreGrid = true;
            try { row.Cells["colDr"].Value = DrList.Join(keep); }
            finally { isUpdatingStoreGrid = was; }
            try { gridStores.AutoResizeRow(row.Index, DataGridViewAutoSizeRowMode.AllCells); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            MessageBox.Show(this.FindForm(), $"A DR number can only be used once, so {(refused.Count == 1 ? "this one was" : "these were")} not added to {Store(row)}:\n\n"
                + string.Join("\n", refused.Take(10).Select(x => "• " + x)) + (refused.Count > 10 ? $"\n• … and {refused.Count - 10} more" : ""),
                "DR number already used", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        /// <summary>
        /// Before saving: DR numbers must not repeat. A DR on two stores of this trip, or already on another dispatch,
        /// stops the save until it is corrected (no "save anyway").
        /// </summary>
        private bool ConfirmDrNumbers(Delivery? del)
        {
            if (gridStores == null) return true;
            gridStores.EndEdit();
            var inTrip = MarkDuplicateDrs(showToast: false);
            var all = gridStores.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).SelectMany(r => DrList.Parse(r.Cells["colDr"].Value?.ToString())).ToList();
            var elsewhere = del != null ? MJdatabase.FindDrNumbersOnOtherTrips(del.Id, all) : new List<string>();
            if (inTrip.Count == 0 && elsewhere.Count == 0) return true;
            string L(List<string> l) => string.Join("\n", l.Take(8).Select(x => "• " + x)) + (l.Count > 8 ? $"\n• … and {l.Count - 8} more" : "");
            string text = (inTrip.Count > 0 ? $"The same DR number is on two stores of this trip:\n{L(inTrip)}\n\n" : "")
                        + (elsewhere.Count > 0 ? $"These DR numbers are already on another dispatch:\n{L(elsewhere)}\n\n" : "")
                        + "A DR number can only be used once. Correct the DR numbers, then save again.";
            MessageBox.Show(this.FindForm(), text, "DR numbers repeated", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        /// <summary>Stores whose returned crates don't match what was delivered, e.g. "RR-Agoo: G short by 2".</summary>
        private List<string> IncompleteStores()
        {
            var list = new List<string>();
            if (gridStores == null) return list;
            foreach (DataGridViewRow row in gridStores.Rows)
            {
                if (row.IsNewRow) continue;
                int I(string c) => int.TryParse(row.Cells[c].Value?.ToString(), out int n) ? Math.Max(0, n) : 0;
                int dg = I("colRetG") - I("colLoadG"), de = I("colRetE") - I("colLoadE");
                if (dg == 0 && de == 0) continue;
                var p = new List<string>();
                if (dg != 0) p.Add($"G {(dg < 0 ? "short by " + (-dg) : "over by " + dg)}");
                if (de != 0) p.Add($"E {(de < 0 ? "short by " + (-de) : "over by " + de)}");
                string nm = (row.Cells["colStore"].Value?.ToString() ?? "").Trim();
                list.Add($"{(nm.Length > 0 ? nm : "Row " + (row.Index + 1))}: {string.Join(", ", p)}");
            }
            return list;
        }
        /// <summary>Double warning before a report with incomplete returned crates is saved.</summary>
        private bool ConfirmIncompleteStoresTwice()
        {
            var inc = IncompleteStores();
            if (inc.Count == 0) return true;
            string list = string.Join("\n", inc.Take(10)) + (inc.Count > 10 ? $"\n… and {inc.Count - 10} more" : "");
            if (MessageBox.Show(this.FindForm(), $"Returned crates don't match the delivered crates for {inc.Count} store(s):\n\n{list}\n\nSave the trip report anyway?",
                    "Incomplete crates (1 of 2)", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return false;
            return MessageBox.Show(this.FindForm(), "Please confirm again: the crate counts above are correct and should be saved as they are?",
                    "Confirm incomplete crates (2 of 2)", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        private void RecalculateTotalsFromStoreGrid()
        {
            if (isUpdatingStoreGrid || gridStores == null) return;
            isUpdatingStoreGrid = true;
            try
            {
                int sumLoadE = 0, sumLoadG = 0, sumRetE = 0, sumRetG = 0;
                string firstRep = "";
                string firstIn = "";
                string lastOut = "";

                foreach (DataGridViewRow row in gridStores.Rows)
                {
                    if (row.IsNewRow) continue;
                    int le = int.TryParse(row.Cells["colLoadE"].Value?.ToString(), out int ple) ? Math.Max(0, ple) : 0;
                    int lg = int.TryParse(row.Cells["colLoadG"].Value?.ToString(), out int plg) ? Math.Max(0, plg) : 0;
                    int re = int.TryParse(row.Cells["colRetE"].Value?.ToString(), out int pre) ? Math.Max(0, pre) : 0;
                    int rg = int.TryParse(row.Cells["colRetG"].Value?.ToString(), out int prg) ? Math.Max(0, prg) : 0;
                    row.Cells["colLoadE"].Value = le;
                    row.Cells["colLoadG"].Value = lg;
                    row.Cells["colRetE"].Value = re;
                    row.Cells["colRetG"].Value = rg;
                    sumLoadE += le;
                    sumLoadG += lg;
                    sumRetE += re;
                    sumRetG += rg;

                    string rep = row.Cells["colRep"].Value?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(firstRep) && !string.IsNullOrEmpty(rep)) firstRep = rep;

                    string sIn = row.Cells["colIn"].Value?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(firstIn) && !string.IsNullOrEmpty(sIn)) firstIn = sIn;

                    string sOut = row.Cells["colOut"].Value?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(sOut)) lastOut = sOut;
                }

                UpdateTripCrateSummary();
                numDeliveredECrates.Value = Math.Min(numDeliveredECrates.Maximum, sumLoadE);
                numDeliveredGCrates.Value = Math.Min(numDeliveredGCrates.Maximum, sumLoadG);
                numReturnedECrates.Value = Math.Min(numReturnedECrates.Maximum, sumRetE);
                numReturnedGCrates.Value = Math.Min(numReturnedGCrates.Maximum, sumRetG);

                // The manager name boxes are hidden on this form: the name always follows the first store's "Received by"
                // (filling it only while empty kept the first letter typed, e.g. "P" from "Pedro Reyes").
                if (!string.IsNullOrEmpty(firstRep))
                {
                    var sp = NameHelper.SplitFullName(firstRep);
                    txtStoreManagerLastName.Text = string.IsNullOrWhiteSpace(sp.LastName) ? firstRep : sp.LastName;
                    txtStoreManagerFirstName.Text = string.IsNullOrWhiteSpace(sp.FirstName) ? "Store Rep" : sp.FirstName;
                    txtStoreManagerMiddleName.Text = sp.MiddleName;
                }
                if (!string.IsNullOrEmpty(firstIn)) txtArrivalTime.Text = firstIn;
                if (!string.IsNullOrEmpty(lastOut)) txtDepartureTime.Text = lastOut;

                CheckCrateVariance();
            }
            finally
            {
                isUpdatingStoreGrid = false;
            }
        }

        private List<DispatchStop> CollectStopsFromGrid(Delivery del)
        {
            var list = new List<DispatchStop>();
            if (gridStores == null) return list;
            int seq = 1;
            foreach (DataGridViewRow row in gridStores.Rows)
            {
                if (row.IsNewRow) continue;
                var orig = row.Tag as DispatchStop;
                int le = int.TryParse(row.Cells["colLoadE"].Value?.ToString(), out int ple) ? Math.Max(0, ple) : 0;
                int lg = int.TryParse(row.Cells["colLoadG"].Value?.ToString(), out int plg) ? Math.Max(0, plg) : 0;
                int re = int.TryParse(row.Cells["colRetE"].Value?.ToString(), out int pre) ? Math.Max(0, pre) : 0;
                int rg = int.TryParse(row.Cells["colRetG"].Value?.ToString(), out int prg) ? Math.Max(0, prg) : 0;
                string rep = row.Cells["colRep"].Value?.ToString()?.Trim() ?? "";
                var sp = NameHelper.SplitFullName(rep);

                list.Add(new DispatchStop
                {
                    DeliveryId = del.Id,
                    StopSequence = seq++,
                    LoadingScheduleId = orig?.LoadingScheduleId ?? (del.LoadingScheduleId > 0 ? del.LoadingScheduleId : null),
                    ClientName = orig?.ClientName ?? del.ClientName,
                    StoreCode = row.Cells["colCode"].Value?.ToString()?.Trim() ?? "",
                    Destination = row.Cells["colStore"].Value?.ToString()?.Trim() ?? "",
                    DrNumbers = DrList.Normalize(row.Cells["colDr"].Value?.ToString()),
                    ArrivalTime = row.Cells["colIn"].Value?.ToString()?.Trim() ?? "",
                    DepartureTime = row.Cells["colOut"].Value?.ToString()?.Trim() ?? "",
                    StoreManagerName = rep,
                    StoreManagerLastName = sp.LastName,
                    StoreManagerFirstName = sp.FirstName,
                    StoreManagerMiddleName = sp.MiddleName,
                    ECrate = orig?.ECrate > 0 ? orig.ECrate : le,
                    GCrate = orig?.GCrate > 0 ? orig.GCrate : lg,
                    TotalCrates = (orig?.ECrate > 0 ? orig.ECrate : le) + (orig?.GCrate > 0 ? orig.GCrate : lg),
                    DeliveredECrates = le,
                    DeliveredGCrates = lg,
                    DeliveredCrates = le + lg,
                    // Box / PFOODS / M.I. are no longer entered; any old values are kept as they were.
                    OthersBox = orig?.OthersBox ?? "",
                    OthersPfoods = orig?.OthersPfoods ?? "",
                    OthersMi = orig?.OthersMi ?? "",
                    ReturnedECrates = re,
                    ReturnedGCrates = rg,
                    ReturnedCrates = re + rg,
                    Remarks = row.Cells["colRem"].Value?.ToString()?.Trim() ?? "",
                    Status = "Delivered"
                });
            }
            return list;
        }

        private void PrintSelectedTripReport()
        {
            if (selectedTripReport != null)
            {
                PrintHelper.PrintOfficialTripReport(selectedTripReport.DeliveryId, selectedTripReport.DispatchNumber);
                return;
            }
            if (cbDelivery.SelectedIndex >= 0)
            {
                string selText = cbDelivery.SelectedItem?.ToString() ?? "";
                var selDel = allDeliveries.FirstOrDefault(del => selText.StartsWith(del.DispatchNumber + " - "));
                if (selDel != null)
                {
                    try { gridStores?.EndEdit(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
                    var stops = CollectStopsFromGrid(selDel);
                    PrintHelper.PrintOfficialTripReport(selDel, selectedTripReport, stops.Count > 0 ? stops : null);
                    return;
                }
            }
            QuietBox.Show("Please select a Trip Report or Dispatch Order to print the Official Trip Report.", "Select Trip", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SetupEvents()
        {
            cbDelivery.SelectedIndexChanged += (s, e) =>
            {
                if (isBindingForm) return;
                string selText = cbDelivery.SelectedItem?.ToString() ?? "";
                var d = allDeliveries.FirstOrDefault(del => selText.StartsWith(del.DispatchNumber + " - "));
                if (d != null)
                {
                    UpdateMultiDropTripInfo(d);
                    RecalculateTotalsFromStoreGrid();
                    LoadCrewPay(d);
                }
                SyncAttachmentOwner();
            };

            numDeliveredGCrates.ValueChanged += (s, e) => { if (!isBindingForm) CheckCrateVariance(); };
            numDeliveredECrates.ValueChanged += (s, e) => { if (!isBindingForm) CheckCrateVariance(); };
            numReturnedGCrates.ValueChanged += (s, e) => { if (!isBindingForm) CheckCrateVariance(); };
            numReturnedECrates.ValueChanged += (s, e) => { if (!isBindingForm) CheckCrateVariance(); };
            SetupDraftAutoSave();
        }

        /// <summary>Settings → Features &amp; Pop-ups → "Check returned empty crates": asks before saving a report with no empties returned.</summary>
        private bool EmptiesRecordedOrOk(int returned, int delivered)
        {
            if (!Features.On(Features.EmptyCrateCheck) || returned > 0 || delivered <= 0) return true;
            return MessageBox.Show(this,
                "No returned empty crates were entered for this trip.\n\nThe crew normally brings back the empty G- and E-crates. Enter how many came back, or click Yes if really none were returned.\n\nSave with 0 returned crates?",
                "Returned empty crates", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }
        private void CheckCrateVariance()
        {
            if (cbDelivery.SelectedIndex < 0)
            {
                btnLogIncident.Visible = false;
                return;
            }
            string selText = cbDelivery.SelectedItem?.ToString() ?? "";
            var selDel = allDeliveries.FirstOrDefault(del => selText.StartsWith(del.DispatchNumber + " - "));
            if (selDel == null)
            {
                btnLogIncident.Visible = false;
                return;
            }

            int dgc = (int)numDeliveredGCrates.Value;
            int dec = (int)numDeliveredECrates.Value;
            int totalDel = dgc + dec;
            int totalDisp = selDel.TotalCrates > 0 ? selDel.TotalCrates : (selDel.GCrate + selDel.ECrate);

            if (totalDisp > totalDel)
            {
                int missing = totalDisp - totalDel;
                btnLogIncident.Text = $"⚠️ Log incident ({missing} missing crates)";
                btnLogIncident.Visible = Features.On(Features.Incidents);
            }
            else
            {
                btnLogIncident.Visible = false;
            }
        }

        private void BtnLogIncident_Click(object? sender, EventArgs e)
        {
            if (cbDelivery.SelectedIndex < 0) return;
            string selText = cbDelivery.SelectedItem?.ToString() ?? "";
            var selDel = allDeliveries.FirstOrDefault(del => selText.StartsWith(del.DispatchNumber + " - "));
            if (selDel == null) return;

            int dgc = (int)numDeliveredGCrates.Value;
            int dec = (int)numDeliveredECrates.Value;
            int totalDel = dgc + dec;
            int totalDisp = selDel.TotalCrates > 0 ? selDel.TotalCrates : (selDel.GCrate + selDel.ECrate);
            int missing = Math.Max(1, totalDisp - totalDel);

            OnLogIncidentRequested?.Invoke(selDel.Id, selDel.DispatchNumber, missing, DateTime.Now);
        }

        private void SetupGridColumns()
        {
            gridTripReports.AutoGenerateColumns = false;
            gridTripReports.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridTripReports.ScrollBars = ScrollBars.Both;
            UIThemeHelper.FitColumnHeaders(gridTripReports, includeCells: true); // full column titles; the table scrolls sideways instead
            gridTripReports.AllowUserToResizeColumns = true;
            gridTripReports.ShowCellToolTips = false;
            gridTripReports.Columns.Clear();
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TripReportNumber", HeaderText = "Report #", FillWeight = 110, MinimumWidth = 125, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DispatchNumber", HeaderText = "Dispatch #", FillWeight = 110, MinimumWidth = 125, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReportDate", HeaderText = "Date", DefaultCellStyle = { Format = "MM/dd/yyyy" }, FillWeight = 80, MinimumWidth = 85, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ArrivalTime", HeaderText = "Arrival", FillWeight = 80, MinimumWidth = 85, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DepartureTime", HeaderText = "Departure", FillWeight = 80, MinimumWidth = 85, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DeliveredGCrates", HeaderText = "Del. G", FillWeight = 65, MinimumWidth = 65, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DeliveredECrates", HeaderText = "Del. E", FillWeight = 65, MinimumWidth = 65, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DeliveredCrates", HeaderText = "Total Del.", FillWeight = 75, MinimumWidth = 75, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReturnedGCrates", HeaderText = "Ret. G", FillWeight = 65, MinimumWidth = 65, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReturnedECrates", HeaderText = "Ret. E", FillWeight = 65, MinimumWidth = 65, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReturnedCrates", HeaderText = "Total Ret.", FillWeight = 75, MinimumWidth = 75, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StoreManagerName", HeaderText = "Manager Signee", FillWeight = 120, MinimumWidth = 125, ReadOnly = true });
            gridTripReports.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Remarks", HeaderText = "Remarks", FillWeight = 130, MinimumWidth = 130, ReadOnly = true });
        }

        private void GridTripReports_SelectionChanged(object? sender, EventArgs e)
        {
            // While the form is open the list is hidden; don't let a background refresh overwrite what is being typed.
            if (pnlForm.Visible) return;
            if (gridTripReports.SelectedRows.Count > 0 && gridTripReports.SelectedRows[0].DataBoundItem is TripReport tr)
            {
                isBindingForm = true;
                selectedTripReport = tr;

                int dIdx = -1;
                for (int i = 0; i < cbDelivery.Items.Count; i++)
                {
                    if ((cbDelivery.Items[i]?.ToString() ?? "").StartsWith(selectedTripReport.DispatchNumber + " - "))
                    {
                        dIdx = i;
                        break;
                    }
                }
                if (dIdx >= 0) cbDelivery.SelectedIndex = dIdx;

                txtArrivalTime.Text = string.IsNullOrEmpty(selectedTripReport.ArrivalTime) ? "10:00 AM" : selectedTripReport.ArrivalTime;
                txtDepartureTime.Text = string.IsNullOrEmpty(selectedTripReport.DepartureTime) ? "11:30 AM" : selectedTripReport.DepartureTime;
                numDeliveredGCrates.Value = Math.Max(0, selectedTripReport.DeliveredGCrates);
                numDeliveredECrates.Value = Math.Max(0, selectedTripReport.DeliveredECrates);
                numReturnedGCrates.Value = Math.Max(0, selectedTripReport.ReturnedGCrates);
                numReturnedECrates.Value = Math.Max(0, selectedTripReport.ReturnedECrates);
                txtStoreManagerLastName.Text = selectedTripReport.StoreManagerLastName ?? "";
                txtStoreManagerFirstName.Text = selectedTripReport.StoreManagerFirstName ?? "";
                txtStoreManagerMiddleName.Text = selectedTripReport.StoreManagerMiddleName ?? "";
                txtRemarks.Text = selectedTripReport.Remarks;
                if (txtTruckTimeIn != null) txtTruckTimeIn.Text = selectedTripReport.TruckTimeIn ?? "";
                if (txtTruckTimeOut != null) txtTruckTimeOut.Text = selectedTripReport.TruckTimeOut ?? "";
                if (txtDtrApprovedBy != null) txtDtrApprovedBy.Text = string.IsNullOrWhiteSpace(selectedTripReport.DtrApprovedBy) ? "RAMIL MENDOZA" : selectedTripReport.DtrApprovedBy;
                if (txtReturnCheckedBy != null) txtReturnCheckedBy.Text = selectedTripReport.ReturnCheckedBy ?? "";
                if (txtDtrPreparedBy != null) txtDtrPreparedBy.Text = string.IsNullOrWhiteSpace(selectedTripReport.DtrPreparedBy) ? "XERXES RYAN MANRIQUE" : selectedTripReport.DtrPreparedBy;
                if (txtDrPreparedBy != null) txtDrPreparedBy.Text = string.IsNullOrWhiteSpace(selectedTripReport.DrPreparedBy) ? "VINJOHN VILLAMOR" : selectedTripReport.DrPreparedBy;
                if (txtDrCheckedBy != null) txtDrCheckedBy.Text = string.IsNullOrWhiteSpace(selectedTripReport.DrCheckedBy) ? "JOE-EM FLORES" : selectedTripReport.DrCheckedBy;
                if (txtGuardName != null) txtGuardName.Text = selectedTripReport.GuardName ?? "";
                var del = allDeliveries.FirstOrDefault(d => d.Id == selectedTripReport.DeliveryId || d.DispatchNumber == selectedTripReport.DispatchNumber);
                if (del != null) UpdateMultiDropTripInfo(del);
                LoadCrewPay(del);
                isBindingForm = false;
                SyncAttachmentOwner();
            }
        }

        private async void LoadData()
        {
            try
            {
                allDeliveries = await MJdatabase.GetDeliveriesAsync();
                crewCache = null;
                var activeStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Scheduled", "Pending", "In Transit", "Rescheduled", "Dispatched" };
                allTripReports = await MJdatabase.GetTripReportsAsync();
                var reported = new HashSet<string>(allTripReports.Select(t => t.DispatchNumber ?? ""), StringComparer.OrdinalIgnoreCase);
                // Special trips ticked "With Trip Report" are listed too (as "Special trip · destination"; Id = −trip Id).
                try { allDeliveries.AddRange(MJdatabase.GetSpecialTripsForTripReports(reported)); }
                catch (Exception sx) { ErrLog.Swallowed(sx); }
                // Orders still needing a report come first: on the road, then delivered without a report, then the rest.
                // Only dispatches marked Completed in Dispatch can get a trip report (plus any that already have one, so it can be edited).
                var ordered = allDeliveries
                    .Where(d => DispatchGate.IsCompleted(d.Status) || reported.Contains(d.DispatchNumber ?? "") || d.Id < 0)
                    .OrderBy(d => activeStatuses.Contains(d.Status ?? "") ? 0
                                : (string.Equals(d.Status, "Completed", StringComparison.OrdinalIgnoreCase) && !reported.Contains(d.DispatchNumber ?? "")) ? 1
                                : string.Equals(d.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) ? 3 : 2)
                    .ToList();
                string? keepDispatch = pnlForm.Visible ? GetSelectedDelivery()?.DispatchNumber : null;
                bool wasBinding = isBindingForm;
                isBindingForm = true;
                cbDelivery.Items.Clear();
                if (allDeliveries.Count == 0)
                {
                    cbDelivery.Text = "(No dispatch orders created yet)";
                }
                else if (ordered.Count == 0)
                {
                    cbDelivery.Text = "(No completed dispatches yet — mark a dispatch completed first)";
                }
                else
                {
                    foreach (var d in ordered)
                    {
                        string tag = reported.Contains(d.DispatchNumber ?? "") ? "  ✓ report filed" : d.Id < 0 && !DispatchGate.IsCompleted(d.Status) ? "  (special trip not completed yet)" : "";
                        cbDelivery.Items.Add($"{d.DispatchNumber} - {d.LoadingScheduleInfo} [{d.Status}] ({d.DriverName}){tag}");
                    }
                    int keepIdx = keepDispatch == null ? -1 : FindDeliveryItemIndex(keepDispatch);
                    cbDelivery.SelectedIndex = keepIdx >= 0 ? keepIdx : 0;
                }

                isBindingForm = wasBinding;
                RefreshCenterData();
                ApplyPendingPicks();
                FilterGridData();
                var reports = allTripReports;

                int totalDel = reports.Sum(r => r.DeliveredCrates);
                int totalRet = reports.Sum(r => r.ReturnedCrates);
                lblSummary.Text = $"📊 Total Completed Trips: {reports.Count:N0}  |  📥 Delivered: {totalDel:N0} Crates  |  🔄 Returned Empties: {totalRet:N0} Crates";
                CheckAndPromptDraft();
                dataLoaded = true;
                if (pendingOpenDispatch != null)
                {
                    string p = pendingOpenDispatch;
                    pendingOpenDispatch = null;
                    OpenForDispatch(p);
                }
            }
            catch (Exception ex)
            { ErrLog.Error("TripReportView", ex); 
                MessageBox.Show($"Failed to load trip completion records: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                var valErrors = new List<string>();
                Control? firstInvalid = null;
                ValidationHelper.ClearErrors(pnlFormBody);

                if (cbDelivery.SelectedIndex < 0)
                {
                    ValidationHelper.ApplyValidationState(cbDelivery, false, "Dispatch order selection is required.");
                    valErrors.Add("Dispatch order selection is required.");
                    firstInvalid ??= cbDelivery;
                }
                if (string.IsNullOrWhiteSpace(txtStoreManagerLastName.Text) && string.IsNullOrWhiteSpace(txtStoreManagerFirstName.Text))
                {
                    // The name comes from the stores' "Received by" column (the manager name boxes are hidden on this form).
                    ValidationHelper.ApplyValidationState(txtStoreManagerLastName, false, "Store Manager / Representative is required.");
                    valErrors.Add("Who received the delivery is required: type a name in \"Received by\" for at least one store.");
                    firstInvalid ??= (Control?)gridStores ?? txtStoreManagerLastName;
                }
                int plannedDelivered = (int)numDeliveredGCrates.Value + (int)numDeliveredECrates.Value;
                bool forSpecialTrip = (GetSelectedDelivery()?.Id ?? 0) < 0; // special trips may carry no crates
                if (plannedDelivered <= 0 && !forSpecialTrip)
                {
                    ValidationHelper.ApplyValidationState(numDeliveredGCrates, false, "Total delivered crates must be greater than zero.");
                    valErrors.Add("Total delivered crates must be greater than 0.");
                    firstInvalid ??= numDeliveredGCrates;
                }

                if (!ValidationHelper.ShowValidationSummaryIfAny(this, valErrors, firstInvalid))
                {
                    return;
                }

                if (cbDelivery.SelectedIndex < 0)
                {
                    MessageBox.Show("Please select a dispatch order to complete.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string selText = cbDelivery.SelectedItem?.ToString() ?? "";
                var selDel = allDeliveries.FirstOrDefault(del => selText.StartsWith(del.DispatchNumber + " - "));
                if (selDel != null && !DispatchGate.RequireCompleted(this, DispatchGate.Fresh(selDel), "file its trip report")) return;
                if (selDel == null)
                {
                    MessageBox.Show("Selected dispatch order not found. Please refresh and try again.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!ConfirmDrNumbers(selDel)) return;
                if (!ConfirmIncompleteStoresTwice()) return;
                var stopsToSave = CollectStopsFromGrid(selDel);
                string smLastName = txtStoreManagerLastName.Text.Trim();
                string smFirstName = txtStoreManagerFirstName.Text.Trim();
                string smMiddleName = txtStoreManagerMiddleName.Text.Trim();

                if (string.IsNullOrWhiteSpace(smLastName) && string.IsNullOrWhiteSpace(smFirstName))
                {
                    var firstRepStop = stopsToSave.FirstOrDefault(st => !string.IsNullOrWhiteSpace(st.StoreManagerName));
                    if (firstRepStop != null)
                    {
                        var sp = NameHelper.SplitFullName(firstRepStop.StoreManagerName);
                        smLastName = string.IsNullOrWhiteSpace(sp.LastName) ? firstRepStop.StoreManagerName : sp.LastName;
                        smFirstName = string.IsNullOrWhiteSpace(sp.FirstName) ? "Store Rep" : sp.FirstName;
                        smMiddleName = sp.MiddleName;
                        txtStoreManagerLastName.Text = smLastName;
                        txtStoreManagerFirstName.Text = smFirstName;
                        txtStoreManagerMiddleName.Text = smMiddleName;
                    }
                    else
                    {
                        smLastName = "Store";
                        smFirstName = "Representative";
                        txtStoreManagerLastName.Text = smLastName;
                        txtStoreManagerFirstName.Text = smFirstName;
                    }
                }

                if (string.IsNullOrWhiteSpace(smLastName) && string.IsNullOrWhiteSpace(smFirstName))
                {
                    var firstRepStop = stopsToSave.FirstOrDefault(st => !string.IsNullOrWhiteSpace(st.StoreManagerName));
                    if (firstRepStop != null)
                    {
                        var sp = NameHelper.SplitFullName(firstRepStop.StoreManagerName);
                        smLastName = string.IsNullOrWhiteSpace(sp.LastName) ? firstRepStop.StoreManagerName : sp.LastName;
                        smFirstName = string.IsNullOrWhiteSpace(sp.FirstName) ? "Store Rep" : sp.FirstName;
                        smMiddleName = sp.MiddleName;
                        txtStoreManagerLastName.Text = smLastName;
                        txtStoreManagerFirstName.Text = smFirstName;
                        txtStoreManagerMiddleName.Text = smMiddleName;
                    }
                    else
                    {
                        smLastName = "Store";
                        smFirstName = "Representative";
                        txtStoreManagerLastName.Text = smLastName;
                        txtStoreManagerFirstName.Text = smFirstName;
                    }
                }

                if (string.IsNullOrWhiteSpace(smLastName) && string.IsNullOrWhiteSpace(smFirstName))
                {
                    var firstRepStop = stopsToSave.FirstOrDefault(st => !string.IsNullOrWhiteSpace(st.StoreManagerName));
                    if (firstRepStop != null)
                    {
                        var sp = NameHelper.SplitFullName(firstRepStop.StoreManagerName);
                        smLastName = string.IsNullOrWhiteSpace(sp.LastName) ? firstRepStop.StoreManagerName : sp.LastName;
                        smFirstName = string.IsNullOrWhiteSpace(sp.FirstName) ? "Store Rep" : sp.FirstName;
                        smMiddleName = sp.MiddleName;
                        txtStoreManagerLastName.Text = smLastName;
                        txtStoreManagerFirstName.Text = smFirstName;
                        txtStoreManagerMiddleName.Text = smMiddleName;
                    }
                    else
                    {
                        smLastName = "Store";
                        smFirstName = "Representative";
                        txtStoreManagerLastName.Text = smLastName;
                        txtStoreManagerFirstName.Text = smFirstName;
                    }
                }

                if (string.IsNullOrWhiteSpace(smLastName) || !SecurityHelper.IsValidName(smLastName))
                {
                    MessageBox.Show("Store Manager / Receiving Personnel Last Name is required for delivery confirmation sign-off.", "Manager Sign-Off Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtStoreManagerLastName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(smFirstName) || !SecurityHelper.IsValidName(smFirstName))
                {
                    MessageBox.Show("Store Manager / Receiving Personnel First Name is required for delivery confirmation sign-off.", "Manager Sign-Off Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtStoreManagerFirstName.Focus();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(smMiddleName) && !SecurityHelper.IsValidName(smMiddleName))
                {
                    MessageBox.Show("Please enter a valid middle name, or leave it blank.", "Manager Sign-Off Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtStoreManagerMiddleName.Focus();
                    return;
                }

                string storeManagerName = NameHelper.FormatFullName(smLastName, smFirstName, smMiddleName);

                if (DateTime.TryParse(txtArrivalTime.Text.Trim(), out var arrTime) && DateTime.TryParse(txtDepartureTime.Text.Trim(), out var depTime))
                {
                    if (depTime < arrTime)
                    {
                        var timePrompt = MessageBox.Show($"Store Departure Time ({txtDepartureTime.Text.Trim()}) is earlier than Store Arrival Time ({txtArrivalTime.Text.Trim()}).\n\nAre you sure this transit chronology is accurate?", "Transit Time Chronology Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (timePrompt != DialogResult.Yes)
                        {
                            txtDepartureTime.Focus();
                            return;
                        }
                    }
                }

                int dgc = (int)numDeliveredGCrates.Value;
                int dec = (int)numDeliveredECrates.Value;
                int rgc = (int)numReturnedGCrates.Value;
                int rec = (int)numReturnedECrates.Value;

                if (!EmptiesRecordedOrOk(rgc + rec, (int)numDeliveredGCrates.Value + (int)numDeliveredECrates.Value)) { numReturnedGCrates.Focus(); return; }
                int totalDispatched = selDel.TotalCrates > 0 ? selDel.TotalCrates : (selDel.GCrate + selDel.ECrate);
                int totalDelivered = dgc + dec;
                if (totalDispatched > 0 && totalDelivered != totalDispatched)
                {
                    var auditPrompt = MessageBox.Show(
                        $"Crate Variance Detected!\n\nDispatched Quantity: {totalDispatched} Crates\nDelivered Quantity: {totalDelivered} Crates\nDifference: {Math.Abs(totalDispatched - totalDelivered)} Crates\n\nDo you wish to proceed with completing this trip report?",
                        "Crate Quantity Reconciliation Notice",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );
                    if (auditPrompt != DialogResult.Yes) return;
                }

                TripReport tr = new TripReport
                {
                    DeliveryId = selDel.Id > 0 ? selDel.Id : 0,
                    SpecialTripId = selDel.Id < 0 ? -selDel.Id : 0,
                    DispatchNumber = selDel.DispatchNumber,
                    ArrivalTime = txtArrivalTime.Text.Trim(),
                    DepartureTime = txtDepartureTime.Text.Trim(),
                    DeliveredGCrates = dgc,
                    DeliveredECrates = dec,
                    DeliveredCrates = dgc + dec,
                    ReturnedGCrates = rgc,
                    ReturnedECrates = rec,
                    ReturnedCrates = rgc + rec,
                    StoreManagerLastName = smLastName,
                    StoreManagerFirstName = smFirstName,
                    StoreManagerMiddleName = smMiddleName,
                    StoreManagerName = storeManagerName,
                    Remarks = txtRemarks.Text.Trim(),
                    ReportDate = DateTime.Now,
                    TruckTimeIn = txtTruckTimeIn?.Text?.Trim() ?? "",
                    TruckTimeOut = txtTruckTimeOut?.Text?.Trim() ?? "",
                    DtrApprovedBy = txtDtrApprovedBy?.Text?.Trim() ?? "RAMIL MENDOZA",
                    ReturnCheckedBy = txtReturnCheckedBy?.Text?.Trim() ?? "",
                    DtrPreparedBy = txtDtrPreparedBy?.Text?.Trim() ?? "XERXES RYAN MANRIQUE",
                    DrPreparedBy = txtDrPreparedBy?.Text?.Trim() ?? "VINJOHN VILLAMOR",
                    DrCheckedBy = txtDrCheckedBy?.Text?.Trim() ?? "JOE-EM FLORES",
                    GuardName = txtGuardName?.Text?.Trim() ?? ""
                };

                if (stopsToSave.Count > 0)
                {
                    MJdatabase.SaveDispatchStops(selDel.Id, stopsToSave);
                }
                MJdatabase.SaveTripReport(tr);
                SaveCrewPayFor(selDel.Id);
                MJdatabase.ClearFormDraft("TripReport", SessionContext.CurrentUsername);
                if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;
                QuietBox.Show("Trip Report saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (selDel.Id > 0 && totalDispatched > totalDelivered && Features.On(Features.Incidents))
                {
                    int missing = totalDispatched - totalDelivered;
                    var promptLog = MessageBox.Show($"Trip Report completed with {missing} missing/damaged crate(s).\n\nWould you like to log an Incident & Damage Report for this delivery now?", "Log Incident Report?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (promptLog == DialogResult.Yes)
                    {
                        OnLogIncidentRequested?.Invoke(selDel.Id, selDel.DispatchNumber, missing, DateTime.Now);
                        return;
                    }
                }

                ClearFields();
                LoadData();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            { ErrLog.Error("TripReportView", ex); 
                MessageBox.Show($"Failed to save trip completion report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedTripReport == null)
                {
                    MessageBox.Show("Please select a trip report to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selDel = allDeliveries.FirstOrDefault(d => d.Id == selectedTripReport.DeliveryId || d.DispatchNumber == selectedTripReport.DispatchNumber);
                if (!ConfirmDrNumbers(selDel)) return;
                if (!ConfirmIncompleteStoresTwice()) return;
                var stopsToSave = selDel != null ? CollectStopsFromGrid(selDel) : new List<DispatchStop>();
                string smLastName = txtStoreManagerLastName.Text.Trim();
                string smFirstName = txtStoreManagerFirstName.Text.Trim();
                string smMiddleName = txtStoreManagerMiddleName.Text.Trim();

                if (string.IsNullOrWhiteSpace(smLastName) && string.IsNullOrWhiteSpace(smFirstName))
                {
                    var firstRepStop = stopsToSave.FirstOrDefault(st => !string.IsNullOrWhiteSpace(st.StoreManagerName));
                    if (firstRepStop != null)
                    {
                        var sp = NameHelper.SplitFullName(firstRepStop.StoreManagerName);
                        smLastName = string.IsNullOrWhiteSpace(sp.LastName) ? firstRepStop.StoreManagerName : sp.LastName;
                        smFirstName = string.IsNullOrWhiteSpace(sp.FirstName) ? "Store Rep" : sp.FirstName;
                        smMiddleName = sp.MiddleName;
                        txtStoreManagerLastName.Text = smLastName;
                        txtStoreManagerFirstName.Text = smFirstName;
                        txtStoreManagerMiddleName.Text = smMiddleName;
                    }
                    else
                    {
                        smLastName = "Store";
                        smFirstName = "Representative";
                        txtStoreManagerLastName.Text = smLastName;
                        txtStoreManagerFirstName.Text = smFirstName;
                    }
                }

                if (string.IsNullOrWhiteSpace(smLastName) && string.IsNullOrWhiteSpace(smFirstName))
                {
                    var firstRepStop = stopsToSave.FirstOrDefault(st => !string.IsNullOrWhiteSpace(st.StoreManagerName));
                    if (firstRepStop != null)
                    {
                        var sp = NameHelper.SplitFullName(firstRepStop.StoreManagerName);
                        smLastName = string.IsNullOrWhiteSpace(sp.LastName) ? firstRepStop.StoreManagerName : sp.LastName;
                        smFirstName = string.IsNullOrWhiteSpace(sp.FirstName) ? "Store Rep" : sp.FirstName;
                        smMiddleName = sp.MiddleName;
                        txtStoreManagerLastName.Text = smLastName;
                        txtStoreManagerFirstName.Text = smFirstName;
                        txtStoreManagerMiddleName.Text = smMiddleName;
                    }
                    else
                    {
                        smLastName = "Store";
                        smFirstName = "Representative";
                        txtStoreManagerLastName.Text = smLastName;
                        txtStoreManagerFirstName.Text = smFirstName;
                    }
                }

                if (string.IsNullOrWhiteSpace(smLastName) && string.IsNullOrWhiteSpace(smFirstName))
                {
                    var firstRepStop = stopsToSave.FirstOrDefault(st => !string.IsNullOrWhiteSpace(st.StoreManagerName));
                    if (firstRepStop != null)
                    {
                        var sp = NameHelper.SplitFullName(firstRepStop.StoreManagerName);
                        smLastName = string.IsNullOrWhiteSpace(sp.LastName) ? firstRepStop.StoreManagerName : sp.LastName;
                        smFirstName = string.IsNullOrWhiteSpace(sp.FirstName) ? "Store Rep" : sp.FirstName;
                        smMiddleName = sp.MiddleName;
                        txtStoreManagerLastName.Text = smLastName;
                        txtStoreManagerFirstName.Text = smFirstName;
                        txtStoreManagerMiddleName.Text = smMiddleName;
                    }
                    else
                    {
                        smLastName = "Store";
                        smFirstName = "Representative";
                        txtStoreManagerLastName.Text = smLastName;
                        txtStoreManagerFirstName.Text = smFirstName;
                    }
                }

                if (string.IsNullOrWhiteSpace(smLastName) || !SecurityHelper.IsValidName(smLastName))
                {
                    MessageBox.Show("Store Manager / Receiving Personnel Last Name is required for delivery confirmation sign-off.", "Manager Sign-Off Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtStoreManagerLastName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(smFirstName) || !SecurityHelper.IsValidName(smFirstName))
                {
                    MessageBox.Show("Store Manager / Receiving Personnel First Name is required for delivery confirmation sign-off.", "Manager Sign-Off Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtStoreManagerFirstName.Focus();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(smMiddleName) && !SecurityHelper.IsValidName(smMiddleName))
                {
                    MessageBox.Show("Please enter a valid middle name, or leave it blank.", "Manager Sign-Off Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtStoreManagerMiddleName.Focus();
                    return;
                }

                string storeManagerName = NameHelper.FormatFullName(smLastName, smFirstName, smMiddleName);

                if (DateTime.TryParse(txtArrivalTime.Text.Trim(), out var arrTime) && DateTime.TryParse(txtDepartureTime.Text.Trim(), out var depTime))
                {
                    if (depTime < arrTime)
                    {
                        var timePrompt = MessageBox.Show($"Store Departure Time ({txtDepartureTime.Text.Trim()}) is earlier than Store Arrival Time ({txtArrivalTime.Text.Trim()}).\n\nAre you sure this transit chronology is accurate?", "Transit Time Chronology Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (timePrompt != DialogResult.Yes)
                        {
                            txtDepartureTime.Focus();
                            return;
                        }
                    }
                }

                int dgc = (int)numDeliveredGCrates.Value;
                int dec = (int)numDeliveredECrates.Value;
                int rgc = (int)numReturnedGCrates.Value;
                int rec = (int)numReturnedECrates.Value;

                if (!EmptiesRecordedOrOk(rgc + rec, (int)numDeliveredGCrates.Value + (int)numDeliveredECrates.Value)) { numReturnedGCrates.Focus(); return; }
                selectedTripReport.ArrivalTime = txtArrivalTime.Text.Trim();
                selectedTripReport.DepartureTime = txtDepartureTime.Text.Trim();
                selectedTripReport.DeliveredGCrates = dgc;
                selectedTripReport.DeliveredECrates = dec;
                selectedTripReport.DeliveredCrates = dgc + dec;
                selectedTripReport.ReturnedGCrates = rgc;
                selectedTripReport.ReturnedECrates = rec;
                selectedTripReport.ReturnedCrates = rgc + rec;
                selectedTripReport.StoreManagerLastName = smLastName;
                selectedTripReport.StoreManagerFirstName = smFirstName;
                selectedTripReport.StoreManagerMiddleName = smMiddleName;
                selectedTripReport.StoreManagerName = storeManagerName;
                selectedTripReport.Remarks = txtRemarks.Text.Trim();
                selectedTripReport.TruckTimeIn = txtTruckTimeIn?.Text?.Trim() ?? "";
                selectedTripReport.TruckTimeOut = txtTruckTimeOut?.Text?.Trim() ?? "";
                selectedTripReport.DtrApprovedBy = txtDtrApprovedBy?.Text?.Trim() ?? "RAMIL MENDOZA";
                selectedTripReport.ReturnCheckedBy = txtReturnCheckedBy?.Text?.Trim() ?? "";
                selectedTripReport.DtrPreparedBy = txtDtrPreparedBy?.Text?.Trim() ?? "XERXES RYAN MANRIQUE";
                selectedTripReport.DrPreparedBy = txtDrPreparedBy?.Text?.Trim() ?? "VINJOHN VILLAMOR";
                selectedTripReport.DrCheckedBy = txtDrCheckedBy?.Text?.Trim() ?? "JOE-EM FLORES";
                selectedTripReport.GuardName = txtGuardName?.Text?.Trim() ?? "";

                if (selDel != null && stopsToSave.Count > 0)
                {
                    MJdatabase.SaveDispatchStops(selDel.Id, stopsToSave);
                }
                MJdatabase.SaveTripReport(selectedTripReport);
                if (selDel != null) SaveCrewPayFor(selDel.Id);
                QuietBox.Show("Trip Report updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadData();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            { ErrLog.Error("TripReportView", ex); 
                MessageBox.Show($"Failed to update trip completion report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedTripReport == null)
                {
                    MessageBox.Show("Please select a trip report to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show($"Are you sure you want to delete Trip Report {selectedTripReport.TripReportNumber}?\n\n" + MJdatabase.DeletePolicyNote("TripReports"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("TripReports", selectedTripReport.Id);
                    if (deleted)
                    {
                        QuietBox.Show(MJdatabase.LastDeleteSummary, "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearFields();
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete Trip Report. It may be locked or referenced by billing invoices.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            { ErrLog.Error("TripReportView", ex); 
                MessageBox.Show($"Delete operation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            ClearFields();
            if (pnlForm.Visible) LoadSelectedDeliveryIntoForm();
        }

        private void FilterGridData()
        {
            string q = txtGridSearch.Text.Trim().ToLowerInvariant();
            var filtered = string.IsNullOrEmpty(q)
                ? allTripReports
                : allTripReports.Where(t =>
                    (!string.IsNullOrEmpty(t.TripReportNumber) && t.TripReportNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.DispatchNumber) && t.DispatchNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.StoreManagerName) && t.StoreManagerName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.StoreManagerLastName) && t.StoreManagerLastName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.StoreManagerFirstName) && t.StoreManagerFirstName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.StoreManagerMiddleName) && t.StoreManagerMiddleName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(t.Remarks) && t.Remarks.ToLower().Contains(q)) ||
                    (DeliveryOf(t) is Delivery dq && ((dq.ClientName ?? "").ToLower().Contains(q) || (dq.DriverName ?? "").ToLower().Contains(q) || (dq.TruckInfo ?? "").ToLower().Contains(q)))
                ).ToList();

            filtered = ApplyCenter(filtered).ToList();
            if (!string.IsNullOrEmpty(currentSortColumn))
            {
                var prop = typeof(TripReport).GetProperty(currentSortColumn);
                if (prop != null) filtered = (isSortAscending ? filtered.OrderBy(x => prop.GetValue(x)) : filtered.OrderByDescending(x => prop.GetValue(x))).ToList();
            }
            gridTripReports.DataSource = null;
            gridTripReports.DataSource = filtered;
            UpdateTripListSummary(filtered.ToList());
            lblGridHeader.Text = filtered.Count == allTripReports.Count
                ? $"Trip reports ({allTripReports.Count})"
                : $"Trip reports — showing {filtered.Count} of {allTripReports.Count}";
            if (filtered.Count > 0)
            {
                gridTripReports.ClearSelection();
                gridTripReports.Rows[0].Selected = true;
            }
            else if (!pnlForm.Visible)
            {
                isBindingForm = true;
                selectedTripReport = null;
                txtStoreManagerLastName.Text = "";
                txtStoreManagerFirstName.Text = "";
                txtStoreManagerMiddleName.Text = "";
                txtRemarks.Text = "";
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

        private bool dataLoaded = false;
        private string? pendingOpenDispatch;
        private int FindDeliveryItemIndex(string dispatchNumber)
        {
            for (int i = 0; i < cbDelivery.Items.Count; i++)
            {
                if ((cbDelivery.Items[i]?.ToString() ?? "").StartsWith(dispatchNumber + " - ", StringComparison.OrdinalIgnoreCase)) return i;
            }
            return -1;
        }
        /// <summary>Opens the trip report for a dispatch: its existing report for editing, or a new report with the dispatch picked.</summary>
        public void OpenForDispatch(string dispatchNumber)
        {
            if (string.IsNullOrWhiteSpace(dispatchNumber)) return;
            if (!dataLoaded)
            {
                pendingOpenDispatch = dispatchNumber;
                return;
            }
            var existing = allTripReports.FirstOrDefault(t => string.Equals(t.DispatchNumber, dispatchNumber, StringComparison.OrdinalIgnoreCase));
            if (pnlForm.Visible) SetFormVisibility(false);
            if (existing != null)
            {
                txtGridSearch.Text = dispatchNumber;
                OpenEditTripReport(existing);
                return;
            }
            var target = allDeliveries.FirstOrDefault(d => string.Equals(d.DispatchNumber, dispatchNumber, StringComparison.OrdinalIgnoreCase));
            if (target != null && !DispatchGate.RequireCompleted(FindForm(), DispatchGate.Fresh(target), "file its trip report")) return;
            OpenNewTripReport();
            int idx = FindDeliveryItemIndex(dispatchNumber);
            if (idx >= 0 && cbDelivery.SelectedIndex != idx) cbDelivery.SelectedIndex = idx;
        }
        public void FilterByDispatch(string dispatchNumber)
        {
            if (txtGridSearch != null)
            {
                txtGridSearch.Text = dispatchNumber ?? "";
                txtGridSearch.Focus();
            }
        }

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            try
            {
                PrintHelper.PrintGridReport("Plant & Store Trip Documentation Report", gridTripReports, "Official trip delivery completion register with G/E crate receipts and store signees.");
            }
            catch (Exception ex)
            { ErrLog.Error("TripReportView", ex); 
                MessageBox.Show($"Print error: {ex.Message}", "Print Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPickArrival_Click(object? sender, EventArgs e)
        {
            string? picked = AnalogClockTimePickerDialog.ShowTimePicker(this.FindForm() ?? this.ParentForm!, txtArrivalTime.Text);
            if (!string.IsNullOrEmpty(picked))
            {
                txtArrivalTime.Text = picked;
            }
        }

        private void BtnPickDeparture_Click(object? sender, EventArgs e)
        {
            string? picked = AnalogClockTimePickerDialog.ShowTimePicker(this.FindForm() ?? this.ParentForm!, txtDepartureTime.Text);
            if (!string.IsNullOrEmpty(picked))
            {
                txtDepartureTime.Text = picked;
            }
        }

        private void ClearFields()
        {
            selectedTripReport = null;
            txtArrivalTime.Text = "10:30 AM";
            txtDepartureTime.Text = "11:45 AM";
            numDeliveredGCrates.Value = 150;
            numDeliveredECrates.Value = 0;
            numReturnedGCrates.Value = 150;
            numReturnedECrates.Value = 0;
            txtStoreManagerLastName.Text = "";
            txtStoreManagerFirstName.Text = "";
            txtStoreManagerMiddleName.Text = "";
            txtRemarks.Text = "";
            if (txtTruckTimeIn != null) txtTruckTimeIn.Text = "";
            if (txtTruckTimeOut != null) txtTruckTimeOut.Text = "";
            if (txtDtrApprovedBy != null) txtDtrApprovedBy.Text = "RAMIL MENDOZA";
            if (txtReturnCheckedBy != null) txtReturnCheckedBy.Text = "";
            if (txtDtrPreparedBy != null) txtDtrPreparedBy.Text = "XERXES RYAN MANRIQUE";
            if (txtDrPreparedBy != null) txtDrPreparedBy.Text = "VINJOHN VILLAMOR";
            if (txtDrCheckedBy != null) txtDrCheckedBy.Text = "JOE-EM FLORES";
            if (txtGuardName != null) txtGuardName.Text = "";
            if (cbDelivery.Items.Count > 0) cbDelivery.SelectedIndex = 0;
            if (pnlMultiDropInfo != null) pnlMultiDropInfo.Visible = false;
            if (tripCrateSummary != null) tripCrateSummary.Visible = false; // shown again with the store table
            LoadCrewPay(null);
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action<TripReport>? OnCreateInvoiceRequested { get; set; }

        private void SetupContextMenu()
        {
            RowMenu.Attach(gridTripReports, (m, rows) =>
            {
                if (rows[0].DataBoundItem is not TripReport tr) return;
                selectedTripReport = tr;
                int shortCrates = 0;
                var del = RowMenu.FindDelivery(tr.DeliveryId, tr.DispatchNumber);
                if (del != null) shortCrates = Math.Max(0, del.TotalCrates - tr.DeliveredCrates - tr.ReturnedCrates);
                Invoice? invoice = null;
                try { invoice = tr.DeliveryId > 0 ? MJdatabase.GetInvoices().FirstOrDefault(i => i.DeliveryId == tr.DeliveryId) : null; } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
                bool invoiced = invoice != null;
                m.Items.AddRange(RowMenu.Header($"📋  Trip report {tr.TripReportNumber}",
                    $"Dispatch {tr.DispatchNumber} · {tr.ReportDate:MMM d, yyyy} · {tr.DeliveredCrates} delivered, {tr.ReturnedCrates} returned{(shortCrates > 0 ? $" · {shortCrates} unaccounted" : "")}"));
                m.Items.Add(RowMenu.Item("Edit trip report", () => OpenEditTripReport(tr), "Enter"));
                m.Items.Add(RowMenu.Item($"Open dispatch {tr.DispatchNumber}", () => RowMenu.OpenDispatch(this, tr.DeliveryId, tr.DispatchNumber)));
                m.Items.Add(RowMenu.Sep());
                if (!invoiced) m.Items.Add(RowMenu.Good("Issue billing invoice for this trip", () => { if (del != null) MainForm.Instance?.CreateInvoiceForDelivery(del); }, del != null));
                else m.Items.Add(RowMenu.Item($"Show invoice {invoice!.InvoiceNumber}", () => MainForm.Instance?.ShowFinanceRecords("Billing", invoice.InvoiceNumber)));
                if (Features.On(Features.Incidents))
                {
                    m.Items.Add(RowMenu.Item(shortCrates > 0 ? $"Log incident ({shortCrates} crates unaccounted)" : "Log an incident for this trip", () =>
                        OnLogIncidentRequested?.Invoke(tr.DeliveryId, tr.DispatchNumber, shortCrates, del?.DispatchDate ?? tr.ReportDate)));
                    m.Items.Add(RowMenu.Item("Show incidents for this dispatch", () => MainForm.Instance?.ShowIncidentsForDispatch(tr.DispatchNumber)));
                }
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Sub("🖨️  Print",
                    RowMenu.Item("Official trip report", () => PrintSelectedTripReport()),
                    RowMenu.Item("End-to-end delivery dossier", () => PrintHelper.PrintEndToEndDeliveryDossier(tr.DeliveryId, tr.DispatchNumber)),
                    RowMenu.Item("Trip report register (table)", () => BtnPrint_Click(this, EventArgs.Empty))));
                m.Items.Add(RowMenu.CopySub(this, ("Trip report number", tr.TripReportNumber), ("Dispatch number", tr.DispatchNumber), ("Store manager", tr.StoreManagerName),
                    ("Summary", $"{tr.TripReportNumber} for {tr.DispatchNumber}: {tr.DeliveredCrates} crates delivered, {tr.ReturnedCrates} returned, arrived {tr.ArrivalTime}, left {tr.DepartureTime}")));
                m.Items.Add(RowMenu.ExportSub(gridTripReports, "Trip_Reports", "Trip Reports", () => BtnPrint_Click(this, EventArgs.Empty)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Danger("Delete trip report…", () => { selectedTripReport = tr; BtnDelete_Click(this, EventArgs.Empty); }, !invoiced || SessionContext.IsAdmin));
            });
            gridTripReports.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && gridTripReports.CurrentRow?.DataBoundItem is TripReport tr) { OpenEditTripReport(tr); e.Handled = true; e.SuppressKeyPress = true; }
            };
        }

        private void SetupDraftAutoSave()
        {
            tmrAutoSaveDraft = new System.Windows.Forms.Timer { Interval = 15000 };
            tmrAutoSaveDraft.Tick += (s, e) => SaveCurrentDraft();
            tmrAutoSaveDraft.Start();
        }

        private void CheckAndPromptDraft()
        {
            // A refresh while the form is open must not bring the banner back or rebuild it (that moved the page while typing).
            if (draftAnswered || (pnlDraftBanner != null && !pnlDraftBanner.IsDisposed && pnlDraftBanner.Visible && pnlDraftBanner.Parent != null)) return;
            try
            {
                var draft = MJdatabase.GetFormDraft("TripReport", SessionContext.CurrentUsername);
                if (draft != null && !string.IsNullOrWhiteSpace(draft.Value.Data))
                {
                    if (pnlDraftBanner != null) { pnlDraftBanner.Parent?.Controls.Remove(pnlDraftBanner); pnlDraftBanner.Dispose(); pnlDraftBanner = null; }
                    pnlDraftBanner = DraftAutoSaveService.CreateDraftBanner("Trip Report", draft.Value.LastSaved, RestoreDraft, DiscardDraft);
                    pnlFormBody.Controls.Add(pnlDraftBanner);
                    pnlFormBody.Controls.SetChildIndex(pnlDraftBanner, pnlFormBody.Controls.Count - 1);
                }
            }
            catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
        }

        private void RestoreDraft()
        {
            try
            {
                var draft = MJdatabase.GetFormDraft("TripReport", SessionContext.CurrentUsername);
                if (draft == null || string.IsNullOrWhiteSpace(draft.Value.Data)) return;
                var map = DraftAutoSaveService.DeserializeDraft(draft.Value.Data);
                draftAnswered = true;
                isBindingForm = true;

                // The dispatch is found by its number (the list order can change); older drafts only kept the position.
                int idx = -1;
                if (map.TryGetValue("DispatchNumber", out var dn) && !string.IsNullOrWhiteSpace(dn))
                {
                    for (int i = 0; i < cbDelivery.Items.Count; i++)
                        if ((cbDelivery.Items[i]?.ToString() ?? "").StartsWith(dn + " - ", StringComparison.Ordinal)) { idx = i; break; }
                }
                else if (map.TryGetValue("DeliveryIndex", out var dIdx) && int.TryParse(dIdx, out int oldIdx) && oldIdx >= 0 && oldIdx < cbDelivery.Items.Count) idx = oldIdx;
                if (idx >= 0) cbDelivery.SelectedIndex = idx;
                if (map.TryGetValue("ArrivalTime", out var at) && !string.IsNullOrWhiteSpace(at))
                {
                    txtArrivalTime.Text = at;
                }
                if (map.TryGetValue("DepartureTime", out var dt) && !string.IsNullOrWhiteSpace(dt))
                {
                    txtDepartureTime.Text = dt;
                }
                if (map.TryGetValue("DeliveredGCrates", out var dgc) && decimal.TryParse(dgc, out var dgcVal))
                {
                    numDeliveredGCrates.Value = Math.Min(numDeliveredGCrates.Maximum, Math.Max(numDeliveredGCrates.Minimum, dgcVal));
                }
                if (map.TryGetValue("DeliveredECrates", out var dec) && decimal.TryParse(dec, out var decVal))
                {
                    numDeliveredECrates.Value = Math.Min(numDeliveredECrates.Maximum, Math.Max(numDeliveredECrates.Minimum, decVal));
                }
                if (map.TryGetValue("ReturnedGCrates", out var rgc) && decimal.TryParse(rgc, out var rgcVal))
                {
                    numReturnedGCrates.Value = Math.Min(numReturnedGCrates.Maximum, Math.Max(numReturnedGCrates.Minimum, rgcVal));
                }
                if (map.TryGetValue("ReturnedECrates", out var rec) && decimal.TryParse(rec, out var recVal))
                {
                    numReturnedECrates.Value = Math.Min(numReturnedECrates.Maximum, Math.Max(numReturnedECrates.Minimum, recVal));
                }
                if (map.TryGetValue("StoreManagerLastName", out var sml) && !string.IsNullOrWhiteSpace(sml))
                {
                    txtStoreManagerLastName.Text = sml;
                }
                if (map.TryGetValue("StoreManagerFirstName", out var smf) && !string.IsNullOrWhiteSpace(smf))
                {
                    txtStoreManagerFirstName.Text = smf;
                }
                if (map.TryGetValue("StoreManagerMiddleName", out var smm) && !string.IsNullOrWhiteSpace(smm))
                {
                    txtStoreManagerMiddleName.Text = smm;
                }
                if (map.TryGetValue("Remarks", out var rem) && !string.IsNullOrWhiteSpace(rem))
                {
                    txtRemarks.Text = rem;
                }

                isBindingForm = false;
                // Load the dispatch's stores, then put back what was typed for each store (DRs, times, receiver, returned crates, drops).
                string selText = cbDelivery.SelectedItem?.ToString() ?? "";
                var selDel = idx >= 0 ? allDeliveries.FirstOrDefault(del => selText.StartsWith(del.DispatchNumber + " - ")) : null;
                if (selDel != null)
                {
                    UpdateMultiDropTripInfo(selDel);
                    if (map.TryGetValue("StoreRows", out var rows) && !string.IsNullOrEmpty(rows)) ApplyDraftStoreRows(rows);
                    RecalculateTotalsFromStoreGrid();
                    LoadCrewPay(selDel);
                    SizeStoreTable(gridStores.Rows.Count);
                    MarkDuplicateDrs(showToast: false);
                    SyncAttachmentOwner();
                }
                CheckCrateVariance();
                ToastHelper.ShowToast(this, "Trip Report draft restored ✓", ToastType.Success);
            }
            catch (Exception ex) { isBindingForm = false; ErrLog.Swallowed(ex); }
        }
        private const char DraftRowSep = '\u001e', DraftFieldSep = '\u001f';
        private static readonly string[] DraftCols = { "colCode", "colStore", "colDr", "colIn", "colOut", "colRep", "colRetE", "colRetG", "colRem" };
        /// <summary>The store rows as typed, for the draft.</summary>
        private string DraftStoreRows()
        {
            if (gridStores == null) return "";
            return string.Join(DraftRowSep, gridStores.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow)
                .Select(r => string.Join(DraftFieldSep, DraftCols.Select(c => (r.Cells[c].Value?.ToString() ?? "").Replace(DraftRowSep, ' ').Replace(DraftFieldSep, ' ')))));
        }
        /// <summary>Puts a draft's typed store values back on the matching stores (same code and name; a store added by hand is added again).</summary>
        private void ApplyDraftStoreRows(string data)
        {
            if (gridStores == null) return;
            isUpdatingStoreGrid = true;
            try
            {
                var used = new HashSet<DataGridViewRow>();
                foreach (var line in data.Split(DraftRowSep))
                {
                    var f = line.Split(DraftFieldSep);
                    if (f.Length != DraftCols.Length) continue;
                    string code = f[0].Trim(), name = f[1].Trim();
                    bool Same(DataGridViewRow r, bool byName) =>
                        string.Equals((r.Cells["colCode"].Value?.ToString() ?? "").Trim(), code, StringComparison.OrdinalIgnoreCase)
                        && (!byName || string.Equals((r.Cells["colStore"].Value?.ToString() ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase));
                    var rows = gridStores.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && !used.Contains(r)).ToList();
                    var row = rows.FirstOrDefault(r => Same(r, true)) ?? (code.Length > 0 ? rows.FirstOrDefault(r => Same(r, false)) : null);
                    if (row == null)
                    {
                        if (code.Length == 0 && name.Length == 0) continue;
                        row = gridStores.Rows[gridStores.Rows.Add()];
                        row.Cells["colCode"].Value = code;
                        row.Cells["colStore"].Value = name;
                        row.Cells["colLoadE"].Value = 0;
                        row.Cells["colLoadG"].Value = 0;
                    }
                    used.Add(row);
                    row.Cells["colDr"].Value = DrList.Normalize(f[2]);
                    row.Cells["colIn"].Value = f[3];
                    row.Cells["colOut"].Value = f[4];
                    row.Cells["colRep"].Value = f[5];
                    if (int.TryParse(f[6], out int re)) row.Cells["colRetE"].Value = Math.Max(0, re);
                    if (int.TryParse(f[7], out int rg)) row.Cells["colRetG"].Value = Math.Max(0, rg);
                    row.Cells["colRem"].Value = f[8];
                }
            }
            finally { isUpdatingStoreGrid = false; }
        }

        private void DiscardDraft()
        {
            draftAnswered = true;
            MJdatabase.ClearFormDraft("TripReport", SessionContext.CurrentUsername);
            if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;
            ToastHelper.ShowToast(this, "Trip Report draft discarded ✓", ToastType.Info);
        }

        private void SaveCurrentDraft()
        {
            if (isBindingForm || selectedTripReport != null) return;
            // While the "Unsaved draft — Restore / Discard" banner is waiting, keep that draft: saving now would overwrite it.
            if (pnlDraftBanner != null && !pnlDraftBanner.IsDisposed && pnlDraftBanner.Visible) return;
            if (string.IsNullOrWhiteSpace(txtStoreManagerLastName.Text) && string.IsNullOrWhiteSpace(txtStoreManagerFirstName.Text) && string.IsNullOrWhiteSpace(txtRemarks.Text) && numDeliveredGCrates.Value == 0 && numDeliveredECrates.Value == 0) return;

            string draftDispatch = "";
            if (cbDelivery.SelectedIndex >= 0)
            {
                string selText = cbDelivery.SelectedItem?.ToString() ?? "";
                draftDispatch = allDeliveries.FirstOrDefault(del => selText.StartsWith(del.DispatchNumber + " - "))?.DispatchNumber ?? "";
            }
            try
            {
                var map = new Dictionary<string, string>
                {
                    ["DeliveryIndex"] = cbDelivery.SelectedIndex.ToString(),
                    ["DispatchNumber"] = draftDispatch,
                    ["StoreRows"] = DraftStoreRows(),
                    ["ArrivalTime"] = txtArrivalTime.Text.Trim(),
                    ["DepartureTime"] = txtDepartureTime.Text.Trim(),
                    ["DeliveredGCrates"] = numDeliveredGCrates.Value.ToString(),
                    ["DeliveredECrates"] = numDeliveredECrates.Value.ToString(),
                    ["ReturnedGCrates"] = numReturnedGCrates.Value.ToString(),
                    ["ReturnedECrates"] = numReturnedECrates.Value.ToString(),
                    ["StoreManagerLastName"] = txtStoreManagerLastName.Text.Trim(),
                    ["StoreManagerFirstName"] = txtStoreManagerFirstName.Text.Trim(),
                    ["StoreManagerMiddleName"] = txtStoreManagerMiddleName.Text.Trim(),
                    ["Remarks"] = txtRemarks.Text.Trim()
                };
                string json = DraftAutoSaveService.SerializeDraft(map);
                MJdatabase.SaveFormDraft("TripReport", SessionContext.CurrentUsername, json);
            }
            catch (System.Exception swx3) { ErrLog.Swallowed(swx3); }
        }
    }
}
