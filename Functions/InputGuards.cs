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
        /// <summary>Nov 17: the length of a Philippine mobile number (09XXXXXXXXX).</summary>
        public const int MobileDigits = 11;

        /// <summary>
        /// Nov 17: contact number boxes take a mobile number of 11 digits only (09XXXXXXXXX): letters, spaces and signs can't be typed,
        /// pasted text keeps its digits ("+63 917 123 4567" becomes 09171234567) and anything past 11 digits is cut off.
        /// </summary>
        public static void MobileNumber(params TextBox?[] boxes)
        {
            foreach (var t in boxes)
            {
                if (t == null) continue;
                t.MaxLength = MobileDigits;
                t.KeyPress -= BlockNonDigits;
                t.KeyPress += BlockNonDigits;
                if (string.IsNullOrEmpty(t.PlaceholderText) || t.PlaceholderText.Contains(' ') || t.PlaceholderText.Contains('-')) t.PlaceholderText = "09XXXXXXXXX";
                bool busy = false;
                t.TextChanged += (s, e) =>
                {
                    if (busy) return;
                    string clean = CleanMobile(t.Text);
                    if (clean == t.Text) return;
                    busy = true;
                    try
                    {
                        int caret = t.SelectionStart;
                        t.Text = clean;
                        t.SelectionStart = Math.Min(caret, t.TextLength);
                    }
                    finally { busy = false; }
                };
                string first = CleanMobile(t.Text);
                if (first != t.Text) t.Text = first;
            }
        }

        /// <summary>Digits only, a leading +63 / 63 turned into 0, at most 11 digits.</summary>
        public static string CleanMobile(string? text)
        {
            string raw = (text ?? "").Trim();
            string digits = new string(raw.Where(char.IsDigit).ToArray());
            if ((raw.StartsWith("+63") || (digits.StartsWith("63") && digits.Length == 12)) && digits.StartsWith("63")) digits = "0" + digits.Substring(2);
            return digits.Length > MobileDigits ? digits.Substring(0, MobileDigits) : digits;
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
        /// <summary>Grid columns that hold a person's name: typing is limited to letters, spaces and the marks . - ' , and a name with numbers can't be committed.</summary>
        public static void PersonNameColumns(DataGridView grid, params string[] columnNames)
        {
            var names = new HashSet<string>(columnNames, StringComparer.OrdinalIgnoreCase);
            void BlockNonLetters(object? s, KeyPressEventArgs e)
            {
                if (char.IsControl(e.KeyChar) || char.IsLetter(e.KeyChar) || " .-',".IndexOf(e.KeyChar) >= 0) return;
                e.Handled = true;
            }
            grid.EditingControlShowing += (s, e) =>
            {
                if (e.Control is not TextBox tb) return;
                tb.KeyPress -= BlockNonLetters;
                var col = grid.CurrentCell?.OwningColumn;
                if (col != null && names.Contains(col.Name)) tb.KeyPress += BlockNonLetters;
            };
            grid.CellValidating += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || !names.Contains(grid.Columns[e.ColumnIndex].Name)) return;
                string v = (e.FormattedValue?.ToString() ?? "").Trim();
                grid.Rows[e.RowIndex].ErrorText = "";
                if (v.Length > 0 && v.Any(char.IsDigit))
                {
                    grid.Rows[e.RowIndex].ErrorText = $"{grid.Columns[e.ColumnIndex].HeaderText.Replace("✎", "").Trim()} must be a person's name: letters only, no numbers.";
                    e.Cancel = true;
                }
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
                        MessageBox.Show(grid.FindForm(), $"{label} \"{v}\" is already used on row {i + 1}. Each {label.ToLowerInvariant()} must be unique.", label + " Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
