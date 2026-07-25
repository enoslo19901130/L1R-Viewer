# L1R-Viewer MCP

Server name: **`l1r-viewer`**  
Entry: `mcp/server.py`  

> **跨 AI（Grok / Claude / Codex）完整設定與對話範例：**  
> **[`docs/AI-INTEGRATION.md`](AI-INTEGRATION.md)**

## Registration（摘要）

| AI | MCP 設定位置 | Skill / 提示 |
|---|---|---|
| **Grok** | `~/.grok/config.toml` · 專案 `.mcp.json` | `/l1r-viewer` |
| **Claude Code** | `.mcp.json` · `~/.claude.json` | `.claude/skills/l1r-viewer` |
| **Codex** | `~/.codex/config.toml` | 貼 `docs/ai-snippets/CODEX-AGENTS-snippet.md` |

```toml
# Grok / Codex 風格
[mcp_servers.l1r-viewer]
command = 'C:\Users\EnosLo\AppData\Local\hermes\hermes-agent\venv\Scripts\python.exe'
args = [
  'C:\Users\EnosLo\Desktop\00-Workspace\000-Ongoing-Projects\006-Tools\L1R-Viewer\mcp\server.py',
]
enabled = true
startup_timeout_sec = 60
tool_timeout_sec = 600
```

改設定後重開對應 AI。需要：`pip install mcp`（Pillow 可選）。

## Install / run smoke

```powershell
cd …\L1R-Viewer
dotnet build L1R-Viewer.slnx -c Release
python -m pip install -r mcp\requirements.txt
python .\mcp\smoke_test.py --map-id 53 --id 167
```

## Tools (all read-only)

| Tool | Purpose |
|---|---|
| `l1r_health` | Backend readiness |
| `validate_client` | Client folder health (aligns with CLI `doctor --json`) |
| `sprite_info` | SPX/SPR variants + frame metadata |
| `search_sprite_entries` | Search sprite*.idx names |
| `export_sprite_frames` | Export PNG frames |
| `create_sprite_sheet` | Contact sheet (Pillow) or per-frame folder |
| `create_sprite_range_sheet` | Inclusive ID range sheet |
| `map_info` | Segment count + sample layer stats |
| `render_map` | Full map PNG via headless Skia exporter |
| `list_portals` | Layer7 teleports for a mapId |
| `export_passability` | Layer3 passability dump |
| `list_regions` | Market / TeleportOk / fishing region files |
| `list_maps` | List map folders with S32 counts |

## Not exposed

Any write/edit surface is **blocked** on MCP:

- `fix`, `trim-s32`, `clear-l8`, `import-fs32`
- `pak add/delete/create`, PakBrowser replace/delete

## Agent tips

- Always pass absolute `client_path` and `output_path`.
- Large maps: set `render_map.max_size` (default 2048).
- Portals JSON from MapViewer is UTF-8 **with BOM** (server handles `utf-8-sig`).
