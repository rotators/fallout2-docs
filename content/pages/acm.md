---
title: ACM File Format
output: acm.html
description: Fallout and Fallout 2 ACM compressed audio file format, decoder notes, playback context, and browser player.
---

# ACM File Format

ACM is Interplay's compressed audio format. Fallout and Fallout 2 use it for music, speech, and sound effects. It is not related to Microsoft's Windows Audio Compression Manager despite sharing the same extension.

An ACM file contains a 14-byte header followed by a little-endian bitstream. Decoding produces signed 16-bit PCM samples. The common Fallout rate is `22050 Hz`, with mono and stereo files both appearing in Interplay-era games. Modern decoders should read the channel count and sample rate from the header, but Fallout's own playback paths also make contextual assumptions about how the sound is used.

## Browser ACM Player

The player below decodes ACM directly in the browser. The decoder mode controls how strict the parser is about Fallout-style standalone files, WAVC wrappers, and the Fallout 2 CE raw-block extension; the playback-channel mode controls how the decoded sample stream is grouped into audio frames. This matters because Fallout's playback context can override what the ACM header appears to say.

If narration or speech sounds too fast, try `Speech / narration mono`. That mode treats each decoded sample value as one mono frame instead of pairing values as stereo; merely downmixing a stereo interpretation to mono still keeps the wrong duration.

```raw-html
<div class="acm-player">
<div class="acm-player-controls">
    <div>
        <label for="acmFileInput">ACM file</label>
        <input id="acmFileInput" type="file" accept=".acm,.wavc,.wav,audio/*" />
    </div>
    <div>
        <label for="acmDecoderMode">Decoder mode</label>
        <select id="acmDecoderMode">
            <option value="auto">Auto / compatibility</option>
            <option value="strict">Fallout strict</option>
            <option value="ce">Fallout 2 CE extensions</option>
            <option value="wavc">WAVC wrapper allowed</option>
        </select>
    </div>
    <div>
        <label for="acmOutputMode">Playback channels</label>
        <select id="acmOutputMode">
            <option value="context">Auto / context</option>
            <option value="original">Use file header</option>
            <option value="mono">Mono stream</option>
            <option value="stereo">Stereo stream</option>
            <option value="downmix">Header, downmix to mono</option>
            <option value="speech">Speech / narration mono</option>
            <option value="music">Music stereo</option>
        </select>
    </div>
    <div>
        <label>&nbsp;</label>
        <button id="acmDecodeButton" type="button">Decode</button>
    </div>
</div>

<div class="acm-player-controls" style="margin-top: 10px;">
    <button id="acmPlayButton" type="button" disabled>Play</button>
    <button id="acmStopButton" type="button" disabled>Stop</button>
    <button id="acmDownloadWavButton" type="button" disabled>Download WAV</button>
    <button id="acmClearButton" type="button">Clear</button>
</div>

<canvas id="acmWaveform" width="1160" height="120"></canvas>
<div id="acmStatus">Choose an ACM file and decode it.</div>
<pre id="acmMetadata"></pre>
</div>
```

## Where Fallout Uses It

| Folder | Use | Notes |
| --- | --- | --- |
| `sound\sfx\` | Interface, combat, ambient, scenery, weapon, and item sound effects. | Often loaded through the sound effects cache. |
| `sound\music\` | Background music. | Map music names come from `maps.txt` or engine calls and are loaded as `.ACM`. |
| `sound\speech\` | Spoken dialogue. | Dialogue [MSG](msg.html) entries reference speech base names without the extension. |

ACM files may be loose in the data tree or stored in [DAT](dat.html) archives. The DAT layer is independent: a DAT2 entry may be stored compressed with zlib, and the entry payload may itself be an ACM compressed audio file.

## Relationship to MSG and LIP

Dialogue lines can reference an ACM file in the second field of an MSG entry:

```text
{104}{hak001}{Greetings, Chosen.}
```

The engine resolves the speech name through the speech sound path and appends `.ACM`. If a talking-head line has lip sync, the corresponding [LIP](lip.html) file stores mouth-shape timing; the ACM stores only audio. The two files must agree in duration closely enough for the animation to remain synchronized.

## Header

All multi-byte header fields are little-endian. The packed stream after the header is read least-significant bit first.

| Offset | Size | Field | Description |
| --- | --- | --- | --- |
| `0x00` | 4 | `signature` | Bytes `97 28 03 01`. Often written as FourCC/value `0x01032897` in little-endian order. |
| `0x04` | 4 | `sample_count` | Total number of decoded 16-bit sample values. For stereo, this counts interleaved left+right sample values, not just sample frames. |
| `0x08` | 2 | `channels` | Usually `1` or `2`. Some decoders warn that some ACMs can lie about this value, so callers should also know the intended playback context. |
| `0x0A` | 2 | `sample_rate` | Commonly `22050` Hz in Fallout data. |
| `0x0C` | 2 | `attributes` | Low 4 bits are `levels`; high 12 bits are `rows` / `subblocks`. |

```text
struct AcmHeader {
    uint32_t signature;     // 0x01032897, bytes 97 28 03 01
    uint32_t sample_count;  // decoded int16 sample values
    uint16_t channels;
    uint16_t sample_rate;
    uint16_t attributes;    // levels in low nibble, rows in high 12 bits
};
```

## Derived Values

The attributes word drives block shape and transform depth:

```text
levels = attributes & 0x000F
rows   = attributes >> 4
cols   = 1 << levels
block_sample_values = rows * cols
transform_state_values = levels == 0 ? 0 : (3 * cols / 2 - 2)
```

Older Fallout documentation calls `levels` `packAttrs` and `rows` `packAttrs2`. Both names describe the same header bits. A decoded block contains `rows * (1 << levels)` 16-bit sample values before channel grouping. Decoder implementations name and size the previous-block transform buffer differently; the formula above matches the classic Fallout/CE-style description.

Fallout 2 CE additionally processes the inverse transform in slices. It computes:

```text
block_rows_per_step = 2048 / cols - 2
if block_rows_per_step < 1:
    block_rows_per_step = 1
