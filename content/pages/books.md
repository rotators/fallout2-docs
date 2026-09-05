---
title: Skill Book Configuration
output: books.html
description: BooksFile INI schema, vanilla skill books, reading gains and time, proto.msg dependencies, and sfall versus Fallout 2 CE behavior.
toc: auto
---

# Skill Book Configuration

`BooksFile` names an optional INI file that maps item prototype ids to skills and reading-result messages. sfall adds this configuration to Fallout 2, and Fallout 2 CE implements a compatible subset with some different loader behavior. Vanilla Fallout 2 has five built-in mappings; it does not read this INI itself.

The file changes which items count as skill books. Reading time, consumption, skill gains, and Comprehension remain engine behavior unless separately modified. The details below were checked against sfall's `Books.cpp`, its distributed `books.ini`, and Fallout 2 CE on 2026-09-05.

## Location and Loading

Set the filename in [ddraw.ini](cfg.html):

```ini
[Misc]
BooksFile=books.ini
```

Place `books.ini` in the game working directory for this example. sfall prepends `.\` to the configured name and checks that the file exists. CE opens the configured path with the ordinary filesystem config reader (`configRead(..., false)`). Neither loader searches DAT archives for this configuration. A file placed only inside `master.dat` will not supply these settings; an explicit relative subdirectory can be used for a loose file.

An empty filename or unreadable file leaves the vanilla mappings available. CE initializes the registry during item initialization, reads vanilla entries first, then custom entries. Its ordinary item reset does not reread the file. Restart the runtime after changing the configuration rather than expecting a save reload to refresh it.

## File Schema

```ini
[main]
count=1
overrideVanilla=0

[1]
PID=626
TextID=802
Skill=12
```

This example registers PID 626 as a Science book and keeps the vanilla books. PID 626 is an example custom item: create or verify that prototype in the target mod before using it. The configuration does not create a PRO or add the item to the player's inventory.

| Section | Key | Meaning |
|---|---|---|
| `[main]` | `count` | Number of numbered sections to scan, starting with `[1]`. Default 0; values above 50 are limited to 50. |
| `[main]` | `overrideVanilla` | Default 0 retains the five built-in entries. Nonzero requests a replacement registry, subject to the loader differences below. |
| `[1]` through `[count]` | `PID` | Full item prototype id, not an FID or a filename. Item PIDs have type byte zero, so ordinary item list ids have the same numeric value. |
| Numbered section | `TextID` | Reading-result message id in `text\<language>\game\proto.msg`. |
| Numbered section | `Skill` | Zero-based skill id, `0..17`. |

Use plain decimal integers and explicit values for all three entry fields. Section numbering controls the scan; `count=2` scans `[1]` and `[2]`, not the first two sections present in the file. Missing sections do not stop the loop. Sections above the capped count are ignored. At most 50 custom sections are processed; keeping the five vanilla entries can give 55 mappings in total.

CE uses the [shared config parser](cfg.html), including semicolon comments and case-insensitive key lookup. sfall uses its own INI reader. The example syntax is suitable for both; do not depend on malformed numeric strings or unspecified fields having identical results.

## Vanilla Mappings

| Book | PID | Skill id | Skill | TextID in proto.msg |
|---|---|---|---|---|
| Big Book of Science | 73 | 12 | Science | 802 |
| Dean's Electronics | 76 | 13 | Repair | 803 |
| First Aid Book | 80 | 6 | First Aid | 804 |
| Guns and Bullets | 102 | 0 | Small Guns | 805 |
| Scout Handbook | 86 | 17 | Outdoorsman | 806 |

With `overrideVanilla=1`, include every vanilla mapping that should remain readable. Removing a mapping removes the default book treatment; it does not remove its prototype or existing instances.

## Skill Ids

| Id | Skill | Id | Skill |
|---|---|---|---|
| 0 | Small Guns | 9 | Lockpick |
| 1 | Big Guns | 10 | Steal |
| 2 | Energy Weapons | 11 | Traps |
| 3 | Unarmed | 12 | Science |
| 4 | Melee Weapons | 13 | Repair |
| 5 | Throwing | 14 | Speech |
| 6 | First Aid | 15 | Barter |
| 7 | Doctor | 16 | Gambling |
| 8 | Sneak | 17 | Outdoorsman |

These are skill ids, not SPECIAL stat ids or message ids. The registry supports skills beyond the five vanilla book skills. Choose a result message that describes the configured skill: changing `Skill` does not automatically change the wording selected by `TextID`.

## sfall and CE Differences

| Situation | Inspected sfall loader | Inspected CE loader |
|---|---|---|
| Duplicate PID | Searches backward; the last loaded entry wins. Custom entries can replace a retained vanilla mapping. | Searches forward; the first loaded entry wins. A retained vanilla entry shadows a custom duplicate. |
| Missing PID | Defaults to 0 and skips the entry. | Missing key skips the entry, but an explicit 0 is accepted into the registry. |
| Missing TextID or Skill | Defaults to 0 for each missing field when PID is nonzero. | Skips the entry if either key is missing. |
| `count<=0` with replacement requested | Does not install the book hook, so the vanilla handler remains. | Clears the vanilla registry after reading replacement mode, then adds no entries. |
| Invalid skill or nonexistent prototype | Loader does not validate the reference. | Loader does not validate the reference. |

For a replacement that behaves consistently in both, set `overrideVanilla=1`, use a positive count, include all desired entries, and keep PIDs unique. Do not use an empty replacement file as a portable way to disable all books. Do not rely on an invalid entry to fail safely: in CE, an invalid skill can still lead to reading messages, time advancement, and consumption even though skill increments are rejected.

## Reading and Skill Gains

In CE, `_protinst_use_item` tries book recognition for weapon and miscellaneous item subtypes. Registering an armor, container, or drug PID alone does not route that item through this handler. A custom book also needs usable prototype settings; cloning a working book's PRO is a useful starting point. The configuration supplies no art, name, description, or use-action flags.

The reader operates on `gDude`: skills, Intelligence, and Comprehension are taken from the player. This is not a companion-training registry.

Outside combat, the algorithm is:

```text
increase = (100 - currentSkillValue) / 10   // integer division
if increase > 0:
    if player has Comprehension:
        increase = (150 * increase) / 100
    repeat increase times:
        skillAddForce(player, skill)
