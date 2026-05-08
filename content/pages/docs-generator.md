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
| `dat.html` | `content/pages/dat.md` |
| `frm.html` | `content/pages/frm.md` |
| `pal.html` | `content/pages/pal.md` |
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
fenced code blocks, ordered and unordered lists, and pipe tables. The renderer is
deliberately conservative and has no external package dependency yet.

When a Markdown URL contains literal parentheses, percent-encode them as `%28`
and `%29`.
