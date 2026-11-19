using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>The New / Edit Dispatch Order form is a 3-step wizard: one step card at a time, Next validates the step, Back / Reset / stepper navigate.</summary>
public static partial class Checks
{
    [Check]
    public static string DispatchWizard_NewOrder_StepsValidateAndNavigate()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        MJdatabase.RegisterCompanyStore("CLIENT A", "A-1", "Store One", "12 Rizal St., Tarlac City");
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var view = DcMakeView(out var host);
            var T = typeof(DispatchManagementView);
            var bad = new List<string>();
            view.OpenNewOrderForm(); Pump(300);
            var panels = new[] { "pnlStep1ClientStores", "pnlStep2LoadCrew", "pnlStep3Schedule" }.Select(n => DcGet<Panel>(view, n)).ToArray();
            var save = DcGet<Button>(view, "btnSave"); var update = DcGet<Button>(view, "btnUpdate");
            var next = DcGet<Button>(view, "btnNext"); var back = DcGet<Button>(view, "btnBack"); var clear = DcGet<Button>(view, "btnClear");
            var client = DcGet<ComboBox>(view, "cbClient"); var grid = DcGet<DataGridView>(view, "gridRouteStores");
            var g = DcGet<NumericUpDown>(view, "numGCrate");
            bool CanJump(int n) => (bool)T.GetMethod("CanJumpToStep", DcF)!.Invoke(view, new object[] { n })!;
            string State() => $"step {view.CurrentWizardStep}, visible [{string.Join(",", panels.Select(p => p.Visible ? "1" : "0"))}]";
            void Expect(int step, string what)
            {
                if (view.CurrentWizardStep != step) bad.Add($"{what}: on step {view.CurrentWizardStep}, expected {step}");
                for (int i = 0; i < panels.Length; i++) if (panels[i].Visible != (i + 1 == step)) { bad.Add($"{what}: panel visibility wrong ({State()})"); break; }
            }
            Expect(1, "new order start");
            if (!clear.Visible || back.Visible || !next.Visible || save.Visible) bad.Add("step 1 buttons wrong");
            if (!next.Text.Contains("Crates, truck & crew")) bad.Add("step 1 Next text: " + next.Text);
            if (CanJump(3) || CanJump(2)) bad.Add("a future step is clickable on a new order");

            int before = boxes.Boxes.Count;
            view.TryNextStep(); Pump(200);
            Expect(1, "Next with no client");
            if (boxes.Boxes.Count == before) bad.Add("no message for a missing client");

            client.Text = "CLIENT A"; Pump(300);
            view.TryNextStep(); Pump(200);
            Expect(1, "Next with a client but no store");
            Expect(1, "Next with a client but no temp type / store");
            // Temp Type now lives on step 1 (inside the entry row), and the Edit button is hidden.
            var temp = DcGet<ComboBox>(view, "cbTempType");
            bool Inside(Control c, Control p) { for (var x = c; x != null; x = x.Parent) if (x == p) return true; return false; }
            if (!Inside(temp, panels[0])) bad.Add("Temp Type is not on step 1");
            if (Inside(temp, panels[1])) bad.Add("Temp Type is still on step 2");
            var editBtn = (Button?)T.GetField("btnEditStoreTable", DcF)!.GetValue(view);
            if (editBtn != null && editBtn.Visible) bad.Add("the table Edit button is visible");
            if (grid.Rows.Count != 0) bad.Add("table not empty at start");
            var tCode = DcGet<TextBox>(view, "txtEntryCode"); var tName = DcGet<TextBox>(view, "txtEntryName");
            var tG = DcGet<TextBox>(view, "txtEntryG"); var tE = DcGet<TextBox>(view, "txtEntryE");
            var add = T.GetMethod("AddStoreFromEntryRow", DcF)!;
            // nothing typed / no crates: refused with a message
            before = boxes.Boxes.Count;
            if ((bool)add.Invoke(view, null)!) bad.Add("an empty entry row added a store");
            tCode.Text = "A-1"; tName.Text = "Store One"; Pump(100);
            if ((bool)add.Invoke(view, null)!) bad.Add("a store with 0 crates was added");
            if (boxes.Boxes.Count < before + 2) bad.Add("no message for an empty entry row / 0 crates");
            tG.Text = "2"; tE.Text = "1";
            if (!(bool)add.Invoke(view, null)!) bad.Add("a good entry row was not added"); Pump(300);
            if (grid.Rows.Count != 1 || (grid.Rows[0].Cells["colStoreCode"].Value?.ToString() ?? "") != "A-1" || (grid.Rows[0].Cells["colGCrate"].Value?.ToString() ?? "") != "2" || (grid.Rows[0].Cells["colECrate"].Value?.ToString() ?? "") != "1")
                bad.Add("entry row did not produce the expected table row");
            if (tCode.Text.Length + tName.Text.Length + tG.Text.Length + tE.Text.Length != 0) bad.Add("entry row not cleared after adding");
            if (g.Value != 2) bad.Add("G crates total " + g.Value);
            // the same store twice is refused
            tCode.Text = "A-1"; tName.Text = "Store One"; tG.Text = "1";
            if ((bool)add.Invoke(view, null)!) bad.Add("the same store was added twice");
            tCode.Text = ""; tName.Text = ""; tG.Text = "";
            // the table can't be typed in
            if (!grid.Columns["colGCrate"]!.ReadOnly || !grid.Columns["colStoreCode"]!.ReadOnly) bad.Add($"a store table column can be edited (G {grid.Columns["colGCrate"]!.ReadOnly}, code {grid.Columns["colStoreCode"]!.ReadOnly}, grid {grid.ReadOnly})");
            if (grid.Columns["colGCrate"]!.HeaderText.Contains("✎")) bad.Add("store table header still has the pencil mark");
            view.TryNextStep(); Pump(200);
            Expect(1, "Next with client and store but no Temp Type");
            temp.SelectedIndex = 1; Pump(200);
            view.TryNextStep(); Pump(300);
            Expect(2, "Next with client, temp type and store");
            if (!back.Visible || clear.Visible || !next.Text.Contains("Schedule & status")) bad.Add("step 2 buttons wrong");
            if (!CanJump(1) || CanJump(3)) bad.Add("stepper clickability wrong at step 2 of a new order");

