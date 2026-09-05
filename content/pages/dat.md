---
title: DAT File Format
output: dat.html
description: Fallout DAT archive formats, resource lookup behavior, DAT2 zlib entries, DAT1 LZSS blocks, and writer validation notes.
toc: auto
---

# DAT File Format

DAT files are Fallout archive containers. They hold most of the game's loose resources: artwork, palettes, fonts, maps, prototypes, scripts, message files, sound effects, music, speech, movies, and assorted text configuration. The archive layer does not change the inner file formats; it only stores paths, compressed or uncompressed byte streams, and lookup metadata.

There are two incompatible archive formats with the same `.dat` extension:

| Name used here | Game | Endian | Compression | Directory location |
|---|---|---|---|---|
| DAT1 | Fallout 1 | Big-endian metadata | Fallout LZSS blocks | Header and directory tables at the start |
| DAT2 | Fallout 2 | Little-endian metadata | zlib streams | Directory tree and footer at the end |

`save.dat`, `worldmap.dat`, and many other files with a `.dat` extension are not DAT archives. They are independent binary formats documented separately.

## Archive Files

Fallout 2 normally uses these archive/search roots:

| Path/source | Role |
|---|---|
| `master.dat` | Main game resources. |
| `critter.dat` | Critter animation and critter-related resources. |
| `data\` or other patch directories | Loose-file override roots configured by the game's system paths. |
| `patch000.dat`, `patch001.dat`, ... | Optional patch archives opened after the base archives by CE. |
| `f2_res.dat` | High-resolution patch resources, when present. |

Paths stored inside DAT archives are relative game paths such as `art\intrface\iface.frm` or `text\english\game\worldmap.msg`. The same relative paths are used when resources are unpacked under `data\`.

## Resource Loading And Override Order

The Fallout resource layer behaves like a small virtual filesystem. Most game code asks for a relative path such as `art\items\stimpak.frm`, `proto\items\items.lst`, or `text\english\game\misc.msg`. The database layer searches all currently opened resource roots until it finds the first matching file.

The base roots come from [`fallout2.cfg`](cfg.html):

| Config key | Typical value | Role |
|---|---|---|
| `[system] master_dat` | `master.dat` | Main archive. |
| `[system] master_patches` | `data` | Loose patch directory for master resources. |
| `[system] critter_dat` | `critter.dat` | Critter archive. |
| `[system] critter_patches` | `data` | Loose patch directory for critter resources. |

Fallout 2 CE opens those roots in this startup order:

1. `master_dat`, if configured.
2. `master_patches`, if configured.
3. `critter_dat`, if configured.
4. `critter_patches`, if configured.
5. Patch archives matching the sfall `[Misc] PatchFile` template, normally `patch000.dat` through `patch999.dat`, if present.
6. `f2_res.dat`, if present.

Every successful `xbaseOpen` inserts the root at the front of the internal search list. Reopening an already-open root moves it to the front. Relative file lookup then searches that list from front to back. Because of that front-insertion behavior, the effective lookup precedence is the reverse of the opening sequence.

With the usual CE startup flow and default patch directory, effective lookup precedence is:

| Priority | Root | Notes |
|---|---|---|
| Highest | `f2_res.dat` | Opened last when present. |
|  | Highest-numbered `patchNNN.dat` | Patch archives are opened from `patch000.dat` upward, so later numbers sit earlier in the search list. |
|  | Lower-numbered `patchNNN.dat` | Still above the base archives. |
|  | `critter_patches` | Commonly `data\`. If the same directory is also `master_patches`, reopening moves it to this position. |
|  | `critter.dat` | Opened after the master roots. |
|  | `master_patches` | Commonly `data\`, unless it was the same root as `critter_patches` and got moved later. |
| Lowest | `master.dat` | Main base archive. |

In the default configuration, both patch directory keys usually point to `data`. The first open from `master_patches` adds it, and the second open from `critter_patches` moves the same root to the front again. The practical result is that loose files under `data\` override both `master.dat` and `critter.dat`, but numbered patch archives and `f2_res.dat` can still override loose `data\` files in CE's normal order.

Different executable builds, launchers, or mods can change which roots are opened and in what order, so archive tools should not assume this table is universal. It is the behavior visible in Fallout 2 CE's `gameDbInit`, `dbOpen`, `xbaseOpen`, and `xfileOpen` flow.

Absolute paths and paths beginning with `.`, `\`, or `/` bypass the DAT search list and are opened directly from the filesystem. Normal game asset references should therefore be relative paths.

Loose filesystem files can be gzip-compressed: CE checks the first two bytes of a plain file for gzip magic `1F 8B` and reopens it as a gzip stream. This is separate from DAT2 entry compression. DAT2 compressed entries are zlib streams inside the archive, not gzip files.

### Lookup Examples

For a relative path such as `art\critters\hmjmpsaa.frm`, CE tries the highest-precedence open root first. If `f2_res.dat` does not contain it, CE tries patch archives from highest number to lowest number, then the loose patch directory, then `critter.dat`, then `master.dat`. The first match wins.

For a text path such as `text\english\game\misc.msg`, the same database search applies. Language-specific code changes the relative path it asks for; the database layer still performs the same root search on that path.

For save-game sidecars and other explicitly absolute or dot-relative paths, the database roots are not used. Those paths are opened directly from the filesystem.

## Path Rules

- DAT2 entry lookup is case-insensitive in CE because archive entries are found with a case-insensitive binary search.
- Directory separators in documentation are normally written as backslashes. Cross-platform tools should normalize both `\` and `/` where appropriate.
- DAT2 entries must be sorted case-insensitively for CE's binary search to find them reliably.
- Older DAT2 documentation says paths are DOS 8.3. Many shipped resources are 8.3-like, but tools should not rely on that as the archive format's hard limit; the entry stores a 32-bit path length.
- File listing APIs merge archive and filesystem names, then sort and deduplicate them case-insensitively.

## DAT2 Overview

DAT2 is the Fallout 2 archive format. It has no global magic number. A reader identifies it by the footer and by successfully parsing the directory tree near the end of the file.

```text
+-----------------------------+
| File data block             |
| compressed/plain file bytes |
+-----------------------------+
| uint32 file_count           |
| directory entries           |
+-----------------------------+
| uint32 tree_size            |
| uint32 data_size            |
+-----------------------------+
```

| Part | Location | Description |
|---|---|---|
| Data block | Usually offset `0` | Raw stored bytes for every entry. |
| File count | Start of directory tree | Little-endian `uint32` count of entries. |
| Directory entries | After file count | Variable-length entry metadata. |
| Tree size | Last 8 bytes, first field | Little-endian `uint32` size of `file_count + entries`. |
| Data size | Last 4 bytes | Little-endian `uint32` size of the DAT data region. |

CE computes the start of the archive data region as:

```text
data_base = physical_file_size - data_size
```

For normal DAT2 files, `data_base` is `0`. The formula allows arbitrary leading bytes before the DAT data region, because entry offsets are relative to `data_base`, not always to the physical start of the file.

## DAT2 Footer

To locate the directory tree, read the last 8 physical bytes:

```text
tree_size = read_u32_le(file_size - 8)
data_size = read_u32_le(file_size - 4)

