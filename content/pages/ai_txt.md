---
title: AI.TXT File Format
output: ai_txt.html
description: Fallout and Fallout 2 AI.TXT combat AI packet format, parser behavior, runtime fields, party member AI semantics, and save compatibility notes.
toc: auto
---
# AI.TXT

The file consists of a description of combat parameters for the player and all NPC classes in the game. Descriptions are separated from one another with an empty line. Each description consists of the (unique) name of the class and settings. Below is a list of parameters in alphabetical order, which is how they are always written in the file. "string" means that the value should be text; "N" indicates a numeric value; and "mixed" can be either.

There seems to be a mismatch between some of the string values used and what the engine actually looks for. Potentially incorrect values are indicated with a (*).

## Format Summary

`AI.TXT` is an INI-style configuration file loaded from `data\ai.txt` during combat AI initialization. Each section defines one AI packet, and each packet is selected at runtime by its numeric `packet_num`. Critter [PRO](pro.html) files store the packet number in their AI Packet field, so the section name is mostly a tool/user label while `packet_num` is the real runtime identifier.

| Property | Meaning |
| --- | --- |
| File path | `data\ai.txt`, relative to the Fallout/Fallout 2 data search path or packed DAT contents. |
| Syntax | INI/config sections. A packet starts with `[Section Name]` and contains `key=value` entries. |
| Runtime identifier | `packet_num`, not the section name or file order. |
| Main consumer | Combat AI code and critter PRO data. Party-member control UI can also modify selected packet fields at runtime. |
| Companion text files | `AIGENMSG.TXT`, `AIBDYMSG.TXT`, `combatai.msg`, `custom.msg`, and party-control data. |

## Parser Behavior

Fallout 2 CE's `combat_ai.cc` gives a useful view of the engine's expectations. The parser allocates one runtime `AiPacket` for every config section, initializes some string-backed enum fields to `-1`, then reads required integer keys and optional string keys from each section.

| Behavior | Practical consequence |
| --- | --- |
| All integer keys read through required `configGetInt` calls must exist. | Missing required numeric values make AI initialization fail. |
| String enum keys are optional in the source code for several fields. | If absent or unrecognized, many enum-backed fields stay `-1`, which often means "no preference" or "not set" depending on the field. |
| Enum string matching is case-insensitive. | `charge`, `Charge`, and similar case changes are treated alike for normal enum fields. |
| `hurt_too_much` is lowercased and parsed as comma-separated flags. | Use comma-separated values such as `crippled, blind`. Unknown flags are logged but do not stop parsing. |
| `chem_primary_desire` is read as an integer list of up to three values. | The runtime packet stores three desired drug/item PIDs, defaulting to `-1` when absent. |
| Packet lookup scans all loaded packets for matching `packet_num`. | File order is not the lookup mechanism, but duplicate packet numbers are ambiguous and dangerous for tools. |
| If a packet number is missing at runtime, CE logs "Missing AI Packet" and returns the first packet. | A bad PRO AI Packet can silently make a critter behave like packet 0 after logging the error. |

## Engine-recognized String Values

The values below are the strings recognized by Fallout 2 CE's combat AI parser. Values present in shipped data but missing from these lists should be treated as data/tooling conventions, unused values, or values handled outside the normal parser path.

| Field | Recognized strings | Internal mapping note |
| --- | --- | --- |
| `area_attack_mode` | `always`, `sometimes`, `be_sure`, `be_careful`, `be_absolutely_sure`. | Maps directly to `0..4`. `no_pref` is not in the engine list and remains unrecognized. |
| `attack_who` | `whomever_attacking_me`, `strongest`, `weakest`, `whomever`, `closest`. | Maps directly to `0..4`. |
| `best_weapon` | `no_pref`, `melee`, `melee_over_ranged`, `ranged_over_melee`, `ranged`, `unarmed`, `unarmed_over_thrown`, `random`. | Maps to weapon preference orderings. `never` is not in the engine list. |
| `chem_use` | `clean`, `stims_when_hurt_little`, `stims_when_hurt_lots`, `sometimes`, `anytime`, `always`. | Maps directly to `0..5`. |
| `distance` | `stay_close`, `charge`, `snipe`, `on_your_own`, `stay`. | Maps directly to `0..4`. `random` is not in the engine list. |
| `run_away_mode` | `none`, `coward`, `finger_hurts`, `bleeding`, `not_feeling_good`, `tourniquet`, `never`. | The parser subtracts one after matching: `none` becomes `-1`, `coward` becomes `0`, and `never` becomes `5`. |
| `disposition` | `none`, `custom`, `coward`, `defensive`, `aggressive`, `berserk`. | The parser subtracts one after matching: `none` becomes `-1`, `custom` becomes `0`, and `berserk` becomes `4`. |
| `hurt_too_much` | `blind`, `crippled`, `crippled_legs`, `crippled_arms`. | Comma-separated damage flag mask. `crippled` combines both arm and leg cripple masks. |

