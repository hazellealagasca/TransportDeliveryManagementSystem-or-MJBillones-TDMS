using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace TDMS.Verify;

/// <summary>Nov 14 (phase 1B): a truck status change needs a description; Under Maintenance needs the breakdown and estimated fix dates; every change leaves a history row with a truck snapshot.</summary>
public static partial class Checks
{
    private static int TmTruck(string plate = "MNT-001", string category = "4T", int capacity = 280, string cargo = "Cold / Chilled")
        => (int)Exec("INSERT INTO Trucks (PlateNumber, TruckType, CapacityCrates, Status, Remarks, CargoType) VALUES (@p, @t, @c, 'Active', '', @g)", ("@p", plate), ("@t", category), ("@c", capacity), ("@g", cargo));

    private static bool TmRefused(Action a, out string why)
    {
        try { a(); why = ""; return false; }
        catch (InvalidOperationException ex) { why = ex.Message; return true; }
    }

    [Check]
    public static string TruckMaintenanceStatus_UnderMaintenance_NeedsDescriptionAndDates()
    {
        Fresh();
        int id = TmTruck();
        var bad = new List<string>();
        void Refuse(string what, string status, string desc, DateTime? b, DateTime? f)
        {
            if (!TmRefused(() => MJdatabase.ApplyTruckStatusChange(id, status, desc, b, f), out _)) bad.Add(what + " was not refused");
        }
        Refuse("no dates", "Under Maintenance", "Engine overheated", null, null);
        Refuse("no breakdown date", "Under Maintenance", "Engine overheated", null, DateTime.Today.AddDays(3));
        Refuse("no fix date", "Under Maintenance", "Engine overheated", DateTime.Today.AddDays(-1), null);
        Refuse("a breakdown date in the future", "Under Maintenance", "Engine overheated", DateTime.Today.AddDays(2), DateTime.Today.AddDays(9));
        Refuse("a fix date before the breakdown", "Under Maintenance", "Engine overheated", DateTime.Today.AddDays(-3), DateTime.Today.AddDays(-5));
        Refuse("a blank description", "Under Maintenance", "   ", DateTime.Today.AddDays(-1), DateTime.Today.AddDays(4));
        Refuse("a description over 500 characters", "Under Maintenance", new string('x', 501), DateTime.Today.AddDays(-1), DateTime.Today.AddDays(4));
        Refuse("decommissioning without a description", "Decommissioned", "", null, null);
        if (Str("SELECT Status FROM Trucks WHERE Id=@i", ("@i", id)) != "Active") bad.Add("a refused change still moved the status");
        if (Long("SELECT COUNT(*) FROM TruckMaintenance WHERE TruckId=@i", ("@i", id)) != 0) bad.Add("a refused change still wrote a history row");
        if (Long("SELECT COUNT(*) FROM TruckStatusHistory WHERE TruckId=@i", ("@i", id)) != 0) bad.Add("a refused change still wrote a status log");
        if (MJdatabase.CheckTruckStatusChange("Under Maintenance", "ok", null, null) == null) bad.Add("CheckTruckStatusChange accepted missing dates");
        if (MJdatabase.CheckTruckStatusChange("Active", "Repaired", null, null) != null) bad.Add("Active needs only a description");
        return string.Join("; ", bad);
    }

