---
title: GAM File Format
output: gam.html
description: Fallout GAM text files for initial global and map variables, parser syntax, save-game variable blocks, and editing hazards.
---

# GAM File Format

GAM files are text files that define initial integer variables. Fallout uses one game-wide GAM file for global variables and optional per-map GAM files for map variables. The files are not save files by themselves; they seed runtime arrays that are later stored inside [saved game data](savegame.html) and saved maps.

## Locations

| File | Section header | Runtime array | Used by |
|---|---|---|---|
| `data\vault13.gam` | `GAME_GLOBAL_VARS:` | Game global variables. | `global_var` and `set_global_var` script operations. |
| `maps\*.gam` | `MAP_GLOBAL_VARS:` | Current map's map-variable array. | `map_var` and `set_map_var` script operations. |

The map GAM filename is derived from the map filename. For example, when loading a clean `maps\MYMAP.MAP`, Fallout 2 CE also looks for `maps\MYMAP.GAM` and reads its `MAP_GLOBAL_VARS:` section.

## Text syntax

The parser searches for the requested section header, then reads each following line as one variable entry. The variable's list index is its position in this parsed order, starting at zero. Scripts refer to variables by this numeric index, not by the source name.

```text
MAP_GLOBAL_VARS:
//MAP VAR                              NUMBER

SecDoor_open            := 0;           // (0)
armory_access           := 0;           // (1)
revolting               := 1;           // (2)
dummyvar                := 0;           // (3)
map_runs                := 0;           // (4)
rebels_leave_date       := 100;         // (5)
```

| Syntax element | Behavior |
|---|---|
| Section header | The engine scans until the requested 16-character header prefix matches, such as `GAME_GLOBAL_VARS:` or `MAP_GLOBAL_VARS:`. |
| Blank lines | Lines whose first character is a newline are skipped. |
| `//` lines | Lines beginning with `//` are skipped. |
| Variable name | Useful to humans and tools, but ignored by the engine's value parser. |
| `=` | The parser searches for an equals sign and reads a signed decimal integer after it with `sscanf("%d")`. |
| `;` | If present, the semicolon terminates the parsed part of the line. Text after it is ignored. |
| Missing `=` | The variable entry still counts as a variable, but its value defaults to `0`. |

Although many examples use Pascal-style `:=`, Fallout 2 CE only looks for the `=` character when parsing the initial value. The colon is part of the conventional syntax, not the part that makes the value parse.

Keep lines short. Fallout 2 CE reads GAM lines into a fixed buffer and requests at most 258 characters per line. The older practical editing rule is still good: keep each line under 256 characters so an overlong line cannot spill into the following logical entry in older tools or engines.

## Parser quirks

The CE parser is simple and line-oriented. A compatible tool should mimic these quirks when it needs engine-equivalent results:

| Quirk | Consequence |
|---|---|
| Section matching uses `strncmp(..., 16)`. | The requested header prefix is matched by its first 16 characters. Use the canonical section lines anyway. |
| Only lines whose first character is `\n` are treated as blank. | Whitespace-only lines are not skipped by that blank-line test. |
| Only lines starting with `//` are skipped as comments. | Indented `//` comments are not skipped by the comment test and can become zero-valued variable entries. |
| The semicolon is removed before parsing the value. | Trailing comments after `;` do not affect the value. |
| The parser searches for `=`, not `:=`. | `name = 1;` and `name := 1;` both provide a parseable value. |
| Values are read with decimal `sscanf("%d")`. | Use plain signed decimal initial values for maximum compatibility. |
| A non-skipped line without `=` still increments the variable count. | Formatting or comments in the wrong place can shift all later variable indexes. |

## Index stability

Variable names are documentation; variable positions are the ABI. Inserting, deleting, or reordering lines changes the numeric indexes used by scripts. This can silently retarget every later `global_var`, `map_var`, or local editor reference that expects the old order.

- Append new variables at the end when compatibility matters.
- Leave obsolete variables as dummy placeholders instead of deleting them.
- Keep the trailing number comments in sync, but do not rely on those comments for parsing.
- Use naming conventions such as `GVAR_...` and `MVAR_...` for readability; the engine does not require them.

## Game global variables

At game initialization and reset, Fallout 2 CE reads `data\vault13.gam` with the `GAME_GLOBAL_VARS:` section and builds the game-global integer array. Scripts access this array through game-global opcodes. Some engine systems also use hard-coded global variable indexes for reputation, addiction, car state, timers, and game progression.

The Pip-Boy quest and holodisk tables are another GVAR consumer. [`data\quests.txt`](pipboy_txt.html) uses GVAR thresholds to decide when quest descriptions appear and when they are drawn as completed. `data\holodisk.txt` uses non-zero GVAR values to decide which DATA entries are known.

During save-game writing, the current game-global array is written into `SAVE.DAT` as big-endian 32-bit integers. Fallout 2 CE's load/save handler table stores the game-global array twice: the first copy is loaded back into the live array, and the second copy is read and discarded. This is save-file behavior, not GAM text-file behavior.

## SAVE.DAT variable blocks

Fallout 2 CE saves and loads `SAVE.DAT` through a fixed handler table. The game-global variable array appears twice in that sequence. The duplicate is historical/engine behavior; CE reads the first copy into the live array and skips the second copy.

