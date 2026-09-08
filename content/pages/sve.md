---
title: SVE File Format
output: sve.html
description: Fallout SVE subtitle cue files for MVE movies, including syntax, lookup paths, parser behavior, timing, rendering, and authoring notes.
---

# SVE File Format

SVE files are plain text subtitle cue lists for Fallout and Fallout 2 [MVE](mve.html) movies. They do not contain audio, video, palettes, or timing in seconds. Each subtitle is keyed by a movie frame number, and the movie player displays the text when playback reaches that frame.

The format is intentionally small, but the engine behavior around it matters: subtitle files are looked up from the current language folder, parsed in file order, rendered with movie-specific palettes, and synchronized to decoded movie frame counts rather than wall-clock time.

## Format Properties

| Property | Description |
|---|---|
| File type | Plain text subtitle cue list. |
| Typical path | `text\<language>\cuts\*.SVE`. |
| Companion file | A same-basename [MVE](mve.html) movie below `art\cuts` or `art\<language>\cuts`. |
| Timing unit | Displayed movie frame number, not milliseconds, seconds, ticks, or audio samples. |
| Byte order | Not applicable; SVE is text. |
| Encoding | Use the game's legacy text encoding for the target language. Do not assume UTF-8 unless the target engine or mod explicitly converts text. |
| Storage | May be loose in `data\text\<language>\cuts` or packed in a [DAT](dat.html) archive using the same virtual path. |

## Basic Syntax

Each cue is one line:

```text
20:Opening subtitle text.
72:Second subtitle text.
135:A colon inside the text is allowed: it belongs to the subtitle.
```

| Part | Description |
|---|---|
| `frame` | Decimal frame number. In normal files this should be a non-negative integer and should increase from line to line. |
| `:` | The first colon separates the frame field from the subtitle text. |
| `text` | The displayed subtitle. Everything after the first colon is copied as subtitle text, including later colons. |

There is no header, cue count, duration field, closing marker, or end-of-subtitle syntax. A line remains the current subtitle until another due line replaces it, the movie redraws over it, or playback ends. For practical authoring, treat each line as "show this text starting at this frame."

## Fallout Lookup

The movie system first resolves the MVE path, then derives the SVE path from the movie filename. Fallout 2 CE uses this logic:

1. Resolve the movie name from the fixed movie table, such as `intro.mve`.
2. If the current language is not English, try `art\<language>\cuts\intro.mve`.
3. If the localized movie is missing, fall back to `art\cuts\intro.mve`.
4. If subtitles are enabled in preferences, strip the movie directory and extension.
5. Build `text\<language>\cuts\intro.SVE`.

The subtitle lookup always uses the configured text language path. It does not follow the final MVE directory. For example, a game can fall back to `art\cuts\intro.mve` while still loading `text\french\cuts\intro.SVE`.

| Movie path | Subtitle path when language is `english` |
|---|---|
| `art\cuts\intro.mve` | `text\english\cuts\intro.SVE` |
| `art\cuts\credits.mve` | `text\english\cuts\credits.SVE` |
| `art\french\cuts\elder.mve` | `text\french\cuts\elder.SVE` |

The engine checks for the SVE file before enabling subtitle rendering for normal game movies. If the file is missing, the movie still plays and the subtitle flag is cleared.

## Movie Table

Fallout 2 CE's game-movie table has 17 entries. SVE filenames are derived from these movie basenames.

| Index | MVE | SVE basename | Subtitle palette |
|---|---|---|---|
| `0` | `iplogo.mve` | `iplogo.SVE` | `subtitle.pal` if subtitles are used. |
| `1` | `intro.mve` | `intro.SVE` | `introsub.pal` |
| `2` | `elder.mve` | `elder.SVE` | `eldersub.pal` |
| `3` | `vsuit.mve` | `vsuit.SVE` | `subtitle.pal` |
| `4` | `afailed.mve` | `afailed.SVE` | `artmrsub.pal` |
| `5` | `adestroy.mve` | `adestroy.SVE` | `subtitle.pal` |
| `6` | `car.mve` | `car.SVE` | `subtitle.pal` |
| `7` | `cartucci.mve` | `cartucci.SVE` | `subtitle.pal` |
| `8` | `timeout.mve` | `timeout.SVE` | `artmrsub.pal` |
| `9` | `tanker.mve` | `tanker.SVE` | `subtitle.pal` |
| `10` | `enclave.mve` | `enclave.SVE` | `subtitle.pal` |
| `11` | `derrick.mve` | `derrick.SVE` | `subtitle.pal` |
| `12` | `artimer1.mve` | `artimer1.SVE` | `artmrsub.pal` |
| `13` | `artimer2.mve` | `artimer2.SVE` | `artmrsub.pal` |
| `14` | `artimer3.mve` | `artimer3.SVE` | `artmrsub.pal` |
| `15` | `artimer4.mve` | `artimer4.SVE` | `artmrsub.pal` |
| `16` | `credits.mve` | `credits.SVE` | `crdtssub.pal` |

