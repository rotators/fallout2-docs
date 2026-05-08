---
title: INT File Format
output: int.html
description: Fallout and Fallout 2 compiled script bytecode format, procedure tables, identifiers, opcode reference, sfall extensions, script lifecycle, and disassembly notes.
---

# INT File Format

INT files are compiled Fallout and Fallout 2 scripts. The source language is usually called SSL. The game does not execute SSL directly: the compiler turns it into an INT bytecode program, and the engine interpreter runs that program when a map, object, critter, item, spatial trigger, dialogue, or timed event asks for one of the script procedures.

Most paths below are relative to `master.dat`, `critter.dat`, `patch000.dat`, or an unpacked Fallout data directory. Runtime scripts are normally loaded from `scripts\*.int` through [`scripts\scripts.lst`](scripts_lst.html). The matching source, when available to modders, is normally `*.ssl`; the matching dialogue text is often `text\ \dialog\*.msg`.

## Format Properties

| Property | Description |
| --- | --- |
| File type | Binary bytecode program. |
| Magic/version | No separate magic header. Normal Fallout scripts start with a known bootstrap instruction sequence. |
| Byte order | Big-endian for 16-bit opcodes and 32-bit integer fields. |
| Entry point | The procedure table always starts at file offset `0x2A` / decimal `42`. |
| Instruction size | Opcodes are 16-bit words. Some opcodes, especially typed literal pushes, consume a following 32-bit operand. |
| Strings | Length-prefixed, null-terminated, even-aligned byte strings in identifier and static-string tables. |
| Compression | The INT file itself is not compressed. It may be stored inside a compressed [DAT](dat.html) entry. |

## High-Level Layout

A normal INT file is best understood as a small bootstrap, a set of tables, and executable bytecode blocks whose addresses are absolute file offsets.

| Order | Section | How the runtime finds it |
| --- | --- | --- |
| 1 | Script bootstrap code | Fixed-size block at `0x0000..0x0029`. |
| 2 | Procedure table | Fixed start at `0x002A`. |
| 3 | Identifier table | `procedure_table + 4 + procedure_count * 24`. |
| 4 | Static string table | `identifier_table + identifier_table_size + 4`. |
| 5 | Initialization code and procedure bodies | Reached by jumps and by procedure record offsets. |

```text
program.procedures    = file + 42
procedure_count       = be32(program.procedures + 0)
program.identifiers   = program.procedures + 4 + procedure_count * 24
program.staticStrings = program.identifiers + be32(program.identifiers + 0) + 4
```

The runtime keeps the entire file in memory and uses file-relative instruction pointers. Procedure `body_offset` and `condition_offset` therefore point into the full INT file, not into a sliced code section.

## Bootstrap Code

The first 42 bytes are executable interpreter code emitted by the compiler. Traditional documentation calls this "script initialization code". The important practical point is that old engine code assumes the procedure table begins immediately after these 42 bytes.

| Offset | Size | Typical value | Meaning |
| --- | --- | --- | --- |
| `0x0000` | 2 | `0x8002` | Enter critical section. |
| `0x0002` | 2 | `0xC001` | Typed integer push. |
| `0x0004` | 4 | `0x00000012` | Return address used by the startup wrapper. |
| `0x0008` | 2 | `0x800D` | Move data-stack value to return/address stack. |
| `0x000A` | 2 | `0xC001` | Typed integer push. |
| `0x000C` | 4 | varies | Absolute file offset of object-initialization code. |
| `0x0010` | 2 | `0x8004` | Jump to the initialization code. |
| `0x0012` | 2 | `0x8010` | Exit program helper target. |
| `0x0014..0x0028` | 22 | pop/return helper opcodes | Small helper targets used by procedure-return paths. |

The exact bootstrap bytes are useful for recognizing normal compiler output, but a loader should still derive section positions the same way the engine does: the procedure table starts at byte `42`.

## Procedure Table

The procedure table starts with a 32-bit big-endian procedure count. It is followed by that many 24-byte records.

| Offset | Size | Field | Description |
| --- | --- | --- | --- |
| `0x00` | 4 | `name_offset` | Offset into the identifier table. It points to the first character of the procedure name. |
| `0x04` | 4 | `flags` | Procedure flags. |
| `0x08` | 4 | `time` | Scheduled time for timed procedures. |
| `0x0C` | 4 | `condition_offset` | Absolute file offset of condition code for conditional procedures. |
| `0x10` | 4 | `body_offset` | Absolute file offset of the procedure body. |
| `0x14` | 4 | `arg_count` | Number of procedure arguments. |

Compiler output traditionally includes a dummy first procedure named with periods. This keeps normal procedure indexes starting at `1`. Fallout 2 CE's execution path still treats a resolved procedure index of `0` specially and substitutes procedure index `1`, which matches the old dummy-slot convention.

### Procedure Flags

| Flag | Name | Description |
| --- | --- | --- |
| `0x01` | `P_TIMED` | Procedure is run later by the interpreter's timed procedure event scan. |
| `0x02` | `P_CONDITIONAL` | Procedure has condition code that is evaluated before the body runs. |
| `0x04` | `P_IMPORT` | Procedure is imported from another running/exporting program. |
| `0x08` | `P_EXPORT` | Procedure is exported for external lookup. |
| `0x10` | `P_CRITICAL` | Procedure runs inside the interpreter's critical-section mechanism. |

Timed and conditional procedure records exist in the interpreter, but old notes warn that conditional procedure compilation was unreliable in the original BIS tooling. Treat those flags as real VM features, not necessarily as common Fallout content.

## Identifier Table

The identifier table stores procedure names and script-level variable names. It does not store local variable names declared inside procedure bodies; those are lost at compilation time unless a decompiler invents names.

| Offset | Size | Field | Description |
| --- | --- | --- | --- |
| `0x00` | 4 | `total_size` | Byte length used by the runtime to find the next table. |
| `0x04` | 2 | `length0` | Length of the first stored string, including its null terminator and any padding. |
| `0x06` | `length0` | `string0` | Null-terminated bytes, padded to an even length. |
| ... | ... | more strings | Repeated `uint16 length` plus bytes. |
| after table data | 4 | `0xFFFFFFFF` | Traditional end marker. The runtime mainly relies on `total_size` for locating the static-string table. |

Procedure-record `name_offset` values are relative to the beginning of this identifier table and point at the first character, not at the 16-bit length word. For the first string in a normal table, that means an offset of `6`.

## Static String Table

The static string table stores string constants used by the script. It uses the same length-prefixed, even-aligned physical structure as the identifier table. A script with no string constants can still have an empty table.

Typed static-string literals in bytecode use `0x9001` followed by a 32-bit offset. The runtime resolves those offsets as `staticStrings + 4 + offset`, so string literal offsets are relative to the bytes after the table's 32-bit `total_size` field. Like identifier offsets, they point at the first character of the stored string, not at the preceding length word.

## Initialization Code

After the tables, the compiler emits script-object initialization code. This code sets up exported variables, external variables, exported procedures, global script storage, and the transfer to `start` if the script has one. It is ordinary bytecode reached by the bootstrap jump at `0x000C`.

Traditional docs describe this block as starting with `O_SET_GLOBAL` (`0x802C`) and then a sequence of typed values plus export/store opcodes. In practice, tools should not assume a single fixed length. Follow the bootstrap jump and procedure offsets instead.

## Imports and Exports

INT programs can export procedures and variables during initialization. Other scripts can then import those names and call or access them through the interpreter's external lookup system. This is separate from normal built-in map/object procedure dispatch: `talk_p_proc` and `map_enter_p_proc` are found inside one script, while imported/exported names are looked up across loaded programs.

| Feature | Bytecode marker | Runtime consequence |
| --- | --- | --- |
| Export variable | `0x8016` | Registers a named external variable supplied by this program. |
| Export procedure | `0x8017` | Registers a named procedure and its argument count for external calls. |
| Import procedure | `P_IMPORT` on a procedure record | Procedure execution resolves the name through the external procedure registry instead of jumping to a local body. |
| Store/fetch external | `0x8015` / `0x8014` | Writes or reads an exported variable by name. |

Imported calls are fragile when viewed as a file-format feature because successful execution depends on runtime state, not only on bytes in the INT file. If the exporting program has not been loaded or has not run its initialization code, the imported procedure or variable may not exist even though the importing INT parses correctly.

## Instruction Encoding

The interpreter reads a 16-bit big-endian opcode word. Opcode dispatch uses the lower opcode index; the high bits can also carry a value type. This is why a typed integer literal appears as `0xC001`: it is a typed form of the push opcode.

| Value | Name | Meaning |
| --- | --- | --- |
| `0x8001` | `OPCODE_PUSH` | Untyped/raw push opcode index. |
| `0xC001` | `VALUE_TYPE_INT` | Push following 32-bit value as an integer. |
| `0xA001` | `VALUE_TYPE_FLOAT` | Push following 32-bit value as a float value. |
| `0x9001` | `VALUE_TYPE_STRING` | Push following 32-bit offset into the static string table. |
| `0x9801` | `VALUE_TYPE_DYNAMIC_STRING` | Runtime-only dynamic string value. Normal file bytecode should not need to store this directly. |
| `0xE001` | `VALUE_TYPE_PTR` | Runtime pointer/object value. This is interpreter state, not a portable file reference. |

Most non-literal opcodes are standalone 16-bit words. Branches, calls, procedure lookups, and many game built-ins consume or produce values on the interpreter stack rather than carrying all arguments inline. A disassembler therefore needs to model stack effects, not just parse bytes sequentially.

## Opcode Reference

The tables below list the vanilla opcode numbers registered by Fallout 2 CE's interpreter sources. They are a useful compatibility baseline for disassemblers, decompilers, and documentation tools: `0x8000..0x804B` are core VM instructions, `0x804C..0x80A0` are generic interpreter-library helpers, and `0x80A1..0x8155` are Fallout game operations. sfall opcodes are documented separately after the vanilla tables.