## Required Numeric Keys

The source parser requires the following numeric keys to be present in every section. If any is missing, AI initialization fails and the engine logs an error while processing `ai.txt`.

```text
packet_num
max_dist
min_to_hit
min_hp
aggression
secondary_freq
called_freq
font
color
outline_color
chance
run_start, run_end
move_start, move_end
attack_start, attack_end
miss_start, miss_end
hit_head_start, hit_head_end
hit_left_arm_start, hit_left_arm_end
hit_right_arm_start, hit_right_arm_end
hit_torso_start, hit_torso_end
hit_right_leg_start, hit_right_leg_end
hit_left_leg_start, hit_left_leg_end
hit_eyes_start, hit_eyes_end
hit_groin_start, hit_groin_end
```

String-backed behavior keys are not all required by the parser, but missing values can leave runtime behavior at `-1`. In practice, copied packets should keep the same complete key set unless the missing value is intentional and tested.

## Message Range Notes

Combat message fields are stored as start/end ranges. The AI code chooses messages from these ranges when displaying attack, miss, movement, run-away, and hit-location floats. These numbers ultimately point at lines in `combatai.msg`, often through `AIGENMSG.TXT` and `AIBDYMSG.TXT` categories.

| Range family | Use |
| --- | --- |
| `attack_start` / `attack_end` | Combat float text when beginning or making an attack. |
| `move_start` / `move_end` | Text when moving into attack position. |
| `miss_start` / `miss_end` | Text when an attack misses. |
| `run_start` / `run_end` | Text when running away. |
| `hit_*_start` / `hit_*_end` | Body-part reaction text when hit in a specific location. |

One source-code quirk: Fallout 2 CE increments `hit_groin_end` after reading it. Tools that compare raw `AI.TXT` values with runtime packet values should account for this difference.

## Runtime Packet Fields

The internal packet structure groups the text keys into combat thresholds, message ranges, enum-backed behavior choices, drug preferences, and display settings.

| Group | Fields | Notes |
| --- | --- | --- |
| Identity and thresholds | `packet_num`, `max_dist`, `min_to_hit`, `min_hp`, `aggression`, `hurt_too_much`. | Control packet identity, hostility distance, attack willingness, fleeing, and damage-condition reactions. |
| Attack frequency | `secondary_freq`, `called_freq`. | Both are divisor-style probabilities in the classic documentation. They should not be zero. |
| Float/message display | `font`, `color`, `outline_color`, `chance`. | Control combat float appearance and likelihood of message display. |
| Message ranges | `run_*`, `move_*`, `attack_*`, `miss_*`, and `hit_*_*` ranges. | These point into combat message text indirectly through `combatai.msg` and the AI message tables. |
| Behavior enums | `area_attack_mode`, `run_away_mode`, `best_weapon`, `distance`, `attack_who`, `chem_use`, `disposition`. | String values are converted to internal numeric indexes during initialization. |
| Drug preferences | `chem_primary_desire`. | Stores up to three item PIDs that should be preferred when AI drug use is allowed. |
| Message table selectors | `body_type`, `general_type`. | Stored as strings and used to select message blocks from `AIBDYMSG.TXT` and `AIGENMSG.TXT`. |

## Combat Behavior Pipeline

An AI packet is not a script by itself. It is a bundle of settings read by combat AI routines during a critter's turn and by support code that chooses combat messages, party AI settings, and retreat behavior.

| Combat decision area | AI.TXT fields involved | Runtime role |
| --- | --- | --- |
| Packet lookup | `packet_num`. | The critter's current AI packet number selects the runtime `AiPacket`. If no packet matches, CE logs a missing-packet message and falls back to the first loaded packet. |
| Target choice | `attack_who`, `aggression`, `max_dist`, team/combat state outside AI.TXT. | Influences which enemy the AI prefers and how far away hostility/engagement should matter. |
| Weapon choice | `best_weapon`, `min_to_hit`, weapon/ammo inventory, weapon PRO data. | Filters and ranks usable weapons and attack types, while still depending on inventory, AP cost, range, ammo, and to-hit chance. |
| Attack mode | `area_attack_mode`, `secondary_freq`, `called_freq`. | Controls willingness to use burst/secondary and called attacks. Actual availability still depends on the weapon's attack modes. |
| Positioning | `distance`, `max_dist`, available AP, map geometry. | Guides whether the critter charges, snipes, stays close, stays put, or acts independently. |
| Drug/item use | `chem_use`, `chem_primary_desire`, current HP, body type, AP, inventory. | Controls whether the critter may use drugs and which drug PIDs are preferred. |
| Fleeing | `run_away_mode`, `min_hp`, `hurt_too_much`. | Determines when damage, HP thresholds, or crippling conditions should cause retreat behavior. |
| Combat floats | `chance`, `font`, `color`, `outline_color`, message ranges, `body_type`, `general_type`. | Controls the chance, appearance, and message-id ranges for combat AI text. |

