using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Display size: zooming and returning to 100 % restores every size, and zooming never changes a value.</summary>
public static partial class Checks
{
    private static void Snapshot(Control root, List<string> sizes, List<string> values)
    {
        foreach (Control c in root.Controls)
        {
            if (!c.Visible) continue; // hidden controls re-measure themselves when they are shown again
            sizes.Add($"{c.GetType().Name}:{c.Name}:{c.Size.Width}x{c.Size.Height}:{c.Font.Size}:{c.Padding}:{c.Margin}");
            switch (c)
            {
                case TextBox t: values.Add($"{c.Name}={t.Text}"); break;
                case ComboBox cb: values.Add($"{c.Name}={cb.Text}|{cb.SelectedIndex}"); break;
                case NumericUpDown n: values.Add($"{c.Name}={n.Value}"); break;
                case DateTimePicker d: values.Add($"{c.Name}={d.Value:O}"); break;
                case DataGridView g: values.Add($"{c.Name}:rows={g.Rows.Count}"); break;
            }
            Snapshot(c, sizes, values);
        }
    }

    [Check]
    public static string UiZoom_RoundTrip_RestoresEverySize_AndChangesNoValue()
    {
        Fresh(); RunBackfill();
        return RunOnSta(() =>
        {
            UiZoom.PersistToDisk = false;
            try
            {
                string dbg = ""; UiZoom.Set(1f);
                using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(1920, 1000), Opacity = 0 };
                var hub = new FinanceHubView("Collections") { Dock = DockStyle.Fill };
                host.Controls.Add(hub); host.Show(); Pump(1500);
                var coll = FindFirst<CollectionView>(hub);
                if (coll == null) return "receipts screen not found";
                coll.GetType().GetMethod("OpenNewRecordForm", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(coll, null);
                Pump(500);
                UiZoom.Apply(coll); // captures the originals (100 %)
                Func<string> kids = () => string.Join(",", FieldOf<Panel>(coll, "pnlForm")!.Controls.Cast<Control>().Select(k => $"{k.Name}:{k.Dock}:{k.Visible}:{k.Width}x{k.Height}")); string k0 = kids();
                var s0 = new List<string>(); var v0 = new List<string>(); Snapshot(coll, s0, v0);
                UiZoom.Set(1.5f); UiZoom.Apply(coll); Pump(300);
                var s1 = new List<string>(); var v1 = new List<string>(); Snapshot(coll, s1, v1);
                bool grew = s0.Where((x, i) => x != s1[i]).Count() > s0.Count / 4;
                var tbl = FindFirst<TableLayoutPanel>(coll)!; string m150 = tbl.Margin.ToString(); UiZoom.Set(1f); string mAfterSet = tbl.Margin.ToString(); UiZoom.Apply(coll); Pump(300); string mAfterApply = tbl.Margin.ToString(); dbg = $"tblMargin 150:{m150} afterSet1:{mAfterSet} afterApply:{mAfterApply} factor {UiZoom.Factor}";
                string k2 = kids(); var s2 = new List<string>(); var v2 = new List<string>(); Snapshot(coll, s2, v2);
                host.Close();
                if (s0.Count != s2.Count) return $"control count changed {s0.Count} -> {s2.Count}";
                var diff = s0.Select((x, i) => (x, y: s2[i])).Where(p => p.x != p.y).Take(8).Select(p => p.x + " => " + p.y).ToList();
                if (diff.Count > 0) return "100 % did not restore: " + string.Join(" | ", diff.Take(0)) + " || " + string.Join(" ;; ", s0.Select((x, i) => (x, y: s2[i])).Where(p => p.x != p.y).Select(p => { var a = p.x.Split(':'); var b2 = p.y.Split(':'); string d = a[1] + "/" + a[0]; if (a[2] != b2[2]) d += " size " + a[2] + ">" + b2[2]; if (a[3] != b2[3]) d += " font " + a[3] + ">" + b2[3]; if (a[4] != b2[4]) d += " pad"; if (a[5] != b2[5]) d += " margin " + a[5] + ">" + b2[5]; return d; }).Take(14));
                if (!v0.SequenceEqual(v1) || !v0.SequenceEqual(v2)) return "a value changed while zooming";
                return grew ? "" : "150 % did not change the sizes";
            }
            finally { UiZoom.Set(1f); UiZoom.PersistToDisk = true; }
        });
    }

