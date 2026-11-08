using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>Phase 0a: service invoice and collection receipt numbers are strictly unique (plan section 17).</summary>
public static partial class Checks
{
    private static string Pair(NumberKind k, string a, string b, bool same)
    {
        bool eq = NumberKey.Of(k, a) == NumberKey.Of(k, b);
        return eq == same ? "" : $"{k}: '{a}' and '{b}' should be {(same ? "the same" : "different")} (keys '{NumberKey.Of(k, a)}' / '{NumberKey.Of(k, b)}')";
    }

    [Check]
    public static string NumberKey_Si_SameAndDifferent()
    {
        var bad = new[]
        {
            Pair(NumberKind.SI, "SI-1001", "1001", true), Pair(NumberKind.SI, "si 01001", "SI-1001", true), Pair(NumberKind.SI, "0765", "765", true),
            Pair(NumberKind.SI, "SI/1001", "1001", true), Pair(NumberKind.SI, " SI-1001 ", "SI1001", true), Pair(NumberKind.SI, "A012", "A-12", true),
            Pair(NumberKind.SI, "A12", "12", false), Pair(NumberKind.SI, "SIERRA-12", "12", false), Pair(NumberKind.SI, "SI-1001", "SI-1002", false),
            Pair(NumberKind.SI, "SI-TEST-1003", "1003", false)
        }.Where(x => x.Length > 0).ToList();
        return bad.Count == 0 ? "" : string.Join("; ", bad);
    }

    [Check]
    public static string NumberKey_Cr_And_Dr()
    {
        var bad = new[]
        {
            Pair(NumberKind.CR, "0006", "00006", true), Pair(NumberKind.CR, "0006", "0007", false),
            Pair(NumberKind.DR, "dr-1001", "DR-1001", true), Pair(NumberKind.DR, " DR-1001 ", "DR-1001", true),
            Pair(NumberKind.DR, "DR-1001", "1001", false), Pair(NumberKind.DR, "DR-1001", "DR-01001", false), Pair(NumberKind.DR, "DR 1001", "DR-1001", false),
            Pair(NumberKind.DSP, "bcs0001", "BCS0001", true)
        }.Where(x => x.Length > 0).ToList();
        string core = NumberKey.Core("SI-1001") == NumberKey.Core("DR-1001") && NumberKey.Core("1001") == NumberKey.Core("DR-01001") ? "" : "core keys differ";
        return bad.Count == 0 && core.Length == 0 ? "" : string.Join("; ", bad) + core;
    }

