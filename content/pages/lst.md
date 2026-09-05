---
title: LST File Format and FID Resolution
output: lst.html
description: Fallout line-indexed LST files, art-list parsing, FID layout, critter and talking-head filename construction, and editing hazards.
---

# LST File Format and FID Resolution

LST files are line-indexed tables. The first physical line is entry `0`, the second line is entry `1`, and so on. Many Fallout data structures store only an integer index; the matching LST file turns that index back into a filename, base name, script name, or other ordered entry.

This page covers generic LST rules, the art LST/FID system, critter and talking-head filename construction, and common editing hazards. [`scripts.lst`](scripts_lst.html) is documented separately because its parser has script-specific metadata rules.

## General Rules

| Rule | Description |
|---|---|
| Indexing | Zero-based. Entry `0` is the first physical line in the file. |
| Ordering | Order is data. Reordering a list changes every reference that stores a line index. |
| Blank lines | Do not use them as visual spacing. They still occupy an index and can produce an empty entry. |
| Line endings | Original data commonly uses CRLF. Readers should tolerate other endings, but tools should avoid needless churn. |
| Case | Original files often use uppercase names. Preserve case because loose files can be case-sensitive on modern platforms. |
| Extensions | Some lists store complete filenames such as `STRENGTH.FRM`. Others store base names that the engine combines with a [FRM](frm.html) extension, animation suffix, or directional suffix. |
| Comments | Semicolon comments are common in simple lists. Specialized lists may use other metadata syntax, so preserve trailing text unless the parser behavior is known. |

## Simple Example

The first five entries in `skilldex.lst` are entries `0` through `4`. A skilldex art FID with object type `10` and index `0` resolves to the first line, `STRENGTH.FRM`.

```text
STRENGTH.FRM  ; Strength     (Basic Stat)
PERCEPTN.FRM  ; Perception   (Basic Stat)
ENDUR.FRM     ; Endurance    (Basic Stat)
CHARISMA.FRM  ; Charisma     (Basic Stat)
INTEL.FRM     ; Intelligence (Basic Stat)
```

## Art LST Parser

Fallout 2 CE loads one art list for each art object type from:

```text
art\TYPE\TYPE.lst
```

The loader first counts every physical line, then rewinds and extracts the first token from each line. For art lists, token parsing stops at any space, comma, semicolon, carriage return, tab, or newline. The stored token is copied into a fixed 13-byte slot: up to 12 characters plus a trailing null byte.

| Input line | Stored art token | Notes |
|---|---|---|
| `STRENGTH.FRM ; Strength` | `STRENGTH.FRM` | Semicolon comment is ignored by art-list tokenization. |
| `hmjmps,0,1` | `hmjmps` | The first pass stores the critter base name; a later critter-specific pass reads the comma metadata. |
| ` verylongfilename.frm` | empty | A leading space is a delimiter, so the first token is empty. |
| `abcdefghijklmnop.frm` | `abcdefghijkl` | Only the first 12 characters are stored by CE's art table. |

Because line count is taken before token extraction, blank/comment-only lines still reserve indexes. A line that begins with a delimiter becomes an empty filename at that index.

## Art Object Types

| Type | Directory | List file | Entry meaning |
|---|---|---|---|
| 0 | `items` | `art\items\items.lst` | Ground item FRM filename. |
| 1 | `critters` | `art\critters\critters.lst` | Critter animation base name plus optional alias/run metadata. |
| 2 | `scenery` | `art\scenery\scenery.lst` | Scenery FRM filename. |
| 3 | `walls` | `art\walls\walls.lst` | Wall FRM filename. |
| 4 | `tiles` | `art\tiles\tiles.lst` | Tile FRM filename. CE searches for `grid001.frm` to find the mapper blank tile index. |
| 5 | `misc` | `art\misc\misc.lst` | Miscellaneous FRM filename. |
| 6 | `intrface` | `art\intrface\intrface.lst` | Interface FRM filename. |
| 7 | `inven` | `art\inven\inven.lst` | Inventory FRM filename. |
| 8 | `heads` | `art\heads\heads.lst` | Talking-head base name plus fidget counts. |
| 9 | `backgrnd` | `art\backgrnd\backgrnd.lst` | Talking-head background FRM filename. |
| 10 | `skilldex` | `art\skilldex\skilldex.lst` | Skilldex/stat FRM filename. |

## Art FID Layout

Most art references are packed 32-bit FIDs rather than direct paths. The object type selects the art list family; the low 12 bits select the line in that list.

```text
bits 28..30  rotation / directional selector
bits 24..27  art object type
bits 16..23  animation type
bits 12..15  weapon animation or head fidget number
bits  0..11  art LST index
```

```c
fid = ((rotation << 28) & 0x70000000)
    | (objectType << 24)
    | ((animType << 16) & 0x00FF0000)
    | ((weaponCode << 12) & 0x0000F000)
    | (lstIndex & 0x00000FFF);
```

