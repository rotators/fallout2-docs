---
title: MAP File Format
output: map.html
description: Fallout and Fallout 2 MAP file layout, clean versus saved maps, tiles, scripts, objects, inventory objects, coordinates, dependencies, validation, and writing notes.
toc: auto
---

# MAP File Format

MAP files describe playable locations: header metadata, map variables, floor and roof tiles, active scripts, and placed objects. Fallout maps can contain up to three elevations. Each elevation has a 200 by 200 hex grid for objects and a 100 by 100 square grid for floor and roof tiles.

In Fallout 2, map files are normally loaded from `maps\*.map`. When a save-game version exists, the engine checks for a matching `.sav` map first and loads that instead. The saved map has the same broad structure, but includes runtime state that a clean mapper-authored MAP may not contain. See [Savegame Structure](savegame.html) for the surrounding `SAVE.DAT` and slot sidecar layout.

## Clean maps and saved maps

Clean `.MAP` files and saved `.SAV` map files use the same main section order. The difference is mostly in the meaning and freshness of the stored values: a clean map describes the mapper-authored starting state, while a saved map records the state after scripts, combat, inventory changes, doors, locks, and player interaction have modified the map.

| Area | Clean `.MAP` | Saved `.SAV` map |
| --- | --- | --- |
| Header flags | Saved-map flag `0x00000001` is normally clear. | Saved-map flag is set before the engine writes an in-game map save. |
| `last_visit_time` | Often zero or mapper/default data. | Updated to the current game time when saving, then used for aging and healing logic when returning. |
| Map variables | Often initial values or zero-filled arrays. | Contains runtime `map_var` and script local-variable values. |
| Script records | Can be rebuilt or initialized from map/object script references. | Preserves runtime script fields, local-variable offsets/counts, fixed parameters, and queued/removed state. |
| Object flags | Usually close to PRO defaults plus mapper placement choices. | May include hidden, seen, queued, equipped, open-door, and other runtime bits. |
| Critter payloads | Initial critter state. | Current hit points, radiation, poison, combat maneuver/results, AI packet, team, and last-attacker combat id. |
| Items and inventory | Initial placement, stack sizes, charges, ammo, and container contents. | Current quantities, weapon ammo, charges, equipped state, moved items, and nested inventory contents. |
| Doors and containers | Initial open/locked/jammed state. | Current open, locked, jammed, searched, or script-modified state. |

Fallout 2 CE also treats saved and clean maps differently during load. For example, when the saved-map flag is not set, loaded script `local_vars_count` is cleared and can be recovered later from `scripts.lst`; when the flag is set, saved local variable state is preserved. Static parsers should therefore avoid assuming that zeroed runtime fields mean the format lacks those fields.

Some travel systems reference MAPs indirectly. [Elevator configuration](elevators.html), for example, stores destination map indexes, elevations, and tiles; the map index resolves through `data\maps.txt`, while the target elevation and tile must be valid in the destination MAP.

## File order

The engine reads a MAP file in this order:

1. Map header.
2. Global map variables.
3. Local map variables.
4. Tile data for each elevation present in the map flags.
5. Script lists.
6. Object lists and nested inventory objects.

All integer fields are stored as big-endian values, using the same file helpers described on the [PRO page](pro.html).

## Header

The header is `0xEC` bytes long in Fallout 2 CE. Versions `19` and `20` are accepted; Fallout 1 maps are generally version `19`, and Fallout 2 maps are generally version `20`.

| Offset | Type | Field | Description |
| --- | --- | --- | --- |
| `0x0000` | `int32` | `version` | Map version, usually `19` or `20`. |
| `0x0004` | `char[16]` | `name` | Map file name stored in the header. |
| `0x0014` | `int32` | `entering_tile` | Default starting hex tile. |
| `0x0018` | `int32` | `entering_elevation` | Default starting elevation, `0` through `2`. |
| `0x001C` | `int32` | `entering_rotation` | Default starting rotation, `0` through `5`. |
| `0x0020` | `int32` | `local_vars_count` | Number of local map variables following the global variable array. |
| `0x0024` | `int32` | `script_index` | Map script list index. Fallout 2 CE creates a map script only when this value is greater than zero, then subtracts one for the zero-based `scripts.lst` index. |
| `0x0028` | `int32` | `flags` | Save/elevation flags. |
| `0x002C` | `int32` | `darkness` | Mapper darkness field. Fallout 2 CE writes `1`. |
| `0x0030` | `int32` | `global_vars_count` | Number of global map variables immediately after the header. |
| `0x0034` | `int32` | `map_index` | Worldmap/maps.txt map index. |
| `0x0038` | `uint32` | `last_visit_time` | Game time tick when the map was last visited. |
| `0x003C` | `int32[44]` | `unknown` | Reserved/unknown header data. |

