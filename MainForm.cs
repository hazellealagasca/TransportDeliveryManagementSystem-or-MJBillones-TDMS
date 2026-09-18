using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    public partial class MainForm : Form
    {
        private User currentUser;
        private Button? activeButton;
        private Dictionary<string, UserControl> viewCache = new Dictionary<string, UserControl>();

        private FlowLayoutPanel? pnlKpiFlow;
        private Button? btnKpiTransit;
        private Button? btnKpiPending;
        private Button? btnKpiUnpaid;
        private Button? btnKpiShortcuts;

        public MainForm(User user)
        {
            InitializeComponent();
            UIThemeHelper.EnableDoubleBuffering(this);
            currentUser = user;

            string cleanName = (currentUser.FullName ?? "").Replace(" (Owner)", "").Replace(" (Admin)", "").Trim();
            lblUserInfo.Text = $"👤 {cleanName} • {currentUser.Role}";
            lblUserInfo.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            lblUserInfo.ForeColor = Color.FromArgb(71, 85, 105);
            lblUserInfo.AutoSize = true;

            pnlSearchCenter.Resize += (s, e) => RepositionSearchBar();
            RepositionSearchBar();

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
                            this.Icon = Icon.FromHandle(hIcon);
                        }
                        break;
                    }
                }
            }
            catch { }

            MJdatabase.PerformStartupSanityCheck();
            MJdatabase.OnCategoriesChanged += () => { RefreshCurrentActiveView(); };
            this.FormClosing += (s, e) => { MJdatabase.PerformAutoBackup(); };

            SetupAccessibilityAndKpis();
            SetupSidebarHover();
            OpenDashboard();
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

            lblUserInfo.AutoSize = true;
            lblUserInfo.Margin = new Padding(8, 6, 8, 0);
            lblUserInfo.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            lblUserInfo.ForeColor = Color.FromArgb(71, 85, 105);

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
                        pnlKpiFlow.Controls.Add(btnHeaderSettings);
            pnlKpiFlow.Controls.Add(btnLogout);

            panelHeader.Controls.Add(pnlKpiFlow);
            pnlKpiFlow.SendToBack();
            pnlHeaderLeft.SendToBack();
            pnlSearchCenter.BringToFront();

            pnlSearchCenter.Resize += (s, e) =>
            {
                int avail = pnlSearchCenter.ClientSize.Width - 30;
                if (avail > 160)
                {
                    int searchW = Math.Clamp(avail - 85, 130, 260);
                    txtGlobalSearch.Width = searchW;
                    btnGlobalSearch.Left = txtGlobalSearch.Right + 5;
                    txtGlobalSearch.Visible = true;
                    btnGlobalSearch.Visible = true;
                }
                else if (avail > 60)
                {
                    txtGlobalSearch.Width = avail;
                    btnGlobalSearch.Visible = false;
                }
            };
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
                if (this.InvokeRequired)
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
                private void btnNavLoading_Click(object sender, EventArgs e)
        {
            NavigateToView("LoadingManagementView", btnNavLoading, () =>
            {
                var lv = new LoadingManagementView();
                lv.OnDispatchScheduleRequested = (sch) =>
                {
                    NavigateToView("DispatchManagementView", btnNavDispatch, () => new DispatchManagementView());
                };
                return lv;
            });
        }
                private void btnNavTrucks_Click(object sender, EventArgs e)
        {
            NavigateToView("TruckManagementView", btnNavTrucks, () =>
            {
                var tv = new TruckManagementView();
                tv.OnDispatchTruckRequested = (truck) =>
                {
                    NavigateToView("DispatchManagementView", btnNavDispatch, () => new DispatchManagementView());
                    if (viewCache.TryGetValue("DispatchManagementView", out var cachedView) && cachedView is DispatchManagementView dsp)
                    {
                        dsp.PreSelectTruck(truck.Id);
                    }
                };
                return tv;
            });

            if (viewCache.TryGetValue("TruckManagementView", out var tvCached) && tvCached is TruckManagementView tvExisting)
            {
                tvExisting.OnDispatchTruckRequested = (truck) =>
                {
                    NavigateToView("DispatchManagementView", btnNavDispatch, () => new DispatchManagementView());
                    if (viewCache.TryGetValue("DispatchManagementView", out var cachedView) && cachedView is DispatchManagementView dsp)
                    {
                        dsp.PreSelectTruck(truck.Id);
                    }
                };
            }
        }
        private void btnNavCrew_Click(object sender, EventArgs e) => NavigateToView("CrewManagementView", btnNavCrew, () => new CrewManagementView());
                private void btnNavDispatch_Click(object sender, EventArgs e)
        {
            NavigateToView("DispatchManagementView", btnNavDispatch, () =>
            {
                var dv = new DispatchManagementView();
                dv.OnRecordTripRequested = (del) =>
                {
                    NavigateToView("TripReportView", btnNavTripReports, () => new TripReportView());
                };
                dv.OnCreateInvoiceRequested = (del) =>
                {
                    NavigateToView("BillingView", btnNavBilling, () => new BillingView());
                };
                return dv;
            });
        }
        private void btnNavTripReports_Click(object sender, EventArgs e)
        {
            NavigateToView("TripReportView", btnNavTripReports, () =>
            {
                var trView = new TripReportView();
                trView.OnLogIncidentRequested = (deliveryId, dispatchNumber, missingCrates, tripDate) =>
                {
                    var incView = new IncidentReportView();
                    incView.PrefillFromTripVariance(deliveryId, dispatchNumber, missingCrates, tripDate);
                    LoadView(incView, btnNavIncidents);
                };
                return trView;
            });
        }
        private void btnNavIncidents_Click(object sender, EventArgs e) => NavigateToView("IncidentReportView", btnNavIncidents, () => new IncidentReportView());
                private void btnNavBilling_Click(object sender, EventArgs e)
        {
            NavigateToView("BillingView", btnNavBilling, () =>
            {
                var bv = new BillingView();
                bv.OnRecordPaymentRequested = (inv) =>
                {
                    NavigateToView("CollectionView", btnNavCollections, () => new CollectionView());
                };
                return bv;
            });
        }
        private void btnNavCollections_Click(object sender, EventArgs e) => NavigateToView("CollectionView", btnNavCollections, () => new CollectionView());
        private void btnNavSettings_Click(object sender, EventArgs e) => NavigateToView("SettingsView", btnNavSettings, () => new SettingsView(currentUser));
        private void btnNavRateMatrix_Click(object sender, EventArgs e) => LoadView(new SettingsView(currentUser, "RateMatrix"), btnNavSettings);
        private void btnNavUsers_Click(object sender, EventArgs e) => LoadView(new SettingsView(currentUser, "Users"), btnNavSettings);
        private void btnNavReports_Click(object sender, EventArgs e) => LoadView(new SettingsView(currentUser, "Reports"), btnNavSettings);
        private void btnNavBackup_Click(object sender, EventArgs e) => LoadView(new SettingsView(currentUser, "Backup"), btnNavSettings);

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
            int availableWidth = pnlSearchCenter.ClientSize.Width;
            if (availableWidth <= 0) return;

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
                lstSearchDropdown.Width = Math.Max(260, txtGlobalSearch.Width + btnGlobalSearch.Width + 6);
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
