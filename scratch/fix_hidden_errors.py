import os
import sqlite3
import glob

# 1. Update Database/MJdatabase.cs
mj_path = 'Database/MJdatabase.cs'
with open(mj_path, 'r', encoding='utf-8') as f:
    mj_text = f.read()

# Update SaveDeliveryRoute to prevent SQLite Error 19 UNIQUE constraint
save_route_target = '''                if (r.Id == 0)

                {

                    cmd.CommandText = @"INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, Description, ConnectedPlaces, IsActive, CreatedAt, UpdatedAt)'''

save_route_replacement = '''                if (r.Id == 0 && !string.IsNullOrWhiteSpace(r.RouteCode))
                {
                    using var checkCmd = conn.CreateCommand();
                    checkCmd.CommandText = "SELECT Id FROM DeliveryRoutes WHERE RouteCode = @rc LIMIT 1";
                    checkCmd.Parameters.AddWithValue("@rc", r.RouteCode);
                    var existingIdObj = checkCmd.ExecuteScalar();
                    if (existingIdObj != null && existingIdObj != DBNull.Value)
                    {
                        r.Id = Convert.ToInt32(existingIdObj);
                    }
                }

                if (r.Id == 0)

                {

                    cmd.CommandText = @"INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, Description, ConnectedPlaces, IsActive, CreatedAt, UpdatedAt)'''

if save_route_target in mj_text:
    mj_text = mj_text.replace(save_route_target, save_route_replacement, 1)
    print("Updated SaveDeliveryRoute (LF)")
elif save_route_target.replace('\n', '\r\n') in mj_text:
    mj_text = mj_text.replace(save_route_target.replace('\n', '\r\n'), save_route_replacement.replace('\n', '\r\n'), 1)
    print("Updated SaveDeliveryRoute (CRLF)")
else:
    print("save_route_target not found!")

# Add sanitization migration into migrations array
mig_target = '''"UPDATE DeliveryRoutes SET ClientCompanyName = 'BCS GROUP COMPANY' WHERE ClientCompanyName IS NULL OR TRIM(ClientCompanyName) = '';",'''
mig_rep = '''"UPDATE DeliveryRoutes SET ClientCompanyName = 'BCS GROUP COMPANY' WHERE ClientCompanyName IS NULL OR TRIM(ClientCompanyName) = '';",
                    "UPDATE DeliveryRoutes SET Description = REPLACE(Description, 'Franchise ', '') WHERE Description LIKE '%Franchise%';",
                    "UPDATE DeliveryRoutes SET Description = REPLACE(Description, 'FODGAA', 'EODGAA') WHERE Description LIKE '%FODGAA%';",'''

if mig_target in mj_text:
    mj_text = mj_text.replace(mig_target, mig_rep, 1)
    print("Added sanitization migration (LF)")
elif mig_target.replace('\n', '\r\n') in mj_text:
    mj_text = mj_text.replace(mig_target.replace('\n', '\r\n'), mig_rep.replace('\n', '\r\n'), 1)
    print("Added sanitization migration (CRLF)")
else:
    print("mig_target not found!")

with open(mj_path, 'w', encoding='utf-8') as f:
    f.write(mj_text)

# 2. Update Views/CategoryManagementView.cs
cat_path = 'Views/CategoryManagementView.cs'
with open(cat_path, 'r', encoding='utf-8') as f:
    cat_text = f.read()

# Fix currentFormWidth from 490F to 520F
cat_text = cat_text.replace('private float currentFormWidth = 490F;', 'private float currentFormWidth = 520F;', 1)

# Fix pnlSelectorRow layout so btnNewRoute is never clipped
pnl_sel_target = '''            Panel pnlSelectorRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
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
            pnlSelectorRow.Controls.Add(btnNewRoute);'''

pnl_sel_rep = '''            TableLayoutPanel pnlSelectorRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                ColumnCount = 3,
                Margin = new Padding(0, 2, 0, 6)
            };
            pnlSelectorRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            pnlSelectorRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlSelectorRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105F));

            Label lblSel = new Label
            {
                Text = "Select Route:",
                Font = new Font("Segoe UI Semibold", 8.5F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            cmbClientRoutes = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cmbClientRoutes.SelectedIndexChanged += CmbClientRoutes_SelectedIndexChanged;

            btnNewRoute = new Button
            {
                Text = "+ New Route",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 1, 0, 1)
            };
            btnNewRoute.FlatAppearance.BorderSize = 0;
            btnNewRoute.Click += BtnNewRoute_Click;

            pnlSelectorRow.Controls.Add(lblSel, 0, 0);
            pnlSelectorRow.Controls.Add(cmbClientRoutes, 1, 0);
            pnlSelectorRow.Controls.Add(btnNewRoute, 2, 0);'''

