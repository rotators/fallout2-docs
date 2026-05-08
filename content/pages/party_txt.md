---
title: PARTY.TXT File Format
output: party_txt.html
description: Fallout 2 PARTY.TXT party-member registry, combat UI option whitelists, companion level-up data, save/load behavior, and validation notes.
---

# PARTY.TXT File Format

`PARTY.TXT` is Fallout 2's party-member registry. It lists objects that can become party members, defines which combat-control options the party UI may expose for each one, and provides the prototype chain used when companions level up. Savegames serialize party roster, companion level-up state, mutable party AI, and party prototype sidecars using this registry; see [Savegame Structure](savegame.html).

The file is loaded from `data\party.txt`. Most paths and message references below are relative to `master.dat`, `critter.dat`, or an unpacked Fallout 2 data directory.

## Format Summary

| Property | Description |
|---|---|
| File path | `data\party.txt`. |
| File type | INI/config style text file. |
| Section names | `[Party Member 0]`, `[Party Member 1]`, and so on. |
| Primary key | `party_member_pid`, a Fallout object PID. |
| Main consumers | Party-member registry, party combat-control UI, companion level-up code, save/load party state. |
| Related text | `game\custom.msg` for combat-control UI labels and `game\misc.msg` for companion level-up messages. |

## Section Discovery

The parser starts at `Party Member 0` and increments the section number while `party_member_pid` exists. The list length is therefore determined by the first missing numbered section. Tools should keep sections contiguous even if the shipped transcript or comments make some entries look sparse.

`Party Member 0` is conventionally the player. Runtime description lookup and support checks start at index 1, while the active party list stores the player at slot 0. Treat section 0 as reserved.

```ini
[Party Member 0]
party_member_pid=16777216
area_attack_mode=always, sometimes, be_sure, be_careful, be_absolutely_sure
attack_who=whomever_attacking_me, strongest, weakest, whomever, closest
best_weapon=no_pref, melee, melee_over_ranged, ranged_over_melee, ranged, unarmed
chem_use=clean, stims_when_hurt_little, stims_when_hurt_lots, sometimes, anytime
distance=stay_close, charge, snipe, on_your_own, stay
run_away_mode=none, coward, finger_hurts, bleeding, not_feeling_good, tourniquet, never
disposition=none, custom, coward, defensive, aggressive, berserk
level_minimum=0
level_up_every=0
level_pids=-1
```

## Parsed Keys

| Key | Type | Runtime role |
|---|---|---|
| `party_member_pid` | Integer PID | Identifies the party-capable object or critter prototype. |
| `area_attack_mode` | Comma-separated strings | Allowed burst/area-attack safety choices in the party customization UI. |
| `attack_who` | Comma-separated strings | Allowed target-preference choices. |
| `best_weapon` | Comma-separated strings | Allowed weapon-preference choices. |
| `chem_use` | Comma-separated strings | Allowed drug-use policy choices. |
| `distance` | Comma-separated strings | Allowed movement/distance preference choices. |
| `run_away_mode` | Comma-separated strings | Allowed flee-threshold choices. |
| `disposition` | Comma-separated strings | Allowed main combat-control dispositions shown on the party control panel. |
| `level_minimum` | Integer | Minimum player level before this party member can level up. |
| `level_up_every` | Integer | Player-level cadence used by the companion level-up algorithm. A value of `0` disables leveling. |
| `level_pids` | Comma-separated integer PIDs | Prototype stages copied into the companion as they level up. `-1` means no stages. |

Behavior-list values use the same string tables as [AI.TXT](ai_txt.html): `area_attack_mode`, `attack_who`, `best_weapon`, `chem_use`, `distance`, `run_away_mode`, and `disposition`. The string parser lowercases the field, skips leading spaces for each comma-separated item, and matches case-insensitively against the engine's known values.

Be strict in tools: unknown strings are logged by the parser but the party-member loader does not check the return value before indexing the support arrays. A bad option name is therefore more dangerous than an ignored cosmetic typo.

## Combat UI Whitelist

The comma-separated behavior lists do not set the companion's current AI behavior by themselves. They define which choices are enabled in the party-member control and customization windows. The current values still come from the companion's current [AI.TXT](ai_txt.html) packet and from runtime setters.

| UI area | PARTY.TXT key | Runtime setter used when changed |
|---|---|---|
| Main combat mode buttons | `disposition` | `aiSetDisposition`, which switches within the companion's contiguous AI packet family. |
| Burst/area attack safety | `area_attack_mode` | `aiSetAreaAttackMode`. |
| Run-away threshold | `run_away_mode` | `aiSetRunAwayMode`. |
| Weapon preference | `best_weapon` | `aiSetBestWeapon`. |
| Distance preference | `distance` | `aiSetDistance`. |
| Target preference | `attack_who` | `aiSetAttackWho`. |
| Drug-use policy | `chem_use` | `aiSetChemUse`. |

