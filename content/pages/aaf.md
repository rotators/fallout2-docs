---
title: AAF File Format
output: aaf.html
description: Fallout and Fallout 2 AAF bitmap font file format, parser notes, runtime behavior, and interactive viewer.
---

# AAF File Format

The AAF file format stores bitmap fonts used by Fallout and Fallout 2. This page describes the English-version files; localized releases may contain variations that are not covered here.

An AAF file stores one glyph descriptor for each of the 256 byte values. Glyphs are usually proportional: each glyph has its own width and height. The glyph bitmap is stored as one byte per pixel, with values in the range `0` through `9`.

- `0` means the pixel is transparent.
- `1` through `9` represent relative brightness, with `9` being the brightest. The brightness scale does not appear to be linear.

The following bitmap is a glyph from `FONT1.AAF` representing the character `b` (ASCII 98):

```text
  77
  77
  777763
  77  76
  77  77
  77  76
  777763
```

Pixels with value `0` are omitted because they are transparent. The values `7` and `6` mark brighter parts of the glyph, while `3` is dimmer and appears around the rounded corners.

All integer fields are stored in Motorola / big-endian byte order. The file starts with a 12-byte header followed by 256 glyph descriptors. Each descriptor is 8 bytes: width, height, and an offset relative to the start of the glyph data block. The glyph data block starts at `0x000C + 256 * 8 = 0x080C`.

### AAF viewer

The viewer below reads an uploaded `.aaf` file, validates the header and glyph descriptors, and renders both a text preview and a 256-slot glyph grid. Pixel values are shown as brightness levels of the selected foreground color.

```raw-html
<div class="viewer-panel">
    <div class="viewer-controls">
        <label>AAF file
            <input id="aaf-file" type="file" accept=".aaf" />
        </label>
        <label>Zoom
            <input id="aaf-zoom" type="number" min="1" max="12" value="4" />
        </label>
        <label>Foreground
            <input id="aaf-fg" type="color" value="#cdcdcd" />
        </label>
        <label>Background
            <input id="aaf-bg" type="color" value="#040c00" />
        </label>
    </div>
    <label>Preview text
        <input id="aaf-preview-text" type="text" value="Fallout 2 interface text" />
    </label>
    <div id="aaf-status" class="viewer-status"></div>
    <table class="font-summary">
        <tbody>
            <tr><td>Maximum glyph height</td><td id="aaf-max-height">-</td></tr>
            <tr><td>Horizontal gap</td><td id="aaf-horizontal-gap">-</td></tr>
            <tr><td>Space width</td><td id="aaf-space-width">-</td></tr>
            <tr><td>Vertical gap</td><td id="aaf-vertical-gap">-</td></tr>
            <tr><td>Glyph data size</td><td id="aaf-data-size">-</td></tr>
        </tbody>
    </table>
    <h4>Text preview</h4>
    <div class="canvas-wrap"><canvas id="aaf-preview" width="1" height="1"></canvas></div>
    <h4>Glyph grid</h4>
    <div class="canvas-wrap"><canvas id="aaf-grid" width="1" height="1"></canvas></div>
</div>
```

| Offset | Bytes | Data Type | Description |
| --- | --- | --- | --- |
| 0x000 | 4 | char[4] = "AAFF" | Signature used to identify the file type. |
| 0x004 | 2 | unsigned short | Maximum glyph height, including ascenders and descenders. |
| 0x006 | 2 | unsigned short | Horizontal gap, in pixels, added between adjacent glyphs. |
| 0x008 | 2 | unsigned short | Width, in pixels, used for the space character. |
| 0x00A | 2 | unsigned short | Vertical gap, in pixels, added between two lines of glyphs. |
| 0x00C | 2 | unsigned short | Width, in pixels, of glyph 0. |
| 0x00E | 2 | unsigned short | Height, in pixels, of glyph 0. |
| 0x010 | 4 | unsigned long | Offset of glyph 0, relative to the glyph data block at `0x080C`. If `width * height = 0`, this can be the offset of the next non-empty glyph. |
| 0x014 | 2 | unsigned short | Width, in pixels, of glyph 1. |
| 0x016 | 2 | unsigned short | Height, in pixels, of glyph 1. |
| 0x018 | 4 | unsigned long | Offset of glyph 1, relative to the glyph data block at `0x080C`. |
| 0x01C | (2 + 2 + 4) * (256 - 2) |  | Descriptions of glyphs 2 through 255, as described above. |
| 0x80C | GLYPH-0-WIDTH * GLYPH-0-HEIGHT | byte = [0..9] | Pixel data for glyph 0. Pixels are stored left to right, then top to bottom. Each pixel is one byte in the range `0` through `9`. |
| 0x080C + (GLYPH-0-WIDTH * GLYPH-0-HEIGHT) | GLYPH-1-WIDTH * GLYPH-1-HEIGHT | byte = [0..9] | Pixel data for glyph 1. |
| 0x080C + (GLYPH-0-WIDTH * GLYPH-0-HEIGHT) + (GLYPH-1-WIDTH * GLYPH-1-HEIGHT) | ... | byte = [0..9] | Pixel data for glyphs 2 through 255, as described above. |

