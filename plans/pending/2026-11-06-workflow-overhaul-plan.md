# Workflow overhaul: trip report → invoice → SOA → collection (Implementation Plan)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Source: the 6-module workflow spec Hazel sent on Nov 6, 2026 (saved next to this file as `2026-11-06-workflow-spec.md`). Status: **draft, waiting for answers to the decisions below.**

**Goal:** Make the whole billing chain run as one strict line: Dispatch (G/E crates only) → Trip Report (Draft → Completed → Invoiced) → Service Invoice → SOA (one client, Normal *or* Special) → one Collection Receipt for 100% → everything Paid and in History. Crew pay stays inside Crew Management.

**Architecture:** Every status change happens in the database layer (`Database/MJdatabase*.cs`) inside the same transaction as the record that causes it, so the different screens that make invoices (Billing, the special-trip form, the batch queue) can't drift apart. Screens only show and ask. Schema changes are additive (`ALTER TABLE ADD COLUMN` plus a backfill once, after a backup). No column is dropped, so old records keep every value.

**Tech Stack:** .NET 9 WinForms (`net9.0-windows`, Nullable enabled), Microsoft.Data.Sqlite 10.0.10, SQLite file `mj_billones.db`.

**Spec:** `plans/pending/2026-11-06-workflow-spec.md`

## Global Constraints
- Not a payroll system. Crew wage / meal allowance never reach an invoice, SOA, receipt or any of their prints.
- Statuses are only: Trip Report `Draft` | `Completed` | `Invoiced`. Invoice `Draft` | `Unpaid` | `Paid` | `Cancelled`. SOA `Unpaid` | `Paid` | `Cancelled`. Collection Receipt `Completed` (voided rows keep `IsVoid = 1`). No `Partial` anywhere.
- Normal-trip and special-trip invoices never share an SOA, an SOA print layout, or a receipt slip.
- Amount paid = full net amount due, always. No balance, no overpayment.
- SOA lines are strictly: DR No., Destination Store, Plate No., Tonnage, Drops, Freight Tariff Amount, Fuel Surcharge, plus a Driver / Helper **names-only** column. Totals are strictly: Total Sales (VAT inc.), 12% VAT, Net of VAT, 2% EWT, Total Amount Due.
- Words: "crate / crates" (never "tray"); "Trip Report" (never "franchise"). From `.agents/rules/billing_workflow_and_terminology.md`.
- Existing records are never deleted or renumbered by this work. Every schema step takes a backup first (`BackupDatabase(...)`, same as `EnsureReceiptSchemaForSoa`).
- Build ends with **0 errors and 0 warnings** (baseline today: 0 errors, 14 warnings at 6 sites; see Task 13).

