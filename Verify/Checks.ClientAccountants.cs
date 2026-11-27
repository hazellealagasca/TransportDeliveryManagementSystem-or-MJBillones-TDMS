using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 28: a client has Accountant 1 / 2; a new statement fills Received by 1 / 2 from them (only into empty or auto-filled boxes) and Prepared by with the signed-in user.</summary>
public static partial class Checks
{
    private const string AccUser = "Verify Tester";

    private static void AccSignIn() =>
        SessionContext.ActiveUser = new User { Id = 1, Username = "verify", Role = "Admin", FullName = AccUser, IsActive = true };

    [Check]
    public static string ClientAccountants_SavedAndReadBack()
    {
        Fresh(); RunBackfill();
        MJdatabase.SaveClient(new ClientCompany { CompanyName = "ACC CORP", TIN = "123-456-789-000", Address = "1 Main", ContactPerson = "Cruz, Ana", ContactNumber = "09171234567", Email = "a@acc.co", Accountant1 = "  Dela Cruz,  Juan ", Accountant2 = "Reyes, Maria" });
        var c = MJdatabase.GetClientByCompanyName("ACC CORP");
        if (c == null) return "client not saved";
        if (c.Accountant1 != "Dela Cruz,  Juan" && c.Accountant1 != "Dela Cruz, Juan") return $"Accountant1 '{c.Accountant1}'";
        if (c.Accountant2 != "Reyes, Maria") return $"Accountant2 '{c.Accountant2}'";
        var listed = MJdatabase.GetClients().FirstOrDefault(x => x.CompanyName == "ACC CORP");
        if (listed == null || listed.Accountant2 != "Reyes, Maria") return "GetClients does not return the accountants";
        c.Accountant1 = ""; c.Accountant2 = "";
        MJdatabase.SaveClient(c);
        var again = MJdatabase.GetClientByCompanyName("ACC CORP");
        return again != null && again.Accountant1 == "" && again.Accountant2 == "" ? "" : "clearing the accountants was not saved";
    }

    [Check]
    public static string ClientAccountants_NewStatementFillsReceivedByAndPreparedBy()
    {
        Fresh(); RunBackfill();
        MJdatabase.SaveClient(new ClientCompany { CompanyName = "ACC ONE", TIN = "123-456-789-000", Address = "1 Main", Accountant1 = "One, Uno", Accountant2 = "One, Dos" });
        MJdatabase.SaveClient(new ClientCompany { CompanyName = "ACC TWO", TIN = "123-456-789-001", Address = "2 Main", Accountant1 = "Two, Uno", Accountant2 = "" });
        int soaId = Seed.Soa(client: "ACC ONE", status: PayStatus.Unpaid);
        Exec("UPDATE StatementsOfAccount SET PreparedBy='Saved Preparer', ReceivedBy1='Saved One', ReceivedBy2='Saved Two' WHERE Id=@i", ("@i", soaId));
        return RunOnSta(() =>
        {
            AccSignIn();
            using var boxes = new VdBoxes();
            var (view, host) = LockOpen();
            using var _ = host;
            var T = typeof(StatementOfAccountView);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var prep = LockMember<TextBox>(view, "txtPreparedBy")!; var r1 = LockMember<TextBox>(view, "txtReceivedBy1")!; var r2 = LockMember<TextBox>(view, "txtReceivedBy2")!;
            var setClient = T.GetMethod("SetClientQuietly", flags)!;
            void Open() { T.GetMethod("OpenNewStatement", flags)!.Invoke(view, null); Pump(400); }
            void Pick(string n) { setClient.Invoke(view, new object[] { n }); Pump(200); }
            var bad = new List<string>();

            Open();
            if (prep.Text != AccUser) bad.Add($"Prepared by '{prep.Text}' on a new statement");
            Pick("ACC ONE");
            if (r1.Text != "One, Uno" || r2.Text != "One, Dos") bad.Add($"Received by after picking ACC ONE: '{r1.Text}' / '{r2.Text}'");
            Pick("ACC TWO");   // auto-filled values are replaced by the next client's
            if (r1.Text != "Two, Uno" || r2.Text != "") bad.Add($"Received by after switching to ACC TWO: '{r1.Text}' / '{r2.Text}'");

            // typed text is never overwritten
            r1.Text = "Typed, Person";
            Pick("ACC ONE");
            if (r1.Text != "Typed, Person") bad.Add($"typed Received by 1 was overwritten: '{r1.Text}'");
            if (r2.Text != "One, Dos") bad.Add($"Received by 2 (still auto) did not follow the client: '{r2.Text}'");
            prep.Text = "Typed Preparer";
            Pick("ACC TWO");
            if (prep.Text != "Typed Preparer") bad.Add("typed Prepared by was overwritten");

            // Reset / Clear form: back to a fresh form, Prepared by is the user again
            T.GetMethod("ClearForm", flags)!.Invoke(view, null); Pump(200);
            if (prep.Text != AccUser || r1.Text != "" || r2.Text != "") bad.Add($"after Reset: prepared '{prep.Text}', received '{r1.Text}' / '{r2.Text}'");
            Pick("ACC ONE");
            if (r1.Text != "One, Uno") bad.Add($"after Reset the client's accountant was not filled: '{r1.Text}'");

            // a saved statement keeps its own values
            T.GetMethod("OpenEditStatement", flags)!.Invoke(view, new object[] { MJdatabase.GetStatementOfAccountById(soaId)! }); Pump(500);
            if (prep.Text != "Saved Preparer" || r1.Text != "Saved One" || r2.Text != "Saved Two") bad.Add($"saved statement shows '{prep.Text}' / '{r1.Text}' / '{r2.Text}'");
            host.Close();
            return bad.Count == 0 ? "" : string.Join("; ", bad);
        });
    }
}
