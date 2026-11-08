using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Archive for Master Data: clients, stores and delivery routes are archived instead of deleted, so every
    /// past order, trip report and invoice that mentions them stays readable. Archived records leave the pick-lists
    /// and can be restored from Master Data → Archive.
    /// </summary>
    public static partial class MJdatabase
    {
        /// <summary>One archived record for the Archive tab.</summary>
        public sealed class ArchivedItem
        {
            public string Kind { get; init; } = "";
            public int Id { get; init; }
            public string Code { get; init; } = "";
            public string Name { get; init; } = "";
            public string Client { get; init; } = "";
            public string ArchivedOn { get; init; } = "";
        }



        public static List<ArchivedItem> GetArchivedItems()
        {
            var list = new List<ArchivedItem>();
            try
            {
                foreach (var c in GetClients().Where(c => !c.IsActive))
                    list.Add(new ArchivedItem { Kind = "Client", Id = c.Id, Code = "", Name = c.CompanyName, Client = c.CompanyName, ArchivedOn = c.DeactivatedAt.HasValue ? c.DeactivatedAt.Value.ToString("MMM d, yyyy") : "" });
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, COALESCE(StoreCode,''), COALESCE(StoreName,''), COALESCE(ClientCompanyName,''), COALESCE(ArchivedAt,'') FROM ClientStores WHERE COALESCE(IsArchived,0) = 1 ORDER BY ClientCompanyName, StoreName";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new ArchivedItem { Kind = "Store", Id = r.GetInt32(0), Code = r.GetString(1), Name = r.GetString(2), Client = r.GetString(3), ArchivedOn = FormatArchivedOn(r.GetString(4)) });
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            try
            {
                foreach (var rt in GetDeliveryRoutes(activeOnly: false).Where(x => !x.IsActive))
                    list.Add(new ArchivedItem { Kind = "Route", Id = rt.Id, Code = rt.RouteCode, Name = rt.RouteName, Client = rt.ClientCompanyName, ArchivedOn = rt.UpdatedAt.ToString("MMM d, yyyy") });
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return list;
        }



        private static string FormatArchivedOn(string raw) => DateTime.TryParse(raw, out var d) ? d.ToString("MMM d, yyyy") : "";



        /// <summary>Archives a store: it leaves the client's store list, its route and the dispatch pick-list. History keeps its name.</summary>
        public static void ArchiveClientStore(int storeId)
        {
            if (storeId <= 0) return;
            var old = GetClientStoreById(storeId);
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE ClientStores SET IsArchived = 1, ArchivedAt = @at WHERE Id = @id";
                cmd.Parameters.AddWithValue("@at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@id", storeId);
                cmd.ExecuteNonQuery();
            }
            try
            {
                if (old != null)
                {
                    var r = ResolveRouteFromLabel(old.ClientCompanyName, old.DestinationRoute);
                    if (r != null) RemoveRoutePlace(r, old);
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            LogAuditTrail("ARCHIVE", "ClientStores", storeId, old == null ? $"#{storeId}" : $"{old.StoreCode} - {old.StoreName}".Trim(' ', '-'), "IsArchived", "0", "1", "Store archived (history is kept)");
        }



        /// <summary>Brings an archived store back, and puts it back on its route if that route still exists.</summary>
        public static void RestoreClientStore(int storeId)
        {
            if (storeId <= 0) return;
            var old = GetClientStoreById(storeId);
            if (old != null && !string.IsNullOrWhiteSpace(old.StoreCode) && IsStoreCodeTaken(old.ClientCompanyName, old.StoreCode, storeId))
            {
                // Another (active) store took the code meanwhile: only block when that store is not archived itself.
                using var c0 = new SqliteConnection(connectionString);
                c0.Open();
                using var q = c0.CreateCommand();
                q.CommandText = "SELECT COUNT(*) FROM ClientStores WHERE LOWER(TRIM(ClientCompanyName)) = LOWER(TRIM(@c)) AND LOWER(TRIM(StoreCode)) = LOWER(TRIM(@s)) AND Id <> @id AND COALESCE(IsArchived,0) = 0";
                q.Parameters.AddWithValue("@c", old.ClientCompanyName);
                q.Parameters.AddWithValue("@s", old.StoreCode.Trim());
                q.Parameters.AddWithValue("@id", storeId);
                if (Convert.ToInt64(q.ExecuteScalar() ?? 0) > 0)
                    throw new InvalidOperationException($"Store code \"{old.StoreCode}\" is now used by another store of {old.ClientCompanyName}. Change that store's code first, then restore this one.");
            }
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE ClientStores SET IsArchived = 0, ArchivedAt = '' WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", storeId);
                cmd.ExecuteNonQuery();
            }
            try
            {
                if (old != null)
                {
                    var r = ResolveRouteFromLabel(old.ClientCompanyName, old.DestinationRoute);
                    if (r != null) SetStoreOnRoute(r, old, true);
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            LogAuditTrail("RESTORE", "ClientStores", storeId, old == null ? $"#{storeId}" : $"{old.StoreCode} - {old.StoreName}".Trim(' ', '-'), "IsArchived", "1", "0", "Store restored from the archive");
        }



        public static void ArchiveDeliveryRoute(int routeId, string label)
        {
            if (ToggleDeliveryRouteActive(routeId, false))
                LogAuditTrail("ARCHIVE", "DeliveryRoutes", routeId, label, "IsActive", "1", "0", "Route archived (history is kept)");
        }



        public static void RestoreDeliveryRoute(int routeId, string label)
        {
            if (ToggleDeliveryRouteActive(routeId, true))
                LogAuditTrail("RESTORE", "DeliveryRoutes", routeId, label, "IsActive", "0", "1", "Route restored from the archive");
        }


        /// <summary>
        /// One-time clean-up of store codes the system made up earlier ("R1-S01", "S22"). When the store name starts
        /// with its real code ("203077 - RR-Laoag Bacarra") the code moves out of the name; otherwise the code is left
        /// blank for the user to fill in. Route store lists are renamed in step so routes keep matching their stores.
        /// Past orders keep the text they were saved with.
        /// </summary>
        public static void MigrateAutoStoreCodes()
        {
            if (string.Equals(GetSystemSetting("Fix.AutoStoreCodes.v1", ""), "done", StringComparison.OrdinalIgnoreCase)) return;
            var auto = new System.Text.RegularExpressions.Regex(@"^([A-Za-z0-9]+-)?S\d{2,}$");
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();
            var stores = new List<(int Id, string Client, string Code, string Name, bool Archived)>();
            using (var q = conn.CreateCommand())
            {
                q.Transaction = tx;
                q.CommandText = "SELECT Id, COALESCE(ClientCompanyName,''), COALESCE(StoreCode,''), COALESCE(StoreName,''), COALESCE(IsArchived,0) FROM ClientStores";
                using var r = q.ExecuteReader();
                while (r.Read()) stores.Add((r.GetInt32(0), r.GetString(1), r.GetString(2).Trim(), r.GetString(3).Trim(), r.GetInt32(4) == 1));
            }
            // Each store as it is now (updated as the loop cleans codes), for the "no other active store" test of the rename cascade.
            var current = stores.ToDictionary(x => x.Id, x => new ClientStore { Id = x.Id, ClientCompanyName = x.Client, StoreCode = x.Code, StoreName = x.Name });
            var routes = new List<(int Id, string Client, string Places)>();
            using (var q = conn.CreateCommand())
            {
                q.Transaction = tx;
                q.CommandText = "SELECT Id, COALESCE(ClientCompanyName,''), COALESCE(ConnectedPlaces,'') FROM DeliveryRoutes";
                using var r = q.ExecuteReader();
                while (r.Read()) routes.Add((r.GetInt32(0), r.GetString(1), r.GetString(2)));
            }
            void Exec(string sql, params (string, object)[] ps)
            {
                using var c = conn.CreateCommand();
                c.Transaction = tx;
                c.CommandText = sql;
                foreach (var (k, v) in ps) c.Parameters.AddWithValue(k, v);
                c.ExecuteNonQuery();
            }
            var taken = stores.Where(x => !auto.IsMatch(x.Code) && x.Code.Length > 0).Select(x => (x.Client.ToUpperInvariant(), x.Code.ToUpperInvariant())).ToHashSet();
            int changed = 0;
            foreach (var st in stores.Where(x => auto.IsMatch(x.Code)))
            {
                var (code, name) = DeliveryRoute.SplitStoreCodeAndName(st.Name);
                string newCode = "", newName = st.Name;
                if (code.Length > 0 && name.Length > 0 && !taken.Contains((st.Client.ToUpperInvariant(), code.ToUpperInvariant())))
                {
                    newCode = code;
                    newName = name;
                    taken.Add((st.Client.ToUpperInvariant(), code.ToUpperInvariant()));
                }
                Exec("UPDATE ClientStores SET StoreCode = @c, StoreName = @n WHERE Id = @id", ("@c", newCode), ("@n", newName), ("@id", st.Id));
                changed++;
                var before = new ClientStore { Id = st.Id, ClientCompanyName = st.Client, StoreCode = st.Code, StoreName = st.Name };
                var after = new ClientStore { Id = st.Id, ClientCompanyName = st.Client, StoreCode = newCode, StoreName = newName };
                current[st.Id] = after;
                var others = stores.Where(x => x.Id != st.Id && !x.Archived && string.Equals(x.Client.Trim(), st.Client.Trim(), StringComparison.OrdinalIgnoreCase))
                                   .Select(x => current[x.Id]).ToList();
                for (int i = 0; i < routes.Count; i++)
                {
                    var rt = routes[i];
                    if (!string.Equals(rt.Client.Trim(), st.Client.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                    var parts = rt.Places.Split(new[] { ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                    var renamed = RenamedPlaces(parts, before, after, others); // this store's own entries, now as "code - name"
                    if (renamed.SequenceEqual(parts)) continue;
                    WriteRoutePlaces(rt.Id, renamed, conn, tx);
                    routes[i] = (rt.Id, rt.Client, string.Join("; ", renamed));
                }
            }
            Exec("INSERT OR REPLACE INTO SystemSettings (SettingKey, SettingValue, Description, UpdatedAt) VALUES ('Fix.AutoStoreCodes.v1', 'done', 'Removed made-up store codes', datetime('now'))");
            tx.Commit();
            if (changed > 0) LogAuditTrail("UPDATE", "ClientStores", 0, $"{changed} store(s)", "StoreCode", "auto code", "real / blank", "Made-up store codes removed (real codes taken from the store names)");
        }
    }
}
