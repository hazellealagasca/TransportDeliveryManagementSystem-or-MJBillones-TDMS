using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// One store of a trip report: shows how many G and E crates were delivered (read-only reference), lets the user
    /// enter several delivery receipts, the crates returned, the number of drops, the store times and representative.
    /// Delivered crates come from the dispatch and can't be changed here; returned crates can only be changed here.
    /// </summary>
    public sealed class TripStoreDialog : Form
    {
        public string StoreCode => txtCode.Text.Trim();
        public string StoreName => txtName.Text.Trim();
        public string DrNumbers => string.Join(", ", lstDr.Items.Cast<object>().Select(o => o.ToString()!.Trim()).Where(x => x.Length > 0));
        public int ReturnedG => (int)numRetG.Value;
        public int ReturnedE => (int)numRetE.Value;
        public string Drops => numDrops.Value > 0 ? ((int)numDrops.Value).ToString() : "";
        public string TimeIn => txtIn.Text.Trim();
        public string TimeOut => txtOut.Text.Trim();
        public string Representative => txtRep.Text.Trim();



        private readonly int deliveredG, deliveredE, originalRetG, originalRetE;
        private readonly TextBox txtCode = new TextBox(), txtName = new TextBox(), txtDrNew = new TextBox(), txtIn = new TextBox(), txtOut = new TextBox(), txtRep = new TextBox();
        private readonly ListBox lstDr = new ListBox();
        private readonly NumericUpDown numRetG = new NumericUpDown(), numRetE = new NumericUpDown(), numDrops = new NumericUpDown();
        private readonly Label lblStatus = new Label();
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);



        public TripStoreDialog(string code, string name, bool canRenameStore, int deliveredG, int deliveredE, int returnedG, int returnedE,
                               string drNumbers, string drops, string timeIn, string timeOut, string representative)
        {
            this.deliveredG = Math.Max(0, deliveredG);
            this.deliveredE = Math.Max(0, deliveredE);
            originalRetG = Math.Max(0, returnedG);
            originalRetE = Math.Max(0, returnedE);
            Text = "Store details";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            Font = new Font("Segoe UI", 10F);
            BackColor = AppTheme.Surface;
            ForeColor = AppTheme.TextPrimary;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(D(20), D(16), D(20), D(16));
            KeyPreview = true;



            var root = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, BackColor = AppTheme.Surface };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(260)));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(260)));
            Controls.Add(root);
            int row = 0;
            Label Cap(string t) => new Label { Text = t, AutoSize = true, ForeColor = AppTheme.TextSecondary, Font = new Font("Segoe UI Semibold", 9.5F), Margin = new Padding(0, D(8), D(12), D(4)), UseMnemonic = false };
            void Field(Control c) { c.Dock = DockStyle.Fill; c.Margin = new Padding(0, 0, D(12), D(4)); c.Font = new Font("Segoe UI", 10.5F); c.BackColor = AppTheme.InputBack; c.ForeColor = AppTheme.TextPrimary; }
            void Add2(Control a, Control? b, int span = 1) { root.Controls.Add(a, 0, row); if (b != null) root.Controls.Add(b, 1, row); else if (span == 2) root.SetColumnSpan(a, 2); row++; }



            var title = new Label { Text = string.IsNullOrWhiteSpace(name) ? "New store" : name, AutoSize = true, Font = new Font("Segoe UI Semibold", 14F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, 0, 0, D(2)), UseMnemonic = false, MaximumSize = new Size(D(520), 0) };
            Add2(title, null, 2);
            var sub = new Label { Text = "Delivered crates come from the dispatch and can't be changed. Enter what the store returned.", AutoSize = true, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9.5F), MaximumSize = new Size(D(520), 0), Margin = new Padding(0, 0, 0, D(8)), UseMnemonic = false };
            Add2(sub, null, 2);



            Add2(Cap("Store code"), Cap("Store name"));
            txtCode.Text = code; txtName.Text = name; Field(txtCode); Field(txtName);
            txtCode.ReadOnly = txtName.ReadOnly = !canRenameStore;
            if (!canRenameStore) { txtCode.BackColor = AppTheme.SurfaceAlt; txtName.BackColor = AppTheme.SurfaceAlt; txtCode.TabStop = txtName.TabStop = false; }
            Add2(txtCode, txtName);



            // Reference: delivered crates (locked)
            var refBox = new Label
            {
                AutoSize = true,
                Text = $"Delivered (from dispatch):   G crates {this.deliveredG:N0}   ·   E crates {this.deliveredE:N0}   ·   Total {this.deliveredG + this.deliveredE:N0}",
                BackColor = AppTheme.SurfaceAlt,
                ForeColor = AppTheme.TextPrimary,
                Font = new Font("Segoe UI Semibold", 10.5F),
                Padding = new Padding(D(10), D(8), D(10), D(8)),
                Margin = new Padding(0, D(10), 0, D(4)),
                UseMnemonic = false,
                MaximumSize = new Size(D(520), 0)
            };
            Add2(refBox, null, 2);



            Add2(Cap("Returned G crates"), Cap("Returned E crates"));
            foreach (var n in new[] { numRetG, numRetE }) { n.Minimum = 0; n.Maximum = 99999; Field(n); n.TextAlign = HorizontalAlignment.Right; }
            numRetG.Value = Math.Min(numRetG.Maximum, originalRetG);
            numRetE.Value = Math.Min(numRetE.Maximum, originalRetE);
            numRetG.ValueChanged += (s, e) => UpdateStatus();
            numRetE.ValueChanged += (s, e) => UpdateStatus();
            Add2(numRetG, numRetE);
            lblStatus.AutoSize = true; lblStatus.Font = new Font("Segoe UI Semibold", 10F); lblStatus.Margin = new Padding(0, D(4), 0, D(6)); lblStatus.MaximumSize = new Size(D(520), 0); lblStatus.UseMnemonic = false;
            Add2(lblStatus, null, 2);



            // Delivery receipts (several allowed)
            Add2(Cap("Delivery receipts (DR numbers): add one or more"), null, 2);
            var drRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, Margin = new Padding(0), BackColor = AppTheme.Surface };
            drRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            drRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            drRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            Field(txtDrNew); txtDrNew.PlaceholderText = "Type a DR number and press Enter"; txtDrNew.MaxLength = 60;
            var btnAddDr = Btn("Add", "secondary");
            var btnRemoveDr = Btn("Remove", "secondary");
            btnAddDr.Click += (s, e) => AddDr();
            btnRemoveDr.Click += (s, e) => { if (lstDr.SelectedIndex >= 0) lstDr.Items.RemoveAt(lstDr.SelectedIndex); };
            txtDrNew.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddDr(); } };
            drRow.Controls.Add(txtDrNew, 0, 0); drRow.Controls.Add(btnAddDr, 1, 0); drRow.Controls.Add(btnRemoveDr, 2, 0);
            Add2(drRow, null, 2);
            foreach (var dr in (drNumbers ?? "").Split(new[] { ',', ';', '/', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0))
                lstDr.Items.Add(dr);
            lstDr.Height = D(90); Field(lstDr); lstDr.IntegralHeight = false;
            Add2(lstDr, null, 2);



            Add2(Cap("No. Drops"), Cap("Representative"));
            numDrops.Minimum = 0; numDrops.Maximum = 999; Field(numDrops);
            if (int.TryParse((drops ?? "").Trim(), out int dp) && dp > 0) numDrops.Value = Math.Min(999, dp);
            txtRep.Text = representative; Field(txtRep); txtRep.MaxLength = 120;
            Add2(numDrops, txtRep);
            Add2(Cap("Store in (time)"), Cap("Store out (time)"));
            txtIn.Text = timeIn; txtOut.Text = timeOut; Field(txtIn); Field(txtOut);
            txtIn.PlaceholderText = txtOut.PlaceholderText = "e.g. 08:00 AM";
            Add2(txtIn, txtOut);



            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, D(14), 0, 0), BackColor = AppTheme.Surface };
            var btnOk = Btn("Apply to this store", "primary");
            var btnCancel = Btn("Cancel", "secondary");
            btnOk.Click += (s, e) => TryApply();
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            buttons.Controls.Add(btnOk); buttons.Controls.Add(btnCancel);
            Add2(buttons, null, 2);
            CancelButton = btnCancel;
            UpdateStatus();
            Shown += (s, e) => { (canRenameStore && string.IsNullOrWhiteSpace(name) ? (Control)txtName : numRetG).Focus(); };
        }



        private Button Btn(string text, string role)
        {
            var b = new Button { Text = text, AutoSize = true, MinimumSize = new Size(D(96), D(36)), Padding = new Padding(D(10), 0, D(10), 0), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F), Cursor = Cursors.Hand, Margin = new Padding(D(6), 0, 0, D(4)), UseMnemonic = false };
            UIThemeHelper.SetButtonRole(b, role);
            return b;
        }



        private void AddDr()
        {
            string dr = txtDrNew.Text.Trim();
            if (dr.Length == 0) return;
            if (lstDr.Items.Cast<object>().Any(o => string.Equals(o.ToString(), dr, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(this, $"DR \"{dr}\" is already listed for this store.", "Delivery receipt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            lstDr.Items.Add(dr);
            txtDrNew.Clear();
            txtDrNew.Focus();
        }



        private string Difference()
        {
            var parts = new List<string>();
            int dg = ReturnedG - deliveredG, de = ReturnedE - deliveredE;
            if (dg < 0) parts.Add($"G crates short by {-dg:N0}");
            if (dg > 0) parts.Add($"G crates over by {dg:N0}");
            if (de < 0) parts.Add($"E crates short by {-de:N0}");
            if (de > 0) parts.Add($"E crates over by {de:N0}");
            return string.Join(", ", parts);
        }



        private void UpdateStatus()
        {
            string diff = Difference();
            if (diff.Length == 0)
            {
                lblStatus.Text = "✓ Complete: returned crates match the delivered crates.";
                lblStatus.ForeColor = AppTheme.SuccessText;
            }
            else
            {
                lblStatus.Text = "⚠ Incomplete: " + diff + ".";
                lblStatus.ForeColor = AppTheme.WarningText;
            }
        }



        private void TryApply()
        {
            if (!txtName.ReadOnly && StoreName.Length == 0)
            {
                MessageBox.Show(this, "Please enter the store name.", "Store details", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }
            if (txtDrNew.Text.Trim().Length > 0) AddDr(); // a DR typed but not added yet is kept
            string diff = Difference();
            bool modified = ReturnedG != originalRetG || ReturnedE != originalRetE;
            if (diff.Length > 0 || modified)
            {
                // Double warning: the user confirms twice before incomplete or changed crate counts are kept.
                string first = (diff.Length > 0 ? $"The returned crates for this store are INCOMPLETE ({diff}).\n\n" : "")
                             + (modified ? $"Returned crates were changed: G {originalRetG:N0} → {ReturnedG:N0}, E {originalRetE:N0} → {ReturnedE:N0}.\n\n" : "")
                             + $"Delivered: G {deliveredG:N0}, E {deliveredE:N0}.\n\nDo you want to continue?";
                if (MessageBox.Show(this, first, "Check the crates (1 of 2)", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                string second = $"Please confirm again.\n\nSave returned crates for \"{(StoreName.Length > 0 ? StoreName : "this store")}\" as G {ReturnedG:N0} and E {ReturnedE:N0}?";
                if (MessageBox.Show(this, second, "Confirm the crates (2 of 2)", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
