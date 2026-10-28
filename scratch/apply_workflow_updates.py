import os

def update_file(path, target, replacement):
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()
    assert target in content, f"Target not found in {path}"
    content = content.replace(target, replacement, 1)
    with open(path, 'w', encoding='utf-8', newline='\r\n') as f:
        f.write(content)
    print(f"Successfully updated {path}")

# 1. Update Views/DispatchManagementView.cs
dispatch_path = 'Views/DispatchManagementView.cs'

# Remove btnActions from pnlGridActionsFlow
target_actions = """            Button btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);

            pnlGridActionsFlow.Controls.Add(btnActions);"""
repl_actions = """            Button btnActions = UIThemeHelper.CreateActionsDropdown(menuActions);"""
update_file(dispatch_path, target_actions, repl_actions)

# Update InitializeFormToggle and SetFormVisibility
target_toggle = """        private void InitializeFormToggle()

        {

            btnToggleForm = new Button

            {

                Text = \"📝 + New Order / Form ▾\",

                Size = new Size(165, 30),

                BackColor = Color.FromArgb(30, 41, 59),

                ForeColor = Color.White,

                FlatStyle = FlatStyle.Flat,

                Font = new Font(\"Segoe UI Semibold\", 9F, FontStyle.Bold),

                Cursor = Cursors.Hand,

                Margin = new Padding(0, 4, 8, 4)

            };

            btnToggleForm.FlatAppearance.BorderSize = 0;

            btnToggleForm.Click += (s, e) => SetFormVisibility(!pnlForm.Visible);

            pnlGridActionsFlow.Controls.Add(btnToggleForm);

            pnlGridActionsFlow.Controls.SetChildIndex(btnToggleForm, 0);



            SetFormVisibility(false);

        }"""

repl_toggle = """        private void InitializeFormToggle()

        {

            btnToggleForm = new Button

            {

                Text = \"📝 + Add Order ▾\",

                Size = new Size(150, 30),

                BackColor = Color.FromArgb(185, 28, 28),

                ForeColor = Color.White,

                FlatStyle = FlatStyle.Flat,

                Font = new Font(\"Segoe UI Semibold\", 9F, FontStyle.Bold),

                Cursor = Cursors.Hand,

                Margin = new Padding(12, 4, 8, 4)

            };

            btnToggleForm.FlatAppearance.BorderSize = 0;

            btnToggleForm.Click += (s, e) => SetFormVisibility(!pnlForm.Visible);

            pnlGridActionsFlow.Controls.Add(btnToggleForm);



            SetFormVisibility(false);

        }"""
update_file(dispatch_path, target_toggle, repl_toggle)

target_toggle_text = """                btnToggleForm.Text = visible ? \"✕ Hide Form ▴\" : \"📝 + New Order / Form ▾\";

                btnToggleForm.BackColor = visible ? Color.FromArgb(226, 232, 240) : Color.FromArgb(30, 41, 59);

                btnToggleForm.ForeColor = visible ? Color.FromArgb(15, 23, 42) : Color.White;"""

repl_toggle_text = """                btnToggleForm.Text = visible ? \"✕ Hide Form ▴\" : \"📝 + Add Order ▾\";

                btnToggleForm.BackColor = visible ? Color.FromArgb(226, 232, 240) : Color.FromArgb(185, 28, 28);

                btnToggleForm.ForeColor = visible ? Color.FromArgb(15, 23, 42) : Color.White;"""
update_file(dispatch_path, target_toggle_text, repl_toggle_text)

# Add EditDeliveryOrder in DispatchManagementView.cs
target_initiate = """        public void InitiateDispatchForSchedule(LoadingSchedule schedule)"""

repl_initiate = """        public void EditDeliveryOrder(Delivery delivery)

        {

            if (delivery == null) return;

            try

            {

                SetFormVisibility(true);

                BindDeliveryToForm(delivery);

                for (int i = 0; i < gridDispatch.Rows.Count; i++)

                {

                    if (gridDispatch.Rows[i].DataBoundItem is Delivery rowD && rowD.Id == delivery.Id)

                    {

                        gridDispatch.ClearSelection();

                        gridDispatch.Rows[i].Selected = true;

                        break;

                    }

                }

                txtDispatchNumber.Focus();

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($\"[Dispatch] EditDeliveryOrder: {ex.Message}\");

            }

        }



        public void InitiateDispatchForSchedule(LoadingSchedule schedule)"""
update_file(dispatch_path, target_initiate, repl_initiate)


# 2. Update Functions/RecordDetailsDialog.cs
rec_path = 'Functions/RecordDetailsDialog.cs'

