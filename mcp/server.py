"""
L1R-Viewer MCP server — read-only agent tools for Lineage client assets.

Backends:
  - L1R.Cli (pakviewer-cli): version / spr / til / pak
  - L1R.MapViewer (-cli): map render / portals / passability / info / list-maps

No write/edit tools are exposed (fix/trim/import/clear-* stay CLI-only with --enable-edit).
"""
from __future__ import annotations

import json
import os
import re
import subprocess
import tempfile
from pathlib import Path
from typing import Any

from mcp.server.fastmcp import FastMCP

ROOT = Path(__file__).resolve().parents[1]

# Build outputs (Release preferred)
_CLI_CANDIDATES = [
    ROOT / "src" / "L1R.Cli" / "bin" / "Release" / "net10.0" / "pakviewer-cli.exe",
    ROOT / "src" / "L1R.Cli" / "bin" / "Debug" / "net10.0" / "pakviewer-cli.exe",
]
_MAP_CANDIDATES = [
    ROOT / "src" / "L1R.MapViewer" / "bin" / "Release" / "net10.0-windows" / "L1R-MapViewer.exe",
    ROOT / "src" / "L1R.MapViewer" / "bin" / "Debug" / "net10.0-windows" / "L1R-MapViewer.exe",
    ROOT / "src" / "L1R.MapViewer" / "bin" / "Release" / "net10.0-windows" / "L1MapViewerCore.exe",
    ROOT / "src" / "L1R.MapViewer" / "bin" / "Debug" / "net10.0-windows" / "L1MapViewerCore.exe",
    ROOT / "src" / "L1R.MapViewer" / "bin" / "Release" / "net10.0" / "L1R-MapViewer.exe",
]

mcp = FastMCP(
    "l1r-viewer",
    instructions=(
        "Read-only access to Lineage Remastered offline client assets via L1R-Viewer. "
        "Always use absolute client_path and output paths. "
        "Tools: l1r_health, validate_client, map_info, render_map, list_portals, "
        "export_passability, list_regions, list_maps, sprite_info, search_sprite_entries, "
        "export_sprite_frames, create_sprite_sheet, create_sprite_range_sheet. "
        "Never attempt write/edit (fix/import/delete) through MCP — not exposed."
    ),
)


def _find_exe(candidates: list[Path]) -> Path | None:
    for p in candidates:
        if p.is_file():
            return p
    return None


def _ensure_cli() -> Path:
    exe = _find_exe(_CLI_CANDIDATES)
    if exe:
        return exe
    completed = subprocess.run(
        ["dotnet", "build", str(ROOT / "src" / "L1R.Cli" / "PakViewer.Cli.csproj"), "-c", "Release"],
        cwd=str(ROOT),
        capture_output=True,
        text=True,
        encoding="utf-8",
        timeout=180,
        check=False,
    )
    if completed.returncode != 0:
        raise RuntimeError(
            f"L1R.Cli build failed ({completed.returncode}): "
            f"{completed.stderr or completed.stdout}"
        )
    exe = _find_exe(_CLI_CANDIDATES)
    if not exe:
        raise RuntimeError("L1R.Cli build succeeded but exe not found")
    return exe


def _ensure_map_cli() -> Path:
    exe = _find_exe(_MAP_CANDIDATES)
    if exe:
        return exe
    completed = subprocess.run(
        [
            "dotnet",
            "build",
            str(ROOT / "src" / "L1R.MapViewer" / "L1MapViewerCore.csproj"),
            "-c",
            "Release",
        ],
        cwd=str(ROOT),
        capture_output=True,
        text=True,
        encoding="utf-8",
        timeout=300,
        check=False,
    )
    if completed.returncode != 0:
        raise RuntimeError(
            f"L1R.MapViewer build failed ({completed.returncode}): "
            f"{completed.stderr or completed.stdout}"
        )
    exe = _find_exe(_MAP_CANDIDATES)
    if not exe:
        raise RuntimeError("L1R.MapViewer build succeeded but exe not found")
    return exe


