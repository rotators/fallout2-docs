---
title: Documentation Generator
output: docs-generator.html
description: Notes for the static documentation generator migration.
---

# Documentation Generator

This page is generated from Markdown by the local C# documentation generator.
It documents the migration path from hand-authored HTML to Markdown sources and
shared templates.

The generator is intentionally small:

- it copies existing legacy HTML pages into the output directory
- it renders Markdown pages from `content/pages`
- generated pages overwrite copied legacy pages with the same output path
- the shared layout lives in `content/templates/layout.html`
- the output directory is `_site`

Run it from the repository root:

```powershell
dotnet run --project tools/FoDocs.Generator -- --clean
```

The generated site is written to `_site`.

## Current Migration Status

The following pages have Markdown sources:

| Output | Source |
|---|---|
| `aaf.html` | `content/pages/aaf.md` |
| `acm.html` | `content/pages/acm.md` |
| `ai_txt.html` | `content/pages/ai_txt.md` |
| `anim_names.html` | `content/pages/anim_names.md` |
| `bio.html` | `content/pages/bio.md` |
| `cfg.html` | `content/pages/cfg.md` |
| `criticals.html` | `content/pages/criticals.md` |
| `dat.html` | `content/pages/dat.md` |
| `elevators.html` | `content/pages/elevators.md` |
| `fallout2_re.html` | `content/pages/fallout2_re.md` and `content/data/fallout2_re.tsv` |
| `fo1in2.html` | `content/pages/fo1in2.md` |
| `fo_colors.html` | `content/pages/fo_colors.md` |
| `fon.html` | `content/pages/fon.md` |
| `frm.html` | `content/pages/frm.md` |
| `gam.html` | `content/pages/gam.md` |
| `gcd.html` | `content/pages/gcd.md` |
| `hrp.html` | `content/pages/hrp.md` |
| `index.html` | `content/pages/index.md` |
| `int.html` | `content/pages/int.md` |
| `lip.html` | `content/pages/lip.md` |
| `lst.html` | `content/pages/lst.md` |
| `map.html` | `content/pages/map.md` |
| `mods.html` | `content/pages/mods.md` |
| `msk.html` | `content/pages/msk.md` |
| `msg.html` | `content/pages/msg.md` |
| `mve.html` | `content/pages/mve.md` |
| `pal.html` | `content/pages/pal.md` |
| `party_txt.html` | `content/pages/party_txt.md` |
| `pipboy_txt.html` | `content/pages/pipboy_txt.md` |
| `pro.html` | `content/pages/pro.md` |
| `rix.html` | `content/pages/rix.md` |
| `savegame.html` | `content/pages/savegame.md` |
| `scripts_lst.html` | `content/pages/scripts_lst.md` |
| `sfall_refs.html` | `content/pages/sfall_refs.md` and `content/data/sfall_refs.tsv` |
| `ssl.html` | `content/pages/ssl.md` |
| `structs.html` | `content/pages/structs.md` |
| `symbols.html` | `content/pages/symbols.md` and `content/data/symbols.tsv` |
| `sve.html` | `content/pages/sve.md` |
| `tools.html` | `content/pages/tools.md` |
| `worldmap_config.html` | `content/pages/worldmap_config.md` |
| `worldmap_dat.html` | `content/pages/worldmap_dat.md` |
| `docs-generator.html` | `content/pages/docs-generator.md` |

## Migration Notes

Migrated pages can keep their old public URLs by setting `output` in frontmatter:

```markdown
---
title: DAT File Format
output: dat.html
---
```

Large generated or interactive pages can stay as legacy HTML until they have a
better source representation.

Simple Markdown is supported today: headings, paragraphs, links, inline code,
fenced code blocks, ordered and unordered lists, pipe tables, fenced `raw-html`
blocks for page-specific interactive islands, fenced `fallout-palette` blocks
for compact color palette data, fenced `symbol-table` blocks for large symbol
references backed by `content/data`, and fenced `tsv-table` blocks for generated
reference tables. The renderer is deliberately conservative and has no external
package dependency yet.

When a Markdown URL contains literal parentheses, percent-encode them as `%28`
and `%29`.
