using System.Text.RegularExpressions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace TDMS.Verify;

/// <summary>Nov 14 (phase 1A): one date format (MM/dd/yyyy), one time format (hh:mm AM/PM), one email rule, all in Fmt; and no screen or printout uses the old display formats.</summary>
public static partial class Checks
{
    [Check]
    public static string GlobalFormat_FmtDates_AreMonthDayYear_StoredFormsStillRead()
    {
        var bad = new List<string>();
        var d = new DateTime(2026, 11, 14);
        if (Fmt.Date(d) != "11/14/2026") bad.Add("Date: " + Fmt.Date(d));
        if (Fmt.Date((DateTime?)null) != "") bad.Add("a missing date should be blank");
        if (Fmt.Date((DateTime?)d) != "11/14/2026") bad.Add("nullable Date");
        if (Fmt.Short(d) != "11/14") bad.Add("Short: " + Fmt.Short(d));
        if (Fmt.DateTimeText(new DateTime(2026, 11, 14, 20, 42, 0)) != "11/14/2026 08:42 PM") bad.Add("DateTimeText: " + Fmt.DateTimeText(new DateTime(2026, 11, 14, 20, 42, 0)));
        foreach (var (text, y, m, day) in new[] { ("11/14/2026", 2026, 11, 14), ("1/5/2026", 2026, 1, 5), ("2026-11-14", 2026, 11, 14), ("2026-01-05 08:30:00", 2026, 1, 5) })
        {
            if (!Fmt.TryParseDate(text, out var p) || p.Year != y || p.Month != m || p.Day != day) bad.Add($"TryParseDate '{text}' gave {p:yyyy-MM-dd}");
        }
        foreach (string no in new[] { "", "abc", "13/45/2026", "14/11/2026", "Nov 14, 2026" })
            if (Fmt.TryParseDate(no, out _)) bad.Add($"TryParseDate accepted '{no}'");
        if (Fmt.DateText("2026-11-14") != "11/14/2026") bad.Add("DateText of a stored date");
        if (Fmt.StoredDate("11/14/2026") != "2026-11-14") bad.Add("StoredDate of a shown date");
        if (Fmt.StoredDate("not a date") != "not a date") bad.Add("StoredDate must keep text that is not a date");
        if (Fmt.DatesIn("2026-11-01 to 2026-11-09") != "11/01/2026 to 11/09/2026") bad.Add("DatesIn: " + Fmt.DatesIn("2026-11-01 to 2026-11-09"));
        return string.Join("; ", bad);
    }

    [Check]
    public static string GlobalFormat_FmtTimes_TwelveHour_OldHhmmRead_OutBeforeInIsFalse()
    {
        var bad = new List<string>();
        void Is(string input, string want) { string got = Fmt.Time(input); if (got != want) bad.Add($"Time('{input}') = '{got}', wanted '{want}'"); }
        Is("2042", "08:42 PM"); Is("0139", "01:39 AM"); Is("20:42", "08:42 PM"); Is("8:42 PM", "08:42 PM"); Is("08:42 pm", "08:42 PM");
        Is("842p", "08:42 PM"); Is("1230a", "12:30 AM"); Is("12:00 PM", "12:00 PM"); Is("", ""); Is("   ", "");
        Is("soon", "soon"); // text that is not a time is kept, never lost
        foreach (string no in new[] { "13:00 PM", "0:30 AM", "8:75 PM", "2560", "25:00", "abc", "" })
            if (Fmt.TryParseTime(no, out _)) bad.Add($"TryParseTime accepted '{no}'");
        if (!Fmt.TryParseTime("12:00 AM", out var midnight) || midnight != TimeSpan.Zero) bad.Add("12:00 AM is midnight");
        if (!Fmt.TryParseTime("12:30 PM", out var noon) || noon != new TimeSpan(12, 30, 0)) bad.Add("12:30 PM is half past noon");
        if (Fmt.TimeOutOnOrAfter("2042", "0139")) bad.Add("out 01:39 AM before in 08:42 PM must be false");
        if (Fmt.TimeOutOnOrAfter("08:42 PM", "01:39 AM")) bad.Add("out before in (12-hour) must be false");
        if (!Fmt.TimeOutOnOrAfter("8:00 AM", "5:00 PM")) bad.Add("out after in must be true");
        if (!Fmt.TimeOutOnOrAfter("8:00 AM", "8:00 AM")) bad.Add("out equal to in must be true");
        if (!Fmt.TimeOutOnOrAfter("", "5:00 PM") || !Fmt.TimeOutOnOrAfter("8:00 AM", "")) bad.Add("a blank time must be true");
        if (Fmt.TimeOutOnOrAfter("junk", "5:00 PM")) bad.Add("an unreadable time must be false");
        return string.Join("; ", bad);
    }

