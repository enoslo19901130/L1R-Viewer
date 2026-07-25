# L1R-Viewer × Grok / Claude / Codex

本文件說明如何在 **Grok Build**、**Claude Code（與相容環境）**、**OpenAI Codex** 呼叫 **MCP** 與 **Skill**。  
伺服器名一律：`l1r-viewer`（唯讀）。

---

## 0. 共用前置（三套 AI 都要）

在 **L1R-Viewer 倉庫根目錄**：

```powershell
cd C:\Users\EnosLo\Desktop\00-Workspace\000-Ongoing-Projects\006-Tools\L1R-Viewer
dotnet build L1R-Viewer.slnx -c Release
python -m pip install -r mcp\requirements.txt
# 可選煙測
python .\mcp\smoke_test.py --map-id 53 --id 167
```

| 變數 / 路徑 | 說明 |
|---|---|
| `L1R_CLIENT` | 客戶端根目錄（含 `map\`、`Tile.idx`） |
| Repo | `…\006-Tools\L1R-Viewer` |
| MCP 入口 | `mcp\server.py` |
| Python | 建議用已裝 `mcp` 的解譯器（例：hermes venv 或系統 `python`） |

**建議 absolute path 範例（請依本機修改）：**

```text
REPO   = C:\Users\EnosLo\Desktop\00-Workspace\000-Ongoing-Projects\006-Tools\L1R-Viewer
PYTHON = C:\Users\EnosLo\AppData\Local\hermes\hermes-agent\venv\Scripts\python.exe
SERVER = %REPO%\mcp\server.py
```

---

## 1. Grok Build

### 1.1 註冊 MCP

**全域** `~/.grok/config.toml`：

```toml
[mcp_servers.l1r-viewer]
command = 'C:\Users\EnosLo\AppData\Local\hermes\hermes-agent\venv\Scripts\python.exe'
args = [
  'C:\Users\EnosLo\Desktop\00-Workspace\000-Ongoing-Projects\006-Tools\L1R-Viewer\mcp\server.py',
]
enabled = true
startup_timeout_sec = 60
tool_timeout_sec = 600
```

**專案**（二擇一或並存）：

- `L1R-Viewer/.mcp.json`（Claude/Cursor 相容格式）
- `L1R-Viewer/.grok/config.toml`

CLI：

```powershell
grok mcp list
# 若需手動加（路徑請改）：
# grok mcp add l1r-viewer -- python mcp/server.py
```

改完設定後 **重開 Grok 工作階段** 或重新載入 MCP。

### 1.2 Skill

| 範圍 | 路徑 |
|---|---|
| User | `~/.grok/skills/l1r-viewer/SKILL.md` |
| Project | `L1R-Viewer/.grok/skills/l1r-viewer/SKILL.md` |

**呼叫方式：**

- Slash：`/l1r-viewer`
- 選單：`/skills l1r-viewer`
- 自然語言：提到「地圖讀取 / S32 / 傳點 / sprite / L1R-Viewer」會自動匹配 description

### 1.3 使用示例（對話）

```text
用 L1R-Viewer 驗證 client：D:\...\LineageRemastered-...
列出 map 53 的傳送點
把 map 53 渲成 PNG 到 C:\Temp\map-53.png，max_size 1024
查 sprite id 167 有幾個變體
```

Agent 應優先呼叫 MCP：`validate_client` → `list_portals` / `render_map` / `sprite_info`。

---

## 2. Claude Code

### 2.1 註冊 MCP

#### A) 專案根 `.mcp.json`（建議，repo 已提供）

```json
{
  "mcpServers": {
    "l1r-viewer": {
      "command": "python",
      "args": ["mcp/server.py"],
      "cwd": ".",
      "env": { "PYTHONUTF8": "1" }
    }
  }
}
```

在 **L1R-Viewer 目錄** 啟動 Claude Code，會自動載入。

#### B) 使用者全域 `~/.claude.json` → `mcpServers`

```json
{
  "mcpServers": {
    "l1r-viewer": {
      "command": "C:/Users/EnosLo/AppData/Local/hermes/hermes-agent/venv/Scripts/python.exe",
      "args": [
        "C:/Users/EnosLo/Desktop/00-Workspace/000-Ongoing-Projects/006-Tools/L1R-Viewer/mcp/server.py"
      ],
      "env": { "PYTHONUTF8": "1" }
    }
  }
}
```

#### C) CLI（若版本支援）

```powershell
claude mcp add l1r-viewer -- python mcp/server.py
# 或
claude mcp list
```

### 2.2 Skill / 指令

Claude Code 會掃：

| 位置 | 用途 |
|---|---|
| `L1R-Viewer/.claude/skills/l1r-viewer/SKILL.md` | 專案 skill |
| `L1R-Viewer/.grok/skills/l1r-viewer/SKILL.md` | Grok 格式（Grok 讀；Claude 以 `.claude` 為準） |
| `~/.claude/skills/l1r-viewer/SKILL.md` | 使用者全域 |

**呼叫：**

- `/l1r-viewer`（若 skill 名註冊為 slash）
- 或：「用 L1R-Viewer MCP 檢查 client 並 render map 53」

可選：在 `CLAUDE.md` 加一行：

```markdown
地圖/客戶端資產請用 MCP `l1r-viewer` 或 skill l1r-viewer；唯讀。
```

### 2.3 使用示例

```text
Call validate_client on <absolute client path>
Then list_portals for map_id 53
Then render_map to C:\Temp\map-53.png with max_size 1024
```

---

## 3. OpenAI Codex（CLI / Desktop）

### 3.1 註冊 MCP

編輯 **`~/.codex/config.toml`**：

```toml
[mcp_servers.l1r-viewer]
command = 'C:\Users\EnosLo\AppData\Local\hermes\hermes-agent\venv\Scripts\python.exe'
args = [
  'C:\Users\EnosLo\Desktop\00-Workspace\000-Ongoing-Projects\006-Tools\L1R-Viewer\mcp\server.py',
]
startup_timeout_sec = 60
```

（若需 env，依 Codex 版本可加 `[mcp_servers.l1r-viewer.env]`。）

重啟 Codex / 新開 thread。確認：

```powershell
# 依本機 Codex CLI 指令為準
codex mcp list
```

### 3.2 Skill

Codex 對 Grok/Claude 的 `SKILL.md` **不一定**自動掃描。建議：

1. **MCP 為主**（工具會直接進 session）
2. 在 Codex 的 **AGENTS.md / project instructions** 貼上精簡規則（見下方 §5）
3. 或複製 skill 內容到 Codex 可讀的 instruction 檔

專案已提供：`docs/ai-snippets/CODEX-AGENTS-snippet.md` 可整段貼上。

### 3.3 使用示例

```text
Use MCP l1r-viewer:
1) validate_client(client_path=...)
2) list_maps
3) render_map(map_id=53, output_path=..., max_size=1024)
Do not attempt write tools.
```

---

## 4. 工具速查（三套 AI 相同）

| 需求 | MCP tool |
|---|---|
| 健康 | `l1r_health` |
| 驗證 client | `validate_client` |
| 地圖清單 | `list_maps` |
| 地圖資訊 | `map_info` |
| 整圖 PNG | `render_map` |
| 傳點 | `list_portals` |
| 通行 | `export_passability` |
| 區域檔 | `list_regions` |
| 精靈 | `sprite_info` / `search_sprite_entries` / `export_sprite_frames` |

**禁止：** fix / import / delete / 任何寫回（MCP 未暴露）。

完整參數：`.grok/skills/l1r-viewer/references/tools.md` · `docs/mcp.md`。

---

## 5. 跨 AI 共用提示詞（可貼進任何 system / AGENTS.md）

```markdown
## L1R-Viewer (read-only)

