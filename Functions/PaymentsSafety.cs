using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Scrolling the page must never change a number, a choice or a date. Every <see cref="NumericUpDown"/>, <see cref="ComboBox"/>,
    /// <see cref="DateTimePicker"/> and <see cref="DomainUpDown"/> under the root (also ones added later, and read-only ones) ignores the
    /// mouse wheel and hands the scroll to the page instead. A drop-down list that is open still scrolls its own list.
    /// Read-only numbers also stop reacting to the up / down arrow keys. Pure Windows Forms.
    /// </summary>
    public static class NoWheel
    {
        private static readonly ConditionalWeakTable<Control, object> done = new();

        /// <summary>Protects <paramref name="root"/> and everything inside it, now and in the future.</summary>
        public static void Attach(Control? root)
        {
            if (root == null) return;
            Walk(root);
        }

        private static void Walk(Control c)
        {
            if (!done.TryGetValue(c, out _))
            {
                done.Add(c, new object());
                c.ControlAdded += (s, e) => { if (e.Control != null) Walk(e.Control); };
                if (c is NumericUpDown or ComboBox or DateTimePicker or DomainUpDown) c.MouseWheel += Block;
                if (c is NumericUpDown n && n.ReadOnly) n.InterceptArrowKeys = false;
            }
            foreach (Control child in c.Controls) Walk(child);
        }

        private static void Block(object? sender, MouseEventArgs e)
        {
            if (sender is not Control c) return;
            if (c is ComboBox cb && cb.DroppedDown) return; // let an open list scroll itself
            if (e is HandledMouseEventArgs h) h.Handled = true;
            for (Control? p = c.Parent; p != null; p = p.Parent)
            {
                if (p is ScrollableControl sc && sc.VerticalScroll.Visible)
                {
                    var pos = sc.AutoScrollPosition; // negative offsets; setting takes positive ones
                    sc.AutoScrollPosition = new Point(-pos.X, -pos.Y - e.Delta);
                    break;
                }
            }
        }
    }

    /// <summary>Stops a control from repainting while it is rebuilt, then paints it once (no half-drawn screens when tabs switch).</summary>
    public readonly struct RedrawFreeze : IDisposable
    {
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0x000B;
        private readonly Control? c;

        public RedrawFreeze(Control? control)
        {
            c = control != null && control.IsHandleCreated ? control : null;
            if (c != null) SendMessage(c.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }

        public void Dispose()
        {
            if (c == null || c.IsDisposed) return;
            SendMessage(c.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            c.Invalidate(true);
        }
    }

    /// <summary>Turns on double-buffering for a control whose class does not expose it (the property is protected).</summary>
    public static class Smooth
    {
        private static readonly PropertyInfo? prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void Enable(Control? c)
        {
            try { if (c != null) prop?.SetValue(c, true); } catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
}
