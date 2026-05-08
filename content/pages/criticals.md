---
title: Critical Hit Tables
output: criticals.html
description: Fallout and Fallout 2 critical hit table structure, sfall and CE override modes, hit locations, kill types, damage flags, and editing notes.
---

# Critical Hit Tables

Fallout and Fallout 2 do not ship critical hit tables as a normal loose data file. The vanilla tables are compiled into the executable. sfall and Fallout 2 CE expose them as runtime-editable data through `ddraw.ini` and an optional INI-style override file, commonly named `CriticalOverrides.ini` or another filename selected by `[Misc] OverrideCriticalFile`.

A critical hit table entry decides the damage multiplier, extra damage flags, optional defensive stat check, combat-message id, and alternate message/flags when that stat check fails. It is only used after an attack has already become a critical success; it does not by itself determine the chance to score a critical hit.

## Runtime Shape

The engine stores critical effects as a three-dimensional table indexed by critter kill type, hit location, and effect tier.

| Axis | Count | Meaning |
|---|---|---|
| Kill type | 19 vanilla, 38 in sfall's extended range | Normally read from the target critter's [PRO](pro.html) `kill type` field. |
| Hit location | 9 | Body part used by the attack, with uncalled attacks normalized to torso before the critical table lookup. |
| Effect tier | 6 | The result of the separate critical-effect roll. |
| Data fields | 7 | The fields inside one critical effect record. |

The player character has a separate player table. In CE/sfall table APIs it is addressed with kill type `38`, which is `SFALL_KILL_TYPE_COUNT`, not a normal critter kill type.

## ddraw.ini Controls

Critical table override loading is controlled by `ddraw.ini` under `[Misc]`:

```ini
[Misc]
OverrideCriticalTable=3
OverrideCriticalFile=CriticalOverrides.ini
```

| Mode | Behavior |
|---|---|
| `0` | Use the vanilla built-in executable table only. No sfall corrections and no external override file are applied. |
| `1` | Load an external override file in the original flat section layout. This covers the 19 vanilla kill types plus the player table. |
| `2` | Apply sfall's built-in critical table corrections, but do not read an external override file. Fallout 2 CE defaults to this mode. |
| `3` | Apply sfall's built-in corrections and then read an external override file in the expanded sfall layout. This covers 38 sfall kill-type slots plus the player table. |

If `OverrideCriticalTable` is outside `0..3`, CE falls back to mode `0`. If `OverrideCriticalFile` is empty or the file cannot be read, the built-in table state for the selected mode remains in use.

`[Misc] RemoveCriticalTimelimits` is related but separate. It removes the early-game time gate for random critical successes/failures and for player critical failures. It does not change the table data itself.

## Hit Locations

| Id | Name | Default hit modifier |
|---|---|---|
| `0` | Head | `-40` |
| `1` | Left arm | `-30` |
| `2` | Right arm | `-30` |
| `3` | Torso | `0` |
| `4` | Right leg | `-20` |
| `5` | Left leg | `-20` |
| `6` | Eyes | `-60` |
| `7` | Groin | `-30` |
| `8` | Uncalled | `0` |

Although the table contains uncalled rows, CE converts `HIT_LOCATION_UNCALLED` to torso before the normal attack roll and critical table lookup. The uncalled rows still matter for compatibility with data patches, script APIs, tooling, and source-level table corrections.

## Kill Types

Normal critter kill types come from the critter PRO data. The vanilla ids are:

| Id | Name |
|---|---|
| `0` | Men |
| `1` | Women |
| `2` | Children |
| `3` | Super mutants |
| `4` | Ghouls |
| `5` | Brahmin |
| `6` | Radscorpions |
| `7` | Rats |
| `8` | Floaters |
| `9` | Centaurs |
| `10` | Robots |
| `11` | Dogs |
| `12` | Mantis |
| `13` | Deathclaws |
| `14` | Plants |
| `15` | Geckos |
| `16` | Aliens |
| `17` | Giant ants |
| `18` | Big bad boss |
| `19..37` | sfall extended range. Names and meaning are mod/runtime convention. |
| `38` | Special player table slot. |

## Effect Tier Roll

Once an attack has become a critical success, the game rolls `1..100` and adds the attacker's `STAT_BETTER_CRITICALS` value. Better Criticals adds `+20` through this stat, while the Heavy Handed trait contributes `-30`. The resulting value selects the effect tier:

