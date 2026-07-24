# L1R-Viewer 執行進度（Live Log）

> 產出 repo: https://github.com/enoslo19901130/L1R-Viewer  
> 更新: 2026-07-25

## 現行權威計畫

| 計畫 | 路徑 |
|---|---|
| **v2 Goal** | `docs/plans/L1R-Viewer-Goal-執行計畫-操作者友善-v2.md` |
| v1 | `docs/plans/L1R-Viewer-整併執行計畫.md` |

## 狀態總表

### v1（0–6）✅ 完成

### v2

| Phase | 說明 | 狀態 |
|---|---|---|
| **7** | 設定、路徑驗證、doctor、GETTING-STARTED、MCP validate_client | ✅ |
| 8 | Shell 入口 | ⬜ |
| 9–13 | GUI UX / 對齊 / 品牌 / 品質 | ⬜ |

## Phase 7 完成摘要

- `src/L1R.Shared`: `ClientPathValidator`, `AppSettings`, `OperatorMessage`
- CLI: `pakviewer-cli doctor <client> [--json] [--remember]`
- `l1r.ps1 doctor` 別名
- MCP: `validate_client`（唯讀）
- `docs/GETTING-STARTED.md`（zh-TW）
- 單元測試: `tests/L1R.Shared.Tests`（4 passed）
- Gate: 錯誤路徑 exit=2 + 錯誤/原因/建議；真實 client `ok=true` map_count=829

## 驗證指令

```powershell
dotnet build L1R-Viewer.slnx -c Release
dotnet test tests\L1R.Shared.Tests -c Release
.\l1r.ps1 doctor C:\Windows
.\l1r.ps1 doctor $env:L1R_CLIENT --json
python .\mcp\smoke_test.py --map-id 53 --id 167
```