## Combat Rating and Target Selection

The `strongest` and `weakest` target preferences are based on a small combat-rating helper, not on level, current HP, total SPECIAL stats, or armor stats directly. CE's rating is:

```text
max(melee_damage, right_hand_weapon_max_damage, left_hand_weapon_max_damage) + armor_class
```

Dead, knocked-out, non-critter, or null objects rate as zero. This means `strongest` favors targets with the highest visible damage ceiling plus AC, while `weakest` favors the opposite ordering. The rating does not account for ammo availability, perks, damage type, resistances, or current HP by itself; those can still matter later when choosing an actual attack.

## Weapon Preference Ordering

`best_weapon` does not directly name a specific item. It chooses an attack-type priority order that the AI combines with inventory, ammo, range, AP, and hit chance checks.

| Value | Internal id | Preferred attack types |
| --- | --- | --- |
| `no_pref` | 0 | Ranged, thrown, melee, unarmed. |
| `melee` | 1 | Melee only. |
| `melee_over_ranged` | 2 | Melee, then ranged. |
| `ranged_over_melee` | 3 | Ranged, then melee. |
| `ranged` | 4 | Ranged only. |
| `unarmed` | 5 | Unarmed only. |
| `unarmed_over_thrown` | 6 | Unarmed, then thrown. |
| `random` | 7 | No fixed attack-type priority in the ordering table. |

The source table also includes a default row before `no_pref` with the same ordering as `no_pref`. This is useful to know when reverse-engineering fallback behavior.

## Flee Logic

`run_away_mode` can either be explicit or derived. If the loaded packet has a concrete `run_away_mode` other than `-1`, `aiGetRunAwayMode` returns it directly. If it is unset, CE derives a mode from `min_hp` as a percentage of the critter's maximum HP.

| Derived mode | HP percent threshold |
| --- | --- |
| `coward` | 0% |
| `finger_hurts` | 25% |
| `bleeding` | 40% |
| `not_feeling_good` | 60% |
| `tourniquet` | 75% |
| `never` | 100% |

`hurt_too_much` is separate from HP percentage. It stores a damage-condition mask for blindness and crippled limbs. A packet can therefore express both "run when HP is low" and "run when specific body damage happens".

Runtime setters can also change this relationship. `aiSetRunAwayMode` stores the requested mode and recomputes `min_hp` from the critter's maximum HP and the run-away threshold table. During the combat turn, the flee check still tests the critter's current HP against the packet's stored `min_hp`, plus the flee maneuver flag and `hurt_too_much` damage mask.

## Drug-use Logic

`chem_use` is only permission and timing policy. The critter must still be able to use drugs: CE's drug-use code skips non-biped critters and requires enough AP for use. It checks inventory drugs first, and can then try to retrieve nearby drug/misc items if the inventory search did not produce a usable item.

| `chem_use` | Behavior note |
| --- | --- |
| `clean` | Do not use drugs. |
| `stims_when_hurt_little` | Use healing drugs when current HP is below 60% of maximum HP. |
| `stims_when_hurt_lots` | Use healing drugs when current HP is below 30% of maximum HP. |
| `sometimes` | Every third combat turn, make a 25% chance check for non-healing drug use. |
| `anytime` | Every third combat turn, make a 75% chance check for non-healing drug use. |
| `always` | Always pass the non-healing drug-use chance check. |

`chem_primary_desire` contains up to three item PIDs that should be preferred when selecting drugs. It is a preference list, not proof that the critter actually has those items. Inventory, body type, AP, and combat state still decide whether anything is used.

In CE, non-healing drug selection separates inventory drugs into primary-desire and secondary buckets, then randomly consumes from the primary bucket first. Each bucket is capped at three items during this selection pass. `sometimes` stops after one non-healing drug, `anytime` stops after two, and `always` can continue until AP or the two small buckets are exhausted. This is behavior, not extra syntax, but it matters when testing critters carrying many drugs.

## Target and Distance Modes

`attack_who` and `distance` are high-level preferences. They are not absolute commands: combat state, map geometry, perception/line-of-sight, team relationships, AP, weapon range, and scripts can still change what the critter actually does.

| Field | Value | Behavior intent |
| --- | --- | --- |
| `attack_who` | `whomever_attacking_me` | Prefer the enemy currently attacking the critter or its side. |
| `attack_who` | `strongest` | Prefer a target selected through the combat-rating strength ordering. |
| `attack_who` | `weakest` | Prefer a target selected through the combat-rating weakness ordering. |
| `attack_who` | `whomever` | No strong target preference. |
| `attack_who` | `closest` | Prefer the nearest valid target. |
| `distance` | `stay_close` | Try to remain near the player or party context. |
| `distance` | `charge` | Close distance aggressively. |
| `distance` | `snipe` | Prefer fighting from range when possible. |
| `distance` | `on_your_own` | Act independently rather than staying near the player. |
| `distance` | `stay` | Prefer holding position. |

