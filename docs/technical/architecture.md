# Architecture

Local-first Windows desktop app: WinUI 3, .NET 10, C#, MVVM (CommunityToolkit.Mvvm), dependency injection, packaged as MSIX. No server; no network dependency for any core workflow. Decisions: [[decisions]].

## Projects

```text
estoq.App            WinUI 3 views, view models, navigation, theme, pop-ups, composition root  (Windows only)  BUILT
estoq.Application    use cases, service interfaces, Result type, permission checks       -> Core          PLANNED
estoq.Infrastructure SQLite data access, migrations, encryption, paths, logging, backup  -> Core, Application PLANNED
estoq.Core           entities, enums, pure business rules (stock, money, permissions)    -> nothing       PLANNED
tests/*              xUnit per layer; Infrastructure tests use a real SQLite file                         PLANNED
```

Only `estoq.App` is in `estoq.sln` today; the other folders hold build leftovers only.

Dependency rule: references point inward. Core knows nothing about the database, WinUI or the file system. Views never query data; view models call service interfaces.

## Front end first

During the front-end phase ([[roadmap]]) each view model depends on an interface (for example `IInventoryService`, `ISalesService`). The first implementation lives in the App project and returns sample data. The back end later provides real implementations in Application and Infrastructure and the composition root switches registrations. Screens do not change.

## Runtime

1. `App` builds the service provider and shows the main window at once.
2. Back end phase: the database initialiser creates `{Data,Backups,Logs,ProductImages}` under `%ProgramData%\estoq`, applies migrations and seeds roles and settings (idempotent).
3. No owner yet: [[setup-wizard]]. Otherwise: [[sign-in]], then the shell ([[app-shell]]).
4. Startup failure: logged, plain-language message, no stack trace.

## Cross-cutting rules

- **Stock**: only the stock movement service writes quantities; movements are append-only ([[data-model]]).
- **Audit**: sensitive actions, settings changes and staff changes write an append-only audit log entry with old and new values ([[roles-and-permissions]]).
- **Core rules first**: business rules live in `estoq.Core` as pure, unit-tested code and are built alongside the front end; the sample-data services call them, so screens show real calculations before the database exists.
- **Atomic operations**: sale, return, void, adjustment and product save each run in one transaction.
- **Permissions and approval**: checked in application services; approval records the approver ([[roles-and-permissions]]).
- **Money**: `decimal` in code, stored as integers scaled by 10,000; multi-currency with rate snapshots.
- **Time**: stored UTC, shown local.
- **Security**: whole database, local backups and images encrypted at rest (SQLite3 Multiple Ciphers, MIT licence, loaded through SQLitePCLRaw; see [[encryption]]). Key generated at setup, protected with Windows DPAPI at machine scope so every Windows account on the PC opens the same database; the recovery key also unlocks it. Keys never logged. No cardholder data stored.
- **Localisation**: all user-facing text in resource files; English and Spanish both ship in the MVP, English is the fallback. Allow about 30 % longer Spanish text in layouts.
- **Offline**: [[backup]] is the only network feature; it defers when offline.
- **Logging**: Microsoft.Extensions.Logging with a daily rolling file in `%ProgramData%\estoq\Logs`, 14 days kept. Never log passwords, codes, keys or tokens.
- **Errors**: expected business failures return `Result`/error values, not exceptions.

## Testing

- Core: unit tests for stock, money and permission rules.
- Infrastructure: integration tests on a real migrated SQLite file (constraints, append-only ledger, rollback, model matches migrations).
- App: built by the Windows CI job; UI acceptance tests before release.
- Performance target: 10,000 variants and 100,000 sales on modest hardware.
