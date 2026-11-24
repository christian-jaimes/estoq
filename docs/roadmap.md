# Roadmap

The single place for what is planned and what is built. Replaces the former `Development plan.md`. Tick an item when its work is merged and its doc is updated. Mockups in `docs/mockups/` are visual references only; behaviour is in the linked docs.

## Order

1. **Front end, with core rules alongside.** Every screen built on sample data, in the checklist order below. View models depend on service interfaces from the start; the first implementation of each returns sample data, so the back end can replace it without changing screens. Business rules are built in parallel in `estoq.Core` (pure code, unit-tested, no database) and the sample-data services use them, so screens show real numbers from day one.
2. **Back end.** Encryption spike first, then the SQLite data layer (encrypted from the first migration), application services and permission checks. Screens switch from sample data to real services one by one. See [[architecture]] and [[data-model]].
3. **Extra features.** Scheduled Google Drive [[backup]]. Later: [[label-printing]] once the printer is chosen.

## Checklist

### Front end

- [x] App shell: sidebar, top bar, F1 to F4 pop-up frame, theme toggle, placeholder pages ([[app-shell]], [[ui-implementation]])
- [x] Splash screen, 2 seconds ([[app-shell]])
- [x] Setup wizard ([[setup-wizard]]); mockup `0 - Elegant Dark Store Setup Wizard.png`
- [x] Sign-in screen, log out (button, F12) and inactivity log out ([[sign-in]]); mockups `1 - Premium Dark Gold Login Screen.png`, `1.1 - estoQ Dark Login Screen.png`
- [ ] Pop-ups
    - [x] Point of sale, F1 ([[point-of-sale]]); mockup `3 - Dark Mode Point of Sale Interface.png`
    - [ ] Add product, F2 ([[add-product]]); mockup `2 - Dark Gold-Accented Product Form.png`
    - [ ] Stock adjustments, F3 ([[stock-adjustments]]); mockup `4 - Stock Adjustment Modal Dashboard.png`
    - [ ] Returns, F4, and voids ([[returns-and-voids]]); mockup `5 - Dark Return Process Modal.png`
- [ ] Pages
    - [ ] Inventory ([[inventory]]); mockup `6 - inventory mock up.png`
    - [ ] Sales ([[sales]]); mockup `7 - sales mockup.png`
    - [ ] Dashboard ([[dashboard]]); mockup `8 - Estoq Analytics Dashboard Overview.png`
- [x] Notifications panel ([[app-shell]]), mock data
- [ ] Dark and light theme, all screens ([[app-shell]])
- [ ] Settings pop-up ([[settings]])
    - [ ] Account; mockup `9 - Dark Estoq Account Settings Modal.png`
    - [ ] Store; mockup `10 - Dark EstoQ Store Settings Modal.png`
    - [ ] Staff; mockup `11 - Dark Staff Management Settings Modal.png`
    - [ ] Exchange rates
    - [ ] Defaults and rules: owner-configurable inactivity timeout (setting `ISecuritySettings.InactivityTimeout`, default 3 minutes; [[sign-in]])
    - [ ] Configurable POS discount preset percentages (initial defaults: 5, 10, 15, 20, 25 and 30; [[point-of-sale]])
- [ ] English and Spanish resource files for every screen (done for the setup wizard; mechanism in [[ui-implementation]])

### Core rules (alongside the front end)

Build each rule before or with the first screen that needs it.

- [ ] Money and currency: conversion with rate snapshots and rounding done (`Money`, `ExchangeRates`); scaled integer storage comes with the data layer ([[decisions]] 3, 13)
- [x] Cart totals: discounts, discount cap and approval threshold, tax, split payments, change ([[point-of-sale]])
- [ ] Refund and exchange limits ([[returns-and-voids]])
- [ ] Stock rules: sale-side check and units sold without stock done (`StockCheck`); movement types and signs with [[stock-adjustments]]
- [ ] Product rules: generated name, duplicate check, SKU and internal barcode generation ([[add-product]])
- [x] Roles and permissions matrix ([[roles-and-permissions]])
- [x] Sign-in rules: code input, lockout, inactivity policy ([[sign-in]])
- [x] Setup rules: owner email, password, store fields, six-digit user code, currency codes and accepted list, tax default 0 % and tax included or not ([[setup-wizard]])
- [ ] Audit events: what each action and settings change records ([[data-model]])

### Back end

- [ ] Encryption spike ([[encryption]])
- [ ] SQLite data layer and migrations, encrypted from the start ([[data-model]], [[encryption]])
- [ ] Application services, permissions and approval ([[roles-and-permissions]])
- [ ] Audit log for sensitive actions, settings and staff changes ([[data-model]])

### Extra features

- [ ] Scheduled Google Drive backup and restore ([[backup]])

## Status notes

| Area | Status |
|---|---|
| WinUI shell | Built; first run on Windows still to verify |
| Setup wizard | Built on sample data; builds clean, UI not yet run on Windows |
| Sign-in | Built on sample accounts; app builds and launches, layout not yet checked by eye |
| Point of sale | Built on sample data; app builds clean, UI not yet run on Windows |
| Core rules | `estoq.Core` started: roles and permissions, setup rules, sign-in rules, sale rules; `tests/estoq.Core.Tests` |
| Back end projects (Application, Infrastructure) | Not started; folders hold only build leftovers |
| Documentation | Consolidated 2026-11-21; open items in [[open-questions]] |

## Beyond the MVP

[[label-printing]], purchasing and supplier orders, Transactions page, dashboard tabs beyond the overview, customer data and email receipts, full onboarding, PIN sign-in, SSO, OneDrive or Dropbox backup, sync and cloud features, business types other than clothing.
