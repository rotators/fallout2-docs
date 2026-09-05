---
title: RIX File Format
output: rix.html
description: Fallout RIX splash-screen image format, header layout, embedded VGA palette, pixel data, validation, and authoring notes.
---

# RIX File Format

RIX files are ColoRIX indexed bitmap images. Fallout and Fallout 2 use a narrow subset of the format for startup/loading splash screens, stored under `art\splash` in `master.dat` or in the unpacked `data` tree.

The wider ColoRIX format can describe several palette and storage variants, including compressed or planar data. Fallout's splash loader expects the simple VGA form: a `RIX3` signature, little-endian dimensions, a 256-color palette, and one linear byte per pixel. For Fallout modding tools, support that subset first.

## Where Fallout Uses It

Fallout 2 CE looks for splash files named `splashN.rix`, where `N` is the current splash index. It searches up to ten possible indexes, wrapping from `9` back to `0`, and displays the first existing file. English games use:

```text
art\splash\splash0.rix
art\splash\splash1.rix
...
```

For non-English languages, CE checks a localized folder first:

```text
art\<language>\splash\splashN.rix
```

Classic Fallout 2 data is commonly described as having six RIX splash screens. The CE loader's search range is ten slots, so tools should not hard-code six as a format limit.

## Fallout Subset

| Property | Fallout expectation |
|---|---|
| Signature | `RIX3` |
| Endianness | Little-endian control values |
| Typical dimensions | `640 x 480` |
| Palette type | `0xAF`, VGA palette: 256 mapped colors, 6 bits per channel |
| Storage type | `0x00`, linear uncompressed pixels |
| Palette length | `256 * 3 = 768` bytes |
| Pixel length | `width * height` bytes |
| Transparency | None in the splash-screen use case |
| Animation | None |

A vanilla-size `640 x 480` Fallout RIX file is `10 + 768 + 640 * 480 = 307978` bytes (`0x4B30A`).

## File Structure

| Offset | Size | Description |
|---|---|---|
| `0x0000` | 4 | Signature. Fallout splash files use ASCII `RIX3`. |
| `0x0004` | 2 | Image width in pixels, little-endian. Fallout splash art is normally `640` (`0x0280`). |
| `0x0006` | 2 | Image height in pixels, little-endian. Fallout splash art is normally `480` (`0x01E0`). |
| `0x0008` | 1 | Palette type. Fallout files use `0xAF`. |
| `0x0009` | 1 | Storage type. Fallout files use `0x00` for linear, uncompressed data. |
| `0x000A` | 768 | Palette: 256 RGB triples. |
| `0x030A` | `width * height` | Pixel indexes, one byte per pixel, left-to-right and top-to-bottom. |

## Header

```c
struct FalloutRixHeader {
    char signature[4];      // "RIX3"
    uint16_t width;         // little-endian
    uint16_t height;        // little-endian
    uint8_t palette_type;   // usually 0xAF
    uint8_t storage_type;   // usually 0x00
};
```

The original RIX documentation describes the first four bytes as `"RIX"` in a 4-byte field; Fallout files use the visible four-character value `RIX3`. Treat `RIX3` as the required magic for Fallout splash screens.

## Palette Type

The RIX palette type byte encodes whether a palette exists and what kind of indexed color it contains. Fallout's `0xAF` is the standard VGA mapped-color case:

| Bits/value | Meaning |
|---|---|
| `0x80` | Palette table is present. |
| `0x28` | Six RGB bits per channel, encoded as `(6 - 1) << 3`. |
| `0x07` | Eight pixel bits, encoded as `8 - 1`, giving 256 palette entries. |

Other RIX palette types exist, such as EGA, Targa, and PGA variants, but they are not part of Fallout's normal splash-screen subset.

## Storage Type

Fallout splash screens use storage type `0x00`: linear one-byte-per-pixel data. The broader RIX format defines flags and modes for compression, extension blocks, encryption, planar EGA data, and text-mode data. Fallout's splash loader does not implement those variants; a compressed or extension-bearing RIX should be converted to the plain linear form before use.

Fallout 2 CE only checks the `RIX3` signature before reading splash data. It then seeks to offset `0x000A`, reads 768 palette bytes, and reads `width * height` pixel bytes. A stricter external tool should still validate `palette_type == 0xAF`, `storage_type == 0x00`, and file length before accepting the image.

## Palette Data

The palette is stored immediately after the header as 256 RGB triples:

```text
palette_index_0: red, green, blue
palette_index_1: red, green, blue
...
palette_index_255: red, green, blue
```

