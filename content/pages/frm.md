---
title: FRM File Format
output: frm.html
description: Fallout FRM indexed-art format, frame layout, palette behavior, FID relationships, critter filenames, validation, and tooling notes.
---

# FRM File Format

FRM files are Fallout and Fallout 2 indexed-color art containers. They store one or more raw 8-bit frames, optionally arranged into the six isometric rotations used by map objects and critters.

The format is used for map objects, critter animations, tiles, walls, scenery, items, inventory art, interface screens, skilldex images, talking heads, and talking-head backgrounds. Movies use [MVE](mve.html), splash/loading images commonly use [RIX](rix.html), and FRM color data comes from external [PAL](pal.html) files.

## Format Properties

| Property | Description |
|---|---|
| File type | Binary indexed-color image or animation. |
| Typical extensions | `.frm`, plus directional critter extensions `.fr0` through `.fr5`. |
| Byte order | All 16-bit and 32-bit numeric fields are big-endian. |
| Compression | None. Pixel data is stored as one byte per pixel. |
| Palette | External. Usually `color.pal`, with special same-basename PAL files for some screens. |
| Transparency | Pixel value `0` is transparent in transparent blitters, especially object/FRM drawing paths. |
| Frame order | All frames for rotation 0, then all frames for rotation 1, continuing through rotation 5 when present. |
| Header size | `0x3E` bytes. |
| Frame record header size | `0x0C` bytes before each frame's pixels. |

## Where FRMs Live

The art system resolves FRM names through art list files. The art type selects a directory and LST file, while the low bits of the art FID select a line in that list. See [LST File Format](lst.html) for the full FID layout.

| Art type | Directory | List | Typical contents |
|---|---|---|---|
| 0 | `art\items` | `items.lst` | Ground item art. |
| 1 | `art\critters` | `critters.lst` | Critter animation base names. |
| 2 | `art\scenery` | `scenery.lst` | Doors, ladders, containers, plants, props. |
| 3 | `art\walls` | `walls.lst` | Wall art. |
| 4 | `art\tiles` | `tiles.lst` | Hex floor/roof tile art. |
| 5 | `art\misc` | `misc.lst` | Projectiles, effects, and miscellaneous art. |
| 6 | `art\intrface` | `intrface.lst` | Interface screens and widgets. |
| 7 | `art\inven` | `inven.lst` | Inventory item art. |
| 8 | `art\heads` | `heads.lst` | Talking-head animation base names. |
| 9 | `art\backgrnd` | `backgrnd.lst` | Talking-head dialogue backgrounds. |
| 10 | `art\skilldex` | `skilldex.lst` | Skilldex and stat images. |

When `[system] language` is not English, Fallout 2 CE can try a localized art path before the normal path. The FID and LST line do not change; only the resolved resource path changes.

## Palette And Transparency

FRM pixel bytes are palette indexes, not RGB colors. To display a frame, read each byte and map it through the active [PAL](pal.html) palette. Most world art uses `color.pal`. Some full-screen interface art uses a same-basename palette such as `helpscrn.pal`.

Pixel value `0` is treated as transparent by the transparent blitters used for most object art. This is a drawing-rule convention, not a field inside the FRM. If an FRM is drawn into an opaque full-screen buffer, index `0` can still appear as whatever color the active palette assigns to index `0`.

Palette indexes `229..254` are animated by the main palette color-cycling system. Static FRM artwork should avoid those indexes unless it intentionally wants glowing slime, monitors, fire, shoreline, or pulse effects.

## Header Layout

The FRM header is always `0x3E` bytes. Offsets in the header's direction table are relative to the beginning of the frame area, not to the start of the file. The frame area begins immediately after the header at file offset `0x3E`.

