using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// The one capacity window for S1–S4 and D4/D6/D7: what the load is, the truck's matrix, what is wrong, a list of
    /// trucks that can carry the load (Part B), and the way forward — exceed with a reason (S2), pick another truck,
    /// or exceed the capacity with a warning and a reason.
    /// </summary>
    public class CapacityCheckDialog : Form
    {
        public enum Decision { Cancel, Proceed, UseTruck }
        public Decision Result { get; private set; } = Decision.Cancel;
        public string Reason { get; private set; } = "";
        public Truck? ChosenTruck { get; private set; }
        private readonly CapacityResult res;
        private TextBox? txtReason;
        private Label? lblReasonErr;
        private ListView? lvTrucks;
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);




        /// <param name="gate">True when called from Save: shows the actions. False: details only.</param>
        public CapacityCheckDialog(CapacityResult result, IEnumerable<Truck> candidateTrucks, bool gate)
        {
            res = result;
            Text = "Capacity check";
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", 9.5F);
            ClientSize = new Size(D(640), D(gate && CanExceed(result) ? 650 : 580));
            MinimumSize = new Size(D(560), D(480));
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = AppTheme.Canvas;




            bool good = res.State == CapacityState.Allow;
            bool warn = res.State == CapacityState.ExceedWithReason;
            Color tone = good ? AppTheme.SuccessText : warn ? AppTheme.WarningText : AppTheme.DangerText;
            Color soft = good ? AppTheme.SuccessSoft : warn ? AppTheme.WarningSoft : AppTheme.DangerSoft;




            var header = new Panel { Dock = DockStyle.Top, Height = D(92), BackColor = soft, Padding = new Padding(D(20), D(12), D(20), D(10)) };
            header.Controls.Add(new Label { Dock = DockStyle.Fill, Text = res.Message, Font = new Font("Segoe UI", 9.75F), ForeColor = AppTheme.TextPrimary, UseMnemonic = false });
            header.Controls.Add(new Label { Dock = DockStyle.Top, Height = D(30), Text = $"{(good ? "✓" : warn ? "⚠" : "⛔")}  {res.Title}", Font = new Font("Segoe UI Semibold", 13F), ForeColor = tone, UseMnemonic = false });




            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(D(20), D(12), D(20), D(8)), BackColor = AppTheme.Canvas, AutoScroll = true };




            // Part A: the load against the truck's matrix
            var m = res.Matrix;
            var tbl = new ListView { View = View.Details, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.Nonclickable, Dock = DockStyle.Top, Height = D(130), BorderStyle = BorderStyle.FixedSingle, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, Font = new Font("Segoe UI", 9.5F) };
            tbl.Columns.Add("", D(170));
            tbl.Columns.Add("This load", D(110));
            tbl.Columns.Add("Truck allows", D(130));
            tbl.Columns.Add("", D(150));
            string truckName = res.Truck == null ? "—" : $"{res.Truck.PlateNumber} · {res.Truck.TruckType} · {MJdatabase.NormalizeCargo(res.Truck.CargoType)}";
            void Row(string k, string a, string b, bool bad) => tbl.Items.Add(new ListViewItem(new[] { k, a, b, bad ? "over" : "" }) { ForeColor = bad ? AppTheme.DangerText : AppTheme.TextPrimary });
            if (res.Cargo == MJdatabase.CargoCombi)
            {
                Row("Cold crates", res.Cold.ToString("N0"), $"{m.ColdMax:N0} max", res.Cold > m.ColdMax);
                Row("Dry crates", res.Dry.ToString("N0"), $"{m.DryMax:N0} max", res.Dry > m.DryMax);
                Row("Combi plan", res.Total.ToString("N0"), m.CombiMax > 0 ? $"{m.CombiColdMax} + {m.CombiDryMax} = {m.CombiMax}" : "not set", m.CombiMax > 0 && res.Total > m.CombiMax);
                Row("Truck maximum", res.Total.ToString("N0"), $"{m.TruckMax:N0}", res.Total > m.TruckMax);
            }
            else if (res.Cargo == MJdatabase.CargoDry) Row("Dry crates", res.Total.ToString("N0"), $"{m.DryMax:N0} max", res.Total > m.DryMax);
            else Row("Cold crates", res.Total.ToString("N0"), $"{m.ColdMax:N0} max", res.Total > m.ColdMax);
            var lblTruck = new Label { Dock = DockStyle.Top, Height = D(26), Text = $"Truck: {truckName}   ·   Crate type: {res.Cargo}", Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = AppTheme.TextSecondary, UseMnemonic = false };




            // Part B: trucks that can carry the load
            var candidates = res.State == CapacityState.NoCrates || res.State == CapacityState.CombiSplitMissing
                ? new List<(Truck Truck, CapacityResult Result)>()
                : CapacityValidator.TrucksThatFit(candidateTrucks, res.Cargo, res.Cold, res.Dry, res.Truck?.Id ?? 0);
            var lblB = new Label { Dock = DockStyle.Top, Height = D(34), Text = candidates.Count == 0 ? "No other free truck can carry this whole load." : "Trucks that can carry this load (double-click to use one):", Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = AppTheme.TextSecondary, TextAlign = ContentAlignment.BottomLeft, UseMnemonic = false };
            lvTrucks = new ListView { View = View.Details, FullRowSelect = true, MultiSelect = false, HeaderStyle = ColumnHeaderStyle.Nonclickable, Dock = DockStyle.Top, Height = D(150), BorderStyle = BorderStyle.FixedSingle, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, Font = new Font("Segoe UI", 9.5F) };
            lvTrucks.Columns.Add("Truck", D(120));
            lvTrucks.Columns.Add("Category", D(160));
            lvTrucks.Columns.Add("Crate type", D(130));
            lvTrucks.Columns.Add("Load", D(160));
            foreach (var (t, r) in candidates)
                lvTrucks.Items.Add(new ListViewItem(new[] { t.PlateNumber, t.TruckType, MJdatabase.NormalizeCargo(t.CargoType), $"{r.Total:N0} of {r.Limit:N0}" }) { Tag = t, ForeColor = AppTheme.TextPrimary });
            lvTrucks.Visible = candidates.Count > 0;
            if (gate) lvTrucks.DoubleClick += (s, e) => UseSelectedTruck();




            // S2: reason
            Panel? reasonBox = null;
            if (gate && CanExceed(res))
            {
                reasonBox = new Panel { Dock = DockStyle.Top, Height = D(104), Padding = new Padding(0, D(10), 0, 0), BackColor = AppTheme.Canvas };
                txtReason = new TextBox { Dock = DockStyle.Fill, Multiline = true, Font = new Font("Segoe UI", 10F), PlaceholderText = $"Why does this trip go over the plan? (at least {CapacityValidator.MinReasonLength} characters)", MaxLength = 300 };
                lblReasonErr = new Label { Dock = DockStyle.Bottom, Height = D(24), ForeColor = AppTheme.DangerText, Font = new Font("Segoe UI", 8.75F) };
                reasonBox.Controls.Add(txtReason);
                reasonBox.Controls.Add(lblReasonErr);
                reasonBox.Controls.Add(new Label { Dock = DockStyle.Top, Height = D(22), Text = res.State == CapacityState.ExceedWithReason ? $"Reason for going over the matrix (+{res.Over} crates) *" : $"⚠ Reason for exceeding the truck's capacity (+{res.Over} crates) *", Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = AppTheme.WarningText });
            }




            // Dock order: added last = top-most.
            if (reasonBox != null) body.Controls.Add(reasonBox);
            body.Controls.Add(lvTrucks);
            body.Controls.Add(lblB);
            body.Controls.Add(tbl);
            body.Controls.Add(lblTruck);




            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = D(58), FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(D(16), D(10), D(16), D(10)), BackColor = AppTheme.Surface, WrapContents = false };
            Button B(string t, string role, Action a)
            {
                var b = new Button { Text = t, AutoSize = true, MinimumSize = new Size(D(96), D(36)), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(D(8), 0, 0, 0), Padding = new Padding(D(6), 0, D(6), 0), Font = new Font("Segoe UI Semibold", 9.25F), UseMnemonic = false };
                b.FlatAppearance.BorderSize = 0;
                UIThemeHelper.SetButtonRole(b, role);
                b.Click += (s, e) => a();
                return b;
            }
            if (!gate || res.State == CapacityState.Allow)
            {
                var ok = B("OK", "primary", () => { Result = res.State == CapacityState.Allow ? Decision.Proceed : Decision.Cancel; DialogResult = DialogResult.OK; });
                footer.Controls.Add(ok);
                AcceptButton = ok;
                if (gate && candidates.Count > 0) footer.Controls.Add(B("Use selected truck", "secondary", UseSelectedTruck));
            }
            else
            {
                if (CanExceed(res)) footer.Controls.Add(B($"Exceed capacity (+{res.Over}) and save", res.State == CapacityState.ExceedWithReason ? "strong" : "danger", ProceedWithReason));
                if (res.State == CapacityState.NoCrates || res.State == CapacityState.CombiSplitMissing)
                    footer.Controls.Add(B("OK, I'll fix it", "primary", () => { Result = Decision.Cancel; DialogResult = DialogResult.Cancel; }));
                else
                {
                    if (candidates.Count > 0) footer.Controls.Add(B("Use selected truck", "primary", UseSelectedTruck));
                    footer.Controls.Add(B("Cancel", "secondary", () => { Result = Decision.Cancel; DialogResult = DialogResult.Cancel; }));
                }
            }




            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(header);
            UIThemeHelper.ApplyTheme(this);
            header.BackColor = soft;
            if (lvTrucks.Items.Count > 0) lvTrucks.Items[0].Selected = true;
        }




        /// <summary>Over-capacity loads (S2, S3, S4) may go ahead with a warning and a reason; D4, D6 and D7 can't.</summary>
        private static bool CanExceed(CapacityResult r) => r.State == CapacityState.ExceedWithReason || r.State == CapacityState.BlockPure || r.State == CapacityState.BlockCombi;



        private void UseSelectedTruck()
        {
            if (lvTrucks == null || lvTrucks.SelectedItems.Count == 0 || lvTrucks.SelectedItems[0].Tag is not Truck t)
            {
                QuietBox.Show("Pick a truck from the list first.", "Use another truck", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ChosenTruck = t;
            Result = Decision.UseTruck;
            DialogResult = DialogResult.OK;
        }




        private void ProceedWithReason()
        {
            string r = (txtReason?.Text ?? "").Trim();
            if (r.Length < CapacityValidator.MinReasonLength)
            {
                if (lblReasonErr != null) lblReasonErr.Text = $"Please type a reason of at least {CapacityValidator.MinReasonLength} characters.";
                txtReason?.Focus();
                return;
            }
            if (res.State != CapacityState.ExceedWithReason &&
                MessageBox.Show(this, $"⚠ EXCEED CAPACITY WARNING\n\n{res.Truck?.PlateNumber} will carry {res.Total:N0} crates — {res.Over:N0} more than its {res.Limit:N0}-crate limit.\n\nThis is recorded on the dispatch (\"Over matrix +{res.Over}\") and in the audit log with your reason.\n\nAre you sure you want to exceed the capacity?", "Exceed capacity warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            Reason = r;
            Result = Decision.Proceed;
            DialogResult = DialogResult.OK;
        }




        protected override void Dispose(bool disposing)
        {
            DarkModeKit.RunWithoutFalseCrossThreadCheck(() => base.Dispose(disposing), nameof(CapacityCheckDialog));
        }
    }
}
