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
    /// <summary>
    /// Crew classification: Driver vs Delivery Aide / Helper, and the driver's license recorded as a class
    /// (Professional / Non-Professional / None) with its issue and expiry dates. The license number is not stored.
    /// Pay and availability are not edited here: pay is set per trip after it is completed, and availability follows the trips.
    /// </summary>
    public partial class CrewManagementView
    {
        public const string RoleDriver = "Driver";
        public const string RoleAide = "Delivery Aide / Helper";
        public const string LicPro = "Professional Driver's License";
        public const string LicNonPro = "Non-Professional Driver's License";
        public const string LicNone = "None";
        private Panel pnlEmployment = null!;
        private Label lblRoleRequirements = null!;
        private Panel pnlLicense = null!;
        private ComboBox cbLicenseClass = null!;
        private DateTimePicker dtpLicenseIssued = null!;
        private DateTimePicker dtpLicenseExpiry = null!;
        private ComboBox cbLicenseValidity = null!;
        private Label lblLicenseStatus = null!;
        private bool settingLicense;
        private int Dp(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        /// <summary>Builds the employment and license sections (called before the full-page form is assembled).</summary>
        private void InitClassification()
        {
            // Employment type first: seasonal / temporary
            pnlEmployment = new Panel { Height = Dp(64), BackColor = AppTheme.Surface, Margin = new Padding(0, 0, 0, Dp(6)) };
            chkIsSeasonal.Parent?.Controls.Remove(chkIsSeasonal);
            chkIsSeasonal.Text = "Seasonal / temporary crew member";
            chkIsSeasonal.AutoSize = true;
            chkIsSeasonal.Dock = DockStyle.Top;
            chkIsSeasonal.Font = new Font("Segoe UI Semibold", 10F);
            var hint = new Label { Text = "Tick for crew hired only for peak season or as a temporary replacement. Leave unticked for regular crew.", Dock = DockStyle.Top, Height = Dp(36), Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, Padding = new Padding(Dp(20), 0, 0, 0) };
            pnlEmployment.Controls.Add(hint);
            pnlEmployment.Controls.Add(chkIsSeasonal);
            // Pay and availability are not part of this form any more.
            chkAvailable.Visible = false;
            if (tblCheckboxes != null) tblCheckboxes.Visible = false;
            lblLicense.Visible = false;
            txtLicense.Visible = false;
            // Role: exactly two classifications
            cbRole.DropDownStyle = ComboBoxStyle.DropDownList;
            lblRoleRequirements = new Label { Height = Dp(38), Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, AutoEllipsis = false, Margin = new Padding(0, 0, 0, Dp(4)) };
            cbRole.SelectedIndexChanged += (s, e) => UpdateRoleRequirements();
            // License
            pnlLicense = new Panel { Height = Dp(170), BackColor = AppTheme.Surface };
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 5, BackColor = AppTheme.Surface };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, Dp(24)));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, Dp(36)));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, Dp(24)));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, Dp(36)));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Label L(string t) => new Label { Text = t, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary };
            cbLicenseClass = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 0, Dp(12), 0) };
            cbLicenseClass.Items.AddRange(new object[] { LicPro, LicNonPro, LicNone });
            cbLicenseClass.SelectedIndex = 2;
            cbLicenseValidity = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 0, Dp(12), 0) };
            cbLicenseValidity.Items.AddRange(new object[] { "5 years", "10 years", "Other (set expiry)" });
            cbLicenseValidity.SelectedIndex = 0;
            dtpLicenseIssued = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Custom, CustomFormat = "MMM d, yyyy", Font = new Font("Segoe UI", 10F), MaxDate = DateTime.Today, Margin = new Padding(0, 0, Dp(12), 0) };
            dtpLicenseExpiry = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Custom, CustomFormat = "MMM d, yyyy", Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 0, Dp(12), 0) };
            lblLicenseStatus = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextMuted, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, Dp(4), 0, 0) };
            tbl.Controls.Add(L("License classification"), 0, 0);
            tbl.Controls.Add(L("Valid for"), 1, 0);
            tbl.Controls.Add(new Label(), 2, 0);
            tbl.Controls.Add(cbLicenseClass, 0, 1);
            tbl.Controls.Add(cbLicenseValidity, 1, 1);
            tbl.Controls.Add(L("Date issued"), 0, 2);
            tbl.Controls.Add(L("Expiry date"), 1, 2);
            tbl.Controls.Add(dtpLicenseIssued, 0, 3);
            tbl.Controls.Add(dtpLicenseExpiry, 1, 3);
            tbl.Controls.Add(lblLicenseStatus, 0, 4);
            tbl.SetColumnSpan(lblLicenseStatus, 3);
            pnlLicense.Controls.Add(tbl);
            cbLicenseClass.SelectedIndexChanged += (s, e) => { UpdateLicenseEnabled(); UpdateLicenseStatus(); };
            cbLicenseValidity.SelectedIndexChanged += (s, e) => { if (!settingLicense) ApplyValidity(); };
            dtpLicenseIssued.ValueChanged += (s, e) => { if (!settingLicense) ApplyValidity(); };
            dtpLicenseExpiry.ValueChanged += (s, e) => { if (!settingLicense) { SyncValidityFromDates(); UpdateLicenseStatus(); } };
            UpdateLicenseEnabled();
            UpdateLicenseStatus();
        }
        /// <summary>Fills the role list with the two classifications (plus a legacy role if the record being edited has one).</summary>
        private void FillRoleChoices(string? keep = null)
        {
            cbRole.Items.Clear();
            cbRole.Items.Add(RoleDriver);
            cbRole.Items.Add(RoleAide);
            string k = NormalizeRole(keep);
            if (k.Length > 0 && !cbRole.Items.Contains(k)) cbRole.Items.Add(k);
            if (k.Length > 0) cbRole.SelectedItem = k;
        }
        public static string NormalizeRole(string? role)
        {
            string r = (role ?? "").Trim();
            if (r.Length == 0) return "";
            if (r.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0) return RoleDriver;
            if (r.IndexOf("Aide", StringComparison.OrdinalIgnoreCase) >= 0 || r.IndexOf("Helper", StringComparison.OrdinalIgnoreCase) >= 0) return RoleAide;
            return r;
        }
        private bool IsDriverSelected => string.Equals(cbRole.Text, RoleDriver, StringComparison.OrdinalIgnoreCase);
        private void UpdateRoleRequirements()
        {
            if (lblRoleRequirements == null) return;
            if (IsDriverSelected)
            {
                lblRoleRequirements.Text = "Driver: needs a valid driver's license (Professional is required for trucks; Non-Professional needs your confirmation). An expired license can't be saved.";
                lblRoleRequirements.ForeColor = AppTheme.InfoText;
                if (cbLicenseClass.SelectedIndex == 2) cbLicenseClass.SelectedIndex = 0;
            }
            else if (cbRole.SelectedIndex >= 0)
            {
                lblRoleRequirements.Text = "Delivery aide / helper: no license needed. If they have one, record its class and expiry so they can relieve a driver.";
                lblRoleRequirements.ForeColor = AppTheme.TextMuted;
            }
            else lblRoleRequirements.Text = "Choose Driver or Delivery Aide / Helper.";
            UpdateLicenseStatus();
        }
        private bool HasLicenseSelected => cbLicenseClass.SelectedIndex is 0 or 1;
        private void UpdateLicenseEnabled()
        {
            bool on = HasLicenseSelected;
            dtpLicenseIssued.Enabled = on;
            dtpLicenseExpiry.Enabled = on;
            cbLicenseValidity.Enabled = on;
        }
        private void ApplyValidity()
        {
            if (!HasLicenseSelected) return;
            settingLicense = true;
            try
            {
                if (cbLicenseValidity.SelectedIndex == 0) dtpLicenseExpiry.Value = dtpLicenseIssued.Value.Date.AddYears(5);
                else if (cbLicenseValidity.SelectedIndex == 1) dtpLicenseExpiry.Value = dtpLicenseIssued.Value.Date.AddYears(10);
            }
            finally { settingLicense = false; }
            UpdateLicenseStatus();
        }
        private void SyncValidityFromDates()
        {
            var years = dtpLicenseExpiry.Value.Date == dtpLicenseIssued.Value.Date.AddYears(5) ? 0 : dtpLicenseExpiry.Value.Date == dtpLicenseIssued.Value.Date.AddYears(10) ? 1 : 2;
            settingLicense = true;
            try { cbLicenseValidity.SelectedIndex = years; } finally { settingLicense = false; }
        }
        private void UpdateLicenseStatus()
        {
            if (lblLicenseStatus == null) return;
            if (!HasLicenseSelected)
            {
                lblLicenseStatus.Text = IsDriverSelected ? "⚠ A driver must have a license." : "No license recorded.";
                lblLicenseStatus.ForeColor = IsDriverSelected ? AppTheme.DangerText : AppTheme.TextMuted;
                return;
            }
            var issued = dtpLicenseIssued.Value.Date;
            var exp = dtpLicenseExpiry.Value.Date;
            int days = (int)(exp - DateTime.Today).TotalDays;
            double years = (exp - issued).TotalDays / 365.25;
            string duration = $"Valid {years:0.#} year(s) ({issued:MMM d, yyyy} – {exp:MMM d, yyyy})";
            if (exp <= issued) { lblLicenseStatus.Text = "⚠ Expiry must be after the issue date."; lblLicenseStatus.ForeColor = AppTheme.DangerText; }
            else if (years > 10.05) { lblLicenseStatus.Text = "⚠ Philippine licenses are valid for at most 10 years. " + duration; lblLicenseStatus.ForeColor = AppTheme.DangerText; }
            else if (days < 0) { lblLicenseStatus.Text = $"⚠ EXPIRED {(-days)} day(s) ago. {duration}"; lblLicenseStatus.ForeColor = AppTheme.DangerText; }
            else if (days <= 60) { lblLicenseStatus.Text = $"▲ Expires in {days} day(s) — remind them to renew. {duration}"; lblLicenseStatus.ForeColor = AppTheme.WarningText; }
            else { lblLicenseStatus.Text = $"✓ {duration} · {days / 365} yr {(days % 365) / 30} mo left"; lblLicenseStatus.ForeColor = AppTheme.SuccessText; }
        }
        private void BindClassification(Crew c)
        {
            settingLicense = true;
            try
            {
                FillRoleChoices(c.Role);
                chkIsSeasonal.Checked = c.IsSeasonal;
                cbLicenseClass.SelectedIndex = !c.HasLicense ? 2 : (c.LicenseClass.StartsWith("Professional", StringComparison.OrdinalIgnoreCase) ? 0 : 1);
                var issued = c.LicenseIssued ?? (c.LicenseExpiry.HasValue ? c.LicenseExpiry.Value.AddYears(-5) : DateTime.Today);
                dtpLicenseIssued.Value = issued > dtpLicenseIssued.MaxDate ? dtpLicenseIssued.MaxDate : issued;
                dtpLicenseExpiry.Value = c.LicenseExpiry ?? dtpLicenseIssued.Value.AddYears(5);
            }
            finally { settingLicense = false; }
            SyncValidityFromDates();
            UpdateLicenseEnabled();
            UpdateRoleRequirements();
        }
        private void ClearClassification()
        {
            settingLicense = true;
            try
            {
                FillRoleChoices();
                cbRole.SelectedIndex = -1;
                chkIsSeasonal.Checked = false;
                cbLicenseClass.SelectedIndex = 2;
                dtpLicenseIssued.Value = DateTime.Today;
                cbLicenseValidity.SelectedIndex = 0;
                dtpLicenseExpiry.Value = DateTime.Today.AddYears(5);
            }
            finally { settingLicense = false; }
            UpdateLicenseEnabled();
            UpdateRoleRequirements();
        }
        /// <summary>Checks the role / license rules. Returns false (after telling the person) when the record can't be saved.</summary>
        private bool ValidateClassification()
        {
            var errs = new List<string>();
            if (cbRole.SelectedIndex < 0) errs.Add("Choose a role: Driver or Delivery Aide / Helper.");
            bool driver = IsDriverSelected;
            if (driver && !HasLicenseSelected) errs.Add("A driver must have a driver's license — choose Professional or Non-Professional.");
            if (HasLicenseSelected)
            {
                var issued = dtpLicenseIssued.Value.Date;
                var exp = dtpLicenseExpiry.Value.Date;
                if (issued > DateTime.Today) errs.Add("The license issue date can't be in the future.");
                if (exp <= issued) errs.Add("The license expiry date must be after the issue date.");
                else if ((exp - issued).TotalDays > 366 * 10 + 3) errs.Add("A Philippine driver's license is valid for at most 10 years — check the dates.");
                else if ((exp - issued).TotalDays < 180) errs.Add("The license validity looks too short — check the dates.");
                if (driver && exp < DateTime.Today) errs.Add($"This driver's license expired on {exp:MMM d, yyyy}. Update it with the renewed license before saving.");
            }
            if (errs.Count > 0)
            {
                MessageBox.Show(FindForm(), "Please fix the following:\n\n• " + string.Join("\n• ", errs), "Role and license", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                (cbRole.SelectedIndex < 0 ? (Control)cbRole : cbLicenseClass).Focus();
                return false;
            }
            if (driver && cbLicenseClass.SelectedIndex == 1)
            {
                if (MessageBox.Show(FindForm(), "Truck drivers normally need a Professional Driver's License.\n\nSave this driver with a Non-Professional license anyway?", "Non-Professional license", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return false;
            }
            return true;
        }
        private void ApplyClassification(Crew c)
        {
            c.Role = cbRole.Text.Trim();
            c.IsSeasonal = chkIsSeasonal.Checked;
            c.LicenseNumber = "";
            if (HasLicenseSelected)
            {
                c.LicenseClass = cbLicenseClass.Text;
                c.LicenseIssued = dtpLicenseIssued.Value.Date;
                c.LicenseExpiry = dtpLicenseExpiry.Value.Date;
            }
            else
            {
                c.LicenseClass = LicNone;
                c.LicenseIssued = null;
                c.LicenseExpiry = null;
            }
        }
        /// <summary>Dispatch logs, trip performance, vehicles used and incidents / violations for one crew member.</summary>
        private void OpenCrewProfile(Crew c)
        {
            Crew? toEdit = null;
            Forms.CrewProfileDialog.ShowFor(FindForm(), c, cr => toEdit = cr);
            if (toEdit != null)
            {
                BindCrewToForm(toEdit);
                SetFormVisibility(true);
                txtLastName.Focus();
            }
        }
    }
}
