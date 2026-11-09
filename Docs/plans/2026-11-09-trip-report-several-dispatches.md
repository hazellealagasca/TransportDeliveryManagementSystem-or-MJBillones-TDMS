# Trip report: pick several dispatches of one client, fill their stores together (Nov 9, 2026)

> **PLAN ONLY, one page.** Only this feature. Built and committed as one patch right after you answer D1 (or say "use recommended").

## 1. What you asked for

On the **New Trip Report** form (Dispatch order card):
1. Pick **more than one dispatch**.
2. They must be **the same company / client** only.
3. Their **stores, times and signatures come together** in section 2 ("Stores, times & signatures"), and the crate totals add up.

## 2. What the code does today (verified)

| Fact | Where |
|---|---|
| The form holds **one** dispatch (the drop-down). The stores table is filled from that dispatch's stops. | `UpdateMultiDropTripInfo`, `GetOrBuildTripStopsForDelivery` |
| A saved trip report has **one** `DeliveryId`. The stops are saved against that dispatch. | `TripReport` model, `SaveDispatchStops` |
| Everything after the report is **per dispatch**: the trip report number, the invoice (one per trip), the statement line, the DR duplicate check. | Billing, SOA |
| Billing already has a **same-client, several trips at once** path ("Issue Invoice (n selected)", one invoice form per trip). | `IssueInvoiceForSelected`, `StartBatch` |
| The save routine is long and runs many checks (received by, crates, times, DR numbers, incomplete stores, incidents). | `SaveNewReport` |

So a report that covers two dispatches **as one record** would have to change billing, statements, numbering and prints (the "large" option from the earlier picker plan). That is not a one-patch job.

## 3. Recommended design: one screen, one report record per dispatch

You fill **one combined form**; the system saves **one trip report for each dispatch** from it. Nothing downstream changes (each dispatch keeps its report number, its invoice, its statement line).

1. **Add dispatches.** Under the drop-down: **＋ Add dispatches of this client**. A tick list opens with only the waiting dispatches of the **same client** as the first pick (other companies are not listed, so the same-company rule cannot be broken; the title says "BCS GROUP COMPANY only"). Dispatches that already have a draft are not listed (open those alone).
2. **Chips.** The picked dispatches show under the drop-down as chips: `BCS0007 · Nov 3 ✕`. Removing one takes its stores out of the table. Changing the first drop-down pick, or the client, clears the extras.
3. **One stores table.** Section 2 shows the stores of all picked dispatches together, with a **Dispatch** column in front (only when 2 or more are picked). You type times, DR numbers and "Received by" exactly as now. Crate totals and the crate summary add up across all of them.
4. **Save.** The existing checks run **once** over the whole table. Then one report is saved **per dispatch** from that dispatch's own rows (its stores, its crate totals, arrival = its first store in, departure = its last store out). Gate times, signatories and remarks you typed once are copied to each. After saving, Billing opens with all of those trips queued (one invoice per trip, same client), the same as "Issue Invoice (n selected)".
5. **Editing an existing report** stays as it is: one dispatch.

## 4. What if

| What if | Behaviour |
|---|---|
| Another company's dispatch | Not listed. |
| Two picked dispatches go to the same store | Both rows are kept (each belongs to its own dispatch); the Dispatch column tells them apart. |
| A DR number is typed twice | The existing duplicate-DR warning applies across the whole table. |
| One dispatch's stores are incomplete | The existing "incomplete stores" warning names it; nothing is saved until you confirm. |
| Save fails halfway | Reports already saved stay saved; the form keeps the unsaved dispatches and tells you which. |
| Missing or damaged crates on one dispatch | The incident question is asked for that dispatch. |
| Different dates or trucks | Allowed (you checked them yourself). Each report keeps its own dispatch's date, truck and crew. |
| "Save draft" with several picked | **Not allowed** (a draft belongs to one dispatch). The button says why. Use Issue Invoice, or save each alone. |

## 5. Checks

Same-client list only; chips add/remove and clear on a new pick; combined table with Dispatch column; totals add up; save creates one report per dispatch with the right stores and crate counts, shared gate times and signatories; Billing opens with the queue; single-dispatch form behaves exactly as before; no pop-up errors.

## 6. Decisions

| # | Question | Recommendation |
|---|---|---|
| **D1** | **One report record per dispatch**, filled on one form (recommended), or **one record covering all picked dispatches** (large: billing, statements, numbering, prints)? | **Per dispatch.** |
| **D2** | Gate times, signatories and remarks typed once, copied to every dispatch's report? | **Yes.** |

Reply **"use recommended"** and I build and commit the whole feature in one patch.

---

## 7. Built (Nov 9, 2026)

As planned (D1 one report per dispatch, D2 shared gate times / signatories / remarks), in `Views/TripReportView.Several.cs`:
- **＋ Add dispatches of this client** under the drop-down opens a tick list of the waiting dispatches of the **same client** only (no drafts, no special trips). Added dispatches show as chips (click to remove) with a summary "3 dispatches · 4 stores · 42 crates". Another company's dispatch is refused even if passed in by code.
- **One stores table** with a **Dispatch** column (shown only when several are picked). Crates add up. Each row remembers its dispatch, so one dispatch's stops are never collected with another's (Print and Update use the same rule).
- **Save (Issue Invoice):** the existing checks run once; DR numbers are checked per dispatch against the other dispatches; one crate-variance question for all; then **one report per dispatch** from its own rows (own crates, first time in, last time out, first "received by"), with gate times, signatories and remarks copied. Billing opens with all of them queued. If a save fails halfway, the saved ones stay saved and the message names the rest.
- **Save draft** is refused while dispatches are added (message says why). Changing the main dispatch or pressing Clear form removes the added ones.
- Checks: `Verify/Checks.TripReportSeveral.cs` (2 checks).

---

## 8. Warning when the dispatches may not have ridden together (Nov 9, 2026)

Found in review: the feature only checked the client, so wrong picks would silently copy the same gate times and signatories onto unrelated reports. Now the form compares each added dispatch with the main one on **date, truck and driver**:
1. **When you add them:** a Yes / No question (default No) lists what differs ("D-0002 has a different date (Nov 8, not Nov 6) and a different driver (…)").
2. **While they differ:** a standing amber note under the chips.
3. **Before saving:** the same question once more, because saving is the step that cannot be undone. No saves nothing.

Dispatches of the same day, truck and driver show no warning at all. Checks: `TripReportSeveral_Warning_...` and the end-to-end check `EndToEnd_SeveralTripReports_ToInvoice_AndBack` (filed reports → waiting for invoice → Billing → invoice saved → report Invoiced → cancel → back to waiting).
