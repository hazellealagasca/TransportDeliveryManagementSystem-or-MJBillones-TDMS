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
        private readonly Dictionary<string, UserControl> cachedViews = new Dictionary<string, UserControl>();

        public MasterDataHubView(string defaultTab = "Rates")
        {
            InitializeComponent();
            SetupTabs();
            SelectTab(defaultTab);
            UIThemeHelper.ApplyTheme(this);
        }

        private void SetupTabs()
        {
            AddTab("Rates", "📈  Rate Matrix", () => new RateMatrixView());
            AddTab("Categories", "🏢  Categories & Clients", () => new CategoryManagementView());
            AddTab("History", "📜  Master Data History", () => new ModuleHistoryView("Master Data & Rates History", new[] { "RateMatrix", "LookupCategories", "Clients" }, isAdminOnly: true));
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
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);

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

        public void SelectTab(string key)
        {
            if (!tabButtons.ContainsKey(key) || !cachedViews.ContainsKey(key))
                return;

            foreach (var kvp in tabButtons)
            {
                bool isSelected = kvp.Key == key;
                var btn = kvp.Value;
                if (isSelected)
                {
                    btn.BackColor = Color.FromArgb(15, 23, 42);
                    btn.ForeColor = Color.White;
                    btn.FlatAppearance.BorderColor = Color.FromArgb(15, 23, 42);
                    activeTabBtn = btn;
                }
                else
                {
                    btn.BackColor = Color.White;
                    btn.ForeColor = Color.FromArgb(71, 85, 105);
                    btn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
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
    }
}
