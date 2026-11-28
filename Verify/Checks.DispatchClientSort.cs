using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 28: the Dispatch list's Sort picker has Client (A-Z) / Client (Z-A), case-insensitive, newest first within one client.</summary>
public static partial class Checks
{
    [Check]
    public static string DispatchClientSort_PickerSortsByClientName()
    {
        Fresh(); MJdatabase.RunOneTimeMigrations();
        // Dates run opposite to the names so a date sort cannot pass by accident.
        int a = Seed.Delivery("zeta foods", "Scheduled");
        int b = Seed.Delivery("ALPHA Mart", "Scheduled");
        int c = Seed.Delivery("Mango Store", "Scheduled");
        Exec("UPDATE Deliveries SET DispatchDate=@d WHERE Id=@i", ("@d", DateTime.Today.AddDays(1).ToString("yyyy-MM-dd")), ("@i", a));
        Exec("UPDATE Deliveries SET DispatchDate=@d WHERE Id=@i", ("@d", DateTime.Today.AddDays(3).ToString("yyyy-MM-dd")), ("@i", b));
        Exec("UPDATE Deliveries SET DispatchDate=@d WHERE Id=@i", ("@d", DateTime.Today.AddDays(2).ToString("yyyy-MM-dd")), ("@i", c));
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var view = DcMakeView(out var host);
            var grid = DcGet<DataGridView>(view, "gridDispatch");
            var chip = DcGet<ChoiceChip>(view, "cbSortBy");
            view.RefreshData(); Pump(600);
            var bad = new List<string>();
            string Order() => string.Join(",", grid.Rows.Cast<DataGridViewRow>().Where(r => r.DataBoundItem is Delivery).Select(r => ((Delivery)r.DataBoundItem!).ClientName));
            chip.SelectedIndex = IndexOfLabel(chip, "Client (A–Z)"); Pump(500);
            if (Order() != "ALPHA Mart,Mango Store,zeta foods") bad.Add("Client (A-Z) gave: " + Order());
            chip.SelectedIndex = IndexOfLabel(chip, "Client (Z–A)"); Pump(500);
            if (Order() != "zeta foods,Mango Store,ALPHA Mart") bad.Add("Client (Z-A) gave: " + Order());
            host.Close();
            return string.Join("; ", bad);
        });
    }

    private static int IndexOfLabel(ChoiceChip chip, string label)
    {
        var f = typeof(ChoiceChip).GetField("options", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var opts = ((IEnumerable<string>)f!.GetValue(chip)!).ToList();
        return opts.IndexOf(label);
    }
}
