---
title: SSL Script Source Format
output: ssl.html
description: Fallout and Fallout 2 SSL script source format, build workflow, syntax, procedures, script categories, compiler behavior, sfall extensions, and authoring guidance.
toc: auto
---

# SSL Script Source Format

SSL files are Fallout and Fallout 2 script source files. They are not loaded by the game engine at runtime. A script compiler turns `*.ssl` source into [INT](int.html) bytecode, and the engine loads the compiled `scripts\*.int` file when a map, object, critter, item, scenery, spatial trigger, global script, or hook needs to run script code.

SSL is therefore a source format and build input rather than a game data format in the same sense as [MAP](map.html), [PRO](pro.html), [MSG](msg.html), or INT. Still, documenting it is useful because most script behavior is authored and reviewed at the SSL level, and because source conventions explain many otherwise opaque INT, MSG, and [`scripts.lst`](scripts_lst.html) relationships.

## Format Properties

| Property | Description |
| --- | --- |
| File type | Plain text source code. |
| Runtime loading | The game does not load SSL files. It loads compiled INT bytecode. |
| Typical source location | Mapper and modding projects commonly keep source under a `scripts` directory, often next to `headers`. |
| Compiled output | `scripts\NAME.int`, listed by line number in [`scripts\scripts.lst`](scripts_lst.html). |
| Preprocessor | Original Fallout 2 script builds used an external C preprocessor. sfall's SSLC has an integrated preprocessor. |
| Encoding | Use the byte encoding expected by the compiler and target game language. Keep source ASCII-compatible unless the toolchain and target engine explicitly support other text encodings. |
| Case | Original data is often uppercase or mixed uppercase. Preserve script base-name case when it is used to match INT, MSG, and list entries. |

## Build Pipeline

A normal script pipeline has three layers:

| Layer | Input | Output | Notes |
| --- | --- | --- | --- |
| Preprocessing | `*.ssl` plus included `*.h` files. | Expanded SSL source. | Handles `#include`, `#define`, conditional compilation, and constants from the header library. |
| Compilation | Preprocessed SSL. | `*.int`. | Emits INT procedure tables, identifier strings, static strings, initialization code, and bytecode. |
| Runtime integration | `*.int`, `scripts.lst`, MAP/PRO references, MSG files. | Loaded script instances. | The engine resolves script ids, built-in procedure names, local variable counts, and dialogue files at runtime. |

The original script compiler did not have a built-in preprocessing pass. The Fallout 2 Mapper script library notes describe using Watcom's C preprocessor before the script compiler, with batch files choosing the preprocessor. Modern sfall SSLC removes much of that friction by integrating preprocessing and adding command-line options for include paths, macros, preprocessing-only output, optimizer levels, and sfall-aware syntax.

## Original Build Workflow

The historical build chain is important because many original scripts assume it. A typical project did not feed raw SSL directly to the script compiler. Instead, a batch file preprocessed the source, then passed the expanded output to the compiler.

```text
SSL source + headers
    -> C preprocessor
        -> expanded temporary source
            -> SSL compiler
                -> INT bytecode
```

| Workflow piece | Why it matters |
| --- | --- |
| External preprocessor | Original scripts may depend on C-preprocessor behavior for `#include`, `#define`, comments, and conditional compilation. |
| Batch files such as `p.bat` | Often encode include paths, temporary filenames, DOS extender invocation, and compiler flags. |
| Watcom-style environment | The original workflow commonly expected Watcom tools. Other preprocessors may work, but subtle macro/comment behavior can differ. |
| sfall SSLC | Modern replacement compiler with integrated preprocessing, optional preprocessing-only output, include path options, macro definitions, optimization, and sfall opcode support. |

For reproducible builds, document the exact compiler, preprocessor, command line, include paths, header set, and optimization mode. A source file that compiles under sfall SSLC might not compile under the original compiler, and a script that compiles under both may not produce byte-identical INT.

## Compiler Command Examples

Exact command lines vary by project, but the important thing is to record the compiler, include paths, macro definitions, preprocessing mode, optimization level, output path, and compatibility mode. The original compiler path was often hidden behind batch files, while sfall SSLC exposes most of this directly on the command line.

| Task | Example | What to record |
| --- | --- | --- |
| Original-style batch build | `p.bat scripts\example.ssl` | The batch file contents, selected preprocessor, compiler executable, temporary file path, include paths, and output INT path. |
| Separate preprocess and compile | `cpp -Iheaders scripts\example.ssl > temp\example.ssl`; then `compile temp\example.ssl` | Preprocessor brand/version and whether the compiler receives original source or expanded source. |
| sfall SSLC normal build | `sslc -p -Iheaders -mDEBUG=0 -o scripts\example.int scripts_src\example.ssl` | `-p` enables preprocessing, `-I` adds include paths, `-m` defines macros, and `-o` sets output. |
| sfall SSLC preprocess-only check | `sslc -P -F -Iheaders scripts_src\example.ssl` | `-P` preprocesses without generating INT; `-F` keeps full paths in generated line directives. |
| sfall SSLC compatibility build | `sslc -b -p -Iheaders scripts_src\example.ssl` | `-b` requests backward compatibility behavior for scripts sensitive to newer compiler fixes/features. |
| sfall SSLC optimized build | `sslc -O1 -p -Iheaders scripts_src\example.ssl` | `-O` and `-O ` choose optimization levels; document the level because optimization can affect decompilation and dynamic-call assumptions. |

The examples are intentionally illustrative. If a project uses a wrapper script, document the wrapper and the fully expanded command it runs; future maintainers need both.

## Source File Shape

Official-style Fallout 2 SSL files usually follow a recognizable layout. The exact order is a convention, but it matters in practice because headers define constants and macros that later declarations and procedure bodies rely on.

1. Comment header: name, location, description, change log, and copyright notice in official sources.
2. `#include` directives for shared headers such as `define.h`, `command.h`, map/town headers, and local helper headers.
3. `#define` constants such as `NAME`, town variables, or local message ids.
4. Procedure forward declarations, especially for engine-called procedures and dialogue nodes.
5. File-scope variables or constants used by multiple procedures.
6. Procedure bodies.

```text
#include "..\headers\define.h"
#define NAME SCRIPT_EXAMPLE
#include "..\headers\command.h"

procedure start;
procedure map_enter_p_proc;
procedure use_p_proc;

procedure start begin
end

procedure use_p_proc begin
    display_msg(message_str(NAME, 100));
end
```

The example is intentionally tiny. Real Fallout 2 scripts often have many forward declarations because the compiler and macros can reference procedures before their bodies appear. Dialogue scripts in particular commonly declare a long list of `NodeNNN` procedures before defining them.

## Preprocessor and Headers

Much of what looks like the SSL language in real scripts is actually preprocessor and header-library behavior. Constants such as PIDs, global variables, map ids, procedure names, message-file ids, critter stats, skill ids, object flags, and dialogue helpers normally come from headers.

| Header role | Typical contents |
| --- | --- |
| Global definitions | Object type ids, stat ids, skill ids, trait ids, damage types, flags, map ids, and script ids. |
| Command macros | Convenience wrappers around built-in opcodes, dialogue helpers, object helpers, and common tests. |
| Area headers | Town-specific globals, quest states, map constants, local object PIDs, and script names. |
| Dialogue headers | Macros for `Reply`, `NOption`, `GOption`, `giq_option`, reaction checks, and message lookup. |

When documenting or porting an SSL script, keep the preprocessed and source forms separate. The source form is readable but depends heavily on macros. The preprocessed form is closer to what the compiler sees, but it loses the author's intent and may be much harder to compare with human-written scripts.

