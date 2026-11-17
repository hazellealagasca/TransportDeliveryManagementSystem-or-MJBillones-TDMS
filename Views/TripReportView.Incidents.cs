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
        /// <summary>Nov 17: "Charge to" row: one tick box per crew member of the trip (driver, aides), shown while "Chargeable to crew" is ticked.</summary>
        private Panel? pnlIncCrewPick;
        private FlowLayoutPanel? flowIncCrew;
        private Label? lblIncShare;
        private readonly List<CheckBox> incCrewBoxes = new List<CheckBox>();
        /// <summary>The incident lines of the trip being reported (Id 0 = not saved yet).</summary>
        private readonly List<IncidentReport> tripIncidentLines = new List<IncidentReport>();
        /// <summary>Saved incident lines the person took off; they are voided when the report is saved.</summary>
        private readonly List<int> removedIncidentIds = new List<int>();
        private bool incidentBusy;
        /// <summary>Nov 18: the dispatch step 3 belongs to, read fresh from the database when the trip is picked (its truck, driver and aides as assigned now).</summary>
        private Delivery? incidentTrip;
        /// <summary>Nov 18: "Truck · Driver · Aides" of that dispatch, at the top of step 3.</summary>
        private Label? lblIncTrip;

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
            // Nov 18: the trip's assigned truck and crew, so the incident is clearly tied to them (and "Charge to" lists the same people).
            lblIncTrip = new Label { AutoSize = true, Font = new Font("Segoe UI", 9.5F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, 0, 0, Dpi(6)), UseMnemonic = false, Name = "lblIncidentTrip",
                Text = "Pick the dispatch first: its truck, driver and aides show here." };
            root.Controls.Add(lblIncTrip);
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
            // Nov 17: who pays: the trip's driver and/or aides (the cost is shared equally). Record only.
            pnlIncCrewPick = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AppTheme.Surface, Visible = false, Margin = new Padding(0, 0, 0, Dpi(8)), Name = "pnlIncidentCrewPick" };
            var crewCap = Cap("Charge to (driver / aide) *");
            crewCap.Dock = DockStyle.Top;
            flowIncCrew = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, BackColor = AppTheme.Surface, Margin = new Padding(0), Padding = new Padding(0, Dpi(2), 0, 0), Name = "flowIncidentCrew" };
            lblIncShare = new Label { Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, UseMnemonic = false, Padding = new Padding(0, Dpi(2), 0, 0), Name = "lblIncidentShare" };
            pnlIncCrewPick.Controls.Add(lblIncShare);
            pnlIncCrewPick.Controls.Add(flowIncCrew);
            pnlIncCrewPick.Controls.Add(crewCap);
            pnlIncFields.Controls.Add(pnlIncCrewPick, 0, 4);
            pnlIncFields.SetColumnSpan(pnlIncCrewPick, 4);
            chkIncCrew.CheckedChanged += (s, e) => RefreshIncidentCrewPicker();
            numIncCost.ValueChanged += (s, e) => UpdateIncidentShare();
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
            gridInc.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIncCrew", HeaderText = "Charged to", FillWeight = 24 });
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
                var gone = ChargedNoLongerOnTrip(l);   // Nov 18: the dispatch's crew changed after this incident was saved
                string charged = gone.Count == 0 ? l.ChargedToDisplay : $"{l.ChargedToDisplay}  ⚠ no longer on this trip: {string.Join("; ", gone)}";
                int i = gridInc.Rows.Add(l.Cause, l.DamagedQuantity.ToString("N0"), l.EstimatedCost.ToString("N2"), charged, l.Description);
                gridInc.Rows[i].Tag = l;
                if (gone.Count > 0)
                {
                    gridInc.Rows[i].Cells["colIncCrew"].Style.ForeColor = AppTheme.WarningText;
                    gridInc.Rows[i].Cells["colIncCrew"].ToolTipText = "The dispatch's crew was changed after this incident was saved. Remove the incident and add it again to charge the current crew.";
                }
            }
            bool yes = rbIncYes != null && rbIncYes.Checked;
            gridInc.Visible = yes && tripIncidentLines.Count > 0;
            btnIncRemove.Visible = gridInc.Visible;
            int rows = Math.Min(Math.Max(tripIncidentLines.Count, 1), 5);
            gridInc.Height = gridInc.ColumnHeadersHeight + rows * Math.Max(gridInc.RowTemplate.Height, Dpi(28)) + Dpi(4);
            try { UpdateTripCrateSummary(); } catch (Exception ex) { ErrLog.Swallowed(ex); }   // Nov 18: the damaged-crates count follows step 3 at once
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
                // Nov 18: the trip as assigned NOW (the list behind the drop-down can be older than a change made on the dispatch).
                incidentTrip = null;
                if (d != null && d.Id > 0)
                {
                    try { incidentTrip = MJdatabase.GetDeliveryById(d.Id) ?? d; }
                    catch (Exception ex) { ErrLog.Swallowed(ex); incidentTrip = d; }
                }
                ShowIncidentTripLine();
                bool usable = d != null && d.Id > 0 && IncidentStepOn;
                rbIncNo.Enabled = rbIncYes.Enabled = usable;
                if (usable) tripIncidentLines.AddRange(MJdatabase.GetTripIncidents(d!.Id));
                bool yes = tripIncidentLines.Count > 0;
                rbIncYes.Checked = yes;
                rbIncNo.Checked = !yes;
                pnlIncFields.Visible = yes;
                ClearIncidentFields();
                RefreshIncidentCrewPicker();   // the picked trip decides who can be charged
                RefreshIncidentGrid();
            }
            finally { incidentBusy = false; }
            UpdateTripStepper();
        }

        /// <summary>Nov 18: "🚚 Truck: … · 👤 Driver: … · 🤝 Aides: …" of the trip step 3 belongs to.</summary>
        private void ShowIncidentTripLine()
        {
            if (lblIncTrip == null) return;
            var t = incidentTrip;
            if (t == null) { lblIncTrip.Text = "Pick the dispatch first: its truck, driver and aides show here."; return; }
            string aides = string.Join(", ", MJdatabase.GetTripCrewForCharge(t).Where(c => c.Role == "Aide").Select(c => c.Name));
            string driver = MJdatabase.GetTripCrewForCharge(t).FirstOrDefault(c => c.Role == "Driver").Name ?? "";
            lblIncTrip.Text = $"Dispatch {t.DispatchNumber}   ·   🚚 Truck: {(string.IsNullOrWhiteSpace(t.TruckInfo) ? "none" : t.TruckInfo)}   ·   👤 Driver: {(driver.Length == 0 ? "none" : driver)}   ·   🤝 Aides: {(aides.Length == 0 ? "none" : aides)}";
        }

        /// <summary>Nov 18: crew ids of a saved incident line that are no longer on the trip's crew (after the dispatch's crew was changed).</summary>
        private List<string> ChargedNoLongerOnTrip(IncidentReport l)
        {
            var left = new List<string>();
            if (!l.ChargeableToCrew || string.IsNullOrWhiteSpace(l.ChargedCrewIds) || incidentTrip == null) return left;
            var crewIds = MJdatabase.GetTripCrewForCharge(incidentTrip).Select(c => c.Id).ToHashSet();
            var ids = l.ChargedCrewIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var names = (l.ChargedCrewNames ?? "").Split(';', StringSplitOptions.TrimEntries);
            for (int i = 0; i < ids.Length; i++)
                if (int.TryParse(ids[i], out int id) && !crewIds.Contains(id)) left.Add(i < names.Length && names[i].Length > 0 ? names[i] : $"Crew #{id}");
            return left;
        }

        /// <summary>Back to No, no lines and empty fields (a new report, or after a save).</summary>
        private void ResetIncidentStep() => LoadIncidentStep(null);

        private void ClearIncidentFields()
        {
            if (txtIncCause == null || numIncQty == null || numIncCost == null || chkIncCrew == null || txtIncNotes == null) return;
            txtIncCause.Text = ""; numIncQty.SetSafe(0); numIncCost.SetSafe(0); chkIncCrew.Checked = false; txtIncNotes.Text = "";
            foreach (var b in incCrewBoxes) b.Checked = false;
        }

        /// <summary>The ticked crew members of the "Charge to" row (Id and name).</summary>
        private List<(int Id, string Name)> PickedIncidentCrew()
            => incCrewBoxes.Where(b => b.Checked && b.Tag is ValueTuple<int, string>).Select(b => ((ValueTuple<int, string>)b.Tag!)).Select(t => (t.Item1, t.Item2)).ToList();

        /// <summary>Shows the "Charge to" row while "Chargeable to crew" is ticked, with one tick box per crew member of the trip (ticks are kept for the same people).</summary>
        private void RefreshIncidentCrewPicker()
        {
            if (pnlIncCrewPick == null || flowIncCrew == null || chkIncCrew == null) return;
            var keep = PickedIncidentCrew().Select(p => p.Id).ToHashSet();
            flowIncCrew.SuspendLayout();
            try
            {
                foreach (var b in incCrewBoxes) { flowIncCrew.Controls.Remove(b); b.Dispose(); }
                incCrewBoxes.Clear();
                foreach (Control c in flowIncCrew.Controls.Cast<Control>().ToList()) { flowIncCrew.Controls.Remove(c); c.Dispose(); }
                List<(int Id, string Name, string Role)> crew;
                try { crew = MJdatabase.GetTripCrewForCharge(incidentTrip ?? TripDelivery()); }   // Nov 18: the trip step 3 was loaded for
                catch (Exception ex) { ErrLog.Swallowed(ex); crew = new List<(int, string, string)>(); }
                foreach (var m in crew)
                {
                    var b = new CheckBox
                    {
                        AutoSize = true, Text = $"{m.Name} ({m.Role})", Tag = (m.Id, m.Name), Checked = keep.Contains(m.Id) || (crew.Count == 1 && chkIncCrew.Checked),
                        Font = new Font("Segoe UI", 10F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, 0, Dpi(18), Dpi(2)), UseMnemonic = false, Name = $"chkIncidentCrew_{m.Id}"
                    };
                    b.CheckedChanged += (s, e) => UpdateIncidentShare();
                    incCrewBoxes.Add(b);
                    flowIncCrew.Controls.Add(b);
                }
                if (crew.Count == 0)
                    flowIncCrew.Controls.Add(new Label { AutoSize = true, Text = "This trip has no driver or aide on record, so nobody can be charged.", Font = new Font("Segoe UI", 9.5F), ForeColor = AppTheme.WarningText, UseMnemonic = false });
            }
            finally { flowIncCrew.ResumeLayout(); }
            pnlIncCrewPick.Visible = chkIncCrew.Checked;
            UpdateIncidentShare();
        }

        /// <summary>"Each pays PHP x (cost shared by n)" under the tick boxes.</summary>
        private void UpdateIncidentShare()
        {
            if (lblIncShare == null) return;
            int n = PickedIncidentCrew().Count;
            decimal cost = numIncCost?.Value ?? 0m;
            lblIncShare.Text = n == 0 ? "Tick who is charged. The estimated cost is shared equally. Record only: pay, statements, invoices and receipts are not changed."
                : n == 1 ? $"Charged PHP {cost:N2} in full. Record only: pay, statements, invoices and receipts are not changed."
                : $"Each pays PHP {Math.Round(cost / n, 2):N2} (PHP {cost:N2} shared by {n}). Record only: pay, statements, invoices and receipts are not changed.";
            lblIncShare.ForeColor = n == 0 && (chkIncCrew?.Checked ?? false) ? AppTheme.WarningText : AppTheme.TextMuted;
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
            if (chkIncCrew != null && chkIncCrew.Checked && PickedIncidentCrew().Count == 0)
            {
                field = incCrewBoxes.FirstOrDefault() ?? (Control?)chkIncCrew;
                return incCrewBoxes.Count == 0
                    ? "This incident is chargeable to the crew, but the trip has no driver or aide on record. Untick \"Chargeable to crew\"."
                    : "This incident is chargeable to the crew: tick who is charged under \"Charge to (driver / aide)\".";
            }
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
            var charged = chkIncCrew!.Checked ? PickedIncidentCrew() : new List<(int Id, string Name)>();
            tripIncidentLines.Add(new IncidentReport
            {
                DeliveryId = del?.Id ?? 0,
                DispatchNumber = del?.DispatchNumber ?? "",
                IncidentDate = DateTime.Today,
                Cause = DailyRules.Tidy(txtIncCause!.Text),
                DamagedQuantity = (int)numIncQty!.Value,
                EstimatedCost = numIncCost!.Value,
                ChargeableToCrew = chkIncCrew!.Checked,
                ChargedCrewIds = string.Join(",", charged.Select(c => c.Id)),
                ChargedCrewNames = string.Join("; ", charged.Select(c => c.Name)),   // names are "Last, First", so people are split by ";"
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
