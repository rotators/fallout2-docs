---
title: MVE File Format
output: mve.html
description: Interplay MVE movie container used by Fallout, including lookup behavior, headers, chunks, opcodes, audio, palette, video block modes, subtitles, and savegame relationship.
toc: auto
---

# MVE File Format

MVE is Interplay's full-motion video container used by Fallout, Fallout 2, and several other Interplay-era games. In Fallout it stores intro, ending, logo, credits, and cutscene movies below `art\cuts`. The companion subtitle file, when present, is a plain text [SVE](sve.html) file below `text\<language>\cuts`.

This page describes the Interplay MVE format used by Fallout's `.mve` movies. Do not confuse it with unrelated files that happen to use an `.mve` extension, such as the Wing Commander III MVE format.

For the separate FRM-based slideshow and death narration tables, see [Ending Configuration](endings.html). Their narrator text is not frame-timed SVE data.

## Format Properties

| Property | Description |
|---|---|
| File type | Binary movie container with custom video and optional audio streams. |
| Typical Fallout path | `art\cuts\*.mve`, or localized `art\<language>\cuts\*.mve`. |
| Header signature | `Interplay MVE File`, followed by DOS EOF byte `0x1A` and NUL. |
| Byte order | Container fields are little-endian, unlike many Fallout game-data formats. |
| Video | Interplay video. Fallout-era content is usually 8-bit paletted video; later Interplay variants can use 16-bit RGB555. |
| Audio | PCM or Interplay DPCM, depending on audio init flags. |
| Palette | 8-bit movies carry VGA-style 6-bit RGB palette updates inside the movie stream. |
| Compression | The MVE file itself is not a DAT archive or gzip file. It may be stored inside [master.dat](dat.html) or another DAT archive. |

## Fallout Lookup

Fallout 2 CE keeps a fixed movie-name table for the shipped Fallout 2 movies:

| Index | File | Typical use |
|---|---|---|
| `0` | `iplogo.mve` | Interplay logo. |
| `1` | `intro.mve` | Opening narration. |
| `2` | `elder.mve` | Elder cutscene. |
| `3` | `vsuit.mve` | Vault suit/character-related cutscene. |
| `4` | `afailed.mve` | Arroyo failed ending/cutscene. |
| `5` | `adestroy.mve` | Arroyo destroyed ending/cutscene. |
| `6` | `car.mve` | Car movie. |
| `7` | `cartucci.mve` | Cathedral/Tucci-style special movie entry. |
| `8` | `timeout.mve` | Time-out ending. |
| `9` | `tanker.mve` | Tanker cutscene. |
| `10` | `enclave.mve` | Enclave cutscene. |
| `11` | `derrick.mve` | Derrick/oil-related cutscene. |
| `12` | `artimer1.mve` | Arroyo timer movie 1. |
| `13` | `artimer2.mve` | Arroyo timer movie 2. |
| `14` | `artimer3.mve` | Arroyo timer movie 3. |
| `15` | `artimer4.mve` | Arroyo timer movie 4. |
| `16` | `credits.mve` | Credits. |

The Fallout 2 CE lookup order is:

1. If the configured language is not English, try `art\<language>\cuts\<movie>.mve`.
2. If that file is missing, fall back to `art\cuts\<movie>.mve`.
3. If subtitles are enabled, build `text\<language>\cuts\<movie>.SVE` from the movie filename.

Subtitles use special [palette](pal.html) files for some movies. Fallout 2 CE uses `art\cuts\introsub.pal`, `eldersub.pal`, `artmrsub.pal`, `crdtssub.pal`, or the default `art\cuts\subtitle.pal` depending on the movie index.

## File Header

An Interplay MVE begins with a fixed 26-byte header:

| Offset | Size | Field | Description |
|---|---|---|---|
| `0x0000` | `20` | Signature | ASCII `Interplay MVE File`, byte `0x1A`, and byte `0x00`. |
| `0x0014` | `2` | Magic word 1 | Little-endian `0x001A`. |
| `0x0016` | `2` | Magic word 2 | Little-endian `0x0100`. |
| `0x0018` | `2` | Magic word 3 | Little-endian `0x1133`. |

ScummVM asserts the three magic words when loading an MVE. FFmpeg's demuxer searches for the signature in probe/read-header code, then positions the stream at the first chunk.

## Container Layout

After the header, the file is a sequence of chunks. Each chunk contains one or more opcodes.

```text
MVE file
    header
    chunk
        opcode
        opcode
        ...
    chunk
        opcode
        ...
```

### Chunk Header

