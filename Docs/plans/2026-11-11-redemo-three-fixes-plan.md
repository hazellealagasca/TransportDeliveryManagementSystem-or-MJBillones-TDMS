# Plan: three redemo fixes (store address, truck/driver search, invoice total)

**Date:** 11 November 2026
**Target branch:** `workflow-overhaul-2026-11-06` (the latest version). The work happens in fresh worktrees cut from that branch, because the main checkout has someone else's uncommitted changes.
**Status:** for review. No code is written until this plan is approved.

| # | Fix | Size | Database change |
|---|---|---|---|
| 1 | Stores get an address | About half a day | Yes, one new column (additive) |
| 2 | Truck and driver: type to search | About half a day | No |
| 3 | Service invoice shows its total | About 2 hours | No |

None of the three changes a business rule: statement-first billing, no partial payments, the DR, TIN and time rules, and crew pay kept off client documents all stay the same. Existing records are kept. The new column starts empty on old rows.

**Order of work:** Fix 3 → Fix 2 → Fix 1 (smallest and most visible first). Each fix is one session and one commit, and each fix's branch starts from the previous fix's branch.

---

## Fix 3: Service invoice shows its total

**What is wrong now.** The saved invoice total **does** include the fuel surcharge (`Views/BillingView.cs`, the save around line 713: total = amount + fuel). But on the form the total line is deliberately blanked (`RecalculateNet` sets `lblNetAmount.Text = ""`), so the sum appears only in the strip at the top. To a viewer the computation looks missing.

**Why it was hidden (the reason to respect).** The amount the client actually pays is worked out on the **Statement of Account**: VAT, then withholding, then total due. That is a different figure from amount + fuel. Showing only "amount + fuel" on the invoice could look like the amount to collect.

**Change.** Fill the existing total panel (`pnlNetDue`, label `lblNetAmount`) with clearly labelled, read-only lines that update while typing:

```
Amount                         45,000.00
+ Fuel surcharge                3,500.00
= Invoice total                48,500.00
Statement total due (after VAT and withholding): PHP 47,632.14   <- only when the invoice bills a statement
```
(The figures are an example.)

- "Invoice total" uses exactly the formula the save already uses, so the screen and the database always agree.
- The statement line reads the statement's **saved** total due (the statement record). It is never recalculated on the invoice.
- The invoice list shows an **Invoice total** column, so a saved invoice can be checked at a glance. Keep it if it already exists, otherwise add it.
- The printed service invoice: see D2.

**What stays the same.** How the total is computed and saved, statement-first billing, the amounts locked to the statement, and the no-deductions rule on invoices.

**Checks (Verify harness)**
- The form total = amount + fuel for a new invoice, an edited invoice and a statement-billed invoice.
- The shown total equals the saved `TotalAmount`.
- The statement line equals the statement's saved total due.

---

## Fix 2: Truck and driver: type to search

**What is wrong now.** In New Dispatch Order (`Views/DispatchManagementView.cs`), `cbTruck` and `cbDriver` are pick-only lists (`ComboBoxStyle.DropDownList`, set in `DispatchManagementView.Designer.cs`). The aide boxes already have type-ahead (`MakeAideSlot`). The truck and driver items begin with a status tag:
- `"[ACTIVE] ABC-1234 [6T]"`, `"[ON TRIP] …"`, `"[MAINTENANCE] …"` (filled around line 4543)
- `"[ACTIVE] Juan Dela Cruz (12 Trips)"` (filled around line 4567)

That is why plain Windows auto-complete was never added: it matches from the start, so you would have to type "[ACTIVE]" first. Both boxes are owner-drawn (`DrawStatusComboBoxItem`) to colour the status.

**Technical constraint (must not break).** About 50 places in the dispatch screen read the pick as `trucks[cbTruck.SelectedIndex]` / `drivers[cbDriver.SelectedIndex]` (e.g. `SelectedTruckOrNull` in `DispatchManagementView.Capacity.cs`, and `DispatchManagementView.Availability.cs`). Filtering or reordering the box's items would break that mapping and pick the wrong truck. **The box's items, their order and `DropDownList` style stay exactly as they are.**