## What the code does today (research, Nov 6)
| Spec says | What the code has now | Effect on the plan |
|---|---|---|
| Zero partial payments | Mostly done already (Nov 6 work): `SaveOneReceipt` pays the full SOA/invoice, `numAmountPaid.Enabled = false`. **But** `RecalculateInvoiceStatus` (`MJdatabase.cs:323`) can still write `Partial`. `UpdateOverdueInvoices` (`:7731`) writes `Overdue` as a stored status. ~25 UI spots still show "partly paid" / balance: `BillingView.cs:243,545,552`, `BillingView.Summary.cs:40-66`, `BillingView.Guide.cs:160`, `CollectionView.Guide.cs:210`, `StatementOfAccountView.cs:1148`, `DashboardView.cs:566-567`, `MainForm.cs:851`, `MainForm.Shell.cs:159`, `RecordDetailsDialog.cs:394`, `ModuleHistoryView.cs:478-485`, `MJdatabase.cs:7749,7799,12582` | Clean-up pass plus one status normalization of old rows |
| Receipt print has no balance | `PrintForms.CollectionReceipt` still has the "Remaining balance" section and a "Balance" column (`PrintForms.cs:144-147, 176`) | Remove both |
| Trip report `Status` field | **No Status column** on `TripReports`. "Completed" today means "a report row exists and is not void" | New column, backfilled from the invoices |
| Invoice made → trip report "Invoiced" | Invoices are made in **three** places: `BillingView.BtnSave_Click` (`:652`), `BillingView` update (`:739`), and the special-trip form via `MJdatabase.SpecialTripFlow.cs:353,367` | The status change goes inside `SaveInvoice` / `LinkInvoiceToSpecialTrip`, not in a screen |
| Remove `total <= 0` block | Blocked **twice**: in the screen (`BillingView.cs:624,709`) and in the database (`SaveInvoice`, `MJdatabase.cs:11122` throws unless Draft) | A zero-gross invoice saves as `Draft`. SOA and Collections already refuse drafts (`WhyTripNotReadyForSoa`, `SaveOneReceipt`) |
| SOA picks invoices by client | The SOA form's picker (`StatementOfAccountView.Dispatches.cs:244 RefreshPicker`) lists **completed trip reports in a date range**, shows each one's invoice state, has an "All clients" box, and mixes normal and special trips in one list. The save already links per-trip invoices (`LinkTripInvoicesToSoa`, `MJdatabase.SoaInvoices.cs`). A hidden older dialog (`GenerateSoaDialog`, `btnGenerateSoa.Visible = false`) already ticks invoices | Change the picker's source to invoices (reuse the line-building code `AppendDeliveries`/`AppendSpecialTrips`). Delete nothing from `GenerateSoaDialog` beyond leaving it hidden |
| Normal vs Special SOA | No type on `StatementsOfAccount`. An invoice's kind is already known: `SpecialTripId > 0` = special, `DeliveryId > 0` = normal | New `SoaType` column, checked in the database save |
| No wage fields on SOA | Screen already writes zeros (`StatementOfAccountView.cs:1404`). Model `StatementOfAccountItem` and the INSERT/SELECT (`MJdatabase.cs:11481, 11807-11853`) still carry `DriverWage, HelperWage, DistanceKm, DieselCost, Excess`. Prints don't show them | Remove from the model and SQL. **Keep the database columns** (old rows keep their values; nothing reads them) |
| Wage only in Crew views | Crew pay is **entered on the Trip Report** (card 3, `TripReportView.Layout.cs:105-122, 469-760`). It also shows in `DispatchDetailsDialog.cs:430-446` (tile), the delivery dossier print (`PrintForms.cs:478-483`), and `ReportsView.cs:355-390` (crew payout report). `CrewTripHistoryView` has **no pay at all** | Move the pay editor into Crew Trip History. Remove the other three |
| "Payment Completed" button | Collections has two buttons: `btnSave` "Record payment" (makes the receipt) and `btnUpdate` "Update receipt" (edits number/method/notes only) | See decision D1 |
| Finance / Trip Report History | "Finance History" is the **audit change log** (`ModuleHistoryView`), not a list of records. There is no Trip Report History | New "Settled" list in Finance History, and an Active / History switch on Trip Reports |
| Hide cargo dropdown | `cbCargoType` drives truck recommendation (`GetTruckRecommendation`), capacity check (`CapacityValidator.Validate`), the Combi cold/dry split (`pnlRowCombi`, `numCombiCold`), and "non-crate → Special Trip" routing. Empty cargo is read as Cold (`NormalizeCargo`), which would wrongly refuse every Dry truck | See decision D3 |
| Store breakdown columns | Today: Code, Name, **Route**, E-Crates, G-Crates, **Total** (`DispatchManagementView.cs:1846-1857`), columns are user-customisable and saved per computer. DR numbers are typed later on the Trip Report into the same field (`DispatchStops.DrNumbers`, `LoadingScheduleStops.DrNumbers`) | New default column set; reset old saved layouts once; the DR typed at dispatch shows up on the Trip Report |
| Trip overview "Tonnage", "Reference No." | Trucks have no tonnage field; `MJdatabase.TonnageLabel(truck)` works it out. "Reference" = the loading order no. (`LoadingSchedule.ScheduleNumber`), or `OrderReference` for a special trip | Use those |
| Tests | No test project and no CI. The `.csproj` compiles **every `.cs` under its folder** | A small verification console project goes **next to** the project folder, not inside it (Task 0) |

## Decisions (recommended default first; tasks are written with the default)
- **D1 Collections buttons.** *Default:* the button that records the receipt (today "Record payment") becomes **"✔ Payment Completed"** and runs the atomic settlement. The old "Update receipt" becomes "Edit receipt details" (receipt no., method, reference, notes; the amount is never editable). *Alternative:* follow the spec literally and rename only "Update receipt".
- **D2 Overdue.** *Default:* stop storing `Overdue`. Old `Overdue` rows go back to `Unpaid`; "overdue" becomes a red badge worked out from the due date. *Alternative:* keep `Overdue` as a stored status.
- **D3 Cargo type once the dropdown is hidden.** *Default:* save each dispatch with its **truck's** cargo type (the code already falls back to it, `DispatchManagementView.cs:5283,5715`), and check capacity as **total crates ≤ the truck's maximum** (`TruckMax`), with no cold/dry split. Combi is typed into remarks. Non-crate cargo still goes through the Special Trips tab. *Alternative:* keep cold/dry capacity rules and keep a small Combi split box.
- **D4 Batch "Issue Invoice (Selected)".** *Default:* **one invoice per trip** (the SOA and receipt code depend on this): the Billing form opens on the first selected trip, and after each save it moves to the next. "Back to Trip Reports" stops the queue; trips not yet saved get no invoice. *Alternative:* one invoice covering several trips (needs an invoice-to-trip link table and changes to the SOA/receipt code; much bigger).
- **D5 Returned crates on the Trip Report.** *Default:* remove the delivered-crate inputs, totals and return-rate tile, but **keep** the per-store "Returned" entry (needed for crate accounting and incidents). `DeliveredCrates` (NOT NULL) is filled with the dispatched G + E. *Alternative:* remove returned crates too.
- **D6 Crew payout report in Reports.** *Default:* move it into Crew Management (a "Pay summary" button there), and remove it from Reports. *Alternative:* keep it in Reports for Admin only.
- **D7 Old SOAs with both normal and special trips.** *Default:* label them `SoaType = 'Mixed'`, read-only, still collectable once, printed with the normal layout. New ones can never be mixed.
- **D8 Trip report Draft.** *Default:* the secondary button "💾 Save draft" saves with `Status = 'Draft'` (steps may be unfinished). "Issue Invoice" checks steps 1–3, sets `Completed`, then opens Billing.

