using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// Small "Add driver" / "Add aide" window opened from the Driver and Aide search boxes of the dispatch screen when the typed name is not found.
    /// Last name, first name, optional middle name, contact number, address and employment type (Regular or Seasonal); the role is fixed. It runs the same checks as the Crew screen's add
    /// (name standard, letters only, phone format, duplicate check) and saves the person as active and available.
    /// Problems are shown inside the window (never in a pop-up). Optional details (photo, license, wage) are filled in later on the Crew screen.
    /// </summary>
    public sealed class QuickAddCrewDialog : Form
    {
        private readonly bool driver;
        private TextBox txtLast = null!, txtFirst = null!, txtMiddle = null!, txtContact = null!, txtAddress = null!;
        private ComboBox cbEmployment = null!;
        private Label lblProblem = null!;
        private ComboBox cbLicense = null!;
        private DateTimePicker dtpIssued = null!, dtpExpiry = null!;
        private Button btnSave = null!, btnCancel = null!;

        /// <summary>The id of the person that was saved (0 until Save succeeded).</summary>
        public int SavedId { get; private set; }

        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);

        public QuickAddCrewDialog(bool driver, string typed = "")
        {
            this.driver = driver;
            Build();
            var (last, first) = SplitTyped(typed);
            txtLast.Text = last; txtFirst.Text = first;
            UIThemeHelper.ApplyTheme(this);
            Shown += (s, e) => { (txtLast.Text.Length == 0 ? txtLast : txtFirst.Text.Length == 0 ? txtFirst : txtContact).Focus(); };
        }

        /// <summary>Turns what was typed in the search box into a starting point: "Juan Santos" gives first Juan and last Santos, "SANTOS, JUAN" gives last Santos and first Juan, one word is the last name.</summary>
        public static (string Last, string First) SplitTyped(string? typed)
        {
            string t = (typed ?? "").Trim();
            if (t.Length == 0) return ("", "");
            int comma = t.IndexOf(',');
            if (comma >= 0) return (t.Substring(0, comma).Trim(), t.Substring(comma + 1).Trim());
            var parts = t.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return (parts[0], "");
            return (parts[parts.Length - 1], string.Join(" ", parts, 0, parts.Length - 1));
        }

        /// <summary>
        /// Runs the Crew screen's add checks and saves. Returns the new id, or a problem text (and the field it is about: last, first, middle or contact) when nothing was saved.
        /// Never shows a message box.
        /// </summary>
        public static (int Id, string? Problem, string Field) TrySave(bool driver, string last, string first, string middle, string contact,
            string licenseClass = "", DateTime? licenseIssued = null, DateTime? licenseExpiry = null, string address = "", bool seasonal = false)
        {
            try
            {
                last = (last ?? "").Trim(); first = (first ?? "").Trim(); middle = (middle ?? "").Trim(); contact = (contact ?? "").Trim();
                if (last.Length == 0) return (0, "Last name is required.", "last");
                if (first.Length == 0) return (0, "First name is required.", "first");
                var np = NameHelper.CheckStandard(last, first, middle);
                if (np != null) return (0, np.Value.Message, np.Value.Field);
                string name = NameHelper.FormatFullName(last, first, middle);
                if (!SecurityHelper.IsValidName(last) || !NameHelper.FirstNameOk(first))
                    return (0, "Please enter a valid last name and first name containing only letters, spaces, hyphens, and apostrophes.", !SecurityHelper.IsValidName(last) ? "last" : "first");
                if (middle.Length > 0 && !SecurityHelper.IsValidName(middle))
                    return (0, "Please enter a valid middle name containing only letters, spaces, hyphens, and apostrophes, or leave it blank.", "middle");
                if (contact.Length > 0 && !SecurityHelper.IsValidPhone(contact))
                    return (0, "Invalid contact number format. Please enter a valid mobile number (e.g. 09171234567).", "contact");
                string? addressProblem = CrewRules.CheckAddress(address);
                if (addressProblem != null) return (0, addressProblem, "address");
                var lp = CrewRules.CheckLicense(driver, licenseClass, licenseIssued, licenseExpiry);
                if (lp != null) return (0, lp.Value.Message, lp.Value.Field);
                string? duplicate = MJdatabase.ValidateDuplicateCrew(0, name, contact, null, last, first);
                if (!string.IsNullOrEmpty(duplicate)) return (0, duplicate, "last");
                string role = driver ? CrewManagementView.RoleDriver : CrewManagementView.RoleAide;
                MJdatabase.SaveLookupItem("Crew Role", role);
                var crew = new Crew
                {
                    FullName = name, LastName = last, FirstName = first, MiddleName = middle, Role = role, ContactNumber = contact,
                    DailyWage = 0, FoodAllowance = 0, IsAvailable = true, IsActive = true, PhotoPath = "",
                    Address = InputRules.Tidy(address), IsSeasonal = seasonal
                };
                bool hasLicense = !string.IsNullOrWhiteSpace(licenseClass) && licenseClass != CrewRules.LicNone;
                if (hasLicense)
                {
                    // Stored the same way the Crew screen's ApplyClassification does.
                    crew.LicenseNumber = "";
                    crew.LicenseClass = licenseClass;
                    crew.LicenseIssued = licenseIssued!.Value.Date;
                    crew.LicenseExpiry = licenseExpiry!.Value.Date;
                }
                MJdatabase.SaveCrew(crew);
                return (crew.Id, null, "");
            }
            catch (Exception ex)
            {
                ErrLog.Error("QuickAddCrewDialog", ex);
                return (0, "Could not save: " + ex.Message, "");
            }
        }

        /// <summary>
        /// The Crew screen's driver license rules (CrewRules.CheckLicense): a Professional license is required, with issue and expiry dates;
        /// issue date not in the future; expiry after issue, at most 10 years and at least 6 months of validity; an expired license is refused.
        /// Returns null when fine, otherwise the problem and the field it is about (license, issued or expiry).
        /// </summary>
        public static (string Message, string Field)? CheckDriverLicense(string? licenseClass, DateTime? issued, DateTime? expiry)
            => CrewRules.CheckLicense(true, licenseClass, issued, expiry);

        private void Build()
        {
            string noun = driver ? "driver" : "aide";
            Text = driver ? "Add driver" : "Add aide";
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", LargePrint.On ? 11F : 9.5F);
            ClientSize = new Size(D(LargePrint.Px(560, 460)), D(LargePrint.On ? (driver ? 760 : 520) : (driver ? 640 : 440)));
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            BackColor = AppTheme.Canvas;

            var header = new Panel { Dock = DockStyle.Top, Height = D(64), BackColor = AppTheme.Surface, Padding = new Padding(D(20), D(10), D(20), D(8)) };
            header.Paint += (s, e) => { using var p = new Pen(AppTheme.Border); e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1); };
            header.Controls.Add(new Label { Text = $"Save a new {noun}. Photo and pay are added later on the Crew screen.", Dock = DockStyle.Top, Height = D(20), Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, AutoEllipsis = true });
            header.Controls.Add(new Label { Text = driver ? "New driver" : "New aide / helper", Dock = DockStyle.Top, Height = D(28), Font = new Font("Segoe UI Semibold", 13F), ForeColor = AppTheme.TextPrimary });

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = D(54), FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(D(16), D(8), D(16), D(8)), BackColor = AppTheme.Surface };
            btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(D(96), D(34)), Cursor = Cursors.Hand };
            btnSave = new Button { Text = driver ? "Save driver" : "Save aide", Size = new Size(D(130), D(34)), Cursor = Cursors.Hand };
            UIThemeHelper.SetButtonRole(btnCancel, "secondary");
            UIThemeHelper.SetButtonRole(btnSave, "primary");
            btnSave.Click += (s, e) => DoSave();
            buttons.Controls.Add(btnCancel); buttons.Controls.Add(btnSave);
            CancelButton = btnCancel; AcceptButton = btnSave;

            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(D(20), D(12), D(20), D(4)), BackColor = AppTheme.Canvas, AutoScroll = false };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            TextBox Add(string caption, int row)
            {
                var tb = new TextBox { Dock = DockStyle.Top, Margin = new Padding(0, D(4), 0, D(6)), Font = Font };
                grid.RowStyles.Add(new RowStyle(SizeType.Absolute, D(LargePrint.On ? 40 : 34)));
                grid.Controls.Add(new Label { Text = caption, AutoSize = true, Margin = new Padding(0, D(9), D(8), 0), ForeColor = AppTheme.TextSecondary, UseMnemonic = false }, 0, row);
                grid.Controls.Add(tb, 1, row);
                UIThemeHelper.StyleInput(tb);
                tb.TextChanged += (s, e) => lblProblem.Text = "";
                return tb;
            }
            lblProblem = new Label { Dock = DockStyle.Fill, ForeColor = AppTheme.DangerText, Font = new Font("Segoe UI Semibold", 9.25F), UseMnemonic = false, Margin = new Padding(0, D(4), 0, 0) };
            txtLast = Add("Last name", 0); txtFirst = Add("First name", 1); txtMiddle = Add("Middle name (optional)", 2); txtContact = Add("Contact number (optional)", 3);
            txtAddress = Add("Address", 4); txtAddress.MaxLength = CrewRules.AddressMax;
            int pr = 5;
            cbEmployment = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, D(4), 0, D(6)), Font = Font };
            cbEmployment.Items.AddRange(new object[] { CrewRules.TypeRegular, CrewRules.TypeSeasonal });
            cbEmployment.SelectedIndex = 0;
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, D(LargePrint.On ? 40 : 34)));
            grid.Controls.Add(new Label { Text = "Employment type", AutoSize = true, Margin = new Padding(0, D(9), D(8), 0), ForeColor = AppTheme.TextSecondary, UseMnemonic = false }, 0, pr);
            grid.Controls.Add(cbEmployment, 1, pr); pr++;
            if (driver)
            {
                int rh = D(LargePrint.On ? 40 : 34);
                var f = new Font("Segoe UI", LargePrint.On ? 11F : 9.5F);
                cbLicense = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, D(4), 0, D(6)), Font = f };
                cbLicense.Items.AddRange(new object[] { CrewRules.LicPro });
                cbLicense.SelectedIndex = 0;
                dtpIssued = new DateTimePicker { Dock = DockStyle.Top, Format = DateTimePickerFormat.Custom, CustomFormat = Fmt.DateFormat, Margin = new Padding(0, D(4), 0, D(6)), Font = f };
                dtpIssued.CapAtToday();
                dtpExpiry = new DateTimePicker { Dock = DockStyle.Top, Format = DateTimePickerFormat.Custom, CustomFormat = Fmt.DateFormat, Margin = new Padding(0, D(4), 0, D(6)), Font = f };
                dtpExpiry.SetSafe(DateTime.Today.AddYears(5));
                void Row(string caption, Control c) { grid.RowStyles.Add(new RowStyle(SizeType.Absolute, rh)); grid.Controls.Add(new Label { Text = caption, AutoSize = true, Margin = new Padding(0, D(9), D(8), 0), ForeColor = AppTheme.TextSecondary, UseMnemonic = false }, 0, pr); grid.Controls.Add(c, 1, pr); pr++; }
                Row("License type", cbLicense); Row("License issued", dtpIssued); Row("License expires", dtpExpiry);
                cbLicense.SelectedIndexChanged += (s, e) => lblProblem.Text = "";
                dtpIssued.ValueChanged += (s, e) => { dtpExpiry.SetSafe(dtpIssued.Value.Date.AddYears(5)); lblProblem.Text = ""; };
                dtpExpiry.ValueChanged += (s, e) => lblProblem.Text = "";
            }
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            grid.Controls.Add(lblProblem, 0, pr); grid.SetColumnSpan(lblProblem, 2);

            Controls.Add(grid); Controls.Add(buttons); Controls.Add(header);
        }

        private void DoSave()
        {
            var (id, problem, field) = driver
                ? TrySave(true, txtLast.Text, txtFirst.Text, txtMiddle.Text, txtContact.Text, cbLicense.Text, dtpIssued.Value.Date, dtpExpiry.Value.Date, txtAddress.Text, cbEmployment.SelectedItem as string == CrewRules.TypeSeasonal)
                : TrySave(false, txtLast.Text, txtFirst.Text, txtMiddle.Text, txtContact.Text, address: txtAddress.Text, seasonal: cbEmployment.SelectedItem as string == CrewRules.TypeSeasonal);
            if (problem != null)
            {
                lblProblem.Text = problem;
                Control target = field switch
                {
                    "first" => txtFirst, "middle" => txtMiddle, "contact" => txtContact, "address" => txtAddress,
                    "license" => cbLicense, "issued" => dtpIssued, "expiry" => dtpExpiry,
                    _ => txtLast
                };
                target.Focus();
                return;
            }
            SavedId = id;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
