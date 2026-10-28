with open(r'Views\CategoryManagementView.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# 1. Update Category Form Inputs to Side-by-Side rows
old_inputs_block = """            Panel pnlCat = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 0, 6, 0) };
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
            tblInputs.Controls.Add(pnlClientRoutesSection, 0, 3);"""

new_inputs_block = """            Label lblCat = new Label { Text = "Category:", Dock = DockStyle.Left, Width = 115, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };
            cbCategoryType = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cbCategoryType.Items.AddRange(new object[] { "Client Company", "Payment Method" });
            cbCategoryType.SelectedIndex = 0;
            cbCategoryType.SelectedIndexChanged += CbCategoryType_SelectedIndexChanged;
            Panel pnlRowCat = new Panel { Dock = DockStyle.Top, Height = 30, Margin = new Padding(0, 0, 0, 4) };
            pnlRowCat.Controls.Add(cbCategoryType);
            pnlRowCat.Controls.Add(lblCat);

            lblVal = new Label { Text = "Client Name *:", Dock = DockStyle.Left, Width = 115, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };
            txtItemValue = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F), MaxLength = 150 };
            txtItemValue.TextChanged += (s, e) => {
                if (selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase))
                {
                    LoadClientRoutes(txtItemValue.Text.Trim());
                }
            };
            Panel pnlRowVal = new Panel { Dock = DockStyle.Top, Height = 30, Margin = new Padding(0, 0, 0, 4) };
            pnlRowVal.Controls.Add(txtItemValue);
            pnlRowVal.Controls.Add(lblVal);

            lblContactPerson = new Label { Text = "Contact Person:", Dock = DockStyle.Left, Width = 115, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 8.5F), ForeColor = Color.FromArgb(30, 41, 59) };
            txtContactPerson = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F), MaxLength = 100 };
            Panel pnlRowCP = new Panel { Dock = DockStyle.Top, Height = 30, Margin = new Padding(0, 0, 0, 4) };
            pnlRowCP.Controls.Add(txtContactPerson);
            pnlRowCP.Controls.Add(lblContactPerson);

            lblContactNumber = new Label { Text = "Contact Phone:", Dock = DockStyle.Left, Width = 115, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 8.5F), ForeColor = Color.FromArgb(30, 41, 59) };
            txtContactNumber = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F), MaxLength = 20 };
            txtContactNumber.KeyPress += (s, e) => {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '+' && e.KeyChar != '-')
                {
                    e.Handled = true;
                }
            };
            Panel pnlRowCN = new Panel { Dock = DockStyle.Top, Height = 30, Margin = new Padding(0, 0, 0, 4) };
            pnlRowCN.Controls.Add(txtContactNumber);
            pnlRowCN.Controls.Add(lblContactNumber);

            lblAddress = new Label { Text = "Address:", Dock = DockStyle.Left, Width = 115, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 8.5F), ForeColor = Color.FromArgb(30, 41, 59) };
            txtAddress = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F), MaxLength = 250 };
            Panel pnlRowAddr = new Panel { Dock = DockStyle.Top, Height = 30, Margin = new Padding(0, 0, 0, 6) };
            pnlRowAddr.Controls.Add(txtAddress);
            pnlRowAddr.Controls.Add(lblAddress);

            pnlClientRoutesSection = BuildClientRoutesSection();

            Panel pnlFieldsStack = new Panel { Dock = DockStyle.Top, AutoSize = true };
            pnlFieldsStack.Controls.Add(pnlRowAddr);
            pnlFieldsStack.Controls.Add(pnlRowCN);
            pnlFieldsStack.Controls.Add(pnlRowCP);
            pnlFieldsStack.Controls.Add(pnlRowVal);
            pnlFieldsStack.Controls.Add(pnlRowCat);

            tblInputs.Controls.Add(pnlFieldsStack, 0, 0);
            tblInputs.Controls.Add(pnlClientRoutesSection, 0, 1);"""

code_norm = code.replace('\r\n', '\n')
old_norm = old_inputs_block.replace('\r\n', '\n')
new_norm = new_inputs_block.replace('\r\n', '\n')

assert old_norm in code_norm, "old_inputs_block not found in CategoryManagementView.cs"
code_norm = code_norm.replace(old_norm, new_norm, 1)

# 2. Update Route Section inputs to Side-by-Side rows
old_route_inputs = """            TableLayoutPanel tblRouteInputs = new TableLayoutPanel
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
            pnlRDesc.Controls.Add(lblRDesc);"""

new_route_inputs = """            Label lblRCode = new Label { Text = "Route Code *:", Dock = DockStyle.Left, Width = 115, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 8.5F), ForeColor = Color.FromArgb(30, 41, 59) };
            txtRouteCode = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F), MaxLength = 20 };
            Panel pnlRCode = new Panel { Dock = DockStyle.Top, Height = 30, Margin = new Padding(0, 0, 0, 3) };
            pnlRCode.Controls.Add(txtRouteCode);
            pnlRCode.Controls.Add(lblRCode);

            Label lblRName = new Label { Text = "Route Name *:", Dock = DockStyle.Left, Width = 115, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 8.5F), ForeColor = Color.FromArgb(30, 41, 59) };
            txtRouteName = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F), MaxLength = 100 };
            Panel pnlRName = new Panel { Dock = DockStyle.Top, Height = 30, Margin = new Padding(0, 0, 0, 3) };
            pnlRName.Controls.Add(txtRouteName);
            pnlRName.Controls.Add(lblRName);

            Label lblRDesc = new Label { Text = "Description:", Dock = DockStyle.Left, Width = 115, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 8, 0), Font = new Font("Segoe UI Semibold", 8.5F), ForeColor = Color.FromArgb(30, 41, 59) };
            txtRouteDescription = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F), MaxLength = 200 };
            Panel pnlRDesc = new Panel { Dock = DockStyle.Top, Height = 30, Margin = new Padding(0, 0, 0, 4) };
            pnlRDesc.Controls.Add(txtRouteDescription);
            pnlRDesc.Controls.Add(lblRDesc);

            Panel tblRouteInputs = new Panel { Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 4, 0, 4) };
            tblRouteInputs.Controls.Add(pnlRDesc);
            tblRouteInputs.Controls.Add(pnlRName);
            tblRouteInputs.Controls.Add(pnlRCode);"""

old_r_norm = old_route_inputs.replace('\r\n', '\n')
new_r_norm = new_route_inputs.replace('\r\n', '\n')

assert old_r_norm in code_norm, "old_route_inputs not found in CategoryManagementView.cs"
code_norm = code_norm.replace(old_r_norm, new_r_norm, 1)

with open(r'Views\CategoryManagementView.cs', 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(code_norm)

print("CategoryManagementView.cs successfully updated to side-by-side layout!")
