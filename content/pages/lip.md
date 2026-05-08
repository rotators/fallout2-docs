---
title: LIP File Format
output: lip.html
description: Fallout talking-head LIP lip-sync files, binary layout, marker records, phoneme codes, talking-head FRM mapping, and authoring notes.
---

# LIP File Format

LIP files are talking-head lip-sync files. They do not contain audio or image pixels. A LIP file stores a timed sequence of phoneme codes; the dialogue system uses those codes to choose frames from the current talking head's phoneme FRM while playing the matching speech ACM.

In Fallout 2 dialogue, the path starts in a [MSG](msg.html) file. The MSG entry's audio field is passed to the lip-sync system, the current talking-head art base name selects a speech subdirectory, the LIP file supplies mouth timing, and the matching [ACM](acm.html) file supplies sound.

## Location and lookup

Speech assets are stored below `SOUND\SPEECH`. For a talking head with art-list base name `MYRON` and a MSG audio field `myron01`, the dialogue code looks for:

```text
SOUND\SPEECH\MYRON\myron01.LIP
SOUND\SPEECH\MYRON\myron01.ACM
```

| Input | Source | Use |
|---|---|---|
| Head base name | `art\heads\heads.lst`, through the current talking-head FID. | Speech subdirectory under `SOUND\SPEECH` and the base for talking-head FRM names. |
| Audio token | Second field of the dialogue MSG entry. | Initial LIP filename. Any extension in the token is stripped. |
| LIP internal audio name | The fixed 8-byte name field inside the LIP file. | Final ACM filename used by the lip-sync loader. |

The internal audio name matters. Fallout 2 CE copies the MSG audio token into the LIP state before opening the file, but after the LIP header is read it uses the LIP file's own 8-byte name field when loading the ACM. For normal assets these names match. Tools should warn when they do not.

## Binary properties

| Property | Description |
|---|---|
| File type | Binary, fixed header followed by variable-length phoneme and marker arrays. |
| Version | Version `2` is the Fallout 2 asset format documented here. Fallout 2 CE also contains a reader for an older version `1` layout. |
| Integer byte order | 32-bit integers are read through the normal Fallout file helpers, which store high byte first. |
| Strings | Fixed-size byte arrays. Names are expected to be short, null-terminated game strings. |
| No compression | The LIP file itself is not compressed. The paired speech audio is an ACM file. |

## Version 2 layout

All numeric fields in this table are 32-bit signed values as read by the engine, except for the phoneme byte array and fixed strings.

| Offset | Size | Field | Description |
|---|---|---|---|
| `0x00` | `4` | `version` | File version. Fallout 2 LIP assets use `2`. |
| `0x04` | `4` | `unknown_04` | Usually `0x00005800`. CE reads it as `field_4`. Version 1 data is normalized to this value after loading. |
| `0x08` | `4` | `flags` | Stored flags. Runtime playback uses low bits for internal play state, so files should normally store `0`. |
| `0x0C` | `4` | `unknown_0C` | Read by the engine, not used for the normal frame selection path. |
| `0x10` | `4` | `decoded_audio_length` | Traditional docs describe this as the unpacked ACM length. CE stores it as `field_1C` and uses it only to compute an average marker spacing after sound load. |
| `0x14` | `4` | `phoneme_count` | Number of one-byte phoneme codes following the header. |
| `0x18` | `4` | `unknown_18` | Read by the engine, not used for normal playback. |
| `0x1C` | `4` | `marker_count` | Number of marker records following the phoneme array. |
| `0x20` | `8` | `audio_name[8]` | Base filename for the matching speech ACM. Keep it to at most 7 visible bytes plus a null terminator for original-tool compatibility. |
| `0x28` | `4` | `audio_ext[4]` | Historical extension field. Older docs list `VOC`; CE reads it, then overwrites the runtime value with `ACM`. |
| `0x2C` | `phoneme_count` | `phonemes[]` | One byte per phoneme code. Valid codes are `0` through `41`. |
| `0x2C + phoneme_count` | `marker_count * 8` | `markers[]` | Each marker is two int32 values: marker type and decoded-audio position. |

The expected file size for version 2 is `0x2C + phoneme_count + marker_count * 8`. There is no checksum or footer.

## Marker records

| Relative offset | Size | Field | Description |
|---|---|---|---|
| `0x00` | `4` | `type` | Expected to be `0` or `1`. The playback code validates and logs unexpected values, but frame selection uses only the position and phoneme arrays. |
| `0x04` | `4` | `position` | Position in the decoded speech stream. Playback advances through markers by comparing this value with the current sound position. |

The first marker should have position `0` and a marker type of `0` or `1`. Later marker positions should be monotonically non-decreasing. CE logs invalid marker type and decreasing-position problems, but authoring tools should treat them as errors because they can desynchronize or break mouth playback.