Opcode words are stored big-endian in the file. Literal pushes often combine `OPCODE_PUSH` with a value type in the high bits, so a disassembler should decode typed push forms before falling back to the raw opcode number.

### Core VM Opcodes

| Opcode | Name | Role |
| --- | --- | --- |
| `0x8000` | `OPCODE_NOOP` | No operation. |
| `0x8001` | `OPCODE_PUSH` | Pushes an immediate value; usually appears with a value-type tag. |
| `0x8002` | `OPCODE_ENTER_CRITICAL_SECTION` | Enters an interpreter critical section. |
| `0x8003` | `OPCODE_LEAVE_CRITICAL_SECTION` | Leaves an interpreter critical section. |
| `0x8004` | `OPCODE_JUMP` | Transfers control to a bytecode address. |
| `0x8005` | `OPCODE_CALL` | Calls a local procedure or procedure address. |
| `0x8006` | `OPCODE_CALL_AT` | Schedules a call at a specified time. |
| `0x8007` | `OPCODE_CALL_WHEN` | Schedules a call when a condition becomes true. |
| `0x8008` | `OPCODE_CALLSTART` | Begins call setup. |
| `0x8009` | `OPCODE_EXEC` | Executes another program/procedure context. |
| `0x800A` | `OPCODE_SPAWN` | Spawns another program context. |
| `0x800B` | `OPCODE_FORK` | Forks execution. |
| `0x800C` | `OPCODE_A_TO_D` | Moves a value from the address stack to the data stack. |
| `0x800D` | `OPCODE_D_TO_A` | Moves a value from the data stack to the address stack. |
| `0x800E` | `OPCODE_EXIT` | Exits the current procedure context. |
| `0x800F` | `OPCODE_DETACH` | Detaches the current program from its caller/scheduler context. |
| `0x8010` | `OPCODE_EXIT_PROGRAM` | Terminates the current program. |
| `0x8011` | `OPCODE_STOP_PROGRAM` | Stops a program. |
| `0x8012` | `OPCODE_FETCH_GLOBAL` | Reads a global interpreter variable slot. |
| `0x8013` | `OPCODE_STORE_GLOBAL` | Writes a global interpreter variable slot. |
| `0x8014` | `OPCODE_FETCH_EXTERNAL` | Reads an exported variable by name. |
| `0x8015` | `OPCODE_STORE_EXTERNAL` | Writes an exported variable by name. |
| `0x8016` | `OPCODE_EXPORT_VARIABLE` | Registers a variable export during initialization. |
| `0x8017` | `OPCODE_EXPORT_PROCEDURE` | Registers a procedure export during initialization. |
| `0x8018` | `OPCODE_SWAP` | Swaps stack values. |
| `0x8019` | `OPCODE_SWAPA` | Swaps address-stack values. |
| `0x801A` | `OPCODE_POP` | Pops and discards a value. |
| `0x801B` | `OPCODE_DUP` | Duplicates the top stack value. |
| `0x801C` | `OPCODE_POP_RETURN` | Pops a return address and returns. |
| `0x801D` | `OPCODE_POP_EXIT` | Pops state and exits the procedure. |
| `0x801E` | `OPCODE_POP_ADDRESS` | Pops an address value. |
| `0x801F` | `OPCODE_POP_FLAGS` | Pops procedure flags/state. |
| `0x8020` | `OPCODE_POP_FLAGS_RETURN` | Pops flags and returns. |
| `0x8021` | `OPCODE_POP_FLAGS_EXIT` | Pops flags and exits. |
| `0x8022` | `OPCODE_POP_FLAGS_RETURN_EXTERN` | Pops flags and returns from an external call. |
| `0x8023` | `OPCODE_POP_FLAGS_EXIT_EXTERN` | Pops flags and exits from an external call. |
| `0x8024` | `OPCODE_POP_FLAGS_RETURN_VAL_EXTERN` | Pops flags and returns a value from an external call. |
| `0x8025` | `OPCODE_POP_FLAGS_RETURN_VAL_EXIT` | Pops flags, returns a value, and exits. |
| `0x8026` | `OPCODE_POP_FLAGS_RETURN_VAL_EXIT_EXTERN` | Pops flags, returns a value, and exits from an external call. |
| `0x8027` | `OPCODE_CHECK_PROCEDURE_ARGUMENT_COUNT` | Checks the number of arguments supplied to a procedure. |
| `0x8028` | `OPCODE_LOOKUP_PROCEDURE_BY_NAME` | Looks up a procedure by identifier string. |
| `0x8029` | `OPCODE_POP_BASE` | Pops the current stack base/frame pointer. |
| `0x802A` | `OPCODE_POP_TO_BASE` | Pops values back to the current base/frame pointer. |
| `0x802B` | `OPCODE_PUSH_BASE` | Pushes the current stack base/frame pointer. |
| `0x802C` | `OPCODE_SET_GLOBAL` | Sets up global variable storage. |
| `0x802D` | `OPCODE_FETCH_PROCEDURE_ADDRESS` | Fetches a procedure's bytecode address. |
| `0x802E` | `OPCODE_DUMP` | Debug/dump helper. |
| `0x802F` | `OPCODE_IF` | Conditional branch helper. |
| `0x8030` | `OPCODE_WHILE` | Loop branch helper. |
| `0x8031` | `OPCODE_STORE` | Stores a value in a local/global slot. |
| `0x8032` | `OPCODE_FETCH` | Fetches a value from a local/global slot. |
| `0x8033` | `OPCODE_EQUAL` | Compares for equality. |
| `0x8034` | `OPCODE_NOT_EQUAL` | Compares for inequality. |
| `0x8035` | `OPCODE_LESS_THAN_EQUAL` | Compares less-than-or-equal. |
| `0x8036` | `OPCODE_GREATER_THAN_EQUAL` | Compares greater-than-or-equal. |
| `0x8037` | `OPCODE_LESS_THAN` | Compares less-than. |
| `0x8038` | `OPCODE_GREATER_THAN` | Compares greater-than. |
| `0x8039` | `OPCODE_ADD` | Adds two values or concatenates strings where supported. |
| `0x803A` | `OPCODE_SUB` | Subtracts values. |
| `0x803B` | `OPCODE_MUL` | Multiplies values. |
| `0x803C` | `OPCODE_DIV` | Divides values. |
| `0x803D` | `OPCODE_MOD` | Computes remainder. |
| `0x803E` | `OPCODE_AND` | Logical AND. |
| `0x803F` | `OPCODE_OR` | Logical OR. |
| `0x8040` | `OPCODE_BITWISE_AND` | Bitwise AND. |
| `0x8041` | `OPCODE_BITWISE_OR` | Bitwise OR. |
| `0x8042` | `OPCODE_BITWISE_XOR` | Bitwise XOR. |
| `0x8043` | `OPCODE_BITWISE_NOT` | Bitwise NOT. |
| `0x8044` | `OPCODE_FLOOR` | Floors a numeric value. |
| `0x8045` | `OPCODE_NOT` | Logical NOT. |
| `0x8046` | `OPCODE_NEGATE` | Arithmetic negation. |
| `0x8047` | `OPCODE_WAIT` | Suspends execution for a delay. |
| `0x8048` | `OPCODE_CANCEL` | Cancels a scheduled event/call. |
| `0x8049` | `OPCODE_CANCEL_ALL` | Cancels scheduled events/calls. |
| `0x804A` | `OPCODE_START_CRITICAL` | Starts a critical section for script scheduling. |
| `0x804B` | `OPCODE_END_CRITICAL` | Ends a critical section for script scheduling. |

### Vanilla Interpreter-Library Opcodes

These opcodes come from the generic interpreter library rather than the Fallout-specific game API. They cover dialogue window helpers, window drawing, movies, regions, buttons, mouse/input hooks, named events, sound handles, and simple utility routines. The gaps at `0x807D` and `0x807E` are not registered in Fallout 2 CE's library table.

