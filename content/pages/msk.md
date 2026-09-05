---
title: MSK File Format
output: msk.html
description: Fallout world-map walk mask format, dimensions, bit packing, movement behavior, conversion, validation, and related world-map files.
---

# MSK File Format

MSK files are world-map walk masks. They mark pixels on a world-map tile that the party cannot cross, such as ocean or other deliberately blocked terrain. They are not display graphics, do not use the Fallout palette, and do not contain a header. They are fixed-size 1-bit masks used alongside the world-map FRM tiles defined in `data\worldmap.txt`.

The important relationship is in `worldmap.txt`. A tile section can name a mask with `walk_mask_name`. The engine then looks for `data\<walk_mask_name>.msk`. If no mask name is present, the whole world-map tile is treated as walkable by the mask system.

## Format properties

| Property | Description |
|---|---|
| File type | Raw 1-bit bitmap. No magic number, no version, no dimensions, no palette, no compression, no checksum. |
| Stored size | `13200` bytes. |
| Logical dimensions | `352 x 300` bits. The visible world-map FRM tile is `350 x 300`, so the final two bits of each row are padding. |
| Stride | `44` bytes per row: `352 / 8`. |
| Tile pairing | One MSK file belongs to one world-map FRM tile, but only tiles with blocked terrain need a mask. |
| Path | Usually `data\wrldmp00.msk`, `data\wrldmp04.msk`, etc., as named by `walk_mask_name`. |

## Worldmap.txt link

MSK files are not discovered by scanning `data\*.msk`. They are attached to world-map tiles by `walk_mask_name`:

```text
[Tile 0]
art_idx=339
encounter_difficulty=0
walk_mask_name=wrldmp00
```

Fallout 2 CE copies the mask name into a fixed 40-byte tile field and opens `data\wrldmp00.msk` lazily, the first time it needs to test movement on that tile. The value should be written without an extension. An empty or missing `walk_mask_name` means no mask data is loaded and the mask layer blocks nothing on that tile.

## Dimensions and packing

Each world-map art tile is `350 x 300` pixels. The MSK file stores each row as `44` bytes, or `352` bits. Pixels `x = 0..349` correspond to the visible world-map tile. Bits `x = 350` and `x = 351` are padding and should be written as `0`.

The conventional 1-bit interpretation is row-major:

```text
row_stride = 44
byte_offset = y * row_stride + (x / 8)
bit_mask = 1 << (x % 8)
blocked = (data[byte_offset] & bit_mask) != 0
```

This uses low-bit-first order within each byte. For a visual editor, draw only the first `350` columns and ignore the two padding bits at the end of each scanline.

## Bit meaning

The source-backed runtime meaning is:

| Bit | Meaning |
|---|---|
| `0` | Not blocked by this mask. The party can continue moving unless another world-map rule stops it. |
| `1` | Blocked/invalid world-map position. The current walking command stops before entering this pixel. |

This is worth stating because older mirrored format notes sometimes say the opposite, describing `1` as passable and `0` as impassable. Fallout 2 CE's movement check returns "invalid" when the tested mask bit is set, and an old No Mutants Allowed technical note describes `1` as sea/blocked and `0` as land.

## CE compatibility note

Fallout 2 CE confirms the fixed size and path behavior: it allocates `13200` bytes, reads exactly that many bytes from `data\<name>.msk`, and treats a set tested bit as invalid terrain. The current CE source around the bit test also contains a comment questioning the exact math. Tools that need strict target-engine compatibility should test generated masks in the engine they target, especially when converting from BMP or PNG.

For documentation and authoring purposes, the stable external description is a `352 x 300` 1-bit image with two unused padding bits per row. If a conversion tool offers both low-bit-first and high-bit-first output, use a small test mask on a known world-map tile and verify that blocked pixels line up with the intended coastline.

## Movement behavior

MSK masks are checked while the party is walking on the world map. The engine computes the destination step in world-map pixel coordinates, maps that coordinate to a world-map tile, loads the tile's mask if needed, and tests the bit for the pixel inside that tile. If the mask says the next pixel is blocked, walking stops and the party remains at the previous valid world position.

The mask does not change terrain type, encounter frequency, fog reveal, city visibility, or random encounter table selection. Those come from `worldmap.txt` subtile data and `worldmap.dat` runtime state. MSK is only a fine-grained per-pixel movement barrier layered over the larger tile and subtile definitions.

## Coordinate mapping

World-map positions are global pixels. To test an MSK bit, first find the world-map tile and then the local pixel inside that tile:

```text
tile_x = world_x / 350
tile_y = world_y / 300
tile_index = tile_y * num_horizontal_tiles + tile_x

local_x = world_x % 350
local_y = world_y % 300
```

