---
title: World-map Text Configuration Files
output: worldmap_config.html
description: Fallout and Fallout 2 world-map text configuration files: maps.txt, city.txt, worldmap.txt, terrain, tiles, encounters, message lookups, and validation notes.
toc: auto
---

# World-map Text Configuration Files

The Fallout and Fallout 2 world map is not defined by one file. The editable source data is split across three text configuration files:

| File | Role |
|---|---|
| `data\maps.txt` | Defines the map index namespace, MAP filenames, map flags, music, ambient SFX, and random encounter start points. |
| `data\city.txt` | Defines world-map areas, town-map art, city marker positions, city visibility defaults, and town-map entrances. |
| `data\worldmap.txt` | Defines terrain types, encounter frequencies, world-map tiles, subtiles, random encounter maps, encounter tables, encounter groups, and encounter conditions. |

`worldmap.dat` is the saved or cached runtime state derived from these definitions. To edit the world map itself, edit these text files and their referenced MAP, FRM, MSK, PRO, script, sound, and message assets first.

## Loading order

Fallout 2 CE loads the world-map configuration in this order:

1. Load `text\<language>\game\worldmap.msg` for world-map UI and encounter text.
2. Read `data\maps.txt` while initializing map lookup data.
3. Read `data\city.txt` while initializing world-map areas.
4. Read `data\worldmap.txt` while initializing terrain, world-map tiles, and encounter tables.
5. Initialize runtime state and save/load `worldmap.dat` state as needed.

The order matters. `city.txt` entrances resolve map names through `maps.txt`, and `worldmap.txt` encounter tables also resolve map names through `maps.txt`. A tool should load `maps.txt` before validating either of the other two files.

## Engine capacities

The text files do not always state the practical runtime limits. The current CE structures use these capacities:

| Data | Capacity | Notes |
|---|---|---|
| Ambient SFX entries per map | 7 | Raised from the original-looking 6 to support larger mod data. |
| Random start points per map | 15 | Used mostly by random encounter maps. |
| Entrances per area | 10 | Extra `entrance_N` keys are not stored. |
| Random maps per terrain | 20 | Fallback map pool for terrain-based random encounters. |
| Maps per encounter table | 6 | Used before the terrain fallback map pool. |
| Encounter table entries | 40 parsed entries | The structure has an extra slot, but the parser stops at 40 normal entries. |
| `Enc:` subentries per table entry | 6 | Each subentry references one encounter group or `player`. |
| Entries per encounter group | 10 | `type_NN` entries beyond the structure limit are unsafe for CE-style parsers. |
| Items per encounter group entry | 10 | Each `item:` token consumes one slot. |
| Conditions per encounter/table entry | 3 | Joined by up to two `and`/`or` qualifiers. |

## Shared syntax

The three files use the engine's INI-like configuration parser.

| Rule | Behavior |
|---|---|
| Sections | Section headers use square brackets, such as `[Map 000]` or `[Encounter Table 0]`. |
| Key/value pairs | Entries use `key=value`. Whitespace around the key and value is trimmed. |
| Comments | A semicolon starts a comment. Everything after the first `;` on a line is discarded before parsing. |
| No quoting | There is no quote or escape syntax in the config parser. Semicolons cannot be kept in values. |
| Case | Section and key lookup is case-insensitive. Many world-map value parsers lowercase their input before matching tokens. |
| Duplicates | Duplicate section/key pairs replace earlier values while loading; the last parsed value wins. |
| Integers | Integer parsing accepts leading numeric text and ignores trailing characters. This is why values such as `Frequent=38%` parse as `38`. |
| Line length | Fallout 2 CE uses a 256-byte config line buffer. The original `worldmap.txt` comments mention a smaller Watcom-era path limit, so very long lines should still be avoided for original-tool compatibility. |

Most list-like values are comma-separated. The shared string parsers skip leading spaces before each comma-delimited item and match tokens case-insensitively. They also mutate and lowercase the loaded value buffer, so a format-preserving editor should keep its own copy of the original text if it wants to preserve case and spacing.

## Numbered sections

Several parts of the world-map config are read as contiguous numbered sequences. Gaps are not skipped; the loader stops at the first missing required key.

