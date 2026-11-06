# Trip Reports: master plan (one file, for Claude Sonnet 5.5, medium effort)

Read this file top to bottom and do the steps **in order**. Each step is small and has a check. Finish a step's check before the next. Do not stop until Step 8 is done.
Background (only if you need the reason for something): `trip report form analysis.md` (finding numbers like #1 below) and `trip report visual fix plan (detailed).md`.

## Rules
1. Smallest change that fixes the problem. No refactors, no renames, no reformatting of untouched code.
2. Build must have **0 errors and 0 warnings** (the project treats warnings as errors).
3. The working tree already has other people's uncommitted edits. Run `git status` first. Never revert, stash or checkout them. Do **not** commit.
4. Never touch the real database (`C:\ProgramData\MJ Billones Transport\TDMS\mj_billones.db` and the project-root `.db`, `-wal`, `-shm`). Tests and screenshots use a **copy**. Check how `Verify/Program.cs` takes a copy and follow it.
5. No schema or numbering changes. No new packages.
6. Be honest in the final report: say what you did not run or verify.

## Files
`Views/TripReportView.Workflow.cs` (awaiting grid, tabs, create click) · `Views/TripReportView.Center.cs` (tiles, filters) · `Views/TripReportView.cs` (~1060-1065 loads deliveries and special trips, ~1595 header) · `Database/MJdatabase.AwaitingReport.cs` (SQL, create draft) · `Functions/PageKit.cs` (`StatTile`, `TileStrip`) · `Functions/UITheme.cs` (`ApplyGridTheme`) · `Verify/Checks.*.cs` (add checks here).

---

## PART A: make the data right (do this first)

### Step 1. One awaiting list that includes Special Trips (#1, confirmed bug)
Problem: the tile (15) and pop-up use `allDeliveries`, which `TripReportView.cs` extends with `GetSpecialTripsForTripReports` (special trips have negative `Id`, numbers like `ST-20261106-0003`). The table uses `AwaitingReportSql`, which reads only `Deliveries`, so the 3 special trips have **no row and no details**. Also each of those 3 special trips is **listed twice** in the pop-up.
- Build one list `awaitingRows` of `AwaitingReportRow` = completed regular dispatches with no finished report (existing SQL) **plus** completed special trips with no finished report. Add `IsSpecialTrip` (bool) and the special trip id to the row. For special-trip details (client, destination, truck, driver, date) use `BillingView.SpecialTripStandIn(st)` (see `TripReportView.Workflow.cs` ~404).
- Fix the duplicate special trips at the source (`GetSpecialTripsForTripReports`, probably a join returning one row per stop/client). Each special trip appears once.
- The tiles, the header, the table and the pop-up all read this one list. Delete the old in-memory `awaitingReport` computation once nothing uses it (`grep` first).
- `CreateReportFor` must work for special trips too. Follow how `TripReportView.cs` ~617, 1151, 1313 handle `SpecialTripId = -Id`. Do not invent a new path.
- Real data today: **9 dispatches + 3 special trips = 12**.
**Check (Verify):** insert completed dispatches with no report / Draft / Completed / Invoiced / Void-only, plus special trips; the list has exactly the right ones, each once; tile count == row count.

### Step 2. Header and filters follow the Awaiting tab (#2, #3)
- Awaiting header: `N completed trips need a report (X dispatches, Y special trips)`; with filters: `showing A of N`. Do not let `ApplyFilters` (~1609) overwrite it while `showAwaiting` is true.
- Make search, client, driver, truck and date filters work **in memory over `awaitingRows`** (reuse the period logic in `ApplyCenter`). Fill the client/driver/truck combos from `awaitingRows` in this tab. Disable the "All reports" combo here (tooltip: "Not used for trips awaiting a report"). Sort: newest/oldest by date. Reset filters resets these too, and **without** the "Are you sure?" dialog (`Center.cs` ~326).
**Check:** in the Awaiting tab, search/driver/truck/date change the rows; Reset restores them; the header matches.

### Step 3. Stop creating a draft on a stray click (#4)
- `Workflow.cs` ~268 `CellDoubleClick`: a double-click on a row **with no report** must do nothing; on a row with a draft it opens the draft.
- `CreateReportFor` when no report exists: ask `Create a trip report for {dispatch number}?` Yes/No (default No). "Open draft" needs no question.
**Check:** double-click creates nothing; answering No creates nothing.

---

## PART B: make it look right

### Step 4. The table (#15-#21, #28, #29)
- Columns: **Dispatch** (number, and the date plus "1 day ago" in a smaller line under it; add a small `Special trip` tag for special trips) · **Client** · **Route** · **Truck** · **Driver** · **Report status** · **Button**. This removes the separate Date and Report # columns.
- Real data: destinations are up to 177 characters (`R3 - Route 3: Malolos City Store ➔ … ➔ Bulacan North Depot`), dispatch numbers up to 25 (`DSP-20260925-001600-32089`). So Route shows a short two-line cell: label (`R3 · Route 3`, or the single destination) and `first ➔ last · N stops` (muted). A `(+2 stops)` text is shown as is. Full route in a tooltip.
- Widths (logical px, always through `Dpi()`): Dispatch 160, Client 150 (fill), Route 260 (fill, takes spare space), Truck 140, Driver 160, Status 150, Button 170. Total minimum is about 1170, which fits the ~1350 available at the user's screen (1918 px at 125%). Freeze Dispatch. Row height `Dpi(48)`. Set `FirstDisplayedScrollingColumnIndex = 0` after binding.
- Put the layout in one `ApplyAwaitingLayout()` and call it after every `ApplyGridTheme` (it re-picks a Fill column and sets `ShowCellToolTips = false`; set tooltips on again after it and in the `ThemeManager.ThemeChanged` handler at `TripReportView.cs` ~97).
- Status pill (own filled colours, so it is readable even on a selected row): grey "Needs report" 0-2 days, amber 3-6 days, red "Overdue" 7+ days, amber "Draft saved" (with report number under it). Put the 3 and 7 in two named constants. Today: 6 rows grey, 3 red.
- Colours only from `AppTheme` and the `Soft/Ink(SummaryTone)` helpers in `Functions/SummaryPanels.cs`. No hard-coded colours. Themed button (accent for Create, amber for Open draft, hover/pressed), row hover = `AppTheme.GridHover`, selection painted across **all** cells including the button. Remove the per-row red/white text override.
- Create bold/regular/small fonts **once** per grid, dispose with the view (no `new Font` in paint/format handlers). Replace the `Columns.Count - 3` index with a column `Name`.
- Empty/loading/error messages over the grid (themed label): "Every completed trip has a trip report." / "No completed trips match these filters." / "Loading…" / "Couldn't load the list." with Retry.
- Enter/Space on the button cell does the same as a click (with the Step 3 question).
**Check:** at 1918 px / 125% there is no horizontal scrollbar and Dispatch, Client, Route are visible; long routes and `DSP-…` numbers show a tooltip; selected row is readable in dark and light.

### Step 5. The tiles (light gray, small, simple)
Awaiting tab, four tiles, all computed from `awaitingRows`:

| Caption | Number (today) | Note | Click |
|---|---|---|---|
| Dispatches | 9 | need a report | show only regular dispatches |
| Special trips | 3 | need a report | show only special trips |
| Overdue | 3 | 7+ days waiting | show only these (both kinds) |
| Draft saved | 0 | started, not finished | show only these |

- Active/History tabs: Reports, Drafts, **No proof of delivery** (`7` with note `of 8 reports`), Incidents. Build two tile sets and swap them in `SetAwaitingMode()` / `SetHistoryMode()`. Replace the `cbShowF` click hack in `Center.cs` ~41-50. Remove the old pop-up (`ShowAwaitingMenu`): the table is the list. Tab buttons show counts: `Awaiting report (12)`.
- Look: add one opt-in `Minimal` property to `StatTile` (`PageKit.cs:201`) and a compact option on `TileStrip`, and turn it on **only** for these tiles. Other screens must not change.
  - Size: height 64 (was 96), width about 200 fixed and left-aligned, padding 12, radius 8, caption 8.5 pt semibold, number 17 pt, note 8 pt, 1 px border.
  - Colours (fixed, both themes): fill 232,234,237; hover 222,225,229; border 209,213,219; caption 55,65,81; number 31,41,55; note 75,85,99. Selected = darker fill plus 1 px accent border.
  - No dot. A 3 px left bar (amber or red) shows only when the tile needs attention. A zero is never coloured.
  - Notes are 22 characters or fewer. Longer explanation goes in a hover tooltip, set in the same method that sets the numbers.
**Check (Verify):** each tile number equals the number of rows its click shows, on the real-data copy (12 / 9 / 3 / 3 / 0).

### Step 6. Small fixes
- Search box about 280 px wide, placeholder per tab (`Search dispatch #, client, driver, truck…` in Awaiting), the clear X inside the box and only when there is text; remove the separate big X button.
- Combos styled like the chips (same height 36, flat, theme colours). Widths: Client 210, Driver 190, Truck 150, and set `DropDownWidth` from the longest item so `GOLDILOCKS CORPORATION` and `CCE 9458 (FORWARD 4 TONS)` read in full.
- Tab buttons, New trip report, Official trip report, Actions: same height. Official trip report uses the secondary role, not hard-coded pink.
- White scrollbar corner: should disappear once Step 4 removes the horizontal scrollbar. If it still shows at narrow widths, call the existing `ThemeManager.StyleNativeControl` path; only if still white, overlay a small themed panel.

### Step 6b. History tab: all invoiced trip reports, special trips included, with sorting and filtering
The "History (invoiced)" tab already exists (`ApplyActiveOrHistory`: status Invoiced). Make it complete and usable:
- **Include special trip reports.** A report with `SpecialTripId > 0` and status Invoiced must show here exactly like a dispatch report. Check that `DeliveryOf(t)` (`Center.cs`) returns details for special trips (they use the stand-in from `BillingView.SpecialTripStandIn`, negative id). If it returns null, the client/driver/truck columns are blank and the client/driver/truck filters silently drop them: fix `RefreshCenterData` so special trips are in the lookup, and make `GetSpecialTripsForTripReports` return special trips that **already have** reports, not only ones awaiting. Add a Verify check: an invoiced special-trip report appears in History with its details and passes the filters.
- **Tag and filter by kind.** Show the same small `Special trip` tag in the dispatch cell, and add a "Type" filter in this tab: All / Dispatches / Special trips.
- **Filters that work here:** date range (use the invoice or report date, state which in the tooltip), client, driver, truck, type, and search (report #, dispatch #, client, driver, truck, invoice #). The client/driver/truck combos are filled from the History rows.
- **Sorting:** clicking any column header sorts (second click reverses), and the existing "Sort:" chip offers Newest first / Oldest first / Client A-Z / Highest amount if that column exists. Sort and filters are remembered per tab (`SaveView`).
- **Columns:** report #, dispatch # (with tag), date, client, route, truck, driver, invoice #, status. Same widths/tooltips as Step 4. Read-only: no Create/Issue-invoice buttons in History (`btnIssueSelected` stays hidden).
- Header: `History: N invoiced trip reports (X dispatches, Y special trips)`; with filters `showing A of N`. Empty state: "No invoiced trip reports yet." / "No invoiced trip reports match these filters."
- Tiles for History: Reports, Dispatches, Special trips, plus total invoiced if an amount exists; same light gray look.
**Check:** on the real-data copy History shows the 3 invoiced reports with details; add a special-trip one in the Verify data and confirm it appears; each filter and each sort changes the rows as expected.

---

## PART C: prove it

### Step 7. Verify
1. Build: 0 errors, 0 warnings.
2. Run the Verify harness on a copy of the real data: all old checks pass plus the new ones (Steps 1, 2, 5). Never weaken or delete an old check.
3. Screenshots with `_tools\FormScreenshotter` (or run the app and capture) on the copy, **dark and light**, at 1918 px / 125% and about 1366 px wide. Look at: the longest route (`BCS0003`), the longest dispatch number (`DSP-20260925-001600-32089`), a special trip row, a multi-drop (`mariveles (+2 stops)`), the empty and loading states, and one **other** screen's tiles (they must look unchanged). If you cannot run or capture the UI, say so. Do not claim it passed.

### Step 8. Final report (short, plain)
1. What changed, one line per step (file names).
2. Verified: build result, Verify passed/total, screenshots yes/no.
3. Not done or not verified, honestly. Leave on purpose: draft-on-create still burns a report number (design change, separate job); "Missing POD 7 of 8" needs a data check (is POD really not uploaded?); `butter world` vs `BUTTERWORLD` are the same client typed two ways; `TripReportView` is a ~1,980-line class.
4. Files touched (only yours, from `git status`).

## Out of scope
The inner trip-report form, schema/numbering changes, splitting `TripReportView`, title-casing names, any change to other screens' tiles.
