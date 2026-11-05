using System;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    public partial class MainForm
    {
        /// <summary>
        /// Nov 6, 2026: the special trip form's flow bar (Special trip → Trip report → Service invoice → SOA → Collection receipt)
        /// opens the next step's screen with this trip already picked, or shows the record that step made.
        /// </summary>
        public void GoToSpecialTripStep(string step, SpecialTrip trip, string? existingNumber)
        {
            try
            {
                FinanceHubView? Finance(string tab)
                {
                    NavigateToFinance(tab);
                    return viewCache.TryGetValue("FinanceHubView", out var fv) ? fv as FinanceHubView : null;
                }
                switch (step)
                {
                    case "report":
                        NavigateToOperations("TripReports");
                        GetOperationsView<TripReportView>("TripReports")?.OpenForDispatch(trip.OrderReference);
                        break;
                    case "invoice":
                        if (!string.IsNullOrWhiteSpace(existingNumber)) ShowFinanceRecords("Billing", existingNumber);
                        else Finance("Billing")?.OpenTab<BillingView>("Billing")?.PrefillForDelivery(BillingView.SpecialTripStandIn(trip));
                        break;
                    case "soa":
                        if (!string.IsNullOrWhiteSpace(existingNumber)) ShowFinanceRecords("StatementOfAccount", existingNumber);
                        else Finance("StatementOfAccount")?.OpenTab<StatementOfAccountView>("StatementOfAccount")?.StartSoaFor(trip.Company, trip.DispatchDate.Date, trip.DispatchDate.Date);
                        break;
                    case "receipt":
                        if (!string.IsNullOrWhiteSpace(existingNumber)) ShowFinanceRecords("Collections", existingNumber);
                        else RecordSpecialTripCollection(trip);
                        break;
                }
            }
            catch (Exception ex) { ErrLog.Error("MainForm.GoToSpecialTripStep", ex); }
        }
    }
}