| Sequence | Section/key pattern | Stop condition |
|---|---|---|
| Maps | `[Map 000]`, `[Map 001]`, ... with required `lookup_name`. | First missing `lookup_name`. |
| Areas | `[Area 00]`, `[Area 01]`, ... with required `townmap_art_idx`. | First missing `townmap_art_idx`, then CE checks the final count against its compiled city count. |
| Encounter tables | `[Encounter Table 0]`, `[Encounter Table 1]`, ... with required `lookup_name`. | First missing `lookup_name`. |
| World-map tiles | `[Tile 0]`, `[Tile 1]`, ... with required `art_idx`. | First missing `art_idx`. |
| Area entrances | `entrance_0`, `entrance_1`, ... | First missing entrance key, or the engine capacity of 10 entrances. |
| Random maps | `map_00`, `map_01`, ... inside `[Random Maps: terrain]`. | First missing map key, or the terrain map capacity. |
| Encounter entries | `enc_00`, `enc_01`, ... inside an encounter table. | First missing encounter key, or the CE limit of 40 table entries. |
| Encounter group entries | `type_00`, `type_01`, ... inside `[Encounter: name]`. | First missing type key. CE's runtime structure has room for 10 entries. |

## data\maps.txt

`maps.txt` defines map indexes. MAP headers, world-map entrances, scripts, encounter tables, map name lookups, and save behavior all depend on this ordering. Once saves or scripts exist, changing map order is a compatibility break.

```text
[Map 000]
lookup_name=Desert Encounter 1
map_name=desert1
music=07desert
ambient_sfx=gustwind:20, gustwin1:20
saved=No
dead_bodies_age=No
can_rest_here=No, No, No
random_start_point_0=elev:0, tile_num:20195
```

### Map section keys

| Key | Required | Description |
|---|---|---|
| `lookup_name` | Yes | Symbolic map name. This is the name used by `city.txt`, `worldmap.txt`, and engine helper functions. |
| `map_name` | Yes | Base MAP filename. CE lowercases this value and appends `.MAP` when resolving a map index to a filename. |
| `music` | No | Background music base name, normally without the `.ACM` extension. If absent or empty, CE simply does not start map music from this field. |
| `ambient_sfx` | No | Comma-separated `name:chance` list for ambient SFX names, normally without file extensions. CE's current capacity is 7 entries per map. |
| `saved` | No | `Yes` or `No`. Controls whether the map is saveable and whether entering it can mark the containing world-map area visited. Default flag state is on. |
| `dead_bodies_age` | No | `Yes` or `No`. Controls dead-body aging. Default flag state is on. |
| `can_rest_here` | No | Three `Yes`/`No` values, one for elevation 0, 1, and 2. Default flag state is on for all elevations. |
| `pipboy_active` | No | `Yes` or `No`. CE parses the flag, but its current `wmMapPipboyActive` helper checks the vault-suit movie state instead. |
| `automap` | No | Optional sfall/CE automap display flag. |
| `random_start_point_N` | No | Random encounter start points, each with `elev:` and `tile_num:`. CE's current capacity is 15 start points per map. |

Classic docs also mention a `state` key. It is not read by the CE world-map map loader shown in `worldmap.cc`, so treat it as historical or tool-facing data unless the target engine documents otherwise.

### Map defaults and flags

CE initializes each map with flags `0x3F`, meaning `saved`, `dead_bodies_age`, `pipboy_active`, and all three `can_rest_here` elevation flags start enabled. Optional keys override those bits only when present. If a random encounter map should not be saved or rested in, set those keys explicitly.

| Flag | Meaning |
|---|---|
| `0x01` | Saved map state is preserved. |
| `0x02` | Dead bodies age. |
| `0x04` | Pip-Boy active flag, parsed but not currently used by CE's public helper. |
| `0x08` | Can rest on elevation 0. |
| `0x10` | Can rest on elevation 1. |
| `0x20` | Can rest on elevation 2. |

### Random start points

`random_start_point_N` entries are used when placing encounter groups with non-surrounding formations. CE reads `elev:` and `tile_num:`; the start-point rotation field exists in memory but defaults to `-1` and is not parsed from `maps.txt` by the current loader.

```text
random_start_point_0=elev:0, tile_num:20195
random_start_point_1=elev:0, tile_num:20305
```

Tile numbers here are normal MAP hex tile numbers. See [MAP File Format](map.html) for tile numbering.

## data\city.txt

`city.txt` defines the areas shown on the world map and town map. The section number is the area index used by scripts, save state, and `map.msg` city-name lookups.

