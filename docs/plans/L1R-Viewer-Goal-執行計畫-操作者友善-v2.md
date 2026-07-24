# L1R-Viewer Goal 執行計畫 v2  
## 以「操作者友善」為核心 · GUI 整併 + MCP/CLI 對齊

| 欄位 | 內容 |
|---|---|
| 版本 | **v2.0**（2026-07-25） |
| 狀態 | **待執行規格**（給後續 Agent / 工程師逐項執行） |
| 前一版 | `docs/plans/L1R-Viewer-整併執行計畫.md`（v1：Core→CLI→MCP 為主，已大部分完成） |
| 產出 repo | https://github.com/enoslo19901130/L1R-Viewer |
| 本機路徑 | `…\006-Tools\L1R-Viewer` |
| Owner 已拍板 | 讀取為主；寫入 opt-in；MCP 唯讀；GitHub 可 push |
| 本版核心目標 | **讓「人」好用，「Agent」也好用；GUI 從「並列收編」升級到「體驗整併」** |

---

## 0. 一句話目標

> 把 L1R-Viewer 做成：**離線讀取天堂 R 客戶端資產**的統一工具箱——  
> **操作者**用一個清楚的入口就能開地圖、看圖素、匯出 PNG/JSON；  
> **Agent** 用 MCP/CLI 做同一件事且契約穩定；  
> 寫入能力存在但**預設關、明示開、不上 MCP**。

---

## 1. 現況盤點（v2 開工前必須承認的事實）

### 1.1 已完成（v1 成果，勿重做）

| 區塊 | 狀態 | 說明 |
|---|---|---|
| monorepo + Core 單一來源 | ✅ | `Lin.Helper.Core` ProjectReference |
| CLI launcher `l1r.ps1` | ✅ | map → MapViewer；pak/spr → Cli |
| headless 地圖 PNG | ✅ | MapExporter/Skia 路徑 |
| MCP `l1r-viewer` 12 唯讀工具 | ✅ | smoke_test PASS |
| 預設唯讀 / `--enable-edit` | ✅ | CLI + 兩 GUI 基本 gate |
| GitHub + CI build | ✅ | Actions: build + py_compile |
| 文件骨架 | ✅ | README / cli / mcp / HEADLESS |

### 1.2 缺口（v2 要解的痛）

| # | 痛點 | 對誰痛 | 嚴重度 |
|---|---|---|---|
| P1 | **雙 GUI 並列**，入口/名稱/exe 不統一（`L1MapViewerCore.exe` / `PakViewer.exe`） | 操作者 | 高 |
| P2 | **沒有「首次使用」嚮導**：找不到 client 路徑、不知 Tile.idx/map 結構 | 操作者 | 高 |
| P3 | 地圖匯出參數（max-size、輸出資料夾）**散在 CLI 參數**，GUI 與 MCP 體驗不一致 | 兩者 | 中高 |
| P4 | 傳送點 / 通行 / 區域 **MCP 有、GUI 未必一眼可見** | 操作者 | 中 |
| P5 | Sprite 用數字 ID / spx 變體概念 **對新手不直觀** | 操作者 | 中 |
| P6 | 錯誤訊息中英混雜、部分 Console 亂碼（code page） | 操作者 | 中 |
| P7 | 設定（client 路徑、語系、輸出目錄）**兩套 app 各存一份** | 操作者 | 中 |
| P8 | 文件多但**沒有「5 分鐘上手」操作手冊** | 操作者 | 中 |
| P9 | CLI 仍是 launcher 雙後端，參數風格不一致 | 腳本/進階者 | 中低 |
| P10 | 解碼 parity 與舊工具尺寸不同（已記錄，可選優化） | 進階/對帳 | 低 |

### 1.3 刻意不做（v2 範圍外）

- 破解 / 改殼 / live client / 網路封包  
- 重寫 `Lin.Helper.Core` 命名空間  
- 把舊 LineageTool 18k 行 Form1 整包搬進 GUI  
- MCP 暴露任何寫入工具  
- 預設開啟編輯（仍須 `--enable-edit`）

---

## 2. 產品願景與使用對象

### 2.1 三種操作者（Persona）