| Offset | Size | Type | Name | Description |
|---|---|---|---|---|
| `0x0000` | 4 | `uint32` | `version` | Format version. Released Fallout 1/2 art normally uses `4`. |
| `0x0004` | 2 | `uint16` | `fps` | Animation frames per second. Fallout 2 CE exposes this as `framesPerSecond` and uses `10` if the value is zero. |
| `0x0006` | 2 | `uint16` | `action_frame` | Frame index associated with the action point of an animation, such as a shot, swing, or door action. Some older notes call this field unknown because it is merely loaded in lower-level art code. |
| `0x0008` | 2 | `uint16` | `frames_per_direction` | Number of frame records in each stored direction. Static art usually has `1`. |
| `0x000A` | 12 | `int16[6]` | `x_offsets` | Base X offset for rotations 0 through 5. |
| `0x0016` | 12 | `int16[6]` | `y_offsets` | Base Y offset for rotations 0 through 5. |
| `0x0022` | 24 | `uint32[6]` | `data_offsets` | Offset of the first frame record for each rotation, relative to file offset `0x3E`. |
| `0x003A` | 4 | `uint32` | `frame_area_size` | Total byte size of the serialized frame area. CE tolerates malformed files with this value set to zero by using the file size as a fallback. |

## Frame Record Layout

Every stored frame starts with a 12-byte frame header followed by `size` bytes of pixel data. The pixel data is row-major: left to right, then top to bottom.

| Relative offset | Size | Type | Name | Description |
|---|---|---|---|---|
| `0x00` | 2 | `uint16` | `width` | Frame width in pixels. |
| `0x02` | 2 | `uint16` | `height` | Frame height in pixels. |
| `0x04` | 4 | `uint32` | `size` | Number of pixel bytes. Normally `width * height`. |
| `0x08` | 2 | `int16` | `x` | Per-frame X offset from the previous/current animation position. |
| `0x0A` | 2 | `int16` | `y` | Per-frame Y offset from the previous/current animation position. |
| `0x0C` | `size` | `uint8[]` | `pixels` | Palette indexes. |

```text
frame_offset = 0x3E + data_offsets[rotation]
for frame_index in 0..frames_per_direction - 1:
    read width, height, size, x, y
    read size pixel bytes
    next frame follows immediately
```

On disk, frame records are tightly packed. Fallout 2 CE adds 32-bit alignment padding only in memory after loading each frame, so tools should not write CE's in-memory padding back into the file.

## Directions And Suffixes

Fallout uses six rotations:

| Rotation | Direction |
|---|---|
| 0 | Northeast |
| 1 | East |
| 2 | Southeast |
| 3 | Southwest |
| 4 | West |
| 5 | Northwest |

A normal `.frm` can contain one direction or all six directions. Directional critter art can also be split into `.fr0` through `.fr5` files. Public format notes describe those suffixes as the six orientations, starting with northeast and rotating clockwise.

In Fallout 2 CE's FID-to-path logic, a packed critter rotation field of `0` resolves to `.frm`. Nonzero packed rotation fields resolve to `.frN` by subtracting one from the packed value. The higher-level FID builder usually forces rotation `0` for non-critter art and most non-death critter animations. Directional lookup is mainly preserved for knockdown/death critter animations, where the requested object rotation can resolve to `.fr0`, `.fr1`, and so on.

If two consecutive entries in `data_offsets` are equal, the engine can reuse the already-read frame data for that rotation. This is common for art that is not actually directional.

## Positioning Model

The logical anchor of an FRM is normally the center of the bottom edge of the visible frame. For a static frame with no offsets, a top-left draw position can be approximated as:

```text
left = anchor_x - (width / 2)
top  = anchor_y - height
```

The header's `x_offsets` and `y_offsets` give a base adjustment for each rotation. Each frame record also has `x` and `y` offsets used while animations advance. The engine applies those offsets to keep feet, doors, weapon swings, death animations, and other moving frames visually anchored as dimensions change.

When playing an animation forward, CE advances frames and applies each frame's stored offset. When playing an animation in reverse, it subtracts the previous frame offset. This means per-frame offsets are motion deltas in the animation stream, not just standalone crop origins.

## Timing And Action Frames

The `fps` field controls animation timing. Fallout 2 CE computes frame duration as:

```text
ticks_per_frame = 1000 / fps
```

If `fps` is zero, CE treats it as `10`. Combat speed preferences can add to walking animation FPS in combat, depending on player/NPC state and settings.

The `action_frame` field is exposed by the art layer. It is best treated as an animation metadata marker: the frame on which the gameplay action visually happens. Tools should preserve it even if they do not use it for simple preview playback.

## FID Relationship