### Parsing notes

The `AAFF` signature identifies the file type. After the 12-byte header, the descriptor table always contains 256 entries and therefore always ends at `0x080C`. Descriptor offsets are relative to this glyph data block, not to the beginning of the file.

Each glyph consumes `width * height` bytes. A NULL glyph has no pixel data of its own, and its offset can be the same as the next glyph with data, so readers should use width and height to decide whether to read pixels.

AAF stores glyph brightness, not final palette colors. At render time, the engine blends each non-zero pixel value with the selected text color.

### Runtime notes

In Fallout 2 CE, the AAF loader attempts to load interface fonts named `font0.aaf`, `font1.aaf`, and so on. The interface font manager exposes AAF fonts as font ids `100` through `110`, separate from the FON text font manager.

When rendering a string, a space character uses the header's space width field instead of the width in glyph descriptor 32. Each character advances by its character width plus the horizontal gap. The line height reported by the engine is the maximum glyph height plus the vertical gap.

Glyph pixels are bottom-aligned within the maximum glyph height. If a glyph is shorter than the maximum height, rendering skips the difference before drawing that glyph's rows.

### AAF and FON comparison

| Feature | AAF | FON |
| --- | --- | --- |
| Typical role | Interface font manager | Low-numbered text font manager |
| Typical files | `font0.aaf` and other interface font files | `font0.fon` through `font9.fon` |
| Font id range | `100` through `110` in Fallout 2 CE | `0` through `9` in Fallout 2 CE |
| Glyph slots | Always 256 descriptor slots | Glyph count is stored in the file header |
| Pixel data | One byte per pixel; values `1` through `9` represent brightness levels | 1 bit per pixel; set bits draw the chosen text color |
| Integer byte order | Big-endian 16-bit and 32-bit integers | Little-endian 32-bit integers |

### Source code

