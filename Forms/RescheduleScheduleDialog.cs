using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    public class RescheduleScheduleDialog : Form
    {
        private readonly LoadingSchedule schedule;
        private DateTimePicker dtpNewDate = null!;
        private TextBox txtNewTime = null!;
        private Button btnPickTime = null!;
        private ComboBox cbReason = null!;
        private TextBox txtNotes = null!;
        private Button btnConfirm = null!;
        private Button btnCancel = null!;

        public RescheduleScheduleDialog(LoadingSchedule targetSchedule)
        {
            schedule = targetSchedule ?? throw new ArgumentNullException(nameof(targetSchedule));
            InitializeComponent();
            UIThemeHelper.ApplyTheme(this);
        }

        private void InitializeComponent()
        {
            this.Text = $"Reschedule Order #{schedule.ScheduleNumber}";
            this.Size = new Size(620, 720);
            this.MinimumSize = new Size(580, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(24, 14, 24, 14)
            };

            Label lblTitle = new Label
            {
                Text = "📅 Reschedule Loading Order",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 12),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = $"Adjust target dispatch date and departure time for {schedule.ClientName}",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(20, 40),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                AutoScroll = true
            };

            Panel pnlInfo = new Panel
            {
                Location = new Point(20, 10),
                Size = new Size(484, 135),
                BackColor = Color.FromArgb(241, 245, 249),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblInfoOrder = new Label
            {
                Text = $"Order Number: #{schedule.ScheduleNumber}",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(14, 10),
                AutoSize = true
            };

            Label lblInfoClient = new Label
            {
                Text = $"Client: {schedule.ClientName}  |  Destination: {schedule.Destination}",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(14, 34),
                AutoSize = true
            };

            Label lblInfoCargo = new Label
            {
                Text = $"Cargo: {schedule.TotalCrates:N0} Crates ({schedule.CargoType})",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(14, 58),
                AutoSize = true
            };

            Label lblInfoCurDate = new Label
            {
                Text = $"Current Scheduled: {schedule.ScheduleDate:MM/dd/yyyy} at {schedule.DepartureTime}",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(194, 65, 12),
                Location = new Point(14, 82),
                AutoSize = true
            };

            string priorText = schedule.RescheduleCount == 0
                ? "Prior Adjustments: None (First Reschedule)"
                : $"Prior Adjustments: Rescheduled {schedule.RescheduleCount} time(s) (Original Date: {(schedule.OriginalScheduleDate.HasValue ? schedule.OriginalScheduleDate.Value.ToString("MM/dd/yyyy") : "N/A")})";

            Label lblInfoPrior = new Label
            {
                Text = priorText,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(14, 106),
                AutoSize = true
            };

            pnlInfo.Controls.AddRange(new Control[] { lblInfoOrder, lblInfoClient, lblInfoCargo, lblInfoCurDate, lblInfoPrior });

            Label lblNewDate = new Label
            {
                Text = "New Scheduled Date: *",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 160),
                AutoSize = true
            };

            dtpNewDate = new DateTimePicker
            {
                Location = new Point(20, 185),
                Size = new Size(484, 26),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9.5F),
                Value = schedule.ScheduleDate > DateTime.Today ? schedule.ScheduleDate : DateTime.Today
            };

            Label lblNewTime = new Label
            {
                Text = "New Departure Time: *",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 225),
                AutoSize = true
            };

            txtNewTime = new TextBox
            {
                Location = new Point(20, 250),
                Size = new Size(396, 26),
                Font = new Font("Segoe UI", 9.5F),
                MaxLength = 20,
                Text = string.IsNullOrEmpty(schedule.DepartureTime) ? "08:00 AM" : schedule.DepartureTime
            };

            btnPickTime = new Button
            {
                Text = "⏰ Clock",
                Location = new Point(424, 248),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(30, 58, 138),
                ForeColor = Color.White,
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

            Label lblReason = new Label
            {
                Text = "Reason for Rescheduling: *",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 290),
                AutoSize = true
            };

            cbReason = new ComboBox
            {
                Location = new Point(20, 315),
                Size = new Size(484, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbReason.Items.AddRange(new object[]
            {
                "Client Packing Delay",
                "Consignee Warehouse Postponement",
                "Typhoon / Severe Weather Advisory",
                "Vehicle Mechanical Hold",
                "Road Repair / Route Access Delay",
                "Production / Supply Shortage",
                "Customer Requested Delivery Window Shift",
                "Operational Dispatch Reallocation",
                "Other Reason (Specify in notes below)"
            });
            cbReason.SelectedIndex = 0;

            Label lblNotes = new Label
            {
                Text = "Detailed Remarks / Operational Notes:",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 355),
                AutoSize = true
            };

            txtNotes = new TextBox
            {
                Location = new Point(20, 380),
                Size = new Size(484, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5F),
                MaxLength = 500
            };

            pnlBody.Controls.AddRange(new Control[]
            {
                pnlInfo,
                lblNewDate, dtpNewDate,
                lblNewTime, txtNewTime, btnPickTime,
                lblReason, cbReason,
                lblNotes, txtNotes
            });

            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Color.White,
                Padding = new Padding(20, 14, 20, 14)
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(110, 36),
                Location = new Point(266, 14),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            btnConfirm = new Button
            {
                Text = "📅 Confirm Reschedule",
                Size = new Size(160, 36),
                Location = new Point(384, 14),
                BackColor = Color.FromArgb(234, 88, 12),
                ForeColor = Color.White,
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

            if (string.IsNullOrWhiteSpace(newTime) || !System.Text.RegularExpressions.Regex.IsMatch(newTime, @"^(0?[1-9]|1[0-2]):[0-5][0-9]\s*(AM|PM)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
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

            string fullReason = string.IsNullOrWhiteSpace(notes) ? selectedReason : $"{selectedReason} - {notes}";

            string confirmMsg = $"Are you sure you want to reschedule Order #{schedule.ScheduleNumber}?\n\n" +
                                $"From: {schedule.ScheduleDate:MM/dd/yyyy} ({schedule.DepartureTime})\n" +
                                $"To:   {newDate:MM/dd/yyyy} ({newTime})\n" +
                                $"Reason: {fullReason}";

            var res = MessageBox.Show(confirmMsg, "Confirm Order Reschedule", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res != DialogResult.Yes) return;

            string currentUser = SessionContext.ActiveUser?.Username ?? SessionContext.CurrentUsername;
            var result = MJdatabase.RescheduleLoadingSchedule(schedule.Id, newDate, newTime, fullReason, currentUser);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Schedule Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