## File map
| File | Responsibility in this work |
|---|---|
| `Models/Models.cs` | Status constants (`TripReportStatus`, `SoaTypes`, `PayStatus`), `TripReport.Status`, `StatementOfAccount.SoaType`, `CollectionReceipt.Status`. Drop wage props from `StatementOfAccountItem` |
| `Database/MJdatabase.Workflow.cs` (new) | Schema step + backfill, status transitions, SOA candidate invoices, settled-records query |
| `Database/MJdatabase.cs` | `SaveInvoice`, `RecalculateInvoiceStatus`, `UpdateOverdueInvoices`, trip report read/write, SOA item SQL, remove dead `SaveCollectionReceipt` |
| `Database/MJdatabase.OneReceipt.cs` | Settlement writes receipt `Completed` + SOA `Paid` in one transaction |
| `Database/MJdatabase.SoaInvoices.cs` | Refuse mixed kinds; check `SoaType` |
| `Database/MJdatabase.Integrity.cs` | Cancel invoice → trip report back to `Completed`; void receipt → SOA/invoices back to `Unpaid` |
| `Views/DispatchManagementView*.cs` | Module 1 |
| `Views/TripReportView*.cs` | Module 2 (+ removing crew pay card) |
| `Views/BillingView*.cs`, `Views/FinanceHubView.cs`, `MainForm.cs` | Module 3, batch queue, navigation |
| `Views/StatementOfAccountView*.cs` | Module 4 |
| `Views/CollectionView*.cs` | Module 5 |
| `Functions/PrintForms.cs`, `Functions/Printer.cs` | SOA Normal/Special layouts, receipt slips, dossier without pay |
| `Views/CrewTripHistoryView.cs`, `Views/CrewManagementView.cs`, `Forms/DispatchDetailsDialog.cs`, `Views/ReportsView.cs` | Module 6 |
| `Views/FinanceSettledView.cs` (new) | Finance History → Settled records |
| `../TDMS.Verify/` (new, outside the project folder) | Verification console |

## Review Focus
1. **Old data after the schema step**: a database where an invoice was cancelled and the trip re-invoiced must show its trip report as `Invoiced` (the newest non-cancelled invoice wins), not `Completed`. Test: `Backfill_ReinvoicedTrip_IsInvoiced` (Task 1).
2. **Cancelling or voiding afterwards**: cancelling an invoice must put its trip report back to `Completed` (back in the active list); voiding a receipt must put the SOA and its invoices back to `Unpaid`. Tests: `CancelInvoice_RevertsTripReport` (Task 2), `VoidReceipt_RevertsSoaAndInvoices` (Task 3).
3. **Special trips invoiced from the special-trip form** (not Billing) must also flip their trip report to `Invoiced`. Test: `SpecialTripFormInvoice_MarksReportInvoiced` (Task 2).
4. **Back halfway through a batch**: three selected, one saved, then Back: exactly one new invoice, two trip reports still `Completed`, no Draft rows left. Test: manual check M8.3 + `NoOrphanDraftInvoices` (Task 8).
5. **Mixed kinds forced past the UI** (for example an old SOA being edited): the database save must refuse it. Test: `LinkInvoices_RefusesMixedKinds` (Task 4).

---

### Task 0: Verification harness and test database hook

**Files:**
- Create: `..\TDMS.Verify\TDMS.Verify.csproj` (console, `net9.0-windows`, `UseWindowsForms`, ProjectReference to the main `.csproj`), `..\TDMS.Verify\Program.cs`, `..\TDMS.Verify\Checks.cs`
- Modify: `Database/MJdatabase.cs:45-49` (make `dbFilePath`/`connectionString` settable)

**Interfaces:**
- Produces: `public static void MJdatabase.UseDatabaseFile(string path)` (sets both fields, resets the `*Checked` flags). `Checks.Fresh()` returns a new temp DB path after `InitializeDatabase()`; `Checks.FromCopy(string realDb)` copies the user's DB to temp. `Check(string name, Func<bool> ok)` prints `PASS name` / `FAIL name`, exit code = number of failures.

