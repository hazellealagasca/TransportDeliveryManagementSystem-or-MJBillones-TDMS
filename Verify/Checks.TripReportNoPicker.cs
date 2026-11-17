using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// Nov 18: the new trip report form shows no "Dispatch order" caption, search box or drop-down. The dispatch (picked by code from the waiting list)
/// is named by the banner; the form still loads its stores. A new form opened with no dispatch is closed again.
/// </summary>
public static partial class Checks
{
    [Check]
    public static string TripReportNoPicker_PickerHidden_BannerNamesDispatch_FormStillWorks_EmptyFormClosed()
    {
        Fresh();
        int id = P4DeliveryWithStores();
        string number = NumberOf(id);
        return WithTripReportView((view, host, problems) =>
        {
            var cb = FieldOf<ComboBox>(view, "cbDelivery")!;
            var search = FieldOf<TextBox>(view, "txtPickSearch")!;
            var caption = FieldOf<Label>(view, "lblDel")!;
            var banner = FieldOf<Label>(view, "lblDispatchInfoBanner")!;
            var grid = FieldOf<DataGridView>(view, "gridStores")!;
            var form = FieldOf<Control>(view, "pnlForm")!;

            // no dispatch: the empty form is closed again
            typeof(TripReportView).GetMethod("OpenNewTripReport", NonPub)!.Invoke(view, null);
            Pump(300);
            bool closed = (bool)typeof(TripReportView).GetMethod("CloseNewFormIfNoDispatch", NonPub)!.Invoke(view, null)!;
            Pump(200);
            if (!closed || form.Visible) problems.Add("a new form with no dispatch was left open");

            typeof(TripReportView).GetMethod("OpenNewTripReport", NonPub)!.Invoke(view, null);
            Pump(500);
            PickMain(view, number);
            if (!form.Visible) problems.Add("the form is not shown after picking a dispatch");
            if (cb.Visible) problems.Add("the dispatch drop-down is visible");
            if (search.Visible) problems.Add("the search box is visible");
            if (caption.Visible) problems.Add("the 'Dispatch order' caption is visible");
            if (!banner.Visible) problems.Add("the dispatch banner is not visible");
            if (!banner.Text.StartsWith("Dispatch " + number)) problems.Add("banner does not start with the dispatch number: " + banner.Text);
            if (grid.Rows.Count != 2) problems.Add($"expected 2 store rows, found {grid.Rows.Count}");
            if ((bool)typeof(TripReportView).GetMethod("CloseNewFormIfNoDispatch", NonPub)!.Invoke(view, null)!) problems.Add("the guard closed a form that has a dispatch");
            return "";
        });
    }
}
