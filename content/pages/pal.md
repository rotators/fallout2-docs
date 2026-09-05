---
title: PAL/COL File Format and Color Tables
output: pal.html
description: Fallout PAL palette layout, RGB555 lookup, NEWC color tables, runtime palette state, color cycling, and related indexed-art behavior.
---

# PAL/COL File Format and Color Tables

PAL files are Fallout and Fallout 2 palette resources. They define a 256-entry indexed-color palette, a 15-bit RGB-to-palette-index lookup table, and optionally precomputed color transformation tables used by lighting and blending code.

The "COL" part of this page refers to the engine's color tables and color-index behavior, not to a separate well-attested Fallout 1/2 `.COL` resource format. In the released games and current source references, palette files are `.pal`; color conversion, intensity, blending, and cycling are runtime systems built from PAL data.

## Format Properties

| Property | Description |
|---|---|
| File type | Binary palette and color-table file. |
| Typical main path | `color.pal` at the root of the Fallout data namespace. |
| Typical special paths | `art\intrface\*.pal`, `art\cuts\*.pal`, and same-basename PAL files for special FRM screens. |
| Required size | `0x8300` bytes: `768` palette bytes plus `32768` RGB555 lookup bytes. |
| Extended size | `0x38304` bytes when the optional `NEWC` block and three 65536-byte tables are present. |
| Byte order | Mostly byte arrays. The optional `NEWC` tag is read as the literal four bytes `N E W C`. |
| Color component range | Stored palette components are VGA-style `0..63`, not full `0..255` RGB. |
| Primary users | [FRM](frm.html) art, interface screens, dialogue/head backgrounds, movie subtitles, splash/help-style screens, lighting, translucency, and UI text color lookup. |

## File Layout

| Offset | Size | Name | Description |
|---|---|---|---|
| `0x000000` | `768` | Palette | 256 RGB triples. Entry `n` is stored at `n * 3` as red, green, blue. |
| `0x000300` | `32768` | RGB conversion table | Maps a 15-bit RGB555 value to the nearest palette index. |
| `0x008300` | `4` | Optional tag | Literal `NEWC`, present only when the precomputed color tables follow. |
| `0x008304` | `65536` | Intensity table | `intensityColorTable[paletteIndex][intensity]`. |
| `0x018304` | `65536` | Additive mix table | `colorMixAddTable[colorA][colorB]`. |
| `0x028304` | `65536` | Multiplicative mix table | `colorMixMulTable[colorA][colorB]`. |

A minimal PAL file can stop after the RGB conversion table. Fallout 2 CE tries to read the four bytes after `0x8300`; if they are `NEWC`, it reads the three precomputed tables. Otherwise it regenerates them from the palette and RGB conversion table.

## Palette Entries

The first 768 bytes are 256 RGB triples:

```text
offset = palette_index * 3
red   = file[offset + 0]
green = file[offset + 1]
blue  = file[offset + 2]
```

Each component should be in the range `0..63`. Fallout 2 CE marks an entry as mapped only when all three components are at most `0x3F`. If any component is above `0x3F`, the loader stores black for that palette entry and marks the index as unmapped for generated color tables.

For modern display, expand 6-bit components to 8-bit components. A quick conversion is:

```text
rgb8 = rgb6 << 2
```

A slightly fuller bit-spread conversion is:

```text
rgb8 = (rgb6 << 2) | (rgb6 >> 4)
```

The original engine keeps palette values in the `0..63` range internally. Gamma/brightness is applied when the current palette is sent to the system palette, not by rewriting the PAL file itself.

## Index 0 And Transparency

Palette index `0` is special in many Fallout drawing routines. Transparent blitters treat pixel value `0` as transparent for FRM/object art, so the visible RGB value of palette entry `0` is usually irrelevant for those paths.

Do not assume that every indexed buffer treats `0` as transparent. Some buffers are full-screen or opaque surfaces, and some code fills regions with palette index `0` as a visible background/clear color. Transparency is a property of the drawing routine and asset role, not merely of the palette file.

## RGB555 Lookup Table

The table at `0x300` maps every 15-bit RGB555 color to a palette index. The index into this table is:

