using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// Nov 14, Phase 2 (master data). 2A: Corporate and Individual clients (a corporate client needs every detail, the address too; an Individual
/// client gets one automatic IND-#### store, called the recipient, so a dispatch has one drop and one DR; the SOA TIN check is for corporate
/// clients only). 2B: a blank store code becomes a generated PREFIX-NNN code on every store writer; it shows as "(no code)" and prints blank;
/// codes stay unique.
/// </summary>
public static partial class Checks
{
    private const BindingFlags CtF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static (TextBox Box, Func<string, bool> Save) CtClientForm(ClientManagementView view, bool isNew = true)
    {
        var vt = typeof(ClientManagementView);
        vt.GetField("isNewClient", CtF)!.SetValue(view, isNew);
        if (isNew) vt.GetField("selectedClient", CtF)!.SetValue(view, null);
        return ((TextBox)vt.GetField("txtClientName", CtF)!.GetValue(view)!, _ => (bool)vt.GetMethod("SaveClient", CtF, null, new[] { typeof(bool) }, null)!.Invoke(view, new object[] { false })!);
    }

    private static void CtSet(ClientManagementView view, string field, string value) => ((TextBox)typeof(ClientManagementView).GetField(field, CtF)!.GetValue(view)!).Text = value;

    private static void CtType(ClientManagementView view, string type) => ((ComboBox)typeof(ClientManagementView).GetField("cbClientType", CtF)!.GetValue(view)!).SelectedItem = type;

    // ------------------------------------------------------------------ 2A: corporate

    [Check]
    public static string ClientTypes_Corporate_RequiresEveryDetail_AddressToo()
    {
        Fresh();
        return RunOnSta(() =>
        {
            var bad = new List<string>();
            using var boxes = new VdBoxes();
            using var view = new ClientManagementView();
            var (_, save) = CtClientForm(view);
            void Fill(string name, string tin, string addr, string person, string phone, string email)
            {
                CtType(view, ClientTypes.Corporate);
                CtSet(view, "txtClientName", name); CtSet(view, "txtClientTin", tin); CtSet(view, "txtClientAddress", addr);
                CtSet(view, "txtContactPerson", person); CtSet(view, "txtContactNumber", phone); CtSet(view, "txtEmail", email);
            }
            void Refused(string what, string expectedInBox)
            {
                int before = boxes.Boxes.Count;
                if (save("")) bad.Add(what + ": saved");
                else if (boxes.Boxes.Count == before || !boxes.Boxes.Skip(before).Any(b => b.Contains(expectedInBox))) bad.Add(what + ": refused without naming " + expectedInBox + " [" + string.Join(" / ", boxes.Boxes.Skip(before)) + "]");
            }
            Fill("CT CORP ONE", "006-896-371-000", "", "Juan Dela Cruz", "09171234567", "billing@ctcorp.co");
            Refused("no address", "Address");
            Fill("CT CORP ONE", "006-896-371-000", "   ", "Juan Dela Cruz", "09171234567", "billing@ctcorp.co");
            Refused("spaces-only address", "Address");
            Fill("CT CORP ONE", "006-896-371-000", "1 Rizal St", "Juan Dela Cruz", "09171234567", "");
            Refused("no email", "Email");
            Fill("CT CORP ONE", "006-896-371-000", "1 Rizal St", "", "09171234567", "billing@ctcorp.co");
            Refused("no contact person", "Contact person");
            Fill("CT CORP ONE", "", "1 Rizal St", "Juan Dela Cruz", "09171234567", "billing@ctcorp.co");
            Refused("no TIN", "TIN");
            Fill("CT CORP ONE", "006-896-371-000", "1 Rizal St", "Juan Dela Cruz", "", "billing@ctcorp.co");
            Refused("no contact number", "Contact number");
            if (MJdatabase.GetClients().Any(c => c.CompanyName == "CT CORP ONE")) bad.Add("a refused corporate client was written");
            Fill("CT CORP ONE", "006-896-371-000", "1 Rizal St", "Juan Dela Cruz", "09171234567", "billing@ctcorp.co");
            if (!save("")) bad.Add("a complete corporate client was refused: " + string.Join(" / ", boxes.Boxes));
            return string.Join("; ", bad);
        }) is { Length: > 0 } ui ? ui : CtCorporateSavedCheck();
    }

