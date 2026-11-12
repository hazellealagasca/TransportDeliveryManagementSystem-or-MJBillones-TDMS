# Aide and Driver boxes: "Available" can't be erased, and "not found → add?" (12 Nov 2026)

## What the user sees (master ae841da)
1. **Aide 1 / Aide 2:** the box is a free-text box (`MakeAideSlot`, Views/DispatchManagementView.cs:6613) and each item's
   text is "DELA CRUZ, JUAN  ·  Available". Clicking and typing edits that text, so Backspace erases "Available"
   (and any typing breaks the name, which then snaps back on leaving the box).
2. **Driver and Aide:** when nothing matches the typed name, the list only says
   "No driver matches 'xyz'" (driver) or shows nothing (aide). There is no way to add the person from there.

## Fix
1. **Aide boxes work like the Driver and Truck boxes.** Change both slots to the same search box (`SearchPicker`):
   the box is not editable text; typing opens the search list with a "Search: juan▏" row and filters by name.
   The status ("Available" / "On this order") is **drawn as a chip on the right**, not part of the text, so it can't be
   erased. Picking, Clear, "Pick 2 free aides", hiding the other slot's aide and "fill Aide 1 first" stay as now.
2. **Not found → add.** When the typed name matches nobody, the search list shows:
   ```
   Driver "Juan Santos" cannot be found.
   [ + Add driver ]      Esc to close
   ```
   (same for the aide boxes: "Aide "…" cannot be found." / [ + Add aide ]). Enter or a click on the row opens a small
   **Add driver / Add aide** window on top of the dispatch screen, so the order being typed is not lost:
   - Last name, First name, Middle name (optional), Contact number (optional). Role fixed to Driver or Aide.
   - The typed text is put in as a starting point (e.g. "Juan Santos" → First "Juan", Last "Santos"; editable).
   - Same checks as the Crew screen: name standard (`NameHelper`), letters only, phone format, duplicate check
     (`ValidateDuplicateCrew`). Problems show inside the window, no pop-ups.
   - Save → the person is added as active and available, the list reloads, and **the new person is picked** in the
     box that asked. Cancel → back to the box, nothing changed.
   - Optional details (photo, classification, wage) are filled in later on the Crew screen, as today.

## Not changing
Who can be assigned (only active, free crew), the 2-aide limit, how crew are saved on the Crew screen, any business rule.

## Where
- `Functions/SearchPicker.cs`: optional "not found" action (text + callback) drawn in the empty-match row; Enter/click runs it.
- `Views/DispatchManagementView.cs`: aide slots become owner-drawn search boxes with a status chip; wire "Add driver" /
  "Add aide".
- New `Forms/QuickAddCrewDialog.cs`: the small add window (reuses the Crew screen's checks and `MJdatabase.SaveCrew`).

## Checks
- Verify: typing in an aide box never changes the "Available" chip; picking an aide still works; slot 2 hides slot 1's aide.
- Verify: no match in Driver → "Driver "…" cannot be found" row; the add window saves a new driver (fresh db) and he is
  picked in the Driver box. Same for an aide. A duplicate name is refused inside the window.
- Existing SearchPicker / dispatch aide checks still pass; zero-warning build; screenshots of the aide box and the
  not-found row.