            g.Value = 0; DcGet<NumericUpDown>(view, "numECrate").Value = 0; Pump(200);
            view.TryNextStep(); Pump(200);
            Expect(2, "Next with 0 crates");
            g.Value = 5; Pump(200);
            before = boxes.Boxes.Count;
            view.TryNextStep(); Pump(200);
            Expect(2, "Next with crates but no truck / driver");
            if (boxes.Boxes.Count == before) bad.Add("no message for a missing truck / driver");

            view.GoToStep(3); Pump(300);
            Expect(3, "step 3");
            if (!save.Visible || update.Visible || next.Visible) bad.Add("step 3 should show Save only");
            DcGet<Button>(view, "btnBack").PerformClick(); Pump(200);
            Expect(2, "Back from step 3");
            if (save.Visible) bad.Add("Save visible before the last step");

            // Anything focusing a control jumps to its step first.
            T.GetMethod("ShowStepFor", DcF)!.Invoke(view, new object?[] { client }); Pump(100);
            Expect(1, "ShowStepFor(client)");
            T.GetMethod("ShowStepFor", DcF)!.Invoke(view, new object?[] { DcGet<ComboBox>(view, "cbTruck") }); Pump(100);
            Expect(2, "ShowStepFor(truck)");

            // Reset form goes back to step 1.
            view.GoToStep(3); Pump(100);
            T.GetMethod("BtnClear_Click", DcF)!.Invoke(view, new object?[] { null, EventArgs.Empty }); Pump(300);
            Expect(1, "Reset form");
            if (!clear.Visible || back.Visible) bad.Add("Reset did not restore the step 1 buttons");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string DispatchWizard_EditMode_StartsAtStep1_StepperJumpsFreely()
    {
        Fresh();
        MJdatabase.RunOneTimeMigrations();
        int id = Seed.Delivery("CLIENT A", "Scheduled");
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new VdBoxes();
            var view = DcMakeView(out var host);
            var T = typeof(DispatchManagementView);
            var bad = new List<string>();
            view.OpenNewOrderForm(); Pump(300);
            view.GoToStep(3); Pump(100);
            T.GetMethod("BindDeliveryToForm", DcF)!.Invoke(view, new object?[] { MJdatabase.GetDeliveries().First(d => d.Id == id) }); Pump(400);
            var save = DcGet<Button>(view, "btnSave"); var update = DcGet<Button>(view, "btnUpdate");
            if (view.CurrentWizardStep != 1) bad.Add("an opened order starts on step " + view.CurrentWizardStep);
            bool CanJump(int n) => (bool)T.GetMethod("CanJumpToStep", DcF)!.Invoke(view, new object[] { n })!;
            if (!CanJump(2) || !CanJump(3)) bad.Add("stepper items are not clickable in edit mode");
            view.GoToStep(3); Pump(300);
            if (view.CurrentWizardStep != 3) bad.Add("stepper jump to 3 failed");
            if (!update.Visible || save.Visible) bad.Add($"step 3 of an edit: Update {update.Visible}, Save {save.Visible}");
            view.GoToStep(2); Pump(200);
            if (update.Visible) bad.Add("Update visible on step 2");
            host.Close();
            return string.Join("; ", bad);
        });
    }
}
