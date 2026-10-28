with open('Views/UserManagementView.cs', 'r', encoding='utf-8') as f:
    content = f.read()

target = """            btnPrint.Visible = false;
            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("📊 Export Table to CSV...", null, (s, e) => DataExportHelper.ExportToCsv(gridUsers, "User_Accounts", "User Accounts"));
            menuActions.Items.Add("🖨 Print User Directory", null, BtnPrint_Click);
            var btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);
            pnlGridActionsFlow.Controls.Add(btnActions);
        }"""

replacement = """            btnPrint.Visible = false;
            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("📊 Export Table to CSV...", null, (s, e) => DataExportHelper.ExportToCsv(gridUsers, "User_Accounts", "User Accounts"));
            menuActions.Items.Add("🖨 Print User Directory", null, BtnPrint_Click);
            var btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);
            pnlGridActionsFlow.Controls.Add(btnActions);

            var btnRegisterStaff = new Button
            {
                Text = "👤 Register Staff Account",
                Size = new Size(185, 30),
                BackColor = Color.FromArgb(185, 28, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 4)
            };
            btnRegisterStaff.FlatAppearance.BorderSize = 0;
            btnRegisterStaff.Click += (s, e) =>
            {
                using (var dlg = new RegisterDialog(isAdminMode: true))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        LoadUsers();
                        ToastHelper.ShowToast(this, "New staff account registered and activated ✓", ToastType.Success);
                    }
                }
            };
            pnlGridActionsFlow.Controls.Add(btnRegisterStaff);
            pnlGridActionsFlow.Controls.SetChildIndex(btnRegisterStaff, 1);
        }"""

assert target in content, 'target not found in UserManagementView.cs'
content = content.replace(target, replacement)

with open('Views/UserManagementView.cs', 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(content)
print('Updated Views/UserManagementView.cs successfully')
