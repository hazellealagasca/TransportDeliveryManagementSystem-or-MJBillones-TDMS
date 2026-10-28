/**
 * M.J. Billones Transport Service - Print Document Studio
 * Generates print-ready HTML for Official Collection Receipts, Billing Statements,
 * Trip Reports, and Dispatch Slips. Matches Printer.cs formatting.
 */

class DocumentPrinter {
  constructor() {
    this.printContainer = null;
    this.init();
  }

  init() {
    let el = document.getElementById('print-area');
    if (!el) {
      el = document.createElement('div');
      el.id = 'print-area';
      document.body.appendChild(el);
    }
    this.printContainer = el;
  }

  printDispatchSlip(delivery) {
    const html = `
      <div style="font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 24px; border: 2px solid #333;">
        <div style="display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #B4182C; padding-bottom: 12px; margin-bottom: 16px;">
          <div>
            <h1 style="color: #B4182C; margin: 0; font-size: 22px; font-weight: bold;">M.J. BILLONES TRANSPORT SERVICE</h1>
            <p style="margin: 4px 0 0 0; font-size: 13px; color: #555;">Commercial Hauling & Delivery Logistics Management</p>
          </div>
          <div style="text-align: right;">
            <div style="font-size: 16px; font-weight: bold; color: #1E293B;">DELIVERY DISPATCH SLIP</div>
            <div style="font-size: 14px; font-weight: bold; color: #B4182C; margin-top: 4px;">${delivery.DispatchNumber}</div>
          </div>
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 20px; font-size: 13px;">
          <div>
            <p style="margin: 4px 0;"><strong>Dispatch Date:</strong> ${delivery.DispatchDate}</p>
            <p style="margin: 4px 0;"><strong>Departure Time:</strong> ${delivery.DepartureTime}</p>
            <p style="margin: 4px 0;"><strong>Client / Order:</strong> ${delivery.LoadingScheduleInfo || 'N/A'}</p>
            <p style="margin: 4px 0;"><strong>Cargo Type:</strong> <span style="display: inline-block; padding: 2px 6px; background: #E0F2FE; color: #0369A1; border-radius: 4px;">${delivery.CargoType || 'Standard'}</span></p>
          </div>
          <div>
            <p style="margin: 4px 0;"><strong>Assigned Truck:</strong> ${delivery.TruckInfo}</p>
            <p style="margin: 4px 0;"><strong>Assigned Driver:</strong> ${delivery.DriverName}</p>
            <p style="margin: 4px 0;"><strong>Assigned Aides:</strong> ${delivery.AideNames || delivery.AideName || 'None'}</p>
            <p style="margin: 4px 0;"><strong>Trip Status:</strong> ${delivery.Status}</p>
          </div>
        </div>

        <table style="width: 100%; border-collapse: collapse; margin-bottom: 24px; font-size: 13px;">
          <thead>
            <tr style="background: #F1F5F9; border-top: 1px solid #CBD5E1; border-bottom: 2px solid #94A3B8;">
              <th style="padding: 8px; text-align: left;">Item / Description</th>
              <th style="padding: 8px; text-align: right;">G-Crates</th>
              <th style="padding: 8px; text-align: right;">E-Crates</th>
              <th style="padding: 8px; text-align: right;">Total Crates</th>
            </tr>
          </thead>
          <tbody>
            <tr style="border-bottom: 1px solid #E2E8F0;">
              <td style="padding: 10px 8px;">Hauling Cargo Load</td>
              <td style="padding: 10px 8px; text-align: right;">${delivery.GCrate || 0}</td>
              <td style="padding: 10px 8px; text-align: right;">${delivery.ECrate || 0}</td>
              <td style="padding: 10px 8px; text-align: right; font-weight: bold;">${delivery.TotalCrates || 0}</td>
            </tr>
          </tbody>
        </table>

        <div style="font-size: 12px; color: #64748B; margin-bottom: 30px;">
          <strong>Remarks:</strong> ${delivery.Remarks || 'None'}
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 20px; margin-top: 40px; text-align: center; font-size: 12px;">
          <div>
            <div style="border-bottom: 1px solid #333; height: 30px;"></div>
            <p style="margin-top: 6px; font-weight: bold;">Prepared By (Dispatcher)</p>
          </div>
          <div>
            <div style="border-bottom: 1px solid #333; height: 30px;"></div>
            <p style="margin-top: 6px; font-weight: bold;">Driver Signature</p>
          </div>
          <div>
            <div style="border-bottom: 1px solid #333; height: 30px;"></div>
            <p style="margin-top: 6px; font-weight: bold;">Store Receiver / Manager</p>
          </div>
        </div>
      </div>
    `;

    this.print(html);
  }

