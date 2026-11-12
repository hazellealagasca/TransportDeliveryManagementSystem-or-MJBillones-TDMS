# Validation audit: every input, start to finish (12 Nov 2026, master e50ecb7)

How to read this: for each step of the business process, **Checked now** is what the system already refuses or
warns about on save, and **To add** is what is missing. Nothing here changes a business rule (statement-first
billing, no partial payments, DR 4–8 digits, TIN required, store and gate times required, crew pay off client
documents, special trips and incidents hidden).

Priority: **P1** = wrong data can get in or print today. **P2** = easy typo that should be caught.
**P3** = polish (how the problem is shown).

---

## Set up once

### 1. Users and sign-in (Settings › Users, Forgot password, My profile)
**Checked now:** username required, at least 3 characters, unique; password rules; last, first and middle name
(letters, 2–60 characters, name standard); role required; Philippine mobile number; you can't deactivate yourself
or the last active administrator.
**To add:** nothing required.

### 2. Company details (Settings › Branding): printed on every SOA, invoice and receipt
**Checked now:** nothing. Each box saves as soon as you leave it.
**To add (P1):**
- Company name required. A blank name is not saved; the box keeps the old name and shows why.
- Company TIN: same rule as the client TIN (9 digits, or 12 with the branch code), with the same 000-000-000-000
  formatting as you type.
- Contact number: same phone check as the client screen (optional).
- Lengths: name 150, tagline 150, address 250, invoice footer 300 characters.

### 3. Clients (Master Data › Clients)
**Checked now:** company name, contact person and contact number required; TIN required (9 or 12 digits);
phone shape; email has "@" and "."; no duplicate client name; address optional.
**To add:**
- P2: Email: a real address pattern (`name@domain.tld`). "a@." passes today.
- P2: Contact person: letters, spaces, period, hyphen and apostrophe only (no digits).
- P2: Lengths: company name 150 (the dispatch screen already refuses more than 150), address 250, contact
  person 100, contact number 20, email 100.

### 4. Stores and routes (Clients › stores, + New route, Delivery routes)
**Checked now:** store code (letters, numbers and hyphens, max length), store name length, address 200; route
code required and unique; origin and destination required and different; no duplicate route for the client;
at least one store; similar routes and shared stores are flagged.
**To add:** nothing required.

### 5. Rates (Master Data › Rate matrix)
**Checked now:** destination required, truck type required, rate above ₱0, no duplicate rate (route, truck type, season).
**To add (P2):** a warning, not a block, when a rate is more than 3 times or less than a third of the other rates
for the same truck type ("₱45,000 is much higher than the other 6-tonner rates (₱4,000–₱6,500). Save anyway?").
This catches an extra zero.

### 6. Trucks (Resources › Trucks, Truck profile)
**Checked now:** plate number required, Philippine LTO plate format, unique; crate capacity above 0; category
required; crate type required; a truck on an open dispatch can't be set to maintenance.
**To add (P2):** in the truck profile, an insurance or registration date that is already expired shows a red note
("Registration expired on …"). Saving is still allowed.

### 7. Crew (Resources › Crew)
**Checked now:** last name, first name and role required; name standard; letters only; phone format; duplicate
check; drivers need a license, and an expired license can't be saved.
**To add (P1):** **the new "+ Add driver" window (added today) does not ask for the license**, so a driver can be
saved without one, which the Crew screen never allows. Fix: the Add driver window also asks for license type and
expiry date, with the same rule (Professional, or Non-Professional with the same confirmation; expired is refused).
The Add aide window stays as it is.

---

## Every day

### 8. Dispatch order (Operations › Dispatch)
**Checked now:** dispatch number required and unique; client required; at least one store; truck, driver and at
least one aide required; temp type required; crates above 0; client name up to 150 and destination up to 200
characters; driver, aide and truck must be free; schedule conflicts; capacity check with a reason when overloaded;
a past date asks for confirmation; added store rows can't be blank.
**To add (P2):**
- Dispatch number: letters, digits, "-" and "/" only, up to 30 characters (today any text is accepted, for example
  a space inside or a pasted line break).
- Remarks or notes boxes: up to 500 characters.

### 9. Trip report (Operations › Trip reports, store details)
**Checked now:** dispatch picked; for every store, a DR number (4–8 digits, used only once, not used on another
dispatch), time in and time out; plant gate time in and out (4-digit 24-hour time); "Received by" names (letters
only); delivered crates above 0; store time out earlier than time in asks for confirmation; returned crates that
don't match ask twice.
**To add (P2):** plant gate **time out earlier than time in** asks for confirmation ("Truck left the plant at 0130,
before it came in at 2042. Is this an overnight trip?"). Today only the format is checked. This is a confirmation,
not a block, because overnight trips are real.

### 10. Statement of Account (Finance › Statement of Account)
**Checked now:** client, delivery type (REF or DRY) and "Prepared by" required; at least one line; period end not
before start; total not negative; client TIN required; DR on every line (4–8 digits, billed once); special trips
not on two statements; the checklist must pass; a blank SI number is allowed but warned.
**To add (P2):**
- "Prepared by" (and "Checked by" / "Approved by" if shown): a name, letters only, up to 100 characters.
- An SI number typed on the statement uses the same check as the invoice screen (letters, digits, "-" and "/",
  maximum length, not used before), so the statement doesn't save an SI number that the invoice screen would refuse.

### 11. Service invoice (Finance › Service invoices)
**Checked now:** completed trip picked; not invoiced twice; SI number format, length and uniqueness; client
required; an updated total can't be below what was already collected.
**To add:** nothing required.

### 12. Collection receipt (Finance › Collections)
**Checked now:** statement or invoice picked; receipt number 4–8 digits, never reused; payment date (not in the
future, not before the invoice); payment method required; reference required for non-cash payments, never used on
another receipt; full amount only; "I checked the overall amount" ticked.
**To add (P2):** payment reference (check or transfer number): letters, digits and "-" only, up to 30 characters.

---

## Across all screens

- **P2: Length limits on every box that prints.** About 30 boxes have no limit today (client screen, statement
  screen, crew window and others). Give each one a limit that fits its column on the printout.
- **P2: Tidy on save.** Trim spaces and turn double spaces into one in every name, address and number box (the
  client screen already does this; the others don't all do it).
- **P3: One way to show problems.** Dispatch, trip report, routes, receipts and the Add driver window list
  problems inside the form, in red, next to the box. Clients, trucks, rates, users and the statement still show
  them one at a time in a pop-up. Move those five screens to the same in-form list (`ValidationHelper`
  already exists for it). This is the largest item and can be done screen by screen later.

## Suggested order
1. **P1:** Add driver window asks for the license (step 7); company details checks (step 2).
2. **P2:** client email, contact person and lengths; dispatch number format; gate time order; SOA "Prepared by"
   and SI check; receipt reference; rate warning; truck expiry note; length limits and tidy on save.
3. **P3:** in-form problem list for clients, trucks, rates, users and the statement.

## Checks
A Verify check for every new rule (refused value, accepted value), a zero-warning build, the related checks on a
copy of the database, and screenshots of the new messages.
