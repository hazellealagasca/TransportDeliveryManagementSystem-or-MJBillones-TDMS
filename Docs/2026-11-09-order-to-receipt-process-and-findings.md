# Order to Collection Receipt: how it works now, and what is wrong with it

Date: 2026-11-09. Scope: Loading Schedule and Dispatch up to the Collection Receipt.
Written as a read-only review. **Update, same day: F1 and F3 are now fixed** (see "Fix status" below). All other findings are still open.

**Fix status**

| Finding | Status | What changed |
|---|---|---|
| F1 | Fixed | Cancelling a statement (either way: Cancel with a reason, or Delete) now resets each freed invoice to its full amount (`Deductions = 0`, `NetAmount = TotalAmount`). `Database/MJdatabase.Integrity.cs`. |
| F3 | Fixed | One rule for all paths in the new `Database/MJdatabase.BilledGuards.cs`: a finished trip report whose trip has a live invoice (dispatch, special trip, or an old statement's invoice) cannot be voided or edited. The void guard now sits inside `SoftDelete`, so no screen can skip it. `SaveTripReport` refuses the edit, and the Trip Reports screen checks first (before the store lines are written) and shows the reason. The form header says "billed, locked". A Draft is not locked. Cancel the invoice and the report opens for changes again. An "unlock with a reason" option was **not** built. |
| F5 | Decided and applied | Decision: no deduction in billing, ever. Any deduction is a free-text note per crew member in the trip pay screen (column "Deduction / notes (text only, never billed)"); no amount is stored or calculated. The incident details label no longer says "Deducted". F8 decision: one truck, one dispatch per date (kept as is). |
| Harness | Fixed | The pop-up closer in two trip-report checks now skips windows of other threads (`CloseOpenPopups`). |
| Checks | Added | `Verify/Checks.BilledGuards.cs`: 12 checks. They failed before the fix (8 of 12) and pass now. |

No existing data needed repair: the live copy had no invoice with a leftover withholding share.

**How this was checked**
- Read the code that runs the flow: `Database/MJdatabase*.cs`, `Views/DispatchManagementView*.cs`, `Views/TripReportView*.cs`, `Views/BillingView*.cs`, `Views/StatementOfAccountView*.cs`, `Views/CollectionView*.cs`, `Functions/SoaTotals.cs`, `Functions/CapacityValidator.cs`, `Functions/DispatchGate.cs`.
- Read a **read-only copy** of the live database (`C:\ProgramData\MJ Billones Transport\TDMS\mj_billones.db`, copied on 2026-11-09) and ran queries on the copy. The live file was not touched. (The `mj_billones.db` inside the repo is an old sample and is not the live data.)
- **Not done:** I did not run the app or the `Verify` suite end to end. Each finding says whether it was confirmed by code, by data, or both.

---

## 1. The process, step by step (as the code does it today)

| # | Step | What happens | Rules that gate it | State after |
|---|---|---|---|---|
| 1 | **Order / Loading Schedule** | There is **no separate order screen any more**. The Dispatch form creates the order itself ("Dispatch & Loading Order"): client, route and stores, G and E crates per store, date, time, peak flag. | Client is saved to the client list if new. | Dispatch row with `LoadingScheduleId = 0`. |
| 2 | **Dispatch** | Pick truck, driver, 1 to 2 aides. | Truck, driver, aide required. Aides max 2. Crew and truck must be free (today) and not blocked. **One truck, driver or aide per date** across dispatches and special trips. Crates must fit the truck maximum, or save over it **with a typed reason (5+ characters)**. Dispatch number unique (`RR0001` style, typed or generated). Past date asks for confirmation. | `Scheduled` (or the status picked: `In Transit`, `Completed`, `Cancelled`). |
| 3 | **Truck leaves** | Automatic: when the departure time passes, the dispatch becomes `In Transit` (checked every 10 s while screens load). Or **Start trip now** by hand. | A truck and crew are marked busy from **00:00 of the dispatch day**, not from departure. | `In Transit`; truck `In Use`; crew unavailable. |
| 4 | **Mark order complete** (by a person) | Releases truck and crew, marks stops Delivered, closes any linked loading schedule. | Not before the dispatch date. Must be In Transit, Delayed, or past its departure time. | `Completed`. This is the **gate for everything below**. |
| 5 | **Trip Report** | Per store: DR numbers, delivered G/E, returned (empty) G/E, arrival and departure times, receiving person. Crew pay recorded (daily wage + allowance). Can be saved as **Draft** or Completed. | Dispatch must be Completed. One live report per dispatch. DR numbers unique. Returned not equal to loaded needs a **double confirmation** (not a block). Delivered differing from dispatched needs one confirmation. | Trip report `Draft` or `Completed`. |
| 6 | **Service Invoice (Billing)** | One invoice per trip. The **SI No. is typed** from the printed booklet. Amount comes from the Rate Matrix or is typed. Fuel surcharge is always typed. Due date is issue date + 15 days. | Trip report must exist and **not be Draft**. One live invoice per trip. SI No. unique forever. Amount 0 stays `Draft`. | Invoice `Unpaid` (or `Draft`). Trip report becomes `Invoiced`. |
| 7 | **Statement of Account (optional)** | Groups several invoices of **one client** (normal trips and special trips never mix). Works out VAT and 2% withholding **on the statement**, then writes each invoice's share of the withholding back to the invoice. | Invoices must be Unpaid, have an amount, and be on no other statement. Client compared by typed name. Statement total must equal what its invoices work out to. | Statement `Unpaid`. Invoices carry a withholding share (`Deductions`), so `NetAmount` is lower. |
| 8 | **Collection Receipt** | One receipt pays **a whole statement** (all its open invoices) or **one lone invoice**, for the **full net amount only**. The receipt number is typed. One row is stored per invoice, all with the same receipt number. | Receipt number unique. Date not in the future and not before the invoice date. Amount is **not typed**: it is the invoice's `NetAmount`. | Each invoice `Paid`; statement `Paid`. |
| 9 | **Void a receipt** | Voids every row of that receipt number. | User and time are logged (no reason is asked). | Invoices back to `Unpaid`; statement back to `Unpaid`. |

**What the whole flow is doing, in one line:** an operations record (dispatch, trip report) is turned into a money record (invoice, statement, receipt) using **typed numbers and typed amounts**, with a completed dispatch as the only hard gate and a 2% withholding that the system **assumes** instead of recording.

---

## 2. Findings, ranked

Severity: **HIGH** = wrong money or wrong record. **MED** = wrong work-flow or misleading data. **LOW** = tidy-up.
"Confirmed by" says how I know.

### HIGH

**F1. Cancelling a statement leaves the 2% withholding on its invoices**
- `Database/MJdatabase.Integrity.cs:173` frees the invoices with `UPDATE Invoices SET SoaId = 0 ...` and nothing else. The code that drops a single trip off a statement does reset them (`Database/MJdatabase.SoaInvoices.cs:114`: `Deductions = 0, NetAmount = TotalAmount`). The whole-statement cancel does not.
- Result: the freed invoice shows in "ready for a statement" and in Collections as a **lone invoice with a reduced Net** (total minus its old withholding share). If it is collected on its own, the client is asked for about 2% less with no statement and no withholding agent behind it. It is corrected only if the invoice is re-saved or re-added to a new statement.
- Confirmed by: code. The existing check `ReadyForStatement_ACancelledStatementFreesItsInvoices` only counts the freed invoices; it does not look at `NetAmount`. Not seen in live data yet (the cancelled statements there predate this model).
- Fix: reset `Deductions` and `NetAmount` in the same `UPDATE`, and add a check for it.

**F2. The collectable amount depends on which route was used, and the withholding is assumed, not recorded**
- Statement route: invoice net = total minus a 2% share. Direct route (lone invoice): net = total, no withholding (`SaveInvoice` sets withholding to 0 unless the invoice is on a live statement).
- The same trip for the same client therefore collects **a different amount** depending on the button used. In the live data, 8 of the 10 live statement invoices carry a share (the 2 without are on the two older statements); both lone invoices carry none.
- The receipt amount is always the invoice net (`MJdatabase.OneReceipt.cs:201`). A client that does not withhold, or withholds a different rate, cannot be recorded. Two statements made before the formula (SOA-2026-0005 and SOA-2026-0006) have 0 withholding; SOA-2026-0006 is still Unpaid for 45,000.00, while the current formula would give 44,196.43.
- VAT is only worked out on a statement (`Functions/SoaTotals.cs`, fixed 12%). The system stores **no VAT on an invoice**, so a direct invoice has no VAT figure anywhere in the data.
- Confirmed by: code and data. Already raised as D4, F1, F2 in the reality-check plan; this adds the proof that the two routes disagree today.
- Fix: decide one rule (per-client withholding and VAT settings), then apply it on both routes.

**F3. A billed trip report can be edited, and can be voided**
- **Edit:** the Update path (`Views/TripReportView.cs` around 1426 to 1590) and `SaveTripReport` (`Database/MJdatabase.cs:10627`) have no check for "invoiced" or "on a statement". Update also rewrites the store lines (DR numbers, crates) with `SaveDispatchStops`. Statement lines hold **copies** of DR numbers and crate totals taken when the statement was saved, so the printed statement can disagree with the trip report afterwards. The screen only uses the Invoiced status to hide the "Issue invoice" button and to sort the list (`TripReportView.Workflow.cs:69, 157, 178`).
- **Void:** a guard that refuses to void a billed report exists (`ValidateRecordDeletion("TripReports")`, `MJdatabase.cs:~705`), but the Trip Reports screen never calls it. It calls `DeleteRecord("TripReports", id)` (`TripReportView.cs:1606`), which goes straight to `SoftDelete`, and that case has no billing check (`MJdatabase.Integrity.cs`, case `"TripReports"`). Only Invoices, Crews, Trucks and Deliveries call `ValidateRecordDeletion`.
- Result: an invoiced trip can lose its live report. The dispatch then shows as "waiting for a trip report" again and a second report can be filed (status `Completed`, not `Invoiced`) while the invoice and statement stay live.
- Confirmed by: code (call sites traced). Not run in the app.
- Fix: call the guard inside `SoftDelete` (so no screen can skip it), and lock a billed report against Update unless unlocked with a reason.

### MED

**F4. The "peak season" flag is ignored when pricing a new dispatch**
- `CalculateDeliveryTariff` (`Database/MJdatabase.cs:5666 to 5672`) takes peak from the **loading schedule** (`ls.IsPeak`). New dispatches have no loading schedule (`LoadingScheduleId = 0`), so `isPeak` is always false. The dispatch's own `IsPeak` is never read. For a single-stop dispatch the route text it searches is "client (destination)", so the client name is part of the match.
- Latent today: the Rate Matrix holds only 3 regular rates and no peak rate, and live invoices are mostly typed amounts. Four live dispatches are flagged peak.
- Confirmed by: code and data.
- Fix: use `del.IsPeak` and `del.Destination`.

**F5. Incident damage never touches money**
- Invoices carry no deductions since Nov 7 (`BillingView.Guide.cs:66`).
- An incident marked "Chargeable to crew" is described as "Deducted from driver/aide" (`Functions/RecordDetailsDialog.cs:488`), but crew pay is `BasePay + Allowance` only (`Models/Models.cs:566`). The `Extra` and `Deduction` fields exist and are ignored.
- Result: damage cost is recorded and printed, and is **recovered nowhere**. The label tells staff a deduction happened when it did not.
- Confirmed by: code.
- Fix: a business decision (bill the client, deduct from crew, or absorb), then make the label match.

**F6. Billing ignores what was really delivered**
- The price is per trip (Rate Matrix or typed). Delivered and returned crates change nothing. Short delivery only triggers two pop-up confirmations. Live example: one invoiced trip was dispatched with 108 G + 108 E crates and reported as 150 G + 0 E delivered, and it was billed in full.
- Confirmed by: code and data. Note: "returned" here means empty crates brought back, not undelivered goods (in the live reports that have returns, returned usually equals delivered), so "delivered + returned = dispatched" is not an invariant. The useful rule would be delivered not more than loaded, per store.

**F7. A crew member marked unavailable by hand is switched back on**
- The Crew screen has an Available checkbox (`Views/CrewManagementView.cs:42 to 45`). `ReconcileResourceStatuses` (`Database/MJdatabase.cs:236 to 246`) runs about every 60 seconds and on every dispatch, trip report and reschedule save. It sets `IsAvailable` for **every** crew member to "busy or free" from the trips alone.
- Result: leave, sick or suspended staff become available again and can be offered for dispatch.
- Confirmed by: code. (Trucks are protected: only statuses `In Use/Active/Available/On Trip` are rewritten.)
- Fix: give manual unavailability its own flag, or leave non-trip changes alone.

**F8. Trucks and crew are locked for the whole dispatch day, and for one trip a day**
- Busy test: any active dispatch dated today or earlier (`MJdatabase.cs:162`). Conflict test: same calendar date only (`MJdatabase.cs:8946 to 8976`). A 9 PM departure locks the truck and crew from midnight, and a truck cannot do a morning and an evening run.
- Live example: dispatch BCS0011 (today, 9 PM) already shows truck CBA 2699 as `In Use` and its aide (id 9) as unavailable.
- Confirmed by: code and data. May be intended; worth a decision.

**F9. A stale special trip locks a truck and crew with no way out by itself**
- Regular dispatches move to In Transit by the clock. Special trips never do (`UPDATE SpecialTrips SET Status` is only written on cancel). A special trip left in `Rescheduled` keeps its truck and crew busy until someone closes it.
- Live example: special trip #2 is `Rescheduled` for 2026-11-07 (two days ago). It keeps truck CAA 3866, driver id 11 and aide id 17 locked. Driver 11 and aide 17 are also assigned to dispatch BCS0011 today, so the same people are booked twice: the date-only conflict test does not see it.
- Confirmed by: code and data.

**F10. The receiving person can be invented**
- If **both** name boxes of the receiving person are blank, the form fills in "Store Representative" instead of asking (`Views/TripReportView.cs:1251, 1273, 1295` when saving a new report; `1459, 1481, 1503` when updating). The "name is required" messages that follow (1304, 1512) therefore cannot fire in that case.
- The default signatories in the trip report (`RAMIL MENDOZA`, `XERXES RYAN MANRIQUE`, `VINJOHN VILLAMOR`, `JOE-EM FLORES`) are written into the code (`MJdatabase.cs:10671 to 10679`, `2002`, `10080`), so a person who leaves still appears on new reports.
- Confirmed by: code.

### LOW

| # | Finding | Evidence |
|---|---|---|
| F11 | **Temperature rule is gone but still advertised.** `CapacityValidator.Validate` (cold cargo on a dry truck) has no caller in the app; only the checks use it. Dispatch cargo is set to the truck's own type, so a mismatch cannot happen. 21 of 22 trucks are cold, 1 is Combi, none is dry-only; six old dispatches show cargo different from the truck. README and `SYSTEM_SUMMARY.md` still say the system checks temperature. | `DispatchManagementView.cs:5438`; query on live copy |
| F12 | **Loading Schedule is a dead stage.** Nothing creates one (`SaveLoadingSchedule` is only called when cancelling from Order History). 16 of 20 completed dispatches have none; the 5 existing ones are old. Dashboard, "Past Due" automation, tariff and delete rules still branch on them. | `grep SaveLoadingSchedule`; live counts |
| F13 | **Special-trip invoice link is a second write.** `SaveInvoice` inserts the invoice without `SpecialTripId`; `LinkInvoiceToSpecialTrip` sets it afterwards (`MJdatabase.SpecialTripFlow.cs:283 to 290, 357, 371`). If the second write fails, the invoice has no trip and is treated like an old whole-statement invoice (cannot go on a statement). The one-live-invoice-per-trip rule in the database layer covers dispatches only (`TripBilling.cs:184`); special trips rely on the screen. | code |
| F14 | **Payment method is free text.** Live receipts hold `CHECK`, `Cheque` and `CASH`. Reports that group by method will split them. | live copy |
| F15 | **One-time data fixes live in product code.** `RunPaymentCleanupOnce` names specific invoices (`SI-1001`, `0765`, two system numbers). It does nothing on another database, but it should be removed once it has run. | `MJdatabase.OneReceipt.cs:409` |
| F16 | **Starting a trip can rewind a trip already on the road.** `RescheduleDelivery` refuses only Completed and Cancelled, so an In Transit dispatch can be set back to `Rescheduled`, then the clock sets it In Transit again. A reschedule to a past time flips to In Transit within seconds. | `MJdatabase.cs:7087 onward` |
| F17 | **Words that no longer match the rules.** `BillingView.cs:213` says "deductions"; `BillingView.cs:902` prints "damage deductions"; README lists "Incident Loss Deductions", card payments and "Fixed/per-crate" pricing. Invoices have no deductions, no card mode, and only per-trip pricing. | grep |

---

## 3. Already known and still true (see `Docs/plans/2026-11-09-statement-first-reality-check.md`)
Client identity is the typed name. No partial payment, no combined payment, no credit note. Fuel surcharge is typed. No rate effective dates. Due date is always 15 days. No month-close lock. I did not re-test these; none was contradicted.

## 4. Things that are solid
- The completed-dispatch gate is applied on every path to trip report, invoice and statement (`DispatchGate`).
- Typed numbers (SI, receipt, DR, dispatch) are checked for uniqueness inside the write transaction.
- Voiding a receipt re-opens its invoices and statement in one step.
- Statement shares of the withholding add up to the centavo (checked on live SOA-2026-0009 and -0010).
- No live invoice, receipt or statement is out of step with the others: paid invoices all have a live receipt for exactly their net, and statement totals equal the sum of their invoices.

## 5. Suggested order
1. F1 (one-line fix plus a check) and F3 (put the billed-report guard where no screen can skip it).
2. Decide the withholding and VAT rule once (F2), then apply it on both routes.
3. F7 (crew flag): small and safe.
4. F9 and F8 together: when does a trip stop holding its people.
5. F4, F10, F11, F12, F17: tidy-ups.

**Two questions only you can answer**
- Is a damaged-goods incident supposed to cost anyone money (client, crew, or the company)? (F5)
- May a truck run two dispatches on one date? (F8)
