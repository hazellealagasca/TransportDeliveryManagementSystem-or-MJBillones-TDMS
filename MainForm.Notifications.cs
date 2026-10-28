using System;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    /// <summary>Deep links: clicking a notification opens the exact record it is about, not just the page.</summary>
    public partial class MainForm
    {
        /// <summary>Opens what a notification points at. Falls back to the module page when the record can't be found.</summary>
        public void OpenNotificationTarget(SystemNotification n)
        {
            string module = (n.TargetModule ?? "").Trim().ToLowerInvariant();
            string? id = NotificationLinks.FindRecordId(n);
            try
            {
                if (id != null)
                {
                    var delivery = MJdatabase.GetDeliveries().FirstOrDefault(d => string.Equals(d.DispatchNumber, id, StringComparison.OrdinalIgnoreCase));
                    if (delivery != null)
                    {
                        switch (module)
                        {
                            case "tripreports":
                                if (DispatchGate.IsCompleted(delivery.Status)) OpenTripReportForDispatch(delivery);
                                else { NavigateToOperations("Dispatch"); DispatchDetailsDialog.ShowFor(this, delivery); ToastHelper.ShowToast(this, $"{delivery.DispatchNumber} isn't completed yet — mark it complete to file its trip report.", ToastType.Info); }
                                return;
                            case "incidents":
                                ShowIncidentsForDispatch(delivery.DispatchNumber);
                                return;
                            case "billing":
                            case "invoices":
                                if (SessionContext.CanAccessFinance) { ShowFinanceRecords("Billing", delivery.DispatchNumber); return; }
                                break;
                        }
                        NavigateToOperations("Dispatch");
                        DispatchDetailsDialog.ShowFor(this, delivery);
                        return;
                    }
                    // Any other record the user may see: reuse the global search index, preferring the notification's module.
                    var hits = SearchIndex.Build().Where(i => i.Group != "Go to" && i.Hay.Contains(id.ToLowerInvariant())).ToList();
                    var hit = hits.FirstOrDefault(i => GroupMatches(i.Group, module)) ?? hits.FirstOrDefault();
                    if (hit != null) { hit.Open(); return; }
                }
                // Plate numbers mentioned in truck alerts.
                if (module == "trucks")
                {
                    var truck = MJdatabase.GetTrucks().FirstOrDefault(t => (n.Title + " " + n.Message).IndexOf(t.PlateNumber, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (truck != null) { NavigateToResources("Trucks"); TruckProfileDialog.ShowFor(this, truck); return; }
                }
                OpenModule(module);
                if (id != null) ToastHelper.ShowToast(this, $"Couldn't find {id} any more — opened the list instead.", ToastType.Info);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Notifications] open: {ex.Message}");
                OpenModule(module);
            }
        }
        private static bool GroupMatches(string group, string module) => module switch
        {
            "loading" => group == "Loading schedules",
            "billing" or "invoices" => group == "Invoices",
            "collections" => group == "Receipts",
            "tripreports" => group == "Trip reports",
            "incidents" => group == "Incidents",
            "trucks" => group == "Trucks",
            "crew" => group == "Crew",
            "statements" or "soa" => group == "Statements of account",
            _ => false
        };
        private void OpenModule(string module)
        {
            switch (module)
            {
                case "dispatch": case "loading": NavigateToOperations("Dispatch"); break;
                case "tripreports": NavigateToOperations("TripReports"); break;
                case "incidents": NavigateToOperations("Incidents"); break;
                case "specialtrips": NavigateToOperations("SpecialTrips"); break;
                case "billing": case "invoices": if (SessionContext.CanAccessFinance) NavigateToFinance("Billing"); break;
                case "collections": if (SessionContext.CanAccessFinance) NavigateToFinance("Collections"); break;
                case "statements": case "soa": if (SessionContext.CanAccessFinance) NavigateToFinance("StatementOfAccount"); break;
                case "trucks": NavigateToResources("Trucks"); break;
                case "crew": NavigateToResources("Crew"); break;
                case "settings": if (SessionContext.IsAdmin) btnNavSettings_Click(this, EventArgs.Empty); break;
                default: NavigateHome(); break;
            }
        }
    }
}
