using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// The one place for how dates, times and emails are shown and read. Display is always MM/dd/yyyy and hh:mm AM/PM.
    /// Stored values (SQL parameters, database text, file names) stay yyyy-MM-dd and are NOT written through here.
    /// </summary>
    public static class Fmt
    {
        public const string DateFormat = "MM/dd/yyyy";
        public const string TimeFormat = "hh:mm tt";

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ---- dates ----
        public static string Date(DateTime d) => d.ToString(DateFormat, Inv);
        public static string Date(DateTime? d) => d.HasValue ? Date(d.Value) : "";
        /// <summary>Date and time of day, e.g. 11/14/2026 08:42 PM.</summary>
        public static string DateTimeText(DateTime d) => d.ToString(DateFormat + " " + TimeFormat, Inv);
        public static string DateTimeText(DateTime? d) => d.HasValue ? DateTimeText(d.Value) : "";
        /// <summary>Short form for chips and headers, e.g. 11/14.</summary>
        public static string Short(DateTime d) => d.ToString("MM/dd", Inv);
        public static string Short(DateTime? d) => d.HasValue ? Short(d.Value) : "";
        public static string ShortDateTime(DateTime d) => d.ToString("MM/dd " + TimeFormat, Inv);
        public static string ShortDateTime(DateTime? d) => d.HasValue ? ShortDateTime(d.Value) : "";
        /// <summary>Weekday and date, e.g. Sat, 11/14/2026.</summary>
        public static string DayDate(DateTime d) => d.ToString("ddd", Inv) + ", " + Date(d);
        public static string DayDate(DateTime? d) => d.HasValue ? DayDate(d.Value) : "";
        public static string DayShort(DateTime d) => d.ToString("ddd", Inv) + ", " + Short(d);
        public static string DayShort(DateTime? d) => d.HasValue ? DayShort(d.Value) : "";

        private static readonly string[] DateInputs = { "MM/dd/yyyy", "M/d/yyyy", "MM/d/yyyy", "M/dd/yyyy", "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-ddTHH:mm:ss" };

        /// <summary>Reads MM/dd/yyyy, M/d/yyyy and the old stored yyyy-MM-dd.</summary>
        public static bool TryParseDate(string? s, out DateTime d)
        {
            d = default;
            string t = (s ?? "").Trim();
            if (t.Length == 0) return false;
            return DateTime.TryParseExact(t, DateInputs, Inv, DateTimeStyles.None, out d);
        }

        /// <summary>A stored date text (yyyy-MM-dd or older) shown as MM/dd/yyyy. Text that is not a date is returned as it was.</summary>
        public static string DateText(string? stored) => TryParseDate(stored, out var d) ? Date(d) : (stored ?? "").Trim();

        /// <summary>A date typed or shown as MM/dd/yyyy turned back into the stored yyyy-MM-dd. Text that is not a date is returned as it was.</summary>
        public static string StoredDate(string? shown) => TryParseDate(shown, out var d) ? d.ToString("yyyy-MM-dd", Inv) : (shown ?? "").Trim();

        private static readonly Regex IsoDateRx = new(@"\b(\d{4})-(\d{2})-(\d{2})\b", RegexOptions.Compiled);

        /// <summary>Every stored yyyy-MM-dd inside a line of text shown as MM/dd/yyyy ("2026-11-01 to 2026-11-09" becomes "11/01/2026 to 11/09/2026").</summary>
        public static string DatesIn(string? text) => IsoDateRx.Replace(text ?? "", m => $"{m.Groups[2].Value}/{m.Groups[3].Value}/{m.Groups[1].Value}");

        // ---- times ----
        private static readonly Regex Twelve = new(@"^(\d{1,2}):?(\d{2})?\s*([AP])\.?M?\.?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex Colon24 = new(@"^(\d{1,2})[:.](\d{2})$", RegexOptions.Compiled);
        private static readonly Regex Plain24 = new(@"^(\d{2})(\d{2})$", RegexOptions.Compiled);

        /// <summary>
        /// Reads h:mm AM/PM, hh:mm AM/PM, "842p" (hmm a/p), and the older stored 24-hour HH:mm and HHmm ("2042", "0139").
        /// Hours 1-12 with AM/PM, minutes 00-59.
        /// </summary>
        public static bool TryParseTime(string? s, out TimeSpan t)
        {
            t = default;
            string x = Regex.Replace((s ?? "").Trim(), @"\s+", " ");
            if (x.Length == 0) return false;
            var m = Twelve.Match(x);
            if (m.Success)
            {
                int h = int.Parse(m.Groups[1].Value, Inv);
                int min = m.Groups[2].Success ? int.Parse(m.Groups[2].Value, Inv) : 0;
                if (h < 1 || h > 12 || min > 59) return false;
                bool pm = char.ToUpperInvariant(m.Groups[3].Value[0]) == 'P';
                int h24 = h % 12 + (pm ? 12 : 0);
                t = new TimeSpan(h24, min, 0);
                return true;
            }
            m = Colon24.Match(x);
            if (!m.Success) m = Plain24.Match(x);
            if (m.Success)
            {
                int h = int.Parse(m.Groups[1].Value, Inv);
                int min = int.Parse(m.Groups[2].Value, Inv);
                if (h > 23 || min > 59) return false;
                t = new TimeSpan(h, min, 0);
                return true;
            }
            return false;
        }

        /// <summary>Stored HHmm, HH:mm or h:mm tt shown as hh:mm AM/PM. Blank stays blank; text that is not a time is returned as it was.</summary>
        public static string Time(string? stored)
        {
            string x = (stored ?? "").Trim();
            if (x.Length == 0) return "";
            return TryParseTime(x, out var t) ? Time(t) : x;
        }

        public static string Time(TimeSpan t) => new DateTime(2000, 1, 1).Add(t).ToString(TimeFormat, Inv);

        /// <summary>True when both times read and out is not before in, or when either is blank. Unreadable text is false.</summary>
        public static bool TimeOutOnOrAfter(string? timeIn, string? timeOut)
        {
            if (string.IsNullOrWhiteSpace(timeIn) || string.IsNullOrWhiteSpace(timeOut)) return true;
            return TryParseTime(timeIn, out var a) && TryParseTime(timeOut, out var b) && b >= a;
        }

        // ---- email ----
        private static readonly Regex EmailRx = new(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9](?:[A-Za-z0-9\-]*[A-Za-z0-9])?(?:\.[A-Za-z0-9](?:[A-Za-z0-9\-]*[A-Za-z0-9])?)*\.[A-Za-z]{2,}$", RegexOptions.Compiled);

        /// <summary>name@domain.tld: no spaces, one @, a dot-separated domain ending in at least two letters.</summary>
        public static bool IsValidEmail(string? s)
        {
            string e = (s ?? "").Trim();
            return e.Length <= 254 && EmailRx.IsMatch(e) && !e.Contains("..");
        }

        // ---- controls ----
        /// <summary>
        /// The same 12-hour behaviour for the time cells of a table: while typing only digits, a/p, m, ":" and space are taken (capitals),
        /// a cell that is left shows hh:mm AM/PM, and text that is not a time gets an error text on the cell (it is refused when saving).
        /// Other columns of the table are left alone.
        /// </summary>
        public static void ApplyTimeColumns(DataGridView g, params string[] columns)
        {
            var names = new System.Collections.Generic.HashSet<string>(columns, StringComparer.Ordinal);
            var wired = new System.Collections.Generic.Dictionary<TextBox, ClockButton>();
            // Nov 17: the clock picker for the cell being edited: the picked time goes into the editing box and the edit is committed (the CellEndEdit below then formats and checks it).
            bool IsTimeCell(DataGridViewCell? c) => c != null && names.Contains(g.Columns[c.ColumnIndex].Name);
            void pickIntoCell(TextBox tb)
            {
                if (g.CurrentCell == null || !IsTimeCell(g.CurrentCell) || g.CurrentCell.ReadOnly || g.ReadOnly) return;
                string? v = PickTime(g, tb.Text);
                if (v == null) return;
                if (g.IsCurrentCellInEditMode && g.EditingControl is TextBox et) { et.Text = v; g.EndEdit(); }
                else if (g.CurrentCell != null) { g.CurrentCell.Value = v; g.CurrentCell.ErrorText = ""; }
            }
            g.EditingControlShowing += (s, e) =>
            {
                if (e.Control is not TextBox tb) return;
                bool timeCell = g.CurrentCell != null && names.Contains(g.Columns[g.CurrentCell.ColumnIndex].Name);
                if (timeCell) tb.MaxLength = 8;
                else if (tb.MaxLength == 8) tb.MaxLength = 32767;
                if (!wired.TryGetValue(tb, out var clock))
                {
                    clock = AttachClockButton(tb, () => pickIntoCell(tb));
                    wired[tb] = clock;
                    tb.KeyDown += (kd, ke) =>
                    {
                        if (g.CurrentCell == null || !names.Contains(g.Columns[g.CurrentCell.ColumnIndex].Name)) return;
                        if (ke.KeyCode == Keys.F4 || (ke.Alt && ke.KeyCode == Keys.Down)) { ke.Handled = ke.SuppressKeyPress = true; pickIntoCell(tb); }
                    };
                }
                else
                {
                    // the one editing box is reused for every column: the clock shows for time cells only
                    clock.Visible = timeCell;
                    SetRightMargin(tb, timeCell ? clock.Width : 0);
                    return;
                }
                clock.Visible = timeCell;
                SetRightMargin(tb, timeCell ? clock.Width : 0);
                tb.KeyPress += (ks, ke) =>
                {
                    if (g.CurrentCell == null || !names.Contains(g.Columns[g.CurrentCell.ColumnIndex].Name) || char.IsControl(ke.KeyChar)) return;
                    char c = char.ToUpperInvariant(ke.KeyChar);
                    if (char.IsDigit(c) || c == ':' || c == ' ' || c == 'A' || c == 'P' || c == 'M') ke.KeyChar = c;
                    else ke.Handled = true;
                };
            };
            g.CellEndEdit += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || !names.Contains(g.Columns[e.ColumnIndex].Name)) return;
                var cell = g.Rows[e.RowIndex].Cells[e.ColumnIndex];
                string text = (cell.Value?.ToString() ?? "").Trim();
                if (text.Length == 0) { cell.ErrorText = ""; return; }
                if (TryParseTime(text, out var t)) { string shown = Time(t); if (shown != text) cell.Value = shown; cell.ErrorText = ""; }
                else cell.ErrorText = "Enter a time like 08:42 PM.";
            };
            // Nov 17: a real mouse click on an EMPTY time cell that was not already being edited opens the clock once. Only mouse events are used,
            // never focus / BeginEdit / CurrentCell changes / setting a value, so code and checks that fill the cells never see a picker.
            bool wasEditingThisCell = false;
            g.CellMouseDown += (s, e) =>
            {
                wasEditingThisCell = e.RowIndex >= 0 && e.ColumnIndex >= 0 && g.IsCurrentCellInEditMode && g.CurrentCell != null
                    && g.CurrentCell.RowIndex == e.RowIndex && g.CurrentCell.ColumnIndex == e.ColumnIndex;
            };
            g.CellMouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Left || e.RowIndex < 0 || e.ColumnIndex < 0 || wasEditingThisCell || !names.Contains(g.Columns[e.ColumnIndex].Name)) return;
                var cell = g.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (cell.ReadOnly || g.ReadOnly || !string.IsNullOrWhiteSpace(cell.Value?.ToString())) return;
                int row = e.RowIndex, col = e.ColumnIndex;
                g.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (g.IsDisposed || row >= g.Rows.Count) return;
                        var c = g.Rows[row].Cells[col];
                        if (c.ReadOnly || !string.IsNullOrWhiteSpace(c.Value?.ToString())) return;
                        if (g.CurrentCell != c) g.CurrentCell = c;
                        if (!g.IsCurrentCellInEditMode && !g.BeginEdit(false)) return;
                        if (g.EditingControl is TextBox tb) pickIntoCell(tb);
                    }
                    catch (Exception ex) { ErrLog.Swallowed(ex); }
                }));
            };
        }

        // ---- Nov 17: clock button on time inputs ----
        private sealed class ClockButton : Button
        {
            public ClockButton() { SetStyle(ControlStyles.Selectable, false); TabStop = false; }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        /// <summary>Keeps typed text clear of the clock button: the box's right text margin is the button's width.</summary>
        private static void SetRightMargin(TextBox tb, int px)
        {
            const int EM_SETMARGINS = 0xD3, EC_RIGHTMARGIN = 2;
            if (!tb.IsHandleCreated) return;
            SendMessage(tb.Handle, EM_SETMARGINS, (IntPtr)EC_RIGHTMARGIN, (IntPtr)(px << 16));
        }

        /// <summary>A small flat clock button docked at the right inside the box. It cannot take focus, so clicking it does not make the box "leave".</summary>
        private static ClockButton AttachClockButton(TextBox tb, Action pick)
        {
            var b = new ClockButton
            {
                Text = "\U0001F552", FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Dock = DockStyle.Right,
                Width = Math.Max(18, tb.Font.Height + 6), Margin = Padding.Empty, Padding = Padding.Empty,
                Font = new System.Drawing.Font("Segoe UI Emoji", Math.Max(7f, tb.Font.Size - 1.5f)),
                UseVisualStyleBackColor = false, BackColor = tb.BackColor, ForeColor = tb.ForeColor,
            };
            b.FlatAppearance.BorderSize = 0;
            new ToolTip().SetToolTip(b, "Pick a time");
            b.Click += (s, e) => pick();
            tb.Controls.Add(b);
            tb.HandleCreated += (s, e) => SetRightMargin(tb, b.Visible ? b.Width : 0);
            tb.BackColorChanged += (s, e) => b.BackColor = tb.BackColor;
            tb.ForeColorChanged += (s, e) => b.ForeColor = tb.ForeColor;
            SetRightMargin(tb, b.Width);
            return b;
        }

        /// <summary>Opens the clock with the box's time and returns the picked time as hh:mm AM/PM, or null (cancelled / not a time).</summary>
        private static string? PickTime(Control owner, string current)
        {
            string start = TryParseTime(current, out var cur) ? Time(cur) : "";
            string? picked = Views.AnalogClockTimePickerDialog.ShowTimePicker(owner.FindForm(), start);
            return picked != null && TryParseTime(picked, out var t) ? Time(t) : null;
        }

        public static void ApplyDatePicker(DateTimePicker p)
        {
            p.Format = DateTimePickerFormat.Custom;
            p.CustomFormat = DateFormat;
        }

        /// <summary>
        /// 12-hour time box: digits and a/p are typed, the box shows hh:mm AM when you leave it; text that is not a time turns red
        /// (the ValidationHelper state) and an empty box is left alone.
        /// </summary>
        /// <summary>Nov 18: raised after a time was picked from the clock of a time box (never for typed or code-set text).</summary>
        public static event Action<TextBox>? TimeBoxPicked;

        public static void ApplyTimeBox(TextBox tb)
        {
            tb.MaxLength = 8;
            tb.PlaceholderText = "e.g. 08:42 PM";
            tb.KeyPress += (s, e) =>
            {
                if (char.IsControl(e.KeyChar)) return;
                char c = char.ToUpperInvariant(e.KeyChar);
                if (char.IsDigit(c) || c == ':' || c == ' ' || c == 'A' || c == 'P' || c == 'M') e.KeyChar = c;
                else e.Handled = true;
            };
            void validate()
            {
                string text = tb.Text.Trim();
                if (text.Length == 0) { ValidationHelper.ApplyValidationState(tb, true, ""); return; }
                if (TryParseTime(text, out var t)) { tb.Text = Time(t); ValidationHelper.ApplyValidationState(tb, true, ""); }
                else ValidationHelper.ApplyValidationState(tb, false, "Enter a time like 08:42 PM.");
            }
            // Nov 17: clock button inside the box, F4 / Alt+Down, and one automatic clock when a mouse click lands in an empty box.
            bool picking = false, autoOpened = false;
            void pick()
            {
                if (picking || tb.ReadOnly || !tb.Enabled) return;
                picking = true;
                try
                {
                    string? v = PickTime(tb, tb.Text);
                    if (v != null) { tb.Text = v; validate(); TimeBoxPicked?.Invoke(tb); }   // Nov 18: only a time picked from the clock raises it
                }
                finally { picking = false; }
            }
            AttachClockButton(tb, pick);
            tb.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F4 || (e.Alt && e.KeyCode == Keys.Down)) { e.Handled = e.SuppressKeyPress = true; pick(); }
            };
            tb.MouseDown += (s, e) =>
            {
                // real mouse only (MouseDown): focus, Tab and code setting .Text never open it
                if (e.Button != MouseButtons.Left || autoOpened || tb.Text.Length > 0 || tb.ReadOnly || !tb.Enabled) return;
                autoOpened = true;
                tb.BeginInvoke(new Action(pick));
            };
            tb.Leave += (s, e) =>
            {
                autoOpened = false;
                validate();
            };
            tb.TextChanged += (s, e) =>
            {
                if (tb.Tag as string == "InvalidInput" && TryParseTime(tb.Text, out _)) ValidationHelper.ApplyValidationState(tb, true, "");
            };
        }
    }
}
