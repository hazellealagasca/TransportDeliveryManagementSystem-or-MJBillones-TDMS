using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Live check of a typed unique number (plan 17.7): judged on every keystroke against an in-memory snapshot, a red message under the field
    /// that names the owner, a confirmation tick for a same-digits service invoice / receipt pair, hints for the next number and skipped numbers.
    /// While the number is used the keyboard cannot move on from the field and the form's checklist fails, so Save stays disabled.
    /// (Mouse clicks are not trapped, so nobody is locked inside the field; the save itself re-checks inside the write transaction.)
    /// </summary>
    public sealed class NumberFieldGuard
    {
        private readonly TextBox box;
        private readonly NumberKind kind;
        private readonly Func<int> exceptInvoiceId;
        private readonly Func<string?> exceptReceipt;
        private NumberIndex? index;
        private string tickKey = "";
        private readonly System.Windows.Forms.Timer refresh = new() { Interval = 15000 };

        /// <summary>The message under the field. Add it to the form right after the text box.</summary>
        public readonly Label Message;
        /// <summary>"Yes, this is a Service Invoice No., not the Collection Receipt No. ..." (shown only when a confirmation is needed).</summary>
        public readonly CheckBox Tick;
        public NumberCheck Current { get; private set; } = new();
        public event Action? Changed;

        public NumberFieldGuard(TextBox box, NumberKind kind, Func<int>? exceptInvoiceId = null, Func<string?>? exceptReceipt = null)
        {
            this.box = box; this.kind = kind;
            this.exceptInvoiceId = exceptInvoiceId ?? (() => 0);
            this.exceptReceipt = exceptReceipt ?? (() => null);
            Message = new Label { AutoSize = true, Dock = DockStyle.Top, UseMnemonic = false, Font = new Font("Segoe UI", 8.75F), Padding = new Padding(0, 2, 0, 4), Visible = false };
            Tick = new CheckBox { AutoSize = true, Dock = DockStyle.Top, Font = new Font("Segoe UI Semibold", 8.75F), Padding = new Padding(0, 0, 0, 6), Visible = false };
            box.TextChanged += (s, e) => Recheck();
            box.Enter += (s, e) => { ReloadIndex(); Recheck(); };
            box.SizeChanged += (s, e) => FitWidth();
            box.PreviewKeyDown += (s, e) => { if (HasProblem && (e.KeyCode == Keys.Tab || e.KeyCode == Keys.Enter)) e.IsInputKey = true; };
            box.KeyDown += (s, e) =>
            {
                if (HasProblem && (e.KeyCode == Keys.Tab || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Down))
                {
                    e.SuppressKeyPress = true; e.Handled = true; // the number is used: stay here and fix it
                    box.SelectAll();
                }
            };
            Tick.CheckedChanged += (s, e) => Changed?.Invoke();
            Message.Click += (s, e) => { if (HasProblem && Current.Conflict != null) OpenOwner(Message, Current.Conflict); };
            refresh.Tick += (s, e) => { if (box.IsHandleCreated && box.Visible && box.Focused) { ReloadIndex(); Recheck(); } };
            refresh.Start();
            box.Disposed += (s, e) => { refresh.Stop(); refresh.Dispose(); };
        }

        /// <summary>Opens the record that owns a number: the dispatch for a dispatch number or DR, otherwise the record inspector for the invoice / receipt.</summary>
        public static void OpenOwner(Control parent, NumberEntry owner)
        {
            try
            {
                switch (owner.Kind)
                {
                    case NumberKind.DSP:
                    case NumberKind.DR:
                        RowMenu.OpenDispatch(parent, owner.OwnerId, owner.Kind == NumberKind.DSP ? owner.Text : null);
                        return;
                    case NumberKind.SI:
                        {
                            var inv = MJdatabase.GetInvoices().FirstOrDefault(i => i.Id == owner.OwnerId);
                            if (inv != null) { RecordDetailsDialog.ShowDetails(parent, inv); return; }
                            break;
                        }
                    case NumberKind.CR:
                        {
                            var rc = MJdatabase.GetCollectionReceipts().FirstOrDefault(r => r.Id == owner.OwnerId);
                            if (rc != null) { RecordDetailsDialog.ShowDetails(parent, rc); return; }
                            break;
                        }
                }
                QuietBox.Show($"{owner.Describe()} could not be opened.", "Not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        private void FitWidth() { try { int w = Math.Max(220, box.Width - 4); Message.MaximumSize = new Size(w, 0); Tick.MaximumSize = new Size(w, 0); } catch { } }

        /// <summary>A same-type owner or a format problem on a non-empty number.</summary>
        public bool HasProblem => box.Text.Trim().Length > 0 && (Current.FormatError != null || Current.Conflict != null);
        /// <summary>The person has not yet ticked the same-digits confirmation that is required.</summary>
        public bool TickMissing => Current.TickNeeded && !Tick.Checked;
        public bool CrossTypeConfirmed => Tick.Checked;

        /// <summary>Forgets the tick and the message (the form was cleared).</summary>
        public void Reset() { Tick.Checked = false; tickKey = ""; Recheck(); }

        public void ReloadIndex()
        {
            try { index = MJdatabase.LoadNumberIndex(); }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        public void Recheck()
        {
            if (index == null || (DateTime.Now - index.LoadedAt).TotalSeconds > 15) ReloadIndex();
            if (index == null) return;
            Current = index.Check(kind, box.Text, exceptInvoiceId(), exceptReceipt());
            if (Current.Key != tickKey) { Tick.Checked = false; tickKey = Current.Key; } // the confirmation belongs to the number typed
            Render();
            Changed?.Invoke();
        }

        private void Render()
        {
            FitWidth();
            string t = box.Text.Trim();
            var c = Current;
            if (t.Length == 0) { Message.Visible = false; Tick.Visible = false; return; }
            string label = NumberKey.Label(kind);
            string text; Color color;
            bool canOpen = c.FormatError == null && c.Conflict != null && c.Conflict.OwnerId > 0;
            Message.Cursor = canOpen ? Cursors.Hand : Cursors.Default;
            if (c.FormatError != null || c.Conflict != null) { text = "⛔ " + c.Why() + (canOpen ? "  Click here to open it." : ""); color = AppTheme.DangerText; }
            else
            {
                var parts = new System.Collections.Generic.List<string>();
                bool warn = c.Warnings.Count > 0;
                parts.Add(warn ? "⚠ " + string.Join(" ", c.Warnings) + (c.Recommendation != null ? " " + c.Recommendation : "") : $"✓ {label} {t} is free.");
                if (c.Hint != null) parts.Add(c.Hint);
                text = string.Join("   ", parts);
                color = warn ? AppTheme.WarningText : AppTheme.SuccessText;
            }
            Message.Text = text; Message.ForeColor = color; Message.Visible = true;
            var pair = c.CrossHits.FirstOrDefault(h => h.Kind == NumberKind.SI || h.Kind == NumberKind.CR);
            bool needTick = c.TickNeeded && pair != null && !HasProblem;
            Tick.Visible = needTick || (pair != null && Tick.Checked && !HasProblem);
            if (pair != null)
                Tick.Text = kind == NumberKind.SI ? $"Yes, this is a Service Invoice No., not the {NumberKey.Label(pair.Kind)} {pair.Text}" : $"Yes, this is a Collection Receipt No., not the {NumberKey.Label(pair.Kind)} {pair.Text}";
            Tick.ForeColor = AppTheme.WarningText;
        }
    }
}