def _run(
    exe: Path,
    args: list[str],
    *,
    timeout: int = 300,
    cwd: Path | None = None,
) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [str(exe), *args],
        cwd=str(cwd or ROOT),
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
        timeout=timeout,
        check=False,
    )


def _parse_json_line(stdout: str) -> dict[str, Any] | None:
    """Parse last non-empty line that looks like JSON object."""
    for line in reversed(stdout.splitlines()):
        line = line.strip()
        if line.startswith("{") and line.endswith("}"):
            try:
                return json.loads(line)
            except json.JSONDecodeError:
                continue
    return None


def _map_dir(client_path: str, map_id: int | str) -> Path:
    client = Path(client_path).resolve()
    mid = str(map_id)
    d = client / "map" / mid
    if not d.is_dir():
        raise FileNotFoundError(f"Map folder not found: {d}")
    return d


def _first_s32(map_dir: Path) -> Path:
    files = sorted(map_dir.glob("*.s32"))
    if not files:
        raise FileNotFoundError(f"No .s32 files in {map_dir}")
    return files[0]


# ---------------------------------------------------------------------------
# Health / version
# ---------------------------------------------------------------------------


@mcp.tool()
def l1r_health() -> dict[str, Any]:
    """Confirm L1R-Viewer CLI backends are built and ready (read-only)."""
    cli = _ensure_cli()
    map_cli = _ensure_map_cli()
    completed = _run(cli, ["version"], timeout=30)
    version_line = (completed.stdout or "").strip().splitlines()
    version = version_line[0] if version_line else "unknown"
    return {
        "ok": True,
        "cli_ready": True,
        "map_cli_ready": True,
        "cli_path": str(cli),
        "map_cli_path": str(map_cli),
        "assembly_version": version,
        "server": "l1r-viewer",
        "read_only": True,
    }


@mcp.tool()
def validate_client(client_path: str) -> dict[str, Any]:
    """
    Validate a Lineage client root folder (read-only).
    Checks for map\\ and *.idx (prefers Tile.idx). Agrees with CLI `doctor --json`.
    """
    cli = _ensure_cli()
    path = str(Path(client_path).expanduser())
    # Do not require resolve() when path missing — doctor handles that
    completed = _run(cli, ["doctor", path, "--json"], timeout=60)
    result = _parse_json_line(completed.stdout) or _parse_json_line(completed.stderr or "")
    if result is None:
        # Synthesize from exit code if backend printed non-JSON
        text = ((completed.stdout or "") + "\n" + (completed.stderr or "")).strip()
        return {
            "ok": completed.returncode == 0,
            "command": "validate_client",
            "path": path,
            "error": None if completed.returncode == 0 else "doctor failed",
            "reason": text[:500] or f"exit {completed.returncode}",
            "suggestion": "請確認路徑含 map\\ 與 Tile.idx",
            "exit_code": completed.returncode,
        }
    result["command"] = result.get("command") or "validate_client"
    result["exit_code"] = completed.returncode
    return result


# ---------------------------------------------------------------------------
# Sprite tools (via L1R.Cli)
# ---------------------------------------------------------------------------


@mcp.tool()
def sprite_info(client_path: str, sprite_id: int) -> dict[str, Any]:
    """Return SPX/SPR variants and frame metadata for one client sprite ID."""
    cli = _ensure_cli()
    client = str(Path(client_path).resolve())
    completed = _run(cli, ["spr", "info", client, str(sprite_id), "--json"], timeout=120)
    result = _parse_json_line(completed.stdout)
    if result is None:
        raise RuntimeError(
            f"sprite_info failed ({completed.returncode}): "
            f"{completed.stderr or completed.stdout}"
        )
    if not result.get("ok", False):
        raise RuntimeError(result.get("error") or "sprite_info failed")
    return result


@mcp.tool()
def search_sprite_entries(
    client_path: str,
    query: str,
    limit: int = 100,
) -> dict[str, Any]:
    """Search sprite*.idx entry names without opening a GUI."""
    cli = _ensure_cli()
    client = str(Path(client_path).resolve())
    completed = _run(
        cli,
        ["spr", "search", client, query, "--limit", str(limit), "--json"],
        timeout=180,
    )
    result = _parse_json_line(completed.stdout)
    if result is None:
        raise RuntimeError(
            f"search failed ({completed.returncode}): {completed.stderr or completed.stdout}"
        )
    return result