Traditional TeamX/Anchorite documentation describes marker positions as decoded sample offsets and gives the authoring rule `time_seconds * sample_rate * 4` for 22,100 Hz speech. In practice, a tool should align marker units with the sound API used by the target engine and validate against in-game playback.

## Runtime playback

The dialogue UI does not use every MSG lookup as speech. Fallout 2 CE asks for speech when rendering the NPC reply line, but option text is fetched without starting speech. When speech is requested and the MSG audio field is non-empty, the game loads the LIP/ACM pair, starts the sound, then calls the LIP ticker while the dialogue window is active.

During playback, the ticker asks the sound system for the current decoded position, walks forward through marker records, copies the corresponding phoneme code into the current phoneme state, and redraws the talking head when the phoneme changes. When the speech sound stops, the dialogue code ends lip-sync playback and redraws the head at frame `0`.

The LIP marker type is not part of that frame-selection path. It is useful authoring metadata, but mouth movement is driven by marker positions and phoneme codes.

## Counts and alignment

Classic docs describe `marker_count` as usually one more than `phoneme_count`, with an initial zero-time closed-mouth marker. Fallout 2 CE reads both counts independently and does not enforce that relationship. The playback loop indexes the phoneme array by marker progression, so mismatched counts are dangerous even when the loader accepts the file.

- Include an initial silent or closed-mouth phoneme at the beginning.
- Keep marker and phoneme arrays consistent for the target engine and test with the real dialogue screen.
- Do not depend on the loader to reject malformed count combinations.

## Phoneme codes

Fallout 2 CE defines `PHONEME_COUNT` as `42`. Codes `0x00` through `0x29` are valid. During dialogue rendering, each code is mapped to one of the first nine frames in the selected phoneme FRM.

| Code | FRM frame | Code | FRM frame | Code | FRM frame |
|---|---|---|---|---|---|
| `0x00` | `0` | `0x0E` | `1` | `0x1C` | `2` |
| `0x01` | `3` | `0x0F` | `7` | `0x1D` | `2` |
| `0x02` | `1` | `0x10` | `7` | `0x1E` | `2` |
| `0x03` | `1` | `0x11` | `6` | `0x1F` | `2` |
| `0x04` | `3` | `0x12` | `6` | `0x20` | `6` |
| `0x05` | `1` | `0x13` | `2` | `0x21` | `2` |
| `0x06` | `1` | `0x14` | `2` | `0x22` | `2` |
| `0x07` | `1` | `0x15` | `2` | `0x23` | `5` |
| `0x08` | `7` | `0x16` | `2` | `0x24` | `8` |
| `0x09` | `8` | `0x17` | `4` | `0x25` | `2` |
| `0x0A` | `7` | `0x18` | `4` | `0x26` | `2` |
| `0x0B` | `3` | `0x19` | `5` | `0x27` | `2` |
| `0x0C` | `1` | `0x1A` | `5` | `0x28` | `2` |
| `0x0D` | `8` | `0x1B` | `2` | `0x29` | `8` |

Invalid phoneme codes are only logged by CE during load. They can later be used as indexes into the phoneme-to-frame table, so a robust validator should reject any code above `0x29`.

## Talking-head FRMs

LIP playback renders frames from a talking-head phoneme FRM. The current reaction selects which phoneme animation is locked:

| Reaction | Head animation id | Filename suffix | Meaning |
|---|---|---|---|
| Good | `9` | `gp` | Good phoneme frames. |
| Neutral | `10` | `np` | Neutral phoneme frames. |
| Bad | `11` | `bp` | Bad phoneme frames. |

For a head base `MYRON`, the neutral phoneme FRM is built as `art\heads\MYRONnp.frm`. The phoneme FRM must provide at least the frames referenced by the phoneme table, normally frames `0` through `8`.

The full talking-head suffix table used by CE is:

| Animation id | Suffix | Purpose |
|---|---|---|
| `0` | `gv` | Very good reaction. |
| `1` | `gf#` | Good fidget. The number is the selected fidget index. |
| `2` | `gn` | Good to neutral transition. |
| `3` | `ng` | Neutral to good transition. |
| `4` | `nf#` | Neutral fidget. |
| `5` | `nb` | Neutral to bad transition. |
| `6` | `bn` | Bad to neutral transition. |
| `7` | `bf#` | Bad fidget. |
| `8` | `bv` | Very bad reaction. |
| `9` | `gp` | Good phonemes. |
| `10` | `np` | Neutral phonemes. |
| `11` | `bp` | Bad phonemes. |

`heads.lst` is also used for fidget counts. After the head base name, the comma-separated values are read as good, neutral, and bad fidget counts. This metadata is not stored in the LIP file, but it matters for the same talking-head presentation system.

