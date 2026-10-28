using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    public class RescheduleDeliveryDialog : Form
    {
        private readonly Delivery? delivery;
        private readonly SpecialTrip? specialTrip;
        private readonly bool isSpecialTrip;

        private DateTimePicker dtpNewDate = null!;
        private TextBox txtNewTime = null!;
        private Button btnPickTime = null!;
        private ComboBox cbTruck = null!;
        private ComboBox cbDriver = null!;
        private ComboBox cbAide = null!;
        private ComboBox cbReason = null!;
        private TextBox txtNotes = null!;
        private Button btnConfirm = null!;
        private Button btnCancel = null!;

        private List<Truck> availableTrucks = new List<Truck>();
        private List<Crew> availableDrivers = new List<Crew>();
        private List<Crew> availableAides = new List<Crew>();

        public DateTime RescheduledDate { get; private set; }
        public string RescheduledTime { get; private set; } = "";
        public string RescheduledStatus => "Rescheduled";
        public int? RescheduledTruckId { get; private set; }
        public string? RescheduledTruckInfo { get; private set; }
        public int? RescheduledDriverId { get; private set; }
        public string? RescheduledDriverName { get; private set; }
        public int? RescheduledAideId { get; private set; }
        public string? RescheduledAideName { get; private set; }

        public RescheduleDeliveryDialog(Delivery targetDelivery)
        {
            delivery = targetDelivery ?? throw new ArgumentNullException(nameof(targetDelivery));
            specialTrip = null;
            isSpecialTrip = false;
            InitializeCustomComponent();
            LoadResources();
            UIThemeHelper.ApplyTheme(this);
        }

        public RescheduleDeliveryDialog(SpecialTrip targetSpecialTrip)
        {
            specialTrip = targetSpecialTrip ?? throw new ArgumentNullException(nameof(targetSpecialTrip));
            delivery = null;
            isSpecialTrip = true;
            InitializeCustomComponent();
            LoadResources();
            UIThemeHelper.ApplyTheme(this);
        }

        private void InitializeCustomComponent()
        {
            string orderRef = isSpecialTrip ? specialTrip!.OrderReference : delivery!.DispatchNumber;
            string clientName = isSpecialTrip ? (specialTrip!.Company ?? "") : (delivery!.ClientName ?? "");

            this.Text = isSpecialTrip ? $"Reschedule Special Trip: {orderRef}" : $"Reschedule Dispatch #{orderRef}";
            this.Size = new Size(740, 800);
            this.MinimumSize = new Size(720, 740);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Pal.C(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = Pal.C(15, 23, 42),
                Padding = new Padding(24, 14, 24, 14)
            };

            Label lblTitle = new Label
            {
                Text = isSpecialTrip ? "📅 Reschedule Special Trip" : "📅 Reschedule Dispatch Order",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Pal.White,
                Location = new Point(20, 12),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = $"Adjust target dispatch date, departure time, and resource assignments for {clientName}",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Pal.C(148, 163, 184),
                Location = new Point(20, 40),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 14, 24, 14),
                AutoScroll = true
            };

            Panel pnlInfo = new Panel
            {
                Location = new Point(24, 10),
                Size = new Size(670, 142),
                BackColor = Pal.C(241, 245, 249),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblInfoOrder = new Label
            {
                Text = isSpecialTrip
                    ? $"Special Trip: {specialTrip!.OrderReference}   |   Service Type: {specialTrip.ServiceType}"
                    : $"Dispatch Order: #{delivery!.DispatchNumber}",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Pal.C(15, 23, 42),
                Location = new Point(14, 10),
                MaximumSize = new Size(640, 22),
                AutoEllipsis = true,
                AutoSize = true
            };

            Label lblInfoClient = new Label
            {
                Text = isSpecialTrip
                    ? $"Company / Client: {specialTrip!.Company}   |   Destination: {specialTrip.Address}"
                    : $"Client: {delivery!.ClientName}   |   Route: {delivery.CleanRouteDisplay}",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Pal.C(51, 65, 85),
                Location = new Point(14, 34),
                MaximumSize = new Size(640, 22),
                AutoEllipsis = true,
                AutoSize = true
            };

            Label lblInfoCargo = new Label
            {
                Text = isSpecialTrip
                    ? $"Assigned Truck: {(!string.IsNullOrWhiteSpace(specialTrip!.TruckPlate) ? specialTrip.TruckPlate : "Unassigned")}"
                    : $"Cargo: {delivery!.TotalCrates:N0} Crates ({delivery.CargoType})   |   Truck: {(!string.IsNullOrWhiteSpace(delivery.TruckInfo) ? delivery.TruckInfo : "Unassigned")}",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Pal.C(51, 65, 85),
                Location = new Point(14, 58),
                MaximumSize = new Size(640, 22),
                AutoEllipsis = true,
                AutoSize = true
            };

            DateTime curDate = isSpecialTrip ? specialTrip!.DispatchDate : delivery!.DispatchDate;
            string curTime = isSpecialTrip ? (specialTrip!.DepartureTime ?? "") : (delivery!.DepartureTime ?? "");

            Label lblInfoCurDate = new Label
            {
                Text = $"Current Scheduled: {curDate:MM/dd/yyyy} at {curTime}",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Pal.C(194, 65, 12),
                Location = new Point(14, 82),
                MaximumSize = new Size(640, 22),
                AutoEllipsis = true,
                AutoSize = true
            };

            string curStatus = isSpecialTrip ? specialTrip!.Status : delivery!.Status;
            string curDriver = isSpecialTrip ? (specialTrip!.DriverName ?? "") : (delivery!.DriverName ?? "");
            string curAide = isSpecialTrip ? (specialTrip!.AideName ?? "") : (delivery!.AideName ?? "");

            Label lblInfoStatus = new Label
            {
                Text = $"Current Status: {curStatus}   |   Driver: {(!string.IsNullOrWhiteSpace(curDriver) ? curDriver : "Unassigned")}   |   Aide: {(!string.IsNullOrWhiteSpace(curAide) ? curAide : "Unassigned")}",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Pal.C(100, 116, 139),
                Location = new Point(14, 108),
                MaximumSize = new Size(640, 22),
                AutoEllipsis = true,
                AutoSize = true
            };

            pnlInfo.Controls.AddRange(new Control[] { lblInfoOrder, lblInfoClient, lblInfoCargo, lblInfoCurDate, lblInfoStatus });

            Label lblNewDate = new Label
            {
                Text = "New Scheduled Date: *",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                Location = new Point(24, 164),
                AutoSize = true
            };

            dtpNewDate = new DateTimePicker
            {
                Location = new Point(24, 188),
                Size = new Size(260, 28),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9.5F),
                Value = curDate > DateTime.MinValue ? curDate : DateTime.Today
            };

            Label lblNewTime = new Label
            {
                Text = "New Departure Time: *",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                Location = new Point(300, 164),
                AutoSize = true
            };

            txtNewTime = new TextBox
            {
                Location = new Point(300, 188),
                Size = new Size(260, 28),
                Font = new Font("Segoe UI", 9.5F),
                MaxLength = 20,
                Text = string.IsNullOrWhiteSpace(curTime) ? "08:00 AM" : curTime
            };

            btnPickTime = new Button
            {
                Text = "⏰ Time Picker",
                Location = new Point(570, 187),
                Size = new Size(124, 30),
                BackColor = Pal.C(30, 58, 138),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPickTime.FlatAppearance.BorderSize = 0;
            btnPickTime.Click += (s, e) =>
            {
                string? picked = AnalogClockTimePickerDialog.ShowTimePicker(this, txtNewTime.Text);
                if (!string.IsNullOrEmpty(picked))
                {
                    txtNewTime.Text = picked;
                }
            };

            Panel pnlResources = new Panel
            {
                Location = new Point(24, 228),
                Size = new Size(670, 136),
                BackColor = Pal.C(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12)
            };

            Label lblResTitle = new Label
            {
                Text = "🚚 Reassign Resources (Optional - Leave as '(Keep Current)' to preserve assignments)",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                Location = new Point(12, 10),
                AutoSize = true
            };

            Label lblTrk = new Label
            {
                Text = "Truck:",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Pal.C(71, 85, 105),
                Location = new Point(12, 38),
                AutoSize = true
            };

            cbTruck = new ComboBox
            {
                Location = new Point(12, 60),
                Size = new Size(204, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };

            Label lblDrv = new Label
            {
                Text = "Driver:",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Pal.C(71, 85, 105),
                Location = new Point(230, 38),
                AutoSize = true
            };

            cbDriver = new ComboBox
            {
                Location = new Point(230, 60),
                Size = new Size(204, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };

            Label lblAid = new Label
            {
                Text = "Aide / Crew:",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Pal.C(71, 85, 105),
                Location = new Point(446, 38),
                AutoSize = true
            };

            cbAide = new ComboBox
            {
                Location = new Point(446, 60),
                Size = new Size(208, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };

            Label lblResHint = new Label
            {
                Text = "Note: Selecting a specific truck, driver, or aide will update that assignment upon confirmation.",
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Pal.C(100, 116, 139),
                Location = new Point(12, 98),
                AutoSize = true
            };

            pnlResources.Controls.AddRange(new Control[]
            {
                lblResTitle,
                lblTrk, cbTruck,
                lblDrv, cbDriver,
                lblAid, cbAide,
                lblResHint
            });

            Label lblReason = new Label
            {
                Text = "Reason for Rescheduling: *",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                Location = new Point(24, 376),
                AutoSize = true
            };

            cbReason = new ComboBox
            {
                Location = new Point(24, 400),
                Size = new Size(670, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbReason.Items.AddRange(new object[]
            {
                "Adverse Weather / Typhoon Advisory",
                "Vehicle Mechanical Breakdown / Repair",
                "Client Request / Store Postponement",
                "Driver / Crew Emergency or Sickness",
                "Road Closure / Route Inaccessible",
                "Crate Loading Delay / Depot Congestion",
                "Customer Delivery Window Shift",
                "Other Reason (Specify in notes below)"
            });
            cbReason.SelectedIndex = 0;

            Label lblNotes = new Label
            {
                Text = "Detailed Remarks / Operational Notes:",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                Location = new Point(24, 440),
                AutoSize = true
            };

            txtNotes = new TextBox
            {
                Location = new Point(24, 464),
                Size = new Size(670, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5F),
                MaxLength = 500
            };

            pnlBody.Controls.AddRange(new Control[]
            {
                pnlInfo,
                lblNewDate,
                dtpNewDate,
                lblNewTime,
                txtNewTime,
                btnPickTime,
                pnlResources,
                lblReason,
                cbReason,
                lblNotes,
                txtNotes
            });

            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Pal.C(241, 245, 249),
                Padding = new Padding(24, 12, 24, 12)
            };

            btnCancel = new Button
            {
                Text = "✕ Cancel",
                Location = new Point(390, 14),
                Size = new Size(120, 36),
                BackColor = Pal.White,
                ForeColor = Pal.C(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            btnConfirm = new Button
            {
                Text = "📅 Confirm Reschedule",
                Location = new Point(520, 14),
                Size = new Size(174, 36),
                BackColor = Pal.C(234, 88, 12),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += BtnConfirm_Click;

            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(btnConfirm);

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);
        }

        private void LoadResources()
        {
            try
            {
                availableTrucks = MJdatabase.GetTrucks().Where(t => t.Status == "Active" || t.Status == "Available").ToList();
                cbTruck.Items.Clear();
                string curTrkText = isSpecialTrip
                    ? (!string.IsNullOrWhiteSpace(specialTrip!.TruckPlate) ? $" (Current: {specialTrip.TruckPlate})" : "")
                    : (!string.IsNullOrWhiteSpace(delivery!.TruckInfo) ? $" (Current: {delivery.TruckInfo})" : "");
                cbTruck.Items.Add($"(Keep Current){curTrkText}");
                foreach (var trk in availableTrucks)
                {
                    cbTruck.Items.Add($"{trk.PlateNumber} [{trk.TruckType}]");
                }
                cbTruck.SelectedIndex = 0;

                var allCrew = MJdatabase.GetCrews().Where(c => c.IsActive).ToList();
                availableDrivers = allCrew.Where(c => c.Role.Equals("Driver", StringComparison.OrdinalIgnoreCase)).ToList();
                availableAides = allCrew.Where(c => !c.Role.Equals("Driver", StringComparison.OrdinalIgnoreCase)).ToList();

                cbDriver.Items.Clear();
                string curDrvText = isSpecialTrip
                    ? (!string.IsNullOrWhiteSpace(specialTrip!.DriverName) ? $" (Current: {specialTrip.DriverName})" : "")
                    : (!string.IsNullOrWhiteSpace(delivery!.DriverName) ? $" (Current: {delivery.DriverName})" : "");
                cbDriver.Items.Add($"(Keep Current){curDrvText}");
                foreach (var d in availableDrivers)
                {
                    cbDriver.Items.Add(d.FullName);
                }
                cbDriver.SelectedIndex = 0;

                cbAide.Items.Clear();
                string curAideText = isSpecialTrip
                    ? (!string.IsNullOrWhiteSpace(specialTrip!.AideName) ? $" (Current: {specialTrip.AideName})" : "")
                    : (!string.IsNullOrWhiteSpace(delivery!.AideName) ? $" (Current: {delivery.AideName})" : "");
                cbAide.Items.Add($"(Keep Current){curAideText}");
                foreach (var a in availableAides)
                {
                    cbAide.Items.Add(a.FullName);
                }
                cbAide.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RescheduleDialog] LoadResources error: {ex.Message}");
            }
        }

        private void BtnConfirm_Click(object? sender, EventArgs e)
        {
            DateTime newDate = dtpNewDate.Value.Date;
            string newTime = txtNewTime.Text.Trim();
            string selectedReason = cbReason.SelectedItem?.ToString() ?? "";
            string notes = txtNotes.Text.Trim();

            if (notes.Length > 500)
            {
                MessageBox.Show("Remarks / operational notes cannot exceed 500 characters.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNotes.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(newTime) || !Regex.IsMatch(newTime, @"^(0?[1-9]|1[0-2]):[0-5][0-9]\s*(AM|PM)$", RegexOptions.IgnoreCase))
            {
                MessageBox.Show("Please enter or select a valid departure time (e.g. 08:00 AM).", "Invalid Departure Time", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNewTime.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedReason))
            {
                MessageBox.Show("Please select a reason for rescheduling.", "Reason Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbReason.Focus();
                return;
            }

            if (selectedReason.StartsWith("Other") && string.IsNullOrWhiteSpace(notes))
            {
                MessageBox.Show("Please specify the exact reschedule reason in the remarks notes box.", "Specific Reason Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNotes.Focus();
                return;
            }

            int? newTruckId = null;
            string? newTruckInfo = null;
            string? newTruckPlate = null;
            if (cbTruck.SelectedIndex > 0 && cbTruck.SelectedIndex - 1 < availableTrucks.Count)
            {
                var selTrk = availableTrucks[cbTruck.SelectedIndex - 1];
                newTruckId = selTrk.Id;
                newTruckInfo = $"{selTrk.PlateNumber} [{selTrk.TruckType}]";
                newTruckPlate = selTrk.PlateNumber;
            }

            int? newDriverId = null;
            string? newDriverName = null;
            if (cbDriver.SelectedIndex > 0 && cbDriver.SelectedIndex - 1 < availableDrivers.Count)
            {
                var selDrv = availableDrivers[cbDriver.SelectedIndex - 1];
                newDriverId = selDrv.Id;
                newDriverName = selDrv.FullName;
            }

            int? newAideId = null;
            string? newAideName = null;
            if (cbAide.SelectedIndex > 0 && cbAide.SelectedIndex - 1 < availableAides.Count)
            {
                var selAide = availableAides[cbAide.SelectedIndex - 1];
                newAideId = selAide.Id;
                newAideName = selAide.FullName;
            }

            string fullReason = string.IsNullOrWhiteSpace(notes) ? selectedReason : $"{selectedReason} - {notes}";

            string orderIdentifier = isSpecialTrip ? $"Special Trip '{specialTrip!.OrderReference}'" : $"Dispatch #{delivery!.DispatchNumber}";
            DateTime curDate = isSpecialTrip ? specialTrip!.DispatchDate : delivery!.DispatchDate;
            string curTime = isSpecialTrip ? (specialTrip!.DepartureTime ?? "") : (delivery!.DepartureTime ?? "");

            string confirmMsg = $"Are you sure you want to reschedule {orderIdentifier}?\n\n" +
                                $"From: {curDate:MM/dd/yyyy} ({curTime})\n" +
                                $"To:   {newDate:MM/dd/yyyy} ({newTime})\n" +
                                $"Reason: {fullReason}";

            var res = MessageBox.Show(confirmMsg, "Confirm Reschedule", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res != DialogResult.Yes) return;

            string currentUser = SessionContext.ActiveUser?.Username ?? SessionContext.CurrentUsername;
            (bool Success, string Message) result;

            if (isSpecialTrip)
            {
                result = MJdatabase.RescheduleSpecialTrip(
                    specialTrip!.Id,
                    newDate,
                    newTime,
                    fullReason,
                    currentUser,
                    newTruckId,
                    newTruckPlate,
                    newDriverId,
                    newDriverName,
                    newAideId,
                    newAideName
                );
            }
            else
            {
                result = MJdatabase.RescheduleDelivery(
                    delivery!.Id,
                    newDate,
                    newTime,
                    fullReason,
                    currentUser,
                    newTruckId,
                    newTruckInfo,
                    newDriverId,
                    newDriverName,
                    newAideId,
                    newAideName
                );
            }

            if (result.Success)
            {
                RescheduledDate = newDate;
                RescheduledTime = newTime;
                RescheduledTruckId = newTruckId;
                RescheduledTruckInfo = newTruckInfo ?? newTruckPlate;
                RescheduledDriverId = newDriverId;
                RescheduledDriverName = newDriverName;
                RescheduledAideId = newAideId;
                RescheduledAideName = newAideName;

                MessageBox.Show(result.Message, "Reschedule Confirmed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(result.Message, "Reschedule Blocked", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