## Map flags

| Flag | Meaning |
| --- | --- |
| `0x00000001` | Saved map state. The engine sets this when saving an in-game `.sav` map. |
| `0x00000002` | Elevation 0 has no map data when set. |
| `0x00000004` | Elevation 1 has no map data when set. |
| `0x00000008` | Elevation 2 has no map data when set. |

Tile data is present only for elevations whose flag is clear. Fallout 2 CE also uses these flags while saving: an elevation with no non-default tiles and no saveable objects is marked absent.

## Variables

Immediately after the header, the file stores `global_vars_count` 32-bit integers, then `local_vars_count` 32-bit integers. Negative counts are treated as zero by Fallout 2 CE before allocation.

Global map variables are the map-wide `map_var` array. Local map variables back script local variables. Saved maps can contain runtime values here; clean mapper maps often contain zero-initialized arrays.

## Tiles

For each present elevation, MAP stores `10000` 32-bit tile entries. Each entry packs one roof tile id and one floor tile id:

| Bits | Meaning |
| --- | --- |
| `31..16` | Roof tile halfword. |
| `15..0` | Floor tile halfword. |

Within each halfword, the low 12 bits are the tile art id and the high 4 bits are tile flags:

| Halfword bits | Mask | Meaning |
| --- | --- | --- |
| `11..0` | `0x0FFF` | Tile art id, resolved through `art\tiles\tiles.lst`. |
| `15..12` | `0xF000` | Tile flags. Preserve these bits when rewriting unless intentionally normalizing the map. |

For rendering or preview purposes, use `roof_id = (entry >> 16) & 0x0FFF` and `floor_id = entry & 0x0FFF`. Tile id `1` is the default/no-tile value used when the engine resets the square grid.

Fallout 2 CE normalizes roof tile data while loading: it keeps the roof art id, keeps the high-nibble roof flags, but clears bit `0x1000` from the roof halfword. The floor halfword is left as read. A read-only parser can report the raw file value; an editor that writes maps should decide explicitly whether it preserves raw tile halfwords or follows CE's normalized in-memory value.

The tile grid is 100 by 100 square cells, not the 200 by 200 hex grid used by objects. Readers should not use object tile coordinates to index this array directly.

## Scripts

The script section stores five script lists in fixed script-type order. Each list begins with a live script count, followed by enough 16-slot extents to hold that many records. Fallout 2 CE computes the number of extents as `ceil(script_count / 16)`.

Deleted script slots are compacted away when saving. This means `script_count` is the number of live records, not necessarily the number of physical records that follow: every stored extent still contains 16 serialized script slots, then two extent trailer integers.

| Script type | Value |
| --- | --- |
| System | `0` |
| Spatial | `1` |
| Timed | `2` |
| Item | `3` |
| Critter | `4` |

| List field | Type | Description |
| --- | --- | --- |
| `script_count` | `int32` | Number of live scripts in this script-type list. If zero, no extents follow for that list. |
| `extent.records` | 16 script records | Each extent always stores 16 record slots, even when the final extent is only partly used. |
| `extent.length` | `int32` | Number of valid slots in this extent. Full extents usually store `16`; the final extent may store a smaller value. |
| `extent.next` | `int32` | Serialized linked-list pointer placeholder. Fallout 2 CE writes `0` and ignores the value when reading. |

Script ids use the packed style described on the [SCRIPTS.LST page](scripts_lst.html). The high byte is the script type, which also controls whether the record has spatial or timed extra fields.

| Offset | Field | Description |
| --- | --- | --- |
| `0x00` | `sid` | Packed script id. |
| `0x04` | `next` | Linked-list field in the original engine; usually not useful to external tools. |
| `0x08` | `built_tile` | Only present for spatial scripts. Packed tile/elevation value. |
| `0x0C` | `radius` | Only present for spatial scripts. Spatial trigger radius. |
| `0x08` | `time` | Only present for timed scripts. Game-time value for the timed script. |

After the optional spatial/timed fields, every script record stores the same 14 integers:

