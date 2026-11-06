# Finance Module Plan — Billing Invoices First

## 1. Why Billing goes first
Finance work starts when a Trip Report is completed and the trip is ready to be invoiced.
The tabs should follow the order the work happens:

| # | Tab (key) | What it does in the flow |
|---|-----------|--------------------------|
| 1 | 🧾 Billing Invoices (`Billing`) | One service invoice per completed trip. Entry from Trip Reports "Issue Invoice". |
| 2 | 📑 Statement of Account (`StatementOfAccount`) | Groups a client's invoiced trips into one statement. |
| 3 | 💵 Collections & Receipts (`Collections`) | One receipt pays the full amount due on an invoice or statement. |
| 4 | 📜 Finance History (`History`) | Settled records. |

Before: SOA, Billing, Collections, History (`Views/FinanceHubView.cs:36-50`).

## 2. What I found (current behavior)
- **Tab order and default are SOA-first** in five places:
  - `FinanceHubView` constructor default, `defaultTab = "StatementOfAccount"` (line 19)
  - `SetupTabs()` order (lines 36-50)
  - `MainForm.NavigateToFinance(string tab = "StatementOfAccount")` (`MainForm.cs:1318`)
  - `MainForm.Shell.cs:429`, the `tab ?? "StatementOfAccount"` fallback
  - sidebar button `btnNavFinance_Click` (`MainForm.cs:1164`), which uses the constructor default
- **Trip-driven entries already go to Billing**: Trip Reports "Issue Invoice" (`OpenBillingFromTripReports`, `MainForm.cs:1229`), `CreateInvoiceForDelivery` and `OnCreateInvoiceForward` all open Billing. Only the plain Finance button lands on SOA.
- **Dashboard "delivered trips not billed yet"** (`DashboardView.cs:630`) sends users to Statement of Account. That skips the invoice step, so it should point at Billing.
- **One way to bill a trip (corrected).** Since the Nov 6 workflow, a statement cannot bill a trip on its own. `LinkTripInvoicesToSoa` (`MJdatabase.SoaInvoices.cs:46`) refuses any trip that has no service invoice ("Make it in Billing"). So Billing is already step 1 in the data, and the tab order should say so. Statement of Account only groups existing invoices.
- **After the SOA is saved**, `ProceedToCollection(soaId)` opens Collections with the receipt pre-filled (`StatementOfAccountView.cs:1536`). This flow is correct for the new order and is not changed.
- **Permissions**: `ApplyAccess()` and `AllowedTab()` pick the first tab the account can open. With Billing first, an account without Billing access falls through to SOA. That is correct, but it needs a check.
- **Verification lists** in `Verify/Checks.Screens.cs:251,316` list Finance tabs in the old order.

## 3. Tasks
- [ ] **T1 — Reorder tabs.** In `SetupTabs()` register Billing, StatementOfAccount, Collections, History. Keep the keys unchanged so permissions and saved state still work.
- [ ] **T2 — Make Billing the default.** Change the default in `FinanceHubView(...)`, `NavigateToFinance(...)` and the `MainForm.Shell.cs:429` fallback to `"Billing"`.
- [ ] **T3 — Fix the Dashboard action.** The unbilled-trips item (`DashboardView.cs:630`) is a dead end today: it opens SOA, but SOA refuses trips with no invoice. Open Billing instead and change the text from "put them on a statement of account" to "make their service invoices".
- [ ] **T4 — Notifications and links.** Check `MainForm.Notifications.cs:86` and the Client Management / Global Search links. Keep the ones that mean SOA specifically, and re-point the ones that mean "bill this".
- [ ] **T5 — Close the duplicate gaps.** Do items B1-B4 in section 7, with a `Verify/` check for each.
- [ ] **T6 — Guides and wording.** Update `BillingView.Guide.cs`, `StatementOfAccountView.Guide.cs`, `CollectionView.Guide.cs` and `Functions/PaymentGuide.cs` so the "step 1, 2, 3" text follows the new order.
- [ ] **T7 — Verify.** Update the tab order in `Checks.Screens.cs`. Run the Verify project. Check by hand: sidebar Finance opens Billing; Trip Reports "Issue Invoice" still opens Billing with the trip; an account without Billing access opens SOA; the theme change returns to the same tab (`CurrentTabKey`).

