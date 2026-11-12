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
        private Button? btnUseSuggested;
        private TableLayoutPanel? tblSuggest;
        private FlowLayoutPanel? pnlUseRow;
        private Label? lblSuggestKicker, lblSuggestMain, lblSuggestCap, lblSuggestHint;
        private string suggestFallbackText = "";
        private bool suggestBusy;
        private int suggestedTruckId;






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

            // The "SUGGESTED TRUCK" footer: small kicker, a bold "{tonner} · {plate}" with the capacity after it, and a real button on the right.
            Color muted = AppTheme.TextSecondary;
            lblSuggestKicker = new Label { Text = "SUGGESTED TRUCK", AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = muted, Margin = new Padding(0, 0, 0, Dpi(1)), UseMnemonic = false };
            lblSuggestMain = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold), Margin = new Padding(0, 0, Dpi(10), 0), UseMnemonic = false };
            lblSuggestCap = new Label { AutoSize = true, Font = new Font("Segoe UI", 10.5F), Margin = new Padding(0, Dpi(3), 0, 0), UseMnemonic = false };
            lblSuggestHint = new Label { AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = muted, Margin = new Padding(0, Dpi(2), 0, 0), UseMnemonic = false, Visible = false };
            var flowMain = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0), Dock = DockStyle.Fill };
            flowMain.Controls.Add(lblSuggestMain);
            flowMain.Controls.Add(lblSuggestCap);
            tblSuggest = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 3, BackColor = Color.Transparent, Margin = new Padding(0), Visible = false };
            tblSuggest.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblSuggest.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            for (int i = 0; i < 3; i++) tblSuggest.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tblSuggest.Controls.Add(lblSuggestKicker, 0, 0);
            tblSuggest.Controls.Add(flowMain, 0, 1);
            tblSuggest.Controls.Add(lblSuggestHint, 0, 2);
            tblSuggest.SetColumnSpan(lblSuggestHint, 2);

            // "Use this truck" / "Use it": the user's own click selects the suggested truck (nothing is ever selected for them).
            btnUseSuggested = new Button
            {
                Text = "Use this truck", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false,
                BackColor = AppTheme.Accent, ForeColor = AppTheme.OnAccent, Font = new Font("Segoe UI Semibold", 9.5F), Cursor = Cursors.Hand,
                Margin = new Padding(Dpi(10), Dpi(2), 0, Dpi(2)), Padding = new Padding(Dpi(8), Dpi(2), Dpi(8), Dpi(2)), TabStop = true, Visible = false, Anchor = AnchorStyles.Right
            };
            btnUseSuggested.FlatAppearance.BorderSize = 0;
            btnUseSuggested.FlatAppearance.MouseOverBackColor = AppTheme.AccentHover;
            btnUseSuggested.Click += (s, e) => UseSuggestedTruck();
            tblSuggest.Controls.Add(btnUseSuggested, 1, 0);
            tblSuggest.SetRowSpan(btnUseSuggested, 2);
            pnlUseRow = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0), Visible = false };
            pnlRec.Controls.Add(pnlUseRow);
            pnlRec.Controls.Add(tblSuggest);
            lblRec.TextChanged += LblRec_TextChangedForSuggest;
        }

        /// <summary>Someone else wrote a plain message into the footer: show it, unless it only appended a note to the suggestion text (the date hint).</summary>
        private void LblRec_TextChangedForSuggest(object? sender, EventArgs e)
        {
            if (suggestBusy || tblSuggest == null || !tblSuggest.Visible) return;
            if (suggestFallbackText.Length > 0 && lblRec.Text.StartsWith(suggestFallbackText, StringComparison.Ordinal) && lblRec.Text.Length > suggestFallbackText.Length)
            {
                if (lblSuggestHint != null) { lblSuggestHint.Text = lblRec.Text.Substring(suggestFallbackText.Length).TrimStart(' ', '|'); lblSuggestHint.Visible = true; }
                return;
            }
            HideSuggestPanel();
        }

        private void HideSuggestPanel()
        {
            if (tblSuggest != null) tblSuggest.Visible = false;
            suggestFallbackText = "";
            lblRec.Visible = true;
        }

        /// <summary>"6-tonner" from "FORWARD 6 TONS"; a truck type with no tonnage stays as typed.</summary>
        private static string TonnerOf(Truck t) => CapacityValidator.TonnerOf(t);

        /// <summary>The smallest free truck that fits the crates, sized for the form's current Temp Type (same check as the capacity status).</summary>
        private TruckRecommendation SuggestTruckForForm(int total)
        {
            string cargo = cbTempType != null && cbTempType.SelectedIndex >= 0 ? CargoOfTempType(cbTempType.SelectedItem as string) : "";
            var (cold, dry) = CurrentCrateSplit(total);
            return MJdatabase.GetTruckRecommendation(total, dtpDispatchDate.Value, cargo, cold, dry);
        }

        /// <summary>Shows (or hides, when truck is null) the "use it" button. beside = inside the "SUGGESTED TRUCK" footer, else on its own row under the status line.</summary>
        private void ShowUseSuggested(Truck? truck, string text, bool beside = false)
        {
            suggestedTruckId = truck?.Id ?? 0;
            if (btnUseSuggested == null || pnlUseRow == null || tblSuggest == null) return;
            btnUseSuggested.Visible = false;
            if (truck == null) { pnlUseRow.Visible = false; HideSuggestPanel(); return; }
            btnUseSuggested.Text = text;
            if (beside)
            {
                if (btnUseSuggested.Parent != tblSuggest) { tblSuggest.Controls.Add(btnUseSuggested, 1, 0); tblSuggest.SetRowSpan(btnUseSuggested, 2); }
                btnUseSuggested.Anchor = AnchorStyles.Right;
                pnlUseRow.Visible = false;
            }
            else
            {
                if (btnUseSuggested.Parent != pnlUseRow) pnlUseRow.Controls.Add(btnUseSuggested);
                btnUseSuggested.Anchor = AnchorStyles.Left;
                pnlUseRow.Visible = true;
            }
            btnUseSuggested.Visible = true;
        }

        /// <summary>No truck picked and one is suggested: the "SUGGESTED TRUCK" footer with the larger line and the button.</summary>
        private void ShowSuggestedTruckFooter(Truck truck, int total, Color back, Color fore)
        {
            if (tblSuggest == null || lblSuggestMain == null || lblSuggestCap == null || lblSuggestHint == null) return;
            var r = ValidateLoad(truck, total);
            int limit = r.Limit > 0 ? r.Limit : CapacityValidator.MatrixFor(truck).TruckMax;
            string main = $"{TonnerOf(truck)}  ·  {truck.PlateNumber}";
            string cap = limit > 0 ? $"fits {total:N0} of {limit:N0} crates ({Math.Round(total * 100.0 / limit):0}%)" : $"fits {total:N0} crates";
            suggestBusy = true;
            try
            {
                lblRec.Text = $"ℹ️ Total {total:N0} crates  ·  suggested truck {main}  ·  {cap}";
                suggestFallbackText = lblRec.Text;
            }
            finally { suggestBusy = false; }
            pnlRec.BackColor = back;
            lblSuggestMain.Text = main; lblSuggestCap.Text = cap;
            lblSuggestMain.ForeColor = fore; lblSuggestCap.ForeColor = fore;
            lblSuggestHint.Visible = false; lblSuggestHint.Text = "";
            ShowUseSuggested(truck, "Use this truck", beside: true);
            tblSuggest.Visible = true;
            lblRec.Visible = false;
        }

        private void UseSuggestedTruck()
        {
            if (suggestedTruckId <= 0) return;
            int idx = trucks.FindIndex(t => t.Id == suggestedTruckId);
            if (idx >= 0 && idx < cbTruck.Items.Count) cbTruck.SelectedIndex = idx; // the normal selection logic (driver suggestion, footer) runs
        }






        /// <summary>Nov 6 workflow: there is no cold / dry split on the dispatch any more (Combi is a note in the remarks).</summary>
        /// <summary>No typed cold / dry split any more: for a Combi load G crates are dry and E crates are cold (the category sets the limits).</summary>
        private bool IsCombiCargo => false;
        private bool IsCombiTemp => cbTempType != null && cbTempType.SelectedIndex == 2;






        /// <summary>Cold and dry crates of the load: Dry Goods = all dry, Cold = all cold, Combi = the split typed in.</summary>
        private (int cold, int dry) CurrentCrateSplit(int total)
        {
            if (IsCombiTemp) return ((int)numECrate.Value, (int)numGCrate.Value); // G crates = dry, E crates = cold
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
            lblRec.Text = $"{icon} {TonnerOf(selTruck)}  ·  {selTruck.PlateNumber}  ·  {total:N0} / {r.Limit:N0} crates" + (r.Limit > 0 ? $" ({Math.Round(total * 100.0 / r.Limit):0}% full)" : "") + (r.State == CapacityState.Allow ? "" : $"  ·  {r.Title}");
            if (lnkCapDetails != null) lnkCapDetails.Visible = true;
            // Soft note only: a smaller free truck would also fit. Never blocks, never asks for a reason.
            if (r.State == CapacityState.Allow && Features.On(Features.TruckSuggestion))
            {
                try
                {
                    var rec = SuggestTruckForForm(total);
                    var alt = rec.RecommendedTruck;
                    if (alt != null && !rec.IsMultiTruckNeeded && alt.Id != selTruck.Id)
                    {
                        var ar = ValidateLoad(alt, total);
                        if (ar.State == CapacityState.Allow && ar.Limit > 0 && ar.Limit < r.Limit)
                        {
                            lblRec.Text += $"\nA smaller truck ({TonnerOf(alt)}  ·  {alt.PlateNumber}) is free and fits {total:N0} crates.";
                            ShowUseSuggested(alt, "Use it");
                        }
                    }
                }
                catch (Exception ex) { ErrLog.Swallowed(ex); }
            }
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
