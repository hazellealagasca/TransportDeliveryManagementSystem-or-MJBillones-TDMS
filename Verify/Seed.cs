namespace TDMS.Verify;

/// <summary>Raw rows for the checks (foreign keys are not enforced on the check connection).</summary>
public static class Seed
{
    private static int n;
    private static string Next(string p) => $"{p}{System.Threading.Interlocked.Increment(ref n):D4}";

    public static int Delivery(string client = "CLIENT A", string status = "Completed", int g = 10, int e = 5)
        => (int)Checks.Exec(@"INSERT INTO Deliveries (DispatchNumber, ClientName, Destination, TruckId, TruckInfo, DriverId, DriverName, AideId, AideNames, GCrate, ECrate, TotalCrates, DispatchDate, DepartureTime, Status)
                              VALUES (@no, @c, 'Route 1', 1, 'ABC-123', 1, 'Juan Dela Cruz', 2, 'Pedro Santos', @g, @e, @t, @d, '08:00 AM', @s)",
            ("@no", Next("D-")), ("@c", client), ("@g", g), ("@e", e), ("@t", g + e), ("@d", DateTime.Today.ToString("yyyy-MM-dd")), ("@s", status));

    public static int SpecialTrip(string client = "CLIENT A", decimal charge = 5000m, string status = "Completed")
        => (int)Checks.Exec(@"INSERT INTO SpecialTrips (OrderReference, DispatchDate, Company, Status, TripCharge, DestinationAddress) VALUES (@r, @d, @c, @s, @ch, 'Tarlac')",
            ("@r", Next("ST-")), ("@d", DateTime.Today.ToString("yyyy-MM-dd")), ("@c", client), ("@s", status), ("@ch", (double)charge));

    public static int TripReport(int deliveryId, int specialTripId = 0, bool isVoid = false)
        => (int)Checks.Exec(@"INSERT INTO TripReports (TripReportNumber, DeliveryId, DispatchNumber, DeliveredCrates, ReturnedCrates, ReportDate, SpecialTripId, IsVoid)
                              VALUES (@no, @d, '', 0, 0, @rd, @s, @v)",
            ("@no", Next("TR-")), ("@d", deliveryId), ("@rd", DateTime.Today.ToString("yyyy-MM-dd")), ("@s", specialTripId), ("@v", isVoid ? 1 : 0));

    public static int Invoice(int deliveryId, decimal net, string status = "Unpaid", string client = "CLIENT A", int soaId = 0, int specialTripId = 0, int dueInDays = 15)
        => (int)Checks.Exec(@"INSERT INTO Invoices (InvoiceNumber, SoaId, DeliveryId, ClientName, IssueDate, DueDate, TotalAmount, Deductions, NetAmount, Status, SpecialTripId)
                              VALUES (@no, @soa, @d, @c, @i, @due, @n, 0, @n, @s, @st)",
            ("@no", Next("9")), ("@soa", soaId), ("@d", deliveryId), ("@c", client), ("@i", DateTime.Today.ToString("yyyy-MM-dd")),
            ("@due", DateTime.Today.AddDays(dueInDays).ToString("yyyy-MM-dd")), ("@n", (double)net), ("@s", status), ("@st", specialTripId));

    public static int Soa(string client = "CLIENT A", string status = "Issued", decimal total = 0m)
        => (int)Checks.Exec(@"INSERT INTO StatementsOfAccount (SoaNumber, ClientName, PeriodCovered, IssueDate, TotalAmount, Status) VALUES (@no, @c, 'Nov 2026', @d, @t, @s)",
            ("@no", Next("SOA-")), ("@c", client), ("@d", DateTime.Today.ToString("yyyy-MM-dd")), ("@t", (double)total), ("@s", status));

    public static int SoaItem(int soaId, int deliveryId, int specialTripId = 0)
        => (int)Checks.Exec(@"INSERT INTO StatementOfAccountItems (SoaId, TripNumber, DeliveryId, SpecialTripId, Amount) VALUES (@s, 1, @d, @st, 100)",
            ("@s", soaId), ("@d", deliveryId), ("@st", specialTripId));

    public static int Receipt(int invoiceId, decimal amount, string receiptNo = "", int soaId = 0)
        => (int)Checks.Exec(@"INSERT INTO CollectionReceipts (ReceiptNumber, InvoiceId, PaymentDate, AmountPaid, PaymentMethod, SoaId) VALUES (@no, @i, @d, @a, 'Cash', @s)",
            ("@no", receiptNo.Length > 0 ? receiptNo : Next("7")), ("@i", invoiceId), ("@d", DateTime.Today.ToString("yyyy-MM-dd")), ("@a", (double)amount), ("@s", soaId));
}
