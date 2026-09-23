using System;
using System.Drawing;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    public static class UITheme
    {
        public static readonly Font TitleFont          = new Font("Segoe UI", 18F, FontStyle.Bold);
        public static readonly Font CardHeaderFont     = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
        public static readonly Font FieldLabelFont     = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
        public static readonly Font InputFont          = new Font("Segoe UI", 11F, FontStyle.Regular);
        public static readonly Font ButtonPrimaryFont   = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
        public static readonly Font ButtonSecondaryFont = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        public static readonly Font GridCellFont       = new Font("Segoe UI", 10.5F, FontStyle.Regular);
        public static readonly Font GridHeaderFont     = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
        public static readonly Font HelperSubtextFont  = new Font("Segoe UI", 9.5F, FontStyle.Regular);

        public static readonly Color CanvasBackground   = Color.FromArgb(248, 250, 252);
        public static readonly Color SurfaceBackground  = Color.FromArgb(255, 255, 255);
        public static readonly Color TextPrimary        = Color.FromArgb(15, 23, 42);
        public static readonly Color TextSecondary      = Color.FromArgb(51, 65, 85);
        public static readonly Color TextMuted          = Color.FromArgb(100, 116, 139);

        public static readonly Color PrimaryAccent      = Color.FromArgb(37, 99, 235);
        public static readonly Color PrimaryHover       = Color.FromArgb(29, 78, 216);
        public static readonly Color PrimaryActive      = Color.FromArgb(30, 64, 175);
        public static readonly Color BrandDark          = Color.FromArgb(30, 41, 59);

        public static readonly Color SecondarySurface   = Color.FromArgb(241, 245, 249);
        public static readonly Color BorderDefault      = Color.FromArgb(203, 213, 225);
        public static readonly Color BorderSubtle       = Color.FromArgb(226, 232, 240);
        public static readonly Color SuccessGreen       = Color.FromArgb(5, 150, 105);
        public static readonly Color SuccessHover       = Color.FromArgb(4, 120, 87);
        public static readonly Color DangerRed          = Color.FromArgb(220, 38, 38);
        public static readonly Color DangerHover        = Color.FromArgb(185, 28, 28);
        public static readonly Color SelectionHighlight = Color.FromArgb(238, 242, 255);
        public static readonly Color SelectionText      = Color.FromArgb(30, 27, 75);
    }

    public enum ToastType { Success, Info, Warning, Error }

    public static class ToastHelper
    {
        public static void ShowToast(Control parent, string message, ToastType type = ToastType.Success)
        {
            try
            {
                Form? parentForm = parent is Form f ? f : parent.FindForm();
                if (parentForm == null || parentForm.IsDisposed || !parentForm.Visible) return;

                Color bg = Color.FromArgb(15, 23, 42);
                Color fg = Color.White;
                string icon = "✓";

                switch (type)
                {
                    case ToastType.Success:
                        bg = Color.FromArgb(22, 101, 52);
                        icon = "✓";
                        break;
                    case ToastType.Info:
                        bg = Color.FromArgb(30, 64, 175);
                        icon = "ℹ";
                        break;
                    case ToastType.Warning:
                        bg = Color.FromArgb(194, 65, 12);
                        icon = "⚠️";
                        break;
                    case ToastType.Error:
                        bg = Color.FromArgb(185, 28, 28);
                        icon = "✕";
                        break;
                }

                Panel toast = new Panel
                {
                    AutoSize = true,
                    BackColor = bg,
                    Padding = new Padding(16, 10, 18, 10),
                    Cursor = Cursors.Hand
                };

                Label lbl = new Label
                {
                    Text = $"{icon}  {message}",
                    ForeColor = fg,
                    Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    Cursor = Cursors.Hand
                };
                toast.Controls.Add(lbl);

                toast.Location = new Point(Math.Max(10, parentForm.ClientSize.Width - 360), Math.Max(10, parentForm.ClientSize.Height - 68));
                toast.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

                parentForm.Controls.Add(toast);
                toast.BringToFront();

                System.Windows.Forms.Timer tmr = new System.Windows.Forms.Timer { Interval = 3200 };
                Action dismiss = () =>
                {
                    try
                    {
                        tmr.Stop();
                        tmr.Dispose();
                        if (!parentForm.IsDisposed && parentForm.Controls.Contains(toast))
                        {
                            parentForm.Controls.Remove(toast);
                            toast.Dispose();
                        }
                    }
                    catch { }
                };

                toast.Click += (s, e) => dismiss();
                lbl.Click += (s, e) => dismiss();

                tmr.Tick += (s, e) => dismiss();
                tmr.Start();
            }
            catch { }
        }
    }

    public static class UIThemeHelper
    {

        public static void EnableDoubleBuffering(Control c)
        {
            if (c == null) return;
            try
            {
                var prop = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                prop?.SetValue(c, true, null);
            }
            catch { }

            foreach (Control child in c.Controls)
            {
                EnableDoubleBuffering(child);
            }
        }

        public static void ApplyTheme(Control parent)
        {
            if (parent == null) return;
            EnableDoubleBuffering(parent);

            if (parent is Form || parent is UserControl)
            {
                parent.BackColor = UITheme.CanvasBackground;
            }

            if (parent is UserControl uc)
            {
                uc.AutoScroll = true;
                if (uc.AutoScrollMinSize.Width < 1000 || uc.AutoScrollMinSize.Height < 620)
                {
                    uc.AutoScrollMinSize = new Size(1000, 620);
                }
            }

            foreach (Control ctrl in parent.Controls)
            {
                ApplyControlStyle(ctrl);

                if (ctrl.HasChildren)
                {
                    ApplyTheme(ctrl);
                }
            }
        }

        private static void ApplyControlStyle(Control ctrl)
        {
                        if (ctrl is TextBox txt)
            {
                txt.Font = UITheme.InputFont;
                txt.ForeColor = UITheme.TextPrimary;
                txt.BackColor = Color.White;
                txt.BorderStyle = BorderStyle.FixedSingle;
                txt.Enter += (s, e) => { txt.BackColor = Color.FromArgb(240, 249, 255); };
                txt.Leave += (s, e) => { txt.BackColor = Color.White; };
            }
            else if (ctrl is ComboBox cb)
            {
                cb.Font = UITheme.InputFont;
                cb.ForeColor = UITheme.TextPrimary;
                cb.BackColor = Color.White;
                cb.FlatStyle = FlatStyle.Flat;

                if (cb.Name.Contains("Time", StringComparison.OrdinalIgnoreCase) && (cb.Items.Count == 0 || cb.Items.Count <= 7))
                {
                    string currentVal = cb.Text;
                    PopulateTimeSlots(cb, 30, false, string.IsNullOrEmpty(currentVal) ? "06:00 AM" : currentVal);
                }
            }
            else if (ctrl is DateTimePicker dtp)
            {
                if (dtp.Name.Contains("Time", StringComparison.OrdinalIgnoreCase))
                {
                    ConfigureTimePicker(dtp, false);
                }
                else
                {
                    dtp.Font = UITheme.InputFont;
                    dtp.CalendarFont = UITheme.InputFont;
                    dtp.CalendarForeColor = UITheme.TextPrimary;
                    dtp.CalendarMonthBackground = Color.White;
                }
            }
            else if (ctrl is NumericUpDown num)
            {
                num.Font = UITheme.InputFont;
                num.ForeColor = UITheme.TextPrimary;
                num.BackColor = Color.White;
                num.BorderStyle = BorderStyle.FixedSingle;
                if (num.Name.Contains("Amount") || num.Name.Contains("Paid") || num.Name.Contains("Cost") || num.Name.Contains("Wage") || num.Name.Contains("Allowance") || num.Name.Contains("Rate") || num.Name.Contains("Deduction"))
                {
                    if (num.Maximum < 100000000m) num.Maximum = 100000000m;
                }
                else
                {
                    if (num.Maximum < 1000000m) num.Maximum = 1000000m;
                }
            }
            else if (ctrl is CheckBox chk)
            {
                chk.Font = UITheme.FieldLabelFont;
                chk.ForeColor = UITheme.TextSecondary;
                chk.Cursor = Cursors.Hand;
                chk.AutoSize = true;
            }
            else if (ctrl is RadioButton rb)
            {
                rb.Font = UITheme.FieldLabelFont;
                rb.ForeColor = UITheme.TextSecondary;
                rb.Cursor = Cursors.Hand;
                rb.AutoSize = true;
            }
            else if (ctrl is Button btn)
            {
                ApplyButtonStyle(btn);
            }
            else if (ctrl is Label lbl)
            {
                ApplyLabelStyle(lbl);
            }
            else if (ctrl is DataGridView grid)
            {
                ApplyGridTheme(grid);
            }
            else if (ctrl is CheckedListBox clb)
            {
                clb.Font = UITheme.InputFont;
                clb.ForeColor = UITheme.TextPrimary;
                clb.BackColor = Color.White;
                clb.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (ctrl is Panel pnl)
            {
                if (pnl.Name.StartsWith("pnlForm", StringComparison.OrdinalIgnoreCase) ||
                    pnl.Name.StartsWith("pnlGridCard", StringComparison.OrdinalIgnoreCase) ||
                    pnl.Name.StartsWith("pnlCard", StringComparison.OrdinalIgnoreCase) ||
                    pnl.Name.StartsWith("pnlDispatchCard", StringComparison.OrdinalIgnoreCase) ||
                    pnl.Name.StartsWith("pnlLoadingCard", StringComparison.OrdinalIgnoreCase) ||
                    pnl.Name.StartsWith("pnlFilter", StringComparison.OrdinalIgnoreCase))
                {
                    pnl.BackColor = UITheme.SurfaceBackground;
                }
            }
            else if (ctrl is GroupBox gb)
            {
                gb.Font = UITheme.CardHeaderFont;
                gb.ForeColor = UITheme.TextPrimary;
                gb.BackColor = UITheme.SurfaceBackground;
            }
        }

        private static void ApplyLabelStyle(Label lbl)
        {
            string name = lbl.Name;
            lbl.AutoSize = true;

            if (name.StartsWith("lblTitle", StringComparison.OrdinalIgnoreCase) ||
                (lbl.Font.Size >= 15 && name.Contains("Title", StringComparison.OrdinalIgnoreCase)))
            {
                lbl.Font = UITheme.TitleFont;
                lbl.ForeColor = UITheme.TextPrimary;
            }
            else if (name.StartsWith("lblHeader", StringComparison.OrdinalIgnoreCase) ||
                     name.EndsWith("Header", StringComparison.OrdinalIgnoreCase) ||
                     name.StartsWith("lblFormHeader", StringComparison.OrdinalIgnoreCase) ||
                     name.StartsWith("lblGridHeader", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Title", StringComparison.OrdinalIgnoreCase))
            {
                lbl.Font = UITheme.CardHeaderFont;
                lbl.ForeColor = UITheme.TextPrimary;
            }
            else if (name.StartsWith("lblSub", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Desc", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Note", StringComparison.OrdinalIgnoreCase))
            {
                lbl.Font = UITheme.HelperSubtextFont;
                lbl.ForeColor = UITheme.TextMuted;
            }
            else if (name.StartsWith("lblError", StringComparison.OrdinalIgnoreCase))
            {
                lbl.Font = UITheme.HelperSubtextFont;
                lbl.ForeColor = UITheme.DangerRed;
            }
            else
            {
                lbl.Font = UITheme.FieldLabelFont;
                lbl.ForeColor = UITheme.TextSecondary;
            }
        }

        private static void ApplyButtonStyle(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = Cursors.Hand;

            if (btn.Name.Contains("Photo", StringComparison.OrdinalIgnoreCase) || btn.Name.Contains("TimePicker", StringComparison.OrdinalIgnoreCase))
            {
                btn.Font = UITheme.ButtonSecondaryFont;
                if (btn.Height < 32) btn.Height = 34;
                return;
            }

            btn.Font = UITheme.ButtonPrimaryFont;
            if (btn.Height < 36)
            {
                btn.Height = 38;
            }

            string text = btn.Text.ToUpperInvariant();
            string name = btn.Name.ToUpperInvariant();

            if (text.Contains("ADD") || text.Contains("SAVE") || text.Contains("FILE") || name.Contains("SAVE") || name.Contains("ADD"))
            {
                btn.BackColor = UITheme.PrimaryAccent;
                btn.ForeColor = Color.White;
                btn.FlatAppearance.MouseOverBackColor = UITheme.PrimaryHover;
                btn.FlatAppearance.MouseDownBackColor = UITheme.PrimaryActive;
            }
            else if (text.Contains("UPDATE") || text.Contains("EDIT") || name.Contains("UPDATE"))
            {
                btn.BackColor = UITheme.SuccessGreen;
                btn.ForeColor = Color.White;
                btn.FlatAppearance.MouseOverBackColor = UITheme.SuccessHover;
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(6, 95, 70);
            }
            else if (text.Contains("DELETE") || text.Contains("REMOVE") || name.Contains("DELETE"))
            {
                btn.BackColor = UITheme.DangerRed;
                btn.ForeColor = Color.White;
                btn.FlatAppearance.MouseOverBackColor = UITheme.DangerHover;
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(153, 27, 27);
            }
            else if (text.Contains("CLEAR") || text.Contains("CANCEL") || text.Contains("EXIT") || name.Contains("CLEAR") || name.Contains("EXIT"))
            {
                btn.BackColor = UITheme.SecondarySurface;
                btn.ForeColor = UITheme.TextSecondary;
                btn.FlatAppearance.MouseOverBackColor = UITheme.BorderDefault;
                btn.FlatAppearance.MouseDownBackColor = UITheme.BorderSubtle;
            }
            else if (text.Contains("PRINT") || name.Contains("PRINT") || text.Contains("EXPORT"))
            {
                btn.BackColor = Color.FromArgb(51, 65, 85);
                btn.ForeColor = Color.White;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 41, 59);
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(15, 23, 42);
            }
        }

                public static void ApplyGridTheme(DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            try
            {
                typeof(DataGridView).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                    null, grid, new object[] { true });
            }
            catch { }
            grid.BackgroundColor = UITheme.SurfaceBackground;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = UITheme.BorderSubtle;
            grid.ShowCellToolTips = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.RowHeadersVisible = false;
            grid.ScrollBars = ScrollBars.Both;
            grid.AllowUserToResizeColumns = true;

            if (grid.AutoSizeColumnsMode != DataGridViewAutoSizeColumnsMode.Fill)
            {
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                if (grid.Columns.Count > 0)
                {
                    DataGridViewColumn? primaryCol = null;
                    foreach (DataGridViewColumn col in grid.Columns)
                    {
                        string h = col.HeaderText?.ToLowerInvariant() ?? "";
                        string p = col.DataPropertyName?.ToLowerInvariant() ?? "";
                        if (h.Contains("destination") || p.Contains("destination") ||
                            h.Contains("client") || p.Contains("client") ||
                            h.Contains("remarks") || p.Contains("remarks") ||
                            h.Contains("fullname") || p.Contains("fullname") ||
                            (h.Contains("name") && !h.Contains("user")) ||
                            h.Contains("cause") || p.Contains("cause") ||
                            h.Contains("route") || p.Contains("route"))
                        {
                            primaryCol = col;
                            break;
                        }
                    }

                    if (primaryCol != null)
                    {
                        if (primaryCol.MinimumWidth < 150) primaryCol.MinimumWidth = 150;
                        primaryCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    }
                }
            }

            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            grid.ColumnHeadersHeight = 44;
            grid.RowTemplate.Height = 38;

            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 10, 0);

            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            grid.DefaultCellStyle.ForeColor = UITheme.TextPrimary;
            grid.DefaultCellStyle.BackColor = UITheme.SurfaceBackground;
            grid.DefaultCellStyle.SelectionBackColor = UITheme.SelectionHighlight;
            grid.DefaultCellStyle.SelectionForeColor = UITheme.SelectionText;
            grid.DefaultCellStyle.Padding = new Padding(10, 4, 10, 4);

            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            grid.AlternatingRowsDefaultCellStyle.ForeColor = UITheme.TextPrimary;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = UITheme.SelectionHighlight;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = UITheme.SelectionText;
            grid.AlternatingRowsDefaultCellStyle.Padding = new Padding(10, 4, 10, 4);

            grid.SortCompare += (s, e) =>
            {
                if (e.Column.HeaderText.Contains("Time", StringComparison.OrdinalIgnoreCase) ||
                    e.Column.DataPropertyName.Contains("Time", StringComparison.OrdinalIgnoreCase))
                {
                    string? v1 = e.CellValue1?.ToString();
                    string? v2 = e.CellValue2?.ToString();
                    if (DateTime.TryParse(v1, out DateTime dt1) && DateTime.TryParse(v2, out DateTime dt2))
                    {
                        e.SortResult = dt1.TimeOfDay.CompareTo(dt2.TimeOfDay);
                        e.Handled = true;
                    }
                }
            };

            grid.CellPainting += (s, e) =>
            {
                if (e.RowIndex == -1 && e.ColumnIndex >= 0 && e.Graphics != null)
                {
                    e.PaintBackground(e.CellBounds, true);
                    using (var headerBgBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    {
                        e.Graphics.FillRectangle(headerBgBrush, e.CellBounds);
                    }
                    using (var borderPen = new Pen(Color.FromArgb(51, 65, 85), 1f))
                    {
                        e.Graphics.DrawLine(borderPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                        e.Graphics.DrawLine(borderPen, e.CellBounds.Right - 1, e.CellBounds.Top + 6, e.CellBounds.Right - 1, e.CellBounds.Bottom - 6);
                    }
                    string headerText = grid.Columns[e.ColumnIndex].HeaderText;
                    using (var headerFont = new Font("Segoe UI Semibold", 10F, FontStyle.Bold))
                    using (var textBrush = new SolidBrush(Color.White))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                    {
                        var textRect = new Rectangle(e.CellBounds.X + 10, e.CellBounds.Y, e.CellBounds.Width - 16, e.CellBounds.Height);
                        e.Graphics.DrawString(headerText, headerFont, textBrush, textRect, sf);
                    }
                    e.Handled = true;
                    return;
                }

                                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.Value != null && e.Graphics != null)
                {
                    string colHeader = grid.Columns[e.ColumnIndex]?.HeaderText ?? "";
                    string colProp = grid.Columns[e.ColumnIndex]?.DataPropertyName ?? "";
                    if (colHeader.Contains("Status", StringComparison.OrdinalIgnoreCase) ||
                        colProp.Contains("Status", StringComparison.OrdinalIgnoreCase) ||
                        colHeader.Contains("Availability", StringComparison.OrdinalIgnoreCase) ||
                        colProp.Contains("Availability", StringComparison.OrdinalIgnoreCase) ||
                        colHeader.Contains("Active", StringComparison.OrdinalIgnoreCase) ||
                        colProp.Contains("IsActive", StringComparison.OrdinalIgnoreCase))
                    {
                        string val = e.Value.ToString() ?? "";
                        if (e.Value is bool b)
                        {
                            val = b ? "Active" : "Inactive";
                            if (colHeader.Contains("Avail", StringComparison.OrdinalIgnoreCase))
                            {
                                val = b ? "Active" : "On Trip";
                            }
                        }

                        Color bg = Color.FromArgb(241, 245, 249);
                        Color fg = Color.FromArgb(71, 85, 105);
                        Color border = Color.FromArgb(203, 213, 225);

                        string valUpper = val.ToUpperInvariant();

                        if (valUpper == "AVAILABLE" || valUpper.Contains("READY") || valUpper.Contains("COMPLETED") ||
                            valUpper.Contains("PAID") || valUpper.Contains("RESOLVED") || valUpper == "ACTIVE" ||
                            valUpper.Contains("DELIVERED") || valUpper == "TRUE" || valUpper == "YES")
                        {
                            bg = Color.FromArgb(220, 252, 231);
                            fg = Color.FromArgb(22, 101, 52);
                            border = Color.FromArgb(134, 239, 172);
                        }
                        else if (valUpper.Contains("ON TRIP") || valUpper.Contains("TRIP") || valUpper.Contains("TRANSIT") ||
                                 valUpper.Contains("DISPATCH") || valUpper.Contains("ASSIGNED") || valUpper.Contains("IN USE") ||
                                 valUpper.Contains("PENDING") || valUpper.Contains("IN PROGRESS") || valUpper.Contains("PARTIAL"))
                        {
                            bg = Color.FromArgb(255, 237, 213);
                            fg = Color.FromArgb(194, 65, 12);
                            border = Color.FromArgb(253, 186, 116);
                        }
                        else if (valUpper.Contains("MAINTENANCE") || valUpper.Contains("REPAIR") || valUpper.Contains("UNAVAILABLE") ||
                                 valUpper.Contains("NOT AVAILABLE") || valUpper.Contains("CANCEL") || valUpper.Contains("OVERDUE") ||
                                 valUpper.Contains("UNPAID") || valUpper == "OPEN" || valUpper.Contains("INACTIVE"))
                        {
                            bg = Color.FromArgb(254, 226, 226);
                            fg = Color.FromArgb(185, 28, 28);
                            border = Color.FromArgb(252, 165, 165);
                        }
                        else
                        {
                            bg = Color.FromArgb(241, 245, 249);
                            fg = Color.FromArgb(71, 85, 105);
                            border = Color.FromArgb(203, 213, 225);
                        }

                        e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);

                        int pillHeight = 26;
                        int pillWidth = Math.Min(e.CellBounds.Width - 12, Math.Max(80, (int)e.Graphics.MeasureString(val, UITheme.GridCellFont).Width + 24));
                        int pillX = e.CellBounds.X + (e.CellBounds.Width - pillWidth) / 2;
                        int pillY = e.CellBounds.Y + (e.CellBounds.Height - pillHeight) / 2;

                        var rect = new Rectangle(pillX, pillY, pillWidth, pillHeight);
                        using (var brush = new SolidBrush(bg))
                        using (var pen = new Pen(border, 1.2f))
                        using (var textBrush = new SolidBrush(fg))
                        using (var pillFont = new Font("Segoe UI Semibold", 9F, FontStyle.Bold))
                        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            e.Graphics.FillRectangle(brush, rect);
                            e.Graphics.DrawRectangle(pen, rect);
                            e.Graphics.DrawString(val, pillFont, textBrush, rect, sf);
                        }
                        e.Handled = true;
                    }
                }
            };
        }

        public static void ConfigureTimePicker(DateTimePicker picker, bool use24Hour = false)
        {
            if (picker == null) return;
            picker.Format = DateTimePickerFormat.Custom;
            picker.CustomFormat = use24Hour ? "HH:mm" : "hh:mm tt";
            picker.ShowUpDown = true;
            picker.Font = UITheme.InputFont;
            picker.CalendarFont = UITheme.InputFont;
            picker.ForeColor = UITheme.TextPrimary;
            picker.BackColor = Color.White;
        }

        public static void PopulateTimeSlots(ComboBox combo, int intervalMinutes = 30, bool use24Hour = false, string defaultSelection = "06:00 AM")
        {
            if (combo == null) return;
            if (intervalMinutes <= 0) intervalMinutes = 30;

            combo.Items.Clear();
            combo.Font = UITheme.InputFont;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.ForeColor = UITheme.TextPrimary;
            combo.BackColor = Color.White;
            combo.FlatStyle = FlatStyle.Flat;

            DateTime start = DateTime.Today;
            DateTime end = start.AddDays(1);

            while (start < end)
            {
                string timeStr = use24Hour ? start.ToString("HH:mm") : start.ToString("hh:mm tt");
                combo.Items.Add(timeStr);
                start = start.AddMinutes(intervalMinutes);
            }

            if (!string.IsNullOrEmpty(defaultSelection))
            {
                int idx = combo.Items.IndexOf(defaultSelection);
                if (idx >= 0) combo.SelectedIndex = idx;
                else if (combo.Items.Count > 0) combo.SelectedIndex = 0;
            }
            else if (combo.Items.Count > 0)
            {
                combo.SelectedIndex = 0;
            }
        }

        public static string? ShowAnalogClockPicker(IWin32Window? owner, string initialTime = "08:00 AM")
        {
            using (var dlg = new Views.AnalogClockTimePickerDialog(initialTime))
            {
                if (dlg.ShowDialog(owner) == DialogResult.OK)
                {
                    return dlg.SelectedTimeString;
                }
            }
            return null;
        }

        public static void AttachAnalogClockPicker(Button button, ComboBox targetInput)
        {
            if (button == null || targetInput == null) return;
            button.Click += (s, e) =>
            {
                string currentTime = targetInput.Text;
                string? chosen = ShowAnalogClockPicker(button.FindForm(), string.IsNullOrEmpty(currentTime) ? "08:00 AM" : currentTime);
                if (!string.IsNullOrEmpty(chosen))
                {
                    if (!targetInput.Items.Contains(chosen))
                    {
                        targetInput.Items.Add(chosen);
                    }
                    targetInput.SelectedItem = chosen;
                    targetInput.Text = chosen;
                }
            };
        }

        public static void ApplyLoginTheme(Form loginForm, Panel cardPanel, Button loginBtn)
        {
            loginForm.BackColor = UITheme.CanvasBackground;
            if (cardPanel != null)
            {
                cardPanel.BackColor = UITheme.SurfaceBackground;
            }
            if (loginBtn != null)
            {
                loginBtn.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
                loginBtn.Height = 44;
                loginBtn.BackColor = UITheme.PrimaryAccent;
                loginBtn.ForeColor = Color.White;
                loginBtn.FlatAppearance.BorderSize = 0;
                loginBtn.FlatStyle = FlatStyle.Flat;
            }
        }
    }

    public static class UiThemeHelper
    {
        public static void ApplyModernStyling(Control parent) => UIThemeHelper.ApplyTheme(parent);
        public static void ApplyGridStyling(DataGridView grid) => UIThemeHelper.ApplyGridTheme(grid);
        public static void ConfigureTimePicker(DateTimePicker picker, bool use24Hour = false) => UIThemeHelper.ConfigureTimePicker(picker, use24Hour);
        public static void PopulateTimeSlots(ComboBox combo, int intervalMinutes = 30, bool use24Hour = false, string defaultSelection = "06:00 AM")
            => UIThemeHelper.PopulateTimeSlots(combo, intervalMinutes, use24Hour, defaultSelection);
        public static string? ShowAnalogClockPicker(IWin32Window? owner, string initialTime = "08:00 AM")
            => UIThemeHelper.ShowAnalogClockPicker(owner, initialTime);
    }
}
