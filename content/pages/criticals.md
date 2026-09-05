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

## Weapon Critical Failures

The following describes Fallout 2's executable failure table as represented by Fallout 2 CE. It contains seven rows and five severity columns. Each entry is a single damage-flag bitmask; there are no per-entry damage multipliers, defensive stat checks, or message ids. The `FailureEffect` field in the successful-critical override format above has a different purpose: it applies when the defender fails a stat check after being critically hit.

### Selecting the Table

The attacking weapon's [PRO](pro.html) field `crit_fail_table` (CE: `criticalFailureType`) selects row `0..6`. This is a signed big-endian 32-bit field at weapon PRO offset `0x0061`. It belongs to the weapon, so the defender's kill type and hit location do not select this table.

`weaponGetCriticalFailureType` returns `-1` for a null weapon, and the failure handler maps `-1` to row `0`. A weapon PRO containing `-1` receives the same fallback. Other out-of-range values are not clamped before indexing in the inspected CE handler; editors should accept only `-1` or `0..6`. Table numbers should not be inferred from weapon animation, caliber, or damage type.

### Failure Chance and Severity

First, an attack must become a critical failure. In CE's ordinary `randomRoll` path, a miss with margin `roll - toHit` has a second percentage roll against the integer quotient `(roll - toHit) / 10`. This is not a fixed 10% chance per missed attack. Combat also has other routes, including Jinxed promoting an ordinary failure with a 50% roll.

Once the failure handler runs, it rolls a fresh percentage die for severity:

```text
severity = random(1, 100) - 5 * (attackerLuck - 5)
flags = failureTable[weaponTable][severityIndex]
```

| Modified severity | Index | Probability at Luck 5, conditional on reaching this roll |
|---|---|---|
| `<= 20` | `0` | 20% |
| `21..50` | `1` | 30% |
| `51..75` | `2` | 25% |
| `76..95` | `3` | 20% |
| `> 95` | `4` | 5% |

Higher Luck reduces severity by five per point above five. At Luck 10 the roll spans `-24..75`, making indices 3 and 4 unreachable through this calculation. At Luck 1 it spans `21..120`, making index 0 unreachable. Better Criticals and Heavy Handed's successful-critical severity modifier are not used here.

### Complete Failure Matrix

Names below omit the `DAM_` prefix. A plus sign means bitwise OR of flags; `0` means no additional failure effect. These are the literal CE `_cf_table` entries, not names assigned to weapon classes by an editor.

| Table | Index 0 | Index 1 | Index 2 | Index 3 | Index 4 |
|---|---|---|---|---|---|
| `0` | `0` | LOSE_TURN | LOSE_TURN | HURT_SELF + KNOCKED_DOWN | CRIP_RANDOM |
| `1` | `0` | LOSE_TURN | DROP | RANDOM_HIT | HIT_SELF |
| `2` | `0` | LOSE_AMMO | DROP | RANDOM_HIT | DESTROY |
| `3` | LOSE_TURN | LOSE_TURN + LOSE_AMMO | DROP + LOSE_TURN | RANDOM_HIT | EXPLODE + LOSE_TURN |
| `4` | DUD | DROP | DROP + HURT_SELF | RANDOM_HIT | EXPLODE |
| `5` | LOSE_TURN | DUD | DESTROY | RANDOM_HIT | EXPLODE + LOSE_TURN + KNOCKED_DOWN |
| `6` | `0` | LOSE_TURN | RANDOM_HIT | DESTROY | EXPLODE + LOSE_TURN + ON_FIRE |

For binary inspection, the same table is shown below as integer masks. Rows correspond to table ids; columns correspond to severity indices.

```text
0: 00000000 00008000 00008000 00080002 00200000
1: 00000000 00008000 00004000 00100000 00010000
2: 00000000 00020000 00004000 00100000 00002000
3: 00008000 00028000 0000C000 00100000 00009000
4: 00040000 00004000 00084000 00100000 00001000
5: 00008000 00040000 00002000 00100000 00009002
6: 00000000 00008000 00100000 00002000 00009400
```

The original 32-bit executable representation occupies `7 * 5 * 4 = 140` bytes. An entry is at relative offset `4 * (table * 5 + index)` from the table base. The masks above are hexadecimal integer values: an x86 executable stores `00008000` as bytes `00 80 00 00`. This byte order differs from the big-endian selector field in the PRO. CE annotates the original table with address `0x517FA0`; this is an executable address reference, not a portable on-disk offset.

### Applying Failure Flags

The handler clears `DAM_HIT` first. It returns early for an invulnerable attacker or a zero table entry. Otherwise it adds `DAM_CRITICAL` and the selected flags to the attacker's results, then processes them:

