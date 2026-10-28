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
    public partial class TripReportView : UserControl, IRefreshable
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
                Text = "📋 Official Trip Report",
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
            menuActions.Items.Add("📋 Print Official Trip Report", null, (s, e) => PrintSelectedTripReport());
            menuActions.Items.Add("📊 Export Trip Documentation to CSV", null, (s, e) => DataExportHelper.ExportToCsv(gridTripReports, "Trip_Reports", "Completed Trip Reports"));
            menuActions.Items.Add("🖨️ Print Trip Reports Register", null, (s, e) => BtnPrint_Click(this, EventArgs.Empty));
            var btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);
            pnlGridActionsFlow.Controls.Add(btnActions);

            currentFormWidth = UIThemeHelper.GetAdaptiveDrawerWidth(this, 420F, 480F);
            InitializeFormToggle();
            UIThemeHelper.ApplyTheme(this);
            StyleFullPageForm();
            InitReportCenter();
        }

        private float currentFormWidth = 420F;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && pnlForm.Visible)
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
            btnToggleForm.Click += (s, e) => { if (pnlForm.Visible) SetFormVisibility(false); else OpenNewTripReport(); };
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
            btnCloseForm.Click += (s, e) => SetFormVisibility(false);
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
                int nextIdx = gridStores.Rows.Count + 1;
                int rIdx = gridStores.Rows.Add($"ST-{nextIdx:00}", $"Store {nextIdx}", "", "08:00 AM", "08:30 AM", "", 0, 0, "", "", "", 0, 0, "");
                SizeStoreTable(gridStores.Rows.Count);
                if (rIdx >= 0 && gridStores.Columns.Contains("colStore"))
                {
                    gridStores.CurrentCell = gridStores.Rows[rIdx].Cells["colStore"];
                    gridStores.BeginEdit(true);
                }
            };

            var btnEditTripStoreTable = MakeBarButton("✎ Edit table", "secondary");
            btnEditTripStoreTable.Click += (s, e) => OpenTripStoreTableEditor();
            pnlMultiDropHeaderBar.Controls.Add(lblMultiDropDetails, 0, 0);
            pnlMultiDropHeaderBar.Controls.Add(btnAddTripStoreRow, 1, 0);
            pnlMultiDropHeaderBar.Controls.Add(btnEditTripStoreTable, 2, 0);
            var lblStoresHint = new Label
            {
                Dock = DockStyle.Top,
                Height = Dpi(24),
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 8.75F),
                ForeColor = AppTheme.TextMuted,
                AutoEllipsis = true,
                Text = "Yellow cells can be edited. Changes here apply to this trip report only; the crate totals below add up automatically."
            };
            gridStores = new DataGridView
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
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = Dpi(34),
                RowTemplate = { Height = Dpi(30) },
                Font = new Font("Segoe UI", 9F),
                EnableHeadersVisualStyles = false
            };
            gridStores.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(219, 234, 254);
            gridStores.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(30, 58, 138);
            gridStores.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5F);
            var roStyle = new DataGridViewCellStyle { BackColor = Pal.C(248, 250, 252), ForeColor = Pal.C(15, 23, 42) };
            var editStyle = new DataGridViewCellStyle { BackColor = Pal.C(254, 252, 232), ForeColor = Pal.C(15, 23, 42) };
            var editNumStyle = new DataGridViewCellStyle { BackColor = Pal.C(254, 252, 232), ForeColor = Pal.C(15, 23, 42), Alignment = DataGridViewContentAlignment.MiddleRight };
            DataGridViewTextBoxColumn Col(string name, string header, int weight, DataGridViewCellStyle style, int min = 48) =>
                new DataGridViewTextBoxColumn { Name = name, HeaderText = header, FillWeight = weight, MinimumWidth = Dpi(min), ReadOnly = false, DefaultCellStyle = new DataGridViewCellStyle(style) };
            gridStores.Columns.Add(Col("colCode", "Store code", 72, editStyle, 60));
            gridStores.Columns["colCode"]!.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
            gridStores.Columns.Add(Col("colStore", "Store name", 150, editStyle, 110));
            gridStores.Columns.Add(Col("colDr", "DR / Inv #", 90, editStyle, 70));
            gridStores.Columns.Add(Col("colIn", "Store in", 66, editStyle, 58));
            gridStores.Columns.Add(Col("colOut", "Store out", 66, editStyle, 58));
            gridStores.Columns.Add(Col("colRep", "Representative", 120, editStyle, 90));
            gridStores.Columns.Add(Col("colLoadE", "Load E", 56, editNumStyle));
            gridStores.Columns.Add(Col("colLoadG", "Load G", 56, editNumStyle));
            gridStores.Columns.Add(Col("colBox", "Box", 44, editStyle, 40));
            gridStores.Columns.Add(Col("colPfoods", "PFOODS", 56, editStyle, 48));
            gridStores.Columns.Add(Col("colMi", "M.I", 44, editStyle, 40));
            gridStores.Columns.Add(Col("colRetE", "Ret E", 56, editNumStyle));
            gridStores.Columns.Add(Col("colRetG", "Ret G", 56, editNumStyle));
            gridStores.Columns.Add(Col("colRem", "Remarks", 110, editStyle, 80));
            gridStores.CellValueChanged += (s, e) =>
            {
                if (isBindingForm || isUpdatingStoreGrid || e.RowIndex < 0) return;
                RecalculateTotalsFromStoreGrid();
            };
            gridStores.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (gridStores.IsCurrentCellDirty)
                {
                    gridStores.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

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
            txtTruckTimeIn = MakeTxt("2042");
            txtTruckTimeOut = MakeTxt("0139");
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
            if (pnlMultiDropInfo == null) return;
            int chrome = Dpi(46) + Dpi(24) + Dpi(34) + Dpi(22);
            int h = chrome + Math.Max(3, rowCount) * Dpi(30);
            pnlMultiDropInfo.Height = Math.Clamp(h, Dpi(240), Dpi(560));
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
                    lblDispatchInfoBanner.Text = $"📄 REF: {refNum}   |   🛣️ ROUTE: {routeTitle}   |   📅 DATE: {d.DispatchDate:dd/MM/yyyy} (STD ETD: {d.DepartureTime})" + Environment.NewLine + $"🚚 TRUCK: {d.TruckInfo}   |   👤 DRIVER: {d.DriverName}   |   🤝 HELPER: {d.AideDisplay}";
                }
                var incidents = MJdatabase.GetIncidentReports().FindAll(ir => ir.DeliveryId == d.Id);
                if (incidents.Count > 0)
                {
                    string incSummary = string.Join(" | ", incidents.Select(i => $"{i.Cause}: {i.DamagedQuantity} damaged crates (PHP {i.EstimatedCost:N2})"));
                    if (lblDispatchInfoBanner != null)
                    {
                        lblDispatchInfoBanner.Text += Environment.NewLine + $"⚠️ INCIDENT RECORDED: {incSummary}";
                        lblDispatchInfoBanner.BackColor = Pal.C(254, 242, 242);
                        lblDispatchInfoBanner.ForeColor = Pal.C(185, 28, 28);
                    }
                    if (string.IsNullOrWhiteSpace(txtRemarks.Text))
                    {
                        txtRemarks.Text = $"Incident Recorded: {incSummary}";
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
                        s.OthersBox,
                        s.OthersPfoods,
                        s.OthersMi,
                        retE,
                        retG,
                        s.Remarks
                    );
                    gridStores.Rows[rIdx].Tag = s;
                }
                SizeStoreTable(stops.Count);
                pnlMultiDropInfo.Visible = true;
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

                numDeliveredECrates.Value = Math.Min(numDeliveredECrates.Maximum, sumLoadE);
                numDeliveredGCrates.Value = Math.Min(numDeliveredGCrates.Maximum, sumLoadG);
                numReturnedECrates.Value = Math.Min(numReturnedECrates.Maximum, sumRetE);
                numReturnedGCrates.Value = Math.Min(numReturnedGCrates.Maximum, sumRetG);

                if (!string.IsNullOrEmpty(firstRep) && string.IsNullOrWhiteSpace(txtStoreManagerLastName.Text) && string.IsNullOrWhiteSpace(txtStoreManagerFirstName.Text))
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
                    DrNumbers = row.Cells["colDr"].Value?.ToString()?.Trim() ?? "",
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
                    OthersBox = row.Cells["colBox"].Value?.ToString()?.Trim() ?? "",
                    OthersPfoods = row.Cells["colPfoods"].Value?.ToString()?.Trim() ?? "",
                    OthersMi = row.Cells["colMi"].Value?.ToString()?.Trim() ?? "",
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
                    var stops = CollectStopsFromGrid(selDel);
                    PrintHelper.PrintOfficialTripReport(selDel, selectedTripReport, stops.Count > 0 ? stops : null);
                    return;
                }
            }
            MessageBox.Show("Please select a Trip Report or Dispatch Order to print the Official Trip Report.", "Select Trip", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            };

            numDeliveredGCrates.ValueChanged += (s, e) => { if (!isBindingForm) CheckCrateVariance(); };
            numDeliveredECrates.ValueChanged += (s, e) => { if (!isBindingForm) CheckCrateVariance(); };
            numReturnedGCrates.ValueChanged += (s, e) => { if (!isBindingForm) CheckCrateVariance(); };
            numReturnedECrates.ValueChanged += (s, e) => { if (!isBindingForm) CheckCrateVariance(); };
            SetupDraftAutoSave();
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
                btnLogIncident.Text = $"⚠️ LOG INCIDENT ({missing} MISSING CRATES)";
                btnLogIncident.Visible = true;
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
                if (txtTruckTimeIn != null) txtTruckTimeIn.Text = string.IsNullOrWhiteSpace(selectedTripReport.TruckTimeIn) ? "2042" : selectedTripReport.TruckTimeIn;
                if (txtTruckTimeOut != null) txtTruckTimeOut.Text = string.IsNullOrWhiteSpace(selectedTripReport.TruckTimeOut) ? "0139" : selectedTripReport.TruckTimeOut;
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
                // Orders still needing a report come first: on the road, then delivered without a report, then the rest.
                // Only dispatches marked Completed in Dispatch can get a trip report (plus any that already have one, so it can be edited).
                var ordered = allDeliveries
                    .Where(d => DispatchGate.IsCompleted(d.Status) || reported.Contains(d.DispatchNumber ?? ""))
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
                        string tag = reported.Contains(d.DispatchNumber ?? "") ? "  ✓ report filed" : "";
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
            {
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
                    ValidationHelper.ApplyValidationState(txtStoreManagerLastName, false, "Store Manager / Representative is required.");
                    valErrors.Add("Store Manager / Representative Name is required.");
                    firstInvalid ??= txtStoreManagerLastName;
                }
                int plannedDelivered = (int)numDeliveredGCrates.Value + (int)numDeliveredECrates.Value;
                if (plannedDelivered <= 0)
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
                    DeliveryId = selDel.Id,
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
                    TruckTimeIn = txtTruckTimeIn?.Text?.Trim() ?? "2042",
                    TruckTimeOut = txtTruckTimeOut?.Text?.Trim() ?? "0139",
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
                MessageBox.Show("Trip Report saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (totalDispatched > totalDelivered)
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
            {
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
                selectedTripReport.TruckTimeIn = txtTruckTimeIn?.Text?.Trim() ?? "2042";
                selectedTripReport.TruckTimeOut = txtTruckTimeOut?.Text?.Trim() ?? "0139";
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
                MessageBox.Show("Trip Report updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadData();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            {
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
                        MessageBox.Show(MJdatabase.LastDeleteSummary, "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            {
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
            {
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
            if (txtTruckTimeIn != null) txtTruckTimeIn.Text = "2042";
            if (txtTruckTimeOut != null) txtTruckTimeOut.Text = "0139";
            if (txtDtrApprovedBy != null) txtDtrApprovedBy.Text = "RAMIL MENDOZA";
            if (txtReturnCheckedBy != null) txtReturnCheckedBy.Text = "";
            if (txtDtrPreparedBy != null) txtDtrPreparedBy.Text = "XERXES RYAN MANRIQUE";
            if (txtDrPreparedBy != null) txtDrPreparedBy.Text = "VINJOHN VILLAMOR";
            if (txtDrCheckedBy != null) txtDrCheckedBy.Text = "JOE-EM FLORES";
            if (txtGuardName != null) txtGuardName.Text = "";
            if (cbDelivery.Items.Count > 0) cbDelivery.SelectedIndex = 0;
            if (pnlMultiDropInfo != null) pnlMultiDropInfo.Visible = false;
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
                try { invoice = tr.DeliveryId > 0 ? MJdatabase.GetInvoices().FirstOrDefault(i => i.DeliveryId == tr.DeliveryId) : null; } catch { }
                bool invoiced = invoice != null;
                m.Items.AddRange(RowMenu.Header($"📋  Trip report {tr.TripReportNumber}",
                    $"Dispatch {tr.DispatchNumber} · {tr.ReportDate:MMM d, yyyy} · {tr.DeliveredCrates} delivered, {tr.ReturnedCrates} returned{(shortCrates > 0 ? $" · {shortCrates} unaccounted" : "")}"));
                m.Items.Add(RowMenu.Item("Edit trip report", () => OpenEditTripReport(tr), "Enter"));
                m.Items.Add(RowMenu.Item($"Open dispatch {tr.DispatchNumber}", () => RowMenu.OpenDispatch(this, tr.DeliveryId, tr.DispatchNumber)));
                m.Items.Add(RowMenu.Sep());
                if (!invoiced) m.Items.Add(RowMenu.Good("Issue billing invoice for this trip", () => { if (del != null) MainForm.Instance?.CreateInvoiceForDelivery(del); }, del != null));
                else m.Items.Add(RowMenu.Item($"Show invoice {invoice!.InvoiceNumber}", () => MainForm.Instance?.ShowFinanceRecords("Billing", invoice.InvoiceNumber)));
                m.Items.Add(RowMenu.Item(shortCrates > 0 ? $"Log incident ({shortCrates} crates unaccounted)" : "Log an incident for this trip", () =>
                    OnLogIncidentRequested?.Invoke(tr.DeliveryId, tr.DispatchNumber, shortCrates, del?.DispatchDate ?? tr.ReportDate)));
                m.Items.Add(RowMenu.Item("Show incidents for this dispatch", () => MainForm.Instance?.ShowIncidentsForDispatch(tr.DispatchNumber)));
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
            try
            {
                var draft = MJdatabase.GetFormDraft("TripReport", SessionContext.CurrentUsername);
                if (draft != null && !string.IsNullOrWhiteSpace(draft.Value.Data))
                {
                    if (pnlDraftBanner != null) pnlFormBody.Controls.Remove(pnlDraftBanner);
                    pnlDraftBanner = DraftAutoSaveService.CreateDraftBanner("Trip Report", draft.Value.LastSaved, RestoreDraft, DiscardDraft);
                    pnlFormBody.Controls.Add(pnlDraftBanner);
                    pnlFormBody.Controls.SetChildIndex(pnlDraftBanner, pnlFormBody.Controls.Count - 1);
                }
            }
            catch { }
        }

        private void RestoreDraft()
        {
            try
            {
                var draft = MJdatabase.GetFormDraft("TripReport", SessionContext.CurrentUsername);
                if (draft == null || string.IsNullOrWhiteSpace(draft.Value.Data)) return;
                var map = DraftAutoSaveService.DeserializeDraft(draft.Value.Data);
                isBindingForm = true;

                if (map.TryGetValue("DeliveryIndex", out var dIdx) && int.TryParse(dIdx, out int idx) && idx >= 0 && idx < cbDelivery.Items.Count)
                {
                    cbDelivery.SelectedIndex = idx;
                }
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
                CheckCrateVariance();
                ToastHelper.ShowToast(this, "Trip Report draft restored ✓", ToastType.Success);
            }
            catch { isBindingForm = false; }
        }

        private void DiscardDraft()
        {
            MJdatabase.ClearFormDraft("TripReport", SessionContext.CurrentUsername);
            if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;
            ToastHelper.ShowToast(this, "Trip Report draft discarded ✓", ToastType.Info);
        }

        private void SaveCurrentDraft()
        {
            if (isBindingForm || selectedTripReport != null) return;
            if (string.IsNullOrWhiteSpace(txtStoreManagerLastName.Text) && string.IsNullOrWhiteSpace(txtStoreManagerFirstName.Text) && string.IsNullOrWhiteSpace(txtRemarks.Text) && numDeliveredGCrates.Value == 0 && numDeliveredECrates.Value == 0) return;

            try
            {
                var map = new Dictionary<string, string>
                {
                    ["DeliveryIndex"] = cbDelivery.SelectedIndex.ToString(),
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
            catch { }
        }
    }
}