FRM files are rarely referenced by path in game data. Most references are packed art FIDs stored in [PRO](pro.html) files, [MAP](map.html) objects, scripts, interface state, and save data.

```text
bits 28..30  rotation / directional file selector
bits 24..27  art object type
bits 16..23  animation type
bits 12..15  weapon animation code
bits  0..11  art LST index
```

The art object type selects the art list family, and the low 12 bits select the line in that list. For most non-critter art, the LST entry is already a complete FRM filename. For critters and talking heads, the LST entry is a base name and the engine appends animation/fidget suffixes.

## Critter Filename Construction

Critter entries in `critters.lst` are base names, not full filenames. The engine constructs the final filename from:

- the base name from `critters.lst`, such as `hmjmps`;
- a weapon-animation letter, usually `a` for no weapon or `d..m` for weapon classes;
- an animation letter derived from the animation type;
- `.frm` or a directional `.frN` extension.

For example, a critter base name can combine with action letters to produce names like `hmjmpsaa.frm` or directional variants such as `hmjmpsaa.fr0`. The exact suffix pair depends on the animation and weapon code. See [Animation Names](anim_names.html) and the weapon animation values in [PRO](pro.html).

| Weapon animation code | Letter | Typical meaning |
|---|---|---|
| 0 | none / `a` path for many base animations | No weapon. |
| 1 | `d` | Knife. |
| 2 | `e` | Club. |
| 3 | `f` | Sledgehammer. |
| 4 | `g` | Spear. |
| 5 | `h` | Pistol. |
| 6 | `i` | SMG. |
| 7 | `j` | Rifle/shotgun class. |
| 8 | `k` | Laser rifle/big gun class in CE naming. |
| 9 | `l` | Minigun. |
| 10 | `m` | Launcher. |

Some death/effect animations alias to another critter base. CE uses the extra fields in `critters.lst` to find an anonymous/alias critter for animations such as electrify, burned-to-nothing, fire dance, and called-shot pictures.

## Talking Heads

Talking heads are also FRM-based, but their filenames are generated differently from world critters. The head FID's animation field selects reaction/fidget/phoneme groups. The engine appends two letters from internal head-animation code tables, and for fidget variants it can append a fidget number before `.frm`.

The companion `heads.lst` metadata stores the count of good, neutral, and bad fidgets for each head. [LIP](lip.html) files drive mouth/phoneme timing, while FRM files provide the actual talking-head frames.

## Validation Rules

A strict FRM reader should check at least these conditions:

- `version` is normally `4`.
- `frames_per_direction` is greater than zero.
- Each unique `data_offsets` entry points inside the frame area.
- Each frame has enough bytes for its 12-byte header and pixel payload.
- `size` should equal `width * height` for normal Fallout art.
- The serialized unique directions should not read beyond `frame_area_size` or the file size.
- Pixel indexes are bytes, so every value is already in `0..255`; interpretation depends on the active palette.

For compatibility, a reader may choose to tolerate `frame_area_size = 0`. Fallout 2 CE handles this by substituting the file size before allocating its art-cache block, a workaround added for malformed mod files.

## Reading Algorithm

```text
read header
base = 0x3E
for rotation in 0..5:
    if rotation != 0 and data_offsets[rotation] == data_offsets[rotation - 1]:
        reuse previous rotation frames
        continue

    seek base + data_offsets[rotation]
    for frame in 0..frames_per_direction - 1:
        read width, height, size, x, y
        read size bytes of pixels
```

This is the logical disk algorithm. CE's cache loader reads unique directions in order and then stores the frames in a larger in-memory block that includes alignment padding.

## Writing Notes

- Write big-endian numeric fields.
- Keep frame records tightly packed on disk.
- Set `size = width * height` unless deliberately preserving malformed data.
- Set repeated `data_offsets` only when rotations truly share the same frame records.
- Use `.fr0` through `.fr5` only for directional critter files expected by the target engine's FID path builder.
- Preserve `fps`, `action_frame`, base rotation offsets, and per-frame offsets when round-tripping existing art.
- When importing from PNG or another RGB format, remap colors through the target [PAL](pal.html) rather than writing arbitrary RGB bytes.
- Avoid palette index `0` for visible pixels in transparent object art.

## Inter-File Dependencies

