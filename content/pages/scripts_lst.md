---
title: SCRIPTS.LST File Format
output: scripts_lst.html
description: Fallout scripts.lst registry format, script indexes, local_vars metadata, dialogue MSG binding, packed script ids, and validation notes.
---

# SCRIPTS.LST File Format

`scripts\scripts.lst` is the indexed registry for compiled Fallout and Fallout 2 scripts. Each physical line names one [INT](int.html) bytecode file, and the zero-based line number is used by MAP records, PRO records, script message lookups, decompiler headers, and modding tools.

Although it looks like a normal [LST](lst.html) file, `scripts.lst` has its own parser rules. It uses `.int` filenames, strips the extension before storing the script name, and can carry `local_vars=N` metadata after a `#` marker.

## Format Properties

| Property | Description |
|---|---|
| Location | `scripts\scripts.lst`, relative to `master.dat`, `patch000.dat`, or an unpacked data directory. |
| File type | Plain text index table. |
| Indexing | Zero-based by physical line number. The first line is script index `0`. |
| Entry value | A script filename containing `.int`. The engine stores the base name without the extension. |
| Script file lookup | `scripts\<base>.int`. |
| Dialogue file lookup | `text\<language>\dialog\<base>.msg`. |
| Metadata | Optional `# local_vars=N` text on the same line. |
| Compression | None at the file level. The file may be stored inside a compressed [DAT](dat.html) entry. |

## Example

```text
arroyo.int        # local_vars=4
doorlook.int      # local_vars=0
v13door.int
```

Line `0` resolves to `scripts\arroyo.int` and, when script message list `1` is used, to `dialog\arroyo.msg`. Line `1` resolves to `scripts\doorlook.int` and message list `2`. The message-list id is one-based, while the script-list index is zero-based.

## Parser Behavior

Fallout 2 CE opens `scripts.lst` as text and reads it line by line into a 260-byte buffer. Each read line appends one entry to the script list before the parser extracts the name and local-variable metadata.

| Behavior | Consequence |
|---|---|
| Every physical line becomes an entry. | Blank or malformed lines still shift all following indexes. Avoid blank lines in production lists. |
| The parser searches for lowercase `.int`. | Use lowercase extension text for best compatibility with the CE source path. Older tools may be more forgiving, but a portable list should not rely on that. |
| The stored name is the text before `.int`. | The engine later appends `.int` again when building the runtime script filename. |
| Names longer than 13 characters before `.int` fail to load. | Keep script base names at 13 characters or fewer. |
| The name is copied from the start of the line. | Do not indent entries. Leading spaces become part of the copied base name. |
| `#` enables metadata scanning. | The loader looks for `local_vars=` only if the line contains `#`. |
| `local_vars=` is parsed with `atoi`. | Use plain decimal integers. Text after the number is ignored by `atoi`. |
| Missing `local_vars` defaults to zero. | Scripts without script-local state do not need the metadata. |

The loader does not use the semicolon comment convention described for many art lists. A semicolon can still appear in the text, but it has no special meaning to the `scripts.lst` parser. Use `# local_vars=N` when the line needs local-variable metadata.

## Script Indexes

The zero-based index is the most important value in the file. It selects the compiled INT filename, the dialogue MSG filename, and the local-variable count for runtime script instances.

