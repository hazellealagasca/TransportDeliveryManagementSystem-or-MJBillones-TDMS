using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>The New / open Collection Receipt form is a 2-step wizard (What is being paid, Payment &amp; save): one card at a time, Next validates step 1, Back / Reset / header navigate.</summary>
public static partial class Checks
{
    private const BindingFlags CwF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static (CollectionView view, Form host) CwMakeView()
    {
        var view = new CollectionView { Dock = DockStyle.Fill };
        var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(1500, 900) };
        host.Controls.Add(view); host.Show(); Pump(800);
        return (view, host);
    }

    private static T CwGet<T>(Control root, string name) where T : Control =>
        (root.Controls.Find(name, true).OfType<T>().FirstOrDefault()) ?? throw new InvalidOperationException("control not found: " + name);

    [Check]
    public static string CollectionWizard_NewReceipt_StepsValidateAndNavigate()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        int soaId = SaveSoaFor("CLIENT A", (a, 6000m, 300m));
        MJdatabase.SaveInvoiceForSoa(soaId, "SI-9411");
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var (view, host) = CwMakeView();
            var T = typeof(CollectionView);
            var bad = new List<string>();
            T.GetMethod("LoadData", CwF)!.Invoke(view, null);
            T.GetMethod("OpenNewRecordForm", CwF)!.Invoke(view, null); Pump(400);
            var panels = new[] { "pnlStep1WhatIsPaid", "pnlStep2PaymentSave" }.Select(n => CwGet<Panel>(view, n)).ToArray();
            var save = FieldOf<Button>(view, "btnSave")!; var update = FieldOf<Button>(view, "btnUpdate")!;
            var next = CwGet<Button>(view, "btnNext"); var back = CwGet<Button>(view, "btnBack"); var clear = FieldOf<Button>(view, "btnClear")!;
            var pick = FieldOf<ComboBox>(view, "cbInvoice")!;
            var pnlForm = FieldOf<Panel>(view, "pnlForm")!;
            bool CanJump(int n) => (bool)T.GetMethod("CanJumpToStep", CwF)!.Invoke(view, new object[] { n })!;
            string State() => $"step {view.CurrentWizardStep}, visible [{string.Join(",", panels.Select(p => p.Visible ? "1" : "0"))}]";
            void Expect(int step, string what)
            {
                if (view.CurrentWizardStep != step) bad.Add($"{what}: on step {view.CurrentWizardStep}, expected {step}");
                for (int i = 0; i < panels.Length; i++) if (panels[i].Visible != (i + 1 == step)) { bad.Add($"{what}: panel visibility wrong ({State()})"); break; }
            }
            if (!pnlForm.Visible) bad.Add("the receipt form did not open");
            Expect(1, "new receipt start");
            if (!clear.Visible || back.Visible || !next.Visible || save.Visible || update.Visible) bad.Add($"step 1 buttons wrong (clear {clear.Visible}, back {back.Visible}, next {next.Visible}, save {save.Visible}, update {update.Visible})");
            if (!next.Text.Contains("Payment & save")) bad.Add("step 1 Next text: " + next.Text);
            if (CanJump(2)) bad.Add("step 2 is clickable on a new receipt");
            if (pick.Items.Count == 0) bad.Add("no statement or invoice is waiting for payment (seed problem)");
            // the statement / invoice picker is on step 1, the payment fields on step 2
            bool Inside(Control c, Control p) { for (var x = c; x != null; x = x.Parent) if (x == p) return true; return false; }
            if (!Inside(pick, panels[0])) bad.Add("the statement / invoice picker is not on step 1");
            if (!Inside(FieldOf<TextBox>(view, "txtReceiptNo")!, panels[1]) || !Inside(FieldOf<ComboBox>(view, "cbPaymentMethod")!, panels[1]) || !Inside(FieldOf<NumericUpDown>(view, "numAmountPaid")!, panels[1]))
                bad.Add("the payment fields are not on step 2");

            // Next with nothing picked: stays, with the save's own message
            int before = boxes.Boxes.Count;
            if (view.TryNextStep()) bad.Add("Next passed with nothing picked");
            Pump(200);
            Expect(1, "Next with nothing picked");
            if (!boxes.Boxes.Skip(before).Any(m => m.Contains("Pick the statement or invoice being paid"))) bad.Add("no message for a missing statement / invoice: " + string.Join(" | ", boxes.Boxes.Skip(before)));

            // pick one but don't tick "I checked the overall amount": Next stays on step 1 and says so
            pick.SelectedIndex = 0; Pump(400);
            var verify = FieldOf<CheckBox>(view, "chkVerifyAmount")!;
            if (!Inside(verify, panels[0])) bad.Add("the \"I checked the overall amount\" tick is not on step 1");
            before = boxes.Boxes.Count;
            if (view.TryNextStep()) bad.Add("Next passed without ticking the overall amount");
            Pump(200);
            Expect(1, "Next without the overall-amount tick");
            if (!boxes.Boxes.Skip(before).Any(m => m.Contains("I checked the overall amount"))) bad.Add("no message for the missing overall-amount tick: " + string.Join(" | ", boxes.Boxes.Skip(before)));

            // tick it: Next goes to step 2, which shows Save
            verify.Checked = true; Pump(200);
            if (!view.TryNextStep()) bad.Add("Next refused with a statement picked and the amount ticked");
            Pump(300);
            Expect(2, "Next with a statement picked");
            if (!save.Visible || update.Visible || next.Visible || !back.Visible || clear.Visible) bad.Add($"step 2 buttons wrong (save {save.Visible}, update {update.Visible}, next {next.Visible}, back {back.Visible}, clear {clear.Visible})");
            if (!CanJump(1) || CanJump(2)) bad.Add("header clickability wrong at step 2 of a new receipt");

            // Back
            back.PerformClick(); Pump(200);
            Expect(1, "Back from step 2");
            if (save.Visible || !next.Visible) bad.Add("Save visible / Next hidden on step 1 after Back");

            // anything focusing a control jumps to its step first
            T.GetMethod("ShowStepFor", CwF)!.Invoke(view, new object?[] { FieldOf<TextBox>(view, "txtRefNo") }); Pump(100);
            Expect(2, "ShowStepFor(reference)");
            T.GetMethod("ShowStepFor", CwF)!.Invoke(view, new object?[] { pick }); Pump(100);
            Expect(1, "ShowStepFor(statement picker)");

            // Reset returns to step 1 and clears the pick
            view.GoToStep(2); Pump(100);
            T.GetMethod("BtnClear_Click", CwF)!.Invoke(view, new object?[] { null, EventArgs.Empty }); Pump(300);
            Expect(1, "Reset form");
            if (pick.SelectedIndex >= 0) bad.Add("Reset did not clear the picked statement");
            if (!clear.Visible || back.Visible) bad.Add("Reset did not restore the step 1 buttons");

            // a statement opened from another screen (Proceed to Collection Receipt) stays on step 1, where the overall amount is ticked
            T.GetMethod("ClearFields", CwF)!.Invoke(view, null);
            view.PrefillForSoa(soaId); Pump(500);
            Expect(1, "opened for a statement");
            if (pick.SelectedIndex < 0) bad.Add("the statement was not picked when opened from another screen");
            if (!next.Visible || save.Visible) bad.Add("step 1 buttons wrong after opening for a statement");

            // typing a reference number already used does not search or open that receipt; it is only flagged
            var refBox = FieldOf<TextBox>(view, "txtRefNo")!;
            var existing = MJdatabase.GetCollectionReceipts().FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.ReferenceNumber));
            refBox.Text = existing?.ReferenceNumber ?? "REF-NONE"; Pump(300);
            if (T.GetField("selectedCollection", CwF)!.GetValue(view) != null) bad.Add("typing a used reference number opened a saved receipt");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string CollectionWizard_SavedReceipt_HeaderJumpsFreely_ThenResetsToStep1()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        int soaId = SaveSoaFor("CLIENT A", (a, 6000m, 300m));
        MJdatabase.SaveInvoiceForSoa(soaId, "SI-9412");
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var no = new NoPresser(GetCurrentThreadId());   // "print the receipt now?" → No
            var (view, host) = CwMakeView();
            var T = typeof(CollectionView);
            var bad = new List<string>();
            view.PrefillForSoa(soaId); Pump(400);
            FieldOf<TextBox>(view, "txtReceiptNo")!.Text = "4611";
            var method = FieldOf<ComboBox>(view, "cbPaymentMethod")!;
            int cash = method.FindStringExact("Cash"); if (cash >= 0) method.SelectedIndex = cash; else method.Text = "Cash";
            FieldOf<TextBox>(view, "txtRefNo")!.Text = "REF-4611";
            FieldOf<CheckBox>(view, "chkVerifyAmount")!.Checked = true;
            Pump(300);
            T.GetMethod("SaveNewReceipt", CwF)!.Invoke(view, null); Pump(600);
            if (view.CurrentWizardStep != 1) bad.Add("after saving the form is on step " + view.CurrentWizardStep);
            var saved = MJdatabase.GetCollectionReceipts().FirstOrDefault(r => r.ReceiptNumber == "4611");
            if (saved == null) { host.Close(); return "the receipt was not saved"; }
            // open the saved receipt
            T.GetMethod("BindCollectionToForm", CwF)!.Invoke(view, new object[] { saved });
            T.GetMethod("SetFormVisibility", CwF)!.Invoke(view, new object[] { true }); Pump(400);
            var save = FieldOf<Button>(view, "btnSave")!; var update = FieldOf<Button>(view, "btnUpdate")!; var next = CwGet<Button>(view, "btnNext");
            bool CanJump(int n) => (bool)T.GetMethod("CanJumpToStep", CwF)!.Invoke(view, new object[] { n })!;
            if (view.CurrentWizardStep != 1) bad.Add("an opened receipt starts on step " + view.CurrentWizardStep);
            if (!CanJump(2)) bad.Add("step 2 is not clickable on a saved receipt");
            if (save.Visible || update.Visible || !next.Visible) bad.Add($"step 1 of a saved receipt: save {save.Visible}, update {update.Visible}, next {next.Visible}");
            view.GoToStep(2); Pump(300);
            if (view.CurrentWizardStep != 2) bad.Add("header jump to step 2 failed");
            // Nov 24: an issued receipt is read-only, so its last step has neither Save nor Update (it used to offer Update).
            if (save.Visible || update.Visible || next.Visible) bad.Add($"step 2 of a saved receipt: save {save.Visible}, update {update.Visible}, next {next.Visible}");
            if (!CanJump(1)) bad.Add("step 1 is not clickable from step 2 of a saved receipt");
            view.GoToStep(1); Pump(200);
            if (update.Visible) bad.Add("Update visible on step 1");
            // Reset (Clear form) goes back to a new receipt on step 1
            view.GoToStep(2); Pump(100);
            T.GetMethod("BtnClear_Click", CwF)!.Invoke(view, new object?[] { null, EventArgs.Empty }); Pump(300);
            if (view.CurrentWizardStep != 1 || CanJump(2)) bad.Add("Reset did not return to a new receipt on step 1");
            host.Close();
            return string.Join("; ", bad);
        });
    }
}