## 4. Out of scope
- No database or schema changes.
- No change to how invoices, statements or receipts are calculated.
- Tab keys and permission names stay the same.

## 5. Risks
- Users who are used to SOA opening first. Mitigation: the tab is still one click away.
- The Dashboard and notification links are the easiest to miss. T4 and T7 cover them.

## 6. Open questions
1. Should Finance History stay last, or move next to Collections in the sidebar quick links (`MainForm.cs:418`)?

## 7. Bugs and gaps found (duplicates and double booking)
What already works: a trip can't go on two statements (`LinkTripInvoicesToSoa` refuses an invoice already on another one); a statement can't be paid twice or after it is cancelled; an invoice with a receipt can't get another (`SaveOneReceipt` re-checks inside a transaction); invoice and receipt numbers are checked for repeats, ignoring case and spaces.

| # | Problem | Where | Fix |
|---|---------|-------|-----|
| B1 | **A cancelled invoice blocks re-billing its trip.** The "one invoice per trip" check does not skip cancelled invoices, but `SaveInvoice` says "start a new invoice for this trip". The user can never do that, so the trip stays unbilled. | `BillingView.cs:641-647` and the same mistake in the checklist at `BillingView.Guide.cs:215` | Ignore invoices whose status is Cancelled in both places. |
| B2 | **The one-invoice-per-trip rule lives only in the screen.** `SaveInvoice` has no check, and no unique index exists on `Invoices(DeliveryId)`. Any other caller (the special-trip flow, a future import) can save a second live invoice for the same trip. | `MJdatabase.cs:11063`; indexes at `:2147` | Reject in `SaveInvoice` when a non-cancelled invoice for the same `DeliveryId` or `SpecialTripId` already exists. Optionally add a unique partial index. Check existing data for duplicates first. |
| B3 | **The SI number is checked only in the screen.** `SaveInvoice` does not call `CheckSiNumber`. The table's UNIQUE rule ignores case and spaces, so `si-001` and `SI-001 ` both pass at database level. | `MJdatabase.cs:11119`; `Numbers.cs:20` | Call `CheckSiNumber` inside `SaveInvoice` for new invoices. |
| B4 | **Dashboard "not billed yet" sends users to a screen that refuses them** (see T3). Its wording is stale too. | `DashboardView.cs:626-630` | T3. |
| B5 | **Stale wording.** Billing says "billed on statement… its invoice comes from that statement", and a code comment says "billed directly". Neither matches the Nov 6 flow, where the invoice always comes first. | `BillingView.cs:34`; `StatementOfAccountView.Dispatches.cs:664` | Reword to "already invoiced as SI …, on statement …". |
| B6 | **Number checks and inserts are separate steps.** Two people saving at once could take the same number. Low risk with one office PC, so just note it. | `Numbers.cs`, `SaveOneReceipt` | Only if more than one PC is used: do the check inside the same transaction. |

Section 2's "Two ways to bill" point was wrong and has been replaced. The Open question about direct billing is closed.

## 8. Deeper audit — more bugs (round 2)
Traced: invoice save/edit, invoice status, statement save/cancel, receipt save/void, trip-report status sync.

