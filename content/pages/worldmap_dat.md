---
title: Worldmap.dat File Format
output: worldmap_dat.html
description: Fallout worldmap.dat binary state stream, city state records, subtile fog, encounter counters, static world-map text dependencies, and validation notes.
---

# Worldmap.dat File Format

`worldmap.dat` is the serialized state of the Fallout and Fallout 2 world map. It records where the party and car are, which towns and town-map entrances have been revealed, which world-map subtiles are fogged or visited, and which limited-use encounter entries have changed counters. It does not contain the full world map definition.

The static world map definition is split across text files: `data\maps.txt` defines map indexes and map-level behavior, `data\city.txt` defines towns and entrances, and `data\worldmap.txt` defines terrain, tiles, encounter frequencies, random maps, and encounter tables. See [World-map Text Configuration Files](worldmap_config.html) for the editable source definitions.

## What the file is

| Property | Description |
|---|---|
| File type | Binary runtime state stream. There is no magic number, version field, checksum, or embedded section names. |
| Integer byte order | All stored values are 32-bit big-endian integers. Engine booleans are also written as 32-bit integers: `0` or `1`. |
| Static dependency | The file can only be interpreted safely after the matching `maps.txt`, `city.txt`, and `worldmap.txt` data have been loaded. |
| Variable length | Length depends on the number of loaded cities, each city's entrance count, the number of world-map tiles, and the number of stored encounter counters. |
| Typical location | The engine reads and writes a plain `worldmap.dat` through the normal file layer. Save-game code also serializes the same world-map state into [`SAVE.DAT`](savegame.html). |

Classic documentation describes `worldmap.dat` as a cache, and that is a good mental model. It is small state derived from larger configuration files. If `city.txt` or `worldmap.txt` is changed but an old saved world-map state is reused, the saved state can point at the wrong amount of city, entrance, tile, or encounter data.

## Binary layout

The stream is written as a sequence of big-endian int32 values. Offsets after the fixed header are data-dependent, so this table uses order instead of absolute offsets.

| Order | Field | Description |
|---|---|---|
| `0` | `did_meet_frank_horrigan` | Bool. The one-time Horrigan world-map movie encounter has already fired. |
| `1` | `current_area_id` | Current city/area index, or `-1` when the party is not inside a world-map area. |
| `2` | `world_pos_x` | Party world-map X coordinate in pixels. |
| `3` | `world_pos_y` | Party world-map Y coordinate in pixels. |
| `4` | `encounter_icon_is_visible` | Bool. The flashing random-encounter icon is visible. |
| `5` | `encounter_map_id` | Map index selected for the pending encounter, or `-1`. |
| `6` | `encounter_table_id` | Encounter table index selected for the pending encounter, or `-1`. |
| `7` | `encounter_entry_id` | Entry index inside the selected encounter table, or `-1`. |
| `8` | `is_in_car` | Bool. The party is currently travelling in the Highwayman. |
| `9` | `current_car_area_id` | Area index where the car is parked, or `-1`. |
| `10` | `car_fuel` | Current fuel amount. Fallout 2 CE defines the maximum as `80000`. |
| `11` | `city_count` | Number of city state records that follow. |
| repeat `city_count` | `city_state_record` | Variable-length city state record, described below. |
| after cities | `tile_count` | Number of world-map tile state records that follow. |
| after `tile_count` | `num_horizontal_tiles` | Width of the tile grid in whole world-map tiles. |
| repeat `tile_count` | `subtile_states` | For each tile, 42 int32 subtile states: 6 rows of 7 columns. |
| after tiles | `counter_count` | Number of encounter counter records that follow. |
| repeat `counter_count` | `encounter_counter_record` | Triplet `{encounter_table_index, encounter_entry_index, counter_value}`. |

## City state records

Each city record stores only mutable state. Static art, labels, entrance coordinates, destination maps, and town-map definitions are read from `city.txt`.