| Flag | Integer mask | Handling in the inspected CE source |
|---|---|---|
| `LOSE_TURN` | `0x00008000` | Sets the attacker's current combat AP to zero. |
| `LOSE_AMMO` | `0x00020000` | For ranged attacks, sets ammunition expenditure to the weapon's remaining ammunition. For other attack types, clears this flag. |
| `DROP` | `0x00004000` | Requests a weapon drop. The handler strips it for a no-drop critter or a hidden/integral weapon. |
| `DESTROY` | `0x00002000` | The action code schedules weapon destruction. |
| `HIT_SELF` | `0x00010000` | Calculates damage against the attacker with multiplier `2` (normal damage). Uses the attack's ammunition quantity for ranged attacks, otherwise one damage roll. |
| `EXPLODE` | `0x00001000` | Calculates self-damage with one damage roll and multiplier `2`, then passes the explosion flag to the action code. The table contains no independent explosion damage value or radius. |
| `HURT_SELF` | `0x00080000` | Distinct self-injury flag present in rows 0 and 4. The inspected failure handler has no damage-calculation branch for this bit; do not equate it with `HIT_SELF` or invent a damage amount from the table. |
| `DUD` | `0x00040000` | Dud/misfire flag passed to action handling. The failure handler does not calculate self-damage for this bit alone. |
| `RANDOM_HIT` | `0x00100000` | Calls the combat AI random-target selector. When a target is found, sets `DAM_HIT`, clears `DAM_CRITICAL`, targets the torso, and computes normal damage. |
| `CRIP_RANDOM` | `0x00200000` | Replaced with one of the four limb-crippling flags, selected uniformly. It does not select blindness. |
| `KNOCKED_DOWN` | `0x00000002` | Adds knockdown to the result. |
| `ON_FIRE` | `0x00000400` | Adds the on-fire flag to the result. |

If random-target selection fails, CE restores the original target pointer and leaves the attack without the successful random-hit conversion. A selected failure mask is therefore an input to further processing, not a guarantee that every named outcome occurs. In particular, the drop filter removes `DROP`; it does not also remove `DESTROY` or `EXPLODE`.

For example, table 3 at severity 60 selects index 2, `DROP + LOSE_TURN` (`0xC000`). With a hidden weapon, the drop bit is removed, but the attacker still loses their remaining AP.

### Time Gates and Overrides

CE checks two distinct thresholds when `[Misc] RemoveCriticalTimelimits` is false:

- The generic random-roll translator permits critical promotion when `gameTime / ticksPerDay >= 1`.
- The combat failure handler suppresses player failure effects while `gameTime / ticksPerDay < 6`. This second check applies specifically to `gDude`; NPC failures do not use it.

These checks use the engine's absolute game-time counter. They should not be described as a count of days since loading the current save. Other routes that promote an attack to critical failure still encounter the player-specific check when the handler runs. Setting `RemoveCriticalTimelimits=1` bypasses both gates, without changing the failure matrix or Luck formula.

In the inspected CE implementation, `OverrideCriticalFile` loads the successful-critical records documented above; it does not load `_cf_table`. Changing the weapon PRO selector chooses an existing failure row. Replacing the matrix requires engine modification or a runtime-specific extension. Do not assume an arbitrary sfall version exposes the same editing facilities: its hooks and patches must be checked separately.

Failure entries also have no `Message` or `FailMessage` member. Combat output is derived from the resulting attack state and the combat message system; the successful-critical message-id schema cannot be copied into a failure-table entry.

## Editing Notes

- Use decimal or parser-supported integer syntax accepted by the target INI parser. Decimal values are safest for classic tools.
- Keep effect indices zero-based in override files: `e0_*` is the first effect tier and `e5_*` is the sixth.
- Use mode `3` when you want sfall fixes plus an override file. Use mode `1` only when you intentionally want the original flat layout without the built-in correction pass.
- Do not copy damage flag values from executable byte-sequence tables unless you convert them to integer flag values first.
- When changing `Message` or `FailMessage`, update [`combat.msg`](msg.html) at the same time.
- When changing a target class, update [critter PRO](pro.html) kill types and the critical table together so the same enemy does not silently move to a different table.

## Sources

- [Fallout 2 CE `item.cc`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/item.cc), `weaponGetCriticalFailureType`, for weapon PRO selection and the null-weapon sentinel.
- [Fallout 2 CE `random.cc`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/random.cc), `randomTranslateRoll`, for failure promotion and its game-time threshold.
- [Fallout 2 CE `actions.cc`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/actions.cc) for downstream weapon destruction, drop, and dud handling.

- [Fallout 2 CE `combat.cc`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/combat.cc) for the built-in tables, critical effect roll, override loader, sfall corrections, and damage application.
- [Fallout 2 CE `combat_defs.h`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/combat_defs.h) for hit-location and critical-record structure definitions.
- [Fallout 2 CE `proto_types.h`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/proto_types.h) for kill-type ids and the sfall extended kill-type count.
- [Fallout 2 CE `obj_types.h`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/obj_types.h) for damage flag integer values.
- [The Fallout Wiki critical hit table article](https://fallout.wiki/wiki/Critical_Hit_Tables) for the public table legend and vanilla executable-table interpretation.
- [sfall combat scripting documentation](https://sfall.bgforge.net/combat/) for runtime critical-table script APIs.