## Version 1 notes

Fallout 2 CE contains a version `1` reader, but the normal documented Fallout 2 format is version `2`. Version 1 has a much larger header with old pointer-like fields, extra extension strings, and a 260-byte path/string field before the phoneme and marker arrays. CE reads those fields, then clears pointer fields and normalizes some runtime values.

| Offset | Size | Field | Notes |
|---|---|---|---|
| `0x00` | `4` | `version` | Value `1`. |
| `0x04` | `4` | `unknown_04` | Normalized by CE after load. |
| `0x08` | `4` | `flags` | Runtime flags. |
| `0x0C` | `4` | `sound_pointer` | Old pointer-like field. CE reads and discards it. |
| `0x10` | `4` | `unknown_10` | Preserved in runtime state. |
| `0x14` | `4` | `buffer_pointer` | Old pointer-like field. CE reads and discards it. |
| `0x18` | `4` | `phoneme_pointer` | Old pointer-like field. CE reads and discards it. |
| `0x1C` | `4` | `decoded_audio_length` | Same practical role as version 2 offset `0x10`. |
| `0x20` | `4` | `start_offset` | Playback start offset. |
| `0x24` | `4` | `phoneme_count` | Number of phoneme bytes after the version 1 header. |
| `0x28` | `4` | `unknown_28` | Preserved in runtime state. |
| `0x2C` | `4` | `marker_count` | Number of marker records after the phoneme bytes. |
| `0x30` | `4` | `marker_pointer` | Old pointer-like field. CE reads and discards it. |
| `0x34` | `0x1C` | `unknowns` | Seven int32 fields preserved or normalized in runtime state. |
| `0x50` | `8` | `audio_name[8]` | Base filename. |
| `0x58` | `4` | `audio_ext[4]` | Extension string. |
| `0x5C` | `4` | `text_ext[4]` | Extension string. |
| `0x60` | `4` | `lip_ext[4]` | Extension string. |
| `0x64` | `260` | `path_or_text[260]` | Old fixed string block. |
| `0x168` | `phoneme_count` | `phonemes[]` | Same byte array concept as version 2. |
| `0x168 + phoneme_count` | `marker_count * 8` | `markers[]` | Same marker record concept as version 2. |

Unless you are preserving known old data, new files should be written as version `2`.

## Reader recipe

1. Open `SOUND\SPEECH\<head>\<audio>.LIP` in binary mode.
2. Read the 32-bit big-endian version.
3. For version `2`, read the fixed fields through `audio_ext[4]`.
4. Allocate and read `phoneme_count` bytes.
5. Allocate and read `marker_count` marker records, each `{int32 type, int32 position}`.
6. Validate phoneme codes, marker types, first marker position, monotonic marker positions, and expected file length.
7. Load the matching ACM from `SOUND\SPEECH\<head>\<audio_name>.ACM`.
8. During playback, advance through markers by decoded sound position and render the mapped frame from the current reaction's phoneme FRM.

## Authoring notes

- Keep the MSG audio token, LIP filename, LIP internal `audio_name`, and ACM filename synchronized.
- Use short uppercase-safe filenames if original tools are involved. The engine buffers are small and the original assets use compact names.
- Start with a closed-mouth/silent phoneme at position `0`.
- Make marker positions non-decreasing and within the decoded audio duration.
- Make sure the selected talking head has `gp`, `np`, and `bp` phoneme FRMs if dialogue can move between reactions.
- Test in the game dialogue screen. A LIP file can parse but still look wrong if marker units, counts, or head FRM frames do not match the target engine.

## Related formats

- [MSG File Format](msg.html) supplies the audio token that starts speech lookup.
- [ACM File Format](acm.html) stores the paired speech audio.
- [FRM File Format](frm.html) stores talking-head phoneme frames.
- [LST File Format](lst.html) documents `heads.lst` and FID/list lookup behavior.

## Source references

- [Fallout 2 CE `lips.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/lips.cc) - LIP loading, validation, marker playback, and ACM path construction.
- [Fallout 2 CE `lips.h`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/lips.h) - LIP runtime structures, flags, and phoneme count.
- [Fallout 2 CE `game_dialog.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/game_dialog.cc) - MSG audio field handoff, reaction-to-phoneme-FRM selection, and phoneme-to-frame table.
- [Fallout 2 CE `art.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/art.cc) and [`art.h`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/art.h) - talking-head animation ids, filename suffixes, and `heads.lst` parsing.
- [Fallout 2 CE `db.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/db.cc) - binary integer/string reading behavior used by LIP.
- [Vault-Tec Labs LIP File Format](https://falloutmods.fandom.com/wiki/LIP_File_Format) - classic TeamX/Anchorite notes on the version 2 layout, talking-head naming, and marker timing.