data_base = file_size - data_size
tree_offset = file_size - tree_size - 8
```

The first 4 bytes at `tree_offset` are `file_count`. The remaining `tree_size - 4` bytes are directory entries.

## DAT2 Entry Structure

| Field | Size | Description |
|---|---|---|
| `path_length` | 4 | Little-endian length of the path string in bytes. |
| `path` | `path_length` | Entry path bytes. No null terminator is stored in the DAT. |
| `compressed` | 1 | `1` for zlib-compressed data, `0` for plain stored data. |
| `uncompressed_size` | 4 | Size returned to callers after decompression. |
| `data_size` | 4 | Stored byte count in the data block. For compressed entries, this is the compressed zlib stream length. |
| `data_offset` | 4 | Offset of the stored bytes relative to `data_base`. |

```c
struct Dat2Entry {
    uint32_t path_length;
    char     path[path_length];
    uint8_t  compressed;
    uint32_t uncompressed_size;
    uint32_t data_size;
    uint32_t data_offset;
};
```

The physical byte range for an entry is:

```text
stored_start = data_base + data_offset
stored_end   = stored_start + data_size
```

For uncompressed entries, `data_size` should match `uncompressed_size`. For compressed entries, `data_size` is usually smaller, but a validator should still check both the stored byte range and the inflated byte count.

## DAT2 Compression

Compressed DAT2 entries are zlib streams. They are not gzip streams, even though older documentation sometimes says "gzipped". Common compressed entries begin with zlib header bytes such as `78 DA`, but a reader should trust the directory entry's `compressed` flag and use zlib inflate rather than only sniffing the first bytes.

CE reads compressed entries with a `0x400`-byte input buffer and zlib `inflate`. Stream position is tracked in uncompressed bytes. Seeking backward in a compressed entry rewinds the stream and inflates forward to the requested offset; seeking forward inflates and discards bytes until the destination is reached.

## DAT2 Text Reads

When a DAT entry is opened in text mode, CE normalizes Windows CRLF line endings to a single LF character while reading. The same logic is used for compressed and uncompressed DAT entries. This matters for text formats such as [MSG](msg.html), [LST](lst.html), and world-map text configuration files.

Text-mode seeks are restricted. CE only supports rewinding or seeking to an absolute offset; arbitrary relative/end seeks in text streams are rejected because CRLF normalization makes byte positions ambiguous.

## DAT2 Reader Algorithm

1. Open the file as binary and get its physical size.
2. Read `tree_size` and `data_size` from the last 8 bytes.
3. Set `data_base = physical_size - data_size`.
4. Seek to `physical_size - tree_size - 8`.
5. Read little-endian `file_count`.
6. Read `file_count` variable-length entries.
7. Sort-check entries case-insensitively if the archive will be used by an engine that binary-searches the directory.
8. For each entry, read from `data_base + data_offset` for `data_size` bytes.
9. If `compressed == 1`, inflate with zlib and require `uncompressed_size` output bytes.

## DAT2 Writer Notes

- Write entry paths exactly as game-relative paths, preferably with backslashes for classic-tool compatibility.
- Sort directory entries case-insensitively by path before writing.
- Use `compressed = 0` for already-compressed media when recompression is not worthwhile.
- Use zlib stream compression, not raw deflate and not gzip, when `compressed = 1`.
- Write `tree_size` as the byte length of `file_count + all directory entries`.
- Write `data_size` as the byte length from `data_base` through the 8-byte footer. For normal files with no leading prefix, this is the physical file size.
- Keep paths unique under case-insensitive comparison. Duplicate paths make lookup order ambiguous and can break binary search assumptions.

## DAT2 Validation Checklist

- `physical_size >= 8`.
- `data_size <= physical_size`.
- `tree_size >= 4` and `tree_size + 8 <= data_size`.
- `tree_offset = physical_size - tree_size - 8` is inside the file.
- All directory entries fit inside the tree.
- Every `data_base + data_offset + data_size` fits inside the physical file.
- `compressed` is `0` or `1`.
- Uncompressed entries have matching stored and real sizes.
- Compressed entries inflate successfully to `uncompressed_size`.
- Entries are sorted case-insensitively and have no duplicate paths.

## DAT1 Overview

DAT1 is the Fallout 1 archive format. It is structurally unrelated to DAT2. Metadata values are big-endian and the archive uses a directory-oriented layout followed by file data.

### DAT1 Header

| Field | Size | Description |
|---|---|---|
| `directory_count` | 4 | Number of directory records. |
| `folder_allocation_hint` | 4 | Allocation hint used by the engine. It must be at least `directory_count` or classic code can overrun its allocation. |
| `reserved` | 4 | Usually `0`. |
| `timestamp` | 4 | Unix-style creation timestamp in known archives. Read by tools, not normally validated by the game. |

### DAT1 Directory Name Block

The header is followed by `directory_count` directory names:

```text
uint8  name_length
char   name[name_length]
```

Fallout 1 `master.dat` includes a root directory named `.`. Do not discard it as padding; it contains files such as `color.pal` and fonts.

### DAT1 Directory Content Block

After the directory names, each directory has a content block:

| Field | Size | Description |
|---|---|---|
| `file_count` | 4 | Number of files in the directory. |
| `file_allocation_hint` | 4 | Allocation hint; should be at least `file_count`. |
| `fixed_metadata_size` | 4 | Usually `16`, matching the four 32-bit metadata fields in each file entry. |
| `timestamp` | 4 | Directory timestamp. |

Each file entry then contains:

| Field | Size | Description |
|---|---|---|
| `name_length` | 1 | Filename length. |
| `name` | Variable | Filename within the current directory. |
| `attributes` | 4 | `0x20` for plain data, `0x40` for LZSS-compressed data in common docs/tools. |
| `offset` | 4 | Physical offset of file data from the beginning of the DAT. |
| `size` | 4 | Uncompressed size. |
| `packed_size` | 4 | Compressed size. Usually `0` for plain entries. |

## DAT1 LZSS Blocks

DAT1 compressed entries use a Fallout LZSS block stream. This is the file-entry decompression layer, not the archive directory parser.

- The dictionary size is `4096` bytes.
- The initial dictionary is filled with spaces (`0x20`).
- The initial dictionary write index is `4096 - 18`.
- Minimum match length is `3`; maximum encoded match length is `18`.
- The stream is divided into signed 16-bit block lengths. A zero block ends the stream.
- Negative block lengths copy literal bytes directly without feeding them into the dictionary.
- Positive block lengths contain flag-controlled LZSS tokens.

The token flags are consumed least-significant bit first. If the current flag bit is set, the next input byte is literal data and is also written into the dictionary. If the bit is clear, the next two bytes encode a dictionary offset and a length nibble; `length + 3` bytes are copied from the dictionary to output and back into the dictionary. The implementation used by community tools allows copied bytes to overwrite dictionary positions that may still be read later in the same match, which is normal for LZSS-style overlap.

## Practical Modding Notes

- For editing, it is usually simpler to unpack resources into the configured patch/data directory than to rebuild `master.dat` or `critter.dat`.
- Do not confuse DAT2 zlib entry compression with gzipped loose files or gzipped save-game MAP/PRO files.
- When making a compatibility patch DAT, include only changed files and preserve their game-relative paths.
- When debugging "wrong file loaded" problems, check all open roots: loose `data\`, patch DATs, `f2_res.dat`, `master.dat`, and `critter.dat`.
- Many old tools extract everything using uppercase names. The game lookup is case-insensitive, but version control and cross-platform tooling are easier when paths are normalized consistently.

## Related Formats

- [FRM File Format](frm.html) - game art stored inside DAT archives.
- [PAL File Format](pal.html) - palettes stored as loose resources or inside DAT archives.
- [RIX File Format](rix.html) - splash images stored in `art\splash`.
- [ACM File Format](acm.html) - compressed audio stored in DAT archives or loose sound folders.
- [MSG File Format](msg.html) - text message files commonly loaded from DAT or loose `data\`.
- [MAP File Format](map.html) - map resources stored as DAT entries or loose files.
- [PRO File Format](pro.html) - prototype resources stored in `proto\` paths.

## Source References

- [Fallout 2 CE `dfile.cc`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/dfile.cc) - DAT2 footer parsing, entry loading, zlib inflate, text-mode reads, seeks, and case-insensitive entry lookup.
- [Fallout 2 CE `dfile.h`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/dfile.h) - in-memory DAT2 structures.
- [Fallout 2 CE `xfile.cc`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/xfile.cc) - resource root search order, DAT/directory opening, loose gzip detection, and file listing behavior.
- [Fallout 2 CE `db.cc`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/db.cc) - higher-level database/file API and big-endian game-data read helpers.
- [Vault-Tec Labs DAT file format](https://falloutmods.fandom.com/wiki/DAT_file_format) - classic community DAT1/DAT2 documentation and credits.
- [Fallout Wiki DAT file](https://fallout.fandom.com/wiki/DAT_file) - DAT archive overview and DAT2 community notes.
- [Kaitai Struct Fallout 2 DAT specification](https://formats.kaitai.io/fallout2_dat/) - formal DAT2 parser specification useful for cross-checking field order.
- [NMA Fallout DAT files thread](https://www.nma-fallout.com/threads/fallout-dat-files.160366/) - Shadowbird's DAT1/LZSS discussion referenced by older tools.

## Tools

- [Dat Explorer 1.43](https://fodev.net/files/mirrors/teamx-utils/dat_explorer.rar)
- [TeamX DAT utilities](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html#dat)
- [Fallout 1 DAT unpacking - C#](https://github.com/rotators/Fo1in2/blob/master/Tools/UndatUI/src/dat.cs)
- [Fallout 2 DAT reading - C#](https://github.com/rotators/tools/tree/master/DATLib)
- [Fallout 2 DAT creation - C#](https://github.com/rotators/Fo1in2/blob/master/Tools/Packrat/src/dat.cs)
- [DAT unpacking for Fallout 1 and Fallout 2 - Python](https://github.com/berenm/game-data-reverse-engineering/tree/master/python)
- [Falltergeist DAT unpacker - C++](https://github.com/falltergeist/dat-unpacker)

## Credits

Original DAT1 format notes are credited by older community documents to Shadowbird. Original DAT2 format notes are credited to MatuX. This page also incorporates behavior verified against Fallout 2 Community Edition source.

## History

2019-12-15 - Ported from [Vault-Tec Labs DAT file format](https://falloutmods.fandom.com/wiki/DAT_file_format) by [ghost](https://github.com/ghost2238).
