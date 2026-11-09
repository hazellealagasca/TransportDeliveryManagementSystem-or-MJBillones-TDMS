# New Invoice picks a Statement of Account (SOA) — plan (Nov 10, 2026)

> **Status: PLAN ONLY. No code has been written.** Section 5 has five decisions, each with a recommendation. Answer "use recommended" and I will build in the order of section 6.

## 1. What you asked for

In **Billing Invoices → New invoice**, replace the **Completed Delivery Trip** box with a **Statement of Account** box. After you pick the SOA, the form fills in by itself: client, the trips, and the **amounts as the SOA computed them**. You confirmed: **one invoice for the whole SOA** (not one per trip).

What the form would show after picking an SOA (example: freight ₱10,000, fuel ₱500):

| Field | Filled from the SOA | Example |
|---|---|---|
| Billed client | SOA client | BUTTERWORLD |
| Trips being billed | The SOA's trip lines (read only) | 5 trips |
| Amount / freight tariff | SOA Total Sales (VAT inclusive) | ₱10,000.00 |
| Fuel surcharge | SOA fuel total | ₱500.00 |
| Withholding tax (2%) | SOA withholding (see `SoaTotals`) | ₱178.57 |
| **Net amount due** | SOA **Total Amount Due** | **₱10,321.43** |
| Service Invoice No. | **Typed** from the printed booklet (the only thing typed) | 2451 |

The numbers come from the one existing formula (`Functions/SoaTotals.cs`), so the invoice, the SOA, the print and the receipt cannot disagree.

## 2. What I found in the code (this is why it is a plan, not a quick edit)

1. **The database already allows it.** An invoice with `SoaId` set and `DeliveryId = 0` and `SpecialTripId = 0` is "a statement's own invoice". The code calls it a **legacy statement** (`MJdatabase.SoaInvoices.cs:179-194` `SoaHasOwnInvoice`; `StatementOfAccountView.Invoices.cs:18-33` `IsLegacySoa`). So **no new table or column is needed**.
2. **But that is the model the company moved away from on Nov 6.** Since then the rule everywhere is *"a statement groups the service invoices of its trips, one per trip"* (`StatementOfAccountView.Invoices.cs:10-12`). What you ask for is, in effect, **bringing the old model back as the main one**. That is allowed, but it reverses a decision, so it must be a conscious one (D1).
3. **Most of the billing code assumes one invoice = one trip.** Roughly 141 database statements across 30 files touch `Invoices`. These pieces would need to learn "an invoice may belong to a whole SOA":

| Piece | Today | Where |
|---|---|---|
| New invoice form | Needs a picked trip, and takes Amount/Fuel from that trip's SOA line, locked | `Views/BillingView.cs:27-54`, `BillingView.Guide.cs:272` |
| Issue Invoice from a SOA | Opens one form per trip that has no invoice | `BillingView.cs:80-87`, `StatementDesk.cs:58` |
| Withholding | Split over the trips' invoices in proportion (`SoaTotals.Share`), centavo-exact | `SoaFirst.cs:118-178`, `SoaInvoices.cs:299-320` |
| "Trips still without an invoice" guard | Blocks collecting a SOA | `SoaFirst.cs:181` `GetSoaTripsWithoutInvoice` |
| Waiting-for-invoice list | Lists trips, not statements | `MJdatabase.Waiting.cs`, `Functions/WaitingListPanel.cs` |
| Double-billing finder | Flags "an own invoice + an old statement" as a problem | `MJdatabase.OrderChain.cs:132` `GetTripsBilledMoreThanOnce` |
| Cancel, receipts, prints, Finance History, search | Keyed on the invoice's trip | `NumberCancel.cs`, `OneReceipt.cs`, `Printer.cs`, `FinanceStatementHistoryView.cs` |

Good news: collections already pay **a whole SOA with one receipt**, so a SOA-level invoice fits the receipt better than per-trip invoices do.

## 3. Target design (recommended)

```
STATEMENT ROUTE (new):  Trip reports ─▶ SOA ─▶ ONE INVOICE for the SOA ─▶ COLLECTION RECEIPT
DIRECT ROUTE (as today): Trip report ─▶ invoice per trip ─────────────────▶ COLLECTION RECEIPT
```

### 3.1 New invoice form
- The first box becomes **"Statement of Account *"**. It lists only SOAs that are **saved, not cancelled, and have no live invoice yet**, newest first, each shown as `SOA-0012 · BUTTERWORLD · ₱10,321.43 · 5 trips`.
- Picking one fills section 2 (Amounts) and the "trip being billed" card with the SOA's trips, **all locked**. You type only the **SI No.** and (as today) the issue date.
- Amount = SOA Total Sales + fuel. Withholding = SOA withholding. Net = SOA Total Amount Due. Nothing is typed, nothing can disagree.
- The **Completed Delivery Trip** box stays for the **direct route**, as a switch at the top of the form: **"Bill a Statement of Account"** (default) or **"Bill one trip directly"**. The direct route is untouched, so Trip reports → "Issue Invoice (n selected)" keeps working.

