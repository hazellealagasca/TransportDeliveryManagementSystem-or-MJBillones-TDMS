using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>The last time a statement or invoice was printed or sent to the client, and the amount it showed.</summary>
    public sealed class IssuedInfo
    {
        public string Kind = "";      // Printed | Sent
        public DateTime At;
        public decimal Amount;
        public string By = "";
    }

    public static partial class MJdatabase
    {
        public const string DocSoa = "SOA", DocInvoice = "Invoice";

        private static void EnsureDocumentIssues(SqliteConnection conn)
        {
            using var c = conn.CreateCommand();
            c.CommandText = @"CREATE TABLE IF NOT EXISTS DocumentIssues (Id INTEGER PRIMARY KEY AUTOINCREMENT, DocType TEXT NOT NULL, DocId INTEGER NOT NULL, DocNumber TEXT,
                              Amount REAL NOT NULL, Kind TEXT NOT NULL, IssuedAt TEXT NOT NULL, IssuedBy TEXT)";
            c.ExecuteNonQuery();
        }

        /// <summary>Writes one print / sent record. Never throws: a log failure must not stop a print.</summary>
        public static void LogDocumentIssue(string docType, int docId, string docNumber, decimal amount, string kind)
        {
            if (docId <= 0) return;
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                EnsureDocumentIssues(conn);
                using var c = conn.CreateCommand();
                c.CommandText = "INSERT INTO DocumentIssues (DocType, DocId, DocNumber, Amount, Kind, IssuedAt, IssuedBy) VALUES (@t,@i,@n,@a,@k,@at,@by)";
                c.Parameters.AddWithValue("@t", docType); c.Parameters.AddWithValue("@i", docId); c.Parameters.AddWithValue("@n", docNumber ?? "");
                c.Parameters.AddWithValue("@a", amount); c.Parameters.AddWithValue("@k", kind);
                c.Parameters.AddWithValue("@at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                c.Parameters.AddWithValue("@by", SessionContext.ActiveUser?.Username ?? SessionContext.CurrentUsername ?? "");
                c.ExecuteNonQuery();
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        /// <summary>Statement printed or sent: every live invoice on it counts as printed or sent too (the statement lists them with their amounts).</summary>
        public static void LogStatementIssued(int soaId, string soaNumber, decimal total, string kind)
        {
            LogDocumentIssue(DocSoa, soaId, soaNumber, total, kind);
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var c = conn.CreateCommand();
                c.CommandText = "SELECT Id, COALESCE(InvoiceNumber,''), COALESCE(NetAmount,0) FROM Invoices WHERE COALESCE(SoaId,0) = @s AND LOWER(COALESCE(Status,'')) NOT IN ('cancelled','void','voided')";
                c.Parameters.AddWithValue("@s", soaId);
                var list = new List<(int, string, decimal)>();
                using (var r = c.ExecuteReader()) while (r.Read()) list.Add((r.GetInt32(0), r.GetString(1), Convert.ToDecimal(r.GetValue(2))));
                foreach (var (id, no, net) in list) LogDocumentIssue(DocInvoice, id, no, net, kind);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        /// <summary>The latest print / sent record of a statement or invoice, or null when it was never printed or sent.</summary>
        public static IssuedInfo? GetIssuedInfo(string docType, int docId)
        {
            if (docId <= 0) return null;
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                EnsureDocumentIssues(conn);
                using var c = conn.CreateCommand();
                c.CommandText = "SELECT Kind, IssuedAt, Amount, COALESCE(IssuedBy,'') FROM DocumentIssues WHERE DocType = @t AND DocId = @i ORDER BY Id DESC LIMIT 1";
                c.Parameters.AddWithValue("@t", docType); c.Parameters.AddWithValue("@i", docId);
                using var r = c.ExecuteReader();
                if (!r.Read()) return null;
                return new IssuedInfo { Kind = r.GetString(0), At = DateTime.TryParse(r.GetString(1), out var at) ? at : DateTime.MinValue, Amount = Convert.ToDecimal(r.GetValue(2)), By = r.GetString(3) };
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); return null; }
        }

        /// <summary>The document was printed or sent and its amount has changed since: the client's copy is out of date.</summary>
        public static IssuedInfo? NeedsReissue(string docType, int docId, decimal currentAmount)
        {
            var i = GetIssuedInfo(docType, docId);
            return i != null && Math.Abs(i.Amount - currentAmount) > 0.004m ? i : null;
        }
    }
}
