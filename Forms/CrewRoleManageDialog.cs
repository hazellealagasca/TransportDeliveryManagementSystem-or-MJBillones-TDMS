using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    public class CrewRoleManageDialog : Form
    {
        public string? SelectedRoleName { get; private set; }

        private TextBox txtRoleName = null!;
        private Button btnAdd = null!;
        private Button btnUpdate = null!;
        private Button btnDelete = null!;
        private Button btnClear = null!;

        private TextBox txtSearch = null!;
        private Label lblSummary = null!;
        private DataGridView gridRoles = null!;
        private Button btnClose = null!;

        private List<CrewRoleItem> allRoles = new List<CrewRoleItem>();
        private CrewRoleItem? currentSelectedRole = null;

        public CrewRoleManageDialog(string? initialRoleToSelect = null)
        {
            this.SelectedRoleName = initialRoleToSelect;
            InitializeCustomComponent();
            LoadRoles();
            if (!string.IsNullOrWhiteSpace(initialRoleToSelect))
            {
                SelectRoleByName(initialRoleToSelect);
            }
            UIThemeHelper.ApplyTheme(this);
        }

        private void InitializeCustomComponent()
        {
            this.Text = "Crew Operational Role Configuration";
            this.Size = new Size(700, 530);
            this.MinimumSize = new Size(680, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Pal.C(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Pal.C(15, 23, 42),
                Padding = new Padding(20, 10, 20, 10)
            };

            Label lblTitle = new Label
            {
                Text = "🏷️ Crew Operational Roles Configuration",
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Pal.White,
                Dock = DockStyle.Top,
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Manage personnel operational titles and track active crew assignments",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Pal.C(148, 163, 184),
                Dock = DockStyle.Top,
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(lblTitle);

            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Pal.White,
                Padding = new Padding(16, 10, 20, 10)
            };

            btnClose = new Button
            {
                Text = "Done / Close",
                DialogResult = DialogResult.OK,
                Size = new Size(110, 32),
                Dock = DockStyle.Right,
                BackColor = Pal.C(15, 23, 42),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnClose);

            TableLayoutPanel tblMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(16, 14, 16, 8)
            };
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270F));
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Panel pnlFormCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Pal.White,
                Padding = new Padding(14),
                Margin = new Padding(0, 0, 12, 0)
            };

            Label lblFormTitle = new Label
            {
                Text = "Operational Role Details",
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                ForeColor = Pal.C(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 28
            };

            Label lblRoleName = new Label
            {
                Text = "Operational Role / Title Name *:",
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = Pal.C(71, 85, 105),
                Dock = DockStyle.Top,
                Height = 22
            };

            txtRoleName = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "e.g. Driver, Aide, Mechanic"
            };

            Panel pnlSpacer1 = new Panel { Dock = DockStyle.Top, Height = 16 };

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
                Text = "+ Add Role",
                Size = new Size(110, 32),
                Margin = new Padding(0, 0, 6, 6),
                BackColor = Pal.C(37, 99, 235),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += BtnAdd_Click;

            btnUpdate = new Button
            {
                Text = "Update",
                Size = new Size(110, 32),
                Margin = new Padding(0, 0, 0, 6),
                BackColor = Pal.C(22, 163, 74),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.Click += BtnUpdate_Click;

            btnDelete = new Button
            {
                Text = "Delete",
                Size = new Size(110, 32),
                Margin = new Padding(0, 0, 6, 6),
                BackColor = Pal.C(220, 38, 38),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += BtnDelete_Click;

            btnClear = new Button
            {
                Text = "Clear",
                Size = new Size(110, 32),
                Margin = new Padding(0, 0, 0, 6),
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 1;
            btnClear.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnClear.Click += (s, e) => ClearForm();

            flowButtons.Controls.Add(btnAdd);
            flowButtons.Controls.Add(btnUpdate);
            flowButtons.Controls.Add(btnDelete);
            flowButtons.Controls.Add(btnClear);

            pnlFormCard.Controls.Add(flowButtons);
            pnlFormCard.Controls.Add(pnlSpacer1);
            pnlFormCard.Controls.Add(txtRoleName);
            pnlFormCard.Controls.Add(lblRoleName);
            pnlFormCard.Controls.Add(lblFormTitle);

            Panel pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Pal.White,
                Padding = new Padding(0)
            };

            Panel pnlGridToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(12, 6, 12, 6),
                BackColor = Pal.White
            };

            lblSummary = new Label
            {
                Text = "📊 Total Roles: 0",
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };

            Panel pnlSearchWrap = new Panel { Dock = DockStyle.Right, Width = 180 };
            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Search roles..."
            };
            txtSearch.TextChanged += (s, e) => FilterRoles();
            pnlSearchWrap.Controls.Add(txtSearch);

            pnlGridToolbar.Controls.Add(pnlSearchWrap);
            pnlGridToolbar.Controls.Add(lblSummary);

            gridRoles = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Pal.White,
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
            gridRoles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Operational Role / Title", FillWeight = 62, MinimumWidth = 140 });
            gridRoles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Active Crew Assigned", FillWeight = 38, MinimumWidth = 110 });
            gridRoles.SelectionChanged += GridRoles_SelectionChanged;

            pnlGridCard.Controls.Add(gridRoles);
            pnlGridCard.Controls.Add(pnlGridToolbar);

            tblMain.Controls.Add(pnlFormCard, 0, 0);
            tblMain.Controls.Add(pnlGridCard, 1, 0);

            this.Controls.Add(tblMain);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);
        }

        private void LoadRoles()
        {
            try
            {
                allRoles = MJdatabase.GetCrewRoles();
                FilterRoles();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load crew roles: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterRoles()
        {
            gridRoles.SelectionChanged -= GridRoles_SelectionChanged;
            gridRoles.Rows.Clear();

            string q = txtSearch.Text.Trim().ToLowerInvariant();
            var list = string.IsNullOrEmpty(q)
                ? allRoles
                : allRoles.Where(r => r.RoleName.ToLower().Contains(q)).ToList();

            foreach (var r in list)
            {
                gridRoles.Rows.Add(r.RoleName, $"{r.ActiveCrewCount} active");
            }
            lblSummary.Text = $"📊 Total Roles: {list.Count} of {allRoles.Count}";

            if (gridRoles.Rows.Count > 0)
            {
                gridRoles.ClearSelection();
                gridRoles.Rows[0].Selected = true;
                PopulateFromRow(gridRoles.Rows[0]);
            }
            else
            {
                ClearForm();
            }

            gridRoles.SelectionChanged += GridRoles_SelectionChanged;
        }

        private void GridRoles_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridRoles.SelectedRows.Count > 0)
            {
                PopulateFromRow(gridRoles.SelectedRows[0]);
            }
        }

        private void PopulateFromRow(DataGridViewRow row)
        {
            string roleName = row.Cells[0].Value?.ToString() ?? string.Empty;
            currentSelectedRole = allRoles.FirstOrDefault(r => string.Equals(r.RoleName, roleName, StringComparison.OrdinalIgnoreCase));
            if (currentSelectedRole != null)
            {
                txtRoleName.Text = currentSelectedRole.RoleName;
                SelectedRoleName = currentSelectedRole.RoleName;
            }
        }

        private void SelectRoleByName(string name)
        {
            for (int i = 0; i < gridRoles.Rows.Count; i++)
            {
                string rName = gridRoles.Rows[i].Cells[0].Value?.ToString() ?? string.Empty;
                if (string.Equals(rName, name, StringComparison.OrdinalIgnoreCase))
                {
                    gridRoles.ClearSelection();
                    gridRoles.Rows[i].Selected = true;
                    PopulateFromRow(gridRoles.Rows[i]);
                    break;
                }
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            string name = txtRoleName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter an operational role title name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRoleName.Focus();
                return;
            }

            if (allRoles.Any(r => string.Equals(r.RoleName, name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"The operational role '{name}' already exists.", "Duplicate Role", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRoleName.Focus();
                return;
            }

            bool success = MJdatabase.SaveCrewRole(name);
            if (success)
            {
                SelectedRoleName = name;
                MJdatabase.NotifyCategoriesChanged();
                LoadRoles();
                SelectRoleByName(name);
                MessageBox.Show($"Operational role '{name}' added successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to save operational role.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            if (currentSelectedRole == null)
            {
                MessageBox.Show("Please select an operational role from the table to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string name = txtRoleName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a valid role title name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRoleName.Focus();
                return;
            }

            string oldName = currentSelectedRole.RoleName;
            bool success = MJdatabase.SaveCrewRole(name, oldName);
            if (success)
            {
                SelectedRoleName = name;
                MJdatabase.NotifyCategoriesChanged();
                LoadRoles();
                SelectRoleByName(name);
                MessageBox.Show($"Operational role updated successfully!", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to update operational role.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (currentSelectedRole == null)
            {
                MessageBox.Show("Please select an operational role from the table to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string name = currentSelectedRole.RoleName;

            if (currentSelectedRole.ActiveCrewCount > 0)
            {
                MessageBox.Show(
                    $"Cannot delete operational role '{name}' because {currentSelectedRole.ActiveCrewCount} active personnel are assigned to it.{Environment.NewLine}{Environment.NewLine}Please reassign or update those crew members before deleting this role.",
                    "Active Personnel Assigned",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete operational role '{name}'?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm == DialogResult.Yes)
            {
                MJdatabase.DeleteCrewRole(name);
                MJdatabase.NotifyCategoriesChanged();
                SelectedRoleName = null;
                ClearForm();
                LoadRoles();
                MessageBox.Show($"Operational role '{name}' was deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ClearForm()
        {
            currentSelectedRole = null;
            txtRoleName.Clear();
            if (gridRoles.Rows.Count > 0)
            {
                gridRoles.ClearSelection();
            }
        }
    }
}
