# L1R-Viewer 執行進度（Live Log）

> 產出 repo: https://github.com/enoslo19901130/L1R-Viewer  
> 本機: `006-Tools\L1R-Viewer`  
> 更新: 2026-07-25

## 狀態總表

| Phase | 說明 | 狀態 |
|---|---|---|
| 0 | Solution 骨架 + 引擎統一 | ✅ |
| 1 | 統一 CLI `l1r` + headless render | ✅ |
| 2 | Python MCP 接上新 CLI | ✅ smoke PASS |
| 3 | 解碼一致性驗證 | 🟡 已記錄差異（非 diff=0，不阻塞） |
| 4 | 地圖 GUI 唯讀 gate | ✅ 工具列/存檔/EnsureWritable 加深 |
| 5 | PakBrowser 唯讀 gate | ✅ 刪除/寫回需 `--enable-edit` |
| 6 | 改名/品牌/文件/launcher | ✅ README/docs/Launch script/產品標題 |

## GitHub

- Remote: `https://github.com/enoslo19901130/L1R-Viewer.git`
- Branch: `main`（每階段 commit + push）
- Owner 已授權同步上傳

## 驗證指令

```powershell
dotnet build L1R-Viewer.slnx -c Release
.\l1r.ps1 map render "<client>\map\53" .\tests\out\map-53.png
python .\mcp\smoke_test.py --map-id 53 --id 167
.\Launch-L1R-Viewer.ps1 help
```

## 續作建議（補充項）

1. CI: GitHub Actions `dotnet build` + Python syntax check
2. `spr export --scale classic` 對齊舊 headless 尺寸後再 parity
3. 更多 MapViewer 編輯對話框入口掃描
4. 可選：exe 改名為 `l1r-mapviewer.exe` / `l1r-pakbrowser.exe`（需同步 launcher）