    private static string CtCorporateSavedCheck()
    {
        var c = MJdatabase.GetClients().FirstOrDefault(x => x.CompanyName == "CT CORP ONE");
        if (c == null) return "complete corporate client not saved";
        var bad = new List<string>();
        if (c.ClientType != ClientTypes.Corporate || c.IsIndividual) bad.Add("type is " + c.ClientType);
        if (c.Address != "1 Rizal St") bad.Add("address '" + c.Address + "'");
        if (MJdatabase.GetClientStores("CT CORP ONE", includeArchived: true).Count != 0) bad.Add("a corporate client got an automatic store");
        return string.Join("; ", bad);
    }

    // ------------------------------------------------------------------ 2A: individual

    [Check]
    public static string ClientTypes_Individual_SavesWithOneIndStore_RecipientLabels()
    {
        Fresh();
        string ui = RunOnSta(() =>
        {
            var bad = new List<string>();
            using var boxes = new VdBoxes();
            using var view = new ClientManagementView();
            var (_, save) = CtClientForm(view);
            CtType(view, ClientTypes.Individual);
            var vt = typeof(ClientManagementView);
            var fldContact = (Panel)vt.GetField("fldContact", CtF)!.GetValue(view)!;
            var storesCard = (Panel)vt.GetField("pnlStoresCard", CtF)!.GetValue(view)!;
            if (fldContact.Visible) bad.Add("the contact person box is still shown for an Individual client");
            if (storesCard.Visible) bad.Add("the stores and routes section is still shown for an Individual client");
            var labels = AllControls(view).OfType<Label>().Select(l => l.Text).ToList();
            if (!labels.Contains("Full name") || !labels.Contains("Drop-off address") || !labels.Contains("TIN (optional)") || !labels.Contains("Email (optional)")) bad.Add("labels: " + string.Join(" | ", labels.Where(l => l.Length > 0 && l.Length < 30).Take(14)));
            void Fill(string name, string phone, string addr, string tin = "", string email = "")
            {
                CtSet(view, "txtClientName", name); CtSet(view, "txtContactNumber", phone); CtSet(view, "txtClientAddress", addr);
                CtSet(view, "txtClientTin", tin); CtSet(view, "txtEmail", email); CtSet(view, "txtContactPerson", "");
            }
            Fill("Dela Cruz, Juan", "09171234567", "");
            if (save("")) bad.Add("an Individual client without a drop-off address was saved");
            else if (!boxes.Boxes.Any(b => b.Contains("Drop-off address"))) bad.Add("the refusal did not name the drop-off address: " + string.Join(" / ", boxes.Boxes));
            Fill("Juan 2", "09171234567", "12 Mabini St");
            if (save("")) bad.Add("an Individual name with a digit was saved");
            Fill("Dela Cruz, Juan", "", "12 Mabini St");
            if (save("")) bad.Add("an Individual client without a contact number was saved");
            Fill("Dela Cruz, Juan", "09171234567", "12 Mabini St", tin: "12345");
            if (save("")) bad.Add("an Individual client with a malformed TIN was saved");
            if (MJdatabase.GetClients().Any(c => c.CompanyName == "Dela Cruz, Juan")) bad.Add("a refused Individual client was written");
            Fill("Dela Cruz, Juan", "09171234567", "12 Mabini St");   // no TIN, no email, no contact person: all optional or not asked
            if (!save("")) bad.Add("a complete Individual client was refused: " + string.Join(" / ", boxes.Boxes));
            return string.Join("; ", bad);
        });
        var bad2 = new List<string>();
        if (ui.Length > 0) bad2.Add(ui);
        var cl = MJdatabase.GetClients().FirstOrDefault(c => c.CompanyName == "Dela Cruz, Juan");
        if (cl == null) return string.Join("; ", bad2.Append("the Individual client was not saved"));
        if (!cl.IsIndividual || cl.ClientType != ClientTypes.Individual) bad2.Add("type is " + cl.ClientType);
        if (cl.Address != "12 Mabini St") bad2.Add("drop-off address saved in Address is '" + cl.Address + "'");
        if (cl.TIN != "") bad2.Add("TIN '" + cl.TIN + "'");
        var stores = MJdatabase.GetClientStores(cl.CompanyName, includeArchived: true);
        string want = "IND-" + cl.Id.ToString("D4");
        if (stores.Count != 1) bad2.Add(stores.Count + " stores, expected exactly 1");
        else
        {
            var s = stores[0];
            if (s.StoreCode != want) bad2.Add($"store code '{s.StoreCode}', expected {want}");
            if (s.StoreName != cl.CompanyName) bad2.Add("store name '" + s.StoreName + "'");
            if (s.Address != "12 Mabini St") bad2.Add("store address '" + s.Address + "'");
            if (!s.IsGeneratedCode) bad2.Add("the IND store is not marked as a generated code");
            if (MJdatabase.StoreCodeForScreen(s) != MJdatabase.NoCodeText) bad2.Add("the IND store shows '" + MJdatabase.StoreCodeForScreen(s) + "' on screens");
            if (MJdatabase.StoreCodeForPrint(s.StoreCode) != "") bad2.Add("the IND store code prints");
        }
        // Saving again changes the one store's address and never adds a second.
        cl.Address = "99 Burgos Ave";
        MJdatabase.SaveClient(cl);
        stores = MJdatabase.GetClientStores(cl.CompanyName, includeArchived: true);
        if (stores.Count != 1 || stores[0].Address != "99 Burgos Ave" || stores[0].StoreCode != want) bad2.Add("a second save left " + stores.Count + " store(s): " + string.Join(",", stores.Select(s => s.StoreCode + "/" + s.Address)));
        if (!MJdatabase.IsIndividualClient("Dela Cruz, Juan") || MJdatabase.IsIndividualClient("CT NOBODY")) bad2.Add("IsIndividualClient is wrong");
        return string.Join("; ", bad2);
    }

