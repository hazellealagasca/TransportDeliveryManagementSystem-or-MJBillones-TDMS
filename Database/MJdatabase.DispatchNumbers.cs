using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Dispatch numbers are the client's initials plus a running number per client: "Red Ribbon" → RR0001, RR0002…
    /// One-word names use their first three letters ("Jollibee" → JOL0001). Words like "Inc", "Corp", "Bakeshop", "Group" and "the" are skipped,
    /// so "Red Ribbon Bakeshop Inc." → RR0001 and "BCS Group Company" → BCS0001.
    /// </summary>
    public static partial class MJdatabase
    {
        private static readonly string[] SkipWords = { "THE", "OF", "AND", "INC", "CORP", "CORPORATION", "CO", "COMPANY", "LTD", "LLC", "OPC", "ENTERPRISE", "ENTERPRISES", "BRANCH",
            "GROUP", "BAKESHOP", "BAKERY", "BAKERIES", "FOODS", "FOOD", "TRADING", "STORE", "STORES", "MART", "SUPERMARKET", "PHILIPPINES", "PHILS", "PHIL", "SERVICE", "SERVICES", "INTERNATIONAL", "HOLDINGS", "INDUSTRIES" };
        public static string ClientPrefix(string? clientName)
        {
            string name = Regex.Replace((clientName ?? "").ToUpperInvariant(), @"\(.*?\)", " ");
            var words = Regex.Split(name, @"[^A-Z0-9]+").Where(w => w.Length > 0).ToList();
            var useful = words.Where(w => !SkipWords.Contains(w)).ToList();
            if (useful.Count == 0) useful = words;
            if (useful.Count == 0) return "MJ";
            if (useful.Count == 1)
            {
                string w = useful[0];
                string letters = new string(w.Where(char.IsLetter).ToArray());
                string p = (letters.Length > 0 ? letters : w);
                return p.Length <= 3 ? p : p.Substring(0, 3);
            }
            var sb = new StringBuilder();
            foreach (var w in useful) { if (sb.Length >= 4) break; sb.Append(w[0]); }
            return sb.ToString();
        }
        /// <summary>Next free dispatch number for this client, e.g. RR0001. Checks every dispatch ever saved (including cancelled or deleted ones).</summary>
        public static string NextDispatchNumber(string? clientName)
        {
            string prefix = ClientPrefix(clientName);
            int max = 0;
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT DispatchNumber FROM Deliveries WHERE UPPER(DispatchNumber) LIKE @p";
                cmd.Parameters.AddWithValue("@p", prefix + "%");
                using var r = cmd.ExecuteReader();
                var rx = new Regex("^" + Regex.Escape(prefix) + @"-?(\d+)$", RegexOptions.IgnoreCase);
                while (r.Read())
                {
                    if (r.IsDBNull(0)) continue;
                    var m = rx.Match(r.GetString(0).Trim());
                    if (m.Success && int.TryParse(m.Groups[1].Value, out int n) && n > max) max = n;
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[DispatchNo] " + ex.Message); }
            return $"{prefix}{max + 1:D4}";
        }
    }
}
