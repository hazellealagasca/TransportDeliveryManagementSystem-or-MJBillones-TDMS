using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Payments: the reason kept when an amount is changed from what the trip reports work out to
    /// (statement totals, invoice amounts). Each change is also written to the audit log.
    /// </summary>
    public static partial class MJdatabase
    {
        private static bool adjustmentColumnsReady;
        public static void EnsureAdjustmentColumns()
        {
            if (adjustmentColumnsReady) return;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            void Add(string table, string column, string type)
            {
                bool exists = false, tableExists = false;
                using (var c = conn.CreateCommand())
                {
                    c.CommandText = $"PRAGMA table_info({table})";
                    using var r = c.ExecuteReader();
                    while (r.Read()) { tableExists = true; if (string.Equals(r.GetString(1), column, StringComparison.OrdinalIgnoreCase)) exists = true; }
                }
                if (!tableExists || exists) return;
                using var a = conn.CreateCommand();
                a.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {type}";
                try { a.ExecuteNonQuery(); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
            }
            Add("StatementsOfAccount", "AdjustmentReason", "TEXT DEFAULT ''");
            Add("Invoices", "AdjustmentReason", "TEXT DEFAULT ''");
            // Cancelled trips: "Chargeable" (client still pays CancelFee, so it can be billed) or "NoCharge".
            Add("Deliveries", "CancelCharge", "TEXT DEFAULT ''");
            Add("Deliveries", "CancelFee", "REAL DEFAULT 0");
            adjustmentColumnsReady = true;
        }
        private static readonly HashSet<string> adjustableTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "StatementsOfAccount", "Invoices" };
        /// <summary>The reason saved with a statement or invoice whose amount was changed by hand ("" when none).</summary>
        public static string GetAdjustmentReason(string table, int id)
        {
            if (id <= 0 || !adjustableTables.Contains(table)) return "";
            try
            {
                EnsureAdjustmentColumns();
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT COALESCE(AdjustmentReason, '') FROM {table} WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                return cmd.ExecuteScalar()?.ToString() ?? "";
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); return ""; }
        }
        /// <summary>
        /// Keeps why an amount differs from the trip-report figure (blank clears it) and, when there is a reason,
        /// writes an ADJUSTMENT entry to the audit log with the original and the entered amount.
        /// </summary>
        public static void SaveAdjustmentReason(string table, int id, string identifier, string reason, decimal original, decimal current)
        {
            if (id <= 0 || !adjustableTables.Contains(table)) return;
            reason = (reason ?? "").Trim();
            EnsureAdjustmentColumns();
            string before = GetAdjustmentReason(table, id);
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"UPDATE {table} SET AdjustmentReason = @r WHERE Id = @id";
                cmd.Parameters.AddWithValue("@r", reason);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            if (reason.Length > 0 && (reason != before || original != current))
                LogAuditTrail("ADJUSTMENT", table, id, identifier, "Amount", original.ToString("0.00"), current.ToString("0.00"), reason);
        }
            /// <summary>
        /// Dispatches billed by a direct invoice (not through a statement): dispatch id → invoice number.
        /// A dispatch billed one way can't be billed the other way.
        /// </summary>
        public static Dictionary<int, string> GetDirectlyInvoicedDeliveryIds(int excludeInvoiceId = 0)
        {
            var map = new Dictionary<int, string>();
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT DeliveryId, InvoiceNumber FROM Invoices
                                    WHERE COALESCE(DeliveryId, 0) > 0 AND COALESCE(SoaId, 0) = 0 AND Id <> @ex
                                      AND LOWER(COALESCE(Status, '')) NOT IN ('cancelled', 'void', 'voided')";
                cmd.Parameters.AddWithValue("@ex", excludeInvoiceId);
                using var r = cmd.ExecuteReader();
                while (r.Read()) { int id = r.GetInt32(0); if (!map.ContainsKey(id)) map[id] = r.IsDBNull(1) ? "" : r.GetString(1); }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return map;
        }
        /// <summary>Records whether a cancelled trip is still charged to the client. Chargeable trips can be billed for <paramref name="fee"/>; no-charge ones never can.</summary>
        public static void SetDeliveryCancelCharge(int deliveryId, bool chargeable, decimal fee)
        {
            if (deliveryId <= 0) return;
            EnsureAdjustmentColumns();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Deliveries SET CancelCharge = @c, CancelFee = @f WHERE Id = @id";
            cmd.Parameters.AddWithValue("@c", chargeable ? "Chargeable" : "NoCharge");
            cmd.Parameters.AddWithValue("@f", (double)(chargeable ? Centavos(fee) : 0m));
            cmd.Parameters.AddWithValue("@id", deliveryId);
            cmd.ExecuteNonQuery();
        }
        /// <summary>Cancelled trips marked chargeable, with the agreed fee: the only cancelled trips Billing may invoice.</summary>
        public static Dictionary<int, decimal> GetChargeableCancelledFees()
        {
            var map = new Dictionary<int, decimal>();
            try
            {
                EnsureAdjustmentColumns();
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, COALESCE(CancelFee, 0) FROM Deliveries WHERE Status = 'Cancelled' COLLATE NOCASE AND CancelCharge = 'Chargeable' AND COALESCE(CancelFee, 0) > 0";
                using var r = cmd.ExecuteReader();
                while (r.Read()) map[r.GetInt32(0)] = Centavos(Convert.ToDecimal(r.GetDouble(1)));
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return map;
        }

            /// <summary>Rule 16b: an order (loading schedule / dispatch) carries at most this many aides.</summary>
        public const int MaxAidesPerOrder = 2;
        /// <summary>The aide ids stored in a comma list, in their saved order.</summary>
        public static List<int> AideIdList(string? aideIds) =>
            (aideIds ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x.Trim(), out var v) ? v : 0).Where(v => v > 0).Distinct().ToList();
        /// <summary>
        /// One-time cleanup (rule 16b, approved Nov 6, 2026): orders saved with more than 2 aides keep the first 2.
        /// A database backup is made first and each trimmed order is written to the audit trail. Crew pay rows are not touched.
        /// </summary>
        public static int TrimOrdersToTwoAidesOnce()
        {
            const string key = "Migration_TrimAidesToTwo";
            if (GetSystemSetting(key, "") == "done") return 0;
            var targets = new List<(int Id, string No, string Ids, string Names)>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, DispatchNumber, COALESCE(AideIds,''), COALESCE(AideNames,'') FROM Deliveries";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    string ids = r.GetString(2);
                    if (AideIdList(ids).Count > MaxAidesPerOrder) targets.Add((r.GetInt32(0), r.IsDBNull(1) ? "" : r.GetString(1), ids, r.GetString(3)));
                }
            }
            if (targets.Count > 0)
            {
                string backup = System.IO.Path.Combine(Functions.AppPaths.Backups, $"BeforeAideTrim_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
                System.IO.Directory.CreateDirectory(Functions.AppPaths.Backups);
                if (!BackupDatabase(backup)) return -1; // no backup, no change
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var tx = conn.BeginTransaction();
                foreach (var t in targets)
                {
                    var keepIds = AideIdList(t.Ids).Take(MaxAidesPerOrder).ToList();
                    var names = t.Names.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Take(MaxAidesPerOrder).ToList();
                    using var u = conn.CreateCommand();
                    u.Transaction = tx;
                    u.CommandText = "UPDATE Deliveries SET AideIds = @i, AideNames = @n, AideId = @a1 WHERE Id = @id";
                    u.Parameters.AddWithValue("@i", string.Join(",", keepIds));
                    u.Parameters.AddWithValue("@n", string.Join(", ", names));
                    u.Parameters.AddWithValue("@a1", keepIds.Count > 0 ? keepIds[0] : 0);
                    u.Parameters.AddWithValue("@id", t.Id);
                    u.ExecuteNonQuery();
                }
                tx.Commit();
                foreach (var t in targets)
                    try { LogAuditTrail("UPDATE", "Deliveries", t.Id, t.No, "AideIds", t.Ids, string.Join(",", AideIdList(t.Ids).Take(MaxAidesPerOrder)), "Rule 16b: trimmed to 2 aides (one-time cleanup)"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            }
            SetSystemSetting(key, "done", "Orders with more than 2 aides were trimmed to the first 2");
            return targets.Count;
        }

            /// <summary>Schema additions and one-time data cleanups, run once at start-up (each step is safe to repeat).</summary>
        public static void RunOneTimeMigrations()
        {
            EnsureAdjustmentColumns();
            TrimOrdersToTwoAidesOnce();
            RecomputeAllStoreRouteLabels();
            ClearSavedDraftsOnce();
            // Nov 6: one receipt per statement (receipts table allows the shared number), then the agreed cleanup of older records.
            try { EnsureReceiptSchemaForSoa(); RunPaymentCleanupOnce(); } catch (Exception ex) { ErrLog.Error("RunOneTimeMigrations.Payments", ex); }
            // Nov 6 workflow: trip report Draft/Completed/Invoiced, SOA Normal/Special, no Partial or stored Overdue (after a backup, once).
            try { EnsureWorkflowSchema(); } catch (Exception ex) { ErrLog.Error("RunOneTimeMigrations.Workflow", ex); }
            // Nov 6: trips billed by an older statement's single invoice are Invoiced too (re-checked once for databases already updated).
            try
            {
                if (GetSystemSetting("Nov6_LegacySoaTripSync", "0") != "1")
                {
                    ResyncTripReportStatuses();
                    SetSystemSetting("Nov6_LegacySoaTripSync", "1", "Nov 6 workflow: trip reports billed by an older statement's invoice marked Invoiced");
                }
            }
            catch (Exception ex) { ErrLog.Error("RunOneTimeMigrations.LegacySoaTrips", ex); }
        }

    }
}
