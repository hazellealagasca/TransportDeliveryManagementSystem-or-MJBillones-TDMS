using System;
using Microsoft.Data.Sqlite;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    public static partial class MJdatabase
    {
        /// <summary>Nov 18: the live (not cancelled / void) statement of account holding this delivery's trip, or null. Read-only.</summary>
        public static (string Number, string Issued)? GetLiveSoaOfDelivery(int deliveryId)
        {
            if (deliveryId <= 0) return null;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var c = conn.CreateCommand();
            c.CommandText = @"SELECT s.SoaNumber, COALESCE(s.IssueDate,'') FROM StatementOfAccountItems i
                              JOIN StatementsOfAccount s ON s.Id = i.SoaId
                              WHERE i.DeliveryId = @d AND LOWER(COALESCE(s.Status,'')) NOT IN ('cancelled','void','voided')
                              ORDER BY s.Id DESC LIMIT 1";
            c.Parameters.AddWithValue("@d", deliveryId);
            using var r = c.ExecuteReader();
            if (!r.Read()) return null;
            return (r.IsDBNull(0) ? "" : r.GetString(0), r.IsDBNull(1) ? "" : r.GetString(1));
        }
    }
}
