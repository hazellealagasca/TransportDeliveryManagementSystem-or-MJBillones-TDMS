using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>Task 1: status columns and the one-time backfill.</summary>
public static partial class Checks
{
    /// <summary>Fresh database with seeded rows, then the backfill runs as on first start after the update.</summary>
    private static void RunBackfill()
    {
        MJdatabase.SetSystemSetting("Nov6_WorkflowSchemaDone", "0");
        MJdatabase.EnsureWorkflowSchema();
    }
    private static string TrStatus(int trId) => Str("SELECT COALESCE(Status,'') FROM TripReports WHERE Id=@i", ("@i", trId));
    private static string InvStatus(int id) => Str("SELECT COALESCE(Status,'') FROM Invoices WHERE Id=@i", ("@i", id));
    private static string SoaStatus(int id) => Str("SELECT COALESCE(Status,'') FROM StatementsOfAccount WHERE Id=@i", ("@i", id));

    [Check]
    public static string Backfill_ReportWithInvoice_IsInvoiced()
    {
        Fresh();
        int d = Seed.Delivery(); int tr = Seed.TripReport(d); Seed.Invoice(d, 1000m);
        RunBackfill();
        return TrStatus(tr) == TripReportStatus.Invoiced ? "" : "got " + TrStatus(tr);
    }

    [Check]
    public static string Backfill_ReportWithoutInvoice_IsCompleted()
    {
        Fresh();
        int d = Seed.Delivery(); int tr = Seed.TripReport(d);
        int d2 = Seed.Delivery(); int tr2 = Seed.TripReport(d2); Seed.Invoice(d2, 1000m, "Cancelled");
        RunBackfill();
        return TrStatus(tr) == TripReportStatus.Completed && TrStatus(tr2) == TripReportStatus.Completed ? "" : $"got {TrStatus(tr)}, {TrStatus(tr2)}";
    }

    [Check]
    public static string Backfill_ReinvoicedTrip_IsInvoiced()
    {
        Fresh();
        int d = Seed.Delivery(); int tr = Seed.TripReport(d);
        Seed.Invoice(d, 1000m, "Cancelled"); Seed.Invoice(d, 1200m, "Unpaid");
        RunBackfill();
        return TrStatus(tr) == TripReportStatus.Invoiced ? "" : "got " + TrStatus(tr);
    }

    [Check]
    public static string Backfill_SpecialTripReport_IsInvoiced()
    {
        Fresh();
        int st = Seed.SpecialTrip(); int tr = Seed.TripReport(0, st); Seed.Invoice(0, 5000m, specialTripId: st);
        RunBackfill();
        return TrStatus(tr) == TripReportStatus.Invoiced ? "" : "got " + TrStatus(tr);
    }

    [Check]
    public static string Backfill_NoPartialOrOverdueLeft()
    {
        Fresh();
        int a = Seed.Invoice(Seed.Delivery(), 1000m, "Partial"); Seed.Receipt(a, 400m);
        int b = Seed.Invoice(Seed.Delivery(), 1000m, "Overdue", dueInDays: -5);
        int c = Seed.Invoice(Seed.Delivery(), 1000m, "Partial"); Seed.Receipt(c, 1000m);
        RunBackfill();
        long left = Long("SELECT COUNT(*) FROM Invoices WHERE Status IN ('Partial','Overdue')");
        return left == 0 && InvStatus(a) == PayStatus.Unpaid && InvStatus(b) == PayStatus.Unpaid && InvStatus(c) == PayStatus.Paid
            ? "" : $"left {left}; a={InvStatus(a)} b={InvStatus(b)} c={InvStatus(c)}";
    }

    [Check]
    public static string Backfill_SoaTypeFromItems()
    {
        Fresh();
        int st = Seed.SpecialTrip();
        int normal = Seed.Soa(); Seed.SoaItem(normal, Seed.Delivery());
        int special = Seed.Soa(); Seed.SoaItem(special, 0, st);
        int mixed = Seed.Soa(); Seed.SoaItem(mixed, Seed.Delivery()); Seed.SoaItem(mixed, 0, Seed.SpecialTrip());
        RunBackfill();
        string T(int id) => Str("SELECT SoaType FROM StatementsOfAccount WHERE Id=@i", ("@i", id));
        return T(normal) == SoaTypes.Normal && T(special) == SoaTypes.Special && T(mixed) == SoaTypes.Mixed ? "" : $"{T(normal)}/{T(special)}/{T(mixed)}";
    }

