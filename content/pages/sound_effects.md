---
title: Sound Effect Lookup and SNDLIST.LST
output: sound_effects.html
description: Legacy SNDLIST.LST records, CE sound-cache discovery, runtime tags, gameplay filename builders, and PRO sound dependencies.
toc: auto
---

# Sound Effect Lookup and SNDLIST.LST

Fallout sound effects combine three separate mechanisms: gameplay selects a filename, the resource filesystem locates its audio, and the sound cache supplies encoded bytes and decoded-length metadata. `SNDLIST.LST` belongs to the cache layer; it does not assign sounds to weapons or gameplay events.

The primary source is the preferred `fallout2-ce/fallout2-ce` fork at revision `0290b2c98d538902235339b406291a426529fda0`. That implementation scans sound files and does not read or write `SNDLIST.LST`. To document the actual disk format, this page separately uses the older `alexbatalov/fallout2-ce` reader at `e97087b9582f37075db347a89898887320753f8b`. These are source-derived observations checked on 2026-09-12, not tests of shipped archives or every original executable/sfall version.

## Resource Lookup

The ordinary `soundPlayFile`/`soundEffectLoad` route builds `sound\sfx\<name>.ACM`. Supply a stem without the path or extension. For example, `toggle` requests `sound\sfx\toggle.ACM`; adding `.ACM` to the supplied name would append the extension twice. Script `play_sfx` uses this gameplay sound route.

[DAT and loose-file precedence](dat.html) determines which resource supplies those bytes. A conventional loose replacement is `DATA\sound\sfx\<name>.ACM`. Music and speech use different paths and playback contexts; they are not registered by this sound-effect list. See [ACM](acm.html) for decoding and channel interpretation.

Creating a file or adding a cache entry does not schedule playback. A script, animation/action handler, interface callback, or ambient-sound configuration must request its name. Conversely, a PRO sound byte is not a `SNDLIST.LST` line number.

## Legacy Disk Format

The older reader opens `SNDLIST.LST` under the sound-effects directory, normally `sound\sfx\SNDLIST.LST`. It is a count followed by four physical lines for each record:

| Line | Value |
|---|---|
| First line | Decimal number of records, N. |
| Record line 1 | Filename including extension, relative to the sound-effects directory. |
| Record line 2 | Decoded data size in bytes. |
| Record line 3 | Encoded file size in bytes. |
| Record line 4 | Stored integer tag; read but not used as the runtime lookup tag in the inspected implementation. |

An illustrative, not asset-verified, two-record file is:

```text
2
EXAMPLE1.ACM
44100
1234
0
EXAMPLE2.ACM
88200
2345
0
```

The sizes are placeholders that must be replaced with real values. This file has nine logical lines: `1 + 4*N`. It is not the usual one-filename-per-line [LST](lst.html) format. There are no blank separators, comments, section names, or CSV fields.

For scanned ACM files, the implementation computes decoded bytes as `2 * sampleCount`, using the decoder's total count of 16-bit samples. Do not multiply by channel count again. The encoded size is the ACM resource's byte length, not its compressed storage size inside a DAT archive and not a WAV export's size. The legacy uncompressed scan mode sets decoded size equal to file size for `.SND` files; this does not establish a general-purpose SND container format.

### Ordering and Parser Hazards

The older existing-file branch trusts the supplied count and sizes and does not sort its records. Lookup uses case-insensitive binary search, so records must already be sorted by filename using a compatible comparison. Avoid duplicate names, especially case-only duplicates, whose matching entry is not a stable choice.

Numeric lines use `atoi`, not a strict validating parser. The reader does not check each line-read result or adequately validate counts and allocation sizes. It reads into a 255-byte request buffer, then unconditionally removes the last character from the filename string as though it were a newline. Malformed, truncated, overlong, or newline-free filename records are unsafe or can lose their final character. Use short conventional filenames and complete newline-terminated records; an authoring tool should reject malformed input rather than reproduce these weaknesses.

The existing-file branch does not remeasure the referenced audio. Replacing an ACM with different encoded or decoded length can leave stale metadata. The cache uses encoded size for allocation and decoded size for exposed length/read bounds; stale values can cause more than an incorrect duration display. Regenerate or correctly update the list for runtimes that consume it.

