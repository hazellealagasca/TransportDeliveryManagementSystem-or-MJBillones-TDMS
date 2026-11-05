using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>A table whose key presses can be looked at first (used by the store lookup list: Up / Down / Enter / Tab / Esc).</summary>
    public class LookupGrid : DataGridView
    {
        /// <summary>Return true to say the key was handled.</summary>
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Func<Keys, bool>? KeyHook { get; set; }
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (KeyHook != null && KeyHook(keyData)) return true;
            return base.ProcessDialogKey(keyData);
        }
        protected override bool ProcessDataGridViewKey(KeyEventArgs e)
        {
            if (KeyHook != null && KeyHook(e.KeyData)) return true;
            return base.ProcessDataGridViewKey(e);
        }
    }



    /// <summary>
    /// Keeps the Store Code, Store Name and Route cells of a store table tied to the selected client's registered stores:
    /// <list type="bullet">
    /// <item>No client selected: the store cells can't be typed in (an inline warning says why).</item>
    /// <item>Typing either cell shows a floating list (Code | Name | Route) of the client's matching stores.</item>
    /// <item>Picking or typing a known code or name fills in the other two cells; a code and name of different stores can't stay together.</item>
    /// <item>Unknown stores, another client's stores and stores already in the table are refused (the cell goes back to what it was), with an offer to register an unknown store.</item>
    /// <item>The same checks run when pasting, when the client changes (rows are re-checked and marked) and before saving.</item>
    /// </list>
    /// </summary>
    public sealed class StoreEntryGuard
    {
        private readonly LookupGrid grid;
        private readonly string codeCol, nameCol;
        private readonly string? routeCol;
        private readonly Func<string> client;
        private readonly Func<string?> defaultRoute;
        private readonly Action<string?> inlineWarning;
        private readonly StoreLookupPopup popup = new StoreLookupPopup();
        private readonly Dictionary<DataGridViewRow, ClientStore> picked = new Dictionary<DataGridViewRow, ClientStore>();
        private readonly HashSet<TextBox> hookedBoxes = new HashSet<TextBox>();
        private List<ClientStore> cache = new List<ClientStore>();
        private string cacheClient = "\0";
        private int cacheVersion = -1;
        private string beginValue = "";
        private bool syncing;
        private Form? hookedForm;
        private TextBox? activeBox;
        /// <summary>Called after a row's store was filled in, changed or cleared.</summary>
        public event Action<DataGridViewRow>? RowChanged;
        /// <summary>Optional: does this row hold data (e.g. crates) even without a store? Such rows can't be saved without a store.</summary>
        public Func<DataGridViewRow, bool>? RowHasData { get; set; }



        public StoreEntryGuard(LookupGrid grid, string codeColumn, string nameColumn, string? routeColumn, Func<string> clientName, Func<string?> defaultRouteLabel, Action<string?> showInlineWarning)
        {
            this.grid = grid;
            codeCol = codeColumn;
            nameCol = nameColumn;
            routeCol = routeColumn;
            client = clientName;
            defaultRoute = defaultRouteLabel;
            inlineWarning = showInlineWarning;
            grid.KeyHook = OnKey;
            grid.CellBeginEdit += Grid_CellBeginEdit;
            grid.EditingControlShowing += Grid_EditingControlShowing;
            grid.CellEndEdit += Grid_CellEndEdit;
            grid.KeyDown += Grid_KeyDown;
            grid.Scroll += (s, e) => popup.HideList();
            grid.Leave += (s, e) => popup.HideList();
            grid.Disposed += (s, e) => { try { popup.Dispose(); } catch (Exception ex) { ErrLog.Swallowed(ex); } };
            popup.Picked += Popup_Picked;
        }



        private string Client => StoreDirectory.Norm(client());
        private bool IsStoreCol(int col) => col >= 0 && col < grid.Columns.Count && (grid.Columns[col].Name == codeCol || grid.Columns[col].Name == nameCol);
        private string Val(DataGridViewRow r, string col) => grid.Columns.Contains(col) ? StoreDirectory.Norm(r.Cells[col].Value?.ToString()) : "";



        /// <summary>The selected client's registered stores (loaded again when the client changes or after <see cref="Refresh"/>).</summary>
        public List<ClientStore> Stores()
        {
            string c = Client;
            if (!string.Equals(c, cacheClient, StringComparison.OrdinalIgnoreCase) || cacheVersion != AppEvents.Version) // rule 16e: reload after any store/route change
            {
                cacheClient = c;
                cacheVersion = AppEvents.Version;
                try { cache = c.Length == 0 ? new List<ClientStore>() : MJdatabase.GetClientStores(c); }
                catch (Exception ex) { ErrLog.Swallowed(ex); cache = new List<ClientStore>(); }
            }
            return cache;
        }
        /// <summary>Forget the loaded stores (a store was registered elsewhere).</summary>
        public void Refresh() => cacheClient = "\0";



        // ------------------------------------------------------------------ typing
        private void Grid_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || !IsStoreCol(e.ColumnIndex)) return;
            if (Client.Length == 0)
            {
                e.Cancel = true;
                WarnNoClient();
                return;
            }
            inlineWarning(null);
            beginValue = StoreDirectory.Norm(grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString());
        }



        private void WarnNoClient()
        {
            inlineWarning(StoreDirectory.NeedClientMessage);
            try { ToastHelper.ShowToast(grid.FindForm() ?? (Control)grid, StoreDirectory.NeedClientMessage, ToastType.Warning); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }



        private void Grid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is not TextBox tb || grid.CurrentCell == null) return;
            bool store = IsStoreCol(grid.CurrentCell.ColumnIndex);
            activeBox = store ? tb : null;
            if (store)
            {
                tb.AutoCompleteMode = AutoCompleteMode.None;
                tb.MaxLength = Math.Max(tb.MaxLength > 0 ? Math.Min(tb.MaxLength, 200) : 200, MJdatabase.StoreNameMaxLength);
            }
            if (hookedBoxes.Add(tb))
            {
                tb.TextChanged += (s, a) => { if (activeBox == tb && tb.Focused) ShowList(tb.Text); };
                tb.LostFocus += (s, a) => { if (grid.IsHandleCreated) grid.BeginInvoke(new Action(() => { if (!tb.Focused) popup.HideList(); })); };
            }
            popup.HideList(); // the list appears once typing starts
        }



        private void ShowList(string typed)
        {
            if (grid.CurrentCell == null || !grid.IsCurrentCellInEditMode || !IsStoreCol(grid.CurrentCell.ColumnIndex)) { popup.HideList(); return; }
            HookForm();
            var rect = grid.GetCellDisplayRectangle(grid.CurrentCell.ColumnIndex, grid.CurrentCell.RowIndex, false);
            if (rect.IsEmpty) { popup.HideList(); return; }
            var screen = grid.RectangleToScreen(rect);
            popup.ShowMatches(grid, screen, StoreDirectory.Search(Stores(), typed));
        }



        private void HookForm()
        {
            var f = grid.FindForm();
            if (f == null || f == hookedForm) return;
            if (hookedForm != null) { hookedForm.Move -= FormMoved; hookedForm.Deactivate -= FormMoved; hookedForm.Resize -= FormMoved; }
            hookedForm = f;
            f.Move += FormMoved; f.Deactivate += FormMoved; f.Resize += FormMoved;
        }
        private void FormMoved(object? sender, EventArgs e) => popup.HideList();



        private bool OnKey(Keys keyData)
        {
            if (!grid.IsCurrentCellInEditMode || grid.CurrentCell == null || !IsStoreCol(grid.CurrentCell.ColumnIndex)) return false;
            Keys k = keyData & Keys.KeyCode;
            if ((keyData & (Keys.Control | Keys.Alt | Keys.Shift)) != Keys.None) return false;
            if (!popup.Visible)
            {
                if (k != Keys.Enter) return false;
                // Enter in a store cell finishes it here (moving on), so the store is checked right away.
                FinishCell();
                return true;
            }
            switch (k)
            {
                case Keys.Down: return popup.MoveSelection(1);
                case Keys.Up: return popup.MoveSelection(-1);
                case Keys.Escape: popup.HideList(); return true;
                case Keys.Enter:
                case Keys.Tab:
                    var st = popup.ShowsNoMatch ? null : popup.Selected;
                    if (st != null) PutInEditor(st);
                    popup.HideList();
                    if (k == Keys.Tab) return false; // the table moves to the next cell and finishes the edit; the row is filled in then
                    FinishCell();
                    return true;
            }
            return false;
        }



        private void PutInEditor(ClientStore st)
        {
            if (grid.CurrentCell == null) return;
            var row = grid.Rows[grid.CurrentCell.RowIndex];
            picked[row] = st;
            if (grid.EditingControl is TextBox tb)
            {
                bool code = grid.Columns[grid.CurrentCell.ColumnIndex].Name == codeCol;
                activeBox = null; // no list for this change
                tb.Text = StoreDirectory.Norm(code ? st.StoreCode : st.StoreName);
                activeBox = tb;
            }
        }



        private void Popup_Picked(ClientStore st)
        {
            if (grid.CurrentCell == null || !grid.IsCurrentCellInEditMode) return;
            PutInEditor(st);
            try { grid.EndEdit(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }



        private void Grid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            popup.HideList();
            activeBox = null;
            if (syncing || e.RowIndex < 0 || !IsStoreCol(e.ColumnIndex)) return;
            var row = grid.Rows[e.RowIndex];
            string colName = grid.Columns[e.ColumnIndex].Name;
            string typed = StoreDirectory.Norm(row.Cells[e.ColumnIndex].Value?.ToString());
            string before = beginValue;
            // Checked after the table has finished this edit (changing cells or showing a window during it re-enters it).
            void Run() { try { Resolve(row, colName, typed, before); } catch (Exception ex) { ErrLog.Error("StoreEntryGuard", ex); } }
            if (grid.IsHandleCreated) grid.BeginInvoke(new Action(Run)); else Run();
        }



        // ------------------------------------------------------------------ paste
        private void Grid_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!(e.Control && e.KeyCode == Keys.V) || grid.IsCurrentCellInEditMode || grid.CurrentCell == null || !IsStoreCol(grid.CurrentCell.ColumnIndex)) return;
            e.Handled = e.SuppressKeyPress = true;
            if (grid.CurrentCell.ReadOnly) return;
            if (Client.Length == 0) { WarnNoClient(); return; }
            string clip = "";
            try { if (Clipboard.ContainsText()) clip = Clipboard.GetText(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            string first = (clip ?? "").Split(new[] { '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            var row = grid.Rows[grid.CurrentCell.RowIndex];
            string colName = grid.Columns[grid.CurrentCell.ColumnIndex].Name;
            Resolve(row, colName, StoreDirectory.Norm(first), Val(row, colName));
        }



        // ------------------------------------------------------------------ checking a typed value
        private void Resolve(DataGridViewRow row, string colName, string typed, string before)
        {
            if (row.DataGridView != grid || row.Index < 0) return;
            picked.Remove(row, out var chosen);
            string c = Client;
            if (c.Length == 0) { SetCell(row, colName, before); WarnNoClient(); return; }
            if (typed.Length == 0)
            {
                // Clearing the code or the name takes the store off this row.
                if (before.Length > 0 || Val(row, codeCol).Length > 0 || Val(row, nameCol).Length > 0) ClearRow(row);
                return;
            }
            bool isCode = colName == codeCol;
            ClientStore? store = chosen;
            if (store == null)
            {
                var chk = StoreDirectory.Check(c, Stores(), isCode ? typed : null, isCode ? null : typed);
                if (!chk.Ok)
                {
                    popup.HideList();
                    SetCell(row, colName, before); // never left half-typed: the cell goes back to what it was
                    bool canRegister = chk.Problem == StoreProblem.NotRegistered || (chk.Problem == StoreProblem.OtherClient && !isCode);
                    string msg = chk.Message + (chk.Problem == StoreProblem.OtherClient && isCode ? " Store codes can't be shared between clients." : "");
                    if (!UnregisteredStorePrompt.Ask(grid.FindForm(), msg, canRegister)) { FocusCell(row, colName); return; }
                    using var dlg = new RegisterStoreDialog(c, isCode ? typed : "", isCode ? "" : typed, defaultRoute());
                    if (dlg.ShowDialog(grid.FindForm()) != DialogResult.OK || dlg.RegisteredStore == null) { FocusCell(row, colName); return; }
                    Refresh();
                    store = Stores().FirstOrDefault(s => s.Id == dlg.RegisteredStore.Id) ?? dlg.RegisteredStore;
                    try { ToastHelper.ShowToast(grid.FindForm() ?? (Control)grid, $"Store {store.StoreCode} - {store.StoreName} registered for {c}.", ToastType.Success); } catch (Exception ex) { ErrLog.Swallowed(ex); }
                }
                else store = chk.Store!;
            }
            var other = RowWithStore(store, row);
            if (other != null)
            {
                popup.HideList();
                SetCell(row, colName, before);
                HighlightRow(other);
                MessageBox.Show(grid.FindForm(), $"\"{StoreDirectory.Norm(store.StoreName)}\" ({StoreDirectory.Norm(store.StoreCode)}) is already in this table (row {other.Index + 1}, highlighted). Change its crates there instead.", "Store Already Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
                FocusCell(row, colName);
                return;
            }
            FillRow(row, store);
        }



        private DataGridViewRow? RowWithStore(ClientStore st, DataGridViewRow except)
        {
            foreach (DataGridViewRow r in grid.Rows)
            {
                if (r == except || r.IsNewRow) continue;
                string rc = Val(r, codeCol), rn = Val(r, nameCol);
                if ((rc.Length > 0 && StoreDirectory.Same(rc, st.StoreCode)) || (rc.Length == 0 && rn.Length > 0 && StoreDirectory.Same(rn, st.StoreName))) return r;
            }
            return null;
        }



        /// <summary>
        /// Finishes typing in the current store cell by moving to the next cell of the row that can be typed in (the
        /// table keeps a cell in typing mode while it stays selected, so moving on is what makes it finish and be checked).
        /// In the last such cell the value is checked straight away.
        /// </summary>
        private void FinishCell()
        {
            var cur = grid.CurrentCell;
            if (cur == null) return;
            var row = grid.Rows[cur.RowIndex];
            var next = grid.Columns.GetNextColumn(grid.Columns[cur.ColumnIndex], DataGridViewElementStates.Visible, DataGridViewElementStates.None);
            while (next != null && (next.ReadOnly || next is DataGridViewButtonColumn)) next = grid.Columns.GetNextColumn(next, DataGridViewElementStates.Visible, DataGridViewElementStates.None);
            try
            {
                if (next != null) { grid.CurrentCell = row.Cells[next.Index]; return; }
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                string colName = grid.Columns[cur.ColumnIndex].Name;
                string typed = StoreDirectory.Norm(cur.Value?.ToString()), before = beginValue;
                popup.HideList();
                if (grid.IsHandleCreated) grid.BeginInvoke(new Action(() => { try { Resolve(row, colName, typed, before); } catch (Exception ex) { ErrLog.Error("StoreEntryGuard", ex); } }));
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        /// <summary>
        /// After this row's store cells were set in code: if one of them is being typed in, show the new value in the
        /// typing box too (otherwise finishing that cell later would put the old text back).
        /// </summary>
        private void RefreshCurrentStoreEdit(DataGridViewRow? row)
        {
            try
            {
                if (!grid.IsCurrentCellInEditMode || grid.CurrentCell == null || !IsStoreCol(grid.CurrentCell.ColumnIndex)) return;
                if (row != null && grid.CurrentCell.RowIndex != row.Index) return;
                popup.HideList();
                var box = activeBox;
                activeBox = null; // no lookup list for this change
                syncing = true;
                try { grid.RefreshEdit(); } finally { syncing = false; activeBox = box; }
                beginValue = StoreDirectory.Norm(grid.CurrentCell.Value?.ToString());
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }



        private void SetCell(DataGridViewRow row, string col, string value)
        {
            if (!grid.Columns.Contains(col) || row.DataGridView != grid) return;
            syncing = true;
            try { row.Cells[col].Value = value.Length == 0 ? null : value; } finally { syncing = false; }
            RefreshCurrentStoreEdit(row);
        }



        /// <summary>Puts the registered store's code, name and route on the row (the official spelling).</summary>
        public void FillRow(DataGridViewRow row, ClientStore st)
        {
            if (row.DataGridView != grid) return;
            syncing = true;
            try
            {
                row.Cells[codeCol].Value = StoreDirectory.Norm(st.StoreCode);
                row.Cells[nameCol].Value = StoreDirectory.Norm(st.StoreName);
                if (routeCol != null && grid.Columns.Contains(routeCol)) row.Cells[routeCol].Value = StoreDirectory.Route(st);
                Mark(row, null);
            }
            finally { syncing = false; }
            RefreshCurrentStoreEdit(row);
            RowChanged?.Invoke(row);
        }



        private void ClearRow(DataGridViewRow row)
        {
            syncing = true;
            try
            {
                row.Cells[codeCol].Value = null;
                row.Cells[nameCol].Value = null;
                if (routeCol != null && grid.Columns.Contains(routeCol)) row.Cells[routeCol].Value = null;
                Mark(row, null);
            }
            finally { syncing = false; }
            RefreshCurrentStoreEdit(row);
            RowChanged?.Invoke(row);
        }



        private void Mark(DataGridViewRow row, string? problem)
        {
            foreach (var col in new[] { codeCol, nameCol })
            {
                if (!grid.Columns.Contains(col)) continue;
                var cell = row.Cells[col];
                cell.ErrorText = problem ?? "";
                cell.Style.BackColor = problem == null ? Color.Empty : AppTheme.DangerSoft;
                cell.Style.ForeColor = problem == null ? Color.Empty : AppTheme.DangerText;
            }
        }



        private void FocusCell(DataGridViewRow row, string col)
        {
            try { if (row.DataGridView == grid && grid.Columns.Contains(col)) grid.CurrentCell = row.Cells[col]; } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }



        private void HighlightRow(DataGridViewRow row)
        {
            try
            {
                // Scrolled into view and flashed; the cell isn't selected (that would start typing in it).
                if (!row.Displayed)
                {
                    try { grid.FirstDisplayedScrollingRowIndex = Math.Max(0, row.Index); } catch (Exception sx) { ErrLog.Swallowed(sx); }
                }
                var old = row.DefaultCellStyle.BackColor;
                row.DefaultCellStyle.BackColor = AppTheme.WarningSoft;
                var t = new System.Windows.Forms.Timer { Interval = 1800 };
                t.Tick += (s, e) => { t.Stop(); t.Dispose(); if (row.DataGridView == grid) row.DefaultCellStyle.BackColor = old; };
                t.Start();
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }



        // ------------------------------------------------------------------ whole-table checks
        /// <summary>
        /// Checks every row against the selected client: known rows get the official code, name and route; rows that
        /// don't belong (unknown, another client's, code and name of different stores, or listed twice) are marked red.
        /// Returns the problems found, by row.
        /// </summary>
        public List<(DataGridViewRow Row, string Problem)> RevalidateAll(bool showToast)
        {
            var problems = new List<(DataGridViewRow, string)>();
            string c = Client;
            popup.HideList();
            if (c.Length == 0)
            {
                // No client yet: nothing to check the rows against (saving asks for the client first).
                foreach (DataGridViewRow r in grid.Rows) if (!r.IsNewRow) Mark(r, null);
                return problems;
            }
            var stores = Stores();
            var seen = new Dictionary<int, DataGridViewRow>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                string code = Val(row, codeCol), name = Val(row, nameCol);
                if (code.Length == 0 && name.Length == 0)
                {
                    Mark(row, null);
                    if (routeCol != null && grid.Columns.Contains(routeCol) && Val(row, routeCol).Length > 0) { syncing = true; try { row.Cells[routeCol].Value = null; } finally { syncing = false; } }
                    if (RowHasData != null && RowHasData(row)) problems.Add((row, $"Row {row.Index + 1} has crates but no store."));
                    continue;
                }
                var chk = StoreDirectory.Check(c, stores, code, name);
                if (!chk.Ok)
                {
                    string msg = chk.Problem == StoreProblem.NoClient ? StoreDirectory.NeedClientMessage : chk.Message;
                    Mark(row, msg);
                    problems.Add((row, $"Row {row.Index + 1}: {msg}"));
                    continue;
                }
                if (seen.TryGetValue(chk.Store!.Id, out var first))
                {
                    string msg = $"\"{StoreDirectory.Norm(chk.Store.StoreName)}\" is already on row {first.Index + 1}.";
                    Mark(row, msg);
                    problems.Add((row, $"Row {row.Index + 1}: {msg}"));
                    continue;
                }
                seen[chk.Store.Id] = row;
                // Official spelling and route (no change event: nothing about the store changed).
                syncing = true;
                try
                {
                    string oc = StoreDirectory.Norm(chk.Store.StoreCode), on = StoreDirectory.Norm(chk.Store.StoreName);
                    if (code != oc) row.Cells[codeCol].Value = oc;
                    if (name != on) row.Cells[nameCol].Value = on;
                    if (routeCol != null && grid.Columns.Contains(routeCol)) { string rt = StoreDirectory.Route(chk.Store); if (Val(row, routeCol) != rt) row.Cells[routeCol].Value = rt; }
                    Mark(row, null);
                }
                finally { syncing = false; }
            }
            RefreshCurrentStoreEdit(null);
            if (showToast && problems.Count > 0)
            {
                string who = c.Length > 0 ? c : "the selected client";
                try { ToastHelper.ShowToast(grid.FindForm() ?? (Control)grid, $"{problems.Count} store row{(problems.Count == 1 ? "" : "s")} don't match {who}'s stores — marked in red. Fix or remove {(problems.Count == 1 ? "it" : "them")} before saving.", ToastType.Warning); }
                catch (Exception ex) { ErrLog.Swallowed(ex); }
            }
            return problems;
        }



        /// <summary>Final check before saving: every row must be one of the selected client's stores, listed once.</summary>
        public bool ValidateForSave(IWin32Window? owner)
        {
            try { grid.EndEdit(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            bool anyStore = grid.Rows.Cast<DataGridViewRow>().Any(r => !r.IsNewRow && (Val(r, codeCol).Length > 0 || Val(r, nameCol).Length > 0 || (RowHasData != null && RowHasData(r))));
            if (anyStore && Client.Length == 0)
            {
                inlineWarning(StoreDirectory.NeedClientMessage);
                MessageBox.Show(owner, StoreDirectory.NeedClientMessage, "Store details", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            var problems = RevalidateAll(showToast: false);
            if (problems.Count == 0) return true;
            string list = string.Join("\n", problems.Take(6).Select(p => "• " + p.Problem)) + (problems.Count > 6 ? $"\n• …and {problems.Count - 6} more" : "");
            MessageBox.Show(owner, "These store rows can't be saved:\n\n" + list + "\n\nPick each store from the list (or register it), or remove the row.", "Store not registered", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            FocusCell(problems[0].Row, Val(problems[0].Row, codeCol).Length == 0 ? codeCol : nameCol);
            return false;
        }
    }



    /// <summary>
    /// The same store lookup for a pair of text boxes (store code + store name) in a window: the floating list while
    /// typing, picking fills both boxes, and <see cref="Confirm"/> checks the pair (with the register offer) before applying.
    /// </summary>
    public sealed class TextBoxStoreLookup
    {
        private readonly TextBox code, name;
        private readonly Func<string> client;
        private readonly Func<string?> defaultRoute;
        private readonly StoreLookupPopup popup = new StoreLookupPopup();
        private List<ClientStore> cache = new List<ClientStore>();
        private string cacheClient = "\0";
        private int cacheVersion = -1;
        private bool filling;
        private TextBox? listFor;
        public TextBoxStoreLookup(TextBox codeBox, TextBox nameBox, Func<string> clientName, Func<string?> defaultRouteLabel)
        {
            code = codeBox; name = nameBox; client = clientName; defaultRoute = defaultRouteLabel;
            foreach (var tb in new[] { code, name })
            {
                var box = tb;
                box.AutoCompleteMode = AutoCompleteMode.None;
                box.TextChanged += (s, e) => { if (!filling && box.Focused && !box.ReadOnly) Show(box); };
                box.Enter += (s, e) => { if (!box.ReadOnly && StoreDirectory.Norm(client()).Length == 0) ToastHelper.ShowToast(box.FindForm() ?? (Control)box, StoreDirectory.NeedClientMessage, ToastType.Warning); };
                box.LostFocus += (s, e) => { if (box.IsHandleCreated) box.BeginInvoke(new Action(() => { if (!code.Focused && !name.Focused) popup.HideList(); })); };
                box.PreviewKeyDown += (s, e) => { if (popup.Visible && (e.KeyCode == Keys.Tab || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Down || e.KeyCode == Keys.Up || e.KeyCode == Keys.Escape)) e.IsInputKey = true; };
                box.KeyDown += (s, e) =>
                {
                    if (!popup.Visible || listFor != box) return;
                    switch (e.KeyCode)
                    {
                        case Keys.Down: popup.MoveSelection(1); e.Handled = e.SuppressKeyPress = true; break;
                        case Keys.Up: popup.MoveSelection(-1); e.Handled = e.SuppressKeyPress = true; break;
                        case Keys.Escape: popup.HideList(); e.Handled = e.SuppressKeyPress = true; break;
                        case Keys.Enter:
                        case Keys.Tab:
                            var st = popup.ShowsNoMatch ? null : popup.Selected;
                            popup.HideList();
                            e.Handled = e.SuppressKeyPress = true;
                            if (st != null) Fill(st);
                            box.FindForm()?.SelectNextControl(box == code ? code : name, true, true, true, true);
                            break;
                    }
                };
                box.Disposed += (s, e) => { try { popup.Dispose(); } catch (Exception ex) { ErrLog.Swallowed(ex); } };
            }
            popup.Picked += Fill;
        }
        private List<ClientStore> Stores()
        {
            string c = StoreDirectory.Norm(client());
            if (!string.Equals(c, cacheClient, StringComparison.OrdinalIgnoreCase) || cacheVersion != AppEvents.Version) // rule 16e: reload after any store/route change
            {
                cacheClient = c;
                cacheVersion = AppEvents.Version;
                try { cache = c.Length == 0 ? new List<ClientStore>() : MJdatabase.GetClientStores(c); }
                catch (Exception ex) { ErrLog.Swallowed(ex); cache = new List<ClientStore>(); }
            }
            return cache;
        }
        private void Show(TextBox box)
        {
            if (StoreDirectory.Norm(client()).Length == 0) { popup.HideList(); return; }
            listFor = box;
            var both = box == code ? code.Bounds : name.Bounds;
            var rect = box.Parent?.RectangleToScreen(both) ?? box.RectangleToScreen(box.ClientRectangle);
            popup.ShowMatches(box, rect, StoreDirectory.Search(Stores(), box.Text));
        }
        private void Fill(ClientStore st)
        {
            filling = true;
            try { code.Text = StoreDirectory.Norm(st.StoreCode); name.Text = StoreDirectory.Norm(st.StoreName); }
            finally { filling = false; }
        }



        /// <summary>
        /// Checks the code and name before they are applied. Unknown store: offers to register it. Returns the store,
        /// or null when the window should stay open. <paramref name="alreadyListed"/> refuses a store already in the table.
        /// </summary>
        public ClientStore? Confirm(IWin32Window owner, Func<ClientStore, string?>? alreadyListed)
        {
            popup.HideList();
            string c = StoreDirectory.Norm(client());
            if (c.Length == 0) { MessageBox.Show(owner, StoreDirectory.NeedClientMessage, "Store details", MessageBoxButtons.OK, MessageBoxIcon.Warning); return null; }
            string tc = StoreDirectory.Norm(code.Text), tn = StoreDirectory.Norm(name.Text);
            if (tc.Length == 0 && tn.Length == 0) { MessageBox.Show(owner, "Enter the store code or store name (pick it from the list).", "Store details", MessageBoxButtons.OK, MessageBoxIcon.Warning); name.Focus(); return null; }
            var chk = StoreDirectory.Check(c, Stores(), tc, tn);
            ClientStore? st = chk.Store;
            if (chk.Problem == StoreProblem.Mismatch)
            {
                MessageBox.Show(owner, chk.Message, "Store code and name don't match", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                code.Focus(); code.SelectAll();
                return null;
            }
            if (!chk.Ok)
            {
                bool codeTyped = tc.Length > 0;
                bool canRegister = chk.Problem == StoreProblem.NotRegistered || (chk.Problem == StoreProblem.OtherClient && !codeTyped);
                string msg = chk.Message + (chk.Problem == StoreProblem.OtherClient && codeTyped ? " Store codes can't be shared between clients." : "");
                if (!UnregisteredStorePrompt.Ask(owner, msg, canRegister)) return null;
                using var dlg = new RegisterStoreDialog(c, tc, tn, defaultRoute());
                if (dlg.ShowDialog(owner) != DialogResult.OK || dlg.RegisteredStore == null) return null;
                cacheClient = "\0";
                st = dlg.RegisteredStore;
            }
            if (st == null) return null;
            string? listed = alreadyListed?.Invoke(st);
            if (listed != null) { MessageBox.Show(owner, listed, "Store Already Added", MessageBoxButtons.OK, MessageBoxIcon.Information); return null; }
            Fill(st);
            return st;
        }
    }
}
