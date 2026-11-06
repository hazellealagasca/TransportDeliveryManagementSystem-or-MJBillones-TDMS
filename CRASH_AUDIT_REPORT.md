# Crash Audit Report — M.J. Billones TDMS

Date: 2026-11-07 · Branch: `workflow-overhaul-2026-11-06` · Status: **audit only, nothing in the codebase was changed**

## How far this audit goes (read this first)

- The code base is about 87,000 lines of C#. I did **not** read every line.
- I searched the whole code base for known crash patterns, then read the surrounding code for every hit I report.
  - Patterns searched: unguarded `First()` and `SelectedRows[0]`; `Convert.To*` on database values; `DataGridView` error handlers; `DateTimePicker` assignments; static event subscriptions; timers; `async void`; file and export code; `Process.Start`; SQL built by string concatenation.
- Nothing was run. The app was not started, nothing was compiled and no patch was test-applied. **Treat every patch below as unverified until you build it.**
- The code base is already defensive. The checks below came back clean:
  - SQL values are parameterised; the only concatenated SQL pieces are fixed strings.
  - `ExecuteScalar` results are wrapped in `COALESCE`.
  - There are no raw `int.Parse` or `decimal.Parse` calls.
  - Every `SelectedRows[0]` read is preceded by a count check.
  - `Process.Start` calls are wrapped in try/catch.
  - A global exception handler exists in `Program.cs`.
- I found **no bug that crashes the process every time**. The findings below are conditional: they depend on timing, on the clock, or on the user leaving a screen while it loads.
- "Critical" here means: a user can see an error dialog or a stuck screen through normal clicking.

### Applying the patches

Patches are unified diffs. Hunk line counts are approximate and some context lines carry trailing spaces in the source. Apply them with:

```bash
git apply --recount --ignore-whitespace fix-name.patch
```

Save the diff from each finding into its own `.patch` file, or paste it into one file.

---

## 🔴 Critical / Crash Risk

### C1. Clearing the Crew form after midnight throws `ArgumentOutOfRangeException`

- **Root cause:** `dtpLicenseIssued.MaxDate` is fixed to `DateTime.Today` once, when the screen is built. The view is cached and lives as long as the session. If the app stays open past midnight, `MaxDate` is yesterday. `ClearClassification()` then sets `Value = DateTime.Today`, which is above `MaxDate`. WinForms throws "Value of '…' is not valid for 'Value'". The exception is not caught, so the user sees the global "System Error" dialog. This hits the Add / Clear / Cancel actions in Crew Management.
- **Files:**
  - `Views/CrewManagementView.Classification.cs:73` (MaxDate fixed at build time)
  - `Views/CrewManagementView.Classification.cs:205` (the assignment that throws)
  - `Views/CrewManagementView.Classification.cs:186` (same stale MaxDate, but clamped so it does not throw; it silently clamps to yesterday)
  - Caller: `Views/CrewManagementView.cs:939`

```diff
--- a/Views/CrewManagementView.Classification.cs
+++ b/Views/CrewManagementView.Classification.cs
@@ -183,7 +183,8 @@
                 chkIsSeasonal.Checked = c.IsSeasonal;
                 cbLicenseClass.SelectedIndex = !c.HasLicense ? 2 : (c.LicenseClass.StartsWith("Professional", StringComparison.OrdinalIgnoreCase) ? 0 : 1);
                 var issued = c.LicenseIssued ?? (c.LicenseExpiry.HasValue ? c.LicenseExpiry.Value.AddYears(-5) : DateTime.Today);
+                dtpLicenseIssued.MaxDate = DateTime.Today;
                 dtpLicenseIssued.Value = issued > dtpLicenseIssued.MaxDate ? dtpLicenseIssued.MaxDate : issued;
                 dtpLicenseExpiry.Value = c.LicenseExpiry ?? dtpLicenseIssued.Value.AddYears(5);
             }
@@ -202,7 +203,8 @@
                 cbRole.SelectedIndex = -1;
                 chkIsSeasonal.Checked = false;
                 cbLicenseClass.SelectedIndex = 2;
+                dtpLicenseIssued.MaxDate = DateTime.Today;   // MaxDate was fixed at build time; refresh it after midnight
                 dtpLicenseIssued.Value = DateTime.Today;
                 cbLicenseValidity.SelectedIndex = 0;
                 dtpLicenseExpiry.Value = DateTime.Today.AddYears(5);
```

