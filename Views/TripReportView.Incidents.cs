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
    /// Nov 14: the incident report is step 3 of the trip report. A panel asks "Any damaged or missing crates?" (No by default). Yes shows the
    /// incident fields (cause, damaged quantity, estimated cost, chargeable to crew, notes); each incident is added to a list and saved to
    /// IncidentReports with the trip's DeliveryId when the trip report is saved. Several incidents per trip are allowed. The lines are read-only
    /// reference: no statement, invoice, receipt or total reads them. The whole step follows <see cref="Features.TripIncidents"/>.
    /// </summary>
    public partial class TripReportView
    {
        private const int IncidentCauseMax = 100, IncidentNotesMax = 500;
        private Panel? cardIncident;
        private Panel? pnlIncidentStep;
        private RadioButton? rbIncNo, rbIncYes;
        private TableLayoutPanel? pnlIncFields;
        private TextBox? txtIncCause, txtIncNotes;
        private NumericUpDown? numIncQty, numIncCost;
        private CheckBox? chkIncCrew;
        private Button? btnIncAdd, btnIncRemove;
        private DataGridView? gridInc;
        private Label? lblIncHint;
        /// <summary>The incident lines of the trip being reported (Id 0 = not saved yet).</summary>
        private readonly List<IncidentReport> tripIncidentLines = new List<IncidentReport>();
        /// <summary>Saved incident lines the person took off; they are voided when the report is saved.</summary>
        private readonly List<int> removedIncidentIds = new List<int>();
        private bool incidentBusy;

        /// <summary>True while step 3 is shown (the trip-incident switch is on).</summary>
        private bool IncidentStepOn => Features.On(Features.TripIncidents);

        /// <summary>The panel of step 3 (built once; Layout puts it in its own card).</summary>
        private Control BuildIncidentStep()
        {
            pnlIncidentStep = new Panel { Name = "pnlIncidentStep", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            var root = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var ask = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, BackColor = AppTheme.Surface, Margin = new Padding(0, 0, 0, Dpi(6)) };
            var lblAsk = new Label { AutoSize = true, Text = "Any damaged or missing crates?", Font = new Font("Segoe UI Semibold", 10F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, Dpi(5), Dpi(16), 0), UseMnemonic = false };
            rbIncNo = new RadioButton { AutoSize = true, Text = "No", Checked = true, Font = new Font("Segoe UI", 10F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, 0, Dpi(14), 0), Name = "rbIncidentNo" };
            rbIncYes = new RadioButton { AutoSize = true, Text = "Yes", Font = new Font("Segoe UI", 10F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0), Name = "rbIncidentYes" };
            lblIncHint = new Label { AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.WarningText, Margin = new Padding(Dpi(18), Dpi(6), 0, 0), UseMnemonic = false, Visible = false };
            ask.Controls.AddRange(new Control[] { lblAsk, rbIncNo, rbIncYes, lblIncHint });
            root.Controls.Add(ask);

            Label Cap(string t) => new Label { AutoSize = true, Text = t, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, 0, Dpi(12), Dpi(2)), UseMnemonic = false };
            pnlIncFields = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 4, Dock = DockStyle.Top, BackColor = AppTheme.Surface, Visible = false, Margin = new Padding(0) };
            pnlIncFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            pnlIncFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
            pnlIncFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            pnlIncFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
            txtIncCause = new TextBox { Dock = DockStyle.Fill, MaxLength = IncidentCauseMax, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 0, Dpi(12), Dpi(8)), PlaceholderText = "e.g. Broken crates", Name = "txtIncidentCause" };
            numIncQty = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 1000000, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 0, Dpi(12), Dpi(8)), Name = "numIncidentQty" };
            numIncCost = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100000000, DecimalPlaces = 2, ThousandsSeparator = true, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 0, Dpi(12), Dpi(8)), Name = "numIncidentCost" };
            chkIncCrew = new CheckBox { AutoSize = true, Text = "Chargeable to crew", Font = new Font("Segoe UI", 10F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, Dpi(3), 0, Dpi(8)), Name = "chkIncidentCrew" };
            txtIncNotes = new TextBox { Dock = DockStyle.Fill, MaxLength = IncidentNotesMax, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 0, Dpi(12), Dpi(8)), PlaceholderText = "What happened (optional)", Name = "txtIncidentNotes" };
            btnIncAdd = new Button { Text = "＋ Add incident", AutoSize = true, MinimumSize = new Size(Dpi(130), Dpi(34)), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, UseMnemonic = false, Name = "btnIncidentAdd", Margin = new Padding(0, 0, 0, Dpi(8)) };
            UIThemeHelper.SetButtonRole(btnIncAdd, "secondary");
            btnIncAdd.Click += (s, e) => AddIncidentFromFields(showMessages: true);
            pnlIncFields.Controls.Add(Cap("Cause *"), 0, 0);
            pnlIncFields.Controls.Add(Cap("Damaged quantity *"), 1, 0);
            pnlIncFields.Controls.Add(Cap("Estimated cost (₱)"), 2, 0);
            pnlIncFields.Controls.Add(new Label { Text = "", AutoSize = true }, 3, 0);
            pnlIncFields.Controls.Add(txtIncCause, 0, 1);
            pnlIncFields.Controls.Add(numIncQty, 1, 1);
            pnlIncFields.Controls.Add(numIncCost, 2, 1);
            pnlIncFields.Controls.Add(chkIncCrew, 3, 1);
            pnlIncFields.Controls.Add(Cap("Notes"), 0, 2);
            pnlIncFields.Controls.Add(txtIncNotes, 0, 3);
            pnlIncFields.SetColumnSpan(txtIncNotes, 3);
            pnlIncFields.Controls.Add(btnIncAdd, 3, 3);
            root.Controls.Add(pnlIncFields);

            gridInc = new DataGridView
            {
                Name = "gridIncidentLines", Dock = DockStyle.Top, Height = Dpi(120), ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = AppTheme.Surface, BorderStyle = BorderStyle.FixedSingle, Visible = false, Margin = new Padding(0)
            };
            UIThemeHelper.ApplyGridTheme(gridInc);
            gridInc.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIncCause", HeaderText = "Cause", FillWeight = 30 });
            gridInc.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIncQty", HeaderText = "Damaged qty", FillWeight = 14, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            gridInc.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIncCost", HeaderText = "Est. cost (₱)", FillWeight = 16, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            gridInc.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIncCrew", HeaderText = "Charged to crew", FillWeight = 14 });
            gridInc.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIncNotes", HeaderText = "Notes", FillWeight = 40 });
            root.Controls.Add(gridInc);
            btnIncRemove = new Button { Text = "Remove selected incident", AutoSize = true, MinimumSize = new Size(Dpi(170), Dpi(32)), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, UseMnemonic = false, Visible = false, Name = "btnIncidentRemove", Margin = new Padding(0, Dpi(6), 0, 0) };
            UIThemeHelper.SetButtonRole(btnIncRemove, "secondary");
            btnIncRemove.Click += (s, e) => RemoveSelectedIncident();
            root.Controls.Add(btnIncRemove);

            rbIncYes.CheckedChanged += (s, e) => { if (!incidentBusy && rbIncYes.Checked) ShowIncidentFields(); };
            rbIncNo.CheckedChanged += (s, e) => { if (!incidentBusy && rbIncNo.Checked) ChooseNoIncident(); };
            pnlIncidentStep.Controls.Add(root);
            return pnlIncidentStep;
        }

        /// <summary>The whole card appears only while the trip-incident switch is on.</summary>
        private void ApplyIncidentFeature()
        {
            if (cardIncident != null) cardIncident.Visible = IncidentStepOn;
            UpdateTripStepper();
        }

        private void ShowIncidentFields()
        {
            if (pnlIncFields == null || gridInc == null || btnIncRemove == null) return;
            pnlIncFields.Visible = true;
            RefreshIncidentGrid();
            UpdateTripStepper();
        }

        /// <summary>"No" with incident lines on the list asks before taking them off the trip.</summary>
        private void ChooseNoIncident()
        {
            if (pnlIncFields == null || gridInc == null || btnIncRemove == null) return;
            if (tripIncidentLines.Count > 0)
            {
                var ans = MessageBox.Show(FindForm(), $"Take the {tripIncidentLines.Count} incident line{(tripIncidentLines.Count == 1 ? "" : "s")} off this trip?\n\nThey are removed when you save the trip report.", "Any damaged or missing crates?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (ans != DialogResult.Yes)
                {
                    incidentBusy = true;
                    try { rbIncYes!.Checked = true; } finally { incidentBusy = false; }
                    return;
                }
                foreach (var l in tripIncidentLines) if (l.Id > 0) removedIncidentIds.Add(l.Id);
                tripIncidentLines.Clear();
            }
            pnlIncFields.Visible = false;
            RefreshIncidentGrid();
            UpdateTripStepper();
        }

        private void RefreshIncidentGrid()
        {
            if (gridInc == null || btnIncRemove == null) return;
            gridInc.Rows.Clear();
            foreach (var l in tripIncidentLines)
            {
                int i = gridInc.Rows.Add(l.Cause, l.DamagedQuantity.ToString("N0"), l.EstimatedCost.ToString("N2"), l.ChargeableToCrew ? "Yes" : "No", l.Description);
                gridInc.Rows[i].Tag = l;
            }
            bool yes = rbIncYes != null && rbIncYes.Checked;
            gridInc.Visible = yes && tripIncidentLines.Count > 0;
            btnIncRemove.Visible = gridInc.Visible;
            int rows = Math.Min(Math.Max(tripIncidentLines.Count, 1), 5);
            gridInc.Height = gridInc.ColumnHeadersHeight + rows * Math.Max(gridInc.RowTemplate.Height, Dpi(28)) + Dpi(4);
        }

        /// <summary>Shows the saved incidents of the picked dispatch (Yes when there are some, otherwise No). Special trips and no trip: the step is switched off.</summary>
        private void LoadIncidentStep(Delivery? d)
        {
            if (rbIncNo == null || rbIncYes == null || pnlIncFields == null) return;
            incidentBusy = true;
            try
            {
                tripIncidentLines.Clear();
                removedIncidentIds.Clear();
                bool usable = d != null && d.Id > 0 && IncidentStepOn;
                rbIncNo.Enabled = rbIncYes.Enabled = usable;
                if (usable) tripIncidentLines.AddRange(MJdatabase.GetTripIncidents(d!.Id));
                bool yes = tripIncidentLines.Count > 0;
                rbIncYes.Checked = yes;
                rbIncNo.Checked = !yes;
                pnlIncFields.Visible = yes;
                ClearIncidentFields();
                RefreshIncidentGrid();
            }
            finally { incidentBusy = false; }
            UpdateTripStepper();
        }

        /// <summary>Back to No, no lines and empty fields (a new report, or after a save).</summary>
        private void ResetIncidentStep() => LoadIncidentStep(null);

        private void ClearIncidentFields()
        {
            if (txtIncCause == null || numIncQty == null || numIncCost == null || chkIncCrew == null || txtIncNotes == null) return;
            txtIncCause.Text = ""; numIncQty.SetSafe(0); numIncCost.SetSafe(0); chkIncCrew.Checked = false; txtIncNotes.Text = "";
        }

        private bool IncidentFieldsTouched()
            => (txtIncCause?.Text.Trim().Length ?? 0) > 0 || (numIncQty?.Value ?? 0) > 0 || (numIncCost?.Value ?? 0) > 0 || (txtIncNotes?.Text.Trim().Length ?? 0) > 0 || (chkIncCrew?.Checked ?? false);

        /// <summary>The message that says what is wrong with the typed incident, or "" when it can be added.</summary>
        private string IncidentFieldsProblem(out Control? field)
        {
            field = null;
            string cause = DailyRules.Tidy(txtIncCause?.Text);
            if (cause.Length == 0) { field = txtIncCause; return "Incident cause is required: say what happened to the crates."; }
            if (!System.Text.RegularExpressions.Regex.IsMatch(cause, @"\p{L}")) { field = txtIncCause; return "Incident cause must have some letters."; }
            int qty = (int)(numIncQty?.Value ?? 0);
            if (qty < 1) { field = numIncQty; return "Damaged quantity must be at least 1."; }
            var del = TripDelivery();
            int loaded = del == null ? 0 : (del.TotalCrates > 0 ? del.TotalCrates : del.GCrate + del.ECrate);
            if (loaded > 0 && qty > loaded) { field = numIncQty; return $"Damaged quantity ({qty:N0}) can't be more than the {loaded:N0} crates loaded on dispatch {del!.DispatchNumber}."; }
            if (chkIncCrew != null && chkIncCrew.Checked && (numIncCost?.Value ?? 0) <= 0) { field = numIncCost; return "This incident is chargeable to the crew, but the estimated cost is 0.00. Enter the cost, or untick \"Chargeable to crew\"."; }
            if (tripIncidentLines.Any(l => string.Equals(l.Cause, cause, StringComparison.OrdinalIgnoreCase)))
            { field = txtIncCause; return $"\"{cause}\" is already on this trip's list. Remove it first, or use a different cause."; }
            return "";
        }

        /// <summary>Adds the typed incident to the trip's list. False when it is not valid (and, with <paramref name="showMessages"/>, says why).</summary>
        private bool AddIncidentFromFields(bool showMessages)
        {
            string problem = IncidentFieldsProblem(out var field);
            if (problem.Length > 0)
            {
                if (showMessages) { MessageBox.Show(FindForm(), problem, "Incident report", MessageBoxButtons.OK, MessageBoxIcon.Warning); try { field?.Focus(); } catch (Exception ex) { ErrLog.Swallowed(ex); } }
                return false;
            }
            var del = TripDelivery();
            tripIncidentLines.Add(new IncidentReport
            {
                DeliveryId = del?.Id ?? 0,
                DispatchNumber = del?.DispatchNumber ?? "",
                IncidentDate = DateTime.Today,
                Cause = DailyRules.Tidy(txtIncCause!.Text),
                DamagedQuantity = (int)numIncQty!.Value,
                EstimatedCost = numIncCost!.Value,
                ChargeableToCrew = chkIncCrew!.Checked,
                Description = DailyRules.CollapseSpaces(txtIncNotes!.Text),
                Status = "Pending"
            });
            ClearIncidentFields();
            RefreshIncidentGrid();
            UpdateTripStepper();
            return true;
        }

        private void RemoveSelectedIncident()
        {
            if (gridInc == null || gridInc.CurrentRow?.Tag is not IncidentReport line) return;
            if (line.Id > 0) removedIncidentIds.Add(line.Id);
            tripIncidentLines.Remove(line);
            RefreshIncidentGrid();
            UpdateTripStepper();
        }

        /// <summary>Step 3 is done when the answer is No, or Yes with at least one incident and nothing half-typed.</summary>
        private bool StepIncidentDone()
        {
            if (rbIncYes == null || !rbIncYes.Checked) return true;
            return tripIncidentLines.Count > 0 && !IncidentFieldsTouched();
        }

        /// <summary>The blocking messages of step 3 for a finished report (Yes needs at least one valid incident). A typed incident that is valid is added first.</summary>
        private void IncidentChecks(List<(string Message, Control Field)> list)
        {
            if (!IncidentStepOn || rbIncYes == null || !rbIncYes.Checked || !rbIncYes.Enabled) return;
            if (IncidentFieldsTouched())
            {
                string problem = IncidentFieldsProblem(out var field);
                if (problem.Length > 0) { list.Add((problem, field ?? (Control)rbIncYes)); return; }
                AddIncidentFromFields(showMessages: false);
            }
            if (tripIncidentLines.Count == 0)
                list.Add(("Incident report: add at least one incident, or choose No under \"Any damaged or missing crates?\".", txtIncCause ?? (Control)rbIncYes));
        }

        /// <summary>Saves the incident lines of the trip just reported (new lines are added, removed ones are voided). The trip report itself is already saved.</summary>
        private void PersistTripIncidents(Delivery? del)
        {
            if (del == null || del.Id <= 0 || !IncidentStepOn || rbIncYes == null) return;
            try
            {
                if (IncidentFieldsTouched() && rbIncYes.Checked) AddIncidentFromFields(showMessages: false);   // a valid typed line is kept even on a draft
                foreach (int id in removedIncidentIds.ToList()) { MJdatabase.VoidTripIncident(id); removedIncidentIds.Remove(id); }
                if (!rbIncYes.Checked) return;
                foreach (var line in tripIncidentLines.Where(l => l.Id == 0).ToList())
                {
                    line.DeliveryId = del.Id;
                    line.DispatchNumber = del.DispatchNumber ?? "";
                    MJdatabase.SaveIncidentReport(line, notify: false);
                }
            }
            catch (Exception ex)
            {
                ErrLog.Error("TripReportView.Incidents", ex);
                MessageBox.Show(FindForm(), "The trip report is saved, but its incident report could not be saved: " + ex.Message + "\n\nOpen the trip report again and add the incident.", "Incident report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
