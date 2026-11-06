# Trip Reports screen: brutal analysis

Scope: the screenshot of Operations > Trip Reports (opens on "Awaiting report") plus the code behind it:
`Views/TripReportView.Workflow.cs`, `Views/TripReportView.Center.cs`, `Views/TripReportView.cs`, `Database/MJdatabase.AwaitingReport.cs`.
Nothing in the system was changed. This is a read-only review.
I did **not** audit the inner trip-report editing form (`pnlForm`) line by line. This covers the list/entry screen you showed me and the logic feeding it.

Severity: **P0** wrong data or data-integrity risk, **P1** broken or misleading behaviour, **P2** UX or visual defect, **P3** code health.

---

## The one-line verdict

The screen says "Trip reports, showing 4 of 7" and the four cards describe trip reports. The table underneath is a different list (dispatches awaiting a report), and one card (Awaiting = 15) is computed by different rules from that table. It looks like one coherent screen but it is two screens glued together, and the numbers don't reconcile.

---

## P0: Wrong numbers and data integrity

### 1. The "Awaiting a report: 15" card and the "Awaiting report" table use different logic, so they can disagree
- **Card** (`TripReportView.Center.cs:151`): `DispatchGate.IsCompleted(d.Status)` AND the dispatch number is not in the set of **all loaded trip reports** (any status, matched by dispatch *number string*).
- **Table** (`MJdatabase.AwaitingReport.cs`): `Status='Completed' COLLATE NOCASE` AND no non-void report with status Completed/Invoiced (matched by **DeliveryId**).
- Consequences:
  - A dispatch with only a **Draft** report is **in the table** but **not in the card count**.
  - A dispatch whose only report is **Void** is **in the table** but excluded from the card, **if** `allTripReports` includes voided rows. I didn't verify that.
  - Two completed dispatches with the same dispatch number collapse into one in the card (`GroupBy(...).First()`) but stay separate in the table.
  - The "completed" test is written twice (a C# helper and a SQL string). They will drift apart.
- **Fix:** one source of truth. Use the SQL result for the card and the table, or run the same predicate for both. Delete the in-memory duplicate.

### 2. The header and the three left cards lie while "Awaiting report" is selected
- The header "Trip reports, showing 4 of 7" and the cards "Trip reports 7 / Drafts 0 / Missing POD 7" describe the **trip-report grid**. That grid is **hidden** (`SetAwaitingMode` sets `gridTripReports.Visible = false`).
- You are looking at 6+ dispatch rows under a heading that says 4 of 7.
- **Fix:** the header, cards, filters and search must follow the active tab, or the cards must be labelled as "all reports" and the tab must have its own header such as "15 completed trips need a report".

### 3. Every filter control is dead in the default view
- Date, client, driver, truck, "All reports", sort, search and "Reset filters" all act on `gridTripReports` (`ApplyCenter`, `ApplyActiveOrHistory`, line ~1595).
- The awaiting grid is bound straight from `GetDispatchesAwaitingReport()` with a hard-coded `ORDER BY d.DispatchDate DESC`.
- The screen opens on this view, so **the first thing a user does (pick a driver, search a dispatch #, change the date) does nothing** and gives no feedback.
- "Sort: Newest first" is not the sort of the visible table. Clicking Reset prompts a confirmation and changes nothing visible.
- **Fix:** apply the same filter/search/sort to the awaiting rows, or disable and grey out the controls that don't apply, with a tooltip explaining why.

### 4. "Create trip report" writes a Draft to the database with no confirmation
- `CreateTripReportForDispatch` immediately calls `SaveTripReport`, which inserts a Draft and **burns a trip report number**.
- **Double-clicking any cell in the row** (`CellDoubleClick`, Workflow.cs:268) also triggers it. A stray double-click on a driver name creates a DB record.
- Result: abandoned empty drafts, gaps in report numbering, and a dispatch that flips to "Draft saved" the moment someone was just curious.
- **Fix:** don't write on click. Open the form pre-filled from the dispatch and assign the number only on first save. At minimum, remove the double-click-creates behaviour or confirm it.

### 5. Empty drafts count as "reported" in one place and "awaiting" in another
- An empty Draft created by the button lands in the Drafts card ("saved, not yet completed") **and** stays in the awaiting table as "Draft saved / Open draft".
- The same dispatch is counted in two buckets while the awaiting card (see #1) may have dropped it.

### 6. "Missing POD: 7" on "7" reports means 100% of reports have no proof of delivery
- Either POD upload is genuinely not being done, or `HasPod` (`fileCounts` keyed by `"Dispatch"` attachments, `Center.cs:181`) is not finding files that exist. Both are serious.
- Nothing stops a trip from being Completed and reported with no POD, and nothing in this screen shows POD status per row.
- **Fix:** verify `GetAttachmentCounts("Dispatch")` against real uploads. If they are genuinely missing, enforce POD before a report can be completed, or at least show a POD column here.

---

## P1: Broken or misleading behaviour

7. **The awaiting grid has no empty state.** If zero rows come back, the user sees a blank rectangle. The card says "all completed trips are reported" but the table says nothing.
8. **Load failure is a toast only.** `LoadAwaitingGrid` catches and toasts, then leaves a stale or empty grid with no inline retry. The toast disappears and the user is left with a blank table.
9. **Incident load failure is swallowed silently** (`Center.cs:149`: `catch { incidentCounts = new ... }`). Incident-based filters and counts will silently be wrong. Nobody is told and nothing is logged.
10. **Every create or save reloads the world.** `RefreshAfterCreate` sets `dataLoaded = false` and calls `LoadData()`, which reloads all reports, deliveries, attachment counts and incidents, then `LoadAwaitingGrid` again. No paging and no limit on the awaiting query. This gets slower as history grows.
11. **The "Awaiting a report" card opens a popup menu of 25 items** that is a *third* way to start a report. It is a different list from the table (see #1) and truncates at 25 with "… and N more" that cannot be clicked.
12. **Four entry points for the same job:** the "New trip report" button, the "Official trip report" button, the per-row "Create trip report" button, and the card menu. Their relationship is unclear. "Official trip report" in particular is ambiguous. Is it a print, a template, or a different document type?
13. **Row actions are limited to one button.** Nothing for open dispatch details, attach POD, or view the incident. The user can't see *why* a trip needs attention without leaving the screen.
14. **The tab is called "Awaiting report" and the table column is called "Trip report"** with the value "No trip report yet" in every row. The column carries the same information as the tab, so it is noise. "Report #" is empty in every row until a draft exists, so two of the eight columns are mostly dead space.

---

## P2: Visual and UX defects (visible in your screenshot)

15. **The leftmost columns are cut off.** The first visible text is "ila: mandaluyong" and "2 stops)", so the Dispatch #, Date and Client columns are scrolled off the left. The horizontal scroll thumb starts around x=675. At this window width (~1900px) a 9-column table should not need horizontal scrolling. The hard-coded column widths (120+100+180+230+190+170+170+240+190) plus DPI scaling exceed the viewport.
    - **Fix:** use proportional widths, let Destination absorb the slack, drop or merge Report #, and don't open scrolled.
16. **The most important identifier (Dispatch #) is the one that gets hidden.** The user has no way to say *which* dispatch a row is without scrolling left.
17. **Destination text is truncated mid-word** ("Malolos City Store ➜ …", "Bulacan North Depot …") with no tooltip, so routes can't be read.
18. **The Create buttons are painted in hard-coded light pink** (`Color.FromArgb(252,222,225)` in `WireAwaitingButton`) on a dark theme. They look pasted in and ignore `AppTheme`. The hover colours are also hard-coded.
19. **Red "No trip report yet" on every row** is alarm fatigue. When everything is red nothing is urgent. Use neutral text and reserve colour for overdue items such as completed 3+ days ago.
20. **The selected row looks inconsistent.** Row 1 is dark red with white bold text while the others are red text on dark. The selection highlight stops before the button column, so the button area looks detached from its row.
21. **The scrollbar corner at bottom right is a bright white square** (about 1845,936), and the scrollbars are not themed. It is the brightest pixel on the screen.
22. **The filter dropdowns are native, flat, white-on-black ComboBoxes**, square and misaligned with the rounded chips and buttons beside them. The "All dates" chip, the arrows and the Sort chip use a different visual language. The previous/next date arrows are greyed out with no explanation.
23. **The search box is cramped.** The placeholder is clipped ("Search report #, dis…") and the clear "X" is a separate, oversized button. It also searches report fields, not the awaiting rows (see #3).
24. **Reset filters asks "Are you sure?"** for a harmless, reversible action with default button No. That is friction for nothing.
25. **Missing info a dispatcher actually needs per row:** crates delivered or planned, delivery date vs today (age), whether POD exists, whether an incident is linked, and who completed it. The table has a date column, but it is off-screen.
26. **The card subtitles are inconsistent in tone and scope.** "in the selected period", "saved, not yet completed" and "click to start one". The first three scope to a date period, the fourth ignores the period entirely.
27. **The tile named "Drafts" is backed by a variable called `tShort`** and the "Missing POD" tile stays orange even when the context is irrelevant. This is leftover naming from older versions of the screen.

---

## P3: Code health

28. **GDI font leak in the grid.** `new Font(g.Font, FontStyle.Bold)` is created inside `CellPainting` (Workflow.cs:306) and `CellFormatting` (Workflow.cs:260) on every paint and never disposed. A busy grid leaks GDI handles continuously. Cache one bold font per grid and dispose it with the control.
29. **Magic column index:** `e.ColumnIndex == gridAwaiting.Columns.Count - 3` (Workflow.cs:257) means "the Trip report column". Add a column in front of the button and this silently styles the wrong column. Use the column `Name`.
30. **Fragile coupling:** the "completed" definition lives in `DispatchGate`, in raw SQL, and in the status strings the view builds. The matching is by dispatch number string in one place and by DeliveryId in another.
31. **God class:** `TripReportView` is ~1,980 lines plus ~850 designer lines plus 4 partials. Hard to reason about, which is how #1 and #2 slipped in.
32. **The grid is added at runtime and `BringToFront()`-ed over `gridTripReports`**, and the two are swapped by toggling `Visible`. Fragile z-order and focus. A single grid with a switchable data source, or a proper tab control, would remove a class of bugs.
33. **The button is custom-painted rather than a real button column**, so there is no keyboard activation, no focus rectangle, and no accessibility name for screen readers. The hover/press state lives in view-level fields (`btnHoverRow`, `btnPressRow`).

---

## Fix order (do these first)

1. **#1, #2, #3:** make the card, header, table and filters agree. One query, one scope, filters wired to the visible tab. This is what makes the screen untrustworthy today.
2. **#4, #5:** stop writing a Draft and burning a report number on click or double-click.
3. **#15, #16, #17:** fix the layout so Dispatch #, Date and Client are visible at this window size.
4. **#6:** find out whether POD really is missing across the board. That is a business-process problem, not just UI.
5. **#28, #29:** the GDI leak and the magic index. They are small, quick fixes.
6. **#18, #19, #20, #21:** theme the button, scrollbars and row states so the screen stops looking half-finished.
7. Everything else (#7 to #14, #22 to #27, #30 to #33) as follow-up cleanup.

---

## What I did not verify
- Whether `allTripReports` includes Void reports (decides how big the mismatch in #1 is).
- Whether `GetAttachmentCounts("Dispatch")` really finds uploaded POD files (#6).
- The inner trip-report form: field validation, required fields, totals/crate reconciliation and POD upload. Send a screenshot of it, or say the word, and I'll do the same analysis there.
