using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 9, 2026: search, client, show and sort controls above the "Dispatch order" drop-down. They only decide which of the waiting trips
    /// the drop-down contains; nothing is saved. The trip that is picked always stays in the list.
    /// </summary>
    public partial class TripReportView
    {
        private sealed class PickRow
        {
            public Delivery D = null!;
            public string Text = "";
            public string Client = "";
            public DateTime Date;
            public bool Draft;
        }

        private List<PickRow> pickRows = new List<PickRow>();
        private TextBox? txtPickSearch;
        private ComboBox? cbPickClient, cbPickShow, cbPickSort;
        private Label? lblPickCount;
        private LinkLabel? lnkPickClear;
        private bool pickFilterBusy;

        private static string NormPick(string? s) => new string((s ?? "").Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();

        /// <summary>The row of controls (named "noDirty…" so typing in it never counts as unsaved changes on the form).</summary>
        private FlowLayoutPanel BuildPickFilterRow()
        {
            var row = new FlowLayoutPanel
            {
                Name = "noDirtyPickFilters",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = AppTheme.Surface,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 0, Dpi(6))
            };
            txtPickSearch = new TextBox { Width = Dpi(300), Margin = new Padding(0, 0, Dpi(8), Dpi(4)) };
            PageKit.StyleSearch(txtPickSearch, "Search dispatch #, client, route, driver…");
            cbPickClient = NewPickCombo(Dpi(210), "All clients");
            cbPickShow = NewPickCombo(Dpi(170), "All waiting", "No report yet", "Drafts to finish", "Special trips");
            cbPickSort = NewPickCombo(Dpi(150), "Oldest first", "Newest first", "Client A–Z", "Dispatch #");
            lblPickCount = new Label { AutoSize = true, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9F), Margin = new Padding(Dpi(6), Dpi(7), Dpi(6), 0), UseMnemonic = false };
            lnkPickClear = new LinkLabel { Text = "Clear", AutoSize = true, Visible = false, Margin = new Padding(0, Dpi(7), 0, 0), LinkColor = AppTheme.AccentText, ActiveLinkColor = AppTheme.AccentText, UseMnemonic = false };
            row.Controls.AddRange(new Control[] { txtPickSearch, cbPickClient, cbPickShow, cbPickSort, lblPickCount, lnkPickClear });

            void Changed(object? s, EventArgs e) { if (!pickFilterBusy) ApplyPickFilter(GetSelectedDelivery()?.DispatchNumber); }
            txtPickSearch.TextChanged += Changed;
            cbPickClient.SelectedIndexChanged += Changed;
            cbPickShow.SelectedIndexChanged += Changed;
            cbPickSort.SelectedIndexChanged += Changed;
            lnkPickClear.LinkClicked += (s, e) => { ResetPickControls(); ApplyPickFilter(GetSelectedDelivery()?.DispatchNumber); };
            txtPickSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && cbDelivery.Items.Count > 0 && cbDelivery.Enabled)
                {
                    cbDelivery.SelectedIndex = 0;   // the top match
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Down && cbDelivery.Items.Count > 0 && cbDelivery.Enabled)
                {
                    cbDelivery.Focus(); cbDelivery.DroppedDown = true;
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Escape && txtPickSearch.TextLength > 0)
                {
                    txtPickSearch.Clear();
                    e.SuppressKeyPress = true;
                }
            };
            return row;
        }

        private ComboBox NewPickCombo(int width, params string[] items)
        {
            var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = width, Margin = new Padding(0, 0, Dpi(8), Dpi(4)) };
            PageKit.StyleCombo(cb);
            cb.Items.AddRange(items);
            cb.SelectedIndex = 0;
            return cb;
        }

        private bool PickFilterActive()
            => (txtPickSearch?.TextLength ?? 0) > 0 || (cbPickClient?.SelectedIndex ?? 0) > 0 || (cbPickShow?.SelectedIndex ?? 0) > 0;

        /// <summary>Puts the controls back to: empty search, All clients, All waiting, Oldest first.</summary>
        private void ResetPickControls()
        {
            if (txtPickSearch == null || cbPickClient == null || cbPickShow == null || cbPickSort == null) return;
            bool was = pickFilterBusy;
            pickFilterBusy = true;
            try { txtPickSearch.Clear(); cbPickClient.SelectedIndex = 0; cbPickShow.SelectedIndex = 0; cbPickSort.SelectedIndex = 0; }
            finally { pickFilterBusy = was; }
        }

        /// <summary>Starting a new report: controls back to default and the list rebuilt from the waiting trips.</summary>
        private void ResetPickFilter()
        {
            if (txtPickSearch == null) return;
            ResetPickControls();
            if (pickRows.Count > 0) ApplyPickFilter(null);
            else UpdatePickCount(0);
        }

        private void SetPickControlsEnabled(bool on)
        {
            foreach (Control? c in new Control?[] { txtPickSearch, cbPickClient, cbPickShow, cbPickSort, lnkPickClear }) if (c != null) c.Enabled = on;
        }

        /// <summary>The client list holds only clients that have waiting trips; the chosen client stays chosen when it is still there.</summary>
        private void RefreshPickClients()
        {
            if (cbPickClient == null) return;
            string? current = cbPickClient.SelectedIndex > 0 ? cbPickClient.SelectedItem?.ToString() : null;
            bool was = pickFilterBusy;
            pickFilterBusy = true;
            try
            {
                cbPickClient.Items.Clear();
                cbPickClient.Items.Add("All clients");
                foreach (var c in pickRows.Select(r => r.Client).Where(c => c.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
                    cbPickClient.Items.Add(c);
                int idx = current == null ? 0 : cbPickClient.FindStringExact(current);
                cbPickClient.SelectedIndex = idx > 0 ? idx : 0;
            }
            finally { pickFilterBusy = was; }
        }

        /// <summary>Fills the drop-down from the waiting trips using the four controls. <paramref name="keepNo"/> (the picked trip) is always included and stays selected.</summary>
        private void ApplyPickFilter(string? keepNo)
        {
            if (txtPickSearch == null || cbPickClient == null || cbPickShow == null || cbPickSort == null) return;
            bool wasBinding = isBindingForm, wasBusy = pickFilterBusy;
            isBindingForm = true; pickFilterBusy = true;
            try
            {
                string q = NormPick(txtPickSearch.Text);
                string? client = cbPickClient.SelectedIndex > 0 ? cbPickClient.SelectedItem?.ToString() : null;
                int show = Math.Max(0, cbPickShow.SelectedIndex), sort = Math.Max(0, cbPickSort.SelectedIndex);
                bool IsKeep(PickRow r) => keepNo != null && string.Equals(r.D.DispatchNumber, keepNo, StringComparison.OrdinalIgnoreCase);
                bool Match(PickRow r) =>
                    (client == null || string.Equals(r.Client, client, StringComparison.OrdinalIgnoreCase))
                    && (show == 0 || (show == 1 && !r.Draft) || (show == 2 && r.Draft) || (show == 3 && r.D.Id < 0))
                    && (q.Length == 0 || NormPick(r.Text).Contains(q));
                var rows = pickRows.Where(r => IsKeep(r) || Match(r));
                var no = StringComparer.OrdinalIgnoreCase;
                IEnumerable<PickRow> sorted = sort switch
                {
                    1 => rows.OrderByDescending(r => r.Date).ThenBy(r => r.D.DispatchNumber, no),
                    2 => rows.OrderBy(r => r.Client, no).ThenBy(r => r.Date).ThenBy(r => r.D.DispatchNumber, no),
                    3 => rows.OrderBy(r => r.D.DispatchNumber, no),
                    _ => rows.OrderBy(r => r.Date).ThenBy(r => r.D.DispatchNumber, no)
                };
                cbDelivery.Items.Clear();
                pickItemsAdded.Clear();
                foreach (var r in sorted) cbDelivery.Items.Add(r.Text);
                cbDelivery.SelectedIndex = keepNo == null ? -1 : FindDeliveryItemIndex(keepNo);
                UpdatePickCount(pickRows.Count(Match));
            }
            finally { pickFilterBusy = wasBusy; isBindingForm = wasBinding; }
        }

        private void UpdatePickCount(int matched)
        {
            if (lblPickCount == null || lnkPickClear == null) return;
            bool active = PickFilterActive();
            lblPickCount.Text = pickRows.Count == 0 ? ""
                : matched == 0 ? "No trip matches."
                : active ? $"{matched} of {pickRows.Count} waiting"
                : $"{pickRows.Count} waiting";
            lnkPickClear.Visible = active;
        }
    }
}
