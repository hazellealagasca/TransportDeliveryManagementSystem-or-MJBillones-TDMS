using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public class AnalogClockControl : Control
    {
        public enum ClockMode { Hours, Minutes }

        private ClockMode currentMode = ClockMode.Hours;
        private int selectedHour = 8;
        private int selectedMinute = 0;
        private bool isDragging = false;

        public event Action? TimeChanged;
        public event Action? HourSelected;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ClockMode Mode
        {
            get => currentMode;
            set
            {
                if (currentMode != value)
                {
                    currentMode = value;
                    Invalidate();
                }
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Hour
        {
            get => selectedHour;
            set
            {
                selectedHour = value < 1 ? 12 : (value > 12 ? 12 : value);
                Invalidate();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Minute
        {
            get => selectedMinute;
            set
            {
                selectedMinute = value < 0 ? 0 : (value > 59 ? 59 : value);
                Invalidate();
            }
        }

        public AnalogClockControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Size = new Size(260, 260);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int w = Width;
            int h = Height;
            int cx = w / 2;
            int cy = h / 2;
            int radius = Math.Min(cx, cy) - 15;

            using (SolidBrush faceBrush = new SolidBrush(Pal.C(248, 250, 252)))
            {
                g.FillEllipse(faceBrush, cx - radius, cy - radius, radius * 2, radius * 2);
            }
            using (Pen borderPen = new Pen(Pal.C(203, 213, 225), 2))
            {
                g.DrawEllipse(borderPen, cx - radius, cy - radius, radius * 2, radius * 2);
            }

            int handTargetX = cx;
            int handTargetY = cy;
            int markerRadius = 18;

            if (currentMode == ClockMode.Hours)
            {
                double angleDeg = (selectedHour % 12) * 30.0 - 90.0;
                double angleRad = angleDeg * Math.PI / 180.0;
                int numRadius = radius - 30;
                handTargetX = (int)(cx + numRadius * Math.Cos(angleRad));
                handTargetY = (int)(cy + numRadius * Math.Sin(angleRad));

                using (Pen handPen = new Pen(UITheme.PrimaryAccent, 3))
                {
                    g.DrawLine(handPen, cx, cy, handTargetX, handTargetY);
                }

                using (SolidBrush selBrush = new SolidBrush(UITheme.PrimaryAccent))
                {
                    g.FillEllipse(selBrush, handTargetX - markerRadius, handTargetY - markerRadius, markerRadius * 2, markerRadius * 2);
                }

                using (Font numFont = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold))
                {
                    for (int i = 1; i <= 12; i++)
                    {
                        double a = (i * 30.0 - 90.0) * Math.PI / 180.0;
                        int nx = (int)(cx + numRadius * Math.Cos(a));
                        int ny = (int)(cy + numRadius * Math.Sin(a));

                        string numStr = i.ToString();
                        SizeF strSize = g.MeasureString(numStr, numFont);
                        PointF strPos = new PointF(nx - strSize.Width / 2f, ny - strSize.Height / 2f);

                        Color textColor = (i == selectedHour) ? Pal.White : UITheme.TextPrimary;
                        using (SolidBrush textBrush = new SolidBrush(textColor))
                        {
                            g.DrawString(numStr, numFont, textBrush, strPos);
                        }
                    }
                }
            }
            else
            {
                double angleDeg = selectedMinute * 6.0 - 90.0;
                double angleRad = angleDeg * Math.PI / 180.0;
                int numRadius = radius - 30;
                handTargetX = (int)(cx + numRadius * Math.Cos(angleRad));
                handTargetY = (int)(cy + numRadius * Math.Sin(angleRad));

                using (Pen handPen = new Pen(UITheme.PrimaryAccent, 2.5f))
                {
                    g.DrawLine(handPen, cx, cy, handTargetX, handTargetY);
                }

                using (SolidBrush selBrush = new SolidBrush(UITheme.PrimaryAccent))
                {
                    g.FillEllipse(selBrush, handTargetX - markerRadius, handTargetY - markerRadius, markerRadius * 2, markerRadius * 2);
                }

                using (Font numFont = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold))
                {
                    for (int m = 0; m < 60; m += 5)
                    {
                        double a = (m * 6.0 - 90.0) * Math.PI / 180.0;
                        int nx = (int)(cx + numRadius * Math.Cos(a));
                        int ny = (int)(cy + numRadius * Math.Sin(a));

                        string numStr = m.ToString("00");
                        SizeF strSize = g.MeasureString(numStr, numFont);
                        PointF strPos = new PointF(nx - strSize.Width / 2f, ny - strSize.Height / 2f);

                        Color textColor = (m == selectedMinute || (selectedMinute == 0 && m == 0)) ? Pal.White : UITheme.TextPrimary;
                        using (SolidBrush textBrush = new SolidBrush(textColor))
                        {
                            g.DrawString(numStr, numFont, textBrush, strPos);
                        }
                    }
                }

                using (SolidBrush dotBrush = new SolidBrush(Pal.C(148, 163, 184)))
                {
                    for (int m = 0; m < 60; m++)
                    {
                        if (m % 5 != 0)
                        {
                            double a = (m * 6.0 - 90.0) * Math.PI / 180.0;
                            int nx = (int)(cx + numRadius * Math.Cos(a));
                            int ny = (int)(cy + numRadius * Math.Sin(a));
                            g.FillEllipse(dotBrush, nx - 2, ny - 2, 4, 4);
                        }
                    }
                }
            }

            using (SolidBrush centerBrush = new SolidBrush(UITheme.PrimaryAccent))
            {
                g.FillEllipse(centerBrush, cx - 6, cy - 6, 12, 12);
            }
            using (SolidBrush hubInner = new SolidBrush(Pal.White))
            {
                g.FillEllipse(hubInner, cx - 2, cy - 2, 4, 4);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                UpdateFromPoint(e.X, e.Y, isFinalClick: false);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (isDragging)
            {
                UpdateFromPoint(e.X, e.Y, isFinalClick: false);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (isDragging)
            {
                isDragging = false;
                UpdateFromPoint(e.X, e.Y, isFinalClick: true);
            }
        }

        private void UpdateFromPoint(int x, int y, bool isFinalClick)
        {
            int cx = Width / 2;
            int cy = Height / 2;
            double dx = x - cx;
            double dy = y - cy;

            double angleDeg = Math.Atan2(dy, dx) * 180.0 / Math.PI + 90.0;
            if (angleDeg < 0) angleDeg += 360.0;

            if (currentMode == ClockMode.Hours)
            {
                int hour = (int)Math.Round(angleDeg / 30.0);
                if (hour == 0) hour = 12;
                if (hour > 12) hour = 12;
                selectedHour = hour;
                Invalidate();
                TimeChanged?.Invoke();

                if (isFinalClick)
                {
                    HourSelected?.Invoke();
                }
            }
            else
            {
                int min = (int)Math.Round(angleDeg / 6.0) % 60;
                selectedMinute = min;
                Invalidate();
                TimeChanged?.Invoke();
            }
        }
    }

    public class AnalogClockTimePickerDialog : Form
    {
        private AnalogClockControl clockControl = null!;
        private Label lblHourDisplay = null!;
        private Label lblMinuteDisplay = null!;
        private Label lblColon = null!;
        private Button btnAm = null!;
        private Button btnPm = null!;
        private bool isAm = true;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedTimeString
        {
            get
            {
                string amPm = isAm ? "AM" : "PM";
                return $"{clockControl.Hour:D2}:{clockControl.Minute:D2} {amPm}";
            }
            set
            {
                ParseTimeString(value);
            }
        }

        public AnalogClockTimePickerDialog(string initialTime = "08:00 AM")
        {
            InitializeComponent();
            ParseTimeString(initialTime);
            UpdateDisplayHeader();
        }

        private void ParseTimeString(string timeStr)
        {
            if (DateTime.TryParse(timeStr, out DateTime dt))
            {
                int h = dt.Hour;
                isAm = h < 12;
                if (h == 0) h = 12;
                else if (h > 12) h -= 12;

                clockControl.Hour = h;
                clockControl.Minute = dt.Minute;
            }
            else
            {
                clockControl.Hour = 8;
                clockControl.Minute = 0;
                isAm = true;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Select Departure Time";
            this.Size = new Size(340, 490);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Pal.C(248, 250, 252);
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = UITheme.PrimaryAccent,
                Padding = new Padding(15, 10, 15, 10)
            };

            lblHourDisplay = new Label
            {
                Text = "08",
                Font = new Font("Segoe UI", 32F, FontStyle.Bold),
                ForeColor = Pal.White,
                AutoSize = true,
                Location = new Point(35, 12),
                Cursor = Cursors.Hand
            };
            lblHourDisplay.Click += (s, e) => SwitchMode(AnalogClockControl.ClockMode.Hours);

            lblColon = new Label
            {
                Text = ":",
                Font = new Font("Segoe UI", 32F, FontStyle.Bold),
                ForeColor = Pal.C(219, 234, 254),
                AutoSize = true,
                Location = new Point(100, 10)
            };

            lblMinuteDisplay = new Label
            {
                Text = "00",
                Font = new Font("Segoe UI", 32F, FontStyle.Bold),
                ForeColor = Pal.C(191, 219, 254),
                AutoSize = true,
                Location = new Point(125, 12),
                Cursor = Cursors.Hand
            };
            lblMinuteDisplay.Click += (s, e) => SwitchMode(AnalogClockControl.ClockMode.Minutes);

            btnAm = new Button
            {
                Text = "AM",
                Location = new Point(220, 16),
                Size = new Size(55, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAm.FlatAppearance.BorderSize = 0;
            btnAm.Click += (s, e) => { isAm = true; UpdateAmPmButtons(); };

            btnPm = new Button
            {
                Text = "PM",
                Location = new Point(220, 48),
                Size = new Size(55, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPm.FlatAppearance.BorderSize = 0;
            btnPm.Click += (s, e) => { isAm = false; UpdateAmPmButtons(); };

            pnlHeader.Controls.Add(lblHourDisplay);
            pnlHeader.Controls.Add(lblColon);
            pnlHeader.Controls.Add(lblMinuteDisplay);
            pnlHeader.Controls.Add(btnAm);
            pnlHeader.Controls.Add(btnPm);

            clockControl = new AnalogClockControl
            {
                Location = new Point(35, 100),
                Size = new Size(255, 255)
            };
            clockControl.TimeChanged += UpdateDisplayHeader;
            clockControl.HourSelected += () =>
            {

                SwitchMode(AnalogClockControl.ClockMode.Minutes);
            };

            FlowLayoutPanel pnlPresets = new FlowLayoutPanel
            {
                Location = new Point(15, 362),
                Size = new Size(295, 36),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };

            string[] presets = { "06:00 AM", "08:00 AM", "10:00 AM", "01:00 PM", "04:00 PM", "08:00 PM" };
            foreach (var pr in presets)
            {
                Button btnPreset = new Button
                {
                    Text = pr,
                    Size = new Size(68, 26),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Pal.C(241, 245, 249),
                    ForeColor = UITheme.TextPrimary,
                    Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                    Margin = new Padding(2),
                    Cursor = Cursors.Hand
                };
                btnPreset.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
                {
                    ParseTimeString(pr);
                    UpdateDisplayHeader();
                };
                pnlPresets.Controls.Add(btnPreset);
            }

            Button btnCancel = new Button
            {
                Text = "CANCEL",
                DialogResult = DialogResult.Cancel,
                Location = new Point(85, 405),
                Size = new Size(105, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Pal.C(241, 245, 249),
                ForeColor = UITheme.TextSecondary,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            Button btnOk = new Button
            {
                Text = "SET TIME",
                DialogResult = DialogResult.OK,
                Location = new Point(200, 405),
                Size = new Size(110, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.PrimaryAccent,
                ForeColor = Pal.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;

            this.Controls.Add(pnlHeader);
            this.Controls.Add(clockControl);
            this.Controls.Add(pnlPresets);
            this.Controls.Add(btnCancel);
            this.Controls.Add(btnOk);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void SwitchMode(AnalogClockControl.ClockMode mode)
        {
            clockControl.Mode = mode;
            if (mode == AnalogClockControl.ClockMode.Hours)
            {
                lblHourDisplay.ForeColor = Pal.White;
                lblMinuteDisplay.ForeColor = Pal.C(191, 219, 254);
            }
            else
            {
                lblHourDisplay.ForeColor = Pal.C(191, 219, 254);
                lblMinuteDisplay.ForeColor = Pal.White;
            }
        }

        private void UpdateDisplayHeader()
        {
            lblHourDisplay.Text = clockControl.Hour.ToString("D2");
            lblMinuteDisplay.Text = clockControl.Minute.ToString("D2");
            UpdateAmPmButtons();
        }

        private void UpdateAmPmButtons()
        {
            if (isAm)
            {
                btnAm.BackColor = Pal.White;
                btnAm.ForeColor = UITheme.PrimaryAccent;
                btnPm.BackColor = Pal.C(30, 64, 175);
                btnPm.ForeColor = Pal.C(224, 231, 255);
            }
            else
            {
                btnPm.BackColor = Pal.White;
                btnPm.ForeColor = UITheme.PrimaryAccent;
                btnAm.BackColor = Pal.C(30, 64, 175);
                btnAm.ForeColor = Pal.C(224, 231, 255);
            }
        }

        public static string? ShowTimePicker(IWin32Window? owner, string currentTime = "08:00 AM")
        {
            using (var dlg = new AnalogClockTimePickerDialog(currentTime))
            {
                if (dlg.ShowDialog(owner) == DialogResult.OK)
                {
                    return dlg.SelectedTimeString;
                }
            }
            return null;
        }
    }
}
