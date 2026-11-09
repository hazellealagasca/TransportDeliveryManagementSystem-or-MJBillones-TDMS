# Statement of Account: a payment window for the picked trip reports (Nov 10, 2026)

> **Status: PLAN ONLY. No code written.** Three decisions in section 6. Reply with your answers (or "go with your picks") and I build it.

## 1. What you asked for (in my words, please check)

1. In **New Statement of Account** you pick **several trip reports** of one client (this already works: the tick boxes in "Ready service invoices").
2. After picking, a **separate payment window** opens, placed under **Client** and **SOA type**. It lists the picked trip reports and, **per trip report, you type the Amount and the Fuel surcharge**.
3. The window shows the **computation** (total sales, VAT, net of VAT, fuel, withholding, total due) and **adds the totals into the statement**.
4. The picked trips and their stores are **gathered into the "Trips & stores on this statement" table**.
5. In that table and on the **printed statement**, the **Amount** and **Fuel surcharge** cells of a trip are **one tall cell** that spans the trip's store rows (grows with the number of drops) instead of one small cell on the last row.

## 2. How it works today (verified in the code)

| Fact | Where |
|---|---|
| Picking uses the **"Ready service invoices"** tab (`gridPicker`) with a tick per row; `AddTickedDispatches()` copies the ticked trips into the table. | `StatementOfAccountView.Dispatches.cs` |
| The table (`gridItems`) has **one row per store**. `colAmount` / `colFuel` are typed on **one row per trip** (the yellow cells, the last row of the trip in your screenshot). | `StatementOfAccountView.cs` ~636-680 |
| All totals come from **one formula**, `SoaTotals.From(amount, fuel)`: VAT and the 2% withholding on the freight only, fuel added outside both. | `SoaTotals`, `PrintForms.cs` |
| The **print already merges** Amount and Fuel per trip: `rowspan = number of store rows of the trip` (your first screenshot shows this). | `PrintForms.cs` (`PerTrip` columns) |
| Save, checks ("2 trips without an amount") and print all read `colAmount` / `colFuel`. | `Guide.cs`, `Invoices.cs`, `PrintForms.cs` |

So the **numbers already flow correctly**. What is missing is the **window to type them per trip report** and the **merged cell on screen**.

## 3. The design

### 3.1 The payment window (new)
Opens **automatically right after you tick trip reports and press "Add selected"**, and any time from a new button **"Trip payments…"** next to Client and SOA type.

| Part | Behaviour |
|---|---|
| **Header** | Client · SOA type · "N trip reports". |
| **One row per trip report** | Trip report # · Dispatch # · Date · Destination (short) · Drops · **Amount (typed)** · **Fuel surcharge (typed)** · Row total. Only Amount and Fuel are editable. |
| **Starting value** | Blank, or the Rate Matrix amount as a **suggestion** shown grey (you accept or overtype). Never filled silently. |
| **Checks** | Each trip needs an Amount above 0; Fuel may be 0. Red cell + the reason, same style as the Billing checklist. |
| **Computation panel** | Live, from `SoaTotals`: Total sales (VAT incl.) · Less VAT · Net of VAT · Fuel surcharge · Add VAT · Less withholding · **Total amount due**. Same numbers the print shows. |
| **Buttons** | **Apply to statement** (writes the amounts into the table and the totals), **Cancel** (nothing changes). Reopening later edits the same figures. |

### 3.2 The table on the statement screen
- The window **writes into the existing `colAmount` / `colFuel`**, so Save, the checks, the totals box and the print need **no new data path**.
- Amount and Fuel are shown as **one tall merged cell per trip** (painted across the trip's store rows). Click it to edit in place, or use **Trip payments…**.
- Rows stay one per store, so DR numbers and stores keep working.

### 3.3 The print
- **Already** merged per trip. The change is only the **height rule** (decision 2 below): by default keep "one cell as tall as the trip's store rows". If you want height by **number of drops**, the cell is made at least as tall as the drops.

## 4. Build order (each step ships alone)
1. **Window + computation panel** (read/write `colAmount`/`colFuel`, uses `SoaTotals`). Opens after "Add selected" and from **Trip payments…**.
2. **Merged Amount / Fuel cell** on screen (paint only; data unchanged).
3. **Print height rule** (only if you choose drops-based height).
4. **Verify checks:** window totals equal the printed totals; blank amount blocks Apply; editing a trip updates the table and totals; Cancel changes nothing.

## 5. Risks and how they are handled
| Risk | Handling |
|---|---|
| Two places to type the same amount | The window is the main place; the table cell is the same value and updates it. One source: `colAmount`/`colFuel`. |
| Old statements | Untouched: they keep their saved figures and print as before. |
| A trip report with no store rows | Gets one row, so it still has an Amount cell. |

## 6. Decisions I need
1. **Where the window opens.** (a) **Automatically after "Add selected" and from a "Trip payments…" button (my pick)**, (b) only from the button.
2. **"Match the number of drops".** The Amount/Fuel cell is today as tall as the trip's **store rows** (your trip 1 has 3 store rows but "6 drops"). (a) **Keep store rows (my pick)**, because every row has a store; (b) make it as tall as the **drops count** (6 rows high, extra blank space).
3. **Suggested amounts.** (a) **Show the Rate Matrix amount as a grey suggestion (my pick)**, (b) always blank.
