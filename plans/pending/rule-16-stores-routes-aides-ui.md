# Rule 16: stores and routes, aides, large-print UI, and keeping modules in sync
Source: Rule 16 as written by Hazel (chat, Nov 6, 2026). Status: **approved Nov 6, 2026, in progress.**

## What the code does today (research, Nov 6)
| Rule 16 says | What the code has now | Effect on the plan |
|---|---|---|
| 16a "With Collection" becomes "With Collection Receipt" | One label in `Forms/SpecialTripFormDialog.cs:490` (`chkWithCollection`) | Label change only; the database column stays the same |
| 16b two aide slots, max 2 | `DispatchManagementView` keeps a `HashSet<int> selectedAideIds` with **no limit**. Aides are saved as text `AideIds "1,4,7"` on Deliveries. There is no separate Loading Schedule screen; it's the "Loading Schedule & Dispatch" tab | New two-slot picker; a 2-aide limit in the UI and the database; a decision on old records with more than 2 aides |
| 16c a store on any route of its company | `ClientStores.DestinationRoute` is **one text field**, so each store belongs to one route. It's read or written in about 45 places in 9 files | **New link table** `RouteStores(RouteId, StoreId)` plus moving the existing data over. Highest risk, so it goes first |
| 16d locked route rows vs "+ Add" rows | The dispatch stores grid has Store Code, Name, Route, **E-Crates, G-Crates**, Total. Saved rows (`LoadingScheduleStops`, `DispatchStops`) have **no route-vs-added flag** | New `IsBaselineRouteStore` column on the stop tables, plus a rule for marking existing rows |
| 16d Total / **Intact / Missing / Damaged** columns | **No such columns anywhere.** Dispatch has E/G crates. Trip Reports have Delivered and Returned (E/G). Damage lives in Incident Reports (`DamagedQuantity`) | Mismatch between the rule's terms and the app's data, so it's a question |
| 16e real-time sync across all modules | No app-wide change event. Every view reloads on open | New `DataChanged` event that open views listen to |
| 16e.4 SOA "Cancelled Without/With Pay" | This is rule 13, deferred on Nov 5 (only the cancel guard shipped) | Scope question |

## Phases (riskiest first; each one is a working, tested build)
### Phase 0: Finish the open work from Nov 5
Verified when: the chargeable / no-charge cancel is tested on a test dispatch. The branch `payments-redesign-2026-11-05` is merged or left as you decide.

### Phase 1: Store and route data model (16c, database side) (Size: M)
- Add a `RouteStores` table (RouteId, StoreId, unique pair). Fill it once from `DestinationRoute`, with a backup taken first. Keep `DestinationRoute` for now, so nothing that still reads it breaks.
- Move the 9 files that read `DestinationRoute` over to the link table, in batches.
- Verified when: every existing store still shows on its old route, and one store can be added to 2 routes of the same company and appears in both route lists in Dispatch.
- Files: `Database/MJdatabase.cs`, `MJdatabase.StoreRegistry.cs`, `MJdatabase.Archive.cs`, `Functions/StoreEntryGuard.cs`, `StoreLookup.cs`, `Forms/RouteStoresDialog.cs`, `Views/DispatchManagementView.cs`, `Views/ClientManagementView.cs`, `Models/Models.cs`
- Blocked by: Phase 0

### Phase 2: Master Data → Clients, two tabs (16c, screen side) (Size: M)
- Tab 1 "Company Stores": a quick-entry bar (code auto-uppercased, name, Enter to add) and a list of code and name only. The "Add Store Details First" popup saves here.
- Tab 2 "Delivery Routes": routes on the left with a store-count badge; on the right, the company's stores with checkboxes, a search box and "Included / Available" tags.
- Verified when: a store added in Tab 1 can be ticked onto any route in Tab 2, and the count badge updates.
- Files: `Views/ClientManagementView.cs` (or a new `ClientStoresRoutesPanel.cs`), `Forms/RegisterStoreDialog.cs`
- Blocked by: Phase 1

