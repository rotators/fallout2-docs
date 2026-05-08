---
title: PRO File Format
output: pro.html
description: Fallout PRO binary prototype records, lookup through prototype lists, shared fields, item/critter/scenery layouts, flags, scripts, and editing notes.
---

# PRO File Format

PRO files are binary prototype records. A prototype describes the default properties of an object: its PID, art FID, name and description message id, flags, script id, material, and type-specific data such as weapon damage, critter stats, or door key codes.

Map objects and inventory entries usually store per-instance state separately. The PRO file is the shared template that the engine loads when it needs the object's base data.

## Location and lookup

Prototype files are stored below `proto\<type>`. These paths are relative to `master.dat` or `critter.dat` when packed, or to the game's `data` directory when unpacked. The file name is not derived directly from the PID number at runtime; it is looked up through the matching `.lst` file.

| Object type | PID high byte | Directory | List file | Message file |
|---|---|---|---|---|
| Item | `0` | `proto\items` | `proto\items\items.lst` | `text\<language>\game\pro_item.msg` |
| Critter | `1` | `proto\critters` | `proto\critters\critters.lst` | `text\<language>\game\pro_crit.msg` |
| Scenery | `2` | `proto\scenery` | `proto\scenery\scenery.lst` | `text\<language>\game\pro_scen.msg` |
| Wall | `3` | `proto\walls` | `proto\walls\walls.lst` | `text\<language>\game\pro_wall.msg` |
| Tile | `4` | `proto\tiles` | `proto\tiles\tiles.lst` | `text\<language>\game\pro_tile.msg` |
| Misc | `5` | `proto\misc` | `proto\misc\misc.lst` | `text\<language>\game\pro_misc.msg` |

The low 24 bits of a PID are a 1-based line number in the relevant list file. For example, PID `0x02000031` has object type `2` (scenery) and list index `0x31` (49), so the engine reads line 49 from `proto\scenery\scenery.lst` and opens that file in `proto\scenery`.

When reading a list line, the engine cuts the line at the first space and then removes CR/LF. This allows comments after the filename, but it also means filenames cannot contain spaces.

Many vanilla PRO filenames follow an eight-digit decimal naming convention such as `00000032.pro`, but the list file is authoritative. Tools should use the list entry rather than generating a filename from the PID.

PRO files stored inside save-game directories, for example `data\savegame\slot##\proto`, may be gzip-compressed like saved map objects. If a saved-game PRO does not parse as the binary format described here, try treating it as gzip data first. See [Savegame Structure](savegame.html) for how party-member PRO sidecars are copied into save slots.

## Encoding and byte order

All numeric fields below are stored as big-endian 32-bit signed integers unless the table says `uint8`. The engine's file helpers write a 32-bit integer as high word first and high byte first.

There is no magic number or version field in a PRO file. The engine decides which structure to read from the high byte of the PID stored at the start of the file.

## Type sizes

| Type | Serialized size | Notes |
|---|---|---|
| Item | `0x81`, `0x7D`, `0x7A`, `0x51`, `0x45`, `0x41`, or `0x3D` | Depends on item subtype: armor, drug, weapon, ammo, misc, container, or key. |
| Critter | `0x1A0` in Fallout 2, `0x19C` in Fallout 1 | Fallout 2 adds the native damage type at the end. |
| Scenery | `0x31` or `0x2D` | Door, stairs, and elevator have two subtype fields; ladders and generic scenery have one. |
| Wall | `0x24` | Fixed size. |
| Tile | `0x1C` | Fixed size. |
| Misc | `0x1C` | Fixed size. |

## Common header

Every PRO file starts with these three fields:

| Offset | Type | Field | Description |
|---|---|---|---|
| `0x00` | `int32` | `pid` | Prototype id. High byte is the object type; low 24 bits are the list index. |
| `0x04` | `int32` | `message_num` | Base id in the type's `pro_*.msg` file. |
| `0x08` | `int32` | `fid` | Art FID. See [LST File Format](lst.html) for the packed FID layout and [FRM File Format](frm.html) for the art container it usually resolves to. |

