using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public class PhotoViewerDialog : Form
    {
        private PictureBox picViewer = null!;
        private Label lblCaption = null!;
        private Button btnClose = null!;

        public PhotoViewerDialog(string photoPath, string title = "Reference Photo Viewer")
        {
            InitializeComponent(title);
            LoadPhoto(photoPath);
        }

        private void InitializeComponent(string title)
        {
            this.Text = title;
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Pal.C(15, 23, 42);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(400, 300);
            this.MaximizeBox = true;
            this.MinimizeBox = false;

            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Pal.C(30, 41, 59),
                Padding = new Padding(15, 10, 15, 10)
            };

            lblCaption = new Label
            {
                Dock = DockStyle.Left,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Pal.White,
                AutoSize = true,
                Text = title
            };

            btnClose = new Button
            {
                Dock = DockStyle.Right,
                Text = "✖ Close",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Pal.White,
                BackColor = Pal.C(153, 27, 27),
                FlatStyle = FlatStyle.Flat,
                Width = 90,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            pnlTop.Controls.Add(lblCaption);
            pnlTop.Controls.Add(btnClose);

            picViewer = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Pal.C(15, 23, 42)
            };

            this.Controls.Add(picViewer);
            this.Controls.Add(pnlTop);
        }

        private void LoadPhoto(string photoPath)
        {
            try
            {
                string abs = SecurityHelper.GetAbsolutePhotoPath(photoPath);
                if (!string.IsNullOrEmpty(abs) && File.Exists(abs))
                {
                    SecurityHelper.SafeSetPictureBoxImage(picViewer, abs);
                    lblCaption.Text = $"{Path.GetFileName(abs)} ({new FileInfo(abs).Length / 1024} KB)";
                }
                else
                {
                    SecurityHelper.SafeSetPictureBoxImage(picViewer, null);
                    lblCaption.Text = "Photo not found or removed.";
                }
            }
            catch (Exception ex)
            {
                lblCaption.Text = $"Error loading photo: {ex.Message}";
            }
        }
    }
}