else:
    resultMessage = 801
```

The current value comes from `skillGetValue`, including applicable stat, tag, trait, perk, and difficulty modifiers. The gain is computed once before incrementing. Intelligence directly determines reading time and can also indirectly affect the starting value of skills that depend on it.

Comprehension multiplies the already-rounded increment count by 1.5, rounding down again. It is checked as a present/absent perk, not multiplied by its rank. `skillAddForce` adds one invested skill point per call without spending the player's unspent skill points. Tagged skills double that invested-point contribution in the displayed skill calculation, so increment count is not always the displayed percentage gain.

| Starting effective skill | Increment calls | With Comprehension |
|---|---|---|
| 40 | 6 | 9 |
| 75 | 2 | 3 |
| 90 | 1 | 1 |
| 91 | 0 | 0 |
| 100 or above | 0 | 0 |

For an ordinary untagged skill with unchanged modifiers, 75 becomes 77, or 78 with Comprehension. A tagged skill starting at 75 instead gains 4 displayed points, or 6 with Comprehension. The practical cutoff is 91, where integer rounding already produces no gain. This is not a hard post-reading clamp to 100: changes in temporary modifiers can affect the eventual displayed value. The skill increment routine separately checks the general 300 skill limit.

## Time, Consumption, and Messages

Reading in combat is rejected with `proto.msg` id 902. The handler returns without its reading-time advancement or book consumption.

Otherwise reading advances game time by `3600 * (11 - Intelligence)` seconds: 10 hours at Intelligence 1, 6 hours at 5, and 1 hour at 10. CE fades the palette, advances time, and executes map update scripts before restoring the palette and displaying the reading messages.

| Message id | Use |
|---|---|
| 800 | General book-read notification. |
| Configured TextID | Result notification when the computed gain is positive. |
| 801 | Replaces TextID when the computed gain is zero or negative. |
| 902 | Reading denied in combat. |

All four lookups use `proto.msg`. The item name and description remain in `pro_item.msg` through the PRO's `message_num`; neither those strings nor `item.msg` supply `TextID`. A missing reading message is skipped by CE's guarded lookup and does not undo the skill change or time advancement.

The handler returns 1 after reading, even when there was no gain. Its normal caller removes one inventory item and destroys it. Thus an unhelpful book still takes time and is consumed. `BooksFile` has no key for reusable books, fixed skill gains, a custom cap, or reading duration; those require separate scripting or engine features.

## Modding Dependencies

- Add the item [PRO](pro.html) and its indexed `proto\items\items.lst` entry; verify subtype, use availability, and art references.
- Add its name and description to `pro_item.msg`, and any custom reading-result text to [proto.msg](msg.html) in each supported language.
- Register the same PID in the configured loose INI. Use a unique PID or the explicit replacement strategy described above.
- Test a low skill, skill 90, skill 91, a tagged skill, Comprehension, and attempted reading during combat. Check inventory quantity and elapsed time as well as displayed text.

The registry is startup configuration rather than a save-slot book-definition file. Existing inventory objects still carry their PIDs, so changing the registry can change how those objects behave in an existing save after restarting. Already-earned skill points are not rolled back when a mapping changes.

## Sources

- [sfall distributed books.ini](https://github.com/sfall-team/sfall/blob/master/artifacts/config_files/books.ini): supported fields and intended vanilla override behavior.
- [sfall Books.cpp](https://github.com/sfall-team/sfall/blob/master/sfall/Modules/Books.cpp): filesystem path, 50-entry limit, defaults, backward lookup, and hook installation.
- [Fallout 2 CE item.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/item.cc): `booksInitVanilla`, `booksInitCustom`, and `booksGetInfo`.
- [Fallout 2 CE proto_instance.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/proto_instance.cc): `_obj_use_book`, `_protinst_use_item`, and `_obj_use_item`.
- [Fallout 2 CE skill.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/skill.cc): `skillGetValue` and `skillAddForce`.
- [Fallout 2 CE config.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/config.cc): filesystem versus database config reading.