## Known Header Ecosystem

Header names vary between the official source tree, mapper releases, total conversions, Restoration Project-style projects, sfall modder packs, and decompiled script collections. The names below are representative rather than exhaustive.

| Header or header family | Typical role | Common dependency risk |
| --- | --- | --- |
| `define.h` | Central constants: script ids, PIDs, global variables, skills, stats, traits, maps, message files, object flags. | Usually included early. Many other headers and scripts assume these constants already exist. |
| `command.h` | Common macro wrappers around engine calls, dialogue helpers, message helpers, object helpers, and convenience tests. | Can hide many low-level opcodes behind readable source calls. |
| Town or map headers | Area-specific globals, quest states, map numbers, local PIDs, local script constants. | Constants can collide or mean different things in different projects. |
| Dialogue helper headers | Macros for `Reply`, `NOption`, `GOption`, `giq_option`, reaction handling, and message ids. | Source may not show the real built-in calls until preprocessed. |
| Object/item/proto headers | PID names and object classification helpers. | Generated or edited headers must match [PRO](pro.html), [LST](lst.html), and art data. |
| sfall headers | sfall opcode names, hook constants, array helpers, global script helpers, new metarules, and macro sugar. | Compiled INT may require sfall even when the source looks like ordinary function calls. |

Include order can matter. Some original-style scripts explicitly warn that the following include lines must remain in order. When modernizing source, prefer small include-order changes with compile checks over broad reorganization.

## Core Syntax

SSL has a C-like expression language wrapped in a Pascal-like block style. It uses `begin` and `end` for blocks, semicolons for most statements, and procedure declarations instead of C-style function definitions.

| Construct | Shape | Notes |
| --- | --- | --- |
| Block | `begin ... end` | Used for procedure bodies and grouped statements. |
| Procedure declaration | `procedure name;` | Forward declaration. |
| Procedure body | `procedure name begin ... end` | Defines executable code. |
| Procedure parameters | `procedure name(variable arg)` | Arguments are compiler symbols and become stack values in INT. |
| Variable declaration | `variable name;` | Declares a compiler-level variable. sfall SSLC also supports multiple declarations in one line. |
| Assignment | `name := value;` | Classic SSL assignment. sfall SSLC also accepts `=` as an assignment operator. |
| Conditional | `if expr then statement else statement` | Blocks can be used for multi-statement branches. |
| Loop | `while expr do statement` | Classic loop form. sfall SSLC adds `for`, `foreach`, `break`, and `continue`. |
| Call | `call procedure_name;` | Can call a procedure by symbol. Vanilla also supports calling through a string value containing a procedure name. |
| Return-like value | `scr_return(value)` | Compiled scripts communicate some event results through script return storage rather than C-style function returns. |

SSL comments commonly use C-style `/* ... */` block comments and `//` line comments, depending on compiler/preprocessor support. Because the original build chain used a C preprocessor, comments and macro syntax should be tested with the exact compiler chain used for the target project.

## Operator Precedence

Operator handling is compiler-sensitive enough that generated tools should prefer the compiler's parser over a hand-rolled expression parser. For human documentation, the practical rule is simple: parenthesize any expression where side effects, mixed boolean/arithmetic operators, or sfall short-circuit behavior matter.

| Group, high to low | Examples | Notes |
| --- | --- | --- |
| Grouping, calls, indexes, member-like access | `(expr)`, `foo(x)`, `a[i]`, `map.key` | Array and dot syntax are sfall SSLC features when used as high-level source syntax. |
| Unary operators | `not x`, `-x` | Be careful with constants and initialized variables in sfall SSLC; expressions beginning with constants may need parentheses in some forms. |
| Multiplicative arithmetic | `*`, `/`, `%` | Exact accepted spellings depend on compiler support. |
| Additive arithmetic and string concatenation | `+`, `-` | `+` is commonly used in source for numeric addition and message/string composition. |
| Comparisons | `<`, `<=`, `>`, `>=` | Comparison results are runtime values used as integer-like booleans. |
| Equality | `==`, `!=` | Prefer explicit comparisons when an engine API may return sentinel values such as `-1`. |
| Logical AND | `and`, `AndAlso` | Classic `and` behavior and sfall short-circuit behavior can differ. `AndAlso` is explicitly short-circuiting. |
| Logical OR | `or`, `OrElse` | `OrElse` is explicitly short-circuiting. sfall's `-s` option or `#pragma sce` can affect ordinary `and`/`or`. |
| Conditional expression | `a if (cond) else b` | sfall SSLC feature. |
| Assignment | `:=`, `=`, compound forms | `:=` is the classic form. `=` assignment and several shorthand forms are sfall SSLC features. |

For round-tripping tools, preserve parentheses more often than strictly necessary. It makes the reconstructed source safer across compiler modes and easier to compare with decompiled INT.

## Grammar Sketch

This sketch is intentionally not a full compiler grammar. It gives parser and documentation authors a compact model of the source shape they will encounter in ordinary SSL.

```text
program        ::= { top_level }
top_level      ::= include | define | pragma | declaration | procedure_body

declaration    ::= "procedure" identifier [ parameters ] ";"
                 | "variable" variable_list ";"
                 | "import" declaration
                 | "export" declaration

procedure_body ::= "procedure" identifier [ parameters ] "begin"
                       { statement }
                   "end"

parameters     ::= "(" parameter_list ")"
parameter_list ::= parameter { "," parameter }
parameter      ::= "variable" identifier [ ":=" constant ]

statement      ::= block
                 | declaration
                 | assignment ";"
                 | call_statement ";"
                 | if_statement
                 | while_statement
                 | return_statement ";"
                 | expression ";"

block          ::= "begin" { statement } "end"
if_statement   ::= "if" expression "then" statement [ "else" statement ]
while_statement::= "while" expression "do" statement
call_statement ::= "call" identifier_or_expression
assignment     ::= identifier assignment_operator expression
assignment_operator ::= ":=" | "=" | "+=" | "-=" | "*=" | "/="
return_statement ::= "scr_return" "(" expression ")"
call           ::= identifier "(" [ argument_list ] ")"
argument_list  ::= expression { "," expression }
identifier_or_expression ::= identifier | expression
expression     ::= literal | identifier | call | unary_expr | binary_expr | grouped_expr
```

sfall SSLC adds source forms that are best modeled as grammar extensions: `for`, `switch`, `break`, `continue`, `foreach`, array literals, map literals, dot access, initialized variables, optional procedure arguments, `@procedure` stringification, conditional expressions, and short-circuit operators.

## Concrete Source Examples

The examples below are deliberately small and avoid project-specific macros where possible. Real scripts often use header macros to make these patterns shorter or more readable.

### Procedures and Calls

```text
procedure start;
procedure helper;

procedure start begin
    call helper;
end

procedure helper begin
    display_msg("helper ran");
end
```

Forward declarations make procedure names known before the bodies are compiled. This is especially useful when two procedures call each other or when dialogue nodes are listed near the top of the file.

### Variables and Control Flow

```text
procedure timed_event_p_proc begin
    variable count;

    count := get_local_var(0);
    count := count + 1;
    set_local_var(0, count);

    if (count < 3) then begin
        add_timer_event(self_obj, 10, 0);
    end else begin
        display_msg("timer finished");
    end
end
```

The source variable `count` is temporary script code state. The persistent value is local variable slot `0`, which requires enough local variable storage in `scripts.lst`.

### Message Lookup