| Relative order | Field | Description |
|---|---|---|
| `0` | `x` | World-map X coordinate for the city marker. |
| `1` | `y` | World-map Y coordinate for the city marker. |
| `2` | `state` | City visibility state. Common values are `0` unknown, `1` known, `2` visited, and `-66` invisible. |
| `3` | `visited_state` | Town-map/menu state. The engine uses this separately from `state`; a town can be known on the world map before its town map is fully available. |
| `4` | `entrance_count` | Number of entrance-state values following this city. The static engine capacity is 10 entrances per city. |
| repeat `entrance_count` | `entrance_state` | One int32 per entrance. `0` means off/hidden and `1` means on/available. |

Special encounters can move a city marker at runtime before revealing it. The saved `x` and `y` fields are therefore state, not merely copies of `city.txt`.

## Subtile fog records

The world map is divided into large image tiles. Each tile contains a `7 x 6` grid of subtiles. In the text definition, each subtile is addressed as `row_column`, with rows `0` through `6` and columns `0` through `5`. In the binary state stream, the engine writes them column-first: for each column `0..5`, each row `0..6`.

| State | Meaning |
|---|---|
| `0` | Unknown. Rendered as black/unexplored. |
| `1` | Known. The region has been revealed but is still fogged. |
| `2` | Visited. The region is fully visible. |

The party position is stored in world-map pixels. A subtile is 50 pixels square; a tile is therefore `350 x 300` pixels. The engine marks nearby subtiles while travelling, with a larger reveal radius when the player has the Scout perk.

The coordinate mapping used by the engine is:

```text
tile_index = (x / 350) % num_horizontal_tiles + (y / 300) * num_horizontal_tiles
subtile_x = (x % 350) / 50
subtile_y = (y % 300) / 50
```

## Encounter counters

The final table stores only encounter-table entries whose `counter` value is not `-1`. The counter table is sparse: each saved record names the encounter table and the entry inside that table, then stores the current counter value. This is mainly relevant to special or limited random encounters declared in `worldmap.txt`.

Readers should validate every table index and entry index against the already-loaded `worldmap.txt` encounter tables before applying a counter. The engine code assumes the saved counts and indexes match the current static configuration.

## File size formula

Because all values are int32, the size can be checked after parsing:

```text
44
+ 4
+ sum_for_each_city(20 + entrance_count * 4)
+ 8
+ tile_count * 42 * 4
+ 4
+ counter_count * 12
```

The first `44` bytes are the eleven fixed state fields through `car_fuel`. The following `4` bytes store `city_count`. The two tile count fields take `8` bytes, and the counter count takes `4` bytes. A mismatch usually means the file was read with the wrong static data, the wrong endian order, or a corrupt count.

## data\city.txt

`city.txt` supplies static area definitions. Fallout 2 CE reads sections named `[Area 00]`, `[Area 01]`, and so on until the next required area is missing. For Fallout 2, the loaded area count is expected to match the compiled city count; CE's Fallout 2 city enum contains 76 entries, including normal towns, special encounter markers, fake/special states, and the car-out-of-gas marker.

| Key | Description |
|---|---|
| `townmap_art_idx` | Interface art list index for the town map image. `-1` means no town-map art. |
| `townmap_label_art_idx` | Optional interface art list index for the town-map label. |
| `area_name` | Internal area lookup name. Displayed city names are normally read from `map.msg` with id `1500 + area_index`. |
| `world_pos` | Two integers: world-map X and Y coordinate for the city marker. |
| `start_state` | `off` or `on`. Initial world-map visibility. |
| `lock_state` | Optional `off` or `on`. Locked areas resist normal reveal behavior. |
| `size` | `small`, `medium`, or `large`. Selects the city marker size and hit-test behavior. |
| `entrance_N` | One town-map entrance. The parsed fields are `state`, `x`, `y`, `map_lookup_name`, `elevation`, `tile`, and `rotation`. |

Entrance map names are matched against `lookup_name` values from `data\maps.txt`, not against raw `.MAP` filenames. The state field again uses `off`/`on`.

