---
title: Pip-Boy Text Data Files
output: pipboy_txt.html
description: Fallout Pip-Boy quests.txt and holodisk.txt text data, message-list dependencies, GVAR behavior, and validation notes.
---

# Pip-Boy Text Data Files

The Pip-Boy status screen combines two small text configuration files with normal [MSG](msg.html) message lists and the current game-global variable array. `data\quests.txt` decides which quest descriptions appear under each location. `data\holodisk.txt` decides which holodisk entries appear in the DATA column. The visible strings themselves are stored in `game\map.msg`, `game\quests.msg`, and `game\pipboy.msg`.

These files are not save files. They are loaded when the Pip-Boy window opens, then interpreted against the current [GVAR](gam.html) values from the running game or save slot.

## Related files

| File | Role |
|---|---|
| `data\quests.txt` | Quest-to-location table. Each row points at a location name, a quest text line, and one GVAR used for visibility and completion state. |
| `data\holodisk.txt` | Holodisk registry. Each row points at one GVAR, one displayed title, and the first MSG id of the holodisk body. |
| `text\<language>\game\map.msg` | Location names used by the quest list. Quest rows store message ids from this file. |
| `text\<language>\game\quests.msg` | Quest descriptions shown after choosing a location in the Status tab. |
| `text\<language>\game\pipboy.msg` | Pip-Boy UI labels, holodisk titles, and holodisk body text. |
| `data\vault13.gam` and saves | Define and store the GVAR indexes used by both text config files. |

## Loading sequence

Fallout 2 CE loads the Pip-Boy text data while opening the Pip-Boy window:

1. Read `data\holodisk.txt` into a holodisk description array.
2. Load `game\pipboy.msg` for UI labels and holodisk text.
3. Load the Pip-Boy interface FRMs.
4. Load `game\quests.msg`.
5. Read `data\quests.txt` into a quest description array and sort it by location id.

The source comment above the quest structure says `data\quest.txt`, but the loader opens `data\quests.txt`. Tools should use the actual plural path.

## Shared text syntax

Both `quests.txt` and `holodisk.txt` are line-oriented text files read with a 256-byte buffer. The parser trims leading whitespace, skips lines whose first non-space character is `#`, and splits data with spaces, tabs, or commas as delimiters. Values are parsed with `atoi`, so use plain decimal integers.

| Syntax feature | Behavior |
|---|---|
| Leading whitespace | Ignored before comment detection and token parsing. |
| `#` comments | Only skipped when `#` is the first non-space character on the line. |
| Separators | Any run of spaces, tabs, or commas separates fields. The common comma-separated style is conventional, not required. |
| Missing fields | The line is ignored once a required token is missing. |
| Extra fields | Ignored by the loader after the required fields are parsed. |
| Inline comments | Unsafe unless they are after all required numeric fields. A trailing `#` token is not special to the tokenizer. |
| Line length | Keep each physical line below 256 bytes for engine-equivalent behavior. |

## quests.txt format

```text
# location, description, gvar_index, display_threshold, completed_threshold
1500, 100, 9, 2, 6
1500, 110, 183, 1, 3
```

| Field | Description |
|---|---|
| `location` | Message id in `game\map.msg`. The engine uses this as the displayed location heading and as the grouping/sorting key. |
| `description` | Message id in `game\quests.msg` for the quest description. |
| `gvar_index` | Index into the game-global variable array. |
| `display_threshold` | The quest is visible when `global_var(gvar_index) >= display_threshold`. |
| `completed_threshold` | The visible quest is drawn as completed when `global_var(gvar_index) >= completed_threshold`. |

After loading, quest rows are sorted by `location`. The Status tab then draws each visible location once, using the first visible quest for that location as the representative row. When the player chooses a location, the engine displays all visible quest rows in that sorted location group.

Completed quests are not removed. They are rendered with strike-through styling and a dimmer color. If a quest should disappear after completion, that behavior has to be implemented through scripts and GVAR choices; `quests.txt` itself only knows the two thresholds.

## Quest display behavior

| Situation | Result |
|---|---|
| No visible quests | The Status tab displays message `203` from `pipboy.msg`. |
| At least one holodisk is known | Quest locations are drawn in the left column and holodisks in the right DATA column. |
| No known holodisks | The Status heading and quest locations are centered instead of using the two-column layout. |
| Quest selected | The detail page title is `map.msg[location]` followed by `pipboy.msg[210]`. |
| Quest description wraps | The engine word-wraps the numbered description to the Pip-Boy content width and applies completion styling to every wrapped line. |

The `location` field is a message id, not necessarily a map index. In vanilla Fallout 2, city names commonly live in the `1500 + city_index` range of `map.msg`, but the quest loader only sees the final message number.

## holodisk.txt format

```text
# gvar_index, name, description
42, 300, 301
```

| Field | Description |
|---|---|
| `gvar_index` | Index into the game-global variable array. The holodisk appears when this value is non-zero. |
| `name` | Message id in `game\pipboy.msg` for the title shown in the DATA list and on the first page of the disk. |
| `description` | First message id in `game\pipboy.msg` for the disk body text. |

Holodisks are displayed in file order. Unlike quests, the holodisk array is not sorted after loading. The list includes only rows whose GVAR value is not zero.

## Holodisk body text

A holodisk body is a run of consecutive `pipboy.msg` entries starting at the row's `description` id. The body ends at a message whose text is exactly `**END-DISK**`. Paragraph breaks are represented by a message whose text is exactly `**END-PAR**`.

