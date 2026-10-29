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
        /// <summary>Key of the tab that is showing now (used to come back to the same screen after a theme change).</summary>
        public string? CurrentTabKey => System.Linq.Enumerable.FirstOrDefault(tabButtons, k => k.Value == activeTabBtn).Key;
        private readonly Dictionary<string, UserControl> cachedViews = new Dictionary<string, UserControl>();
        private readonly Models.Delivery? prefillDelivery;

        public FinanceHubView(string defaultTab = "StatementOfAccount", Models.Delivery? prefillDelivery = null)
        {
            this.prefillDelivery = prefillDelivery;
            InitializeComponent();
            Pal.RemapTree(this);
            SetupTabs();
            ApplyAccess();
            SelectTab(prefillDelivery != null ? "Billing" : defaultTab);
            if (prefillDelivery != null && cachedViews.TryGetValue("Billing", out var cached) && cached is BillingView bv)
            {
                bv.PrefillForDelivery(prefillDelivery);
            }
            UIThemeHelper.ApplyTheme(this);
        }

        private void SetupTabs()
        {
            AddTab("StatementOfAccount", "📑  Statement of Account", () => new StatementOfAccountView());

            AddTab("Billing", "🧾  Billing Invoices", () =>
            {
                var bv = new BillingView();
                bv.OnRecordPaymentRequested = (inv) => RecordPaymentFor(inv);
                if (prefillDelivery != null)
                {
                    bv.PrefillForDelivery(prefillDelivery);
                }
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

        /// <summary>The finance hub that contains a control, if any.</summary>
        public static FinanceHubView? Of(Control c)
        {
            for (Control? p = c.Parent; p != null; p = p.Parent) if (p is FinanceHubView hub) return hub;
            return null;
        }
        public T? OpenTab<T>(string key) where T : UserControl
        {
            if (!Permissions.Can("Payments", key)) { Permissions.TellDenied(this, "Payments", key); return null; }
            SelectTab(key);
            return cachedViews.TryGetValue(key, out var v) ? v as T : null;
        }
        public void RecordPaymentFor(Invoice inv) => OpenTab<CollectionView>("Collections")?.PrefillForInvoice(inv);
        public void ShowInvoices(string search) => OpenTab<BillingView>("Billing")?.FilterByText(search);
        public void ShowCollections(string search) => OpenTab<CollectionView>("Collections")?.FilterByText(search);
        public void ShowStatements(string search) => OpenTab<StatementOfAccountView>("StatementOfAccount")?.FilterByText(search);
        /// <summary>Hides the tabs this account may not open (Settings → User Accounts → Access &amp; permissions).</summary>
        private void ApplyAccess()
        {
            foreach (var kv in tabButtons) kv.Value.Visible = Permissions.Can("Payments", kv.Key) && true;
        }
        /// <summary>When the asked-for tab is hidden, explains why and picks the first tab this account can open.</summary>
        private string? AllowedTab(string key)
        {
            if (tabButtons.ContainsKey(key) && !Permissions.Can("Payments", key)) { Permissions.TellDenied(this, "Payments", key); key = ""; }
            if (tabButtons.TryGetValue(key, out var b0) && b0.Visible) return key;
            foreach (var kv in tabButtons) if (kv.Value.Visible) return kv.Key;
            return null;
        }
        public void SelectTab(string key)
        {
            key = AllowedTab(key) ?? "";
            if (tabButtons.TryGetValue(key, out var btn))
            {
                btn.PerformClick();
            }
        }

        private void ActivateTab(Button btn, UserControl view)
        {
            if (activeTabBtn != null)
            {
                activeTabBtn.BackColor = Pal.White;
                activeTabBtn.ForeColor = Pal.C(71, 85, 105);
                activeTabBtn.FlatAppearance.BorderColor = Pal.C(226, 232, 240);
            }

            activeTabBtn = btn;
            activeTabBtn.BackColor = Pal.C(15, 23, 42);
            activeTabBtn.ForeColor = Pal.White;
            activeTabBtn.FlatAppearance.BorderColor = Pal.C(15, 23, 42);
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
