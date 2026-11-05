using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Asks why a record is being cancelled. Sized for the screen's DPI and the app theme: a warning header that names the
    /// record and says what will happen, a list of common reasons, a reason box (at least 5 characters), and two plain buttons.
    /// </summary>
    public class CancellationReasonDialog : Form
    {
        private const int MinChars = 5, MaxChars = 500;
        private readonly TextBox txtReason;
        private readonly ComboBox cbPresetReason;
        private readonly Button btnConfirm;
        private readonly Button btnKeep;
        private readonly Label lblCount;
        private string cancellationReason = string.Empty;
        // Optional charge choice (dispatch orders only): is the client still charged for the cancelled trip?
        private readonly RadioButton? rbNoCharge;
        private readonly RadioButton? rbCharge;
        private readonly NumericUpDown? numFee;
        public bool Chargeable => rbCharge?.Checked == true;
        public decimal Fee => Chargeable && numFee != null ? numFee.Value : 0m;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CancellationReason => cancellationReason;

        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        /// <summary>"Dispatch Order" → "order", "Loading Order"/"Loading Schedule" → "loading order", anything else in lower case.</summary>
        private static string Noun(string itemType)
        {
            string t = (itemType ?? "").Trim().ToLowerInvariant();
            if (t.Contains("loading")) return "loading order";
            if (t.Contains("dispatch")) return "order";
            if (t.Contains("special")) return "special trip";
            return string.IsNullOrEmpty(t) ? "record" : t;
        }

        private static string Title(string itemType)
        {
            string t = (itemType ?? "").Trim();
            if (t.ToLowerInvariant().Contains("loading")) return "loading order";
            return string.IsNullOrEmpty(t) ? "record" : t.ToLowerInvariant();
        }
        public CancellationReasonDialog(string itemType = "Dispatch Order", string identifier = "", string? subject = null, decimal? chargeDefault = null)
        {
            bool isDispatch = (itemType ?? "").ToLowerInvariant().Contains("dispatch");
            bool isLoading = (itemType ?? "").ToLowerInvariant().Contains("loading");
            string noun = Noun(itemType ?? "");
            Text = "Cancel " + Title(itemType ?? "");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false; MaximizeBox = false; ShowIcon = false; ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = AppTheme.Surface; ForeColor = AppTheme.TextPrimary;
            Font = new Font("Segoe UI", 9.75F);
            KeyPreview = true;
            try { ThemeManager.StyleWindowChrome(this); } catch (Exception swx) { ErrLog.Swallowed(swx); }
            int bodyW = D(500);
            Label L(string t, Font f, Color c, int top, Color back) => new Label { Text = t, Font = f, ForeColor = c, BackColor = back, AutoSize = true, MaximumSize = new Size(bodyW - D(60), 0), Margin = new Padding(0, top, 0, 0), UseMnemonic = false };
            // ---- header: warning badge + what is being cancelled + what will happen
            var header = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Margin = new Padding(0), Padding = new Padding(D(24), D(20), D(24), D(18)), BackColor = AppTheme.SurfaceAlt };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var badge = new Panel { Size = new Size(D(40), D(40)), Margin = new Padding(0, D(2), D(14), 0), BackColor = AppTheme.SurfaceAlt };
            badge.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(AppTheme.SurfaceAlt);
                using (var b = new SolidBrush(AppTheme.DangerSoft)) g.FillEllipse(b, 0, 0, badge.Width - 1, badge.Height - 1);
                int inset = D(7);
                using (var b = new SolidBrush(AppTheme.Danger)) g.FillEllipse(b, inset, inset, badge.Width - 1 - inset * 2, badge.Height - 1 - inset * 2);
                using var f = new Font("Segoe UI", 11F, FontStyle.Bold);
                TextRenderer.DrawText(g, "!", f, new Rectangle(0, 0, badge.Width, badge.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };

            var head = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0), BackColor = AppTheme.SurfaceAlt };
            head.Controls.Add(L("Cancel " + Title(itemType ?? ""), new Font("Segoe UI Semibold", 13F), AppTheme.TextPrimary, 0, AppTheme.SurfaceAlt));
            string who = string.Join("  ·  ", new[] { identifier, subject }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (who.Length > 0) head.Controls.Add(L(who, new Font("Segoe UI Semibold", 10F), AppTheme.TextSecondary, D(2), AppTheme.SurfaceAlt));
            string effect = isDispatch
                ? "The truck, driver and aides will be freed. The order stays in Order History as Cancelled."
                : $"The {noun} stays in Order History as Cancelled. Nothing is deleted.";
            head.Controls.Add(L(effect, new Font("Segoe UI", 9.5F), AppTheme.TextMuted, D(8), AppTheme.SurfaceAlt));
            header.Controls.Add(badge, 0, 0);
            header.Controls.Add(head, 1, 0);
            // ---- body: common reason + reason box
            var body = new TableLayoutPanel { ColumnCount = 1, RowCount = 6, Margin = new Padding(0), Padding = new Padding(D(24), D(18), D(24), D(10)), BackColor = AppTheme.Surface, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, bodyW - D(48)));
            var lblPreset = L("Common reason (optional)", new Font("Segoe UI Semibold", 9.5F), AppTheme.TextPrimary, 0, AppTheme.Surface);
            cbPresetReason = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Margin = new Padding(0, D(6), 0, D(16)), Font = new Font("Segoe UI", 10F), IntegralHeight = false, MaxDropDownItems = 10 };
            cbPresetReason.Items.Add("Pick a reason…");
            if (isLoading) cbPresetReason.Items.Add("Old loading order, never dispatched");
            cbPresetReason.Items.AddRange(new object[]
            {
                "Client cancelled the delivery",
                "Store closed / could not receive the delivery",
                "Bad weather / road not passable",
                "Truck breakdown / emergency repair",
                "Driver or crew emergency",
                "Wrong crate count or loading problem",
                "Entered twice by mistake"
            });
            cbPresetReason.SelectedIndex = 0;
            var lblReason = L("Reason (required)", new Font("Segoe UI Semibold", 9.5F), AppTheme.TextPrimary, 0, AppTheme.Surface);
            txtReason = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true, MaxLength = MaxChars, Dock = DockStyle.Top, Height = D(96), Margin = new Padding(0, D(6), 0, D(6)), Font = new Font("Segoe UI", 10F) };
            var hintRow = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0), BackColor = AppTheme.Surface };
            hintRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            hintRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var lblHint = new Label { Text = $"At least {MinChars} characters", AutoSize = true, Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, BackColor = AppTheme.Surface, Margin = new Padding(0), UseMnemonic = false };
            lblCount = new Label { Text = $"0 / {MaxChars}", AutoSize = true, Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, BackColor = AppTheme.Surface, Margin = new Padding(0), Anchor = AnchorStyles.Right };
            hintRow.Controls.Add(lblHint, 0, 0);
            hintRow.Controls.Add(lblCount, 1, 0);
            body.Controls.Add(lblPreset, 0, 0);
            body.Controls.Add(cbPresetReason, 0, 1);
            body.Controls.Add(lblReason, 0, 2);
            body.Controls.Add(txtReason, 0, 3);
            body.Controls.Add(hintRow, 0, 4);
            if (chargeDefault.HasValue)
            {
                // Billing: only a chargeable cancelled trip can be invoiced later; a no-charge one never appears in Billing.
                var charge = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(0, D(12), 0, 0), BackColor = AppTheme.Surface };
                charge.Controls.Add(L("Is the client still charged? (required)", new Font("Segoe UI Semibold", 9.5F), AppTheme.TextPrimary, 0, AppTheme.Surface));
                rbNoCharge = new RadioButton { Text = "Cancelled – No charge (this trip can't be billed)", AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = AppTheme.TextPrimary, BackColor = AppTheme.Surface, Margin = new Padding(0, D(6), 0, 0), Checked = true, UseMnemonic = false };
                rbCharge = new RadioButton { Text = "Cancelled – Chargeable (client still pays; bill it in Billing)", AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = AppTheme.TextPrimary, BackColor = AppTheme.Surface, Margin = new Padding(0, D(4), 0, 0), UseMnemonic = false };
                var feeRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, Margin = new Padding(D(20), D(6), 0, 0), BackColor = AppTheme.Surface };
                feeRow.Controls.Add(new Label { Text = "Amount to bill (₱):", AutoSize = true, Font = new Font("Segoe UI", 9.5F), ForeColor = AppTheme.TextSecondary, BackColor = AppTheme.Surface, Margin = new Padding(0, D(6), D(8), 0), UseMnemonic = false });
                numFee = new NumericUpDown { DecimalPlaces = 2, Maximum = 100000000, Minimum = 0, ThousandsSeparator = true, Width = D(160), Font = new Font("Segoe UI", 10F), Enabled = false };
                numFee.Value = Math.Max(0, Math.Min(numFee.Maximum, Math.Round(chargeDefault.Value, 2)));
                InputGuard.MoneyOnly(numFee);
                feeRow.Controls.Add(numFee);
                charge.Controls.Add(rbNoCharge);
                charge.Controls.Add(rbCharge);
                charge.Controls.Add(feeRow);
                body.Controls.Add(charge, 0, 5);
                rbCharge.CheckedChanged += (s, e) => { numFee.Enabled = rbCharge.Checked; UpdateState(); if (rbCharge.Checked) numFee.Focus(); };
                numFee.ValueChanged += (s, e) => UpdateState();
            }
            // ---- footer: Keep (secondary) and Cancel (danger), right-aligned on a band
            var footer = new FlowLayoutPanel { Height = D(62), FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = new Padding(0), Padding = new Padding(D(16), D(13), D(20), D(13)), BackColor = AppTheme.SurfaceAlt };
            footer.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0); };
            Button B(string t, string role, int minW)
            {
                var b = new Button { Text = t, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(D(minW), D(36)), Padding = new Padding(D(10), 0, D(10), 0), Margin = new Padding(D(8), 0, 0, 0), Cursor = Cursors.Hand, UseMnemonic = false };
                UIThemeHelper.SetButtonRole(b, role);
                return b;
            }
            btnConfirm = B("Cancel " + noun, "danger", 130);
            btnConfirm.BackColor = AppTheme.Danger; btnConfirm.ForeColor = Color.White; // solid red: this is the action that cancels
            btnConfirm.FlatAppearance.MouseOverBackColor = AppTheme.DangerHover;
            btnConfirm.FlatAppearance.MouseDownBackColor = AppTheme.DangerHover;
            btnKeep = B("Keep " + noun, "secondary", 110);
            btnKeep.DialogResult = DialogResult.Cancel;
            btnConfirm.Click += BtnConfirm_Click;
            footer.Controls.Add(btnConfirm);
            footer.Controls.Add(btnKeep);
            // ---- behaviour
            cbPresetReason.SelectedIndexChanged += (s, e) =>
            {
                if (cbPresetReason.SelectedIndex > 0)
                {
                    txtReason.Text = cbPresetReason.SelectedItem?.ToString() ?? "";
                    txtReason.SelectionStart = txtReason.TextLength;
                    txtReason.Focus();
                }
            };

            txtReason.TextChanged += (s, e) => UpdateState();
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && e.Control && btnConfirm.Enabled) { e.SuppressKeyPress = true; btnConfirm.PerformClick(); }
            };

            new ToolTip().SetToolTip(btnConfirm, "Ctrl + Enter");
            new ToolTip().SetToolTip(btnKeep, "Esc");
            // ---- size to content, then dock
            var hp = header.GetPreferredSize(new Size(bodyW, 0));
            var bp = body.GetPreferredSize(new Size(bodyW, 0));
            int w = Math.Max(bodyW, hp.Width);
            ClientSize = new Size(w, hp.Height + bp.Height + footer.Height);
            header.AutoSize = false; header.Dock = DockStyle.Top; header.Height = hp.Height;
            body.AutoSize = false; body.Dock = DockStyle.Fill;
            footer.Dock = DockStyle.Bottom;
            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(header);
            CancelButton = btnKeep;
            UIThemeHelper.StyleInput(cbPresetReason);
            UIThemeHelper.StyleInput(txtReason);
            UpdateState();
            Shown += (s, e) => { UIThemeHelper.KeepOnScreen(this); txtReason.Focus(); };
        }
        private void UpdateState()
        {
            int n = txtReason.Text.Trim().Length;
            lblCount.Text = $"{txtReason.TextLength} / {MaxChars}";
            btnConfirm.Enabled = n >= MinChars && (!Chargeable || (numFee != null && numFee.Value > 0));
        }

        private void BtnConfirm_Click(object? sender, EventArgs e)
        {
            string reason = txtReason.Text.Trim();
            if (reason.Length < MinChars) { txtReason.Focus(); return; }
            if (Chargeable && (numFee == null || numFee.Value <= 0)) { numFee?.Focus(); return; } // a chargeable cancellation needs an amount
            cancellationReason = reason;
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>Dispatch cancellation: reason plus whether the client is still charged (and how much). Returns null when the user keeps the order.</summary>
        public static string? PromptDispatchCancellation(IWin32Window owner, string identifier, string? subject, decimal defaultFee, out bool chargeable, out decimal fee)
        {
            using var dlg = new CancellationReasonDialog("Dispatch Order", identifier, subject, defaultFee);
            bool ok = dlg.ShowDialog(owner) == DialogResult.OK;
            chargeable = ok && dlg.Chargeable;
            fee = ok ? dlg.Fee : 0m;
            return ok ? dlg.CancellationReason : null;
        }
        public static string? PromptCancellationReason(IWin32Window owner, string itemType = "Dispatch Order", string identifier = "", string? subject = null)
        {
            using var dlg = new CancellationReasonDialog(itemType, identifier, subject);
            return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.CancellationReason : null;
        }
    }
}
