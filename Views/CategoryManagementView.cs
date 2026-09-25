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
    public class CategoryManagementView : UserControl, IRefreshable
    {
        private ComboBox cbCategoryType = null!;
        private Label lblVal = null!;
        private TextBox txtItemValue = null!;
        private Label lblContactPerson = null!;
        private TextBox txtContactPerson = null!;
        private Label lblContactNumber = null!;
        private TextBox txtContactNumber = null!;
        private Label lblCapacity = null!;
        private NumericUpDown numCapacity = null!;
        private Label lblAddress = null!;
        private TextBox txtAddress = null!;
        private Button btnAdd = null!;
        private Button btnUpdate = null!;
        private Button btnDelete = null!;
        private Button btnClear = null!;
        private TextBox txtGridSearch = null!;
        private Button btnClearSearch = null!;
        private Label lblSummary = null!;
        private DataGridView gridItems = null!;
        private GroupBox grpForm = null!;
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;

        private string selectedCategory = "Client Company";
        private List<ClientCompany> allClients = new List<ClientCompany>();
        private List<(string ItemValue, string ExtraValue)> allTruckTypes = new List<(string ItemValue, string ExtraValue)>();
        private List<string> allGenericItems = new List<string>();

        public CategoryManagementView()
        {
            InitializeComponent();
            SetupGridColumns();
            LoadCategoryData();
            gridItems.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && gridItems.Rows[e.RowIndex].DataBoundItem is object item) RecordDetailsDialog.ShowDetails(this, item); };
            SetFormVisibility(false);
            UIThemeHelper.ApplyTheme(this);
        }

        private void SetFormVisibility(bool visible)
        {
            grpForm.Visible = visible;
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = visible ? "📝 Hide Form ▴" : "📝 Input Form ▾";
                btnToggleForm.BackColor = visible ? Color.FromArgb(226, 232, 240) : Color.FromArgb(30, 41, 59);
                btnToggleForm.ForeColor = visible ? Color.FromArgb(15, 23, 42) : Color.White;
            }
            MainForm.Instance?.NotifyDrawerToggled(visible);
        }

        public void RefreshData()
        {
            LoadCategoryData();
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
                Padding = new Padding(24, 12, 24, 10),
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
                Text = "🏢 Dropdown Categories & Client Profile Directory",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            Label lblHeaderSub = new Label
            {
                Text = "Manage client company contacts, delivery destinations, truck vehicle classifications, and crew operational roles.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Margin = new Padding(0)
            };

            tblHeaderFlow.Controls.Add(lblHeaderTitle, 0, 0);
            tblHeaderFlow.Controls.Add(lblHeaderSub, 0, 1);
            pnlHeader.Controls.Add(tblHeaderFlow);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16)
            };

            grpForm = new GroupBox
            {
                Text = "Category Configuration & Input Form",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = Color.White
            };

            TableLayoutPanel tblFormRows = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 4
            };
            tblFormRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblFormRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tblFormRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tblFormRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tblFormRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            TableLayoutPanel tblRow1 = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 8)
            };
            tblRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tblRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

            Panel pnlCat = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 0, 8, 0) };
            Label lblCat = new Label { Text = "Dropdown Category:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F) };
            cbCategoryType = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cbCategoryType.Items.AddRange(new object[] { "Client Company", "Destination Route", "Truck Type / Category", "Crew Role", "Payment Method" });
            cbCategoryType.SelectedIndex = 0;
            cbCategoryType.SelectedIndexChanged += CbCategoryType_SelectedIndexChanged;
            pnlCat.Controls.Add(cbCategoryType);
            pnlCat.Controls.Add(lblCat);

            Panel pnlVal = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(8, 0, 0, 0) };
            lblVal = new Label { Text = "Client Company Name:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F) };
            txtItemValue = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), MaxLength = 150 };
            pnlVal.Controls.Add(txtItemValue);
            pnlVal.Controls.Add(lblVal);

            tblRow1.Controls.Add(pnlCat, 0, 0);
            tblRow1.Controls.Add(pnlVal, 1, 0);

            TableLayoutPanel tblRow2 = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                Margin = new Padding(0, 0, 0, 8)
            };
            tblRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tblRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tblRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

            Panel pnlCP = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 0, 6, 0) };
            lblContactPerson = new Label { Text = "Contact Person Name:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F) };
            txtContactPerson = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), MaxLength = 100 };
            pnlCP.Controls.Add(txtContactPerson);
            pnlCP.Controls.Add(lblContactPerson);

            Panel pnlCN = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(6, 0, 6, 0) };
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

            Panel pnlCap = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(6, 0, 0, 0) };
            lblCapacity = new Label { Text = "Crate Capacity:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F), Visible = false };
            numCapacity = new NumericUpDown
            {
                Dock = DockStyle.Top,
                Maximum = 1000000,
                Minimum = 1,
                Value = 300,
                Visible = false,
                Font = new Font("Segoe UI", 9F)
            };
            pnlCap.Controls.Add(numCapacity);
            pnlCap.Controls.Add(lblCapacity);

            tblRow2.Controls.Add(pnlCP, 0, 0);
            tblRow2.Controls.Add(pnlCN, 1, 0);
            tblRow2.Controls.Add(pnlCap, 2, 0);

            Panel pnlAddr = new Panel { Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
            lblAddress = new Label { Text = "Address / Branch Location:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F) };
            txtAddress = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), MaxLength = 250 };
            pnlAddr.Controls.Add(txtAddress);
            pnlAddr.Controls.Add(lblAddress);

            FlowLayoutPanel flowButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0)
            };

            btnAdd = new Button
            {
                Text = "+ ADD NEW",
                Size = new Size(100, 32),
                Margin = new Padding(0, 0, 8, 0),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += BtnAdd_Click;

            btnUpdate = new Button
            {
                Text = "UPDATE",
                Size = new Size(90, 32),
                Margin = new Padding(0, 0, 8, 0),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.Click += BtnUpdate_Click;

            btnDelete = new Button
            {
                Text = "DELETE",
                Size = new Size(90, 32),
                Margin = new Padding(0, 0, 8, 0),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += BtnDelete_Click;

            btnClear = new Button
            {
                Text = "CLEAR",
                Size = new Size(85, 32),
                Margin = new Padding(0),
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => ClearInputFields();

            btnCloseForm = new Button
            {
                Text = "✖ HIDE FORM",
                Size = new Size(110, 32),
                Margin = new Padding(8, 0, 0, 0),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCloseForm.FlatAppearance.BorderSize = 0;
            btnCloseForm.Click += (s, e) => SetFormVisibility(false);

            flowButtons.Controls.Add(btnAdd);
            flowButtons.Controls.Add(btnUpdate);
            flowButtons.Controls.Add(btnDelete);
            flowButtons.Controls.Add(btnClear);
            flowButtons.Controls.Add(btnCloseForm);

            tblFormRows.Controls.Add(tblRow1, 0, 0);
            tblFormRows.Controls.Add(tblRow2, 0, 1);
            tblFormRows.Controls.Add(pnlAddr, 0, 2);
            tblFormRows.Controls.Add(flowButtons, 0, 3);
            grpForm.Controls.Add(tblFormRows);

            Panel pnlGridToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                Padding = new Padding(0, 8, 0, 6)
            };

            lblSummary = new Label
            {
                Text = "📊 Total Entries: 0",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Padding = new Padding(12, 6, 0, 0)
            };

            btnToggleForm = new Button
            {
                Text = "📝 Input Form ▾",
                Size = new Size(130, 30),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left
            };
            btnToggleForm.FlatAppearance.BorderSize = 0;
            btnToggleForm.Click += (s, e) => SetFormVisibility(!grpForm.Visible);

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
                Font = new Font("Segoe UI Semibold", 9F),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize = true,
                Margin = new Padding(0, 4, 6, 0)
            };

            txtGridSearch = new TextBox
            {
                Width = 220,
                MaxLength = 100,
                PlaceholderText = "Search name, contact, address...",
                Margin = new Padding(0, 1, 4, 0)
            };
            txtGridSearch.TextChanged += (s, e) => FilterGrid();

            btnClearSearch = new Button
            {
                Text = "✕",
                Size = new Size(28, 25),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
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
                ColumnHeadersHeight = 40,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                RowTemplate = { Height = 34 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                ShowCellToolTips = true,
                ScrollBars = ScrollBars.Both,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            gridItems.SelectionChanged += GridItems_SelectionChanged;

            pnlMain.Controls.Add(gridItems);
            pnlMain.Controls.Add(pnlGridToolbar);
            pnlMain.Controls.Add(grpForm);
            gridItems.BringToFront();

            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlMain);
            pnlMain.BringToFront();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void CbCategoryType_SelectedIndexChanged(object? sender, EventArgs e)
        {
            selectedCategory = cbCategoryType.SelectedItem?.ToString() ?? "Client Company";
            bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);
            bool isTruckType = selectedCategory.Contains("Truck Type", StringComparison.OrdinalIgnoreCase);

            lblContactPerson.Visible = isClient;
            txtContactPerson.Visible = isClient;
            lblContactNumber.Visible = isClient;
            txtContactNumber.Visible = isClient;
            lblAddress.Visible = isClient;
            txtAddress.Visible = isClient;

            lblCapacity.Visible = isTruckType;
            numCapacity.Visible = isTruckType;

            if (isClient)
            {
                lblVal.Text = "Client Company Name:";
                btnUpdate.Visible = true;
            }
            else if (isTruckType)
            {
                lblVal.Text = "Truck Category Name:";
                btnUpdate.Visible = false;
            }
            else
            {
                lblVal.Text = "Category / Item Name:";
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
            bool isTruckType = selectedCategory.Contains("Truck Type", StringComparison.OrdinalIgnoreCase);

            if (isClient)
            {
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Company Name", FillWeight = 28, MinimumWidth = 150 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Contact Person", FillWeight = 22, MinimumWidth = 120 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Contact Phone / Mobile #", FillWeight = 20, MinimumWidth = 120 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Address / Notes", FillWeight = 30, MinimumWidth = 160 });
            }
            else if (isTruckType)
            {
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Truck Category / Classification", FillWeight = 65, MinimumWidth = 180 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Crate Capacity", FillWeight = 35, MinimumWidth = 120 });
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
                bool isTruckType = selectedCategory.Contains("Truck Type", StringComparison.OrdinalIgnoreCase);

                if (isClient)
                {
                    allClients = MJdatabase.GetClients();
                }
                else if (isTruckType)
                {
                    allTruckTypes = MJdatabase.GetLookupItemsWithExtra(selectedCategory);
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

        private void FilterGrid()
        {
            string q = txtGridSearch.Text.Trim().ToLowerInvariant();
            bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);
            bool isTruckType = selectedCategory.Contains("Truck Type", StringComparison.OrdinalIgnoreCase);

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
                    gridItems.Rows.Add(c.CompanyName, c.ContactPerson, c.ContactNumber, c.Address);
                }
                lblSummary.Text = $"📊 Registered Client Companies: {list.Count} of {allClients.Count}";
            }
            else if (isTruckType)
            {
                var list = string.IsNullOrEmpty(q)
                    ? allTruckTypes
                    : allTruckTypes.Where(t =>
                        (!string.IsNullOrEmpty(t.ItemValue) && t.ItemValue.ToLower().Contains(q)) ||
                        (!string.IsNullOrEmpty(t.ExtraValue) && t.ExtraValue.ToLower().Contains(q))
                    ).ToList();

                foreach (var item in list)
                {
                    string capStr = string.IsNullOrEmpty(item.ExtraValue) ? "300" : item.ExtraValue;
                    gridItems.Rows.Add(item.ItemValue, $"{capStr} crates");
                }
                lblSummary.Text = $"📊 Truck Classifications: {list.Count} of {allTruckTypes.Count}";
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
            txtItemValue.Text = row.Cells[0].Value?.ToString() ?? "";
            bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);
            bool isTruckType = selectedCategory.Contains("Truck Type", StringComparison.OrdinalIgnoreCase);

            if (isClient)
            {
                txtContactPerson.Text = row.Cells.Count > 1 ? row.Cells[1].Value?.ToString() ?? "" : "";
                txtContactNumber.Text = row.Cells.Count > 2 ? row.Cells[2].Value?.ToString() ?? "" : "";
                txtAddress.Text = row.Cells.Count > 3 ? row.Cells[3].Value?.ToString() ?? "" : "";
            }
            else if (isTruckType && row.Cells.Count > 1 && row.Cells[1].Value != null)
            {
                string capVal = row.Cells[1].Value?.ToString() ?? "";
                string cleanDigits = System.Text.RegularExpressions.Regex.Replace(capVal, @"[^\d]", "");
                if (int.TryParse(cleanDigits, out int parsedCap) && parsedCap >= numCapacity.Minimum && parsedCap <= numCapacity.Maximum)
                {
                    numCapacity.Value = parsedCap;
                }
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
                bool isTruckType = selectedCategory.Contains("Truck Type", StringComparison.OrdinalIgnoreCase);

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
                    MessageBox.Show($"Client Company '{val}' with contact profile saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (isTruckType)
                {
                    string cap = numCapacity.Value.ToString();
                    MJdatabase.SaveLookupItemWithExtra(selectedCategory, val, cap);
                    MessageBox.Show($"Truck Category '{val}' with capacity of {cap} crates saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    MessageBox.Show($"Client Company '{val}' contact details updated successfully!", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (numCapacity.Maximum < 1000000) numCapacity.Maximum = 1000000;
            if (numCapacity.Minimum < 1) numCapacity.Minimum = 1;
            numCapacity.Value = 300;
        }
    }
}
