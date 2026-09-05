---
title: Elevators.ini File Format
output: elevators.html
description: Fallout 2 CE and sfall-compatible elevators.ini configuration, destination fields, image templates, scripting, and validation notes.
---

# Elevators.ini File Format

Elevator data controls the modal floor-selection window used by elevator scripts. In the original game the destination tables are hardcoded in the executable. Fallout 2 CE implements the sfall-compatible override path: `ddraw.ini` can name an external elevator config file with `[Misc] ElevatorsFile`, commonly `elevators.ini`.

This file does not create the map trigger by itself. A script still has to request an elevator, usually with `metarule(15, elevator_id)`, and the destination map ids must match the indexes from [`data\maps.txt`](worldmap_config.html).

## Format summary

| Property | Description |
|---|---|
| Typical filename | `elevators.ini`, although CE reads the path configured by `[Misc] ElevatorsFile`. |
| File type | INI-style text file parsed by the normal config parser. |
| Sections | Numeric section names: `[0]`, `[1]`, ... `[49]`. |
| Maximum elevators | 50 entries, ids `0` through `49`. |
| Levels per elevator | 2 through 4 selectable levels. The internal destination array has exactly four slots. |
| Primary consumer | `metarule(15, elevator_id)`, which queues an elevator request from script code. |

## ddraw.ini hook

Fallout 2 CE initializes `[Misc] ElevatorsFile` to an empty string. If the value remains empty, the built-in elevator tables are used. If the value is non-empty, CE reads that file and applies any sections and keys it finds over the built-in arrays.

```ini
[Misc]
ElevatorsFile=elevators.ini
```

The filename is passed directly to the config reader. For portable mods, keep it relative to the game directory unless the target runtime documents another base path.

## Section shape

```ini
[24]
Image=0
ID1=157
Elevation1=2
Tile1=15684
ID2=158
Elevation2=0
Tile2=23442
ID3=158
Elevation3=1
Tile3=23442
ID4=158
Elevation4=2
Tile4=23442
```

| Key | Description |
|---|---|
| `Image` | Copies the visual template, button count, and keyboard labels from another elevator id. The destination rows of the current section are not copied. |
| `ButtonCount` | Number of selectable buttons, clamped to `2..4`. CE only reads this for custom ids `24` and higher, and an `Image` key can later replace the level count by copying the template's count. |
| `MainFrm` | Interface FRM id for the elevator background. The engine resolves it as `art\intrface` art through a normal interface FID. |
| `ButtonsFrm` | Optional interface FRM id for the lower panel/buttons overlay. Use `-1` for no panel in CE-style data. |
| `ID1` .. `ID4` | Destination map index for each button. These are `maps.txt` indexes, not MAP filenames. |
| `Elevation1` .. `Elevation4` | Destination elevation for each button. Normal map elevations are `0`, `1`, and `2`. |
| `Tile1` .. `Tile4` | Destination hex tile for each button. |

Missing keys leave the previous value in place. For ids `0` through `23`, that previous value is the built-in elevator data. For ids `24` through `49`, uninitialized values are usually zero or unusable unless the section supplies a template and destinations.

## Parser behavior

The file uses the same parser behavior as other CE config files. See [CFG/INI Configuration Files](cfg.html) for the shared details.

| Feature | Behavior |
|---|---|
| Comments | A semicolon starts a comment. Everything after it on the line is ignored. |
| Case | Section and key lookup is case-insensitive in CE's config dictionary. |
| Duplicates | Later duplicate keys replace earlier values while loading. |
| Integer parsing | Integer keys are read with the normal config integer reader. |
| Section discovery | CE loops over all possible section names from `0` through `49`; sections do not need to be contiguous. |

## Built-in elevator ids

Fallout 2 CE has built-in definitions for ids `0` through `23`. Ids `24` through `49` are available for custom elevator definitions in the sfall-compatible table.

| Id | Built-in name |
|---|---|
| `0` | Brotherhood of Steel main |
| `1` | Brotherhood of Steel surface |
| `2` | Master upper |
| `3` | Master lower |
| `4` | Military Base upper |
| `5` | Military Base lower |
| `6` | Glow upper |
| `7` | Glow lower |
| `8` | Vault 13 |
| `9` | Necropolis |
| `10` | Sierra 1 |
| `11` | Sierra 2 |
| `12` | Sierra service |
| `13` | Klamath toxic caves |
| `14` | Elevator 14 |
| `15` | Vault City |
| `16` | Vault 15 main |
| `17` | Vault 15 surface |
| `18` | Navarro northern |
| `19` | Navarro center |
| `20` | Navarro lab |
| `21` | Navarro canteen |
| `22` | San Francisco Shi temple |
| `23` | Redding Wanamingo mine |

The built-in ids exist even without an external file. A config file can override their destination rows and art keys, but CE does not read `ButtonCount` for ids below `24`.

## Image templates and FRMs

The elevator window always loads three shared interface FRMs: button-down id `141`, button-up id `142`, and gauge id `149`. The per-elevator background comes from `MainFrm` or the selected `Image` template. The optional panel comes from `ButtonsFrm` or the template.

`Image` is a template id, not a destination map id. CE applies it after reading the other section keys. When `Image=N` is present and `N` differs from the current section id, CE copies these fields from elevator `N` to the current id:

- Background FRM id.
- Panel FRM id.
- Number of levels.
- Keyboard labels for level selection.

Destination maps, elevations, and tiles remain those of the current section. This is why most custom examples use `Image` for the UI shape and then provide their own `ID*`, `Elevation*`, and `Tile*` rows.

