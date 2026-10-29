using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    /// <summary>
    /// Shell styling for the main window: sidebar, header bar, brand mark and the light/dark quick toggle.
    /// Everything here reads colours from the active theme, so a theme change only needs the window to be rebuilt.
    /// </summary>
    public partial class MainForm
    {
        /// <summary>Set when the window is being rebuilt for a new theme; the new window replaces this one.</summary>
        public MainForm? Replacement { get; private set; }
        public bool IsReloading { get; private set; }
        private Button? hoveredNavButton;
        private Button? btnThemeToggle;
        private Panel? pnlSidebarFooter;
        private Image? logoFull;
        private Image? logoMark;
        private static readonly Font NavFont = new Font("Segoe UI Semibold", 9.75F, FontStyle.Regular);
        private static readonly Font NavIconFont = new Font("Segoe UI Emoji", 11F, FontStyle.Regular);
        private static readonly Font ChipFont = new Font("Segoe UI Semibold", 8.75F, FontStyle.Regular);
        private int S(int v) => (int)Math.Round(v * this.DeviceDpi / 96f);
        private void ApplyShellTheme()
        {
            this.BackColor = AppTheme.Canvas;
            ThemeManager.StyleWindowChrome(this);
            // ----- Sidebar -----
            panelSidebar.BackColor = AppTheme.Sidebar;
            panelLogo.BackColor = AppTheme.Sidebar;
            panelLogo.Height = S(88);
            panelLogo.Padding = new Padding(S(16), S(14), S(16), S(12));
            picLogo.BackColor = AppTheme.Sidebar;
            picLogo.Padding = new Padding(0);
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            lblLogoTitle.ForeColor = AppTheme.SidebarText;
            lblLogoSubtitle.ForeColor = AppTheme.SidebarTextMuted;
            logoFull = BrandAssets.LogoFor(AppTheme.Sidebar);
            logoMark = BrandAssets.EagleMark(AppTheme.Sidebar);
            if (logoFull != null)
            {
                picLogo.Image = logoFull;
                lblLogoTitle.Visible = false;
                lblLogoSubtitle.Visible = false;
            }
            BrandAssets.LogoChanged -= OnBrandLogoChanged;
            BrandAssets.LogoChanged += OnBrandLogoChanged;
            this.FormClosed -= UnhookBrandLogo;
            this.FormClosed += UnhookBrandLogo;
            panelLogo.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.SidebarDivider, 1f);
                e.Graphics.DrawLine(pen, 14, panelLogo.Height - 1, panelLogo.Width - 14, panelLogo.Height - 1);
            };
            var navSpacer = new Panel { Dock = DockStyle.Top, Height = S(10), BackColor = AppTheme.Sidebar };
            panelSidebar.Controls.Add(navSpacer);
            panelSidebar.Controls.SetChildIndex(navSpacer, panelSidebar.Controls.GetChildIndex(panelLogo));
            foreach (Control c in panelSidebar.Controls)
            {
                if (c is Button nav && nav != btnThemeToggle)
                {
                    StyleNavButton(nav);
                }
            }
            BuildSidebarFooter();
            // ----- Header -----
            panelHeader.BackColor = AppTheme.Surface;
            panelHeader.Height = S(64);
            panelHeader.Padding = new Padding(0, 0, 0, 1);
            panelHeader.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.Border, 1f);
                e.Graphics.DrawLine(pen, 0, panelHeader.Height - 1, panelHeader.Width, panelHeader.Height - 1);
            };
            pnlHeaderLeft.BackColor = AppTheme.Surface;
            pnlHeaderLeft.AutoSize = false;
            pnlHeaderLeft.Width = S(460);
            pnlHeaderLeft.Height = S(63);
            btnToggleSidebar.Location = new Point(S(16), S(15));
            btnToggleSidebar.Size = new Size(S(34), S(34));
            btnToggleSidebar.Text = "☰";
            btnToggleSidebar.Font = new Font("Segoe UI Symbol", 12F, FontStyle.Regular);
            btnToggleSidebar.BackColor = AppTheme.Surface;
            btnToggleSidebar.ForeColor = AppTheme.TextSecondary;
            btnToggleSidebar.FlatStyle = FlatStyle.Flat;
            btnToggleSidebar.FlatAppearance.BorderSize = 0;
            btnToggleSidebar.FlatAppearance.MouseOverBackColor = AppTheme.SurfaceHover;
            btnToggleSidebar.FlatAppearance.MouseDownBackColor = AppTheme.Border;
            btnToggleSidebar.Cursor = Cursors.Hand;
            UIThemeHelper.StyleButtonChrome(btnToggleSidebar);
            lblHeaderTitle.AutoSize = true;
            lblHeaderTitle.Location = new Point(S(60), S(8));
            lblHeaderTitle.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Regular);
            lblHeaderTitle.ForeColor = AppTheme.TextPrimary;
            lblUserInfo.Location = new Point(S(62), S(37));
            lblUserInfo.Font = new Font("Segoe UI", 8.75F, FontStyle.Regular);
            lblUserInfo.ForeColor = AppTheme.TextMuted;
            lblUserInfo.MouseEnter += (s, e) => lblUserInfo.ForeColor = AppTheme.AccentText;
            lblUserInfo.MouseLeave += (s, e) => lblUserInfo.ForeColor = AppTheme.TextMuted;
            pnlSearchCenter.BackColor = AppTheme.Surface;
            txtGlobalSearch.BackColor = AppTheme.InputBack;
            txtGlobalSearch.ForeColor = AppTheme.TextPrimary;
            btnGlobalSearch.BackColor = AppTheme.Accent;
            btnGlobalSearch.ForeColor = AppTheme.OnAccent;
            UIThemeHelper.StyleButtonChrome(btnGlobalSearch);
            lstSearchDropdown.BackColor = AppTheme.Surface;
            lstSearchDropdown.ForeColor = AppTheme.TextPrimary;
            panelMainContent.BackColor = AppTheme.Canvas;
            panelMainContent.Padding = new Padding(S(20), S(18), S(20), S(18));
            ttSidebar.BackColor = AppTheme.Surface;
            ttSidebar.ForeColor = AppTheme.TextPrimary;
        }
        // ---------- Header chips ----------
        private static void StyleHeaderChip(Button b, Color back, Color fore, Color hover, bool outline = false)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = outline ? 1 : 0;
            b.FlatAppearance.BorderColor = AppTheme.Border;
            b.BackColor = back;
            b.ForeColor = fore;
            b.FlatAppearance.MouseOverBackColor = hover;
            b.FlatAppearance.MouseDownBackColor = ThemeManager.Mix(hover, fore, 0.08);
            b.Font = ChipFont;
            b.Padding = new Padding(10, 0, 10, 0);
            b.AutoSize = false;
            b.Height = (int)Math.Round(34 * b.DeviceDpi / 96f);
            b.Cursor = Cursors.Hand;
            b.TextAlign = ContentAlignment.MiddleCenter;
            UIThemeHelper.StyleButtonChrome(b);
            FitChip(b);
            b.TextChanged += (s, e) => FitChip(b);
        }
        private static void FitChip(Button b)
        {
            int w = TextRenderer.MeasureText(b.Text, b.Font, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width + 28;
            b.Width = Math.Max((int)Math.Round(38 * b.DeviceDpi / 96f), w + (int)Math.Round(4 * b.DeviceDpi / 96f));
            b.Invalidate();
        }
        private void StyleHeaderChips()
        {
            StyleHeaderChip(btnKpiScheduled, AppTheme.WarningSoft, AppTheme.WarningText, ThemeManager.Mix(AppTheme.WarningSoft, AppTheme.Warning, 0.14));
            StyleHeaderChip(btnKpiInTransit, AppTheme.InfoSoft, AppTheme.InfoText, ThemeManager.Mix(AppTheme.InfoSoft, AppTheme.Info, 0.12));
            StyleHeaderChip(btnKpiUnpaid, AppTheme.DangerSoft, AppTheme.DangerText, ThemeManager.Mix(AppTheme.DangerSoft, AppTheme.Danger, 0.12));
            StyleHeaderChip(btnNotificationBell, AppTheme.Surface, AppTheme.TextSecondary, AppTheme.SurfaceHover, outline: true);
            StyleHeaderChip(btnHeaderSettings, AppTheme.Surface, AppTheme.TextSecondary, AppTheme.SurfaceHover, outline: true);
            StyleHeaderChip(btnLogout, AppTheme.Surface, AppTheme.TextSecondary, AppTheme.DangerSoft, outline: true);
            btnLogout.MouseEnter += (s, e) => btnLogout.ForeColor = AppTheme.DangerText;
            btnLogout.MouseLeave += (s, e) => btnLogout.ForeColor = AppTheme.TextSecondary;
            foreach (var b in new[] { btnKpiScheduled, btnKpiInTransit, btnKpiUnpaid, btnNotificationBell, btnHeaderSettings, btnLogout })
            {
                b.Margin = new Padding(0, 0, 8, 0);
            }
            btnLogout.Margin = new Padding(0);
            ttSidebar.SetToolTip(btnKpiScheduled, "Pending & scheduled deliveries");
            ttSidebar.SetToolTip(btnKpiInTransit, "Trucks currently on the road");
            ttSidebar.SetToolTip(btnKpiUnpaid, "Unpaid & partially paid invoices");
            ttSidebar.SetToolTip(btnNotificationBell, "Notifications & alerts");
            ttSidebar.SetToolTip(btnLogout, "Sign out");
            if (pnlKpiFlow != null)
            {
                pnlKpiFlow.BackColor = AppTheme.Surface;
                pnlKpiFlow.Padding = new Padding(0, S(15), S(20), 0);
            }
        }
        private void UpdateBellChip(int unread)
        {
            if (btnNotificationBell == null) return;
            if (unread > 0)
            {
                btnNotificationBell.BackColor = AppTheme.AccentSoft;
                btnNotificationBell.ForeColor = AppTheme.AccentText;
                btnNotificationBell.FlatAppearance.BorderColor = AppTheme.AccentSoftStrong;
                btnNotificationBell.FlatAppearance.MouseOverBackColor = AppTheme.AccentSoftStrong;
            }
            else
            {
                btnNotificationBell.BackColor = AppTheme.Surface;
                btnNotificationBell.ForeColor = AppTheme.TextSecondary;
                btnNotificationBell.FlatAppearance.BorderColor = AppTheme.Border;
                btnNotificationBell.FlatAppearance.MouseOverBackColor = AppTheme.SurfaceHover;
            }
            btnNotificationBell.Invalidate();
        }
        // ---------- Sidebar navigation ----------
        private void StyleNavButton(Button nav)
        {
            nav.FlatStyle = FlatStyle.Flat;
            nav.FlatAppearance.BorderSize = 0;
            nav.FlatAppearance.MouseOverBackColor = AppTheme.Sidebar;
            nav.FlatAppearance.MouseDownBackColor = AppTheme.Sidebar;
            nav.UseVisualStyleBackColor = false;
            nav.BackColor = AppTheme.Sidebar;
            nav.ForeColor = AppTheme.SidebarTextMuted;
            nav.Height = S(46);
            nav.Cursor = Cursors.Hand;
            nav.Font = NavFont;
            nav.TabStop = false;
            UIThemeHelper.ExcludeFromChrome(nav);
            nav.MouseEnter += (s, e) => { hoveredNavButton = nav; nav.Invalidate(); };
            nav.MouseLeave += (s, e) => { if (hoveredNavButton == nav) hoveredNavButton = null; nav.Invalidate(); };
            nav.Paint += (s, e) => PaintNavButton(nav, e.Graphics);
        }
        private void PaintNavButton(Button nav, Graphics g)
        {
            bool active = nav == activeButton;
            bool hover = nav == hoveredNavButton;
            bool collapsed = panelSidebar.Width < S(120);
            Gfx.HighQuality(g);
            g.Clear(AppTheme.Sidebar);
            var r = collapsed ? new Rectangle(S(8), S(4), nav.Width - S(16), nav.Height - S(8)) : new Rectangle(S(10), S(4), nav.Width - S(20), nav.Height - S(8));
            if (active || hover)
            {
                using var path = Gfx.RoundedRect(r, S(10));
                using var b = new SolidBrush(active ? AppTheme.SidebarActive : AppTheme.SidebarHover);
                g.FillPath(b, path);
            }
            if (active)
            {
                using var barPath = Gfx.RoundedRect(new RectangleF(collapsed ? S(2) : S(3), r.Y + S(11), S(3) + 0.5f, Math.Max(6, r.Height - S(22))), 1.75f);
                using var bb = new SolidBrush(AppTheme.SidebarIndicator);
                g.FillPath(bb, barPath);
            }
            Color fg = active ? AppTheme.SidebarActiveText : hover ? AppTheme.SidebarText : AppTheme.SidebarTextMuted;
            string text = nav.Text.Trim();
            string icon = text;
            string label = "";
            int split = text.IndexOf("  ", StringComparison.Ordinal);
            if (split > 0)
            {
                icon = text.Substring(0, split).Trim();
                label = text.Substring(split).Trim();
            }
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;
            if (collapsed || string.IsNullOrEmpty(label))
            {
                if (string.IsNullOrEmpty(label) || collapsed)
                    TextRenderer.DrawText(g, icon, NavIconFont, r, fg, flags | TextFormatFlags.HorizontalCenter);
            }
            else
            {
                var iconRect = new Rectangle(r.X + S(8), r.Y, S(30), r.Height);
                TextRenderer.DrawText(g, icon, NavIconFont, iconRect, fg, flags | TextFormatFlags.HorizontalCenter);
                var textRect = new Rectangle(r.X + S(44), r.Y, r.Width - S(50), r.Height);
                TextRenderer.DrawText(g, label, NavFont, textRect, fg, flags | TextFormatFlags.Left);
            }
        }
        private void SetActiveNav(Button btn)
        {
            var previous = activeButton;
            activeButton = btn;
            previous?.Invalidate();
            btn.Invalidate();
            string raw = btn.Text.Trim();
            int split = raw.IndexOf("  ", StringComparison.Ordinal);
            string title = split > 0 ? raw.Substring(split).Trim() : NavTitleFor(btn);
            lblHeaderTitle.Text = title;
        }
        private string NavTitleFor(Button btn)
        {
            if (btn == btnNavDashboard) return "Dashboard";
            if (btn == btnNavOperations) return "Operations";
            if (btn == btnNavResources) return "Resources";
            if (btn == btnNavFinance) return "Payments";
            if (btn == btnNavMasterData) return "Master Data";
            if (btn == btnNavSettings) return "Settings";
            return btn.Text.Trim();
        }
        private void UnhookBrandLogo(object? sender, FormClosedEventArgs e) => BrandAssets.LogoChanged -= OnBrandLogoChanged;
        /// <summary>Settings changed the company logo: show the new one in the sidebar straight away.</summary>
        private void OnBrandLogoChanged()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action(OnBrandLogoChanged)); return; }
            logoFull = BrandAssets.LogoFor(AppTheme.Sidebar);
            logoMark = BrandAssets.EagleMark(AppTheme.Sidebar);
            if (logoFull == null) { picLogo.Image = null; lblLogoTitle.Visible = true; lblLogoSubtitle.Visible = true; return; }
            UpdateSidebarBrand(panelSidebar.Width < S(120));
        }
        private void UpdateSidebarBrand(bool collapsed)
        {
            if (logoFull == null) return;
            picLogo.Image = collapsed && logoMark != null ? logoMark : logoFull;
            panelLogo.Padding = collapsed ? new Padding(S(10), S(20), S(10), S(16)) : new Padding(S(16), S(14), S(16), S(12));
            lblLogoTitle.Visible = false;
            lblLogoSubtitle.Visible = false;
            foreach (Control c in panelSidebar.Controls) c.Invalidate(true);
            btnThemeToggle?.Invalidate();
        }
        // ---------- Footer: light / dark quick toggle ----------
        private void BuildSidebarFooter()
        {
            pnlSidebarFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = S(62),
                BackColor = AppTheme.Sidebar,
                Padding = new Padding(0, S(8), 0, S(8))
            };
            pnlSidebarFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.SidebarDivider, 1f);
                e.Graphics.DrawLine(pen, 14, 0, pnlSidebarFooter.Width - 14, 0);
            };
            btnThemeToggle = new Button
            {
                Dock = DockStyle.Fill,
                Text = AppTheme.IsDark ? "☀️  Light mode" : "🌙  Dark mode",
                TabStop = false
            };
            StyleNavButton(btnThemeToggle);
            ttSidebar.SetToolTip(btnThemeToggle, AppTheme.IsDark ? "Switch to light mode" : "Switch to dark mode");
            btnThemeToggle.Click += (s, e) =>
            {
                if (!CanLeaveCurrentView()) return;
                var next = AppTheme.IsDark ? ThemeMode.Light : ThemeMode.Dark;
                ThemeManager.Apply(next, ThemeManager.AccentColor, ThemeManager.Sidebar);
                ReloadForTheme(null);
            };
            pnlSidebarFooter.Controls.Add(btnThemeToggle);
            panelSidebar.Controls.Add(pnlSidebarFooter);
        }
        private void UpdateThemeToggleText(bool collapsed)
        {
            if (btnThemeToggle == null) return;
            btnThemeToggle.Text = collapsed
                ? (AppTheme.IsDark ? "☀️" : "🌙")
                : (AppTheme.IsDark ? "☀️  Light mode" : "🌙  Dark mode");
        }
        // ---------- Pop-over styling (nav flyout & notifications) ----------
        private static void PaintPopoverBorder(Control c, PaintEventArgs e)
        {
            using var pen = new Pen(AppTheme.BorderStrong);
            e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
        }
        /// <summary>
        /// Rebuilds the main window so every screen picks up the new theme colours.
        /// The user stays signed in; when <paramref name="settingsTab"/> is given, Settings reopens on that tab.
        /// </summary>
        public bool CanLeaveCurrentView()
        {
            if (panelMainContent.Controls.Count > 0 && panelMainContent.Controls[0] is OperationsHubView oph)
            {
                return oph.CheckAndPromptUnsavedBeforeLeaving();
            }
            return true;
        }
        public void ReloadForTheme(string? settingsTab)
        {
            try
            {
                ThemeManager.ApplyNativeColorMode();
                this.Cursor = Cursors.WaitCursor;
                var here = CurrentLocation();
                var next = new MainForm(currentUser, isThemeReload: true)
                {
                    StartPosition = FormStartPosition.Manual
                };
                if (this.WindowState == FormWindowState.Normal)
                {
                    next.Bounds = this.Bounds;
                }
                else
                {
                    next.Bounds = this.RestoreBounds;
                    next.WindowState = this.WindowState == FormWindowState.Minimized ? FormWindowState.Normal : this.WindowState;
                }
                if (!isSidebarCollapsed) next.SetSidebarExpandedImmediately();
                Replacement = next;
                IsReloading = true;
                next.Show();
                if (!string.IsNullOrEmpty(settingsTab))
                {
                    next.OpenSettingsTab(settingsTab);
                }
                else
                {
                    // Stay on the same module and tab the person was looking at.
                    next.RestoreLocation(here.module, here.tab);
                }
                next.Activate();
                this.Close();
            }
            catch (Exception ex)
            {
                IsReloading = false;
                Replacement = null;
                this.Cursor = Cursors.Default;
                System.Diagnostics.Debug.WriteLine($"[MainForm] ReloadForTheme: {ex.Message}");
                MessageBox.Show("The new theme was saved and will be fully applied the next time you sign in.", "Theme", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        internal void SetSidebarExpandedImmediately()
        {
            isSidebarCollapsed = false;
            panelSidebar.Width = S(SidebarExpandedWidth);
            UpdateSidebarDisplay(false);
        }
        /// <summary>Which module (and tab inside it) is on screen now.</summary>
        private (string? module, string? tab) CurrentLocation()
        {
            try
            {
                var view = panelMainContent.Controls.Count > 0 ? panelMainContent.Controls[0] : null;
                return view switch
                {
                    OperationsHubView o => ("Operations", o.CurrentTabKey),
                    ResourcesHubView r => ("Resources", r.CurrentTabKey),
                    FinanceHubView f => ("Finance", f.CurrentTabKey),
                    MasterDataHubView m => ("MasterData", m.CurrentTabKey),
                    SettingsView sv => ("Settings", sv.CurrentTabKey),
                    _ => (null, null)
                };
            }
            catch { return (null, null); }
        }
        /// <summary>Opens a module and tab captured by <see cref="CurrentLocation"/>. Unknown screens stay on the dashboard.</summary>
        internal void RestoreLocation(string? module, string? tab)
        {
            try
            {
                switch (module)
                {
                    case "Operations": NavigateToOperations(tab ?? "Dispatch"); break;
                    case "Resources": NavigateToResources(tab ?? "Trucks"); break;
                    case "Finance": NavigateToFinance(tab ?? "StatementOfAccount"); break;
                    case "MasterData": NavigateToMasterData(tab ?? "Clients"); break;
                    case "Settings": OpenSettingsTab(tab ?? "DispatchRules"); break;
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainForm] RestoreLocation: {ex.Message}"); }
        }
        public void OpenSettingsTab(string tab)
        {
            if (!SessionContext.CanAccessSettings) return;
            NavigateToView("SettingsView", btnNavSettings, () => new SettingsView(currentUser, tab));
            if (viewCache.TryGetValue("SettingsView", out var v) && v is SettingsView sv)
            {
                sv.SelectTab(tab);
            }
        }
    }
}
