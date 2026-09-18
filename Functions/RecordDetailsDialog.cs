using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    public static class RecordDetailsDialog
    {
        public static void ShowDetails(Control parent, object record)
        {
            if (record == null) return;

            Form? parentForm = parent.FindForm();

            Form dlg = new Form
            {
                Text = "🔍 System Record Details & Activity Inspector",
                Size = new Size(880, 640),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MinimumSize = new Size(760, 520),
                BackColor = Color.FromArgb(248, 250, 252),
                ShowIcon = false,
                ShowInTaskbar = false
            };

            string title = "Record Details";
            string subtitle = "M.J. Billones Transport Service Management System";
            string statusBadge = "Active";
            Color badgeBg = Color.FromArgb(220, 252, 231);
            Color badgeFg = Color.FromArgb(22, 101, 52);

            var keyValues = new List<(string Label, string Value)>();
            var historyData = new List<object>();
            string historyTitle = "Related Operational Activity";

            if (record is Truck truck)
            {
                title = $"🚚 Vehicle Profile: {truck.PlateNumber}";
                subtitle = $"Fleet Vehicle • Category: {truck.TruckType}";
                statusBadge = truck.Status;
                if (truck.Status == "Available") { badgeBg = Color.FromArgb(220, 252, 231); badgeFg = Color.FromArgb(22, 101, 52); }
                else if (truck.Status == "In Use" || truck.Status == "On Trip") { badgeBg = Color.FromArgb(254, 243, 199); badgeFg = Color.FromArgb(180, 83, 9); }
                else { badgeBg = Color.FromArgb(254, 226, 226); badgeFg = Color.FromArgb(185, 28, 28); }

                var allDel = MJdatabase.GetDeliveries().Where(d => d.TruckId == truck.Id || (!string.IsNullOrEmpty(d.TruckInfo) && d.TruckInfo.Contains(truck.PlateNumber))).ToList();
                int totalCratesHailed = allDel.Sum(d => d.TotalCrates);

                keyValues.Add(("Plate Number", truck.PlateNumber));
                keyValues.Add(("Truck Category / Type", truck.TruckType));
                keyValues.Add(("Cargo / Temperature Zone", truck.CargoType));
                keyValues.Add(("Max Crate Capacity", $"{truck.CapacityCrates:N0} Crates"));
                keyValues.Add(("Current Operational Status", truck.Status));
                keyValues.Add(("Total Dispatches Logged", $"{allDel.Count} Trips"));
                keyValues.Add(("Total Crates Hauled", $"{totalCratesHailed:N0} Crates"));
                keyValues.Add(("Vehicle Remarks / Condition", string.IsNullOrEmpty(truck.Remarks) ? "None" : truck.Remarks));

                historyTitle = $"Trip & Dispatch History for {truck.PlateNumber} ({allDel.Count} Dispatches)";
                foreach (var d in allDel.OrderByDescending(x => x.DispatchDate))
                {
                    historyData.Add(new {
                        Dispatch = d.DispatchNumber,
                        Date = d.DispatchDate.ToString("yyyy-MM-dd"),
                        Driver = d.DriverName,
                        Crates = $"{d.TotalCrates} Crates",
                        Status = d.Status,
                        Schedule = d.LoadingScheduleInfo
                    });
                }
            }
            else if (record is Crew crew)
            {
                title = $"👤 Crew Profile: {crew.FullName}";
                subtitle = $"Staff Member • Role: {crew.Role}";
                statusBadge = crew.AvailabilityStatus;
                if (crew.IsAvailable) { badgeBg = Color.FromArgb(220, 252, 231); badgeFg = Color.FromArgb(22, 101, 52); }
                else { badgeBg = Color.FromArgb(254, 243, 199); badgeFg = Color.FromArgb(180, 83, 9); }

                var allDel = MJdatabase.GetDeliveries().Where(d => d.DriverId == crew.Id || (!string.IsNullOrEmpty(d.AideIds) && d.AideIds.Split(',').Contains(crew.Id.ToString())) || (!string.IsNullOrEmpty(d.DriverName) && d.DriverName.Contains(crew.FullName))).ToList();

                keyValues.Add(("Full Name", crew.FullName));
                keyValues.Add(("Designated Role", crew.Role));
                keyValues.Add(("Contact Mobile #", crew.ContactNumber));
                keyValues.Add(("Driver's License #", string.IsNullOrEmpty(crew.LicenseNumber) ? "N/A" : crew.LicenseNumber));
                keyValues.Add(("Employment Type", crew.IsSeasonal ? "Seasonal / Extra" : "Regular Active"));
                                                keyValues.Add(("Current Availability", crew.AvailabilityStatus));
                keyValues.Add(("Total Dispatches Assigned", $"{allDel.Count} Trips"));

                historyTitle = $"Assigned Dispatch History for {crew.FullName} ({allDel.Count} Trips)";
                foreach (var d in allDel.OrderByDescending(x => x.DispatchDate))
                {
                    historyData.Add(new {
                        Dispatch = d.DispatchNumber,
                        Date = d.DispatchDate.ToString("yyyy-MM-dd"),
                        Truck = d.TruckInfo,
                        Crates = $"{d.TotalCrates} Crates",
                        Status = d.Status
                    });
                }
            }
            else if (record is LoadingSchedule sch)
            {
                title = $"📅 Loading Schedule: {sch.ScheduleNumber}";
                subtitle = $"Client Haul Order • {sch.ClientName}";
                statusBadge = sch.Status;
                if (sch.Status == "Dispatched" || sch.Status == "Completed") { badgeBg = Color.FromArgb(220, 252, 231); badgeFg = Color.FromArgb(22, 101, 52); }
                else if (sch.Status == "Cancelled") { badgeBg = Color.FromArgb(254, 226, 226); badgeFg = Color.FromArgb(185, 28, 28); }
                else { badgeBg = Color.FromArgb(241, 245, 249); badgeFg = Color.FromArgb(51, 65, 85); }

                var matchDel = MJdatabase.GetDeliveries().FirstOrDefault(d => d.LoadingScheduleId == sch.Id || (!string.IsNullOrEmpty(d.LoadingScheduleInfo) && d.LoadingScheduleInfo.Contains(sch.ScheduleNumber)));

                keyValues.Add(("Schedule Number", sch.ScheduleNumber));
                keyValues.Add(("Client / Principal", sch.ClientName));
                keyValues.Add(("Delivery Destination", sch.Destination));
                keyValues.Add(("Scheduled Date", sch.ScheduleDate.ToString("MMMM dd, yyyy")));
                keyValues.Add(("Target Departure", sch.DepartureTime));
                keyValues.Add(("G-Crates (Goods)", $"{sch.GCrate:N0} Crates"));
                keyValues.Add(("E-Crates (Empty)", $"{sch.ECrate:N0} Crates"));
                keyValues.Add(("Total Crates", $"{sch.TotalCrates:N0} Crates"));
                keyValues.Add(("Season Rate Tier", sch.IsPeak ? "🔥 Peak Season Rate" : "Regular Season Rate"));
                keyValues.Add(("Cargo / Temp Category", sch.CargoType));
                keyValues.Add(("Schedule Status", sch.Status));
                keyValues.Add(("Dispatched Vehicle Order", matchDel != null ? matchDel.DispatchNumber : "Not yet dispatched"));

                historyTitle = "Linked Dispatch & Haul Record";
                if (matchDel != null)
                {
                    historyData.Add(new {
                        Dispatch = matchDel.DispatchNumber,
                        Date = matchDel.DispatchDate.ToString("yyyy-MM-dd"),
                        Truck = matchDel.TruckInfo,
                        Driver = matchDel.DriverName,
                        Status = matchDel.Status
                    });
                }
            }
            else if (record is Delivery del)
            {
                title = $"📦 Delivery Dispatch: {del.DispatchNumber}";
                subtitle = $"Transit Order • Destination: {del.LoadingScheduleInfo}";
                statusBadge = del.Status;
                if (del.Status == "Completed") { badgeBg = Color.FromArgb(220, 252, 231); badgeFg = Color.FromArgb(22, 101, 52); }
                else if (del.Status == "In Transit") { badgeBg = Color.FromArgb(254, 243, 199); badgeFg = Color.FromArgb(180, 83, 9); }
                else { badgeBg = Color.FromArgb(241, 245, 249); badgeFg = Color.FromArgb(51, 65, 85); }

                var matchTrip = MJdatabase.GetTripReports().FirstOrDefault(t => t.DeliveryId == del.Id || (!string.IsNullOrEmpty(t.DispatchNumber) && t.DispatchNumber == del.DispatchNumber));
                var matchInv = MJdatabase.GetInvoices().FirstOrDefault(i => i.DeliveryId == del.Id);

                keyValues.Add(("Dispatch Number", del.DispatchNumber));
                keyValues.Add(("Dispatch Date", del.DispatchDate.ToString("MMMM dd, yyyy")));
                keyValues.Add(("Departure Time", del.DepartureTime));
                keyValues.Add(("Assigned Truck", del.TruckInfo));
                keyValues.Add(("Assigned Driver", del.DriverName));
                keyValues.Add(("Assigned Helpers / Aides", string.IsNullOrEmpty(del.AideNames) ? (string.IsNullOrEmpty(del.AideName) ? "None" : del.AideName) : del.AideNames));
                keyValues.Add(("G-Crates (Goods)", $"{del.GCrate:N0}"));
                keyValues.Add(("E-Crates (Empty)", $"{del.ECrate:N0}"));
                keyValues.Add(("Total Dispatched Crates", $"{del.TotalCrates:N0} Crates"));
                keyValues.Add(("Cargo / Temp Category", del.CargoType));
                keyValues.Add(("Current Dispatch Status", del.Status));
                keyValues.Add(("Linked Invoice", matchInv != null ? $"{matchInv.InvoiceNumber} ({matchInv.Status} - ₱{matchInv.NetAmount:N2})" : "Not yet billed"));
                keyValues.Add(("Trip Completion Sign-Off", matchTrip != null ? $"Delivered by {matchTrip.StoreManagerName} on {matchTrip.ReportDate:yyyy-MM-dd}" : "Pending Trip Report"));

                historyTitle = "Linked Downstream Documents";
                if (matchTrip != null)
                {
                    historyData.Add(new {
                        Type = "Trip Report",
                        Reference = matchTrip.TripReportNumber,
                        Delivered = $"{matchTrip.DeliveredCrates} Crates",
                        Returned = $"{matchTrip.ReturnedCrates} Crates",
                        SignOff = matchTrip.StoreManagerName
                    });
                }
                if (matchInv != null)
                {
                    historyData.Add(new {
                        Type = "Billing Invoice",
                        Reference = matchInv.InvoiceNumber,
                        Delivered = matchInv.ClientName,
                        Returned = $"₱{matchInv.NetAmount:N2}",
                        SignOff = matchInv.Status
                    });
                }
            }
            else if (record is TripReport tr)
            {
                title = $"📝 Trip Report: {tr.TripReportNumber}";
                subtitle = $"Delivery Audit • Dispatch: {tr.DispatchNumber}";
                statusBadge = "Recorded";
                badgeBg = Color.FromArgb(220, 252, 231); badgeFg = Color.FromArgb(22, 101, 52);

                var matchDel = MJdatabase.GetDeliveries().FirstOrDefault(d => d.Id == tr.DeliveryId || d.DispatchNumber == tr.DispatchNumber);
                int totalDisp = matchDel != null ? matchDel.TotalCrates : (tr.DeliveredCrates);
                int variance = totalDisp - tr.DeliveredCrates;

                keyValues.Add(("Trip Report #", tr.TripReportNumber));
                keyValues.Add(("Dispatch Reference #", tr.DispatchNumber));
                keyValues.Add(("Report / Audit Date", tr.ReportDate.ToString("MMMM dd, yyyy")));
                keyValues.Add(("Store Arrival Time", tr.ArrivalTime));
                keyValues.Add(("Store Departure Time", tr.DepartureTime));
                keyValues.Add(("Delivered G-Crates", $"{tr.DeliveredGCrates:N0}"));
                keyValues.Add(("Delivered E-Crates", $"{tr.DeliveredECrates:N0}"));
                keyValues.Add(("Total Delivered Crates", $"{tr.DeliveredCrates:N0}"));
                keyValues.Add(("Returned G-Crates", $"{tr.ReturnedGCrates:N0}"));
                keyValues.Add(("Returned E-Crates", $"{tr.ReturnedECrates:N0}"));
                keyValues.Add(("Total Returned Crates", $"{tr.ReturnedCrates:N0}"));
                keyValues.Add(("Crate Discrepancy / Variance", variance == 0 ? "0 Crates (Exact Match ✓)" : $"⚠️ {variance} Crates Difference"));
                keyValues.Add(("Store Manager Sign-Off", tr.StoreManagerName));
                keyValues.Add(("Trip Condition Remarks", string.IsNullOrEmpty(tr.Remarks) ? "None" : tr.Remarks));

                historyTitle = "Delivery Assignment Details";
                if (matchDel != null)
                {
                    historyData.Add(new {
                        Dispatch = matchDel.DispatchNumber,
                        Date = matchDel.DispatchDate.ToString("yyyy-MM-dd"),
                        Truck = matchDel.TruckInfo,
                        Driver = matchDel.DriverName,
                        Status = matchDel.Status
                    });
                }
            }
            else if (record is Invoice inv)
            {
                title = $"💰 Billing Invoice: {inv.InvoiceNumber}";
                subtitle = $"Client Freight Statement • {inv.ClientName}";
                statusBadge = inv.Status;
                if (inv.Status == "Paid") { badgeBg = Color.FromArgb(220, 252, 231); badgeFg = Color.FromArgb(22, 101, 52); }
                else if (inv.Status == "Partial") { badgeBg = Color.FromArgb(254, 243, 199); badgeFg = Color.FromArgb(180, 83, 9); }
                else { badgeBg = Color.FromArgb(254, 226, 226); badgeFg = Color.FromArgb(185, 28, 28); }

                var matchReceipts = MJdatabase.GetCollectionReceipts().Where(c => c.InvoiceId == inv.Id).ToList();
                decimal totalPaid = matchReceipts.Sum(c => c.AmountPaid);
                decimal balance = Math.Max(0, inv.NetAmount - totalPaid);

                keyValues.Add(("Invoice Number", inv.InvoiceNumber));
                if (!string.IsNullOrEmpty(inv.LoadingScheduleNumber))
                {
                    keyValues.Add(("Loading Schedule #", inv.LoadingScheduleNumber));
                }
                if (!string.IsNullOrEmpty(inv.DispatchNumber))
                {
                    keyValues.Add(("Delivery Dispatch #", inv.DispatchNumber));
                }
                keyValues.Add(("Client / Account Name", inv.ClientName));
                keyValues.Add(("Invoice Date", inv.IssueDate.ToString("MMMM dd, yyyy")));
                keyValues.Add(("Due Date", inv.DueDate.ToString("MMMM dd, yyyy")));
                keyValues.Add(("Gross Freight Tariff", $"₱{inv.TotalAmount:N2}"));
                keyValues.Add(("Damage Deductions", $"-₱{inv.Deductions:N2}"));
                keyValues.Add(("Net Total Amount Due", $"₱{inv.NetAmount:N2}"));
                keyValues.Add(("Total Amount Paid", $"₱{totalPaid:N2}"));
                keyValues.Add(("Outstanding Balance", $"₱{balance:N2}"));
                keyValues.Add(("Payment Settlement Status", inv.Status));

                historyTitle = $"Payment Receipts Log ({matchReceipts.Count} Collections)";
                foreach (var cr in matchReceipts.OrderByDescending(x => x.PaymentDate))
                {
                    historyData.Add(new {
                        Receipt = cr.ReceiptNumber,
                        Date = cr.PaymentDate.ToString("yyyy-MM-dd"),
                        Mode = cr.PaymentMethod,
                        RefNo = string.IsNullOrEmpty(cr.ReferenceNumber) ? "N/A" : cr.ReferenceNumber,
                        Amount = $"₱{cr.AmountPaid:N2}"
                    });
                }
            }
            else if (record is CollectionReceipt cr)
            {
                title = $"🧾 Collection Receipt: {cr.ReceiptNumber}";
                subtitle = $"Official Payment Receipt • Invoice: {cr.InvoiceNumber}";
                statusBadge = "Collected ✓";
                badgeBg = Color.FromArgb(220, 252, 231); badgeFg = Color.FromArgb(22, 101, 52);

                var matchInv = MJdatabase.GetInvoices().FirstOrDefault(i => i.Id == cr.InvoiceId || i.InvoiceNumber == cr.InvoiceNumber);

                keyValues.Add(("Receipt Number (OR)", cr.ReceiptNumber));
                keyValues.Add(("Payment Settlement Date", cr.PaymentDate.ToString("MMMM dd, yyyy")));
                keyValues.Add(("Payment Mode", cr.PaymentMethod));
                keyValues.Add(("Check / Ref / Trans #", string.IsNullOrEmpty(cr.ReferenceNumber) ? "Cash / Standard" : cr.ReferenceNumber));
                keyValues.Add(("Amount Collected", $"₱{cr.AmountPaid:N2}"));
                keyValues.Add(("Settled Invoice #", matchInv != null ? matchInv.InvoiceNumber : cr.InvoiceNumber));
                keyValues.Add(("Client / Account Name", matchInv != null ? matchInv.ClientName : "Client"));
                keyValues.Add(("Invoice Total Amount", matchInv != null ? $"₱{matchInv.NetAmount:N2}" : "N/A"));
                keyValues.Add(("Cashier / Collector Notes", string.IsNullOrEmpty(cr.Notes) ? "None" : cr.Notes));

                historyTitle = "Applied Invoice Summary";
                if (matchInv != null)
                {
                    historyData.Add(new {
                        Invoice = matchInv.InvoiceNumber,
                        Date = matchInv.IssueDate.ToString("yyyy-MM-dd"),
                        Client = matchInv.ClientName,
                        Total = $"₱{matchInv.NetAmount:N2}",
                        Status = matchInv.Status
                    });
                }
            }
            else if (record is IncidentReport inc)
            {
                title = $"⚠️ Incident Report: #{inc.Id}";
                subtitle = $"Transit Discrepancy & Claims • Dispatch: {inc.DispatchNumber}";
                statusBadge = inc.Status;
                if (inc.Status == "Resolved") { badgeBg = Color.FromArgb(220, 252, 231); badgeFg = Color.FromArgb(22, 101, 52); }
                else { badgeBg = Color.FromArgb(254, 226, 226); badgeFg = Color.FromArgb(185, 28, 28); }

                var matchDel = MJdatabase.GetDeliveries().FirstOrDefault(d => d.Id == inc.DeliveryId || d.DispatchNumber == inc.DispatchNumber);

                keyValues.Add(("Incident ID", $"INC-{inc.Id:D5}"));
                keyValues.Add(("Dispatch Number", inc.DispatchNumber));
                keyValues.Add(("Incident Date", inc.IncidentDate.ToString("MMMM dd, yyyy")));
                keyValues.Add(("Damaged Crates Quantity", $"{inc.DamagedQuantity:N0} Crates"));
                keyValues.Add(("Estimated Damage Cost", $"₱{inc.EstimatedCost:N2}"));
                keyValues.Add(("Root Cause / Category", inc.Cause));
                keyValues.Add(("Chargeable to Crew", inc.ChargeableToCrew ? "Yes (Deducted from driver/aide)" : "No (Company Liability / Client Damage)"));
                keyValues.Add(("Investigation Status", inc.Status));
                keyValues.Add(("Photo Evidence Attached", !string.IsNullOrEmpty(inc.PhotoPath) ? "Attached ✓" : "None"));
                keyValues.Add(("Incident Narrative", inc.Description));

                historyTitle = "Dispatched Haul Context";
                if (matchDel != null)
                {
                    historyData.Add(new {
                        Dispatch = matchDel.DispatchNumber,
                        Date = matchDel.DispatchDate.ToString("yyyy-MM-dd"),
                        Truck = matchDel.TruckInfo,
                        Driver = matchDel.DriverName,
                        Status = matchDel.Status
                    });
                }
            }

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(20, 14, 20, 14)
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(18, 12),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(20, 42),
                AutoSize = true
            };

            Label lblBadge = new Label
            {
                Text = statusBadge.ToUpperInvariant(),
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                BackColor = badgeBg,
                ForeColor = badgeFg,
                Padding = new Padding(10, 4, 10, 4),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(dlg.ClientSize.Width - 140, 24)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(lblBadge);

            TabControl tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Padding = new Point(16, 8)
            };

            TabPage tabOverview = new TabPage("📋 Full Record Specifications")
            {
                BackColor = Color.White,
                Padding = new Padding(12)
            };

            DataGridView gridProps = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9.5F),
                RowTemplate = { Height = 32 }
            };
            gridProps.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Field Specification / Parameter", FillWeight = 38 });
            gridProps.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Recorded Data Value", FillWeight = 62 });
            gridProps.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            gridProps.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            gridProps.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            gridProps.EnableHeadersVisualStyles = false;
            gridProps.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            foreach (var kv in keyValues)
            {
                gridProps.Rows.Add(kv.Label, kv.Value);
            }
            tabOverview.Controls.Add(gridProps);
            tabs.TabPages.Add(tabOverview);

            TabPage tabHistory = new TabPage($"📊 {historyTitle}")
            {
                BackColor = Color.White,
                Padding = new Padding(12)
            };

            DataGridView gridHist = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9F),
                RowTemplate = { Height = 28 }
            };
            gridHist.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            gridHist.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            gridHist.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            gridHist.EnableHeadersVisualStyles = false;
            gridHist.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            if (historyData.Count > 0)
            {
                gridHist.DataSource = historyData;
            }
            else
            {
                Label lblNoHist = new Label
                {
                    Text = "No previous activity or linked records found for this entry.",
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                tabHistory.Controls.Add(lblNoHist);
            }
            if (historyData.Count > 0) tabHistory.Controls.Add(gridHist);
            tabs.TabPages.Add(tabHistory);

            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(16, 10, 16, 10)
            };

            Button btnCopy = new Button
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
            };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += (s, e) =>
            {
                PrintDocument pd = new PrintDocument();
                pd.DefaultPageSettings.PaperSize = new PaperSize("ShortBond", 850, 1100);
                pd.PrintPage += (ps, pe) =>
                {
                    Graphics g = pe.Graphics!;
                    float y = 40;
                    g.DrawString("M.J. BILLONES TRANSPORT SERVICE", new Font("Segoe UI", 14F, FontStyle.Bold), Brushes.Black, 50, y);
                    y += 24;
                    g.DrawString("Official Record Specification & Audit Summary", new Font("Segoe UI", 10F, FontStyle.Italic), Brushes.DarkSlateGray, 50, y);
                    y += 20;
                    g.DrawLine(new Pen(Color.FromArgb(15, 23, 42), 2), 50, y, 780, y);
                    y += 18;

                    g.DrawString(title, new Font("Segoe UI", 12F, FontStyle.Bold), Brushes.Black, 50, y);
                    y += 26;

                    Font lblFont = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
                    Font valFont = new Font("Segoe UI", 9.5F, FontStyle.Regular);

                    foreach (var kv in keyValues)
                    {
                        g.DrawString(kv.Label + ":", lblFont, Brushes.DimGray, 50, y);
                        g.DrawString(kv.Value, valFont, Brushes.Black, 280, y);
                        y += 22;
                    }

                    y += 20;
                    g.DrawLine(Pens.LightGray, 50, y, 780, y);
                    y += 10;
                    g.DrawString($"Printed on {DateTime.Now:MMMM dd, yyyy hh:mm tt} • Confidential System Record", new Font("Segoe UI", 8F), Brushes.Gray, 50, y);
                };

                PrintPreviewDialog ppd = new PrintPreviewDialog { Document = pd, Width = 800, Height = 600 };
                ppd.ShowDialog(dlg);
            };

            Button btnClose = new Button
            {
                Text = "✕ Close",
                Height = 36,
                Width = 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(dlg.ClientSize.Width - 116, 10),
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => dlg.Close();

            pnlFooter.Controls.Add(btnCopy);
            pnlFooter.Controls.Add(btnPrint);
            pnlFooter.Controls.Add(btnClose);

            dlg.Controls.Add(tabs);
            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlHeader);

            dlg.ShowDialog(parentForm);
        }
    }
}
