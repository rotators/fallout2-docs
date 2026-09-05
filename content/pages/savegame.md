---
title: Fallout 2 Savegame Structure
output: savegame.html
description: Fallout 2 save slot structure, SAVE.DAT header and handler order, map/prototype sidecars, sfall sidecar notes, and Fallout 1 compatibility boundaries.
---

# Fallout 2 Savegame Structure

Fallout 2 savegames are slot directories, not a single archive file. The central file is `SAVE.DAT`, but visited maps, automap data, companion prototype overrides, backups, and sfall/CE extension files can live beside it in the same `SAVEGAME\SLOT##` folder.

`SAVE.DAT` is not a normal Fallout [DAT](dat.html) archive. It is a big-endian binary stream written as a header followed by 27 save/load handler sections. The handler sections have no section id or length prefix in the file; the engine knows the order from its save/load handler table.

Fallout 1 uses the same broad slot concept and the same fixed header shape, but its handler order is not the same as Fallout 2. The detailed handler table and handler descriptions on this page are for Fallout 2 version `1.2R` and Fallout 2 Community Edition unless a Fallout 1 difference is stated explicitly.

## Slot Directory

Save slots are stored below `SAVEGAME\SLOT01` through `SAVEGAME\SLOT10`. Paths are relative to the configured master patches/data directory in the original engine and Fallout 2 CE.

| Path | Description |
|---|---|
| `SAVEGAME\SLOT##\SAVE.DAT` | Main save stream: header, player/global state, worldmap state, party state, queue state, and module-owned runtime data. |
| `SAVEGAME\SLOT##\*.SAV` | Compressed saved-map files copied from temporary `MAPS\*.SAV` files. Names match the map save names listed inside handler 3 of `SAVE.DAT`. |
| `SAVEGAME\SLOT##\AUTOMAP.SAV` | Compressed copy of `MAPS\AUTOMAP.DB`. The size of the uncompressed automap database is also written in handler 3. |
| `SAVEGAME\SLOT##\proto\critters\*.pro` | Compressed saved prototype overrides for party-member critter PIDs. |
| `SAVEGAME\SLOT##\proto\items\*.pro` | Compressed saved prototype overrides for party-member item PIDs, when a party entry points at an item prototype. |
| `SAVEGAME\SLOT##\sfallgv.sav` | Fallout 2 CE/sfall-compatible sidecar for sfall global variables and extension sections. |
| `*.BAK` | Temporary backup files made while overwriting a slot. They are normally removed after a successful save. |

The engine also uses temporary working files outside the slot. Before saving, it writes the current map state under `MAPS\*.SAV`, then compresses those files into the selected slot. When loading, it clears the working `MAPS\*.SAV` files and decompresses the slot's saved maps back into `MAPS\` before loading the current map.

## Compression

The map and prototype sidecar files in a save slot are gzip-compressed. The decompressed map `.SAV` data has the same broad structure as a saved [MAP](map.html) file. Saved party prototype files decompress to normal [PRO](pro.html) data.

`SAVE.DAT` itself is not gzip-compressed and is not a DAT archive. It should be read directly as a binary stream. Most numbers in the engine-owned save stream use the same big-endian file helpers as other Fallout binary formats.

## SAVE.DAT Header

The header is fixed-size in both Fallout 1 CE and Fallout 2 CE: `0x7563` bytes, or decimal `30051`. After the header, the game-specific handler sections follow immediately.