    [Check]
    public static string GlobalFormat_Email_OneRule_InputRulesUsesIt()
    {
        var bad = new List<string>();
        foreach (string e in new[] { "a@.", "a@b", "a b@c.co", "@b.co", "a@b..co", "" })
            if (Fmt.IsValidEmail(e) || InputRules.IsValidEmail(e)) bad.Add($"email '{e}' accepted");
        foreach (string e in new[] { "a@b.co", "billing@company.com", "first.last+tag@sub.example.ph" })
            if (!Fmt.IsValidEmail(e) || !InputRules.IsValidEmail(e)) bad.Add($"email '{e}' refused");
        string src = File.ReadAllText(Path.Combine(SourceRoot(), "Views", "ClientManagementView.cs"));
        if (!src.Contains("Fmt.IsValidEmail(")) bad.Add("the client screen does not call Fmt.IsValidEmail on save");
        return string.Join("; ", bad);
    }

    [Check]
    public static string GlobalFormat_Controls_DatePickerAndTimeBox()
    {
        return RunOnSta(() =>
        {
            var bad = new List<string>();
            var p = new DateTimePicker();
            Fmt.ApplyDatePicker(p);
            if (p.Format != DateTimePickerFormat.Custom || p.CustomFormat != "MM/dd/yyyy") bad.Add("ApplyDatePicker: " + p.Format + " " + p.CustomFormat);
            var tb = new TextBox();
            Fmt.ApplyTimeBox(tb);
            var leave = typeof(Control).GetMethod("OnLeave", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            tb.Text = "842p"; leave.Invoke(tb, new object[] { EventArgs.Empty });
            if (tb.Text != "08:42 PM" || tb.Tag as string == "InvalidInput") bad.Add("a typed 842p became '" + tb.Text + "'");
            tb.Text = "2042"; leave.Invoke(tb, new object[] { EventArgs.Empty });
            if (tb.Text != "08:42 PM") bad.Add("an old 2042 became '" + tb.Text + "'");
            tb.Text = "99:99"; leave.Invoke(tb, new object[] { EventArgs.Empty });
            if (tb.Tag as string != "InvalidInput") bad.Add("an unreadable time was not marked red");
            tb.Text = "8:15 AM";
            if (tb.Tag as string == "InvalidInput") bad.Add("a readable time left the box red");
            return string.Join("; ", bad);
        });
    }

    /// <summary>Acceptance: no screen, form, printout or data-layer text uses the old display formats (stored yyyy-MM-dd values are fine).</summary>
    [Check]
    public static string GlobalFormat_NoOldDisplayFormatsInViewsFormsFunctions()
    {
        string root = SourceRoot();
        var old = new Regex(@"MMM d, yyyy|MMMM d, yyyy|dd/MM/yyyy|MMM dd, yyyy|MMMM dd, yyyy|dd-MMM-yyyy|DateTimePickerFormat\.(?:Short|Long)\b", RegexOptions.Compiled);
        var hits = new List<string>();
        foreach (var dir in new[] { "Views", "Forms", "Functions", "Database", "Models" })
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, dir), "*.cs", SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                    if (old.IsMatch(lines[i])) hits.Add($"{Path.GetRelativePath(root, file)}:{i + 1}");
            }
        return hits.Count == 0 ? "" : "old date formats still in: " + string.Join(", ", hits.Take(12));
    }
}
