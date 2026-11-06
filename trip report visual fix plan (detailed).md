# Trip Reports: visual fix plan (layout built around the real data)

Status: **plan only. Nothing has been changed.** This covers the visuals of Operations > Trip Reports, opening on the **Awaiting report** tab (your second screenshot).
Companion files: `trip report form analysis.md` (what's wrong) and `trip report fix instructions for sonnet 5.5.md` (logic fixes). This plan is the *look and layout* half. Where it touches logic (tab-specific cards, filters), it only does what the layout needs and defers to the instructions file for the rest.

---

## 1. What the real data looks like (this drives every size below)

I queried a **copy** of the live database (`C:\ProgramData\MJ Billones Transport\TDMS\mj_billones.db`). The live file was not touched.

| Field | Real values | Consequence for the layout |
|---|---|---|
| Dispatch # | 4 to 25 chars: `8555`, `BUT0002`, `BCS0006`, `DSP-20260925-001600-3208` | Needs a column wide enough for 25 chars (about 175 px), or it ellipsises with a tooltip. Today's 120 px clips every `DSP-…` number. |
| Date | 2026-09-24 to 2026-11-07 among the dispatches awaiting a report | Age spans **0 to 44 days**: 6 of the 9 waiting trips are 0 to 2 days old, **3 are 42 to 44 days old** (Sep 24 to 26). An age indicator is meaningful, and it would have caught those three. |
| Client | Only **5 distinct** (BCS GROUP COMPANY ×8, GOLDILOCKS CORPORATION ×4, BUTTERWORLD ×2, `butter world` ×1, RED RIBBON BAKESHOP INC. ×1). Longest is 24 chars. | A client combo is small, but it must be about 210 px wide. Note `butter world` vs `BUTTERWORLD` is the **same client typed two ways**. |
| Destination | 15 to **177 chars, average 89**. Multi-stop routes look like `R3 - Route 3: Malolos City Store ➔ Marilao Central Mall ➔ … ➔ Bulacan North Depot`. Multi-drop shortcut looks like `mariveles (+2 stops)`. Single stops look like `BTW1 - manila: mandaluyong`. | **No single-line column can ever show this.** It needs a two-line route cell (label plus summary) with the full route in a tooltip. This is the biggest layout problem. |
| Truck | Always 25 chars: `CCE 9458 (FORWARD 4 TONS)` | Split into plate and type for a compact two-line cell. |
| Driver | 13 to 22 chars, ALL CAPS, 11 distinct (`DEL ROSARIO, JHON PAUL`, `CAMPANILLA C.`) | About 170 px. ALL CAPS reads as shouting. |
| Report state | 16 completed dispatches in my copy; 8 trip reports (5 Completed, 3 Invoiced) | Only 2 states appear in this list: "No trip report yet" and "Draft saved". |

Caveat: my copy shows **9** dispatches without a finished report, while your screenshot shows **15** on the card (and 7 reports where I count 8). The screenshot was taken at a different moment or against different data, so I can't reconcile them. The mismatch itself supports finding #1 of the analysis (card and table use different rules), but I haven't proven the cause.

The screenshot is about 1918 px wide at roughly **125% DPI** (row pitch ~51 px for a 40-logical-px row). The grid has about 1690 physical px, or **about 1350 logical px**, to work with. That's the number the plan is sized to.

---

## 2. Why it looks wrong today (visual root causes only)

1. **Width budget is overspent.** Minimum widths sum to about 1590 logical px (120+100+180+230+190+170+170+240+190), which at 125% is about 1990 px against 1690 available. The grid therefore opens scrolled right and the Dispatch #, Date and Client columns are off-screen.
2. **Two columns are mostly dead space.** "Trip report" repeats the tab name and "Report #" is empty on most rows, yet together they take about 410 logical px.
3. **The route can't fit a column.** See the table above.
4. **Red everywhere.** Every row has red text, so colour carries no information.
5. **The selected row is red on red.** The theme accent is red, so the selection tint (`GridSelection`) is red and the red "No trip report yet" text on top of it is hard to read. The code also forces white bold text only on that row.
6. **The button ignores the theme.** It uses hard-coded pastel colours, which glare on dark. The selection tint also stops before the button column.
7. **Native controls on a custom-drawn page.** The combos are flat square white-on-black boxes next to rounded chips. Heights and radii differ across the tab buttons (30), chips and combos.
8. **The search box and its clear button are mis-sized.** The placeholder is clipped and the "X" is a separate 70 px button.
9. **A bright white square sits at the scrollbar corner.** It exists only because both scrollbars are visible (see 1).
10. **Tooltips are off by design.** `UIThemeHelper.ApplyGridTheme` sets `ShowCellToolTips = false`, so truncated text can't be read.

---

## 3. Target design

### 3.1 Awaiting grid: columns (logical px at 100%, always through `Dpi()`)

| # | Column | Content | Width | Notes |
|---|---|---|---|---|
| 1 | **Dispatch** | line 1: dispatch # (semibold). line 2: `Nov 6 · 1 day ago` (muted, small) | min **160** | Replaces the separate Date column. Ellipsis plus tooltip for `DSP-…` ids. **Frozen** so it stays visible if the user scrolls sideways. |
| 2 | **Client** | single line, client as stored | min 150, fill weight 60 | Tooltip when truncated. |
| 3 | **Route** | line 1: route label (`R3 · Route 3`, or the single destination). line 2 (muted): `Malolos City Store ➔ … ➔ Bulacan North Depot · 9 stops` | min **260**, fill weight 100 | Absorbs all spare width. Tooltip shows the full route as a numbered stop list. |
| 4 | **Truck** | line 1: plate `CCE 9458`. line 2 (muted): `Forward 4 tons` | min 140 | Falls back to the raw text if it doesn't parse. |
| 5 | **Driver** | single line | min 160 | Display-only title case is optional (see 3.6). |
| 6 | **Report** | status pill plus, for drafts, the report number in muted text | min 150 | Merges "Trip report" and "Report #". |
| 7 | **Action** | themed button, right-aligned | fixed **170** | Frozen at the right edge if scrolling occurs. |

Budget: minimums sum to **1,170** logical px against about 1,350 available, so **no horizontal scrollbar** at the screenshot size. Route and Client take the spare ~180. Below about 1,170 logical px the grid scrolls with Dispatch and Action pinned.

Row height: `Dpi(48)` (two lines at 9.25 pt plus 8.25 pt). `ApplyGridTheme` only grows rows, never shrinks, so this survives.

### 3.2 Row design (sketch, one row at 1350 px)

```
| BCS0003              | BCS GROUP COMPANY | R3 · Route 3                                  | ICC 8370      | Capiendo,     | ● Needs report   | [ Create trip report ] |
| Nov 5 · 2 days ago   |                   | Malolos City Store ➔ … ➔ Bulacan North Depot  | Forward 8 tons| Petronillo    |   Overdue? no    |                        |
|                      |                   | · 9 stops                                     |               |               |                  |                        |
```

### 3.3 Status pill and age colour (replaces "red on every row")

| Condition | Pill | Colour source |
|---|---|---|
| No report, age 0 to 2 days | `Needs report` | Neutral: muted text on the `SurfaceAlt` tone |
| No report, age 3 to 6 days | `Needs report · 4d` | Amber tone |
| No report, age 7+ days | `Overdue · 14d` | Red tone |
| Draft saved | `Draft saved` plus report # beneath | Amber tone |

- Thresholds live in two named constants at the top of the class (`AmberAgeDays = 3`, `RedAgeDays = 7`). With real ages of 0 to 44 days this gives a real mix instead of a wall of red: today 6 rows would be neutral and 3 red (none amber).
- Pills are **filled with their own background and text colour**, so they stay legible on a selected row (this removes the red-on-red problem without touching the app-wide selection colour).
- Colours come from the existing helpers `SummaryTones.Soft(tone)` / `Ink(tone)` in `Functions/SummaryPanels.cs` (lines 34-36), which already handle dark vs light, and from `AppTheme` (`Danger*`, `Warning*`, `TextMuted`, `SurfaceAlt`, `Border`). **No new hard-coded colours.**

### 3.4 Row states and button
- **Hover:** row background `AppTheme.GridHover` (via `CellMouseEnter` / `CellMouseLeave`, invalidating the row). The cursor becomes a hand over the button only.
- **Selected:** keep `AppTheme.GridSelection` (app-wide consistency). The row's background is painted across **every** cell, including the button cell.
- **No more per-row forced text colour.** Remove the `CellFormatting` override that makes the selected row white-bold and the others red.
- **Button:** a filled, rounded rectangle in the theme accent (`Create trip report`) or the amber tone (`Open draft`), with distinct hover and pressed shades derived from the theme, and a visible focus ring. Both themes are checked.
- **Cached fonts:** one regular, one bold, one small-muted `Font`, created once per grid and disposed with the control (this also fixes the GDI leak from finding #28).

### 3.5 Header, cards, filter bar

**Header.** In Awaiting: `Awaiting a report` (large) with a muted subtitle `15 completed trips still need a trip report` (the count comes from the same list as the grid). Active/History keep the existing text.

**Cards.** See section 3.5a below. The short version: the tiles must describe the list the user is looking at, use plain words, and do something sensible when clicked.

**Tabs and buttons.**
- One height for the tab buttons, `New trip report`, `Official trip report`, `Actions` and the combos: **36 logical px**, same corner radius.
- `New trip report` stays the single primary (red). `Official trip report` drops from a hard-coded pastel to the secondary role. Check how `UIThemeHelper.SetButtonRole` renders it and use that.

**Search.** Width about 280. Placeholder per tab: Awaiting reads `Search dispatch #, client, driver, truck…`; Active/History keep `Search report #, dispatch…`. The clear "X" becomes a small glyph **inside** the box, visible only when there is text. The detached 70 px X button is removed.

**Combos.** Restyle to match the chips: flat, `InputBack` background, theme border, rounded, same height.
- Widths from the real data: **Client 210, Driver 190, Truck 150** (the old 170/150/120 clip `GOLDILOCKS CORPORATION` and `DEL ROSARIO, JHON PAUL`).
- Set `DropDownWidth` by measuring the longest item, so the open list shows `CCE 9458 (FORWARD 4 TONS)` in full while the closed box shows the plate.
- In Awaiting mode the **"All reports" combo is disabled** with a tooltip (it filters reports, not dispatches).

**Reset filters.** Same size and style as the chips. (Removing its confirmation dialog is in the logic file.)

### 3.5a The tiles: why they don't make sense, and the fix

**What you see today (Awaiting tab):** `Trip reports 7`, `Drafts 0`, `Missing POD 7`, `Awaiting a report 15`, sitting above a table of 15-ish dispatches that need a report.

**Why that fails common sense** (confirmed in `TripReportView.Center.cs:34-50, 227-243`):

1. **Wrong list.** Three of the four tiles count *trip reports* (7). The table under them is *dispatches with no report* (15). Looking at "7" above a table of 15 rows reads as a bug.
2. **The tile that matters is last and says the least.** "Awaiting a report 15" is the whole point of this tab, but it is the fourth tile, in the same colour as the unrelated ones, with the note "click to start one", which doesn't describe a number.
3. **"Missing POD 7" has no "of".** 7 of 7 reports have no POD. Shown alone it looks like a minor warning; as "7 of 7" it is an emergency. Also, POD belongs to a *report*, but this tab is about trips that have *no report yet*, so POD can't even exist for them.
4. **"Drafts 0" contradicts the table.** The table can show rows marked "Draft saved", yet the tile says there are 0 drafts, because the tile counts drafts only inside the selected period and ignores the awaiting list. Two places count the same thing differently.
5. **"Trip reports 7, in the selected period"** repeats the old header. The period is "All dates", so "in the selected period" is meaningless noise.
6. **Clicking does nothing visible.** On this tab, clicking *Drafts* or *Missing POD* changes a filter on the **hidden** report grid (`cbShowF` on `gridTripReports`), so the tile highlights and the table stays the same. *Awaiting* opens a pop-up list of 25 dispatches, which duplicates the table you are already looking at.
7. **Colour means nothing.** Orange dots and orange numbers appear on tiles that are fine (zero drafts) and on tiles that are urgent alike. Nothing tells you which one needs action.
8. **Jargon and units.** "POD", "Drafts", "Awaiting a report" with no plain-language line telling a dispatcher what to do about it.

**Fix: tiles follow the tab, and every tile answers one plain question.**

Awaiting tab (the screen you showed):

| Tile | Big number | Plain line under it | Click does |
|---|---|---|---|
| **Need a report** | all rows in the table | `completed trips with no finished trip report` | Clears the tile filters (shows every row) |
| **Overdue** | rows waiting 7+ days | `waiting a week or more` (number turns red only when > 0; otherwise the muted `None, you're caught up`) | Filters the table to those rows |
| **Draft saved** | rows whose state is "Draft saved" | `started but not finished` | Filters the table to those rows |
| **Waiting longest** | `44 days` (or `—` when empty) | `since Sep 24` | Selects and scrolls to that row |

- The first tile is the **primary** one: first position, accent colour, and its number always equals the table row count (same list, so they can't disagree).
- Overdue and Draft come from the **same rows** as the table, which fixes the contradiction in point 4.
- **POD does not appear here at all.** It can't apply to trips with no report.
- Clicking filters the table that is on screen (point 6), and the active tile shows a clear "selected" state with a "Clear" affordance.
- Colour rule: **grey/neutral = fine or zero, amber = needs attention, red = overdue.** A zero is never coloured.

Active and History tabs (reports exist, so these are valid there):

| Tile | Big number | Plain line | Click does |
|---|---|---|---|
| **Reports** | count in the current filters | `trip reports in this list` (drop "in the selected period") | Clears tile filter |
| **Drafts** | drafts in this list | `saved, not finished` | Filters |
| **No proof of delivery** | `7 of 7` (count of total, always shown as "of") | `reports with no POD file attached` (spell out POD once here) | Filters |
| **Incidents** | reports with a linked incident | `trips with damage or incident logs` | Filters |

- "Missing POD" becomes **`7 of 7`**, with red when the share is high and a neutral tone when it's 0. The "of N" is what makes the number readable.
- The old `Awaiting a report` tile moves to the Awaiting tab; on Active/History it would just be a shortcut, so I'd add a small `Awaiting: N →` link on the tab button instead (`Awaiting report (9)`). That also gives the tab itself a live count.

**Tab label counts (small, useful):** `Awaiting report (9)`, `Active (5)`, `History (3)`, so the user sees the sizes without reading tiles.

**Real-data check:** on today's copy of the database this reads Need a report **9**, Overdue **3** (the Sep 24 to 26 dispatches), Draft saved **0**, Waiting longest **44 days**. The numbers match the table by construction. I'll confirm with a Verify check (tile counts equal row counts on the real-data copy) rather than eyeballing it.

**Implementation notes:**
- Tiles are built once in `InitReportCenter` (`Center.cs:33-50`) and refreshed in `UpdateCenterTiles` (`Center.cs:227-243`). I'll build **two tile sets** and swap them in `SetAwaitingMode()` / `SetHistoryMode()` rather than mutating one set, so a stale tile can never show the wrong scope.
- Replace the `FilterChanged` handler's `cbShowF` hack with a per-tab handler: awaiting tiles filter `awaitingRows`; report tiles keep today's `cbShowF` behaviour.
- Remove the `ShowAwaitingMenu()` pop-up (the table is the list) once the Awaiting tab exists. This also drops the 25-item cap, which goes with it.
- The unused `tDelivered` / `tReturnRate` tiles stay unused; I won't revive them.

### 3.5b Tile look: light gray

**Where the look comes from today.** `StatTile` (`Functions/PageKit.cs:201-291`) paints itself: fill is `AppTheme.Surface` (so near-black in your dark theme), hover is `AppTheme.SurfaceHover`, selected is `Mix(Surface, Accent, 0.07)` with a 2 px accent border, and all text colours come from `AppTheme` (`TextPrimary`, `TextSecondary`, `TextMuted`). That is why the tiles blend into the dark page.

**The change: tiles become light gray cards with dark text.**

| Part | Colour (RGB) | Why |
|---|---|---|
| Resting fill | **232, 234, 237** | The light gray you asked for |
| Hover fill | 222, 225, 229 | A step darker so the click affordance is visible |
| Selected fill | gray mixed 10% with the tile's accent, plus the 2 px accent border | Same selected behaviour as today, readable on gray |
| Border | 209, 213, 219 (1 px) | Soft edge, no heavy outline |
| Caption | 55, 65, 81 | About 8:1 on the fill |
| Big number | 31, 41, 55 | About 12:1 |
| Note line | 75, 85, 99 | About 6:1 (the old muted tone would be about 4:1 and too faint on gray) |
| Accent dot | the tile's accent (grey, amber, red) | Keeps the "needs attention" signal |
| Number colour when urgent | amber 146, 64, 14; red 185, 28, 28 | The current `AppTheme.WarningText` is tuned for a dark background and goes pale on light gray, so the tiles get their own darker tones |

**How it is applied, safely.** `StatTile` is shared by other screens, so I would **not** change its default look. I add one opt-in switch (`Minimal = true`, which also carries the compact size from 3.5c) that swaps the colour set above, and turn it on only for the Trip Reports tiles. Other screens stay exactly as they are. If you like the result I can switch it on for the rest later.

**Things to know:**
- On the dark theme these become the **brightest elements on the page** (light cards on a dark background). That is what "light gray" means there. If it feels too glaring, the fallback is a mid-light gray (around 206, 210, 215), same text colours. I would show you both in the "after" screenshots rather than guess.
- On the light theme (white surface) the light gray cards sit naturally.
- The fixed colours live as named constants in one place in `StatTile`, not scattered, so changing the shade later is one edit.
- The tile numbers and text use theme-independent dark colours, so legibility does not depend on the user's theme choice.
- Both themes are included in the verification screenshots (section 5).

### 3.5c Tile details: smaller and minimal (this section sets the size and wording; 3.5b sets the colours)

**Goal:** each tile says one thing in about two seconds. Less height, less decoration, short wording; the longer explanation moves into a hover tooltip.

**Size.**

| | Today | New |
|---|---|---|
| Strip height | 96 | **64** logical px (about 30 px back for the table, roughly half a row) |
| Tile width | stretched to ~410 px each (4 equal columns across the page) | **fixed ~200**, left-aligned, 8 px gap (about 820 px total, so tiles stop looking like empty banners) |
| Padding | 16 | **12** |
| Corner radius | 10 | **8** |
| Caption font | Segoe UI Semibold 9 | Semibold **8.5** |
| Number font | Segoe UI Semibold 19 | Semibold **17** |
| Note font | Segoe UI 8.5 | Segoe UI **8** |
| Border | 1 px, 2 px when selected | **1 px always**; selected = darker gray fill plus an accent-coloured 1 px border |

**Layout inside a tile (two lines, no dot, no third line):**

```
Need a report                 <- caption, 8.5 pt
9   completed trips           <- number 17 pt, then the note 8 pt on the same baseline, ellipsised
```

**Minimal decoration.**
- The accent dot is **removed**. A thin 3 px accent bar on the tile's left edge appears **only** when the tile needs attention (amber or red). Neutral tiles show nothing. That way a mark means something.
- No icons, no second figure, no shadows.
- All fonts are created once and reused (the current code builds new fonts on every repaint).

**Exact content: Awaiting tab** (real numbers from today's copy of the data in brackets):

| Caption | Number | Note | Mark | Tooltip (hover) |
|---|---|---|---|---|
| **Dispatches** | `9` | `need a report` (`all caught up` at 0) | none | Completed regular dispatches with no finished trip report (nothing started, or only a draft). Click to show only these. |
| **Special trips** | `3` | `need a report` (`none waiting` at 0) | none | Completed special trips with no finished trip report. Click to show only these. |
| **Overdue** | `3` | `7+ days waiting` (`none overdue` at 0) | red bar when > 0 | Completed 7 or more days ago and still without a finished report. Click to show only these. |
| **Draft saved** | `0` | `started, not finished` | amber bar when > 0 | A trip report was started but not completed. Click to show only these. |
(To keep four tiles, **Waiting longest** is dropped. The age is already visible on each row. "Overdue" counts dispatches and special trips together.)

**Exact content: Active and History tabs:**

| Caption | Number | Note | Mark | Tooltip |
|---|---|---|---|---|
| **Reports** | `8` | `in this list` | none | Trip reports in the list below. Click to clear any tile filter. |
| **Drafts** | `0` | `saved, not finished` | amber bar when > 0 | Drafts in the list below. |
| **No proof of delivery** | `7` | `of 8 reports` | red bar when it is more than half | Reports with no proof-of-delivery file attached. |
| **Incidents** | `n` | `with damage or incident log` | amber bar when > 0 | Reports whose trip has an incident or damage log. |

**Wording rules.**
- Caption: 1 to 3 words, plain language ("proof of delivery" spelled out, not "POD").
- Note: 22 characters or fewer, never a command ("click to start one" is gone). The click hint lives in the tooltip.
- Numbers are never decorated: no colour on a zero, no unit inside the figure except `44 days`.
- Loading shows `—` and an empty note. A failed load shows `—` and `couldn't load` (and the table's own error state appears too).

**How it is built.**
- It is the same opt-in switch as 3.5b. One property on `StatTile`, **`Minimal = true`**, switches on the compact size, the new layout and the light gray colours together, so Trip Reports tiles change and every other screen's tiles keep their current look.
- `StatTile.OnPaint` currently uses fixed y positions for the 96 px layout (`S(10)`, `S(32)`, `S(68)`). The `Minimal` path gets its own small layout branch. The old branch stays untouched so other screens are not affected.
- `TileStrip` sets its height to 96 on handle creation. It gets a matching `Compact` option so Trip Reports can use 64, with absolute 200 px columns instead of equal percent columns.
- Tooltips use one `ToolTip` shared by the strip, and the text is set from `UpdateCenterTiles` (together with the numbers, so a tooltip can never disagree with its tile).

**Verify.** Check the tile numbers against the table rows on the real-data copy (9 / 3 / 0 / 44 days today), then screenshot both themes at 100% and 125% DPI. No caption, number or note may clip at the 200 px tile width (long values such as dispatch ids are kept out of the tiles on purpose).

### 3.6 Optional polish (off unless you say yes)
- **Display-only title case** for driver and client (`Campanilla C.`, `Del Rosario, Jhon Paul`) to stop the shouting. Stored data stays as is. Risk: names with unusual capitalisation; tooltip would show the stored text.
- **Client casing clean-up** (`butter world` vs `BUTTERWORLD`): a data fix, not a visual one. I'd flag it and leave it unless you want a one-time cleanup script.

### 3.7 Empty, loading and error states
- **Loading:** a centered muted `Loading…` label over the grid while `LoadAwaitingGrid` runs, so the user never sees a stale or blank table.
- **Empty:** `Every completed trip has a trip report.` (no rows at all) or `No completed trips match these filters.` (filters hid them), with a Reset filters link.
- **Error:** `Couldn't load the list.` with a Retry button; the toast stays.
- All three use theme colours and sit over the grid, not in a modal.

### 3.8 Scrollbar corner
With the width budget fixed, the horizontal scrollbar does not appear at normal sizes, so the white corner disappears. For narrow windows where both bars show: call the existing `ThemeManager.StyleNativeControl` path and, if the corner is still bright, overlay a small themed `Panel` at the bottom-right of the grid. This is a known WinForms workaround and may need a quick visual check.

### 3.9 Tooltips
`ApplyGridTheme` turns `ShowCellToolTips` off and would re-assert that on any re-theme. So: set `ToolTipText` per cell in `CellFormatting` for Dispatch, Client, Route and Truck (full text, and the numbered stop list for routes), set `ShowCellToolTips = true` **after** `ApplyGridTheme`, and re-apply both in the existing `ThemeManager.ThemeChanged` handler (`TripReportView.cs:97`).

---

## 4. Implementation plan (in order, one verifiable step each)

| Step | What I do | Files | Check |
|---|---|---|---|
| 0 | Run `git status`, note the files already modified, never touch those edits. Locate the existing screenshot tool in `_tools\FormScreenshotter` and the `Verify` harness. Capture a **"before"** screenshot of this tab on the real data copy. | none | before image saved |
| 1 | Add small pure helpers: `ParseRoute(string)` (label, first, last, stop count, full list), `ParseTruck(string)` (plate, type), `AgeDays(date)` and `AgeTone(days, hasDraft)`. Cover them with Verify checks using the real strings from section 1. | new static helper in `Views/` (or next to `AwaitingReportRow`), `Verify/Checks.*.cs` | Verify passes. Routes with `➔`, with `(+2 stops)`, and with no separator all parse; garbage falls back to raw text. |
| 2 | Rebuild the grid columns as in 3.1 (merge Date into Dispatch and Report # into Report, Fill weights, freeze Dispatch and Action, row height). Set the layout in one `ApplyAwaitingLayout()` method and call it after every `ApplyGridTheme` and on theme change (otherwise `ApplyGridTheme` re-picks Client as the Fill column). | `Views/TripReportView.Workflow.cs` | No horizontal scrollbar at 1918 px / 125%. |
| 3 | Custom cell painting: two-line cells, status pill, hover, selection across all cells, themed button, cached fonts. Remove the per-row red/white `CellFormatting` override. | `Views/TripReportView.Workflow.cs` | Selected row is legible in dark and light; no `new Font` inside paint handlers. |
| 4 | Tooltips (3.9). | same | Hovering a `DSP-…` id and a long route shows the full text. |
| 5 | Tab-specific header, tab-label counts and the two tile sets (3.5 and 3.5a), with a Verify check that tile counts equal row counts. | `Views/TripReportView.Center.cs`, `Views/TripReportView.cs` | Awaiting cards match the grid rows. |
| 5b | Light gray, smaller, minimal tile look (3.5b and 3.5c): add the opt-in `Minimal` switch to `StatTile` (compact layout plus light gray colours), a compact option on `TileStrip`, tooltips, and enable it for the Trip Reports tiles only. | `Functions/PageKit.cs`, `Views/TripReportView.Center.cs` | Tiles are 64 px high, about 200 px wide, light gray with dark legible text in both themes; no other screen changed (compare one other screen's tiles before and after). |
| 6 | Filter bar: unified heights, restyled combos with real-data widths and measured drop-downs, search width and inline clear, disabled "All reports" in Awaiting. | `Views/TripReportView.Center.cs`, `Views/TripReportView.Designer.cs` (only if the search box is declared there) | Long client/driver/truck names are fully readable in the dropdown. |
| 7 | Loading, empty and error states (3.7). | `Views/TripReportView.Workflow.cs` | Force each state once. |
| 8 | Scrollbar corner (3.8) and a final pass on spacing and alignment. | same | No white square at any size. |
| 9 | Take **"after"** screenshots (see 5) and compare. | none | all checks below |

I stop and report at the end of step 9. I will not commit.

---

## 5. How I will verify (not just "it builds")

1. **Build:** 0 errors and 0 warnings (the project builds warnings-as-errors).
2. **Verify harness:** all existing checks plus the new helper checks pass. I run it on a **copy** of the real database, never on the live file.
3. **Screenshots on the real data copy**, via `_tools\FormScreenshotter` (or the built app plus computer-use if that tool can't open this view). I check:
   - 1918 px wide at 125% DPI (your screenshot): no horizontal scrollbar, Dispatch, Client and Route visible.
   - About 1366 px wide: still no clipped Dispatch or Action columns.
   - **Dark and light themes**: pills, button, selected row, combos.
   - Rows with the longest real route (`BCS0003`, 177 chars), the longest dispatch number (`DSP-20260925-001600-3208`), and a multi-drop (`mariveles (+2 stops)`).
   - Empty and loading states.
4. **Honesty rule:** if I cannot run or capture the UI, I say so and do not claim the visual checks passed.

---

## 6. Out of scope here

- The logic fixes (single source for the Awaiting count, filters wired to the awaiting rows, no-write-on-click, GDI font disposal beyond this grid): see the instructions file. Where steps overlap, I do it once and say so.
- The inner trip-report form.
- Any schema, numbering or stored-data change.
- Splitting `TripReportView`.

## 7. Risks

- **Custom painting of two-line cells** is the riskiest part. It touches `CellPainting`, which also runs in the theme helper's own handler (`UITheme.cs` header painting). I only handle row cells in my handler and leave header painting alone.
- **`ApplyGridTheme` fights the layout** (it picks the Fill column and re-enables `ShowCellToolTips = false` on every call), hence the single `ApplyAwaitingLayout()` re-applied after theming.
- **Tab-specific cards** change the screen's behaviour a little. Easy to revert (the Active/History set is unchanged).
- **DPI:** every number goes through `Dpi()`; I'll test 100% and 125%, and 150% if the screenshot tool allows.

## 8. Decisions I'll default on unless you say otherwise

| Decision | Default |
|---|---|
| Merge Date into the Dispatch cell and Report # into the status pill | **Yes** |
| Age thresholds (amber 3 days, red 7 days) | **3 / 7** |
| Tab-specific tiles (Need a report / Overdue / Draft saved / Waiting longest), POD removed from the Awaiting tab, `7 of 7` wording, tab counts | **Yes** |
| Display-only title case for names | **No** |
| Tile colour | **Light gray (232, 234, 237) with dark text, Trip Reports tiles only**, in both themes; mid-light gray as fallback if too bright on dark |
| Overlay panel for the scrollbar corner | **Only if the white square still shows after the width fix** |
