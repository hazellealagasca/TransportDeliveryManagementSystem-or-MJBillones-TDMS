# "Keep my changes" option on the Unsaved-changes pop-up — Plan

> **Status: PLAN ONLY. No code has been written.** Please review and answer the open questions at the bottom before implementation starts.

**Goal:** When the "Unsaved changes" pop-up appears, the user can choose to **keep** what they typed, leave the form, and find it again exactly as they left it when they come back.

**Spec:** the screenshot — title *Unsaved changes*, text *"Discard your unsaved changes? What you typed in this form will be cleared."*, buttons **Yes** / **No**.

---

## 1. What happens today

The pop-up is shown from three places. All three use the same text and Yes/No buttons:

| Place | File | Trigger |
|---|---|---|
| `FormGuard.ConfirmDiscard` | `Functions/FormGuard.cs:53` | Back / Close-form / "+ New" toggle button, module and tab switches (`ConfirmLeave`) |
| `EscapeGuard.AllowClose` | `Functions/EscapeGuard.cs:11` | Esc pressed on an open add/edit form |
| `SpecialTripFormDialog.OnFormClosing` | `Forms/SpecialTripFormDialog.Page.cs:191` | Closing the full-page Special Trip form |

Today **Yes = discard** (form is cleared via `DiscardAndCloseForm()` → `SetFormVisibility(false)` → `ClearFields()`), **No = stay on the form**. There is no way to leave *and* keep the typing.

Facts that make "keep" feasible:

- `MainForm` caches every module view in `viewCache`, and each hub (`OperationsHubView`, `FinanceHubView`, `MasterDataHubView`, `ResourcesHubView`) caches its tab views in `cachedViews`. Leaving a page only detaches the control; the typed values are still in memory. **Keep needs no storage — it only has to not call `ClearFields()`.**
- `FormDirtyTracker` already knows whether the form has unsaved typing.
- 12 screens implement `IUnsavedForm` (Billing, Collection, Crew, Dispatch, Incident, RateMatrix, SOA, TripReport, Truck, User, SpecialTripHistory, plus the Category tab via `EscapeGuard`).

Facts that limit it:

- Views are **disposed on logout / exit and rebuilt on theme change** (`MainForm.cs:136`, `ReloadForTheme`). A kept draft cannot survive those in v1. In those cases the pop-up must **not** offer Keep (see Task 4).
- `OpenNewRecordForm()` calls `ClearFields()` first, so "+ New" would wipe a kept draft unless changed.

> **⚠ Base branch.** `FormGuard.cs`, `EscapeGuard.cs`, `IUnsavedForm`, the SpecialTrip page form and the `ConfirmLeave` calls in `MainForm.cs` exist **only on `workflow-overhaul-2026-11-06`** (commit `6374bc0`, `3a015e1`). This worktree (`claude/popup-keep-changes-option-1e3804`) is at master `d0d5fa2` and does not contain them. **Implementation must start from `workflow-overhaul-2026-11-06`** (new branch off it), not from this worktree as it is.

---

## 2. Proposed behavior

New three-choice pop-up:

```
 Unsaved changes
 You have unsaved changes on this form.

 [ Keep for later ]   [ Discard ]   [ Stay here ]
```

| Button | Result |
|---|---|
| **Keep for later** (default, primary) | Leaves the form / page. Typed values stay in the form. Coming back restores it as left. |
| **Discard** | Same as today's *Yes*: form is cleared and closed. |
| **Stay here** | Same as today's *No*: nothing changes. Esc and the title-bar ✕ also mean *Stay here*. |

Why *Keep for later* is the default: it is the only non-destructive way to leave. Today's default is *No* (safe); this keeps that principle.

**Coming back:**

1. Opening the same module/tab again shows the form already open with the kept values, plus a small note at the top of the form: *"Restored your unsaved changes."* with a **Discard** link.
2. If the person left via the in-page Close/Esc (same page, list visible): the "+ New" button reads **Resume draft** while a kept draft exists. Clicking it reopens the kept form. It never blanks the form.
3. The kept draft is **one slot per screen**. Clicking a row to edit another record while a draft is kept first asks *"You have kept changes. Replace them with this record?"* (Replace / Cancel). Saving the form successfully clears the kept flag.
4. A kept form that is still dirty asks the same three-choice question again the next time it is left.

**Not offered (two-button Discard / Stay only):** logging out, closing the program, switching the theme — because the view is destroyed. The text there says *"Leaving now will clear what you typed."*

---

## 3. Files

