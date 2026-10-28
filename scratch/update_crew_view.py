import os

path = os.path.join("Views", "CrewManagementView.cs")
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

field_insert_marker = "public partial class CrewManagementView : UserControl, IRefreshable\n    {"
field_insert_marker_crlf = "public partial class CrewManagementView : UserControl, IRefreshable\r\n    {"

subtab_fields = """
        private Button btnSubTabCrew = null!;
        private Button btnSubTabRoles = null!;
        private Panel pnlSubNavTabs = null!;
        private Panel pnlRolesContainer = null!;
        private DataGridView gridCrewRoles = null!;
        private TextBox txtRoleName = null!;
        private Button btnAddRole = null!;
        private Button btnUpdateRole = null!;
        private Button btnDeleteRole = null!;
        private Button btnClearRole = null!;
        private TextBox txtRoleSearch = null!;
        private Label lblRoleSummary = null!;
        private List<CrewRoleItem> allCrewRoles = new List<CrewRoleItem>();
        private CrewRoleItem? selectedCrewRole = null;
"""

if field_insert_marker in text:
    text = text.replace(field_insert_marker, field_insert_marker + subtab_fields, 1)
elif field_insert_marker_crlf in text:
    text = text.replace(field_insert_marker_crlf, field_insert_marker_crlf + subtab_fields.replace("\n", "\r\n"), 1)
else:
    raise Exception("field_insert_marker not found")

init_marker = "InitializeComponent();"
assert init_marker in text, "init_marker not found"
text = text.replace(init_marker, init_marker + "\n            InitializeSubNavAndRoles();", 1)

old_load_roles = 'var crewRoles = MJdatabase.GetLookupItems("Crew Role");\n                string currentRole = cbRole.Text;\n                cbRole.Items.Clear();\n                var coreRoles = new[] { "Driver", "Aide/Helper", "Mechanic", "Fabricator" };\n                foreach (var cr in coreRoles)\n                {\n                    cbRole.Items.Add(cr);\n                }\n                foreach (var customRole in crewRoles)\n                {\n                    string cleanRole = customRole?.Trim() ?? "";\n                    if (!string.IsNullOrWhiteSpace(cleanRole) &&\n                        !cbRole.Items.Cast<string>().Any(i => i.Equals(cleanRole, StringComparison.OrdinalIgnoreCase)))\n                    {\n                        cbRole.Items.Add(cleanRole);\n                    }\n                }'
old_load_roles_crlf = old_load_roles.replace("\n", "\r\n")

new_load_roles = 'var rolesList = MJdatabase.GetCrewRoles();\n                string currentRole = cbRole.Text;\n                cbRole.Items.Clear();\n                var coreRoles = new[] { "Driver", "Aide/Helper", "Mechanic", "Fabricator" };\n                foreach (var cr in coreRoles)\n                {\n                    cbRole.Items.Add(cr);\n                }\n                foreach (var r in rolesList)\n                {\n                    string cleanRole = r.RoleName?.Trim() ?? "";\n                    if (!string.IsNullOrWhiteSpace(cleanRole) &&\n                        !cbRole.Items.Cast<string>().Any(i => i.Equals(cleanRole, StringComparison.OrdinalIgnoreCase)))\n                    {\n                        cbRole.Items.Add(cleanRole);\n                    }\n                }'

if old_load_roles in text:
    text = text.replace(old_load_roles, new_load_roles, 1)
elif old_load_roles_crlf in text:
    text = text.replace(old_load_roles_crlf, new_load_roles.replace("\n", "\r\n"), 1)
else:
    raise Exception("old_load_roles not found")

old_refresh = "public void RefreshData()\n        {\n            LoadCrew();\n        }"
old_refresh_crlf = old_refresh.replace("\n", "\r\n")
new_refresh = "public void RefreshData()\n        {\n            LoadCrew();\n            LoadCrewRoles();\n        }"
if old_refresh in text:
    text = text.replace(old_refresh, new_refresh, 1)
elif old_refresh_crlf in text:
    text = text.replace(old_refresh_crlf, new_refresh.replace("\n", "\r\n"), 1)

