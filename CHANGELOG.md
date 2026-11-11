# Update list

M.J. Billones Transport Service · changes since the September 26, 2026 release (newest first)

## November 5–10, 2026 — Workflow overhaul (latest version)

Branch workflow-overhaul-2026-11-06. Not yet merged into master.

### Workflow: Trip report → Statement of Account → Invoice → Collection receipt

- Trip reports: waiting list and drop-down show only trips waiting for a report, with search, filters and sort
- Trip reports: pick several dispatches of one client and fill their stores together; warning when they differ in date, truck or driver
- Trip reports: delivered E/G crate columns and totals (also on the printout)
- Trip reports: DR numbers digits only (4 to 8); Received by letters only; plant gate and sign-off use 24-hour times with a guard name required; every store's time in/out required
- Trip reports: Issue Invoice became Proceed to SOA
- Statement of Account comes first: completed trip reports queue into it and choosing a client fills the trip table; normal or special type with separate print layouts
- Statement of Account: inline Trip payments card; client picked from a list and locked once trips are added; TIN comes from the client
- Statement of Account: taking a trip off a saved statement needs a reason, restores and audits invoice amounts; cancelling a statement frees its invoices; amounts under 1.00 refused; printed or sent statements recorded and flagged if an amount changes
- Billing waits on statements (SOA # column); a statement is billed with one invoice; each trip has one live invoice; Billing Invoices renamed Service Invoices; Payments opens on the Statement of Account
- Collections: one receipt per statement for the full amount (no partial payments), only through a statement, typed 4–8 digit receipt number never repeated; payment method is Check, Cash, Bank Transfer or GCash; verify the overall amount then Complete / Issue Receipt; settled records go to Finance History

### Routes, stores and clients

- Clients panel: add stores to a route (checked entry and paste), reorder stops, see where a store is used on other routes
- Optional one-route-per-store rule per client; repeated route codes or names refused
- One-time repair of duplicate and unrecognised route stops
- New clients can have stores and routes added before the client is saved; TIN is mandatory

### Dispatch and trucks

- Temp Type (Dry/Cold/Combi) is required and checks crates against the truck's zone limit; in Combi trucks G crates are dry and E crates are cold
- Default truck categories (4T, FWD/6T, 8T) built in and protected; order form is full screen

### Forms and settings

- Forms start blank and ask before discarding typing; Unsaved changes pop-up has Keep for later
- Settings > Appearance large-print switch (off by default)
- "Cargo" is now "crate" in all labels and hints
- Crew and user names use separate fields with no initials; crew pay stays inside Crew Management

### Hidden or removed

- Special trips and Incident & Damage Logs hidden everywhere (tabs, search, menus, permissions, filters)
- Paste list button on Clients hidden

### Build and stability

- Clean hand-over build starts with no sample data; warnings treated as errors; new TDMS.Verify test harness
- Fixed: crash opening a receipt with no reference number; invoice issuing on older databases; DR-number guard blocking other cells

## November 4, 2026

### UI cleanup: sentence-case labels, modern list pages, fixed dialogs, manual update

- Sentence case for all buttons and menu items; plain labels on Preferences and Reschedule
- Clients, Special Trips, Audit Logs and Change History pages modernised; raw column names fixed
- Generate SOA window rebuilt; its Billing menu item works again
- Time picker: buttons no longer cut off, quick times work, AM/PM highlight
- Dialogs grow to fit their content
- Old unused screens emptied
- User manual updated with new screenshots

### Remove unused screens, rename Special Trip tab, update manual

- Deleted emptied old screens and the unused Add Stop and Crew Roles windows (nothing opened them)
- Removed their leftover code in the dispatch and crew forms
- Operations tab and sidebar now say "Special Trip"
- Manual: new Operations screenshots and wording

### Fix cross-thread error when closing windows with type-ahead boxes

- Windows' auto-complete clean-up raised a false 'Cross-thread operation not valid' error when Add route, truck/crew profiles, trucks, incidents or SOA screens were closed under the debugger; disposal is now protected
- Overdue invoice alerts no longer fire for cancelled, void or draft invoices

### Archive, editable routes, truck capacity matrix, capacity check, SOA editing, trip report store panel

- Master Data: Archive tab (clients, stores, routes) with Restore; no made-up store codes (one-time clean-up); Destination column; Delivery Routes as one editable table with store picker
- Trucks: capacity locked to category matrix (Dry/Cold/Combi), category manager, Decommissioned status with reasons and history, Resource History hidden
- Dispatch: shared capacity check with exceed-capacity warning and reason, Combi cold/dry split, store table always shown, at least 1 aide
- Trip report: store details window (DRs, returned crates, double warning), No. Drops, wrapped headers
- SOA: edit saved statements (blocked once paid), open dispatch from a line

## November 3, 2026

### Dispatch list shows active orders first; finished orders move to Order History

- Loading Schedule & Dispatch: completed and cancelled orders hidden by default; Show finished switch (remembered per user); Status menu gains Active orders and All orders; default sort is active first (awaiting completion, in transit, delayed, scheduled)
- Order History: new Cancelled tab; Cancel loading order for old loading orders never dispatched
- Past Due loading order notification opens Order History
- User manual updated (pages 11-13 and 26)

## November 2, 2026

### New Dispatch Order: Back returns to the order; add truck, driver or aide from the form

- Open client > shows 'Back to New Dispatch Order' and returns to the same order with everything kept
- '+ Add a truck / driver / aide' links in Truck & crew open a blank new form in Resources; saving or Back returns to the order and picks the new one
- Coming back no longer clears client, route, truck, driver or aides, and no longer loads an old order into the form
- Right-click Open client profile in the order list returns to the list
- Dashboard '+ New loading order' opens a new order form
- '2 Aides' button no longer cut off

### Block PHP 0 / negative statements and invoices; driver licence notice; crash-proofing

- From the stability check of Nov 2:
- Statement of Account can no longer be saved with a total of PHP 0.00 or negative sales (it used to create a PHP 0 invoice); the database refuses it too. Special-trip drafts still allowed.
- Invoices must be more than PHP 0.00 (drafts excepted) and deductions can't exceed the amount
- Dashboard notice: drivers with no licence expiry date, and licences expired or expiring within 30 days (new 'Crew' area)
- Crew profile and route store check no longer fail on a blank name, code or role

## October 30, 2026

### Per-account access, sign-in details for admins, several aides when rescheduling

- User Accounts: Access & permissions (admin only) to hide or show each screen per staff account; hidden screens leave the menu, tabs, dashboard and links
- User Accounts: Sign-in details shows the username after the admin confirms their own password, and can set a new password (shown once); existing passwords stay hashed
- Reschedule window: pick one or more aides (tick list); window scales on high-DPI screens and never exceeds the screen

### Hide Crates hauled from Crew Trip History and the printed crew profile


### Dispatch Rules as switches in Features & Pop-ups; fix scrolling and windows going under the taskbar

- Dispatch Rules tab removed; 'Suggest the best truck' and 'Check returned empty crates' are now switches that really work
- Theme no longer turns off scrolling on long pages (Features, Preferences, Backup, Loading)
- Pop-up windows are kept above the taskbar
- Unsaved draft bar no longer shows twice
- Crates hauled hidden on Crew Trip History

### Update user manual: Features & Pop-ups, access per account, sign-in details, Rate Matrix, reschedule with several aides, backups

- 59 pages, Word and PDF. New pages for Features & Pop-ups (with Dispatch rules), Access & permissions, Sign-in details, Rate Matrix and Reschedule; updated Crew Trip History, User Accounts, Backup & Restore, settings screenshots, rules, workflow and messages. Contents page now filled in the PDF.

## October 29, 2026

### Order History redesign, auto dispatch numbers, dashboard cleanup, updated user manual

- Order History: one place for every transaction (completed dispatches merged in), search, find helper, pins, recently viewed
- Dispatch number auto-generated from client initials (e.g. RR0001)
- Dashboard: Available crew and Trucks available, Billing card, whole page scrolls
- Special trip: payment dropdown, multiple aides, Service Type removed
- Trip report photo upload fixed; crew pay shows wage + allowance only
- Truck maintenance tab and payroll deductions removed (out of scope)
- Close confirmation when the app is minimized; cross-thread autocomplete fix
- Docs: user manual rebuilt (DOCX + PDF) from the latest build

### Add option to change the company logo in Settings

- Settings > Preferences & Customization: Company Logo row with preview, Change logo and Use default logo
- Logo stored in the database (travels with backups), used on the sidebar, sign-in screen and printed forms
- Admin only; changes recorded in the security audit log

### Needs attention filters and sort, settings card headings fix, manual update

- Dashboard Needs attention: All / Urgent (orange) / Incoming (blue) / Notice (red) filter with counts, Area filter, Sort (most urgent, oldest, newest, most records, group by area), Show all / Show fewer
- Settings > Preferences & Customization: card headings now sit above their fields
- User manual: new pages for Needs attention filters and the company logo

### Stability: safe data folder, backups with photos, error log, redesigned messages

- Data (database, attachments, crew photos, backups, logs) moved out of bin to C:\ProgramData\MJ Billones Transport\TDMS with a one-time safe copy; stored file paths updated
- Manual backups also save photos and documents (_files.zip) and restore them; backup reminder in Needs attention; Backup page shows data folder and links
- Error log (Logs\errors_yyyy-MM.log) replaces silently ignored errors; new message window with Copy error details
- Redesigned Data folder moved / Where is my data window
- Fixed: false cross-thread error when closing truck and crew profiles while debugging
- Retired the old Goldilocks -> Red Ribbon rename migration
- Departure times saved in one format; database indexes for faster lists

### Features & Pop-ups settings: switch Rate Matrix, special trips, incidents and crew pay on/off; quiet notices

- New Settings tab Features & Pop-ups (admin): Rate Matrix, Special trips, Incident & damage logs, Crew pay on trip reports, Quiet notices, Ask before saving, Small notices, Backup reminder
- Turning a part off only hides it (tabs, buttons, menu items, search, dashboard items); records are kept and come back when turned on; links to a hidden part show a short notice instead of an error
- Rate Matrix is back as a Master Data tab; duplicate rates are flagged and highlighted; clearer duplicate message
- Information-only messages show as small notices instead of pop-ups (questions, warnings and errors still use windows)

### Setting to turn automatic dispatch numbers on or off

- Settings > Features & Pop-ups: Automatic dispatch numbers (on by default)
- On: new orders get the client-initials number (RR0001...) by themselves; the box is read-only for new orders
- Off: the box is empty and the number is typed in (still required, duplicates still blocked)
- Features page now scrolls with the mouse wheel

## October 28, 2026

### System fixes and UI update; add user manual


