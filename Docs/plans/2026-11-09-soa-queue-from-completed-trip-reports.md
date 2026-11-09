# Trip Report → SOA → Service Invoice → Collection Receipt (build plan)

Date: 2026-11-09. **Status: PLAN ONLY, no code written.** Builds on `2026-11-09-statement-first-workflow.md` (decision D1 = A, D2 = A, D3 = yes), which this message confirms.
Strict rules from you, kept everywhere: **Service Invoice number is typed by the user. Collection Receipt number is typed by the user. Neither is ever auto-generated.**

## 0. Two conflicts with the current code — need your answer first (Q1, Q2)

| # | Your spec | What the code does today | Recommendation |
|---|---|---|---|
| **Q1** | `Total Sales + Fuel Surcharge = Total Amount (VAT Inclusive)` | `SoaTotals.From` (Functions/SoaTotals.cs): Total due = Net of VAT + Fuel + VAT **− 2% withholding**. The statement sheet total (327,165.07) includes that deduction. | Follow your formula: **Total Amount = Total Sales (VAT inc.) + Fuel**. This is the amount copied (locked) to the invoice. Show VAT and the 2% as **information lines** on the SOA ("VAT included", "Withholding the client may deduct"); the client deducts it at payment. Existing check 266,500.00 / 65,424.00 → 327,165.07 must then be updated. |
| **Q2** | Link trip reports with a `SoaId` | `TripReports` has **no SoaId**. A trip is "on a statement" through `StatementOfAccountItems` (`SoaId`, `DeliveryId`, `TripReportNumber`). `GetBilledDeliveryIds` already reads it. | Do **not** add a second copy of the link. "HasSoa" = a non-cancelled statement item exists for that trip report. No schema change, no way for the two to disagree. |

Also confirm: **SOA number** stays auto-generated (`GenerateNextSoaNumber`). You only ruled out auto-numbering for invoice and receipt.

### Q1 RESOLVED (user sent the statement sheet, Nov 9): keep the sheet formula, do NOT drop the withholding
Sheet: Total Sales + Fuel (VAT inclusive) 266,500.00 and 65,424.00 → Less VAT 28,553.57 → Net of VAT 237,946.43 → Fuel 65,424.00 → Add VAT 28,553.57 → Less Withholding 4,758.93 → **TOTAL AMOUNT DUE 327,165.07**. This is exactly `SoaTotals.From`, so **`SoaTotals` and its check (266,500.00 / 65,424.00 → 327,165.07) do not change.**
- The SOA form shows the sheet's rows; the line "Total Sales + Fuel Surcharge (VAT Inclusive)" is `sales | fuel` as on the sheet, and the final figure is **Total Amount Due** per the sheet.
- Each trip's service invoice is created with `TotalAmount = Amount + Fuel` and takes its **share of the withholding** (`SoaTotals.Share`), so the invoices' **net amounts add up to the SOA's Total Amount Due to the centavo**. The invoice shows the SOA figures locked; collection collects the net, as today.
- Wherever sections 3.3 to 3.5 and the Q1 row in section 7 say "Total = sales + fuel, no deduction", read: **sheet formula, as above.**

## 1. What exists (verified)

- Tables: `TripReports(Id, TripReportNumber, DeliveryId, Status Draft/…, IsVoid…)`, `StatementsOfAccount(SoaNumber, ClientName, TotalSalesVatInc, FuelSurchargeTotal, WithholdingTax, TotalAmount, Status, SoaType…)`, `StatementOfAccountItems(SoaId, DeliveryId, SpecialTripId, TripReportNumber, Amount, Fuel…)`, `Invoices(InvoiceNumber UNIQUE, SoaId, DeliveryId, TotalAmount, FuelSurcharge, NetAmount, Status)`, `CollectionReceipts(ReceiptNumber UNIQUE, InvoiceId, AmountPaid…)`.
- `SaveStatementOfAccount` already runs in one `SqliteTransaction` and already refuses mixed normal/special lines.
- **Blocking today** (this is what we change): `LinkTripInvoicesToSoa` (Database/MJdatabase.SoaInvoices.cs:46) throws "no service invoice yet" — the SOA cannot be saved without invoices, and it reads amounts **from** the invoice.
- "Waiting for invoice" list = `GetTripsWaitingForInvoice` (Database/MJdatabase.Waiting.cs:57); it **hides** trips on a statement. Billing refuses a trip on a statement.
- Invoice number and receipt number are already typed and unique (`InputGuard.ReferenceOnly`, UNIQUE columns). We keep that and add checks that no code path fills them.

