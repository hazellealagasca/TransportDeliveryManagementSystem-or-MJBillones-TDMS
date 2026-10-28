using System;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Regular dispatches and loading orders are crate deliveries only. Anything that is not counted in crates
    /// (boxes, bulk, equipment, oversized or fragile items, other specialised cargo) is booked as a Special Trip.
    /// </summary>
    public static class CargoRouting
    {
        public const string SpecialOption = "Non-crate / specialized cargo → Special Trip";
        /// <summary>Crate cargo types a regular dispatch or loading order can carry.</summary>
        public static readonly string[] CrateCargoTypes = { "Cold / Chilled", "Dry Goods", "Dry and Chilled Mix" };
        private static readonly string[] SpecialWords = { "non-crate", "noncrate", "special", "bulk", "equipment", "oversize", "fragile", "furniture", "machinery", "appliance", "box", "pallet", "loose" };
        public static bool IsSpecial(string? cargoType)
        {
            string t = (cargoType ?? "").Trim().ToLowerInvariant();
            if (t.Length == 0) return false;
            if (t == SpecialOption.ToLowerInvariant()) return true;
            return SpecialWords.Any(w => t.Contains(w));
        }
        /// <summary>Fills a cargo combo with the crate types plus the Special Trip option, keeping the current text.</summary>
        public static void FillCargoCombo(ComboBox cb)
        {
            string keep = cb.Text;
            cb.Items.Clear();
            cb.Items.AddRange(CrateCargoTypes.Cast<object>().ToArray());
            cb.Items.Add(SpecialOption);
            if (!string.IsNullOrEmpty(keep) && !IsSpecial(keep))
            {
                int i = cb.FindStringExact(keep);
                if (i >= 0) cb.SelectedIndex = i; else cb.Text = keep;
            }
        }
        /// <summary>
        /// Explains that non-crate cargo goes under Special Trips and, if the person agrees, opens a new Special Trip
        /// pre-filled with the client and date. Returns true when the Special Trip form was opened.
        /// </summary>
        public static bool OfferSpecialTrip(IWin32Window? owner, string? company, DateTime date, string? note = null)
        {
            var dr = MessageBox.Show(owner,
                "Regular dispatches and loading orders are for crate deliveries only.\n\n" +
                "Non-crate deliveries and specialized cargo (boxes, bulk, equipment, oversized or fragile items) must be booked as a Special Trip / Special Delivery.\n\n" +
                "Open a new Special Trip now?",
                "Book as a Special Trip", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
            if (dr != DialogResult.Yes) return false;
            var mf = MainForm.Instance;
            if (mf == null) return false;
            mf.NavigateToOperations("SpecialTrips");
            var view = mf.GetOperationsView<SpecialTripHistoryView>("SpecialTrips");
            if (view == null) return false;
            view.OpenSpecialTripDialog(0, dlg => dlg.Prefill(company, date, "Non-Crate / Specialized Cargo", note));
            return true;
        }
    }
}