VGA palette components are 6-bit values in the range `0..63`, stored in full bytes. Fallout's palette code uses this 0..63 range directly. When converting to modern 24-bit or 32-bit RGB, scale the values to `0..255`. A common quick conversion is `component << 2`; a slightly fuller mapping is `(component << 2) | (component >> 4)`.

Unlike FRM art, a Fallout RIX carries its own palette. It does not use `color.pal`, and switching from the game palette to the RIX palette is part of the splash display. This is why a screenshot or conversion that ignores the embedded palette will show the wrong colors.

## Pixel Data

Pixel data starts at offset `0x030A` for the normal 256-color Fallout subset. Each byte is a palette index. Pixels are stored in screen order:

```text
offset = 0x030A + y * width + x
palette_index = file[offset]
```

The first pixel byte is the top-left pixel. The last pixel byte is the bottom-right pixel. There is no row padding in the Fallout subset.

## Displaying a RIX

A simple reader for Fallout RIX files can follow this process:

1. Read the 10-byte header.
2. Require signature `RIX3`.
3. Read little-endian `width` and `height`.
4. Warn or reject if `palette_type` is not `0xAF` or `storage_type` is not `0x00`.
5. Read 768 palette bytes.
6. Read exactly `width * height` pixel bytes.
7. Convert each pixel index through the embedded palette.

Fallout fades from black to the RIX palette, then blits the indexed pixel buffer. Modern engines may scale the image if the game is running at a non-640x480 resolution.

## Validation Checklist

- Signature should be `RIX3`.
- Width and height should be positive and should not overflow `width * height`.
- For vanilla compatibility, prefer `640 x 480`.
- `palette_type` should be `0xAF` for Fallout splash screens.
- `storage_type` should be `0x00`.
- File should contain at least `10 + 768 + width * height` bytes.
- Palette components should normally be in the VGA range `0..63`. Values above `63` suggest a converter wrote 8-bit RGB values instead of VGA DAC values.
- Pixel bytes are palette indexes and can use the full `0..255` range.

## Authoring Notes

- Create the image as indexed 256-color art, not truecolor art with an external palette.
- Store the palette in RGB order, 256 triples, with 6-bit VGA component values.
- Keep the pixel buffer linear with no compression and no row padding.
- Name files `splash0.rix`, `splash1.rix`, and so on. For localized releases, place replacements under `art\<language>\splash` when targeting engines that support that lookup.
- When replacing classic Fallout 2 splash screens, use `640 x 480` unless the target engine or patch explicitly documents other dimensions.
- Some modern setups and high-resolution patches can use 8-bit BMP splash screens as an alternative, but plain RIX remains the base Fallout format.

## Relationship to Other Art Formats

| Format | Difference from RIX |
|---|---|
| [FRM](frm.html) | Used for almost all game art, may contain multiple frames/directions, and relies on external palettes. Splash screens are the main exception. |
| [PAL](pal.html) | External palette files used by FRM and other UI/game art. RIX embeds its own palette. |
| [DAT](dat.html) | Archive container that stores RIX files; it does not change the RIX byte format. |
| BMP | Some high-resolution patch configurations can load 8-bit BMP splash art, but BMP is not the original Fallout splash format. |

## Source References

- [Fallout 2 CE `game.cc`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/game.cc) - splash filename search, `RIX3` signature check, palette read, pixel read, scaling behavior, and splash index range.
- [RIX.TXT](https://www.fileformat.info/format/rix/spec/e384b123f2bf49108c71b7a3d595bfc8/view.htm) - original-style RIX header, palette type, storage type, and extension notes.
- [Encyclopedia of Graphics File Formats RIX summary](https://www.fileformat.info/format/rix/egff.htm) - broader ColoRIX family notes, palette/storage variants, and little-endian numeric format.
- [The Fallout Wiki RIX File Format](https://fallout.wiki/wiki/RIX_File_Format) - Fallout-specific RIX splash-screen notes inherited from older modding documentation.
- [Fallout 2 High Resolution Patch notes](https://falloutmods.fandom.com/wiki/Fallout_2_High_Resolution_Patch) - high-resolution patch context, including 8-bit BMP splash support in later versions.

## Tools

- [Graphics viewer 1.36](https://fodev.net/files/mirrors/teamx-utils/viewer.rar)
- [XnView](https://www.xnview.com/en/xnview/#formats)
- [rix2bmp](https://fodev.net/files/mirrors/teamx-utils/rix2bmp.rar)
- [rix2gif](https://fodev.net/files/mirrors/teamx-utils/rix2gif0.2.0.rar)
