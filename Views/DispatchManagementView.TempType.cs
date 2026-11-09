using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// "Truck &amp; crew" card: the required Temp Type (Dry / Cold / Combi) that decides which of the truck's capacity limits the crates are
    /// checked against, and the footer at the bottom of the order form that shows the live crate count against the truck's limit.
    /// The choice is kept in the (hidden) crate-type box <c>cbCargoType</c>, which is what is saved with the order.
    /// </summary>
    public partial class DispatchManagementView
    {
        private ComboBox? cbTempType;
        private Label? lblTempType;
        private Panel? pnlCrateFooter;
        private Label? lblCrateFooter;
        private bool syncingTempType;

        private static readonly string[] TempTypes = { "Dry", "Cold", "Combi" };

        private static string CargoOfTempType(string? temp) => temp switch
        {
            "Dry" => MJdatabase.CargoDry,
            "Cold" => MJdatabase.CargoCold,
            "Combi" => MJdatabase.CargoCombi,
            _ => ""
        };

        private static int TempTypeIndexOfCargo(string? cargo)
        {
            if (string.IsNullOrWhiteSpace(cargo)) return -1;
            return MJdatabase.NormalizeCargo(cargo) switch
            {
                MJdatabase.CargoDry => 0,
                MJdatabase.CargoCold => 1,
                MJdatabase.CargoCombi => 2,
                _ => -1
            };
        }

        /// <summary>The row with the required Temp Type box, placed first in the "Truck &amp; crew" card.</summary>
        private Control BuildTempTypeRow()
        {
            if (cbTempType == null)
            {
                lblTempType = new Label { Text = "Temp Type *", AutoSize = true, UseMnemonic = false };
                cbTempType = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 9F),
                    Dock = DockStyle.Fill,
                    FormattingEnabled = true,
                    BackColor = cbTruck.BackColor,
                    ForeColor = cbTruck.ForeColor,
                    FlatStyle = cbTruck.FlatStyle,
                    Name = "cbTempType"
                };
                cbTempType.Items.AddRange(TempTypes);
                cbTempType.SelectedIndexChanged += (s, e) =>
                {
                    if (syncingTempType) return;
                    ValidationHelper.ApplyValidationState(cbTempType, true, "");
                    if (isBindingForm) return;
                    syncingTempType = true;
                    try { cbCargoType.Text = CargoOfTempType(cbTempType.SelectedItem as string); }
                    finally { syncingTempType = false; }
                    CalculateCratesAndRecommendation();
                };
                // A saved order, a draft or Clear form sets the (hidden) crate type: the Temp Type follows it.
                cbCargoType.TextChanged += (s, e) =>
                {
                    if (syncingTempType || cbTempType == null) return;
                    syncingTempType = true;
                    try { cbTempType.SelectedIndex = TempTypeIndexOfCargo(cbCargoType.Text); }
                    finally { syncingTempType = false; }
                    UpdateCombiSplitUi((int)(numGCrate.Value + numECrate.Value));
                    if (pnlRowCombi != null) pnlRowCombi.Visible = IsCombiCargo;
                    UpdateCrateFooter();
                };
                cbTruck.SelectedIndexChanged += (s, e) => UpdateCrateFooter();
                numGCrate.ValueChanged += (s, e) => UpdateCrateFooter();
                numECrate.ValueChanged += (s, e) => UpdateCrateFooter();
                cbTempType.SelectedIndexChanged += (s, e) => { if (pnlRowCombi != null) pnlRowCombi.Visible = IsCombiCargo; };
                storeTips.SetToolTip(cbTempType, "Dry, Cold or Combi (cold + dry). The crates are checked against that part of the truck's capacity.");
            }
            return CreateTwoColumnRow(lblTempType!, cbTempType, new Label { Visible = false }, new Panel { Visible = false });
        }

        /// <summary>A clear footer at the bottom of the order form: the live crate count and the truck's limit.</summary>
        private void BuildCrateFooter()
        {
            if (pnlCrateFooter != null) return;
            lblCrateFooter = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Semibold", 12.5F, FontStyle.Bold),
                UseMnemonic = false,
                AutoEllipsis = true
            };
            pnlCrateFooter = new Panel { Dock = DockStyle.Bottom, Height = Dpi(50), Padding = new Padding(Dpi(12), 0, Dpi(12), 0) };
            pnlCrateFooter.Controls.Add(lblCrateFooter);
            pnlForm.Controls.Add(pnlCrateFooter);
            pnlForm.Controls.SetChildIndex(pnlCrateFooter, 1); // just above the Save / Cancel bar
            UpdateCrateFooter();
        }

        /// <summary>The crate check for the form: by Temp Type when one is chosen (Dry / Cold / Combi limits), else the truck's overall maximum.</summary>
        private CapacityResult ValidateLoad(Truck truck, int total)
        {
            if (cbTempType == null || cbTempType.SelectedIndex < 0 || string.IsNullOrWhiteSpace(cbCargoType.Text))
                return CapacityValidator.ValidateTotal(truck, total);
            var (cold, dry) = CurrentCrateSplit(total);
            // Combi: G = dry, E = cold. A load with only one of them is simply that kind of load (no "needs both parts").
            if (IsCombiTemp && (cold == 0 || dry == 0))
                return CapacityValidator.Validate(truck, cold == 0 ? MJdatabase.CargoDry : MJdatabase.CargoCold, cold, dry);
            return CapacityValidator.Validate(truck, cbCargoType.Text, cold, dry);
        }

        private void UpdateCrateFooter()
        {
            if (lblCrateFooter == null || pnlCrateFooter == null) return;
            int g = (int)numGCrate.Value, e = (int)numECrate.Value, total = g + e;
            string text = $"Crates:  G crates {g:N0}  +  E crates {e:N0}  =  {total:N0} total crates";
            Color back = Pal.C(241, 245, 249), fore = Pal.C(30, 41, 59);
            var truck = SelectedTruckOrNull();
            if (truck != null && total > 0)
            {
                var r = ValidateLoad(truck, total);
                string zone = cbTempType != null && cbTempType.SelectedIndex >= 0 ? $" ({cbTempType.SelectedItem} limit)" : "";
                if (r.Limit > 0) text += $"   ·   Truck max {r.Limit:N0}{zone}";
                switch (r.State)
                {
                    case CapacityState.Allow: back = Pal.C(220, 252, 231); fore = Pal.C(22, 101, 52); break;
                    case CapacityState.ExceedWithReason: back = Pal.C(254, 243, 199); fore = Pal.C(146, 64, 14); text += $"   ⚠ over by {r.Over:N0}"; break;
                    case CapacityState.BlockPure:
                    case CapacityState.BlockCombi: back = Pal.C(254, 226, 226); fore = Pal.C(153, 27, 27); text += $"   ⚠ over by {r.Over:N0}"; break;
                    default: back = Pal.C(254, 226, 226); fore = Pal.C(153, 27, 27); text += $"   ⛔ {r.Title}"; break;
                }
            }
            else if (truck != null && cbTempType != null && cbTempType.SelectedIndex >= 0)
            {
                var m = CapacityValidator.MatrixFor(truck);
                int lim = cbTempType.SelectedIndex == 0 ? m.DryMax : cbTempType.SelectedIndex == 1 ? m.ColdMax : (m.CombiMax > 0 ? m.CombiMax : m.TruckMax);
                text += $"   ·   Truck max {lim:N0} ({cbTempType.SelectedItem} limit)";
            }
            pnlCrateFooter.BackColor = back;
            lblCrateFooter.BackColor = back;
            lblCrateFooter.ForeColor = fore;
            lblCrateFooter.Text = text;
        }
    }
}