```text
{300}{}{Security Disk}
{301}{}{First displayed line.}
{302}{}{**END-PAR**}
{303}{}{Second paragraph line.}
{304}{}{**END-DISK**}
```

| Marker or limit | Behavior |
|---|---|
| `**END-DISK**` | Stops rendering the holodisk. The marker itself is not displayed. |
| `**END-PAR**` | Adds a blank line and is not displayed. |
| 500 message ids | The engine scans at most 500 ids from the starting description id while searching for the end marker. |
| 35 lines per page | Holodisk text paginates after 35 message entries. Paragraph markers count as a line for pagination. |
| First page | The holodisk title is drawn from `pipboy.msg[name]`. |
| Later pages | Later pages continue from the calculated body offset and show the page counter. |

The page counter uses message `212` from `pipboy.msg`, normally the word "of". Navigation labels are also hardcoded message ids in `pipboy.msg`: `200` for More, `201` for Back, and `214` for Done.

## GVAR dependencies

Both tables depend on game-global variable indexes, so they must be edited together with scripts and `vault13.gam` index allocation. The name beside a variable in `vault13.gam` is only documentation for humans; the Pip-Boy loaders store the numeric index from the text file and read the current integer from the runtime array.

| Use | GVAR rule |
|---|---|
| Quest becomes visible | `GVAR >= display_threshold`. |
| Quest appears completed | `GVAR >= completed_threshold`. |
| Holodisk becomes visible | `GVAR != 0`. |

For save editors, the current values are in `SAVE.DAT`, not in `vault13.gam`. For mods, append new GVARs where possible and keep old indexes stable, because scripts, quest rows, holodisk rows, and existing saves all refer to numbers.

## Inter-file dependencies

| Dependency | What can go wrong |
|---|---|
| `quests.txt` -> `map.msg` | A missing location id displays the MSG fallback string instead of a location heading. |
| `quests.txt` -> `quests.msg` | A missing description id produces a bad quest line in the detail page. |
| `holodisk.txt` -> `pipboy.msg` | Missing title or body ids make the DATA list or disk text unreadable. |
| Holodisk body -> end marker | Without `**END-DISK**` within 500 ids, the engine logs an error and can scan unrelated message ids. |
| Text rows -> GVAR indexes | Wrong indexes make quests or holodisks appear too early, too late, completed incorrectly, or never. |
| Scripts -> GVAR thresholds | Scripts must set progression values that match the thresholds used by the tables. |

## Editing notes

- Keep quest rows for the same location together after sorting by location id. The engine sorts by location, but preserving grouped source text makes reviews much easier.
- Use monotonic quest state values when possible, because both visibility and completion are threshold checks.
- Set `completed_threshold` higher than or equal to `display_threshold` unless an already-completed first appearance is intentional.
- Keep holodisk body ids contiguous and reserve enough space for translations that may need more lines.
- Do not reuse holodisk sentinel strings as visible text. Exact marker text has control meaning.
- Validate every referenced MSG id and GVAR index after adding or removing rows.
- When localizing, keep the config numbers stable and translate the MSG text, not the numeric references.

## Validation checklist

- Every `quests.txt` data line has five numeric fields.
- Every `holodisk.txt` data line has three numeric fields.
- Quest location ids exist in `map.msg`.
- Quest description ids exist in `quests.msg`.
- Holodisk title and body ids exist in `pipboy.msg`.
- Every holodisk body reaches `**END-DISK**` within 500 ids.
- Every referenced GVAR index exists in the target game-global array.
- Scripts can actually set the GVAR values required by the display and completion thresholds.
- Save-game testing covers both a fresh game state and a state where some referenced GVARs are already set.

## Source-backed function map

| Function | Relationship to these files |
|---|---|
| `pipboyWindowInit` | Initializes the Pip-Boy window, loads `pipboy.msg`, and calls the holodisk and quest loaders. |
| `questInit` | Loads `game\quests.msg`, parses `data\quests.txt`, and sorts quest rows by location. |
| `pipboyWindowRenderQuestLocationList` | Draws visible quest locations using `map.msg` and the quest display thresholds. |
| `pipboyWindowHandleStatus` | Handles the Status tab, selected quest locations, completed quest strike-through, holodisk selection, and navigation. |
| `holodiskInit` | Parses `data\holodisk.txt`. |
| `pipboyWindowRenderHolodiskList` | Draws known holodisks whose GVAR value is non-zero. |
| `pipboyRenderHolodiskText` | Reads holodisk body lines from `pipboy.msg`, handles markers, pagination, and More/Done labels. |

## Source references

- [Fallout 2 CE `pipboy.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/pipboy.cc) - quest and holodisk loaders, Status tab rendering, GVAR checks, holodisk pagination, and Pip-Boy MSG usage.
- [Quests.txt File Format on The Fallout Wiki](https://fallout.wiki/wiki/Quests.txt_File_Format) - classic field description and examples.
- [Objects in Fallout 2 on Fallout Mod Wiki](https://falloutmod.fandom.com/wiki/Objects_in_Fallout_2) - high-level relationship among holodisks, quests, GVARs, and Pip-Boy message files.

## History

2026-05-08 - Documented Pip-Boy quest and holodisk text data with Fallout 2 CE source-backed behavior by [OpenAI](https://github.com/OpenAI)