| # | Problem | Where | Fix |
|---|---------|-------|-----|
| B7 | **A paid invoice can be made bigger and then gets stuck.** Billing only checks that the new net is at least what was collected. Raise a paid invoice from 1,000 to 1,200: the status drops back to Unpaid, but the 1,000 receipt still exists. Collections then refuses it ("already has a receipt"), and no partial payments are allowed, so the invoice can never be settled. The statement's status is also not re-synced after an invoice edit. | `BillingView.Guide.cs:238-239`; `BillingView.cs:760`; `MJdatabase.cs:278`; `SaveInvoice` has no `SyncSoaStatus` call | Once an invoice has a receipt, lock its amounts (net must equal what was collected). Enforce it in `SaveInvoice`, not just the screen. To change a paid invoice, void the receipt first. |
| B8 | **The lock on invoices that are on a statement is also screen-only.** The screen refuses to change amounts or client (`BillingView.Guide.cs:230-234`), but `SaveInvoice` doesn't. A direct call could make the statement total differ from its invoices. That total is only compared at the moment a statement is saved. | `MJdatabase.cs:11063` | In `SaveInvoice`, if the invoice has a `SoaId` on a live statement, reject changes to amounts and client. |
| B9 | **Old "Generate SOA" dialog is hidden but still wired up.** The button is hidden (`BillingView.cs:127`), yet `GenerateSoaDialog` and `SaveStatementOfAccount(soa, List<int>)` still exist. That overload links invoices with a blind `UPDATE` — no check for a draft, paid, cancelled, other-client or already-on-another-statement invoice — so it could steal an invoice from another statement. It also makes SOA numbers in a different format. | `MJdatabase.cs:12048`; `GenerateSoaDialog.cs:394` | Delete the dialog, the button and the overload (nothing else calls them). |
| B10 | **Receipts are always dated "now".** `SaveOneReceipt` sets `PaymentDate = DateTime.Now`, and the form has no date field. A check received yesterday or on Friday can't be recorded with its real date, which also skews the "this month" figures in Collections. | `MJdatabase.OneReceipt.cs:177`; `CollectionView.Summary.cs:38` | Add a payment-date field (default today, not in the future) and pass it through. |
| B11 | **SOA lines have no uniqueness rule.** `StatementOfAccountItems` has no unique key on (statement, trip). The save routine groups by trip, so a repeated line is caught by the total-mismatch check, but only by luck of the numbers. | `MJdatabase.cs:2061`; `SoaInvoices.cs:52` | Reject repeated trips explicitly with a clear message, and add a unique index if existing data allows. |

What I checked and found fine:
- Paying a statement twice, or after it is cancelled.
- A second receipt on an invoice that already has one.
- The same payment reference recorded twice (`CollectionView.OneReceipt.cs:171`).
- DR numbers already billed on another statement.
- Voiding a receipt re-syncs the invoice and the statement.
- Cancelling a statement frees its invoices, and cancelling an invoice that is on a live statement is refused.
- Cancelled invoices and voided receipts keep their numbers.

Not checked yet (needs the app running or more reading):
- Incident deductions changing after an invoice is issued.
- The special-trip invoice flow end to end.
- Finance History / settled views for rows shown twice.
- Printing (SOA and receipt) amounts against saved amounts.
- Behavior when two people use the database at the same time.

## 9. Updated task list
T1 reorder tabs · T2 Billing default · T3 Dashboard action · T4 links · T5 duplicate gaps B1-B3 and B8, B11 (with a `Verify/` check each) · T6 guides wording (B5) · T7 verify · **T8** lock paid invoices (B7) · **T9** remove the old Generate SOA dialog (B9) · **T10** payment date on receipts (B10).

## 10. End-to-end audit: trip report → service invoice → statement → receipt → history
Method: I read a copy of the live database (`C:\ProgramData\MJ Billones Transport\TDMS\mj_billones.db`, 17 dispatches, 8 trip reports, 7 invoices, 4 statements, 6 receipts, 4 special trips) and traced every trip through the chain, then read the code behind each hop. The database itself was not touched.

### 10.1 Root cause: two billing models live side by side
- **Old model:** a statement makes its own whole-statement invoice (`DeliveryId = 0`, `SoaId > 0`). Live examples: SI-1001 and SI-1002.
- **Nov 6 model:** one service invoice (SI) per trip (`DeliveryId` or `SpecialTripId` set), and a statement only groups SIs.
- Most lookups are keyed on `Invoices.DeliveryId`, so they cannot see old-model invoices. This is the main source of duplicates and wrong screens (C1-C3 below).