| Opcode | Handler | Category |
| --- | --- | --- |
| `0x804C` | `opSayQuit` | Dialogue UI |
| `0x804D` | `opSayEnd` | Dialogue UI |
| `0x804E` | `opSayStart` | Dialogue UI |
| `0x804F` | `opSayStartPos` | Dialogue UI |
| `0x8050` | `opSayReplyTitle` | Dialogue UI |
| `0x8051` | `opSayGoToReply` | Dialogue UI |
| `0x8052` | `opSayOption` | Dialogue UI |
| `0x8053` | `opSayReply` | Dialogue UI |
| `0x8054` | `opSayMessage` | Dialogue UI |
| `0x8055` | `opSayReplyWindow` | Dialogue UI |
| `0x8056` | `opSayOptionWindow` | Dialogue UI |
| `0x8057` | `opSayBorder` | Dialogue UI |
| `0x8058` | `opSayScrollUp` | Dialogue UI |
| `0x8059` | `opSayScrollDown` | Dialogue UI |
| `0x805A` | `opSaySetSpacing` | Dialogue UI |
| `0x805B` | `opSayOptionColor` | Dialogue UI |
| `0x805C` | `opSayReplyColor` | Dialogue UI |
| `0x805D` | `opSayRestart` | Dialogue UI |
| `0x805E` | `opSayGetLastPos` | Dialogue UI |
| `0x805F` | `opSayReplyFlags` | Dialogue UI |
| `0x8060` | `opSayOptionFlags` | Dialogue UI |
| `0x8061` | `opSayMessageTimeout` | Dialogue UI |
| `0x8062` | `opCreateWin` | Window/display |
| `0x8063` | `opDeleteWin` | Window/display |
| `0x8064` | `opSelect` | Window/display |
| `0x8065` | `opResizeWin` | Window/display |
| `0x8066` | `opScaleWin` | Window/display |
| `0x8067` | `opShowWin` | Window/display |
| `0x8068` | `opFillWin` | Window/display |
| `0x8069` | `opFillRect` | Window/display |
| `0x806A` | `opFillWin3x3` | Window/display |
| `0x806B` | `opDisplay` | Window/display |
| `0x806C` | `opDisplayGfx` | Window/display |
| `0x806D` | `opDisplayRaw` | Window/display |
| `0x806E` | `opLoadPaletteTable` | Palette/display |
| `0x806F` | `opFadeIn` | Palette/display |
| `0x8070` | `opFadeOut` | Palette/display |
| `0x8071` | `opGotoXY` | Text drawing |
| `0x8072` | `opPrint` | Text drawing |
| `0x8073` | `opFormat` | Text drawing |
| `0x8074` | `opPrintRect` | Text drawing |
| `0x8075` | `opSetFont` | Text drawing |
| `0x8076` | `opSetTextFlags` | Text drawing |
| `0x8077` | `opSetTextColor` | Text drawing |
| `0x8078` | `opSetHighlightColor` | Text drawing |
| `0x8079` | `opStopMovie` | Movie |
| `0x807A` | `opPlayMovie` | Movie |
| `0x807B` | `opSetMovieFlags` | Movie |
| `0x807C` | `opPlayMovieRect` | Movie |
| `0x807F` | `opAddRegion` | Regions |
| `0x8080` | `opAddRegionFlag` | Regions |
| `0x8081` | `opAddRegionProc` | Regions |
| `0x8082` | `opAddRegionRightProc` | Regions |
| `0x8083` | `opDeleteRegion` | Regions |
| `0x8084` | `opActivateRegion` | Regions |
| `0x8085` | `opCheckRegion` | Regions |
| `0x8086` | `opAddButton` | Buttons |
| `0x8087` | `opAddButtonText` | Buttons |
| `0x8088` | `opAddButtonFlag` | Buttons |
| `0x8089` | `opAddButtonGfx` | Buttons |
| `0x808A` | `opAddButtonProc` | Buttons |
| `0x808B` | `opAddButtonRightProc` | Buttons |
| `0x808C` | `opDeleteButton` | Buttons |
| `0x808D` | `opHideMouse` | Mouse/input |
| `0x808E` | `opShowMouse` | Mouse/input |
| `0x808F` | `opMouseShape` | Mouse/input |
| `0x8090` | `opRefreshMouse` | Mouse/input |
| `0x8091` | `opSetGlobalMouseFunc` | Mouse/input |
| `0x8092` | `opAddNamedEvent` | Named events |
| `0x8093` | `opAddNamedHandler` | Named events |
| `0x8094` | `opClearNamed` | Named events |
| `0x8095` | `opSignalNamed` | Named events |
| `0x8096` | `opAddKey` | Keyboard/input |
| `0x8097` | `opDeleteKey` | Keyboard/input |
| `0x8098` | `opSoundPlay` | Sound |
| `0x8099` | `opSoundPause` | Sound |
| `0x809A` | `opSoundResume` | Sound |
| `0x809B` | `opSoundStop` | Sound |
| `0x809C` | `opSoundRewind` | Sound |
| `0x809D` | `opSoundDelete` | Sound |
| `0x809E` | `opSetOneOptPause` | Dialogue/UI option behavior |
| `0x809F` | `opSelectFileList` | File list UI |
| `0x80A0` | `opTokenize` | String utility |

### Vanilla Fallout Game Opcodes

These are the game-facing SSL operations. The "SSL name" column uses the names found in older opcode references and script compilers where available; the "CE handler" column gives the corresponding Fallout 2 CE implementation symbol. Argument counts and exact stack effects are a runtime/API concern, so this table is best treated as an opcode identity and compatibility map.

