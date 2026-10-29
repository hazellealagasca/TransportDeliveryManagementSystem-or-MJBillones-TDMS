using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Per-user pins and "recently viewed" for Order History. Kind is "Loading order", "Dispatch" or "Special trip";
    /// Number is the record's reference (dispatch #, schedule #, order reference).
    /// </summary>
    public static partial class MJdatabase
    {
        private static bool marksReady;
        public const int RecentLimit = 15;
        private static void EnsureMarksTable(SqliteConnection conn)
        {
            if (marksReady) return;
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS UserRecordMarks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL,
                Mark TEXT NOT NULL,
                Kind TEXT NOT NULL,
                Number TEXT NOT NULL,
                MarkedAt TEXT NOT NULL);
                CREATE UNIQUE INDEX IF NOT EXISTS ux_user_record_marks ON UserRecordMarks(Username, Mark, Kind, Number);";
            cmd.ExecuteNonQuery();
            marksReady = true;
        }
        /// <summary>Records for this user with the given mark ("pin" or "recent"), newest first.</summary>
        public static List<(string Kind, string Number, DateTime At)> GetRecordMarks(string username, string mark)
        {
            var list = new List<(string, string, DateTime)>();
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                EnsureMarksTable(conn);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Kind, Number, MarkedAt FROM UserRecordMarks WHERE Username = @u AND Mark = @m ORDER BY MarkedAt DESC LIMIT 200";
                cmd.Parameters.AddWithValue("@u", (username ?? "").Trim().ToLowerInvariant());
                cmd.Parameters.AddWithValue("@m", mark);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add((r.GetString(0), r.GetString(1), DateTime.TryParse(r.GetString(2), out var t) ? t : DateTime.MinValue));
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine("[Marks] " + ex.Message); ErrLog.Swallowed(ex); }
            return list;
        }
        public static void SetRecordPinned(string username, string kind, string number, bool pinned)
        {
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                EnsureMarksTable(conn);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = pinned
                    ? "INSERT OR REPLACE INTO UserRecordMarks (Username, Mark, Kind, Number, MarkedAt) VALUES (@u, 'pin', @k, @n, @t)"
                    : "DELETE FROM UserRecordMarks WHERE Username = @u AND Mark = 'pin' AND Kind = @k AND Number = @n";
                cmd.Parameters.AddWithValue("@u", (username ?? "").Trim().ToLowerInvariant());
                cmd.Parameters.AddWithValue("@k", kind);
                cmd.Parameters.AddWithValue("@n", number);
                cmd.Parameters.AddWithValue("@t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine("[Marks] " + ex.Message); ErrLog.Swallowed(ex); }
        }
        /// <summary>Puts the record at the top of the user's "recently viewed" list (keeps the latest 15).</summary>
        public static void TouchRecentRecord(string username, string kind, string number)
        {
            try
            {
                string u = (username ?? "").Trim().ToLowerInvariant();
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                EnsureMarksTable(conn);
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT OR REPLACE INTO UserRecordMarks (Username, Mark, Kind, Number, MarkedAt) VALUES (@u, 'recent', @k, @n, @t)";
                    cmd.Parameters.AddWithValue("@u", u);
                    cmd.Parameters.AddWithValue("@k", kind);
                    cmd.Parameters.AddWithValue("@n", number);
                    cmd.Parameters.AddWithValue("@t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
                using (var trim = conn.CreateCommand())
                {
                    trim.CommandText = $"DELETE FROM UserRecordMarks WHERE Username = @u AND Mark = 'recent' AND Id NOT IN (SELECT Id FROM UserRecordMarks WHERE Username = @u AND Mark = 'recent' ORDER BY MarkedAt DESC LIMIT {RecentLimit})";
                    trim.Parameters.AddWithValue("@u", u);
                    trim.ExecuteNonQuery();
                }
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine("[Marks] " + ex.Message); ErrLog.Swallowed(ex); }
        }
    }
}
