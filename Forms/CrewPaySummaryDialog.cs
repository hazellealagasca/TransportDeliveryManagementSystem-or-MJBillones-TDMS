using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// Nov 6 workflow (D6): the crew pay summary that used to sit in Reports. Pay saved for a trip wins; trips without saved
    /// pay use the crew member's standard rates. Internal only: it lives in Crew Management and never reaches client documents.
    /// </summary>
    public sealed class CrewPaySummaryDialog : Form
    {
        private readonly DataGridView grid;
        private readonly Label lblSummary;
        private readonly Dictionary<int, (Crew Crew, List<Delivery> Trips)> rows = new();

        public CrewPaySummaryDialog()
        {
            Text = "Crew pay summary";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false; ShowIcon = false; ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = AppTheme.Surface; ForeColor = AppTheme.TextPrimary; Font = new Font("Segoe UI", 9.5F);
            int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            Size = new Size(D(960), D(520));
            try { ThemeManager.StyleWindowChrome(this); } catch (Exception ex) { ErrLog.Swallowed(ex); }

            var hint = new Label
            {
                Dock = DockStyle.Top, Height = D(40), Padding = new Padding(D(12), D(10), D(12), 0), ForeColor = AppTheme.TextSecondary,
                Text = "Completed trips per crew member. Internal record only (not billed to the client)."
            };
            grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                BackgroundColor = AppTheme.Surface, BorderStyle = BorderStyle.FixedSingle
            };
            UIThemeHelper.ApplyGridTheme(grid);
            void Col(string name, string header, int weight, bool money = false) => grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name, HeaderText = header, FillWeight = weight,
                DefaultCellStyle = money ? new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } : new DataGridViewCellStyle()
            });
            Col("colName", "Crew member", 150); Col("colRole", "Role", 70); Col("colTrips", "Completed trips", 70);
            Col("colRate", "Daily wage", 80, true); Col("colWages", "Total wages", 90, true);
            Col("colAllowRate", "Meal allowance", 80, true); Col("colAllow", "Total allowance", 90, true); Col("colTotal", "Total pay", 90, true);

            lblSummary = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 10F) };
            var btnPrint = new Button { Text = "📄  Print pay record", AutoSize = true, MinimumSize = new Size(D(150), D(34)), Cursor = Cursors.Hand, Margin = new Padding(0, 0, D(8), 0) };
            var btnClose = new Button { Text = "Close", AutoSize = true, MinimumSize = new Size(D(90), D(34)), Cursor = Cursors.Hand, DialogResult = DialogResult.Cancel };
            UIThemeHelper.SetButtonRole(btnPrint, "primary");
            UIThemeHelper.SetButtonRole(btnClose, "secondary");
            btnPrint.Click += (s, e) => PrintSelected();
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, D(8), D(8), 0) };
            buttons.Controls.Add(btnPrint); buttons.Controls.Add(btnClose);
            var footer = new Panel { Dock = DockStyle.Bottom, Height = D(54), Padding = new Padding(D(12), 0, 0, 0), BackColor = AppTheme.Surface };
            footer.Controls.Add(lblSummary); footer.Controls.Add(buttons);
            Controls.Add(grid); Controls.Add(footer); Controls.Add(hint);
            CancelButton = btnClose;
            Load += async (s, e) => await LoadSummary();
        }

        private static bool OnTrip(Delivery d, int crewId) =>
            d.DriverId == crewId ||
            (!string.IsNullOrEmpty(d.AideIds) && d.AideIds.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Contains(crewId.ToString()));

        private async Task LoadSummary()
        {
            try
            {
                var crews = await MJdatabase.GetCrewsAsync();
                var completed = (await MJdatabase.GetDeliveriesAsync()).Where(d => d.Status == "Completed").ToList();
                var recorded = await Task.Run(() => MJdatabase.GetAllTripCrewPay());
                if (IsDisposed) return;
                grid.Rows.Clear(); rows.Clear();
                decimal totalPay = 0; int totalTrips = 0;
                foreach (var c in crews)
                {
                    var trips = completed.Where(d => OnTrip(d, c.Id)).ToList();
                    var pay = trips.Select(t => MJdatabase.ResolveTripPay(c, t, recorded)).ToList();
                    decimal wages = pay.Sum(p => p.BasePay), allow = pay.Sum(p => p.Allowance);
                    int r = grid.Rows.Add(c.FullName, c.Role, trips.Count, c.DailyWage, wages, c.FoodAllowance, allow, wages + allow);
                    rows[r] = (c, trips);
                    totalPay += wages + allow; totalTrips += trips.Count;
                }
                lblSummary.Text = $"{crews.Count:N0} crew · {totalTrips:N0} trips · Total crew pay PHP {totalPay:N2}";
            }
            catch (Exception ex)
            {
                ErrLog.Error("CrewPaySummaryDialog", ex);
                lblSummary.Text = "The crew pay summary could not be loaded: " + ex.Message;
            }
        }

        private void PrintSelected()
        {
            if (grid.CurrentRow == null || !rows.TryGetValue(grid.CurrentRow.Index, out var row))
            {
                QuietBox.Show("Select a crew member first.", "Print pay record", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DateTime from = row.Trips.Count > 0 ? row.Trips.Min(t => t.DispatchDate) : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            PrintHelper.PrintCrewPaySlip(row.Crew, row.Trips, from, DateTime.Today);
        }
    }
}
