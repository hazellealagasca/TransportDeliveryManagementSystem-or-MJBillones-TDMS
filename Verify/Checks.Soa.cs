using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace TDMS.Verify;

/// <summary>Task 4: SOA type, the invoices a new SOA can take, and no crew wage on SOA lines.</summary>
public static partial class Checks
{
    private static StatementOfAccount NewSoa(string type, decimal total, string client = "CLIENT A") => new StatementOfAccount
    {
        ClientName = client, PeriodCovered = "Nov 2026", IssueDate = DateTime.Today, SoaType = type,
        TotalAmount = total, TotalSalesVatInc = total, Status = PayStatus.Unpaid
    };
    private static StatementOfAccountItem Line(int deliveryId, int specialTripId = 0) => new StatementOfAccountItem
    { TripNumber = 1, DeliveryId = deliveryId, SpecialTripId = specialTripId, Destination = "Store 1", Amount = 100m, DrNumbers = "DR1" };

    [Check]
    public static string Candidates_OnlyThisClientAndKind()
    {
        Fresh(); RunBackfill();
        int normal = Seed.Invoice(Seed.Delivery(), 100m);
        int special = Seed.Invoice(0, 200m, specialTripId: Seed.SpecialTrip());
        Seed.Invoice(Seed.Delivery("CLIENT B"), 300m, client: "CLIENT B");
        var n = MJdatabase.GetSoaCandidateInvoices("client a", SoaTypes.Normal, 0).Select(i => i.Id).ToList();
        var s = MJdatabase.GetSoaCandidateInvoices("CLIENT A", SoaTypes.Special, 0).Select(i => i.Id).ToList();
        return n.SequenceEqual(new[] { normal }) && s.SequenceEqual(new[] { special }) ? "" : $"normal [{string.Join(",", n)}] special [{string.Join(",", s)}]";
    }

    [Check]
    public static string Candidates_ExcludeDraftPaidAndOnOtherSoa()
    {
        Fresh(); RunBackfill();
        int other = Seed.Soa(status: PayStatus.Unpaid), mine = Seed.Soa(status: PayStatus.Unpaid);
        Seed.Invoice(Seed.Delivery(), 0m, PayStatus.Draft);
        Seed.Invoice(Seed.Delivery(), 100m, PayStatus.Paid);
        Seed.Invoice(Seed.Delivery(), 100m, PayStatus.Cancelled);
        Seed.Invoice(Seed.Delivery(), 100m, soaId: other);
        int onMine = Seed.Invoice(Seed.Delivery(), 100m, soaId: mine);
        int free = Seed.Invoice(Seed.Delivery(), 100m);
        var forNew = MJdatabase.GetSoaCandidateInvoices("CLIENT A", SoaTypes.Normal, 0).Select(i => i.Id).ToList();
        var forMine = MJdatabase.GetSoaCandidateInvoices("CLIENT A", SoaTypes.Normal, mine).Select(i => i.Id).OrderBy(x => x).ToList();
        return forNew.SequenceEqual(new[] { free }) && forMine.SequenceEqual(new[] { onMine, free }.OrderBy(x => x))
            ? "" : $"new [{string.Join(",", forNew)}] mine [{string.Join(",", forMine)}]";
    }

    [Check]
    public static string LinkInvoices_RefusesMixedKinds()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.Invoice(d, 100m);
        int st = Seed.SpecialTrip(); Seed.Invoice(0, 100m, specialTripId: st);
        try { MJdatabase.SaveStatementOfAccount(NewSoa(SoaTypes.Normal, 200m), new List<StatementOfAccountItem> { Line(d), Line(0, st) }); return "saved a mixed statement"; }
        catch (InvalidOperationException ex) when (ex.Message.Contains("same statement", StringComparison.OrdinalIgnoreCase)) { }
        return Long("SELECT COUNT(*) FROM StatementsOfAccount") == 0 ? "" : "a statement row was left";
    }

    [Check]
    public static string LinkInvoices_RefusesWrongKindForType()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(); Seed.Invoice(d, 100m);
        try { MJdatabase.SaveStatementOfAccount(NewSoa(SoaTypes.Special, 100m), new List<StatementOfAccountItem> { Line(d) }); return "saved a normal trip on a special statement"; }
        catch (InvalidOperationException) { return ""; }
    }

    [Check]
    public static string SaveSoa_WritesTypeAndUnpaid()
    {
        Fresh(); RunBackfill();
        int st = Seed.SpecialTrip(); int inv = Seed.Invoice(0, 100m, specialTripId: st);
        var soa = NewSoa(SoaTypes.Special, Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions.SoaTotals.From(100m, 0m).Due); soa.Status = "Issued"; // what older screens still send (the total is what the invoices work out to: 2% withholding on the freight)
        int id = MJdatabase.SaveStatementOfAccount(soa, new List<StatementOfAccountItem> { Line(0, st) });
        string t = Str("SELECT SoaType FROM StatementsOfAccount WHERE Id=@i", ("@i", id));
        long linked = Long("SELECT COALESCE(SoaId,0) FROM Invoices WHERE Id=@i", ("@i", inv));
        return t == SoaTypes.Special && SoaStatus(id) == PayStatus.Unpaid && linked == id ? "" : $"type {t}, status {SoaStatus(id)}, linked {linked}";
    }

    [Check]
    public static string MixedSoa_CannotBeEdited()
    {
        Fresh(); RunBackfill();
        int soa = Seed.Soa(status: PayStatus.Unpaid);
        Exec("UPDATE StatementsOfAccount SET SoaType='Mixed' WHERE Id=@i", ("@i", soa));
        var s = MJdatabase.GetStatementOfAccountById(soa)!;
        return MJdatabase.WhySoaCannotBeEdited(s).Length > 0 ? "" : "an old mixed statement could be edited";
    }

    [Check]
    public static string SoaItems_HaveNoWageFields()
    {
        string[] banned = { "DriverWage", "HelperWage", "DistanceKm", "DieselCost", "Excess" };
        var found = typeof(StatementOfAccountItem).GetProperties().Select(p => p.Name).Intersect(banned).ToList();
        return found.Count == 0 ? "" : "still has " + string.Join(", ", found);
    }
}
