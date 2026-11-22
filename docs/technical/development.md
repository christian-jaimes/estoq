# Development

## Toolchain

- .NET SDK 10 (`global.json`, roll forward to latest feature band).
- Windows 10 1809 or later (Windows 11 recommended), Visual Studio 2022 17.14 or later with the **.NET desktop development** and **WinUI application development** workloads and the latest Windows 11 SDK.
- Developer Mode on (Settings > System > For developers).
- Package versions pinned in `Directory.Packages.props` (central package management). No floating or preview versions.

## Build and run

```bash
dotnet build estoq.sln -p:Platform=x64
```

In Visual Studio: open `estoq.sln`, platform **x64**, startup project `estoq.App`, launch profile **estoq (Package)**, F5.

The app targets `net10.0-windows10.0.19041.0`, platforms x64 and ARM64, packaged as MSIX. Warnings are errors outside the App project.

When the back-end projects return: a portable solution filter (`estoq.Portable.slnf`) builds and tests Core, Application and Infrastructure on any OS with `dotnet test`.

## CI

`.github/workflows/ci.yml`: a Windows job builds `src/estoq.App` in Release for x64. A Linux job for the portable projects comes back with the back end.

## Data during development

Back end phase: `%ProgramData%\estoq`. Delete `Data\estoq.db*` to reset.

## Way of working

Branch, commit and PR rules: `CLAUDE.md`. Update the matching doc in `docs/` in the same change.

## Troubleshooting F5 on the packaged app

1. Launch profile reads **estoq (Package)**.
2. Platform is x64 or ARM64, never Any CPU.
3. Build > Configuration Manager: Build and Deploy ticked for `estoq.App` (Debug, x64).
4. Developer Mode on.
5. Clean and rebuild; if that fails, close Visual Studio, delete `bin` and `obj`, reopen. Uninstall an old "estoq" from Settings > Apps if deployment is blocked.
6. Read View > Output (Build, Debug) for the real error.
7. Running `estoq.exe` directly fails with "Class not registered": the app needs package identity, so always launch through the package.