| Offset | Size | Field | Description |
|---|---|---|---|
| `0x00` | `2` | `chunk_size` | Little-endian byte length of chunk payload, not including this 4-byte chunk header. |
| `0x02` | `2` | `chunk_type` | Little-endian chunk type. |

### Opcode Header

| Offset | Size | Field | Description |
|---|---|---|---|
| `0x00` | `2` | `opcode_size` | Little-endian byte length of opcode payload, not including this 4-byte opcode header. |
| `0x02` | `1` | `opcode_type` | Operation id, normally `0x00..0x15`. |
| `0x03` | `1` | `opcode_version` | Version/subtype byte for that opcode. |

Some source code displays opcodes as a big-endian 16-bit pair such as `0x0502`, which means type `0x05`, version `0x02`. A binary parser should still read the type and version as separate bytes.

## Chunk Types

| Type | Name | Description |
|---|---|---|
| `0x0000` | Init audio | Audio stream setup, usually contains opcode `0x03`. |
| `0x0001` | Audio only | Audio prebuffer or audio-only chunk. |
| `0x0002` | Init video | Video mode/buffer/palette setup. |
| `0x0003` | Video | Main movie chunk, commonly contains audio, decoding maps, video data, and send-buffer opcode. |
| `0x0004` | Shutdown | End-of-playback/shutdown chunk. |
| `0x0005` | End | Terminal end chunk. Usually no useful payload. |

## Stream Opcodes

| Opcode | Name | Payload and behavior |
|---|---|---|
| `0x00` | End of stream | No payload. Stops playback. |
| `0x01` | End of chunk | No payload in normal files. Decoder moves to the next chunk. |
| `0x02` | Create timer | Version 0, size 6: `uint32 rate`, `uint16 subdivision`. Frame interval is `rate * subdivision` microseconds. |
| `0x03` | Init audio buffers | Version 0 size 8 or version 1 size 10. Contains unknown word, flags, sample rate, and buffer length. |
| `0x04` | Start/stop audio | Playback-control opcode. Common demuxers skip the payload. |
| `0x05` | Init video buffers | Version 0/1/2. Width and height are stored in 8-pixel blocks. Version 2 can indicate 16-bit video. |
| `0x06` | Video data format 6 | Older 8-bit video frame format. FFmpeg treats its decoding map as embedded at the start of the video payload. |
| `0x07` | Send buffer | Marks the current decoded frame ready for display. ScummVM also reads palette start/count from version 1 payload. |
| `0x08` | Audio frame | Audio payload: sequence index, stream mask, stream length, and audio data. |
| `0x09` | Silence frame | Silent audio payload: sequence index, stream mask, and stream length. |
| `0x0A` | Init video mode | Usually contains width, height, and flags. Fallout/ScummVM playback code reads and ignores these values after setup. |
| `0x0B` | Create gradient | Documented/recognized by demuxers, but often skipped for normal playback. |
| `0x0C` | Set palette | Palette start, palette count, then 3 bytes per entry in 6-bit VGA RGB format. |
| `0x0D` | Set compressed palette | Compressed palette update. FFmpeg recognizes and skips it; older specs describe 32 groups of 8 palette entries selected by bit masks. |
| `0x0E` | Set skip map | Stores the skip map used by video format `0x10`. |
| `0x0F` | Set decoding map | Stores the block decoding map used by video formats `0x10` and `0x11`. |
| `0x10` | Video data format 10 | 8-bit video frame format using separate skip map and decoding map opcodes. |
| `0x11` | Video data format 11 | Interplay block-coded video. FFmpeg supports 8-bit paletted and 16-bit RGB555 variants. |
| `0x12..0x15` | Unknown/documented opcodes | Recognized in old specs and FFmpeg as documented-but-unknown values. Normal decoders skip them or treat unrecognized payloads conservatively. |

## Timing

The timer opcode stores a 32-bit rate and a 16-bit subdivision. The effective frame interval is:

```text
frame_interval_us = timer_rate * timer_subdivision
fps = 1000000 / frame_interval_us
```

Older MVE notes mention a common timer rate of `8341` and subdivision `8`, giving about `14.99` frames per second. Do not hardcode that value: read the timer opcode because files can differ.

## Audio

Audio is optional. FFmpeg checks whether the first chunk after video initialization is another video chunk; if so, it treats the file as silent. Otherwise it expects an audio initialization chunk.

### Audio Init Flags

| Bit | Meaning |
|---|---|
| `0` | `0` mono, `1` stereo. |
| `1` | `0` 8-bit samples, `1` 16-bit samples. |
| `2` | Version 1 only: `0` uncompressed PCM, `1` Interplay DPCM. |

