using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 10: each trip report carries its type of delivery (REF or DRY). The Statement of Account prints it for the trip
    /// (it is not typed again there).
    /// </summary>
    public partial class TripReportView
    {
        private ComboBox cbTripType = null!;

        private Control BuildTripTypeRow()
        {
            var row = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, BackColor = AppTheme.Surface, Margin = new Padding(0), Padding = new Padding(0, Dpi(8), 0, 0) };
            var lbl = new Label { AutoSize = true, Text = "Type of delivery *", Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, Dpi(6), Dpi(10), 0), UseMnemonic = false };
            cbTripType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Width = Dpi(110), Margin = new Padding(0) };
            cbTripType.Items.AddRange(MJdatabase.TripTypes);
            cbTripType.SelectedIndex = 0;
            row.Controls.Add(lbl);
            row.Controls.Add(cbTripType);
            return row;
        }

        private void ResetTripType() { if (cbTripType != null && cbTripType.Items.Count > 0) cbTripType.SelectedIndex = 0; }

        /// <summary>Shows the type saved on the trip report being opened (REF when none was chosen).</summary>
        private void ShowTripType(TripReport tr)
        {
            if (cbTripType == null) return;
            string t = MJdatabase.GetTripReportDeliveryType(tr.Id);
            int i = cbTripType.Items.IndexOf(t);
            cbTripType.SelectedIndex = i >= 0 ? i : 0;
        }

        /// <summary>Saves the chosen type on the trip report that was just saved.</summary>
        private void SaveTripType(int deliveryId, int specialTripId)
        {
            if (cbTripType == null) return;
            MJdatabase.SetTripReportDeliveryType(deliveryId, specialTripId, cbTripType.Text);
        }
    }
}
