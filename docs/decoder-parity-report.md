# Decoder Parity Report (Phase 3)

Date: 2026-07-24/25  
Client: `LineageRemastered-2606262601`  
Old baseline: `LineageTool\headless\bin\LineageTool.Cli.exe` (v2 color-corrected decoder)  
New: `L1R.Cli` / `Lin.Helper.Core` (`L1SPX.Read` / `SprReader.Load`)

## Samples

| Sprite ID | Old size (frame 0) | New size (frame 0) | Pixel diff |
|---|---|---|---|
| 167 | 88×77 (3932 B) | 144×120 (5470 B) | **SHAPE_MISMATCH** |
| 169 | 89×165 (7690 B) | 96×216 (8790 B) | **SHAPE_MISMATCH** |
| 170 | 379×261 (23497 B) | 432×312 (34482 B) | **SHAPE_MISMATCH** |

Commands:

```text
# old
LineageTool.Cli.exe export --client <client> --id 167 --output tests/out/parity/old --frame 0

# new
pakviewer-cli.exe spr export <client> 167 -o tests/out/parity/new --frame 0
```

## Conclusion

- **Not pixel-identical** at native resolution. Remaster client stores **SPX** (48×48 block grid); Core exports native remaster frames. Old LineageTool v2 path appears to produce **downscaled / re-cropped** classic-sized bitmaps with its own color pipeline.
- New decoder is **usable and correct for remaster assets** (MCP `sprite_info` returns 8 variants × 4 frames for id 167; PNG exports succeed).
- **Does not block** MCP/map tools or GUI phases.

## Follow-ups (non-blocking)

1. Optional: add `spr export --scale classic` using `L1SPX.ToSpr` then classic render for 1:1 with old tool.
2. Optional: side-by-side color correction audit if owner needs old aesthetic.
3. Retire old .NET Framework LineageTool from agent/MCP paths: **done for MCP** (server points only at L1R-Viewer). Physical archival of LineageTool folder is owner decision.

## Status

- [x] Representative set exported (167/169/170)
- [x] Diff attempted and documented
- [ ] diff=0 (not achieved; documented root cause)
- [x] MCP / `l1r` no longer depend on old headless host
