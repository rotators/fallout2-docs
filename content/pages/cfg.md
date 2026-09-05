---
title: CFG/INI Configuration Files
output: cfg.html
description: Fallout and Fallout 2 CFG and INI parser behavior, fallout2.cfg, mapper2.cfg, ddraw.ini, f2_res.ini, movie CFG sidecars, and cross-format dependencies.
toc: auto
---

# CFG/INI Configuration Files

Fallout and Fallout 2 use several INI-style configuration files. Some are global runtime settings, such as `fallout2.cfg`. Others are extension configs, such as sfall's `ddraw.ini` and the high-resolution patch's `f2_res.ini`. A few resource-side files, such as movie `.cfg` files, reuse the same parser for local data.

This page documents the shared parser behavior, the main game configuration keys, and the most important extension/config side effects that influence file lookup, language paths, subtitles, palettes, music, movies, scripts, and mod compatibility.

## Config Files

| File | Typical location | Role |
|---|---|---|
| `fallout2.cfg` | Game directory. | Main game settings: resource paths, language, preferences, sound, debug, and saved options-menu state. |
| `mapper2.cfg` | Mapper directory. | Mapper variant of the main game config. Uses the same parser plus `[mapper]` keys. |
| `ddraw.ini` | Game directory. | sfall configuration. CE reads a compatibility subset; classic sfall supports many more options. |
| `f2_res.ini` | Game directory or high-resolution patch config location. | Resolution, fullscreen/windowed behavior, movie/static-screen scaling, and high-resolution patch options. |
| `art\cuts\*.cfg` | Beside matching [MVE](mve.html) movie files, loose or inside DAT. | Movie palette fade effects keyed by movie frame number. |
| Data configs | `data\*.txt` and related paths. | Files such as [AI.TXT](ai_txt.html) and [worldmap.txt](worldmap_config.html) use the same broad INI/config style, but are documented separately because their schemas are game-data formats. |

## Parser Rules

Fallout 2 CE's `config.cc` is a useful reference for the classic parser behavior.

| Feature | Behavior |
|---|---|
| Line length | CE reads config lines with a 256-byte buffer. Keep lines shorter for compatibility. |
| Sections | A section line starts with `[` after leading whitespace and ends at the next `]`. |
| Key/value | Entries are `key=value`. The first equals sign separates key and value. |
| Whitespace | Leading and trailing whitespace around section names, keys, and values is trimmed. |
| Comments | A semicolon `;` truncates the rest of the line before parsing. |
| Quotes | There is no quote or escape syntax. Quotes become literal characters, and semicolons cannot be preserved in values. |
| Missing section | A key/value before any section is stored under the parser's default last-section name, `unknown`. |
| Case | Section and key lookup is case-insensitive in CE's dictionary implementation. |
| Duplicates | Setting the same section/key again replaces the previous value. Practically, the later value wins. |
| Integers | CE uses `strtol` for normal integer reads and intentionally tolerates trailing nonnumeric characters for compatibility with old data such as percentages. |
| Booleans | Boolean settings are integer-backed. `0` is false, any nonzero value is true. |
| Lists | Some call sites read comma-separated integer lists with `configGetIntList`. |

Command-line overrides are also parsed by the same config code. Arguments of the form `[section]key=value` are merged after the file has been loaded, so they override file contents.

```text
fallout2-ce.exe [system]language=german [preferences]subtitles=1
```

## Load Order

1. sfall config initializes first with built-in defaults.
2. `ddraw.ini` is loaded from the executable directory, if present.
3. Command-line `[section]key=value` overrides are merged into the sfall config.
4. The main game config initializes with built-in defaults.
5. If `ddraw.ini` has `[Misc] ConfigFile`, CE can use that filename instead of `fallout2.cfg` for the game config.
6. `fallout2.cfg` or `mapper2.cfg` is loaded, if present.
7. Command-line overrides are merged into the game config.
8. Configured resource paths are normalized and resolved for the current platform.

This means defaults are always present even when the file is missing. The file changes defaults, and command-line overrides win over both.

## fallout2.cfg Sections

