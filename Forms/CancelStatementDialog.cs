using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// Cancelling a Statement of Account: says plainly what happens (the service invoices go back to "ready for a statement", nothing is erased)
    /// and asks for the reason, which is kept with the statement and in the audit log.
    /// </summary>
    public sealed class CancelStatementDialog : Form
    {
        private readonly TextBox txtReason;
        private readonly Button btnCancel, btnKeep;
        public string Reason => txtReason.Text.Trim();

        private CancelStatementDialog(string soaNumber, string client, int invoiceCount, decimal total)
        {
            int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            Text = "Cancel statement of account";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            BackColor = AppTheme.Surface; ForeColor = AppTheme.TextPrimary; Font = new Font("Segoe UI", 9.75F);
            ClientSize = new Size(D(560), D(380));
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(D(20)), BackColor = AppTheme.Surface };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Label L(string t, Font f, Color c, int top = 0) => new Label { Text = t, AutoSize = true, MaximumSize = new Size(D(520), 0), Font = f, ForeColor = c, UseMnemonic = false, Margin = new Padding(0, D(top), 0, D(4)) };
            root.Controls.Add(L($"Cancel Statement of Account {soaNumber}?", new Font("Segoe UI Semibold", 12F), AppTheme.TextPrimary));
            root.Controls.Add(L($"{client} · {invoiceCount} service invoice{(invoiceCount == 1 ? "" : "s")} · ₱{total:N2}", new Font("Segoe UI", 9.75F), AppTheme.TextSecondary, 2));
            root.Controls.Add(L("What happens:\n• The statement is marked Cancelled. It is not erased: it stays in the list and the audit log.\n" +
                                "• Its service invoices are not cancelled. They go back to \"Ready for a statement\" so you can put them on a new statement.\n" +
                                "• You can't edit a cancelled statement. Make a new one instead.", new Font("Segoe UI", 9.25F), AppTheme.TextMuted, 8));
            root.Controls.Add(L("Why is it being cancelled? (required)", new Font("Segoe UI Semibold", 9.5F), AppTheme.TextPrimary, 10));
            txtReason = new TextBox { Multiline = true, Height = D(70), Width = D(520), MaxLength = 300, ScrollBars = ScrollBars.Vertical, PlaceholderText = "e.g. wrong client picked, period changed, an invoice was missing" };
            root.Controls.Add(txtReason);
            var footer = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = D(54), Padding = new Padding(D(14), D(10), D(20), D(8)), BackColor = AppTheme.Surface };
            btnCancel = new Button { Text = "Cancel statement", AutoSize = true, MinimumSize = new Size(D(140), D(36)), Enabled = false, BackColor = AppTheme.Danger, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnKeep = new Button { Text = "Keep statement", AutoSize = true, MinimumSize = new Size(D(120), D(36)), DialogResult = DialogResult.Cancel };
            UIThemeHelper.SetButtonRole(btnKeep, "secondary");
            footer.Controls.Add(btnCancel); footer.Controls.Add(btnKeep);
            Controls.Add(root); Controls.Add(footer);
            CancelButton = btnKeep;
            txtReason.TextChanged += (s, e) => btnCancel.Enabled = txtReason.Text.Trim().Length >= MJdatabase.MinCancelReasonLength;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            Shown += (s, e) => { try { UIThemeHelper.KeepOnScreen(this); txtReason.Focus(); } catch { } };
        }

        /// <summary>Shows the dialog; false when the person keeps the statement.</summary>
        public static bool Prompt(IWin32Window? owner, string soaNumber, string client, int invoiceCount, decimal total, out string reason)
        {
            using var dlg = new CancelStatementDialog(soaNumber, client, invoiceCount, total);
            bool ok = (owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog()) == DialogResult.OK;
            reason = ok ? dlg.Reason : "";
            return ok;
        }
    }
}
