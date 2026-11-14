using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Truck status changes: every change needs a description; Under Maintenance also needs the breakdown date and the estimated fix date.
    /// Each change writes one maintenance-history row that keeps a snapshot of the truck (plate, category, crate type, capacity).
    /// </summary>
    public static partial class MJdatabase
    {
        public const string TruckEntryService = "Service";
        public const string TruckEntryBreakdown = "Breakdown";
        public const string TruckEntryDecommissioned = "Decommissioned";
        public const string TruckEntryBackInService = "Back in service";
        public const int TruckStatusDescriptionMax = 500;

        private static bool IsUnderMaintenanceStatus(string? status) => string.Equals((status ?? "").Trim(), "Under Maintenance", StringComparison.OrdinalIgnoreCase);

        /// <summary>Null when the status change can be saved, otherwise the problem in plain words.</summary>
        public static string? CheckTruckStatusChange(string? newStatus, string? description, DateTime? breakdownDate, DateTime? estimatedFixDate)
        {
            string d = InputRules.Tidy(description);
            if (d.Length == 0) return "Enter a description.";
            if (d.Length > TruckStatusDescriptionMax) return $"The description can be at most {TruckStatusDescriptionMax} characters.";
            if (!IsUnderMaintenanceStatus(newStatus)) return null;
            if (!breakdownDate.HasValue) return "Enter the breakdown date.";
            if (breakdownDate.Value.Date > DateTime.Today) return "The breakdown date can't be in the future.";
            if (!estimatedFixDate.HasValue) return "Enter the estimated fix date.";
            if (estimatedFixDate.Value.Date < breakdownDate.Value.Date) return "The estimated fix date can't be before the breakdown date.";
            return null;
        }

        /// <summary>
        /// Moves a truck to a new status: refuses a missing description (and, for Under Maintenance, missing or impossible dates), saves the status and the two dates
        /// on the truck, writes one maintenance-history row with the truck snapshot, and logs the status change. Active clears the dates.
        /// </summary>
        public static void ApplyTruckStatusChange(int truckId, string newStatus, string description, DateTime? breakdownDate = null, DateTime? estimatedFixDate = null)
        {
            string? problem = CheckTruckStatusChange(newStatus, description, breakdownDate, estimatedFixDate);
            if (problem != null) throw new InvalidOperationException(problem);
            EnsureProfileTables();
            EnsureCapacitySchema();
            string desc = InputRules.Tidy(description);
            bool toMaint = IsUnderMaintenanceStatus(newStatus);
            bool toDecom = IsDecommissioned(newStatus);
            string oldStatus;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var tx = conn.BeginTransaction();
                var m = new TruckMaintenance { TruckId = truckId };
                using (var rd = conn.CreateCommand())
                {
                    rd.Transaction = tx;
                    rd.CommandText = "SELECT COALESCE(Status,'Active') FROM Trucks WHERE Id = @id";
                    rd.Parameters.AddWithValue("@id", truckId);
                    object? o = rd.ExecuteScalar();
                    if (o == null) throw new InvalidOperationException("That truck no longer exists.");
                    oldStatus = Convert.ToString(o) ?? "";
                }
                FillTruckSnapshot(conn, m, tx);
                using (var up = conn.CreateCommand())
                {
                    up.Transaction = tx;
                    up.CommandText = "UPDATE Trucks SET Status = @s, BreakdownDate = @b, EstimatedFixDate = @f WHERE Id = @id";
                    up.Parameters.AddWithValue("@s", newStatus);
                    up.Parameters.AddWithValue("@b", toMaint ? breakdownDate!.Value.ToString("yyyy-MM-dd") : "");
                    up.Parameters.AddWithValue("@f", toMaint ? estimatedFixDate!.Value.ToString("yyyy-MM-dd") : "");
                    up.Parameters.AddWithValue("@id", truckId);
                    up.ExecuteNonQuery();
                }
                bool wasBlocked = IsUnderMaintenanceStatus(oldStatus) || IsDecommissioned(oldStatus);
                string? entryType = toMaint ? TruckEntryBreakdown : toDecom ? TruckEntryDecommissioned : wasBlocked ? TruckEntryBackInService : null;
                if (entryType != null)
                {
                    using var ins = conn.CreateCommand();
                    ins.Transaction = tx;
                    ins.CommandText = @"INSERT INTO TruckMaintenance (TruckId, ServiceDate, Task, Category, OdometerKm, Cost, ServiceProvider, Notes, NextDueDate, CreatedBy, PlateNumber, TruckCategory, CrateTempType, Capacity, EntryType)
                                        VALUES (@t, @d, @task, @cat, 0, 0, '', '', @next, @by, @pl, @tc, @ct, @cap, @et)";
                    ins.Parameters.AddWithValue("@t", truckId);
                    ins.Parameters.AddWithValue("@d", (toMaint ? breakdownDate!.Value.Date : DateTime.Today).ToString("yyyy-MM-dd"));
                    ins.Parameters.AddWithValue("@task", desc);
                    ins.Parameters.AddWithValue("@cat", toMaint ? "Repair" : "Status");
                    ins.Parameters.AddWithValue("@next", toMaint ? estimatedFixDate!.Value.ToString("yyyy-MM-dd") : "");
                    ins.Parameters.AddWithValue("@by", Who);
                    ins.Parameters.AddWithValue("@pl", m.PlateNumber);
                    ins.Parameters.AddWithValue("@tc", m.TruckCategory);
                    ins.Parameters.AddWithValue("@ct", m.CrateTempType);
                    ins.Parameters.AddWithValue("@cap", m.Capacity);
                    ins.Parameters.AddWithValue("@et", entryType);
                    ins.ExecuteNonQuery();
                }
                tx.Commit();
            }
            if (!string.Equals(oldStatus.Trim(), newStatus.Trim(), StringComparison.OrdinalIgnoreCase)) LogTruckStatusChange(truckId, oldStatus, newStatus, desc);
        }

        /// <summary>Copies the truck's plate, category, crate type and capacity onto a maintenance record, as they are right now.</summary>
        private static void FillTruckSnapshot(SqliteConnection conn, TruckMaintenance m, SqliteTransaction? tx = null)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT COALESCE(PlateNumber,''), COALESCE(TruckType,''), COALESCE(CargoType,''), COALESCE(CapacityCrates,0) FROM Trucks WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", m.TruckId);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return;
            m.PlateNumber = r.GetString(0); m.TruckCategory = r.GetString(1); m.CrateTempType = r.GetString(2); m.Capacity = Convert.ToInt32(r.GetValue(3));
        }

        /// <summary>The maintenance history of every truck, newest first (for the trucks list).</summary>
        public static List<TruckMaintenance> GetAllTruckMaintenance()
        {
            EnsureProfileTables();
            var list = new List<TruckMaintenance>();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT m.Id, m.TruckId, m.ServiceDate, m.Task, m.Category, m.NextDueDate,
                                       COALESCE(NULLIF(m.PlateNumber,''), t.PlateNumber, ''), COALESCE(NULLIF(m.TruckCategory,''), t.TruckType, ''),
                                       COALESCE(NULLIF(m.CrateTempType,''), t.CargoType, ''), CASE WHEN COALESCE(m.Capacity,0) > 0 THEN m.Capacity ELSE COALESCE(t.CapacityCrates,0) END, COALESCE(m.EntryType,'Service')
                                FROM TruckMaintenance m LEFT JOIN Trucks t ON t.Id = m.TruckId
                                WHERE COALESCE(m.IsVoid,0) = 0 ORDER BY m.ServiceDate DESC, m.Id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new TruckMaintenance
                {
                    Id = r.GetInt32(0), TruckId = r.GetInt32(1), ServiceDate = ParseDate(r.GetValue(2)) ?? DateTime.Today,
                    Task = r.IsDBNull(3) ? "" : r.GetString(3), Category = r.IsDBNull(4) ? "" : r.GetString(4), NextDueDate = ParseDate(r.GetValue(5)),
                    PlateNumber = r.GetString(6), TruckCategory = r.GetString(7), CrateTempType = r.GetString(8), Capacity = Convert.ToInt32(r.GetValue(9)), EntryType = r.GetString(10)
                });
            }
            return list;
        }
    }
}
