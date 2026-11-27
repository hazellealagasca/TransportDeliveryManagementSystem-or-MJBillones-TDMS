# TDMS: continue here

A hand-off note so any Claude account on this computer (including Hazel Lea Lagasca's) can pick up the work where it stopped, and so Hazel can follow the progress of the patches.

**Claude: read this file first, then `git log --oneline -20` for anything newer. Add a line to the Patch log after every commit you make.**

Last updated: Nov 26, 2026 (last app commit: top row of the Patch log).

> **Note:** Smart App Control was turned off on this PC (Nov 24) because it blocked every new build, including F5 and the Verify checks. If builds are blocked again, that's the cause; don't try to bypass it.

---

## The project

| What | Where |
|---|---|
| App | M.J. Billones Transport Delivery Management System (TDMS): C# WinForms, .NET 9, SQLite |
| Main folder (work here) | `C:\TDMS` |
| Project file | `C:\TDMS\Transport Delivery Management System for M.J. Billones Transport Service.csproj` |
| GitHub | `github.com/hazellealagasca/TransportDeliveryManagementSystem-or-MJBillones-TDMS`, branch `master` |
| Published copy (what users run) | `C:\Users\Acer-pc\Downloads\MJ BILLONES` |
| Live data | `C:\ProgramData\MJ Billones Transport\TDMS\mj_billones.db` |
| Spare files (old plans, saved changes) | `C:\Users\Acer-pc\Documents\TDMS extra files` |

Opening the solution in Visual Studio 2022 and pressing F5 runs the latest code. Click **Reload All** if it asks. Do **not** open the partial `.cs` files (for example `MainForm.CloseGuard.cs`, `DispatchManagementView.Wizard.cs`) in the designer: Visual Studio then creates a broken `.resx` file.

## How we work

- **Every finished change is checked, then committed, pushed and republished, but only after the user says so** (usually "Yes, all three"). Never commit, push or publish without that answer. One yes covers only the change it was given for.
- **Coding is done by a Sonnet 5.5 subagent at medium effort.** Claude writes the brief, then reviews the diff, builds and runs the checks itself.
- The user is often short on time. Keep tasks small and checks short.

## Commands

Git (Git for Windows is not on PATH, use GitHub Desktop's):

```powershell
& "C:\Users\Acer-pc\AppData\Local\GitHubDesktop\app-3.6.11\resources\app\git\cmd\git.exe" -C C:\TDMS status
```

Build check (warnings are errors; build into a temporary folder so Visual Studio's output isn't touched):

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" "C:\TDMS\Transport Delivery Management System for M.J. Billones Transport Service.csproj" /p:Configuration=Release /p:OutDir="$env:TEMP\tdms_build\\" /v:m
```

Automatic checks (the `Verify` project):

- Build `C:\TDMS\Verify\TDMS.Verify.csproj` with `/p:OutDir=C:\TDMS\Verify\_tmp_run\`. The folder must be inside `C:\TDMS\Verify`, because some checks read the source code. Delete it afterwards.
- Run `TDMS.Verify.exe <name filter>`, for example `Dispatch`, `Soa` or `DispatchWizard`. Use `Start-Process -PassThru`, wait with a time limit, and kill it if it hangs.
- Screen checks need the real database path as a second argument. The checks work on a copy, so the real data is never changed.

Republish (first make sure nobody is running the app from `Downloads\MJ BILLONES`; if someone is, ask them to close it):

```powershell
dotnet publish "C:\TDMS\Transport Delivery Management System for M.J. Billones Transport Service.csproj" -c Release -p:PublishProfile=FolderProfile23
```

**Checks that were already failing before recent work (ignore them):**

- `PaidSoa_Opens_ReadOnly_WithHistory`, `CancelStatement_GivesItsInvoicesBackTheirFullAmount`, `CollectionForm_ListsEveryStatementAndInvoiceWaitingForPayment`, `SettledRecords_ListsPaidOnly`
- `EndToEnd_SeveralTripReports_ToInvoice_AndBack`, `TripReportDropdown_*`, `TripReportSeveral_*`, `TripReportWaiting_*`, `UiZoom_RoundTrip_*`
- Any check that says "no real database given" or "source folder not found".
- `ValidationSetup_Client_Email` sometimes hangs (a timing issue).

The harmless "failed to delete .git/worktrees/..." messages after a commit are expected.

## Feature switches

These live in Settings → Features & Pop-ups (code in `Functions\Features.cs`):

- **Delivery routes:** off by default. When off, routes are hidden everywhere, including dispatch.
- **Special trips** and **Incidents:** hidden for good. `Features.On` always returns false for them.
- **Trip incidents:** controls step 3 of the trip report.
- **Crew pay:** controls crew pay.

## Patch log (newest first)

| Date | Commit | Change |
|---|---|---|
| Nov 28 | (this commit) | Clients have Accountant 1 and Accountant 2 (Company details card, optional, same name rule as Contact person; `Clients.Accountant1/2`). A new Statement of Account fills Received by 1 / 2 from them only into empty boxes (or ones auto-filled from the previous client; typed text and saved statements are never overwritten) and Prepared by with the signed-in user's name when it passes the name rule, also after Reset (check `ClientAccountants_*`). |
| Nov 28 | (this commit) | Order History shows the dispatch stage (same words and colours as the Dispatch list, shared `DispatchStage.PillColors`), active orders first (Active (N), then Completed & cancelled (N)), a new Active tab after All, and Completed = paid only (`TxRecord.Stage` / `IsActive`; check `OrderHistoryStage_*`). |
| Nov 28 | (earlier) | Dispatch list Status column shows the order's whole life: Scheduled, Current, For trip report, For Statement of Account, For Service Invoice, For Collection, Completed (paid). Rules in one pure method `DispatchStage.For` (Functions\DispatchStage.cs; a Draft trip report still reads For trip report, a cancelled statement does not count, an invoice of any kind reads For Collection); one stage map per list load (`DispatchStage.BuildMap`, `MJdatabase.GetLiveSoaIdsByDelivery`); stored Delivery.Status unchanged. `IsFinished` is now cancelled or paid, so a completed unpaid order stays in the active list (Show finished, counts, muted rows, sort, summary bar and the Status filter follow; the filter has the four For stages, "Completed (paid)"). The four For pills are clickable (hand cursor, tooltip, first item of the right-click menu): trip report opens Trip Reports for that dispatch, Statement of Account opens a new statement for the client with just that trip ticked (`StatementOfAccountView.StartSoaForTrip`), Service Invoice opens the invoice for its statement, Collection opens the receipt for its statement (or the invoice when it is on no statement); all through `MainForm.OpenNextStepForDispatch` with the Payments permission checks. Checks: `DispatchStage_*` (the finishing-report check in `AutoComplete_*` now expects a completed unpaid order to stay listed) |
| Nov 28 | (earlier) | Typed store codes are at most 10 characters (letters, numbers, hyphens): the check is `MJdatabase.StoreCodeProblem` (rule text `StoreCodeRuleText`, limit `TypedStoreCodeMaxLength`), used by `CheckNewStore`, `CheckCompanyStore` (so Register store dialog, Stores panel, pending stores, New Dispatch Order registration), `SaveClientStore` (edits) and the paste list (`StorePasteModel`); boxes limited to 10: Register store dialog, Stores panel code box, Category route stop box (the dispatch entry box and grid also look up saved codes so they allow 20 and are checked on save). Codes the system made and codes already saved (even longer ones) are not affected. Check: `StoreCodeMax10_*` |
| Nov 26 | (earlier) | Trip report fits one screen at 100%: plant gate fields on one row (two rows of four when too narrow, e.g. 150%), no QA / store remarks box, dispatch banner beside the "New Trip Report" title (moves above the steps when it does not fit), one-line card headers, tighter padding, stores table as tall as its rows, attachments area 170. Card titles and the shared step header follow the display size. Dispatch list: "+ Add order" is first, left of the search box. Payments titles: "Creation of Statement of Account" and "Statement of Account (N)" (SOA tab, no View linked invoice button), "Creation of Service Invoice (N)" (no + New service invoice button), "For Collection Receipt (N)" (waiting count) and "Completed Collection receipts (N)" (no issued-receipts line). Every page and list title is one size: `UITheme.CardHeaderFont` (Segoe UI Semibold 12.5), used by PageKit headers too. Known flaky: `CompletedPayments_IssuedReceipt_ListedReadOnly_NotEditable` failed once (details window timing), passed on re-run. |
| Nov 26 | (earlier) | Display size is one app-wide control: A− / 100% / A+ in the main top bar beside Settings (Ctrl + / Ctrl − / Ctrl 0 work on every screen, handled in `MainForm.ProcessCmdKey`; code in `MainForm.Zoom.cs`). Every screen follows it (shown screens and the tabs inside a hub are scaled when they appear: `UiZoom.Apply` / `UiZoom.ApplyChild`), and windows opened later are scaled by `DarkModeKit` (`UiZoom.Apply(form)` also resizes the window and moves absolutely placed controls). The top bar and sidebar stay at their normal size. The Payments-only control (`FinanceHubView.Zoom.cs`) is removed. Centred form columns follow the size (`UiZoom.OnApplied`), a caption or button a little too wide for its box gets a slightly smaller font (`UiZoom.FitText`), the trip report filter bar wraps, the dispatch store entry captions have room. New Verify checks `ZoomNoClipping_*_150` and `_100` (Dashboard, Operations, Payments, Resources, Settings, Forms) in `VerifyChecks.ZoomNoClipping.cs`. |
| Nov 26 | (earlier) | Trip report form is 2 steps: 1 Plant gate & sign-off, then Stores, DRs & times; 2 Incident / Damage Report, then Attachments, and Save (incident switch off: step 2 is Attachments & save). The missing-crates incident rule is now on step 2. Verify trip report wizard checks rewritten for 2 steps. |
| Nov 25 | (earlier) | Missing input: the box that is missing (or wrong) is tinted light red with a red outline, the form goes to its step, scrolls to it, and focuses it after OK; the tint clears as soon as a proper value is entered (text typed, item chosen, number above 0, date changed, a row added) and on Clear form. One call does it all: `MissingInput.Show(field, message, owner, goTo)` in `Functions\MissingInput.cs` (also `Mark`, `Clear`, `ClearAll`, `IsMarked`); every "Missing input" box and the wrong-format ones use it (`SoaWarn` too). `ValidationHelper` uses it and no longer sets Tag. Verify checks `MissingInput_MarksAndClearsOnInput` and `MissingInput_SoaNextWithoutDeliveryType_MarksIt`. New files `Functions\MissingInput.cs`, `Verify\Checks.MissingInput.cs` |
| Nov 25 | (earlier) | Message titles: a box for missing input is titled "Missing input" (never "Validation Error"); a box for input filled in wrong (too long, wrong format, too short, not found) is titled "Check the input"; the SOA end-before-start date box is "Check the dates". Real failures (load / save) keep their error titles. Statement of Account: no "No ready service invoices found for ... in that period" pop-up when a new statement opens with nothing to fill, and no "Filled from ... service invoice" pop-up either (`StartSoaFor`). Service invoice form: no "worked out on the statement" under the total; an empty Service Invoice No. is a "Missing input" box ("Type the Service Invoice No.", no "(it is not made by the system)"). Collection Receipts form header: "New collection receipt". |
| Nov 25 | (earlier) | Collection Receipts tab: the bottom list shows the completed (issued) receipts, read-only, no inline preview: double-click a receipt (or Enter, or right-click > Open details) opens the Collection Receipt details window (`Forms\CollectionReceiptDetailsDialog.cs`): Client beside Statement (same height), then one full-width Collection Receipt box (receipt and payment details, the breakdown by service invoice, total paid); the window opens just tall enough for its content (scrolls only on a small screen); Previous / Next, Print Official Receipt; read-only. Client column; Client, Method and Date chips; Sort chip (Newest first default, Oldest, Highest / Lowest amount, Client, Receipt #, Invoice #) and click-a-column-heading sort. Selecting a row never loads the entry form; no Void / delete in the row menu. Verify check renamed `CompletedPayments_IssuedReceipt_ListedReadOnly_NotEditable`. Supersedes the Nov 24 "issued receipts are not listed here" rule. New file `Forms\CollectionReceiptDetailsDialog.cs` |
| Nov 25 | (earlier) | Settings: Print layouts tab hidden (saved layouts still apply); print window hint no longer mentions it |
| Nov 25 | `3aecf06` | Service Invoices tab: "For collection · N invoices" heading above the list |
| Nov 25 | `36e2487` | Statement of Account step 1: the trips card shows its "2 · Trips of this client" title again (no subtitle) |
| Nov 25 | `a13e015` | Statement of Account step 1: trip search box, + Add trip and the card heading removed; the trips list sits under the client card |
| Nov 25 | `44c2e30` | Step forms parts 3-5 (plan complete): Service Invoice 2 steps; Statement of Account 3 steps (step 1 "Client & trips", read-only trips list, new statements open on step 1); Trip Report on the shared wizard (3 steps, 2 when incidents are off). Two-pane workbench layouts gone |
| Nov 25 | `1814e21` | Step forms part 2 + payments: Collection Receipt in 2 steps (details card with VAT/withholding breakdown, big "I checked the overall amount" box required by Next); reference # and Service Invoice No. only validated (no search); Completed Delivery Trip hidden; Paid invoices / issued receipts read-only and off their tabs; Finance History = Completed (newest first) + Voided, admin Void receipt / Cancel invoice / Cancel statement there; locked Billed Client readable. Next: part 3 Service Invoice steps |
| Nov 24 | `76e09c4` | Login: Enter moves to the next empty box (username, then password); logs in once both are filled |
| Nov 24 | `d751302` | Print window shows print options only (copy label, watermark, paper, orientation, size, margins, style); title, header, labels, columns, signatures, notes and footer are edited in Settings → Print layouts (admin) |
| Nov 24 | `8c37a47` | Step forms, part 1 of 5: shared step component `Functions\FormWizard.cs`; New Dispatch Order uses it (no visible change). Next: Collection Receipt, Service Invoice, Statement of Account, Trip Report. Plan: `Documents\TDMS extra files\PLAN - Step forms everywhere.md` |
| Nov 24 | `5da0f6a` | No "Mark complete": a departed dispatch shows Current and appears in the Trip Report tab; finishing its trip report completes it automatically (drafts don't count); truck and crew are free 24 h after departure. Plan: `Documents\TDMS extra files\PLAN - No Mark Complete.md` |
| Nov 19 | `87e361f` | Dispatch entry row: labels no longer cut off; + Add store lines up with the text boxes |
| Nov 19 | `ed7a842` | New Dispatch Order is a 3-step wizard (Client & stores → Crates, truck & crew → Schedule & status). Step 1 has a store entry row (code, name, G/E crates, Temp Type, + Add store); the store table is read-only and its Edit button is hidden |
| Nov 19 | `8677e2d` | The mouse wheel never changes drop-downs, number, date or text boxes; it scrolls the page instead |
| Nov 19 | `22f4b14` | Delivery routes: switch in Settings, off by default, hidden everywhere including dispatch |
| Nov 18 | `ab2b46e` | Trip report step 3 renamed "Incident / Damage Report"; required when crates are missing or damaged; picking a time in jumps to time out |
| Nov 18 | `d812ea5` | Crew pay easy to see: Trip pay tab in the crew profile; wage and meal columns plus "View trip pay" in Crew Management |
| Nov 18 | `7bdb7f8` | Dispatch tracker has a Statement of Account step before Invoiced; crew profiles show their Incidents tab again |
| Nov 18 | `d60f92c` | Security settings window can't be closed with X, Alt+F4 or Esc; Cancel is now "Do it later" |
| Nov 18 | `f326417` | Service invoice: Statement of Account and Billed Client are locked once a statement is shown |
| Nov 18 | `3484cd8` | Statement of Account: Zone column hidden on screen (data and print unchanged) |
| Nov 18 | `cde36fa` | Trip report: a store's time out can't be earlier than its time in |
| Nov 18 | `c69a635` | New trip report: no Dispatch order search or drop-down |
| Nov 18 | `3e238e3` | Incidents from trip report step 3 show everywhere; Resources → Incident log |
| Nov 18 | `94e1839` | Trip report step 3 follows the dispatch's current truck, driver and aides |
| Nov 18 | `8ec6fcf` | Truck form: "Vehicle & capacity" is one section |
| Nov 18 | `c39964e` | Statement of Account: "Date store received" is the dispatch date; trips sorted oldest first |
| Nov 17 | `7237ede` | Special trips hidden everywhere |

Earlier work (Nov 17 and before) is in `git log` and `CHANGELOG`.

## Open items and things not checked yet

- **New Dispatch Order wizard:**
  - A truck capacity or schedule-conflict warning at Save doesn't jump to a step (the message still shows).
  - The step header and layout were checked by eye on 1920×1080 only. Large-print mode and dark mode haven't been looked at.
- **Delivery routes off:**
  - The loading-order form hasn't been clicked through.
  - The "Stores & crates" title on the dispatch form only changes after a restart.
- **Mouse wheel:** inside a long multi-line text box the wheel no longer scrolls (as asked). Its scrollbar can still be dragged.
- **Rate Matrix:** still shows route fields. It has its own switch.
