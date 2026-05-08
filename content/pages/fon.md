---
title: FON File Format
output: fon.html
description: Fallout and Fallout 2 FON bitmap font file format, parser notes, runtime behavior, and interactive viewer.
---

# FON File Format

This document describes the FON bitmap font format used by the Fallout engine.

### Background

FON files are one of the Fallout engine's bitmap font formats. Fallout 2 loads up to ten text fonts named `font0.fon` through `font9.fon` for the low-numbered text font manager. Worldmap and interface text can also use AAF-based interface fonts, so FON should not be treated as the only worldmap font format. FON glyphs are non-scalable raster images and do not contain anti-aliasing information.

All numeric fields in the file are 32-bit little-endian integers. The two pointer fields in the header are serialized from the original in-memory structure, but the engine ignores their file values and allocates fresh arrays when loading.

### FON viewer

The viewer below parses the same layout described on this page. The glyph grid labels each glyph by its numeric index, and the text preview maps each input byte directly to the glyph with the same index. This matches the engine's 8-bit text rendering path: values outside the loaded glyph count do not draw anything.

```raw-html
<div class="viewer-panel">
    <div class="viewer-controls">
        <label>FON file
            <input id="fon-file" type="file" accept=".fon" />
        </label>
        <label>Zoom
            <input id="fon-zoom" type="number" min="1" max="12" value="4" />
        </label>
        <label>Foreground
            <input id="fon-fg" type="color" value="#cdcdcd" />
        </label>
        <label>Background
            <input id="fon-bg" type="color" value="#040c00" />
        </label>
    </div>
    <label>Preview text
        <input id="fon-preview-text" type="text" value="Fallout 2 worldmap text" />
    </label>
    <div id="fon-status" class="viewer-status"></div>
    <table class="font-summary">
        <tbody>
            <tr><td>Glyph count</td><td id="fon-glyph-count">-</td></tr>
            <tr><td>Line height</td><td id="fon-line-height">-</td></tr>
            <tr><td>Letter spacing</td><td id="fon-letter-spacing">-</td></tr>
            <tr><td>Bitmap data start</td><td id="fon-data-start">-</td></tr>
            <tr><td>Bitmap data size</td><td id="fon-data-size">-</td></tr>
        </tbody>
    </table>
    <h4>Text preview</h4>
    <div class="canvas-wrap"><canvas id="fon-preview" width="1" height="1"></canvas></div>
    <h4>Glyph grid</h4>
    <div class="canvas-wrap"><canvas id="fon-grid" width="1" height="1"></canvas></div>
</div>
```

### Structure

| Offset | Size | Type | Block | Description |
| --- | --- | --- | --- | --- |
| 0x0 | 4 | int | Header | Number of glyph images in the file. The shorthand `num` is used below. Some fonts contain fewer than 256 glyphs. |
| 0x4 | 4 | int | Header | Font height in pixels. The renderer uses this as the line height for every glyph. |
| 0x8 | 4 | int | Header | Horizontal spacing, in pixels, added after each glyph. |
| 0xC | 4 | FontInfo* | Header | Serialized pointer to the glyph descriptor array. Note: This field only matters in memory. In the file, the loader ignores the stored value. |
| 0x10 | 4 | unsigned char* | Header | Serialized pointer to the glyph bitmap data. Note: This field only matters in memory. In the file, the loader ignores the stored value. |
| 0x14 | 4 | int | Glyph descriptor array | Glyph 0 width in pixels |
| 0x18 | 4 | int | Glyph descriptor array | Glyph 0 offset, relative to the start of the bitmap data block |
| 0x1C | 4 | int | Glyph descriptor array | Glyph 1 width in pixels |
| 0x20 | 4 | int | Glyph descriptor array | Glyph 1 offset, relative to the start of the bitmap data block |
| ... | ... | ... | Glyph descriptor array | ... |
| 0x14 + 8 * num | 1 | unsigned char |  | Glyph bitmap data. Each glyph starts at its descriptor's data offset. |
| ... | ... | ... |  | Glyph bitmap data. Each glyph starts at its descriptor's data offset. |

### Parsing notes

FON has no magic number or version field. Tools usually identify it by filename and location, or by validating that the header, descriptor table, and bitmap offsets describe a plausible file.

The header is always 20 bytes long. The descriptor table begins immediately after it at `0x14` and contains `num` entries, each 8 bytes long. The bitmap data block therefore starts at `0x14 + 8 * num`. Descriptor offsets are relative to this bitmap data block, not to the beginning of the file.

