import re

path = r'Views\CrewManagementView.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

fields_target = """        private Button btnSubTabCrew = null!;
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
        private CrewRoleItem? selectedCrewRole = null;"""

fields_replacement = """        private Button btnManageRoles = null!;"""

assert fields_target in content, "fields_target not found"
content = content.replace(fields_target, fields_replacement)

init_target = """            InitializeComponent();
            InitializeSubNavAndRoles();"""

init_replacement = """            InitializeComponent();
            SetupRoleFieldWithManageButton();"""

assert init_target in content, "init_target not found"
content = content.replace(init_target, init_replacement)

refresh_target = """        public void RefreshData()
        {
            LoadCrew();
            LoadCrewRoles();
        }"""

refresh_replacement = """        public void RefreshData()
        {
            LoadCrew();
        }"""

assert refresh_target in content, "refresh_target not found"
content = content.replace(refresh_target, refresh_replacement)

methods_start = content.find("        private void InitializeSubNavAndRoles()")
assert methods_start != -1, "InitializeSubNavAndRoles not found"

new_methods = """        private void SetupRoleFieldWithManageButton()
        {
            int roleIdx = pnlFormBody.Controls.IndexOf(cbRole);
            if (roleIdx >= 0)
            {
                pnlFormBody.Controls.Remove(cbRole);

                TableLayoutPanel tblRoleWrap = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Height = 32,
                    ColumnCount = 2,
                    RowCount = 1,
                    Margin = new Padding(0, 0, 0, 8),
                    Padding = new Padding(0)
                };
                tblRoleWrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                tblRoleWrap.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
                tblRoleWrap.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                btnManageRoles = new Button
                {
                    Dock = DockStyle.Fill,
                    Text = "⚙️ Roles",
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(241, 245, 249),
                    ForeColor = Color.FromArgb(30, 41, 59),
                    Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0)
                };
                btnManageRoles.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
                btnManageRoles.FlatAppearance.BorderSize = 1;
                btnManageRoles.Click += BtnManageRoles_Click;

                cbRole.Dock = DockStyle.Fill;
                cbRole.Margin = new Padding(0, 0, 6, 0);

                tblRoleWrap.Controls.Add(cbRole, 0, 0);
                tblRoleWrap.Controls.Add(btnManageRoles, 1, 0);

                pnlFormBody.Controls.Add(tblRoleWrap);
                pnlFormBody.Controls.SetChildIndex(tblRoleWrap, roleIdx);
            }
        }

        private void BtnManageRoles_Click(object? sender, EventArgs e)
        {
            string currentRole = cbRole.Text;
            using (var dlg = new Forms.CrewRoleManageDialog(currentRole))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK || !string.IsNullOrWhiteSpace(dlg.SelectedRoleName))
                {
                    LoadCrew();
                    if (!string.IsNullOrWhiteSpace(dlg.SelectedRoleName))
                    {
                        var match = cbRole.Items.Cast<string>().FirstOrDefault(i => i.Equals(dlg.SelectedRoleName.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                        {
                            cbRole.SelectedItem = match;
                        }
                        else
                        {
                            cbRole.Items.Add(dlg.SelectedRoleName.Trim());
                            cbRole.SelectedItem = dlg.SelectedRoleName.Trim();
                        }
                    }
                }
            }
        }
    }
}
"""

content = content[:methods_start] + new_methods

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print("SUCCESS: CrewManagementView.cs successfully refactored!")