If the list is absent, the older implementation scans files, measures sizes, sorts records, and attempts to write a replacement. Its scan initializes stored tags to zero; this does not prevent its own runtime lookup from working. Failure to write the generated list is logged but does not discard the successfully built in-memory list. A higher-priority or archive-resident stale list can still be found by the resource filesystem, so deleting one loose copy is not proof that scanning will occur.

## Preferred Fork Behavior

The preferred fork's `soundEffectsListInit` always enumerates files, measures sizes, and sorts the resulting records. There is no `SNDLIST.LST` read/write branch. Editing the legacy list consequently cannot add, remove, reorder, or resize cache entries in this implementation.

| Scan mode | Pattern | Decoded size |
|---|---|---|
| 0 | `*.SND` | Same as file size. |
| 1 | `*.ACM` | Twice the decoder's sample count. |

The normal cache uses mode 1. The scan accepts 1 through 10,000 enumerated files; zero files, more than 10,000, an unsupported mode, nonpositive file sizes, missing resources, or a failed ACM header initialization cause list initialization to fail. It does not silently omit the bad entry and continue. Header initialization is not full-payload validation: a corrupt later ACM block may still fail during playback.

Cache initialization failure is not automatically a game-start failure: `gameSoundInit` logs the unavailable cache and continues. When preparing effects, it chooses cache I/O if initialized and generic audio I/O otherwise. This is not an automatic per-file retry after a cache lookup fails. A newly added file may therefore remain unavailable through an already-built cache list until the audio system is reinitialized; restart when changing the sound set or replacing cached bytes.

## Runtime Tags

Both inspected implementations derive the tag from the record's current array position:

```text
tag = 2 * zeroBasedIndex + 2
index = tag / 2 - 1
```

Valid tags are positive even integers within the array bounds. For three entries, they are 2, 4, and 6. Zero, negative values, odd values, and out-of-range values are rejected. The saved fourth field does not override this calculation.

Name lookup first checks the effects-path prefix case-insensitively, then binary-searches the remaining filename case-insensitively. Path construction concatenates the registered directory and filename; callers must use the directory form expected by the subsystem, including its separator. These internal tags are not script ids, PIDs, FIDs, PRO sound characters, or stable mod-facing identifiers. Adding a filename earlier in sorted order shifts subsequent tags.

## Gameplay Filename Builders

These builders return stems, normally uppercased, for subsequent `.ACM` lookup. The shared builder buffer holds 13 bytes, so conventional eight-character names are the safest baseline. `printf` widths such as `%6s` and `%4s` are minimum field widths, not truncation limits: short input gets leading spaces, while overlong input can be truncated by the enclosing buffer. Do not treat them as space-free padding or arbitrary-length naming APIs.

| Family | Construction | Notes |
|---|---|---|
| Ambient helper | `A` + six-column name + `1` | Format `A%6s%1d`. |
| Interface helper | `N` + six-column name + `1` | Format `N%6s%1d`. Not every UI sound uses this helper. |
| Scenery helper | `S` + active/passive code + action code + four-column name + `1` | Passive code is `P`; otherwise `A`. |
| Door | `S` + action code + `DOORS` + scenery sound byte | Example: open with sound byte `A` gives `SODOORSA`. |
| Container | `I` + action code + `CNTNR` + item sound byte | Example: open with sound byte `A` gives `IOCNTNRA`. |
| Weapon | `W` + effect + weapon sound byte + variant + material + `XX1` | Eight characters with ordinary one-byte inputs. |
| Character | Art-list base + weapon/animation letters | Uses critter art and animation naming logic, with action-specific substitutions. |

Scenery action indices map to `0=O` open, `1=C` close, `2=L` lock, `3=N` unlock, and `4=U` use. A sound byte is a character: byte 65 (`0x41`) inserts `A`, not the digits `65`. A zero byte can prematurely terminate the generated C string. Verify the target PRO's intended code instead of treating any integer as a meaningful sound selection.

Some UI callbacks bypass helper construction and use literal names: the red-button press/release uses `ib1p1xx1` and `ib1lu1x1`; the toggle callback uses `toggle`. A filename prefix is therefore a convention followed by particular callers, not a universal classifier that the sound loader interprets.

### Weapon Effects

| Effect index | Letter | Meaning in the enum |
|---|---|---|
| 0 | R | Ready |
| 1 | A | Attack |
| 2 | O | Out of ammunition |
| 3 | F | Ammunition flying |
| 4 | H | Hit |