Each glyph consumes `((width + 7) / 8) * height` bytes. This is integer arithmetic, so adding 7 before division is equivalent to rounding the row width up to the next byte. A zero-width glyph consumes no bitmap bytes, but can still occupy a glyph index. The original engine computes the total bitmap block length from the last descriptor; file inspection tools should additionally validate every descriptor so malformed offsets cannot point outside the file.

FON files store shape only. They do not store colors, palette indexes, kerning pairs, Unicode mappings, or proportional layout metadata beyond each glyph's width and the global spacing value.

### Runtime notes

In Fallout 2, FON files are registered as font ids `0` through `9`. The separate AAF interface font manager registers font ids `100` through `110`. A call such as `fontSetCurrent(3)` selects a loaded FON font, while `fontSetCurrent(101)` selects an AAF interface font.

The window manager initializes the FON text font system during startup. Missing individual `fontN.fon` files are tolerated, but initialization fails if none of the ten FON fonts can be loaded.

Shadow, underline, and monospaced text are renderer options applied by the font manager at draw time. They are not stored in the FON file.

### FON and AAF comparison

| Feature | FON | AAF |
| --- | --- | --- |
| Typical files | `font0.fon` through `font9.fon` | `font0.aaf` and other interface font files |
| Font id range | `0` through `9` in Fallout 2 CE | `100` through `110` in Fallout 2 CE |
| Glyph count | Stored in the file header | Always 256 descriptor slots |
| Pixel data | 1 bit per pixel; set bits draw the chosen text color | One value per pixel; values `1` through `9` represent brightness levels |
| Integer byte order | Little-endian 32-bit integers | Big-endian 16-bit and 32-bit integers |

### Memory structure

Mapper2.exe and fallout2.exe use the following in-memory structures for loaded FON files:

#### Header

```text
typedef struct {
    int num;                // number of glyph images in the file
    int height;             // line height in pixels
    int spacing;            // horizontal spacing between adjacent glyphs
    FontInfo* info;         // pointer to glyph descriptors
    unsigned char* data;    // pointer to glyph bitmap data
} Font;
```

#### Glyph descriptor

```text
typedef struct {
    int width;   // glyph width in pixels
    int offset;  // glyph image offset in the data block
} FontInfo;
```

The engine computes the size of the bitmap data block from the final glyph descriptor:

```text
last = font.num-1; // Index of the last glyph in the font
size = font.info[last].offset + (font.info[last].width + 7) / 8 * font.height;
```

#### Row byte calculation

```text
(font.info[last].width + 7) / 8
```

is the number of bytes needed for one row of a glyph image. Glyph bitmap data is a 1-bit matrix, with the most significant bit of each byte drawn first. A set bit draws the current text color; a clear bit leaves the destination pixel unchanged.

#### Text rendering

During rendering, each byte in the source text is used as a glyph index. When the index is present in the font, the renderer draws that glyph and advances by `glyph.width + font.spacing` pixels. Missing glyph indexes are skipped.

### Example

The following example shows an 8x16 pixel glyph described by 16 bitmap bytes:

```text
00 00 7e 81 a5 81 81 bd 99 81 81 7e 00 00 00 00
```

The bit matrix is:

```text
00       00000000        ........
00       00000000        ........
7e       01111110        .######.
81       10000001        #......#
a5       10100101        #.#..#.#
81       10000001        #......#
81       10000001        #......#
bd  ==>  10111101  ==>   #.####.#
99       10011001        #..##..#
81       10000001        #......#
81       10000001        #......#
7e       01111110        .######.
00       00000000        ........
00       00000000        ........
00       00000000        ........
00       00000000        ........
```

### Tools

