using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public class DeliveryRoutesView : UserControl, IRefreshable
    {
        private DeliveryRoute? selectedRoute = null;
        private bool isBindingForm = false;
        private List<DeliveryRoute> allRoutes = new List<DeliveryRoute>();

        private TableLayoutPanel tblMasterContent = null!;
        private Panel pnlForm = null!;
        private Panel pnlFormHeaderBar = null!;
        private Label lblFormHeader = null!;
        private Button btnCloseForm = null!;
        private Panel pnlFormBody = null!;

        private TextBox txtRouteCode = null!;
        private TextBox txtRouteName = null!;
        private TextBox txtDescription = null!;
        private CheckBox chkActive = null!;

        private TextBox txtNewPlace = null!;
        private Button btnAddPlace = null!;
        private ListBox lstPlaces = null!;
        private Button btnMoveUp = null!;
        private Button btnMoveDown = null!;
        private Button btnRemovePlace = null!;
        private Button btnClearPlaces = null!;
        private Label lblRoutePreview = null!;

        private Button btnSave = null!;
        private Button btnUpdate = null!;
        private Button btnClear = null!;

        private Panel pnlGridCard = null!;
        private DataGridView gridRoutes = null!;
        private TextBox txtGridSearch = null!;
        private ComboBox cbStatusFilter = null!;
        private Label lblGridHeader = null!;
        private Button btnToggleForm = null!;
        private float currentFormWidth = 430F;
        private bool isLoadingRoutes = false;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action? OnNavigateBack { get; set; }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string ReturnTargetName { get; set; } = "Dispatch";
        private Button btnBackToSourceGrid = null!;
        private Button btnBackToSourceForm = null!;

        public DeliveryRoutesView()
        {
            InitializeCustomComponents();
            UIThemeHelper.ApplyTheme(this);
            gridRoutes.ShowCellToolTips = false;
            SetupContextMenu();
            _ = LoadRoutesAsync();
        }

        private void InitializeCustomComponents()
        {
            this.SuspendLayout();
            this.Dock = DockStyle.Fill;
            this.BackColor = Pal.C(241, 245, 249);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            tblMasterContent = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Pal.C(241, 245, 249)
            };
            tblMasterContent.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, currentFormWidth));
            tblMasterContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblMasterContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            pnlForm = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Pal.White,
                Margin = new Padding(0, 0, 12, 0)
            };

            pnlFormHeaderBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Pal.C(248, 250, 252),
                Padding = new Padding(12, 6, 12, 6)
            };

            lblFormHeader = new Label
            {
                Text = "Delivery Route Specification",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Pal.C(15, 23, 42),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            btnCloseForm = new Button
            {
                Text = "✕ Close",
                Font = new Font("Segoe UI", 8.5F),
                Dock = DockStyle.Right,
                Width = 70,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCloseForm.FlatAppearance.BorderSize = 0;
            btnCloseForm.Click += (s, e) => SetFormVisibility(false);

            btnBackToSourceForm = new Button
            {
                Text = "⬅ Back to Dispatch",
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Dock = DockStyle.Right,
                Width = 140,
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = false,
                Margin = new Padding(0, 0, 4, 0)
            };
            btnBackToSourceForm.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnBackToSourceForm.Click += (s, e) => OnNavigateBack?.Invoke();

            pnlFormHeaderBar.Controls.Add(lblFormHeader);
            pnlFormHeaderBar.Controls.Add(btnBackToSourceForm);
            pnlFormHeaderBar.Controls.Add(btnCloseForm);

            pnlFormBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 10, 16, 10)
            };

            var lblCode = new Label { Text = "Route Shortcut / Code * (e.g. R1, R2):", Dock = DockStyle.Top, Height = 20, Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105) };
            txtRouteCode = new TextBox { Dock = DockStyle.Top, Height = 28, Font = new Font("Segoe UI", 9F), ReadOnly = false, BackColor = Pal.White };
            txtRouteCode.TextChanged += (s, e) => { if (!isBindingForm) HandleRouteCodeInput(); };

            var lblName = new Label { Text = "Route Name * (e.g. Route 1, Tarlac-Pampanga Loop):", Dock = DockStyle.Top, Height = 24, Padding = new Padding(0, 4, 0, 0), Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105) };
            txtRouteName = new TextBox { Dock = DockStyle.Top, Height = 28, Font = new Font("Segoe UI", 9F) };

            txtRouteCode.TextChanged += (s, e) => { if (!isBindingForm) LiveDuplicateCheck(); };
            txtRouteName.TextChanged += (s, e) => { if (!isBindingForm) LiveDuplicateCheck(); };
            var lblDesc = new Label { Text = "Description / Service Area:", Dock = DockStyle.Top, Height = 24, Padding = new Padding(0, 4, 0, 0), Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold), ForeColor = Pal.C(71, 85, 105) };
            txtDescription = new TextBox { Dock = DockStyle.Top, Height = 28, Font = new Font("Segoe UI", 9F) };

            chkActive = new CheckBox
            {
                Text = "Active Route (Available for dispatch & schedules)",
                Dock = DockStyle.Top,
                Height = 28,
                Checked = true,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = Pal.C(22, 101, 52)
            };

            var lblPlacesTitle = new Label
            {
                Text = "Stores to Deliver in this Route (Ordered Sequence) *:",
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(0, 6, 0, 0),
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = Pal.C(15, 23, 42)
            };

            var pnlAddPlaceBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 4)
            };
            pnlAddPlaceBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlAddPlaceBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));

            txtNewPlace = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "Type store name / branch...",
                Font = new Font("Segoe UI", 9F)
            };
            txtNewPlace.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    InsertPlaceFromInput();
                }
            };

            btnAddPlace = new Button
            {
                Text = "➕ Add Store",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                BackColor = Pal.C(15, 23, 42),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 0, 0)
            };
            btnAddPlace.FlatAppearance.BorderSize = 0;
            btnAddPlace.Click += (s, e) => InsertPlaceFromInput();

            pnlAddPlaceBar.Controls.Add(txtNewPlace, 0, 0);
            pnlAddPlaceBar.Controls.Add(btnAddPlace, 1, 0);

            lstPlaces = new ListBox
            {
                Dock = DockStyle.Top,
                Height = 140,
                Font = new Font("Segoe UI", 9F),
                IntegralHeight = false,
                BorderStyle = BorderStyle.FixedSingle
            };

            var pnlListButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 4, 0, 4)
            };

            btnMoveUp = CreateSubButton("⬆ Move Up", () => MoveSelectedPlace(-1));
            btnMoveDown = CreateSubButton("⬇ Move Down", () => MoveSelectedPlace(1));
            btnRemovePlace = CreateSubButton("❌ Remove", RemoveSelectedPlace);
            btnClearPlaces = CreateSubButton("🗑 Clear All", ClearAllPlaces);

            pnlListButtons.Controls.Add(btnMoveUp);
            pnlListButtons.Controls.Add(btnMoveDown);
            pnlListButtons.Controls.Add(btnRemovePlace);
            pnlListButtons.Controls.Add(btnClearPlaces);

            lblRoutePreview = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                MaximumSize = new Size(390, 0),
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Pal.C(71, 85, 105),
                Padding = new Padding(0, 4, 0, 8),
                Text = "Route Sequence Preview: (None added yet)"
            };

            var pnlFormActions = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 10, 0, 0)
            };
            pnlFormActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            pnlFormActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            pnlFormActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

            btnSave = new Button
            {
                Text = "💾 SAVE ROUTE",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                BackColor = Pal.C(13, 110, 253),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 3, 0)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => SaveRoute();

            btnUpdate = new Button
            {
                Text = "✏️ UPDATE",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                BackColor = Pal.C(25, 135, 84),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(3, 0, 3, 0)
            };
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.Click += (s, e) => SaveRoute();

            btnClear = new Button
            {
                Text = "🔄 RESET",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(3, 0, 0, 0)
            };
            btnClear.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnClear.Click += (s, e) => ClearForm();

            pnlFormActions.Controls.Add(btnSave, 0, 0);
            pnlFormActions.Controls.Add(btnUpdate, 1, 0);
            pnlFormActions.Controls.Add(btnClear, 2, 0);

            pnlFormBody.Controls.Add(pnlFormActions);
            pnlFormBody.Controls.Add(lblRoutePreview);
            pnlFormBody.Controls.Add(pnlListButtons);
            pnlFormBody.Controls.Add(lstPlaces);
            pnlFormBody.Controls.Add(pnlAddPlaceBar);
            pnlFormBody.Controls.Add(lblPlacesTitle);
            pnlFormBody.Controls.Add(chkActive);
            pnlFormBody.Controls.Add(txtDescription);
            pnlFormBody.Controls.Add(lblDesc);
            pnlFormBody.Controls.Add(txtRouteName);
            pnlFormBody.Controls.Add(lblName);
            pnlFormBody.Controls.Add(txtRouteCode);
            pnlFormBody.Controls.Add(lblCode);

            pnlForm.Controls.Add(pnlFormBody);
            pnlForm.Controls.Add(pnlFormHeaderBar);

            pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Pal.White,
                Padding = new Padding(16)
            };

            var pnlGridHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                Padding = new Padding(0, 0, 0, 8)
            };

            lblGridHeader = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Text = "Customizable Delivery Routes & Connected Stops",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Pal.C(15, 23, 42)
            };

            var flowGridActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            txtGridSearch = new TextBox
            {
                PlaceholderText = "🔍 Search route name, code, or connected place...",
                Size = new Size(270, 28),
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 2, 6, 0)
            };
            txtGridSearch.TextChanged += (s, e) => FilterGrid();

            cbStatusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(110, 28),
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 2, 6, 0)
            };
            cbStatusFilter.Items.AddRange(new object[] { "All Status", "Active", "Inactive" });
            cbStatusFilter.SelectedIndex = 0;
            cbStatusFilter.SelectedIndexChanged += (s, e) => FilterGrid();

            btnToggleForm = new Button
            {
                Text = "📝 + New Route ▾",
                Size = new Size(140, 28),
                BackColor = Pal.C(15, 23, 42),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 2, 6, 0)
            };
            btnToggleForm.FlatAppearance.BorderSize = 0;
            btnToggleForm.Click += (s, e) => SetFormVisibility(!pnlForm.Visible);

            var btnExportCsv = new Button
            {
                Text = "📊 Export CSV",
                Size = new Size(100, 28),
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 6, 0)
            };
            btnExportCsv.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnExportCsv.Click += (s, e) => DataExportHelper.ExportToCsv(gridRoutes, "Delivery_Routes", "Configured Delivery Routes");

            var btnPrintGrid = new Button
            {
                Text = "🖨️ Print Directory",
                Size = new Size(130, 28),
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 0)
            };
            btnPrintGrid.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnPrintGrid.Click += (s, e) => PrintHelper.PrintGridReport("Delivery Route Directory", gridRoutes, "Official master list of customizable delivery routes and connected destination places.");

            btnBackToSourceGrid = new Button
            {
                Text = "⬅ Back to Dispatch",
                Size = new Size(150, 28),
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false,
                Margin = new Padding(0, 2, 6, 0)
            };
            btnBackToSourceGrid.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnBackToSourceGrid.Click += (s, e) => OnNavigateBack?.Invoke();

            flowGridActions.Controls.Add(txtGridSearch);
            flowGridActions.Controls.Add(cbStatusFilter);
            flowGridActions.Controls.Add(btnToggleForm);
            flowGridActions.Controls.Add(btnBackToSourceGrid);
            flowGridActions.Controls.Add(btnExportCsv);
            flowGridActions.Controls.Add(btnPrintGrid);

            pnlGridHeader.Controls.Add(flowGridActions);
            pnlGridHeader.Controls.Add(lblGridHeader);

            gridRoutes = new DataGridView
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
            gridRoutes.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(241, 245, 249);
            gridRoutes.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(71, 85, 105);
            gridRoutes.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            gridRoutes.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
            gridRoutes.ColumnHeadersHeight = 36;
            gridRoutes.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            gridRoutes.DefaultCellStyle.SelectionBackColor = Pal.C(224, 231, 255);
            gridRoutes.DefaultCellStyle.SelectionForeColor = Pal.C(15, 23, 42);
            SetupGridColumns();

            gridRoutes.SelectionChanged += GridRoutes_SelectionChanged;

            pnlGridCard.Controls.Add(gridRoutes);
            pnlGridCard.Controls.Add(pnlGridHeader);

            tblMasterContent.Controls.Add(pnlForm, 0, 0);
            tblMasterContent.Controls.Add(pnlGridCard, 1, 0);

            this.Controls.Add(tblMasterContent);
            this.ResumeLayout(false);

            SetButtonStates(false);
            SetFormVisibility(false);
        }

        private Button CreateSubButton(string text, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 24,
                Font = new Font("Segoe UI", 8F),
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 4, 2)
            };
            btn.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void SetupGridColumns()
        {
            gridRoutes.ShowCellToolTips = false;
            gridRoutes.Columns.Clear();

            gridRoutes.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "IsActive",
                HeaderText = "Active",
                Width = 65,
                ReadOnly = true
            });

            gridRoutes.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "RouteCode",
                HeaderText = "Shortcut / Code",
                Width = 95,
                DefaultCellStyle = { Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) }
            });

            gridRoutes.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "RouteName",
                HeaderText = "Route Name",
                Width = 140,
                DefaultCellStyle = { Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) }
            });

            gridRoutes.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TotalStops",
                HeaderText = "Stores",
                Width = 75,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            gridRoutes.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "FormattedPlaces",
                HeaderText = "Stores to Deliver (Sequence)",
                Width = 360
            });

            gridRoutes.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Description",
                HeaderText = "Description / Service Area",
                Width = 180
            });

            gridRoutes.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridRoutes.Rows[e.RowIndex].DataBoundItem is DeliveryRoute r && !r.IsActive)
                {
                    e.CellStyle.ForeColor = Pal.C(148, 163, 184);
                }
            };
        }

        private void SetupContextMenu()
        {
            RowMenu.Attach(gridRoutes, (m, rows) =>
            {
                if (rows[0].DataBoundItem is not DeliveryRoute r) return;
                if (selectedRoute?.Id != r.Id) BindRouteToForm(r);
                string owner = string.IsNullOrWhiteSpace(r.ClientCompanyName) ? "no client" : r.ClientCompanyName;
                m.Items.AddRange(RowMenu.Header($"🗺️  {r.RouteCode} - {r.RouteName}", $"{RowMenu.Short(owner, 26)} · {r.TotalStops} store(s) · {r.StatusDisplay}"));
                m.Items.Add(RowMenu.Item("Edit route", () => { SetFormVisibility(true); txtRouteName.Focus(); }));
                m.Items.Add(r.IsActive
                    ? RowMenu.Item("Deactivate route (hide from new orders)", () => SetRouteActive(r, false))
                    : RowMenu.Good("Activate route", () => SetRouteActive(r, true)));
                m.Items.Add(RowMenu.Item("Open client profile", () => MainForm.Instance?.OpenClientProfile(r.ClientCompanyName), "", !string.IsNullOrWhiteSpace(r.ClientCompanyName)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.CopySub(this, ("Route code", r.RouteCode), ("Route name", r.RouteName), ("Stores in order", r.FormattedPlaces),
                    ("Stores, one per line", string.Join(Environment.NewLine, r.PlacesList.Select((p, i) => $"{i + 1}. {p}")))));
                m.Items.Add(RowMenu.Item("Other routes of this client", () => RowMenu.Search(txtGridSearch, r.ClientCompanyName), "", !string.IsNullOrWhiteSpace(r.ClientCompanyName)));
                m.Items.Add(RowMenu.ExportSub(gridRoutes, "Delivery_Routes", "Delivery Routes", () => PrintHelper.PrintGridReport("Delivery Routes", gridRoutes, "Configured delivery routes and their stores.")));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Danger("Delete route…", () => DeleteSelectedRoute()));
            });

        }
        private void SetRouteActive(DeliveryRoute r, bool active)
        {
            MJdatabase.ToggleDeliveryRouteActive(r.Id, active);
            r.IsActive = active;
            gridRoutes.Refresh();
            if (selectedRoute?.Id == r.Id) { isBindingForm = true; try { chkActive.Checked = active; } finally { isBindingForm = false; } }
            ToastHelper.ShowToast(this, $"{r.RouteName} is now {(active ? "active" : "inactive")}", ToastType.Success);
        }

        public async void RefreshData()
        {
            await LoadRoutesAsync();
        }

        public async System.Threading.Tasks.Task LoadRoutesAsync()
        {
            if (isLoadingRoutes) return;
            isLoadingRoutes = true;
            try
            {
                allRoutes = await MJdatabase.GetDeliveryRoutesAsync();
                FilterGrid();
                if (string.IsNullOrWhiteSpace(txtRouteCode.Text))
                {
                    txtRouteCode.Text = MJdatabase.GenerateNextRouteCode();
                }
            }
            catch (Exception ex)
            { ErrLog.Error("DeliveryRoutesView", ex); 
                MessageBox.Show("Failed to load delivery routes: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isLoadingRoutes = false;
            }
        }

        private void FilterGrid()
        {
            string q = txtGridSearch.Text.Trim().ToLowerInvariant();
            string status = cbStatusFilter.SelectedItem?.ToString() ?? "All Status";

            var query = allRoutes.AsEnumerable();

            if (status == "Active") query = query.Where(r => r.IsActive);
            else if (status == "Inactive") query = query.Where(r => !r.IsActive);

            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(r =>
                    r.RouteCode.ToLowerInvariant().Contains(q) ||
                    r.RouteName.ToLowerInvariant().Contains(q) ||
                    r.Description.ToLowerInvariant().Contains(q) ||
                    r.ConnectedPlaces.ToLowerInvariant().Contains(q));
            }

            var list = query.ToList();
            gridRoutes.DataSource = list;

            lblGridHeader.Text = string.IsNullOrEmpty(q) && status == "All Status"
                ? $"Configured Delivery Routes ({allRoutes.Count})"
                : $"Configured Delivery Routes - Showing {list.Count} of {allRoutes.Count}";
        }

        private void GridRoutes_SelectionChanged(object? sender, EventArgs e)
        {
            if (isBindingForm) return;

            if (gridRoutes.SelectedRows.Count > 0 && gridRoutes.SelectedRows[0].DataBoundItem is DeliveryRoute r)
            {
                BindRouteToForm(r);
            }
        }

        private void HandleRouteCodeInput()
        {
            string code = txtRouteCode.Text.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                selectedRoute = null;
                lblFormHeader.Text = "Delivery Route Specification";
                SetButtonStates(false);
                return;
            }

            var matched = allRoutes.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.RouteCode) && r.RouteCode.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (matched != null)
            {
                BindRouteToForm(matched);
                for (int i = 0; i < gridRoutes.Rows.Count; i++)
                {
                    if (gridRoutes.Rows[i].DataBoundItem is DeliveryRoute rowR && rowR.Id == matched.Id)
                    {
                        gridRoutes.ClearSelection();
                        gridRoutes.Rows[i].Selected = true;
                        break;
                    }
                }
            }
            else
            {
                selectedRoute = null;
                lblFormHeader.Text = $"Route: {code} (New)";
                SetButtonStates(false);
            }
        }

        private void BindRouteToForm(DeliveryRoute r)
        {
            isBindingForm = true;
            try
            {
                selectedRoute = r;
                txtRouteCode.Text = r.RouteCode;
                txtRouteName.Text = r.RouteName;
                txtDescription.Text = r.Description;
                chkActive.Checked = r.IsActive;

                lstPlaces.Items.Clear();
                foreach (var p in r.PlacesList)
                {
                    lstPlaces.Items.Add(p);
                }

                UpdatePlacesPreview();
                lblFormHeader.Text = $"Route: {r.RouteName} ({r.RouteCode})";
            }
            finally
            {
                isBindingForm = false;
            }
            SetButtonStates(true);
        }

        private void InsertPlaceFromInput()
        {
            string place = txtNewPlace.Text.Trim();
            if (string.IsNullOrWhiteSpace(place)) return;

            var items = place.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item) && !lstPlaces.Items.Contains(item))
                {
                    lstPlaces.Items.Add(item);
                }
            }

            txtNewPlace.Clear();
            txtNewPlace.Focus();
            UpdatePlacesPreview();
        }

        private void MoveSelectedPlace(int direction)
        {
            int idx = lstPlaces.SelectedIndex;
            if (idx < 0) return;

            int targetIdx = idx + direction;
            if (targetIdx < 0 || targetIdx >= lstPlaces.Items.Count) return;

            object item = lstPlaces.Items[idx];
            lstPlaces.Items.RemoveAt(idx);
            lstPlaces.Items.Insert(targetIdx, item);
            lstPlaces.SelectedIndex = targetIdx;
            UpdatePlacesPreview();
        }

        private void RemoveSelectedPlace()
        {
            int idx = lstPlaces.SelectedIndex;
            if (idx >= 0)
            {
                lstPlaces.Items.RemoveAt(idx);
                if (idx < lstPlaces.Items.Count) lstPlaces.SelectedIndex = idx;
                else if (lstPlaces.Items.Count > 0) lstPlaces.SelectedIndex = lstPlaces.Items.Count - 1;
                UpdatePlacesPreview();
            }
        }

        private void ClearAllPlaces()
        {
            lstPlaces.Items.Clear();
            UpdatePlacesPreview();
        }

        private void UpdatePlacesPreview()
        {
            if (lstPlaces.Items.Count == 0)
            {
                lblRoutePreview.Text = "Route Sequence Preview: (None added yet)";
            }
            else
            {
                var places = lstPlaces.Items.Cast<object>().Select(o => o.ToString() ?? "").ToList();
                lblRoutePreview.Text = $"Route Sequence ({places.Count} stops): {string.Join(" ➔ ", places)}";
            }
        }

        private void SetButtonStates(bool isEditing)
        {
            string code = txtRouteCode.Text.Trim();
            var matched = !string.IsNullOrWhiteSpace(code)
                ? allRoutes.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.RouteCode) && r.RouteCode.Equals(code, StringComparison.OrdinalIgnoreCase))
                : null;
            bool exists = (selectedRoute != null || matched != null) && !string.IsNullOrWhiteSpace(code);

            btnSave.Enabled = !exists;
            btnSave.Visible = !exists;
            btnSave.BackColor = !exists ? Pal.C(13, 110, 253) : Pal.C(226, 232, 240);
            btnSave.ForeColor = !exists ? Pal.White : Pal.C(148, 163, 184);
            btnUpdate.Enabled = exists;
            btnUpdate.Visible = exists;
            btnUpdate.BackColor = exists ? Pal.C(25, 135, 84) : Pal.C(226, 232, 240);
            btnUpdate.ForeColor = exists ? Pal.White : Pal.C(148, 163, 184);
        }

        private void SetFormVisibility(bool visible)
        {
            pnlForm.Visible = visible;
            tblMasterContent.ColumnStyles[0].Width = visible ? currentFormWidth : 0F;
            btnToggleForm.Text = visible ? "📝 Hide Form ▴" : "📝 + New Route ▾";
        }

        private void ClearForm()
        {
            selectedRoute = null;
            txtRouteCode.Text = "";
            txtRouteName.Text = "";
            txtDescription.Text = "";
            chkActive.Checked = true;
            txtNewPlace.Text = "";
            lstPlaces.Items.Clear();
            UpdatePlacesPreview();
            lblFormHeader.Text = "Delivery Route Specification";
            SetButtonStates(false);
            gridRoutes.ClearSelection();
        }

        private void SaveRoute()
        {
            try
            {
                var valErrors = new List<string>();
                Control? firstInvalid = null;
                ValidationHelper.ClearErrors(this);

                if (!ValidationHelper.ValidateRequiredText(txtRouteName, "Route Name", valErrors)) firstInvalid ??= txtRouteName;
                if (lstPlaces.Items.Count == 0)
                {
                    valErrors.Add("At least one connected store/destination must be added to this route.");
                    firstInvalid ??= txtNewPlace;
                }

                if (!ValidationHelper.ShowValidationSummaryIfAny(this, valErrors, firstInvalid))
                {
                    return;
                }

                string name = txtRouteName.Text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Please enter a Route Name (e.g. Route 1).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtRouteName.Focus();
                    return;
                }

                if (lstPlaces.Items.Count == 0)
                {
                    MessageBox.Show("Please add at least one connected place / destination to this route.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtNewPlace.Focus();
                    return;
                }

                var placesList = lstPlaces.Items.Cast<object>().Select(o => o.ToString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                var dupPlace = placesList.GroupBy(x => x.Trim(), StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
                if (dupPlace != null)
                {
                    MessageBox.Show($"'{dupPlace.Key}' is in this route more than once. Remove the extra entry.", "Duplicate store", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    lstPlaces.Focus();
                    return;
                }
                int selfId = selectedRoute?.Id ?? 0;
                string typedCode = txtRouteCode.Text.Trim();
                var codeClash = string.IsNullOrWhiteSpace(typedCode) ? null : allRoutes.FirstOrDefault(x => x.Id != selfId && x.RouteCode.Equals(typedCode, StringComparison.OrdinalIgnoreCase));
                if (codeClash != null)
                {
                    MessageBox.Show($"Route code '{typedCode}' is already used by '{codeClash.RouteName}'{(string.IsNullOrWhiteSpace(codeClash.ClientCompanyName) ? "" : $" ({codeClash.ClientCompanyName})")}. Use a different code.", "Duplicate route code", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtRouteCode.Focus();
                    return;
                }
                string owner = selectedRoute?.ClientCompanyName ?? "";
                var nameClash = allRoutes.FirstOrDefault(x => x.Id != selfId && x.RouteName.Equals(name, StringComparison.OrdinalIgnoreCase) && string.Equals(x.ClientCompanyName ?? "", owner, StringComparison.OrdinalIgnoreCase));
                if (nameClash != null)
                {
                    MessageBox.Show($"{(owner.Length > 0 ? owner : "Another route")} already has a route named '{name}' ({nameClash.RouteCode}). Use a different name.", "Duplicate route name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtRouteName.Focus();
                    return;
                }
                string connected = string.Join("; ", placesList);

                DeliveryRoute route = selectedRoute ?? new DeliveryRoute();
                route.RouteCode = !string.IsNullOrWhiteSpace(txtRouteCode.Text) ? txtRouteCode.Text.Trim() : MJdatabase.GenerateNextRouteCode();
                route.RouteName = name;
                route.Description = txtDescription.Text.Trim();
                route.ConnectedPlaces = connected;
                route.IsActive = chkActive.Checked;

                bool success = MJdatabase.SaveDeliveryRoute(route);
                if (success)
                {
                    ToastHelper.ShowToast(this, $"Route '{route.RouteName}' saved successfully ✓");
                    _ = LoadRoutesAsync();
                    ClearForm();
                }
            }
            catch (Exception ex)
            { ErrLog.Error("DeliveryRoutesView", ex); 
                MessageBox.Show("Failed to save delivery route: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private readonly ErrorProvider dupErrors = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };
        /// <summary>Flags a route code or name that is already taken while it is being typed.</summary>
        private void LiveDuplicateCheck()
        {
            int selfId = selectedRoute?.Id ?? 0;
            string code = txtRouteCode.Text.Trim();
            var codeClash = code.Length == 0 ? null : allRoutes.FirstOrDefault(x => x.Id != selfId && x.RouteCode.Trim().Equals(code, StringComparison.OrdinalIgnoreCase));
            dupErrors.SetError(txtRouteCode, codeClash == null ? "" : $"Route code '{code}' is already used by '{codeClash.RouteName}'.");
            txtRouteCode.BackColor = codeClash == null ? Pal.White : AppTheme.DangerSoft;
            string name = System.Text.RegularExpressions.Regex.Replace(txtRouteName.Text.Trim(), @"\s+", " ");
            string owner = selectedRoute?.ClientCompanyName ?? "";
            var nameClash = name.Length == 0 ? null : allRoutes.FirstOrDefault(x => x.Id != selfId && x.RouteName.Trim().Equals(name, StringComparison.OrdinalIgnoreCase) && string.Equals(x.ClientCompanyName ?? "", owner, StringComparison.OrdinalIgnoreCase));
            dupErrors.SetError(txtRouteName, nameClash == null ? "" : $"A route named '{name}' already exists ({nameClash.RouteCode}).");
            txtRouteName.BackColor = nameClash == null ? Pal.White : AppTheme.DangerSoft;
        }
        private void DeleteSelectedRoute()
        {
            if (selectedRoute == null) return;

            var confirm = MessageBox.Show($"Are you sure you want to remove delivery route '{selectedRoute.RouteName}' ({selectedRoute.RouteCode})?\n\n" + MJdatabase.DeletePolicyNote("DeliveryRoutes"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (confirm == DialogResult.Yes)
            {
                try
                {
                    bool deleted = MJdatabase.DeleteDeliveryRoute(selectedRoute.Id);
                    if (deleted)
                    {
                        ToastHelper.ShowToast(this, "Route deactivated — kept for past orders ✓");
                        _ = LoadRoutesAsync();
                        ClearForm();
                    }
                }
                catch (Exception ex)
                { ErrLog.Error("DeliveryRoutesView", ex); 
                    MessageBox.Show("Failed to delete route: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void OpenForShortcut(string? routeCode, bool isNewRoute = false, string returnTarget = "Dispatch", Action? backAction = null)
        {
            ReturnTargetName = returnTarget;
            OnNavigateBack = backAction;
            string backText = $"⬅ Back to {returnTarget}";
            btnBackToSourceGrid.Text = backText;
            btnBackToSourceGrid.Visible = (backAction != null);
            btnBackToSourceForm.Text = backText;
            btnBackToSourceForm.Visible = (backAction != null);

            SetFormVisibility(true);

            if (isNewRoute)
            {
                ClearForm();
                txtRouteCode.Text = MJdatabase.GenerateNextRouteCode();
                txtRouteName.Focus();
            }
            else if (!string.IsNullOrWhiteSpace(routeCode))
            {
                txtGridSearch.Text = routeCode.Trim();
                var match = allRoutes.FirstOrDefault(r =>
                    string.Equals(r.RouteCode, routeCode.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.RouteName, routeCode.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    (r.RouteCode != null && routeCode.Trim().StartsWith(r.RouteCode, StringComparison.OrdinalIgnoreCase)));
                if (match != null)
                {
                    BindRouteToForm(match);
                    txtNewPlace.Focus();
                }
                else
                {
                    ClearForm();
                    txtRouteCode.Text = routeCode.Trim();
                    txtNewPlace.Focus();
                }
            }
            else
            {
                txtGridSearch.Text = "";
            }
        }

        public void ResetNavigationBack()
        {
            OnNavigateBack = null;
            ReturnTargetName = "Dispatch";
            btnBackToSourceGrid.Visible = false;
            btnBackToSourceForm.Visible = false;
        }
    }
}