| Opcode | SSL name | CE handler |
| --- | --- | --- |
| `0x80A1` | `give_exp_points` | `opGiveExpPoints` |
| `0x80A2` | `scr_return` | `opScrReturn` |
| `0x80A3` | `play_sfx` | `opPlaySfx` |
| `0x80A4` | `obj_name` | `opGetObjectName` |
| `0x80A5` | `sfx_build_open_name` | `opSfxBuildOpenName` |
| `0x80A6` | `get_pc_stat` | `opGetPcStat` |
| `0x80A7` | `tile_contains_pid_obj` | `opTileGetObjectWithPid` |
| `0x80A8` | `set_map_start` | `opSetMapStart` |
| `0x80A9` | `override_map_start` | `opOverrideMapStart` |
| `0x80AA` | `has_skill` | `opHasSkill` |
| `0x80AB` | `using_skill` | `opUsingSkill` |
| `0x80AC` | `roll_vs_skill` | `opRollVsSkill` |
| `0x80AD` | `skill_contest` | `opSkillContest` |
| `0x80AE` | `do_check` | `opDoCheck` |
| `0x80AF` | `success` | `opSuccess` |
| `0x80B0` | `critical` | `opCritical` |
| `0x80B1` | `how_much` | `opHowMuch` |
| `0x80B2` | `mark_area_known` | `opMarkAreaKnown` |
| `0x80B3` | `reaction_influence` | `opReactionInfluence` |
| `0x80B4` | `random` | `opRandom` |
| `0x80B5` | `roll_dice` | `opRollDice` |
| `0x80B6` | `move_to` | `opMoveTo` |
| `0x80B7` | `create_object` | `opCreateObject` |
| `0x80B8` | `display_msg` | `opDisplayMsg` |
| `0x80B9` | `script_overrides` | `opScriptOverrides` |
| `0x80BA` | `obj_is_carrying_obj` | `opObjectIsCarryingObjectWithPid` |
| `0x80BB` | `tile_contains_obj_pid` | `opTileContainsObjectWithPid` |
| `0x80BC` | `self_obj` | `opGetSelf` |
| `0x80BD` | `source_obj` | `opGetSource` |
| `0x80BE` | `target_obj` | `opGetTarget` |
| `0x80BF` | `dude_obj` | `opGetDude` |
| `0x80C0` | `obj_being_used_with` | `opGetObjectBeingUsed` |
| `0x80C1` | `get_local_var` | `opGetLocalVar` |
| `0x80C2` | `set_local_var` | `opSetLocalVar` |
| `0x80C3` | `get_map_var` | `opGetMapVar` |
| `0x80C4` | `set_map_var` | `opSetMapVar` |
| `0x80C5` | `get_global_var` | `opGetGlobalVar` |
| `0x80C6` | `set_global_var` | `opSetGlobalVar` |
| `0x80C7` | `script_action` | `opGetScriptAction` |
| `0x80C8` | `obj_type` | `opGetObjectType` |
| `0x80C9` | `item_subtype` | `opGetItemType` |
| `0x80CA` | `get_critter_stat` | `opGetCritterStat` |
| `0x80CB` | `set_critter_stat` | `opSetCritterStat` |
| `0x80CC` | `animate_stand_obj` | `opAnimateStand` |
| `0x80CD` | `animate_stand_reverse_obj` | `opAnimateStandReverse` |
| `0x80CE` | `animate_move_obj_to_tile` | `opAnimateMoveObjectToTile` |
| `0x80CF` | `tile_in_tile_rect` | `opTileInTileRect` |
| `0x80D0` | `attack_complex` | `opAttackComplex` |
| `0x80D1` | `make_daytime` | `opMakeDayTime` |
| `0x80D2` | `tile_distance` | `opTileDistanceBetween` |
| `0x80D3` | `tile_distance_objs` | `opTileDistanceBetweenObjects` |
| `0x80D4` | `tile_num` | `opGetObjectTile` |
| `0x80D5` | `tile_num_in_direction` | `opGetTileInDirection` |
| `0x80D6` | `pickup_obj` | `opPickup` |
| `0x80D7` | `drop_obj` | `opDrop` |
| `0x80D8` | `add_obj_to_inven` | `opAddObjectToInventory` |
| `0x80D9` | `rm_obj_from_inven` | `opRemoveObjectFromInventory` |
| `0x80DA` | `wield_obj_critter` | `opWieldItem` |
| `0x80DB` | `use_obj` | `opUseObject` |
| `0x80DC` | `obj_can_see_obj` | `opObjectCanSeeObject` |
| `0x80DD` | `attack` | `opAttackComplex` |
| `0x80DE` | `start_gdialog` | `opStartGameDialog` |
| `0x80DF` | `end_gdialog` | `opEndGameDialog` |
| `0x80E0` | `dialogue_reaction` | `opGameDialogReaction` |
| `0x80E1` | `metarule3` | `opMetarule3` |
| `0x80E2` | `set_map_music` | `opSetMapMusic` |
| `0x80E3` | `set_obj_visibility` | `opSetObjectVisibility` |
| `0x80E4` | `load_map` | `opLoadMap` |
| `0x80E5` | `wm_area_set_pos` | `opWorldmapCitySetPos` |
| `0x80E6` | `set_exit_grids` | `opSetExitGrids` |
| `0x80E7` | `anim_busy` | `opAnimBusy` |
| `0x80E8` | `critter_heal` | `opCritterHeal` |
| `0x80E9` | `set_light_level` | `opSetLightLevel` |
| `0x80EA` | `game_time` | `opGetGameTime` |
| `0x80EB` | `game_time_in_seconds` | `opGetGameTimeInSeconds` |
| `0x80EC` | `elevation` | `opGetObjectElevation` |
| `0x80ED` | `kill_critter` | `opKillCritter` |
| `0x80EE` | `kill_critter_type` | `opKillCritterType` |
| `0x80EF` | `critter_damage` | `opCritterDamage` |
| `0x80F0` | `add_timer_event` | `opAddTimerEvent` |
| `0x80F1` | `rm_timer_event` | `opRemoveTimerEvent` |
| `0x80F2` | `game_ticks` | `opGameTicks` |
| `0x80F3` | `has_trait` | `opHasTrait` |
| `0x80F4` | `destroy_object` | `opDestroyObject` |
| `0x80F5` | `obj_can_hear_obj` | `opObjectCanHearObject` |
| `0x80F6` | `game_time_hour` | `opGameTimeHour` |
| `0x80F7` | `fixed_param` | `opGetFixedParam` |
| `0x80F8` | `tile_is_visible` | `opTileIsVisible` |
| `0x80F9` | `dialogue_system_enter` | `opGameDialogSystemEnter` |
| `0x80FA` | `action_being_used` | `opGetActionBeingUsed` |
| `0x80FB` | `critter_state` | `opGetCritterState` |
| `0x80FC` | `game_time_advance` | `opGameTimeAdvance` |
| `0x80FD` | `radiation_inc` | `opRadiationIncrease` |
| `0x80FE` | `radiation_dec` | `opRadiationDecrease` |
| `0x80FF` | `critter_attempt_placement` | `opCritterAttemptPlacement` |
| `0x8100` | `obj_pid` | `opGetObjectPid` |
| `0x8101` | `cur_map_index` | `opGetCurrentMap` |
| `0x8102` | `critter_add_trait` | `opCritterAddTrait` |
| `0x8103` | `critter_rm_trait` | `opCritterRemoveTrait` |
| `0x8104` | `proto_data` | `opGetProtoData` |
| `0x8105` | `message_str` | `opGetMessageString` |
| `0x8106` | `critter_inven_obj` | `opCritterGetInventoryObject` |
| `0x8107` | `obj_set_light_level` | `opSetObjectLightLevel` |
| `0x8108` | `scripts_request_world_map` | `opWorldmap` |
| `0x8109` | `inven_cmds` | `_op_inven_cmds` |
| `0x810A` | `float_msg` | `opFloatMessage` |
| `0x810B` | `metarule` | `opMetarule` |
| `0x810C` | `anim` | `opAnim` |
| `0x810D` | `obj_carrying_pid_obj` | `opObjectCarryingObjectByPid` |
| `0x810E` | `reg_anim_func` | `opRegAnimFunc` |
| `0x810F` | `reg_anim_animate` | `opRegAnimAnimate` |
| `0x8110` | `reg_anim_animate_reverse` | `opRegAnimAnimateReverse` |
| `0x8111` | `reg_anim_obj_move_to_obj` | `opRegAnimObjectMoveToObject` |
| `0x8112` | `reg_anim_obj_run_to_obj` | `opRegAnimObjectRunToObject` |
| `0x8113` | `reg_anim_obj_move_to_tile` | `opRegAnimObjectMoveToTile` |
| `0x8114` | `reg_anim_obj_run_to_tile` | `opRegAnimObjectRunToTile` |
| `0x8115` | `play_gmovie` | `opPlayGameMovie` |
| `0x8116` | `add_mult_objs_to_inven` | `opAddMultipleObjectsToInventory` |
| `0x8117` | `rm_mult_objs_from_inven` | `opRemoveMultipleObjectsFromInventory` |
| `0x8118` | `month` | `opGetMonth` |
| `0x8119` | `day` | `opGetDay` |
| `0x811A` | `explosion` | `opExplosion` |
| `0x811B` | `days_since_visited` | `opGetDaysSinceLastVisit` |
| `0x811C` | `gsay_start` | `_op_gsay_start` |
| `0x811D` | `gsay_end` | `_op_gsay_end` |
| `0x811E` | `gsay_reply` | `_op_gsay_reply` |
| `0x811F` | `gsay_option` | `_op_gsay_option` |
| `0x8120` | `gsay_message` | `_op_gsay_message` |
| `0x8121` | `giq_option` | `_op_giq_option` |
| `0x8122` | `poison` | `opPoison` |
| `0x8123` | `get_poison` | `opGetPoison` |
| `0x8124` | `party_add` | `opPartyAdd` |
| `0x8125` | `party_remove` | `opPartyRemove` |
| `0x8126` | `reg_anim_animate_forever` | `opRegAnimAnimateForever` |
| `0x8127` | `critter_injure` | `opCritterInjure` |
| `0x8128` | `is_in_combat` | `opCombatIsInitialized` |
| `0x8129` | `gdialog_barter` | `_op_gdialog_barter` |
| `0x812A` | `game_difficulty` | `opGetGameDifficulty` |
| `0x812B` | `running_burning_guy` | `opGetRunningBurningGuy` |
| `0x812C` | `inven_unwield` | `_op_inven_unwield` |
| `0x812D` | `obj_is_locked` | `opObjectIsLocked` |
| `0x812E` | `obj_lock` | `opObjectLock` |
| `0x812F` | `obj_unlock` | `opObjectUnlock` |
| `0x8130` | `obj_is_open` | `opObjectIsOpen` |
| `0x8131` | `obj_open` | `opObjectOpen` |
| `0x8132` | `obj_close` | `opObjectClose` |
| `0x8133` | `game_ui_disable` | `opGameUiDisable` |
| `0x8134` | `game_ui_enable` | `opGameUiEnable` |
| `0x8135` | `game_ui_is_disabled` | `opGameUiIsDisabled` |
| `0x8136` | `gfade_out` | `opGameFadeOut` |
| `0x8137` | `gfade_in` | `opGameFadeIn` |
| `0x8138` | `item_caps_total` | `opItemCapsTotal` |
| `0x8139` | `item_caps_adjust` | `opItemCapsAdjust` |
| `0x813A` | `anim_action_frame` | `_op_anim_action_frame` |
| `0x813B` | `reg_anim_play_sfx` | `opRegAnimPlaySfx` |
| `0x813C` | `critter_mod_skill` | `opCritterModifySkill` |
| `0x813D` | `sfx_build_char_name` | `opSfxBuildCharName` |
| `0x813E` | `sfx_build_ambient_name` | `opSfxBuildAmbientName` |
| `0x813F` | `sfx_build_interface_name` | `opSfxBuildInterfaceName` |
| `0x8140` | `sfx_build_item_name` | `opSfxBuildItemName` |
| `0x8141` | `sfx_build_weapon_name` | `opSfxBuildWeaponName` |
| `0x8142` | `sfx_build_scenery_name` | `opSfxBuildSceneryName` |
| `0x8143` | `attack_setup` | `opAttackSetup` |
| `0x8144` | `destroy_mult_objs` | `opDestroyMultipleObjects` |
| `0x8145` | `use_obj_on_obj` | `opUseObjectOnObject` |
| `0x8146` | `endgame_slideshow` | `opEndgameSlideshow` |
| `0x8147` | `move_obj_inven_to_obj` | `opMoveObjectInventoryToObject` |
| `0x8148` | `endgame_movie` | `opEndgameMovie` |
| `0x8149` | `obj_art_fid` | `opGetObjectFid` |
| `0x814A` | `art_anim` | `opGetFidAnim` |
| `0x814B` | `party_member_obj` | `opGetPartyMember` |
| `0x814C` | `rotation_to_tile` | `opGetRotationToTile` |
| `0x814D` | `jam_lock` | `opJamLock` |
| `0x814E` | `gdialog_set_barter_mod` | `opGameDialogSetBarterMod` |
| `0x814F` | `combat_difficulty` | `opGetCombatDifficulty` |
| `0x8150` | `obj_on_screen` | `opObjectOnScreen` |
| `0x8151` | `critter_is_fleeing` | `opCritterIsFleeing` |
| `0x8152` | `critter_set_flee_state` | `opCritterSetFleeState` |
| `0x8153` | `terminate_combat` | `opTerminateCombat` |
| `0x8154` | `debug_msg` | `opDebugMessage` |
| `0x8155` | `critter_stop_attacking` | `opCritterStopAttacking` |

## sfall Extended Opcodes

sfall extends the Fallout 2 scripting VM with additional opcodes and compiler-visible functions. These are real INT bytecode operations in sfall-enabled runtimes, but they are not portable to the original executable unless a compatibility layer implements them. Some higher-level sfall functions are also exposed through `sfall_func0` through `sfall_func8`, where the function name is a string argument rather than a dedicated opcode.

Rows marked with `*` require `AllowUnsafeScripting` in `ddraw.ini`. They expose raw memory writes or direct calls into executable addresses, so they are powerful but intentionally gated.

The table below expands the current sfall opcode artifact into one row per published opcode. It intentionally keeps the published signature spelling and type style. One entry in that artifact currently lists `set_perk_int` as `0x8196`, which collides with `set_target_knockback`; tools should treat that as a version/source-list caveat and check the target sfall headers or compiler definitions.

