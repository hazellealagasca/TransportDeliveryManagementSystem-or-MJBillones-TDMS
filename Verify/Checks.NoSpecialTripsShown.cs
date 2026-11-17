using System.Text.RegularExpressions;

namespace TDMS.Verify;

/// <summary>Nov 17: special trips are removed from everything the user sees (source scan; the records and database code stay).</summary>
public static partial class Checks
{
    private static string CodeOnly(string path)
    {
        string text = File.ReadAllText(path);
        return Regex.Replace(text, @"//[^\n]*|/\*.*?\*/", "", RegexOptions.Singleline);   // comments may explain the removal; only code counts
    }

    [Check]
    public static string NoSpecialTripsShown_SourceScan()
    {
        string root = RepoRoot();
        var problems = new List<string>();
        string Code(params string[] parts) => CodeOnly(Path.Combine(new[] { root }.Concat(parts).ToArray()));

        // Master Data → Clients → profile: no "Special trips" tile.
        string cm = Code("Views", "ClientManagementView.cs");
        if (cm.Contains("\"Special trips\"")) problems.Add("ClientManagementView still shows a \"Special trips\" text/tile");

        // Order History: no tab, no summary metric, no type in Find a transaction.
        string oh = Code("Views", "OrderHistoryView.cs");
        if (oh.Contains("\"Special trips\"")) problems.Add("OrderHistoryView still has \"Special trips\" (tab or summary metric)");
        if (Regex.IsMatch(oh, @"SegmentTabs\([^)]*Special", RegexOptions.IgnoreCase)) problems.Add("OrderHistoryView SegmentTabs still lists Special trips");
        if (Code("Forms", "FindTransactionDialog.cs").Contains("KSpecial")) problems.Add("FindTransactionDialog still offers the Special trip type");

        // Global search: special-trip results only behind the (always false) gate.
        string gs = Code("Functions", "GlobalSearch.cs");
        foreach (Match m in Regex.Matches(gs, @"GetSpecialTrips\(\)"))
        {
            string before = gs.Substring(Math.Max(0, m.Index - 160), Math.Min(160, m.Index));
            if (!before.Contains("Features.On(Features.SpecialTrips)")) problems.Add("GlobalSearch adds special-trip results without the Features gate");
        }

        // Order History data: the special trip rows are added only behind the gate.
        string ti = Code("Functions", "TransactionIndex.cs");
        if (!Regex.IsMatch(ti, @"foreach \(var t in Features\.On\(Features\.SpecialTrips\)")) problems.Add("TransactionIndex lists special trips without the gate");

        // Settings: no switch row; Trip report picker: no show-option.
        if (Code("Views", "FeaturesSettingsView.cs").Contains("new(Features.SpecialTrips")) problems.Add("FeaturesSettingsView still lists the Special trips switch");
        if (Code("Views", "TripReportView.PickFilter.cs").Contains("\"Special trips\"")) problems.Add("TripReportView.PickFilter still has the Special trips option");

        // Audit trail: the Area filter drops the Special trips entry.
        if (!Code("Views", "AuditTrailView.cs").Contains("ModuleNames[\"SpecialTrips\"]")) problems.Add("AuditTrailView Area filter does not drop Special trips");

        // Statement waiting list and invoice picker: special rows only behind the gate.
        if (!Regex.IsMatch(Code("Functions", "StatementReadyPanel.cs"), @"Features\.On\(Features\.SpecialTrips\)[^;]*\s*all\.AddRange")) problems.Add("StatementReadyPanel lists special trips without the gate");
        if (!Code("Views", "BillingView.cs").Contains("Features.On(Features.SpecialTrips)) completedDeliveries.AddRange(SpecialTripsToInvoice())")) problems.Add("BillingView lists special trips to invoice without the gate");

        return string.Join("; ", problems);
    }
}