### C2. Trip Report screen can freeze its own form after a failed reload (`isBindingForm` stuck `true`)

- **Root cause:** `LoadData()` sets `isBindingForm = true` and only sets it back at the end of the happy path. If anything between them throws, the `catch` shows the error but never resets the flag. Every control event is ignored while `isBindingForm` is true. From then on, picking a dispatch, typing crates and similar actions silently do nothing until the app restarts.
- The same method also has no `IsDisposed` check after its two `await`s. When the 30-second timer refresh, a theme reload or a logout disposes the view mid-load, the user gets a modal "Failed to load trip completion records … Cannot access a disposed object" box (`TripReportView.cs:1119`).
- **Files:** `Views/TripReportView.cs:1060-1062` (awaits with no guard), `:1076-1098` (flag set and reset), `:1117-1120` (catch)

```diff
--- a/Views/TripReportView.cs
+++ b/Views/TripReportView.cs
@@ -1058,8 +1058,9 @@
             try
             {
                 allDeliveries = await MJdatabase.GetDeliveriesAsync();
                 var activeStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Scheduled", "Pending", "In Transit", "Rescheduled", "Dispatched" };
                 allTripReports = await MJdatabase.GetTripReportsAsync();
+                if (IsDisposed) return;   // the screen was closed or rebuilt while loading
                 var reported = new HashSet<string>(allTripReports.Select(t => t.DispatchNumber ?? ""), StringComparer.OrdinalIgnoreCase);
@@ -1074,7 +1075,8 @@
                 string? keepDispatch = pnlForm.Visible ? GetSelectedDelivery()?.DispatchNumber : null;
                 bool wasBinding = isBindingForm;
                 isBindingForm = true;
+                try {
                 cbDelivery.Items.Clear();
                 if (allDeliveries.Count == 0)
                 {
@@ -1094,7 +1096,8 @@
                     int keepIdx = keepDispatch == null ? -1 : FindDeliveryItemIndex(keepDispatch);
                     cbDelivery.SelectedIndex = keepIdx; // Nov 6: -1 (blank) unless a trip was already picked
                 }
-                isBindingForm = wasBinding;
+                } finally { isBindingForm = wasBinding; }   // always give the form back, even if filling the list failed
                 RefreshCenterData();
                 ApplyPendingPicks();
                 FilterGridData();
@@ -1115,7 +1118,7 @@
             }
             catch (Exception ex)
-            { ErrLog.Error("TripReportView", ex); 
-                MessageBox.Show($"Failed to load trip completion records: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
+            { ErrLog.Error("TripReportView", ex);
+                if (!IsDisposed) MessageBox.Show($"Failed to load trip completion records: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
             }
         }
```

(The `try {` is left unindented to keep the diff small; reformat if you prefer.)

### C3. Crew, Crew Trip History and Security Audit screens use controls after an `await` without checking they still exist

- **Root cause:** all three are `async void` loaders, or fire-and-forget (`_ = LoadDataAsync()`), started from constructors and from `RefreshData()`.
  - After `await`, they touch grids and combo boxes without an `IsDisposed` check.
  - `MainForm` refreshes the active view every 30 seconds when data changed (`MainForm.cs:771-781`), and rebuilds all views on a theme change.
  - If the view is disposed while the query runs, the continuation throws `ObjectDisposedException`. The `catch` then opens a modal "Failed to load …" box.
- **Do not use `IsHandleCreated` as the guard.** These views are constructed (and start loading) before they are added to the page, so the handle may legitimately not exist yet. A check on it would stop the first load from ever filling the screen. Use `IsDisposed` only.
- **Files:**
  - `Views/CrewManagementView.cs:513-520` (also `:73`, started from the constructor)
  - `Views/CrewTripHistoryView.cs:361-371` (also `:56`, `:352`)
  - `Views/SecurityAuditView.cs:210-221` (also `:30`)

