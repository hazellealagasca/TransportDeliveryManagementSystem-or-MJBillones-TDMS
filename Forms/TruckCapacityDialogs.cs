using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>Asks for a short reason (maintenance, decommissioning, reactivation). Returns null when cancelled.</summary>
    public static class ReasonPrompt
    {
        public static string? Ask(IWin32Window? owner, string title, string message, bool required, string okText = "Save")
        {
            float scale = 1f;
            try { if (owner is Control oc) scale = oc.DeviceDpi / 96f; } catch { }
            int D(int v) => (int)Math.Round(v * scale);
            using var f = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                AutoScaleMode = AutoScaleMode.None,
                ClientSize = new Size(D(540), D(300)),
                BackColor = AppTheme.Surface,
                Font = new Font("Segoe UI", 9.5F)
            };
            var lbl = new Label { Text = message, Dock = DockStyle.Top, Height = D(78), Padding = new Padding(D(16), D(14), D(16), 0), ForeColor = AppTheme.TextPrimary, UseMnemonic = false };
            var txt = new TextBox { Multiline = true, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), PlaceholderText = required ? "Reason (required)" : "Note (optional)", MaxLength = 400 };
            var wrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(D(16), D(6), D(16), D(6)), BackColor = AppTheme.Surface };
            wrap.Controls.Add(txt);
            var err = new Label { Dock = DockStyle.Bottom, Height = D(24), Padding = new Padding(D(16), 0, D(16), 0), ForeColor = AppTheme.DangerText, Font = new Font("Segoe UI", 8.75F) };
            var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = D(58), FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(D(12), D(8), D(12), D(8)), BackColor = AppTheme.Surface };
            var ok = new Button { Text = okText, AutoSize = true, MinimumSize = new Size(D(110), D(36)), FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "Cancel", AutoSize = true, MinimumSize = new Size(D(96), D(36)), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel, Margin = new Padding(0, 0, D(8), 0) };
            ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
            UIThemeHelper.SetButtonRole(ok, "primary");
            UIThemeHelper.SetButtonRole(cancel, "secondary");
            ok.Click += (s, e) =>
            {
                if (required && txt.Text.Trim().Length < 3) { err.Text = "Please type the reason (at least 3 characters)."; txt.Focus(); return; }
                f.DialogResult = DialogResult.OK;
            };
            bar.Controls.Add(ok);
            bar.Controls.Add(cancel);
            f.Controls.Add(wrap);
            f.Controls.Add(err);
            f.Controls.Add(bar);
            f.Controls.Add(lbl);
            f.CancelButton = cancel;
            UIThemeHelper.ApplyTheme(f);
            return f.ShowDialog(owner) == DialogResult.OK ? txt.Text.Trim() : null;
        }
    }




    /// <summary>
    /// Truck categories and their load-planning matrix (Dry / Cold / Combi crates). Saving checks the hard rules
    /// (blocks) and warns when trucks of that category will change capacity.
    /// </summary>
    public class TruckCategoryMatrixDialog : Form
    {
        private readonly DataGridView grid;
        private TextBox txtName = null!;
        private NumericUpDown numDry = null!, numCold = null!, numCombiCold = null!, numCombiDry = null!;
        private Label lblEditTitle = null!, lblRule = null!;
        private Button btnSave = null!, btnRemove = null!, btnNew = null!;
        private string? editingName;
        private List<TruckCategoryItem> categories = new List<TruckCategoryItem>();
        private List<TruckCapacity> matrix = new List<TruckCapacity>();
        public bool Changed { get; private set; }
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);




        public TruckCategoryMatrixDialog(bool startWithNew = false)
        {
            Text = "Truck categories & capacities";
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", 9.5F);
            ClientSize = new Size(D(940), D(560));
            MinimumSize = new Size(D(860), D(500));
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = AppTheme.Canvas;




            var header = new Panel { Dock = DockStyle.Top, Height = D(64), BackColor = AppTheme.Surface, Padding = new Padding(D(20), D(10), D(20), D(6)) };
            header.Controls.Add(new Label { Dock = DockStyle.Top, Height = D(22), Text = "Crates each category can carry. A truck's capacity is locked to its category and cargo type. Combi = cold part + dry part; the Dry capacity is the truck's hard maximum.", Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, AutoEllipsis = true });
            header.Controls.Add(new Label { Dock = DockStyle.Top, Height = D(28), Text = "Truck categories & capacities", Font = new Font("Segoe UI Semibold", 13F), ForeColor = AppTheme.TextPrimary });




            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(D(16)), BackColor = AppTheme.Canvas };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));




            grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, RowHeadersVisible = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = AppTheme.Surface, BorderStyle = BorderStyle.FixedSingle, EnableHeadersVisualStyles = false, ColumnHeadersHeight = D(36),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing, Margin = new Padding(0, 0, D(12), 0)
            };
            grid.RowTemplate.Height = D(32);
            grid.Columns.Add("colName", "Category");
            grid.Columns.Add("colDry", "Dry");
            grid.Columns.Add("colCold", "Cold");
            grid.Columns.Add("colCombi", "Combi");
            grid.Columns.Add("colTrucks", "Trucks");
            grid.Columns["colName"]!.FillWeight = 160;
            grid.Columns["colCombi"]!.FillWeight = 130;
            foreach (var c in new[] { "colDry", "colCold", "colCombi", "colTrucks" }) grid.Columns[c]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.SelectionChanged += (s, e) => { if (grid.Focused && grid.CurrentRow?.Tag is string n) LoadEditor(n); };
            grid.CellClick += (s, e) => { if (e.RowIndex >= 0 && grid.Rows[e.RowIndex].Tag is string n) LoadEditor(n); };
            body.Controls.Add(grid, 0, 0);




            var editor = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 9, BackColor = AppTheme.Surface, Padding = new Padding(D(14)) };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            for (int i = 0; i < 8; i++) editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            lblEditTitle = new Label { Text = "New category", AutoSize = true, Font = new Font("Segoe UI Semibold", 11F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, 0, 0, D(8)) };
            editor.Controls.Add(lblEditTitle, 0, 0); editor.SetColumnSpan(lblEditTitle, 2);
            Label Cap(string t) => new Label { Text = t, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, D(6), 0, D(2)) };
            NumericUpDown Num() { var n = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 5000, Minimum = 0, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 0, D(8), 0) }; n.ValueChanged += (s, e) => ShowRules(); return n; }
            txtName = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), PlaceholderText = "e.g. FORWARD 6 TONS", MaxLength = 60, CharacterCasing = CharacterCasing.Upper };
            txtName.TextChanged += (s, e) => ShowRules();
            var capName = Cap("Category name *");
            editor.Controls.Add(capName, 0, 1); editor.SetColumnSpan(capName, 2);
            editor.Controls.Add(txtName, 0, 2); editor.SetColumnSpan(txtName, 2);
            numDry = Num(); numCold = Num(); numCombiCold = Num(); numCombiDry = Num();
            editor.Controls.Add(Cap("Dry crates *"), 0, 3); editor.Controls.Add(Cap("Cold crates *"), 1, 3);
            editor.Controls.Add(numDry, 0, 4); editor.Controls.Add(numCold, 1, 4);
            editor.Controls.Add(Cap("Combi — cold part"), 0, 5); editor.Controls.Add(Cap("Combi — dry part"), 1, 5);
            editor.Controls.Add(numCombiCold, 0, 6); editor.Controls.Add(numCombiDry, 1, 6);
            lblRule = new Label { AutoSize = true, MaximumSize = new Size(D(320), 0), Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, Margin = new Padding(0, D(10), 0, D(6)), UseMnemonic = false };
            editor.Controls.Add(lblRule, 0, 7); editor.SetColumnSpan(lblRule, 2);
            var btns = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, BackColor = AppTheme.Surface, Margin = new Padding(0, D(8), 0, 0) };
            Button B(string t, string role, EventHandler h) { var b = new Button { Text = t, AutoSize = true, MinimumSize = new Size(0, D(34)), FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, D(8), D(6)), Padding = new Padding(D(8), 0, D(8), 0) }; b.FlatAppearance.BorderSize = 0; UIThemeHelper.SetButtonRole(b, role); b.Click += h; return b; }
            btnSave = B("💾 Save category", "primary", (s, e) => SaveCurrent());
            btnNew = B("＋ Add truck category", "secondary", (s, e) => LoadEditor(null));
            btnRemove = B("Remove", "danger", (s, e) => RemoveCurrent());
            btns.Controls.Add(btnSave); btns.Controls.Add(btnNew); btns.Controls.Add(btnRemove);
            editor.Controls.Add(btns, 0, 8); editor.SetColumnSpan(btns, 2);
            body.Controls.Add(editor, 1, 0);




            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = D(54), FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(D(16), D(8), D(16), D(8)), BackColor = AppTheme.Surface };
            var close = new Button { Text = "Close", AutoSize = true, MinimumSize = new Size(D(100), D(34)), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            close.FlatAppearance.BorderSize = 0;
            UIThemeHelper.SetButtonRole(close, "secondary");
            footer.Controls.Add(close);
            CancelButton = close;




            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(header);
            UIThemeHelper.ApplyTheme(this);
            UIThemeHelper.SetButtonRole(btnNew, "secondary");
            UIThemeHelper.SetButtonRole(btnRemove, "danger");
            grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.SurfaceAlt;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextSecondary;
            Reload();
            LoadEditor(startWithNew ? null : categories.FirstOrDefault()?.CategoryName);
        }




        private void Reload()
        {
            try { MJdatabase.EnsureCapacitySchema(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            try { categories = MJdatabase.GetTruckCategories(); } catch { categories = new List<TruckCategoryItem>(); }
            try { matrix = MJdatabase.GetCapacityMatrix(); } catch { matrix = new List<TruckCapacity>(); }
            grid.Rows.Clear();
            foreach (var c in categories)
            {
                var m = Find(c.CategoryName);
                int i = grid.Rows.Add(c.CategoryName, m?.DryMax.ToString() ?? "—", m?.ColdMax.ToString() ?? "—", m == null || m.CombiMax == 0 ? "not set" : $"{m.CombiColdMax} + {m.CombiDryMax} = {m.CombiMax}", c.AssignedTrucksCount);
                grid.Rows[i].Tag = c.CategoryName;
            }
            grid.ClearSelection();
        }




        private TruckCapacity? Find(string? name) => matrix.FirstOrDefault(x => string.Equals(x.CategoryName.Trim(), (name ?? "").Trim(), StringComparison.OrdinalIgnoreCase));




        private void LoadEditor(string? name)
        {
            editingName = name;
            var m = Find(name);
            var c = categories.FirstOrDefault(x => string.Equals(x.CategoryName, name, StringComparison.OrdinalIgnoreCase));
            txtName.Text = name ?? "";
            numDry.Value = Math.Min(numDry.Maximum, m?.DryMax ?? c?.CrateCapacity ?? 0);
            numCold.Value = Math.Min(numCold.Maximum, m?.ColdMax ?? 0);
            numCombiCold.Value = Math.Min(numCombiCold.Maximum, m?.CombiColdMax ?? 0);
            numCombiDry.Value = Math.Min(numCombiDry.Maximum, m?.CombiDryMax ?? 0);
            lblEditTitle.Text = name == null ? "New truck category" : $"Edit {name}";
            btnRemove.Visible = name != null;
            if (name == null) { grid.ClearSelection(); txtName.Focus(); }
            ShowRules();
        }




        private TruckCapacity FromEditor() => new TruckCapacity
        {
            CategoryName = System.Text.RegularExpressions.Regex.Replace(txtName.Text.Trim(), @"\s+", " "),
            DryMax = (int)numDry.Value, ColdMax = (int)numCold.Value, CombiColdMax = (int)numCombiCold.Value, CombiDryMax = (int)numCombiDry.Value
        };




        private void ShowRules()
        {
            var m = FromEditor();
            var errs = MJdatabase.CapacityRuleErrors(m);
            if (errs.Count > 0) { lblRule.Text = "⛔ " + string.Join("\n⛔ ", errs); lblRule.ForeColor = AppTheme.DangerText; }
            else { lblRule.Text = $"✓ Dry {m.DryMax} · Cold {m.ColdMax} · Combi {(m.CombiMax == 0 ? "not set" : $"{m.CombiColdMax} cold + {m.CombiDryMax} dry = {m.CombiMax}")} · truck maximum {m.TruckMax}"; lblRule.ForeColor = AppTheme.SuccessText; }
        }




        private void SaveCurrent()
        {
            var m = FromEditor();
            var errs = MJdatabase.CapacityRuleErrors(m);
            if (errs.Count > 0)
            {
                QuietBox.Show("This category can't be saved:\n\n• " + string.Join("\n• ", errs), "Check the capacities", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var clash = categories.FirstOrDefault(c => string.Equals(c.CategoryName.Trim(), m.CategoryName, StringComparison.OrdinalIgnoreCase) && !string.Equals(c.CategoryName, editingName, StringComparison.OrdinalIgnoreCase));
            if (clash != null) { QuietBox.Show($"There is already a category named \"{clash.CategoryName}\".", "Category Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            // Warn (Section 8): trucks of this category will change capacity.
            var old = Find(editingName);
            int trucks = categories.FirstOrDefault(c => string.Equals(c.CategoryName, editingName, StringComparison.OrdinalIgnoreCase))?.AssignedTrucksCount ?? 0;
            string msg = editingName == null ? $"Are you sure you want to add truck category \"{m.CategoryName}\"?" : $"Are you sure you want to save \"{m.CategoryName}\"?";
            if (old != null && trucks > 0 && (old.DryMax != m.DryMax || old.ColdMax != m.ColdMax || old.CombiMax != m.CombiMax))
                msg += $"\n\n⚠ {trucks} truck{(trucks == 1 ? "" : "s")} in this category will get the new capacity (Dry {old.DryMax}→{m.DryMax}, Cold {old.ColdMax}→{m.ColdMax}, Combi {old.CombiMax}→{m.CombiMax}). Orders already planned keep their crate counts, but new capacity checks will use the new numbers.";
            if (old != null && (m.DryMax < old.DryMax || m.ColdMax < old.ColdMax || m.CombiMax < old.CombiMax))
                msg += "\n\nYou are lowering a capacity — open orders that were within the old limit may now show as over capacity.";
            if (MessageBox.Show(msg, "Save truck category", MessageBoxButtons.YesNo, (msg.Contains("⚠") ? MessageBoxIcon.Warning : MessageBoxIcon.Question), MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            try
            {
                int changed = MJdatabase.SaveCapacityMatrix(m, editingName);
                Changed = true;
                Reload();
                LoadEditor(m.CategoryName);
                ToastHelper.ShowToast(this, changed > 0 ? $"{m.CategoryName} saved · {changed} truck(s) updated" : $"{m.CategoryName} saved ✓", ToastType.Success);
            }
            catch (Exception ex) { ErrLog.Error("TruckCategoryMatrixDialog", ex); MessageBox.Show(ex.Message, "Save truck category", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }




        private void RemoveCurrent()
        {
            if (editingName == null) return;
            int trucks = categories.FirstOrDefault(c => string.Equals(c.CategoryName, editingName, StringComparison.OrdinalIgnoreCase))?.AssignedTrucksCount ?? 0;
            if (trucks > 0)
            {
                QuietBox.Show($"{editingName} is used by {trucks} truck{(trucks == 1 ? "" : "s")}, so it can't be removed. Move those trucks to another category first.", "Category in use", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show($"Are you sure you want to remove truck category \"{editingName}\"?", "Remove category", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            try
            {
                MJdatabase.DeleteTruckCategory(editingName);
                MJdatabase.NotifyCategoriesChanged();
                Changed = true;
                Reload();
                LoadEditor(categories.FirstOrDefault()?.CategoryName);
            }
            catch (Exception ex) { ErrLog.Error("TruckCategoryMatrixDialog", ex); MessageBox.Show(ex.Message, "Remove category", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }




        protected override void Dispose(bool disposing)
        {
            DarkModeKit.RunWithoutFalseCrossThreadCheck(() => base.Dispose(disposing), nameof(TruckCategoryMatrixDialog));
        }
    }
}