Prototype names and descriptions are not stored as strings in the PRO file. The engine uses `message_num + 0` for the name and `message_num + 1` for the description in the message file associated with the PID's object type. See [MSG File Format](msg.html) for the message-list syntax and lookup behavior.

## Shared object fields

Most object types continue with lighting, flags, extended flags, and a script id. Tiles are smaller and do not have light fields. Misc protos do not store a script id in the PRO file.

| Field | Used by | Description |
|---|---|---|
| `light_distance` | Items, critters, scenery, walls, misc | Light radius emitted by the object. |
| `light_intensity` | Items, critters, scenery, walls, misc | Light intensity. |
| `flags` | All six PRO types | Main object flags. |
| `flags_ext` | All six PRO types | Extended flags. Some values control action availability. |
| `sid` | Items, critters, scenery, walls, tiles | Script id, or `-1` for no script. This refers to [`scripts.lst`](scripts_lst.html). |
| `material` | Items, scenery, walls, tiles | Material type used by engine logic and editor UI. |

## Known shared values

### Main flags

| Flag | Name | Meaning |
|---|---|---|
| `0x00000008` | Flat | Rendered early, just after tiles. |
| `0x00000010` | NoBlock | Does not block the tile. |
| `0x00000800` | MultiHex | Uses more than one hex. |
| `0x00001000` | No Highlight | Does not draw the object highlight outline. |
| `0x00004000` | TransRed | Red transparency mode. |
| `0x00008000` | TransNone | Opaque transparency mode. |
| `0x00010000` | TransWall | Wall transparency mode. |
| `0x00020000` | TransGlass | Glass transparency mode. |
| `0x00040000` | TransSteam | Steam transparency mode. |
| `0x00080000` | TransEnergy | Energy transparency mode. |
| `0x10000000` | WallTransEnd | Changes wall transparency logic. |
| `0x20000000` | LightThru | Light passes through. |
| `0x80000000` | ShootThru | Projectiles can pass through. |

### Materials

| Value | Material |
|---|---|
| `0` | Glass |
| `1` | Metal |
| `2` | Plastic |
| `3` | Wood |
| `4` | Dirt |
| `5` | Stone |
| `6` | Cement |
| `7` | Leather |

### Script ids

Script ids are stored as packed 32-bit values. `0xFFFFFFFF` means no script. For normal object scripts the high byte is the script type and the low 16 bits are the line number in [`scripts\scripts.lst`](scripts_lst.html).

| Script type | Value |
|---|---|
| System | `0` |
| Spatial | `1` |
| Time | `2` |
| Item | `3` |
| Critter | `4` |

Older format notes often describe the packed form as `0x0Y00XXXX`, where `Y` is the script type and `XXXX` is the script-list index. This matches the practical values used by normal item, scenery, and critter scripts.

## File layouts

The following tables list the serialized field order after the common header. They intentionally describe the file order, not the in-memory C struct padding.

### Item

| Order | Field | Notes |
|---|---|---|
| 1 | `0x000C`-`0x001F` | `light_distance`, `light_intensity`, `flags`, `flags_ext`, `sid`. |
| 2 | `0x0020` | `type`. Item subtype: armor, container, drug, weapon, ammo, misc, or key. |
| 3 | `0x0024`-`0x0037` | `material`, `size`, `weight`, `cost`, `inv_fid`. `inv_fid` is the inventory art FID, or `-1`. |
| 4 | `0x0038` | `field_80` (`uint8`). Older notes identify this as an item sound id. |
| 5 | `0x0039` onward | Subtype payload. Depends on `type`. |

Older notes split item `flags_ext` into lower flag bytes plus an attack-mode byte for weapons. Fallout 2 CE reads and writes it as one 32-bit `flags_ext` value, so this split is best treated as a UI/editor interpretation of the same four bytes.

| Extended flag | Meaning |
|---|---|
| `0x00000100` | Big gun. |
| `0x00000200` | Two-handed weapon. |
| `0x00000800` | Can be used. |
| `0x00001000` | Can be used on another object. |
| `0x00008000` | Container can be picked up. |
| `0x08000000` | Hidden or integral item, for example natural claws or robot weapons. |

