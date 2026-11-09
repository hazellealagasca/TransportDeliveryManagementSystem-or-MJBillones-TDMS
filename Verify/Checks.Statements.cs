using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>Statement of Account: the "ready for a statement" list of service invoices, and cancelling a statement with a reason.</summary>
public static partial class Checks
{
    private static int SoaWithInvoice(int deliveryId, string number, decimal gross, out int invoiceId)
    {
        var inv = InvoiceWithNumber(deliveryId, number, gross);
        MJdatabase.SaveInvoice(inv);
        invoiceId = inv.Id;
        int soa = Seed.Soa(status: "Unpaid", total: gross);
        Seed.SoaItem(soa, deliveryId);
        Exec("UPDATE Invoices SET SoaId=@s WHERE Id=@i", ("@s", soa), ("@i", inv.Id));
        return soa;
    }

    [Check]
    public static string ReadyForStatement_ListsOnlyOpenInvoicesOnNoStatement()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(); Seed.TripReport(a); MJdatabase.SaveInvoice(InvoiceWithNumber(a, "SI-8101", 1000m));
        int b = Seed.Delivery(); Seed.TripReport(b); SoaWithInvoice(b, "SI-8102", 2000m, out _);   // already on a statement
        int c = Seed.Delivery(); Seed.TripReport(c); var cancelled = InvoiceWithNumber(c, "SI-8103", 500m); MJdatabase.SaveInvoice(cancelled); MJdatabase.DeleteRecord("Invoices", cancelled.Id);
        int d = Seed.Delivery(); Seed.TripReport(d); MJdatabase.SaveInvoice(InvoiceWithNumber(d, "SI-8104", 0m)); // no amount: a draft
        var rows = MJdatabase.GetInvoicesReadyForStatement();
        bool ok = rows.Count == 1 && rows[0].InvoiceNumber == "SI-8101" && rows[0].Net == 1000m && rows[0].TripNumber.Length > 0;
        return ok ? "" : "ready: " + string.Join(", ", rows.Select(r => r.InvoiceNumber));
    }

    [Check]
    public static string ReadyForStatement_ACancelledStatementFreesItsInvoices()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int soa = SoaWithInvoice(d, "SI-8201", 1500m, out int invId);
        bool before = MJdatabase.GetInvoicesReadyForStatement().Count == 0;
        MJdatabase.CancelStatementOfAccount(soa, "Wrong client was picked");
        var after = MJdatabase.GetInvoicesReadyForStatement();
        return before && after.Count == 1 && after[0].InvoiceId == invId ? "" : $"before empty {before}, after {after.Count}";
    }

    [Check]
    public static string CancelStatement_KeepsWhoWhenAndWhy()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int soa = SoaWithInvoice(d, "SI-8301", 800m, out _);
        MJdatabase.CancelStatementOfAccount(soa, "Client asked for a new period");
        var info = MJdatabase.GetSoaCancelInfo(soa);
        string status = Str("SELECT Status FROM StatementsOfAccount WHERE Id=@i", ("@i", soa));
        bool again = Refused(() => MJdatabase.CancelStatementOfAccount(soa, "Trying twice"), out var why);
        return status == "Cancelled" && info.Reason == "Client asked for a new period" && info.At.HasValue && again && why.Contains("already cancelled") ? "" : $"status {status}, reason '{info.Reason}', second cancel {(again ? "refused" : "ALLOWED")} ({why})";
    }

    [Check]
    public static string CancelStatement_NeedsAReason()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int soa = SoaWithInvoice(d, "SI-8401", 800m, out _);
        bool none = Refused(() => MJdatabase.CancelStatementOfAccount(soa, "   "), out _);
        bool shortOne = Refused(() => MJdatabase.CancelStatementOfAccount(soa, "oops"), out _);
        string status = Str("SELECT Status FROM StatementsOfAccount WHERE Id=@i", ("@i", soa));
        return none && shortOne && status != "Cancelled" ? "" : $"blank refused {none}, short refused {shortOne}, status {status}";
    }

    [Check]
    public static string CancelStatement_RefusedWhileAReceiptIsLive_AllowedAfterItIsVoided()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.TripReport(d);
        int soa = SoaWithInvoice(d, "SI-8501", 1200m, out int invId);
        Seed.Receipt(invId, 1200m, "9501", soa);
        var s = MJdatabase.GetStatementOfAccountById(soa)!;
        string why = MJdatabase.WhySoaCannotBeCancelled(s);
        bool refused = Refused(() => MJdatabase.CancelStatementOfAccount(soa, "Cancel while paid"), out _);
        Exec("UPDATE CollectionReceipts SET IsVoid=1 WHERE InvoiceId=@i", ("@i", invId));
        MJdatabase.CancelStatementOfAccount(soa, "Receipt was voided first");
        string status = Str("SELECT Status FROM StatementsOfAccount WHERE Id=@i", ("@i", soa));
        return why.Contains("Void the receipt") && refused && status == "Cancelled" ? "" : $"why '{why}', refused {refused}, status {status}";
    }
}

