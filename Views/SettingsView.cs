using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public class SettingsView : UserControl, IRefreshable
    {
        private User? currentUser;
        private Panel pnlTabStrip = null!;
        private Panel pnlContent = null!;
        private Button? activeTabButton;
        private Dictionary<string, Button> tabButtons = new Dictionary<string, Button>();

        public SettingsView(User? user = null, string initialTab = "RateMatrix")
        {
            currentUser = user ?? SessionContext.ActiveUser;
            InitializeComponent();
            UIThemeHelper.ApplyTheme(this);
            this.AutoScroll = false;
            this.AutoScrollMinSize = Size.Empty;
            SelectTab(initialTab);
        }

        public void RefreshData()
        {
            try
            {
                if (pnlContent.Controls.Count > 0 && pnlContent.Controls[0] is IRefreshable refreshable)
                {
                    refreshable.RefreshData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to refresh settings view: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Dock = DockStyle.Fill;
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(24, 16, 24, 14),
                BackColor = Color.White
            };

            TableLayoutPanel tblHeaderFlow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2
            };
            tblHeaderFlow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblHeaderFlow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tblHeaderFlow.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label lblTitle = new Label
            {
                Text = "⚙️ System Settings",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            Label lblSubtitle = new Label
            {
                Text = "Configure hauling transport rates, dropdown categories & client profiles, user account permissions, and database backup.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Margin = new Padding(0)
            };

            tblHeaderFlow.Controls.Add(lblTitle, 0, 0);
            tblHeaderFlow.Controls.Add(lblSubtitle, 0, 1);
            pnlHeader.Controls.Add(tblHeaderFlow);

            FlowLayoutPanel flowTabs = new FlowLayoutPanel
            {
                Name = "flowTabs",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(24, 10, 24, 10),
                BackColor = Color.FromArgb(241, 245, 249),
                Margin = new Padding(0)
            };
            pnlTabStrip = flowTabs;
            pnlTabStrip.AutoSize = true;

            AddTabButton(flowTabs, "RateMatrix", "🏷️  Rate Matrix", (s, e) => LoadChildView(new RateMatrixView(), (Button)s!));
            AddTabButton(flowTabs, "Categories", "🏢  Categories & Clients", (s, e) => LoadChildView(new CategoryManagementView(), (Button)s!));
            AddTabButton(flowTabs, "DispatchRules", "🚚  Dispatch Rules", (s, e) => LoadChildView(new DispatchRulesSettingsView(), (Button)s!));

            bool isAdmin = (currentUser != null && (currentUser.Role == "Admin" || currentUser.Role == "Owner/Admin")) || SessionContext.IsAdmin;
            if (isAdmin)
            {
                AddTabButton(flowTabs, "Users", "👤  User Accounts", (s, e) => LoadChildView(new UserManagementView(), (Button)s!));
            }

            AddTabButton(flowTabs, "Reports", "📈  Reports & Analytics", (s, e) => LoadChildView(new ReportsView(), (Button)s!));
            AddTabButton(flowTabs, "Customization", "🎨  Preferences & Customization", (s, e) => LoadChildView(new SystemCustomizationView(currentUser), (Button)s!));

            if (isAdmin)
            {
                AddTabButton(flowTabs, "SystemHistory", "📜  System Change History", (s, e) => LoadChildView(new ModuleHistoryView("Master System Change Tracking (Add, Update, Delete)", null, isAdminOnly: true), (Button)s!));
                AddTabButton(flowTabs, "Backup", "💾  Backup & Restore", (s, e) => LoadChildView(new BackupRestoreView(), (Button)s!));
                AddTabButton(flowTabs, "Audit", "🛡️  Security Audit Logs", (s, e) => LoadChildView(new SecurityAuditView(), (Button)s!));
            }

            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                AutoScroll = true
            };

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlTabStrip);
            this.Controls.Add(pnlHeader);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void AddTabButton(FlowLayoutPanel container, string tabKey, string label, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = label,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                MinimumSize = new Size(160, 36),
                Padding = new Padding(12, 0, 12, 0),
                Margin = new Padding(0, 0, 8, 6),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                Cursor = Cursors.Hand,
                Tag = tabKey
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btn.Click += onClick;
            container.Controls.Add(btn);
            tabButtons[tabKey] = btn;
        }

        public void SelectTab(string tabKey)
        {
            if (tabButtons.TryGetValue(tabKey, out var btn))
            {
                btn.PerformClick();
            }
            else if (tabButtons.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsView] Requested tab '{tabKey}' not found or unauthorized. Falling back to default.");
                tabButtons.Values.First().PerformClick();
            }
        }

        private void LoadChildView(UserControl child, Button btn)
        {
            try
            {
                if (activeTabButton != null)
                {
                    activeTabButton.BackColor = Color.White;
                    activeTabButton.ForeColor = Color.FromArgb(71, 85, 105);
                    activeTabButton.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
                }

                activeTabButton = btn;
                activeTabButton.BackColor = Color.FromArgb(190, 18, 60);
                activeTabButton.ForeColor = Color.White;
                activeTabButton.FlatAppearance.BorderColor = Color.FromArgb(159, 18, 57);

                foreach (Control c in pnlContent.Controls)
                {
                    c.Dispose();
                }
                pnlContent.Controls.Clear();
                child.Dock = DockStyle.Fill;
                pnlContent.Controls.Add(child);
                if (child is IRefreshable refreshable)
                {
                    refreshable.RefreshData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load configuration view: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