| Opcode | Signature or operator |
| --- | --- |
| `0x8156` | `int read_byte(int address)` |
| `0x8157` | `int read_short(int address)` |
| `0x8158` | `int read_int(int address)` |
| `0x8159` | `string read_string(int address)` |
| `*0x81CF` | `void write_byte(int address, int value)` |
| `*0x81D0` | `void write_short(int address, int value)` |
| `*0x81D1` | `void write_int(int address, int value)` |
| `*0x821B` | `void write_string(int address, string value)` |
| `*0x81D2` | `void call_offset_v0(int address)` |
| `*0x81D3` | `void call_offset_v1(int address, int arg1)` |
| `*0x81D4` | `void call_offset_v2(int address, int arg1, int arg2)` |
| `*0x81D5` | `void call_offset_v3(int address, int arg1, int arg2, int arg3)` |
| `*0x81D6` | `void call_offset_v4(int address, int arg1, int arg2, int arg3, int arg4)` |
| `*0x81D7` | `int call_offset_r0(int address)` |
| `*0x81D8` | `int call_offset_r1(int address, int arg1)` |
| `*0x81D9` | `int call_offset_r2(int address, int arg1, int arg2)` |
| `*0x81DA` | `int call_offset_r3(int address, int arg1, int arg2, int arg3)` |
| `*0x81DB` | `int call_offset_r4(int address, int arg1, int arg2, int arg3, int arg4)` |
| `0x815A` | `void set_pc_base_stat(int StatID, int value)` |
| `0x815B` | `void set_pc_extra_stat(int StatID, int value)` |
| `0x815C` | `int get_pc_base_stat(int StatID)` |
| `0x815D` | `int get_pc_extra_stat(int StatID)` |
| `0x815E` | `void set_critter_base_stat(object, int StatID, int value)` |
| `0x815F` | `void set_critter_extra_stat(object, int StatID, int value)` |
| `0x8160` | `int get_critter_base_stat(object, int StatID)` |
| `0x8161` | `int get_critter_extra_stat(object, int StatID)` |
| `0x8242` | `void set_critter_skill_points(int critter, int skill, int value)` |
| `0x8243` | `int get_critter_skill_points(int critter, int skill)` |
| `0x8244` | `void set_available_skill_points(int value)` |
| `0x8245` | `int get_available_skill_points` |
| `0x8246` | `void mod_skill_points_per_level(int value)` |
| `0x81B4` | `void set_stat_max(int stat, int value)` |
| `0x81B5` | `void set_stat_min(int stat, int value)` |
| `0x81B7` | `void set_pc_stat_max(int stat, int value)` |
| `0x81B8` | `void set_pc_stat_min(int stat, int value)` |
| `0x81B9` | `void set_npc_stat_max(int stat, int value)` |
| `0x81BA` | `void set_npc_stat_min(int stat, int value)` |
| `0x816B` | `int input_funcs_available` |
| `0x816C` | `int key_pressed(int dxScancode)` |
| `0x8162` | `void tap_key(int dxScancode)` |
| `0x821C` | `int get_mouse_x` |
| `0x821D` | `int get_mouse_y` |
| `0x821E` | `int get_mouse_buttons` |
| `0x821F` | `int get_window_under_mouse` |
| `0x8163` | `int get_year` |
| `0x8164` | `bool game_loaded` |
| `0x8165` | `bool graphics_funcs_available` |
| `0x8166` | `int load_shader(string path)` |
| `0x8167` | `void free_shader(int ID)` |
| `0x8168` | `void activate_shader(int ID)` |
| `0x8169` | `void deactivate_shader(int ID)` |
| `0x816D` | `void set_shader_int(int ID, string param, int value)` |
| `0x816E` | `void set_shader_float(int ID, string param, float value)` |
| `0x816F` | `void set_shader_vector(int ID, string param, float f1, float f2, float f3, float f4)` |
| `0x81AD` | `int get_shader_version` |
| `0x81AE` | `void set_shader_mode(int mode)` |
| `0x81B0` | `void force_graphics_refresh(bool enabled)` |
| `0x81B1` | `int get_shader_texture(int ID, int texture)` |
| `0x81B2` | `void set_shader_texture(int ID, string param, int texID)` |
| `0x816A` | `void set_global_script_repeat(int frames)` |
| `0x819B` | `void set_global_script_type(int type)` |
| `0x819C` | `int available_global_script_types` |
| `0x8170` | `bool in_world_map` |
| `0x8171` | `void force_encounter(int map)` |
| `0x8229` | `void force_encounter_with_flags(int map, int flags)` |
| `0x822A` | `void set_map_time_multi(float multi)` |
| `0x8172` | `void set_world_map_pos(int x, int y)` |
| `0x8173` | `int get_world_map_x_pos` |
| `0x8174` | `int get_world_map_y_pos` |
| `0x8175` | `void set_dm_model(string name)` |
| `0x8176` | `void set_df_model(string name)` |
| `0x8177` | `void set_movie_path(string filename, int movieid)` |
| `0x8178` | `void set_perk_image(int perkID, int value)` |
| `0x8179` | `void set_perk_ranks(int perkID, int value)` |
| `0x817A` | `void set_perk_level(int perkID, int value)` |
| `0x817B` | `void set_perk_stat(int perkID, int value)` |
| `0x817C` | `void set_perk_stat_mag(int perkID, int value)` |
| `0x817D` | `void set_perk_skill1(int perkID, int value)` |
| `0x817E` | `void set_perk_skill1_mag(int perkID, int value)` |
| `0x817F` | `void set_perk_type(int perkID, int value)` |
| `0x8180` | `void set_perk_skill2(int perkID, int value)` |
| `0x8181` | `void set_perk_skill2_mag(int perkID, int value)` |
| `0x8182` | `void set_perk_str(int perkID, int value)` |
| `0x8183` | `void set_perk_per(int perkID, int value)` |
| `0x8184` | `void set_perk_end(int perkID, int value)` |
| `0x8185` | `void set_perk_chr(int perkID, int value)` |
| `0x8196` | `void set_perk_int(int perkID, int value)` |
| `0x8187` | `void set_perk_agl(int perkID, int value)` |
| `0x8188` | `void set_perk_lck(int perkID, int value)` |
| `0x8189` | `void set_perk_name(int perkID, string value)` |
| `0x818A` | `void set_perk_desc(int perkID, string value)` |
| `0x8247` | `void set_perk_freq(int value)` |
| `0x818B` | `void set_pipboy_available(int available)` |
| `0x818C` | `int get_kill_counter(int critterType)` |
| `0x818D` | `void mod_kill_counter(int critterType, int amount)` |
| `0x818E` | `int get_perk_owed` |
| `0x818F` | `void set_perk_owed(int value)` |
| `0x8190` | `int get_perk_available(int perk)` |
| `0x8191` | `int get_critter_current_ap(object critter)` |
| `0x8192` | `void set_critter_current_ap(object critter, int ap)` |
| `0x8193` | `int active_hand` |
| `0x8194` | `void toggle_active_hand` |
| `0x8195` | `void set_weapon_knockback(object weapon, int type, int/float value)` |
| `0x8196` | `void set_target_knockback(object critter, int type, int/float value)` |
| `0x8197` | `void set_attacker_knockback(object critter, int type, int/float value)` |
| `0x8198` | `void remove_weapon_knockback(object weapon)` |
| `0x8199` | `void remove_target_knockback(object critter)` |
| `0x819A` | `void remove_attacker_knockback(object critter)` |
| `0x819D` | `void set_sfall_global(string/int varname, int/float value)` |
| `0x819E` | `int get_sfall_global_int(string/int varname)` |
| `0x819F` | `float get_sfall_global_float(string/int varname)` |
| `0x822D` | `int create_array(int element_count, int flags)` |
| `0x822E` | `void set_array(int array, any element, any value)` |
| `0x822F` | `any get_array(int array, any element)` |
| `0x8230` | `void free_array(int array)` |
| `0x8231` | `int len_array(int array)` |
| `0x8232` | `void resize_array(int array, int new_element_count)` |
| `0x8233` | `int temp_array(int element_count, int flags)` |
| `0x8234` | `void fix_array(int array)` |
| `0x8239` | `int scan_array(int array, int/float var)` |
| `0x8254` | `void save_array(any key, int array)` |
| `0x8255` | `int load_array(any key)` |
| `0x8256` | `int array_key(int array, int index)` |
| `0x8257` | `int arrayexpr(any key, any value)` |
| `0x81A0` | `void set_pickpocket_max(int percentage)` |
| `0x81A1` | `void set_hit_chance_max(int percentage)` |
| `0x81A2` | `void set_skill_max(int value)` |
| `0x81AA` | `void set_xp_mod(int percentage)` |
| `0x81AB` | `void set_perk_level_mod(int levels)` |
| `0x81C5` | `void set_critter_hit_chance_mod(object, int max, int mod)` |
| `0x81C6` | `void set_base_hit_chance_mod(int max, int mod)` |
| `0x81C7` | `void set_critter_skill_mod(object, int max)` |
| `0x81C8` | `void set_base_skill_mod(int max)` |
| `0x81C9` | `void set_critter_pickpocket_mod(object, int max, int mod)` |
| `0x81CA` | `void set_base_pickpocket_mod(int max, int mod)` |
| `0x81A3` | `int eax_available` |
| `0x81A4` | `void set_eax_environment(int environment)` |
| `0x81A5` | `void inc_npc_level(int pid/string name)` |
| `0x8241` | `int get_npc_level(int pid/string name)` |
| `0x81A6` | `int get_viewport_x` |
| `0x81A7` | `int get_viewport_y` |
| `0x81A8` | `void set_viewport_x(int view_x)` |
| `0x81A9` | `void set_viewport_y(int view_y)` |
| `0x81AC` | `int get_ini_setting(string setting)` |
| `0x81EB` | `string get_ini_string(string setting)` |
| `0x81AF` | `int get_game_mode` |
| `0x81B3` | `int get_uptime` |
| `0x81B6` | `void set_car_current_town(int town)` |
| `0x81BB` | `void set_fake_perk(string name, int level, int image, string desc)` |
| `0x81BC` | `void set_fake_trait(string name, int active, int image, string desc)` |
| `0x81BD` | `void set_selectable_perk(string name, int active, int image, string desc)` |
| `0x81BE` | `void set_perkbox_title(string title)` |
| `0x81BF` | `void hide_real_perks` |
| `0x81C0` | `void show_real_perks` |
| `0x81C1` | `int has_fake_perk(string name/int extraPerkID)` |
| `0x81C2` | `int has_fake_trait(string name)` |
| `0x81C3` | `void perk_add_mode(int type)` |
| `0x81C4` | `void clear_selectable_perks` |
| `0x8225` | `void remove_trait(int traitID)` |
| `0x81CB` | `void set_pyromaniac_mod(int bonus)` |
| `0x81CC` | `void apply_heaveho_fix` |
| `0x81CD` | `void set_swiftlearner_mod(int bonus)` |
| `0x81CE` | `void set_hp_per_level_mod(int mod)` |
| `0x81DC` | `void show_iface_tag(int tag)` |
| `0x81DD` | `void hide_iface_tag(int tag)` |
| `0x81DE` | `int is_iface_tag_active(int tag)` |
| `0x81DF` | `int get_bodypart_hit_modifier(int bodypart)` |
| `0x81E0` | `void set_bodypart_hit_modifier(int bodypart, int value)` |
| `0x81E1` | `void set_critical_table(int crittertype, int bodypart, int level, int valuetype, int value)` |
| `0x81E2` | `int get_critical_table(int crittertype, int bodypart, int level, int valuetype)` |
| `0x81E3` | `void reset_critical_table(int crittertype, int bodypart, int level, int valuetype)` |
| `0x81E4` | `int get_sfall_arg` |
| `0x823C` | `array get_sfall_args` |
| `0x823D` | `void set_sfall_arg(int argnum, int value)` |
| `0x81E5` | `void set_sfall_return(any value)` |
| `0x81EA` | `int init_hook` |
| `0x81E6` | `void set_unspent_ap_bonus(int multiplier)` |
| `0x81E7` | `int get_unspent_ap_bonus` |
| `0x81E8` | `void set_unspent_ap_perk_bonus(int multiplier)` |
| `0x81E9` | `int get_unspent_ap_perk_bonus` |
| `0x81EC` | `float sqrt(float)` |
| `0x81ED` | `int/float abs(int/float)` |
| `0x81EE` | `float sin(float)` |
| `0x81EF` | `float cos(float)` |
| `0x81F0` | `float tan(float)` |
| `0x81F1` | `float arctan(float x, float y)` |
| `0x8263` | `^` operator (exponentiation) |
| `0x8264` | `float log(float)` |
| `0x8265` | `float exponent(float)` |
| `0x8266` | `int ceil(float)` |
| `0x8267` | `int round(float)` |
| `0x81F2` | `void set_palette(string path)` |
| `0x81F3` | `void remove_script(object)` |
| `0x81F4` | `void set_script(object, int scriptid)` |
| `0x81F5` | `int get_script(object)` |
| `0x81F6` | `int nb_create_char` |
| `0x81F7` | `int fs_create(string path, int size)` |
| `0x81F8` | `int fs_copy(string path, string source)` |
| `0x81F9` | `int fs_find(string path)` |
| `0x81FA` | `void fs_write_byte(int id, int data)` |
| `0x81FB` | `void fs_write_short(int id, int data)` |
| `0x81FC` | `void fs_write_int(int id, int data)` |
| `0x81FD` | `void fs_write_float(int id, int data)` |
| `0x81FE` | `void fs_write_string(int id, string data)` |
| `0x8208` | `void fs_write_bstring(int id, string data)` |
| `0x8209` | `int fs_read_byte(int id)` |
| `0x820A` | `int fs_read_short(int id)` |
| `0x820B` | `int fs_read_int(int id)` |
| `0x820C` | `float fs_read_float(int id)` |
| `0x81FF` | `void fs_delete(int id)` |
| `0x8200` | `int fs_size(int id)` |
| `0x8201` | `int fs_pos(int id)` |
| `0x8202` | `void fs_seek(int id, int pos)` |
| `0x8203` | `void fs_resize(int id, int size)` |
| `0x8204` | `int get_proto_data(int pid, int offset)` |
| `0x8205` | `void set_proto_data(int pid, int offset, int value)` |
| `0x8206` | `void set_self(object)` |
| `0x8207` | `void register_hook(int hook)` |
| `0x820D` | `int list_begin(int type)` |
| `0x820E` | `int list_next(int listid)` |
| `0x820F` | `void list_end(int listid)` |
| `0x8236` | `array list_as_array(int type)` |
| `0x8210` | `int sfall_ver_major` |
| `0x8211` | `int sfall_ver_minor` |
| `0x8212` | `int sfall_ver_build` |
| `0x8213` | `void hero_select_win(int)` |
| `0x8214` | `void set_hero_race(int style)` |
| `0x8215` | `void set_hero_style(int style)` |
| `0x8216` | `void set_critter_burst_disable(object critter, int disable)` |
| `0x8217` | `int get_weapon_ammo_pid(object weapon)` |
| `0x8218` | `void set_weapon_ammo_pid(object weapon, int pid)` |
| `0x8219` | `int get_weapon_ammo_count(object weapon)` |
| `0x821A` | `void set_weapon_ammo_count(object weapon, int count)` |
| `0x8220` | `int get_screen_width` |
| `0x8221` | `int get_screen_height` |
| `0x8222` | `void stop_game` |
| `0x8223` | `void resume_game` |
| `0x8224` | `void create_message_window(string message)` |
| `0x8226` | `int get_light_level` |
| `0x8227` | `void refresh_pc_art` |
| `0x8228` | `int get_attack_type` |
| `0x822B` | `int play_sfall_sound(string file, int mode)` |
| `0x822C` | `void stop_sfall_sound(int soundID)` |
| `0x8235` | `array string_split(string string, string split)` |
| `0x8237` | `int atoi(string string)` |
| `0x8238` | `float atof(string string)` |
| `0x824E` | `string substr(string string, int start, int length)` |
| `0x824F` | `int strlen(string string)` |
| `0x8250` | `string sprintf(string format, any value)` |
| `0x8251` | `int charcode(string string)` |
| `0x8253` | `int typeof(any value)` |
| `0x823A` | `int get_tile_fid(int tileData)` |
| `0x823B` | `int modified_ini` |
| `0x823E` | `void force_aimed_shots(int pid)` |
| `0x823F` | `void disable_aimed_shots(int pid)` |
| `0x8240` | `void mark_movie_played(int id)` |
| `0x8248` | `object get_last_target(object critter)` |
| `0x8249` | `object get_last_attacker(object critter)` |
| `0x824A` | `void block_combat(int enable)` |
| `0x824B` | `int tile_under_cursor` |
| `0x824C` | `int gdialog_get_barter_mod` |
| `0x824D` | `void set_inven_ap_cost(int cost)` |
| `0x825C` | `void reg_anim_combat_check(int enable)` |
| `0x825A` | `void reg_anim_destroy(object object)` |
| `0x825B` | `void reg_anim_animate_and_hide(object object, int animID, int delay)` |
| `0x825D` | `void reg_anim_light(object object, int radius, int delay)` |
| `0x825E` | `void reg_anim_change_fid(object object, int FID, int delay)` |
| `0x825F` | `void reg_anim_take_out(object object, int holdFrameID, int delay)` |
| `0x8260` | `void reg_anim_turn_towards(object object, int tile/targetObj, int delay)` |
| `0x8261` | `int metarule2_explosions(object object)` |
| `0x8262` | `void register_hook_proc(int hook, procedure proc)` |
| `0x826B` | `string message_str_game(int fileId, int messageId)` |
| `0x826C` | `int sneak_success` |
| `0x826D` | `int tile_light(int elevation, int tileNum)` |
| `0x826E` | `object obj_blocking_line(object objFrom, int tileTo, int blockingType)` |
| `0x826F` | `object obj_blocking_tile(int tileNum, int elevation, int blockingType)` |
| `0x8270` | `array tile_get_objs(int tileNum, int elevation)` |
| `0x8271` | `array party_member_list(int includeHidden)` |
| `0x8272` | `array path_find_to(object objFrom, int tileTo, int blockingType)` |
| `0x8273` | `object create_spatial(int scriptID, int tile, int elevation, int radius)` |
| `0x8274` | `int art_exists(int artFID)` |
| `0x8275` | `int obj_is_carrying_obj(object invenObj, object itemObj)` |
| `0x8276` | `any sfall_func0(string funcName)` |
| `0x8277` | `any sfall_func1(string funcName, arg1)` |
| `0x8278` | `any sfall_func2(string funcName, arg1, arg2)` |
| `0x8279` | `any sfall_func3(string funcName, arg1, arg2, arg3)` |
| `0x827A` | `any sfall_func4(string funcName, arg1, arg2, arg3, arg4)` |
| `0x827B` | `any sfall_func5(string funcName, arg1, arg2, arg3, arg4, arg5)` |
| `0x827C` | `any sfall_func6(string funcName, arg1, arg2, arg3, arg4, arg5, arg6)` |
| `0x827D` | `void register_hook_proc_spec(int hook, procedure proc)` |
| `0x827E` | `void reg_anim_callback(procedure proc)` |
| `0x827F` | `div` operator (unsigned integer division) |
| `0x8280` | `any sfall_func7(string funcName, arg1, arg2, arg3, arg4, arg5, arg6, arg7)` |
| `0x8281` | `any sfall_func8(string funcName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)` |

