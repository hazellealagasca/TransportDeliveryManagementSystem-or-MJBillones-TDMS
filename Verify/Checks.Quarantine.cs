using System.Text.RegularExpressions;

namespace TDMS.Verify;

/// <summary>Task 11: crew wage / meal allowance code appears only in the internal crew screens (and the data layer).</summary>
public static partial class Checks
{
    private static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !d.GetFiles("*.csproj").Any(f => f.Name.StartsWith("Transport Delivery"))) d = d.Parent;
        return d?.FullName ?? throw new DirectoryNotFoundException("repository root not found");
    }

    /// <summary>The body of one public static method of a file (up to the next public static member).</summary>
    private static string MethodText(string src, string name)
    {
        int a = src.IndexOf($"public static PrintFormDef {name}(", StringComparison.Ordinal);
        if (a < 0) return "";
        int b = src.IndexOf("public static ", a + 10, StringComparison.Ordinal);
        return b < 0 ? src[a..] : src[a..b];
    }

    [Check]
    public static string NoWageInClientDocs()
    {
        string root = RepoRoot();
        string[] banned = { "TripCrewPay", "GetTripCrewPay", "GetAllTripCrewPay", "DailyWage", "FoodAllowance", "BasePay", "DriverWage", "HelperWage", "DieselCost", "MealAllowance", "ResolveTripPay" };
        var hits = new List<string>();
        void Scan(string label, string text, string[] tokens)
        {
            // Comments may explain the rule; only code counts.
            string code = Regex.Replace(text, @"//[^\n]*|/\*.*?\*/", "", RegexOptions.Singleline);
            foreach (var t in tokens) if (Regex.IsMatch(code, @"\b" + t + @"\b")) hits.Add($"{label}: {t}");
        }
        foreach (var pattern in new[] { "BillingView*.cs", "StatementOfAccountView*.cs", "CollectionView*.cs", "TripReportView*.cs", "ReportsView*.cs" })
            foreach (var f in Directory.GetFiles(Path.Combine(root, "Views"), pattern)) Scan(Path.GetFileName(f), File.ReadAllText(f), banned);
        // The dispatch details window may name the "Crew pay" table in its change history, but must not read or show pay.
        Scan("DispatchDetailsDialog.cs", File.ReadAllText(Path.Combine(root, "Forms", "DispatchDetailsDialog.cs")), banned.Where(t => t != "TripCrewPay").ToArray());
        string print = File.ReadAllText(Path.Combine(root, "Functions", "PrintForms.cs"));
        foreach (var m in new[] { "CollectionReceipt", "StatementOfAccount", "SpecialTripStatementOfAccount", "StatementOfInvoices", "TripReport", "DeliveryDossier" })
            Scan("PrintForms." + m, MethodText(print, m), banned);
        return hits.Count == 0 ? "" : string.Join("; ", hits);
    }
}
