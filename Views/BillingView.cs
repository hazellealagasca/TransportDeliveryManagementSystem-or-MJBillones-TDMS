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
    public partial class BillingView : UserControl, IRefreshable, IUnsavedForm
    {
        private Invoice? selectedInvoice = null;
        private List<Invoice> allInvoices = new List<Invoice>();
        private string currentSortColumn = "InvoiceNumber";
        private bool isSortAscending = true;
        private List<Delivery> completedDeliveries = new List<Delivery>();
        /// <summary>Cancelled trips the client is still charged for (trip id → agreed fee). Only these cancelled trips can be billed.</summary>
        private Dictionary<int, decimal> chargeFees = new Dictionary<int, decimal>();
        private bool IsChargeableCancelled(Delivery d) => string.Equals(d.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) && chargeFees.ContainsKey(d.Id);
        private bool isBindingForm = false;
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;
        private float currentFormWidth = 420F;

        public bool PrefillForDelivery(Delivery del)
        {
            if (del == null) return false;
            del = DispatchGate.Fresh(del);
            if (!DispatchGate.RequireCompleted(FindForm(), del, "bill it")) return false;
            // Strict binding: only a trip with a saved trip report, not billed on a statement or another invoice.
            string? block = !(del.Id < 0 ? SpecialTripHasReport(-del.Id) : MJdatabase.HasTripReport(del.Id)) ? $"{del.DispatchNumber} has no saved trip report yet. File it first (Operations → Trip reports), then invoice the trip."
                : del.Id > 0 && MJdatabase.GetLiveInvoiceForTrip(del.Id) is LiveTripInvoice liveInv ? $"{del.DispatchNumber} is already invoiced: {liveInv.Describe()}."
                : del.Id < 0 && MJdatabase.SpecialTripHasActiveInvoice(-del.Id) ? $"{del.DispatchNumber} already has its service invoice."
                : null;
            if (block != null) { MessageBox.Show(FindForm(), block, "Can't invoice this trip", MessageBoxButtons.OK, MessageBoxIcon.Information); return false; }
            SetFormVisibility(true);
            // Always a NEW invoice for this trip: nothing carried over from the invoice that was selected in the list.
            ClearFields();
            if (cbSoa != null) chkDirectTrip.Checked = true; // Nov 10: opened for one trip (Trip reports / special trip): the trip form
            int idx = completedDeliveries.FindIndex(d => d.Id == del.Id);
            if (idx >= 0 && idx < cbDelivery.Items.Count)
            {
                cbDelivery.SelectedIndex = idx;
            }
            else
            {
                completedDeliveries.Insert(0, del);
                cbDelivery.Items.Insert(0, $"{del.DispatchNumber} - {del.LoadingScheduleInfo} (Driver: {del.DriverName})");
                cbDelivery.SelectedIndex = 0;
            }
            if (del.Id < 0) cbDelivery.Enabled = false; // Nov 6: opened from the special trip: this invoice is for that trip only
            return true;
        }

        // ------------------------------------------------------------------ Nov 6 workflow: invoices issued from Trip Reports
        private List<Delivery> batchQueue = new List<Delivery>();
        private int batchIndex;
        private Action? backToTripReports;

        /// <summary>
        /// Opens the invoice form for each trip in turn (one service invoice per trip): the next one opens after each save.
        /// "Back to Trip Reports" stops the queue; trips not saved yet get no invoice (nothing is made until Save).
        /// </summary>
        public void StartBatch(IReadOnlyList<Delivery> trips, Action backToTripReports)
        {
            this.backToTripReports = backToTripReports;
            batchQueue = (trips ?? new List<Delivery>()).Where(t => t != null).ToList();
            batchIndex = 0;
            SetBackLabel(true);
            OpenBatchItem();
        }

        private string? batchBackLabel;

        /// <summary>
        /// "Issue Invoice" from a statement: opens the service invoice form for each of its trips that has no invoice yet, in turn. The Amount and
        /// Fuel surcharge come from the statement and are locked; only the invoice number is typed. Nothing is made until each invoice is saved.
        /// </summary>
        public void StartForStatement(int soaId)
        {
            if (OpenInvoiceForSoa(soaId)) { batchBackLabel = null; return; } // Nov 10: a statement is billed with one invoice
            var trips = new List<Delivery>();
            foreach (int id in MJdatabase.GetSoaDeliveryIdsWithoutInvoice(soaId)) { var d = MJdatabase.GetDeliveryById(id); if (d != null) trips.Add(d); }
            if (trips.Count == 0) { QuietBox.Show("Every trip of this statement already has its service invoice.", "Issue invoice", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            batchBackLabel = "←  Back to invoices";
            StartBatch(trips, () => { batchBackLabel = null; LoadData(); });
        }

        private void OpenBatchItem()
        {
            while (batchIndex < batchQueue.Count)
            {
                if (PrefillForDelivery(batchQueue[batchIndex]))
                {
                    if (batchQueue.Count > 1) lblFormHeader.Text = $"Invoice {batchIndex + 1} of {batchQueue.Count} · {batchQueue[batchIndex].DispatchNumber}";
                    return;
                }
                batchIndex++; // this trip can't be invoiced (the message said why): go on with the next one
            }
            if (batchQueue.Count > 1) ToastHelper.ShowToast(this, $"Done: invoices were issued for the selected trips", ToastType.Success);
            ReturnToTripReportsIfOpenedFromThere();
        }

        /// <summary>Ends the queue and goes back to Trip Reports when Billing was opened from there.</summary>
        private void ReturnToTripReportsIfOpenedFromThere()
        {
            var back = backToTripReports;
            batchQueue = new List<Delivery>();
            batchIndex = 0;
            backToTripReports = null;
            SetBackLabel(false);
            back?.Invoke();
        }

        private void SetBackLabel(bool fromTripReports)
        {
            if (btnCloseForm == null) return;
            btnCloseForm.Text = fromTripReports ? (batchBackLabel ?? "←  Back to Trip Reports") : "←  Back to invoices";
            btnCloseForm.Width = Math.Max(btnCloseForm.Width, TextRenderer.MeasureText(btnCloseForm.Text, btnCloseForm.Font).Width + 36);
        }

        public BillingView()
        {
            InitializeComponent();
            PageKit.SummaryUnderTitle(pnlSummary, lblSummary, pnlGridHeaderBar, lblGridHeader); // totals line under the title, not cut off at the bottom
            Pal.RemapTree(this);
            gridInvoices.BringToFront();
            SetupGridColumns();
            InitListChips();
            SetupContextMenu();
            gridInvoices.ColumnHeaderMouseClick += (s, e) => {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridInvoices.Columns.Count) return;
                string col = gridInvoices.Columns[e.ColumnIndex].DataPropertyName;
                if (string.IsNullOrEmpty(col)) return;
                if (currentSortColumn == col) isSortAscending = !isSortAscending;
                else { currentSortColumn = col; isSortAscending = true; }
                FilterGridData();
            };
            LoadData();

            btnPrint.Visible = false;

            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("🧾 Make a statement of account (SOA)", null, (s, e) => OpenStatementOfAccount());
            menuActions.Items.Add("📋 Cancelled as typing mistakes…", null, (s, e) => { using var dlg = new Forms.TypingMistakeReportDialog(); dlg.ShowDialog(FindForm()); });
            menuActions.Items.Add("📊 Export invoices to CSV", null, (s, e) => DataExportHelper.ExportToCsv(gridInvoices, "Billing_Invoices", "Invoices and Accounts Receivable"));
            menuActions.Items.Add("🖨️ Print invoice register", null, (s, e) => BtnPrint_Click(this, EventArgs.Empty));
            var btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);
            pnlGridActionsFlow.Controls.Add(btnActions);

            txtInvoiceNumber.Text = "";
            txtInvoiceNumber.TextChanged += (s, e) =>
            {
                if (!isBindingForm)
                {
                    HandleInvoiceNumberInput();
                }
            };

            currentFormWidth = UIThemeHelper.GetAdaptiveDrawerWidth(this, 420F, 480F);
            InitializeFormToggle();
            SetButtonStates(false);
            UIThemeHelper.ApplyTheme(this);
                    InitFullPageForm();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && pnlForm.Visible && EscapeGuard.AllowClose(this))
            {
                SetFormVisibility(false);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitializeFormToggle()
        {
            btnToggleForm = new Button
            {
                Text = "📝 + New / form ▾",
                Size = new Size(150, 30),
                BackColor = Pal.C(30, 41, 59),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 4)
            };
            btnToggleForm.FlatAppearance.BorderSize = 0;
            btnToggleForm.Click += (s, e) => { if (pnlForm.Visible) { if (FormGuard.ConfirmDiscard(this)) SetFormVisibility(false); } else OpenNewRecordForm(); };
            pnlGridActionsFlow.Controls.Add(btnToggleForm);
            pnlGridActionsFlow.Controls.SetChildIndex(btnToggleForm, 0);

            btnCloseForm = new Button
            {
                Text = "✕ Close",
                Dock = DockStyle.Right,
                Width = 72,
                Height = 30,
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCloseForm.FlatAppearance.BorderSize = 0;
            btnCloseForm.Click += (s, e) => { if (FormGuard.ConfirmDiscard(this)) { SetFormVisibility(false); ReturnToTripReportsIfOpenedFromThere(); } };
            pnlFormHeaderBar.Controls.Add(btnCloseForm);

            SetFormVisibility(false);
        }


        // ------------------------------------------------------------------ Full-page form (same tab, back button, section cards)
        private FullPageFormKit? formKit;
        /// <summary>Live check of the typed Service Invoice No.: unique in any spelling, same-digits warning with a confirmation tick (plan 17).</summary>
        private NumberFieldGuard billNumberGuard = null!;
        private void InitFullPageForm()
        {
            billNumberGuard = new NumberFieldGuard(txtInvoiceNumber, NumberKind.SI, () => selectedInvoice?.Id ?? 0);
            billNumberGuard.Changed += () => ScheduleBillGuide();
            BuildSoaPicker();
            formKit =new FullPageFormKit(this, tblMasterContent, pnlForm, pnlGridCard, pnlFormHeaderBar, lblFormHeader, btnCloseForm, pnlFormBody, pnlFormActionsBar, "Back to invoices");
            formKit.Build(new[]
            {
                new FullPageFormKit.Section("Invoice & order", "Which statement of account (or trip) this invoice bills and who it is for.",lblInvoiceNumber, txtInvoiceNumber, billNumberGuard.Message, billNumberGuard.Tick, lblSoa, cbSoa, chkDirectTrip, lblDel, cbDelivery, lblLoadingSchedule, txtLoadingSchedule, lblClient, cbClient),
                new FullPageFormKit.Section("Amounts", "Gross amount, deductions and the net amount due.", pnlClientSpacer, tblAmounts, pnlAmountsSpacer, pnlNetDue, pnlNetDueSpacer, lblTariffMatch, BuildBillAdjustPanel())
            });
            InitBillGuide();
            if (Features.On(Features.PaymentsWorkbench)) ApplyBillWorkbench(); // Ledger Desk layout (Settings → Features can switch it off)
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = "＋ New invoice";
                UIThemeHelper.SetButtonRole(btnToggleForm, "primary");
            }
            gridInvoices.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || gridInvoices.Rows[e.RowIndex].DataBoundItem is not Invoice) return;
                gridInvoices.ClearSelection();
                gridInvoices.Rows[e.RowIndex].Selected = true;
                SetFormVisibility(true);
            };
            ApplyInvoiceSource();
            SetFormVisibility(false);
        }
        private void OpenNewRecordForm()
        {
            if (keep.Kept) { keep.Resume(this, SetFormVisibility); return; } // a kept draft comes back as it was left, never blank
            ClearFields();
            ReloadSoaChoices(); // Nov 10: the statements that can be billed now
            lblFormHeader.Text = "New invoice";
            SetFormVisibility(true);
        }
        // Nov 6: unsaved-typing tracking for the add / edit form (see FormGuard).
        private FormDirtyTracker? formDirty;
        private readonly KeptDraftState keep = new KeptDraftState();
        public bool FormIsOpenAndDirty => pnlForm.Visible && (formDirty?.IsDirty ?? false);
        public void DiscardAndCloseForm() => SetFormVisibility(false);
        public bool HasKeptDraft => keep.Kept;
        public void KeepAndCloseForm()
        {
            // Nov 9: "Keep for later": hide the form but leave the typing in the fields; it reopens when the person returns.
            keep.Keeping = true;
            try { SetFormVisibility(false); } finally { keep.Keeping = false; }
            keep.MarkKept(this, btnToggleForm, () => keep.Resume(this, SetFormVisibility));
        }
        private void SetFormVisibility(bool visible)
        {
            // Nov 6: a form is blank whenever it is opened or left.
            if (!visible && pnlForm.Visible && IsHandleCreated && !keep.Keeping) ClearFields();
            if (visible) formDirty ??= new FormDirtyTracker(pnlForm);
            if (!keep.Keeping && !(visible && keep.Resuming)) formDirty?.Reset();
            if (visible) keep.Restored(btnToggleForm);
            if (formKit != null) formKit.Show(visible);
            else pnlForm.Visible = visible;
            if (btnToggleForm != null) btnToggleForm.Visible = !visible;
            OnBillFormShown(visible);
            MainForm.Instance?.NotifyDrawerToggled(visible);
        }

        public void RefreshData()
        {
            LoadData();
        }

        private void SetupGridColumns()
        {
            gridInvoices.AutoGenerateColumns = false;
            gridInvoices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridInvoices.ScrollBars = ScrollBars.Both;
            gridInvoices.AllowUserToResizeColumns = true;
            gridInvoices.ShowCellToolTips = false;
            gridInvoices.Columns.Clear();
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceNumber", HeaderText = "Invoice #", FillWeight = 12F, MinimumWidth = 100, ReadOnly = true });
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SoaNumber", HeaderText = "SOA #", FillWeight = 14F, MinimumWidth = 110, ReadOnly = true });
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DispatchNumber", HeaderText = "Dispatch #", FillWeight = 12F, MinimumWidth = 100, ReadOnly = true });
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = "Client", FillWeight = 18F, MinimumWidth = 120, ReadOnly = true });
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "IssueDate", HeaderText = "Date", DefaultCellStyle = { Format = "MM/dd/yyyy" }, FillWeight = 10F, MinimumWidth = 85, ReadOnly = true });
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Freight", HeaderText = "Amount", DefaultCellStyle = { Format = "N2" }, FillWeight = 12F, MinimumWidth = 95, ReadOnly = true });
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FuelSurcharge", HeaderText = "Fuel surcharge", DefaultCellStyle = { Format = "N2" }, FillWeight = 12F, MinimumWidth = 95, ReadOnly = true });
            gridInvoices.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", FillWeight = 10F, MinimumWidth = 80, ReadOnly = true });
            gridInvoices.CellFormatting -= GridInvoices_CellFormatting;
            gridInvoices.CellFormatting += GridInvoices_CellFormatting;
        }

        private void GridInvoices_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (gridInvoices.Columns[e.ColumnIndex].DataPropertyName == "Status" && e.Value != null)
            {
                string status = e.Value.ToString() ?? "";
                if (status.Equals("Paid", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Pal.C(220, 252, 231);
                    e.CellStyle.ForeColor = Pal.C(22, 101, 52);
                    e.CellStyle.Font = FontCache.Get(gridInvoices.Font, FontStyle.Bold);
                }
                else if (status.Equals("Draft", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Pal.C(254, 243, 199);
                    e.CellStyle.ForeColor = Pal.C(180, 83, 9);
                    e.CellStyle.Font = FontCache.Get(gridInvoices.Font, FontStyle.Bold);
                }
                else if (status.Equals("Unpaid", StringComparison.OrdinalIgnoreCase) && gridInvoices.Rows[e.RowIndex].DataBoundItem is Invoice od && od.DueDate.Date < DateTime.Today)
                {
                    // Nov 6 workflow: "Overdue" is shown from the due date; the stored status stays Unpaid until it is Paid.
                    e.Value = "Unpaid · overdue";
                    e.FormattingApplied = true;
                    e.CellStyle.BackColor = Pal.C(254, 226, 226);
                    e.CellStyle.ForeColor = Pal.C(185, 28, 28);
                    e.CellStyle.Font = FontCache.Get(gridInvoices.Font, FontStyle.Bold);
                }
                else if (status.Equals("Unpaid", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Pal.C(241, 245, 249);
                    e.CellStyle.ForeColor = Pal.C(51, 65, 85);
                }
            }
        }

        private void CbDelivery_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (isBindingForm) return;
            if (cbDelivery.SelectedIndex >= 0 && cbDelivery.SelectedIndex < completedDeliveries.Count)
            {
                var del = completedDeliveries[cbDelivery.SelectedIndex];
                ApplyTripLock(); // Nov 7: the trip is fixed once it is picked
                var schedules = MJdatabase.GetLoadingSchedules();
                var ls = schedules.FirstOrDefault(s => s.Id == del.LoadingScheduleId);

                txtLoadingSchedule.Text = TripReportSummary(del); // Nov 7: the completed trip report of this trip (not the loading schedule)

                // The trip's own client comes first (parsing the loading info split names that contain " - ").
                string clientName = (del.ClientName ?? "").Trim();
                if (clientName.Length > 0) { }
                else if (ls != null && !string.IsNullOrWhiteSpace(ls.ClientName))
                {
                    clientName = ls.ClientName.Trim();
                }
                else if (!string.IsNullOrWhiteSpace(del.LoadingScheduleInfo))
                {
                    string raw = del.LoadingScheduleInfo.Trim();
                    if (raw.Contains("-"))
                    {
                        var parts = raw.Split(new[] { '-' }, 2);
                        raw = parts.Length > 1 ? parts[1].Trim() : raw;
                    }
                    int pIdx = raw.LastIndexOf('(');
                    clientName = pIdx > 0 ? raw.Substring(0, pIdx).Trim() : raw.Trim();
                }

                if (!string.IsNullOrWhiteSpace(clientName))
                {
                    if (!cbClient.Items.Contains(clientName))
                    {
                        cbClient.Items.Add(clientName);
                    }
                    cbClient.SelectedItem = clientName;
                    cbClient.Text = clientName;
                }

                string tariffDesc;
                decimal calculatedAmount;
                // Nov 9 (statement first): a trip already on a Statement of Account takes its Amount and Fuel surcharge from the statement, locked.
                if (ApplyStatementLock(del)) { RecalculateNet(sender, e); return; }
                if (IsChargeableCancelled(del))
                {
                    // Cancelled but chargeable: bill the agreed fee recorded at cancellation; no incident deductions.
                    calculatedAmount = chargeFees[del.Id];
                    tariffDesc = "Cancelled – chargeable (agreed fee)";
                    lblTariffMatch.Text = $"✓ {tariffDesc} -> PHP {calculatedAmount:N2}";
                    lblTariffMatch.Visible = true;
                    if (calculatedAmount > numTotalAmount.Maximum) numTotalAmount.Maximum = calculatedAmount * 2;
                    numTotalAmount.SetSafe(Math.Max(numTotalAmount.Minimum, Math.Min(numTotalAmount.Maximum, calculatedAmount)));
                    numFuel.SetSafe(0);
                    RecalculateNet(sender, e);
                    return;
                }
                if (del.Id < 0) // Nov 6: a special trip is billed its trip charge
                {
                    calculatedAmount = SpecialCharge(del);
                    lblTariffMatch.Text = $"Special trip charge -> PHP {calculatedAmount:N2}";
                    lblTariffMatch.Visible = true;
                    if (calculatedAmount > numTotalAmount.Maximum) numTotalAmount.Maximum = calculatedAmount * 2;
                    numTotalAmount.SetSafe(Math.Max(numTotalAmount.Minimum, Math.Min(numTotalAmount.Maximum, calculatedAmount)));
                    numFuel.SetSafe(0);
                    RecalculateNet(sender, e);
                    return;
                }
                calculatedAmount = 0m; tariffDesc = "Type the Amount for this trip."; // Nov 10: no Rate Matrix amounts, the Amount is always typed
                lblTariffMatch.Text = calculatedAmount > 0 ? $"✓ {tariffDesc} -> PHP {calculatedAmount:N2}" : tariffDesc;
                lblTariffMatch.Visible = true;

                if (calculatedAmount > numTotalAmount.Maximum) numTotalAmount.Maximum = calculatedAmount * 2;
                numTotalAmount.SetSafe(Math.Max(numTotalAmount.Minimum, Math.Min(numTotalAmount.Maximum, calculatedAmount)));

                // Nov 7: no deductions on an invoice. The fuel surcharge is typed (the Rate Matrix only gives the freight tariff).
                numFuel.SetSafe(0);

                RecalculateNet(sender, e);
            }
        }

        /// <summary>
        /// True when the trip is on a live statement: the Amount and Fuel surcharge are filled from the statement line and locked
        /// (disabled), so the invoice equals the statement. Otherwise both boxes are unlocked.
        /// </summary>
        private bool ApplyStatementLock(Delivery del)
        {
            StatementTripLine? line = null;
            try { line = del.Id > 0 ? MJdatabase.GetStatementLineForTrip(del.Id) : MJdatabase.GetStatementLineForTrip(0, -del.Id); }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            if (line == null)
            {
                numTotalAmount.Enabled = true; numFuel.Enabled = true;
                return false;
            }
            if (line.Freight > numTotalAmount.Maximum) numTotalAmount.Maximum = line.Freight * 2;
            numTotalAmount.SetSafe(line.Freight);
            numFuel.SetSafe(Math.Min(numFuel.Maximum, line.Fuel));
            numTotalAmount.Enabled = false; numFuel.Enabled = false;
            lblTariffMatch.Text = $"🔒 On statement {line.SoaNumber}: Amount PHP {line.Freight:N2} + Fuel surcharge PHP {line.Fuel:N2} come from the statement and can't be changed here";
            lblTariffMatch.Visible = true;
            return true;
        }

        /// <summary>One line about the trip's completed trip report: its number, the date it was completed, crates and who received.</summary>
        private string TripReportSummary(Delivery del)
        {
            try
            {
                TripReport? tr;
                if (del.Id < 0) tr = MJdatabase.GetTripReports().Where(t => t.SpecialTripId == -del.Id && !string.Equals(t.Status, TripReportStatus.Draft, StringComparison.OrdinalIgnoreCase)).OrderByDescending(t => t.Id).FirstOrDefault();
                else MJdatabase.GetTripReportsByDelivery().TryGetValue(del.Id, out tr);
                if (tr == null) return IsChargeableCancelled(del) ? "(Cancelled trip: it has no trip report)" : "(No completed trip report yet)";
                string who = (tr.StoreManagerName ?? "").Trim();
                return $"{tr.TripReportNumber} · completed {tr.ReportDate:dd-MMM-yyyy} · delivered {tr.DeliveredCrates:N0}, returned {tr.ReturnedCrates:N0}" + (who.Length > 0 ? $" · received by {who}" : "");
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); return ""; }
        }

        /// <summary>Nov 7: the invoiced trip can't be changed: once it is picked (or the invoice is saved) the box is locked; Clear form unlocks it.</summary>
        private void ApplyTripLock()
        {
            cbDelivery.Enabled = selectedInvoice == null && cbDelivery.SelectedIndex < 0;
        }

        private void UpdateClientContactHint()
        {
            string cname = cbClient.Text.Trim();
            if (!string.IsNullOrWhiteSpace(cname))
            {
                var cl = MJdatabase.GetClientByCompanyName(cname);
                if (cl != null && !string.IsNullOrEmpty(cl.ContactNumber))
                {
                    lblClient.Text = $"Client: (📞 {cl.ContactPerson} {cl.ContactNumber})";
                    return;
                }
            }
            lblClient.Text = "Client Company:";
        }

        private void RecalculateNet(object? sender, EventArgs e)
        {
            if (isBindingForm) return;
            decimal net = numTotalAmount.Value + numFuel.Value; // stored as the invoice's total; never shown as a computed figure on the invoice
            lblNetAmount.Text = "";
            DateTime baseDate = selectedInvoice != null ? selectedInvoice.IssueDate : DateTime.Today;
            lblDueDate.Text = $"Payment Due Date (15 Days): {(selectedInvoice != null ? selectedInvoice.DueDate : baseDate.AddDays(15)):MM/dd/yyyy}";
        }

        private void GridInvoices_SelectionChanged(object? sender, EventArgs e)
        {
            if (pnlForm.Visible) return; // the list is hidden while the form is open; keep what is being typed
            if (gridInvoices.SelectedRows.Count > 0 && gridInvoices.SelectedRows[0].DataBoundItem is Invoice inv)
            {
                BindInvoiceToForm(inv);
            }
        }

        private void HandleInvoiceNumberInput()
        {
            string invNum = txtInvoiceNumber.Text.Trim();
            if (string.IsNullOrWhiteSpace(invNum))
            {
                selectedInvoice = null;
                lblFormHeader.Text = "Generate Service Invoice";
                SetButtonStates(false);
                return;
            }

            // A cancelled invoice is never opened for editing from its number (the checklist shows the number as taken).
            var matched = allInvoices.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.InvoiceNumber) && i.InvoiceNumber.Equals(invNum, StringComparison.OrdinalIgnoreCase) && !RowMenu.Is(i.Status, PayStatus.Cancelled));
            if (matched != null)
            {
                BindInvoiceToForm(matched);
                for (int i = 0; i < gridInvoices.Rows.Count; i++)
                {
                    if (gridInvoices.Rows[i].DataBoundItem is Invoice rowInv && rowInv.Id == matched.Id)
                    {
                        gridInvoices.ClearSelection();
                        gridInvoices.Rows[i].Selected = true;
                        break;
                    }
                }
            }
            else
            {
                selectedInvoice = null;
                lblFormHeader.Text = $"Invoice #{invNum} (New)";
                SetButtonStates(false);
            }
        }

        private void BindInvoiceToForm(Invoice inv)
        {
            isBindingForm = true;
            try
            {
                selectedInvoice = inv;

                int dIdx = completedDeliveries.FindIndex(d => d.Id == selectedInvoice.DeliveryId);
                if (dIdx >= 0 && dIdx < cbDelivery.Items.Count) cbDelivery.SelectedIndex = dIdx;

                {
                    var invDel = selectedInvoice.DeliveryId > 0 ? MJdatabase.GetDeliveryById(selectedInvoice.DeliveryId) : null;
                    txtLoadingSchedule.Text = invDel != null ? TripReportSummary(invDel)
                        : (!string.IsNullOrWhiteSpace(selectedInvoice.TripReportNumber) ? selectedInvoice.TripReportNumber : "(Trip report recorded on the invoice)");
                }

                if (selectedInvoice.SoaId > 0 && selectedInvoice.DeliveryId <= 0 && string.IsNullOrWhiteSpace(selectedInvoice.DispatchNumber))
                {
                    // Nov 10: an invoice that bills a whole statement shows that statement's number and trips.
                    try
                    {
                        var own = MJdatabase.GetStatementOfAccountById(selectedInvoice.SoaId);
                        if (own != null)
                        {
                            int n = MJdatabase.GetStatementOfAccountItems(own.Id).Where(i => i.DeliveryId > 0 || i.SpecialTripId > 0).Select(i => (i.DeliveryId, i.SpecialTripId)).Distinct().Count();
                            txtLoadingSchedule.Text = $"{own.SoaNumber} · {n} trip{(n == 1 ? "" : "s")} · {own.PeriodCovered}";
                        }
                    }
                    catch (Exception ex) { ErrLog.Swallowed(ex); }
                }
                txtInvoiceNumber.Text = selectedInvoice.InvoiceNumber;
                cbClient.Text = selectedInvoice.ClientName;
                numTotalAmount.SetSafe(Math.Min(numTotalAmount.Maximum, selectedInvoice.Freight)); // Amount (freight tariff); the fuel surcharge is on top
                numFuel.SetSafe(Math.Min(numFuel.Maximum, selectedInvoice.FuelSurcharge));
                RecalculateNet(this, EventArgs.Empty);
                lblDueDate.Text = $"Payment Due Date (15 Days): {selectedInvoice.DueDate:MM/dd/yyyy}";
                lblFormHeader.Text = $"Invoice #{selectedInvoice.InvoiceNumber}";
                ApplyTripLock(); // a saved invoice keeps its trip
                ApplyInvoiceSource(); // a saved invoice shows the trip box, never the statement picker
            }
            finally
            {
                isBindingForm = false;
            }
            SetButtonStates(true);
        }

        private void LoadData()
        {
            try
            {
                allInvoices = MJdatabase.GetInvoices();
                billingPaid.Clear(); // the summary reads the payments again
                var invoicedDeliveryIds = new HashSet<int>(allInvoices.Select(inv => inv.DeliveryId));

                var clients = MJdatabase.GetLookupItems("Client Company");
                var allSchedules = MJdatabase.GetLoadingSchedules();
                var clientSet = new HashSet<string>(clients, StringComparer.OrdinalIgnoreCase);
                foreach (var s in allSchedules)
                {
                    if (!string.IsNullOrWhiteSpace(s.ClientName)) clientSet.Add(s.ClientName.Trim());
                }

                cbClient.Items.Clear();
                foreach (var c in clientSet.OrderBy(x => x))
                {
                    cbClient.Items.Add(c);
                }

                // Ready to invoice: completed, with a saved trip report, not on a statement and not invoiced already.
                var reportedIds = MJdatabase.GetTripReportDeliveryIds();
                var onStatement = MJdatabase.GetBilledDeliveryIds(0);
                chargeFees = MJdatabase.GetChargeableCancelledFees();
                // Billable: completed trips with a trip report, plus cancelled trips marked chargeable. Cancelled no-charge trips never appear.
                var waitingIds = new HashSet<int>(MJdatabase.GetTripsWaitingForInvoice().Where(w => w.DeliveryId > 0).Select(w => w.DeliveryId)); // the one shared list (plan section 4)
                completedDeliveries = MJdatabase.GetDeliveries().FindAll(d => waitingIds.Contains(d.Id));
                completedDeliveries.AddRange(SpecialTripsToInvoice()); // Nov 6: completed special trips with a trip report
                // Nov 7: each trip is listed by its trip report number, then its dispatch and client (no loading schedule number).
                var reportNos = MJdatabase.GetTripReportsByDelivery();
                var specialLinks = MJdatabase.GetSpecialTripLinks();
                cbDelivery.Items.Clear();
                foreach (var d in completedDeliveries)
                {
                    string trNo = d.Id < 0 ? (specialLinks.TryGetValue(-d.Id, out var spl) ? spl.TripReportNumber : "") : (reportNos.TryGetValue(d.Id, out var rep0) ? rep0.TripReportNumber : "");
                    cbDelivery.Items.Add($"{(trNo.Length > 0 ? trNo + " · " : "")}{d.DispatchNumber} · {d.ClientName}{(IsChargeableCancelled(d) ? $"  [Cancelled – chargeable ₱{chargeFees[d.Id]:N2}]" : "")}");
                }
                if (cbDelivery.Items.Count > 0)
                {
                    cbDelivery.SelectedIndex = -1; cbDelivery.Text = "";
                }
                else
                {
                    cbDelivery.Text = "(All completed dispatches are invoiced)";
                    txtLoadingSchedule.Text = "";
                }

                FilterGridData();

                if (!pnlForm.Visible) { RefreshBillReady(); ReloadSoaChoices(); }
                int unpaid = allInvoices.Count(i => i.Status == "Unpaid");
                int drafts = allInvoices.Count(i => i.Status == "Draft");
                int overdue = allInvoices.Count(i => i.Status == "Unpaid" && i.DueDate.Date < DateTime.Today);
                int paid = allInvoices.Count(i => i.Status == "Paid");
                int cancelled = allInvoices.Count(i => i.Status == "Cancelled");
                // Cancelled invoices are not money billed.
                decimal totalNet = allInvoices.Where(i => i.Status != "Cancelled").Sum(i => i.NetAmount);

                lblSummary.Text = $"{allInvoices.Count:N0} invoice{(allInvoices.Count == 1 ? "" : "s")}  ·  {unpaid} unpaid ({overdue} overdue)  ·  {drafts} draft  ·  {paid} paid{(cancelled > 0 ? $"  ·  {cancelled} cancelled" : "")}  ·  ₱{totalNet:N2} billed";
            }
            catch (Exception ex)
            { ErrLog.Error("BillingView", ex); 
                System.Diagnostics.Debug.WriteLine($"[BillingView] LoadData: {ex}");
                MessageBox.Show($"Failed to load invoice billing records: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (!BillGateBeforeSave()) return;
            if (SoaModeOn) { SaveInvoiceForSelectedSoa(); return; } // Nov 10: one invoice for the whole statement
            try
            {
                var valErrors = new List<string>();
                Control? firstInvalid = null;
                ValidationHelper.ClearErrors(pnlForm);

                if (cbDelivery.SelectedIndex < 0 || completedDeliveries.Count <= cbDelivery.SelectedIndex)
                {
                    ValidationHelper.ApplyValidationState(cbDelivery, false, "Completed delivery selection is required.");
                    valErrors.Add("Completed delivery selection is required.");
                    firstInvalid ??= cbDelivery;
                }
                if (!ValidationHelper.ValidateRequiredText(txtInvoiceNumber, "Invoice Number", valErrors)) firstInvalid ??= txtInvoiceNumber;
                if (!ValidationHelper.ValidateRequiredCombo(cbClient, "Client Company", valErrors)) firstInvalid ??= cbClient;
                if (!ValidationHelper.ValidatePositiveNumber(numTotalAmount, "Amount (freight tariff)", valErrors)) firstInvalid ??= numTotalAmount;

                if (!ValidationHelper.ShowValidationSummaryIfAny(this, valErrors, firstInvalid))
                {
                    return;
                }

                if (cbDelivery.SelectedIndex < 0 || completedDeliveries.Count <= cbDelivery.SelectedIndex)
                {
                    MessageBox.Show("Please select a completed trip to invoice.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selDel = completedDeliveries[cbDelivery.SelectedIndex];
                if (!DispatchGate.RequireCompleted(this, DispatchGate.Fresh(selDel), "bill it")) return;
                var existingInvoices = MJdatabase.GetInvoices();
                // A cancelled invoice does not count; an old statement's invoice that bills the trip does.
                var duplicateInvoice = selDel.Id > 0 ? MJdatabase.GetLiveInvoiceForTrip(selDel.Id, 0, selectedInvoice?.Id ?? 0) : null;
                if (duplicateInvoice != null)
                {
                    MessageBox.Show($"INVOICE ALREADY EXISTS:\nDelivery #{selDel.DispatchNumber} has already been invoiced under {duplicateInvoice.Describe()}{(duplicateInvoice.IssueDate > DateTime.MinValue ? $" (Date: {duplicateInvoice.IssueDate:MMM dd, yyyy})" : "")}. A delivery trip can be invoiced only once.", "Invoice Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }
                if (selDel.Id < 0 && MJdatabase.SpecialTripHasActiveInvoice(-selDel.Id)) { MessageBox.Show($"Special trip {selDel.DispatchNumber} already has its service invoice.", "Invoice Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Stop); return; }

                string invNum = txtInvoiceNumber.Text.Trim();
                var siWhy = MJdatabase.CheckSiNumber(invNum, selectedInvoice?.Id ?? 0); // Nov 6: typed, required, never repeated
                if (siWhy != null) { MessageBox.Show(siWhy, "Service Invoice No.", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtInvoiceNumber.Focus(); return; }

                var duplicateNum = existingInvoices.Find(i => (selectedInvoice == null || i.Id != selectedInvoice.Id) &&
                    !string.IsNullOrWhiteSpace(i.InvoiceNumber) && i.InvoiceNumber.Equals(invNum, StringComparison.OrdinalIgnoreCase));
                if (duplicateNum != null)
                {
                    MessageBox.Show($"Invoice Reference Number '{invNum}' is already in use by another invoice (Client: {duplicateNum.ClientName}). Please provide a unique reference number.", "Reference Number Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtInvoiceNumber.Focus();
                    return;
                }

                // Nov 9: a trip on a statement is invoiced for exactly the statement line's amounts (the boxes are locked; this is the real guard).
                var stLine = selDel.Id > 0 ? MJdatabase.GetStatementLineForTrip(selDel.Id) : MJdatabase.GetStatementLineForTrip(0, -selDel.Id);
                decimal fuel = stLine != null ? stLine.Fuel : numFuel.Value;
                decimal total = stLine != null ? stLine.Freight + stLine.Fuel : numTotalAmount.Value + fuel; // what the trip is billed in all: freight tariff + fuel surcharge

                // Nov 6 workflow: no amount yet (no rate) is allowed; the invoice is kept as a Draft until it gets its amount.
                // Nov 7: no deductions on an invoice; the net due is the whole amount.
                decimal net = total;

                Invoice inv = new Invoice
                {
                    InvoiceNumber = invNum,
                    SoaId = 0,
                    DeliveryId = selDel.Id > 0 ? selDel.Id : 0, // a special trip is linked by its SpecialTripId (Nov 6)
                    ClientName = cbClient.Text.Trim(),
                    IssueDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(15),
                    TotalAmount = total,
                    FuelSurcharge = fuel,
                    Deductions = 0,
                    NetAmount = net,
                    Status = total > 0 ? PayStatus.Unpaid : PayStatus.Draft
                };

                MJdatabase.SaveInvoice(inv, crossTypeConfirmed: billNumberGuard.CrossTypeConfirmed);
                if (selDel.Id < 0) MJdatabase.LinkSpecialTripInvoice(inv.Id, -selDel.Id);
                if (stLine != null) MJdatabase.ApplyStatementToNewInvoice(inv.Id, selDel.Id > 0 ? selDel.Id : 0, selDel.Id < 0 ? -selDel.Id : 0); // on the statement, with its share of the withholding
                BillAfterSave(inv.Id, inv.InvoiceNumber);
                if (inv.Status == PayStatus.Draft) ToastHelper.ShowToast(this, $"Invoice {inv.InvoiceNumber} saved as a draft — add its amount before it can go on a statement", ToastType.Warning);
                else if (batchQueue.Count == 0) QuietBox.Show("Service Invoice generated and issued with 15-day credit terms!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else ToastHelper.ShowToast(this, $"Invoice {inv.InvoiceNumber} issued (PHP {inv.NetAmount:N2})", ToastType.Success);
                ClearFields();
                LoadData();
                SetFormVisibility(false);
                if (batchQueue.Count > 0) { batchIndex++; OpenBatchItem(); }
            }
            catch (Exception ex)
            { ErrLog.Error("BillingView", ex); 
                System.Diagnostics.Debug.WriteLine($"[BillingView] BtnSave_Click: {ex}");
                MessageBox.Show($"Failed to save invoice: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { BillSaveFinished(); }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            if (!BillGateBeforeSave()) return;
            try
            {
                if (selectedInvoice == null)
                {
                    MessageBox.Show("Please select an invoice to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string invNum = txtInvoiceNumber.Text.Trim();
                if (string.IsNullOrWhiteSpace(invNum))
                {
                    MessageBox.Show("Please enter an Invoice Reference Number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtInvoiceNumber.Focus();
                    return;
                }
                { var siWhy2 = MJdatabase.CheckSiNumber(invNum, selectedInvoice.Id); if (siWhy2 != null) { MessageBox.Show(siWhy2, "Service Invoice No.", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtInvoiceNumber.Focus(); return; } } // Nov 6

                var existingInvoices = MJdatabase.GetInvoices();
                var duplicateNum = existingInvoices.Find(i => i.Id != selectedInvoice.Id &&
                    !string.IsNullOrWhiteSpace(i.InvoiceNumber) && i.InvoiceNumber.Equals(invNum, StringComparison.OrdinalIgnoreCase));
                if (duplicateNum != null)
                {
                    MessageBox.Show($"Invoice Reference Number '{invNum}' is already in use by another invoice (Client: {duplicateNum.ClientName}). Please provide a unique reference number.", "Reference Number Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtInvoiceNumber.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(cbClient.Text))
                {
                    MessageBox.Show("Please pick the client this invoice is for.", "Client Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cbClient.Focus();
                    return;
                }
                selectedInvoice.InvoiceNumber = invNum;

                decimal fuel = numFuel.Value;
                decimal total = numTotalAmount.Value + fuel; // freight tariff + fuel surcharge

                // Nov 6 workflow: an amount of PHP 0.00 keeps (or makes) the invoice a Draft instead of being refused.
                // Nov 7: no deductions on an invoice; the net due is the whole amount.
                decimal net = total;
                decimal alreadyCollected = MJdatabase.GetTotalCollectedForInvoice(selectedInvoice.Id);
                if (net < alreadyCollected)
                {
                    MessageBox.Show($"The updated total (PHP {net:N2}) cannot be less than the payments already collected for this invoice (PHP {alreadyCollected:N2}).", "Invoice Update Restricted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                selectedInvoice.ClientName = cbClient.Text.Trim();
                selectedInvoice.TotalAmount = total;
                selectedInvoice.FuelSurcharge = fuel;
                selectedInvoice.Deductions = 0;
                selectedInvoice.NetAmount = net;

                if (total <= 0) selectedInvoice.Status = PayStatus.Draft;
                else if (string.Equals(selectedInvoice.Status, "Draft", StringComparison.OrdinalIgnoreCase))
                {
                    // A draft (e.g. from a completed special trip) becomes a real invoice once it has an amount.
                    selectedInvoice.Status = "Unpaid";
                }
                MJdatabase.SaveInvoice(selectedInvoice, crossTypeConfirmed: billNumberGuard.CrossTypeConfirmed);
                BillAfterSave(selectedInvoice.Id, selectedInvoice.InvoiceNumber);
                QuietBox.Show("Invoice updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearFields();
                LoadData();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            { ErrLog.Error("BillingView", ex); 
                System.Diagnostics.Debug.WriteLine($"[BillingView] BtnUpdate_Click: {ex}");
                MessageBox.Show($"Failed to update invoice: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { BillSaveFinished(); }
        }

        /// <summary>"Re-enter" for an invoice cancelled as a typing mistake: reopens the same record (keeping its number) so it can be corrected.</summary>
        private void ReinstateInvoiceNow(Invoice inv)
        {
            try
            {
                if (MessageBox.Show(FindForm(), $"Reinstate invoice {inv.InvoiceNumber} so it can be corrected?\n\nIt keeps its number. After you correct it, save it as usual.", "Reinstate invoice", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                MJdatabase.ReinstateInvoice(inv.Id);
                ToastHelper.ShowToast(this, $"Invoice {inv.InvoiceNumber} reinstated. Choose Edit invoice to correct it.", ToastType.Success);
                LoadData();
            }
            catch (Exception ex) { MessageBox.Show(FindForm(), ex.Message, "Reinstate invoice", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedInvoice == null)
                {
                    MessageBox.Show("Please select an invoice to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string? deleteBlock = MJdatabase.ValidateRecordDeletion("Invoices", selectedInvoice.Id);
                if (!string.IsNullOrEmpty(deleteBlock))
                {
                    MessageBox.Show(deleteBlock, "Deletion Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Plan 17.6: say WHY. A typing mistake keeps the number for the corrected record; a real void burns it.
                bool proceed = Forms.CancelInvoiceDialog.Prompt(FindForm(), selectedInvoice.InvoiceNumber, out bool typingMistake, out string cancelReason);
                if (proceed)
                {
                    bool deleted = MJdatabase.CancelInvoice(selectedInvoice.Id, typingMistake, cancelReason);
                    if (deleted)
                    {
                        QuietBox.Show(MJdatabase.LastDeleteSummary + (typingMistake ? $"\n\nIts number {selectedInvoice.InvoiceNumber} is kept for {MJdatabase.PendingReenterDays} days. Right-click the cancelled invoice and choose Reinstate to correct it." : $"\n\nThe number {selectedInvoice.InvoiceNumber} can never be used again."), "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearFields();
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete Service Invoice. It may be locked or referenced by collections/statements of account.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            { ErrLog.Error("BillingView", ex); 
                System.Diagnostics.Debug.WriteLine($"[BillingView] BtnDelete_Click: {ex}");
                MessageBox.Show($"Delete operation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            gridInvoices.ClearSelection();
            ClearFields();
        }

        private void FilterGridData()
        {
            string q = txtGridSearch.Text.Trim().ToLowerInvariant();
            var filtered = string.IsNullOrEmpty(q)
                ? allInvoices
                : allInvoices.Where(i =>
                    (!string.IsNullOrEmpty(i.InvoiceNumber) && i.InvoiceNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(i.LoadingScheduleNumber) && i.LoadingScheduleNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(i.DispatchNumber) && i.DispatchNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(i.ClientName) && i.ClientName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(i.Status) && i.Status.ToLower().Contains(q)) ||
                    (q == "overdue" && RowMenu.Is(i.Status, PayStatus.Unpaid) && i.DueDate.Date < DateTime.Today)
                ).ToList();

            filtered = ApplyListChips(filtered, searching: !string.IsNullOrEmpty(q));
            try
            {
                var soaNo = MJdatabase.GetStatementsOfAccount().ToDictionary(x => x.Id, x => x.SoaNumber);
                foreach (var inv in allInvoices) inv.SoaNumber = inv.SoaId > 0 && soaNo.TryGetValue(inv.SoaId, out var sn) ? sn : "";
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            gridInvoices.DataSource = null;
            gridInvoices.DataSource = filtered;
            UpdateBillingSummary(filtered.ToList(), reloadPayments: false);
            lblGridHeader.Text = string.IsNullOrEmpty(q) && filtered.Count == allInvoices.Count
                ? $"Invoices ({allInvoices.Count})"
                : $"Invoices ({filtered.Count} of {allInvoices.Count})";
            if (filtered.Count > 0)
            {
                gridInvoices.ClearSelection();
                gridInvoices.Rows[0].Selected = true;
            }
            else
            {
                selectedInvoice = null;
                if (cbDelivery.SelectedIndex >= 0 && cbDelivery.SelectedIndex < completedDeliveries.Count)
                {
                    CbDelivery_SelectedIndexChanged(cbDelivery, EventArgs.Empty);
                }
                else
                {
                    isBindingForm = true;
                    cbClient.Text = "";
                    txtLoadingSchedule.Text = "";
                    numTotalAmount.SetSafe(0);
                    numFuel.SetSafe(0);
                    lblNetAmount.Text = "";
                    lblFormHeader.Text = "Generate Service Invoice";
                    isBindingForm = false;
                }
                SetButtonStates(false);
            }
        }

        private void TxtGridSearch_TextChanged(object? sender, EventArgs e)
        {
            FilterGridData();
        }

        private void BtnClearSearch_Click(object? sender, EventArgs e)
        {
            txtGridSearch.Text = "";
            txtGridSearch.Focus();
        }

        /// <summary>
        /// Statements are made in Statement of Account, from trips that already have their service invoice (Nov 6 workflow).
        /// With a client, a new statement for that client opens with its ready trips added.
        /// </summary>
        private void OpenStatementOfAccount(string? client = null, DateTime? around = null)
        {
            var hub = FinanceHubView.Of(this);
            var soa = hub?.OpenTab<StatementOfAccountView>("StatementOfAccount");
            if (soa == null || hub!.CurrentTabKey != "StatementOfAccount") return; // no access, or the person stayed on this screen
            if (!string.IsNullOrWhiteSpace(client)) soa.StartSoaFor(client.Trim(), (around ?? DateTime.Today).Date.AddDays(-31), DateTime.Today);
        }

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            PrintHelper.PrintGridReport("Client Billing & Accounts Receivable Statement", gridInvoices, "Official client billing invoice statement with gross totals, damage deductions, net payables, and 15-day due terms.");
        }

        private void ClearFields()
        {
            selectedInvoice = null;
            cbDelivery.SelectedIndex = -1; // Nov 7: Clear form frees the trip box again
            cbDelivery.Enabled = true;
            txtInvoiceNumber.Text = "";
            billNumberGuard?.Reset();
            lblFormHeader.Text = "Generate Service Invoice";
            txtLoadingSchedule.Text = "";
            cbClient.Text = "";
            numTotalAmount.Enabled = true; numFuel.Enabled = true; // unlocked again (a trip on a statement locks them)
            numTotalAmount.SetSafe(0); // filled from the trip's rate when a dispatch is picked
            numFuel.SetSafe(0);
            lblTariffMatch.Visible = false;
            if (cbSoa != null) { bool was = isBindingForm; isBindingForm = true; try { cbSoa.SelectedIndex = -1; chkDirectTrip.Checked = false; } finally { isBindingForm = was; } ApplyInvoiceSource(); } // Nov 10: a blank form bills a statement
            lblDueDate.Text = $"Payment Due Date (15 Days): {DateTime.Today.AddDays(15):MM/dd/yyyy}";
            RecalculateNet(this, EventArgs.Empty);
            if (cbDelivery.SelectedIndex >= 0 && cbDelivery.SelectedIndex < completedDeliveries.Count)
            {
                CbDelivery_SelectedIndexChanged(cbDelivery, EventArgs.Empty);
            }
            SetButtonStates(false);
        }

        private void SetButtonStates(bool isEditing)
        {
            string invNum = txtInvoiceNumber.Text.Trim();
            var matched = !string.IsNullOrWhiteSpace(invNum)
                ? allInvoices.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.InvoiceNumber) && i.InvoiceNumber.Equals(invNum, StringComparison.OrdinalIgnoreCase))
                : null;
            bool exists = (selectedInvoice != null || matched != null) && !string.IsNullOrWhiteSpace(invNum);

            btnSave.Enabled = !exists;
            btnSave.Visible = !exists;
            btnUpdate.Enabled = exists;
            btnUpdate.Visible = exists;
            ApplyButtonVisualState(btnSave, !exists, Pal.C(13, 110, 253), Pal.White);
            ApplyButtonVisualState(btnUpdate, exists, Pal.C(25, 135, 84), Pal.White);
            ScheduleBillGuide(); // the checklist decides whether Issue / Update can be clicked
        }

        private void ApplyButtonVisualState(Button btn, bool enabled, Color enabledBack, Color enabledFore)
        {
            if (enabled)
            {
                btn.BackColor = enabledBack;
                btn.ForeColor = enabledFore;
                btn.Cursor = Cursors.Hand;
            }
            else
            {
                btn.BackColor = Pal.C(226, 232, 240);
                btn.ForeColor = Pal.C(148, 163, 184);
                btn.Cursor = Cursors.Default;
            }
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action<Invoice>? OnRecordPaymentRequested { get; set; }

        private void SetupContextMenu()
        {
            RowMenu.Attach(gridInvoices, (m, rows) =>
            {
                var invs = rows.Select(r => r.DataBoundItem).OfType<Invoice>().ToList();
                if (invs.Count == 0) return;
                if (invs.Count > 1) { BuildMultiInvoiceMenu(m, invs); return; }
                var inv = invs[0];
                selectedInvoice = inv;
                decimal paid = 0;
                try { paid = MJdatabase.GetTotalCollectedForInvoice(inv.Id); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
                decimal balance = Math.Max(0, inv.NetAmount - paid);
                bool isPaid = RowMenu.Is(inv.Status, "Paid") || (inv.NetAmount > 0 && balance <= 0);
                bool isDraft = RowMenu.Is(inv.Status, "Draft");
                m.Items.AddRange(RowMenu.Header($"🧾  Invoice {inv.InvoiceNumber}",
                    $"{RowMenu.Short(inv.ClientName, 28)} · {RowMenu.Money(inv.NetAmount)} · {inv.Status} · due {inv.DueDate:MMM d}"));
                m.Items.Add(RowMenu.Item("Open details", () => RecordDetailsDialog.ShowDetails(this, inv)));
                m.Items.Add(RowMenu.Item(isDraft ? "Complete this draft invoice" : "Edit invoice", () => { SetFormVisibility(true); cbDelivery.Focus(); }));
                if (!isPaid && !isDraft) m.Items.Add(RowMenu.Good($"Collect payment in full ({RowMenu.Money(inv.NetAmount)})", () => OnRecordPaymentRequested?.Invoke(inv)));
                if (paid > 0) m.Items.Add(RowMenu.Item($"Show payments ({RowMenu.Money(paid)} received)", () => FinanceHubView.Of(this)?.ShowCollections(inv.InvoiceNumber)));
                m.Items.Add(RowMenu.Item("Mark as sent to client", () =>
                {
                    MJdatabase.LogDocumentIssue(MJdatabase.DocInvoice, inv.Id, inv.InvoiceNumber, inv.NetAmount, "Sent");
                    ToastHelper.ShowToast(this, $"Invoice {inv.InvoiceNumber} marked as sent (PHP {inv.NetAmount:N2}). If its amount changes later you will be told to resend it.", ToastType.Success);
                }, "", !isDraft));
                var staleInv = MJdatabase.NeedsReissue(MJdatabase.DocInvoice, inv.Id, inv.NetAmount);
                if (staleInv != null) m.Items.Add(RowMenu.Item($"⚠ Amount changed since it was {staleInv.Kind.ToLowerInvariant()} ({staleInv.At:MMM d}, PHP {staleInv.Amount:N2}): resend", () => { }));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Item($"Open dispatch {inv.DispatchNumber}", () => RowMenu.OpenDispatch(this, inv.DeliveryId, inv.DispatchNumber), "", inv.DeliveryId > 0 || !string.IsNullOrWhiteSpace(inv.DispatchNumber)));
                m.Items.Add(RowMenu.Item("Open client profile", () => MainForm.Instance?.OpenClientProfile(inv.ClientName), "", !string.IsNullOrWhiteSpace(inv.ClientName)));
                m.Items.Add(RowMenu.Item("Make a statement for this client…", () => OpenStatementOfAccount(inv.ClientName, inv.IssueDate), "", !string.IsNullOrWhiteSpace(inv.ClientName)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Sub("🖨️  Print",
                    RowMenu.Item("End-to-end delivery dossier", () => PrintHelper.PrintEndToEndDeliveryDossier(inv.DeliveryId, inv.DispatchNumber), "", inv.DeliveryId > 0),
                    RowMenu.Item("Selected invoices (table)", () => PrintHelper.PrintSelectedOrAllGridReport("Client Service Invoices", gridInvoices, "Selected service invoices.")),
                    RowMenu.Item("Whole invoice register", () => BtnPrint_Click(this, EventArgs.Empty))));
                m.Items.Add(RowMenu.CopySub(this, ("Invoice number", inv.InvoiceNumber), ("Dispatch number", inv.DispatchNumber), ("Client", inv.ClientName),
                    ("Amount due", balance.ToString("N2")),
                    ("Payment reminder", $"Invoice {inv.InvoiceNumber} for {inv.ClientName}: {RowMenu.Money(inv.NetAmount)}, amount due {RowMenu.Money(balance)}, due {inv.DueDate:MMMM d, yyyy}.")));
                m.Items.Add(RowMenu.Sub("🔎  Find",
                    RowMenu.Item("Other invoices for this client", () => RowMenu.Search(txtGridSearch, inv.ClientName)),
                    RowMenu.Item("Statements of account for this client", () => FinanceHubView.Of(this)?.ShowStatements(inv.ClientName))));
                m.Items.Add(RowMenu.ExportSub(gridInvoices, "Billing_Invoices", "Invoices and Accounts Receivable", () => BtnPrint_Click(this, EventArgs.Empty)));
                m.Items.Add(RowMenu.Sep());
                if (MJdatabase.CanReinstateInvoice(inv))
                    m.Items.Add(RowMenu.Good("Reinstate to correct (cancelled as a typing mistake)…", () => ReinstateInvoiceNow(inv)));
                m.Items.Add(RowMenu.Danger(paid > 0 ? "Cancel invoice… (has payments)" : "Cancel / void invoice…", () => { selectedInvoice = inv; BtnDelete_Click(this, EventArgs.Empty); }));
            }, allowMultiSelect: true);
        }
        private void BuildMultiInvoiceMenu(ContextMenuStrip m, List<Invoice> invs)
        {
            decimal total = invs.Sum(i => i.NetAmount);
            m.Items.AddRange(RowMenu.Header($"🧾  {invs.Count} invoices", $"{RowMenu.Money(total)} in total · {invs.Select(i => i.ClientName).Distinct().Count()} client(s)"));
            m.Items.Add(RowMenu.Item("Print the selected invoices (table)", () => PrintHelper.PrintSelectedOrAllGridReport("Client Service Invoices", gridInvoices, "Selected service invoices.")));
            m.Items.Add(RowMenu.Item("Copy invoice numbers", () => RowMenu.Copy(this, string.Join(", ", invs.Select(i => i.InvoiceNumber)), $"{invs.Count} invoice numbers copied")));
            m.Items.Add(RowMenu.Item("Copy as a list with amounts", () => RowMenu.Copy(this, string.Join(Environment.NewLine, invs.Select(i => $"{i.InvoiceNumber}\t{i.ClientName}\t{i.NetAmount:N2}\t{i.Status}")), "List copied")));
            m.Items.Add(RowMenu.Sep());
            m.Items.Add(RowMenu.ExportSub(gridInvoices, "Billing_Invoices", "Invoices and Accounts Receivable", () => BtnPrint_Click(this, EventArgs.Empty)));
        }
        public void FilterByText(string text)
        {
            if (pnlForm.Visible) SetFormVisibility(false);
            RowMenu.Search(txtGridSearch, text);
        }
    }
}