For tooling, treat these as intent labels. They are useful for editing and diagnostics, but a complete turn simulation needs combat code, map data, inventory, weapon PRO data, and current object state.

## Distance Behavior Details

CE gives some of the `distance` values concrete movement behavior during a combat turn:

| Value | Runtime behavior notes |
| --- | --- |
| `stay_close` | If the critter is not currently reacting to the player as its attacker and is more than 5 hexes from the player, it tries to move back within that range. |
| `charge` | If a target exists, the critter tries to move closer to that target. |
| `snipe` | If the target is closer than 10 hexes, the critter may move away, especially when it has little AP after an attack or when its combat rating is lower than the target's. |
| `on_your_own` | No special movement branch in the main distance-preference switch; it mainly affects party-follow distance behavior. |
| `stay` | No special movement branch in the main distance-preference switch; for party-follow logic it effectively disables the normal "catch up to teammate" pull by using a very large allowed distance. |

For party members without an active target, CE also uses a small follow-distance table keyed by `distance`: 5 hexes for `stay_close`, 7 hexes for `charge`, `snipe`, and `on_your_own`, and a very large distance for `stay`.

## Area Attacks and Called Attacks

`area_attack_mode`, `secondary_freq`, and `called_freq` are easy to overread. They do not grant abilities; they only influence choices when the critter already has a weapon/attack that can use the relevant mode.

| Field | Meaning | Notes |
| --- | --- | --- |
| `area_attack_mode` | Burst/area-attack safety preference. | Values range from aggressive use (`always`) to stricter friendly-fire avoidance (`be_absolutely_sure`). |
| `secondary_freq` | Chance divisor for secondary/burst mode. | Classic docs describe this as `1/N`. It must not be zero. |
| `called_freq` | Chance divisor for aimed/called attacks. | Classic docs describe this as `1/N`. It must not be zero. |
| `min_to_hit` | Minimum acceptable hit chance. | Can prevent attacks that the preference fields would otherwise allow. |

`no_pref` appears in some data and UI text for area attacks, but it is not in CE's recognized `area_attack_mode` string table. Existing documentation keeps it marked with an asterisk for that reason.

## Combat Float Selection

Combat floats are controlled by both packet fields and the message-list files. The packet stores numeric ranges; the actual text is in message data.

| Field | Role |
| --- | --- |
| `chance` | Overall likelihood of showing an AI combat message. |
| `font`, `color`, `outline_color` | Visual presentation for combat floats. |
| `general_type` | Chooses a general combat-message block from `AIGENMSG.TXT`. |
| `body_type` | Chooses body/hit reaction message behavior from `AIBDYMSG.TXT`. |
| `*_start` / `*_end` | Message id range used for the specific event family. |

When a packet appears silent in-game, check `chance`, the selected range, the category file, and `combatai.msg`. A valid packet can still produce no visible text if the chance is zero or the message range points to empty/missing entries.

Combat taunts also have runtime suppression rules outside the packet. CE skips them when the preference for combat taunts is disabled, when the speaker is the player, when the speaker is dead or knocked out, when the random `chance` check fails, when the selected range has `end < start`, or when the chosen `combatai.msg` line is missing. Even after a taunt is selected, the text callback avoids adding another float if another text object is already on screen.

## Combat Participation Notes

`AI.TXT` does not decide by itself whether a critter joins or leaves combat. CE also checks object visibility, elevation, dead/knocked-out state, damage taken last turn, maneuver flags, perception, sneaking state, current danger source, and script combat procedures. A critter with a valid packet can therefore ignore combat, join late, disengage, or flee because of runtime state rather than because of the packet text.

## Inter-file Dependencies

| File | Dependency |
| --- | --- |
| [PRO](pro.html) | Critter prototypes store an AI Packet number. The engine uses that number to find the matching `AI.TXT` packet. |
| [MAP](map.html) | Placed critters inherit their prototype AI unless changed by scripts or runtime state. Saved map/game state may include critter state built from these packets. |
| [SSL](ssl.html) / [INT](int.html) | Scripts can query or change party AI settings through engine procedures, and combat event scripts run alongside AI behavior. |
| [MSG](msg.html) | `combatai.msg` stores the final text for combat floats; ranges in AI packets choose message ids. |
| `AIGENMSG.TXT` | Maps `general_type` names to general combat taunt ranges. |
| `AIBDYMSG.TXT` | Maps `body_type` names to hit/body-part reaction message ranges. |
| `custom.msg` and party-control data | Provide user-facing labels and selectable AI settings for party members. |

## Prototype, MAP, and Runtime Overrides