Movies without a special palette entry use `art\cuts\subtitle.pal`. The [palette](pal.html) selection is tied to the movie index, not the SVE file itself.

## Parser Behavior

Fallout's movie subtitle parser is line-based. Fallout 2 CE's `movieLoadSubtitles` implementation reads up to 259 characters into a temporary buffer for each line, strips line endings, splits at the first colon, parses the left side with `atoi`, and stores the right side as subtitle text.

| Behavior | Consequence |
|---|---|
| Only the first colon is structural. | `100:Vault: Level 13` displays `Vault: Level 13`. |
| The frame number is parsed with `atoi`. | Leading spaces and a leading sign are tolerated by the C library, but nonnumeric prefixes become frame `0`. Use clean decimal numbers. |
| CR and LF are removed before parsing. | Windows CRLF and Unix LF line endings are both harmless in CE-style code. |
| Lines without a colon are rejected for display. | They are not comments in the useful sense. Keep comments out of shipped SVE files unless the target engine/tool explicitly supports them. |
| Cues are kept in input order. | Sort by increasing frame number. A cue with a lower frame after a future cue will be delayed behind that future cue. |
| There is no duplicate-id merge step. | Two cues with the same frame are both due at the same time; in practice the later one in that due batch can immediately replace the earlier draw. |
| The line buffer is small. | Keep source lines below 259 bytes for vanilla-like behavior. Short subtitle lines are safer for wrapping anyway. |
| The parser does not decode escape sequences. | `\n`, `\t`, or escaped colons are just text unless a custom engine adds processing. |

Because the parser uses `atoi`, malformed frames are easy to accept accidentally. Authoring tools should validate more strictly than the game: require `^[0-9]+:`, reject negative frames, warn about duplicate frames, and warn if frame numbers decrease.

## Timing Model

SVE timing is frame-count based. During playback the movie code asks the MVE decoder for frame counts, then consumes all subtitle nodes whose stored frame is less than or equal to the current displayed frame.

```text
while next_subtitle exists:
    if current_movie_frame < next_subtitle.frame:
        stop
    clear subtitle band
    draw next_subtitle.text
    remove next_subtitle from the list
```

This has several important effects:

- Subtitle timing follows the movie decoder, not the audio device or real-time clock.
- If frames are dropped internally, due subtitles are caught up on the next render/update pass.
- The same SVE file depends on the MVE's real frame cadence. If a replacement movie has different frame timing, the subtitle frame numbers must be retimed.
- There is no explicit duration. A subtitle is replaced by the next subtitle event or by later drawing over the subtitle area.
- There is no built-in way to clear a subtitle except to schedule a later cue with empty text or whitespace, assuming the target parser and renderer handle that as desired.

## Rendering

Normal game movies play in a 640 by 480 window. When subtitles are enabled, Fallout 2 CE loads a subtitle [palette](pal.html), switches to font `101`, and sets text color to white before playback. The lower-level movie renderer centers subtitle text across the movie window.

The subtitle Y position is computed from the most recently displayed movie rectangle. In simplified form, it places one subtitle band in the vertical space below the movie image:

```text
y = (480 - movie_height - movie_y - line_height) / 2 + movie_height + movie_y
```

For movies that leave a bottom subtitle band, this centers the subtitle in that band. For full-height movies, the subtitle appears near the bottom edge and can be overwritten by later video drawing depending on the playback path and content.

| Rendering field | Behavior |
|---|---|
| Horizontal alignment | Centered. |
| Width | The full movie window width is used for wrapping. |
| Height | Normally one font line height plus four pixels, clamped to the bottom of the screen if necessary. |
| Clear behavior | The subtitle band is filled with palette index `0` before drawing a due subtitle. |
| Color | White by default through the current subtitle palette and color table. |
| Font | Game-movie playback uses font `101` while subtitles are active. |

