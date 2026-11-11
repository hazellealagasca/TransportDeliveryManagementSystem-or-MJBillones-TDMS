using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Fix 1: stores carry an address (additive column). The address is optional everywhere (a store with none saves through every writer and entry point), and the address survives rename, archive and restore.</summary>
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
    public static string StoreAddress_OldStoresWithNoAddress_StillWork()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        // An older store: inserted the way it was before the column existed (Address left to its default).
        Exec("INSERT INTO ClientStores (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute) VALUES (0, 'CLIENT A', 'OLD-1', 'Old Store', '')");
        Exec("INSERT INTO ClientStores (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute, Address) VALUES (0, 'CLIENT A', 'NEW-1', 'New Store', '', '1 Plaza St.')");
        Exec("INSERT INTO ClientStores (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute, IsArchived) VALUES (0, 'CLIENT A', 'OLD-2', 'Archived Old', '', 1)");
        var bad = new List<string>();
        var old = StoreByCode("CLIENT A", "OLD-1");
        if (old.Address != "") bad.Add("old store address is '" + old.Address + "'");
        if (StoreAddressBook.Get("CLIENT A", "OLD-1", "") != "") bad.Add("old store shows an address");
        if (MJdatabase.FindStoresAnyClient("OLD-1").Count != 1) bad.Add("old store not found by the registry");
        // A store can be edited and saved without an address.
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
    public static string StoreAddress_PrintColumnOffByDefault()
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
        return string.Join("; ", bad);
    }

    [Check]
    public static string StoreAddress_BlankSavesFromEveryEntryPoint_NoDashboardRow()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        var bad = new List<string>();
        // Writers: a new store with no address saves.
        MJdatabase.SaveClientStore(new ClientStore { ClientCompanyName = "CLIENT A", StoreCode = "B-1", StoreName = "Blank One", Address = "" });
        if (MJdatabase.GetClientStores("CLIENT A", includeArchived: true).All(x => x.StoreCode != "B-1")) bad.Add("SaveClientStore with a blank address did not save");
        MJdatabase.RegisterCompanyStore("CLIENT A", "B-2", "Blank Two", "");
        MJdatabase.RegisterClientStore("CLIENT A", "B-3", "Blank Three", "", "");
        MJdatabase.RegisterCompanyStore("CLIENT A", "B-4", "Filled Four", "5 Roxas Ave.");
        foreach (var c in new[] { "B-2", "B-3" }) if (StoreByCode("CLIENT A", c).Address != "") bad.Add(c + " has an address");
        if (StoreByCode("CLIENT A", "B-4").Address != "5 Roxas Ave.") bad.Add("a filled address did not read back");
        // The dashboard no longer counts or lists stores without an address.
        if (typeof(MJdatabase).GetMethod("CountActiveStoresWithoutAddress") != null) bad.Add("the no-address count still exists");
        var dash = typeof(MJdatabase).Assembly.GetTypes().FirstOrDefault(t => t.Name == "DashboardView");
        if (dash != null && dash.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public).Any(t => t.GetField("StoresNoAddress") != null)) bad.Add("the dashboard data still has StoresNoAddress");
        string ui = RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var msgs = new List<string>();
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            // Register a store window
            using (var dlg = new RegisterStoreDialog("CLIENT A", "N-1", "Brand New Store", null))
            {
                dlg.Show(); Pump(300);
                var T = typeof(RegisterStoreDialog);
                T.GetMethod("Save", F)!.Invoke(dlg, null);
                var lbl = (Label)T.GetField("lblProblem", F)!.GetValue(dlg)!;
                if (dlg.RegisteredStore == null) msgs.Add("Register a store window did not save a blank address (" + lbl.Text + ")");
            }
            // Clients > Stores and routes quick-add bar: a saved client, then a new unsaved client
            using (var host = new Form())
            {
                var grid = new DataGridView();
                var panel = new ClientStoresRoutesPanel(() => "CLIENT A", grid, () => { });
                host.Controls.Add(panel); host.Show(); Pump(300);
                var T = typeof(ClientStoresRoutesPanel);
                ((TextBox)T.GetField("txtCode", F)!.GetValue(panel)!).Text = "Q-1";
                ((TextBox)T.GetField("txtName", F)!.GetValue(panel)!).Text = "Quick Store";
                T.GetMethod("AddStore", F)!.Invoke(panel, null);
                bool pending = true;
                panel.SetPendingCheck(() => pending);
                ((TextBox)T.GetField("txtCode", F)!.GetValue(panel)!).Text = "Q-2";
                ((TextBox)T.GetField("txtName", F)!.GetValue(panel)!).Text = "Pending Store";
                T.GetMethod("AddStore", F)!.Invoke(panel, null);
                var pend = (System.Collections.IEnumerable)T.GetField("pendingStores", F)!.GetValue(panel)!;
                if (!pend.Cast<object>().Any()) msgs.Add("an unsaved client's store with no address was not added");
                pending = false;
                var err = panel.SavePending("CLIENT A");
                if (err != null) msgs.Add("saving the pending store failed: " + err);
            }
            // Master data > Clients Stores table
            using (var view = new ClientManagementView())
            {
                
                var vt = typeof(ClientManagementView);
                var client = MJdatabase.GetClients().FirstOrDefault(c => c.CompanyName == "CLIENT A") ?? new ClientCompany { CompanyName = "CLIENT A" };
                vt.GetField("selectedClient", F)!.SetValue(view, client);
                var list = (List<ClientStore>)vt.GetField("currentStores", F)!.GetValue(view)!;
                list.Add(new ClientStore { ClientCompanyName = "CLIENT A", StoreCode = "T-1", StoreName = "Table Store", Address = "" });
                using var yes = new YesPresser(GetCurrentThreadId()); // the "Are you sure you want to save" box
                vt.GetMethod("SaveStoreRow", F)!.Invoke(view, new object[] { list.Count - 1 });
            }
            return string.Join("; ", msgs);
        });
        if (ui.Length > 0) bad.Add(ui);
        foreach (var c in new[] { "N-1", "Q-1", "Q-2", "T-1" })
            if (MJdatabase.GetClientStores("CLIENT A", includeArchived: true).All(x => x.StoreCode != c)) bad.Add("store " + c + " (blank address) was not saved");
        return string.Join("; ", bad);
    }

    /// <summary>Clicks "Yes" on the app's own pop-up (the Clients table asks "Are you sure you want to save" before saving a store row).</summary>
    private sealed class YesPresser : IDisposable
    {
        private readonly System.Threading.Timer timer;
        public YesPresser(uint uiThread)
        {
            timer = new System.Threading.Timer(_ => EnumThreadWindows(uiThread, (h, __) =>
            {
                if (!IsWindowVisible(h)) return true;
                EnumChildWindows(h, (c, ___) =>
                {
                    var t = new System.Text.StringBuilder(64); GetWindowText(c, t, 64);
                    if (t.ToString().Replace("&", "") == "Yes") PostMessage(c, 0x00F5 /* BM_CLICK */, IntPtr.Zero, IntPtr.Zero);
                    return true;
                }, IntPtr.Zero);
                return true;
            }, IntPtr.Zero), null, 300, 300);
        }
        public void Dispose() => timer.Dispose();
    }
}
