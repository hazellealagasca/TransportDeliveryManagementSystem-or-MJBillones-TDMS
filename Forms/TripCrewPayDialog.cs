using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// Nov 6 workflow: the pay recorded for each crew member on one trip (daily wage + meal allowance). Internal documentation
    /// for M.J. Billones only. It lives in Resources → Crew Trip History and never reaches an invoice, SOA or receipt.
    /// </summary>
    public sealed class TripCrewPayDialog : Form
    {
        private readonly Delivery trip;
        private readonly DataGridView grid;
        private readonly Label lblTotal;
        private readonly List<Crew> crews;
        private bool binding;

        public TripCrewPayDialog(Delivery trip)
        {
            this.trip = trip;
            try { crews = MJdatabase.GetCrews(); } catch (Exception ex) { ErrLog.Swallowed(ex); crews = new List<Crew>(); }
            Text = $"Crew pay · {trip.DispatchNumber}";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false; ShowIcon = false; ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = AppTheme.Surface; ForeColor = AppTheme.TextPrimary; Font = new Font("Segoe UI", 9.5F);
            int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            Size = new Size(D(780), D(420));
            try { ThemeManager.StyleWindowChrome(this); } catch (Exception ex) { ErrLog.Swallowed(ex); }

            var hint = new Label
            {
                Dock = DockStyle.Top, Height = D(48), Padding = new Padding(D(12), D(8), D(12), 0), ForeColor = AppTheme.TextSecondary,
                Text = $"{trip.DispatchNumber} · {trip.DispatchDate:MMM d, yyyy} · {trip.ClientName}\nStarts from each person's daily wage and meal allowance in Crew Management. Internal record only (not billed to the client)."
            };
            grid = new DataGridView
            {
                Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.CellSelect,
                BackgroundColor = AppTheme.Surface, BorderStyle = BorderStyle.FixedSingle, EditMode = DataGridViewEditMode.EditOnEnter
            };
            UIThemeHelper.ApplyGridTheme(grid);
            DataGridViewTextBoxColumn Money(string name, string header, bool ro = false) => new DataGridViewTextBoxColumn
            { Name = name, HeaderText = header, FillWeight = 70, ReadOnly = ro, DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } };
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRole", HeaderText = "Role", FillWeight = 55, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Name", FillWeight = 150, ReadOnly = true });
            grid.Columns.Add(Money("colBase", "Daily wage"));
            grid.Columns.Add(Money("colAllow", "Meal allowance"));
            grid.Columns.Add(Money("colNet", "Total", ro: true));
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNotes", HeaderText = "Notes", FillWeight = 110 });
            grid.CellValidating += (s, e) =>
            {
                string col = grid.Columns[e.ColumnIndex].Name;
                if (col is not ("colBase" or "colAllow")) return;
                string txt = (e.FormattedValue?.ToString() ?? "").Replace(",", "").Trim();
                if (txt.Length > 0 && (!decimal.TryParse(txt, out var v) || v < 0)) { grid.Rows[e.RowIndex].ErrorText = "Enter an amount of 0 or more, e.g. 650 or 650.50"; e.Cancel = true; }
                else grid.Rows[e.RowIndex].ErrorText = "";
            };
            grid.CellParsing += (s, e) =>
            {
                string col = grid.Columns[e.ColumnIndex].Name;
                if (col is ("colBase" or "colAllow") && decimal.TryParse((e.Value?.ToString() ?? "").Replace(",", ""), out var v)) { e.Value = v; e.ParsingApplied = true; }
            };
            grid.DataError += (s, e) => { e.ThrowException = false; };
            grid.CellValueChanged += (s, e) => { if (!binding && e.RowIndex >= 0) Recalculate(); };

            lblTotal = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 10F) };
            var btnReset = new Button { Text = "↺  Use standard rates", AutoSize = true, MinimumSize = new Size(D(150), D(34)), Cursor = Cursors.Hand, Margin = new Padding(0, 0, D(8), 0) };
            var btnSave = new Button { Text = "💾  Save crew pay", AutoSize = true, MinimumSize = new Size(D(140), D(34)), Cursor = Cursors.Hand, Margin = new Padding(0, 0, D(8), 0) };
            var btnClose = new Button { Text = "Close", AutoSize = true, MinimumSize = new Size(D(90), D(34)), Cursor = Cursors.Hand, DialogResult = DialogResult.Cancel };
            btnClose.Click += (s, e) => Close();
            UIThemeHelper.SetButtonRole(btnReset, "secondary");
            UIThemeHelper.SetButtonRole(btnSave, "primary");
            UIThemeHelper.SetButtonRole(btnClose, "secondary");
            btnReset.Click += (s, e) => ResetToStandardRates();
            btnSave.Click += (s, e) => Save();
            // Same footer as the profile windows: buttons docked right with fixed widths, the total fills the rest.
            var footer = new Panel { Dock = DockStyle.Bottom, Height = D(60), BackColor = AppTheme.Surface, Padding = new Padding(D(16), D(12), D(16), D(12)) };
            footer.Paint += (s, e) => { using var p = new Pen(AppTheme.Border); e.Graphics.DrawLine(p, 0, 0, footer.Width, 0); };
            footer.Controls.Add(lblTotal);
            btnReset.AutoSize = false; btnReset.MinimumSize = Size.Empty; btnReset.Dock = DockStyle.Right; btnReset.Width = D(180); btnReset.Margin = Padding.Empty;
            footer.Controls.Add(btnReset);
            footer.Controls.Add(new Panel { Dock = DockStyle.Right, Width = D(8), BackColor = AppTheme.Surface });
            btnSave.AutoSize = false; btnSave.MinimumSize = Size.Empty; btnSave.Dock = DockStyle.Right; btnSave.Width = D(160); btnSave.Margin = Padding.Empty;
            footer.Controls.Add(btnSave);
            footer.Controls.Add(new Panel { Dock = DockStyle.Right, Width = D(8), BackColor = AppTheme.Surface });
            btnClose.AutoSize = false; btnClose.MinimumSize = Size.Empty; btnClose.Dock = DockStyle.Right; btnClose.Width = D(100); btnClose.Margin = Padding.Empty;
            footer.Controls.Add(btnClose);
            Controls.Add(grid); Controls.Add(footer); Controls.Add(hint);
            CancelButton = btnClose;
            Load += (s, e) => Bind();
        }

        private static decimal CellMoney(DataGridViewRow row, string col)
        {
            var v = row.Cells[col].Value;
            if (v is decimal m) return m;
            return decimal.TryParse((v?.ToString() ?? "").Replace(",", ""), out var p) ? p : 0m;
        }

        private void Bind()
        {
            binding = true;
            try
            {
                grid.Rows.Clear();
                var recorded = MJdatabase.GetTripCrewPay(trip.Id).GroupBy(p => p.CrewId).ToDictionary(g => g.Key, g => g.First());
                foreach (var (crewId, role) in MJdatabase.GetDeliveryCrewIds(trip))
                {
                    var crew = crews.FirstOrDefault(c => c.Id == crewId);
                    var p = recorded.TryGetValue(crewId, out var saved) ? saved
                        : new TripCrewPay { DeliveryId = trip.Id, CrewId = crewId, BasePay = crew?.DailyWage ?? 0, Allowance = crew?.FoodAllowance ?? 0 };
                    p.CrewName = crew?.FullName ?? (role == "Driver" ? trip.DriverName : $"Aide #{crewId}");
                    p.Role = role;
                    int r = grid.Rows.Add(role, p.CrewName, p.BasePay, p.Allowance, p.NetPay, p.Notes);
                    grid.Rows[r].Tag = p;
                }
            }
            finally { binding = false; }
            Recalculate();
        }

        private void Recalculate()
        {
            decimal total = 0;
            binding = true;
            try
            {
                foreach (DataGridViewRow row in grid.Rows)
                {
                    decimal net = CellMoney(row, "colBase") + CellMoney(row, "colAllow");
                    row.Cells["colNet"].Value = net;
                    total += net;
                }
            }
            finally { binding = false; }
            lblTotal.Text = grid.Rows.Count == 0 ? "No driver or aides are assigned to this dispatch." : $"Total crew pay for this trip:  PHP {total:N2}";
        }

        private void ResetToStandardRates()
        {
            grid.EndEdit();
            binding = true;
            try
            {
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.Tag is not TripCrewPay p) continue;
                    var crew = crews.FirstOrDefault(c => c.Id == p.CrewId);
                    row.Cells["colBase"].Value = crew?.DailyWage ?? 0m;
                    row.Cells["colAllow"].Value = crew?.FoodAllowance ?? 0m;
                }
            }
            finally { binding = false; }
            Recalculate();
        }

        private void Save()
        {
            grid.EndEdit();
            var list = new List<TripCrewPay>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Tag is not TripCrewPay src) continue;
                list.Add(new TripCrewPay
                {
                    DeliveryId = trip.Id, CrewId = src.CrewId, CrewName = src.CrewName, Role = src.Role,
                    BasePay = CellMoney(row, "colBase"), Allowance = CellMoney(row, "colAllow"),
                    Notes = row.Cells["colNotes"].Value?.ToString()?.Trim() ?? ""
                });
            }
            try
            {
                MJdatabase.SaveTripCrewPay(trip.Id, list);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ErrLog.Error("TripCrewPayDialog", ex);
                MessageBox.Show(this, "The crew pay could not be saved: " + ex.Message, "Crew pay", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
