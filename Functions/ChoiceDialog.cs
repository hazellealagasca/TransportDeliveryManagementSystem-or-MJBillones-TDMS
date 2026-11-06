using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// A small message window with buttons named for what they do ("Proceed to Collection Receipt", "Print SOA", "Close").
    /// Returns the index of the button clicked, or the last button's index when the window is closed.
    /// The first button is the main action (filled), the others are secondary.
    /// </summary>
    public static class ChoiceDialog
    {
        public static int Show(IWin32Window? owner, string title, string heading, string body, params string[] buttons)
        {
            if (buttons == null || buttons.Length == 0) buttons = new[] { "OK" };
            int chosen = buttons.Length - 1;
            using var dlg = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = owner == null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent,
                MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false, ShowIcon = false,
                AutoScaleMode = AutoScaleMode.Dpi, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, Font = new Font("Segoe UI", 9.75F)
            };
            try { ThemeManager.StyleWindowChrome(dlg); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            int D(int v) => (int)Math.Round(v * dlg.DeviceDpi / 96f);
            var root = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, Padding = new Padding(D(24), D(20), D(24), D(16)), BackColor = AppTheme.Surface };
            root.Controls.Add(new Label { Text = heading, AutoSize = true, MaximumSize = new Size(D(460), 0), Font = new Font("Segoe UI Semibold", 12F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, 0, 0, D(8)) });
            root.Controls.Add(new Label { Text = body, AutoSize = true, MaximumSize = new Size(D(460), 0), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, 0, 0, D(18)) });
            var row = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0), BackColor = AppTheme.Surface };
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                var b = new Button { Text = buttons[i], AutoSize = true, MinimumSize = new Size(D(110), D(36)), Padding = new Padding(D(10), 0, D(10), 0), Margin = new Padding(0, 0, D(8), 0), Cursor = Cursors.Hand };
                UIThemeHelper.SetButtonRole(b, i == 0 ? "primary" : "secondary");
                b.Click += (s, e) => { chosen = index; dlg.Close(); };
                row.Controls.Add(b);
                if (i == 0) dlg.AcceptButton = b;
                if (i == buttons.Length - 1) dlg.CancelButton = b;
            }
            root.Controls.Add(row);
            dlg.Controls.Add(root);
            dlg.ShowDialog(owner);
            return chosen;
        }
    }
}
