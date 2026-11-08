using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Rule 16c: a client's stores are one pool (code + name), and any store can be put on any of that same client's
    /// routes. A route's store list (DeliveryRoutes.ConnectedPlaces) is the link; ClientStores.DestinationRoute now only
    /// remembers the first route a store is on (for older screens and the archive), and is recomputed from the routes.
    /// </summary>
    public static partial class MJdatabase
    {
        /// <summary>
        /// True when a route place ("DD-2 - Tarlac", "203077 - RR Laoag" or "RR Laoag") is this store. In order:
        /// (a) the whole text is <see cref="PlaceFor"/> of the store (case and spacing ignored);
        /// (b) when the place and the store both have a code, the code alone decides (no name fallback);
        /// (c) otherwise the name part, or the whole text, equals the store name.
        /// </summary>
        public static bool PlaceIsStore(string place, ClientStore st)
        {
            if (st == null) return false;
            place ??= "";
            if (SamePlace(place, PlaceFor(st))) return true;
            var (code, name) = DeliveryRoute.SplitStoreCodeAndName(place);
            string placeCode = CleanStoreCode(code), storeCode = CleanStoreCode(st.StoreCode);
            if (placeCode.Length > 0 && storeCode.Length > 0) return string.Equals(placeCode, storeCode, StringComparison.OrdinalIgnoreCase);
            return SamePlace(name.Length > 0 ? name : place, st.StoreName) || SamePlace(place, st.StoreName);
        }

        /// <summary>True when the store is on the route: same client, and one of the route's places is the store.</summary>
        public static bool StoreIsOnRoute(DeliveryRoute route, ClientStore st)
        {
            if (route == null || st == null) return false;
            if (!string.Equals(CleanStoreText(route.ClientCompanyName), CleanStoreText(st.ClientCompanyName), StringComparison.OrdinalIgnoreCase)) return false;
            return route.PlacesList.Any(p => PlaceIsStore(p, st));
        }

        /// <summary>How a store is written on a route's store list.</summary>
        public static string PlaceFor(ClientStore st) =>
            string.IsNullOrWhiteSpace(st.StoreCode) ? CleanStoreText(st.StoreName) : $"{CleanStoreCode(st.StoreCode)} - {CleanStoreText(st.StoreName)}";

        private static List<DeliveryRoute> OwnRoutes(string client) =>
            GetDeliveryRoutesByClient(client, activeOnly: false)
                .Where(r => string.Equals(CleanStoreText(r.ClientCompanyName), CleanStoreText(client), StringComparison.OrdinalIgnoreCase)).ToList();

        /// <summary>The client's routes this store is on (any number).</summary>
        public static List<DeliveryRoute> RoutesContainingStore(string client, ClientStore st) =>
            OwnRoutes(client).Where(r => r.PlacesList.Any(p => PlaceIsStore(p, st))).ToList();

        /// <summary>Sets each store's DestinationRoute to the first route it is on ("" when on none).</summary>
        public static void RecomputeStoreRouteLabels(string client)
        {
            client = CleanStoreText(client);
            if (client.Length == 0) return;
            var routes = OwnRoutes(client);
            var stores = GetClientStores(client, includeArchived: true);
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();
            foreach (var st in stores)
            {
                var first = routes.FirstOrDefault(r => r.PlacesList.Any(p => PlaceIsStore(p, st)));
                string label = first == null ? "" : RouteLabel(first);
                if (string.Equals((st.DestinationRoute ?? "").Trim(), label, StringComparison.Ordinal)) continue;
                using var u = conn.CreateCommand();
                u.Transaction = tx;
                u.CommandText = "UPDATE ClientStores SET DestinationRoute = @l WHERE Id = @id";
                u.Parameters.AddWithValue("@l", label);
                u.Parameters.AddWithValue("@id", st.Id);
                u.ExecuteNonQuery();
            }
            tx.Commit();
        }

        /// <summary>Tab 1 quick entry: adds a store to the client's pool (no route needed). Throws with a readable message.</summary>
        public static ClientStore RegisterCompanyStore(string client, string code, string name)
        {
            client = CleanStoreText(client);
            string cleanCode = CleanStoreCode(code), cleanName = CleanStoreText(name);
            if (client.Length == 0) throw new InvalidOperationException("Choose the client company first.");
            if (cleanCode.Length == 0) throw new InvalidOperationException("Enter the store code (letters, numbers and hyphens).");
            if (cleanName.Length == 0) throw new InvalidOperationException("Enter the store name.");
            if (cleanName.Length > StoreNameMaxLength) throw new InvalidOperationException($"The store name is too long (at most {StoreNameMaxLength} characters).");
            var owner = FindStoreByCodeAnyClient(cleanCode);
            if (owner != null) throw new InvalidOperationException($"Record Already Exists: store code {cleanCode} belongs to \"{owner.StoreName}\" of {owner.ClientCompanyName}.");
            var sameName = GetClientStores(client, includeArchived: true).FirstOrDefault(st => string.Equals(CleanStoreText(st.StoreName), cleanName, StringComparison.OrdinalIgnoreCase));
            if (sameName != null) throw new InvalidOperationException($"Record Already Exists: \"{sameName.StoreName}\" is already one of {client}'s stores (code {sameName.StoreCode}).");
            int clientId = GetClientByCompanyName(client)?.Id ?? 0;
            var store = new ClientStore { ClientId = clientId, ClientCompanyName = client, StoreCode = cleanCode, StoreName = cleanName, DestinationRoute = "" };
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var ins = conn.CreateCommand();
                ins.CommandText = @"INSERT INTO ClientStores (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute, ExtraFieldsJson)
                                    VALUES (@cid, @cl, @sc, @sn, '', ''); SELECT last_insert_rowid();";
                ins.Parameters.AddWithValue("@cid", clientId);
                ins.Parameters.AddWithValue("@cl", client);
                ins.Parameters.AddWithValue("@sc", cleanCode);
                ins.Parameters.AddWithValue("@sn", cleanName);
                store.Id = Convert.ToInt32(ins.ExecuteScalar() ?? 0);
            }
            try { LogAuditTrail("ADD", "ClientStores", store.Id, $"{cleanCode} - {cleanName}", null, null, null, $"Added store {cleanCode} - {cleanName} to {client}'s stores"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            AppEvents.Raise(AppDataKind.Stores);
            return store;
        }

        /// <summary>
        /// The one writer of a route's store list (Tab 2): puts a store on a route of the same client, or takes it off.
        /// Idempotent: adding writes one canonical <see cref="PlaceFor"/> only when the store is not on the route yet;
        /// removing deletes every entry <see cref="PlaceIsStore"/> matches. Returns true only when the saved list changed.
        /// </summary>
        public static bool SetStoreOnRoute(DeliveryRoute route, ClientStore st, bool include)
        {
            if (route == null || st == null || route.Id <= 0) return false;
            if (!string.Equals(CleanStoreText(route.ClientCompanyName), CleanStoreText(st.ClientCompanyName), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{st.StoreName} belongs to {st.ClientCompanyName}; route {route.RouteCode} is {route.ClientCompanyName}'s. Only stores of the same client can be on its routes.");
            var fresh = OwnRoutes(route.ClientCompanyName).FirstOrDefault(r => r.Id == route.Id) ?? route;
            var places = fresh.PlacesList;
            bool on = StoreIsOnRoute(fresh, st);
            if (include == on) return false;
            if (include) places.Add(PlaceFor(st));
            else places.RemoveAll(p => PlaceIsStore(p, st));
            WriteRoutePlaces(fresh.Id, places);
            route.ConnectedPlaces = string.Join("; ", places);
            RecomputeStoreRouteLabels(st.ClientCompanyName);
            try { LogAuditTrail("UPDATE", "DeliveryRoutes", fresh.Id, fresh.RouteCode, "ConnectedPlaces", null, null, $"{(include ? "Added" : "Removed")} store {PlaceFor(st)} {(include ? "to" : "from")} route {fresh.RouteCode}"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            AppEvents.Raise(AppDataKind.Routes);
            return true;
        }

        /// <summary>At start-up: every store's first-route label matches the routes (safe to repeat).</summary>
        public static void RecomputeAllStoreRouteLabels()
        {
            var clients = GetDeliveryRoutes(activeOnly: false).Select(r => CleanStoreText(r.ClientCompanyName)).Where(c => c.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var c in clients)
                try { RecomputeStoreRouteLabels(c); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
            // ---- Rule 16d: which store rows of an order came from its route (🔒 locked) and which were added (➕).
        private static bool stopTypesReady;
        private static void EnsureStopTypes(SqliteConnection conn)
        {
            if (stopTypesReady) return;
            using var c = conn.CreateCommand();
            c.CommandText = "CREATE TABLE IF NOT EXISTS DispatchStopTypes (DeliveryId INTEGER NOT NULL, StoreKey TEXT NOT NULL, IsBaselineRouteStore INTEGER NOT NULL DEFAULT 0, PRIMARY KEY (DeliveryId, StoreKey))";
            c.ExecuteNonQuery();
            stopTypesReady = true;
        }
        /// <summary>The key a store row is remembered by: its code, or its name when it has no code.</summary>
        public static string StopKey(string? code, string? name)
        {
            string c = CleanStoreCode(code ?? "");
            return c.Length > 0 ? "C:" + c.ToUpperInvariant() : "N:" + NormPlace(name ?? "");
        }
        /// <summary>Saves the row types of an order (replaces what was there).</summary>
        public static void SetStopTypes(int deliveryId, IEnumerable<(string Code, string Name, bool Baseline)> rows)
        {
            if (deliveryId <= 0) return;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            EnsureStopTypes(conn);
            using var tx = conn.BeginTransaction();
            using (var del = conn.CreateCommand()) { del.Transaction = tx; del.CommandText = "DELETE FROM DispatchStopTypes WHERE DeliveryId = @d"; del.Parameters.AddWithValue("@d", deliveryId); del.ExecuteNonQuery(); }
            foreach (var r in rows)
            {
                string key = StopKey(r.Code, r.Name);
                if (key.Length <= 2) continue;
                using var ins = conn.CreateCommand();
                ins.Transaction = tx;
                ins.CommandText = "INSERT OR REPLACE INTO DispatchStopTypes (DeliveryId, StoreKey, IsBaselineRouteStore) VALUES (@d, @k, @b)";
                ins.Parameters.AddWithValue("@d", deliveryId);
                ins.Parameters.AddWithValue("@k", key);
                ins.Parameters.AddWithValue("@b", r.Baseline ? 1 : 0);
                ins.ExecuteNonQuery();
            }
            tx.Commit();
        }
        /// <summary>Saved row types of an order (store key → is a route store). Empty for orders saved before rule 16.</summary>
        public static Dictionary<string, bool> GetStopTypes(int deliveryId)
        {
            var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (deliveryId <= 0) return map;
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                EnsureStopTypes(conn);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT StoreKey, IsBaselineRouteStore FROM DispatchStopTypes WHERE DeliveryId = @d";
                cmd.Parameters.AddWithValue("@d", deliveryId);
                using var r = cmd.ExecuteReader();
                while (r.Read()) map[r.GetString(0)] = r.GetInt32(1) == 1;
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            return map;
        }
        /// <summary>
        /// Is this row a route store? Uses the saved type; for older orders (nothing saved) it matches the store against the
        /// order's route, as approved on Nov 6, 2026 (on the route = 🔒 route store, otherwise ➕ added).
        /// </summary>
        public static bool IsRouteStoreRow(Dictionary<string, bool> saved, DeliveryRoute? route, string? code, string? name)
        {
            if (saved.TryGetValue(StopKey(code, name), out var b)) return b;
            if (route == null) return false;
            var st = new ClientStore { StoreCode = code ?? "", StoreName = name ?? "" };
            return route.PlacesList.Any(p => PlaceIsStore(p, st));
        }

    }
}
