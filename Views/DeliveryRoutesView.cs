using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Delivery routes as one editable table (the same way the client's "Stores &amp; branches" table works):
    /// type straight into the cells, click the Stores cell to pick and order the client's stores, then Save changes.
    /// New routes start completely blank, nothing is selected on its own, and routes are archived, never deleted.
    /// </summary>
    public class DeliveryRoutesView : UserControl, IRefreshable
    {
        private sealed class RouteRow
        {
            public DeliveryRoute Route { get; }
            public bool Dirty { get; set; }
            public RouteRow(DeliveryRoute r, bool dirty = false) { Route = r; Dirty = dirty; }
            public bool IsNew => Route.Id <= 0;
        }



        private readonly List<RouteRow> rows = new List<RouteRow>();
        private List<string> clientNames = new List<string>();
        private DataGridView gridRoutes = null!;
        private TextBox txtGridSearch = null!;
        private Label lblGridHeader = null!;
        private Label lblStatus = null!;
        private Button btnNewRoute = null!;
        private Button btnSaveAll = null!;
        private Button btnBackToSourceGrid = null!;
        private bool binding;
        private bool isLoadingRoutes;
        private readonly ToolTip tips = new ToolTip { InitialDelay = 400, ShowAlways = true };
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);



        [System.ComponentModel.Browsable(false)][System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public Action? OnNavigateBack { get; set; }
        [System.ComponentModel.Browsable(false)][System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public string ReturnTargetName { get; set; } = "Dispatch";



        public DeliveryRoutesView()
        {
            InitializeCustomComponents();
            UIThemeHelper.ApplyTheme(this);
            StyleAfterTheme();
            SetupContextMenu();
            _ = LoadRoutesAsync();
        }




        // ------------------------------------------------------------------ layout
        private Button MakeButton(string text, string role)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(0, D(36)),
                Padding = new Padding(D(12), 0, D(12), 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F),
                Cursor = Cursors.Hand,
                UseMnemonic = false,
                Margin = new Padding(0, D(4), D(8), D(4))
            };
            b.FlatAppearance.BorderSize = 0;
            UIThemeHelper.SetButtonRole(b, role);
            return b;
        }



        private void InitializeCustomComponents()
        {
            SuspendLayout();
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Canvas;
            Font = new Font("Segoe UI", 9.5F);
            Padding = new Padding(D(12));



            var card = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(D(20), D(16), D(20), D(16)) };
            card.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Resize += (s, e) => card.Invalidate();



            var header = new Panel { Dock = DockStyle.Top, Height = D(156), BackColor = AppTheme.Surface, Padding = new Padding(0, 0, 0, D(8)) };
            lblGridHeader = new Label { Dock = DockStyle.Top, AutoSize = false, Height = D(36), Text = "Delivery routes", Font = new Font("Segoe UI Semibold", 14F), ForeColor = AppTheme.TextPrimary, UseMnemonic = false };
            var lblSub = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = D(30),
                AutoEllipsis = true,
                Text = "Type straight into the table. Click a route's Stores cell to pick its stores from the client's master list and set the delivery order. Nothing is saved until you click Save changes.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppTheme.TextMuted,
                Padding = new Padding(0, D(2), 0, D(8)),
                UseMnemonic = false
            };

            var flow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = D(54), WrapContents = false, BackColor = AppTheme.Surface };
            txtGridSearch = new TextBox { PlaceholderText = "🔍 Search code, route, client or store…", Width = D(300), Font = new Font("Segoe UI", 10F), Margin = new Padding(0, D(8), D(12), D(4)) };
            txtGridSearch.TextChanged += (s, e) => ApplySearch();
            btnNewRoute = MakeButton("➕  New route", "primary");
            btnNewRoute.Click += (s, e) => AddNewRouteRow();
            tips.SetToolTip(btnNewRoute, "Adds a blank row at the top. Fill in the client, code, route name and stores, then Save changes.");
            btnSaveAll = MakeButton("💾  Save changes", "strong");
            btnSaveAll.Click += (s, e) => SaveAllChanges();
            tips.SetToolTip(btnSaveAll, "Checks every new or changed route and saves them.");
            var btnExport = MakeButton("📊 Export CSV", "secondary");
            btnExport.Click += (s, e) => DataExportHelper.ExportToCsv(gridRoutes, "Delivery_Routes", "Delivery routes");
            var btnPrint = MakeButton("🖨️ Print", "secondary");
            btnPrint.Click += (s, e) => PrintHelper.PrintGridReport("Delivery Route Directory", gridRoutes, "Delivery routes and the stores each one serves, in delivery order.");
            btnBackToSourceGrid = MakeButton("⬅ Back to dispatch", "secondary");
            btnBackToSourceGrid.Visible = false;
            btnBackToSourceGrid.Click += (s, e) => { if (ConfirmLeave()) OnNavigateBack?.Invoke(); };
            flow.Controls.Add(txtGridSearch);
            flow.Controls.Add(btnNewRoute);
            flow.Controls.Add(btnSaveAll);
            flow.Controls.Add(btnBackToSourceGrid);
            flow.Controls.Add(btnExport);
            flow.Controls.Add(btnPrint);
            lblStatus = new Label { Dock = DockStyle.Top, Height = D(22), Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false };
            header.Controls.Add(lblStatus);
            header.Controls.Add(flow);
            header.Controls.Add(lblSub);
            header.Controls.Add(lblGridHeader);



            gridRoutes = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppTheme.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = D(38),
                // Not EditOnEnter: getting focus while the tab was still being shown started an edit with no editor and the
                // next repaint failed (NullReferenceException in PaintPrivate). A click or a keystroke starts editing instead.
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
                Font = new Font("Segoe UI", 9.5F),
                EnableHeadersVisualStyles = false,
                ShowCellToolTips = true
            };
            gridRoutes.RowTemplate.Height = D(36);
            gridRoutes.Columns.Add(new DataGridViewComboBoxColumn { Name = "colClient", HeaderText = "Client", FillWeight = 140, MinimumWidth = D(150), DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton, FlatStyle = FlatStyle.Flat });
            gridRoutes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Route code", FillWeight = 60, MinimumWidth = D(90), MaxInputLength = 12, DefaultCellStyle = { Font = new Font("Segoe UI Semibold", 9.5F) } });
            gridRoutes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Route name / destination", FillWeight = 140, MinimumWidth = D(160) });
            var colStores = new DataGridViewTextBoxColumn { Name = "colStores", HeaderText = "Stores (click to pick, in delivery order)", FillWeight = 260, MinimumWidth = D(220), ReadOnly = true };
            gridRoutes.Columns.Add(colStores);
            gridRoutes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDesc", HeaderText = "Description / service area", FillWeight = 140, MinimumWidth = D(140) });
            var colArchive = new DataGridViewButtonColumn { Name = "colArchive", HeaderText = "", Text = "Archive", UseColumnTextForButtonValue = true, FillWeight = 45, MinimumWidth = D(84), FlatStyle = FlatStyle.Flat };
            colArchive.DefaultCellStyle.ForeColor = AppTheme.TextSecondary;
            colArchive.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
            gridRoutes.Columns.Add(colArchive);
            gridRoutes.DataError += (s, e) => { e.ThrowException = false; };
            gridRoutes.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (gridRoutes.IsCurrentCellDirty && gridRoutes.CurrentCell is DataGridViewComboBoxCell) gridRoutes.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            gridRoutes.CellValueChanged += Grid_CellValueChanged;
            gridRoutes.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string col = gridRoutes.Columns[e.ColumnIndex].Name;
                if (col == "colStores") EditStores(e.RowIndex);
                else if (col != "colArchive" && !gridRoutes.Columns[e.ColumnIndex].ReadOnly && !gridRoutes.IsCurrentCellInEditMode) gridRoutes.BeginEdit(true);
            };
            gridRoutes.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && gridRoutes.Columns[e.ColumnIndex].Name == "colArchive") ArchiveRow(e.RowIndex);
            };
            gridRoutes.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.CellStyle == null || gridRoutes.Rows[e.RowIndex].Tag is not RouteRow rr) return;
                if (rr.Dirty && gridRoutes.Columns[e.ColumnIndex].Name != "colArchive") e.CellStyle.BackColor = AppTheme.WarningSoft;
            };
            gridRoutes.CellEndEdit += (s, e) => { if (e.RowIndex >= 0) gridRoutes.Rows[e.RowIndex].ErrorText = ""; };
            gridRoutes.EditingControlShowing += (s, e) =>
            {
                if (e.Control is TextBox tb && gridRoutes.CurrentCell?.OwningColumn?.Name == "colCode")
                {
                    tb.CharacterCasing = CharacterCasing.Upper;
                    tb.KeyPress -= CodeKeyPress;
                    tb.KeyPress += CodeKeyPress;
                }
                else if (e.Control is TextBox other)
                {
                    other.CharacterCasing = CharacterCasing.Normal;
                    other.KeyPress -= CodeKeyPress;
                }
            };



            card.Controls.Add(gridRoutes);
            card.Controls.Add(header);
            Controls.Add(card);
            ResumeLayout(false);
        }




        private static void CodeKeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsLetterOrDigit(e.KeyChar) && e.KeyChar != '-') e.Handled = true;
        }



        private void StyleAfterTheme()
        {
            UIThemeHelper.SetButtonRole(btnNewRoute, "primary");
            UIThemeHelper.SetButtonRole(btnSaveAll, "strong");
            gridRoutes.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.SurfaceAlt;
            gridRoutes.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextSecondary;
            gridRoutes.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
        }




        // ------------------------------------------------------------------ data
        public async void RefreshData()
        {
            // Don't throw away routes that are being typed in.
            if (rows.Any(r => r.Dirty)) return;
            await LoadRoutesAsync();
        }




        public async System.Threading.Tasks.Task LoadRoutesAsync()
        {
            if (isLoadingRoutes) return;
            isLoadingRoutes = true;
            bool handedOff = false;
            try
            {
                // Read everything off the screen's thread, then fill the table ON the screen's thread. The load can be
                // started before the screen has a window (from the constructor), when the continuation may run on a
                // background thread; filling the table from there corrupted the shared drawing state and the next table
                // to repaint failed (NullReferenceException in DataGridView...Cell.PaintPrivate).
                var (all, clients) = await System.Threading.Tasks.Task.Run(() =>
                {
                    var routes = MJdatabase.GetDeliveryRoutes(false);
                    List<string> names;
                    try { names = MJdatabase.GetClients().Where(c => c.IsActive).Select(c => c.CompanyName).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList(); }
                    catch (Exception ex) { ErrLog.Swallowed(ex); names = new List<string>(); }
                    return (routes, names);
                });
                void Apply()
                {
                    if (IsDisposed) return;
                    clientNames = clients;
                    rows.Clear();
                    // Archived routes are listed in Master Data → Archive.
                    foreach (var r in all.Where(r => r.IsActive).OrderBy(r => r.ClientCompanyName, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.RouteCode, StringComparer.OrdinalIgnoreCase))
                        rows.Add(new RouteRow(r));
                    BindGrid();
                }
                if (!InvokeRequired) Apply();
                else if (IsHandleCreated) { handedOff = true; BeginInvoke(new Action(() => { try { Apply(); } finally { isLoadingRoutes = false; } })); }
                else { handedOff = true; void Once(object? s, EventArgs e) { HandleCreated -= Once; try { Apply(); } finally { isLoadingRoutes = false; } } HandleCreated += Once; }
            }
            catch (Exception ex)
            {
                ErrLog.Error("DeliveryRoutesView", ex);
                if (!InvokeRequired) MessageBox.Show("Failed to load delivery routes: " + ex.Message, "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { if (!handedOff) isLoadingRoutes = false; }
        }



        private void BindGrid()
        {
            binding = true;
            try
            {
                if (gridRoutes.Columns["colClient"] is DataGridViewComboBoxColumn cc)
                {
                    cc.Items.Clear();
                    cc.Items.Add("");
                    foreach (var n in clientNames) cc.Items.Add(n);
                    foreach (var r in rows)
                        if (!string.IsNullOrWhiteSpace(r.Route.ClientCompanyName) && !cc.Items.Contains(r.Route.ClientCompanyName)) cc.Items.Add(r.Route.ClientCompanyName);
                }
                // Leave edit mode before the rows are replaced: the grid edits on focus, and replacing the rows under an open
                // editor made the next repaint fail (NullReferenceException in DataGridViewTextBoxCell.PaintPrivate).
                if (gridRoutes.IsCurrentCellInEditMode) { gridRoutes.CancelEdit(); gridRoutes.EndEdit(); }
                gridRoutes.CurrentCell = null;
                gridRoutes.Rows.Clear();
                foreach (var rr in rows)
                {
                    int i = gridRoutes.Rows.Add(rr.Route.ClientCompanyName ?? "", rr.Route.RouteCode, rr.Route.RouteName, StoresText(rr.Route), rr.Route.Description, "Archive");
                    gridRoutes.Rows[i].Tag = rr;
                    gridRoutes.Rows[i].Cells["colStores"].ToolTipText = rr.Route.PlacesList.Count == 0 ? "Click to pick this route's stores" : string.Join(Environment.NewLine, rr.Route.PlacesList.Select((p, n) => $"{n + 1}. {p}"));
                }
                // Nothing is picked on its own.
                gridRoutes.ClearSelection();
                gridRoutes.CurrentCell = null;
            }
            finally { binding = false; }
            ApplySearch();
            UpdateCounts();
        }



        private static string StoresText(DeliveryRoute r)
        {
            var p = r.PlacesList;
            return p.Count == 0 ? "＋ Pick stores…" : $"{p.Count} · {string.Join(" ➔ ", p)}";
        }



        private void ApplySearch()
        {
            string q = (txtGridSearch.Text ?? "").Trim();
            if (gridRoutes.IsCurrentCellInEditMode) gridRoutes.EndEdit();
            gridRoutes.CurrentCell = null;
            foreach (DataGridViewRow row in gridRoutes.Rows)
            {
                if (row.Tag is not RouteRow rr) continue;
                var r = rr.Route;
                bool show = rr.IsNew || rr.Dirty || q.Length == 0 ||
                    (r.RouteCode ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (r.RouteName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (r.ClientCompanyName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (r.Description ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (r.ConnectedPlaces ?? "").Contains(q, StringComparison.OrdinalIgnoreCase);
                row.Visible = show;
            }
            UpdateCounts();
        }



        private void UpdateCounts()
        {
            int saved = rows.Count(r => !r.IsNew);
            int shown = gridRoutes.Rows.Cast<DataGridViewRow>().Count(r => r.Visible && r.Tag is RouteRow rr && !rr.IsNew);
            lblGridHeader.Text = shown == saved ? $"Delivery routes ({saved})" : $"Delivery routes ({shown} of {saved})";
            int pending = rows.Count(r => r.Dirty);
            lblStatus.Text = pending == 0 ? "All changes saved." : $"● {pending} route{(pending == 1 ? "" : "s")} not saved yet — click Save changes.";
            lblStatus.ForeColor = pending == 0 ? AppTheme.TextMuted : AppTheme.WarningText;
        }



        private void Grid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (binding || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (gridRoutes.Rows[e.RowIndex].Tag is not RouteRow rr) return;
            string col = gridRoutes.Columns[e.ColumnIndex].Name;
            string v = (gridRoutes.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "").Trim();
            switch (col)
            {
                case "colClient": rr.Route.ClientCompanyName = v; break;
                case "colCode": rr.Route.RouteCode = v.ToUpperInvariant(); break;
                case "colName": rr.Route.RouteName = System.Text.RegularExpressions.Regex.Replace(v, @"\s+", " "); break;
                case "colDesc": rr.Route.Description = v; break;
                default: return;
            }
            rr.Dirty = true;
            gridRoutes.InvalidateRow(e.RowIndex);
            UpdateCounts();
        }



        // ------------------------------------------------------------------ actions
        private void AddNewRouteRow(string? presetCode = null)
        {
            if (gridRoutes.IsCurrentCellInEditMode) gridRoutes.EndEdit();
            // Re-use an untouched blank row instead of stacking empty ones.
            var blank = rows.FirstOrDefault(r => r.IsNew && string.IsNullOrWhiteSpace(r.Route.RouteCode) && string.IsNullOrWhiteSpace(r.Route.RouteName) && r.Route.PlacesList.Count == 0);
            if (blank == null)
            {
                // Completely blank: no client, code or stores are filled in for you.
                blank = new RouteRow(new DeliveryRoute { ClientCompanyName = "", RouteCode = presetCode ?? "", RouteName = "", Description = "", IsActive = true }, dirty: true);
                rows.Insert(0, blank);
                BindGrid();
            }
            for (int i = 0; i < gridRoutes.Rows.Count; i++)
            {
                if (ReferenceEquals(gridRoutes.Rows[i].Tag, blank))
                {
                    gridRoutes.Rows[i].Visible = true;
                    gridRoutes.FirstDisplayedScrollingRowIndex = i;
                    gridRoutes.CurrentCell = gridRoutes.Rows[i].Cells["colClient"];
                    gridRoutes.BeginEdit(true);
                    break;
                }
            }
        }



        private void EditStores(int rowIndex)
        {
            if (rowIndex < 0 || gridRoutes.Rows[rowIndex].Tag is not RouteRow rr) return;
            if (gridRoutes.IsCurrentCellInEditMode) gridRoutes.EndEdit();
            string client = (rr.Route.ClientCompanyName ?? "").Trim();
            if (client.Length == 0)
            {
                QuietBox.Show("Pick the route's client first. Its stores come from that client's Stores & branches list.", "Pick a client", MessageBoxButtons.OK, MessageBoxIcon.Information);
                gridRoutes.CurrentCell = gridRoutes.Rows[rowIndex].Cells["colClient"];
                gridRoutes.BeginEdit(true);
                return;
            }
            var others = rows.Where(o => !ReferenceEquals(o, rr) && string.Equals((o.Route.ClientCompanyName ?? "").Trim(), client, StringComparison.OrdinalIgnoreCase)).Select(o => o.Route).ToList();
            using var dlg = new RouteStoresDialog(client, string.IsNullOrWhiteSpace(rr.Route.RouteName) ? (rr.Route.RouteCode ?? "New route") : rr.Route.RouteName, rr.Route.PlacesList, others);
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
            string before = rr.Route.ConnectedPlaces ?? "";
            MJdatabase.SetRoutePlaces(rr.Route, dlg.Places); // canonical "CODE - NAME"; saved by SaveDeliveryRoute
            if (rr.Route.ConnectedPlaces == before) return;
            rr.Dirty = true;
            binding = true;
            try
            {
                gridRoutes.Rows[rowIndex].Cells["colStores"].Value = StoresText(rr.Route);
                gridRoutes.Rows[rowIndex].Cells["colStores"].ToolTipText = string.Join(Environment.NewLine, rr.Route.PlacesList.Select((p, n) => $"{n + 1}. {p}"));
            }
            finally { binding = false; }
            gridRoutes.InvalidateRow(rowIndex);
            UpdateCounts();
        }



        private void ArchiveRow(int rowIndex)
        {
            if (rowIndex < 0 || gridRoutes.Rows[rowIndex].Tag is not RouteRow rr) return;
            if (rr.IsNew)
            {
                rows.Remove(rr);
                BindGrid();
                return;
            }
            string label = $"{rr.Route.RouteCode} - {rr.Route.RouteName}";
            if (MessageBox.Show($"Are you sure you want to archive route \"{label}\"?\n\n{MJdatabase.DeletePolicyNote("DeliveryRoutes")}\nYou can restore it from Master Data → Archive.", "Archive route", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            try
            {
                MJdatabase.ArchiveDeliveryRoute(rr.Route.Id, label);
                rows.Remove(rr);
                BindGrid();
                ToastHelper.ShowToast(this, $"Route {rr.Route.RouteCode} archived — kept for past orders", ToastType.Info);
            }
            catch (Exception ex)
            {
                ErrLog.Error("DeliveryRoutesView", ex);
                MessageBox.Show("Couldn't archive the route: " + ex.Message, "Archive route", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        /// <summary>Save is always available; it checks every new or changed route and lists what still needs fixing.</summary>
        private void SaveAllChanges()
        {
            if (gridRoutes.IsCurrentCellInEditMode) gridRoutes.EndEdit();
            gridRoutes.CommitEdit(DataGridViewDataErrorContexts.Commit);
            foreach (DataGridViewRow row in gridRoutes.Rows) row.ErrorText = "";
            var pending = rows.Where(r => r.Dirty).ToList();
            if (pending.Count == 0)
            {
                QuietBox.Show("There are no new or changed routes to save.\n\nClick ➕ New route to add one, or type in the table to change a route.", "Save changes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var problems = new List<string>();
            (int row, string col)? first = null;
            int RowOf(RouteRow rr) { for (int i = 0; i < gridRoutes.Rows.Count; i++) if (ReferenceEquals(gridRoutes.Rows[i].Tag, rr)) return i; return -1; }
            void Flag(RouteRow rr, string col, string msg)
            {
                int i = RowOf(rr);
                string who = string.IsNullOrWhiteSpace(rr.Route.RouteCode) ? (rr.IsNew ? "New route" : "Route") : rr.Route.RouteCode;
                problems.Add($"• {who}: {msg}");
                if (i >= 0)
                {
                    gridRoutes.Rows[i].ErrorText = string.IsNullOrEmpty(gridRoutes.Rows[i].ErrorText) ? msg : gridRoutes.Rows[i].ErrorText;
                    gridRoutes.Rows[i].Visible = true;
                    first ??= (i, col);
                }
            }
            foreach (var rr in pending)
            {
                var r = rr.Route;
                if (string.IsNullOrWhiteSpace(r.ClientCompanyName)) Flag(rr, "colClient", "pick the client.");
                if (string.IsNullOrWhiteSpace(r.RouteCode)) Flag(rr, "colCode", "type a route code (e.g. R5).");
                else if (rows.Any(o => !ReferenceEquals(o, rr) && string.Equals((o.Route.RouteCode ?? "").Trim(), r.RouteCode.Trim(), StringComparison.OrdinalIgnoreCase)))
                    Flag(rr, "colCode", $"route code {r.RouteCode} is already used by another route.");
                if (string.IsNullOrWhiteSpace(r.RouteName)) Flag(rr, "colName", "type the route name / destination.");
                else if (rows.Any(o => !ReferenceEquals(o, rr) && string.Equals((o.Route.RouteName ?? "").Trim(), r.RouteName.Trim(), StringComparison.OrdinalIgnoreCase) && string.Equals((o.Route.ClientCompanyName ?? "").Trim(), (r.ClientCompanyName ?? "").Trim(), StringComparison.OrdinalIgnoreCase)))
                    Flag(rr, "colName", $"{r.ClientCompanyName} already has a route named \"{r.RouteName}\".");
                if (r.PlacesList.Count == 0) Flag(rr, "colStores", "pick at least one store (click the Stores cell).");
            }
            if (problems.Count > 0)
            {
                QuietBox.Show("These routes can't be saved yet:\n\n" + string.Join("\n", problems.Take(12)) + (problems.Count > 12 ? $"\n…and {problems.Count - 12} more" : ""), "Please check the routes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (first.HasValue)
                {
                    gridRoutes.CurrentCell = gridRoutes.Rows[first.Value.row].Cells[first.Value.col];
                    if (first.Value.col == "colStores") EditStores(first.Value.row);
                    else gridRoutes.BeginEdit(true);
                }

                return;
            }
            int news = pending.Count(p => p.IsNew), changes = pending.Count - news;
            string what = string.Join(" and ", new[] { news > 0 ? $"{news} new route{(news == 1 ? "" : "s")}" : "", changes > 0 ? $"{changes} changed route{(changes == 1 ? "" : "s")}" : "" }.Where(x => x.Length > 0));
            if (MessageBox.Show($"Are you sure you want to save {what}?", "Save changes", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            int saved = 0;
            var failures = new List<string>();
            foreach (var rr in pending)
            {
                try
                {
                    rr.Route.RouteCode = rr.Route.RouteCode.Trim();
                    rr.Route.RouteName = rr.Route.RouteName.Trim();
                    rr.Route.IsActive = true;
                    if (MJdatabase.SaveDeliveryRoute(rr.Route)) { rr.Dirty = false; saved++; }
                }
                catch (Exception ex)
                {
                    ErrLog.Error("DeliveryRoutesView", ex);
                    failures.Add($"• {rr.Route.RouteCode}: {ex.Message}");
                }
            }
            if (failures.Count > 0)
                MessageBox.Show("Some routes were not saved:\n\n" + string.Join("\n", failures), "Save changes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (saved > 0) ToastHelper.ShowToast(this, $"{saved} route{(saved == 1 ? "" : "s")} saved ✓", ToastType.Success);
            if (failures.Count == 0) _ = LoadRoutesAsync();
            else { BindGrid(); }
        }




        private bool ConfirmLeave()
        {
            if (!rows.Any(r => r.Dirty)) return true;
            var res = MessageBox.Show("Some routes are not saved yet. Leave anyway and lose those changes?", "Unsaved routes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (res != DialogResult.Yes) return false;
            foreach (var r in rows) r.Dirty = false;
            _ = LoadRoutesAsync();
            return true;
        }



        private void SetupContextMenu()
        {
            RowMenu.Attach(gridRoutes, (m, sel) =>
            {
                if (sel[0].Tag is not RouteRow rr) return;
                var r = rr.Route;
                int idx = sel[0].Index;
                string owner = string.IsNullOrWhiteSpace(r.ClientCompanyName) ? "no client" : r.ClientCompanyName;
                m.Items.AddRange(RowMenu.Header($"🗺️  {(string.IsNullOrWhiteSpace(r.RouteCode) ? "New route" : r.RouteCode)} {(string.IsNullOrWhiteSpace(r.RouteName) ? "" : "- " + r.RouteName)}", $"{RowMenu.Short(owner, 26)} · {r.TotalStops} store(s)"));
                m.Items.Add(RowMenu.Item("Pick / order stores…", () => EditStores(idx)));
                m.Items.Add(RowMenu.Item("Open client profile", () => { if (ConfirmLeave()) MainForm.Instance?.OpenClientProfile(r.ClientCompanyName); }, "", !string.IsNullOrWhiteSpace(r.ClientCompanyName)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.CopySub(this, ("Route code", r.RouteCode), ("Route name", r.RouteName), ("Stores in order", r.FormattedPlaces),
                    ("Stores, one per line", string.Join(Environment.NewLine, r.PlacesList.Select((p, i) => $"{i + 1}. {p}")))));
                m.Items.Add(RowMenu.Item("Other routes of this client", () => RowMenu.Search(txtGridSearch, r.ClientCompanyName), "", !string.IsNullOrWhiteSpace(r.ClientCompanyName)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Danger(rr.IsNew ? "Remove this blank row" : "Archive route…", () => ArchiveRow(idx)));
            });
        }



        // ------------------------------------------------------------------ navigation from other screens
        public void OpenForShortcut(string? routeCode, bool isNewRoute = false, string returnTarget = "Dispatch", Action? backAction = null)
        {
            ReturnTargetName = returnTarget;
            OnNavigateBack = backAction;
            btnBackToSourceGrid.Text = $"⬅ Back to {returnTarget}";
            btnBackToSourceGrid.Visible = backAction != null;
            if (isNewRoute)
            {
                txtGridSearch.Text = "";
                AddNewRouteRow();
            }
            else if (!string.IsNullOrWhiteSpace(routeCode))
            {
                string key = routeCode.Trim();
                var match = rows.FirstOrDefault(r =>
                    string.Equals(r.Route.RouteCode, key, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.Route.RouteName, key, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(r.Route.RouteCode) && key.StartsWith(r.Route.RouteCode + " ", StringComparison.OrdinalIgnoreCase)));
                if (match != null)
                {
                    txtGridSearch.Text = match.Route.RouteCode;
                    for (int i = 0; i < gridRoutes.Rows.Count; i++)
                        if (ReferenceEquals(gridRoutes.Rows[i].Tag, match)) { gridRoutes.CurrentCell = gridRoutes.Rows[i].Cells["colName"]; break; }
                }
                else txtGridSearch.Text = key;
            }
            else txtGridSearch.Text = "";
        }




        public void ResetNavigationBack()
        {
            OnNavigateBack = null;
            ReturnTargetName = "Dispatch";
            btnBackToSourceGrid.Visible = false;
        }
    }
}
