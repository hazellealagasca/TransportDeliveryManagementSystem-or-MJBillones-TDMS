using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class OperationsHubView : UserControl, IRefreshable
    {
        private Button? activeTabBtn = null;
        private readonly Dictionary<string, Button> tabButtons = new Dictionary<string, Button>();
        /// <summary>Key of the tab that is showing now (used to come back to the same screen after a theme change).</summary>
        public string? CurrentTabKey => System.Linq.Enumerable.FirstOrDefault(tabButtons, k => k.Value == activeTabBtn).Key;
        private readonly Dictionary<string, UserControl> cachedViews = new Dictionary<string, UserControl>();

        public Action<int, string, int, DateTime>? OnLogIncidentForward;
        public Action<Models.Delivery>? OnCreateInvoiceForward;

        public OperationsHubView(string defaultTab = "Dispatch")
        {
            InitializeComponent();
            Pal.RemapTree(this);
            SetupTabs();
            UIThemeHelper.ApplyTheme(this);
            SelectTab(defaultTab);
        }

        private void SetupTabs()
        {
            AddTab("Dispatch", "🚚  Loading Schedule & Dispatch", () =>
            {
                var dv = new DispatchManagementView();
                dv.OnRecordTripRequested = (del) =>
                {
                    SelectTab("TripReports");
                };
                dv.OnNavigateToCompletedRequested = () =>
                {
                    SelectTab("Completed");
                };
                dv.OnCreateInvoiceRequested = (del) =>
                {
                    OnCreateInvoiceForward?.Invoke(del);
                };
                return dv;
            });

            // Completed Dispatches is now the "Completed" tab inside Order History.
            AddTab("SpecialTrips", "🌟  Special Trip History", () => new SpecialTripHistoryView());

            AddTab("Incidents", "⚠️  Incident & Damage Logs", () =>
            {
                var iv = new IncidentReportView();
                iv.OnNavigateToTripReportRequested = (delId, dispNum) =>
                {
                    FilterTripReportsByDispatch(dispNum);
                };
                return iv;
            });

            AddTab("TripReports", "📝  Trip Reports", () =>
            {
                var tv = new TripReportView();
                tv.OnLogIncidentRequested = (delId, dispNum, missing, date) =>
                {
                    PrefillIncidentFromTrip(delId, dispNum, missing, date);
                    OnLogIncidentForward?.Invoke(delId, dispNum, missing, date);
                };
                return tv;
            });

            AddTab("History", "📜  Order History", () =>
            {
                var hv = new OrderHistoryView();
                hv.OnIncidentsRequested = (delId, dispNum) => FilterIncidentsByDispatch(dispNum);
                hv.OnTripReportRequested = (delId, dispNum) => FilterTripReportsByDispatch(dispNum);
                hv.OnCreateInvoiceRequested = (del) => OnCreateInvoiceForward?.Invoke(del);
                return hv;
            });
        }

        private void AddTab(string key, string title, Func<UserControl> viewFactory)
        {
            Button btn = new Button
            {
                Text = title,
                Tag = key,
                UseMnemonic = false,
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
                if (activeTabBtn != null && activeTabBtn != btn)
                {
                    if (!CheckAndPromptUnsavedBeforeLeaving()) return;
                }
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

        /// <summary>The cached view behind a tab (created when the tab was first opened).</summary>
        public T? GetView<T>(string key) where T : UserControl => cachedViews.TryGetValue(key, out var v) ? v as T : null;
        public static bool SuppressUnsavedPrompt { get; set; } = false;

        public bool CheckAndPromptUnsavedBeforeLeaving()
        {
            if (SuppressUnsavedPrompt) return true;
            if (pnlContentHost.Controls.Count > 0 && pnlContentHost.Controls[0] is DispatchManagementView dvCurr)
            {
                if (dvCurr.HasUnsavedChanges())
                {
                    // The order is kept as a draft, so there is nothing to lose: save it and let the person go.
                    dvCurr.AutoSaveDraft();
                    try { ToastHelper.ShowToast(FindForm() ?? (Control)this, "Dispatch draft saved. It will be offered when you open the order form again.", ToastType.Info); } catch { }
                }
            }
            else if (pnlContentHost.Controls.Count > 0 && pnlContentHost.Controls[0] is LoadingManagementView lvCurr)
            {
                if (lvCurr.HasUnsavedChanges())
                {
                    var owner = FindForm();
                    if (owner != null) { if (owner.WindowState == FormWindowState.Minimized) owner.WindowState = FormWindowState.Normal; owner.Activate(); }
                    var dr = MessageBox.Show(owner,
                        "Unsaved inputs detected.\n\nDo you want to leave this tab?",
                        "Unsaved Changes",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    return dr == DialogResult.Yes;
                }
            }
            return true;
        }

        public void SelectTab(string key)
        {
            if (key == "Loading") key = "Dispatch";
            bool completed = key == "Completed";
            if (completed) key = "History";
            if (tabButtons.TryGetValue(key, out var btn))
            {
                btn.PerformClick();
            }
            if (completed && cachedViews.TryGetValue("History", out var hv) && hv is OrderHistoryView oh) oh.ShowCompleted();
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

        public void EditDeliveryInDispatch(Delivery delivery)
        {
            SelectTab("Dispatch");
            if (cachedViews.TryGetValue("Dispatch", out var view) && view is DispatchManagementView dsp)
            {
                dsp.EditDeliveryOrder(delivery);
            }
        }

        public void DispatchLoadingSchedule(LoadingSchedule schedule)
        {
            SelectTab("Dispatch");
            if (cachedViews.TryGetValue("Dispatch", out var view) && view is DispatchManagementView dsp)
            {
                dsp.InitiateDispatchForSchedule(schedule);
            }
        }

        public void PreSelectTruckForDispatch(int truckId)
        {
            SelectTab("Dispatch");
            if (cachedViews.TryGetValue("Dispatch", out var view) && view is DispatchManagementView dsp)
            {
                dsp.PreSelectTruck(truckId);
            }
        }

        public void PrefillIncidentFromTrip(int deliveryId, string dispatchNumber, int missingCrates, DateTime tripDate)
        {
            SelectTab("Incidents");
            if (cachedViews.TryGetValue("Incidents", out var view) && view is IncidentReportView inc)
            {
                inc.PrefillFromTripVariance(deliveryId, dispatchNumber, missingCrates, tripDate);
            }
        }

        public void FilterIncidentsByDispatch(string dispatchNumber)
        {
            SelectTab("Incidents");
            if (cachedViews.TryGetValue("Incidents", out var view) && view is IncidentReportView inc)
            {
                inc.FilterByDispatch(dispatchNumber);
            }
        }

        public void OpenTripReportForDispatch(Delivery delivery)
        {
            SelectTab("TripReports");
            if (cachedViews.TryGetValue("TripReports", out var view) && view is TripReportView trip)
            {
                trip.OpenForDispatch(delivery.DispatchNumber);
            }
        }
        public void FilterTripReportsByDispatch(string dispatchNumber)
        {
            SelectTab("TripReports");
            if (cachedViews.TryGetValue("TripReports", out var view) && view is TripReportView trip)
            {
                trip.FilterByDispatch(dispatchNumber);
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