### 3.2 Rules
- **One live invoice per SOA.** A second one is refused ("SOA-0012 already has SI 2451").
- **A SOA that has its invoice is locked for trips and amounts.** To change an amount: cancel the invoice, fix the SOA, issue again (the existing "typing mistake" / "void" cancel rules and the SI-number rules stay).
- **A trip on a SOA cannot also get its own invoice** (and the reverse): one trip is billed once. The "billed more than once" finder keeps checking.
- **Collecting** is the SOA's receipt as today. The "trips still need an invoice" guard becomes "this SOA has no invoice yet".
- **SOA print and invoice print** show the same figures; the invoice shows "per SOA-0012" and its SI No. on the SOA.

### 3.3 Old data
- Statements already saved **with per-trip invoices** keep working exactly as now (open, print, collect). Nothing is converted.
- The old "legacy" statements with their own invoice are the same shape as the new ones, so they are simply treated as **normal** from now on instead of "old rules".

## 4. Risks
- **Money and tax.** A single invoice for many trips changes what the BIR invoice shows. **Ask your accountant first:** "May one service invoice cover several trips on a statement of account, and may it be issued after the statement?" If no, the main route must stay per trip, and only the *picker* can be changed (see D1, option B).
- **Billing code is touched** in about 10 places (table in section 2). Mitigation: a feature switch, no deleted columns, and a full check run on a **copy** of the live database before anything is committed.
- **The invoice's "Trip Report #" column** (one trip today) must show "5 trips" or the SOA number.

## 5. Decisions needed

**D1 — Replace per-trip invoices on the statement route, or add the SOA option next to them?**
- **A (recommended): add.** New invoice offers "Bill a Statement of Account" first and "one trip directly" second. Per-trip invoices from a SOA ("Issue Invoice" opening one form per trip) are kept for SOAs that already use them, but new SOAs get one invoice. Safest.
- B: **Only the picker changes**: you pick the SOA, then pick one of its trips, and the trip's amounts fill in. Still one invoice per trip. Much smaller, but it is not "one invoice for the whole SOA".

**D2 — Which SOAs appear in the picker?** Recommended: saved, same-client, not cancelled, no live invoice, and **no trip already invoiced one by one**. Others are shown greyed with the reason.

**D3 — If the SOA's amount changes after the invoice is issued.** Recommended: **blocked**; cancel the invoice first. (The system already warns which printed/sent documents are out of date when an amount changes; that warning stays.)

**D4 — Safety switch.** Recommended: **Settings → Features → "Invoice from a Statement of Account"**, shipped **on** after testing, so the old form can come back instantly.

**D5 — Withholding on the invoice.** Recommended: the invoice carries the SOA's **single withholding figure** in the existing Deductions column, so Net = SOA Total Amount Due to the centavo. The per-trip split (`SoaTotals.Share`) is used only for the direct route and old statements.

## 6. Build order (each step checked before the next)

1. **Query and rules:** `GetSoasWaitingForInvoice`, "SOA already invoiced" rule, SOA-level invoice save with locked amounts (database only, no screen).
2. **New invoice form:** the SOA box, auto-fill, the "SOA / one trip" switch, the trip card, the checks list ("Pick a SOA", "Enter the SI No."), Issue invoice.
3. **Guards:** collect only an invoiced SOA, cancel flow, "billed more than once" finder, the invoice's Trip Report # display.
4. **Prints and lists:** invoice print and Billing list show "SOA-xxxx · n trips"; SOA shows its SI No.
5. **Switch, manual, and `Verify` checks**, then the whole check run on a copy of the live database.

## 7. Checks to add (`Verify`)

- Picking a SOA fills client, freight, fuel, withholding and Net; Net equals the SOA's Total Amount Due (₱10,000 + ₱500 → ₱10,321.43).
- Only the SI No. can be typed; any other amount edit is refused at save, not only on the screen.
- A SOA cannot get two live invoices; a cancelled invoice frees it again.
- A trip on an invoiced SOA cannot be invoiced one by one; a directly invoiced trip cannot be put on a SOA.
- Collecting the SOA pays the invoice in full and marks the SOA and invoice Paid.
- Existing statements with per-trip invoices still open, print and collect unchanged.
- The direct route (trip report → invoice → receipt) is unchanged.
- No error popups on these screens (project rule).

## 8. What I need from you

Reply **"use recommended"** to take D1A, D2, D3 blocked, D4 switch on after testing, D5 single withholding figure. The answer that changes the size of the work is **D1** (A = full one-invoice-per-SOA, B = only a SOA picker with one invoice per trip). Please also ask your accountant the question in section 4 before we build.