| Field | Description |
| --- | --- |
| `flags` | Runtime script flags. Fallout 2 CE uses `0x08` as the removed/deleted marker while compacting saves. |
| `script_index` | Index into `scripts\scripts.lst`. |
| `program` | Original-engine program pointer slot. Fallout 2 CE writes `0` and discards the value when reading. |
| `owner_id` | Object id owned by this script, or another script owner id depending on script type. |
| `local_vars_offset` | Offset into the map local-variable array. |
| `local_vars_count` | Number of local variables assigned to this script. Fallout 2 CE clears it when the map is not marked as saved state. |
| `return_value` | Last value set by the script return opcode. |
| `action` | Currently executed action. |
| `fixed_param` | Fixed parameter used by queued/timed script actions. |
| `action_being_used` | Action id currently being used. |
| `script_overrides` | Runtime override flag/state. |
| `field_48` | Unknown/runtime field preserved by the file format. |
| `how_much` | Runtime quantity parameter used by some actions. |
| `field_50` | Unknown/runtime field preserved by the file format. |

Record size therefore depends on script type: system, item, and critter records are `16` int32 values (`0x40` bytes), timed records are `17` int32 values (`0x44` bytes), and spatial records are `18` int32 values (`0x48` bytes). To skip a script list safely, skip `ceil(script_count / 16)` extents, not just `script_count` records. The byte size of one extent is `16 * record_size + 8`.

### Script ownership and local variables

There are two related script references in play. A PRO file stores the default script id for an object prototype. A MAP file stores runtime script instances: the object `sid` at offset `0x40` points to a script record in the MAP script lists, and the script record's `owner_id` points back to the object's `id`.

| Place | Field | Meaning |
| --- | --- | --- |
| PRO | `sid` | Default packed script id for objects created from that prototype. The low bits identify an entry in [`scripts\scripts.lst`](scripts_lst.html). |
| MAP header | `script_index` | Map script reference. Fallout 2 CE treats positive values as one-based and stores `script_index - 1` in the created system script record. |
| MAP object | `sid` | Runtime script instance id attached to this placed object, or `-1` when no runtime script is attached. |
| MAP object | `script_index` | Cached script-list index for the object. For critters, CE updates this from the runtime script when creating an instance. |
| MAP script record | `sid` | Runtime script instance id. The high byte is the script type. |
| MAP script record | `script_index` | Zero-based index into [`scripts\scripts.lst`](scripts_lst.html); this selects the `.int` program name. |
| MAP script record | `owner_id` | Object id of the owning object. For spatial/map helper scripts, the engine may create hidden helper objects as owners. |

When CE creates a new object script instance, it allocates a fresh runtime `sid`, copies the script-list index from the PRO script id, and sets `owner_id` to the object's id. For spatial scripts it also initializes `built_tile` from the object's tile/elevation and gives the script a default radius of `3`.

Script local variables are stored in the MAP local-variable array, not inside the script record itself. A script record's `local_vars_offset` is the starting index in that array, and `local_vars_count` is the number of values reserved for the script. The `local_vars=N` metadata in [`scripts.lst`](scripts_lst.html) tells the engine how many locals a script wants when it needs to allocate or re-discover them.

Saved maps preserve local variable values and set the saved-map flag `0x00000001`. For non-saved maps, Fallout 2 CE clears loaded script `local_vars_count`; the script engine can later recover the intended count from `scripts.lst` and allocate fresh zeroed local variables. This is why clean mapper-authored maps and runtime `.sav` maps can differ even when their script records point to the same `.int` files.

## Objects

The object section begins with the total number of saved objects across all elevations. Then, for each of the three elevations, it stores that elevation's object count followed by the serialized objects for that elevation.

| Field | Meaning |
| --- | --- |
| `total_object_count` | Total number of top-level saved objects across all elevations. |
| `elevation_object_count[0]` | Number of top-level saved objects serialized for elevation `0`. |
| `elevation_object_count[1]` | Number of top-level saved objects serialized for elevation `1`. |
| `elevation_object_count[2]` | Number of top-level saved objects serialized for elevation `2`. |

Inventory objects are not counted in the per-elevation object counts. They are serialized recursively under their owner after the owner's base object and subtype payload. Objects with `OBJECT_NO_SAVE` are skipped when the engine writes the object lists.

