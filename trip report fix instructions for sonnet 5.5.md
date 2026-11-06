# Trip Reports: implementation instructions (for Claude Sonnet 5.5, medium effort)

You are fixing the Trip Reports screen of a WinForms/C# app (SQLite, `Microsoft.Data.Sqlite`).
The diagnosis is already done. Read `trip report form analysis.md` in the project root first. Its finding numbers (#1, #2, ...) are used below.
Do **not** re-audit the screen. Implement the fixes, verify them, and stop.

---

## 0. Ground rules (read twice)

1. **Work in small steps and finish them all.** Follow the phases in order. Finish each phase's "Done when" check before starting the next. Do not stop after Phase 1.
2. **Make the smallest change that fixes the finding.** Do not refactor `TripReportView.cs` (about 1,980 lines), do not rename things you don't need to, and do not reformat untouched code.
3. **Do not touch** `.db`, `.db-shm` or `.db-wal` files, existing data, or the schema, unless a phase explicitly says so. No migrations are needed for this job.
4. **The project builds with warnings as errors (zero warnings).** Every change must build clean. A warning is a failed build.
5. **Comments:** keep the existing style (short `///` summaries on public members). No decorative comments.
6. **Don't commit and don't push** unless the user asks. Leave changes in the working tree.
7. **Be honest in the final report.** If something could not be verified (for example, no way to run the UI), say so. Do not claim a check passed if you did not run it.
8. **The working tree is already dirty** (many modified files). Run `git status` and `git diff --stat` first and remember what was already changed, so you only report your own changes. Never `git checkout`, `git restore` or `git stash` the user's existing work.

### Files you will work in
| File | Role |
|---|---|
| `Views/TripReportView.Workflow.cs` | Awaiting grid, tab buttons, create-draft click handlers |
| `Views/TripReportView.Center.cs` | Stat cards, filter bar, `RefreshCenterData`, `ApplyCenter` |
| `Views/TripReportView.cs` | Main view, `ApplyFilters`/`lblGridHeader` (around line 1595), `LoadData` |
| `Database/MJdatabase.AwaitingReport.cs` | `AwaitingReportSql`, `GetDispatchesAwaitingReport`, `CreateTripReportForDispatch` |
| `Verify/Checks.*.cs`, `Verify/Program.cs` | Existing self-check harness. Add checks here. |

Before editing, run `git log --oneline -5` and open `Verify/Program.cs` to see how checks are registered and how the Verify project is run. Follow that pattern exactly. Do not invent a new test framework.

---

## Phase 1: make the screen's numbers trustworthy (analysis #1, #2, #3)

This is the highest-value phase. Do it first and do it properly.

### 1a. One source of truth for "awaiting a report" (#1)
- In `Center.cs` `RefreshCenterData` (around line 149-151), `awaitingReport` is computed in memory from `allDeliveries` and `allTripReports`. The grid uses `MJdatabase.GetDispatchesAwaitingReport()` (SQL).
- Make the **SQL result the only source**. The card count and the card's popup menu must come from the same rows as the grid.
- Suggested approach: keep a `List<AwaitingReportRow> awaitingRows` field in the view. Fill it from `GetDispatchesAwaitingReport()` (run it off the UI thread the way `LoadAwaitingGrid` already does). Have `LoadAwaitingGrid` bind that same list. Change the card and `ShowAwaitingMenu` to use `awaitingRows` (rename uses of `awaitingReport`; remove the old in-memory computation when nothing else uses it. `grep` for every use first).
- `ShowAwaitingMenu` currently calls `OpenForDispatch(d.DispatchNumber)` with a `Delivery`. Adapt it to `AwaitingReportRow` (it has `DispatchNumber`, `DispatchDate`, `ClientName`, `DriverName`).
- Keep the 25-item cap, but make "… and N more" do something useful: either remove the cap or make that entry switch to the Awaiting tab (`SetAwaitingMode()`). Pick the simpler one.
- The card must refresh after a save or create. `TripReportView.cs` line ~1102 already calls `LoadAwaitingGrid()` after saves; make sure the card is updated from the same load.

**Special Trips must be in the awaiting list (confirmed bug, seen by the user).** The card and pop-up list comes from `allDeliveries`, which `TripReportView.cs` (~line 1065) extends with `MJdatabase.GetSpecialTripsForTripReports(reported)` (special trips have negative `Id`, dispatch numbers like `ST-20261106-0003`). The SQL in `AwaitingReportSql` only reads `Deliveries`, so those trips appear in the pop-up and in the count (15) but **not in the table** (9 rows). Fix:
- Make the awaiting rows include completed special trips that have no finished trip report, each marked as a special trip (a "Special trip" tag in the Dispatch cell is enough). Add `AwaitingReportRow.IsSpecialTrip` and carry the special-trip id so `CreateReportFor` / `CreateTripReportForDispatch` can create the report for it (see how `TripReportView.cs` ~lines 617, 1151 and 1313 handle `SpecialTripId = -Id`; do not invent a new path).
- **Duplicates:** in the pop-up each of `ST-20261106-0003`, `ST-20261106-0002` and `ST-20261105-0001` is listed **twice**. Find why in `GetSpecialTripsForTripReports` (likely a join returning one row per stop or per client) and return each special trip once. Add a Verify check for it.
- Make the "details" columns (client, destination, truck, driver, date) show for special trips too. Use `BillingView.SpecialTripStandIn(st)` (used in `TripReportView.Workflow.cs` ~line 404) as the source for them.

**Special trips get their own tile (user request).** In the Awaiting tab the tiles are: Dispatches (regular, `IsSpecialTrip == false`), Special trips (`IsSpecialTrip == true`), Overdue (7+ days, both kinds) and Draft saved. Clicking Dispatches or Special trips filters the awaiting table to that kind. The awaiting header reads `N completed trips need a report (X dispatches, Y special trips)`. Real data today: 9 dispatches, 3 special trips, 12 in total.

**Done when:** for any data state, the "Awaiting a report" card number equals the number of rows in the awaiting grid, and every dispatch number in the pop-up (until the pop-up is removed) is a row in the table with its details filled in. Write a Verify check for this (see Phase 5).

### 1b. Make the header and cards follow the active tab (#2)
- In awaiting mode the visible table is the awaiting list. The header (`lblGridHeader`, set in `TripReportView.cs` near line 1609) must read something like `Awaiting a report: N completed trips` (or `... showing X of N` when filtered). In Active/History mode keep the existing text.
- Update it in `SetAwaitingMode()` and `SetHistoryMode()` and again whenever the awaiting rows load. Do not let `ApplyFilters` overwrite the awaiting header while awaiting mode is active: guard with `showAwaiting`.
- The first three cards (Trip reports / Drafts / Missing POD) describe reports. Leave their numbers alone, but change the first card's note so it is honest: `in the selected period` is fine for Active/History; in awaiting mode that card is still "all reports in the period", which is acceptable as long as the header no longer claims "showing 4 of 7" for the awaiting list. Do not redesign the cards.

### 1c. Wire the filters to the awaiting list (#3)
The awaiting grid is bound to `List<AwaitingReportRow>`. Implement filtering **in memory over `awaitingRows`** (no new SQL):
- **Search box** (existing search control): match case-insensitively against DispatchNumber, ClientName, Destination, TruckInfo, DriverName, ReportNumber.
- **Client / Driver / Truck combos** (`cbClientF`, `cbDriverF`, `cbTruckF`): when `SelectedIndex > 0`, filter by exact case-insensitive match of the row's ClientName / DriverName / TruckInfo. The combo item lists are currently filled from reports (`Refill` in `Center.cs` around line 153-155) and may not contain every awaiting dispatch's client, driver or truck. In awaiting mode, fill them from `awaitingRows` instead, or the user cannot filter to those rows. Re-fill when the tab switches.
- **Date filter** (`cbPeriod`, `dtpFromF`, `dtpToF`): filter on `DispatchDate`. Reuse the existing period-to-range logic in `ApplyCenter` (Center.cs around lines 185-200) instead of rewriting it. Extract the range calculation into a small helper if needed.
- **"All reports" combo (`cbShowF`)**: it filters reports by Drafts/returned crates/incidents/POD, which does not apply to dispatches. In awaiting mode set `Enabled = false` and add a tooltip `"Not used for completed trips awaiting a report"`. Re-enable it in the other tabs.
- **Sort combo (`cbSortF`)**: in awaiting mode, support at least Newest first / Oldest first by `DispatchDate`. If the sort combo has other options that make no sense for dispatches, sort by date and disable the combo with a tooltip. Choose the simpler path.
- **Reset filters** must reset these awaiting filters too. Also remove the "Are you sure?" confirmation dialog at `Center.cs` line ~326 (#24): reset is harmless and reversible, so it should just reset.
- After filtering, update the awaiting header with `showing X of N`, and show the empty state from Phase 3.

**Done when:** in the Awaiting tab, searching a dispatch number, picking a driver or truck, and changing the date range each change the rows; Reset filters restores all rows; the header count matches the rows shown.

---

## Phase 2: stop writing on click (#4, #5)

### 2a. Remove the accidental double-click create
- `Workflow.cs` line ~268, `gridAwaiting.CellDoubleClick`: this creates a draft from a double-click on **any** non-button cell. Change it so a double-click does **not** write to the database. Acceptable behaviours:
  - **Preferred:** a double-click on a row that already has a draft (`ReportNumber.Length > 0`) opens that draft (this is safe); a double-click on a row with no report does nothing.
  - Alternative: ask `Create a trip report for dispatch {number}?` with Yes/No (default No) before creating.
- Keep the explicit "Create trip report" button working as it does.

### 2b. Confirm the create click, and keep the number honest
- In `CreateReportFor` (`Workflow.cs` ~313), when `wasNew` is true, show a Yes/No confirmation (`MessageBox` styled like the other dialogs in this view; default button No) naming the dispatch: `Create a trip report for dispatch {row.DispatchNumber}?`. Only proceed on Yes. Rows that already have a draft ("Open draft") need no confirmation.
- **Do not** change `CreateTripReportForDispatch` semantics or numbering logic in this job: removing the draft-on-create design is a bigger change than this task. Instead record it as a follow-up in your final report (see section 7).
- Do not add a reason for the confirmation beyond the dispatch number. Keep it one sentence.

**Done when:** a double-click on a non-button cell of a row with no report creates nothing; clicking the button asks first; answering No creates nothing and the awaiting count is unchanged.

---

## Phase 3: layout and visible defects (#7, #8, #14, #15, #16, #17)

### 3a. Make the key columns visible (#15, #16, #17)
- `Workflow.cs` `BuildAwaitingGrid` (~line 215-250) sets fixed widths via a dictionary (Dispatch# 120, Date 100, Client 180, Destination 230, Truck 190, Driver 170, Trip report 170, Report# 240) plus a 190 px button column. The sum exceeds the viewport at normal window sizes, so the grid opens scrolled right and hides Dispatch #, Date and Client.
- Change to this layout:
  - Dispatch #, Date, Client, Truck, Driver, Trip report, button: fixed or minimum widths, **reduced** (Dispatch # about 110, Date about 90, Truck about 170, Driver about 150, Trip report about 140, button about 170 at 100% DPI, always passed through `Dpi(...)`).
  - **Destination** and **Client** use `AutoSizeMode = Fill` with sensible `FillWeight`, so they absorb the free space.
  - **Report #:** keep it, but make it narrow (about 110) since it is empty until a draft exists.
- After binding data (`LoadAwaitingGrid`), set `gridAwaiting.FirstDisplayedScrollingColumnIndex = 0` and reset `HorizontalScrollingOffset` so it never opens scrolled.
- Add a **tooltip with the full cell text** for truncated cells (`DataGridView.ShowCellToolTips` is true by default; set `ToolTipText` in `CellFormatting` for Destination, Client and Truck).
- Stay inside the existing `ScrollBars.Both` setting; the goal is that horizontal scrolling is not needed at 1366 px width or wider, and still works below that.

### 3b. Empty and error states (#7, #8)
- Add a small themed label (use `AppTheme` colours and the same label style the view already uses; do not hard-code colours) shown over the awaiting grid when the filtered row count is 0:
  - No rows at all: `Every completed trip has a trip report.`
  - Rows exist but filters hide them: `No completed trips match these filters.` with the "Reset filters" button still available.
- In the `catch` of `LoadAwaitingGrid`, besides the existing toast, show the same label with `Couldn't load the list. Try again.` and keep `ErrLog.Error(...)`.
- Do not use a modal dialog for either state.

### 3c. Remove dead column noise (#14)
- The "Trip report" column repeats the tab's meaning. Keep it (it distinguishes "No trip report yet" from "Draft saved"), but change the "No trip report yet" text colour from alarm red to the **normal text colour**, and keep a muted amber only for "Draft saved". Use `AppTheme` constants. Look in `AppTheme` for existing text, warning and muted colours before adding anything.
- Reserve red for a dispatch **completed more than 3 days ago**: if `DispatchDate` is older than 3 days, show that row's "Trip report" cell in the theme's danger/warning colour. Put the day count in a named constant at the top of the class.

**Done when:** at a 1366 px-wide window the Dispatch #, Date and Client columns are visible with no horizontal scrolling; an empty list shows a message instead of a blank box; most rows no longer glow red.

---

## Phase 4: visual consistency and code health (#18-#21, #28, #29, #33)

### 4a. Dispose the fonts (#28) and drop the magic index (#29)
- In `Workflow.cs`, `CellFormatting` (~line 260) and `CellPainting` (~line 306) create `new Font(g.Font, FontStyle.Bold)` on every call and never dispose it.
  - Create **one** bold `Font` field per grid when the grid is built, reuse it in both handlers, and dispose it when the view is disposed (follow how this view already overrides `Dispose` or handles `Disposed`; do not add a second disposal mechanism).
  - Same check for `Font` allocations inside `Center.cs` handlers you touch.
- Replace `e.ColumnIndex == gridAwaiting.Columns.Count - 3` (~line 257) with a comparison against the column's `Name`. Give the Trip report column a `Name` (e.g. `colReportState`) in `Col(...)` / `BuildAwaitingGrid`; check how `Col` creates columns and extend it minimally to set `Name`.

### 4b. Theme the Create button (#18)
- `WireAwaitingButton` (~line 280-310) hard-codes `Color.FromArgb(252,222,225)`, `(150,20,35)`, `(253,238,205)` and so on, which are light pastels that clash with the dark theme.
- Replace them with `AppTheme` colours. Look up how other custom-painted controls in this project pick colours (search for `AppTheme.` usage and `UIThemeHelper.SetButtonRole`) and reuse: primary/danger tint for "Create trip report", warning tint for "Open draft", with clear hover and pressed states. The dark theme and the light theme must both be legible; check how `AppTheme` exposes the current theme, and do not assume dark.
- Keep the button's text, bold weight, and size.

### 4c. Row selection and scrollbar corner (#20, #21)
- Make the selected row use the same theme selection colours as the other grids: `UIThemeHelper.ApplyGridTheme` is already called; check whether it sets `DefaultCellStyle.SelectionBackColor/SelectionForeColor`. If the red text and white-bold inconsistency comes from `CellFormatting` overriding `ForeColor` per row, make the override respect `e.State & DataGridViewElementStates.Selected` so a selected row uses `SelectionForeColor`.
- Make the selection cover the button column as well (the button cell is painted via `CellPainting`: paint the background with `DataGridViewPaintParts.Background` including selection, which `e.Paint(..., Background | Border)` already does; verify that the selection colour shows in that cell before changing anything).
- The white square at the scrollbar intersection: if the project already has a helper for themed scrollbars, apply it to `gridAwaiting`. If not, set the grid's `BackgroundColor` to the theme background (the corner takes the control's `BackColor`/`BackgroundColor`). Do not write a new native scrollbar renderer.

### 4d. Keyboard access (#33)
- Make sure the button column responds to **Space/Enter** on the current cell: handle `gridAwaiting.KeyDown` for Enter/Space when the current cell is in `colCreate` and call `CreateReportFor(row)`. This is minimal; do not rewrite the column as a native `DataGridViewButtonColumn` painter.

**Done when:** no `new Font(...)` calls remain inside paint/format handlers; the Create button is legible in both themes; the selected row looks consistent across all cells; the corner is not a white square; Enter on a focused button cell creates/opens the report (with the Phase 2 confirmation).

---

## Phase 5: verification (do not skip)

### 5a. Build
1. Find the solution/project file (`*.sln` / `*.csproj` in the root). Build in Release or Debug with `dotnet build` (check what the project uses; look at the csproj `TargetFramework`).
2. The build must have **0 errors and 0 warnings**.

### 5b. Add self-checks in the existing Verify harness
Add checks next to the existing ones (`Verify/Checks.Audit.cs`, `Checks.Settled.cs`), registered the same way they are. They must run against a **temporary copy of test data or an in-memory/temp SQLite database**, never against the user's `mj_billones.db`. Look at how existing checks set up their data and copy that approach.
Cover:
1. **Awaiting rows equal the card count:** insert completed dispatches with: no report; a Draft report; a Completed report; an Invoiced report; a Void-only report. Assert that `GetDispatchesAwaitingReport()` returns exactly the first, second and fifth, and the shared count helper agrees.
2. **Creating a draft removes nothing else:** `CreateTripReportForDispatch` on a no-report dispatch creates exactly one Draft; calling it again returns the same report number and creates no second row.
3. **Non-completed dispatches are refused:** `CreateTripReportForDispatch` on a dispatch whose status is not Completed throws/refuses and creates nothing.
4. **In-memory awaiting filter:** if you extracted the filtering into a pure method (recommended, put it in a small static helper so it can be tested), assert search, client, driver, truck and date-range filtering on a hand-built list of `AwaitingReportRow`.

Run the Verify project (see `Verify/Program.cs` for how). All checks, old and new, must pass. If an old check fails, work out whether your change caused it. Do not delete or weaken existing checks.

### 5c. UI check (if you can run the app)
- Run the app in the way the project's launch config or README says. Open Operations > Trip Reports. If you cannot launch or drive the UI in this environment, say so in your report and rely on 5a/5b. Do not claim UI verification you did not do.
- If you can: confirm the Phase 1-4 "Done when" lines. Check at least a 1366 px window and the default maximised size.

---

## 6. Do not do (out of scope for this job)

- Do not change the inner trip-report form (`pnlForm`) or its validation.
- Do not change the schema, numbering scheme or `SaveTripReport` behaviour.
- Do not "fix" the Missing POD finding (#6) in code. It is a data/process question. Just report whether `GetAttachmentCounts("Dispatch")` looked correct if you happened to see it.
- Do not split `TripReportView` into new classes (#31, #32). Note it as follow-up only.
- Do not add packages or new dependencies.
- Do not run commands that modify or delete the user's database or `*.db-wal` / `*.db-shm` files.

---

## 7. Final report format (keep it short, plain language)

End with:
1. **What changed**: one line per finding number fixed (#1, #2, ...), with the file it is in.
2. **What was verified**: build result (errors/warnings), Verify run result (passed/total), and whether the UI was checked.
3. **What was not done or not verified**, honestly. Include the follow-ups left on purpose:
   - draft-on-create still burns a report number and writes an empty draft (#4/#5 design);
   - Missing POD 7 of 7 (#6) needs a data check;
   - `TripReportView` size and the two-grid swap (#31/#32);
   - any finding from the analysis you skipped, with the reason.
4. **Files touched** (from `git status`, only the ones you changed).

Do not paste code in the report. Do not commit.
