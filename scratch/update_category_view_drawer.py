import os

content = """using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public class CategoryManagementView : UserControl, IRefreshable
    {
        private ComboBox cbCategoryType = null!;
        private Label lblVal = null!;
        private TextBox txtItemValue = null!;
        private Label lblContactPerson = null!;
        private TextBox txtContactPerson = null!;
        private Label lblContactNumber = null!;
        private TextBox txtContactNumber = null!;
        private Label lblAddress = null!;
        private TextBox txtAddress = null!;

        private Panel pnlClientRoutesSection = null!;
        private ComboBox cmbClientRoutes = null!;
        private Button btnNewRoute = null!;
        private TextBox txtRouteCode = null!;
        private TextBox txtRouteName = null!;
        private TextBox txtRouteDescription = null!;
        private CheckBox chkRouteActive = null!;

        private Label lblRouteStoresHeader = null!;
        private TextBox txtNewStoreCode = null!;
        private TextBox txtNewStoreName = null!;
        private Button btnAddStoreStop = null!;
        private DataGridView gridRouteStores = null!;
        private Button btnMoveStoreUp = null!;
        private Button btnMoveStoreDown = null!;
        private Button btnRemoveStore = null!;
        private Button btnClearStores = null!;
        private Button btnSaveRoute = null!;
        private Button btnDeleteRoute = null!;

        private List<DeliveryRoute> currentClientRoutes = new List<DeliveryRoute>();
        private DeliveryRoute? selectedRoute = null;

        private Button btnAdd = null!;
        private Button btnUpdate = null!;
        private Button btnDelete = null!;
        private Button btnClear = null!;

        private TextBox txtGridSearch = null!;
        private Button btnClearSearch = null!;
        private Label lblSummary = null!;
        private DataGridView gridItems = null!;

        private TableLayoutPanel tblMasterContent = null!;
        private Panel pnlForm = null!;
        private Panel pnlFormHeaderBar = null!;
        private Label lblFormHeader = null!;
        private Button btnCloseForm = null!;
        private Panel pnlFormBody = null!;
        private Panel pnlFormActionsBar = null!;
        private Panel pnlGridCard = null!;
        private Panel pnlGridToolbar = null!;
        private Button btnToggleForm = null!;

        private float currentFormWidth = 490F;
        private string selectedCategory = "Client Company";
        private List<ClientCompany> allClients = new List<ClientCompany>();
        private List<string> allGenericItems = new List<string>();

        public CategoryManagementView()
        {
            InitializeComponent();
            currentFormWidth = UIThemeHelper.GetAdaptiveDrawerWidth(this, 460F, 520F);
            SetupGridColumns();
            LoadCategoryData();
            gridItems.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridItems.Rows[e.RowIndex].DataBoundItem is object item) RecordDetailsDialog.ShowDetails(this, item); };
            SetFormVisibility(false);
            UIThemeHelper.ApplyTheme(this);
            UIThemeHelper.AttachSideBySideFormResizer(tblMasterContent, pnlForm, pnlFormBody, gridItems, currentFormWidth, w => currentFormWidth = w);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && pnlForm.Visible)
            {
                SetFormVisibility(false);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public void SetFormVisibility(bool visible)
        {
            tblMasterContent.SuspendLayout();
            pnlForm.Visible = visible;
            if (tblMasterContent.ColumnStyles.Count > 0)
            {
                tblMasterContent.ColumnStyles[0].Width = visible ? currentFormWidth : 0F;
                tblMasterContent.ColumnStyles[0].SizeType = SizeType.Absolute;
            }
            if (tblMasterContent.ColumnStyles.Count > 1)
            {
                tblMasterContent.ColumnStyles[1].Width = 100F;
                tblMasterContent.ColumnStyles[1].SizeType = SizeType.Percent;
            }
            tblMasterContent.ResumeLayout(true);
            if (gridItems.Columns.Count > 0)
            {
                gridItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                gridItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = visible ? "✕ Hide Form ▴" : "📝 + New / Form ▾";
                btnToggleForm.BackColor = visible ? Color.FromArgb(226, 232, 240) : Color.FromArgb(30, 41, 59);
                btnToggleForm.ForeColor = visible ? Color.FromArgb(15, 23, 42) : Color.White;
            }
            MainForm.Instance?.NotifyDrawerToggled(visible);
        }

        public void RefreshData()
        {
            LoadCategoryData();
            if (selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase))
            {
                LoadClientRoutes(txtItemValue.Text.Trim());
            }
        }

        public void OpenForRouteShortcut(string? routeCode = null, bool isNewRoute = false, string returnTarget = "Dispatch", Action? backAction = null)
        {
            cbCategoryType.SelectedItem = "Client Company";
            SetFormVisibility(true);

            if (!string.IsNullOrWhiteSpace(routeCode))
            {
                var route = MJdatabase.FindRouteByNameOrCode(routeCode);
                if (route != null && !string.IsNullOrWhiteSpace(route.ClientCompanyName))
                {
                    for (int i = 0; i < gridItems.Rows.Count; i++)
                    {
                        var cellVal = gridItems.Rows[i].Cells[0].Value?.ToString();
                        if (string.Equals(cellVal, route.ClientCompanyName, StringComparison.OrdinalIgnoreCase))
                        {
                            gridItems.ClearSelection();
                            gridItems.Rows[i].Selected = true;
                            PopulateInputsFromRow(gridItems.Rows[i]);
                            break;
                        }
                    }

                    LoadClientRoutes(route.ClientCompanyName, route.RouteCode);
                }
            }
            else if (isNewRoute)
            {
                BtnNewRoute_Click(this, EventArgs.Empty);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScroll = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Dock = DockStyle.Fill;
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20, 12, 20, 10),
                BackColor = Color.White
            };

            TableLayoutPanel tblHeaderFlow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2
            };
            tblHeaderFlow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblHeaderFlow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tblHeaderFlow.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label lblHeaderTitle = new Label
            {
                Text = "🏢 Client Directory & Dropdown Categories Configuration",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 3)
            };

            Label lblHeaderSub = new Label
            {
                Text = "Manage registered client companies, delivery route assignments, and store loading stop sequences.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Margin = new Padding(0)
            };

            tblHeaderFlow.Controls.Add(lblHeaderTitle, 0, 0);
            tblHeaderFlow.Controls.Add(lblHeaderSub, 0, 1);
            pnlHeader.Controls.Add(tblHeaderFlow);

            tblMasterContent = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(16, 12, 16, 12)
            };
            tblMasterContent.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, currentFormWidth));
            tblMasterContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblMasterContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            pnlForm = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 12, 0)
            };

            pnlFormHeaderBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                Padding = new Padding(14, 6, 14, 6),
                BackColor = Color.FromArgb(241, 245, 249)
            };

            lblFormHeader = new Label
            {
                Text = "Category Configuration & Input Form",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };

            btnCloseForm = new Button
            {
                Text = "✕ Close",
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(68, 26),
                Padding = new Padding(6, 0, 6, 0),
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCloseForm.FlatAppearance.BorderSize = 0;
            btnCloseForm.Click += (s, e) => SetFormVisibility(false);

            pnlFormHeaderBar.Controls.Add(lblFormHeader);
            pnlFormHeaderBar.Controls.Add(btnCloseForm);

            pnlFormActionsBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                Padding = new Padding(12, 10, 12, 10),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            FlowLayoutPanel flowFormBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };

            btnAdd = new Button
            {
                Text = "+ ADD NEW",
                Size = new Size(115, 32),
                Margin = new Padding(0, 0, 6, 0),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += BtnAdd_Click;

            btnUpdate = new Button
            {
                Text = "UPDATE",
                Size = new Size(95, 32),
                Margin = new Padding(0, 0, 6, 0),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.Click += BtnUpdate_Click;

            btnDelete = new Button
            {
                Text = "DELETE",
                Size = new Size(90, 32),
                Margin = new Padding(0, 0, 6, 0),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += BtnDelete_Click;

            btnClear = new Button
            {
                Text = "CLEAR",
                Size = new Size(80, 32),
                Margin = new Padding(0),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 1;
            btnClear.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnClear.Click += (s, e) => ClearInputFields();

            flowFormBtns.Controls.Add(btnAdd);
            flowFormBtns.Controls.Add(btnUpdate);
            flowFormBtns.Controls.Add(btnDelete);
            flowFormBtns.Controls.Add(btnClear);
            pnlFormActionsBar.Controls.Add(flowFormBtns);

            pnlFormBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(14, 10, 14, 10),
                BackColor = Color.White
            };

            TableLayoutPanel tblInputs = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 5
            };
            tblInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            TableLayoutPanel tblRow1 = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 8)
            };
            tblRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tblRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

            Panel pnlCat = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 0, 6, 0) };
            Label lblCat = new Label { Text = "Dropdown Category:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F) };
            cbCategoryType = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cbCategoryType.Items.AddRange(new object[] { "Client Company", "Payment Method" });
            cbCategoryType.SelectedIndex = 0;
            cbCategoryType.SelectedIndexChanged += CbCategoryType_SelectedIndexChanged;
            pnlCat.Controls.Add(cbCategoryType);
            pnlCat.Controls.Add(lblCat);

            Panel pnlVal = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(6, 0, 0, 0) };
            lblVal = new Label { Text = "Client Company Name *:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold) };
            txtItemValue = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), MaxLength = 150 };
            txtItemValue.TextChanged += (s, e) => {
                if (selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase))
                {
                    LoadClientRoutes(txtItemValue.Text.Trim());
                }
            };
            pnlVal.Controls.Add(txtItemValue);
            pnlVal.Controls.Add(lblVal);

            tblRow1.Controls.Add(pnlCat, 0, 0);
            tblRow1.Controls.Add(pnlVal, 1, 0);

            TableLayoutPanel tblRow2 = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 8)
            };
            tblRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            Panel pnlCP = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 0, 6, 0) };
            lblContactPerson = new Label { Text = "Contact Person Name:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F) };
            txtContactPerson = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), MaxLength = 100 };
            pnlCP.Controls.Add(txtContactPerson);
            pnlCP.Controls.Add(lblContactPerson);

            Panel pnlCN = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(6, 0, 0, 0) };
            lblContactNumber = new Label { Text = "Contact Phone / Mobile #:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F) };
            txtContactNumber = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), MaxLength = 20 };
            txtContactNumber.KeyPress += (s, e) => {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '+' && e.KeyChar != '-')
                {
                    e.Handled = true;
                }
            };
            pnlCN.Controls.Add(txtContactNumber);
            pnlCN.Controls.Add(lblContactNumber);

            tblRow2.Controls.Add(pnlCP, 0, 0);
            tblRow2.Controls.Add(pnlCN, 1, 0);

            Panel pnlAddr = new Panel { Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
            lblAddress = new Label { Text = "Address / Branch Location:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F) };
            txtAddress = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), MaxLength = 250 };
            pnlAddr.Controls.Add(txtAddress);
            pnlAddr.Controls.Add(lblAddress);

            pnlClientRoutesSection = BuildClientRoutesSection();

            tblInputs.Controls.Add(tblRow1, 0, 0);
            tblInputs.Controls.Add(tblRow2, 0, 1);
            tblInputs.Controls.Add(pnlAddr, 0, 2);
            tblInputs.Controls.Add(pnlClientRoutesSection, 0, 3);

            pnlFormBody.Controls.Add(tblInputs);

            pnlForm.Controls.Add(pnlFormBody);
            pnlForm.Controls.Add(pnlFormActionsBar);
            pnlForm.Controls.Add(pnlFormHeaderBar);

            pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            pnlGridToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(8, 6, 8, 6),
                BackColor = Color.White
            };

            btnToggleForm = new Button
            {
                Text = "📝 + New / Form ▾",
                Size = new Size(140, 30),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left
            };
            btnToggleForm.FlatAppearance.BorderSize = 0;
            btnToggleForm.Click += (s, e) => SetFormVisibility(!pnlForm.Visible);

            lblSummary = new Label
            {
                Text = "📊 Total Entries: 0",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Padding = new Padding(12, 0, 0, 0)
            };

            FlowLayoutPanel flowSearch = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            Label lblSearch = new Label
            {
                Text = "Search:",
                Font = new Font("Segoe UI Semibold", 8.5F),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize = true,
                Margin = new Padding(0, 4, 6, 0)
            };

            txtGridSearch = new TextBox
            {
                Width = 230,
                MaxLength = 100,
                PlaceholderText = "Search name, contact, address...",
                Margin = new Padding(0, 1, 4, 0)
            };
            txtGridSearch.TextChanged += (s, e) => FilterGrid();

            btnClearSearch = new Button
            {
                Text = "✕",
                Size = new Size(32, 26),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btnClearSearch.FlatAppearance.BorderSize = 0;
            btnClearSearch.Click += (s, e) => { txtGridSearch.Text = ""; txtGridSearch.Focus(); };

            flowSearch.Controls.Add(lblSearch);
            flowSearch.Controls.Add(txtGridSearch);
            flowSearch.Controls.Add(btnClearSearch);

            pnlGridToolbar.Controls.Add(flowSearch);
            pnlGridToolbar.Controls.Add(lblSummary);
            pnlGridToolbar.Controls.Add(btnToggleForm);

            gridItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeight = 36,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                RowTemplate = { Height = 32 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                ShowCellToolTips = false,
                ScrollBars = ScrollBars.Both,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            gridItems.SelectionChanged += GridItems_SelectionChanged;

            pnlGridCard.Controls.Add(gridItems);
            pnlGridCard.Controls.Add(pnlGridToolbar);

            tblMasterContent.Controls.Add(pnlForm, 0, 0);
            tblMasterContent.Controls.Add(pnlGridCard, 1, 0);

            this.Controls.Add(tblMasterContent);
            this.Controls.Add(pnlHeader);

            pnlHeader.BringToFront();
            tblMasterContent.BringToFront();

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private Panel BuildClientRoutesSection()
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 4, 0, 8),
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(10)
            };

            Label lblSectionTitle = new Label
            {
                Text = "🛣️ Delivery Routes & Store Sequences (Connected to Loading Schedule)",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 26
            };

            Panel pnlSelectorRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                Margin = new Padding(0, 2, 0, 6)
            };

            Label lblSel = new Label
            {
                Text = "Select Route:",
                Font = new Font("Segoe UI Semibold", 8.5F),
                Dock = DockStyle.Left,
                Width = 85,
                TextAlign = ContentAlignment.MiddleLeft
            };

            btnNewRoute = new Button
            {
                Text = "+ New Route",
                Dock = DockStyle.Right,
                Width = 100,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNewRoute.FlatAppearance.BorderSize = 0;
            btnNewRoute.Click += BtnNewRoute_Click;

            cmbClientRoutes = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cmbClientRoutes.SelectedIndexChanged += CmbClientRoutes_SelectedIndexChanged;

            pnlSelectorRow.Controls.Add(cmbClientRoutes);
            pnlSelectorRow.Controls.Add(lblSel);
            pnlSelectorRow.Controls.Add(btnNewRoute);

            TableLayoutPanel tblRouteInputs = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 6, 0, 6)
            };
            tblRouteInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tblRouteInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

            Panel pnlRCode = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 0, 6, 0) };
            Label lblRCode = new Label { Text = "Route Code *:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F) };
            txtRouteCode = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), MaxLength = 20 };
            pnlRCode.Controls.Add(txtRouteCode);
            pnlRCode.Controls.Add(lblRCode);

            Panel pnlRName = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(6, 0, 0, 0) };
            Label lblRName = new Label { Text = "Route Name *:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F) };
            txtRouteName = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), MaxLength = 100 };
            pnlRName.Controls.Add(txtRouteName);
            pnlRName.Controls.Add(lblRName);

            tblRouteInputs.Controls.Add(pnlRCode, 0, 0);
            tblRouteInputs.Controls.Add(pnlRName, 1, 0);

            Panel pnlRDesc = new Panel { Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 0, 0, 6) };
            Label lblRDesc = new Label { Text = "Description / Region:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F) };
            txtRouteDescription = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), MaxLength = 200 };
            pnlRDesc.Controls.Add(txtRouteDescription);
            pnlRDesc.Controls.Add(lblRDesc);

            chkRouteActive = new CheckBox
            {
                Text = "Active Delivery Route (Available in Loading & Dispatch)",
                Checked = true,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI Semibold", 8.5F),
                Height = 24
            };

            lblRouteStoresHeader = new Label
            {
                Text = "🏪 Route Stores & Sequence (Loading Stops):",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 26,
                Margin = new Padding(0, 6, 0, 2)
            };

            TableLayoutPanel tblAddStoreBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                ColumnCount = 3,
                Margin = new Padding(0, 0, 0, 6)
            };
            tblAddStoreBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            tblAddStoreBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblAddStoreBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));

            txtNewStoreCode = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Store Code",
                MaxLength = 20,
                Margin = new Padding(0, 1, 4, 1)
            };

            txtNewStoreName = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Store Name (e.g. RR-Laoag Bacarra)",
                MaxLength = 100,
                Margin = new Padding(0, 1, 4, 1)
            };
            txtNewStoreName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { BtnAddStoreStop_Click(this, EventArgs.Empty); e.Handled = true; e.SuppressKeyPress = true; } };

            btnAddStoreStop = new Button
            {
                Text = "+ Add Stop",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 0, 1)
            };
            btnAddStoreStop.FlatAppearance.BorderSize = 0;
            btnAddStoreStop.Click += BtnAddStoreStop_Click;

            tblAddStoreBar.Controls.Add(txtNewStoreCode, 0, 0);
            tblAddStoreBar.Controls.Add(txtNewStoreName, 1, 0);
            tblAddStoreBar.Controls.Add(btnAddStoreStop, 2, 0);

            gridRouteStores = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 175,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ColumnHeadersHeight = 28,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                RowTemplate = { Height = 26 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                ShowCellToolTips = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Margin = new Padding(0, 0, 0, 6)
            };
            gridRouteStores.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(219, 234, 254);
            gridRouteStores.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 58, 138);
            gridRouteStores.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);

            gridRouteStores.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#", FillWeight = 15, MinimumWidth = 35 });
            gridRouteStores.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Store Code", FillWeight = 30, MinimumWidth = 80 });
            gridRouteStores.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Store Name", FillWeight = 55, MinimumWidth = 140 });

            FlowLayoutPanel flowSeqBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 2, 0, 8)
            };

            btnMoveStoreUp = new Button
            {
                Text = "⬆️ Move Up",
                Size = new Size(88, 28),
                Margin = new Padding(0, 0, 6, 4),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnMoveStoreUp.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnMoveStoreUp.FlatAppearance.BorderSize = 1;
            btnMoveStoreUp.Click += BtnMoveStoreUp_Click;

            btnMoveStoreDown = new Button
            {
                Text = "⬇️ Move Down",
                Size = new Size(100, 28),
                Margin = new Padding(0, 0, 6, 4),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnMoveStoreDown.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnMoveStoreDown.FlatAppearance.BorderSize = 1;
            btnMoveStoreDown.Click += BtnMoveStoreDown_Click;

            btnRemoveStore = new Button
            {
                Text = "🗑️ Remove",
                Size = new Size(82, 28),
                Margin = new Padding(0, 0, 6, 4),
                BackColor = Color.FromArgb(254, 242, 242),
                ForeColor = Color.FromArgb(220, 38, 38),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRemoveStore.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnRemoveStore.FlatAppearance.BorderSize = 1;
            btnRemoveStore.Click += BtnRemoveStore_Click;

            btnClearStores = new Button
            {
                Text = "🧹 Clear Stops",
                Size = new Size(95, 28),
                Margin = new Padding(0, 0, 0, 4),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClearStores.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnClearStores.FlatAppearance.BorderSize = 1;
            btnClearStores.Click += BtnClearStores_Click;

            flowSeqBtns.Controls.Add(btnMoveStoreUp);
            flowSeqBtns.Controls.Add(btnMoveStoreDown);
            flowSeqBtns.Controls.Add(btnRemoveStore);
            flowSeqBtns.Controls.Add(btnClearStores);

            FlowLayoutPanel flowRouteActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 4, 0, 2)
            };

            btnSaveRoute = new Button
            {
                Text = "💾 Save Route & Stores",
                Size = new Size(160, 30),
                Margin = new Padding(0, 0, 6, 0),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSaveRoute.FlatAppearance.BorderSize = 0;
            btnSaveRoute.Click += BtnSaveRoute_Click;

            btnDeleteRoute = new Button
            {
                Text = "🗑️ Delete Route",
                Size = new Size(110, 30),
                Margin = new Padding(0),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDeleteRoute.FlatAppearance.BorderSize = 0;
            btnDeleteRoute.Click += BtnDeleteRoute_Click;

            flowRouteActions.Controls.Add(btnSaveRoute);
            flowRouteActions.Controls.Add(btnDeleteRoute);

            pnl.Controls.Add(flowRouteActions);
            pnl.Controls.Add(flowSeqBtns);
            pnl.Controls.Add(gridRouteStores);
            pnl.Controls.Add(tblAddStoreBar);
            pnl.Controls.Add(lblRouteStoresHeader);
            pnl.Controls.Add(chkRouteActive);
            pnl.Controls.Add(pnlRDesc);
            pnl.Controls.Add(tblRouteInputs);
            pnl.Controls.Add(pnlSelectorRow);
            pnl.Controls.Add(lblSectionTitle);

            return pnl;
        }

        private void CbCategoryType_SelectedIndexChanged(object? sender, EventArgs e)
        {
            selectedCategory = cbCategoryType.SelectedItem?.ToString() ?? "Client Company";
            bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);

            lblContactPerson.Visible = isClient;
            txtContactPerson.Visible = isClient;
            lblContactNumber.Visible = isClient;
            txtContactNumber.Visible = isClient;
            lblAddress.Visible = isClient;
            txtAddress.Visible = isClient;
            pnlClientRoutesSection.Visible = isClient;

            if (isClient)
            {
                lblVal.Text = "Client Company Name *:";
                btnAdd.Text = "+ ADD CLIENT";
                btnUpdate.Text = "UPDATE CLIENT";
                btnUpdate.Visible = true;
            }
            else
            {
                lblVal.Text = $"{selectedCategory} Name *:";
                btnAdd.Text = "+ ADD ENTRY";
                btnUpdate.Visible = false;
            }

            SetupGridColumns();
            LoadCategoryData();
        }

        private void SetupGridColumns()
        {
            gridItems.Columns.Clear();
            gridItems.AutoGenerateColumns = false;
            bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);

            if (isClient)
            {
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Company Name", FillWeight = 26, MinimumWidth = 140 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Contact Person", FillWeight = 20, MinimumWidth = 110 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Contact Phone", FillWeight = 18, MinimumWidth = 110 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Address / Branch Location", FillWeight = 24, MinimumWidth = 140 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Assigned Routes", FillWeight = 12, MinimumWidth = 90 });
            }
            else
            {
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = $"{selectedCategory} Name / Entry", FillWeight = 100, MinimumWidth = 200 });
            }
        }

        private void LoadCategoryData()
        {
            try
            {
                bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);

                if (isClient)
                {
                    allClients = MJdatabase.GetClients();
                }
                else
                {
                    allGenericItems = MJdatabase.GetLookupItems(selectedCategory);
                }

                FilterGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load categories: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadClientRoutes(string clientName, string? routeToSelect = null)
        {
            cmbClientRoutes.Items.Clear();
            currentClientRoutes.Clear();
            selectedRoute = null;

            if (string.IsNullOrWhiteSpace(clientName))
            {
                ClearRouteFields();
                return;
            }

            try
            {
                currentClientRoutes = MJdatabase.GetDeliveryRoutesByClient(clientName, activeOnly: false);
                foreach (var r in currentClientRoutes)
                {
                    cmbClientRoutes.Items.Add(r.ShortcutDisplay);
                }

                if (currentClientRoutes.Count > 0)
                {
                    int selIdx = 0;
                    if (!string.IsNullOrWhiteSpace(routeToSelect))
                    {
                        for (int i = 0; i < currentClientRoutes.Count; i++)
                        {
                            if (string.Equals(currentClientRoutes[i].RouteCode, routeToSelect, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(currentClientRoutes[i].RouteName, routeToSelect, StringComparison.OrdinalIgnoreCase))
                            {
                                selIdx = i;
                                break;
                            }
                        }
                    }
                    cmbClientRoutes.SelectedIndex = selIdx;
                }
                else
                {
                    ClearRouteFields();
                    txtRouteCode.Text = MJdatabase.GenerateNextRouteCode();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading client routes: {ex.Message}");
            }
        }

        private void CmbClientRoutes_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int idx = cmbClientRoutes.SelectedIndex;
            if (idx >= 0 && idx < currentClientRoutes.Count)
            {
                selectedRoute = currentClientRoutes[idx];
                PopulateRouteFields(selectedRoute);
            }
            else
            {
                selectedRoute = null;
                ClearRouteFields();
            }
        }

        private void PopulateRouteFields(DeliveryRoute? r)
        {
            if (r == null)
            {
                ClearRouteFields();
                return;
            }

            txtRouteCode.Text = r.RouteCode;
            txtRouteName.Text = r.RouteName;
            txtRouteDescription.Text = r.Description;
            chkRouteActive.Checked = r.IsActive;

            gridRouteStores.Rows.Clear();
            var places = r.PlacesList;
            for (int i = 0; i < places.Count; i++)
            {
                var (code, name) = DeliveryRoute.SplitStoreCodeAndName(places[i]);
                gridRouteStores.Rows.Add(i + 1, code, name);
            }
            UpdateRouteStoresCountLabel();
        }

        private void ClearRouteFields()
        {
            txtRouteCode.Clear();
            txtRouteName.Clear();
            txtRouteDescription.Clear();
            chkRouteActive.Checked = true;
            gridRouteStores.Rows.Clear();
            txtNewStoreCode.Clear();
            txtNewStoreName.Clear();
            UpdateRouteStoresCountLabel();
        }

        private void UpdateRouteStoresCountLabel()
        {
            int count = gridRouteStores.Rows.Count;
            lblRouteStoresHeader.Text = $"🏪 Route Stores & Sequence ({count} Stores — Loading Sequence):";
        }

        private void BtnNewRoute_Click(object? sender, EventArgs e)
        {
            cmbClientRoutes.SelectedIndexChanged -= CmbClientRoutes_SelectedIndexChanged;
            cmbClientRoutes.SelectedIndex = -1;
            cmbClientRoutes.SelectedIndexChanged += CmbClientRoutes_SelectedIndexChanged;

            selectedRoute = null;
            ClearRouteFields();
            txtRouteCode.Text = MJdatabase.GenerateNextRouteCode();
            txtRouteName.Focus();
        }

        private void BtnAddStoreStop_Click(object? sender, EventArgs e)
        {
            string code = txtNewStoreCode.Text.Trim();
            string name = txtNewStoreName.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a Store Name for this stop.", "Store Name Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNewStoreName.Focus();
                return;
            }

            int nextNum = gridRouteStores.Rows.Count + 1;
            gridRouteStores.Rows.Add(nextNum, code, name);
            txtNewStoreCode.Clear();
            txtNewStoreName.Clear();
            txtNewStoreCode.Focus();
            UpdateRouteStoresCountLabel();
        }

        private void BtnMoveStoreUp_Click(object? sender, EventArgs e)
        {
            if (gridRouteStores.SelectedRows.Count == 0) return;
            int idx = gridRouteStores.SelectedRows[0].Index;
            if (idx <= 0) return;

            var curRow = gridRouteStores.Rows[idx];
            string cCode = curRow.Cells[1].Value?.ToString() ?? "";
            string cName = curRow.Cells[2].Value?.ToString() ?? "";

            var prevRow = gridRouteStores.Rows[idx - 1];
            string pCode = prevRow.Cells[1].Value?.ToString() ?? "";
            string pName = prevRow.Cells[2].Value?.ToString() ?? "";

            curRow.Cells[1].Value = pCode;
            curRow.Cells[2].Value = pName;
            prevRow.Cells[1].Value = cCode;
            prevRow.Cells[2].Value = cName;

            gridRouteStores.ClearSelection();
            gridRouteStores.Rows[idx - 1].Selected = true;
        }

        private void BtnMoveStoreDown_Click(object? sender, EventArgs e)
        {
            if (gridRouteStores.SelectedRows.Count == 0) return;
            int idx = gridRouteStores.SelectedRows[0].Index;
            if (idx < 0 || idx >= gridRouteStores.Rows.Count - 1) return;

            var curRow = gridRouteStores.Rows[idx];
            string cCode = curRow.Cells[1].Value?.ToString() ?? "";
            string cName = curRow.Cells[2].Value?.ToString() ?? "";

            var nextRow = gridRouteStores.Rows[idx + 1];
            string nCode = nextRow.Cells[1].Value?.ToString() ?? "";
            string nName = nextRow.Cells[2].Value?.ToString() ?? "";

            curRow.Cells[1].Value = nCode;
            curRow.Cells[2].Value = nName;
            nextRow.Cells[1].Value = cCode;
            nextRow.Cells[2].Value = cName;

            gridRouteStores.ClearSelection();
            gridRouteStores.Rows[idx + 1].Selected = true;
        }

        private void BtnRemoveStore_Click(object? sender, EventArgs e)
        {
            if (gridRouteStores.SelectedRows.Count == 0) return;
            int idx = gridRouteStores.SelectedRows[0].Index;
            gridRouteStores.Rows.RemoveAt(idx);

            for (int i = 0; i < gridRouteStores.Rows.Count; i++)
            {
                gridRouteStores.Rows[i].Cells[0].Value = i + 1;
            }
            UpdateRouteStoresCountLabel();
        }

        private void BtnClearStores_Click(object? sender, EventArgs e)
        {
            if (gridRouteStores.Rows.Count == 0) return;
            var confirm = MessageBox.Show("Clear all stores from this delivery route?", "Confirm Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                gridRouteStores.Rows.Clear();
                UpdateRouteStoresCountLabel();
            }
        }

        private void BtnSaveRoute_Click(object? sender, EventArgs e)
        {
            string clientName = txtItemValue.Text.Trim();
            if (string.IsNullOrEmpty(clientName))
            {
                MessageBox.Show("Please specify a Client Company Name first.", "Client Company Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtItemValue.Focus();
                return;
            }

            string code = txtRouteCode.Text.Trim();
            string name = txtRouteName.Text.Trim();

            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("Please enter a Route Code (e.g. R1, R2).", "Route Code Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRouteCode.Focus();
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a Route Name.", "Route Name Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRouteName.Focus();
                return;
            }

            List<string> placesList = new List<string>();
            foreach (DataGridViewRow row in gridRouteStores.Rows)
            {
                string sc = row.Cells[1].Value?.ToString()?.Trim() ?? "";
                string sn = row.Cells[2].Value?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(sn))
                {
                    placesList.Add(string.IsNullOrWhiteSpace(sc) ? sn : $"{sc} - {sn}");
                }
            }

            DeliveryRoute routeToSave = selectedRoute ?? new DeliveryRoute();
            routeToSave.ClientCompanyName = clientName;
            routeToSave.RouteCode = code;
            routeToSave.RouteName = name;
            routeToSave.Description = txtRouteDescription.Text.Trim();
            routeToSave.ConnectedPlaces = string.Join("; ", placesList);
            routeToSave.IsActive = chkRouteActive.Checked;

            bool success = MJdatabase.SaveDeliveryRoute(routeToSave);
            if (success)
            {
                MJdatabase.NotifyCategoriesChanged();
                LoadClientRoutes(clientName, code);
                FilterGrid();
                MessageBox.Show($"Route '{code} - {name}' with {placesList.Count} store stop(s) saved successfully!", "Route Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to save delivery route to database.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteRoute_Click(object? sender, EventArgs e)
        {
            if (selectedRoute == null || selectedRoute.Id <= 0)
            {
                MessageBox.Show("Please select an existing route to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Are you sure you want to delete delivery route '{selectedRoute.RouteCode} - {selectedRoute.RouteName}'?", "Confirm Delete Route", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                MJdatabase.DeleteDeliveryRoute(selectedRoute.Id);
                MJdatabase.NotifyCategoriesChanged();
                LoadClientRoutes(txtItemValue.Text.Trim());
                FilterGrid();
                MessageBox.Show("Route deleted successfully.", "Route Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void FilterGrid()
        {
            string q = txtGridSearch.Text.Trim().ToLowerInvariant();
            bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);

            gridItems.SelectionChanged -= GridItems_SelectionChanged;
            gridItems.Rows.Clear();

            if (isClient)
            {
                var list = string.IsNullOrEmpty(q)
                    ? allClients
                    : allClients.Where(c =>
                        (!string.IsNullOrEmpty(c.CompanyName) && c.CompanyName.ToLower().Contains(q)) ||
                        (!string.IsNullOrEmpty(c.ContactPerson) && c.ContactPerson.ToLower().Contains(q)) ||
                        (!string.IsNullOrEmpty(c.ContactNumber) && c.ContactNumber.ToLower().Contains(q)) ||
                        (!string.IsNullOrEmpty(c.Address) && c.Address.ToLower().Contains(q))
                    ).ToList();

                foreach (var c in list)
                {
                    int routeCount = MJdatabase.GetDeliveryRoutesByClient(c.CompanyName, activeOnly: false).Count;
                    string routeLabel = routeCount == 1 ? "1 route" : $"{routeCount} routes";
                    gridItems.Rows.Add(c.CompanyName, c.ContactPerson, c.ContactNumber, c.Address, routeLabel);
                }
                lblSummary.Text = $"📊 Registered Client Companies: {list.Count} of {allClients.Count}";
            }
            else
            {
                var list = string.IsNullOrEmpty(q)
                    ? allGenericItems
                    : allGenericItems.Where(i => i.ToLower().Contains(q)).ToList();

                foreach (var item in list)
                {
                    gridItems.Rows.Add(item);
                }
                lblSummary.Text = $"📊 {selectedCategory} Entries: {list.Count} of {allGenericItems.Count}";
            }

            if (gridItems.Rows.Count > 0)
            {
                gridItems.ClearSelection();
                gridItems.Rows[0].Selected = true;
                PopulateInputsFromRow(gridItems.Rows[0]);
            }
            else
            {
                ClearInputFields();
            }

            gridItems.SelectionChanged += GridItems_SelectionChanged;
        }

        private void GridItems_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridItems.SelectedRows.Count > 0 && gridItems.SelectedRows[0].Cells[0].Value != null)
            {
                PopulateInputsFromRow(gridItems.SelectedRows[0]);
            }
        }

        private void PopulateInputsFromRow(DataGridViewRow row)
        {
            string val = row.Cells[0].Value?.ToString() ?? "";
            txtItemValue.Text = val;
            bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);

            if (isClient)
            {
                txtContactPerson.Text = row.Cells.Count > 1 ? row.Cells[1].Value?.ToString() ?? "" : "";
                txtContactNumber.Text = row.Cells.Count > 2 ? row.Cells[2].Value?.ToString() ?? "" : "";
                txtAddress.Text = row.Cells.Count > 3 ? row.Cells[3].Value?.ToString() ?? "" : "";
                LoadClientRoutes(val);
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            try
            {
                string val = txtItemValue.Text.Trim();
                if (string.IsNullOrEmpty(val))
                {
                    MessageBox.Show("Please enter a name for this category entry.", "Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtItemValue.Focus();
                    return;
                }

                bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);

                if (isClient)
                {
                    string contactNumber = txtContactNumber.Text.Trim();
                    if (!string.IsNullOrEmpty(contactNumber) && !SecurityHelper.IsValidPhone(contactNumber))
                    {
                        MessageBox.Show("Please enter a valid Philippine contact number (e.g. 09123456789 or +639123456789).", "Invalid Contact Number", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtContactNumber.Focus();
                        return;
                    }
                    ClientCompany client = new ClientCompany
                    {
                        CompanyName = val,
                        ContactPerson = txtContactPerson.Text.Trim(),
                        ContactNumber = contactNumber,
                        Address = txtAddress.Text.Trim()
                    };
                    MJdatabase.SaveClient(client);

                    string rCode = txtRouteCode.Text.Trim();
                    string rName = txtRouteName.Text.Trim();
                    if (!string.IsNullOrEmpty(rCode) && !string.IsNullOrEmpty(rName))
                    {
                        List<string> placesList = new List<string>();
                        foreach (DataGridViewRow row in gridRouteStores.Rows)
                        {
                            string sc = row.Cells[1].Value?.ToString()?.Trim() ?? "";
                            string sn = row.Cells[2].Value?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(sn))
                            {
                                placesList.Add(string.IsNullOrWhiteSpace(sc) ? sn : $"{sc} - {sn}");
                            }
                        }
                        DeliveryRoute newR = new DeliveryRoute
                        {
                            ClientCompanyName = val,
                            RouteCode = rCode,
                            RouteName = rName,
                            Description = txtRouteDescription.Text.Trim(),
                            ConnectedPlaces = string.Join("; ", placesList),
                            IsActive = chkRouteActive.Checked
                        };
                        MJdatabase.SaveDeliveryRoute(newR);
                    }

                    MessageBox.Show($"Client Company '{val}' saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MJdatabase.SaveLookupItem(selectedCategory, val);
                    MessageBox.Show($"'{val}' added to {selectedCategory} successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                MJdatabase.NotifyCategoriesChanged();
                LoadCategoryData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add category item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                string val = txtItemValue.Text.Trim();
                if (string.IsNullOrEmpty(val))
                {
                    MessageBox.Show("Please enter or select a client company to update.", "Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtItemValue.Focus();
                    return;
                }

                bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);
                if (isClient)
                {
                    string contactNumber = txtContactNumber.Text.Trim();
                    if (!string.IsNullOrEmpty(contactNumber) && !SecurityHelper.IsValidPhone(contactNumber))
                    {
                        MessageBox.Show("Please enter a valid Philippine contact number (e.g. 09123456789 or +639123456789).", "Invalid Contact Number", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtContactNumber.Focus();
                        return;
                    }
                    ClientCompany client = new ClientCompany
                    {
                        CompanyName = val,
                        ContactPerson = txtContactPerson.Text.Trim(),
                        ContactNumber = contactNumber,
                        Address = txtAddress.Text.Trim()
                    };
                    MJdatabase.SaveClient(client);

                    string rCode = txtRouteCode.Text.Trim();
                    string rName = txtRouteName.Text.Trim();
                    if (!string.IsNullOrEmpty(rCode) && !string.IsNullOrEmpty(rName))
                    {
                        List<string> placesList = new List<string>();
                        foreach (DataGridViewRow row in gridRouteStores.Rows)
                        {
                            string sc = row.Cells[1].Value?.ToString()?.Trim() ?? "";
                            string sn = row.Cells[2].Value?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(sn))
                            {
                                placesList.Add(string.IsNullOrWhiteSpace(sc) ? sn : $"{sc} - {sn}");
                            }
                        }
                        DeliveryRoute rToSave = selectedRoute ?? new DeliveryRoute();
                        rToSave.ClientCompanyName = val;
                        rToSave.RouteCode = rCode;
                        rToSave.RouteName = rName;
                        rToSave.Description = txtRouteDescription.Text.Trim();
                        rToSave.ConnectedPlaces = string.Join("; ", placesList);
                        rToSave.IsActive = chkRouteActive.Checked;
                        MJdatabase.SaveDeliveryRoute(rToSave);
                    }

                    MessageBox.Show($"Client Company '{val}' updated successfully!", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    MJdatabase.NotifyCategoriesChanged();
                    LoadCategoryData();
                }
                else
                {
                    BtnAdd_Click(sender, e);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update client company: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                string val = txtItemValue.Text.Trim();
                if (string.IsNullOrEmpty(val))
                {
                    MessageBox.Show("Please select an entry to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirm = MessageBox.Show($"Are you sure you want to delete '{val}' from {selectedCategory}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);
                    if (isClient)
                    {
                        MJdatabase.DeleteClient(val);
                    }
                    else
                    {
                        MJdatabase.DeleteLookupItem(selectedCategory, val);
                    }
                    MJdatabase.NotifyCategoriesChanged();
                    MessageBox.Show($"'{val}' removed successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadCategoryData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearInputFields()
        {
            txtItemValue.Clear();
            txtContactPerson.Clear();
            txtContactNumber.Clear();
            txtAddress.Clear();
            cmbClientRoutes.Items.Clear();
            currentClientRoutes.Clear();
            selectedRoute = null;
            ClearRouteFields();
        }
    }
}
"""

with open(r'Views\CategoryManagementView.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print("SUCCESS: CategoryManagementView.cs successfully written!")
