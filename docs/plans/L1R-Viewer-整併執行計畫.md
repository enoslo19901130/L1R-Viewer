# L1R-Viewer 整併執行計畫（給執行 Agent 的作業規格）

> 產出：2026-07-24 · 策略：**選項 C（分階段:Core → CLI → MCP → GUI → 改名）**（owner 已拍板）
> 讀者:**這份文件是交給另一個 AI Agent 逐項執行的規格**,不是概念摘要。請照 §F 的 Phase 順序做,每個 Phase 做完必須通過該 Phase 的「驗收 Gate」才進下一步。
> 你(執行 Agent)**沒有**產生這份計畫時的對話脈絡。開工前先讀完 §A–§E。

---

## §A 你必須先知道的背景（沒有這段你會做錯方向）

1. **要整併的三個專案(本機路徑,均非 git repo,原樣勿改,一律在複本上作業):**
   - 舊主體(將被取代/改名):`C:\Users\EnosLo\Desktop\00-Workspace\000-Ongoing-Projects\006-Tools\LineageTool`
   - 參考A:`...\006-Tools\參考\PakViewer-1.26.07162329`
   - 參考B:`...\006-Tools\參考\L1MapViewer-1.26.07162306`
2. **最關鍵的事實(決定整個架構):PakViewer 與 L1MapViewer 已經共用同一顆引擎。**
   `L1MapViewer` 的 `L1MapViewerCore.csproj` 內有 `<PackageReference Include="Lin.Helper.Core" Version="1.5.4" />`,而 `Lin.Helper.Core` 的原始碼就在 `PakViewer\src\Lin.Helper.Core`(MIT、`PackageId=Lin.Helper.Core`、TFM `net8;net9;net10`)。→ 兩者是「同一引擎上的兩個前端」:PakViewer=封存/圖素瀏覽器,L1MapViewer=地圖檢視/編輯器。
3. **舊 LineageTool 是異類**:.NET Framework 4.0/4.8、WinForms 巨石(`LineageTool\LineageTool\Form1.cs` 18,818 行)。它**唯一的獨門價值**是:
   - `LineageTool\mcp\server.py`(Python FastMCP,6 個工具)
   - `LineageTool\headless\LineageTool.Cli`(輸出單行 JSON 的 headless CLI,靠反射載入 `LineageTool.exe`——**這是脆弱設計,要退役**)
   - 專案根 `.mcp.json`(登錄名 `lineage-tool-v2`)
4. **`Lin.Helper.Core` 已涵蓋舊工具全部解碼能力,外加完整 S32 地圖**(見 §D)。所以「地圖讀取」不是從零寫,是把 Core 既有能力接出來。
5. **owner 的硬要求:以讀取客戶端資料為主、不破解客戶端。** 兩個參考帶有的「寫入/編輯」(改 S32、寫回 pak)**不刪除,但預設關閉**,且 **MCP/agent 可用面一律只給讀取**(見 §C)。

---

## §B 環境與前置條件

