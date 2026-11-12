using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 12 validation audit, set-up screens: Add driver asks for the license, company details, client email / contact person / lengths, rate outlier warning, truck expiry note.</summary>
public static partial class Checks
{
    private const string VsPro = CrewManagementView.LicPro, VsNonPro = CrewManagementView.LicNonPro;

    [Check]
    public static string ValidationSetup_AddDriver_LicenseRequired_ExpiredRefused_AideNeedsNone()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        var bad = new List<string>();
        var issued = DateTime.Today.AddYears(-1); var future = DateTime.Today.AddYears(4);

        var none = QuickAddCrewDialog.TrySave(true, "Alba", "Rosa", "", "");
        if (none.Id != 0 || none.Field != "license") bad.Add($"driver without a license was not refused ({none.Problem})");
        var expired = QuickAddCrewDialog.TrySave(true, "Alba", "Rosa", "", "", VsPro, DateTime.Today.AddYears(-6), DateTime.Today.AddDays(-10));
        if (expired.Id != 0 || expired.Field != "expiry" || expired.Problem == null || !expired.Problem.Contains("expired")) bad.Add($"expired license not refused ({expired.Problem})");
        var futureIssue = QuickAddCrewDialog.TrySave(true, "Alba", "Rosa", "", "", VsPro, DateTime.Today.AddDays(5), DateTime.Today.AddYears(5));
        if (futureIssue.Id != 0 || futureIssue.Field != "issued") bad.Add("a future issue date was not refused");
        var backwards = QuickAddCrewDialog.TrySave(true, "Alba", "Rosa", "", "", VsPro, issued, issued.AddDays(-3));
        if (backwards.Id != 0 || backwards.Field != "expiry") bad.Add("expiry before issue was not refused");
        var tooLong = QuickAddCrewDialog.TrySave(true, "Alba", "Rosa", "", "", VsPro, DateTime.Today.AddYears(-1), DateTime.Today.AddYears(11));
        if (tooLong.Id != 0) bad.Add("a license longer than 10 years was not refused");
        var nonPro = QuickAddCrewDialog.TrySave(true, "Alba", "Rosa", "", "", VsNonPro, issued, future, nonProConfirmed: false);
        if (nonPro.Id != 0 || nonPro.Field != "confirm") bad.Add($"Non-Professional without the tick was not refused ({nonPro.Problem})");
        if (Long("SELECT COUNT(*) FROM Crews WHERE LastName='Alba'") != 0) bad.Add("a refused driver was still saved");

