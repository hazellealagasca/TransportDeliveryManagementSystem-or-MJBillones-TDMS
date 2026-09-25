using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public class CancellationReasonDialog : Form
    {
        private TextBox txtReason = null!;
        private ComboBox cbPresetReason = null!;
        private Button btnConfirm = null!;
        private Button btnCancel = null!;
        private string cancellationReason = string.Empty;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CancellationReason => cancellationReason;

        public CancellationReasonDialog(string itemType = "Dispatch Order", string identifier = "")
        {
            InitializeComponent(itemType, identifier);
        }

        private void InitializeComponent(string itemType, string identifier)
        {
            this.Text = "Cancellation Reason Required";
            this.Size = new Size(560, 400);
            this.MinimumSize = new Size(480, 360);
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
                BackColor = Color.FromArgb(254, 242, 242),
                Padding = new Padding(18, 12, 18, 12)
            };

            Label lblTitle = new Label
            {
                Text = $"Cancel {itemType} {(string.IsNullOrEmpty(identifier) ? "" : $"#{identifier}")}",
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Dock = DockStyle.Top,
                Height = 24
            };

            Label lblSubtitle = new Label
            {
                Text = "Please provide the official reason for cancelling this record.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(127, 29, 29),
                Dock = DockStyle.Bottom,
                Height = 20
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            Panel pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16)
            };

            Label lblPreset = new Label
            {
                Text = "Common Cancellation Reasons:",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };

            cbPresetReason = new ComboBox
            {
                Dock = DockStyle.Top,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = UITheme.InputFont,
                Margin = new Padding(0, 0, 0, 14)
            };
            cbPresetReason.Items.AddRange(new object[]
            {
                "-- Select a standard reason or type custom details below --",
                "Client requested schedule cancellation",
                "Store closed / Unable to receive shipment",
                "Severe weather condition / Route impassable",
                "Mechanical vehicle failure / Emergency maintenance",
                "Driver / Crew medical emergency",
                "Cargo / Crate loading discrepancy",
                "Duplicate entry / Operator scheduling error"
            });
            cbPresetReason.SelectedIndex = 0;
            cbPresetReason.SelectedIndexChanged += (s, e) =>
            {
                if (cbPresetReason.SelectedIndex > 0)
                {
                    txtReason.Text = cbPresetReason.SelectedItem?.ToString() ?? "";
                }
            };

            Panel pnlDetailsSpacing = new Panel { Dock = DockStyle.Top, Height = 12 };

            Label lblDetails = new Label
            {
                Text = "Detailed Reason / Explanation (Mandatory):",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = UITheme.TextPrimary,
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };

            txtReason = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = UITheme.InputFont,
                MaxLength = 500
            };

            pnlContent.Controls.Add(txtReason);
            pnlContent.Controls.Add(lblDetails);
            pnlContent.Controls.Add(pnlDetailsSpacing);
            pnlContent.Controls.Add(cbPresetReason);
            pnlContent.Controls.Add(lblPreset);

            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = UITheme.SecondarySurface,
                Padding = new Padding(16, 10, 24, 10)
            };

            btnCancel = new Button
            {
                Text = "Back / Do Not Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 160,
                Height = 35,
                Dock = DockStyle.Right,
                BackColor = Color.White,
                ForeColor = UITheme.TextPrimary
            };

            Panel pnlSpacer = new Panel { Width = 12, Dock = DockStyle.Right };

            btnConfirm = new Button
            {
                Text = "Confirm Cancellation",
                DialogResult = DialogResult.OK,
                Width = 170,
                Height = 35,
                Dock = DockStyle.Right,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White
            };
            btnConfirm.Click += BtnConfirm_Click;

            pnlFooter.Controls.Add(btnConfirm);
            pnlFooter.Controls.Add(pnlSpacer);
            pnlFooter.Controls.Add(btnCancel);

            this.Controls.Add(pnlContent);
            pnlContent.BringToFront();
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);

            this.AcceptButton = btnConfirm;
            this.CancelButton = btnCancel;
        }

        private void BtnConfirm_Click(object? sender, EventArgs e)
        {
            string reason = txtReason.Text.Trim();
            if (string.IsNullOrWhiteSpace(reason) || reason.Length < 5)
            {
                MessageBox.Show("A valid cancellation reason (minimum 5 characters) is required before proceeding.", "Reason Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtReason.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }

            cancellationReason = reason;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public static string? PromptCancellationReason(IWin32Window owner, string itemType = "Dispatch Order", string identifier = "")
        {
            using (var dlg = new CancellationReasonDialog(itemType, identifier))
            {
                if (dlg.ShowDialog(owner) == DialogResult.OK)
                {
                    return dlg.CancellationReason;
                }
            }
            return null;
        }
    }
}
