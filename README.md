# L1R-Viewer

Lineage Remastered **read-first** client asset toolkit: map render, sprite/tile/pak browse, CLI + MCP.

Repository: https://github.com/enoslo19901130/L1R-Viewer

## Layout

```
src/
  Lin.Helper.Core/   # shared decoder engine (namespace unchanged)
  L1R.Cli/           # pakviewer-cli — pak/spr/til/dat/xml
  L1R.MapViewer/     # map GUI + -cli (render/portals/passability/…)
  L1R.PakBrowser/    # asset browser GUI (Eto)
mcp/
  server.py          # FastMCP server name: l1r-viewer (read-only tools)
docs/                # CLI / MCP / decoder parity notes
l1r.ps1              # unified launcher
```

## Build

```powershell
dotnet build L1R-Viewer.slnx -c Release
```

Requires .NET SDK 10.x on Windows.

## CLI (launcher)

```powershell
.\l1r.ps1 help
.\l1r.ps1 map render  <client>\map\53  .\tests\out\map-53.png
.\l1r.ps1 map portals <client>\map\53\7fff7ffe.s32 .\tests\out\p.json
.\l1r.ps1 map passability <client>\map\53 .\tests\out\pass.txt
.\src\L1R.Cli\bin\Release\net10.0\pakviewer-cli.exe spr info <client> 167 --json
```

Write commands require `--enable-edit` (map CLI + `pak add/delete/create`).

## MCP

```powershell
python .\mcp\smoke_test.py --map-id 53 --id 167
```

Register via `.mcp.json` (`l1r-viewer`). Tools are **read-only** (no fix/import/clear).

## GUI

```powershell
# Map viewer (read-only by default)
.\src\L1R.MapViewer\bin\Release\net10.0-windows\L1MapViewerCore.exe <client>
# Edit mode:
.\src\L1R.MapViewer\bin\Release\net10.0-windows\L1MapViewerCore.exe --enable-edit <client>

# Pak browser
.\src\L1R.PakBrowser\bin\Release\net10.0-windows\PakViewer.exe
```

## Docs

- **`docs/plans/L1R-Viewer-Goal-執行計畫-操作者友善-v2.md`** — **現行 Goal 規格**（操作者友善、GUI 整併、Phase 7–13）
- `docs/plans/L1R-Viewer-執行進度.md` — live 進度
- `docs/cli.md` / `docs/mcp.md` / `docs/HEADLESS.md` — 進階與 Agent
- `docs/decoder-parity-report.md` — 解碼對照

## Rules

- Offline static assets only; no client cracking / live traffic.
- Do not rename `Lin.Helper.Core` namespaces.
- Read-first; write paths opt-in via `--enable-edit`.