- [ ] Step 1: Write `Checks.SmokeOpensFreshDb`: `UseDatabaseFile(Fresh())`, `InitializeDatabase()`, assert table `TripReports` exists.
- [ ] Step 2: Run `dotnet run --project ..\TDMS.Verify` and expect a build failure (no `UseDatabaseFile`).
- [ ] Step 3: Add `UseDatabaseFile`.
- [ ] Step 4: Run again and expect `PASS SmokeOpensFreshDb` with exit code 0.
- [ ] Step 5: Commit `test: add TDMS.Verify harness and test DB hook`.

### Task 1: Schema step + backfill of statuses

**Files:** Create `Database/MJdatabase.Workflow.cs`. Modify `Models/Models.cs` (TripReport, StatementOfAccount, CollectionReceipt, new constants), `Database/MJdatabase.cs` (`InitializeDatabase` calls the new step once; `GetTripReports*` and `SaveTripReport` read/write `Status`; SOA and receipt readers read the new columns).

**Interfaces:**
- Produces: `public static class TripReportStatus { Draft="Draft", Completed="Completed", Invoiced="Invoiced" }`, `public static class SoaTypes { Normal="Normal", Special="Special", Mixed="Mixed" }`, `public static class PayStatus { Draft, Unpaid, Paid, Cancelled }`.
- Produces: `public static void MJdatabase.EnsureWorkflowSchema()`, run once and guarded by SystemSetting `Nov6_WorkflowSchemaDone`, after a backup to `Backups/before_workflow_schema_{yyyyMMdd_HHmmss}.db`:
  - `ALTER TABLE TripReports ADD COLUMN Status TEXT DEFAULT 'Completed'`
  - `ALTER TABLE StatementsOfAccount ADD COLUMN SoaType TEXT DEFAULT 'Normal'`
  - `ALTER TABLE CollectionReceipts ADD COLUMN Status TEXT DEFAULT 'Completed'`
  - Backfill: a trip report is `Invoiced` when its `DeliveryId` / `SpecialTripId` has a non-cancelled invoice, otherwise `Completed`. An SOA is `Special` when every item has `SpecialTripId > 0`, `Mixed` when some do, otherwise `Normal`. Invoices with `Partial`/`Overdue` go back through `RecalculateInvoiceStatus` (now Paid/Unpaid only). SOA `Issued` → `Unpaid`, or `Paid` when all its invoices are Paid. Every changed row is written to `AuditTrail` (action `SCHEMA`).

- [ ] Step 1: Write the checks on `FromCopy(<staged mj_billones.db>)` plus seeded rows: `Backfill_ReportWithInvoice_IsInvoiced`, `Backfill_ReportWithoutInvoice_IsCompleted`, `Backfill_ReinvoicedTrip_IsInvoiced` (cancelled invoice + newer invoice), `Backfill_NoPartialOrOverdueLeft` (`SELECT COUNT(*) FROM Invoices WHERE Status IN ('Partial','Overdue')` = 0), `Backfill_RowCountsUnchanged` (counts of all 4 tables equal before/after), `Backfill_RunsOnlyOnce`.
- [ ] Step 2: Run them and expect FAIL.
- [ ] Step 3: Implement `EnsureWorkflowSchema`, the model properties and the reader/writer columns.
- [ ] Step 4: Run them and expect all PASS. Then `dotnet build` and expect 0 errors.
- [ ] Step 5: Commit `feat(db): status columns for trip reports, SOA type, receipt status`.

### Task 2: Status transitions in the database layer

**Files:** `Database/MJdatabase.Workflow.cs`, `Database/MJdatabase.cs` (`SaveInvoice` :11118, `RecalculateInvoiceStatus` :265, `UpdateOverdueInvoices` :7731), `Database/MJdatabase.Numbers.cs` (`LinkSpecialTripInvoice`), `Database/MJdatabase.Integrity.cs:112` (invoice soft-delete).

**Interfaces:**
- Produces: `private static void SetTripReportStatusFor(SqliteConnection c, SqliteTransaction? tx, int deliveryId, int specialTripId, string status)`. It never touches void reports and never moves `Invoiced` back to `Draft`.
- `SaveInvoice`: `TotalAmount <= 0` → saved as `Draft` (no throw). New non-draft or draft invoice for a trip → that trip's report `Invoiced`. The same happens after `LinkInvoiceToSpecialTrip`.
- Invoice cancelled (Integrity soft delete) → report `Completed`, only if no other non-cancelled invoice exists for the trip.
- `RecalculateInvoiceStatus`: `Paid` when collected ≥ net > 0, else `Unpaid` (Draft/Cancelled untouched). `UpdateOverdueInvoices` no longer writes statuses (D2); it returns the overdue count only.

- [ ] Step 1: Write the checks: `ZeroGrossInvoice_SavesAsDraft`, `InvoiceForTrip_MarksReportInvoiced`, `SpecialTripFormInvoice_MarksReportInvoiced` (through the `SpecialTripFlow` save path), `CancelInvoice_RevertsTripReport`, `Recalc_NeverWritesPartial` (one receipt row below net → `Unpaid`), `OverdueNotStored`.
- [ ] Step 2: Run and expect FAIL.
- [ ] Step 3: Implement.
- [ ] Step 4: Run and expect PASS.
- [ ] Step 5: Commit `feat(db): trip report Draft/Completed/Invoiced driven by invoices`.