  printOfficialReceipt(receipt, invoice) {
    const formattedAmount = Number(receipt.AmountPaid || 0).toLocaleString('en-US', { minimumFractionDigits: 2 });
    const html = `
      <div style="font-family: Arial, sans-serif; max-width: 750px; margin: 0 auto; padding: 24px; border: 2px solid #800020; border-radius: 6px;">
        <div style="text-align: center; border-bottom: 2px solid #800020; padding-bottom: 12px; margin-bottom: 16px;">
          <h2 style="color: #800020; margin: 0; font-size: 22px; font-weight: bold;">M.J. BILLONES TRANSPORT SERVICE</h2>
          <p style="margin: 4px 0 0 0; font-size: 12px; color: #444;">Commercial Delivery & Hauling Solutions</p>
          <div style="display: inline-block; margin-top: 8px; padding: 4px 16px; background: #800020; color: white; font-weight: bold; font-size: 14px; border-radius: 4px; letter-spacing: 1px;">
            OFFICIAL COLLECTION RECEIPT
          </div>
        </div>

        <div style="display: flex; justify-content: space-between; font-size: 13px; margin-bottom: 16px;">
          <div>
            <p style="margin: 4px 0;"><strong>Receipt No:</strong> <span style="color: #800020; font-weight: bold;">${receipt.ReceiptNumber}</span></p>
            <p style="margin: 4px 0;"><strong>Invoice Reference:</strong> ${receipt.InvoiceNumber || (invoice ? invoice.InvoiceNumber : 'N/A')}</p>
            <p style="margin: 4px 0;"><strong>Client Name:</strong> ${invoice ? invoice.ClientName : 'RED RIBBON BAKESHOP INC.'}</p>
          </div>
          <div style="text-align: right;">
            <p style="margin: 4px 0;"><strong>Date:</strong> ${receipt.PaymentDate}</p>
            <p style="margin: 4px 0;"><strong>Payment Method:</strong> ${receipt.PaymentMethod || 'CASH'}</p>
            ${receipt.ReferenceNumber ? `<p style="margin: 4px 0;"><strong>Check / Ref No:</strong> ${receipt.ReferenceNumber}</p>` : ''}
          </div>
        </div>

        <div style="background: #FDF2F4; border: 1px solid #F6CCD1; padding: 16px; border-radius: 6px; margin-bottom: 20px; text-align: center;">
          <div style="font-size: 13px; color: #64748B;">Amount Received</div>
          <div style="font-size: 28px; font-weight: bold; color: #800020; margin-top: 4px;">PHP ${formattedAmount}</div>
        </div>

        <table style="width: 100%; border-collapse: collapse; font-size: 13px; margin-bottom: 24px;">
          <tr style="border-bottom: 1px solid #DDD;">
            <td style="padding: 8px 0;">Invoice Total Due:</td>
            <td style="padding: 8px 0; text-align: right;">PHP ${invoice ? Number(invoice.NetAmount).toLocaleString('en-US', { minimumFractionDigits: 2 }) : formattedAmount}</td>
          </tr>
          <tr style="border-bottom: 1px solid #DDD;">
            <td style="padding: 8px 0; font-weight: bold;">Amount Paid This Transaction:</td>
            <td style="padding: 8px 0; text-align: right; font-weight: bold; color: #059669;">PHP ${formattedAmount}</td>
          </tr>
        </table>

        <div style="display: flex; justify-content: space-between; margin-top: 40px; font-size: 12px;">
          <div style="width: 40%; text-align: center;">
            <div style="border-bottom: 1px solid #333; height: 35px;"></div>
            <p style="margin-top: 6px; font-weight: bold;">Received By (Authorized Collector)</p>
          </div>
          <div style="width: 40%; text-align: center;">
            <div style="border-bottom: 1px solid #333; height: 35px;"></div>
            <p style="margin-top: 6px; font-weight: bold;">Customer / Representative</p>
          </div>
        </div>
      </div>
    `;

    this.print(html);
  }