Because sfall evolves, treat the table above as a compatibility snapshot and check the target sfall version when authoring scripts. Scripts can use `sfall_ver_major`, `sfall_ver_minor`, and `sfall_ver_build` to guard version-sensitive behavior.

## Stack Effects and Arguments

INT bytecode is stack based. Built-in opcode handlers pop their arguments from the interpreter stack, usually in reverse of the SSL source order, then optionally push a result. In the tables below, `(a b -- c)` means the source-level arguments `a` then `b` are consumed and `c` is pushed. The right side of the left-hand list is the top of stack at call time.

| Pattern | Stack effect | Notes |
| --- | --- | --- |
| Typed literal push | `(-- value)` | `0xC001`, `0xA001`, and `0x9001` push a following 32-bit integer, float bits, or static-string-table offset. |
| Unary arithmetic/logical opcode | `(a -- result)` | Examples: `floor`, `not`, `negate`, `bitwise_not`. |
| Binary arithmetic/comparison opcode | `(a b -- result)` | Examples: `add`, `sub`, `mul`, `equal`, `less_than`. String concatenation can also be reached through `add` in compatible runtimes. |
| Fetch variable | `(slot -- value)` | Local, map, global, and interpreter-global fetches pop an index or slot and push the stored value. |
| Store variable | `(slot value -- )` | Store opcodes pop the value first internally, but SSL source naturally reads as slot then value. |
| Procedure/built-in call | `(arg0 ... argN -- result?)` | Game and sfall built-ins normally carry arguments on the stack, not inline in the instruction stream. |
| Conditional branch helpers | `(condition -- )` | Branch target handling is encoded by surrounding bytecode and compiler patterns; disassemblers should recover control flow from offsets, not only from opcode names. |
| Procedure return/exit cleanup | varies | The `POP_FLAGS*` family restores frame/flag state and may return a value or exit an external call. Treat these as procedure-frame instructions, not user-visible SSL calls. |