### Task 3: Atomic settlement and reversal

**Files:** `Database/MJdatabase.OneReceipt.cs` (`SaveOneReceipt`), `Database/MJdatabase.Integrity.cs` (receipt void), `Database/MJdatabase.cs:12483` (delete the dead `SaveCollectionReceipt` that has the "remaining balance" logic; it has no callers).

**Interfaces:**
- `SaveOneReceipt(...)` (same signature) now also, in the same transaction: inserts rows with `Status = 'Completed'`, sets every invoice `Paid`, sets `StatementsOfAccount.Status = 'Paid'` when the target is an SOA. Refuses when the target's SOA is `Paid` or `Cancelled`.
- Voiding a receipt number voids all its rows. Its invoices go back to `Unpaid` and its SOA to `Unpaid`, in one transaction.
- Produces: `public static string MJdatabase.PayTargetKind(PayTarget t)` → `SoaTypes.Normal|Special|Mixed` (used by the slip choice).

- [ ] Step 1: Write the checks: `Settle_Soa_AllPaidInOneGo` (SOA Paid, 3 invoices Paid, 3 rows `Completed`, same receipt no.), `Settle_RollsBackOnChangedInvoice` (net changed mid-way → nothing written), `Settle_RefusesPaidSoa`, `VoidReceipt_RevertsSoaAndInvoices`.
- [ ] Step 2: Run and expect FAIL. Step 3: Implement. Step 4: Run and expect PASS.
- [ ] Step 5: Commit `feat(db): one-transaction settlement marks receipt, SOA and invoices`.

### Task 4: SOA rules in the database

**Files:** `Database/MJdatabase.SoaInvoices.cs`, `Database/MJdatabase.OneReceipt.cs` (`TripInvoice`), `Database/MJdatabase.cs` (`SaveStatementOfAccount(soa, items)` :11645 writes `SoaType`; item SQL drops wage columns from INSERT/SELECT), `Models/Models.cs` (remove `DriverWage, HelperWage, DistanceKm, DieselCost, Excess` from `StatementOfAccountItem`; the DB columns stay).

**Interfaces:**
- Produces: `TripInvoice.Kind` → `SoaTypes.Special` if `SpecialTripId > 0`, `SoaTypes.Normal` if `DeliveryId > 0`, else `""` (legacy lone invoice: never offered).
- Produces: `public static List<TripInvoice> MJdatabase.GetSoaCandidateInvoices(string client, string soaType, int thisSoaId)`: invoices of that client (exact match, case-insensitive), of that kind, `Unpaid`, `Net > 0`, nothing collected, and `SoaId` 0 or `thisSoaId`. Ordered by IssueDate, then Number.
- `LinkTripInvoicesToSoa` adds: every found invoice's `Kind` must equal `soa.SoaType`, otherwise it throws "Normal and special trips can't be on the same statement…". A `Mixed` SOA can't be edited.

- [ ] Step 1: Write the checks: `Candidates_OnlyThisClientAndKind`, `Candidates_ExcludeDraftPaidAndOnOtherSoa`, `LinkInvoices_RefusesMixedKinds`, `SoaItems_NoWageColumnsWritten` (insert writes zeros/NULL only via defaults; read model has no wage props, a compile-level check).
- [ ] Step 2: Run and expect FAIL. Step 3: Implement. Step 4: Run and expect PASS.
- [ ] Step 5: Commit `feat(db): SOA type, invoice candidates, no crew wage on SOA lines`.

### Task 5: SOA screen (Module 4)

**Files:** `Views/StatementOfAccountView.cs` (`BuildFormControls` :459, `CollectItemsFromGrid` :1358, `LoadData` :1119 statuses), `Views/StatementOfAccountView.Dispatches.cs` (`RefreshPicker` :244 and picker columns), `Views/StatementOfAccountView.Guide.cs`, `Views/FinanceHubView.cs`, `MainForm.cs`.

**Interfaces:**
- Consumes: `GetSoaCandidateInvoices`, `TripInvoice.Kind`, `SoaTypes`.
- Produces: `public void FinanceHubView.ProceedToCollection(int soaId)`, which selects the Collections tab and calls `CollectionView.PrefillForSoa(soaId)` (Task 7).

