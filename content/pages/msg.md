---
title: MSG File Format
output: msg.html
description: Fallout MSG brace-triplet text files, parser behavior, important message paths, PRO/MAP/script dependencies, repository ids, and editing notes.
---

# MSG File Format

MSG files are Fallout and Fallout 2 text message lists. They store numbered strings for dialogue, object names and descriptions, UI labels, combat floats, map names, script errors, and editor-facing vocabulary. The files are plain text, but the engine parser is not line-oriented: it scans for brace-delimited fields and reads entries as repeated `{number}{audio}{text}` triplets.

Most paths below are relative to `master.dat`, `critter.dat`, or an unpacked Fallout data directory. Runtime loading prepends `text\<language>\` to the path passed by the caller, so `game\misc.msg` is normally loaded from `text\english\game\misc.msg` in English installations.

## Format properties

| Property | Description |
|---|---|
| File type | Plain text. There is no magic number, version field, entry count, or table header. |
| Entry shape | Every loaded entry is exactly three brace-delimited fields: `{number}{audio}{text}`. |
| Indexing | The numeric message id is the key. File order is not used after loading. |
| Byte order | Not applicable; MSG files are not binary records. |
| Encoding | Use the game's legacy text encoding for the target language. Treat MSG files as game text assets, not UTF-8 by default, unless the target engine/toolchain explicitly converts them. |

## Entry syntax

```text
{100}{narrator}{Welcome to the wasteland.}
{101}{}{This line has no audio field.}
{102}{v13_intro}{Line breaks inside a field are ignored by the parser.}
```

| Field | Description |
|---|---|
| `number` | Signed decimal message id. The parser accepts an optional leading `+` or `-`, then digits only. Whitespace inside this field makes the file fail to load. |
| `audio` | Optional speech or sound name. Dialogue code uses it when speech playback is requested and the current dialogue head can play speech. |
| `text` | The displayed string. The engine returns this field for normal message lookups. |

The audio and text fields may be empty. The number field may not be empty. The parser does not require one entry per physical line; formatting outside brace fields is ignored until the next opening brace.

Because formatting outside fields is skipped, comments can use almost any convention as long as they remain outside braces and do not contain a stray closing brace before the next entry. Common files often place comments or visual separators between entries; those are not part of the loaded data.

## Parser behavior

The engine repeatedly reads three fields. A field starts at the next `{` and ends at the next `}`. Anything before the next `{` is skipped, which is why comments and blank lines work when they are outside fields.

| Behavior | Consequence |
|---|---|
| Entries are read as `{number}{audio}{text}`. | A missing second or third field makes the file fail to load. |
| EOF before a new entry is normal. | Trailing whitespace or comments outside fields are safe. |
| A stray `}` before the next `{` is an error. | Do not put unbalanced closing braces in comments. |
| The parser has no escape syntax. | A literal `}` cannot be stored in a field. A literal `{` inside a field is read as normal text. |
| Newline characters inside a field are skipped. | Wrapped text joins together without an inserted space unless the file already contains one. |
| Each field uses a fixed 1024-byte buffer. | Keep each number, audio, and text field below 1024 stored characters; shorter is safer for old tools. |
| Numbers are parsed with decimal validation and `atoi`. | Use plain signed decimal ids. Hex values, leading/trailing whitespace, and symbolic names are not accepted by the engine parser. |
| Message ids are kept sorted in memory. | Lookups are by numeric id, not file order. |
| Duplicate ids replace earlier entries while loading. | The last successfully parsed entry for a number is the one that matters. |
| A missing lookup returns the engine's fallback error string. | Some call sites display `Error`; others log an error and return their own fallback. |

A compatible reader can be simple: repeatedly skip characters until `{`, read bytes until `}`, repeat two more times, validate the first field as a signed decimal id, and insert or replace that id in a sorted or hashed message table. EOF is successful only when it happens while searching for the next entry, not while reading an unfinished field.

## Important locations

| Path | Typical use |
|---|---|
| `text\<language>\game\misc.msg` | General game messages loaded during game initialization. |
| `text\<language>\game\map.msg` | Map and city names. |
| `text\<language>\game\script.msg` | Script-system messages. |
| `text\<language>\game\proto.msg` | Prototype editor/type vocabulary such as materials, damage types, calibers, body types, and shared labels. |
| `text\<language>\game\pro_item.msg` | Item prototype names and descriptions. |
| `text\<language>\game\pro_crit.msg` | Critter prototype names and descriptions. |
| `text\<language>\game\pro_scen.msg` | Scenery prototype names and descriptions. |
| `text\<language>\game\pro_wall.msg` | Wall prototype names and descriptions. |
| `text\<language>\game\pro_tile.msg` | Tile prototype names and descriptions. |
| `text\<language>\game\pro_misc.msg` | Misc prototype names and descriptions. |
| `text\<language>\dialog\*.msg` | Script dialogue files, loaded lazily from the script filename. |
| `text\<language>\game\combatai.msg` | Combat AI floats referenced by [AI.TXT](ai_txt.html), `AIBDYMSG.TXT`, and `AIGENMSG.TXT`. |
| `text\<language>\game\combat.msg` | General combat messages, including message ids referenced by [critical hit tables](criticals.html). |
| `text\<language>\game\item.msg` | Shared item-use messages, book text hooks, explosive messages, and other item-system strings. |
| `text\<language>\game\stat.msg`, `skill.msg`, `perk.msg`, `trait.msg` | Displayed names and descriptions for character stats, skills, perks, and traits. |
| `text\<language>\game\pipboy.msg` | Pip-Boy UI labels, holodisk titles, and holodisk body text used with [`holodisk.txt`](pipboy_txt.html). |
| `text\<language>\game\quests.msg` | Quest description lines displayed through [`quests.txt`](pipboy_txt.html). |
| `text\<language>\game\worldmap.msg` | Worldmap UI and encounter text. |

Fallout 2 CE falls back to `text\english\...` when the configured language's message file is missing. The original executables should be treated more conservatively: ship the localized file in the language directory you expect the game to use.

## Critical hit messages

[Critical hit tables](criticals.html) store numeric `Message` and `FailMessage` ids. These ids are looked up in `combat.msg` after the critical effect and any failed defensive stat check have been resolved. If a critical table is edited without matching `combat.msg` entries, the combat log can show fallback lookup text even though the critical effect still applies mechanically.

## PRO dependencies

PRO files do not store object names or descriptions directly. The common PRO header stores a base `message_num`, and the PID object type selects one of the six `pro_*.msg` files. The engine resolves `message_num + 0` as the object name and `message_num + 1` as the description.

| PID type | Message list | Lookup |
|---|---|---|
| `0` item | `game\pro_item.msg` | `message_num + 0` name, `message_num + 1` description. |
| `1` critter | `game\pro_crit.msg` | `message_num + 0` name, `message_num + 1` description. |
| `2` scenery | `game\pro_scen.msg` | `message_num + 0` name, `message_num + 1` description. |
| `3` wall | `game\pro_wall.msg` | `message_num + 0` name, `message_num + 1` description. |
| `4` tile | `game\pro_tile.msg` | `message_num + 0` name, `message_num + 1` description. |
| `5` misc | `game\pro_misc.msg` | `message_num + 0` name, `message_num + 1` description. |

Vanilla prototypes usually use base ids that are multiples of 100, but the engine only adds the small name/description offset to whatever base id the PRO file contains. Tools should not infer an object id from the MSG id; read the PRO header and the corresponding list file instead.

## Proto editor vocabulary

`proto.msg` is not the same as the six `pro_*.msg` name/description files. It supplies shared vocabulary used by the engine and tools: item types, scenery types, materials, damage types, calibers, body types, kill types, and labels such as `<None>`. PRO data fields often store small integer ids that are displayed through `proto.msg` or other standard message files such as `stat.msg` and `perk.msg`.

## MAP dependencies

`map.msg` is a companion table for map and city names. Fallout 2 CE loads `game\map.msg` during map initialization and exposes it as a standard message list.

| Lookup | Message id |
|---|---|
| Map display name for map index `m` and elevation `e`. | `m * 3 + e + 200` |
| Worldmap city name for city index `c`. | `1500 + c` |

This means map names depend on both `maps.txt` ordering and `map.msg` numbering. Reordering maps without updating `map.msg` can make the UI show the wrong map names even when the MAP files themselves still load.

## Script and dialogue use

Compiled scripts call message operations by numeric message-list id and message id. For normal script dialogue, the engine converts the list id to a script index, reads that script's filename from [`scripts\scripts.lst`](scripts_lst.html), strips the extension, and loads `dialog\<scriptname>.msg` on first use.

For example, a script file named `VAULT13.INT` expects dialogue text in `text\<language>\dialog\VAULT13.msg`. The dialogue MSG file is cached after the first lookup. During dialogue speech playback, the audio field is used as the speech name if it is non-empty.

| Script value | Meaning |
|---|---|
| `message_str(list, id)` | Looks up `id` in the dialogue list selected by `list`. In normal dialogue use, `list` is one-based: list `1` maps to the first entry in [`scripts.lst`](scripts_lst.html), list `2` to the second, and so on. |
| `message_str(0, 0)` | Returns an empty string. |
| `message_str(-1, -1)` | Returns an empty string. |
| `message_str(-2, -2)` | Returns message `650` from `proto.msg`. |
| Negative message id | The script opcode returns `Error` without attempting a normal lookup. |

Use the script compiler's generated constants or the same list numbering convention as the game scripts. Passing an arbitrary non-special list id to `message_str` can point at the wrong `scripts.lst` line or fail to load the expected dialogue file.

## Speech audio field

The second MSG field is only meaningful to call sites that ask for speech. Normal string lookups return the text field and ignore the audio field. In game dialogue with speech enabled, `message_str` can pass the audio field to the lip-sync system.

The dialogue code strips any extension from the audio field, combines it with the talking head art filename, and looks below `SOUND\SPEECH\<head>\`. The lip-sync loader reads `<audio>.LIP`, then loads the speech ACM named by the LIP file's internal audio-name field. In normal assets the MSG audio token, LIP filename, internal audio name, and ACM filename all match. Keep the audio token short and extension-free for best compatibility with the original fixed-size buffers. See [LIP File Format](lip.html) for the full lookup path.

## Message-list repository ids

Fallout 2 CE and sfall-compatible script APIs also expose repository lookups such as `message_str_game(listId, messageId)`. These ids are not the same namespace as normal dialogue `message_str` list ids.

| Range | Meaning |
|---|---|
| `0x0000` to `0x0013` | Standard game message lists. Examples include `combat.msg`, `combatai.msg`, `misc.msg`, `item.msg`, `map.msg`, `proto.msg`, `script.msg`, `skill.msg`, `stat.msg`, `trait.msg`, and `worldmap.msg`. Some standard lists are only initialized while their owning UI/system is active. |
| `0x1000` to `0x1005` | Prototype message lists for `pro_item.msg`, `pro_crit.msg`, `pro_scen.msg`, `pro_wall.msg`, `pro_tile.msg`, and `pro_misc.msg` in object-type order. |
| `0x2000` to `0x2FFF` | Persistent extra message lists loaded from sfall's `ExtraGameMsgFileList` setting. |
| `0x3000` to `0x3FFF` | Temporary extra message-list ids reserved by the CE repository. |

Fallout 2 CE's standard-list enum uses these ids:

| Id | Enum name | Common file |
|---|---|---|
| `0` | `STANDARD_MESSAGE_LIST_COMBAT` | `combat.msg` |
| `1` | `STANDARD_MESSAGE_LIST_COMBAT_AI` | `combatai.msg` |
| `2` | `STANDARD_MESSAGE_LIST_SCRNAME` | `scrname.msg` |
| `3` | `STANDARD_MESSAGE_LIST_MISC` | `misc.msg` |
| `4` | `STANDARD_MESSAGE_LIST_CUSTOM` | `custom.msg` |
| `5` | `STANDARD_MESSAGE_LIST_INVENTORY` | `inventry.msg` |
| `6` | `STANDARD_MESSAGE_LIST_ITEM` | `item.msg` |
| `7` | `STANDARD_MESSAGE_LIST_LSGAME` | `lsgame.msg` |
| `8` | `STANDARD_MESSAGE_LIST_MAP` | `map.msg` |
| `9` | `STANDARD_MESSAGE_LIST_OPTIONS` | `options.msg` |
| `10` | `STANDARD_MESSAGE_LIST_PERK` | `perk.msg` |
| `11` | `STANDARD_MESSAGE_LIST_PIPBOY` | `pipboy.msg` |
| `12` | `STANDARD_MESSAGE_LIST_QUESTS` | `quests.msg` |
| `13` | `STANDARD_MESSAGE_LIST_PROTO` | `proto.msg` |
| `14` | `STANDARD_MESSAGE_LIST_SCRIPT` | `script.msg` |
| `15` | `STANDARD_MESSAGE_LIST_SKILL` | `skill.msg` |
| `16` | `STANDARD_MESSAGE_LIST_SKILLDEX` | `skilldex.msg` |
| `17` | `STANDARD_MESSAGE_LIST_STAT` | `stat.msg` |
| `18` | `STANDARD_MESSAGE_LIST_TRAIT` | `trait.msg` |
| `19` | `STANDARD_MESSAGE_LIST_WORLDMAP` | `worldmap.msg` |

These ids are useful for scripting and diagnostics, but they are not a guarantee that a list is currently loaded. Modal UI lists can be unavailable outside the screen that owns them.

| Repository id | Prototype message list |
|---|---|
| `0x1000` | `pro_item.msg` |
| `0x1001` | `pro_crit.msg` |
| `0x1002` | `pro_scen.msg` |
| `0x1003` | `pro_wall.msg` in CE's current registration order. |
| `0x1004` | `pro_tile.msg` in CE's current registration order. |
| `0x1005` | `pro_misc.msg` |

Extra message lists are loaded from the `game` message directory. In CE, an `ExtraGameMsgFileList` entry such as `mytext:12` loads `text\<language>\game\mytext.msg` and assigns it repository id `0x2000 + 12`. Without explicit ids, the order of entries determines the assigned ids, so stable mods should prefer explicit numbering.

One compatibility wrinkle: Fallout 2 CE's public proto-list enum names tiles before walls, but the actual registration loop follows object type order from the prototype system, where walls are type `3` and tiles are type `4`. If a tool exposes raw `0x1003`/`0x1004` repository ids, verify the target engine's behavior instead of trusting enum names alone.

## Filtering and gender words

Dialogue MSG files loaded through the script dialogue path can be filtered after loading. The bad-word filter reads `data\badwords.txt`, uppercases both the bad-word list and a temporary copy of each line, and replaces matching words in the original text when the language filter preference is enabled.

Fallout 2 CE also supports optional sfall-style gender-specific words. When enabled, text in the form `<male^female>` is rewritten according to the player character's gender after the dialogue MSG file is loaded.

These filters are applied by the script dialogue loading path. They should not be assumed for every MSG file. Object names, map names, and general UI strings are usually consumed directly from their loaded message lists.

## Tool implementation notes

- Preserve unknown comments and spacing when possible, but parse only brace triplets if the goal is engine-equivalent behavior.
- Report unmatched braces with file offset information. The engine reports load failure near the current file offset, which is often enough to find a bad entry.
- Warn on duplicate ids. The engine allows them by replacement, but duplicates are usually accidental in source files.
- Warn on ids that look like shifted PRO bases, map indexes, or script list ids when editing a known file type.
- When editing localized files, compare id coverage against the English source file and report missing ids before runtime.
- For dialogue files, optionally validate that non-empty audio fields have matching `.LIP` and `.ACM` assets for the expected talking head.

## Editing notes

- Keep message ids stable. Scripts, PRO records, map names, AI data, and UI code refer to numbers, not to file order.
- Append or reserve ids when compatibility matters. Reusing an id changes every caller that already points at it.
- Keep comments outside braces. A stray closing brace in a comment can break the whole file.
- Do not add spaces inside numeric fields. Use `{100}`, not `{ 100 }`.
- When translating, preserve the numbering and audio fields even if the text changes.
- Do not rely on line order to override behavior except for duplicate ids inside the same file; callers always request a numeric id.
- Avoid literal braces in player-visible text. There is no escape sequence for `}`, and `{` is visually confusing even though it can be stored inside a field.

## Related formats

- [PRO File Format](pro.html) stores object message base ids and selects `pro_*.msg` by PID type.
- [MAP File Format](map.html) stores map indexes that are displayed through `map.msg`.
- [LST File Format](lst.html) documents the indexed lists used by prototypes, art, and scripts.
- [AI.TXT](ai_txt.html) references combat message ranges that resolve through `combatai.msg`.
- [Pip-Boy Text Data](pipboy_txt.html) documents how `quests.txt` and `holodisk.txt` reference `map.msg`, `quests.msg`, and `pipboy.msg`.
- [LIP File Format](lip.html) documents the lip-sync companion files used with dialogue MSG audio fields.

## Source references

- [Fallout 2 CE `message.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/message.cc) - MSG loading, field parsing, duplicate replacement, bad-word filtering, gender-word filtering, and message-list repository behavior.
- [Fallout 2 CE `message.h`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/message.h) - message item structures, field buffer size, and standard/proto message list ids.
- [Fallout 2 CE `proto.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/proto.cc) - `pro_*.msg` loading and PRO name/description lookup.
- [Fallout 2 CE `map.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/map.cc) - `map.msg` loading and map/city name lookup formulas.
- [Fallout 2 CE `pipboy.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/pipboy.cc) - `pipboy.msg` and `quests.msg` loading for Pip-Boy status and holodisk text.
- [Fallout 2 CE `scripts.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/scripts.cc) - `script.msg` loading, lazy `dialog\*.msg` loading, and `message_str` speech handling.
- [Fallout 2 CE `sfall_opcodes.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/sfall_opcodes.cc) - `message_str_game` repository lookups.
- [Fallout 2 CE `game_dialog.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/game_dialog.cc) and [`lips.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/lips.cc) - dialogue audio-field and speech/lip-sync lookup behavior.