**Change**
1. Add a new shared helper, `Functions/SearchPicker.cs`. Attach it to a ComboBox with a function that gives each item's search text (truck: plate + truck type; driver: full name).
2. When the box has focus and the user types a letter or digit, a small suggestion list opens under the box. It shows every item whose search text **contains** the typed text (case-insensitive, not only its start), keeping the box's order and status colours. Backspace edits the typed text. Arrow keys move, Enter or a click picks, and Esc or leaving the box closes it without changing the pick.
3. Picking a suggestion sets the box's `SelectedIndex` to that item's **original position**, so the existing `SelectedIndexChanged` handlers (capacity check, crate footer, availability) run as they do today.
4. A small grey hint under or inside each box says "Type to search".
5. Use the same helper on the truck and driver boxes in the Reschedule window (`Forms/RescheduleDeliveryDialog.cs`).
6. **Do not use Windows AutoComplete** (`AutoCompleteMode`) on these boxes. That is what caused the cross-thread close error fixed on Nov 4. The suggestion list is a plain owned popup or ListBox, disposed with the form.

**What stays the same.** Availability rules, blocked trucks, the capacity check, the zone limit check, the truck recommendation and the aide slots.

**Checks**
- Typing "123" lists ABC-1234; typing "cruz" lists Juan Dela Cruz.
- The picked item is the same truck or driver that the capacity check uses and that is saved on the order.
- Esc or leaving the box keeps the previous pick.
- Closing New Dispatch Order or the Reschedule window while the list is open raises no error.
- The same works in the Reschedule window.

---

## Fix 1: Stores get an address

**What is wrong now.** A store has only a code, a name and a route (`ClientStores`: StoreCode, StoreName, DestinationRoute; model `ClientStore` in `Models/Models.cs`). There is no address anywhere: not in the database, the forms or the printouts.

**Change**
1. **Database.** Add `ClientStores.Address TEXT DEFAULT ''` through the existing start-up migration list in `Database/MJdatabase.cs`, next to the `ALTER TABLE ClientStores ADD COLUMN ExtraFieldsJson / IsArchived / ArchivedAt` lines. Add `Address` to the `ClientStore` model, and to the INSERT, UPDATE and SELECT in `SaveClientStore` and `GetClientStores` (around lines 12757–12830), plus any other query that builds a `ClientStore`.
2. **Where stores are typed:**
   - Master Data › Clients store table (`Views/ClientManagementView.cs`, `gridBranches`): a new **Address** column after Store name, editable and saved like the others.
   - Register a new store dialog (`Forms/RegisterStoreDialog.cs`): a new Address box under Store name.
   - Clients › stores and routes panel (`Views/ClientStoresRoutesPanel.cs` and `ClientStoresRoutesPanel.Pending.cs`, the stores for a client not yet saved): the address is carried with the store.
   - The store writers `RegisterClientStore` (`Database/MJdatabase.StoreRegistry.cs`) and `RegisterCompanyStore` (`Database/MJdatabase.RouteBuilder.cs`) take an optional address, so the "one writer for route store lists" rule stays intact.
3. **Where the address is shown:**
   - The trip report's store details window (`Views/TripStoreDialog.cs`) and as a tooltip on the store rows of the dispatch order and trip report tables.
   - Printouts: an optional **Address** column in the Print Studio column choices for the dispatch order and trip report prints (`Functions/PrintForms.cs`). It is off by default, so current prints look the same unless it is switched on.
   - Master Data › Archive (`Views/MasterDataArchiveView.cs`): the address shown with archived stores.

**What stays the same.** Store code rules, the one-route-per-store rule, archive and restore, and the route store lists.

**Checks**
- Save and read back a store with an address.
- Old stores read back with an empty address and still work in dispatch and trip reports.
- Renaming a store keeps its address.
- An archived store keeps its address after restore.
- A new store follows D1.

---

## Done means (for every fix)

- The build has **zero warnings** (the project treats warnings as errors).
- The checks listed for that fix are added to the Verify harness and pass.
- The full Verify run passes on a **copy** of the real database (`mj_billones.db`), never the real file.
- One commit on the session's worktree branch. Nothing is merged into `workflow-overhaul-2026-11-06` or pushed until the fix has been tested in the app.
- If the code differs from what this plan says, stop and report before going on.

## Also needed before the redemo (not part of this plan)

- Update the user manual and the paper to the statement-first flow and these three changes.
- Merge the overhaul into `master`, and demo from a clean hand-over build.
- The quick wins from the audit (`Docs/TDMS_System_Audit_2026-11-11.pdf`, fix order items 1–4).

## Decisions

| | Question | Recommendation | Answer |
|---|---|---|---|
| D1 | Store address required? | Address optional for all stores (user decision, 2026-11-11); no dashboard reminder | **User decision (used)** |
| D2 | Total line on the printed service invoice too? | Yes, if the paper invoice has a total line; otherwise screen only | **Yes, print it too (recommended, used)** |
