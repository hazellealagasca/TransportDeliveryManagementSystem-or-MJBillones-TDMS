import os

dispatch_path = r"Views/DispatchManagementView.cs"
with open(dispatch_path, "r", encoding="utf-8") as f:
    disp = f.read()

assert 'lblDest.Text = "Delivery Route (Select R1 / R2 or Type Route Code):";' in disp, "Route label target not found"
disp = disp.replace('lblDest.Text = "Delivery Route (Select R1 / R2 or Type Route Code):";', 'lblDest.Text = "Delivery Route:";')

old_step_banner = """        private Label CreateStepHeaderBanner(string title)
        {
            return new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 26,
                BackColor = Color.FromArgb(239, 246, 255),
                ForeColor = Color.FromArgb(30, 58, 138),
                Font = new Font("Segoe UI Semibold", 8.75F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Margin = new Padding(0, 6, 0, 4)
            };
        }"""

new_step_banner = """        private Label CreateStepHeaderBanner(string title)
        {
            return new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 22,
                BackColor = Color.FromArgb(239, 246, 255),
                ForeColor = Color.FromArgb(30, 58, 138),
                Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                Margin = new Padding(0, 3, 0, 2)
            };
        }"""
assert old_step_banner in disp, "old_step_banner not found"
disp = disp.replace(old_step_banner, new_step_banner)

old_stores_panel = """            pnlRouteStoresBreakdown = new Panel
            {
                Dock = DockStyle.Top,
                Height = 280,
                Visible = true,
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6, 6, 6, 6),
                Margin = new Padding(0, 4, 0, 8)
            };"""

new_stores_panel = """            pnlRouteStoresBreakdown = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                Visible = true,
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6, 6, 6, 6),
                Margin = new Padding(0, 2, 0, 4)
            };"""
assert old_stores_panel in disp, "old_stores_panel not found"
disp = disp.replace(old_stores_panel, new_stores_panel)

old_route_btn = """            btnAddRoute = new Button
            {
                Text = "+",
                Dock = DockStyle.Right,
                Width = 38,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Bold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };"""
new_route_btn = """            btnAddRoute = new Button
            {
                Text = "+",
                Dock = DockStyle.Right,
                Width = 36,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Bold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };"""
assert old_route_btn in disp, "old_route_btn not found"
disp = disp.replace(old_route_btn, new_route_btn)

old_route_containers = """            Panel pnlRouteContainer = new Panel { Dock = DockStyle.Fill, Height = 28 };
            cbDestination.Dock = DockStyle.Fill;
            pnlRouteContainer.Controls.Add(cbDestination);
            pnlRouteContainer.Controls.Add(btnAddRoute);

            Panel pnlCargoContainer = new Panel { Dock = DockStyle.Fill, Height = 28 };"""

new_route_containers = """            Panel pnlRouteContainer = new Panel { Dock = DockStyle.Fill };
            cbDestination.Dock = DockStyle.Fill;
            pnlRouteContainer.Controls.Add(cbDestination);
            pnlRouteContainer.Controls.Add(btnAddRoute);

            Panel pnlCargoContainer = new Panel { Dock = DockStyle.Fill };"""
assert old_route_containers in disp, "old_route_containers not found"
disp = disp.replace(old_route_containers, new_route_containers)

old_two_col_row = """        private TableLayoutPanel CreateTwoColumnRow(Control lbl1, Control ctrl1, Control lbl2, Control ctrl2)
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
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));"""

new_two_col_row = """        private TableLayoutPanel CreateTwoColumnRow(Control lbl1, Control ctrl1, Control lbl2, Control ctrl2)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 3),
                Padding = new Padding(0)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));"""
assert old_two_col_row in disp, "old_two_col_row not found"
disp = disp.replace(old_two_col_row, new_two_col_row)

old_theme_overrides = """            flowAidesModule.Height = 110;

            if (chkMultiDrop != null)

            {

                chkMultiDrop.Padding = new Padding(0, 1, 0, 1);

            }

            pnlAidesSearchRow.Height = 26;

            btnClearAides.Height = 26;

            btnPickAnalogTime.Height = 26;"""

new_theme_overrides = """            flowAidesModule.Height = 66;

            if (chkMultiDrop != null)

            {

                chkMultiDrop.Padding = new Padding(0, 1, 0, 1);

            }

            pnlAidesSearchRow.Height = 28;

            btnClearAides.Height = 28;

            btnPickAnalogTime.Height = 28;"""
assert old_theme_overrides in disp, "old_theme_overrides not found"
disp = disp.replace(old_theme_overrides, new_theme_overrides)

old_grid_clamp1 = "pnlRouteStoresBreakdown.Height = Math.Clamp(62 + savedStops.Count * 28, 260, 420);"
new_grid_clamp1 = "pnlRouteStoresBreakdown.Height = Math.Clamp(48 + savedStops.Count * 25, 96, 175);"
assert old_grid_clamp1 in disp, "old_grid_clamp1 not found"
disp = disp.replace(old_grid_clamp1, new_grid_clamp1)

old_grid_clamp2 = "pnlRouteStoresBreakdown.Height = Math.Clamp(62 + cnt * 28, 260, 420);"
new_grid_clamp2 = "pnlRouteStoresBreakdown.Height = Math.Clamp(48 + cnt * 25, 96, 175);"
assert old_grid_clamp2 in disp, "old_grid_clamp2 not found"
disp = disp.replace(old_grid_clamp2, new_grid_clamp2)

with open(dispatch_path, "w", encoding="utf-8") as f:
    f.write(disp)

print("DispatchManagementView.cs updated successfully.")

loading_path = r"Views/LoadingManagementView.cs"
with open(loading_path, "r", encoding="utf-8") as f:
    load = f.read()

old_load_dest = "Panel pnlDestContainer = new Panel { Dock = DockStyle.Fill, Height = 28 };"
new_load_dest = "Panel pnlDestContainer = new Panel { Dock = DockStyle.Fill };"
assert old_load_dest in load, "old_load_dest not found"
load = load.replace(old_load_dest, new_load_dest)

old_load_two_col = """        private TableLayoutPanel CreateTwoColumnRow(Control lbl1, Control ctrl1, Control lbl2, Control ctrl2)
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
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));"""

new_load_two_col = """        private TableLayoutPanel CreateTwoColumnRow(Control lbl1, Control ctrl1, Control lbl2, Control ctrl2)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 3),
                Padding = new Padding(0)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));"""
assert old_load_two_col in load, "old_load_two_col not found"
load = load.replace(old_load_two_col, new_load_two_col)

old_load_clamp1 = "pnlRouteStoresBreakdown.Height = Math.Min(230, 38 + savedStops.Count * 25);"
new_load_clamp1 = "pnlRouteStoresBreakdown.Height = Math.Clamp(38 + savedStops.Count * 25, 88, 175);"
assert old_load_clamp1 in load, "old_load_clamp1 not found"
load = load.replace(old_load_clamp1, new_load_clamp1)

old_load_clamp2 = "pnlRouteStoresBreakdown.Height = Math.Min(230, 38 + cnt * 25);"
new_load_clamp2 = "pnlRouteStoresBreakdown.Height = Math.Clamp(38 + cnt * 25, 88, 175);"
assert old_load_clamp2 in load, "old_load_clamp2 not found"
load = load.replace(old_load_clamp2, new_load_clamp2)

with open(loading_path, "w", encoding="utf-8") as f:
    f.write(load)

print("LoadingManagementView.cs updated successfully.")
