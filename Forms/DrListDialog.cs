using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// The DR numbers of one store or line, one per row: type or paste one or more (commas, semicolons or new lines
    /// between them) and press Enter or Add; select one to remove it or move it up / down.
    /// </summary>
    public sealed class DrListDialog : Form
    {
        public string DrNumbers => DrList.Join(lst.Items.Cast<object>().Select(o => o.ToString() ?? ""));


        private readonly TextBox txtNew = new TextBox();
        private readonly ListBox lst = new ListBox();
        private readonly Label lblCount = new Label();
        private readonly Label lblNote = new Label();
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);


        public DrListDialog(string title, string drNumbers)
        {
            Text = "DR numbers";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            Font = new Font("Segoe UI", 10F);
            BackColor = AppTheme.Surface;
            ForeColor = AppTheme.TextPrimary;
            AutoScaleMode = AutoScaleMode.None;
            KeyPreview = true;
            ClientSize = new Size(D(460), D(470));
            try { ThemeManager.StyleWindowChrome(this); } catch (Exception ex) { ErrLog.Swallowed(ex); }


            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(D(18), D(14), D(18), D(14)), BackColor = AppTheme.Surface };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // title
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // hint
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // input
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // note
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // list
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // count
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // buttons
            Controls.Add(root);


            var lblTitle = new Label { Text = string.IsNullOrWhiteSpace(title) ? "DR numbers" : $"DR numbers · {title}", AutoSize = true, Font = new Font("Segoe UI Semibold", 13F), ForeColor = AppTheme.TextPrimary, UseMnemonic = false, MaximumSize = new Size(D(420), 0), Margin = new Padding(0, 0, 0, D(2)) };
            root.Controls.Add(lblTitle, 0, 0); root.SetColumnSpan(lblTitle, 2);
            var lblHint = new Label { Text = "Type or paste one or more DR numbers (commas or new lines between them), then press Enter or Add.", AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, UseMnemonic = false, MaximumSize = new Size(D(420), 0), Margin = new Padding(0, 0, 0, D(8)) };
            root.Controls.Add(lblHint, 0, 1); root.SetColumnSpan(lblHint, 2);


            txtNew.Dock = DockStyle.Fill;
            txtNew.Font = new Font("Segoe UI", 10.5F);
            DrList.GuardLength(txtNew);   // Nov 10: a DR number can't pass 8 digits
            txtNew.BackColor = AppTheme.InputBack;
            txtNew.ForeColor = AppTheme.TextPrimary;
            txtNew.PlaceholderText = "e.g. DR-1001";
            txtNew.MaxLength = 2000;
            txtNew.Margin = new Padding(0, D(2), D(8), D(4));
            txtNew.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; e.Handled = true; AddTyped(); }
                else if (e.KeyCode == Keys.V && e.Control) { e.SuppressKeyPress = true; e.Handled = true; PasteList(); }
            };
            var btnAdd = Btn("Add", "primary");
            btnAdd.Click += (s, e) => AddTyped();
            root.Controls.Add(txtNew, 0, 2);
            root.Controls.Add(btnAdd, 1, 2);


            lblNote.AutoSize = true; lblNote.Font = new Font("Segoe UI", 9F); lblNote.ForeColor = AppTheme.WarningText; lblNote.UseMnemonic = false; lblNote.MaximumSize = new Size(D(420), 0); lblNote.Margin = new Padding(0, 0, 0, D(4));
            root.Controls.Add(lblNote, 0, 3); root.SetColumnSpan(lblNote, 2);


            lst.Dock = DockStyle.Fill;
            lst.Font = new Font("Segoe UI", 10.5F);
            lst.BackColor = AppTheme.InputBack;
            lst.ForeColor = AppTheme.TextPrimary;
            lst.IntegralHeight = false;
            lst.Margin = new Padding(0, 0, D(8), D(4));
            lst.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete) { e.Handled = true; RemoveSelected(); } };
            root.Controls.Add(lst, 0, 4);
            var side = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = new Padding(0), BackColor = AppTheme.Surface };
            var btnRemove = Btn("Remove", "secondary");
            var btnUp = Btn("Move up", "secondary");
            var btnDown = Btn("Move down", "secondary");
            btnRemove.Click += (s, e) => RemoveSelected();
            btnUp.Click += (s, e) => MoveItem(-1);
            btnDown.Click += (s, e) => MoveItem(1);
            side.Controls.AddRange(new Control[] { btnRemove, btnUp, btnDown });
            root.Controls.Add(side, 1, 4);


            lblCount.AutoSize = true; lblCount.Font = new Font("Segoe UI Semibold", 9.5F); lblCount.ForeColor = AppTheme.TextSecondary; lblCount.Margin = new Padding(0, D(2), 0, D(6));
            root.Controls.Add(lblCount, 0, 5); root.SetColumnSpan(lblCount, 2);


            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, D(6), 0, 0), BackColor = AppTheme.Surface };
            var btnOk = Btn("Apply", "primary");
            var btnCancel = Btn("Cancel", "secondary");
            btnOk.Click += (s, e) => { if (txtNew.Text.Trim().Length > 0) AddTyped(); DialogResult = DialogResult.OK; Close(); };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            buttons.Controls.Add(btnOk);
            buttons.Controls.Add(btnCancel);
            root.Controls.Add(buttons, 0, 6); root.SetColumnSpan(buttons, 2);
            CancelButton = btnCancel;


            foreach (var d in DrList.Parse(drNumbers)) lst.Items.Add(d);
            UpdateCount();
            Shown += (s, e) => txtNew.Focus();
        }


        private Button Btn(string text, string role)
        {
            var b = new Button { Text = text, AutoSize = true, MinimumSize = new Size(D(104), D(34)), Padding = new Padding(D(8), 0, D(8), 0), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F), Cursor = Cursors.Hand, Margin = new Padding(0, 0, 0, D(6)), UseMnemonic = false };
            b.FlatAppearance.BorderSize = 0;
            UIThemeHelper.SetButtonRole(b, role);
            return b;
        }


        private void PasteList()
        {
            string clip = "";
            try { if (Clipboard.ContainsText()) clip = Clipboard.GetText(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            if (clip.IndexOfAny(new[] { '\r', '\n', ',', ';', '\t' }) >= 0) { AddMany(clip); return; }
            txtNew.SelectedText = clip;
        }


        private void AddTyped()
        {
            string text = txtNew.Text;
            if (text.Trim().Length == 0) { txtNew.Focus(); return; }
            AddMany(text);
            txtNew.Clear();
            txtNew.Focus();
        }


        private void AddMany(string text)
        {
            var existing = new HashSet<string>(lst.Items.Cast<object>().Select(o => o.ToString() ?? ""), StringComparer.OrdinalIgnoreCase);
            var skipped = new List<string>();
            int added = 0;
            foreach (var d in DrList.Parse(text))
            {
                if (existing.Add(d)) { lst.Items.Add(d); added++; }
                else skipped.Add(d);
            }
            lblNote.Text = skipped.Count == 0 ? "" : $"Already listed, not added again: {string.Join(", ", skipped.Take(5))}{(skipped.Count > 5 ? $" and {skipped.Count - 5} more" : "")}";
            if (added > 0) lst.SelectedIndex = lst.Items.Count - 1;
            UpdateCount();
        }


        private void RemoveSelected()
        {
            int i = lst.SelectedIndex;
            if (i < 0) return;
            lst.Items.RemoveAt(i);
            if (lst.Items.Count > 0) lst.SelectedIndex = Math.Min(i, lst.Items.Count - 1);
            lblNote.Text = "";
            UpdateCount();
        }


        private void MoveItem(int delta)
        {
            int i = lst.SelectedIndex, j = i + delta;
            if (i < 0 || j < 0 || j >= lst.Items.Count) return;
            object item = lst.Items[i];
            lst.Items.RemoveAt(i);
            lst.Items.Insert(j, item);
            lst.SelectedIndex = j;
        }


        private void UpdateCount()
        {
            int n = lst.Items.Count;
            lblCount.Text = n == 0 ? "No DR numbers yet." : n == 1 ? "1 DR number" : $"{n} DR numbers";
        }
    }
}
