using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// A table where some text cells hold several values, one per line (e.g. DR numbers). While such a cell is being
    /// typed in, Enter starts a new line instead of leaving the cell; Tab or clicking elsewhere finishes it.
    /// </summary>
    public class MultiValueGrid : DataGridView
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public HashSet<string> MultiLineColumns { get; } = new HashSet<string>(StringComparer.Ordinal);






        private bool TryNewLine(Keys keyData)
        {
            if ((keyData & Keys.KeyCode) != Keys.Enter || (keyData & (Keys.Control | Keys.Shift | Keys.Alt)) != Keys.None) return false;
            if (!IsCurrentCellInEditMode || CurrentCell == null || CurrentCell.ColumnIndex < 0) return false;
            if (!MultiLineColumns.Contains(Columns[CurrentCell.ColumnIndex].Name) || EditingControl is not TextBox tb) return false;
            tb.SelectedText = Environment.NewLine;
            tb.ScrollToCaret();
            return true;
        }






        protected override bool ProcessDataGridViewKey(KeyEventArgs e)
        {
            if (TryNewLine(e.KeyData)) return true;
            return base.ProcessDataGridViewKey(e);
        }






        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (TryNewLine(keyData)) return true;
            return base.ProcessDialogKey(keyData);
        }
    }






    /// <summary>
    /// Wires a DR-numbers cell: shown one per line (the row grows), typed with commas or Enter between them, cleaned
    /// on leaving the cell (trimmed, no repeats), and a small "…" button beside it that opens the DR list window.
    /// The cell value is always the stored form "DR-1001, DR-1002".
    /// </summary>
    public static class DrCellSupport
    {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TextBox, object> hookedBoxesTable = new System.Runtime.CompilerServices.ConditionalWeakTable<TextBox, object>();
        private static class HookedBoxes
        {
            public static bool Contains(TextBox tb) => hookedBoxesTable.TryGetValue(tb, out _);
            public static void Add(TextBox tb) { if (!Contains(tb)) hookedBoxesTable.Add(tb, true); }
        }
        public static DataGridViewButtonColumn MakeButtonColumn(string name, int width)
        {
            var c = new DataGridViewButtonColumn
            {
                Name = name,
                HeaderText = "",
                Text = "…",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = width,
                MinimumWidth = width,
                Resizable = DataGridViewTriState.False,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ToolTipText = "Open the DR list (add or remove one DR per line)"
            };
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            c.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
            c.DefaultCellStyle.Padding = new Padding(2, 3, 2, 3);
            return c;
        }






        /// <param name="rowTitle">Name shown on the DR list window for a row (e.g. the store name).</param>
        /// <param name="changed">Called after a row's DRs were changed (typed or through the window).</param>
        /// <param name="canEdit">Whether the row's DRs may be changed now.</param>
        public static void Attach(MultiValueGrid grid, string drCol, string btnCol, Func<DataGridViewRow, string> rowTitle, Action<DataGridViewRow> changed, Func<bool>? canEdit = null)
        {
            grid.MultiLineColumns.Add(drCol);
            if (grid.Columns.Contains(drCol))
            {
                var col = grid.Columns[drCol]!;
                col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
                col.ToolTipText = "Several DR numbers: press Enter (or type a comma) between them, or click … beside the cell.";
            }
            // Row heights are set by this helper (fitted to the DR lines), not by automatic sizing: with automatic sizing a
            // height change while typing only shows after a later redraw, so the row lagged a line behind.
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != drCol || e.Value == null) return;
                e.Value = DrList.Lines(e.Value.ToString());
                e.FormattingApplied = true;
            };
            grid.CellParsing += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != drCol) return;
                e.Value = DrList.Normalize(e.Value?.ToString());
                e.ParsingApplied = true;
            };
            grid.EditingControlShowing += (s, e) =>
            {
                if (e.Control is not TextBox tb || grid.CurrentCell == null) return;
                bool isDr = grid.Columns[grid.CurrentCell.ColumnIndex].Name == drCol;
                if (isDr)
                {
                    tb.Multiline = true;
                    tb.WordWrap = true;
                    tb.AcceptsReturn = true;
                    tb.MaxLength = 4000;
                    // The table selects all the text when editing starts (after this event). In a DR list the first key
                    // would then replace every DR already there, so the cursor is put at the end once the table is done.
                    if (grid.IsHandleCreated)
                        grid.BeginInvoke(new Action(() =>
                        {
                            try { if (!tb.IsDisposed && grid.EditingControl == tb) { tb.SelectionStart = tb.TextLength; tb.SelectionLength = 0; tb.ScrollToCaret(); } }
                            catch (Exception ex) { ErrLog.Swallowed(ex); }
                        }));
                    // The row grows while lines are added (the editing box is shared by all text cells, so hook it once).
                    if (!HookedBoxes.Contains(tb))
                    {
                        HookedBoxes.Add(tb);
                        tb.TextChanged += (s2, e2) =>
                        {
                            try
                            {
                                if (tb.IsDisposed || tb.Parent == null) return;
                                if ((tb as IDataGridViewEditingControl)?.EditingControlDataGridView is not MultiValueGrid g || g.CurrentCell == null || !g.IsCurrentCellInEditMode) return;
                                if (!g.MultiLineColumns.Contains(g.Columns[g.CurrentCell.ColumnIndex].Name)) return;
                                int lines = Math.Max(1, tb.Text.Split('\n').Length);
                                int pad = (int)Math.Round(10 * g.DeviceDpi / 96f);
                                var row = g.Rows[g.CurrentCell.RowIndex];
                                int want = Math.Max(g.RowTemplate.MinimumHeight, lines * tb.Font.Height + pad);
                                if (want > row.Height) row.Height = Math.Min(want, 2000);
                            }
                            catch (Exception ex) { ErrLog.Swallowed(ex); }
                        };
                    }
                }
                else
                {
                    tb.AcceptsReturn = false;
                }
            };
            // A click on the DR cell always starts typing there (the theme can switch tables to whole-row selection).
            grid.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != drCol) return;
                var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (cell.ReadOnly || grid.ReadOnly || (canEdit != null && !canEdit())) return;
                try
                {
                    if (grid.CurrentCell != cell) grid.CurrentCell = cell;
                    if (!grid.IsCurrentCellInEditMode) grid.BeginEdit(false);
                    if (grid.EditingControl is TextBox tb) { tb.SelectionStart = tb.TextLength; tb.SelectionLength = 0; }
                }
                catch (Exception ex) { ErrLog.Swallowed(ex); }
            };
            grid.CellEndEdit += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != drCol) return;
                try
                {
                    var row = grid.Rows[e.RowIndex];
                    if (row.MinimumHeight != grid.RowTemplate.MinimumHeight) row.MinimumHeight = Math.Max(2, grid.RowTemplate.MinimumHeight);
                    grid.AutoResizeRow(e.RowIndex, DataGridViewAutoSizeRowMode.AllCells);
                }
                catch (Exception ex) { ErrLog.Swallowed(ex); }
                // Run the row's checks after the table has finished this edit: changing the cell or showing a message
                // while the edit is still being closed re-entered it and could wipe the cell.
                var doneRow = grid.Rows[e.RowIndex];
                void RunChanged()
                {
                    try
                    {
                        if (grid.IsDisposed || doneRow.DataGridView != grid || doneRow.Index < 0) return;
                        // Back in the same DR cell already: leave it alone (the save check still catches repeats).
                        if (grid.IsCurrentCellInEditMode && grid.CurrentCell != null && grid.CurrentCell.RowIndex == doneRow.Index && grid.Columns[grid.CurrentCell.ColumnIndex].Name == drCol) return;
                        changed(doneRow);
                    }
                    catch (Exception ex) { ErrLog.Swallowed(ex); }
                }
                if (grid.IsHandleCreated) grid.BeginInvoke(new Action(RunChanged)); else RunChanged();
            };
            grid.CellContentClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != btnCol) return;
                if (canEdit != null && !canEdit()) return;
                grid.EndEdit();
                var row = grid.Rows[e.RowIndex];
                string current = row.Cells[drCol].Value?.ToString() ?? "";
                using var dlg = new DrListDialog(rowTitle(row), current);
                if (dlg.ShowDialog(grid.FindForm()) != DialogResult.OK) return;
                if (DrList.Normalize(current) == dlg.DrNumbers) return;
                row.Cells[drCol].Value = dlg.DrNumbers;
                try { grid.AutoResizeRow(e.RowIndex, DataGridViewAutoSizeRowMode.AllCells); } catch (Exception ex) { ErrLog.Swallowed(ex); }
                changed(row);
            };
        }






        /// <summary>Sum of the row heights plus the header, for sizing the panel that holds the table.</summary>
        public static int ContentHeight(DataGridView grid)
        {
            int h = grid.ColumnHeadersVisible ? grid.ColumnHeadersHeight : 0;
            foreach (DataGridViewRow r in grid.Rows) if (r.Visible) h += r.Height;
            return h;
        }
    }
}