```text
rgb555 = (red5 << 10) | (green5 << 5) | blue5
palette_index = colorTable[rgb555]
```

Each channel is `0..31`, producing `32 * 32 * 32 = 32768` table entries. This table is why tools can map arbitrary RGB colors back into Fallout's 256-color palette when importing images, generating lighting tables, drawing text, or blending colors.

Fallout 2 CE exposes this table as `_colorTable[32768]`. Functions such as `Color2RGB` convert a palette index back to RGB555 by reading `_cmap`, shifting the `0..63` palette components down to `0..31`, and packing them as RGB555.

## NEWC Tables

The optional `NEWC` block contains tables that older public notes called ".exe-generated data". Source code gives them concrete names and behavior:

| Table | Runtime name | Use |
|---|---|---|
| Table 1 | `intensityColorTable[256][256]` | Maps a source palette index and intensity level to a darker or lighter palette index. |
| Table 2 | `colorMixAddTable[256][256]` | Additive color mixing, used for light-like blending effects. |
| Table 3 | `colorMixMulTable[256][256]` | Multiplicative color mixing, used for darker/tint-like blending effects. |

If the `NEWC` tag is missing, Fallout 2 CE rebuilds the same runtime tables after loading the palette. Rebuilding is slower than loading precomputed data, but it means tools can emit a minimal `0x8300`-byte PAL and still be compatible with engines that regenerate the tables.

The intensity table has 256 intensity slots per color. CE builds the first 128 slots as darker versions and the next 128 slots as lighter versions toward white, remapping each computed RGB555 value through `_colorTable`.

## Runtime Palette State

The engine keeps several palette arrays with different meanings:

| Name | Description |
|---|---|
| `_cmap` | The palette loaded from the current PAL file, in 6-bit RGB triples. |
| `_systemCmap` | The current system palette state before gamma mapping. Color cycling edits this array. |
| `_currentGammaTable` | 64-entry brightness/gamma table applied when sending palette entries to the display backend. |
| `gPalette` | The palette module's current fade/source palette. |
| `gPaletteBlack` | All components zero, used for fades to black. |
| `gPaletteWhite` | All components `63`, used as a white target palette. |

Loading a PAL changes `_cmap` and the color tables. Applying a palette to the screen is a separate step: `paletteSetEntries` or `paletteFadeTo` sends palette data to the display. This distinction matters for screens that temporarily load a special PAL, then later restore `color.pal`.

## Color Cycling

Fallout's animated colors are palette animation, not per-pixel animation. Art pixels that use the cycling indexes remain unchanged, while the RGB values assigned to those indexes rotate or pulse over time.

| Indexes | Runtime group | Period in CE before speed factor | Typical use |
|---|---|---|---|
| `229..232` | `slime` | `1000 / 5` ms | Green radioactive waste/slime. |
| `233..237` | `monitors` | `1000 / 10` ms | Blue monitor/computer glow. |
| `238..242` | `fire_slow` | `1000 / 5` ms | Orange/yellow fire cycle. |
| `243..247` | `fire_fast` | `1000 / 7` ms | Red faster fire cycle. |
| `248..253` | `shoreline` | `1000 / 5` ms | Shoreline/water-edge shimmer. |
| `254` | `bobber_red` | `1000 / 30` ms | Fast red pulse. |

CE stores the hardcoded cycling colors as `0..255`-style values in source, shifts them down by two bits during color-cycle initialization, and then writes `0..63` palette entries. The ticker updates palette range `229..255` through `paletteSetEntriesInRange`.

Because these colors are overwritten at runtime, custom art should treat indexes `229..254` as animated/reserved unless it intentionally wants those effects. Ordinary static artwork should avoid them.

## Common Palette Files

| Path | Use |
|---|---|
| `color.pal` | Main game palette loaded during color initialization and restored after many special screens. |
| `art\intrface\helpscrn.pal` | Help screen palette used with the help-screen FRM. |
| `art\intrface\<frm-basename>.pal` | Special interface/endgame-screen palettes loaded by basename in some screen-specific paths. |
| `art\cuts\subtitle.pal` | Default movie subtitle palette. Movie palette fade effects can also be controlled by same-basename [CFG](cfg.html) sidecars. |
| `art\cuts\introsub.pal` | Subtitle palette for `intro.mve`. |
| `art\cuts\eldersub.pal` | Subtitle palette for `elder.mve`. |
| `art\cuts\artmrsub.pal` | Subtitle palette for Arroyo timer/failure/timeout-style movies. |
| `art\cuts\crdtssub.pal` | Subtitle palette for `credits.mve`. |

