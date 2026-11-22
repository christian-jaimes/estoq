# Builds estoq.App (Release, x64), refreshes the packaged layout and launches it.
# The app runs from the AppX folder: it holds Assets (fonts, icons, logos) and is the layout registered with Windows.
# `dotnet build` writes the new binaries one level up, so they are copied into AppX; without that step the app runs
# stale code or loses its icons and logos.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Get-Process estoq -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet build "$root\src\estoq.App\estoq.App.csproj" -p:Platform=x64 -c Release -v:q
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$out = Get-ChildItem "$root\src\estoq.App\bin\x64\Release" -Directory | Select-Object -First 1
$layout = Join-Path $out.FullName 'AppX'
if (-not (Test-Path "$layout\AppxManifest.xml")) { throw "No AppX layout at $layout. Run the package launch once from Visual Studio." }
robocopy $out.FullName $layout /E /XD AppX /XF AppxManifest.xml /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw 'Copying into the AppX layout failed.' }

$package = Get-AppxPackage Estoq.Desktop
if ($package.InstallLocation -ne $layout) {
    if ($package) { Remove-AppxPackage $package }
    Add-AppxPackage -Register "$layout\AppxManifest.xml"
    $package = Get-AppxPackage Estoq.Desktop
}
Start-Process "shell:AppsFolder\$($package.PackageFamilyName)!App"
