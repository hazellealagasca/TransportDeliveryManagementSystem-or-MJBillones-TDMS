using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 12 validation audit, daily screens: dispatch number, plant gate order, statement sign-off name and SI number, payment reference.</summary>
public static partial class Checks
{
    private const BindingFlags VdF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

    private static void VdShot(Control c, string name)
    {
        try
        {
            if (Environment.GetEnvironmentVariable("TDMS_SHOT_DIR") is not string dir || c.Width <= 0 || c.Height <= 0) return;
            Directory.CreateDirectory(dir);
            using var bmp = new Bitmap(c.Width, c.Height);
            c.DrawToBitmap(bmp, new Rectangle(0, 0, c.Width, c.Height));
            bmp.Save(Path.Combine(dir, name));
        }
        catch { }
    }

    /// <summary>The app's pop-ups are themed forms (not Windows boxes): a UI-thread timer records the text of any modal form and closes it (as Cancel / No).</summary>
    private sealed class VdBoxes : IDisposable
    {
        public readonly List<string> Boxes = new();
        private readonly System.Windows.Forms.Timer timer = new() { Interval = 150 };
        public VdBoxes([System.Runtime.CompilerServices.CallerMemberName] string tag = "")
        {
            timer.Tick += (s, e) =>
            {
                foreach (var f in Application.OpenForms.Cast<Form>().Where(f => f.Modal && !f.IsDisposed).ToList())
                {
                    var sb = new System.Text.StringBuilder(f.Text).Append("] ");
                    void Walk(Control c) { foreach (Control k in c.Controls) { if (k is Label or Button || k is TextBox) sb.Append(k.Text).Append(' '); Walk(k); } }
                    Walk(f);
                    Boxes.Add("[" + sb.ToString().Trim());
                    VdShot(f, "popup-" + tag.Replace("ValidationDaily_", "") + "-" + Boxes.Count + ".png");
                    f.DialogResult = DialogResult.Cancel; f.Close();
                }
            };
            timer.Start();
        }
        public void Dispose() { timer.Stop(); timer.Dispose(); }
    }

    [Check]
    public static string ValidationDaily_Rules_AcceptedAndRefusedValues()
    {
        var bad = new List<string>();
        foreach (var ok in new[] { "DSP-20260901-140140", "RR0001", "a/b-9", new string('A', 30) })
            if (!DailyRules.DispatchNumberOk(ok)) bad.Add("dispatch # refused: " + ok);
        foreach (var no in new[] { "", "   ", "RR 0001", "RR0001\r\nX", "RR_1", "RR#1", new string('A', 31), "RR.1" })
            if (DailyRules.DispatchNumberOk(no)) bad.Add("dispatch # accepted: [" + no + "]");
        foreach (var ok in new[] { "CHK-990214", "BDO-TXN-55410", "7001", new string('9', 30) })
            if (!DailyRules.PaymentReferenceOk(ok)) bad.Add("reference refused: " + ok);
        foreach (var no in new[] { "CHK 9902", "A/B", "A_B", "CHK#1", "", new string('9', 31) })
            if (DailyRules.PaymentReferenceOk(no)) bad.Add("reference accepted: [" + no + "]");
        foreach (var ok in new[] { "Maria Santos", "J. dela Cruz-Reyes", "O'Brien", "Niño Peña", new string('A', 100) })
            if (!DailyRules.SignOffNameOk(ok)) bad.Add("name refused: " + ok);
        foreach (var no in new[] { "", "Billing 2", "Ana123", "A@B", "-Ana", new string('A', 101) })
            if (DailyRules.SignOffNameOk(no)) bad.Add("name accepted: [" + no + "]");
        // gate order
        if (!DailyRules.GateOutBeforeIn("2042", "0130")) bad.Add("out 0130 before in 2042 not flagged");
        if (DailyRules.GateOutBeforeIn("0800", "1700")) bad.Add("a normal day trip flagged");
        if (DailyRules.GateOutBeforeIn("0800", "0800")) bad.Add("equal times flagged");
        if (DailyRules.GateOutBeforeIn("", "0130") || DailyRules.GateOutBeforeIn("2042", "") || DailyRules.GateOutBeforeIn("99", "0130")) bad.Add("blank/bad times flagged");
        if (DailyRules.GateOvernightQuestion("2042", "0130") != "Truck left the plant at 0130, before it came in at 2042. Is this an overnight trip?") bad.Add("question wording");
        if (DailyRules.Tidy("  Ana   Cruz \r\n") != "Ana Cruz") bad.Add("Tidy");
        return string.Join("; ", bad);
    }

