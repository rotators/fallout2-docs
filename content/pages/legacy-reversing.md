---
title: Legacy Reverse-Engineering Resources
output: legacy-reversing.html
description: Original Fallout executable and high-resolution patch references, Watcom calling conventions, assembly tools, and historical IDA databases.
toc: auto
---

# Legacy Reverse-Engineering Resources

[Back to the resource index](index.html)

These references support work on the original Fallout binaries and older tooling.
For source-level engine research, start with the Community Edition and Reference
Edition projects listed under [Projects](index.html#fo_projects).

The binary offsets, compiler notes, and databases below are retained for historical
reference and compatibility work. Check the executable version before using an
offset or database; these resources are not a current source-code reference.

<a id="fo2exe"></a>

## Fallout2.exe

[Structures](structs.html)

[Sfall references](sfall_refs.html)

[Fallout 2 RE references](fallout2_re.html)

[Function and variable offsets](symbols.html)

[Call structure](https://rotators.fodev.net/atom/F2_function_structure.txt)

[x64dbg database](https://github.com/rotators/sfall/blob/rotators/db/Fallout2.dd32)

[Historical IDA database](#ida)

<a id="f2res"></a>

## f2_res.dll (High resolution patch)

[Symbols](hrp.html)

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

This database is outdated and is preserved as a historical snapshot. Prefer the
CE and RE source projects for engine research.

[Fallout_1_and_2_IDA68.rar](https://rotators.fodev.net/ghosthack/scrapheap/reversing/ida/Fallout_1_and_2_IDA68.rar)

[idbtool.exe](https://rotators.fodev.net/ghosthack/scrapheap/reversing/ida/idbtool.exe)

`idbtool.exe --enums Fallout2.idb > enums.txt`

`idbtool.exe --names Fallout2.idb > names.txt`

