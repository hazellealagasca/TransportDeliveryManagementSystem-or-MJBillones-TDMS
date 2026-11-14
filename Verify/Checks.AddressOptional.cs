using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// Nov 14 (reverses Nov 12 for corporate clients): a corporate client needs its address, so the Clients screen refuses one without it and the
/// label no longer says optional. The business address in Settings stays optional (no "*"). A client saved earlier with a blank address
/// (the old rule) still prints a receipt and a statement with an empty address (no dash, no error).
/// </summary>
public static partial class Checks
{
    private static IEnumerable<Control> AllControls(Control root) { foreach (Control c in root.Controls) { yield return c; foreach (var d in AllControls(c)) yield return d; } }

    [Check]
    public static string ClientAddress_Blank_SavesAndPrints()
    {
        Fresh();
        var bad = new List<string>();
        const string name = "BLANK ADDR CO";
        var ui = RunOnSta(() =>
        {
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
            var msgs = new List<string>();
            using (var boxes = new VdBoxes())
            using (var view = new ClientManagementView())
            {
                var vt = typeof(ClientManagementView);
                void Set(string f, string v) => ((TextBox)vt.GetField(f, F)!.GetValue(view)!).Text = v;
                vt.GetField("isNewClient", F)!.SetValue(view, true);
                vt.GetField("selectedClient", F)!.SetValue(view, null);
                Set("txtClientName", name); Set("txtClientTin", "006-896-371-000"); Set("txtClientAddress", "   ");
                Set("txtContactPerson", "Juan Dela Cruz"); Set("txtContactNumber", "09171234567"); Set("txtEmail", "billing@blankaddr.co");
                var save = vt.GetMethod("SaveClient", F, null, new[] { typeof(bool) }, null)!;
                bool ok = (bool)save.Invoke(view, new object[] { false })!;
                if (ok) msgs.Add("the Clients screen saved a corporate client with a blank address");
                if (!boxes.Boxes.Any(b => b.Contains("Address"))) msgs.Add("the refusal did not name the Address: " + string.Join(" / ", boxes.Boxes));
                if (MJdatabase.GetClients().Any(c => c.CompanyName == name)) msgs.Add("a corporate client without an address was written to the database");
                var addrLabels = AllControls(view).OfType<Label>().Where(l => l.Text.StartsWith("Address")).ToList();
                if (addrLabels.Count == 0) msgs.Add("the client Address label was not found");
                if (addrLabels.Any(l => l.Text.Contains("optional"))) msgs.Add("the client Address label still says optional");
                Set("txtClientAddress", "BCS Cmpd., Alcantara St., Vigan City");
                ok = (bool)save.Invoke(view, new object[] { false })!;
                if (!ok) msgs.Add("the Clients screen refused a corporate client with every detail: " + string.Join(" / ", boxes.Boxes));
            }
            using (var view = new SystemCustomizationView())
            {
                var labels = AllControls(view).OfType<Label>().Where(l => l.Text.Contains("Business address")).ToList();
                if (labels.Count == 0) msgs.Add("settings Business address label not found");
                foreach (var l in labels) if (l.Text.Contains('*')) msgs.Add("settings label still has a star: " + l.Text);
                if (!labels.Any(l => l.Text.Contains("(optional)"))) msgs.Add("settings label does not say optional");
            }
            return string.Join("; ", msgs);
        });
        if (ui.Length > 0) bad.Add(ui);
        var saved = MJdatabase.GetClients().FirstOrDefault(c => c.CompanyName == name);
        if (saved == null) { bad.Add("client not saved"); return string.Join("; ", bad); }
        if (saved.Address != "BCS Cmpd., Alcantara St., Vigan City") bad.Add("saved address is '" + saved.Address + "'");
        if (saved.ClientType != ClientTypes.Corporate) bad.Add("saved client type is '" + saved.ClientType + "'");

        // A client saved under the old rule (no address) still prints without a dash or an error.
        Exec("UPDATE Clients SET Address = '' WHERE CompanyName = @n", ("@n", name));
        int soaId = Seed.Soa(client: name, status: PayStatus.Unpaid, total: 600m);
        foreach (var a in new[] { 100m, 200m, 300m }) Seed.Invoice(Seed.Delivery(client: name), a, PayStatus.Unpaid, client: name, soaId: soaId);
        var rows = MJdatabase.SaveOneReceipt("0150", TargetFor(soaId), "Cash", "", "");
        string rh;
        try { rh = Html(PrintForms.CollectionReceipt(rows[0])); } catch (Exception ex) { return string.Join("; ", bad.Append("receipt print threw " + ex.Message)); }
        if (!rh.Contains(name)) bad.Add("receipt does not show the client");
        if (rh.Contains("—")) bad.Add("receipt shows a dash for the blank address");
        var (soa, items) = PrintSample(SoaTypes.Normal);
        soa.ClientName = name; soa.ClientAddress = "";
        string sh;
        try { sh = Html(PrintForms.StatementOfAccount(soa, items)); } catch (Exception ex) { return string.Join("; ", bad.Append("statement print threw " + ex.Message)); }
        if (!sh.Contains(name)) bad.Add("statement does not show the client");
        if (sh.Contains("—")) bad.Add("statement shows a dash for the blank address");
        return string.Join("; ", bad);
    }
}
