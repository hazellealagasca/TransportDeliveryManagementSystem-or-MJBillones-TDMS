using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class TripReportView
    {
        /// <summary>Nov 6: the form starts blank, so the times and the sign-off names must be typed before saving.</summary>
        private List<(string Message, Control Field)> BlankFormChecks()
        {
            var list = new List<(string, Control)>();
            Control timeField(Control box) => box.Visible ? box : (Control?)gridStores ?? box;
            if (string.IsNullOrWhiteSpace(txtArrivalTime.Text))
                list.Add(("Store arrival time is required: type the In time for the stores.", timeField(txtArrivalTime)));
            if (string.IsNullOrWhiteSpace(txtDepartureTime.Text))
                list.Add(("Store departure time is required: type the Out time for the stores.", timeField(txtDepartureTime)));
            void need(TextBox? t, string name)
            {
                if (t != null && t.Visible && string.IsNullOrWhiteSpace(t.Text)) list.Add(($"{name} is required.", t));
            }
            if (gridStores != null)
            {
                gridStores.EndEdit();
                var noDr = gridStores.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && DrList.Parse(r.Cells["colDr"].Value?.ToString()).Count == 0)
                    .Select(r => (r.Cells["colStore"]?.Value?.ToString() ?? "").Trim()).ToList();
                if (noDr.Count > 0) list.Add(($"A DR (delivery receipt) number is required for every store. Missing for: {string.Join(", ", noDr.Select(n => n.Length > 0 ? n : "a store").Take(6))}{(noDr.Count > 6 ? ", …" : "")}.", gridStores));
            }
            if (gridStores != null)
            {
                // Nov 10: a DR number is 4 to 8 digits.
                var badDr = gridStores.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow)
                    .SelectMany(r => DrList.Parse(r.Cells["colDr"].Value?.ToString())).Where(d => !System.Text.RegularExpressions.Regex.IsMatch(d, @"^\d{4,8}$")).Distinct().ToList();
                if (badDr.Count > 0) list.Add(($"A DR number must be 4 to 8 digits (numbers only). Fix: {string.Join(", ", badDr.Take(6))}{(badDr.Count > 6 ? ", …" : "")}.", gridStores));
                // Nov 10: every store needs its time in and time out.
                var noIn = gridStores.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && string.IsNullOrWhiteSpace(r.Cells["colIn"].Value?.ToString())).Select(r => (r.Cells["colStore"]?.Value?.ToString() ?? "").Trim()).ToList();
                var noOut = gridStores.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && string.IsNullOrWhiteSpace(r.Cells["colOut"].Value?.ToString())).Select(r => (r.Cells["colStore"]?.Value?.ToString() ?? "").Trim()).ToList();
                if (noIn.Count > 0) list.Add(($"Time in is required for every store. Missing for: {string.Join(", ", noIn.Select(n => n.Length > 0 ? n : "a store").Take(6))}{(noIn.Count > 6 ? ", …" : "")}.", gridStores));
                if (noOut.Count > 0) list.Add(($"Time out is required for every store. Missing for: {string.Join(", ", noOut.Select(n => n.Length > 0 ? n : "a store").Take(6))}{(noOut.Count > 6 ? ", …" : "")}.", gridStores));
            }
            if (txtTruckTimeIn != null && string.IsNullOrWhiteSpace(txtTruckTimeIn.Text)) list.Add(("Truck time in (plant gate) is required.", txtTruckTimeIn));
            if (txtTruckTimeOut != null && string.IsNullOrWhiteSpace(txtTruckTimeOut.Text)) list.Add(("Truck time out (plant gate) is required.", txtTruckTimeOut));
            // Nov 14: plant gate times and store times are 12-hour (08:42 PM); an old 4-digit HHMM still reads. The sign-off names are real names.
            foreach (var (tb, nm) in new[] { (txtTruckTimeIn, "Truck time in"), (txtTruckTimeOut, "Truck time out") })
                if (tb != null && !string.IsNullOrWhiteSpace(tb.Text) && !GateTimeOk(tb.Text)) list.Add(($"{nm} must be a time like 08:42 PM.", tb));
            // Nov 26: the truck time in at the plant gate must be later than the departure time on the dispatch order (the same or earlier is refused;
            // an overnight trip is fine). Blocks Next / Save, not Save draft.
            string departure = (GetSelectedDelivery()?.DepartureTime ?? "").Trim();
            if (txtTruckTimeIn != null && DailyRules.TruckInNotAfterDeparture(departure, txtTruckTimeIn.Text))
                list.Add(($"Truck time in ({Fmt.Time(txtTruckTimeIn.Text)}) must be later than the departure time on the dispatch order ({Fmt.Time(departure)}): it can't be the same or earlier.", txtTruckTimeIn));
            if (gridStores != null)
            {
                foreach (var (col, nm) in new[] { ("colIn", "Time in"), ("colOut", "Time out") })
                {
                    var bad = gridStores.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && !string.IsNullOrWhiteSpace(r.Cells[col].Value?.ToString()) && !Fmt.TryParseTime(r.Cells[col].Value?.ToString(), out _))
                        .Select(r => (r.Cells["colStore"]?.Value?.ToString() ?? "").Trim()).ToList();
                    if (bad.Count > 0) list.Add(($"{nm} must be a time like 08:42 PM. Fix it for: {string.Join(", ", bad.Select(n => n.Length > 0 ? n : "a store").Take(6))}{(bad.Count > 6 ? ", …" : "")}.", gridStores));
                }
            }
            // Nov 17: a store's time in / time out can't be earlier than the truck time in at the plant gate (an overnight trip is the exception). Blocks Proceed / Save, not Save draft.
            // (rule change: also not earlier than the truck time out; the check is in DailyRules.StoreTimeBeforePlantGate)
            if (gridStores != null && (Fmt.TryParseTime(txtTruckTimeIn?.Text, out _) || Fmt.TryParseTime(txtTruckTimeOut?.Text, out _)))
            {
                string tin = (txtTruckTimeIn?.Text ?? "").Trim(), tout = (txtTruckTimeOut?.Text ?? "").Trim();
                var early = new List<string>();
                foreach (DataGridViewRow r in gridStores.Rows)
                {
                    if (r.IsNewRow) continue;
                    string store = (r.Cells["colStore"]?.Value?.ToString() ?? "").Trim(); if (store.Length == 0) store = "a store";
                    foreach (var (col, nm) in new[] { ("colIn", "time in"), ("colOut", "time out") })
                    {
                        string v = (r.Cells[col].Value?.ToString() ?? "").Trim();
                        if (DailyRules.StoreTimeBeforePlantGate(tin, tout, v) != null) early.Add($"{store} ({nm} {Fmt.Time(v)})");
                    }
                }
                if (early.Count > 0)
                {
                    var gate = new List<string>();
                    if (Fmt.TryParseTime(tin, out _)) gate.Add($"time in {Fmt.Time(tin)}");
                    if (Fmt.TryParseTime(tout, out _)) gate.Add($"time out {Fmt.Time(tout)}");
                    list.Add(($"A store's time in and time out can't be earlier than the truck's plant gate times ({string.Join(", ", gate)}). Fix it for: {string.Join(", ", early.Take(6))}{(early.Count > 6 ? ", …" : "")}.", gridStores));
                }
            }
            // Nov 18: a store's time out can't be earlier than its own time in (blocks Proceed / Save, not Save draft).
            if (gridStores != null)
            {
                var backwards = new List<string>();
                foreach (DataGridViewRow r in gridStores.Rows)
                {
                    if (r.IsNewRow) continue;
                    string tin = (r.Cells["colIn"].Value?.ToString() ?? "").Trim(), tout = (r.Cells["colOut"].Value?.ToString() ?? "").Trim();
                    if (!DailyRules.GateOutBeforeIn(tin, tout)) continue;
                    string store = (r.Cells["colStore"]?.Value?.ToString() ?? "").Trim();
                    backwards.Add($"{(store.Length > 0 ? store : "a store")} (in {Fmt.Time(tin)}, out {Fmt.Time(tout)})");
                }
                if (backwards.Count > 0)
                    list.Add(($"A store's time out can't be earlier than its time in. Fix it for: {string.Join(", ", backwards.Take(6))}{(backwards.Count > 6 ? ", …" : "")}.", gridStores));
            }
            // Nov 18: missing crates must be answered in step 2 (Yes with an incident; was step 3 before Nov 26). Blocks Proceed / Save, not Save draft.
            if (MissingCratesNeedIncident(out int missingCrates, out var missingStores))
                list.Add(($"{missingCrates:N0} crate(s) are missing (returned fewer than delivered: {string.Join(", ", missingStores.Take(6))}{(missingStores.Count > 6 ? ", …" : "")}). Answer Yes under 'Any damaged or missing crates?' in step 2 Incident / Damage Report and add the incident.", (Control?)rbIncYes ?? gridStores!));
            IncidentChecks(list);   // step 3: Yes needs at least one incident
            foreach (var (tb, nm) in SignOffBoxes())
                if (tb != null && !string.IsNullOrWhiteSpace(tb.Text) && !SecurityHelper.IsValidName(tb.Text.Trim())) list.Add(($"{nm} must be a name (letters only).", tb));
            need(txtGuardName, "Guard name & sign");
            need(txtDtrApprovedBy, "DTR \"Approved by\"");
            need(txtDtrPreparedBy, "DTR \"Prepared by\"");
            need(txtDrPreparedBy, "DR \"Prepared by\"");
            need(txtDrCheckedBy, "DR \"Checked by\"");
            foreach (var (_, field) in list) ValidationHelper.ApplyValidationState(field, false, "Required.");
            return list;
        }

        /// <summary>Nov 12: the question to ask when the truck's plant gate time out is earlier than its time in (an overnight trip is real, so this asks and never blocks). Null when the order is fine.</summary>
        private string? GateOrderQuestion()
        {
            string tin = (txtTruckTimeIn?.Text ?? "").Trim(), tout = (txtTruckTimeOut?.Text ?? "").Trim();
            return DailyRules.GateOutBeforeIn(tin, tout) ? DailyRules.GateOvernightQuestion(tin, tout) : null;
        }

        /// <summary>Asks the overnight question; false (stay on the form, back to the time out box) when the answer is No.</summary>
        private bool ConfirmGateOrder()
        {
            var q = GateOrderQuestion();
            if (q == null) return true;
            if (MessageBox.Show(FindForm(), q, "Plant gate times", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes) return true;
            try { txtTruckTimeOut.Focus(); txtTruckTimeOut.SelectAll(); } catch { }
            return false;
        }

        private static bool GateTimeOk(string t) => Fmt.TryParseTime(t, out _);

        /// <summary>Nov 14: the same overnight question for every store whose time out is earlier than its time in. Asked one store at a time; No sends the person back to that store's Time out cell.</summary>
        private bool ConfirmStoreOrder()
        {
            // Nov 18: a store's time out earlier than its time in is no longer asked about ("overnight?"): it is refused by BlankFormChecks.
            return true;
#pragma warning disable CS0162 // kept for reference
            if (gridStores == null) return true;
            gridStores.EndEdit();
            foreach (DataGridViewRow r in gridStores.Rows)
            {
                if (r.IsNewRow) continue;
                string tin = (r.Cells["colIn"].Value?.ToString() ?? "").Trim(), tout = (r.Cells["colOut"].Value?.ToString() ?? "").Trim();
                if (!DailyRules.GateOutBeforeIn(tin, tout)) continue;
                string store = (r.Cells["colStore"].Value?.ToString() ?? "").Trim();
                if (MessageBox.Show(FindForm(), DailyRules.OvernightQuestion(tin, tout), store.Length > 0 ? $"Store times · {store}" : "Store times", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes) continue;
                try { gridStores.CurrentCell = r.Cells["colOut"]; gridStores.Focus(); } catch (Exception ex) { ErrLog.Swallowed(ex); }
                return false;
            }
            return true;
#pragma warning restore CS0162
        }

        private IEnumerable<(TextBox? Box, string Name)> SignOffBoxes() => new (TextBox?, string)[]
        {
            (txtDtrApprovedBy, "DTR \"Approved by\""), (txtReturnCheckedBy, "Return checked by"), (txtDtrPreparedBy, "DTR \"Prepared by\""),
            (txtDrPreparedBy, "DR \"Prepared by\""), (txtDrCheckedBy, "DR \"Checked by\""), (txtGuardName, "Guard name & sign")
        };

        /// <summary>Plant gate &amp; sign-off boxes check themselves when you leave them; the times take digits only.</summary>
        private void WirePlantGateValidation()
        {
            foreach (var tb in new[] { txtTruckTimeIn, txtTruckTimeOut })
                if (tb != null) Fmt.ApplyTimeBox(tb);   // Nov 14: 12-hour box, shown as hh:mm AM/PM when left; "e.g. 08:42 PM"
            if (gridStores != null) Fmt.ApplyTimeColumns(gridStores, "colIn", "colOut");
            // Nov 17: live hint (cell error text only, no pop-ups) when a store time or the truck times change. Added after Fmt's own handlers so its format errors are set first.
            if (gridStores != null) gridStores.CellEndEdit += (s, e) => MarkStoreTimesBeforeTruckIn();
            WireTimeAutoAdvance();
            foreach (var tb in new[] { txtTruckTimeIn, txtTruckTimeOut })
                if (tb != null) { tb.TextChanged += (s, e) => MarkStoreTimesBeforeTruckIn(); tb.Leave += (s, e) => MarkStoreTimesBeforeTruckIn(); }
            foreach (var (tb, _) in SignOffBoxes())
            {
                if (tb == null) continue;
                var box = tb;
                box.MaxLength = 60; // Nov 12: the name check allows up to 60 letters
                box.Leave += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(box.Text)) { ValidationHelper.ClearErrors(box); return; }
                    ValidationHelper.ApplyValidationState(box, SecurityHelper.IsValidName(box.Text.Trim()), "Type a name (letters only).");
                };
            }
        }

        /// <summary>Nov 18: after the Truck time in is complete (clock, Enter, or typed up to AM/PM) the focus goes to Truck time out; after a store's Time in is edited to a valid time the same row's Time out cell opens.
        /// Only user actions count (key presses, clock picks, cell edits): code that sets the values never moves the focus.</summary>
        private void WireTimeAutoAdvance()
        {
            if (txtTruckTimeIn != null && txtTruckTimeOut != null)
            {
                var tin = txtTruckTimeIn; var tout = txtTruckTimeOut;
                void toOut()
                {
                    if (!Fmt.TryParseTime(tin.Text, out var t)) return;
                    tin.Text = Fmt.Time(t);
                    if (tout.Visible && tout.Enabled && !tout.ReadOnly) { tout.Focus(); tout.SelectAll(); }
                }
                tin.KeyDown += (s, e) =>
                {
                    if (e.KeyCode != Keys.Enter || !Fmt.TryParseTime(tin.Text, out _)) return;
                    e.Handled = e.SuppressKeyPress = true; toOut();
                };
                tin.KeyUp += (s, e) =>
                {
                    // the last typed letter M of a complete "hh:mm AM/PM"; a partial "08" or "08:4" never moves on
                    if (e.KeyCode == Keys.M && tin.Focused && System.Text.RegularExpressions.Regex.IsMatch(tin.Text.Trim(), @"^\d{1,2}:\d{2} ?[AP]M$", System.Text.RegularExpressions.RegexOptions.IgnoreCase) && Fmt.TryParseTime(tin.Text, out _)) toOut();
                };
                Action<TextBox> onPicked = picked => { if (ReferenceEquals(picked, tin) && !tin.IsDisposed) tin.BeginInvoke(new Action(toOut)); };
                Fmt.TimeBoxPicked += onPicked;
                tin.Disposed += (s, e) => Fmt.TimeBoxPicked -= onPicked;   // Nov 18: unsubscribe when the screen goes, so it isn't kept alive
            }
            if (gridStores != null)
            {
                var g = gridStores;
                string before = "";
                g.CellBeginEdit += (s, e) => { if (e.ColumnIndex >= 0 && g.Columns[e.ColumnIndex].Name == "colIn") before = (g.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "").Trim(); };
                g.CellEndEdit += (s, e) =>
                {
                    if (e.RowIndex < 0 || e.ColumnIndex < 0 || g.Columns[e.ColumnIndex].Name != "colIn") return;
                    string now = (g.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "").Trim();
                    if (now.Length == 0 || now == before || !Fmt.TryParseTime(now, out _)) return;   // blank, unchanged or invalid: stay
                    int row = e.RowIndex;
                    g.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (g.IsDisposed || row >= g.Rows.Count || g.Rows[row].IsNewRow || !g.Columns.Contains("colOut")) return;
                            var c = g.Rows[row].Cells["colOut"];
                            if (c.ReadOnly || !c.Visible) return;
                            g.CurrentCell = c;
                            g.BeginEdit(true);
                        }
                        catch (Exception ex) { ErrLog.Swallowed(ex); }
                    }));
                };
            }
        }

        /// <summary>Nov 17: marks store time cells that are earlier than the truck time in (cell error text) and clears it when fixed. Only its own message is touched, never a format error.</summary>
        private void MarkStoreTimesBeforeTruckIn()
        {
            if (gridStores == null) return;
            try
            {
                string tin = (txtTruckTimeIn?.Text ?? "").Trim(), tout = (txtTruckTimeOut?.Text ?? "").Trim();
                foreach (DataGridViewRow r in gridStores.Rows)
                {
                    if (r.IsNewRow) continue;
                    // Nov 18: time out earlier than this store's own time in is marked on the Time out cell first.
                    var outCell = r.Cells["colOut"];
                    bool backwards = DailyRules.GateOutBeforeIn(r.Cells["colIn"].Value?.ToString(), outCell.Value?.ToString());
                    bool oursOut = outCell.ErrorText.StartsWith("Earlier than this store's time in");
                    if (backwards)
                    {
                        string m = $"Earlier than this store's time in ({Fmt.Time(r.Cells["colIn"].Value?.ToString())})";
                        if (outCell.ErrorText != m && (oursOut || outCell.ErrorText.Length == 0 || outCell.ErrorText.StartsWith("Earlier than the truck time"))) outCell.ErrorText = m;
                    }
                    else if (oursOut) outCell.ErrorText = "";
                    foreach (var col in new[] { "colIn", "colOut" })
                    {
                        var cell = r.Cells[col];
                        if (col == "colOut" && backwards) continue;   // Nov 18: the time-in message above wins
                        bool ours = cell.ErrorText.StartsWith("Earlier than the truck time");
                        string? which = DailyRules.StoreTimeBeforePlantGate(tin, tout, cell.Value?.ToString());
                        bool early = which != null;
                        string msg = early ? DailyRules.StoreBeforeGateHint(which!, tin, tout) : "";
                        if (early) { if (cell.ErrorText != msg && (ours || cell.ErrorText.Length == 0)) cell.ErrorText = msg; }
                        else if (ours) cell.ErrorText = "";
                    }
                }
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        /// <summary>Shows the missing fields (if any). True when the form may be saved.</summary>
        private bool PassBlankFormChecks()
        {
            var list = BlankFormChecks();
            if (list.Count == 0) return true;
            Control? first = null;
            foreach (var (_, field) in list) first = EarlierStep(first, field);   // jump to the step of the first problem
            return ShowFormProblems(list.Select(x => x.Message).ToList(), first);
        }
    }
}