    [Check]
    public static string UiZoom_SimpleControls_ScaleAndRestore()
    {
        return RunOnSta(() =>
        {
            UiZoom.PersistToDisk = false;
            try
            {
                UiZoom.Set(1f);
                using var host = new Form { ClientSize = new Size(800, 600), ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), Opacity = 0 };
                var root = new Panel { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };
                var bar = new Panel { Dock = DockStyle.Top, Height = 40, Margin = new Padding(4), Padding = new Padding(6) };
                var lbl = new Label { Text = "x", Margin = new Padding(4), AutoSize = true, Dock = DockStyle.Top };
                var box = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F) };
                root.Controls.Add(box); root.Controls.Add(lbl); root.Controls.Add(bar); host.Controls.Add(root); host.Show(); Pump(200);
                UiZoom.Apply(root);
                string a0 = $"{bar.Height}/{bar.Margin.Left}/{bar.Padding.Left}/{box.Font.Size}";
                UiZoom.Set(1.5f); UiZoom.Apply(root);
                string a1 = $"{bar.Height}/{bar.Margin.Left}/{bar.Padding.Left}/{box.Font.Size}";
                UiZoom.Set(1f); UiZoom.Apply(root);
                string a2 = $"{bar.Height}/{bar.Margin.Left}/{bar.Padding.Left}/{box.Font.Size}";
                host.Close();
                return a0 == "40/4/6/9" && a1 == "60/6/9/13.5" && a2 == a0 ? "" : $"100%:{a0}  150%:{a1}  back:{a2}";
            }
            finally { UiZoom.Set(1f); UiZoom.PersistToDisk = true; }
        });
    }

    [Check]
    public static string Workbench_KeepsEveryFieldAndTheNumberGuardUnderItsField()
    {
        Fresh(); RunBackfill();
        return RunOnSta(() =>
        {
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(1920, 1000), Opacity = 0 };
            var hub = new FinanceHubView("Billing") { Dock = DockStyle.Fill };
            host.Controls.Add(hub); host.Show(); Pump(1200);
            var bill = FindFirst<BillingView>(hub);
            if (bill == null) return "billing screen not found";
            hub.SelectTab("Collections"); Pump(800);
            var coll = FindFirst<CollectionView>(hub);
            if (coll == null) return "receipts screen not found";
            hub.SelectTab("StatementOfAccount"); Pump(800);
            var soaView = FindFirst<StatementOfAccountView>(hub);
            if (soaView == null) return "statement screen not found";
            var missing = new List<string>();
            foreach (var (view, names) in new (Control, string[])[] {
                (bill, new[] { "txtInvoiceNumber", "cbDelivery", "txtLoadingSchedule", "cbClient", "numTotalAmount", "numFuel", "pnlNetDue", "tblAmounts" }),
                (soaView, new[] { "cbClient", "cbSoaType", "dtpIssueDate", "dtpPeriodFrom", "dtpPeriodTo", "gridPicker", "gridItems", "numTotalSales", "numVat", "numNetOfVat", "numFuelSurchargeTotal", "numWithholdingTax", "numTotalAmountDue", "txtPreparedBy", "txtReceivedBy1", "txtReceivedBy2", "txtSalesInvoiceNo", "btnAddSelected", "btnPickAllReady" }),
                (coll, new[] { "cbInvoice", "txtReceiptNo", "numAmountPaid", "cbPaymentMethod", "dtpPaymentDate", "txtRefNo", "txtNotes", "pnlLinkedInvoice" }) })
                foreach (var n in names)
                {
                    var f = view.GetType().GetField(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    if (f?.GetValue(view) is not Control c || c.IsDisposed) missing.Add(view.GetType().Name + "." + n);
                }
            // the number guard's message and tick stay in the same card as their number box
            var billNo = FieldOf<TextBox>(bill, "txtInvoiceNumber"); var billGuard = bill.GetType().GetField("billNumberGuard", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(bill) as NumberFieldGuard;
            var recNo = FieldOf<TextBox>(coll, "txtReceiptNo"); var recGuard = coll.GetType().GetField("receiptNumberGuard", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(coll) as NumberFieldGuard;
            if (billNo == null || billGuard == null || billNo.Parent != billGuard.Message.Parent || billNo.Parent != billGuard.Tick.Parent) missing.Add("invoice number guard is no longer under its field");
            if (recNo == null || recGuard == null || recNo.Parent != recGuard.Message.Parent || recNo.Parent != recGuard.Tick.Parent) missing.Add("receipt number guard is no longer under its field");
            var billList = FieldOf<ValidationChecklist>(bill, "billChecklist"); var collList = FieldOf<ValidationChecklist>(coll, "collChecklist");
            var soaList = FieldOf<ValidationChecklist>(soaView, "soaChecklist");
            bool on = billList?.ShowOnlyProblems == true && collList?.ShowOnlyProblems == true && soaList?.ShowOnlyProblems == true;
            host.Close();
            if (missing.Count > 0) return "lost: " + string.Join(", ", missing);
            return on ? "" : "the problems-only list is not switched on";
        });
    }
}
