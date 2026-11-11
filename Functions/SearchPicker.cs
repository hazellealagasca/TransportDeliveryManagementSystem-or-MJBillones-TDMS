using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Type-to-search for a pick-only ComboBox (DropDownList) such as the Truck and Driver boxes. When the box has focus and a letter or digit is typed,
    /// a small list opens under it with every item whose search text CONTAINS what was typed (any part of the text, not only the start). Picking one sets the
    /// box's SelectedIndex to that item's ORIGINAL position, so the many places that read <c>list[box.SelectedIndex]</c> and every SelectedIndexChanged handler
    /// work exactly as they do when the item is picked from the normal drop-down.
    /// The box itself is not touched: its items, their order and its DropDownList style stay as they are. No Windows AutoComplete is used (it caused a
    /// cross-thread error when a window closed). The list is a plain owned window that never takes focus; it is disposed with the box.
    /// Esc, Tab, clicking elsewhere or leaving the box closes it without changing the pick.
    /// </summary>
    public sealed class SearchPicker : Form
    {
        private const int WS_EX_NOACTIVATE = 0x08000000, WS_EX_TOOLWINDOW = 0x00000080, WM_MOUSEACTIVATE = 0x0021, MA_NOACTIVATE = 3;
        public const string HintText = "Type to search";
        private const int MaxRows = 8;

        private readonly ComboBox box;
        private readonly Func<int, string> searchText;
        private readonly List<int> matches = new List<int>();   // original item indexes, in the box's own order
        private int selected = -1, extra;
        private string query = "";
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private int RowH => D(30);

        private SearchPicker(ComboBox box, Func<int, string> searchText)
        {
            this.box = box; this.searchText = searchText;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = AppTheme.BorderStrong;
            Padding = new Padding(1);
            DoubleBuffered = true;
            SetStyle(ControlStyles.Selectable, false);
        }
        protected override bool ShowWithoutActivation => true;
        protected override CreateParams CreateParams { get { var cp = base.CreateParams; cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW; return cp; } }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE) { m.Result = (IntPtr)MA_NOACTIVATE; return; }
            base.WndProc(ref m);
        }

        /// <summary>
        /// Adds type-to-search to <paramref name="box"/>. <paramref name="searchText"/> gives the text to search for item number i (its position in the box).
        /// Returns the picker (disposed automatically with the box).
        /// </summary>
        public static SearchPicker Attach(ComboBox box, Func<int, string> searchText)
        {
            var p = new SearchPicker(box, searchText);
            box.KeyPress += (s, e) => p.HandleKeyPress(e);
            box.PreviewKeyDown += (s, e) => { if (p.Visible && (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)) e.IsInputKey = true; };
            box.KeyDown += (s, e) => p.HandleKeyDown(e);
            box.Leave += (s, e) => p.Close2();
            box.MouseDown += (s, e) => p.Close2();
            box.DropDown += (s, e) => p.Close2();
            box.SelectedIndexChanged += (s, e) => p.Close2();
            box.EnabledChanged += (s, e) => p.Close2();
            box.VisibleChanged += (s, e) => p.Close2();
            box.ParentChanged += (s, e) => p.Close2();
            box.HandleDestroyed += (s, e) => p.Close2();
            box.Disposed += (s, e) => { try { p.Close(); p.Dispose(); } catch (Exception ex) { ErrLog.Swallowed(ex); } };
            new ToolTip().SetToolTip(box, HintText);
            return p;
        }

        /// <summary>Draws the grey "Type to search" hint in a box that has nothing picked (call from the box's DrawItem when e.Index is below 0).</summary>
        public static void DrawHint(ComboBox box, DrawItemEventArgs e)
        {
            e.DrawBackground();
            TextRenderer.DrawText(e.Graphics, HintText, box.Font, e.Bounds, AppTheme.TextMuted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }

        /// <summary>The original item numbers whose search text contains <paramref name="typed"/> (case-insensitive), in the box's own order.</summary>
        public static List<int> Match(int itemCount, Func<int, string> searchText, string typed)
        {
            var res = new List<int>();
            string q = (typed ?? "").Trim();
            if (q.Length == 0) return res;
            for (int i = 0; i < itemCount; i++)
                if ((searchText(i) ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) res.Add(i);
            return res;
        }

        private void HandleKeyPress(KeyPressEventArgs e)
        {
            if (box.DroppedDown || !box.Enabled) return;
            char c = e.KeyChar;
            if (c == '\b')
            {
                if (query.Length == 0) return;
                query = query.Substring(0, query.Length - 1);
                e.Handled = true;
                Refresh2();
                return;
            }
            if (char.IsControl(c)) return;
            if (c == ' ' && query.Length == 0) return;
            query += c;
            e.Handled = true; // stops the box jumping to the first item that starts with the letter (that would change the pick)
            Refresh2();
        }

        private void HandleKeyDown(KeyEventArgs e)
        {
            if (!Visible) return;
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up)
            {
                if (matches.Count > 0) selected = (selected + (e.KeyCode == Keys.Down ? 1 : -1) + matches.Count) % matches.Count;
                Invalidate();
                e.Handled = true; e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (selected >= 0 && selected < matches.Count) Pick(matches[selected]);
                e.Handled = true; e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape) { Close2(); e.Handled = true; e.SuppressKeyPress = true; }
        }

        private void Pick(int original)
        {
            Close2();
            if (original >= 0 && original < box.Items.Count && box.SelectedIndex != original) box.SelectedIndex = original;
        }

        /// <summary>Hides the list and forgets what was typed. The box keeps whatever is picked.</summary>
        public void Close2() { query = ""; matches.Clear(); selected = -1; if (!IsDisposed && Visible) Hide(); }

        private void Refresh2()
        {
            if (IsDisposed) return;
            if (query.Length == 0) { Close2(); return; }
            var all = Match(box.Items.Count, searchText, query);
            matches.Clear();
            matches.AddRange(all.Take(MaxRows));
            extra = Math.Max(0, all.Count - matches.Count);
            selected = matches.Count > 0 ? 0 : -1;
            var owner = box.FindForm();
            if (owner == null || !box.IsHandleCreated) return;
            var r = box.RectangleToScreen(box.ClientRectangle);
            int rows = 1 + Math.Max(1, matches.Count) + (extra > 0 ? 1 : 0); // header + matches (or a no-match line) + more line
            var size = new Size(Math.Max(D(300), r.Width), rows * RowH + 2);
            var wa = Screen.FromRectangle(r).WorkingArea;
            int x = Math.Max(wa.Left, Math.Min(r.Left, wa.Right - size.Width));
            int y = r.Bottom + 1;
            if (y + size.Height > wa.Bottom) y = Math.Max(wa.Top, r.Top - size.Height - 1);
            Bounds = new Rectangle(new Point(x, y), size);
            if (Owner != owner) { if (Visible) Hide(); Owner = owner; }
            if (!Visible) Show(owner);
            Invalidate();
        }

        private int Hit(Point pt) { int i = (pt.Y - 1) / RowH - 1; return i >= 0 && i < matches.Count ? i : -1; }
        protected override void OnMouseMove(MouseEventArgs e) { int i = Hit(e.Location); if (i >= 0 && i != selected) { selected = i; Invalidate(); } base.OnMouseMove(e); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            int i = Hit(e.Location);
            if (i >= 0) Pick(matches[i]);
        }

        private static (Color Dot, string Tag) Status(string text)
        {
            if (text.StartsWith("[ACTIVE]", StringComparison.OrdinalIgnoreCase) || text.StartsWith("[AVAILABLE]", StringComparison.OrdinalIgnoreCase)) return (Pal.C(16, 185, 129), "ACTIVE");
            if (text.StartsWith("[ON TRIP]", StringComparison.OrdinalIgnoreCase)) return (Pal.C(249, 115, 22), "ON TRIP");
            if (text.StartsWith("[MAINTENANCE]", StringComparison.OrdinalIgnoreCase)) return (Pal.C(239, 68, 68), "MAINTENANCE");
            return (Color.Empty, "");
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(AppTheme.Surface);
            int h = RowH, w = Width, pad = D(10), y0 = 1;
            using var bold = new Font("Segoe UI Semibold", 9.5F);
            using var small = new Font("Segoe UI", 8.5F);
            var flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
            using (var hb = new SolidBrush(AppTheme.SurfaceAlt)) g.FillRectangle(hb, 1, y0, w - 2, h);
            TextRenderer.DrawText(g, $"Search: {query}", bold, new Rectangle(pad, y0, w - pad * 2, h), AppTheme.TextPrimary, flags);
            using var lp = new Pen(AppTheme.Border);
            g.DrawLine(lp, 1, y0 + h - 1, w - 1, y0 + h - 1);
            if (matches.Count == 0)
            {
                TextRenderer.DrawText(g, "No match. Backspace to change it, Esc to close.", small, new Rectangle(pad, y0 + h, w - pad * 2, h), AppTheme.TextMuted, flags);
                return;
            }
            for (int i = 0; i < matches.Count; i++)
            {
                int y = y0 + (i + 1) * h;
                if (i == selected) using (var sb = new SolidBrush(AppTheme.AccentSoftStrong)) g.FillRectangle(sb, 1, y, w - 2, h);
                string full = matches[i] < box.Items.Count ? box.Items[matches[i]]?.ToString() ?? "" : "";
                var (dot, tag) = Status(full);
                int tx = pad;
                if (dot != Color.Empty)
                {
                    int ds = D(8);
                    using (var db = new SolidBrush(dot)) g.FillEllipse(db, pad, y + (h - ds) / 2, ds, ds);
                    tx = pad + ds + D(8);
                }
                string label = tag.Length > 0 ? full.Substring(full.IndexOf(']') + 1).Trim() : full;
                int tagW = 0;
                if (tag.Length > 0)
                {
                    tagW = TextRenderer.MeasureText(tag, small).Width + D(6);
                    TextRenderer.DrawText(g, tag, small, new Rectangle(w - pad - tagW, y, tagW, h), AppTheme.TextMuted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }
                TextRenderer.DrawText(g, label, bold, new Rectangle(tx, y, w - tx - pad - tagW, h), AppTheme.TextPrimary, flags);
                g.DrawLine(lp, 1, y + h - 1, w - 1, y + h - 1);
            }
            if (extra > 0)
                TextRenderer.DrawText(g, $"+{extra} more. Keep typing to narrow the list.", small, new Rectangle(pad, y0 + (matches.Count + 1) * h, w - pad * 2, h), AppTheme.TextMuted, flags);
        }
    }
}
