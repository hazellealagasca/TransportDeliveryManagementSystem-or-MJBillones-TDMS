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

        /// <summary>
        /// The message to show when this store cannot be added to the client's stores, or null when it can. The one check behind
        /// <see cref="RegisterCompanyStore"/>, the live check under the boxes and the paste. With <paramref name="partial"/> (typing in progress)
        /// a box that is still empty is not reported; only a code or name that is already taken is.
        /// </summary>
        public static string? CheckCompanyStore(string client, string code, string name, bool partial = false)
        {
            client = CleanStoreText(client);
            string cleanCode = CleanStoreCode(code), cleanName = CleanStoreText(name);
            if (client.Length == 0) return "Choose the client company first.";
            if (cleanCode.Length == 0 && !(partial && CleanStoreText(code).Length == 0)) return "Enter the store code (letters, numbers and hyphens).";
            if (cleanName.Length == 0 && !partial) return "Enter the store name.";
            if (cleanName.Length > StoreNameMaxLength) return $"The store name is too long (at most {StoreNameMaxLength} characters).";
            if (cleanCode.Length > 0)
            {
                var owner = FindStoreByCodeAnyClient(cleanCode);
                if (owner != null) return $"Record Already Exists: store code {cleanCode} belongs to \"{owner.StoreName}\" of {owner.ClientCompanyName}.";
            }
            if (cleanName.Length > 0)
            {
                var sameName = GetClientStores(client, includeArchived: true).FirstOrDefault(st => string.Equals(CleanStoreText(st.StoreName), cleanName, StringComparison.OrdinalIgnoreCase));
                if (sameName != null) return $"Record Already Exists: \"{sameName.StoreName}\" is already one of {client}'s stores (code {sameName.StoreCode}).";
            }
            return null;
        }

        /// <summary>
        /// The message to show when this route cannot be created for the client, or null when it can. A route code is unique across all
        /// clients (archived routes too) and a route name is unique per client. The text for another client's code is the one
        /// <c>SaveDeliveryRoute</c> throws. With <paramref name="partial"/> (typing in progress) an empty box is not reported.
        /// </summary>
        public static string? CheckRouteCode(string client, string code, string name, bool partial = false)
        {
            client = CleanStoreText(client);
            string cleanCode = CleanStoreCode(code), cleanName = CleanStoreText(name);
            if (client.Length == 0) return "Save the client first.";
            if (cleanCode.Length == 0 && !(partial && CleanStoreText(code).Length == 0)) return "Enter the route code (letters, numbers and hyphens).";
            if (cleanName.Length == 0 && !partial) return "Enter the route name.";
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            if (cleanCode.Length > 0)
            {
                using var byCode = conn.CreateCommand();
                byCode.CommandText = "SELECT COALESCE(ClientCompanyName, ''), COALESCE(RouteName, ''), IsActive FROM DeliveryRoutes WHERE LOWER(TRIM(RouteCode)) = LOWER(@c) LIMIT 1";
                byCode.Parameters.AddWithValue("@c", cleanCode);
                using var r = byCode.ExecuteReader();
                if (r.Read())
                {
                    string owner = r.GetString(0).Trim(), existingName = r.GetString(1);
                    if (owner.Length > 0 && !string.Equals(owner, client, StringComparison.OrdinalIgnoreCase))
                        return $"Route code '{cleanCode}' is already used by '{existingName}' ({owner}). Please use a different route code.";
                    return $"Route code '{cleanCode}' is already one of {client}'s routes ('{existingName}'{(r.GetInt32(2) == 0 ? ", archived" : "")}). Use a different route code.";
                }
            }
            if (cleanName.Length > 0)
            {
                using var byName = conn.CreateCommand();
                byName.CommandText = "SELECT RouteCode, RouteName FROM DeliveryRoutes WHERE LOWER(TRIM(COALESCE(ClientCompanyName, ''))) = LOWER(@cl) AND LOWER(TRIM(RouteName)) = LOWER(@n) LIMIT 1";
                byName.Parameters.AddWithValue("@cl", client);
                byName.Parameters.AddWithValue("@n", cleanName);
                using var r = byName.ExecuteReader();
                if (r.Read()) return $"{client} already has a route named '{r.GetString(1)}' (code {r.GetString(0)}). Use a different route name.";
            }
            return null;
        }

        /// <summary>Tab 1 quick entry: adds a store to the client's pool (no route needed). Throws with a readable message.</summary>
        public static ClientStore RegisterCompanyStore(string client, string code, string name)
        {
            string? problem = CheckCompanyStore(client, code, name);
            if (problem != null) throw new InvalidOperationException(problem);
            client = CleanStoreText(client);
            string cleanCode = CleanStoreCode(code), cleanName = CleanStoreText(name);
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

        private static string OneRouteKey(string client) => "Routes.OneRoutePerStore." + CleanStoreText(client).ToUpperInvariant();

        /// <summary>Whether the client allows a store on only one route. Off when never set.</summary>
        public static bool GetOneRoutePerStore(string client) => GetSystemSetting(OneRouteKey(client), "") == "1";

        /// <summary>A store of the client that is on two or more of its active routes, with those route codes.</summary>
        public sealed record MultiRouteStore(ClientStore Store, List<string> RouteCodes);

        /// <summary>The client's stores that are on two or more of its active routes (what blocks turning the rule on).</summary>
        public static List<MultiRouteStore> StoresOnSeveralRoutes(string client)
        {
            var routes = OwnRoutes(client).Where(r => r.IsActive).ToList();
            var result = new List<MultiRouteStore>();
            foreach (var st in GetClientStores(client))
            {
                var codes = routes.Where(r => StoreIsOnRoute(r, st)).Select(r => r.RouteCode).ToList();
                if (codes.Count >= 2) result.Add(new MultiRouteStore(st, codes));
            }
            return result;
        }

        /// <summary>
        /// Turns the client's one-route-per-store rule on or off. Off always succeeds. On is refused while a store is on
        /// two or more routes: returns the message naming them (nothing is saved). Returns null when the setting was saved.
        /// </summary>
        public static string? SetOneRoutePerStore(string client, bool on)
        {
            if (on)
            {
                var multi = StoresOnSeveralRoutes(client);
                if (multi.Count > 0)
                {
                    const int shown = 5; // the panel label is 2 to 3 lines; the grid marks every such store "Also on ..."
                    string list = string.Join("; ", multi.Take(shown).Select(m => $"{StoreLabel(m.Store)} ({string.Join(", ", m.RouteCodes)})"));
                    if (multi.Count > shown) list += $" and {multi.Count - shown} more";
                    return "Cannot turn this on yet. Take these stores off all but one route first: " + list + ". They are marked 'Also on ...' in the store list.";
                }
            }
            SetSystemSetting(OneRouteKey(client), on ? "1" : "0", "Optional rule: a store may be on only one route of this client");
            return null;
        }

        private static string StoreLabel(ClientStore st) => string.IsNullOrWhiteSpace(st.StoreCode) ? CleanStoreText(st.StoreName) : CleanStoreCode(st.StoreCode);

        /// <summary>
        /// Why adding the store to the route is refused by the client's one-route rule; null when the add is allowed
        /// (rule off, or the store is on no other active route of the client).
        /// </summary>
        public static string? OneRouteRefusal(DeliveryRoute route, ClientStore st)
        {
            if (route == null || st == null || !GetOneRoutePerStore(route.ClientCompanyName)) return null;
            var other = OwnRoutes(route.ClientCompanyName).FirstOrDefault(r => r.IsActive && r.Id != route.Id && StoreIsOnRoute(r, st));
            return other == null ? null : $"{StoreLabel(st)} is already on route {other.RouteCode}. Take it off there first.";
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
            if (include && OneRouteRefusal(fresh, st) != null) return false; // the client's optional one-route rule
            if (include) places.Add(PlaceFor(st));
            else places.RemoveAll(p => PlaceIsStore(p, st));
            WriteRoutePlaces(fresh.Id, places);
            route.ConnectedPlaces = string.Join("; ", places);
            RecomputeStoreRouteLabels(st.ClientCompanyName);
            try { LogAuditTrail("UPDATE", "DeliveryRoutes", fresh.Id, fresh.RouteCode, "ConnectedPlaces", null, null, $"{(include ? "Added" : "Removed")} store {PlaceFor(st)} {(include ? "to" : "from")} route {fresh.RouteCode}"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            AppEvents.Raise(AppDataKind.Routes);
            return true;
        }

        /// <summary>
        /// For a store being registered (not saved yet): why it cannot go on the route, or null with <paramref name="places"/> set to
        /// the route's saved list plus the store's canonical <see cref="PlaceFor"/> entry. The rules of <see cref="SetStoreOnRoute"/>:
        /// same client, no entry on the route is the store already, and the client's one-route rule.
        /// </summary>
        private static string? PlacesWithNewStore(DeliveryRoute route, ClientStore st, out List<string> places)
        {
            var fresh = OwnRoutes(route.ClientCompanyName).FirstOrDefault(r => r.Id == route.Id) ?? route;
            places = fresh.PlacesList;
            if (!string.Equals(CleanStoreText(route.ClientCompanyName), CleanStoreText(st.ClientCompanyName), StringComparison.OrdinalIgnoreCase))
                return $"{st.StoreName} belongs to {st.ClientCompanyName}; route {route.RouteCode} is {route.ClientCompanyName}'s. Only stores of the same client can be on its routes.";
            string? entry = places.FirstOrDefault(p => PlaceIsStore(p, st));
            if (entry != null)
                return $"Route {fresh.RouteCode} already has the entry \"{entry}\", which would be this store. Take that entry off the route first, or register the store under another name.";
            string? refusal = OneRouteRefusal(fresh, st);
            if (refusal != null) return refusal;
            places.Add(PlaceFor(st));
            return null;
        }

        /// <summary>
        /// Moves a store one place earlier (delta -1) or later (+1) in a route's stop order. Only reorders the saved list:
        /// no entry is added or removed. A store with several entries moves its first one. Returns true only when the saved list changed
        /// (first store up, last store down, a store not on the route, or any other delta returns false).
        /// </summary>
        public static bool MoveStoreOnRoute(DeliveryRoute route, ClientStore st, int delta)
        {
            if (route == null || st == null || route.Id <= 0 || (delta != -1 && delta != 1)) return false;
            if (!string.Equals(CleanStoreText(route.ClientCompanyName), CleanStoreText(st.ClientCompanyName), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{st.StoreName} belongs to {st.ClientCompanyName}; route {route.RouteCode} is {route.ClientCompanyName}'s. Only stores of the same client can be on its routes.");
            var fresh = OwnRoutes(route.ClientCompanyName).FirstOrDefault(r => r.Id == route.Id) ?? route;
            var places = fresh.PlacesList;
            int from = places.FindIndex(p => PlaceIsStore(p, st));
            int to = from + delta;
            if (from < 0 || to < 0 || to >= places.Count) return false;
            string before = string.Join("; ", places);
            (places[from], places[to]) = (places[to], places[from]);
            if (string.Join("; ", places) == before) return false;
            WriteRoutePlaces(fresh.Id, places);
            route.ConnectedPlaces = string.Join("; ", places);
            try { LogAuditTrail("UPDATE", "DeliveryRoutes", fresh.Id, fresh.RouteCode, "ConnectedPlaces", null, null, $"Moved store {PlaceFor(st)} {(delta < 0 ? "up" : "down")} on route {fresh.RouteCode}"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            AppEvents.Raise(AppDataKind.Routes);
            return true;
        }

        /// <summary>
        /// The list writer: saves a route's whole store list as given. Every other writer of route store text comes through
        /// here, <see cref="SetStoreOnRoute"/>, or <see cref="SetRoutePlaces"/> followed by SaveDeliveryRoute (the Verify check
        /// RouteStores_AllWritersCanonical keeps it that way). With <paramref name="conn"/> it writes inside that transaction.
        /// </summary>
        private static void WriteRoutePlaces(int routeId, List<string> places, SqliteConnection? conn = null, SqliteTransaction? tx = null)
        {
            using var own = conn == null ? new SqliteConnection(connectionString) : null;
            if (own != null) own.Open();
            using var cmd = (conn ?? own!).CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE DeliveryRoutes SET ConnectedPlaces = @p, UpdatedAt = datetime('now') WHERE Id = @id";
            cmd.Parameters.AddWithValue("@p", string.Join("; ", places));
            cmd.Parameters.AddWithValue("@id", routeId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Takes every entry of the store off the route's list (<see cref="PlaceIsStore"/>, as SetStoreOnRoute removes), without
        /// recomputing the store's route label: an archived store keeps the label, so a restore puts it back on the same route.
        /// </summary>
        private static void RemoveRoutePlace(DeliveryRoute r, ClientStore st)
        {
            var list = r.PlacesList;
            int before = list.Count;
            list.RemoveAll(p => PlaceIsStore(p, st));
            if (list.Count != before) WriteRoutePlaces(r.Id, list);
        }

        /// <summary>
        /// The rename cascade for one store list: an entry that is the store's own (it names the store before or after the change,
        /// <see cref="PlaceIsStore"/>, and none of <paramref name="otherStores"/>) becomes <see cref="PlaceFor"/> of the store after it,
        /// unless that new text would also name one of <paramref name="otherStores"/>. Any other entry is left as it is. No entry is
        /// added or removed. <paramref name="otherStores"/> are the client's other active stores.
        /// </summary>
        public static List<string> RenamedPlaces(IEnumerable<string> places, ClientStore before, ClientStore after, IReadOnlyList<ClientStore> otherStores)
        {
            string canon = PlaceFor(after);
            if (otherStores.Any(o => PlaceIsStore(canon, o))) return places.ToList(); // the new text would be ambiguous
            return places.Select(p => (PlaceIsStore(p, before) || PlaceIsStore(p, after)) && !otherStores.Any(o => PlaceIsStore(p, o)) ? canon : p).ToList();
        }

        /// <summary>A store was renamed or re-coded: its own entries on every route of its client follow it. Returns the routes changed.</summary>
        private static int RenameStoreOnRoutes(ClientStore before, ClientStore after)
        {
            if (!string.Equals(CleanStoreText(before.ClientCompanyName), CleanStoreText(after.ClientCompanyName), StringComparison.OrdinalIgnoreCase)) return 0;
            if (PlaceFor(before) == PlaceFor(after)) return 0;
            var others = GetClientStores(CleanStoreText(after.ClientCompanyName)).Where(o => o.Id != after.Id).ToList();
            int changed = 0;
            foreach (var r in OwnRoutes(after.ClientCompanyName))
            {
                var places = r.PlacesList;
                var renamed = RenamedPlaces(places, before, after, others);
                if (renamed.SequenceEqual(places)) continue;
                WriteRoutePlaces(r.Id, renamed);
                changed++;
                try { LogAuditTrail("UPDATE", "DeliveryRoutes", r.Id, r.RouteCode, "ConnectedPlaces", r.ConnectedPlaces, string.Join("; ", renamed), $"Store {PlaceFor(before)} is now {PlaceFor(after)} on route {r.RouteCode}"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            }
            if (changed > 0)
            {
                RecomputeStoreRouteLabels(after.ClientCompanyName);
                AppEvents.Raise(AppDataKind.Routes);
            }
            return changed;
        }

        /// <summary>
        /// The canonical form of a whole store list: an entry that is exactly one of <paramref name="stores"/> becomes its
        /// <see cref="PlaceFor"/> (kept as typed when that text would also name another store); a later entry for a store already
        /// listed is dropped; other text is kept, with spaces tidied. Order is kept.
        /// </summary>
        public static List<string> CanonicalPlaces(IEnumerable<string> places, IReadOnlyList<ClientStore> stores)
        {
            var result = new List<string>();
            var seen = new HashSet<int>();
            foreach (var raw in places)
            {
                string p = CleanStoreText(raw);
                if (p.Length == 0) continue;
                var hits = stores.Where(st => PlaceIsStore(p, st)).Take(2).ToList();
                if (hits.Count == 1)
                {
                    if (!seen.Add(hits[0].Id)) continue;
                    string canon = PlaceFor(hits[0]);
                    if (stores.Count(st => PlaceIsStore(canon, st)) == 1) p = canon;
                }
                result.Add(p);
            }
            return result;
        }

        /// <summary>
        /// Sets a route's whole store list in memory, in canonical form (<see cref="CanonicalPlaces"/> against the route's client's
        /// stores); SaveDeliveryRoute saves it. A list edit, not an add: the one-route rule is not checked here. When the client's
        /// stores cannot be read the list is set as given (spaces tidied) and the failure is logged.
        /// </summary>
        public static void SetRoutePlaces(DeliveryRoute route, IEnumerable<string> places)
        {
            if (route == null) return;
            List<ClientStore> stores;
            try { stores = GetClientStores(CleanStoreText(route.ClientCompanyName)); }
            catch (Exception ex) { ErrLog.Swallowed(ex); stores = new List<ClientStore>(); }
            route.ConnectedPlaces = string.Join("; ", CanonicalPlaces(places ?? Enumerable.Empty<string>(), stores));
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
