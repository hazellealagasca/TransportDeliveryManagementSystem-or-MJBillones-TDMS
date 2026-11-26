using System.Collections.Generic;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace TDMS.Verify;

/// <summary>Nov 26: the truck time in at the plant gate can't be earlier than the dispatch order's departure time (an overnight trip is fine).</summary>
public static partial class Checks
{
    [Check]
    public static string TruckTimeIn_NotBeforeDeparture_Rule()
    {
        var bad = new List<string>();
        void Expect(string dep, string tin, bool refused)
        {
            if (DailyRules.TruckInBeforeDeparture(dep, tin) != refused) bad.Add($"departure {dep}, truck in {tin}: expected {(refused ? "refused" : "allowed")}");
        }
        Expect("07:55 AM", "07:00 AM", true);    // earlier the same morning
        Expect("01:30 PM", "09:00 AM", true);    // earlier the same day
        Expect("07:55 AM", "08:10 AM", false);   // after departure
        Expect("08:00 AM", "08:00 AM", false);   // the same minute
        Expect("11:00 PM", "01:00 AM", false);   // left late, at the plant after midnight
        Expect("0755", "0700", true);            // old HHMM still reads
        Expect("", "07:00 AM", false);           // no departure on the order: not judged
        Expect("07:55 AM", "", false);           // blank truck time: the required check reports it
        Expect("07:55 AM", "abc", false);        // unreadable: the "must be a time" check reports it
        var src = System.IO.File.ReadAllText(System.IO.Path.Combine(RepoRoot(), "Views", "TripReportView.Blank.cs"));
        if (!src.Contains("DailyRules.TruckInBeforeDeparture(")) bad.Add("the trip report checks do not use the departure rule");
        return string.Join("; ", bad);
    }
}