```diff
--- a/Views/CrewManagementView.cs
+++ b/Views/CrewManagementView.cs
@@ -511,12 +511,13 @@
                 }
 
                 allCrews = await MJdatabase.GetCrewsAsync();
+                if (IsDisposed) return;
                 RefreshBusyCrew();
                 FilterGridData();
             }
             catch (Exception ex)
-            { ErrLog.Error("CrewManagementView", ex); 
-                MessageBox.Show($"Failed to load crew records: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
+            { ErrLog.Error("CrewManagementView", ex);
+                if (!IsDisposed) MessageBox.Show($"Failed to load crew records: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
             }
```

```diff
--- a/Views/CrewTripHistoryView.cs
+++ b/Views/CrewTripHistoryView.cs
@@ -359,12 +359,13 @@
             try
             {
                 allCrews = await MJdatabase.GetCrewsAsync();
                 allDeliveries = await MJdatabase.GetDeliveriesAsync();
+                if (IsDisposed) return;
                 PopulateCrewDropdown();
                 RebuildHistoryRows();
                 ApplyFilter();
             }
             catch (Exception ex)
-            { ErrLog.Error("CrewTripHistoryView", ex); 
-                MessageBox.Show("Failed to load crew trip history: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
+            { ErrLog.Error("CrewTripHistoryView", ex);
+                if (!IsDisposed) MessageBox.Show("Failed to load crew trip history: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
             }
```

```diff
--- a/Views/SecurityAuditView.cs
+++ b/Views/SecurityAuditView.cs
@@ -207,17 +207,19 @@
             {
                 if (cbAuditCategory.SelectedIndex == 0)
                 {
                     rawDataAuditLogs = await MJdatabase.GetAuditTrailAsync(500);
                 }
                 else
                 {
                     rawLoginLogs = await MJdatabase.GetSecurityAuditLogsAsync(300);
                 }
+                if (IsDisposed) return;
                 ApplyFilter();
             }
             catch (Exception ex)
-            { ErrLog.Error("SecurityAuditView", ex); 
-                MessageBox.Show("Failed to load audit logs: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
+            { ErrLog.Error("SecurityAuditView", ex);
+                if (!IsDisposed) MessageBox.Show("Failed to load audit logs: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
             }
```

---

## 🟠 Medium Risk

### M1. Truck screen fills its busy-truck list before checking the view is still alive

- **Root cause:** `RefreshBusyTrucks()` runs before the `IsDisposed` check. The check only guards `FilterGridData()`. A view disposed during the load still has controls touched. The catch is correctly guarded already.
- **File:** `Views/TruckManagementView.cs:762-767`

```diff
--- a/Views/TruckManagementView.cs
+++ b/Views/TruckManagementView.cs
@@ -760,10 +760,9 @@
                 if (cbStatus.SelectedIndex < 0) cbStatus.SelectedIndex = 0;
 
                 allTrucks = await MJdatabase.GetTrucksAsync();
-                RefreshBusyTrucks();
-                if (!this.IsDisposed)
-                {
-                    FilterGridData();
-                }
+                if (this.IsDisposed) return;
+                RefreshBusyTrucks();
+                FilterGridData();
             }
             catch (Exception ex)
```

### M2. Background alert scan reads the open-forms list from a worker thread

- **Root cause:** `MainForm` starts `ScanOperationalAlerts()` on the thread pool at startup and every 15 minutes (`MainForm.cs:172`, `:763`). It calls `Publish(..., contextControl: null, ...)`, which loops over `Application.OpenForms` and reads `f.Visible` from that worker thread (`EventNotificationService.cs:280-298`).
  - The list can change while it is being enumerated (a dialog opens or closes), which throws "Collection was modified".
  - The exception is caught and ignored, so the user sees nothing, but the toast for that alert is lost.
  - The main form is already available as `MainForm.Instance`, so the loop is not needed in the normal case.
- **File:** `Functions/EventNotificationService.cs:277-299`

```diff
--- a/Functions/EventNotificationService.cs
+++ b/Functions/EventNotificationService.cs
@@ -275,6 +275,8 @@
             try
             {
                 Control? target = contextControl;
+                // Use the main window directly: walking Application.OpenForms from the alert-scan worker thread can race with dialogs opening and closing.
+                if (target == null && MainForm.Instance is { IsDisposed: false, Visible: true } mainWin) target = mainWin;
                 if (target == null)
                 {
                     foreach (Form f in Application.OpenForms)
```

### M3. Repeating "overdue" alerts once there are more than 100 recent notifications

