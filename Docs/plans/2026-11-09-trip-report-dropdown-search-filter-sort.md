# Trip report drop-down: search, filter and sort controls (Nov 9, 2026)

> **PLAN ONLY, no code yet.** Small: one screen, one section of the form, no database change. Follows the drop-down fix already committed (`98c3c84`).

## 1. You are right, here is exactly what is missing

Your screenshot shows the fix is working, but only half of what you need:

| | Status |
|---|---|
| Hides trips whose report is finished | **Done** (11 lines left) |
| Short lines, oldest first | **Done** (Sep 24 → Nov 8) |
| **Search** ("find BCS0004 / Goldilocks / Lipa") | **Missing.** A plain drop-down can only be scrolled. |
| **Filter** (one client, or only drafts, or only "no report yet") | **Missing** |
| **Sort** (choose oldest first, newest first, client, dispatch #) | **Missing.** It is fixed at oldest first. |

Also visible in your screenshot: **10 of the 11 lines say "draft saved"**. Today you cannot separate "drafts to finish" from "trips with no report at all", and with more clients the list will mix them all together. That is what the filter is for.

## 2. The solution: controls above the drop-down, same drop-down below

A single row inside card 1 ("Dispatch order"), above the drop-down:

```
Dispatch order
[🔍 Search dispatch #, client, route, driver…]  [Client: All ▾]  [Show: All waiting ▾]  [Sort: Oldest first ▾]    11 of 11 waiting
[ DSP-20260924-031329-39643 - GOLDILOCKS CORPORATION · Sep 24 · TABUK (+1 stops) · YORO, JOSE CEAZAR S.        ▾ ]
```

| Control | Behaviour |
|---|---|
| **Search** | Live, as you type. Matches anywhere in the line: dispatch #, client, date text, route, driver, "draft saved". Capital letters and spaces ignored. **Enter** picks the top match when there is one. **Down arrow** jumps into the list. |
| **Client** | "All clients" + only the clients that actually have waiting trips. |
| **Show** | **All waiting** · **No report yet** · **Drafts to finish** · **Special trips**. |
| **Sort** | **Oldest first** (default) · Newest first · Client A–Z · Dispatch # |
| **Count** | "3 of 11 waiting". When nothing matches: "No trip matches. Clear the filters." with a **Clear** link. |

The drop-down underneath stays the thing you pick from, so nothing that reads the picked trip changes. Filters only decide which lines it contains.

## 3. Rules

1. **The picked trip never disappears.** If you pick a trip and then type a search that doesn't match it, it stays in the list (and selected).
2. **Starting a new report resets the controls** to All clients / All waiting / Oldest first / empty search.
3. **When the list is reloaded** (a report was saved, Refresh), the current search, client, show and sort are **kept**, applied to the fresh data.
4. **Editing an existing report:** the controls are disabled (the dispatch can't be changed there, as the drop-down already is).
5. **Same line text as now**, so everything that finds the trip by `"{Dispatch #} - "` keeps working.
6. **Nothing is saved or changed in the database** by these controls.

## 4. How it is built (so it is quick and safe)

| Piece | Change |
|---|---|
| `TripReportView.cs` `LoadData()` | Keep the full waiting list in memory (`pickRows`: trip, line text, client, date, draft flag). A new `ApplyPickFilter()` fills the drop-down from it using the four controls. The existing "added trip" handling (`FindDeliveryItemIndex`, `DropAddedPickItems`) keeps working. |
| `TripReportView.Layout.cs` (card 1) | One row of controls added between the "Dispatch order" label and the drop-down, using the app's standard search box and combo styling (`PageKit.StyleSearch`, `StyleCombo`). |
| `TripReportView.cs` `ClearFields()` | Resets the controls (rule 2). |
| Database | **No change.** |

## 5. Checks (`Verify`) and your 2-minute test

Automatic: search by dispatch #, client, driver and "draft"; client filter lists only clients that have waiting trips; Show: No report yet / Drafts / Special trips each give the right lines; each sort order is correct; the picked trip stays when it no longer matches; reload keeps the controls; "+ New" resets them; count text is right; no pop-up.

You: Trip reports → + New → type "BCS" → only BCS lines; Show → Drafts → only drafts; Sort → Newest first; pick one → route, truck and crew load.

## 6. What if

| What if | Behaviour |
|---|---|
| 300 waiting trips | Search narrows it instantly; the list is capped at 10 visible rows and scrolls. |
| Client typed two ways (BUTTERWORLD / butter world) | Both appear in the Client list, as two entries. The real fix is client identity (billing plans), not here. |
| You type a search, then switch Show | Both apply together. |
| Nothing matches | The drop-down is empty and says so, with a Clear link. |
| Very small window | The controls wrap onto a second line; nothing is cut off. |
| Multi-select (several dispatches, one client) | **Not in this step.** Needs the picker with tick boxes (`2026-11-09-trip-report-dispatch-picker.md`, steps 2 and 3). |

## 7. Decisions (only two)

| # | Question | Recommendation |
|---|---|---|
| D1 | Which controls? | **All four** (Search, Client, Show, Sort). They are one row and cost little extra. |
| D2 | Default sort | **Oldest first** (as now). |

Reply **"use recommended"** and I build it, run the checks and commit.

---

## 8. Built (Nov 9, 2026)

As planned, all four controls (Search, Client, Show, Sort), new file `Views/TripReportView.PickFilter.cs`. Differences and additions:
- The row is named `noDirtyPickFilters`, and the form's unsaved-typing tracker now skips any control named `noDirty…`, so typing in the search box never triggers the "Unsaved changes" pop-up.
- Escape in the search box clears it; Enter picks the top match; Down arrow opens the list.
- Checks (`Verify/Checks.TripReportPicker.cs`): search, client list, Show (3 modes), Sort (4 orders), picked trip stays, count text, reset on new report, tracker ignores `noDirty` controls. All pass, as do the screen checks on a copy of the real data and the Keep-for-later checks.
- Not checked by a script: how the row looks at narrow widths (it wraps by design).