  printInvoice(invoice) {
    const gross = Number(invoice.TotalAmount || 0).toLocaleString('en-US', { minimumFractionDigits: 2 });
    const deductions = Number(invoice.Deductions || 0).toLocaleString('en-US', { minimumFractionDigits: 2 });
    const net = Number(invoice.NetAmount || 0).toLocaleString('en-US', { minimumFractionDigits: 2 });

    const html = `
      <div style="font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 24px; border: 1px solid #CBD5E1;">
        <div style="display: flex; justify-content: space-between; border-bottom: 3px solid #B4182C; padding-bottom: 16px; margin-bottom: 20px;">
          <div>
            <h2 style="color: #B4182C; margin: 0; font-size: 24px;">M.J. BILLONES TRANSPORT SERVICE</h2>
            <p style="margin: 4px 0 0 0; font-size: 13px; color: #64748B;">Official Statement of Delivery Account & Billing Invoice</p>
          </div>
          <div style="text-align: right;">
            <div style="font-size: 18px; font-weight: bold; color: #1E293B;">INVOICE</div>
            <div style="font-size: 14px; font-weight: bold; color: #B4182C;">${invoice.InvoiceNumber}</div>
            <div style="font-size: 12px; color: #64748B; margin-top: 4px;">Terms: 15 Days</div>
          </div>
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px; font-size: 13px; margin-bottom: 24px;">
          <div>
            <h4 style="margin: 0 0 8px 0; color: #475569; text-transform: uppercase;">Billed To:</h4>
            <p style="margin: 2px 0; font-weight: bold; font-size: 14px;">${invoice.ClientName}</p>
          </div>
          <div>
            <p style="margin: 3px 0;"><strong>Issue Date:</strong> ${invoice.IssueDate}</p>
            <p style="margin: 3px 0;"><strong>Due Date:</strong> ${invoice.DueDate}</p>
            <p style="margin: 3px 0;"><strong>Payment Status:</strong> <span style="font-weight: bold;">${invoice.Status}</span></p>
          </div>
        </div>

        <table style="width: 100%; border-collapse: collapse; font-size: 13px; margin-bottom: 24px;">
          <thead>
            <tr style="background: #F8FAFC; border-top: 1px solid #E2E8F0; border-bottom: 2px solid #CBD5E1;">
              <th style="padding: 10px; text-align: left;">Description</th>
              <th style="padding: 10px; text-align: right;">Amount (PHP)</th>
            </tr>
          </thead>
          <tbody>
            <tr style="border-bottom: 1px solid #F1F5F9;">
              <td style="padding: 12px 10px;">Gross Delivery Hauling Transport Fee (Delivery #${invoice.DeliveryId || 'N/A'})</td>
              <td style="padding: 12px 10px; text-align: right;">PHP ${gross}</td>
            </tr>
            <tr style="border-bottom: 1px solid #F1F5F9; color: #DC2626;">
              <td style="padding: 12px 10px;">Less: Incident Losses & Damage Deductions</td>
              <td style="padding: 12px 10px; text-align: right;">- PHP ${deductions}</td>
            </tr>
            <tr style="background: #FDF2F4; font-size: 15px; font-weight: bold; border-top: 2px solid #B4182C;">
              <td style="padding: 12px 10px; color: #6B0F1B;">TOTAL NET AMOUNT DUE:</td>
              <td style="padding: 12px 10px; text-align: right; color: #B4182C;">PHP ${net}</td>
            </tr>
          </tbody>
        </table>

        <div style="font-size: 12px; color: #64748B; margin-top: 30px; border-top: 1px dashed #CBD5E1; padding-top: 16px;">
          <p>Please make all checks payable to: <strong>M.J. Billones Transport Service</strong>.</p>
          <p>Payment is due within 15 calendar days from statement issue date.</p>
        </div>
      </div>
    `;

    this.print(html);
  }

