using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>Keystroke-level guards: whole numbers only in crate fields, and duplicate-free key columns in grids.</summary>
    public static class InputGuards
    {
        private static readonly Regex UnsignedInt = new Regex(@"^\d{0,6}$");
        private static void BlockNonDigits(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
        }
        /// <summary>Text boxes and number boxes: only the digits 0-9 can be typed or pasted (no letters, signs or decimals).</summary>
        public static void DigitsOnly(params Control?[] controls)
        {
            foreach (var c in controls)
            {
                if (c == null) continue;
                c.KeyPress -= BlockNonDigits;
                c.KeyPress += BlockNonDigits;
                if (c is NumericUpDown n)
                {
                    n.DecimalPlaces = 0;
                    if (n.Minimum < 0) n.Minimum = 0;
                    // Pasted text: keep the digits only.
                    n.Validating += (s, e) =>
                    {
                        string digits = new string((n.Text ?? "").Where(char.IsDigit).ToArray());
                        if (digits != n.Text) n.Text = digits.Length == 0 ? "0" : digits;
                    };
                }
                else if (c is TextBox t)
                {
                    t.TextChanged += (s, e) =>
                    {
                        if (UnsignedInt.IsMatch(t.Text)) return;
                        int caret = t.SelectionStart;
                        string digits = new string(t.Text.Where(char.IsDigit).ToArray());
                        if (digits.Length > 6) digits = digits.Substring(0, 6);
                        t.Text = digits;
                        t.SelectionStart = Math.Min(caret, t.TextLength);
                    };
                }
            }
        }
        /// <summary>Grid columns that hold counts: typing is limited to digits, and a non-number can't be committed.</summary>
        public static void DigitsOnlyColumns(DataGridView grid, params string[] columnNames)
        {
            var names = new HashSet<string>(columnNames, StringComparer.OrdinalIgnoreCase);
            grid.EditingControlShowing += (s, e) =>
            {
                if (e.Control is not TextBox tb) return;
                tb.KeyPress -= BlockNonDigits;
                var col = grid.CurrentCell?.OwningColumn;
                if (col != null && names.Contains(col.Name)) tb.KeyPress += BlockNonDigits;
            };
            grid.CellValidating += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || !names.Contains(grid.Columns[e.ColumnIndex].Name)) return;
                string v = (e.FormattedValue?.ToString() ?? "").Trim();
                grid.Rows[e.RowIndex].ErrorText = "";
                if (!UnsignedInt.IsMatch(v))
                {
                    grid.Rows[e.RowIndex].ErrorText = $"{grid.Columns[e.ColumnIndex].HeaderText.Replace("✎", "").Trim()} must be a whole number (0 or more).";
                    e.Cancel = true;
                }
            };
            grid.CellEndEdit += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || !names.Contains(grid.Columns[e.ColumnIndex].Name)) return;
                var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (string.IsNullOrWhiteSpace(cell.Value?.ToString())) cell.Value = "0";
            };
        }
        /// <summary>A grid column whose values must be unique (case-insensitive), checked while typing.</summary>
        public static void UniqueColumn(DataGridView grid, string columnName, string label, Func<int, string, string?>? extraCheck = null)
        {
            grid.CellValidating += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != columnName) return;
                string v = (e.FormattedValue?.ToString() ?? "").Trim();
                if (v.Length == 0) return;
                for (int i = 0; i < grid.Rows.Count; i++)
                {
                    if (i == e.RowIndex || grid.Rows[i].IsNewRow) continue;
                    if (string.Equals((grid.Rows[i].Cells[columnName].Value?.ToString() ?? "").Trim(), v, StringComparison.OrdinalIgnoreCase))
                    {
                        grid.Rows[e.RowIndex].ErrorText = $"{label} {v} is already used on row {i + 1}.";
                        MessageBox.Show(grid.FindForm(), $"{label} \"{v}\" is already used on row {i + 1}. Each {label.ToLowerInvariant()} must be unique.", "Duplicate " + label.ToLowerInvariant(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        e.Cancel = true;
                        return;
                    }
                }
                string? problem = extraCheck?.Invoke(e.RowIndex, v);
                if (!string.IsNullOrEmpty(problem))
                {
                    grid.Rows[e.RowIndex].ErrorText = problem;
                    MessageBox.Show(grid.FindForm(), problem, "Check " + label.ToLowerInvariant(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
            };
        }
    }
}
