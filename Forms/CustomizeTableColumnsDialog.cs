using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    public class CustomizeTableColumnsDialog : Form
    {
        private readonly DataGridView targetGrid;
        private readonly List<CheckBox> checkBoxes = new List<CheckBox>();

        public CustomizeTableColumnsDialog(DataGridView grid)
        {
            this.targetGrid = grid;
            InitializeCustomComponent();
            UIThemeHelper.ApplyTheme(this);
        }

        private void InitializeCustomComponent()
        {
            this.Text = "Customize Table Columns";
            this.Size = new Size(380, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Pal.White;
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Pal.C(15, 23, 42),
                Padding = new Padding(16, 0, 16, 0)
            };

            Label lblTitle = new Label
            {
                Text = "⚙️  Select Columns to Display",
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                ForeColor = Pal.White,
                AutoSize = true,
                Location = new Point(16, 15)
            };
            pnlHeader.Controls.Add(lblTitle);

            Panel pnlList = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 16, 20, 16),
                AutoScroll = true
            };

            int y = 10;
            foreach (DataGridViewColumn col in targetGrid.Columns)
            {
                if (string.IsNullOrEmpty(col.HeaderText)) continue;

                var chk = new CheckBox
                {
                    Text = col.HeaderText,
                    Checked = col.Visible,
                    Tag = col,
                    AutoSize = true,
                    Location = new Point(12, y),
                    Font = new Font("Segoe UI", 9.5F),
                    ForeColor = Pal.C(30, 41, 59)
                };
                checkBoxes.Add(chk);
                pnlList.Controls.Add(chk);
                y += 36;
            }

            Panel pnlActions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Pal.C(248, 250, 252),
                Padding = new Padding(0, 0, 16, 0)
            };

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(90, 34),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand,
                ForeColor = Pal.C(71, 85, 105),
                BackColor = Pal.White
            };
            btnCancel.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnCancel.Location = new Point(pnlActions.Width - 210, 10);
            btnCancel.Anchor = AnchorStyles.Right | AnchorStyles.Top;

            Button btnApply = new Button
            {
                Text = "✓ Apply",
                Size = new Size(100, 34),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                ForeColor = Pal.White,
                BackColor = Pal.C(15, 23, 42)
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Location = new Point(pnlActions.Width - 110, 10);
            btnApply.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnApply.Click += (s, e) =>
            {
                foreach (var chk in checkBoxes)
                {
                    if (chk.Tag is DataGridViewColumn col)
                    {
                        col.Visible = chk.Checked;
                    }
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            pnlActions.Controls.Add(btnCancel);
            pnlActions.Controls.Add(btnApply);

            this.Controls.Add(pnlList);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlActions);
            this.AcceptButton = btnApply;
            this.CancelButton = btnCancel;
        }
    }
}
