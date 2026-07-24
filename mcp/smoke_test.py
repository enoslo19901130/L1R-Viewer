"""Smoke test for L1R-Viewer MCP server (read-only tools)."""
from __future__ import annotations

import argparse
import asyncio
import json
import os
import sys
from pathlib import Path

from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "mcp" / "server.py"

EXPECTED_TOOLS = {
    "l1r_health",
    "sprite_info",
    "search_sprite_entries",
    "export_sprite_frames",
    "create_sprite_sheet",
    "create_sprite_range_sheet",
    "map_info",
    "render_map",
    "list_portals",
    "export_passability",
    "list_regions",
    "list_maps",
}

# Must never appear (write/edit surface)
FORBIDDEN = {
    "fix",
    "trim",
    "import",
    "clear_l8",
    "enable_edit",
    "pak_import",
}


def _text_payload(result) -> str:
    return "".join(getattr(item, "text", "") or "" for item in (result.content or []))


def _as_dict(result) -> dict:
    raw = _text_payload(result)
    try:
        return json.loads(raw)
    except json.JSONDecodeError:
        # some MCP stacks wrap JSON in text blocks
        start = raw.find("{")
        end = raw.rfind("}")
        if start >= 0 and end > start:
            return json.loads(raw[start : end + 1])
        raise RuntimeError(f"Non-JSON tool result: {raw[:500]}")


async def main(client_path: str | None, map_id: int, sprite_id: int) -> None:
    parameters = StdioServerParameters(
        command=sys.executable,
        args=[str(SERVER)],
        cwd=str(ROOT),
    )
    async with stdio_client(parameters) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            tools = await session.list_tools()
            names = sorted(tool.name for tool in tools.tools)

            missing = sorted(EXPECTED_TOOLS.difference(names))
            if missing:
                raise RuntimeError(f"Missing MCP tools: {missing}")

            lower = {n.lower() for n in names}
            leaked = sorted(f for f in FORBIDDEN if any(f in n for n in lower))
            if leaked:
                raise RuntimeError(f"Write tools must not be exposed: {leaked}")

            health = await session.call_tool("l1r_health", {})
            if health.isError:
                raise RuntimeError(f"l1r_health error: {_text_payload(health)}")
            health_data = _as_dict(health)
            if not health_data.get("ok"):
                raise RuntimeError(f"l1r_health not ok: {health_data}")

            detail = f" health.ok tools={len(names)}"

            if client_path:
                # map_info
                mi = await session.call_tool(
                    "map_info", {"client_path": client_path, "map_id": map_id}
                )
                if mi.isError:
                    raise RuntimeError(f"map_info: {_text_payload(mi)}")
                mi_data = _as_dict(mi)
                if not mi_data.get("ok"):
                    raise RuntimeError(f"map_info not ok: {mi_data}")

                # render_map
                out_png = ROOT / "tests" / "out" / f"mcp-map-{map_id}.png"
                out_png.parent.mkdir(parents=True, exist_ok=True)
                if out_png.exists():
                    out_png.unlink()
                rm = await session.call_tool(
                    "render_map",
                    {
                        "client_path": client_path,
                        "map_id": map_id,
                        "output_path": str(out_png),
                        "max_size": 1024,
                    },
                )
                if rm.isError:
                    raise RuntimeError(f"render_map: {_text_payload(rm)}")
                rm_data = _as_dict(rm)
                if not rm_data.get("ok") or not out_png.is_file() or out_png.stat().st_size == 0:
                    raise RuntimeError(f"render_map failed: {rm_data}")

                # list_portals (map 53 known to have portals; map_id may not)
                lp = await session.call_tool(
                    "list_portals", {"client_path": client_path, "map_id": map_id}
                )
                if lp.isError:
                    raise RuntimeError(f"list_portals: {_text_payload(lp)}")
                lp_data = _as_dict(lp)
                if not lp_data.get("ok"):
                    raise RuntimeError(f"list_portals not ok: {lp_data}")

                # Prefer map 53 if caller used a map without portals
                portal_count = int(lp_data.get("count") or 0)
                if portal_count == 0 and map_id != 53:
                    lp53 = await session.call_tool(
                        "list_portals", {"client_path": client_path, "map_id": 53}
                    )
                    if not lp53.isError:
                        lp53_data = _as_dict(lp53)
                        portal_count = int(lp53_data.get("count") or 0)

                if portal_count == 0:
                    raise RuntimeError(
                        "list_portals returned empty (expected at least map 53 portals)"
                    )

                # sprite_info
                si = await session.call_tool(
                    "sprite_info",
                    {"client_path": client_path, "sprite_id": sprite_id},
                )
                if si.isError:
                    raise RuntimeError(f"sprite_info: {_text_payload(si)}")
                si_data = _as_dict(si)
                if not si_data.get("ok"):
                    raise RuntimeError(f"sprite_info not ok: {si_data}")
                if si_data.get("sprite_id") != sprite_id and str(
                    si_data.get("sprite_id")
                ) != str(sprite_id):
                    # still ok if variants present
                    if not si_data.get("variants") and not si_data.get("frames"):
                        raise RuntimeError(f"sprite_info missing data: {si_data}")

                detail += (
                    f" map_id={map_id} render={out_png.stat().st_size}B"
                    f" portals={portal_count} sprite_id={sprite_id}"
                    f" variants={si_data.get('variant_count', '?')}"
                )

            print(f"MCP PASS tools={len(names)} names={','.join(names)}{detail}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--client",
        default=os.environ.get("L1R_CLIENT"),
    )
    parser.add_argument("--map-id", type=int, default=53)
    parser.add_argument("--id", type=int, default=167, help="sprite id")
    args = parser.parse_args()

    # default client path used in plans
    if not args.client:
        default = Path(
            r"C:\Users\EnosLo\Desktop\00-Workspace\000-Ongoing-Projects"
            r"\LineageR-2606262601\001-CLIENT\LineageRemastered-2606262601"
        )
        args.client = str(default) if default.is_dir() else None

    asyncio.run(main(args.client, args.map_id, args.id))