```text
#define NAME SCRIPT_EXAMPLE

procedure use_p_proc begin
    display_msg(message_str(NAME, 100));
end
```

This pattern displays entry `100` from the message list associated with `NAME`. In macro-heavy source, the literal `message_str` call might be hidden behind a wrapper such as `mstr(100)`, `Reply(100)`, or a project-specific message macro.

### Dynamic Procedure Calls

```text
procedure Node001;
procedure Node002;

procedure start begin
    variable next_node;

    next_node := "Node001";
    call next_node;
end
```

Vanilla scripts can call a procedure by storing its name in a string value and calling that variable. sfall SSLC's `@` stringify operator makes the intent clearer by allowing source like `next_node := @Node001;`, while also helping tools notice that the procedure is referenced.

## Values and Types

Classic SSL source looks weakly typed from a modder's perspective, but the INT interpreter carries typed runtime values: integers, floats, strings, object pointers, and procedure/address values. Source-level type names in documentation are mostly there to explain what a built-in expects, not to enforce a rich static type system.

| Type | Meaning in scripts | INT/runtime note |
| --- | --- | --- |
| `int` | Numbers, ids, flags, tiles, elevations, stats, message ids, booleans. | Typed integer pushes compile as `0xC001` plus a 32-bit value. |
| `float` | Used by some math and sfall APIs. | Typed float pushes compile as `0xA001` plus 32-bit float data. |
| `string` | Text literals, procedure names, filenames, INI keys, dialogue and message values. | Static literals are stored in the INT static string table. |
| `object` / `ObjectPtr` | Runtime pointer to a map object, critter, item, scenery object, or special object like `dude_obj`. | Pointer values are runtime-only. They are not portable file offsets. |
| `procedure` / `proc` | Procedure symbols used for callbacks, hooks, or dynamic calls. | Compiled to procedure lookups, procedure addresses, or strings depending on syntax and compiler feature. |
| `array` | sfall array id and syntax sugar around sfall array opcodes. | sfall-specific. The array id is effectively an integer handle. |
| `any` / `mixed` | Documentation shorthand for APIs that accept or return different runtime value types. | Useful in sfall docs; not a separate classic SSL storage type. |

Booleans are conventionally integers: false is `0`, true is usually `1` or any non-zero value depending on context. Many engine APIs return `-1` as an error or missing-value sentinel, so do not treat every non-zero value as a clean boolean unless the specific function says so.

## Variables and Persistence

There are several different things called variables in Fallout scripting. Keeping them separate prevents many tool and modding mistakes.

| Kind | Where it is declared or addressed | Lifetime |
| --- | --- | --- |
| Compiler/source variable | `variable name;` in SSL source. | Compiled into INT variable slots and stack operations. It is part of the program, not automatically a game-global. |
| Procedure argument | `procedure foo(variable x)`. | Passed on the interpreter stack for that call. |
| Script local variable | `get_local_var(index)` / `set_local_var(index, value)`; count from `scripts.lst` `# local_vars=N`. | Stored with a script instance and saved in MAP/SAV state. |
| Map variable | `get_map_var(index)` / `set_map_var(index, value)`; names often come from [GAM](gam.html). | Stored with the current map state. |
| Game global variable | `get_global_var(index)` / `set_global_var(index, value)`; indexes from `vault13.gam`, `genrep.gam`, map GAM files, or headers. | Persistent game-wide state. |
| sfall global | `set_sfall_global` / `get_sfall_global_int` / `get_sfall_global_float`. | sfall-managed state, separate from vanilla global variables. |
| Exported INT variable | `export variable` / external fetch-store bytecode. | Runtime interpreter export. Depends on loaded programs and initialization. |

A source variable named `LVar0` is only a name unless the script uses it as a constant or wrapper around a real local-var index. The persistent local storage used by `get_local_var` and `set_local_var` is allocated from `scripts.lst`, not from merely declaring SSL variables.

## Procedures

Procedures are the main unit of source organization and the main callable unit in INT. The compiler emits a procedure table with name offsets, argument counts, flags, condition offsets, and body offsets.

| Procedure kind | Example | Runtime role |
| --- | --- | --- |
| Engine event procedure | `map_enter_p_proc`, `use_p_proc`, `talk_p_proc`. | Looked up by known name and called by the engine for map/object events. |
| Helper procedure | `Node001`, `GiveReward`. | Called from other procedures in the same script. |
| Imported procedure | `import procedure helper;` | Resolved through the interpreter external procedure registry. |
| Exported procedure | `export procedure helper;` | Registered so other loaded programs can call it. |
| Hook callback | `register_hook_proc(HOOK_TOHIT, tohit_hook);` | sfall-specific callback procedure. |

Event procedure names are not arbitrary. The engine has a known list that includes `start`, `spatial_p_proc`, `description_p_proc`, `pickup_p_proc`, `drop_p_proc`, `use_p_proc`, `use_obj_on_p_proc`, `use_skill_on_p_proc`, `talk_p_proc`, `critter_p_proc`, `combat_p_proc`, `damage_p_proc`, `map_enter_p_proc`, `map_exit_p_proc`, `create_p_proc`, `destroy_p_proc`, `look_at_p_proc`, `timed_event_p_proc`, `map_update_p_proc`, `push_p_proc`, `is_dropping_p_proc`, `combat_is_starting_p_proc`, and `combat_is_over_p_proc`. See [INT](int.html) for the procedure-table/runtime side.

## Event Procedure Reference

The procedures below are conventional entry points that the engine or sfall runtime calls by name. A script may define only the procedures it needs; undefined event procedures simply have no script code to run for that event.

| Procedure | Common script types | When it runs |
| --- | --- | --- |
| `start` | Most scripts; especially global scripts. | Initialization entry point. In many object scripts it is empty, while sfall global scripts commonly use it for setup, repeat timing, and hook registration. |
| `map_enter_p_proc` | Map, global. | When the player enters or loads a map. Used for map setup, one-time spawning, local state repair, ambient setup, and sfall global-script map-enter work. |
| `map_exit_p_proc` | Map, global. | When leaving a map. Used for cleanup, state capture, or undoing temporary setup. |
| `map_update_p_proc` | Map, global. | Periodic/update-style map processing. In sfall global scripts it can be driven by the selected global script mode and repeat interval. |
| `create_p_proc` | Critter, item, scenery, wall, map object. | When a script instance or object is created/initialized. Used for object setup, animation state, object flags, and local variable defaults. |
| `destroy_p_proc` | Critter, item, scenery, wall. | When the object or script instance is destroyed. Used for death/removal side effects. |
| `look_at_p_proc` | Critter, item, scenery, wall. | When the player examines an object. Usually emits a description through MSG lookup or a float message. |
| `description_p_proc` | Item, scenery, wall, critter. | When the engine asks for a detailed description. Often paired with `look_at_p_proc` but used by a different UI path. |
| `use_p_proc` | Item, scenery, wall. | When the object is used directly. Containers, doors, computers, ladders, and usable items often put their main behavior here. |
| `use_obj_on_p_proc` | Item, scenery, critter. | When another object is used on this object, such as an item on scenery or a tool on a target. |
| `use_skill_on_p_proc` | Scenery, critter, wall. | When the player uses a skill on the object. Common for repair, science, lockpick, doctor, steal, and traps interactions. |
| `talk_p_proc` | Critter. | When the player starts dialogue with a critter. Usually opens dialogue UI and calls `NodeNNN` procedures. |
| `critter_p_proc` | Critter. | Critter behavior processing. Used for AI-adjacent scripted behavior, proximity checks, quest reactions, and recurring critter logic. |
| `combat_p_proc` | Critter. | During combat processing for a critter. Used for scripted tactics, fleeing, shouting, weapon changes, or quest-sensitive combat behavior. |
| `combat_is_starting_p_proc` | Critter. | When combat involving the critter starts. Used for pre-combat reactions and setup. |
| `combat_is_over_p_proc` | Critter. | When combat ends. Used for cleanup, post-combat dialogue state, or resetting temporary flags. |
| `damage_p_proc` | Critter, scenery. | When the object takes damage. Used for scripted retaliation, alarms, breakage, or special death handling. |
| `pickup_p_proc` | Item. | When an item is picked up. Used for quest item behavior, stealing checks, or one-shot messages. |
| `drop_p_proc` | Item. | When an item is dropped. Used for cleanup or special placement behavior. |
| `is_dropping_p_proc` | Item. | When the engine asks whether an item may be dropped. Scripts can block or modify drop behavior through script return values. |
| `push_p_proc` | Critter, scenery. | When the player pushes or attempts to move an object or critter. |
| `spatial_p_proc` | Spatial, map. | When the player enters or triggers a spatial script area. Used for traps, map transitions, ambient messages, and area triggers. |
| `timed_event_p_proc` | Any script that schedules timers. | When a scheduled timer event fires for the script. The event argument usually distinguishes multiple timer purposes. |

