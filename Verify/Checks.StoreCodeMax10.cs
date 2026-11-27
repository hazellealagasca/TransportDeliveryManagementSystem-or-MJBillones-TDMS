using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 28: a store code TYPED by a person is at most 10 characters (letters, numbers, hyphens); generated and already saved codes are not held to it.</summary>
public static partial class Checks
{
    [Check]
    public static string StoreCodeMax10_RuleText()
    {
        var bad = new List<string>();
        void Is(string? code, bool want) { if (MJdatabase.StoreCodeTypedOk(code) != want) bad.Add($"StoreCodeTypedOk(\"{code}\") should be {want}"); }
        Is("", true); Is("   ", true); Is(null, true);
        Is("204848", true); Is("S11", true); Is("AB-1234567", true); Is("ab-1234567", true);
        Is("ABCDEFGHIJK", false); Is("12345678901", false); Is("___", false);
        // the app's normaliser drops spaces and odd characters before saving, so these are judged by what would be saved
        Is("AB 12", true); Is("A_B", true);
        if (MJdatabase.StoreCodeRuleText != "Store code: up to 10 characters (letters, numbers, hyphens).") bad.Add("rule text: " + MJdatabase.StoreCodeRuleText);
        return string.Join("; ", bad);
    }

    [Check]
    public static string StoreCodeMax10_PasteMarksTooLongInvalid()
    {
        Fresh();
        var rows = StorePasteModel.Classify("DWARDO", new[]
        {
            new PasteRow("S11", "Store One"), new PasteRow("7654321", "Store Two"),
            new PasteRow("ABCDEFGHIJK", "Store Three"), new PasteRow("12345678901", "Store Four"),
        });
        var bad = new List<string>();
        if (rows[0].Status != PasteStatus.Ok) bad.Add("S11: " + rows[0].Status + " " + rows[0].Message);
        if (rows[1].Status != PasteStatus.Ok) bad.Add("7654321: " + rows[1].Status + " " + rows[1].Message);
        for (int i = 2; i < 4; i++)
            if (rows[i].Status != PasteStatus.Invalid || rows[i].Message != MJdatabase.StoreCodeRuleText) bad.Add($"row {i}: {rows[i].Status} \"{rows[i].Message}\"");
        return string.Join("; ", bad);
    }

    [Check]
    public static string StoreCodeMax10_RegisteringRefusesLongTypedCode()
    {
        Fresh();
        var bad = new List<string>();
        try { MJdatabase.RegisterClientStore("DWARDO", "ABCDEFGHIJK", "Too Long", ""); bad.Add("an 11-character code was registered"); }
        catch (InvalidOperationException ex) { if (ex.Message != MJdatabase.StoreCodeRuleText) bad.Add("message: " + ex.Message); }
        if (MJdatabase.CheckCompanyStore("DWARDO", "12345678901", "Too Long") != MJdatabase.StoreCodeRuleText) bad.Add("CheckCompanyStore did not give the rule text");
        if (MJdatabase.CheckNewStore("DWARDO", "12345678901", "Too Long", "", out _) != MJdatabase.StoreCodeRuleText) bad.Add("CheckNewStore did not give the rule text");
        if (MJdatabase.GetClientStores("DWARDO", includeArchived: true).Any(s => s.StoreName == "Too Long")) bad.Add("a refused store was saved");
        var ok = MJdatabase.RegisterClientStore("DWARDO", "AB-1234567", "Ten Chars", "");
        if (ok.StoreCode != "AB-1234567") bad.Add("a 10-character code saved as " + ok.StoreCode);
        var num = MJdatabase.RegisterClientStore("DWARDO", "123456", "Numeric", "");
        if (num.StoreCode != "123456") bad.Add("123456 saved as " + num.StoreCode);
        var blank = MJdatabase.RegisterClientStore("DWARDO", "", "No Code", "");
        if (!blank.IsGeneratedCode) bad.Add("a blank code was not generated");
        // editing a saved short store to a too-long code is refused
        var edit = MJdatabase.GetClientStores("DWARDO", includeArchived: true).First(s => s.StoreName == "Numeric");
        edit.StoreCode = "ABCDEFGHIJK";
        try { MJdatabase.SaveClientStore(edit); bad.Add("editing to an 11-character code was saved"); }
        catch (InvalidOperationException ex) { if (ex.Message != MJdatabase.StoreCodeRuleText) bad.Add("edit message: " + ex.Message); }
        return string.Join("; ", bad);
    }

    [Check]
    public static string StoreCodeMax10_AlreadySavedLongCodeStillWorks()
    {
        Fresh();
        var bad = new List<string>();
        AddStore("DWARDO", "OLDCODE-ABCD", "Old Long Store"); // 12 characters, saved before the rule
        string? msg = MJdatabase.CheckCompanyStore("DWARDO", "OLDCODE-ABCD", "Another");
        if (msg == MJdatabase.StoreCodeRuleText) bad.Add("an already saved code was refused by the length rule");
        if (msg == null || !msg.Contains("Old Long Store")) bad.Add("the saved code was not found as taken: " + msg);
        var st = MJdatabase.GetClientStores("DWARDO", includeArchived: true).First(s => s.StoreName == "Old Long Store");
        if (MJdatabase.StoreCodeProblem(st.StoreCode) != null) bad.Add("StoreCodeProblem refused the saved code");
        st.StoreName = "Old Long Store Renamed";
        try { MJdatabase.SaveClientStore(st); }
        catch (Exception ex) { bad.Add("re-saving the store unchanged failed: " + ex.Message); }
        var again = MJdatabase.GetClientStores("DWARDO", includeArchived: true).FirstOrDefault(s => s.StoreName == "Old Long Store Renamed");
        if (again == null || again.StoreCode != "OLDCODE-ABCD") bad.Add("the saved code changed: " + again?.StoreCode);
        return string.Join("; ", bad);
    }
}
