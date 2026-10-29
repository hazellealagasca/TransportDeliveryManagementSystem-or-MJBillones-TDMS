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
        private void InitInlineCategories()
        {
            // The separate Categories page is folded into the truck form.
            pnlSubNavTabs.Visible = false;
            if (pnlCategoriesContainer != null) pnlCategoriesContainer.Visible = false;
            cbType.DropDownStyle = ComboBoxStyle.DropDown;
            cbType.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cbType.AutoCompleteSource = AutoCompleteSource.ListItems;
            cbType.MaxLength = 60;
            lblType.Text = "Truck category * (pick one or type a new one):";
            lblCatHint = new Label { Dock = DockStyle.Top, AutoSize = false, Height = 36, Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, UseMnemonic = false, Margin = new Padding(0, 0, 0, 2) };
            chkSaveCatDefault = new CheckBox { Dock = DockStyle.Top, AutoSize = false, Height = 26, Font = new Font("Segoe UI", 9F), Visible = false, Margin = new Padding(0, 0, 0, 6) };
            chkSaveCatDefault.CheckedChanged += (s, e) => UpdateCategoryHint();
            lnkManageCats = new LinkLabel { Dock = DockStyle.Top, AutoSize = false, Height = 24, Text = "Rename or remove categories…", Font = new Font("Segoe UI", 9F), LinkColor = AppTheme.Accent, ActiveLinkColor = AppTheme.AccentHover, Visible = SessionContext.IsAdmin };
            lnkManageCats.LinkClicked += (s, e) => OpenCategoryManager();
            cbType.TextChanged += (s, e) => { if (!isBindingForm) UpdateCategoryHint(); };
            cbType.SelectedIndexChanged += (s, e) => UpdateCategoryHint();
            cbType.Leave += (s, e) =>
            {
                // Use the existing spelling when the typed name matches a category.
                var match = MatchCategory(cbType.Text);
                if (match == null) return;
                if (match.CategoryName != cbType.Text.Trim()) cbType.Text = match.CategoryName;
                if ((selectedTruck == null || selectedTruck.Id == 0) && match.CrateCapacity > 0) numCapacity.Value = Math.Min(numCapacity.Maximum, match.CrateCapacity);
            };
            numCapacity.ValueChanged += (s, e) => UpdateCategoryHint();
            InputGuards.DigitsOnly(numCapacity);
        }
        private TruckCategoryItem? MatchCategory(string? name)
        {
            string n = (name ?? "").Trim();
            if (n.Length == 0) return null;
            if (allTruckCategories.Count == 0) try { allTruckCategories = MJdatabase.GetTruckCategories(); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
            return allTruckCategories.FirstOrDefault(c => string.Equals(c.CategoryName.Trim(), n, StringComparison.OrdinalIgnoreCase));
        }
        private static string CleanCategoryName(string? s) => System.Text.RegularExpressions.Regex.Replace((s ?? "").Trim(), @"\s+", " ");
        private void UpdateCategoryHint()
        {
            if (lblCatHint == null || chkSaveCatDefault == null) return;
            string name = CleanCategoryName(cbType.Text);
            int cap = (int)numCapacity.Value;
            if (name.Length == 0)
            {
                lblCatHint.Text = "Choose a category to fill in its usual capacity.";
                lblCatHint.ForeColor = AppTheme.TextMuted;
                chkSaveCatDefault.Visible = false;
                return;
            }
            var cat = MatchCategory(name);
            if (cat == null)
            {
                lblCatHint.Text = $"\"{name}\" is a new category. It will be added with {cap:N0} crates as its usual capacity when you save.";
                lblCatHint.ForeColor = AppTheme.InfoText;
                chkSaveCatDefault.Visible = false;
                return;
            }
            int trucks = cat.AssignedTrucksCount;
            if (cat.CrateCapacity > 0 && cat.CrateCapacity != cap)
            {
                lblCatHint.Text = $"{cat.CategoryName} usually carries {cat.CrateCapacity:N0} crates ({trucks} truck{(trucks == 1 ? "" : "s")}). This truck will be saved with {cap:N0}.";
                lblCatHint.ForeColor = AppTheme.WarningText;
                chkSaveCatDefault.Text = $"Also make {cap:N0} crates the usual capacity for {cat.CategoryName}";
                chkSaveCatDefault.Visible = SessionContext.IsAdmin;
            }
            else
            {
                lblCatHint.Text = $"{cat.CategoryName}: usual capacity {cat.CrateCapacity:N0} crates · {trucks} truck{(trucks == 1 ? "" : "s")} in this category.";
                lblCatHint.ForeColor = AppTheme.TextMuted;
                chkSaveCatDefault.Visible = false;
                chkSaveCatDefault.Checked = false;
            }
        }
        /// <summary>Called on Save/Update: adds a new category, or updates its usual capacity when asked. Returns the name to store on the truck.</summary>
        private string? CommitCategoryFromForm(int capacity)
        {
            string name = CleanCategoryName(cbType.Text);
            if (name.Length == 0) return null;
            if (name.Length < 2 || !name.Any(char.IsLetterOrDigit))
            {
                MessageBox.Show(FindForm(), "Enter a proper truck category name, for example \"FORWARD 6 TONS\".", "Truck category", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbType.Focus();
                return null;
            }
            var cat = MatchCategory(name);
            if (cat == null)
            {
                if (!MJdatabase.SaveTruckCategory(name, capacity)) throw new InvalidOperationException($"The new category \"{name}\" couldn't be saved.");
                MJdatabase.NotifyCategoriesChanged();
                return name;
            }
            if (chkSaveCatDefault != null && chkSaveCatDefault.Visible && chkSaveCatDefault.Checked && cat.CrateCapacity != capacity)
            {
                MJdatabase.SaveTruckCategory(cat.CategoryName, capacity, cat.CategoryName);
                MJdatabase.NotifyCategoriesChanged();
            }
            return cat.CategoryName;
        }
        private void ResetCategoryHint()
        {
            if (chkSaveCatDefault != null) chkSaveCatDefault.Checked = false;
            try { allTruckCategories = MJdatabase.GetTruckCategories(); } catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
            UpdateCategoryHint();
        }
        /// <summary>Rename or remove categories without leaving the truck form.</summary>
        private void OpenCategoryManager()
        {
            if (pnlCategoriesContainer == null) return;
            var owner = FindForm();
            using var dlg = new Form
            {
                Text = "Truck categories & capacities",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(PageKit.D(this, 980), PageKit.D(this, 620)),
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = AppTheme.Canvas,
                Font = Font
            };
            var oldParent = pnlCategoriesContainer.Parent;
            var oldDock = pnlCategoriesContainer.Dock;
            pnlCategoriesContainer.Parent = dlg;
            pnlCategoriesContainer.Dock = DockStyle.Fill;
            pnlCategoriesContainer.Visible = true;
            LoadTruckCategories();
            try { UIThemeHelper.ApplyTheme(dlg); } catch (System.Exception swx3) { ErrLog.Swallowed(swx3); }
            try { dlg.ShowDialog(owner); }
            finally
            {
                pnlCategoriesContainer.Visible = false;
                pnlCategoriesContainer.Parent = oldParent;
                pnlCategoriesContainer.Dock = oldDock;
                string typed = cbType.Text;
                LoadTrucks();
                cbType.Text = typed;
                ResetCategoryHint();
            }
        }
    }
}