## data\maps.txt

The world-map system reads `maps.txt` before `city.txt`. It defines the map index namespace used by MAP headers, city entrances, random encounters, scripts, and map loading.

| Key | Description |
|---|---|
| `[Map 000]`, `[Map 001]`, ... | Map sections are read in numeric order until `lookup_name` is missing. |
| `lookup_name` | Symbolic name used by `city.txt`, `worldmap.txt`, and engine helpers. |
| `map_name` | Base MAP filename. The engine lowercases it and appends `.MAP` when resolving by map index. |
| `music` | Optional background music name started when the map loads. |
| `ambient_sfx` | Optional comma-separated `name:chance` list. The engine stores up to six ambient sound definitions per map. |
| `saved` | `yes` or `no`. Controls whether the map is saveable and whether entering it marks the containing area visited. |
| `dead_bodies_age` | `yes` or `no`. Controls corpse aging behavior. |
| `can_rest_here` | Three `yes`/`no` values, one per elevation. |
| `pipboy_active` | `yes` or `no` map flag. CE parses this flag, but the current CE `wmMapPipboyActive` helper returns the vault-suit movie state instead. |
| `automap` | Optional sfall/CE automap display flag. |
| `random_start_point_N` | Random encounter start points. Each entry can contain `elev:` and `tile_num:` values. Rotation defaults to `-1` in CE's start-point structure. |

Fallout 2 CE's source enum names 150 vanilla Fallout 2 map indexes, `0` through `149`, but the runtime map list is still loaded from `data\maps.txt`. Treat the text file as the authoritative ordering for a particular data set.

## data\worldmap.txt

`worldmap.txt` supplies world-map terrain, encounter probabilities, random encounter maps, tile art, mask names, and encounter table definitions.

### [data]

| Key | Description |
|---|---|
| `none`, `rare`, `uncommon`, `common`, `frequent`, `forced` | Encounter-frequency percentage values. The symbolic names are later used by tile subtiles. |
| `terrain_types` | Comma-separated `name:difficulty` list. Names are lowercased by the parser. Each terrain can have a companion `[Random Maps: name]` section. |

### [Random Maps: terrain]

Each terrain section can list up to 20 map lookup names as `map_00`, `map_01`, and so on. These are the candidate MAPs used for normal random encounters on that terrain.

### [Tile Data] and [Tile N]

| Key | Description |
|---|---|
| `num_horizontal_tiles` | Width of the world-map tile grid in whole image tiles. |
| `art_idx` | Interface art list index for the world-map tile FRM. |
| `encounter_difficulty` | Optional modifier applied to the Outdoorsman check for detecting random encounters. |
| `walk_mask_name` | Optional world-map mask name. Masks mark impassable parts of a tile. See [MSK File Format](msk.html). |
| `row_column` | Subtile definition. Required for every `0_0` through `6_5` entry. |

A subtile definition is parsed as:

```text
terrain fill morning_frequency afternoon_frequency night_frequency encounter_table
```

Valid fill tokens are `no_fill`, `fill_n`, `fill_s`, `fill_e`, `fill_w`, `fill_nw`, `fill_ne`, `fill_sw`, and `fill_se`. Fill controls how the fog/visited overlay blends at subtile edges.

### [Encounter Table N]

| Key | Description |
|---|---|
| `lookup_name` | Name used by tile subtiles to select this encounter table. |
| `maps` | Optional list of up to six map lookup names associated with the encounter table. |
| `enc_00`, `enc_01`, ... | Encounter entries. CE accepts up to 40 entries per table. |

An encounter-table entry can contain `chance:`, `counter:`, `special`, `map:`, one or more `enc:` clauses, `scenery:`, and optional `if(...)` conditions. Scenery tokens are `none`, `light`, `normal`, and `heavy`. Encounter situations are `nothing`, `ambush`, `fighting`, and `and`.

The `enc:` clause names an encounter base type, optionally with a count range and situation:

