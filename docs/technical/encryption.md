# Encryption at rest

Decision 12 in [[decisions]]. Implemented in the back-end phase ([[roadmap]]), before any real data is written: encrypting an existing database later needs a migration.

## What is encrypted

- The SQLite database.
- Local backups and Google Drive backups ([[backup]]).
- The product images folder.

## Library

**SQLite3 Multiple Ciphers**, loaded through the NuGet package `SQLitePCLRaw.bundle_e_sqlite3mc`.

| Part | Licence |
|---|---|
| SQLite | Public domain |
| SQLite3 Multiple Ciphers | MIT |
| SQLitePCLRaw | Apache 2.0 |

All are compatible with estoq being open source; no fees or licence keys. SQLCipher was dropped because its maintained .NET packages are commercial.

Why this package: same maintainer and version line as the SQLitePCLRaw packages EF Core already uses, so the fewest version conflicts. Fallback with the same API: `SQLite3MC.PCLRaw.bundle` (by the cipher's author, newer cipher releases sooner).

Default cipher: the library default (ChaCha20-Poly1305). SQLCipher-format files can also be read and written if ever needed.

## Setup

1. Infrastructure references `Microsoft.EntityFrameworkCore.Sqlite.Core` (not `Microsoft.EntityFrameworkCore.Sqlite`, which brings unencrypted SQLite) and `SQLitePCLRaw.bundle_e_sqlite3mc`. Versions pinned in `Directory.Packages.props`.
2. Call `SQLitePCL.Batteries_V2.Init()` once at startup, before the first connection.
3. Open connections with `Data Source=<path>;Password=<key>` (built with `SqliteConnectionStringBuilder`, never string concatenation). The existing connection pragmas (foreign keys, WAL, busy timeout) still apply.
4. Migrations, queries and the online backup API work unchanged on the encrypted file.

## Keys

- A random 256-bit database key is generated during the [[setup-wizard]].
- Stored on disk only protected by Windows DPAPI at machine scope (`DataProtectionScope.LocalMachine`), so every Windows account on the PC opens the same database ([[decisions]] 4). The key file lives in `%ProgramData%\estoq` with restricted file permissions.
- The recovery key (shown once at setup) can also unlock the database; it is the master backup of the key.
- Cloud backups use a key derived from the recovery key, so a backup restores on a new computer.
- Losing both the recovery key and the Windows installation makes data unrecoverable. The wizard says so plainly.

## Open source rules

- Security comes from the key, not from hiding the method; the code is public by design.
- Never commit a real key, a sample key, a test database or a recovery key. Tests generate throwaway keys at run time.
- Never log keys, passwords or connection strings with a password.

## Spike before the database work (R3)

Prove on a throwaway branch, then record the result here:

- [ ] Package versions resolve with EF Core 10 without conflicts.
- [ ] Migrations create and update an encrypted database.
- [ ] Opening the file without the key fails; with the key succeeds.
- [ ] Online backup API produces an encrypted copy that opens with the key.
- [ ] Speed on modest hardware with 10,000 variants and 100,000 sales is acceptable.