### Phase 3: Dispatch "Stores and Crates for This Order" (16d) (Size: L, split into 3a and 3b)
- 3a: add `IsBaselineRouteStore` to `LoadingScheduleStops` and `DispatchStops`; mark existing rows; save and load the flag.
- 3b: grid redesign. Route rows are locked (code and name read-only, can't be deleted, 🔒 badge, Tab goes to the crate cells). Rows added with "+ Add Extra Store" can be edited and removed (➕ badge, red Remove button). Other rules: no "+ Add" until a client is picked, no duplicate store code (jump to the existing row), unregistered store opens the Add Store popup, no saving with blank added rows. Crate cells select their contents on focus.
- Verified when: a route's stores load locked, an added store can be removed, a duplicate is refused, and reopening the order shows the same rows and badges in Dispatch, Trip Reports and Order History.
- Blocked by: Phase 1

### Phase 4: Two-slot aide picker, max 2 (16b) (Size: S)
- Two large searchable slots, each with a Clear button. Each slot hides the person picked in the other. Only active, available aides are listed. A counter shows "0/1/2 of 2". The database refuses a third aide.
- Verified when: the same aide can't be picked twice, a third can't be added, and old orders follow your answer on records with more than 2 aides.
- Files: `Views/DispatchManagementView.cs`, `Database/MJdatabase.cs` (SaveDelivery check)
- Blocked by: none (can run alongside Phase 2)

### Phase 5: Special Trip checklist cards (16a) (Size: S)
- Rename the 4th option to "With Collection Receipt". The 4 checkboxes become large toggle cards: grey with [ ] when off, green with ✔ when on. Destination Address becomes a wide large text box. Mode of Payment becomes a large dropdown: Cash, Bank Transfer, Cheque, Online Payment.
- Verified when: the cards switch on and off with mouse and keyboard, the existing rules still apply (for example "Collection Receipt needs SOA or Service Invoice"), and the trip list shows the new label.
- Files: `Forms/SpecialTripFormDialog.cs`, `Views/SpecialTripHistoryView.cs`
- Blocked by: none

### Phase 6: App-wide refresh event (16e.1–3) (Size: S)
- New `AppEvents.DataChanged(kind)`, raised by saves of stores, routes, dispatches and special trips. Open Dispatch, Special Trip, Trip Report and Order History views reload their lookups and summary panels when it fires.
- Verified when: a store added in Master Data appears in an already-open Dispatch form's search without reopening it.
- Blocked by: Phases 1–3

### Phase 7: Large-print look (16a–d visuals) (Size: M)
- The rule's sizes (13–15 pt bold, 44 px rows, 2 px borders, emerald / navy / crimson states) become shared theme tokens, so all the redesigned screens use the same values and both themes still work.
- Verified when: screenshots at your laptop size (1456×819) in light and dark mode, with no clipped text.
- Blocked by: Phases 2–5

### Phase 9: Rule 13, full SOA cancellation (16e.4) (Size: L, split into 9a–9c)
- 9a: database. CancelType, CancelReason, CancelledBy and CancelledAt columns on SOAs and invoices; an EntryType column on CollectionReceipts (Payment / Refund / CreditApplied); a ClientCredits table; a NonBillable flag on trips.
- 9b: cancel dialog. Without Pay asks for a reason, voids or keeps payments, cancels invoices and either releases the trips or marks them non-billable. With Pay records or confirms the payment, then 2A Settled (write-off), 2B Credit carried over or 2C Refunded, then the trip choice. All of it in one transaction.
- 9c: credit applied on the next SOA for the same client. Net collections = payments − refunds everywhere (Collections, dashboard, reports).
- Verified when: each of the 5 paths (Without Pay, Without Pay plus void, 2A, 2B, 2C) is run on test data, and totals and statuses match by hand.
- Blocked by: Phase 0

### Phase 8: Full test (Size: S)
- Re-run the Nov 5 tests, plus one test path per phase, using computer use.

## Decision log
| Decision | Chosen option | Why |
|---|---|---|
| Timing | Start now, not after the demo | Hazel: "I need it now with no errors". Work happens on a new branch, so the current working build stays runnable until each phase is tested |
| Store ↔ route | Many-to-many (`RouteStores` link table) | A store can be on any route of its company |
| Intact / Missing / Damaged | Trip Report only; dispatch keeps E / G / Total | Missing and damage are only known after delivery |
| Locked route row with no delivery | Can be removed, with a reason (at least 10 characters), logged in the audit trail | Hazel's choice |
| Old orders with more than 2 aides | Trim to the first 2, once, after a backup; every trimmed order is logged | Hazel's choice |
| Old stop rows | Store on the order's route = 🔒 route store; anything else = ➕ added | Matches what actually happened |
| Rule 13 full cancellation | **Included** (new Phase 9), using the Nov 5 answers: Status stays "Cancelled" plus a CancelType column; refunds are their own entry type; carried-over credit is a "Credit applied" entry; trips not billed get a NonBillable flag (the chargeable cancel fee from Nov 5 is reused) | Hazel's choice |
| Large print | A switch in Settings → Appearance, on by default | Hazel's choice |
| Trimmed aides' pay | Keep their TripCrewPay rows | Pay history doesn't change |
| Order of work | Quick wins first: Special Trip (P5) → aides (P4) → store↔route (P1+P2) → dispatch rows (P3) → refresh event (P6) → large print (P7) → rule 13 (P9) → full test (P8) | Hazel approved |
| Approval | Approved Nov 6, 2026. Branch `rule-16`; back up the DB before each migration; commit per phase; never push. Only Admin/Finance can cancel an SOA. "No errors" means the build has 0 errors and each phase's test passes in the app | Hazel approved |

## Not yet specified
- Phase 3: what "Intact / Missing / Damaged" means at dispatch time (question 3)
- Phase 7: whether large print is always on or a setting (question 8)
- 16e.4 (rule 13 full cancellation): in or out of this plan (question 7)

## Always / Never
- Always: back up the database before any migration; each phase builds and is tested before the next; commit at each phase.
- Never: delete `DestinationRoute` or other old columns in this plan; change payment logic outside 16e.4; push to GitHub unless asked.

## Progress
| Phase | Status |
|---|---|
| 0 | open (chargeable cancel test pending) |
| 5 Special Trip cards | in progress |
| 4 Aides | not started |
| 1+2 Store↔route + Clients tabs | not started |
| 3 Dispatch rows | not started |
| 6 Refresh event | not started |
| 7 Large print | not started |
| 9 Rule 13 | not started |
| 8 Full test | not started |
