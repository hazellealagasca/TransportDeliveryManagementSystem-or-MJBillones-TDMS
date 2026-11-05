using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Delivery receipts across trips and statements: which dispatches have a saved trip report (only those can be
    /// billed), DR numbers already used on other trips, and the store code kept on each statement line.
    /// </summary>
    public static partial class MJdatabase
    {
        /// <summary>
        /// Adds the StoreCode, TripReportNumber and TotalCrates columns to statement lines (older databases don't have them;
        /// old lines stay blank / 0). Checked every time (a cheap query), so a backup restored while the app is open still gets them.
        /// </summary>
        public static void EnsureSoaStoreCodeColumn()
        {
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var chk = conn.CreateCommand())
                {
                    chk.CommandText = "PRAGMA table_info(StatementOfAccountItems)";
                    using var r = chk.ExecuteReader();
                    while (r.Read()) have.Add(r.GetString(1));
                }
                foreach (var (col, ddl) in new[] { ("StoreCode", "TEXT DEFAULT ''"), ("TripReportNumber", "TEXT DEFAULT ''"), ("TotalCrates", "INTEGER DEFAULT 0") })
                {
                    if (have.Contains(col)) continue;
                    using var alt = conn.CreateCommand();
                    alt.CommandText = $"ALTER TABLE StatementOfAccountItems ADD COLUMN {col} {ddl}";
                    alt.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }




        /// <summary>Saved (not voided) trip reports by dispatch id: the newest report of each dispatch.</summary>
        public static Dictionary<int, TripReport> GetTripReportsByDelivery()
        {
            var map = new Dictionary<int, TripReport>();
            try
            {
                var dels = GetDeliveries();
                var byNumber = dels.Where(d => !string.IsNullOrWhiteSpace(d.DispatchNumber)).GroupBy(d => d.DispatchNumber.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
                foreach (var tr in GetTripReports().OrderBy(t => t.Id))
                {
                    int id = tr.DeliveryId > 0 ? tr.DeliveryId : (byNumber.TryGetValue((tr.DispatchNumber ?? "").Trim(), out int n) ? n : 0);
                    if (id > 0) map[id] = tr; // ordered by Id, so the newest report of a dispatch wins
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return map;
        }

        /// <summary>Ids of dispatches that have a saved (not voided) trip report.</summary>
        public static HashSet<int> GetTripReportDeliveryIds()
        {
            var set = new HashSet<int>();
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT DISTINCT d.Id FROM Deliveries d
                                    JOIN TripReports t ON (t.DeliveryId = d.Id OR (COALESCE(t.DeliveryId, 0) = 0 AND t.DispatchNumber = d.DispatchNumber))
                                    WHERE COALESCE(t.IsVoid, 0) = 0";
                using var r = cmd.ExecuteReader();
                while (r.Read()) set.Add(r.GetInt32(0));
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return set;
        }




        public static bool HasTripReport(int deliveryId) => deliveryId > 0 && GetTripReportDeliveryIds().Contains(deliveryId);




        /// <summary>
        /// DR numbers from <paramref name="drs"/> that are already on another (not cancelled) dispatch, as
        /// "DR-1001 is on BCS0002 (RR-Vigan)".
        /// </summary>
        public static List<string> FindDrNumbersOnOtherTrips(int deliveryId, IEnumerable<string> drs) =>
            FindDrOwnersOnOtherTrips(deliveryId, drs).Select(kv => $"{kv.Key} is on {kv.Value}").ToList();


        /// <summary>For each DR in <paramref name="drs"/> already on another (not cancelled) dispatch: where, e.g. "BCS0002 (204526 RR-Vigan)".</summary>
        public static Dictionary<string, string> FindDrOwnersOnOtherTrips(int deliveryId, IEnumerable<string> drs)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var wanted = new HashSet<string>(drs.Where(d => !string.IsNullOrWhiteSpace(d)).Select(d => d.Trim()), StringComparer.OrdinalIgnoreCase);
            if (wanted.Count == 0) return result;
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT COALESCE(ds.DrNumbers, ''), COALESCE(ds.StoreCode, ''), COALESCE(ds.Destination, ''), COALESCE(d.DispatchNumber, '')
                                    FROM DispatchStops ds JOIN Deliveries d ON d.Id = ds.DeliveryId
                                    WHERE ds.DeliveryId <> @id AND COALESCE(ds.DrNumbers, '') <> '' AND COALESCE(d.Status, '') <> 'Cancelled'
                                    ORDER BY d.Id, ds.StopSequence";
                cmd.Parameters.AddWithValue("@id", deliveryId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    string store = (r.GetString(1) + " " + r.GetString(2)).Trim();
                    foreach (var dr in DrList.Parse(r.GetString(0)))
                        if (wanted.Contains(dr) && !result.ContainsKey(dr))
                            result[dr] = $"{r.GetString(3)}{(store.Length > 0 ? $" ({store})" : "")}";
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return result;
        }




        /// <summary>
        /// For each DR in <paramref name="drs"/> already billed on another (not cancelled) statement: where, e.g.
        /// "SOA-2026-0002 (RR-Vigan)". <paramref name="soaId"/> is the statement being edited (0 for a new one).
        /// </summary>
        public static Dictionary<string, string> FindDrOwnersOnOtherStatements(int soaId, IEnumerable<string> drs)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var wanted = new HashSet<string>(drs.Where(d => !string.IsNullOrWhiteSpace(d)).Select(d => d.Trim()), StringComparer.OrdinalIgnoreCase);
            if (wanted.Count == 0) return result;
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT COALESCE(i.DrNumbers, ''), COALESCE(i.Destination, ''), COALESCE(s.SoaNumber, '')
                                    FROM StatementOfAccountItems i JOIN StatementsOfAccount s ON s.Id = i.SoaId
                                    WHERE i.SoaId <> @id AND COALESCE(i.DrNumbers, '') <> '' AND COALESCE(s.Status, '') <> 'Cancelled'
                                    ORDER BY s.Id, i.Id";
                cmd.Parameters.AddWithValue("@id", soaId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    string dest = r.GetString(1).Trim();
                    foreach (var dr in DrList.Parse(r.GetString(0)))
                        if (wanted.Contains(dr) && !result.ContainsKey(dr))
                            result[dr] = $"{r.GetString(2)}{(dest.Length > 0 ? $" ({dest})" : "")}";
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return result;
        }


        /// <summary>Drops entered for a store on its trip report (No. Drops); 1 when none was entered.</summary>
        public static int DropsOf(DispatchStop s)
        {
            string r = (s?.Remarks ?? "").Trim();
            return Regex.IsMatch(r, @"^\d{1,3}$") && int.TryParse(r, out int n) && n > 0 ? n : 1;
        }
    }
}
