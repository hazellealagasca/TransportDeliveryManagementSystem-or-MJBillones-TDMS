using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace TDMS.Verify;

/// <summary>The shared FormWizard component on a plain test form: 2-step and 3-step wizards.</summary>
public static partial class Checks
{
    private sealed class FwRig
    {
        public Form Host = null!; public FormWizard Wiz = null!; public Panel[] Panels = null!; public Panel Scroll = null!;
        public Button Reset = null!, Save = null!, Update = null!; public bool Edit, Pass = true; public List<int> Validated = new(); public List<int> Changed = new();
        public bool UpdateWanted;
        public Panel Bar = null!; public TextBox[] Fields = null!;
    }

    private static FwRig FwMake(int steps)
    {
        var r = new FwRig();
        r.Host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(900, 600) };
        r.Scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        r.Bar = new Panel { Dock = DockStyle.Bottom, Height = 60 };
        r.Reset = new Button { Text = "Reset" }; r.Save = new Button { Text = "Save" }; r.Update = new Button { Text = "Update" };
        var names = Enumerable.Range(1, steps).Select(i => "Step " + i + " name").ToArray();
        r.Wiz = new FormWizard(r.Host, names, () => r.Edit, s => { r.Validated.Add(s); return r.Pass; });
        r.Wiz.FirstStepButton = r.Reset; r.Wiz.ScrollHost = r.Scroll;
        r.Wiz.AddLastStepButton(r.Save, () => !r.UpdateWanted);
        r.Wiz.AddLastStepButton(r.Update, () => r.UpdateWanted);
        r.Wiz.StepChanged = s => r.Changed.Add(s);
        r.Panels = new Panel[steps]; r.Fields = new TextBox[steps];
        for (int i = 0; i < steps; i++)
        {
            r.Panels[i] = new Panel { Dock = DockStyle.Top, Height = 100 };
            r.Fields[i] = new TextBox(); r.Panels[i].Controls.Add(r.Fields[i]);
            var wrap = new Panel { Dock = DockStyle.Fill }; // field lives two levels down on purpose
        }
        for (int i = steps - 1; i >= 0; i--) r.Scroll.Controls.Add(r.Panels[i]);
        r.Wiz.SetPanels(r.Panels);
        r.Scroll.Controls.Add(r.Wiz.Header);
        r.Bar.Controls.Add(r.Wiz.NextButton); r.Bar.Controls.Add(r.Save); r.Bar.Controls.Add(r.Update);
        r.Wiz.BuildLeftActions(r.Bar);
        r.Host.Controls.Add(r.Scroll); r.Host.Controls.Add(r.Bar);
        r.Host.Show(); Pump(100);
        r.Wiz.GoToStep(1); Pump(50);
        return r;
    }

    private static void FwState(FwRig r, int step, string what, List<string> bad)
    {
        var w = r.Wiz; int n = r.Panels.Length;
        if (w.CurrentStep != step) bad.Add($"{what}: on step {w.CurrentStep}, expected {step}");
        for (int i = 0; i < n; i++) if (r.Panels[i].Visible != (i + 1 == step)) { bad.Add($"{what}: panel {i + 1} visibility wrong"); break; }
        bool last = step == n;
        if (w.BackButton.Visible != (step > 1)) bad.Add($"{what}: Back visibility wrong");
        if (r.Reset.Visible != (step == 1)) bad.Add($"{what}: first-step button visibility wrong");
        if (w.NextButton.Visible == last) bad.Add($"{what}: Next visibility wrong");
        if (r.Save.Visible != (last && !r.UpdateWanted)) bad.Add($"{what}: Save visibility wrong");
        if (r.Update.Visible != (last && r.UpdateWanted)) bad.Add($"{what}: Update visibility wrong");
        if (!last && w.NextButton.Text != "Step " + (step + 1) + " name  →") bad.Add($"{what}: Next text '{w.NextButton.Text}'");
    }

    [Check]
    public static string FormWizard_TwoAndThreeSteps_NavigateValidateAndShowButtons()
    {
        return RunOnSta(() =>
        {
            var bad = new List<string>();
            foreach (int steps in new[] { 2, 3 })
            {
                var r = FwMake(steps); var w = r.Wiz; string tag = steps + "-step";
                if (w.StepCount != steps) bad.Add(tag + ": StepCount " + w.StepCount);
                FwState(r, 1, tag + " start", bad);
                if (w.CanJump(2) || w.CanJump(1)) bad.Add(tag + ": new record can jump forward / to the current step");

                // Next blocked when validation fails
                r.Pass = false; r.Validated.Clear();
                if (w.TryNextStep()) bad.Add(tag + ": TryNextStep passed with failing validation");
                if (r.Validated.Count != 1 || r.Validated[0] != 1) bad.Add(tag + ": validation not called for step 1");
                FwState(r, 1, tag + " blocked", bad);
                r.Wiz.NextButton.PerformClick(); Pump(50);
                FwState(r, 1, tag + " blocked via button", bad);

                r.Pass = true;
                if (!w.TryNextStep()) bad.Add(tag + ": TryNextStep refused with passing validation");
                FwState(r, 2, tag + " step 2", bad);
                if (!w.CanJump(1) || w.CanJump(2)) bad.Add(tag + ": jump rule on step 2 of a new record");
                if (steps == 3 && w.CanJump(3)) bad.Add(tag + ": future step clickable in a new record");
                if (!r.Changed.Contains(2)) bad.Add(tag + ": StepChanged not raised");

                if (steps == 3)
                {
                    w.NextButton.PerformClick(); Pump(50);
                    FwState(r, 3, tag + " step 3 via Next button", bad);
                }
                if (!w.TryNextStep() || w.CurrentStep != steps) bad.Add(tag + ": TryNextStep on the last step moved or failed");
                // Back
                w.BackButton.PerformClick(); Pump(50);
                FwState(r, steps - 1, tag + " Back", bad);
                w.BackButton.PerformClick(); Pump(50);
                FwState(r, steps == 3 ? 1 : 1, tag + " Back to 1", bad);
                if (steps == 3 && w.CurrentStep != 1) bad.Add(tag + ": Back twice did not reach 1");

                // last step buttons follow what the form wants
                w.GoToStep(steps); r.UpdateWanted = true; w.Refresh();
                FwState(r, steps, tag + " update wanted", bad);
                r.UpdateWanted = false; w.Refresh();
                w.GoToStep(99); if (w.CurrentStep != steps) bad.Add(tag + ": GoToStep not clamped high");
                w.GoToStep(-4); if (w.CurrentStep != 1) bad.Add(tag + ": GoToStep not clamped low");

                // edit mode: any other step is clickable
                r.Edit = true;
                for (int s = 1; s <= steps; s++) if (w.CanJump(s) != (s != w.CurrentStep)) bad.Add($"{tag}: edit-mode CanJump({s}) wrong");
                r.Edit = false;

                // scroll position resets on step change
                r.Panels[0].Height = 1500; r.Panels[1].Height = 1500; w.GoToStep(1); Pump(100);
                try { r.Scroll.AutoScrollPosition = new Point(0, 400); } catch { }
                w.GoToStep(2); Pump(100);
                if (r.Scroll.VerticalScroll.Value != 0) bad.Add(tag + ": scroll not reset on step change");
                r.Panels[0].Height = 100; r.Panels[1].Height = 100;

                // ShowStepFor / Focus walk the parents
                w.GoToStep(1);
                w.ShowStepFor(r.Fields[steps - 1]); Pump(50);
                FwState(r, steps, tag + " ShowStepFor", bad);
                w.GoToStep(1); w.Focus(r.Fields[1]); Pump(50);
                FwState(r, 2, tag + " Focus", bad);
                if (!r.Fields[1].Focused) bad.Add(tag + ": Focus did not focus the control");
                w.ShowStepFor(new TextBox()); w.ShowStepFor(null);
                FwState(r, 2, tag + " ShowStepFor outside", bad);

                // header exists, is named and docked top
                if (w.Header.Name != "pnlStepperHeader" || w.Header.Dock != DockStyle.Top) bad.Add(tag + ": header name / dock");
                // header jump: click inside the cell of step 1 while on step 2 (new record)
                w.GoToStep(2); Pump(50);
                var cell1 = new Point(w.Header.Width / steps / 2, w.Header.Height / 2);
                var click = typeof(Control).GetMethod("OnMouseClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                click.Invoke(w.Header, new object[] { new MouseEventArgs(MouseButtons.Left, 1, cell1.X, cell1.Y, 0) });
                if (w.CurrentStep != 1) bad.Add(tag + ": header click on a finished step did not go back");
                if (steps == 3)
                {
                    var cell3 = new Point(w.Header.Width * 5 / 6, w.Header.Height / 2);
                    click.Invoke(w.Header, new object[] { new MouseEventArgs(MouseButtons.Left, 1, cell3.X, cell3.Y, 0) });
                    if (w.CurrentStep != 1) bad.Add(tag + ": header click on a future step moved in a new record");
                    r.Edit = true;
                    click.Invoke(w.Header, new object[] { new MouseEventArgs(MouseButtons.Left, 1, cell3.X, cell3.Y, 0) });
                    if (w.CurrentStep != 3) bad.Add(tag + ": header click on step 3 failed in edit mode");
                }
                else
                {
                    var cell2 = new Point(w.Header.Width * 3 / 4, w.Header.Height / 2);
                    click.Invoke(w.Header, new object[] { new MouseEventArgs(MouseButtons.Left, 1, cell2.X, cell2.Y, 0) });
                    if (w.CurrentStep != 1) bad.Add(tag + ": header click on a future step moved in a new record");
                    r.Edit = true;
                    click.Invoke(w.Header, new object[] { new MouseEventArgs(MouseButtons.Left, 1, cell2.X, cell2.Y, 0) });
                    if (w.CurrentStep != 2) bad.Add(tag + ": header click on step 2 failed in edit mode");
                }
                r.Host.Close();
            }
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string FormWizard_CustomNextText_AndNeedsTwoSteps()
    {
        return RunOnSta(() =>
        {
            var bad = new List<string>();
            var r = FwMake(3);
            r.Wiz.SetNextText(1, "Go on");
            if (r.Wiz.NextButton.Text != "Go on") bad.Add("custom Next text not applied: " + r.Wiz.NextButton.Text);
            r.Wiz.GoToStep(2);
            if (r.Wiz.NextButton.Text != "Step 3 name  →") bad.Add("step 2 Next text: " + r.Wiz.NextButton.Text);
            r.Wiz.GoToStep(1);
            if (r.Wiz.NextButton.Text != "Go on") bad.Add("custom text lost: " + r.Wiz.NextButton.Text);
            r.Host.Close();
            try { _ = new FormWizard(new Form(), new[] { "only" }, () => false, _ => true); bad.Add("a 1-step wizard was accepted"); } catch (ArgumentException) { }
            return string.Join("; ", bad);
        });
    }
}