if pnl_sel_target in cat_text:
    cat_text = cat_text.replace(pnl_sel_target, pnl_sel_rep, 1)
    print("Replaced pnlSelectorRow (LF)")
elif pnl_sel_target.replace('\n', '\r\n') in cat_text:
    cat_text = cat_text.replace(pnl_sel_target.replace('\n', '\r\n'), pnl_sel_rep.replace('\n', '\r\n'), 1)
    print("Replaced pnlSelectorRow (CRLF)")
else:
    print("pnl_sel_target not found!")

# Fix BtnAdd_Click to use selectedRoute ?? new DeliveryRoute()
btn_add_route_target = '''                        DeliveryRoute newR = new DeliveryRoute
                        {
                            ClientCompanyName = val,
                            RouteCode = rCode,
                            RouteName = rName,
                            Description = txtRouteDescription.Text.Trim(),
                            ConnectedPlaces = string.Join("; ", placesList),
                            IsActive = chkRouteActive.Checked
                        };
                        MJdatabase.SaveDeliveryRoute(newR);'''

btn_add_route_rep = '''                        DeliveryRoute newR = selectedRoute ?? new DeliveryRoute();
                        newR.ClientCompanyName = val;
                        newR.RouteCode = rCode;
                        newR.RouteName = rName;
                        newR.Description = txtRouteDescription.Text.Trim();
                        newR.ConnectedPlaces = string.Join("; ", placesList);
                        newR.IsActive = chkRouteActive.Checked;
                        MJdatabase.SaveDeliveryRoute(newR);'''

if btn_add_route_target in cat_text:
    cat_text = cat_text.replace(btn_add_route_target, btn_add_route_rep, 1)
    print("Replaced BtnAdd_Click route logic (LF)")
elif btn_add_route_target.replace('\n', '\r\n') in cat_text:
    cat_text = cat_text.replace(btn_add_route_target.replace('\n', '\r\n'), btn_add_route_rep.replace('\n', '\r\n'), 1)
    print("Replaced BtnAdd_Click route logic (CRLF)")
else:
    print("btn_add_route_target not found!")

# Fix column headers in gridItems to prevent truncation
col_headers_target = '''                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Company Name", FillWeight = 26, MinimumWidth = 140 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Contact Person", FillWeight = 20, MinimumWidth = 110 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Contact Phone", FillWeight = 18, MinimumWidth = 110 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Address / Branch Location", FillWeight = 24, MinimumWidth = 140 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Assigned Routes", FillWeight = 12, MinimumWidth = 90 });'''

col_headers_rep = '''                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Company Name", FillWeight = 26, MinimumWidth = 130 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Contact Person", FillWeight = 20, MinimumWidth = 105 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Contact Phone", FillWeight = 18, MinimumWidth = 115 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Address / Branch", FillWeight = 24, MinimumWidth = 125 });
                gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Assigned Routes", FillWeight = 12, MinimumWidth = 95 });'''

if col_headers_target in cat_text:
    cat_text = cat_text.replace(col_headers_target, col_headers_rep, 1)
    print("Replaced gridItems column headers (LF)")
elif col_headers_target.replace('\n', '\r\n') in cat_text:
    cat_text = cat_text.replace(col_headers_target.replace('\n', '\r\n'), col_headers_rep.replace('\n', '\r\n'), 1)
    print("Replaced gridItems column headers (CRLF)")
else:
    print("col_headers_target not found!")

with open(cat_path, 'w', encoding='utf-8') as f:
    f.write(cat_text)

# 3. Sanitize all sqlite databases on disk
for db_file in glob.glob('**/mj_billones.db', recursive=True):
    try:
        conn = sqlite3.connect(db_file)
        c = conn.cursor()
        c.execute("UPDATE DeliveryRoutes SET Description = REPLACE(Description, 'Franchise ', '') WHERE Description LIKE '%Franchise%';")
        c.execute("UPDATE DeliveryRoutes SET Description = REPLACE(Description, 'FODGAA', 'EODGAA') WHERE Description LIKE '%FODGAA%';")
        conn.commit()
        conn.close()
        print(f"Sanitized db: {db_file}")
    except Exception as e:
        print(f"Error sanitizing {db_file}: {e}")

print("Fix completed successfully!")