        var ok = QuickAddCrewDialog.TrySave(true, "Alba", "Rosa", "", "", VsPro, issued, future);
        if (ok.Id <= 0 || ok.Problem != null) bad.Add("a driver with a valid Professional license was refused: " + ok.Problem);
        else
        {
            var c = MJdatabase.GetCrews().FirstOrDefault(x => x.Id == ok.Id);
            if (c == null || c.LicenseClass != VsPro || c.LicenseIssued?.Date != issued.Date || c.LicenseExpiry?.Date != future.Date || !c.HasLicense) bad.Add($"license not stored on the crew ({c?.LicenseClass} {c?.LicenseIssued} {c?.LicenseExpiry})");
        }
        var ok2 = QuickAddCrewDialog.TrySave(true, "Alba", "Pedro", "", "", VsNonPro, issued, future, nonProConfirmed: true);
        if (ok2.Id <= 0) bad.Add("Non-Professional with the tick was refused: " + ok2.Problem);
        var aide = QuickAddCrewDialog.TrySave(false, "Alba", "Tina", "", "");
        if (aide.Id <= 0 || aide.Problem != null) bad.Add("an aide without a license was refused: " + aide.Problem);
        return string.Join("; ", bad);
    }

    [Check]
    public static string ValidationSetup_CompanyDetails_NameRequired_TinRule_Tidy()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        var bad = new List<string>();
        string Get(string k) => MJdatabase.GetSystemSetting(k, "?");
        var good = SystemCustomizationView.SaveCompanyDetails("  M.J.   Billones  Transport ", "Tag  line", "0917-123-4567", "708839875000", " Tarlac   City ", "Thanks");
        if (good.Name != null || good.Contact != null || good.Tin != null) bad.Add("valid details were refused");
        if (Get("CompanyName") != "M.J. Billones Transport") bad.Add("name not tidied: " + Get("CompanyName"));
        if (Get("CompanyTin") != "708-839-875-000") bad.Add("TIN not formatted: " + Get("CompanyTin"));
        if (Get("CompanyAddress") != "Tarlac City") bad.Add("address not tidied: " + Get("CompanyAddress"));
        // blank name: refused, old value kept
        var blank = SystemCustomizationView.SaveCompanyDetails("   ", "x", "0917-123-4567", "708-839-875-000", "a", "b");
        if (blank.Name == null) bad.Add("blank company name was accepted");
        if (Get("CompanyName") != "M.J. Billones Transport") bad.Add("a blank name replaced the saved one: " + Get("CompanyName"));
        // bad TIN
        var tin = SystemCustomizationView.SaveCompanyDetails("M.J. Billones Transport", "x", "0917-123-4567", "12345", "a", "b");
        if (tin.Tin == null) bad.Add("a 5-digit TIN was accepted");
        if (Get("CompanyTin") != "708-839-875-000") bad.Add("a bad TIN replaced the saved one: " + Get("CompanyTin"));
        var tin9 = SystemCustomizationView.SaveCompanyDetails("M.J. Billones Transport", "x", "0917-123-4567", "708839875", "a", "b");
        if (tin9.Tin != null || Get("CompanyTin") != "708-839-875") bad.Add("a 9-digit TIN was refused: " + tin9.Tin);
        var tinBlank = SystemCustomizationView.SaveCompanyDetails("M.J. Billones Transport", "x", "0917-123-4567", "", "a", "b");
        if (tinBlank.Tin != null) bad.Add("a blank company TIN was refused");
        // phone
        var ph = SystemCustomizationView.SaveCompanyDetails("M.J. Billones Transport", "x", "call me", "708-839-875-000", "a", "b");
        if (ph.Contact == null) bad.Add("letters in the contact number were accepted");
        if (Get("CompanyContact") != "0917-123-4567") bad.Add("a bad phone replaced the saved one: " + Get("CompanyContact"));
        var phBlank = SystemCustomizationView.SaveCompanyDetails("M.J. Billones Transport", "x", "", "708-839-875-000", "a", "b");
        if (phBlank.Contact != null) bad.Add("a blank contact number was refused (it is optional)");
        // as-you-type format + lengths on the screen
        if (InputRules.FormatTin("7088a3987-5000999") != "708-839-875-000") bad.Add("FormatTin: " + InputRules.FormatTin("7088a3987-5000999"));
        string shot = "";
        string ui = RunOnSta(() =>
        {
            SignInAsAdmin();
            var msgs = new List<string>();
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var T = typeof(SystemCustomizationView);
            var view = new SystemCustomizationView();
            var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1400, 900) };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show(); Pump(500);
            TextBox B(string f) => (TextBox)T.GetField(f, F)!.GetValue(view)!;
            if (B("txtCompanyName").MaxLength != 150) msgs.Add("name MaxLength " + B("txtCompanyName").MaxLength);
            if (B("txtCompanyTagline").MaxLength != 150) msgs.Add("tagline MaxLength");
            if (B("txtCompanyAddress").MaxLength != 250) msgs.Add("address MaxLength");
            if (B("txtInvoiceFooter").MaxLength != 300) msgs.Add("footer MaxLength");
            B("txtCompanyTin").Text = "708839875000";
            if (B("txtCompanyTin").Text != "708-839-875-000") msgs.Add("TIN box did not format as you type: " + B("txtCompanyTin").Text);
            B("txtCompanyName").Text = "";
            B("txtCompanyTin").Text = "12345";
            T.GetMethod("AutoSavePreferences", F)!.Invoke(view, new object[] { false });
            Pump(200);
            var notes = AllControls(view).OfType<Label>().Where(l => l.Visible && l.ForeColor == Color.FromArgb(185, 28, 28)).Select(l => l.Text).ToList();
            if (!notes.Any(n => n.Contains("Company name is required"))) msgs.Add("no inline note for the blank name");
            if (!notes.Any(n => n.Contains("TIN"))) msgs.Add("no inline note for the bad TIN");
            TbShot(host, "company_details_bad_tin.png");
            host.Close(); host.Dispose();
            return string.Join("; ", msgs);
        });
        if (ui.Length > 0) bad.Add(ui);
        _ = shot;
        return string.Join("; ", bad);
    }

    [Check]
    public static string ValidationSetup_Client_Email_ContactPerson_Lengths()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        var bad = new List<string>();
        foreach (var e in new[] { "a@.", "a@b", "a b@c.com", "@c.com", "a@c.", "a@@c.com", "a@c..com", "plain" })
            if (InputRules.IsValidEmail(e)) bad.Add($"email '{e}' accepted");
        foreach (var e in new[] { "billing@company.com", "a.b-c@mail.co.ph", "x_y+z@sub.domain.org" })
            if (!InputRules.IsValidEmail(e)) bad.Add($"email '{e}' refused");
        foreach (var n in new[] { "Juan 2", "R2D2", "Juan_Cruz", "Juan@Cruz", "" })
            if (InputRules.IsValidPersonName(n)) bad.Add($"contact person '{n}' accepted");
        foreach (var n in new[] { "Juan Dela Cruz", "Dela Cruz, Juan", "Ma. Cristina O'Neil-Reyes", "Peña, Niño" })
            if (!InputRules.IsValidPersonName(n)) bad.Add($"contact person '{n}' refused");
        string ui = RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var msgs = new List<string>();
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var vt = typeof(ClientManagementView);
            using var view = new ClientManagementView();
            TextBox B(string f) => (TextBox)vt.GetField(f, F)!.GetValue(view)!;
            foreach (var (f, len) in new[] { ("txtClientName", 150), ("txtClientAddress", 250), ("txtContactPerson", 100), ("txtContactNumber", 20), ("txtEmail", 100), ("txtClientTin", 15) })
                if (B(f).MaxLength != len) msgs.Add($"{f} MaxLength {B(f).MaxLength}, expected {len}");
            bool Try(string person, string email, string name)
            {
                vt.GetField("isNewClient", F)!.SetValue(view, true); vt.GetField("selectedClient", F)!.SetValue(view, null);
                B("txtClientName").Text = name; B("txtClientTin").Text = "006896371000"; B("txtClientAddress").Text = "  Some   street ";
                B("txtContactPerson").Text = person; B("txtContactNumber").Text = "09171234567"; B("txtEmail").Text = email;
                return (bool)vt.GetMethod("SaveClient", F, null, new[] { typeof(bool) }, null)!.Invoke(view, new object[] { false })!;
            }
            if (Try("Juan Dela Cruz", "a@.", "VS CLIENT ONE")) msgs.Add("client saved with email a@.");
            if (Try("Juan 2", "billing@co.com", "VS CLIENT TWO")) msgs.Add("client saved with a digit in the contact person");
            if (!Try("Juan   Dela  Cruz", "billing@co.com", "VS  CLIENT   THREE")) msgs.Add("a good client was refused");
            return string.Join("; ", msgs);
        });
        if (ui.Length > 0) bad.Add(ui);
        var saved = MJdatabase.GetClients().FirstOrDefault(c => c.CompanyName == "VS CLIENT THREE");
        if (saved == null) bad.Add("good client not saved (name tidy?)");
        else if (saved.ContactPerson != "Juan Dela Cruz" || saved.Address != "Some street") bad.Add($"text not tidied: '{saved.ContactPerson}' / '{saved.Address}'");
        if (MJdatabase.GetClients().Any(c => c.CompanyName is "VS CLIENT ONE" or "VS CLIENT TWO")) bad.Add("a refused client was saved");
        return string.Join("; ", bad);
    }

    [Check]
    public static string ValidationSetup_RateOutlier_WarnsOnExtraZero_NotOnNormal()
    {
        var bad = new List<string>();
        RateMatrix R(int id, string tt, decimal amt) => new RateMatrix { Id = id, TruckType = tt, RateAmount = amt, RouteName = "r" + id };
        var others = new[] { R(1, "FORWARD 6 TONS", 4000m), R(2, "FORWARD 6 TONS", 6500m), R(3, "FORWARD 6 TONS", 5000m), R(4, "FORWARD 4 TONS", 100m) };
        string? hi = RateMatrixView.RateOutlierWarning(45000m, "forward 6 tons", others);
        if (hi == null || !hi.Contains("₱45,000") || !hi.Contains("much higher") || !hi.Contains("₱4,000–₱6,500") || !hi.EndsWith("Save anyway?")) bad.Add("high outlier message: " + hi);
        string? lo = RateMatrixView.RateOutlierWarning(500m, "FORWARD 6 TONS", others);
        if (lo == null || !lo.Contains("much lower")) bad.Add("low outlier not flagged: " + lo);
        if (RateMatrixView.RateOutlierWarning(7000m, "FORWARD 6 TONS", others) != null) bad.Add("a normal rate was flagged");
        if (RateMatrixView.RateOutlierWarning(19000m, "FORWARD 6 TONS", others) != null) bad.Add("just under 3x was flagged");
        if (RateMatrixView.RateOutlierWarning(45000m, "FORWARD 4 TONS", others) != null) bad.Add("flagged with only one other rate for the truck type");
        if (RateMatrixView.RateOutlierWarning(45000m, "FORWARD 6 TONS", others.Take(1)) != null) bad.Add("flagged with one other rate");
        // the rate being edited is not compared with itself
        if (RateMatrixView.RateOutlierWarning(45000m, "FORWARD 6 TONS", new[] { R(1, "FORWARD 6 TONS", 4000m), R(2, "FORWARD 6 TONS", 45000m) }, excludeId: 2) != null) bad.Add("compared with itself / too few others");
        return string.Join("; ", bad);
    }

    [Check]
    public static string ValidationSetup_TruckProfile_ExpiredPaperShowsRedNote()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        MJdatabase.EnsureCapacitySchema();
        var bad = new List<string>();
        if (TruckProfileDialog.ExpiryNote("Registration", new DateTime(2026, 3, 5), new DateTime(2026, 11, 12)) != "Registration expired on Mar 5, 2026") bad.Add("note text");
        if (TruckProfileDialog.ExpiryNote("Insurance", new DateTime(2026, 11, 12), new DateTime(2026, 11, 12)) != null) bad.Add("a date of today was called expired");
        if (TruckProfileDialog.ExpiryNote("Insurance", null) != null) bad.Add("no date gave a note");
        var t = new Truck { PlateNumber = "VSX 123", TruckType = "FORWARD 4 TONS", CapacityCrates = 120, CargoType = "Cold / Chilled", Status = "Active" };
        MJdatabase.SaveTruck(t);
        MJdatabase.SaveTruckSpecs(new TruckSpecs { TruckId = t.Id, RegistrationExpiry = DateTime.Today.AddDays(-20), InsuranceExpiry = DateTime.Today.AddMonths(6) });
        string ui = RunOnSta(() =>
        {
            SignInAsAdmin();
            using var dlg = new TruckProfileDialog(t);
            dlg.StartPosition = FormStartPosition.Manual; dlg.Location = new Point(-5000, -5000);
            dlg.Show(); Pump(300);
            var texts = AllControls(dlg).OfType<Label>().Where(l => l.Text.Contains("expired on")).Select(l => l.Text).ToList();
            TbShot(dlg, "truck_profile_expired.png");
            if (!texts.Any(x => x.StartsWith("Registration expired on " + DateTime.Today.AddDays(-20).ToString("MMM d, yyyy")))) return "no red registration note: " + string.Join("|", texts);
            if (texts.Any(x => x.StartsWith("Insurance"))) return "insurance note shown for a valid date";
            return "";
        });
        if (ui.Length > 0) bad.Add(ui);
        return string.Join("; ", bad);
    }
}