Not every event has useful `self_obj`, `source_obj`, `target_obj`, or action context. Those values are runtime-state dependent, so documentation for a script should state which object context it assumes when the procedure is called.

## Object Context Values

Many script procedures read implicit runtime object values. These are not stored in the SSL file; they are provided by the engine/interpreter for the current call.

| Value | Typical meaning | Common caveat |
| --- | --- | --- |
| `self_obj` | The object or script instance that owns the running script. | Usually meaningful for map objects, critters, items, scenery, and walls. sfall global scripts do not automatically have an attached object. |
| `source_obj` | The actor or object that caused the current action. | Most useful in use, skill, combat, and damage events. It may be unset or stale outside action-driven contexts. |
| `target_obj` | The object being targeted by the current action. | Relevant for use-object-on, skill, combat, and hook-like contexts; do not assume it exists in map/global update code. |
| `dude_obj` | The player character object. | Convenient and common, but still a runtime object pointer, not a serialized file id. |
| `obj_being_used_with` | The item/object being used with another object in use-on interactions. | Only meaningful for the matching event path. |
| `fixed_param` | An integer argument supplied to some script events, especially timer events. | Scripts often define local constants for timer meanings because the raw number has no meaning by itself. |

When documenting a procedure, note whether it assumes an object context. For example, `talk_p_proc` usually expects `self_obj` to be the critter being spoken to, while a repeated sfall global script should either avoid `self_obj` or set it deliberately.

## Overrides and Return Values

Some event procedures are not merely notifications. They can tell the engine that the script handled the event and that default behavior should be skipped or replaced.

| Mechanism | Meaning | Where it commonly appears |
| --- | --- | --- |
| `script_overrides` | Marks the current scripted action as handled by the script. | `look_at_p_proc`, `description_p_proc`, `use_p_proc`, `use_skill_on_p_proc`, door/scenery/item scripts. |
| `scr_return(value)` | Stores an event return value for engine code or hook code that reads script results. | Drop checks, use/skill results, some sfall hooks, and event procedures that need to accept/deny/override behavior. |
| `set_sfall_return(value)` | sfall hook return mechanism. | Hook scripts and registered hook callbacks where the hook contract defines a return value. |

Documentation should say what a return value means for that procedure. A value such as `0`, `1`, or `-1` is not self-describing; its meaning depends on the event or hook that reads it.

```text
procedure look_at_p_proc begin
    script_overrides;
    display_msg(message_str(NAME, 100));
end
```

In this example the script supplies its own look text and prevents the normal object description path from being the only response. Without the override, the engine may still perform its default behavior after the scripted message.

## Timer Events

Timer events let a script schedule future work for itself or another object. The most common pattern is `add_timer_event(object, delay, arg)`, followed later by `timed_event_p_proc` reading the event argument.

```text
#define TIMER_WARN  1
#define TIMER_DONE  2

procedure use_p_proc begin
    add_timer_event(self_obj, 10, TIMER_WARN);
    add_timer_event(self_obj, 30, TIMER_DONE);
end

procedure timed_event_p_proc begin
    if (fixed_param == TIMER_WARN) then begin
        display_msg(message_str(NAME, 100));
    end else if (fixed_param == TIMER_DONE) then begin
        display_msg(message_str(NAME, 101));
    end
end
```

The delay unit and scheduling behavior should be checked against the target engine/compiler/header set, but the documentation point is stable: the third argument is a script-defined discriminator. Treat raw timer numbers as local protocol values and name them with constants where possible.

## Script Categories

SSL source does not have a separate file header saying "this is an item script" or "this is a map script". The category comes from how the compiled INT is referenced by MAP, PRO, SIDs, `scripts.lst`, and sfall naming conventions.

| Script category | How it is attached | Common procedures |
| --- | --- | --- |
| Map script | Map script record in a MAP file. | `map_enter_p_proc`, `map_exit_p_proc`, `map_update_p_proc`, `spatial_p_proc`. |
| Critter script | Critter PRO or MAP object script id. | `talk_p_proc`, `critter_p_proc`, `combat_p_proc`, `damage_p_proc`, `destroy_p_proc`. |
| Item script | Item PRO or MAP object script id. | `use_p_proc`, `pickup_p_proc`, `drop_p_proc`, `use_obj_on_p_proc`. |
| Scenery/wall script | Scenery or wall PRO/MAP script id. | `description_p_proc`, `use_p_proc`, `use_skill_on_p_proc`, `look_at_p_proc`. |
| Spatial script | MAP spatial script or sfall-created spatial trigger. | `spatial_p_proc`. |
| Global script | sfall script name beginning with `gl`. | `start`, `map_enter_p_proc`, `map_exit_p_proc`, `map_update_p_proc`. |
| Hook script | sfall hook naming or registered hook callback. | Hook-specific procedures and callbacks. |

## Script Category Templates

These tiny skeletons are not meant to be canonical style. They document the normal shape of each script category and the cross-file data that must be present for the script to do anything useful.

### Map Script

```text
#define NAME SCRIPT_EXAMPLE_MAP

procedure start;
procedure map_enter_p_proc;
procedure map_exit_p_proc;
procedure spatial_p_proc;

procedure start begin
end

procedure map_enter_p_proc begin
    if (get_map_var(0) == 0) then begin
        set_map_var(0, 1);
    end
end

procedure spatial_p_proc begin
    display_msg(message_str(NAME, 100));
end
```

A map script is attached through MAP script records and `scripts.lst`. If it uses `get_map_var` or `set_map_var`, the map variable meaning should also be documented in the map's GAM/config notes.

### Critter Dialogue Script

```text
#define NAME SCRIPT_EXAMPLE_CRITTER

procedure start;
procedure talk_p_proc;
procedure Node001;

procedure start begin
end

procedure talk_p_proc begin
    start_gdialog(NAME, self_obj, 4, -1, -1);
    call Node001;
    end_dialogue;
end

procedure Node001 begin
    gsay_reply(NAME, 100);
end
```

Critter scripts are normally referenced by a critter PRO script id or by a placed MAP object's script id. Dialogue scripts additionally depend on a matching MSG file and stable message ids.

### Item Script

