using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Truck load-planning matrix (crates per truck category for Dry, Cold and Combi loads), truck status history
    /// (maintenance and decommission reasons) and the one-time data conversions that go with them.
    /// </summary>
    public static partial class MJdatabase
    {
        public const string CargoDry = "Dry Goods";
        public const string CargoCold = "Cold / Chilled";
        public const string CargoCombi = "Combi (Cold + Dry)";
        public const string LegacyCargoMix = "Dry and Chilled Mix";
        public const string TruckDecommissioned = "Decommissioned";




        /// <summary>Normalises any stored cargo text to one of the three crate cargo types.</summary>
        public static string NormalizeCargo(string? cargo)
        {
            string c = (cargo ?? "").Trim();
            if (c.Length == 0) return CargoCold;
            if (c.Contains("Combi", StringComparison.OrdinalIgnoreCase) || c.Contains("Mix", StringComparison.OrdinalIgnoreCase)) return CargoCombi;
            if (c.Contains("Dry", StringComparison.OrdinalIgnoreCase)) return CargoDry;
            if (c.Contains("Cold", StringComparison.OrdinalIgnoreCase) || c.Contains("Chill", StringComparison.OrdinalIgnoreCase)) return CargoCold;
            return c;
        }




        public static bool IsDecommissioned(string? status) =>
            string.Equals((status ?? "").Trim(), TruckDecommissioned, StringComparison.OrdinalIgnoreCase) ||
            string.Equals((status ?? "").Trim(), "Inactive", StringComparison.OrdinalIgnoreCase);




        private static bool capacitySchemaReady;
        public static void EnsureCapacitySchema()
        {
            if (capacitySchemaReady) return;
            EnsureSoaStoreCodeColumn();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            void Try(string sql)
            {
                try { using var c = conn.CreateCommand(); c.CommandText = sql; c.ExecuteNonQuery(); }
                catch (Exception ex) { ErrLog.Swallowed(ex); }
            }
            Try(@"CREATE TABLE IF NOT EXISTS TruckCapacityMatrix (
                    CategoryName TEXT PRIMARY KEY COLLATE NOCASE,
                    DryMax INTEGER NOT NULL DEFAULT 0,
                    ColdMax INTEGER NOT NULL DEFAULT 0,
                    CombiColdMax INTEGER NOT NULL DEFAULT 0,
                    CombiDryMax INTEGER NOT NULL DEFAULT 0,
                    UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP)");
            Try(@"CREATE TABLE IF NOT EXISTS TruckStatusHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TruckId INTEGER NOT NULL,
                    OldStatus TEXT DEFAULT '',
                    NewStatus TEXT DEFAULT '',
                    Reason TEXT DEFAULT '',
                    ChangedBy TEXT DEFAULT '',
                    ChangedAt TEXT DEFAULT CURRENT_TIMESTAMP)");
            Try("CREATE INDEX IF NOT EXISTS idx_truck_status_hist ON TruckStatusHistory(TruckId, ChangedAt DESC)");
            Try("CREATE TRIGGER IF NOT EXISTS trg_nodelete_TruckStatusHistory BEFORE DELETE ON TruckStatusHistory BEGIN SELECT RAISE(ABORT, 'Truck status history is kept.'); END;");
            foreach (var t in new[] { "Deliveries", "LoadingSchedules" })
            {
                Try($"ALTER TABLE {t} ADD COLUMN OverCapacityCrates INTEGER DEFAULT 0");
                Try($"ALTER TABLE {t} ADD COLUMN OverCapacityReason TEXT DEFAULT ''");
                Try($"ALTER TABLE {t} ADD COLUMN ColdCrates INTEGER DEFAULT 0");
                Try($"ALTER TABLE {t} ADD COLUMN DryCrates INTEGER DEFAULT 0");
            }
            // "Dry and Chilled Mix" is now "Combi (Cold + Dry)"; "Inactive" trucks are now "Decommissioned".
            foreach (var t in new[] { "Trucks", "Deliveries", "LoadingSchedules" })
                Try($"UPDATE {t} SET CargoType = '{CargoCombi}' WHERE CargoType = '{LegacyCargoMix}'");
            Try($"UPDATE Trucks SET Status = '{TruckDecommissioned}' WHERE LOWER(TRIM(Status)) = 'inactive'");
            capacitySchemaReady = true;
            try { SeedCapacityMatrix(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            try { SyncAllTruckCapacities(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }




        /// <summary>Default load-planning matrix: (Dry, Cold, Combi cold part, Combi dry part).</summary>
        private static (int dry, int cold, int cc, int cd)? DefaultMatrixFor(string category)
        {
            string n = (category ?? "").ToUpperInvariant();
            var digits = System.Text.RegularExpressions.Regex.Match(n, @"\d+");
            int tons = digits.Success && int.TryParse(digits.Value, out var t) ? t : 0;
            // The tonnage decides first ("FORWARD 4 TONS" is a 4-ton truck); "FWD/FORWARD" alone means the 6-ton forward.
            if (tons == 4) return (280, 120, 75, 72);
            if (tons == 6) return (300, 200, 150, 100);
            if (tons == 8) return (500, 280, 250, 140);
            if (tons == 0 && (n.Contains("FWD") || n.Contains("FORWARD"))) return (300, 200, 150, 100);
            return null;
        }




        private static void SeedCapacityMatrix()
        {
            // One-time fix: the first seeding gave "FORWARD 4 TONS"-style names the 6-ton numbers.
            if (!string.Equals(GetSystemSetting("Fix.MatrixSeed.v2", ""), "done", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var row in GetCapacityMatrix())
                {
                    var d = DefaultMatrixFor(row.CategoryName);
                    bool hasSixTonDefault = row.DryMax == 300 && row.ColdMax == 200 && row.CombiColdMax == 150 && row.CombiDryMax == 100;
                    if (d.HasValue && hasSixTonDefault && d.Value != (300, 200, 150, 100))
                        WriteCapacityRow(new TruckCapacity { CategoryName = row.CategoryName, DryMax = d.Value.dry, ColdMax = d.Value.cold, CombiColdMax = d.Value.cc, CombiDryMax = d.Value.cd });
                }
                try { SetSystemSetting("Fix.MatrixSeed.v2", "done", "Corrected seeded truck capacity matrix"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            }
            var existing = GetCapacityMatrix().Select(m => m.CategoryName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var cat in GetTruckCategories())
            {
                if (existing.Contains(cat.CategoryName)) continue;
                var d = DefaultMatrixFor(cat.CategoryName);
                // Unknown categories start from their old single capacity; Combi stays unset until someone fills it in.
                var row = d.HasValue
                    ? new TruckCapacity { CategoryName = cat.CategoryName, DryMax = d.Value.dry, ColdMax = d.Value.cold, CombiColdMax = d.Value.cc, CombiDryMax = d.Value.cd }
                    : new TruckCapacity { CategoryName = cat.CategoryName, DryMax = cat.CrateCapacity, ColdMax = cat.CrateCapacity, CombiColdMax = 0, CombiDryMax = 0 };
                WriteCapacityRow(row);
            }
        }




        public static List<TruckCapacity> GetCapacityMatrix()
        {
            var list = new List<TruckCapacity>();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CategoryName, DryMax, ColdMax, CombiColdMax, CombiDryMax FROM TruckCapacityMatrix ORDER BY CategoryName";
            try
            {
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new TruckCapacity { CategoryName = r.GetString(0), DryMax = r.GetInt32(1), ColdMax = r.GetInt32(2), CombiColdMax = r.GetInt32(3), CombiDryMax = r.GetInt32(4) });
            }
            catch (SqliteException) { }
            return list;
        }




        public static TruckCapacity? GetCapacityFor(string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) return null;
            EnsureCapacitySchema();
            return GetCapacityMatrix().FirstOrDefault(m => string.Equals(m.CategoryName.Trim(), category.Trim(), StringComparison.OrdinalIgnoreCase));
        }




        private static void WriteCapacityRow(TruckCapacity m)
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO TruckCapacityMatrix (CategoryName, DryMax, ColdMax, CombiColdMax, CombiDryMax, UpdatedAt)
                                VALUES (@n, @d, @c, @cc, @cd, datetime('now'))
                                ON CONFLICT(CategoryName) DO UPDATE SET DryMax=@d, ColdMax=@c, CombiColdMax=@cc, CombiDryMax=@cd, UpdatedAt=datetime('now')";
            cmd.Parameters.AddWithValue("@n", m.CategoryName.Trim());
            cmd.Parameters.AddWithValue("@d", m.DryMax);
            cmd.Parameters.AddWithValue("@c", m.ColdMax);
            cmd.Parameters.AddWithValue("@cc", m.CombiColdMax);
            cmd.Parameters.AddWithValue("@cd", m.CombiDryMax);
            cmd.ExecuteNonQuery();
        }




        /// <summary>Hard rules for a category's matrix (Section 8: these block saving). Empty when it is valid.</summary>
        public static List<string> CapacityRuleErrors(TruckCapacity m)
        {
            var e = new List<string>();
            if (string.IsNullOrWhiteSpace(m.CategoryName)) e.Add("Type the category name.");
            if (m.DryMax <= 0) e.Add("Dry capacity must be more than 0 crates.");
            if (m.ColdMax <= 0) e.Add("Cold capacity must be more than 0 crates.");
            if (m.DryMax > 0 && m.ColdMax > m.DryMax) e.Add($"Cold capacity ({m.ColdMax}) can't be more than Dry capacity ({m.DryMax}) — the truck body is the same size.");
            if ((m.CombiColdMax > 0) != (m.CombiDryMax > 0)) e.Add("Fill in both Combi numbers (cold part and dry part), or leave both at 0.");
            if (m.CombiColdMax > m.ColdMax && m.ColdMax > 0) e.Add($"Combi cold part ({m.CombiColdMax}) can't be more than Cold capacity ({m.ColdMax}).");
            if (m.CombiMax > m.TruckMax && m.TruckMax > 0) e.Add($"Combi total ({m.CombiMax}) can't be more than the truck's maximum ({m.TruckMax} = Dry capacity).");
            return e;
        }




        /// <summary>Saves a category's matrix and refreshes every truck of that category (capacity is locked to it).</summary>
        public static int SaveCapacityMatrix(TruckCapacity m, string? oldName = null)
        {
            EnsureCapacitySchema();
            var errors = CapacityRuleErrors(m);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            var before = GetCapacityFor(oldName ?? m.CategoryName);
            if (!string.IsNullOrWhiteSpace(oldName) && !string.Equals(oldName.Trim(), m.CategoryName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM TruckCapacityMatrix WHERE CategoryName = @o";
                cmd.Parameters.AddWithValue("@o", oldName.Trim());
                cmd.ExecuteNonQuery();
            }
            WriteCapacityRow(m);
            // Keep the old single "usual capacity" in step (used by older screens and reports).
            try { SaveTruckCategory(m.CategoryName, m.DryMax, oldName); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            int changed = SyncAllTruckCapacities();
            LogAuditTrail(before == null ? "ADD" : "UPDATE", "TruckCapacityMatrix", 0, m.CategoryName, "Matrix",
                before == null ? "" : $"Dry {before.DryMax} / Cold {before.ColdMax} / Combi {before.CombiColdMax}+{before.CombiDryMax}",
                $"Dry {m.DryMax} / Cold {m.ColdMax} / Combi {m.CombiColdMax}+{m.CombiDryMax}", "Truck category capacity matrix saved");
            NotifyCategoriesChanged();
            return changed;
        }




        /// <summary>The locked crate capacity of a truck: from its category's matrix and its cargo type.</summary>
        public static int LockedCapacity(string? category, string? truckCargo, int fallback)
        {
            var m = GetCapacityFor(category);
            if (m == null) return fallback;
            int v = NormalizeCargo(truckCargo) switch
            {
                CargoDry => m.DryMax,
                CargoCombi => m.CombiMax > 0 ? m.CombiMax : m.ColdMax,
                _ => m.ColdMax
            };
            return v > 0 ? v : fallback;
        }




        /// <summary>Sets every truck's capacity from its category and cargo type. Returns how many trucks changed.</summary>
        public static int SyncAllTruckCapacities()
        {
            int changed = 0;
            var matrix = GetCapacityMatrix();
            if (matrix.Count == 0) return 0;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            var updates = new List<(int id, int cap)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, COALESCE(TruckType,''), COALESCE(CargoType,''), CapacityCrates FROM Trucks";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var m = matrix.FirstOrDefault(x => string.Equals(x.CategoryName.Trim(), r.GetString(1).Trim(), StringComparison.OrdinalIgnoreCase));
                    if (m == null) continue;
                    int want = NormalizeCargo(r.GetString(2)) switch { CargoDry => m.DryMax, CargoCombi => m.CombiMax > 0 ? m.CombiMax : m.ColdMax, _ => m.ColdMax };
                    if (want > 0 && want != r.GetInt32(3)) updates.Add((r.GetInt32(0), want));
                }
            }
            foreach (var (id, cap) in updates)
            {
                using var u = conn.CreateCommand();
                u.CommandText = "UPDATE Trucks SET CapacityCrates = @c WHERE Id = @id";
                u.Parameters.AddWithValue("@c", cap);
                u.Parameters.AddWithValue("@id", id);
                changed += u.ExecuteNonQuery();
            }
            return changed;
        }




        // ------------------------------------------------------------------ status history
        public static void LogTruckStatusChange(int truckId, string oldStatus, string newStatus, string reason)
        {
            if (truckId <= 0) return;
            EnsureCapacitySchema();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO TruckStatusHistory (TruckId, OldStatus, NewStatus, Reason, ChangedBy, ChangedAt) VALUES (@t, @o, @n, @r, @by, @at)";
            cmd.Parameters.AddWithValue("@t", truckId);
            cmd.Parameters.AddWithValue("@o", oldStatus ?? "");
            cmd.Parameters.AddWithValue("@n", newStatus ?? "");
            cmd.Parameters.AddWithValue("@r", (reason ?? "").Trim());
            cmd.Parameters.AddWithValue("@by", SessionContext.ActiveUser?.Username ?? SessionContext.CurrentUsername ?? "");
            cmd.Parameters.AddWithValue("@at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
            LogAuditTrail("STATUS", "Trucks", truckId, $"Truck #{truckId}", "Status", oldStatus ?? "", newStatus ?? "", string.IsNullOrWhiteSpace(reason) ? "Truck status changed" : $"Reason: {reason.Trim()}");
        }




        public static List<(DateTime At, string OldStatus, string NewStatus, string Reason, string By)> GetTruckStatusHistory(int truckId)
        {
            var list = new List<(DateTime, string, string, string, string)>();
            try
            {
                EnsureCapacitySchema();
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT ChangedAt, COALESCE(OldStatus,''), COALESCE(NewStatus,''), COALESCE(Reason,''), COALESCE(ChangedBy,'') FROM TruckStatusHistory WHERE TruckId = @t ORDER BY ChangedAt DESC, Id DESC";
                cmd.Parameters.AddWithValue("@t", truckId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add((DateTime.TryParse(r.GetString(0), out var d) ? d : DateTime.MinValue, r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4)));
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return list;
        }




        // ------------------------------------------------------------------ per-dispatch capacity info
        /// <summary>Stores the cold/dry split and any over-matrix exception on a dispatch, with an audit entry when it goes over.</summary>
        public static void SetDeliveryCapacityInfo(int deliveryId, string dispatchNumber, int cold, int dry, int overCrates, string reason)
        {
            if (deliveryId <= 0) return;
            EnsureCapacitySchema();
            var before = GetDeliveryCapacityInfo(deliveryId);
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Deliveries SET ColdCrates = @c, DryCrates = @d, OverCapacityCrates = @o, OverCapacityReason = @r WHERE Id = @id";
                cmd.Parameters.AddWithValue("@c", Math.Max(0, cold));
                cmd.Parameters.AddWithValue("@d", Math.Max(0, dry));
                cmd.Parameters.AddWithValue("@o", Math.Max(0, overCrates));
                cmd.Parameters.AddWithValue("@r", (reason ?? "").Trim());
                cmd.Parameters.AddWithValue("@id", deliveryId);
                cmd.ExecuteNonQuery();
            }
            if (overCrates > 0 && (before.Over != overCrates || before.Reason != (reason ?? "").Trim()))
                LogAuditTrail("OVER CAPACITY", "Deliveries", deliveryId, dispatchNumber, "OverCapacityCrates", before.Over.ToString(), overCrates.ToString(), $"Over matrix +{overCrates}. Reason: {(reason ?? "").Trim()}");
        }




        public static (int Cold, int Dry, int Over, string Reason) GetDeliveryCapacityInfo(int deliveryId)
        {
            try
            {
                EnsureCapacitySchema();
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COALESCE(ColdCrates,0), COALESCE(DryCrates,0), COALESCE(OverCapacityCrates,0), COALESCE(OverCapacityReason,'') FROM Deliveries WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", deliveryId);
                using var r = cmd.ExecuteReader();
                if (r.Read()) return (r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetString(3));
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return (0, 0, 0, "");
        }




        /// <summary>Dispatches that went over the matrix: id → crates over.</summary>
        public static Dictionary<int, int> GetOverCapacityMap()
        {
            var map = new Dictionary<int, int>();
            try
            {
                EnsureCapacitySchema();
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, OverCapacityCrates FROM Deliveries WHERE COALESCE(OverCapacityCrates,0) > 0";
                using var r = cmd.ExecuteReader();
                while (r.Read()) map[r.GetInt32(0)] = r.GetInt32(1);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return map;
        }




        /// <summary>"4T" / "6T" / "8T" for statements: from the category name, else from the truck's dry capacity.</summary>
        public static string TonnageLabel(Truck trk)
        {
            string n = (trk.TruckType ?? "").ToUpperInvariant();
            var mt = System.Text.RegularExpressions.Regex.Match(n, @"(\d+)\s*(T\b|TON)");
            if (mt.Success) return mt.Groups[1].Value + "T";
            if (n.Contains("FWD") || n.Contains("FORWARD")) return "6T";
            int cap = trk.CapacityCrates;
            try { var m = GetCapacityFor(trk.TruckType); if (m != null && m.DryMax > 0) cap = m.DryMax; } catch (Exception ex) { ErrLog.Swallowed(ex); }
            return cap >= 400 ? "8T" : (cap >= 290 ? "6T" : "4T");
        }
    }
}
