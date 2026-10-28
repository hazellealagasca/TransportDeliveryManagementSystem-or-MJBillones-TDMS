path = 'Views/DispatchManagementView.cs'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

target = '''            using (var dlg = new RescheduleDeliveryDialog(target))
            {
                if (dlg.ShowDialog(this.FindForm() ?? this.ParentForm) == DialogResult.OK)
                {
                    ClearFields();
                    LoadDropdownsAndGrid();
                }
            }'''

replacement = '''            if (dtpDispatchDate.Value >= dtpDispatchDate.MinDate && dtpDispatchDate.Value <= dtpDispatchDate.MaxDate)
            {
                target.DispatchDate = dtpDispatchDate.Value.Date;
            }
            if (!string.IsNullOrWhiteSpace(cbDepartureTime.Text))
            {
                target.DepartureTime = cbDepartureTime.Text.Trim();
            }
            if (cbTruck.SelectedIndex >= 0 && cbTruck.SelectedIndex < trucks.Count)
            {
                target.TruckId = trucks[cbTruck.SelectedIndex].Id;
                target.TruckInfo = $"{trucks[cbTruck.SelectedIndex].PlateNumber} [{trucks[cbTruck.SelectedIndex].TruckType}]";
            }
            if (cbDriver.SelectedIndex >= 0 && cbDriver.SelectedIndex < drivers.Count)
            {
                target.DriverId = drivers[cbDriver.SelectedIndex].Id;
                target.DriverName = drivers[cbDriver.SelectedIndex].FullName;
            }
            var (activeAideIds, activeAideNames) = GetSelectedAides();
            if (activeAideIds.Count > 0)
            {
                target.AideId = activeAideIds[0];
                target.AideName = string.Join(", ", activeAideNames);
            }

            using (var dlg = new RescheduleDeliveryDialog(target))
            {
                if (dlg.ShowDialog(this.FindForm() ?? this.ParentForm) == DialogResult.OK)
                {
                    LoadDropdownsAndGrid();
                    var reloaded = MJdatabase.GetDeliveryById(target.Id);
                    if (reloaded != null)
                    {
                        BindDeliveryToForm(reloaded);
                        cbStatus.Text = "Rescheduled";
                    }
                    else
                    {
                        target.DispatchDate = dlg.RescheduledDate;
                        target.DepartureTime = dlg.RescheduledTime;
                        target.Status = "Rescheduled";
                        BindDeliveryToForm(target);
                    }
                }
            }'''

if target in text:
    text = text.replace(target, replacement, 1)
    print('Replaced target (LF)')
elif target.replace('\n', '\r\n') in text:
    text = text.replace(target.replace('\n', '\r\n'), replacement.replace('\n', '\r\n'), 1)
    print('Replaced target (CRLF)')
else:
    raise Exception('target not found')

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print('Updated DispatchManagementView.cs successfully!')
