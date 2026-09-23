using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public class ScheduleFutureTripDialog : Form
    {
        private readonly Delivery source;
        private DateTimePicker dtpTargetDate = null!;
        private ComboBox cbDepartureTime = null!;
        private Button btnConfirm = null!;
        private Button btnCancel = null!;
        private Delivery? createdDelivery;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Delivery? CreatedDelivery => createdDelivery;

        public ScheduleFutureTripDialog(Delivery sourceDelivery)
        {
            source = sourceDelivery ?? throw new ArgumentNullException(nameof(sourceDelivery));
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Schedule Future Trip";
            this.Size = new Size(640, 590);
            this.MinimumSize = new Size(580, 540);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = UITheme.SurfaceBackground;
            this.Font = UITheme.InputFont;

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(238, 242, 255),
                Padding = new Padding(20, 12, 20, 12)
            };

            Label lblTitle = new Label
            {
                Text = $"📅 Schedule Future Trip from #{source.DispatchNumber}",
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 64, 175),
                Dock = DockStyle.Top,
                Height = 24
            };

            Label lblSubtitle = new Label
            {
                Text = "Duplicate vehicle, crew assignments, and route consignment into a new future dispatch.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Bottom,
                Height = 20
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                AutoScroll = true
            };

            Panel pnlSourceCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(14)
            };

            Label lblCardTitle = new Label
            {
                Text = "Source Dispatch Parameters (Will be Replicated):",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 20
            };

            string routeDesc = source.IsMultiDrop
                ? $"🗺️ Multi-Drop Route ({source.TotalStopsCount} stops): {source.LoadingScheduleInfo}"
                : $"📍 Direct Route: {source.LoadingScheduleInfo}";

            Label lblSourceDetails = new Label
            {
                Text = $"• Vehicle: {source.TruckInfo}\n• Driver: {source.DriverName}  |  Aide(s): {source.AideNames}\n• Route: {routeDesc}\n• Total Cargo: {source.TotalCrates} Crates ({source.GCrate} G + {source.ECrate} E)",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(51, 65, 85),
                Dock = DockStyle.Fill
            };

            pnlSourceCard.Controls.Add(lblSourceDetails);
            pnlSourceCard.Controls.Add(lblCardTitle);

            Panel pnlInputs = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                Padding = new Padding(0, 14, 0, 0)
            };

            Label lblDatePrompt = new Label
            {
                Text = "Target Dispatch Date (Future):",
                Font = UITheme.FieldLabelFont,
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 22
            };

            dtpTargetDate = new DateTimePicker
            {
                Dock = DockStyle.Top,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dddd, MMMM dd, yyyy",
                MinDate = DateTime.Today.AddDays(1),
                Value = DateTime.Today.AddDays(1),
                Font = new Font("Segoe UI", 10F)
            };

            Label lblTimePrompt = new Label
            {
                Text = "Target Departure Time:",
                Font = UITheme.FieldLabelFont,
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(0, 6, 0, 0)
            };

            cbDepartureTime = new ComboBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F)
            };
            cbDepartureTime.Items.AddRange(new object[]
            {
                "05:00 AM", "05:30 AM", "06:00 AM", "06:30 AM", "07:00 AM", "07:30 AM",
                "08:00 AM", "08:30 AM", "09:00 AM", "10:00 AM", "11:00 AM", "01:00 PM",
                "02:00 PM", "03:00 PM", "08:00 PM", "09:00 PM", "10:00 PM"
            });
            cbDepartureTime.Text = string.IsNullOrWhiteSpace(source.DepartureTime) ? "08:00 AM" : source.DepartureTime;

            pnlInputs.Controls.Add(cbDepartureTime);
            pnlInputs.Controls.Add(lblTimePrompt);
            pnlInputs.Controls.Add(dtpTargetDate);
            pnlInputs.Controls.Add(lblDatePrompt);

            Panel pnlRuleNote = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(240, 253, 244),
                Padding = new Padding(12),
                Margin = new Padding(0, 10, 0, 0)
            };

            Label lblRuleNote = new Label
            {
                Text = "✓ Date-Aware Temporal Scheduling Active: Vehicles and crew marked 'On Trip' today can be safely scheduled for future dates. Date-specific conflict validation will run on save.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(22, 101, 52),
                Dock = DockStyle.Fill
            };
            pnlRuleNote.Controls.Add(lblRuleNote);

            pnlBody.Controls.Add(pnlRuleNote);
            pnlBody.Controls.Add(pnlInputs);
            pnlBody.Controls.Add(pnlSourceCard);

            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = UITheme.SecondarySurface,
                Padding = new Padding(16, 12, 16, 12)
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(110, 36),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            btnConfirm = new Button
            {
                Text = "📅 Confirm Future Dispatch",
                Size = new Size(210, 36),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.PrimaryAccent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += BtnConfirm_Click;

            pnlFooter.Controls.Add(btnConfirm);
            pnlFooter.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 10 });
            pnlFooter.Controls.Add(btnCancel);

            this.Controls.Add(pnlBody);
            pnlBody.BringToFront();
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
            this.AcceptButton = btnConfirm;
            this.CancelButton = btnCancel;
        }

        private void BtnConfirm_Click(object? sender, EventArgs e)
        {
            DateTime targetDate = dtpTargetDate.Value.Date;
            if (targetDate <= DateTime.Today)
            {
                MessageBox.Show("Please select a future date (tomorrow or later) for scheduling.", "Invalid Date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dtpTargetDate.Focus();
                return;
            }

            string depTime = cbDepartureTime.Text.Trim();
            if (string.IsNullOrWhiteSpace(depTime)) depTime = "08:00 AM";

            var (success, msg, cloned) = MJdatabase.CloneDeliveryForFutureDate(source.Id, targetDate, depTime);
            if (!success)
            {
                MessageBox.Show(msg, "Scheduling Conflict", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            createdDelivery = cloned;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