    [Check]
    public static string ClientTypes_Individual_DispatchHasOneStoreAndOneDrop_TripReportDr()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        MJdatabase.SaveClient(new ClientCompany { CompanyName = "Reyes, Maria", ContactNumber = "09171234567", Address = "5 Quezon Blvd", ClientType = ClientTypes.Individual });
        var cl = MJdatabase.GetClientByCompanyName("Reyes, Maria")!;
        var store = MJdatabase.GetClientStores("Reyes, Maria").Single();
        var bad = new List<string>();

        // An order with no stops of its own gets its stops built: the one store, one drop.
        int did = Seed.Delivery(client: "Reyes, Maria");
        var del = MJdatabase.GetDeliveries().First(d => d.Id == did);
        var built = MJdatabase.GetOrBuildTripStopsForDelivery(del);
        if (built.Count != 1 || built[0].StoreCode != store.StoreCode || built[0].Destination != store.StoreName) bad.Add("built stops: " + string.Join(",", built.Select(s => s.StoreCode + "/" + s.Destination)));
        if (built.Count == 1 && MJdatabase.DropsOf(built[0]) != 1) bad.Add("drops " + MJdatabase.DropsOf(built[0]));

        // The trip report's DR for that store is kept on the store's stop and reaches the statement line: one drop, the DR, no store code.
        built[0].DrNumbers = "123456";
        MJdatabase.SaveDispatchStops(did, built);
        var saved = MJdatabase.GetOrBuildTripStopsForDelivery(del);
        if (saved.Count != 1 || saved[0].DrNumbers != "123456" || saved[0].StoreCode != store.StoreCode) bad.Add("saved stop: " + string.Join(",", saved.Select(s => s.StoreCode + "/" + s.DrNumbers)));
        try
        {
            var m = typeof(StatementOfAccountView).GetMethod("BuildTripLines", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (m == null) bad.Add("BuildTripLines not found");
            else
            {
                var args = new object?[] { del, 0 };
                var lines = (System.Collections.IList)m.Invoke(null, args)!;
                if ((int)args[1]! != 1) bad.Add("the statement counts " + args[1] + " drops, expected 1");
                if (lines.Count != 1) bad.Add(lines.Count + " statement lines, expected 1");
            }
        }
        catch (Exception ex) { bad.Add("statement lines threw " + (ex.InnerException ?? ex).Message); }

        // The dispatch window: choosing the client puts its one store in the table, and the table says Recipient.
        string ui = RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var view = (DispatchManagementView)Activator.CreateInstance(typeof(DispatchManagementView), nonPublic: true)!;
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900), FormBorderStyle = FormBorderStyle.None };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(800);
            var vt = typeof(DispatchManagementView);
            var grid = (DataGridView?)vt.GetField("gridRouteStores", CtF)!.GetValue(view);
            if (grid == null) return "store table not created";
            vt.GetMethod("ApplyIndividualClient", CtF)!.Invoke(view, new object[] { "Reyes, Maria", true });
            var rows = grid.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
            var msgs = new List<string>();
            if (rows.Count != 1) msgs.Add(rows.Count + " store rows, expected 1");
            else if ((rows[0].Cells["colStoreCode"].Value?.ToString() ?? "") != store.StoreCode || (rows[0].Cells["colStoreName"].Value?.ToString() ?? "") != store.StoreName) msgs.Add("the row holds " + rows[0].Cells["colStoreCode"].Value + " / " + rows[0].Cells["colStoreName"].Value);
            if (grid.Columns["colStoreName"]!.HeaderText != "Recipient") msgs.Add("table header '" + grid.Columns["colStoreName"]!.HeaderText + "', expected Recipient");
            vt.GetMethod("ApplyIndividualClient", CtF)!.Invoke(view, new object[] { "Reyes, Maria", true });
            if (grid.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow) != 1) msgs.Add("choosing the client again added another row");
            vt.GetMethod("ApplyIndividualClient", CtF)!.Invoke(view, new object[] { "CT NOBODY", true });
            if (grid.Columns["colStoreName"]!.HeaderText != "Store Name") msgs.Add("a corporate client's table header is '" + grid.Columns["colStoreName"]!.HeaderText + "'");
            view.Dispose();
            return string.Join("; ", msgs);
        });
        if (ui.Length > 0) bad.Add(ui);

        // Individual: the SOA TIN line prints only when there is a TIN; a corporate client with no TIN still prints the line.
        var (soa, items) = PrintSample(SoaTypes.Normal);
        soa.ClientName = "Reyes, Maria"; soa.ClientTin = "";
        string noTin = Html(PrintForms.StatementOfAccount(soa, items));
        if (noTin.Contains(">TIN<")) bad.Add("an Individual statement without a TIN prints a TIN line");
        if (!noTin.Contains("Reyes, Maria")) bad.Add("the Individual statement does not show the name");
        soa.ClientTin = "006-896-371-000";
        if (!Html(PrintForms.StatementOfAccount(soa, items)).Contains(">TIN<")) bad.Add("an Individual statement with a TIN does not print it");
        soa.ClientName = "CLIENT A"; soa.ClientTin = "";
        if (!Html(PrintForms.StatementOfAccount(soa, items)).Contains(">TIN<")) bad.Add("a corporate statement lost its TIN line");
        return string.Join("; ", bad);
    }

    [Check]
    public static string ClientTypes_SoaTinCheck_AppliesToCorporateOnly()
    {
        Fresh();
        MJdatabase.SaveClient(new ClientCompany { CompanyName = "CT TIN CORP", TIN = "", ClientType = ClientTypes.Corporate });
        MJdatabase.SaveClient(new ClientCompany { CompanyName = "Santos, Ana", Address = "3 Roxas Ave", ClientType = ClientTypes.Individual });
        var bad = new List<string>();
        if (MJdatabase.IsIndividualClient("CT TIN CORP")) bad.Add("a corporate client counts as Individual");
        if (!MJdatabase.IsIndividualClient("Santos, Ana")) bad.Add("an Individual client counts as corporate");
        string src = File.ReadAllText(Path.Combine(SourceRoot(), "Views", "StatementOfAccountView.cs"));
        int at = src.IndexOf("The client's TIN is required on a statement", StringComparison.Ordinal);
        string before = at < 0 ? "" : src.Substring(Math.Max(0, at - 700), Math.Min(700, at));
        if (at < 0) bad.Add("the statement TIN message was not found in the source");
        else if (!before.Contains("IsIndividualClient")) bad.Add("the statement TIN check does not skip Individual clients");
        return string.Join("; ", bad);
    }

    // ------------------------------------------------------------------ 2B: blank store codes

    [Check]
    public static string StoreCodes_Blank_GeneratesPrefixNumber_OnEveryWriter()
    {
        Fresh();
        var bad = new List<string>();
        const string client = "ACME FOODS";
        MJdatabase.SaveClient(new ClientCompany { CompanyName = client, TIN = "006896371000", Address = "1 Main", ContactPerson = "A B", ContactNumber = "09171234567", Email = "a@acme.co" });
        ClientStore Find(string name) => MJdatabase.GetClientStores(client, includeArchived: true).First(s => s.StoreName == name);

        var a = MJdatabase.RegisterCompanyStore(client, "", "Store A");
        if (a.StoreCode != "ACM-001" || !a.IsGeneratedCode) bad.Add($"RegisterCompanyStore blank: {a.StoreCode} generated={a.IsGeneratedCode}");
        var b = MJdatabase.RegisterCompanyStore(client, "   ", "Store B", "2 Side St");
        if (b.StoreCode != "ACM-002") bad.Add("second blank code: " + b.StoreCode);
        var typed = MJdatabase.RegisterCompanyStore(client, "ac 77", "Store Typed");
        if (typed.StoreCode != "AC77" || typed.IsGeneratedCode) bad.Add($"a typed code: {typed.StoreCode} generated={typed.IsGeneratedCode}");

        // The Register a store window's writer, on a route and without one.
        long routeId = Exec("INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, ConnectedPlaces, IsActive) VALUES (@cl, 'AC-R1', 'North', '', 1)", ("@cl", client));
        var onRoute = MJdatabase.RegisterClientStore(client, "", "Store On Route", "AC-R1 - North");
        if (onRoute.StoreCode != "ACM-003" || !onRoute.IsGeneratedCode) bad.Add($"RegisterClientStore blank on a route: {onRoute.StoreCode} generated={onRoute.IsGeneratedCode}");
        var route = MJdatabase.GetDeliveryRoutes(activeOnly: false).First(r => r.Id == routeId);
        if (!route.PlacesList.Any(p => p.StartsWith("ACM-003"))) bad.Add("the route holds [" + string.Join("|", route.PlacesList) + "], expected ACM-003 - Store On Route");
        var noRoute = MJdatabase.RegisterClientStore(client, "", "Store No Route", "");
        if (noRoute.StoreCode != "ACM-004" || !noRoute.IsGeneratedCode) bad.Add($"RegisterClientStore blank, no route: {noRoute.StoreCode} generated={noRoute.IsGeneratedCode}");

        // The Clients store table's writer.
        var row = new ClientStore { ClientCompanyName = client, StoreCode = "", StoreName = "Store Row", Address = "" };
        MJdatabase.SaveClientStore(row);
        var rowSaved = Find("Store Row");
        if (rowSaved.StoreCode != "ACM-005" || !rowSaved.IsGeneratedCode) bad.Add($"SaveClientStore blank: {rowSaved.StoreCode} generated={rowSaved.IsGeneratedCode}");
        // Renaming a generated store keeps its code; typing a code of its own makes it the user's; blanking it again makes a new one.
        rowSaved.StoreName = "Store Row Renamed"; rowSaved.StoreCode = "";
        MJdatabase.SaveClientStore(rowSaved);
        var renamed = Find("Store Row Renamed");
        if (renamed.StoreCode != "ACM-005" || !renamed.IsGeneratedCode) bad.Add($"rename kept {renamed.StoreCode} generated={renamed.IsGeneratedCode}");
        renamed.StoreCode = "MINE-1";
        MJdatabase.SaveClientStore(renamed);
        var own = Find("Store Row Renamed");
        if (own.StoreCode != "MINE-1" || own.IsGeneratedCode) bad.Add($"a typed code over a generated one: {own.StoreCode} generated={own.IsGeneratedCode}");
        own.StoreCode = "(no code)";
        MJdatabase.SaveClientStore(own);
        var again = Find("Store Row Renamed");
        if (!again.IsGeneratedCode || !System.Text.RegularExpressions.Regex.IsMatch(again.StoreCode, @"^ACM-\d{3}$")) bad.Add($"'(no code)' typed back: {again.StoreCode} generated={again.IsGeneratedCode}");

        // Screens and printouts.
        if (MJdatabase.StoreCodeForScreen(a) != "(no code)") bad.Add("screen shows '" + MJdatabase.StoreCodeForScreen(a) + "'");
        if (MJdatabase.StoreCodeForScreen(typed) != "AC77") bad.Add("a typed code shows as '" + MJdatabase.StoreCodeForScreen(typed) + "'");
        if (MJdatabase.StoreCodeForPrint("ACM-001") != "" || MJdatabase.StoreCodeForPrint("AC77") != "AC77") bad.Add("print: '" + MJdatabase.StoreCodeForPrint("ACM-001") + "' / '" + MJdatabase.StoreCodeForPrint("AC77") + "'");
        var found = StoreDirectory.Search(MJdatabase.GetClientStores(client), "ACM-001");
        if (found.Count != 1 || found[0].StoreName != "Store A") bad.Add("search by the generated code found " + found.Count);
        var del = new Delivery { Id = Seed.Delivery(client: client), ClientName = client, DispatchNumber = "D-CT", Destination = "Store A" };
        var stops = new List<DispatchStop>
        {
            new() { DeliveryId = del.Id, StopSequence = 1, ClientName = client, StoreCode = "ACM-001", Destination = "Store A", GCrate = 1, TotalCrates = 1, DrNumbers = "111111" },
            new() { DeliveryId = del.Id, StopSequence = 2, ClientName = client, StoreCode = "AC77", Destination = "Store Typed", GCrate = 1, TotalCrates = 1, DrNumbers = "222222" }
        };
        string h = Html(PrintForms.TripReport(del, null, stops));
        if (h.Contains("ACM-001")) bad.Add("the trip report prints the generated code");
        if (!h.Contains("AC77") || !h.Contains("Store A")) bad.Add("the trip report lost the typed code or the store name");
        return string.Join("; ", bad);
    }

    [Check]
    public static string StoreCodes_Generated_StayUnique()
    {
        Fresh();
        var bad = new List<string>();
        MJdatabase.SaveClient(new ClientCompany { CompanyName = "ACME FOODS", Address = "x" });
        MJdatabase.SaveClient(new ClientCompany { CompanyName = "ACMEX TRADING", Address = "x" });   // the same prefix, ACM
        MJdatabase.SaveClient(new ClientCompany { CompanyName = "A&B", Address = "x" });             // fewer than 3 letters
        MJdatabase.SaveClient(new ClientCompany { CompanyName = "123", Address = "x" });             // no letters
        if (MJdatabase.RegisterCompanyStore("ACME FOODS", "", "One").StoreCode != "ACM-001") bad.Add("first ACM code");
        var other = MJdatabase.RegisterCompanyStore("ACMEX TRADING", "", "One");
        if (other.StoreCode != "ACM-002") bad.Add("another client with the same prefix got " + other.StoreCode + ", expected the next free ACM-002");
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < 12; i++) codes.Add(MJdatabase.RegisterCompanyStore("ACME FOODS", "", "Many " + i).StoreCode);
        if (codes.Count != 12) bad.Add("twelve blank codes gave " + codes.Count + " different codes");
        if (Long("SELECT COUNT(*) FROM (SELECT UPPER(StoreCode) c FROM ClientStores WHERE TRIM(StoreCode) <> '' GROUP BY c HAVING COUNT(*) > 1)") != 0) bad.Add("a store code is used twice");   // (the sample stores of a new database have no code at all)
        if (MJdatabase.RegisterCompanyStore("A&B", "", "One").StoreCode != "AB-001") bad.Add("two-letter prefix: " + MJdatabase.GenerateStoreCode("A&B"));
        if (MJdatabase.RegisterCompanyStore("123", "", "One").StoreCode != "STO-001") bad.Add("no-letter prefix");
        // A duplicate stays impossible: a typed code that a generated one already holds, the same name, and a typed duplicate.
        void Refuses(string what, Action act) { try { act(); bad.Add(what + ": accepted"); } catch (InvalidOperationException) { } }
        Refuses("typed code already generated for another store", () => MJdatabase.RegisterCompanyStore("ACMEX TRADING", "ACM-001", "Other"));
        Refuses("same name again", () => MJdatabase.RegisterCompanyStore("ACME FOODS", "", "One"));
        Refuses("typed duplicate", () => { MJdatabase.RegisterCompanyStore("ACME FOODS", "T-1", "T One"); MJdatabase.RegisterCompanyStore("ACME FOODS", "t-1", "T Two"); });
        Refuses("SaveClientStore with a code another store has", () => MJdatabase.SaveClientStore(new ClientStore { ClientCompanyName = "ACME FOODS", StoreCode = "ACM-001", StoreName = "Dup" }));
        return string.Join("; ", bad);
    }

    [Check]
    public static string StoreCodes_QuickAdd_BlankCode_AndNewClientsUnsavedStores()
    {
        Fresh();
        MJdatabase.SaveClient(new ClientCompany { CompanyName = "ACME FOODS", Address = "x" });
        return RunOnSta(() =>
        {
            var bad = new List<string>();
            var pt = typeof(ClientStoresRoutesPanel);
            string Msg(ClientStoresRoutesPanel p) => ((Label)pt.GetField("lblMsg", CtF)!.GetValue(p)!).Text;
            void Type(ClientStoresRoutesPanel p, string code, string name) { ((TextBox)pt.GetField("txtCode", CtF)!.GetValue(p)!).Text = code; ((TextBox)pt.GetField("txtName", CtF)!.GetValue(p)!).Text = name; }

            // The quick-add of a saved client.
            int reloads = 0;
            using (var panel = new ClientStoresRoutesPanel(() => "ACME FOODS", new DataGridView(), () => reloads++))
            {
                Type(panel, "", "Quick Store");
                pt.GetMethod("AddStore", CtF)!.Invoke(panel, null);
                var st = MJdatabase.GetClientStores("ACME FOODS").FirstOrDefault(s => s.StoreName == "Quick Store");
                if (st == null) bad.Add("the quick-add refused a blank code: " + Msg(panel));
                else if (st.StoreCode != "ACM-001" || !st.IsGeneratedCode) bad.Add($"quick-add saved {st.StoreCode} generated={st.IsGeneratedCode}");
                if (st != null && !Msg(panel).Contains("(no code)")) bad.Add("the quick-add message does not say (no code): " + Msg(panel));
                if (reloads == 0) bad.Add("the store table was not reloaded");
            }

            // A new client's unsaved stores: a blank code is made at once, listed as (no code), and kept when the client is saved.
            using (var panel = new ClientStoresRoutesPanel(() => "", new DataGridView(), () => { }))
            {
                panel.SetPendingCheck(() => true);
                panel.SetPendingNameSource(() => "Pending Foods Inc");
                Type(panel, "", "Pending One");
                pt.GetMethod("AddPendingStore", CtF)!.Invoke(panel, null);
                Type(panel, "", "Pending Two");
                pt.GetMethod("AddPendingStore", CtF)!.Invoke(panel, null);
                Type(panel, "pf-9", "Pending Typed");
                pt.GetMethod("AddPendingStore", CtF)!.Invoke(panel, null);
                var list = (List<(string Code, string Name)>)pt.GetField("pendingStores", CtF)!.GetValue(panel)!;
                if (list.Count != 3) { bad.Add(list.Count + " unsaved stores, expected 3: " + Msg(panel)); return string.Join("; ", bad); }
                if (list[0].Code != "PEN-001" || list[1].Code != "PEN-002" || list[2].Code != "PF-9") bad.Add("unsaved codes: " + string.Join(",", list.Select(x => x.Code)));
                var grid = (DataGridView)pt.GetField("pendingGrid", CtF)!.GetValue(panel)!;
                if ((grid.Rows[0].Cells["colCode"].Value?.ToString() ?? "") != "(no code)" || (grid.Rows[2].Cells["colCode"].Value?.ToString() ?? "") != "PF-9") bad.Add("the unsaved list shows " + grid.Rows[0].Cells["colCode"].Value + " / " + grid.Rows[2].Cells["colCode"].Value);
                MJdatabase.SaveClient(new ClientCompany { CompanyName = "PENDING FOODS INC", Address = "y" });
                string? err = panel.SavePending("PENDING FOODS INC");
                if (err != null) bad.Add("saving the unsaved stores: " + err);
                var saved = MJdatabase.GetClientStores("PENDING FOODS INC");
                if (saved.Count != 3) bad.Add(saved.Count + " stores saved with the client");
                if (saved.Count(s => s.IsGeneratedCode) != 2 || saved.Any(s => s.StoreCode == "PF-9" && s.IsGeneratedCode)) bad.Add("generated flags: " + string.Join(",", saved.Select(s => s.StoreCode + "=" + s.IsGeneratedCode)));
            }

            // The Clients screen's store table shows "(no code)" for a generated code, and the real code for one typed.
            MJdatabase.RegisterCompanyStore("ACME FOODS", "ACM-TYPED", "Typed Store");
            using (var view = new ClientManagementView())
            using (var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900), FormBorderStyle = FormBorderStyle.None })
            {
                view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
                view.OpenClientByName("ACME FOODS");
                var grid = (DataGridView)typeof(ClientManagementView).GetField("gridBranches", CtF)!.GetValue(view)!;
                grid.CurrentCell = null;
                string Shown(string store)
                {
                    var r = grid.Rows.Cast<DataGridViewRow>().First(x => x.Cells["StoreName"].Value?.ToString() == store);
                    return r.Cells["StoreCode"].FormattedValue?.ToString() ?? "";
                }
                if (Shown("Quick Store") != "(no code)") bad.Add("the Clients store table shows '" + Shown("Quick Store") + "' for a generated code");
                if (Shown("Typed Store") != "ACM-TYPED") bad.Add("the Clients store table shows '" + Shown("Typed Store") + "' for a typed code");
            }

            // The Register a store window: the code box is optional.
            using (var dlg = new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms.RegisterStoreDialog("ACME FOODS", "", "Dialog Store", null))
            {
                var dt = dlg.GetType();
                ((TextBox)dt.GetField("txtCode", CtF)!.GetValue(dlg)!).Text = "";
                dt.GetMethod("Save", CtF)!.Invoke(dlg, null);
                var made = dlg.RegisteredStore;
                if (made == null) bad.Add("the Register a store window refused a blank code: " + ((Label)dt.GetField("lblProblem", CtF)!.GetValue(dlg)!).Text);
                else if (!made.IsGeneratedCode || !System.Text.RegularExpressions.Regex.IsMatch(made.StoreCode, @"^ACM-\d{3}$")) bad.Add($"the window saved {made.StoreCode} generated={made.IsGeneratedCode}");
            }
            return string.Join("; ", bad);
        });
    }
}