| Persona | 是誰 | 主要任務 | 成功標準 |
|---|---|---|---|
| **A. 地圖/企劃操作者** | 人，不熟命令列 | 開 client → 選 mapId → 看圖 → 匯出 PNG / 傳點表 | 3 次點擊內完成「開圖+匯出」 |
| **B. 資產操作者** | 人，查 spr/til/文字 | 開 client → 搜 ID/名稱 → 預覽 → 匯出 PNG | 不用記 idx 檔名 |
| **C. Agent / 自動化** | MCP 或腳本 | 穩定 JSON、可重跑、唯讀 | smoke_test 全綠；契約不破壞 |

### 2.2 友善性原則（所有 Phase 強制）

1. **單一心智模型**：Client 根目錄（含 `map\`、`Tile.idx`）= 工作區。  
2. **預設安全**：唯讀；危險操作二次確認 + 明確旗標。  
3. **可見進度**：長任務（render 大地圖）要有狀態列 / 百分比 / 可取消（GUI）；CLI 印階段訊息。  
4. **錯誤可行動**：訊息必須回答「發生什麼、為什麼、下一步按哪」。禁止只丟 exception type。  
5. **中文優先 UI**（zh-TW 預設），英文次之；技術 log 可用英文。  
6. **同一能力三入口對齊**：GUI 按鈕 ⇄ CLI 指令 ⇄ MCP 工具（命名與參數語意一致）。  
7. **不強迫命令列**：常用路徑 GUI 必須可做；CLI/MCP 是進階與自動化。

### 2.3 目標架構（v2 結束時）

```
L1R-Viewer/
  Launch-L1R-Viewer.ps1 / L1R-Viewer.exe (shell)   ← 操作者第一入口
  src/
    Lin.Helper.Core/          # 引擎（不變 ns）
    L1R.Cli/                  # 統一 CLI（逐步收斂參數）
    L1R.MapViewer/            # 地圖模組（GUI + -cli）
    L1R.PakBrowser/           # 資產模組（GUI）
    L1R.Shell/  (NEW)         # 輕量主殼：歡迎頁、最近 client、開地圖/開資產、設定
    L1R.Shared/ (NEW 可選)    # 共用設定、路徑驗證、錯誤訊息、輸出路徑慣例
  mcp/                        # Agent 面（唯讀）
  docs/
    GETTING-STARTED.md        # 5 分鐘上手（人）
    OPERATOR-MANUAL.md        # 操作手冊（人）
    cli.md / mcp.md / …