```text
enc:(2-4) gecko ambush
enc:player
```

The special name `player` is accepted as a sub-encounter target and is stored internally as `-1`.

### [Encounter: name]

Encounter base types are loaded on demand from sections named `[Encounter: name]`. Each section has `type_00`, `type_01`, and so on, with optional `team_num` and `position` keys.

| Token/key | Description |
|---|---|
| `ratio:` | Weight or ratio for selecting this critter/object entry inside the encounter base type. |
| `dead,` | Marks spawned critters from this entry as dead. |
| `pid:` | PID to spawn. `0` is normalized to `-1`. |
| `distance:` | Placement distance override. |
| `tilenum:` | Fixed map tile number for placement. |
| `item:` | Item PID to add. A quantity can be written as `(min-max)` before the PID. `{wielded}`, `(wielded)`, `{worn}`, or `(worn)` marks it equipped. |
| `script:` | Script index to attach to the spawned entry. |
| `team_num` | Team number applied to critter PID entries in the encounter. |
| `position` | Formation token: `surrounding`, `straight_line`, `double_line`, `wedge`, `cone`, or `huddle`. Optional `spacing:` and `distance:` values can follow. |

### Encounter conditions

Encounter table entries and encounter base entries can include up to three conditions joined with `and` or `or`. Operators are `==`, `!=`, `<`, `>`, and the special placeholder `_` used by random checks.

| Condition | Meaning |
|---|---|
| `if(rand(N))` | Random percentage-style condition parameter. |
| `if(global(GVAR) == value)` | Global variable condition. |
| `if(player(level) > value)` | Player level condition. |
| `if(days_played > value)` | Game-days elapsed condition. |
| `if(time_of_day < value)` | Current time-of-day condition. |
| `if(enctr(num_critters) < value)` | Condition based on the number of encounter critters. |

The parser is forgiving in some places and brittle in others: it lowercases condition strings, searches for keywords inside comma-separated fragments, and mutates parse buffers in place. Tools should normalize only when they can preserve comments and original text that the engine ignores.

## Runtime behavior

- World-map initialization loads `worldmap.msg`, initializes general state, reads static configuration, marks nearby subtiles, and writes a temporary `worldmap.dat`.
- World-map reset starts the party near the default Arroyo coordinates, clears transient encounter fields, restores car fuel to `80000`, and reinitializes the fog state.
- Entering a saveable map can mark the containing area visited. Town-map entrances can be revealed independently by map and elevation.
- The world-map loop updates travel, car fuel, party healing, game time, fog reveal, and random encounter checks.
- Random encounter frequency is read from the current subtile's morning/afternoon/night frequency token, adjusted by game difficulty and car travel, then checked against Outdoorsman for detection.

## Forced encounters

Forced encounters are part of the world-map runtime API, but the queued forced-encounter request is not written by `wmWorldMap_save`. If a tool is inspecting only `worldmap.dat`, it will see pending random encounter ids and icon state, not the separate transient forced-encounter request.

| Flag | Meaning |
|---|---|
| `0x01` | `NO_CAR`. If the party is in the car, park/update the car area before entering the forced encounter. |
| `0x02` | `LOCK`. Encounter flag parsed by the world-map API; behavior depends on the call path using it. |
| `0x04` | `NO_ICON`. Do not show the blinking encounter icon. |
| `0x08` | `ICON_SP`. Use the special encounter icon style. |
| `0x10` | `FADEOUT`. Fade out directly instead of using the encounter icon path. |

## Messages and names

`worldmap.msg` stores world-map UI and encounter text, including the default random encounter prompt. City and map display names are a separate dependency: `map.msg` supplies map names with `map_index * 3 + elevation + 200` and city names with `1500 + city_index`. See [MSG File Format](msg.html) for the message-list syntax.

## Validation notes

