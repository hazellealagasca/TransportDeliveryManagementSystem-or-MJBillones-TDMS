with open(r'Views\DispatchManagementView.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# 1. Update constructor to call ApplyMinimalisticSideBySideFormLayout and disable pnlSummary
old_ctor_call = """            gridDispatch.BringToFront();

            SetupAideModuleEvents();"""

new_ctor_call = """            gridDispatch.BringToFront();
            if (pnlSummary != null)
            {
                pnlSummary.Visible = false;
                pnlGridCard.Controls.Remove(pnlSummary);
            }

            SetupAideModuleEvents();"""

assert old_ctor_call in code, "old_ctor_call not found"
code = code.replace(old_ctor_call, new_ctor_call, 1)

# 2. Update InitializeSummaryBadges to immediately return and hide pnlSummary
old_summary_badges = """        private void InitializeSummaryBadges()

        {

            if (pnlSummary == null) return;

            lblSummary.Visible = false;"""

new_summary_badges = """        private void InitializeSummaryBadges()

        {

            if (pnlSummary == null) return;

            pnlSummary.Visible = false;
            return;

            lblSummary.Visible = false;"""

assert old_summary_badges in code, "old_summary_badges not found"
code = code.replace(old_summary_badges, new_summary_badges, 1)

# 3. Add btnAddRoute and side by side layout method after InitializeRouteStoresBreakdownPanel
side_by_side_methods = """
        private Button btnAddRoute = null!;

        private void ApplyMinimalisticSideBySideFormLayout()
        {
            if (pnlFormBody == null) return;
            pnlFormBody.SuspendLayout();
            pnlFormBody.Controls.Clear();

            btnAddRoute = new Button
            {
                Text = "＋ Add",
                Dock = DockStyle.Right,
                Width = 68,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddRoute.FlatAppearance.BorderSize = 0;
            btnAddRoute.Click += BtnAddRoute_Click;

            var pnlRowOrder = CreateSideBySideRow(lblDispatchNumber, txtDispatchNumber, 32);
            var pnlRowClient = CreateSideBySideRow(lblClient, cbClient, 32);

            Panel pnlRouteContainer = new Panel { Dock = DockStyle.Fill };
            cbDestination.Dock = DockStyle.Fill;
            pnlRouteContainer.Controls.Add(cbDestination);
            pnlRouteContainer.Controls.Add(btnAddRoute);
            var pnlRowRoute = CreateSideBySideRow(lblDest, pnlRouteContainer, 32);

            var pnlRowCargoPeak = CreateSideBySideRow(lblCargoType, tblCargoPeak, 34);
            var pnlRowCrates = CreateSideBySideRow(lblGCrate, tblCrates, 34);
            lblGCrate.Text = "Crates (G / E):";

            var pnlRowTruck = CreateSideBySideRow(lblTr, cbTruck, 32);
            var pnlRowDriver = CreateSideBySideRow(lblDr, cbDriver, 32);

            var pnlRowDateTime = CreateSideBySideRow(lblDd, tblDateTime, 34);
            lblDd.Text = "Dispatch Date & Time:";

            var pnlRowStatus = CreateSideBySideRow(lblSt, cbStatus, 32);
            lblSt.Text = "Dispatch Status:";

            Panel pnlAidesContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            lblAidesSummary.Dock = DockStyle.Top;
            flowAidesModule.Dock = DockStyle.Top;
            pnlAidesSearchRow.Dock = DockStyle.Top;
            pnlAidesContainer.Controls.Add(lblAidesSummary);
            pnlAidesContainer.Controls.Add(flowAidesModule);
            pnlAidesContainer.Controls.Add(pnlAidesSearchRow);
            var pnlRowAides = CreateSideBySideRow(lblAi, pnlAidesContainer, 140);
            lblAi.TextAlign = ContentAlignment.TopRight;
            lblAi.Padding = new Padding(0, 6, 8, 0);

            var lblStep1 = CreateStepHeaderBanner("① STEP 1: ROUTE & STORE CRATE LOADING");
            var lblStep2 = CreateStepHeaderBanner("② STEP 2: TRUCK & CREW ASSIGNMENT");
            var lblStep3 = CreateStepHeaderBanner("③ STEP 3: SCHEDULE & DISPATCH STATUS");

            pnlFormBody.Controls.Add(pnlRowStatus);
            pnlFormBody.Controls.Add(pnlRowDateTime);
            pnlFormBody.Controls.Add(lblStep3);

            pnlFormBody.Controls.Add(pnlRowAides);
            pnlFormBody.Controls.Add(pnlRowDriver);
            pnlFormBody.Controls.Add(pnlRowTruck);
            pnlFormBody.Controls.Add(lblStep2);

            pnlFormBody.Controls.Add(pnlRec);
            pnlFormBody.Controls.Add(pnlRowCrates);
            pnlFormBody.Controls.Add(pnlRowCargoPeak);
            pnlFormBody.Controls.Add(pnlRouteStoresBreakdown);
            pnlFormBody.Controls.Add(pnlRowRoute);
            pnlFormBody.Controls.Add(pnlRowClient);
            pnlFormBody.Controls.Add(pnlRowOrder);
            pnlFormBody.Controls.Add(lblStep1);

            pnlFormBody.ResumeLayout(true);
        }

        private Panel CreateSideBySideRow(Control labelCtrl, Control inputCtrl, int height)
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = height,
                Padding = new Padding(0, 2, 0, 2),
                Margin = new Padding(0, 0, 0, 2)
            };

            labelCtrl.Dock = DockStyle.Left;
            labelCtrl.Width = 135;
            if (labelCtrl is Label lbl)
            {
                lbl.TextAlign = ContentAlignment.MiddleRight;
                lbl.Padding = new Padding(0, 0, 8, 0);
                lbl.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
                lbl.ForeColor = Color.FromArgb(30, 41, 59);
            }

            inputCtrl.Dock = DockStyle.Fill;

            pnl.Controls.Add(inputCtrl);
            pnl.Controls.Add(labelCtrl);
            return pnl;
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

target_end_breakdown = "            pnlFormBody.Controls.SetChildIndex(lblStep3, dtIdx + 1);"
assert target_end_breakdown in code, "target_end_breakdown not found"
code = code.replace(target_end_breakdown, target_end_breakdown + "\n            ApplyMinimalisticSideBySideFormLayout();", 1)

target_insert_point = "        private void PopulateRouteStoresGrid(DeliveryRoute? route, List<DispatchStop>? savedStops = null)"
assert target_insert_point in code, "target_insert_point not found"
code = code.replace(target_insert_point, side_by_side_methods + "\n" + target_insert_point, 1)

with open(r'Views\DispatchManagementView.cs', 'w', encoding='utf-8') as f:
    f.write(code)

print("DispatchManagementView.cs updated with minimalistic layout and + Add Route button!")