The UI has its own hardcoded choice tables and labels from `game\custom.msg`. This means `PARTY.TXT` can only enable or disable choices the UI knows how to show. In Fallout 2 CE's UI table, for example, `best_weapon` exposes `no_pref`, `melee`, `melee_over_ranged`, `ranged_over_melee`, `ranged`, and `unarmed`, while `unarmed_over_thrown` and `random` are parser-recognized AI values but not normal customization-window choices. The `chem_use` customization list similarly exposes up through `anytime`, not `always`.

## AI.TXT Relationship

For a normal companion, three separate pieces need to line up:

| Place | Responsibility |
|---|---|
| `PARTY.TXT` | Declares that the PID is party-capable and lists the UI-supported choices for that companion. |
| Critter [PRO](pro.html) | Stores the current/default AI packet number for the companion's prototype. |
| `AI.TXT` | Defines the actual AI packet fields and the companion's contiguous disposition packet family. |

The `disposition` support list controls which main mode buttons are available. The packet-number arithmetic still depends on `AI.TXT` ordering: `BERSERK`, `AGGRESSIVE`, `DEFENSIVE`, `COWARD`, then `CUSTOM`. `PARTY.TXT` cannot fix a broken AI packet family.

## Level-up Data

`level_minimum`, `level_up_every`, and `level_pids` drive the companion level-up mechanism. If `level_up_every` is missing or zero, the companion does not use this mechanism.

| Field | Meaning |
|---|---|
| `level_minimum` | The player must be at least this level before the companion can advance. |
| `level_up_every` | Number of player level-ups used as the normal cadence for companion advancement. |
| `level_pids` | Prototype stages copied into the companion. Fallout 2 CE supports up to six stage PIDs. |

The level-up state saved per party-member description is `level`, `numLevelUps`, and `isEarly`. When the player levels up, the engine increments the companion's counter, checks the cadence, and may allow an early level-up by probability. If a companion levels early, it waits until the next exact cadence boundary before the next cycle starts.

One indexing detail is easy to miss: CE increments the saved `level` value before indexing `level_pids`. A fresh companion starts at `level=0`, so the first successful advancement copies `level_pids[1]`, not `level_pids[0]`. This matches the idea that the list can include a baseline/current-stage PID before later upgrade stages. Do not reorder these lists unless you have checked the intended companion progression.

When a companion advances, the engine copies base stats, bonus stats, and skills from the stage prototype into the companion's current prototype. It temporarily removes armor and unwields the right-hand item so derived stats can be recalculated, restores HP to the new maximum, then restores the armor and right-hand item. It does not wholesale replace the object with the stage PID.

Level-up messages are in `game\misc.msg`. Message `9000` is the generic display-monitor line, and individual companion float messages are addressed as `9000 + 10 * party_member_index + level - 1`.

## Runtime Party State

`PARTY.TXT` defines descriptions, but the active party is runtime state. Adding a member sets a stable object id based on the PID, marks the object `NO_REMOVE` and `NO_SAVE` while it is actively managed, moves critters to the player's team, and retargets the script owner id to the party-member object id. Removing a member reverses the managed flags and disables the party script flags.

Save data stores the active party length, a party-member item id counter, active member object ids, and the level-up state for each described party member. During load, the engine resolves saved object ids back to objects on the map and repairs duplicate party-member instances when possible.

The active party list is allocated with room for `gPartyMemberDescriptionsLength + 20` entries. This is runtime capacity, not an invitation to leave holes in the numbered descriptions.

## Active Count and Rest Healing

The active party list and the player-visible active party count are not identical. The count helper starts from the active party list length, then excludes dead, hidden, and non-critter entries after slot 0. This is why a managed object can exist in the party system without behaving like a live companion for count-limited logic.

Rest-healing helpers apply similar filters. "Rest until party healed" checks party entries after the player and ignores non-critters, dead critters, hidden critters, and robots. The lower-level rest-heal routine applies healing-rate based HP adjustment to critter party members, but the "does anyone need healing" and "maximum wound" queries deliberately exclude robots because they cannot be healed by resting.

## Party Positioning

When the engine synchronizes party positions, it places visible critter party members around the player. It skips hidden entries and non-critters, alternates between two rotations offset from the player's facing, and slowly increases the placement distance as more members are positioned. This is runtime placement behavior; no coordinates are stored in `PARTY.TXT`.

## Save and Script Recovery

Party members receive special save/load treatment because their objects and scripts need to survive map transitions. Before loading or changing party state, the engine can copy the party member's script record and local variables, remove the active script, then later recreate and reattach the script with its saved locals. Scripted inventory items carried by party members go through similar prep/recover handling.

The party loader also runs duplicate repair after recovery. It scans objects for PIDs listed in `PARTY.TXT`, compares them with the active party member object for that PID, and tries to remove duplicate party-member critters that are not the active instance. This is one reason duplicate companion prototypes or copied map objects can create confusing load-time behavior.

## Non-critter Entries