| File | Change |
|---|---|
| `ThemedMessageBox.cs` | Add an overload that takes custom button labels/results, so we are not stuck with Yes/No/Cancel wording. Existing `Show(...)` calls unchanged. |
| `Functions/FormGuard.cs` | `LeaveChoice` enum; `IUnsavedForm.KeepAndCloseForm()` + `HasKeptDraft`; `ConfirmDiscard`/`ConfirmLeave` gain `allowKeep` parameter; new `AskLeave(...)` that is the one place the pop-up text lives. |
| `Functions/EscapeGuard.cs` | Use `FormGuard.AskLeave` instead of its own copy of the message. |
| `Forms/SpecialTripFormDialog.Page.cs` | Use `FormGuard.AskLeave`; Keep = hide page form without resetting `dirtyTracker`. |
| 11 views implementing `IUnsavedForm` | Implement `KeepAndCloseForm()`, `HasKeptDraft`, "Resume draft" label, restore-on-show. |
| `MainForm.cs`, `MainForm.CloseGuard.cs`, `MainForm.Shell.cs`, hub views | Pass `allowKeep: true` for module/tab switches; `false` for logout, exit, theme toggle. |
| `Docs/MJ_Billones_TDMS_User_Manual.docx` | One short paragraph under the form-handling section. |

To avoid copy-pasting the same ~15 lines into 11 views, put the shared behavior in one helper in `FormGuard.cs`:

```csharp
public enum LeaveChoice { Stay, Discard, Keep }
public static LeaveChoice AskLeave(IWin32Window? owner, bool allowKeep);
```

and let each view's `KeepAndCloseForm()` be a one-liner that calls its existing `SetFormVisibility(hide, keepValues: true)`.

---

## 4. Tasks

Each task ends with the app building (`dotnet build`) and a manual check. The repo has no unit-test project (only `scratch/*.ps1` and `*.py` scripts), so checks are written as manual steps plus one small harness script, per the project's existing verify-harness pattern run against a **copy** of the real database.

### Task 0 — Branch setup
- [ ] Create a new branch from `workflow-overhaul-2026-11-06` (not from master).
- [ ] Confirm `Functions/FormGuard.cs` exists and `dotnet build` passes before touching anything.

### Task 1 — Three-button pop-up
**Files:** `ThemedMessageBox.cs`, `Functions/FormGuard.cs`

**Interfaces — produces:**
- `MessageBox.Ask(IWin32Window? owner, string text, string caption, MessageBoxIcon icon, (string Text, DialogResult Result)[] buttons, int defaultIndex, DialogResult escapeResult) : DialogResult` — same themed look as `ShowThemed`; the first button is styled primary only when its result is not Cancel.
- `FormGuard.AskLeave(IWin32Window? owner, bool allowKeep) : LeaveChoice`

- [ ] Refactor `ShowThemed` so button definitions come from a parameter (the `defs` array at `ThemedMessageBox.cs:152`); old `MessageBoxButtons` overloads build the array and call the same code.
- [ ] `AskLeave` shows Keep for later / Discard / Stay here when `allowKeep`, else Discard / Stay here. Title `Unsaved changes`; Esc and ✕ return `Stay`.
- [ ] Check: temporarily call `AskLeave` from a test button; each button returns the right `LeaveChoice`; Esc and ✕ return `Stay`; dark theme looks right; the existing Yes/No pop-ups elsewhere look unchanged.

### Task 2 — Keep on one screen (Crew), end to end
Do one screen first so the pattern is proven before copying it to the rest.

**Files:** `Functions/FormGuard.cs`, `Views/CrewManagementView.cs`

**Interfaces — produces:**
- `IUnsavedForm.KeepAndCloseForm() : void` — hides the form, does **not** call `ClearFields()`, does **not** reset `formDirty`.
- `IUnsavedForm.HasKeptDraft { get; } : bool`
- `FormGuard.ConfirmDiscard(Control view, bool allowKeep = true) : bool` — returns true when the person may leave; on `Keep` it calls `KeepAndCloseForm()`, on `Discard` it calls `DiscardAndCloseForm()`, on `Stay` returns false.

- [ ] In `CrewManagementView`: add `keptDraft` flag; `SetFormVisibility(false)` skips `ClearFields()` and skips `formDirty.Reset()` when called from `KeepAndCloseForm`.
- [ ] `OpenNewRecordForm()`: if `keptDraft`, show the form without `ClearFields()` and clear the flag.
- [ ] `btnToggleForm.Text` is **Resume draft** while `keptDraft`.
- [ ] `OnVisibleChanged(Visible == true)` with `keptDraft` → reopen the form and show *Restored your unsaved changes* with a Discard link.
- [ ] Row-click while `keptDraft` → *Replace them with this record?* prompt.
- [ ] Successful Save/Update clears `keptDraft`.
- [ ] Check (manual, real db copy): type a name in Crew → click another sidebar module → pop-up → *Keep for later* → return to Crew → name is still there, note shown. Repeat with *Discard* (form blank), *Stay here* (still on form), Esc (still on form). Keep → Save → leave again → no pop-up.

### Task 3 — Same behavior on every other screen
**Files:** `Billing`, `Collection`, `Incident`, `RateMatrix`, `StatementOfAccount`, `TripReport`, `Truck`, `User`, `Dispatch` views; `Forms/SpecialTripFormDialog.Page.cs`; `Views/SpecialTripHistoryView.Page.cs`; `Functions/EscapeGuard.cs`; `Views/CategoryManagementView.cs`.