| Roll after modifiers | Effect index | Common table name |
|---|---|---|
| `<= 20` | `0` | Effect 1 |
| `21..45` | `1` | Effect 2 |
| `46..70` | `2` | Effect 3 |
| `71..90` | `3` | Effect 4 |
| `91..100` | `4` | Effect 5 |
| `> 100` | `5` | Effect 6 |

The critical chance roll is earlier and different. For a normal non-burst attack, CE calls `randomRoll(toHit, criticalChance - hitLocationPenalty)`. The hit-location penalty is negative, so aimed shots increase critical chance by the same magnitude as their accuracy penalty. Burst attacks use a separate spray calculation path.

## Record Fields

Every critical effect record has seven signed integer fields. The key names below are the names used by sfall/CE override files.

| Index | Key | Meaning |
|---|---|---|
| `0` | `DamageMultiplier` | Half-unit damage multiplier. `2` means normal damage, `3` means 1.5x, `4` means 2x, `8` means 4x. The vanilla damage path multiplies by this value and divides by `2`. |
| `1` | `EffectFlags` | Damage flags always applied when this critical tier is selected. |
| `2` | `StatCheck` | SPECIAL/stat id checked for a massive critical side effect, or `-1` for no check. |
| `3` | `StatMod` | Modifier added to the target's stat for the stat check. Positive is easier for the defender to pass; negative is harder. |
| `4` | `FailureEffect` | Additional damage flags applied if the stat check fails. |
| `5` | `Message` | Message id in `combat.msg` used for the normal critical result. |
| `6` | `FailMessage` | Message id in `combat.msg` used instead when the stat check fails. |

The source names the stat-check fields as `massiveCriticalStat`, `massiveCriticalStatModifier`, `massiveCriticalFlags`, and `massiveCriticalMessageId`. The INI keys use the older `FailureEffect` and `FailMessage` names because they are triggered by a failed defensive stat check.

## Stat Ids

The built-in critical tables mainly use Endurance, Agility, and Luck checks, but the engine accepts any stat id passed to `statRoll`. For compatibility, use SPECIAL ids unless a target runtime explicitly supports more.

| Id | Stat |
|---|---|
| `0` | Strength |
| `1` | Perception |
| `2` | Endurance |
| `3` | Charisma |
| `4` | Intelligence |
| `5` | Agility |
| `6` | Luck |
| `-1` | No stat check. |

## Damage Flags

Use the integer flag values below in sfall/CE override files and script APIs. Older executable table articles sometimes show little-endian byte sequences such as `0x01000000` for knockout; that is the byte order seen when patching the original executable data directly, not the integer value used by the INI parser.

| Value | Flag | Meaning |
|---|---|---|
| `0x00000001` | `DAM_KNOCKED_OUT` | Knockout. |
| `0x00000002` | `DAM_KNOCKED_DOWN` | Knockdown. |
| `0x00000004` | `DAM_CRIP_LEG_LEFT` | Cripple left leg. |
| `0x00000008` | `DAM_CRIP_LEG_RIGHT` | Cripple right leg. |
| `0x00000010` | `DAM_CRIP_ARM_LEFT` | Cripple left arm. |
| `0x00000020` | `DAM_CRIP_ARM_RIGHT` | Cripple right arm. |
| `0x00000040` | `DAM_BLIND` | Blind. |
| `0x00000080` | `DAM_DEAD` | Kill immediately. |
| `0x00000400` | `DAM_ON_FIRE` | On-fire/flame animation flag. |
| `0x00000800` | `DAM_BYPASS` | Bypass armor. CE reduces DT and DR to 20% before damage reduction for non-EMP damage. |
| `0x00004000` | `DAM_DROP` | Drop weapon. CE strips this flag when the defender cannot drop items or the held item is hidden. |
| `0x00008000` | `DAM_LOSE_TURN` | Lose next turn/action. |
| `0x00020000` | `DAM_LOSE_AMMO` | Lose ammo. Used by critical failure/random effects. |
| `0x00100000` | `DAM_RANDOM_HIT` | Redirect hit to a random target. Used by critical failure logic. |
| `0x00200000` | `DAM_CRIP_RANDOM` | Randomly choose a cripple effect. |

Multiple flags are combined by addition or bitwise OR. For example, knockdown plus bypass armor is `0x00000002 | 0x00000800 = 0x00000802`.

## Original Flat Override Layout

Mode `1` reads the original flat section layout. It loops over kill types `0..19`, where `19` is remapped internally to the player table slot `38`. Each section represents one kill type, one hit location, and one effect index:

```ini
[c_00_0_0]
DamageMultiplier=4
EffectFlags=0
StatCheck=-1
StatMod=0
FailureEffect=0
Message=5001
FailMessage=5000
```