```text
[Area 00]
townmap_art_idx=-1
area_name=Arroyo
world_pos=473, 313
start_state=on
lock_state=off
size=medium
entrance_0=on, 258, 168, Arroyo Village, 0, 20195, 0
```

### Area section keys

| Key | Required | Description |
|---|---|---|
| `townmap_art_idx` | Yes | Interface art index for the town-map image. `-1` means no town-map image. The value is converted to an interface FID. |
| `townmap_label_art_idx` | No | Interface art index for the town-map label. `-1` means no label. |
| `area_name` | Yes | Internal area name stored in the city structure. Display names come from `map.msg`, not from this value. |
| `world_pos` | Yes | Two integers: X and Y world-map marker position in pixels. |
| `start_state` | Yes | `off` or `on`. Initial world-map visibility state. |
| `lock_state` | No | `off` or `on`. Locked areas resist normal visibility changes unless a script forces the change. |
| `size` | Yes | `small`, `medium`, or `large`. Selects the world-map marker size/hit shape. |
| `entrance_N` | No | Town-map entrance definition. Up to 10 are stored by CE. |

For Fallout 2 CE, the loaded area count is checked against the compiled Fallout 2 city enum count of 76. Mods that change the number of areas may require engine support, not only text-file edits.

### Entrance syntax

```text
entrance_N=state, x, y, map_lookup_name, elevation, tile, rotation
```

| Field | Description |
|---|---|
| `state` | `off` or `on`. Hidden entrances are not available from the town map. |
| `x`, `y` | Town-map button/hotspot position. |
| `map_lookup_name` | Name matched against `lookup_name` in `maps.txt`. |
| `elevation` | Destination elevation. `-1` acts as a wildcard when matching an entrance by map/elevation. |
| `tile` | Destination MAP tile number. |
| `rotation` | Destination critter rotation, usually `0` through `5`. |

The first valid entrance can be auto-revealed when entering a city that has no revealed entrance yet. Scripts and map visits can also reveal entrances by map and elevation.

### City names

World-map city names are looked up from `text\<language>\game\map.msg` using message id `1500 + area_index`. Map elevation names use `map_index * 3 + elevation + 200`. Town-map entrance labels are different: CE looks them up from `worldmap.msg` using message id `200 + 10 * area_index + entrance_index`. See [MSG File Format](msg.html).

## data\worldmap.txt

`worldmap.txt` defines the tile grid, terrain, encounter frequencies, and random encounter content. It is the densest of the three files and has the most historical comments that do not map directly to parsed CE behavior.

### [data]

```text
[data]
Forced=100%
Frequent=38%
Common=22%
Uncommon=12%
Rare=4%
None=0%
terrain_types=Desert:1, Mountain:2, City:1, Ocean:1
```

| Key | Description |
|---|---|
| `none`, `rare`, `uncommon`, `common`, `frequent`, `forced` | Encounter frequency values. Percent signs are tolerated because integer parsing stops after the leading number. |
| `terrain_types` | Comma-separated `name:difficulty` list. The loader lowercases the names and stores the difficulty integer. |

The source reads frequency keys in the fixed order `none`, `rare`, `uncommon`, `common`, `frequent`, `forced`, stopping that loop at the first missing key. Keep all six for clarity. `terrain_short_names` appears in some original comments/transcripts, but it is not read by the CE loader.

### Terrain difficulty

The terrain difficulty value affects travel speed. During world-map walking, the current subtile's terrain difficulty is clamped to at least `1` and used to decide how often the party advances a pixel. Larger values slow travel. The Pathfinder perk reduces the game-time increment, not the text value itself.

### [Random Maps: terrain]

```text
[Random Maps: desert]
map_00=Desert Encounter 1
map_01=Desert Encounter 2
```

The section name uses the lowercased terrain name as stored from `terrain_types`. Each `map_NN` value is matched against `maps.txt` `lookup_name`. If an encounter table entry and table do not specify their own map list, the engine selects from the current terrain's random map list.

CE stores 20 map slots per terrain. Validators should reject entries beyond `map_19` and should warn when a terrain used by subtiles has no random maps, because fallback encounter map selection can then fail.

### [Tile Data]

```text
[Tile Data]
num_horizontal_tiles=4
```

