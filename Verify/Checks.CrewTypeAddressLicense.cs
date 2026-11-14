using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 14 (phase 1C): crew employment type, required address, Professional licence for drivers (optional for aides), list filters, status-change reason.</summary>
public static partial class Checks
{
    private static readonly DateTime CtIssued = DateTime.Today.AddYears(-1), CtExpiry = DateTime.Today.AddYears(4);
    private const string CtPro = CrewManagementView.LicPro, CtNonPro = CrewManagementView.LicNonPro;

    [Check]
    public static string CrewTypeAddressLicense_Driver_NeedsAProfessionalLicense()
    {
        Fresh();
        var bad = new List<string>();
        var none = QuickAddCrewDialog.TrySave(true, "Dela", "Ana", "", "", address: VsAddr);
        if (none.Id != 0 || none.Field != "license") bad.Add($"a driver with no licence was not refused ({none.Problem})");
        var noneText = QuickAddCrewDialog.TrySave(true, "Dela", "Ana", "", "", CrewManagementView.LicNone, null, null, VsAddr);
        if (noneText.Id != 0 || noneText.Field != "license") bad.Add("a driver with licence 'None' was not refused");
        var nonPro = QuickAddCrewDialog.TrySave(true, "Dela", "Ana", "", "", CtNonPro, CtIssued, CtExpiry, VsAddr);
        if (nonPro.Id != 0 || nonPro.Field != "license") bad.Add($"a driver with a Non-Professional licence was not refused ({nonPro.Problem})");
        var noDates = QuickAddCrewDialog.TrySave(true, "Dela", "Ana", "", "", CtPro, null, null, VsAddr);
        if (noDates.Id != 0 || noDates.Field != "issued") bad.Add("a Professional licence without its dates was not refused");
        var expired = QuickAddCrewDialog.TrySave(true, "Dela", "Ana", "", "", CtPro, DateTime.Today.AddYears(-6), DateTime.Today.AddDays(-10), VsAddr);
        if (expired.Id != 0 || expired.Field != "expiry") bad.Add("an expired driver's licence was not refused");
        if (Long("SELECT COUNT(*) FROM Crews WHERE LastName='Dela'") != 0) bad.Add("a refused driver was still saved");
        var ok = QuickAddCrewDialog.TrySave(true, "Dela", "Ana", "", "", CtPro, CtIssued, CtExpiry, VsAddr);
        if (ok.Id <= 0 || ok.Problem != null) bad.Add("a driver with a Professional licence was refused: " + ok.Problem);
        if (CrewRules.CheckLicense(true, CtNonPro, CtIssued, CtExpiry) == null) bad.Add("CrewRules accepted a Non-Professional driver");
        if (CrewRules.CheckLicense(true, CtPro, CtIssued, CtExpiry) != null) bad.Add("CrewRules refused a Professional driver");
        // the screen: a driver's licence list offers Professional only; an aide's offers all three
        string screen = RunOnSta(() =>
        {
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(1500, 900), Opacity = 0 };
            var view = new CrewManagementView { Dock = DockStyle.Fill };
            host.Controls.Add(view); host.Show(); Pump(600);
            var role = FieldOf<ComboBox>(view, "cbRole"); var lic = FieldOf<ComboBox>(view, "cbLicenseClass");
            if (role == null || lic == null) return "role or licence box not found";
            var probs = new List<string>();
            role.SelectedItem = CrewManagementView.RoleDriver; Pump(100);
            if (lic.Items.Count != 1 || lic.Items[0]?.ToString() != CtPro) probs.Add("a driver's licence list: " + string.Join("/", lic.Items.Cast<object>()));
            role.SelectedItem = CrewManagementView.RoleAide; Pump(100);
            if (lic.Items.Count != 3 || !lic.Items.Contains(CrewManagementView.LicNone)) probs.Add("an aide's licence list: " + string.Join("/", lic.Items.Cast<object>()));
            return string.Join("; ", probs);
        });
        if (screen.Length > 0) bad.Add(screen);
        return string.Join("; ", bad);
    }

    [Check]
    public static string CrewTypeAddressLicense_Aide_LicenceOptional()
    {
        Fresh();
        var bad = new List<string>();
        var plain = QuickAddCrewDialog.TrySave(false, "Reyes", "Ben", "", "", address: VsAddr);
        if (plain.Id <= 0 || plain.Problem != null) bad.Add("an aide without a licence was refused: " + plain.Problem);
        var withNone = QuickAddCrewDialog.TrySave(false, "Reyes", "Cora", "", "", CrewManagementView.LicNone, null, null, VsAddr);
        if (withNone.Id <= 0) bad.Add("an aide with licence 'None' was refused: " + withNone.Problem);
        var withLic = QuickAddCrewDialog.TrySave(false, "Reyes", "Dino", "", "", CtNonPro, CtIssued, CtExpiry, VsAddr);
        if (withLic.Id <= 0) bad.Add("an aide with a licence and its dates was refused: " + withLic.Problem);
        var noDates = QuickAddCrewDialog.TrySave(false, "Reyes", "Eli", "", "", CtPro, null, null, VsAddr);
        if (noDates.Id != 0) bad.Add("an aide's licence without dates was accepted");
        var crews = MJdatabase.GetCrews();
        var a = crews.FirstOrDefault(c => c.Id == plain.Id);
        if (a == null || a.HasLicense || a.LicenseClass != "None") bad.Add("the aide without a licence is stored with one");
        var b = crews.FirstOrDefault(c => c.Id == withLic.Id);
        if (b == null || b.LicenseClass != CtNonPro || b.LicenseExpiry?.Date != CtExpiry.Date) bad.Add("the aide's licence was not stored");
        return string.Join("; ", bad);
    }

    [Check]
    public static string CrewTypeAddressLicense_Address_IsRequired_AndStored()
    {
        Fresh();
        var bad = new List<string>();
        foreach (var (what, addr) in new[] { ("a missing address", ""), ("a blank address", "    "), ("an address over 250 characters", new string('a', 251)) })
        {
            var r = QuickAddCrewDialog.TrySave(false, "Soto", "Gina", "", "", address: addr);
            if (r.Id != 0 || r.Field != "address") bad.Add($"{what} was not refused ({r.Problem})");
        }
        if (Long("SELECT COUNT(*) FROM Crews WHERE LastName='Soto'") != 0) bad.Add("a refused crew member was still saved");
        if (CrewRules.CheckAddress("  ") == null || CrewRules.CheckAddress(null) == null) bad.Add("CrewRules accepted a blank address");
        if (CrewRules.CheckAddress("12 Rizal St") != null || CrewRules.CheckAddress(new string('a', 250)) != null) bad.Add("CrewRules refused a good address");
        var ok = QuickAddCrewDialog.TrySave(false, "Soto", "Gina", "", "", address: "  12   Rizal St,   Tarlac City ");
        if (ok.Id <= 0) bad.Add("a good address was refused: " + ok.Problem);
        else
        {
            string saved = MJdatabase.GetCrews().First(c => c.Id == ok.Id).Address;
            if (saved != "12 Rizal St, Tarlac City") bad.Add("the address was not tidied and stored: '" + saved + "'");
            var crew = MJdatabase.GetCrews().First(c => c.Id == ok.Id);
            crew.Address = "99 New Road";
            MJdatabase.SaveCrew(crew);
            if (MJdatabase.GetCrews().First(c => c.Id == ok.Id).Address != "99 New Road") bad.Add("an updated address was not stored");
        }
        // the screen refuses a blank address too
        string screen = RunOnSta(() =>
        {
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(1500, 900), Opacity = 0 };
            var view = new CrewManagementView { Dock = DockStyle.Fill };
            host.Controls.Add(view); host.Show(); Pump(600);
            var txt = FieldOf<TextBox>(view, "txtAddress");
            if (txt == null) return "the address box was not found on the crew form";
            return txt.MaxLength == 250 ? "" : "the address box allows " + txt.MaxLength + " characters";
        });
        if (screen.Length > 0) bad.Add(screen);
        return string.Join("; ", bad);
    }

    [Check]
    public static string CrewTypeAddressLicense_EmploymentType_AndTheListFilters()
    {
        Fresh();
        var bad = new List<string>();
        var reg = QuickAddCrewDialog.TrySave(false, "Filter", "Reg", "", "", address: VsAddr);
        var sea = QuickAddCrewDialog.TrySave(false, "Filter", "Sea", "", "", address: VsAddr, seasonal: true);
        var sea2 = QuickAddCrewDialog.TrySave(true, "Filter", "Drv", "", "", CtPro, CtIssued, CtExpiry, VsAddr, true);
        if (reg.Id <= 0 || sea.Id <= 0 || sea2.Id <= 0) return "could not seed crew: " + reg.Problem + sea.Problem + sea2.Problem;
        MJdatabase.ChangeCrewStatus(sea2.Id, false, "Contract ended");
        var all = MJdatabase.GetCrews().Where(c => c.LastName == "Filter").ToList();
        if (all.First(c => c.Id == reg.Id).EmploymentType != "Regular" || all.First(c => c.Id == sea.Id).EmploymentType != "Seasonal") bad.Add("employment type was not stored");
        if (!all.First(c => c.Id == sea.Id).IsSeasonal) bad.Add("Seasonal is not kept in IsSeasonal");
        int N(string type, string status) => CrewRules.Filter(all, type, status).Count;
        if (N("All", "All") != 3) bad.Add("All/All = " + N("All", "All"));
        if (N("Regular", "All") != 1) bad.Add("Regular = " + N("Regular", "All"));
        if (N("Seasonal", "All") != 2) bad.Add("Seasonal = " + N("Seasonal", "All"));
        if (N("All", "Active") != 2) bad.Add("Active = " + N("All", "Active"));
        if (N("All", "Inactive") != 1) bad.Add("Inactive = " + N("All", "Inactive"));
        if (N("Seasonal", "Inactive") != 1 || N("Regular", "Inactive") != 0) bad.Add("Type and Status together");
        if (N("All", "On trip") != 0) bad.Add("nobody is on a trip");
        all.First(c => c.Id == reg.Id).IsAvailable = false;
        if (N("All", "On trip") != 1) bad.Add("On trip = " + N("All", "On trip"));
        string screen = RunOnSta(() =>
        {
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(1500, 900), Opacity = 0 };
            var view = new CrewManagementView { Dock = DockStyle.Fill };
            host.Controls.Add(view); host.Show(); Pump(600);
            var type = FieldOf<ComboBox>(view, "cbTypeFilter"); var status = FieldOf<ComboBox>(view, "cbStatusFilter");
            if (type == null || status == null) return "the Type or Status filter was not found";
            var probs = new List<string>();
            if (string.Join("/", type.Items.Cast<object>()) != "All/Regular/Seasonal") probs.Add("Type choices: " + string.Join("/", type.Items.Cast<object>()));
            if (string.Join("/", status.Items.Cast<object>()) != "All/Active/On trip/Inactive") probs.Add("Status choices: " + string.Join("/", status.Items.Cast<object>()));
            var flow = FieldOf<FlowLayoutPanel>(view, "pnlGridActionsFlow");
            if (flow != null && flow.Controls.GetChildIndex(type) > flow.Controls.Count - 2) probs.Add("the filters are not left of the Actions button");
            type.SelectedItem = "Seasonal"; Pump(100);
            var apply = typeof(CrewManagementView).GetMethod("ApplyCrewFilters", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var narrowed = (List<Crew>)apply.Invoke(view, new object[] { all })!;
            if (narrowed.Count != 2 || narrowed.Any(c => !c.IsSeasonal)) probs.Add("the Type filter on the screen left " + narrowed.Count);
            var grid = FieldOf<DataGridView>(view, "gridCrew");
            if (grid == null || !grid.Columns.Cast<DataGridViewColumn>().Any(c => c.HeaderText.StartsWith("Employment type"))) probs.Add("the list has no Employment type column");
            return string.Join("; ", probs);
        });
        if (screen.Length > 0) bad.Add(screen);
        return string.Join("; ", bad);
    }

    [Check]
    public static string CrewTypeAddressLicense_StatusChange_NeedsADescription_AndIsKeptInTheRecords()
    {
        Fresh();
        var bad = new List<string>();
        var r = QuickAddCrewDialog.TrySave(false, "Status", "Test", "", "", address: VsAddr);
        if (r.Id <= 0) return "could not seed: " + r.Problem;
        foreach (string blank in new[] { "", "   ", null! })
            if (!TmRefused(() => MJdatabase.ChangeCrewStatus(r.Id, false, blank), out _)) bad.Add("a status change without a description was not refused");
        if (!TmRefused(() => MJdatabase.ChangeCrewStatus(r.Id, false, new string('x', 501)), out _)) bad.Add("a description over 500 characters was not refused");
        if (Long("SELECT IsActive FROM Crews WHERE Id=@i", ("@i", r.Id)) != 1) bad.Add("a refused change still made the person inactive");
        if (Long("SELECT COUNT(*) FROM CrewRecords WHERE CrewId=@i", ("@i", r.Id)) != 0) bad.Add("a refused change still wrote a record");
        MJdatabase.ChangeCrewStatus(r.Id, false, "  Resigned   last week ");
        if (Long("SELECT IsActive FROM Crews WHERE Id=@i", ("@i", r.Id)) != 0) bad.Add("the person is still active");
        var rec = MJdatabase.GetCrewRecords(r.Id).FirstOrDefault(x => x.Kind == "Status change");
        if (rec == null) bad.Add("no 'Status change' record was written");
        else if (rec.Description != "Resigned last week" || !rec.Type.Contains("Inactive")) bad.Add($"record: '{rec.Type}' / '{rec.Description}'");
        MJdatabase.ChangeCrewStatus(r.Id, true, "Rehired");
        if (Long("SELECT IsActive FROM Crews WHERE Id=@i", ("@i", r.Id)) != 1) bad.Add("the person is not active again");
        if (MJdatabase.GetCrewRecords(r.Id).Count(x => x.Kind == "Status change") != 2) bad.Add("expected two status-change records");
        if (CrewRules.CheckStatusReason("") == null || CrewRules.CheckStatusReason("ok") != null) bad.Add("CrewRules.CheckStatusReason");
        return string.Join("; ", bad);
    }
}
