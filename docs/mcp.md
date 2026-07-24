# L1R-Viewer MCP

Server name: **`l1r-viewer`**  
Entry: `mcp/server.py`  
Config sample: `.mcp.json`

## Install / run smoke

```powershell
# requires: pip install mcp  (and Pillow optional for contact sheets)
python .\mcp\smoke_test.py --map-id 53 --id 167
```

## Tools (all read-only)

| Tool | Purpose |
|---|---|
| `l1r_health` | Backend readiness |
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
