using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class ReportsView : UserControl, IRefreshable
    {
        public ReportsView()
        {
            InitializeComponent();
            gridReport.BringToFront();
            SetupGridColumns();
            cbReportType.SelectedIndex = 0;
            cbStatusFilter.SelectedIndex = 0;
            LoadReportData();
            UIThemeHelper.ApplyTheme(this);
        }

        public void RefreshData()
        {
            LoadReportData();
        }

        private void SetupGridColumns()
        {
            gridReport.AutoGenerateColumns = true;
            gridReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridReport.AllowUserToResizeColumns = true;
            gridReport.ScrollBars = ScrollBars.Both;
            gridReport.ShowCellToolTips = true;
        }

        private void CbReportType_SelectedIndexChanged(object? sender, EventArgs e)
        {
            LoadReportData();
        }

        private void CbStatusFilter_SelectedIndexChanged(object? sender, EventArgs e)
        {
            LoadReportData();
        }

        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            LoadReportData();
        }

        private async void LoadReportData()
        {
            try
            {
                string search = txtSearch.Text.Trim().ToLowerInvariant();
            string statusFilter = cbStatusFilter.SelectedItem?.ToString() ?? "All Statuses";

            switch (cbReportType.SelectedIndex)
            {
                case 0:
                    var deliveries = await MJdatabase.GetDeliveriesAsync();
                    if (statusFilter == "Completed Only") deliveries = deliveries.Where(d => d.Status == "Completed").ToList();
                    else if (statusFilter == "Cancelled Only") deliveries = deliveries.Where(d => d.Status == "Cancelled").ToList();
                    else if (statusFilter == "Pending / In Transit") deliveries = deliveries.Where(d => d.Status == "Pending" || d.Status == "In Transit").ToList();

                    if (!string.IsNullOrEmpty(search))
                    {
                        deliveries = deliveries.Where(d => d.DispatchNumber.ToLower().Contains(search) ||
                                                           d.LoadingScheduleInfo.ToLower().Contains(search) ||
                                                           d.DriverName.ToLower().Contains(search) ||
                                                           d.AideNames.ToLower().Contains(search) ||
                                                           d.CancellationReason.ToLower().Contains(search)).ToList();
                    }

                    var cleanDeliveries = deliveries.Select(d => new
                    {
                        Dispatch_Number = d.DispatchNumber,
                        Loading_Schedule = d.LoadingScheduleInfo,
                        Assigned_Truck = d.TruckInfo,
                        Driver = d.DriverName,
                        Aides = d.AideNames,
                        Dispatch_Date = d.DispatchDate.ToString("yyyy-MM-dd"),
                        Departure_Time = d.DepartureTime,
                        G_Crates = d.GCrate,
                        E_Crates = d.ECrate,
                        Total_Crates = d.TotalCrates,
                        Cargo_Temp = d.CargoType,
                        Status = d.Status
                    }).ToList();

                    gridReport.DataSource = cleanDeliveries;
                    int totalDelCrates = deliveries.Sum(d => d.TotalCrates);
                    lblSummary.Text = $"📊 Total Dispatches: {deliveries.Count:N0}  |  📦 Total Dispatched Crates: {totalDelCrates:N0}  |  🚛 Active Trips: {deliveries.Count(d => d.Status == "In Transit"):N0}";
                    break;

                case 1:
                    var schedules = await MJdatabase.GetLoadingSchedulesAsync();
                    if (statusFilter == "Completed Only") schedules = schedules.Where(s => s.Status == "Completed").ToList();
                    else if (statusFilter == "Cancelled Only") schedules = schedules.Where(s => s.Status == "Cancelled").ToList();
                    else if (statusFilter == "Pending / In Transit") schedules = schedules.Where(s => s.Status == "Pending" || s.Status == "Assigned").ToList();

                    if (!string.IsNullOrEmpty(search))
                    {
                        schedules = schedules.Where(s => s.ScheduleNumber.ToLower().Contains(search) ||
                                                         s.ClientName.ToLower().Contains(search) ||
                                                         s.Destination.ToLower().Contains(search) ||
                                                         s.CancellationReason.ToLower().Contains(search)).ToList();
                    }

                    var cleanSchedules = schedules.Select(s => new
                    {
                        Schedule_Number = s.ScheduleNumber,
                        Client_Name = s.ClientName,
                        Destination = s.Destination,
                        Schedule_Date = s.ScheduleDate.ToString("yyyy-MM-dd"),
                        Departure = s.DepartureTime,
                        G_Crates = s.GCrate,
                        E_Crates = s.ECrate,
                        Total_Crates = s.TotalCrates,
                        Peak_Season = s.IsPeak ? "Yes" : "No",
                        Cargo_Temp = s.CargoType,
                        Status = s.Status
                    }).ToList();

                    gridReport.DataSource = cleanSchedules;
                    int totalSchCrates = schedules.Sum(s => s.TotalCrates);
                    lblSummary.Text = $"📊 Total Schedules: {schedules.Count:N0}  |  📦 Total Booked Crates: {totalSchCrates:N0}  |  ⏳ Pending Schedules: {schedules.Count(s => s.Status == "Pending"):N0}";
                    break;

                case 2:
                    var tripReports = await MJdatabase.GetTripReportsAsync();
                    if (!string.IsNullOrEmpty(search))
                    {
                        tripReports = tripReports.Where(t => t.TripReportNumber.ToLower().Contains(search) ||
                                                             t.DispatchNumber.ToLower().Contains(search) ||
                                                             t.StoreManagerName.ToLower().Contains(search) ||
                                                             t.Remarks.ToLower().Contains(search)).ToList();
                    }

                    var cleanTripReports = tripReports.Select(t => new
                    {
                        Trip_Report_No = t.TripReportNumber,
                        Dispatch_No = t.DispatchNumber,
                        Report_Date = t.ReportDate.ToString("yyyy-MM-dd"),
                        Arrival_Time = t.ArrivalTime,
                        Departure_Time = t.DepartureTime,
                        Delivered_G = t.DeliveredGCrates,
                        Delivered_E = t.DeliveredECrates,
                        Total_Delivered = t.DeliveredCrates,
                        Returned_G = t.ReturnedGCrates,
                        Returned_E = t.ReturnedECrates,
                        Total_Returned = t.ReturnedCrates,
                        Store_Manager = t.StoreManagerName,
                        Remarks = t.Remarks
                    }).ToList();

                    gridReport.DataSource = cleanTripReports;
                    int totalDeliveredCrates = tripReports.Sum(t => t.DeliveredCrates);
                    int totalReturnedCrates = tripReports.Sum(t => t.ReturnedCrates);
                    lblSummary.Text = $"📊 Total Completed Trip Reports: {tripReports.Count:N0}  |  📥 Delivered: {totalDeliveredCrates:N0} Crates  |  🔄 Returned Empties: {totalReturnedCrates:N0} Crates";
                    break;

                case 3:
                    var trucks = await MJdatabase.GetTrucksAsync();
                    if (!string.IsNullOrEmpty(search))
                    {
                        trucks = trucks.Where(t => t.PlateNumber.ToLower().Contains(search) ||
                                                   t.TruckType.ToLower().Contains(search) ||
                                                   t.Status.ToLower().Contains(search)).ToList();
                    }

                    var cleanTrucks = trucks.Select(t => new
                    {
                        Plate_Number = t.PlateNumber,
                        Truck_Type = t.TruckType,
                        Cargo_Capability = t.CargoType,
                        Capacity_Crates = t.CapacityCrates,
                        Status = t.Status,
                        Remarks = t.Remarks
                    }).ToList();

                    gridReport.DataSource = cleanTrucks;
                    int totalTruckCapacity = trucks.Sum(t => t.CapacityCrates);
                    lblSummary.Text = $"🚛 Total Registered Trucks: {trucks.Count:N0}  |  🟢 Active: {trucks.Count(t => t.Status == "Active" || t.Status == "Available"):N0}  |  🔴 In Use: {trucks.Count(t => t.Status == "In Use"):N0}  |  📦 Total Truck Capacity: {totalTruckCapacity:N0} Crates";
                    break;

                case 4:
                    var crews = await MJdatabase.GetCrewsAsync();
                    if (!string.IsNullOrEmpty(search))
                    {
                        crews = crews.Where(c => c.FullName.ToLower().Contains(search) ||
                                                 c.Role.ToLower().Contains(search) ||
                                                 c.ContactNumber.ToLower().Contains(search) ||
                                                 c.LicenseNumber.ToLower().Contains(search)).ToList();
                    }

                    var cleanCrews = crews.Select(c => new
                    {
                        Full_Name = c.FullName,
                        Role = c.Role,
                        Contact_Number = c.ContactNumber,
                        Drivers_License = c.LicenseNumber,
                        Employment_Type = c.IsSeasonal ? "Seasonal / Extra" : "Regular Active",
                        Availability = c.IsAvailable ? "Active" : "On Trip",
                        Status = c.IsActive ? "Active" : "Inactive"
                    }).ToList();

                    gridReport.DataSource = cleanCrews;
                    lblSummary.Text = $"👤 Total Personnel: {crews.Count:N0}  |  🚗 Drivers: {crews.Count(c => c.Role.IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0):N0}  |  🤝 Aides/Helpers: {crews.Count(c => c.Role.IndexOf("Aide", StringComparison.OrdinalIgnoreCase) >= 0 || c.Role.IndexOf("Helper", StringComparison.OrdinalIgnoreCase) >= 0):N0}  |  🟢 Active: {crews.Count(c => c.IsAvailable):N0}";
                    break;

                case 5:
                    var invoices = await MJdatabase.GetInvoicesAsync();
                    if (!string.IsNullOrEmpty(search))
                    {
                        invoices = invoices.Where(i => i.InvoiceNumber.ToLower().Contains(search) ||
                                                       i.ClientName.ToLower().Contains(search) ||
                                                       i.Status.ToLower().Contains(search)).ToList();
                    }

                    var cleanInvoices = invoices.Select(i => new
                    {
                        Invoice_Number = i.InvoiceNumber,
                        Client_Company = i.ClientName,
                        Issue_Date = i.IssueDate.ToString("yyyy-MM-dd"),
                        Due_Date = i.DueDate.ToString("yyyy-MM-dd"),
                        Gross_Amount = $"PHP {i.TotalAmount:N2}",
                        Deductions = $"PHP {i.Deductions:N2}",
                        Net_Amount_Due = $"PHP {i.NetAmount:N2}",
                        Payment_Status = i.Status
                    }).ToList();

                    gridReport.DataSource = cleanInvoices;
                    decimal totalNet = invoices.Sum(i => i.NetAmount);
                    decimal totalDeductions = invoices.Sum(i => i.Deductions);
                    lblSummary.Text = $"🧾 Total Invoices: {invoices.Count:N0}  |  💰 Total Net Billing: PHP {totalNet:N2}  |  📉 Total Deductions: PHP {totalDeductions:N2}  |  🟢 Paid: {invoices.Count(i => i.Status == "Paid"):N0}  |  🔴 Unpaid: {invoices.Count(i => i.Status == "Unpaid"):N0}";
                    break;

                case 6:
                    var receipts = await MJdatabase.GetCollectionReceiptsAsync();
                    if (!string.IsNullOrEmpty(search))
                    {
                        receipts = receipts.Where(r => r.ReceiptNumber.ToLower().Contains(search) ||
                                                       r.InvoiceNumber.ToLower().Contains(search) ||
                                                       r.PaymentMethod.ToLower().Contains(search) ||
                                                       r.ReferenceNumber.ToLower().Contains(search)).ToList();
                    }

                    var cleanReceipts = receipts.Select(r => new
                    {
                        Receipt_Number = r.ReceiptNumber,
                        Invoice_Number = r.InvoiceNumber,
                        Payment_Date = r.PaymentDate.ToString("yyyy-MM-dd"),
                        Amount_Paid = $"PHP {r.AmountPaid:N2}",
                        Payment_Method = r.PaymentMethod,
                        Reference_Number = r.ReferenceNumber,
                        Notes = r.Notes
                    }).ToList();

                    gridReport.DataSource = cleanReceipts;
                    decimal totalCollected = receipts.Sum(r => r.AmountPaid);
                    lblSummary.Text = $"💳 Total Collection Receipts: {receipts.Count:N0}  |  💵 Total Amount Collected: PHP {totalCollected:N2}";
                    break;

                case 7:
                    var incidents = await MJdatabase.GetIncidentReportsAsync();
                    if (!string.IsNullOrEmpty(search))
                    {
                        incidents = incidents.Where(i => i.DispatchNumber.ToLower().Contains(search) ||
                                                         i.Cause.ToLower().Contains(search) ||
                                                         i.Description.ToLower().Contains(search) ||
                                                         i.Status.ToLower().Contains(search)).ToList();
                    }

                    var cleanIncidents = incidents.Select(i => new
                    {
                        Dispatch_Number = i.DispatchNumber,
                        Incident_Date = i.IncidentDate.ToString("yyyy-MM-dd"),
                        Cause = i.Cause,
                        Damaged_Crates = i.DamagedQuantity,
                        Estimated_Loss = $"PHP {i.EstimatedCost:N2}",
                        Chargeable_To_Crew = i.ChargeableToCrew ? "Yes" : "No",
                        Description = i.Description,
                        Status = i.Status
                    }).ToList();

                    gridReport.DataSource = cleanIncidents;
                    decimal totalLoss = incidents.Sum(i => i.EstimatedCost);
                    int totalDamaged = incidents.Sum(i => i.DamagedQuantity);
                    lblSummary.Text = $"⚠️ Total Incidents: {incidents.Count:N0}  |  💥 Damaged Crates: {totalDamaged:N0}  |  💸 Total Estimated Loss: PHP {totalLoss:N2}  |  🟢 Resolved: {incidents.Count(i => i.Status == "Resolved"):N0}";
                    break;

                case 8:
                    var allCrews = await MJdatabase.GetCrewsAsync();
                    var allDels = await MJdatabase.GetDeliveriesAsync();
                    var completedDels = allDels.Where(d => d.Status == "Completed").ToList();

                    if (!string.IsNullOrEmpty(search))
                    {
                        allCrews = allCrews.Where(c => c.FullName.ToLower().Contains(search) ||
                                                       c.Role.ToLower().Contains(search) ||
                                                       c.ContactNumber.ToLower().Contains(search)).ToList();
                    }

                    var crewPayList = new List<object>();
                    decimal totalPayoutSum = 0;
                    int totalTripsSum = 0;

                    foreach (var c in allCrews)
                    {
                        var myTrips = completedDels.Where(d =>
                            d.DriverId == c.Id ||
                            (!string.IsNullOrEmpty(d.AideIds) && d.AideIds.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Contains(c.Id.ToString()))
                        ).ToList();

                        int tripCount = myTrips.Count;
                        decimal baseWages = tripCount * c.DailyWage;
                        decimal allowances = tripCount * c.FoodAllowance;
                        decimal netPayout = baseWages + allowances;

                        totalTripsSum += tripCount;
                        totalPayoutSum += netPayout;

                        crewPayList.Add(new
                        {
                            Crew_Name = c.FullName,
                            Role = c.Role,
                            Contact = c.ContactNumber,
                            Completed_Trips = tripCount,
                            Daily_Wage = $"PHP {c.DailyWage:N2}",
                            Total_Wages = $"PHP {baseWages:N2}",
                            Food_Allowance = $"PHP {c.FoodAllowance:N2}",
                            Total_Allowance = $"PHP {allowances:N2}",
                            Net_Compensation = $"PHP {netPayout:N2}"
                        });
                    }

                    gridReport.DataSource = crewPayList;
                    lblSummary.Text = $"👥 Crew Personnel: {allCrews.Count:N0}  |  🚚 Total Hauling Trips: {totalTripsSum:N0}  |  💵 Total Net Compensation Payable: PHP {totalPayoutSum:N2}";
                    break;
            }

                foreach (DataGridViewColumn col in gridReport.Columns)
                {
                    col.HeaderText = col.HeaderText.Replace("_", " ");
                    col.MinimumWidth = 90;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load report data: {ex.Message}", "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnPrintPaySlip_Click(object? sender, EventArgs e)
        {
            try
            {
                var crews = await MJdatabase.GetCrewsAsync();
                if (crews.Count == 0)
                {
                    MessageBox.Show("No registered crew members found.", "No Personnel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Crew? selectedCrew = null;
                if (gridReport.SelectedRows.Count > 0 && gridReport.SelectedRows[0].Cells.Count > 0)
                {
                    string? name = gridReport.SelectedRows[0].Cells[0].Value?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        selectedCrew = crews.FirstOrDefault(c => c.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));
                    }
                }

                if (selectedCrew == null)
                {
                    selectedCrew = crews[0];
                }

                var allDels = await MJdatabase.GetDeliveriesAsync();
                var crewTrips = allDels.Where(d =>
                    d.Status == "Completed" && (
                        d.DriverId == selectedCrew.Id ||
                        (!string.IsNullOrEmpty(d.AideIds) && d.AideIds.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Contains(selectedCrew.Id.ToString()))
                    )
                ).ToList();

                DateTime startDate = crewTrips.Count > 0 ? crewTrips.Min(t => t.DispatchDate) : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                DateTime endDate = DateTime.Today;

                PrintHelper.PrintCrewPaySlip(selectedCrew, crewTrips, startDate, endDate);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to print pay slip: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            try
            {
                string selectedType = cbReportType.SelectedItem?.ToString() ?? "Analytical Audit Report";
                string filterNote = $"Filter: Status '{cbStatusFilter.SelectedItem}', Search '{txtSearch.Text.Trim()}'.";
                PrintHelper.PrintGridReport(selectedType, gridReport, $"Official generated report summary. {filterNote}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate print report: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            if (gridReport.Rows.Count == 0)
            {
                MessageBox.Show("No data available to export.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"Report_{(cbReportType.SelectedItem?.ToString() ?? "Summary").Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < gridReport.Columns.Count; i++)
                    {
                        sb.Append(gridReport.Columns[i].HeaderText);
                        if (i < gridReport.Columns.Count - 1) sb.Append(",");
                    }
                    sb.AppendLine();

                    foreach (DataGridViewRow row in gridReport.Rows)
                    {
                        for (int i = 0; i < gridReport.Columns.Count; i++)
                        {
                            string val = row.Cells[i].Value?.ToString()?.Replace(",", " ") ?? "";
                            sb.Append(val);
                            if (i < gridReport.Columns.Count - 1) sb.Append(",");
                        }
                        sb.AppendLine();
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString());
                    MessageBox.Show("Report exported successfully to CSV!", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
