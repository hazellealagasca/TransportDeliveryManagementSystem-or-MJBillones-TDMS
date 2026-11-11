using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using static Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions.PrintKit;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Every printable form, built from its record: the fields (with editable labels), the parts and table columns
    /// that can be switched on/off, and the signature lines. <see cref="PrintStudio"/> shows them for review.
    /// </summary>
    public static class PrintForms
    {
        private static PrintOptions NewOptions(string key, string title, string subtitle = "")
            => new PrintOptions { FormKey = key, Title = title, Subtitle = subtitle, Header = DefaultHeader(), Sections = new List<PrintChoice>() };
        private static void AddCommon(PrintOptions o) => o.Sections.AddRange(CommonSections());
        private static string D(DateTime d) => d.ToString("MMMM d, yyyy");
        private static string Crew(Delivery d)
        {
            string aides = !string.IsNullOrWhiteSpace(d.AideNames) ? d.AideNames : d.AideName;
            return string.IsNullOrWhiteSpace(aides) || aides == "None" ? d.DriverName : $"{d.DriverName} / {aides}";
        }
        // =====================================================================================================
        // Table report (any list in the app)
        // =====================================================================================================
        /// <summary>
        /// A record's details as label / value pairs (the record pop-up's Print button), through the same print window
        /// as every other printout.
        /// </summary>
        public static PrintFormDef RecordSheet(string title, string subtitle, IEnumerable<(string Label, string Value)> details)
        {
            var list = details.Where(d => !string.IsNullOrWhiteSpace(d.Label)).ToList();
            string clean = Regex.Replace(title ?? "", @"[^\p{L}\p{N}\s:·#\-/().,&']", "").Trim();
            if (clean.Length == 0) clean = "Record details";
            string key = "Record." + Regex.Replace(clean.Split(':')[0], "[^A-Za-z0-9]+", "");
            var o = NewOptions(key, clean, string.IsNullOrWhiteSpace(subtitle) ? D(DateTime.Today) : Regex.Replace(subtitle, @"[^\p{L}\p{N}\s:·#\-/().,&']", "").Trim());
            o.Fields = list.Select((d, i) => new PrintField($"f{i}", d.Label, d.Value ?? "")).ToList();
            AddCommon(o);
            o.Signatures = new List<PrintSig>
            {
                new PrintSig("Prepared by", CurrentUserName, "Prepared by", true),
                new PrintSig("Approved by", OwnerName, "Owner")
            };
            var def = new PrintFormDef { Key = key, Name = clean, FileName = Regex.Replace(clean, "[^A-Za-z0-9]+", "_"), Defaults = o };
            def.Body = opt => FieldGrid(opt, 2, o.Fields.Select(f => f.Key).ToArray());
            return def;
        }
        public static PrintFormDef GridReport(string title, DataGridView grid, string notes)
        {
            var cols = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible && !(c is DataGridViewButtonColumn) && !(c is DataGridViewImageColumn)).OrderBy(c => c.DisplayIndex).ToList();
            var rows = new List<(bool Selected, Dictionary<string, string> Cells)>();
            foreach (DataGridViewRow r in grid.Rows)
            {
                if (r.IsNewRow || !r.Visible) continue;
                var cells = new Dictionary<string, string>();
                foreach (var c in cols)
                {
                    var v = r.Cells[c.Index].FormattedValue;
                    cells[$"c{c.Index}"] = Esc(v is bool b ? (b ? "Yes" : "No") : Convert.ToString(v) ?? "");
                }
                rows.Add((r.Selected, cells));
            }
            int selected = rows.Count(r => r.Selected);
            string key = "Grid." + Regex.Replace(title, "[^A-Za-z0-9]+", "");
            var o = NewOptions(key, title, $"{D(DateTime.Today)}");
            o.Notes = notes ?? "";
            o.HasRowScope = selected > 0 && selected < rows.Count;
            o.SelectedRowCount = selected;
            o.Columns = cols.Select(c => new PrintChoice($"c{c.Index}", Regex.Replace(c.HeaderText, "[▲▼]", "").Trim())).ToList();
            bool money = Regex.IsMatch(title, "Billing|Collection|Invoice|Receipt|Financial|Payment", RegexOptions.IgnoreCase);
            o.Sections.Add(new PrintChoice("count", "Record count line"));
            if (money) o.Sections.Add(new PrintChoice("fin", "Collected / receivable totals"));
            AddCommon(o);
            o.Signatures = new List<PrintSig>
            {
                new PrintSig("Prepared by", CurrentUserName, "Prepared by", true),
                new PrintSig("Checked by", "", "", false, false),
                new PrintSig("Approved by", OwnerName, "Owner")
            };
            if (rows.Count > 14 || cols.Count > 7) o.Orientation = "Landscape";
            var def = new PrintFormDef { Key = key, Name = title, FileName = Regex.Replace(title, "[^A-Za-z0-9]+", "_"), Defaults = o };
            def.Body = opt =>
            {
                var use = opt.HasRowScope && opt.SelectedRowsOnly ? rows.Where(r => r.Selected).ToList() : rows;
                var sb = new StringBuilder();
                if (opt.On("count")) sb.Append($"<div style='margin:2px 0 6px 0;font-weight:600'>{use.Count} record{(use.Count == 1 ? "" : "s")}{(opt.HasRowScope && opt.SelectedRowsOnly ? " (selected rows only)" : "")}</div>");
                if (money && opt.On("fin"))
                {
                    try
                    {
                        var fin = MJdatabase.GetFinancialTotals();
                        sb.Append(Stats(new[] { ("Total billed", Money(fin.TotalBilled)), ("Collected", Money(fin.TotalCollected)), ("Still receivable", Money(fin.TotalToBeCollected)) }));
                    }
                    catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
                }
                sb.Append(Table(opt, cols.Select(c => ($"c{c.Index}", Regex.Replace(c.HeaderText, "[▲▼]", "").Trim(), c.DefaultCellStyle.Alignment.ToString().EndsWith("Right") ? "num" : "")), use.Select(r => (IDictionary<string, string>)r.Cells)));
                return sb.ToString();
            };
            return def;
        }
        // =====================================================================================================
        // Official / collection receipt
        // =====================================================================================================
        public static PrintFormDef CollectionReceipt(CollectionReceipt cr)
        {
            // Nov 6: one receipt can pay a whole statement (one row per invoice in the database); the slip shows each invoice and the totals.
            var rcRows = MJdatabase.GetReceiptRows(cr.ReceiptNumber);
            if (rcRows.Count == 0) rcRows.Add(cr);
            var allInv = MJdatabase.GetInvoices();
            var paidInvs = rcRows.Select(r => (Row: r, Inv: allInv.Find(i => i.Id == r.InvoiceId))).ToList();
            var inv = paidInvs[0].Inv ?? allInv.Find(i => !string.IsNullOrEmpty(cr.InvoiceNumber) && i.InvoiceNumber.Equals(cr.InvoiceNumber, StringComparison.OrdinalIgnoreCase));
            decimal rcTotal = rcRows.Sum(r => r.AmountPaid);
            var rcSoa = cr.SoaId > 0 || rcRows.Any(r => r.SoaId > 0) ? MJdatabase.GetStatementsOfAccount().FirstOrDefault(s => s.Id == (cr.SoaId > 0 ? cr.SoaId : rcRows.First(r => r.SoaId > 0).SoaId)) : null;
            var client = inv == null ? null : MJdatabase.GetClients().FirstOrDefault(c => c.CompanyName.Equals(inv.ClientName, StringComparison.OrdinalIgnoreCase));
            decimal gross = paidInvs.Sum(x => x.Inv?.Freight ?? x.Row.AmountPaid), fuelSum = paidInvs.Sum(x => x.Inv?.FuelSurcharge ?? 0), whtSum = paidInvs.Sum(x => x.Inv?.Deductions ?? 0), net = paidInvs.Sum(x => x.Inv?.NetAmount ?? x.Row.AmountPaid); // Nov 7: amount + fuel surcharge, less the statement's withholding tax
            string method = string.IsNullOrWhiteSpace(cr.PaymentMethod) ? "Cash" : cr.PaymentMethod;
            // Nov 6 workflow: special trips get their own receipt slip (their trip reference and destination), normal trips the SI / trip slip.
            var tripInvs = MJdatabase.GetTripInvoices().Where(t => rcRows.Any(r => r.InvoiceId == t.Id)).ToList();
            bool special = rcSoa != null ? rcSoa.SoaType == SoaTypes.Special : tripInvs.Count > 0 && tripInvs.All(t => t.SpecialTripId > 0);
            var specialTrips = special ? MJdatabase.GetSpecialTrips().Where(t => tripInvs.Any(i => i.SpecialTripId == t.Id)).ToDictionary(t => t.Id) : new Dictionary<int, SpecialTrip>();
            var o = NewOptions(special ? "OfficialReceiptSpecial" : "OfficialReceipt", special ? "Collection Receipt — Special Trip" : "Collection Receipt", $"No. {cr.ReceiptNumber}");
            o.CopyLabel = "Original";
            o.Fields = new List<PrintField>
            {
                new PrintField("receipt_no", "Receipt no.", cr.ReceiptNumber),
                new PrintField("date", "Date", D(cr.PaymentDate)),
                new PrintField("received_from", "Received from", inv?.ClientName ?? ""),
                new PrintField("address", "Address", client?.Address ?? ""),
                new PrintField("client_tin", "TIN", client?.TIN ?? ""),
                new PrintField("for", "In payment of", rcSoa != null ? $"Statement of Account {rcSoa.SoaNumber}: Service Invoice{(paidInvs.Count == 1 ? "" : "s")} {string.Join(", ", paidInvs.Select(x => x.Row.InvoiceNumber))}" : inv != null ? $"Service Invoice {inv.InvoiceNumber}{(string.IsNullOrWhiteSpace(inv.DispatchNumber) ? "" : " (dispatch " + inv.DispatchNumber + ")")}" : "Hauling & delivery services"),
                new PrintField("amount", "Amount", Money(rcTotal)),
                new PrintField("amount_words", "Amount in words", AmountInWords(rcTotal)),
                new PrintField("method", "Payment method", method),
                new PrintField("reference", "Reference / check no.", cr.ReferenceNumber),
                new PrintField("remarks", "Remarks", cr.Notes, true, true, true),
                new PrintField("atp", "BIR / printer details", "BIR Authority to Print OCN: 17AAU20240000004957", false, true, true)
            };
            o.Sections = new List<PrintChoice>
            {
                new PrintChoice("method_boxes", "Payment method tick boxes"),
                new PrintChoice("breakdown", special ? "Special trips paid" : "Invoice breakdown table"),
                new PrintChoice("totals", "Payment summary"),
            };
            AddCommon(o);
            // Payments are always in full, so there is no balance column.
            o.Columns = special
                ? new List<PrintChoice> { new("ref", "Trip reference"), new("completed", "Date completed", false), new("dest", "Destination"), new("inv", "SI No."), new("paid", "Amount paid") }
                : new List<PrintChoice> { new("inv", "Invoice #"), new("amount", "Amount"), new("fuel", "Fuel surcharge"), new("wht", "Less: withholding tax"), new("net", "Total due"), new("paid", "Amount paid") };
            o.Signatures = new List<PrintSig> { new PrintSig("Received by", CurrentUserName, "Cashier / Collector", true), new PrintSig("Paid by", "", "Client representative", false) };
            var def = new PrintFormDef { Key = o.FormKey, Name = special ? "Collection receipt (special trip)" : "Collection receipt", FileName = $"Receipt_{cr.ReceiptNumber}", Defaults = o };
            def.Body = opt =>
            {
                var sb = new StringBuilder();
                sb.Append(FieldGrid(opt, 2, "receipt_no", "date", "received_from", "client_tin", "address", "for"));
                if (opt.FOn("amount"))
                {
                    sb.Append("<table style='width:100%;border-collapse:collapse;margin-top:10px'><tr>");
                    sb.Append($"<td style='border:2px solid #000;padding:10px 12px;width:38%'><div style='font-size:.8em;font-weight:700;text-transform:uppercase'>{Esc(opt.L("amount"))}</div><div style='font-size:1.9em;font-weight:900'>{Esc(opt.F("amount"))}</div></td>");
                    sb.Append($"<td style='padding:10px 12px;border:1px solid #777'>{(opt.FOn("amount_words") ? $"<div style='font-size:.8em;font-weight:700;text-transform:uppercase'>{Esc(opt.L("amount_words"))}</div><div style='font-size:1.1em;font-weight:700;font-style:italic'>{Esc(opt.F("amount_words"))}</div>" : "")}</td>");
                    sb.Append("</tr></table>");
                }
                if (opt.On("method_boxes"))
                {
                    string m = opt.F("method");
                    bool cash = m.Equals("Cash", StringComparison.OrdinalIgnoreCase), chk = Regex.IsMatch(m, "Check|Cheque", RegexOptions.IgnoreCase),
                         bank = Regex.IsMatch(m, "Bank|Transfer|GCash|Maya", RegexOptions.IgnoreCase), card = m.Contains("Card", StringComparison.OrdinalIgnoreCase);
                    sb.Append($"<div style='margin-top:10px'>{Checks(new[] { ("Cash", cash), ("Check", chk), ("Bank / e-wallet", bank), ("Card", card), (cash || chk || bank || card ? "Other" : "Other: " + m, !(cash || chk || bank || card)) })}</div>");
                }
                sb.Append(FieldGrid(opt, 2, "reference", "method"));
                if (opt.On("breakdown"))
                {
                    sb.Append(SectionTitle(special ? "Special trips paid" : "Applied to"));
                    if (special)
                    {
                        var completedOn = opt.Col("completed") ? MJdatabase.GetTripCompletedDates() : null;
                        sb.Append(Table(opt, new[] { ("ref", "Trip reference", ""), ("completed", "Date completed", ""), ("dest", "Destination", ""), ("inv", "SI No.", ""), ("paid", "Amount paid", "num") },
                            paidInvs.Select(x =>
                            {
                                var ti = tripInvs.FirstOrDefault(t => t.Id == x.Row.InvoiceId);
                                specialTrips.TryGetValue(ti?.SpecialTripId ?? 0, out var st);
                                return Row(("ref", Esc(st?.OrderReference ?? x.Inv?.DispatchNumber ?? "")), ("completed", Esc(completedOn?.Text(0, ti?.SpecialTripId ?? 0, "") ?? "")), ("dest", Esc(st == null ? "" : (string.IsNullOrWhiteSpace(st.DestinationAddress) ? st.Address : st.DestinationAddress))),
                                           ("inv", Esc(x.Inv?.InvoiceNumber ?? x.Row.InvoiceNumber)), ("paid", $"<b>{Esc(Money(x.Row.AmountPaid))}</b>"));
                            }).ToArray()));
                    }
                    else
                        sb.Append(Table(opt, new[] { ("inv", "Invoice #", ""), ("amount", "Amount", "num"), ("fuel", "Fuel surcharge", "num"), ("wht", "Less: withholding tax", "num"), ("net", "Total due", "num"), ("paid", "Amount paid", "num") },
                            paidInvs.Select(x => Row(("inv", (x.Inv?.InvoiceNumber ?? x.Row.InvoiceNumber) + (string.IsNullOrWhiteSpace(x.Inv?.TripReportNumber) && string.IsNullOrWhiteSpace(x.Inv?.DispatchNumber) ? "" : " (" + string.Join(" · ", new[] { string.IsNullOrWhiteSpace(x.Inv?.TripReportNumber) ? "" : "trip report " + x.Inv!.TripReportNumber, string.IsNullOrWhiteSpace(x.Inv?.DispatchNumber) ? "" : "dispatch " + x.Inv!.DispatchNumber }.Where(t => t.Length > 0)) + ")")),
                                ("amount", Money(x.Inv?.Freight ?? x.Row.AmountPaid)), ("fuel", (x.Inv?.FuelSurcharge ?? 0) > 0 ? Money(x.Inv!.FuelSurcharge) : "-"), ("wht", (x.Inv?.Deductions ?? 0) > 0 ? "- " + Money(x.Inv!.Deductions) : "-"), ("net", Money(x.Inv?.NetAmount ?? x.Row.AmountPaid)), ("paid", $"<b>{Esc(Money(x.Row.AmountPaid))}</b>"))).ToArray()));
                }
                // Paid in full: the summary shows what was received, never a balance.
                if (opt.On("totals")) sb.Append(Summary(new[] { (special ? "Special trips paid" : "Invoices paid", paidInvs.Count.ToString()), ("Amount", Money(gross)), ("Fuel surcharge", Money(fuelSum)), ("Less: withholding tax", "- " + Money(whtSum)), ("Total due", Money(net)), ("Amount received (paid in full)", Money(rcTotal)), ("Method / reference", method + (string.IsNullOrWhiteSpace(cr.ReferenceNumber) ? "" : " · " + cr.ReferenceNumber)) }));
                sb.Append(FieldGrid(opt, 1, "remarks"));
                if (opt.FOn("atp") && !string.IsNullOrWhiteSpace(opt.F("atp"))) sb.Append($"<div style='margin-top:10px;font-size:.78em;text-align:center;color:#333'>{Multi(opt.F("atp"))}</div>");
                return sb.ToString();
            };
            return def;
        }
        // =====================================================================================================
        // Statement of account (trip lines)
        // =====================================================================================================
        /// <summary>The trip report number(s) of a trip's lines, one per line (normally one).</summary>
        private static string TripReportNos(List<StatementOfAccountItem> g) =>
            string.Join("<br/>", g.Select(x => (x.TripReportNumber ?? "").Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(x => Esc(x).Replace("-", "-<wbr>"))); // may break after a hyphen, so the table fits the page
        /// <summary>The official SOA print: the Special Trip billing layout for a Special Trips SOA, else the multi-drop store DR layout.</summary>
        public static PrintFormDef StatementOfAccount(StatementOfAccount soa, List<StatementOfAccountItem> items)
            => BuildStatementOfAccount(soa, items, soa.SoaType == SoaTypes.Special);

        /// <summary>The Special Trip billing layout (one line per special trip: its reference, destination and trip charge).</summary>
        public static PrintFormDef SpecialTripStatementOfAccount(StatementOfAccount soa, List<StatementOfAccountItem> items)
            => BuildStatementOfAccount(soa, items, special: true);

        private static PrintFormDef BuildStatementOfAccount(StatementOfAccount soa, List<StatementOfAccountItem> items, bool special)
        {
            var groups = items.GroupBy(i => i.TripNumber).OrderBy(g => g.Key).Select(g => g.ToList()).ToList();
            // Nov 7: a trip's amount and fuel surcharge are those of its service invoice on this statement (read now, so the table always
            // matches the invoice). Lines with no such invoice (older statements) keep their own lines added up.
            var tripInvoices = items.Any(i => i.DeliveryId > 0 || i.SpecialTripId > 0) && soa.Id > 0 ? MJdatabase.GetTripInvoices() : new List<TripInvoice>();
            TripInvoice? InvoiceOf(List<StatementOfAccountItem> g)
            {
                var ti = MJdatabase.TripInvoiceFor(tripInvoices, g[0].DeliveryId, g[0].DeliveryId > 0 ? 0 : g[0].SpecialTripId);
                return ti != null && ti.SoaId == soa.Id ? ti : null;
            }
            decimal TripAmount(List<StatementOfAccountItem> g) => InvoiceOf(g)?.Freight ?? g.Sum(x => x.Amount);
            decimal TripFuel(List<StatementOfAccountItem> g) => InvoiceOf(g)?.Fuel ?? g.Sum(x => x.FuelSurcharge);
            decimal amountSum = groups.Sum(TripAmount), fuelSum = groups.Sum(TripFuel);
            int crateSum = items.Sum(i => Math.Max(0, i.TotalCrates));
            // The figures of the statement (Nov 7): the ones saved with it; a statement not saved yet gets them from the one formula (SoaTotals):
            // VAT and the 2% withholding on the freight only, the fuel surcharge added outside both.
            bool saved = soa.TotalAmount > 0;
            var fm = SoaTotals.From(amountSum, fuelSum);
            decimal totalSales = saved ? soa.TotalSalesVatInc : fm.TotalSales;
            decimal vat = saved ? soa.VatAmount : fm.Vat;
            decimal netOfVat = saved ? soa.NetOfVat : fm.NetOfVat;
            decimal fuel = saved ? soa.FuelSurchargeTotal : fm.Fuel;
            decimal wtax = saved ? soa.WithholdingTax : fm.Withholding;
            decimal due = saved ? soa.TotalAmount : fm.Due;
            // An older statement kept its fuel inside the sales, so its figures don't add up the sheet's way: it keeps the older five-line computation.
            bool sheetStyle = Math.Abs(netOfVat + fuel + vat - wtax - due) <= 0.02m;
            // Nov 6 workflow: new form keys, so layouts saved before (with zone, crates, trip report # columns) don't come back.
            var o = NewOptions(special ? "SOA_Special" : "SOA_Official", special ? "Statement of Account — Special Trips" : "Statement of Account", $"{soa.SoaNumber} · {D(soa.IssueDate)}");
            o.Orientation = "Landscape";
            o.Fields = new List<PrintField>
            {
                new PrintField("soa_no", "SOA no.", soa.SoaNumber),
                new PrintField("date", "Date", D(soa.IssueDate)),
                new PrintField("billed_to", "Billed to", soa.ClientName),
                new PrintField("period", "Period covered", soa.PeriodCovered),
                new PrintField("client_address", "Address", soa.ClientAddress),
                new PrintField("client_tin", "TIN", soa.ClientTin),
                new PrintField("type", "Delivery type", soa.DeliveryType),
                new PrintField("si_no", "Sales invoice no.", soa.SalesInvoiceNo),
                new PrintField("pay_instr", "Payment instructions", "Please make checks payable to M.J. BILLONES TRANSPORT SERVICE. Kindly settle within 15 days of receipt.", false, true, true)
            };
            o.Sections = new List<PrintChoice> { new("billto", "Billed-to details"), new("trips", "Trip lines table"), new("trip_total", "Trip totals row"), new("taxes", "VAT / withholding computation"), new("pay", "Payment instructions") };
            AddCommon(o);
            // Commercial line items only. Crew appear by name as proof of who delivered, never with an amount.
            o.Columns = special
                ? new List<PrintChoice> { new("no", "No."), new("date", "Date completed"), new("trip_report", "Trip reference"), new("dest", "Destination"), new("plate", "Plate no."), new("tons", "Tons"), new("amount", "Trip charge"), new("fuel", "Fuel surcharge"), new("driver", "Driver / helper") }
                : new List<PrintChoice> { new("no", "Trip"), new("date", "Date store received"), new("zone", "Zone"), new("dr", "DR no."), new("dest", "Destination"), new("type", "Type of delivery"), new("plate", "Plate no."), new("tons", "Tons"), new("drops", "No. of drops"), new("amount", "Amount"), new("fuel", "Fuel surcharge"), new("driver", "Driver / Aide") };
            o.Signatures = new List<PrintSig>
            {
                new PrintSig("Prepared by", soa.PreparedBy, "Billing Officer", true),
                new PrintSig("Received by", soa.ReceivedBy1, "Accountant", true),
                new PrintSig("Received by", soa.ReceivedBy2, "Accountant", true, !string.IsNullOrWhiteSpace(soa.ReceivedBy2)),
                new PrintSig("Approved by", OwnerName, "Owner", false, false)
            };
            var def = new PrintFormDef { Key = o.FormKey, Name = special ? "Statement of account (special trips)" : "Statement of account", FileName = $"SOA_{soa.SoaNumber}", Defaults = o };
            def.Body = opt =>
            {
                var sb = new StringBuilder();
                if (opt.On("billto")) sb.Append(FieldGrid(opt, 2, "billed_to", "soa_no", "client_address", "date", "client_tin", "period", "type", "si_no"));
                bool sheetBlock = false;
                if (opt.On("trips"))
                {
                    var cols = (special
                        ? new (string Key, string Header, string Css, bool PerTrip)[]
                        {
                            ("no", "No.", "center", true), ("date", "Date completed", "center", true), ("trip_report", "Trip reference", "center", true), ("dest", "Destination", "", false),
                            ("plate", "Plate no.", "center", true), ("tons", "Tons", "center", true), ("amount", "Trip charge", "num", true), ("fuel", "Fuel surcharge", "num", true), ("driver", "Driver / helper", "", true)
                        }
                        : new (string Key, string Header, string Css, bool PerTrip)[]
                        {
                            ("no", "Trip", "center", true), ("date", "Date store received", "center", true), ("zone", "Zone", "center", true), ("dr", "DR no.", "center", false), ("dest", "Destination", "center", false), ("type", "Type of delivery", "center", true),
                            ("plate", "Plate no.", "center", true), ("tons", "Tons", "center", true),
                            ("drops", "No. of drops", "center", true), ("amount", "Amount", "num", true), ("fuel", "Fuel surcharge", "num", true), ("driver", "Driver / Aide", "center", true)
                        }).Where(c => opt.Col(c.Key)).ToList();
                    // Date completed = the trip report's date (looked up now, so statements saved earlier print it too); the saved line date is the fallback.
                    var completed = cols.Any(c => c.Key == "date") ? MJdatabase.GetTripCompletedDates() : null;
                    sb.Append(SectionTitle("Trips"));
                    sb.Append("<table class='grid sheet'><thead><tr>");
                    foreach (var c in cols) sb.Append($"<th class='{c.Css}{(c.Key == "fuel" ? " red" : "")}'>{Esc(c.Header)}</th>");
                    sb.Append("</tr></thead><tbody>");
                    int n = 0;
                    foreach (var g in groups)
                    {
                        n++;
                        var f = g[0];
                        for (int i = 0; i < g.Count; i++)
                        {
                            sb.Append("<tr>");
                            foreach (var c in cols)
                            {
                                if (c.PerTrip && i > 0) continue;
                                string span = c.PerTrip && g.Count > 1 ? $" rowspan='{g.Count}'" : "";
                                string v = c.Key switch
                                {
                                    "no" => n.ToString(), "trip_report" => TripReportNos(g), "crates" => g[i].TotalCrates > 0 ? g[i].TotalCrates.ToString("N0") : "-", "date" => Esc(completed!.Text(f.DeliveryId, f.SpecialTripId, f.DateReceived)), "zone" => Esc(f.Zone), "dr" => DrList.Html(g[i].DrNumbers), "code" => Esc(g[i].StoreCode), "dest" => $"<b>{Esc(g[i].Destination)}</b>",
                                    "type" => Esc(f.DeliveryType), "plate" => $"<b>{Esc(f.PlateNumber)}</b>", "tons" => Esc(f.Tonnage), "drops" => f.DropCount.ToString(),
                                    "amount" => $"<b>{TripAmount(g):N2}</b>", "fuel" => TripFuel(g) > 0 ? TripFuel(g).ToString("N2") : "-", "driver" => Esc(f.DriverHelper), _ => ""
                                };
                                sb.Append($"<td class='{c.Css}'{span}>{v}</td>");
                            }
                            sb.Append("</tr>");
                        }
                    }
                    if (groups.Count == 0) sb.Append($"<tr><td colspan='{cols.Count}' class='empty'>No trips on this statement.</td></tr>");
                    sb.Append("</tbody>");
                    int amtIdx = cols.FindIndex(c => c.Key == "amount");
                    sheetBlock = sheetStyle && opt.On("taxes") && amtIdx > 0;
                    if (sheetBlock)
                    {
                        // The statement sheet's totals block: directly under the table, labels on the left, values in the Amount column, the fuel column's total beside the first line.
                        int fuelIdx = cols.FindIndex(c => c.Key == "fuel"), after = cols.Count - amtIdx - 1;
                        var lines = new (string Label, decimal Value)[]
                        {
                            ("Total Sales (VAT Inclusive)", totalSales), ("Less: VAT", vat), ("Amount: Net of VAT", netOfVat), ("Fuel surcharge", fuel),
                            ("Add: VAT", vat), ("Less: Withholding Tax", wtax), ("TOTAL AMOUNT DUE", due)
                        };
                        sb.Append("<tfoot>");
                        for (int li = 0; li < lines.Length; li++)
                        {
                            string strong = li == lines.Length - 1 ? " strong" : "";
                            sb.Append($"<tr class='tot'><td colspan='{amtIdx}' class='lbl{strong}'>{Esc(lines[li].Label)}</td><td class='num{strong}'>{lines[li].Value:N2}</td>");
                            if (after > 0)
                            {
                                if (li == 0 && fuelIdx == amtIdx + 1) { sb.Append($"<td class='num'>{fuel:N2}</td>"); if (after > 1) sb.Append($"<td colspan='{after - 1}' class='nob'></td>"); }
                                else sb.Append($"<td colspan='{after}' class='nob'></td>");
                            }
                            sb.Append("</tr>");
                        }
                        sb.Append("</tfoot>");
                    }
                    else if (opt.On("trip_total") && cols.Count > 0)
                    {
                        sb.Append("<tfoot><tr>");
                        bool labelled = false;
                        foreach (var c in cols)
                        {
                            string v = c.Key == "amount" ? amountSum.ToString("N2") : c.Key == "fuel" ? fuelSum.ToString("N2") : c.Key == "crates" ? crateSum.ToString("N0") : "";
                            if (v.Length == 0 && !labelled && c.Key is not ("amount" or "fuel" or "crates")) { v = groups.Count == 1 ? "1 trip" : $"{groups.Count} trips"; labelled = true; }
                            sb.Append($"<td class='{c.Css}'>{v}</td>");
                        }
                        sb.Append("</tr></tfoot>");
                    }
                    sb.Append("</table>");
                }
                if (opt.On("taxes") && !sheetBlock)
                    sb.Append(Summary(new[]
                    {
                        ("Total sales (VAT inclusive)", totalSales.ToString("N2")), ("Less: VAT (12%)", vat.ToString("N2")), ("Net of VAT", netOfVat.ToString("N2")),
                        ("Less: Expanded withholding tax (2%)", "- " + wtax.ToString("N2")), ("Total amount due", "₱" + due.ToString("N2"))
                    }, "Computation"));
                if (opt.On("pay") && opt.FOn("pay_instr") && !string.IsNullOrWhiteSpace(opt.F("pay_instr"))) sb.Append($"<div class='note'><b>{Esc(opt.L("pay_instr"))}:</b> {Multi(opt.F("pay_instr"))}</div>");
                return sb.ToString();
            };
            return def;
        }
        // =====================================================================================================
        // Statement of account (invoice list)
        // =====================================================================================================
        public static PrintFormDef StatementOfInvoices(StatementOfAccount soa, List<Invoice> invoices)
        {
            var o = NewOptions("SOAInvoices", "Statement of Account", $"{soa.SoaNumber} · {D(soa.IssueDate)}");
            o.Fields = new List<PrintField>
            {
                new PrintField("billed_to", "Billed to", soa.ClientName), new PrintField("soa_no", "SOA no.", soa.SoaNumber),
                new PrintField("period", "Period covered", string.IsNullOrWhiteSpace(soa.PeriodCovered) ? "Current billing cycle" : soa.PeriodCovered), new PrintField("date", "Date", D(soa.IssueDate)),
                new PrintField("status", "Status", soa.Status),
                new PrintField("pay_instr", "Payment instructions", "Please make checks payable to M.J. BILLONES TRANSPORT SERVICE.", false, true, true)
            };
            o.Sections = new List<PrintChoice> { new("billto", "Billed-to details"), new("invoices", "Invoice table"), new("totals", "Totals"), new("pay", "Payment instructions") };
            AddCommon(o);
            o.Columns = new List<PrintChoice> { new("no", "No."), new("inv", "Invoice #"), new("trip_report", "Trip report #"), new("dispatch", "Dispatch #"), new("completed", "Date completed", false), new("issued", "Issued"), new("due", "Due"), new("amount", "Amount"), new("fuel", "Fuel surcharge"), new("wht", "Less: withholding tax"), new("net", "Total due"), new("status", "Status") };
            o.Signatures = new List<PrintSig> { new PrintSig("Prepared by", string.IsNullOrWhiteSpace(soa.PreparedBy) ? CurrentUserName : soa.PreparedBy, "Billing Officer", true), new PrintSig("Received by", soa.ReceivedBy1, "Client", true) };
            var def = new PrintFormDef { Key = o.FormKey, Name = "Statement of account (invoices)", FileName = $"SOA_{soa.SoaNumber}", Defaults = o };
            def.Body = opt =>
            {
                var sb = new StringBuilder();
                if (opt.On("billto")) sb.Append(FieldGrid(opt, 2, "billed_to", "soa_no", "period", "date", "status"));
                int n = 0;
                var completedOn = opt.On("invoices") && opt.Col("completed") ? MJdatabase.GetTripCompletedDates() : null;
                if (opt.On("invoices"))
                    sb.Append(Table(opt, new[] { ("no", "No.", "center"), ("inv", "Invoice #", ""), ("trip_report", "Trip report #", ""), ("dispatch", "Dispatch #", ""), ("completed", "Date completed", ""), ("issued", "Issued", ""), ("due", "Due", ""), ("amount", "Amount", "num"), ("fuel", "Fuel surcharge", "num"), ("wht", "Less: withholding tax", "num"), ("net", "Total due", "num"), ("status", "Status", "") },
                        invoices.Select(i => Row(("no", ++n), ("inv", $"<b>{Esc(i.InvoiceNumber)}</b>"), ("trip_report", Esc(i.TripReportNumber)), ("dispatch", i.DispatchNumber), ("completed", Esc(completedOn?.Text(i.DeliveryId, 0, "") ?? "")), ("issued", i.IssueDate.ToString("MM/dd/yyyy")), ("due", i.DueDate.ToString("MM/dd/yyyy")),
                            ("amount", i.Freight.ToString("N2")), ("fuel", i.FuelSurcharge > 0 ? i.FuelSurcharge.ToString("N2") : "-"), ("wht", i.Deductions > 0 ? "- " + i.Deductions.ToString("N2") : "-"), ("net", $"<b>{i.NetAmount:N2}</b>"), ("status", i.Status)))));
                if (opt.On("totals"))
                    sb.Append(Summary(new[] { ("Total amount", invoices.Sum(i => i.Freight).ToString("N2")), ("Total fuel surcharge", invoices.Sum(i => i.FuelSurcharge).ToString("N2")), ("Less: withholding tax", "- " + invoices.Sum(i => i.Deductions).ToString("N2")), ("Total payable", "₱" + invoices.Sum(i => i.NetAmount).ToString("N2")) }));
                if (opt.On("pay") && opt.FOn("pay_instr")) sb.Append($"<div class='note'><b>{Esc(opt.L("pay_instr"))}:</b> {Multi(opt.F("pay_instr"))}</div>");
                return sb.ToString();
            };
            return def;
        }
        // =====================================================================================================
        // Crew pay slip
        // =====================================================================================================
        public static PrintFormDef PaySlip(Crew crew, List<Delivery> trips, DateTime from, DateTime to)
        {
            Dictionary<(int, int), TripCrewPay>? recorded = null;
            try { recorded = MJdatabase.GetAllTripCrewPay(); } catch (System.Exception swx2) { ErrLog.Swallowed(swx2); }
            var pay = trips.OrderBy(t => t.DispatchDate).Select(t => (Trip: t, Pay: MJdatabase.ResolveTripPay(crew, t, recorded))).ToList();
            decimal basePay = pay.Sum(p => p.Pay.BasePay), allow = pay.Sum(p => p.Pay.Allowance);
            decimal net = basePay + allow;
            var o = NewOptions("PaySlip", "Crew Trip Pay Record", $"{from:MMM d} – {to:MMM d, yyyy}");
            o.Fields = new List<PrintField>
            {
                new PrintField("name", "Employee", crew.FullName), new PrintField("code", "Crew ID", crew.CrewCode),
                new PrintField("role", "Position", crew.Role), new PrintField("period", "Pay period", $"{from:MMMM d} – {to:MMMM d, yyyy}"),
                new PrintField("contact", "Contact no.", crew.ContactNumber, true, false), new PrintField("license", "Driver's license", crew.LicenseDisplay, true, false),
                new PrintField("rate", "Pay per trip", Money(crew.DailyWage)), new PrintField("meal", "Meal allowance per trip", Money(crew.FoodAllowance)),
                new PrintField("issued", "Date issued", D(DateTime.Today)), new PrintField("pay_method", "Paid via", "Cash", false, false)
            };
            o.Sections = new List<PrintChoice> { new("employee", "Employee details"), new("trips", "Trip-by-trip table"), new("summary", "Pay summary"), new("words", "Total in words") };
            AddCommon(o);
            o.Columns = new List<PrintChoice> { new("no", "No."), new("dispatch", "Dispatch #"), new("date", "Date"), new("client", "Client"), new("route", "Route", false), new("crates", "Crates"), new("base", "Daily wage"), new("allow", "Meal allowance"), new("net", "Total") };
            o.Signatures = new List<PrintSig> { new PrintSig("Prepared by", CurrentUserName, "Office Staff", true), new PrintSig("Approved by", OwnerName, "Owner"), new PrintSig("Received by", crew.FullName, crew.Role, true) };
            var def = new PrintFormDef { Key = o.FormKey, Name = "Crew trip pay record", FileName = $"PaySlip_{Regex.Replace(crew.FullName, "[^A-Za-z0-9]+", "_")}", Defaults = o };
            def.Body = opt =>
            {
                var sb = new StringBuilder();
                if (opt.On("employee")) sb.Append(FieldGrid(opt, 2, "name", "code", "role", "period", "rate", "meal", "contact", "license", "issued", "pay_method"));
                int n = 0;
                if (opt.On("trips"))
                {
                    sb.Append(SectionTitle($"Trips ({pay.Count})"));
                    sb.Append(Table(opt, new[] { ("no", "No.", "center"), ("dispatch", "Dispatch #", ""), ("date", "Date", ""), ("client", "Client", ""), ("route", "Route", ""), ("crates", "Crates", "num"), ("base", "Daily wage", "num"), ("allow", "Meal allowance", "num"), ("net", "Total", "num") },
                        pay.Select(p => Row(("no", ++n), ("dispatch", p.Trip.DispatchNumber), ("date", p.Trip.DispatchDate.ToString("MM/dd/yyyy")), ("client", p.Trip.ClientName), ("route", p.Trip.CleanRouteDisplay), ("crates", p.Trip.TotalCrates.ToString("N0")),
                            ("base", p.Pay.BasePay.ToString("N2")), ("allow", p.Pay.Allowance.ToString("N2")), ("net", $"<b>{p.Pay.NetPay:N2}</b>"))),
                        new Dictionary<string, string> { ["dispatch"] = "Totals", ["crates"] = pay.Sum(p => p.Trip.TotalCrates).ToString("N0"), ["base"] = basePay.ToString("N2"), ["allow"] = allow.ToString("N2"), ["net"] = net.ToString("N2") }));
                }
                if (opt.On("summary"))
                {
                    var lines = new List<(string, string)> { ("Completed trips", pay.Count.ToString()), ("Base pay", Money(basePay)), ("Meal / allowances", Money(allow)) };
                    lines.Add(("Total recorded", Money(net)));
                    sb.Append(Summary(lines, "Pay summary"));
                }
                if (opt.On("words")) sb.Append($"<div style='text-align:right;margin-top:4px;font-style:italic'>{Esc(AmountInWords(net))}</div>");
                return sb.ToString();
            };
            return def;
        }
        // =====================================================================================================
        // Crew profile & trip history
        // =====================================================================================================
        public static PrintFormDef CrewDossier(Crew crew, List<Delivery> history)
        {
            var list = history.OrderByDescending(h => h.DispatchDate).ToList();
            var o = NewOptions("CrewDossier", "Personnel Profile & Trip History", $"{crew.FullName} · {crew.CrewCode}");
            o.Fields = new List<PrintField>
            {
                new PrintField("name", "Full name", crew.FullName), new PrintField("code", "Crew ID", crew.CrewCode),
                new PrintField("role", "Role", crew.Role), new PrintField("status", "Status", crew.AvailabilityStatus),
                new PrintField("contact", "Contact no.", crew.ContactNumber), new PrintField("license", "Driver's license", crew.LicenseDisplay),
                new PrintField("employment", "Employment", crew.IsSeasonal ? "Seasonal / temporary" : "Regular"), new PrintField("rate", "Pay per trip", Money(crew.DailyWage)),
                new PrintField("meal", "Meal allowance", Money(crew.FoodAllowance), true, false)
            };
            o.Sections = new List<PrintChoice> { new("profile", "Profile details"), new("stats", "Summary boxes"), new("history", "Trip history table") };
            AddCommon(o);
            o.Columns = new List<PrintChoice> { new("dispatch", "Dispatch #"), new("date", "Date"), new("time", "Time"), new("client", "Client"), new("route", "Route"), new("truck", "Truck"), new("crates", "Crates"), new("status", "Status") };
            o.Signatures = new List<PrintSig> { new PrintSig("Personnel", crew.FullName, crew.Role, true), new PrintSig("Verified by", OwnerName, "Owner") };
            var def = new PrintFormDef { Key = o.FormKey, Name = "Crew profile & history", FileName = $"Crew_{Regex.Replace(crew.FullName, "[^A-Za-z0-9]+", "_")}", Defaults = o };
            def.Body = opt =>
            {
                var sb = new StringBuilder();
                if (opt.On("profile")) sb.Append(FieldGrid(opt, 2, "name", "code", "role", "status", "contact", "license", "employment", "rate", "meal"));
                if (opt.On("stats"))
                {
                    var done = list.Where(h => DispatchGate.IsCompleted(h.Status)).ToList();
                    sb.Append(Stats(new[] { ("Trips assigned", list.Count.ToString("N0")), ("Completed", done.Count.ToString("N0")), ("Last trip", list.Count > 0 ? list[0].DispatchDate.ToString("MMM d, yyyy") : "—") }));
                }
                if (opt.On("history"))
                {
                    sb.Append(SectionTitle("Trip history"));
                    sb.Append(Table(opt, new[] { ("dispatch", "Dispatch #", ""), ("date", "Date", ""), ("time", "Time", ""), ("client", "Client", ""), ("route", "Route", ""), ("truck", "Truck", ""), ("crates", "Crates", "num"), ("status", "Status", "") },
                        list.Select(h => Row(("dispatch", h.DispatchNumber), ("date", h.DispatchDate.ToString("MM/dd/yyyy")), ("time", h.DepartureTime), ("client", h.ClientName), ("route", h.CleanRouteDisplay), ("truck", h.TruckInfo), ("crates", h.TotalCrates.ToString("N0")), ("status", h.Status)))));
                }
                return sb.ToString();
            };
            return def;
        }
        // =====================================================================================================
        // Delivery dossier (everything about one dispatch)
        // =====================================================================================================
        public static PrintFormDef DeliveryDossier(Delivery d, IncidentReport? incident, TripReport? trip, List<DispatchStop>? stops)
        {
            stops ??= new List<DispatchStop>();
            Invoice? inv = null;
            // The live invoice (not a cancelled one), also when an old statement's invoice bills the trip.
            OrderChain chain = new OrderChain();
            try { chain = MJdatabase.GetOrderChainForTrip(d.Id); if (chain.Invoice != null) inv = MJdatabase.GetInvoices().FirstOrDefault(i => i.Id == chain.Invoice.InvoiceId); } catch (System.Exception swx3) { ErrLog.Swallowed(swx3); }
            var o = NewOptions("DeliveryDossier", "Delivery Dossier", $"Dispatch {d.DispatchNumber} · {d.Status}");
            o.Fields = new List<PrintField>
            {
                new PrintField("dispatch_no", "Dispatch #", d.DispatchNumber), new PrintField("status", "Status", d.Status),
                new PrintField("client", "Client", d.ClientName), new PrintField("date", "Dispatch date", $"{D(d.DispatchDate)} · {d.DepartureTime}"),
                new PrintField("route", "Route / destination", d.CleanRouteDisplay), new PrintField("schedule", "Loading order", d.LoadingScheduleInfo),
                new PrintField("cargo", "Crate type", d.CargoType), new PrintField("priority", "Season", d.IsPeak ? "Peak season" : "Regular"),
                new PrintField("truck", "Truck", d.TruckInfo), new PrintField("driver", "Driver", d.DriverName),
                new PrintField("aides", "Aides / helpers", string.IsNullOrWhiteSpace(d.AideDisplay) ? "None" : d.AideDisplay),
                new PrintField("remarks", "Order remarks", d.Remarks, true, !string.IsNullOrWhiteSpace(d.Remarks), true)
            };
            o.Sections = new List<PrintChoice>
            {
                new("stats", "Crate summary boxes"), new("order", "Order details"), new("crew", "Truck & crew"), new("stops", "Stores / stops table"),
                new("trip", "Trip report"), new("incident", "Incident"), new("billing", "Billing")
            };
            AddCommon(o);
            o.Columns = new List<PrintChoice> { new("seq", "#"), new("store", "Store"), new("store_address", "Store address", false), new("planned", "Planned"), new("delivered", "Delivered"), new("returned", "Returned"), new("time", "Arrival / departure"), new("received", "Received by"), new("status", "Status") };
            o.Signatures = new List<PrintSig> { new PrintSig("Dispatched by", CurrentUserName, "Dispatcher", true), new PrintSig("Driver", d.DriverName, "Driver", true), new PrintSig("Verified by", OwnerName, "Owner") };
            var def = new PrintFormDef { Key = o.FormKey, Name = "Delivery dossier", FileName = $"Dossier_{d.DispatchNumber}", Defaults = o };
            def.Body = opt =>
            {
                var sb = new StringBuilder();
                int deliveredTot = trip?.DeliveredCrates ?? stops.Sum(s => s.DeliveredCrates);
                int returnedTot = trip?.ReturnedCrates ?? stops.Sum(s => s.ReturnedCrates);
                if (opt.On("stats")) sb.Append(Stats(new[] { ("Crates loaded", d.TotalCrates.ToString("N0")), ("G-crates", d.GCrate.ToString("N0")), ("E-crates", d.ECrate.ToString("N0")), ("Delivered", deliveredTot.ToString("N0")), ("Returned", returnedTot.ToString("N0")) }));
                if (opt.On("order")) { sb.Append(SectionTitle("Order")); sb.Append(FieldGrid(opt, 2, "dispatch_no", "status", "client", "date", "route", "schedule", "cargo", "priority", "remarks")); }
                if (opt.On("crew")) { sb.Append(SectionTitle("Truck & crew")); sb.Append(FieldGrid(opt, 2, "truck", "driver", "aides")); }
                if (opt.On("stops") && stops.Count > 0)
                {
                    sb.Append(SectionTitle($"Stores ({stops.Count})"));
                    sb.Append(Table(opt, new[] { ("seq", "#", "center"), ("store", "Store", ""), ("store_address", "Store address", ""), ("planned", "Planned", "num"), ("delivered", "Delivered", "num"), ("returned", "Returned", "num"), ("time", "Arrival / departure", ""), ("received", "Received by", ""), ("status", "Status", "") },
                        stops.OrderBy(s => s.StopSequence).Select(s => Row(("seq", s.StopSequence), ("store", $"<b>{Esc((s.StoreCode + " " + s.Destination).Trim())}</b>{(DrList.Count(s.DrNumbers) == 0 ? "" : "<br/>" + DrList.Html(s.DrNumbers, "DR# "))}"), ("store_address", Esc(StoreAddressBook.Get(d.ClientName, s.StoreCode, s.Destination))),
                            ("planned", $"{s.TotalCrates} (G {s.GCrate} / E {s.ECrate})"), ("delivered", $"{s.DeliveredCrates}"), ("returned", $"{s.ReturnedCrates}"), ("time", $"{s.ArrivalTime} – {s.DepartureTime}"), ("received", s.StoreManagerName), ("status", s.Status)))));
                }
                if (opt.On("trip"))
                {
                    sb.Append(SectionTitle("Trip report"));
                    sb.Append(trip == null ? "<p class='empty'>No trip report filed yet.</p>"
                        : $"<table class='kv'><tr><td class='k'>Report #</td><td class='v'>{Esc(trip.TripReportNumber)}</td><td class='k'>Report date</td><td class='v'>{D(trip.ReportDate)}</td></tr><tr><td class='k'>Store arrival</td><td class='v'>{Esc(trip.ArrivalTime)}</td><td class='k'>Store departure</td><td class='v'>{Esc(trip.DepartureTime)}</td></tr><tr><td class='k'>Received by</td><td class='v'>{Esc(trip.StoreManagerName)}</td><td class='k'>Delivered / returned</td><td class='v'>{trip.DeliveredCrates} / {trip.ReturnedCrates}</td></tr><tr><td class='k'>Remarks</td><td class='v' colspan='3'>{Multi(trip.Remarks)}</td></tr></table>");
                }
                if (opt.On("incident"))
                {
                    sb.Append(SectionTitle("Incident"));
                    sb.Append(incident == null ? "<p class='empty'>No incident recorded.</p>"
                        : $"<table class='kv'><tr><td class='k'>Date</td><td class='v'>{D(incident.IncidentDate)}</td><td class='k'>Status</td><td class='v'>{Esc(incident.Status)}</td></tr><tr><td class='k'>Damaged / missing</td><td class='v'><b>{incident.DamagedQuantity} crates</b></td><td class='k'>Estimated cost</td><td class='v'><b>{Money(incident.EstimatedCost)}</b></td></tr><tr><td class='k'>Cause</td><td class='v'>{Esc(incident.Cause)}</td><td class='k'>Crew liability</td><td class='v'>{(incident.ChargeableToCrew ? "Chargeable to crew" : "Company")}</td></tr><tr><td class='k'>Details</td><td class='v' colspan='3'>{Multi(incident.Description)}</td></tr></table>");
                }
                if (opt.On("billing"))
                {
                    sb.Append(SectionTitle("Billing"));
                    sb.Append(inv == null ? "<p class='empty'>Not billed yet.</p>"
                        : $"<table class='kv'><tr><td class='k'>Invoice #</td><td class='v'>{Esc(inv.InvoiceNumber)}</td><td class='k'>Status</td><td class='v'>{Esc(inv.Status)}</td></tr><tr><td class='k'>Net amount</td><td class='v'><b>{Money(inv.NetAmount)}</b></td><td class='k'>Due</td><td class='v'>{D(inv.DueDate)}</td></tr><tr><td class='k'>Amount</td><td class='v'>{Money(inv.Freight)}</td><td class='k'>Fuel surcharge</td><td class='v'>{Money(inv.FuelSurcharge)}</td></tr><tr><td class='k'>Invoice total</td><td class='v'><b>{Money(InvoiceTotal.Of(inv.Freight, inv.FuelSurcharge))}</b></td><td class='k'></td><td class='v'></td></tr><tr><td class='k'>Statement</td><td class='v'>{Esc(chain.SoaNumber.Length > 0 ? chain.SoaNumber : "—")}</td><td class='k'>Stage</td><td class='v'>{Esc(chain.Stage)}</td></tr><tr><td class='k'>Collection receipt</td><td class='v'>{Esc(chain.ReceiptNo.Length > 0 ? chain.ReceiptNo : "—")}</td><td class='k'>Paid on</td><td class='v'>{(chain.PaidOn.HasValue ? D(chain.PaidOn.Value) : "—")}</td></tr>{(chain.ReceiptNo.Length > 0 ? $"<tr><td class='k'>Payment method</td><td class='v'>{Esc(chain.PayMethod)}</td><td class='k'>Reference</td><td class='v'>{Esc(chain.PayReference.Length > 0 ? chain.PayReference : "—")}</td></tr>" : "")}</table>");
                }
                return sb.ToString();
            };
            return def;
        }
        // =====================================================================================================
        // Loading schedule sheet (one route per block, stores listed under it)
        // =====================================================================================================
        private sealed class LoadGroup
        {
            public string Route = "", Departure = "", Truck = "", Crew = "", Client = "", Order = "";
            public int Capacity = 360;
            public List<(string Code, string Name, int E, int G)> Stores = new();
        }
        public static PrintFormDef LoadingSheetFromDeliveries(List<Delivery> deliveries)
        {
            var trucks = MJdatabase.GetTrucks();
            var groups = new List<LoadGroup>();
            foreach (var del in deliveries)
            {
                var route = MJdatabase.FindRouteByNameOrCode(del.Destination) ?? MJdatabase.FindRouteByNameOrCode(del.LoadingScheduleInfo);
                var trk = trucks.FirstOrDefault(t => t.Id == del.TruckId);
                var g = new LoadGroup
                {
                    Route = route != null ? route.RouteName.Split('(')[0].Trim() : del.CleanRouteDisplay.Split('(')[0].Trim(),
                    Departure = del.DepartureTime,
                    Capacity = trk != null && trk.CapacityCrates > 0 ? trk.CapacityCrates : 360,
                    Truck = trk != null ? $"{trk.PlateNumber} · {trk.TruckType}" : del.TruckInfo,
                    Crew = Crew(del), Client = del.ClientName, Order = del.DispatchNumber
                };
                foreach (var s in MJdatabase.GetOrBuildTripStopsForDelivery(del)) g.Stores.Add((s.StoreCode, s.Destination, s.ECrate, s.GCrate));
                groups.Add(g);
            }
            return LoadingSheet(groups, deliveries.Count > 0 ? deliveries.Min(d => d.DispatchDate) : DateTime.Today);
        }
        public static PrintFormDef LoadingSheetFromSchedules(List<LoadingSchedule> schedules)
        {
            var dels = MJdatabase.GetDeliveries();
            var trucks = MJdatabase.GetTrucks();
            var groups = new List<LoadGroup>();
            foreach (var ls in schedules)
            {
                var del = dels.FirstOrDefault(d => d.LoadingScheduleId == ls.Id && !DispatchGate.IsCancelled(d.Status));
                var route = MJdatabase.FindRouteByNameOrCode(ls.Destination);
                var stops = MJdatabase.GetLoadingScheduleStops(ls.Id);
                if (stops.Count == 0 && del != null) stops = MJdatabase.GetOrBuildTripStopsForDelivery(del);
                var g = new LoadGroup { Route = route != null ? route.RouteName.Split('(')[0].Trim() : ls.Destination.Split(':')[0].Trim(), Departure = ls.DepartureTime, Client = ls.ClientName, Order = ls.ScheduleNumber };
                if (stops.Count > 0) foreach (var s in stops) g.Stores.Add((s.StoreCode, s.Destination, s.ECrate, s.GCrate));
                else if (route != null && route.PlacesList.Count > 0)
                {
                    int cnt = route.PlacesList.Count;
                    for (int i = 0; i < cnt; i++)
                    {
                        var (sc, sn) = DeliveryRoute.SplitStoreCodeAndName(route.PlacesList[i]);
                        g.Stores.Add((sc, sn, ls.ECrate / cnt + (i < ls.ECrate % cnt ? 1 : 0), ls.GCrate / cnt + (i < ls.GCrate % cnt ? 1 : 0)));
                    }
                }
                else { var (sc, sn) = DeliveryRoute.SplitStoreCodeAndName(ls.Destination); g.Stores.Add((sc, sn, ls.ECrate, ls.GCrate)); }
                if (del != null)
                {
                    var trk = trucks.FirstOrDefault(t => t.Id == del.TruckId);
                    g.Capacity = trk != null && trk.CapacityCrates > 0 ? trk.CapacityCrates : 360;
                    g.Truck = trk != null ? $"{trk.PlateNumber} · {trk.TruckType}" : del.TruckInfo;
                    g.Crew = Crew(del);
                }
                else
                {
                    int tot = g.Stores.Sum(s => s.E + s.G);
                    g.Capacity = tot > 360 ? 540 : 360;
                    g.Truck = "To be assigned";
                    g.Crew = "To be assigned";
                }
                groups.Add(g);
            }
            return LoadingSheet(groups, schedules.Count > 0 ? schedules.Min(s => s.ScheduleDate) : DateTime.Today);
        }
        private static PrintFormDef LoadingSheet(List<LoadGroup> groups, DateTime date)
        {
            int gE = groups.Sum(g => g.Stores.Sum(s => s.E)), gG = groups.Sum(g => g.Stores.Sum(s => s.G));
            var o = NewOptions("LoadingSheet", "Loading Schedule", D(date));
            o.Orientation = "Landscape";
            o.Fields = new List<PrintField>
            {
                new PrintField("date", "Loading date", date.ToString("dddd, MMMM d, yyyy")),
                new PrintField("routes", "Routes", groups.Count.ToString()),
                new PrintField("crates", "Total crates", $"{gE + gG:N0} (E {gE:N0} · G {gG:N0})"),
                new PrintField("warehouse", "Warehouse / plant", "", false, false)
            };
            o.Sections = new List<PrintChoice> { new("info", "Date & totals line"), new("subtotals", "Route subtotal rows"), new("grand", "Grand total row") };
            AddCommon(o);
            o.Columns = new List<PrintChoice> { new("route", "Route"), new("order", "Order / dispatch #", false), new("client", "Client", false), new("code", "Store code"), new("store", "Store name"), new("store_address", "Store address", false), new("e", "E-crates"), new("g", "G-crates"), new("total", "Total"), new("volume", "Route volume"), new("ratio", "Truck load %"), new("departure", "Departure"), new("truck", "Truck"), new("crew", "Driver / helper") };
            o.Signatures = new List<PrintSig> { new PrintSig("Prepared by", CurrentUserName, "Dispatcher", true), new PrintSig("Checked by", "", "Warehouse / checker"), new PrintSig("Approved by", OwnerName, "Owner") };
            var def = new PrintFormDef { Key = o.FormKey, Name = "Loading schedule", FileName = $"LoadingSchedule_{date:yyyyMMdd}", Defaults = o };
            def.Body = opt =>
            {
                var sb = new StringBuilder();
                if (opt.On("info")) sb.Append(FieldGrid(opt, 2, "date", "routes", "crates", "warehouse"));
                var cols = new (string Key, string Header, string Css, bool PerRoute)[]
                {
                    ("route", "Route", "center", true), ("order", "Order #", "center", true), ("client", "Client", "", true), ("code", "Store code", "center", false), ("store", "Store name", "", false), ("store_address", "Store address", "", false),
                    ("e", "E-crates", "num", false), ("g", "G-crates", "num", false), ("total", "Total", "num", false), ("volume", "Route volume", "center", true), ("ratio", "Load %", "center", true),
                    ("departure", "Departure", "center", true), ("truck", "Truck", "center", true), ("crew", "Driver / helper", "", true)
                }.Where(c => opt.Col(c.Key)).ToList();
                sb.Append("<table class='grid'><thead><tr>");
                foreach (var c in cols) sb.Append($"<th class='{c.Css}'>{Esc(c.Header)}</th>");
                sb.Append("</tr></thead><tbody>");
                foreach (var g in groups)
                {
                    int rE = g.Stores.Sum(s => s.E), rG = g.Stores.Sum(s => s.G), rT = rE + rG;
                    int span = Math.Max(1, g.Stores.Count);
                    var stores = g.Stores.Count > 0 ? g.Stores : new List<(string, string, int, int)> { ("", "", 0, 0) };
                    for (int i = 0; i < stores.Count; i++)
                    {
                        var s = stores[i];
                        sb.Append("<tr>");
                        foreach (var c in cols)
                        {
                            if (c.PerRoute && i > 0) continue;
                            string rs = c.PerRoute && span > 1 ? $" rowspan='{span}'" : "";
                            string v = c.Key switch
                            {
                                "route" => $"<b>{Esc(g.Route)}</b>", "order" => Esc(g.Order), "client" => Esc(g.Client), "code" => $"<b>{Esc(s.Item1)}</b>", "store" => Esc(s.Item2), "store_address" => Esc(StoreAddressBook.Get(g.Client, s.Item1, s.Item2)),
                                "e" => s.Item3.ToString(), "g" => s.Item4.ToString(), "total" => $"<b>{s.Item3 + s.Item4}</b>", "volume" => $"<b>{rT}</b>",
                                "ratio" => g.Capacity > 0 ? $"{Math.Round(rT * 100.0 / g.Capacity):0}%" : "", "departure" => $"<b>{Esc(g.Departure)}</b>", "truck" => Esc(g.Truck), "crew" => Esc(g.Crew), _ => ""
                            };
                            sb.Append($"<td class='{c.Css}'{rs}>{v}</td>");
                        }
                        sb.Append("</tr>");
                    }
                    if (opt.On("subtotals"))
                        sb.Append("<tr style='font-weight:800'>" + string.Concat(cols.Select(c => $"<td class='{c.Css}' style='border-top:1.5px solid #000;background:#e6e6e6'>{(c.Key == "e" ? rE.ToString() : c.Key == "g" ? rG.ToString() : c.Key == "total" ? rT.ToString() : c.Key == "store" ? "Route subtotal" : "")}</td>")) + "</tr>");
                }
                sb.Append("</tbody>");
                if (opt.On("grand"))
                    sb.Append("<tfoot><tr>" + string.Concat(cols.Select(c => $"<td class='{c.Css}'>{(c.Key == "e" ? gE.ToString("N0") : c.Key == "g" ? gG.ToString("N0") : c.Key == "total" ? (gE + gG).ToString("N0") : c.Key == "store" ? "GRAND TOTAL" : "")}</td>")) + "</tr></tfoot>");
                sb.Append("</table>");
                return sb.ToString();
            };
            return def;
        }
        // =====================================================================================================
        // Trip report (the form the driver brings to each store)
        // =====================================================================================================
        public static PrintFormDef TripReport(Delivery d, TripReport? trip, List<DispatchStop> stops)
        {
            var trk = MJdatabase.GetTrucks().FirstOrDefault(t => t.Id == d.TruckId);
            string plate = trk?.PlateNumber ?? d.TruckInfo;
            var route = MJdatabase.FindRouteByNameOrCode(d.Destination) ?? MJdatabase.FindRouteByNameOrCode(d.LoadingScheduleInfo);
            string routeTxt = route != null ? $"{route.RouteCode} - {route.RouteName}" : d.CleanRouteDisplay;
            string aides = !string.IsNullOrWhiteSpace(d.AideNames) ? d.AideNames : d.AideName;
            var o = NewOptions("TripReport", "Trip Report", $"Dispatch {d.DispatchNumber}");
            o.Orientation = "Landscape";
            o.HasBlankRows = true;
            o.BlankRows = 8;
            o.Fields = new List<PrintField>
            {
                new PrintField("ref", "Ref.", trip?.TripReportNumber ?? $"{d.DispatchNumber}"), new PrintField("date", "Delivery date", d.DispatchDate.ToString("MM/dd/yyyy")),
                new PrintField("client", "Client", d.ClientName), new PrintField("route", "Route", routeTxt),
                new PrintField("driver", "Driver's name", d.DriverName), new PrintField("helper", "Helper's name", aides),
                new PrintField("hauler", "Hauler", $"MJB ({plate})"), new PrintField("plate", "Plate no.", plate),
                new PrintField("seal", "Seal no.", d.DispatchNumber), new PrintField("etd", "Std ETD", d.DepartureTime),
                new PrintField("time_in", "Truck time in", trip?.TruckTimeIn ?? ""), new PrintField("time_out", "Truck time out", trip?.TruckTimeOut ?? ""),
                new PrintField("dtr_approved", "DTR approved by", trip?.DtrApprovedBy ?? ""), new PrintField("return_checked", "Return checked by", trip?.ReturnCheckedBy ?? ""),
                new PrintField("dtr_prepared", "DTR prepared by", trip?.DtrPreparedBy ?? ""), new PrintField("dr_prepared", "DR prepared by", trip?.DrPreparedBy ?? ""),
                new PrintField("dr_checked", "DR checked by", trip?.DrCheckedBy ?? ""), new PrintField("guard", "Guard name & sign", trip?.GuardName ?? ""),
                new PrintField("remarks", "Trip remarks", trip?.Remarks ?? "", true, true, true)
            };
            o.Sections = new List<PrintChoice> { new("head", "Trip details (driver, route, plate…)"), new("stores", "Stores table"), new("totals", "Totals row"), new("signoff", "Plant gate & sign-off box"), new("remarks_box", "Trip remarks") };
            AddCommon(o);
            o.Sections.First(s => s.Key == "printed").On = false;
            o.Columns = new List<PrintChoice> { new("no", "No."), new("code", "Store code"), new("store", "Store name & DR"), new("store_address", "Store address", false), new("in", "Store in"), new("out", "Store out"), new("rep", "Store representative"), new("le", "Loaded E-crates"), new("lg", "Loaded G-crates"), new("re", "Returned E-crates"), new("rg", "Returned G-crates"), new("remarks", "No. Drops") };
            o.Signatures = new List<PrintSig> { new PrintSig("Driver", d.DriverName, "Signature over printed name", true, false), new PrintSig("Checked by", "", "Plant checker", false, false) };
            var def = new PrintFormDef { Key = o.FormKey, Name = "Trip report", FileName = $"TripReport_{d.DispatchNumber}", Defaults = o };
            def.Body = opt =>
            {
                var sb = new StringBuilder();
                if (opt.On("head")) sb.Append(FieldGrid(opt, 3, "ref", "driver", "hauler", "date", "helper", "route", "client", "plate", "seal", "etd"));
                if (opt.On("stores"))
                {
                    int n = 0;
                    int Le(DispatchStop s) => s.DeliveredECrates > 0 || s.DeliveredGCrates > 0 ? s.DeliveredECrates : s.ECrate;
                    int Lg(DispatchStop s) => s.DeliveredECrates > 0 || s.DeliveredGCrates > 0 ? s.DeliveredGCrates : s.GCrate;
                    var rows = stops.Select(s => Row(("no", ++n), ("code", $"<b>{Esc(s.StoreCode)}</b>"), ("store", $"<b>{Esc(s.Destination)}</b>{(DrList.Count(s.DrNumbers) == 0 ? "" : "<br/>" + DrList.Html(s.DrNumbers, "DR# "))}"),
                        ("store_address", Esc(StoreAddressBook.Get(d.ClientName, s.StoreCode, s.Destination))), ("in", s.ArrivalTime), ("out", s.DepartureTime), ("rep", s.StoreManagerName), ("le", Le(s)), ("lg", Lg(s)),
                        ("re", s.ReturnedECrates), ("rg", s.ReturnedGCrates), ("remarks", s.Remarks))).ToList();
                    var totals = opt.On("totals") ? new Dictionary<string, string> { ["store"] = "TOTAL", ["le"] = stops.Sum(Le).ToString(), ["lg"] = stops.Sum(Lg).ToString(), ["re"] = stops.Sum(s => s.ReturnedECrates).ToString(), ["rg"] = stops.Sum(s => s.ReturnedGCrates).ToString() } : null;
                    sb.Append(SectionTitle("Stores"));
                    sb.Append(Table(opt, new[] { ("no", "No", "center"), ("code", "Store code", "center"), ("store", "Store name & DR / invoice #", ""), ("store_address", "Store address", ""), ("in", "Store in", "center"), ("out", "Store out", "center"), ("rep", "Store representative (sign over printed name)", ""),
                        ("le", "Loaded E", "center"), ("lg", "Loaded G", "center"), ("re", "Returned E", "center"), ("rg", "Returned G", "center"), ("remarks", "No. Drops", "center") },
                        rows, totals, Math.Max(0, opt.BlankRows - stops.Count)));
                    if (opt.On("totals"))
                    {
                        // Nov 10: the crate totals at the bottom of the stores table: total loaded, total returned and the total crates.
                        int tle = stops.Sum(Le), tlg = stops.Sum(Lg), tre = stops.Sum(s => s.ReturnedECrates), trg = stops.Sum(s => s.ReturnedGCrates);
                        string Cr(string label, int e, int g, bool strong = false) => $"<tr><td class='lbl{(strong ? " strong" : "")}' style='text-align:right'>{label}</td><td class='center'>{e:N0}</td><td class='center'>{g:N0}</td><td class='center'><b>{e + g:N0}</b></td></tr>";
                        sb.Append("<table class='grid sheet' style='width:55%;margin:6px 0 0 auto'><thead><tr><th></th><th class='center'>E crates</th><th class='center'>G crates</th><th class='center'>Total</th></tr></thead><tbody>"
                            + Cr("TOTAL LOADED", tle, tlg) + Cr("TOTAL RETURNED", tre, trg) + Cr("TOTAL CRATES (loaded + returned)", tle + tre, tlg + trg, true) + "</tbody></table>");
                    }
                }
                if (opt.On("remarks_box") && !string.IsNullOrWhiteSpace(opt.F("remarks"))) sb.Append(FieldGrid(opt, 1, "remarks"));
                if (opt.On("signoff")) { sb.Append(SectionTitle("Plant gate & sign-off")); sb.Append(FieldGrid(opt, 4, "time_in", "time_out", "dtr_approved", "return_checked", "dtr_prepared", "dr_prepared", "dr_checked", "guard")); }
                return sb.ToString();
            };
            def.ExtraCss = ".sheet table.kv td.v { min-width:90px; }";
            return def;
        }
    }
}