`num_horizontal_tiles` defines the width of the world-map tile grid. The height is derived from the number of loaded `[Tile N]` sections divided by this width. Each whole tile is `350 x 300` pixels and contains a `7 x 6` grid of `50 x 50` subtiles.

### [Tile N]

```text
[Tile 0]
art_idx=339
encounter_difficulty=0
walk_mask_name=wrldmp00
0_0=Ocean,Fill_W,None,None,None,Fish_O
0_1=Ocean,Fill_W,None,None,None,Fish_O
```

| Key | Required | Description |
|---|---|---|
| `art_idx` | Yes | Interface art list index for the visible world-map FRM tile. |
| `encounter_difficulty` | No | Modifier added to the Outdoorsman detection check for random encounters on this tile. |
| `walk_mask_name` | No | Base name of a `data\*.msk` walk mask. See [MSK File Format](msk.html). |
| `row_column` | Yes | Subtile definition. Every key `0_0` through `6_5` is required. |

Tile sections are read from `[Tile 0]` upward until `art_idx` is missing. A missing subtile key inside a loaded tile is fatal.

### Subtile syntax

```text
row_column=terrain, fill, morning_frequency, afternoon_frequency, night_frequency, encounter_table
```

| Field | Description |
|---|---|
| `terrain` | Terrain name from `terrain_types`. |
| `fill` | Fog fill token: `no_fill`, `fill_n`, `fill_s`, `fill_e`, `fill_w`, `fill_nw`, `fill_ne`, `fill_sw`, or `fill_se`. |
| `morning_frequency` | One of the six frequency names from `[data]`. |
| `afternoon_frequency` | One of the six frequency names from `[data]`. |
| `night_frequency` | One of the six frequency names from `[data]`. |
| `encounter_table` | `lookup_name` from an `[Encounter Table N]` section. |

The coordinate in the key is `x_y`: X/subtile column `0..6`, then Y/subtile row `0..5`. CE's loop creates keys in that order: `0_0`, `1_0`, ... `6_0`, then `0_1`, and so on.

### Encounter tables

```text
[Encounter Table 52]
lookup_name=SRNRRN_M
maps=Mountain Encounter 1, Mountain Encounter 2
enc_00=Chance:8%,Enc:(4-7) KLA_Trappers
enc_01=Chance:2%,Counter:1,Special,Map:Special Toxic Encounter,Enc:Special1, If(Player(Level) > 5)
```

| Key/token | Description |
|---|---|
| `lookup_name` | Name referenced by tile subtiles. |
| `maps` | Optional list of up to 6 map lookup names. Used before terrain random maps when an entry does not name a specific map. |
| `enc_NN` | Sequential encounter table entries. CE accepts up to 40 entries per table. |
| `Chance:N` | Relative entry weight. The values do not need to sum to 100. |
| `Counter:N` | Limited-use counter. `-1` is the default unlimited state; `0` disables the entry; positive values decrement when selected. |
| `Special` | Marks the entry as a special encounter. Special entries can reveal/move the related area marker. |
| `Map:name` | Specific encounter map lookup name. If absent, the table map list or terrain map list is used. |
| `Scenery:name` | Optional scenery density token: `none`, `light`, `normal`, or `heavy`. Default is `normal`. |
| `Enc:...` | One or more encounter group references, described below. |
| `If(...)` | Optional condition expression. |

When selecting a random encounter, CE filters entries whose conditions fail or whose counter is `0`. It sums the remaining `Chance` values, adjusts the roll with Luck, Explorer, Ranger, Scout, and game difficulty, then selects a weighted entry. If the selected entry has a positive counter, the counter is decremented and later saved in `worldmap.dat`.

### Enc: subentries

```text
Enc:(4-7) KLA_Trappers
Enc:(3-6) KLA_Trappers AND (4-5) KLAD_Caravan FIGHTING
Enc:player
```

An `Enc:` clause contains up to six subentries in CE's runtime structure. Each subentry can start with a count range, then an encounter group name, then an optional situation token.

| Part | Description |
|---|---|
| `(min-max)` | Optional count range. Defaults to `1-1`. |
| Group name | Name of an `[Encounter: name]` section, loaded on demand. The special name `player` is stored as `-1`. |
| Situation | `nothing`, `ambush`, `fighting`, or `and`. |

Classic data often writes phrases such as `AMBUSH Player`. The CE parser stores the situation token and does not store the trailing target word from that phrase.

