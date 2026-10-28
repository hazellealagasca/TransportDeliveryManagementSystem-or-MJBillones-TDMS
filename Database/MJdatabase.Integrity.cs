using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Data integrity rules: records that make up the business history are never hard-deleted. "Delete" on them
    /// cancels, voids or deactivates the record instead, the database itself refuses DELETE on those tables (so
    /// nothing can cascade away), audit logs are append-only, and identifiers people type are kept unique.
    /// </summary>
    public static partial class MJdatabase
    {
        /// <summary>What the last <see cref="DeleteRecord"/> call actually did, in plain words for the success message.</summary>
        public static string LastDeleteSummary { get; private set; } = "";
        /// <summary>Tables whose rows are history: the database refuses DELETE on them.</summary>
        private static readonly string[] ProtectedTables =
        {
            "Deliveries", "LoadingSchedules", "Invoices", "CollectionReceipts", "TripReports", "IncidentReports",
            "StatementsOfAccount", "SpecialTrips", "Clients", "Trucks", "Crews", "AuditTrail", "LoginAudit"
        };
        /// <summary>Tables "delete" still removes outright: settings-style lists that no history row points at.</summary>
        private static readonly HashSet<string> HardDeleteAllowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "RateMatrix", "LookupCategories" };
        /// <summary>One sentence for the confirmation prompt saying what "delete" will really do to this kind of record.</summary>
        public static string DeletePolicyNote(string tableName) => (tableName ?? "") switch
        {
            "Deliveries" => "The dispatch is not erased: it is marked Cancelled and stays in the dispatch history, and its truck and crew are freed.",
            "LoadingSchedules" => "The loading order is not erased: it is marked Cancelled and stays in the history.",
            "Invoices" => "The invoice is not erased: it is marked Cancelled and stays in the billing history.",
            "CollectionReceipts" => "The receipt is not erased: it is voided, its amount no longer counts toward the invoice, and it stays in the audit log.",
            "TripReports" => "The trip report is not erased: it is voided and kept for the audit log, and the dispatch can get a new report.",
            "IncidentReports" => "The incident is not erased: it is voided, stops counting toward deductions, and stays in the audit log.",
            "StatementsOfAccount" => "The statement is not erased: it is marked Cancelled and its invoices can be put on a new statement.",
            "SpecialTrips" => "The special trip is not erased: it is marked Cancelled and stays in the history.",
            "Users" => "The account is not erased: it is deactivated so it can no longer sign in. Its history stays.",
            "Crews" => "The crew member is not erased: they are set to inactive and no longer offered for new trips. Their trip history stays.",
            "Trucks" => "The truck is not erased: it is set to Inactive and no longer offered for new trips. Its trip history stays.",
            "Clients" => "The client is not erased: it is deactivated and hidden from new orders. Its orders, invoices and payments stay.",
            "DeliveryRoutes" => "The route is not erased: it is deactivated and no longer offered for new orders.",
            _ => "This can't be undone."
        };
        /// <summary>Soft delete for history tables. Returns null when the table is not handled here.</summary>
        private static bool? SoftDelete(string tableName, int id)
        {
            if (HardDeleteAllowed.Contains(tableName)) return null;
            string user = SessionContext.ActiveUser?.Username ?? SessionContext.CurrentUsername;
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();
            string Scalar(string sql)
            {
                using var c = conn.CreateCommand();
                c.Transaction = tx;
                c.CommandText = sql;
                c.Parameters.AddWithValue("@id", id);
                return c.ExecuteScalar()?.ToString() ?? "";
            }
            int Exec(string sql, params (string, object)[] ps)
            {
                using var c = conn.CreateCommand();
                c.Transaction = tx;
                c.CommandText = sql;
                c.Parameters.AddWithValue("@id", id);
                c.Parameters.AddWithValue("@now", now);
                c.Parameters.AddWithValue("@user", user);
                foreach (var (k, v) in ps) c.Parameters.AddWithValue(k, v);
                return c.ExecuteNonQuery();
            }
            string ident = $"#{id}";
            string action = "ARCHIVE";
            switch (tableName)
            {
                case "Deliveries":
                    {
                        ident = Scalar("SELECT DispatchNumber FROM Deliveries WHERE Id = @id");
                        string status = Scalar("SELECT Status FROM Deliveries WHERE Id = @id");
                        if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException($"Dispatch {ident} is completed. Completed dispatches are part of the delivery history and can't be removed.");
                        if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException($"Dispatch {ident} is already cancelled. Cancelled dispatches stay in the history for the audit trail.");
                        Exec("UPDATE Deliveries SET Status = 'Cancelled', CancellationReason = CASE WHEN COALESCE(CancellationReason,'') = '' THEN @r ELSE CancellationReason END WHERE Id = @id", ("@r", $"Removed by {user} on {now}"));
                        Exec("UPDATE LoadingSchedules SET Status = 'Pending' WHERE Id = (SELECT LoadingScheduleId FROM Deliveries WHERE Id = @id) AND Status <> 'Completed' AND Status <> 'Cancelled'");
                        ReconcileResourceStatuses(conn, tx);
                        action = "CANCEL";
                        LastDeleteSummary = $"Dispatch {ident} was cancelled. It stays in the dispatch history and its truck and crew are free again.";
                        break;
                    }
                case "LoadingSchedules":
                    {
                        ident = Scalar("SELECT ScheduleNumber FROM LoadingSchedules WHERE Id = @id");
                        string status = Scalar("SELECT Status FROM LoadingSchedules WHERE Id = @id");
                        if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException($"Loading order {ident} is completed and is part of the history, so it can't be removed.");
                        Exec("UPDATE LoadingSchedules SET Status = 'Cancelled', CancellationReason = CASE WHEN COALESCE(CancellationReason,'') = '' THEN @r ELSE CancellationReason END WHERE Id = @id", ("@r", $"Removed by {user} on {now}"));
                        action = "CANCEL";
                        LastDeleteSummary = $"Loading order {ident} was cancelled. It stays in the history.";
                        break;
                    }
                case "Invoices":
                    {
                        ident = Scalar("SELECT InvoiceNumber FROM Invoices WHERE Id = @id");
                        if (Scalar("SELECT COUNT(*) FROM CollectionReceipts WHERE InvoiceId = @id AND COALESCE(IsVoid,0) = 0") is var n && n != "0" && n != "")
                            throw new InvalidOperationException($"Invoice {ident} has payments recorded. Void its collection receipts first.");
                        Exec("UPDATE Invoices SET Status = 'Cancelled' WHERE Id = @id");
                        action = "CANCEL";
                        LastDeleteSummary = $"Invoice {ident} was cancelled. It stays in the billing history and no longer counts as outstanding.";
                        break;
                    }
                case "CollectionReceipts":
                    {
                        ident = Scalar("SELECT ReceiptNumber FROM CollectionReceipts WHERE Id = @id");
                        int invoiceId = int.TryParse(Scalar("SELECT InvoiceId FROM CollectionReceipts WHERE Id = @id"), out var iv) ? iv : 0;
                        Exec("UPDATE CollectionReceipts SET IsVoid = 1, VoidedAt = @now, VoidedBy = @user WHERE Id = @id");
                        RecalculateInvoiceStatus(conn, tx, invoiceId);
                        action = "VOID";
                        LastDeleteSummary = $"Receipt {ident} was voided. Its amount no longer counts toward the invoice; the record stays in the audit log.";
                        break;
                    }
                case "TripReports":
                    {
                        ident = Scalar("SELECT TripReportNumber FROM TripReports WHERE Id = @id");
                        Exec("UPDATE TripReports SET IsVoid = 1, VoidedAt = @now, VoidedBy = @user WHERE Id = @id");
                        action = "VOID";
                        LastDeleteSummary = $"Trip report {ident} was voided and kept for the audit log. The dispatch can now get a corrected report.";
                        break;
                    }
                case "IncidentReports":
                    {
                        ident = Scalar("SELECT COALESCE(DispatchNumber,'') FROM IncidentReports WHERE Id = @id");
                        Exec("UPDATE IncidentReports SET Status = 'Voided' WHERE Id = @id");
                        action = "VOID";
                        LastDeleteSummary = $"The incident on {ident} was voided. It no longer counts toward deductions; the record stays in the audit log.";
                        break;
                    }
                case "StatementsOfAccount":
                    {
                        ident = Scalar("SELECT SoaNumber FROM StatementsOfAccount WHERE Id = @id");
                        Exec("UPDATE StatementsOfAccount SET Status = 'Cancelled' WHERE Id = @id");
                        Exec("UPDATE Invoices SET SoaId = 0 WHERE SoaId = @id");
                        action = "CANCEL";
                        LastDeleteSummary = $"Statement {ident} was cancelled. Its invoices are free to go on a new statement.";
                        break;
                    }
                case "SpecialTrips":
                    {
                        ident = Scalar("SELECT OrderReference FROM SpecialTrips WHERE Id = @id");
                        string status = Scalar("SELECT Status FROM SpecialTrips WHERE Id = @id");
                        if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException($"Special trip {ident} is completed and is part of the history, so it can't be removed.");
                        Exec("UPDATE SpecialTrips SET Status = 'Cancelled' WHERE Id = @id");
                        ReconcileResourceStatuses(conn, tx);
                        action = "CANCEL";
                        LastDeleteSummary = $"Special trip {ident} was cancelled. It stays in the history.";
                        break;
                    }
                case "Users":
                    {
                        ident = Scalar("SELECT Username FROM Users WHERE Id = @id");
                        Exec("UPDATE Users SET IsActive = 0 WHERE Id = @id");
                        action = "DEACTIVATE";
                        LastDeleteSummary = $"Account @{ident} was deactivated. It can no longer sign in; its history stays.";
                        break;
                    }
                case "Crews":
                    {
                        ident = Scalar("SELECT FullName FROM Crews WHERE Id = @id");
                        Exec("UPDATE Crews SET IsActive = 0, IsAvailable = 0 WHERE Id = @id");
                        action = "DEACTIVATE";
                        LastDeleteSummary = $"{ident} was set to inactive and won't be offered for new trips. Their trip history stays.";
                        break;
                    }
                case "Trucks":
                    {
                        ident = Scalar("SELECT PlateNumber FROM Trucks WHERE Id = @id");
                        Exec("UPDATE Trucks SET Status = 'Inactive' WHERE Id = @id");
                        action = "DEACTIVATE";
                        LastDeleteSummary = $"Truck {ident} was set to Inactive and won't be offered for new trips. Its trip history stays.";
                        break;
                    }
                default:
                    return null;
            }
            tx.Commit();
            LogAuditTrail(action, tableName, id, string.IsNullOrWhiteSpace(ident) ? $"#{id}" : ident, "", "", "", $"{action.ToLowerInvariant()} instead of delete (history is kept)");
            return true;
        }
        // ------------------------------------------------------------------ clients
        /// <summary>Activates or deactivates a client (the soft delete for clients). Deactivated clients drop out of order pick-lists.</summary>
        public static void SetClientActive(string companyName, bool active)
        {
            if (string.IsNullOrWhiteSpace(companyName)) return;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Clients SET IsActive = @a, DeactivatedAt = @at WHERE LOWER(TRIM(CompanyName)) = LOWER(TRIM(@n))";
                cmd.Parameters.AddWithValue("@a", active ? 1 : 0);
                cmd.Parameters.AddWithValue("@at", active ? "" : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@n", companyName.Trim());
                cmd.ExecuteNonQuery();
            }
            NotifyCategoriesChanged();
            LogAuditTrail(active ? "REACTIVATE" : "DEACTIVATE", "Clients", 0, companyName.Trim(), "IsActive", active ? "0" : "1", active ? "1" : "0", active ? "Client reactivated" : "Client deactivated (soft delete)");
        }
        /// <summary>Last activity date per client (newest dispatch, loading order or invoice), keyed case-insensitively.</summary>
        public static Dictionary<string, DateTime> GetClientLastActivity()
        {
            var map = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                void Take(string sql)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        if (r.IsDBNull(0) || r.IsDBNull(1)) continue;
                        string name = r.GetString(0).Trim();
                        if (name.Length == 0 || !DateTime.TryParse(r.GetValue(1)?.ToString(), out var d)) continue;
                        if (!map.TryGetValue(name, out var cur) || d > cur) map[name] = d;
                    }
                }
                Take("SELECT ClientName, MAX(DispatchDate) FROM Deliveries WHERE Status <> 'Cancelled' GROUP BY ClientName");
                Take("SELECT ClientName, MAX(ScheduleDate) FROM LoadingSchedules WHERE Status <> 'Cancelled' GROUP BY ClientName");
                Take("SELECT ClientName, MAX(IssueDate) FROM Invoices WHERE Status <> 'Cancelled' GROUP BY ClientName");
                Take("SELECT Company, MAX(DispatchDate) FROM SpecialTrips WHERE Status <> 'Cancelled' GROUP BY Company");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] GetClientLastActivity: {ex.Message}"); }
            return map;
        }
        /// <summary>True when a store appears on any dispatch or loading order, so removing it would orphan history.</summary>
        public static bool IsClientStoreInUse(ClientStore store, out string where)
        {
            where = "";
            if (store == null) return false;
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT 'dispatch' FROM DispatchStops WHERE LOWER(TRIM(ClientName)) = LOWER(TRIM(@c)) AND ((TRIM(@code) <> '' AND LOWER(TRIM(StoreCode)) = LOWER(TRIM(@code))) OR LOWER(TRIM(Destination)) = LOWER(TRIM(@name)))
                                    UNION ALL
                                    SELECT 'loading order' FROM LoadingScheduleStops s JOIN LoadingSchedules l ON l.Id = s.LoadingScheduleId WHERE LOWER(TRIM(l.ClientName)) = LOWER(TRIM(@c)) AND ((TRIM(@code) <> '' AND LOWER(TRIM(s.StoreCode)) = LOWER(TRIM(@code))) OR LOWER(TRIM(s.StoreName)) = LOWER(TRIM(@name)))
                                    LIMIT 1";
                cmd.Parameters.AddWithValue("@c", store.ClientCompanyName ?? "");
                cmd.Parameters.AddWithValue("@code", store.StoreCode ?? "");
                cmd.Parameters.AddWithValue("@name", store.StoreName ?? "");
                var v = cmd.ExecuteScalar();
                if (v == null || v == DBNull.Value) return false;
                where = v.ToString() ?? "";
                return true;
            }
            catch { return false; }
        }
        /// <summary>True when another store of the same client already uses this code (case-insensitive).</summary>
        public static bool IsStoreCodeTaken(string clientName, string storeCode, int exceptStoreId)
        {
            if (string.IsNullOrWhiteSpace(storeCode)) return false;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM ClientStores WHERE LOWER(TRIM(ClientCompanyName)) = LOWER(TRIM(@c)) AND LOWER(TRIM(StoreCode)) = LOWER(TRIM(@s)) AND Id <> @id";
            cmd.Parameters.AddWithValue("@c", clientName ?? "");
            cmd.Parameters.AddWithValue("@s", storeCode.Trim());
            cmd.Parameters.AddWithValue("@id", exceptStoreId);
            return Convert.ToInt64(cmd.ExecuteScalar() ?? 0) > 0;
        }
        // ------------------------------------------------------------------ startup
        /// <summary>Runs at startup: unique indexes, no-delete / append-only triggers, and one-time clean-ups.</summary>
        /// <summary>Runs SQLite's integrity check. Index-only damage (rows missing from an index, wrong entry counts)
        /// is repaired with REINDEX, which rebuilds the indexes from the table data without losing anything.
        /// Returns "ok", or the problems that remain.</summary>
        public static string CheckAndRepairIndexes(SqliteConnection conn, out bool repaired)
        {
            repaired = false;
            List<string> Check()
            {
                var problems = new List<string>();
                using var c = conn.CreateCommand();
                c.CommandText = "PRAGMA integrity_check(50);";
                using var r = c.ExecuteReader();
                while (r.Read()) problems.Add(r.GetString(0));
                return problems;
            }
            var found = Check();
            if (found.Count == 1 && found[0].Equals("ok", StringComparison.OrdinalIgnoreCase)) return "ok";
            bool indexOnly = found.All(m => m.Contains("index", StringComparison.OrdinalIgnoreCase)
                && (m.Contains("missing from index", StringComparison.OrdinalIgnoreCase) || m.Contains("wrong # of entries", StringComparison.OrdinalIgnoreCase) || m.Contains("non-unique entry", StringComparison.OrdinalIgnoreCase)));
            if (!indexOnly) return string.Join("; ", found.Take(3));
            try
            {
                using (var c = conn.CreateCommand()) { c.CommandText = "REINDEX;"; c.ExecuteNonQuery(); }
                repaired = true;
            }
            catch (Exception ex) { return $"{found[0]} (repair failed: {ex.Message})"; }
            var after = Check();
            return after.Count == 1 && after[0].Equals("ok", StringComparison.OrdinalIgnoreCase) ? "ok" : string.Join("; ", after.Take(3));
        }
        private static void EnsureDataIntegrity(SqliteConnection conn)
        {
            try { CheckAndRepairIndexes(conn, out bool fixedIdx); if (fixedIdx) System.Diagnostics.Debug.WriteLine("[DB] Rebuilt damaged indexes"); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] index check: {ex.Message}"); }
            void Try(string what, string sql)
            {
                try
                {
                    using var c = conn.CreateCommand();
                    c.CommandText = sql;
                    c.ExecuteNonQuery();
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] Integrity {what}: {ex.Message}"); }
            }
            MigrateLicenseNumbers(conn);
            try { EnsureProfileTables(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] profile tables: {ex.Message}"); }
            Try("client created", "UPDATE Clients SET CreatedAt = datetime('now') WHERE CreatedAt IS NULL OR TRIM(CreatedAt) = ''");
            Try("client active", "UPDATE Clients SET IsActive = 1 WHERE IsActive IS NULL");
            // One-time clean-up: stores that were invented for a client from other clients' / unassigned routes.
            if (!string.Equals(GetSystemSettingSafe(conn, "Fix.DummyStores.v1"), "done", StringComparison.OrdinalIgnoreCase))
            {
                Try("dummy stores", @"
                    DELETE FROM ClientStores
                    WHERE ClientId = 0
                      AND EXISTS (SELECT 1 FROM DeliveryRoutes r
                                  WHERE ClientStores.StoreCode LIKE r.RouteCode || '-S%'
                                    AND LOWER(TRIM(COALESCE(r.ClientCompanyName,''))) <> LOWER(TRIM(ClientStores.ClientCompanyName)))
                      AND NOT EXISTS (SELECT 1 FROM DispatchStops d WHERE LOWER(TRIM(d.ClientName)) = LOWER(TRIM(ClientStores.ClientCompanyName)) AND (LOWER(TRIM(d.StoreCode)) = LOWER(TRIM(ClientStores.StoreCode)) OR LOWER(TRIM(d.Destination)) = LOWER(TRIM(ClientStores.StoreName))))
                      AND NOT EXISTS (SELECT 1 FROM LoadingScheduleStops s JOIN LoadingSchedules l ON l.Id = s.LoadingScheduleId WHERE LOWER(TRIM(l.ClientName)) = LOWER(TRIM(ClientStores.ClientCompanyName)) AND (LOWER(TRIM(s.StoreCode)) = LOWER(TRIM(ClientStores.StoreCode)) OR LOWER(TRIM(s.StoreName)) = LOWER(TRIM(ClientStores.StoreName))));");
                Try("dummy stores flag", "INSERT OR REPLACE INTO SystemSettings (SettingKey, SettingValue, Description, UpdatedAt) VALUES ('Fix.DummyStores.v1', 'done', 'Removed placeholder stores auto-created for clients', datetime('now'))");
            }
            // Exact duplicate stores (same client, code and name) are merged into the first one before the unique index.
            Try("dup stores", "DELETE FROM ClientStores WHERE TRIM(StoreCode) <> '' AND Id NOT IN (SELECT MIN(Id) FROM ClientStores GROUP BY LOWER(TRIM(ClientCompanyName)), LOWER(TRIM(StoreCode)), LOWER(TRIM(StoreName)))");
            // Unique identifiers (case- and space-insensitive). If old data already has duplicates the index is skipped
            // and the app-level checks still stop new ones.
            Try("ux store code", "CREATE UNIQUE INDEX IF NOT EXISTS ux_clientstores_code ON ClientStores(LOWER(TRIM(ClientCompanyName)), LOWER(TRIM(StoreCode))) WHERE TRIM(StoreCode) <> ''");
            Try("ux route code", "CREATE UNIQUE INDEX IF NOT EXISTS ux_routes_code_ci ON DeliveryRoutes(LOWER(TRIM(RouteCode)))");
            Try("ux route name", "CREATE UNIQUE INDEX IF NOT EXISTS ux_routes_name_client ON DeliveryRoutes(LOWER(TRIM(COALESCE(ClientCompanyName,''))), LOWER(TRIM(RouteName)))");
            Try("ux client name", "CREATE UNIQUE INDEX IF NOT EXISTS ux_clients_name_ci ON Clients(LOWER(TRIM(CompanyName)))");
            Try("ux plate", "CREATE UNIQUE INDEX IF NOT EXISTS ux_trucks_plate_ci ON Trucks(UPPER(REPLACE(REPLACE(TRIM(PlateNumber),' ',''),'-','')))");
            Try("ux rate", "CREATE UNIQUE INDEX IF NOT EXISTS ux_rate_rule ON RateMatrix(LOWER(TRIM(RouteName)), LOWER(TRIM(Destination)), LOWER(TRIM(TruckType)), IsPeakSeason)");
            Try("ux username", "CREATE UNIQUE INDEX IF NOT EXISTS ux_users_name_ci ON Users(LOWER(TRIM(Username)))");
            // History is never hard-deleted (this also stops ON DELETE CASCADE from wiping child rows).
            foreach (var t in ProtectedTables)
                Try("no delete " + t, $"CREATE TRIGGER IF NOT EXISTS trg_nodelete_{t} BEFORE DELETE ON {t} BEGIN SELECT RAISE(ABORT, '{t} records are kept for history and cannot be deleted. Cancel, void or deactivate them instead.'); END;");
            // Audit logs are append-only.
            foreach (var t in new[] { "AuditTrail", "LoginAudit" })
                Try("append only " + t, $"CREATE TRIGGER IF NOT EXISTS trg_noupdate_{t} BEFORE UPDATE ON {t} BEGIN SELECT RAISE(ABORT, 'Audit log entries cannot be changed.'); END;");
        }
        private static string GetSystemSettingSafe(SqliteConnection conn, string key)
        {
            try
            {
                using var c = conn.CreateCommand();
                c.CommandText = "SELECT SettingValue FROM SystemSettings WHERE SettingKey = @k";
                c.Parameters.AddWithValue("@k", key);
                return c.ExecuteScalar()?.ToString() ?? "";
            }
            catch { return ""; }
        }
    }
}
