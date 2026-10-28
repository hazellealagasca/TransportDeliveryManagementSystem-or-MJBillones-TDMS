import os
import re

path = os.path.join("Views", "TruckManagementView.cs")
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

# Add fields for subtabs and category view
field_insert_marker = "public partial class TruckManagementView : UserControl, IRefreshable\n    {"
field_insert_marker_crlf = "public partial class TruckManagementView : UserControl, IRefreshable\r\n    {"

subtab_fields = """
        private Button btnSubTabFleet = null!;
        private Button btnSubTabCategories = null!;
        private Panel pnlSubNavTabs = null!;
        private Panel pnlCategoriesContainer = null!;
        private DataGridView gridTruckCategories = null!;
        private TextBox txtCatName = null!;
        private NumericUpDown numCatCapacity = null!;
        private Button btnAddCat = null!;
        private Button btnUpdateCat = null!;
        private Button btnDeleteCat = null!;
        private Button btnClearCat = null!;
        private TextBox txtCatSearch = null!;
        private Label lblCatSummary = null!;
        private List<TruckCategoryItem> allTruckCategories = new List<TruckCategoryItem>();
        private TruckCategoryItem? selectedTruckCategory = null;
"""

if field_insert_marker in text:
    text = text.replace(field_insert_marker, field_insert_marker + subtab_fields, 1)
elif field_insert_marker_crlf in text:
    text = text.replace(field_insert_marker_crlf, field_insert_marker_crlf + subtab_fields.replace("\n", "\r\n"), 1)
else:
    raise Exception("field_insert_marker not found")

# In constructor, after InitializeComponent(); add InitializeSubNavAndCategories();
init_marker = "InitializeComponent();"
assert init_marker in text, "init_marker not found"
text = text.replace(init_marker, init_marker + "\n            InitializeSubNavAndCategories();", 1)

# In LoadTrucks, replace GetLookupItems with GetTruckCategories
old_load_types = 'var types = MJdatabase.GetLookupItems("Truck Type / Category");\n                string currentType = cbType.Text;\n                cbType.Items.Clear();\n                foreach (var t in types) cbType.Items.Add(t);'
old_load_types_crlf = old_load_types.replace("\n", "\r\n")

new_load_types = 'var categories = MJdatabase.GetTruckCategories();\n                string currentType = cbType.Text;\n                cbType.Items.Clear();\n                foreach (var c in categories) cbType.Items.Add(c.CategoryName);'
if old_load_types in text:
    text = text.replace(old_load_types, new_load_types, 1)
elif old_load_types_crlf in text:
    text = text.replace(old_load_types_crlf, new_load_types.replace("\n", "\r\n"), 1)
else:
    raise Exception("old_load_types not found")

# Replace RefreshData
old_refresh = "public void RefreshData()\n        {\n            LoadTrucks();\n        }"
old_refresh_crlf = old_refresh.replace("\n", "\r\n")
new_refresh = "public void RefreshData()\n        {\n            LoadTrucks();\n            LoadTruckCategories();\n        }"
if old_refresh in text:
    text = text.replace(old_refresh, new_refresh, 1)
elif old_refresh_crlf in text:
    text = text.replace(old_refresh_crlf, new_refresh.replace("\n", "\r\n"), 1)