- Read every numeric value as a signed big-endian int32.
- Validate `city_count` against the loaded `city.txt` area count before applying city records.
- Validate every `entrance_count` against the static entrance count and the engine capacity of 10.
- Validate `tile_count` and `num_horizontal_tiles` against the loaded `worldmap.txt` tile grid.
- Require exactly 42 subtile states per tile and allow only known fog states unless you are preserving unknown modded data.
- Validate encounter counter indexes against the loaded encounter tables before writing into them.
- When comparing files, remember that moving a special encounter or revealing an entrance legitimately changes state that originally came from text files.

## Editing notes

- For new world-map content, edit `maps.txt`, `city.txt`, `worldmap.txt`, art, masks, MAP files, PROs, scripts, and messages first. Rebuild or discard stale `worldmap.dat` state afterward.
- Keep map indexes stable once saves exist. MAP headers, city entrances, encounter tables, scripts, and `map.msg` all depend on the same ordering.
- Do not treat old absolute-offset tables as universal. They describe one generated file for one static data set; this format is count-driven.
- When importing a save or state file into a different mod, report count and index mismatches instead of silently truncating or extending records.
- Use `worldmap.dat` edits for state repair, testing revealed areas, car location/fuel, encounter flags, and fog state. Use text-file edits for real world-map design.

## Inter-file dependencies

| File | Used for |
|---|---|
| `data\maps.txt` | Map indexes, MAP filenames, music, ambient sound, save/rest flags, start points, and lookup names used by city and encounter definitions. |
| `data\city.txt` | World-map areas, town-map art, marker positions, city size, visibility defaults, entrances, and entrance destination maps. |
| `data\worldmap.txt` | Encounter frequencies, terrain types, world-map tile art/masks, subtile terrain/fog fill rules, random map pools, and random encounter definitions. |
| `text\<language>\game\worldmap.msg` | World-map UI and encounter text. |
| `text\<language>\game\map.msg` | City and map display names. |
| `art\intrface\intrface.lst` and FRMs | World-map tile art, city circles, town-map art, labels, and interface pieces. |
| [MSK files](msk.html) | Walk masks for impassable world-map regions, named by `walk_mask_name`. |
| `maps\*.map` | Destination maps for city entrances and random encounters. |
| `proto\...\*.pro` and PRO LSTs | Encounter spawned objects and items through PIDs. |
| `scripts\scripts.lst` and `scripts\*.int` | Scripts attached to random encounter entries or reached by destination maps. |
| Sound and music assets | Map background music and ambient SFX named from `maps.txt`. |

## Related formats

- [World-map Text Configuration Files](worldmap_config.html) documents `maps.txt`, `city.txt`, and `worldmap.txt`.
- [MAP File Format](map.html) uses map indexes that resolve through the world-map map list.
- [MSG File Format](msg.html) documents `worldmap.msg` and `map.msg` message lookup behavior.
- [MSK File Format](msk.html) documents the per-tile world-map walk masks referenced by `walk_mask_name`.
- [PRO File Format](pro.html) documents PIDs and prototypes used by random encounter spawn definitions.
- [LST File Format](lst.html) documents the art, prototype, and script list files that world-map definitions reference.
- [FRM File Format](frm.html) documents the interface art frames used for world-map tiles and town maps.

## Source references

- [Fallout 2 CE `worldmap.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/worldmap.cc) - world-map static configuration loading, binary save/load order, city/map helpers, subtile fog, and encounter parsing.
- [Fallout 2 CE `worldmap.h`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/worldmap.h) - map flags, city states, Fallout 2 city/map indexes, encounter flags, and public world-map APIs.
- [Fallout 2 CE `db.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/db.cc) - big-endian int32 and bool read/write helpers used by the world-map state stream.
- [The Fallout Wiki `Worldmap.txt` File Format](https://fallout.wiki/wiki/Worldmap.txt_File_Format) - human-readable notes on the editable terrain and encounter definition file.
- [Vault-Tec Labs `Worldmap.dat` File Format](https://falloutmods.fandom.com/wiki/Worldmap.dat_File_Format) - older cache/table description for vanilla generated data.