Uncompressed audio is PCM: 8-bit unsigned PCM or 16-bit little-endian PCM. Compressed audio is Interplay DPCM.

### Audio Frame Payload

| Offset | Size | Field | Description |
|---|---|---|---|
| `0x00` | `2` | Sequence index | Sequential audio frame number. |
| `0x02` | `2` | Stream mask | Bitmask selecting one or more of up to 16 audio tracks. |
| `0x04` | `2` | Stream length | Length of the following audio data for opcode `0x08`; silence duration for opcode `0x09`. |
| `0x06` | variable | Audio data | Present only for opcode `0x08`. |

ScummVM exposes a selectable audio track index and queues only audio frames whose stream mask includes that track. This matters for multi-language MVE files, even though Fallout usually relies on localized files and a single active audio stream.

### Interplay DPCM

Interplay DPCM uses a 256-entry signed delta table. Each chunk begins with one signed 16-bit little-endian predictor for mono audio, or two predictors for stereo audio. The remaining bytes are delta-table indexes. For each byte, the decoder adds the selected delta to the current predictor, clamps to signed 16-bit range, and outputs the new sample. Stereo samples are interleaved left/right.

## Palette

8-bit MVE video uses palette indexes. Palette opcode `0x0C` carries a start index, count, and RGB triplets. Each RGB component is a VGA-style 6-bit value from `0` to `63`. Decoders typically expand to 8-bit display color with a shift and low-bit fill, for example `(value << 2) | (value >> 4)`.

Palette changes are frame-adjacent state. A demuxer should attach palette updates to the next decoded video packet or apply them before showing the corresponding frame.

## Video Setup

Video buffer initialization stores width and height in 8-pixel blocks. For example, width block count `80` and height block count `60` describe a `640x480` movie. FFmpeg multiplies both fields by `8` when setting stream dimensions.

Version 2 of the video-buffer opcode can select 16-bit video. In FFmpeg, 8-bit output is `PAL8` and 16-bit output is `RGB555`. Fallout's normal cutscene path is paletted, and the movie window is a 640 by 480 indexed-color window.

## Video Data Formats

| Format | Opcode | Maps | Description |
|---|---|---|---|
| `0x06` | `0x06` | Embedded decoding map | Older block copy format. FFmpeg requires 8-bit video and rejects external skip/decoding maps for this format. |
| `0x10` | `0x10` | Separate skip map and decoding map | 8-bit block copy format using opcode `0x0E` skip map and `0x0F` decoding map. |
| `0x11` | `0x11` | Separate decoding map | Interplay block codec. 8-bit and 16-bit variants are supported by FFmpeg. |

Each decoded frame is built from 8 by 8 blocks. The codec keeps previous frames because many block modes copy from the previous frame, the second previous frame, or a previously decoded block in the current frame.

## Format 0x11 Block Modes

For video format `0x11`, the decoding map is a packed stream of 4-bit block opcodes, one per 8x8 block. FFmpeg's decoder names block modes `0x0` through `0xF`:

| Block opcode | 8-bit meaning |
|---|---|
| `0x0` | Copy 8x8 block from previous frame at the same position. |
| `0x1` | Copy 8x8 block from the second previous frame at the same position. |
| `0x2` | Copy from the second previous frame using a one-byte motion vector. |
| `0x3` | Copy from the current frame using an up/left motion vector. |
| `0x4` | Copy from the previous frame using a small one-byte motion vector. |
| `0x5` | Copy from the previous frame using two signed motion-vector bytes. |
| `0x6` | Mystery/rare mode in the 8-bit decoder. FFmpeg logs it as suspicious but does not consume extra data. |
| `0x7` | 2-color encoding for either individual pixels or 2x2 blocks, depending on color ordering. |
| `0x8` | 2-color encoding split by quadrants or by half-frame regions inside the 8x8 block. |
| `0x9` | 4-color encoding with pixel, 2x2, 2x1, or 1x2 grouping depending on color ordering. |
| `0xA` | 4-color encoding split by quadrants or half-block regions. |
| `0xB` | Raw 64-color block: one byte per pixel. |
| `0xC` | 16-color block: one color per 2x2 cell. |
| `0xD` | 4-color block: one color per 4x4 quadrant-style area. |
| `0xE` | Solid-color block. |
| `0xF` | Dithered two-color block. |

The 16-bit variant uses the same broad block-mode table but reads RGB555 words instead of palette indexes for color-coded modes. FFmpeg maps 16-bit MVE frames to `RGB555`.