- Prefer MCP server **l1r-viewer** for offline Lineage Remastered client assets.
- Always use absolute client_path and output paths.
- Client root must contain map\ and Tile.idx.
- Workflow: validate_client → then list_maps / map_info / list_portals / render_map / sprite_*.
- Large maps: set max_size (1024–2048).
- Never use write/edit tools; MCP is read-only.
- Fallback CLI (repo L1R-Viewer): l1r.ps1 doctor|map|… after dotnet build.
- Env: L1R_CLIENT optional.
```

---

## 6. 故障排除

| 現象 | 處理 |
|---|---|
| MCP 起不來 | 確認 `python -c "import mcp"`；路徑有空白要用正確 quoting |
| 找不到 MapViewer | `dotnet build`；exe 為 `L1R-MapViewer.exe` 或舊名 `L1MapViewerCore.exe` |
| validate 失敗 | client 不是根目錄（缺 `map` / `Tile.idx`） |
| Claude 看不到 skill | 放在 `.claude/skills/l1r-viewer/SKILL.md` 並在該 repo 開 session |
| Codex 無 skill | 用 MCP + 貼 §5 提示詞 |
| 渲染很久 | 降 `max_size`；大地圖本就耗時 |

---

## 7. 驗證清單

- [ ] `dotnet build` 成功  
- [ ] `python mcp/smoke_test.py` 通過（有真實 client 時）  
- [ ] Grok：`grok mcp list` 含 `l1r-viewer`；`/l1r-viewer` 可用  
- [ ] Claude：專案內能列到 MCP tools  
- [ ] Codex：`config.toml` 有 `[mcp_servers.l1r-viewer]` 且 thread 能 call tool  
