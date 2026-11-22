# Decisions

Architecture decision log. Replaces the legacy ADR files. Add new decisions at the bottom with the next number; mark replaced ones instead of deleting them.

| # | Decision | Status | Date |
|---|---|---|---|
| 1 | Layered projects (Core, Application, Infrastructure, App), MVVM with CommunityToolkit.Mvvm, Microsoft DI. Views never touch data. | Accepted; only App exists today | 2026-11-19 |
| 2 | SQLite with enforced foreign keys, WAL, migrations applied at startup; backups via the online backup API | Accepted, back-end phase | 2026-11-19 |
| 3 | Money as `decimal` in code, integers scaled by 10,000 in SQLite; timestamps UTC | Accepted | 2026-11-19 |
| 4 | Data in `%ProgramData%\estoq`, shared by every Windows account on the PC; survives uninstall | Accepted | 2026-11-19 |
| 5 | Stock changes only through an append-only movement ledger; cached on-hand quantity | Accepted | 2026-11-19 |
| 6 | Windows App SDK 1.8 stable, self-contained MSIX, all package versions pinned; 2.x upgrade is deliberate, post-MVP | Accepted | 2026-11-19 |
| 7 | User code plus PIN or password | Replaced by 11 | 2026-11-19 |
| 8 | Defaults: tax-exclusive prices; keyboard-wedge barcode scanners, no scanner SDK; standard Windows printing; Windows 10 1809 floor, 22H2 and 11 tested; scale 10,000 variants and 100,000 sales | Accepted; tax rate default 0 % (decision 23) | 2026-11-19 |
| 9 | Bundled Manrope, Poppins and a Material Symbols Rounded subset | Accepted | 2026-11-19 |
| 10 | Logging with Microsoft.Extensions.Logging and a small rolling file provider | Accepted | 2026-11-19 |
| 11 | Six-digit code signs in without a PIN; PIN behind a feature flag; lockout, generic errors, approval for sensitive actions, auto-lock | Accepted | 2026-11-20 |
| 12 | Encryption at rest, key under DPAPI machine scope, recovery key shown once. Library: SQLite3 Multiple Ciphers (MIT licence) through its SQLitePCLRaw bundle (`SQLitePCLRaw.bundle_e_sqlite3mc`) with EF Core; see [[encryption]] | Accepted by the product owner; spike to confirm migrations, backup and speed (R3) | 2026-11-21 |
| 13 | Multi-currency: USD base, EUR, COP, VES; append-only rates; each sale and payment snapshots its rate; split payments; refunds capped at the original base amount | Accepted | 2026-11-20 |
| 14 | Automatic encrypted Google Drive backup to the app data folder, local copy, offline retry | Proposed; research open (R4, R5) | 2026-11-20 |
| 15 | English and Spanish in the MVP; per-user app language; store language for receipts and labels | Accepted | 2026-11-20 |
| 16 | Shortcuts: F1 point of sale, F2 add product, F3 stock adjustments, F4 returns | Accepted by the product owner | 2026-11-21 |
| 17 | Build order: front end on sample data behind service interfaces, then back end, then extra features | Accepted by the product owner | 2026-11-21 |
| 18 | Database naming follows CLAUDE.md: snake_case, string `_id` identifiers | Accepted | 2026-11-21 |
| 19 | Sign-in: six-digit code is the main path; email and password optional, required for the owner (administrator); no usernames | Accepted by the product owner | 2026-11-21 |
| 20 | Roles Owner, Manager, Cashier; account status Active or Inactive | Accepted by the product owner | 2026-11-21 |
| 21 | Sidebar: Dashboard, Inventory, Sales; Account settings at the bottom. Dashboard follows the analytics visual reference (mockup 8) | Accepted by the product owner | 2026-11-21 |
| 22 | Setup wizard: Welcome, Owner information, Store information, Preferences, Recovery key (with safe-keeping guidance); no install or data location choice (MSIX default location, data in `%ProgramData%\estoq`); fuller onboarding later | Accepted by the product owner | 2026-11-21 |
| 23 | Payment methods in order Cash, Card, Transfer, Zelle, USDT, Mobile payment, PayPal, Other; tax default 0 %, set in the wizard, changed in settings | Accepted by the product owner | 2026-11-21 |
| 24 | Add product follows the generated-name flow (product type, brand, model label, duplicate check) | Accepted by the product owner | 2026-11-21 |
| 25 | Stock adjustments: several items per adjustment, one type and reason per adjustment | Accepted by the product owner | 2026-11-21 |
| 26 | No customer data captured in the MVP | Accepted by the product owner | 2026-11-21 |
| 27 | Mockups are visual references for UI elements; features are specified in the docs | Accepted by the product owner | 2026-11-21 |
| 28 | Dashboard MVP is the overview only; tabs later. Email receipts deferred | Accepted by the product owner | 2026-11-21 |
| 29 | Business rules built in `estoq.Core` alongside the front end, before the database | Accepted by the product owner | 2026-11-21 |
| 30 | Settings and staff changes are audit-logged with old and new values, never changed silently | Accepted by the product owner | 2026-11-21 |
| 31 | UI text in `.resx` files read through `ILocalizer` (English neutral, Spanish satellite); XAML uses `Loc.Get` function bindings and the page that offers a language switch is rebuilt on change | Accepted | 2026-11-22 |
| 32 | The base and default currency is chosen in the setup wizard (USD unless changed) instead of fixed to USD; any currency can be accepted, found by search. Setup also records whether prices include tax (default not included). Supersedes the USD-only part of 13 and the fixed-USD part of 22 | Accepted by the product owner | 2026-11-22 |

Details for each: [[architecture]], [[data-model]], and the feature docs.
