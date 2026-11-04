using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>Truck category and capacity are edited in the truck form itself: pick or type a category,
    /// the capacity fills in from it, and a new category (or a new default capacity) is saved with the truck.</summary>
    public partial class TruckManagementView
    {
        private Label? lblCatHint;
        private CheckBox? chkSaveCatDefault;
        private LinkLabel? lnkManageCats;
        private Panel? pnlCapRow;
        private void InitInlineCategories()
        {
            // The separate Categories page is folded into the truck form.
            pnlSubNavTabs.Visible = false;
            if (pnlCategoriesContainer != null) pnlCategoriesContainer.Visible = false;
            // Categories are picked from the list; new ones are added with "+ Add truck category" (with their capacities).
            cbType.DropDownStyle = ComboBoxStyle.DropDownList;
            lblType.Text = "Truck category *:";
            lblCap.Text = "Crate capacity (locked):";
            lblCatHint = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 40, Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, UseMnemonic = false, Margin = new Padding(0, 0, 0, 2) };
            chkSaveCatDefault = new CheckBox { Dock = DockStyle.Top, AutoSize = false, Height = 0, Visible = false };
            lnkManageCats = new LinkLabel { Visible = false };
            // Capacity is locked: it comes from the category's matrix and the truck's cargo type.
            numCapacity.ReadOnly = true;
            numCapacity.Increment = 0;
            numCapacity.InterceptArrowKeys = false;
            numCapacity.TabStop = false;
            numCapacity.MouseWheel += (s, e) => { if (e is HandledMouseEventArgs h) h.Handled = true; };
            numCapacity.Minimum = 0;
            if (numCapacity.Controls.Count > 0) numCapacity.Controls[0].Visible = false; // no up/down arrows on a locked number
            // Category buttons sit beside the capacity box.
            pnlCapRow = new Panel { Dock = DockStyle.Top, Height = 40, Margin = new Padding(0, 0, 0, 6) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Padding = new Padding(8, 0, 0, 0) };
            Button B(string t, string role, Action a)
            {
                var b = new Button { Text = t, AutoSize = true, MinimumSize = new Size(0, 32), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 6, 0), Padding = new Padding(6, 0, 6, 0), Font = new Font("Segoe UI Semibold", 9F), UseMnemonic = false };
                b.FlatAppearance.BorderSize = 0;
                UIThemeHelper.SetButtonRole(b, role);
                b.Click += (s, e) => a();
                b.Visible = SessionContext.IsAdmin;
                return b;
            }
            flow.Controls.Add(B("✏  Rename / remove category", "secondary", () => OpenCategoryManager(false)));
            flow.Controls.Add(B("＋ Add truck category", "secondary", () => OpenCategoryManager(true)));
            numCapacity.Parent?.Controls.Remove(numCapacity);
            numCapacity.Dock = DockStyle.Fill;
            numCapacity.Margin = Padding.Empty;
            pnlCapRow.Controls.Add(numCapacity);
            pnlCapRow.Controls.Add(flow);
            cbType.SelectedIndexChanged += (s, e) => UpdateCategoryHint();
            cbCargoType.SelectedIndexChanged += (s, e) => UpdateCategoryHint();
        }
        private TruckCategoryItem? MatchCategory(string? name)
        {
            string n = (name ?? "").Trim();
            if (n.Length == 0) return null;
            if (allTruckCategories.Count == 0) try { allTruckCategories = MJdatabase.GetTruckCategories(); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
            return allTruckCategories.FirstOrDefault(c => string.Equals(c.CategoryName.Trim(), n, StringComparison.OrdinalIgnoreCase));
        }
        private static string CleanCategoryName(string? s) => System.Text.RegularExpressions.Regex.Replace((s ?? "").Trim(), @"\s+", " ");
        /// <summary>Shows the category's matrix and sets the locked capacity for the chosen cargo type.</summary>
        private void UpdateCategoryHint()
        {
            if (lblCatHint == null) return;
            string name = CleanCategoryName(cbType.Text);
            if (name.Length == 0)
            {
                lblCatHint.Text = "Choose a category — its capacity fills in for the cargo type.";
                lblCatHint.ForeColor = AppTheme.TextMuted;
                return;
            }
            TruckCapacity? m = null;
            try { m = MJdatabase.GetCapacityFor(name); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            string cargo = MJdatabase.NormalizeCargo(cbCargoType.SelectedItem?.ToString());
            if (m == null)
            {
                lblCatHint.Text = $"{name} has no capacities yet. Click \"Rename / remove category\" to set its Dry, Cold and Combi crates.";
                lblCatHint.ForeColor = AppTheme.WarningText;
                return;
            }
            int cap = MJdatabase.LockedCapacity(name, cargo, (int)numCapacity.Value);
            bool was = isBindingForm;
            isBindingForm = true;
            try { numCapacity.Value = Math.Min(numCapacity.Maximum, Math.Max(numCapacity.Minimum, cap)); } finally { isBindingForm = was; }
            string combi = m.CombiMax == 0 ? "Combi not set" : $"Combi {m.CombiColdMax} cold + {m.CombiDryMax} dry";
            lblCatHint.Text = $"{m.CategoryName}: Dry {m.DryMax} · Cold {m.ColdMax} · {combi}. This truck ({cargo}) carries {cap:N0} crates.";
            lblCatHint.ForeColor = AppTheme.TextMuted;
        }
        /// <summary>Called on Save/Update: the category must exist (new ones are added with "+ Add truck category").</summary>
        private string? CommitCategoryFromForm(int capacity)
        {
            string name = CleanCategoryName(cbType.Text);
            if (name.Length == 0) return null;
            var cat = MatchCategory(name);
            if (cat == null)
            {
                MessageBox.Show(FindForm(), $"\"{name}\" is not a truck category yet. Click \"+ Add truck category\" to add it with its capacities first.", "Truck category", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbType.Focus();
                return null;
            }
            return cat.CategoryName;
        }
        /// <summary>Asks for the reason when a truck goes to maintenance or is decommissioned, and confirms reactivation.</summary>
        private bool AskStatusReason(Truck t, string oldStatus, string newStatus, out string reason)
        {
            reason = "";
            var owner = FindForm();
            bool toMaint = RowMenu.Is(newStatus, "Under Maintenance", "Maintenance", "Under Repair");
            bool toDecom = MJdatabase.IsDecommissioned(newStatus);
            bool fromDecom = MJdatabase.IsDecommissioned(oldStatus) && !toDecom;
            if (toMaint || toDecom)
            {
                string? r = Forms.ReasonPrompt.Ask(owner, toDecom ? "Decommission truck" : "Truck under maintenance",
                    toDecom ? $"Why is {t.PlateNumber} being decommissioned? It will no longer be offered for trips (it can be reactivated later)."
                            : $"What maintenance is {t.PlateNumber} getting? It can't be dispatched until it is set back to Active.", required: true, okText: toDecom ? "Decommission" : "Save");
                if (r == null) return false;
                reason = r;
                return true;
            }
            if (fromDecom)
            {
                if (MessageBox.Show(owner, $"Are you sure you want to reactivate {t.PlateNumber}?\n\nIt will be offered for trips again. Its decommission history is kept in the truck profile.", "Reactivate truck", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return false;
                reason = Forms.ReasonPrompt.Ask(owner, "Reactivate truck", $"Note for reactivating {t.PlateNumber} (optional).", required: false, okText: "Reactivate") ?? "Reactivated";
                if (reason.Length == 0) reason = "Reactivated";
            }
            return true;
        }
        private void ResetCategoryHint()
        {
            if (chkSaveCatDefault != null) chkSaveCatDefault.Checked = false;
            try { allTruckCategories = MJdatabase.GetTruckCategories(); } catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
            UpdateCategoryHint();
        }
        /// <summary>Truck categories and their Dry / Cold / Combi capacities, without leaving the truck form.</summary>
        private void OpenCategoryManager(bool startWithNew)
        {
            using var dlg = new Forms.TruckCategoryMatrixDialog(startWithNew);
            dlg.ShowDialog(FindForm());
            if (!dlg.Changed) return;
            string typed = cbType.Text;
            LoadTrucks();
            BeginInvoke(new Action(() =>
            {
                int i = cbType.FindStringExact(typed);
                if (i >= 0) cbType.SelectedIndex = i;
                ResetCategoryHint();
            }));
        }
    }
}
