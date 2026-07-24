<#
.SYNOPSIS
  Single entry launcher for L1R-Viewer (defaults to Shell GUI).
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('shell', 'map', 'pak', 'cli', 'help')]
    [string]$Mode = 'shell',

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

$shell = Find-Exe 'L1R.Shell' 'L1R-Viewer.exe'
$map = Find-Exe 'L1R.MapViewer' 'L1MapViewerCore.exe'
$pak = Find-Exe 'L1R.PakBrowser' 'PakViewer.exe'
$cli = Find-Exe 'L1R.Cli' 'pakviewer-cli.exe'

switch ($Mode) {
    'help' {
        Write-Host 'L1R-Viewer launcher'
        Write-Host ''
        Write-Host '  .\Launch-L1R-Viewer.ps1              # open Shell (default)'
        Write-Host '  .\Launch-L1R-Viewer.ps1 shell [-Client <path>] [-EnableEdit]'
        Write-Host '  .\Launch-L1R-Viewer.ps1 map [-Client <path>] [-EnableEdit]'
        Write-Host '  .\Launch-L1R-Viewer.ps1 pak [-Client <path>] [-EnableEdit]'
        Write-Host '  .\Launch-L1R-Viewer.ps1 cli <args...>'
        Write-Host '  .\l1r.ps1 doctor <client>'
        Write-Host ''
        Write-Host "  Shell     : $(if ($shell) { $shell } else { 'NOT BUILT' })"
        Write-Host "  MapViewer : $(if ($map) { $map } else { 'NOT BUILT' })"
        Write-Host "  PakBrowser: $(if ($pak) { $pak } else { 'NOT BUILT' })"
        Write-Host "  CLI       : $(if ($cli) { $cli } else { 'NOT BUILT' })"
        exit 0
    }
    'shell' {
        if (-not $shell) { Write-Error 'Shell not built. Run: dotnet build L1R-Viewer.slnx -c Release'; exit 2 }
        $argsList = @()
        if ($EnableEdit) { $argsList += '--enable-edit' }
        if ($Client) { $argsList += $Client }
        Start-Process -FilePath $shell -ArgumentList $argsList
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
        if ($Client) { $argsList += $Client }
        Start-Process -FilePath $pak -ArgumentList $argsList
    }
    'cli' {
        if (-not $cli) { Write-Error 'CLI not built.'; exit 2 }
        & $cli @Rest
        exit $LASTEXITCODE
    }
}