Behaviour to build:
- New first field **"SOA type"** (`cbSoaType`: "Normal Operations SOA" / "Special Trips SOA"). It is locked once a line is on the grid, and locked when editing a saved SOA.
- The period date pickers stop filling the picker. `PeriodCovered` is set from the min–max invoice dates.
- Picker rows = `GetSoaCandidateInvoices(client, type, CurrentSoaId)`. Columns: ☐, SI No., Issue date, Trip report #, Dispatch / reference, Destination, Net amount. Remove the "All clients" box. "Tick all" / "Add ticked to statement" stay. Adding a ticked invoice expands its trip into lines with the existing `AppendDeliveries` (normal) or `AppendSpecialTrips` (special).
- The line grid shows only the strict columns + "Driver / Helper" names (`Driver name / Aide names` from the dispatch, no amounts). Hidden helper columns (`colDeliveryId`, `colSpecialTrip`, `colTripReport`) stay hidden.
- After a successful save, show a green bar with **"➜ Proceed to Collection Receipt"**, which calls `ProceedToCollection(savedId)`.
- Statuses shown: Unpaid / Paid / Cancelled, plus a "Mixed (old)" tag.

- [ ] Step 1: Manual check M5 on a DB copy: pick client A + Normal: only A's unpaid normal invoices are listed. Switch to Special: only special ones. A special invoice never appears under Normal.
- [ ] Step 2: Tick 2 invoices, save; the total equals the sum of the 2 nets; the "Proceed" bar opens Collections with that SOA picked.
- [ ] Step 3: `dotnet build`: 0 errors.
- [ ] Step 4: Commit `feat(soa): invoice-based SOA with Normal/Special type`.

### Task 6: SOA prints

**Files:** `Functions/PrintForms.cs` (`StatementOfAccount` :189, new `SpecialTripStatementOfAccount`), `Functions/Printer.cs:26`.

**Interfaces:**
- Produces: `public static PrintFormDef PrintForms.SpecialTripStatementOfAccount(StatementOfAccount soa, List<StatementOfAccountItem> items)` with FormKey `"SOA_Special"`. `PrintHelper.PrintOfficialStatementOfAccount` picks it when `soa.SoaType == SoaTypes.Special`.
- Normal layout default columns: No., DR No., Destination store, Plate, Tons, Drops, Freight tariff, Fuel surcharge, Driver / Helper. "Trip report #", "Zone", "Store code", "Total crates", "Type" are removed from the list of choices. The tax block is exactly the 5 totals.
- Special layout columns: No., Date, Trip reference, Destination, Plate, Tons, Trip charge (freight tariff), Fuel surcharge, Driver / Helper. Same 5 totals. Title "Statement of Account — Special Trips".
- Saved print options for key `"SOA"` are reset once (`PrintKit.Reset("SOA")`) so old column picks don't bring removed columns back.

- [ ] Step 1: Manual check M6: print one Normal and one Special SOA to PDF; check that the columns match the lists above and that no peso amount appears in the Driver / Helper column.
- [ ] Step 2: Commit `feat(print): separate Normal and Special SOA layouts`.

### Task 7: Collections (Module 5)

**Files:** `Views/CollectionView.cs`, `Views/CollectionView.Designer.cs:306,336,142,208`, `Views/CollectionView.OneReceipt.cs`, `Views/CollectionView.Guide.cs:50,210`, `Functions/PrintForms.cs:108-187`.

**Interfaces:**
- Consumes: `SaveOneReceipt`, `PayTargetKind`.
- Produces: `public void CollectionView.PrefillForSoa(int soaId)`.
- Produces: `PrintForms.CollectionReceipt(cr)` chooses FormKey `"OfficialReceipt"` (normal: "Applied to" table of SI / trip) or `"OfficialReceiptSpecial"` (title "Collection Receipt — Special Trip", lines: trip reference, destination, SI No., amount). Both drop the "Remaining balance" section and the "Balance" column.

Behaviour:
- `btnSave.Text = "✔ Payment Completed"` (D1). `btnUpdate.Text = "✏️ Edit receipt details"`. `lblAmt` "Amount (full amount due)". `numAmountPaid` is read-only and always the target's `Net`. `lblInv` "Statement or invoice to collect".
- After a successful save: `"Payment recorded in full. Do you want to print the Official Collection Receipt now?"` (Yes/No). Yes → `PrintHelper.PrintOfficialCollectionReceipt(rows[0])`.
- Remove the partial-payment wording at `CollectionView.Guide.cs:210`.

- [ ] Step 1: Manual check M7: pay a Normal SOA → the prompt shows; Yes opens the normal slip without a balance line; the SOA and its invoices show Paid. Pay a Special SOA → special slip.
- [ ] Step 2: `dotnet build`: 0 errors. Step 3: Commit `feat(collections): Payment Completed, full amount, print prompt, two slips`.

### Task 8: Billing (Module 3) + batch queue

**Files:** `Views/BillingView.cs` (:27 `PrefillForDelivery`, :624, :709, :243, :545-552, :941-947), `Views/BillingView.Summary.cs`, `Views/BillingView.Guide.cs:160`, `Views/FinanceHubView.cs`, `MainForm.cs` (navigation).

