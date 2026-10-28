import os

view_path = r"Views/CompletedDispatchesView.cs"
with open(view_path, "r", encoding="utf-8") as f:
    text = f.read()

old_fields = """        private Label lblTotalTrips = null!;
        private Label lblTotalCrates = null!;
        private Label lblTotalGCrates = null!;
        private Label lblTotalECrates = null!;
        private Label lblGridCount = null!;"""

new_fields = """        private Label lblTotalTrips = null!;
        private Label lblGridCount = null!;"""

assert old_fields in text, "old_fields not found"
text = text.replace(old_fields, new_fields)

old_row_styles = """            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F));
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));"""

new_row_styles = """            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F));
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));"""

assert old_row_styles in text, "old_row_styles not found"
text = text.replace(old_row_styles, new_row_styles)

old_kpis_and_toolbar = """            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 8)
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            pnlKpis.Controls.Add(CreateKpiCard("TOTAL COMPLETED TRIPS", "0", Color.FromArgb(16, 185, 129), out lblTotalTrips), 0, 0);
            pnlKpis.Controls.Add(CreateKpiCard("TOTAL CRATES DELIVERED", "0", Color.FromArgb(59, 130, 246), out lblTotalCrates), 1, 0);
            pnlKpis.Controls.Add(CreateKpiCard("G-CRATES COMPLETED", "0", Color.FromArgb(139, 92, 246), out lblTotalGCrates), 2, 0);
            pnlKpis.Controls.Add(CreateKpiCard("E-CRATES COMPLETED", "0", Color.FromArgb(245, 158, 11), out lblTotalECrates), 3, 0);

            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Color.White,
                Padding = new Padding(8)
            };

            var flowFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            txtSearch = new TextBox
            {
                PlaceholderText = "🔍 Search dispatch #, client, destination, plate, driver...",
                Size = new Size(290, 28),
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 2, 8, 0)
            };
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            cbDateFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(130, 28),
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 2, 8, 0)
            };
            cbDateFilter.Items.AddRange(new object[] { "All Dates", "Today", "Last 7 Days", "This Month", "Last 30 Days" });
            cbDateFilter.SelectedIndex = 0;
            cbDateFilter.SelectedIndexChanged += (s, e) => ApplyFilter();

            var btnClearFilter = new Button
            {
                Text = "Reset",
                Font = new Font("Segoe UI", 8.5F),
                Size = new Size(60, 28),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 8, 0)
            };
            btnClearFilter.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnClearFilter.Click += (s, e) =>
            {
                txtSearch.Clear();
                cbDateFilter.SelectedIndex = 0;
            };

            flowFilter.Controls.Add(txtSearch);
            flowFilter.Controls.Add(cbDateFilter);
            flowFilter.Controls.Add(btnClearFilter);

            var flowActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            var btnViewDetails = new Button
            {
                Text = "🔍 View Schedule",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(125, 28),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 2, 4, 0)
            };
            btnViewDetails.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnViewDetails.Click += (s, e) => ShowSelectedDetails();

            var btnPrintDossier = new Button
            {
                Text = "🖨️ Print Dossier",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(125, 28),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 2, 4, 0)
            };
            btnPrintDossier.FlatAppearance.BorderSize = 0;
            btnPrintDossier.Click += (s, e) => PrintSelectedDossier();

            var btnPrintTripReport = new Button
            {
                Text = "📋 Trip Report",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(148, 28),
                BackColor = Color.FromArgb(30, 58, 138),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 2, 4, 0)
            };
            btnPrintTripReport.FlatAppearance.BorderSize = 0;
            btnPrintTripReport.Click += (s, e) => PrintSelectedTripReport();

            var btnIncidents = new Button
            {
                Text = "⚠️ Incident Log",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(115, 28),
                BackColor = Color.FromArgb(254, 242, 242),
                ForeColor = Color.FromArgb(185, 28, 28),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 2, 4, 0)
            };
            btnIncidents.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnIncidents.Click += (s, e) => NavigateToIncident();

            var btnTripReport = new Button
            {
                Text = "📝 Trip Report",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(110, 28),
                BackColor = Color.FromArgb(238, 242, 255),
                ForeColor = Color.FromArgb(67, 56, 202),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 2, 0, 0)
            };
            btnTripReport.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
            btnTripReport.Click += (s, e) => NavigateToTripReport();

            var btnCreateInvoice = new Button
            {
                Text = "🧾 Create Invoice",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(130, 28),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 2, 4, 0)
            };
            btnCreateInvoice.FlatAppearance.BorderSize = 0;
            btnCreateInvoice.Click += (s, e) =>
            {
                if (gridCompleted.SelectedRows.Count > 0 && gridCompleted.SelectedRows[0].DataBoundItem is Delivery del)
                {
                    OnNavigateToCreateInvoiceRequested?.Invoke(del);
                }
                else
                {
                    MessageBox.Show("Please select a completed delivery from the list to create its billing invoice.", "Select Delivery", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            flowActions.Controls.Add(btnCreateInvoice);
            flowActions.Controls.Add(btnViewDetails);
            flowActions.Controls.Add(btnPrintTripReport);
            flowActions.Controls.Add(btnPrintDossier);
            flowActions.Controls.Add(btnIncidents);
            flowActions.Controls.Add(btnTripReport);

            pnlToolbar.Controls.Add(flowFilter);
            pnlToolbar.Controls.Add(flowActions);"""

