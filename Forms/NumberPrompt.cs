using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>Asks for one typed number (an SI No., a receipt no.). The check runs before OK closes the box. Returns null when cancelled.</summary>
    public static class NumberPrompt
    {
        public static string? Ask(IWin32Window? owner, string title, string message, string placeholder, int maxLength, Func<string, string?> check, string okText = "Save")
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
                ClientSize = new Size(D(480), D(210)),
                BackColor = AppTheme.Surface,
                Font = new Font("Segoe UI", 9.5F)
            };
            var lbl = new Label { Text = message, Dock = DockStyle.Top, Height = D(70), Padding = new Padding(D(16), D(14), D(16), 0), ForeColor = AppTheme.TextPrimary, UseMnemonic = false };
            var txt = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 11F), PlaceholderText = placeholder, MaxLength = maxLength };
            var wrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(D(16), D(4), D(16), D(4)), BackColor = AppTheme.Surface };
            wrap.Controls.Add(txt);
            var err = new Label { Dock = DockStyle.Bottom, Height = D(34), Padding = new Padding(D(16), 0, D(16), 0), ForeColor = AppTheme.DangerText, Font = new Font("Segoe UI", 8.75F) };
            var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = D(58), FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(D(12), D(8), D(12), D(8)), BackColor = AppTheme.Surface };
            var ok = new Button { Text = okText, AutoSize = true, MinimumSize = new Size(D(110), D(36)), FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "Cancel", AutoSize = true, MinimumSize = new Size(D(96), D(36)), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel, Margin = new Padding(0, 0, D(8), 0) };
            ok.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.BorderSize = 0;
            UIThemeHelper.SetButtonRole(ok, "primary");
            UIThemeHelper.SetButtonRole(cancel, "secondary");
            ok.Click += (s, e) =>
            {
                string? why = null;
                try { why = check(txt.Text.Trim()); } catch (Exception ex) { ErrLog.Swallowed(ex); why = "Couldn't check the number: " + ex.Message; }
                if (why != null) { err.Text = why; txt.Focus(); txt.SelectAll(); return; }
                f.DialogResult = DialogResult.OK;
            };
            txt.TextChanged += (s, e) => err.Text = "";
            bar.Controls.Add(ok);
            bar.Controls.Add(cancel);
            f.Controls.Add(wrap);
            f.Controls.Add(err);
            f.Controls.Add(bar);
            f.Controls.Add(lbl);
            f.AcceptButton = ok;
            f.CancelButton = cancel;
            UIThemeHelper.ApplyTheme(f);
            return f.ShowDialog(owner) == DialogResult.OK ? txt.Text.Trim() : null;
        }
    }
}