Most entries are critter PIDs, but the car trunk is also represented in the party system. Several runtime helpers explicitly filter to critter objects, alive objects, or visible objects, so a non-critter party entry can be managed by party save/load without behaving like a living companion in combat counts, best-skill queries, rest healing, or party positioning.

## custom.msg Message IDs

The party customization window uses hardcoded message id groups in `game\custom.msg`. `PARTY.TXT` controls which choices are enabled, but the text shown for those choices comes from these ids:

| Customization group | Message ids | Notes |
|---|---|---|
| Burst/area attack safety | `100..104` | `99` is used for "Not Applicable" when no burst option is selected. |
| Run-away threshold | `200..205` | Six run-away choices, from cowardly through never. |
| Weapon preference | `300..305` | The normal UI exposes six weapon-preference choices. |
| Distance preference | `400..404` | Five distance choices. |
| Target preference | `500..504` | Five target choices. |
| Drug-use policy | `600..604` | The normal UI exposes five drug-use choices. |

The selection popup also reads `custom.msg` lines `10` and `11` for its shared action labels. Missing message lines do not change parser behavior, but they make the UI unreadable.

## Inter-file Dependencies

| File | Dependency |
|---|---|
| [AI.TXT](ai_txt.html) | Defines actual combat behavior packets. `PARTY.TXT` only whitelists which options the player may choose for a companion. |
| [PRO](pro.html) | Party-member PIDs and level-stage PIDs refer to prototype entries. Level-up copies stats and skills from these prototypes. |
| [MSG](msg.html) | `game\custom.msg` labels party-combat choices. `game\misc.msg` contains party level-up messages. |
| [SSL](ssl.html) / [INT](int.html) | Scripts add and remove party members and may change AI settings through engine procedures. |
| SSL headers / PID constants | Classic modding notes warn that adding a party member also requires script-side PID definitions, commonly in party-related headers. The text file alone does not make scripts know the new companion. |
| Save data | Active member ids and per-companion level-up counters are saved separately from the text file. |

## Validation Checklist

- Sections are contiguous from `Party Member 0` onward.
- Every section has `party_member_pid`.
- Section 0 remains reserved for the player convention.
- Every behavior-list token is recognized by the corresponding [AI.TXT](ai_txt.html) string table.
- Behavior lists do not rely on values the party customization UI cannot expose, unless the target engine or UI is known to be extended.
- Every `party_member_pid` and `level_pids` entry points to an existing prototype.
- `level_pids` order matches intended companion progression, including the baseline/current-stage slot.
- Companion AI packet families in `AI.TXT` remain contiguous and ordered correctly.
- Related `custom.msg` and `misc.msg` lines exist when UI labels or level-up messages are expected.
- Scripts and PID constants are updated together with the text file.
- Copied companion objects do not create duplicate active instances with the same PID.
- Non-critter party entries are tested against the specific systems that should see them.

## Source-backed Function Map

| Function | Relationship to PARTY.TXT |
|---|---|
| `partyMembersInit` | Loads `data\party.txt`, counts contiguous sections, parses support lists, and stores level-up data. |
| `partyMemberSupports*` | Checks whether a companion supports a requested UI option. |
| `partyMemberAdd` / `partyMemberRemove` | Maintain the active party list and party-specific object/script flags. |
| `partyMembersSave` / `partyMembersLoad` | Serialize active party object ids and level-up state. |
| `_partyMemberPrepLoad` / `_partyMemberRecoverLoad` | Preserve party-member scripts, local variables, and scripted inventory items across load/recovery. |
| `_partyMemberSyncPosition` | Places visible critter party members around the player. |
| `_getPartyMemberCount` | Counts active visible living critter party members and excludes hidden/dead/non-critter entries. |
| `partyIsAnyoneCanBeHealedByRest` / `partyGetMaxWoundToHealByRest` | Check rest-healing needs while excluding hidden, dead, non-critter, and robot entries. |
| `partyFixMultipleMembers` | Attempts to remove duplicate party-member critters after load/recovery. |
| `_partyMemberIncLevels` | Applies the `level_minimum`, `level_up_every`, and `level_pids` rules. |
| `_partyMemberCopyLevelInfo` | Copies stats and skills from the selected level-stage prototype. |
| `partyMemberCustomizationWindowInit` | Loads `game\custom.msg` and displays the customization UI. |
| `_gdCustomUpdateSetting` | Calls the AI setters after a supported customization value is selected. |

## Source References

- [Fallout 2 CE `party_member.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/party_member.cc) - parser, active party state, save/load, support checks, and level-up behavior.
- [Fallout 2 CE `game_dialog.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/game_dialog.cc) - party combat-control UI, hardcoded option lists, `custom.msg` loading, and AI setter calls.
- [Fallout 2 CE `string_parsers.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/string_parsers.cc) - comma-separated integer and string-list parsing behavior.
- [Party.txt on The Fallout Wiki](https://fallout.wiki/wiki/Party.txt) - transcript of the shipped Fallout 2 party file and classic modding notes.

## History

2026-05-07 - Documented PARTY.TXT from Fallout 2 CE source and available format notes.
