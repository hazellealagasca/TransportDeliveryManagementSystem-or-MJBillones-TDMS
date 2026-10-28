with open(r'Views\DispatchManagementView.cs', 'r', encoding='utf-8') as f:
    code = f.read()

two_column_methods = """
        private Button btnAddRoute = null!;

        private void ApplyTwoColumnFormLayout()
        {
            if (pnlFormBody == null) return;
            pnlFormBody.SuspendLayout();
            pnlFormBody.Controls.Clear();

            btnAddRoute = new Button
            {
                Text = "+",
                Dock = DockStyle.Right,
                Width = 38,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Bold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddRoute.FlatAppearance.BorderSize = 0;
            btnAddRoute.Click += BtnAddRoute_Click;

            var pnlRowOrderClient = CreateTwoColumnRow(
                lblDispatchNumber, txtDispatchNumber,
                lblClient, cbClient
            );

            Panel pnlRouteContainer = new Panel { Dock = DockStyle.Fill, Height = 28 };
            cbDestination.Dock = DockStyle.Fill;
            pnlRouteContainer.Controls.Add(cbDestination);
            pnlRouteContainer.Controls.Add(btnAddRoute);

            Panel pnlCargoContainer = new Panel { Dock = DockStyle.Fill, Height = 28 };
            cbCargoType.Dock = DockStyle.Fill;
            chkIsPeak.Dock = DockStyle.Right;
            chkIsPeak.Width = 100;
            chkIsPeak.Text = "Tariff Season";
            pnlCargoContainer.Controls.Add(cbCargoType);
            pnlCargoContainer.Controls.Add(chkIsPeak);

            var pnlRowRouteCargo = CreateTwoColumnRow(
                lblDest, pnlRouteContainer,
                lblCargoType, pnlCargoContainer
            );

            var pnlRowTruckDriver = CreateTwoColumnRow(
                lblTr, cbTruck,
                lblDr, cbDriver
            );

            var pnlRowDateTime = CreateTwoColumnRow(
                lblDd, dtpDispatchDate,
                lblDepartureTime, pnlTimeRow
            );
            lblDd.Text = "Dispatch Date & Time:";
            lblDepartureTime.Text = "Departure Time:";

            var pnlRowStatus = CreateTwoColumnRow(
                lblSt, cbStatus,
                new Label { Visible = false }, new Panel { Visible = false }
            );
            lblSt.Text = "Dispatch Status:";

            Panel pnlAidesSection = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 4)
            };
            lblAidesSummary.Dock = DockStyle.Top;
            flowAidesModule.Dock = DockStyle.Top;
            pnlAidesSearchRow.Dock = DockStyle.Top;
            lblAi.Dock = DockStyle.Top;
            lblAi.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            lblAi.ForeColor = Color.FromArgb(30, 41, 59);
            lblAi.Padding = new Padding(0, 2, 0, 2);
            pnlAidesSection.Controls.Add(lblAidesSummary);
            pnlAidesSection.Controls.Add(flowAidesModule);
            pnlAidesSection.Controls.Add(pnlAidesSearchRow);
            pnlAidesSection.Controls.Add(lblAi);

            var lblStep1 = CreateStepHeaderBanner("① STEP 1: ROUTE & STORE CRATE LOADING");
            var lblStep2 = CreateStepHeaderBanner("② STEP 2: TRUCK & CREW ASSIGNMENT");
            var lblStep3 = CreateStepHeaderBanner("③ STEP 3: SCHEDULE & DISPATCH STATUS");

            pnlFormBody.Controls.Add(pnlRowStatus);
            pnlFormBody.Controls.Add(pnlRowDateTime);
            pnlFormBody.Controls.Add(lblStep3);

            pnlFormBody.Controls.Add(pnlAidesSection);
            pnlFormBody.Controls.Add(pnlRowTruckDriver);
            pnlFormBody.Controls.Add(lblStep2);

            pnlFormBody.Controls.Add(pnlRec);
            pnlFormBody.Controls.Add(tblCrates);
            pnlFormBody.Controls.Add(pnlRouteStoresBreakdown);
            pnlFormBody.Controls.Add(pnlRowRouteCargo);
            pnlFormBody.Controls.Add(pnlRowOrderClient);
            pnlFormBody.Controls.Add(lblStep1);

            pnlFormBody.ResumeLayout(true);
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

        private void BtnAddRoute_Click(object? sender, EventArgs e)
        {
            string currentClient = cbClient.Text.Trim();
            using var dlg = new QuickAddRouteDialog(currentClient);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.CreatedRoute != null)
            {
                var route = dlg.CreatedRoute;
                if (!string.IsNullOrWhiteSpace(route.ClientCompanyName) && cbClient.Items.Contains(route.ClientCompanyName))
                {
                    cbClient.SelectedItem = route.ClientCompanyName;
                }
                ReloadRoutesList();
                string routeDisplay = $"{route.RouteCode} - {route.RouteName}";
                if (!cbDestination.Items.Contains(routeDisplay))
                {
                    cbDestination.Items.Add(routeDisplay);
                }
                cbDestination.SelectedItem = routeDisplay;
                cbDestination.Text = routeDisplay;
                PopulateRouteStoresGrid(route);
                numGCrate.Focus();
            }
        }
"""

old_method_start = "        private Button btnAddRoute = null!;"
old_method_end = "PopulateRouteStoresGrid(route);\n                numGCrate.Focus();\n            }\n        }"

idx_start = code.find(old_method_start)
idx_end = code.find(old_method_end)

assert idx_start != -1, "old_method_start not found"
assert idx_end != -1, "old_method_end not found"

code = code[:idx_start] + two_column_methods + "\n" + code[idx_end + len(old_method_end):]

# Also change the call in InitializeRouteStoresBreakdownPanel
old_call = "ApplyMinimalisticSideBySideFormLayout();"
new_call = "ApplyTwoColumnFormLayout();"
code = code.replace(old_call, new_call)

with open(r'Views\DispatchManagementView.cs', 'w', encoding='utf-8') as f:
    f.write(code)

print("DispatchManagementView.cs successfully updated with 2-column layout matching Image 2!")