  printTripReport(report) {
    const html = `
      <div style="font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 24px; border: 2px solid #333;">
        <div style="display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #B4182C; padding-bottom: 12px; margin-bottom: 16px;">
          <div>
            <h1 style="color: #B4182C; margin: 0; font-size: 22px; font-weight: bold;">M.J. BILLONES TRANSPORT SERVICE</h1>
            <p style="margin: 4px 0 0 0; font-size: 13px; color: #555;">Official Commercial Trip Delivery Report</p>
          </div>
          <div style="text-align: right;">
            <div style="font-size: 16px; font-weight: bold; color: #1E293B;">TRIP REPORT</div>
            <div style="font-size: 14px; font-weight: bold; color: #B4182C; margin-top: 4px;">#TR-${report.Id || '001'}</div>
          </div>
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 20px; font-size: 13px;">
          <div>
            <p style="margin: 4px 0;"><strong>Delivery Reference:</strong> #${report.DeliveryId || 'N/A'}</p>
            <p style="margin: 4px 0;"><strong>Trip Date:</strong> ${report.TripDate || 'N/A'}</p>
            <p style="margin: 4px 0;"><strong>Odometer:</strong> ${report.StartKm || 0} km -> ${report.EndKm || 0} km (${(report.EndKm || 0) - (report.StartKm || 0)} km)</p>
          </div>
          <div>
            <p style="margin: 4px 0;"><strong>Fuel Consumed:</strong> ${report.FuelLiters || 0} Liters (PHP ${Number(report.FuelExpense || 0).toLocaleString()})</p>
            <p style="margin: 4px 0;"><strong>Toll & Other Fees:</strong> PHP ${Number(report.TollFees || 0).toLocaleString()}</p>
            <p style="margin: 4px 0;"><strong>Trip Status:</strong> Completed / Cleared</p>
          </div>
        </div>

        <table style="width: 100%; border-collapse: collapse; margin-bottom: 24px; font-size: 13px;">
          <thead>
            <tr style="background: #F1F5F9; border-top: 1px solid #CBD5E1; border-bottom: 2px solid #94A3B8;">
              <th style="padding: 8px; text-align: left;">Category</th>
              <th style="padding: 8px; text-align: right;">Delivered</th>
              <th style="padding: 8px; text-align: right;">Returned</th>
              <th style="padding: 8px; text-align: right;">Damaged / Missing</th>
            </tr>
          </thead>
          <tbody>
            <tr style="border-bottom: 1px solid #E2E8F0;">
              <td style="padding: 10px 8px;">Plastic Bread Crates (Bakeshop)</td>
              <td style="padding: 10px 8px; text-align: right;">${report.DeliveredCrates || 0}</td>
              <td style="padding: 10px 8px; text-align: right;">${report.ReturnedCrates || 0}</td>
              <td style="padding: 10px 8px; text-align: right; color: ${(report.DamagedCrates || 0) > 0 ? '#DC2626' : '#059669'}; font-weight: bold;">${report.DamagedCrates || 0}</td>
            </tr>
          </tbody>
        </table>

        <div style="font-size: 12px; color: #64748B; margin-bottom: 30px;">
          <strong>Driver Remarks & Logs:</strong> ${report.Remarks || 'Smooth trip with on-time delivery across all stores.'}
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 40px; margin-top: 40px; text-align: center; font-size: 12px;">
          <div>
            <div style="border-bottom: 1px solid #333; height: 35px;"></div>
            <p style="margin-top: 6px; font-weight: bold;">Driver Signature</p>
          </div>
          <div>
            <div style="border-bottom: 1px solid #333; height: 35px;"></div>
            <p style="margin-top: 6px; font-weight: bold;">Fleet Supervisor / Dispatch Officer</p>
          </div>
        </div>
      </div>
    `;
    this.print(html);
  }

