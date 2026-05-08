# Fallout 1 & 2 Engine and File Format Documentation

This repository contains static HTML documentation for Fallout 1 and Fallout 2
engine behavior, resource formats, modding data, reverse-engineering notes, and
tooling references.

The hosted copy is available at:

https://fodev.net/files/fo2/

## Audience

These notes are intended for:

- mod authors editing Fallout 1/2 data files
- tool authors writing readers, validators, converters, or editors
- reverse engineers comparing executable behavior with community notes
- maintainers documenting sfall, Fallout 2 Community Edition, and related tools

The documentation is mostly written from a practical compatibility point of view:
what the game, tools, and common modding workflows actually read, write, or
depend on.

## Where to Start

- `index.html` is the main entry point and resource index.
- File formats are documented in pages such as `dat.html`,
  `frm.html`, `pro.html`, `map.html`,
  `int.html`, and `ssl.html`.
- Executable and reverse-engineering references include
  `structs.html`, `symbols.html`,
  `fallout2_re.html`, and `sfall_refs.html`.
- Tool and project lists live in `tools.html` and `mods.html`.

Useful reading paths:

- Art and interface work: `DAT`, `FRM`, `PAL/COL`, `LST`, `PRO`
- Script work: `SSL`, `INT`, `MSG`, `SCRIPTS.LST`, `MAP`
- Data editing: `DAT`, `PRO`, `MAP`, `MSG`, `GAM`, `CFG/INI`
- Reverse engineering: structures, symbols, Fallout 2 RE references, sfall refs

## Repository Shape

The published site is static HTML plus CSS. Documentation pages are authored in
Markdown and generated into `_site/`.

- `style.css` - shared page styling
- `img/` - images used by pages
- `highslide/` - bundled image viewer assets used by older pages
- `symbols/` - supporting symbol/reference pages
- `fallout2.sym` - symbol data
- `content/` - Markdown sources, data files, and templates
- `tools/FoDocs.Generator/` - .NET 10 static site generator

Pages should be reviewed from `_site/` after running the generator.

## Generating the Site

Markdown pages in `content/pages` are rendered into the output site and keep
the public `.html` URLs used by the hosted copy.

Requirements:

- .NET 10 SDK

Generate the site:

```powershell
dotnet run --project tools/FoDocs.Generator -- --clean
```

The output is written to `_site/`. Generated files and .NET build artifacts are
ignored by Git.

Current generated pages:

- `aaf.html` from `content/pages/aaf.md`
- `acm.html` from `content/pages/acm.md`
- `ai_txt.html` from `content/pages/ai_txt.md`
- `anim_names.html` from `content/pages/anim_names.md`
- `bio.html` from `content/pages/bio.md`
- `cfg.html` from `content/pages/cfg.md`
- `criticals.html` from `content/pages/criticals.md`
- `dat.html` from `content/pages/dat.md`
- `elevators.html` from `content/pages/elevators.md`
- `fallout2_re.html` from `content/pages/fallout2_re.md` and `content/data/fallout2_re.tsv`
- `fo1in2.html` from `content/pages/fo1in2.md`
- `fo_colors.html` from `content/pages/fo_colors.md`
- `fon.html` from `content/pages/fon.md`
- `frm.html` from `content/pages/frm.md`
- `gam.html` from `content/pages/gam.md`
- `gcd.html` from `content/pages/gcd.md`
- `hrp.html` from `content/pages/hrp.md`
- `index.html` from `content/pages/index.md`
- `int.html` from `content/pages/int.md`
- `lip.html` from `content/pages/lip.md`
- `lst.html` from `content/pages/lst.md`
- `map.html` from `content/pages/map.md`
- `mods.html` from `content/pages/mods.md`
- `msk.html` from `content/pages/msk.md`
- `msg.html` from `content/pages/msg.md`
- `mve.html` from `content/pages/mve.md`
- `pal.html` from `content/pages/pal.md`
- `party_txt.html` from `content/pages/party_txt.md`
- `pipboy_txt.html` from `content/pages/pipboy_txt.md`
- `pro.html` from `content/pages/pro.md`
- `rix.html` from `content/pages/rix.md`
- `savegame.html` from `content/pages/savegame.md`
- `scripts_lst.html` from `content/pages/scripts_lst.md`
- `sfall_refs.html` from `content/pages/sfall_refs.md` and `content/data/sfall_refs.tsv`
- `ssl.html` from `content/pages/ssl.md`
- `structs.html` from `content/pages/structs.md`
- `symbols.html` from `content/pages/symbols.md` and `content/data/symbols.tsv`
- `sve.html` from `content/pages/sve.md`
- `tools.html` from `content/pages/tools.md`
- `worldmap_config.html` from `content/pages/worldmap_config.md`
- `worldmap_dat.html` from `content/pages/worldmap_dat.md`
- `docs-generator.html` from `content/pages/docs-generator.md`

Source work should happen under `content/pages`, and the generated `_site/`
directory is the best target for checking pages before publishing.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for writing conventions, source/reference
expectations, and lightweight validation checks.

Good contributions include:

- clarifying ambiguous format fields
- adding verified engine behavior
- documenting cross-file dependencies
- adding small examples or validation notes
- fixing broken links, invalid markup, or stale tool references
- marking uncertain legacy notes as unverified instead of presenting them as fact

## Source and Verification Notes

When possible, prefer source-backed statements and cite the relevant project or
page. Common anchors include Fallout 2 Community Edition, sfall, original tools,
known community writeups, and direct binary/resource inspection.

If behavior differs between Fallout 1, Fallout 2, Fallout 2 Community Edition,
sfall, or a modding tool, document the distinction explicitly.
