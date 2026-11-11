using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>Fix 1: stores carry an address (additive column). New stores need one, older stores without one keep working, and the address survives rename, archive and restore.</summary>
public static partial class Checks
{
    private static ClientStore StoreByCode(string client, string code) => MJdatabase.GetClientStores(client, includeArchived: true).First(s => s.StoreCode == code);

    [Check]
    public static string StoreAddress_SaveAndReadBack_ThroughEveryWriter()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        var bad = new List<string>();
        var saved = new ClientStore { ClientCompanyName = "CLIENT A", StoreCode = "A-1", StoreName = "Store One", Address = "12 Rizal St., Tarlac City" };
        MJdatabase.SaveClientStore(saved);
        if (StoreByCode("CLIENT A", "A-1").Address != "12 Rizal St., Tarlac City") bad.Add("SaveClientStore address not read back");
        MJdatabase.RegisterCompanyStore("CLIENT A", "A-2", "Store Two", "  45   Mabini Ave. ");
        if (StoreByCode("CLIENT A", "A-2").Address != "45 Mabini Ave.") bad.Add("RegisterCompanyStore address: '" + StoreByCode("CLIENT A", "A-2").Address + "'");
        MJdatabase.RegisterCompanyStore("CLIENT A", "A-3", "Store Three"); // the address is optional at the writer
        if (StoreByCode("CLIENT A", "A-3").Address != "") bad.Add("an address appeared from nowhere");
        // The route writer takes it too (a route-less pick goes to the company pool through the same call).
        MJdatabase.RegisterClientStore("CLIENT A", "A-4", "Store Four", "", "7 Burgos St.");
        if (StoreByCode("CLIENT A", "A-4").Address != "7 Burgos St.") bad.Add("RegisterClientStore address");
        var fromBook = StoreAddressBook.Get("CLIENT A", "A-1", "");
        if (fromBook != "12 Rizal St., Tarlac City") bad.Add("StoreAddressBook by code: '" + fromBook + "'");
        if (StoreAddressBook.Get("CLIENT A", "", "store two") != "45 Mabini Ave.") bad.Add("StoreAddressBook by name");
        return string.Join("; ", bad);
    }

    [Check]
    public static string StoreAddress_OldStoresWithNoAddress_StillWork_AndAreCounted()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        int baseline = MJdatabase.CountActiveStoresWithoutAddress(); // the copied real database already has older stores without one
        // An older store: inserted the way it was before the column existed (Address left to its default).
        Exec("INSERT INTO ClientStores (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute) VALUES (0, 'CLIENT A', 'OLD-1', 'Old Store', '')");
        Exec("INSERT INTO ClientStores (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute, Address) VALUES (0, 'CLIENT A', 'NEW-1', 'New Store', '', '1 Plaza St.')");
        Exec("INSERT INTO ClientStores (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute, IsArchived) VALUES (0, 'CLIENT A', 'OLD-2', 'Archived Old', '', 1)");
        var bad = new List<string>();
        var old = StoreByCode("CLIENT A", "OLD-1");
        if (old.Address != "") bad.Add("old store address is '" + old.Address + "'");
        if (StoreAddressBook.Get("CLIENT A", "OLD-1", "") != "") bad.Add("old store shows an address");
        if (MJdatabase.FindStoresAnyClient("OLD-1").Count != 1) bad.Add("old store not found by the registry");
        if (MJdatabase.CountActiveStoresWithoutAddress() != baseline + 1) bad.Add("stores without an address counted as " + MJdatabase.CountActiveStoresWithoutAddress() + " (" + (baseline + 1) + " expected: archived and filled-in ones do not count)");
        // An old store can still be edited and saved without an address (optional for existing stores).
        old.StoreName = "Old Store Renamed";
        MJdatabase.SaveClientStore(old);
        if (StoreByCode("CLIENT A", "OLD-1").StoreName != "Old Store Renamed") bad.Add("an old store could not be saved without an address");
        return string.Join("; ", bad);
    }

    [Check]
    public static string StoreAddress_KeptOnRename_ArchiveAndRestore()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        MJdatabase.RegisterCompanyStore("CLIENT A", "R-1", "Rename Me", "9 Quezon Blvd.");
        var bad = new List<string>();
        var st = StoreByCode("CLIENT A", "R-1");
        st.StoreName = "Renamed Store"; st.StoreCode = "R-2";
        MJdatabase.SaveClientStore(st);
        var after = StoreByCode("CLIENT A", "R-2");
        if (after.Address != "9 Quezon Blvd.") bad.Add("rename lost the address: '" + after.Address + "'");
        MJdatabase.ArchiveClientStore(after.Id);
        var archived = MJdatabase.GetArchivedItems().FirstOrDefault(i => i.Kind == "Store" && i.Id == after.Id);
        if (archived == null || archived.Address != "9 Quezon Blvd.") bad.Add("the archive list does not show the address: '" + archived?.Address + "'");
        MJdatabase.RestoreClientStore(after.Id);
        if (StoreByCode("CLIENT A", "R-2").Address != "9 Quezon Blvd.") bad.Add("restore lost the address");
        return string.Join("; ", bad);
    }

    [Check]
    public static string StoreAddress_NewStoreDialog_RequiresIt_PrintColumnOffByDefault()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        MJdatabase.RegisterCompanyStore("CLIENT A", "P-1", "Print Store", "3 Lacson St.");
        int d = Seed.Delivery(client: "CLIENT A");
        var del = MJdatabase.GetDeliveryById(d)!;
        var stops = new List<DispatchStop> { new() { StoreCode = "P-1", Destination = "Print Store", GCrate = 5, ECrate = 5 } };
        var def = PrintForms.TripReport(del, null, stops);
        var bad = new List<string>();
        var choice = def.Defaults.Columns.FirstOrDefault(c => c.Key == "store_address");
        if (choice == null) bad.Add("no Store address column choice on the trip report print");
        else if (choice.On) bad.Add("the Store address column is on by default");
        if (Html(def).Contains("3 Lacson St.")) bad.Add("the address printed although the column is off");
        // Saved print settings from before this change do not list the column: it must stay off for them too.
        var old = def.Defaults.Clone(); old.Columns.RemoveAll(c => c.Key == "store_address");
        if (def.Body(old).Contains("3 Lacson St.")) bad.Add("an older saved print setting switched the address on");
        var on = def.Defaults.Clone(); on.Columns.First(c => c.Key == "store_address").On = true;
        if (!def.Body(on).Contains("3 Lacson St.")) bad.Add("the address did not print when ticked");
        // The Register-a-store window refuses a new store with no address and saves it with one.
        string dialogResult = RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            using var dlg = new RegisterStoreDialog("CLIENT A", "N-1", "Brand New Store", null);
            dlg.Show();
            Pump(300);
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var T = typeof(RegisterStoreDialog);
            var save = T.GetMethod("Save", F)!;
            var lbl = (Label)T.GetField("lblProblem", F)!.GetValue(dlg)!;
            var addr = (TextBox)T.GetField("txtAddress", F)!.GetValue(dlg)!;
            var msgs = new List<string>();
            save.Invoke(dlg, null);
            if (dlg.RegisteredStore != null) msgs.Add("a new store was saved without an address");
            if (!lbl.Text.Contains("address", StringComparison.OrdinalIgnoreCase)) msgs.Add("no address message: '" + lbl.Text + "'");
            addr.Text = "88 Aguinaldo Hwy.";
            save.Invoke(dlg, null);
            if (dlg.RegisteredStore == null) msgs.Add("the store was not saved once the address was typed (" + lbl.Text + ")");
            return string.Join("; ", msgs);
        });
        if (dialogResult.Length > 0) bad.Add(dialogResult);
        else if (StoreByCode("CLIENT A", "N-1").Address != "88 Aguinaldo Hwy.") bad.Add("the dialog did not save the address");
        return string.Join("; ", bad);
    }
}