## Script activation

Vanilla-style scripts request elevators through metarule `15`:

```text
procedure spatial_p_proc begin
   if (source_obj == dude_obj) then begin
      metarule(15, 24);
   end
end
```

The metarule queues a request rather than moving the player immediately. When the script system processes the request, it opens the elevator window, lets the player choose a level, and then moves the player or sets a map transition.

CE's request helper also searches near the triggering object for the elevator-stub scenery PID `0x0200050D`. If a stub is found within the search area, the helper uses the stub's scenery elevator payload as the requested elevator type and starting level. If no stub is found, it uses the metarule argument as the elevator type and the current map elevation as the starting level.

## PRO and MAP relationship

An elevator scenery [PRO](pro.html) stores two subtype fields: elevator type and elevator level. The elevator type selects the row in the built-in or external elevator table. The elevator level is the current/start level used by the UI gauge logic.

The external config's `ID*` fields point at map indexes. Those indexes come from `data\maps.txt`, and the target MAP file is resolved through that table. The target tile and elevation are applied after the user chooses a button.

| Target situation | Runtime behavior |
|---|---|
| Target map is the current map and target elevation is current elevation | Clears the player's current animation, faces southeast, and attempts to place the player at the target tile. |
| Target map is current map but target elevation differs | Places the player on the target elevation/tile and tries to close nearby old elevator doors. |
| Target map differs | Closes nearby old elevator doors if found, then sets a map transition with target map, elevation, tile, and southeast rotation. |

The runtime door-closing helper looks for a few hardcoded scenery PIDs near the player. This is visual cleanup for known elevator door art, not part of the config file schema.

## Current-level quirks

The elevator window tries to infer the currently selected button from the current map and elevation before drawing the gauge. This is straightforward for many same-map elevators, but some built-in elevator types have special-case adjustment code for Sierra and Military Base layouts. For custom elevators, keep the level ordering simple and test the initial gauge position from each possible source map.

Keyboard selection uses the built-in label table for the chosen elevator/template. For example, some templates use `1`, `2`, `3`, `4`, while another uses `G` and `1`. CE's current external file does not expose custom label strings directly; choosing the right `Image` template is the practical way to inherit suitable labels and key bindings.

## Editing notes

- Use ids `24` through `49` for new elevators when compatibility with built-in ids matters.
- Use `Image` when you want to inherit an existing two-, three-, or four-button UI template.
- If `Image` is present, do not rely on `ButtonCount` to survive; the copied template's level count wins in CE.
- Every active button needs matching `ID*`, `Elevation*`, and `Tile*` values.
- Map ids are numeric indexes from `maps.txt`. Reordering maps can silently retarget elevator destinations.
- Elevations are zero-based engine elevations, not mapper UI labels.
- Tile numbers should be valid hex tiles and should be tested for player placement, blocking objects, and nearby door art.
- Test both click selection and keyboard shortcuts for the chosen template.

## Validation checklist

- `[Misc] ElevatorsFile` names the intended file and is not commented out.
- Every section name is a numeric id from `0` through `49`.
- Custom ids have a valid template or explicit art and level data.
- Every destination map id exists in `maps.txt`.
- Every destination elevation exists in the target map.
- Every destination tile is inside the map hex grid and is suitable for placing the player.
- The spatial or object script calls `metarule(15, elevator_id)` only when the player should activate the elevator.
- If an elevator stub is placed, its PRO type/level values match the intended table entry.
- Same-map and cross-map travel both close or ignore nearby door art acceptably.

## Source-backed function map

| Function | Relationship to elevator data |
|---|---|
| `elevatorsInit` | Reads `[Misc] ElevatorsFile`, loads the external config, applies section keys, and copies `Image` templates. |
| `elevatorSelectLevel` | Opens the modal elevator window, determines the starting gauge level, handles button/keyboard selection, and returns destination map/elevation/tile. |
| `elevatorWindowInit` | Loads interface FRMs, creates buttons, and draws the background/panel. |
| `elevatorGetLevelFromKeyCode` | Maps keyboard input through the selected elevator's label table. |
| `opMetarule` | Handles `METARULE_ELEVATOR` value `15` and calls `scriptsRequestElevator`. |
| `scriptsRequestElevator` | Finds a nearby elevator stub when present, stores the requested type/level, and queues the elevator request. |
| `scriptsHandleRequests` | Processes the queued request, calls the elevator UI, places the player, or sets a map transition. |

## Source references

- [Fallout 2 CE `elevator.cc`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/elevator.cc) - built-in elevator tables, external config loading, image/template copying, UI behavior, and level selection.
- [Fallout 2 CE `elevator.h`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/elevator.h) - built-in elevator id enum.
- [Fallout 2 CE `scripts.cc`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/scripts.cc) - elevator script request and transition handling.
- [Fallout 2 CE `interpreter_extra.cc`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/interpreter_extra.cc) - `METARULE_ELEVATOR` value `15`.
- [Fallout 2 CE `proto_types.h`](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/proto_types.h) - scenery elevator subtype and payload fields.
- [NMA custom elevator discussion](https://www.nma-fallout.com/threads/cant-use-custom-made-elevator-from-elevator-ini-in-sfall-moddiers-folder.219495/) - practical sfall examples using `Image`, `ID*`, `Elevation*`, `Tile*`, and `metarule(15, X)`.

## History

2026-05-08 - Documented sfall/CE elevator configuration with source-backed behavior by [OpenAI](https://github.com/OpenAI)