The main game config uses five major sections in Fallout 2 CE: `[system]`, `[preferences]`, `[sound]`, `[debug]`, and for mapper mode `[mapper]`. Classic files may include only `[system]` and `[sound]` immediately after installation; the game can write the expanded set later.

### [system]

| Key | CE default | Meaning |
|---|---|---|
| `executable` | `game` | Identifies game or mapper mode in classic configs. |
| `master_dat` | `master.dat` | Main archive path. Case and spelling matter on case-sensitive platforms. |
| `master_patches` | `data` | Loose/patch resource root for master data. |
| `critter_dat` | `critter.dat` | Critter archive path. |
| `critter_patches` | `data` | Loose/patch resource root for critter data. |
| `language` | `english` | Language folder used for text, localized art/movie lookup, keyboard layout choices, and message paths. |
| `scroll_lock` | `0` | Scrolling/input behavior flag. |
| `interrupt_walk` | `1` | Whether input can interrupt walking. |
| `art_cache_size` | `8` | Art cache size in megabytes in CE's art cache initialization. |
| `color_cycling` | `1` | Enables animated palette cycling. See [PAL/COL](pal.html). |
| `cycle_speed_factor` | `1` | Multiplier applied to color-cycling periods. |
| `hashing` | `1` | Legacy/system behavior flag retained by the config. |
| `splash` | `0` | Startup splash index. CE increments this after showing a splash. |
| `free_space` | `20480` | Legacy free-space threshold setting. |
| `times_run` | Legacy | Known from classic configs; not part of CE's current settings struct flow. |

### [preferences]

| Key | CE default | Meaning |
|---|---|---|
| `game_difficulty` | `1` | `0` easy, `1` normal, `2` hard. |
| `combat_difficulty` | `1` | `0` easy, `1` normal, `2` hard. |
| `violence_level` | `3` | `0` none, `1` minimal, `2` normal, `3` maximum blood. |
| `target_highlight` | `2` | `0` off, `1` on, `2` targeting only. |
| `item_highlight` | `1` | Highlight items when requested. |
| `combat_looks` | `0` | Combat look/inspect preference. |
| `combat_messages` | `1` | Combat message display. |
| `combat_taunts` | `1` | Combat taunt/message behavior. |
| `language_filter` | `0` | Profanity/language filter preference. |
| `running` | `0` | Default movement preference. |
| `subtitles` | `0` | Enables movie subtitles and ending narration subtitles where supported. See [SVE](sve.html). |
| `combat_speed` | `0` | Combat animation speed preference. |
| `player_speed` | `0` | Classic key name for player speedup. |
| `player_speedup` | CE struct name | CE stores this setting internally as `player_speedup`, while the game-config key is `player_speed`. |
| `text_base_delay` | `3.5` | Base delay for floating/dialog text timing. |
| `text_line_delay` | `1.399994` | Per-line text delay. Older public docs often round this differently. |
| `brightness` | `1.0` | Brightness/gamma value applied through the color system. |
| `mouse_sensitivity` | `1.0` | Mouse sensitivity preference. |
| `running_burning_guy` | `1` | CE-tracked preference for the burning-guy running behavior. |

### [sound]

