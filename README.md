# L1R-Viewer

Lineage Remastered **read-first** client asset toolkit: map / sprite / pak · CLI · MCP · GUI Shell.

Repository: https://github.com/enoslo19901130/L1R-Viewer

---

## 進度速查（複查用）

| Phase | 內容 | 狀態 |
|---|---|---|
| v1 · 0–2 | monorepo / CLI / MCP 唯讀 | ✅ |
| v1 · 3 | 解碼 parity 記錄 | 🟡 |
| v1 · 4–6 | 唯讀 gate、文件、CI | ✅ |
| v2 · 7 | doctor / settings / GETTING-STARTED | ✅ |
| v2 · 8 | Shell 主畫面 `L1R-Viewer.exe` | ✅ |
| v2 · 9 | 地圖 GUI 一鍵匯出 + 資訊分頁 | ✅ |
| v2 · 10 | 資產 GUI ID 搜尋 + 預設匯出 | ✅ |
| v2 · 11 | 三入口對齊 / `map regions` | ✅ |
| v2 · 12 | 產品 exe 名 / CHANGELOG | ✅ |
| v2 · 13 | regression.ps1 / CI doctor | ✅ |

規格：`docs/plans/L1R-Viewer-Goal-執行計畫-操作者友善-v2.md` · 手冊：`docs/OPERATOR-MANUAL.md`

---

## 從這裡開始

```powershell
dotnet build L1R-Viewer.slnx -c Release
.\Launch-L1R-Viewer.ps1                 # Shell 主畫面
.\l1r.ps1 doctor "<client-root>"
.\tests\regression.ps1                  # CLI 回歸（可設 L1R_CLIENT）
```

詳見 `docs/GETTING-STARTED.md`。捷徑：`.\Install-Shortcuts.ps1`

## 產出檔名

| 元件 | exe |
|---|---|
| Shell | `L1R-Viewer.exe` |
| MapViewer | `L1R-MapViewer.exe` |
| PakBrowser | `L1R-PakBrowser.exe` |
| CLI | `pakviewer-cli.exe`（`l1r.ps1` 包裝） |

## CLI / MCP

```powershell
.\l1r.ps1 map render  <client>\map\53  .\tests\out\m.png
.\l1r.ps1 map portals <s32> .\tests\out\p.json
pakviewer-cli map regions <client>\map\53 --json
python .\mcp\smoke_test.py --map-id 53 --id 167
```

MCP **唯讀**（含 `validate_client`）。寫入需 `--enable-edit`，不上 MCP。

## Layout

```
src/Lin.Helper.Core | L1R.Shared | L1R.Shell | L1R.Cli | L1R.MapViewer | L1R.PakBrowser
mcp/  docs/  l1r.ps1  Launch-L1R-Viewer.ps1  tests/
```

## Rules

- Offline static assets only  
- Do not rename `Lin.Helper.Core` namespaces  
- Read-first; write opt-in only  
