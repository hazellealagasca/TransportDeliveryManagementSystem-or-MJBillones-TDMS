using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>A completed dispatch that still needs its trip report (Trip Reports opens on this list).</summary>
    public sealed class AwaitingReportRow
    {
        public int DeliveryId { get; set; }
        public string DispatchNumber { get; set; } = "";
        public DateTime DispatchDate { get; set; }
        public string ClientName { get; set; } = "";
        public string Destination { get; set; } = "";
        public string TruckInfo { get; set; } = "";
        public string DriverName { get; set; } = "";
        public string ReportState { get; set; } = "";   // "No trip report yet" or "Draft saved"
        public string ReportNumber { get; set; } = ""; // empty until the trip report is created
        public int TotalCrates { get; set; }
        /// <summary>Whole days since the dispatch date (0 when the date is unknown).</summary>
        public int WaitingDays => DispatchDate <= DateTime.MinValue ? 0 : Math.Max(0, (int)(DateTime.Today - DispatchDate.Date).TotalDays);
        public string Waiting => WaitingDays == 0 ? "today" : WaitingDays == 1 ? "1 day" : $"{WaitingDays} days";
    }


    /// <summary>A trip report saved as "Draft" with nothing typed in it (left by the old "Create trip report" button).</summary>
    public sealed class EmptyDraftReport
    {
        public int Id { get; set; }
        public int DeliveryId { get; set; }
        public string TripReportNumber { get; set; } = "";
        public string DispatchNumber { get; set; } = "";
    }

    public static partial class MJdatabase
    {
        /// <summary>
        /// Dispatches whose status is Completed and that have no finished trip report: either no report at all, or only
        /// a Draft. A report that is Completed or Invoiced (Invoiced = completed and billed) takes the dispatch off the list.
        /// Voided reports are ignored.
        /// </summary>
        public const string AwaitingReportSql = @"
            SELECT d.Id, d.DispatchNumber, d.DispatchDate, COALESCE(d.ClientName,''), COALESCE(d.Destination,''),
                   COALESCE(d.TruckInfo,''), COALESCE(d.DriverName,''),
                   CASE WHEN EXISTS (SELECT 1 FROM TripReports t
                                     WHERE t.DeliveryId = d.Id AND COALESCE(t.IsVoid,0) = 0)
                        THEN 'Draft saved' ELSE 'No trip report yet' END,
                   COALESCE((SELECT t.TripReportNumber FROM TripReports t
                             WHERE t.DeliveryId = d.Id AND COALESCE(t.IsVoid,0) = 0 ORDER BY t.Id DESC LIMIT 1), ''),
                   COALESCE(d.TotalCrates,0)
            FROM Deliveries d
            WHERE d.Status = 'Completed' COLLATE NOCASE
              AND NOT EXISTS (SELECT 1 FROM TripReports t
                              WHERE t.DeliveryId = d.Id
                                AND COALESCE(t.IsVoid,0) = 0
                                AND COALESCE(t.Status,'Completed') IN ('Completed','Invoiced'))
            ORDER BY d.DispatchDate DESC, d.Id DESC";

        public static List<AwaitingReportRow> GetDispatchesAwaitingReport()
        {
            EnsureWorkflowColumns();
            var list = new List<AwaitingReportRow>();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = AwaitingReportSql;
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new AwaitingReportRow
                {
                    DeliveryId = r.GetInt32(0),
                    DispatchNumber = r.GetString(1),
                    DispatchDate = DateTime.TryParse(Convert.ToString(r.GetValue(2)), out var dt) ? dt : DateTime.MinValue,
                    ClientName = r.GetString(3),
                    Destination = r.GetString(4),
                    TruckInfo = r.GetString(5),
                    DriverName = r.GetString(6),
                    ReportState = r.GetString(7),
                    ReportNumber = r.GetString(8),
                    TotalCrates = r.IsDBNull(9) ? 0 : Convert.ToInt32(r.GetValue(9)),
                });
            }
            return list;
        }

        /// <summary>
        /// "Create trip report" for a completed dispatch: saves an empty Draft report, which gives it its trip report number.
        /// If the dispatch already has a report (not void) nothing is created and that report's number is returned.
        /// </summary>
        public static string CreateTripReportForDispatch(int deliveryId)
        {
            string dispatchNumber;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT COALESCE((SELECT t.TripReportNumber FROM TripReports t WHERE t.DeliveryId = @d AND COALESCE(t.IsVoid,0) = 0 ORDER BY t.Id DESC LIMIT 1), ''),
                                           COALESCE((SELECT DispatchNumber FROM Deliveries WHERE Id = @d), '')";
                cmd.Parameters.AddWithValue("@d", deliveryId);
                using var r = cmd.ExecuteReader();
                if (!r.Read() || r.GetString(1).Length == 0) throw new InvalidOperationException("That dispatch no longer exists.");
                if (r.GetString(0).Length > 0) return r.GetString(0);
                dispatchNumber = r.GetString(1);
            }
            var tr = new Models.TripReport { DeliveryId = deliveryId, DispatchNumber = dispatchNumber, Status = Models.TripReportStatus.Draft, ReportDate = DateTime.Now };
            SaveTripReport(tr); // refuses a dispatch that is not Completed
            return tr.TripReportNumber;
        }

        /// <summary>
        /// Draft trip reports that hold nothing the person typed: no times, no store manager, no remarks, no crate counts, and no typed
        /// store details on the dispatch's stops. (The pre-filled signatory names and the dispatch's own DR numbers do not count.)
        /// </summary>
        public const string EmptyDraftReportSql = @"
            SELECT t.Id, t.DeliveryId, COALESCE(t.TripReportNumber,''), COALESCE(t.DispatchNumber,'')
            FROM TripReports t
            WHERE COALESCE(t.IsVoid,0) = 0 AND COALESCE(t.Status,'Completed') = 'Draft' COLLATE NOCASE
              AND COALESCE(t.ArrivalTime,'') = '' AND COALESCE(t.DepartureTime,'') = ''
              AND COALESCE(t.TruckTimeIn,'') IN ('','2042') AND COALESCE(t.TruckTimeOut,'') IN ('','0139')   -- the plant gate times come pre-filled (2042 / 0139)
              AND COALESCE(t.StoreManagerName,'') = '' AND COALESCE(t.StoreManagerLastName,'') = '' AND COALESCE(t.StoreManagerFirstName,'') = ''
              AND COALESCE(t.Remarks,'') = '' AND COALESCE(t.ReturnCheckedBy,'') = '' AND COALESCE(t.GuardName,'') = ''
              AND COALESCE(t.DeliveredCrates,0) = 0 AND COALESCE(t.ReturnedCrates,0) = 0
              AND COALESCE(t.DeliveredGCrates,0) = 0 AND COALESCE(t.DeliveredECrates,0) = 0
              AND COALESCE(t.ReturnedGCrates,0) = 0 AND COALESCE(t.ReturnedECrates,0) = 0
              AND NOT EXISTS (SELECT 1 FROM DispatchStops s WHERE s.DeliveryId = t.DeliveryId
                              AND (COALESCE(s.ArrivalTime,'') <> '' OR COALESCE(s.DepartureTime,'') <> ''
                                   OR COALESCE(s.DeliveredCrates,0) > 0 OR COALESCE(s.ReturnedCrates,0) > 0
                                   OR COALESCE(s.StoreManagerName,'') <> ''))
            ORDER BY t.Id";

        public static List<EmptyDraftReport> GetEmptyDraftReports()
        {
            EnsureWorkflowColumns();
            var list = new List<EmptyDraftReport>();
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = EmptyDraftReportSql;
                using var r = cmd.ExecuteReader();
                while (r.Read()) list.Add(new EmptyDraftReport { Id = r.GetInt32(0), DeliveryId = r.GetInt32(1), TripReportNumber = r.GetString(2), DispatchNumber = r.GetString(3) });
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return list;
        }

        /// <summary>
        /// Voids empty draft trip reports (only ones that are still empty and still Draft at this moment). With <paramref name="backup"/> a database
        /// backup is made first and the removal stops if it fails. Each one is written to the audit trail. Returns how many were voided.
        /// </summary>
        public static int VoidEmptyDraftReports(IEnumerable<int> ids, bool backup, string reason)
        {
            var wanted = new HashSet<int>(ids);
            if (wanted.Count == 0) return 0;
            if (backup)
            {
                string path = Path.Combine(Path.GetDirectoryName(dbFilePath) ?? ".", "Backups", $"before_empty_draft_cleanup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                if (!BackupDatabase(path)) throw new InvalidOperationException("Couldn't back up the database first: " + LastBackupError);
            }
            int removed = 0;
            foreach (var d in GetEmptyDraftReports().Where(d => wanted.Contains(d.Id)))   // re-checked now: only still-empty drafts
            {
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    // Trip reports are never deleted (the database refuses it); a voided report is kept for history and ignored by every list.
                    cmd.CommandText = "UPDATE TripReports SET IsVoid = 1, VoidedAt = @now, VoidedBy = @user WHERE Id = @id AND COALESCE(Status,'') = 'Draft' COLLATE NOCASE AND COALESCE(IsVoid,0) = 0";
                    cmd.Parameters.AddWithValue("@id", d.Id);
                    cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@user", Functions.SessionContext.CurrentUsername ?? "");
                    if (cmd.ExecuteNonQuery() == 0) continue;
                }
                removed++;
                try { LogAuditTrail("VOID", "TripReports", d.Id, d.TripReportNumber, "Status", "Draft (empty)", "voided", reason); }
                catch (Exception ex) { ErrLog.Swallowed(ex); }
            }
            return removed;
        }
    }
}