[Fallout 2 Community Edition AAF interface font manager - C++](https://github.com/alexbatalov/fallout2-ce/blob/main/src/font_manager.cc)

[Fallout 2 Community Edition FON text font manager - C++](https://github.com/alexbatalov/fallout2-ce/blob/main/src/text_font.cc)
## History

2019-12-16 - Ported from [https://falloutmods.fandom.com/wiki/AAF_File_Format](https://falloutmods.fandom.com/wiki/AAF_File_Format) by [ghost](https://github.com/ghost2238) Patched by Anchorite (anchorite2001@yandex.ru) Created by Noid (noid@888.nu)

```raw-html
<script>
(function () {
    "use strict";

    var HEADER_SIZE = 12;
    var GLYPH_COUNT = 256;
    var DESCRIPTOR_SIZE = 8;
    var DATA_START = HEADER_SIZE + GLYPH_COUNT * DESCRIPTOR_SIZE;
    var currentFont = null;

    var fileInput = document.getElementById("aaf-file");
    var zoomInput = document.getElementById("aaf-zoom");
    var fgInput = document.getElementById("aaf-fg");
    var bgInput = document.getElementById("aaf-bg");
    var previewTextInput = document.getElementById("aaf-preview-text");
    var statusEl = document.getElementById("aaf-status");
    var previewCanvas = document.getElementById("aaf-preview");
    var gridCanvas = document.getElementById("aaf-grid");

    function setStatus(message) {
        statusEl.textContent = message || "";
    }

    function parseColor(hex) {
        var value = parseInt(hex.slice(1), 16);
        return {
            r: (value >> 16) & 0xFF,
            g: (value >> 8) & 0xFF,
            b: value & 0xFF
        };
    }

    function brightnessColor(hex, level) {
        var color = parseColor(hex);
        var scale = Math.max(0, Math.min(9, level)) / 9;
        return "rgb(" +
            Math.round(color.r * scale) + "," +
            Math.round(color.g * scale) + "," +
            Math.round(color.b * scale) + ")";
    }

    function parseAaf(buffer) {
        if (buffer.byteLength < DATA_START) {
            throw new Error("File is too small for an AAF header and descriptor table.");
        }

        var view = new DataView(buffer);
        if (view.getUint8(0) !== 0x41 || view.getUint8(1) !== 0x41 || view.getUint8(2) !== 0x46 || view.getUint8(3) !== 0x46) {
            throw new Error("Missing AAFF signature.");
        }

        var maxHeight = view.getUint16(4, false);
        var horizontalGap = view.getUint16(6, false);
        var spaceWidth = view.getUint16(8, false);
        var verticalGap = view.getUint16(10, false);

        if (maxHeight === 0 || maxHeight > 512) {
            throw new Error("Unreasonable maximum glyph height: " + maxHeight + ".");
        }

        var glyphs = [];
        var dataSize = buffer.byteLength - DATA_START;
        var maxPixelValue = 0;

        for (var index = 0; index < GLYPH_COUNT; index++) {
            var descriptorOffset = HEADER_SIZE + index * DESCRIPTOR_SIZE;
            var width = view.getUint16(descriptorOffset, false);
            var height = view.getUint16(descriptorOffset + 2, false);
            var dataOffset = view.getUint32(descriptorOffset + 4, false);
            var glyphSize = width * height;

            if (width > 512 || height > 512) {
                throw new Error("Unreasonable size for glyph " + index + ": " + width + "x" + height + ".");
            }

            if (height > maxHeight) {
                throw new Error("Glyph " + index + " is taller than the maximum glyph height.");
            }

            if (glyphSize > 0 && dataOffset + glyphSize > dataSize) {
                throw new Error("Pixel data for glyph " + index + " extends past the end of the file.");
            }

            for (var byteIndex = 0; byteIndex < glyphSize; byteIndex++) {
                var pixel = view.getUint8(DATA_START + dataOffset + byteIndex);
                if (pixel > maxPixelValue) {
                    maxPixelValue = pixel;
                }
            }

            glyphs.push({ width: width, height: height, dataOffset: dataOffset });
        }

        if (maxPixelValue > 9) {
            throw new Error("Pixel value " + maxPixelValue + " is outside the expected 0..9 range.");
        }

        return {
            bytes: new Uint8Array(buffer),
            maxHeight: maxHeight,
            horizontalGap: horizontalGap,
            spaceWidth: spaceWidth,
            verticalGap: verticalGap,
            dataSize: dataSize,
            glyphs: glyphs
        };
    }

    function characterWidth(font, ch) {
        return ch === 32 ? font.spaceWidth : font.glyphs[ch].width;
    }

    function drawGlyph(ctx, font, glyphIndex, x, y, zoom, color) {
        var glyph = font.glyphs[glyphIndex];
        if (!glyph || glyph.width === 0 || glyph.height === 0) {
            return;
        }

        var glyphY = y + (font.maxHeight - glyph.height) * zoom;
        for (var gy = 0; gy < glyph.height; gy++) {
            for (var gx = 0; gx < glyph.width; gx++) {
                var pixel = font.bytes[DATA_START + glyph.dataOffset + gy * glyph.width + gx];
                if (pixel !== 0) {
                    ctx.fillStyle = brightnessColor(color, pixel);
                    ctx.fillRect(x + gx * zoom, glyphY + gy * zoom, zoom, zoom);
                }
            }
        }
    }

    function updateSummary(font) {
        document.getElementById("aaf-max-height").textContent = font ? font.maxHeight + " px" : "-";
        document.getElementById("aaf-horizontal-gap").textContent = font ? font.horizontalGap + " px" : "-";
        document.getElementById("aaf-space-width").textContent = font ? font.spaceWidth + " px" : "-";
        document.getElementById("aaf-vertical-gap").textContent = font ? font.verticalGap + " px" : "-";
        document.getElementById("aaf-data-size").textContent = font ? font.dataSize + " bytes" : "-";
    }

    function drawPreview(font) {
        var zoom = getZoom();
        var text = previewTextInput.value || "";
        var width = 1;
        var height = Math.max(1, (font.maxHeight + font.verticalGap) * zoom);

        for (var i = 0; i < text.length; i++) {
            width += (characterWidth(font, text.charCodeAt(i) & 0xFF) + font.horizontalGap) * zoom;
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
            drawGlyph(ctx, font, glyphIndex, x, 0, zoom, fgInput.value);
            x += (characterWidth(font, glyphIndex) + font.horizontalGap) * zoom;
        }
    }

    function drawGrid(font) {
        var zoom = getZoom();
        var maxWidth = font.spaceWidth;
        for (var i = 0; i < GLYPH_COUNT; i++) {
            maxWidth = Math.max(maxWidth, font.glyphs[i].width);
        }

        var columns = 16;
        var labelHeight = 14;
        var cellPadding = 4;
        var cellWidth = Math.max(28, maxWidth * zoom + cellPadding * 2);
        var cellHeight = font.maxHeight * zoom + labelHeight + cellPadding * 2;
        var rows = Math.ceil(GLYPH_COUNT / columns);

        gridCanvas.width = columns * cellWidth;
        gridCanvas.height = rows * cellHeight;

        var ctx = gridCanvas.getContext("2d");
        ctx.imageSmoothingEnabled = false;
        ctx.fillStyle = bgInput.value;
        ctx.fillRect(0, 0, gridCanvas.width, gridCanvas.height);
        ctx.font = "10px Verdana, Arial, sans-serif";
        ctx.textBaseline = "top";

        for (var index = 0; index < GLYPH_COUNT; index++) {
            var col = index % columns;
            var row = Math.floor(index / columns);
            var cellX = col * cellWidth;
            var cellY = row * cellHeight;
            var glyphX = cellX + cellPadding;
            var glyphY = cellY + labelHeight + cellPadding;

            ctx.fillStyle = "#333";
            ctx.fillRect(cellX, cellY, cellWidth - 1, cellHeight - 1);
            ctx.fillStyle = bgInput.value;
            ctx.fillRect(cellX + 1, cellY + 1, cellWidth - 3, cellHeight - 3);
            ctx.fillStyle = "#f49205";
            ctx.fillText(index.toString(), cellX + cellPadding, cellY + 2);
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
                currentFont = parseAaf(reader.result);
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