| Offset | Field | Description |
| --- | --- | --- |
| `0x00` | `id` | Object id. |
| `0x04` | `tile` | Object hex tile, or `-1` for objects not placed on the map. |
| `0x08` | `x` | Pixel x offset. |
| `0x0C` | `y` | Pixel y offset. |
| `0x10` | `sx` | Cached screen x. |
| `0x14` | `sy` | Cached screen y. |
| `0x18` | `frame` | Current FRM frame. |
| `0x1C` | `rotation` | Rotation, `0` through `5`. |
| `0x20` | `fid` | Current art FID. |
| `0x24` | `flags` | Instance object flags. |
| `0x28` | `elevation` | Object elevation. During load the engine also forces this to the current elevation loop. |
| `0x2C` | `pid` | Prototype PID. Use this with [PRO files](pro.html) to enrich the object. |
| `0x30` | `cid` | Combat id, mostly relevant to saved/in-combat state. |
| `0x34` | `light_distance` | Instance light radius. |
| `0x38` | `light_intensity` | Instance light intensity. |
| `0x3C` | `outline` | Outline color/state in saved maps. Clean map reads ignore this value. |
| `0x40` | `sid` | Runtime script id attached to the object. |
| `0x44` | `script_index` | Script index from `scripts.lst`, or `-1`. |

After these base fields, the engine reads object instance data. This part is not determined by the object's FID. It is determined by the object PID and, for items and scenery, by the subtype in the matching [PRO file](pro.html).

### Object flags

The object `flags` field at offset `0x24` is the runtime instance flag word. New objects usually inherit many of these bits from the matching PRO record, but MAP stores the instance value that the engine actually uses for rendering, blocking, saving, and interaction state. Static tools should preserve bits they do not interpret.

| Flag | Name | Meaning |
| --- | --- | --- |
| `0x00000001` | `OBJECT_HIDDEN` | Object is hidden and should not be visible or trigger normal spatial processing. |
| `0x00000004` | `OBJECT_NO_SAVE` | Runtime/system object should not be saved with the map. |
| `0x00000008` | `OBJECT_FLAT` | Flat object; rendered in the flat/pre-roof layer. |
| `0x00000010` | `OBJECT_NO_BLOCK` | Does not block movement on its tile. |
| `0x00000020` | `OBJECT_LIGHTING` | Object participates in light handling. |
| `0x00000400` | `OBJECT_NO_REMOVE` | Runtime/system object should not be freed or removed normally. |
| `0x00000800` | `OBJECT_MULTIHEX` | Multi-hex object. |
| `0x00001000` | `OBJECT_NO_HIGHLIGHT` | Suppresses normal object highlight outline. |
| `0x00002000` | `OBJECT_QUEUED` | Object has or had a queued event. |
| `0x00004000` | `OBJECT_TRANS_RED` | Red translucency mode. |
| `0x00008000` | `OBJECT_TRANS_NONE` | Opaque/no translucency mode. |
| `0x00010000` | `OBJECT_TRANS_WALL` | Wall translucency mode. |
| `0x00020000` | `OBJECT_TRANS_GLASS` | Glass translucency mode. |
| `0x00040000` | `OBJECT_TRANS_STEAM` | Steam translucency mode. |
| `0x00080000` | `OBJECT_TRANS_ENERGY` | Energy translucency mode. |
| `0x01000000` | `OBJECT_IN_LEFT_HAND` | Item is equipped in the left hand. |
| `0x02000000` | `OBJECT_IN_RIGHT_HAND` | Item is equipped in the right hand. |
| `0x04000000` | `OBJECT_WORN` | Item is worn as armor. |
| `0x10000000` | `OBJECT_WALL_TRANS_END` | Wall-transparency end marker used by the transparency system. |
| `0x20000000` | `OBJECT_LIGHT_THRU` | Light passes through the object. |
| `0x40000000` | `OBJECT_SEEN` | Object has been seen/discovered in runtime state. |
| `0x80000000` | `OBJECT_SHOOT_THRU` | Projectiles can pass through the object. |

The six translucency bits from `0x00004000` through `0x00080000` are a mutually meaningful group in engine code. Fallout 2 CE names their combined mask `OBJECT_FLAG_0xFC000`. Open doors are represented on the base object flags as `OBJECT_OPEN_DOOR`, which is `OBJECT_SHOOT_THRU | OBJECT_LIGHT_THRU | OBJECT_NO_BLOCK`.

### Common instance data

Every object stores inventory metadata immediately after the base object record:

| Offset | Field | Description |
| --- | --- | --- |
| `0x48` | `inventory_length` | Number of inventory entries owned by this object. |
| `0x4C` | `inventory_capacity` | Allocated inventory capacity in the saved object data. |
| `0x50` | `inventory_items_ptr` | Serialized pointer placeholder from the original engine; Fallout 2 CE reads it and discards it. |

### Critter instance data

If `PID_TYPE(pid) == 1`, the object continues with critter runtime state:

| Offset | Field | Description |
| --- | --- | --- |
| `0x54` | `flags` | Critter instance flags. |
| `0x58` | `damage_last_turn` | Combat damage tracker. |
| `0x5C` | `maneuver` | Combat maneuver flags/state. |
| `0x60` | `ap` | Current combat action points. |
| `0x64` | `results` | Combat result flags/state. |
| `0x68` | `ai_packet` | Runtime AI packet. |
| `0x6C` | `team` | Runtime team number. |
| `0x70` | `who_hit_me_cid` | Combat id of the critter that last hit this critter, or `-1`. |
| `0x74` | `hit_points` | Current hit points. |
| `0x78` | `radiation` | Current radiation level. |
| `0x7C` | `poison` | Current poison level. |

### Non-critter instance data

All non-critter objects store an additional instance `flags` field at `0x54`. Fallout 2 CE normalizes the uninitialized debug value `0xCCCCCCCC` to `0` when reading. For lockable containers this field uses `0x02000000` for locked and `0x04000000` for jammed. Door lock and jam state uses the door `open_flags` field instead.

| Object kind | Extra fields after `0x54` | Notes |
| --- | --- | --- |
| Item, armor | None. | Only the common inventory metadata and instance flags are stored. |
| Item, container | None. | Lock/jam/open state uses the instance flags. |
| Item, drug | None. | Drug behavior comes from the PRO payload. |
| Item, weapon | `0x58 ammo_quantity`, `0x5C ammo_type_pid`. | Fallout 2 CE repairs invalid clean-map ammo values from the weapon PRO when the map is not a saved map. |
| Item, ammo | `0x58 quantity`. | Current rounds in the stack. |
| Item, misc | `0x58 charges`. | Current charges. |
| Item, key | `0x58 key_code`. | Instance key code. |
| Wall | None. | No additional object data after instance flags. |
| Tile | None. | Tiles are not normally handled as placed object records. |
| Misc | Only exit grids add fields. | Most misc objects stop after instance flags. |

### Scenery instance data

Scenery object payloads depend on the scenery subtype in the PRO file:

| Scenery subtype | Value | Extra fields after `0x54 flags` |
| --- | --- | --- |
| Door | `0` | `0x58 open_flags`. |
| Stairs | `1` | `0x58 destination_built_tile`, `0x5C destination_map`. |
| Elevator | `2` | `0x58 elevator_type`, `0x5C elevator_level`. |
| Ladder up | `3` | Fallout 1/version `19`: `0x58 destination_built_tile`. Fallout 2/version `20`: `0x58 destination_map`, `0x5C destination_built_tile`. |
| Ladder down | `4` | Same as ladder up. |
| Generic | `5` | No extra MAP instance field is read by Fallout 2 CE. |

Door `open_flags` is also used for lock and jam state. Stairs and ladders use packed built-tile values for tile, elevation, and rotation. The destination map field determines whether the destination is on the current map or should be handed to the map-transition system.

### Exit grid instance data

Misc objects whose PID is in the exit-grid range store four additional fields after instance flags:

| Offset | Field | Description |
| --- | --- | --- |
| `0x58` | `map` | Destination map id. |
| `0x5C` | `tile` | Destination tile. |
| `0x60` | `elevation` | Destination elevation. |
| `0x64` | `rotation` | Destination rotation. |

### Map transitions

Stairs, ladders, and exit grids all describe movement, but the engine does not treat their map fields identically. Stairs create a cross-map transition only when `destination_map > 0`; otherwise they move the object on the current map. Ladders create a transition when `destination_map != 0`; otherwise they move the object on the current map.

| Map value | Meaning in transition handling | Notes |
| --- | --- | --- |
| `0` | Usually same-map movement before a transition is created. | If a `MapTransition` is explicitly created with map `0`, Fallout 2 CE converts it to `-2`. |
| `> 0` | Load another map by map index. | The destination tile, elevation, and rotation are then applied after the map load when valid. |
| `-1` | Town map transition in CE's transition handler. | Older notes sometimes describe negative values differently for specific scenery data, so preserve the original value when rewriting. |
| `-2` | World map transition in CE's transition handler. | This value can be produced internally from transition map `0`. |

Exit-grid misc objects store separate `map`, `tile`, `elevation`, and `rotation` fields instead of a packed built-tile value. Scenery stairs and ladders store tile/elevation/rotation in `destination_built_tile` and store the destination map separately.

## Inventory objects

If an object's inventory length is non-zero, inventory entries immediately follow that object. Each entry starts with a quantity, followed by another serialized object. These nested objects use the same object format, but their tile is usually `-1` and their owner is the containing object.