The AI packet number can enter runtime state through several routes. The critter PRO AI Packet field is the canonical prototype default, but placed critters, scripts, party control, and save data can leave the current runtime object using a different packet number.

| Source | Effect |
| --- | --- |
| Critter [PRO](pro.html) | Defines the prototype's default AI packet number. |
| [MAP](map.html) placed object/runtime object state | Can carry a concrete critter instance whose combat fields differ from a freshly loaded prototype. |
| Party-control UI | Can switch among a companion's contiguous party packet family. |
| Scripts / engine calls | Can set selected AI behavior fields at runtime through AI setter functions. Setting a potential party member's AI packet can also update the critter prototype's AI Packet reference. |
| Saved game | Can preserve custom party AI packet fields independently of the original text file. |

This distinction matters when debugging: changing `AI.TXT` or a PRO file may not affect an already-saved critter instance in the way a new game or freshly loaded prototype would.

## Minimal Packet Shape

The file is conventionally written with keys in alphabetical order, but the config parser looks up keys by name. A small packet normally looks like this:

```text
[Example Critter]
aggression=45
attack_end=50140
attack_start=50140
attack_who=closest
best_weapon=unarmed
body_type=None
called_freq=20
chance=0
color=58
distance=charge
font=101
general_type=None
hit_eyes_end=50080
hit_eyes_start=50080
hit_groin_end=50090
hit_groin_start=50090
hit_head_end=50000
hit_head_start=50000
hit_left_arm_end=50010
hit_left_arm_start=50010
hit_left_leg_end=50070
hit_left_leg_start=50070
hit_right_arm_end=50020
hit_right_arm_start=50020
hit_right_leg_end=50060
hit_right_leg_start=50060
hit_torso_end=50030
hit_torso_start=50030
hurt_too_much=crippled, blind
max_dist=12
min_hp=10
min_to_hit=10
miss_end=50160
miss_start=50160
move_end=50120
move_start=50120
outline_color=55
packet_num=187
run_away_mode=bleeding
run_end=50100
run_start=50100
secondary_freq=200
```

Fields such as `area_attack_mode`, `chem_use`, `chem_primary_desire`, and `disposition` are common for party-controlled critters but not present on every non-party packet.

## Party Member Packets

Party members are special because the player can change their combat AI mode. The packet numbers for each companion's modes must remain a contiguous family. The runtime `aiSetDisposition` logic changes the critter's packet number by subtracting the difference between the requested disposition and the current packet's disposition.

| Mode | Internal disposition | Packet order relative to CUSTOM |
| --- | --- | --- |
| `BERSERK` | 4 | `CUSTOM - 4` |
| `AGGRESSIVE` | 3 | `CUSTOM - 3` |
| `DEFENSIVE` | 2 | `CUSTOM - 2` |
| `COWARD` | 1 | `CUSTOM - 1` |
| `CUSTOM` | 0 | Base custom packet. |

This explains the required order shown below. A companion should start in `CUSTOM` mode so the game can derive the other packet numbers correctly.

### [Class Name]

This option must be specified first. For example, [Enclave Patrol], [Fighting Mantis] or [Arroyo Villager]. The player is the class named Player AI. Every party member must have the following classes:

```text
[PARTY NAME BERSERK]
[PARTY NAME AGGRESSIVE]
[PARTY NAME DEFENSIVE]
[PARTY NAME COWARD]
[PARTY NAME CUSTOM]
```

Here NAME is the name of a member of the team, such as: DOC, LENNY, GORIS, etc.

NOTE: The combat ai-mode types for party members must always be in the above shown order, and the character must always start out in the CUSTOM-mode, otherwise the game will not be able to correctly detect the set ai-mode.

Only custom party-member AI packets are saved and loaded as mutable AI state in Fallout 2 CE: the save/load code finds party member PIDs, resolves their prototype AI packet, and serializes packets whose internal disposition is `0` (`custom`). This is why changing party packet layout can have save compatibility consequences.

## Party UI vs Runtime Values

The party-control UI exposes a friendly subset of AI behavior. It does not rewrite the whole packet; it changes selected enum/threshold values or switches the party member to another packet in the companion's packet family.

| Player-facing idea | AI.TXT/runtime fields | Notes |
| --- | --- | --- |
| Disposition/mode | `disposition`, `packet_num`. | Changing mode changes the critter's current AI packet number using the contiguous packet-family assumption. |
| Burst/area attack safety | `area_attack_mode`, `secondary_freq`. | Controls willingness to use secondary/burst modes, but actual attack availability comes from weapon PRO data. |
| Weapon preference | `best_weapon`. | Chooses attack-type priority, not a specific weapon. |
| Distance preference | `distance`. | Guides movement behavior relative to enemies or the player. |
| Target preference | `attack_who`. | Guides target retargeting/rating behavior. |
| Drug policy | `chem_use`, `chem_primary_desire`. | Needs matching inventory and body/AP conditions to have visible effect. |
| Flee policy | `run_away_mode`, `min_hp`, `hurt_too_much`. | Runtime setters can recompute `min_hp` from the selected run-away mode. |

