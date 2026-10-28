import re

path = r'MainForm.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Add fields
fields_target = """        private System.Windows.Forms.Timer? tmrAutoTransitCheck;"""
fields_replacement = """        private System.Windows.Forms.Timer? tmrAutoTransitCheck;
        private Panel? pnlNavFlyout;
        private System.Windows.Forms.Timer? tmrNavFlyoutGrace;
        private Button? currentHoveredNavBtn;"""

assert fields_target in content, "fields_target not found"
content = content.replace(fields_target, fields_replacement, 1)

# 2. Add SetupNavFlyoutMenus() in constructor
ctor_target = """            SetupSidebarHover();
            SetupCollapsibleSidebar();"""
ctor_replacement = """            SetupSidebarHover();
            SetupNavFlyoutMenus();
            SetupCollapsibleSidebar();"""

assert ctor_target in content, "ctor_target not found"
content = content.replace(ctor_target, ctor_replacement, 1)

# 3. Add HideNavFlyout to panelMainContent.MouseDown and panelHeader.MouseDown
mouse_target = """            panelMainContent.MouseDown += (s, e) => { if (pnlNotificationMenu != null) pnlNotificationMenu.Visible = false; if (lstSearchDropdown != null) lstSearchDropdown.Visible = false; };"""
mouse_replacement = """            panelMainContent.MouseDown += (s, e) => { if (pnlNotificationMenu != null) pnlNotificationMenu.Visible = false; if (lstSearchDropdown != null) lstSearchDropdown.Visible = false; HideNavFlyout(); };
            panelHeader.MouseDown += (s, e) => { HideNavFlyout(); };"""

assert mouse_target in content, "mouse_target not found"
content = content.replace(mouse_target, mouse_replacement, 1)

# 4. Add HideNavFlyout to TmrSidebarAnimation_Tick
anim_target = """        private void TmrSidebarAnimation_Tick(object? sender, EventArgs e)
        {"""
anim_replacement = """        private void TmrSidebarAnimation_Tick(object? sender, EventArgs e)
        {
            HideNavFlyout();"""

assert anim_target in content, "anim_target not found"
content = content.replace(anim_target, anim_replacement, 1)

# 5. Clear tooltips on the 4 hub buttons when collapsed so they don't overlap flyouts
tt_target = """                ttSidebar.SetToolTip(btnNavDashboard, "Dashboard");
                ttSidebar.SetToolTip(btnNavOperations, "Dispatch & Delivery Operations");
                ttSidebar.SetToolTip(btnNavResources, "Trucks & Personnel Resources");
                ttSidebar.SetToolTip(btnNavFinance, "Billing, Invoices & Payment Collections");
                ttSidebar.SetToolTip(btnNavMasterData, "Rate & Business Master Data");
                ttSidebar.SetToolTip(btnNavSettings, "System Administration & Settings");"""

tt_replacement = """                ttSidebar.SetToolTip(btnNavDashboard, "Dashboard");
                ttSidebar.SetToolTip(btnNavOperations, null);
                ttSidebar.SetToolTip(btnNavResources, null);
                ttSidebar.SetToolTip(btnNavFinance, null);
                ttSidebar.SetToolTip(btnNavMasterData, null);
                ttSidebar.SetToolTip(btnNavSettings, "System Administration & Settings");"""

assert tt_target in content, "tt_target not found"
content = content.replace(tt_target, tt_replacement, 1)

# 6. Add HideNavFlyout to NavigateToView
navview_target = """            if (pnlNotificationMenu != null) pnlNotificationMenu.Visible = false;
            if (lstSearchDropdown != null) lstSearchDropdown.Visible = false;"""

navview_replacement = """            if (pnlNotificationMenu != null) pnlNotificationMenu.Visible = false;
            if (lstSearchDropdown != null) lstSearchDropdown.Visible = false;
            HideNavFlyout();"""

assert navview_target in content, "navview_target not found"
content = content.replace(navview_target, navview_replacement, 1)

# 7. Add SetupNavFlyoutMenus, StartFlyoutGrace, HideNavFlyout, ShowNavFlyout right after SetupSidebarHover
hover_target = """        private void SetupSidebarHover()
        {
            foreach (Control c in panelSidebar.Controls)
            {
                if (c is Button navBtn)
                {
                    navBtn.BackColor = Color.FromArgb(15, 23, 42);
                    navBtn.ForeColor = Color.FromArgb(148, 163, 184);
                    navBtn.Cursor = Cursors.Hand;
                    navBtn.MouseEnter += (s, e) =>
                    {
                        if (navBtn != activeButton)
                        {
                            navBtn.BackColor = Color.FromArgb(30, 41, 59);
                            navBtn.ForeColor = Color.White;
                        }
                    };
                    navBtn.MouseLeave += (s, e) =>
                    {
                        if (navBtn != activeButton)
                        {
                            navBtn.BackColor = Color.FromArgb(15, 23, 42);
                            navBtn.ForeColor = Color.FromArgb(148, 163, 184);
                        }
                    };
                }
            }
        }"""

