using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 12: the head office address is optional. A client with a blank address saves through the Clients screen's own save path,
/// the Settings label has no "*", and the receipt and statement print with an empty address (no dash, no error).</summary>
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
            using (var view = new ClientManagementView())
            {
                var vt = typeof(ClientManagementView);
                void Set(string f, string v) => ((TextBox)vt.GetField(f, F)!.GetValue(view)!).Text = v;
                vt.GetField("isNewClient", F)!.SetValue(view, true);
                vt.GetField("selectedClient", F)!.SetValue(view, null);
                Set("txtClientName", name); Set("txtClientTin", "006-896-371-000"); Set("txtClientAddress", "   ");
                Set("txtContactPerson", "Juan Dela Cruz"); Set("txtContactNumber", "09171234567");
                bool ok = (bool)vt.GetMethod("SaveClient", F, null, new[] { typeof(bool) }, null)!.Invoke(view, new object[] { false })!;
                if (!ok) msgs.Add("the Clients screen refused a client with a blank address");
                if (!AllControls(view).OfType<Label>().Any(l => l.Text.Contains("Address (optional)"))) msgs.Add("the client Address label does not say optional");
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
        if (saved.Address != "") bad.Add("saved address is '" + saved.Address + "'");

        // Receipt and statement for that client.
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
