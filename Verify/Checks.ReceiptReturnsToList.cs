using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

public static partial class Checks
{
    /// <summary>Nov 17: after "Complete / Issue Receipt" the screen goes back to the Collection Receipts list (the receipt form closes).</summary>
    [Check]
    public static string ReceiptComplete_ReturnsToReceiptsList()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        int soaId = SaveSoaFor("CLIENT A", (a, 6000m, 300m));
        MJdatabase.SaveInvoiceForSoa(soaId, "SI-9401");
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var no = new NoPresser(GetCurrentThreadId());   // "print the receipt now?" → No
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var view = new CollectionView { Dock = DockStyle.Fill };
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-5000, -5000), ClientSize = new System.Drawing.Size(1500, 900) };
            host.Controls.Add(view); host.Show(); Pump(800);
            view.PrefillForSoa(soaId); Pump(400);
            var pnl = FieldOf<Panel>(view, "pnlForm")!;
            if (!pnl.Visible) return "the receipt form did not open for the statement";
            FieldOf<TextBox>(view, "txtReceiptNo")!.Text = "4501";
            var method = FieldOf<ComboBox>(view, "cbPaymentMethod")!;
            int cash = method.FindStringExact("Cash"); if (cash >= 0) method.SelectedIndex = cash; else method.Text = "Cash";
            FieldOf<TextBox>(view, "txtRefNo")!.Text = "REF-4501";
            var chk = FieldOf<CheckBox>(view, "chkVerifyAmount");
            if (chk != null) chk.Checked = true;
            Pump(300);
            typeof(CollectionView).GetMethod("SaveNewReceipt", F)!.Invoke(view, null);
            Pump(600);
            var msgs = new List<string>();
            if (Convert.ToInt64(Scalar("SELECT COUNT(*) FROM CollectionReceipts WHERE ReceiptNumber = '4501' AND COALESCE(IsVoid,0) = 0") ?? 0) == 0) msgs.Add("the receipt was not saved");
            if (pnl.Visible) msgs.Add("the receipt form is still open after Complete / Issue Receipt");
            var grid = FieldOf<DataGridView>(view, "gridCollections");
            if (grid != null && !grid.Visible) msgs.Add("the receipts list is not shown after Complete / Issue Receipt");
            host.Close();
            return string.Join("; ", msgs);
        });
    }

    /// <summary>Clicks "No" on the app's own pop-up.</summary>
    private sealed class NoPresser : IDisposable
    {
        private readonly System.Threading.Timer timer;
        public NoPresser(uint uiThread)
        {
            timer = new System.Threading.Timer(_ => EnumThreadWindows(uiThread, (h, __) =>
            {
                if (!IsWindowVisible(h)) return true;
                EnumChildWindows(h, (c, ___) =>
                {
                    var t = new System.Text.StringBuilder(64); GetWindowText(c, t, 64);
                    if (t.ToString().Replace("&", "") == "No") PostMessage(c, 0x00F5 /* BM_CLICK */, IntPtr.Zero, IntPtr.Zero);
                    return true;
                }, IntPtr.Zero);
                return true;
            }, IntPtr.Zero), null, 300, 300);
        }
        public void Dispose() => timer.Dispose();
    }
}