| Handler index | Save handler | Load handler | Meaning |
|---|---|---|---|
| `2` | `scriptsSaveGameGlobalVars` | `scriptsLoadGameGlobalVars` | Primary game-global variable array. |
| `3` | `_GameMap2Slot` | `_SlotMap2Game` | Saved maps, party-member PRO snapshots, automap transfer, and current map restoration. |
| `4` | `scriptsSaveGameGlobalVars` | `scriptsSkipGameGlobalVars` | Duplicate game-global variable array, discarded on load. |

A save editor should update both global-variable copies if it rewrites `SAVE.DAT` in place. A reader that only wants current values can read handler `2` and ignore handler `4`, as CE does.

## Map variables

Map variables are map-wide values for the currently loaded map. The [MAP file](map.html) contains a `global_vars_count` field and a serialized array of map variables immediately after the header. For a clean, non-saved map, Fallout 2 CE later reloads the matching `maps\*.gam` file and replaces the map-variable array with the text-defined initial values.

For a saved map, the saved-map flag is set in the MAP header, and the serialized map-variable array from the saved MAP/SAV data is preserved. In that case the map GAM file is not used to reset the current runtime values.

| Situation | Map-variable source |
|---|---|
| Clean mapper-authored map | Initial values from `maps\NAME.GAM`, section `MAP_GLOBAL_VARS:`, after the MAP structure is read. |
| Saved map state | Serialized map-variable array stored inside the saved MAP/SAV data. |
| Missing or unreadable map GAM | The MAP's serialized map-variable array remains the available source; tools should tolerate this case. |

### Map load lifecycle

| Step | Clean map | Saved map |
|---|---|---|
| Read MAP header | Reads `global_vars_count`, `local_vars_count`, and flags. | Same. |
| Read serialized map variables | Reads the map-variable array stored after the header. | Reads the saved runtime map-variable array. |
| Read serialized local variables | Reads the local-variable array stored after map variables. | Reads the saved runtime local-variable array. |
| Check saved-map flag | Flag `0x00000001` is clear. | Flag `0x00000001` is set. |
| Load matching `maps\NAME.GAM` | CE derives the GAM path from the map filename, reads `MAP_GLOBAL_VARS:`, replaces the current map-variable array, and updates `global_vars_count`. | Skipped; saved runtime map variables are preserved. |
| Script locals | Not loaded from GAM. Clean-map script local counts can be rebuilt from `scripts.lst` metadata. | Not loaded from GAM. Saved script local values and offsets are preserved from the MAP/SAV data. |

## Local variables

Script local variables are related but are not defined by GAM files. Script local counts come from `scripts\scripts.lst` metadata such as `local_vars=N`, and the actual local values are stored in the MAP local-variable array. A script record points into that array with `local_vars_offset` and `local_vars_count`.

This distinction matters when comparing files: `MAP_GLOBAL_VARS:` defines map variables used by `map_var`; `local_vars=N` in `scripts.lst` defines how much local storage a script wants; the MAP file stores both map-variable values and local-variable values as separate arrays.

## Save metadata

Save-slot metadata is stored in `SAVE.DAT`, not in a GAM file. It is included here because global variables and map variables are saved alongside this metadata, and tools that inspect save state often need both.

| Save header field | Size or type | Description |
|---|---|---|
| `signature` | `char[24]` | Save signature, normally beginning with `FALLOUT SAVE FILE`. |
| `version_minor` | `int16` | Minor version stored in the save header. |
| `version_major` | `int16` | Major version stored in the save header. |
| `version_release` | `uint8` | Release/build byte in the save header. |
| `character_name` | `char[32]` | Player character name shown in the load/save UI. |
| `description` | `char[30]` | User-entered save description. |
| `file_month`, `file_day`, `file_year`, `file_time` | date/time fields | Real-world save timestamp metadata. |
| `game_month`, `game_day`, `game_year`, `game_time` | date/time fields | In-game date and game-time tick. |
| `elevation` | `int16` | Current elevation for the save preview/state. |
| `map` | `int16` | Current map index. |
| `file_name` | `char[16]` | Current map file name used when restoring the saved map. |

Save slots also carry compressed saved maps, party member PRO snapshots, automap data, the player object, critter data, queue data, worldmap state, interface state, and other subsystems. GAM text files provide initial variables; the save slot preserves current variables and broader runtime state.

## Editing notes

- Do not reorder existing variables unless all scripts and tools that use their numeric indexes are updated.
- Do not assume the number in a trailing comment is authoritative; it is only a human annotation.
- Use decimal integer initial values for compatibility with the engine parser.
- For clean maps, update the matching `maps\*.gam` when changing initial map variables.
- For saved maps, edit the serialized MAP/SAV variable arrays if the goal is to change current runtime state.
- When documenting save-game state, distinguish initial values from `.GAM` and current values from `SAVE.DAT` or saved MAP files.

## Source References

- [Fallout 2 CE - game.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/game.cc)
- [Fallout 2 CE - map.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/map.cc)
- [Fallout 2 CE - scripts.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/scripts.cc)
- [Fallout 2 CE - loadsave.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/loadsave.cc)

## History

2026-05-05 - Expanded with Fallout 2 CE source-backed notes by [OpenAI](https://github.com/OpenAI)

2020-01-16 - Ported from [Vault-Tec Labs GAM File Format](https://falloutmods.fandom.com/wiki/GAM_File_Format)