Fields such as combat float message ranges, `body_type`, `general_type`, `font`, and `color` are packet data rather than normal player-facing party controls.

### aggression = N

The chance of aggressive behavior by the NPC in combat. Measured as a percentage. Valid values are: 0-100.

### area_attack_mode = string

Conditions for burst-fire. Existing values, the internal numeric representation, and the text used for setting custom behavior in-game:

```text
no_pref*               -1  "Not Applicable."
always                  0  "Always!"
sometimes               1  "Sometimes, don't worry about hitting me"
be_sure                 2  "Be sure you won't hit me"
be_careful              3  "Be careful not to hit me"
be_absolutely_sure      4  "Be absolutely sure you won't hit me"
```

'no_pref' is used by Cyberdog and Goris, and has a line in custom.msg, but is not checked for by the engine.

For party members, these options must be defined in the party.txt file.

### combat float

attack_end = N
attack_start = N

Combat floats said when attacking. At the beginning of the attack, a random number between the two values is chosen. Then the appropriate line number from master.dat/text/english/game/combatai.msg is displayed.

### attack_who = string

Who the NPC should attack. Existing values:

```text
whomever_attacking_me     0  "Whomever is attacking me"
strongest                 1  "The strongest"
weakest                   2  "The weakest"
whomever                  3  "Whomever you want"
closest                   4  "Whomever is closest"
```

For party members, these options must be defined in the party.txt file.

### best_weapon = string

Preferred weapon types. This option does not exist for all NPCs. Available values:

```text
no_pref               0  "None"
melee                 1  "Melee"
melee_over_ranged     2  "Melee then ranged"
ranged_over_melee     3  "Ranged then melee"
ranged                4  "Ranged"
unarmed               5  "Unarmed"
unarmed_over_thrown   6  (Spore Plant)
random                7
never*                ?  (Deathclaw)
```

The CE parser recognizes `unarmed_over_thrown` and `random`; it does not recognize `never` for this field.

For party members, these options must be defined in the party.txt file.

### body_type = string

Defines the block of messages that critters say in response to being hit. The blocks are from master.dat/data/AIBDYMSG.TXT, which in turn refers to lines in combatai.msg . Existing values (examples of critters that use them in parens):

```text
None
Primitive
Wimpy Person (Whimpy, Coward ...)
Punk (SF)
Berserk Person (Kamakazi. ..)
Tough Person (Bounty Hunter, Caravan Driver ...)
Boxer
Normal Person
Robot
Junkie (Crazy Addict)
Elron (Habologisty)
Ghoul
Kaga
Raider (Khan)
Raider (Non-Khan)
Gangster (Mobsters)
Shi
Super Mutant (Master Army)
Goris
Miron
Marcus
Lenny
Cassidy
Robo Brain (Hum / Cyb)
Sulik
```

### called_freq = N

Sets the probability of using an aimed attack, which is calculated as 1/N. Cannot be zero.

### chance = N

The likelihood of any message. Measured as a percentage. Valid values are: 0-100.

### chem_primary_desire = N

Preference for drug use. PIDs of items, or -1 if there is no preference. If you want to specify multiple types of drugs, they are listed separated by commas (e.g., 284,81,103). Strangely, most critters use 81 (Iguana-on-a-stick, whole).

### chem_use = string

NPC drug use in combat. Existing values:

```text
clean                      0  "I'm clean"
stims_when_hurt_little     1  "Stimpacks when hurt a bit"
stims_when_hurt_lots       2  "Stimpacks when hurt a lot"
sometimes                  3  "Any drug some of the time"
anytime                    4  "Any drug any time"
always                     5
```

For party members, these options must be defined in the party.txt file.

### color = N

Perhaps the font color used for combat floats. It is 58 for all critter types.

### disposition = mixed

Determines NPC behavior during a battle. Existing values:

```text
-1*
none
custom
coward
defensive
aggressive
berserk
```

### distance = string

The distance between the NPC and its enemy, managing battle tactics. Existing values:

```text
stay_close        0  "Stay close to me"
charge            1  "Charge!"
snipe             2  "Snipe the enemy"
on_your_own       3  "On your own"
stay              4  "Stay where you are"
random*           ?  (listed only for Mobsters and Tough Khan)
```

For party members, these options must be defined in the party.txt file.

### font = N

Perhaps, the font used for combat floats. Value in the US version of the game is 101. In other versions 102, 103, and 104 are used.

### general_type = string

Defines the block of combat taunts that the critter periodically shouts during a battle (attack, retreat, miss). The blocks are in master.dat/data/AIGENMSG.TXT, and refer to lines in combatai.msg . Existing values:

```text
Badger
Berserk Person
Bounty Hunter
Boxer
Cassidy
Chip
Crazed Robot
Cyberdog
Dragon (The)
Elron Guards
Gangster
Ghoul
Goris
Guard (Normal)
Guard (Tough)
Junkie
Kaga (1)
Lenny
Lo Pan
Marcus
Myron
None
Normal Person
OZ - 7
OZ - 9
Primitive
Raider
Raider Captain
Raider Mercs
Rat God
Robot
Ryan
Shi Guards
Sulik
Super Mutant (Master Army)
The Brain
Tough Person
Wimpy Person
```

### hit_eyes_end = N

### hit_eyes_start = N

### hit_groin_end = N

### hit_groin_start = N

### hit_head_end = N

### hit_head_start = N

### hit_left_arm_end = N

### hit_left_arm_start = N

### hit_left_leg_end = N

### hit_left_leg_start = N

### hit_right_arm_end = N

### hit_right_arm_start = N

### hit_right_leg_end = N

### hit_right_leg_start = N

### hit_torso_end = N

### hit_torso_start = N

NPC messages when hit in the eyes, groin, head, left arm, left leg, right arm, right leg and torso, respectively. For details, see the description of attack_end.

### hurt_too_much = string

Indicates the condition in which the NPC starts running away. Multiple conditions are separated by commas.

```text
blind          40
crippled       3c
crippled_arms  30
crippled_legs  0c
```

'crippled' is equivalent to 'crippled_arms, crippled_legs'

For party members, these options must be defined in the party.txt file.

### max_dist = N

The maximum distance from the player where the NPC is hostile. Measured in hexes.

### min_hp = N

The minimum number of hit points, at which the NPC starts running away.

### min_to_hit = N

The NPC will attack only if it has at least this high a chance (percentage) of hitting its target.

### miss_end = N

### miss_start = N

NPC combat taunts when missing a target. For details, see the description of attack_end.

### move_end = N

### move_start = N

Combat taunts used when moving to attack (i.e., when not yet in range of the enemy, but already hostile) For details, see description of attack_end.

### outline_color = N

Maybe the color used for target highlighting. For all NPC classes, it is 55.

### packet_num = N

Number of the AI package. Never change it! If the new AI package you indicate packet_num, which already exists, then when you click on the AI button in the Mapper (in critter properties), it closes the window. When you create a new AI package, the value of packet_num must be consecutive. In AI.TXT there are 187 values for packet_num, from 0 to 186 inclusive. If you want to add a new package, it should have packet_num 187, the next added package 188, and so on. If packet_num is not followed consecutively, for example, 188 immediately after 186, the mapper will close.

### run_away_mode = string

Condition when the NPC will run away. Existing values (second column is minimum amount of damage taken, as a percentage of max HP):

```text
coward              0%   0  "Abject coward"
finger_hurts       25%   1  "Your finger hurts"
bleeding           40%   2  "You're bleeding a bit"
not_feeling_good   60%   3  "Not feeling good"
tourniquet         75%   4  "You need a tourniquet"
never             100%   5  "Never!"
none                0%  -1  "None"
```

The parser's string table lists `none` first, then subtracts one from the matched index. This makes `none` the unset value `-1`, and keeps `coward` through `never` at `0..5`.

For party members, these options must be defined in the party.txt file.

### run_end = N

### run_start = N

Combat floats used when running away. For details, see the description of attack_end.

### secondary_freq = N

Similar to called_freq. Sets the probability of using a secondary (i.e., burst) attack mode, which is calculated as 1/N. Cannot be zero. Only used when area_attack_mode is 'sometimes' or 'no_pref'.

## Saved Custom AI State

`AI.TXT` itself is text, but custom party-member AI packets are serialized into save data. Fallout 2 CE reads/writes these fields in a fixed binary order for party members whose prototype AI packet has `disposition=custom`:

```text
packet_num
max_dist
min_to_hit
min_hp
aggression
hurt_too_much
secondary_freq
called_freq
font
color
outline_color
chance
run_start, run_end
move_start, move_end
attack_start, attack_end
miss_start, miss_end
hit ranges for each location
area_attack_mode
best_weapon
distance
attack_who
chem_use
run_away_mode
chem_primary_desire[0..2]
```

Because of this, changing custom party packet numbers, dispositions, or field meanings can affect existing saves. Treat party AI changes more like a save-format change than a harmless text edit.

## Validation Checklist

- Every packet has a unique `packet_num`.
- Packet numbers referenced by critter PRO files exist in `AI.TXT`.
- New packet numbers are appended consecutively when compatibility with mapper/tool assumptions matters.
- Required numeric keys are present in every section.
- Enum/string values are in the engine-recognized lists unless the value is intentionally unused by the engine.
- `secondary_freq` and `called_freq` are non-zero.
- Message ranges point at valid `combatai.msg` entries through the expected message category files.
- Party member packet families are contiguous and ordered `BERSERK`, `AGGRESSIVE`, `DEFENSIVE`, `COWARD`, `CUSTOM`.
- Party members start from their `CUSTOM` packet if the player-facing AI mode UI should work correctly.
- Save compatibility is considered before changing existing party custom packets.