Common Fallout game built-ins have the following practical stack contracts in Fallout 2 CE. The list is not a replacement for the handler source, but it covers the calls most often seen when reading map, object, dialogue, and utility scripts.

| Opcode / SSL name | Stack effect | Purpose |
| --- | --- | --- |
| `display_msg` | `(string -- )` | Adds a line to the message display. |
| `float_msg` | `(object message type -- )` | Shows or clears floating text over an object. |
| `message_str` | `(fileId messageId -- string)` | Looks up a message string. |
| `create_object` | `(pid tile elevation sid -- object)` | Creates an object. `sid` can attach a script; many SSL macros hide or default this argument. |
| `destroy_object` | `(object -- )` | Destroys an object. |
| `move_to` | `(object tile elevation -- )` | Moves an object to a tile/elevation. |
| `self_obj`, `source_obj`, `target_obj`, `dude_obj` | `(-- object)` | Push script context objects. |
| `obj_pid` | `(object -- pid)` | Returns an object's prototype id. |
| `obj_name` | `(object -- string)` | Returns display name text for an object. |
| `obj_type`, `item_subtype` | `(object -- int)` | Returns object/prototype classification. |
| `tile_num`, `elevation` | `(object -- int)` | Returns object location components. |
| `tile_distance` | `(tile1 tile2 -- int)` | Distance between two tile numbers. |
| `tile_distance_objs` | `(object1 object2 -- int)` | Distance between two objects, with elevation checks. |
| `tile_num_in_direction` | `(origin rotation distance -- tile)` | Computes a tile in a hex direction. |
| `tile_contains_pid_obj` | `(tile elevation pid -- object)` | Finds an object with a PID at a location. |
| `get_local_var`, `get_map_var`, `get_global_var` | `(index -- value)` | Reads script-local, map, or game-global variable storage. |
| `set_local_var`, `set_map_var`, `set_global_var` | `(index value -- )` | Writes script-local, map, or game-global variable storage. |
| `has_skill`, `using_skill` | `(object skill -- int)` | Skill value or skill state checks. |
| `roll_vs_skill` | `(object skill modifier -- int)` | Performs a skill roll. |
| `random` | `(min max -- int)` | Random integer in a range. |
| `roll_dice` | `(count sides -- int)` | Dice roll helper. |
| `get_critter_stat` | `(object stat -- int)` | Reads a critter stat. |
| `set_critter_stat` | `(object stat value -- )` | Writes a critter stat. |
| `add_obj_to_inven`, `rm_obj_from_inven` | `(container item -- )` | Inventory transfer helpers. |
| `wield_obj_critter` | `(critter item -- )` | Equips an item on a critter. |
| `use_obj` | `(object -- )` | Uses an object. |
| `use_obj_on_obj` | `(item target -- )` | Uses one object on another. |
| `add_timer_event` | `(object delay param -- )` | Adds a script timer event. |
| `rm_timer_event` | `(object -- )` | Removes queued events for an object. |
| `game_time`, `game_ticks`, `cur_map_index` | `(-- int)` | Push runtime state values. |

For sfall opcodes, the signature table above doubles as an argument-count table. A signature like `int get_weapon_ammo_pid(object weapon)` has stack effect `(weapon -- int)`; `void set_weapon_ammo_count(object weapon, int count)` has `(weapon count -- )`; and `any sfall_func4(string funcName, arg1, arg2, arg3, arg4)` has `(funcName arg1 arg2 arg3 arg4 -- any)`.

## Annotated Bytecode Example

The fragment below is not a complete INT file and not promised to be byte-for-byte compiler output. It is a compact illustration of instruction encoding inside a procedure body: a typed string push, a game built-in call, an integer push, and a script return value. Multi-byte values are shown in file byte order.

| Bytes | Decoded operation | Stack effect |
| --- | --- | --- |
| `90 01 00 00 00 02` | `push string @ static-string offset 2` | `(-- "Hello")` |
| `80 B8` | `display_msg` | `("Hello" -- )` |
| `C0 01 00 00 00 01` | `push int 1` | `(-- 1)` |
| `80 A2` | `scr_return` | `(1 -- )` |
| `80 0E` | `exit` | `(-- )` |

The static string offset in the first row is relative to the static string table, not to the file start. A real procedure can also contain compiler-emitted frame setup, branch targets, flag cleanup opcodes, and procedure-table offsets that surround the recognizable source-level operations.

## Compatibility and Aliases

The opcode number is the only reliable identity in raw bytecode. Names vary across Fallout 2 CE symbols, older opcode lists, SSL compiler headers, sfall documentation, and decompiler output.

- Vanilla Fallout 2 accepts the vanilla opcode range only. Direct sfall extension opcodes require an sfall-enabled runtime or another engine that deliberately implements them.
- Fallout 2 CE is a strong source reference for vanilla opcode behavior, but sfall support in CE-compatible engines should still be checked feature by feature rather than assumed from the opcode number alone.
- sfall has two extension styles: direct opcodes such as `create_array` or `get_mouse_x`, and dynamic dispatch through `sfall_func0` through `sfall_func8`. Dynamic dispatch stores the function name as a string argument, so a bytecode scanner must inspect the pushed string to identify the real operation.
- Unsafe sfall opcodes marked with `*` are not just compatibility-sensitive; they depend on `AllowUnsafeScripting` and may be tied to executable memory layout.
- `0x80D0` and `0x80DD` are both registered to the same CE attack handler. Older references may call one `attack` and another `attack_complex`.
- `obj_is_carrying_obj` is historically confusing: the vanilla opcode at `0x80BA` checks an inventory object against a PID, while the sfall opcode `0x8275` takes two object pointers.
- The CE registration comments preserve some historical spellings and aliases, such as `critter_rm_trait`, `game_time / 10`, and underscore-prefixed dialogue/inventory helper names.
- The published sfall opcode artifact currently gives `0x8196` for both `set_perk_int` and `set_target_knockback`. Treat duplicate or surprising entries as versioned source-list data and verify against the compiler/runtime you target.

## Script Lifecycle

Scripts are loaded lazily. A MAP or PRO record can reference a script index long before the corresponding INT is opened. When the engine first needs to execute a procedure for an active script instance, it resolves the index through `scripts.lst`, opens `scripts\name.int`, creates an interpreter program, finds the built-in procedures by name, runs the program initialization path, and then calls the requested procedure.

1. MAP/PRO data creates or references a script instance with an SID and a `scripts.lst` index.
2. The first procedure execution loads the INT from disk or DAT.
3. The interpreter uses the procedure table and identifier table to cache known procedure indexes such as `start` and `talk_p_proc`.
4. Initialization bytecode runs once for that loaded program, exporting names and preparing script state.
5. The requested procedure is executed. Later calls reuse the loaded program while it remains active.

This explains why an INT can parse correctly but still fail in game: missing `scripts.lst` entries, wrong script indexes, unsupported opcodes, failed imports, or missing dialogue MSG files are all runtime integration problems rather than file-layout problems.

## Built-In Script Procedures

The engine does not call arbitrary SSL procedure names for map/object events. It looks up a known list of procedure names in the INT procedure table and caches their procedure indexes after the script is loaded.

| Index | Procedure name | Typical trigger |
| --- | --- | --- |
| 0 | `no_p_proc` | Placeholder/no-procedure slot. |
| 1 | `start` | Script startup. |
| 2 | `spatial_p_proc` | Entering a spatial trigger. |
| 3 | `description_p_proc` | Description/inspect behavior. |
| 4 | `pickup_p_proc` | Picking up an item. |
| 5 | `drop_p_proc` | Dropping an item. |
| 6 | `use_p_proc` | Using an object. |
| 7 | `use_obj_on_p_proc` | Using one object on another. |
| 8 | `use_skill_on_p_proc` | Using a skill on an object. |
| 11 | `talk_p_proc` | Starting character dialogue. |
| 12 | `critter_p_proc` | Periodic critter processing. |
| 13 | `combat_p_proc` | Combat processing. |
| 14 | `damage_p_proc` | Taking damage. |
| 15 | `map_enter_p_proc` | Entering a map. |
| 16 | `map_exit_p_proc` | Leaving a map. |
| 17 | `create_p_proc` | Script/object creation. |
| 18 | `destroy_p_proc` | Script/object destruction. |
| 21 | `look_at_p_proc` | Look-at behavior. |
| 22 | `timed_event_p_proc` | Script timer event. |
| 23 | `map_update_p_proc` | Periodic map update. |
| 24 | `push_p_proc` | Pushing an object. |
| 25 | `is_dropping_p_proc` | Drop decision logic. |
| 26 | `combat_is_starting_p_proc` | Combat start notification. |
| 27 | `combat_is_over_p_proc` | Combat end notification. |

Indexes `9`, `10`, `19`, and `20` are present in the enum in Fallout 2 CE but mapped to placeholder names in the known-procedure name array. Older script headers may still document historical names such as `use_ad_on_proc`, `use_disad_on_proc`, or barter-related procedures.

## scripts.lst Integration

`scripts\scripts.lst` maps script indexes to INT filenames. The script index is what appears in SIDs, [PRO](pro.html) script fields, and [MAP](map.html) script records. Fallout 2 CE reads this list as text and stores a 13-character maximum base name before `.int`.

```text
ACMAP.int # local_vars=12
DCJOEY.int # local_vars=8
ZSDOOR.int
```

| Feature | Behavior |
| --- | --- |
| Indexing | The physical line number is the script index. Reordering lines changes script references. |
| Filename | The engine stores the base name before `.int` and later appends `.int` when loading. |
| Name length | Fallout 2 CE rejects names longer than 13 bytes before `.int`, matching the fixed script-name storage used by the original engine. |
| Local vars | If a line contains `#` and `local_vars=N`, that number defines the local variable storage requested by script instances using that script index. |
| Dialogue MSG | Script dialogue lookup uses the script base name to load `dialog\name.msg`. |

