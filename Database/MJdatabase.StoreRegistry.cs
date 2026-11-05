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
    /// Store registry used by the store tables: finding which client a store code or name belongs to, and registering a
    /// new store (client store + its place on the route) in one save.
    /// </summary>
    public static partial class MJdatabase
    {
        /// <summary>Longest store code allowed when registering a store.</summary>
        public const int StoreCodeMaxLength = 20;
        /// <summary>Longest store name allowed when registering a store.</summary>
        public const int StoreNameMaxLength = 120;

        /// <summary>Spaces trimmed and runs of spaces made single.</summary>
        public static string CleanStoreText(string? s) => Regex.Replace((s ?? "").Trim(), @"\s+", " ");

        /// <summary>A store code as it is registered: upper case, only letters, digits and hyphens, at most <see cref="StoreCodeMaxLength"/> characters.</summary>
        public static string CleanStoreCode(string? s)
        {
            string c = Regex.Replace(CleanStoreText(s).ToUpperInvariant(), @"[^A-Z0-9\-]", "");
            c = Regex.Replace(c, "-{2,}", "-").Trim('-');
            return c.Length > StoreCodeMaxLength ? c.Substring(0, StoreCodeMaxLength) : c;
        }

        /// <summary>Active (not archived) stores of every client whose code or name equals <paramref name="text"/> (spaces and case ignored).</summary>
        public static List<ClientStore> FindStoresAnyClient(string? text)
        {
            var list = new List<ClientStore>();
            string t = CleanStoreText(text);
            if (t.Length == 0) return list;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, COALESCE(ClientId,0), COALESCE(ClientCompanyName,''), COALESCE(StoreCode,''), COALESCE(StoreName,''), COALESCE(DestinationRoute,'')
                                FROM ClientStores WHERE COALESCE(IsArchived,0) = 0";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var st = new ClientStore { Id = r.GetInt32(0), ClientId = r.GetInt32(1), ClientCompanyName = r.GetString(2), StoreCode = r.GetString(3), StoreName = r.GetString(4), DestinationRoute = r.GetString(5) };
                if (string.Equals(CleanStoreText(st.StoreCode), t, StringComparison.OrdinalIgnoreCase) || string.Equals(CleanStoreText(st.StoreName), t, StringComparison.OrdinalIgnoreCase))
                    list.Add(st);
            }
            return list;
        }

        /// <summary>The store (any client, archived included) that already uses this code, or null.</summary>
        public static ClientStore? FindStoreByCodeAnyClient(string? code, bool includeArchived = true)
        {
            string c = CleanStoreText(code);
            if (c.Length == 0) return null;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Id, COALESCE(ClientId,0), COALESCE(ClientCompanyName,''), COALESCE(StoreCode,''), COALESCE(StoreName,''), COALESCE(DestinationRoute,''), COALESCE(IsArchived,0)
                                FROM ClientStores WHERE UPPER(TRIM(StoreCode)) = UPPER(@c)" + (includeArchived ? "" : " AND COALESCE(IsArchived,0) = 0") + " LIMIT 1";
            cmd.Parameters.AddWithValue("@c", c);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new ClientStore { Id = r.GetInt32(0), ClientId = r.GetInt32(1), ClientCompanyName = r.GetString(2), StoreCode = r.GetString(3), StoreName = r.GetString(4), DestinationRoute = r.GetString(5) };
        }

        /// <summary>
        /// Checks a store about to be registered. Returns the problem to show, or null when it can be saved.
        /// Rules: route, code and name required; the code is unique across all clients; the same name on the same route
        /// of the same client can't be registered twice; the route must be one of the client's routes.
        /// </summary>
        public static string? CheckNewStore(string client, string code, string name, string routeLabel, out DeliveryRoute? route)
        {
            route = null;
            client = CleanStoreText(client);
            string cleanCode = CleanStoreCode(code), cleanName = CleanStoreText(name), label = CleanStoreText(routeLabel);
            if (client.Length == 0) return "Choose the client company first.";
            // Rule 16c: the route is optional; a store can be registered to the client's pool first and put on routes later.
            if (cleanCode.Length == 0) return "Enter the store code (letters, numbers and hyphens).";
            if (cleanName.Length == 0) return "Enter the store name.";
            if (cleanName.Length > StoreNameMaxLength) return $"The store name is too long (at most {StoreNameMaxLength} characters).";
            if (label.Length == 0)
            {
                var codeOwner0 = FindStoreByCodeAnyClient(cleanCode);
                if (codeOwner0 != null) return $"Record Already Exists: store code {cleanCode} belongs to \"{codeOwner0.StoreName}\" of {codeOwner0.ClientCompanyName}.";
                var same0 = GetClientStores(client, includeArchived: true).FirstOrDefault(st => string.Equals(CleanStoreText(st.StoreName), cleanName, StringComparison.OrdinalIgnoreCase));
                if (same0 != null) return $"Record Already Exists: \"{same0.StoreName}\" is already one of {client}'s stores (code {same0.StoreCode}).";
                return null;
            }
            var routes = GetDeliveryRoutesByClient(client, activeOnly: false)
            .Where(r => string.Equals(CleanStoreText(r.ClientCompanyName), client, StringComparison.OrdinalIgnoreCase)).ToList();
            route = routes.FirstOrDefault(r => string.Equals(RouteLabel(r), label, StringComparison.OrdinalIgnoreCase))
                 ?? routes.FirstOrDefault(r => string.Equals(r.RouteCode, label, StringComparison.OrdinalIgnoreCase));
            if (route == null) return $"\"{label}\" is not one of {client}'s routes. Pick a route from the list.";
            int routeId = route.Id;
            var codeOwner = FindStoreByCodeAnyClient(cleanCode);
            if (codeOwner != null)
                return $"Store code {cleanCode} already exists: it belongs to \"{codeOwner.StoreName}\" of {codeOwner.ClientCompanyName}. Each store needs its own code.";
            string rl = RouteLabel(route);
            var sameName = GetClientStores(client, includeArchived: true).FirstOrDefault(st =>
                string.Equals(CleanStoreText(st.StoreName), cleanName, StringComparison.OrdinalIgnoreCase)
                && ResolveRouteFromLabel(client, st.DestinationRoute)?.Id == routeId);
            if (sameName != null)
                return $"\"{sameName.StoreName}\" is already registered on route {rl} for {client} (code {sameName.StoreCode}).";
            return null;
        }

        /// <summary>
        /// Registers a store for a client on one of its routes: adds the client store and puts the store on the route's
        /// store list, in one transaction (both are saved, or neither). Throws with a readable message when a rule fails.
        /// </summary>
        public static ClientStore RegisterClientStore(string client, string code, string name, string routeLabel)
        {
            string? problem = CheckNewStore(client, code, name, routeLabel, out var route);
            if (problem != null) throw new InvalidOperationException(problem);
            if (route == null) return RegisterCompanyStore(client, code, name); // no route chosen: company store pool only (rule 16c)
            client = CleanStoreText(client);
            string cleanCode = CleanStoreCode(code), cleanName = CleanStoreText(name), label = RouteLabel(route);
            int clientId = GetClientByCompanyName(client)?.Id ?? 0;
            var store = new ClientStore { ClientId = clientId, ClientCompanyName = client, StoreCode = cleanCode, StoreName = cleanName, DestinationRoute = label };
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var tx = conn.BeginTransaction();
                try
                {
                    using (var ins = conn.CreateCommand())
                    {
                        ins.Transaction = tx;
                        ins.CommandText = @"INSERT INTO ClientStores (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute, ExtraFieldsJson)
                                            VALUES (@cid, @cl, @sc, @sn, @dr, ''); SELECT last_insert_rowid();";
                        ins.Parameters.AddWithValue("@cid", clientId);
                        ins.Parameters.AddWithValue("@cl", client);
                        ins.Parameters.AddWithValue("@sc", cleanCode);
                        ins.Parameters.AddWithValue("@sn", cleanName);
                        ins.Parameters.AddWithValue("@dr", label);
                        store.Id = Convert.ToInt32(ins.ExecuteScalar() ?? 0);
                    }
                    string current = "";
                    using (var get = conn.CreateCommand())
                    {
                        get.Transaction = tx;
                        get.CommandText = "SELECT COALESCE(ConnectedPlaces,'') FROM DeliveryRoutes WHERE Id = @id";
                        get.Parameters.AddWithValue("@id", route.Id);
                        current = Convert.ToString(get.ExecuteScalar()) ?? "";
                    }
                    var places = current.Split(';').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
                    if (!places.Any(p => SamePlace(p, cleanName)))
                    {
                        places.Add(cleanName);
                        using var upd = conn.CreateCommand();
                        upd.Transaction = tx;
                        upd.CommandText = "UPDATE DeliveryRoutes SET ConnectedPlaces = @p, UpdatedAt = datetime('now') WHERE Id = @id";
                        upd.Parameters.AddWithValue("@p", string.Join("; ", places));
                        upd.Parameters.AddWithValue("@id", route.Id);
                        upd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
                catch
                {
                    try { tx.Rollback(); } catch (Exception rbx) { ErrLog.Swallowed(rbx); }
                    throw;
                }
            }
            try { LogAuditTrail("ADD", "ClientStores", store.Id, $"{cleanCode} - {cleanName}", "DestinationRoute", "", label, $"Registered store {cleanCode} - {cleanName} for {client} on route {label}"); }
            catch (Exception ax) { ErrLog.Swallowed(ax); }
            try { RecomputeStoreRouteLabels(client); } catch (Exception rex) { ErrLog.Swallowed(rex); }
            AppEvents.Raise(AppDataKind.Stores);
            return store;
        }
    }
}
