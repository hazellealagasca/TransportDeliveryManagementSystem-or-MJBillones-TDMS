path = 'Forms/SpecialTripFormDialog.cs'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

# 1. Add field
text = text.replace(
    'private Button btnClose = null!;\n',
    'private Button btnClose = null!;\n        private Button btnReschedule = null!;\n',
    1
)

# 2. Add btnReschedule setup in InitializeCustomComponent
target_flow = '''            btnSave = new Button
            {
                Text = "💾 Save",'''

btn_resched_code = '''            btnReschedule = new Button
            {
                Text = "📅 Reschedule",
                BackColor = Color.FromArgb(234, 88, 12),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Size = new Size(130, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0),
                Visible = false
            };
            btnReschedule.FlatAppearance.BorderSize = 0;
            btnReschedule.Click += BtnReschedule_Click;

            btnSave = new Button
            {
                Text = "💾 Save",'''

text = text.replace(target_flow, btn_resched_code, 1)

target_add_btns = '''            flowActions.Controls.Add(btnSave);
            flowActions.Controls.Add(btnUpdate);
            flowActions.Controls.Add(btnClear);
            flowActions.Controls.Add(btnClose);'''

rep_add_btns = '''            flowActions.Controls.Add(btnSave);
            flowActions.Controls.Add(btnUpdate);
            flowActions.Controls.Add(btnReschedule);
            flowActions.Controls.Add(btnClear);
            flowActions.Controls.Add(btnClose);'''

text = text.replace(target_add_btns, rep_add_btns, 1)

# 3. High contrast breadcrumbs
text = text.replace(
    'ForeColor = Color.FromArgb(56, 189, 248),',
    'ForeColor = Color.FromArgb(224, 242, 254),',
    1
)

# 4. Spacing for lblTableDesc
text = text.replace(
    'Location = new Point(130, 10)',
    'Location = new Point(145, 10)',
    1
)

# 5. colDepTime width
text = text.replace(
    'DataGridViewTextBoxColumn colDepTime = new DataGridViewTextBoxColumn\n            {\n                Name = "colDepTime",\n                HeaderText = "Departure Time",\n                Width = 130',
    'DataGridViewTextBoxColumn colDepTime = new DataGridViewTextBoxColumn\n            {\n                Name = "colDepTime",\n                HeaderText = "Departure Time",\n                Width = 155',
    1
)

# 6. ResetFormForNew visibility
text = text.replace(
    'btnUpdate.Visible = false;\n            btnSave.Text = "💾 Save";',
    'btnUpdate.Visible = false;\n            btnReschedule.Visible = false;\n            btnSave.Text = "💾 Save";',
    1
)

# 7. LoadExistingTrip visibility
text = text.replace(
    'btnUpdate.Visible = true;\n            btnSave.Text = "💾 Save Copy as New";',
    'btnUpdate.Visible = true;\n            btnReschedule.Visible = true;\n            btnSave.Text = "💾 Save Copy as New";',
    1
)

# 8. Add BtnReschedule_Click
btn_click_handler = '''
        private void BtnReschedule_Click(object? sender, EventArgs e)
        {
            if (currentTripId <= 0)
            {
                MessageBox.Show("Please save or open a valid Special Trip record before rescheduling.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var trip = MJdatabase.GetSpecialTripById(currentTripId);
            if (trip == null)
            {
                MessageBox.Show("Special Trip record could not be found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (trip.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Special Trip '{trip.OrderReference}' has already been completed and cannot be rescheduled.", "Reschedule Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (trip.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Special Trip '{trip.OrderReference}' is cancelled and cannot be rescheduled.", "Reschedule Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dtpDispatchDate.Value >= dtpDispatchDate.MinDate && dtpDispatchDate.Value <= dtpDispatchDate.MaxDate)
            {
                trip.DispatchDate = dtpDispatchDate.Value.Date;
            }
            if (!string.IsNullOrWhiteSpace(cbDepartureTime.Text))
            {
                trip.DepartureTime = cbDepartureTime.Text.Trim();
            }
            if (cbTruck.SelectedIndex > 0 && cbTruck.SelectedIndex - 1 < availableTrucks.Count)
            {
                var trk = availableTrucks[cbTruck.SelectedIndex - 1];
                trip.TruckId = trk.Id;
                trip.TruckPlate = trk.PlateNumber;
            }
            if (cbDriver.SelectedIndex > 0 && cbDriver.SelectedIndex - 1 < availableDrivers.Count)
            {
                var drv = availableDrivers[cbDriver.SelectedIndex - 1];
                trip.DriverId = drv.Id;
                trip.DriverName = drv.FullName;
            }
            if (cbAide.SelectedIndex > 0 && cbAide.SelectedIndex - 1 < availableAides.Count)
            {
                var aid = availableAides[cbAide.SelectedIndex - 1];
                trip.AideId = aid.Id;
                trip.AideName = aid.FullName;
            }

            using (var dlg = new RescheduleDeliveryDialog(trip))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadExistingTrip(trip.Id);
                    cbStatus.Text = "Rescheduled";
                }
            }
        }
'''

target_end = '        private void AddInitialDefaultStop()'
idx_target_end = text.find(target_end)
if idx_target_end != -1:
    text = text[:idx_target_end] + btn_click_handler + '\n' + text[idx_target_end:]
    print("Added BtnReschedule_Click")
else:
    raise Exception("target_end not found")

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print("SpecialTripFormDialog.cs updated successfully!")