  printSOA(soa) {
    const sales = Number(soa.TotalSales || 0).toLocaleString('en-US', { minimumFractionDigits: 2 });
    const vat = Number(soa.VatAmount || 0).toLocaleString('en-US', { minimumFractionDigits: 2 });
    const ewt = Number(soa.WithholdingTax || 0).toLocaleString('en-US', { minimumFractionDigits: 2 });
    const totalDue = Number(soa.TotalAmountDue || soa.TotalSales || 0).toLocaleString('en-US', { minimumFractionDigits: 2 });

    const html = `
      <div style="font-family: Arial, sans-serif; max-width: 850px; margin: 0 auto; padding: 28px; border: 2px solid #B4182C;">
        <div style="display: flex; justify-content: space-between; border-bottom: 3px solid #B4182C; padding-bottom: 14px; margin-bottom: 18px;">
          <div>
            <h1 style="color: #B4182C; margin: 0; font-size: 24px; font-weight: bold;">M.J. BILLONES TRANSPORT SERVICE</h1>
            <p style="margin: 4px 0 0 0; font-size: 13px; color: #555;">Tarlac City, Central Luzon | Commercial Hauling Services</p>
          </div>
          <div style="text-align: right;">
            <div style="font-size: 18px; font-weight: bold; color: #1E293B;">STATEMENT OF ACCOUNT</div>
            <div style="font-size: 14px; font-weight: bold; color: #B4182C; margin-top: 4px;">${soa.SoaNumber || 'SOA-2026-0001'}</div>
          </div>
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px; font-size: 13px; margin-bottom: 24px;">
          <div>
            <p style="margin: 3px 0;"><strong>Client Company:</strong> ${soa.ClientName || 'RED RIBBON BAKESHOP INC.'}</p>
            <p style="margin: 3px 0;"><strong>Billing Period:</strong> ${soa.PeriodFrom || '2026-10-01'} to ${soa.PeriodTo || '2026-10-31'}</p>
            <p style="margin: 3px 0;"><strong>Delivery Service Type:</strong> ${soa.DeliveryType || 'Standard Retail Stores'}</p>
          </div>
          <div style="text-align: right;">
            <p style="margin: 3px 0;"><strong>Statement Date:</strong> ${soa.IssueDate || '2026-10-25'}</p>
            <p style="margin: 3px 0;"><strong>Status:</strong> <span style="font-weight: bold; color: #B4182C;">OFFICIALLY ISSUED</span></p>
          </div>
        </div>

        <table style="width: 100%; border-collapse: collapse; font-size: 13px; margin-bottom: 20px;">
          <thead>
            <tr style="background: #F8FAFC; border-top: 1px solid #CBD5E1; border-bottom: 2px solid #94A3B8;">
              <th style="padding: 10px; text-align: left;">Item Description / Coverage</th>
              <th style="padding: 10px; text-align: right;">Amount (PHP)</th>
            </tr>
          </thead>
          <tbody>
            <tr style="border-bottom: 1px solid #E2E8F0;">
              <td style="padding: 10px;">Total Freight Hauling Charges (Consolidated Dispatches)</td>
              <td style="padding: 10px; text-align: right;">PHP ${sales}</td>
            </tr>
            <tr style="border-bottom: 1px solid #E2E8F0;">
              <td style="padding: 10px;">Value Added Tax (12% VAT)</td>
              <td style="padding: 10px; text-align: right;">PHP ${vat}</td>
            </tr>
            <tr style="border-bottom: 1px solid #E2E8F0; color: #DC2626;">
              <td style="padding: 10px;">Less: Expanded Withholding Tax (2% EWT)</td>
              <td style="padding: 10px; text-align: right;">- PHP ${ewt}</td>
            </tr>
            <tr style="background: #FDF2F4; font-size: 16px; font-weight: bold; border-top: 2px solid #B4182C;">
              <td style="padding: 12px 10px; color: #6B0F1B;">TOTAL AMOUNT DUE:</td>
              <td style="padding: 12px 10px; text-align: right; color: #B4182C;">PHP ${totalDue}</td>
            </tr>
          </tbody>
        </table>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 40px; margin-top: 40px; text-align: center; font-size: 12px;">
          <div>
            <div style="border-bottom: 1px solid #333; height: 35px;"></div>
            <p style="margin-top: 6px; font-weight: bold;">Prepared by: Marie Ann J. Billones (Authorized Signatory)</p>
          </div>
          <div>
            <div style="border-bottom: 1px solid #333; height: 35px;"></div>
            <p style="margin-top: 6px; font-weight: bold;">Received and Accepted by Client</p>
          </div>
        </div>
      </div>
    `;
    this.print(html);
  }

  print(content) {
    this.printContainer.innerHTML = content;
    window.print();
  }
}

window.docPrinter = new DocumentPrinter();