The sound character comes from the weapon-specific `soundCode` byte, obtained by `weaponGetSoundId`, not the common item `soundId`. In the documented weapon PRO layout these are separate fields: common item sound at `0x38`, weapon sound at `0x79`. Container actions use the common item byte; doors use scenery sound at `0x28`. See [PRO](pro.html).

Ready and out-of-ammo effects always use variant 1. Other effects use variant 1 for left/right primary hit modes and punch, and variant 2 for the other hit modes. This is not a random alternate-sample digit.

Material is `X` unless this is a hit with a target and the damage type is neither the configured explosion damage type, plasma, nor EMP. Otherwise target item/scenery/wall material determines the code:

| Material | Code |
|---|---|
| Glass, metal, plastic | M |
| Wood | W |
| Dirt, stone, cement | S |
| Other/default, including targets outside those prototype categories | F |

For example, attack effect `A`, weapon code `B`, primary variant 1, and generic material `X` gives `WAB1XXX1`. This illustrates construction, not proof that the sample exists. The table does not create missing ready, secondary, or impact variants for a new weapon.

### Character Sounds and Fallback

`sfxBuildCharName` obtains the object's art-list base and calls the art animation-code helper. Take-out uses the supplied weapon animation; other animations use the weapon animation encoded in the object's FID. Falling-front/back pass-out and death requests substitute `Y` and `Z`, respectively, for the weapon letter. Punch/kick contact requests also substitute `Z`. See [LST/FID resolution](lst.html) for the underlying art names.

When an object-aware effect load fails for a critter and the requested name begins with `H` or `N`, the loader tries a generic `H<second-character>XXXX<suffix-after-first-six-characters>` name. If the second character is `A`, player/critter gender selects `F` or `M`. This is a narrow character-sound fallback, not a universal missing-file search. A simple `soundPlayFile` request supplies no object, so it does not get this object-aware fallback.

Do not infer footstep/material selection from the weapon impact table. The inspected character builder and action call sites do not establish a general floor-material footstep schema. A movement-sound extension needs its own caller-specific documentation; naming an ACM after a walking animation alone does not prove the engine will request it.

## Practical Validation

1. Decide whether the target consumes the legacy list or scans resources. Do not prescribe list regeneration for the preferred fork, which ignores that file.
2. Decode custom ACM files independently. Check total samples, encoded size, channel interpretation, and payload integrity using the [ACM reference](acm.html).
3. Verify the exact generated stem, including action variant and PRO character code. Resolve the full `sound\sfx\...ACM` path through the active resource configuration.
4. For legacy lists, validate count, four-line records, ordering, uniqueness, and both sizes. Do not use stale metadata after replacing audio.
5. Restart to refresh the cache. Test primary/secondary attacks, impacts on different materials, and opening/closing separately; they can request different files.
6. Check sound enablement, volume, and the active-effect limit before diagnosing silence as a format failure. The loader can reject an effect before attempting any file lookup.

Music, speech, and ambient scheduling remain separate concerns. The list neither sets volume nor defines event probabilities. The gameplay loader reports failure when effects are disabled or cannot be prepared, and missing files normally yield no effect rather than a user-facing format error dialog. Debug output and inspection of the exact requested path are more useful than assuming every silent action is a decoder fault.

## Sources

- [Preferred fork sound_effects_list.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/sound_effects_list.cc): resource enumeration, size measurement, sorting, and runtime tags.
- [Legacy sound_effects_list.cc](https://github.com/alexbatalov/fallout2-ce/blob/e97087b9582f37075db347a89898887320753f8b/src/sound_effects_list.cc): actual `SNDLIST.LST` read/write schema and trusted metadata. Used specifically because that disk reader is absent from the preferred fork.
- [sound_effects_cache.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/sound_effects_cache.cc): list initialization, cache lookup, encoded storage, and decoded reads.
- [game_sound.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/game_sound.cc) and [game_sound.h](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/game_sound.h): filename builders, effect enums, object-aware fallback, and playback preparation.
- [item.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/item.cc): weapon-specific sound code accessor.
- [actions.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/actions.cc) and [animation.cc](https://github.com/fallout2-ce/fallout2-ce/blob/0290b2c98d538902235339b406291a426529fda0/src/animation.cc): action-triggered character sounds and animation scheduling.
