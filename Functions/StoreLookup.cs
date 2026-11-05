using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>What is wrong with a store typed into a store table.</summary>
    public enum StoreProblem { None, Empty, NoClient, NotRegistered, OtherClient, Mismatch }



    /// <summary>Result of checking a store code / name against the selected client's registered stores.</summary>
    public sealed class StoreCheck
    {
        public StoreProblem Problem { get; init; }
        public ClientStore? Store { get; init; }
        /// <summary>Stores of other clients with this code or name (for the "not this client's store" message).</summary>
        public List<ClientStore> OtherClients { get; init; } = new List<ClientStore>();
        public string Message { get; init; } = "";
        public bool Ok => Problem == StoreProblem.None && Store != null;
    }



    /// <summary>Checks store codes and names against Master Data (the client's registered stores).</summary>
    public static class StoreDirectory
    {
        public const string NeedClientMessage = "Please select a Client Company first before entering store details.";
        public static string Norm(string? s) => MJdatabase.CleanStoreText(s);
        public static bool Same(string? a, string? b) => string.Equals(Norm(a), Norm(b), StringComparison.OrdinalIgnoreCase);
        public static string Route(ClientStore? st) => Norm(st?.DestinationRoute);



        public static ClientStore? ByCode(List<ClientStore> stores, string? code) { string c = Norm(code); return c.Length == 0 ? null : stores.FirstOrDefault(s => Same(s.StoreCode, c)); }
        public static ClientStore? ByName(List<ClientStore> stores, string? name) { string n = Norm(name); return n.Length == 0 ? null : stores.FirstOrDefault(s => Same(s.StoreName, n)); }



        /// <summary>The client's stores matching a partial code or name (case ignored): exact first, then starts-with, then contains.</summary>
        public static List<ClientStore> Search(List<ClientStore> stores, string? typed)
        {
            string t = Norm(typed);
            if (t.Length == 0) return stores.OrderBy(s => Norm(s.StoreCode), StringComparer.OrdinalIgnoreCase).ToList();
            int Rank(ClientStore s)
            {
                string c = Norm(s.StoreCode), n = Norm(s.StoreName);
                if (c.Equals(t, StringComparison.OrdinalIgnoreCase) || n.Equals(t, StringComparison.OrdinalIgnoreCase)) return 0;
                if (c.StartsWith(t, StringComparison.OrdinalIgnoreCase) || n.StartsWith(t, StringComparison.OrdinalIgnoreCase)) return 1;
                if (c.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) return 2;
                return 9;
            }
            return stores.Select(s => (S: s, R: Rank(s))).Where(x => x.R < 9).OrderBy(x => x.R).ThenBy(x => Norm(x.S.StoreName), StringComparer.OrdinalIgnoreCase).Select(x => x.S).ToList();
        }



        /// <summary>
        /// Checks a code and/or name for <paramref name="client"/>. Both given: they must be the same store.
        /// One given: it must be a store of this client. Not found: says if another client has it.
        /// </summary>
        public static StoreCheck Check(string client, List<ClientStore> stores, string? code, string? name)
        {
            string c = Norm(code), n = Norm(name);
            if (Norm(client).Length == 0) return new StoreCheck { Problem = StoreProblem.NoClient, Message = NeedClientMessage };
            if (c.Length == 0 && n.Length == 0) return new StoreCheck { Problem = StoreProblem.Empty };
            var byCode = ByCode(stores, c);
            var byName = ByName(stores, n);
            if (c.Length > 0 && n.Length > 0)
            {
                if (byCode != null && byName != null && byCode.Id == byName.Id) return new StoreCheck { Problem = StoreProblem.None, Store = byCode };
                if (byCode != null || byName != null)
                {
                    string msg = byCode != null
                        ? $"Store code {Norm(byCode.StoreCode)} is \"{Norm(byCode.StoreName)}\", not \"{n}\". The code and name must be the same store."
                        : $"\"{Norm(byName!.StoreName)}\" has store code {Norm(byName.StoreCode)}, not {c}. The code and name must be the same store.";
                    return new StoreCheck { Problem = StoreProblem.Mismatch, Store = byCode ?? byName, Message = msg };
                }
            }
            else
            {
                var hit = byCode ?? byName;
                if (hit != null) return new StoreCheck { Problem = StoreProblem.None, Store = hit };
            }
            // Not one of this client's stores: is it another client's?
            string typed = c.Length > 0 ? c : n;
            List<ClientStore> others;
            try { others = MJdatabase.FindStoresAnyClient(typed).Where(s => !Same(s.ClientCompanyName, client)).ToList(); }
            catch (Exception ex) { ErrLog.Swallowed(ex); others = new List<ClientStore>(); }
            if (others.Count > 0)
            {
                var o = others[0];
                return new StoreCheck
                {
                    Problem = StoreProblem.OtherClient,
                    OtherClients = others,
                    Message = $"\"{typed}\" is registered under {Norm(o.ClientCompanyName)} ({Norm(o.StoreCode)} - {Norm(o.StoreName)}), not under {Norm(client)}."
                };
            }
            return new StoreCheck { Problem = StoreProblem.NotRegistered, Message = $"\"{typed}\" is not registered under {Norm(client)}." };
        }
    }



    /// <summary>
    /// Floating list under a store cell or box while typing: Code | Name | Route of the client's matching stores.
    /// It never takes the keyboard focus (typing stays in the cell); pick with a click, or with Enter / Tab via
    /// <see cref="Selected"/>. When nothing matches it shows how to register the store.
    /// </summary>
    public sealed class StoreLookupPopup : Form
    {
        /// <summary>Show the Route column (off in the dispatch store table: there every row takes the order's route).</summary>
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool ShowRoute { get; set; } = true;
        private const int WS_EX_NOACTIVATE = 0x08000000, WS_EX_TOOLWINDOW = 0x00000080, WM_MOUSEACTIVATE = 0x0021, MA_NOACTIVATE = 3;
        private const int MaxRows = 8;
        private readonly RowsView view;
        private List<ClientStore> items = new List<ClientStore>();
        private int selected = -1, extra;
        private bool noMatch;
        /// <summary>A store was clicked.</summary>
        public event Action<ClientStore>? Picked;
        public ClientStore? Selected => selected >= 0 && selected < items.Count ? items[selected] : null;
        public bool ShowsNoMatch => Visible && noMatch;



        public StoreLookupPopup()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = AppTheme.BorderStrong;
            Padding = new Padding(1);
            view = new RowsView(this) { Dock = DockStyle.Fill };
            Controls.Add(view);
        }
        protected override bool ShowWithoutActivation => true;
        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW; return cp; }
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE) { m.Result = (IntPtr)MA_NOACTIVATE; return; }
            base.WndProc(ref m);
        }



        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private int RowH => D(28);



        /// <summary>Shows the matches under <paramref name="screenRect"/> (the cell or box, in screen coordinates).</summary>
        public void ShowMatches(Control anchor, Rectangle screenRect, List<ClientStore> matches)
        {
            var owner = anchor.FindForm();
            if (owner == null || anchor.IsDisposed) return;
            items = matches.Take(MaxRows).ToList();
            extra = Math.Max(0, matches.Count - items.Count);
            noMatch = matches.Count == 0;
            selected = items.Count > 0 ? 0 : -1;
            int rows = 1 + (noMatch ? 2 : items.Count) + (extra > 0 ? 1 : 0); // the "no match" note takes two lines
            int w = Math.Max(D(440), screenRect.Width);
            var size = new Size(w, rows * RowH + 2);
            var wa = Screen.FromRectangle(screenRect).WorkingArea;
            int x = Math.Max(wa.Left, Math.Min(screenRect.Left, wa.Right - size.Width));
            int y = screenRect.Bottom + 1;
            if (y + size.Height > wa.Bottom) y = Math.Max(wa.Top, screenRect.Top - size.Height - 1); // no room below: show above
            Bounds = new Rectangle(new Point(x, y), size);
            if (Owner != owner) { if (Visible) Hide(); Owner = owner; }
            if (!Visible) Show(owner);
            view.Invalidate();
        }



        /// <summary>Moves the highlighted row (Up / Down keys). Returns false when there is nothing to move through.</summary>
        public bool MoveSelection(int delta)
        {
            if (!Visible || items.Count == 0) return false;
            selected = (selected + delta + items.Count) % items.Count;
            view.Invalidate();
            return true;
        }



        public void HideList() { if (Visible) Hide(); }



        /// <summary>Draws the rows; it can't take the focus, so typing stays in the cell.</summary>
        private sealed class RowsView : Control
        {
            private readonly StoreLookupPopup p;
            public RowsView(StoreLookupPopup popup)
            {
                p = popup;
                SetStyle(ControlStyles.Selectable, false);
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Hand;
            }
            private int Hit(Point pt) { int i = pt.Y / p.RowH - 1; return i >= 0 && i < p.items.Count && !p.noMatch ? i : -1; }
            protected override void OnMouseMove(MouseEventArgs e) { int i = Hit(e.Location); if (i >= 0 && i != p.selected) { p.selected = i; Invalidate(); } base.OnMouseMove(e); }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button != MouseButtons.Left) return;
                int i = Hit(e.Location);
                if (i >= 0) { var st = p.items[i]; p.HideList(); p.Picked?.Invoke(st); }
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(AppTheme.Surface);
                int h = p.RowH, w = Width;
                int c1 = (int)(w * (p.ShowRoute ? 0.24 : 0.30)), c2 = p.ShowRoute ? (int)(w * 0.46) : w - (int)(w * 0.30);
                var pad = p.D(8);
                using var headFont = new Font("Segoe UI Semibold", 9F);
                using var font = new Font("Segoe UI", 9.5F);
                using var bold = new Font("Segoe UI Semibold", 9.5F);
                using (var hb = new SolidBrush(AppTheme.SurfaceAlt)) g.FillRectangle(hb, 0, 0, w, h);
                void Cell(string t, Font f, Color c, int x, int y, int cw)
                    => TextRenderer.DrawText(g, t, f, new Rectangle(x + pad, y, Math.Max(0, cw - pad * 2), h), c, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                Cell("Store Code", headFont, AppTheme.TextSecondary, 0, 0, c1);
                Cell("Store Name", headFont, AppTheme.TextSecondary, c1, 0, c2);
                if (p.ShowRoute) Cell("Route", headFont, AppTheme.TextSecondary, c1 + c2, 0, w - c1 - c2);
                using (var lp = new Pen(AppTheme.Border)) g.DrawLine(lp, 0, h - 1, w, h - 1);
                if (p.noMatch)
                {
                    TextRenderer.DrawText(g, "No matching registered store found — Press Enter to register this store under Client Company Routes.", bold,
                        new Rectangle(pad, h, Math.Max(0, w - pad * 2), h * 2), AppTheme.WarningText,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                    return;
                }
                for (int i = 0; i < p.items.Count; i++)
                {
                    int y = (i + 1) * h;
                    var st = p.items[i];
                    bool sel = i == p.selected;
                    if (sel) using (var sb = new SolidBrush(AppTheme.AccentSoftStrong)) g.FillRectangle(sb, 0, y, w, h);
                    Color fc = AppTheme.TextPrimary;
                    Cell(StoreDirectory.Norm(st.StoreCode), bold, fc, 0, y, c1);
                    Cell(StoreDirectory.Norm(st.StoreName), font, fc, c1, y, c2);
                    if (!p.ShowRoute) continue;
                    string route = StoreDirectory.Route(st);
                    Cell(route.Length > 0 ? route : "(no route)", font, route.Length > 0 ? AppTheme.TextSecondary : AppTheme.TextMuted, c1 + c2, y, w - c1 - c2);
                }
                if (p.extra > 0)
                    Cell($"+{p.extra} more — keep typing to narrow the list", font, AppTheme.TextMuted, 0, (p.items.Count + 1) * h, w);
            }
        }
    }
}