| Offset | Size | Field | Description |
|---|---|---|---|
| `0x0000` | `24` | Signature buffer | Contains `FALLOUT SAVE FILE`. Both CE implementations write a 24-byte buffer and validate only the first 18 bytes. |
| `0x0018` | `2` | Version word 0 | Major version. Both CE implementations write and expect `1`. |
| `0x001A` | `2` | Version word 1 | Minor version: Fallout 1 writes `1`; Fallout 2 writes and expects `2`. |
| `0x001C` | `1` | Release byte | Both CE implementations write ASCII `R`. The complete version is `1.1R` for Fallout 1 and `1.2R` for Fallout 2. |
| `0x001D` | `32` | Character name | Current dude/player name, fixed buffer. |
| `0x003D` | `30` | Save description | User-entered slot description, fixed buffer. The UI limits input to 29 characters plus terminator. |
| `0x005B` | `2` | Real day | Local real-world day of month when saved. |
| `0x005D` | `2` | Real month | Local real-world month, `1..12`. |
| `0x005F` | `2` | Real year | Local real-world year. |
| `0x0061` | `4` | Real time field | CE writes `hour + minute`, matching the original odd field rather than a normal clock encoding. |
| `0x0065` | `2` | Game month | Game date month from the game-time system. |
| `0x0067` | `2` | Game day | Game date day. |
| `0x0069` | `2` | Game year | Game date year. |
| `0x006B` | `4` | Game time | Current game time in engine ticks. |
| `0x006F` | `2` | Elevation | Current elevation when saved. |
| `0x0071` | `2` | Map index | Current map index from the map system. |
| `0x0073` | `16` | Current map save filename | Current map header name converted to a `.SAV` filename. |
| `0x0083` | `29792` | Preview image | Load/save thumbnail: `224 * 133` bytes of 8-bit indexed pixels. |
| `0x74E3` | `128` | Reserved | Both CE implementations write zero bytes and skip them when loading. |

The preview is captured from the isometric window and stored as raw indexed pixels. It is not an FRM, RIX, or image file with its own header.

The final offsets follow directly from the write order in both CE implementations: the fields before the preview occupy `0x83` bytes, the preview occupies `224 * 133 = 0x7460` bytes, and the reserved block therefore begins at `0x83 + 0x7460 = 0x74E3`. Adding the final `0x80` reserved bytes places the first handler byte at `0x7563`.

## Fallout 1 Compatibility

Fallout 1 and Fallout 2 use the same `0x7563`-byte header layout, with the version difference noted above. Their `SAVE.DAT` bodies must not be decoded with the same handler table.

Handler slots `0` through `14` have the same broad sequence in Fallout 1 CE and Fallout 2 CE. Beginning at slot `15`, Fallout 1 stores the queue immediately, while Fallout 2 postpones the queue until slot `24`. This shifts the intervening sections by one position. The code-level save-handler comparison is:

| # | Fallout 1 CE | Fallout 2 CE |
|---|---|---|
| `15` | `queue_save` | `traitsSave` |
| `16` | `trait_save` | `automapSave` |
| `17` | `automap_save` | `preferencesSave` |
| `18` | `save_options` | `characterEditorSave` |
| `19` | `editor_save` | `wmWorldMap_save` |
| `20` | `save_world_map` | `pipboySave` |
| `21` | `save_pipboy` | `gameMoviesSave` |
| `22` | `gmovie_save` | `skillsUsageSave` |
| `23` | `skill_use_slot_save` | `partyMembersSave` |
| `24` | `partyMemberSave` | `queueSave` |
| `25` | `intface_save` | `interfaceSave` |

Both tables end with an empty/finalization handler at slot `26`, but the bytes in slots `15` through `24` are not interchangeable. A parser should inspect the header version before selecting a handler schema: `1.1R` indicates the Fallout 1 layout and `1.2R` the Fallout 2 layout documented below.

## Fallout 2 SAVE.DAT Handler Order

After the header, a Fallout 2 `SAVE.DAT` contains 27 sections in hardcoded order. The file does not store section names or sizes. Fallout 2 CE logs the size read/written for each handler during debug builds, but those sizes are diagnostic output, not part of the format.

