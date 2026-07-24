<#
.SYNOPSIS
  Create Desktop + Start Menu shortcuts to L1R-Viewer Shell.
#>
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$exe = @(
    "$root\src\L1R.Shell\bin\Release\net10.0-windows\L1R-Viewer.exe",
    "$root\src\L1R.Shell\bin\Debug\net10.0-windows\L1R-Viewer.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $exe) {
    Write-Error 'L1R-Viewer.exe not found. Run: dotnet build L1R-Viewer.slnx -c Release'
    exit 2
}

function New-Shortcut([string]$linkPath, [string]$target) {
    $w = New-Object -ComObject WScript.Shell
    $s = $w.CreateShortcut($linkPath)
    $s.TargetPath = $target
    $s.WorkingDirectory = Split-Path $target
    $s.Description = 'L1R-Viewer — Lineage Remastered offline asset toolkit'
    $s.Save()
    Write-Host "Created: $linkPath"
}

$desktop = [Environment]::GetFolderPath('Desktop')
$start = Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs'
New-Item -ItemType Directory -Force -Path $start | Out-Null
New-Shortcut (Join-Path $desktop 'L1R-Viewer.lnk') $exe
New-Shortcut (Join-Path $start 'L1R-Viewer.lnk') $exe
Write-Host 'Done.'