## Frame Assembly

A demuxer should not pass raw opcode payloads independently to a video decoder. FFmpeg combines the frame format byte, send-buffer flag, video data, decoding map, skip map, and palette side data into a single video packet. ScummVM keeps the latest skip/decoding maps in decoder state and applies them when a video opcode arrives.

The send-buffer opcode controls when a decoded frame is actually displayed. A video-data opcode may update decode buffers, while the send opcode marks the frame complete for presentation and increments the visible frame count.

## Subtitles

Fallout subtitles are not embedded in the MVE stream. The movie subsystem builds a subtitle filename from the movie filename:

```text
art\cuts\intro.mve
text\english\cuts\intro.SVE
```

The [SVE](sve.html) parser reads lines of the form `frame:text`. When playback reaches or passes that frame number, the text replaces the previous subtitle. Fallout 2 CE renders subtitles centered below the movie image, using the selected subtitle palette and font.

A same-basename [CFG](cfg.html) sidecar can also exist beside an MVE to define movie palette fade effects. For example, `art\cuts\intro.mve` can be accompanied by `art\cuts\intro.cfg`.

## Savegame Relationship

[SAVE.DAT](savegame.html) handler 21 stores `MOVIE_COUNT` bytes of movie-seen flags. The movie files themselves are not copied into save slots. A save only records whether each movie index in the engine's fixed table has already been seen.

## Implementation Notes

- MVE container numbers are little-endian. Do not reuse the big-endian helpers used by PRO, MAP, INT, or SAVE.DAT without swapping.
- Chunk sizes exclude the 4-byte chunk header; opcode sizes exclude the 4-byte opcode header.
- Video dimensions are stored in 8-pixel blocks during video-buffer initialization.
- Palette entries are 6-bit VGA RGB values, not full 8-bit RGB values.
- Audio frames can be masked for up to 16 streams. A player should choose a stream and ignore frames whose mask does not include it.
- DPCM audio needs predictor state within each compressed frame and the Interplay DPCM delta table.
- Video decoding needs previous-frame buffers; block-copy modes cannot be decoded statelessly.
- Unknown opcodes `0x12..0x15` may appear in some Interplay movies. A robust player can skip documented unknown payloads, but should reject truly unknown opcode types unless it can safely skip by size.
- Fallout subtitle timing uses decoded/displayed movie frame counts, not wall-clock timestamps.

## Source Code Map

| Source | What it confirms |
|---|---|
| FFmpeg `libavformat/ipmovie.c` | Header probing, chunk types, opcode types, timer/audio/video/palette demuxing, packet assembly. |
| FFmpeg `libavcodec/interplayvideo.c` | Video frame formats `0x06`, `0x10`, `0x11`, block opcodes, previous-frame use, 8-bit and 16-bit output. |
| ScummVM `video/mve_decoder.cpp` | Practical player structure, signature/magic-word checks, selectable audio track, Fallout-like format 6/10 handling. |
| Fallout 2 CE `game_movie.cc` | Fallout movie filename table, localized movie lookup, subtitle filename construction, subtitle palette selection, movie-seen save flags. |
| Fallout 2 CE `movie.cc` | Movie playback integration, subtitle loading/rendering, SVE line parsing, frame-count-based subtitle display. |

## References

- [Interplay MVE - MultimediaWiki](https://wiki.multimedia.cx/index.php/Interplay_MVE)
- [Interplay DPCM - MultimediaWiki](https://wiki.multimedia.cx/index.php/Interplay_DPCM)
- [FFmpeg Interplay MVE demuxer](https://github.com/FFmpeg/FFmpeg/blob/master/libavformat/ipmovie.c)
- [FFmpeg Interplay video decoder](https://github.com/FFmpeg/FFmpeg/blob/master/libavcodec/interplayvideo.c)
- [ScummVM MVE decoder](https://github.com/scummvm/scummvm/blob/master/video/mve_decoder.cpp)
- [Fallout 2 CE game_movie.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/game_movie.cc)
- [Fallout 2 CE movie.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/movie.cc)
- [MVE File Format - Vault-Tec Labs](https://falloutmods.fandom.com/wiki/MVE_File_Format)
- [SVE File Format](sve.html)
- [SVE File Format - Vault-Tec Labs](https://falloutmods.fandom.com/wiki/SVE_File_Format)

## History

2026-05-07 - Added dedicated MVE documentation covering Fallout lookup behavior, container/chunk/opcode structure, audio, palette, video frame formats, block modes, subtitles, and savegame relationship.
