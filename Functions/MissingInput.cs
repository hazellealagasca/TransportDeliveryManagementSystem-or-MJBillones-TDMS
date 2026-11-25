using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Nov 25: a box with missing (or wrong) input is tinted light red with a red outline, the form goes to it, scrolls to it and focuses it
    /// after the message is closed. The tint clears as soon as a proper value is entered. The original look is kept here (never in Tag).
    /// </summary>
    public static class MissingInput
    {
        private sealed class State
        {
            public Control Field = null!;
            public Func<bool>? IsFilled;
            public Color OldBack;
            public bool ChangedBack;
            public Color OldGridBack;
            public Control? Host;
            public PaintEventHandler? Paint;
            public readonly List<Action> Unhook = new();
        }

        private static readonly ConditionalWeakTable<Control, State> marks = new();

        private static Color RedBorder => Pal.C(239, 68, 68);

        public static bool IsMarked(Control field) => marks.TryGetValue(field, out _);

        public static void Mark(Control field, Func<bool>? isFilled = null)
        {
            if (field == null || field.IsDisposed) return;
            if (marks.TryGetValue(field, out var existing)) { if (isFilled != null) existing.IsFilled = isFilled; field.Parent?.Invalidate(); return; }

            var st = new State { Field = field, IsFilled = isFilled };
            marks.Add(field, st);

            if (field is TextBox or ComboBox or NumericUpDown or MaskedTextBox or RichTextBox)
            {
                st.OldBack = field.BackColor; st.ChangedBack = true;
                field.BackColor = AppTheme.DangerSoft;
            }
            else if (field is DataGridView g)
            {
                st.OldGridBack = g.BackgroundColor;
                g.BackgroundColor = AppTheme.DangerSoft;
            }

            // red outline drawn by the parent so it shows for any control (date pickers, grids, check boxes)
            st.Host = field.Parent;
            if (st.Host != null)
            {
                st.Paint = (s, e) =>
                {
                    if (!field.Visible) return;
                    var r = field.Bounds; r.Inflate(2, 2);
                    using var pen = new Pen(RedBorder, 2f);
                    e.Graphics.DrawRectangle(pen, r);
                };
                st.Host.Paint += st.Paint;
                st.Host.Invalidate();
            }

            void Check(object? s, EventArgs e) { if (Filled(st)) Clear(field); }
            void CheckRows(object? s, DataGridViewRowsAddedEventArgs e) { if (Filled(st)) Clear(field); }
            void CheckCell(object? s, DataGridViewCellEventArgs e) { if (Filled(st)) Clear(field); }
            void Hide(object? s, EventArgs e) { if (!field.Visible) Clear(field); }
            void Gone(object? s, EventArgs e) { Clear(field); }
            void Moved(object? s, EventArgs e) { st.Host?.Invalidate(); }

            switch (field)
            {
                case NumericUpDown n:
                    n.ValueChanged += Check; st.Unhook.Add(() => n.ValueChanged -= Check); break;
                case ComboBox cb:
                    cb.TextChanged += Check; st.Unhook.Add(() => cb.TextChanged -= Check);
                    cb.SelectedIndexChanged += Check; st.Unhook.Add(() => cb.SelectedIndexChanged -= Check);
                    break;
                case TextBoxBase tb:
                    tb.TextChanged += Check; st.Unhook.Add(() => tb.TextChanged -= Check); break;
                case DateTimePicker dtp:
                    dtp.ValueChanged += Check; st.Unhook.Add(() => dtp.ValueChanged -= Check); break;
                case DataGridView dg:
                    dg.RowsAdded += CheckRows; st.Unhook.Add(() => dg.RowsAdded -= CheckRows);
                    dg.CellValueChanged += CheckCell; st.Unhook.Add(() => dg.CellValueChanged -= CheckCell);
                    break;
                default:
                    field.TextChanged += Check; st.Unhook.Add(() => field.TextChanged -= Check);
                    field.Click += Check; st.Unhook.Add(() => field.Click -= Check);
                    break;
            }
            field.VisibleChanged += Hide; st.Unhook.Add(() => field.VisibleChanged -= Hide);
            field.Resize += Moved; st.Unhook.Add(() => field.Resize -= Moved);
            field.Move += Moved; st.Unhook.Add(() => field.Move -= Moved);
            field.Disposed += Gone; st.Unhook.Add(() => field.Disposed -= Gone);
        }

        private static bool Filled(State st)
        {
            if (st.IsFilled != null) { try { return st.IsFilled(); } catch { return false; } }
            switch (st.Field)
            {
                case NumericUpDown n: return n.Value != n.Minimum && n.Value > 0;
                case ComboBox cb: return cb.SelectedIndex >= 0 || !string.IsNullOrWhiteSpace(cb.Text);
                case TextBoxBase tb: return !string.IsNullOrWhiteSpace(tb.Text);
                default: return true; // date picker, grid, any other control: any change clears
            }
        }

        public static void Clear(Control field)
        {
            if (field == null || !marks.TryGetValue(field, out var st)) return;
            marks.Remove(field);
            foreach (var u in st.Unhook) { try { u(); } catch (Exception ex) { ErrLog.Swallowed(ex); } }
            st.Unhook.Clear();
            if (!field.IsDisposed)
            {
                if (st.ChangedBack) field.BackColor = st.OldBack;
                if (field is DataGridView g) g.BackgroundColor = st.OldGridBack;
            }
            if (st.Host != null && st.Paint != null)
            {
                st.Host.Paint -= st.Paint;
                if (!st.Host.IsDisposed) st.Host.Invalidate();
            }
        }

        public static void ClearAll(Control root)
        {
            if (root == null) return;
            Clear(root);
            foreach (Control c in root.Controls) ClearAll(c);
        }

        /// <summary>Shows the missing-input message: goes to the field's step, scrolls to it, tints it, shows the message, then focuses the field.</summary>
        public static void Show(Control? field, string message, IWin32Window? owner = null, Action<Control>? goTo = null, string caption = "Missing input", Func<bool>? isFilled = null)
        {
            if (field != null && !field.IsDisposed)
            {
                try { goTo?.Invoke(field); } catch (Exception ex) { ErrLog.Swallowed(ex); }
                ScrollTo(field);
                Mark(field, isFilled);
            }
            MessageBox.Show(owner, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (field == null || field.IsDisposed) return;
            FocusField(field, goTo);
            if (field.IsHandleCreated)
            {
                try { field.BeginInvoke(new Action(() => { if (!field.IsDisposed) FocusField(field, null); })); } catch (Exception ex) { ErrLog.Swallowed(ex); }
            }
        }

        private static void ScrollTo(Control field)
        {
            try
            {
                for (var p = field.Parent; p != null; p = p.Parent)
                    if (p is ScrollableControl sc && sc.AutoScroll) { sc.ScrollControlIntoView(field); break; }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        private static void FocusField(Control field, Action<Control>? goTo)
        {
            try
            {
                goTo?.Invoke(field);
                field.Focus();
                switch (field)
                {
                    case TextBoxBase tb: tb.SelectAll(); break;
                    case ComboBox cb when cb.DropDownStyle == ComboBoxStyle.DropDown: cb.SelectAll(); break;
                    case NumericUpDown n: n.Select(0, n.Text.Length); break;
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }
    }
}
