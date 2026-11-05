using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Rule 16c, Master Data → Clients: two large tabs.
    /// 1. Company Stores: quick entry (code + name, Enter to add) above the client's store table (code and name, no route).
    /// 2. Delivery Routes: the client's routes on the left (with a store count), and on the right every store of the same
    ///    client with a checkbox to include it on the selected route. A store can be on any number of the client's routes.
    /// </summary>
    public sealed partial class ClientStoresRoutesPanel : Panel
    {
        private readonly Func<string> client;
        private readonly Action reloadStores;
        private readonly Button tab1, tab2;
        private readonly Panel page1, page2;
        private readonly TextBox txtCode, txtName, txtSearch, txtNewCode, txtNewName;
        private readonly DataGridView gridRoutes, gridAssign;
        private readonly Label lblAssignTitle, lblMsg;
        private List<DeliveryRoute> routes = new List<DeliveryRoute>();
        private List<ClientStore> stores = new List<ClientStore>();
        private bool binding;

        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);

        public ClientStoresRoutesPanel(Func<string> clientName, Control storesGrid, Action reloadStoresGrid)
        {
            client = clientName;
            reloadStores = reloadStoresGrid;
            BackColor = AppTheme.Surface;
            Height = D(LargePrint.Px(520, 420));

            // ---- segmented tabs (48 px, 14 pt bold, active = navy with a 4 px accent bar)
            var tabs = new TableLayoutPanel { Dock = DockStyle.Top, Height = D(LargePrint.Px(46, 38)), ColumnCount = 2, RowCount = 1, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            tabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); tabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tab1 = TabButton("Company stores");
            tab2 = TabButton("Delivery routes");
            tabs.Controls.Add(tab1, 0, 0); tabs.Controls.Add(tab2, 1, 0);

            lblMsg = new Label { Dock = DockStyle.Top, Height = D(28), Font = FontCache.Get("Segoe UI", LargePrint.Pt(10F, 8.75F)), ForeColor = AppTheme.TextSecondary, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(D(4), 0, 0, 0), UseMnemonic = false, AutoEllipsis = true };

            // ---- page 1: quick-entry bar + the existing store table
            page1 = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
            var quick = new FlowLayoutPanel { Dock = DockStyle.Top, Height = D(LargePrint.Px(58, 44)), WrapContents = false, BackColor = AppTheme.SurfaceAlt, Padding = new Padding(D(8), D(8), D(8), D(6)) };
            txtCode = Entry("Store Code", 170, upper: true);
            txtName = Entry("Store Name", 340, upper: false);
            var btnAdd = BigButton("＋ Add store", AppTheme.Success);
            btnAdd.Click += (s, e) => AddStore();
            txtCode.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtName.Focus(); } };
            txtName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddStore(); } };
            quick.Controls.AddRange(new Control[] { txtCode, txtName, btnAdd });
            storesGrid.Dock = DockStyle.Fill;
            StyleGrid(storesGrid as DataGridView);
            page1.Controls.Add(storesGrid);
            page1.Controls.Add(quick);
            BuildPendingGrid(storesGrid); // Nov 6: a new client's stores before it is saved

            // ---- page 2: split view
            page2 = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Visible = false };
            var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = AppTheme.Surface };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42)); split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            // left: routes
            var left = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, D(8), 0), BackColor = AppTheme.Surface };
            var newBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = D(LargePrint.Px(58, 44)), WrapContents = false, BackColor = AppTheme.SurfaceAlt, Padding = new Padding(D(6), D(8), D(6), D(6)) };
            txtNewCode = Entry("Route Code", 110, upper: true);
            txtNewName = Entry("Route Name", 170, upper: false);
            var btnNewRoute = BigButton("＋ New route", AppTheme.Accent);
            btnNewRoute.Click += (s, e) => AddRoute();
            txtNewName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddRoute(); } };
            newBar.Controls.AddRange(new Control[] { txtNewCode, txtNewName, btnNewRoute });
            gridRoutes = NewGrid();
            gridRoutes.MultiSelect = false;
            gridRoutes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Route Code", FillWeight = 60, ReadOnly = true });
            gridRoutes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Route Name", FillWeight = 120, ReadOnly = true });
            gridRoutes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCount", HeaderText = "Stores", FillWeight = 45, ReadOnly = true });
            gridRoutes.SelectionChanged += (s, e) => { if (!binding) RenderAssign(); };
            gridRoutes.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || gridRoutes.Columns[e.ColumnIndex].Name != "colCount") return;
                e.CellStyle.BackColor = AppTheme.InfoSoft; e.CellStyle.ForeColor = AppTheme.InfoText; e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            };
            left.Controls.Add(gridRoutes);
            left.Controls.Add(newBar);
            // right: stores of the same client
            var right = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
            lblAssignTitle = new Label { Dock = DockStyle.Top, Height = D(30), Font = FontCache.Get("Segoe UI Semibold", LargePrint.Pt(11F, 9.5F)), ForeColor = AppTheme.TextPrimary, TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false, AutoEllipsis = true, Text = "Stores on this route" };
            txtSearch = new TextBox { Dock = DockStyle.Top, Font = FontCache.Get("Segoe UI", LargePrint.Pt(11F, 9.75F)), PlaceholderText = "🔍 Search stores (code or name)…" };
            txtSearch.TextChanged += (s, e) => RenderAssign();
            gridAssign = NewGrid();
            gridAssign.ReadOnly = false;
            gridAssign.Columns.Add(new DataGridViewCheckBoxColumn { Name = "colOn", HeaderText = "✔", FillWeight = 20 });
            gridAssign.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Store Code", FillWeight = 55, ReadOnly = true });
            gridAssign.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Store Name", FillWeight = 110, ReadOnly = true });
            gridAssign.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTag", HeaderText = "Status", FillWeight = 80, MinimumWidth = 120, ReadOnly = true });
            gridAssign.CurrentCellDirtyStateChanged += (s, e) => { if (gridAssign.IsCurrentCellDirty) gridAssign.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            gridAssign.CellValueChanged += (s, e) => { if (!binding && e.RowIndex >= 0 && gridAssign.Columns[e.ColumnIndex].Name == "colOn") ToggleStore(e.RowIndex); };
            gridAssign.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || gridAssign.Columns[e.ColumnIndex].Name != "colTag") return;
                bool on = (e.Value?.ToString() ?? "").StartsWith("✔");
                e.CellStyle.BackColor = on ? AppTheme.SuccessSoft : AppTheme.SurfaceAlt;
                e.CellStyle.ForeColor = on ? AppTheme.SuccessText : AppTheme.TextSecondary;
                e.CellStyle.Font = FontCache.Get("Segoe UI Semibold", LargePrint.Pt(10F, 8.5F));
            };
            right.Controls.Add(gridAssign);
            right.Controls.Add(txtSearch);
            right.Controls.Add(lblAssignTitle);
            split.Controls.Add(left, 0, 0); split.Controls.Add(right, 1, 0);
            page2.Controls.Add(split);

            Controls.Add(page2);
            Controls.Add(page1);
            Controls.Add(lblMsg);
            Controls.Add(tabs);
            tab1.Click += (s, e) => ShowTab(1);
            tab2.Click += (s, e) => ShowTab(2);
            ShowTab(1);

            // Rule 16e: refresh when stores or routes change anywhere in the app.
            AppEvents.DataChanged += OnDataChanged;
            Disposed += (s, e) => AppEvents.DataChanged -= OnDataChanged;
        }

        private void OnDataChanged(AppDataKind k)
        {
            if (IsDisposed || (k != AppDataKind.Stores && k != AppDataKind.Routes)) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => OnDataChanged(k))); return; }
            if (page2.Visible) LoadRoutes();
        }

        private Button TabButton(string text)
        {
            var b = new Button { Dock = DockStyle.Fill, Text = text, FlatStyle = FlatStyle.Flat, Font = LargePrint.On ? LargePrint.Bold(13F, 10F) : FontCache.Get("Segoe UI Semibold", 10F), Cursor = Cursors.Hand, Margin = new Padding(0), UseMnemonic = false, TextAlign = ContentAlignment.MiddleCenter };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.Empty;
            b.Paint += (s, e) =>
            {
                if (!(b.Tag is bool active) || !active) return;
                using var br = new SolidBrush(AppTheme.Accent);
                e.Graphics.FillRectangle(br, 0, b.Height - D(3), b.Width, D(3)); // accent bar under the active tab
            };
            return b;
        }
        private TextBox Entry(string placeholder, int width, bool upper)
        {
            var t = new TextBox { PlaceholderText = placeholder, Width = D(width), Font = LargePrint.On ? LargePrint.Bold(12F, 9.75F) : FontCache.Get("Segoe UI", 9.75F), Margin = new Padding(0, 0, D(8), 0), CharacterCasing = upper ? CharacterCasing.Upper : CharacterCasing.Normal, BorderStyle = BorderStyle.FixedSingle };
            return t;
        }
        private Button BigButton(string text, Color back)
        {
            var b = new Button { Text = text, AutoSize = true, Font = FontCache.Get("Segoe UI Semibold", 9F), Cursor = Cursors.Hand, MinimumSize = new Size(0, D(LargePrint.Px(36, 30))), Padding = new Padding(D(8), 0, D(8), 0), UseMnemonic = false };
            UIThemeHelper.SetButtonRole(b, back == AppTheme.Success ? "primary" : "secondary");

            return b;
        }
        private DataGridView NewGrid()
        {
            var g = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BorderStyle = BorderStyle.None, ReadOnly = true };
            StyleGrid(g);
            return g;
        }
        private void StyleGrid(DataGridView? g)
        {
            if (g == null) return;
            if (!g.IsHandleCreated) { g.HandleCreated += (s, e) => g.BeginInvoke(new Action(() => StyleGrid(g))); g.DataBindingComplete += (s, e) => { foreach (DataGridViewRow r in g.Rows) r.Height = D(LargePrint.Px(38, 32)); }; }
            g.BackgroundColor = AppTheme.Surface;
            g.GridColor = AppTheme.BorderStrong;
            g.RowTemplate.Height = D(LargePrint.Px(38, 32));
            g.DefaultCellStyle.Font = LargePrint.On ? LargePrint.Bold(12F, 9.25F) : FontCache.Get("Segoe UI", 9.25F);
            g.DefaultCellStyle.BackColor = AppTheme.Surface;
            g.DefaultCellStyle.ForeColor = AppTheme.TextPrimary;
            g.DefaultCellStyle.SelectionBackColor = AppTheme.GridSelection;
            g.DefaultCellStyle.SelectionForeColor = AppTheme.TextPrimary;
            g.AlternatingRowsDefaultCellStyle.BackColor = AppTheme.SurfaceAlt; // zebra rows
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersDefaultCellStyle.Font = FontCache.Get("Segoe UI Semibold", LargePrint.Pt(10F, 8.75F));
            g.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.GridHeaderBack;
            g.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.GridHeaderText;
            g.ColumnHeadersHeight = D(LargePrint.Px(36, 32));
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            foreach (DataGridViewRow r in g.Rows) r.Height = g.RowTemplate.Height;
        }

        private void ShowTab(int n)
        {
            page1.Visible = n == 1; page2.Visible = n == 2;
            foreach (var (b, on) in new[] { (tab1, n == 1), (tab2, n == 2) })
            {
                b.Tag = on;
                b.BackColor = on ? AppTheme.AccentSoft : AppTheme.Surface; // theme accent tint for the active tab
                b.ForeColor = on ? AppTheme.AccentText : AppTheme.TextSecondary;
                b.Invalidate();
            }
            lblMsg.Text = n == 1 ? "Type a store code and name, then press Enter. Stores are not tied to a route here." : "Pick a route on the left, then tick the stores it serves. A store can be on any of this client's routes.";
            lblMsg.ForeColor = AppTheme.TextSecondary;
            if (n == 2) LoadRoutes();
        }

        private void Say(string text, bool bad)
        {
            lblMsg.Text = text;
            lblMsg.ForeColor = bad ? AppTheme.DangerText : AppTheme.SuccessText;
        }

        private void AddStore()
        {
            if (isPending()) { AddPendingStore(); return; } // Nov 6: client not saved yet
            string c = client();
            try
            {
                var st = MJdatabase.RegisterCompanyStore(c, txtCode.Text, txtName.Text);
                Say($"✔ Added {st.StoreCode} – {st.StoreName} to {c}'s stores.", false);
                txtCode.Clear(); txtName.Clear(); txtCode.Focus();
                reloadStores();
            }
            catch (InvalidOperationException ex) { Say("✕ " + ex.Message, true); }
            catch (Exception ex) { ErrLog.Error("ClientStoresRoutesPanel.AddStore", ex); Say("✕ Couldn't add the store: " + ex.Message, true); }
        }

        private void AddRoute()
        {
            if (isPending()) { AddPendingRoute(); return; } // Nov 6: client not saved yet
            string c = client();
            string code = MJdatabase.CleanStoreCode(txtNewCode.Text), name = MJdatabase.CleanStoreText(txtNewName.Text);
            if (c.Length == 0) { Say("✕ Save the client first.", true); return; }
            if (code.Length == 0 || name.Length == 0) { Say("✕ Enter the route code and the route name.", true); return; }
            try
            {
                var r = new DeliveryRoute { ClientCompanyName = c, RouteCode = code, RouteName = name, Description = "", ConnectedPlaces = "", IsActive = true };
                if (!MJdatabase.SaveDeliveryRoute(r)) { Say("✕ The route wasn't saved.", true); return; }
                Say($"✔ Route {code} – {name} created. Now tick its stores on the right.", false);
                txtNewCode.Clear(); txtNewName.Clear();
                AppEvents.Raise(AppDataKind.Routes);
                LoadRoutes(code);
            }
            catch (Exception ex) { Say("✕ " + ex.Message, true); }
        }

        /// <summary>Called when another client is opened.</summary>
        public void RefreshForClient() { txtCode.Clear(); txtName.Clear(); lblMsg.Text = ""; ApplyPendingMode(); if (page2.Visible) LoadRoutes(); }

        public void LoadRoutes(string? selectCode = null)
        {
            if (isPending()) { LoadPendingRoutes(selectCode); return; } // Nov 6: client not saved yet
            string c = client();
            string keep = selectCode ?? (gridRoutes.CurrentRow?.Cells["colCode"].Value?.ToString() ?? "");
            binding = true;
            try
            {
                routes = c.Length == 0 ? new List<DeliveryRoute>() : MJdatabase.GetDeliveryRoutesByClient(c, activeOnly: false)
                    .Where(r => string.Equals((r.ClientCompanyName ?? "").Trim(), c.Trim(), StringComparison.OrdinalIgnoreCase)).OrderBy(r => r.RouteCode).ToList();
                stores = c.Length == 0 ? new List<ClientStore>() : MJdatabase.GetClientStores(c);
                gridRoutes.Rows.Clear();
                foreach (var r in routes)
                {
                    int n = stores.Count(st => r.PlacesList.Any(p => MJdatabase.PlaceIsStore(p, st)));
                    int i = gridRoutes.Rows.Add(r.RouteCode, r.RouteName, $"{n} store{(n == 1 ? "" : "s")}");
                    gridRoutes.Rows[i].Height = gridRoutes.RowTemplate.Height;
                    if (string.Equals(r.RouteCode, keep, StringComparison.OrdinalIgnoreCase)) gridRoutes.CurrentCell = gridRoutes.Rows[i].Cells[0];
                }
                if (gridRoutes.CurrentRow == null && gridRoutes.Rows.Count > 0) gridRoutes.CurrentCell = gridRoutes.Rows[0].Cells[0];
            }
            catch (Exception ex) { ErrLog.Error("ClientStoresRoutesPanel.LoadRoutes", ex); }
            finally { binding = false; }
            RenderAssign();
        }

        private DeliveryRoute? SelectedRoute()
        {
            string code = gridRoutes.CurrentRow?.Cells["colCode"].Value?.ToString() ?? "";
            return routes.FirstOrDefault(r => string.Equals(r.RouteCode, code, StringComparison.OrdinalIgnoreCase));
        }

        private void RenderAssign()
        {
            if (isPending()) { RenderPendingAssign(); return; } // Nov 6: client not saved yet
            var r = SelectedRoute();
            lblAssignTitle.Text = r == null ? "Stores on this route — pick a route on the left" : $"Stores on route {r.RouteCode} — tick the stores it serves (same client only)";
            string q = txtSearch.Text.Trim();
            binding = true;
            try
            {
                gridAssign.Rows.Clear();
                gridAssign.Enabled = r != null;
                foreach (var st in stores.Where(st => q.Length == 0 || st.StoreCode.Contains(q, StringComparison.OrdinalIgnoreCase) || st.StoreName.Contains(q, StringComparison.OrdinalIgnoreCase)).OrderBy(st => st.StoreName))
                {
                    bool on = r != null && r.PlacesList.Any(p => MJdatabase.PlaceIsStore(p, st));
                    int i = gridAssign.Rows.Add(on, st.StoreCode, st.StoreName, on ? "✔ On this route" : "Available");
                    gridAssign.Rows[i].Tag = st;
                    gridAssign.Rows[i].Height = gridAssign.RowTemplate.Height;
                }
            }
            finally { binding = false; }
        }

        private void ToggleStore(int rowIndex)
        {
            if (isPending()) { TogglePendingStore(rowIndex); return; } // Nov 6: client not saved yet
            var r = SelectedRoute();
            if (r == null || !(gridAssign.Rows[rowIndex].Tag is ClientStore st)) return;
            bool include = gridAssign.Rows[rowIndex].Cells["colOn"].Value is bool b && b;
            try
            {
                MJdatabase.SetStoreOnRoute(r, st, include);
                Say(include ? $"✔ {st.StoreName} is now on route {r.RouteCode}." : $"{st.StoreName} was taken off route {r.RouteCode}.", false);
                reloadStores();
                BeginInvoke(new Action(() => LoadRoutes(r.RouteCode)));
            }
            catch (Exception ex) { Say("✕ " + ex.Message, true); BeginInvoke(new Action(() => LoadRoutes(r.RouteCode))); }
        }
    }
}
