# L1R-Viewer 操作手冊

給**人**用的說明。Agent 請看 `docs/mcp.md` / `docs/HEADLESS.md`。

## 1. 主畫面 Shell

```powershell
.\Launch-L1R-Viewer.ps1
# 或
.\src\L1R.Shell\bin\Release\net10.0-windows\L1R-Viewer.exe
```

| 按鈕 | 作用 |
|---|---|
| 選擇客戶端 | 選含 `map\` + `Tile.idx` 的根目錄 |
| 健康檢查 | 同 CLI `doctor` |
| 開啟地圖 | 啟動 MapViewer（傳入目前 client） |
| 開啟資產 | 啟動 PakBrowser |
| 輸出資料夾 | 開預設匯出目錄 |
| 設定 | max-size、輸出路徑、進階編輯 |

標題 **[唯讀]** 表示不可寫回。進階編輯需設定確認或 `--enable-edit`。

## 2. 地圖 MapViewer

工具列「匯出」列（唯讀可用）：

| 按鈕 | 產出 |
|---|---|
| 地圖 PNG | `文件\L1R-Viewer\exports\map-{id}.png`（自動 max-size） |
| 傳送點 JSON | `map-{id}-portals.json` |
| 通行屬性 | `map-{id}-pass.txt` |
| 輸出資料夾 | 開啟 exports |
| 重新整理資訊 | 更新左側「資訊」分頁 |

左側 **資訊** 分頁：分段數、邊界、Layer7 清單、區域檔數量。

CLI 對照：

```powershell
.\l1r.ps1 map render "$client\map\53" .\tests\out\m.png
.\l1r.ps1 map portals "$client\map\53\xxx.s32" .\tests\out\p.json
```

## 3. 資產 PakBrowser

- 搜尋列：輸入 **數字 ID**（如 `167`）→ 列出 `167-*.spx` 等變體；文字則檔名包含。
- 右鍵 **匯出到預設輸出資料夾** → 與 Shell 相同 exports 目錄。
- 刪除/寫回僅 **編輯模式** 可見。

CLI 對照：

```powershell
pakviewer-cli spr info $client 167 --json
pakviewer-cli spr export $client 167 -o .\tests\out\spr
```

## 4. 三入口對齊

| 能力 | CLI | MCP | GUI |
|---|---|---|---|
| Client 健康 | `doctor` | `validate_client` | Shell 健康檢查 |
| 地圖清單 | `map list-maps` | `list_maps` | MapViewer 下拉 |
| 地圖資訊 | `map info` | `map_info` | 資訊分頁 |
| 渲染 PNG | `map render` | `render_map` | 匯出 PNG |
| 傳送點 | `map portals` | `list_portals` | 資訊+JSON |
| 通行 | `map passability` | `export_passability` | 匯出通行 |
| 區域檔 | `map regions` | `list_regions` | 資訊分頁 |
| Sprite 資訊 | `spr info` | `sprite_info` | 預覽 |
| Sprite 搜尋 | `spr search` | `search_sprite_entries` | 搜尋列 ID |
| Sprite 匯出 | `spr export` | `export_sprite_frames` | 匯出預設資料夾 |
| 版本 | `version` | `l1r_health` | 視窗標題/README |

## 5. 安全

- 預設唯讀；MCP **永不**寫入。
- 編輯模式：Shell 設定或 `--enable-edit`。
