import os

path = 'Functions/RecordDetailsDialog.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Add using Views
target_using = """using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;"""
repl_using = """using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;"""

assert target_using in content, "target_using not found"
content = content.replace(target_using, repl_using, 1)

# Rename del to targetDel and qualify Views.CancellationReasonDialog
target_block = """            if (record is Delivery del)
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

repl_block = """            if (record is Delivery targetDel)
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
                    if (parent is DispatchManagementView dispView)
                    {
                        dispView.EditDeliveryOrder(targetDel);
                    }
                    else if (MainForm.Instance != null)
                    {
                        MainForm.Instance.EditDeliveryOrderInDispatch(targetDel);
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
                    if (targetDel.Status == "Cancelled")
                    {
                        MessageBox.Show("This dispatch order is already cancelled.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    string? reason = CancellationReasonDialog.PromptCancellationReason(dlg, "Dispatch Order", targetDel.DispatchNumber);
                    if (string.IsNullOrEmpty(reason)) return;

                    targetDel.Status = "Cancelled";
                    targetDel.CancellationReason = reason;
                    MJdatabase.SaveDelivery(targetDel);
                    ToastHelper.ShowToast(parentForm ?? dlg, $"Dispatch order {targetDel.DispatchNumber} cancelled ✓", ToastType.Warning);

                    if (parent is IRefreshable refView) refView.RefreshData();
                    else if (parent is DispatchManagementView dv) dv.RefreshData();

                    dlg.Close();
                };

                btnPrint.Location = new Point(322, 10);
                pnlFooter.Controls.Add(btnUpdate);
                pnlFooter.Controls.Add(btnCancelOrder);
            }"""

assert target_block in content, "target_block not found"
content = content.replace(target_block, repl_block, 1)

with open(path, 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(content)
print("Updated RecordDetailsDialog.cs successfully")