    [Check]
    public static string ValidationDaily_Dispatch_NumberFormat_OnSave_AndExistingNumbersLoad()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var view = (DispatchManagementView)Activator.CreateInstance(typeof(DispatchManagementView), nonPublic: true)!;
            var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900) };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(800);
            var T = typeof(DispatchManagementView);
            var bad = new List<string>();
            var box = (TextBox)T.GetField("txtDispatchNumber", VdF)!.GetValue(view)!;
            if (box.MaxLength != 30) bad.Add("dispatch # box MaxLength is " + box.MaxLength);
            bool Allowed(string s) => (bool)T.GetMethod("DispatchNumberAllowed", VdF)!.Invoke(view, new object?[] { s })!;
            if (Allowed("RR 0001")) bad.Add("a number with a space is allowed");
            if (Allowed("RR_0001")) bad.Add("a number with _ is allowed");
            if (!Allowed("RR0001") || !Allowed("DSP-20260901-140140") || !Allowed("12/34-A")) bad.Add("a good number is refused");
            // an order already saved with an odd number still loads and is not refused
            var d = new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models.Delivery { DispatchNumber = "OLD 77 #x", ClientName = "CLIENT A", Destination = "Route 1" };
            T.GetField("selectedDelivery", VdF)!.SetValue(view, d);
            if (!Allowed("OLD 77 #x")) bad.Add("an existing saved number was refused");
            if (Allowed("OLD 78 #x")) bad.Add("a changed odd number was allowed");
            T.GetField("selectedDelivery", VdF)!.SetValue(view, null);
            // saving with a bad number shows the message in the summary
            box.Text = "RR 0001/x_y";
            int before = boxes.Boxes.Count;
            T.GetMethod("BtnSave_Click", VdF)!.Invoke(view, new object?[] { null, EventArgs.Empty });
            Pump(1200);
            VdShot(host, "dispatch-number-error.png");
            var msgs = boxes.Boxes.Skip(before).ToList();
            if (!msgs.Any(m => m.Contains("Dispatch # can only have letters, digits")))
                bad.Add("the summary did not carry the dispatch # message: " + string.Join(" | ", msgs));
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string ValidationDaily_TripReport_GateOutBeforeIn_AsksOvernight_NotABlock()
    {
        Fresh();
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(1500, 900), Opacity = 0 };
            var view = new TripReportView { Dock = DockStyle.Fill };
            host.Controls.Add(view); host.Show(); Pump(1500);
            var T = typeof(TripReportView);
            var bad = new List<string>();
            var tin = FieldOf<TextBox>(view, "txtTruckTimeIn")!; var tout = FieldOf<TextBox>(view, "txtTruckTimeOut")!;
            string? Q() => (string?)T.GetMethod("GateOrderQuestion", VdF)!.Invoke(view, null);
            tin.Text = "2042"; tout.Text = "0130";
            if (Q() != "Truck left the plant at 0130, before it came in at 2042. Is this an overnight trip?") bad.Add("question not raised: " + Q());
            // not part of the required-field list (it is a confirmation, not a block)
            var list = (System.Collections.IEnumerable)T.GetMethod("BlankFormChecks", VdF)!.Invoke(view, null)!;
            foreach (var item in list) if ((item.ToString() ?? "").Contains("overnight")) bad.Add("the overnight question is in the blocking list");
            int before = boxes.Boxes.Count;
            bool go = (bool)T.GetMethod("ConfirmGateOrder", VdF)!.Invoke(view, null)!;
            if (go) bad.Add("closing the question (No) still let the save go on");
            if (!boxes.Boxes.Skip(before).Any(m => m.Contains("overnight trip"))) bad.Add("no question box appeared");
            tout.Text = "2300";
            if (Q() != null) bad.Add("a normal order still asks");
            before = boxes.Boxes.Count;
            if (!(bool)T.GetMethod("ConfirmGateOrder", VdF)!.Invoke(view, null)!) bad.Add("a normal order was stopped");
            if (boxes.Boxes.Count > before) bad.Add("a normal order showed a box");
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string ValidationDaily_Soa_PreparedByName_AndSiNumberSameAsInvoiceScreen()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(d);
        int soa = SoaWithInvoice(d, "SI-9701", 1000m, out _);
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            using var view = new StatementOfAccountView();
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), Size = new Size(1500, 900), Opacity = 0 };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(800);
            var T = typeof(StatementOfAccountView);
            var bad = new List<string>();
            foreach (var n in new[] { "txtPreparedBy", "txtReceivedBy1", "txtReceivedBy2" })
                if (FieldOf<TextBox>(view, n)!.MaxLength != 100) bad.Add(n + " MaxLength not 100");
            bool Prep(string s) => (bool)T.GetMethod("SoaPreparedByAllowed", VdF)!.Invoke(view, new object?[] { s })!;
            if (!Prep("Maria Santos") || !Prep("J. dela Cruz-Reyes")) bad.Add("a good name refused");
            if (Prep("Billing 2") || Prep("Ana@x") || Prep(new string('A', 101))) bad.Add("a bad name accepted");
            string? Si(string n, int id) => (string?)T.GetMethod("SoaSiNumberProblem", VdF)!.Invoke(null, new object?[] { n, id });
            string? invoiceScreen(string n, int id) => MJdatabase.CheckSiNumber(n, id);
            foreach (var n in new[] { "SI-9701", "SI 12", "SI-@@", "SI-" + new string('9', 40), "SI-5555" })
                if ((Si(n, 0) == null) != (invoiceScreen(n, 0) == null)) bad.Add("statement and invoice screen disagree on " + n);
            if (Si("SI-9701", 0) == null) bad.Add("a used SI number was accepted for a new statement");
            if (Si("SI-5555", 0) != null) bad.Add("a fresh SI number was refused: " + Si("SI-5555", 0));
            if (Si("SI-9701", soa) != null) bad.Add("the statement's own SI number was refused: " + Si("SI-9701", soa));
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string ValidationDaily_Receipt_ReferenceFormat_OnSave()
    {
        Fresh();
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(1500, 900) };
            var view = new CollectionView { Dock = DockStyle.Fill };
            host.Controls.Add(view); host.Show(); Pump(1500);
            var T = typeof(CollectionView);
            var bad = new List<string>();
            var rf = FieldOf<TextBox>(view, "txtRefNo")!;
            if (rf.MaxLength != 30) bad.Add("reference MaxLength is " + rf.MaxLength);
            if (FieldOf<TextBox>(view, "txtNotes")!.MaxLength != 500) bad.Add("notes MaxLength is not 500");
            try { T.GetMethod("OpenNewRecordForm", VdF)!.Invoke(view, null); Pump(500); } catch { }
            // typing: the box does not take a space or "/" at all
            rf.Text = "CHK 99/02";
            if (rf.Text != "CHK9902") bad.Add("the typing guard did not strip the space and slash: " + rf.Text);
            // a value that got in some other way (paste of old data) is still caught on save: take the typing guard off for this test
            (typeof(Control).GetProperty("Events", VdF)!.GetValue(rf) as IDisposable)?.Dispose();
            rf.Text = "CHK 99/02";
            int before = boxes.Boxes.Count;
            T.GetMethod("CheckReceiptForm", VdF)!.Invoke(view, new object[] { false });
            Pump(1200);
            VdShot(host, "receipt-reference-error.png");
            if (!boxes.Boxes.Skip(before).Any(m => m.Contains("Payment reference can only have letters, digits")))
                bad.Add("the summary did not carry the reference message: " + string.Join(" | ", boxes.Boxes.Skip(before)));
            rf.Text = "CHK-990214";
            before = boxes.Boxes.Count;
            T.GetMethod("CheckReceiptForm", VdF)!.Invoke(view, new object[] { false });
            Pump(1200);
            if (boxes.Boxes.Skip(before).Any(m => m.Contains("Payment reference can only"))) bad.Add("a good reference was refused");
            host.Close();
            return string.Join("; ", bad);
        });
    }
}
