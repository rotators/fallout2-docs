---
title: Credits and Quotes Text
output: credits.html
description: credits.txt and quotes.txt syntax, formatting markers, font selection, localized lookup, scrolling behavior, and custom endgame credits.
toc: auto
---

# Credits and Quotes Text

`credits.txt` and `quotes.txt` use the same line-based scrolling-text reader. A single optional character at the beginning of each line selects its style or makes it a comment. These files contain no record ids, timing fields, section headers, or declared line count.

This reference follows [fallout2-ce/fallout2-ce](https://github.com/fallout2-ce/fallout2-ce), revision `0290b2c98d538902235339b406291a426529fda0`, inspected on 2026-09-05. Fork-specific configuration and screen-size behavior are identified below. Findings are source-derived; examples have not been tested with game assets or in-game playback.

## Location and Activation

| Entry point | Requested file | Style |
|---|---|---|
| Main-menu Credits | `credits.txt` | Normal |
| Main-menu Quotes | `quotes.txt` | Reversed title/name styles |
| Final credits sequence | `credits.txt` by default; configurable in this fork | Normal |

`creditsOpen` passes the requested filename to `_message_make_path`, which prepends `text\<language>\`. Thus English credits normally reside at `text\english\credits.txt`, not `text\english\game\credits.txt` or `text\english\cuts\credits.txt`. Quotes use `text\english\quotes.txt`. Supply equivalent files for each supported language: this path builder does not retry English if a localized text file is absent.

Files are opened through the resource filesystem, so [DAT and loose-file precedence](dat.html) applies. A conventional English loose override is `DATA\text\english\credits.txt`. A filename supplied by configuration is also language-relative: `mod\credits.txt` resolves to `text\<language>\mod\credits.txt`. Do not supply an already-prefixed `text\english\...` path and expect the prefix to be recognized or removed.

In the inspected main-menu handler, selecting Credits while either Shift key is held selects Quotes instead. The caller supplies reversed styling; there is no directive inside the text that switches the whole file into quotes mode. Renaming a file does not itself change its styling.

The file is opened anew on each invocation and closed afterward. Missing text makes the routine return without a scrolling window or an error dialog. The routine loads `color.pal` before attempting the text open, so this failure is not strictly a side-effect-free operation. Credits do not contain saved progress or resume positions.

## Line Syntax

```text
; This comment is not displayed.
@Development
Example Developer
Example Artist

@Special Thanks
#Community contributors
Everyone who tested the mod
```

| First byte | Meaning | Text passed to the renderer |
|---|---|---|
| `;` | Comment | Entire read chunk is skipped. |
| `@` | Title | The leading `@` is removed. |
| `#` | Gray name-style line | The leading `#` is removed. |
| Anything else | Ordinary name-style line | The line is retained unchanged. |

Markers are recognized only at byte zero. There is no leading-whitespace trim: ` @Heading` is ordinary text containing an at sign, and ` ;comment` is displayed rather than ignored. A semicolon in the middle of a line is literal text. `#` is not a comment marker in this format.

Only one prefix is consumed. `@@Heading` renders `@Heading` in title style; `@#Heading` renders `#Heading` in title style, not gray. Styles do not persist across records: a plain line after a title uses the normal name style again. There is no quoting, backslash escape language, inline markup, or explicit alignment command.

A blank physical line is not discarded. It becomes a spacing row, using the same vertical allocation as a text row. Comments contribute no such row. Use blank lines rather than comment-only lines to separate groups.

## Buffer and Encoding Rules

The parser calls `fileReadString` with a 256-byte buffer and copies its result into the caller's 260-byte buffer. Each read can contain at most 255 bytes before NUL. This is a read-chunk limit, not a safe continuation mechanism. Keep a complete physical line, including its marker and newline, within that limit.

An oversized physical line is processed as multiple records. Each continuation chunk is independently checked for a leading marker and independently styled. Even a long semicolon comment can leak its remaining fragments into the visible credits. There is no whole-file size limit or row-count cap in this reader; it streams records instead of loading the entire text at once.

The credits parser does not strip CR/LF from the returned string. The underlying text I/O can normalize line endings, but bytes it leaves are passed to the font routines. The AAF width routine treats each non-NUL byte other than space as a glyph index, including LF, and adds letter spacing. A trailing newline can therefore contribute to measured width even when its glyph is visually empty. It is not a second layout instruction: the credits reader itself supplies the row boundaries.

A final line without a newline is accepted; unlike the death-table parser, this reader does not rely on newline trimming to terminate a filename. Exact rendering can nevertheless differ because the newline byte is absent. Embedded NUL ends the copied C string and should not appear in authored text.

Use the byte encoding supported by the installed fonts and localization. There is no Unicode decoding or BOM removal in this parser. A UTF-8 BOM before `@` or `;` prevents the first-byte marker test from matching, and UTF-8 multibyte characters are treated as separate glyph bytes by this font backend.

## Fonts and Colors

| Row kind | Normal style | Reversed style |
|---|---|---|
| `@` title | Font 103, `COLOR_DULL_BROWN` | Font 104, `COLOR_DARK_YELLOW_3` |
| Plain name | Font 104, `COLOR_DARK_YELLOW_3` | Font 103, `COLOR_DULL_BROWN` |
| `#` gray name | Font 104, `COLOR_GREY` | Font 103, `COLOR_GREY` |

Font ids 103 and 104 select [AAF](aaf.html) fonts 3 and 4. In this fork, the font loader first tries `text\<language>\fontN.aaf`, then English if appropriate, and finally root-level `fontN.aaf`. This font fallback is separate from credits text lookup: a font fallback does not supply missing translated credits.

The color constants request RGB values `0x6B5A4A` (dull brown), `0x947B29` (dark yellow), and `0x8C8C8C` (gray) through the RGB555 palette lookup. They are not literal palette indices or exact output RGB guarantees. The AAF renderer uses palette blend tables for glyph shading. The credits routine loads the common `color.pal`; it does not look for a same-basename palette beside the text.

The file cannot name a font, provide RGB values, choose a background, or adjust the style table. Such changes require a caller/runtime change or different underlying font/palette assets. Replacing fonts affects both width acceptance and vertical spacing, not just appearance.

## Layout and Scrolling

The inspected fork creates a window using `screenGetWidth()` and `screenGetHeight()`, rather than forcing a 640 by 480 credits viewport. The following dimensions refer to that logical window; this is not a claim about every original executable or display-scaling configuration.

Each accepted row is horizontally centered using its measured font width. There is no word wrapping, clipping fallback, shrinking, or pagination. If `stringWidth >= windowWidth`, the row is skipped entirely and consumes no vertical spacing. A line exactly equal to the window width is also rejected. A long translated credit can consequently disappear at one resolution and appear at a wider one.

Vertical allocation is shared by all row types:

```text
rowHeight = max(titleFontLineHeight, nameFontLineHeight)
fontLineHeight = fontMaxHeight + fontLineSpacing
```

Every accepted row, including a blank one, scrolls into view over `rowHeight` one-pixel steps. Choosing `@` does not add an automatic gap before or after the title. The smaller font still gets the common row height.

The target delay is 38 milliseconds per one-pixel step, approximately 26.3 pixels per second. This is a runtime constant, not a text setting. After EOF the renderer performs another full window-height of scrolling to move the final rows off the screen. An empty or comment-only file still reaches this final clearing loop rather than immediately returning.

A nominal timing estimate is:

```text
duration_seconds ~= 0.038 * (acceptedRows * rowHeight + windowHeight)
```

For 100 accepted rows at a 16-pixel common height in a 480-pixel-high window, this is about 79.0 seconds, before fades and scheduling effects. This is an estimate, not a guaranteed playback duration: rendering, delay compensation, the frame limiter, and user interruption affect it. Higher window height increases the trailing scroll time, while wider windows can admit more rows.

Any input event reported by `inputGetInput()` ends scrolling, both while adding rows and during the final clearing phase. There is no credits-specific pause, seek, speed-control, or per-line timing command.

## Background and Audio

The C++ entry point is `creditsOpen(filePath, backgroundFid, useReversedStyle)`. Its background argument is a packed FID, unlike the plain interface art index in an `endgame.txt` row. The main-menu credits, quotes, and inspected endgame caller all pass -1, producing a black background.

For a nonempty background FID, the renderer attempts to load the first frame and centers it at its native dimensions. It neither scales nor clips oversized background art in this copy path; a custom caller must ensure the image fits the window. If the art cannot be loaded, the black background remains. No background filename or FID can be set inside the credits text itself.

Text is drawn to a separate scrolling buffer and composited over the static background with zero-valued pixels transparent. Background art must work with the common palette; the routine does not load an art-specific palette as the ending slideshow does.

The text has no music, narration, sound-effect, or subtitle bindings. `creditsOpen` services sound but does not select a soundtrack. Music belongs to its caller: the main-menu route and final sequence can therefore play different music while showing the same file. On the normal completed path the routine fades out, destroys the credits window, reenables palette cycling, and restores the previous font.

## Custom Final Credits

The inspected fork reads content configuration from `config\game.cfg` and then `config\game#patch.cfg` through the resource filesystem. A targeted patch can select a separate ending credits file:

```ini
[movies]
endgame_credits_file=modcredits.txt
```

Provide `text\<language>\modcredits.txt` for every supported language. This changes the final sequence's credits, not the main-menu Credits or Quotes filenames. Set the value empty to skip that credits screen explicitly. A missing nonempty filename is not the same configuration choice, even though neither produces visible scrolling text.

The `endgame_movie` script operation invokes the final sequence, including these configured credits; it is not a direct call accepting a credits filename. The related `endgame_play_after_slideshow` setting controls automatic final-sequence playback, while an explicit script call can still run it. See [Ending Configuration](endings.html) for pre-credits movies, music settings, and the continue-playing prompt. Do not call that sequence merely to preview a text file without accounting for its other effects.

## Related Formats

| Resource | Difference |
|---|---|
| [MVE](mve.html), including `credits.mve` | Movie container; not this scrolling text reader. |
| [SVE](sve.html), including `credits.SVE` | Frame-timed movie subtitles; does not format `credits.txt`. |
| [Ending narrator text](endings.html) | Slideshow and death text have separate colon-handling and timing rules. |
| [MSG](msg.html) | Brace-delimited message ids and strings; no MSG lookup is performed for credits lines. |
| [AAF](aaf.html) and [PAL](pal.html) | Supply actual glyph metrics, shading, and colors. |

## Authoring Checks

1. Keep markers at the first byte, and use `;` only for full-line comments. Test normal and reversed styles separately.
2. Keep physical lines within the read buffer limit, including newline bytes. Split long sentences into deliberate rows rather than relying on the reader's chunks.
3. Measure the full parser-returned string with the selected font, including retained control bytes. Require width strictly less than the narrowest target window.
4. Use blank lines for spacing, test non-ASCII credits with the actual font set, and avoid BOMs or embedded NULs.
5. Confirm localized paths, packed/loose override precedence, and the distinction between main-menu and custom final credits.
6. Test early input dismissal and a complete scroll to EOF. Do not infer successful display merely from a file opening without error: overwide rows are silently omitted.

For a new reader, keep source-record parsing separate from font measurement and presentation. A friendly authoring tool can warn about overlong and overwide records instead of silently matching the engine. An exact compatibility preview must additionally reproduce first-byte marker handling, retained newline bytes, skipped rows, fixed row height, and caller-selected styling.

## Source References

- [credits.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/credits.cc): `creditsFileParseNextLine`, style tables, background handling, window sizing, and scrolling loops.
- [message.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/message.cc): `_message_make_path` language-relative lookup.
- [main.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/main.cc) and [mainmenu.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/mainmenu.cc): Credits/Quotes callers and Shift selection.
- [font_manager.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/font_manager.cc): AAF lookup, font-id mapping, byte-based width calculation, and drawing.
- [color.h](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/color.h) and [delay.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/delay.cc): color definitions and delay implementation.
- [endgame.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/endgame.cc) and [interpreter_extra.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/interpreter_extra.cc): configured final credits and `endgame_movie` scripting path.
- [content_config.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/content_config.cc): content configuration and patch load order.
