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

Default currency, cashier discount cap (10 %), inactivity timeout (default 3 minutes, 1 to 60; owner only; see [[sign-in]]), return window (with no limit), receipt behaviour, default low-stock threshold.

## Data (with backup)

Backup schedule and Google Drive connection, back up now, last successful backup with a stale warning, restore, app version and diagnostics. See [[backup]].
