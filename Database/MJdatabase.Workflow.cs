using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Nov 6, 2026 workflow: Trip Report (Draft → Completed → Invoiced) → Service Invoice → SOA (Normal or Special, one client)
    /// → one Collection Receipt for the full amount → SOA and invoices Paid. Every status change is made here, in the
    /// transaction of the record that causes it, so every screen that makes invoices gives the same result.
    /// </summary>
    public static partial class MJdatabase
    {
        private const string WorkflowSchemaKey = "Nov6_WorkflowSchemaDone";
        private static bool workflowColumnsReady;

        /// <summary>Forgets that the workflow columns were checked (after switching database files).</summary>
        private static void ResetWorkflowSchemaFlag() => workflowColumnsReady = false;

        /// <summary>Adds TripReports.Status, StatementsOfAccount.SoaType and CollectionReceipts.Status when missing. Cheap after the first call.</summary>
        public static void EnsureWorkflowColumns()
        {
            if (workflowColumnsReady) return;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            AddColumnIfMissing(conn, "TripReports", "Status", "TEXT DEFAULT 'Completed'");
            AddColumnIfMissing(conn, "StatementsOfAccount", "SoaType", "TEXT DEFAULT 'Normal'");
            AddColumnIfMissing(conn, "CollectionReceipts", "Status", "TEXT DEFAULT 'Completed'");
            workflowColumnsReady = true;
        }

        private static void AddColumnIfMissing(SqliteConnection conn, string table, string column, string type)
        {
            bool tableExists = false, exists = false;
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

        /// <summary>
        /// Once per database (after a backup): every trip report gets Completed or Invoiced from its invoices, every statement
        /// its type from its lines and Unpaid / Paid from its invoices, and invoices left "Partial" or "Overdue" become Unpaid or Paid.
        /// Every changed row is written to the audit log. Nothing is deleted.
        /// </summary>
        public static void EnsureWorkflowSchema()
        {
            EnsureSpecialTripColumns();
            EnsureReceiptSoaColumn();
            EnsureWorkflowColumns();
            if (GetSystemSetting(WorkflowSchemaKey, "0") == "1") return;

            string backup = Path.Combine(Path.GetDirectoryName(dbFilePath) ?? ".", "Backups", $"before_workflow_schema_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
            if (!BackupDatabase(backup)) throw new InvalidOperationException("Couldn't back up the database before the workflow update: " + LastBackupError);

            var log = new List<(string Table, int Id, string Field, string Old, string New)>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var tx = conn.BeginTransaction();
                try
                {
                    List<(int Id, string A, string B)> Rows(string sql)
                    {
                        var l = new List<(int, string, string)>();
                        using var c = conn.CreateCommand(); c.Transaction = tx; c.CommandText = sql;
                        using var r = c.ExecuteReader();
                        while (r.Read()) l.Add((r.GetInt32(0), Convert.ToString(r.GetValue(1)) ?? "", Convert.ToString(r.GetValue(2)) ?? ""));
                        return l;
                    }
                    void Set(string table, string field, int id, string old, string value)
                    {
                        if (string.Equals(old, value, StringComparison.Ordinal)) return;
                        using var c = conn.CreateCommand(); c.Transaction = tx;
                        c.CommandText = $"UPDATE {table} SET {field} = @v WHERE Id = @id";
                        c.Parameters.AddWithValue("@v", value);
                        c.Parameters.AddWithValue("@id", id);
                        c.ExecuteNonQuery();
                        log.Add((table, id, field, old, value));
                    }

                    // 1. Invoices: no Partial and no stored Overdue. Paid when the receipts cover the net amount, else Unpaid.
                    foreach (var (id, status, settled) in Rows(@"SELECT i.Id, COALESCE(i.Status,''),
                                    CASE WHEN i.NetAmount > 0 AND ROUND((SELECT COALESCE(SUM(r.AmountPaid),0) FROM CollectionReceipts r WHERE r.InvoiceId = i.Id AND COALESCE(r.IsVoid,0) = 0), 2) >= ROUND(i.NetAmount, 2) THEN 1 ELSE 0 END
                                  FROM Invoices i WHERE COALESCE(i.Status,'') NOT IN ('Draft','Unpaid','Paid','Cancelled')"))
                        Set("Invoices", "Status", id, status, settled == "1" ? PayStatus.Paid : PayStatus.Unpaid);

                    // 2. Trip reports: Invoiced when the trip has a service invoice that is not cancelled, else Completed.
                    foreach (var (id, status, invoiced) in Rows(@"SELECT t.Id, COALESCE(t.Status,''),
                                    CASE WHEN EXISTS (SELECT 1 FROM Invoices i WHERE COALESCE(i.Status,'') <> 'Cancelled' COLLATE NOCASE AND (
                                         (COALESCE(t.DeliveryId,0) > 0 AND COALESCE(i.DeliveryId,0) = t.DeliveryId) OR
                                         (COALESCE(t.SpecialTripId,0) > 0 AND COALESCE(i.SpecialTripId,0) = t.SpecialTripId))) THEN 1 ELSE 0 END
                                  FROM TripReports t WHERE COALESCE(t.IsVoid,0) = 0"))
                        Set("TripReports", "Status", id, status, invoiced == "1" ? TripReportStatus.Invoiced : TripReportStatus.Completed);

                    // 3. Statements: their type from their lines.
                    foreach (var (id, type, kinds) in Rows(@"SELECT s.Id, COALESCE(s.SoaType,''),
                                    (CASE WHEN EXISTS (SELECT 1 FROM StatementOfAccountItems x WHERE x.SoaId = s.Id AND COALESCE(x.SpecialTripId,0) > 0) THEN 'S' ELSE '' END) ||
                                    (CASE WHEN EXISTS (SELECT 1 FROM StatementOfAccountItems x WHERE x.SoaId = s.Id AND COALESCE(x.SpecialTripId,0) = 0) THEN 'N' ELSE '' END)
                                  FROM StatementsOfAccount s"))
                        Set("StatementsOfAccount", "SoaType", id, type, kinds == "SN" ? SoaTypes.Mixed : kinds == "S" ? SoaTypes.Special : SoaTypes.Normal);

                    // 4. Statements: Unpaid until every invoice on it is paid. Cancelled ones stay cancelled.
                    foreach (var (id, status, allPaid) in Rows(@"SELECT s.Id, COALESCE(s.Status,''),
                                    CASE WHEN EXISTS (SELECT 1 FROM Invoices i WHERE COALESCE(i.SoaId,0) = s.Id AND COALESCE(i.Status,'') <> 'Cancelled' COLLATE NOCASE)
                                          AND NOT EXISTS (SELECT 1 FROM Invoices i WHERE COALESCE(i.SoaId,0) = s.Id AND COALESCE(i.Status,'') NOT IN ('Paid','Cancelled')) THEN 1 ELSE 0 END
                                  FROM StatementsOfAccount s WHERE COALESCE(s.Status,'') <> 'Cancelled' COLLATE NOCASE"))
                        Set("StatementsOfAccount", "Status", id, status, allPaid == "1" ? PayStatus.Paid : PayStatus.Unpaid);

                    // 5. Receipts: every recorded receipt is a completed (full) payment.
                    foreach (var (id, status, _) in Rows("SELECT Id, COALESCE(Status,''), '' FROM CollectionReceipts WHERE COALESCE(Status,'') <> 'Completed'"))
                        Set("CollectionReceipts", "Status", id, status, "Completed");

                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
            SetSystemSetting(WorkflowSchemaKey, "1", "Nov 6 workflow: trip report status, SOA type and payment statuses were filled in once");
            foreach (var l in log)
                try { LogAuditTrail("SCHEMA", l.Table, l.Id, l.Id.ToString(), l.Field, l.Old, l.New, $"Nov 6 workflow update: {l.Table}.{l.Field} {(l.Old.Length == 0 ? "(blank)" : l.Old)} → {l.New}. Backup: {backup}"); }
                catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
        // ------------------------------------------------------------------ Trip report status follows the trip's service invoice

        /// <summary>The special trip an invoice is linked to (0 when none).</summary>
        private static int SpecialTripIdOfInvoice(SqliteConnection conn, SqliteTransaction? tx, int invoiceId)
        {
            if (invoiceId <= 0) return 0;
            using var c = conn.CreateCommand();
            c.Transaction = tx;
            c.CommandText = "SELECT COALESCE(SpecialTripId,0) FROM Invoices WHERE Id = @i";
            c.Parameters.AddWithValue("@i", invoiceId);
            try { return Convert.ToInt32(c.ExecuteScalar() ?? 0); }
            catch (SqliteException) { return 0; } // very old database without the column
        }

        /// <summary>
        /// Sets the trip's (non-void) trip report to Invoiced while the trip has a service invoice that is not cancelled, and back to
        /// Completed when it has none any more. Draft reports stay Draft until they get an invoice.
        /// </summary>
        private static void SyncTripReportStatus(SqliteConnection conn, SqliteTransaction? tx, int deliveryId, int specialTripId)
        {
            EnsureWorkflowColumns();
            void One(string key, int id)
            {
                if (id <= 0) return;
                bool invoiced;
                using (var c = conn.CreateCommand())
                {
                    c.Transaction = tx;
                    c.CommandText = $"SELECT COUNT(*) FROM Invoices WHERE COALESCE({key},0) = @id AND COALESCE(Status,'') <> 'Cancelled' COLLATE NOCASE";
                    c.Parameters.AddWithValue("@id", id);
                    invoiced = Convert.ToInt32(c.ExecuteScalar() ?? 0) > 0;
                }
                using var u = conn.CreateCommand();
                u.Transaction = tx;
                u.CommandText = invoiced
                    ? $"UPDATE TripReports SET Status = 'Invoiced' WHERE COALESCE({key},0) = @id AND COALESCE(IsVoid,0) = 0 AND COALESCE(Status,'') <> 'Invoiced'"
                    : $"UPDATE TripReports SET Status = 'Completed' WHERE COALESCE({key},0) = @id AND COALESCE(IsVoid,0) = 0 AND COALESCE(Status,'') = 'Invoiced'";
                u.Parameters.AddWithValue("@id", id);
                u.ExecuteNonQuery();
            }
            One("DeliveryId", deliveryId);
            One("SpecialTripId", specialTripId);
        }

        /// <summary>Re-reads an invoice's trip and syncs that trip report (used after the invoice is linked, cancelled or changed elsewhere).</summary>
        private static void SyncTripReportForInvoice(SqliteConnection conn, SqliteTransaction? tx, int invoiceId)
        {
            int deliveryId;
            using (var c = conn.CreateCommand())
            {
                c.Transaction = tx;
                c.CommandText = "SELECT COALESCE(DeliveryId,0) FROM Invoices WHERE Id = @i";
                c.Parameters.AddWithValue("@i", invoiceId);
                deliveryId = Convert.ToInt32(c.ExecuteScalar() ?? 0);
            }
            SyncTripReportStatus(conn, tx, deliveryId, SpecialTripIdOfInvoice(conn, tx, invoiceId));
        }
        /// <summary>Normal, Special or Mixed: what kind of trips a collection target pays (chooses the receipt slip).</summary>
        public static string PayTargetKind(PayTarget t)
        {
            if (t == null || t.Invoices.Count == 0) return SoaTypes.Normal;
            bool special = t.Invoices.Any(i => i.SpecialTripId > 0), normal = t.Invoices.Any(i => i.SpecialTripId <= 0);
            return special && normal ? SoaTypes.Mixed : special ? SoaTypes.Special : SoaTypes.Normal;
        }

        /// <summary>A statement is Paid when every invoice on it is paid, otherwise Unpaid. A cancelled statement stays cancelled.</summary>
        private static void SyncSoaStatus(SqliteConnection conn, SqliteTransaction? tx, int soaId)
        {
            if (soaId <= 0) return;
            using var c = conn.CreateCommand();
            c.Transaction = tx;
            c.CommandText = @"UPDATE StatementsOfAccount SET Status =
                                CASE WHEN EXISTS (SELECT 1 FROM Invoices i WHERE COALESCE(i.SoaId,0) = @s AND COALESCE(i.Status,'') <> 'Cancelled' COLLATE NOCASE)
                                      AND NOT EXISTS (SELECT 1 FROM Invoices i WHERE COALESCE(i.SoaId,0) = @s AND COALESCE(i.Status,'') NOT IN ('Paid','Cancelled'))
                                     THEN 'Paid' ELSE 'Unpaid' END
                              WHERE Id = @s AND COALESCE(Status,'') <> 'Cancelled' COLLATE NOCASE";
            c.Parameters.AddWithValue("@s", soaId);
            c.ExecuteNonQuery();
        }
        /// <summary>
        /// The service invoices a statement of this client and type can take: the client's Unpaid invoices of that kind (normal
        /// dispatches or special trips) that have an amount, nothing collected, and are not on another statement.
        /// </summary>
        public static List<TripInvoice> GetSoaCandidateInvoices(string client, string soaType, int thisSoaId)
        {
            string c = (client ?? "").Trim();
            if (c.Length == 0) return new List<TripInvoice>();
            return GetTripInvoices()
                .Where(i => string.Equals((i.Client ?? "").Trim(), c, StringComparison.OrdinalIgnoreCase)
                            && i.Kind == soaType && i.IsStatus(PayStatus.Unpaid) && i.Net > 0 && i.Collected <= 0
                            && (i.SoaId == 0 || (thisSoaId > 0 && i.SoaId == thisSoaId)))
                .OrderBy(i => i.IssueDate).ThenBy(i => i.Number, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>The statement's type from its lines: Special when every line is a special trip, else Normal (Mixed when both).</summary>
        public static string SoaTypeFromLines(IEnumerable<StatementOfAccountItem> items)
        {
            var l = (items ?? Enumerable.Empty<StatementOfAccountItem>()).Where(i => i.DeliveryId > 0 || i.SpecialTripId > 0).ToList();
            bool special = l.Any(i => i.SpecialTripId > 0), normal = l.Any(i => i.SpecialTripId <= 0);
            return special && normal ? SoaTypes.Mixed : special ? SoaTypes.Special : SoaTypes.Normal;
        }

        /// <summary>True when the dispatch has a trip report, but every (non-void) one is still a Draft.</summary>
        public static bool HasOnlyDraftTripReport(int deliveryId)
        {
            if (deliveryId <= 0) return false;
            EnsureWorkflowColumns();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var c = conn.CreateCommand();
            c.CommandText = @"SELECT COUNT(*), COALESCE(SUM(CASE WHEN COALESCE(Status,'Completed') = 'Draft' THEN 1 ELSE 0 END),0)
                              FROM TripReports WHERE COALESCE(DeliveryId,0) = @d AND COALESCE(IsVoid,0) = 0";
            c.Parameters.AddWithValue("@d", deliveryId);
            using var r = c.ExecuteReader();
            if (!r.Read()) return false;
            long all = r.GetInt64(0), drafts = r.GetInt64(1);
            return all > 0 && drafts == all;
        }
    }
}
