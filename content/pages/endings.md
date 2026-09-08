---
title: Ending Configuration
output: endings.html
description: endgame.txt and enddeath.txt layouts, ending selection, narrator subtitles, resource dependencies, and Fallout 2 CE fork behavior.
toc: auto
---

# Ending Configuration

`data\endgame.txt` defines the ending slideshow. `data\enddeath.txt` defines candidate death narrations. These are positional text tables, not INI files, MSG lists, or movie containers. A slideshow combines interface FRM art, a palette, speech, and optional text; it is not an MVE movie.

This reference follows [fallout2-ce/fallout2-ce](https://github.com/fallout2-ce/fallout2-ce), revision `0290b2c98d538902235339b406291a426529fda0`, inspected on 2026-09-05. Loader quirks and selection defects below describe that source snapshot, not a promise that every original executable or sfall build behaves identically. Examples are constructed, not extracted game data; no in-game playback or shipped-data corpus was tested for this reference.

## Files and Loading

| Resource | Purpose |
|---|---|
| `data\endgame.txt` | Ordered slideshow rows, each gated by a global-variable equality test. |
| `data\enddeath.txt` | Death narration candidates with conditions and weights. |
| `art\intrface\intrface.lst` | Resolves the slideshow art index and fixed death-screen art. |
| `art\intrface\<art-base>.pal` | Palette corresponding to a slideshow image. |
| Speech root plus `narrator\<voice>.ACM` | Narration; the normal speech root is `sound\speech\`. |
| `text\<language>\cuts\<voice>.txt` | Slideshow or death text, with different interpretation in each renderer. |

The table paths are virtual resource paths opened with `fileOpen`, so [DAT/loose-file precedence](dat.html) applies. The initial `data\` is part of the resource name: for a conventional loose override below the game's `DATA` root, the table is at `DATA\data\endgame.txt`, not directly at `DATA\endgame.txt`.

The slideshow table is loaded when slideshow initialization starts and freed afterward. Failure to open it aborts that slideshow. The death table is loaded during game initialization; failure propagates as an initialization error. An empty readable death table is different from a missing file. Restart after editing death definitions; their contents are not read from the save slot. Saved globals and world-map state supply the conditions evaluated against the current definitions.

## Shared Table Syntax

Each logical record occupies one short physical line. Separators are spaces, tabs, and commas. Repeated separators collapse, so an empty comma-delimited field is not a placeholder. There is no CSV quoting or escape syntax.

- Leading whitespace is skipped. A first non-whitespace `#` marks a comment; blank lines are skipped.
- Numeric fields use `atoi`: use signed decimal integers. Invalid numeric text can become zero instead of producing an error. Hexadecimal notation is not supported as such.
- Both readers use a 256-byte buffer. Keep a complete physical line below 255 bytes including its newline; long lines are read in fragments, not safely joined or rejected.
- Rows missing required tokens are skipped. Extra tokens beyond those consumed are ignored. There is no count header or explicit maximum row count in these loaders; storage grows dynamically.
- These are not general inline-comment parsers. In particular, a comment after a four-field slideshow row can become its optional direction token.
- Neither reader validates global ids, art ids, city ids, weights, or referenced resource existence at parse time.

Newline handling matters for the final filename token: newline is not one of the token delimiters, and the code removes only one trailing whitespace character after copying the name. Preserve ordinary short, newline-terminated rows. Do not assume arbitrary whitespace normalization is harmless for the death parser.

## Slideshow Rows

```text
# gvar  required_value  interface_art_index  voice_base  [direction]
100     2               300                  demo01      1
100     3               301                  demo02      1
```

The ids and filenames above are illustrative. Verify all of them against the target mod before use.

| Position | Field | Meaning |
|---|---|---|
| 1 | `gvar` | Numeric global-variable index, not a GAM variable name or map variable. |
| 2 | `required_value` | Show the slide only when the current global equals this value. |
| 3 | `interface_art_index` | Zero-based index in `art\intrface\intrface.lst`, not a packed FID. |
| 4 | `voice_base` | Filename stem without `narrator\` or extension; also selects the subtitle basename. |
| 5 | `direction` | Optional; defaults to 1 only if no fifth token exists. Used by the special panning image. |

All matching rows play in file order. There is no first-match stop, deduplication, or implicit grouping by town. Two matching rows for the same global both play. To express a compound condition, scripts can compute a dedicated global first; this table has no expression language. Unlike the death table, the slideshow code does not implement `gvar=-1` as an unconditional sentinel.

The voice buffer holds 12 bytes and is filled with unchecked `strcpy`. With an explicit direction, the stem must fit in 11 bytes plus the terminator. If the filename is the last token, its newline is copied before trimming and also needs space. Prefer conventional stems of at most eight ASCII bytes; long names are a memory-safety problem, not merely truncated paths.

### Artwork and Panning

Ordinary slides copy a 640 by 480 pixel area from the first FRM frame with a fixed 640-byte source stride. Supply a correctly sized 640 by 480 indexed image; the renderer does not scale arbitrary art or play its FRM animation. Missing art makes that slide return without playing its narration.

Interface index 327 (`PanningDesertImage`, normally `dp.frm`) is special. It pans a 640 by 480 viewport across the image instead of using the static renderer. Setting direction on another image does not enable panning.

Use only `1` or `-1`: 1 increases the horizontal source offset; -1 decreases it. The loop tests for exact arrival and adds direction directly. Zero can stall it, and other values can miss the endpoint or access invalid pixels. The panning image must be wider than 640 and have enough height; a width of 640 causes division by zero, while very small extra widths can make the fade distance zero. Preserve the original panning dimensions and test replacement art rather than treating this as a general panorama renderer. Voice playback begins at a fade threshold, not immediately on loading the image.

The palette name comes from the art-list filename, with its extension replaced by `.pal` under `art\intrface`. This is independent of the narrator basename. The routine only constructs that path for an art stem of at most eight characters, and the palette-load result is not checked here. Supply the companion [PAL](pal.html); a missing palette is not a supported fallback to the correct colors.

## Slideshow Narration and Subtitles

The handler passes `narrator\<voice>` to `speechLoad`, whose lookup appends `.ACM` to the configured speech root. Do not put the extension in the table. These paths use the speech system, not music lookup or talking-head [LIP](lip.html) data.

With subtitles enabled, the slideshow reads `text\<language>\cuts\<voice>.txt`. There is no English fallback in this subtitle loader. Its syntax resembles [SVE](sve.html), but the prefix before the first colon is ignored:

```text
0:The settlement rebuilt its homes.
1:Travelers returned the following spring.
```

Changing `0` to `500` does not delay that line. Records remain in file order. Lines without a colon are ignored, and text after the first colon is retained, including subsequent colons and leading spaces. No comment syntax is implemented: a supposed comment containing a colon becomes a subtitle.

### Limits and Timing

The loader retains at most 50 colon-bearing records. It reads through a 256-byte buffer, strips LF, and does not reassemble oversized physical lines. A fragment without a colon is discarded; a fragment containing another colon can become another record. Keep each full line short, and avoid empty subtitle records or a file with no usable text.

Timing is proportional to `strlen`, meaning byte length rather than Unicode characters or rendered width. If speech loaded, its duration is distributed over the retained text. Without speech, the duration is 80 milliseconds per byte. For each record, its duration is truncated to integer milliseconds and added to a cumulative deadline. For example, 40 and 60 bytes divide a ten-second narration into approximately four and six seconds. Line breaks can therefore change pacing without supplying explicit time codes.

The static slide finishes on input, speech completion, or subtitle completion, whichever occurs first. It does not wait for both media streams. If neither loads, it uses a three-second display timer, apart from fades and short pauses. A successfully opened but empty subtitle file can make a slide finish almost immediately; with speech, zero total text length also reaches a division by zero in timing calculation. A missing subtitle file and an empty subtitle file are not equivalent.

Subtitles use font 101, wrap to 540 pixels, and are centered near the bottom of the 640 by 480 display with black backgrounds and white text. Localization must respect the game's font/encoding support; converting text to UTF-8 does not add Unicode rendering and can alter timing.

## Death Narration Rows

```text
# gvar threshold known_area unknown_area minimum_level weight voice_base
-1     0         -1         -1           1             10     demo01
-1     0         -1         -1           1             30     demo02
```

This is a two-row selection illustration, not a safe replacement for the original table: a special-case lookup requires row index 12, described below.

| Position | Field | Eligibility test |
|---|---|---|
| 1 | `gvar` | Global index, or -1 to disable the global condition. |
| 2 | `threshold` | When enabled, current global must be strictly less than this value. Equality rejects the row. |
| 3 | `known_area` | City/area index that must satisfy `wmAreaIsKnown`; -1 disables the test. |
| 4 | `unknown_area` | City/area index that must not satisfy `wmAreaIsKnown`; -1 disables the test. |
| 5 | `minimum_level` | Player level must be at least this number. |
| 6 | `weight` | Relative selection weight; the source calls this `percentage`, but the total need not be 100. |
| 7 | `voice_base` | Narrator filename stem. It does not select death-screen art. |

All enabled conditions must pass. For example, a threshold of 3 admits global values 0, 1, and 2, not 3. City indices are not MAP ids or world-map tile numbers. In this fork, `wmAreaIsKnown` requires a known/visited state (including its Fallout 1 compatibility state) and a visible/known city state. An invalid city returns false: it fails a required-known condition but can pass a required-unknown condition. Validate ids rather than relying on that asymmetry.

### Filename Termination Hazard

The death filename buffer holds 16 bytes. The inspected parser copies exactly `strlen(token)` bytes with `strncpy`, without bounding the length or explicitly appending a terminator. It then replaces a trailing whitespace character with NUL. A normal short last token containing its newline consequently becomes terminated, but a final line without a newline, a separator after the filename, or an extra token can leave it unterminated. Oversized tokens can overflow the buffer.

For compatibility, use short ASCII stems, end every record with a newline immediately after the filename, and do not append comments or trailing separators. A new parser should explicitly bound and terminate strings instead of reproducing this unsafe behavior.

## Death Selection Caveats

Eligibility is recomputed for each selection. The routine sums the weights of enabled rows and draws an integer from 0 through that sum, inclusive. It scans enabled rows until cumulative weight is at least the draw. Thus even before the indexing issue below, this is not exactly conventional weighted selection over `1..sum`: the first enabled row receives the zero outcome. With two always-enabled rows weighted 10 and 30, there are 41 draw values; the first gets 11 and the second 30. A zero-weight first eligible row can still win the zero draw.

The inspected source also increments `selectedEnding` only when passing an enabled row, then uses that counter as an index into the full row array. It does not store the actual selected loop index. Disabled rows before a winner can therefore redirect playback to another, even ineligible, row. For example, if file row 0 is disabled and row 1 is the first enabled row, a draw landing on row 1 leaves `selectedEnding=0` and plays row 0's voice. This is a source-derived defect, not an additional table feature.

Do not assume an editor's mathematically correct weighted preview reproduces this runtime. A compatibility implementation should make its target behavior explicit. Use positive, modest weights, prevent integer-sum overflow, and test changing eligibility as well as probability. Negative weights and an empty eligible set are not validated as errors. With rows present but none enabled, the source falls through to row 0 rather than choosing a documented generic fallback.

### Special Cases and Row Stability

For reason 0 (death), a nonzero `GVAR_MODOC_SHITTY_DEATH` forces array index 12, bypassing ordinary selection and eligibility. This is the thirteenth successfully parsed record, not the thirteenth physical line. The routine does not check that it exists. Removing or inserting earlier records, including accidentally malformed ones that are skipped, can change this special narration or make the access invalid. Preserve this position when modifying the stock table unless the runtime is changed too.

Reason 2 is timeout: the routine plays the timeout movie before continuing to narration selection. The table does not select that movie.

Initialization seeds a default name of `narrator\nar_5`. With no parsed records, selection returns early, and the filename getter substitutes `narrator\nar_4`. This does not make a missing table harmless: opening failure still aborts game initialization through its caller. Nor does it provide a fallback when a selected row names missing audio.

## Death Screen Text

Death narration uses a fixed interface image, index 309 (`DeathScene`, normally `death.frm`), and `art\intrface\death.pal`. There is no image field in `enddeath.txt`.

With subtitles enabled, the death screen opens `text\<language>\cuts\<voice>.TXT`. Unlike slideshow subtitles, it reads the whole file into a single text buffer, replaces newlines with spaces, and displays one wrapped block. It does not apply per-record timing or a 50-record limit.

Its preprocessing replaces each colon and the single character immediately before it with spaces. This crudely removes one-digit prefixes such as `0:`; it is not a numeric-prefix parser. A `12:` prefix leaves `1` behind, and a colon in ordinary prose can remove a preceding letter. Avoid literal prose colons and multi-digit cue labels for this path.

The caller provides a 512-byte buffer, but the reader does not enforce its capacity. Keep the entire decoded text below 512 bytes including the terminating NUL, not merely each line. This is another compatibility/safety constraint, not automatic truncation. Death text wraps at 560 pixels. Without loaded speech the screen uses a three-second timer; text length does not extend it. Input can dismiss it.

## Fork-Specific Final Sequence

For the separate scrolling text format, including `@`/`#` styles, localization, and line-width limits, see [Credits and Quotes](credits.html).

This fork has additional final-sequence settings in `config\game.cfg`, overlaid by `config\game#patch.cfg`, both read through the resource filesystem. These are separate from the two tables and should not be presented as vanilla Fallout 2 table fields.

| Section | Key | Default / effect |
|---|---|---|
| `[movies]` | `endgame_play_after_slideshow` | 1; controls automatic final sequence after the slideshow. |
| `[movies]` | `endgame_movie_male` | -1; optional zero-based pre-credits movie id for a non-female player. |
| `[movies]` | `endgame_movie_female` | -1; corresponding movie id for a female player. |
| `[movies]` | `endgame_credits_file` | `credits.txt`; empty skips the credits screen. |
| `[sound]` | `endgame_movie_music0` | `akiss`; initial final-sequence music. |
| `[sound]` | `endgame_movie_music1` | `10labone`; subsequent looping music. |

The movie ids must be within the runtime movie table; -1 disables the optional movie. Disabling the automatic final sequence does not disable an explicit `endgame_movie` script call. The final sequence also offers the continue-playing prompt via `misc.msg` id 30. None of these settings changes slideshow row conditions or turns narrator text into MVE subtitles.

## Validation and Testing

1. Resolve every global against the target [GAM](gam.html) ordering and every city against its [world-map configuration](worldmap_config.html). Do not reorder those dependencies casually.
2. Validate complete token counts, decimal integers, filename lengths, line lengths, and final newlines. Reject malformed input in authoring tools even where the engine accepts it.
3. Resolve art indices through [LST](lst.html), check [FRM](frm.html) dimensions, and provide the art-derived palette independently of the voice-derived audio/text.
4. Test all matching slides, no matching slides, and multiple matches. For deaths, test disabled rows before enabled ones, no eligible rows, threshold equality, area visibility changes, and the forced index-12 case.
5. Test subtitles on/off, speech disabled/missing, empty versus missing text, and every language. Do not feed oversized files to the runtime to test its unsafe buffers.
6. Preserve a test save. Current globals and area visibility drive the results; replacing table text does not update those values in an existing save.

At the cited revision, the fork provides `--dev-load-game=N` (one-based save slot), `--dev-endgame`, and `--dev-endgame-movie`. For example, pass `--dev-load-game=1 --dev-endgame` to request the ending after loading slot 1. The movie switch requests the separate final sequence; using both ending switches calls slideshow and final sequence directly. These are fork developer options, not portable original-game command-line switches, and they do not exercise death selection.

## Source References

All links below pin the inspected fork revision so later fixes do not silently change the meaning of the documented caveats.

- [endgame.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/endgame.cc): both row parsers, eligibility/selection, palettes, slideshow rendering, subtitle timing, and final sequence.
- [main.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/main.cc): death rendering, whole-file subtitle reader, colon handling, and developer switches.
- [game.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/game.cc): death-table initialization error propagation.
- [worldmap.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/worldmap.cc): `wmAreaIsKnown`.
- [random.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/random.cc): inclusive `randomBetween` bounds.
- [game_sound.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/game_sound.cc): speech filename resolution and loading.
- [art_defs.h](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/art_defs.h): fixed death and panning art indices.
- [content_config.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/content_config.cc) and [distributed game.cfg](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/files/ce.dat/config/game.cfg): fork-specific configuration paths and final-sequence keys.
