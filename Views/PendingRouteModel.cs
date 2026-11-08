using System;
using System.Collections.Generic;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// The new-client path of the Clients panel (stores and routes typed before the client is saved), with no UI. The pending path holds
    /// only store codes in memory, so it matches by store code against each pending route's code set (no database call). Saving goes
    /// through the one writer, <see cref="MJdatabase.SetStoreOnRoute"/>.
    /// </summary>
    public static class PendingRouteModel
    {
        private static bool SameCode(string a, string b) =>
            string.Equals(MJdatabase.CleanStoreCode(a), MJdatabase.CleanStoreCode(b), StringComparison.OrdinalIgnoreCase);

        /// <summary>The codes of the pending routes that hold this store code, in the order the routes are given, each once.</summary>
        public static List<string> RoutesHoldingStore(IEnumerable<(string RouteCode, IEnumerable<string> StoreCodes)> routes, string storeCode) =>
            routes.Where(r => r.StoreCodes.Any(c => SameCode(c, storeCode))).Select(r => r.RouteCode)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        /// <summary>The state text of a pending store: "On DW, DW1 · not saved yet", or "Not saved yet" when no route holds it.</summary>
        public static string StateText(IReadOnlyList<string> routeCodes) =>
            routeCodes.Count == 0 ? "Not saved yet" : $"On {string.Join(", ", routeCodes)} · not saved yet";

        /// <summary>
        /// The stores to write for one route: the ticked codes (any case, a code ticked twice counts once), limited to the stores that were typed,
        /// returned in the order the stores were typed. A ticked code that was never typed is dropped.
        /// </summary>
        public static List<(string Code, string Name)> StoresToWrite(IEnumerable<string> tickedCodes, IEnumerable<(string Code, string Name)> typedStores)
        {
            var ticked = tickedCodes.Select(MJdatabase.CleanStoreCode).Where(c => c.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<(string Code, string Name)>();
            foreach (var s in typedStores)
            {
                string c = MJdatabase.CleanStoreCode(s.Code);
                if (ticked.Contains(c) && seen.Add(c)) result.Add(s);
            }
            return result;
        }

        /// <summary>
        /// Writes one saved route's ticked stores through <see cref="MJdatabase.SetStoreOnRoute"/>. <paramref name="route"/> is the route as read back
        /// from the database; <paramref name="savedStores"/> are the client's stores as saved. A store that did not save is skipped (its own error was already recorded);
        /// a failed write is added to <paramref name="errors"/>. Returns how many entries were added.
        /// </summary>
        public static int SaveRouteStores(DeliveryRoute route, IEnumerable<ClientStore> savedStores, IEnumerable<(string Code, string Name)> toWrite, List<string> errors)
        {
            var saved = savedStores.ToList();
            int added = 0;
            foreach (var (code, _) in toWrite)
            {
                var st = saved.FirstOrDefault(s => SameCode(s.StoreCode, code));
                if (st == null) continue;
                try { if (MJdatabase.SetStoreOnRoute(route, st, true)) added++; }
                catch (Exception ex) { ErrLog.Error("PendingRouteModel.SaveRouteStores", ex); errors.Add($"{code} on route {route.RouteCode}: {ex.Message}"); }
            }
            return added;
        }
    }
}