- **Root cause:** `ScanOperationalAlerts()` decides whether an alert was already sent in the last 12 hours by looking at only the newest 100 notifications (`GetSystemNotifications(100, ...)`).
  - Overdue-invoice alerts are published one per invoice. A client with many overdue invoices can push that list past 100 entries.
  - Older alerts then drop out of the window, the check says "not sent yet", and the same alerts are published again every 15 minutes. That floods the bell and the toasts.
- **File:** `Functions/EventNotificationService.cs:530`

```diff
--- a/Functions/EventNotificationService.cs
+++ b/Functions/EventNotificationService.cs
@@ -527,7 +527,7 @@
         {
             try
             {
-                var existingNotifs = MJdatabase.GetSystemNotifications(100, isAdmin: true);
+                var existingNotifs = MJdatabase.GetSystemNotifications(2000, isAdmin: true);   // must cover everything from the last 12 h, or alerts are re-sent
                 DateTime cutoff = DateTime.Now.AddHours(-12);
```

### M4. Statement of Account and Trip Report keep a permanent hook on the static theme event

- **Root cause:** both views subscribe an anonymous lambda to the static `ThemeManager.ThemeChanged` and never unsubscribe. A theme change rebuilds the whole `MainForm` (`MainForm.Shell.cs:358`) and disposes the old views (`MainForm.cs:138`). The static event still holds references to the old views, so every theme switch leaks one full copy of each view (grids, data, fonts). The Trip Report handler also lacks the `IsHandleCreated` check that the Statement of Account one has, so it can throw before the view has a window (the catch hides it).
- **Files:** `Views/StatementOfAccountView.cs:99`, `Views/TripReportView.cs:97`

```diff
--- a/Views/StatementOfAccountView.cs
+++ b/Views/StatementOfAccountView.cs
@@ -96,5 +96,7 @@
             UIThemeHelper.ApplyTheme(this);
             StyleFullPageForm();
             FitItemsGridColumns();
-            ThemeManager.ThemeChanged += (s, e) => { try { if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(FitItemsGridColumns)); } catch (Exception ex) { ErrLog.Swallowed(ex); } };
+            EventHandler onThemeChanged = (s, e) => { try { if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(FitItemsGridColumns)); } catch (Exception ex) { ErrLog.Swallowed(ex); } };
+            ThemeManager.ThemeChanged += onThemeChanged;
+            Disposed += (s, e) => ThemeManager.ThemeChanged -= onThemeChanged;
         }
```

```diff
--- a/Views/TripReportView.cs
+++ b/Views/TripReportView.cs
@@ -94,5 +94,7 @@
             InitReportCenter();
             InitActiveHistoryBar(); // Nov 6 workflow: Active / History (invoiced) and Issue Invoice (Selected)
             FitStoreGridHeaders();
-            ThemeManager.ThemeChanged += (s, e) => { try { if (!IsDisposed) BeginInvoke(new Action(FitStoreGridHeaders)); } catch (Exception ex) { ErrLog.Swallowed(ex); } };
+            EventHandler onThemeChanged = (s, e) => { try { if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(FitStoreGridHeaders)); } catch (Exception ex) { ErrLog.Swallowed(ex); } };
+            ThemeManager.ThemeChanged += onThemeChanged;
+            Disposed += (s, e) => ThemeManager.ThemeChanged -= onThemeChanged;
         }
```

### M5. Saved Trip Report filter dates are applied without a range check

- **Root cause:** `LoadView()` reads the saved filter string from the settings table and assigns the dates straight to the date pickers. A date outside the picker's range (a hand-edited setting, a restored old backup, or a bad value) throws `ArgumentOutOfRangeException` while the Trip Report screen is being built, so that screen cannot open until the setting is deleted. The nearby dropdown values in the same method are range-checked; these two are not.
- **File:** `Views/TripReportView.Center.cs:305-306`

```diff
--- a/Views/TripReportView.Center.cs
+++ b/Views/TripReportView.Center.cs
@@ -302,8 +302,8 @@
                 cbSortF.SelectedIndex = I(p[2], cbSortF);
                 pendingClient = p[3]; pendingDriver = p[4]; pendingTruck = p[5];
-                if (DateTime.TryParse(p[6], out var f)) dtpFromF.Value = f;
-                if (DateTime.TryParse(p[7], out var t)) dtpToF.Value = t;
+                if (DateTime.TryParse(p[6], out var f) && f >= dtpFromF.MinDate && f <= dtpFromF.MaxDate) dtpFromF.Value = f;
+                if (DateTime.TryParse(p[7], out var t) && t >= dtpToF.MinDate && t <= dtpToF.MaxDate) dtpToF.Value = t;
                 UpdateCustomRange();
```

