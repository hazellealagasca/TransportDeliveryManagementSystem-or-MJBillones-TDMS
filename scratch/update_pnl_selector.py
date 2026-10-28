path = 'Views/CategoryManagementView.cs'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

target = '''            Panel pnlSelectorRow = new Panel
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
            pnlSelectorRow.Controls.Add(btnNewRoute);'''

rep = '''            TableLayoutPanel pnlSelectorRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
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

if target in text:
    text = text.replace(target, rep, 1)
    print("Replaced pnlSelectorRow (LF)")
elif target.replace('\n', '\r\n') in text:
    text = text.replace(target.replace('\n', '\r\n'), rep.replace('\n', '\r\n'), 1)
    print("Replaced pnlSelectorRow (CRLF)")
else:
    raise Exception("Target not found")

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print("pnlSelectorRow updated successfully!")
