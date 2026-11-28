# Settings

Visual reference: `9 - Dark Estoq Account Settings Modal.png`, `10 - Dark EstoQ Store Settings Modal.png`, `11 - Dark Staff Management Settings Modal.png`. Pop-up opened from Account settings at the bottom of the sidebar ([[app-shell]]). Left list of sections, right content, footer Cancel and Save changes.

## Sections and access

| Section | Cashier | Manager | Owner | In plan |
|---|---|---|---|---|
| Account | Yes | Yes | Yes | Yes |
| Store | No | Yes | Yes | Yes |
| Staff | No | Yes | Yes | Yes |
| Exchange rates | Yes | Yes | Yes | Yes |
| Defaults and rules | No | View | Yes | Proposed ([[open-questions]] Q12) |
| Data | No | No | Yes | With [[backup]] |

## Change history

Every saved change in Store, Staff, Defaults and rules and Data writes an audit log entry: field, old value, new value, user and time. Exchange rates keep their own append-only history. Each section has a "History" link (owner and manager) listing its changes, newest first. In the front-end phase the history shows sample data.

## Account

Profile photo (change, remove; initials when empty), role (read only), full name (required), email (required for the owner, optional for staff), change password, six-digit user code (masked, with reveal), app language (English, Spanish), theme (light, dark, system), Log out button (also F12).

## Store

Store logo (change, remove), store name (required), registration number / tax ID, address, phone, email, store language (receipts and labels), tax rate (default 0 %, first set in the [[setup-wizard]]).

## Staff

Search by name or email, status filter, "Add staff". Table: avatar, name, email, role badge (Owner, Manager, Cashier), status badge (Active, Inactive), edit, more menu (regenerate code, deactivate or activate). Add or edit: full name, role, optional email and password, generated six-digit code.

Rules: an inactive account cannot sign in; a manager cannot change or deactivate the owner; there is exactly one owner.

## Exchange rates

List of currencies (the base currency chosen in the [[setup-wizard]], USD unless changed, plus the accepted ones) with the current rate to base, last updated and by whom; "Update rate" adds a new rate with an effective time. History per currency. Rates are never overwritten. Open to all roles because border-shop rates change often.

## Defaults and rules (proposed)

Default currency, cashier discount cap (10 %), configurable POS discount presets (initial defaults: 5 %, 10 %, 15 %, 20 %, 25 %, 30 %), inactivity timeout (default 3 minutes, 1 to 60; owner only; see [[sign-in]]), return window (with no limit), receipt behaviour, default low-stock threshold.

## Data (with backup)

Backup schedule and Google Drive connection, back up now, last successful backup with a stale warning, restore, app version and diagnostics. See [[backup]].

## Build status (CPT13, sample data)

Built: the pop-up with the section list (only the sections the role may open), Account, Store, Staff and Exchange rates, plus the History list (owner and manager, Store and Staff). Rules are in `estoq.Core/Settings` with unit tests (`SettingsTests`): section access, audit diff, store validation, staff directory (one owner, manager cannot touch the owner, unique codes), append-only exchange rate book. `ISettingsService` (sample, in memory) writes an audit entry for every saved change.

- Account and Store edits wait for "Save changes"; the app language and theme apply on save. Staff and Exchange rates apply as they are made and are audited at once.
- Store holds the owner-only automatic log out time (1 to 60 minutes, `ISecuritySettings`); it is audited as a Store change.
- New and regenerated staff codes are shown once in a notice; the audit log stores them masked.
- Exchange rates: a new rate starts "now" (no effective time picker yet); a currency has no rate until one is set.
- Not built: Defaults and rules, Data (backup), real photo and logo storage, password storage, effective time picker. The sample staff list is not yet the list sign-in checks; the sample sign-in gains an owner (code 100200) so the owner view can be tested.
- Icons reuse glyphs already in the font subset (no pencil, store or group icon yet); add them with the font instancing steps in [[ui-implementation]] if wanted.

### Low stock threshold (added after CPT13)

Stock status has three values: In stock, Low stock and No stock. The owner sets the low-stock threshold in the Store section (owner only, whole number 1 to 10,000). On hand of 0 or less is No stock; at or below the threshold is Low stock; above it is In stock. Names used: setting key and audit field `LowStockThreshold`, default `5` (`LowStockPolicy.SettingKey` and `LowStockPolicy.DefaultThreshold` in `estoq.Core/Settings/LowStockPolicy.cs`, which also has `Classify(onHand, threshold)` and the `StockLevel` enum). The app reads it from `ISettingsService.LowStockThreshold`. Changes are audited as a Store change. Inventory (CPT10) should call `LowStockPolicy.Classify` with that value once the branches are merged.