| # | Save handler | Load handler | Contents |
|---|---|---|---|
| `0` | `_DummyFunc` | `_PrepLoad` | No bytes are saved. Loading resets game state, sets the wait cursor, clears the current map name, and restores game time from the header. |
| `1` | `_SaveObjDudeCid` | `_LoadObjDudeCid` | One int32: the player's current combat id (`gDude->cid`). |
| `2` | `scriptsSaveGameGlobalVars` | `scriptsLoadGameGlobalVars` | All game global variables from the active [GAM](gam.html) global table. |
| `3` | `_GameMap2Slot` | `_SlotMap2Game` | Visited map list, map sidecar compression/decompression, companion PRO sidecar copying, and uncompressed automap DB size. |
| `4` | `scriptsSaveGameGlobalVars` | `scriptsSkipGameGlobalVars` | Duplicate copy of the game global variables. Loading reads and discards this second copy. |
| `5` | `_obj_save_dude` | `_obj_load_dude` | The player object in normal object serialization format, followed by the current center tile. |
| `6` | `critterSave` | `critterLoad` | Player critter state: sneak flag plus the dude's critter prototype data. |
| `7` | `killsSave` | `killsLoad` | Kill counts by kill type. |
| `8` | `skillsSave` | `skillsLoad` | Four tagged skill ids. |
| `9` | `randomSave` | `randomLoad` | No real random generator state in CE; both handlers call the roll reset helper. |
| `10` | `perksSave` | `perksLoad` | Perk ranks for each [PARTY.TXT](party_txt.html) description slot and each perk id. |
| `11` | `combatSave` | `combatLoad` | Combat state. If not in combat, only the combat-state word is present. In combat, turn/list/combat AI tracking data follows. |
| `12` | `aiSave` | `aiLoad` | Mutable [AI.TXT](ai_txt.html) packet records for party-member critter prototypes whose AI packet disposition is custom/mutable. |
| `13` | `statsSave` | `statsLoad` | Player character stats array: unspent skill points, level, experience, and related PC stat fields. |
| `14` | `itemsSave` | `itemsLoad` | In CE this shared handler returns without storing meaningful bytes. Older notes list this as unused. |
| `15` | `traitsSave` | `traitsLoad` | Two selected trait ids, with `-1` for unused slots. |
| `16` | `automapSave` | `automapLoad` | One int32 automap flag word. Detailed automap map data is handled separately through `AUTOMAP.SAV`. |
| `17` | `preferencesSave` | `preferencesLoad` | Gameplay/UI/audio preferences such as difficulty, violence, highlighting, volumes, brightness, and mouse sensitivity. |
| `18` | `characterEditorSave` | `characterEditorLoad` | Level-up UI state: last character-editor level and whether a free perk is available. |
| `19` | `wmWorldMap_save` | `wmWorldMap_load` | Serialized worldmap state, also used by [worldmap.dat](worldmap_dat.html). |
| `20` | `pipboySave` | `pipboyLoad` | No meaningful bytes in CE; the shared helper is effectively empty. |
| `21` | `gameMoviesSave` | `gameMoviesLoad` | [MVE](mve.html) movie-seen flags for Pip-Boy video archives and movie availability. |
| `22` | `skillsUsageSave` | `skillsUsageLoad` | Skill-use timestamps: `SKILL_COUNT * SKILLS_MAX_USES_PER_DAY` int32 values. |
| `23` | `partyMembersSave` | `partyMembersLoad` | Active party length, party inventory-count helper, party member object ids, and companion level-up tracking data. |
| `24` | `queueSave` | `queueLoad` | Timed event queue: count, event time, event type, owner object id, and event-type-specific payload. |
| `25` | `interfaceSave` | `interfaceLoad` | Interface bar enabled/hidden state, active hand slot, and combat/end button visibility. |
| `26` | `_DummyFunc` | `_EndLoad` | No bytes are saved. Loading restarts worldmap music, restores the dude name, refreshes UI, and requests combat if loaded into combat. |

## Handler Details

### Global Variables

Handlers 2 and 4 both write the entire global variable array from the game scripts module. CE's load path reads handler 2 into the active array and skips handler 4 into a temporary buffer. The duplicate exists in the original format and should be preserved by writers that want save compatibility.

The number of global variables is the active [GAM](gam.html) global count. If a mod changes `vault13.gam` or the global variable list, old saves can become difficult to interpret because these sections are raw arrays without names.

### Map Sidecars

Handler 3 starts by preparing party members for save, saving the current map into the working `MAPS\` directory, and copying party-member PRO files into the slot. It then scans `MAPS\*.SAV`, writes the count, writes each null-terminated filename, and gzip-compresses each working map save into the slot.

```text
int32 map_save_count
repeat map_save_count:
    char filename[] including trailing NUL
