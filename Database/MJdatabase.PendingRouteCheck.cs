using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    public static partial class MJdatabase
    {
        /// <summary>
        /// Nov 10: the checked entry of a route on a client that is not saved yet (New client). A route code is used by ONE route only, in the
        /// list being typed and in every saved client (the database enforces it), and a client's route names must differ. Said up front, in plain
        /// words, so nothing is left to fail when Create client is clicked. <paramref name="partial"/> checks only what is typed so far.
        /// </summary>
        public static string? CheckPendingRoute(string client, string code, string name, IEnumerable<(string Code, string Name)> alreadyInList, bool partial = false)
        {
            string cleanCode = CleanStoreCode(code), cleanName = CleanStoreText(name);
            if (cleanCode.Length == 0 && !(partial && CleanStoreText(code).Length == 0)) return "Enter the route code (letters, numbers and hyphens).";
            if (cleanName.Length == 0 && !partial) return "Enter the route name.";
            var list = (alreadyInList ?? Enumerable.Empty<(string Code, string Name)>()).ToList();
            if (cleanCode.Length > 0)
            {
                var same = list.FirstOrDefault(r => string.Equals(CleanStoreCode(r.Code), cleanCode, StringComparison.OrdinalIgnoreCase));
                if (same.Code != null) return $"Route code '{cleanCode}' is already used by '{same.Name}' in this list. Each route needs its own code.";
            }
            if (cleanName.Length > 0)
            {
                var same = list.FirstOrDefault(r => string.Equals(CleanStoreText(r.Name), cleanName, StringComparison.OrdinalIgnoreCase));
                if (same.Code != null) return $"This client already has a route named '{same.Name}' (code {same.Code}) in this list. Use a different route name.";
            }
            if (cleanCode.Length > 0)
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var c = conn.CreateCommand();
                c.CommandText = "SELECT COALESCE(ClientCompanyName, ''), COALESCE(RouteName, '') FROM DeliveryRoutes WHERE LOWER(TRIM(RouteCode)) = LOWER(@c) LIMIT 1";
                c.Parameters.AddWithValue("@c", cleanCode);
                using var r = c.ExecuteReader();
                if (r.Read())
                {
                    string owner = r.GetString(0).Trim(), existing = r.GetString(1);
                    return $"Route code '{cleanCode}' is already used by '{existing}'{(owner.Length > 0 ? $" ({owner})" : "")}. Please use a different route code.";
                }
            }
            return null;
        }

        /// <summary>A database "already exists" error in plain words (never the raw SQLite text).</summary>
        public static string PlainDbError(Exception ex)
        {
            string m = ex?.Message ?? "";
            if (m.Contains("ux_routes_name_client", StringComparison.OrdinalIgnoreCase)) return "this client already has a route with that name. Use a different route name.";
            if (m.Contains("ux_routes_code", StringComparison.OrdinalIgnoreCase)) return "that route code is already used by another route. Use a different route code.";
            if (m.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase)) return "it duplicates something that already exists.";
            return m;
        }
    }
}
