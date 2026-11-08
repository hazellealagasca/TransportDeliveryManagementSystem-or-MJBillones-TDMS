using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// Looking tools and the wheel guard check.
/// Screenshot (on a COPY of the data):  TDMS.Verify.exe --shot hub:Billing|hub:StatementOfAccount|hub:Collections|hub:History|billing|soa|coll &lt;db&gt; &lt;out.png&gt; [width height]
/// </summary>
public static partial class Checks
{
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    private const int WM_MOUSEWHEEL = 0x020A;

    public static int Shot(string[] a)
    {
        if (a.Length < 4) { Console.WriteLine("usage: --shot <hub:Tab|billing|soa|coll> <db> <out.png> [w h]"); return 2; }
        string which = a[1], png = a[3];
        int w = a.Length > 4 ? int.Parse(a[4]) : 1920, h = a.Length > 5 ? int.Parse(a[5]) : 1080;
        RealDb = a[2];
        if (FromCopy() == null) { Console.WriteLine("no database"); return 2; }
        string r = RunOnSta(() =>
        {
            if (Environment.GetEnvironmentVariable("TDMS_SHOT_ZOOM") is string zs && float.TryParse(zs, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float zf)) { UiZoom.PersistToDisk = false; UiZoom.Set(zf); }
            Control view = which.StartsWith("hub:") ? new FinanceHubView(which.Substring(4))
                : which == "billing" ? new BillingView() : which == "soa" ? new StatementOfAccountView() : new CollectionView();
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(w, h), FormBorderStyle = FormBorderStyle.None };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(2500);
            if (a.Length > 6 && a[6].Contains(":"))
            {
                Control? target = (Control?)FindFirst<CollectionView>(view) ?? (Control?)FindFirst<BillingView>(view) ?? FindFirst<StatementOfAccountView>(view);
                if (view is CollectionView or BillingView or StatementOfAccountView) target = view;
                foreach (var step in a[6].Split('+'))
                {
                    if (step.StartsWith("open:"))
                    {
                        var m = target?.GetType().GetMethod(step.Substring(5), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                        if (m == null) return "method not found: " + step;
                        m.Invoke(target, m.GetParameters().Length == 0 ? null : m.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : null).ToArray());
                    }
                    else if (step.StartsWith("start:")) // start:<client>  (Statement of Account: "Make statement" for that client's ready invoices)
                    {
                        var soa = target as StatementOfAccountView;
                        if (soa == null) return "start: needs the statement screen";
                        soa.StartSoaFor(step.Substring(6), DateTime.Today.AddDays(-90), DateTime.Today);
                    }
                    else if (step.StartsWith("sel:")) // sel:<comboField>:<index>
                    {
                        var parts = step.Split(':');
                        var cb = target == null ? null : FieldOf<ComboBox>(target, parts[1]);
                        if (cb == null) return "combo not found: " + parts[1];
                        cb.SelectedIndex = int.Parse(parts[2]);
                    }
                    else if (step.StartsWith("txt:")) // txt:<boxField>:<text>
                    {
                        var parts = step.Split(':', 3);
                        var tb = target == null ? null : FieldOf<TextBox>(target, parts[1]);
                        if (tb == null) return "text box not found: " + parts[1];
                        tb.Text = parts[2];
                    }
                    Pump(700);
                }
                Pump(800);
            }
            using var bmp = new Bitmap(w, h);
            host.DrawToBitmap(bmp, new Rectangle(0, 0, w, h));
            bmp.Save(png, ImageFormat.Png);
            host.Close();
            return "";
        });
        Console.WriteLine(r.Length == 0 ? "saved " + png : "FAILED " + r);
        return r.Length == 0 ? 0 : 1;
    }

    private static T? FindFirst<T>(Control root) where T : Control
    {
        foreach (Control c in root.Controls)
        {
            if (c is T t) return t;
            var f = FindFirst<T>(c);
            if (f != null) return f;
        }
        return null;
    }

    private static decimal WheelOn(NumericUpDown n, int delta)
    {
        var p = n.PointToScreen(new Point(n.Width / 2, n.Height / 2));
        IntPtr lp = (IntPtr)((p.Y << 16) | (p.X & 0xFFFF));
        SendMessage(n.Handle, WM_MOUSEWHEEL, (IntPtr)(delta << 16), lp);
        Application.DoEvents();
        return n.Value;
    }

    [Check]
    public static string Payments_MouseWheelCannotChangeAnyMoneyBox()
    {
        Fresh(); RunBackfill();
        return RunOnSta(() =>
        {
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), Size = new Size(1500, 950), Opacity = 0 };
            // a plain money box, no guard: the wheel DOES change it (proves the test can see the bug)
            var bare = new NumericUpDown { Maximum = 1000000, DecimalPlaces = 2, Value = 100, Left = 5, Top = 5, Width = 120 };
            host.Controls.Add(bare);
            var hub = new FinanceHubView("StatementOfAccount") { Dock = DockStyle.Fill };
            host.Controls.Add(hub);
            host.Show(); Pump(1500);
            bare.Focus(); decimal bareAfter = WheelOn(bare, 120);
            var soa = FindFirst<StatementOfAccountView>(hub);
            if (soa == null) return "statement screen not found";
            var report = new List<string>();
            foreach (var name in new[] { "numTotalSales", "numVat", "numNetOfVat", "numFuelSurchargeTotal", "numWithholdingTax", "numTotalAmountDue" })
            {
                var n = FieldOf<NumericUpDown>(soa, name);
                if (n == null) { report.Add(name + " missing"); continue; }
                decimal before = n.Value;
                decimal after = WheelOn(n, 120); after = WheelOn(n, -240);
                if (after != before) report.Add($"{name}: {before} -> {after}");
            }
            host.Close();
            if (bareAfter == 100m) return "sanity: an unguarded box did not react to the test wheel (the test cannot see the bug)";
            return report.Count == 0 ? "" : "wheel changed " + string.Join("; ", report);
        });
    }
}