target_rec_footer = """            Button btnCopy = new Button
            {
                Text = "📋 Copy Full Specifications",
                Height = 36,
                Width = 200,
                Location = new Point(16, 10),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCopy.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCopy.Click += (s, e) =>
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"=== {title.ToUpper()} ===");
                sb.AppendLine($"Status: {statusBadge}");
                sb.AppendLine("--------------------------------------------------");
                foreach (var kv in keyValues)
                {
                    sb.AppendLine($"{kv.Label.PadRight(30)}: {kv.Value}");
                }
                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                try
                {
                    Clipboard.SetText(sb.ToString());
                    ToastHelper.ShowToast(dlg, "Record details copied to clipboard ✓", ToastType.Success);
                }
                catch { }
            };

            Button btnPrint = new Button
            {
                Text = "🖨️ Print Summary",
                Height = 36,
                Width = 140,
                Location = new Point(226, 10),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };"""

repl_rec_footer = """            Button btnPrint = new Button
            {
                Text = "🖨️ Print Summary",
                Height = 36,
                Width = 140,
                Location = new Point(16, 10),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            if (record is Delivery del)
            {
                Button btnUpdate = new Button
                {
                    Text = "✏️ Update Order",
                    Height = 36,
                    Width = 145,
                    Location = new Point(16, 10),
                    BackColor = Color.FromArgb(30, 41, 59),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnUpdate.FlatAppearance.BorderSize = 0;
                btnUpdate.Click += (s, e) =>
                {
                    dlg.Close();
                    if (parent is Views.DispatchManagementView dispView)
                    {
                        dispView.EditDeliveryOrder(del);
                    }
                    else if (MainForm.Instance != null)
                    {
                        MainForm.Instance.EditDeliveryOrderInDispatch(del);
                    }
                };

                Button btnCancelOrder = new Button
                {
                    Text = "🚫 Cancel Order",
                    Height = 36,
                    Width = 145,
                    Location = new Point(169, 10),
                    BackColor = Color.FromArgb(220, 38, 38),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnCancelOrder.FlatAppearance.BorderSize = 0;
                btnCancelOrder.Click += (s, e) =>
                {
                    if (del.Status == "Cancelled")
                    {
                        MessageBox.Show("This dispatch order is already cancelled.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    string? reason = CancellationReasonDialog.PromptCancellationReason(dlg, "Dispatch Order", del.DispatchNumber);
                    if (string.IsNullOrEmpty(reason)) return;

                    del.Status = "Cancelled";
                    del.CancellationReason = reason;
                    MJdatabase.SaveDelivery(del);
                    ToastHelper.ShowToast(parentForm ?? dlg, $"Dispatch order {del.DispatchNumber} cancelled ✓", ToastType.Warning);

                    if (parent is IRefreshable refView) refView.RefreshData();
                    else if (parent is Views.DispatchManagementView dv) dv.RefreshData();

                    dlg.Close();
                };

                btnPrint.Location = new Point(322, 10);
                pnlFooter.Controls.Add(btnUpdate);
                pnlFooter.Controls.Add(btnCancelOrder);
            }"""
update_file(rec_path, target_rec_footer, repl_rec_footer)

target_add_copy = """            pnlFooter.Controls.Add(btnCopy);
            pnlFooter.Controls.Add(btnPrint);
            pnlFooter.Controls.Add(btnClose);"""

repl_add_copy = """            pnlFooter.Controls.Add(btnPrint);
            pnlFooter.Controls.Add(btnClose);"""
update_file(rec_path, target_add_copy, repl_add_copy)


# 3. Update Views/OperationsHubView.cs
ops_path = 'Views/OperationsHubView.cs'

target_tab = """            AddTab(\"History\", \"📜  Loading & Dispatch History\", () => new ModuleHistoryView(\"Loading & Dispatch History\", new[] { \"LoadingSchedules\", \"Deliveries\" }, isAdminOnly: false));"""
repl_tab = """            AddTab(\"History\", \"📜  Order History\", () => new ModuleHistoryView(\"Order History\", new[] { \"LoadingSchedules\", \"Deliveries\" }, isAdminOnly: false));"""
update_file(ops_path, target_tab, repl_tab)

target_bridge = """        public void DispatchLoadingSchedule(LoadingSchedule schedule)"""
repl_bridge = """        public void EditDeliveryInDispatch(Delivery delivery)
        {
            SelectTab("Dispatch");
            if (cachedViews.TryGetValue("Dispatch", out var view) && view is DispatchManagementView dsp)
            {
                dsp.EditDeliveryOrder(delivery);
            }
        }

        public void DispatchLoadingSchedule(LoadingSchedule schedule)"""
update_file(ops_path, target_bridge, repl_bridge)


# 4. Update MainForm.cs
main_path = 'MainForm.cs'

target_main_bridge = """        public void NavigateToOperations(string tab = "Dispatch")"""
repl_main_bridge = """        public void EditDeliveryOrderInDispatch(Delivery delivery)
        {
            NavigateToOperations("Dispatch");
            if (viewCache.TryGetValue("OperationsHubView", out var ov) && ov is OperationsHubView ohv)
            {
                ohv.EditDeliveryInDispatch(delivery);
            }
        }

        public void NavigateToOperations(string tab = "Dispatch")"""
update_file(main_path, target_main_bridge, repl_main_bridge)

print("All code updates applied successfully!")
