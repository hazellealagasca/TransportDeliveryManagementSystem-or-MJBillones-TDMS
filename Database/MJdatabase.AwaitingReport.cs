using System;
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
                             WHERE t.DeliveryId = d.Id AND COALESCE(t.IsVoid,0) = 0 ORDER BY t.Id DESC LIMIT 1), '')
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
    }
}
