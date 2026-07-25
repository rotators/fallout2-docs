---
title: Fallout 1 & 2 reversing and modding resource
output: index.html
description: Main index for Fallout 1 and Fallout 2 reversing, modding, file format, tooling, project, and purchase resources.
---

```toc
1. [Fallout2.exe](#fo2exe)
2. [f2_res.dll](#f2res)
3. [Modding](#modding)
4. [File formats](#formats)
5. [Artwork](#artwork)
6. [Animation](#animation)
7. [Scripting](#scripting)
8. [Worldmap](#worldmap)
9. [Watcom](#watcom)
10. [ASM](#asm)
11. [Reversing tools](#revtools)
12. [IDA](#ida)
13. [Fallout tools](#fo2tools)
14. [sfall](#sfall)
    - [Binary distros](#sfall-bin)
    - [Source code](#sfall-src)
    - [DirectX](#sfall-dx)
    - [sfall dev](#sfall-dev)
15. [Projects](#fo_projects)
16. [Misc](#misc)
17. [Buy](#buy)
```

# Fallout 1 & 2 reversing and modding resource

Updated 2026-05-09

You can find additional information and code at [github.com/rotators](https://github.com/rotators)

<a id="fo2exe"></a>

## Fallout2.exe

[Structures](structs.html)

[Sfall references](sfall_refs.html)

[Fallout 2 RE references](fallout2_re.html)

[Function and variable offsets](symbols.html)

[Call structure](https://rotators.fodev.net/atom/F2_function_structure.txt)

[x64dbg database](https://github.com/rotators/sfall/blob/rotators/db/Fallout2.dd32)

[Fallout_1_and_2_IDA68.rar - IDA database](https://rotators.fodev.net/ghosthack/scrapheap/reversing/ida/Fallout_1_and_2_IDA68.rar)

<a id="f2res"></a>

## f2_res.dll (High resolution patch)

[Symbols](hrp.html)

<a id="modding"></a>

## Modding

[Comprehensive Fallout 2 Modding Guide](https://f3mic.github.io/) - [source](https://github.com/F3mic/F3mic.github.io)

[Quantum's Fallout Modding 'How To' Videos](https://www.nma-fallout.com/threads/quantums-fallout-modding-how-to-videos.220015/)

<a id="formats"></a>

## File formats

| Format | Tools |
| --- | --- |
| [AI.TXT - Description of combat parameters for the player and all NPC classes in the game](ai_txt.html) | [Fallout 2 - Proto Manager](https://www.nma-fallout.com/resources/fallout-2-proto-manager.73/) |
| [PARTY.TXT - Party-member registry, combat-control option whitelist, and companion level-up data.](party_txt.html) | Text editor |
| [Pip-Boy text data - quests.txt, holodisk.txt, quest status, holodisk text, and GVAR-controlled display rules.](pipboy_txt.html) | Text editor |
| [ACM - Interplay compressed audio for music, speech, and sound effects.](acm.html) | Use [libacm](https://github.com/markokr/libacm), [acm2wav](https://fodev.net/files/mirrors/teamx-utils/acm2wav.rar) or [Game Audio Player](https://fodev.net/files/archive/gap.zip) for playback. |
| [AAF - The AAF Font File Format is used to store fonts.](aaf.html) | [Fallout Service Box: Font Editor](https://fodev.net/files/mirrors/teamx-utils/FSB_0.21.rar) |
| [BIO - Story for premade characters (GCD).](bio.html) | Text editor |
| [CFG/INI - Runtime configuration files, parser behavior, sfall INI settings, high-resolution options, and movie sidecars.](cfg.html) | Text editor |
| [Critical hit tables - Executable combat tables, sfall/CE override INI layout, damage flags, and combat message dependencies.](criticals.html) | Text editor, hex editor |
| [DAT - Archive containers for Fallout 1/2 resources, including DAT1 and DAT2 layouts.](dat.html) | [Dat Explorer 1.43](https://fodev.net/files/mirrors/teamx-utils/dat_explorer.rar), [many others](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html#dat) |
| [Elevators.ini - sfall/CE elevator destination tables, interface templates, and script activation rules.](elevators.html) | Text editor |
| [FON - Fonts used for text on the world map](fon.html) | [FON editor](https://fodev.net/files/mirrors/teamx-utils/fonedit1.0.rar) |
| [FRM - Indexed art, animation frames, rotations, offsets, FID lookup, and palette-dependent rendering.](frm.html) | [Titanium FRM browser](https://fodev.net/files/archive/fo2/Titanium%20FRM%20Browser%201.3%20%28en%29.zip), [Graphics viewer 1.36](https://fodev.net/files/mirrors/teamx-utils/viewer.rar) [and many others.](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html#graphics) |
| [INT - Compiled script bytecode for Fallout and Fallout 2 SSL scripts.](int.html) | use int2ssl.exe included in [sfall modderspack](https://sourceforge.net/projects/sfall/files/Modders%20pack/modderspack_4.3.4.7z/download) to decompile or [source repo](https://github.com/phobos2077/int2ssl). |
| [SSL - Source language and build format for Fallout and Fallout 2 scripts.](ssl.html) | [sfall SSLC](https://sfall-team.github.io/sfall/sslc/), [Fallout 2 script library](https://fallout.fandom.com/wiki/Fallout_2_script_library) |
| [SCRIPTS.LST - Indexed registry for compiled INT scripts, dialogue MSG binding, and script local-variable metadata.](scripts_lst.html) | Text editor |
| [Fallout 2 savegame structure - SAVE.DAT, saved map sidecars, party PRO sidecars, automap data, sfall extensions, and Fallout 1 compatibility boundaries.](savegame.html) | Hex editor, gzip tools |
| [GCD File Format - Premade characters.](gcd.html) | [CGCD](https://github.com/rotators/cgcd) |
| [GAM - GAM files are indexed text files. They contain global variables for each core Fallout game and its maps](gam.html) | Text editor |
| [LIP - Talking-head lip-sync timing for speech audio.](lip.html) | [LIP editor](https://fodev.net/files/mirrors/teamx-utils/LIPEditor0.96b.rar), [wav2lip](https://fodev.net/files/mirrors/teamx-utils/wav2lip.rar) |
| [LST - Line-indexed tables, art FID resolution, critter/head metadata, and filename construction.](lst.html) | Text editor |
| [MAP - Maps used for locations.](map.html) | [Patched BIS mapper](https://www.nma-fallout.com/resources/bis-mapper.55/) |
| [MSG - Text message lists for dialogue, object names, UI strings, map names, and combat text.](msg.html) | Text editor |
| [MSK - World-map walk masks that mark blocked terrain pixels.](msk.html) | [MSK tools](https://fodev.net/files/mirrors/teamx-utils/MSKTools.rar), [msk2bmp](https://fodev.net/files/mirrors/teamx-utils/msk2bmp.rar) |
| [PAL/COL - Fallout palette files, RGB555 lookup, color tables, and animated palette ranges.](pal.html) | [Fallout default color sheet](fo_colors.html) |
| [PRO - Prototype, every item, critter, wall, tile, and piece of scenery has its own corresponding PRO file.](pro.html) | [Fallout 2 - Proto Manager](https://www.nma-fallout.com/resources/fallout-2-proto-manager.73/) |
| [RIX - ColoRIX indexed bitmap format used for startup/loading splash screens.](rix.html) | [Graphics viewer 1.36 to load/save](https://fodev.net/files/mirrors/teamx-utils/viewer.rar) |
| [MVE - Interplay movie container for intro, ending, logo, credits, and cutscene videos.](mve.html) | [Various](https://fodev.net/files/mirrors/teamx-utils/!_INDEX_en.html#video) |
| [SVE - Frame-numbered subtitle cue lists for MVE movies.](sve.html) | Text editor |
| [Worldmap.dat - Serialized world-map state and related world map definition files.](worldmap_dat.html) | [Fallout2 worldmap.txt interactive browser and parser](https://github.com/phobos2077/fallout2_worldmap) |
| [World-map text config - maps.txt, city.txt, and worldmap.txt editable world-map definitions.](worldmap_config.html) | Text editor, [Fallout2 worldmap.txt interactive browser and parser](https://github.com/phobos2077/fallout2_worldmap) |

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

<a id="watcom"></a>

## Watcom

[Watcom](https://en.wikipedia.org/wiki/Watcom_C/C%2B%2B) is the compiler that was used to compile both Fallout 1 and 2.

Watcom does not support the __fastcall keyword except to alias it to null. The register calling convention may be selected by command line switch. (However, IDA uses __fastcall anyway for uniformity.)

Up to 4 registers are assigned to arguments in the order eax, edx, ebx, ecx. Arguments are assigned to registers from left to right.

If any argument cannot be assigned to a register (say it is too large) it, and all subsequent arguments, are assigned to the stack. Arguments assigned to the stack are pushed from right to left. Names are mangled by adding a suffixed underscore.

eax->func(edx, ebx, ecx, push...)

func(eax, edx, ebx, ecx, push...)

[Read more](https://web.archive.org/web/20150503230850/http://openwatcom.org/index.php/Calling_Conventions#Specifying_Calling_Conventions)

<a id="asm"></a>

## ASM

[x86 reference](https://c9x.me/x86/)

[x86 and amd64 instruction reference](https://www.felixcloutier.com/x86/)

[x86 opcode table](http://ref.x86asm.net/coder32.html)

[Online x86 / x64 Assembler and Disassembler](https://defuse.ca/online-x86-assembler.htm)

<a id="revtools"></a>

## Reversing tools

[OllyDBG - debugger](https://www.ollydbg.de)

[x64dbg - debugger](https://x64dbg.com/)

[IDA 7 freeware - disassembler/debugger](https://www.hex-rays.com/products/ida/support/download_freeware/)

[IDA 5 - Old version of IDA, suitable for DOS reversing](https://www.scummvm.org/news/20180331/)

[PE explorer](https://www.heaventools.com/overview.htm)

[HxD - Freeware Hex Editor and Disk Editor ( alternatives](https://mh-nexus.de/en/hxd/))

[DLL Export Viewer v1.66](https://nirsoft.net/utils/dll_export_viewer.html)

[Scylla - Imports viewer](https://github.com/NtQuery/Scylla)

[idbutil - Tool for dumping data from IDA pro databases](https://github.com/nlitsme/pyidbutil)

[Additional stuff](https://github.com/tylerha97/awesome-reversing)

<a id="ida"></a>

## IDA database

[Fallout_1_and_2_IDA68.rar](https://rotators.fodev.net/ghosthack/scrapheap/reversing/ida/Fallout_1_and_2_IDA68.rar)

[idbtool.exe](https://rotators.fodev.net/ghosthack/scrapheap/reversing/ida/idbtool.exe)

`idbtool.exe --enums Fallout2.idb > enums.txt`

`idbtool.exe --names Fallout2.idb > names.txt`

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

| Project | Description |
| --- | --- |
| [Fallout 1 Reference Edition](https://github.com/alexbatalov/fallout1-re) | Fallout 1 RE project by Alexander Batalov. |
| [Fallout 1 Community Edition](https://github.com/alexbatalov/fallout1-ce) | Fallout Community Edition is a fully working re-implementation of Fallout, with the same original gameplay, engine bugfixes, and some quality of life improvements, that works (mostly) hassle-free on multiple platforms. |
| [Fallout 2 Reference Edition](https://github.com/alexbatalov/fallout2-re) | Fallout 2 RE project by Alexander Batalov. [Announcement post](https://medium.com/@alex.batalov/reverse-engineering-fallout-2-5dad1421de21) |
| [Fallout 2 Community Edition](https://github.com/alexbatalov/fallout2-ce) | Fallout 2 Community Edition is a fully working re-implementation of Fallout 2, with the same original gameplay, engine bugfixes, and some quality of life improvements, that works (mostly) hassle-free on multiple platforms. |
| [Fallout 2 Javascript port](https://github.com/ajxs/jsFO) | Inactive engine implementation |
| [DarkFO, a post-nuclear RPG remake (of Fallout 2)](https://github.com/darkf/darkfo) | Inactive engine implementation in TypeScript and Python |
| [falltergeist](https://github.com/falltergeist/falltergeist) | Opensource crossplatform Fallout 2 game engine written in C++ and SDL. |
| [Fallout 2 tweaks](https://github.com/BGforgeNet/FO2tweaks) | A collection of convenience tweaks, common sense changes, and cheats for Fallout 2. It is highly configurable, any component can be used with or without others. Some components also allow fine tuning. |
| [Fallout 2 map editor](https://github.com/JanSimek/geck-map-editor) | Fallout 2 map editor by Jan Simek |
| [Klamath](https://github.com/adamkewley/klamath) | C++ utilities for working with Fallout 1/2 assets |
| [Fallout et Tu (Fallout 1 in 2)](fo1in2.html) | A project that aims to bring Fallout 1 into the Fallout 2 engine. |

<a id="misc"></a>

## Misc

- [List of Fallout 1 & 2 mods](mods.html)
- [Documentation generator notes](docs-generator.html)
- [GDC - Classic Game Postmortem: Fallout](https://www.youtube.com/watch?v=T2OxO-4YLRk)
- [The Nearly Ultimate Fallout Guide](https://lemmings19.github.io/fallout-1-walkthrough/)
- [The Nearly Ultimate Fallout 2 Guide](https://twinysam.github.io/fallout2guide/)
- [Fallout Bible](https://fallout.fandom.com/wiki/Fallout_Bible)
- [Mirror of the official site](https://fodev.net/files/fo2/official/) - [based on this repo](https://github.com/twinysam/falloutwebsite)

<a id="buy"></a>

## Buy

| Game | Stores |
| --- | --- |
| Fallout | [GOG](https://www.gog.com/game/fallout), [SteamDB](https://steamdb.info/app/38400/), [Epic](https://store.epicgames.com/en-US/p/fallout) |
| Fallout 2 | [GOG](https://www.gog.com/game/fallout_2), [SteamDB](https://steamdb.info/app/38410/), [Epic](https://store.epicgames.com/sv/p/fallout-2) |