### M6. Special Trip CSV export breaks when a field contains a quote; both exports allow spreadsheet formulas

- **Root cause (a):** the Special Trip history export builds each line with raw `"{t.Company}"` text and does not double embedded quotes. A company or address like `Juan's "Best" Mart` produces a broken row, and the columns shift in Excel.
- **Root cause (b):** the Reports export (and the Special Trip export) write text starting with `=`, `+` or `@` straight into the CSV. Opened in Excel, such a cell runs as a formula. Anyone who can type a client name or remark can plant one (CSV injection).
- **Files:** `Views/SpecialTripHistoryView.cs:701`, `Views/ReportsView.cs:466-467`

```diff
--- a/Views/SpecialTripHistoryView.cs
+++ b/Views/SpecialTripHistoryView.cs
@@ -698,7 +698,9 @@
                             if (r.Tag is SpecialTrip t)
                             {
                                 string flagsVal = r.Cells["colPaymentFlags"].Value?.ToString() ?? "Standard";
-                                string line = $"\"{t.OrderReference}\",\"{t.DispatchDate:yyyy-MM-dd}\",\"{t.Company}\",\"{t.Address}\",\"{t.ServiceType}\",\"{t.TruckPlate}\",\"{t.DriverName}\",\"{t.AideName}\",{t.TotalStopsCount},\"{t.Status}\",\"{flagsVal}\"";
+                                static string Q(string? v) { v ??= ""; if (v.Length > 0 && "=+@".IndexOf(v[0]) >= 0) v = "'" + v; return "\"" + v.Replace("\"", "\"\"") + "\""; }
+                                string line = string.Join(",", Q(t.OrderReference), Q(t.DispatchDate.ToString("yyyy-MM-dd")), Q(t.Company), Q(t.Address), Q(t.ServiceType), Q(t.TruckPlate), Q(t.DriverName), Q(t.AideName), t.TotalStopsCount.ToString(), Q(t.Status), Q(flagsVal));
                                 sb.AppendLine(line);
                             }
```

```diff
--- a/Views/ReportsView.cs
+++ b/Views/ReportsView.cs
@@ -464,6 +464,7 @@
                         for (int i = 0; i < gridReport.Columns.Count; i++)
                         {
                             string raw = row.Cells[i].Value?.ToString() ?? "";
+                            if (raw.Length > 0 && "=+@".IndexOf(raw[0]) >= 0) raw = "'" + raw;   // stop spreadsheet formulas
                             string val = "\"" + raw.Replace("\"", "\"\"") + "\"";
                             sb.Append(val);
```

`DataExportHelper.EscapeCsv` (`Functions/DataExportHelper.cs:232`) and `OrderHistoryView.cs:746` have the same missing formula guard. Apply the same one-line idea there if you want it consistent. Both already escape quotes correctly.

---

## 🟡 Low Risk / Code Smell

### L1. Login screen locks the artwork files while the app runs

- **Root cause:** `Image.FromFile` keeps the file open for the life of the image. If the logo or background file is replaced or deleted (installer update, "change logo" tooling) while the login form is alive, the replacement fails with a file-in-use error. Line 150 also opens the same logo a second time just to make the window icon.
- **File:** `LoginForm.cs:129`, `:149-150`

```diff
--- a/LoginForm.cs
+++ b/LoginForm.cs
@@ -126,7 +126,10 @@
                 if (!System.IO.File.Exists(bgPath)) bgPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "login_bg.jpg");
                 if (System.IO.File.Exists(bgPath))
                 {
-                    rawBackgroundImage = Image.FromFile(bgPath);
+                    using (var fs = new System.IO.FileStream(bgPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
+                    using (var img = Image.FromStream(fs))
+                        rawBackgroundImage = new Bitmap(img);   // copy, so the file is not held open
                 }
@@ -146,7 +149,9 @@
                 else if (System.IO.File.Exists(logoPath))
                 {
-                    cleanLogoImage = Image.FromFile(logoPath);
+                    using (var fs = new System.IO.FileStream(logoPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
+                    using (var img = Image.FromStream(fs))
+                        cleanLogoImage = new Bitmap(img);
                     using (Bitmap bmp = new Bitmap(logoPath))
```

