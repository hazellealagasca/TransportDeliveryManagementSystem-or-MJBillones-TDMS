using System;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 14: an Individual client has one automatic store, its recipient. Choosing such a client on an order puts that one store in the
    /// stores table (so the order has one drop and one DR), and the table says "Recipient" where it says "Store".
    /// </summary>
    public partial class DispatchManagementView
    {
        /// <summary>Applies the "Recipient" wording and fills the one store when the client is an Individual client and the table is still empty.</summary>
        private void ApplyIndividualClient(string client, bool fillStore)
        {
            if (gridRouteStores == null) return;
            bool individual = MJdatabase.IsIndividualClient(client);
            SetRecipientHeaders(individual);
            if (!individual || !fillStore) return;
            bool anyFilled = gridRouteStores.Rows.Cast<DataGridViewRow>().Any(r => !r.IsNewRow
                && (string.IsNullOrWhiteSpace(r.Cells["colStoreName"].Value?.ToString()) == false || string.IsNullOrWhiteSpace(r.Cells["colStoreCode"].Value?.ToString()) == false));
            if (anyFilled) return;
            var recipient = MJdatabase.GetIndividualStore(client);
            if (recipient == null) return;
            isUpdatingRouteStores = true;
            try
            {
                gridRouteStores.Rows.Clear();
                int i = gridRouteStores.Rows.Add();
                var row = gridRouteStores.Rows[i];
                row.Cells["colStoreCode"].Value = recipient.StoreCode;
                row.Cells["colStoreName"].Value = recipient.StoreName;
                SetStoreRowType(row, false);
                if (pnlRouteStoresBreakdown != null) pnlRouteStoresBreakdown.Visible = true;
                SizeStoreBreakdown(gridRouteStores.Rows.Count);
            }
            finally { isUpdatingRouteStores = false; }
            ShowStoreWarning(null);
        }

        private void SetRecipientHeaders(bool individual)
        {
            if (gridRouteStores == null) return;
            if (gridRouteStores.Columns.Contains("colStoreCode")) gridRouteStores.Columns["colStoreCode"]!.HeaderText = individual ? "Recipient Code" : "Store Code";
            if (gridRouteStores.Columns.Contains("colStoreName")) gridRouteStores.Columns["colStoreName"]!.HeaderText = individual ? "Recipient" : "Store Name";
        }
    }
}