- 作業系統:Windows 11;Shell:PowerShell 7+。
- **.NET SDK 已安裝:`10.0.301`**(`dotnet --version` 應回 `10.0.301`)。若不在則停止並回報。
- 測試用真實客戶端(唯讀,驗證地圖渲染用):
  `C:\Users\EnosLo\Desktop\00-Workspace\000-Ongoing-Projects\LineageR-2606262601\001-CLIENT\LineageRemastered-2606262601`
  - 內含 `map\`(數百個 `<mapId>\` 子夾,每夾一堆 `*.s32` / `*.s32.bro`,加 `<mapId>.map`、`*.MarketRegion`、`*.TeleportOkRegion`、`*.fishingRegion`)、`Tile.idx`(`_RMS` 格式)、`Tile.pak`(986MB)。
  - 已驗證可用 map 範例:`map\0\7ff88000.s32`;`Tile.idx` 內圖磚命名為 `<n>.til`(0–5973,共 5,942 條)。
- 產出落點(新專案):`C:\Users\EnosLo\Desktop\00-Workspace\000-Ongoing-Projects\006-Tools\L1R-Viewer`(Phase 0 建立)。
- 這份計畫本身位於:`006-Tools\LineageTool\plans\L1R-Viewer-整併執行計畫.md`。

---

## §C 全域規則與禁止事項（每個 Phase 都適用，違反即失敗）

- ✅ **全程在複本上作業**:不得原地改動上述三個來源資料夾;把需要的原始碼**複製**進新 `L1R-Viewer`。三個來源保持原樣。
- ✅ **新專案第一步就 `git init`**,每個 Phase 完成各一個 commit,commit message 結尾附 `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`。
- ✅ **日誌規範**(沿用 L1MapViewer 慣例):一律 `NLog`,禁止 `Console.WriteLine`(CLI 的 stdout 輸出例外);**exception 不可靜默吞掉**(禁止空 `catch {}`)。
- ✅ **讀取為主**:所有「寫入/編輯」功能(S32 write、`import-fs32`、`pak import`、`fix`、`trim-s32`、`clear-l8` 等)必須 gate 在明確旗標 `--enable-edit` 之後,預設停用。
- ⛔ **MCP / agent 面只暴露讀取工具**,不得經 MCP 暴露任何寫入。
- ⛔ **不得碰** `Lin.bin` / `Lin.exe` / NCGuard 殼 / aegis 反作弊 / 執行中的 client / live 網路流量 / 憑證。本專案只處理**離線靜態資產檔**。
- ⛔ **不要改 `Lin.Helper.Core` 的命名空間**(它是對外發布相依,改名平白打斷 references)。改名只做在「產品/exe/repo/視窗標題/版本資訊」層級。
- ⛔ **不要自行 push 到 GitHub**、不要建遠端 repo。是否上傳、公開/私有是 owner 決策(§K),未獲明示前只做本機 git commit。
- ⛔ 卡關時**不要臆測硬幹**:把「已完成項 / 卡點 / 需 owner 決策項」依 §J 格式回報。

---

## §D 名詞與檔案格式速查（實作與測試時對照用）

- **IDX/PAK**:封存索引+資料。`Lin.Helper.Core.Pak`(`IdxHandler/PakFile/PakTools`)負責。舊工具解密鏈:DES → (Brotli/zlib/zstd) 解壓 → 若 XML 再 AES 解。`_RMS`/`_ext` 是 idx 標頭變體。
- **TIL/TBT**:圖磚(tileset,內含多個 24×24 分片)。`Core.Tile`(`L1Til/MTil`)。
- **SPR/SPX/IMG**:動畫精靈/影像。`Core.Sprite`、`Core.Image`。
- **S32(地圖分段)**:`Core.Map.S32Reader` 權威格式(逐層):
  - Layer1 地板 64×128,每格 `IndexId:byte + TileId:uint16 + nk:byte`。
  - Layer2 覆蓋物;**Layer3 通行/區域屬性 64×64**(`Attribute1/2:int16`,伺服端通行對帳用);Layer4 物件/建築;Layer5 事件;Layer6 til 參照;**Layer7 傳送點**(`name+X+Y+TargetMapId+PortalId`,伺服端 warp 對帳用);Layer8 SPR 特效。
  - 區塊像素 3072×1536。`.s32.bro`=壓縮變體。`.map`/`SEG`/`MarketRegion` 等由 `Core.Map`/L1MapViewer 對應解析。
- **舊 headless JSON 契約**(要保留相容):每次 stdout 輸出**單行 JSON**,固定含 `{ "ok": bool, "command": string, ... }`,錯誤時 `{ "ok": false, "error": string, "type": string }`。

---

## §E 目標架構（Phase 0 就照這個骨架建）

```
L1R-Viewer/
  L1R-Viewer.sln
  Directory.Build.props            ← 統一版本號 1.yy.MMddHHmm / TFM / NoWarn / NLog
  .gitignore  .github/workflows/
  src/
    Lin.Helper.Core/               ← 由 PakViewer\src\Lin.Helper.Core 複製,命名空間不變
    L1R.Cli/                       ← 統一 CLI(合併三方指令 + --json);ProjectReference Core
    L1R.PakBrowser/                ← 由 PakViewer 的 Eto front-end 複製;ProjectReference Core
    L1R.MapViewer/                 ← 由 L1MapViewer 複製;PackageReference 改 ProjectReference Core
  mcp/
    server.py                      ← 由 LineageTool\mcp\server.py 移植,改指向 L1R.Cli
  .mcp.json                        ← server 名改 l1r-viewer,指向 mcp\server.py
  docs/  tests/
