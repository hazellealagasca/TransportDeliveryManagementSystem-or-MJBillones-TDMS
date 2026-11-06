using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;

namespace TDMS.Verify;

/// <summary>
/// Read-only report of old records that conflict with the current rules, on a copy of the real data after the
/// workflow update has run. It never fails: it prints what it finds so the owner can decide what to correct.
/// </summary>
public static partial class Checks
{
    /// <summary>Each finding: what it is, the SQL listing the records (first column = a readable label).</summary>
    public static readonly (string Key, string What, string Sql)[] AuditFindings =
    {
        ("rate-dupes", "Rate rules that are duplicates of each other (same route, destination, truck type and season)",
         @"SELECT GROUP_CONCAT(Id) || ': ' || RouteName || ' → ' || Destination || ' · ' || TruckType || (CASE WHEN IsPeakSeason=1 THEN ' · peak' ELSE '' END)
           FROM RateMatrix GROUP BY LOWER(TRIM(RouteName)), LOWER(TRIM(Destination)), LOWER(TRIM(TruckType)), IsPeakSeason HAVING COUNT(*) > 1"),
        ("stale-dispatch", "Dispatches still open (Pending / Scheduled / In Transit…) although their date has passed; they keep the truck and crew marked busy",
         $@"SELECT DispatchNumber || ' · ' || Status || ' · ' || DATE(DispatchDate) FROM Deliveries
           WHERE Status IN {MJdatabase.ActiveDeliveryStatusesSql} AND DATE(DispatchDate) < DATE('now','localtime') ORDER BY DispatchDate"),
        ("stale-special", "Special trips still open although their date has passed",
         $@"SELECT 'Special trip #' || Id || ' · ' || Status || ' · ' || DATE(DispatchDate) FROM SpecialTrips
           WHERE Status IN {MJdatabase.ActiveSpecialTripStatusesSql} AND DATE(DispatchDate) < DATE('now','localtime') ORDER BY DispatchDate"),
        ("mixed-soa", "Old statements that mix normal and special trips and are not yet paid",
         "SELECT SoaNumber || ' · ' || Status FROM StatementsOfAccount WHERE SoaType = 'Mixed' AND COALESCE(Status,'') NOT IN ('Paid','Cancelled')"),
        ("two-invoices", "Trips with more than one service invoice that is not cancelled (one invoice per trip)",
         @"SELECT 'Dispatch ' || COALESCE(d.DispatchNumber, '#' || i.DeliveryId) || ': ' || GROUP_CONCAT(i.InvoiceNumber, ', ')
           FROM Invoices i LEFT JOIN Deliveries d ON d.Id = i.DeliveryId
           WHERE COALESCE(i.DeliveryId,0) > 0 AND COALESCE(i.Status,'') <> 'Cancelled' GROUP BY i.DeliveryId HAVING COUNT(*) > 1"),
        ("invoice-no-trip", "Service invoices (not cancelled) whose dispatch or special trip no longer exists",
         @"SELECT i.InvoiceNumber || ' · ' || COALESCE(i.Status,'') FROM Invoices i
           WHERE COALESCE(i.Status,'') <> 'Cancelled' AND (
                 (COALESCE(i.DeliveryId,0) > 0 AND NOT EXISTS (SELECT 1 FROM Deliveries d WHERE d.Id = i.DeliveryId))
              OR (COALESCE(i.SpecialTripId,0) > 0 AND NOT EXISTS (SELECT 1 FROM SpecialTrips s WHERE s.Id = i.SpecialTripId)))"),
        ("invoice-dead-soa", "Service invoices still linked to a statement that is cancelled or missing",
         @"SELECT i.InvoiceNumber || ' → statement #' || i.SoaId FROM Invoices i
           WHERE COALESCE(i.SoaId,0) > 0 AND COALESCE(i.Status,'') <> 'Cancelled'
             AND NOT EXISTS (SELECT 1 FROM StatementsOfAccount s WHERE s.Id = i.SoaId AND COALESCE(s.Status,'') <> 'Cancelled')"),
        ("receipt-dead-invoice", "Collection receipts (not voided) for an invoice that is cancelled or missing",
         @"SELECT r.ReceiptNumber || ' · PHP ' || printf('%.2f', r.AmountPaid) || ' · invoice ' || COALESCE(i.InvoiceNumber, '#' || r.InvoiceId) || ' ' || COALESCE(i.Status,'(missing)')
           FROM CollectionReceipts r LEFT JOIN Invoices i ON i.Id = r.InvoiceId
           WHERE COALESCE(r.IsVoid,0) = 0 AND (i.Id IS NULL OR i.Status = 'Cancelled')"),
        ("paid-mismatch", "Invoices whose status does not match their receipts (Paid without a full receipt, or fully received but not Paid)",
         @"SELECT i.InvoiceNumber || ' · ' || i.Status || ' · net ' || printf('%.2f', i.NetAmount) || ' · received ' || printf('%.2f', COALESCE(x.Paid,0))
           FROM Invoices i LEFT JOIN (SELECT InvoiceId, SUM(AmountPaid) Paid FROM CollectionReceipts WHERE COALESCE(IsVoid,0)=0 GROUP BY InvoiceId) x ON x.InvoiceId = i.Id
           WHERE COALESCE(i.Status,'') NOT IN ('Cancelled','Draft') AND (
                 (i.Status = 'Paid' AND ROUND(COALESCE(x.Paid,0),2) < ROUND(i.NetAmount,2))
              OR (i.Status <> 'Paid' AND i.NetAmount > 0 AND ROUND(COALESCE(x.Paid,0),2) >= ROUND(i.NetAmount,2)))"),
        ("report-no-trip", "Trip reports (not voided) whose dispatch no longer exists",
         @"SELECT t.TripReportNumber FROM TripReports t WHERE COALESCE(t.IsVoid,0) = 0 AND COALESCE(t.DeliveryId,0) > 0
             AND NOT EXISTS (SELECT 1 FROM Deliveries d WHERE d.Id = t.DeliveryId)"),
        ("report-two", "Dispatches with more than one trip report that is not voided",
         @"SELECT 'Dispatch ' || COALESCE(d.DispatchNumber, '#' || t.DeliveryId) || ': ' || GROUP_CONCAT(t.TripReportNumber, ', ')
           FROM TripReports t LEFT JOIN Deliveries d ON d.Id = t.DeliveryId
           WHERE COALESCE(t.IsVoid,0) = 0 AND COALESCE(t.DeliveryId,0) > 0 GROUP BY t.DeliveryId HAVING COUNT(*) > 1"),
        ("report-status-drift", "Trip reports whose status disagrees with their invoice (Invoiced without an invoice, or invoiced but not marked)",
         @"SELECT t.TripReportNumber || ' · ' || COALESCE(t.Status,'') FROM TripReports t
           WHERE COALESCE(t.IsVoid,0) = 0 AND COALESCE(t.DeliveryId,0) > 0 AND (
                 (t.Status = 'Invoiced' AND NOT EXISTS (SELECT 1 FROM Invoices i WHERE i.DeliveryId = t.DeliveryId AND COALESCE(i.Status,'') <> 'Cancelled'))
              OR (t.Status = 'Completed' AND EXISTS (SELECT 1 FROM Invoices i WHERE i.DeliveryId = t.DeliveryId AND COALESCE(i.Status,'') <> 'Cancelled')))"),
        ("soa-line-dead", "Statement lines (on statements not cancelled) pointing to a dispatch that no longer exists",
         @"SELECT s.SoaNumber || ' line ' || x.Id || ' (dispatch #' || x.DeliveryId || ')' FROM StatementOfAccountItems x JOIN StatementsOfAccount s ON s.Id = x.SoaId
           WHERE COALESCE(s.Status,'') <> 'Cancelled' AND COALESCE(x.DeliveryId,0) > 0 AND NOT EXISTS (SELECT 1 FROM Deliveries d WHERE d.Id = x.DeliveryId)"),
        ("soa-status-drift", "Statements whose status disagrees with their invoices",
         @"SELECT s.SoaNumber || ' · ' || s.Status FROM StatementsOfAccount s
           WHERE COALESCE(s.Status,'') <> 'Cancelled' AND EXISTS (SELECT 1 FROM Invoices i WHERE i.SoaId = s.Id AND COALESCE(i.Status,'') <> 'Cancelled')
             AND ((s.Status = 'Paid' AND EXISTS (SELECT 1 FROM Invoices i WHERE i.SoaId = s.Id AND COALESCE(i.Status,'') NOT IN ('Paid','Cancelled')))
               OR (s.Status <> 'Paid' AND NOT EXISTS (SELECT 1 FROM Invoices i WHERE i.SoaId = s.Id AND COALESCE(i.Status,'') NOT IN ('Paid','Cancelled'))))"),
    };

    /// <summary>Runs every audit query on the database in use. Returns (key, what, rows) for the findings that have rows.</summary>
    public static List<(string Key, string What, List<string> Rows)> RunAudit()
    {
        var found = new List<(string, string, List<string>)>();
        using var c = Open();
        foreach (var (key, what, sql) in AuditFindings)
        {
            var rows = new List<string>();
            try
            {
                using var cmd = c.CreateCommand(); cmd.CommandText = sql;
                using var r = cmd.ExecuteReader();
                while (r.Read()) rows.Add(Convert.ToString(r.GetValue(0)) ?? "");
            }
            catch (SqliteException ex) { rows.Add("(could not check: " + ex.Message + ")"); }
            if (rows.Count > 0) found.Add((key, what, rows));
        }
        return found;
    }

    /// <summary>Report only: lists old records that conflict with the rules (always passes).</summary>
    [Check]
    public static string Audit_RealData()
    {
        if (FromCopy() == null) return "";
        MJdatabase.InitializeDatabase();
        MJdatabase.RunOneTimeMigrations();   // what the login screen runs
        var found = RunAudit();
        Console.WriteLine(found.Count == 0 ? "     AUDIT: nothing conflicts" : $"     AUDIT: {found.Count} kind(s) of conflicting records");
        foreach (var (key, what, rows) in found)
        {
            Console.WriteLine($"     AUDIT [{key}] {what}: {rows.Count}");
            foreach (var row in rows.Take(40)) Console.WriteLine("       - " + row);
        }
        return "";
    }
}
