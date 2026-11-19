using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace TDMS.Verify;

public static partial class Checks
{
    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "WindowFromPoint")] private static extern IntPtr WindowFromPointT(Point p);

    [Check]
    public static string WheelGuard_FieldsSwallowedAndPanelScrolls()
    {
        return RunOnSta(() =>
        {
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(100, 100), ClientSize = new Size(500, 400) };
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Left = 10, Top = 10, Width = 200 };
            combo.Items.AddRange(new object[] { "a", "b", "c" }); combo.SelectedIndex = 0;
            var num = new NumericUpDown { Left = 10, Top = 50, Width = 120, Value = 5 };
            var memo = new TextBox { Multiline = true, Left = 10, Top = 90, Width = 200, Height = 60 };
            var list = new ListBox { Left = 10, Top = 160, Width = 200, Height = 60 };
            for (int i = 0; i < 30; i++) list.Items.Add("i" + i);
            var grid = new DataGridView { Left = 10, Top = 230, Width = 300, Height = 80 };
            var tail = new Label { Left = 10, Top = 1500, Width = 50, Height = 20, Text = "end" };
            panel.Controls.AddRange(new Control[] { combo, num, memo, list, grid, tail });
            host.Controls.Add(panel);
            host.Show(); host.Activate(); Pump(400);

            IntPtr wp = (IntPtr)unchecked((-120) << 16);
            bool Wheel(Control c)
            {
                var pt = c.PointToScreen(new Point(c.Width / 2, c.Height / 2));
                IntPtr lp = (IntPtr)((pt.Y << 16) | (pt.X & 0xFFFF));
                bool r = WheelGuard.Process(0x020A, wp, lp, pt);
                Pump(100);
                return r;
            }
            if (!Wheel(combo)) { var pt0 = combo.PointToScreen(new Point(5, 5)); return "combo not swallowed; under cursor: " + (Control.FromChildHandle(WindowFromPointT(pt0))?.GetType().Name ?? "null") + " vis=" + host.Visible; }
            if (combo.SelectedIndex != 0) return "combo changed";
            if (panel.VerticalScroll.Value <= 0) return "panel did not scroll";
            panel.AutoScrollPosition = new Point(0, 0); Pump(100);
            if (!Wheel(num)) return "numeric not swallowed";
            if (num.Value != 5) return "numeric changed";
            if (!Wheel(memo)) return "textbox not swallowed";
            if (Wheel(list)) return "listbox swallowed";
            if (Wheel(grid)) return "grid swallowed";
            WheelGuard.Enabled = false;
            bool off = Wheel(combo);
            WheelGuard.Enabled = true;
            if (off) return "Enabled=false still swallowed";
            host.Close();
            return "";
        });
    }
}
