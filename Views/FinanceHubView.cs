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

        public FinanceHubView(string defaultTab = "Billing", Models.Delivery? prefillDelivery = null)
        {
            this.prefillDelivery = prefillDelivery;
            InitializeComponent();
            Smooth.Enable(this);           // H2: double-buffer the hub and its content host
            Smooth.Enable(pnlContentHost);
            NoWheel.Attach(this);          // H1: scrolling the page can never change a number, choice or date in any Payments tab
            InitZoom();                    // Display size: A- / A+ in the header, Ctrl + / - / 0
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
            // The tabs follow the work: a completed trip report is invoiced first, invoices are grouped on a statement,
            // the statement is collected with one receipt, and the settled order goes to history.
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

            AddTab("StatementOfAccount", "📑  Statement of Account", () => new StatementOfAccountView());

            AddTab("Collections", "💵  Collections & Receipts", () => new CollectionView());
            AddTab("History", "📜  Finance History", () => new FinanceHistoryView());
        }

        private readonly Dictionary<string, string> tabTitles = new();
        /// <summary>Shows a count on a tab, e.g. "Billing (6)"; 0 removes it.</summary>
        public void SetTabBadge(string key, int count)
        {
            if (tabButtons.TryGetValue(key, out var b) && tabTitles.TryGetValue(key, out var t))
                b.Text = count > 0 ? $"{t} ({count})" : t;
        }
        private void AddTab(string key, string title, Func<UserControl> viewFactory)
        {
            tabTitles[key] = title;
            Button btn = new Button
            {
                Text = title,
                Tag = key,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                MinimumSize = new Size(140, 30),
                Padding = new Padding(10, 0, 10, 0),
                Margin = new Padding(0, 0, 6, 0),
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
        /// <summary>Nov 6 workflow: right after an SOA is saved, opens its one Collection Receipt (full amount due).</summary>
        public void ProceedToCollection(int soaId) => OpenTab<CollectionView>("Collections")?.PrefillForSoa(soaId);
        public void ShowInvoices(string search) => OpenTab<BillingView>("Billing")?.FilterByText(search);
        /// <summary>Opens a new receipt for a special trip's payment (Mode of Payment and destination pre-filled).</summary>
        public void RecordSpecialTripCollection(Models.SpecialTrip trip) => OpenTab<CollectionView>("Collections")?.PrefillForSpecialTrip(trip);
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
            // Nov 6: leaving a tab with unsaved typing asks first (Yes clears the form).
            if (pnlContentHost.Controls.Count > 0 && pnlContentHost.Controls[0] != view && !FormGuard.ConfirmLeave(pnlContentHost.Controls[0])) return;
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
            // H2: swap the tab AND load its data while nothing is repainted, then paint once (no layers of the old tab bleeding through).
            using (new RedrawFreeze(pnlContentHost))
            {
                pnlContentHost.SuspendLayout();
                pnlContentHost.Controls.Clear();
                Smooth.Enable(view);
                view.Dock = DockStyle.Fill;
                pnlContentHost.Controls.Add(view);
                pnlContentHost.ResumeLayout(true);

                if (view is IRefreshable refreshable)
                {
                    refreshable.RefreshData();
                }
                UiZoom.Apply(view); // Display size (at 100 % this only remembers the original sizes)
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