This recursive structure matters for parsers: the elevation object count counts top-level objects for that elevation, not every nested inventory object.

## Coordinates

Object tiles are hex-grid indexes in the 200 by 200 map grid, normally `0` through `39999`. Rotations use six directions: `0` northeast, `1` east, `2` southeast, `3` southwest, `4` west, and `5` northwest.

Spatial scripts, stairs, and ladders use a packed built-tile value instead of separate tile/elevation/rotation fields:

| Bits | Mask | Field | Description |
| --- | --- | --- | --- |
| `25..0` | `0x03FFFFFF` | `tile` | Hex tile number. Normal MAP values use `0` through `39999`. |
| `28..26` | `0x1C000000` | `rotation` | Rotation, shifted right by `26`. Values normally use `0` through `5`. |
| `31..29` | `0xE0000000` | `elevation` | Elevation, shifted right by `29`. Values normally use `0` through `2`. |

In C-like notation: `tile = built_tile & 0x03FFFFFF`, `rotation = (built_tile & 0x1C000000) >> 26`, and `elevation = (built_tile & 0xE0000000) >> 29`. Fallout 2 CE's `builtTileCreate(tile, elevation)` helper only writes tile and elevation, leaving rotation as zero; however, transition code still reads the rotation bits for stairs and ladders.

## Using PRO data

A MAP object carries both `fid` and `pid`. The `fid` tells the renderer which art is currently displayed; the `pid` points to the prototype that supplies object type, subtype, default flags, name and description message ids, item data, critter data, and other base behavior.

For lightweight object enrichment, the most useful PRO fields are the PID high byte, item/scenery subtype at offset `0x20`, script id at offset `0x1C` for script-bearing prototypes, and the common header FID/message fields.

## Inter-file dependencies

A MAP file can be parsed as bytes on its own, but it cannot be fully named, rendered, or semantically classified without other game data. Most fields are indexes into lists or ids that are resolved elsewhere.

| Dependency | Used for | MAP fields that lead there |
| --- | --- | --- |
| `proto\items\items.lst`, `proto\critters\critters.lst`, `proto\scenery\scenery.lst`, `proto\walls\walls.lst`, `proto\tiles\tiles.lst`, `proto\misc\misc.lst` | Resolve a PID's low bits to a PRO filename. | `pid`. |
| `proto\...\*.pro` | Determine object type/subtype payloads, default flags, message ids, FID, material, scripts, critter stats, item data, and scenery behavior. | `pid`. |
| `art\items\items.lst`, `art\critters\critters.lst`, `art\scenery\scenery.lst`, `art\walls\walls.lst`, `art\tiles\tiles.lst`, `art\misc\misc.lst` | Resolve FID art ids to FRM filenames. | `fid`, tile roof/floor ids, PRO FID fields. |
| [`art\...\*.frm`](frm.html) | Render objects, tiles, inventory images, critters, walls, scenery, and animation frames. | Resolved from `fid` and art LST entries. |
| `scripts\scripts.lst` | Resolve script indexes to `.int` script names and read `local_vars=N` metadata. | Header `script_index`, object `script_index`, script record `script_index`, PRO `sid`. |
| `scripts\*.int` | Executable script bytecode used by the engine. | Resolved through `scripts.lst`. |
| `text\english\game\pro_item.msg`, `pro_crit.msg`, `pro_scen.msg`, `pro_wall.msg`, `pro_tile.msg`, `pro_misc.msg` | Object names and descriptions. See [MSG File Format](msg.html). | PRO message ids reached through object `pid`. |
| `data\maps.txt` / worldmap map list | Resolve map indexes used by the header, transitions, stairs, ladders, and exit grids. See [World-map Text Configuration Files](worldmap_config.html). | Header `map_index`, exit-grid `map`, scenery `destination_map`. |
| [`elevators.ini`](elevators.html) / elevator definitions in modern engines | Resolve elevator type and level behavior. | Elevator scenery `elevator_type` and `elevator_level`. |

For a static object index, PRO files are the most important dependency because MAP object payload size depends on PID type and item/scenery subtype. For a visual map preview, art LSTs and [FRM](frm.html) files become equally important. For script analysis, `scripts.lst` is required even if the `.int` bytecode itself is not parsed.

## Minimal parser recipe

