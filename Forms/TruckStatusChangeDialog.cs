using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// Asks why a truck's status is changing. Every change needs a description (at most 500 characters). Under Maintenance also needs the breakdown date
    /// (not in the future) and the estimated fix date (not before the breakdown). Problems are shown inside the window. Returns null when cancelled.
    /// </summary>
    public static class TruckStatusChangeDialog
    {
        public static (string Description, DateTime? BreakdownDate, DateTime? EstimatedFixDate)? Ask(IWin32Window? owner, string plate, string oldStatus, string newStatus, DateTime? knownBreakdown = null, DateTime? knownFix = null)
        {
            float scale = 1f;
            try { if (owner is Control oc) scale = oc.DeviceDpi / 96f; } catch (Exception ex) { ErrLog.Swallowed(ex); }
            int D(int v) => (int)Math.Round(v * scale);
            bool toMaint = string.Equals((newStatus ?? "").Trim(), "Under Maintenance", StringComparison.OrdinalIgnoreCase);
            bool toDecom = MJdatabase.IsDecommissioned(newStatus);
            bool back = !toMaint && !toDecom && (string.Equals((oldStatus ?? "").Trim(), "Under Maintenance", StringComparison.OrdinalIgnoreCase) || MJdatabase.IsDecommissioned(oldStatus));
            string title = toMaint ? "Truck under maintenance" : toDecom ? "Decommission truck" : back ? "Truck back in service" : "Change truck status";
            string message = toMaint ? $"What is wrong with {plate}? It can't be dispatched until it is set back to Active."
                : toDecom ? $"Why is {plate} being decommissioned? It will no longer be offered for trips (it can be reactivated later)."
                : back ? $"What was done to bring {plate} back into service?"
                : $"Why is the status of {plate} changing to {newStatus}?";
            using var f = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false,
                AutoScaleMode = AutoScaleMode.None,
                ClientSize = new Size(D(560), D(toMaint ? 430 : 320)),
                BackColor = AppTheme.Surface,
                Font = new Font("Segoe UI", 9.5F)
            };
            var lbl = new Label { Text = message, Dock = DockStyle.Top, Height = D(50), Padding = new Padding(D(16), D(14), D(16), 0), ForeColor = AppTheme.TextPrimary, UseMnemonic = false };
            var lblDesc = new Label { Text = "Description (required)", Dock = DockStyle.Top, Height = D(24), Padding = new Padding(D(16), D(4), D(16), 0), ForeColor = AppTheme.TextSecondary, Font = new Font("Segoe UI Semibold", 9F), UseMnemonic = false };
            var txt = new TextBox { Multiline = true, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), MaxLength = MJdatabase.TruckStatusDescriptionMax, PlaceholderText = "Description (required, up to 500 characters)" };
            var wrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(D(16), D(2), D(16), D(6)), BackColor = AppTheme.Surface };
            wrap.Controls.Add(txt);
            var err = new Label { Dock = DockStyle.Bottom, Height = D(26), Padding = new Padding(D(16), D(2), D(16), 0), ForeColor = AppTheme.DangerText, Font = new Font("Segoe UI", 8.75F), UseMnemonic = false };
            var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = D(58), FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(D(12), D(8), D(12), D(8)), BackColor = AppTheme.Surface };
            var ok = new Button { Text = toDecom ? "Decommission" : "Save", AutoSize = true, MinimumSize = new Size(D(120), D(36)), FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "Cancel", AutoSize = true, MinimumSize = new Size(D(96), D(36)), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel, Margin = new Padding(0, 0, D(8), 0) };
            ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
            UIThemeHelper.SetButtonRole(ok, "primary");
            UIThemeHelper.SetButtonRole(cancel, "secondary");
            bar.Controls.Add(ok);
            bar.Controls.Add(cancel);

            DateTimePicker? dtpBreak = null, dtpFix = null;
            Panel? dates = null;
            if (toMaint)
            {
                dtpBreak = new DateTimePicker { Width = D(240), Font = new Font("Segoe UI", 10F) };
                Fmt.ApplyDatePicker(dtpBreak);
                dtpBreak.CapAtToday();
                dtpBreak.SetSafe(knownBreakdown.HasValue && knownBreakdown.Value.Date <= DateTime.Today ? knownBreakdown.Value.Date : DateTime.Today);
                dtpFix = new DateTimePicker { Width = D(240), Font = new Font("Segoe UI", 10F) };
                Fmt.ApplyDatePicker(dtpFix);
                dtpFix.SetSafe(knownFix.HasValue ? knownFix.Value.Date : DateTime.Today.AddDays(7));
                dates = new Panel { Dock = DockStyle.Top, Height = D(80), BackColor = AppTheme.Surface, Padding = new Padding(D(16), D(4), D(16), 0) };
                var lb = new Label { Text = "Breakdown date (required)", Left = D(16), Top = D(6), AutoSize = true, ForeColor = AppTheme.TextSecondary, Font = new Font("Segoe UI Semibold", 9F), UseMnemonic = false };
                var lf = new Label { Text = "Estimated fix date (required)", Left = D(290), Top = D(6), AutoSize = true, ForeColor = AppTheme.TextSecondary, Font = new Font("Segoe UI Semibold", 9F), UseMnemonic = false };
                dtpBreak.Left = D(16); dtpBreak.Top = D(30);
                dtpFix.Left = D(290); dtpFix.Top = D(30); dtpFix.Width = D(250);
                dates.Controls.AddRange(new Control[] { lb, lf, dtpBreak, dtpFix });
                dtpBreak.ValueChanged += (s, e) => { if (dtpFix.Value.Date < dtpBreak.Value.Date) dtpFix.SetSafe(dtpBreak.Value.Date); err.Text = ""; };
                dtpFix.ValueChanged += (s, e) => err.Text = "";
            }
            txt.TextChanged += (s, e) => err.Text = "";

            (string, DateTime?, DateTime?)? result = null;
            ok.Click += (s, e) =>
            {
                string? problem = MJdatabase.CheckTruckStatusChange(newStatus, txt.Text, dtpBreak?.Value.Date, dtpFix?.Value.Date);
                if (problem != null) { err.Text = problem; if (txt.Text.Trim().Length == 0) txt.Focus(); return; }
                result = (InputRules.Tidy(txt.Text), dtpBreak?.Value.Date, dtpFix?.Value.Date);
                f.DialogResult = DialogResult.OK;
            };
            f.Controls.Add(wrap);
            if (dates != null) f.Controls.Add(dates);
            f.Controls.Add(lblDesc);
            f.Controls.Add(err);
            f.Controls.Add(bar);
            f.Controls.Add(lbl);
            f.CancelButton = cancel;
            UIThemeHelper.ApplyTheme(f);
            return f.ShowDialog(owner) == DialogResult.OK ? result : null;
        }
    }
}