@mcp.tool()
def export_sprite_frames(
    client_path: str,
    sprite_id: int,
    output_directory: str,
    frame_index: int | None = None,
) -> dict[str, Any]:
    """Export sprite frames as PNG (first variant for the given ID)."""
    cli = _ensure_cli()
    client = str(Path(client_path).resolve())
    out = str(Path(output_directory).resolve())
    Path(out).mkdir(parents=True, exist_ok=True)
    args = ["spr", "export", client, str(sprite_id), "-o", out, "--json"]
    if frame_index is not None:
        args.extend(["--frame", str(frame_index)])
    completed = _run(cli, args, timeout=180)
    result = _parse_json_line(completed.stdout)
    if result is None:
        # export still writes human-readable lines; synthesize from files
        files = sorted(str(p) for p in Path(out).glob("*.png"))
        if not files:
            raise RuntimeError(
                f"export failed ({completed.returncode}): "
                f"{completed.stderr or completed.stdout}"
            )
        return {
            "ok": True,
            "command": "sprite.export",
            "sprite_id": sprite_id,
            "files": files,
            "output_directory": out,
            "exported_count": len(files),
        }
    return result


@mcp.tool()
def create_sprite_sheet(
    client_path: str,
    sprite_ids: list[int],
    output_path: str,
    columns: int = 6,
    frame_index: int = 0,
) -> dict[str, Any]:
    """
    Create a simple contact sheet by exporting frame_index of each sprite ID
    and stitching into a grid PNG (Pillow if available, else returns per-frame files).
    """
    if not sprite_ids:
        raise ValueError("sprite_ids cannot be empty")
    out = Path(output_path).resolve()
    out.parent.mkdir(parents=True, exist_ok=True)
    tmp = Path(tempfile.mkdtemp(prefix="l1r-sheet-"))
    cells: list[dict[str, Any]] = []
    images: list[Any] = []

    try:
        from PIL import Image, ImageDraw, ImageFont  # type: ignore
    except ImportError:
        # Fallback: export each sprite frame into sibling folder
        folder = out.with_suffix("")
        folder.mkdir(parents=True, exist_ok=True)
        for sid in sprite_ids:
            sub = folder / str(sid)
            sub.mkdir(exist_ok=True)
            info = export_sprite_frames(client_path, sid, str(sub), frame_index=frame_index)
            cells.append({"sprite_id": sid, **info})
        return {
            "ok": True,
            "command": "sprite.sheet",
            "note": "Pillow not installed; exported individual frames instead of stitched sheet",
            "output_directory": str(folder),
            "cells": cells,
            "sprite_ids": sprite_ids,
        }

    for sid in sprite_ids:
        sub = tmp / str(sid)
        sub.mkdir()
        try:
            info = export_sprite_frames(client_path, sid, str(sub), frame_index=frame_index)
            files = info.get("files") or sorted(str(p) for p in sub.glob("*.png"))
            if files:
                img = Image.open(files[0]).convert("RGBA")
                images.append((sid, img))
                cells.append({"sprite_id": sid, "file": files[0], "ok": True})
            else:
                images.append((sid, None))
                cells.append({"sprite_id": sid, "ok": False, "error": "no frames"})
        except Exception as exc:  # noqa: BLE001 — collect per-cell errors
            images.append((sid, None))
            cells.append({"sprite_id": sid, "ok": False, "error": str(exc)})

    cell_w, cell_h = 176, 196
    preview = 160
    cols = max(1, columns)
    rows = (len(images) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell_w, rows * cell_h), (28, 28, 30, 255))
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("consola.ttf", 12)
    except OSError:
        font = ImageFont.load_default()

    for index, (sid, img) in enumerate(images):
        x = (index % cols) * cell_w
        y = (index // cols) * cell_h
        draw.rectangle([x, y, x + cell_w - 1, y + cell_h - 1], outline=(72, 72, 72))
        draw.text((x + 4, y + 4), str(sid), fill=(255, 255, 255), font=font)
        if img is not None:
            img.thumbnail((preview, preview))
            ox = x + (cell_w - img.width) // 2
            oy = y + 20 + (preview - img.height) // 2
            sheet.paste(img, (ox, oy), img)

    sheet.save(out)
    return {
        "ok": True,
        "command": "sprite.sheet",
        "output": str(out),
        "columns": cols,
        "frame_index": frame_index,
        "sprite_ids": sprite_ids,
        "cells": cells,
    }


@mcp.tool()
def create_sprite_range_sheet(
    client_path: str,
    start_id: int,
    end_id: int,
    output_path: str,
    columns: int = 6,
    frame_index: int = 0,
) -> dict[str, Any]:
    """Create a contact sheet for an inclusive sprite-ID range."""
    if end_id < start_id:
        raise ValueError("end_id must be >= start_id")
    ids = list(range(start_id, end_id + 1))
    if len(ids) > 500:
        raise ValueError("range too large (max 500)")
    return create_sprite_sheet(client_path, ids, output_path, columns, frame_index)


# ---------------------------------------------------------------------------
# Map tools (via L1R.MapViewer -cli)
# ---------------------------------------------------------------------------


@mcp.tool()
def map_info(client_path: str, map_id: int) -> dict[str, Any]:
    """Return S32 segment counts and layer stats for a map ID."""
    map_cli = _ensure_map_cli()
    map_dir = _map_dir(client_path, map_id)
    s32_files = sorted(map_dir.glob("*.s32"))
    if not s32_files:
        raise FileNotFoundError(f"No .s32 in {map_dir}")

    # Aggregate from first few segments + total count (full scan of all can be slow)
    sample = s32_files[0]
    completed = _run(map_cli, ["-cli", "info", str(sample)], timeout=60)
    text = (completed.stdout or "") + "\n" + (completed.stderr or "")

    layers: dict[str, Any] = {}
    for m in re.finditer(r"Layer\s*(\d+)\s*[:：]\s*(\d+)", text, re.I):
        layers[f"layer{m.group(1)}"] = int(m.group(2))

    return {
        "ok": True,
        "command": "map.info",
        "map_id": str(map_id),
        "map_dir": str(map_dir),
        "segment_count": len(s32_files),
        "sample_s32": sample.name,
        "sample_layers": layers,
        "cli_output": text.strip()[:2000],
    }


@mcp.tool()
def render_map(
    client_path: str,
    map_id: int,
    output_path: str,
    max_size: int = 2048,
) -> dict[str, Any]:
    """Render an entire map to PNG (headless Skia path). max_size caps the longest edge."""
    map_cli = _ensure_map_cli()
    map_dir = _map_dir(client_path, map_id)
    out = Path(output_path).resolve()
    out.parent.mkdir(parents=True, exist_ok=True)

    args = ["-cli", "export-fullmap", str(map_dir), str(out)]
    if max_size and max_size > 0:
        args.extend(["--max-size", str(max_size)])

    completed = _run(map_cli, args, timeout=600)
    text = ((completed.stdout or "") + "\n" + (completed.stderr or "")).strip()

    if completed.returncode != 0 or not out.is_file() or out.stat().st_size == 0:
        raise RuntimeError(
            f"render_map failed ({completed.returncode}): {text[-2000:]}"
        )

    return {
        "ok": True,
        "command": "map.render",
        "map_id": str(map_id),
        "output_path": str(out),
        "size_bytes": out.stat().st_size,
        "max_size": max_size,
        "log": text[-1500:],
    }


@mcp.tool()
def list_portals(client_path: str, map_id: int) -> dict[str, Any]:
    """List Layer7 portals (teleports) for all S32 segments in a map."""
    map_cli = _ensure_map_cli()
    map_dir = _map_dir(client_path, map_id)
    s32_files = sorted(map_dir.glob("*.s32"))
    if not s32_files:
        raise FileNotFoundError(f"No .s32 in {map_dir}")

    portals: list[dict[str, Any]] = []
    errors: list[str] = []

    with tempfile.TemporaryDirectory(prefix="l1r-portals-") as tmp:
        tmp_path = Path(tmp)
        for s32 in s32_files:
            out_json = tmp_path / f"{s32.stem}.json"
            completed = _run(
                map_cli,
                ["-cli", "export", str(s32), str(out_json)],
                timeout=60,
            )
            if completed.returncode != 0 or not out_json.is_file():
                errors.append(f"{s32.name}: {completed.stderr or completed.stdout}")
                continue
            try:
                # MapViewer export writes UTF-8 with BOM
                data = json.loads(out_json.read_text(encoding="utf-8-sig"))
            except json.JSONDecodeError as exc:
                errors.append(f"{s32.name}: invalid json {exc}")
                continue
            layer7 = data.get("layer7") or []
            for p in layer7:
                portals.append(
                    {
                        "s32": s32.name,
                        "name": p.get("name"),
                        "x": p.get("x"),
                        "y": p.get("y"),
                        "targetMapId": p.get("targetMapId"),
                        "portalId": p.get("portalId"),
                    }
                )

    return {
        "ok": True,
        "command": "map.portals",
        "map_id": str(map_id),
        "count": len(portals),
        "portals": portals,
        "segments_scanned": len(s32_files),
        "errors": errors[:20],
    }


@mcp.tool()
def export_passability(
    client_path: str,
    map_id: int,
    output_path: str | None = None,
) -> dict[str, Any]:
    """Export Layer3 passability attributes for a map (text dump)."""
    map_cli = _ensure_map_cli()
    map_dir = _map_dir(client_path, map_id)
    if output_path:
        out = Path(output_path).resolve()
    else:
        out = Path(tempfile.mkdtemp(prefix="l1r-pass-")) / f"map-{map_id}-pass.txt"
    out.parent.mkdir(parents=True, exist_ok=True)

    completed = _run(
        map_cli,
        ["-cli", "export-passability", str(map_dir), str(out)],
        timeout=180,
    )
    text = ((completed.stdout or "") + "\n" + (completed.stderr or "")).strip()
    if completed.returncode != 0 or not out.is_file():
        raise RuntimeError(f"export_passability failed ({completed.returncode}): {text[-1500:]}")

    preview = out.read_text(encoding="utf-8", errors="replace")[:500]
    return {
        "ok": True,
        "command": "map.passability",
        "map_id": str(map_id),
        "output_path": str(out),
        "size_bytes": out.stat().st_size,
        "preview": preview,
    }


@mcp.tool()
def list_regions(client_path: str, map_id: int) -> dict[str, Any]:
    """List MarketRegion / TeleportOkRegion / fishingRegion files for a map."""
    map_dir = _map_dir(client_path, map_id)
    kinds = {
        "market": list(map_dir.glob("*.MarketRegion")),
        "teleport_ok": list(map_dir.glob("*.TeleportOkRegion")),
        "fishing": list(map_dir.glob("*.fishingRegion")),
    }
    return {
        "ok": True,
        "command": "map.regions",
        "map_id": str(map_id),
        "map_dir": str(map_dir),
        "market": [p.name for p in kinds["market"]],
        "teleport_ok": [p.name for p in kinds["teleport_ok"]],
        "fishing": [p.name for p in kinds["fishing"]],
        "counts": {k: len(v) for k, v in kinds.items()},
    }


@mcp.tool()
def list_maps(client_path: str) -> dict[str, Any]:
    """List all map IDs under client/map (folder names that contain .s32)."""
    client = Path(client_path).resolve()
    map_root = client / "map"
    if not map_root.is_dir():
        raise FileNotFoundError(f"map folder not found: {map_root}")

    maps: list[dict[str, Any]] = []
    for d in sorted(map_root.iterdir(), key=lambda p: (not p.name.isdigit(), p.name)):
        if not d.is_dir():
            continue
        n = len(list(d.glob("*.s32")))
        if n == 0:
            continue
        maps.append({"map_id": d.name, "s32_count": n})

    return {
        "ok": True,
        "command": "map.list",
        "client_path": str(client),
        "count": len(maps),
        "maps": maps,
    }


if __name__ == "__main__":
    mcp.run(transport="stdio")