### 10.2 What the live data shows
| # | Finding | Evidence |
|---|---------|----------|
| D1 | **BCS0001 is billed twice and both were paid.** Once on statement SOA-2026-0002 (SI-1001, PHP 19,151.79, receipt 0002) and once as a lone invoice 0765 (PHP 27,000.00, receipt 0004). That is PHP 46,151.79 collected for one trip, and the two amounts disagree. | Invoices 3 and 4; Settled records shows them as two unrelated rows. |
| D2 | **Invoice 2451 (RED RIBBON, PHP 9,000, paid) sits on a BCS GROUP COMPANY dispatch** (`DSP-20260901-140348`). It is dated 2026-09-23, before the dispatch date 2026-10-21, and the trip report is dated 2026-09-04. The client, the dates and the receipt don't agree. | Invoice 1, delivery 3. |
| D3 | **One customer, two client records:** `BUTTERWORLD` and `butter world`. Trips BUT0001/BUT0002 and BW0001 are split across them, so they can never share a statement. The unique-name index ignores case and outer spaces but not the inner space. | Clients 17 and 19. |
| D4 | **9 of 16 completed trips have no trip report**, so they can't be invoiced: 3 Goldilocks trips from September, BCS0003-BCS0006, BUT0001, BUT0002. | Deliveries 4, 5, 6, 12, 14-17, 19. |
| D5 | **4 trips have a finished report and no invoice yet:** `8555`, `DSP-20260925-001600-3208`, `MB0001`, `BW0001`. These are the real "ready to invoice" list. | Trip reports 5-8. |
| D6 | **Two completed special trips have no invoice, not even a draft** (`ST-20261106-0002`, `-0003`), although the Dashboard says drafts exist for completed special trips. | SpecialTrips 3 and 4. |
| D7 | **Test and junk rows in live data:** SOA-2026-0001 (six blank lines, total sales −7.00), `SI-TEST-1003`, receipt `TEST-PAY-1`, `SI-647` (cancelled, PHP 0). All are cancelled or voided, so totals are unaffected. | |
| D8 | **Receipt 0001 is a CHECK with a blank reference.** Methods are spelled `CHECK` and `Cheque`, so any "by method" totals split them. | Receipts 1 and 6. |
| D9 | **EWT is stored as an invoice "Deduction"** (SI-1001: 348.21 = 2% of net-of-VAT). So "Deductions" means incident deductions on one invoice and withholding tax on another. | Invoices 3 and 5. |

What is consistent: every Paid invoice equals its live receipts to the centavo; no live receipt is attached to a cancelled invoice; no trip is on two live statements; statement status matches its invoices (SOA-0002 Paid, SOA-0003 Unpaid); trip report status matches billing (all three Invoiced reports have a live invoice path).

### 10.3 Hop by hop
**Hop 1 — Trip report → invoice**
- Works: Draft reports and incomplete dispatches are refused (`SaveInvoice`); "Issue Invoice" refuses mixed clients; the report flips to Invoiced and back when the invoice is cancelled.
- **C1:** `BillingView.cs:642` looks for an existing invoice by `DeliveryId` only, so an old-model statement invoice on the same trip is invisible to the save check. Only the screen checklist (`billOnSoa`) sees it. This is the likely route to D1. Same blind spot in the dispatch delete guard (`MJdatabase.cs:622`).
- **B1** (cancelled invoice blocks re-billing) also hits here.

**Hop 2 — Invoice → statement**
- Works: SI required first; a trip can't be on two statements; paid or draft SIs refused; client must match; statement total must equal its SIs.
- **C2:** for old statements, `SoaHasOwnInvoice` (`MJdatabase.cs:11851`) still creates or updates a whole-statement invoice on every edit. Old and new rules are mixed in one save routine.
- B8 (amount lock only in the screen) and B11 (no unique statement line) apply here.

**Hop 3 — Statement/invoice → receipt**
- Works: one receipt for the full amount, re-checked inside a transaction; a statement can't be paid twice; duplicate payment references refused; voiding re-syncs invoice and statement.
- B7 (paid invoice can be increased and gets stuck) and B10 (no payment date) apply here.
- **C3:** a payment date that fails to parse silently becomes today (`MJdatabase.cs:12407`).
- **C4:** `UpdateInvoicePaymentStatus` (`MJdatabase.cs:14552`) is a public method nothing calls. It can mark an invoice Paid with no receipt. Delete it.