new_kpis_and_toolbar = """            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 8)
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280F));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            pnlKpis.Controls.Add(CreateKpiCard("TOTAL COMPLETED DISPATCHES", "0", Color.FromArgb(16, 185, 129), out lblTotalTrips), 0, 0);

            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Color.White,
                Padding = new Padding(10, 8, 10, 8)
            };

            var pnlSearchRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32
            };

            var flowFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            txtSearch = new TextBox
            {
                PlaceholderText = "🔍 Search dispatch #, client, destination, plate, driver...",
                Size = new Size(320, 28),
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 1, 8, 0)
            };
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            cbDateFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(130, 28),
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 1, 8, 0)
            };
            cbDateFilter.Items.AddRange(new object[] { "All Dates", "Today", "Last 7 Days", "This Month", "Last 30 Days" });
            cbDateFilter.SelectedIndex = 0;
            cbDateFilter.SelectedIndexChanged += (s, e) => ApplyFilter();

            var btnClearFilter = new Button
            {
                Text = "Reset",
                Font = new Font("Segoe UI", 8.5F),
                Size = new Size(68, 28),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 8, 0)
            };
            btnClearFilter.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnClearFilter.Click += (s, e) =>
            {
                txtSearch.Clear();
                cbDateFilter.SelectedIndex = 0;
            };

            flowFilter.Controls.Add(txtSearch);
            flowFilter.Controls.Add(cbDateFilter);
            flowFilter.Controls.Add(btnClearFilter);

            pnlSearchRow.Controls.Add(flowFilter);

            var flowActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 4, 0, 0)
            };

            var btnCreateInvoice = new Button
            {
                Text = "🧾 Create Billing Invoice",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(175, 29),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnCreateInvoice.FlatAppearance.BorderSize = 0;
            btnCreateInvoice.Click += (s, e) =>
            {
                if (gridCompleted.SelectedRows.Count > 0 && gridCompleted.SelectedRows[0].DataBoundItem is Delivery del)
                {
                    OnNavigateToCreateInvoiceRequested?.Invoke(del);
                }
                else
                {
                    MessageBox.Show("Please select a completed delivery from the list to create its billing invoice.", "Select Delivery", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            var btnViewDetails = new Button
            {
                Text = "🔍 View Schedule",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(130, 29),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnViewDetails.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnViewDetails.Click += (s, e) => ShowSelectedDetails();

            var btnPrintTripReport = new Button
            {
                Text = "📋 Print Trip Report",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(145, 29),
                BackColor = Color.FromArgb(30, 58, 138),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnPrintTripReport.FlatAppearance.BorderSize = 0;
            btnPrintTripReport.Click += (s, e) => PrintSelectedTripReport();

            var btnPrintDossier = new Button
            {
                Text = "🖨️ Print Dossier",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(130, 29),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnPrintDossier.FlatAppearance.BorderSize = 0;
            btnPrintDossier.Click += (s, e) => PrintSelectedDossier();

            var btnIncidents = new Button
            {
                Text = "⚠️ Incident Log",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(125, 29),
                BackColor = Color.FromArgb(254, 242, 242),
                ForeColor = Color.FromArgb(185, 28, 28),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnIncidents.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnIncidents.Click += (s, e) => NavigateToIncident();

            var btnTripReport = new Button
            {
                Text = "📝 View Trip Report",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(140, 29),
                BackColor = Color.FromArgb(238, 242, 255),
                ForeColor = Color.FromArgb(67, 56, 202),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 0)
            };
            btnTripReport.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
            btnTripReport.Click += (s, e) => NavigateToTripReport();

            flowActions.Controls.Add(btnCreateInvoice);
            flowActions.Controls.Add(btnViewDetails);
            flowActions.Controls.Add(btnPrintTripReport);
            flowActions.Controls.Add(btnPrintDossier);
            flowActions.Controls.Add(btnIncidents);
            flowActions.Controls.Add(btnTripReport);

            pnlToolbar.Controls.Add(flowActions);
            pnlToolbar.Controls.Add(pnlSearchRow);"""

assert old_kpis_and_toolbar in text, "old_kpis_and_toolbar not found"
text = text.replace(old_kpis_and_toolbar, new_kpis_and_toolbar)

old_apply_filter = """            lblTotalTrips.Text = filteredDeliveries.Count.ToString("N0");
            lblTotalCrates.Text = filteredDeliveries.Sum(d => d.TotalCrates).ToString("N0");
            lblTotalGCrates.Text = filteredDeliveries.Sum(d => d.GCrate).ToString("N0");
            lblTotalECrates.Text = filteredDeliveries.Sum(d => d.ECrate).ToString("N0");
            lblGridCount.Text = $"Showing {filteredDeliveries.Count:N0} of {allCompleted.Count:N0} completed dispatches";"""

new_apply_filter = """            lblTotalTrips.Text = filteredDeliveries.Count.ToString("N0");
            lblGridCount.Text = $"Showing {filteredDeliveries.Count:N0} of {allCompleted.Count:N0} completed dispatches";"""

assert old_apply_filter in text, "old_apply_filter not found"
text = text.replace(old_apply_filter, new_apply_filter)

with open(view_path, "w", encoding="utf-8") as f:
    f.write(text)

print("CompletedDispatchesView.cs updated successfully.")
