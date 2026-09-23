using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    public static class PrintHelper
    {
        public static string GeneratePrintableHtml(string moduleTitle, DataGridView grid, string additionalNotes = "", bool selectedOnly = false)
        {
            string logoSrc = GetLogoBase64();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head>");
            sb.AppendLine("<meta charset='utf-8'/>");
            sb.AppendLine($"<title>{moduleTitle} - M.J. Billones Transport</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; margin: 24px; color: #000000; background-color: #ffffff; line-height: 1.4; font-size: 14px; }");
            sb.AppendLine(".header { text-align: center; border-bottom: 2px solid #000000; padding-bottom: 14px; margin-bottom: 20px; }");
            sb.AppendLine(".header img { filter: grayscale(100%) contrast(120%); max-height: 65px; width: auto; margin-bottom: 6px; }");
            sb.AppendLine(".header h1 { margin: 4px 0 2px 0; font-size: 1.6rem; font-weight: 800; color: #000000; letter-spacing: 0.02em; line-height: 1.2; text-transform: uppercase; }");
            sb.AppendLine(".header h2 { margin: 3px 0; font-size: 1.05rem; font-weight: 600; color: #111111; line-height: 1.3; }");
            sb.AppendLine(".header p { margin: 3px 0; font-size: 0.85rem; color: #333333; }");
            sb.AppendLine(".header h3 { margin-top: 10px; color: #000000; font-size: 1.2rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.03em; }");
            sb.AppendLine(".meta-bar { display: flex; justify-content: space-between; font-size: 0.85rem; font-weight: 600; margin-bottom: 16px; background: #ffffff; padding: 10px 14px; border: 1px solid #000000; color: #000000; }");
            sb.AppendLine(".fin-banner { display: flex; justify-content: space-between; gap: 14px; margin-bottom: 18px; }");
            sb.AppendLine(".fin-box { background: #ffffff; border: 1.5px solid #000000; padding: 12px 16px; flex: 1; text-align: center; box-sizing: border-box; }");
            sb.AppendLine(".fin-box-title { font-size: 11px; color: #000000; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; }");
            sb.AppendLine(".fin-box-val { font-size: 20px; color: #000000; font-weight: 800; margin-top: 4px; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 10px; font-size: 0.85rem; border: 1.5px solid #000000; }");
            sb.AppendLine("th { background-color: #000000; color: #ffffff; text-align: left; padding: 8px 10px; font-weight: 700; font-size: 0.82rem; border: 1px solid #000000; letter-spacing: 0.02em; text-transform: uppercase; }");
            sb.AppendLine("td { padding: 8px 10px; border: 1px solid #333333; color: #000000; font-size: 0.82rem; }");
            sb.AppendLine("tr:nth-child(even) { background-color: #f5f5f5; }");
            sb.AppendLine(".footer { margin-top: 40px; display: flex; justify-content: space-between; font-size: 0.85rem; color: #000000; page-break-inside: avoid; }");
            sb.AppendLine(".sig-box { width: 220px; text-align: center; border-top: 1.5px solid #000000; padding-top: 6px; margin-top: 36px; font-weight: 600; }");
            sb.AppendLine("button { font-family: inherit; }");
            sb.AppendLine("@page { size: letter portrait; margin: 12mm 14mm; } @media print { html, body { width: 8.5in; height: 11in; } }");
            sb.AppendLine("@media print { * { -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; } .no-print { display: none !important; } body { margin: 0; } }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<div class='no-print' style='margin-bottom:15px; text-align:right;'>");
            sb.AppendLine("<button onclick='window.print()' style='background:#000000; color:#ffffff; border:1px solid #000000; padding:8px 18px; font-weight:bold; cursor:pointer; border-radius:4px;'>🖨️ PRINT DOCUMENT / SAVE AS PDF</button>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='header'>");
            if (!string.IsNullOrEmpty(logoSrc))
            {
                sb.AppendLine($"<img src='{logoSrc}' alt='M.J. Billones Transport Service Logo'/>");
            }
            sb.AppendLine("<h1>M.J. BILLONES TRANSPORT SERVICE</h1>");
            sb.AppendLine("<h2>24/7 Delivery & Transport Hauling Services | Depots: Tarlac, Pampanga & Northern Luzon</h2>");
            sb.AppendLine("<p>Garage & Depot: Jasmine St., San Francisco, 2300 City of Tarlac | Phone: (045) 982-1234 / 0917-123-4567</p>");
            sb.AppendLine($"<h3>{moduleTitle.ToUpper()} {(selectedOnly ? "(SELECTED SELECTION ONLY)" : "")}</h3>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='meta-bar'>");
            sb.AppendLine($"<div><b>Report Scope:</b> {(selectedOnly ? "Selected Particular Item(s)" : "Complete Master List")}</div>");
            sb.AppendLine($"<div><b>Date & Time Generated:</b> {DateTime.Now:MMMM dd, yyyy - hh:mm:ss tt}</div>");
            sb.AppendLine("</div>");

            if (moduleTitle.Contains("Billing") || moduleTitle.Contains("Collection") || moduleTitle.Contains("Financial"))
            {
                var fin = Database.MJdatabase.GetFinancialTotals();
                sb.AppendLine("<div class='fin-banner'>");
                sb.AppendLine("<div class='fin-box'>");
                sb.AppendLine("<div class='fin-box-title'>TOTAL AMOUNT COLLECTED</div>");
                sb.AppendLine($"<div class='fin-box-val'>PHP {fin.TotalCollected:N2}</div>");
                sb.AppendLine("</div>");
                sb.AppendLine("<div class='fin-box'>");
                sb.AppendLine("<div class='fin-box-title'>TOTAL TO BE COLLECTED (RECEIVABLES)</div>");
                sb.AppendLine($"<div class='fin-box-val'>PHP {fin.TotalToBeCollected:N2}</div>");
                sb.AppendLine("</div>");
                sb.AppendLine("</div>");
            }

            if (!string.IsNullOrEmpty(additionalNotes))
            {
                sb.AppendLine($"<p style='font-size:12px; font-style:italic; background:#f5f5f5; padding:8px 12px; border:1px solid #000000; margin-bottom:10px;'><b>Note:</b> {additionalNotes}</p>");
            }

            sb.AppendLine("<table><thead><tr>");
            List<int> visibleColIndexes = new List<int>();
            for (int i = 0; i < grid.Columns.Count; i++)
            {
                if (grid.Columns[i].Visible)
                {
                    visibleColIndexes.Add(i);
                    sb.AppendLine($"<th>{grid.Columns[i].HeaderText}</th>");
                }
            }
            sb.AppendLine("</tr></thead><tbody>");

            var rowsToPrint = selectedOnly && grid.SelectedRows.Count > 0
                ? grid.SelectedRows.Cast<DataGridViewRow>()
                : grid.Rows.Cast<DataGridViewRow>();

            foreach (DataGridViewRow row in rowsToPrint)
            {
                if (row.IsNewRow) continue;
                sb.AppendLine("<tr>");
                foreach (int colIdx in visibleColIndexes)
                {
                    var val = row.Cells[colIdx].FormattedValue?.ToString() ?? "";
                    sb.AppendLine($"<td>{val}</td>");
                }
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</tbody></table>");

            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<div class='sig-box'>Prepared By:<br/><br/><br/>_______________________<br/>System Operator / Dispatcher</div>");
            sb.AppendLine("<div class='sig-box'>Approved & Verified By:<br/><br/><br/>_______________________<br/>Marie Ann J. Billones (Owner)</div>");
            sb.AppendLine("</div>");

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        public static void PrintGridReport(string moduleTitle, DataGridView grid, string additionalNotes = "")
        {
            PrintSelectedOrAllGridReport(moduleTitle, grid, additionalNotes);
        }

        public static void PrintSelectedOrAllGridReport(string moduleTitle, DataGridView grid, string additionalNotes = "")
        {
            try
            {
                bool selectedOnly = false;
                if (grid.SelectedRows.Count > 0)
                {
                    var dialogResult = MessageBox.Show(
                        $"You have {grid.SelectedRows.Count} record(s) selected in the table.\n\nClick [YES] to print ONLY the selected item(s).\nClick [NO] to print ALL records in the list.",
                        "Print Scope Selection",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question
                    );

                    if (dialogResult == DialogResult.Cancel) return;
                    selectedOnly = (dialogResult == DialogResult.Yes);
                }

                string htmlContent = GeneratePrintableHtml(moduleTitle, grid, additionalNotes, selectedOnly);
                string tempPath = Path.Combine(Path.GetTempPath(), $"Print_{moduleTitle.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                File.WriteAllText(tempPath, htmlContent);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open print preview: " + ex.Message, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void PrintCrewDossierAndHistory(Crew crew, List<Delivery> history)
        {
            try
            {
                string logoSrc = GetLogoBase64();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<!DOCTYPE html>");
                sb.AppendLine("<html><head><meta charset='utf-8'/>");
                sb.AppendLine($"<title>Personnel Profile - {crew.FullName}</title>");
                sb.AppendLine("<style>");
                sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; margin: 24px; color: #000000; background-color: #ffffff; line-height: 1.4; font-size: 14px; }");
                sb.AppendLine(".header { text-align: center; border-bottom: 2px solid #000000; padding-bottom: 14px; margin-bottom: 20px; }");
                sb.AppendLine(".header img { filter: grayscale(100%) contrast(120%); max-height: 60px; margin-bottom: 4px; }");
                sb.AppendLine(".header h1 { margin: 4px 0 2px 0; font-size: 1.6rem; font-weight: 800; color: #000000; letter-spacing: 0.02em; line-height: 1.2; text-transform: uppercase; }");
                sb.AppendLine(".header h3 { color: #000000; font-size: 1.1rem; font-weight: 700; margin: 4px 0; text-transform: uppercase; }");
                sb.AppendLine(".dossier { border: 1.5px solid #000000; padding: 16px; background: #ffffff; margin-bottom: 20px; }");
                sb.AppendLine(".dossier table { width: 100%; border-collapse: collapse; }");
                sb.AppendLine(".dossier td { padding: 6px 10px; font-size: 0.9rem; color: #000000; }");
                sb.AppendLine(".dossier td.label { font-weight: 700; color: #000000; width: 180px; font-size: 0.85rem; }");
                sb.AppendLine("table.hist { width: 100%; border-collapse: collapse; margin-top: 10px; font-size: 0.85rem; border: 1.5px solid #000000; }");
                sb.AppendLine("table.hist th { background: #000000; color: #ffffff; padding: 8px 10px; text-align: left; font-weight: 700; font-size: 0.82rem; border: 1px solid #000000; text-transform: uppercase; }");
                sb.AppendLine("table.hist td { border: 1px solid #333333; padding: 8px 10px; color: #000000; font-size: 0.82rem; }");
                sb.AppendLine("table.hist tr:nth-child(even) { background: #f5f5f5; }");
                sb.AppendLine("@page { size: letter portrait; margin: 12mm 14mm; } @media print { html, body { width: 8.5in; height: 11in; } }");
                sb.AppendLine("@media print { * { -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; } .no-print { display: none !important; } body { margin: 0; } }");
                sb.AppendLine("</style></head><body>");

                sb.AppendLine("<div class='no-print' style='text-align:right; margin-bottom:15px;'>");
                sb.AppendLine("<button onclick='window.print()' style='background:#000000; color:#ffffff; border:1px solid #000000; padding:8px 18px; font-weight:bold; cursor:pointer; border-radius:4px;'>🖨️ PRINT PERSONNEL DOSSIER & HISTORY</button>");
                sb.AppendLine("</div>");

                sb.AppendLine("<div class='header'>");
                if (!string.IsNullOrEmpty(logoSrc)) sb.AppendLine($"<img src='{logoSrc}'/>");
                sb.AppendLine("<h1>M.J. BILLONES TRANSPORT SERVICE</h1>");
                sb.AppendLine("<h3>INDIVIDUAL PERSONNEL PROFILE & PAST DELIVERY HISTORY</h3>");
                sb.AppendLine("</div>");

                sb.AppendLine("<div class='dossier'>");
                sb.AppendLine("<table>");
                sb.AppendLine($"<tr><td class='label'>FULL NAME:</td><td><b>{crew.FullName}</b></td><td class='label'>CREW ROLE:</td><td><b>{crew.Role}</b></td></tr>");
                sb.AppendLine($"<tr><td class='label'>CONTACT NUMBER:</td><td>{crew.ContactNumber}</td><td class='label'>LICENSE NUMBER:</td><td>{crew.LicenseNumber}</td></tr>");
                sb.AppendLine($"<tr><td class='label'>EMPLOYMENT STATUS:</td><td><b>{(crew.IsSeasonal ? "SEASONAL / TEMPORARY" : "REGULAR ACTIVE")}</b></td><td class='label'>ASSIGNED ROLE:</td><td><b>{crew.Role}</b></td></tr>");
                sb.AppendLine($"<tr><td class='label'>CURRENT STATUS:</td><td><b>{(crew.IsAvailable ? "ACTIVE (DUTY READY)" : "INACTIVE / ON TRIP")}</b></td><td class='label'>TOTAL COMPLETED TRIPS:</td><td><b>{history.Count} Trips</b></td></tr>");
                sb.AppendLine("</table>");
                sb.AppendLine("</div>");

                sb.AppendLine("<h4 style='color:#000000; margin-bottom:6px; font-size:14px; text-transform:uppercase; font-weight:700;'>PAST DEPLOYMENT & TRIP HISTORY MANIFEST</h4>");
                if (history.Count == 0)
                {
                    sb.AppendLine("<p style='font-style:italic;'>No past delivery trips recorded for this personnel member yet.</p>");
                }
                else
                {
                    sb.AppendLine("<table class='hist'><thead><tr>");
                    sb.AppendLine("<th>Dispatch #</th><th>Loading Order / Client</th><th>Truck Assigned</th><th>Dispatch Date</th><th>Time</th><th>Status</th>");
                    sb.AppendLine("</tr></thead><tbody>");
                    foreach (var h in history)
                    {
                        sb.AppendLine("<tr>");
                        sb.AppendLine($"<td>{h.DispatchNumber}</td>");
                        sb.AppendLine($"<td>{h.LoadingScheduleInfo}</td>");
                        sb.AppendLine($"<td>{h.TruckInfo}</td>");
                        sb.AppendLine($"<td>{h.DispatchDate:MM/dd/yyyy}</td>");
                        sb.AppendLine($"<td>{h.DepartureTime}</td>");
                        sb.AppendLine($"<td><b>{h.Status}</b></td>");
                        sb.AppendLine("</tr>");
                    }
                    sb.AppendLine("</tbody></table>");
                }

                sb.AppendLine("<div style='margin-top:40px; display:flex; justify-content:space-between; font-size:13px; font-weight:600;'>");
                sb.AppendLine("<div style='text-align:center; width:220px; border-top:1.5px solid #000000; padding-top:6px;'>Personnel Signature</div>");
                sb.AppendLine("<div style='text-align:center; width:220px; border-top:1.5px solid #000000; padding-top:6px;'>Marie Ann J. Billones (Owner)</div>");
                sb.AppendLine("</div>");

                sb.AppendLine("</body></html>");

                string tempPath = Path.Combine(Path.GetTempPath(), $"Crew_History_{crew.FullName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                File.WriteAllText(tempPath, sb.ToString());

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open personnel history report: " + ex.Message, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static string GenerateOfficialCollectionReceiptHtml(CollectionReceipt cr)
        {
            string receiptNo = string.IsNullOrEmpty(cr.ReceiptNumber) ? $"CRV-{DateTime.Now:yyyyMMdd-HHmmss}" : cr.ReceiptNumber;

            var invoices = Database.MJdatabase.GetInvoices();
            var inv = invoices.Find(i => i.Id == cr.InvoiceId || (!string.IsNullOrEmpty(cr.InvoiceNumber) && i.InvoiceNumber.Equals(cr.InvoiceNumber, StringComparison.OrdinalIgnoreCase)));

            string clientName = inv != null && !string.IsNullOrWhiteSpace(inv.ClientName) ? inv.ClientName : "M.J. Billones Transport Service Client";
            string invoiceNo = inv != null ? inv.InvoiceNumber : (!string.IsNullOrEmpty(cr.InvoiceNumber) ? cr.InvoiceNumber : "N/A");

            decimal grossAmount = inv != null ? inv.TotalAmount : cr.AmountPaid;
            decimal deductions = inv != null ? inv.Deductions : 0.00m;
            decimal netDue = inv != null ? inv.NetAmount : cr.AmountPaid;
            decimal amountPaid = cr.AmountPaid;

            decimal totalCollected = inv != null ? Database.MJdatabase.GetTotalCollectedForInvoice(inv.Id) : amountPaid;
            decimal remainingBalance = inv != null ? Math.Max(0, inv.NetAmount - totalCollected) : 0.00m;

            string payMethod = string.IsNullOrEmpty(cr.PaymentMethod) ? "Cash" : cr.PaymentMethod;
            bool isCash = payMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase);
            bool isCheck = payMethod.Equals("Check", StringComparison.OrdinalIgnoreCase) || payMethod.Equals("Cheque", StringComparison.OrdinalIgnoreCase);
            bool isCard = payMethod.Contains("Card", StringComparison.OrdinalIgnoreCase);
            bool isBank = payMethod.Contains("Bank", StringComparison.OrdinalIgnoreCase) || payMethod.Contains("Transfer", StringComparison.OrdinalIgnoreCase) || payMethod.Contains("GCash", StringComparison.OrdinalIgnoreCase);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head>");
            sb.AppendLine("<meta charset='utf-8'/>");
            sb.AppendLine($"<title>Official Collection Receipt {receiptNo} - M.J. Billones Transport Service</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; background-color: #ffffff; margin: 20px; display: flex; flex-direction: column; align-items: center; color: #000000; }");
            sb.AppendLine(".no-print { margin-bottom: 16px; width: 850px; text-align: right; }");
            sb.AppendLine(".receipt-card { width: 850px; background-color: #ffffff; border: 2px solid #000000; padding: 24px 28px; color: #000000; box-sizing: border-box; }");
            sb.AppendLine(".header-table { width: 100%; border-bottom: 2px solid #000000; padding-bottom: 10px; margin-bottom: 12px; }");
            sb.AppendLine(".company-title { font-size: 20px; font-weight: 900; letter-spacing: 0.5px; color: #000000; margin: 0; text-transform: uppercase; }");
            sb.AppendLine(".company-sub { font-size: 12px; font-weight: 600; color: #111111; margin: 2px 0; }");
            sb.AppendLine(".receipt-badge { font-size: 15px; font-weight: 800; color: #000000; background: #f5f5f5; border: 1.5px solid #000000; padding: 4px 10px; display: inline-block; }");
            sb.AppendLine(".meta-grid { width: 100%; border-collapse: collapse; margin-bottom: 12px; }");
            sb.AppendLine(".meta-grid td { padding: 4px 8px; font-size: 12.5px; vertical-align: top; }");
            sb.AppendLine(".box-panel { border: 1.5px solid #000000; padding: 10px 14px; background: #ffffff; margin-bottom: 14px; }");
            sb.AppendLine(".data-table { width: 100%; border-collapse: collapse; margin: 12px 0; border: 1.5px solid #000000; background: #ffffff; }");
            sb.AppendLine(".data-table th { background-color: #000000; color: #ffffff; border: 1px solid #000000; padding: 8px 10px; font-size: 11.5px; font-weight: 700; text-align: center; text-transform: uppercase; }");
            sb.AppendLine(".data-table td { border: 1px solid #000000; padding: 8px 10px; font-size: 12px; text-align: center; font-weight: 600; }");
            sb.AppendLine(".summary-container { display: flex; justify-content: space-between; gap: 16px; margin-top: 10px; }");
            sb.AppendLine(".summary-notes { flex: 1; border: 1.5px solid #000000; padding: 10px 14px; background: #ffffff; font-size: 12px; line-height: 1.6; }");
            sb.AppendLine(".summary-total-card { width: 340px; border: 2px solid #000000; background: #ffffff; padding: 12px 16px; text-align: right; box-sizing: border-box; }");
            sb.AppendLine(".footer-disclaimer { margin-top: 14px; border-top: 1px solid #000000; padding-top: 8px; font-size: 10px; text-align: center; color: #333333; }");
            sb.AppendLine("@page { size: letter portrait; margin: 12mm 14mm; } @media print { html, body { width: 8.5in; height: 11in; } }");
            sb.AppendLine("@media print { * { -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; } body { background-color: #ffffff; margin: 0; } .no-print { display: none !important; } .receipt-card { border: 2px solid #000000; } }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<div class='no-print'>");
            sb.AppendLine("<button onclick='window.print()' style='background:#000000; color:#ffffff; border:1px solid #000000; padding:10px 20px; font-weight:bold; cursor:pointer; border-radius:4px; font-size:14px;'>🖨️ PRINT OFFICIAL COLLECTION RECEIPT SLIP</button>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='receipt-card'>");

            sb.AppendLine("<table class='header-table'><tr>");
            sb.AppendLine("<td style='vertical-align:top; width:65%;'>");
            sb.AppendLine("<div class='company-title'>M.J. BILLONES TRANSPORT SERVICE</div>");
            sb.AppendLine("<div class='company-sub'>Jasmine St., San Francisco, 2300 City of Tarlac</div>");
            sb.AppendLine("<div class='company-sub'>Proprietor: MARIE ANN J. BILLONES | 24/7 Cargo & Delivery Transport</div>");
            sb.AppendLine("<div class='company-sub'>TIN: 708-839-875-00000 Non-VAT Delivery Services</div>");
            sb.AppendLine("</td>");
            sb.AppendLine("<td style='vertical-align:top; text-align:right; width:35%;'>");
            sb.AppendLine("<div style='font-size:11px; font-weight:bold; color:#000000; margin-bottom:4px;'>COLLECTION RECEIPT NO.</div>");
            sb.AppendLine($"<div class='receipt-badge'>{receiptNo}</div>");
            sb.AppendLine("</td></tr></table>");

            sb.AppendLine("<table class='meta-grid'><tr>");
            sb.AppendLine("<td style='width:60%;'>");
            sb.AppendLine("<div style='font-weight:700; font-size:13px; margin-bottom:4px;'>PAYMENT METHOD:</div>");
            sb.AppendLine($"<span style='font-size:13px;'>{(isCash ? "☑" : "☐")} CASH &nbsp;&nbsp;&nbsp;&nbsp; {(isCheck ? "☑" : "☐")} CHECK &nbsp;&nbsp;&nbsp;&nbsp; {(isBank ? "☑" : "☐")} BANK/GCASH &nbsp;&nbsp;&nbsp;&nbsp; {(isCard ? "☑" : "☐")} CARD</span>");
            sb.AppendLine("</td>");
            sb.AppendLine("<td style='width:40%; text-align:right;'>");
            sb.AppendLine($"<b>Payment Date:</b> {cr.PaymentDate:MMMM dd, yyyy}<br/>");
            sb.AppendLine($"<b>Ref / Check #:</b> {(!string.IsNullOrEmpty(cr.ReferenceNumber) ? cr.ReferenceNumber : "N/A")}");
            sb.AppendLine("</td></tr></table>");

            sb.AppendLine("<div class='box-panel'>");
            sb.AppendLine("<table style='width:100%; font-size:13px;'>");
            sb.AppendLine($"<tr><td style='width:120px; font-weight:700;'>RECEIVED FROM:</td><td style='font-weight:700; color:#000000; font-size:14px; text-transform:uppercase;'><u>{clientName}</u></td></tr>");
            sb.AppendLine($"<tr><td style='font-weight:700;'>SERVICE TYPE:</td><td>Hauling & Cargo Delivery Services</td></tr>");
            sb.AppendLine($"<tr><td style='font-weight:700;'>INVOICE APPLIED:</td><td><b>SI NO. {invoiceNo}</b></td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");

            sb.AppendLine("<table class='data-table'>");
            sb.AppendLine("<thead><tr>");
            sb.AppendLine("<th>Service Invoice #</th>");
            sb.AppendLine("<th>Trip Gross Amount</th>");
            sb.AppendLine("<th>Incident Deductions</th>");
            sb.AppendLine("<th>Net Invoice Due</th>");
            sb.AppendLine("<th>Amount Paid</th>");
            sb.AppendLine("<th>Remaining Balance</th>");
            sb.AppendLine("</tr></thead><tbody>");
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{invoiceNo}</td>");
            sb.AppendLine($"<td>PHP {grossAmount:N2}</td>");
            sb.AppendLine($"<td>{(deductions > 0 ? $"- PHP {deductions:N2}" : "PHP 0.00")}</td>");
            sb.AppendLine($"<td>PHP {netDue:N2}</td>");
            sb.AppendLine($"<td><b>PHP {amountPaid:N2}</b></td>");
            sb.AppendLine($"<td>{(remainingBalance > 0 ? $"PHP {remainingBalance:N2}" : "FULLY PAID")}</td>");
            sb.AppendLine("</tr>");
            sb.AppendLine("</tbody></table>");

            sb.AppendLine("<div class='summary-container'>");
            sb.AppendLine("<div class='summary-notes'>");
            sb.AppendLine("<b>Payment Status & Remarks:</b><br/>");
            sb.AppendLine($"• Transaction Reference: {(!string.IsNullOrWhiteSpace(cr.Notes) ? cr.Notes : "Official Transport Service Collection")}<br/>");
            if (deductions > 0)
            {
                sb.AppendLine($"• Cargo incident loss deduction of <b>PHP {deductions:N2}</b> applied per incident logs.<br/>");
            }
            sb.AppendLine($"• Paid Date: {cr.PaymentDate:MMMM dd, yyyy}<br/>");
            sb.AppendLine("• <i>Thank you for your business!</i>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='summary-total-card'>");
            sb.AppendLine("<div style='font-size:11px; font-weight:700; color:#000000;'>OFFICIAL PAYMENT RECEIVED:</div>");
            sb.AppendLine($"<div style='font-size:22px; font-weight:900; color:#000000; margin:4px 0;'>PHP {amountPaid:N2}</div>");
            sb.AppendLine($"<div style='font-size:11.5px; font-weight:700; color:#000000;'>Invoice: SI NO. {invoiceNo}</div>");
            sb.AppendLine($"<div style='font-size:11px; color:#333333;'>Remaining Balance: PHP {remainingBalance:N2}</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='footer-disclaimer'>");
            sb.AppendLine("DIOLAZO Printing Press & Gen. Mdse. Corp. | F. Tañedo Street, San Nicolas, 2300 City of Tarlac<br/>");
            sb.AppendLine("BIR Authority to Print OCN: 17AAU20240000004957 | Official Cargo Delivery Collection Slip");
            sb.AppendLine("</div>");

            sb.AppendLine("</div></body></html>");
            return sb.ToString();
        }

        public static void PrintOfficialCollectionReceipt(CollectionReceipt cr)
        {
            try
            {
                string htmlContent = GenerateOfficialCollectionReceiptHtml(cr);
                string tempPath = Path.Combine(Path.GetTempPath(), $"Official_Receipt_{cr.ReceiptNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                File.WriteAllText(tempPath, htmlContent);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open official receipt slip: " + ex.Message, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static string GenerateStatementOfAccountHtml(StatementOfAccount soa, List<Invoice> invoices)
        {
            string logoSrc = GetLogoBase64();
            StringBuilder sb = new StringBuilder();

            decimal totalGross = 0;
            decimal totalDeductions = 0;
            decimal totalNet = 0;

            foreach (var inv in invoices)
            {
                totalGross += inv.TotalAmount;
                totalDeductions += inv.Deductions;
                totalNet += inv.NetAmount;
            }

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='utf-8'/>");
            sb.AppendLine($"<title>STATEMENT OF ACCOUNT - {soa.SoaNumber}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 20px; color: #000000; background: #ffffff; font-size: 13.5px; line-height: 1.4; }");
            sb.AppendLine(".soa-card { max-width: 900px; margin: 0 auto; border: 1.5px solid #000000; padding: 24px; }");
            sb.AppendLine(".header-table { width: 100%; border-bottom: 2px solid #000000; padding-bottom: 12px; margin-bottom: 16px; }");
            sb.AppendLine(".header-table img { filter: grayscale(100%) contrast(120%); max-height: 55px; margin-bottom: 4px; }");
            sb.AppendLine(".company-title { font-size: 20px; font-weight: 800; color: #000000; text-transform: uppercase; }");
            sb.AppendLine(".company-sub { font-size: 11.5px; color: #333333; margin-top: 2px; }");
            sb.AppendLine(".soa-badge { background: #f5f5f5; border: 1.5px solid #000000; color: #000000; padding: 6px 12px; font-weight: 800; font-size: 15px; display: inline-block; }");
            sb.AppendLine(".client-meta { width: 100%; background: #ffffff; border: 1.5px solid #000000; padding: 12px; margin-bottom: 16px; }");
            sb.AppendLine(".client-meta td { padding: 4px 8px; font-size: 13px; color: #000000; }");
            sb.AppendLine(".data-table { width: 100%; border-collapse: collapse; margin-top: 10px; font-size: 12.5px; border: 1.5px solid #000000; }");
            sb.AppendLine(".data-table th { background: #000000; color: #ffffff; padding: 8px 10px; text-align: left; font-weight: 700; font-size: 12px; border: 1px solid #000000; text-transform: uppercase; }");
            sb.AppendLine(".data-table td { padding: 8px 10px; border: 1px solid #333333; color: #000000; }");
            sb.AppendLine(".data-table tr:nth-child(even) { background-color: #f5f5f5; }");
            sb.AppendLine(".summary-box { width: 340px; margin-left: auto; margin-top: 16px; border: 1.5px solid #000000; background: #ffffff; padding: 12px; }");
            sb.AppendLine(".summary-box table { width: 100%; font-size: 13px; }");
            sb.AppendLine(".summary-box td { padding: 4px 6px; color: #000000; }");
            sb.AppendLine(".sig-area { margin-top: 36px; display: flex; justify-content: space-between; page-break-inside: avoid; }");
            sb.AppendLine(".sig-box { width: 220px; text-align: center; border-top: 1.5px solid #000000; padding-top: 6px; font-size: 12px; font-weight: 600; color: #000000; }");
            sb.AppendLine("@page { size: letter portrait; margin: 12mm 14mm; } @media print { html, body { width: 8.5in; height: 11in; } }");
            sb.AppendLine("@media print { * { -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; } .no-print { display: none !important; } body { margin: 0; } .soa-card { border: 1.5px solid #000000; } }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<div class='no-print' style='text-align:right; margin-bottom:12px;'>");
            sb.AppendLine("<button onclick='window.print()' style='background:#000000; color:#ffffff; border:1px solid #000000; padding:8px 18px; font-weight:bold; cursor:pointer; border-radius:4px;'>🖨️ PRINT STATEMENT OF ACCOUNT</button>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='soa-card'>");
            sb.AppendLine("<table class='header-table'><tr>");
            sb.AppendLine("<td style='width:65%;'>");
            if (!string.IsNullOrEmpty(logoSrc)) sb.AppendLine($"<img src='{logoSrc}'/>");
            sb.AppendLine("<div class='company-title'>M.J. BILLONES TRANSPORT SERVICE</div>");
            sb.AppendLine("<div class='company-sub'>Jasmine St., San Francisco, 2300 City of Tarlac | Phone: (045) 982-1234</div>");
            sb.AppendLine("<div class='company-sub'>24/7 Cold Chain & Dry Cargo Hauling Services</div>");
            sb.AppendLine("</td>");
            sb.AppendLine("<td style='width:35%; text-align:right; vertical-align:top;'>");
            sb.AppendLine("<div style='font-size:11px; font-weight:bold; color:#000000; margin-bottom:4px;'>STATEMENT OF ACCOUNT</div>");
            sb.AppendLine($"<div class='soa-badge'>{soa.SoaNumber}</div>");
            sb.AppendLine($"<div style='font-size:12px; color:#000000; margin-top:6px;'><b>Date:</b> {soa.IssueDate:MMMM dd, yyyy}</div>");
            sb.AppendLine("</td></tr></table>");

            sb.AppendLine("<table class='client-meta'><tr>");
            sb.AppendLine($"<td style='width:60%;'><b>BILLED TO:</b> <span style='font-size:15px; font-weight:800; color:#000000;'>{soa.ClientName.ToUpper()}</span></td>");
            sb.AppendLine($"<td style='width:40%;'><b>PERIOD COVERED:</b> {(!string.IsNullOrEmpty(soa.PeriodCovered) ? soa.PeriodCovered : "Current Billing Cycle")}</td>");
            sb.AppendLine("</tr><tr>");
            sb.AppendLine("<td><b>SERVICE DESCRIPTION:</b> Hauling, Logistics & Truck Delivery Transport</td>");
            sb.AppendLine($"<td><b>STATUS:</b> <b>{soa.Status.ToUpper()}</b></td>");
            sb.AppendLine("</tr></table>");

            sb.AppendLine("<table class='data-table'>");
            sb.AppendLine("<thead><tr>");
            sb.AppendLine("<th>#</th>");
            sb.AppendLine("<th>Service Invoice #</th>");
            sb.AppendLine("<th>Issue Date</th>");
            sb.AppendLine("<th>Due Date</th>");
            sb.AppendLine("<th>Gross Hauling</th>");
            sb.AppendLine("<th>Incident Deductions</th>");
            sb.AppendLine("<th>Net Due Amount</th>");
            sb.AppendLine("<th>Payment Status</th>");
            sb.AppendLine("</tr></thead><tbody>");

            int idx = 1;
            foreach (var inv in invoices)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{idx++}</td>");
                sb.AppendLine($"<td><b>{inv.InvoiceNumber}</b></td>");
                sb.AppendLine($"<td>{inv.IssueDate:MM/dd/yyyy}</td>");
                sb.AppendLine($"<td>{inv.DueDate:MM/dd/yyyy}</td>");
                sb.AppendLine($"<td>PHP {inv.TotalAmount:N2}</td>");
                sb.AppendLine($"<td>{(inv.Deductions > 0 ? $"- PHP {inv.Deductions:N2}" : "PHP 0.00")}</td>");
                sb.AppendLine($"<td><b>PHP {inv.NetAmount:N2}</b></td>");
                sb.AppendLine($"<td>{inv.Status}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</tbody></table>");

            sb.AppendLine("<div class='summary-box'>");
            sb.AppendLine("<table>");
            sb.AppendLine($"<tr><td>Total Gross Transport:</td><td style='text-align:right; font-weight:600;'>PHP {totalGross:N2}</td></tr>");
            if (totalDeductions > 0)
            {
                sb.AppendLine($"<tr><td>Less Incident Deductions:</td><td style='text-align:right; font-weight:600;'>- PHP {totalDeductions:N2}</td></tr>");
            }
            sb.AppendLine("<tr style='border-top:1.5px solid #000000;'><td style='font-weight:800; color:#000000; font-size:14px; padding-top:6px;'>TOTAL NET PAYABLE:</td><td style='text-align:right; font-weight:900; color:#000000; font-size:16px; padding-top:6px;'>PHP {totalNet:N2}</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='sig-area'>");
            sb.AppendLine("<div class='sig-box'>Prepared By:<br/><br/><br/>Billing & Accounting Staff</div>");
            sb.AppendLine("<div class='sig-box'>Checked & Verified By:<br/><br/><br/>Operations Supervisor</div>");
            sb.AppendLine("<div class='sig-box'>Approved For Release:<br/><br/><br/>Marie Ann J. Billones<br/>Proprietor / Management</div>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div></body></html>");
            return sb.ToString();
        }

        public static void PrintStatementOfAccount(StatementOfAccount soa, List<Invoice> invoices)
        {
            try
            {
                string htmlContent = GenerateStatementOfAccountHtml(soa, invoices);
                string tempPath = Path.Combine(Path.GetTempPath(), $"StatementOfAccount_{soa.SoaNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                File.WriteAllText(tempPath, htmlContent);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open Statement of Account: " + ex.Message, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static string GenerateCrewPaySlipHtml(Crew crew, List<Delivery> trips, DateTime startDate, DateTime endDate)
        {
            string logoSrc = GetLogoBase64();
            StringBuilder sb = new StringBuilder();

            int tripCount = trips.Count;
            decimal totalWages = tripCount * crew.DailyWage;
            decimal totalAllowance = tripCount * crew.FoodAllowance;
            decimal grandTotal = totalWages + totalAllowance;

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='utf-8'/>");
            sb.AppendLine($"<title>CREW TRIP PAY SLIP - {crew.FullName}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 20px; color: #000000; background: #ffffff; font-size: 13.5px; line-height: 1.4; }");
            sb.AppendLine(".payslip-card { max-width: 850px; margin: 0 auto; border: 1.5px solid #000000; padding: 24px; }");
            sb.AppendLine(".header-table { width: 100%; border-bottom: 2px solid #000000; padding-bottom: 12px; margin-bottom: 16px; }");
            sb.AppendLine(".header-table img { filter: grayscale(100%) contrast(120%); max-height: 55px; margin-bottom: 4px; }");
            sb.AppendLine(".company-title { font-size: 20px; font-weight: 800; color: #000000; text-transform: uppercase; }");
            sb.AppendLine(".company-sub { font-size: 11.5px; color: #333333; margin-top: 2px; }");
            sb.AppendLine(".crew-badge { background: #f5f5f5; border: 1.5px solid #000000; color: #000000; padding: 6px 12px; font-weight: 800; font-size: 14px; display: inline-block; }");
            sb.AppendLine(".crew-meta { width: 100%; background: #ffffff; border: 1.5px solid #000000; padding: 12px; margin-bottom: 16px; }");
            sb.AppendLine(".crew-meta td { padding: 4px 8px; font-size: 13px; color: #000000; }");
            sb.AppendLine(".data-table { width: 100%; border-collapse: collapse; margin-top: 10px; font-size: 12.5px; border: 1.5px solid #000000; }");
            sb.AppendLine(".data-table th { background: #000000; color: #ffffff; padding: 8px 10px; text-align: left; font-weight: 700; font-size: 12px; border: 1px solid #000000; text-transform: uppercase; }");
            sb.AppendLine(".data-table td { padding: 8px 10px; border: 1px solid #333333; color: #000000; }");
            sb.AppendLine(".data-table tr:nth-child(even) { background-color: #f5f5f5; }");
            sb.AppendLine(".comp-breakdown { width: 360px; margin-left: auto; margin-top: 16px; border: 1.5px solid #000000; background: #ffffff; padding: 12px; }");
            sb.AppendLine(".comp-breakdown table { width: 100%; font-size: 13px; }");
            sb.AppendLine(".comp-breakdown td { padding: 4px 6px; color: #000000; }");
            sb.AppendLine(".sig-area { margin-top: 36px; display: flex; justify-content: space-between; page-break-inside: avoid; }");
            sb.AppendLine(".sig-box { width: 220px; text-align: center; border-top: 1.5px solid #000000; padding-top: 6px; font-size: 12px; font-weight: 600; color: #000000; }");
            sb.AppendLine("@page { size: letter portrait; margin: 12mm 14mm; } @media print { html, body { width: 8.5in; height: 11in; } }");
            sb.AppendLine("@media print { * { -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; } .no-print { display: none !important; } body { margin: 0; } .payslip-card { border: 1.5px solid #000000; } }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<div class='no-print' style='text-align:right; margin-bottom:12px;'>");
            sb.AppendLine("<button onclick='window.print()' style='background:#000000; color:#ffffff; border:1px solid #000000; padding:8px 18px; font-weight:bold; cursor:pointer; border-radius:4px;'>🖨️ PRINT CREW PAY SLIP</button>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='payslip-card'>");
            sb.AppendLine("<table class='header-table'><tr>");
            sb.AppendLine("<td style='width:65%;'>");
            if (!string.IsNullOrEmpty(logoSrc)) sb.AppendLine($"<img src='{logoSrc}'/>");
            sb.AppendLine("<div class='company-title'>M.J. BILLONES TRANSPORT SERVICE</div>");
            sb.AppendLine("<div class='company-sub'>Garage & Logistics Depot: Jasmine St., San Francisco, 2300 City of Tarlac</div>");
            sb.AppendLine("<div class='company-sub'>Official Crew Trip Compensation & Allowance Pay Slip</div>");
            sb.AppendLine("</td>");
            sb.AppendLine("<td style='width:35%; text-align:right; vertical-align:top;'>");
            sb.AppendLine("<div style='font-size:11px; font-weight:bold; color:#000000; margin-bottom:4px;'>PERSONNEL ROLE</div>");
            sb.AppendLine($"<div class='crew-badge'>{crew.Role.ToUpper()}</div>");
            sb.AppendLine($"<div style='font-size:12px; color:#000000; margin-top:6px;'><b>Date Issued:</b> {DateTime.Now:MMMM dd, yyyy}</div>");
            sb.AppendLine("</td></tr></table>");

            sb.AppendLine("<table class='crew-meta'><tr>");
            sb.AppendLine($"<td style='width:55%;'><b>CREW PERSONNEL:</b> <span style='font-size:15px; font-weight:800; color:#000000;'>{crew.FullName.ToUpper()}</span></td>");
            sb.AppendLine($"<td style='width:45%;'><b>PAY PERIOD:</b> {startDate:MM/dd/yyyy} to {endDate:MM/dd/yyyy}</td>");
            sb.AppendLine("</tr><tr>");
            sb.AppendLine($"<td><b>LICENSE / ID NO:</b> {(!string.IsNullOrEmpty(crew.LicenseNumber) ? crew.LicenseNumber : "N/A")}</td>");
            sb.AppendLine($"<td><b>CONTACT:</b> {crew.ContactNumber}</td>");
            sb.AppendLine("</tr></table>");

            sb.AppendLine("<table class='data-table'>");
            sb.AppendLine("<thead><tr>");
            sb.AppendLine("<th>#</th>");
            sb.AppendLine("<th>Dispatch #</th>");
            sb.AppendLine("<th>Dispatch Date</th>");
            sb.AppendLine("<th>Destination / Route</th>");
            sb.AppendLine("<th>Total Crates</th>");
            sb.AppendLine("<th>Status</th>");
            sb.AppendLine("</tr></thead><tbody>");

            int idx = 1;
            foreach (var t in trips)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{idx++}</td>");
                sb.AppendLine($"<td><b>{t.DispatchNumber}</b></td>");
                sb.AppendLine($"<td>{t.DispatchDate:MM/dd/yyyy}</td>");
                sb.AppendLine($"<td>{t.LoadingScheduleInfo}</td>");
                sb.AppendLine($"<td>{t.TotalCrates:N0} Crates</td>");
                sb.AppendLine($"<td><b>{t.Status}</b></td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</tbody></table>");

            sb.AppendLine("<div class='comp-breakdown'>");
            sb.AppendLine("<table>");
            sb.AppendLine($"<tr><td>Total Completed Trips:</td><td style='text-align:right; font-weight:700;'>{tripCount} Trip(s)</td></tr>");
            sb.AppendLine($"<tr><td>Daily Wage Rate:</td><td style='text-align:right;'>PHP {crew.DailyWage:N2} / trip</td></tr>");
            sb.AppendLine($"<tr><td>Total Base Wages:</td><td style='text-align:right; font-weight:600;'>PHP {totalWages:N2}</td></tr>");
            sb.AppendLine($"<tr><td>Meal / Food Allowance:</td><td style='text-align:right;'>PHP {crew.FoodAllowance:N2} / trip</td></tr>");
            sb.AppendLine($"<tr><td>Total Allowances:</td><td style='text-align:right; font-weight:600;'>PHP {totalAllowance:N2}</td></tr>");
            sb.AppendLine("<tr style='border-top:1.5px solid #000000;'><td style='font-weight:800; color:#000000; font-size:14px; padding-top:6px;'>NET PAYABLE AMOUNT:</td><td style='text-align:right; font-weight:900; color:#000000; font-size:16px; padding-top:6px;'>PHP {grandTotal:N2}</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='sig-area'>");
            sb.AppendLine("<div class='sig-box'>Prepared By:<br/><br/><br/>Payroll Officer</div>");
            sb.AppendLine("<div class='sig-box'>Approved By:<br/><br/><br/>Marie Ann J. Billones<br/>Operations Head</div>");
            sb.AppendLine($"<div class='sig-box'>Received & Acknowledged:<br/><br/><br/>{crew.FullName}<br/>Crew Personnel</div>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div></body></html>");
            return sb.ToString();
        }

        public static void PrintCrewPaySlip(Crew crew, List<Delivery> trips, DateTime startDate, DateTime endDate)
        {
            try
            {
                string htmlContent = GenerateCrewPaySlipHtml(crew, trips, startDate, endDate);
                string tempPath = Path.Combine(Path.GetTempPath(), $"CrewPaySlip_{crew.FullName.Replace(' ', '_')}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                File.WriteAllText(tempPath, htmlContent);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open Crew Pay Slip: " + ex.Message, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string GetLogoBase64()
        {
            try
            {
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
                if (!File.Exists(logoPath))
                {
                    logoPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "logo.png");
                }
                if (File.Exists(logoPath))
                {
                    byte[] imageBytes = File.ReadAllBytes(logoPath);
                    return "data:image/png;base64," + Convert.ToBase64String(imageBytes);
                }
            }
            catch { }
            return "";
        }
    }
}
