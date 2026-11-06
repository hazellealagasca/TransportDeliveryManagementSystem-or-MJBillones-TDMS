using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>Task 2: trip report status follows its service invoice; invoices are only Draft / Unpaid / Paid / Cancelled.</summary>
public static partial class Checks
{
    private static Invoice NewInvoice(int deliveryId, decimal gross, string client = "CLIENT A") => new Invoice
    {
        InvoiceNumber = (4000 + new Random().Next(1, 999999)).ToString(),
        DeliveryId = deliveryId, ClientName = client, IssueDate = DateTime.Today, DueDate = DateTime.Today.AddDays(15),
        TotalAmount = gross, Deductions = 0, NetAmount = gross, Status = PayStatus.Unpaid
    };

    [Check]
    public static string ZeroGrossInvoice_SavesAsDraft()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery();
        var inv = NewInvoice(d, 0m);
        MJdatabase.SaveInvoice(inv);
        return InvStatus(inv.Id) == PayStatus.Draft ? "" : "got " + InvStatus(inv.Id);
    }

    [Check]
    public static string InvoiceForTrip_MarksReportInvoiced()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); int tr = Seed.TripReport(d);
        MJdatabase.SaveInvoice(NewInvoice(d, 1500m));
        return TrStatus(tr) == TripReportStatus.Invoiced ? "" : "got " + TrStatus(tr);
    }

    [Check]
    public static string SpecialTripFormInvoice_MarksReportInvoiced()
    {
        Fresh(); RunBackfill();
        int st = Seed.SpecialTrip(); int tr = Seed.TripReport(0, st);
        var inv = NewInvoice(0, 5000m);
        MJdatabase.SaveInvoice(inv);
        MJdatabase.LinkSpecialTripInvoice(inv.Id, st);
        return TrStatus(tr) == TripReportStatus.Invoiced ? "" : "got " + TrStatus(tr);
    }

    [Check]
    public static string CancelInvoice_RevertsTripReport()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); int tr = Seed.TripReport(d);
        var inv = NewInvoice(d, 1500m);
        MJdatabase.SaveInvoice(inv);
        string afterSave = TrStatus(tr);
        MJdatabase.DeleteRecord("Invoices", inv.Id);
        return afterSave == TripReportStatus.Invoiced && TrStatus(tr) == TripReportStatus.Completed && InvStatus(inv.Id) == PayStatus.Cancelled
            ? "" : $"after save {afterSave}, after cancel {TrStatus(tr)}, invoice {InvStatus(inv.Id)}";
    }

    [Check]
    public static string Recalc_NeverWritesPartial()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery();
        var inv = NewInvoice(d, 1000m);
        MJdatabase.SaveInvoice(inv);
        Seed.Receipt(inv.Id, 400m);
        MJdatabase.SaveInvoice(inv); // an update recalculates the status
        return InvStatus(inv.Id) == PayStatus.Unpaid ? "" : "got " + InvStatus(inv.Id);
    }

    [Check]
    public static string OverdueNotStored()
    {
        Fresh(); RunBackfill();
        int id = Seed.Invoice(Seed.Delivery(), 1000m, PayStatus.Unpaid, dueInDays: -5);
        MJdatabase.GetInvoices(); // reading invoices used to flip them to Overdue
        MJdatabase.RunOperationalLifecycleAutomation();
        return InvStatus(id) == PayStatus.Unpaid ? "" : "got " + InvStatus(id);
    }

    /// <summary>Task 8: a batch where one of three trips is saved and the user goes back leaves exactly one invoice and no drafts.</summary>
    [Check]
    public static string NoOrphanDraftInvoices()
    {
        Fresh(); RunBackfill();
        var trips = Enumerable.Range(0, 3).Select(_ => { int d = Seed.Delivery(); Seed.TripReport(d); return d; }).ToList();
        long before = Long("SELECT COUNT(*) FROM Invoices");
        MJdatabase.SaveInvoice(NewInvoice(trips[0], 900m)); // the first trip is saved, then "Back to Trip Reports"
        long after = Long("SELECT COUNT(*) FROM Invoices");
        long others = Long("SELECT COUNT(*) FROM Invoices WHERE DeliveryId IN (@a, @b)", ("@a", trips[1]), ("@b", trips[2]));
        long statusOthers = Long("SELECT COUNT(*) FROM TripReports WHERE DeliveryId IN (@a, @b) AND Status = 'Completed'", ("@a", trips[1]), ("@b", trips[2]));
        return after - before == 1 && others == 0 && statusOthers == 2 ? "" : $"new {after - before}, others {others}, completed {statusOthers}";
    }
}