int32 automap_db_uncompressed_size
```

The matching load handler clears working saved maps and saved party PRO overrides, decompresses the slot's party PRO sidecars into `proto\critters` or `proto\items`, decompresses each listed map save into `MAPS\`, decompresses `AUTOMAP.SAV` into `MAPS\AUTOMAP.DB`, reads the stored automap DB size, and finally calls `mapLoadSaved` for the current map filename stored in the header.

The filename reader used by CE's load path accepts at most 14 bytes before the terminator. This matches short DOS-style map save names such as `ARROYO.SAV`. Long saved-map names are unsafe even if the filesystem allows them.

### Player Object

Handler 5 serializes the player object using the same object serializer used by MAP object lists. Before saving, CE temporarily clears the dude object's `OBJECT_NO_SAVE` flag and temporarily sets the dude script id to `-1`. After object serialization it restores those runtime fields and writes one extra int32: `gCenterTile`.

On load, CE reads a temporary object, copies it over the global dude object, restores the persistent object id, recreates the dude script, restores placement, and reads the center tile. This makes the section closely related to [MAP object serialization](map.html), but with player-specific fixups around scripts and object identity.

### Critter, Perk, Trait, Skill, and Stat State

Handlers 6, 8, 10, 13, 15, 18, and 22 together describe much of the player and companion character state:

| Handler | Fields |
|---|---|
| `6 critterSave` | One sneak flag plus the dude's critter prototype data. Older docs describe this as matching critter PRO data beginning at the critter-stat payload. |
| `8 skillsSave` | `NUM_TAGGED_SKILLS`, normally four int32 skill ids. |
| `10 perksSave` | `gPartyMemberDescriptionsLength * PERK_COUNT` int32 rank values. |
| `13 statsSave` | `PC_STAT_COUNT` int32 values. In normal Fallout 2 this includes unspent skill points, level, experience, and derived PC-level bookkeeping. |
| `15 traitsSave` | Two int32 trait ids. |
| `18 characterEditorSave` | One int32 last-level value plus one byte free-perk flag. |
| `22 skillsUsageSave` | Skill-use history array, used for per-day limitations such as First Aid and Doctor use. |

Many visible stats are not stored in just one place. For example, level and experience are PC stat values, base and bonus SPECIAL values are part of critter prototype data, perk ranks are stored per party-member description slot, and temporary radiation/poison/drug behavior can also involve queued events.

### Combat

Handler 11 always writes the combat-state word. If the game is not in combat, that is the whole section. If combat is active, CE writes turn/free-move/experience fields, combatant counts, the dude combat id, each combatant's combat id, and per-combatant AI tracking ids for friendly-dead, last-target, last-item, and last-move state.

On load, if the combat-state word says the game is not in combat, CE only clears invalid `whoHitMe` pointers for critters. If it is in combat, it rebuilds the combat list from current-elevation critters and resolves saved combat ids back to objects.

### Party AI

Handler 12 serializes mutable AI packets associated with party-member critter prototypes. CE iterates `gPartyMemberPids`, finds critter PIDs, gets the prototype's AI packet, and writes the packet only when its disposition marks it as mutable/custom.

Each stored AI packet is the same field sequence described by the [AI.TXT](ai_txt.html) page: packet number, distance/to-hit/HP/aggression settings, combat-message ranges, called-shot preferences, weapon/distance/target/drug/run modes, and three chem desire values. The saved data has no per-record count; the reader must reproduce the same party-member iteration to know how many packets to read.

### Worldmap State

Handler 19 writes the same worldmap state handled by [worldmap.dat](worldmap_dat.html). CE writes the Frank Horrigan flag, current area, world coordinates, visible encounter icon state, encounter ids, car state, city records, entrance visibility states, tile/subtile fog states, and special encounter records.

The save stream does not include the whole editable [worldmap text config trio](worldmap_config.html). It stores runtime state over the configuration data loaded from `worldmap.txt`, `city.txt`, and related message/list files.

### Party Members

Before map sidecars are written, CE prepares party members by clearing `OBJECT_NO_REMOVE` and `OBJECT_NO_SAVE` on non-player party objects and clearing removed/no-save script flags. After copying the map and prototype sidecars, CE restores those flags.

Handler 23 itself writes:

```text
int32 active_party_member_count
int32 party_member_item_count
repeat active_party_member_count - 1:
    int32 party_member_object_id