```text
procedure start;
procedure use_p_proc;
procedure pickup_p_proc;
procedure drop_p_proc;

procedure start begin
end

procedure use_p_proc begin
    display_msg("used");
end
```

Item scripts are usually attached from an item PRO or from an overridden MAP object script id. Inventory FIDs, item PIDs, and action flags live outside SSL, so the script alone does not describe the whole item.

### Scenery or Wall Script

```text
#define NAME SCRIPT_EXAMPLE_SCENERY

procedure start;
procedure look_at_p_proc;
procedure use_p_proc;
procedure use_skill_on_p_proc;

procedure start begin
end

procedure look_at_p_proc begin
    script_overrides;
    display_msg(message_str(NAME, 100));
end
```

Scenery and wall behavior depends on PRO flags, material, blocking, walk-through behavior, and MAP placement. A script may override look/use behavior, but the physical object behavior is still partly prototype data.

### Spatial Script

```text
procedure spatial_p_proc begin
    display_msg("triggered");
end
```

Spatial scripts are attached to map trigger areas rather than visible objects. The important external contract is the trigger tile/elevation/radius in the MAP data and the script id in `scripts.lst`.

### sfall Global Script

```text
procedure start begin
    set_global_script_repeat(1);
end

procedure map_update_p_proc begin
    /* periodic work */
end
```

sfall global scripts are loaded by sfall naming and configuration rules rather than by a map object. They should not assume an attached `self_obj`.

### sfall Hook Script

```text
procedure start begin
    register_hook_proc(HOOK_TOHIT, tohit_hook);
end

procedure tohit_hook begin
    /* inspect hook arguments with the hook argument helpers */
    /* optionally call set_sfall_return */
end
```

Hook scripts depend on the hook's argument and return contract. Different hooks expose different argument counts and meanings, so hook documentation should name the hook and list the expected arguments.

## Built-In Functions and Opcodes

Most source-level built-ins compile to the vanilla or sfall opcodes documented in [INT](int.html). Header macros can make one source call expand into several lower-level calls, so a source function name does not always map one-to-one to one opcode.

| Source-level call | Compiled/runtime behavior |
| --- | --- |
| `display_msg(text)` | Compiles to a string/value expression followed by the vanilla `display_msg` opcode. |
| `message_str(file, id)` | Looks up text from a loaded MSG list. |
| `get_local_var(i)` / `set_local_var(i, v)` | Reads/writes script-instance local storage, not source variables. |
| `get_global_var(i)` / `set_global_var(i, v)` | Reads/writes persistent game-global variables. |
| `create_object(pid, tile, elev)` | Header macros may wrap the lower-level object creation opcode and default script id behavior. |
| `gsay_*` / `giq_option` | Dialogue helpers that drive game-dialog UI and MSG lookups. |
| `register_hook_proc`, arrays, `sfall_func*` | sfall-only extensions; the compiled INT needs sfall-compatible runtime support. |

When building a decompiler or static analyzer, prefer the opcode table and compiler headers over surface source spelling. The same behavior can appear as a built-in name, macro name, sfall function, historical opcode alias, or decompiler-generated helper.

## Source Calls to INT Opcodes

The table below is a navigation aid from SSL-level source to [INT](int.html)-level bytecode behavior. Header macros can expand to more than one operation, so this should be read as a common mapping, not a complete compiler listing.

| SSL/source-level expression | INT/runtime concept | Documentation note |
| --- | --- | --- |
| `display_msg(text)` | `display_msg` game opcode. | Pushes/evaluates a string value, then calls the engine display-message handler. |
| `message_str(file, id)` | Message-list lookup opcode. | Depends on a loaded MSG list and a numeric entry id. |
| `get_local_var(i)`, `set_local_var(i, value)` | Script local variable opcodes. | Indexes into storage allocated by `scripts.lst` metadata. |
| `get_map_var(i)`, `set_map_var(i, value)` | Map variable opcodes. | Indexes into map-level state, often named by GAM/header constants. |
| `get_global_var(i)`, `set_global_var(i, value)` | Game global variable opcodes. | Indexes into game-global state from GAM/global variable definitions. |
| `call helper;` | Procedure call bytecode. | Uses procedure table/identifier data in the compiled INT. |
| `call next_node;` | Dynamic procedure call. | Uses a runtime value, usually a string containing a procedure name. |
| `if`, `while`, `and`, `or` | Expression, branch, and jump bytecode. | Classic logic may evaluate both sides; sfall short-circuit features can change branch structure. |
| `gsay_reply`, `giq_option`, dialogue macros. | Dialogue opcodes and MSG lookups. | Usually expands through headers into message lookup plus dialogue UI operations. |
| `register_hook_proc`, arrays, direct-memory calls. | sfall opcode range. | Requires sfall-aware compiler/runtime support; see the sfall opcode table in INT. |

## Dialogue and MSG Integration

Dialogue scripts are usually paired with [MSG](msg.html) files. The base script name often points at `text\ \dialog\NAME.msg`, while headers and macros use `NAME` or script-message-list constants to call `message_str` and dialogue helpers.

| Piece | Role |
| --- | --- |
| `NAME` macro | Commonly defines the script/message-list identity used by message macros. |
| `dialog\NAME.msg` | Text and optional audio tags for dialogue lines. |
| `talk_p_proc` | Starts character dialogue or redirects to dialogue setup. |
| `NodeNNN` procedures | Common convention for individual dialogue nodes. |
| `giq_option` and macro wrappers | Add options gated by player intelligence or other conditions. |
| `float_msg` / `display_msg` | Show text outside full dialogue UI. |

MSG ids are source-level contracts. If a script says `message_str(NAME, 150)`, the matching MSG file must contain entry `150`, or the runtime will show/log a fallback error. Renumbering MSG entries is therefore a script change even if the SSL source file is untouched.

### Minimal Dialogue Pattern

The following is a simplified dialogue shape. It shows the important cross-file contracts without trying to reproduce every macro used by the official headers.

```text
#include "..\headers\define.h"
#define NAME SCRIPT_EXAMPLE
#include "..\headers\command.h"

procedure start;
procedure talk_p_proc;
procedure Node001;
procedure Node002;
procedure Node999;

procedure start begin
end

procedure talk_p_proc begin
    start_gdialog(NAME, self_obj, 4, -1, -1);
    call Node001;
    end_dialogue;
end

procedure Node001 begin
    gsay_reply(NAME, 100);
    giq_option(4, NAME, 101, Node002, NEUTRAL_REACTION);
    giq_option(-3, NAME, 102, Node002, NEUTRAL_REACTION);
end

procedure Node002 begin
    gsay_reply(NAME, 110);
    giq_option(4, NAME, 111, Node999, NEUTRAL_REACTION);
end

procedure Node999 begin
    end_dialogue;
end
```

The matching MSG file would need entries such as:

```text
{100}{}{Hello, traveler.}
{101}{}{Can you tell me about this place?}
{102}{}{Uh... place?}
{110}{}{There is not much to say.}
{111}{}{Thanks.}
```

The exact helper names and argument order can differ by header set. Some projects use macros such as `Reply(100)` or `NOption(101, Node002, 004)` which expand into `gsay_reply`, `giq_option`, `message_str`, and reaction constants. When reverse-engineering a dialogue script, inspect the headers before assuming a source helper maps directly to one opcode.

## Header Macro Expansion Examples

Many official-style scripts are intentionally written in a macro dialect on top of SSL. This makes source readable to authors, but it can hide the actual runtime dependency. The exact expansions depend on the header set, so the examples below describe the usual intent rather than a byte-for-byte expansion.

