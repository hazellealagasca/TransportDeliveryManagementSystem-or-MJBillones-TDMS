using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Capacity check on the dispatch form: the Combi cold/dry split, the live status line with
    /// "View capacity details", the Save gate (S1–S4, D4/D6/D7) and the "Over matrix +X" badge in the list.
    /// </summary>
    public partial class DispatchManagementView
    {
        private NumericUpDown numCombiCold = null!;
        private Label lblCombiDry = null!;
        private Label lblCombiSplit = null!;
        private Panel pnlCombiSplit = null!;
        private LinkLabel? lnkCapDetails;
        private Dictionary<int, int> overCapacityMap = new Dictionary<int, int>();






        private void InitCapacityUi()
        {
            lblCombiSplit = new Label { Text = "Combi: cold crates *", AutoSize = true, UseMnemonic = false };
            pnlCombiSplit = new Panel { Dock = DockStyle.Fill };
            numCombiCold = new NumericUpDown { Dock = DockStyle.Left, Width = Dpi(110), Maximum = 100000, Minimum = 0, Font = new Font("Segoe UI", 10F) };
            InputGuards.DigitsOnly(numCombiCold);
            lblCombiDry = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(Dpi(10), 0, 0, 0), Font = new Font("Segoe UI", 9.5F), ForeColor = AppTheme.TextSecondary, UseMnemonic = false };
            pnlCombiSplit.Controls.Add(lblCombiDry);
            pnlCombiSplit.Controls.Add(numCombiCold);
            numCombiCold.ValueChanged += (s, e) => { if (!isBindingForm) CalculateCratesAndRecommendation(); };
            lblCombiSplit.Visible = false;
            pnlCombiSplit.Visible = false;






            lnkCapDetails = new LinkLabel { Text = "View capacity details", AutoSize = true, Dock = DockStyle.Bottom, Font = new Font("Segoe UI Semibold", 8.75F), LinkColor = AppTheme.Accent, ActiveLinkColor = AppTheme.AccentHover, Padding = new Padding(0, Dpi(2), 0, 0), Visible = false };
            lnkCapDetails.LinkClicked += (s, e) => ShowCapacityDetails();
            pnlRec.Controls.Add(lnkCapDetails);
        }






        /// <summary>Nov 6 workflow: there is no cold / dry split on the dispatch any more (Combi is a note in the remarks).</summary>
        private bool IsCombiCargo => cbTempType != null && cbTempType.SelectedIndex == 2;






        /// <summary>Cold and dry crates of the load: Dry Goods = all dry, Cold = all cold, Combi = the split typed in.</summary>
        private (int cold, int dry) CurrentCrateSplit(int total)
        {
            int c = (int)numCombiCold.Value;
            return CapacityValidator.SplitFor(cbCargoType.Text, total, c, Math.Max(0, total - c));
        }






        private void UpdateCombiSplitUi(int total)
        {
            bool combi = IsCombiCargo && !CargoRouting.IsSpecial(cbCargoType.Text);
            lblCombiSplit.Visible = combi;
            pnlCombiSplit.Visible = combi;
            if (pnlRowCombi != null && pnlRowCombi.Visible != combi) pnlRowCombi.Visible = combi;
            if (!combi) return;
            int c = (int)numCombiCold.Value;
            int dry = total - c;
            lblCombiDry.Text = dry < 0 ? $"cold crates  ·  ⚠ more than the {total:N0} crates entered" : $"cold  +  {dry:N0} dry  =  {total:N0} crates";
            lblCombiDry.ForeColor = dry < 0 ? AppTheme.DangerText : AppTheme.TextSecondary;
        }






        private Truck? SelectedTruckOrNull() => (cbTruck.SelectedIndex >= 0 && cbTruck.SelectedIndex < trucks.Count) ? trucks[cbTruck.SelectedIndex] : null;






        /// <summary>The live status line under the crates (uses the same check as Save).</summary>
        private bool ApplyLiveCapacityStatus(Truck selTruck, int total)
        {
            var r = ValidateLoad(selTruck, total); // Nov 6 workflow: total crates vs the truck's maximum
            Color back, fore;
            string icon;
            switch (r.State)
            {
                case CapacityState.Allow: back = Pal.C(240, 253, 244); fore = Pal.C(22, 101, 52); icon = "✓"; break;
                case CapacityState.ExceedWithReason: back = Pal.C(255, 251, 235); fore = Pal.C(180, 83, 9); icon = "⚠️"; break;
                case CapacityState.BlockPure:
                case CapacityState.BlockCombi: back = Pal.C(254, 242, 242); fore = Pal.C(185, 28, 28); icon = "⚠️ Exceeds capacity —"; break;
                default: back = Pal.C(254, 242, 242); fore = Pal.C(185, 28, 28); icon = "⛔"; break;
            }
            pnlRec.BackColor = back;
            lblRec.ForeColor = fore;
            lblRec.Text = $"{icon} Total {total:N0} crates  ·  {selTruck.PlateNumber}  ·  {total:N0} / {r.Limit:N0}" + (r.Limit > 0 ? $" ({Math.Round(total * 100.0 / r.Limit):0}% full)" : "") + (r.State == CapacityState.Allow ? "" : $"  ·  {r.Title}");
            if (lnkCapDetails != null) lnkCapDetails.Visible = true;
            return true;
        }






        private List<Truck> TrucksFreeForThisOrder()
        {
            try
            {
                int selfId = selectedDelivery?.Id ?? 0;
                return trucks.Where(t => !MJdatabase.IsTruckBlockedStatus(t.Status) && !bookedTrucks.Contains(t.Id)).ToList();
            }
            catch { return trucks.ToList(); }
        }






        private void ShowCapacityDetails()
        {
            var t = SelectedTruckOrNull();
            int total = (int)(numGCrate.Value + numECrate.Value);
            if (t == null) { QuietBox.Show("Choose the truck first.", "Capacity details", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var r = ValidateLoad(t, total);
            using var dlg = new CapacityCheckDialog(r, TrucksFreeForThisOrder(), gate: false);
            dlg.ShowDialog(FindForm());
        }






        /// <summary>
        /// Save gate. Returns true when the order may be saved (S1, or S2 with a reason). Picking another truck selects it
        /// on the form and returns false so the person can check and save again. Over capacity needs a warning and a reason.
        /// </summary>
        private bool PassCapacityGate(Truck selTr, int total, out int cold, out int dry, out int over, out string reason)
        {
            (cold, dry) = CurrentCrateSplit(total);
            over = 0;
            reason = "";
            var r = ValidateLoad(selTr, total);
            if (r.State == CapacityState.Allow) return true;
            using var dlg = new CapacityCheckDialog(r, TrucksFreeForThisOrder(), gate: true);
            dlg.ShowDialog(FindForm());
            switch (dlg.Result)
            {
                case CapacityCheckDialog.Decision.Proceed when r.State is CapacityState.ExceedWithReason or CapacityState.BlockPure or CapacityState.BlockCombi:
                    over = r.Over;
                    reason = dlg.Reason;
                    return true;
                case CapacityCheckDialog.Decision.UseTruck when dlg.ChosenTruck != null:
                    int idx = trucks.FindIndex(x => x.Id == dlg.ChosenTruck.Id);
                    if (idx >= 0) cbTruck.SelectedIndex = idx;
                    ToastHelper.ShowToast(this, $"{dlg.ChosenTruck.PlateNumber} selected — check the driver, then save again.", ToastType.Info);
                    return false;
                default:
                    return false;
            }
        }






        private void RefreshOverCapacityMap()
        {
            try { overCapacityMap = MJdatabase.GetOverCapacityMap(); } catch { overCapacityMap = new Dictionary<int, int>(); }
        }






        /// <summary>Shows "Over matrix +X" next to the crate total of dispatches that went over with a reason.</summary>
        private void FormatOverCapacityBadge(DataGridViewCellFormattingEventArgs e, string propName)
        {
            if (propName != "TotalCrates" || overCapacityMap.Count == 0) return;
            if (gridDispatch.DataSource is not IList<Delivery> list || e.RowIndex >= list.Count) return;
            if (!overCapacityMap.TryGetValue(list[e.RowIndex].Id, out int over) || over <= 0) return;
            e.Value = $"{list[e.RowIndex].TotalCrates:N0}  ·  Over matrix +{over}";
            e.FormattingApplied = true;
            e.CellStyle.ForeColor = Pal.C(180, 83, 9);
            e.CellStyle.BackColor = Pal.C(255, 251, 235);
        }
    }
}
