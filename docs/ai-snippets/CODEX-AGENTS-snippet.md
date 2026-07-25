# Paste into Codex project AGENTS.md or custom instructions

## L1R-Viewer (read-only MCP)

- MCP server name: **l1r-viewer**
- Repo: `C:\Users\EnosLo\Desktop\00-Workspace\000-Ongoing-Projects\006-Tools\L1R-Viewer`
- Prefer tools: `validate_client`, `list_maps`, `map_info`, `render_map`, `list_portals`, `export_passability`, `list_regions`, `sprite_info`, `search_sprite_entries`, `export_sprite_frames`, `l1r_health`
- Always absolute paths for `client_path` / `output_path`
- Client root = folder with `map\` + `Tile.idx` (not the `map` subfolder alone)
- Large maps: `render_map(..., max_size=1024)` or `2048`
- **Never** attempt write/edit (fix, import, delete) — not available on MCP
- Fallback: `l1r.ps1 doctor|map render|…` after `dotnet build L1R-Viewer.slnx -c Release`
- Optional env: `L1R_CLIENT`