- [ ] Apply the Task 2 pattern to each view. Dispatch, Trip report and Incident also have `DraftAutoSaveService` banners — confirm the kept draft and the auto-save draft don't both offer to restore (if they do, the kept draft wins and the auto-save banner is suppressed that time).
- [ ] `EscapeGuard.AllowClose` → `FormGuard.AskLeave(owner, allowKeep: true)`; Esc + Keep behaves like the Close button + Keep.
- [ ] Special Trip full-page form: Keep hides the page form and keeps `dirtyTracker` dirty; `HasUnsavedTyping` stays true; re-entering the Special Trip tab shows the page with the typing.
- [ ] Check: repeat the Task 2 manual check on each of the 12 screens (list in the checkbox set at the end of this file).

### Task 4 — No Keep where the view is destroyed
**Files:** `MainForm.CloseGuard.cs`, `MainForm.Shell.cs`, `Views/OperationsHubView.cs`

- [ ] `CanLeaveCurrentView()` (used by logout/exit and the theme toggle) calls `ConfirmLeave(root, allowKeep: false)`.
- [ ] Hub/tab/module switching keeps `allowKeep: true`.
- [ ] Check: with a dirty form, window ✕ → *Stay / Yes, log out* → pop-up shows only Discard / Stay here; toggle theme → same.

### Task 5 — Regression check + manual
- [ ] Run the "no System Error popups" check (project rule) and the existing `scratch/test_ui_layouts.ps1`.
- [ ] Open every screen, type nothing, leave → **no** pop-up (unchanged).
- [ ] Add a paragraph to the user manual (`Docs/MJ_Billones_TDMS_User_Manual.docx`/`.pdf`) describing the three choices.
- [ ] Commit per task with message prefix `feat(forms):`.

---

## 5. Review focus (cases most likely to bite)

1. **Kept draft + "+ New" wipes it.** Must restore instead of `ClearFields()` — Task 2.
2. **Kept draft + save of a different record.** Saving record B must not clear or reuse draft A's values — row-click replace prompt.
3. **Keep, then the data underneath changes** (e.g. truck deleted by another screen, dispatch number taken). Restored combos can point at a missing item; the form must fall back to blank for that field, not crash or show a System Error.
4. **Draft kept on a screen the user later loses permission to** (role change) — the restore must respect `Permissions` and not reopen the form.
5. **Two dirty forms stacked** (hub tab A kept, now tab B dirty, leave the hub) — each asks in turn; *Stay here* on the second must not undo the first Keep.

---

## 6. Open questions

1. **Meaning of "keep".** I read "keep the changes when coming back" as *leave now, find my typing again later*. If you instead meant "stay on the form and keep typing", that is already the **No** button; say so and I will only rename it.
2. **Survive logout / app restart?** v1 is in-memory only (gone on logout, exit, or theme switch). Surviving restarts means saving drafts to the database (`DraftAutoSaveService` already does this for Trip report and Incident on master). Worth doing as a second step if wanted.
3. **Button wording.** *Keep for later / Discard / Stay here* — happy to change (e.g. *Save as draft*).
4. **Row-click while a draft is kept**: replace-with-prompt (proposed) or always discard silently?

---

## 7. Screens to verify (Task 3 checklist)

- [ ] Dispatch order
- [ ] Trip report
- [ ] Incident report
- [ ] Special trip
- [ ] Billing (invoice)
- [ ] Collection receipt
- [ ] Statement of account
- [ ] Clients (note: separate "Save before leaving?" prompt at `ClientManagementView.cs:834` — decide if it joins this)
- [ ] Crew
- [ ] Trucks / Categories
- [ ] Rate matrix
- [ ] Users

---

## 8. Implementation notes (built 2026-11-09)

Built on a branch fast-forwarded to `workflow-overhaul-2026-11-06`. Where the build differs from the plan above:

- **Restored note** is a small corner notice ("Restored your unsaved changes."), not a banner with a Discard link inside the form. Discard is still one click away (Back, then Discard).
- **Task 4 needed no code.** Logout, exit and theme change never go through `FormGuard`, so Keep can never be offered there. `ConfirmLeave(..., allowKeep: false)` exists for any future caller that destroys the screen.
- **Open question 4 (row-click while a draft is kept):** a different record opened from the list replaces the kept draft without asking. "+ New" / "Resume draft" and returning to the screen always bring the draft back first. (The draft is only waiting hidden after Keep from the in-page Close/Esc.)
- **Category items form** does not hold a dirty flag, so it keeps its old Esc behavior (Discard / Stay here only).
- **Special Trip form** is hosted as a page; Keep hides the form instance and shows it again when the tab is reopened.
- **Not done:** user manual paragraph (Task 5) — the manual is a `.docx`/`.pdf` pair, left for a separate edit.
- **Checks:** `Verify/Checks.KeepDraft.cs` — for 10 screens: Keep hides the form, typing intact, button reads "Resume draft"; resume via the add button and via returning to the screen restores the same typing and keeps it "unsaved"; Discard still clears; a new form afterwards is blank. Run: `dotnet run --project Verify -- KeepDraft_`.