```
相依方向(單向、無環):`Core ← L1R.Cli ← mcp`;`Core ← L1R.PakBrowser`;`Core ← L1R.MapViewer`。

---

## §F 分階段任務

> 每個 Phase:**目標 → 逐項任務(勾選)→ 交付物 → 驗收 Gate**。Gate 未過不得進下一 Phase。

### Phase 0 — Solution 骨架與引擎統一
**目標**:建立 `L1R-Viewer` solution,把 `Lin.Helper.Core` 收為單一原始碼來源,三個子專案都能建置。

- [ ] 0.1 建 `006-Tools\L1R-Viewer\`,`git init`(main 分支)。
- [ ] 0.2 **先驗證 §A 事實**:開 `L1MapViewerCore.csproj` 確認有 `PackageReference Lin.Helper.Core 1.5.4`;開 `PakViewer\src\Lin.Helper.Core\Lin.Helper.Core.csproj` 確認 `PackageId=Lin.Helper.Core`。若與描述不符,依 §J 回報後停。
- [ ] 0.3 複製 `PakViewer\src\Lin.Helper.Core` → `L1R-Viewer\src\Lin.Helper.Core`(含 `Resources\*.bin` 內嵌資源)。命名空間保持 `Lin.Helper.Core`。
- [ ] 0.4 複製 PakViewer 的 CLI(`PakViewer\src\PakViewer.Cli`)→ 暫置 `src\L1R.Cli`(Phase 1 再擴充);把它對 Core 的參照改成 `ProjectReference ..\Lin.Helper.Core\Lin.Helper.Core.csproj`。
- [ ] 0.5 複製 L1MapViewer 整個專案 → `src\L1R.MapViewer`;把 `L1MapViewerCore.csproj` 內 `PackageReference Lin.Helper.Core 1.5.4` **改為** `ProjectReference ..\Lin.Helper.Core\Lin.Helper.Core.csproj`(消版本歪斜)。保留其 SkiaSharp/Eto/NLog 相依與 `SKIA_LEGACY` 雙版本設定。
- [ ] 0.6 複製 PakViewer 的 Eto front-end(排除其 csproj 已 `Compile Remove` 的 WinForms `uc*/frm*`)→ `src\L1R.PakBrowser`;參照改 `ProjectReference` Core。
- [ ] 0.7 建 `Directory.Build.props`:統一版本號規則 `1.yy.MMddHHmm`、共用 `NoWarn`、`GenerateAssemblyInfo=false`。
- [ ] 0.8 建 `L1R-Viewer.sln`,加入四個 `src\*` 專案。
- [ ] 0.9 建 `.gitignore`(排除 `bin/ obj/ publish/ tests/ .vs/ *.suo` 與大型輸出)。

**交付物**:可建置的 solution 骨架 + 首個 git commit。
**驗收 Gate**:
- `dotnet build L1R-Viewer.sln -c Release` 全綠。
- `dotnet run --project src\L1R.MapViewer -- -cli info "<client>\map\0\7ff88000.s32"` 能印出該 S32 的層資訊(證明 MapViewer 用 in-repo Core 正常)。

### Phase 1 — 統一 CLI `l1r`（合併三方指令 + JSON 契約）
**目標**:一支 `l1r` 可執行檔涵蓋封存/圖素/地圖的讀取指令,並提供 `--json` 契約給 MCP。
（三方現有指令面見 §G。實作策略:`L1R.Cli` 作為單一進入點,封存/圖素群組沿用 PakViewer.Cli 的 dispatch;地圖群組轉呼 `L1R.MapViewer` 內的 CLI 邏輯或 `Core.Map`。**避免把 L1MapViewer 的 GUI 相依拉進 CLI**——若其 CLI 與 GUI 綁太緊,將所需的地圖 CLI 邏輯抽到一個不依賴 Eto/Skia 的 `L1R.MapViewer.Core`（或直接用 `Lin.Helper.Core.Map`）供 `L1R.Cli` 呼叫。）

- [ ] 1.1 設計 `l1r` 頂層 dispatch:`pak | spr | til | img | dat | xml | map | sprite | version | help`。
- [ ] 1.2 封存/圖素群組(`pak/spr/til/img/dat/xml`):併入 PakViewer.Cli 既有實作。
- [ ] 1.3 地圖群組 `l1r map`(**只做讀取子集**):`info`、`layers`、`portals`(Layer7)、`passability`(Layer3)、`regions`(MarketRegion/TeleportOk/fishing)、`list-maps`、`render`(單一 mapId 整圖→PNG)、`render-adjacent`、`extract-tile`。
- [ ] 1.4 相容別名 `l1r sprite info|search|export|sheet`:對應舊 LineageTool headless 的 5 指令,內部轉呼新實作,**JSON 欄位不得回歸**(見 §D 契約)。
- [ ] 1.5 **全域 `--json`**:任一指令加 `--json` 時輸出單行 JSON(`{ok,command,...}`);錯誤輸出 `{ok:false,error,type}` 並回傳非 0。
- [ ] 1.6 寫入類指令(`pak import`、`fix`、`trim-s32`、`import-fs32`、`clear-l8` 等)一律要求 `--enable-edit`,否則印錯誤並回非 0。
- [ ] 1.7 `l1r help` 與各群組 `--help` 輸出用法。

**交付物**:`L1R.Cli`(exe `l1r`)+ 指令對照文件(§G 落成 `docs/cli.md`)。
**驗收 Gate**(全部對真實 client 跑通):
- `l1r map render --client "<client>" --id 4 --output tests\out\map-4.png` 產出非空 PNG。
- `l1r map portals --client "<client>" --id 4 --json` 輸出可被 `ConvertFrom-Json` 解析且含 Layer7 傳送點清單。
- `l1r map passability --client "<client>" --id 4 --json` 輸出 Layer3 通行資料。
- `l1r sprite info --client "<client>" --id 167 --json` 的 `{ok,command,...}` 欄位與舊 headless 相容。
- 不帶 `--enable-edit` 呼叫任一寫入指令 → 回非 0 並提示。

### Phase 2 — Python MCP 接上新 CLI（★ 本計畫最高價值里程碑）
**目標**:agent 可透過 MCP 直接讀地圖與客戶端資訊。達成後 owner 最初要的「地圖讀取 + 客戶端資訊分析 + agent 可用」即完成。

- [ ] 2.1 複製 `LineageTool\mcp\server.py` → `L1R-Viewer\mcp\server.py`;把 `CLI` 路徑指向 `L1R.Cli` 的建置輸出(`_ensure_cli()` 的 build 指令改成 `dotnet build src\L1R.Cli`);FastMCP 名稱改 `l1r-viewer`。
- [ ] 2.2 保留既有 6 工具(`lineage_tool_health`→`l1r_health`、`sprite_info`、`search_sprite_entries`、`export_sprite_frames`、`create_sprite_sheet`、`create_sprite_range_sheet`),內部改叫 `l1r ... --json`。
- [ ] 2.3 **新增讀取工具**(規格見 §H):`map_info`、`render_map`、`list_portals`、`export_passability`、`list_regions`、`list_maps`。
- [ ] 2.4 更新 `L1R-Viewer\.mcp.json`:server 名 `l1r-viewer`,command 指向 `mcp\server.py`。
- [ ] 2.5 寫 `mcp\smoke_test.py`(仿舊專案):對測試 client 跑 `l1r_health` + `map_info(mapId=4)` + `render_map(mapId=4)`。

**交付物**:`mcp\server.py`、`.mcp.json`、`smoke_test.py`。
**驗收 Gate**:
- `python mcp\smoke_test.py` 全過:`l1r_health.ok==true`、`render_map` 產出 PNG、`list_portals` 回傳非空。
- **MCP 面無任何寫入工具**(人工核對工具清單)。

### Phase 3 — 解碼一致性驗證 → 退役舊 base
**目標**:證明 `Lin.Helper.Core` 的圖素輸出與舊 LineageTool「v2 色彩校正解碼器」一致(或更好),然後正式退役舊 .NET Framework 主體與反射式 headless host。

- [ ] 3.1 選一組代表集(至少:sprite 167/169/170、數個 `<n>.til`、數個 `.img`)。
- [ ] 3.2 用舊 `LineageTool` headless 各出一張 PNG(基準),用 `l1r` 各出一張 PNG(新),逐張像素 diff。
- [ ] 3.3 diff=0 → 記錄結論,標記舊 .NET Framework 專案與 `LineageTool.exe`、反射式 `headless\LineageTool.Cli` 為**退役**(移出建置路徑,不再被任何相依)。
- [ ] 3.4 若有色差 → 把舊色彩校正邏輯移植進 `Core`,建 issue 追蹤(**此項不阻塞 Phase 4/5**)。

**交付物**:`docs\decoder-parity-report.md`(含 diff 結果與樣本)。
**驗收 Gate**:代表集 diff=0,或差異有據可查並已修正/建 issue;`l1r`/MCP 不再相依任何舊 .NET Framework 產物。

### Phase 4 — 地圖 GUI 納入（`L1R.MapViewer`，唯讀優先）
**目標**:成熟地圖檢視器上線,預設唯讀。

- [ ] 4.1 `L1R.MapViewer`(即 Phase 0 複製的 L1MapViewer)可獨立啟動並開啟測試 client。
- [ ] 4.2 **預設唯讀**:編輯入口(S32 存檔、Layer4 批次刪除、undo/redo 寫回)在未給 `--enable-edit`(或設定旗標)時全部禁用/隱藏。
- [ ] 4.3 沿用其 viewport 裁切、minimap、跳座標、匯出 PNG;確認 `SKIA_LEGACY` 雙版本建置仍可產出。

**交付物**:可執行的 `L1R.MapViewer`。
**驗收 Gate**:唯讀開圖/縮放/平移/跳座標/匯出 PNG 正常;未給 edit 旗標時無任何寫入入口可用。

### Phase 5 — 資產瀏覽 GUI 納入（`L1R.PakBrowser`，唯讀優先）
**目標**:封存/文字/spr/til/gallery 瀏覽器上線,預設唯讀。

- [ ] 5.1 `L1R.PakBrowser`(Phase 0 複製的 PakViewer Eto front-end)可獨立啟動。
- [ ] 5.2 `pak import`/寫回 gate 在 `--enable-edit`。
- [ ] 5.3 確認文字檢視的編碼自動偵測(`-c`=Big5、`-k`=EUC-KR、`-j`=Shift_JIS、`-h`=GB2312)仍運作。

**交付物**:可執行的 `L1R.PakBrowser`。
**驗收 Gate**:瀏覽 pak/文字/spr/til/gallery 正常;預設無寫入。

### Phase 6 — 改名 / 品牌 / 單一入口 / 文件
**目標**:對外統一為 **L1R-Viewer**。

- [ ] 6.1 產品名、exe 名、視窗標題、版本資訊、`README` 改 **L1R-Viewer**(**`Lin.Helper.Core` 命名空間不動**)。
- [ ] 6.2 (可選)做一個 launcher:一個入口選 `PakBrowser` / `MapViewer` / CLI。
- [ ] 6.3 更新/新增 `docs\`:整併後的 `README.md`、`HEADLESS.md`(改指 `l1r`)、`cli.md`、`mcp.md`、本計畫與 `decoder-parity-report.md`。
- [ ] 6.4 全新 clone 驗證:一鍵建置 → CLI + 兩 GUI + MCP 全綠。

**交付物**:改名完成的 repo + 完整 docs。
**驗收 Gate**:乾淨環境 `git clone`(本機複本亦可)→ `dotnet build` → 四入口全部可跑;文件齊備。

---

## §G 三方 CLI 指令 → 統一 `l1r` 對照（Phase 1 依此合併）

| 來源 | 現有指令 | 併入 `l1r` |
|---|---|---|
| 舊 LineageTool headless | `health / info / search / export / sheet`(sprite 導向,JSON) | `l1r sprite info|search|export|sheet`(相容別名)+ `l1r --json` |
| PakViewer.Cli | 群組 `pak / spr / dat / xml / map / til`(如 `pak list|read|export|import|info`) | 原樣併入 `l1r pak|spr|dat|xml|til`;`img` 補上 |
| L1MapViewer `-cli` | `info / layers / l1 / l3 / l4 / l5 / l6 / l7 / l8 / tile-stats / scan-tiles / til-info / export / coords / export-tiles / list-maps / extract-tile / render-adjacent / render-material / list-til / list-idx / validate-tiles / export-passability / export-fullmap / batch-export / benchmark-* / **(寫入類)** fix / trim-s32 / clear-l8 / import-fs32 / export-fs32` | **讀取子集**→ `l1r map info|layers|portals|passability|regions|list-maps|render|render-adjacent|extract-tile|export-fullmap`;**寫入類**→ 保留但 gate 在 `--enable-edit` |

原則:`l1r map render`(整張,新增,對應 owner 的「地圖讀取」)= 載入某 mapId 的所有 `.s32`(含 `.s32.bro` 解壓)→ Core 解各層 → 用 `Tile.idx`/`Tile.pak` 解 til 分片 → 依 §D 座標合成 → PNG。L1MapViewer 已有 `render-adjacent`/`export-fullmap` 可直接沿用/包裝。

---

## §H MCP 讀取工具規格（Phase 2 依此實作，全部唯讀）

所有工具內部呼叫 `l1r <...> --json` 並回傳解析後 dict。`client_path` 為客戶端根資料夾絕對路徑。

| 工具 | 參數 | 對應 CLI | 回傳重點 |
|---|---|---|---|
| `l1r_health` | — | `l1r version`/`health` | `{ok, assembly_version, cli_ready}` |
| `sprite_info` | `client_path, sprite_id` | `l1r sprite info` | 變體 + frame metadata |
| `search_sprite_entries` | `client_path, query, limit=100` | `l1r sprite search` | 名稱符合清單 |
| `export_sprite_frames` | `client_path, sprite_id, output_dir, frame_index?` | `l1r sprite export` | 匯出的 PNG + manifest |
| `create_sprite_sheet` | `client_path, sprite_ids[], output_path, columns=6, frame_index=0` | `l1r sprite sheet` | 比較圖 PNG |
| `create_sprite_range_sheet` | `client_path, start_id, end_id, output_path, ...` | `l1r sprite sheet` | 範圍比較圖 |
| **`map_info`** | `client_path, map_id` | `l1r map info` | 分段數、邊界、引用 til 統計 |
| **`render_map`** | `client_path, map_id, output_path` | `l1r map render` | 產出的整圖 PNG 路徑 |
| **`list_portals`** | `client_path, map_id` | `l1r map portals` | Layer7 傳送點(name,x,y,targetMapId,portalId) |
| **`export_passability`** | `client_path, map_id, output_path?` | `l1r map passability` | Layer3 通行/區域屬性 |
| **`list_regions`** | `client_path, map_id` | `l1r map regions` | Market/TeleportOk/fishing 區域 |
| **`list_maps`** | `client_path` | `l1r map list-maps` | 客戶端所有 mapId 清單 |

**禁止**在 MCP 暴露 render 以外的寫入/編輯(`fix/trim/import/clear-*` 一律不上 MCP)。

---

## §I 驗證指令附錄（測試 fixture 與命令）

```powershell
$client = "C:\Users\EnosLo\Desktop\00-Workspace\000-Ongoing-Projects\LineageR-2606262601\001-CLIENT\LineageRemastered-2606262601"

