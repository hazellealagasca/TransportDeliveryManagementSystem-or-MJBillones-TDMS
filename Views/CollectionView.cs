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
    public partial class CollectionView : UserControl, IRefreshable, IUnsavedForm
    {
        private CollectionReceipt? selectedCollection = null;
        private List<Invoice> unpaidInvoices = new List<Invoice>();
        private List<CollectionReceipt> allReceipts = new List<CollectionReceipt>();
        private string currentSortColumn = "ReceiptNumber";
        private bool isSortAscending = true;
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;
        private float currentFormWidth = 400F;

        public CollectionView()
        {
            InitializeComponent();
            PageKit.SummaryUnderTitle(pnlSummary, lblSummary, pnlGridHeaderBar, lblGridHeader); // totals line under the title, not cut off at the bottom
            Pal.RemapTree(this);
            gridCollections.BringToFront();
            SetupGridColumns();
            InitListChips();
            SetupContextMenu();
            gridCollections.ColumnHeaderMouseClick += (s, e) => {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridCollections.Columns.Count) return;
                string col = gridCollections.Columns[e.ColumnIndex].DataPropertyName;
                if (string.IsNullOrEmpty(col)) return;
                if (currentSortColumn == col) isSortAscending = !isSortAscending;
                else { currentSortColumn = col; isSortAscending = true; }
                FilterGridData();
            };
            LoadData();

            btnPrint.Visible = false;

            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("📊 Export collections to CSV", null, (s, e) => DataExportHelper.ExportToCsv(gridCollections, "Collection_Receipts", "Collections and Payment Receipts"));
            menuActions.Items.Add("🖨️ Print collections register", null, (s, e) => BtnPrint_Click(this, EventArgs.Empty));
            var btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);
            pnlGridActionsFlow.Controls.Add(btnActions);

            txtRefNo.TextChanged += (s, e) => HandleRefNoInput();

            currentFormWidth = UIThemeHelper.GetAdaptiveDrawerWidth(this, 400F, 460F);
            InitializeFormToggle();
            SetButtonStates(false);
            UIThemeHelper.ApplyTheme(this);
                    InitFullPageForm();
            numAmountPaid.Enabled = false; // Nov 6: always the full amount due (no partial payments)
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
            btnCloseForm.Click += (s, e) => { if (FormGuard.ConfirmDiscard(this)) SetFormVisibility(false); };
            pnlFormHeaderBar.Controls.Add(btnCloseForm);

            SetFormVisibility(false);
        }


        // ------------------------------------------------------------------ Full-page form (same tab, back button, section cards)
        private FullPageFormKit? formKit;
        private void InitFullPageForm()
        {
            formKit = new FullPageFormKit(this, tblMasterContent, pnlForm, pnlGridCard, pnlFormHeaderBar, lblFormHeader, btnCloseForm, pnlFormBody, pnlFormActionsBar, "Back to receipts");
            formKit.Build(new[]
            {
                new FullPageFormKit.Section("Statement or invoice being paid", "Pick the statement (all its service invoices) or the service invoice this receipt pays in full.", lblInv, cbInvoice, pnlLinkedInvoice),
                new FullPageFormKit.Section("Payment", "Receipt no., the full amount due, method and reference.", BuildReceiptNoField()[0], txtReceiptNo, receiptNumberGuard.Message, receiptNumberGuard.Tick, tblPaymentRow, BuildCollCapLabel(), BuildPaymentDateField()[0], dtpPaymentDate, lblRef, txtRefNo, lblNotes, txtNotes)
            });
            InitCollGuide();
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = "＋ New receipt";
                UIThemeHelper.SetButtonRole(btnToggleForm, "primary");
            }
            gridCollections.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || gridCollections.Rows[e.RowIndex].DataBoundItem is not CollectionReceipt) return;
                gridCollections.ClearSelection();
                gridCollections.Rows[e.RowIndex].Selected = true;
                SetFormVisibility(true);
            };
            if (Features.On(Features.PaymentsWorkbench)) ApplyCollWorkbench(); // Ledger Desk layout (Settings → Features can switch it off)
            SetFormVisibility(false);
        }
        private void OpenNewRecordForm()
        {
            if (keep.Kept) { keep.Resume(this, SetFormVisibility); return; } // a kept draft comes back as it was left, never blank
            ClearFields();
            lblFormHeader.Text = "New payment receipt";
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
            OnCollFormShown(visible);
            MainForm.Instance?.NotifyDrawerToggled(visible);
        }

        public void RefreshData()
        {
            LoadData();
        }

        private void SetupGridColumns()
        {
            gridCollections.AutoGenerateColumns = false;
            gridCollections.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridCollections.ScrollBars = ScrollBars.Both;
            gridCollections.AllowUserToResizeColumns = true;
            gridCollections.ShowCellToolTips = false;
            gridCollections.Columns.Clear();
            gridCollections.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReceiptNumber", HeaderText = "Receipt #", FillWeight = 13, MinimumWidth = 100, ReadOnly = true });
            gridCollections.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceNumber", HeaderText = "Invoice #", FillWeight = 13, MinimumWidth = 100, ReadOnly = true });
            gridCollections.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PaymentDate", HeaderText = "Date", DefaultCellStyle = { Format = "MM/dd/yyyy" }, FillWeight = 11, MinimumWidth = 85, ReadOnly = true });
            gridCollections.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "AmountPaid", HeaderText = "Amount Paid", DefaultCellStyle = { Format = "N2" }, FillWeight = 14, MinimumWidth = 100, ReadOnly = true });
            gridCollections.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PaymentMethod", HeaderText = "Method", FillWeight = 12, MinimumWidth = 85, ReadOnly = true });
            gridCollections.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReferenceNumber", HeaderText = "Reference #", FillWeight = 14, MinimumWidth = 100, ReadOnly = true });
            gridCollections.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Notes", HeaderText = "Notes", FillWeight = 23, MinimumWidth = 120, ReadOnly = true });
        }

        private void CbInvoice_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cbInvoice.SelectedIndex >= 0 && cbInvoice.SelectedIndex < unpaidInvoices.Count)
            {
                var t = payTargets[cbInvoice.SelectedIndex]; // Nov 6: a statement or a lone invoice, always the full amount
                cbInvoice.Enabled = false; // Nov 8: once the statement is picked it can't be swapped (Clear form frees it)
                ShowTargetInfo(t);
                if (!t.IsSoa) { var real = MJdatabase.GetInvoices().FirstOrDefault(i => i.Id == t.Invoices[0].Id); if (real != null) PrefillFromSpecialTrip(real); }
            }
            else
            {
                lblLinkedInvoiceInfo.Text = "📄 Linked invoice: (none selected)";
            }
        }

        private void GridCollections_SelectionChanged(object? sender, EventArgs e)
        {
            if (pnlForm.Visible) return; // the list is hidden while the form is open; keep what is being typed
            if (gridCollections.SelectedRows.Count > 0 && gridCollections.SelectedRows[0].DataBoundItem is CollectionReceipt cr)
            {
                BindCollectionToForm(cr);
            }
        }

        private void HandleRefNoInput()
        {
            string refNum = txtRefNo.Text.Trim();
            if (string.IsNullOrWhiteSpace(refNum))
            {
                selectedCollection = null;
                lblFormHeader.Text = "Record Payment Receipt";
                SetButtonStates(false);
                return;
            }

            var matched = allReceipts.FirstOrDefault(r => (!string.IsNullOrWhiteSpace(r.ReferenceNumber) && r.ReferenceNumber.Equals(refNum, StringComparison.OrdinalIgnoreCase)) ||
                                                          (!string.IsNullOrWhiteSpace(r.ReceiptNumber) && r.ReceiptNumber.Equals(refNum, StringComparison.OrdinalIgnoreCase)));
            if (matched != null)
            {
                BindCollectionToForm(matched);
                for (int i = 0; i < gridCollections.Rows.Count; i++)
                {
                    if (gridCollections.Rows[i].DataBoundItem is CollectionReceipt rowCr && rowCr.Id == matched.Id)
                    {
                        gridCollections.ClearSelection();
                        gridCollections.Rows[i].Selected = true;
                        break;
                    }
                }
            }
            else
            {
                selectedCollection = null;
                lblFormHeader.Text = $"Receipt Ref: {refNum} (New)";
                SetButtonStates(false);
            }
        }

        private void BindCollectionToForm(CollectionReceipt cr)
        {
            selectedCollection = cr;
            editTargetInvoice = EditInvoiceFor(cr);
            if (txtReceiptNo != null) txtReceiptNo.Text = cr.ReceiptNumber;
            SetPaymentDate(cr.PaymentDate);
            lblFormHeader.Text = $"Receipt #{selectedCollection.ReceiptNumber}";
            lblLinkedInvoiceInfo.Text = !string.IsNullOrEmpty(selectedCollection.InvoiceNumber)
                ? $"📄 Linked invoice: {selectedCollection.InvoiceNumber}"
                : "📄 Linked invoice: (none)";
            cbInvoice.Enabled = false; // a saved receipt keeps its statement
            var _amt = selectedCollection.AmountPaid;
            if (_amt > numAmountPaid.Maximum) numAmountPaid.Maximum = _amt * 2;
            numAmountPaid.SetSafe(Math.Max(numAmountPaid.Minimum, Math.Min(numAmountPaid.Maximum, _amt)));
            cbPaymentMethod.Text = selectedCollection.PaymentMethod;
            txtRefNo.Text = selectedCollection.ReferenceNumber;
            txtNotes.Text = selectedCollection.Notes;
            SetButtonStates(true);
        }

        private void LoadData()
        {
            try
            {
                MJdatabase.EnsureOnlinePaymentMethod();
                var methods = MJdatabase.GetLookupItems("Payment Method");
                cbPaymentMethod.Items.Clear();
                foreach (var m in methods) cbPaymentMethod.Items.Add(m);

                LoadPayTargets(); // Nov 6: statements and lone invoices with nothing paid yet

                allReceipts = GroupReceipts(MJdatabase.GetCollectionReceipts()); // one row per receipt number
                FilterGridData();
                if (!pnlForm.Visible) RefreshCollReady();
                var receipts = allReceipts;

                decimal totalPaid = receipts.Sum(c => c.AmountPaid);
                lblSummary.Text = $"{receipts.Count:N0} receipt{(receipts.Count == 1 ? "" : "s")}  ·  ₱{totalPaid:N2} collected";
            }
            catch (Exception ex)
            { ErrLog.Error("CollectionView", ex); 
                MessageBox.Show($"Failed to load collection records: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (!CollGateBeforeSave()) return;
            try { SaveNewReceipt(); } // Nov 6: one receipt for the statement or invoice, full amount, typed receipt no.
            catch (Exception ex)
            { ErrLog.Error("CollectionView", ex);
                MessageBox.Show($"Failed to record collection receipt: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { CollSaveFinished(); }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            if (!CollGateBeforeSave()) return;
            try { UpdateSelectedReceipt(); } // Nov 6: receipt no., method, reference and notes; the amount never changes
            catch (Exception ex)
            { ErrLog.Error("CollectionView", ex);
                MessageBox.Show($"Failed to update collection receipt: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { CollSaveFinished(); }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedCollection == null)
                {
                    MessageBox.Show("Please select a collection receipt to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show($"Are you sure you want to delete Collection Receipt {selectedCollection.ReceiptNumber}?\n\n" + MJdatabase.DeletePolicyNote("CollectionReceipts"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (res == DialogResult.Yes)
                {
                    var grp = MJdatabase.GetReceiptRows(selectedCollection.ReceiptNumber); // a statement's receipt has one row per invoice
                    if (grp.Count == 0) grp.Add(selectedCollection);
                    bool deleted = true;
                    foreach (var row in grp) deleted &= MJdatabase.DeleteRecord("CollectionReceipts", row.Id);
                    if (deleted)
                    {
                        QuietBox.Show(MJdatabase.LastDeleteSummary, "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearFields();
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete Collection Receipt. It may be locked or referenced by other financial records.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            { ErrLog.Error("CollectionView", ex); 
                MessageBox.Show($"Delete operation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            gridCollections.ClearSelection();
            ClearFields();
        }

        private void FilterGridData()
        {
            string q = txtGridSearch.Text.Trim().ToLowerInvariant();
            var filtered = string.IsNullOrEmpty(q)
                ? allReceipts
                : allReceipts.Where(r =>
                    (!string.IsNullOrEmpty(r.ReceiptNumber) && r.ReceiptNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.InvoiceNumber) && r.InvoiceNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.PaymentMethod) && r.PaymentMethod.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.ReferenceNumber) && r.ReferenceNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(r.Notes) && r.Notes.ToLower().Contains(q))
                ).ToList();

            filtered = ApplyListChips(filtered);
            gridCollections.DataSource = null;
            gridCollections.DataSource = filtered;
            UpdateCollectionSummary(filtered.ToList());
            lblGridHeader.Text = string.IsNullOrEmpty(q) && filtered.Count == allReceipts.Count
                ? $"Receipts ({allReceipts.Count})"
                : $"Receipts ({filtered.Count} of {allReceipts.Count})";
            if (filtered.Count > 0)
            {
                gridCollections.ClearSelection();
                gridCollections.Rows[0].Selected = true;
            }
            else
            {
                ClearFields();
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

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            PrintHelper.PrintGridReport("Official Collection Receipts Log Statement", gridCollections, "Official collection receipt log including amounts paid, payment channels, and bank/check references.");
        }

        private void BtnPrintSlip_Click(object? sender, EventArgs e)
        {
            if (selectedCollection == null)
            {
                QuietBox.Show("Please select a collection receipt from the list first, then click Print Slip.", "Select a Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            PrintHelper.PrintOfficialCollectionReceipt(selectedCollection);
        }

        private void ClearFields()
        {
            selectedCollection = null;
            lblFormHeader.Text = "Record Payment Receipt";
            lblLinkedInvoiceInfo.Text = "📄 Linked invoice: (none selected)";
            cbPaymentMethod.SelectedIndex = -1; cbPaymentMethod.Text = ""; // Nov 6: blank until picked
            editTargetInvoice = null;
            if (txtReceiptNo != null) txtReceiptNo.Text = "";
            receiptNumberGuard?.Reset();
            SetPaymentDate(DateTime.Today); // a new receipt is dated today unless the person picks the earlier day the money came in
            cbInvoice.Enabled = true;
            if (cbInvoice.SelectedIndex >= 0) { cbInvoice.SelectedIndex = -1; cbInvoice.Text = ""; }
            txtRefNo.Text = "";
            txtNotes.Text = "";
            if (cbInvoice.SelectedIndex >= 0)
            {
                CbInvoice_SelectedIndexChanged(cbInvoice, EventArgs.Empty);
            }
            else
            {
                numAmountPaid.SetSafe(numAmountPaid.Minimum); // no invoice picked yet: don't keep the last receipt's amount
            }
            SetButtonStates(false);
        }

        private void SetButtonStates(bool isEditing)
        {
            string refNum = txtRefNo.Text.Trim();
            var matched = !string.IsNullOrWhiteSpace(refNum)
                ? allReceipts.FirstOrDefault(r => (!string.IsNullOrWhiteSpace(r.ReferenceNumber) && r.ReferenceNumber.Equals(refNum, StringComparison.OrdinalIgnoreCase)) ||
                                                  (!string.IsNullOrWhiteSpace(r.ReceiptNumber) && r.ReceiptNumber.Equals(refNum, StringComparison.OrdinalIgnoreCase)))
                : null;
            bool exists = (selectedCollection != null || matched != null) && !string.IsNullOrWhiteSpace(refNum);

            btnSave.Enabled = !exists;
            btnSave.Visible = !exists;
            btnUpdate.Enabled = exists;
            btnUpdate.Visible = exists;
            ApplyButtonVisualState(btnSave, !exists, Pal.C(13, 110, 253), Pal.White);
            ApplyButtonVisualState(btnUpdate, exists, Pal.C(25, 135, 84), Pal.White);
            collGuideKey = int.MinValue;
            ScheduleCollGuide(); // the checklist decides whether Save / Update can be clicked
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

        private void SetupContextMenu()
        {
            RowMenu.Attach(gridCollections, (m, rows) =>
            {
                var crs = rows.Select(r => r.DataBoundItem).OfType<CollectionReceipt>().ToList();
                if (crs.Count == 0) return;
                if (crs.Count > 1)
                {
                    m.Items.AddRange(RowMenu.Header($"💵  {crs.Count} receipts", $"{RowMenu.Money(crs.Sum(c => c.AmountPaid))} collected"));
                    m.Items.Add(RowMenu.Item("Print the selected receipts (table)", () => PrintHelper.PrintSelectedOrAllGridReport("Collection Receipts", gridCollections, "Selected collection receipts.")));
                    m.Items.Add(RowMenu.Item("Copy receipt numbers", () => RowMenu.Copy(this, string.Join(", ", crs.Select(c => c.ReceiptNumber)), "Receipt numbers copied")));
                    m.Items.Add(RowMenu.Sep());
                    m.Items.Add(RowMenu.ExportSub(gridCollections, "Collection_Receipts", "Collection Receipts", () => BtnPrint_Click(this, EventArgs.Empty)));
                    return;
                }
                var cr = crs[0];
                selectedCollection = cr;
                m.Items.AddRange(RowMenu.Header($"💵  Receipt {cr.ReceiptNumber}",
                    $"{RowMenu.Money(cr.AmountPaid)} · {cr.PaymentMethod}{(string.IsNullOrWhiteSpace(cr.ReferenceNumber) ? "" : " #" + cr.ReferenceNumber)} · {cr.PaymentDate:MMM d, yyyy} · invoice {cr.InvoiceNumber}"));
                m.Items.Add(RowMenu.Item("Open details", () => RecordDetailsDialog.ShowDetails(this, cr)));
                m.Items.Add(RowMenu.Item("Edit receipt", () => { SetFormVisibility(true); txtReceiptNo?.Focus(); }));
                m.Items.Add(RowMenu.Good("Print official receipt (OR)", () => PrintHelper.PrintOfficialCollectionReceipt(cr)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Item($"Show invoice {cr.InvoiceNumber}", () => FinanceHubView.Of(this)?.ShowInvoices(cr.InvoiceNumber), "", !string.IsNullOrWhiteSpace(cr.InvoiceNumber)));
                m.Items.Add(RowMenu.Item("Other payments for this invoice", () => RowMenu.Search(txtGridSearch, cr.InvoiceNumber), "", !string.IsNullOrWhiteSpace(cr.InvoiceNumber)));
                m.Items.Add(RowMenu.CopySub(this, ("Receipt number", cr.ReceiptNumber), ("Invoice number", cr.InvoiceNumber), ("Reference / check #", cr.ReferenceNumber),
                    ("Amount", cr.AmountPaid.ToString("N2")),
                    ("Summary", $"OR {cr.ReceiptNumber}: {RowMenu.Money(cr.AmountPaid)} by {cr.PaymentMethod} on {cr.PaymentDate:MMMM d, yyyy} for invoice {cr.InvoiceNumber}")));
                m.Items.Add(RowMenu.ExportSub(gridCollections, "Collection_Receipts", "Collection Receipts", () => BtnPrint_Click(this, EventArgs.Empty)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Danger("Void / delete receipt…", () => { selectedCollection = cr; BtnDelete_Click(this, EventArgs.Empty); }));
            }, allowMultiSelect: true);
        }
        public void FilterByText(string text)
        {
            if (pnlForm.Visible) SetFormVisibility(false);
            RowMenu.Search(txtGridSearch, text);
        }
        /// <summary>
        /// A payment for a special trip (its service invoice, or an SOA that bills it): the trip's Mode of Payment is
        /// picked and the notes say which trip and destination it is for (only on a new receipt, never over what was typed).
        /// </summary>
        private void PrefillFromSpecialTrip(Invoice inv)
        {
            try
            {
                if (selectedCollection != null) return; // editing an existing receipt: keep it as it was
                var trip = MJdatabase.GetSpecialTripForInvoice(inv.Id);
                if (trip == null) return;
                if (!string.IsNullOrWhiteSpace(trip.ModeOfPayment))
                {
                    int idx = cbPaymentMethod.Items.Cast<object>().ToList().FindIndex(o => string.Equals(o?.ToString()?.Trim(), trip.ModeOfPayment.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) cbPaymentMethod.SelectedIndex = idx;
                }
                string note = $"Special trip {trip.OrderReference} · Destination: {trip.DestinationAddress}";
                if (string.IsNullOrWhiteSpace(txtNotes.Text)) txtNotes.Text = note;
                lblLinkedInvoiceInfo.Text += $"  ·  {note}  ·  Mode of payment: {(string.IsNullOrWhiteSpace(trip.ModeOfPayment) ? "not set" : trip.ModeOfPayment)}";
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
        /// <summary>Opens a new receipt for a special trip's payment (its service invoice, or the SOA invoice that bills it).</summary>
        public void PrefillForSpecialTrip(SpecialTrip trip)
        {
            LoadData();
            var links = MJdatabase.GetSpecialTripLinks().TryGetValue(trip.Id, out var l) ? l : null;
            int idx = -1;
            if (links != null && links.InvoiceId > 0) idx = payTargets.FindIndex(t => t.Invoices.Any(x => x.Id == links.InvoiceId));
            if (idx < 0) idx = payTargets.FindIndex(t => t.Invoices.Any(x => x.SpecialTripId == trip.Id));
            if (idx < 0)
            {
                QuietBox.Show($"Special trip {trip.OrderReference} has no unpaid invoice to collect yet. Tick \"With Service Invoice\" on the trip, or bill it on a Statement of Account and issue its invoice first.", "Nothing to collect yet", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            OpenNewRecordForm();
            cbInvoice.SelectedIndex = idx;
            txtReceiptNo?.Focus();
        }
        /// <summary>Opens a new receipt with the invoice already picked (from Billing → Record payment).</summary>
        /// <summary>Nov 6 workflow: a new receipt for this statement's full amount (from "Proceed to Collection Receipt" on the SOA).</summary>
        public void PrefillForSoa(int soaId)
        {
            LoadData();
            int idx = payTargets.FindIndex(t => t.SoaId == soaId);
            if (idx < 0)
            {
                QuietBox.Show("This statement has nothing left to collect (it may already be paid).", "Nothing to collect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            OpenNewRecordForm();
            cbInvoice.SelectedIndex = idx;
            txtReceiptNo?.Focus();
        }

        public void PrefillForInvoice(Invoice inv)
        {
            LoadData();
            int idx = payTargets.FindIndex(t => t.Invoices.Any(x => x.Id == inv.Id)); // on a statement: the receipt pays the whole statement
            if (idx < 0)
            {
                QuietBox.Show($"Invoice {inv.InvoiceNumber} is already paid in full (status: {inv.Status}).", "Nothing to collect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            OpenNewRecordForm();
            cbInvoice.SelectedIndex = idx;
            txtReceiptNo?.Focus();
        }
    }
}