## Tooling Notes

- Prefer appending new packets instead of renumbering existing packets.
- When importing a packet from another project, check every enum string against the engine-recognized list, not only against visible Mapper labels.
- When changing `packet_num`, update critter PRO references, MAP overrides if present, scripts/header constants, and documentation together.
- Warn when a referenced packet is missing, because the runtime fallback to packet 0 can hide the mistake.
- Warn when two sections share a `packet_num`. Runtime lookup returns the first matching packet, while tools may behave differently.
- For party members, validate the whole packet family instead of validating only the selected mode.
- Preserve unknown or intentionally unused strings in source only with a note explaining whether the engine reads them.
- For save-compatible mods, treat changes to custom party packets as save-affecting changes.

## Source-backed Function Map

The following runtime functions are useful anchors when reading scripts, source code, or decompiled engine behavior. Names are from Fallout 2 CE.

| Function | AI.TXT relationship |
| --- | --- |
| `aiInit` | Reads `data\ai.txt`, parses sections, initializes packet defaults, and fills runtime `AiPacket` entries. |
| `aiGetPacketByNum` | Finds a packet by `packet_num`; logs and falls back to the first packet if the number is missing. |
| `aiGetPacket` | Finds the current packet for a critter object through its combat AI packet number. |
| `aiGetAreaAttackMode` / `aiSetAreaAttackMode` | Reads or changes burst/area-attack safety behavior. |
| `aiGetRunAwayMode` / `aiSetRunAwayMode` | Reads explicit/derived flee behavior and can recompute `min_hp` from the selected mode. |
| `aiGetBestWeapon` / `aiSetBestWeapon` | Reads or changes attack-type preference. |
| `aiGetDistance` / `aiSetDistance` | Reads or changes distance/movement preference. |
| `aiGetAttackWho` / `aiSetAttackWho` | Reads or changes target preference. |
| `aiGetChemUse` / `aiSetChemUse` | Reads or changes drug-use policy. |
| `aiGetDisposition` / `aiSetDisposition` | Reads party AI disposition and switches packet numbers within the companion's packet family. |
| `aiLoad` / `aiSave` | Serializes custom party-member AI packet state in save data. |
| `_combatai_rating` | Computes the rating used by `strongest`, `weakest`, and some distance/fear comparisons. |
| `_cai_perform_distance_prefs` | Applies the concrete movement behavior for `stay_close`, `charge`, and `snipe`. |
| `_combatai_msg` / `_ai_print_msg` | Applies combat-taunt chance checks, message lookup, and text-float display suppression. |
| `_combatai_want_to_join` / `_combatai_want_to_stop` | Combine AI state with visibility, perception, scripts, and maneuver flags to decide combat participation. |

## Related Formats

- [PRO File Format](pro.html) - critter prototypes store the AI Packet number that selects an `AI.TXT` packet.
- [PARTY.TXT File Format](party_txt.html) - party members use it to whitelist combat-control choices and connect companion PIDs with AI packet families.
- [MAP File Format](map.html) - placed critters and saved map state use prototype/script behavior together with AI state.
- [SSL Script Source Format](ssl.html) and [INT File Format](int.html) - scripts can interact with combat and party AI behavior.
- [MSG File Format](msg.html) - combat float message ids ultimately resolve to message text.

## Source References

- [Fallout 2 CE `combat_ai.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/combat_ai.cc) - runtime parser, recognized string tables, AI packet lookup, party custom AI save/load, and combat AI helpers.
- [Fallout 2 CE `proto_types.h`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/proto_types.h) - critter prototype fields, including the AI packet reference.
- [AI.TXT File Format on The Fallout Wiki](https://fallout.wiki/wiki/AI.TXT_File_Format) - original format notes and parameter descriptions.
- [AI.txt (Fallout 2) on Fallout Wiki/Fandom](https://fallout.fandom.com/wiki/AI.txt_%28Fallout_2%29) - transcript examples from the shipped Fallout 2 file.

## History

2026-05-07 - Expanded parser behavior, engine-recognized values, behavioral semantics, inter-file dependencies, party packet semantics, save notes, validation, and source references.

2019-12-15 - Updated information about party members.

2019-12-14 - Ported from [https://falloutmods.fandom.com/wiki/AI.TXT_File_Format](https://falloutmods.fandom.com/wiki/AI.TXT_File_Format) by [ghost](https://github.com/ghost2238)

2007 - [Translated by Kanhef](https://www.nma-fallout.com/threads/teamx-documents-russian-to-english-translation.179561/)

1999-12-27 - Written by Serge of Team-X (ai_txt_format.htm)