**Interfaces:**
- Produces: `public void BillingView.StartBatch(IReadOnlyList<Delivery> trips, Action backToTripReports)`. It opens the form on `trips[0]`; after each successful save it opens the next; it shows "Invoice 2 of 3" in the header.
- Produces: header button **"← Back to Trip Reports"**, visible whenever Billing was opened from Trip Reports (single or batch). It discards the unsaved form (`FormGuard.ConfirmDiscard`), ends the batch, and calls `backToTripReports`.
- Produces: `public void MainForm.OpenBillingFromTripReports(IReadOnlyList<Delivery> trips)`.
- Remove both `total <= 0` message boxes. A 0 gross saves as `Draft` with an info toast "Saved as draft — add the rate before it can go on a statement". Remove `Partial` styling, "partly paid" counts, and the balance tile; overdue becomes a badge (D2).

- [ ] Step 1: Add check `NoOrphanDraftInvoices`: after the harness's simulated "batch of 3, save 1, back", the invoice count rose by 1 and the 2 unsaved trips have no invoice row of any status.
- [ ] Step 2: Run and expect PASS (the data layer creates rows only on save). Then manual check M8: zero gross saves as Draft; the batch moves 1 → 2 → 3; Back mid-way returns to Trip Reports with nothing extra.
- [ ] Step 3: Commit `feat(billing): back to trip reports, batch queue, zero-gross drafts, no partial`.

### Task 9: Trip Reports (Module 2)

**Files:** `Views/TripReportView.cs` (:995-1007 grid columns, save :1335, update :1514), `Views/TripReportView.Layout.cs` (cards :100-122, buttons :242-246), `Views/TripReportView.Center.cs` (tiles :34-36, :233, extra columns :106-111), `Views/TripReportView.Designer.cs:723` (`MultiSelect = true`), `Views/OperationsHubView.cs`.

**Interfaces:**
- Consumes: `TripReportStatus`, `MainForm.OpenBillingFromTripReports`.
- Produces: `private sealed class TripStepper : Control` with `SetStep(int done)` and 4 labels: "1 Store timestamps & signatures", "2 Gate times (in / out)", "3 Verification & signatory check", "4 Issue invoice". Step N is ticked when its checks pass (1: every store has time in, time out and received-by; 2: `TruckTimeIn` and `TruckTimeOut` valid 24-hour; 3: DTR approved/prepared, DR prepared/checked, guard filled).

Behaviour:
- Form: `btnSave.Text = "🧾 Issue Invoice"` (validates 1–3, saves `Completed`, then `OpenBillingFromTripReports([this trip])`). `btnUpdate.Text = "💾 Save draft"` (D8).
- Replace `lblDispatchInfoBanner`, `pnlMultiDropInfo`, `tripCrateSummary`, `tblCrateTotals` with one read-only `TextBox` (multi-line, Consolas 9.5, about 6 lines) showing: Dispatch No. · Ref No. · Date · Departure / Client · Route · Destination · Stores / Plate · Tonnage (`TonnageLabel`) · Driver · Helper(s) / G-Crates · E-Crates · Total.
- Remove the "Delivered E/G" columns from `gridStores`, the Del./Total Del. columns from `gridTripReports`, the "Crates delivered" and "Return rate" tiles, and `xVariance`. Keep "Returned" per store (D5).
- Remove card 3 (crew pay) and its `Features.CrewPay` binding (moved in Task 11).
- List: toolbar segmented **Active | History**. Active = `Status != Invoiced` and not void; History = `Invoiced`. New column "Status". With multi-select: **"🧾 Issue Invoice (Selected)"** enabled when ≥1 selected rows are all `Completed` and of one client; otherwise a message names the rows that block it.

- [ ] Step 1: Manual check M9: a draft saves with steps unfinished; Issue Invoice refuses until steps 1–3 are ticked; after the invoice is saved, the row leaves Active and appears in History; selecting 3 completed rows opens the batch.
- [ ] Step 2: `dotnet build`: 0 errors. Step 3: Commit `feat(trip-reports): stepper, issue invoice, text overview, active/history`.

### Task 10: Dispatch form (Module 1)

**Files:** `Views/DispatchManagementView.cs` (:1348-1366 cargo change, :1679 recommendation, :2628-2663 cargo/combi rows, :1846-1857 default columns, :2204-2251 grid build, save :5283/:5715), `Views/DispatchManagementView.Capacity.cs`, `Views/DispatchManagementView.Designer.cs` (`pnlRec`), `Functions/CapacityValidator.cs`.

**Interfaces:**
- Produces: `public static CapacityResult CapacityValidator.ValidateTotal(Truck truck, int totalCrates)`, which checks total ≤ `MatrixFor(truck).TruckMax` (0 crates → `NoCrates`) (D3).

Behaviour:
- Hide `pnlCargoContainer` / `cbCargoType` and `pnlRowCombi`. Save `CargoType = selTr.CargoType`. Edits keep the stored value.
- Crate inputs: `numGCrate`, `numECrate`, `txtTotalCrates` (read-only, G + E). Labels G first.
- Store breakdown default columns: Store Code, Store Name, **DR No.** (edits `DrNumbers`), G-Crates, E-Crates. Bump the saved-layout key so old per-computer layouts with Route/Total are dropped once.
- Replace the `pnlRec` card with a one-line label: `"Total 248 crates · ABC-1234 · 248 / 300 (83% full)"`, red when over.
- Placeholder in remarks: "e.g. Combi — dry + chilled".