| Macro-like source | Usually hides | Why it matters |
| --- | --- | --- |
| `Reply(100)` | `message_str(NAME, 100)` plus a dialogue reply call such as `gsay_reply`. | The source appears to reference only `100`, but it also depends on the `NAME` message-list constant and the matching MSG file. |
| `NOption(101, Node002, 004)` | MSG lookup, IQ gate, destination node, and reaction value. | Changing a node name or MSG id can break dialogue even if the macro call still compiles. |
| `GOption(...)` / `BOption(...)` | Gender, reaction, or condition wrappers around `giq_option`. | Dialogue availability can be controlled by macro-side conditions that are easy to miss in decompiled output. |
| `mstr(150)` | `message_str(NAME, 150)` or a project-specific message-list lookup. | Renaming the script or changing `NAME` changes which MSG list is read. |
| `obj_is_carrying_obj_pid(dude_obj, PID_X)` | One or more inventory search/count operations. | Useful source shorthand, but tools should not assume it is a single vanilla opcode. |
| `give_pid_qty(PID_X, 1)` | Object creation plus inventory transfer helpers. | May expand into several object opcodes and depends on PID constants matching PRO/LST data. |

For documentation, quote the source macro when it is useful to modders, but also write down the hidden file dependency: MSG list, PID, script id, global variable, map variable, hook id, or sfall runtime feature.

## scripts.lst and Names

`scripts\scripts.lst` is the bridge between compiled script files and numeric script ids. The physical line number is the script index used by MAP records, PRO script fields, SIDs, and script lookup APIs. A source filename alone does not attach a script to anything; the compiled INT must be listed and referenced.

| Constraint | Consequence |
| --- | --- |
| Line number is data. | Reordering `scripts.lst` changes script ids. |
| `# local_vars=N` metadata controls local variable count. | Changing local-var count affects saved script-instance state and map data. |
| Original-style script names are short. | Fallout 2 CE preserves a 13-byte script base-name limit when reading the list. |
| MSG file matching often follows the script base name. | Renaming a script can break dialogue lookup unless the MSG path/constants are updated. |

Fallout 2 source names often encode area and object role. A common convention uses the first character for location, the second for type, and the remaining characters for a mnemonic name. Examples include `A` for Arroyo, `K` for Klamath, `D` for The Den, `N` for New Reno, and `Z` for generic scripts; role letters include `C` critter, `I` item, `S` scenery, `T` spatial, `P` party, `H` head/talking-head related, and `W` wall. This convention helps humans but is not an SSL grammar rule.

## Naming Conventions

Most SSL names are conventions rather than grammar rules, but the conventions carry useful meaning for readers and tools.

| Convention | Meaning | Why it matters |
| --- | --- | --- |
| `Node001`, `Node002`, ... | Dialogue node procedures. | Dialogue options commonly call these procedures by symbol or by string. Renaming nodes can break dynamic calls and decompiler assumptions. |
| `Node999` | Common "end dialogue" or fallback node convention. | Not magic to the engine; it only matters if the source/header macros call it. |
| `LVar0`, `LVAR_*` | Human-readable names for script local variable slots. | Only persistent if the name maps to a numeric `get_local_var`/`set_local_var` index and `scripts.lst` allocates enough slots. |
| `MVAR_*`, `GVAR_*` | Map variable and global variable constants. | Must match the GAM/global variable data used by the target project. |
| `PID_*`, `SID_*`, `MAP_*` | Prototype ids, script ids, and map ids. | Constants hide numeric links to PRO/LST/MAP data. Rebuilding headers without rebuilding scripts can desynchronize them. |
| `gl*` | sfall global script naming convention. | sfall uses naming/configuration rules to load these scripts outside the vanilla MAP/PRO attachment path. |
| `hs_*` | Older/predefined sfall hook script naming style. | sfall documentation generally recommends registered hooks for better compatibility, but older scripts may still use these names. |

When documenting source, keep the symbolic name and numeric meaning together. A name such as `GVAR_FIND_VIC` is useful only if the reader can trace it to the correct global variable index in the target data set.

## sfall SSLC Extensions

sfall's SSLC is both a compiler update and a language extension layer. Some features only affect source convenience and can compile down to ordinary vanilla behavior; others require sfall opcodes or runtime services and are not portable to the original executable.

| Feature | Portability | Notes |
| --- | --- | --- |
| Integrated preprocessor | Build-tool only. | Improves build workflow; does not by itself require sfall at runtime. |
| Optimizer and command-line macro/include options | Build-tool only. | Useful for reproducible builds and generated source. |
| Optional procedure arguments | Usually source sugar. | Compiler substitutes default constants for omitted arguments. |
| `=` assignment, multiple declarations, initialized variables, hex constants, `++`/`--` | Usually source sugar. | Mostly compiles to ordinary assignments and expressions. |
| `AndAlso`, `OrElse`, `#pragma sce`, `-s` | Behavioral compiler feature. | Short-circuit logic can change evaluation order and side effects. |
| `for`, `switch`, `break`, `continue` | Usually source sugar. | Compiles to lower-level branch/loop bytecode, but may conflict with old identifiers in compatibility mode. |
| Procedure stringify operator `@` | Source/decompiler aid. | Converts procedure symbols to string constants for callback-style calls. |
| Arrays, array expressions, map array expressions, `foreach` | sfall runtime required. | Uses sfall array opcodes and array handles. |
| Global scripts and hooks | sfall runtime required. | Require sfall naming/registration rules and runtime callback support. |
| Direct memory and `call_offset` functions | sfall runtime and unsafe setting required. | Version- and executable-layout-sensitive. |

For shared mods, document whether scripts are vanilla-compatible, sfall-required, or sfall-version-sensitive. If a source uses `sfall_func*`, arrays, hooks, global scripts, or direct sfall opcodes, the resulting INT should be treated as an extended script.

## sfall Version Sensitivity

sfall source compatibility is not only "sfall or not". Some syntax, opcodes, array behavior, hook behavior, and compiler workarounds depend on the sfall and SSLC version targeted by the project.

| Version-sensitive area | Why it matters |
| --- | --- |
| Global and hook script startup. | sfall SSLC documentation notes that sfall 3.4 and below required `procedure start;` before includes that define procedures for global or hook scripts; the note says this is no longer required starting with 3.5. |
| Array syntax and array element sizing. | Array source sugar and runtime array functions evolved over sfall releases. |
| New opcode names and signatures. | Scripts using new sfall functions require a runtime that implements those functions with compatible behavior. |
| Unsafe direct-memory and call-offset helpers. | These depend on executable layout, sfall settings, and sometimes game version. |
| Compiler fixes and compatibility mode. | New compiler versions can reject old source names, fix previously broken emitted code, or change behavior unless `-b` compatibility mode is used. |

Good source releases record the target game executable, sfall version, SSLC version or package, compiler flags, and required `ddraw.ini` settings. Without that information, the same SSL text can compile or run differently in another setup.

## Compiler Compatibility

Compiler choice is part of the format contract. SSL source is not completely portable just because it has the same file extension.