| Use | Indexing rule |
|---|---|
| Runtime INT path | `scriptsGetFileName(index)` returns `<base>.int`, and the script loader opens it below `scripts\`. |
| Dialogue message list | `message_str(listId, messageId)` converts `listId` to `index = listId - 1`, then loads `dialog\<base>.msg`. |
| MAP script record | The record's `script_index` field is a zero-based index into `scripts.lst`. |
| MAP header script | The map header stores a positive one-based script index; CE subtracts one when creating the system script record. |
| PRO script id | Object prototypes store packed script ids whose low bits identify an entry in `scripts.lst`. |
| Local variables | The runtime uses the script record's `script_index` to fetch the matching `local_vars` count from `scripts.lst`. |

Because the list is an index namespace, sorting it alphabetically is destructive. Reordering a working `scripts.lst` can silently attach the wrong script code to objects and maps, and can make `message_str` load the wrong dialogue file.

## Packed Script Ids

Two related values are easy to confuse:

| Value | Where used | Meaning |
|---|---|---|
| Script-list index | `scripts.lst`, MAP script records, message-list ids after subtracting one. | Selects the script base name and metadata line. |
| Runtime script id (`sid`) | MAP object records, MAP script records, queued events, live script lists. | Identifies a live script instance by type and per-type runtime id. |

The script type is stored in the high byte. Fallout 2 CE extracts it with `SID_TYPE(value) = value >> 24`. The known script types are:

| Value | Name | Typical role |
|---|---|---|
| `0` | System | Map/system scripts. The map script created from the MAP header uses this type. |
| `1` | Spatial | Spatial trigger scripts that fire when objects enter a tile or radius. |
| `2` | Timed | Timed script instances and queued timer callbacks. |
| `3` | Item | Item, scenery, wall, and other object-use scripts. |
| `4` | Critter | Critter scripts. |

Older format notes often write stored script references as `0x0Y00XXXX`, where `Y` is the script type and `XXXX` is the `scripts.lst` index. That is a useful way to read many PRO and mapper-authored references. Runtime script ids in saved maps are different: CE allocates a per-type live id with `scriptGetNewId(type)` and combines it as type shifted left 24 bits plus the live id.

Special sentinel values are also common. `0xFFFFFFFF` means no script. Fallout 2 CE also treats `0xCCCCCCCC` as an unset/debug fill value and rejects it when resolving a live script pointer.

## Local Variables

The `local_vars=N` suffix describes the number of script-local variables that should be backed by the current map's local-variable array. These are not the same as SSL procedure-local symbols, which are compiled into INT stack/register usage. They are persistent script locals accessed through script local-variable operations at runtime.

CE loads the count into a `ScriptsListEntry.local_vars_num` field. When a script instance first needs local variables and its count is zero, the engine looks up the script instance's `script_index`, copies the count from `scripts.lst`, and allocates a range in the MAP local-variable array if the count is greater than zero.

| Case | Behavior |
|---|---|
| Clean mapper-authored map | Loaded script records have `local_vars_count` cleared when the saved-map flag is not set. Counts can be rediscovered from `scripts.lst`. |
| Saved map state | The saved-map flag preserves script local-variable counts, offsets, and values from the MAP local-variable array. |
| System/map script | CE rejects script local variables for system scripts and logs that system/map scripts are not allowed local vars. |
| Missing metadata | The script has a count of zero. Reads may return no useful value, and writes fail because no local range is allocated. |

Changing `local_vars=N` after maps have shipped is a save-compatibility risk. Clean maps can recover from the list, but saved maps may already contain offsets and values laid out according to the previous count.

## Dialogue MSG Binding

Normal dialogue string lookup uses `scripts.lst` as an indirect filename table. `message_str(listId, messageId)` uses `listId - 1` as a script-list index, appends `.int`, strips the extension again, and loads `dialog\<base>.msg`.

```text
scripts.lst line 12:  artemple.int
SSL/INT lookup:      message_str(13, 100)
Runtime MSG file:    text\english\dialog\artemple.msg
```

This is why script-name constants in SSL headers are usually one-based message-list ids, not raw zero-based `scripts.lst` indexes. A mismatch can be subtle: the INT script runs correctly, but its dialogue calls display another script's text or fail to load a message file.

## Procedure Dispatch

`scripts.lst` does not list procedures. Procedure names are stored inside each compiled [INT](int.html) file, and the engine maps known procedure names to built-in script events.

| Index | Procedure name | Typical event |
|---|---|---|
| `1` | `start` | Script initialization/start pass. |
| `2` | `spatial_p_proc` | Spatial trigger entered. |
| `3` | `description_p_proc` | Description request. |
| `4` | `pickup_p_proc` | Pickup event. |
| `5` | `drop_p_proc` | Drop event. |
| `6` | `use_p_proc` | Use event. |
| `7` | `use_obj_on_p_proc` | Use object on object. |
| `8` | `use_skill_on_p_proc` | Use skill on object. |
| `11` | `talk_p_proc` | Dialogue start. |
| `12` | `critter_p_proc` | Critter update. |
| `13` | `combat_p_proc` | Combat behavior. |
| `14` | `damage_p_proc` | Damage event. |
| `15` | `map_enter_p_proc` | Map enter. |
| `16` | `map_exit_p_proc` | Map exit. |
| `17` | `create_p_proc` | Object/script creation. |
| `18` | `destroy_p_proc` | Object/script destruction. |
| `21` | `look_at_p_proc` | Look-at event. |
| `22` | `timed_event_p_proc` | Queued/timed event. |
| `23` | `map_update_p_proc` | Periodic map update. |
| `24` | `push_p_proc` | Push event. |
| `25` | `is_dropping_p_proc` | Drop query. |
| `26` | `combat_is_starting_p_proc` | Combat start notification. |
| `27` | `combat_is_over_p_proc` | Combat end notification. |

Indexes `9`, `10`, `19`, and `20` are represented as unused or bad procedure slots in the CE procedure-name table, although historical comments associate them with old use-ad/use-disad and barter hooks. A documentation tool should keep these slots reserved instead of renumbering later procedure ids.

## Runtime Script Instances

When the engine creates a live script, it stores both a runtime `sid` and a `script_index`. The `sid` identifies the live instance, while `script_index` selects the entry in `scripts.lst` that names the INT program.

| Script kind | Runtime notes |
|---|---|
| Map/system script | Created from the MAP header's one-based script index when the value is greater than zero. |
| Object script | Created from PRO or MAP object script information and owned by the placed object. |
| Spatial script | Stores a packed built tile and radius. CE fires matching spatial scripts only for visible, non-flat objects and temporarily disables spatial recursion while dispatching. |
| Timed script | Can be scheduled through queued events. The queued event stores the live `sid` and a fixed parameter. |
| sfall global script | CE/sfall can run global-script update hooks before normal map update script dispatch. Those hooks are related to scripting, but not every sfall global-script configuration is a plain `scripts.lst` line. |

Script records are serialized in MAP saves in five fixed type lists: system, spatial, timed, item, and critter. The live lists and their saved records are runtime state; `scripts.lst` remains the static table that resolves the program name and metadata for those records.

## Inter-file Dependencies

| File | Dependency |
|---|---|
| [SSL](ssl.html) | Source headers usually define script constants that must match `scripts.lst` line numbers or one-based message-list ids. |
| [INT](int.html) | The compiled program loaded for a line is `scripts\<base>.int`. |
| [MSG](msg.html) | Dialogue text for a script is normally `dialog\<base>.msg`; `message_str` selects it through the script-list index. |
| [PRO](pro.html) | Prototype script fields store packed references that ultimately select `scripts.lst` entries. |
| [MAP](map.html) | Map headers, object records, and script records store script indexes or runtime SIDs that must line up with this list. |
| [GAM](gam.html) | Global variable indexes used by scripts are documented separately, but script behavior often depends on both `scripts.lst` and global/map variable tables. |
| [DAT](dat.html) | Packed archives provide the actual `scripts.lst`, `*.int`, and `dialog\*.msg` files loaded by the engine. |

## Editing Guidance

Treat `scripts.lst` as append-only whenever possible. Adding a new script at the end preserves existing script references, dialogue list ids, and local-variable metadata indexes. Inserting a line in the middle should be treated as a migration that requires auditing every PRO, MAP, SSL header constant, dialogue message-list constant, and saved-map compatibility expectation.

| Edit | Risk |
|---|---|
| Rename an entry | Requires renaming the INT file and usually the matching dialogue MSG file. Existing MAP/PRO references still point to the same index but now run different code. |
| Move an entry | Breaks all index-based references unless every dependent file and header constant is updated. |
| Delete an entry | Shifts all following indexes. Use a harmless placeholder instead if the index must remain reserved. |
| Change `local_vars` | Can affect local-variable allocation and saved-map compatibility. |
| Change case | May affect unpacked installs or tools on case-sensitive filesystems. Preserve original case unless the target engine normalizes it. |

## Validation Checklist

- Every non-placeholder line contains a lowercase `.int` extension.
- No script base name before `.int` is longer than 13 characters.
- Lines are not indented.
- There are no accidental blank lines in the middle of the list.
- All listed `scripts\*.int` files exist in the target data set or patch archive.
- Dialogue scripts have matching `dialog\*.msg` files when they call `message_str`.
- SSL header constants match the final line numbers and message-list ids.
- PRO and MAP references have been audited after any insertion, deletion, or reordering.
- `local_vars=N` counts match the script's intended persistent local-variable usage.

## Source Code Notes

The behavior above is backed by Fallout 2 CE source. Important functions and structures include:

| Source symbol | What it shows |
|---|---|
| `ScriptsListEntry` | Stores `name[16]` and `local_vars_num`. |
| `scriptsLoadScriptsList` | Reads `scripts.lst`, strips `.int`, rejects names longer than 13 characters, and parses `local_vars=`. |
| `scriptsGetFileName` | Reconstructs `<base>.int` from a script-list index. |
| `scriptsGetMessageList` | Converts a one-based script message-list id into `dialog\<base>.msg`. |
| `ScriptType` | Defines system, spatial, timed, item, and critter script type values. |
| `scriptGetNewId` | Builds runtime SIDs from a script type and per-type live id. |
| `scriptGetLocalVar` / `scriptSetLocalVar` | Shows how `local_vars` metadata is applied and why system/map scripts reject local vars. |
| `gScriptProcNames` | Lists the engine-recognized built-in procedure names. |

## References

- [Fallout 2 Community Edition scripts.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/scripts.cc)
- [Fallout 2 Community Edition scripts.h](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/scripts.h)
- [Fallout 2 Community Edition obj_types.h](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/obj_types.h)
- [Generic LST file documentation](lst.html)
- [MAP File Format](map.html)
- [PRO File Format](pro.html)
- [INT File Format](int.html)
- [SSL Script Source Format](ssl.html)
- [MSG File Format](msg.html)

## History

2026-05-07 - Added dedicated `scripts.lst` documentation from Fallout 2 CE source and the existing MAP, PRO, INT, SSL, and MSG notes.
