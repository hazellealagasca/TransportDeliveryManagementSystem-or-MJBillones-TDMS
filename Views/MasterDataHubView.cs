using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class MasterDataHubView : UserControl, IRefreshable
    {
        private Button? activeTabBtn = null;
        private readonly Dictionary<string, Button> tabButtons = new Dictionary<string, Button>();
        /// <summary>Key of the tab that is showing now (used to come back to the same screen after a theme change).</summary>
        public string? CurrentTabKey => System.Linq.Enumerable.FirstOrDefault(tabButtons, k => k.Value == activeTabBtn).Key;
        private readonly Dictionary<string, UserControl> cachedViews = new Dictionary<string, UserControl>();

        public MasterDataHubView(string defaultTab = "Clients")
        {
            InitializeComponent();
            Pal.RemapTree(this);
            SetupTabs();
            SelectTab(defaultTab);
            UIThemeHelper.ApplyTheme(this);
        }

        private void SetupTabs()
        {
            AddTab("Clients", "🏢  Clients", () => new ClientManagementView());
            AddTab("Routes", "🗺️  Delivery Routes", () => new DeliveryRoutesView());
            AddTab("Rates", "🏷️  Rate Matrix", () => new RateMatrixView());
            AddTab("History", "📜  Master Data History", () => new ModuleHistoryView("Master Data History", new[] { "Clients", "DeliveryRoutes", "ClientStores", "RateMatrix" }, isAdminOnly: true));
            // Settings → Features & Pop-ups can hide the Rate Matrix; its rates stay saved for when it is turned back on.
            Features.Bind(this, () =>
            {
                ApplyAccess();
                if (!Features.On(Features.RateMatrix) && Equals(activeTabBtn?.Tag, "Rates")) SelectTab("Clients");
            }, Features.RateMatrix);
        }

        private void AddTab(string key, string title, Func<UserControl> viewFactory)
        {
            Button btn = new Button
            {
                Text = title,
                Tag = key,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                MinimumSize = new Size(160, 32),
                Padding = new Padding(12, 0, 12, 0),
                Margin = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                BackColor = Pal.White,
                ForeColor = Pal.C(71, 85, 105),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Pal.C(226, 232, 240);
            btn.Click += (s, e) =>
            {
                SelectTab(key);
            };

            tabButtons[key] = btn;
            flowNavTabs.Controls.Add(btn);

            if (!cachedViews.ContainsKey(key))
            {
                var view = viewFactory();
                view.Dock = DockStyle.Fill;
                cachedViews[key] = view;
            }
        }

        /// <summary>Opens the Clients tab on one client's profile.</summary>
        public void OpenClient(string companyName)
        {
            SelectTab("Clients");
            if (cachedViews.TryGetValue("Clients", out var v) && v is ClientManagementView cv) cv.OpenClientByName(companyName);
        }
        /// <summary>Hides the tabs this account may not open (Settings → User Accounts → Access &amp; permissions).</summary>
        private void ApplyAccess()
        {
            foreach (var kv in tabButtons) kv.Value.Visible = Permissions.Can("MasterData", kv.Key) && FeatureAllows(kv.Key);
        }
        /// <summary>When the asked-for tab is hidden, explains why and picks the first tab this account can open.</summary>
        private string? AllowedTab(string key)
        {
            if (tabButtons.ContainsKey(key) && !Permissions.Can("MasterData", key)) { Permissions.TellDenied(this, "MasterData", key); key = ""; }
            if (tabButtons.TryGetValue(key, out var b0) && b0.Visible) return key;
            foreach (var kv in tabButtons) if (kv.Value.Visible) return kv.Key;
            return null;
        }
        private static bool FeatureAllows(string key) => key != "Rates" || Features.On(Features.RateMatrix);
        public void SelectTab(string key)
        {
            if (!tabButtons.ContainsKey(key) || !cachedViews.ContainsKey(key))
                return;

            if (key == "Rates" && !Features.On(Features.RateMatrix)) { Features.TellOff(this, "The Rate Matrix"); key = "Clients"; }
            var allowed = AllowedTab(key);
            if (allowed == null || !cachedViews.ContainsKey(allowed)) return;
            key = allowed;
            foreach (var kvp in tabButtons)
            {
                bool isSelected = kvp.Key == key;
                var btn = kvp.Value;
                if (isSelected)
                {
                    btn.BackColor = Pal.C(15, 23, 42);
                    btn.ForeColor = Pal.White;
                    btn.FlatAppearance.BorderColor = Pal.C(15, 23, 42);
                    activeTabBtn = btn;
                }
                else
                {
                    btn.BackColor = Pal.White;
                    btn.ForeColor = Pal.C(71, 85, 105);
                    btn.FlatAppearance.BorderColor = Pal.C(226, 232, 240);
                }
            }

            pnlContentHost.SuspendLayout();
            pnlContentHost.Controls.Clear();
            var targetView = cachedViews[key];
            targetView.Dock = DockStyle.Fill;
            pnlContentHost.Controls.Add(targetView);
            pnlContentHost.ResumeLayout(true);

            if (targetView is IRefreshable refreshable)
            {
                refreshable.RefreshData();
            }
        }

        public void RefreshData()
        {
            if (activeTabBtn?.Tag is string key && cachedViews.TryGetValue(key, out var view))
            {
                if (view is IRefreshable refreshable)
                {
                    refreshable.RefreshData();
                }
            }
        }

        public DeliveryRoutesView? GetDeliveryRoutesView()
        {
            if (cachedViews.TryGetValue("Routes", out var v) && v is DeliveryRoutesView drv)
            {
                return drv;
            }
            return null;
        }
    }
}
