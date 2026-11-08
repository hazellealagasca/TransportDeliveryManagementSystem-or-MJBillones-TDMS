using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 6, 2026: a NEW client's stores and routes can be typed before the client is saved. They are kept here (nothing is
    /// written yet) and saved together with the client when "Create client" is clicked.
    /// </summary>
    public sealed partial class ClientStoresRoutesPanel
    {
        private Func<bool> isPending = () => false;
        /// <summary>True while the client on the form is not saved yet (stores and routes are kept in memory).</summary>
        public void SetPendingCheck(Func<bool> check) => isPending = check ?? (() => false);
        /// <summary>Raised when a store or route is added to / removed from the unsaved list.</summary>
        public event Action? PendingChanged;
        private sealed class PendingRoute { public string Code = "", Name = ""; public HashSet<string> Stores = new(StringComparer.OrdinalIgnoreCase); }
        private readonly List<(string Code, string Name)> pendingStores = new();
        private readonly List<PendingRoute> pendingRoutes = new();
        private DataGridView pendingGrid = null!;
        private Control storesGridCtl = null!;
        public bool HasPending => pendingStores.Count > 0 || pendingRoutes.Count > 0;
        public int PendingStoreCount => pendingStores.Count;
        public int PendingRouteCount => pendingRoutes.Count;

        private void BuildPendingGrid(Control storesGrid)
        {
            storesGridCtl = storesGrid;
            pendingGrid = NewGrid();
            pendingGrid.Visible = false;
            pendingGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Store code", FillWeight = 60, ReadOnly = true });
            pendingGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Store name", FillWeight = 140, ReadOnly = true });
            pendingGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colState", HeaderText = "", FillWeight = 70, ReadOnly = true });
            pendingGrid.Columns.Add(new DataGridViewButtonColumn { Name = "colRemove", HeaderText = "", Text = "Remove", UseColumnTextForButtonValue = true, FillWeight = 40 });
            pendingGrid.CellContentClick += (s, e) =>
            {
                if (e.RowIndex < 0 || pendingGrid.Columns[e.ColumnIndex].Name != "colRemove" || e.RowIndex >= pendingStores.Count) return;
                var (code, _) = pendingStores[e.RowIndex];
                pendingStores.RemoveAt(e.RowIndex);
                foreach (var r in pendingRoutes) r.Stores.Remove(code);
                RenderPendingStores();
                PendingChanged?.Invoke();
            };
            page1.Controls.Add(pendingGrid);
            pendingGrid.BringToFront();
        }

        /// <summary>Shows the unsaved list for a new client, or the saved store table for an existing one.</summary>
        private void ApplyPendingMode()
        {
            bool pending = isPending();
            if (!pending) { pendingStores.Clear(); pendingRoutes.Clear(); }
            if (pendingGrid != null) pendingGrid.Visible = pending;
            if (storesGridCtl != null) storesGridCtl.Visible = !pending;
            if (pending) RenderPendingStores();
        }
        /// <summary>Forgets the unsaved stores and routes (a new client form was opened or left).</summary>
        public void ClearPending()
        {
            pendingStores.Clear();
            pendingRoutes.Clear();
            ApplyPendingMode();
            if (page2.Visible) LoadRoutes();
        }

        private void RenderPendingStores()
        {
            if (pendingGrid == null) return;
            pendingGrid.Rows.Clear();
            foreach (var (code, name) in pendingStores)
            {
                int i = pendingGrid.Rows.Add(code, name, PendingRouteModel.StateText(PendingRouteModel.RoutesHoldingStore(PendingRouteSets(), code)));
                pendingGrid.Rows[i].Height = pendingGrid.RowTemplate.Height;
            }
        }

        /// <summary>Each pending route with its set of store codes (what the matcher in <see cref="PendingRouteModel"/> reads).</summary>
        private IEnumerable<(string RouteCode, IEnumerable<string> StoreCodes)> PendingRouteSets() =>
            pendingRoutes.Select(r => (r.Code, (IEnumerable<string>)r.Stores));

        private void AddPendingStore()
        {
            string code = MJdatabase.CleanStoreCode(txtCode.Text), name = MJdatabase.CleanStoreText(txtName.Text);
            if (code.Length == 0) { Say("✕ Enter the store code (letters, numbers and hyphens).", true); txtCode.Focus(); return; }
            if (name.Length == 0) { Say("✕ Enter the store name.", true); txtName.Focus(); return; }
            if (name.Length > MJdatabase.StoreNameMaxLength) { Say($"✕ The store name is too long (at most {MJdatabase.StoreNameMaxLength} characters).", true); return; }
            if (pendingStores.Any(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase))) { Say($"✕ Store code {code} is already in this list.", true); return; }
            if (pendingStores.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))) { Say($"✕ \"{name}\" is already in this list.", true); return; }
            try
            {
                // the same check as a saved client; the new client has no stores yet, so "(new client)" stands in for its name
                string who = MJdatabase.CleanStoreText(client());
                string? problem = MJdatabase.CheckCompanyStore(who.Length > 0 ? who : "(new client)", code, name);
                if (problem != null) { Say("✕ " + problem, true); return; }
            }
            catch (Exception ex) { ErrLog.Error("ClientStoresRoutesPanel.AddPendingStore", ex); Say("✕ Couldn't check the store: " + ex.Message, true); return; }
            pendingStores.Add((code, name));
            Say($"✔ {code} – {name} added. It is saved together with the client when you click Create client.", false);
            txtCode.Clear(); txtName.Clear(); txtCode.Focus();
            RenderPendingStores();
            PendingChanged?.Invoke();
        }

        private void AddPendingRoute()
        {
            string code = MJdatabase.CleanStoreCode(txtNewCode.Text), name = MJdatabase.CleanStoreText(txtNewName.Text);
            if (code.Length == 0 || name.Length == 0) { Say("✕ Enter the route code and the route name.", true); return; }
            if (pendingRoutes.Any(r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase))) { Say($"✕ Route {code} is already in this list.", true); return; }
            pendingRoutes.Add(new PendingRoute { Code = code, Name = name });
            Say($"✔ Route {code} – {name} added (saved with the client). Now tick its stores on the right.", false);
            txtNewCode.Clear(); txtNewName.Clear();
            PendingChanged?.Invoke();
            LoadRoutes(code);
        }

        private PendingRoute? SelectedPendingRoute()
        {
            string code = gridRoutes.CurrentRow?.Cells["colCode"].Value?.ToString() ?? "";
            return pendingRoutes.FirstOrDefault(r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase));
        }

        private void LoadPendingRoutes(string? selectCode)
        {
            string keep = selectCode ?? (gridRoutes.CurrentRow?.Cells["colCode"].Value?.ToString() ?? "");
            binding = true;
            try
            {
                gridRoutes.Rows.Clear();
                foreach (var r in pendingRoutes.OrderBy(r => r.Code))
                {
                    int i = gridRoutes.Rows.Add(r.Code, r.Name, $"{r.Stores.Count} store{(r.Stores.Count == 1 ? "" : "s")}");
                    gridRoutes.Rows[i].Height = gridRoutes.RowTemplate.Height;
                    if (string.Equals(r.Code, keep, StringComparison.OrdinalIgnoreCase)) gridRoutes.CurrentCell = gridRoutes.Rows[i].Cells[0];
                }
                if (gridRoutes.CurrentRow == null && gridRoutes.Rows.Count > 0) gridRoutes.CurrentCell = gridRoutes.Rows[0].Cells[0];
            }
            finally { binding = false; }
            RenderPendingAssign();
        }

        private void RenderPendingAssign()
        {
            var r = SelectedPendingRoute();
            lblAssignTitle.Text = r == null ? "Stores on this route — add a route on the left" : $"Stores on route {r.Code} — tick the stores it serves (saved with the client)";
            string q = txtSearch.Text.Trim();
            binding = true;
            try
            {
                gridAssign.Rows.Clear();
                gridAssign.Enabled = r != null;
                foreach (var (code, name) in pendingStores.Where(p => q.Length == 0 || p.Code.Contains(q, StringComparison.OrdinalIgnoreCase) || p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).OrderBy(p => p.Name))
                {
                    var holding = PendingRouteModel.RoutesHoldingStore(PendingRouteSets(), code);
                    bool on = r != null && holding.Contains(r.Code, StringComparer.OrdinalIgnoreCase);
                    var others = r == null ? holding : holding.Where(h => !string.Equals(h, r.Code, StringComparison.OrdinalIgnoreCase)).ToList();
                    string state = on ? (others.Count > 0 ? $"✔ On this route · also on {string.Join(", ", others)}" : "✔ On this route")
                        : others.Count > 0 ? $"Also on {string.Join(", ", others)}" : "Available";
                    int i = gridAssign.Rows.Add(on, code, name, state);
                    gridAssign.Rows[i].Tag = code;
                    gridAssign.Rows[i].Height = gridAssign.RowTemplate.Height;
                }
            }
            finally { binding = false; }
        }

        private void TogglePendingStore(int rowIndex)
        {
            var r = SelectedPendingRoute();
            if (r == null || !(gridAssign.Rows[rowIndex].Tag is string code)) return;
            bool include = gridAssign.Rows[rowIndex].Cells["colOn"].Value is bool b && b;
            if (include) r.Stores.Add(code); else r.Stores.Remove(code);
            Say(include ? $"✔ {code} is on route {r.Code} (saved with the client)." : $"{code} was taken off route {r.Code}.", false);
            PendingChanged?.Invoke();
            BeginInvoke(new Action(() => { LoadPendingRoutes(r.Code); RenderPendingStores(); }));
        }

        /// <summary>
        /// Saves the unsaved stores and routes for the client that was just created. Returns what could not be saved (null = all saved).
        /// </summary>
        public string? SavePending(string clientName)
        {
            var errors = new List<string>();
            foreach (var (code, name) in pendingStores)
            {
                try { MJdatabase.RegisterCompanyStore(clientName, code, name); }
                catch (Exception ex) { errors.Add($"Store {code} – {name}: {ex.Message}"); }
            }
            var saved = MJdatabase.GetClientStores(clientName);
            foreach (var pr in pendingRoutes)
            {
                try
                {
                    var route = new DeliveryRoute { ClientCompanyName = clientName, RouteCode = pr.Code, RouteName = pr.Name, Description = "", ConnectedPlaces = "", IsActive = true };
                    if (!MJdatabase.SaveDeliveryRoute(route)) { errors.Add($"Route {pr.Code}: it wasn't saved."); continue; }
                    var fresh = MJdatabase.GetDeliveryRoutesByClient(clientName, activeOnly: false).FirstOrDefault(x => string.Equals(x.RouteCode, pr.Code, StringComparison.OrdinalIgnoreCase)) ?? route;
                    // each typed store once, in the order typed, through the one writer
                    PendingRouteModel.SaveRouteStores(fresh, saved, PendingRouteModel.StoresToWrite(pr.Stores, pendingStores), errors);
                }
                catch (Exception ex) { errors.Add($"Route {pr.Code}: {ex.Message}"); }
            }
            pendingStores.Clear();
            pendingRoutes.Clear();
            try { AppEvents.Raise(AppDataKind.Stores); AppEvents.Raise(AppDataKind.Routes); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            return errors.Count == 0 ? null : string.Join("\n", errors);
        }
    }
}