| Key | CE default | Meaning |
|---|---|---|
| `initialize` | `1` | Whether sound initializes. |
| `device` | `-1` | Legacy sound device selector. |
| `port` | `-1` | Legacy DOS sound port setting. |
| `irq` | `-1` | Legacy DOS sound IRQ setting. |
| `dma` | `-1` | Legacy DOS sound DMA setting. |
| `sounds` | `1` | Sound effects enabled. |
| `music` | `1` | Music enabled. |
| `speech` | `1` | Speech enabled. |
| `master_volume` | `22281` | Master volume. Classic docs describe volume values in the `0..32767` range. |
| `music_volume` | `22281` | Music volume. |
| `sndfx_volume` | `22281` | Sound-effects volume. |
| `speech_volume` | `22281` | Speech volume. |
| `cache_size` | `448` | Sound cache size setting. |
| `music_path1` | `sound\music\` | Primary music path. CE may switch this to `data\sound\music\` if it detects ACM files there. |
| `music_path2` | `sound\music\` | Secondary music path. |
| `debug` | CE struct | Sound debug flag in CE settings. |
| `debug_sfxc` | CE struct | Sound-effect-cache debug flag in CE settings. |

### [debug]

| Key | CE default | Meaning |
|---|---|---|
| `mode` | `environment` | Debug output mode. CE recognizes values such as `environment`, `screen`, `log`, `mono`, and `gnw`. |
| `show_tile_num` | `0` | Debug tile-number display flag. |
| `show_script_messages` | `0` | Debug script-message display flag. |
| `show_load_info` | `0` | Debug load-info flag. |
| `output_map_data_info` | `0` | Debug map-data output flag. |

### [mapper]

`mapper2.cfg` uses the same system/preferences/sound/debug structure, but mapper mode also initializes mapper-specific keys.

| Key | CE default | Meaning |
|---|---|---|
| `override_librarian` | `0` | Mapper librarian override. |
| `librarian` | `0` | Mapper librarian mode. |
| `use_art_not_protos` | `0` | Mapper asset/prototype behavior. |
| `rebuild_protos` | `0` | Rebuild prototype data. |
| `fix_map_objects` | `0` | Map object repair option. |
| `fix_map_inventory` | `0` | Map inventory repair option. |
| `ignore_rebuild_errors` | `0` | Ignore rebuild errors. |
| `show_pid_numbers` | `0` | Show PID numbers in mapper UI. |
| `save_text_maps` | `0` | Save text-map output. |
| `run_mapper_as_game` | `0` | Run mapper as game. |
| `default_f8_as_game` | `1` | F8 behavior default. |
| `sort_script_list` | `0` | Sort script list behavior. |

## Language And Path Effects

The `[system] language` value is one of the highest-impact config settings. It participates in:

- [MSG](msg.html) loading through `text\<language>\...`.
- [SVE](sve.html) movie subtitles through `text\<language>\cuts`.
- [MVE](mve.html) lookup, where non-English installations try `art\<language>\cuts` before `art\cuts`.
- Localized art lookup, where the art loader can try `art\<language>\...` before the base art path.
- Keyboard layout selection for French, German, Italian, and Spanish in CE.

The `master_dat`, `master_patches`, `critter_dat`, and `critter_patches` keys control the archive and loose-resource roots that feed the database layer. See [DAT File Format](dat.html) for the effective resource loading and override order. On case-sensitive platforms, the configured names must match the actual asset names.

## ddraw.ini

`[Misc] BooksFile` selects the loose [skill-book configuration](books.html), including custom PID, skill, and `proto.msg` reading-message mappings. sfall and CE differ in duplicate handling and empty replacement behavior; see that page before overriding vanilla books.

`ddraw.ini` is sfall's main configuration file. Full sfall has many settings; Fallout 2 CE currently initializes and consumes a compatibility subset, mostly under `[Misc]` and `[Scripts]`.

| Section | Keys recognized by CE source |
|---|---|
| `[Misc]` | `MaleDefaultModel`, `FemaleDefaultModel`, `MaleStartModel`, `FemaleStartModel`, `StartYear`, `StartMonth`, `StartDay`, `MainMenuBigFontColour`, `MainMenuCreditsOffsetX`, `MainMenuCreditsOffsetY`, `MainMenuFontColour`, `MainMenuOffsetX`, `MainMenuOffsetY`, `SkipOpeningMovies`, `StartingMap`, `KarmaFRMs`, `KarmaPoints`, `DisplayKarmaChanges`, `OverrideCriticalTable`, `OverrideCriticalFile`, `RemoveCriticalTimelimits`, `BooksFile`, `ElevatorsFile`, `ConsoleOutputPath`, `PremadePaths`, `PremadeFIDs`, `ComputeSprayMod`, `ComputeSpray_CenterMult`, `ComputeSpray_CenterDiv`, `ComputeSpray_TargetMult`, `ComputeSpray_TargetDiv`, `Dynamite_DmgMin`, `Dynamite_DmgMax`, `PlasticExplosive_DmgMin`, `PlasticExplosive_DmgMax`, `ExplosionsEmitLight`, `MovieTimer_artimer1`, `MovieTimer_artimer2`, `MovieTimer_artimer3`, `MovieTimer_artimer4`, `CityRepsList`, `UnarmedFile`, `DamageFormula`, `BonusHtHDamageFix`, `DisplayBonusDamage`, `Lockpick`, `Steal`, `Traps`, `FirstAid`, `Doctor`, `Science`, `Repair`, `ScienceOnCritters`, `DialogueFix`, `TweaksFile`, `DialogGenderWords`, `TownMapHotkeysFix`, `ExtraGameMsgFileList`, `NumbersInDialogue`, `AutoQuickSave`, `VersionString`, `ConfigFile`, `PatchFile`, `PipBoyAvailableAtGameStart`. |
| `[Scripts]` | `IniConfigFolder`, `GlobalScriptPaths`. |

Important integration points:

- `[Misc] ConfigFile` can change the main game config filename that CE loads instead of `fallout2.cfg`.
- `[Misc] PatchFile` can change the patch archive filename pattern used when opening numbered patch DAT files.
- `[Misc] OverrideCriticalTable`, `OverrideCriticalFile`, and `RemoveCriticalTimelimits` control [critical hit table](criticals.html) corrections, external override files, and early-game critical time gates.
- `[Misc] ElevatorsFile` names the optional [elevator configuration file](elevators.html) used to override or extend hardcoded elevator destination tables.
- `[Misc] SkipOpeningMovies` is checked before showing startup splash/opening movie behavior.
- `[Scripts] GlobalScriptPaths` is a comma-separated list of file patterns. Matching scripts are sorted and loaded as sfall global scripts.
- `[Scripts] IniConfigFolder` changes where sfall INI script helpers look for non-system INI files.

## sfall INI Script Helpers

sfall-style scripts can access INI files through triplets of the form:

```text
file.ini|Section|Key
```

CE's helper parser limits the filename chunk to 63 characters and the section chunk to 32 characters. `ddraw.ini` and `f2_res.ini` are treated as system config names and are read from the current directory instead of being forced through `IniConfigFolder`.

| Helper/opcode | Behavior |
|---|---|
| `get_ini_setting` | Reads an integer via `file|section|key`. CE returns `-1` only if the triplet cannot be parsed; missing loaded values otherwise pass through helper defaults. |
| `get_ini_string` | Reads a string via `file|section|key`. Missing values return an empty string when the triplet is valid. |
| `set_ini_setting` | Writes an integer or string value to the selected INI file in sfall-style runtimes that expose the setter. |

## f2_res.ini

`f2_res.ini` belongs to the high-resolution patch and modern-compatible runtimes that retain its config convention. Fallout 2 CE's README documents the essential `[MAIN]` keys:

```ini
[MAIN]
SCR_WIDTH=1280
SCR_HEIGHT=720
WINDOWED=1
```

High-resolution patch documentation describes additional sections such as `[MAIN]`, `[EFFECTS]`, `[HI_RES_PANEL]`, `[MOVIES]`, `[MAPS]`, `[IFACE]`, `[MAINMENU]`, `[STATIC_SCREENS]`, and `[OTHER_SETTINGS]`. CE currently also reads `[STATIC_SCREENS] SPLASH_SCRN_SIZE` when displaying RIX splash screens: `0` keeps original size, `1` scales while preserving aspect ratio, and `2` stretches to fill the screen.

Because `f2_res.ini` is used by multiple patches and runtime layers, treat it as an extension config rather than a single stable game-data schema. Document the target patch/runtime version whenever a mod requires specific keys.

## Movie CFG Sidecars

When an [MVE](mve.html) movie is played, CE tries to load a same-basename `.cfg` file through the database layer:

```text
art\cuts\intro.mve
art\cuts\intro.cfg
```

The sidecar describes palette fade effects by movie frame number.

```ini
[info]
total_effects=2
effect_frames=1,120