| Section part | Meaning |
|---|---|
| `c_00` | Kill type `0`, men. |
| `0` | Hit location `0`, head. |
| `0` | Effect index `0`, the `<= 20` tier. |

Every key is optional. Missing keys leave the current table value unchanged, which means mode `1` patches the built-in vanilla table rather than requiring a complete copy of every section.

## Expanded sfall Override Layout

Mode `3` reads the expanded sfall section layout. It loops over kill types `0..38`, where `38` is the player table. A kill-type section enables either selected body parts or the whole kill type, and the body-part section stores prefixed keys for all six effects.

```ini
[c_00]
Enabled=1
Part_0=1
Part_3=1

[c_00_0]
e0_DamageMultiplier=4
e0_EffectFlags=0
e0_StatCheck=-1
e0_StatMod=0
e0_FailureEffect=0
e0_Message=5001
e0_FailMessage=5000
e5_DamageMultiplier=6
e5_EffectFlags=128
e5_Message=5007
```

| Key | Behavior |
|---|---|
| `Enabled=0` | Skip this kill type entirely. |
| `Enabled=1` | Read only hit locations whose `Part_#` key is true. |
| `Enabled=2` or higher | Read all nine hit locations for this kill type without checking `Part_#`. |
| `Part_#` | Boolean selector for one hit location when `Enabled=1`. |
| `e#_KeyName` | One record field for effect index `#`, where `#` is `0..5`. |

Like the flat format, missing field keys leave existing values unchanged. In mode `3`, those existing values already include sfall's built-in corrections because CE applies the corrections before reading the external file.

## Built-in sfall Corrections

Modes `2` and `3` apply a built-in patch set to the vanilla executable table before saving the table as the reset baseline. These corrections mostly repair known table mistakes and Fallout 2 content mismatches.

The CE correction block changes rows for men, children, super mutants, ghouls, brahmin, radscorpions, centaurs, deathclaws, geckos, aliens, giant ants, and the big bad boss. Examples include fixing child leg critical rows that contained unrelated flags, repairing several uncalled-row messages/effects, making some failed stat-check effects match the intended body part, and providing more appropriate big bad boss message ids.

This is why `OverrideCriticalTable=2` should be described as "vanilla plus sfall fixes", not pristine executable data.

## Combat Message Dependency

`Message` and `FailMessage` are ids in `text\<language>\game\combat.msg`. The selected id becomes the attack's `criticalMessageId`, which is then used by combat text generation after damage is computed.

Many vanilla critical messages are grouped by kill type. For example, men use ids around `5000`, women around `5100`, children around `5200`, and so on. This grouping is a data convention, not a parser rule. Custom tables can point at any loaded `combat.msg` id, but missing ids will display lookup fallback text.

## PRO Dependency

For non-player critters, the critical table kill type is read from the target critter's PRO data. Changing a critter's kill type changes which table controls its critical effects. That can be useful for custom anatomy or enemy classes, but it also changes kill-count classification and any other logic using the same PRO field.

The player character does not use the critter PRO kill type path for incoming criticals. CE checks whether the defender is `gDude` and uses the separate player critical table.

## Editing Notes

- Use decimal or parser-supported integer syntax accepted by the target INI parser. Decimal values are safest for classic tools.
- Keep effect indices zero-based in override files: `e0_*` is the first effect tier and `e5_*` is the sixth.
- Use mode `3` when you want sfall fixes plus an override file. Use mode `1` only when you intentionally want the original flat layout without the built-in correction pass.
- Do not copy damage flag values from executable byte-sequence tables unless you convert them to integer flag values first.
- When changing `Message` or `FailMessage`, update [`combat.msg`](msg.html) at the same time.
- When changing a target class, update [critter PRO](pro.html) kill types and the critical table together so the same enemy does not silently move to a different table.

## Sources

- [Fallout 2 CE `combat.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/combat.cc) for the built-in tables, critical effect roll, override loader, sfall corrections, and damage application.
- [Fallout 2 CE `combat_defs.h`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/combat_defs.h) for hit-location and critical-record structure definitions.
- [Fallout 2 CE `proto_types.h`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/proto_types.h) for kill-type ids and the sfall extended kill-type count.
- [Fallout 2 CE `obj_types.h`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/obj_types.h) for damage flag integer values.
- [The Fallout Wiki critical hit table article](https://fallout.wiki/wiki/Critical_Hit_Tables) for the public table legend and vanilla executable-table interpretation.
- [sfall combat scripting documentation](https://sfall.bgforge.net/combat/) for runtime critical-table script APIs.
