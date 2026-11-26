using System.Collections.Generic;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace TDMS.Verify;

/// <summary>Nov 26: the truck time in at the plant gate must be later than the dispatch order's departure time (the same or earlier is refused; overnight is fine).</summary>
public static partial class Checks
{
    [Check]
    public static string TruckTimeIn_MustBeAfterDeparture_Rule()
    {
        var bad = new List<string>();
        void Expect(string dep, string tin, bool refused)
        {
            if (DailyRules.TruckInNotAfterDeparture(dep, tin) != refused) bad.Add($"departure {dep}, truck in {tin}: expected {(refused ? "refused" : "allowed")}");
        }
        Expect("07:55 AM", "08:10 AM", false);   // at the plant after departing: fine
        Expect("07:55 AM", "07:55 AM", true);    // the same time: refused
        Expect("07:55 AM", "07:00 AM", true);    // earlier the same morning: refused
        Expect("01:30 PM", "09:00 AM", true);    // earlier the same day: refused
        Expect("11:00 PM", "01:00 AM", false);   // departs late, at the plant after midnight: fine
        Expect("0755", "0700", true);            // old HHMM still reads
        Expect("", "07:00 AM", false);           // no departure on the order: not judged
        Expect("07:55 AM", "", false);           // blank truck time: the required check reports it
        Expect("07:55 AM", "abc", false);        // unreadable: the "must be a time" check reports it
        var src = System.IO.File.ReadAllText(System.IO.Path.Combine(RepoRoot(), "Views", "TripReportView.Blank.cs"));
        if (!src.Contains("DailyRules.TruckInNotAfterDeparture(")) bad.Add("the trip report checks do not use the departure rule");
        return string.Join("; ", bad);
    }
}