## 2. Target flow

```
Trip report = Completed ─▶ appears in SOA tab queue (no statement yet)
  ─▶ tick trip reports of ONE client ─▶ [+ New Statement]
  ─▶ SOA form: client auto-filled & read-only; trips read-only; edit ONLY Amount + Fuel Surcharge
  ─▶ Save (one transaction) ─▶ trips leave the queue ─▶ jump to Service Invoice list
  ─▶ Service Invoice (waits there): user TYPES invoice no.; amount + client pulled from SOA, LOCKED
  ─▶ Issue ─▶ record appears in Collection list ─▶ user TYPES receipt no. ─▶ amount locked to invoice
```

## 3. Step by step

### 3.1 Queue in the Statements of Account tab (spec 1)
Remove any wording/path that says "create from Service Invoices" in `StatementOfAccountView.Invoices.cs` / `.Guide.cs`. New panel "Completed trip reports waiting for a statement", same style as `WaitingListPanel`.

New `MJdatabase.GetTripReportsWaitingForStatement()` (replaces the statement part of `GetTripsWaitingForInvoice`; keep that method for the direct route):

```sql
SELECT tr.Id, tr.TripReportNumber, tr.DeliveryId, d.DispatchNumber, d.ClientName,
       tr.ReportDate, d.Destination, tr.DeliveredCrates
FROM TripReports tr
JOIN Deliveries d ON d.Id = tr.DeliveryId
WHERE COALESCE(tr.Status,'')   <> 'Draft'  COLLATE NOCASE      -- Completed (finished) only
  AND COALESCE(tr.IsVoid,0)     = 0
  AND COALESCE(d.Status,'')     = 'Completed' COLLATE NOCASE
  AND NOT EXISTS (                                              -- "SoaId IS NULL / HasSoa = 0"
        SELECT 1 FROM StatementOfAccountItems i
        JOIN StatementsOfAccount s ON s.Id = i.SoaId
        WHERE i.DeliveryId = tr.DeliveryId
          AND COALESCE(s.Status,'') <> 'Cancelled' COLLATE NOCASE)
  AND NOT EXISTS (                                              -- already invoiced directly
        SELECT 1 FROM Invoices v
        WHERE COALESCE(v.DeliveryId,0) = tr.DeliveryId
          AND COALESCE(v.Status,'') <> 'Cancelled' COLLATE NOCASE)
ORDER BY d.ClientName, tr.ReportDate;
```
Automatic appearance when a report becomes Completed: the list is a query, so no trigger is needed. Refresh points: tab `Shown`, after Trip Report save (`MainForm` event the hub already uses for the waiting badges), and the tab badge count.

