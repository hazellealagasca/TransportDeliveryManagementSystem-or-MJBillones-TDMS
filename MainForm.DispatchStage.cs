using System;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    /// <summary>Nov 28: the "For ..." status pills of the dispatch list open the screen where the next step is done.</summary>
    public partial class MainForm
    {
        /// <summary>Opens the Payments tab (the same permission check the tab itself makes); null when this account may not open it.</summary>
        private FinanceHubView? OpenFinanceTab(string tab)
        {
            if (!Permissions.Can("Payments", tab)) { Permissions.TellDenied(this, "Payments", tab); return null; }
            NavigateToFinance(tab);
            return viewCache.TryGetValue("FinanceHubView", out var v) ? v as FinanceHubView : null;
        }

        /// <summary>What clicking a "For ..." status does: trip report, new statement with this trip ticked, invoice for its statement, or collection receipt.</summary>
        public void OpenNextStepForDispatch(Delivery delivery, DispatchStageInfo stage)
        {
            switch (stage.Kind)
            {
                case DispatchStageKind.ForTripReport:
                    OpenTripReportForDispatch(delivery);
                    break;
                case DispatchStageKind.ForSoa:
                    OpenFinanceTab("StatementOfAccount")?.OpenTab<StatementOfAccountView>("StatementOfAccount")?.StartSoaForTrip(delivery);
                    break;
                case DispatchStageKind.ForInvoice:
                    if (stage.SoaId > 0) OpenFinanceTab("Billing")?.InvoiceStatement(stage.SoaId);
                    break;
                case DispatchStageKind.ForCollection:
                    if (stage.SoaId > 0) { OpenFinanceTab("Collections")?.ProceedToCollection(stage.SoaId); break; }
                    var inv = MJdatabase.GetInvoices().FirstOrDefault(i => i.Id == stage.InvoiceId);   // an invoice that is on no statement
                    if (inv != null) OpenFinanceTab("Collections")?.RecordPaymentFor(inv);
                    break;
            }
        }
    }
}
