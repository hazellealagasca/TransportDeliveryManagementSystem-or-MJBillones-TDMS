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
                    catch { }
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
            var inv = MJdatabase.GetInvoices().Find(i => i.Id == cr.InvoiceId || (!string.IsNullOrEmpty(cr.InvoiceNumber) && i.InvoiceNumber.Equals(cr.InvoiceNumber, StringComparison.OrdinalIgnoreCase)));
            var client = inv == null ? null : MJdatabase.GetClients().FirstOrDefault(c => c.CompanyName.Equals(inv.ClientName, StringComparison.OrdinalIgnoreCase));
            decimal gross = inv?.TotalAmount ?? cr.AmountPaid, ded = inv?.Deductions ?? 0, net = inv?.NetAmount ?? cr.AmountPaid;
            decimal collected = inv != null ? MJdatabase.GetTotalCollectedForInvoice(inv.Id) : cr.AmountPaid;
            decimal balance = inv != null ? Math.Max(0, inv.NetAmount - collected) : 0;
            string method = string.IsNullOrWhiteSpace(cr.PaymentMethod) ? "Cash" : cr.PaymentMethod;
            var o = NewOptions("OfficialReceipt", "Collection Receipt", $"No. {cr.ReceiptNumber}");
            o.CopyLabel = "Original";
            o.Fields = new List<PrintField>
            {
                new PrintField("receipt_no", "Receipt no.", cr.ReceiptNumber),
                new PrintField("date", "Date", D(cr.PaymentDate)),
                new PrintField("received_from", "Received from", inv?.ClientName ?? ""),
                new PrintField("address", "Address", client?.Address ?? ""),
                new PrintField("client_tin", "TIN", client?.TIN ?? ""),
                new PrintField("for", "In payment of", inv != null ? $"Service Invoice {inv.InvoiceNumber}{(string.IsNullOrWhiteSpace(inv.DispatchNumber) ? "" : " · dispatch " + inv.DispatchNumber)}" : "Hauling & delivery services"),
                new PrintField("amount", "Amount", Money(cr.AmountPaid)),
                new PrintField("amount_words", "Amount in words", AmountInWords(cr.AmountPaid)),
                new PrintField("method", "Payment method", method),
                new PrintField("reference", "Reference / check no.", cr.ReferenceNumber),
                new PrintField("remarks", "Remarks", cr.Notes, true, true, true),
                new PrintField("atp", "BIR / printer details", "BIR Authority to Print OCN: 17AAU20240000004957", false, true, true)
            };
            o.Sections = new List<PrintChoice>
            {
                new PrintChoice("method_boxes", "Payment method tick boxes"),
                new PrintChoice("breakdown", "Invoice breakdown table"),
                new PrintChoice("balance", "Remaining balance"),
            };
            AddCommon(o);
            o.Columns = new List<PrintChoice> { new("inv", "Invoice #"), new("gross", "Gross"), new("ded", "Deductions"), new("net", "Net due"), new("paid", "Amount paid"), new("bal", "Balance") };
            o.Signatures = new List<PrintSig> { new PrintSig("Received by", CurrentUserName, "Cashier / Collector", true), new PrintSig("Paid by", "", "Client representative", false) };
            var def = new PrintFormDef { Key = o.FormKey, Name = "Collection receipt", FileName = $"Receipt_{cr.ReceiptNumber}", Defaults = o };
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
                    sb.Append(SectionTitle("Applied to"));
                    sb.Append(Table(opt, new[] { ("inv", "Invoice #", ""), ("gross", "Gross", "num"), ("ded", "Deductions", "num"), ("net", "Net due", "num"), ("paid", "Amount paid", "num"), ("bal", "Balance", "num") },
                        new[] { Row(("inv", inv?.InvoiceNumber ?? cr.InvoiceNumber), ("gross", Money(gross)), ("ded", ded > 0 ? "- " + Money(ded) : Money(0)), ("net", Money(net)), ("paid", $"<b>{Esc(Money(cr.AmountPaid))}</b>"), ("bal", balance > 0 ? Money(balance) : "Fully paid")) }));
                }
                if (opt.On("balance") && inv != null) sb.Append(Summary(new[] { ("Net invoice amount", Money(net)), ("Total paid to date", Money(collected)), ("Remaining balance", Money(balance)) }));
                sb.Append(FieldGrid(opt, 1, "remarks"));
                if (opt.FOn("atp") && !string.IsNullOrWhiteSpace(opt.F("atp"))) sb.Append($"<div style='margin-top:10px;font-size:.78em;text-align:center;color:#333'>{Multi(opt.F("atp"))}</div>");
                return sb.ToString();
            };
            return def;
        }
        // =====================================================================================================
        // Statement of account (trip lines)
        // =====================================================================================================
        public static PrintFormDef StatementOfAccount(StatementOfAccount soa, List<StatementOfAccountItem> items)
        {
            var groups = items.GroupBy(i => i.TripNumber).OrderBy(g => g.Key).Select(g => g.ToList()).ToList();
            decimal amountSum = groups.Sum(g => g[0].Amount), fuelSum = groups.Sum(g => g[0].FuelSurcharge);
            decimal totalSales = soa.TotalSalesVatInc > 0 ? soa.TotalSalesVatInc : amountSum + fuelSum;
            decimal vat = soa.VatAmount > 0 ? soa.VatAmount : Math.Round(totalSales / 1.12m * 0.12m, 2);
            decimal netOfVat = soa.NetOfVat > 0 ? soa.NetOfVat : Math.Round(totalSales / 1.12m, 2);
            decimal fuel = soa.FuelSurchargeTotal > 0 ? soa.FuelSurchargeTotal : fuelSum;
            decimal wtax = soa.WithholdingTax > 0 ? soa.WithholdingTax : Math.Round(netOfVat * 0.02m, 2);
            decimal due = soa.TotalAmount > 0 ? soa.TotalAmount : Math.Round(netOfVat + vat - wtax, 2);
            var o = NewOptions("SOA", "Statement of Account", $"{soa.SoaNumber} · {D(soa.IssueDate)}");
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
            o.Columns = new List<PrintChoice> { new("no", "No."), new("date", "Date received"), new("zone", "Zone"), new("dr", "DR no."), new("dest", "Destination"), new("type", "Type"), new("plate", "Plate no."), new("tons", "Tons"), new("drops", "Drops"), new("amount", "Amount"), new("fuel", "Fuel surcharge"), new("driver", "Driver / helper") };
            o.Signatures = new List<PrintSig>
            {
                new PrintSig("Prepared by", soa.PreparedBy, "Billing Officer", true),
                new PrintSig("Received by", soa.ReceivedBy1, "Accountant", true),
                new PrintSig("Received by", soa.ReceivedBy2, "Accountant", true, !string.IsNullOrWhiteSpace(soa.ReceivedBy2)),
                new PrintSig("Approved by", OwnerName, "Owner", false, false)
            };
            var def = new PrintFormDef { Key = o.FormKey, Name = "Statement of account", FileName = $"SOA_{soa.SoaNumber}", Defaults = o };
            def.Body = opt =>
            {
                var sb = new StringBuilder();
                if (opt.On("billto")) sb.Append(FieldGrid(opt, 2, "billed_to", "soa_no", "client_address", "date", "client_tin", "period", "type", "si_no"));
                if (opt.On("trips"))
                {
                    var cols = new (string Key, string Header, string Css, bool PerTrip)[]
                    {
                        ("no", "No.", "center", true), ("date", "Date received", "center", true), ("zone", "Zone", "center", true), ("dr", "DR no.", "", false), ("dest", "Destination", "", false),
                        ("type", "Type", "center", true), ("plate", "Plate no.", "center", true), ("tons", "Tons", "center", true), ("drops", "Drops", "center", true),
                        ("amount", "Amount", "num", true), ("fuel", "Fuel surcharge", "num", true), ("driver", "Driver / helper", "", true)
                    }.Where(c => opt.Col(c.Key)).ToList();
                    sb.Append(SectionTitle("Trips"));
                    sb.Append("<table class='grid'><thead><tr>");
                    foreach (var c in cols) sb.Append($"<th class='{c.Css}'>{Esc(c.Header)}</th>");
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
                                    "no" => n.ToString(), "date" => Esc(f.DateReceived), "zone" => Esc(f.Zone), "dr" => Esc(g[i].DrNumbers), "dest" => $"<b>{Esc(g[i].Destination)}</b>",
                                    "type" => Esc(f.DeliveryType), "plate" => $"<b>{Esc(f.PlateNumber)}</b>", "tons" => Esc(f.Tonnage), "drops" => f.DropCount.ToString(),
                                    "amount" => $"<b>{f.Amount:N2}</b>", "fuel" => f.FuelSurcharge > 0 ? f.FuelSurcharge.ToString("N2") : "-", "driver" => Esc(f.DriverHelper), _ => ""
                                };
                                sb.Append($"<td class='{c.Css}'{span}>{v}</td>");
                            }
                            sb.Append("</tr>");
                        }
                    }
                    if (groups.Count == 0) sb.Append($"<tr><td colspan='{cols.Count}' class='empty'>No trips on this statement.</td></tr>");
                    sb.Append("</tbody>");
                    if (opt.On("trip_total") && cols.Count > 0)
                    {
                        sb.Append("<tfoot><tr>");
                        bool labelled = false;
                        foreach (var c in cols)
                        {
                            string v = c.Key == "amount" ? amountSum.ToString("N2") : c.Key == "fuel" ? fuelSum.ToString("N2") : "";
                            if (v.Length == 0 && !labelled && c.Key != "amount" && c.Key != "fuel") { v = $"{groups.Count} trip(s)"; labelled = true; }
                            sb.Append($"<td class='{c.Css}'>{v}</td>");
                        }
                        sb.Append("</tr></tfoot>");
                    }
                    sb.Append("</table>");
                }
                if (opt.On("taxes"))
                    sb.Append(Summary(new[]
                    {
                        ("Total sales (VAT inclusive)", totalSales.ToString("N2")), ("Less: VAT (12%)", vat.ToString("N2")), ("Amount net of VAT", netOfVat.ToString("N2")),
                        ("Fuel surcharge (included above)", fuel.ToString("N2")), ("Add: VAT", vat.ToString("N2")), ("Less: withholding tax", "- " + wtax.ToString("N2")), ("Total amount due", "₱" + due.ToString("N2"))
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
            o.Columns = new List<PrintChoice> { new("no", "No."), new("inv", "Invoice #"), new("dispatch", "Dispatch #"), new("issued", "Issued"), new("due", "Due"), new("gross", "Gross"), new("ded", "Deductions"), new("net", "Net"), new("status", "Status") };
            o.Signatures = new List<PrintSig> { new PrintSig("Prepared by", string.IsNullOrWhiteSpace(soa.PreparedBy) ? CurrentUserName : soa.PreparedBy, "Billing Officer", true), new PrintSig("Received by", soa.ReceivedBy1, "Client", true) };
            var def = new PrintFormDef { Key = o.FormKey, Name = "Statement of account (invoices)", FileName = $"SOA_{soa.SoaNumber}", Defaults = o };
            def.Body = opt =>
            {
                var sb = new StringBuilder();
                if (opt.On("billto")) sb.Append(FieldGrid(opt, 2, "billed_to", "soa_no", "period", "date", "status"));
                int n = 0;
                if (opt.On("invoices"))
                    sb.Append(Table(opt, new[] { ("no", "No.", "center"), ("inv", "Invoice #", ""), ("dispatch", "Dispatch #", ""), ("issued", "Issued", ""), ("due", "Due", ""), ("gross", "Gross", "num"), ("ded", "Deductions", "num"), ("net", "Net", "num"), ("status", "Status", "") },
                        invoices.Select(i => Row(("no", ++n), ("inv", $"<b>{Esc(i.InvoiceNumber)}</b>"), ("dispatch", i.DispatchNumber), ("issued", i.IssueDate.ToString("MM/dd/yyyy")), ("due", i.DueDate.ToString("MM/dd/yyyy")),
                            ("gross", i.TotalAmount.ToString("N2")), ("ded", i.Deductions > 0 ? "- " + i.Deductions.ToString("N2") : "0.00"), ("net", $"<b>{i.NetAmount:N2}</b>"), ("status", i.Status)))));
                if (opt.On("totals"))
                    sb.Append(Summary(new[] { ("Total gross", invoices.Sum(i => i.TotalAmount).ToString("N2")), ("Less deductions", "- " + invoices.Sum(i => i.Deductions).ToString("N2")), ("Total net payable", "₱" + invoices.Sum(i => i.NetAmount).ToString("N2")) }));
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
            try { recorded = MJdatabase.GetAllTripCrewPay(); } catch { }
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
                    sb.Append(Stats(new[] { ("Trips assigned", list.Count.ToString("N0")), ("Completed", done.Count.ToString("N0")), ("Crates hauled", done.Sum(h => h.TotalCrates).ToString("N0")), ("Last trip", list.Count > 0 ? list[0].DispatchDate.ToString("MMM d, yyyy") : "—") }));
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
            try { inv = MJdatabase.GetInvoices().FirstOrDefault(i => i.DeliveryId == d.Id); } catch { }
            List<TripCrewPay> crewPay = new();
            try { crewPay = MJdatabase.GetTripCrewPay(d.Id); } catch { }
            var o = NewOptions("DeliveryDossier", "Delivery Dossier", $"Dispatch {d.DispatchNumber} · {d.Status}");
            o.Fields = new List<PrintField>
            {
                new PrintField("dispatch_no", "Dispatch #", d.DispatchNumber), new PrintField("status", "Status", d.Status),
                new PrintField("client", "Client", d.ClientName), new PrintField("date", "Dispatch date", $"{D(d.DispatchDate)} · {d.DepartureTime}"),
                new PrintField("route", "Route / destination", d.CleanRouteDisplay), new PrintField("schedule", "Loading order", d.LoadingScheduleInfo),
                new PrintField("cargo", "Cargo", d.CargoType), new PrintField("priority", "Season", d.IsPeak ? "Peak season" : "Regular"),
                new PrintField("truck", "Truck", d.TruckInfo), new PrintField("driver", "Driver", d.DriverName),
                new PrintField("aides", "Aides / helpers", string.IsNullOrWhiteSpace(d.AideDisplay) ? "None" : d.AideDisplay),
                new PrintField("remarks", "Order remarks", d.Remarks, true, !string.IsNullOrWhiteSpace(d.Remarks), true)
            };
            o.Sections = new List<PrintChoice>
            {
                new("stats", "Crate summary boxes"), new("order", "Order details"), new("crew", "Truck & crew"), new("stops", "Stores / stops table"),
                new("trip", "Trip report"), new("incident", "Incident"), new("billing", "Billing"), new("pay", "Crew pay", false)
            };
            AddCommon(o);
            o.Columns = new List<PrintChoice> { new("seq", "#"), new("store", "Store"), new("planned", "Planned"), new("delivered", "Delivered"), new("returned", "Returned"), new("time", "Arrival / departure"), new("received", "Received by"), new("status", "Status") };
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
                    sb.Append(Table(opt, new[] { ("seq", "#", "center"), ("store", "Store", ""), ("planned", "Planned", "num"), ("delivered", "Delivered", "num"), ("returned", "Returned", "num"), ("time", "Arrival / departure", ""), ("received", "Received by", ""), ("status", "Status", "") },
                        stops.OrderBy(s => s.StopSequence).Select(s => Row(("seq", s.StopSequence), ("store", $"<b>{Esc((s.StoreCode + " " + s.Destination).Trim())}</b>{(string.IsNullOrWhiteSpace(s.DrNumbers) ? "" : "<br/>DR# " + Esc(s.DrNumbers))}"),
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
                        : $"<table class='kv'><tr><td class='k'>Invoice #</td><td class='v'>{Esc(inv.InvoiceNumber)}</td><td class='k'>Status</td><td class='v'>{Esc(inv.Status)}</td></tr><tr><td class='k'>Net amount</td><td class='v'><b>{Money(inv.NetAmount)}</b></td><td class='k'>Due</td><td class='v'>{D(inv.DueDate)}</td></tr></table>");
                }
                if (opt.On("pay") && crewPay.Count > 0)
                {
                    sb.Append(SectionTitle("Crew pay for this trip"));
                    sb.Append(Table(new PrintOptions(), new[] { ("n", "Crew", ""), ("r", "Role", ""), ("b", "Base", "num"), ("a", "Meal allowance", "num"), ("t", "Total", "num") },
                        crewPay.Select(p => Row(("n", p.CrewName), ("r", p.Role), ("b", p.BasePay.ToString("N2")), ("a", p.Allowance.ToString("N2")), ("t", $"<b>{p.NetPay:N2}</b>")))));
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
            o.Columns = new List<PrintChoice> { new("route", "Route"), new("order", "Order / dispatch #", false), new("client", "Client", false), new("code", "Store code"), new("store", "Store name"), new("e", "E-crates"), new("g", "G-crates"), new("total", "Total"), new("volume", "Route volume"), new("ratio", "Truck load %"), new("departure", "Departure"), new("truck", "Truck"), new("crew", "Driver / helper") };
            o.Signatures = new List<PrintSig> { new PrintSig("Prepared by", CurrentUserName, "Dispatcher", true), new PrintSig("Checked by", "", "Warehouse / checker"), new PrintSig("Approved by", OwnerName, "Owner") };
            var def = new PrintFormDef { Key = o.FormKey, Name = "Loading schedule", FileName = $"LoadingSchedule_{date:yyyyMMdd}", Defaults = o };
            def.Body = opt =>
            {
                var sb = new StringBuilder();
                if (opt.On("info")) sb.Append(FieldGrid(opt, 2, "date", "routes", "crates", "warehouse"));
                var cols = new (string Key, string Header, string Css, bool PerRoute)[]
                {
                    ("route", "Route", "center", true), ("order", "Order #", "center", true), ("client", "Client", "", true), ("code", "Store code", "center", false), ("store", "Store name", "", false),
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
                                "route" => $"<b>{Esc(g.Route)}</b>", "order" => Esc(g.Order), "client" => Esc(g.Client), "code" => $"<b>{Esc(s.Item1)}</b>", "store" => Esc(s.Item2),
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
            o.Columns = new List<PrintChoice> { new("no", "No."), new("code", "Store code"), new("store", "Store name & DR"), new("in", "Store in"), new("out", "Store out"), new("rep", "Store representative"), new("le", "Loaded E-crates"), new("lg", "Loaded G-crates"), new("box", "Box"), new("pf", "P.Foods"), new("mi", "M.I."), new("re", "Returned E-crates"), new("rg", "Returned G-crates"), new("remarks", "Remarks") };
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
                    var rows = stops.Select(s => Row(("no", ++n), ("code", $"<b>{Esc(s.StoreCode)}</b>"), ("store", $"<b>{Esc(s.Destination)}</b>{(string.IsNullOrWhiteSpace(s.DrNumbers) ? "" : "<br/>DR# " + Esc(s.DrNumbers))}"),
                        ("in", s.ArrivalTime), ("out", s.DepartureTime), ("rep", s.StoreManagerName), ("le", Le(s)), ("lg", Lg(s)), ("box", s.OthersBox), ("pf", s.OthersPfoods), ("mi", s.OthersMi),
                        ("re", s.ReturnedECrates), ("rg", s.ReturnedGCrates), ("remarks", s.Remarks))).ToList();
                    var totals = opt.On("totals") ? new Dictionary<string, string> { ["store"] = "TOTAL", ["le"] = stops.Sum(Le).ToString(), ["lg"] = stops.Sum(Lg).ToString(), ["re"] = stops.Sum(s => s.ReturnedECrates).ToString(), ["rg"] = stops.Sum(s => s.ReturnedGCrates).ToString() } : null;
                    sb.Append(SectionTitle("Stores"));
                    sb.Append(Table(opt, new[] { ("no", "No", "center"), ("code", "Store code", "center"), ("store", "Store name & DR / invoice #", ""), ("in", "Store in", "center"), ("out", "Store out", "center"), ("rep", "Store representative (sign over printed name)", ""),
                        ("le", "Loaded E", "center"), ("lg", "Loaded G", "center"), ("box", "Box", "center"), ("pf", "P.Foods", "center"), ("mi", "M.I.", "center"), ("re", "Returned E", "center"), ("rg", "Returned G", "center"), ("remarks", "Remarks", "") },
                        rows, totals, Math.Max(0, opt.BlankRows - stops.Count)));
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
