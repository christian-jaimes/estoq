# Backup and restore

Extra feature, after the back end. Owner only, in [[settings]] Data. Losing shop data is the worst failure, so backups run automatically after a one-time setup.

## Setup

Connect Google Drive, granting only the app data folder scope (a hidden folder only estoq can read). Choose daily (default), weekly or monthly.

## How it works

- Scheduled runs plus "Back up now".
- Uses SQLite's online backup API, never a copy of the live file (WAL mode).
- Encrypted before upload with a key derived from the recovery key.
- A local encrypted copy is always kept in `%ProgramData%\estoq\Backups`.
- Offline at the scheduled time: retries when a connection returns.
- Shows the last successful backup; a notification warns when it is older than the schedule implies ([[app-shell]]).

## Restore

Pick a dated backup, the app validates it, the owner confirms that it replaces current data, the app saves a safety copy, restores and restarts. Also used to move to a new computer; needs the recovery key.

Research: [[open-questions]] R4 (Google verification), R5 (offline restore on a new computer). OneDrive and Dropbox are post-MVP.