public static partial class Checks
{
    private static T? FieldOf<T>(object o, string name) where T : class =>
        o.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(o) as T;

    [Check]
    public static string StatementForm_TotalsFollowTheInvoices_AndCannotBeTyped()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(d);
        var inv = InvoiceWithNumber(d, "SI-8601", 2000m);
        MJdatabase.SaveInvoice(inv);
        decimal net = Convert.ToDecimal(Scalar("SELECT NetAmount FROM Invoices WHERE Id=@i", ("@i", inv.Id)) ?? 0), gross = Convert.ToDecimal(Scalar("SELECT TotalAmount FROM Invoices WHERE Id=@i", ("@i", inv.Id)) ?? 0);
        // Nov 9 (statement first): the queue is the completed trip reports with no statement (keyed by dispatch id), here one that already has an unpaid invoice.
        var queue = MJdatabase.GetTripReportsWaitingForStatement();
        if (queue.Count != 1) return $"queue has {queue.Count}";
        var ready = queue.Select(c => new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database.ReadyInvoiceRow { InvoiceId = c.DeliveryId, InvoiceNumber = c.TripReportNumber, TripNumber = c.DispatchNumber, ClientName = c.ClientName, Kind = "Normal", TripDate = c.ReportDate, IssueDate = c.ReportDate }).ToList();
        return RunOnSta(() =>
        {
            using var view = new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views.StatementOfAccountView();
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), Size = new Size(1400, 900), Opacity = 0 };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(600);
            view.StartSoaFor("CLIENT A", ready);
            Pump(600);
            var due = FieldOf<NumericUpDown>(view, "numTotalAmountDue");
            var sales = FieldOf<NumericUpDown>(view, "numTotalSales");
            if (due == null || sales == null) return "total boxes not found";
            bool readOnly = due.ReadOnly && sales.ReadOnly;
            // Nov 7: the statement works its totals out from the invoices' amount and fuel (VAT and 2% withholding on the freight only).
            decimal expectedDue = Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions.SoaTotals.From(gross, 0m).Due;
            string r = net > 0 && due.Value == expectedDue && sales.Value == gross && readOnly ? "" : $"due {due.Value} (expected {expectedDue}), sales {sales.Value} (gross {gross}), read-only {readOnly}";
            host.Close();
            return r;
        });
    }

    [Check]
    public static string CollectionForm_ListsEveryStatementAndInvoiceWaitingForPayment()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        MJdatabase.SaveInvoice(InvoiceWithNumber(a, "SI-8701", 1000m));                    // a lone invoice
        int b = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(b);
        SoaWithInvoice(b, "SI-8702", 2500m, out _);                                          // a statement
        int c = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(c);
        int paidSoa = SoaWithInvoice(c, "SI-8703", 700m, out int paidInv);
        Exec("UPDATE Invoices SET Status='Paid' WHERE Id=@i", ("@i", paidInv));              // already paid: not waiting
        return RunOnSta(() =>
        {
            using var view = new Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views.CollectionView();
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), Size = new Size(1400, 900), Opacity = 0 };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(800);
            var panel = FieldOf<Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions.CollectionReadyPanel>(view, "collReady");
            if (panel == null) return "waiting list not found";
            int expected = MJdatabase.GetPayTargets().Count;
            string r = panel.Count == 2 && expected == 2 ? "" : $"panel lists {panel.Count}, pay targets {expected} (2 expected)";
            host.Close();
            return r;
        });
    }
}
