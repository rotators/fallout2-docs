---
title: GCD File Format
output: gcd.html
description: Fallout and Fallout 2 GCD starting character profile layout, CritterProtoData fields, tagged skills, traits, and related tools/source references.
---

# GCD File Format

GCD files contain starting character profiles. They are used together with [.BIO (biography) files](bio.html) to create premade characters.

In addition to the theme characters `combat.gcd`, `stealth.gcd`, and `diplomat.gcd`, there are three other files: `demo.gcd`, `blank.gcd`, and `player.gcd`.

Some of the derived stats from the file are not used but rather calculated by the engine instead.

All numeric fields are signed 32-bit integers stored in big-endian byte order. The file begins with the same `CritterProtoData` block used by critter PRO files: flags, 35 base stats, 35 bonus stats, 18 skill bonuses, body type, experience, kill type, and natural damage type. After that, GCD stores the 32-byte player name, four tagged skill indexes, two selected trait indexes, and remaining character points.

### Structure

| Offset | Meaning | Values |
|---|---|---|
| **Game flags** |  |  |
| 0x0 | Dude state flags. These are the status bits controlled by `dudeEnableState` and `dudeDisableState`. Known bits are 0x00000001 - sneaking, 0x00000008 - level-up available, and 0x00000010 - addicted. Poisoned and radiated are not stored here. | Bitfield |
| **Primary stats** |  |  |
| 0x4 | Strength (ST) | 0 to 10 |
| 0x8 | Perception (PE) | 0 to 10 |
| 0xC | Endurance (EN) | 0 to 10 |
| 0x10 | Charisma (CH) | 0 to 10 |
| 0x14 | Intelligence (IN) | 0 to 10 |
| 0x18 | Agility (AG) | 0 to 10 |
| 0x1C | Luck (LK) | 0 to 10 |
| **Secondary stats** |  |  |
| 0x20 | Hit Points | = 15 + 2 * EN + ST |
| 0x24 | Action Points | = 5 + AG / 2 |
| 0x28 | Armor Class | = AG |
| 0x2C | Unarmed Damage (this is not used by the game! Melee Damage stat is used instead.) | - |
| 0x30 | Melee Damage | = ST - 5 |
| 0x34 | Carry Weight | = 25 + 25 * ST |
| 0x38 | Sequence | = 2 * PE |
| 0x3C | Healing Rate | = EN / 3 - This value will always be at least 1. |
| 0x40 | Critical Chance | = LK |
| 0x44 | Better Criticals / critical hit modifier stat | Usually 0 |
| 0x48 | Damage Threshold - normal | 0 to 999 |
| 0x4C | Damage Threshold - laser | 0 to 999 |
| 0x50 | Damage Threshold - fire | 0 to 999 |
| 0x54 | Damage Threshold - plasma | 0 to 999 |
| 0x58 | Damage Threshold - electrical | 0 to 999 |
| 0x5C | Damage Threshold - EMP | 0 to 999 |
| 0x60 | Damage Threshold - explosive | 0 to 999 |
| 0x64 | Damage Resistance - normal | 0 to 100 |
| 0x68 | Damage Resistance - laser | 0 to 100 |
| 0x6C | Damage Resistance - fire | 0 to 100 |
| 0x70 | Damage Resistance - plasma | 0 to 100 |
| 0x74 | Damage Resistance - electrical | 0 to 100 |
| 0x78 | Damage Resistance - EMP | 0 to 100 |
| 0x7C | Damage Resistance - explosive | 0 to 100 |
| 0x80 | Radiation Resistance | = 2 * EN |
| 0x84 | Poison Resistance | = 5 * EN |
| 0x88 | Age | 16 to 99 |
| 0x8C | Gender | 1 - female, 0 - male |
| **Bonuses to primary stats** |  |  |
| 0x90 | Bonus to strength | 0 to 10 |
| 0x94 | Bonus to perception | 0 to 10 |
| 0x98 | Bonus to endurance | 0 to 10 |
| 0x9C | Bonus to charisma | 0 to 10 |
| 0xA0 | Bonus to intelligence | 0 to 10 |
| 0xA4 | Bonus to agility | 0 to 10 |
| 0xA8 | Bonus to luck | 0 to 10 |
| **Bonuses to secondary stats** |  |  |
| 0xAC | Bonus HP | 0 to 999 |
| 0xB0 | Bonus AP | 0 to 999 |
| 0xB4 | Bonus AC | 0 to 999 |
| 0xB8 | Bonus to unarmed damage (NOTE: this is not used by the game! Melee Damage stat is used instead.) | - |
| 0xBC | Bonus to melee damage | 0 to 999 |
| 0xC0 | Bonus to carry weight | 0 to 999 |
| 0xC4 | Bonus to sequence | 0 to 99 |
| 0xC8 | Bonus to healing rate | 0 to 99 |
| 0xCC | Bonus to critical chance | 0 to 100 |
| 0xD0 | Bonus to critical hit modifier | 0 to 100 |
| 0xD4 | Bonus to DT (normal) | 0 to 999 |
| 0xD8 | Bonus to DT (laser) | 0 to 999 |
| 0xDC | Bonus to DT (fire) | 0 to 999 |
| 0xE0 | Bonus to DT (plasma) | 0 to 999 |
| 0xE4 | Bonus to DT (electrical) | 0 to 999 |
| 0xE8 | Bonus to DT (EMP) | 0 to 999 |
| 0xEC | Bonus to DT (explosive) | 0 to 999 |
| 0xF0 | Bonus to DR (normal) | 0 to 100 |
| 0xF4 | Bonus to DR (laser) | 0 to 100 |
| 0xF8 | Bonus to DR (fire) | 0 to 100 |
| 0xFC | Bonus to DR (plasma) | 0 to 100 |
| 0x100 | Bonus to DR (electrical) | 0 to 100 |
| 0x104 | Bonus to DR (EMP) | 0 to 100 |
| 0x108 | Bonus to DR (explosive) | 0 to 100 |
| 0x10C | Bonus to radiation resistance | 0 to 100 |
| 0x110 | Bonus to poison resistance | 0 to 100 |
| 0x114 | Bonus to age | 0 to 35 |
| 0x118 | Bonus to gender | Positive values - female, negative or zero - male |
| **Skills Bonus** |  |  |
| 0x11C | Small Guns | 0 to 300-(5+4*AG) |
| 0x120 | Big Guns | 0 to 300-(2*AG) |
| 0x124 | Energy Weapons | 0 to 300-(2*AG) |
| 0x128 | Unarmed | 0 to 300-(30+2*(ST+AG)) |
| 0x12C | Melee Weapons | 0 to 300-(20+2*(ST+AG)) |
| 0x130 | Throwing | 0 to 300-(4*AG) |
| 0x134 | First Aid | 0 to 300-(2*(PE+IN)) |
| 0x138 | Doctor | 0 to 300-(5+PE+IN) |
| 0x13C | Sneak | 0 to 300-(5+3*AG) |
| 0x140 | Lockpick | 0 to 300-(10+PE+AG) |
| 0x144 | Steal | 0 to 300-(3*AG) |
| 0x148 | Traps | 0 to 300-(10+PE+AG) |
| 0x14C | Science | 0 to 300-(4*IN) |
| 0x150 | Repair | 0 to 300-(3*IN) |
| 0x154 | Speech | 0 to 300-(5*CH) |
| 0x158 | Barter | 0 to 300-(4*CH) |
| 0x15C | Gambling | 0 to 300-(5*LK) |
| 0x160 | Outdoorsman | 0 to 300-(2*(EN+IN)) |
| **Critter proto fields** |  |  |
| 0x164 | Body type. The engine resets this to 0 after loading the player character. | 0 = biped |
| 0x168 | Experience value from the critter proto block. The engine resets this to 0 after loading the player character. | Usually 0 |
| 0x16C | Kill type from the critter proto block. The engine resets this to 0 after loading the player character. | Usually 0 |
| 0x170 | Natural damage type. This is the unarmed/native damage type field from critter protos. For the player, `proto_dude_init` resets it to 0 after loading. | 0 = normal |
| **Other** |  |  |
| 0x174-0x193 | Character name | 0 to 32 characters, padded with zeroes |
| 0x194 | First tagged skill | 0 to 17 |
| 0x198 | Second tagged skill | 0 to 17 |
| 0x19C | Third tagged skill | 0 to 17 |
| 0x1A0 | Fourth tagged skill (This stays default value unless GCD was edited manually outside of the game.) | 0 to 17, default is -1 (`FF FF FF FF`) |
| 0x1A4 | First trait | 0 to 15, or -1 for none |
| 0x1A8 | Second trait | 0 to 15, or -1 for none |
| 0x1AC | Character points (for stats) | 0 to 70 - (ST+PE+EN+CH+IN+AG+LK) |

## Tools

- [CGCD](https://fodev.net/files/archive/fo2/cgcd.zip) - [source](https://github.com/rotators/cgcd)
- [Synalysis grammar collection](http://www.synalysis.net/formats.xml)

## Source code

- [Fallout 2 Community Edition GCD reader/writer](https://github.com/alexbatalov/fallout2-ce/blob/main/src/critter.cc)
- [Fallout 2 Community Edition CritterProtoData structure](https://github.com/alexbatalov/fallout2-ce/blob/main/src/proto_types.h)
- [Fallout 2 Community Edition skill indexes](https://github.com/alexbatalov/fallout2-ce/blob/main/src/skill_defs.h)
- [Fallout 2 Community Edition trait indexes](https://github.com/alexbatalov/fallout2-ce/blob/main/src/trait_defs.h)

## History

2020-01-17 - Ported from [https://falloutmods.fandom.com/wiki/GCD_File_Format](https://falloutmods.fandom.com/wiki/GCD_File_Format)
