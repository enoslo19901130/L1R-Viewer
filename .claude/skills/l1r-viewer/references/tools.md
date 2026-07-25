# L1R-Viewer MCP tools (detail)

Server name: **`l1r-viewer`**  
Entry: `mcp/server.py` (stdio FastMCP)

## Common parameters

- `client_path`: absolute path to client **root** (has `map\` and `Tile.idx`).
- `map_id`: integer or string folder name under `map\`.
- `output_path` / `output_directory`: absolute paths preferred.

## Tools

### l1r_health
No args. Returns `{ ok, cli_ready, map_cli_ready, assembly_version, read_only }`.

### validate_client
- `client_path` (str)
- Returns `{ ok, missing[], error, reason, suggestion, map_count, has_tile_idx, ... }`
- Aligns with CLI `doctor --json`.

### list_maps
- `client_path`
- Returns `{ maps: [{ map_id, s32_count }], count }`

### map_info
- `client_path`, `map_id`
- Segment count + sample layer stats

### render_map
- `client_path`, `map_id`, `output_path`, `max_size` (default 2048)
- Headless Skia full-map PNG

### list_portals
- `client_path`, `map_id`
- Layer7 across all s32; UTF-8 BOM JSON from backend handled

### export_passability
- `client_path`, `map_id`, optional `output_path`

### list_regions
- `client_path`, `map_id`
- MarketRegion / TeleportOkRegion / fishingRegion file names

### sprite_info
- `client_path`, `sprite_id`
- Variants e.g. `167-0.spx` … frames metadata

### search_sprite_entries
- `client_path`, `query`, `limit`

### export_sprite_frames
- `client_path`, `sprite_id`, `output_directory`, optional `frame_index`

### create_sprite_sheet / create_sprite_range_sheet
- Contact sheets (Pillow optional)

## Forbidden on MCP

No: fix, trim-s32, clear-l8, import-fs32, pak add/delete, any write-back.