| Format/file | Relationship |
|---|---|
| [PAL](pal.html) | Supplies RGB colors, RGB555 lookup, lighting/blending tables, and color-cycling behavior for indexed FRM pixels. |
| [LST](lst.html) | Art FIDs resolve to art LST lines before they resolve to FRM filenames or critter/head base names. |
| [PRO](pro.html) | Prototype records store object art FIDs and inventory art FIDs. |
| [MAP](map.html) | Map objects store current FID, frame, rotation, screen offsets, and object state that determines which FRM is drawn. |
| [Savegame](savegame.html) | Saved objects preserve current animation/frame/FID state, so changing art lists can affect existing saves. |
| [LIP](lip.html) | Talking-head lip-sync timing selects mouth/phoneme animation frames; FRM stores the visual frames. |
| [DAT](dat.html) | FRMs are usually loaded from `master.dat`, `critter.dat`, patch DATs, or loose `data` paths. |
| [CFG/INI](cfg.html) | `fallout2.cfg` controls art cache size, language lookup, resource roots, and palette color cycling. |

## Implementation Notes

- The on-disk `Art` header has six X offsets, six Y offsets, six data offsets, and one frame-area size. Fallout 2 CE's in-memory `Art` struct has an extra `padding[6]` array that is not present on disk.
- The low 12 bits of a FID limit a normal art list to indexes `0..4095`.
- Art list entries are stored in fixed 13-byte slots in CE: up to 12 characters plus a null terminator.
- For rendering previews, `artRender` in CE draws frame 0, rotation 0 with transparent blitting and preserves aspect ratio if the destination is smaller than the frame.
- For animation previews, apply both the base rotation offset and the per-frame offsets. A previewer that ignores offsets can appear to jitter or slide incorrectly.
- Static one-frame FRMs can still have meaningful offsets. Do not assume offsets are only for multi-frame animations.
- FRM has no embedded path, name, type, palette name, or compression marker. Context comes from FID, LST, directory, and calling code.

## Source Code Map

| Source | Relevant behavior |
|---|---|
| Fallout 2 CE `art.h` | In-memory `Art` and `ArtFrame` structures, art APIs, weapon animation enum. |
| Fallout 2 CE `art.cc` | Art list loading, FID-to-path construction, FRM header/frame reading and writing, data-size workaround, cache integration. |
| Fallout 2 CE `obj_types.h` | Rotation enum, art object type enum, FID type macro. |
| Fallout 2 CE `animation.h` | Animation type enum and FID animation field macro. |
| Fallout 2 CE `animation.cc` | Animation timing from FRM FPS, frame advancement, forward/reverse frame offset application. |

## References

- [FRM File Format - The Fallout Wiki](https://fallout.wiki/wiki/FRM_File_Format)
- [TeamX FRM Image File Format notes](https://sucs.org/~grepwood/teamx/frm_2.html)
- [FRM files - FODEV mirror](https://fodev.net/files/fo2/frm.html)
- [File Identifiers - The Fallout Wiki](https://fallout.wiki/wiki/File_Identifiers)
- [Fallout 2 CE art.h](https://github.com/alexbatalov/fallout2-ce/blob/main/src/art.h)
- [Fallout 2 CE art.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/art.cc)
- [Fallout 2 CE obj_types.h](https://github.com/alexbatalov/fallout2-ce/blob/main/src/obj_types.h)
- [Fallout 2 CE animation.h](https://github.com/alexbatalov/fallout2-ce/blob/main/src/animation.h)
- [Fallout 2 CE animation.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/animation.cc)
- [frm2png source](https://github.com/rotators/frm2png/)

## Tools

- [frm2png](https://github.com/rotators/frm2png/archive/0.1.2.zip) - [source code](https://github.com/rotators/frm2png/)
- [Titanium FRM Browser](https://fodev.net/files/archive/fo2/Titanium%20FRM%20Browser%201.3%20%28en%29.zip)
- [TeamX graphics tools](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html#graphics)

## History

2026-05-07 - Expanded with source-backed layout notes, FID/path relationships, directional suffix behavior, timing, offsets, validation rules, and implementation notes.

2020-01-18 - Ported from [Vault-Tec Labs FRM File Format](https://falloutmods.fandom.com/wiki/FRM_File_Format).