Long subtitle lines should be avoided. The renderer can wrap text, but the available subtitle band is small and old movie subtitles were authored as short, single-line cues.

## Configuration Dependency

SVE loading depends on the game's subtitle preference. In [fallout2.cfg](cfg.html), this is the `[preferences]` `subtitles` setting. If subtitles are disabled, the SVE path is not useful for normal movie playback even if the file exists.

When subtitles are enabled but the derived SVE file is missing, Fallout 2 CE disables the movie subtitle flag for that playback. It does not treat missing subtitles as a movie error.

## Relationship To Other Text Formats

| Format | Relationship |
|---|---|
| [MVE](mve.html) | The video/audio container. SVE is external and same-basename, not embedded in the MVE stream. |
| [MSG](msg.html) | Dialogue and game-message text. MSG uses brace-delimited numbered records; SVE uses line-based frame cues. |
| [LIP](lip.html) | Talking-head mouth timing. LIP synchronizes phoneme/mouth shapes to speech; SVE synchronizes plain text to movie frames. |
| [Endgame narrator `.txt`](endings.html) | Slideshow text ignores the prefix before the colon and derives timing from speech/text length. Death screens use a separate whole-file reader. Neither is SVE movie subtitles. |
| [credits.txt and quotes.txt](credits.html) | Scrolling text with first-column style markers, not frame cues. A `credits.SVE` can accompany `credits.mve`, but does not control this text renderer. |

## Authoring Notes

- Use the exact same basename as the MVE file, with the SVE stored below `text\<language>\cuts`.
- Retiming means changing frame numbers, not timestamps. Convert from seconds by multiplying by the movie's real frame rate, then adjust by visual testing.
- Keep lines short enough to fit in the subtitle band. Prefer multiple cues over a very long wrapped cue.
- Keep frame numbers monotonic. The engine's linked-list consumption assumes authoring order is meaningful.
- Do not rely on comments, metadata, BOMs, or UTF-8-only punctuation for vanilla compatibility.
- Use empty-text cue lines cautiously. They are the closest thing to a manual clear cue, but old tools and custom engines may handle visually empty strings differently.
- For translations, the SVE is usually the file that changes; the MVE can remain shared if the spoken audio and video timing are unchanged.

## Implementation Notes

A robust parser can be stricter than the original engine while still accepting shipped files:

1. Read the file as target-language text, preserving byte-compatible game characters.
2. Normalize CRLF/LF line endings.
3. Ignore a UTF-8 BOM only if the target toolchain intentionally supports UTF-8.
4. For each non-empty line, find the first colon.
5. Parse the left side as unsigned decimal for authoring validation.
6. Store the right side unchanged as subtitle text.
7. Warn on out-of-order frames, duplicate frames, negative frames, and long lines.
8. During playback, advance through cues while `cue.frame <= displayed_frame`.

For a web-based MVE player, use the decoder's displayed-frame counter as the authoritative SVE clock. Do not schedule subtitles only with `setTimeout`; that will drift if decoding, throttling, seeking, or dropped frames differ from the original playback cadence.

## Source Code Map

| Source | Relevant behavior |
|---|---|
| Fallout 2 CE `game_movie.cc` | Movie name table, localized MVE lookup, SVE filename construction, subtitle preference check, subtitle palette selection, font/color setup. |
| Fallout 2 CE `movie.cc` | SVE line parsing, linked-list cue storage, frame-count-based subtitle consumption, subtitle band clearing and drawing. |
| Fallout 2 CE `endgame.cc` | Ending slideshow subtitles, useful mostly as a contrast because those files are `.txt` and use different timing rules. |
| Vault-Tec Labs / TeamX note | Basic public description of SVE as `frame:text` subtitle files for Interplay videos. |

## References

- [SVE File Format - Vault-Tec Labs](https://falloutmods.fandom.com/wiki/SVE_File_Format)
- [Fallout 2 CE game_movie.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/game_movie.cc)
- [Fallout 2 CE movie.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/movie.cc)
- [Fallout 2 CE endgame.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/endgame.cc)
- [MVE File Format](mve.html)
- [MSG File Format](msg.html)
- [LIP File Format](lip.html)

## Changelog

2026-05-07 - Added dedicated SVE documentation covering syntax, lookup paths, parser behavior, timing, rendering, palette selection, and relationships to MVE, MSG, LIP, endgame subtitles, and credits text.