```

**策略選擇（已預設，除非 owner 改）**  
採 **「輕量 Shell + 兩模組」**，不做巨型單一 Form 重寫：

- Shell：歡迎、選 client、最近清單、捷徑、設定、關於  
- 點「地圖」→ 啟動/嵌入 MapViewer（同 process 優先；不行再 process 啟動並傳 client）  
- 點「資產」→ 啟動/嵌入 PakBrowser  

若嵌入成本過高，**v2 允許 process 啟動**，但必須：

- 共用 `%AppData%\L1R-Viewer\settings.json`  
- 同一 client 路徑  
- 同一輸出資料夾慣例  

---

## 3. 全域規則（繼承 v1 並更新）

| ID | 規則 |
|---|---|
| R1 | 讀取為主；寫入僅 `--enable-edit` / 設定「進階編輯」+ 重啟確認 |
| R2 | MCP **禁止**寫入工具 |
| R3 | 不動 `Lin.Helper.Core` 命名空間 |
| R4 | 離線靜態檔；不碰 live client / 反作弊 / 憑證 |
| R5 | 每個 Phase：**任務勾選 → Gate → commit → push origin main** |
| R6 | commit 訊息英文祈使句；可附 `Co-Authored-By` |
| R7 | 卡關依 §J 回報，不臆測硬幹 |
| R8 | **操作者可見字串**優先走 i18n（zh-TW / en）；禁止新 hardcode 無註解的中英混雜 |
| R9 | 新增能力必須在 `docs/cli.md` + `docs/mcp.md` + 操作手冊同步一行（三入口對齊） |
| R10 | 測試產物只落 `tests/out/`（gitignore） |

### 3.1 設定檔契約（新增）

路徑：`%AppData%\L1R-Viewer\settings.json`

```json
{
  "schemaVersion": 1,
  "language": "zh-TW",
  "recentClients": [
    { "path": "D:\\...\\LineageRemastered-...", "lastOpenedUtc": "..." }
  ],
  "lastClientPath": "...",
  "defaultOutputDir": "%USERPROFILE%\\Documents\\L1R-Viewer\\exports",
  "map": {
    "defaultMaxSize": 2048,
    "defaultShowLayer8": true
  },
  "ui": {
    "enableEdit": false,
    "confirmDangerousActions": true
  },
  "mcp": {
    "note": "MCP never reads enableEdit for writes"
  }
}
```

---

## 4. 分階段任務（v2 Phases）

> 編號接續 v1：從 **Phase 7** 起。  
> 每個 Phase 都有：**目標 · 操作者故事 · 任務清單 · 交付物 · 驗收 Gate · 預估工時（相對）**。

---

### Phase 7 — 操作者友善基礎：設定、路徑驗證、錯誤訊息、5 分鐘上手

**目標**：任何人裝完能在 5 分鐘內「選 client → 確認健康 → 知道下一步」。

**操作者故事**  
> 我第一次打開工具，被引導選擇天堂 client 資料夾；若資料夾不對，它告訴我缺 `map\` 或 `Tile.idx`，而不是崩潰。

**任務**

- [x] 7.1 新增 `L1R.Shared`（或暫放 `Launch-L1R-Viewer.ps1` + 小 C# 函式庫）實作：
  - `ClientPathValidator.Validate(path)` → `{ ok, missing[], hints[] }`
  - 檢查：`map\` 目錄、至少一個 `*.idx`（建議 `Tile.idx`）、可選 `sprite*.idx`
- [x] 7.2 實作 settings.json 讀寫（見 §3.1），最近 8 筆 client
- [x] 7.3 統一錯誤訊息格式（GUI MessageBox + CLI stderr）：
  - `錯誤：…`  
  - `原因：…`  
  - `建議：…`
- [x] 7.4 新增 `docs/GETTING-STARTED.md`（圖文步驟，繁中）
- [x] 7.5 `l1r.ps1 doctor` 或 `pakviewer-cli doctor <client>`：印健康檢查 JSON/文字
- [x] 7.6 MCP 新增唯讀工具 `validate_client(client_path)`（對齊 doctor）

**交付物**：Shared/validator、settings、GETTING-STARTED、doctor、MCP validate_client  

**驗收 Gate**

| # | 條件 |
|---|---|
| G7.1 | 指向錯誤資料夾時，GUI/CLI 出現「缺 map 或 Tile.idx」建議，exit≠0 |
| G7.2 | 指向真實 client 時 doctor/validate_client `ok=true` |
| G7.3 | GETTING-STARTED 依文操作，新人 5 分鐘內完成 doctor + 一張 map render |
| G7.4 | commit + push |

**預估**：M  

---

### Phase 8 — 單一友善入口 Shell（人的主畫面）

**目標**：操作者只記「開 L1R-Viewer」，不再找兩個 exe。

**操作者故事**  
> 桌面捷徑打開主畫面：最近 client 列表、大按鈕「瀏覽地圖」「瀏覽資產」「開啟輸出資料夾」「健康檢查」「說明」。

**任務**

- [x] 8.1 新增 `src/L1R.Shell`（Eto 或 WinForms，建議 **Eto** 與既有一致）
  - 歡迎頁 / 最近清單 / 瀏覽選 client
  - 按鈕：地圖、資產、輸出資料夾、Doctor、說明（開 GETTING-STARTED 或內嵌 Markdown 簡易檢視）
  - 狀態列：唯讀 / 編輯模式、目前 client 路徑
- [x] 8.2 「開啟地圖」：傳 `lastClientPath` 給 MapViewer（命令列參數或 IPC）
- [x] 8.3 「開啟資產」：同上給 PakBrowser
- [x] 8.4 設定對話框：語系、預設 max-size、輸出目錄、進階編輯（需勾選 + 說明危險）
- [x] 8.5 `Launch-L1R-Viewer.ps1` 預設改開 Shell；保留 `map`/`pak` 子命令
- [x] 8.6 （可選）安裝捷徑腳本 `Install-Shortcuts.ps1`（桌面 + 開始功能表）
- [x] 8.7 產品圖示統一（沿用 MapViewer icon 或新 L1R 標誌）

**交付物**：可執行 Shell、捷徑腳本、更新 README 首段「從這裡開始」

**驗收 Gate**

| # | 條件 |
|---|---|
| G8.1 | 雙擊 Shell → 不需參數即可看到主畫面 |
| G8.2 | 選 client 後開地圖 / 資產，兩者看到同一 client |
| G8.3 | 關閉重開仍記住 lastClientPath |
| G8.4 | 未啟用編輯時，Shell 明確顯示「唯讀」徽章 |
| G8.5 | commit + push |

**預估**：L  

---

### Phase 9 — 地圖 GUI 操作者體驗整併（MapViewer UX）

**目標**：地圖相關「常用讀取工作」GUI 一站完成，對齊 MCP 能力。

**操作者故事**  
> 我選 map 53，畫面直接渲染；側欄看到傳送點列表，一鍵匯出 PNG（自動 max-size）與 portals.json，不用開終端機。

**任務**

- [ ] 9.1 **首次開圖流程**  
  - 若無 client：彈出資料夾選擇 + validator  
  - 地圖下拉支援搜尋 mapId / 名稱  
- [ ] 9.2 **一鍵匯出工具列**（唯讀可用）  
  - 匯出目前地圖 PNG（讀 settings.defaultMaxSize）  
  - 匯出傳送點 JSON  
  - 匯出通行屬性  
  - 開啟輸出資料夾  
- [ ] 9.3 **側欄「資訊」面板**（對齊 MCP）  
  - 分段數、邊界、Layer7 清單（可點選跳座標）  
  - 區域檔存在否（Market / TeleportOk / fishing）  
- [ ] 9.4 進度：export-fullmap 顯示狀態（即使 CLI 子行程也要「處理中…」）  
- [ ] 9.5 唯讀模式下：編輯工具列維持隱藏；**匯出仍可用**（匯出是讀取）  
- [ ] 9.6 錯誤：Tile.idx 缺失、記憶體不足時給縮小 max-size 建議  
- [ ] 9.7 文件：`docs/OPERATOR-MANUAL.md` 地圖章節 + 截圖占位說明  

**交付物**：MapViewer UX 增強、操作手冊地圖章  

**驗收 Gate**

| # | 條件 |
|---|---|
| G9.1 | 無 CLI 情況下，GUI 可完成：開 client → map 53 → 匯出 PNG + portals |
| G9.2 | 側欄 Layer7 與 `list_portals` 結果一致（允許排序差異） |
| G9.3 | 唯讀模式無法存 S32，但可匯出 |
| G9.4 | commit + push |

**預估**：L  

---

### Phase 10 — 資產 GUI 操作者體驗整併（PakBrowser UX）

**目標**：用「sprite ID / 關鍵字」工作，而不是先懂 idx。

**操作者故事**  
> 我輸入 167，看到 8 個方向變體縮圖，點一個看 frames，一鍵匯出 PNG 到輸出資料夾。

**任務**

- [ ] 10.1 Client 模式強化：開 client 根目錄即聚合 `sprite*.idx`  
- [ ] 10.2 **智慧搜尋列**：支援  
  - 純數字 → 當 sprite id（列 `{id}-*.spx`）  
  - 文字 → 檔名 contains  
- [ ] 10.3 Sprite 預覽：frames 列表 + 尺寸；SPX 解碼錯誤顯示可讀訊息  
- [ ] 10.4 一鍵「匯出目前選取為 PNG」→ defaultOutputDir  
- [ ] 10.5 刪除/寫回僅編輯模式可見（已有基礎，需回歸測試 + 提示一致）  
- [ ] 10.6 文字檔預覽：保留編碼自動偵測；狀態列顯示目前編碼  
- [ ] 10.7 與 MCP `sprite_info` / `export_sprite_frames` 參數語意對齊文件表  

**交付物**：PakBrowser UX、操作手冊資產章  

**驗收 Gate**

| # | 條件 |
|---|---|
| G10.1 | 搜 167 → 見多變體 → 匯出至少 1 張 PNG 到輸出目錄 |
| G10.2 | 唯讀無法刪除；編輯模式可（本機測試用複本 idx，勿動正式 client） |
| G10.3 | commit + push |

**預估**：M–L  

---

### Phase 11 — 三入口能力對齊表落地（GUI ⇄ CLI ⇄ MCP）

**目標**：同一能力三處可達，命名一致，文件有總表。

**對齊總表（執行時勾選「已 GUI / 已 CLI / 已 MCP」）**

| 能力 | CLI | MCP | GUI 目標位置 |
|---|---|---|---|
| Client 健康檢查 | `doctor` | `validate_client` | Shell 按鈕 / 狀態 |
| 地圖清單 | `map list-maps` | `list_maps` | MapViewer 下拉+搜尋 |
| 地圖資訊 | `map info` | `map_info` | 側欄資訊 |
| 渲染 PNG | `map render` | `render_map` | 一鍵匯出 |
| 傳送點 | `map portals` | `list_portals` | 側欄+匯出 |
| 通行 | `map passability` | `export_passability` | 匯出按鈕 |
| 區域檔 | （補 CLI） | `list_regions` | 側欄 |
| Sprite 資訊 | `spr info --json` | `sprite_info` | 預覽面板 |
| Sprite 搜尋 | `spr search` | `search_sprite_entries` | 搜尋列 |
| Sprite 匯出 | `spr export` | `export_sprite_frames` | 一鍵匯出 |
| 版本/健康 | `version` | `l1r_health` | 關於對話框 |

**任務**

- [ ] 11.1 補齊缺口 CLI（regions 等）  
- [ ] 11.2 更新 `docs/cli.md` / `docs/mcp.md` / OPERATOR-MANUAL 交叉連結  
- [ ] 11.3 MCP smoke 擴充：validate_client + list_regions  
- [ ] 11.4 （可選）`l1r.ps1` 參數風格統一：`--client` `--id` `--output`（保留舊別名一版）  

**驗收 Gate**

| # | 條件 |
|---|---|
| G11.1 | 上表每一列至少 CLI+MCP 可用；GUI 標註「已有/Phase」不得空白 |
| G11.2 | smoke_test 擴充後全過 |
| G11.3 | commit + push |

**預估**：M  

---

### Phase 12 — 品牌與安裝體驗（真正「像一個產品」）

**目標**：名稱、exe、說明、版本一致，降低「這是哪個工具？」困惑。

**任務**

- [ ] 12.1 輸出檔名（可選、建議做）：  
  - `L1R-Viewer.exe`（Shell）  
  - `L1R-MapViewer.exe`  
  - `L1R-PakBrowser.exe`  
  - `l1r.exe`（CLI 或仍 ps1 + 未來 single-file）  
  - 更新所有 launcher 尋找邏輯  
- [ ] 12.2 關於對話框：版本、repo 連結、唯讀說明  
- [ ] 12.3 `docs/CHANGELOG.md` 維護  
- [ ] 12.4 Release 檢查清單：`docs/RELEASE-CHECKLIST.md`  
- [ ] 12.5 （可選）`dotnet publish` 腳本一鍵產出 `dist\` 可攜資料夾  
- [ ] 12.6 Tag 範例：`v1.1.0-operator-ux` 並 push tag  

**驗收 Gate**

| # | 條件 |
|---|---|
| G12.1 | 乾淨目錄依 README「從這裡開始」可 build + 開 Shell |
| G12.2 | 三個 exe 名稱與視窗標題皆含 L1R-Viewer 語意 |
| G12.3 | commit + push（+ tag 若 owner 同意） |

**預估**：M  

---

### Phase 13 — 品質、無障礙、回歸（友善的後半段）

**目標**：長時間使用不踩雷；回歸可自動化。

**任務**

- [ ] 13.1 GUI 煙測腳本清單（人工）+ 半自動 CLI 回歸 `tests/regression.ps1`  
- [ ] 13.2 大 map（id=4）render 超時/記憶體：GUI 建議 max-size  
- [ ] 13.3 日誌：GUI 提供「開啟 log 資料夾」  
- [ ] 13.4 無障礙：主要按鈕有文字不只 icon；高對比狀態（唯讀徽章）  
- [ ] 13.5 CI 增加：`l1r.ps1 help` 與 `doctor` 若有測試 fixture 的輕量路徑  
- [ ] 13.6 （可選）parity：`spr export --scale classic` 實驗分支  

**驗收 Gate**

| # | 條件 |
|---|---|
| G13.1 | `tests/regression.ps1` 在本機真實 client 上全綠（路徑用環境變數 `L1R_CLIENT`） |
| G13.2 | CI 仍綠 |
| G13.3 | commit + push |

**預估**：M  

---

## 5. 操作者旅程（驗收劇本）

### 旅程 U1 — 第一次使用（必須 ≤ 5 分鐘）

1. 建置或解壓 `dist`  
2. 啟動 Shell  
3. 選 client 資料夾  
4. 看到健康檢查通過  
5. 開地圖 → 選 53 → 匯出 PNG  
6. 開資產 → 搜 167 → 匯出一幀  

**失敗即 Phase 7–10 未完成。**

### 旅程 U2 — 日常查傳點

1. Shell 點最近 client  
2. 地圖 53 → 側欄看 Layer7 → 匯出 JSON  
3. 與 MCP `list_portals` 比對 count > 0  

### 旅程 U3 — Agent 批次

1. `python mcp/smoke_test.py`  
2. `render_map` max_size=1024  
3. 不出現寫入工具  

### 旅程 U4 — 誤用防護

1. 選錯資料夾 → 清楚錯誤  
2. 唯讀下嘗試刪除/存檔 → 被擋並提示如何開編輯  
3. 編輯模式需明示（設定或啟動參數）  

---

## 6. UX 細部規格（執行時對照）

### 6.1 文案語氣

- 用「你」或中性：「請選擇客戶端資料夾」  
- 避免內部代號裸奔：不要只寫 `S32Data null`  
- 技術細節放「詳細資訊」可展開區  

### 6.2 預設值（友善預設）

| 項目 | 預設 |
|---|---|
| 模式 | 唯讀 |
| 地圖匯出 max-size | 2048 px |
| 輸出目錄 | `文件\L1R-Viewer\exports` |
| 語系 | zh-TW |
| 最近 client | 最多 8 |

### 6.3 載入與回饋

| 操作 | 回饋 |
|---|---|
| 開啟 client | 狀態列「掃描地圖清單…」 |
| render | 「渲染中 map=53 …」完成後開資料夾可選 |
| 搜 sprite | 300ms debounce，避免每鍵全掃 |

### 6.4 危險操作

- 刪除 pak 項目、存 S32、fix：  
  - 必須編輯模式  
  - 必須確認對話框（標題含「無法復原」）  
  - 建議預設焦點在「取消」  

---

## 7. 技術約束與建議實作順序

### 7.1 建議順序（依賴）

```
Phase 7 (設定/驗證/文件)
    → Phase 8 (Shell 入口)
        → Phase 9 (地圖 UX) ∥ Phase 10 (資產 UX)   # 可平行
            → Phase 11 (三入口對齊)
                → Phase 12 (品牌/安裝)
                    → Phase 13 (品質回歸)
