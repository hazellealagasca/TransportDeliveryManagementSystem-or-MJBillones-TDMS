using System;
using System.Collections.Generic;
using System.Linq;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>One store as the Clients > Delivery routes panel shows it for the selected route.</summary>
    public record RouteStoreRow(ClientStore Store, bool OnThisRoute, IReadOnlyList<string> OtherRoutes, bool HiddenByRule);

    /// <summary>
    /// The true store list of one route, with no UI. One row per distinct store (route entries are never rows):
    /// stores on this route first, then the rest by name. A store on another route carries that route's code in
    /// OtherRoutes; with oneRoutePerStore on, such a store that is not on this route is HiddenByRule.
    /// </summary>
    public static class RouteStoreListModel
    {
        public static List<RouteStoreRow> Build(DeliveryRoute route, IEnumerable<ClientStore> stores, IEnumerable<DeliveryRoute> clientRoutes, bool oneRoutePerStore)
        {
            var others = clientRoutes.Where(r => r.IsActive && !IsSameRoute(r, route)).ToList();
            var rows = new List<RouteStoreRow>();
            foreach (var st in stores)
            {
                bool on = MJdatabase.StoreIsOnRoute(route, st);
                var also = others.Where(r => MJdatabase.StoreIsOnRoute(r, st)).Select(r => r.RouteCode)
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                rows.Add(new RouteStoreRow(st, on, also, oneRoutePerStore && !on && also.Count > 0));
            }
            return rows.OrderBy(r => r.OnThisRoute ? 0 : 1).ThenBy(r => r.Store.StoreName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>Counts of the whole list. Call it on the unfiltered rows: the search text must not change them.</summary>
        public static (int OnThisRoute, int Hidden) Counts(IEnumerable<RouteStoreRow> rows) =>
            (rows.Count(r => r.OnThisRoute), rows.Count(r => !r.OnThisRoute && r.HiddenByRule));

        /// <summary>Whether a row passes the search text (store code or name). Filters what is displayed, never the counts.</summary>
        public static bool Matches(RouteStoreRow row, string q) =>
            q.Length == 0 || row.Store.StoreCode.Contains(q, StringComparison.OrdinalIgnoreCase) || row.Store.StoreName.Contains(q, StringComparison.OrdinalIgnoreCase);

        private static bool IsSameRoute(DeliveryRoute a, DeliveryRoute b) =>
            ReferenceEquals(a, b) || (a.Id != 0 && a.Id == b.Id) ||
            (a.Id == 0 && b.Id == 0 && string.Equals(a.RouteCode, b.RouteCode, StringComparison.OrdinalIgnoreCase));
    }
}
