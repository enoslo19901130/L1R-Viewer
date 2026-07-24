# L1R-Viewer 五分鐘上手

本文件給**操作者**（不需熟悉命令列的人也可照做）。進階請看 `docs/OPERATOR-MANUAL.md`（後續）與 `docs/mcp.md`。

## 你需要什麼

- Windows 10/11
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- 一份**離線**天堂 R 客戶端根目錄（底下要有 `map\` 與 `Tile.idx`）

建議設定環境變數（可選）：

```powershell
$env:L1R_CLIENT = "D:\path\to\LineageRemastered-客戶端根目錄"
```

## 步驟 1：建置

```powershell
cd <L1R-Viewer 倉庫根目錄>
dotnet build L1R-Viewer.slnx -c Release
```

成功條件：`0 個錯誤`。

## 步驟 2：選擇客戶端並健康檢查（doctor）

把「客戶端根目錄」想成工作區：裡面同時有 `map\` 與 `Tile.idx`（不是只選 `map` 子資料夾）。

```powershell
# 失敗範例（資料夾不對）：會印 錯誤/原因/建議，exit ≠ 0
.\l1r.ps1 doctor C:\Windows

# 成功範例
.\l1r.ps1 doctor $env:L1R_CLIENT
# 或
.\src\L1R.Cli\bin\Release\net10.0\pakviewer-cli.exe doctor "<client>" --json
```

通過時可記住路徑到 `%AppData%\L1R-Viewer\settings.json`：

```powershell
.\l1r.ps1 doctor $env:L1R_CLIENT --remember
```

Agent 可用 MCP 工具 `validate_client(client_path)`，結果應與 doctor 一致。

## 步驟 3：匯出一張地圖 PNG

```powershell
$client = $env:L1R_CLIENT
New-Item -ItemType Directory -Force -Path .\tests\out | Out-Null
.\l1r.ps1 map render "$client\map\53" .\tests\out\map-53.png
# 大地圖建議限制邊長：
# 直接：
.\src\L1R.MapViewer\bin\Release\net10.0-windows\L1MapViewerCore.exe -cli export-fullmap "$client\map\53" .\tests\out\map-53.png --max-size 2048
```

成功條件：`tests\out\` 出現非空 PNG。

## 步驟 4（可選）：Agent / MCP

```powershell
python -m pip install -r mcp\requirements.txt
python .\mcp\smoke_test.py --client $env:L1R_CLIENT --map-id 53 --id 167
```

## 常見問題

| 現象 | 怎麼辦 |
|---|---|
| 錯誤提到缺少 `map` | 你選到的不是客戶端**根**目錄；往上一層找同時有 map 與 Tile.idx 的資料夾 |
| 錯誤提到缺少 `*.idx` / Tile.idx | 不完整的客戶端；需要 Tile.idx（地圖渲染） |
| PowerShell 顯示亂碼 | 終端改 UTF-8：`chcp 65001`；訊息本體仍含「錯誤/原因/建議」關鍵字 |
| 地圖很大很慢 | 使用 `--max-size 1024` 或 `2048` |

## 下一步

- 地圖 / 資產 GUI：`.\Launch-L1R-Viewer.ps1 help`（Phase 8 後會有主畫面）
- 完整 Goal：`docs/plans/L1R-Viewer-Goal-執行計畫-操作者友善-v2.md`