### Encounter groups

```text
[Encounter: KLA_Trappers]
type_00=pid:16777418, item:7(wielded), item:(0-10)41, script:484
type_01=ratio:60%, pid:16777419, item:7(wielded)
team_num=17
position=wedge, spacing:2
```

Encounter groups describe what objects or critters are spawned when an encounter table subentry references the group.

| Key/token | Description |
|---|---|
| `type_NN` | Sequential group entries. CE's runtime structure has room for 10 entries. |
| `ratio:N` | Spawn this entry as a percentage of the requested group count. Without `ratio`, the entry spawns a single object. |
| `dead,` | Marks a critter entry as dead at encounter setup. |
| `pid:N` | PID to create. `0` is normalized to `-1`. |
| `distance:N` | Per-entry placement distance override. For surrounding formations, `0` means use Perception plus a small random offset. |
| `tilenum:N` | Fixed MAP tile placement override. |
| `item:N` | Item PID to add to the spawned object. |
| `item:(min-max)N` | Item PID with random quantity. |
| `(wielded)`, `{wielded}`, `(worn)`, `{worn}` | Marks the item as equipped. CE currently calls the wield path for equipped items. |
| `script:N` | Script index override. CE passes `N - 1` to the script instance creation helper. |
| `if(...)` | Optional entry condition. `enctr(num_critters)` conditions can use the current requested group count here. |
| `team_num` | Team number applied to critter PID entries in the group. |
| `position` | Formation token, with optional `spacing:` and `distance:`. |

CE reads `team_num` without checking whether the key was present before applying it to critter entries. Vanilla data generally supplies it where needed; authoring tools should warn when a group with critter PIDs lacks `team_num`.

### Formations

Valid formation tokens are `surrounding`, `straight_line`, `double_line`, `wedge`, `cone`, and `huddle`. The default formation is `surrounding`, default spacing is `1`, and the group-level `distance:` field is parsed but not used by CE's placement code. Per-entry `distance:` is used for surrounding placement.

For non-surrounding formations, CE uses a random start point from the current MAP when one exists; otherwise it centers formation placement on the player. The engine rejects placement tiles that are blocked or unreachable by pathfinding and tries alternate tiles before giving up.

### Encounter conditions

Encounter table entries and encounter group entries can have up to three conditions joined with `and` or `or`.

| Condition | Runtime value |
|---|---|
| `if(rand(N))` | Random roll from `0` to `100`; succeeds when the roll is not greater than `N`. |
| `if(global(N) == value)` | Global variable `N`. |
| `if(player(level) > value)` | Player level. |
| `if(days_played > value)` | Game time divided by ticks per day. |
| `if(time_of_day < value)` | Current hour, `0` through `23`. |
| `if(enctr(num_critters) < value)` | Current requested encounter group count; useful inside encounter group entries. |

Supported comparison operators are `==`, `!=`, `<`, and `>`. The parser has a special placeholder operator `_` for random checks. Classic comments mention other operators, but they are not implemented by the CE condition evaluator shown in source.

Condition evaluation is simple and has edge cases. It does not build a full expression tree, supports only three condition entries, and the CE source even notes a possible overflow when all three conditions are specified. Keep expressions short and test unusual `or` combinations in-game.

## Message lookups

The trio is tightly coupled to two message files. The data files store structural names and lookup names, while player-facing text usually comes from MSG resources.

| Message file | ID pattern | Use |
|---|---|---|
| `text\<language>\game\map.msg` | `1500 + area_index` | World-map city/location names. |
| `text\<language>\game\map.msg` | `map_index * 3 + elevation + 200` | Map elevation names used by automap/save UI. |
| `text\<language>\game\worldmap.msg` | `200 + 10 * area_index + entrance_index` | Town-map entrance labels. |
| `text\<language>\game\worldmap.msg` | `2998` | Display-monitor prefix when a random encounter is detected. |
| `text\<language>\game\worldmap.msg` | `2999` | Random encounter prompt title. |
| `text\<language>\game\worldmap.msg` | `3000 + 50 * encounter_table_index + encounter_entry_index` | Random encounter prompt/body text. |

The original `city.txt` comments say that city names are kept in the world-map message file. In CE source, area names for the visible tabs/markers are read from `map.msg`; `worldmap.msg` supplies town-map entrance labels and random encounter text.

