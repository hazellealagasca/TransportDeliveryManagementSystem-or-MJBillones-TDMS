using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// Everything about one truck: specifications (editable), every dispatch it ran, its documents and the
    /// incidents logged on its trips. All history lists are newest first.
    /// </summary>
    public sealed class TruckProfileDialog : Form
    {
        private readonly Truck truck;
        private readonly Action<Truck>? onEditDetails;
        private readonly ProfileHeader header = new ProfileHeader();
        private readonly SegmentTabs tabs = new SegmentTabs("Specs", "Dispatch history", "Incident logs", "Documents & photos");
        private readonly Panel body = new Panel { Dock = DockStyle.Fill };
        private readonly Panel[] pages = new Panel[5];
        private List<Delivery> trips = new List<Delivery>();
        private List<IncidentReport> incidents = new List<IncidentReport>();
        private List<TruckMaintenance> maintenance = new List<TruckMaintenance>();
        private TruckSpecs specs = new TruckSpecs();
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public static void ShowFor(IWin32Window? owner, Truck truck, Action<Truck>? onEditDetails = null)
        {
            var dlg = new TruckProfileDialog(truck, onEditDetails);
            dlg.FormClosing += (s, e) => DarkModeKit.StopAutoComplete(dlg);
            try { dlg.ShowDialog(owner); }
            finally { DarkModeKit.StopAutoComplete(dlg); DarkModeKit.SafeDispose(dlg); }
        }
        public TruckProfileDialog(Truck truck, Action<Truck>? onEditDetails = null)
        {
            this.truck = truck;
            this.onEditDetails = onEditDetails;
            Text = $"Truck profile — {truck.PlateNumber}";
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", 9.5F);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            KeyPreview = true;
            ClientSize = new Size(D(1060), D(720));
            MinimumSize = new Size(D(860), D(600));
            BackColor = AppTheme.Canvas;
            body.BackColor = AppTheme.Canvas;
            LoadData();
            BuildHeader();
            var footer = new Panel { Dock = DockStyle.Bottom, Height = D(60), BackColor = AppTheme.Surface, Padding = new Padding(D(20), D(12), D(20), D(12)) };
            footer.Paint += (s, e) => { using var p = new Pen(AppTheme.Border); e.Graphics.DrawLine(p, 0, 0, footer.Width, 0); };
            var btnClose = new Button { Text = "Close", Dock = DockStyle.Right, Width = D(100), Cursor = Cursors.Hand, DialogResult = DialogResult.Cancel };
            UIThemeHelper.SetButtonRole(btnClose, "primary");
            var btnEdit = new Button { Text = "Edit truck details", Dock = DockStyle.Left, Width = D(160), Cursor = Cursors.Hand };
            UIThemeHelper.SetButtonRole(btnEdit, "secondary");
            btnEdit.Click += (s, e) => { Close(); onEditDetails?.Invoke(truck); };
            btnEdit.Visible = onEditDetails != null;
            footer.Controls.Add(btnClose);
            footer.Controls.Add(btnEdit);
            CancelButton = btnClose;
            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(tabs);
            Controls.Add(header);
            tabs.Badges = new[] { 0, trips.Count, incidents.Count, DocCount() };
            tabs.SelectedChanged += (s, e) => ShowPage(tabs.Selected);
            ShowPage(0);
            KeyDown += (s, e) => { if (e.Control && e.KeyCode == Keys.Tab) { tabs.Selected = (tabs.Selected + (e.Shift ? 3 : 1)) % 4; e.Handled = true; } };
            try { ThemeManager.StyleWindowChrome(this); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
        }
        private void LoadData()
        {
            try { trips = MJdatabase.GetDeliveries().Where(d => d.TruckId == truck.Id).OrderByDescending(d => d.DispatchDate).ThenByDescending(d => d.Id).ToList(); } catch { trips = new List<Delivery>(); }
            var ids = trips.Select(t => t.Id).ToHashSet();
            var numbers = trips.Select(t => t.DispatchNumber).ToHashSet(StringComparer.OrdinalIgnoreCase);
            try { incidents = MJdatabase.GetIncidentReports().Where(i => ids.Contains(i.DeliveryId) || numbers.Contains(i.DispatchNumber ?? "")).OrderByDescending(i => i.IncidentDate).ToList(); } catch { incidents = new List<IncidentReport>(); }
            try { maintenance = MJdatabase.GetTruckMaintenance(truck.Id); } catch { maintenance = new List<TruckMaintenance>(); }
            specs = MJdatabase.GetTruckSpecs(truck.Id);
        }
        private void BuildHeader()
        {
            header.Badge = "🚚";
            header.Title = truck.PlateNumber;
            string makeModel = string.Join(" ", new[] { specs.Make, specs.Model, specs.YearModel }.Where(x => !string.IsNullOrWhiteSpace(x)));
            header.Subtitle = string.Join("  ·  ", new[] { truck.TruckType, $"{truck.CapacityCrates:N0} crates", truck.CargoType, makeModel }.Where(x => !string.IsNullOrWhiteSpace(x)));
            header.Chips.Clear();
            bool blocked = MJdatabase.IsTruckBlockedStatus(truck.Status);
            bool busy = RowMenu.Is(truck.Status, "In Use", "On Trip");
            header.Chips.Add((truck.Status, blocked ? AppTheme.DangerSoft : busy ? AppTheme.InfoSoft : AppTheme.SuccessSoft, blocked ? AppTheme.DangerText : busy ? AppTheme.InfoText : AppTheme.SuccessText));
            header.Chips.Add(($"{trips.Count(t => RowMenu.Is(t.Status, "Completed"))} completed trips", AppTheme.AccentSoft, AppTheme.AccentText));
            void Due(string what, DateTime? when)
            {
                if (!when.HasValue) return;
                int days = (int)(when.Value.Date - DateTime.Today).TotalDays;
                if (days < 0) header.Chips.Add(($"{what} expired {when:MMM d, yyyy}", AppTheme.DangerSoft, AppTheme.DangerText));
                else if (days <= 30) header.Chips.Add(($"{what} due in {days} day(s)", AppTheme.WarningSoft, AppTheme.WarningText));
            }
            Due("Registration", specs.RegistrationExpiry);
            Due("Insurance", specs.InsuranceExpiry);
            header.Invalidate();
        }
        // Asset maintenance is out of scope (see project limitations), so the Maintenance tab is not shown.
        // Tab order: Specs, Dispatch history, Incident logs, Documents & photos -> page ids 0, 1, 3, 4.
        private void ShowPage(int tab)
        {
            int i = tab >= 2 ? tab + 1 : tab;
            if (pages[i] == null)
            {
                pages[i] = i switch { 0 => BuildSpecs(), 1 => BuildTrips(), 2 => BuildMaintenance(), 3 => BuildIncidents(), _ => BuildDocuments() };
                pages[i].Dock = DockStyle.Fill;
                pages[i].Visible = false;
                body.Controls.Add(pages[i]);
                if (ThemeManager.IsDark) DarkModeKit.Native(pages[i]);
            }
            foreach (var p in pages) if (p != null) p.Visible = p == pages[i];
        }
        private void Rebuild(int page)
        {
            if (pages[page] != null) { body.Controls.Remove(pages[page]); pages[page].Dispose(); pages[page] = null!; }
            tabs.Badges = new[] { 0, trips.Count, incidents.Count, DocCount() };
            tabs.Invalidate();
            BuildHeader();
            ShowPage(tabs.Selected);
        }
        private Panel Page() => new Panel { BackColor = AppTheme.Canvas, Padding = new Padding(D(20), D(16), D(20), D(12)) };
        // ------------------------------------------------------------------ specs
        private TextBox txtMake = null!, txtModel = null!, txtYear = null!, txtChassis = null!, txtEngine = null!, txtOdo = null!;
        private DateTimePicker dtpReg = null!, dtpIns = null!;
        private Panel BuildSpecs()
        {
            var page = Page();
            var stats = new StatRow();
            int completed = trips.Count(t => RowMenu.Is(t.Status, "Completed"));
            var last = trips.FirstOrDefault(t => RowMenu.Is(t.Status, "Completed", "In Transit"));
            stats.Add("Trips completed", completed.ToString("N0"), $"{trips.Count} dispatch(es) in total", AppTheme.Accent);
            stats.Add("Last trip", last == null ? "—" : last.DispatchDate.ToString("MMM d, yyyy"), last == null ? "no trips yet" : last.DispatchNumber, AppTheme.Info);
            stats.Add("Incidents", incidents.Count.ToString("N0"), $"{incidents.Sum(i => i.DamagedQuantity):N0} damaged crate(s)", incidents.Count > 0 ? AppTheme.Danger : AppTheme.Success);
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = AppTheme.Canvas, Padding = new Padding(0, D(14), 0, 0) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            // Left: fixed facts
            var facts = new ListView { View = View.Details, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.None, BorderStyle = BorderStyle.None, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, Font = new Font("Segoe UI", 10F) };
            facts.Columns.Add("k", D(150));
            facts.Columns.Add("v", D(240));
            void Fact(string k, string v) => facts.Items.Add(new ListViewItem(new[] { k, string.IsNullOrWhiteSpace(v) ? "—" : v }) { ForeColor = AppTheme.TextPrimary });
            Fact("Plate number", truck.PlateNumber);
            Fact("Truck type", truck.TruckType);
            Fact("Capacity", $"{truck.CapacityCrates:N0} crates");
            Fact("Cargo type", truck.CargoType);
            Fact("Status", truck.Status);
            Fact("Odometer", specs.OdometerKm > 0 ? $"{specs.OdometerKm:N0} km" : "");
            Fact("Remarks", truck.Remarks);
            var leftWrap = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Padding = new Padding(0, 0, D(10), 0) };
            var leftCard = ProfileKit.Card(facts, D(10));
            leftWrap.Controls.Add(leftCard);
            leftWrap.Controls.Add(ProfileKit.Section("Fleet record", "edit with \"Edit truck details\""));
            // Right: editable specs
            var form = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 9, AutoScroll = true, BackColor = AppTheme.Surface, Padding = new Padding(D(4)) };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            txtMake = ProfileKit.Box("e.g. Isuzu"); txtMake.Text = specs.Make;
            txtModel = ProfileKit.Box("e.g. NQR / Elf"); txtModel.Text = specs.Model;
            txtYear = ProfileKit.Box("e.g. 2019"); txtYear.Text = specs.YearModel; txtYear.MaxLength = 4;
            txtChassis = ProfileKit.Box("Chassis / VIN"); txtChassis.Text = specs.ChassisNumber; txtChassis.CharacterCasing = CharacterCasing.Upper;
            txtEngine = ProfileKit.Box("Engine number"); txtEngine.Text = specs.EngineNumber; txtEngine.CharacterCasing = CharacterCasing.Upper;
            txtOdo = ProfileKit.Box("km"); txtOdo.Text = specs.OdometerKm > 0 ? specs.OdometerKm.ToString() : ""; txtOdo.MaxLength = 7;
            InputGuards.DigitsOnly(txtYear, txtOdo);
            var makes = new AutoCompleteStringCollection();
            makes.AddRange(new[] { "Isuzu", "Hino", "Mitsubishi Fuso", "Foton", "Hyundai", "JAC", "Shacman", "Sinotruk", "Toyota", "Nissan", "UD Trucks" });
            txtMake.AutoCompleteCustomSource = makes; txtMake.AutoCompleteMode = AutoCompleteMode.SuggestAppend; txtMake.AutoCompleteSource = AutoCompleteSource.CustomSource;
            dtpReg = DatePicker(specs.RegistrationExpiry);
            dtpIns = DatePicker(specs.InsuranceExpiry);
            void Pair(int row, string a, Control ca, string b, Control cb)
            {
                var pa = new Panel { Dock = DockStyle.Fill, Height = D(58), Padding = new Padding(D(6), 0, D(6), 0), BackColor = AppTheme.Surface };
                var pb = new Panel { Dock = DockStyle.Fill, Height = D(58), Padding = new Padding(D(6), 0, D(6), 0), BackColor = AppTheme.Surface };
                ca.Dock = DockStyle.Top; cb.Dock = DockStyle.Top;
                pa.Controls.Add(ca); pa.Controls.Add(new Label { Text = a, Dock = DockStyle.Top, Height = D(22), Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary, TextAlign = ContentAlignment.BottomLeft });
                pb.Controls.Add(cb); pb.Controls.Add(new Label { Text = b, Dock = DockStyle.Top, Height = D(22), Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary, TextAlign = ContentAlignment.BottomLeft });
                form.RowStyles.Add(new RowStyle(SizeType.Absolute, D(60)));
                form.Controls.Add(pa, 0, row);
                form.Controls.Add(pb, 1, row);
            }
            Pair(0, "Make", txtMake, "Model", txtModel);
            Pair(1, "Year model", txtYear, "Odometer (km)", txtOdo);
            Pair(2, "Chassis number", txtChassis, "Engine number", txtEngine);
            Pair(3, "Registration (OR/CR) expires", dtpReg, "Insurance expires", dtpIns);
            var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, BackColor = AppTheme.Surface, Padding = new Padding(D(6), D(8), 0, 0) };
            var lblMsg = new Label { AutoSize = true, ForeColor = AppTheme.TextMuted, Margin = new Padding(D(6), D(8), 0, 0) };
            bar.Controls.Add(ProfileKit.Button("Save specifications", "primary", (s, e) => SaveSpecs(lblMsg)));
            bar.Controls.Add(lblMsg);
            form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            form.Controls.Add(bar, 0, 4);
            form.SetColumnSpan(bar, 2);
            var rightWrap = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Padding = new Padding(D(10), 0, 0, 0) };
            rightWrap.Controls.Add(ProfileKit.Card(form, D(10)));
            rightWrap.Controls.Add(ProfileKit.Section("Specifications", "make, numbers and paper expiry dates"));
            grid.Controls.Add(leftWrap, 0, 0);
            grid.Controls.Add(rightWrap, 1, 0);
            page.Controls.Add(grid);
            page.Controls.Add(stats);
            return page;
        }
        private DateTimePicker DatePicker(DateTime? value)
        {
            var d = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "MMM d, yyyy", ShowCheckBox = true, Font = new Font("Segoe UI", 10F) };
            d.Value = value ?? DateTime.Today;
            d.Checked = value.HasValue;
            return d;
        }
        private void SaveSpecs(Label msg)
        {
            int year = int.TryParse(txtYear.Text, out var y) ? y : 0;
            int odo = int.TryParse(txtOdo.Text, out var o) ? o : 0;
            var problems = new List<string>();
            if (txtYear.Text.Length > 0 && (year < 1980 || year > DateTime.Today.Year + 1)) problems.Add($"Year model must be between 1980 and {DateTime.Today.Year + 1}.");
            if (txtChassis.Text.Trim().Length is > 0 and < 5) problems.Add("Chassis number looks too short.");
            if (txtEngine.Text.Trim().Length is > 0 and < 4) problems.Add("Engine number looks too short.");
            if (odo > 0 && odo < specs.OdometerKm) problems.Add($"Odometer can't go down (last recorded {specs.OdometerKm:N0} km).");
            string? clash = null;
            try { clash = MJdatabase.FindTruckSpecClash(truck.Id, txtChassis.Text, txtEngine.Text); } catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
            if (clash != null) problems.Add(clash);
            if (problems.Count > 0)
            {
                msg.ForeColor = AppTheme.DangerText;
                msg.Text = problems[0];
                MessageBox.Show(this, "Please check:\n\n• " + string.Join("\n• ", problems), "Specifications", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show(this, $"Are you sure you want to update the specifications of {truck.PlateNumber}?", "Update specifications", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var updated = new TruckSpecs
            {
                TruckId = truck.Id,
                Make = txtMake.Text.Trim(),
                Model = txtModel.Text.Trim(),
                YearModel = txtYear.Text.Trim(),
                ChassisNumber = txtChassis.Text.Trim(),
                EngineNumber = txtEngine.Text.Trim(),
                OdometerKm = odo,
                RegistrationExpiry = dtpReg.Checked ? dtpReg.Value.Date : null,
                InsuranceExpiry = dtpIns.Checked ? dtpIns.Value.Date : null
            };
            try
            {
                MJdatabase.SaveTruckSpecs(updated);
                specs = updated;
                msg.ForeColor = AppTheme.SuccessText;
                msg.Text = "✓ Saved " + DateTime.Now.ToString("h:mm tt");
                BuildHeader();
            }
            catch (Exception ex)
            { ErrLog.Error("TruckProfileDialog", ex); 
                MessageBox.Show(this, "Couldn't save: " + ex.Message, "Specifications", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // ------------------------------------------------------------------ dispatch history
        private Panel BuildTrips()
        {
            var page = Page();
            var g = ProfileKit.Grid();
            g.Columns.AddRange(ProfileKit.Col("Date", "Date", 70), ProfileKit.Col("Dispatch", "Dispatch #", 80), ProfileKit.Col("Client", "Client", 110), ProfileKit.Col("Route", "Route", 120),
                ProfileKit.Col("Driver", "Driver", 100), ProfileKit.Col("Aides", "Aide(s)", 100), ProfileKit.Col("Crates", "Crates", 50, true), ProfileKit.Col("Status", "Status", 70));
            var search = ProfileKit.Box("Filter by dispatch #, client, route, driver or status…");
            void Fill()
            {
                string q = search.Text.Trim();
                g.Rows.Clear();
                foreach (var t in trips)
                {
                    string route = t.CleanRouteDisplay;
                    if (q.Length > 0 && !new[] { t.DispatchNumber, t.ClientName, route, t.DriverName, t.AideDisplay, t.Status }.Any(x => (x ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))) continue;
                    int r = g.Rows.Add(t.DispatchDate.ToString("yyyy-MM-dd"), t.DispatchNumber, t.ClientName, route, t.DriverName, t.AideDisplay, (t.TotalCrates > 0 ? t.TotalCrates : t.GCrate + t.ECrate).ToString("N0"), t.Status);
                    g.Rows[r].Tag = t;
                }
            }
            search.TextChanged += (s, e) => Fill();
            Fill();
            g.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && g.Rows[e.RowIndex].Tag is Delivery d) DispatchDetailsDialog.ShowFor(this, d); };
            var top = new Panel { Dock = DockStyle.Top, Height = D(40), BackColor = AppTheme.Canvas, Padding = new Padding(0, 0, 0, D(8)) };
            top.Controls.Add(search);
            page.Controls.Add(ProfileKit.Card(g, 1));
            page.Controls.Add(top);
            page.Controls.Add(ProfileKit.Section("Dispatch history", $"{trips.Count} dispatch(es), newest first · double-click to open"));
            return page;
        }
        // ------------------------------------------------------------------ maintenance
        private TruckMaintenance? editing;
        private Panel BuildMaintenance()
        {
            var page = Page();
            var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = AppTheme.Canvas };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            var g = ProfileKit.Grid();
            g.Columns.AddRange(ProfileKit.Col("Date", "Date", 70), ProfileKit.Col("Task", "Task", 150), ProfileKit.Col("Category", "Category", 75), ProfileKit.Col("Odo", "Odometer", 65, true),
                ProfileKit.Col("Cost", "Cost", 65, true), ProfileKit.Col("Next", "Next due", 70), ProfileKit.Col("By", "Shop / by", 80));
            foreach (var m in maintenance)
            {
                int r = g.Rows.Add(m.ServiceDate.ToString("yyyy-MM-dd"), m.Task, m.Category, m.OdometerKm > 0 ? $"{m.OdometerKm:N0}" : "—", m.Cost > 0 ? $"₱{m.Cost:N2}" : "—",
                    m.NextDueDate?.ToString("yyyy-MM-dd") ?? "—", string.IsNullOrWhiteSpace(m.ServiceProvider) ? m.CreatedBy : m.ServiceProvider);
                g.Rows[r].Tag = m;
                if (!string.IsNullOrWhiteSpace(m.Notes)) g.Rows[r].Cells["Task"].ToolTipText = m.Notes;
                if (m.NextDueDate.HasValue && m.NextDueDate.Value.Date < DateTime.Today) g.Rows[r].Cells["Next"].Style.ForeColor = AppTheme.DangerText;
            }
            var leftWrap = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Padding = new Padding(0, 0, D(10), 0) };
            leftWrap.Controls.Add(ProfileKit.Card(g, 1));
            leftWrap.Controls.Add(ProfileKit.Section("Maintenance log", $"{maintenance.Count} job(s), newest first · ₱{maintenance.Sum(m => m.Cost):N0} total"));
            // Entry form
            var f = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = AppTheme.Surface, Padding = new Padding(D(4)) };
            int fw = D(300);
            var lblMode = new Label { Text = "New maintenance entry", AutoSize = true, Font = new Font("Segoe UI Semibold", 10.5F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, 0, 0, D(4)) };
            var dtpDate = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "MMM d, yyyy", Width = fw, Font = new Font("Segoe UI", 10F), MaxDate = DateTime.Today.AddDays(1) };
            var cbTask = new ComboBox { Width = fw, Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDown };
            ProfileKit.Suggest(cbTask, "maintenance");
            var cbCat = new ComboBox { Width = fw, Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList };
            cbCat.Items.AddRange(new object[] { "Preventive", "Repair", "Inspection", "Tires", "Reefer / refrigeration", "Registration / compliance" });
            cbCat.SelectedIndex = 0;
            var txtOdoM = ProfileKit.Box("km reading"); txtOdoM.Width = fw; txtOdoM.Dock = DockStyle.None; txtOdoM.MaxLength = 7;
            var txtCost = ProfileKit.Box("0.00"); txtCost.Width = fw; txtCost.Dock = DockStyle.None;
            txtCost.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && !(e.KeyChar == '.' && !txtCost.Text.Contains('.'))) e.Handled = true; };
            InputGuards.DigitsOnly(txtOdoM);
            var txtShop = ProfileKit.Box("Shop or mechanic"); txtShop.Width = fw; txtShop.Dock = DockStyle.None;
            ProfileKit.Suggest(txtShop, "maintenance-provider");
            var dtpNext = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "MMM d, yyyy", Width = fw, Font = new Font("Segoe UI", 10F), ShowCheckBox = true, Checked = false };
            var txtNotes = ProfileKit.Box("Parts used, findings…"); txtNotes.Width = fw; txtNotes.Dock = DockStyle.None;
            ProfileKit.Suggest(txtNotes, "maintenance-notes");
            cbTask.TextChanged += (s, e) =>
            {
                string t = cbTask.Text.ToLowerInvariant();
                if (t.Contains("tire")) cbCat.SelectedItem = "Tires";
                else if (t.Contains("reefer") || t.Contains("aircon")) cbCat.SelectedItem = "Reefer / refrigeration";
                else if (t.Contains("registration") || t.Contains("emission")) cbCat.SelectedItem = "Registration / compliance";
                else if (t.Contains("repair") || t.Contains("replace")) cbCat.SelectedItem = "Repair";
                else if (t.Contains("inspection") || t.Contains("check")) cbCat.SelectedItem = "Inspection";
            };
            var lblMsg = new Label { AutoSize = true, MaximumSize = new Size(fw, 0), ForeColor = AppTheme.TextMuted, Margin = new Padding(0, D(4), 0, 0) };
            var btns = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, D(8), 0, 0) };
            Button? btnVoid = null;
            void Load(TruckMaintenance? m)
            {
                editing = m;
                lblMode.Text = m == null ? "New maintenance entry" : $"Editing entry from {m.ServiceDate:MMM d, yyyy}";
                dtpDate.Value = m?.ServiceDate ?? DateTime.Today;
                cbTask.Text = m?.Task ?? "";
                cbCat.SelectedItem = m?.Category ?? "Preventive";
                if (cbCat.SelectedIndex < 0) cbCat.SelectedIndex = 0;
                txtOdoM.Text = m != null && m.OdometerKm > 0 ? m.OdometerKm.ToString() : "";
                txtCost.Text = m != null && m.Cost > 0 ? m.Cost.ToString("0.00") : "";
                txtShop.Text = m?.ServiceProvider ?? "";
                dtpNext.Checked = m?.NextDueDate != null;
                dtpNext.Value = m?.NextDueDate ?? DateTime.Today.AddMonths(3);
                txtNotes.Text = m?.Notes ?? "";
                if (btnVoid != null) btnVoid.Visible = m != null;
                lblMsg.Text = "";
            }
            void Save()
            {
                var errs = new List<string>();
                string task = System.Text.RegularExpressions.Regex.Replace(cbTask.Text.Trim(), @"\s+", " ");
                if (task.Length == 0) errs.Add("Task is required (pick a suggestion or type one).");
                if (dtpDate.Value.Date > DateTime.Today) errs.Add("Service date can't be in the future.");
                int odo = int.TryParse(txtOdoM.Text, out var od) ? od : 0;
                decimal cost = decimal.TryParse(txtCost.Text, out var c) ? c : 0;
                if (txtCost.Text.Trim().Length > 0 && !decimal.TryParse(txtCost.Text, out _)) errs.Add("Cost must be a number.");
                if (cost < 0) errs.Add("Cost can't be negative.");
                if (dtpNext.Checked && dtpNext.Value.Date <= dtpDate.Value.Date) errs.Add("Next due date must be after the service date.");
                if (errs.Count > 0)
                {
                    lblMsg.ForeColor = AppTheme.DangerText;
                    lblMsg.Text = errs[0];
                    MessageBox.Show(this, "Please check:\n\n• " + string.Join("\n• ", errs), "Maintenance", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                int maxOdo = Math.Max(specs.OdometerKm, maintenance.Where(m => editing == null || m.Id != editing.Id).Select(m => m.OdometerKm).DefaultIfEmpty(0).Max());
                if (odo > 0 && odo < maxOdo && MessageBox.Show(this, $"The odometer reading ({odo:N0} km) is lower than the last recorded {maxOdo:N0} km. Save anyway?", "Odometer check", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                bool dupSameDay = maintenance.Any(m => (editing == null || m.Id != editing.Id) && m.ServiceDate.Date == dtpDate.Value.Date && string.Equals(m.Task, task, StringComparison.OrdinalIgnoreCase));
                if (dupSameDay && MessageBox.Show(this, $"\"{task}\" is already logged on {dtpDate.Value:MMM d, yyyy}. Add it again?", "Possible duplicate", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                if (MessageBox.Show(this, editing == null ? $"Are you sure you want to add \"{task}\" to {truck.PlateNumber}'s maintenance log?" : "Are you sure you want to update this maintenance entry?", editing == null ? "Add maintenance" : "Update maintenance", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                var rec = editing ?? new TruckMaintenance { TruckId = truck.Id };
                rec.ServiceDate = dtpDate.Value.Date; rec.Task = task; rec.Category = cbCat.Text; rec.OdometerKm = odo; rec.Cost = cost;
                rec.ServiceProvider = txtShop.Text.Trim(); rec.Notes = txtNotes.Text.Trim(); rec.NextDueDate = dtpNext.Checked ? dtpNext.Value.Date : null;
                try
                {
                    MJdatabase.SaveTruckMaintenance(rec);
                    maintenance = MJdatabase.GetTruckMaintenance(truck.Id);
                    specs = MJdatabase.GetTruckSpecs(truck.Id);
                    Rebuild(2);
                    if (pages[0] != null) { body.Controls.Remove(pages[0]); pages[0].Dispose(); pages[0] = null!; }
                }
                catch (Exception ex) { ErrLog.Error("TruckProfileDialog", ex);  MessageBox.Show(this, "Couldn't save: " + ex.Message, "Maintenance", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
            btns.Controls.Add(ProfileKit.Button("Save entry", "primary", (s, e) => Save()));
            btns.Controls.Add(ProfileKit.Button("New", "secondary", (s, e) => Load(null)));
            btnVoid = ProfileKit.Button("Void", "danger", (s, e) =>
            {
                if (editing == null) return;
                if (MessageBox.Show(this, $"Are you sure you want to void \"{editing.Task}\" ({editing.ServiceDate:MMM d, yyyy})?\n\nIt's hidden from the log but kept in the audit trail.", "Void maintenance entry", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                MJdatabase.VoidTruckMaintenance(editing.Id);
                maintenance = MJdatabase.GetTruckMaintenance(truck.Id);
                Rebuild(2);
            });
            btns.Controls.Add(btnVoid);
            Control L(string t) => new Label { Text = t, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, D(6), 0, D(2)) };
            f.Controls.AddRange(new Control[] { lblMode, L("Service date *"), dtpDate, L("Task *  (type to see suggestions)"), cbTask, L("Category"), cbCat, L("Odometer (km)"), txtOdoM, L("Cost (₱)"), txtCost, L("Shop / mechanic"), txtShop, L("Next due date"), dtpNext, L("Notes"), txtNotes, btns, lblMsg });
            Load(null);
            g.SelectionChanged += (s, e) => { if (g.CurrentRow?.Tag is TruckMaintenance m && g.Focused) Load(m); };
            g.CellClick += (s, e) => { if (e.RowIndex >= 0 && g.Rows[e.RowIndex].Tag is TruckMaintenance m) Load(m); };
            var rightWrap = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, Padding = new Padding(D(10), 0, 0, 0) };
            rightWrap.Controls.Add(ProfileKit.Card(f, D(12)));
            rightWrap.Controls.Add(ProfileKit.Section("Log a job"));
            split.Controls.Add(leftWrap, 0, 0);
            split.Controls.Add(rightWrap, 1, 0);
            page.Controls.Add(split);
            return page;
        }
        // ------------------------------------------------------------------ documents
        private int DocCount() { try { return MJdatabase.GetAttachments("Truck", truck.Id).Count; } catch { return 0; } }
        private Panel BuildDocuments()
        {
            var page = Page();
            var panel = new AttachmentPanel("Registration / OR-CR") { Dock = DockStyle.Fill };
            panel.SetOwner("Truck", truck.Id, truck.PlateNumber);
            panel.AttachmentsChanged += (s, e) => { tabs.Badges = new[] { 0, trips.Count, incidents.Count, panel.Items.Count }; tabs.Invalidate(); };
            page.Controls.Add(ProfileKit.Card(panel, 1));
            page.Controls.Add(ProfileKit.Section("Documents & photos", "OR/CR, insurance, inspection photos, repair receipts · double-click to view"));
            return page;
        }
        // ------------------------------------------------------------------ incidents
        private Panel BuildIncidents()
        {
            var page = Page();
            var stats = new StatRow();
            stats.Add("Incidents", incidents.Count.ToString("N0"), "on this truck's trips", incidents.Count > 0 ? AppTheme.Danger : AppTheme.Success);
            stats.Add("Damaged crates", incidents.Sum(i => i.DamagedQuantity).ToString("N0"), "all incidents", AppTheme.Warning);
            stats.Add("Estimated loss", $"₱{incidents.Sum(i => i.EstimatedCost):N0}", "all incidents", AppTheme.Danger);
            var open = incidents.Count(i => !RowMenu.Is(i.Status, "Resolved", "Closed"));
            stats.Add("Still open", open.ToString("N0"), "not yet resolved", open > 0 ? AppTheme.Warning : AppTheme.Success);
            var g = ProfileKit.Grid();
            g.Columns.AddRange(ProfileKit.Col("Date", "Date", 70), ProfileKit.Col("Dispatch", "Dispatch #", 80), ProfileKit.Col("Cause", "Incident type / cause", 150), ProfileKit.Col("Qty", "Damaged", 55, true),
                ProfileKit.Col("Cost", "Est. cost", 65, true), ProfileKit.Col("Crew", "Chargeable to crew", 70), ProfileKit.Col("Status", "Status", 65));
            var tripById = trips.ToDictionary(t => t.Id);
            foreach (var i in incidents)
            {
                int r = g.Rows.Add(i.IncidentDate.ToString("yyyy-MM-dd"), i.DispatchNumber, i.Cause, i.DamagedQuantity.ToString("N0"), $"₱{i.EstimatedCost:N2}", i.ChargeableToCrew ? "Yes" : "No", i.Status);
                g.Rows[r].Tag = tripById.TryGetValue(i.DeliveryId, out var d) ? d : null;
                if (!string.IsNullOrWhiteSpace(i.Description)) g.Rows[r].Cells["Cause"].ToolTipText = i.Description;
            }
            g.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && g.Rows[e.RowIndex].Tag is Delivery d) DispatchDetailsDialog.ShowFor(this, d); };
            var gap = new Panel { Dock = DockStyle.Top, Height = D(14), BackColor = AppTheme.Canvas };
            page.Controls.Add(ProfileKit.Card(g, 1));
            page.Controls.Add(ProfileKit.Section("Incident logs", "newest first · double-click to open the dispatch"));
            page.Controls.Add(gap);
            page.Controls.Add(stats);
            return page;
        }
    }
}
