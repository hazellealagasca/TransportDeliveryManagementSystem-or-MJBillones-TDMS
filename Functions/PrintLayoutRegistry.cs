using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>One printable form that Settings → Print layouts can edit.</summary>
    public sealed class PrintLayoutEntry
    {
        public string Group { get; }
        public string Key { get; }
        public string Name { get; }
        public Func<PrintFormDef> Build { get; }
        public PrintLayoutEntry(string group, string key, string name, Func<PrintFormDef> build) { Group = group; Key = key; Name = name; Build = build; }
    }
    /// <summary>
    /// Every form the app prints, each built from made-up sample records (no real record is needed), so its layout can be
    /// edited in Settings → Print layouts. The same <see cref="PrintFormDef.Key"/> is used by the real print, so what is saved here
    /// applies to every later print of the form.
    /// </summary>
    public static class PrintLayoutRegistry
    {
        public const string GroupForms = "Forms & statements";
        public const string GroupLists = "Lists & registers";
        /// <summary>Titles of the lists printed from a table (the form key is "Grid." + the title without spaces and symbols). A list whose title changes with the screen (dashboard schedule, Reports) is not listed.</summary>
        private static readonly string[] ListTitles =
        {
            "Client Billing & Accounts Receivable Statement", "Client Service Invoices", "Client Companies", "Official Collection Receipts Log Statement", "Collection Receipts",
            "Crew Personnel Roster & Wage Report", "Delivery Route Directory", "Delivery Dispatch Manifest Report", "Incident log", "Incident & Crate Damage Incident Logs",
            "Standard Transport Rate Matrix Schedule", "Statements of Account", "Plant & Store Trip Documentation Report", "Truck & Vehicle Inventory Report",
            "User Accounts Access & Credentials Audit Register"
        };
        public static string GridKey(string title) => "Grid." + Regex.Replace(title, "[^A-Za-z0-9]+", "");
        public static IReadOnlyList<PrintLayoutEntry> All()
        {
            var list = new List<PrintLayoutEntry>
            {
                new(GroupForms, "OfficialReceipt", "Collection receipt", () => PrintForms.CollectionReceipt(SampleReceipt(), forceSpecial: false)),
                new(GroupForms, "OfficialReceiptSpecial", "Collection receipt (special trip)", () => PrintForms.CollectionReceipt(SampleReceipt(), forceSpecial: true)),
                new(GroupForms, "SOA_Official", "Statement of account", () => { var (s, i) = SampleSoa(SoaTypes.Normal); return PrintForms.StatementOfAccount(s, i); }),
                new(GroupForms, "SOA_Special", "Statement of account (special trips)", () => { var (s, i) = SampleSoa(SoaTypes.Special); return PrintForms.SpecialTripStatementOfAccount(s, i); }),
                new(GroupForms, "SOAInvoices", "Statement of account (invoices)", () => PrintForms.StatementOfInvoices(SampleSoa(SoaTypes.Normal).Soa, new List<Invoice> { SampleInvoice() })),
                new(GroupForms, "TripReport", "Trip report", () => PrintForms.TripReport(SampleDelivery(), SampleTrip(), SampleStops())),
                new(GroupForms, "DeliveryDossier", "Delivery dossier", () => PrintForms.DeliveryDossier(SampleDelivery(), null, SampleTrip(), SampleStops())),
                new(GroupForms, "LoadingSheet", "Loading schedule", () => PrintForms.LoadingSheetSample()),
                new(GroupForms, "PaySlip", "Crew trip pay record", () => PrintForms.PaySlip(SampleCrew(), new List<Delivery> { SampleDelivery() }, DateTime.Today.AddDays(-14), DateTime.Today)),
                new(GroupForms, "CrewDossier", "Crew profile & history", () => PrintForms.CrewDossier(SampleCrew(), new List<Delivery> { SampleDelivery() })),
            };
            foreach (var t in ListTitles)
            {
                string title = t;
                list.Add(new(GroupLists, GridKey(title), title, () => GridSample(title)));
            }
            return list;
        }
        private static PrintFormDef GridSample(string title)
        {
            using var grid = new DataGridView();   // no columns: a list's columns follow the list on screen
            var def = PrintForms.GridReport(title, grid, "");
            def.LayoutHint = "A list prints the columns shown on screen when you print it, so the columns are not chosen here. Everything else (title, header, signatures, notes) applies to every print of this list.";
            return def;
        }
        // ---------------------------------------------------------------- made-up records
        private static CollectionReceipt SampleReceipt() => new() { ReceiptNumber = "SAMPLE-0001", PaymentDate = DateTime.Today, AmountPaid = 12500m, PaymentMethod = "Cash" };
        private static (StatementOfAccount Soa, List<StatementOfAccountItem> Items) SampleSoa(string type)
        {
            var soa = new StatementOfAccount { Id = 0, SoaNumber = "SOA-SAMPLE", ClientName = "Sample client", ClientAddress = "Sample address", SoaType = type, IssueDate = DateTime.Today, PeriodCovered = "Sample period" };
            var items = new List<StatementOfAccountItem>
            {
                new() { TripNumber = 1, DrNumbers = "DR-1", Destination = "Sample store", PlateNumber = "ABC 123", Tonnage = "6T", DropCount = 1, Amount = 10000m, FuelSurcharge = 500m,
                        DriverHelper = "Driver / Helper", Zone = "Zone 1", TotalCrates = 100, DeliveryType = "REF", DateReceived = DateTime.Today.ToString("MM/dd/yyyy") }
            };
            return (soa, items);
        }
        private static Invoice SampleInvoice() => new() { InvoiceNumber = "SI-SAMPLE", ClientName = "Sample client", TotalAmount = 10500m, FuelSurcharge = 500m, NetAmount = 10500m, DispatchNumber = "DSP-0001", TripReportNumber = "TR-0001" };
        private static Crew SampleCrew() => new() { Id = 1, FullName = "Sample Driver", Role = "Driver", ContactNumber = "09170000000", DailyWage = 800m, FoodAllowance = 100m };
        private static Delivery SampleDelivery() => new()
        {
            DispatchNumber = "DSP-0001", ClientName = "Sample client", Destination = "Sample route", TruckInfo = "ABC 123", DriverName = "Sample Driver", AideNames = "Sample Helper",
            GCrate = 40, ECrate = 60, TotalCrates = 100, DispatchDate = DateTime.Today, DepartureTime = "06:00", Status = "Completed"
        };
        private static TripReport SampleTrip() => new() { TripReportNumber = "TR-0001", DispatchNumber = "DSP-0001", ReportDate = DateTime.Today, DeliveredCrates = 100, DeliveredECrates = 60, DeliveredGCrates = 40 };
        private static List<DispatchStop> SampleStops() => new()
        {
            new() { StopSequence = 1, Destination = "Sample store one", StoreCode = "S001", ECrate = 30, GCrate = 20, TotalCrates = 50, DeliveredECrates = 30, DeliveredGCrates = 20, DeliveredCrates = 50, ArrivalTime = "08:00", DepartureTime = "08:30", StoreManagerName = "Store representative", Status = "Completed" },
            new() { StopSequence = 2, Destination = "Sample store two", StoreCode = "S002", ECrate = 30, GCrate = 20, TotalCrates = 50, DeliveredECrates = 30, DeliveredGCrates = 20, DeliveredCrates = 50, ArrivalTime = "09:30", DepartureTime = "10:00", StoreManagerName = "Store representative", Status = "Completed" }
        };
    }
}
