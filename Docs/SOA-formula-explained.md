# How the Statement of Account (SOA) Amount Is Computed

This is the one formula the system uses. The screen, the saved statement, the printout, the invoices and the receipt all use it, so they always show the same numbers.
(Source: `Functions/SoaTotals.cs`, from the company's statement sheet.)

## The idea in one line

> **VAT and withholding tax apply only to the freight. The fuel surcharge is added as it is.**

## Step by step

| # | Line on the statement | Formula | Meaning |
|---|---|---|---|
| 1 | **Total Sales (VAT inclusive)** | Sum of the **Amount** of every trip on the statement | The freight total. It already has 12% VAT inside it. |
| 2 | **Less: VAT** | `Total Sales ÷ 1.12 × 0.12` | Takes the 12% VAT out of the VAT-inclusive total. |
| 3 | **Net of VAT** | `Total Sales − VAT` | The freight before VAT. |
| 4 | **Fuel Surcharge** | Sum of the trips' fuel charge | Added as is. No VAT and no withholding on it. |
| 5 | **Withholding Tax** | `Net of VAT × 2%` | The tax the client deducts from payment (EWT, 2%). |
| 6 | **TOTAL AMOUNT DUE** | `Net of VAT + Fuel + VAT − Withholding` | What the client actually pays. |

Every peso figure is rounded to 2 decimals (0.005 rounds up).

## The short version

Steps 3 and 6 simplify to:

```
Total Amount Due = Total Sales + Fuel Surcharge − Withholding Tax
```

Net of VAT plus VAT is just Total Sales again. So the client pays the freight (VAT already included), plus fuel, minus the 2% withholding.

## Worked example

Statement with freight of **₱10,000.00** (VAT inclusive) and fuel surcharge of **₱500.00**:

| Line | Computation | Amount |
|---|---|---|
| Total Sales (VAT inclusive) | given | ₱10,000.00 |
| Less: VAT | 10,000 ÷ 1.12 × 0.12 | ₱1,071.43 |
| Net of VAT | 10,000.00 − 1,071.43 | ₱8,928.57 |
| Fuel Surcharge | given | ₱500.00 |
| Withholding Tax (2%) | 8,928.57 × 0.02 | ₱178.57 |
| **Total Amount Due** | 8,928.57 + 500.00 + 1,071.43 − 178.57 | **₱10,321.43** |

Check with the short version: 10,000.00 + 500.00 − 178.57 = **₱10,321.43**. ✔

## Things to remember

- **The fuel surcharge is never taxed.** It is not part of the VAT base and not part of the withholding base.
- **Withholding is taken from Net of VAT, not from Total Sales.** That is why it is 2% of ₱8,928.57 and not 2% of ₱10,000.
- **The withholding rate defaults to 2%.** It is the `DefaultWithholdingRate` constant in `SoaTotals.cs`.
- **When a statement is split into several invoices**, the withholding is shared out in proportion to each invoice's freight. The last invoice takes any leftover centavos, so the shares add up exactly to the statement's withholding.