**Hop 4 — Receipt → Finance History**
- Works: Settled records lists paid statements and paid lone invoices, newest first, and ignores voided receipts.
- **C5:** a settled row shows SOA, SIs, receipt and amount, but **not the trip**: no dispatch number, trip report number, DR numbers or trip date, and the search box can't find them. You can't start from an order and find its settlement, or the other way round.
- **C6:** nothing flags two settled rows that bill the same trip (D1).
- **C7:** default range is the last 3 months, so older settled records look missing.

**Hop 5 — "History of the completed order" (Order History and the printed Dossier)**
- **C8:** Order History finds a trip's invoice only through `Invoices.DeliveryId` (`TransactionIndex.cs:67`). For trips billed on an old statement (BCS0002 today) it shows "no invoice yet" and offers **Create billing invoice**, which invites a second invoice for a trip that is already billed.
- **C9:** it takes the newest invoice even if cancelled, so a trip whose only invoice was cancelled shows that invoice number and hides the Create button. The Dossier does the opposite and takes the oldest (`PrintForms.cs`, `FirstOrDefault`), also cancelled ones.
- **C10:** special trips get no invoice in Order History at all.
- **C11:** both stop at the invoice. Neither shows the statement number, receipt number, paid date or paid status. The "end-to-end" dossier ends at billing.