```

### 7.2 風險

| 風險 | 緩解 |
|---|---|
| MapViewer 程式碼巨大，改 UI 易回歸 | 只加工具列/側欄，少動渲染核心 |
| 嵌入兩 GUI 同 process 困難 | Shell process 啟動 + 共用 settings |
| SPX 解碼與舊工具差異 | 文件說明；可選 classic scale |
| 大地圖 OOM | 強制預設 max-size；GUI 顯示估算 |

### 7.3 禁止事項（重申）

- 不在 MCP 加寫入  
- 不改 Core 命名空間  
- 不 force-push main（除非 owner 明示）  
- 不把真實 client 資產 commit 進 git  

---

## 8. 回報格式（每個 Phase 結束）

```markdown
## Phase N 回報
- 狀態: done | blocked | partial
- Gate: 逐條 pass/fail + 指令摘要
- 變更檔案: …
- 截圖/路徑: tests/out/…（勿提交大圖可只描述）
- 操作者旅程 U1–U4: 通過哪些
- 需 owner 決策: …
- Git: commit sha + 已 push: yes/no
```

---

## 9. 需 Owner 決策項（執行前可預設，執行中可改）

| ID | 問題 | 建議預設 |
|---|---|---|
| D1 | Shell 要「嵌入」還是「啟動子行程」？ | **子行程**（快、風險低）；Phase 8 採用 |
| D2 | 是否改 exe 檔名（Phase 12）？ | **是** |
| D3 | 進階編輯是否允許 GUI 內切換（需重啟）？ | **是，預設關** |
| D4 | 是否做 Windows 安裝程式（msi）？ | **否**，先可攜 zip/publish |
| D5 | 公開 repo 是否加 LICENSE 檔？ | 沿用各元件授權；根目錄加 NOTICE |
| D6 | 是否繼續支援舊 LineageTool MCP 名稱別名？ | **否**，只維護 `l1r-viewer` |

---

## 10. 成功定義（v2 Done）

當下列全部成立，v2 Goal 宣告完成：

1. **U1 旅程** 新人 5 分鐘內完成（有文件可跟）。  
2. Shell 為預設入口；地圖/資產共用 client 設定。  
3. 常用讀取（render / portals / sprite 搜尋匯出）**不需 CLI**。  
4. MCP smoke 全綠且仍無寫入工具。  
5. README 首屏是「操作者路徑」，進階章節才是 MCP/CLI。  
6. 所有 Phase 7–13 的 Gate 通過並已 push 到 GitHub。  

---

## 11. 附錄 A — 與 v1 對照

| v1 Phase | v1 狀態 | v2 關係 |
|---|---|---|
| 0–2 | ✅ | 基礎，v2 依賴 |
| 3 | 🟡 差異已記錄 | 可選進 13.6 |
| 4–5 | ✅ 基本唯讀 | v2 9/10 加深 UX 而非只 gate |
| 6 | ✅ 文件/品牌雛形 | v2 12 產品化 |

## 12. 附錄 B — 快速命令（執行時）

```powershell
$repo = "…\L1R-Viewer"
$client = $env:L1R_CLIENT  # 建議設定
cd $repo
dotnet build L1R-Viewer.slnx -c Release
.\Launch-L1R-Viewer.ps1          # Phase 8 後預設 Shell
.\l1r.ps1 map render "$client\map\53" .\tests\out\m.png
python .\mcp\smoke_test.py
```

## 13. 附錄 C — 文件樹（v2 結束應存在）

```
docs/
  GETTING-STARTED.md      # Phase 7
  OPERATOR-MANUAL.md      # Phase 9–10
  HEADLESS.md             # 已有
  cli.md / mcp.md         # 持續更新
  CHANGELOG.md            # Phase 12
  RELEASE-CHECKLIST.md    # Phase 12
  decoder-parity-report.md
  plans/
    L1R-Viewer-整併執行計畫.md          # v1
    L1R-Viewer-Goal-執行計畫-操作者友善-v2.md  # 本檔
    L1R-Viewer-執行進度.md              # live log
```

---

## 14. 給執行 Agent 的開工指令

1. 讀完本檔 §1–§4、§7、§9。  
2. 確認 `git remote` 指向 `enoslo19901130/L1R-Viewer`，`main` 可 push。  
3. 從 **Phase 7** 開始；**不要**回頭重做 v1 Phase 0–2。  
4. 每 Phase：實作 → Gate → 更新 `L1R-Viewer-執行進度.md` → commit → **push**。  
5. 任何 UX 決策模糊時：選 **「對操作者更少步驟」** 的方案，並在進度檔記錄。  

---

**本計畫即 v2 Goal 的權威規格。**  
實作以本檔為準；與 v1 衝突時，以 **v2（操作者友善 + GUI 整併）** 為優先，但不得違反 R1–R4 安全規則。
