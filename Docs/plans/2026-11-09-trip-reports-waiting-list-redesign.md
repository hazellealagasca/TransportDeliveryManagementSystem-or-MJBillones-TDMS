# Trip reports screen: waiting list, create invoice button, details window (Nov 9, 2026)

> **PLAN ONLY, no code yet.** Four small steps, each can be built, checked and committed on its own. Decisions are in section 8.

## 1. What you asked for

1. **Waiting list only.** One row per dispatch that is **waiting for its trip report**, with a button **Create trip report**. No "Draft saved" confusion.
2. **Drafts should work like Dispatch** (Loading Schedule & Dispatch): nothing is stored until you save; a saved draft is offered back to you inside the form.
3. **Create invoice button on the trip report rows**, placed **beside the Dispatch # and the Date** (not far away on the right).
4. **A drop button (▾) on every dispatch row** that opens a **summary window with the order's complete information.**

## 2. Why the screen confuses you (found in the code, matches your screenshot)

| What you see | Cause (verified) |
|---|---|
| **Every row says "Draft saved"** with a report number, though nobody typed anything | The **Create trip report** button **saves an empty Draft report straight away** (`CreateTripReportForDispatch`). Every click makes a database record and a report number. The list then shows it as "Draft saved". |
| Rows say **Open draft**, others **Create trip report** | Same list, two states, two verbs, because of the point above. |
| Search box shows **BCS0010** and the list still shows other trips | The search box is **not applied to the Awaiting list** (it only filters finished reports). And the text "BCS0010" was **put there by the app**: opening a dispatch's report sets the list search to that dispatch number (`OpenForDispatch`) and leaves it. |
| Title says **"showing 0 of 19"**, tiles say **0 reports, 0 drafts**, yet 6 rows say Draft saved | The title and tiles count **finished reports in the selected period (Sat, Nov 7)** plus the stale search. The Awaiting list ignores both. Two lists on one screen sharing one toolbar. |
| **Dispatch # and Date are not visible**, the first column starts with "on…" | The Awaiting grid is wider than the window (minimum widths add up to about 1,590 px), so it scrolls sideways and the **key columns scroll out of view**. The button is the **last** column, off to the right. |
| The date button "Sat, Nov 7", client, driver, truck and report filters | They apply to reports only, not to the waiting list. |

## 3. The solution

### 3.1 Awaiting report tab (the default)

```
Awaiting report: 8 dispatches                      [🔍 Search…] [Client ▾] [Driver ▾] [Truck ▾]
 ▾   Dispatch #   Date     Client                  Action                    Route          Truck      Driver      Crates   Waiting
 ▾   BCS0010      Nov 8    BCS GROUP COMPANY       [ Create trip report ]    R3 - Route 3   NBF 8965   ESTOESTA    120      1 day
 ▾   BCS0006      Nov 7    BCS GROUP COMPANY       [ Create trip report ]    mariveles (+2) CBP 2036   DEL ROSARIO 90       2 days
```

- **One row = one dispatch that is completed and has no finished report.** No status column, no report number, no "Draft saved". Everything in this list is, by definition, waiting.
- **Order of columns:** ▾ · Dispatch # · Date · Client · **button** · Route · Truck · Driver · Crates · Waiting. The first five are **frozen**: they never scroll away, whatever the window size.
- **Waiting** chip: amber after 2 days, red after 3 (same rule as the billing list).
- **Search, Client, Driver, Truck and Date all apply to this list.** The "All reports" and report Sort filters appear only on the Active and History tabs.
- **Header counts what you see:** "Awaiting report: 8 dispatches" (or "3 of 8" when filtered).
- **Special trips that need a report** appear as rows too (small "Special trip" tag), so this list matches the drop-down and the waiting counts (D3).

### 3.2 Create trip report: nothing is saved until you save

