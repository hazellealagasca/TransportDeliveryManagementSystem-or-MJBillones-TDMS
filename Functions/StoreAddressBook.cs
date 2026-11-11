using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Finds a store's address for display (tooltips on the store tables of the dispatch order and the trip report, the store details window).
    /// Read-only: addresses are typed and saved in Master Data. A store with no address (older stores) simply has none to show.
    /// The client's stores are cached and re-read after any data change (AppEvents.Version).
    /// </summary>
    public static class StoreAddressBook
    {
        private static readonly Dictionary<string, (int Version, List<ClientStore> Stores)> cache = new(StringComparer.OrdinalIgnoreCase);

        private static List<ClientStore> StoresOf(string client)
        {
            client = (client ?? "").Trim();
            if (client.Length == 0) return new List<ClientStore>();
            if (cache.TryGetValue(client, out var hit) && hit.Version == AppEvents.Version) return hit.Stores;
            List<ClientStore> list;
            try { list = MJdatabase.GetClientStores(client, includeArchived: true); }
            catch (Exception ex) { ErrLog.Swallowed(ex); list = new List<ClientStore>(); }
            cache[client] = (AppEvents.Version, list);
            return list;
        }

        /// <summary>The address of the client's store with this code (or, failing that, this name); blank when the store is unknown or has no address.</summary>
        public static string Get(string client, string? code, string? name)
        {
            var stores = StoresOf(client);
            string c = (code ?? "").Trim(), n = MJdatabase.CleanStoreText(name);
            ClientStore? st = null;
            if (c.Length > 0) st = stores.FirstOrDefault(s => string.Equals((s.StoreCode ?? "").Trim(), c, StringComparison.OrdinalIgnoreCase));
            if (st == null && n.Length > 0) st = stores.FirstOrDefault(s => string.Equals(MJdatabase.CleanStoreText(s.StoreName), n, StringComparison.OrdinalIgnoreCase));
            return (st?.Address ?? "").Trim();
        }

        /// <summary>
        /// Shows "Address: ..." as a tip while the mouse is over a store's code or name cell in <paramref name="grid"/> (nothing when the store has no address).
        /// The tip is disposed with the grid.
        /// </summary>
        public static void AttachTips(DataGridView grid, Func<string> client, string codeColumn, string nameColumn)
        {
            var tip = new ToolTip { InitialDelay = 400, ShowAlways = true };
            grid.Disposed += (s, e) => tip.Dispose();
            grid.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.RowIndex >= grid.Rows.Count) return;
                string col = grid.Columns[e.ColumnIndex].Name;
                if (col != codeColumn && col != nameColumn) return;
                var row = grid.Rows[e.RowIndex];
                string code = grid.Columns.Contains(codeColumn) ? row.Cells[codeColumn].Value?.ToString() ?? "" : "";
                string name = grid.Columns.Contains(nameColumn) ? row.Cells[nameColumn].Value?.ToString() ?? "" : "";
                string address = Get(client(), code, name);
                if (address.Length == 0) { tip.Hide(grid); return; }
                var at = grid.PointToClient(Cursor.Position);
                tip.Show("Address: " + address, grid, at.X + 12, at.Y + 20, 5000);
            };
            grid.CellMouseLeave += (s, e) => tip.Hide(grid);
        }
    }
}
