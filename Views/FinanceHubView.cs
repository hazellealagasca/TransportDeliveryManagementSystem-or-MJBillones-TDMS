using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class FinanceHubView : UserControl, IRefreshable
    {
        private Button? activeTabBtn = null;
        private readonly Dictionary<string, Button> tabButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, UserControl> cachedViews = new Dictionary<string, UserControl>();

        public FinanceHubView(string defaultTab = "Billing")
        {
            InitializeComponent();
            SetupTabs();
            SelectTab(defaultTab);
            UIThemeHelper.ApplyTheme(this);
        }

        private void SetupTabs()
        {
            AddTab("Billing", "🧾  Billing Invoices", () =>
            {
                var bv = new BillingView();
                bv.OnRecordPaymentRequested = (inv) =>
                {
                    SelectTab("Collections");
                };
                return bv;
            });

            AddTab("Collections", "💵  Collections & Receipts", () => new CollectionView());
            AddTab("History", "📜  Finance History", () => new ModuleHistoryView("Billing & Collections History", new[] { "Invoices", "CollectionReceipts", "StatementsOfAccount" }, isAdminOnly: true));
        }

        private void AddTab(string key, string title, Func<UserControl> viewFactory)
        {
            Button btn = new Button
            {
                Text = title,
                Tag = key,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                MinimumSize = new Size(180, 36),
                Padding = new Padding(12, 0, 12, 0),
                Margin = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);

            btn.Click += (s, e) =>
            {
                if (!cachedViews.TryGetValue(key, out var view))
                {
                    view = viewFactory();
                    view.Dock = DockStyle.Fill;
                    cachedViews[key] = view;
                }
                ActivateTab(btn, view);
            };

            flowNavTabs.Controls.Add(btn);
            tabButtons[key] = btn;
        }

        public void SelectTab(string key)
        {
            if (tabButtons.TryGetValue(key, out var btn))
            {
                btn.PerformClick();
            }
        }

        private void ActivateTab(Button btn, UserControl view)
        {
            if (activeTabBtn != null)
            {
                activeTabBtn.BackColor = Color.White;
                activeTabBtn.ForeColor = Color.FromArgb(71, 85, 105);
                activeTabBtn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            }

            activeTabBtn = btn;
            activeTabBtn.BackColor = Color.FromArgb(190, 18, 60);
            activeTabBtn.ForeColor = Color.White;
            activeTabBtn.FlatAppearance.BorderColor = Color.FromArgb(159, 18, 57);

            pnlContentHost.SuspendLayout();
            pnlContentHost.Controls.Clear();
            view.Dock = DockStyle.Fill;
            pnlContentHost.Controls.Add(view);
            pnlContentHost.ResumeLayout(true);

            if (view is IRefreshable refreshable)
            {
                refreshable.RefreshData();
            }
        }


        public void RefreshData()
        {
            if (pnlContentHost.Controls.Count > 0 && pnlContentHost.Controls[0] is IRefreshable active)
            {
                active.RefreshData();
            }
        }
    }
}