| Topic | Original-style compiler chain | sfall SSLC | Documentation impact |
| --- | --- | --- | --- |
| Preprocessing | External C preprocessor, often selected by batch files. | Integrated preprocessor with include path and macro options. | Record include paths, macro defines, and whether stored source is preprocessed or original. |
| Classic syntax | Expected baseline: `:=`, `begin`/`end`, procedure declarations, classic loops and conditions. | Supports classic syntax and compatibility options. | Classic-looking source is safest for vanilla-oriented projects. |
| Modern syntax sugar | May fail to compile. | Supports features such as `=` assignment, initialized variables, multiple declarations, `for`, `switch`, `break`, and `continue`. | The output may still be vanilla bytecode, but the source requires a modern compiler. |
| Short-circuit logic | Classic boolean behavior is not guaranteed to short-circuit. | Supports explicit short-circuit constructs/options such as `AndAlso`, `OrElse`, and short-circuit mode. | Side effects in conditions should be documented and tested with the intended compiler mode. |
| sfall opcodes | Unsupported. | Supports sfall opcode names, arrays, hooks, global scripts, and direct-memory helpers. | Compiled output requires sfall when sfall opcodes or runtime services are emitted. |
| Optimization | Older compiler behavior is the compatibility target for original scripts. | Has optimizer modes and decompiler-aware options. | Optimized output may be harder to compare with original INT or decompiled source. |
| Decompilation | Not a source-preserving operation. | Can produce more modern/decompiler-friendly source forms. | Mark decompiled SSL as reconstructed unless original source and headers are available. |

### Compatibility Examples

| Source feature | Generated dependency | Compatibility note |
| --- | --- | --- |
| `variable x; x := 1;` | Vanilla bytecode. | Safe for vanilla and sfall runtimes. |
| `x = 1;` | Usually vanilla bytecode. | Requires a compiler that accepts `=` assignment, but the output can still be vanilla-compatible. |
| `for (...)` or `switch` | Usually vanilla branch bytecode. | Requires sfall SSLC or another compiler with this syntax; runtime may still be vanilla-compatible if no sfall opcode is emitted. |
| `AndAlso`, `OrElse`, `#pragma sce` | Changed branch/evaluation bytecode. | Compiler feature with behavior implications. It can alter side effects compared with classic `and`/`or`. |
| `variable a[10];` or `foreach` | sfall array/list opcodes. | Requires sfall-compatible runtime support. |
| `register_hook_proc(...)` | sfall hook opcode/API usage. | Requires sfall and a suitable hook context. |
| `read_int(address)` | sfall direct-memory opcode. | Requires sfall; write/call variants may additionally require unsafe scripting enabled. |
| `sfall_func4("name", ...)` | Dynamic sfall function dispatch. | Runtime dependency is hidden in the string argument, so static tools must inspect literals and data flow. |

## Optimization and Dynamic References

SSL permits references that are obvious at runtime but not obvious to static tools. Optimizers, decompilers, and cross-reference generators must handle these carefully.

| Pattern | Risk | Documentation advice |
| --- | --- | --- |
| Procedure called through a string variable. | A compiler optimizer or static analyzer may not see that the procedure is live. | Use sfall SSLC's `@` operator where possible, mark critical procedures if the compiler supports it, or document the dynamic call explicitly. |
| Dialogue node referenced only inside a macro. | Decompiled or preprocessed output may obscure the original node relationship. | Keep node declarations visible and describe dialogue flow by node name. |
| Hook procedure registered by name or callback helper. | The engine does not call the procedure by normal event-name lookup. | Document the registration site and hook contract. |
| `sfall_func*` string dispatch. | The runtime dependency is hidden in a string literal or value. | List the sfall function names used by the script. |
| Compiler optimization enabled. | Unused procedures, constants, or expression shapes may be removed or rewritten. | Record the optimization level and compare behavior, not only source text. |

For archival documentation, it is useful to keep both the original source and a compiler-produced preprocessed source. The first preserves author intent; the second shows what the compiler actually analyzed.

## Global and Hook Scripts

sfall adds script types that are not part of vanilla Fallout 2's normal object/map attachment model. Global scripts run independently of a loaded map object and use names beginning with `gl`. sfall hook scripts run at specific engine events and can inspect hook arguments or return override values.

| Script type | Important behavior |
| --- | --- |
| Global script | Must have a suitable procedure such as `start`, `map_enter_p_proc`, `map_exit_p_proc`, or `map_update_p_proc`. It has no attached `self_obj` unless the script sets one explicitly. |
| Repeated global script | Calls `set_global_script_repeat(frames)` to choose how often it runs. |
| Global script mode | `set_global_script_type` changes whether execution is tied to local-map, input, world-map, or mixed loops. |
| Hook script | Can use `get_sfall_arg`, `get_sfall_args`, `set_sfall_arg`, and `set_sfall_return` depending on hook type. |
| Registered hook callback | sfall recommends normal global scripts plus `register_hook_proc` or `register_hook` for better compatibility than relying only on predefined `hs_*` scripts. |

## Decompilation and Round-Tripping

INT can be decompiled back to SSL-like text with tools such as `int2ssl`, but the result is not the original source format. Preprocessor macros, comments, include boundaries, symbolic constant names, exact formatting, and some high-level syntax are lost during compilation.

| Original SSL feature | What survives in INT |
| --- | --- |
| Comments and formatting | Lost. |
| `#include` structure | Lost after preprocessing. |
| Macro names | Usually lost; only expanded values and calls remain. |
| Procedure names | Stored in the INT identifier table. |
| String literals | Stored in the INT static string table. |
| Source variable names | May be lost or reconstructed heuristically depending on compiler/decompiler data. |
| High-level sfall syntax | May decompile to lower-level calls or to sfall-aware syntax if the decompiler supports it. |

For documentation, treat decompiled SSL as a readable reconstruction. It can explain behavior, but it should not be assumed to match original Interplay source or to recompile to byte-identical INT without the same headers, preprocessor, compiler version, and optimization settings.

## Common Pitfalls

| Pitfall | Why it breaks | What to check |
| --- | --- | --- |
| Reordering `scripts.lst`. | Script ids are line numbers. MAP, PRO, and saved-game references can silently point at the wrong script. | Diff script indexes, not just filenames. |
| Changing `# local_vars=N` after saves or MAP state exist. | Local variable storage is part of script-instance state. | Regenerate/migrate affected MAP/SAV data or keep slot counts stable. |
| Declaring `variable LVar0;` and expecting persistent storage. | Source variables are compiler slots, not script-instance locals. | Use `get_local_var` and `set_local_var` for persistent script locals. |
| Missing or renamed MSG entries. | Dialogue/message helpers use numeric ids and message-list names at runtime. | Validate every `message_str`, reply, and option id against the MSG file. |
| Relying on `self_obj` in a global script. | sfall global scripts are not attached to a map object by default. | Use `set_self` where appropriate and clear it after use. |
| Assuming compiler sugar is runtime-neutral. | Some sfall SSLC syntax compiles to vanilla bytecode; other syntax emits sfall-only opcodes. | Inspect the compiled INT opcode range or compiler output. |
| Using `and`/`or` when a condition has side effects. | Classic eager evaluation and sfall short-circuit modes can behave differently. | Use explicit temporaries or document short-circuit assumptions. |
| Calling a procedure only through a string. | Optimizers and decompilers may not see the reference. | Mark the procedure critical, avoid over-aggressive optimization, or use sfall SSLC's `@` operator where available. |
| Treating decompiled SSL as original source. | Macros, comments, include boundaries, symbolic constants, and some high-level structure are lost. | Prefer original source and headers; otherwise document that the file is reconstructed. |
| Changing header constants without rebuilding all scripts. | Constants are compiled into INT values. | Recompile every dependent SSL file and update related MAP/PRO/MSG data if needed. |

## Save Compatibility

Script changes can be save-breaking even when the SSL still compiles. Saved games can contain active script instances, local variables, map state, timers, object references, and script ids that were created by older versions of the scripts.