| Field | Mask | Description |
|---|---|---|
| LST index | `0x00000FFF` | Zero-based entry within the selected art LST. This limits normal art FIDs to entries `0..4095`. |
| Weapon/fidget field | `0x0000F000` | Critter weapon animation code, or talking-head fidget number when the head filename pattern uses numbered fidgets. |
| Animation type | `0x00FF0000` | Critter animation, talking-head reaction/fidget/phoneme selector, or usually zero for simple art. |
| Art object type | `0x0F000000` | Selects `items`, `critters`, `scenery`, and so on. |
| Rotation | `0x70000000` | Directional selector used mainly by directional critter art. |

### Example FIDs

| FID | Meaning |
|---|---|
| `0x0A000000` | Object type `10` (`skilldex`), index `0`, resolving to entry 0 of `art\skilldex\skilldex.lst`. |
| `0x0700002A` | Object type `7` (`inven`), index `42`, resolving to entry 42 of `art\inven\inven.lst`. |
| `0x01000000` | Critter type `1`, animation `0` stand, weapon code `0`, index `0`; the filename is constructed from `critters.lst` rather than read directly. |

## FID To Path Resolution

1. Extract the art object type from bits `24..27`.
2. Extract the LST index from bits `0..11`.
3. Load the corresponding `art\TYPE\TYPE.lst` table.
4. Use the LST index to get the filename or base name.
5. For most art types, append nothing: the LST token is already the filename.
6. For critters, append weapon/animation letters and `.frm` or `.frN`.
7. For talking heads, append head reaction/fidget/phoneme letters and `.frm`.
8. If localized art is enabled, try `art\<language>\...` before the normal path.

Changing an LST line changes every FID that points to that line. Changing the token at a line preserves indexes but redirects all references to a different file. Inserting or deleting a line shifts all later references.

## Critter Lists

`critters.lst` uses the first token as the critter base name. CE then re-reads the same file and interprets comma-separated metadata after the base name.

```text
base_name,alias_index,should_run
```

| Field | Description |
|---|---|
| `base_name` | Base stem used to construct critter animation filenames, such as `hmjmps`. |
| `alias_index` | Index into `critters.lst` used as an anonymous/death/effect alias for some animations. |
| `should_run` | Runtime flag returned by `artCritterFidShouldRun`. Used by critter movement/animation behavior. |

If a line has no comma, CE defaults the alias to the current native male tribal/jumpsuit fallback index and sets `should_run` to `1`. If a line has one comma but no second comma, CE sets the alias from the first metadata value and leaves `should_run` at `0`.

The alias is used for a small set of animations: electrify, burned-to-nothing, electrified-to-nothing, fire dance, called-shot picture, and the matching single-frame variants. The alias keeps exotic death/effect animations available even when the original critter does not provide all those art files.

## Critter Filename Construction

For critter art, the LST entry is only a base stem. The final file is:

```text
art\critters\BASE + weapon_letter + animation_letter + extension
```

For example, base `hmjmps` with stand/no-weapon suffix `aa` becomes `art\critters\hmjmpsaa.frm`. Directional knockdown/death art can become `hmjmpsba.fr0`, `hmjmpsba.fr1`, and so on.

| Situation | First letter | Second letter |
|---|---|---|
| Basic unarmed/no-weapon animations | `a` | `a + animation`, such as `aa` stand, `ab` walk, `ak` magic hands ground. |
| Stand/walk with weapon code | `d..m` | `a` for stand or `b` for walk. |
| Knockdown/death animations | `b` | `a..p` for animation ids `20..35`. |
| Prone/back to standing | `c` | `h` or `j`. |
| Weapon action animations | `d..m` | `c..l` for take out through continuous fire. No-weapon is invalid for this range. |
| Single-frame death animations | `r` | `a..p` for animation ids `48..63`. |
| Called-shot picture | `n` | `a`. |

### Weapon Letters

| Code | Letter | Meaning |
|---|---|---|
| 0 | `a` in many non-weapon paths | No weapon. |
| 1 | `d` | Knife. |
| 2 | `e` | Club. |
| 3 | `f` | Sledgehammer. |
| 4 | `g` | Spear. |
| 5 | `h` | Pistol. |
| 6 | `i` | SMG. |
| 7 | `j` | Rifle/shotgun class. |
| 8 | `k` | Laser rifle / heavy ranged class in CE naming. |
| 9 | `l` | Minigun. |
| 10 | `m` | Launcher. |

Throw and dodge have special cases. Throwing with a knife uses `dm`; throwing with a spear uses `gm`; other thrown attacks use `as`. Dodge with no weapon uses `an`; dodge with a weapon uses the weapon letter and `e`.

## Directional Critter Files

A normal critter `.frm` can contain all six rotations internally. Some critter animations, especially knockdown/death art, are stored as directional `.fr0` through `.fr5` files.

In CE's path builder, a packed critter rotation value of `0` resolves to `.frm`. A nonzero packed rotation resolves to `.frN`, where `N = packed_rotation - 1`. The higher-level `buildFid` helper usually forces rotation `0` for non-critter art and for most non-death critter animations. For directional knockdown/death animations, it tries the requested rotation, then can fall back to east, then northeast.

## Talking-Head Lists