Same-basename FRM palette files are important for special screens. General world, critter, item, wall, scenery, tile, inventory, and interface art normally relies on `color.pal`, but some full-screen interface images deliberately switch palettes.

## Related Formats

| Format | Palette relationship |
|---|---|
| [FRM](frm.html) | Stores palette indexes only. Most FRMs use `color.pal`; special FRMs can have same-basename PAL files. |
| [RIX](rix.html) | Splash screens embed their own 768-byte palette, also in 6-bit VGA RGB form. |
| [MVE](mve.html) | 8-bit movies carry palette updates inside the movie stream rather than using PAL files for video frames. |
| [SVE](sve.html) | SVE text has no palette data, but movie subtitle rendering uses PAL files from `art\cuts`. |
| [MSK](msk.html) | World-map walk masks are not display images and do not use palettes. |
| [AAF](aaf.html) and [FON](fon.html) | Font pixels are not standalone RGB colors; rendering maps glyph data through current text color and palette/color tables. |
| [DAT](dat.html) | DAT archives store PAL files as ordinary resources; the archive layer does not alter palette bytes. |

## Implementation Notes

- Accept both `0x8300`-byte minimal PAL files and `0x38304`-byte extended PAL files.
- Validate that required palette components are in `0..63`. Preserve invalid/sentinel entries only if the tool is intentionally round-tripping original bytes.
- When importing truecolor art, quantize or remap through the RGB555 lookup table instead of nearest-searching every pixel by hand.
- When exporting to PNG, expand 6-bit palette components to 8-bit RGB and handle index `0` transparency according to the source image role.
- Keep animated palette indexes reserved unless the artwork is meant to animate through color cycling.
- Do not confuse PAL files with generic RIFF/text palette formats used by other software. Fallout PAL is a custom binary layout.
- If a tool edits the first 768 bytes, it should regenerate the RGB555 table and either regenerate or omit the `NEWC` tables. Leaving stale tables can make imports, lighting, and blending inconsistent.

## Source Code Map

| Source | Relevant behavior |
|---|---|
| Fallout 2 CE `color.cc` | PAL loading, invalid component handling, `NEWC` table loading/generation, gamma table, RGB555 conversion, blend-table generation. |
| Fallout 2 CE `palette.cc` | Runtime palette arrays, fades, applying full palettes and palette ranges. |
| Fallout 2 CE `cycle.cc` | Hardcoded animated color groups and palette-index ranges. |
| Fallout 2 CE `game.cc` | Game initialization, help screen palette switching, RIX splash palette loading. |
| Fallout 2 CE `game_movie.cc` | Movie subtitle palette selection and restoration of `color.pal`. |
| Fallout 2 CE `movie_effect.cc` | Movie palette fade effects driven by movie-side `.cfg` files. |

## References

- [PAL File Format - The Fallout Wiki](https://fallout.wiki/wiki/PAL_File_Format)
- [Pal Files - The Fallout Wiki](https://fallout.wiki/wiki/Pal_Files)
- [PAL File Format - FODEV mirror](https://fodev.net/files/fo2/pal.html)
- [TeamX FRM format notes](https://sucs.org/~grepwood/teamx/frm_2.html)
- [Fallout 2 CE color.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/color.cc)
- [Fallout 2 CE palette.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/palette.cc)
- [Fallout 2 CE cycle.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/cycle.cc)
- [Fallout default color palette table](fo_colors.html)

## History

2026-05-07 - Expanded into PAL/COL documentation covering file layout, RGB555 lookup, `NEWC` tables, runtime palette state, color cycling, special palettes, related formats, and implementation notes.

2020-01-18 - Ported from [Vault-Tec Labs PAL File Format](https://falloutmods.fandom.com/wiki/PAL_File_Format).