    [Check]
    public static string Si_BlockedInAnySpelling_AndByTheDatabase()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(); Seed.TripReport(a);
        int b = Seed.Delivery(); Seed.TripReport(b);
        MJdatabase.SaveInvoice(InvoiceWithNumber(a, "SI-2001"));
        bool plain = Refused(() => MJdatabase.SaveInvoice(InvoiceWithNumber(b, "2001")), out _);
        bool spaced = Refused(() => MJdatabase.SaveInvoice(InvoiceWithNumber(b, "si 2001")), out _);
        bool dbRefused = false;
        try { Exec("INSERT INTO Invoices (InvoiceNumber, SoaId, DeliveryId, ClientName, IssueDate, DueDate, TotalAmount, Deductions, NetAmount, Status) VALUES ('si-2001', 0, 0, 'X', '2026-11-01', '2026-11-16', 1, 0, 1, 'Unpaid')"); }
        catch (Microsoft.Data.Sqlite.SqliteException) { dbRefused = true; }
        return plain && spaced && dbRefused ? "" : $"'2001' {(plain ? "refused" : "SAVED")}, 'si 2001' {(spaced ? "refused" : "SAVED")}, database {(dbRefused ? "refused" : "ACCEPTED")} a lower-case duplicate";
    }

    [Check]
    public static string Si_RealVoid_NumberStaysBurned_AndCannotBeReinstated()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(); Seed.TripReport(a);
        int b = Seed.Delivery(); Seed.TripReport(b);
        var inv = InvoiceWithNumber(a, "SI-2101");
        MJdatabase.SaveInvoice(inv);
        bool cancelled = MJdatabase.CancelInvoice(inv.Id, typingMistake: false, "client returned the spoiled paper invoice");
        string? why = MJdatabase.CheckSiNumber("SI-2101");
        bool reuse = Refused(() => MJdatabase.SaveInvoice(InvoiceWithNumber(b, "2101")), out _);
        bool reinstate = Refused(() => MJdatabase.ReinstateInvoice(inv.Id), out var rw) && rw.Contains("void");
        return cancelled && why != null && why.Contains("cancelled") && reuse && reinstate ? "" : $"cancelled {cancelled}, why '{why}', reuse {(reuse ? "refused" : "ALLOWED")}, reinstate {(reinstate ? "refused" : "ALLOWED")}";
    }

    [Check]
    public static string Si_TypingMistake_KeptForTheSameRecord_ThenReinstated()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(); int tr = Seed.TripReport(a);
        int b = Seed.Delivery(); Seed.TripReport(b);
        var inv = InvoiceWithNumber(a, "SI-2201");
        MJdatabase.SaveInvoice(inv);
        MJdatabase.CancelInvoice(inv.Id, typingMistake: true, "typed the wrong client");
        string reportAfterCancel = TrStatus(tr);
        bool otherBlocked = Refused(() => MJdatabase.SaveInvoice(InvoiceWithNumber(b, "2201")), out var why) && why.Contains("kept");
        bool canReinstate = MJdatabase.CanReinstateInvoice(new Invoice { Id = inv.Id, Status = "Cancelled" });
        MJdatabase.ReinstateInvoice(inv.Id);
        string status = InvStatus(inv.Id);
        bool stillBlockedForOthers = Refused(() => MJdatabase.SaveInvoice(InvoiceWithNumber(b, "SI-2201")), out _);
        var rows = MJdatabase.GetTypingMistakeCancellations();
        return reportAfterCancel == TripReportStatus.Completed && otherBlocked && canReinstate && status == PayStatus.Unpaid && TrStatus(tr) == TripReportStatus.Invoiced && stillBlockedForOthers
               && rows.Count == 1 && rows[0].State == "Reinstated" ? ""
            : $"report after cancel {reportAfterCancel}, other blocked {otherBlocked}, can reinstate {canReinstate}, status {status}, report now {TrStatus(tr)}, still blocked {stillBlockedForOthers}, report rows {rows.Count}/{rows.FirstOrDefault()?.State}";
    }

    [Check]
    public static string Si_TypingMistake_ExpiresAfterSevenDays_ThenBurned()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(); Seed.TripReport(a);
        var inv = InvoiceWithNumber(a, "SI-2301");
        MJdatabase.SaveInvoice(inv);
        MJdatabase.CancelInvoice(inv.Id, typingMistake: true, "entered twice by mistake");
        Exec("UPDATE Invoices SET ReenterUntil=@u WHERE Id=@i", ("@u", DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss")), ("@i", inv.Id));
        bool reinstate = Refused(() => MJdatabase.ReinstateInvoice(inv.Id), out var rw);
        string? why = MJdatabase.CheckSiNumber("SI-2301");
        var row = MJdatabase.GetTypingMistakeCancellations().FirstOrDefault();
        return reinstate && why != null && why.Contains("cancelled") && row?.State == "Expired" ? "" : $"reinstate {(reinstate ? "refused" : "ALLOWED")} ({rw}), number says '{why}', report state {row?.State}";
    }

    [Check]
    public static string Si_Reinstate_RefusedWhenTheTripWasBilledAgain()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(); Seed.TripReport(a);
        var first = InvoiceWithNumber(a, "SI-2401");
        MJdatabase.SaveInvoice(first);
        MJdatabase.CancelInvoice(first.Id, typingMistake: true, "wrong amount typed");
        MJdatabase.SaveInvoice(InvoiceWithNumber(a, "SI-2402")); // the trip is billed again with another number
        bool refused = Refused(() => MJdatabase.ReinstateInvoice(first.Id), out var why);
        return refused && why.Contains("already billed") ? "" : "reinstate " + (refused ? "refused: " + why : "ALLOWED");
    }

    [Check]
    public static string Si_Rename_AdminOnly_ReleasesTheOldNumber_RefusedAfterDownstreamUse()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(); Seed.TripReport(a);
        int b = Seed.Delivery(); Seed.TripReport(b);
        var inv = InvoiceWithNumber(a, "SI-2510");
        MJdatabase.SaveInvoice(inv);
        try
        {
            SessionContext.ActiveUser = new User { Id = 2, Username = "clerk", Role = "Billing Clerk", FullName = "Clerk", IsActive = true };
            inv.InvoiceNumber = "SI-2501";
            bool staffRefused = Refused(() => MJdatabase.SaveInvoice(inv), out var sw) && sw.Contains("Administrator");
            SessionContext.ActiveUser = new User { Id = 1, Username = "admin", Role = "Admin", FullName = "Admin", IsActive = true };
            MJdatabase.SaveInvoice(inv);
            bool released = MJdatabase.CheckSiNumber("SI-2510") == null;
            MJdatabase.SaveInvoice(InvoiceWithNumber(b, "SI-2510")); // the released number is free for another invoice
            // downstream use: put it on a live statement, then try another rename
            int soa = Seed.Soa(status: "Unpaid", total: 1500m);
            Exec("UPDATE Invoices SET SoaId=@s WHERE Id=@i", ("@s", soa), ("@i", inv.Id));
            inv.SoaId = soa; inv.InvoiceNumber = "SI-2502";
            bool downstream = Refused(() => MJdatabase.SaveInvoice(inv), out var dw) && dw.Contains("statement");
            return staffRefused && released && downstream ? "" : $"staff {(staffRefused ? "refused" : "ALLOWED")}, old released {released}, on-statement rename {(downstream ? "refused" : "ALLOWED")}";
        }
        finally { SessionContext.ActiveUser = null; }
    }

    [Check]
    public static string SiVsCr_SameDigits_NeedsConfirmation_ThenIsRemembered()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(); Seed.TripReport(a);
        var target = TargetOf(a);
        MJdatabase.SaveOneReceipt("9301", target, "Cash", "", "");
        int b = Seed.Delivery(); Seed.TripReport(b);
        string? needed = null;
        try { MJdatabase.SaveInvoice(InvoiceWithNumber(b, "SI-9301")); }
        catch (NumberNeedsConfirmationException ex) { needed = ex.Message; }
        var inv = InvoiceWithNumber(b, "SI-9301");
        MJdatabase.SaveInvoice(inv, crossTypeConfirmed: true);
        var again = MJdatabase.LoadNumberIndex().Check(NumberKind.SI, "SI-9301", inv.Id);
        return needed != null && needed.Contains("Collection Receipt") && inv.Id > 0 && !again.TickNeeded && again.CrossHits.Count == 1 ? ""
            : $"first save {(needed == null ? "SAVED WITHOUT CONFIRMATION" : "asked")}, saved id {inv.Id}, remembered {(!again.TickNeeded)}";
    }

    [Check]
    public static string DrAndDispatchSameDigits_OnlyWarn_NoConfirmation()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(); Seed.TripReport(a);
        Exec("INSERT INTO DispatchStops (DeliveryId, StopSequence, ClientName, Destination, Status, StoreCode, DrNumbers) VALUES (@d, 1, 'CLIENT A', 'RR-Vigan', 'Delivered', '202400', 'DR-9401')", ("@d", a));
        var c = MJdatabase.LoadNumberIndex().Check(NumberKind.SI, "9401");
        int b = Seed.Delivery(); Seed.TripReport(b);
        MJdatabase.SaveInvoice(InvoiceWithNumber(b, "9401")); // no confirmation needed
        return c.CrossHits.Any(h => h.Kind == NumberKind.DR) && !c.TickNeeded && c.Warnings.Count > 0 && !c.Blocked ? "" : $"hits {c.CrossHits.Count}, tick {c.TickNeeded}, warnings {c.Warnings.Count}, blocked {c.Blocked}";
    }

    [Check]
    public static string Cr_BlockedInAnySpelling_AndGapWarned()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(); Seed.TripReport(a);
        MJdatabase.SaveOneReceipt("0006", TargetOf(a), "Cash", "", "");
        var same = MJdatabase.LoadNumberIndex().Check(NumberKind.CR, "00006");
        var gap = MJdatabase.LoadNumberIndex().Check(NumberKind.CR, "0009");
        var next = MJdatabase.LoadNumberIndex().Check(NumberKind.CR, "0007");
        return same.Blocked && gap.Warnings.Any(w => w.Contains("skips 0007 to 0008")) && gap.Hint != null && gap.Hint.Contains("next: 0007") && next.Warnings.Count == 0 && !next.Blocked ? ""
            : $"'00006' blocked {same.Blocked}; gap warnings [{string.Join(" | ", gap.Warnings)}] hint '{gap.Hint}'; next warnings {next.Warnings.Count}";
    }

    [Check]
    public static string Save_TwoWritersSameInvoiceNumber_ExactlyOneWins()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(); Seed.TripReport(a);
        int b = Seed.Delivery(); Seed.TripReport(b);
        var errors = new List<string>(); int ok = 0;
        void Run(int d)
        {
            try { MJdatabase.SaveInvoice(InvoiceWithNumber(d, "SI-9501")); Interlocked.Increment(ref ok); }
            catch (Exception ex) { lock (errors) errors.Add(ex.GetType().Name + ": " + ex.Message); }
        }
        var t1 = Task.Run(() => Run(a)); var t2 = Task.Run(() => Run(b));
        Task.WaitAll(t1, t2);
        long rows = Long("SELECT COUNT(*) FROM Invoices WHERE UPPER(TRIM(InvoiceNumber))='SI-9501'");
        return ok == 1 && rows == 1 && errors.Count == 1 ? "" : $"{ok} saved, {rows} rows, errors: {string.Join(" | ", errors)}";
    }

    [Check]
    public static string Save_TwoWritersSameReceiptNumber_ExactlyOneWins()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(); Seed.TripReport(a);
        int b = Seed.Delivery(); Seed.TripReport(b);
        var ta = TargetOf(a); var tb = TargetOf(b);
        var errors = new List<string>(); int ok = 0;
        void Run(PayTarget t)
        {
            try { MJdatabase.SaveOneReceipt("9601", t, "Cash", "", ""); Interlocked.Increment(ref ok); }
            catch (Exception ex) { lock (errors) errors.Add(ex.GetType().Name + ": " + ex.Message); }
        }
        var t1 = Task.Run(() => Run(ta)); var t2 = Task.Run(() => Run(tb));
        Task.WaitAll(t1, t2);
        long rows = Long("SELECT COUNT(DISTINCT InvoiceId) FROM CollectionReceipts WHERE ReceiptNumber='9601'");
        return ok == 1 && rows == 1 && errors.Count == 1 ? "" : $"{ok} saved, {rows} invoices paid, errors: {string.Join(" | ", errors)}";
    }

    [Check]
    public static string Numbers_BackfillGivesEveryExistingRowItsKey()
    {
        Fresh(); RunBackfill();
        MJdatabase.EnsureNumberSchema(); // creates the key columns on the new database
        Seed.Invoice(0, 100m); Seed.Invoice(0, 200m);
        int inv = Seed.Invoice(0, 300m); Seed.Receipt(inv, 300m, "9701");
        Exec("UPDATE Invoices SET NumberKey=NULL; UPDATE CollectionReceipts SET NumberKey=NULL;");
        MJdatabase.UseDatabaseFile(MJdatabase.DbFilePath); // forgets the schema flags: the next call back-fills again
        MJdatabase.EnsureNumberSchema();
        long missing = Long("SELECT (SELECT COUNT(*) FROM Invoices WHERE NumberKey IS NULL) + (SELECT COUNT(*) FROM CollectionReceipts WHERE NumberKey IS NULL)");
        return missing == 0 ? "" : $"{missing} rows without a key";
    }

    [Check]
    public static string NumberFieldGuard_ShowsTheOwnerLive_AndAsksForTheTick()
    {
        string dbPath = Fresh(); RunBackfill();
        int a = Seed.Delivery(); Seed.TripReport(a);
        MJdatabase.SaveInvoice(InvoiceWithNumber(a, "SI-2601"));
        int b = Seed.Delivery(); Seed.TripReport(b);
        MJdatabase.SaveOneReceipt("9801", TargetOf(b), "Cash", "", "");
        var box = new TextBox();
        var guard = new NumberFieldGuard(box, NumberKind.SI);
        box.Text = "2601"; guard.Recheck();
        bool used = guard.HasProblem && guard.Message.Visible && guard.Message.Text.Contains("already used") && guard.Message.Text.Contains("SI-2601") && guard.Message.Text.Contains("open it") && guard.Message.Cursor == Cursors.Hand;
        box.Text = "SI-9801"; guard.Recheck();
        bool askTick = !guard.HasProblem && guard.TickMissing && guard.Tick.Visible && guard.Tick.Text.Contains("Collection Receipt No. 9801");
        guard.Tick.Checked = true;
        bool ticked = !guard.TickMissing && guard.CrossTypeConfirmed;
        box.Text = "SI-9802"; guard.Recheck();
        bool freeAgain = !guard.HasProblem && !guard.TickMissing && !guard.Tick.Checked && guard.Message.Text.Contains("is free");
        return used && askTick && ticked && freeAgain ? "" : $"used {used}, ask tick {askTick}, ticked {ticked}, free again {freeAgain} ({guard.Message.Text}) [db {dbPath}; numbers {string.Join(",", MJdatabase.LoadNumberIndex().Entries.Select(e => e.Kind + ":" + e.Key + ":" + e.State))}]";
    }
}