## 11. Added tasks
- [ ] **T11 — One lookup for "the live invoice of a trip".** Add `MJdatabase.GetLiveInvoiceForTrip(deliveryId, specialTripId)` returning the newest non-cancelled invoice, whether per-trip or an old whole-statement one (via the statement's lines). Use it in Billing's save check and checklist (B1, C1), the dispatch delete guard, Order History (C8-C10) and the Dossier. Test: a trip on an old statement is refused a second invoice and doesn't show "Create invoice".
- [ ] **T12 — Complete the history.** Order History and the Dossier show the full chain: trip report no. → SI → SOA → receipt no. → paid date and status, with links. Settled records gain dispatch, trip report and DR columns plus search and an "Open order" action; widen or label the default date range (C5, C7, C11).
- [ ] **T13 — Duplicate-trip warning.** Settled records and Billing flag any trip billed on more than one live invoice or statement (D1 can never go unnoticed again).
- [ ] **T14 — Remove dead and unsafe code.** `UpdateInvoicePaymentStatus` (C4), plus B9. Make a bad payment date an error, not "today" (C3).
- [ ] **T15 — Decide the old model.** Either convert old whole-statement invoices into per-trip SIs, or freeze old statements as read-only. Then remove the `SoaHasOwnInvoice` branch (C2).
- [ ] **T16 — Data clean-up (owner decides, after a backup).** D1: which amount is right (19,151.79 or 27,000.00), reverse the other receipt and cancel its invoice. D2: fix the client on invoice 2451 or the dispatch. D3: merge the two Butterworld records. D4/D5: file the missing trip reports and invoice the four ready trips. D6: create the special-trip invoices. D7: archive the test rows. D8: normalize "CHECK/Cheque" and add the missing reference.

## 12. Sample clean-up and complete test order (done Nov 7, 2026)
- **Backup before anything:** `C:\ProgramData\MJ Billones Transport\TDMS\Backups\before_complete_order_20261107_041034.db`.
- **Clean-up used the app's own cancel/void**, because the database refuses deletes on invoices, statements and receipts (triggers `trg_nodelete_*`, "kept for history"). 4 receipts voided, 2 statements and 5 invoices cancelled. They stay in history and no longer count. Operations data (schedules, dispatches, trip reports, crews, clients) was not touched.
- **Complete order created through the same data-layer calls the screens use:** `LS-20261107-041036-96569` → dispatch `BCS0007` (BCS GROUP COMPANY, truck NEG 1721, driver DEVELA CARLO, 2 aides, 2 stores, 100 crates) → completed → trip report `TRIP-20261107-041037-33391` → `SI-2001` PHP 19,500.00 → `SOA-2026-0005` → receipt `0006` (CHECK CHK-100245) → Finance History and Order History.
- **New bug B12:** cancelling a statement that has an old whole-statement invoice leaves its trip report marked **Invoiced** (BCS0002 stayed stuck), so the trip can't be invoiced again from Trip Reports. Fix: call `SyncTripReportForInvoice` for each invoice cancelled in the `StatementsOfAccount` branch of `SoftDelete` (`MJdatabase.Integrity.cs:162-167`). Add a Verify check.
- **B1 is now visible in the data:** cancelled invoice `2451` still has `DeliveryId` 3, so Billing will refuse to invoice dispatch `DSP-20260901-140348` again.
- Seven trips now show as ready to invoice (report Completed, no live invoice): `BCS0001`, `BCS0002`, `8555`, `DSP-20260925-001600-3208`, `MB0001`, `BW0001`, and `DSP-20260901-140348` (blocked by B1 until that bug is fixed).

## 13. Progress: Steps 1 and 2 done (Nov 7, 2026)
**Step 1 — Billing first (done)**
- Tabs now run Billing, Statement of Account, Collections, History (`FinanceHubView.cs`); the sidebar list matches (`MainForm.cs`).
- Billing is the default in `FinanceHubView`, `NavigateToFinance` and `MainForm.Shell.cs`. Links that mean a statement specifically (notifications, Client Management, Global Search, "Show statement") still open Statement of Account.
- Dashboard "not billed yet" opens Billing, with new wording (`DashboardView.cs`).
- `Verify/Checks.Screens.cs` tab lists reordered.

**Step 2 — Stop double billing (done)**
- New `Database/MJdatabase.TripBilling.cs`: `GetLiveInvoiceForTrip`, `GetLiveInvoicesByDelivery`, `GetLiveInvoicesBySpecialTrip`. They see a trip's own invoice **and** an old statement's whole-statement invoice, and ignore cancelled ones.
- `SaveInvoice` now enforces (for every caller): the SI number can't repeat (any case or spaces), one live invoice per dispatch, and an invoice on a live statement keeps its amounts and client. (B1, B2, B3, B8)
- Billing's save check, its checklist and the "prefill from trip" message use the lookup (B1, B5). A cancelled invoice no longer blocks re-billing its trip.
- Order History, the printed dossier and the dispatch delete guard use the lookup (C1, C2, C8-C10). Special trips now show their invoice in Order History.
- 9 new Verify checks in `Verify/Checks.TripBilling.cs`. All pass.

**B11 withdrawn:** a statement line is one store, so several lines share one trip (SOA-2026-0002 has two lines for one dispatch). "Unique trip per statement" would be wrong. The real guard is per invoice, and that exists.

**Still to do:** Step 3 (B7 lock paid invoices, B12 cancelled statement resets trip reports, B10 payment date, B9 remove the old Generate SOA dialog), Step 4 (full-chain history, duplicate-trip warning), guide wording (T6), Step 5 (decision on the old model).

## 14. Progress: Step 3 (Nov 7, 2026)
**Done**
- **B7 — paid invoices are locked.** `SaveInvoice` refuses new amounts or a new client once a collection receipt exists (message says to void the receipt first). The Billing checklist shows the same rule (`BillingView.Guide.cs`), replacing the old "net must cover what was collected" check that allowed raising a paid invoice.
- **B12 — cancelling a statement resets its trip reports.** In `SoftDelete` (`MJdatabase.Integrity.cs`), trips whose only invoice was the cancelled statement's own go back to **Completed**; trips with their own service invoice stay **Invoiced**.
- **B9 — the old Generate SOA dialog is gone, and it was not dead code.** The button was hidden, but two Billing menu items still opened it ("Generate statement of account (SOA)" in Actions and "Generate statement of account…" on the row menu), so a statement could be made without the Nov 6 checks. Both items now open Statement of Account ("Make a statement of account (SOA)" / "Make a statement for this client…", which starts a new statement for that client with its ready trips). Removed: `Views/GenerateSoaDialog.cs`, its button in `BillingView.Designer.cs`, and the unsafe `SaveStatementOfAccount(soa, List<int>)` overload. `PrintHelper.PrintStatementOfAccount` and `PrintForms.StatementOfInvoices` are now unused and were left in place.
- 3 new Verify checks (paid-invoice lock, cancelled statement resets old invoice's trip reports, cancelled statement keeps a trip with its own invoice Invoiced). All pass.

**Not done — needs your decision: B10 (payment date on receipts).** The Collections help text says the payment date is always today, so receipts are dated now by design. Adding a date field changes that rule and the form layout. If the office needs to record a check received on an earlier day, say so and I will add a date field (default today, not in the future, not before the invoice date).

**B10 decision (Nov 7, 2026): keep the payment date as today.** Receipts stay dated the day they are entered, as the Collections help text says. No change made; B10 is closed.

## 15. Progress: payment date and Step 4 (Nov 7, 2026)
**B10 reversed — receipts can carry an earlier payment date.** (Earlier answer "keep today" was replaced by "it can carry payment earlier than today".)
- Collections form has a new **Payment date** field (default today; the picker stops at today). `SaveOneReceipt(..., DateTime? paymentDate = null)` and `UpdateReceiptDetails(..., DateTime? paymentDate = null)` take it; `CheckPaymentDate` refuses a future day and a day before the invoice date. The checklist, help text and edit form use it. Editing a receipt can correct its date.
- Checks: `Receipt_CarriesAnEarlierPaymentDate`, `Receipt_DefaultsToToday_AndRefusesAFutureDayOrOneBeforeTheInvoice`.

**Step 4 — the whole story of an order (done)**
- New `Database/MJdatabase.OrderChain.cs`: `OrderChain` (trip report → live invoice → statement → receipt, with a one-line `Stage` and `Line()`), `GetOrderChains / GetOrderChainForTrip`, `GetTripsBilledMoreThanOnce`, `ResolveSettledTrips`.
- **Order History** (`TransactionIndex`, `OrderHistoryView`): each completed trip shows Billing stage, Trip report, Invoice, Amount, Statement, Receipt with paid date and method. The search finds trips by trip report, statement or receipt number. The CSV export has the new columns.
- **Printed dossier** (`PrintForms.DeliveryDossier`): the Billing section now shows statement, stage, receipt number, paid-on date, payment method and reference.
- **Finance History / Settled records**: new column "Dispatch · trip report", search by dispatch, trip report, SOA, SI or receipt, and a right-click **Open order** item for each trip of the record.
- **Billed more than once**: Settled records shows a red warning line, and the Dashboard shows an action item, for any trip that more than one live invoice bills.
- Checks: `OrderChain_ShowsEachStageOfATrip`, `OrderChain_FollowsAnOldStatementsInvoice`, `BilledMoreThanOnce_FindsAnOwnInvoicePlusAnOldStatement_AndNothingElse`, `SettledRecords_ShowTheDispatchAndTripReportBehindThem`. 71 checks run; the only failures are the 6 that need a real database or the repository folder when run from the scratch build.

**Left:** Step 5 — decide the old whole-statement invoices (convert them to per-trip invoices, or freeze old statements read-only), then remove the old branch in the statement save. Data clean-up still to do by hand: merge the two Butterworld client records, file the missing trip reports, invoice the seven ready trips.

## 16. Step 5 done: old statements are frozen (Nov 7, 2026)
- A statement that made its own whole-statement invoice (before the per-trip service invoices) can no longer be edited or re-saved. `WhySoaCannotBeEdited` says it is frozen; it can still be collected, printed and cancelled. To change one, cancel it and make a new statement from the trips' service invoices.
- The old branch in `SaveStatementOfAccount` that created or updated a statement's own invoice was removed; a save now always links the trips' service invoices.
- Checks: `OldStatement_IsFrozen_ButStillCollectable`, `StatementWithPerTripInvoices_IsNotFrozen`. 73 checks run; only the 6 that need a real database or the repository folder fail in the scratch build.
- The unused `IsLegacySoa` branches in `StatementOfAccountView` (never reached now) were left in place.
