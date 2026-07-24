<#
  L1R-Viewer CLI regression (read-only).
  Set $env:L1R_CLIENT to real client root, or script creates a minimal fixture for doctor-only paths.
#>
$ErrorActionPreference = 'Continue'
$root = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path "$root\L1R-Viewer.slnx")) { $root = $PSScriptRoot }
Set-Location $root

$failed = 0
function Assert-True($cond, $msg) {
    if (-not $cond) {
        Write-Host "FAIL: $msg" -ForegroundColor Red
        $script:failed++
    } else {
        Write-Host "OK:   $msg" -ForegroundColor Green
    }
}

$cli = @(
    "$root\src\L1R.Cli\bin\Release\net10.0\pakviewer-cli.exe",
    "$root\src\L1R.Cli\bin\Debug\net10.0\pakviewer-cli.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

Assert-True ($null -ne $cli) "CLI exe exists"
if (-not $cli) { exit 2 }

# doctor bad
$bad = Join-Path $env:TEMP ("l1r-reg-bad-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $bad | Out-Null
& $cli doctor $bad 2>$null | Out-Null
Assert-True ($LASTEXITCODE -ne 0) "doctor rejects empty folder (exit=$LASTEXITCODE)"

# doctor good (real or fixture)
$client = $env:L1R_CLIENT
if (-not $client -or -not (Test-Path $client)) {
    $cand = "C:\Users\EnosLo\Desktop\00-Workspace\000-Ongoing-Projects\LineageR-2606262601\001-CLIENT\LineageRemastered-2606262601"
    if (Test-Path $cand) { $client = $cand }
}
if (-not $client -or -not (Test-Path $client)) {
    $client = Join-Path $env:TEMP ("l1r-reg-good-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path "$client\map\1" | Out-Null
    [IO.File]::WriteAllBytes("$client\Tile.idx", [byte[]](0))
    Set-Content "$client\map\1\a.s32" "x"
    Write-Host "NOTE: using fixture client (no real L1R_CLIENT)"
}
$json = & $cli doctor $client --json 2>$null | Out-String
Assert-True ($LASTEXITCODE -eq 0) "doctor accepts client"
Assert-True ($json -match '"ok"\s*:\s*true') "doctor json ok=true"

# map regions if real map folder
$map53 = Join-Path $client "map\53"
if (Test-Path $map53) {
    & $cli map regions $map53 --json 2>$null | Out-Null
    Assert-True ($LASTEXITCODE -eq 0) "map regions on map 53"
}

# shell / mapviewer / pakbrowser present
$shell = Get-ChildItem "$root\src\L1R.Shell\bin\Release" -Recurse -Filter "L1R-Viewer.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
Assert-True ($null -ne $shell) "Shell L1R-Viewer.exe built"

$mapExe = @(
    Get-ChildItem "$root\src\L1R.MapViewer\bin\Release" -Recurse -Filter "L1R-MapViewer.exe" -EA SilentlyContinue
    Get-ChildItem "$root\src\L1R.MapViewer\bin\Release" -Recurse -Filter "L1MapViewerCore.exe" -EA SilentlyContinue
) | Select-Object -First 1
Assert-True ($null -ne $mapExe) "MapViewer exe built"

if ($failed -gt 0) {
    Write-Host "`nREGRESSION FAILED: $failed" -ForegroundColor Red
    exit 1
}
Write-Host "`nREGRESSION PASS" -ForegroundColor Green
exit 0
