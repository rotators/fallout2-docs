---
title: Fallout 1 & 2 reversing and modding resource
output: index.html
description: Main index for Fallout 1 and Fallout 2 reversing, modding, file format, tooling, project, and purchase resources.
---

```toc
1. [Start here](#start-here)
2. [Engine source](#engine-source)
3. [Modding](#modding)
4. [File formats](#formats)
5. [Artwork](#artwork)
6. [Animation](#animation)
7. [Scripting](#scripting)
8. [Worldmap](#worldmap)
9. [Fallout tools](#fo2tools)
10. [sfall](#sfall)
    - [Binary distros](#sfall-bin)
    - [Source code](#sfall-src)
    - [DirectX](#sfall-dx)
    - [sfall dev](#sfall-dev)
11. [Projects](#fo_projects)
12. [Misc](#misc)
13. [Buy](#buy)
```

# Fallout 1 & 2 reversing and modding resource

Updated {{ documentationUpdated }}

You can find additional information and code at [github.com/rotators](https://github.com/rotators)

## Start here

- [Modding guide](#modding) — tutorials for creating and changing mods.
- [File formats](#formats) — references for reading and editing game resources.
- [Scripting](#scripting) — script source, compiled scripts, and opcode references.
- [Tools](tools.html) — editors, viewers, and converters.
- [Engine source](#engine-source) — Community Edition and Reference Edition projects.

## Engine source

Use Community Edition (CE) to explore or modify a playable engine with platform
support and fixes. Use Reference Edition (RE) to study reverse-engineered
original engine code. When checking behavior, distinguish original logic from
changes made by a particular CE fork.

| Project | Purpose | Activity checked 2026-09-08 |
| --- | --- | --- |
| [FOR:CE / Fallout 2 CE](https://github.com/fallout2-ce/fallout2-ce) | Playable engine with platform support and fixes | Active; [latest default-branch commit](https://github.com/fallout2-ce/fallout2-ce/commits) 2026-09-07. |
| [Fallout 1 CE](https://github.com/alexbatalov/fallout1-ce) | Playable Fallout 1 reimplementation | No recent default-branch activity; [latest commit](https://github.com/alexbatalov/fallout1-ce/commits) 2025-01-15. |
| [Fallout 1 RE](https://github.com/alexbatalov/fallout1-re) | Original engine code reference | Reference project; [latest commit](https://github.com/alexbatalov/fallout1-re/commits) 2023-01-30. |
| [Fallout 2 RE](https://github.com/alexbatalov/fallout2-re) | Original engine code reference | Reference project; [latest commit](https://github.com/alexbatalov/fallout2-re/commits) 2023-01-20. |

For original executable offsets and historical tools, see
[legacy reverse-engineering resources](legacy-reversing.html).

<a id="modding"></a>

## Modding

[Comprehensive Fallout 2 Modding Guide](https://f3mic.github.io/) - [source](https://github.com/F3mic/F3mic.github.io)

[Quantum's Fallout Modding 'How To' Videos](https://www.nma-fallout.com/threads/quantums-fallout-modding-how-to-videos.220015/)

<a id="formats"></a>

## File formats

Browse by purpose. Each reference explains the format and its runtime differences.

[Graphics and fonts](#graphics-and-fonts) · [Audio and video](#audio-and-video) · [Scripts and text](#scripts-and-text) · [Maps and world](#maps-and-world) · [Game data and saves](#game-data-and-saves)

### Graphics and fonts

| Format | Contents | Tools |
| --- | --- | --- |
| [AAF](aaf.html) | Interface fonts | [Fallout Service Box: Font Editor](https://fodev.net/files/mirrors/teamx-utils/FSB_0.21.rar) |
| [FON](fon.html) | World-map fonts | [FON editor](https://fodev.net/files/mirrors/teamx-utils/fonedit1.0.rar) |
| [FRM](frm.html) | Sprites and animation frames | [Titanium FRM browser](https://fodev.net/files/archive/fo2/Titanium%20FRM%20Browser%201.3%20%28en%29.zip), [Graphics viewer 1.36](https://fodev.net/files/mirrors/teamx-utils/viewer.rar) [and many others.](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html#graphics) |
| [PAL/COL](pal.html) | Palettes and color lookup tables | [Fallout default color sheet](fo_colors.html) |
| [RIX](rix.html) | Startup and splash images | [Graphics viewer 1.36 to load/save](https://fodev.net/files/mirrors/teamx-utils/viewer.rar) |

### Audio and video

| Format | Contents | Tools |
| --- | --- | --- |
| [ACM](acm.html) | Music, speech, and sound effects | Use [libacm](https://github.com/markokr/libacm), [acm2wav](https://fodev.net/files/mirrors/teamx-utils/acm2wav.rar) or [Game Audio Player](https://fodev.net/files/archive/gap.zip) for playback. |
| [LIP](lip.html) | Talking-head lip sync | [LIP editor](https://fodev.net/files/mirrors/teamx-utils/LIPEditor0.96b.rar), [wav2lip](https://fodev.net/files/mirrors/teamx-utils/wav2lip.rar) |
| [MVE](mve.html) | Movies and cutscenes | [Various](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html#video) |
| [SVE](sve.html) | Movie subtitles | Text editor |

### Scripts and text

| Format | Contents | Tools |
| --- | --- | --- |
| [SSL](ssl.html) | Script source language | [sfall SSLC](https://sfall-team.github.io/sfall/sslc/), [Fallout 2 script library](https://fallout.fandom.com/wiki/Fallout_2_script_library) |
| [INT](int.html) | Compiled scripts | use int2ssl.exe included in [sfall modderspack](https://sourceforge.net/projects/sfall/files/Modders%20pack/modderspack_4.3.4.7z/download) to decompile or [source repo](https://github.com/phobos2077/int2ssl). |
| [SCRIPTS.LST](scripts_lst.html) | Script registry | Text editor |
| [MSG](msg.html) | Dialogue and interface text | Text editor |
| [BIO](bio.html) | Premade character biographies | Text editor |
| [Pip-Boy text](pipboy_txt.html) | Quests and holodisks | Text editor |
| [Credits and quotes](credits.html) | Credits text and scrolling | Text editor |

### Maps and world

| Format | Contents | Tools |
| --- | --- | --- |
| [MAP](map.html) | Location maps | [Patched BIS mapper](https://www.nma-fallout.com/resources/bis-mapper.55/) |
| [MSK](msk.html) | World-map walk masks | [MSK tools](https://fodev.net/files/mirrors/teamx-utils/MSKTools.rar), [msk2bmp](https://fodev.net/files/mirrors/teamx-utils/msk2bmp.rar) |
| [World-map configuration](worldmap_config.html) | Maps, cities, and terrain definitions | Text editor, [Fallout2 worldmap.txt interactive browser and parser](https://github.com/phobos2077/fallout2_worldmap) |
| [Worldmap.dat](worldmap_dat.html) | Serialized world-map state | [Fallout2 worldmap.txt interactive browser and parser](https://github.com/phobos2077/fallout2_worldmap) |
| [Elevators.ini](elevators.html) | Elevator destinations and interfaces | Text editor |

### Game data and saves

| Format | Contents | Tools |
| --- | --- | --- |
| [DAT](dat.html) | Resource archives | [Dat Explorer 1.43](https://fodev.net/files/mirrors/teamx-utils/dat_explorer.rar), [many others](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html#dat) |
| [LST](lst.html) | Indexed resource lists | Text editor |
| [PRO](pro.html) | Object prototypes | [Fallout 2 - Proto Manager](https://www.nma-fallout.com/resources/fallout-2-proto-manager.73/) |
| [GCD](gcd.html) | Premade character data | [CGCD](https://github.com/rotators/cgcd) |
| [GAM](gam.html) | Game and map global variables | Text editor |
| [AI.TXT](ai_txt.html) | Combat AI parameters | [Fallout 2 - Proto Manager](https://www.nma-fallout.com/resources/fallout-2-proto-manager.73/) |
| [PARTY.TXT](party_txt.html) | Party members and combat controls | Text editor |
| [CFG/INI](cfg.html) | Engine and mod configuration | Text editor |
| [Critical hit tables](criticals.html) | Combat effects and overrides | Text editor, hex editor |
| [Skill books](books.html) | Book items and skill gains | Text editor |
| [Ending configuration](endings.html) | Slideshows and death screens | Text editor |
| [Savegames](savegame.html) | SAVE.DAT and supporting files | Hex editor, gzip tools |

<a id="artwork"></a>

## Artwork

[The Complete Fallout 1 & 2 Artwork](https://www.nma-fallout.com/threads/the-complete-fallout-1-2-artwork.191548/)

<a id="animation"></a>

## Animation

[Animation names](anim_names.html)

[Animation viewer](https://rotators.fodev.net/ghosthack/scrapheap/anim_viewer/) - [TypeScript source](https://github.com/rotators/fallout-animations/tree/master/viewer)

<a id="scripting"></a>

## Scripting

[Fallout 2 opcodes](https://fodev.net/files/fo2/opcodes) - does not include [sfall opcodes](https://github.com/sfall-team/sfall/blob/master/artifacts/scripting/sfall%20opcode%20list.txt).
[Official Fallout 2 scripts source code](https://fodev.net/files/mirrors/teamx-utils/F2_scripts.rar)

<a id="worldmap"></a>

## Worldmap

[Fallout2 worldmap.txt interactive browser and parser](https://github.com/phobos2077/fallout2_worldmap)

<a id="fo2tools"></a>

## Fallout tools

[List of tools](tools.html)

[Fallout 2 @ NMA](https://nma-fallout.com/resources/categories/fallout-2.5/)

[Team-X Utilities](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html)

| Tool | Author | Description |
| --- | --- | --- |
| [Sfall Script Editor](https://nma-fallout.com/resources/sfall-script-editor.77/) | Sfall team | Allows to edit and compile SSL scripts in a convenient way. |
| [Dat Explorer 1.43](https://fodev.net/files/mirrors/teamx-utils/dat_explorer.rar) | Dims | DAT file packer / unpacker. With a graphical interface. |

<a id="sfall"></a>

## Sfall

A set of engine modifications that greatly enhances the engine. Includes fixes for bugs in the original engine, allows fallout to run correctly on modern operating systems, and adds additional features for modders.

<a id="sfall-bin"></a>

### Binary distros

[Sourceforge](https://sourceforge.net/projects/sfall/files/sfall/)

<a id="sfall-src"></a>

### Source code

Check out the code from the [main repo](https://github.com/sfall-team/sfall).

<a id="sfall-dx"></a>

### DirectX

Main SDK needed is [DirectX june 2010](https://archive.org/download/dxsdk_2010). [dinput.lib](https://rotators.fodev.net/ghosthack/scrapheap/sfall_dx/dinput.lib) from [DirectX august 2007 also needed](https://archive.org/download/dxsdk_aug2007).

Mirror: [DirectX SDK Collection](https://github.com/NovaRain/DXSDK_Collection)

<a id="sfall-dev"></a>

### sfall dev

- [Documentation](https://sfall.bgforge.net)
- [Timeslip's page - Original sfall dev](http://timeslip.users.sourceforge.net/index.html) ([old page](https://web.archive.org/web/20240615071301/http://timeslip.chorrol.com/))
- [FO2 Engine Tweaks (Sfall) @ NMA](https://nma-fallout.com/threads/fo2-engine-tweaks-sfall.178390/)
- [Sfall 1](http://fforum.kochegarov.com/index.php?showtopic=29288)
- [NMA discord](https://discord.gg/VgcmJCN)
- Github: [@phobos2077](https://github.com/phobos2077) [@NovaRain](https://github.com/NovaRain)

<a id="fo_projects"></a>

## Projects

Status checked 2026-09-08 against repository notices and default-branch commit
history. “Active” indicates recent public development; a quiet repository may
still be useful. These are dated observations, not a guarantee of support.
CE and RE repositories are listed under [Engine source](#engine-source).

### Actively maintained projects

| Project | Description | Recent activity |
| --- | --- | --- |
| [Fallout 2 tweaks](https://github.com/BGforgeNet/FO2tweaks) | Configurable gameplay and convenience tweaks | [Latest commit](https://github.com/BGforgeNet/FO2tweaks/commits) 2026-08-22. |
| [Gecko](https://github.com/JanSimek/gecko) | Fallout 2 map editor, formerly geck-map-editor | [Latest commit](https://github.com/JanSimek/gecko/commits) 2026-08-30. |
| [Fallout et Tu](fo1in2.html) | Fallout 1 in the Fallout 2 engine | [Latest commit](https://github.com/rotators/Fo1in2/commits) 2026-09-07. |

### Historical and dormant projects

| Project | Description | Status |
| --- | --- | --- |
| [jsFO](https://github.com/ajxs/jsFO) | Experimental Fallout 2 implementation for the browser | Author states development has ceased; [latest commit](https://github.com/ajxs/jsFO/commits) 2022-07-27. |
| [DarkFO](https://github.com/darkf/darkfo) | Fallout 2 remake in TypeScript and Python | Repository archived; [latest commit](https://github.com/darkf/darkfo/commits) 2019-03-07. |
| [falltergeist](https://github.com/falltergeist/falltergeist) | Fallout 2 engine implementation in C++ and SDL | Dormant by default-branch activity; [latest commit](https://github.com/falltergeist/falltergeist/commits) 2022-07-15. Not archived. |
| [Klamath](https://github.com/adamkewley/klamath) | Demo code for working with Fallout 1/2 assets | Repository archived; [latest commit](https://github.com/adamkewley/klamath/commits) 2021-04-07. |

<a id="misc"></a>

## Misc

<a id="fo2exe"></a>
<a id="f2res"></a>
<a id="watcom"></a>
<a id="asm"></a>
<a id="revtools"></a>
<a id="ida"></a>

[Legacy reverse-engineering resources](legacy-reversing.html) — original executable and high-resolution patch references, Watcom and assembly notes, debuggers, and historical IDA databases.

- [List of Fallout 1 & 2 mods](mods.html)
- [Documentation generator notes](docs-generator.html)
- [GDC - Classic Game Postmortem: Fallout](https://www.youtube.com/watch?v=T2OxO-4YLRk)
- [The Nearly Ultimate Fallout Guide](https://lemmings19.github.io/fallout-1-walkthrough/)
- [The Nearly Ultimate Fallout 2 Guide](https://twinysam.github.io/fallout2guide/)
- [Fallout Bible](https://fallout.fandom.com/wiki/Fallout_Bible)
- [Mirror of the official site](https://fodev.net/files/fo2/official/) - [based on this repo](https://github.com/twinysam/falloutwebsite)

<a id="buy"></a>

## Buy

| Game | Storefronts | Databases |
| --- | --- | --- |
| Fallout | [GOG](https://www.gog.com/game/fallout), [Steam](https://store.steampowered.com/app/38400/), [Epic](https://store.epicgames.com/en-US/p/fallout) | [SteamDB](https://steamdb.info/app/38400/), [PCGamingWiki](https://www.pcgamingwiki.com/wiki/Fallout) |
| Fallout 2 | [GOG](https://www.gog.com/game/fallout_2), [Steam](https://store.steampowered.com/app/38410/), [Epic](https://store.epicgames.com/en-US/p/fallout-2) | [SteamDB](https://steamdb.info/app/38410/), [PCGamingWiki](https://www.pcgamingwiki.com/wiki/Fallout_2) |
