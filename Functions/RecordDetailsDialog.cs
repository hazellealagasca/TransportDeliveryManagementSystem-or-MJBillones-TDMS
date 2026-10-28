using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    public static class RecordDetailsDialog
    {
        private static Form? _activeInspector = null;

        public static void ShowDetails(Control parent, object record)
        {
            if (record == null) return;

            if (record is Delivery deliveryRecord)
            {
                Forms.DispatchDetailsDialog.ShowFor(parent, deliveryRecord);
                return;
            }
            if (_activeInspector != null && !_activeInspector.IsDisposed)
            {
                _activeInspector.Close();
                _activeInspector = null;
            }

            Form? parentForm = parent.FindForm();

            Form dlg = new Form
            {
                Text = "🔍 System Record Details & Activity Inspector",
                Size = new Size(880, 640),
                FormBorderStyle = FormBorderStyle.Sizable,
                MinimumSize = new Size(760, 520),
                BackColor = Pal.C(248, 250, 252),
                MaximizeBox = true,
                MinimizeBox = true,
                ShowInTaskbar = true
            };

            if (parentForm?.Icon != null)
            {
                dlg.Icon = parentForm.Icon;
                dlg.ShowIcon = true;
            }
            else
            {
                try
                {
                    dlg.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    dlg.ShowIcon = true;
                }
                catch
                {
                    dlg.ShowIcon = false;
                }
            }

            if (parentForm != null && !parentForm.IsDisposed)
            {
                dlg.StartPosition = FormStartPosition.Manual;
                int x = parentForm.Location.X + (parentForm.Width - dlg.Width) / 2;
                int y = parentForm.Location.Y + (parentForm.Height - dlg.Height) / 2;
                Rectangle screenArea = Screen.FromControl(parentForm).WorkingArea;
                x = Math.Max(screenArea.X, Math.Min(x, screenArea.Right - dlg.Width));
                y = Math.Max(screenArea.Y, Math.Min(y, screenArea.Bottom - dlg.Height));
                dlg.Location = new Point(x, y);

                parentForm.SizeChanged += (s, e) =>
                {
                    if (dlg.IsDisposed) return;
                    if (parentForm.WindowState == FormWindowState.Minimized && dlg.WindowState != FormWindowState.Minimized)
                    {
                        dlg.WindowState = FormWindowState.Minimized;
                    }
                };

                parentForm.FormClosed += (s, e) =>
                {
                    if (!dlg.IsDisposed) dlg.Close();
                };
            }
            else
            {
                dlg.StartPosition = FormStartPosition.CenterScreen;
            }

            dlg.Resize += (s, e) =>
            {
                if (dlg.WindowState == FormWindowState.Minimized)
                {
                    if (parentForm != null && !parentForm.IsDisposed)
                    {
                        if (parentForm.WindowState == FormWindowState.Minimized)
                        {
                            parentForm.WindowState = FormWindowState.Normal;
                        }
                        parentForm.Activate();
                    }
                }
            };

            string title = "Record Details";
            string subtitle = "M.J. Billones Transport Service Management System";
            string statusBadge = "Active";
            Color badgeBg = Pal.C(220, 252, 231);
            Color badgeFg = Pal.C(22, 101, 52);
            var keyValues = new List<(string Label, string Value)>();
            var historyData = new List<object>();
            string historyTitle = "Related Operational Activity";

            if (record is Truck truck)
            {
                title = $"🚚 Vehicle Profile: {truck.PlateNumber}";
                subtitle = $"Vehicle • Category: {truck.TruckType}";
                statusBadge = truck.Status;
                if (truck.Status == "Available" || truck.Status == "Active") { badgeBg = Pal.C(220, 252, 231); badgeFg = Pal.C(22, 101, 52); }
                else if (truck.Status == "In Use" || truck.Status == "On Trip") { badgeBg = Pal.C(254, 243, 199); badgeFg = Pal.C(180, 83, 9); }
                else { badgeBg = Pal.C(254, 226, 226); badgeFg = Pal.C(185, 28, 28); }
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
                if (crew.IsAvailable) { badgeBg = Pal.C(220, 252, 231); badgeFg = Pal.C(22, 101, 52); }
                else { badgeBg = Pal.C(254, 243, 199); badgeFg = Pal.C(180, 83, 9); }
                var allDel = MJdatabase.GetDeliveries().Where(d => d.DriverId == crew.Id || (!string.IsNullOrEmpty(d.AideIds) && d.AideIds.Split(',').Contains(crew.Id.ToString())) || (!string.IsNullOrEmpty(d.DriverName) && d.DriverName.Contains(crew.FullName))).ToList();

                keyValues.Add(("Full Name", crew.FullName));
                keyValues.Add(("Designated Role", crew.Role));
                keyValues.Add(("Contact Mobile #", crew.ContactNumber));
                keyValues.Add(("Driver's License", crew.HasLicense ? crew.LicenseDisplay : "None"));
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
                if (sch.Status == "Dispatched" || sch.Status == "Completed") { badgeBg = Pal.C(220, 252, 231); badgeFg = Pal.C(22, 101, 52); }
                else if (sch.Status == "Cancelled" || sch.Status == "Past Due") { badgeBg = Pal.C(254, 226, 226); badgeFg = Pal.C(185, 28, 28); }
                else if (sch.Status == "Rescheduled") { badgeBg = Pal.C(238, 242, 255); badgeFg = Pal.C(67, 56, 202); }
                else { badgeBg = Pal.C(254, 243, 199); badgeFg = Pal.C(180, 83, 9); }
                var matchDel = MJdatabase.GetDeliveries().FirstOrDefault(d => d.LoadingScheduleId == sch.Id || (!string.IsNullOrEmpty(d.LoadingScheduleInfo) && d.LoadingScheduleInfo.Contains(sch.ScheduleNumber)));
                var cl = MJdatabase.GetClientByCompanyName(sch.ClientName);

                keyValues.Add(("Client / Principal", sch.ClientName));
                if (cl != null && (!string.IsNullOrEmpty(cl.ContactPerson) || !string.IsNullOrEmpty(cl.ContactNumber)))
                {
                    keyValues.Add(("Client Contact Person", $"{cl.ContactPerson} {cl.ContactNumber}".Trim()));
                    if (!string.IsNullOrEmpty(cl.Address)) keyValues.Add(("Client Head Office Address", cl.Address));
                }
                keyValues.Add(("Delivery Destination", sch.Destination));
                keyValues.Add(("Schedule Reference Number", sch.ScheduleNumber));
                keyValues.Add(("Scheduled Date", sch.ScheduleDate.ToString("MMMM dd, yyyy")));
                keyValues.Add(("Target Departure Time", sch.DepartureTime));
                keyValues.Add(("G-Crates (Goods)", $"{sch.GCrate:N0} Crates"));
                keyValues.Add(("E-Crates (Empty)", $"{sch.ECrate:N0} Crates"));
                keyValues.Add(("Total Crates Volume", $"{sch.TotalCrates:N0} Crates"));
                keyValues.Add(("Season Rate Tier", sch.IsPeak ? "🔥 Peak Season Rate" : "Regular Season Rate"));
                keyValues.Add(("Cargo / Temperature Zone", sch.CargoType));
                keyValues.Add(("Schedule Status", sch.Status));
                keyValues.Add(("Operational Remarks", string.IsNullOrEmpty(sch.Remarks) ? "None" : sch.Remarks));
                if (!string.IsNullOrEmpty(sch.CancellationReason))
                {
                    keyValues.Add(("Cancellation Reason", sch.CancellationReason));
                }
                if (sch.RescheduleCount > 0)
                {
                    keyValues.Add(("Reschedule Count", $"{sch.RescheduleCount} time(s)"));
                    if (sch.OriginalScheduleDate.HasValue)
                    {
                        keyValues.Add(("Original Scheduled Date", sch.OriginalScheduleDate.Value.ToString("MMMM dd, yyyy")));
                    }
                    if (!string.IsNullOrWhiteSpace(sch.RescheduleReason))
                    {
                        keyValues.Add(("Reschedule Reason", sch.RescheduleReason));
                    }
                }
                keyValues.Add(("Dispatched Vehicle Order", matchDel != null ? matchDel.DispatchNumber : "Not yet dispatched"));
                if (matchDel != null)
                {
                    keyValues.Add(("Assigned Truck", matchDel.TruckInfo));
                    keyValues.Add(("Assigned Driver", matchDel.DriverName));
                    keyValues.Add(("Assigned Helpers / Aides", string.IsNullOrEmpty(matchDel.AideNames) ? (string.IsNullOrEmpty(matchDel.AideName) ? "None" : matchDel.AideName) : matchDel.AideNames));
                }

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
                var matchSched = del.LoadingScheduleId > 0
                    ? MJdatabase.GetLoadingSchedules().FirstOrDefault(s => s.Id == del.LoadingScheduleId)
                    : null;
                string clientName = !string.IsNullOrWhiteSpace(del.ClientName) ? del.ClientName : (matchSched?.ClientName ?? "");
                if (string.IsNullOrEmpty(clientName) && del.Stops != null && del.Stops.Count > 0)
                {
                    clientName = string.Join(", ", del.Stops.Select(s => s.ClientName).Where(c => !string.IsNullOrEmpty(c)).Distinct());
                }
                if (string.IsNullOrEmpty(clientName)) clientName = "General / Direct Client";
                var cl = MJdatabase.GetClientByCompanyName(clientName);

                title = $"📦 Delivery Dispatch: {del.DispatchNumber}";
                subtitle = $"Transit Order • Client: {clientName}";
                statusBadge = del.Status;
                if (del.Status == "Completed") { badgeBg = Pal.C(220, 252, 231); badgeFg = Pal.C(22, 101, 52); }
                else if (del.Status == "Delayed") { badgeBg = Pal.C(254, 226, 226); badgeFg = Pal.C(185, 28, 28); }
                else if (del.Status == "In Transit") { badgeBg = Pal.C(254, 243, 199); badgeFg = Pal.C(180, 83, 9); }
                else { badgeBg = Pal.C(241, 245, 249); badgeFg = Pal.C(51, 65, 85); }
                var matchTrip = MJdatabase.GetTripReports().FirstOrDefault(t => t.DeliveryId == del.Id || (!string.IsNullOrEmpty(t.DispatchNumber) && t.DispatchNumber == del.DispatchNumber));
                var matchInv = MJdatabase.GetInvoices().FirstOrDefault(i => i.DeliveryId == del.Id);

                keyValues.Add(("Client / Account Name", clientName));
                if (cl != null && (!string.IsNullOrEmpty(cl.ContactPerson) || !string.IsNullOrEmpty(cl.ContactNumber)))
                {
                    keyValues.Add(("Client Contact Person", $"{cl.ContactPerson} {cl.ContactNumber}".Trim()));
                }
                string destDisplay = !string.IsNullOrWhiteSpace(del.Destination) ? del.Destination : (string.IsNullOrEmpty(del.LoadingScheduleInfo) ? (matchSched?.Destination ?? "N/A") : del.LoadingScheduleInfo);
                keyValues.Add(("Delivery Destination", destDisplay));
                keyValues.Add(("Peak Season", del.IsPeak ? "⚡ Peak season surcharge applied" : "Regular season rate"));
                keyValues.Add(("Route Type", del.RouteTypeDisplay));
                keyValues.Add(("Order Reference #", del.DispatchNumber));
                if (matchSched != null || del.LoadingScheduleId > 0)
                {
                    keyValues.Add(("Legacy Schedule Reference", matchSched != null ? matchSched.ScheduleNumber : $"#{del.LoadingScheduleId}"));
                }
                keyValues.Add(("Dispatch Date", del.DispatchDate.ToString("MMMM dd, yyyy")));
                keyValues.Add(("Scheduled Departure Time", del.DepartureTime));
                keyValues.Add(("Assigned Truck", del.TruckInfo));
                keyValues.Add(("Assigned Driver", del.DriverName));
                keyValues.Add(("Assigned Helpers / Aides", string.IsNullOrEmpty(del.AideNames) ? (string.IsNullOrEmpty(del.AideName) ? "None" : del.AideName) : del.AideNames));
                keyValues.Add(("G-Crates (Goods)", $"{del.GCrate:N0}"));
                keyValues.Add(("E-Crates (Empty)", $"{del.ECrate:N0}"));
                keyValues.Add(("Total Dispatched Crates", $"{del.TotalCrates:N0} Crates"));
                keyValues.Add(("Cargo / Temp Category", del.CargoType));
                keyValues.Add(("Current Dispatch Status", del.Status));
                keyValues.Add(("Delivery Remarks", string.IsNullOrEmpty(del.Remarks) ? "None" : del.Remarks));
                if (!string.IsNullOrEmpty(del.CancellationReason))
                {
                    keyValues.Add(("Cancellation Reason", del.CancellationReason));
                }
                keyValues.Add(("Linked Invoice", matchInv != null ? $"{matchInv.InvoiceNumber} ({matchInv.Status} - ₱{matchInv.NetAmount:N2})" : "Not yet billed"));
                keyValues.Add(("Trip Completion Sign-Off", matchTrip != null ? $"Delivered by {matchTrip.StoreManagerName} on {matchTrip.ReportDate:yyyy-MM-dd}" : "Pending Trip Report"));

                historyTitle = "Linked Downstream Documents & Route Stops";
                if (del.Stops != null && del.Stops.Count > 0)
                {
                    foreach (var stop in del.Stops)
                    {
                        historyData.Add(new {
                            Type = $"Stop #{stop.StopSequence}",
                            Reference = stop.ClientName,
                            Delivered = stop.Destination,
                            Returned = $"{stop.TotalCrates} Crates",
                            SignOff = stop.Status
                        });
                    }
                }
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
                var matchDel = MJdatabase.GetDeliveries().FirstOrDefault(d => d.Id == tr.DeliveryId || d.DispatchNumber == tr.DispatchNumber);
                var matchSched = matchDel != null && matchDel.LoadingScheduleId > 0
                    ? MJdatabase.GetLoadingSchedules().FirstOrDefault(s => s.Id == matchDel.LoadingScheduleId)
                    : null;
                string clientName = matchSched?.ClientName ?? "";
                if (string.IsNullOrEmpty(clientName) && matchDel?.Stops != null && matchDel.Stops.Count > 0)
                {
                    clientName = string.Join(", ", matchDel.Stops.Select(s => s.ClientName).Where(c => !string.IsNullOrEmpty(c)).Distinct());
                }
                if (string.IsNullOrEmpty(clientName)) clientName = "M.J. Billones Client Partner";

                title = $"📝 Trip Report: {tr.TripReportNumber}";
                subtitle = $"Delivery Audit • Client: {clientName}";
                statusBadge = "Recorded";
                badgeBg = Pal.C(220, 252, 231); badgeFg = Pal.C(22, 101, 52);
                int totalDisp = matchDel != null ? matchDel.TotalCrates : (tr.DeliveredCrates);
                int variance = totalDisp - tr.DeliveredCrates;

                keyValues.Add(("Client / Account Name", clientName));
                keyValues.Add(("Delivery Destination", matchSched?.Destination ?? (matchDel?.LoadingScheduleInfo ?? "N/A")));
                keyValues.Add(("Trip Report #", tr.TripReportNumber));
                keyValues.Add(("Dispatch Reference #", tr.DispatchNumber));
                keyValues.Add(("Assigned Truck", matchDel?.TruckInfo ?? "N/A"));
                keyValues.Add(("Assigned Driver", matchDel?.DriverName ?? "N/A"));
                keyValues.Add(("Assigned Helpers / Aides", matchDel != null ? (string.IsNullOrEmpty(matchDel.AideNames) ? (string.IsNullOrEmpty(matchDel.AideName) ? "None" : matchDel.AideName) : matchDel.AideNames) : "None"));
                keyValues.Add(("Report / Audit Date", tr.ReportDate.ToString("MMMM dd, yyyy")));
                keyValues.Add(("Store Arrival Time", tr.ArrivalTime));
                keyValues.Add(("Store Departure Time", tr.DepartureTime));
                keyValues.Add(("Dispatched Volume", $"{totalDisp:N0} Crates"));
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
                subtitle = $"Client Delivery Statement • {inv.ClientName}";
                statusBadge = inv.Status;
                if (inv.Status == "Paid") { badgeBg = Pal.C(220, 252, 231); badgeFg = Pal.C(22, 101, 52); }
                else if (inv.Status == "Partial") { badgeBg = Pal.C(254, 243, 199); badgeFg = Pal.C(180, 83, 9); }
                else if (inv.Status == "Overdue") { badgeBg = Pal.C(254, 226, 226); badgeFg = Pal.C(185, 28, 28); }
                else { badgeBg = Pal.C(241, 245, 249); badgeFg = Pal.C(51, 65, 85); }
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
                keyValues.Add(("Gross Delivery Rate", $"₱{inv.TotalAmount:N2}"));
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
                badgeBg = Pal.C(220, 252, 231); badgeFg = Pal.C(22, 101, 52);
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
                var matchDel = MJdatabase.GetDeliveries().FirstOrDefault(d => d.Id == inc.DeliveryId || d.DispatchNumber == inc.DispatchNumber);
                var matchSched = matchDel != null && matchDel.LoadingScheduleId > 0
                    ? MJdatabase.GetLoadingSchedules().FirstOrDefault(s => s.Id == matchDel.LoadingScheduleId)
                    : null;
                string clientName = matchSched?.ClientName ?? "";
                if (string.IsNullOrEmpty(clientName) && matchDel?.Stops != null && matchDel.Stops.Count > 0)
                {
                    clientName = string.Join(", ", matchDel.Stops.Select(s => s.ClientName).Where(c => !string.IsNullOrEmpty(c)).Distinct());
                }
                if (string.IsNullOrEmpty(clientName)) clientName = "M.J. Billones Client Partner";

                title = $"⚠️ Incident Report: #{inc.Id}";
                subtitle = $"Transit Discrepancy & Claims • Client: {clientName}";
                statusBadge = inc.Status;
                if (inc.Status == "Resolved") { badgeBg = Pal.C(220, 252, 231); badgeFg = Pal.C(22, 101, 52); }
                else { badgeBg = Pal.C(254, 226, 226); badgeFg = Pal.C(185, 28, 28); }
                keyValues.Add(("Client / Account Name", clientName));
                keyValues.Add(("Delivery Destination", matchSched?.Destination ?? (matchDel?.LoadingScheduleInfo ?? "N/A")));
                keyValues.Add(("Incident ID", $"INC-{inc.Id:D5}"));
                keyValues.Add(("Dispatch Number", inc.DispatchNumber));
                keyValues.Add(("Assigned Truck", matchDel?.TruckInfo ?? "N/A"));
                keyValues.Add(("Assigned Driver", matchDel?.DriverName ?? "N/A"));
                keyValues.Add(("Assigned Helpers / Aides", matchDel != null ? (string.IsNullOrEmpty(matchDel.AideNames) ? (string.IsNullOrEmpty(matchDel.AideName) ? "None" : matchDel.AideName) : matchDel.AideNames) : "None"));
                keyValues.Add(("Incident Date", inc.IncidentDate.ToString("MMMM dd, yyyy")));
                keyValues.Add(("Damaged Crates Quantity", $"{inc.DamagedQuantity:N0} Crates"));
                keyValues.Add(("Estimated Damage Cost", $"₱{inc.EstimatedCost:N2}"));
                keyValues.Add(("Root Cause / Category", inc.Cause));
                keyValues.Add(("Chargeable to Crew", inc.ChargeableToCrew ? "Yes (Deducted from driver/aide)" : "No (Company Liability / Client Damage)"));
                keyValues.Add(("Investigation Status", inc.Status));
                keyValues.Add(("Photo Evidence Attached", !string.IsNullOrEmpty(inc.PhotoPath) ? $"Attached ✓ ({System.IO.Path.GetFileName(inc.PhotoPath)})" : "None"));
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
            else if (record is AuditTrailEntry log)
            {
                title = $"📜 Audit Log: {log.ActionType} on {log.TableName}";
                subtitle = $"Operational History Entry • Reference: {log.RecordIdentifier}";
                statusBadge = log.ActionType;
                if (log.ActionType.Equals("ADD", StringComparison.OrdinalIgnoreCase)) { badgeBg = Pal.C(220, 252, 231); badgeFg = Pal.C(22, 101, 52); }
                else if (log.ActionType.Equals("DELETE", StringComparison.OrdinalIgnoreCase) || log.ActionType.Equals("CANCEL", StringComparison.OrdinalIgnoreCase)) { badgeBg = Pal.C(254, 226, 226); badgeFg = Pal.C(185, 28, 28); }
                else if (log.ActionType.Equals("RESCHEDULE", StringComparison.OrdinalIgnoreCase)) { badgeBg = Pal.C(254, 243, 199); badgeFg = Pal.C(180, 83, 9); }
                else { badgeBg = Pal.C(224, 231, 255); badgeFg = Pal.C(49, 46, 129); }
                keyValues.Add(("Audit Log ID", $"#LOG-{log.Id:D6}"));
                keyValues.Add(("Logged Timestamp", log.Timestamp.ToString("MMMM dd, yyyy HH:mm:ss")));
                keyValues.Add(("Operator Username", log.Username));
                keyValues.Add(("Operator Role", log.UserRole));
                keyValues.Add(("Action Executed", log.ActionType));
                keyValues.Add(("Target Module / Table", log.TableName));
                keyValues.Add(("Target Reference #", log.RecordIdentifier));
                keyValues.Add(("Target Record ID", log.RecordId.ToString()));
                keyValues.Add(("Field Name Modified", string.IsNullOrEmpty(log.FieldName) ? "Entire Record" : log.FieldName));
                keyValues.Add(("Previous Recorded Value", string.IsNullOrEmpty(log.OldValue) ? "N/A" : log.OldValue));
                keyValues.Add(("New / Updated Value", string.IsNullOrEmpty(log.NewValue) ? "N/A" : log.NewValue));
                keyValues.Add(("Activity Notes / Reason", string.IsNullOrEmpty(log.ReasonOrNotes) ? "None" : log.ReasonOrNotes));
                keyValues.Add(("Workstation / Host Machine", string.IsNullOrEmpty(log.IpOrMachine) ? "Local Station" : log.IpOrMachine));

                historyTitle = $"Related Audit Trail for {log.RecordIdentifier}";
                var relatedLogs = MJdatabase.GetAuditTrail(new[] { log.TableName }, null, 50).Where(a => a.RecordIdentifier == log.RecordIdentifier && a.Id != log.Id).ToList();
                foreach (var rl in relatedLogs)
                {
                    historyData.Add(new {
                        Timestamp = rl.Timestamp.ToString("yyyy-MM-dd HH:mm"),
                        User = rl.Username,
                        Action = rl.ActionType,
                        Field = rl.FieldName,
                        Details = rl.ReasonOrNotes
                    });
                }
            }
            else if (record is RateMatrix rm)
            {
                title = $"🏷️ Rate: {rm.RouteName}";
                subtitle = $"Delivery Rate Matrix • Destination: {rm.Destination}";
                statusBadge = rm.IsPeakSeason ? "Peak Season" : "Regular";
                badgeBg = rm.IsPeakSeason ? Pal.C(254, 243, 199) : Pal.C(220, 252, 231);
                badgeFg = rm.IsPeakSeason ? Pal.C(180, 83, 9) : Pal.C(22, 101, 52);
                keyValues.Add(("Route Name / Code", rm.RouteName));
                keyValues.Add(("Destination Drop", rm.Destination));
                keyValues.Add(("Truck Category / Type", rm.TruckType));
                keyValues.Add(("Season Rate Tier", rm.IsPeakSeason ? "🔥 Peak Season Rate" : "Regular Season Rate"));
                keyValues.Add(("Rate Amount", $"₱{rm.RateAmount:N2}"));
                historyTitle = "Rate Details";
            }
            else if (record is User usr)
            {
                title = $"👤 User Account: {usr.Username}";
                subtitle = $"System User Profile • {usr.FullName}";
                statusBadge = usr.IsActive ? "Active" : "Inactive";
                badgeBg = usr.IsActive ? Pal.C(220, 252, 231) : Pal.C(254, 226, 226);
                badgeFg = usr.IsActive ? Pal.C(22, 101, 52) : Pal.C(185, 28, 28);
                keyValues.Add(("Username", usr.Username));
                keyValues.Add(("Full Name", usr.FullName));
                keyValues.Add(("Designated System Role", usr.Role));
                keyValues.Add(("Contact Number", string.IsNullOrEmpty(usr.ContactNumber) ? "N/A" : usr.ContactNumber));
                keyValues.Add(("Account Status", usr.IsActive ? "Active" : "Inactive"));
                keyValues.Add(("Account Created On", usr.CreatedAt.ToString("MMMM dd, yyyy HH:mm")));

                historyTitle = $"Audit Activity for User {usr.Username}";
                var userLogs = MJdatabase.GetAuditTrail(null, null, 30).Where(a => a.Username.Equals(usr.Username, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var ul in userLogs)
                {
                    historyData.Add(new {
                        Timestamp = ul.Timestamp.ToString("yyyy-MM-dd HH:mm"),
                        Action = ul.ActionType,
                        Module = ul.TableName,
                        RefNo = ul.RecordIdentifier,
                        Details = ul.ReasonOrNotes
                    });
                }
            }
            else if (record is ClientCompany cc)
            {
                title = $"🏢 Client Profile: {cc.CompanyName}";
                subtitle = $"Registered Client Company • Contact: {cc.ContactPerson}";
                statusBadge = "Registered";
                badgeBg = Pal.C(220, 252, 231);
                badgeFg = Pal.C(22, 101, 52);
                keyValues.Add(("Company Name", cc.CompanyName));
                keyValues.Add(("Primary Contact Person", string.IsNullOrEmpty(cc.ContactPerson) ? "N/A" : cc.ContactPerson));
                keyValues.Add(("Contact Number / Mobile", string.IsNullOrEmpty(cc.ContactNumber) ? "N/A" : cc.ContactNumber));
                keyValues.Add(("Office / Delivery Address", string.IsNullOrEmpty(cc.Address) ? "N/A" : cc.Address));
                keyValues.Add(("Email Address", string.IsNullOrEmpty(cc.Email) ? "N/A" : cc.Email));

                var clientSchedules = MJdatabase.GetLoadingSchedules().Where(s => s.ClientName.Equals(cc.CompanyName, StringComparison.OrdinalIgnoreCase)).ToList();
                int totalCrates = clientSchedules.Sum(s => s.TotalCrates);
                keyValues.Add(("Total Loading Orders", $"{clientSchedules.Count} Orders"));
                keyValues.Add(("Total Crate Volume", $"{totalCrates:N0} Crates"));

                historyTitle = $"Recent Loading Orders for {cc.CompanyName}";
                foreach (var clientSched in clientSchedules.Take(10))
                {
                    historyData.Add(new {
                        Order = clientSched.ScheduleNumber,
                        Date = clientSched.ScheduleDate.ToString("yyyy-MM-dd"),
                        Destination = clientSched.Destination,
                        Crates = $"{clientSched.TotalCrates} Crates",
                        Status = clientSched.Status
                    });
                }
            }

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Width = dlg.ClientSize.Width,
                Height = 80,
                BackColor = Pal.C(15, 23, 42),
                Padding = new Padding(20, 14, 20, 14)
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Pal.White,
                Location = new Point(18, 12),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Pal.C(148, 163, 184),
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
                Location = new Point(dlg.ClientSize.Width - 140, 24)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(lblBadge);
            pnlHeader.Resize += (s, e) =>
            {
                lblBadge.Location = new Point(pnlHeader.ClientSize.Width - lblBadge.Width - 20, 24);
            };

            TabControl tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Padding = new Point(16, 8)
            };

            TabPage tabOverview = new TabPage("📋 Full Record Specifications")
            {
                BackColor = Pal.White,
                Padding = new Padding(12)
            };

            DataGridView gridProps = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Pal.White,
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
            gridProps.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(241, 245, 249);
            gridProps.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(51, 65, 85);
            gridProps.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            gridProps.EnableHeadersVisualStyles = false;
            gridProps.AlternatingRowsDefaultCellStyle.BackColor = Pal.C(248, 250, 252);
            foreach (var kv in keyValues)
            {
                gridProps.Rows.Add(kv.Label, kv.Value);
            }
            tabOverview.Controls.Add(gridProps);
            tabs.TabPages.Add(tabOverview);

            TabPage tabHistory = new TabPage($"📊 {historyTitle}")
            {
                BackColor = Pal.White,
                Padding = new Padding(12)
            };

            DataGridView gridHist = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Pal.White,
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
            gridHist.ColumnHeadersDefaultCellStyle.BackColor = Pal.C(241, 245, 249);
            gridHist.ColumnHeadersDefaultCellStyle.ForeColor = Pal.C(51, 65, 85);
            gridHist.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            gridHist.EnableHeadersVisualStyles = false;
            gridHist.AlternatingRowsDefaultCellStyle.BackColor = Pal.C(248, 250, 252);
            if (historyData.Count > 0)
            {
                gridHist.DataSource = historyData;
            }
            else
            {
                Label lblNoHist = new Label
                {
                    Text = "No previous activity or linked records found for this entry.",
                    ForeColor = Pal.C(148, 163, 184),
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
                Width = dlg.ClientSize.Width,
                Height = 56,
                BackColor = Pal.C(241, 245, 249),
                Padding = new Padding(16, 10, 16, 10)
            };

            Button btnPrint = new Button
            {
                Text = "🖨️ Print Summary",
                Height = 36,
                Width = 140,
                Location = new Point(16, 10),
                BackColor = Pal.C(30, 41, 59),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            if (record is Delivery targetDel)
            {
                Button btnUpdate = new Button
                {
                    Text = "✏️ Update Order",
                    Height = 36,
                    Width = 145,
                    Location = new Point(16, 10),
                    BackColor = Pal.C(30, 41, 59),
                    ForeColor = Pal.White,
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
                    BackColor = Pal.C(220, 38, 38),
                    ForeColor = Pal.White,
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
            }
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
                    g.DrawLine(new Pen(Pal.C(15, 23, 42), 2), 50, y, 780, y);
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
                Location = new Point(dlg.ClientSize.Width - 116, 10),
                BackColor = Pal.C(226, 232, 240),
                ForeColor = Pal.C(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => dlg.Close();

            pnlFooter.Controls.Add(btnPrint);
            pnlFooter.Controls.Add(btnClose);
            pnlFooter.Resize += (s, e) =>
            {
                btnClose.Location = new Point(pnlFooter.ClientSize.Width - btnClose.Width - 16, 10);
            };

            dlg.Controls.Add(tabs);
            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlHeader);

            _activeInspector = dlg;
            dlg.FormClosed += (s, e) =>
            {
                if (_activeInspector == dlg)
                {
                    _activeInspector = null;
                }
            };

            dlg.Show();
            dlg.BringToFront();
            dlg.Activate();
        }
    }
}
