using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// Nov 6, 2026: the special trip form laid out like the New Dispatch Order form: label-above-box rows in two columns,
    /// grouped in numbered cards (Client &amp; destination, Stops, Truck &amp; crew, Schedule &amp; status, Steps for this trip).
    /// </summary>
    public partial class SpecialTripFormDialog
    {
        private Label FieldLabel(string text) => new Label
        {
            Text = text, AutoSize = true, Font = FontCache.Get("Segoe UI Semibold", 9F, FontStyle.Regular), ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(0, Px(6), Px(12), Px(4)), UseMnemonic = false
        };
        /// <summary>Detaches a box from its old wrapper and makes it fill its cell.</summary>
        private Control Field(Control c, int height = 0)
        {
            c.Parent?.Controls.Remove(c);
            c.Dock = DockStyle.Fill;
            c.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            c.Margin = new Padding(0, 0, Px(12), Px(10));
            if (height > 0) c.MinimumSize = new Size(0, height);
            return c;
        }
        /// <summary>Two fields side by side, each with its label above it (the dispatch form's row).</summary>
        private TableLayoutPanel Row2(string l1, Control c1, string? l2 = null, Control? c2 = null)
        {
            var t = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 2, Margin = new Padding(0), Padding = new Padding(0), BackColor = AppTheme.Surface };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.Controls.Add(FieldLabel(l1), 0, 0);
            t.Controls.Add(Field(c1), 0, 1);
            if (l2 != null && c2 != null)
            {
                t.Controls.Add(FieldLabel(l2), 1, 0);
                t.Controls.Add(Field(c2), 1, 1);
            }
            return t;
        }
        /// <summary>One field across the whole width.</summary>
        private TableLayoutPanel Row1(Control label, Control c, int height = 0)
        {
            var t = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, RowCount = 2, Margin = new Padding(0), Padding = new Padding(0), BackColor = AppTheme.Surface };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            label.Parent?.Controls.Remove(label);
            if (label is Label lb)
            {
                lb.AutoSize = true; lb.Dock = DockStyle.None; lb.Location = Point.Empty;
                lb.Font = FontCache.Get("Segoe UI Semibold", 9F, FontStyle.Regular); lb.ForeColor = AppTheme.TextSecondary;
                lb.Margin = new Padding(0, Px(6), 0, Px(4));
            }
            t.Controls.Add(label, 0, 0);
            var f = Field(c, height);
            if (height > 0) { f.Dock = DockStyle.Top; f.Height = height; f.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top; }
            t.Controls.Add(f, 0, 1);
            return t;
        }

        private void BuildDispatchStyleLayout(FlowLayoutPanel actions, Panel tableSection, Panel ticksBox)
        {
            // 1 Client & destination
            var r1 = Row2("Order reference", txtOrderRef, "Company *", cbCompany);
            var r2 = Row1(FieldLabel("Client address (billing)"), txtAddress);
            var r3 = Row1(FieldLabel("Destination address *  (where the truck goes)"), txtDestination);
            var r4 = Row2("Trip charge (PHP) *", numTripCharge, "Mode of payment *", cbPayment);
            // 2 Stops: the flexible table as it was, a little taller.
            tableSection.Height = Px(300);
            tableSection.Padding = new Padding(0, 0, 0, Px(4));
            // 3 Truck & crew
            var r5 = Row2("Truck", cbTruck, "Driver", cbDriver);
            var lblAides = lblAidesRef ?? FieldLabel("Aides");
            var r6 = Row1(lblAides, clbAides, Px(150));
            // 4 Schedule & status
            var r7 = Row2("Dispatch date *", dtpDispatchDate, "Departure time *", cbDepartureTime);
            var r8 = Row2("Status", cbStatus);
            // 5 Steps for this trip: the four ticks in two columns.
            ticksBox.BorderStyle = BorderStyle.None;
            ticksBox.BackColor = AppTheme.Surface;
            ticksBox.Padding = new Padding(0);
            ticksBox.Height = Px(150);
            var checks = new[] { chkPaymentWithReport, chkWithInvoice, chkWithSOA, chkWithCollection };
            int colW = Px(400);
            for (int i = 0; i < checks.Length; i++) checks[i].Location = new Point((i % 2) * (colW + Px(12)), Px(4) + (i / 2) * Px(48));
            foreach (Control c in ticksBox.Controls.OfType<Label>().ToList())
            {
                if (c == lblChecklistNote) { c.Location = new Point(0, Px(104)); c.Size = new Size(colW * 2 + Px(12), Px(36)); c.ForeColor = AppTheme.TextMuted; }
                else c.Visible = false; // the old "Checklist" heading: the card title says it now
            }
            BuildFullPage(actions,
                ("Client & destination", "Who the trip is for, where the truck goes and what it is charged.", Stack(r1, r2, r3, r4)),
                ("Stops / delivery details", "One line per stop. Double-click the table title to change its columns.", tableSection),
                ("Truck & crew", "Assign the truck, the driver and the helpers for this trip.", Stack(r5, r6)),
                ("Schedule & status", "When the truck leaves and where the trip stands.", Stack(r7, r8)),
                ("Steps for this trip", "Tick what this trip needs after it is completed. Unticked steps are not shown and are skipped.", ticksBox));
        }
        /// <summary>Rows stacked top to bottom in one block.</summary>
        private Panel Stack(params Control[] rows)
        {
            var p = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            for (int i = rows.Length - 1; i >= 0; i--) { rows[i].Dock = DockStyle.Top; p.Controls.Add(rows[i]); }
            return p;
        }
    }
}
