# Trip report "Dispatch order" drop-down — quick fix (Nov 9, 2026)

> **PLAN ONLY, no code yet.** One screen, one file, no database change. Small enough to build, check and commit on its own, before the bigger picker plan (`2026-11-09-trip-report-dispatch-picker.md`).

## 1. What is wrong (verified)

1. **It lists dispatches that can't be chosen.** Every completed dispatch is in the list, **including the ones that already have a trip report** ("✓ report filed"). In your screenshot at least 10 of 23 visible lines were these.
2. **Each line is too long.** `BCS0007 - BCS GROUP COMPANY (R1 - EODLAB (Ilocos Route): RR-Laoag Bacarra → RR-San Nicolas) [Completed] (DEVELA, CARLO)  ✓ report filed`. The whole route is printed and the end is cut off.
3. **No sensible order.** Oldest-waiting trips are not first.
4. **It lists not-yet-completed special trips** ("special trip not completed yet"), which can't get a report either.

Cause: `TripReportView.cs` `LoadData()`, the list is built from "Completed **or already reported** or special trip", and every line prints the full route and status.

## 2. The fix

### 2.1 What the list shows

| Show | Hide |
|---|---|
| Completed dispatches with **no finished report** | Dispatches whose report is already **Completed or Invoiced** |
| Dispatches with a **Draft** report, tagged `· draft saved` (so you can continue them) | Special trips that are **not completed** |
| Completed special trips that need a report | Cancelled and other statuses |
| **Always:** the dispatch you are editing or have just picked, even if its report is finished (so opening an old report still works) | |

### 2.2 What each line looks like

**One short line:** `Dispatch # - Client · Date · Route (+stops) · Driver`

| Before | After |
|---|---|
| `BCS0007 - BCS GROUP COMPANY (R1 - EODLAB (Ilocos Route): RR-Laoag Bacarra → RR-San Nicolas) [Completed] (DEVELA, CARLO)  ✓ report filed` | *(hidden, already reported)* |
| `DSP-20260925-001600-3208 - GOLDILOCKS CORPORATION (R2 - EODGAA (La Union Route): 204799 - RR-Agoo La Union → 200999 - …) [Completed] (…)` | `DSP-20260925-001600-3208 - GOLDILOCKS CORPORATION · Sep 25 · R2 - EODGAA (La Union Route) · RINGOR, REYNAR` |
| `ST-20261106-0003 - Special trip · ilocos [Completed] (GONZALES, ROMEO)` | `ST-20261106-0003 - Special trip · Nov 6 · ilocos · GONZALES, ROMEO` |

- The route uses the app's existing short form (`CleanRouteDisplay`: the route name, or "first stop (+N stops)").
- **The line still starts with `"{Dispatch #} - "`**, because the form finds the chosen dispatch by that text in about a dozen places. This is the one thing that must not change.

### 2.3 Order and size
- **Oldest first**, so the trip that has waited longest is at the top.
- At most **10 rows visible**, then it scrolls (today it opens a wall of 23+).
- Empty list message: **"Every completed dispatch already has a trip report."** (instead of the old wording).

### 2.4 The one trap I found, and its fix
When a saved draft is restored, the form finds the dispatch by number. **Older drafts only stored the list position** (`DeliveryIndex`). With the shorter, filtered list, that position would point at a **different dispatch**, and the draft's typing would land on the wrong trip.
→ **Fix:** ignore the position fallback. An old draft without a dispatch number opens blank (the draft banner still offers it, nothing is lost on disk). Drafts saved with a dispatch number keep working.

## 3. What does NOT change (why it is safe)

- Saving a report, the report numbers, the stops, the crate maths: untouched.
- The "Awaiting report" list and the "Awaiting a report" tile: untouched.
- Billing, statements, collections: untouched.
- The database: no new column, no migration.
- The **Keep for later** pop-up added today: untouched.

## 4. Files

| File | Change |
|---|---|
| `Views/TripReportView.cs` | `LoadData()` (about lines 1062–1096): the filter, the line text, the sort, `MaxDropDownItems`, the empty message. Draft restore (about line 1827): drop the position fallback. About 25 lines in all. |

## 5. Checks before it is committed

**Automatic (`Verify`):**
1. A dispatch with a **Completed report** is **not** in the list; one **without a report** is; one with a **Draft** is, tagged.
2. A **not-completed special trip** is not in the list.
3. Order is oldest first; every line starts with `"{Dispatch #} - "` and is under about 140 characters.
4. Opening an **existing** report still selects its dispatch (it is kept in the list while editing).
5. Restoring an old draft with no dispatch number does **not** select any dispatch.
6. No error pop-up on the screen.

**By you, 2 minutes:** open Trip reports → **+ New** → open the drop-down: only waiting trips, short lines, oldest first. Pick one: its route, truck and crew load. Open an old report from the list: its dispatch shows.

## 6. What if

| What if | Behaviour |
|---|---|
| You open a finished report to edit it | Its dispatch is kept in the list, so it shows as before. |
| A report is Draft | The dispatch is listed with `· draft saved`; picking it creates no second report. |
| A dispatch was completed after you opened the screen | It appears after the next refresh (the list is rebuilt on every load). |
| Two dispatches look the same | The number and date at the start tell them apart. |
| There are 300 waiting dispatches | Still a long list. That is what the **picker with search** (the bigger plan) is for. This fix removes the already-reported ones and the long text, which is most of the noise today. |
| A client name is typed two ways (BUTTERWORLD / butter world) | Shown as typed. The fix for that is in the billing plans, not here. |

## 7. Not in this fix (later, separate)
- Search box and client filter, tick boxes and **several dispatches at once** (same client only): `2026-11-09-trip-report-dispatch-picker.md`, steps 2 and 3.
- Combining two orders into one multi-drop order: `2026-11-09-manual-combine-dispatch-orders.md`.

## 8. Decisions (only two)

| # | Question | Recommendation |
|---|---|---|
| D1 | Hide dispatches that already have a finished report? | **Yes.** They cannot get another report. Editing an old report still works from the reports list. |
| D2 | Order: oldest first or newest first? | **Oldest first.** The oldest waiting trip is the one most likely to be forgotten. |

Reply **"use recommended"** and I build it, run the checks, and commit.

---

## 9. Built (Nov 9, 2026)

Done as planned, with one addition found while testing:
- **Added:** when a report is opened for editing, its trip is added to the list (so the form can show it). The grid also auto-selects the first report on load, which would have left that one finished trip in the list of a *new* report. Starting a new report now takes any such added trip out again (`DropAddedPickItems`).
- **Checks** (`Verify/Checks.TripReportPicker.cs`): list content (waiting, draft, special trip in; finished report, not completed, special not completed out), oldest first, short lines, draft tag, opening a finished report still shows its trip, list clean again after "+ New", empty message. Both pass; the screen checks on a copy of the real data and the Keep-for-later checks still pass.
- **Not automated:** restoring an old draft that only had a list position (it now opens blank by design, section 2.4).
