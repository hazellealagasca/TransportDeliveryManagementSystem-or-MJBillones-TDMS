using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public class AddStopDialog : Form
    {
        private readonly List<LoadingSchedule> availableSchedules;
        private ComboBox cbSelectSchedule = null!;
        private TextBox txtClientName = null!;
        private TextBox txtDestination = null!;
        private NumericUpDown numGCrate = null!;
        private NumericUpDown numECrate = null!;
        private TextBox txtRemarks = null!;
        private Button btnConfirm = null!;
        private Button btnCancel = null!;
        private DispatchStop? createdStop;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DispatchStop? CreatedStop => createdStop;

        public AddStopDialog(List<LoadingSchedule> schedules, int nextSequence = 1)
        {
            availableSchedules = schedules ?? new List<LoadingSchedule>();
            InitializeComponent(nextSequence);
        }

        private void InitializeComponent(int nextSequence)
        {
            this.Text = "Add Delivery Drop / Stop";
            this.Size = new Size(640, 580);
            this.MinimumSize = new Size(580, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = UITheme.SurfaceBackground;
            this.Font = UITheme.InputFont;

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(240, 253, 244),
                Padding = new Padding(20, 12, 20, 12)
            };

            Label lblTitle = new Label
            {
                Text = $"🗺️ Add Route Stop #{nextSequence}",
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 101, 52),
                Dock = DockStyle.Top,
                Height = 24
            };

            Label lblSubtitle = new Label
            {
                Text = "Attach a pending loading schedule or define a custom waypoint destination.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(21, 128, 61),
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

            Label lblPick = new Label
            {
                Text = "Select from Pending Loading Schedule (Optional):",
                Font = UITheme.FieldLabelFont,
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 22
            };

            cbSelectSchedule = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cbSelectSchedule.Items.Add("-- Manual / Custom Destination --");
            foreach (var s in availableSchedules)
            {
                cbSelectSchedule.Items.Add($"{s.ScheduleNumber} - {s.ClientName} ({s.Destination}) [{s.TotalCrates} Crates]");
            }
            cbSelectSchedule.SelectedIndex = 0;
            cbSelectSchedule.SelectedIndexChanged += CbSelectSchedule_SelectedIndexChanged;

            Label lblClient = new Label
            {
                Text = "Client / Store Name:",
                Font = UITheme.FieldLabelFont,
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(0, 6, 0, 0)
            };

            txtClientName = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F),
                MaxLength = 150
            };

            Label lblDest = new Label
            {
                Text = "Destination / Delivery Address:",
                Font = UITheme.FieldLabelFont,
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(0, 6, 0, 0)
            };

            txtDestination = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F),
                MaxLength = 200
            };

            TableLayoutPanel tblCrates = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 60,
                ColumnCount = 2,
                Margin = new Padding(0, 10, 0, 0)
            };
            tblCrates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblCrates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            Label lblG = new Label { Text = "G-Crates (Plastic):", Font = UITheme.FieldLabelFont, Dock = DockStyle.Fill };
            Label lblE = new Label { Text = "E-Crates (Wood):", Font = UITheme.FieldLabelFont, Dock = DockStyle.Fill };
            numGCrate = new NumericUpDown { Maximum = 10000, Value = 100, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };
            numECrate = new NumericUpDown { Maximum = 10000, Value = 0, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };

            tblCrates.Controls.Add(lblG, 0, 0);
            tblCrates.Controls.Add(lblE, 1, 0);
            tblCrates.Controls.Add(numGCrate, 0, 1);
            tblCrates.Controls.Add(numECrate, 1, 1);

            Label lblRem = new Label
            {
                Text = "Stop Remarks / Gate Instructions:",
                Font = UITheme.FieldLabelFont,
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(0, 6, 0, 0)
            };

            txtRemarks = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F),
                MaxLength = 500
            };

            pnlBody.Controls.Add(txtRemarks);
            pnlBody.Controls.Add(lblRem);
            pnlBody.Controls.Add(tblCrates);
            pnlBody.Controls.Add(txtDestination);
            pnlBody.Controls.Add(lblDest);
            pnlBody.Controls.Add(txtClientName);
            pnlBody.Controls.Add(lblClient);
            pnlBody.Controls.Add(cbSelectSchedule);
            pnlBody.Controls.Add(lblPick);

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
                Size = new Size(100, 36),
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
                Text = "➕ Add Stop to Route",
                Size = new Size(180, 36),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.SuccessGreen,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += (s, e) => ConfirmAdd(nextSequence);

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

        private void CbSelectSchedule_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cbSelectSchedule.SelectedIndex > 0 && cbSelectSchedule.SelectedIndex - 1 < availableSchedules.Count)
            {
                var s = availableSchedules[cbSelectSchedule.SelectedIndex - 1];
                txtClientName.Text = s.ClientName;
                txtDestination.Text = s.Destination;
                numGCrate.Value = Math.Max(0, s.GCrate);
                numECrate.Value = Math.Max(0, s.ECrate);
            }
        }

        private void ConfirmAdd(int sequence)
        {
            string client = txtClientName.Text.Trim();
            string dest = txtDestination.Text.Trim();
            int g = (int)numGCrate.Value;
            int ec = (int)numECrate.Value;
            int total = g + ec;

            if (string.IsNullOrWhiteSpace(client))
            {
                MessageBox.Show("Please enter the client or store name for this drop.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtClientName.Focus();
                return;
            }

            if (client.Length > 150)
            {
                MessageBox.Show("Client name cannot exceed 150 characters.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtClientName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(dest))
            {
                MessageBox.Show("Please enter the destination address for this drop.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDestination.Focus();
                return;
            }

            if (dest.Length > 200)
            {
                MessageBox.Show("Destination cannot exceed 200 characters.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDestination.Focus();
                return;
            }

            if (txtRemarks.Text.Trim().Length > 500)
            {
                MessageBox.Show("Remarks cannot exceed 500 characters.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRemarks.Focus();
                return;
            }

            if (total <= 0)
            {
                MessageBox.Show("Please enter at least 1 crate for this delivery stop.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numGCrate.Focus();
                return;
            }

            int? lsid = null;
            if (cbSelectSchedule.SelectedIndex > 0 && cbSelectSchedule.SelectedIndex - 1 < availableSchedules.Count)
            {
                lsid = availableSchedules[cbSelectSchedule.SelectedIndex - 1].Id;
            }

            createdStop = new DispatchStop
            {
                StopSequence = sequence,
                LoadingScheduleId = lsid,
                ClientName = client,
                Destination = dest,
                GCrate = g,
                ECrate = ec,
                TotalCrates = total,
                Remarks = txtRemarks.Text.Trim(),
                Status = "Pending"
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
