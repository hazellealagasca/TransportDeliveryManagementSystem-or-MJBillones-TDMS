using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Asks "Are you sure?" (Yes / No) before any Add, Save, Update or Reset/Clear button does its work, across every
    /// screen. It wraps the button's own Click handlers, so the button's code only runs after a Yes. Reset / Clear
    /// buttons also wipe the screen's temporary search boxes and filters after a Yes, so the page is back to default.
    /// Delete buttons are not wrapped here because each delete already asks first (with what will happen).
    /// </summary>
    public static class ConfirmGate
    {
        private enum Kind { None, Add, Update, Save, Reset }
        private static readonly object? ClickKey =
            typeof(Control).GetField("s_clickEvent", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
            ?? typeof(Control).GetField("EventClick", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
        private static readonly PropertyInfo? EventsProp = typeof(Component).GetProperty("Events", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly ConditionalWeakTable<Button, Gate> gates = new ConditionalWeakTable<Button, Gate>();
        /// <summary>Set while a gated handler runs, so code that asks its own question can skip a second prompt.</summary>
        [ThreadStatic] public static bool InsideConfirmedAction;
        private static readonly string[] NotData = { "file", "print", "pdf", "export", "layout", "draft", "remember", "copy to", "browser", "search", "filter" };
        /// <summary>Watches every button under <paramref name="root"/> (called from the theme pass, which every screen runs).</summary>
        public static void Watch(Control root)
        {
            if (root == null || ClickKey == null || EventsProp == null) return;
            if (root is Button b) Attach(b);
            foreach (Control c in root.Controls) Watch(c);
        }
        private static void Attach(Button b)
        {
            if (gates.TryGetValue(b, out _)) return;
            var gate = new Gate(b);
            gates.Add(b, gate);
            gate.Wrap();
            // Handlers can be added after the theme pass; pick them up just before the person can click or press.
            b.MouseEnter += (s, e) => gate.Wrap();
            b.GotFocus += (s, e) => gate.Wrap();
            b.VisibleChanged += (s, e) => gate.Wrap();
        }
        private static Kind KindOf(Button b)
        {
            string name = b.Name ?? "";
            string text = Clean(b.Text);
            if (text.Length == 0 && name.Length == 0) return Kind.None;
            if (NotData.Any(w => text.Contains(w))) return Kind.None;
            if (text.Contains("delete") || text.Contains("remove") || text.Contains("cancel") || text.Contains("close") || text.Contains("back")) return Kind.None;
            if (text.Contains("reset to default") || text.Contains("restore default") || text.Contains("reset form") || text.Contains("clear form") || text == "clear" || text == "reset"
                || name.StartsWith("btnClear", StringComparison.OrdinalIgnoreCase) || name.StartsWith("btnReset", StringComparison.OrdinalIgnoreCase) || name.Equals("btnRestoreDefaults", StringComparison.OrdinalIgnoreCase))
                return Kind.Reset;
            bool saveName = name.Equals("btnSave", StringComparison.OrdinalIgnoreCase) || name.Equals("btnSubmit", StringComparison.OrdinalIgnoreCase);
            bool updateName = name.Equals("btnUpdate", StringComparison.OrdinalIgnoreCase);
            bool addName = name.Equals("btnAdd", StringComparison.OrdinalIgnoreCase);
            if (updateName || (saveName && text.StartsWith("update"))) return Kind.Update;
            if (saveName && (text.StartsWith("add") || text.StartsWith("register") || text.StartsWith("create") || text.Contains("as new") || text.StartsWith("record") || text.StartsWith("submit"))) return Kind.Add;
            if (saveName) return Kind.Save;
            if (addName && (b.Text.Contains("💾") || text.StartsWith("register") || text.StartsWith("save"))) return Kind.Add;
            return Kind.None;
        }
        private static string Clean(string? t)
        {
            string s = Regex.Replace(t ?? "", @"[^\p{L}\p{N}\s/&-]", " ");
            return Regex.Replace(s, @"\s+", " ").Trim().ToLowerInvariant();
        }
        private static bool Ask(Button b, Kind kind)
        {
            string what = Clean(b.Text);
            if (what.Length == 0) what = kind switch { Kind.Add => "add this record", Kind.Update => "update this record", Kind.Reset => "reset", _ => "save" };
            string question, title;
            switch (kind)
            {
                case Kind.Reset:
                    question = $"Are you sure you want to {what}?\n\nEverything typed into the form, plus any search text and temporary filters on this screen, will be cleared and the screen goes back to its default layout.";
                    title = "Confirm reset";
                    break;
                case Kind.Add:
                    question = $"Are you sure you want to {what}?\n\nA new record will be added.";
                    title = "Confirm add";
                    break;
                case Kind.Update:
                    question = $"Are you sure you want to {what}?\n\nThe existing record will be changed.";
                    title = "Confirm update";
                    break;
                default:
                    question = $"Are you sure you want to {what}?";
                    title = "Confirm save";
                    break;
            }
            var owner = b.FindForm();
            return MessageBox.Show(owner, question, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, kind == Kind.Reset ? MessageBoxDefaultButton.Button2 : MessageBoxDefaultButton.Button1) == DialogResult.Yes;
        }
        /// <summary>After a confirmed reset: clear the screen's search boxes and put filter pickers back on their first choice.</summary>
        private static void ResetTemporaryFilters(Button b)
        {
            Control? scope = b.Parent;
            while (scope != null && scope is not UserControl && scope is not Form) scope = scope.Parent;
            if (scope == null) return;
            foreach (var c in All(scope))
            {
                string n = c.Name ?? "";
                if (c is TextBox t && (n.StartsWith("txtSearch", StringComparison.OrdinalIgnoreCase) || n.StartsWith("txtFilter", StringComparison.OrdinalIgnoreCase)))
                {
                    if (t.TextLength > 0) t.Clear();
                }
                else if (c is ComboBox cb && cb.Items.Count > 0 && (n.StartsWith("cbFilter", StringComparison.OrdinalIgnoreCase) || n.StartsWith("cbStatusFilter", StringComparison.OrdinalIgnoreCase) || n.StartsWith("cmbFilter", StringComparison.OrdinalIgnoreCase)))
                {
                    if (cb.SelectedIndex != 0) cb.SelectedIndex = 0;
                }
            }
        }
        private static IEnumerable<Control> All(Control root)
        {
            foreach (Control c in root.Controls)
            {
                yield return c;
                foreach (var d in All(c)) yield return d;
            }
        }
        private sealed class Gate
        {
            private readonly Button button;
            private readonly List<Delegate> originals = new List<Delegate>();
            private readonly EventHandler proxy;
            private bool running;
            public Gate(Button b) { button = b; proxy = OnClick; }
            /// <summary>Moves any Click handlers that aren't ours behind the confirmation.</summary>
            public void Wrap()
            {
                try
                {
                    if (EventsProp?.GetValue(button) is not EventHandlerList list || ClickKey == null) return;
                    var current = list[ClickKey];
                    if (current == null) return;
                    bool changed = false;
                    foreach (var d in current.GetInvocationList())
                    {
                        if (d.Equals(proxy)) continue;
                        list.RemoveHandler(ClickKey, d);
                        originals.Add(d);
                        changed = true;
                    }
                    if (changed || !current.GetInvocationList().Any(d => d.Equals(proxy)))
                    {
                        list.RemoveHandler(ClickKey, proxy);
                        list.AddHandler(ClickKey, proxy);
                    }
                }
                catch { }
            }
            private void OnClick(object? sender, EventArgs e)
            {
                if (running) return;
                var kind = KindOf(button);
                if (kind != Kind.None && !InsideConfirmedAction && !Ask(button, kind)) return;
                running = true;
                bool outer = InsideConfirmedAction;
                InsideConfirmedAction = kind != Kind.None || outer;
                try
                {
                    foreach (var d in originals.ToList()) ((EventHandler)d)(sender, e);
                    if (kind == Kind.Reset) ResetTemporaryFilters(button);
                }
                finally
                {
                    running = false;
                    InsideConfirmedAction = outer;
                }
            }
        }
    }
}