    [Check]
    public static string TruckMaintenanceStatus_WithDates_SavesBothDatesAndOneHistoryRowWithSnapshot()
    {
        Fresh();
        int id = TmTruck("MNT-002", "FWD/6T", 300, "Dry Goods");
        var bad = new List<string>();
        var breakdown = DateTime.Today.AddDays(-2); var fix = DateTime.Today.AddDays(5);
        MJdatabase.ApplyTruckStatusChange(id, "Under Maintenance", "  Engine   overheated on the road ", breakdown, fix);
        if (Str("SELECT Status FROM Trucks WHERE Id=@i", ("@i", id)) != "Under Maintenance") bad.Add("status not saved");
        if (Str("SELECT BreakdownDate FROM Trucks WHERE Id=@i", ("@i", id)) != breakdown.ToString("yyyy-MM-dd")) bad.Add("BreakdownDate not saved");
        if (Str("SELECT EstimatedFixDate FROM Trucks WHERE Id=@i", ("@i", id)) != fix.ToString("yyyy-MM-dd")) bad.Add("EstimatedFixDate not saved");
        if (Long("SELECT COUNT(*) FROM TruckMaintenance WHERE TruckId=@i", ("@i", id)) != 1) bad.Add("expected exactly one history row");
        var rows = MJdatabase.GetTruckMaintenance(id);
        var r = rows.FirstOrDefault();
        if (r == null) bad.Add("the history row cannot be read");
        else
        {
            if (r.EntryType != "Breakdown") bad.Add("EntryType " + r.EntryType);
            if (r.ServiceDate.Date != breakdown.Date) bad.Add("ServiceDate is not the breakdown date");
            if (r.NextDueDate?.Date != fix.Date) bad.Add("NextDueDate is not the estimated fix date");
            if (r.Task != "Engine overheated on the road") bad.Add("Task is '" + r.Task + "'");
            if (r.PlateNumber != "MNT-002" || r.TruckCategory != "FWD/6T" || r.CrateTempType != "Dry Goods" || r.Capacity != 300) bad.Add($"snapshot: {r.PlateNumber}/{r.TruckCategory}/{r.CrateTempType}/{r.Capacity}");
        }
        // the snapshot is as it was then: a later change of the truck does not rewrite it
        Exec("UPDATE Trucks SET TruckType='8T', CapacityCrates=500 WHERE Id=@i", ("@i", id));
        if (MJdatabase.GetTruckMaintenance(id).First().TruckCategory != "FWD/6T") bad.Add("the snapshot changed with the truck");
        var truck = MJdatabase.GetTrucks().First(t => t.Id == id);
        if (truck.BreakdownDate?.Date != breakdown.Date || truck.EstimatedFixDate?.Date != fix.Date) bad.Add("GetTrucks does not carry the two dates");
        if (!truck.StatusDisplay.Contains("Breakdown " + Fmt.Date(breakdown)) || !truck.StatusDisplay.Contains("fix by " + Fmt.Date(fix))) bad.Add("list status text: " + truck.StatusDisplay.Replace('\n', '|'));
        if (Long("SELECT COUNT(*) FROM TruckStatusHistory WHERE TruckId=@i AND NewStatus='Under Maintenance'", ("@i", id)) != 1) bad.Add("the status change was not logged");
        var all = MJdatabase.GetAllTruckMaintenance().Where(m => m.TruckId == id).ToList();
        if (all.Count != 1 || all[0].PlateNumber != "MNT-002") bad.Add("the all-trucks history does not list the row");
        if (TruckMaintenanceHistoryDialog.ColumnHeaders.Length != 8 || TruckMaintenanceHistoryDialog.ColumnHeaders[0] != "Plate No." || TruckMaintenanceHistoryDialog.ColumnHeaders[7] != "Est. fix date") bad.Add("history columns");
        return string.Join("; ", bad);
    }

    [Check]
    public static string TruckMaintenanceStatus_Decommission_WritesARow_ActiveClearsTheDates()
    {
        Fresh();
        int id = TmTruck("MNT-003");
        var bad = new List<string>();
        MJdatabase.ApplyTruckStatusChange(id, "Under Maintenance", "Gearbox", DateTime.Today.AddDays(-1), DateTime.Today.AddDays(6));
        MJdatabase.ApplyTruckStatusChange(id, "Decommissioned", "Beyond repair");
        if (Long("SELECT COUNT(*) FROM TruckMaintenance WHERE TruckId=@i AND EntryType='Decommissioned' AND Task='Beyond repair'", ("@i", id)) != 1) bad.Add("decommission wrote no row");
        if (Str("SELECT COALESCE(BreakdownDate,'') FROM Trucks WHERE Id=@i", ("@i", id)) != "") bad.Add("decommission left the breakdown date");
        var dec = MJdatabase.GetTruckMaintenance(id).First(m => m.EntryType == "Decommissioned");
        if (dec.ServiceDate.Date != DateTime.Today || dec.PlateNumber != "MNT-003") bad.Add("decommission row date or snapshot");
        MJdatabase.ApplyTruckStatusChange(id, "Under Maintenance", "Second breakdown", DateTime.Today, DateTime.Today.AddDays(2));
        MJdatabase.ApplyTruckStatusChange(id, "Active", "Repaired and tested");
        if (Str("SELECT Status FROM Trucks WHERE Id=@i", ("@i", id)) != "Active") bad.Add("not Active");
        if (Str("SELECT COALESCE(BreakdownDate,'') FROM Trucks WHERE Id=@i", ("@i", id)) != "" || Str("SELECT COALESCE(EstimatedFixDate,'') FROM Trucks WHERE Id=@i", ("@i", id)) != "") bad.Add("Active did not clear the two dates");
        if (Long("SELECT COUNT(*) FROM TruckMaintenance WHERE TruckId=@i AND EntryType='Back in service' AND Task='Repaired and tested'", ("@i", id)) != 1) bad.Add("Active wrote no 'Back in service' row");
        if (MJdatabase.GetTrucks().First(t => t.Id == id).StatusDisplay != "Active") bad.Add("an Active truck shows more than its status");
        if (Long("SELECT COUNT(*) FROM TruckMaintenance WHERE TruckId=@i", ("@i", id)) != 4) bad.Add("expected 4 history rows (breakdown, decommission, breakdown, back in service)");
        if (TmRefused(() => MJdatabase.ApplyTruckStatusChange(id, "Under Maintenance", "again", DateTime.Today, DateTime.Today.AddDays(1)), out _)) bad.Add("a valid change was refused");
        return string.Join("; ", bad);
    }
}
