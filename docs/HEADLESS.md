# Headless / agent usage

## Prefer MCP

For agents, use the **l1r-viewer** MCP server (`mcp/server.py`). It is read-only and returns structured dicts.

```powershell
python -m pip install -r mcp\requirements.txt
python mcp\smoke_test.py
```

## Prefer CLI for scripts

```powershell
dotnet build L1R-Viewer.slnx -c Release
.\l1r.ps1 map render  <client>\map\53  .\tests\out\m.png --max-size 2048
.\l1r.ps1 map portals <client>\map\53\7fff7ffe.s32 .\tests\out\p.json
.\src\L1R.Cli\bin\Release\net10.0\pakviewer-cli.exe spr info <client> 167 --json
```

## Notes

1. **MapViewer is WinExe** — use `l1r.ps1` or Python `subprocess.run` (not bare PowerShell `&` without wait).
2. **Large maps** — always pass `--max-size` for render.
3. **No writes via MCP** — fix/import/delete stay CLI-only behind `--enable-edit`.
4. Legacy `LineageTool` headless is **not required** for agent workflows anymore.