## Runtime flow

1. The party's world pixel position selects a world-map tile and subtile.
2. The subtile selects terrain, encounter frequency by time of day, and encounter table.
3. The terrain affects travel speed and supplies fallback random maps.
4. The MSK mask, if present, blocks individual world-map pixels.
5. The encounter frequency is adjusted by game difficulty and car travel, then random encounters are rolled.
6. The encounter table filters entries by counters and conditions, then selects a weighted entry.
7. The selected entry chooses a MAP from `Map:`, table `maps`, or terrain random maps.
8. Encounter groups spawn critters/items and attach scripts/items according to their `[Encounter: name]` definitions.
9. Map and city names shown to the player are resolved through `map.msg`, while encounter prompt text is resolved through `worldmap.msg`.

## Validation checklist

- Ensure `[Map NNN]`, `[Area NN]`, `[Encounter Table N]`, and `[Tile N]` sequences have no gaps.
- Validate every map lookup name in `city.txt` and `worldmap.txt` against `maps.txt`.
- Validate every subtile terrain against `terrain_types`.
- Validate every subtile encounter-table name against encounter table `lookup_name` values.
- Validate every tile has all 42 subtile keys `0_0` through `6_5`.
- Warn on missing `walk_mask_name` files and on MSK files whose size is not `13200` bytes.
- Warn when encounter table `maps` lists more than 6 maps, terrain random maps exceed 20 entries, table entries exceed 40 entries, subentries exceed 6, group entries exceed 10, or group item entries exceed 10.
- Warn when `Chance` values leave an encounter table with no selectable entries after conditions and counters.
- Warn on conditions using `<=`, `>=`, `&`, or other operators not handled by CE's evaluator.
- Validate PIDs against PRO lists and scripts against `scripts.lst`.
- Compare `map.msg` coverage against map and area indexes.

## Editing notes

- Keep map indexes stable. They are used by MAP headers, save data, scripts, `city.txt`, `worldmap.txt`, and `map.msg`.
- When adding a world-map location, add the MAP to `maps.txt`, add or update an area in `city.txt`, add message names in `map.msg`, and update scripts that reveal or enter the location.
- When adding terrain or tile art, update `worldmap.txt`, interface FRMs, optional MSK masks, and subtile encounter-table references together.
- When adding an encounter group, validate PIDs, item PIDs, script indexes, team number, and placement formation.
- After changing static world-map configuration, discard stale `worldmap.dat` state when testing from a clean game.
- Preserve comments and original ordering in editors. The engine does not need them, but modders do.

## Related formats

- [Worldmap.dat File Format](worldmap_dat.html) documents the runtime state serialized from these definitions.
- [MSK File Format](msk.html) documents per-tile world-map walk masks referenced by `walk_mask_name`.
- [MAP File Format](map.html) documents destination maps and random encounter maps referenced by `maps.txt`.
- [MSG File Format](msg.html) documents `map.msg` and `worldmap.msg` lookup behavior.
- [PRO File Format](pro.html) documents PIDs used by random encounter groups.
- [LST File Format](lst.html) documents art, prototype, and script lists referenced by these files.
- [FRM File Format](frm.html) documents the visible world-map and town-map art.

## Source references

- [Fallout 2 CE `worldmap.cc`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/worldmap.cc) - `maps.txt`, `city.txt`, and `worldmap.txt` loading, parser limits, random encounter selection, travel, and placement behavior.
- [Fallout 2 CE `worldmap.h`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/worldmap.h) - map flags, city indexes/states, and encounter flags.
- [Fallout 2 CE `config.cc`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/config.cc) - INI-style config parser, semicolon comments, duplicate-key replacement, and integer parsing behavior.
- [Fallout 2 CE `string_parsers.cc`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/string_parsers.cc) - comma-separated token parsing, case normalization, and keyed integer parsing.
- [The Fallout Wiki `Worldmap.txt` File Format](https://fallout.wiki/wiki/Worldmap.txt_File_Format) - classic world-map terrain and encounter reference.
- [The Fallout Wiki `MAPS.TXT` File Format](https://fallout.wiki/wiki/MAPS.TXT_File_Format) - classic map-list notes and field meanings.
- [Vault-Tec Labs `CITY.TXT` File Format](https://falloutmods.fandom.com/wiki/CITY.TXT_File_Format) - classic area and entrance notes.
