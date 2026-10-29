using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// The right-click menu used across the app's tables (same design as the dispatch list): a header naming the
    /// record and its status, the actions that fit that record, then Copy / Print / Find / Export submenus, and
    /// destructive actions last in red. The menu is rebuilt every time it opens so it always matches the row.
    /// </summary>
    public static class RowMenu
    {
        /// <summary>
        /// Wires a table: right-click keeps a multi-row selection (or selects the clicked row), the menu key and
        /// Shift+F10 open the menu for the highlighted row, and <paramref name="build"/> fills the menu.
        /// </summary>
        public static void Attach(DataGridView grid, Action<ContextMenuStrip, List<DataGridViewRow>> build, bool allowMultiSelect = false)
        {
            ContextMenuStrip? current = null;
            if (allowMultiSelect && grid.SelectionMode == DataGridViewSelectionMode.FullRowSelect) grid.MultiSelect = true;
            grid.ContextMenuStrip = null;
            void Open(Point at)
            {
                var rows = grid.SelectedRows.Cast<DataGridViewRow>()
                    .Concat(grid.SelectedCells.Cast<DataGridViewCell>().Select(c => c.OwningRow).Where(r => r != null).Select(r => r!))
                    .Where(r => !r.IsNewRow).Distinct().OrderBy(r => r.Index).ToList();
                if (rows.Count == 0 && grid.CurrentRow != null) rows.Add(grid.CurrentRow);
                if (rows.Count == 0) return;
                current?.Dispose();
                current = new ContextMenuStrip { Font = new Font("Segoe UI", 9.5F), ShowImageMargin = false };
                try { build(current, rows); } catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[RowMenu] {ex.Message}"); ErrLog.Swallowed(ex); }
                if (current.Items.Count > 0) current.Show(grid, at);
            }
            grid.CellMouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
                var row = grid.Rows[e.RowIndex];
                bool rowMode = grid.SelectionMode == DataGridViewSelectionMode.FullRowSelect || grid.SelectionMode == DataGridViewSelectionMode.RowHeaderSelect;
                bool alreadyPicked = rowMode ? row.Selected : (e.ColumnIndex >= 0 && row.Cells[e.ColumnIndex].Selected);
                if (!alreadyPicked)
                {
                    try { grid.EndEdit(); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
                    grid.ClearSelection();
                    int col = e.ColumnIndex >= 0 && grid.Columns[e.ColumnIndex].Visible ? e.ColumnIndex
                        : grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).Select(c => c.Index).DefaultIfEmpty(-1).First();
                    try { if (col >= 0) grid.CurrentCell = row.Cells[col]; } catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
                    try { if (rowMode) row.Selected = true; } catch (System.Exception swx3) { ErrLog.Swallowed(swx3); }
                }
                Open(grid.PointToClient(Cursor.Position));
            };
            grid.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Apps || (e.KeyCode == Keys.F10 && e.Shift))
                {
                    var r = grid.CurrentRow;
                    var rect = r != null ? grid.GetRowDisplayRectangle(r.Index, false) : Rectangle.Empty;
                    Open(new Point(rect.Left + 40, rect.Bottom));
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
        }
        public static ToolStripItem[] Header(string title, string sub)
        {
            var items = new List<ToolStripItem>
            {
                new ToolStripLabel(Esc(title)) { Font = new Font("Segoe UI Semibold", 10F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(8, 6, 8, 0) }
            };
            if (!string.IsNullOrWhiteSpace(sub))
                items.Add(new ToolStripLabel(Esc(sub)) { Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, Margin = new Padding(8, 0, 8, 4) });
            items.Add(new ToolStripSeparator());
            return items.ToArray();
        }
        public static ToolStripMenuItem Item(string text, Action act, string shortcut = "", bool enabled = true, Color? color = null)
        {
            var it = new ToolStripMenuItem(Esc(text)) { ShortcutKeyDisplayString = shortcut, Enabled = enabled };
            if (color.HasValue) it.ForeColor = color.Value;
            it.Click += (s, e) =>
            {
                try { act(); }
                catch (Exception ex) { ErrLog.Error("RowMenu", ex);  MessageBox.Show(ex.Message, "Something went wrong", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            return it;
        }
        public static ToolStripMenuItem Danger(string text, Action act, bool enabled = true) => Item(text, act, "", enabled, AppTheme.DangerText);
        public static ToolStripMenuItem Good(string text, Action act, bool enabled = true) => Item(text, act, "", enabled, AppTheme.SuccessText);
        public static ToolStripMenuItem Sub(string text, params ToolStripItem?[] items)
        {
            var it = new ToolStripMenuItem(Esc(text));
            it.DropDown.Font = new Font("Segoe UI", 9.5F);
            if (it.DropDown is ToolStripDropDownMenu ddm) ddm.ShowImageMargin = false;
            it.DropDownItems.AddRange(items.Where(x => x != null).Select(x => x!).ToArray());
            it.Enabled = it.DropDownItems.Count > 0;
            return it;
        }
        public static ToolStripSeparator Sep() => new ToolStripSeparator();
        /// <summary>"📋 Copy ▸" with one entry per non-empty value.</summary>
        public static ToolStripMenuItem CopySub(Control owner, params (string Label, string? Value)[] values)
            => Sub("📋  Copy", values.Where(v => !string.IsNullOrWhiteSpace(v.Value))
                .Select(v => (ToolStripItem)Item(v.Label, () => Copy(owner, v.Value!, $"{v.Label} copied"))).ToArray());
        /// <summary>"📊 Export table ▸" (CSV, Excel, print the table).</summary>
        public static ToolStripMenuItem ExportSub(DataGridView grid, string fileBase, string title, Action? print = null)
        {
            var items = new List<ToolStripItem>
            {
                Item("To CSV…", () => DataExportHelper.ExportToCsv(grid, fileBase, title)),
                Item("To Excel (.xml)…", () => DataExportHelper.ExportToExcelXml(grid, fileBase, title))
            };
            if (print != null) items.Add(Item("Print the table", print));
            return Sub("📊  Export table", items.ToArray());
        }
        public static void Copy(Control owner, string text, string toast)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            try { Clipboard.SetText(text); ToastHelper.ShowToast(owner, toast, ToastType.Success); } catch (System.Exception swx4) { ErrLog.Swallowed(swx4); }
        }
        /// <summary>Puts text in a table's search box (filters the list), e.g. "other orders for this client".</summary>
        public static void Search(TextBox box, string text)
        {
            box.Text = (text ?? "").Trim();
            box.Focus();
            box.SelectionStart = box.Text.Length;
        }
        /// <summary>A plain menu for read-only logs: copy the row or the cell, then export.</summary>
        public static void AttachBasic(DataGridView grid, string fileBase, string title, Action? print = null)
        {
            Attach(grid, (m, rows) =>
            {
                var r = rows[0];
                var cols = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();
                string RowText(DataGridViewRow row) => string.Join("\t", cols.Select(c => row.Cells[c.Index].FormattedValue?.ToString() ?? ""));
                string first = cols.Count > 0 ? r.Cells[cols[0].Index].FormattedValue?.ToString() ?? "" : "";
                m.Items.AddRange(Header(Short(first, 48), rows.Count > 1 ? $"{rows.Count} rows selected" : title));
                var cell = grid.CurrentCell;
                if (cell != null && cell.RowIndex == r.Index)
                {
                    string cv = cell.FormattedValue?.ToString() ?? "";
                    if (cv.Length > 0) m.Items.Add(Item($"Copy \"{Short(cv, 30)}\"", () => Copy(grid, cv, "Copied")));
                }
                m.Items.Add(Item(rows.Count > 1 ? $"Copy {rows.Count} rows" : "Copy row", () =>
                    Copy(grid, string.Join(Environment.NewLine, new[] { string.Join("\t", cols.Select(c => c.HeaderText)) }.Concat(rows.Select(RowText))), "Rows copied"), "Ctrl+C"));
                m.Items.Add(Sep());
                m.Items.Add(ExportSub(grid, fileBase, title, print));
            }, allowMultiSelect: true);
        }
        /// <summary>Finds a dispatch by id (or by its number) and opens the dispatch details window.</summary>
        public static void OpenDispatch(Control owner, int deliveryId, string? dispatchNumber)
        {
            var d = FindDelivery(deliveryId, dispatchNumber);
            if (d == null)
            {
                QuietBox.Show($"Dispatch {dispatchNumber} could not be found. It may have been deleted.", "Dispatch not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Forms.DispatchDetailsDialog.ShowFor(owner, d);
        }
        public static Models.Delivery? FindDelivery(int deliveryId, string? dispatchNumber)
        {
            Models.Delivery? d = null;
            try { if (deliveryId > 0) d = Database.MJdatabase.GetDeliveryById(deliveryId); } catch (System.Exception swx5) { ErrLog.Swallowed(swx5); }
            if (d == null && !string.IsNullOrWhiteSpace(dispatchNumber))
            {
                string num = dispatchNumber.Trim();
                try { d = Database.MJdatabase.GetDeliveries().FirstOrDefault(x => x.DispatchNumber.Equals(num, StringComparison.OrdinalIgnoreCase)); } catch (System.Exception swx6) { ErrLog.Swallowed(swx6); }
            }
            return d;
        }
        public static bool Is(string? status, params string[] values) => values.Any(v => string.Equals((status ?? "").Trim(), v, StringComparison.OrdinalIgnoreCase));
        public static string Short(string? s, int max) => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max - 1) + "…");
        public static string Money(decimal v) => $"₱{v:N2}";
        private static string Esc(string s) => (s ?? "").Replace("&", "&&");
    }
}