1. Read the `0xEC`-byte header and accept only known versions, normally `19` or `20`.
2. Clamp negative `global_vars_count` and `local_vars_count` to zero, matching Fallout 2 CE behavior.
3. Read `global_vars_count` big-endian 32-bit integers, then `local_vars_count` big-endian 32-bit integers.
4. For each elevation `0` through `2`, read `10000` tile entries only when that elevation's map flag bit is clear.
5. Read the five script lists in system, spatial, timed, item, critter order. For each list, read `script_count`, compute `ceil(script_count / 16)` extents, and remember that every extent stores 16 physical script slots plus two trailer integers.
6. Read the object section's total object count, then each elevation's top-level object count and object records.
7. For each object, read the base object fields through `script_index`, then the common inventory metadata.
8. Use the object's `pid` to determine object type. For item and scenery payloads, read the matching PRO file or a cached PRO index to determine the subtype-specific MAP payload.
9. After each top-level or nested object, if `inventory_length` is non-zero, read that many inventory entries. Each entry is a quantity followed by another full serialized object.
10. When rewriting, preserve pointer placeholders, unknown fields, runtime flags, and unrecognized subtype data unless the tool is intentionally normalizing the map.

## Minimal static parser subset

Tools that only need map indexing, object listings, or lightweight previews do not need to interpret every runtime field. They still need to stay byte-aligned, especially across scripts, subtype payloads, and recursive inventory objects.

| Goal | Required parsing | Can usually ignore |
| --- | --- | --- |
| Map metadata | Header, map flags, map index, entrance tile/elevation/rotation, visit time. | Script records, object payloads, inventory contents. |
| Tile preview | Header, elevation flags, present tile arrays, low 12-bit roof/floor tile ids. | Scripts, objects, object payloads, tile flag semantics beyond preservation. |
| Object listing | Object counts, base object records, recursive inventory structure, enough PRO data to skip subtype payloads. | Combat internals, script runtime fields, pointer placeholders, exact animation state. |
| Object enrichment | Object `pid`, `fid`, `sid`, `script_index`, PRO type/subtype, PRO message ids, related LST files. | Most saved-game runtime values unless they are part of the search/index feature. |
| Transition extraction | Misc exit-grid payloads, scenery subtype payloads for stairs/ladders, packed built-tile decoding. | Unrelated item/critter payload details after they have been skipped correctly. |

A practical static reader can treat many fields as opaque while still exposing useful results. The important rule is that opaque does not mean absent: read or skip the correct number of bytes, keep raw values available when rewriting, and use PRO subtype information before deciding how long an object payload is.

## Loader normalization and validation

A parser can either expose raw on-disk values or values after applying the same cleanup Fallout 2 CE performs while loading. Both are useful, but they should not be mixed without saying so.

| Field or section | CE load behavior | Parser/editor note |
| --- | --- | --- |
| Header version | Accepts versions `19` and `20`. | Reject or warn on other versions unless the tool explicitly supports them. |
| Variable counts | Negative global/local variable counts are clamped to zero before allocation. | Report the raw value if validating corrupt maps; use the clamped value for safe reading. |
| Clean-map script locals | When the saved-map flag is clear, loaded script `local_vars_count` is cleared. | Use `scripts.lst` metadata to recover intended local counts for analysis. |
| Tile roof halfword | Roof data is normalized and roof flag bit `0x1000` is cleared in memory. | Rendering tools usually want the normalized art id; preserving editors may want the raw halfword. |
| Object elevation | Object load runs inside an elevation loop and forces the object's elevation to that loop value. | A mismatch between stored object elevation and containing list is suspicious. |
| Non-critter instance flags | `0xCCCCCCCC` is normalized to `0`. | Treat this as an uninitialized/debug fill value, not as meaningful flag bits. |
| Pointer placeholders | Many pointer slots are read and discarded; CE writes some as `0`. | Do not interpret these as file offsets or references. |

Useful consistency checks include: the sum of the three elevation object counts should match `total_object_count`; object elevations should match the elevation list they were read from; normal object tiles should be `-1` or `0..39999`; normal elevations should be `0..2`; rotations should normally be `0..5`; PID/FID/SID high bytes should be in known object or script type ranges; and script list extents should be exactly the number implied by `script_count`.

Inventory recursion should also be bounded by the actual file length and by reasonable tool limits. The engine format is recursive, so a defensive reader should fail gracefully on cyclic-looking or absurdly deep inventory data instead of trusting counts blindly.

## Unknown and preserved fields

Some MAP fields are known to exist and are preserved by the engine, but their complete semantic meaning is not documented here. A reader can expose them as raw values; a writer should keep them unchanged unless it has a specific reason to normalize them.