# Add category helper methods before the closing brace of TruckManagementView class
category_methods = """
        private void InitializeSubNavAndCategories()
        {
            pnlSubNavTabs = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16, 6, 16, 6)
            };

            FlowLayoutPanel flowTabs = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            btnSubTabFleet = new Button
            {
                Text = "🚚  Registered Trucks",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                MinimumSize = new Size(160, 32),
                Padding = new Padding(14, 0, 14, 0),
                Margin = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnSubTabFleet.FlatAppearance.BorderSize = 1;
            btnSubTabFleet.FlatAppearance.BorderColor = Color.FromArgb(15, 23, 42);
            btnSubTabFleet.Click += (s, e) => SwitchSubTab("Fleet");

            btnSubTabCategories = new Button
            {
                Text = "🏷️  Truck Categories & Capacities",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                MinimumSize = new Size(190, 32),
                Padding = new Padding(14, 0, 14, 0),
                Margin = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                Cursor = Cursors.Hand
            };
            btnSubTabCategories.FlatAppearance.BorderSize = 1;
            btnSubTabCategories.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnSubTabCategories.Click += (s, e) => SwitchSubTab("Categories");

            flowTabs.Controls.Add(btnSubTabFleet);
            flowTabs.Controls.Add(btnSubTabCategories);
            pnlSubNavTabs.Controls.Add(flowTabs);

            pnlCategoriesContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(16)
            };

            TableLayoutPanel tblCatLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            tblCatLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380F));
            tblCatLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblCatLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Panel pnlCatForm = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(16)
            };

            Label lblCatHeader = new Label
            {
                Text = "Truck Category & Capacity Details",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 30
            };

            Label lblCatName = new Label
            {
                Text = "Truck Category / Classification Name:",
                Font = new Font("Segoe UI Semibold", 8.5F),
                Dock = DockStyle.Top,
                Height = 22
            };

            txtCatName = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9F),
                MaxLength = 100
            };

            Panel pnlSpacer1 = new Panel { Dock = DockStyle.Top, Height = 10 };

            Label lblCatCap = new Label
            {
                Text = "Default Crate Capacity (crates):",
                Font = new Font("Segoe UI Semibold", 8.5F),
                Dock = DockStyle.Top,
                Height = 22
            };

            numCatCapacity = new NumericUpDown
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9F),
                Minimum = 1,
                Maximum = 1000000,
                Value = 300
            };

            Panel pnlSpacer2 = new Panel { Dock = DockStyle.Top, Height = 16 };

            FlowLayoutPanel flowCatBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            btnAddCat = new Button
            {
                Text = "+ Add Category",
                Size = new Size(110, 32),
                Margin = new Padding(0, 0, 6, 6),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddCat.FlatAppearance.BorderSize = 0;
            btnAddCat.Click += BtnAddCat_Click;

            btnUpdateCat = new Button
            {
                Text = "Update",
                Size = new Size(80, 32),
                Margin = new Padding(0, 0, 6, 6),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnUpdateCat.FlatAppearance.BorderSize = 0;
            btnUpdateCat.Click += BtnUpdateCat_Click;

            btnDeleteCat = new Button
            {
                Text = "Delete",
                Size = new Size(80, 32),
                Margin = new Padding(0, 0, 6, 6),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDeleteCat.FlatAppearance.BorderSize = 0;
            btnDeleteCat.Click += BtnDeleteCat_Click;

            btnClearCat = new Button
            {
                Text = "Clear",
                Size = new Size(70, 32),
                Margin = new Padding(0, 0, 0, 6),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClearCat.FlatAppearance.BorderSize = 1;
            btnClearCat.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnClearCat.Click += (s, e) => ClearCatInputs();

            flowCatBtns.Controls.Add(btnAddCat);
            flowCatBtns.Controls.Add(btnUpdateCat);
            flowCatBtns.Controls.Add(btnDeleteCat);
            flowCatBtns.Controls.Add(btnClearCat);

            pnlCatForm.Controls.Add(flowCatBtns);
            pnlCatForm.Controls.Add(pnlSpacer2);
            pnlCatForm.Controls.Add(numCatCapacity);
            pnlCatForm.Controls.Add(lblCatCap);
            pnlCatForm.Controls.Add(pnlSpacer1);
            pnlCatForm.Controls.Add(txtCatName);
            pnlCatForm.Controls.Add(lblCatName);
            pnlCatForm.Controls.Add(lblCatHeader);

            Panel pnlCatGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            Panel pnlCatToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(12, 6, 12, 6),
                BackColor = Color.White
            };

            lblCatSummary = new Label
            {
                Text = "📊 Total Truck Classifications: 0",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };

            Panel pnlSearchWrap = new Panel { Dock = DockStyle.Right, Width = 220 };
            txtCatSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Search categories..."
            };
            txtCatSearch.TextChanged += (s, e) => FilterTruckCategories();
            pnlSearchWrap.Controls.Add(txtCatSearch);

            pnlCatToolbar.Controls.Add(pnlSearchWrap);
            pnlCatToolbar.Controls.Add(lblCatSummary);

            gridTruckCategories = new DataGridView
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            gridTruckCategories.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Category / Model Classification", FillWeight = 50, MinimumWidth = 160 });
            gridTruckCategories.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Default Crate Capacity", FillWeight = 25, MinimumWidth = 120 });
            gridTruckCategories.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Active Fleet Assigned", FillWeight = 25, MinimumWidth = 120 });
            gridTruckCategories.SelectionChanged += GridTruckCategories_SelectionChanged;

            pnlCatGridCard.Controls.Add(gridTruckCategories);
            pnlCatGridCard.Controls.Add(pnlCatToolbar);

            tblCatLayout.Controls.Add(pnlCatForm, 0, 0);
            tblCatLayout.Controls.Add(pnlCatGridCard, 1, 0);
            pnlCategoriesContainer.Controls.Add(tblCatLayout);

            this.Controls.Add(pnlCategoriesContainer);
            this.Controls.Add(pnlSubNavTabs);

            pnlSubNavTabs.BringToFront();
            tblMasterContent.BringToFront();
        }

        private void SwitchSubTab(string tab)
        {
            if (tab == "Fleet")
            {
                btnSubTabFleet.BackColor = Color.FromArgb(15, 23, 42);
                btnSubTabFleet.ForeColor = Color.White;
                btnSubTabFleet.FlatAppearance.BorderColor = Color.FromArgb(15, 23, 42);

                btnSubTabCategories.BackColor = Color.White;
                btnSubTabCategories.ForeColor = Color.FromArgb(71, 85, 105);
                btnSubTabCategories.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);

                pnlCategoriesContainer.Visible = false;
                tblMasterContent.Visible = true;
                tblMasterContent.BringToFront();
            }
            else
            {
                btnSubTabCategories.BackColor = Color.FromArgb(15, 23, 42);
                btnSubTabCategories.ForeColor = Color.White;
                btnSubTabCategories.FlatAppearance.BorderColor = Color.FromArgb(15, 23, 42);

                btnSubTabFleet.BackColor = Color.White;
                btnSubTabFleet.ForeColor = Color.FromArgb(71, 85, 105);
                btnSubTabFleet.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);

                tblMasterContent.Visible = false;
                pnlCategoriesContainer.Visible = true;
                pnlCategoriesContainer.BringToFront();
                LoadTruckCategories();
            }
        }

        private void LoadTruckCategories()
        {
            try
            {
                allTruckCategories = MJdatabase.GetTruckCategories();
                FilterTruckCategories();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load truck categories: {ex.Message}");
            }
        }

        private void FilterTruckCategories()
        {
            gridTruckCategories.SelectionChanged -= GridTruckCategories_SelectionChanged;
            gridTruckCategories.Rows.Clear();

            string q = txtCatSearch.Text.Trim().ToLowerInvariant();
            var list = string.IsNullOrEmpty(q)
                ? allTruckCategories
                : allTruckCategories.Where(c => c.CategoryName.ToLower().Contains(q)).ToList();

            foreach (var c in list)
            {
                gridTruckCategories.Rows.Add(c.CategoryName, $"{c.CrateCapacity} crates", $"{c.AssignedTrucksCount} trucks");
            }
            lblCatSummary.Text = $"📊 Total Truck Classifications: {list.Count} of {allTruckCategories.Count}";

            if (gridTruckCategories.Rows.Count > 0)
            {
                gridTruckCategories.ClearSelection();
                gridTruckCategories.Rows[0].Selected = true;
                PopulateCategoryFromRow(gridTruckCategories.Rows[0]);
            }
            else
            {
                ClearCatInputs();
            }

            gridTruckCategories.SelectionChanged += GridTruckCategories_SelectionChanged;
        }

        private void GridTruckCategories_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridTruckCategories.SelectedRows.Count > 0)
            {
                PopulateCategoryFromRow(gridTruckCategories.SelectedRows[0]);
            }
        }

        private void PopulateCategoryFromRow(DataGridViewRow row)
        {
            string catName = row.Cells[0].Value?.ToString() ?? "";
            selectedTruckCategory = allTruckCategories.FirstOrDefault(c => string.Equals(c.CategoryName, catName, StringComparison.OrdinalIgnoreCase));
            if (selectedTruckCategory != null)
            {
                txtCatName.Text = selectedTruckCategory.CategoryName;
                numCatCapacity.Value = selectedTruckCategory.CrateCapacity;
            }
        }

        private void BtnAddCat_Click(object? sender, EventArgs e)
        {
            string name = txtCatName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a category name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCatName.Focus();
                return;
            }

            int cap = (int)numCatCapacity.Value;
            bool success = MJdatabase.SaveTruckCategory(name, cap);
            if (success)
            {
                MJdatabase.NotifyCategoriesChanged();
                LoadTruckCategories();
                LoadTrucks();
                MessageBox.Show($"Truck category '{name}' with {cap} crate capacity saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to save truck category.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdateCat_Click(object? sender, EventArgs e)
        {
            if (selectedTruckCategory == null)
            {
                MessageBox.Show("Please select a category to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string name = txtCatName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a category name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCatName.Focus();
                return;
            }

            int cap = (int)numCatCapacity.Value;
            string oldName = selectedTruckCategory.CategoryName;
            bool success = MJdatabase.SaveTruckCategory(name, cap, oldName);
            if (success)
            {
                MJdatabase.NotifyCategoriesChanged();
                LoadTruckCategories();
                LoadTrucks();
                MessageBox.Show($"Truck category updated successfully!", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to update truck category.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteCat_Click(object? sender, EventArgs e)
        {
            if (selectedTruckCategory == null)
            {
                MessageBox.Show("Please select a category to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string name = selectedTruckCategory.CategoryName;
            var confirm = MessageBox.Show($"Are you sure you want to delete category '{name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                MJdatabase.DeleteTruckCategory(name);
                MJdatabase.NotifyCategoriesChanged();
                LoadTruckCategories();
                LoadTrucks();
                ClearCatInputs();
                MessageBox.Show($"Category '{name}' deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ClearCatInputs()
        {
            selectedTruckCategory = null;
            txtCatName.Clear();
            numCatCapacity.Value = 300;
        }
"""

last_brace = text.rfind("}")
second_last_brace = text.rfind("}", 0, last_brace)
assert second_last_brace != -1

text = text[:second_last_brace] + category_methods + text[second_last_brace:]

with open(path, "w", encoding="utf-8") as f:
    f.write(text)

print("TruckManagementView.cs updated successfully!")