# Phase 0
dotnet build .\L1R-Viewer.sln -c Release
dotnet run --project .\src\L1R.MapViewer -- -cli info "$client\map\0\7ff88000.s32"

# Phase 1
dotnet run --project .\src\L1R.Cli -- map render --client "$client" --id 4 --output .\tests\out\map-4.png
dotnet run --project .\src\L1R.Cli -- map portals --client "$client" --id 4 --json | ConvertFrom-Json
dotnet run --project .\src\L1R.Cli -- sprite info --client "$client" --id 167 --json | ConvertFrom-Json

# Phase 2
python .\mcp\smoke_test.py
```
- 已知好用測試 mapId:`4`(7.3MB 大圖)、`105`、`1000`;單一分段 `map\0\7ff88000.s32`。
- render 產物請落 `tests\out\`(已在 `.gitignore`,勿入庫)。

---

## §J 回報格式（每個 Phase 結束、或卡關時）

請輸出:
1. **Phase / 任務編號**(如「Phase 1 / 1.3」)與狀態(done / blocked)。
2. **Gate 結果**:逐條列 §F 該 Phase 的驗收條目 + 實際命令輸出摘要(pass/fail)。
3. **變更清單**:新增/修改的檔案路徑。
4. **卡點**:錯誤訊息原文 + 你已試過什麼。
5. **需 owner 決策項**:明確列出(尤其 §K)。
不要在未過 Gate 時宣稱完成;不要跳 Phase。

---

## §K 待 owner 拍板（執行 Agent 不得自行決定，遇到就停下回報）

- **(d) GitHub**:L1R-Viewer 是否上傳?私有/公開?——**未獲明示前只做本機 git commit,不建遠端、不 push。**
- **(e) 監視器編輯範疇**:是否在本輪就啟用 `--enable-edit` 對外(預設:實作但停用,僅本機、不上 MCP)。
- 其餘 owner 已定:策略=**C**;退役舊 .NET Framework base=**是**;讀取為主、寫入 opt-in=**是**。
