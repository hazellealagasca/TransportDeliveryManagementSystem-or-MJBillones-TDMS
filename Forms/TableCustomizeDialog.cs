using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// Lets the user choose which columns a table shows, rename them, reorder them, set their width,
    /// and (where allowed) add their own columns.
    /// </summary>
    public sealed class TableCustomizeDialog : Form
    {
        public List<GridColumnPref> Result { get; private set; } = new List<GridColumnPref>();
        private readonly List<GridColumnPref> working;
        private readonly List<GridColumnPref> defaults;
        private readonly bool allowCustom;
        private readonly DataGridView grid;
        private Button btnUp = null!, btnDown = null!, btnAdd = null!, btnRemove = null!;
        private static readonly (string Label, float Factor)[] Widths = { ("Narrow", 0.6f), ("Normal", 1f), ("Wide", 1.5f), ("Extra wide", 2.2f) };
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public TableCustomizeDialog(string title, string subtitle, List<GridColumnPref> current, List<GridColumnPref> defaults, bool allowCustom)
        {
            working = current.Select(c => c.Clone()).OrderBy(c => c.Order).ToList();
            this.defaults = defaults;
            this.allowCustom = allowCustom;
            Text = title;
            Font = new Font("Segoe UI", 9.5F);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(D(760), D(560));
            MinimumSize = new Size(D(640), D(440));
            BackColor = AppTheme.Canvas;
            KeyPreview = true;
            try { ThemeManager.StyleWindowChrome(this); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
            // Header
            var header = new Panel { Dock = DockStyle.Top, Height = D(72), BackColor = AppTheme.Surface, Padding = new Padding(D(20), D(12), D(20), D(8)) };
            header.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1); };
            var lblTitle = new Label { Dock = DockStyle.Top, Height = D(28), Text = title, Font = new Font("Segoe UI Semibold", 13F), ForeColor = AppTheme.TextPrimary };
            var lblSub = new Label { Dock = DockStyle.Top, Height = D(22), Text = subtitle, Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, AutoEllipsis = true };
            header.Controls.Add(lblSub);
            header.Controls.Add(lblTitle);
            // Footer
            var footer = new Panel { Dock = DockStyle.Bottom, Height = D(64), BackColor = AppTheme.Surface, Padding = new Padding(D(20), D(12), D(20), D(12)) };
            footer.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0); };
            var btnApply = MakeButton("✓  Apply", "primary", D(120));
            btnApply.Dock = DockStyle.Right;
            btnApply.Click += (s, e) => Apply();
            var gap = new Panel { Dock = DockStyle.Right, Width = D(8), BackColor = AppTheme.Surface };
            var btnCancel = MakeButton("Cancel", "secondary", D(100));
            btnCancel.Dock = DockStyle.Right;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            var lblHint = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, Text = "Untick to hide a column. Click a name to rename it. The layout is saved for next time." };
            footer.Controls.Add(lblHint);
            footer.Controls.Add(btnCancel);
            footer.Controls.Add(gap);
            footer.Controls.Add(btnApply);
            // Body: list of columns + side buttons
            var body = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Padding = new Padding(D(20), D(16), D(20), D(16)) };
            var side = new FlowLayoutPanel { Dock = DockStyle.Right, Width = D(190), FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = AppTheme.Canvas, Padding = new Padding(D(14), 0, 0, 0) };
            btnUp = MakeButton("↑  Move up", "secondary", D(170));
            btnUp.Click += (s, e) => MoveRow(-1);
            btnDown = MakeButton("↓  Move down", "secondary", D(170));
            btnDown.Click += (s, e) => MoveRow(1);
            btnAdd = MakeButton("＋  Add custom column", "primary", D(170));
            btnAdd.Click += (s, e) => AddCustom();
            btnAdd.Visible = allowCustom;
            btnRemove = MakeButton("Remove column", "danger", D(170));
            btnRemove.Click += (s, e) => RemoveCustom();
            btnRemove.Visible = allowCustom;
            var btnReset = MakeButton("↺  Reset to default", "secondary", D(170));
            btnReset.Click += (s, e) => ResetDefaults();
            var lblCustomNote = new Label
            {
                Width = D(170),
                Height = D(90),
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = AppTheme.TextMuted,
                Text = allowCustom ? "Custom columns are your own fields (e.g. Region, Payment terms). You can type values into them in the table." : "",
                Margin = new Padding(0, D(6), 0, 0)
            };
            foreach (var b in new Control[] { btnUp, btnDown, Spacer(), btnAdd, btnRemove, lblCustomNote, Spacer(), btnReset })
            {
                if (b is Button) b.Margin = new Padding(0, 0, 0, D(8));
                side.Controls.Add(b);
            }
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppTheme.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EditMode = DataGridViewEditMode.EditOnEnter,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = D(36),
                Font = new Font("Segoe UI", 9.5F),
                EnableHeadersVisualStyles = false
            };
            UIThemeHelper.ApplyGridTheme(grid);
            grid.RowTemplate.Height = D(34);
            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Show", HeaderText = "Show", FillWeight = 40, MinimumWidth = D(56) });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Column name", FillWeight = 160 });
            var widthCol = new DataGridViewComboBoxColumn { Name = "Width", HeaderText = "Width", FillWeight = 80, DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton, FlatStyle = FlatStyle.Flat };
            foreach (var w in Widths) widthCol.Items.Add(w.Label);
            grid.Columns.Add(widthCol);
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Kind", HeaderText = "Type", FillWeight = 70, ReadOnly = true });
            grid.CurrentCellDirtyStateChanged += (s, e) => { if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewCheckBoxCell or DataGridViewComboBoxCell) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            grid.CellValueChanged += (s, e) => PullRow(e.RowIndex);
            grid.CellValidating += (s, e) =>
            {
                if (filling || e.RowIndex < 0 || grid.Columns[e.ColumnIndex].Name != "Name") return;
                string typed = (e.FormattedValue?.ToString() ?? "").Trim();
                grid.Rows[e.RowIndex].ErrorText = "";
                if (typed.Length == 0) return;
                bool clash = working.Where((w, i) => i != e.RowIndex).Any(w => string.Equals(w.Header.Trim(), typed, StringComparison.OrdinalIgnoreCase));
                if (clash)
                {
                    grid.Rows[e.RowIndex].ErrorText = $"\"{typed}\" is already used by another column.";
                    MessageBox.Show(this, $"There's already a column called \"{typed}\". Column names must be unique.", "Duplicate column", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
            };
            grid.SelectionChanged += (s, e) => UpdateButtons();
            grid.DataError += (s, e) => e.ThrowException = false;
            body.Controls.Add(grid);
            body.Controls.Add(side);
            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(header);
            AcceptButton = null;
            CancelButton = btnCancel;
            Fill(0);
        }
        private Control Spacer() => new Panel { Width = D(10), Height = D(10), BackColor = AppTheme.Canvas };
        private Button MakeButton(string text, string role, int width)
        {
            var b = new Button { Text = text, Width = width, Height = D(36), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.25F), Cursor = Cursors.Hand, UseMnemonic = false };
            b.FlatAppearance.BorderSize = 0;
            UIThemeHelper.SetButtonRole(b, role);
            return b;
        }
        private static string WidthLabel(GridColumnPref p)
        {
            float baseW = p.DefaultWeight > 0 ? p.DefaultWeight : 100;
            float f = (p.Weight > 0 ? p.Weight : baseW) / baseW;
            return Widths.OrderBy(w => Math.Abs(w.Factor - f)).First().Label;
        }
        private bool filling;
        private void Fill(int selectIndex)
        {
            filling = true;
            grid.Rows.Clear();
            foreach (var p in working)
            {
                int r = grid.Rows.Add(p.Locked || p.Visible, p.Header, WidthLabel(p), p.Custom ? "Custom" : (p.Locked ? "Built-in (always shown)" : "Built-in"));
                if (p.Locked) grid.Rows[r].Cells["Show"].ReadOnly = true;
                if (p.Custom) grid.Rows[r].Cells["Kind"].Style.ForeColor = AppTheme.AccentText;
            }
            filling = false;
            if (grid.Rows.Count > 0)
            {
                selectIndex = Math.Clamp(selectIndex, 0, grid.Rows.Count - 1);
                grid.ClearSelection();
                grid.Rows[selectIndex].Selected = true;
                grid.CurrentCell = grid.Rows[selectIndex].Cells["Name"];
            }
            UpdateButtons();
        }
        private void PullRow(int r)
        {
            if (filling || r < 0 || r >= working.Count) return;
            var p = working[r];
            var row = grid.Rows[r];
            p.Visible = p.Locked || (row.Cells["Show"].Value is bool b && b);
            string name = (row.Cells["Name"].Value?.ToString() ?? "").Trim();
            p.Header = name.Length == 0 ? (p.Custom ? "Custom" : p.DefaultHeader) : name;
            string wl = row.Cells["Width"].Value?.ToString() ?? "Normal";
            float factor = Widths.FirstOrDefault(w => w.Label == wl).Factor;
            if (factor <= 0) factor = 1;
            p.Weight = (p.DefaultWeight > 0 ? p.DefaultWeight : 100) * factor;
        }
        private int SelectedIndex => grid.CurrentRow?.Index ?? -1;
        private void UpdateButtons()
        {
            int i = SelectedIndex;
            btnUp.Enabled = i > 0;
            btnDown.Enabled = i >= 0 && i < working.Count - 1;
            btnRemove.Enabled = i >= 0 && i < working.Count && working[i].Custom;
        }
        private void MoveRow(int step)
        {
            grid.EndEdit();
            int i = SelectedIndex;
            int j = i + step;
            if (i < 0 || j < 0 || j >= working.Count) return;
            (working[i], working[j]) = (working[j], working[i]);
            Fill(j);
        }
        private void AddCustom()
        {
            grid.EndEdit();
            string name = PromptName("Add custom column", "Column name (e.g. Region, Payment terms, Store manager):", "");
            if (string.IsNullOrWhiteSpace(name)) return;
            if (working.Any(w => string.Equals(w.Header.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                QuietBox.Show(this, $"There's already a column called \"{name}\".", "Add custom column", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            working.Add(new GridColumnPref
            {
                Key = GridLayoutStore.NewCustomKey(working),
                Header = name.Trim(),
                DefaultHeader = name.Trim(),
                Visible = true,
                Custom = true,
                Weight = 100,
                DefaultWeight = 100
            });
            Fill(working.Count - 1);
        }
        private void RemoveCustom()
        {
            int i = SelectedIndex;
            if (i < 0 || i >= working.Count || !working[i].Custom) return;
            var r = MessageBox.Show(this, $"Remove the column \"{working[i].Header}\"?\n\nValues typed into it will no longer be shown.", "Remove column", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;
            working.RemoveAt(i);
            Fill(i);
        }
        private void ResetDefaults()
        {
            var keepCustom = working.Where(w => w.Custom).ToList();
            var r = keepCustom.Count > 0
                ? MessageBox.Show(this, "Reset the built-in columns to their original names, order and widths?\n\nYes keeps your custom columns (moved to the end); No removes them too.", "Reset to default", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)
                : (MessageBox.Show(this, "Are you sure you want to reset the columns to their original names, order and widths?", "Reset to default", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes ? DialogResult.Yes : DialogResult.Cancel);
            if (r == DialogResult.Cancel) return;
            working.Clear();
            working.AddRange(defaults.Select(d => d.Clone()));
            if (r == DialogResult.Yes) working.AddRange(keepCustom);
            Fill(0);
        }
        private void Apply()
        {
            grid.EndEdit();
            for (int i = 0; i < working.Count; i++) { PullRow(i); working[i].Order = i; }
            if (!working.Any(w => w.Visible || w.Locked))
            {
                QuietBox.Show(this, "Keep at least one column visible.", "Customize table", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (working.Any(w => string.IsNullOrWhiteSpace(w.Header)))
            {
                MessageBox.Show(this, "Every column needs a name. Type a name for the blank column first.", "Customize table", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var dups = working.GroupBy(w => w.Header.Trim(), StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dups.Count > 0)
            {
                MessageBox.Show(this, "Column names must be unique. These appear more than once:\n\n• " + string.Join("\n• ", dups) + "\n\nRename one of each before applying.", "Duplicate columns", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Result = working;
            DialogResult = DialogResult.OK;
            Close();
        }
        private string PromptName(string title, string label, string value)
        {
            using var f = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(D(420), D(150)),
                BackColor = AppTheme.Surface,
                Font = Font
            };
            try { ThemeManager.StyleWindowChrome(f); } catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
            var lbl = new Label { Text = label, Location = new Point(D(18), D(16)), Size = new Size(D(384), D(22)), ForeColor = AppTheme.TextSecondary };
            var txt = new TextBox { Text = value, Location = new Point(D(18), D(44)), Size = new Size(D(384), D(28)), Font = new Font("Segoe UI", 10.5F) };
            var ok = MakeButton("Add", "primary", D(96));
            ok.Location = new Point(D(306), D(96));
            ok.DialogResult = DialogResult.OK;
            var cancel = MakeButton("Cancel", "secondary", D(96));
            cancel.Location = new Point(D(202), D(96));
            cancel.DialogResult = DialogResult.Cancel;
            f.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
            f.AcceptButton = ok;
            f.CancelButton = cancel;
            return f.ShowDialog(this) == DialogResult.OK ? txt.Text.Trim() : "";
        }
    }
}
