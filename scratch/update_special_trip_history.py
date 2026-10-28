path = 'Views/SpecialTripHistoryView.cs'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

target_cols = '''            DataGridViewButtonColumn colBtnView = new DataGridViewButtonColumn
            {
                Name = "colBtnView",
                HeaderText = "",
                Text = "👁️ View",
                UseColumnTextForButtonValue = true,
                Width = 70,
                FlatStyle = FlatStyle.Flat
            };

            DataGridViewButtonColumn colBtnComplete = new DataGridViewButtonColumn'''

rep_cols = '''            DataGridViewButtonColumn colBtnView = new DataGridViewButtonColumn
            {
                Name = "colBtnView",
                HeaderText = "",
                Text = "👁️ View",
                UseColumnTextForButtonValue = true,
                Width = 70,
                FlatStyle = FlatStyle.Flat
            };

            DataGridViewButtonColumn colBtnResched = new DataGridViewButtonColumn
            {
                Name = "colBtnResched",
                HeaderText = "",
                Text = "📅 Resched",
                UseColumnTextForButtonValue = true,
                Width = 85,
                FlatStyle = FlatStyle.Flat
            };

            DataGridViewButtonColumn colBtnComplete = new DataGridViewButtonColumn'''

text = text.replace(target_cols, rep_cols, 1)

text = text.replace(
    'gridSpecialTrips.Columns.AddRange(colBtnView, colBtnComplete, colBtnDel);',
    'gridSpecialTrips.Columns.AddRange(colBtnView, colBtnResched, colBtnComplete, colBtnDel);',
    1
)

target_click = '''            int idxView = gridSpecialTrips.Columns["colBtnView"]?.Index ?? -1;
            int idxComp = gridSpecialTrips.Columns["colBtnComplete"]?.Index ?? -1;
            int idxDel = gridSpecialTrips.Columns["colBtnDel"]?.Index ?? -1;

            if (e.ColumnIndex == idxView)
            {
                OpenSpecialTripDialog(trip.Id);
            }
            else if (e.ColumnIndex == idxComp)'''

rep_click = '''            int idxView = gridSpecialTrips.Columns["colBtnView"]?.Index ?? -1;
            int idxResched = gridSpecialTrips.Columns["colBtnResched"]?.Index ?? -1;
            int idxComp = gridSpecialTrips.Columns["colBtnComplete"]?.Index ?? -1;
            int idxDel = gridSpecialTrips.Columns["colBtnDel"]?.Index ?? -1;

            if (e.ColumnIndex == idxView)
            {
                OpenSpecialTripDialog(trip.Id);
            }
            else if (e.ColumnIndex == idxResched)
            {
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

                using (var dlg = new RescheduleDeliveryDialog(trip))
                {
                    if (dlg.ShowDialog(this.FindForm() ?? this.ParentForm) == DialogResult.OK)
                    {
                        LoadData();
                    }
                }
            }
            else if (e.ColumnIndex == idxComp)'''

text = text.replace(target_click, rep_click, 1)

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print("SpecialTripHistoryView.cs updated successfully!")