[FON editor](https://fodev.net/files/mirrors/teamx-utils/fonedit1.0.rar)

[FON editor source code](https://fodev.net/files/mirrors/teamx-utils/fonedit_src.rar)

### Source code

[Fallout 2 Community Edition FON loader and renderer - C++](https://github.com/alexbatalov/fallout2-ce/blob/main/src/text_font.cc)

[Fallout 2 Community Edition font manager declarations - C++](https://github.com/alexbatalov/fallout2-ce/blob/main/src/text_font.h)

[Fallout 2 Community Edition AAF interface font manager - C++](https://github.com/alexbatalov/fallout2-ce/blob/main/src/font_manager.cc)

[Fallout 2 Community Edition window manager font initialization - C++](https://github.com/alexbatalov/fallout2-ce/blob/main/src/window_manager.cc)

[Fallout 2 Community Edition worldmap font usage - C++](https://github.com/alexbatalov/fallout2-ce/blob/main/src/worldmap.cc)
## History

2020-01-16 - Ported from [https://falloutmods.fandom.com/wiki/FON_File_Format](https://falloutmods.fandom.com/wiki/FON_File_Format)

```raw-html
<script>
(function () {
    "use strict";

    var currentFont = null;

    var fileInput = document.getElementById("fon-file");
    var zoomInput = document.getElementById("fon-zoom");
    var fgInput = document.getElementById("fon-fg");
    var bgInput = document.getElementById("fon-bg");
    var previewTextInput = document.getElementById("fon-preview-text");
    var statusEl = document.getElementById("fon-status");
    var previewCanvas = document.getElementById("fon-preview");
    var gridCanvas = document.getElementById("fon-grid");

    function setStatus(message) {
        statusEl.textContent = message || "";
    }

    function toHex(value) {
        return "0x" + value.toString(16).toUpperCase();
    }

    function rowBytes(width) {
        return Math.floor((width + 7) / 8);
    }

    function parseFon(buffer) {
        if (buffer.byteLength < 20) {
            throw new Error("File is too small for a FON header.");
        }

        var view = new DataView(buffer);
        var glyphCount = view.getInt32(0, true);
        var lineHeight = view.getInt32(4, true);
        var letterSpacing = view.getInt32(8, true);
        var dataStart = 20 + glyphCount * 8;

        if (glyphCount <= 0 || glyphCount > 512) {
            throw new Error("Unreasonable glyph count: " + glyphCount + ".");
        }

        if (lineHeight <= 0 || lineHeight > 256) {
            throw new Error("Unreasonable line height: " + lineHeight + ".");
        }

        if (dataStart > buffer.byteLength) {
            throw new Error("Glyph descriptor table extends past the end of the file.");
        }

        var glyphs = [];
        var bitmapSize = 0;

        for (var index = 0; index < glyphCount; index++) {
            var descriptorOffset = 20 + index * 8;
            var width = view.getInt32(descriptorOffset, true);
            var dataOffset = view.getInt32(descriptorOffset + 4, true);

            if (width < 0 || width > 512) {
                throw new Error("Unreasonable width for glyph " + index + ": " + width + ".");
            }

            if (dataOffset < 0) {
                throw new Error("Negative data offset for glyph " + index + ".");
            }

            var glyphSize = rowBytes(width) * lineHeight;
            bitmapSize = Math.max(bitmapSize, dataOffset + glyphSize);
            glyphs.push({ width: width, dataOffset: dataOffset });
        }

        if (dataStart + bitmapSize > buffer.byteLength) {
            throw new Error("Bitmap data extends past the end of the file.");
        }

        return {
            bytes: new Uint8Array(buffer),
            glyphCount: glyphCount,
            lineHeight: lineHeight,
            letterSpacing: letterSpacing,
            dataStart: dataStart,
            bitmapSize: bitmapSize,
            glyphs: glyphs
        };
    }

    function glyphHasPixel(font, glyphIndex, x, y) {
        var glyph = font.glyphs[glyphIndex];
        if (!glyph || glyph.width <= 0) {
            return false;
        }

        var stride = rowBytes(glyph.width);
        var byteOffset = font.dataStart + glyph.dataOffset + y * stride + (x >> 3);
        var mask = 0x80 >> (x & 7);
        return (font.bytes[byteOffset] & mask) !== 0;
    }

    function drawGlyph(ctx, font, glyphIndex, x, y, zoom, color) {
        var glyph = font.glyphs[glyphIndex];
        if (!glyph || glyph.width <= 0) {
            return;
        }

        ctx.fillStyle = color;
        for (var gy = 0; gy < font.lineHeight; gy++) {
            for (var gx = 0; gx < glyph.width; gx++) {
                if (glyphHasPixel(font, glyphIndex, gx, gy)) {
                    ctx.fillRect(x + gx * zoom, y + gy * zoom, zoom, zoom);
                }
            }
        }
    }

    function updateSummary(font) {
        document.getElementById("fon-glyph-count").textContent = font ? font.glyphCount : "-";
        document.getElementById("fon-line-height").textContent = font ? font.lineHeight + " px" : "-";
        document.getElementById("fon-letter-spacing").textContent = font ? font.letterSpacing + " px" : "-";
        document.getElementById("fon-data-start").textContent = font ? toHex(font.dataStart) : "-";
        document.getElementById("fon-data-size").textContent = font ? font.bitmapSize + " bytes" : "-";
    }

    function drawPreview(font) {
        var zoom = getZoom();
        var text = previewTextInput.value || "";
        var width = 1;
        var height = Math.max(1, font.lineHeight * zoom);

        for (var i = 0; i < text.length; i++) {
            var ch = text.charCodeAt(i) & 0xFF;
            if (ch < font.glyphCount) {
                width += (font.glyphs[ch].width + font.letterSpacing) * zoom;
            }
        }

        previewCanvas.width = Math.max(1, width);
        previewCanvas.height = height;

        var ctx = previewCanvas.getContext("2d");
        ctx.imageSmoothingEnabled = false;
        ctx.fillStyle = bgInput.value;
        ctx.fillRect(0, 0, previewCanvas.width, previewCanvas.height);

        var x = 0;
        for (var index = 0; index < text.length; index++) {
            var glyphIndex = text.charCodeAt(index) & 0xFF;
            if (glyphIndex < font.glyphCount) {
                drawGlyph(ctx, font, glyphIndex, x, 0, zoom, fgInput.value);
                x += (font.glyphs[glyphIndex].width + font.letterSpacing) * zoom;
            }
        }
    }

    function drawGrid(font) {
        var zoom = getZoom();
        var maxWidth = 0;
        for (var i = 0; i < font.glyphCount; i++) {
            maxWidth = Math.max(maxWidth, font.glyphs[i].width);
        }

        var columns = 16;
        var labelHeight = 14;
        var cellPadding = 4;
        var cellWidth = Math.max(24, maxWidth * zoom + cellPadding * 2);
        var cellHeight = font.lineHeight * zoom + labelHeight + cellPadding * 2;
        var rows = Math.ceil(font.glyphCount / columns);

        gridCanvas.width = columns * cellWidth;
        gridCanvas.height = rows * cellHeight;

        var ctx = gridCanvas.getContext("2d");
        ctx.imageSmoothingEnabled = false;
        ctx.fillStyle = bgInput.value;
        ctx.fillRect(0, 0, gridCanvas.width, gridCanvas.height);
        ctx.font = "10px Verdana, Arial, sans-serif";
        ctx.textBaseline = "top";

        for (var index = 0; index < font.glyphCount; index++) {
            var col = index % columns;
            var row = Math.floor(index / columns);
            var cellX = col * cellWidth;
            var cellY = row * cellHeight;
            var label = index.toString();
            var glyphX = cellX + cellPadding;
            var glyphY = cellY + labelHeight + cellPadding;

            ctx.fillStyle = "#333";
            ctx.fillRect(cellX, cellY, cellWidth - 1, cellHeight - 1);
            ctx.fillStyle = bgInput.value;
            ctx.fillRect(cellX + 1, cellY + 1, cellWidth - 3, cellHeight - 3);
            ctx.fillStyle = "#f49205";
            ctx.fillText(label, cellX + cellPadding, cellY + 2);
            drawGlyph(ctx, font, index, glyphX, glyphY, zoom, fgInput.value);
        }
    }

    function getZoom() {
        var zoom = parseInt(zoomInput.value, 10);
        if (!Number.isFinite(zoom)) {
            zoom = 4;
        }
        return Math.max(1, Math.min(12, zoom));
    }

    function redraw() {
        if (!currentFont) {
            return;
        }
        zoomInput.value = getZoom();
        drawPreview(currentFont);
        drawGrid(currentFont);
    }

    fileInput.addEventListener("change", function () {
        var file = fileInput.files && fileInput.files[0];
        if (!file) {
            return;
        }

        var reader = new FileReader();
        reader.onload = function () {
            try {
                currentFont = parseFon(reader.result);
                updateSummary(currentFont);
                setStatus("Loaded " + file.name + ".");
                redraw();
            } catch (err) {
                currentFont = null;
                updateSummary(null);
                previewCanvas.width = 1;
                previewCanvas.height = 1;
                gridCanvas.width = 1;
                gridCanvas.height = 1;
                setStatus(err.message);
            }
        };
        reader.readAsArrayBuffer(file);
    });

    zoomInput.addEventListener("input", redraw);
    fgInput.addEventListener("input", redraw);
    bgInput.addEventListener("input", redraw);
    previewTextInput.addEventListener("input", function () {
        if (currentFont) {
            drawPreview(currentFont);
        }
    });
})();
</script>
```
