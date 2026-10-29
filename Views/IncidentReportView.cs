using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class IncidentReportView : UserControl, IRefreshable
    {
        private IncidentReport? selectedIncident = null;
        private bool isBindingForm = false;
        private List<IncidentReport> allIncidents = new List<IncidentReport>();
        private string currentSortColumn = "Id";
        private bool isSortAscending = true;
        private List<Delivery> deliveries = new List<Delivery>();
        private string currentPhotoPath = string.Empty;
        private System.Windows.Forms.Timer? tmrAutoSaveDraft;
        private Panel? pnlDraftBanner;
        private Button btnToggleForm = null!;
        private Button btnCloseForm = null!;
        public Action<int, string>? OnNavigateToTripReportRequested;

        public IncidentReportView()
        {
            InitializeComponent();
            Pal.RemapTree(this);
            gridIncidents.BringToFront();
            try { ProfileKit.Suggest(txtCause, "incident"); ProfileKit.Suggest(txtDescription, "incident-notes"); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
            SetupGridColumns();
            InitListChips();
            SetupContextMenu();
            gridIncidents.ColumnHeaderMouseClick += (s, e) => {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= gridIncidents.Columns.Count) return;
                string col = gridIncidents.Columns[e.ColumnIndex].DataPropertyName;
                if (string.IsNullOrEmpty(col)) return;
                if (currentSortColumn == col) isSortAscending = !isSortAscending;
                else { currentSortColumn = col; isSortAscending = true; }
                FilterGridData();
            };
            gridIncidents.CurrentCellDirtyStateChanged += (s, e) => { if (gridIncidents.IsCurrentCellDirty && gridIncidents.CurrentCell is DataGridViewCheckBoxCell) gridIncidents.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            gridIncidents.CellValueChanged += (s, e) => {
                if (isBindingForm) return;
                if (e.RowIndex >= 0 && e.ColumnIndex == 0 && gridIncidents.Rows[e.RowIndex].DataBoundItem is IncidentReport inc) {
                    try
                    {
                        bool success = MJdatabase.ToggleIncidentChargeable(inc.Id, inc.ChargeableToCrew);
                        if (!success)
                        {
                            inc.ChargeableToCrew = !inc.ChargeableToCrew;
                            gridIncidents.InvalidateRow(e.RowIndex);
                            MessageBox.Show("Failed to update chargeable status.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        if (selectedIncident != null && selectedIncident.Id == inc.Id) {
                            chkChargeable.Checked = inc.ChargeableToCrew;
                        }
                        gridIncidents.InvalidateRow(e.RowIndex);
                    }
                    catch (Exception ex)
                    { ErrLog.Error("IncidentReportView", ex); 
                        inc.ChargeableToCrew = !inc.ChargeableToCrew;
                        gridIncidents.InvalidateRow(e.RowIndex);
                        MessageBox.Show($"Failed to update incident chargeable status: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            SetupDraftAutoSave();
            LoadData();

            btnPrint.Visible = false;

            var menuActions = new ContextMenuStrip();
            menuActions.Items.Add("📊 Export Incident Logs to CSV", null, (s, e) => DataExportHelper.ExportToCsv(gridIncidents, "Incident_Reports", "Goods Damage and Incident Reports"));
            menuActions.Items.Add("🖨️ Print Incident Investigation Report", null, (s, e) => BtnPrint_Click(this, EventArgs.Empty));
            var btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);
            menuActions.Items.Add("🖨️ Print End-to-End Delivery Dossier", null, (s, e) => PrintSelectedDeliveryDossier());
            pnlGridActionsFlow.Controls.Add(btnActions);

            var btnProceedTrip = new Button
            {
                Text = "Proceed to Trip Reports ➔",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(190, 28),
                BackColor = Pal.C(15, 23, 42),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 4, 0, 0)
            };
            btnProceedTrip.FlatAppearance.BorderSize = 0;
            btnProceedTrip.Click += (s, e) => ProceedToTripReports();
            pnlGridActionsFlow.Controls.Add(btnProceedTrip);

            currentFormWidth = UIThemeHelper.GetAdaptiveDrawerWidth(this, 400F, 460F);
            InitializeFormToggle();
            UIThemeHelper.ApplyTheme(this);
            pnlFormHeaderBar.Controls.SetChildIndex(lblFormHeader, 0);
            pnlFormHeaderBar.Controls.SetChildIndex(btnCloseForm, 1);
            UIThemeHelper.SyncFormBodyControlsWidth(pnlFormBody);
            SetButtonStates(false);
                    InitFullPageForm();
        }

        private float currentFormWidth = 400F;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UIThemeHelper.SyncFormBodyControlsWidth(pnlFormBody);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && pnlForm.Visible)
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
                Text = "📝 + New / Form ▾",
                Size = new Size(150, 30),
                BackColor = Pal.C(30, 41, 59),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 4)
            };
            btnToggleForm.FlatAppearance.BorderSize = 0;
            btnToggleForm.Click += (s, e) => { if (pnlForm.Visible) SetFormVisibility(false); else OpenNewRecordForm(); };
            pnlGridActionsFlow.Controls.Add(btnToggleForm);
            pnlGridActionsFlow.Controls.SetChildIndex(btnToggleForm, 0);

            btnCloseForm = new Button
            {
                Name = "btnCloseForm",
                Text = "✕ Close",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(68, 26),
                Padding = new Padding(6, 0, 6, 0),
                Dock = DockStyle.Right,
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCloseForm.FlatAppearance.BorderSize = 0;
            btnCloseForm.Click += (s, e) => SetFormVisibility(false);
            pnlFormHeaderBar.Controls.Add(btnCloseForm);

            SetFormVisibility(false);
        }


        // ------------------------------------------------------------------ Full-page form (same tab, back button, section cards)
        private FullPageFormKit? formKit;
        private void InitFullPageForm()
        {
            formKit = new FullPageFormKit(this, tblMasterContent, pnlForm, pnlGridCard, pnlFormHeaderBar, lblFormHeader, btnCloseForm, pnlFormBody, pnlFormActionsBar, "Back to incidents");
            formKit.Build(new[]
            {
                new FullPageFormKit.Section("Dispatch & damage", "Which trip it happened on, how many crates and the estimated cost.", lblDel, cbDelivery, tblQtyCost),
                new FullPageFormKit.Section("What happened", "Cause, who is charged and a description.", lblCause, txtCause, chkChargeable, lblDesc, txtDescription),
                new FullPageFormKit.Section("Photo evidence", "Pictures of the damage.", lblPhoto, tblPhotoModule)
            });
            if (btnToggleForm != null)
            {
                btnToggleForm.Text = "＋ New incident";
                UIThemeHelper.SetButtonRole(btnToggleForm, "primary");
            }
            gridIncidents.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || gridIncidents.Rows[e.RowIndex].DataBoundItem is not IncidentReport) return;
                gridIncidents.ClearSelection();
                gridIncidents.Rows[e.RowIndex].Selected = true;
                SetFormVisibility(true);
            };
            SetFormVisibility(false);
        }
        private void OpenNewRecordForm()
        {
            ClearForm();
            lblFormHeader.Text = "New incident report";
            SetFormVisibility(true);
        }
        private void SetFormVisibility(bool visible)
        {
            if (formKit != null) formKit.Show(visible);
            else pnlForm.Visible = visible;
            if (btnToggleForm != null) btnToggleForm.Visible = !visible;
            MainForm.Instance?.NotifyDrawerToggled(visible);
        }

        public void RefreshData()
        {
            LoadData();
        }

        /// <summary>A dispatch the user already confirmed for an in-transit incident (so saving doesn't ask again).</summary>
        private int preApprovedIncidentDeliveryId;
        public void PrefillFromTripVariance(int deliveryId, string dispatchNumber, int missingCrates, DateTime tripDate)
        {
            preApprovedIncidentDeliveryId = deliveryId;
            for (int i = 0; i < deliveries.Count; i++)
            {
                if (deliveries[i].Id == deliveryId || deliveries[i].DispatchNumber == dispatchNumber)
                {
                    cbDelivery.SelectedIndex = i;
                    break;
                }
            }
            numDamagedQty.Value = Math.Max(numDamagedQty.Minimum, Math.Min(numDamagedQty.Maximum, missingCrates));
            numEstimatedCost.Value = Math.Max(numEstimatedCost.Minimum, Math.Min(numEstimatedCost.Maximum, missingCrates * 450m));
            txtCause.Text = "Missing / Damaged Crates from Trip Delivery";
            chkChargeable.Checked = true;
            txtDescription.Text = $"Automated variance log: {missingCrates} crate(s) discrepancy noted on delivery {dispatchNumber} on {tripDate:MM/dd/yyyy}.";
        }

        private void SetupGridColumns()
        {
            gridIncidents.ReadOnly = false;
            gridIncidents.EditMode = DataGridViewEditMode.EditOnEnter;
            gridIncidents.AutoGenerateColumns = false;
            gridIncidents.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridIncidents.ScrollBars = ScrollBars.Both;
            gridIncidents.AllowUserToResizeColumns = true;
            gridIncidents.ShowCellToolTips = false;
            gridIncidents.Columns.Clear();
            gridIncidents.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "ChargeableToCrew", HeaderText = "Charge Crew", FillWeight = 10, MinimumWidth = 90, ReadOnly = false });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DispatchNumber", HeaderText = "Dispatch #", FillWeight = 12, MinimumWidth = 100, ReadOnly = true });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "IncidentDate", HeaderText = "Date", DefaultCellStyle = { Format = "MM/dd/yyyy" }, FillWeight = 10, MinimumWidth = 85, ReadOnly = true });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DamagedQuantity", HeaderText = "Damaged", FillWeight = 9, MinimumWidth = 75, ReadOnly = true });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EstimatedCost", HeaderText = "Est. Cost", DefaultCellStyle = { Format = "N2" }, FillWeight = 11, MinimumWidth = 90, ReadOnly = true });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Cause", HeaderText = "Cause", FillWeight = 14, MinimumWidth = 100, ReadOnly = true });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Description", HeaderText = "Remarks", FillWeight = 18, MinimumWidth = 120, ReadOnly = true });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PhotoPath", HeaderText = "Photo", FillWeight = 8, MinimumWidth = 65, ReadOnly = true });
            gridIncidents.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", FillWeight = 8, MinimumWidth = 75, ReadOnly = true });
        }

        private static void ApplyButtonVisualState(Button btn, bool isEnabled, Color activeBgColor, Color activeFgColor)
        {
            btn.Enabled = isEnabled;
            if (isEnabled)
            {
                btn.BackColor = activeBgColor;
                btn.ForeColor = activeFgColor;
                btn.Cursor = Cursors.Hand;
            }
            else
            {
                btn.BackColor = Pal.C(226, 232, 240);
                btn.ForeColor = Pal.C(148, 163, 184);
                btn.Cursor = Cursors.Default;
            }
        }

        private void SetButtonStates(bool isEditing)
        {
            ApplyButtonVisualState(btnSave, !isEditing, Pal.C(13, 110, 253), Pal.White);
            ApplyButtonVisualState(btnUpdate, isEditing, Pal.C(25, 135, 84), Pal.White);
        }

        private void GridIncidents_SelectionChanged(object? sender, EventArgs e)
        {
            if (pnlForm.Visible) return; // the list is hidden while the form is open; keep what is being typed
            if (gridIncidents.SelectedRows.Count > 0 && gridIncidents.SelectedRows[0].DataBoundItem is IncidentReport inc)
            {
                isBindingForm = true;
                selectedIncident = inc;

                int dIdx = deliveries.FindIndex(d => d.Id == selectedIncident.DeliveryId);
                if (dIdx >= 0 && dIdx < cbDelivery.Items.Count) cbDelivery.SelectedIndex = dIdx;

                numDamagedQty.Value = Math.Max(0, selectedIncident.DamagedQuantity);
                numEstimatedCost.Value = Math.Max(0, selectedIncident.EstimatedCost);
                txtCause.Text = selectedIncident.Cause;
                chkChargeable.Checked = selectedIncident.ChargeableToCrew;
                txtDescription.Text = selectedIncident.Description;

                currentPhotoPath = selectedIncident.PhotoPath ?? "";
                DisplayPhotoPreview(currentPhotoPath);
                lblFormHeader.Text = $"Incident Details: {selectedIncident.DispatchNumber}";
                SetButtonStates(true);
                isBindingForm = false;
            }
        }

        private void DisplayPhotoPreview(string photoPath)
        {
            try
            {
                string abs = SecurityHelper.GetAbsolutePhotoPath(photoPath);
                if (!string.IsNullOrEmpty(abs) && File.Exists(abs))
                {
                    SecurityHelper.SafeSetPictureBoxImage(picPhoto, abs);
                    btnViewPhoto.Enabled = true;
                    btnRemovePhoto.Enabled = true;
                }
                else
                {
                    SecurityHelper.SafeSetPictureBoxImage(picPhoto, null);
                    btnViewPhoto.Enabled = false;
                    btnRemovePhoto.Enabled = false;
                }
            }
            catch
            {
                SecurityHelper.SafeSetPictureBoxImage(picPhoto, null);
                btnViewPhoto.Enabled = false;
                btnRemovePhoto.Enabled = false;
            }
        }

        private void LoadData()
        {
            try
            {
                allIncidents = MJdatabase.GetIncidentReports();
                var withIncidents = new HashSet<int>(allIncidents.Select(i => i.DeliveryId));
                // A dispatch can get an incident once it has left (on the road) or is completed — not while it's still being prepared, and never once cancelled.
                deliveries = MJdatabase.GetDeliveries()
                    .Where(d => DispatchGate.IsCompleted(d.Status) || DispatchGate.HasDeparted(d.Status) || withIncidents.Contains(d.Id))
                    .ToList();
                cbDelivery.Items.Clear();
                foreach (var d in deliveries)
                {
                    cbDelivery.Items.Add($"{d.DispatchNumber} - {d.LoadingScheduleInfo} [{d.Status}]");
                }
                if (cbDelivery.Items.Count > 0) cbDelivery.SelectedIndex = 0;

                FilterGridData();

                int totalDamaged = allIncidents.Sum(i => i.DamagedQuantity);
                decimal totalCost = allIncidents.Sum(i => i.EstimatedCost);
                lblSummary.Text = $"📊 Total Incident Reports: {allIncidents.Count:N0}  |  📦 Total Damaged: {totalDamaged:N0} Units  |  ⚠️ Estimated Loss: PHP {totalCost:N2}";

                ClearForm();
                CheckAndPromptDraft();
            }
            catch (Exception ex)
            { ErrLog.Error("IncidentReportView", ex); 
                MessageBox.Show($"Failed to load incident reports: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUploadPhoto_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Select Cargo Damage / Incident Proof Photo",
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp;*.webp)|*.jpg;*.jpeg;*.png;*.bmp;*.webp|All Files (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (!SecurityHelper.IsValidImageFile(ofd.FileName, out string errorMsg))
                    {
                        MessageBox.Show(errorMsg, "Invalid Image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        string savedRelPath = SecurityHelper.SaveReferencePhoto(ofd.FileName, "Incidents");
                        currentPhotoPath = savedRelPath;
                        DisplayPhotoPreview(currentPhotoPath);
                        QuietBox.Show("Photo uploaded and attached successfully for reference!", "Upload Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    { ErrLog.Error("IncidentReportView", ex); 
                        MessageBox.Show($"Failed to save photo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnViewPhoto_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentPhotoPath))
            {
                QuietBox.Show("No photo attached to view.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PhotoViewerDialog viewer = new PhotoViewerDialog(currentPhotoPath, $"Incident Photo - {txtCause.Text}");
            viewer.ShowDialog(this);
        }

        private void BtnRemovePhoto_Click(object? sender, EventArgs e)
        {
            currentPhotoPath = string.Empty;
            DisplayPhotoPreview(currentPhotoPath);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                var valErrors = new List<string>();
                Control? firstInvalid = null;
                ValidationHelper.ClearErrors(pnlForm);

                if (cbDelivery.SelectedIndex < 0)
                {
                    ValidationHelper.ApplyValidationState(cbDelivery, false, "Trip / Delivery selection is required.");
                    valErrors.Add("Trip / Delivery selection is required.");
                    firstInvalid ??= cbDelivery;
                }
                if (!ValidationHelper.ValidateRequiredText(txtCause, "Incident Cause", valErrors)) firstInvalid ??= txtCause;
                if (!ValidationHelper.ValidateRequiredText(txtDescription, "Incident Description / Details", valErrors)) firstInvalid ??= txtDescription;

                if (!ValidationHelper.ShowValidationSummaryIfAny(this, valErrors, firstInvalid))
                {
                    return;
                }

                if (cbDelivery.SelectedIndex < 0)
                {
                    MessageBox.Show("Please select a trip order.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selDel = DispatchGate.Fresh(deliveries[cbDelivery.SelectedIndex]);
                if (selDel.Id != preApprovedIncidentDeliveryId && !DispatchGate.AllowIncident(this, selDel)) return;
                if (string.IsNullOrWhiteSpace(txtCause.Text))
                {
                    MessageBox.Show("Please enter or select the primary cause of the incident.", "Incident Cause Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCause.Focus();
                    return;
                }

                if (!ValidateIncidentAmounts(selDel)) return;
                IncidentReport ir = new IncidentReport
                {
                    DeliveryId = selDel.Id,
                    DispatchNumber = selDel.DispatchNumber,
                    IncidentDate = DateTime.Now,
                    DamagedQuantity = (int)numDamagedQty.Value,
                    EstimatedCost = numEstimatedCost.Value,
                    Cause = txtCause.Text.Trim(),
                    ChargeableToCrew = chkChargeable.Checked,
                    Description = txtDescription.Text.Trim(),
                    Status = "Logged",
                    PhotoPath = currentPhotoPath
                };

                MJdatabase.SaveIncidentReport(ir);
                MJdatabase.ClearFormDraft("IncidentReport", SessionContext.CurrentUsername);
                if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;
                QuietBox.Show("Incident & cargo damage log saved successfully!", "Log Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            { ErrLog.Error("IncidentReportView", ex); 
                MessageBox.Show($"Failed to save incident report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedIncident == null)
                {
                    MessageBox.Show("Please select an incident record from the table to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtCause.Text))
                {
                    MessageBox.Show("Please enter or select the primary cause of the incident.", "Incident Cause Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCause.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtDescription.Text))
                {
                    MessageBox.Show("Please describe what happened.", "Description Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtDescription.Focus();
                    return;
                }
                if (!ValidateIncidentAmounts(RowMenu.FindDelivery(selectedIncident.DeliveryId, selectedIncident.DispatchNumber))) return;
                selectedIncident.DamagedQuantity = (int)numDamagedQty.Value;
                selectedIncident.EstimatedCost = numEstimatedCost.Value;
                selectedIncident.Cause = txtCause.Text.Trim();
                selectedIncident.ChargeableToCrew = chkChargeable.Checked;
                selectedIncident.Description = txtDescription.Text.Trim();
                selectedIncident.PhotoPath = currentPhotoPath;

                MJdatabase.SaveIncidentReport(selectedIncident);
                QuietBox.Show("Incident record updated successfully!", "Update Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
                SetFormVisibility(false);
            }
            catch (Exception ex)
            { ErrLog.Error("IncidentReportView", ex); 
                MessageBox.Show($"Failed to update incident report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (selectedIncident == null)
                {
                    MessageBox.Show("Please select an incident to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var res = MessageBox.Show($"Are you sure you want to delete incident log for Dispatch '{selectedIncident.DispatchNumber}'?\n\n" + MJdatabase.DeletePolicyNote("IncidentReports"), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (res == DialogResult.Yes)
                {
                    bool deleted = MJdatabase.DeleteRecord("IncidentReports", selectedIncident.Id);
                    if (deleted)
                    {
                        QuietBox.Show(MJdatabase.LastDeleteSummary, "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete incident report. The record may be referenced by other data or an error occurred.", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            { ErrLog.Error("IncidentReportView", ex); 
                MessageBox.Show($"Failed to delete incident report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            selectedIncident = null;
            if (cbDelivery.Items.Count > 0) cbDelivery.SelectedIndex = 0;
            numDamagedQty.Value = 2;
            numEstimatedCost.Value = 1500;
            txtCause.Text = "";
            chkChargeable.Checked = false;
            txtDescription.Text = "";
            currentPhotoPath = string.Empty;
            DisplayPhotoPreview(currentPhotoPath);
            lblFormHeader.Text = "Incident Details Form";
            SetButtonStates(false);
            gridIncidents.ClearSelection();
        }

        private void FilterGridData()
        {
            string q = txtGridSearch.Text.Trim().ToLowerInvariant();
            var filtered = string.IsNullOrEmpty(q)
                ? allIncidents
                : allIncidents.Where(i =>
                    (!string.IsNullOrEmpty(i.DispatchNumber) && i.DispatchNumber.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(i.Cause) && i.Cause.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(i.Description) && i.Description.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(i.Status) && i.Status.ToLower().Contains(q))
                ).ToList();

            filtered = ApplyDateChip(filtered);
            switch (currentSortColumn)
            {
                case "Id":
                    filtered = isSortAscending ? filtered.OrderBy(i => i.Id).ToList() : filtered.OrderByDescending(i => i.Id).ToList();
                    break;
                case "DispatchNumber":
                    filtered = isSortAscending ? filtered.OrderBy(i => i.DispatchNumber).ToList() : filtered.OrderByDescending(i => i.DispatchNumber).ToList();
                    break;
                case "IncidentDate":
                    filtered = isSortAscending ? filtered.OrderBy(i => i.IncidentDate).ToList() : filtered.OrderByDescending(i => i.IncidentDate).ToList();
                    break;
                case "DamagedQuantity":
                    filtered = isSortAscending ? filtered.OrderBy(i => i.DamagedQuantity).ToList() : filtered.OrderByDescending(i => i.DamagedQuantity).ToList();
                    break;
                case "EstimatedCost":
                    filtered = isSortAscending ? filtered.OrderBy(i => i.EstimatedCost).ToList() : filtered.OrderByDescending(i => i.EstimatedCost).ToList();
                    break;
                case "Cause":
                    filtered = isSortAscending ? filtered.OrderBy(i => i.Cause).ToList() : filtered.OrderByDescending(i => i.Cause).ToList();
                    break;
                case "Status":
                    filtered = isSortAscending ? filtered.OrderBy(i => i.Status).ToList() : filtered.OrderByDescending(i => i.Status).ToList();
                    break;
            }

            foreach (DataGridViewColumn col in gridIncidents.Columns)
            {
                string baseText = col.HeaderText.Replace(" ▲", "").Replace(" ▼", "");
                col.HeaderText = col.DataPropertyName == currentSortColumn ? baseText + (isSortAscending ? " ▲" : " ▼") : baseText;
            }

            gridIncidents.SelectionChanged -= GridIncidents_SelectionChanged;
            gridIncidents.DataSource = null;
            gridIncidents.DataSource = filtered;
            lblGridHeader.Text = string.IsNullOrEmpty(q)
                ? $"Active Incident & Damage Logs ({allIncidents.Count})"
                : $"Active Incident & Damage Logs ({filtered.Count} of {allIncidents.Count})";

            if (pnlForm.Visible)
            {
                // The form is open (new or edit): leave what is being typed alone.
            }
            else if (filtered.Count > 0)
            {
                gridIncidents.ClearSelection();
                gridIncidents.Rows[0].Selected = true;
                isBindingForm = true;
                selectedIncident = filtered[0];
                int dIdx = deliveries.FindIndex(d => d.Id == selectedIncident.DeliveryId);
                if (dIdx >= 0 && dIdx < cbDelivery.Items.Count) cbDelivery.SelectedIndex = dIdx;
                numDamagedQty.Value = Math.Max(0, selectedIncident.DamagedQuantity);
                numEstimatedCost.Value = Math.Max(0, selectedIncident.EstimatedCost);
                txtCause.Text = selectedIncident.Cause;
                chkChargeable.Checked = selectedIncident.ChargeableToCrew;
                txtDescription.Text = selectedIncident.Description;
                currentPhotoPath = selectedIncident.PhotoPath ?? "";
                DisplayPhotoPreview(currentPhotoPath);
                lblFormHeader.Text = "Incident Details Form";
                isBindingForm = false;
            }
            else
            {
                isBindingForm = true;
                selectedIncident = null;
                txtDescription.Text = "";
                txtCause.Text = "";
                numDamagedQty.Value = 0;
                numEstimatedCost.Value = 0;
                lblFormHeader.Text = "Incident Details Form";
                isBindingForm = false;
            }
            gridIncidents.SelectionChanged += GridIncidents_SelectionChanged;
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

        public void FilterByDispatch(string dispatchNumber)
        {
            if (txtGridSearch != null)
            {
                txtGridSearch.Text = dispatchNumber ?? "";
                txtGridSearch.Focus();
            }
        }

        private void PrintSelectedDeliveryDossier()
        {
            var inc = selectedIncident;
            if (inc == null && gridIncidents.CurrentRow != null && gridIncidents.CurrentRow.DataBoundItem is IncidentReport rowInc)
            {
                inc = rowInc;
            }
            if (inc == null)
            {
                QuietBox.Show("Please select an incident record first.", "Select Incident", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            PrintHelper.PrintEndToEndDeliveryDossier(inc.DeliveryId, inc.DispatchNumber);
        }

        private void ProceedToTripReports()
        {
            var inc = selectedIncident;
            if (inc == null && gridIncidents.CurrentRow != null && gridIncidents.CurrentRow.DataBoundItem is IncidentReport rowInc)
            {
                inc = rowInc;
            }
            if (inc != null)
            {
                OnNavigateToTripReportRequested?.Invoke(inc.DeliveryId, inc.DispatchNumber);
            }
            else
            {
                OnNavigateToTripReportRequested?.Invoke(0, "");
            }
        }

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            try
            {
                PrintHelper.PrintGridReport("Incident & Cargo Damage Incident Logs", gridIncidents, "Official log of cargo damage, transit incidents, and driver liability charges.");
            }
            catch (Exception ex)
            { ErrLog.Error("IncidentReportView", ex); 
                MessageBox.Show($"Failed to generate print report: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupContextMenu()
        {
            RowMenu.Attach(gridIncidents, (m, rows) =>
            {
                if (rows[0].DataBoundItem is not IncidentReport inc) return;
                selectedIncident = inc;
                bool resolved = RowMenu.Is(inc.Status, "Resolved", "Closed");
                m.Items.AddRange(RowMenu.Header($"⚠️  Incident on {inc.DispatchNumber}",
                    $"{inc.IncidentDate:MMM d, yyyy} · {RowMenu.Short(inc.Cause, 30)} · {inc.DamagedQuantity} crates · {RowMenu.Money(inc.EstimatedCost)} · {inc.Status}"));
                m.Items.Add(RowMenu.Item("Open details", () => RecordDetailsDialog.ShowDetails(this, inc)));
                m.Items.Add(RowMenu.Item("Edit incident", () => { SetFormVisibility(true); txtCause.Focus(); }));
                m.Items.Add(RowMenu.Item($"Open dispatch {inc.DispatchNumber}", () => RowMenu.OpenDispatch(this, inc.DeliveryId, inc.DispatchNumber)));
                m.Items.Add(RowMenu.Item("Go to the trip report", () => ProceedToTripReports()));
                m.Items.Add(RowMenu.Sep());
                if (!resolved) m.Items.Add(RowMenu.Good("Mark as resolved", () => SetIncidentStatus(inc, "Resolved")));
                else m.Items.Add(RowMenu.Item("Reopen incident", () => SetIncidentStatus(inc, "Logged")));
                m.Items.Add(RowMenu.Item(inc.ChargeableToCrew ? "Don't charge to crew" : "Charge to crew", () => ToggleChargeable(inc)));
                if (!string.IsNullOrWhiteSpace(inc.PhotoPath) && File.Exists(inc.PhotoPath))
                    m.Items.Add(RowMenu.Item("View photo", () => { using var v = new PhotoViewerDialog(inc.PhotoPath); v.ShowDialog(this); }));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Sub("🖨️  Print",
                    RowMenu.Item("End-to-end delivery dossier", () => PrintSelectedDeliveryDossier()),
                    RowMenu.Item("Incident log (table)", () => BtnPrint_Click(this, EventArgs.Empty))));
                m.Items.Add(RowMenu.CopySub(this, ("Dispatch number", inc.DispatchNumber), ("Cause", inc.Cause), ("Description", inc.Description),
                    ("Summary", $"Incident on {inc.DispatchNumber} ({inc.IncidentDate:MMM d, yyyy}): {inc.Cause}. {inc.DamagedQuantity} crates, est. {RowMenu.Money(inc.EstimatedCost)}{(inc.ChargeableToCrew ? ", chargeable to crew" : "")}. Status: {inc.Status}.")));
                m.Items.Add(RowMenu.Item("Other incidents on this dispatch", () => RowMenu.Search(txtGridSearch, inc.DispatchNumber)));
                m.Items.Add(RowMenu.ExportSub(gridIncidents, "Incident_Reports", "Incident and Cargo Damage Logs", () => BtnPrint_Click(this, EventArgs.Empty)));
                m.Items.Add(RowMenu.Sep());
                m.Items.Add(RowMenu.Danger("Delete incident…", () => { selectedIncident = inc; BtnDelete_Click(this, EventArgs.Empty); }));
            });
        }
        private void SetIncidentStatus(IncidentReport inc, string status)
        {
            string old = inc.Status;
            inc.Status = status;
            try
            {
                MJdatabase.SaveIncidentReport(inc);
                ToastHelper.ShowToast(this, $"Incident on {inc.DispatchNumber} is now {status.ToLowerInvariant()}", ToastType.Success);
            }
            catch (Exception ex)
            { ErrLog.Error("IncidentReportView", ex); 
                inc.Status = old;
                MessageBox.Show($"Failed to update the incident: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            LoadData();
        }
        private void ToggleChargeable(IncidentReport inc)
        {
            bool next = !inc.ChargeableToCrew;
            if (next && inc.EstimatedCost <= 0)
            {
                QuietBox.Show("Enter an estimated cost first (Edit incident) — there is nothing to charge to the crew yet.", "No cost to charge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            MJdatabase.ToggleIncidentChargeable(inc.Id, next);
            inc.ChargeableToCrew = next;
            LoadData();
        }
        /// <summary>Checks shared by saving a new incident and saving changes.</summary>
        private bool ValidateIncidentAmounts(Delivery? trip)
        {
            int qty = (int)numDamagedQty.Value;
            if (trip != null && trip.TotalCrates > 0 && qty > trip.TotalCrates)
            {
                MessageBox.Show($"Damaged / missing crates ({qty}) can't be more than the {trip.TotalCrates} crates loaded on dispatch {trip.DispatchNumber}.", "Check the crate count", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numDamagedQty.Focus();
                return false;
            }
            if (chkChargeable.Checked && numEstimatedCost.Value <= 0)
            {
                MessageBox.Show("This incident is marked chargeable to the crew, but the estimated cost is ₱0.00. Enter the cost to charge, or untick 'chargeable'.", "Cost Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numEstimatedCost.Focus();
                return false;
            }
            return true;
        }

        private void SetupDraftAutoSave()
        {
            tmrAutoSaveDraft = new System.Windows.Forms.Timer { Interval = 15000 };
            tmrAutoSaveDraft.Tick += (s, e) => SaveCurrentDraft();
            tmrAutoSaveDraft.Start();
        }

        private void CheckAndPromptDraft()
        {
            try
            {
                var draft = MJdatabase.GetFormDraft("IncidentReport", SessionContext.CurrentUsername);
                if (draft != null && !string.IsNullOrWhiteSpace(draft.Value.Data))
                {
                    if (pnlDraftBanner != null) { pnlDraftBanner.Parent?.Controls.Remove(pnlDraftBanner); pnlDraftBanner.Dispose(); pnlDraftBanner = null; }
                    pnlDraftBanner = DraftAutoSaveService.CreateDraftBanner("Incident Report", draft.Value.LastSaved, RestoreDraft, DiscardDraft);
                    pnlFormBody.Controls.Add(pnlDraftBanner);
                    pnlDraftBanner.BringToFront();
                }
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[IncidentReport] CheckAndPromptDraft: {ex.Message}"); ErrLog.Swallowed(ex); }
        }

        private void RestoreDraft()
        {
            try
            {
                var draft = MJdatabase.GetFormDraft("IncidentReport", SessionContext.CurrentUsername);
                if (draft == null || string.IsNullOrWhiteSpace(draft.Value.Data)) return;
                var map = DraftAutoSaveService.DeserializeDraft(draft.Value.Data);
                isBindingForm = true;

                if (map.TryGetValue("DeliveryIndex", out var dIdx) && int.TryParse(dIdx, out int idx) && idx >= 0 && idx < cbDelivery.Items.Count)
                {
                    cbDelivery.SelectedIndex = idx;
                }
                if (map.TryGetValue("DamagedQuantity", out var dq) && decimal.TryParse(dq, out var dqVal))
                {
                    numDamagedQty.Value = Math.Min(numDamagedQty.Maximum, Math.Max(numDamagedQty.Minimum, dqVal));
                }
                if (map.TryGetValue("EstimatedCost", out var ec) && decimal.TryParse(ec, out var ecVal))
                {
                    numEstimatedCost.Value = Math.Min(numEstimatedCost.Maximum, Math.Max(numEstimatedCost.Minimum, ecVal));
                }
                if (map.TryGetValue("Cause", out var c) && !string.IsNullOrWhiteSpace(c))
                {
                    txtCause.Text = c;
                }
                if (map.TryGetValue("Chargeable", out var ch) && bool.TryParse(ch, out var chVal))
                {
                    chkChargeable.Checked = chVal;
                }
                if (map.TryGetValue("Description", out var desc) && !string.IsNullOrWhiteSpace(desc))
                {
                    txtDescription.Text = desc;
                }
                if (map.TryGetValue("PhotoPath", out var pp))
                {
                    currentPhotoPath = pp ?? "";
                    DisplayPhotoPreview(currentPhotoPath);
                }

                isBindingForm = false;
                ToastHelper.ShowToast(this, "Incident Report draft restored ✓", ToastType.Success);
            }
            catch (Exception ex)
            {
                isBindingForm = false;
                System.Diagnostics.Debug.WriteLine($"[IncidentReport] RestoreDraft: {ex.Message}");
            }
        }

        private void DiscardDraft()
        {
            MJdatabase.ClearFormDraft("IncidentReport", SessionContext.CurrentUsername);
            if (pnlDraftBanner != null) pnlDraftBanner.Visible = false;
            ToastHelper.ShowToast(this, "Incident Report draft discarded ✓", ToastType.Info);
        }

        private void SaveCurrentDraft()
        {
            if (isBindingForm || selectedIncident != null) return;
            if (string.IsNullOrWhiteSpace(txtCause.Text) && string.IsNullOrWhiteSpace(txtDescription.Text) && numDamagedQty.Value == 0 && string.IsNullOrWhiteSpace(currentPhotoPath)) return;

            try
            {
                var map = new Dictionary<string, string>
                {
                    ["DeliveryIndex"] = cbDelivery.SelectedIndex.ToString(),
                    ["DamagedQuantity"] = numDamagedQty.Value.ToString(),
                    ["EstimatedCost"] = numEstimatedCost.Value.ToString(),
                    ["Cause"] = txtCause.Text.Trim(),
                    ["Chargeable"] = chkChargeable.Checked.ToString(),
                    ["Description"] = txtDescription.Text.Trim(),
                    ["PhotoPath"] = currentPhotoPath
                };
                string json = DraftAutoSaveService.SerializeDraft(map);
                MJdatabase.SaveFormDraft("IncidentReport", SessionContext.CurrentUsername, json);
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[IncidentReport] SaveCurrentDraft: {ex.Message}"); ErrLog.Swallowed(ex); }
        }
    }
}
