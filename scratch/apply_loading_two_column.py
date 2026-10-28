with open(r'Views\LoadingManagementView.cs', 'r', encoding='utf-8') as f:
    code = f.read()

two_column_methods = """
        private void ApplyTwoColumnFormLayout()
        {
            if (pnlForm == null) return;
            pnlForm.SuspendLayout();
            pnlForm.Controls.Clear();

            var pnlRowSchedClient = CreateTwoColumnRow(
                lblScheduleNumber, txtScheduleNumber,
                lblClient, cbClient
            );

            Panel pnlDestContainer = new Panel { Dock = DockStyle.Fill, Height = 28 };
            cbDestination.Dock = DockStyle.Fill;
            btnAddStop.Dock = DockStyle.Right;
            btnAddStop.Width = 140;
            pnlDestContainer.Controls.Add(cbDestination);
            pnlDestContainer.Controls.Add(btnAddStop);

            var pnlRowRouteCargo = CreateTwoColumnRow(
                lblDest, pnlDestContainer,
                lblCargoType, tblCargoPeak
            );

            var pnlRowDateTime = CreateTwoColumnRow(
                lblDate, dtpScheduleDate,
                lblTime, pnlTimeRow
            );

            pnlForm.Controls.Add(btnClone);
            pnlForm.Controls.Add(btnClear);
            pnlForm.Controls.Add(tblUpdateDelete);
            pnlForm.Controls.Add(btnSave);
            pnlForm.Controls.Add(pnlRowDateTime);
            pnlForm.Controls.Add(pnlRec);
            pnlForm.Controls.Add(txtTotalCrates);
            pnlForm.Controls.Add(lblTotalCrates);
            pnlForm.Controls.Add(tblCrates);
            pnlForm.Controls.Add(pnlRouteStoresBreakdown);
            pnlForm.Controls.Add(pnlRowRouteCargo);
            pnlForm.Controls.Add(pnlRowSchedClient);
            pnlForm.Controls.Add(pnlFormHeaderBar);

            pnlForm.ResumeLayout(true);
        }

        private TableLayoutPanel CreateTwoColumnRow(Control lbl1, Control ctrl1, Control lbl2, Control ctrl2)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 4),
                Padding = new Padding(0)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

            lbl1.Dock = DockStyle.Fill;
            if (lbl1 is Label l1)
            {
                l1.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
                l1.ForeColor = Color.FromArgb(30, 41, 59);
                l1.TextAlign = ContentAlignment.BottomLeft;
                l1.Padding = new Padding(0, 0, 4, 2);
            }
            ctrl1.Dock = DockStyle.Fill;
            tbl.Controls.Add(lbl1, 0, 0);
            tbl.Controls.Add(ctrl1, 0, 1);

            lbl2.Dock = DockStyle.Fill;
            if (lbl2 is Label l2)
            {
                l2.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
                l2.ForeColor = Color.FromArgb(30, 41, 59);
                l2.TextAlign = ContentAlignment.BottomLeft;
                l2.Padding = new Padding(4, 0, 0, 2);
            }
            ctrl2.Dock = DockStyle.Fill;
            tbl.Controls.Add(lbl2, 1, 0);
            tbl.Controls.Add(ctrl2, 1, 1);

            return tbl;
        }
"""

target_hook = "            pnlForm.Controls.Add(pnlRouteStoresBreakdown);"
assert target_hook in code, "target_hook not found"
code = code.replace(target_hook, target_hook + "\n            ApplyTwoColumnFormLayout();", 1)

target_insert = "        private void PopulateRouteStoresGrid(DeliveryRoute? route, List<DispatchStop>? savedStops = null)"
assert target_insert in code, "target_insert not found"
code = code.replace(target_insert, two_column_methods + "\n" + target_insert, 1)

with open(r'Views\LoadingManagementView.cs', 'w', encoding='utf-8') as f:
    f.write(code)

print("LoadingManagementView.cs successfully updated with 2-column layout matching Image 2!")