[1]
fade_type=in
fade_color=0,0,0
fade_steps=30

[120]
fade_type=out
fade_color=0,0,0
fade_steps=30
```

| Key | Meaning |
|---|---|
| `[info] total_effects` | Number of effect frames to read. |
| `[info] effect_frames` | One integer frame or a comma-separated list of frames. |
| `[frame] fade_type` | `in` or `out`. |
| `[frame] fade_color` | Three comma-separated color components. Movie effect code stores them as bytes and treats them as palette component values. |
| `[frame] fade_steps` | Number of movie frames over which the fade runs. |

Movie CFG files are optional. Missing or incomplete sidecars do not prevent the movie from playing; they only omit the palette fade effect.

## Inter-File Dependencies

| Config setting | Affected documentation areas |
|---|---|
| `[system] language` | [MSG](msg.html), [SVE](sve.html), [MVE](mve.html), art lookup, keyboard layout. |
| `[system] master_dat` / `master_patches` | [DAT](dat.html), all loose resource overrides, message/art/proto/map loading. |
| `[system] color_cycling` / `cycle_speed_factor` | [PAL/COL](pal.html) animated palette ranges. |
| `[preferences] subtitles` | [SVE](sve.html), [MVE](mve.html), endgame narrator subtitles. |
| `[sound] music_path1` / `music_path2` | [ACM](acm.html) music lookup. |
| `[Scripts] GlobalScriptPaths` | [SSL](ssl.html), [INT](int.html), sfall global scripts. |
| `art\cuts\*.cfg` | [MVE](mve.html) movie palette effects and [PAL/COL](pal.html) fades. |

## Implementation Notes

- Use a real INI/config parser matching Fallout's rules when possible. Generic INI parsers often preserve quotes, support escapes, or treat comments differently.
- Keep semicolons out of values; the engine parser truncates the line at the first semicolon.
- Keep config lines short. CE uses 256-byte buffers, and old tools may be stricter.
- When writing config files, preserve unknown keys when possible. sfall and high-resolution patch configs often contain keys that a specific runtime does not currently consume.
- Record the target runtime: vanilla Fallout 2, mapper, sfall version, high-resolution patch version, Fallout 2 CE, or a total-conversion fork.
- For portable installs, prefer relative resource paths that match the runtime's working directory assumptions.
- On Linux, macOS, Android, and iOS, case sensitivity can make old Windows-era path values fail if they do not match actual asset casing.

## Source Code Map

| Source | Relevant behavior |
|---|---|
| Fallout 2 CE `config.cc` | Shared parser, comments, trimming, integer/list reads, command-line overrides, config write format. |
| Fallout 2 CE `dictionary.cc` | Case-insensitive section/key lookup and replacement behavior. |
| Fallout 2 CE `game_config.cc` | Main config defaults, `fallout2.cfg`/`mapper2.cfg` path resolution, load order, save behavior. |
| Fallout 2 CE `settings.h` / `settings.cc` | Runtime settings structs and mapping from config keys to engine settings. |
| Fallout 2 CE `sfall_config.cc` | `ddraw.ini` defaults and load order. |
| Fallout 2 CE `sfall_ini.cc` | sfall INI helper triplet parsing, file/section length limits, and `IniConfigFolder` behavior. |
| Fallout 2 CE `movie_effect.cc` | Movie `.cfg` sidecar syntax and palette fade behavior. |

## References

- [CFG File Format - The Fallout Wiki](https://fallout.wiki/wiki/CFG_File_Format_%28settings%29)
- [CFG File Format - Vault-Tec Labs](https://falloutmods.fandom.com/wiki/CFG_File_Format_%28settings%29)
- [Fallout 2 CE README configuration notes](https://github.com/fallout2-ce/fallout2-ce/blob/main/README.md)
- [Fallout 2 CE config.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/config.cc)
- [Fallout 2 CE game_config.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/game_config.cc)
- [Fallout 2 CE settings.h](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/settings.h)
- [Fallout 2 CE sfall_config.cc](https://github.com/fallout2-ce/fallout2-ce/blob/main/src/sfall_config.cc)
- [sfall INI settings documentation](https://fakelshub.github.io/sfall-wiki/ini-settings/)
- [Fallout 2 High Resolution Patch notes](https://falloutmods.fandom.com/wiki/Fallout_2_High_Resolution_Patch)

## History

2026-05-07 - Added CFG/INI documentation covering parser behavior, `fallout2.cfg`, `mapper2.cfg`, `ddraw.ini`, `f2_res.ini`, movie sidecar `.cfg` files, and cross-format dependencies.