### 3.2 Multi-select, same client only, `+ New Statement` (spec 2)
Grid: checkbox column, group by client. Validation (one function, used by the button and by the save, so they can't disagree):

```csharp
// Functions/SoaQueue.cs
public static string? WhySelectionNotAllowed(IReadOnlyList<WaitingReportRow> picked)
{
    if (picked.Count == 0) return "Tick at least one completed trip report.";
    var clients = picked.Select(p => ClientKey(p.ClientName)).Distinct().ToList();
    if (clients.Count > 1)
        return "Select one client at a time. You ticked: " + string.Join(", ", picked.Select(p => p.ClientName).Distinct()) + ".";
    return null;
}
// ClientKey: trim, collapse spaces, ignore case. (Typed names like "BUTTERWORLD" vs "butter world" are still
// different clients; see reality-check A1. Recommended: match on Clients.Id; until then ClientKey + a "merge duplicates" tool.)
```
Handler:
```csharp
private void btnNewStatement_Click(object? s, EventArgs e)
{
    var picked = queueGrid.Rows.Cast<DataGridViewRow>()
        .Where(r => r.Cells["colPick"].Value is true).Select(r => (WaitingReportRow)r.Tag!).ToList();
    var why = SoaQueue.WhySelectionNotAllowed(picked);
    if (why != null) { Toast.Warn(why); return; }          // no error popup (project rule)
    OpenNewStatement(picked);                              // client, address, TIN auto-filled from Clients; lines = picked trip reports
}
```
Selecting a second client's row can also untick/refuse at click time (`CellContentClick`) so the mistake is caught early.

### 3.3 SOA form rules and calculation (spec 3)
- Read-only: client, address, TIN, trip report no., dispatch no., date, route. **Editable: `Amount` (Total Sales, VAT inc.) per trip line and `Fuel Surcharge` per trip line only.** Amount pre-fills from the Rate Matrix when it is on (existing code), fuel starts at 0 (typed).
- Digits-only guards (`InputGuards.DigitsOnlyColumns` / money box) on those two columns; `Amount > 0`, `Fuel >= 0`.
- Live total line, exactly in your format:
  `Total Sales ₱{sales:N2}  +  Fuel Surcharge ₱{fuel:N2}  =  Total Amount (VAT Inclusive) ₱{sales+fuel:N2}`
  ```csharp
  decimal TotalAmount(decimal sales, decimal fuel) => Math.Round(sales, 2, MidpointRounding.AwayFromZero) + Math.Round(fuel, 2, MidpointRounding.AwayFromZero);
  ```
  (This replaces `SoaTotals.Due`; `SoaTotals` is kept for the info lines VAT / net of VAT. See Q1.)
- `LinkTripInvoicesToSoa` is no longer called on this route. New `LinkTripReportsToSoa(conn, tx, soa, soaId, items)` inside the **existing** `SaveStatementOfAccount` transaction:
  1. Re-check, inside the transaction, each trip report is still Completed, not void, not already on a live statement, no live invoice, and all lines are one client (same query as 3.1 filtered to the picked ids). Any failure → `InvalidOperationException` with the reasons → rollback, "Nothing was saved."
  2. Insert the statement and its `StatementOfAccountItems` (existing code). That insert **is** the link; once it commits the trips leave the queue (3.1 query).
  3. For each line insert an invoice row in state **`Pending`** with `SoaId`, `DeliveryId`, `ClientName`, `TotalAmount = Amount + Fuel`, `FuelSurcharge`, `NetAmount = TotalAmount`, `Deductions = 0`, and **`InvoiceNumber` empty** (see 3.4 about the UNIQUE column).
  4. `tx.Commit()`; on any exception `tx.Rollback()`.
- After save, `MainForm.OpenTab<BillingView>("Billing")` filtered to this SOA ("Go to Service Invoice list").

### 3.4 Service Invoice list and the amount lock (spec 4)
- **Schema point (needs your OK):** `Invoices.InvoiceNumber` is `UNIQUE NOT NULL`, and you want the number typed later. So the SOA must not insert an invoice row with a made-up number. Two ways:
  - **A (recommended): no invoice row at save.** The Service Invoice list shows "SOAs waiting for invoice numbers" from `StatementOfAccountItems` where no live invoice exists for the trip. The row is created when the user types the number and presses Issue. No schema change, no placeholder numbers.
  - B: add `Pending` rows with a NULL number (needs a table rebuild in SQLite). Not recommended.
  With A, **replace step 3 of 3.3** by "nothing"; the SOA save only writes the statement and its lines.
- Queue query:
```sql
SELECT s.Id AS SoaId, s.SoaNumber, s.ClientName, i.DeliveryId, i.TripReportNumber,
       i.Amount, COALESCE(i.Fuel,0) AS Fuel, i.Amount + COALESCE(i.Fuel,0) AS TotalAmount
FROM StatementOfAccountItems i
JOIN StatementsOfAccount s ON s.Id = i.SoaId AND COALESCE(s.Status,'') <> 'Cancelled' COLLATE NOCASE
WHERE NOT EXISTS (SELECT 1 FROM Invoices v
                  WHERE COALESCE(v.DeliveryId,0) = i.DeliveryId
                    AND COALESCE(v.Status,'') <> 'Cancelled' COLLATE NOCASE)
ORDER BY s.SoaNumber, i.TripNumber;
```
- Billing form (`BillingView`) when the picked trip is on a statement: client, trip, **Amount and Fuel filled from the SOA line and `ReadOnly = true` / `Enabled = false`**; only the **invoice number (typed)** and the issue date are editable. Remove the "already on a statement" block at `BillingView.cs:33-36`.
- Save validation (server side too, not only the UI):
```csharp
if (string.IsNullOrWhiteSpace(no)) return "Type the service invoice number from the booklet.";
if (!Regex.IsMatch(no, @"^[A-Za-z0-9\-]{1,30}$")) return "Invoice number: letters, digits and dashes only.";
if (InvoiceNumberExists(no)) return $"Invoice number {no} is already used.";          // UNIQUE, burned numbers stay burned
if (soaLine != null && amount != soaLine.Amount + soaLine.Fuel) return "The invoice amount must equal the statement's amount.";
```
  The last line is the real lock: even if a control is unlocked by mistake, the save recomputes from the SOA and ignores the typed amount.
- Withholding is **not** shared onto invoices any more (Q1); `SoaTotals.Share` stays only for old statements.

### 3.5 Invoice → Collection Receipt (spec 5)
- Issued invoices (status Unpaid) already show in Collections via `GetPayTargets` (Database/MJdatabase.OneReceipt.cs:112). Keep it. The amount field is already fixed to the invoice amount (full payment only, no partials, per reality-check H2; unchanged).
- **Receipt number: typed.** Existing 4–8 digits, never reused. Add a check (Verify) that no code path writes `ReceiptNumber` except from the textbox.
- Guard (plan C5): a statement is collectable only when every trip on it has its invoice; otherwise "2 of 5 trips still need an invoice."
- After issuing the last invoice of a SOA, `MainForm` offers "Go to Collections" (not forced).

## 3.6 Saved data: bring existing records in line with the new flow (added on request)

**Nothing is changed in the database until you approve this section.** Money on issued or paid documents is never rewritten.

What the new flow needs from old data, and what I do about each:

| Old record | Problem under the new flow | Fix |
|---|---|---|
| Statement made invoice-first, its trips have invoices (`Invoices.SoaId` set) | None for the flow: trip is on a statement and has an invoice. Total on the SOA includes the 2% deduction (old formula). | **Leave amounts as they are.** Mark them "legacy" in the view only (no column): shown by "SOA has its own invoices made before Nov 9". They open, print and collect as before. |
| Completed trip report, no statement, **has** a live direct invoice | Would wrongly show in the SOA queue. | Excluded by the queue query (3.1). No data change. |
| Completed trip report, no statement, no invoice | Belongs in the new queue. | No data change; the query lists it. |
| Statement with items but **no** invoice (can exist only if made by hand) | Shows in the invoice queue (3.4). | No data change. |
| `Invoices.SoaId` pointing at a missing or cancelled statement | Invoice looks "on a statement" but is not. | **Repair:** set `SoaId = 0`, `Deductions = 0`, `NetAmount = TotalAmount` only when nothing is paid on it. Listed first, applied after your OK. |
| Invoice on a statement whose client name differs from the statement's | Breaks the same-client rule. | **Report only.** You decide per case (merge the names, or take the trip off). |
| Statement `TotalAmount` not equal to the sum of its lines + fuel (old formula drift) | Would disagree with the new total line. | **Report only** for statements not yet paid. Paid ones are never touched. |
| Empty or auto-looking `InvoiceNumber` / `ReceiptNumber` (system generated) | Breaks your strict rule. | **Report only.** Real numbers can't be guessed; you type the right ones through the normal edit. |

How it is done (safe order):
1. **Back up** the database file (copy with a dated name) before anything.
2. **Dry run, read only:** a Verify check / admin report that prints, per table, the rows in each category above with counts and ids. No writes.
3. You read the report. I apply only the **Repair** row, in **one `SqliteTransaction`**, writing an audit-trail line per row (`LogAuditTrail`, existing).
4. Run all checks on the repaired **copy** first, then on the live file.
5. No deletes, no column dropped, nothing to PAID documents.

If by "match the patch" you mean also **rewriting old statement totals to Sales + Fuel** (Q1), say so: I would do it only for unpaid, un-invoiced statements, because rewriting a total the client already received on paper is an accounting change, not a flow fix. Add as **Q6**.

## 4. Files to change

| Area | Files |
|---|---|
| Queue + validation | `Database/MJdatabase.Waiting.cs` (new `GetTripReportsWaitingForStatement`), new `Functions/SoaQueue.cs`, `Views/StatementOfAccountView.Dispatches.cs` / `.Invoices.cs` / `.Guide.cs` (queue panel, `+ New Statement`, remove "create from Service Invoice") |
| SOA form and totals | `Views/StatementOfAccountView.cs` / `.Summary.cs`, `Functions/SoaTotals.cs` (new total = sales + fuel; info lines), `Database/MJdatabase.cs` `SaveStatementOfAccount`, `Database/MJdatabase.SoaInvoices.cs` (new `LinkTripReportsToSoa`; old link kept for old statements) |
| Invoice list and lock | `Views/BillingView.cs`, `BillingView.Guide.cs`, waiting list panel |
| Collections | `Database/MJdatabase.OneReceipt.cs`, `Views/CollectionView*.cs` (guard) |
| Navigation | `MainForm.cs` (after-save jumps), `Views/FinanceHubView.cs` (tab order: Statement of Account first, default tab, badges) |
| Checks | `Verify/Checks.*.cs` |

No new database column or table (with 3.4 option A and Q2).

## 5. Checks to add (Verify, run on a copy of the live database)

1. Completed trip report with no statement appears in the SOA queue; Draft, void, and already-statemented ones do not; the moment a report is completed it is listed.
2. Two clients ticked → refused with both names; one client → opens the form with client auto-filled.
3. Only Amount and Fuel are editable in the form; total line text equals `Total Sales + Fuel Surcharge = Total Amount (VAT Inclusive)` and the numbers add up to the centavo.
4. Save inside one transaction: force a failure on the 2nd line → nothing saved, trips still in the queue.
5. After save the trips leave the queue and show in the invoice queue; the invoice number is empty and **not** auto-filled.
6. Invoice amount is read-only; tampering the amount in code is rejected by the server check; duplicate or empty invoice number rejected.
7. Issued invoice appears in Collections; receipt number empty by default, typed, 4–8 digits, never reused; amount equals the invoice.
8. Old statements (made invoice-first) still open, print and collect unchanged.
9. Static check: nothing but the textbox writes `InvoiceNumber` or `ReceiptNumber`.
10. No error popups on any of these screens.

## 6. Build order (each step checked before the next)

1. Queue query + panel + `+ New Statement` with same-client check (nothing saved yet).
2. SOA form: locked fields, two editable columns, total line, save without invoices, one transaction.
3. Invoice queue from SOA lines; locked amount; typed invoice number; remove "on a statement" block.
4. Collection guard and typed receipt number check; navigation jumps; tab order.
5. Verify checks on a copy of the live data; manual page. Back up the database before the first run.

## 7. Decisions I need

| # | Question | My recommendation |
|---|---|---|
| Q1 | Total = Sales + Fuel (no 2% deducted on the SOA; the client deducts at payment)? | Yes, as you wrote. Existing check totals change. |
| Q2 | Use the existing statement-item link instead of a new `SoaId` on trip reports? | Yes. |
| Q3 | Invoice rows created only when the user types the number (option A)? | Yes. |
| Q4 | SOA number stays automatic? | Yes. |
| Q5 | Same-client match: now by typed name (case-insensitive); Clients.Id link later? | Yes. But BUTTERWORLD / butter world will block until merged. |

Reply "use recommended" and I start at build step 1.