Do not add blank lines casually to `scripts.lst`. Like other [LST](lst.html)-style files, line position is data. A blank or malformed line can shift every later script index.

## Script Naming Conventions

Fallout 2 scripts commonly use compact names that encode broad ownership in the filename. The convention is not enforced by the INT bytecode format, but it is useful when matching scripts, dialogue files, and design intent.

| Part | Meaning | Examples |
| --- | --- | --- |
| First character | Area or content group. | `A` for Arroyo, `K` for Klamath, `D` for The Den, `G` for Gecko, `N` for New Reno, `Z` for generic/shared scripts. |
| Second character | Script role. | `C` critter, `I` item, `S` scenery, `T` spatial, `P` party, `H` head/talking-head related, `W` wall. |
| Remaining characters | Short object, NPC, place, or behavior mnemonic. | `DCJOEY`, `ZSDOOR`, `ACMAP`. |

The same base name often connects several files: `scripts\DCJOEY.int`, source `DCJOEY.ssl`, and dialogue `text\english\dialog\DCJOEY.msg`. Tools should not require the naming convention, but preserving it makes cross-file debugging much easier.

## Script IDs

A script ID, often called an SID, packs the script type into the high byte and a per-type script id/index into the low 24 bits. Fallout 2 CE creates new SIDs with:

```text
sid = script_id | (script_type << 24)
```

| Type value | Script type | Typical source |
| --- | --- | --- |
| `0` | System/map script | Map-level script. |
| `1` | Spatial script | MAP spatial trigger. |
| `2` | Timed script | Timed script state. |
| `3` | Item script | Item PRO or object instance. |
| `4` | Critter script | Critter PRO or object instance. |

Older PRO documentation sometimes describes packed script ids with a type nibble and a scripts.lst line number. When comparing sources, keep the distinction clear: `scripts.lst` maps indexes to INT filenames, while runtime SIDs identify active script instances of a given type.

## Variables and Saved State

INT files contain compiled code and some script-level identifiers, but they are not where normal game variable values live after a game starts. Runtime values are split across several systems:

| Kind | Definition/reference | Runtime storage |
| --- | --- | --- |
| Global variables | Referenced by index from script opcodes and named in [GAM](gam.html) files. | Global variable array saved with game state. |
| Map variables | Referenced by `map_var` and `set_map_var`. | MAP/map-state variable array. |
| Script local variables | Count comes from `local_vars=N` in `scripts.lst`. | MAP/SAV script local-variable array, pointed to by each script instance. |
| Procedure locals | Compiler stack slots inside procedure bytecode. | Interpreter stack frame while the procedure runs. |
| Static strings | Stored in the INT static string table. | Read-only program data while the INT is loaded. |
| Dynamic strings | Created at runtime by the interpreter. | Program heap with reference counts; not a normal file section. |

This is why decompiling an INT cannot recover the current value of map variables or script locals from a save. For that, inspect the active MAP/SAV data instead.

## Dialogue and MSG Files

Dialogue scripts usually call message opcodes rather than embedding every displayed line as a static string. The runtime script message loader derives the dialogue MSG path from the script base name:

```text
scripts.lst line:  DCJOEY.int
dialogue file:    text\english\dialog\DCJOEY.msg
```

The [MSG](msg.html) file then supplies message ids, optional speech tokens, and displayed text. The INT code decides which message id to request and when to start dialogue; the MSG file supplies the localizable content.

## Minimal Parser and Disassembler Recipe

A basic INT inspection tool does not need to emulate the whole VM, but it should validate the tables before walking opcode bytes.

1. Read the file into memory and use big-endian helpers for all integer fields.
2. Require at least `42` bytes, then read `procedure_count` at `0x2A`.
3. Compute the identifier-table start as `0x2A + 4 + procedure_count * 24`.
4. Validate the identifier table size and decode length-prefixed names.
5. Compute and validate the static-string table start.
6. Decode procedure records and resolve procedure names from identifier offsets.
7. Collect all procedure body offsets, condition offsets, and the bootstrap initialization jump target.
8. Disassemble code from known entry offsets, following jump/call targets and stopping at return/exit patterns.
9. When decoding literals, treat `0xC001`, `0xA001`, and `0x9001` as push-with-32-bit-operand forms.
10. Report unsupported opcodes by numeric value and runtime family, rather than failing the whole file immediately.

Full decompilation requires more: stack-effect modeling, control-flow reconstruction, procedure-call analysis, string resolution, and engine-specific opcode metadata. That is where tools such as `int2ssl` earn their keep.

## Decompilation Notes

Tools such as `int2ssl` recover readable SSL-like source from bytecode, but INT is not a lossless source archive.

| Source feature | Can it be recovered? |
| --- | --- |
| Procedure names | Usually yes, because they are stored in the identifier table. |
| Script-level exported names | Often yes, when present in the identifier table and export setup code. |
| Static strings | Yes, from the static string table. |
| Local variable names | No. Decompilers must invent names such as `local_var0` or infer them from patterns. |
| Comments and formatting | No. They are not compiled into INT. |
| Preprocessor macros/includes | No direct recovery. Decompiled output shows the expanded result, not the original macro source. |
| Control-flow structure | Partly. If/else/loops are reconstructed from jumps and stack effects, so difficult bytecode may decompile differently from the original SSL. |
| Compiler bugs | They remain in bytecode. A decompiler may show odd stack behavior or refuse to reconstruct invalid patterns. |

Fallout 2's original script build pipeline used preprocessing before compilation. Some script libraries therefore depend on header files and macros that are not visible in the final INT. Keep the original SSL tree when possible; use decompilation as a recovery and inspection tool rather than as a perfect source-control format.

## Validation Checklist

- File should be at least 42 bytes long.
- The procedure table should be readable at offset `0x2A`.
- `procedure_count` should be reasonable, and `42 + 4 + procedure_count * 24` should remain inside the file.
- Identifier and static string table sizes should point to valid file ranges.
- String lengths should include the null terminator and keep the next string aligned to an even offset.
- Procedure name offsets should point inside the identifier table and land on string bytes, not on length fields.
- Static string literal offsets should resolve inside the static string table.
- Procedure body and condition offsets should be absolute file offsets inside the INT file.
- Opcode stream should be parsed as big-endian 16-bit words, with 32-bit operands where required.
- Do not assume every opcode is valid for every engine. Vanilla Fallout 2, sfall, and CE-compatible engines can differ.
- For sfall scripts, check whether the opcode is a direct extension, a `sfall_func*` dispatch, or an unsafe opcode gated by `AllowUnsafeScripting`.

## Authoring Notes

- Edit SSL source when possible and compile to INT. Direct INT editing is brittle because jumps, table offsets, and stack effects must all remain consistent.
- Keep `scripts.lst` line order stable. Changing a line number changes references from PRO and MAP data.
- Keep script base names short and consistent with the matching `dialog\*.msg` file.
- When adding script local variables, update `local_vars=N` in `scripts.lst` and make sure existing MAP/SAV state is migrated or rebuilt as needed.
- Do not use sfall-only opcodes unless the target runtime supports them.
- For sfall-dependent scripts, check `sfall_ver_major`, `sfall_ver_minor`, and `sfall_ver_build` before relying on newer functions.
- Avoid unsafe memory/call opcodes in distributed mods unless there is no safer API and the required `ddraw.ini` setting is documented.
- For dialogue scripts, treat INT and MSG as a pair: INT chooses ids and flow; MSG stores text and speech names.

## Related Formats

- [LST File Format](lst.html) - `scripts.lst` is a line-indexed script table.
- [MSG File Format](msg.html) - dialogue text and script message strings.
- [MAP File Format](map.html) - map script records, map variables, spatial scripts, and script local values.
- [PRO File Format](pro.html) - prototype script ids for items, critters, scenery, and walls.
- [GAM File Format](gam.html) - global variables and map variable definitions.
- [ACM File Format](acm.html) and [LIP File Format](lip.html) - dialogue scripts can trigger spoken lines through MSG entries.

## Source References

- [Vault-Tec Labs INT File Format](https://falloutmods.fandom.com/wiki/INT_File_Format) - TeamX-derived layout notes for bootstrap, procedure table, identifier table, string table, and initialization code.
- [Fallout 2 CE `interpreter.h`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/interpreter.h) - opcode constants, procedure records, value tags, and runtime program structure.
- [Fallout 2 CE `interpreter.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/interpreter.cc) - loader offsets, big-endian reads, string accessors, opcode dispatch, and procedure execution.
- [Fallout 2 CE `interpreter_lib.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/interpreter_lib.cc) - UI, dialogue, movie, sound, and window opcode registrations.
- [Fallout 2 CE `interpreter_extra.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/interpreter_extra.cc) - Fallout game-specific opcode registrations.
- [Fallout 2 CE `scripts.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/scripts.cc) - `scripts.lst` loading, procedure lookup, script IDs, script local counts, and MSG lookup behavior.
- [Fallout2 Opcode Playground](https://fodev.net/files/fo2/opcodes/) - opcode index and historical opcode naming notes.
- [int2ssl](https://github.com/phobos2077/int2ssl) - decompiler source/tooling reference for recovering SSL-like source from INT bytecode.
- [sfall opcode list](https://github.com/sfall-team/sfall/blob/master/artifacts/scripting/sfall%20opcode%20list.md) - maintained list of sfall opcode numbers and function signatures.
- [sfall scripting documentation](https://sfall-team.github.io/sfall/) - global scripts, hooks, arrays, sfall functions, and script extender behavior.
- [sfall SSLC documentation](https://sfall-team.github.io/sfall/sslc/) - compiler compatibility notes and int2ssl support for sfall opcodes.
