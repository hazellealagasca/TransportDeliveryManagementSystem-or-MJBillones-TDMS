using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Nov 10: the type of delivery (REF or DRY) is chosen on each trip report; the Statement of Account takes it from there.
    /// Kept in its own column and its own small queries so the trip report save/load SQL stays untouched.
    /// </summary>
    public static partial class MJdatabase
    {
        public static readonly string[] TripTypes = { "REF", "DRY" };

        /// <summary>Adds the DeliveryType column to trip reports (older databases don't have it). Cheap; checked each time.</summary>
        public static void EnsureTripTypeColumn()
        {
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                bool have = false;
                using (var chk = conn.CreateCommand())
                {
                    chk.CommandText = "PRAGMA table_info(TripReports)";
                    using var r = chk.ExecuteReader();
                    while (r.Read()) if (string.Equals(r.GetString(1), "DeliveryType", StringComparison.OrdinalIgnoreCase)) have = true;
                }
                if (have) return;
                using var alt = conn.CreateCommand();
                alt.CommandText = "ALTER TABLE TripReports ADD COLUMN DeliveryType TEXT DEFAULT ''";
                alt.ExecuteNonQuery();
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        /// <summary>Saves the type (REF / DRY) on the newest saved trip report of a dispatch or special trip.</summary>
        public static void SetTripReportDeliveryType(int deliveryId, int specialTripId, string type)
        {
            if (deliveryId <= 0 && specialTripId <= 0) return;
            type = (type ?? "").Trim().ToUpperInvariant();
            if (type != "REF" && type != "DRY") return;
            try
            {
                EnsureTripTypeColumn(); EnsureWorkflowColumns(); EnsureSpecialTripColumns();
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = deliveryId > 0
                    ? "UPDATE TripReports SET DeliveryType = @t WHERE Id = (SELECT MAX(Id) FROM TripReports WHERE DeliveryId = @d AND COALESCE(IsVoid,0) = 0)"
                    : "UPDATE TripReports SET DeliveryType = @t WHERE Id = (SELECT MAX(Id) FROM TripReports WHERE SpecialTripId = @d AND COALESCE(IsVoid,0) = 0)";
                cmd.Parameters.AddWithValue("@t", type);
                cmd.Parameters.AddWithValue("@d", deliveryId > 0 ? deliveryId : specialTripId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        /// <summary>The type saved on one trip report ("" when none was chosen).</summary>
        public static string GetTripReportDeliveryType(int tripReportId)
        {
            if (tripReportId <= 0) return "";
            try
            {
                EnsureTripTypeColumn();
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COALESCE(DeliveryType, '') FROM TripReports WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", tripReportId);
                return (cmd.ExecuteScalar()?.ToString() ?? "").Trim();
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); return ""; }
        }

        /// <summary>The type of each dispatch's finished trip report, by dispatch id (special trips use −special trip id). Newest report wins; blank types are left out.</summary>
        public static Dictionary<int, string> GetTripReportDeliveryTypes()
        {
            var map = new Dictionary<int, string>();
            try
            {
                EnsureTripTypeColumn(); EnsureWorkflowColumns(); EnsureSpecialTripColumns();   // older databases may lack these columns
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT COALESCE(DeliveryId, 0), COALESCE(SpecialTripId, 0), COALESCE(DeliveryType, '') FROM TripReports
                                    WHERE COALESCE(IsVoid, 0) = 0 AND COALESCE(Status, 'Completed') <> 'Draft' ORDER BY Id";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    int key = r.GetInt32(0) > 0 ? r.GetInt32(0) : -r.GetInt32(1);
                    string t = r.GetString(2).Trim();
                    if (key == 0) continue;
                    if (t.Length > 0) map[key] = t; else map.Remove(key);
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return map;
        }
    }
}
