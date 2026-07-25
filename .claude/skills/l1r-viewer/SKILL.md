---
name: l1r-viewer
description: >
  Use L1R-Viewer to read offline Lineage Remastered client assets (maps, sprites,
  pak/idx, portals, passability). Prefer MCP tools `l1r-viewer` when connected;
  otherwise use CLI via l1r.ps1 / pakviewer-cli. Triggers: L1R-Viewer, lineage map,
  S32, render map, list portals, sprite 167, Tile.idx, client doctor, validate_client,
  /l1r-viewer, 天堂地圖, 地圖讀取, 傳點, 精靈匯出.
metadata:
  short-description: "L1R-Viewer: maps/sprites/MCP/CLI (read-only)"
---

# L1R-Viewer skill

Read-only toolkit for **offline** Lineage Remastered client folders
(`map\`, `Tile.idx`, `sprite*.idx`). Never crack live clients or write via MCP.

## Repo & paths

- Default repo:
  `C:\Users\EnosLo\Desktop\00-Workspace\000-Ongoing-Projects\006-Tools\L1R-Viewer`
- Client root example:
  `…\LineageR-2606262601\001-CLIENT\LineageRemastered-2606262601`
- Env override: `L1R_CLIENT`
- Docs: `docs/GETTING-STARTED.md`, `docs/mcp.md`, `docs/OPERATOR-MANUAL.md`

## Multi-AI setup (Grok / Claude / Codex)

Full install & usage: **`docs/AI-INTEGRATION.md`** in the L1R-Viewer repo.

| AI | MCP | Skill / instructions |
|---|---|---|
| **Grok** | `~/.grok/config.toml` `[mcp_servers.l1r-viewer]` | `/l1r-viewer` · `.grok/skills/l1r-viewer` |
| **Claude Code** | `.mcp.json` or `~/.claude.json` `mcpServers` | `.claude/skills/l1r-viewer` |
| **Codex** | `~/.codex/config.toml` `[mcp_servers.l1r-viewer]` | Paste `docs/ai-snippets/CODEX-AGENTS-snippet.md` into AGENTS.md |

## Prefer MCP (when server `l1r-viewer` is available)

Use `search_tool` / `use_tool` with server tools. All tools are **read-only**.

| Need | Tool | Notes |
|---|---|---|
| Health | `l1r_health` | CLI backends ready |
| Client valid? | `validate_client` | needs `map\` + `*.idx` |
| List maps | `list_maps` | `client_path` |
| Map meta | `map_info` | `client_path`, `map_id` |
| Full map PNG | `render_map` | `output_path`, `max_size` default 2048 |
| Portals L7 | `list_portals` | `map_id` (e.g. 53 has data) |
| Passability L3 | `export_passability` | optional `output_path` |
| Region files | `list_regions` | Market/TeleportOk/fishing |
| Sprite meta | `sprite_info` | `sprite_id` e.g. 167 |
| Search sprites | `search_sprite_entries` | query string |
| Export frames | `export_sprite_frames` | absolute `output_directory` |

### MCP rules

1. Always pass **absolute** `client_path` / `output_path`.
2. Large maps: set `max_size` (1024–2048).
3. **Never** call fix/import/delete — not exposed; do not invent write tools.
4. If MCP missing: fall back to CLI below (build first if needed).

## CLI fallback

```powershell
cd <L1R-Viewer-repo>
dotnet build L1R-Viewer.slnx -c Release

.\l1r.ps1 doctor "<client>" --json
.\l1r.ps1 map render  "<client>\map\53" ".\tests\out\map-53.png"
.\l1r.ps1 map portals "<client>\map\53\7fff7ffe.s32" ".\tests\out\p.json"

.\src\L1R.Cli\bin\Release\net10.0\pakviewer-cli.exe spr info "<client>" 167 --json
.\src\L1R.Cli\bin\Release\net10.0\pakviewer-cli.exe map regions "<client>\map\53" --json
```

Bad client → exit ≠ 0 and text `錯誤` / `原因` / `建議`.

## Operator GUI (when user asks for UI)

```powershell
.\Launch-L1R-Viewer.ps1
# Shell: pick client → doctor → open Map / Pak
```

MapViewer export toolbar: PNG / portals JSON / passability (read-only OK).

## Workflow checklist (agent)

1. Resolve `client_path` (env `L1R_CLIENT` or user path).
2. `validate_client` / `doctor` before heavy work.
3. Use the smallest tool that answers the question (info before full render).
4. Write outputs under `tests/out/` or user Documents `L1R-Viewer\exports` — never commit large PNGs.
5. Summarize results with paths and counts (portals, map size, sprite variants).

## Safety

- Offline static files only.
- Do not touch live `Lin.exe`, anti-cheat, or network.
- Do not enable edit mode unless user explicitly asks; MCP stays read-only always.

See `references/tools.md` for full tool parameter notes.
