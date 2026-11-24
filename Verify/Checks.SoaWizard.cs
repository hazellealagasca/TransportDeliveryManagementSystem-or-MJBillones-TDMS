using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>The Statement of Account form is a 3-step wizard (Client &amp; trips, Trips &amp; totals, Sign-off &amp; save): one step at a time, Next validates with the save's own messages, Back / Reset / header navigate, a paid statement is read-only on every step.</summary>
public static partial class Checks
{
    private const BindingFlags SwF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static T SwGet<T>(Control root, string name) where T : Control =>
        (root.Controls.Find(name, true).OfType<T>().FirstOrDefault()) ?? throw new InvalidOperationException("control not found: " + name);

    private static bool SwInside(Control c, Control p) { for (var x = c; x != null; x = x.Parent) if (x == p) return true; return false; }

    [Check]
    public static string SoaWizard_NewStatement_StepsValidateAndNavigate()
    {
        Fresh(); RunBackfill(); LockClient();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var (view, host) = LockOpen();
            var T = typeof(StatementOfAccountView);
            var bad = new List<string>();
            T.GetMethod("OpenNewStatement", SwF)!.Invoke(view, null); Pump(500);
            var panels = new[] { "pnlStep1ClientInvoices", "pnlStep2TripsTotals", "pnlStep3SignSave" }.Select(n => SwGet<Panel>(view, n)).ToArray();
            var save = FieldOf<Button>(view, "btnSaveSoa")!; var clear = FieldOf<Button>(view, "btnClearForm")!;
            var next = SwGet<Button>(view, "btnNext"); var back = SwGet<Button>(view, "btnBack");
            var cbClient = FieldOf<ComboBox>(view, "cbClient")!; var cbType = FieldOf<ComboBox>(view, "cbDeliveryType")!;
            var grid = FieldOf<DataGridView>(view, "gridItems")!;
            var strip = FieldOf<Control>(view, "soaStrip")!;
            var form = FieldOf<Panel>(view, "pnlForm")!;
            bool CanJump(int n) => (bool)T.GetMethod("CanJumpToStep", SwF)!.Invoke(view, new object[] { n })!;
            string State() => $"step {view.CurrentWizardStep}, visible [{string.Join(",", panels.Select(p => p.Visible ? "1" : "0"))}]";
            void Expect(int step, string what)
            {
                if (view.CurrentWizardStep != step) bad.Add($"{what}: on step {view.CurrentWizardStep}, expected {step}");
                for (int i = 0; i < panels.Length; i++) if (panels[i].Visible != (i + 1 == step)) { bad.Add($"{what}: panel visibility wrong ({State()})"); break; }
                if (!strip.Visible || SwInside(strip, panels[0]) || SwInside(strip, panels[1]) || SwInside(strip, panels[2])) bad.Add($"{what}: the amount-due strip is not shown on its own");
            }
            bool Said(int from, string text) => boxes.Boxes.Skip(from).Any(m => m.Contains(text));
            if (!form.Visible) bad.Add("the statement form did not open");
            Expect(1, "new statement start");
            if (!clear.Visible || back.Visible || !next.Visible || save.Visible) bad.Add($"step 1 buttons wrong (clear {clear.Visible}, back {back.Visible}, next {next.Visible}, save {save.Visible})");
            if (!next.Text.Contains("Trips & totals")) bad.Add("step 1 Next text: " + next.Text);
            if (CanJump(2) || CanJump(3)) bad.Add("a later step is clickable on a new statement");
            // where the fields live
            if (!SwInside(cbClient, panels[0]) || !SwInside(cbType, panels[0]) || !SwInside(FieldOf<TextBox>(view, "txtClientTin")!, panels[0]) || !SwInside(FieldOf<DataGridView>(view, "gridPicker")!, panels[0])) bad.Add("client / delivery type / TIN / service-invoice picker are not on step 1");
            if (!SwInside(grid, panels[1]) || !SwInside(FieldOf<Control>(view, "soaTape")!, panels[1]) || !SwInside(FieldOf<Control>(view, "sheetTotals")!, panels[1])) bad.Add("the trip table and totals are not on step 2");
            if (!SwInside(FieldOf<TextBox>(view, "txtPreparedBy")!, panels[2]) || !SwInside(FieldOf<TextBox>(view, "txtReceivedBy1")!, panels[2]) || !SwInside(FieldOf<Control>(view, "soaChecklist")!, panels[2])) bad.Add("the sign-off and the problem list are not on step 3");

            // Next with no client: stays, with the save's message
            int before = boxes.Boxes.Count;
            if (view.TryNextStep()) bad.Add("Next passed with no client");
            Pump(200);
            Expect(1, "Next with no client");
            if (!Said(before, "select or enter the client company")) bad.Add("no client message: " + string.Join(" | ", boxes.Boxes.Skip(before)));

            // client picked, no delivery type
            T.GetMethod("SetClientQuietly", SwF)!.Invoke(view, new object[] { "CLIENT A" }); Pump(400);
            cbType.SelectedIndex = -1;
            before = boxes.Boxes.Count;
            if (view.TryNextStep()) bad.Add("Next passed with no delivery type");
            Pump(200);
            Expect(1, "Next with no delivery type");
            if (!Said(before, "choose the delivery type")) bad.Add("no delivery type message: " + string.Join(" | ", boxes.Boxes.Skip(before)));

            // delivery type chosen, no trips
            cbType.SelectedIndex = 0; Pump(200);
            grid.Rows.Clear();
            before = boxes.Boxes.Count;
            if (view.TryNextStep()) bad.Add("Next passed with no trips");
            Pump(200);
            Expect(1, "Next with no trips");
            if (!Said(before, "add at least one trip")) bad.Add("no trips message: " + string.Join(" | ", boxes.Boxes.Skip(before)));

            // trips added: Next goes to step 2
            T.GetMethod("AutoFillClientTrips", SwF)!.Invoke(view, null); Pump(400);
            if (!grid.Rows.Cast<DataGridViewRow>().Any(r => !r.IsNewRow)) bad.Add("the client's trips were not added (seed problem)");
            before = boxes.Boxes.Count;
            if (!view.TryNextStep()) bad.Add("Next refused with client, delivery type and trips: " + string.Join(" | ", boxes.Boxes.Skip(before)));
            Pump(300);
            Expect(2, "Next from step 1");
            if (!back.Visible || !next.Visible || save.Visible || clear.Visible) bad.Add($"step 2 buttons wrong (back {back.Visible}, next {next.Visible}, save {save.Visible}, clear {clear.Visible})");
            if (!next.Text.Contains("Sign-off & save")) bad.Add("step 2 Next text: " + next.Text);
            if (!CanJump(1) || CanJump(3)) bad.Add("header clickability wrong at step 2 of a new statement");

            // step 2 with no amounts: stays, with the save's message
            before = boxes.Boxes.Count;
            if (view.TryNextStep()) bad.Add("Next passed with PHP 0.00 due");
            Pump(200);
            Expect(2, "Next with no amounts");
            if (!Said(before, "PHP 0.00")) bad.Add("no amount message: " + string.Join(" | ", boxes.Boxes.Skip(before)));

            // Back, then anything that focuses a control jumps to its step
            back.PerformClick(); Pump(200);
            Expect(1, "Back from step 2");
            view.GoToStep(3); Pump(100);   // (code may jump; the header may not)
            Expect(3, "GoToStep(3)");
            if (!save.Visible || next.Visible || !back.Visible || clear.Visible) bad.Add($"step 3 buttons wrong (save {save.Visible}, next {next.Visible}, back {back.Visible}, clear {clear.Visible})");
            T.GetMethod("ShowStepFor", SwF)!.Invoke(view, new object?[] { cbClient }); Pump(100);
            Expect(1, "ShowStepFor(client)");
            T.GetMethod("ShowStepFor", SwF)!.Invoke(view, new object?[] { grid }); Pump(100);
            Expect(2, "ShowStepFor(trip table)");

            // Reset returns to step 1 and clears the form
            T.GetMethod("ClearForm", SwF)!.Invoke(view, null); Pump(300);
            Expect(1, "Reset form");
            if (cbClient.Text.Trim().Length != 0 || grid.Rows.Cast<DataGridViewRow>().Any(r => !r.IsNewRow)) bad.Add("Reset did not clear the client and trips");
            if (!clear.Visible || back.Visible) bad.Add("Reset did not restore the step 1 buttons");
            // the trips list on step 1 is read-only: no tick column, no SI No. column, no Tick all / Add ticked buttons
            var picker = FieldOf<DataGridView>(view, "gridPicker")!;
            if (picker.Columns["pkPick"]!.Visible || picker.Columns["pkSi"]!.Visible) bad.Add("the trips list still shows the tick or SI No. column");
            if (SwInside(FieldOf<Button>(view, "btnAddSelected")!, panels[0]) || SwInside(FieldOf<Button>(view, "btnPickAllReady")!, panels[0])) bad.Add("Tick all / Add ticked buttons are still on step 1");

            // opened with trips already added (Create statement): still starts on step 1
            var rows = MJdatabase.GetTripReportsWaitingForStatement().Where(c => c.ClientName == "CLIENT A")
                .Select(c => new ReadyInvoiceRow { InvoiceId = c.DeliveryId, InvoiceNumber = c.TripReportNumber, TripNumber = c.DispatchNumber, ClientName = c.ClientName, TripDate = DateTime.Today }).ToList();
            view.StartSoaFor("CLIENT A", rows); Pump(700);
            Expect(1, "StartSoaFor (Create statement)");
            if (!grid.Rows.Cast<DataGridViewRow>().Any(r => !r.IsNewRow)) bad.Add("StartSoaFor did not put the trips on the statement");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string SoaWizard_SavedStatement_HeaderJumpsFreely_PaidIsReadOnlyOnEveryStep()
    {
        Fresh(); RunBackfill(); LockClient();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        int openSoa = SaveSoaFor("CLIENT A", (a, 6000m, 300m));
        int d = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(d);
        int paidSoa = SoaWithInvoice(d, "SI-9611", 1000m, out int invId);
        Exec("UPDATE StatementsOfAccount SET Status='Paid' WHERE Id=@i", ("@i", paidSoa));
        Exec("UPDATE Invoices SET Status='Paid' WHERE Id=@i", ("@i", invId));
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var (view, host) = LockOpen();
            var T = typeof(StatementOfAccountView);
            var bad = new List<string>();
            var save = FieldOf<Button>(view, "btnSaveSoa")!; var clear = FieldOf<Button>(view, "btnClearForm")!;
            var next = SwGet<Button>(view, "btnNext"); var back = SwGet<Button>(view, "btnBack");
            bool CanJump(int n) => (bool)T.GetMethod("CanJumpToStep", SwF)!.Invoke(view, new object[] { n })!;
            void Open(int id) { T.GetMethod("OpenEditStatement", SwF)!.Invoke(view, new object[] { MJdatabase.GetStatementOfAccountById(id)! }); Pump(700); }

            // a saved, editable statement: every step is clickable, Save only on the last one
            Open(openSoa);
            if (view.CurrentWizardStep != 1) bad.Add("an opened statement starts on step " + view.CurrentWizardStep);
            if (!CanJump(2) || !CanJump(3)) bad.Add("steps 2 / 3 are not clickable on a saved statement");
            if (save.Visible || clear.Visible || !next.Visible || back.Visible) bad.Add($"step 1 of a saved statement: save {save.Visible}, reset {clear.Visible}, next {next.Visible}, back {back.Visible}");
            view.GoToStep(3); Pump(200);
            if (!save.Visible || next.Visible || clear.Visible) bad.Add($"step 3 of a saved statement: save {save.Visible}, next {next.Visible}, reset {clear.Visible}");
            if (!CanJump(1) || !CanJump(2)) bad.Add("steps 1 / 2 are not clickable from step 3");
            view.GoToStep(2); Pump(200);
            if (save.Visible) bad.Add("Save is shown on step 2");
            T.GetMethod("SetFormVisibility", SwF)!.Invoke(view, new object[] { false }); Pump(300);

            // a paid statement: read-only, no Save on any step, Next never blocks
            int before = boxes.Boxes.Count;
            Open(paidSoa);
            if (!(bool)T.GetField("soaViewOnly", SwF)!.GetValue(view)!) bad.Add("a paid statement did not open view-only");
            if (view.CurrentWizardStep != 2) bad.Add("a paid statement opens on step " + view.CurrentWizardStep + ", expected 2 (trips and totals)");
            for (int s = 1; s <= 3; s++)
            {
                view.GoToStep(s); Pump(200);
                if (save.Visible) bad.Add($"Save is shown on step {s} of a paid statement");
                if (clear.Visible) bad.Add($"Reset is shown on step {s} of a paid statement");
            }
            view.GoToStep(1); Pump(100);
            if (!view.TryNextStep() || view.CurrentWizardStep != 2) bad.Add("Next from step 1 of a paid statement did not just move on");
            if (!FieldOf<DataGridView>(view, "gridItems")!.ReadOnly) bad.Add("the trip lines of a paid statement can be edited");
            if (boxes.Boxes.Count > before) bad.Add("a message box appeared on a paid statement: " + string.Join(" | ", boxes.Boxes.Skip(before)));

            // back to an editable one: Save returns on the last step
            T.GetMethod("SetFormVisibility", SwF)!.Invoke(view, new object[] { false }); Pump(300);
            Open(openSoa);
            view.GoToStep(3); Pump(200);
            if (!save.Visible) bad.Add("Save did not come back on step 3 of an editable statement after a paid one");
            host.Close();
            return string.Join("; ", bad);
        });
    }
}