| Item subtype | Value | Size | Payload fields |
|---|---|---|---|
| Armor | `0` | `0x81` | `ac`, 7 damage resistances, 7 damage thresholds, `perk`, `male_fid`, `female_fid`. |
| Container | `1` | `0x41` | `max_size`, `open_flags`. |
| Drug | `2` | `0x7D` | 3 affected stats, 3 immediate amounts, first duration, 3 first delayed amounts, second duration, 3 second delayed amounts, addiction chance, withdrawal effect, withdrawal onset. |
| Weapon | `3` | `0x7A` | Animation code, max damage, min damage, damage type, two ranges, projectile PID, minimum strength, two AP costs, critical failure type, perk, burst rounds, caliber, ammo type PID, ammo capacity, sound code byte. |
| Ammo | `4` | `0x51` | Caliber, quantity, AC modifier, DR modifier, damage multiplier, damage divisor. |
| Misc | `5` | `0x45` | Power type PID, power type, charges. |
| Key | `6` | `0x3D` | Key code. |

Weapon attack modes are often shown in editors as two 4-bit values: `0` none, `1` punch, `2` kick, `3` swing, `4` thrust, `5` throw, `6` fire single, `7` fire burst, and `8` flame. The lower nibble is attack mode 1 and the upper nibble is attack mode 2.

Weapon animation codes select the critter weapon animation set: `0` none, `1` knife, `2` club, `3` sledgehammer, `4` spear, `5` pistol, `6` SMG, `7` rifle, `8` big gun, `9` minigun, and `10` rocket launcher.

### Critter

Critter PRO files are fixed-size in normal Fallout 2 data: `0x1A0` bytes. After the common header they store:

| Field group | Contents |
|---|---|
| Shared fields | `light_distance`, `light_intensity`, `flags`, `flags_ext`, `sid`. |
| Critter identity | `head_fid`, `ai_packet`, `team_num`. |
| Critter data | `d.flags`, 35 base stats, 35 bonus stats, 18 skills, body type, experience, kill type, native damage type. |

The player character has a special built-in PID, `0x01000000`. For normal critter PRO files, the high PID byte is still `1`.

Critter base and bonus stat arrays each contain 35 values in engine stat order: seven SPECIAL stats, hit points, action points, armor class, unarmed damage, melee damage, carry weight, sequence, healing rate, critical chance, better criticals, seven damage thresholds, nine damage resistances, age, and gender. The skill array contains 18 values in skill order from Small Guns through Outdoorsman.

The critter `kill type` field selects the target table used by [critical hit effects](criticals.html). For example, men, ghouls, robots, and deathclaws each have different critical rows for the same hit location. The player character is special: incoming criticals against `gDude` use a separate player critical table instead of the critter PRO kill type path.

| Critter data flag | Meaning |
|---|---|
| `0x00000002` | Can barter. |
| `0x00000020` | Cannot be stolen from. |
| `0x00000040` | Does not drop items. |
| `0x00000080` | Cannot lose limbs. |
| `0x00000100` | Corpse does not age away. |
| `0x00000200` | Does not heal damage over time. |
| `0x00000400` | Invulnerable. |
| `0x00000800` | Leaves no dead body. |
| `0x00001000` | Special death behavior. |
| `0x00002000` | Melee attack can be used at range. |
| `0x00004000` | Cannot be knocked down. |

### Scenery

| Order | Field | Notes |
|---|---|---|
| 1 | `light_distance`, `light_intensity`, `flags`, `flags_ext`, `sid` | Shared non-tile fields. |
| 2 | `type` | Scenery subtype. |
| 3 | `material` | Named `field_2C` in Fallout 2 CE. |
| 4 | `field_34` (`uint8`) | Stored as one byte. |
| 5 | Subtype payload | Depends on `type`. |

The one-byte scenery field at `0x0028` is commonly treated as a sound id by older tools. Typical sound ids include `0x21`, `0x23`, `0x24`, and values in the range `0x30` through `0x5A`.