| Field | Location | What is known | Recommendation |
| --- | --- | --- | --- |
| `field_3C[44]` | Header `0x003C`-`0x00EB` | Reserved/unknown header payload preserved by the header read/write code. | Preserve exactly when rewriting. |
| `program` | Script record shared tail | Original-engine program pointer slot. Fallout 2 CE writes `0` and discards the read value. | Do not treat as an offset or file reference. |
| `field_48` | Script record shared tail | Runtime/unknown script integer preserved in the serialized record. | Expose as raw integer; preserve on rewrite. |
| `field_50` | Script record shared tail | Runtime script field; CE also uses it while finding script run information. | Preserve unless rebuilding scripts intentionally. |
| `outline` | Object base offset `0x3C` | Outline color/state in saved maps. Clean map loading may ignore this value. | Preserve for saved maps and round-trip editors. |
| `inventory_items_ptr` | Object common data offset `0x50` | Serialized pointer placeholder from the original engine; CE reads and discards it. | Do not interpret as a pointer in the file. |
| Extent `next` | Script list extent trailer | Serialized linked-list pointer placeholder. CE writes `0` and ignores the read value. | Write `0` for CE-style output, or preserve raw value for strict round trips. |
| Tile flag bits | High nibble of roof/floor halfwords | Used by the tile system, but not fully described by this MAP page. | Use low 12 bits for art ids; preserve high bits unless normalizing. |

When documenting or implementing support for these fields, keep source-backed behavior separate from interpretation. For example, "CE writes this slot as zero" is known behavior; "the original engine used every possible value this way" would require stronger evidence.

## Writing and rebuilding maps

Writing a MAP file is not just writing the same records back in place. Several counts and flags describe the serialized shape of the file, so they must match the data being written.

| Area | Writer responsibility |
| --- | --- |
| Header variable counts | Set `global_vars_count` and `local_vars_count` to the number of 32-bit values actually written after the header. |
| Saved-map flag | Set or clear `0x00000001` deliberately. Saved maps preserve runtime state; clean maps may have script locals rebuilt from `scripts.lst`. |
| Elevation flags | Set elevation-absent flags only when the elevation has no non-default tiles and no saveable top-level objects. |
| Tile arrays | Write exactly `10000` tile entries for each elevation whose absent flag is clear, and no tile array for elevations whose absent flag is set. |
| Script lists | Recompute each script type's live `script_count`, write enough 16-slot extents, and write each extent's `length` plus trailer. |
| Object counts | Recompute the total and per-elevation top-level object counts. Do not include nested inventory objects in these counts. |
| Object payloads | Use PID and PRO subtype data to choose the correct instance payload shape before writing. |
| Inventory | Write each inventory entry immediately after its owner object as `quantity` followed by a full nested object. |
| Pointer placeholders | Either preserve raw placeholder values for strict round trips, or write CE-style zero placeholders consistently. |

A format-preserving editor should avoid silently converting a saved map into a clean map or vice versa. In particular, normalizing script locals, object flags, outline state, tile flags, or pointer placeholders can be reasonable for a map-cleaning tool, but it should be an explicit operation.

## Editing notes

- Preserve the section order. Later sections cannot be found reliably without reading the counts and flags before them.
- Do not count inventory objects as elevation-level objects; they are nested under their owner.
- Use map flags to decide which elevation tile arrays are present.
- Use object PID plus PRO data for classification. FID alone can be misleading when objects change animation or appearance.
- Saved `.sav` maps contain runtime state. A parser that only targets clean `.map` files should still tolerate save-specific fields when possible.

## Source References

- [Fallout 2 CE - map.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/map.cc)
- [Fallout 2 CE - map.h](https://github.com/alexbatalov/fallout2-ce/blob/main/src/map.h)
- [Fallout 2 CE - obj_types.h](https://github.com/alexbatalov/fallout2-ce/blob/main/src/obj_types.h)
- [Fallout 2 CE - object.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/object.cc)
- [Fallout 2 CE - proto.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/proto.cc)
- [Fallout 2 CE - proto_instance.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/proto_instance.cc)
- [Fallout 2 CE - scripts.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/scripts.cc)
- [Fallout 2 CE - scripts.h](https://github.com/alexbatalov/fallout2-ce/blob/main/src/scripts.h)

## History

2026-05-01 - Added source-backed local documentation based on Fallout 2 CE by [OpenAI](https://github.com/OpenAI)

Original public documentation: [https://falloutmods.fandom.com/wiki/MAP_File_Format](https://falloutmods.fandom.com/wiki/MAP_File_Format)
