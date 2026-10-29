using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    public partial class MainForm : Form
    {
        public static MainForm? Instance { get; private set; }

        /// <summary>True when the window is closing because the user chose Logout (the login screen is shown again instead of exiting).</summary>
        public bool IsLoggingOut { get; private set; }
        private Action? categoriesChangedHandler;
        private Action? notificationsChangedHandler;
        private const int SidebarExpandedWidth = 236;
        private const int SidebarCollapsedWidth = 68;
        private bool isSidebarCollapsed = false;
        private bool autoCollapsedForDrawer = false;

        private int lastInTransitCount = 0;
        private int lastPendingCount = 0;
        private int lastUnpaidCount = 0;

        private User currentUser;
        private Button? activeButton;
        private Dictionary<string, UserControl> viewCache = new Dictionary<string, UserControl>();

        private FlowLayoutPanel? pnlKpiFlow;
        private Button btnKpiScheduled = null!;
        private Button btnKpiInTransit = null!;
        private Button btnKpiUnpaid = null!;

        private Panel? pnlNotificationMenu;
        private System.Windows.Forms.Timer? tmrRollingSnapshot;
        private System.Windows.Forms.Timer? tmrOperationalAlerts;
        private System.Windows.Forms.Timer? tmrAutoTransitCheck;
        private Panel? pnlNavFlyout;
        private System.Windows.Forms.Timer? tmrNavFlyoutGrace;
        private Button? currentHoveredNavBtn;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public MainForm(User user) : this(user, false)
        {
        }
        public MainForm(User user, bool isThemeReload)
        {
            Instance = this;
            InitializeComponent();
            DarkModeKit.Install();
            UIThemeHelper.EnableDoubleBuffering(this);
            currentUser = user;
            SessionContext.ActiveUser = user;
            this.Resize += (s, e) => ApplyAdaptiveHeader();

            string cleanName = (currentUser.FullName ?? "").Replace(" (Owner)", "").Replace(" (Admin)", "").Trim();
            string roleBadge = currentUser.Role?.ToUpperInvariant() ?? "STAFF";
            string roleLabel = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase((currentUser.Role ?? "Staff").ToLowerInvariant());
            lblUserInfo.Text = $"{cleanName}  ·  {roleLabel}";
            lblUserInfo.AutoSize = true;
            lblUserInfo.Cursor = Cursors.Hand;
            lblUserInfo.Click += (s, e) => ShowUserProfileDialog();

            pnlSearchCenter.Visible = false;
            pnlSearchCenter.Resize += (s, e) => RepositionSearchBar();
            RepositionSearchBar();

            txtGlobalSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    btnGlobalSearch_Click(this, EventArgs.Empty);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            try
            {
                string[] possiblePaths = new[]
                {
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png"),
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_orig.png"),
                    System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Assets", "logo.png"),
                    System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Assets", "logo_orig.png")
                };

                foreach (var path in possiblePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        using (var stream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                                picLogo.Image = new Bitmap(Image.FromStream(stream));
                        }
                        lblLogoTitle.Visible = false;
                        lblLogoSubtitle.Visible = false;
                        using (Bitmap bmp = new Bitmap(path))
                        {
                            IntPtr hIcon = bmp.GetHicon();
                            using (Icon tempIcon = Icon.FromHandle(hIcon))
                            {
                                this.Icon = (Icon)tempIcon.Clone();
                            }
                            DestroyIcon(hIcon);
                        }
                        break;
                    }
                }
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainForm] LoadLogo: {ex.Message}"); ErrLog.Swallowed(ex); }
            if (!isThemeReload) MJdatabase.PerformStartupSanityCheck();
            categoriesChangedHandler = () =>
            {
                if (this.IsDisposed || !this.IsHandleCreated) return;
                if (this.InvokeRequired) this.BeginInvoke(new Action(RefreshCurrentActiveView));
                else RefreshCurrentActiveView();
            };
            MJdatabase.OnCategoriesChanged += categoriesChangedHandler;
            this.FormClosing += (s, e) => { if (!IsReloading) MJdatabase.PerformAutoBackup(); tmrRollingSnapshot?.Stop(); tmrOperationalAlerts?.Stop(); tmrAutoTransitCheck?.Stop(); };
            this.FormClosed += (s, e) =>
            {
                if (categoriesChangedHandler != null) MJdatabase.OnCategoriesChanged -= categoriesChangedHandler;
                if (notificationsChangedHandler != null) EventNotificationService.OnNotificationsChanged -= notificationsChangedHandler;
                tmrRollingSnapshot?.Dispose();
                tmrOperationalAlerts?.Dispose();
                tmrAutoTransitCheck?.Dispose();
                foreach (var cached in viewCache.Values)
                {
                    try { if (!cached.IsDisposed) cached.Dispose(); } catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainForm] Dispose view: {ex.Message}"); ErrLog.Swallowed(ex); }
                }
                viewCache.Clear();
                if (Instance == this) Instance = null;
                if (!IsLoggingOut && !IsReloading)
                {
                    Application.Exit();
                }
            };
            SetupRollingSnapshotTimer();
            SetupOperationalAlertsTimer();
            SetupAutoTransitCheckTimer();

            SetupAccessibilityAndKpis();
            SetupSidebarHover();
            SetupNavFlyoutMenus();
            SetupCollapsibleSidebar();
            SetupNotifications();
            InstallPopoverGuards();
            ApplyShellTheme();
            StyleHeaderChips();
            UpdateNotificationUI();
            UpdateSidebarDisplay(isSidebarCollapsed);
            SetupAutoHideSidebar();
            ApplyUserRolePermissions();
            InitHeaderRedesign();
            panelMainContent.MouseDown += (s, e) => { if (pnlNotificationMenu != null) pnlNotificationMenu.Visible = false; if (lstSearchDropdown != null) lstSearchDropdown.Visible = false; HideNavFlyout(); };
            panelHeader.MouseDown += (s, e) => { HideNavFlyout(); };
            OpenDashboard();
            if (!isThemeReload) EventNotificationService.Publish("Session Active", $"Authenticated as @{currentUser.Username} ({currentUser.Role})", ToastType.Info, this);
            this.Shown += (s, e) =>
            {
                if (isThemeReload) return;
                CheckMandatorySecurityQuestion();
                Task.Run(() => EventNotificationService.ScanOperationalAlerts());
            };
        }

        private void CheckMandatorySecurityQuestion()
        {
            try
            {
                if (currentUser == null) return;

                if (string.IsNullOrWhiteSpace(currentUser.SecurityQuestion) || string.IsNullOrWhiteSpace(currentUser.SecurityAnswer))
                {
                    var (dbQ, dbA) = MJdatabase.GetUserSecurityCredentials(currentUser.Id);
                    if (!string.IsNullOrWhiteSpace(dbQ))
                        currentUser.SecurityQuestion = dbQ;
                    if (!string.IsNullOrWhiteSpace(dbA))
                        currentUser.SecurityAnswer = dbA;
                }

                if (string.IsNullOrWhiteSpace(currentUser.SecurityQuestion) || string.IsNullOrWhiteSpace(currentUser.SecurityAnswer))
                {
                    MessageBox.Show(
                        "SECURITY SETUP REQUIRED:\n\n" +
                        "Your account does not have a secret security recovery question configured.\n\n" +
                        "Please configure your security question now so you can easily reset your password if you ever forget it.",
                        "Account Security Protection",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    using (var secDlg = new SecuritySettingsDialog(currentUser))
                    {
                        secDlg.ShowDialog(this);
                    }
                }
            }
            catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
        }

        private void ShowUserProfileDialog()
        {
            var dlg = new UserProfileDialog(currentUser);
            dlg.OnNavigateToUserManagement = () => btnNavUsers_Click(this, EventArgs.Empty);
            dlg.OnLogout = () => btnLogout_Click(this, EventArgs.Empty);
            dlg.ShowDialog(this);
        }

        private void SetupCollapsibleSidebar()
        {
            tmrSidebarAnimation.Interval = 10;
            tmrSidebarAnimation.Tick += TmrSidebarAnimation_Tick;
            // The menu starts open; once the mouse leaves it, it tucks away to a slim icon rail (see MainForm.Sidebar.cs).
            isSidebarCollapsed = false;
            panelSidebar.Width = S(SidebarExpandedWidth);
            UpdateSidebarDisplay(false);
        }

        private void btnToggleSidebar_Click(object? sender, EventArgs e)
        {
            TogglePinSidebar();
        }

        public void ToggleSidebar()
        {
            isSidebarCollapsed = !isSidebarCollapsed;
            tmrSidebarAnimation.Start();
        }

        public void SetSidebarCollapsed(bool collapsed)
        {
            if (isSidebarCollapsed == collapsed) return;
            isSidebarCollapsed = collapsed;
            tmrSidebarAnimation.Start();
        }

        public void NotifyDrawerToggled(bool isDrawerOpen)
        {
            if (this.ClientSize.Width <= 1366)
            {
                if (isDrawerOpen && !isSidebarCollapsed)
                {
                    autoCollapsedForDrawer = true;
                    SetSidebarCollapsed(true);
                }
                else if (!isDrawerOpen && autoCollapsedForDrawer)
                {
                    autoCollapsedForDrawer = false;
                    SetSidebarCollapsed(false);
                }
            }
        }

        private void TmrSidebarAnimation_Tick(object? sender, EventArgs e)
        {
            HideNavFlyout();
            int target = isSidebarCollapsed ? S(SidebarCollapsedWidth) : S(SidebarExpandedWidth);
            int step = S(28);
            if (panelSidebar.Width < target)
            {
                panelSidebar.Width = Math.Min(panelSidebar.Width + step, target);
            }
            else if (panelSidebar.Width > target)
            {
                panelSidebar.Width = Math.Max(panelSidebar.Width - step, target);
            }

            if (panelSidebar.Width == target)
            {
                tmrSidebarAnimation.Stop();
                UpdateSidebarDisplay(isSidebarCollapsed);
                SyncSidebarSpacer();
            }
        }

        private void UpdateSidebarDisplay(bool collapsed)
        {
            if (collapsed)
            {
                btnNavDashboard.Text = "📊";
                btnNavOperations.Text = "🚚";
                btnNavResources.Text = "👥";
                btnNavFinance.Text = "💰";
                btnNavMasterData.Text = "🏷️";
                btnNavSettings.Text = "⚙️";

                foreach (Control c in panelSidebar.Controls)
                {
                    if (c is Button b)
                    {
                        b.TextAlign = ContentAlignment.MiddleCenter;
                        b.Padding = new Padding(0);
                    }
                }

                lblLogoTitle.Visible = false;
                lblLogoSubtitle.Visible = false;

                UpdateThemeToggleText(true);
                UpdateSidebarBrand(true);
                ttSidebar.SetToolTip(btnNavDashboard, "Dashboard");
                ttSidebar.SetToolTip(btnNavOperations, null);
                ttSidebar.SetToolTip(btnNavResources, null);
                ttSidebar.SetToolTip(btnNavFinance, null);
                ttSidebar.SetToolTip(btnNavMasterData, null);
                ttSidebar.SetToolTip(btnNavSettings, "System Administration & Settings");
            }
            else
            {
                btnNavDashboard.Text = "📊  Dashboard";
                btnNavOperations.Text = "🚚  Operations";
                btnNavResources.Text = "👥  Resources";
                btnNavFinance.Text = "💰  Payments";
                btnNavMasterData.Text = "🏷️  Master Data";
                btnNavSettings.Text = "⚙️  Settings";

                foreach (Control c in panelSidebar.Controls)
                {
                    if (c is Button b)
                    {
                        b.TextAlign = ContentAlignment.MiddleLeft;
                        b.Padding = new Padding(18, 0, 0, 0);
                    }
                }

                if (picLogo.Image == null)
                {
                    lblLogoTitle.Visible = true;
                    lblLogoSubtitle.Visible = true;
                }
                else
                {
                    lblLogoTitle.Visible = false;
                    lblLogoSubtitle.Visible = false;
                }

                UpdateThemeToggleText(false);
                UpdateSidebarBrand(false);
                ttSidebar.SetToolTip(btnNavDashboard, null);
                ttSidebar.SetToolTip(btnNavSettings, null);
            }
        }

        private void SetupSidebarHover()
        {
            // Hover and active states are painted by the shell (see MainForm.Shell.cs).
        }

        private void SetupNavFlyoutMenus()
        {
            pnlNavFlyout = new Panel
            {
                Size = new Size(240, 200),
                BackColor = AppTheme.Surface,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(1),
                Visible = false
            };
            pnlNavFlyout.Paint += (s, e) => PaintPopoverBorder(pnlNavFlyout, e);
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
                        ("🌟  Special Trip History", () => NavigateToOperations("SpecialTrips")),
                        ("⚠️  Incident & Damage Logs", () => NavigateToOperations("Incidents")),
                        ("📝  Trip Reports", () => NavigateToOperations("TripReports")),
                        ("📜  Order History", () => NavigateToOperations("History")),
                        ("✅  Completed trips (in Order History)", () => NavigateToOperations("Completed"))
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
                        ("🏢  Clients", () => NavigateToMasterData("Clients")),
                        ("🗺️  Delivery Routes", () => NavigateToMasterData("Routes")),
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

            int itemHeight = 38;
            int headerHeight = 36;
            int totalHeight = headerHeight + (items.Length * itemHeight) + 10;
            int flyoutWidth = 260;
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = headerHeight,
                BackColor = AppTheme.Surface,
                Padding = new Padding(16, 10, 8, 2)
            };

            string cleanTitle = title.Contains("  ") ? title.Substring(title.IndexOf("  ", StringComparison.Ordinal)).Trim() : title;
            Label lblHeader = new Label
            {
                Text = cleanTitle,
                Font = new Font("Segoe UI Semibold", 8F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblHeader);
            pnlHeader.MouseEnter += (s, e) => tmrNavFlyoutGrace?.Stop();
            pnlHeader.MouseLeave += (s, e) => StartFlyoutGrace();

            Panel pnlItemsContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Surface,
                Padding = new Padding(6, 2, 6, 6)
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
                    BackColor = AppTheme.Surface,
                    ForeColor = AppTheme.TextPrimary,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(4, 0, 8, 0),
                    Cursor = Cursors.Hand,
                    Margin = Padding.Empty
                };
                btnItm.FlatAppearance.BorderSize = 0;
                btnItm.FlatAppearance.MouseOverBackColor = AppTheme.SurfaceHover;
                btnItm.FlatAppearance.MouseDownBackColor = AppTheme.Border;
                UIThemeHelper.StyleButtonChrome(btnItm);
                btnItm.MouseEnter += (s, e) =>
                {
                    tmrNavFlyoutGrace?.Stop();
                };
                btnItm.MouseLeave += (s, e) =>
                {
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
            int targetX = panelSidebar.Width + 6;
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
        }

        private void SetupAccessibilityAndKpis()
        {
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
            CreateHeaderKpiBar();
            UpdateHeaderKpiBadges();
        }

        private void CreateHeaderKpiBar()
        {
            pnlKpiFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Right,
                Padding = new Padding(0, 15, 20, 0),
                BackColor = AppTheme.Surface
            };

            btnKpiScheduled = new Button
            {
                Text = "⏳ 0 Scheduled",
                AutoSize = true,
                Height = 30,
                Margin = new Padding(0, 0, 6, 0),
                BackColor = Pal.C(30, 41, 59),
                ForeColor = Pal.C(147, 197, 253),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnKpiScheduled.FlatAppearance.BorderSize = 0;
            btnKpiScheduled.Click += (s, e) =>
            {
                if (!SessionContext.CanAccessOperations)
                {
                    ToastHelper.ShowToast(this, "Access restricted to Operations staff and Admin.", ToastType.Warning);
                    return;
                }
                btnNavOperations_Click(this, EventArgs.Empty);
                if (viewCache.TryGetValue("OperationsHubView", out var ov) && ov is OperationsHubView ohv)
                {
                    ohv.SelectTab("Dispatch");
                }
            };

            btnKpiInTransit = new Button
            {
                Text = "\U0001f69a 0 In Transit",
                AutoSize = true,
                Height = 30,
                Margin = new Padding(0, 0, 6, 0),
                BackColor = Pal.C(30, 41, 59),
                ForeColor = Pal.C(253, 224, 71),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnKpiInTransit.FlatAppearance.BorderSize = 0;
            btnKpiInTransit.Click += (s, e) =>
            {
                if (!SessionContext.CanAccessOperations)
                {
                    ToastHelper.ShowToast(this, "Access restricted to Operations staff and Admin.", ToastType.Warning);
                    return;
                }
                btnNavOperations_Click(this, EventArgs.Empty);
                if (viewCache.TryGetValue("OperationsHubView", out var ov) && ov is OperationsHubView ohv)
                {
                    ohv.SelectTab("Dispatch");
                }
            };

            btnKpiUnpaid = new Button
            {
                Text = "\U0001f4b3 0 Unpaid",
                AutoSize = true,
                Height = 30,
                Margin = new Padding(0, 0, 8, 0),
                BackColor = Pal.C(30, 41, 59),
                ForeColor = Pal.C(252, 165, 165),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnKpiUnpaid.FlatAppearance.BorderSize = 0;
            btnKpiUnpaid.Click += (s, e) =>
            {
                if (!SessionContext.CanAccessFinance)
                {
                    ToastHelper.ShowToast(this, "Access restricted to Finance staff and Admin.", ToastType.Warning);
                    return;
                }
                btnNavFinance_Click(this, EventArgs.Empty);
                if (viewCache.TryGetValue("FinanceHubView", out var fv) && fv is FinanceHubView fhv)
                {
                    fhv.SelectTab("Billing");
                }
            };

            btnNotificationBell.Text = "\U0001f514 Alerts";
            btnNotificationBell.AutoSize = true;
            btnNotificationBell.Height = 30;
            btnNotificationBell.Margin = new Padding(0, 0, 6, 0);
            btnNotificationBell.BackColor = Pal.C(30, 41, 59);
            btnNotificationBell.ForeColor = Pal.C(226, 232, 240);
            btnNotificationBell.FlatStyle = FlatStyle.Flat;
            btnNotificationBell.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            btnNotificationBell.Cursor = Cursors.Hand;
            btnNotificationBell.FlatAppearance.BorderSize = 0;
            btnNotificationBell.Click += (s, e) => ToggleNotificationMenu();

            btnHeaderSettings.Size = new Size(110, 30);
            btnHeaderSettings.Margin = new Padding(4, 0, 6, 0);
            btnHeaderSettings.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            btnHeaderSettings.Visible = SessionContext.IsAdmin;

            btnLogout.Text = "\U0001f6aa LOGOUT";
            btnLogout.Size = new Size(95, 30);
            btnLogout.Margin = new Padding(4, 0, 0, 0);
            btnLogout.BackColor = Pal.C(185, 28, 28);
            btnLogout.ForeColor = Pal.White;
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            btnLogout.Cursor = Cursors.Hand;
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Visible = true;

            pnlKpiFlow.Controls.Add(btnKpiScheduled);
            pnlKpiFlow.Controls.Add(btnKpiInTransit);
            pnlKpiFlow.Controls.Add(btnKpiUnpaid);
            pnlKpiFlow.Controls.Add(btnNotificationBell);
            pnlKpiFlow.Controls.Add(btnHeaderSettings);
            pnlKpiFlow.Controls.Add(btnLogout);

            panelHeader.Controls.Add(pnlKpiFlow);
            pnlKpiFlow.SendToBack();
            pnlHeaderLeft.SendToBack();
            pnlSearchCenter.BringToFront();

            pnlSearchCenter.Resize += (s, e) => RepositionSearchBar();
        }

        private void SetupNotifications()
        {
            var flyout = new NotificationFlyout(n => OpenNotificationTarget(n));
            pnlNotificationMenu = flyout;
            this.Controls.Add(pnlNotificationMenu);
            pnlNotificationMenu.BringToFront();

            notificationsChangedHandler = () =>
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(UpdateNotificationUI));
                }
            };

            EventNotificationService.OnNotificationsChanged += notificationsChangedHandler;
            UpdateNotificationUI();
        }


        private void SetupRollingSnapshotTimer()
        {
            tmrRollingSnapshot = new System.Windows.Forms.Timer { Interval = 1800000 };
            tmrRollingSnapshot.Tick += (s, e) =>
            {
                try
                {
                    string snap = MJdatabase.PerformTimedRollingBackup(5);
                    if (!string.IsNullOrEmpty(snap))
                    {
                        EventNotificationService.Publish("Database Snapshot", "Automatic background database backup snapshot created.", ToastType.Info, this);
                    }
                }
                catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[Snapshot] {ex.Message}"); ErrLog.Swallowed(ex); }
            };
            tmrRollingSnapshot.Start();
        }

        private void SetupOperationalAlertsTimer()
        {
            tmrOperationalAlerts = new System.Windows.Forms.Timer { Interval = 900000 };
            tmrOperationalAlerts.Tick += (s, e) =>
            {
                Task.Run(() => EventNotificationService.ScanOperationalAlerts());
            };
            tmrOperationalAlerts.Start();
        }

        private void SetupAutoTransitCheckTimer()
        {
            tmrAutoTransitCheck = new System.Windows.Forms.Timer { Interval = 30000 };
            tmrAutoTransitCheck.Tick += (s, e) =>
            {
                try
                {
                    var result = MJdatabase.RunOperationalLifecycleAutomation();
                    bool anyChanged = result.transitStarted > 0 || result.invoicesOverdue > 0 || result.schedulesPastDue > 0;
                    if (anyChanged)
                    {
                        UpdateHeaderKpiBadges();
                        RefreshCurrentActiveView();
                    }
                    if (result.transitStarted > 0)
                    {
                        EventNotificationService.Publish("Dispatches on the road", $"{result.transitStarted} dispatch(es) reached their departure time and are now In Transit.", ToastType.Info, this, "Operations", "Dispatch");
                    }

                    if (result.invoicesOverdue > 0)
                    {
                        EventNotificationService.Publish("Overdue Invoices Alert", $"{result.invoicesOverdue} invoice(s) have lapsed past their payment due date.", ToastType.Warning, this, "Finance", "Invoices");
                    }
                    if (result.schedulesPastDue > 0)
                    {
                        EventNotificationService.Publish("Past Due Schedules", $"{result.schedulesPastDue} pending loading order(s) passed scheduled dates without dispatch.", ToastType.Warning, this, "Operations", "LoadingManagement");
                    }

                    {

                    }
                }
                catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainForm] tmrAutoTransitCheck: {ex.Message}"); ErrLog.Swallowed(ex); }
            };
            tmrAutoTransitCheck.Start();
        }

        private void ToggleNotificationMenu()
        {
            if (pnlNotificationMenu == null) return;

            if (pnlNotificationMenu.Visible)
            {
                pnlNotificationMenu.Visible = false;
                return;
            }

            if (lstSearchDropdown != null) lstSearchDropdown.Visible = false;

            RenderNotificationList();
            Point pt = btnNotificationBell.PointToScreen(new Point(btnNotificationBell.Width - pnlNotificationMenu.Width, btnNotificationBell.Height + 8));
            Point clientPt = this.PointToClient(pt);
            clientPt.X = Math.Max(4, clientPt.X);
            pnlNotificationMenu.Location = clientPt;
            pnlNotificationMenu.BringToFront();
            pnlNotificationMenu.Visible = true;
        }

        private void UpdateNotificationUI()
        {
            int unread = EventNotificationService.GetBusinessProcessUnreadCount();
            btnNotificationBell.Text = unread > 0 ? $"🔔  {unread} new" : "🔔";
            UpdateBellChip(unread);
            if (pnlNotificationMenu != null && pnlNotificationMenu.Visible)
            {
                RenderNotificationList();
            }
        }

        private void RenderNotificationList()
        {
            (pnlNotificationMenu as NotificationFlyout)?.Render();
        }

        public void UpdateHeaderKpiBadges()
        {
            try
            {
                var deliveries = MJdatabase.GetDeliveries();
                lastInTransitCount = deliveries.Count(d => d.Status == "In Transit");
                lastPendingCount = deliveries.Count(d => d.Status == "Pending" || d.Status == "Scheduled");

                var invoices = MJdatabase.GetInvoices();
                lastUnpaidCount = invoices.Count(i => i.Status == "Unpaid" || i.Status == "Partial");

                if (btnKpiScheduled != null)
                {
                    btnKpiScheduled.Text = $"⏳ {lastPendingCount} Scheduled";
                }
                if (btnKpiInTransit != null)
                {
                    btnKpiInTransit.Text = $"\U0001f69a {lastInTransitCount} In Transit";
                }
                if (btnKpiUnpaid != null)
                {
                    btnKpiUnpaid.Text = $"\U0001f4b3 {lastUnpaidCount} Unpaid";
                }

                ApplyAdaptiveHeader();
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainForm] UpdateHeaderKpiBadges: {ex.Message}"); ErrLog.Swallowed(ex); }
        }

        public void ApplyAdaptiveHeader()
        {
            try
            {
                bool isCompact = this.ClientSize.Width <= 1300;

                if (btnHeaderSettings != null)
                {
                    btnHeaderSettings.Text = isCompact ? "\u2699\ufe0f" : "\u2699\ufe0f  Settings";
                    ttSidebar.SetToolTip(btnHeaderSettings, "System Administration & Settings");
                    btnHeaderSettings.Visible = SessionContext.IsAdmin;
                }

                if (btnLogout != null)
                {
                    btnLogout.Text = isCompact ? "\u238B" : "Log out";
                    btnLogout.Visible = true;
                }

                if (btnKpiScheduled != null)
                {
                    btnKpiScheduled.Text = isCompact ? $"⏳  {lastPendingCount}" : $"⏳  {lastPendingCount} scheduled";
                }
                if (btnKpiInTransit != null)
                {
                    btnKpiInTransit.Text = isCompact ? $"\U0001f69a  {lastInTransitCount}" : $"\U0001f69a  {lastInTransitCount} in transit";
                }
                if (btnKpiUnpaid != null)
                {
                    btnKpiUnpaid.Text = isCompact ? $"\U0001f4b3  {lastUnpaidCount}" : $"\U0001f4b3  {lastUnpaidCount} unpaid";
                }
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainForm] ApplyAdaptiveHeader: {ex.Message}"); ErrLog.Swallowed(ex); }
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                txtGlobalSearch.Focus();
                txtGlobalSearch.SelectAll();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F2)
            {
                btnNavDispatch_Click(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F3)
            {
                btnNavIncidents_Click(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F4)
            {
                btnNavTripReports_Click(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F5)
            {
                RefreshCurrentActiveView();
                UpdateHeaderKpiBadges();
                ToastHelper.ShowToast(this, "Data refreshed ✓", ToastType.Info);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F6)
            {
                btnNavBilling_Click(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F7)
            {
                btnNavCollections_Click(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F8)
            {
                if (SessionContext.IsAdmin)
                {
                    btnNavSettings_Click(this, EventArgs.Empty);
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                lstSearchDropdown.Visible = false;
                if (pnlNotificationMenu != null) pnlNotificationMenu.Visible = false;
            }
        }

        private void ShowShortcutsHelpDialog()
        {
            Form dlg = new Form
            {
                Text = "⌨️ System Keyboard Speed Shortcuts & Accessibility",
                Size = new Size(520, 420),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Pal.White
            };

            Label lblTitle = new Label
            {
                Text = "M.J. Billones Transport Service - Keyboard Shortcuts",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Pal.C(15, 23, 42),
                Location = new Point(20, 15),
                AutoSize = true
            };

            string helpText =
                "⚡ Operational Quick Navigation Keys:\n" +
                "   • F1  → Focus Global Search Bar\n" +
                "   • F2  → Jump to Delivery & Dispatch\n" +
                "   • F3  → Jump to Incident & Damage Logs\n" +
                "   • F4  → Jump to Trip Reports & Variance\n" +
                "   • F5  → Instant Refresh Active Data & KPI Badges\n" +
                "   • F6  → Jump to Billing & Invoices\n" +
                "   • F7  → Jump to Collections & Receipts\n" +
                "   • F8  → Jump to Settings & Administration\n" +
                "   • ESC → Close popups & clear search dropdown\n\n" +
                "💡 Form & Table Ergonomics:\n" +
                "   • Real-Time Search: Type in any table search box to filter instantly\n" +
                "   • Auto-Save: Selected records save changes to SQLite as you type\n" +
                "   • Front Checkboxes: 1-click toggles status with automatic persistence";

            Label lblContent = new Label
            {
                Text = helpText,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Pal.C(51, 65, 85),
                Location = new Point(20, 50),
                Size = new Size(465, 280)
            };

            Button btnClose = new Button
            {
                Text = "GOT IT",
                Location = new Point(370, 335),
                Size = new Size(115, 32),
                BackColor = Pal.C(37, 99, 235),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => dlg.Close();

            dlg.Controls.Add(lblTitle);
            dlg.Controls.Add(lblContent);
            dlg.Controls.Add(btnClose);
            dlg.ShowDialog(this);
        }

        public void RefreshCurrentActiveView()
        {
            if (panelMainContent.Controls.Count > 0 && panelMainContent.Controls[0] is IRefreshable refreshable)
            {
                if (this.InvokeRequired && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() => refreshable.RefreshData()));
                }
                else
                {
                    refreshable.RefreshData();
                }
            }
        }

        private void OpenDashboard()
        {
            NavigateToView("DashboardView", btnNavDashboard, () =>
            {
                var dash = new DashboardView();
                dash.OnNavigateRequested = target =>
                {
                    switch (target)
                    {
                        case "Trucks": btnNavTrucks_Click(this, EventArgs.Empty); break;
                        case "Loading": btnNavLoading_Click(this, EventArgs.Empty); break;
                        case "Dispatch": btnNavDispatch_Click(this, EventArgs.Empty); break;
                        case "TripReports": btnNavTripReports_Click(this, EventArgs.Empty); break;
                        case "Crew": btnNavCrew_Click(this, EventArgs.Empty); break;
                    }
                };
                return dash;
            });
        }

        private bool skipNextViewRefresh;
        private void NavigateToView<T>(string cacheKey, Button btn, Func<T> factory) where T : UserControl, IRefreshable
        {
            bool skipRefresh = skipNextViewRefresh;
            skipNextViewRefresh = false;
            if (pnlNotificationMenu != null) pnlNotificationMenu.Visible = false;
            if (lstSearchDropdown != null) lstSearchDropdown.Visible = false;
            HideNavFlyout();

            if (panelMainContent.Controls.Count > 0 && panelMainContent.Controls[0] is OperationsHubView oph)
            {
                if (!oph.CheckAndPromptUnsavedBeforeLeaving()) return;
            }

            if (!viewCache.TryGetValue(cacheKey, out UserControl? view))
            {
                view = factory();
                view.Dock = DockStyle.Fill;
                UIThemeHelper.ApplyTheme(view);
                viewCache[cacheKey] = view;
            }

            if (panelMainContent.Controls.Count == 0 || panelMainContent.Controls[0] != view)
            {
                panelMainContent.SuspendLayout();
                panelMainContent.Controls.Clear();
                panelMainContent.Controls.Add(view);
                panelMainContent.ResumeLayout(true);
            }

            if (!skipRefresh) ((IRefreshable)view).RefreshData();
            SetActiveNav(btn);
            UpdateHeaderKpiBadges();
        }

        private void LoadView(UserControl viewControl, Button btn)
        {
            if (pnlNotificationMenu != null) pnlNotificationMenu.Visible = false;
            if (lstSearchDropdown != null) lstSearchDropdown.Visible = false;

            if (panelMainContent.Controls.Count > 0 && panelMainContent.Controls[0] is OperationsHubView oph)
            {
                if (!oph.CheckAndPromptUnsavedBeforeLeaving()) return;
            }

            panelMainContent.SuspendLayout();
            panelMainContent.Controls.Clear();
            viewControl.Dock = DockStyle.Fill;
            panelMainContent.Controls.Add(viewControl);
            panelMainContent.ResumeLayout(true);

            SetActiveNav(btn);
            UpdateHeaderKpiBadges();
        }

        private void btnNavDashboard_Click(object sender, EventArgs e) => OpenDashboard();

        private void btnNavOperations_Click(object? sender, EventArgs e)
        {
            NavigateToView("OperationsHubView", btnNavOperations, CreateOperationsHub);
        }
        private OperationsHubView CreateOperationsHub()
        {
            var ops = new OperationsHubView();
            ops.OnLogIncidentForward = (delId, dispNum, missing, date) =>
            {
                ops.PrefillIncidentFromTrip(delId, dispNum, missing, date);
            };
            ops.OnCreateInvoiceForward = (del) =>
            {
                del = DispatchGate.Fresh(del);
                if (!DispatchGate.RequireCompleted(this, del, "bill it")) return;
                NavigateToView("FinanceHubView", btnNavFinance, () => new FinanceHubView("Billing", del));
            };
            return ops;
        }

        private void btnNavResources_Click(object? sender, EventArgs e)
        {
            NavigateToView("ResourcesHubView", btnNavResources, CreateResourcesHub);
        }
        private ResourcesHubView CreateResourcesHub()
        {
            var res = new ResourcesHubView();
            res.OnDispatchTruckForward = (truck) =>
            {
                btnNavOperations_Click(this, EventArgs.Empty);
                if (viewCache.TryGetValue("OperationsHubView", out var ov) && ov is OperationsHubView ohv)
                {
                    ohv.SelectTab("Dispatch");
                }
            };
            return res;
        }

        private void btnNavFinance_Click(object? sender, EventArgs e)
        {
            NavigateToView("FinanceHubView", btnNavFinance, () => new FinanceHubView());
        }

        private void btnNavMasterData_Click(object? sender, EventArgs e)
        {
            NavigateToView("MasterDataHubView", btnNavMasterData, () => new MasterDataHubView());
        }

        public void NavigateToDeliveryRoutes(string? routeCode = null, bool isNewRoute = false, string returnTarget = "Dispatch", Action? backAction = null)
        {
            try
            {
                OperationsHubView.SuppressUnsavedPrompt = true;
                btnNavMasterData_Click(this, EventArgs.Empty);
                if (viewCache.TryGetValue("MasterDataHubView", out var mv) && mv is MasterDataHubView mdv)
                {
                    mdv.SelectTab("Routes");
                    mdv.GetDeliveryRoutesView()?.OpenForShortcut(routeCode, isNewRoute, returnTarget, backAction);
                }
            }
            finally
            {
                OperationsHubView.SuppressUnsavedPrompt = false;
            }
        }

        public void EditDeliveryOrderInDispatch(Delivery delivery)
        {
            NavigateToOperations("Dispatch");
            if (viewCache.TryGetValue("OperationsHubView", out var ov) && ov is OperationsHubView ohv)
            {
                ohv.EditDeliveryInDispatch(delivery);
            }
        }

        private OperationsHubView? GetOperationsHub(string tab)
        {
            NavigateToOperations(tab);
            return viewCache.TryGetValue("OperationsHubView", out var ov) ? ov as OperationsHubView : null;
        }
        /// <summary>Opens the trip report for a dispatch: the existing one for editing, or a new one with the dispatch picked.</summary>
        public void OpenTripReportForDispatch(Delivery delivery)
        {
            delivery = DispatchGate.Fresh(delivery);
            if (!DispatchGate.RequireCompleted(this, delivery, "file its trip report")) return;
            GetOperationsHub("TripReports")?.OpenTripReportForDispatch(delivery);
        }
        public void ShowTripReportsForDispatch(string dispatchNumber)
        {
            GetOperationsHub("TripReports")?.FilterTripReportsByDispatch(dispatchNumber);
        }
        public void ShowIncidentsForDispatch(string dispatchNumber)
        {
            GetOperationsHub("Incidents")?.FilterIncidentsByDispatch(dispatchNumber);
        }
        public void LogIncidentForDispatch(Delivery delivery)
        {
            delivery = DispatchGate.Fresh(delivery);
            if (!DispatchGate.AllowIncident(this, delivery)) return;
            GetOperationsHub("Incidents")?.PrefillIncidentFromTrip(delivery.Id, delivery.DispatchNumber, 0, delivery.DispatchDate);
        }
        public void CreateInvoiceForDelivery(Delivery delivery)
        {
            delivery = DispatchGate.Fresh(delivery);
            if (!DispatchGate.RequireCompleted(this, delivery, "bill it")) return;
            NavigateToView("FinanceHubView", btnNavFinance, () => new FinanceHubView("Billing", delivery));
        }
        /// <summary>Opens a client's profile in Master Data → Clients.</summary>
        public void OpenClientProfile(string companyName)
        {
            NavigateToMasterData("Clients");
            if (viewCache.TryGetValue("MasterDataHubView", out var mv) && mv is MasterDataHubView mdv) mdv.OpenClient(companyName);
        }
        public T? GetOperationsView<T>(string tab) where T : UserControl
            => viewCache.TryGetValue("OperationsHubView", out var ov) && ov is OperationsHubView ohv ? ohv.GetView<T>(tab) : null;
        public void NavigateToOperations(string tab = "Dispatch")
        {
            try
            {
                OperationsHubView.SuppressUnsavedPrompt = true;
                skipNextViewRefresh = true; // the tab selection below refreshes the visible page once
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
            skipNextViewRefresh = true; // the tab selection below refreshes the visible page once
                btnNavResources_Click(this, EventArgs.Empty);
            if (viewCache.TryGetValue("ResourcesHubView", out var rv) && rv is ResourcesHubView rhv)
            {
                rhv.SelectTab(tab);
            }
        }

        /// <summary>Opens a finance tab with its search box filled in (e.g. one invoice number).</summary>
        public void ShowFinanceRecords(string tab, string search)
        {
            NavigateToFinance(tab);
            if (!viewCache.TryGetValue("FinanceHubView", out var fv) || fv is not FinanceHubView fhv) return;
            if (tab == "Billing") fhv.ShowInvoices(search);
            else if (tab == "Collections") fhv.ShowCollections(search);
            else fhv.ShowStatements(search);
        }
        public void NavigateToFinance(string tab = "StatementOfAccount")
        {
            skipNextViewRefresh = true; // the tab selection below refreshes the visible page once
                btnNavFinance_Click(this, EventArgs.Empty);
            if (viewCache.TryGetValue("FinanceHubView", out var fv) && fv is FinanceHubView fhv)
            {
                fhv.SelectTab(tab);
            }
        }

        public void NavigateToMasterData(string tab = "Clients")
        {
            skipNextViewRefresh = true; // the tab selection below refreshes the visible page once
                btnNavMasterData_Click(this, EventArgs.Empty);
            if (viewCache.TryGetValue("MasterDataHubView", out var mv) && mv is MasterDataHubView mdv)
            {
                mdv.SelectTab(tab);
            }
        }

        private void btnNavSettings_Click(object? sender, EventArgs e)
        {
            if (!SessionContext.IsAdmin) return;
            NavigateToView("SettingsView", btnNavSettings, () => new SettingsView(currentUser, "DispatchRules"));
        }

        private void btnNavLoading_Click(object sender, EventArgs e)
        {
            btnNavDispatch_Click(this, EventArgs.Empty);
        }

        private void btnNavTrucks_Click(object sender, EventArgs e)
        {
            btnNavResources_Click(this, EventArgs.Empty);
            if (viewCache.TryGetValue("ResourcesHubView", out var rv) && rv is ResourcesHubView rhv)
            {
                rhv.SelectTab("Trucks");
            }
        }

        private void btnNavCrew_Click(object sender, EventArgs e)
        {
            btnNavResources_Click(this, EventArgs.Empty);
            if (viewCache.TryGetValue("ResourcesHubView", out var rv) && rv is ResourcesHubView rhv)
            {
                rhv.SelectTab("Crew");
            }
        }

        private void btnNavDispatch_Click(object sender, EventArgs e)
        {
            btnNavOperations_Click(this, EventArgs.Empty);
            if (viewCache.TryGetValue("OperationsHubView", out var ov) && ov is OperationsHubView ohv)
            {
                ohv.SelectTab("Dispatch");
            }
        }

        private void btnNavTripReports_Click(object sender, EventArgs e)
        {
            btnNavOperations_Click(this, EventArgs.Empty);
            if (viewCache.TryGetValue("OperationsHubView", out var ov) && ov is OperationsHubView ohv)
            {
                ohv.SelectTab("TripReports");
            }
        }

        private void btnNavIncidents_Click(object sender, EventArgs e)
        {
            btnNavOperations_Click(this, EventArgs.Empty);
            if (viewCache.TryGetValue("OperationsHubView", out var ov) && ov is OperationsHubView ohv)
            {
                ohv.SelectTab("Incidents");
            }
        }

        private void btnNavBilling_Click(object sender, EventArgs e)
        {
            btnNavFinance_Click(this, EventArgs.Empty);
            if (viewCache.TryGetValue("FinanceHubView", out var fv) && fv is FinanceHubView fhv)
            {
                fhv.SelectTab("Billing");
            }
        }

        private void btnNavCollections_Click(object sender, EventArgs e)
        {
            btnNavFinance_Click(this, EventArgs.Empty);
            if (viewCache.TryGetValue("FinanceHubView", out var fv) && fv is FinanceHubView fhv)
            {
                fhv.SelectTab("Collections");
            }
        }

        private void btnNavRateMatrix_Click(object sender, EventArgs e)
        {
            btnNavMasterData_Click(this, EventArgs.Empty);
            if (viewCache.TryGetValue("MasterDataHubView", out var mv) && mv is MasterDataHubView mdv)
            {
                mdv.SelectTab("Rates");
            }
        }

        private void btnNavUsers_Click(object sender, EventArgs e)
        {
            NavigateToView("SettingsView", btnNavSettings, () => new SettingsView(currentUser, "Users"));
            if (viewCache.TryGetValue("SettingsView", out var sv) && sv is SettingsView setv)
            {
                setv.SelectTab("Users");
            }
        }

        private void btnNavReports_Click(object sender, EventArgs e)
        {
            NavigateToView("SettingsView", btnNavSettings, () => new SettingsView(currentUser, "Reports"));
            if (viewCache.TryGetValue("SettingsView", out var sv) && sv is SettingsView setv)
            {
                setv.SelectTab("Reports");
            }
        }

        private void btnNavBackup_Click(object sender, EventArgs e)
        {
            NavigateToView("SettingsView", btnNavSettings, () => new SettingsView(currentUser, "Backup"));
            if (viewCache.TryGetValue("SettingsView", out var sv) && sv is SettingsView setv)
            {
                setv.SelectTab("Backup");
            }
        }

        private void btnGlobalSearch_Click(object sender, EventArgs e)
        {
            string q = txtGlobalSearch.Text.Trim();
            if (string.IsNullOrEmpty(q))
            {
                QuietBox.Show("Please enter an order #, employee name, truck plate, or place to search.", "Search Prompt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var results = MJdatabase.GlobalSearch(q);
            if (results.Count == 0)
            {
                QuietBox.Show($"No records found matching '{q}'.", "Search Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Form searchDialog = new Form
            {
                Text = $"Search Results for '{q}' ({results.Count} Found)",
                Size = new Size(600, 400),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Pal.White
            };

            Label lblInfo = new Label
            {
                Text = $"Found {results.Count} matches across all system databases:",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Location = new Point(15, 12),
                AutoSize = true
            };

            ListBox lstResults = new ListBox
            {
                Location = new Point(15, 38),
                Size = new Size(555, 290),
                Font = new Font("Segoe UI", 9.5F)
            };

            foreach (var r in results)
            {
                lstResults.Items.Add(r);
            }

            Button btnOpen = new Button
            {
                Text = "OPEN RECORD",
                BackColor = Pal.C(37, 99, 235),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Location = new Point(450, 335),
                Size = new Size(120, 32),
                Cursor = Cursors.Hand
            };

            btnOpen.Click += (s, ev) =>
            {
                if (lstResults.SelectedItem is GlobalSearchResult selected)
                {
                    searchDialog.DialogResult = DialogResult.OK;
                    searchDialog.Close();
                    NavigateToSearchResult(selected);
                }
            };

            lstResults.DoubleClick += (s, ev) =>
            {
                if (lstResults.SelectedItem is GlobalSearchResult selected)
                {
                    searchDialog.DialogResult = DialogResult.OK;
                    searchDialog.Close();
                    NavigateToSearchResult(selected);
                }
            };

            searchDialog.Controls.Add(lblInfo);
            searchDialog.Controls.Add(lstResults);
            searchDialog.Controls.Add(btnOpen);
            searchDialog.ShowDialog(this);
        }

        private void RepositionSearchBar()
        {
            if (!pnlSearchCenter.Visible) return;
            int availableWidth = pnlSearchCenter.ClientSize.Width;
            if (availableWidth <= 0) return;

            if (btnGlobalSearch.Visible)
            {
                int btnWidth = 75;
                int spacing = 6;
                int targetSearchWidth = Math.Clamp(availableWidth - btnWidth - spacing - 40, 160, 360);

                int totalWidth = targetSearchWidth + spacing + btnWidth;
                int startX = Math.Max(10, (availableWidth - totalWidth) / 2);
                int startY = Math.Max(0, (pnlSearchCenter.ClientSize.Height - 26) / 2);

                txtGlobalSearch.Location = new Point(startX, startY);
                txtGlobalSearch.Size = new Size(targetSearchWidth, 26);

                btnGlobalSearch.Location = new Point(startX + targetSearchWidth + spacing, startY);
                btnGlobalSearch.Size = new Size(btnWidth, 26);

                if (lstSearchDropdown.Visible)
                {
                    Point screenPt = txtGlobalSearch.PointToScreen(new Point(0, txtGlobalSearch.Height + 2));
                    lstSearchDropdown.Location = this.PointToClient(screenPt);
                    lstSearchDropdown.Width = totalWidth;
                }
            }
            else
            {
                int targetSearchWidth = Math.Clamp(availableWidth - 40, 160, 420);
                int startX = Math.Max(10, (availableWidth - targetSearchWidth) / 2);
                int startY = Math.Max(0, (pnlSearchCenter.ClientSize.Height - 26) / 2);

                txtGlobalSearch.Location = new Point(startX, startY);
                txtGlobalSearch.Size = new Size(targetSearchWidth, 26);

                if (lstSearchDropdown.Visible)
                {
                    Point screenPt = txtGlobalSearch.PointToScreen(new Point(0, txtGlobalSearch.Height + 2));
                    lstSearchDropdown.Location = this.PointToClient(screenPt);
                    lstSearchDropdown.Width = targetSearchWidth;
                }
            }
        }

        private void txtGlobalSearch_TextChanged(object? sender, EventArgs e)
        {
            string q = txtGlobalSearch.Text.Trim();
            if (string.IsNullOrEmpty(q))
            {
                lstSearchDropdown.Visible = false;
                return;
            }

            var results = MJdatabase.GlobalSearch(q);
            if (results.Count > 0)
            {
                lstSearchDropdown.Items.Clear();
                foreach (var res in results)
                {
                    lstSearchDropdown.Items.Add(res);
                }
                Point screenPt = txtGlobalSearch.PointToScreen(new Point(0, txtGlobalSearch.Height + 2));
                lstSearchDropdown.Location = this.PointToClient(screenPt);
                lstSearchDropdown.Width = btnGlobalSearch.Visible ? (txtGlobalSearch.Width + 6 + btnGlobalSearch.Width) : txtGlobalSearch.Width;
                if (!this.Controls.Contains(lstSearchDropdown))
                {
                    this.Controls.Add(lstSearchDropdown);
                }
                if (pnlNotificationMenu != null) pnlNotificationMenu.Visible = false;
                lstSearchDropdown.BringToFront();
                lstSearchDropdown.Visible = true;
            }
            else
            {
                lstSearchDropdown.Visible = false;
            }
        }

        private void lstSearchDropdown_Click(object? sender, EventArgs e)
        {
            if (lstSearchDropdown.SelectedItem is GlobalSearchResult selected)
            {
                lstSearchDropdown.Visible = false;
                txtGlobalSearch.Text = "";
                NavigateToSearchResult(selected);
            }
        }

        private void NavigateToSearchResult(GlobalSearchResult item)
        {
            switch (item.TargetModule)
            {
                case "Dispatch":
                    btnNavDispatch_Click(this, EventArgs.Empty);
                    break;
                case "Loading":
                    btnNavLoading_Click(this, EventArgs.Empty);
                    break;
                case "Crew":
                    btnNavCrew_Click(this, EventArgs.Empty);
                    break;
                case "Trucks":
                    btnNavTrucks_Click(this, EventArgs.Empty);
                    break;
                case "RateMatrix":
                    btnNavRateMatrix_Click(this, EventArgs.Empty);
                    break;
                case "Billing":
                    btnNavBilling_Click(this, EventArgs.Empty);
                    break;
                default:
                    btnNavDashboard_Click(this, EventArgs.Empty);
                    break;
            }
        }

        private void ApplyUserRolePermissions()
        {
            btnNavSettings.Visible = SessionContext.CanAccessSettings;
            if (btnHeaderSettings != null)
            {
                btnHeaderSettings.Visible = SessionContext.CanAccessSettings;
            }

            if (SessionContext.IsDispatcher)
            {
                btnNavFinance.Visible = false;
                btnNavMasterData.Visible = false;
            }
            else if (SessionContext.IsBillingClerk)
            {
                btnNavOperations.Visible = false;
                btnNavResources.Visible = false;
                btnNavMasterData.Visible = false;
            }
            else if (!SessionContext.IsAdmin)
            {
                btnNavSettings.Visible = false;
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show("Are you sure you want to log out of the system?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                MJdatabase.LogSecurityEvent(currentUser?.Username ?? "", "LOGOUT", "User logged out");
                IsLoggingOut = true;
                SessionContext.ActiveUser = null;
                this.Close();
            }
        }
    }
}
