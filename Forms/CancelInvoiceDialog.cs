using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// Cancelling a service invoice asks WHY (plan 17.6): a typing mistake keeps the number for the corrected record (reinstate within
    /// 7 days); a real void (the paper invoice is cancelled) burns the number for good. No default: the person must choose.
    /// </summary>
    public sealed class CancelInvoiceDialog : Form
    {
        private readonly RadioButton rbTyping, rbVoid;
        private readonly TextBox txtReason;
        private readonly Button btnCancel, btnKeep;
        public bool TypingMistake => rbTyping.Checked;
        public string Reason => txtReason.Text.Trim();

        private CancelInvoiceDialog(string invoiceNumber)
        {
            int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            Text = "Cancel service invoice";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            BackColor = AppTheme.Surface; ForeColor = AppTheme.TextPrimary; Font = new Font("Segoe UI", 9.75F);
            ClientSize = new Size(D(560), D(430));
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(D(20)), BackColor = AppTheme.Surface };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Label L(string t, Font f, Color c, int top = 0) => new Label { Text = t, AutoSize = true, MaximumSize = new Size(D(520), 0), Font = f, ForeColor = c, UseMnemonic = false, Margin = new Padding(0, D(top), 0, D(4)) };
            root.Controls.Add(L($"Cancel Service Invoice {invoiceNumber}?", new Font("Segoe UI Semibold", 12F), AppTheme.TextPrimary));
            root.Controls.Add(L("Why is it being cancelled? Choose one:", new Font("Segoe UI", 9.75F), AppTheme.TextSecondary, 4));
            rbTyping = new RadioButton { AutoSize = true, MaximumSize = new Size(D(520), 0), Margin = new Padding(0, D(6), 0, 0), Text = "I typed something wrong. The paper invoice is fine and I will enter it again with the same number." };
            root.Controls.Add(rbTyping);
            root.Controls.Add(L($"The number {invoiceNumber} is kept for this invoice for {MJdatabase.PendingReenterDays} days: use Reinstate to correct it. After that it is used for good.", new Font("Segoe UI", 8.75F), AppTheme.TextMuted));
            rbVoid = new RadioButton { AutoSize = true, MaximumSize = new Size(D(520), 0), Margin = new Padding(0, D(8), 0, 0), Text = "The paper invoice itself is cancelled or void." };
            root.Controls.Add(rbVoid);
            root.Controls.Add(L($"The number {invoiceNumber} can never be used again.", new Font("Segoe UI", 8.75F), AppTheme.TextMuted));
            root.Controls.Add(L("Reason (required)", new Font("Segoe UI Semibold", 9.5F), AppTheme.TextPrimary, 8));
            txtReason = new TextBox { Multiline = true, Height = D(70), Width = D(520), MaxLength = 300, ScrollBars = ScrollBars.Vertical };
            root.Controls.Add(txtReason);
            var footer = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = D(54), Padding = new Padding(D(14), D(10), D(20), D(8)), BackColor = AppTheme.Surface };
            btnCancel = new Button { Text = "Cancel invoice", AutoSize = true, MinimumSize = new Size(D(130), D(36)), Enabled = false, BackColor = AppTheme.Danger, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnKeep = new Button { Text = "Keep invoice", AutoSize = true, MinimumSize = new Size(D(110), D(36)), DialogResult = DialogResult.Cancel };
            UIThemeHelper.SetButtonRole(btnKeep, "secondary");
            footer.Controls.Add(btnCancel); footer.Controls.Add(btnKeep);
            Controls.Add(root); Controls.Add(footer);
            CancelButton = btnKeep;
            void State() => btnCancel.Enabled = (rbTyping.Checked || rbVoid.Checked) && txtReason.Text.Trim().Length >= 3;
            rbTyping.CheckedChanged += (s, e) => State(); rbVoid.CheckedChanged += (s, e) => State(); txtReason.TextChanged += (s, e) => State();
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            Shown += (s, e) => { try { UIThemeHelper.KeepOnScreen(this); } catch { } };
        }

        /// <summary>Shows the dialog; false when the person keeps the invoice.</summary>
        public static bool Prompt(IWin32Window? owner, string invoiceNumber, out bool typingMistake, out string reason)
        {
            using var dlg = new CancelInvoiceDialog(invoiceNumber);
            bool ok = (owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog()) == DialogResult.OK;
            typingMistake = ok && dlg.TypingMistake; reason = ok ? dlg.Reason : "";
            return ok;
        }
    }

    /// <summary>The "Cancelled as typing mistakes" report: who cancelled what and why, and whether it was reinstated, is waiting or expired.</summary>
    public sealed class TypingMistakeReportDialog : Form
    {
        public TypingMistakeReportDialog()
        {
            int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            Text = "Invoices cancelled as typing mistakes";
            StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false; MinimizeBox = false;
            BackColor = AppTheme.Surface; Font = new Font("Segoe UI", 9.5F);
            ClientSize = new Size(D(980), D(460));
            var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = AppTheme.Surface, BorderStyle = BorderStyle.None };
            UIThemeHelper.ApplyGridTheme(grid);
            foreach (var (n, h, w) in new[] { ("no", "SI No.", 70), ("cl", "Client", 120), ("amt", "Net", 70), ("by", "Cancelled by", 80), ("at", "Cancelled", 100), ("why", "Reason", 190), ("st", "State", 80), ("until", "Keep until / reinstated", 120) })
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = n, HeaderText = h, FillWeight = w });
            var rows = MJdatabase.GetTypingMistakeCancellations();
            foreach (var r in rows)
                grid.Rows.Add(r.Number, r.Client, r.Net.ToString("N2"), r.CancelledBy, r.CancelledAt?.ToString("MMM d, yyyy h:mm tt") ?? "", r.Reason, r.State,
                    r.State == "Reinstated" ? (r.ReinstatedAt?.ToString("MMM d, yyyy") ?? "") : (r.Until?.ToString("MMM d, yyyy") ?? ""));
            var note = new Label { Dock = DockStyle.Bottom, Height = D(34), Padding = new Padding(D(12), D(8), 0, 0), ForeColor = AppTheme.TextSecondary,
                Text = rows.Count == 0 ? "No invoice has been cancelled as a typing mistake." : $"{rows.Count} invoice(s). Waiting = number kept for the corrected record; Expired = number now used for good." };
            Controls.Add(grid); Controls.Add(note);
        }
    }
}
