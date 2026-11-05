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

    }
}