| Change | Save risk | Safer approach |
| --- | --- | --- |
| Reordering `scripts.lst`. | Existing MAP/SAV references can point at the wrong INT. | Append new scripts or migrate every reference deliberately. |
| Changing `# local_vars=N` or repurposing local slots. | Existing script instances keep old slot values with new meanings. | Reserve old slots, add new slots at the end, or write migration logic. |
| Changing timer arguments. | Already scheduled timers may fire with old argument values. | Keep old handlers during a transition or ignore unknown timer args safely. |
| Changing script attachment on PRO/MAP objects. | Objects already placed or saved may retain old script state. | Regenerate affected MAP data or provide compatibility behavior in the new script. |
| Renaming scripts or MSG files. | Runtime lookup by script id, base name, or message-list constant can break. | Keep aliases/constants stable until all references are migrated. |
| Changing global variable meanings. | Existing saves keep old global values. | Use version flags and explicit migration code where possible. |

When documenting a script update, state whether it is safe for existing saves, requires a new game, or requires map/save migration. This is often more important to players than the source-level change itself.

## Debugging and Logging

SSL source often uses temporary messages while developing. Those diagnostics are useful, but they can become noisy or user-visible if left in release scripts.

| Technique | Use | Release caution |
| --- | --- | --- |
| `debug_msg(text)` | Write diagnostic text to the runtime debug log. | Good for developer builds; avoid excessive per-frame or per-update logging. |
| `display_msg(text)` | Show text in the interface message area. | User-visible, so it should normally be real game text or guarded by a debug macro. |
| `float_msg(obj, text, color)` | Show a floating message above an object. | Useful for testing object context; remove or gate temporary diagnostics. |
| Debug macros such as `DEBUG` or `GVAR_DEBUG_MODE`. | Compile-time or runtime switches around diagnostic output. | Document whether the switch is a preprocessor macro, global variable, or project-specific helper. |
| sfall hook/global diagnostics. | Trace hook arguments, return values, and repeated global-script behavior. | Repeated scripts can flood logs quickly; throttle or disable before release. |

```text
#ifdef DEBUG
debug_msg("example: map_enter_p_proc");
#endif
```

For reproducible bug reports, include the script name, compiled INT timestamp/version, sfall version if used, relevant MSG/GAM constants, and whether the log came from a debug or release build.

## Authoring Notes

- Keep source, compiled INT, `scripts.lst`, MSG, MAP, and PRO changes together. Script behavior often crosses file boundaries.
- Preserve script line numbers in `scripts.lst` unless you intentionally migrate every reference.
- Use symbolic constants from headers instead of raw numbers where possible. This makes decompiled/ported scripts easier to audit.
- Do not confuse source variables with persistent local/map/global variables. Use `get_local_var`, `set_local_var`, `get_map_var`, and `get_global_var` deliberately.
- Prefer editing SSL and recompiling over editing INT directly. INT editing requires correct offsets, procedure tables, string tables, and stack behavior.
- When using sfall syntax, note whether the feature is source-only sugar or requires sfall at runtime.
- For hooks and global scripts, avoid relying on implicit `self_obj`; many sfall contexts do not have an attached object.
- For dialogue, reserve stable MSG id ranges and keep script message ids synchronized with the MSG file.
- For source releases, include the headers needed to compile the scripts. SSL without its matching headers is often incomplete.

## Validation Checklist

- Every included header can be found by the chosen preprocessor/compiler include paths.
- The source compiles with the intended compiler, not merely with a different modern compiler.
- Generated INT is listed in `scripts.lst` at the expected line.
- `# local_vars=N` in `scripts.lst` matches the script's use of local variable indexes.
- MAP and PRO references point at the intended script index and script type.
- Dialogue MSG files exist for scripts that call message/dialogue helpers.
- Message ids used by the script exist in the matching MSG file.
- sfall-only source features are allowed by the target runtime and documented for users.
- Unsafe sfall memory/call features are avoided unless absolutely necessary and the required `ddraw.ini` setting is documented.
- Decompiled source is not treated as byte-identical original source unless the build has been verified.

## What SSL Alone Cannot Tell You

An SSL file is source code, not a complete script installation. Important runtime facts live elsewhere.

| Missing from SSL alone | Where to look |
| --- | --- |
| Numeric script id. | `scripts.lst` line number and any header constant that names it. |
| Allocated script local variable count. | `scripts.lst` metadata such as `# local_vars=N`. |
| Whether the script is attached to a map, critter, item, scenery object, wall, or spatial trigger. | MAP records, PRO script ids, SID records, sfall global/hook configuration, and naming rules. |
| Object placement, tile, elevation, trigger radius, and prototype data. | MAP and PRO files. |
| Dialogue/message text and audio tags. | MSG files and any message-list constants in headers. |
| Meaning of global and map variable indexes. | GAM files, map variable definitions, and project headers. |
| Target runtime requirements. | Compiler command line, sfall version, ddraw.ini settings, and whether sfall opcodes/hooks/arrays are used. |
| Original author intent after compilation/decompilation. | Original source comments, headers, build scripts, and version history. Decompiled SSL cannot recover all of this. |

This is why script documentation should treat SSL, INT, `scripts.lst`, MSG, MAP, PRO, GAM, headers, and compiler settings as one connected system.

## Related Formats

- [INT File Format](int.html) - compiled bytecode emitted from SSL.
- [LST File Format](lst.html) - `scripts.lst` maps line indexes to compiled scripts.
- [MSG File Format](msg.html) - dialogue and message text referenced from scripts.
- [MAP File Format](map.html) - map script records, script locals, spatial scripts, and object script references.
- [PRO File Format](pro.html) - prototype script ids for items, critters, scenery, and walls.
- [GAM File Format](gam.html) - global and map variable definitions used by script headers.

## Source References

- [sfall SSLC documentation](https://sfall-team.github.io/sfall/sslc/) - compiler options, added syntax, compatibility mode, preprocessing, optimizer, and int2ssl notes.
- [sfall data types](https://sfall-team.github.io/sfall/data-types/) - practical script documentation types such as `int`, `float`, `string`, `ObjectPtr`, `array`, `any`, and `void`.
- [sfall global scripts](https://sfall-team.github.io/sfall/global-scripts/) - global script naming, required procedures, repeat behavior, and execution modes.
- [sfall hooks](https://sfall-team.github.io/sfall/hooks/) - hook script behavior, hook arguments/returns, and registered-hook compatibility advice.
- [sfall opcode list](https://github.com/sfall-team/sfall/blob/master/artifacts/scripting/sfall%20opcode%20list.md) - opcode numbers and signatures for sfall-only functions compiled from SSL.
- [Fallout 2 script library](https://fallout.fandom.com/wiki/Fallout_2_script_library) - decompiled script corpus and historical notes about the external preprocessor build chain.
- [Fallout 2 dialogue files](https://fallout.wiki/wiki/Fallout_2_dialogue_files) - naming convention notes tying SSL scripts to MSG dialogue files.
- [SCRIPTS.LST (Fallout 2)](https://fallout.wiki/wiki/SCRIPTS.LST_%28Fallout_2%29) - script list examples, local variable metadata, and script index context.
- [Fallout 2 CE `scripts.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/scripts.cc) - runtime loading of `scripts.lst`, local variable counts, script IDs, procedure lookup, and MSG lookup behavior.
- [Fallout 2 CE `interpreter.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/interpreter.cc) and [`interpreter_extra.cc`](https://github.com/alexbatalov/fallout2-ce/blob/main/src/interpreter_extra.cc) - runtime interpreter behavior and built-in game opcode handlers that SSL compiles to.
