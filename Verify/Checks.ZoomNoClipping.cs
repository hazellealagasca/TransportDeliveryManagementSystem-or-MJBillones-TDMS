using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// Display size: at 150 % (and at 100 %) no text may be cut off on any screen or full-page form. Every screen is opened in a host sized like a
/// maximised 1920 x 1080 window and every visible control is measured: text wider / taller than its box, input boxes shorter than their text,
/// controls sticking out of a parent that does not scroll, controls bigger than their table cell.
/// </summary>
public static partial class Checks
{
    private const BindingFlags ZnNonPub = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private sealed class ZnCtx
    {
        public readonly List<string> Issues = new();
        public Form? Host;
        public void Open(string label, Control view, int height = 930)
        {
            Close();
            Host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1830, height), FormBorderStyle = FormBorderStyle.None, Opacity = 0 };
            view.Dock = DockStyle.Fill; Host.Controls.Add(view); Host.Show(); Pump(700);
            if (Math.Abs(UiZoom.Factor - 1f) > 0.001f) UiZoom.Apply(view);   // what the main window does when it shows a screen
            Pump(900);
        }
        public void Scan(string label)
        {
            if (Host == null) return;
            Pump(300);
            Issues.AddRange(ZnScan(Host, label));
        }
        public void Close() { if (Host != null) { try { Host.Close(); Host.Dispose(); } catch { } Host = null; } }
    }

    private static string ZnGroup(float zoom, Action<ZnCtx> body)
    {
        string? db = FromCopy();
        if (db == null) Fresh(); else { MJdatabase.InitializeDatabase(); MJdatabase.RunOneTimeMigrations(); }
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            UiZoom.PersistToDisk = false;
            var ctx = new ZnCtx();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            try
            {
                UiZoom.Set(zoom);
                body(ctx);
            }
            finally { ctx.Close(); UiZoom.Set(1f); UiZoom.PersistToDisk = true; }
            var lines = ctx.Issues.Distinct().ToList();
            if (lines.Count > 40) { int more = lines.Count - 40; lines = lines.Take(40).ToList(); lines.Add($"... and {more} more"); }
            return lines.Count == 0 ? "" : "\n     " + string.Join("\n     ", lines);
        });
    }

    private static void ZnTabs(ZnCtx c, string name, Control hub, Action<string> select, params string[] keys)
    {
        c.Open(name, hub);
        foreach (var k in keys)
        {
            try { select(k); } catch (Exception ex) { c.Issues.Add($"{name} › {k}: could not open ({ex.GetType().Name}: {ex.Message})"); continue; }
            Pump(1200);
            if (Math.Abs(UiZoom.Factor - 1f) > 0.001f) UiZoom.Apply(hub);
            Pump(500);
            c.Scan($"{name} › {k}");
        }
    }

    // ------------------------------------------------------------------ the scan
    private static IEnumerable<string> ZnScan(Control root, string screen)
    {
        var found = new List<string>();
        void Add(Control c, string why)
        {
            string text = c.Text is { Length: > 0 } t ? $" '{(t.Length > 28 ? t.Substring(0, 28) + "…" : t).Replace("\r", " ").Replace("\n", " ")}'" : "";
            string name = string.IsNullOrEmpty(c.Name) ? c.GetType().Name : c.Name;
            string par = c.Parent == null ? "" : (string.IsNullOrEmpty(c.Parent.Name) ? c.Parent.GetType().Name : c.Parent.Name) + "/";
            found.Add($"{screen} › {par}{name}{text}: {why}");
        }
        void Walk(Control c, bool inScroll)
        {
            if (!c.Visible || c.Width < 4 || c.Height < 4) return;
            bool scroll = inScroll || (c is ScrollableControl sc && sc.AutoScroll);
            // text controls
            switch (c)
            {
                case Label l when !l.AutoSize && !l.AutoEllipsis && l.Text.Length > 0 && l.Image == null: CheckText(l, l.Padding, 0, Add); break;
                case CheckBox cb when !cb.AutoSize && !cb.AutoEllipsis && cb.Text.Length > 0 && cb.Image == null && cb.Appearance == Appearance.Normal: CheckText(cb, cb.Padding, 22, Add); break;
                case RadioButton rb when !rb.AutoSize && !rb.AutoEllipsis && rb.Text.Length > 0 && rb.Image == null && rb.Appearance == Appearance.Normal: CheckText(rb, rb.Padding, 22, Add); break;
                case Button b when !b.AutoSize && !b.AutoEllipsis && b.Text.Length > 0 && b.Image == null && b.BackgroundImage == null: CheckText(b, b.Padding, 10, Add); break;
                case TextBox tb when !tb.Multiline: if (tb.Height < (int)Math.Ceiling(tb.Font.GetHeight(tb.DeviceDpi)) + (tb.BorderStyle == BorderStyle.None ? 0 : 4)) Add(tb, $"box height {tb.Height} < text height {(int)Math.Ceiling(tb.Font.GetHeight(tb.DeviceDpi)) + 4} (parent {tb.Parent?.Size}, pad {tb.Parent?.Padding}, dock {tb.Dock}, dpi {tb.DeviceDpi})"); break;
                case ComboBox cx: if (cx.Height < (int)Math.Ceiling(cx.Font.GetHeight(cx.DeviceDpi)) + 4) Add(cx, $"box height {cx.Height} < text height {(int)Math.Ceiling(cx.Font.GetHeight(cx.DeviceDpi)) + 4}"); break;
                case NumericUpDown nu: if (nu.Height < (int)Math.Ceiling(nu.Font.GetHeight(nu.DeviceDpi)) + 4) Add(nu, $"box height {nu.Height} < text height {(int)Math.Ceiling(nu.Font.GetHeight(nu.DeviceDpi)) + 4}"); break;
                case DateTimePicker dp: if (dp.Height < (int)Math.Ceiling(dp.Font.GetHeight(dp.DeviceDpi)) + 4) Add(dp, $"box height {dp.Height} < text height {(int)Math.Ceiling(dp.Font.GetHeight(dp.DeviceDpi)) + 4}"); break;
                case DataGridView g: CheckGrid(g, Add); break;
            }
            // sticking out of / not fitting inside the parent
            if (c.Parent != null && !scroll && c.Parent is not Form && c.Dock == DockStyle.None && c.Parent is not TabControl)
            {
                var pr = c.Parent.ClientRectangle;
                if (c.Parent is TableLayoutPanel tlp) { CheckCell(tlp, c, Add); }
                else if (c.Right > pr.Right + 1 || c.Bottom > pr.Bottom + 1 || c.Left < pr.Left - 1 || c.Top < pr.Top - 1)
                    if (c is not (Label { AutoSize: true } or Panel { Visible: false }) || c.Right > pr.Right + 1 || c.Bottom > pr.Bottom + 1)
                        Add(c, $"sticks out of '{(string.IsNullOrEmpty(c.Parent.Name) ? c.Parent.GetType().Name : c.Parent.Name)}' (control {c.Bounds}, parent {pr.Size})");
            }
            else if (c.Parent is TableLayoutPanel tlp2 && !scroll && c.Dock != DockStyle.None) CheckCell(tlp2, c, Add);
            if (c is DataGridView or ComboBox or NumericUpDown or TextBoxBase or ScrollBar) return;
            foreach (Control k in c.Controls) Walk(k, scroll);
        }
        foreach (Control k in root.Controls) Walk(k, false);
        return found;
    }

    private static void CheckCell(TableLayoutPanel t, Control c, Action<Control, string> add)
    {
        try
        {
            int col = t.GetColumn(c), row = t.GetRow(c);
            if (col < 0 || row < 0) return;
            var widths = t.GetColumnWidths(); var heights = t.GetRowHeights();
            var dr = t.DisplayRectangle; int x = dr.Left + widths.Take(col).Sum(), y = dr.Top + heights.Take(row).Sum();
            int w = widths.Skip(col).Take(Math.Max(1, t.GetColumnSpan(c))).Sum(), h = heights.Skip(row).Take(Math.Max(1, t.GetRowSpan(c))).Sum();
            var cell = new Rectangle(x, y, w, h);
            if (c.Bounds.Right > cell.Right + 2 || c.Bounds.Bottom > cell.Bottom + 2) add(c, $"bigger than its table cell (control {c.Bounds.Size}, cell {cell.Size})");
        }
        catch { }
    }

    private static void CheckText(Control c, Padding pad, int extraW, Action<Control, string> add)
    {
        var f = TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
        string text = c.Text;
        int availW = c.ClientSize.Width - pad.Horizontal - extraW, availH = c.ClientSize.Height - pad.Vertical;
        if (availW < 4) { add(c, $"no room for its text (width {c.ClientSize.Width})"); return; }
        var one = TextRenderer.MeasureText(text, c.Font, new Size(int.MaxValue, 0), TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        var wrapped = TextRenderer.MeasureText(text, c.Font, new Size(availW, 0), f);
        bool explicitLines = text.Contains('\n');
        if (!explicitLines && one.Width <= availW) return;                       // single line that fits
        if (wrapped.Height > availH + 1) add(c, $"text cut: needs {(explicitLines ? wrapped.Width : one.Width)}x{wrapped.Height}, has {availW}x{availH}");
        else if (c is Button or CheckBox or RadioButton && !explicitLines && wrapped.Height > one.Height + 1 && availH < wrapped.Height + 2) add(c, $"text wraps in a short box ({availW}x{availH})");
    }

    private static void CheckGrid(DataGridView g, Action<Control, string> add)
    {
        int cellH = (g.DefaultCellStyle.Font ?? g.Font).Height + 2;
        if (g.RowTemplate.Height < cellH && g.RowTemplate.Height > 0) add(g, $"row height {g.RowTemplate.Height} < text height {cellH}");
        int hdrH = (g.ColumnHeadersDefaultCellStyle.Font ?? g.Font).Height + 4;
        if (g.ColumnHeadersVisible && g.ColumnHeadersHeightSizeMode != DataGridViewColumnHeadersHeightSizeMode.AutoSize && g.ColumnHeadersHeight < hdrH) add(g, $"header height {g.ColumnHeadersHeight} < text height {hdrH}");
        var hf = g.ColumnHeadersDefaultCellStyle.Font ?? g.Font;
        var cut = new List<string>();
        foreach (DataGridViewColumn col in g.Columns)
        {
            if (!col.Visible || string.IsNullOrWhiteSpace(col.HeaderText) || col.AutoSizeMode is DataGridViewAutoSizeColumnMode.Fill or DataGridViewAutoSizeColumnMode.AllCells or DataGridViewAutoSizeColumnMode.ColumnHeader or DataGridViewAutoSizeColumnMode.DisplayedCells) continue;
            if (g.ColumnHeadersDefaultCellStyle.WrapMode == DataGridViewTriState.True) continue;
            if (g.AutoSizeColumnsMode is DataGridViewAutoSizeColumnsMode.Fill or DataGridViewAutoSizeColumnsMode.AllCells or DataGridViewAutoSizeColumnsMode.ColumnHeader && col.AutoSizeMode == DataGridViewAutoSizeColumnMode.NotSet) continue;
            int need = TextRenderer.MeasureText(col.HeaderText, hf, new Size(int.MaxValue, 0), TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width + 10;
            if (col.Width < need - 6 && col.Width > 0) cut.Add($"'{col.HeaderText}' {col.Width}<{need}");
        }
        if (cut.Count > 0) add(g, $"column header(s) cut: {string.Join(", ", cut.Take(4))}{(cut.Count > 4 ? $" (+{cut.Count - 4})" : "")}");
    }

    // ------------------------------------------------------------------ screens
    private static string ZnDashboard(float z) => ZnGroup(z, c => { c.Open("Dashboard", new DashboardView()); Pump(1500); c.Scan("Dashboard"); });

    private static string ZnOperations(float z) => ZnGroup(z, c =>
    {
        var hub = new OperationsHubView();
        ZnTabs(c, "Operations", hub, hub.SelectTab, "Dispatch", "TripReports", "History", "Incidents", "SpecialTrips");
    });

    private static string ZnPayments(float z) => ZnGroup(z, c =>
    {
        var hub = new FinanceHubView("StatementOfAccount");
        ZnTabs(c, "Payments", hub, hub.SelectTab, "StatementOfAccount", "Billing", "Collections", "History");
    });

    private static string ZnResources(float z) => ZnGroup(z, c =>
    {
        var rh = new ResourcesHubView();
        ZnTabs(c, "Resources", rh, rh.SelectTab, "Trucks", "Crew", "CrewHistory", "IncidentLog");
        var mh = new MasterDataHubView();
        ZnTabs(c, "MasterData", mh, mh.SelectTab, "Clients", "Routes", "Rates", "Archive", "History");
    });

    private static string ZnSettings(float z) => ZnGroup(z, c =>
    {
        var sv = new SettingsView(SessionContext.ActiveUser, "Features");
        ZnTabs(c, "Settings", sv, sv.SelectTab, "Features", "RateMatrix", "Categories", "Users", "Reports", "Appearance", "Customization", "PrintLayouts", "AuditTrail", "Backup");
    });

    private static string ZnForms(float z) => ZnGroup(z, c =>
    {
        void Form(string label, Control view, Action open, int h = 870)
        {
            c.Open(label, view, h);
            try { open(); } catch (Exception ex) { c.Issues.Add($"{label}: could not open ({(ex is TargetInvocationException t && t.InnerException != null ? t.InnerException.Message : ex.Message)})"); return; }
            Pump(900);
            if (Math.Abs(UiZoom.Factor - 1f) > 0.001f) UiZoom.Apply(view);
            Pump(600);
            c.Scan(label);
        }
        var dv = new DispatchManagementView();
        Form("New dispatch order", dv, () => dv.OpenNewOrderForm());
        var tv = new TripReportView();
        Form("New trip report", tv, () => typeof(TripReportView).GetMethod("OpenNewTripReport", ZnNonPub)!.Invoke(tv, null));
        var sv = new StatementOfAccountView();
        Form("New statement", sv, () => typeof(StatementOfAccountView).GetMethod("OpenNewStatement", ZnNonPub)!.Invoke(sv, null));
        var bv = new BillingView();
        Form("New service invoice", bv, () => typeof(BillingView).GetMethod("OpenNewRecordForm", ZnNonPub)!.Invoke(bv, null));
        var cv = new CollectionView();
        Form("New collection receipt", cv, () => typeof(CollectionView).GetMethod("OpenNewRecordForm", ZnNonPub)!.Invoke(cv, null));
    });

    [Check] public static string ZoomNoClipping_Dashboard_150() => ZnDashboard(1.5f);
    [Check] public static string ZoomNoClipping_Operations_150() => ZnOperations(1.5f);
    [Check] public static string ZoomNoClipping_Payments_150() => ZnPayments(1.5f);
    [Check] public static string ZoomNoClipping_Resources_150() => ZnResources(1.5f);
    [Check] public static string ZoomNoClipping_Settings_150() => ZnSettings(1.5f);
    [Check] public static string ZoomNoClipping_Forms_150() => ZnForms(1.5f);

    [Check] public static string ZoomNoClipping_Dashboard_100() => ZnDashboard(1f);
    [Check] public static string ZoomNoClipping_Operations_100() => ZnOperations(1f);
    [Check] public static string ZoomNoClipping_Payments_100() => ZnPayments(1f);
    [Check] public static string ZoomNoClipping_Resources_100() => ZnResources(1f);
    [Check] public static string ZoomNoClipping_Settings_100() => ZnSettings(1f);
    [Check] public static string ZoomNoClipping_Forms_100() => ZnForms(1f);
}
