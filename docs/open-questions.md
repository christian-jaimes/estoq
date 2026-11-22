# Open questions

Items still waiting on the product owner, plus research. Each has a working default that the feature docs follow until answered. Remove a row once decided and update the feature doc and [[decisions]].

Mockups are a visual reference only; features are specified in these docs. A mockup that differs from a doc is not a conflict.

## Product decisions

| # | Question | Working default | Affects |
|---|---|---|---|
| Q12 | Settings sections beyond the plan: Defaults and rules (discount cap, inactivity timeout, return window, low-stock default) and Data (backup) | Add both after the four planned sections; Data arrives with [[backup]] | [[settings]] |
| Q13 | Inventory import: the mockup has an Import button, no flow is defined | Button hidden until a CSV import flow is specified | [[inventory]] |
| Q21 | Owner password rules: the setup wizard asks for a password and confirmation but no rule is specified | At least 8 characters, no other rule (`SetupRules.MinPasswordLength`) | [[setup-wizard]], [[sign-in]] |
| Q15 | `.gitignore` excludes `branding/`, which on Windows also ignores `Branding/` | Unknown; confirm whether intentional | repo |
| Q16 | Complete Sale key: POS mockup shows F2, which opens Add product | Ctrl+Enter completes the sale | [[point-of-sale]] |

## Deferred (to be decided later)

| # | Item | Until then |
|---|---|---|
| Q20 | Email receipts | Not in the MVP; receipts print only ([[point-of-sale]]) |

## Research and hardware

| # | Item | Needed by |
|---|---|---|
| R1 | Label printer model and label size | [[label-printing]] |
| R2 | Receipt printer and receipt layout (maybe the same device) | [[point-of-sale]] back end |
| R3 | Encryption spike: library chosen (SQLite3 Multiple Ciphers); prove versions, migrations, backup and speed with the checklist in [[encryption]] | Back end, before real data is written |
| R4 | Google verification for the Drive app data scope (long lead time) | [[backup]] |
| R5 | Restore on a new computer with no internet | [[backup]] |
| R6 | Lockout growth curve (working default: 1 minute after five failures, doubling each further five, max 15 minutes; inactivity timeout decided: 3 minutes) | [[sign-in]] back end |
| R7 | Expected scale (default 10,000 variants, 100,000 sales) | Back end performance tests |
| R8 | Code signing certificate for MSIX | Release |