| Scenery subtype | Value | Payload fields |
|---|---|---|
| Door | `0` | `open_flags`, `key_code`. |
| Stairs | `1` | `lower_tile`, `upper_tile`. |
| Elevator | `2` | Elevator type, elevator level. The type selects the built-in or [external elevator table](elevators.html). |
| Ladder up | `3` | One destination field. |
| Ladder down | `4` | One destination field. |
| Generic | `5` | One generic field. |

For stairs and ladders, destination tile values are packed together with elevation in a single 32-bit value in the traditional documentation. Valid destination tiles are normal map tile numbers. Map-instance data can also carry destination map ids for some scenery objects, so do not assume every travel target is fully described by the PRO file alone.

### Wall, tile, and misc

| Type | Fields after common header | Serialized size |
|---|---|---|
| Wall | `light_distance`, `light_intensity`, `flags`, `flags_ext`, `sid`, `material`. | `0x24` |
| Tile | `flags`, `flags_ext`, `sid`, `material`. | `0x1C` |
| Misc | `light_distance`, `light_intensity`, `flags`, `flags_ext`. | `0x1C` |

Older format notes split wall and scenery `flags_ext` into two 16-bit halves: wall-light type flags and action flags. The serialized file still contains the same four bytes as the 32-bit `flags_ext` field used by the CE source.

| Wall-light value | Meaning |
|---|---|
| `0x0000` | North / South |
| `0x0800` | East / West |
| `0x1000` | North corner |
| `0x2000` | South corner |
| `0x4000` | East corner |
| `0x8000` | West corner |

| Action value | Meaning |
|---|---|
| `0x0001` | Kneel down when using. |
| `0x0008` | Use. |
| `0x0010` | Use on another object. |
| `0x0020` | Look. |
| `0x0040` | Talk. |
| `0x0080` | Pick up. |

## Action behavior

Several common interactions are inferred from the PID type and flags rather than from a single named field:

- An object can be used if extended flag `0x0800` is set, or if it is an item container.
- An object can be used on another object if extended flag `0x1000` is set, or if it is an item drug.
- An object can be talked to if it is a critter, or if extended flag `0x4000` is set.
- Items can normally be picked up, but item containers require extended flag `0x8000`.

## Editing notes

- Keep `pid`, list position, directory, and object type consistent. The engine uses all of them during lookup.
- Changing a PRO filename requires updating the matching `proto\<type>\<type>.lst` line.
- Changing `message_num` changes which name and description are loaded from `pro_*.msg`.
- Script ids refer to `scripts.lst`; inserting or deleting script-list lines can retarget existing protos.
- FID fields use the same packed object-art identifiers described on the [LST page](lst.html), and usually resolve to [FRM](frm.html) art.

## Minimal parser subset

A tool that only needs to enrich MAP objects can often get useful classifications without parsing every subtype payload. The high-value subset is:

- `0x0000`-`0x0014`: PID, message id, FID, light radius, light intensity, and main flags.
- `0x001C`: script id for items, critters, scenery, and walls.
- `0x0020`: item subtype for item PRO files and scenery subtype for scenery PRO files.
- PID high-byte object types `0` through `5`: item, critter, scenery, wall, tile, misc.

This subset is enough to distinguish many MAP object categories and script-bearing object kinds, but it is not enough to fully model item stats, critter combat data, scenery destinations, or other subtype-specific behavior.

## Source References

- [Fallout 2 CE - proto.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/proto.cc)
- [Fallout 2 CE - proto.h](https://github.com/alexbatalov/fallout2-ce/blob/main/src/proto.h)
- [Fallout 2 CE - proto_types.h](https://github.com/alexbatalov/fallout2-ce/blob/main/src/proto_types.h)
- [Fallout 2 CE - db.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/db.cc)

## History

2026-05-01 - Added source-backed local documentation based on Fallout 2 CE by [OpenAI](https://github.com/OpenAI)

Original public documentation: [https://falloutmods.fandom.com/wiki/PRO_File_Format](https://falloutmods.fandom.com/wiki/PRO_File_Format)
