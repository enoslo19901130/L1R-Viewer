# L1R-Viewer

Lineage Remastered **read-first** client asset toolkit: map render, sprite/tile/pak browse, CLI + MCP.

Repository: https://github.com/enoslo19901130/L1R-Viewer

---

## 進度速查（複查用）

> 詳細規格：`docs/plans/L1R-Viewer-Goal-執行計畫-操作者友善-v2.md`  
> Live log：`docs/plans/L1R-Viewer-執行進度.md`

| Phase | 內容 | 狀態 |
|---|---|---|
| **v1 · 0–2** | 引擎 monorepo、CLI `l1r`、MCP 唯讀 | ✅ |
| **v1 · 3** | 解碼 parity（尺寸差異已記錄） | 🟡 |
| **v1 · 4–6** | 唯讀 gate、文件、CI、品牌 | ✅ |
| **v2 · 7** | doctor / settings / GETTING-STARTED / `validate_client` | ✅ |
| **v2 · 8** | Shell 單一主畫面入口 | ✅ |
| **v2 · 9** | 地圖 GUI 一鍵匯出 / 側欄 | ⬜ |
| **v2 · 10** | 資產 GUI 智慧搜尋 | ⬜ |
| **v2 · 11** | GUI⇄CLI⇄MCP 對齊 | ⬜ |
| **v2 · 12** | exe 改名 / 可攜發布 | ⬜ |
| **v2 · 13** | 回歸與品質 | ⬜ |

**目前可用：** CLI + MCP 讀地圖/精靈；`doctor` 檢查 client；**Shell 主畫面**開地圖/資產。  
**尚未：** 地圖/資產 GUI 深度 UX（側欄傳點、一鍵匯出等）。

---

## Layout

```
src/
  Lin.Helper.Core/   # shared decoder engine (namespace unchanged)
  L1R.Shared/        # client validation + AppData settings
  L1R.Shell/         # operator main window (L1R-Viewer.exe)
  L1R.Cli/           # pakviewer-cli — pak/spr/til/dat/xml/doctor
  L1R.MapViewer/     # map GUI + -cli
  L1R.PakBrowser/    # asset browser GUI
mcp/
  server.py          # FastMCP l1r-viewer (read-only)
docs/
l1r.ps1
Launch-L1R-Viewer.ps1
```

## 從這裡開始（操作者）

1. 建置  
2. 開 Shell（主畫面）  
3. 選 client → 健康檢查 → 開地圖 / 資產  

```powershell
dotnet build L1R-Viewer.slnx -c Release
.\Launch-L1R-Viewer.ps1              # 預設開 Shell
# 或直接：
.\src\L1R.Shell\bin\Release\net10.0-windows\L1R-Viewer.exe

.\l1r.ps1 doctor "<client-root>"
.\l1r.ps1 doctor "<client-root>" --remember
```

可選捷徑：`.\Install-Shortcuts.ps1`  
完整步驟見 **`docs/GETTING-STARTED.md`**。

## Build

```powershell
dotnet build L1R-Viewer.slnx -c Release
dotnet test tests\L1R.Shared.Tests -c Release
```

Requires .NET SDK 10.x on Windows.

## CLI (launcher)

```powershell
.\l1r.ps1 help
.\l1r.ps1 doctor <client>
.\l1r.ps1 map render  <client>\map\53  .\tests\out\map-53.png
.\l1r.ps1 map portals <client>\map\53\7fff7ffe.s32 .\tests\out\p.json
.\src\L1R.Cli\bin\Release\net10.0\pakviewer-cli.exe spr info <client> 167 --json
```

Write commands require `--enable-edit`.

## MCP

```powershell
python -m pip install -r mcp\requirements.txt
python .\mcp\smoke_test.py --map-id 53 --id 167
```

Tools are **read-only** (includes `validate_client`).

## GUI

```powershell
.\Launch-L1R-Viewer.ps1 shell          # 主畫面（建議）
.\Launch-L1R-Viewer.ps1 map -Client <client>
.\Launch-L1R-Viewer.ps1 pak -Client <client>
# 編輯模式（危險）：
.\Launch-L1R-Viewer.ps1 shell -EnableEdit
```

## Docs

- `docs/GETTING-STARTED.md` — 五分鐘上手  
- `docs/plans/L1R-Viewer-Goal-執行計畫-操作者友善-v2.md` — Goal 規格  
- `docs/cli.md` / `docs/mcp.md` / `docs/HEADLESS.md`

## Rules

- Offline static assets only; no client cracking / live traffic.  
- Do not rename `Lin.Helper.Core` namespaces.  
- Read-first; write paths opt-in via `--enable-edit` / Shell 設定.  