repeat party_description_count - 1:
    int32 level
    int32 num_level_ups
    int32 is_early
```

The player is party member 0 and is not written in the object-id loop. On load, object ids are resolved against loaded map objects. If an object cannot be found, CE has fallback/recovery behavior, but missing party objects are still a strong sign that the saved map/object sidecars and the party section disagree.

### Queue Events

Handler 24 stores the event queue as a count followed by queue records:

```text
int32 event_count
repeat event_count:
    uint32 event_time
    int32 event_type
    int32 owner_object_id_or_-2
    event-specific payload
```

The payload depends on the event type's registered write/read functions. This is where delayed script events, radiation/poison/drug events, explosion actions, and other scheduled runtime behavior can be preserved. An external parser that cannot decode a particular event type cannot safely skip it unless it knows that event type's payload size.

### Interface, Movies, Preferences, Automap Flags

These handlers are small but useful for save editors:

| Handler | Stored data |
|---|---|
| `16 automapSave` | One automap flag word. The heavy automap database is `AUTOMAP.SAV`. |
| `17 preferencesSave` | 13 int32 gameplay/UI settings, one float text-base-delay, four int32 volume values, one float brightness, and one float mouse sensitivity. |
| `21 gameMoviesSave` | `MOVIE_COUNT` bytes of movie-seen flags. |
| `25 interfaceSave` | Two booleans for interface bar state, one int32 current hand, and one boolean for combat/end button visibility. |

## sfallgv.sav

Fallout 2 CE writes an optional `sfallgv.sav` file after `SAVE.DAT`. In CE, the file begins with sfall global-variable data through `sfall_gl_vars_save`. CE then writes placeholder zero sections for several sfall-compatible extension blocks: next object id, added years, fake traits, fake perks, fake selectable perks, old array data, new array data, and drug pid data.

When loading, CE only reads the sfall global variables and silently ignores the remaining sections. Full sfall builds may use more of this file. Treat it as a sidecar extension format, not as a section inside `SAVE.DAT`.

## Save Overwrite and Backups

When overwriting an existing slot, CE first backs up `SAVE.DAT` as `SAVE.BAK`, renames existing slot `.SAV` files to `.BAK`, and copies `AUTOMAP.SAV` to `AUTOMAP.BAK` if it exists. If the save fails, it erases the partial save and restores the backups. If the save succeeds, the backup `.BAK` files are removed.

This means a slot can briefly contain both current and backup files during an interrupted save. Tools should avoid treating stray `.BAK` files as authoritative save content unless they are deliberately implementing recovery.

## Clean MAP vs Saved SAV

A saved map sidecar is a compressed runtime map snapshot. It preserves live objects, inventories, script records, map variables, local script variables, object flags, queued state, door/container state, critter combat data, and other values described on the [MAP page](map.html). Clean mapper-authored `.MAP` files describe initial state; saved `.SAV` maps describe state after gameplay.

The main `SAVE.DAT` map handler only lists and copies those sidecar files. The actual object/script/tile/map-variable payload is inside the decompressed saved MAP data.

## Compatibility Notes

- Do not use the Fallout 2 handler table for a Fallout 1 `1.1R` save. The handler order diverges at slot `15`.
- Do not reorder the 27 handler sections. There are no section tags in `SAVE.DAT`.
- Preserve the duplicate global-variable section unless targeting an engine that explicitly removed it.
- Changing [GAM](gam.html) global variable counts can make handlers 2 and 4 misalign.
- Changing [PARTY.TXT](party_txt.html) can change perk-rank, party AI, and party-member section sizes.
- Changing [SCRIPTS.LST](scripts_lst.html) or `local_vars=N` can break saved MAP script records and local-variable layouts.
- Changing [worldmap.txt](worldmap_config.html) or `city.txt` can make handler 19 worldmap state disagree with the newly loaded worldmap configuration.
- Changing PRO list indexes or party-member PIDs can make saved party PRO sidecars decompress to unexpected override paths.
- Editing queued events is risky unless every event type payload is decoded.
- Saved maps and saved prototypes are gzip-compressed sidecars; editing `SAVE.DAT` alone may not change the visible in-world state.

## Practical Parser Strategy

1. Open `SAVE.DAT` as an uncompressed binary file.
2. Read and validate the fixed `0x7563`-byte header.
3. Check the header version and select the matching game schema. The handler table below applies to Fallout 2 `1.2R`, not Fallout 1 `1.1R`.
4. Decode handler sections in exact engine order.
5. For handler 3, collect the saved map filenames and inspect the corresponding compressed `.SAV` files in the slot.
6. Decompress saved map files before parsing them with the [MAP format](map.html).
7. Decompress saved party PRO files before parsing them with the [PRO format](pro.html).
8. Load or model the same supporting tables as the game: [GAM](gam.html), [PARTY.TXT](party_txt.html), [AI.TXT](ai_txt.html), [SCRIPTS.LST](scripts_lst.html), worldmap config, and message files.
9. Treat unknown or module-owned sections as runtime state, not as independent named resources.

## Source Code Map

| Source symbol | Role |
|---|---|
| `LOAD_SAVE_SIGNATURE`, `LoadSaveSlotData` | Header signature and UI/header metadata fields. |
| `lsgSaveHeaderInSlot`, `lsgLoadHeaderInSlot` | Exact fixed header layout. |
| `_master_save_list`, `_master_load_list` | Hardcoded 27-section `SAVE.DAT` handler order. |
| `lsgPerformSaveGame`, `lsgLoadGameInSlot` | Top-level save/load flow, backup handling, and `sfallgv.sav` sidecar handling. |
| `_GameMap2Slot`, `_SlotMap2Game` | Map sidecar list, gzip copy/decompress behavior, party PRO sidecars, and automap DB sidecar. |
| `_obj_save_dude`, `_obj_load_dude` | Player object serialization and player-specific fixups. |
| `scriptsSaveGameGlobalVars`, `scriptsLoadGameGlobalVars`, `scriptsSkipGameGlobalVars` | Global variable sections and duplicate skip behavior. |
| `wmWorldMap_save`, `wmWorldMap_load` | Worldmap runtime state section. |
| `queueSave`, `queueLoad` | Timed event queue section. |
| `partyMembersSave`, `partyMembersLoad` | Party roster and companion level-up state. |
| `aiSave`, `aiLoad` | Mutable party AI packet state. |

## References

- [Fallout 1 Community Edition loadsave.cc](https://github.com/alexbatalov/fallout1-ce/blob/main/src/game/loadsave.cc)
- [Fallout 1 Community Edition version.h](https://github.com/alexbatalov/fallout1-ce/blob/main/src/game/version.h)
- [Fallout 2 Community Edition loadsave.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/loadsave.cc)
- [Fallout 2 Community Edition loadsave.h](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/loadsave.h)
- [Fallout 2 Community Edition version.h](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/version.h)
- [Fallout 2 Community Edition object.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/object.cc)
- [Fallout 2 Community Edition scripts.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/scripts.cc)
- [Fallout 2 Community Edition worldmap.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/worldmap.cc)
- [Fallout 2 Community Edition party_member.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/party_member.cc)
- [Fallout 2 Community Edition queue.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/queue.cc)
- [SAVE.DAT File Format - The Fallout Wiki](https://fallout.wiki/wiki/SAVE.DAT_File_Format)
- [SAV File Format - Vault-Tec Labs](https://falloutmods.fandom.com/wiki/SAV_File_Format)

## History

2026-07-25 - Verified the fixed header layout against Fallout 1 CE and Fallout 2 CE, corrected the reserved block offset to `0x74E3`, and documented the incompatible Fallout 1/Fallout 2 handler orders.

2026-05-07 - Added slot-level savegame structure, `SAVE.DAT` header, handler order, map/proto sidecars, sfall sidecar notes, and source-backed compatibility guidance.
