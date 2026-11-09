using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>
/// Opens every screen (UserControl) and every window (Form) that can be created without arguments, plus the main
/// hubs, on a copy of the real data. A screen fails when it throws while opening or loading, or when it pops up a
/// message box on its own (the box text is recorded and the box is closed).
/// </summary>
public static partial class Checks
{
    private static readonly HashSet<string> SkipScreens = new(StringComparer.Ordinal)
    {
        "MainForm",            // opened separately below (needs the whole shell)
        "LoginForm", "SplashForm", "FirstRunSetupForm", "SetupWizardForm", // start-up windows that wait for the person
    };

    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint threadId, EnumWindowsProc cb, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr hwnd, EnumWindowsProc cb, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hWnd, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder s, int n);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    /// <summary>Closes any message box on the UI thread and returns its text.</summary>
    private sealed class BoxCloser : IDisposable
    {
        private readonly uint uiThread;
        private readonly System.Threading.Timer timer;
        public readonly List<string> Boxes = new();
        public BoxCloser(uint uiThread)
        {
            this.uiThread = uiThread;
            timer = new System.Threading.Timer(_ => Sweep(), null, 400, 400);
        }
        private void Sweep()
        {
            EnumThreadWindows(uiThread, (h, _) =>
            {
                var cls = new StringBuilder(64); GetClassName(h, cls, 64);
                if (cls.ToString() != "#32770" || !IsWindowVisible(h)) return true;
                var text = new StringBuilder();
                var title = new StringBuilder(256); GetWindowText(h, title, 256);
                EnumChildWindows(h, (c, __) =>
                {
                    var cc = new StringBuilder(64); GetClassName(c, cc, 64);
                    if (cc.ToString() == "Static") { var t = new StringBuilder(1024); GetWindowText(c, t, 1024); if (t.Length > 0) text.Append(t).Append(' '); }
                    return true;
                }, IntPtr.Zero);
                lock (Boxes) Boxes.Add($"[{title}] {text.ToString().Trim()}");
                PostMessage(h, 0x0010 /* WM_CLOSE */, IntPtr.Zero, IntPtr.Zero);
                return true;
            }, IntPtr.Zero);
        }
        public void Dispose() => timer.Dispose();
    }

    private static bool crossThreadHooked;
    private static string RunOnSta(Func<string> body)
    {
        Control.CheckForIllegalCrossThreadCalls = true;   // report any control touched from a background thread
        if (!crossThreadHooked) AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
        {
            if (e.Exception is InvalidOperationException ioe && ioe.Message.Contains("Cross-thread") && Environment.GetEnvironmentVariable("TDMS_VERIFY_STACKS") == "1")
                Console.WriteLine("CROSSTHREAD " + Environment.StackTrace.Split('\n').Where(l => l.Contains("Transport_Delivery")).Take(6).Aggregate("", (x, y) => x + " | " + y.Trim()));
        };
        crossThreadHooked = true;
        string result = "";
        Exception? err = null;
        // Run inside Application.Run like the real app (Program.Main), so async loads resume on the screen's thread exactly as they do for users.
        var t = new Thread(() =>
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            var appCtx = new ApplicationContext();
            var kick = new System.Windows.Forms.Timer { Interval = 10 };
            kick.Tick += (s, e) =>
            {
                kick.Stop();
                try { result = body(); } catch (Exception ex) { err = ex; }
                finally { appCtx.ExitThread(); }
            };
            kick.Start();
            Application.Run(appCtx);
            kick.Dispose();
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        if (!t.Join(TimeSpan.FromMinutes(8))) return "timed out (a window is waiting for input): " + string.Join(" / ", Application.OpenForms.Cast<Form>().Select(f => $"'{f.Text}' modal={f.Modal} {f.GetType().Name}"));
        if (err != null) throw err;
        return result;
    }

    private static void Pump(int ms)
    {
        var until = DateTime.Now.AddMilliseconds(ms);
        while (DateTime.Now < until) { Application.DoEvents(); Thread.Sleep(15); }
    }

    /// <summary>Shows one control in a hidden host window, lets it load, then closes it. Returns "" or what went wrong.</summary>
    private static string OpenOne(Func<Control> make, BoxCloser boxes)
    {
        int before; lock (boxes.Boxes) before = boxes.Boxes.Count;
        Exception? thrown = null;
        ThreadExceptionEventHandler h = (s, e) => thrown ??= e.Exception;
        Application.ThreadException += h;
        Form? host = null;
        try
        {
            var c = make();
            if (c is Form f) { host = f; f.ShowInTaskbar = false; f.StartPosition = FormStartPosition.Manual; f.Location = new Point(-4000, -4000); f.Opacity = 0; f.Show(); }
            else
            {
                host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), Size = new Size(1400, 900), Opacity = 0 };
                c.Dock = DockStyle.Fill; host.Controls.Add(c); host.Show();
            }
            Pump(2500);
            host.Size = new Size(900, 600); Pump(200);   // a resize runs the layout code once more
            host.Close(); Pump(150);
        }
        catch (Exception ex) { thrown ??= ex; }
        finally
        {
            Application.ThreadException -= h;
            try { host?.Dispose(); } catch (Exception ex) { thrown ??= ex; }
        }
        var problems = new List<string>();
        if (thrown != null)
        {
            var ex = thrown is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : thrown;
            var frames = (ex.StackTrace ?? "").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            string at = frames.FirstOrDefault(l => l.Contains("Transport_Delivery")) ?? string.Join(" < ", frames.Take(2));
            if (Environment.GetEnvironmentVariable("TDMS_VERIFY_STACKS") == "1") Console.WriteLine("STACK " + ex);
            at = at.Replace("Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.", "");
            int i = at.IndexOf(" in ", StringComparison.Ordinal);
            if (i > 0) { int j = at.LastIndexOf('\\'); at = at.Substring(0, i) + (j > 0 ? " @ " + at.Substring(j + 1) : ""); }
            problems.Add($"{ex.GetType().Name}: {ex.Message} {at}".Trim());
        }
        lock (boxes.Boxes) problems.AddRange(boxes.Boxes.Skip(before).Select(b => "message box " + b));
        return string.Join(" | ", problems);
    }

    private static IEnumerable<Type> ScreenTypes() =>
        typeof(MJdatabase).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
            .Where(t => typeof(UserControl).IsAssignableFrom(t) || typeof(Form).IsAssignableFrom(t))
            .Where(t => t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes) != null && !SkipScreens.Contains(t.Name))
            .OrderBy(t => t.FullName);

    private static void SignInAsAdmin() =>
        SessionContext.ActiveUser = new User { Id = 1, Username = "verify", Role = "Admin", FullName = "Verify", IsActive = true };

    /// <summary>Every screen and window opens on the real data without an error or an unexpected message box.</summary>
    [Check]
    public static string Screens_AllOpenWithoutErrors()
    {
        if (FromCopy() == null) return "no real database given";
        MJdatabase.InitializeDatabase();
        MJdatabase.RunOneTimeMigrations();   // what the login screen runs
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var bad = new List<string>();
            int opened = 0;
            foreach (var t in ScreenTypes())
            {
                string r = OpenOne(() => (Control)Activator.CreateInstance(t, nonPublic: true)!, boxes);
                opened++;
                if (r.Length > 0) bad.Add($"{t.Name}: {r}");
            }
            Console.WriteLine($"     ({opened} screens and windows opened)");
            return string.Join("\n     ", bad);
        });
    }

    /// <summary>Opens a window that a screen shows with ShowFor(parent, ...) and returns it.</summary>
    private static Form ShowForAndGet(Action<Control> showFor)
    {
        var parentHost = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000) };
        var parent = new Panel { Dock = DockStyle.Fill };
        parentHost.Controls.Add(parent); parentHost.Show();
        var before = Application.OpenForms.Cast<Form>().ToHashSet();
        showFor(parent);
        var opened = Application.OpenForms.Cast<Form>().FirstOrDefault(f => !before.Contains(f)) ?? throw new InvalidOperationException("no window opened");
        bool closing = false;
        opened.FormClosed += (s, e) => { if (closing) return; closing = true; parentHost.Close(); };
        return opened;
    }

    /// <summary>The record windows (dispatch details, special trip, crew and truck profiles, crew pay) open for real records.</summary>
    [Check]
    public static string Screens_RecordWindowsOpen()
    {
        if (FromCopy() == null) return "no real database given";
        MJdatabase.InitializeDatabase();
        MJdatabase.RunOneTimeMigrations();   // what the login screen runs
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var bad = new List<string>();
            int opened = 0;
            void Try(string what, Func<Control> make) { opened++; string r = OpenOne(make, boxes); if (r.Length > 0) bad.Add($"{what}: {r}"); }
            foreach (var d in MJdatabase.GetDeliveries().OrderByDescending(x => x.Id).Take(12))
            {
                Try($"Dispatch details {d.DispatchNumber}", () => ShowForAndGet(p => Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms.DispatchDetailsDialog.ShowFor(p, d)));
                Try($"Crew pay {d.DispatchNumber}", () => new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms.TripCrewPayDialog(d));
                Try($"Reschedule {d.DispatchNumber}", () => new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms.RescheduleDeliveryDialog(d));
            }
            foreach (var t in MJdatabase.GetSpecialTrips().OrderByDescending(x => x.Id).Take(12))
            {
                Try($"Special trip form {t.Id}", () => new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms.SpecialTripFormDialog(t.Id));
                Try($"Special trip details {t.Id}", () => ShowForAndGet(p => Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms.SpecialTripDetailsDialog.ShowFor(p, t.Id, _ => { }, () => { })));
            }
            foreach (var c in MJdatabase.GetCrews())
                Try($"Crew profile {c.FullName}", () => new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms.CrewProfileDialog(c));
            foreach (var t in MJdatabase.GetTrucks())
                Try($"Truck profile {t.PlateNumber}", () => new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms.TruckProfileDialog(t));
            Console.WriteLine($"     ({opened} record windows opened)");
            return string.Join("\n     ", bad);
        });
    }

    /// <summary>The main window opens and every tab of Operations, Resources, Payments and Master Data opens inside it.</summary>
    [Check]
    public static string Screens_MainWindowEveryTab()
    {
        if (FromCopy() == null) return "no real database given";
        MJdatabase.InitializeDatabase();
        MJdatabase.RunOneTimeMigrations();   // what the login screen runs
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var bad = new List<string>();
            Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.MainForm? main = null;
            string r = OpenOne(() => { main = new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.MainForm(SessionContext.ActiveUser!); main.WindowState = FormWindowState.Normal; main.StartPosition = FormStartPosition.Manual; main.Location = new Point(-4000, -4000); main.ShowInTaskbar = false; main.Opacity = 0; main.Show(); Pump(1500); return new Panel(); }, boxes);
            if (r.Length > 0) bad.Add("Main window: " + r);
            if (main == null) return string.Join("\n     ", bad);
            var tabs = new (string Area, string Tab)[]
            {
                ("Operations","Dispatch"),("Operations","SpecialTrips"),("Operations","Incidents"),("Operations","TripReports"),("Operations","History"),
                ("Resources","Trucks"),("Resources","Crew"),("Resources","CrewHistory"),
                ("Finance","Billing"),("Finance","StatementOfAccount"),("Finance","Collections"),("Finance","History"),
                ("MasterData","Clients"),("MasterData","Routes"),("MasterData","Rates"),("MasterData","Archive"),("MasterData","History"),
            };
            foreach (var (area, tab) in tabs)
            {
                var m = main;
                string x = OpenOne(() =>
                {
                    switch (area)
                    {
                        case "Operations": m.NavigateToOperations(tab); break;
                        case "Resources": m.NavigateToResources(tab); break;
                        case "Finance": m.NavigateToFinance(tab); break;
                        default: m.NavigateToMasterData(tab); break;
                    }
                    Pump(1200);
                    return new Panel();
                }, boxes);
                if (x.Length > 0) bad.Add($"{area} › {tab}: {x}");
            }
            try { main.Hide(); main.Dispose(); } catch (Exception ex) { bad.Add("closing the main window: " + ex.Message); }
            return string.Join("\n     ", bad);
        });
    }

    /// <summary>Diagnostic (prints, always passes): the state of the Delivery Routes table when it is opened inside the main window.</summary>
    [Check]
    public static string Diag_RoutesGridInMainWindow()
    {
        if (Environment.GetEnvironmentVariable("TDMS_VERIFY_DIAG") != "1" || FromCopy() == null) return "";
        MJdatabase.InitializeDatabase();
        MJdatabase.RunOneTimeMigrations();
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var main = new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.MainForm(SessionContext.ActiveUser!);
            main.WindowState = FormWindowState.Normal; main.StartPosition = FormStartPosition.Manual; main.Location = new Point(-4000, -4000); main.ShowInTaskbar = false; main.Opacity = 0; main.Show(); Pump(1500);
            DataGridView? grid = null;
            string Describe(string when)
            {
                if (grid == null) return when + ": grid not found";
                string F(Font? f) { if (f == null) return "null"; try { return f.Name + " " + f.Size + (f.FontFamily == null ? " (no family)" : ""); } catch (Exception ex) { return "BROKEN " + ex.GetType().Name; } }
                var cols = string.Join("; ", grid.Columns.Cast<DataGridViewColumn>().Select(c => $"{c.Name}:{c.GetType().Name.Replace("DataGridView", "")} font={F(c.DefaultCellStyle.Font)} vis={c.Visible}"));
                return $"{when}: rows={grid.Rows.Count} visible={grid.Visible} handle={grid.IsHandleCreated} focused={grid.Focused} font={F(grid.Font)} default={F(grid.DefaultCellStyle.Font)} rowsDefault={F(grid.RowsDefaultCellStyle.Font)} alt={F(grid.AlternatingRowsDefaultCellStyle.Font)} " +
                       $"current={(grid.CurrentCell == null ? "none" : grid.CurrentCell.ColumnIndex + "," + grid.CurrentCell.RowIndex)} editMode={grid.IsCurrentCellInEditMode} editor={(grid.EditingControl == null ? "none" : grid.EditingControl.GetType().Name)} cols=[{cols}]";
            }
            bool logged = false;
            var hooked = new HashSet<DataGridView>();
            void HookAll()
            {
                var q = new Queue<Control>(new Control[] { main });
                while (q.Count > 0)
                {
                    var c = q.Dequeue();
                    if (c is DataGridView g && hooked.Add(g)) g.CellPainting += (s, e) => grid = g;
                    foreach (Control ch in c.Controls) q.Enqueue(ch);
                }
            }
            string Owner(Control? c) { var names = new List<string>(); while (c != null) { names.Add(c.GetType().Name + (c.Name.Length > 0 ? "#" + c.Name : "")); c = c.Parent; } return string.Join(" < ", names.Take(4)); }
            Application.ThreadException += (s, e) => { if (logged) return; logged = true; Console.WriteLine("DIAG at failure in " + Owner(grid) + ": " + Describe("failure") + " | " + e.Exception.GetType().Name); Console.WriteLine("DIAG fonts: " + FontHealth()); };
            var tabs = new (string Area, string Tab)[]
            {
                ("Operations","Dispatch"),("Operations","SpecialTrips"),("Operations","Incidents"),("Operations","TripReports"),("Operations","History"),
                ("Resources","Trucks"),("Resources","Crew"),("Resources","CrewHistory"),
                ("Finance","Billing"),("Finance","StatementOfAccount"),("Finance","Collections"),("Finance","History"),
                ("MasterData","Clients"),("MasterData","Routes"),("MasterData","Rates"),
            };
            foreach (var (area, tab) in tabs)
            {
                switch (area)
                {
                    case "Operations": main.NavigateToOperations(tab); break;
                    case "Resources": main.NavigateToResources(tab); break;
                    case "Finance": main.NavigateToFinance(tab); break;
                    default: main.NavigateToMasterData(tab); break;
                }
                for (int k = 0; k < 24; k++) { Pump(50); HookAll(); }
                Console.WriteLine($"DIAG step {area}/{tab} done{(logged ? " (failure seen)" : "")}");
                if (logged) break;
            }
            Console.WriteLine("DIAG before: " + Describe("before"));
            Pump(2500);
            Console.WriteLine("DIAG after: " + Describe("after"));
            try { main.Hide(); main.Dispose(); } catch { }
            return "";
        });
    }

    /// <summary>Which shared fonts (FontCache, UITheme statics) are disposed (diagnostic).</summary>
    private static string FontHealth()
    {
        var asm = typeof(MJdatabase).Assembly;
        var parts = new List<string>();
        string T(Font f) { try { f.GetHeight(); return "ok"; } catch (Exception ex) { return "DISPOSED(" + ex.GetType().Name + ")"; } }
        var fc = asm.GetTypes().FirstOrDefault(t => t.Name == "FontCache");
        if (fc?.GetField("fonts", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) is System.Collections.IDictionary d)
            foreach (System.Collections.DictionaryEntry kv in d) if (kv.Value is Font f) parts.Add($"cache {kv.Key}={T(f)}");
        foreach (var t in asm.GetTypes())
            foreach (var fld in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(x => x.FieldType == typeof(Font)))
            {
                try { if (fld.GetValue(null) is Font f && T(f) != "ok") parts.Add($"{t.Name}.{fld.Name}={T(f)}"); } catch { }
            }
        return string.Join("; ", parts.Where(x => !x.EndsWith("=ok")).DefaultIfEmpty("all shared fonts ok (" + parts.Count + " checked)"));
    }
}