`heads.lst` uses a base name plus three fidget counts:

```text
base_name,good_fidgets,neutral_fidgets,bad_fidgets
```

The base name is combined with a head animation suffix. The good, neutral, and bad fidget counts are returned when the engine needs to choose random fidget variants.

| Head animation | Suffix pattern | Meaning |
|---|---|---|
| 0 | `gv` | Very good reaction. |
| 1 | `gfN` | Good fidget number `N`. |
| 2 | `gn` | Good to neutral. |
| 3 | `ng` | Neutral to good. |
| 4 | `nfN` | Neutral fidget number `N`. |
| 5 | `nb` | Neutral to bad. |
| 6 | `bn` | Bad to neutral. |
| 7 | `bfN` | Bad fidget number `N`. |
| 8 | `bv` | Very bad reaction. |
| 9 | `gp` | Good phonemes. |
| 10 | `np` | Neutral phonemes. |
| 11 | `bp` | Bad phonemes. |

For fidget suffixes, `N` comes from the FID's bits `12..15`. Talking-head animation frame timing is coordinated with [LIP](lip.html) files and speech audio.

## Prototype And Script Lists

Prototype LST files under `proto\...` map PID low bits to [PRO](pro.html) filenames. They are also line-indexed tables, but they are a different namespace from art LSTs. A PID does not directly resolve through `art\...`; it resolves through `proto\...`, then the PRO header can contain an art FID.

`scripts.lst` maps script indexes to compiled [INT](int.html) files and can carry `local_vars=N` metadata. Because that parser differs from simple art lists, see [SCRIPTS.LST File Format](scripts_lst.html) for the full behavior.

## Inter-File Dependencies

| File/format | Relationship |
|---|---|
| [FRM](frm.html) | Art LST and FID resolution usually selects the FRM file that should be loaded. |
| [PRO](pro.html) | PRO files store object art FIDs and inventory art FIDs; proto LST files map PIDs to PRO filenames. |
| [MAP](map.html) | MAP objects store current FIDs, frame numbers, rotations, and PIDs. Visual previewers need art LSTs and FRMs. |
| [Savegame](savegame.html) | Saved object FIDs and PIDs remain index-based, so changing LST order can affect existing saves. |
| [SSL](ssl.html) / [INT](int.html) | Scripts can read, test, or change object FIDs; script-list indexes are resolved through `scripts.lst`. |
| [CFG/INI](cfg.html) | Language and resource-path settings influence where resolved art paths are searched. |

## Editing Notes

- Append new entries whenever possible. Appending preserves existing indexes.
- Replace removed entries with placeholders instead of deleting lines when compatibility matters.
- Do not sort or deduplicate LST files; the order is part of the binary data model.
- Keep art tokens to 12 characters or fewer for CE/original-style compatibility.
- Preserve comma metadata in `critters.lst` and `heads.lst`.
- Preserve comments and case unless a tool has an explicit reason to normalize them.
- When changing art list entries, check PROs, MAPs, saves, scripts, and generated headers that may store the old FID.

## Parser Pitfalls

- A blank line still becomes an indexed entry.
- A leading delimiter creates an empty token.
- Names longer than 12 characters are truncated by CE's art list table.
- Entries past index `4095` cannot be represented by the standard 12-bit art-index field.
- Different LST-like files have different comment and metadata conventions. Do not use one generic parser for all lists unless it is schema-aware.
- A valid FID can still fail to load if the LST index is out of range, the constructed critter/head filename is invalid, or the resolved file is missing from DAT/loose data paths.

## Source Code Map

| Source | Relevant behavior |
|---|---|
| Fallout 2 CE `art.cc` | Art list loading, token truncation, critter/head metadata, FID-to-path construction, localized art lookup, alias handling. |
| Fallout 2 CE `art.h` | Weapon animation enum and art APIs. |
| Fallout 2 CE `obj_types.h` | Art object type and rotation enums, FID type macro. |
| Fallout 2 CE `animation.h` | Animation type enum and FID animation-field macro. |
| Fallout 2 CE `scripts.cc` | `scripts.lst` parsing, script names, and local-variable metadata. |

## References

- [LST File Format - The Fallout Wiki](https://fallout.wiki/wiki/LST_File_Format)
- [File Identifiers - The Fallout Wiki](https://fallout.wiki/wiki/File_Identifiers)
- [Vault-Tec Labs LST File Format](https://falloutmods.fandom.com/wiki/LST_File_Format)
- [Fallout 2 CE art.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/art.cc)
- [Fallout 2 CE art.h](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/art.h)
- [Fallout 2 CE obj_types.h](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/obj_types.h)
- [Fallout 2 CE animation.h](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/animation.h)
- [Fallout 2 CE scripts.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/scripts.cc)

## History

2026-05-08 - Expanded into an LST/FID resolution hub covering art-list parsing, FID fields, critter/head metadata, filename construction, dependencies, and editing hazards.

2019-12-15 - Ported from [Vault-Tec Labs LST File Format](https://falloutmods.fandom.com/wiki/LST_File_Format) by [ghost](https://github.com/ghost2238)

Created by Noid.