```

This limits how much previous-block state is needed while reconstructing the waveform.

## High-Level Decode Flow

1. Read and validate the 14-byte header.
2. Allocate a coefficient block of `rows * cols` integer values.
3. Allocate a wrap/previous-sample buffer if `levels != 0`.
4. For each encoded block, read a 20-bit scale header: 4-bit `pwr` and 16-bit `val`.
5. Build the amplitude lookup table around index zero using multiples of `val`.
6. For each column/subband, read a 5-bit filler code and decode coefficient values into the block.
7. If `levels != 0`, apply the inverse transform using the wrap buffer from previous blocks.
8. Output each reconstructed value shifted right by `levels` as signed 16-bit PCM.
9. Stop after `sample_count` sample values have been produced.

## Block Scale Header

Every encoded block starts with:

| Bits | Field | Description |
| --- | --- | --- |
| 4 | `pwr` | Amplitude table power. `count = 1 << pwr`. |
| 16 | `val` | Amplitude step value. |

The decoder fills the midpoint of a 65536-entry amplitude table so signed indexes can be used directly:

```text
mid[0] = 0
mid[1] = val
mid[2] = 2 * val
...
mid[count - 1] = (count - 1) * val

mid[-1] = -val
mid[-2] = -2 * val
...
mid[-count] = -count * val
```

## Filler Codes

After the scale header, the decoder reads one 5-bit filler code per column. The column count is `cols = 1 << levels`. Each filler writes values down that column across all rows.

| Code range | Name | Purpose |
| --- | --- | --- |
| `0` | zero | Fill the column with zero coefficients. |
| `1`, `2`, `25`, `28`, `30`, `31` | invalid / fail in strict decoders | Classic notes called these `Ret0` or failed fillers. Standard ACMs should not need them. |
| `3..16` | linear | Read the code number of bits per row and index the amplitude table directly. |
| `17` | `k13` | Sparse coding for values `0`, `-1`, `+1`, with repeated-zero shortcuts. |
| `18` | `k12` | Sparse coding for `0`, `-1`, `+1`. |
| `19` | `t15` | 5 bits encode three base-3 digits, mapping to `-1, 0, +1`. |
| `20` | `k24` | Sparse coding for `0`, `-2`, `-1`, `+1`, `+2`, with repeated-zero shortcuts. |
| `21` | `k23` | Sparse coding for `0` and nearby two-bit nonzero values. |
| `22` | `t27` | 7 bits encode three base-5 digits, mapping to `-2..+2`. |
| `23` | `k35` | Sparse coding for `0`, `-3`, `-2`, `-1`, `+1`, `+2`, `+3`. |
| `24` | `k34` | Sparse coding similar to `k35`, without the double-zero shortcut. |
| `26` | `k45` | Sparse coding for `0` and three-bit nonzero values `-4..-1`, `+1..+4`. |
| `27` | `k44` | Compact coding for `0` and three-bit nonzero values. |
| `29` | `t37` | 7 bits encode two base-11 digits, mapping to `-5..+5`. |

Fallout 2 CE also contains a handler for filler code `31` used by some Russian localizations. That handler reads raw 16-bit values for the entire block and acts as both filler and transformer. Standard Fallout ACM files and most general decoders treat code `31` as invalid.

## Inverse Transform

The filled block is not always PCM yet. If `levels` is nonzero, the decoder applies a recursive inverse transform over subbands. Implementations often call this step `juggle`, `untransform`, or unpacking.

The transform uses previous-block state, which is why a decoder must keep the wrap buffer between blocks. It also adds `1` to a subset of reconstructed values during processing before later output scaling. The final PCM sample value is the reconstructed integer shifted right by `levels` and written as signed little-endian 16-bit audio.

For seeking backward in ACM audio, CE resets the decoder and decodes forward until the requested byte position is reached. A robust standalone decoder can use the same strategy for arbitrary seeks, but it is not cheap because the bitstream has no independent seek table.

## PCM Output

| Property | Decoded output |
| --- | --- |
| Sample format | Signed 16-bit PCM |
| Byte order | Little-endian when written to WAV/raw files on PC |
| Sample count | `sample_count` 16-bit values from the header |
| Byte count | `sample_count * 2` |
| Frame count | `sample_count / channels`, if the channel count is trusted |
| Duration | `sample_count / (channels * sample_rate)` seconds |

Fallout 2 CE's audio wrapper asks the ACM decoder for `sample_count` and then doubles it to get the decoded byte size. This is another useful confirmation that the header count is a count of 16-bit sample values, not bytes.

## Channel Interpretation and Playback Context

The ACM header has a `channels` field, but Fallout-era playback code should not be treated as a pure "decode header, play header" pipeline. The compressed stream decodes to a flat sequence of signed 16-bit sample values. Grouping those values into frames is a playback decision: mono uses one sample value per frame, while stereo uses two interleaved sample values per frame.

This distinction matters because some narration or speech-like files can play at the wrong speed if a tool trusts a stereo interpretation where the game context expects mono. In that failure mode, every two decoded values are incorrectly paired into one stereo frame, so the frame count is halved and the audio plays roughly twice as fast. Downmixing that mistaken stereo stream to mono does not fix the duration; the stream must be reinterpreted as mono before frame construction.

| Interpretation | Frame construction | Duration formula | Typical use |
| --- | --- | --- | --- |
| Mono stream | Each decoded sample value is one frame. | `sample_count / sample_rate` | Speech, narration, many effects. |
| Stereo stream | Pairs of decoded sample values form left/right frames. | `sample_count / (2 * sample_rate)` | Music and stereo assets. |
| Header-driven | Uses `channels` from the ACM header. | `sample_count / (channels * sample_rate)` | Useful default for well-behaved standalone tools. |

For a format viewer, it is useful to expose both operations separately: reinterpret the sample stream as mono or stereo, and optionally downmix stereo to mono for listening. Reinterpretation changes duration; downmixing changes only channel count after the frame grouping has already happened.

## WAVC Wrapper

Some Interplay/BioWare-era files use a `WAVC` wrapper before an ACM stream. This is not the normal Fallout 1/2 standalone `.ACM` file layout, but it appears in broader Interplay ACM documentation and tooling.

```text
struct WavcHeader {
    char     signature[4];      // "WAVC"
    char     version[4];        // "V1.0"
    uint32_t uncompressed_size;
    uint32_t compressed_size;
    uint32_t header_size;       // often 28
    uint16_t channels;
    uint16_t bits;
    uint16_t sample_rate;
    uint16_t unknown;
};
```

If a file starts with `WAVC`, a general-purpose tool should skip to the embedded ACM header using the wrapper's header size rather than expecting `97 28 03 01` at offset `0`.

## Validation Checklist

- File should contain at least 14 bytes.
- Standalone Fallout ACM signature should be bytes `97 28 03 01`.
- `sample_count` should be nonzero for normal playable files.
- `channels` should normally be `1` or `2`.
- `sample_rate` should usually be `22050` for Fallout data, though the format can carry other rates.
- `levels` should produce a reasonable `cols = 1 << levels`; very large values can overflow allocations.
- `rows` should be nonzero and `rows * cols` should fit the decoder's block buffer limits.
- Decode should stop after exactly `sample_count` sample values even if the last block has padding or unused bitstream data.
- If a speech or narration file has the right pitch but plays too fast, test mono stream interpretation before assuming the ACM bitstream decode is wrong.
- Reject or explicitly handle filler codes not supported by the target engine.

## Authoring Notes

- For Fallout 1/2 compatibility, encode to 16-bit PCM ACM and prefer `22050 Hz`.
- Use mono or stereo according to the playback path. Speech, narration, and many SFX are effectively mono; music is commonly treated as stereo by playback code.
- When converting or previewing audio, keep "reinterpret stream as mono/stereo" separate from "downmix to mono". Reinterpretation changes duration; downmixing does not.
- Dialogue speech file names are usually referenced from MSG without path or extension.
- When replacing talking-head speech, update or regenerate the matching [LIP](lip.html) timing file if the duration or phrasing changes.
- Do not wrap Fallout replacement speech/music in WAV unless the target engine/tool explicitly supports that wrapper.
- When storing ACM in DAT2, remember that DAT compression is separate and optional. Recompressing already-compressed ACM data may not save much space.

## Related Formats

- [MSG File Format](msg.html) - dialogue lines can reference speech ACM base names.
- [LIP File Format](lip.html) - talking-head mouth timing paired with speech audio.
- [DAT File Format](dat.html) - archive container that often stores ACM resources.
- [MAP File Format](map.html) - maps select background music indirectly through map indexes and world-map configuration.
- [PRO File Format](pro.html) - item, scenery, and weapon sound IDs feed SFX name construction.
- [World-map Text Configuration Files](worldmap_config.html) - map music and ambient sound effects are referenced from text configuration.

## Source References

- [Fallout 2 CE `sound_decoder.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/sound_decoder.cc) - ACM header parsing, bitstream reader, filler table, inverse transform, and PCM output.
- [Fallout 2 CE `audio_file.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/audio_file.cc) - compressed audio file wrapper, decoded byte-size handling, and seek behavior.
- [Fallout 2 CE `game_sound.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/game_sound.cc) - Fallout music, speech, and SFX paths and ACM playback integration.
- [FFmpeg `interplayacm.c`](https://github.com/FFmpeg/FFmpeg/blob/master/libavcodec/interplayacm.c) - independent Interplay ACM decoder, filler functions, block transform, and output model.
- [MultimediaWiki Interplay ACM](https://wiki.multimedia.cx/index.php/Interplay_ACM) - concise header summary, broader game usage, and WAVC wrapper note.
- [Vault-Tec Labs ACM File Format](https://falloutmods.fandom.com/wiki/ACM_File_Format) - Abel's original Fallout-oriented ACM notes.

## Tools

- [TeamX sound utilities](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html#sound)
- [Game Audio Player](https://fodev.net/files/archive/gap.zip)
- [libacm](https://sourceforge.net/projects/libacm.berlios/)
- [FFmpeg](https://ffmpeg.org/) with the `interplayacm` decoder
- [SND2ACM](https://www.moddb.com/games/fallout-2/downloads/snd2acm)

## Credits

The original Fallout ACM write-up was written by Abel in 2000. Later decoder work by Marko Kreen, Adam Gashlin, Paul B Mahol, Fallout 2 CE contributors, and other open-source authors makes the format much easier to verify today.

```raw-html
<script>
(function () {
    "use strict";

    var fileInput = document.getElementById("acmFileInput");
    var decoderMode = document.getElementById("acmDecoderMode");
    var outputMode = document.getElementById("acmOutputMode");
    var decodeButton = document.getElementById("acmDecodeButton");
    var playButton = document.getElementById("acmPlayButton");
    var stopButton = document.getElementById("acmStopButton");
    var downloadButton = document.getElementById("acmDownloadWavButton");
    var clearButton = document.getElementById("acmClearButton");
    var canvas = document.getElementById("acmWaveform");
    var status = document.getElementById("acmStatus");
    var metadata = document.getElementById("acmMetadata");
    var ctx = canvas.getContext("2d");

    var audioContext = null;
    var sourceNode = null;
    var decodedFile = null;
    var renderedAudio = null;
    var wavUrl = null;

    var MAP_1BIT = [-1, 1];
    var MAP_2BIT_NEAR = [-2, -1, 1, 2];
    var MAP_2BIT_FAR = [-3, -2, 2, 3];
    var MAP_3BIT = [-4, -3, -2, -1, 1, 2, 3, 4];

    function BitReader(bytes, offset, strict) {
        this.bytes = bytes;
        this.byteOffset = offset;
        this.bitBuffer = 0;
        this.bitCount = 0;
        this.strict = strict;
        this.paddedBits = 0;
    }

    BitReader.prototype.getBits = function (count) {
        var value = 0;
        var written = 0;

        while (written < count) {
            if (this.bitCount === 0) {
                if (this.byteOffset >= this.bytes.length) {
                    if (this.strict) {
                        throw new Error("Unexpected end of ACM bitstream.");
                    }
                    this.bitBuffer = 0;
                    this.paddedBits += 8;
                } else {
                    this.bitBuffer = this.bytes[this.byteOffset++];
                }
                this.bitCount = 8;
            }

            var take = Math.min(this.bitCount, count - written);
            var mask = (1 << take) - 1;
            value += (this.bitBuffer & mask) * Math.pow(2, written);
            this.bitBuffer = Math.floor(this.bitBuffer / Math.pow(2, take));
            this.bitCount -= take;
            written += take;
        }

        return value;
    };

    function readU16LE(bytes, offset) {
        return bytes[offset] | (bytes[offset + 1] << 8);
    }

    function readU32LE(bytes, offset) {
        return (bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24)) >>> 0;
    }

    function readAscii(bytes, offset, length) {
        var text = "";
        for (var i = 0; i < length; i++) {
            text += String.fromCharCode(bytes[offset + i] || 0);
        }
        return text;
    }

    function sign16(value) {
        value &= 0xFFFF;
        return value & 0x8000 ? value - 0x10000 : value;
    }

    function clamp16(value) {
        if (value < -32768) {
            return -32768;
        }
        if (value > 32767) {
            return 32767;
        }
        return value | 0;
    }

    function AcmDecoder(buffer, mode) {
        this.bytes = new Uint8Array(buffer);
        this.mode = mode;
        this.allowWavc = mode !== "strict";
        this.allowCode31 = mode === "auto" || mode === "ce";
        this.strict = mode === "strict";
        this.wrapper = null;
        this.parseHeader();
    }

    AcmDecoder.prototype.parseHeader = function () {
        var offset = 0;

        if (this.bytes.length >= 4 && readAscii(this.bytes, 0, 4) === "WAVC") {
            if (!this.allowWavc) {
                throw new Error("WAVC wrapper found, but Fallout strict mode expects a standalone ACM header.");
            }
            if (this.bytes.length < 28) {
                throw new Error("WAVC wrapper is too short.");
            }

            var headerSize = readU32LE(this.bytes, 16);
            if (headerSize < 28 || headerSize + 14 > this.bytes.length) {
                throw new Error("WAVC header size does not point to an embedded ACM stream.");
            }

            this.wrapper = {
                version: readAscii(this.bytes, 4, 4),
                uncompressedSize: readU32LE(this.bytes, 8),
                compressedSize: readU32LE(this.bytes, 12),
                headerSize: headerSize,
                channels: readU16LE(this.bytes, 20),
                bits: readU16LE(this.bytes, 22),
                sampleRate: readU16LE(this.bytes, 24)
            };
            offset = headerSize;
        }

        if (offset + 14 > this.bytes.length) {
            throw new Error("File is too short for an ACM header.");
        }

        if (this.bytes[offset] !== 0x97 || this.bytes[offset + 1] !== 0x28 || this.bytes[offset + 2] !== 0x03 || this.bytes[offset + 3] !== 0x01) {
            throw new Error("Missing ACM signature bytes 97 28 03 01.");
        }

        this.streamOffset = offset;
        this.sampleCount = readU32LE(this.bytes, offset + 4);
        this.channels = readU16LE(this.bytes, offset + 8);
        this.sampleRate = readU16LE(this.bytes, offset + 10);
        this.attributes = readU16LE(this.bytes, offset + 12);
        this.levels = this.attributes & 0x0F;
        this.rows = this.attributes >>> 4;
        this.cols = Math.pow(2, this.levels);
        this.blockLength = this.rows * this.cols;
        this.reader = new BitReader(this.bytes, offset + 14, this.strict);

        if (this.sampleCount === 0) {
            throw new Error("ACM header reports zero sample values.");
        }
        if (this.channels < 1 || this.channels > 2) {
            throw new Error("This player supports mono and stereo ACM files; header channels=" + this.channels + ".");
        }
        if (this.sampleRate < 4000 || this.sampleRate > 192000) {
            throw new Error("Sample rate looks unreasonable: " + this.sampleRate + " Hz.");
        }
        if (this.levels > 15 || this.rows === 0 || this.blockLength <= 0 || this.blockLength > 1048576) {
            throw new Error("ACM block shape is not reasonable: rows=" + this.rows + ", levels=" + this.levels + ".");
        }

        this.block = new Int32Array(this.blockLength);
        this.wrap = new Int32Array(Math.max(0, 2 * this.cols - 2, Math.floor(3 * this.cols / 2 - 2)));
        this.mid = new Int32Array(0x10000);
        this.midBase = 0x8000;
        this.usedCode31 = false;
        this.blocksDecoded = 0;
    };

    AcmDecoder.prototype.midValue = function (index) {
        var pos = this.midBase + index;
        if (pos < 0 || pos >= this.mid.length) {
            throw new Error("Amplitude-table index out of range: " + index + ".");
        }
        return this.mid[pos];
    };

    AcmDecoder.prototype.setPos = function (row, col, index) {
        this.block[row * this.cols + col] = this.midValue(index);
    };

    AcmDecoder.prototype.decode = function () {
        var out = new Int16Array(this.sampleCount);
        var outPos = 0;

        while (outPos < out.length) {
            this.decodeBlock();
            var count = Math.min(this.blockLength, out.length - outPos);
            for (var i = 0; i < count; i++) {
                out[outPos++] = clamp16(this.block[i] >> this.levels);
            }
            this.blocksDecoded++;
        }

        return {
            samples: out,
            sampleCount: this.sampleCount,
            channels: this.channels,
            sampleRate: this.sampleRate,
            levels: this.levels,
            rows: this.rows,
            cols: this.cols,
            blockLength: this.blockLength,
            attributes: this.attributes,
            wrapper: this.wrapper,
            usedCode31: this.usedCode31,
            paddedBits: this.reader.paddedBits,
            blocksDecoded: this.blocksDecoded
        };
    };

    AcmDecoder.prototype.decodeBlock = function () {
        var pwr = this.reader.getBits(4);
        var val = this.reader.getBits(16);
        var count = Math.pow(2, pwr);
        var x = 0;

        this.mid.fill(0);
        for (var i = 0; i < count; i++) {
            this.mid[this.midBase + i] = x;
            x += val;
        }

        x = -val;
        for (i = 1; i <= count; i++) {
            this.mid[this.midBase - i] = x;
            x -= val;
        }

        var transformed = this.fillBlock();
        if (transformed) {
            this.juggleBlock();
        }
    };

    AcmDecoder.prototype.fillBlock = function () {
        for (var col = 0; col < this.cols; col++) {
            var code = this.reader.getBits(5);
            if (code === 31 && this.allowCode31) {
                this.readRawCode31Block();
                return false;
            }
            this.runFiller(code, col);
        }
        return true;
    };

    AcmDecoder.prototype.runFiller = function (code, col) {
        if (code === 0) {
            for (var row = 0; row < this.rows; row++) {
                this.setPos(row, col, 0);
            }
        } else if (code >= 3 && code <= 16) {
            this.linear(code, col);
        } else if (code === 17) {
            this.k13(col);
        } else if (code === 18) {
            this.k12(col);
        } else if (code === 19) {
            this.t15(col);
        } else if (code === 20) {
            this.k24(col);
        } else if (code === 21) {
            this.k23(col);
        } else if (code === 22) {
            this.t27(col);
        } else if (code === 23) {
            this.k35(col);
        } else if (code === 24) {
            this.k34(col);
        } else if (code === 26) {
            this.k45(col);
        } else if (code === 27) {
            this.k44(col);
        } else if (code === 29) {
            this.t37(col);
        } else {
            throw new Error("Unsupported ACM filler code " + code + " at column " + col + ".");
        }
    };

    AcmDecoder.prototype.linear = function (bits, col) {
        var middle = Math.pow(2, bits - 1);
        for (var row = 0; row < this.rows; row++) {
            this.setPos(row, col, this.reader.getBits(bits) - middle);
        }
    };

    AcmDecoder.prototype.k13 = function (col) {
        for (var row = 0; row < this.rows; row++) {
            var bit = this.reader.getBits(1);
            if (bit === 0) {
                this.setPos(row++, col, 0);
                if (row >= this.rows) {
                    break;
                }
                this.setPos(row, col, 0);
                continue;
            }
            bit = this.reader.getBits(1);
            if (bit === 0) {
                this.setPos(row, col, 0);
                continue;
            }
            this.setPos(row, col, MAP_1BIT[this.reader.getBits(1)]);
        }
    };

    AcmDecoder.prototype.k12 = function (col) {
        for (var row = 0; row < this.rows; row++) {
            if (this.reader.getBits(1) === 0) {
                this.setPos(row, col, 0);
            } else {
                this.setPos(row, col, MAP_1BIT[this.reader.getBits(1)]);
            }
        }
    };

    AcmDecoder.prototype.k24 = function (col) {
        for (var row = 0; row < this.rows; row++) {
            var bit = this.reader.getBits(1);
            if (bit === 0) {
                this.setPos(row++, col, 0);
                if (row >= this.rows) {
                    break;
                }
                this.setPos(row, col, 0);
                continue;
            }
            bit = this.reader.getBits(1);
            if (bit === 0) {
                this.setPos(row, col, 0);
                continue;
            }
            this.setPos(row, col, MAP_2BIT_NEAR[this.reader.getBits(2)]);
        }
    };

    AcmDecoder.prototype.k23 = function (col) {
        for (var row = 0; row < this.rows; row++) {
            if (this.reader.getBits(1) === 0) {
                this.setPos(row, col, 0);
            } else {
                this.setPos(row, col, MAP_2BIT_NEAR[this.reader.getBits(2)]);
            }
        }
    };

    AcmDecoder.prototype.k35 = function (col) {
        for (var row = 0; row < this.rows; row++) {
            var bit = this.reader.getBits(1);
            if (bit === 0) {
                this.setPos(row++, col, 0);
                if (row >= this.rows) {
                    break;
                }
                this.setPos(row, col, 0);
                continue;
            }
            bit = this.reader.getBits(1);
            if (bit === 0) {
                this.setPos(row, col, 0);
                continue;
            }
            bit = this.reader.getBits(1);
            if (bit === 0) {
                this.setPos(row, col, MAP_1BIT[this.reader.getBits(1)]);
                continue;
            }
            this.setPos(row, col, MAP_2BIT_FAR[this.reader.getBits(2)]);
        }
    };

    AcmDecoder.prototype.k34 = function (col) {
        for (var row = 0; row < this.rows; row++) {
            var bit = this.reader.getBits(1);
            if (bit === 0) {
                this.setPos(row, col, 0);
                continue;
            }
            bit = this.reader.getBits(1);
            if (bit === 0) {
                this.setPos(row, col, MAP_1BIT[this.reader.getBits(1)]);
                continue;
            }
            this.setPos(row, col, MAP_2BIT_FAR[this.reader.getBits(2)]);
        }
    };

    AcmDecoder.prototype.k45 = function (col) {
        for (var row = 0; row < this.rows; row++) {
            var bit = this.reader.getBits(1);
            if (bit === 0) {
                this.setPos(row++, col, 0);
                if (row >= this.rows) {
                    break;
                }
                this.setPos(row, col, 0);
                continue;
            }
            bit = this.reader.getBits(1);
            if (bit === 0) {
                this.setPos(row, col, 0);
                continue;
            }
            this.setPos(row, col, MAP_3BIT[this.reader.getBits(3)]);
        }
    };

    AcmDecoder.prototype.k44 = function (col) {
        for (var row = 0; row < this.rows; row++) {
            if (this.reader.getBits(1) === 0) {
                this.setPos(row, col, 0);
            } else {
                this.setPos(row, col, MAP_3BIT[this.reader.getBits(3)]);
            }
        }
    };

    AcmDecoder.prototype.t15 = function (col) {
        for (var row = 0; row < this.rows;) {
            var bits = this.reader.getBits(5);
            if (bits > 26) {
                throw new Error("Invalid t15 ACM tuple value " + bits + ".");
            }
            this.setPos(row++, col, bits % 3 - 1);
            bits = Math.floor(bits / 3);
            if (row >= this.rows) {
                break;
            }
            this.setPos(row++, col, bits % 3 - 1);
            bits = Math.floor(bits / 3);
            if (row >= this.rows) {
                break;
            }
            this.setPos(row++, col, bits % 3 - 1);
        }
    };

    AcmDecoder.prototype.t27 = function (col) {
        for (var row = 0; row < this.rows;) {
            var bits = this.reader.getBits(7);
            if (bits > 124) {
                throw new Error("Invalid t27 ACM tuple value " + bits + ".");
            }
            this.setPos(row++, col, bits % 5 - 2);
            bits = Math.floor(bits / 5);
            if (row >= this.rows) {
                break;
            }
            this.setPos(row++, col, bits % 5 - 2);
            bits = Math.floor(bits / 5);
            if (row >= this.rows) {
                break;
            }
            this.setPos(row++, col, bits % 5 - 2);
        }
    };

    AcmDecoder.prototype.t37 = function (col) {
        for (var row = 0; row < this.rows;) {
            var bits = this.reader.getBits(7);
            if (bits > 120) {
                throw new Error("Invalid t37 ACM tuple value " + bits + ".");
            }
            this.setPos(row++, col, bits % 11 - 5);
            bits = Math.floor(bits / 11);
            if (row >= this.rows) {
                break;
            }
            this.setPos(row++, col, bits % 11 - 5);
        }
    };

    AcmDecoder.prototype.readRawCode31Block = function () {
        var scale = Math.pow(2, this.levels);
        for (var i = 0; i < this.blockLength; i++) {
            this.block[i] = sign16(this.reader.getBits(16)) * scale;
        }
        this.usedCode31 = true;
    };

    AcmDecoder.prototype.juggle = function (wrapOffset, blockOffset, subLen, subCount) {
        for (var i = 0; i < subLen; i++) {
            var p = blockOffset + i;
            var r0 = this.wrap[wrapOffset];
            var r1 = this.wrap[wrapOffset + 1];

            for (var j = 0; j < subCount / 2; j++) {
                var r2 = this.block[p];
                this.block[p] = r1 * 2 + r0 + r2;
                p += subLen;
                var r3 = this.block[p];
                this.block[p] = r2 * 2 - r1 - r3;
                p += subLen;
                r0 = r2;
                r1 = r3;
            }

            this.wrap[wrapOffset++] = r0;
            this.wrap[wrapOffset++] = r1;
        }
    };

    AcmDecoder.prototype.juggleBlock = function () {
        if (this.levels === 0) {
            return;
        }

        var stepRows = this.levels > 9 ? 1 : Math.floor(2048 / this.cols) - 2;
        if (stepRows < 1) {
            stepRows = 1;
        }

        var remainingRows = this.rows;
        var blockOffset = 0;

        while (true) {
            var wrapOffset = 0;
            var subCount = Math.min(stepRows, remainingRows);
            var subLen = this.cols / 2;
            subCount *= 2;

            this.juggle(wrapOffset, blockOffset, subLen, subCount);
            wrapOffset += subLen * 2;

            for (var i = 0, p = blockOffset; i < subCount; i++, p += subLen) {
                this.block[p]++;
            }

            while (subLen > 1) {
                subLen = Math.floor(subLen / 2);
                subCount *= 2;
                this.juggle(wrapOffset, blockOffset, subLen, subCount);
                wrapOffset += subLen * 2;
            }

            if (remainingRows <= stepRows) {
                break;
            }
            remainingRows -= stepRows;
            blockOffset += stepRows * this.cols;
        }
    };

    function inferPlaybackMode(file) {
        var name = file ? (file.webkitRelativePath || file.name || "").toLowerCase() : "";
        if (/(^|[\\\/_-])(nar|narr|narrat|narrator|voice|speech|dialog|dialogue)/.test(name) || name.indexOf("sound\\speech") !== -1 || name.indexOf("sound/speech") !== -1) {
            return "speech";
        }
        if (/(^|[\\\/_-])(mus|music|theme|world|ambient)/.test(name) || name.indexOf("sound\\music") !== -1 || name.indexOf("sound/music") !== -1) {
            return "music";
        }
        return "original";
    }

    function chooseChannelPlan(decoded, mode, file) {
        var headerChannels = decoded.channels === 2 ? 2 : 1;
        var effectiveMode = mode === "context" ? inferPlaybackMode(file) : mode;

        if (effectiveMode === "mono" || effectiveMode === "speech") {
            return {
                sourceChannels: 1,
                outputChannels: 1,
                mode: effectiveMode,
                description: effectiveMode === "speech" ? "speech/narration mono stream" : "mono stream"
            };
        }
        if (effectiveMode === "stereo" || effectiveMode === "music") {
            return {
                sourceChannels: 2,
                outputChannels: 2,
                mode: effectiveMode,
                description: effectiveMode === "music" ? "music stereo stream" : "stereo stream"
            };
        }
        if (effectiveMode === "downmix") {
            return {
                sourceChannels: headerChannels,
                outputChannels: 1,
                mode: effectiveMode,
                description: "header channels downmixed to mono"
            };
        }
        return {
            sourceChannels: headerChannels,
            outputChannels: headerChannels,
            mode: "original",
            description: "header channels"
        };
    }

    function renderSamples(decoded, mode, file) {
        var plan = chooseChannelPlan(decoded, mode, file);
        var sourceChannels = plan.sourceChannels;
        var outChannels = plan.outputChannels;
        var frameCount = Math.floor(decoded.samples.length / sourceChannels);
        var channels = [];

        for (var ch = 0; ch < outChannels; ch++) {
            channels[ch] = new Float32Array(frameCount);
        }

        for (var frame = 0; frame < frameCount; frame++) {
            var left;
            var right;

            if (sourceChannels === 2) {
                left = decoded.samples[frame * 2] / 32768;
                right = decoded.samples[frame * 2 + 1] / 32768;
            } else {
                left = decoded.samples[frame] / 32768;
                right = left;
            }

            if (outChannels === 1) {
                channels[0][frame] = (left + right) / 2;
            } else {
                channels[0][frame] = left;
                channels[1][frame] = right;
            }
        }

        return {
            channels: channels,
            channelCount: outChannels,
            sourceChannelCount: sourceChannels,
            frameCount: frameCount,
            sampleRate: decoded.sampleRate,
            duration: frameCount / decoded.sampleRate,
            channelPlan: plan,
            droppedSamples: decoded.samples.length - frameCount * sourceChannels
        };
    }

    function buildAudioBuffer(rendered) {
        if (!audioContext) {
            audioContext = new (window.AudioContext || window.webkitAudioContext)();
        }
        var buffer = audioContext.createBuffer(rendered.channelCount, rendered.frameCount, rendered.sampleRate);
        for (var ch = 0; ch < rendered.channelCount; ch++) {
            buffer.copyToChannel(rendered.channels[ch], ch);
        }
        return buffer;
    }

    function drawWaveform(rendered) {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = "#050805";
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        ctx.strokeStyle = "#535b5a";
        ctx.beginPath();
        ctx.moveTo(0, canvas.height / 2);
        ctx.lineTo(canvas.width, canvas.height / 2);
        ctx.stroke();

        if (!rendered || rendered.frameCount === 0) {
            return;
        }

        var data = rendered.channels[0];
        var step = Math.max(1, Math.floor(data.length / canvas.width));

        ctx.strokeStyle = "#F49205";
        ctx.beginPath();
        for (var x = 0; x < canvas.width; x++) {
            var start = x * step;
            var end = Math.min(start + step, data.length);
            var min = 1;
            var max = -1;

            for (var i = start; i < end; i++) {
                if (data[i] < min) {
                    min = data[i];
                }
                if (data[i] > max) {
                    max = data[i];
                }
            }

            var y1 = (1 - max) * canvas.height / 2;
            var y2 = (1 - min) * canvas.height / 2;
            ctx.moveTo(x + 0.5, y1);
            ctx.lineTo(x + 0.5, y2);
        }
        ctx.stroke();
    }

    function formatDuration(seconds) {
        if (!isFinite(seconds)) {
            return "unknown";
        }
        var minutes = Math.floor(seconds / 60);
        var rest = seconds - minutes * 60;
        return minutes + ":" + rest.toFixed(3).padStart(6, "0");
    }

    function updateMetadata(file, decoded, rendered) {
        var lines = [
            "File: " + file.name + " (" + file.size.toLocaleString() + " bytes)",
            "Header: " + decoded.sampleRate + " Hz, " + decoded.channels + " channel(s), " + decoded.sampleCount.toLocaleString() + " sample values",
            "Block shape: rows=" + decoded.rows + ", levels=" + decoded.levels + ", cols=" + decoded.cols + ", block values=" + decoded.blockLength,
            "Decoded blocks: " + decoded.blocksDecoded.toLocaleString(),
            "Playback: " + rendered.sampleRate + " Hz, " + rendered.channelCount + " channel(s), " + rendered.frameCount.toLocaleString() + " frames, " + formatDuration(rendered.duration),
            "Channel interpretation: " + rendered.channelPlan.description + " (source grouping: " + rendered.sourceChannelCount + ", output: " + rendered.channelCount + ")"
        ];

        if (decoded.wrapper) {
            lines.push("WAVC wrapper: " + decoded.wrapper.version + ", header size=" + decoded.wrapper.headerSize + ", wrapper rate=" + decoded.wrapper.sampleRate + " Hz, bits=" + decoded.wrapper.bits);
        }
        if (decoded.usedCode31) {
            lines.push("Compatibility note: CE filler code 31 raw-block extension was used.");
        }
        if (decoded.paddedBits) {
            lines.push("Compatibility note: " + decoded.paddedBits + " zero padding bit(s) were read at end of stream.");
        }
        if (rendered.droppedSamples) {
            lines.push("Compatibility note: " + rendered.droppedSamples + " trailing sample value(s) did not fit the selected channel grouping.");
        }

        metadata.textContent = lines.join("\n");
    }

    function makeWavBlob(rendered) {
        var bytesPerFrame = rendered.channelCount * 2;
        var buffer = new ArrayBuffer(44 + rendered.frameCount * bytesPerFrame);
        var view = new DataView(buffer);

        function writeString(offset, text) {
            for (var i = 0; i < text.length; i++) {
                view.setUint8(offset + i, text.charCodeAt(i));
            }
        }

        writeString(0, "RIFF");
        view.setUint32(4, 36 + rendered.frameCount * bytesPerFrame, true);
        writeString(8, "WAVE");
        writeString(12, "fmt ");
        view.setUint32(16, 16, true);
        view.setUint16(20, 1, true);
        view.setUint16(22, rendered.channelCount, true);
        view.setUint32(24, rendered.sampleRate, true);
        view.setUint32(28, rendered.sampleRate * bytesPerFrame, true);
        view.setUint16(32, bytesPerFrame, true);
        view.setUint16(34, 16, true);
        writeString(36, "data");
        view.setUint32(40, rendered.frameCount * bytesPerFrame, true);

        var offset = 44;
        for (var frame = 0; frame < rendered.frameCount; frame++) {
            for (var ch = 0; ch < rendered.channelCount; ch++) {
                var sample = Math.max(-1, Math.min(1, rendered.channels[ch][frame]));
                view.setInt16(offset, sample < 0 ? sample * 32768 : sample * 32767, true);
                offset += 2;
            }
        }

        return new Blob([buffer], { type: "audio/wav" });
    }

    function resetWavUrl() {
        if (wavUrl) {
            URL.revokeObjectURL(wavUrl);
            wavUrl = null;
        }
    }

    function stopPlayback() {
        if (sourceNode) {
            sourceNode.onended = null;
            sourceNode.stop();
            sourceNode.disconnect();
            sourceNode = null;
        }
        stopButton.disabled = true;
    }

    async function decodeSelectedFile() {
        var file = fileInput.files && fileInput.files[0];
        if (!file) {
            status.textContent = "Choose an ACM file first.";
            return;
        }

        stopPlayback();
        resetWavUrl();
        playButton.disabled = true;
        downloadButton.disabled = true;
        status.textContent = "Reading and decoding " + file.name + "...";
        metadata.textContent = "";
        drawWaveform(null);

        try {
            var buffer = await file.arrayBuffer();
            var decoder = new AcmDecoder(buffer, decoderMode.value);
            decodedFile = decoder.decode();
            renderedAudio = renderSamples(decodedFile, outputMode.value, file);
            drawWaveform(renderedAudio);
            updateMetadata(file, decodedFile, renderedAudio);
            status.textContent = "Decoded successfully. Ready to play.";
            playButton.disabled = false;
            downloadButton.disabled = false;
        } catch (error) {
            decodedFile = null;
            renderedAudio = null;
            status.textContent = "Decode failed: " + error.message;
            metadata.textContent = "";
        }
    }

    decodeButton.addEventListener("click", decodeSelectedFile);

    outputMode.addEventListener("change", function () {
        if (!decodedFile) {
            return;
        }
        stopPlayback();
        resetWavUrl();
        renderedAudio = renderSamples(decodedFile, outputMode.value, fileInput.files && fileInput.files[0]);
        drawWaveform(renderedAudio);
        if (fileInput.files && fileInput.files[0]) {
            updateMetadata(fileInput.files[0], decodedFile, renderedAudio);
        }
    });

    playButton.addEventListener("click", async function () {
        if (!renderedAudio) {
            return;
        }
        stopPlayback();
        var buffer = buildAudioBuffer(renderedAudio);
        if (audioContext.state === "suspended") {
            await audioContext.resume();
        }
        sourceNode = audioContext.createBufferSource();
        sourceNode.buffer = buffer;
        sourceNode.connect(audioContext.destination);
        sourceNode.onended = function () {
            sourceNode = null;
            stopButton.disabled = true;
            status.textContent = "Playback finished.";
        };
        sourceNode.start();
        stopButton.disabled = false;
        status.textContent = "Playing.";
    });

    stopButton.addEventListener("click", function () {
        stopPlayback();
        status.textContent = renderedAudio ? "Playback stopped." : "Choose an ACM file and decode it.";
    });

    downloadButton.addEventListener("click", function () {
        if (!renderedAudio) {
            return;
        }
        resetWavUrl();
        wavUrl = URL.createObjectURL(makeWavBlob(renderedAudio));
        var link = document.createElement("a");
        var file = fileInput.files && fileInput.files[0];
        var baseName = file ? file.name.replace(/\.[^.]+$/, "") : "decoded";
        link.href = wavUrl;
        link.download = baseName + "-" + outputMode.value + ".wav";
        document.body.appendChild(link);
        link.click();
        link.remove();
    });

    clearButton.addEventListener("click", function () {
        stopPlayback();
        resetWavUrl();
        decodedFile = null;
        renderedAudio = null;
        fileInput.value = "";
        playButton.disabled = true;
        downloadButton.disabled = true;
        metadata.textContent = "";
        status.textContent = "Choose an ACM file and decode it.";
        drawWaveform(null);
    });

    drawWaveform(null);
})();
</script>
```
