# L1R-Viewer 執行進度（Live Log）

> 產出 repo: https://github.com/enoslo19901130/L1R-Viewer  
> 更新: 2026-07-25

## 現行權威計畫

`docs/plans/L1R-Viewer-Goal-執行計畫-操作者友善-v2.md`  
README 亦有**進度速查表**方便複查。

## 狀態總表

| Phase | 說明 | 狀態 |
|---|---|---|
| v1 0–6 | 引擎 / CLI / MCP / 唯讀 / 文件 | ✅ |
| **7** | doctor / settings / GETTING-STARTED / validate_client | ✅ |
| **8** | Shell 主畫面 `L1R-Viewer.exe` | ✅ |
| 9–13 | 地圖/資產 UX、對齊、品牌、品質 | ⬜ |

## Phase 8 摘要

- `src/L1R.Shell` → 輸出 `L1R-Viewer.exe`
- 選 client、doctor、最近清單、開 Map/Pak、輸出資料夾、說明、設定
- 預設標題顯示 **[唯讀]**；`--enable-edit` 或設定解鎖編輯
- MapViewer / PakBrowser 接受 client 路徑參數（同一 client）
- `Launch-L1R-Viewer.ps1` 預設 `shell`；`Install-Shortcuts.ps1`

## 指令

```powershell
dotnet build L1R-Viewer.slnx -c Release
.\Launch-L1R-Viewer.ps1
.\src\L1R.Shell\bin\Release\net10.0-windows\L1R-Viewer.exe
```