### L2. A wrongly-dated Statement of Account will refuse to open for edit

- **Root cause:** `StatementOfAccountView.cs:1215-1220` assigns `soa.IssueDate` and the dates parsed from `PeriodCovered` directly to date pickers. Loaded dates fall back to `DateTime.Today` when unreadable (`MJdatabase.cs:11234`), so normal data is safe. But a period text such as "Jan 1, 0001 to …" or a year before 1753 would throw. The same unchecked pattern is in `StatementOfAccountView.Guide.cs:498-499`. I found no code path that writes such a value today, so this is a hardening item only.

```diff
--- a/Views/StatementOfAccountView.cs
+++ b/Views/StatementOfAccountView.cs
@@ -1212,10 +1212,10 @@
             txtClientAddress.Text = soa.ClientAddress;
             txtClientTin.Text = soa.ClientTin;
-            dtpIssueDate.Value = soa.IssueDate;
+            dtpIssueDate.Value = soa.IssueDate < dtpIssueDate.MinDate || soa.IssueDate > dtpIssueDate.MaxDate ? DateTime.Today : soa.IssueDate;
             var period = (soa.PeriodCovered ?? "").Split(new[] { " to " }, StringSplitOptions.None);
-            if (period.Length == 2 && DateTime.TryParse(period[0].Trim(), out var pFrom) && DateTime.TryParse(period[1].Trim(), out var pTo))
+            if (period.Length == 2 && DateTime.TryParse(period[0].Trim(), out var pFrom) && DateTime.TryParse(period[1].Trim(), out var pTo)
+                && pFrom >= dtpPeriodFrom.MinDate && pFrom <= dtpPeriodFrom.MaxDate && pTo >= dtpPeriodTo.MinDate && pTo <= dtpPeriodTo.MaxDate)
             {
```

### L3. Other smells noted, no patch proposed

- **Timer refresh during data entry.** The 30-second timer (`MainForm.cs:771-781`) calls `RefreshCurrentActiveView()` whenever something changed. `TripReportView` keeps the picked dispatch across a refresh, and `DeliveryRoutesView` skips the refresh while rows are being typed. I did **not** confirm that `DispatchManagementView.LoadDropdownsAndGridCore` keeps a half-filled form intact. Test this by hand: start a new dispatch, wait for an invoice to go overdue, and see whether the form resets.
- **Database work on the screen thread.** The 30-second lifecycle timer (`RunOperationalLifecycleAutomation`) and the 30-minute snapshot timer run synchronously on the UI thread. On a large database the window can stall for a moment each time. They also fire while a modal dialog is open.
- **Views started from constructors.** `DeliveryRoutesView.cs:266-269` documents a past crash (`NullReferenceException` in `DataGridViewCell.PaintPrivate`) caused by a load started in a constructor whose continuation ran on a background thread. `TruckManagementView.cs:73`, `CrewManagementView.cs:73`, `TripReportView.cs:64`, `CrewTripHistoryView.cs:56` and `SecurityAuditView.cs:30` start the same kind of load in their constructors. I could not tell whether they share that hazard. `DeliveryRoutesView` is the pattern to copy if it recurs.
- **Stale rows in `StoreEntryGuard`.** `picked` (`Functions/StoreEntryGuard.cs:51`) is keyed by grid row and is only trimmed on specific edits (`:281`). Rows dropped when a grid is cleared stay referenced until the guard is disposed. It is a small leak, not a crash.
- **Date pickers capped at "today".** `CrewProfileDialog.cs:323` and `TruckProfileDialog.cs:327` set `MaxDate = DateTime.Today` too. They are dialogs created fresh each time, so the midnight problem in C1 does not apply to them.

---

## Suggested order

1. C1, C2 and C3 (small, local, user-visible).
2. M3 and M2 (alert spam, and lost toasts).
3. M4, M5 and M6.
4. Everything else as time allows.

After applying anything, build with warnings-as-errors (as the project is configured), then run the `Verify` project.
