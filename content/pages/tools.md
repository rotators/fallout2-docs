---
title: Fallout 1 & 2 Tools
output: tools.html
description: Fallout 1 and 2 tools by file format and task: archives, prototypes, scripting, maps, graphics, fonts, audio, lip sync, video, saves, and developer libraries.
toc: auto
---

# Fallout 1 & 2 Tools

A task-oriented directory of Fallout 1 and Fallout 2 editors, viewers, converters, and developer tools. Start with the format or job below, then compare the tools in that category.

Project descriptions and selected upstream documentation were reviewed on **2026-09-12**. This is a coverage audit, not a hands-on compatibility test of every executable. Repository links lead to source and project documentation; use their release/download instructions for binaries. **Legacy** identifies older utilities retained for a specific workflow, not a claim that they no longer work. This directory cannot guarantee discovery of every community tool.

## Choose by format or task

| Format or task | Where to start | What to check |
|---|---|---|
| [DAT](dat.html): extract or package resources | [Archives](#archives): DAT Explorer II, dat-unpacker, fo2dat | Fallout 1 DAT1 and Fallout 2 DAT2 support; packing versus extraction. |
| [PRO](pro.html), critters and items | [Prototypes and critters](#critters): ProtoManager, F2wedit, Critters Editor | Supported object types and game version; [LST](lst.html) and [MSG](msg.html) references. |
| [SSL](ssl.html), [INT](int.html), dialogue | [Scripting](#scripting): sfall Script Editor, BGforge MLS, SSLC, int2ssl | Compiler/runtime compatibility; decompiled text is not the original source. |
| [MAP](map.html), elevators | [Mapping](#mapping): BIS Mapper, Dims Mapper, Gecko | Game format and editor-specific setup. |
| [Worldmap configuration](worldmap_config.html), [MSK](msk.html) | [Worldmap and masks](#worldmap-and-masks) | Encounter browsing, image tiling, and walk masks are different jobs. |
| [FRM](frm.html), FR0–FR5, [RIX](rix.html), [PAL](pal.html) | [Graphics](#graphics): FRM Workshop, frm2png, BGforge MLS, palette utilities | Palette indices, frame offsets, directions, and export format. |
| [AAF](aaf.html), [FON](fon.html) | [Fonts](#fonts): font editors and converters | AAF and FON are distinct formats. |
| [ACM](acm.html), WAV, [LIP](lip.html) | [Audio](#audio): FFmpeg, snd2acm, LIP Editor, VOCK | Decoding, encoding, and lip synchronization need different tools. |
| [MVE](mve.html) | [Video](#video): FFmpeg, avi2mve, Interplay tools | Playback/export support does not imply MVE encoding. |
| [GCD](gcd.html): premade characters | [Character templates](#character-templates): CGCD | Creates GCD from a text definition; separate from save editing. |
| [SAVE.DAT](savegame.html), character and inventory edits | [Saves](#saves): F12se, fallout-se | Save format, mod-specific data, and supported fields. |
| [MSG](msg.html), [LST](lst.html), [GAM](gam.html), [BIO](bio.html), [CFG/INI](cfg.html), TXT configuration | [Text and configuration](#text-and-configuration) | Encoding, identifiers, list order, and syntax. |
| Runtime state, executable research, parser development | [Debugging](#debugging) and [Libraries](#libraries) | Runtime/version dependencies; code libraries are not standalone editors. |

## Archives

Related format: [DAT](dat.html).

| Tool | Use and interface | Compatibility / limits | Project or download |
|---|---|---|---|
| DAT Explorer II | GUI archive browsing, search, extraction, and packing. | Fallout 1 and Fallout 2; Fallout 1 compression is unsupported. | [Source and limitations](https://github.com/rotators/fallout-tools/tree/master/DatExplorer-II), [NMA download](https://www.nma-fallout.com/resources/fallout-dat-explorer-ii.140/) |
| Dat Explorer 1.43 — Dims | Legacy GUI packer/unpacker. | Retained for older workflows; DAT Explorer II describes itself as its replacement. | [Mirror download](https://fodev.net/files/mirrors/teamx-utils/dat_explorer.rar) |
| dat-unpacker — Falltergeist | Command-line extraction, including optional lowercase filenames. | Explicit DAT1/DAT2 selection. The documented usage covers extraction; do not assume packing from the repository tagline. | [Source and usage](https://github.com/falltergeist/dat-unpacker) |
| fo2dat — Adam Kewley | Command-line DAT2 archive extractor/creator. | Fallout 2; upstream repository is archived. | [Source and usage](https://github.com/adamkewley/fo2dat) |
| F1Undat-UI | GUI Fallout 1 archive extraction. | Legacy, archived repository; useful for DAT1-specific extraction. | [Source](https://github.com/FalloutTeamX/F1Undat-UI) |

## Scripting

Related formats: [SSL](ssl.html), [INT](int.html), [MSG](msg.html), [SCRIPTS.LST](scripts_lst.html).

| Tool | Use and interface | Compatibility / limits | Project or download |
|---|---|---|---|
| sfall Script Editor — phobos2077 and contributors | GUI SSL editing, completion, function hints, associated MSG preview, and external compiler integration. | Fallout 2/sfall workflow; configure the compiler and headers for the target project. | [Original project](https://github.com/phobos2077/sfall_script_editor), [rotators source collection](https://github.com/rotators/fallout-tools) |
| BGforge MLS | Editor integration for SSL completion, navigation, diagnostics, compilation, and visual dialogue editing. | VS Code extension and language-server integrations; consult editor-specific setup. | [Project and installation](https://github.com/BGforgeNet/BGforge-MLS) |
| sfall SSLC | Command-line SSL-to-INT compiler with preprocessing and optimization. | Select syntax/options for the target runtime; sfall-only functions require sfall. | [Source](https://github.com/sfall-team/sslc), [compiler documentation](https://sfall-team.github.io/sfall/sslc/) |
| int2ssl — Anchorite / sfall updates | Recover readable script text from INT bytecode. | Updated version is included in the sfall modderspack; comments and original source structure are not recovered. | [Distribution and opcode support](https://sfall-team.github.io/sfall/sslc/#int2ssl-note) |
| ReDefine — rotators | Batch script-source transformations. | Developer workflow for mass editing; review generated changes. | [Project](https://github.com/rotators/ReDefine) |
| BIS compiler — Interplay | Legacy official Fallout 2 script compilation. | Useful for reproducing old build environments. | [TeamX compiler collection](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) |

<a id="critters"></a>

## Prototypes and critters

Related format: [PRO](pro.html). Prototype edits also depend on [LST](lst.html) and [MSG](msg.html) identifiers.

| Tool | Use | Source / availability |
|---|---|---|
| Fallout ProtoManager | Edit item/critter PRO files and AI packets; export selected parameters to CSV. The NMA download is an unofficial build based on Mr.Stalin's source. Support described here is for items and critters, not every PRO type. | [Resource listing](https://www.nma-fallout.com/resources/fallout-protomanager.126/), [source collection](https://github.com/rotators/fallout-tools) |
| [F2wedit — Cubik2k](https://www.nma-fallout.com/resources/f2wedit.99/) | Create/edit item PRO files for Fallout 1 and Fallout 2. Uses the game DAT files and executable. | [Author's release thread](https://www.nma-fallout.com/threads/f2wedit.215841/); resource updated March 2026. |
| Prototype Editor — KIA | Legacy Fallout 2 PRO editor. | [TeamX listing](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) |

The original specialist critter editor remains available:

| Tool | Description | Author | Links |
|---|---|---|---|
| [Fallout 1/2 Critters Editor v.1.4.6.11](https://fodev.net/files/archive/fo2/FO12_critters_editor.zip) | Simple editor for critters `*.pro` files in Fallout 1 and Fallout 2. | Cubik2k | [[1]](https://nma-fallout.com/resources/fallout-1-2-critters-editor.100/) |

## Fonts

Related formats: [AAF](aaf.html), [FON](fon.html). These pages also contain browser previews for inspecting a font before editing it.

[Fallout Font Converter](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html), by Trykos, is a legacy AAF/BMP conversion option.

| Tool | Description | Author | Links |
|---|---|---|---|
| [AAF editor](https://fodev.net/files/mirrors/teamx-utils/AAF_editor.rar) | AAF font editor. | Wasteland rat | [[1]](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) |
| [aaf2txt](https://fodev.net/files/mirrors/teamx-utils/aaf2txt0.2.0.rar) | Represents an AAF font as a text file. | Noid | [[1]](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) |
| [Fallout Service Box: Font Editor](https://fodev.net/files/mirrors/teamx-utils/FSB_0.21.rar) | AAF font editor. | Keymone | [[1]](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) |
| [FON edit](https://fodev.net/files/mirrors/teamx-utils/fonedit1.0.rar) - [Source code](https://fodev.net/files/mirrors/teamx-utils/fonedit_src.rar) | A program for viewing and editing font FON files. | Anchorite, Wasteland Ghost | [[1]](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) |

## Mapping

Related formats: [MAP](map.html), [PRO](pro.html), [elevators](elevators.html).

[Gecko](https://github.com/JanSimek/gecko) is an additional Fallout 2 map editor with source and build instructions. It reads loose resources and DAT archives; its stated compatibility goal includes vanilla Fallout 2 and original Mapper maps. Follow its resource setup instructions before use.

The established mappers and conversion utilities below remain useful for existing projects:

| Tool | Description | Author | Links |
|---|---|---|---|
| [BIS mapper](https://fodev.net/files/mirrors/teamx-utils/BIS_mapper.rar) | The official map editor for Fallout 2. | Interplay | [[1]](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) |
| [BIS Mapper Patched + resources](https://fodev.net/files/archive/BIS%20mapper%20patched%20%28+complementary%20ressources%29.rar) | The official map editor for Fallout 2, patched and with additional resources. | Horusxav | [[1]](https://www.nma-fallout.com/resources/bis-mapper.55/) |
| [Elevator editor](https://fodev.net/files/mirrors/teamx-utils/f2_elev.rar) | Elevator editor for Fallout 2. | Mynah | [[1]](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) |
| [Dims Mapper 0.99.3.1](https://fodev.net/files/archive/F2_Mapper_Dims_0.99.3.1.rar) | An alternative map editor to the official BIS Mapper for Fallout 1/2. Made by Dims, updated by Fakels (aka Mr.Stalin) with usability improvements and fixes, and further updated by Radegast for properly loading `epax.map`/`easter.map` from F2RP. | Dims, updated by Mr.Stalin and Radegast | [[1]](https://nma-fallout.com/resources/dims-mapper.139/) [[2]](https://github.com/JanSimek/F2_Mapper_Dims) [[3]](https://www.nma-fallout.com/threads/dims-mapper-troubleshooting.220757/) |
| [FO2 to FO1 map converter 2.1.3.7](https://fodev.net/files/archive/fo2/FO2FO1_map_converter.zip) | Tool for converting from Fallout 2 format, edited in BIS mapper, to Fallout 1 format. | Cubik2k | [[1]](https://nma-fallout.com/resources/fo2-to-fo1-map-converter.98/) |

## Worldmap and masks

Related formats: [worldmap configuration](worldmap_config.html), [worldmap data](worldmap_dat.html), [MSK](msk.html), [FRM](frm.html).

| Tool | Use | Requirements / limits |
|---|---|---|
| [Fallout 2 Worldmap Editor — Cubik2k](https://nma-fallout.com/resources/fallout-2-worldmap-editor-alfa-version.101/) | Edit encounters in worldmap.txt. | Distributed as an alpha version; narrower than a full MAP editor. |
| [Fallout 1 Encounter Tweaker — Cubik2k](https://www.nma-fallout.com/threads/new-tools-for-fallout-1-2.196393/) | Change Fallout 1 worldmap encounter frequency. | Legacy executable patching tool; consult the author's version-specific high-resolution patch compatibility notes. |
| [Fallout encounter table viewer and parser — phobos2077](https://github.com/phobos2077/fallout2_worldmap) | Browse encounter tables from worldmap.txt. | Python 3 converts the input to JSON; the viewer runs from a local web server. This is a browser/parser, not a general worldmap editor. |
| [msk2bmpGUI — QuantumApprentice](https://github.com/QuantumApprentice/msk2bmpGUI) | Worldmap image preparation, tiling and mask workflows; also previews FRM animation. | Read the project controls and export instructions for the specific image/mask operation. |

## Audio

Related formats: [ACM](acm.html), [LIP](lip.html), [sound effects](sound_effects.html). The ACM page includes a local-file browser player for quick inspection.

| Tool | Use | Requirements / limits |
|---|---|---|
| [FFmpeg / ffplay](https://ffmpeg.org/general.html) | Decode ACM to standard audio; play or inspect supported media from the command line. | Interplay ACM decoding is documented; ACM encoding is not. |
| [libacm / acmtool](https://github.com/markokr/libacm) | Command-line ACM decoding, playback, and inspection, plus a reusable decoder library. | Documents mono/stereo overrides for incorrectly tagged audio. |
| [snd2acm — Abel](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) | Legacy WAV-to-ACM encoding. | Use for the reverse direction from acm2wav. |
| [LIP Editor — Anchorite](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) | Legacy LIP editing. | Talking-head synchronization. |
| [wav2lip — Eimink](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) | Legacy WAV-to-LIP generation. | Inspect the generated timing in game. |
| [Regsnd — Abel](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) | Legacy sndlist.lst updates. | Sound-list metadata workflow. |
| [VOCK — Vocal Output Creation Kit](https://github.com/dweltvauller/VOCK) | Pipeline from dialogue/audio through ACM, phoneme alignment, LIP generation, and DAT packaging. | Requires external tools including FFmpeg, snd2acm and alignment dependencies; follow the step-specific setup. |

Legacy standalone decoder:

| Tool | Description | Author | Links |
|---|---|---|---|
| [acm2wav](https://fodev.net/files/mirrors/teamx-utils/acm2wav.rar) | Convert ACM sound to WAV. | Abel | [[1]](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) |

## Graphics

Related formats: [FRM](frm.html), [PAL](pal.html), [colors](fo_colors.html), [RIX](rix.html). For worldmap tiling and masks, see [Worldmap and masks](#worldmap-and-masks).

| Tool | Use | Requirements / limits |
|---|---|---|
| [FRM Workshop — Metaphyzik](https://www.nexusmods.com/fallout2/mods/19) | Create FRM sprites from BMP frames, preview animation, adjust frame positions, and export BMP frames. | Legacy tool preserved on Nexus; useful for sprite assembly and offsets. |
| [frm2png — rotators](https://github.com/rotators/frm2png/) | Convert FRM art to PNG. | Export/conversion tool, not a general FRM editor. |
| [BGforge MLS animation viewer](https://github.com/BGforgeNet/BGforge-MLS/blob/master/docs/changelog.md) | Preview and convert animations, including FRM and directional FR0–FR5; PNG import/export. | Version 3.12.0 notes specify combined FRM output and FPS as the only editable option. |
| [FRM recoloring scripts — Dr Felix](https://github.com/Dr-Felix116/frm_recolour_python) | Batch palette-index replacement and a GUI for selecting frame regions. | FRM and FR0–FR5; [author's NMA instructions](https://ns1.nma-fallout.com/threads/how-to-recolor-sprites-automatically-a-python-script-to-replace-colors-in-frm-files.222796/) cover palettes and the recolour.txt mapping. |
| [Frame Animator — Joshua](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) | Legacy FRM editing. | Alternative sprite workflow. |
| [frmcat — Anchorite](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) | Combine six directional FRMs. | Legacy command-line utility. |
| [PAL View — Anchorite](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) | Inspect PAL files. | Legacy viewer. |
| [rix2bmp — Serge](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) | RIX-to-BMP export. | Legacy converter. |
| [HeadFrmPatcher](https://github.com/FalloutTeamX/HeadFrmPatcher) | Patch talking-head FRMs for sfall's 32-bit head options. | Archived, runtime-specific specialist utility. |

Legacy FRM/RIX browser:

| Tool | Description | Author | Links |
|---|---|---|---|
| [Titanium FRM Browser (English) 1.3](https://fodev.net/files/archive/fo2/Titanium%20FRM%20Browser%201.3%20%28en%29.zip) | Playing of FRM and RIX files on Windows platforms, easy to use, maximum usability. | Titanium | [[1]](https://www.nma-fallout.com/resources/titanium-frm-browser-english.105/) |

## Video

Related format: [MVE](mve.html).

[FFmpeg / ffplay](https://ffmpeg.org/general.html) can decode Interplay MVE for viewing or conversion to other formats. The published support table does not list MVE encoding; use the legacy encoding tools below for that workflow.


| Tool | Description | Author | Links |
|---|---|---|---|
| [avi2mve](https://fodev.net/files/mirrors/teamx-utils/avi2mve_040919.rar) | AVI to MVE converter. | Abel | [[1]](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) |
| [GUI for avi2mve](https://fodev.net/files/mirrors/teamx-utils/conv2mve0.1b.rar) | GUI for avi2mve converter. | Tehnokrat | [[1]](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) |
| [MVE Joiner](https://fodev.net/files/mirrors/teamx-utils/mve_joiner.rar) | Merges several MVEs into one. | Abel | [[1]](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) |
| [MVE Player](https://fodev.net/files/mirrors/teamx-utils/mve-play.rar), [MVE Player Stuff](https://fodev.net/files/mirrors/teamx-utils/mvestuff.rar) | Library for playing MVE. MVE to EXE converter. With source code and format description. | Unknown | [[1]](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) |
| [MVE Tools](https://fodev.net/files/mirrors/teamx-utils/MVETools.rar) | Utilities for working with MVE. | Interplay | [[1]](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) |

<a id="misc"></a>

## Text and configuration

Plain-text editing is the normal workflow for [MSG](msg.html), [LST](lst.html), [GAM](gam.html), [BIO](bio.html), [CFG/INI](cfg.html), [AI.TXT](ai_txt.html), [PARTY.TXT](party_txt.html), [PIPBOY.TXT](pipboy_txt.html), [worldmap configuration](worldmap_config.html), [credits](credits.html), and text-based [book](books.html), [ending](endings.html), and [critical-hit](criticals.html) overrides.

Use a text editor that preserves the required encoding and line endings. Follow the linked format page for syntax and identifiers. For SSL/MSG projects, [BGforge MLS](https://github.com/BGforgeNet/BGforge-MLS) and [sfall Script Editor](https://github.com/phobos2077/sfall_script_editor) add language-specific assistance. A dedicated GUI is not required for every text format.

## Debugging

| Tool or reference | Use | Scope |
|---|---|---|
| [sfall Debug Editor](https://github.com/sfall-team/sfall/blob/master/sfall/Modules/DebugEditor.cpp) | Inspect/change live globals, map variables, objects, prototypes, and arrays through FalloutDebug.exe. | Requires the matching sfall debugging setup; this is runtime editing rather than an offline save editor. |
| [Engine research resources](legacy-reversing.html) | Disassemblers, debuggers, historical databases, and executable tooling. | Select references for the exact executable version. See also [symbols](symbols.html) and [structures](structs.html). |

## Libraries

For tool authors rather than point-and-click editing:

| Project | Useful area | Scope |
|---|---|---|
| [rotators fallout-tools](https://github.com/rotators/fallout-tools) | DatLib, libacm, compiler/editor source, and prototype tooling. | Source collection; inspect the individual project's build requirements and license. |
| [Fallout 2 CE](https://github.com/fallout2-ce/fallout2-ce) | Cross-check readers, writers, and runtime behavior against engine code. | Engine implementation, not a standalone format SDK. |
| [fallout-se](https://github.com/ali-raheem/fallout-se) | SAVE.DAT parsing and structured character export. | Rust core and command-line/browser frontends; see documented unsupported edits. |

Historical compiler source retained from the original directory:

| Tool | Description | Author | Links |
|---|---|---|---|
| [Klingon Academy compiler source code](https://fodev.net/files/mirrors/teamx-utils/KA_CompilerSource.rar) | Related Star Trek Scripting Language compiler source; retained for historical compiler research, not as a Fallout build tool. | Interplay | [[1]](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) |

## Character templates

[CGCD — rotators](https://github.com/rotators/cgcd) creates [GCD](gcd.html) premade-character data for Fallout 1 and Fallout 2 from an editable character.txt definition. It is a command-line generation workflow, separate from changing an existing SAVE.DAT. The [GCD page](gcd.html#tools) also links a binary mirror.

## Saves

Related formats: [savegame structure](savegame.html), [GCD](gcd.html), [SVE](sve.html). A SAVE.DAT editor does not automatically edit every sidecar or character-template format.

| Tool | Use | Compatibility / limits |
|---|---|---|
| [Falche Fallout 1 editor](https://www.nma-fallout.com/resources/falche-fallout-1-editor.15/) | Legacy character stats, skills, traits, and perks editing. | The listing targets Windows Fallout 1 and explicitly excludes inventory editing. |
| [F12se](https://github.com/nousrnam/F12se) | Universal Fallout 1 and Fallout 2 save editor. | Source project for the established editor listed below; check its instructions for the target game/mod. |
| [fallout-se — Ali Raheem](https://github.com/ali-raheem/fallout-se) | Command-line save inspection, JSON export, and selected character/inventory edits; browser frontend also available. | Work in progress. Upstream marks full world-state editing and adding a previously absent inventory PID as unsupported; web editing is in early testing. |

Original distribution and attribution:

| Tool | Description | Author | Links |
|---|---|---|---|
| [Universal savegame editor for Fallout & Fallout 2](https://fodev.net/files/archive/fo2/F12se.zip) | Save editing for Fallout 1 and Fallout 2. [Source code](https://fodev.net/files/archive/fo2/F12se_src.7z) | vad | [[1]](https://sites.google.com/site/chulancheg/) [[2]](https://www.nma-fallout.com/threads/fallout-2-savegame-editor.185130/) |


## Sources and further discovery

- [rotators/fallout-tools](https://github.com/rotators/fallout-tools): Fallout 1/2 tool sources, including DAT Explorer II, ProtoManager, Script Editor, and audio utilities.
- [NMA Fallout 2 utilities](https://www.nma-fallout.com/resources/categories/utilities.7/): release pages and downloads. Individual entries above link to the relevant resource or author thread.
- [NMA Fallout General Modding](https://www.nma-fallout.com/forums/fallout-general-modding.18/): specialist tools, author updates, and usage discussions that are not always mirrored as resources.
- [TeamX historical utility mirror](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html): legacy tools and original attributions.

## Coverage and contributions

Some niches are covered by source references or manual workflows rather than a verified dedicated editor: raw worldmap.dat state, SVE exploration records, and complete save sidecar editing. Do not infer support for them from a tool's general Fallout label.

When adding a tool, state the input/output formats, operation, target game/runtime, interface, project/download link, and any known limitation. Identify mirrors and forks. Record hands-on verification separately from a documentation review. The [TeamX mirror](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html) preserves additional historical utilities; listings here prioritize distinct tasks over duplicating every old version.
