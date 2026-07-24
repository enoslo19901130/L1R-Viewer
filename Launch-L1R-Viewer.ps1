<#
.SYNOPSIS
  Single entry launcher for L1R-Viewer GUIs and CLI help.
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('map', 'pak', 'cli', 'help')]
    [string]$Mode = 'help',

    [string]$Client,

    [switch]$EnableEdit,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Rest
)

$ErrorActionPreference = 'Continue'
$root = $PSScriptRoot

function Find-Exe([string]$proj, [string]$exe) {
    $cands = @(
        "$root\src\$proj\bin\Release\net10.0-windows\$exe",
        "$root\src\$proj\bin\Release\net10.0\$exe",
        "$root\src\$proj\bin\Debug\net10.0-windows\$exe",
        "$root\src\$proj\bin\Debug\net10.0\$exe"
    )
    foreach ($c in $cands) { if (Test-Path $c) { return $c } }
    return $null
}

$map = Find-Exe 'L1R.MapViewer' 'L1MapViewerCore.exe'
$pak = Find-Exe 'L1R.PakBrowser' 'PakViewer.exe'
$cli = Find-Exe 'L1R.Cli' 'pakviewer-cli.exe'

switch ($Mode) {
    'help' {
        Write-Host 'L1R-Viewer launcher'
        Write-Host ''
        Write-Host '  .\Launch-L1R-Viewer.ps1 map [-Client <path>] [-EnableEdit]'
        Write-Host '  .\Launch-L1R-Viewer.ps1 pak [-EnableEdit]'
        Write-Host '  .\Launch-L1R-Viewer.ps1 cli <args...>'
        Write-Host '  .\l1r.ps1 map render <mapDir> <out.png>'
        Write-Host ''
        Write-Host "  MapViewer : $(if ($map) { $map } else { 'NOT BUILT' })"
        Write-Host "  PakBrowser: $(if ($pak) { $pak } else { 'NOT BUILT' })"
        Write-Host "  CLI       : $(if ($cli) { $cli } else { 'NOT BUILT' })"
        exit 0
    }
    'map' {
        if (-not $map) { Write-Error 'MapViewer not built. Run: dotnet build L1R-Viewer.slnx -c Release'; exit 2 }
        $argsList = @()
        if ($EnableEdit) { $argsList += '--enable-edit' }
        if ($Client) { $argsList += $Client }
        Start-Process -FilePath $map -ArgumentList $argsList
    }
    'pak' {
        if (-not $pak) { Write-Error 'PakBrowser not built. Run: dotnet build L1R-Viewer.slnx -c Release'; exit 2 }
        $argsList = @()
        if ($EnableEdit) { $argsList += '--enable-edit' }
        Start-Process -FilePath $pak -ArgumentList $argsList
    }
    'cli' {
        if (-not $cli) { Write-Error 'CLI not built.'; exit 2 }
        & $cli @Rest
        exit $LASTEXITCODE
    }
}