The mask then uses `local_x` and `local_y` with the `44`-byte row stride. This is independent of the `7 x 6` world-map subtile grid. One subtile is `50 x 50` pixels, but the mask can block individual pixels inside a subtile.

## Reader recipe

1. Read the mask filename from `walk_mask_name` in the corresponding `[Tile N]` section of `data\worldmap.txt`.
2. If the key is absent or empty, treat the tile as having no blocked pixels from MSK data.
3. Open `data\<walk_mask_name>.msk` and read exactly `13200` bytes.
4. For display, expand each row into `350` visible pixels and ignore the last two padding bits.
5. For movement checks, map world coordinates to `tile_index`, `local_x`, and `local_y`, then test the mask bit.
6. Treat set bits as blocked for Fallout 2 CE-compatible behavior.

## Writer recipe

1. Create a `352 x 300` 1-bit buffer or a `350 x 300` editable image plus two padding bits per row.
2. Mark blocked pixels as `1` and open pixels as `0`.
3. Pack rows low-bit-first into `44` bytes per row.
4. Clear the two padding bits after visible columns `350` and `351`.
5. Write exactly `13200` bytes, with no BMP/PNG header or palette data.
6. Add or update the tile's `walk_mask_name` in `data\worldmap.txt`.

## Validation notes

- Reject or warn on files that are not exactly `13200` bytes.
- Warn if padding bits at columns `350` and `351` are set.
- Warn if `walk_mask_name` references a missing file. Fallout 2 CE's movement helper treats a load failure as "not invalid", which effectively leaves the tile unblocked by the mask.
- Show both "blocked" and "open" colors clearly in visual tools, because older notes and converters may use inverted black/white conventions.
- Compare the mask overlay against the matching `WRLDMPxx.FRM` tile; a correct file can still be the wrong mask for the tile if `walk_mask_name` points to the wrong base name.
- Do not infer a mask from terrain type. Ocean subtiles usually need masks, but the mask is attached by filename and can block any shape on any terrain.

## Editing notes

- MSK files are per world-map tile, not per whole world map. A large coastline across several FRMs needs several separate masks.
- Tiles without water or blocked terrain often have no MSK file at all. That is normal.
- The filename convention `wrldmp00.msk` mirrors the matching world-map tile art, but the engine follows `walk_mask_name`; the name does not have to be inferred from `art_idx`.
- Keep masks and world-map art synchronized. Moving a coastline in the FRM without editing the MSK will leave an invisible barrier or allow walking into water.
- When changing world-map dimensions or tile ordering, update `num_horizontal_tiles`, tile sections, FRMs, masks, and `walk_mask_name` together.
- Because there is no header, a mislabeled BMP or PNG will not fail gracefully as an image file; it will just be the wrong byte stream. Conversion tools should strip image headers before writing MSK.

## Visual conversion

An MSK file can be displayed as a black-and-white image, but the color choice is only a tool convention. A practical convention is to draw blocked bits as black or red and open bits as transparent/white. If importing from an image editor, provide an explicit inversion option and preview the overlay on the world-map FRM.

For a BMP-to-MSK workflow, use a thresholded 1-bit or grayscale source image at `350 x 300`, then pack it to `352 x 300` with two zero padding bits per row. Do not store a BMP header in the MSK file.

## Related formats

- [World-map Text Configuration Files](worldmap_config.html) documents the `worldmap.txt` tile definitions that reference MSK files.
- [Worldmap.dat File Format](worldmap_dat.html) documents the world-map state derived from the text definitions.
- [FRM File Format](frm.html) documents the visible world-map tile art that MSK masks line up with.
- [PAL File Format](pal.html) matters for the visible FRM art, but MSK files themselves do not use palettes.
- [LST File Format](lst.html) documents `art\intrface\intrface.lst`, where the visible world-map tile FRMs are indexed.

## Source references

- [Fallout 2 CE `worldmap.cc`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/worldmap.cc) - `walk_mask_name` loading, `data\*.msk` path construction, fixed `13200`-byte reads, and movement blocking checks.
- [The Fallout Wiki MSK File Format](https://fallout.wiki/wiki/MSK_File_Format) - classic one-bit mask description and location notes.
- [Vault-Tec Labs MSK File Format](https://falloutmods.fandom.com/wiki/MSK_File_Format) - older TeamX-derived notes and tool references.
- [No Mutants Allowed worldmap quick question](https://www.nma-fallout.com/threads/worldmap-quick-question.154894/) - ABel's note describing `352 x 300` masks, unused row padding, and set bits as sea/blocked terrain.
- [Vault-Tec Labs Worldmap.txt File Format](https://falloutmods.fandom.com/wiki/Worldmap.txt_File_Format) - `walk_mask_name` usage in tile definitions.
