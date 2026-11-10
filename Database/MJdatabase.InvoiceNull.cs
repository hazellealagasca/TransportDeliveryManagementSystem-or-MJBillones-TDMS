using System;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    public static partial class MJdatabase
    {
        /// <summary>
        /// True when an Invoices column is declared NOT NULL (older databases declare DeliveryId and SoaId that way; newer ones allow NULL).
        /// An invoice that has no dispatch must then store 0, not NULL, or the save fails with "NOT NULL constraint failed: Invoices.DeliveryId".
        /// </summary>
        private static bool InvoiceColumnNotNull(SqliteConnection conn, SqliteTransaction? tx, string column)
        {
            try
            {
                using var c = conn.CreateCommand();
                c.Transaction = tx;
                c.CommandText = "PRAGMA table_info(Invoices)";
                using var r = c.ExecuteReader();
                while (r.Read())
                    if (string.Equals(r.GetString(1), column, StringComparison.OrdinalIgnoreCase)) return r.GetInt32(3) == 1;
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return false;
        }
    }
}
