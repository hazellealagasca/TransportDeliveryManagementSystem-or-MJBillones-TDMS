using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class ResourcesHubView : UserControl, IRefreshable
    {
        private Button? activeTabBtn = null;
        private readonly Dictionary<string, Button> tabButtons = new Dictionary<string, Button>();
        /// <summary>Key of the tab that is showing now (used to come back to the same screen after a theme change).</summary>
        public string? CurrentTabKey => System.Linq.Enumerable.FirstOrDefault(tabButtons, k => k.Value == activeTabBtn).Key;
        private readonly Dictionary<string, UserControl> cachedViews = new Dictionary<string, UserControl>();

        public Action<Models.Truck>? OnDispatchTruckForward;

        public ResourcesHubView(string defaultTab = "Trucks")
        {
            InitializeComponent();
            Pal.RemapTree(this);
            SetupTabs();
            UIThemeHelper.ApplyTheme(this);
            SelectTab(defaultTab);
        }

        private void SetupTabs()
        {
            AddTab("Trucks", "🚛  Trucks", () =>
            {
                var tv = new TruckManagementView();
                tv.OnDispatchTruckRequested = (truck) =>
                {
                    OnDispatchTruckForward?.Invoke(truck);
                };
                return tv;
            });

            AddTab("Crew", "👨‍✈️  Crew Management", () =>
            {
                var cv = new CrewManagementView();
                cv.OnViewCrewHistoryRequested = (crewId) =>
                {
                    ViewCrewHistory(crewId);
                };
                return cv;
            });

            AddTab("CrewHistory", "📋  Crew Trip History", () => new CrewTripHistoryView());
            AddTab("History", "📜  Resource History", () => new ModuleHistoryView("Trucks & Crew Resource History", new[] { "Trucks", "Crews" }, isAdminOnly: true));
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

        public void ViewCrewHistory(int crewId)
        {
            SelectTab("CrewHistory");
            if (cachedViews.TryGetValue("CrewHistory", out var view) && view is CrewTripHistoryView chv)
            {
                chv.SelectCrew(crewId);
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