role_methods = """
        private void InitializeSubNavAndRoles()
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

            btnSubTabCrew = new Button
            {
                Text = "👤  Crew Members",
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
            btnSubTabCrew.FlatAppearance.BorderSize = 1;
            btnSubTabCrew.FlatAppearance.BorderColor = Color.FromArgb(15, 23, 42);
            btnSubTabCrew.Click += (s, e) => SwitchSubTab("Crew");

            btnSubTabRoles = new Button
            {
                Text = "🏷️  Crew Roles",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                MinimumSize = new Size(160, 32),
                Padding = new Padding(14, 0, 14, 0),
                Margin = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                Cursor = Cursors.Hand
            };
            btnSubTabRoles.FlatAppearance.BorderSize = 1;
            btnSubTabRoles.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnSubTabRoles.Click += (s, e) => SwitchSubTab("Roles");

            flowTabs.Controls.Add(btnSubTabCrew);
            flowTabs.Controls.Add(btnSubTabRoles);
            pnlSubNavTabs.Controls.Add(flowTabs);

            pnlRolesContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(16)
            };

            TableLayoutPanel tblRoleLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            tblRoleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380F));
            tblRoleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblRoleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Panel pnlRoleForm = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(16)
            };

            Label lblRoleHeader = new Label
            {
                Text = "Crew Operational Role Configuration",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 30
            };

            Label lblRoleName = new Label
            {
                Text = "Operational Role / Title Name:",
                Font = new Font("Segoe UI Semibold", 8.5F),
                Dock = DockStyle.Top,
                Height = 22
            };

            txtRoleName = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9F),
                MaxLength = 100
            };

            Panel pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 16 };

            FlowLayoutPanel flowRoleBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            btnAddRole = new Button
            {
                Text = "+ Add Role",
                Size = new Size(100, 32),
                Margin = new Padding(0, 0, 6, 6),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddRole.FlatAppearance.BorderSize = 0;
            btnAddRole.Click += BtnAddRole_Click;

            btnUpdateRole = new Button
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
            btnUpdateRole.FlatAppearance.BorderSize = 0;
            btnUpdateRole.Click += BtnUpdateRole_Click;

            btnDeleteRole = new Button
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
            btnDeleteRole.FlatAppearance.BorderSize = 0;
            btnDeleteRole.Click += BtnDeleteRole_Click;

            btnClearRole = new Button
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
            btnClearRole.FlatAppearance.BorderSize = 1;
            btnClearRole.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnClearRole.Click += (s, e) => ClearRoleInputs();

            flowRoleBtns.Controls.Add(btnAddRole);
            flowRoleBtns.Controls.Add(btnUpdateRole);
            flowRoleBtns.Controls.Add(btnDeleteRole);
            flowRoleBtns.Controls.Add(btnClearRole);

            pnlRoleForm.Controls.Add(flowRoleBtns);
            pnlRoleForm.Controls.Add(pnlSpacer);
            pnlRoleForm.Controls.Add(txtRoleName);
            pnlRoleForm.Controls.Add(lblRoleName);
            pnlRoleForm.Controls.Add(lblRoleHeader);

            Panel pnlRoleGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            Panel pnlRoleToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(12, 6, 12, 6),
                BackColor = Color.White
            };

            lblRoleSummary = new Label
            {
                Text = "📊 Total Operational Roles: 0",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };

            Panel pnlSearchWrap = new Panel { Dock = DockStyle.Right, Width = 220 };
            txtRoleSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Search roles..."
            };
            txtRoleSearch.TextChanged += (s, e) => FilterCrewRoles();
            pnlSearchWrap.Controls.Add(txtRoleSearch);

            pnlRoleToolbar.Controls.Add(pnlSearchWrap);
            pnlRoleToolbar.Controls.Add(lblRoleSummary);

            gridCrewRoles = new DataGridView
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
            gridCrewRoles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Operational Role / Title", FillWeight = 60, MinimumWidth = 180 });
            gridCrewRoles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Active Crew Assigned", FillWeight = 40, MinimumWidth = 140 });
            gridCrewRoles.SelectionChanged += GridCrewRoles_SelectionChanged;

            pnlRoleGridCard.Controls.Add(gridCrewRoles);
            pnlRoleGridCard.Controls.Add(pnlRoleToolbar);

            tblRoleLayout.Controls.Add(pnlRoleForm, 0, 0);
            tblRoleLayout.Controls.Add(pnlRoleGridCard, 1, 0);
            pnlRolesContainer.Controls.Add(tblRoleLayout);

            this.Controls.Add(pnlRolesContainer);
            this.Controls.Add(pnlSubNavTabs);

            pnlSubNavTabs.BringToFront();
            tblMasterContent.BringToFront();
        }

        private void SwitchSubTab(string tab)
        {
            if (tab == "Crew")
            {
                btnSubTabCrew.BackColor = Color.FromArgb(15, 23, 42);
                btnSubTabCrew.ForeColor = Color.White;
                btnSubTabCrew.FlatAppearance.BorderColor = Color.FromArgb(15, 23, 42);

                btnSubTabRoles.BackColor = Color.White;
                btnSubTabRoles.ForeColor = Color.FromArgb(71, 85, 105);
                btnSubTabRoles.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);

                pnlRolesContainer.Visible = false;
                tblMasterContent.Visible = true;
                tblMasterContent.BringToFront();
            }
            else
            {
                btnSubTabRoles.BackColor = Color.FromArgb(15, 23, 42);
                btnSubTabRoles.ForeColor = Color.White;
                btnSubTabRoles.FlatAppearance.BorderColor = Color.FromArgb(15, 23, 42);

                btnSubTabCrew.BackColor = Color.White;
                btnSubTabCrew.ForeColor = Color.FromArgb(71, 85, 105);
                btnSubTabCrew.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);

                tblMasterContent.Visible = false;
                pnlRolesContainer.Visible = true;
                pnlRolesContainer.BringToFront();
                LoadCrewRoles();
            }
        }

        private void LoadCrewRoles()
        {
            try
            {
                allCrewRoles = MJdatabase.GetCrewRoles();
                FilterCrewRoles();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load crew roles: {ex.Message}");
            }
        }

        private void FilterCrewRoles()
        {
            gridCrewRoles.SelectionChanged -= GridCrewRoles_SelectionChanged;
            gridCrewRoles.Rows.Clear();

            string q = txtRoleSearch.Text.Trim().ToLowerInvariant();
            var list = string.IsNullOrEmpty(q)
                ? allCrewRoles
                : allCrewRoles.Where(r => r.RoleName.ToLower().Contains(q)).ToList();

            foreach (var r in list)
            {
                gridCrewRoles.Rows.Add(r.RoleName, $"{r.ActiveCrewCount} active personnel");
            }
            lblRoleSummary.Text = $"📊 Total Operational Roles: {list.Count} of {allCrewRoles.Count}";

            if (gridCrewRoles.Rows.Count > 0)
            {
                gridCrewRoles.ClearSelection();
                gridCrewRoles.Rows[0].Selected = true;
                PopulateRoleFromRow(gridCrewRoles.Rows[0]);
            }
            else
            {
                ClearRoleInputs();
            }

            gridCrewRoles.SelectionChanged += GridCrewRoles_SelectionChanged;
        }

        private void GridCrewRoles_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridCrewRoles.SelectedRows.Count > 0)
            {
                PopulateRoleFromRow(gridCrewRoles.SelectedRows[0]);
            }
        }

        private void PopulateRoleFromRow(DataGridViewRow row)
        {
            string roleName = row.Cells[0].Value?.ToString() ?? "";
            selectedCrewRole = allCrewRoles.FirstOrDefault(r => string.Equals(r.RoleName, roleName, StringComparison.OrdinalIgnoreCase));
            if (selectedCrewRole != null)
            {
                txtRoleName.Text = selectedCrewRole.RoleName;
            }
        }

        private void BtnAddRole_Click(object? sender, EventArgs e)
        {
            string name = txtRoleName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a role name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRoleName.Focus();
                return;
            }

            bool success = MJdatabase.SaveCrewRole(name);
            if (success)
            {
                MJdatabase.NotifyCategoriesChanged();
                LoadCrewRoles();
                LoadCrew();
                MessageBox.Show($"Crew role '{name}' saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to save crew role.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdateRole_Click(object? sender, EventArgs e)
        {
            if (selectedCrewRole == null)
            {
                MessageBox.Show("Please select a role to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string name = txtRoleName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a role name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRoleName.Focus();
                return;
            }

            string oldName = selectedCrewRole.RoleName;
            bool success = MJdatabase.SaveCrewRole(name, oldName);
            if (success)
            {
                MJdatabase.NotifyCategoriesChanged();
                LoadCrewRoles();
                LoadCrew();
                MessageBox.Show($"Crew role updated successfully!", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to update crew role.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteRole_Click(object? sender, EventArgs e)
        {
            if (selectedCrewRole == null)
            {
                MessageBox.Show("Please select a role to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string name = selectedCrewRole.RoleName;
            var confirm = MessageBox.Show($"Are you sure you want to delete role '{name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                MJdatabase.DeleteCrewRole(name);
                MJdatabase.NotifyCategoriesChanged();
                LoadCrewRoles();
                LoadCrew();
                ClearRoleInputs();
                MessageBox.Show($"Role '{name}' deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ClearRoleInputs()
        {
            selectedCrewRole = null;
            txtRoleName.Clear();
        }
"""

last_brace = text.rfind("}")
second_last_brace = text.rfind("}", 0, last_brace)
assert second_last_brace != -1

text = text[:second_last_brace] + role_methods + text[second_last_brace:]

with open(path, "w", encoding="utf-8") as f:
    f.write(text)

print("CrewManagementView.cs updated successfully!")
