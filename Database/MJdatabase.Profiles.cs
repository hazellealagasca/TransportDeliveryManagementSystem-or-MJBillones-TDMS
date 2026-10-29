using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>Truck and crew profile data: specs, maintenance log, crew violations / remarks, and type-ahead suggestions.</summary>
    public static partial class MJdatabase
    {
        private static bool profileTablesReady;
        private static void EnsureProfileTables()
        {
            if (profileTablesReady) return;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS TruckMaintenance (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TruckId INTEGER NOT NULL,
                    ServiceDate TEXT NOT NULL,
                    Task TEXT NOT NULL,
                    Category TEXT DEFAULT 'Preventive',
                    OdometerKm INTEGER DEFAULT 0,
                    Cost REAL DEFAULT 0,
                    ServiceProvider TEXT DEFAULT '',
                    Notes TEXT DEFAULT '',
                    NextDueDate TEXT DEFAULT '',
                    CreatedBy TEXT DEFAULT '',
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                    IsVoid INTEGER DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS idx_truck_maint_truck ON TruckMaintenance(TruckId, ServiceDate DESC);
                CREATE TABLE IF NOT EXISTS CrewRecords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CrewId INTEGER NOT NULL,
                    RecordDate TEXT NOT NULL,
                    Kind TEXT NOT NULL DEFAULT 'Violation',
                    Type TEXT NOT NULL DEFAULT '',
                    Description TEXT DEFAULT '',
                    DispatchNumber TEXT DEFAULT '',
                    Severity TEXT DEFAULT 'Minor',
                    CreatedBy TEXT DEFAULT '',
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                    IsVoid INTEGER DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS idx_crew_records_crew ON CrewRecords(CrewId, RecordDate DESC);
                CREATE TRIGGER IF NOT EXISTS trg_nodelete_TruckMaintenance BEFORE DELETE ON TruckMaintenance BEGIN SELECT RAISE(ABORT, 'Maintenance records are kept for history. Void them instead.'); END;
                CREATE TRIGGER IF NOT EXISTS trg_nodelete_CrewRecords BEFORE DELETE ON CrewRecords BEGIN SELECT RAISE(ABORT, 'Crew records are kept for history. Void them instead.'); END;";
            cmd.ExecuteNonQuery();
            profileTablesReady = true;
        }
        /// <summary>One-time: the license number is no longer kept. Existing numbers become a license class, then are erased.</summary>
        private static void MigrateLicenseNumbers(SqliteConnection conn)
        {
            try
            {
                using var check = conn.CreateCommand();
                check.CommandText = "SELECT SettingValue FROM SystemSettings WHERE SettingKey = 'Fix.LicenseNumbers.v1'";
                if ((check.ExecuteScalar()?.ToString() ?? "") == "done") return;
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Crews SET LicenseClass = 'Professional Driver''s License'
                    WHERE UPPER(Role) LIKE '%DRIVER%' AND COALESCE(LicenseClass, 'None') = 'None' AND TRIM(COALESCE(LicenseNumber, '')) <> '' AND UPPER(TRIM(LicenseNumber)) NOT IN ('N/A', 'NA', 'NONE');
                    UPDATE Crews SET LicenseNumber = '';
                    UPDATE Crews SET Role = 'Driver' WHERE UPPER(TRIM(Role)) = 'DRIVER' AND Role <> 'Driver';
                    UPDATE Crews SET Role = 'Delivery Aide / Helper' WHERE LOWER(TRIM(Role)) IN ('aide/helper', 'aide', 'helper', 'delivery aide', 'delivery helper', 'aide / helper');
                    INSERT OR REPLACE INTO SystemSettings (SettingKey, SettingValue, Description, UpdatedAt) VALUES ('Fix.LicenseNumbers.v1', 'done', 'License numbers replaced by license class', datetime('now'));";
                cmd.ExecuteNonQuery();
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] MigrateLicenseNumbers: {ex.Message}"); ErrLog.Swallowed(ex); }
        }
        private static DateTime? ParseDate(object? v) => v == null || v == DBNull.Value ? null : (DateTime.TryParse(v.ToString(), out var d) ? d : null);
        private static string Who => SessionContext.ActiveUser?.Username ?? SessionContext.CurrentUsername;
        // ------------------------------------------------------------------ truck specs
        public static TruckSpecs GetTruckSpecs(int truckId)
        {
            var s = new TruckSpecs { TruckId = truckId };
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COALESCE(Make,''), COALESCE(Model,''), COALESCE(YearModel,''), COALESCE(ChassisNumber,''), COALESCE(EngineNumber,''), COALESCE(OdometerKm,0), COALESCE(RegistrationExpiry,''), COALESCE(InsuranceExpiry,'') FROM Trucks WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", truckId);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    s.Make = r.GetString(0); s.Model = r.GetString(1); s.YearModel = r.GetString(2);
                    s.ChassisNumber = r.GetString(3); s.EngineNumber = r.GetString(4);
                    s.OdometerKm = Convert.ToInt32(r.GetValue(5));
                    s.RegistrationExpiry = ParseDate(r.GetValue(6)); s.InsuranceExpiry = ParseDate(r.GetValue(7));
                }
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] GetTruckSpecs: {ex.Message}"); ErrLog.Swallowed(ex); }
            return s;
        }
        public static void SaveTruckSpecs(TruckSpecs s)
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Trucks SET Make=@mk, Model=@md, YearModel=@yr, ChassisNumber=@ch, EngineNumber=@en, OdometerKm=@od, RegistrationExpiry=@re, InsuranceExpiry=@ie WHERE Id=@id";
            cmd.Parameters.AddWithValue("@mk", s.Make.Trim());
            cmd.Parameters.AddWithValue("@md", s.Model.Trim());
            cmd.Parameters.AddWithValue("@yr", s.YearModel.Trim());
            cmd.Parameters.AddWithValue("@ch", s.ChassisNumber.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@en", s.EngineNumber.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@od", s.OdometerKm);
            cmd.Parameters.AddWithValue("@re", s.RegistrationExpiry?.ToString("yyyy-MM-dd") ?? "");
            cmd.Parameters.AddWithValue("@ie", s.InsuranceExpiry?.ToString("yyyy-MM-dd") ?? "");
            cmd.Parameters.AddWithValue("@id", s.TruckId);
            cmd.ExecuteNonQuery();
            LogAuditTrail("UPDATE", "Trucks", s.TruckId, $"Truck #{s.TruckId}", "Specs", "", $"{s.Make} {s.Model} {s.YearModel}".Trim(), "Truck specifications updated");
        }
        /// <summary>True when another truck already has this chassis or engine number.</summary>
        public static string? FindTruckSpecClash(int truckId, string chassis, string engine)
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT PlateNumber, CASE WHEN TRIM(@ch) <> '' AND UPPER(TRIM(ChassisNumber)) = UPPER(TRIM(@ch)) THEN 'chassis' ELSE 'engine' END FROM Trucks
                                WHERE Id <> @id AND ((TRIM(@ch) <> '' AND UPPER(TRIM(ChassisNumber)) = UPPER(TRIM(@ch))) OR (TRIM(@en) <> '' AND UPPER(TRIM(EngineNumber)) = UPPER(TRIM(@en)))) LIMIT 1";
            cmd.Parameters.AddWithValue("@id", truckId);
            cmd.Parameters.AddWithValue("@ch", chassis ?? "");
            cmd.Parameters.AddWithValue("@en", engine ?? "");
            using var r = cmd.ExecuteReader();
            return r.Read() ? $"Truck {r.GetString(0)} already has this {r.GetString(1)} number." : null;
        }
        // ------------------------------------------------------------------ maintenance
        public static List<TruckMaintenance> GetTruckMaintenance(int truckId)
        {
            EnsureProfileTables();
            var list = new List<TruckMaintenance>();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, TruckId, ServiceDate, Task, Category, OdometerKm, Cost, ServiceProvider, Notes, NextDueDate, CreatedBy FROM TruckMaintenance WHERE TruckId = @t AND COALESCE(IsVoid,0) = 0 ORDER BY ServiceDate DESC, Id DESC";
            cmd.Parameters.AddWithValue("@t", truckId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new TruckMaintenance
                {
                    Id = r.GetInt32(0), TruckId = r.GetInt32(1), ServiceDate = ParseDate(r.GetValue(2)) ?? DateTime.Today,
                    Task = r.IsDBNull(3) ? "" : r.GetString(3), Category = r.IsDBNull(4) ? "" : r.GetString(4),
                    OdometerKm = r.IsDBNull(5) ? 0 : Convert.ToInt32(r.GetValue(5)), Cost = r.IsDBNull(6) ? 0 : Convert.ToDecimal(r.GetDouble(6)),
                    ServiceProvider = r.IsDBNull(7) ? "" : r.GetString(7), Notes = r.IsDBNull(8) ? "" : r.GetString(8),
                    NextDueDate = ParseDate(r.GetValue(9)), CreatedBy = r.IsDBNull(10) ? "" : r.GetString(10)
                });
            }
            return list;
        }
        public static void SaveTruckMaintenance(TruckMaintenance m)
        {
            EnsureProfileTables();
            bool isNew = m.Id <= 0;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = isNew
                ? "INSERT INTO TruckMaintenance (TruckId, ServiceDate, Task, Category, OdometerKm, Cost, ServiceProvider, Notes, NextDueDate, CreatedBy) VALUES (@t, @d, @task, @cat, @od, @cost, @sp, @n, @next, @by)"
                : "UPDATE TruckMaintenance SET ServiceDate=@d, Task=@task, Category=@cat, OdometerKm=@od, Cost=@cost, ServiceProvider=@sp, Notes=@n, NextDueDate=@next WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", m.Id);
            cmd.Parameters.AddWithValue("@t", m.TruckId);
            cmd.Parameters.AddWithValue("@d", m.ServiceDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@task", m.Task.Trim());
            cmd.Parameters.AddWithValue("@cat", m.Category.Trim());
            cmd.Parameters.AddWithValue("@od", m.OdometerKm);
            cmd.Parameters.AddWithValue("@cost", (double)m.Cost);
            cmd.Parameters.AddWithValue("@sp", m.ServiceProvider.Trim());
            cmd.Parameters.AddWithValue("@n", m.Notes.Trim());
            cmd.Parameters.AddWithValue("@next", m.NextDueDate?.ToString("yyyy-MM-dd") ?? "");
            cmd.Parameters.AddWithValue("@by", Who);
            cmd.ExecuteNonQuery();
            if (isNew) { using var idc = conn.CreateCommand(); idc.CommandText = "SELECT last_insert_rowid()"; m.Id = Convert.ToInt32(idc.ExecuteScalar()); }
            // Keep the truck's odometer at the highest reading recorded.
            if (m.OdometerKm > 0) { using var od = conn.CreateCommand(); od.CommandText = "UPDATE Trucks SET OdometerKm = @od WHERE Id = @t AND COALESCE(OdometerKm,0) < @od"; od.Parameters.AddWithValue("@od", m.OdometerKm); od.Parameters.AddWithValue("@t", m.TruckId); od.ExecuteNonQuery(); }
            LogAuditTrail(isNew ? "ADD" : "UPDATE", "TruckMaintenance", m.Id, $"Truck #{m.TruckId}", "Task", "", m.Task, $"Maintenance {(isNew ? "logged" : "updated")}: {m.Task} on {m.ServiceDate:yyyy-MM-dd}");
        }
        public static void VoidTruckMaintenance(int id)
        {
            EnsureProfileTables();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE TruckMaintenance SET IsVoid = 1 WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            LogAuditTrail("VOID", "TruckMaintenance", id, $"Maintenance #{id}", "", "", "", "Maintenance entry voided (kept for history)");
        }
        // ------------------------------------------------------------------ crew records
        public static List<CrewRecord> GetCrewRecords(int crewId)
        {
            EnsureProfileTables();
            var list = new List<CrewRecord>();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, CrewId, RecordDate, Kind, Type, Description, DispatchNumber, Severity, CreatedBy FROM CrewRecords WHERE CrewId = @c AND COALESCE(IsVoid,0) = 0 ORDER BY RecordDate DESC, Id DESC";
            cmd.Parameters.AddWithValue("@c", crewId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new CrewRecord
                {
                    Id = r.GetInt32(0), CrewId = r.GetInt32(1), RecordDate = ParseDate(r.GetValue(2)) ?? DateTime.Today,
                    Kind = r.IsDBNull(3) ? "" : r.GetString(3), Type = r.IsDBNull(4) ? "" : r.GetString(4), Description = r.IsDBNull(5) ? "" : r.GetString(5),
                    DispatchNumber = r.IsDBNull(6) ? "" : r.GetString(6), Severity = r.IsDBNull(7) ? "" : r.GetString(7), CreatedBy = r.IsDBNull(8) ? "" : r.GetString(8)
                });
            }
            return list;
        }
        public static void SaveCrewRecord(CrewRecord rec)
        {
            EnsureProfileTables();
            bool isNew = rec.Id <= 0;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = isNew
                ? "INSERT INTO CrewRecords (CrewId, RecordDate, Kind, Type, Description, DispatchNumber, Severity, CreatedBy) VALUES (@c, @d, @k, @t, @desc, @dn, @sev, @by)"
                : "UPDATE CrewRecords SET RecordDate=@d, Kind=@k, Type=@t, Description=@desc, DispatchNumber=@dn, Severity=@sev WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", rec.Id);
            cmd.Parameters.AddWithValue("@c", rec.CrewId);
            cmd.Parameters.AddWithValue("@d", rec.RecordDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@k", rec.Kind);
            cmd.Parameters.AddWithValue("@t", rec.Type.Trim());
            cmd.Parameters.AddWithValue("@desc", rec.Description.Trim());
            cmd.Parameters.AddWithValue("@dn", rec.DispatchNumber.Trim());
            cmd.Parameters.AddWithValue("@sev", rec.Severity);
            cmd.Parameters.AddWithValue("@by", Who);
            cmd.ExecuteNonQuery();
            if (isNew) { using var idc = conn.CreateCommand(); idc.CommandText = "SELECT last_insert_rowid()"; rec.Id = Convert.ToInt32(idc.ExecuteScalar()); }
            LogAuditTrail(isNew ? "ADD" : "UPDATE", "CrewRecords", rec.Id, $"Crew #{rec.CrewId}", rec.Kind, "", rec.Type, $"{rec.Kind} {(isNew ? "recorded" : "updated")}: {rec.Type}");
        }
        public static void VoidCrewRecord(int id)
        {
            EnsureProfileTables();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE CrewRecords SET IsVoid = 1 WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            LogAuditTrail("VOID", "CrewRecords", id, $"Crew record #{id}", "", "", "", "Crew record voided (kept for history)");
        }
        // ------------------------------------------------------------------ suggestions
        public static readonly string[] DefaultMaintenanceTasks =
        {
            "Change oil and oil filter", "Replace air filter", "Replace fuel filter", "Brake inspection and adjustment", "Replace brake pads / linings",
            "Tire rotation", "Replace tires", "Wheel alignment", "Battery check / replacement", "Coolant flush", "Transmission service",
            "Reefer unit service (refrigeration)", "Reefer gas recharge", "Aircon cleaning", "Clutch adjustment", "Change engine belts",
            "Lights and wiring check", "Suspension / leaf spring repair", "Body / door repair", "LTO registration renewal", "Emission test", "Preventive maintenance service (PMS)"
        };
        public static readonly string[] DefaultIncidentTypes =
        {
            "Damaged crates during unloading", "Damaged crates in transit", "Missing crates", "Temperature breach (reefer failure)", "Late delivery",
            "Vehicle breakdown", "Flat tire", "Road accident (minor)", "Road accident (major)", "Rejected by store", "Wrong store delivered",
            "Short delivery", "Spoiled goods", "Route closure / traffic delay"
        };
        public static readonly string[] DefaultViolationTypes =
        {
            "Overspeeding", "Reckless driving", "Traffic violation (LTO ticket)", "Unauthorized stop / detour", "Late for dispatch", "Absent without notice",
            "No helmet / PPE", "Improper handling of crates", "Incomplete documents (DR / trip report)", "Expired license", "Smoking in vehicle", "Rude to client staff"
        };
        public static readonly string[] DefaultPerformanceRemarks =
        {
            "On time, complete delivery", "Excellent handling of crates", "Helpful to store staff", "Good coordination with aide", "Needs to improve punctuality",
            "Needs to improve documentation", "Commended by client", "Completed extra stops", "Handled breakdown well"
        };
        /// <summary>Distinct values typed before, newest first, followed by the defaults (for type-ahead boxes).</summary>
        public static List<string> GetSuggestions(string source)
        {
            var list = new List<string>();
            try
            {
                EnsureProfileTables();
                string sql = source switch
                {
                    "maintenance" => "SELECT Task FROM TruckMaintenance WHERE COALESCE(IsVoid,0)=0 GROUP BY LOWER(Task) ORDER BY MAX(Id) DESC LIMIT 200",
                    "maintenance-provider" => "SELECT ServiceProvider FROM TruckMaintenance WHERE TRIM(ServiceProvider) <> '' GROUP BY LOWER(ServiceProvider) ORDER BY MAX(Id) DESC LIMIT 100",
                    "maintenance-notes" => "SELECT Notes FROM TruckMaintenance WHERE TRIM(Notes) <> '' GROUP BY LOWER(Notes) ORDER BY MAX(Id) DESC LIMIT 100",
                    "incident" => "SELECT Cause FROM IncidentReports WHERE TRIM(COALESCE(Cause,'')) <> '' GROUP BY LOWER(Cause) ORDER BY MAX(Id) DESC LIMIT 200",
                    "incident-notes" => "SELECT Description FROM IncidentReports WHERE TRIM(COALESCE(Description,'')) <> '' GROUP BY LOWER(Description) ORDER BY MAX(Id) DESC LIMIT 100",
                    "violation" => "SELECT Type FROM CrewRecords WHERE Kind = 'Violation' GROUP BY LOWER(Type) ORDER BY MAX(Id) DESC LIMIT 200",
                    "performance" => "SELECT Type FROM CrewRecords WHERE Kind = 'Performance remark' GROUP BY LOWER(Type) ORDER BY MAX(Id) DESC LIMIT 200",
                    "crew-notes" => "SELECT Description FROM CrewRecords WHERE TRIM(Description) <> '' GROUP BY LOWER(Description) ORDER BY MAX(Id) DESC LIMIT 100",
                    "truck-remarks" => "SELECT Remarks FROM Trucks WHERE TRIM(COALESCE(Remarks,'')) <> '' GROUP BY LOWER(Remarks) LIMIT 100",
                    _ => ""
                };
                if (sql.Length > 0)
                {
                    using var conn = new SqliteConnection(connectionString);
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) if (!r.IsDBNull(0)) list.Add(r.GetString(0).Trim());
                }
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] GetSuggestions({source}): {ex.Message}"); ErrLog.Swallowed(ex); }
            string[] defaults = source switch
            {
                "maintenance" => DefaultMaintenanceTasks,
                "incident" => DefaultIncidentTypes,
                "violation" => DefaultViolationTypes,
                "performance" => DefaultPerformanceRemarks,
                _ => Array.Empty<string>()
            };
            list.AddRange(defaults);
            return list.Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
