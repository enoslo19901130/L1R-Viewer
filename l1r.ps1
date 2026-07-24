<#
  l1r — L1R-Viewer 統一命令列前端（Phase 1 交付：launcher 版）

  路由:
    l1r map <verb> ...   → L1R.MapViewer(-cli):地圖/S32 讀取與算圖
    l1r pak|spr|til|dat|xml|version|help ... → L1R.Cli(pakviewer-cli):封存/圖素/文字

  map 別名(方便使用,對應 L1MapViewer 的實際 verb):
    l1r map render      <mapDir> <out.png>   →  export-fullmap   (★目前 headless 算圖有已知 bug,見 plans 進度)
    l1r map passability <mapDir> <out.txt>   →  export-passability
    l1r map portals     <s32>    <out.json>  →  export            (輸出含 layer7 傳送點)
    l1r map list-maps   <client>             →  list-maps
    l1r map info        <s32>                →  info
    其餘 map verb 直接透傳給 -cli(export-tiles / render-adjacent / batch-export ...)

  註:此為 Phase 1 的「launcher 式」統一入口;未來 Phase 3+ 可收斂為單一原生 exe。
#>
[CmdletBinding()]
param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Rest)

# 不要用 Stop:後端 exe 會往 stderr 寫進度/log,PS7 在 Stop 下可能把 native stderr 當成終止錯誤。
$ErrorActionPreference = 'Continue'
$PSNativeCommandUseErrorActionPreference = $false
$root = $PSScriptRoot

function Find-Backend([string]$proj, [string]$exe) {
    $cands = @(
        "$root\src\$proj\bin\Release\net10.0\$exe",
        "$root\src\$proj\bin\Release\net10.0-windows\$exe",
        "$root\src\$proj\bin\Debug\net10.0\$exe",
        "$root\src\$proj\bin\Debug\net10.0-windows\$exe"
    )
    foreach ($c in $cands) { if (Test-Path $c) { return $c } }
    return $null
}

$pakCli = Find-Backend 'L1R.Cli'       'pakviewer-cli.exe'
$mapCli = Find-Backend 'L1R.MapViewer' 'L1MapViewerCore.exe'

function Show-Usage {
    Write-Host 'l1r — L1R-Viewer unified CLI (launcher)'
    Write-Host ''
    Write-Host 'Usage: l1r <group> <command> [args]'
    Write-Host ''
    Write-Host 'Groups:'
    Write-Host '  map    map / S32 read + render  (backend: L1R.MapViewer)'
    Write-Host '  pak    PAK/IDX archive          (backend: L1R.Cli)'
    Write-Host '  spr    SPR sprite'
    Write-Host '  til    TIL tile'
    Write-Host '  dat    DAT (Lineage M)'
    Write-Host '  xml    XML encryption/decryption'
    Write-Host ''
    Write-Host 'map aliases:'
    Write-Host '  l1r map render      <mapDir> <out.png>'
    Write-Host '  l1r map passability <mapDir> <out.txt>'
    Write-Host '  l1r map portals     <s32>    <out.json>'
    Write-Host '  l1r map list-maps   <client>'
    Write-Host '  l1r map info        <s32>'
}

if (-not $Rest -or $Rest.Count -eq 0 -or $Rest[0] -in @('help', '--help', '-h')) {
    Show-Usage
    exit 0
}

$group = $Rest[0].ToLowerInvariant()
$args2 = if ($Rest.Count -gt 1) { $Rest[1..($Rest.Count - 1)] } else { @() }

# L1R.MapViewer 是 WinExe(GUI 子系統):用 `&` 直接呼叫時 PowerShell 不會等它結束、
# 也不保證 stdout 接回 console。改用 Start-Process -Wait + 重導向,確保等待完成並取得輸出。
function Invoke-MapBackend {
    param([string[]]$BackendArgs)
    $tmpOut = [System.IO.Path]::GetTempFileName()
    $tmpErr = [System.IO.Path]::GetTempFileName()
    try {
        $full = @('-cli') + $BackendArgs
        $proc = Start-Process -FilePath $mapCli -ArgumentList $full -Wait -NoNewWindow -PassThru `
            -RedirectStandardOutput $tmpOut -RedirectStandardError $tmpErr
        # 後端(NLog/DebugLog)常把資訊訊息寫到 stderr;JSON/檔案類指令是寫到「輸出檔」而非 stdout,
        # 故這裡把 stdout 與 stderr 都轉到 stdout,方便 info/list-maps 這類純訊息指令也看得到。
        Get-Content -LiteralPath $tmpOut
        Get-Content -LiteralPath $tmpErr
        return $proc.ExitCode
    }
    finally {
        Remove-Item -LiteralPath $tmpOut, $tmpErr -Force -ErrorAction SilentlyContinue
    }
}

if ($group -eq 'map') {
    if (-not $mapCli) { Write-Error 'L1R.MapViewer backend not built. Run: dotnet build src\L1R.MapViewer'; exit 2 }
    if ($args2.Count -eq 0) { exit (Invoke-MapBackend @('help')) }
    $verb = $args2[0].ToLowerInvariant()
    $vargs = if ($args2.Count -gt 1) { $args2[1..($args2.Count - 1)] } else { @() }
    # alias translation → L1MapViewer 實際 verb
    switch ($verb) {
        'render'      { exit (Invoke-MapBackend (@('export-fullmap')     + $vargs)) }
        'passability' { exit (Invoke-MapBackend (@('export-passability') + $vargs)) }
        'portals'     { exit (Invoke-MapBackend (@('export')             + $vargs)) }
        default       { exit (Invoke-MapBackend (@($verb)                + $vargs)) }
    }
}
else {
    if (-not $pakCli) { Write-Error 'L1R.Cli backend not built. Run: dotnet build src\L1R.Cli'; exit 2 }
    & $pakCli @Rest
    exit $LASTEXITCODE
}