flyout_code = """        private void SetupSidebarHover()
        {
            foreach (Control c in panelSidebar.Controls)
            {
                if (c is Button navBtn)
                {
                    navBtn.BackColor = Color.FromArgb(15, 23, 42);
                    navBtn.ForeColor = Color.FromArgb(148, 163, 184);
                    navBtn.Cursor = Cursors.Hand;
                    navBtn.MouseEnter += (s, e) =>
                    {
                        if (navBtn != activeButton)
                        {
                            navBtn.BackColor = Color.FromArgb(30, 41, 59);
                            navBtn.ForeColor = Color.White;
                        }
                    };
                    navBtn.MouseLeave += (s, e) =>
                    {
                        if (navBtn != activeButton)
                        {
                            navBtn.BackColor = Color.FromArgb(15, 23, 42);
                            navBtn.ForeColor = Color.FromArgb(148, 163, 184);
                        }
                    };
                }
            }
        }

        private void SetupNavFlyoutMenus()
        {
            pnlNavFlyout = new Panel
            {
                Size = new Size(240, 200),
                BackColor = Color.FromArgb(30, 41, 59),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            this.Controls.Add(pnlNavFlyout);
            pnlNavFlyout.BringToFront();

            tmrNavFlyoutGrace = new System.Windows.Forms.Timer { Interval = 250 };
            tmrNavFlyoutGrace.Tick += (s, e) =>
            {
                Point cursorClient = this.PointToClient(Cursor.Position);
                bool insideFlyout = pnlNavFlyout != null && pnlNavFlyout.Visible && pnlNavFlyout.Bounds.Contains(cursorClient);
                bool insideBtn = currentHoveredNavBtn != null && currentHoveredNavBtn.RectangleToScreen(currentHoveredNavBtn.ClientRectangle).Contains(Cursor.Position);
                if (!insideFlyout && !insideBtn)
                {
                    HideNavFlyout();
                }
            };

            pnlNavFlyout.MouseEnter += (s, e) => tmrNavFlyoutGrace.Stop();
            pnlNavFlyout.MouseLeave += (s, e) => StartFlyoutGrace();

            var hubFlyoutConfigs = new (Button Button, string Title, (string Label, Action Action)[] Items)[]
            {
                (
                    btnNavOperations,
                    "🚚  OPERATIONS",
                    new (string, Action)[]
                    {
                        ("🚚  Loading Schedule & Dispatch", () => NavigateToOperations("Dispatch")),
                        ("✅  Completed Dispatches", () => NavigateToOperations("Completed")),
                        ("⚠️  Incident & Damage Logs", () => NavigateToOperations("Incidents")),
                        ("📝  Trip Reports", () => NavigateToOperations("TripReports")),
                        ("📜  Order History", () => NavigateToOperations("History"))
                    }
                ),
                (
                    btnNavResources,
                    "👥  RESOURCES",
                    new (string, Action)[]
                    {
                        ("🚛  Trucks", () => NavigateToResources("Trucks")),
                        ("👨‍✈️  Crew Management", () => NavigateToResources("Crew")),
                        ("📋  Crew Trip History", () => NavigateToResources("CrewHistory")),
                        ("📜  Resource History", () => NavigateToResources("History"))
                    }
                ),
                (
                    btnNavFinance,
                    "💰  PAYMENTS",
                    new (string, Action)[]
                    {
                        ("📑  Statement of Account", () => NavigateToFinance("StatementOfAccount")),
                        ("🧾  Billing Invoices", () => NavigateToFinance("Billing")),
                        ("💵  Collections & Receipts", () => NavigateToFinance("Collections")),
                        ("📜  Finance History", () => NavigateToFinance("History"))
                    }
                ),
                (
                    btnNavMasterData,
                    "🏷️  MASTER DATA",
                    new (string, Action)[]
                    {
                        ("🏢  Categories & Clients", () => NavigateToMasterData("Categories")),
                        ("📜  Master Data History", () => NavigateToMasterData("History"))
                    }
                )
            };

            foreach (var cfg in hubFlyoutConfigs)
            {
                var targetBtn = cfg.Button;
                var targetTitle = cfg.Title;
                var targetItems = cfg.Items;

                targetBtn.MouseEnter += (s, e) =>
                {
                    currentHoveredNavBtn = targetBtn;
                    tmrNavFlyoutGrace.Stop();
                    ShowNavFlyout(targetBtn, targetTitle, targetItems);
                };

                targetBtn.MouseLeave += (s, e) =>
                {
                    StartFlyoutGrace();
                };

                targetBtn.Click += (s, e) =>
                {
                    HideNavFlyout();
                };
            }
        }

        private void StartFlyoutGrace()
        {
            tmrNavFlyoutGrace?.Stop();
            tmrNavFlyoutGrace?.Start();
        }

        private void HideNavFlyout()
        {
            tmrNavFlyoutGrace?.Stop();
            if (pnlNavFlyout != null)
            {
                pnlNavFlyout.Visible = false;
            }
            currentHoveredNavBtn = null;
        }

        private void ShowNavFlyout(Button navBtn, string title, (string Label, Action Action)[] items)
        {
            if (pnlNavFlyout == null) return;

            pnlNavFlyout.SuspendLayout();
            pnlNavFlyout.Controls.Clear();

            int itemHeight = 36;
            int headerHeight = 26;
            int totalHeight = headerHeight + (items.Length * itemHeight) + 4;
            int flyoutWidth = 246;

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = headerHeight,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(12, 4, 8, 4)
            };

            Label lblHeader = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblHeader);
            pnlHeader.MouseEnter += (s, e) => tmrNavFlyoutGrace?.Stop();
            pnlHeader.MouseLeave += (s, e) => StartFlyoutGrace();

            Panel pnlItemsContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 41, 59)
            };
            pnlItemsContainer.MouseEnter += (s, e) => tmrNavFlyoutGrace?.Stop();
            pnlItemsContainer.MouseLeave += (s, e) => StartFlyoutGrace();

            for (int i = items.Length - 1; i >= 0; i--)
            {
                var itm = items[i];
                Button btnItm = new Button
                {
                    Text = itm.Label,
                    Dock = DockStyle.Top,
                    Height = itemHeight,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(30, 41, 59),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(12, 0, 8, 0),
                    Cursor = Cursors.Hand,
                    Margin = Padding.Empty
                };
                btnItm.FlatAppearance.BorderSize = 0;
                btnItm.MouseEnter += (s, e) =>
                {
                    tmrNavFlyoutGrace?.Stop();
                    btnItm.BackColor = Color.FromArgb(51, 65, 85);
                };
                btnItm.MouseLeave += (s, e) =>
                {
                    btnItm.BackColor = Color.FromArgb(30, 41, 59);
                    StartFlyoutGrace();
                };
                btnItm.Click += (s, e) =>
                {
                    HideNavFlyout();
                    itm.Action();
                };
                pnlItemsContainer.Controls.Add(btnItm);
            }

            pnlNavFlyout.Controls.Add(pnlItemsContainer);
            pnlNavFlyout.Controls.Add(pnlHeader);

            Point btnScreenPt = navBtn.PointToScreen(Point.Empty);
            Point clientPt = this.PointToClient(btnScreenPt);
            int targetX = panelSidebar.Width;
            int targetY = clientPt.Y;

            if (targetY + totalHeight > this.ClientSize.Height - 10)
            {
                targetY = Math.Max(panelHeader.Bottom + 4, this.ClientSize.Height - totalHeight - 10);
            }

            pnlNavFlyout.Location = new Point(targetX, targetY);
            pnlNavFlyout.Size = new Size(flyoutWidth, totalHeight);
            pnlNavFlyout.ResumeLayout(true);
            pnlNavFlyout.BringToFront();
            pnlNavFlyout.Visible = true;
        }"""