- [ ] Step 1: Add check `ValidateTotal_DryTruckAcceptsCrates` (dry truck, 100 crates, max 300 → Allow) and `ValidateTotal_OverMaxBlocks`. Run: FAIL → implement → PASS.
- [ ] Step 2: Manual check M10: new dispatch shows no cargo dropdown; DR typed at dispatch appears on its Trip Report; the summary line updates as crates change.
- [ ] Step 3: Commit `feat(dispatch): G/E crates only, DR in store breakdown, compact capacity line`.

### Task 11: Crew pay quarantine (Module 6)

**Files:** `Views/CrewTripHistoryView.cs` (gets the pay editor moved from `TripReportView.Layout.cs:469-760`), `Views/CrewManagementView.cs` (pay summary, D6), `Forms/DispatchDetailsDialog.cs:430-446`, `Functions/PrintForms.cs:418-483` (delivery dossier), `Views/ReportsView.cs:355-390`, `Functions/Features.cs` (rename the label of `Features.CrewPay` to "Crew pay in Crew Trip History").

**Interfaces:**
- Consumes (unchanged): `MJdatabase.GetTripCrewPay(int deliveryId)`, `SaveTripCrewPay(int deliveryId, List<TripCrewPay>)`, `ResolveTripPay`.
- Produces: in Crew Trip History, select a trip → "Pay for this trip" panel (base pay, meal allowance, notes; "Use standard rates").

- [ ] Step 1: Check `NoWageInClientDocs`: a static scan in the harness that reads the source of `BillingView*.cs`, `StatementOfAccountView*.cs`, `CollectionView*.cs`, `TripReportView*.cs`, `PrintForms.cs` (the `CollectionReceipt`, `StatementOfAccount*`, `TripReport`, `DeliveryDossier` methods) and fails if any contains `TripCrewPay`, `DailyWage`, `FoodAllowance`, `BasePay`, `DriverWage`, `HelperWage`, `DieselCost`, `MealAllowance`. Run: FAIL today.
- [ ] Step 2: Move and remove. Run: PASS.
- [ ] Step 3: Manual check M11: pay entered in Crew Trip History is saved and shows on the pay slip; the Trip Report has no pay card.
- [ ] Step 4: Commit `refactor(crew): keep crew pay inside Crew Management`.

### Task 12: Finance History "Settled" + leftover wording

**Files:** Create `Views/FinanceSettledView.cs`. Modify `Views/FinanceHubView.cs:50` (History tab = sub-tabs "Settled records" | "Change log"), `Database/MJdatabase.Workflow.cs`, `Views/DashboardView.cs:566-567`, `MainForm.cs:851`, `MainForm.Shell.cs:159`, `Functions/RecordDetailsDialog.cs:394`, `Views/ModuleHistoryView.cs:478-485`, `MJdatabase.cs:7749,7799`.

**Interfaces:**
- Produces: `public static List<(StatementOfAccount Soa, List<TripInvoice> Invoices, string ReceiptNo, DateTime PaidOn)> MJdatabase.GetSettledRecords(DateTime from, DateTime to)` (Paid SOAs and Paid lone invoices).
- The active Billing list and SOA list hide `Paid` by default (filter chip "Show paid" off).

- [ ] Step 1: Check `SettledRecords_ListsPaidOnly`. Run: FAIL → implement → PASS.
- [ ] Step 2: Search `grep -rn -i "partial\b\|partly paid\|remaining balance" --include=*.cs` and expect matches only for `partial class` and the Nov 6 cleanup comments.
- [ ] Step 3: Commit `feat(finance): settled records in Finance History; remove partial wording`.

### Task 13: Zero warnings and final verification

**Files:** `Views/DashboardView.cs:1052` (rename `AttentionBar.Name(int)` → `NameAt`), `Forms/TruckCapacityDialogs.cs:125-127`, `Forms/RouteStoresDialog.cs:197,199`, `Views/StatementOfAccountView.Dispatches.cs:567-568` (may already be gone after Task 5), `Views/StatementOfAccountView.cs:37` (unused `currentFormWidth`), `.csproj` (add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` so warnings can't come back).

- [ ] Step 1: Run `dotnet build --no-incremental` and expect `0 Warning(s)`, `0 Error(s)`.
- [ ] Step 2: Run `dotnet run --project ..\TDMS.Verify` against a **copy** of the real `mj_billones.db`. Expect all PASS and exit code 0.
- [ ] Step 3: A fresh reviewer walks the full chain on the copy: dispatch → trip report → batch invoice → Normal SOA → Payment Completed → print, then the same for a special trip. They confirm the 4 spec verification items.
- [ ] Step 4: Commit `chore: zero warnings, warnings as errors`.