- The button **opens the blank trip report form for that dispatch**. It does **not** write anything and does **not** make a report number. The number is created when you press **Save** (as it is today for a new report).
- Click it and close the form: **nothing is left behind** (today an empty draft is left behind).
- **A real draft** (one you saved on purpose, or the form's autosave) is offered inside the form, exactly as Dispatch does it: a banner **"You have a saved draft for this dispatch from Nov 8. [Continue] [Start fresh]"**. The trip report form already has this banner (`CheckAndPromptDraft`); it will be the only way drafts show up.
- The list **never** shows a draft state. A dispatch with a real draft is still "waiting for report", because no report has been finished.
- **Opening a report no longer rewrites the search box** (the `txtGridSearch.Text = dispatchNumber` line is removed).

### 3.3 Clean-up of the empty drafts already in your data
- An **empty draft** = a Draft report with no times, no store manager, no remarks, no crate counts and no stop data (the pre-filled signatory names do not count).
- **One-time clean-up:** the screen counts them and asks once: "6 empty drafts were left by the old Create button. Remove them?" A backup is made first and each removal goes in the audit trail (the same pattern as the earlier clean-ups in this app).
- Drafts with anything typed in them are **kept untouched**; they appear only through the banner.
- Until the clean-up is accepted, empty drafts are simply **treated as "waiting"** (no banner for an empty one; it is silently reused).

### 3.4 Trip reports tab (Active / History): invoice button beside Dispatch # and Date

```
 ▾   Dispatch #   Date     Client             Action                 Report #                  Truck      Delivered/Returned   Manager
 ▾   BCS0004      Nov 7    BCS GROUP COMPANY  [ 🧾 Create invoice ]  TRIP-20261107-183209…     CBP 2036   88 / 2               R. Santos
 ▾   BCS0002      Nov 5    BCS GROUP COMPANY  Invoice 78123 ✓        TRIP-20261105-…           NBF 8965   120 / 0              M. Cruz
```

- A **Client** column is added (the report list has none today).
- **Action column right after Date/Client:**
  - report completed, not invoiced → **🧾 Create invoice** (opens Billing on that trip, with "Back to Trip Reports", as "Issue Invoice (Selected)" does now);
  - invoiced → **Invoice 78123 ✓** (a link to that invoice in Billing);
  - History tab: link only.
- The existing **Issue Invoice (n selected)** for several trips of one client stays, with its same-client rule.
- The same five left columns are frozen.

### 3.5 The ▾ drop button: the full order summary window (reuse)

- Every row on **both** lists has **▾** as its first column. One click opens the app's **existing dispatch details window** (the one Dispatch opens with "Open dispatch details"). Nothing new to design:
  - **Header** with number, client, status and the Peak season pill;
  - **Progress tracker:** Scheduled → In transit → Delivered → Trip report → Invoiced → Paid;
  - **"Next:"** line with a button (Create trip report, Open billing…);
  - **Tiles:** crates (G / E), stores (single or multi-drop), truck load %, delivered / returned, billing amount;
  - **Tabs:** Overview (client, contact person and number, address, route, order type, crate type, peak, truck + capacity, driver + license, aides, dates) · Stores · Documents · Activity · Notes;
  - **Copy summary** (plain text for Viber, SMS, email).
- The window is one at a time; clicking ▾ on another row reloads it.
- Small fix: **Previous / Next** inside the window follow the on-screen list, but they read only rows that are orders. Trip report rows are not, so I add a small adapter so Previous/Next works from these lists too.

### 3.6 Tiles and header
- Awaiting tab headline: **Awaiting report: 8** (same number as the list, the Dashboard and the Operations badge).
- The **Drafts** tile goes (drafts are not a list concept any more). It becomes **Ready to invoice** (completed reports with no invoice); clicking it opens Active filtered to those (D2).
- Trip reports and Missing POD stay, still for the selected period, **and say so** ("in the selected period: Sat, Nov 7").

## 4. Build order

| Step | What | Ships alone? |
|---|---|---|
| **S1** | Stop creating empty drafts; one waiting state in the list; remove the search-box side effect; empty-draft clean-up. **This removes the confusion.** | Yes |
| **S2** | Awaiting list layout: column order, frozen columns, button beside Date, ▾ button, filters apply, header and tiles agree. | Yes (after S1) |
| **S3** | Trip reports list: Client column, Create invoice / invoice link beside Dispatch # and Date, ▾, frozen columns. | Yes |
| **S4** | Checks, manual page, Previous/Next adapter. | With S3 |

## 5. Files

| Area | Files |
|---|---|
| Create button, awaiting list, banner | `Views/TripReportView.Workflow.cs` (`CreateReportFor`, `BuildAwaitingGrid`, `LoadAwaitingGrid`), `Views/TripReportView.cs` (`OpenForDispatch`, `CheckAndPromptDraft`, `FilterGridData`) |
| Waiting query and clean-up | `Database/MJdatabase.AwaitingReport.cs` (the SQL's "Draft saved" branch, `CreateTripReportForDispatch` no longer called, new `IsEmptyDraft` + clean-up with backup and audit) |
| Reports list | `Views/TripReportView.cs` (`SetupGridColumns`), `TripReportView.Workflow.cs` |
| Tiles | `Views/TripReportView.Center.cs` |
| Details window | `Forms/DispatchDetailsDialog.cs` (sibling adapter only) |
| Checks | `Verify/Checks.*` |

**No new database column.**

## 6. Checks

- Clicking **Create trip report** creates **no** report row and no number; closing the form leaves nothing.
- A dispatch with a finished report is not in the list; one with an **empty** draft is (as "waiting"); one with a **real** draft is (as "waiting") and the form offers the banner.
- Search / client / driver / truck / date narrow the **Awaiting** list; header says "3 of 8".
- Opening a report does not change the search box.
- Clean-up removes only **empty** drafts, makes a backup, and leaves real drafts untouched.
- Reports list: Create invoice appears only for completed, not-invoiced reports; invoiced rows show the invoice number; ▾ opens the details window for that dispatch.
- Dispatch #, Date, Client and the button stay visible when the grid is scrolled sideways.
- No error pop-up; the existing screen and Keep-for-later checks still pass.

## 7. What if

| What if | Behaviour |
|---|---|
| Two people press **Create trip report** on the same dispatch | Nothing is stored, so no clash. The second **Save** is refused: "BCS0010 already has trip report TRIP-… filed". |
| You press Create, type for 5 minutes, then the app closes | The form's autosave draft is offered next time (banner). |
| A dispatch has a real draft from another user | Form drafts are **per user**; a draft saved with the Save draft button is shared. The banner says who saved it and when. |
| A completed dispatch is later cancelled | It leaves the waiting list. |
| 300 dispatches are waiting | Search and filters narrow it; waiting chips show the oldest first; the list is a normal table, not 300 widgets. |
| You invoice a report, then open ▾ | The tracker shows Invoiced and the "Next:" button becomes Open billing. |
| Narrow window | Left five columns stay; the rest scroll. Nothing important is hidden off-screen. |
| A trip report is edited after it is invoiced | Not changed by this plan (see the billing reality-check file). |
| Special trip needing a report | Shown with a tag; **Create** opens the same form for it. |

## 8. Decisions

| # | Question | Recommendation |
|---|---|---|
| **D1** | Drafts: **A:** Create opens a blank form, nothing saved; real drafts only via the form banner; the old empty ones cleaned. **B:** keep the Draft rows as they are. | **A.** It is exactly Dispatch's behaviour and removes the root cause. |
| **D2** | Replace the **Drafts** tile with **Ready to invoice**? | **Yes.** |
| **D3** | Show **special trips** that need a report in the waiting list? | **Yes.** |
| **D4** | ▾ opens the **existing details window** (recommended) or expands the row inline? | **Window.** It already exists and shows everything; inline expansion would have to be built and would be small. |
| **D5** | Empty drafts: **delete after one confirmation** or just hide? | **Delete after one confirmation, with backup.** |

Reply **"use recommended"** and I start with **S1**, run the checks and commit.

---

## 9. S1 built (Nov 9, 2026)

Done: the Create trip report button saves nothing; the waiting list has one look and one button (the state and report-number columns are hidden); opening a report no longer writes into the search box; empty drafts are cleared once on request.

Two things found while building, which change the plan's wording:
- **Reports are voided, never deleted.** The database refuses to delete a trip report ("kept for history"). So "remove the empty drafts" is **void** (kept for the audit log, ignored by every list), the same way the app voids a report today. A backup is still made first.
- **The plant gate times come pre-filled** (`2042` / `0139`), so they count as "nothing typed" when deciding whether a draft is empty.

Also fixed: **Open draft failed silently** when the list's date or client filter hid that report (nothing happened). The form now opens the report whether or not the list shows it.

Not done yet (S2 to S4): column order and frozen columns, the filters applying to the waiting list, header and tiles agreeing, the ▾ details button, the Create invoice button beside Dispatch # and Date, and the Client column.
