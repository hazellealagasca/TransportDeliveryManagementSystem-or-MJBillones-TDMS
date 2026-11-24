# TDMS: continue here

A hand-off note so any Claude account on this computer (including Hazel Lea Lagasca's) can pick up the work where it stopped, and so Hazel can follow the progress of the patches.

**Claude: read this file first, then `git log --oneline -20` for anything newer. Add a line to the Patch log after every commit you make.**

Last updated: Nov 24, 2026 (last app commit `8c37a47`).

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