assert hover_target in content, "hover_target not found"
content = content.replace(hover_target, flyout_code, 1)

# 8. Add NavigateToResources, NavigateToFinance, NavigateToMasterData helpers
navhelpers_target = """        public void NavigateToOperations(string tab = "Dispatch")
        {
            try
            {
                OperationsHubView.SuppressUnsavedPrompt = true;
                btnNavOperations_Click(this, EventArgs.Empty);
                if (viewCache.TryGetValue("OperationsHubView", out var ov) && ov is OperationsHubView ohv)
                {
                    ohv.SelectTab(tab);
                }
            }
            finally
            {
                OperationsHubView.SuppressUnsavedPrompt = false;
            }
        }"""

navhelpers_replacement = """        public void NavigateToOperations(string tab = "Dispatch")
        {
            try
            {
                OperationsHubView.SuppressUnsavedPrompt = true;
                btnNavOperations_Click(this, EventArgs.Empty);
                if (viewCache.TryGetValue("OperationsHubView", out var ov) && ov is OperationsHubView ohv)
                {
                    ohv.SelectTab(tab);
                }
            }
            finally
            {
                OperationsHubView.SuppressUnsavedPrompt = false;
            }
        }

        public void NavigateToResources(string tab = "Trucks")
        {
            btnNavResources_Click(this, EventArgs.Empty);
            if (viewCache.TryGetValue("ResourcesHubView", out var rv) && rv is ResourcesHubView rhv)
            {
                rhv.SelectTab(tab);
            }
        }

        public void NavigateToFinance(string tab = "StatementOfAccount")
        {
            btnNavFinance_Click(this, EventArgs.Empty);
            if (viewCache.TryGetValue("FinanceHubView", out var fv) && fv is FinanceHubView fhv)
            {
                fhv.SelectTab(tab);
            }
        }

        public void NavigateToMasterData(string tab = "Categories")
        {
            btnNavMasterData_Click(this, EventArgs.Empty);
            if (viewCache.TryGetValue("MasterDataHubView", out var mv) && mv is MasterDataHubView mdv)
            {
                mdv.SelectTab(tab);
            }
        }"""

assert navhelpers_target in content, "navhelpers_target not found"
content = content.replace(navhelpers_target, navhelpers_replacement, 1)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print("SUCCESS: MainForm.cs updated with sidebar hover flyout menus!")