    [Check]
    public static string Backfill_SoaIssuedBecomesUnpaidOrPaid()
    {
        Fresh();
        int open = Seed.Soa(); Seed.Invoice(Seed.Delivery(), 500m, "Unpaid", soaId: open);
        int paid = Seed.Soa(); Seed.Invoice(Seed.Delivery(), 500m, "Paid", soaId: paid); Seed.Invoice(Seed.Delivery(), 700m, "Paid", soaId: paid);
        int cancelled = Seed.Soa(status: "Cancelled");
        RunBackfill();
        return SoaStatus(open) == PayStatus.Unpaid && SoaStatus(paid) == PayStatus.Paid && SoaStatus(cancelled) == PayStatus.Cancelled
            ? "" : $"{SoaStatus(open)}/{SoaStatus(paid)}/{SoaStatus(cancelled)}";
    }

    [Check]
    public static string Backfill_RunsOnlyOnce()
    {
        Fresh();
        int d = Seed.Delivery(); int tr = Seed.TripReport(d);
        RunBackfill();
        Exec("UPDATE TripReports SET Status='Draft' WHERE Id=@i", ("@i", tr));
        MJdatabase.EnsureWorkflowSchema(); // second start: must not touch it
        return TrStatus(tr) == TripReportStatus.Draft ? "" : "got " + TrStatus(tr);
    }

    [Check]
    public static string Backfill_TakesBackupFirst()
    {
        string db = Fresh();
        RunBackfill();
        string dir = Path.Combine(Path.GetDirectoryName(db)!, "Backups");
        return Directory.Exists(dir) && Directory.GetFiles(dir, "before_workflow_schema_*.db").Length > 0 ? "" : "no backup file";
    }

    [Check]
    public static string Backfill_ReceiptsGetCompletedStatus()
    {
        Fresh();
        int inv = Seed.Invoice(Seed.Delivery(), 100m, "Paid"); int r = Seed.Receipt(inv, 100m);
        RunBackfill();
        string s = Str("SELECT COALESCE(Status,'') FROM CollectionReceipts WHERE Id=@i", ("@i", r));
        return s == "Completed" ? "" : "got " + s;
    }

    [Check]
    public static string Backfill_TripReportModelReadsStatus()
    {
        Fresh();
        int d = Seed.Delivery(); Seed.TripReport(d); Seed.Invoice(d, 1000m);
        RunBackfill();
        var t = MJdatabase.GetTripReports().FirstOrDefault();
        return t != null && t.Status == TripReportStatus.Invoiced ? "" : "got " + t?.Status;
    }

    /// <summary>On a copy of the real database: nothing is lost and every status is one of the allowed values.</summary>
    [Check]
    public static string Backfill_RealCopy_RowCountsUnchanged()
    {
        if (FromCopy() == null) return "no real database given";
        MJdatabase.InitializeDatabase();
        string[] tables = { "TripReports", "Invoices", "StatementsOfAccount", "CollectionReceipts", "StatementOfAccountItems", "Deliveries" };
        var before = tables.ToDictionary(t => t, t => Long($"SELECT COUNT(*) FROM {t}"));
        RunBackfill();
        var bad = tables.Where(t => Long($"SELECT COUNT(*) FROM {t}") != before[t]).ToList();
        long badTr = Long("SELECT COUNT(*) FROM TripReports WHERE COALESCE(Status,'') NOT IN ('Draft','Completed','Invoiced')");
        long badInv = Long("SELECT COUNT(*) FROM Invoices WHERE COALESCE(Status,'') NOT IN ('Draft','Unpaid','Paid','Cancelled')");
        long badSoa = Long("SELECT COUNT(*) FROM StatementsOfAccount WHERE COALESCE(Status,'') NOT IN ('Unpaid','Paid','Cancelled')");
        return bad.Count == 0 && badTr == 0 && badInv == 0 && badSoa == 0 ? "" : $"changed counts: {string.Join(",", bad)}; bad statuses tr={badTr} inv={badInv} soa={badSoa}";
    }
}
