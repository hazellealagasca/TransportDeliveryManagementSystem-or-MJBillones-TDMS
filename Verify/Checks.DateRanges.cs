using System.Reflection;
using System.Text.RegularExpressions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// "Value of '…' is not valid for 'Value'. 'Value' should be between 'MinDate' and 'MaxDate'" (and the same for
/// numbers) must never reach a person. These checks reproduce it on the real screens and keep it from coming back.
/// </summary>
public static partial class Checks
{
    /// <summary>The Crew screen after the app was left open past midnight: its "up to today" cap is a day old.</summary>
    [Check]
    public static string DateRange_CrewScreen_LeftOpenOvernight_ClearAndBindNeverThrow()
    {
        Fresh();
        return RunOnSta(() =>
        {
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(1500, 900), Opacity = 0 };
            var view = new CrewManagementView { Dock = DockStyle.Fill };
            host.Controls.Add(view); host.Show(); Pump(800);
            var issued = FieldOf<DateTimePicker>(view, "dtpLicenseIssued");
            if (issued == null) return "the license issue date box was not found";
            issued.MaxDate = DateTime.Today.AddDays(-1);   // what the box looks like the morning after the screen was opened

            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
            var problems = new List<string>();
            void Try(string what, Action a)
            {
                try { a(); }
                catch (TargetInvocationException ex) { problems.Add($"{what}: {ex.InnerException?.GetType().Name}: {ex.InnerException?.Message}"); }
                catch (Exception ex) { problems.Add($"{what}: {ex.GetType().Name}: {ex.Message}"); }
            }
            var clear = typeof(CrewManagementView).GetMethod("ClearClassification", F)!;
            var bind = typeof(CrewManagementView).GetMethod("BindClassification", F)!;
            Try("new crew member (clear the form)", () => clear.Invoke(view, null));
            if (issued.Value.Date != DateTime.Today) problems.Add($"a cleared form should show today, shows {issued.Value:d}");
            Try("open a driver whose license dates were never filled in", () => bind.Invoke(view, new object[] { new Crew { Role = "Driver", LicenseClass = "Professional" } }));
            Try("open a driver with an issue date that is not a real date", () => bind.Invoke(view, new object[] { new Crew { Role = "Driver", LicenseClass = "Professional", LicenseIssued = DateTime.MinValue, LicenseExpiry = DateTime.MaxValue } }));
            Try("open a driver whose license was issued in the future", () => bind.Invoke(view, new object[] { new Crew { Role = "Driver", LicenseClass = "Non-Professional", LicenseIssued = DateTime.Today.AddYears(2), LicenseExpiry = DateTime.Today.AddYears(7) } }));
            host.Close();
            return string.Join(" | ", problems);
        });
    }

    /// <summary>The guard itself: any date or number ends up inside the box's limits, and "today" limits follow the calendar.</summary>
    [Check]
    public static string RangeGuard_FitsEveryValue_AndTheTodayLimitFollowsTheDay()
    {
        return RunOnSta(() =>
        {
            var problems = new List<string>();
            void Expect(string what, bool ok) { if (!ok) problems.Add(what); }

            var range = new DateTimePicker { MinDate = new DateTime(2020, 1, 1), MaxDate = new DateTime(2030, 12, 31) };
            var inside = new DateTime(2025, 6, 15);
            range.SetSafe(inside); Expect("a date inside the limits changed", range.Value == inside);
            range.SetSafe(DateTime.MinValue); Expect("a blank (0001) date was not moved to the earliest allowed", range.Value == range.MinDate);
            range.SetSafe(DateTime.MaxValue); Expect("a far-future date was not moved to the latest allowed", range.Value == range.MaxDate);
            range.SetSafe(new DateTime(2019, 12, 31)); Expect("a date just before the limit was not moved to it", range.Value == range.MinDate);

            var plain = new DateTimePicker();   // no limits set: even an unset date must not fail
            plain.SetSafe(DateTime.MinValue); Expect("an unset date failed on a box with default limits", plain.Value == plain.MinDate);

            // "up to today": built a day ago, the limit still has to let today in
            var cap = new DateTimePicker();
            cap.CapAtToday();
            Expect("the limit is not today", cap.MaxDate == DateTime.Today);
            cap.MaxDate = DateTime.Today.AddDays(-1);
            cap.SetSafe(DateTime.Today); Expect("today was refused after the day changed", cap.Value.Date == DateTime.Today && cap.MaxDate == DateTime.Today);
            cap.SetSafe(DateTime.Today.AddDays(3)); Expect("a future date was accepted by an up-to-today box", cap.Value.Date == DateTime.Today);
            cap.SetSafe(DateTime.Now); Expect("the current time of day was refused", cap.Value.Date == DateTime.Today);
            var tomorrowOk = new DateTimePicker(); tomorrowOk.CapAtToday(1);
            tomorrowOk.MaxDate = DateTime.Today.AddDays(-1);
            tomorrowOk.SetSafe(DateTime.Today.AddDays(1)); Expect("tomorrow was refused by an up-to-tomorrow box", tomorrowOk.Value.Date == DateTime.Today.AddDays(1));
            var opened = cap.Fit(DateTime.Today.AddDays(-5)); Expect("a past date was changed", opened == DateTime.Today.AddDays(-5));

            var num = new NumericUpDown { Minimum = 1, Maximum = 5000, DecimalPlaces = 2 };
            num.SetSafe(0); Expect("a number below the minimum was not moved to it", num.Value == 1);
            num.SetSafe(99999); Expect("a number above the maximum was not moved to it", num.Value == 5000);
            num.SetSafe(-3.5m); Expect("a negative number failed", num.Value == 1);
            num.SetSafe(1234.56m); Expect("a number inside the limits changed", num.Value == 1234.56m);
            var def = new NumericUpDown();      // default limits are 0 to 100
            def.SetSafe(250); Expect("a number above the default maximum failed", def.Value == 100);
            return string.Join(" | ", problems);
        });
    }

    private static string SourceRoot()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            if (d.GetFiles("Transport Delivery Management System for M.J. Billones Transport Service.csproj").Length > 0) return d.FullName;
        throw new DirectoryNotFoundException("the app's source folder was not found above " + AppContext.BaseDirectory);
    }

    /// <summary>
    /// Nobody may fill a date or number box with a bare <c>.Value =</c> or freeze "today" as its limit again: both are how
    /// "Value … is not valid for 'Value'" reached people. (Boxes are recognised by the dtp / num / nud prefix or by their declared type.)
    /// </summary>
    [Check]
    public static string RangeGuard_NoBoxIsFilledOrLimitedTheUnsafeWay()
    {
        string root = SourceRoot();
        var boxType = new Regex(@"\b(?:DateTimePicker|NumericUpDown)\??\s+(\w+)\s*[=,;)]|\bvar\s+(\w+)\s*=\s*new\s+(?:System\.Windows\.Forms\.)?(?:DateTimePicker|NumericUpDown)\b", RegexOptions.Compiled);
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, file);
            string[] parts = rel.Split(Path.DirectorySeparatorChar);
            if (parts[0] is "bin" or "obj" or "Verify" or "scratch" or "mockups" or "Downloads" or "Archive_Docs") continue;
            if (rel.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(file) == "RangeGuard.cs") continue;
            var lines = File.ReadAllLines(file);
            var typed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var l in lines) foreach (Match m in boxType.Matches(l)) typed.Add(m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i]; int c = code.IndexOf("//", StringComparison.Ordinal); if (c >= 0) code = code.Substring(0, c);
                foreach (Match m in Regex.Matches(code, @"\b(\w+)\.Value\s*=(?!=)"))
                {
                    string who = m.Groups[1].Value;
                    if (who.StartsWith("dtp", StringComparison.Ordinal) || who.StartsWith("num", StringComparison.Ordinal) || who.StartsWith("nud", StringComparison.Ordinal) || typed.Contains(who))
                        offenders.Add($"{rel}:{i + 1} {code.Trim()}");
                }
                if (Regex.IsMatch(code, @"\b(?:MaxDate|MinDate)\s*=\s*(?:System\.)?DateTime\.(?:Today|Now)\b"))
                    offenders.Add($"{rel}:{i + 1} (limit frozen to today: use CapAtToday) {code.Trim()}");
            }
        }
        return offenders.Count == 0 ? "" : $"{offenders.Count} unsafe place(s): use .SetSafe(...) / .CapAtToday() — " + string.Join(" ;; ", offenders.Take(8));
    }
}
