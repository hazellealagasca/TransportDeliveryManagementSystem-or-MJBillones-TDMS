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
        private const int SidebarExpandedWidth = 220;
        private const int SidebarCollapsedWidth = 55;
        private bool isSidebarCollapsed = false;

        private User currentUser;
        private Button? activeButton;
        private Dictionary<string, UserControl> viewCache = new Dictionary<string, UserControl>();

        private FlowLayoutPanel? pnlKpiFlow;
        private Button? btnKpiTransit;
        private Button? btnKpiPending;
        private Button? btnKpiUnpaid;
        private Button? btnKpiShortcuts;

        private Panel? pnlNotificationMenu;
        private Panel? pnlNotificationList;
        private FlowLayoutPanel? pnlNotificationFilters;
        private string activeNotificationFilter = "All";
        private System.Windows.Forms.Timer? tmrRollingSnapshot;
        private System.Windows.Forms.Timer? tmrOperationalAlerts;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public MainForm(User user)
        {
            InitializeComponent();
            UIThemeHelper.EnableDoubleBuffering(this);
            currentUser = user;
            SessionContext.ActiveUser = user;

            string cleanName = (currentUser.FullName ?? "").Replace(" (Owner)", "").Replace(" (Admin)", "").Trim();
            lblUserInfo.Text = $"👤 {cleanName} • {currentUser.Role}";
            lblUserInfo.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            lblUserInfo.ForeColor = Color.FromArgb(71, 85, 105);
            lblUserInfo.AutoSize = true;
            lblUserInfo.Cursor = Cursors.Hand;
            lblUserInfo.MouseEnter += (s, e) => { lblUserInfo.ForeColor = Color.FromArgb(30, 58, 138); };
            lblUserInfo.MouseLeave += (s, e) => { lblUserInfo.ForeColor = Color.FromArgb(71, 85, 105); };
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
                            picLogo.Image = Image.FromStream(stream);
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
            catch { }

            MJdatabase.PerformStartupSanityCheck();
            MJdatabase.OnCategoriesChanged += () => { RefreshCurrentActiveView(); };
            this.FormClosing += (s, e) => { MJdatabase.PerformAutoBackup(); tmrRollingSnapshot?.Stop(); tmrOperationalAlerts?.Stop(); };
            this.FormClosed += (s, e) => { Application.Exit(); };
            SetupRollingSnapshotTimer();
            SetupOperationalAlertsTimer();

            SetupAccessibilityAndKpis();
            SetupSidebarHover();
            SetupCollapsibleSidebar();
            SetupNotifications();
            panelMainContent.MouseDown += (s, e) => { if (pnlNotificationMenu != null) pnlNotificationMenu.Visible = false; };
            OpenDashboard();
            EventNotificationService.Publish("Session Active", $"Authenticated as @{currentUser.Username} ({currentUser.Role})", ToastType.Info, this);
            this.Shown += (s, e) =>
            {
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
            catch { }
        }

        private void ShowUserProfileDialog()
        {
            var dlg = new UserProfileDialog(currentUser);
            dlg.OnNavigateToUserManagement = () => btnNavUsers_Click(this, EventArgs.Empty);
            dlg.ShowDialog(this);
        }

        private void SetupCollapsibleSidebar()
        {
            tmrSidebarAnimation.Interval = 10;
            tmrSidebarAnimation.Tick += TmrSidebarAnimation_Tick;
            isSidebarCollapsed = true;
            panelSidebar.Width = SidebarCollapsedWidth;
            UpdateSidebarDisplay(true);
        }

        private void btnToggleSidebar_Click(object? sender, EventArgs e)
        {
            ToggleSidebar();
        }

        public void ToggleSidebar()
        {
            isSidebarCollapsed = !isSidebarCollapsed;
            tmrSidebarAnimation.Start();
        }

        private void TmrSidebarAnimation_Tick(object? sender, EventArgs e)
        {
            int target = isSidebarCollapsed ? SidebarCollapsedWidth : SidebarExpandedWidth;
            int step = 25;

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

                ttSidebar.SetToolTip(btnNavDashboard, "Dashboard");
                ttSidebar.SetToolTip(btnNavOperations, "Dispatch & Delivery Operations");
                ttSidebar.SetToolTip(btnNavResources, "Trucks & Personnel Resources");
                ttSidebar.SetToolTip(btnNavFinance, "Billing, Invoices & Payment Collections");
                ttSidebar.SetToolTip(btnNavMasterData, "Rate & Business Master Data");
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

                ttSidebar.RemoveAll();
            }
        }

        private void SetupSidebarHover()
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
                Padding = new Padding(0, 14, 15, 0),
                BackColor = Color.Transparent
            };

            btnKpiTransit = CreateKpiBadge("🚚 0 In Transit", Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9), (s, e) => btnNavDispatch_Click(this, EventArgs.Empty));
            btnKpiPending = CreateKpiBadge("⏳ 0 Pending", Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85), (s, e) => btnNavLoading_Click(this, EventArgs.Empty));
            btnKpiUnpaid = CreateKpiBadge("💰 0 Unpaid", Color.FromArgb(254, 226, 226), Color.FromArgb(185, 28, 28), (s, e) => btnNavBilling_Click(this, EventArgs.Empty));
            btnKpiShortcuts = CreateKpiBadge("⌨️ ShortKeys", Color.FromArgb(238, 242, 255), Color.FromArgb(67, 56, 202), (s, e) => ShowShortcutsHelpDialog());

            btnNotificationBell.Text = "🔔 Alerts";
            btnNotificationBell.AutoSize = true;
            btnNotificationBell.Height = 28;
            btnNotificationBell.Margin = new Padding(0, 0, 6, 0);
            btnNotificationBell.BackColor = Color.FromArgb(241, 245, 249);
            btnNotificationBell.ForeColor = Color.FromArgb(30, 58, 138);
            btnNotificationBell.FlatStyle = FlatStyle.Flat;
            btnNotificationBell.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            btnNotificationBell.Cursor = Cursors.Hand;
            btnNotificationBell.FlatAppearance.BorderSize = 0;
            btnNotificationBell.Click += (s, e) => ToggleNotificationMenu();

            btnHeaderSettings.Size = new Size(105, 28);
            btnHeaderSettings.Margin = new Padding(4, 0, 6, 0);
            btnHeaderSettings.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);

            btnLogout.Size = new Size(80, 28);
            btnLogout.Margin = new Padding(0, 0, 0, 0);
            btnLogout.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);

            pnlKpiFlow.Controls.Add(btnKpiTransit);
            pnlKpiFlow.Controls.Add(btnKpiPending);
            pnlKpiFlow.Controls.Add(btnKpiUnpaid);
            pnlKpiFlow.Controls.Add(btnKpiShortcuts);
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
            pnlNotificationMenu = new Panel
            {
                Size = new Size(420, 500),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(12, 8, 12, 8)
            };

            Label lblTitle = new Label
            {
                Text = "🔔 Notifications & Alerts",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };

            FlowLayoutPanel pnlHeaderActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0)
            };

            Button btnMarkAllRead = new Button
            {
                Text = "✓ Read All",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                Height = 28,
                Cursor = Cursors.Hand
            };
            btnMarkAllRead.FlatAppearance.BorderSize = 0;
            btnMarkAllRead.Click += (s, e) =>
            {
                EventNotificationService.MarkAllAsRead(SessionContext.IsAdmin);
                RenderNotificationList();
            };

            Button btnClear = new Button
            {
                Text = "Clear",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 63, 94),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                Height = 28,
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) =>
            {
                EventNotificationService.ClearAll(SessionContext.IsAdmin);
                RenderNotificationList();
            };

            pnlHeaderActions.Controls.Add(btnMarkAllRead);
            pnlHeaderActions.Controls.Add(btnClear);

            pnlHeader.Controls.Add(pnlHeaderActions);
            pnlHeader.Controls.Add(lblTitle);

            pnlNotificationFilters = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(6, 4, 6, 4),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            List<string> filterList = new List<string> { "All", "Unread", "Alerts", "Operations", "Payments", "Trucks" };
            if (SessionContext.IsAdmin)
            {
                filterList.Add("Security");
            }
            string[] filterNames = filterList.ToArray();
            foreach (var filter in filterNames)
            {
                Button btnFilter = new Button
                {
                    Text = filter,
                    AutoSize = true,
                    Height = 26,
                    Margin = new Padding(2, 0, 2, 0),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Tag = filter
                };
                btnFilter.FlatAppearance.BorderSize = 0;
                UpdateFilterButtonAppearance(btnFilter, filter == activeNotificationFilter);

                btnFilter.Click += (s, e) =>
                {
                    activeNotificationFilter = (string)((Control)s!).Tag!;
                    foreach (Control c in pnlNotificationFilters.Controls)
                    {
                        if (c is Button b && b.Tag is string t)
                        {
                            UpdateFilterButtonAppearance(b, t == activeNotificationFilter);
                        }
                    }
                    RenderNotificationList();
                };
                pnlNotificationFilters.Controls.Add(btnFilter);
            }

            pnlNotificationList = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(8, 8, 8, 8),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            pnlNotificationMenu.Controls.Add(pnlNotificationList);
            pnlNotificationMenu.Controls.Add(pnlNotificationFilters);
            pnlNotificationMenu.Controls.Add(pnlHeader);

            this.Controls.Add(pnlNotificationMenu);
            pnlNotificationMenu.BringToFront();

            EventNotificationService.OnNotificationsChanged += () =>
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(UpdateNotificationUI));
                }
            };

            UpdateNotificationUI();
        }

        private void UpdateFilterButtonAppearance(Button btn, bool isActive)
        {
            if (isActive)
            {
                btn.BackColor = Color.FromArgb(15, 23, 42);
                btn.ForeColor = Color.White;
            }
            else
            {
                btn.BackColor = Color.FromArgb(226, 232, 240);
                btn.ForeColor = Color.FromArgb(71, 85, 105);
            }
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
                catch { }
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

        private void ToggleNotificationMenu()
        {
            if (pnlNotificationMenu == null) return;

            if (pnlNotificationMenu.Visible)
            {
                pnlNotificationMenu.Visible = false;
                return;
            }

            RenderNotificationList();
            Point pt = btnNotificationBell.PointToScreen(new Point(btnNotificationBell.Width - 420, btnNotificationBell.Height + 6));
            Point clientPt = this.PointToClient(pt);
            pnlNotificationMenu.Location = clientPt;
            pnlNotificationMenu.BringToFront();
            pnlNotificationMenu.Visible = true;
        }

        private void UpdateNotificationUI()
        {
            int unread = EventNotificationService.UnreadCount;
            btnNotificationBell.Text = unread > 0 ? $"🔔 {unread} New" : "🔔 Alerts";
            btnNotificationBell.BackColor = unread > 0 ? Color.FromArgb(254, 243, 199) : Color.FromArgb(241, 245, 249);
            btnNotificationBell.ForeColor = unread > 0 ? Color.FromArgb(180, 83, 9) : Color.FromArgb(51, 65, 85);

            if (pnlNotificationMenu != null && pnlNotificationMenu.Visible)
            {
                RenderNotificationList();
            }
        }

        private void RenderNotificationList()
        {
            if (pnlNotificationList == null) return;

            pnlNotificationList.SuspendLayout();
            pnlNotificationList.Controls.Clear();

            bool unreadOnly = activeNotificationFilter.Equals("Unread", StringComparison.OrdinalIgnoreCase);
            string? catFilter = unreadOnly ? null : activeNotificationFilter;

            var notifs = EventNotificationService.GetRecentNotifications(60, catFilter, unreadOnly, SessionContext.IsAdmin);
            if (notifs.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "No notifications in this filter ✓\nAll system operations normal",
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };
                pnlNotificationList.Controls.Add(lblEmpty);
            }
            else
            {
                TableLayoutPanel tblStack = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    ColumnCount = 1,
                    Padding = new Padding(0),
                    Margin = new Padding(0)
                };
                tblStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                foreach (var n in notifs)
                {
                    Panel card = new Panel
                    {
                        Dock = DockStyle.Top,
                        Height = 84,
                        BackColor = n.IsRead ? Color.White : Color.FromArgb(254, 252, 232),
                        Padding = new Padding(12, 8, 12, 8),
                        Margin = new Padding(0, 0, 0, 6),
                        BorderStyle = BorderStyle.None,
                        Cursor = Cursors.Hand
                    };

                    Color accentColor;
                    switch (n.Type)
                    {
                        case ToastType.Success: accentColor = Color.FromArgb(16, 185, 129); break;
                        case ToastType.Warning: accentColor = Color.FromArgb(245, 158, 11); break;
                        case ToastType.Error: accentColor = Color.FromArgb(239, 68, 68); break;
                        default: accentColor = Color.FromArgb(59, 130, 246); break;
                    }

                    card.Paint += (s, e) =>
                    {
                        var g = e.Graphics;
                        using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1))
                        {
                            g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                        }
                        using (var brush = new SolidBrush(accentColor))
                        {
                            g.FillRectangle(brush, 0, 0, 4, card.Height);
                        }
                    };

                    card.MouseEnter += (s, e) =>
                    {
                        if (card.BackColor == Color.White) card.BackColor = Color.FromArgb(241, 245, 249);
                    };
                    card.MouseLeave += (s, e) =>
                    {
                        if (card.BackColor == Color.FromArgb(241, 245, 249)) card.BackColor = Color.White;
                    };

                    Label lblHeader = new Label
                    {
                        Text = $"{n.IconSymbol}  {n.Title}",
                        Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(15, 23, 42),
                        Location = new Point(12, 6),
                        AutoSize = true,
                        Cursor = Cursors.Hand
                    };

                    Label lblTime = new Label
                    {
                        Text = n.RelativeTime,
                        Font = new Font("Segoe UI", 7.5F),
                        ForeColor = Color.FromArgb(148, 163, 184),
                        Anchor = AnchorStyles.Top | AnchorStyles.Right,
                        TextAlign = ContentAlignment.TopRight,
                        Size = new Size(80, 16),
                        Location = new Point(card.Width - 110, 6)
                    };

                    Button btnDismiss = new Button
                    {
                        Text = "✕",
                        Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(156, 163, 175),
                        BackColor = Color.Transparent,
                        FlatStyle = FlatStyle.Flat,
                        Size = new Size(20, 20),
                        Location = new Point(card.Width - 26, 4),
                        Anchor = AnchorStyles.Top | AnchorStyles.Right,
                        Cursor = Cursors.Hand
                    };
                    btnDismiss.FlatAppearance.BorderSize = 0;
                    btnDismiss.Click += (s, e) =>
                    {
                        EventNotificationService.DeleteNotification(n.Id);
                        RenderNotificationList();
                    };

                    Label lblDesc = new Label
                    {
                        Text = n.Message,
                        Font = new Font("Segoe UI", 8.5F),
                        ForeColor = Color.FromArgb(71, 85, 105),
                        Location = new Point(12, 26),
                        Size = new Size(Math.Max(100, card.Width - 30), 34),
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                        Cursor = Cursors.Hand
                    };

                    Label lblAction = new Label
                    {
                        Text = string.IsNullOrEmpty(n.ActionHint) ? "" : n.ActionHint,
                        Font = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(37, 99, 235),
                        Location = new Point(12, 62),
                        Size = new Size(Math.Max(100, card.Width - 30), 16),
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                        Cursor = Cursors.Hand
                    };

                    card.Resize += (s, e) =>
                    {
                        lblTime.Location = new Point(card.Width - 110, 6);
                        btnDismiss.Location = new Point(card.Width - 26, 4);
                        lblDesc.Size = new Size(Math.Max(100, card.Width - 30), 34);
                        lblAction.Size = new Size(Math.Max(100, card.Width - 30), 16);
                    };

                    void OnCardClicked(object? sender, EventArgs e)
                    {
                        EventNotificationService.MarkAsRead(n.Id);
                        if (pnlNotificationMenu != null) pnlNotificationMenu.Visible = false;

                        if (!string.IsNullOrEmpty(n.TargetModule))
                        {
                            switch (n.TargetModule.ToLowerInvariant())
                            {
                                case "dispatch":
                                    btnNavDispatch_Click(this, EventArgs.Empty);
                                    break;
                                case "loading":
                                    btnNavLoading_Click(this, EventArgs.Empty);
                                    break;
                                case "tripreports":
                                    btnNavTripReports_Click(this, EventArgs.Empty);
                                    break;
                                case "invoices":
                                case "billing":
                                    btnNavBilling_Click(this, EventArgs.Empty);
                                    break;
                                case "collections":
                                    btnNavCollections_Click(this, EventArgs.Empty);
                                    break;
                                case "incidents":
                                    btnNavIncidents_Click(this, EventArgs.Empty);
                                    break;
                                case "trucks":
                                    btnNavTrucks_Click(this, EventArgs.Empty);
                                    break;
                                case "crew":
                                    btnNavCrew_Click(this, EventArgs.Empty);
                                    break;
                                case "settings":
                                    if (SessionContext.IsAdmin)
                                    {
                                        btnNavSettings_Click(this, EventArgs.Empty);
                                    }
                                    break;
                            }
                        }
                    }

                    card.Click += OnCardClicked;
                    lblHeader.Click += OnCardClicked;
                    lblDesc.Click += OnCardClicked;
                    lblAction.Click += OnCardClicked;

                    card.Controls.Add(btnDismiss);
                    card.Controls.Add(lblTime);
                    card.Controls.Add(lblHeader);
                    card.Controls.Add(lblDesc);
                    card.Controls.Add(lblAction);

                    tblStack.Controls.Add(card);
                }

                pnlNotificationList.Controls.Add(tblStack);
            }

            pnlNotificationList.ResumeLayout(true);
        }

        private Button CreateKpiBadge(string text, Color bg, Color fg, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 28,
                Margin = new Padding(0, 0, 6, 0),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        public void UpdateHeaderKpiBadges()
        {
            try
            {
                var deliveries = MJdatabase.GetDeliveries();
                int inTransit = deliveries.Count(d => d.Status == "In Transit");
                int pending = deliveries.Count(d => d.Status == "Pending");

                var invoices = MJdatabase.GetInvoices();
                int unpaid = invoices.Count(i => i.Status == "Unpaid" || i.Status == "Partial");

                if (btnKpiTransit != null) btnKpiTransit.Text = $"🚚 {inTransit} Transit";
                if (btnKpiPending != null) btnKpiPending.Text = $"⏳ {pending} Pending";
                if (btnKpiUnpaid != null) btnKpiUnpaid.Text = $"💰 {unpaid} Unpaid";
            }
            catch { }
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
                btnNavLoading_Click(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F3)
            {
                btnNavDispatch_Click(this, EventArgs.Empty);
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
                btnNavSettings_Click(this, EventArgs.Empty);
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
                BackColor = Color.White
            };

            Label lblTitle = new Label
            {
                Text = "M.J. Billones Transport Service - Keyboard Shortcuts",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(20, 15),
                AutoSize = true
            };

            string helpText =
                "⚡ Operational Quick Navigation Keys:\n" +
                "   • F1  → Focus Global Search Bar\n" +
                "   • F2  → Jump to Loading Schedules\n" +
                "   • F3  → Jump to Delivery & Dispatch\n" +
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
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(20, 50),
                Size = new Size(465, 280)
            };

            Button btnClose = new Button
            {
                Text = "GOT IT",
                Location = new Point(370, 335),
                Size = new Size(115, 32),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
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

        private void NavigateToView<T>(string cacheKey, Button btn, Func<T> factory) where T : UserControl, IRefreshable
        {
            if (pnlNotificationMenu != null) pnlNotificationMenu.Visible = false;

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

            ((IRefreshable)view).RefreshData();

            if (activeButton != null)
            {
                activeButton.BackColor = Color.FromArgb(15, 23, 42);
                activeButton.ForeColor = Color.FromArgb(148, 163, 184);
                activeButton.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular);
            }

            activeButton = btn;
            activeButton.BackColor = Color.FromArgb(30, 41, 59);
            activeButton.ForeColor = Color.White;
            activeButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);

            string rawText = btn.Text.Trim();
            lblHeaderTitle.Text = rawText.Length > 2 ? rawText.Substring(2).Trim().ToUpper() : rawText.ToUpper();
            UpdateHeaderKpiBadges();
        }

        private void LoadView(UserControl viewControl, Button btn)
        {
            if (pnlNotificationMenu != null) pnlNotificationMenu.Visible = false;

            panelMainContent.SuspendLayout();
            panelMainContent.Controls.Clear();
            viewControl.Dock = DockStyle.Fill;
            panelMainContent.Controls.Add(viewControl);
            panelMainContent.ResumeLayout(true);

            if (activeButton != null)
            {
                activeButton.BackColor = Color.FromArgb(15, 23, 42);
                activeButton.ForeColor = Color.FromArgb(148, 163, 184);
                activeButton.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular);
            }

            activeButton = btn;
            activeButton.BackColor = Color.FromArgb(30, 41, 59);
            activeButton.ForeColor = Color.White;
            activeButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);

            string rawText = btn.Text.Trim();
            lblHeaderTitle.Text = rawText.Length > 2 ? rawText.Substring(2).Trim().ToUpper() : rawText.ToUpper();
            UpdateHeaderKpiBadges();
        }

        private void btnNavDashboard_Click(object sender, EventArgs e) => OpenDashboard();

        private void btnNavOperations_Click(object? sender, EventArgs e)
        {
            NavigateToView("OperationsHubView", btnNavOperations, () =>
            {
                var ops = new OperationsHubView();
                ops.OnLogIncidentForward = (delId, dispNum, missing, date) =>
                {
                    ops.PrefillIncidentFromTrip(delId, dispNum, missing, date);
                };
                ops.OnCreateInvoiceForward = (del) =>
                {
                    NavigateToView("FinanceHubView", btnNavFinance, () => new FinanceHubView("Billing"));
                };
                return ops;
            });
        }

        private void btnNavResources_Click(object? sender, EventArgs e)
        {
            NavigateToView("ResourcesHubView", btnNavResources, () =>
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
            });
        }

        private void btnNavFinance_Click(object? sender, EventArgs e)
        {
            NavigateToView("FinanceHubView", btnNavFinance, () => new FinanceHubView());
        }

        private void btnNavMasterData_Click(object? sender, EventArgs e)
        {
            NavigateToView("MasterDataHubView", btnNavMasterData, () => new MasterDataHubView());
        }

        private void btnNavSettings_Click(object? sender, EventArgs e)
        {
            NavigateToView("SettingsView", btnNavSettings, () => new SettingsView(currentUser, "DispatchRules"));
        }

        private void btnNavLoading_Click(object sender, EventArgs e)
        {
            btnNavOperations_Click(this, EventArgs.Empty);
            if (viewCache.TryGetValue("OperationsHubView", out var ov) && ov is OperationsHubView ohv)
            {
                ohv.SelectTab("Loading");
            }
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
                MessageBox.Show("Please enter an order #, employee name, truck plate, or place to search.", "Search Prompt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var results = MJdatabase.GlobalSearch(q);
            if (results.Count == 0)
            {
                MessageBox.Show($"No records found matching '{q}'.", "Search Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                BackColor = Color.White
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
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
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
                lstSearchDropdown.Width = txtGlobalSearch.Width;
                if (!this.Controls.Contains(lstSearchDropdown))
                {
                    this.Controls.Add(lstSearchDropdown);
                }
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

        private void btnLogout_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show("Are you sure you want to log out of the system?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                this.Hide();
                LoginForm login = new LoginForm();
                login.FormClosed += (s, args) => this.Close();
                login.Show();
            }
        }
    }
}
