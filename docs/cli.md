# L1R-Viewer CLI Reference

## Unified launcher: `l1r.ps1`

```powershell
.\l1r.ps1 help
.\l1r.ps1 <group> <command> [args]
```

| Group | Backend | Purpose |
|---|---|---|
| `map` | `L1MapViewerCore.exe -cli` | S32 maps: info, render, portals, passability |
| `doctor` | `pakviewer-cli doctor` | Client health (`map\` + `*.idx` / Tile.idx) |
| `pak` | `pakviewer-cli.exe` | IDX/PAK archives |
| `spr` | `pakviewer-cli.exe` | SPR/SPX sprites |
| `til` | `pakviewer-cli.exe` | TIL tiles |
| `dat` | `pakviewer-cli.exe` | Lineage M DAT |
| `xml` | `pakviewer-cli.exe` | XML crypto helpers |
| `version` | `pakviewer-cli.exe` | Version string |

### Doctor (client validation)

```powershell
.\l1r.ps1 doctor <client-folder>
.\l1r.ps1 doctor <client-folder> --json
.\l1r.ps1 doctor <client-folder> --remember   # write %AppData%\L1R-Viewer\settings.json
# direct:
pakviewer-cli doctor <client-folder> [--json] [--remember]
```

On failure: prints `錯誤` / `原因` / `建議` and exits with code **2**.

### Map aliases

| Launcher | Backend verb |
|---|---|
| `l1r map render <mapDir> <out.png> [--max-size N]` | `export-fullmap` |
| `l1r map passability <mapDir> <out.txt>` | `export-passability` |
| `l1r map portals <s32> <out.json>` | `export` (includes layer7) |
| `l1r map list-maps <client>` | `list-maps` |
| `l1r map info <s32>` | `info` |

Other map verbs are passed through (`export-tiles`, `render-adjacent`, `batch-export`, …).

```powershell
# region side-car files (Market / TeleportOk / fishing)
pakviewer-cli map regions <client>\map\53 --json
```

### Sprite (direct CLI)

```powershell
pakviewer-cli spr info <client> <id|name> [--json]
pakviewer-cli spr export <client> <id|name> -o <dir> [--frame N] [--json]
pakviewer-cli spr search <client> <query> [--limit N] [--json]
```

Numeric `id` lists all `{id}-*.spx` variants (Remaster).

### Write gates

Map write verbs require app-level flag:

```powershell
L1MapViewerCore.exe --enable-edit -cli fix ...
```

Pak write verbs:

```powershell
pakviewer-cli pak add|delete|create ... --enable-edit
```

Without `--enable-edit`, write commands exit with code `2`.

## Direct backends

```text
src/L1R.MapViewer/bin/Release/net10.0-windows/L1MapViewerCore.exe
src/L1R.Cli/bin/Release/net10.0/pakviewer-cli.exe
```

MapViewer is a **WinExe**: prefer `Start-Process -Wait` (as `l1r.ps1` does) or Python `subprocess` when capturing output.
