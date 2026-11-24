using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>The New / open Service Invoice form is a 2-step wizard (Invoice &amp; statement, Amounts &amp; save): one card at a time, Next validates step 1, Back / Reset / header navigate, a Paid invoice stays read-only.</summary>
public static partial class Checks
{
    private const BindingFlags IwF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static (BillingView view, Form host) IwMakeView()
    {
        var view = (BillingView)Activator.CreateInstance(typeof(BillingView), nonPublic: true)!;
        var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900), FormBorderStyle = FormBorderStyle.None };
        view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show(); Pump(800);
        return (view, host);
    }

    private static T IwGet<T>(Control root, string name) where T : Control =>
        (root.Controls.Find(name, true).OfType<T>().FirstOrDefault()) ?? throw new InvalidOperationException("control not found: " + name);

    private static bool IwInside(Control c, Control p) { for (var x = c; x != null; x = x.Parent) if (x == p) return true; return false; }

    [Check]
    public static string InvoiceWizard_NewInvoice_StepsValidateAndNavigate()
    {
        Fresh(); RunBackfill();
        int used = Seed.Invoice(Seed.Delivery(), 500m, PayStatus.Unpaid);
        string usedNo = Str("SELECT InvoiceNumber FROM Invoices WHERE Id=@i", ("@i", used));
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        int soaId = SaveSoaFor("CLIENT A", (a, 6000m, 300m));
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var (view, host) = IwMakeView();
            var T = typeof(BillingView);
            var bad = new List<string>();
            T.GetMethod("OpenNewRecordForm", IwF)!.Invoke(view, null); Pump(500);
            var panels = new[] { "pnlStep1InvoiceStatement", "pnlStep2AmountsSave" }.Select(n => IwGet<Panel>(view, n)).ToArray();
            var save = FieldOf<Button>(view, "btnSave")!; var update = FieldOf<Button>(view, "btnUpdate")!;
            var next = IwGet<Button>(view, "btnNext"); var back = IwGet<Button>(view, "btnBack"); var clear = FieldOf<Button>(view, "btnClear")!;
            var txtNo = FieldOf<TextBox>(view, "txtInvoiceNumber")!; var cbSoa = FieldOf<ComboBox>(view, "cbSoa")!; var cbClient = FieldOf<ComboBox>(view, "cbClient")!;
            var amount = FieldOf<NumericUpDown>(view, "numTotalAmount")!; var fuel = FieldOf<NumericUpDown>(view, "numFuel")!;
            var checklist = FieldOf<ValidationChecklist>(view, "billChecklist")!; var strip = FieldOf<Control>(view, "billStrip")!;
            var pnlForm = FieldOf<Panel>(view, "pnlForm")!;
            bool CanJump(int n) => (bool)T.GetMethod("CanJumpToStep", IwF)!.Invoke(view, new object[] { n })!;
            string State() => $"step {view.CurrentWizardStep}, visible [{string.Join(",", panels.Select(p => p.Visible ? "1" : "0"))}]";
            void Expect(int step, string what)
            {
                if (view.CurrentWizardStep != step) bad.Add($"{what}: on step {view.CurrentWizardStep}, expected {step}");
                for (int i = 0; i < panels.Length; i++) if (panels[i].Visible != (i + 1 == step)) { bad.Add($"{what}: panel visibility wrong ({State()})"); break; }
            }
            if (!pnlForm.Visible) bad.Add("the invoice form did not open");
            Expect(1, "new invoice start");
            if (!clear.Visible || back.Visible || !next.Visible || save.Visible || update.Visible) bad.Add($"step 1 buttons wrong (clear {clear.Visible}, back {back.Visible}, next {next.Visible}, save {save.Visible}, update {update.Visible})");
            if (!next.Text.Contains("Amounts & save")) bad.Add("step 1 Next text: " + next.Text);
            if (CanJump(2)) bad.Add("step 2 is clickable on a new invoice");
            if (cbSoa.Items.Count == 0) { host.Close(); return "no statement is waiting for an invoice (seed problem)"; }
            // the invoice no., the statement and the client are on step 1; the amounts and the totals card on step 2; the figures strip is on neither (it stays)
            if (!IwInside(txtNo, panels[0]) || !IwInside(cbSoa, panels[0]) || !IwInside(cbClient, panels[0])) bad.Add("the invoice no. / statement / client are not on step 1");
            if (!IwInside(FieldOf<Control>(view, "billTape")!, panels[0])) bad.Add("the statement-being-billed card is not on step 1");
            if (!IwInside(amount, panels[1]) || !IwInside(fuel, panels[1]) || !IwInside(FieldOf<Control>(view, "billNetTape")!, panels[1])) bad.Add("the amount / fuel surcharge / totals card are not on step 2");
            if (IwInside(strip, panels[0]) || IwInside(strip, panels[1])) bad.Add("the figures strip sits inside a step");
            if (!strip.Visible) bad.Add("the figures strip is hidden on step 1");
            if (checklist.Visible) bad.Add("the checklist shows on step 1");
            if (view.Controls.Find("pnlStepperHeader", true).Length == 0) bad.Add("no step header");

            // Next with no number: stays, with the number's message
            int before = boxes.Boxes.Count;
            if (view.TryNextStep()) bad.Add("Next passed with no invoice number");
            Pump(200);
            Expect(1, "Next with no number");
            if (!boxes.Boxes.Skip(before).Any(m => m.Contains("Service Invoice No"))) bad.Add("no message for a missing invoice number: " + string.Join(" | ", boxes.Boxes.Skip(before)));

            // a number already used by another invoice: stays
            txtNo.Text = usedNo; Pump(400);
            before = boxes.Boxes.Count;
            if (view.TryNextStep()) bad.Add("Next passed with a number already used");
            Pump(200);
            Expect(1, "Next with a used number");
            if (!boxes.Boxes.Skip(before).Any(m => m.Contains("Service Invoice No"))) bad.Add("no message for a used invoice number: " + string.Join(" | ", boxes.Boxes.Skip(before)));

            // a free number but no statement: stays with the save's own message
            txtNo.Text = "8811"; Pump(400);
            before = boxes.Boxes.Count;
            if (view.TryNextStep()) bad.Add("Next passed with no statement picked");
            Pump(200);
            Expect(1, "Next with no statement");
            if (!boxes.Boxes.Skip(before).Any(m => m.Contains("Pick a Statement of Account to invoice"))) bad.Add("no message for a missing statement: " + string.Join(" | ", boxes.Boxes.Skip(before)));

            // pick the statement: the client is filled and locked; Next goes to step 2
            cbSoa.SelectedIndex = 0; Pump(500);
            if (cbClient.Text != "CLIENT A") bad.Add("the client was not filled from the statement: " + cbClient.Text);
            if (cbClient.Enabled) bad.Add("the client is not locked to the statement");
            if (!view.TryNextStep()) bad.Add("Next refused with a number and a statement");
            Pump(400);
            Expect(2, "Next with number and statement");
            if (!save.Visible || update.Visible || next.Visible || !back.Visible || clear.Visible) bad.Add($"step 2 buttons wrong (save {save.Visible}, update {update.Visible}, next {next.Visible}, back {back.Visible}, clear {clear.Visible})");
            if (!save.Enabled) bad.Add("Issue is disabled on step 2 although every check is green: " + string.Join(" | ", checklist.Checks.Where(c => !c.Passed).Select(c => c.Text)));
            if (!checklist.Visible) bad.Add("the checklist is hidden on step 2");
            if (!strip.Visible) bad.Add("the figures strip is hidden on step 2");
            if (amount.Enabled || fuel.Enabled) bad.Add("the amounts are not locked to the statement");
            if (!CanJump(1) || CanJump(2)) bad.Add("header clickability wrong at step 2 of a new invoice");

            // Back
            IwGet<Button>(view, "btnBack").PerformClick(); Pump(200);
            Expect(1, "Back from step 2");
            if (save.Visible || !next.Visible) bad.Add("Issue visible / Next hidden on step 1 after Back");

            // anything focusing a control jumps to its step first
            T.GetMethod("ShowStepFor", IwF)!.Invoke(view, new object?[] { fuel }); Pump(100);
            Expect(2, "ShowStepFor(fuel surcharge)");
            T.GetMethod("ShowStepFor", IwF)!.Invoke(view, new object?[] { txtNo }); Pump(100);
            Expect(1, "ShowStepFor(invoice no.)");

            // Issue with a problem on step 1 (number removed) jumps back there and saves nothing
            view.GoToStep(2); Pump(100);
            txtNo.Text = ""; Pump(300);
            long invBefore = Long("SELECT COUNT(*) FROM Invoices");
            T.GetMethod("BtnSave_Click", IwF)!.Invoke(view, new object?[] { null, EventArgs.Empty }); Pump(300);
            Expect(1, "Issue with the number missing");
            if (Long("SELECT COUNT(*) FROM Invoices") != invBefore) bad.Add("Issue saved an invoice without a number");

            // Reset returns to step 1 and clears the pick
            txtNo.Text = "8812"; Pump(300);
            view.TryNextStep(); Pump(200);
            Expect(2, "second Next");
            T.GetMethod("BtnClear_Click", IwF)!.Invoke(view, new object?[] { null, EventArgs.Empty }); Pump(300);
            Expect(1, "Clear form");
            if (cbSoa.SelectedIndex >= 0 || txtNo.Text.Length > 0) bad.Add("Clear form did not clear the statement / number");
            if (!clear.Visible || back.Visible) bad.Add("Clear form did not restore the step 1 buttons");

            // Issue / Next: a whole statement still issues its invoice from step 2
            cbSoa.SelectedIndex = 0; Pump(300);
            txtNo.Text = "8813"; Pump(400);
            if (!view.TryNextStep()) bad.Add("Next refused for the final run");
            Pump(300);
            T.GetMethod("BtnSave_Click", IwF)!.Invoke(view, new object?[] { null, EventArgs.Empty }); Pump(600);
            if (Long("SELECT COUNT(*) FROM Invoices WHERE InvoiceNumber='8813'") != 1) bad.Add("Issue from step 2 did not save the statement's invoice");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string InvoiceWizard_OpenedFromStatement_LandsOnStep1_Filled()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        int soaId = SaveSoaFor("CLIENT A", (a, 6000m, 300m));
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var (view, host) = IwMakeView();
            var bad = new List<string>();
            view.StartForStatement(soaId); Pump(600);
            var panels = new[] { "pnlStep1InvoiceStatement", "pnlStep2AmountsSave" }.Select(n => IwGet<Panel>(view, n)).ToArray();
            if (!FieldOf<Panel>(view, "pnlForm")!.Visible) bad.Add("the invoice form did not open for the statement");
            if (view.CurrentWizardStep != 1 || !panels[0].Visible || panels[1].Visible) bad.Add("opened for a statement but not on step 1 (step " + view.CurrentWizardStep + ")");
            var cbSoa = FieldOf<ComboBox>(view, "cbSoa")!;
            if (cbSoa.SelectedIndex < 0) bad.Add("the statement was not picked");
            if (FieldOf<ComboBox>(view, "cbClient")!.Text != "CLIENT A") bad.Add("the client was not filled");
            if (FieldOf<NumericUpDown>(view, "numTotalAmount")!.Value <= 0) bad.Add("the amount was not filled from the statement");
            if (!IwGet<Button>(view, "btnNext").Visible || FieldOf<Button>(view, "btnSave")!.Visible) bad.Add("step 1 buttons wrong after opening for a statement");
            // only the invoice no. is left to type; then Next reaches step 2
            FieldOf<TextBox>(view, "txtInvoiceNumber")!.Text = "8821"; Pump(400);
            if (!view.TryNextStep()) bad.Add("Next refused on a filled statement with a number");
            Pump(300);
            if (view.CurrentWizardStep != 2) bad.Add("Next did not reach step 2");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string InvoiceWizard_SavedInvoice_HeaderJumpsFreely_PaidIsReadOnlyOnBothSteps()
    {
        Fresh(); RunBackfill();
        int paid = Seed.Invoice(Seed.Delivery(), 700m, PayStatus.Paid);
        int open = Seed.Invoice(Seed.Delivery(), 800m, PayStatus.Unpaid);
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var (view, host) = IwMakeView();
            var T = typeof(BillingView);
            var bad = new List<string>();
            var save = FieldOf<Button>(view, "btnSave")!; var update = FieldOf<Button>(view, "btnUpdate")!; var next = IwGet<Button>(view, "btnNext");
            bool CanJump(int n) => (bool)T.GetMethod("CanJumpToStep", IwF)!.Invoke(view, new object[] { n })!;
            T.GetMethod("SetFormVisibility", IwF)!.Invoke(view, new object[] { true }); Pump(300);
            // a saved, unpaid invoice
            var o = MJdatabase.GetInvoices().First(i => i.Id == open);
            T.GetMethod("BindInvoiceToForm", IwF)!.Invoke(view, new object[] { o }); Pump(400);
            view.GoToStep(1); Pump(100);
            if (!CanJump(2)) bad.Add("step 2 is not clickable on a saved invoice");
            if (save.Visible || update.Visible || !next.Visible) bad.Add($"step 1 of a saved invoice: save {save.Visible}, update {update.Visible}, next {next.Visible}");
            view.GoToStep(2); Pump(300);
            if (view.CurrentWizardStep != 2) bad.Add("header jump to step 2 failed");
            if (save.Visible || !update.Visible || next.Visible) bad.Add($"step 2 of a saved invoice: save {save.Visible}, update {update.Visible}, next {next.Visible}");
            if (!CanJump(1)) bad.Add("step 1 is not clickable from step 2 of a saved invoice");
            view.GoToStep(1); Pump(200);
            if (update.Visible) bad.Add("Update visible on step 1");
            if (!view.TryNextStep()) bad.Add("Next refused on a saved invoice with its own number");
            Pump(200);
            if (view.CurrentWizardStep != 2) bad.Add("Next on a saved invoice did not reach step 2");
            // Reset (Clear form) goes back to a new invoice on step 1
            T.GetMethod("BtnClear_Click", IwF)!.Invoke(view, new object?[] { null, EventArgs.Empty }); Pump(300);
            if (view.CurrentWizardStep != 1 || CanJump(2)) bad.Add("Clear form did not return to a new invoice on step 1");

            // a Paid invoice: read-only, no Issue / Update on either step
            var p = MJdatabase.GetInvoices().First(i => i.Id == paid);
            T.GetMethod("BindInvoiceToForm", IwF)!.Invoke(view, new object[] { p }); Pump(400);
            view.GoToStep(1); Pump(100);
            if (save.Visible || update.Visible) bad.Add($"Paid invoice step 1: save {save.Visible}, update {update.Visible}");
            if (!CanJump(2)) bad.Add("a Paid invoice's header is not clickable");
            if (!FieldOf<TextBox>(view, "txtInvoiceNumber")!.ReadOnly) bad.Add("the number of a Paid invoice is editable");
            if (!view.TryNextStep()) bad.Add("Next refused on a Paid invoice");
            Pump(300);
            if (view.CurrentWizardStep != 2) bad.Add("Paid invoice: Next did not reach step 2");
            if (save.Visible || update.Visible || next.Visible) bad.Add($"Paid invoice step 2: save {save.Visible}, update {update.Visible}, next {next.Visible}");
            if (FieldOf<NumericUpDown>(view, "numTotalAmount")!.Enabled || FieldOf<NumericUpDown>(view, "numFuel")!.Enabled) bad.Add("the amounts of a Paid invoice are editable");
            T.GetMethod("BtnUpdate_Click", IwF)!.Invoke(view, new object?[] { null, EventArgs.Empty }); Pump(300);
            if (Convert.ToDecimal(Scalar("SELECT NetAmount FROM Invoices WHERE Id=@i", ("@i", paid)) ?? 0) != 700m) bad.Add("Update changed a Paid invoice");
            host.Close();
            return string.Join("; ", bad);
        });
    }
}
