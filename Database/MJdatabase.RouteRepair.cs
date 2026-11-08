using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// One-time repair of route store lists (DeliveryRoutes.ConnectedPlaces) written before there was one matcher and one
    /// writer: the same store several times, or written without its code. Run it as a dry run first and review the report.
    /// </summary>
    public static partial class MJdatabase
    {
        public const string RoutePlacesFixKey = "Fix.RoutePlaces.v1";

        /// <summary>What <see cref="RepairRoutePlaces"/> found (dry run) or did (apply).</summary>
        public sealed class RouteRepairReport
        {
            public int EntriesBefore { get; set; }
            public int EntriesAfter { get; set; }
            /// <summary>Later copies of a store already on the same route (dropped).</summary>
            public int DuplicatesRemoved { get; set; }
            /// <summary>Kept entries rewritten as <see cref="PlaceFor"/> of their store.</summary>
            public int Canonicalised { get; set; }
            /// <summary>Entries that are none of the client's active stores (left as they are).</summary>
            public List<string> Unmatched { get; } = new();
            /// <summary>Entries that two or more of the client's active stores match (left as they are).</summary>
            public List<string> Ambiguous { get; } = new();
            /// <summary>Entries with one match kept in their own text, because <see cref="PlaceFor"/> of the store would match two or more stores.</summary>
            public List<string> KeptWouldBeAmbiguous { get; } = new();
            /// <summary>Orders built from a route that had duplicates. Listed for review only; never changed.</summary>
            public List<string> OrdersToReview { get; } = new();
        }

        /// <summary>
        /// Each route entry is matched (<see cref="PlaceIsStore"/>) against the route client's active stores.
        /// One match: written as <see cref="PlaceFor"/> (unless that text would match two or more stores: then kept as it
        /// was and listed in KeptWouldBeAmbiguous); the first occurrence is kept, later copies dropped, order kept.
        /// No match, or two or more: left unchanged and listed. Orders are only listed.
        /// With <paramref name="apply"/> false nothing is written. With true: backs up the database first (Backups folder
        /// beside the database file), writes the changed lists, and records <see cref="RoutePlacesFixKey"/>; once recorded,
        /// later calls only report. Throws InvalidOperationException when the backup fails (nothing is written then).
        /// </summary>
        public static RouteRepairReport RepairRoutePlaces(bool apply)
        {
            var report = new RouteRepairReport();
            var storesByClient = new Dictionary<string, List<ClientStore>>(StringComparer.OrdinalIgnoreCase);
            var changes = new List<(DeliveryRoute Route, List<string> Places)>();
            var duplicatedRoutes = new List<DeliveryRoute>();

            foreach (var route in GetDeliveryRoutes(activeOnly: false))
            {
                string client = CleanStoreText(route.ClientCompanyName);
                if (!storesByClient.TryGetValue(client, out var stores))
                    storesByClient[client] = stores = GetClientStores(client, includeArchived: false);
                var places = route.PlacesList;
                var kept = new List<string>();
                var seen = new HashSet<int>();
                bool hadDuplicate = false;
                report.EntriesBefore += places.Count;
                foreach (var place in places)
                {
                    var hits = stores.Where(st => PlaceIsStore(place, st)).ToList();
                    if (hits.Count != 1)
                    {
                        string where = $"route {route.RouteCode} ({client}): \"{place}\"";
                        if (hits.Count == 0) report.Unmatched.Add(where);
                        else report.Ambiguous.Add($"{where} matches {string.Join(", ", hits.Select(st => $"store #{st.Id} \"{PlaceFor(st)}\""))}");
                        kept.Add(place);
                        continue;
                    }
                    var store = hits[0];
                    if (!seen.Add(store.Id)) { report.DuplicatesRemoved++; hadDuplicate = true; continue; }
                    string canonical = PlaceFor(store);
                    var canonicalHits = stores.Where(st => PlaceIsStore(canonical, st)).ToList();
                    if (canonicalHits.Count > 1)
                    {
                        // The new text would name another store too: keep the entry as it was written.
                        report.KeptWouldBeAmbiguous.Add($"route {route.RouteCode} ({client}): \"{place}\" kept; \"{canonical}\" would match {string.Join(", ", canonicalHits.Select(st => $"store #{st.Id}"))}");
                        kept.Add(place);
                        continue;
                    }
                    if (!string.Equals(canonical, place, StringComparison.Ordinal)) report.Canonicalised++;
                    kept.Add(canonical);
                }
                report.EntriesAfter += kept.Count;
                if (hadDuplicate) duplicatedRoutes.Add(route);
                if (!kept.SequenceEqual(places, StringComparer.Ordinal)) changes.Add((route, kept));
            }
            if (duplicatedRoutes.Count > 0) report.OrdersToReview.AddRange(OrdersBuiltFrom(duplicatedRoutes));

            if (!apply || string.Equals(GetSystemSetting(RoutePlacesFixKey, ""), "done", StringComparison.OrdinalIgnoreCase)) return report;

            string backup = Path.Combine(Path.GetDirectoryName(dbFilePath) ?? ".", "Backups", $"before_route_places_{DateTime.Now:yyyyMMdd_HHmmss_fff}.db");
            Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
            if (!BackupDatabase(backup)) throw new InvalidOperationException("Couldn't back up the database before repairing the route store lists: " + LastBackupError);

            foreach (var (route, places) in changes)
            {
                WriteRoutePlaces(route.Id, places);
                try { LogAuditTrail("UPDATE", "DeliveryRoutes", route.Id, route.RouteCode, "ConnectedPlaces", route.ConnectedPlaces, string.Join("; ", places), "One-time repair: repeated stores removed, stores written as code - name"); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            }
            SetSystemSetting(RoutePlacesFixKey, "done", "Route store lists repaired (repeats removed, code - name)");
            if (changes.Count > 0)
            {
                RecomputeAllStoreRouteLabels();
                AppEvents.Raise(AppDataKind.Routes);
            }
            return report;
        }

        /// <summary>Orders whose destination was built from one of these routes ("R1 - North: ..."), same client.</summary>
        private static List<string> OrdersBuiltFrom(List<DeliveryRoute> routes)
        {
            var found = new List<string>();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(DispatchNumber,''), COALESCE(ClientName,''), COALESCE(Destination,''), COALESCE(Status,'') FROM Deliveries ORDER BY Id";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                string client = CleanStoreText(r.GetString(1)), destination = r.GetString(2).Trim();
                var route = routes.FirstOrDefault(rt =>
                    string.Equals(CleanStoreText(rt.ClientCompanyName), client, StringComparison.OrdinalIgnoreCase)
                    && (string.Equals(destination, RouteLabel(rt), StringComparison.OrdinalIgnoreCase)
                        || destination.StartsWith(RouteLabel(rt) + ":", StringComparison.OrdinalIgnoreCase)));
                if (route != null) found.Add($"{r.GetString(0)} ({client}, route {route.RouteCode}, {r.GetString(3)})");
            }
            return found;
        }
    }
}
