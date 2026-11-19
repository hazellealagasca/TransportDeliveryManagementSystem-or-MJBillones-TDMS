using System.Runtime.InteropServices;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// App-wide: the mouse wheel never changes (or scrolls) an input field. It scrolls the panel the field sits in instead.
    /// Installed once at start-up. Open combo drop-downs, grids, lists, trees and canvases are left alone.
    /// </summary>
    public sealed class WheelGuard : IMessageFilter
    {
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E;

        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point p);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        /// <summary>For tests only; not a user setting.</summary>
        public static bool Enabled = true;

        private static bool installed;

        public static void Install()
        {
            if (installed) return;
            installed = true;
            Application.AddMessageFilter(new WheelGuard());
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (!Enabled || (m.Msg != WM_MOUSEWHEEL && m.Msg != WM_MOUSEHWHEEL)) return false;
            return Process(m.Msg, m.WParam, m.LParam, Cursor.Position);
        }

        /// <summary>True when the message was swallowed (and, where possible, forwarded to the scrolling parent).</summary>
        public static bool Process(int msg, IntPtr wParam, IntPtr lParam, Point screenPoint)
        {
            if (!Enabled || (msg != WM_MOUSEWHEEL && msg != WM_MOUSEHWHEEL)) return false;
            IntPtr h = WindowFromPoint(screenPoint);
            if (h == IntPtr.Zero) return false;
            Control? c = Control.FromChildHandle(h);
            if (c == null) return false;

            // a DataGridView (or its editing control): the grid itself scrolls
            for (Control? p = c; p != null; p = p.Parent)
            {
                if (p is DataGridView grid)
                {
                    if (ReferenceEquals(c, grid)) return false;
                    SendMessage(grid.Handle, msg, wParam, lParam);
                    return true;
                }
            }

            Control? field = InputField(c);
            if (field == null) return false;

            // hand the wheel to the field's parent: it scrolls if it can, otherwise Windows passes it on up
            // to whatever does scroll the page (AutoScroll panels and custom scrollers alike)
            if (field.Parent != null && field.Parent.IsHandleCreated)
                SendMessage(field.Parent.Handle, msg, wParam, lParam);
            return true;
        }

        /// <summary>The input field under the cursor (the owner, for the inner edit of a ComboBox / NumericUpDown), or null.</summary>
        private static Control? InputField(Control c)
        {
            // the inner edit of a NumericUpDown / DomainUpDown is itself a TextBox: use the owner, or the wheel would go to it
            if (c.Parent is UpDownBase || c.Parent is ComboBox) c = c.Parent;
            for (Control? p = c; p != null && p != c.Parent?.Parent?.Parent; p = p.Parent)
            {
                switch (p)
                {
                    case ComboBox cb: return cb.DroppedDown ? null : cb;
                    case NumericUpDown:
                    case DomainUpDown:
                    case DateTimePicker:
                    case TrackBar:
                    case TextBox:
                    case MaskedTextBox:
                    case RichTextBox:
                        return p;
                }
                if (p is Form) break;
            }
            return null;
        }
    }
}
