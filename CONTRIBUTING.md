# Contributing

Thanks for helping keep these Fallout 1/2 notes useful. The project is a static
HTML documentation site, so the best contributions are clear, specific, and easy
to verify.

## Goals

Documentation should help readers answer practical questions:

- What file or engine feature is this?
- Where is it used?
- What exact layout, field, key, opcode, or table value matters?
- Which game/runtime/tool behavior is verified?
- What breaks if a modder or tool author changes it?

Prefer precise, source-backed notes over broad rewrites. Small corrections are
welcome.

## Page Structure

For new or heavily revised format pages, use this shape when it fits:

1. Title and short summary
2. Format properties or quick facts
3. Where the file/data appears
4. Layout, syntax, or table definitions
5. Engine behavior and edge cases
6. Examples
7. Validation or tooling notes
8. Related formats/pages
9. Source references
10. Changelog, when the page already keeps one

Not every page needs every section. Generated references and simple index pages
can stay simpler.

## Writing Style

- Write in English.
- Be direct and technical, but explain terms the first time they appear.
- Use `Fallout 1`, `Fallout 2`, `Fallout 2 CE`, and `sfall` consistently.
- Use uppercase file format names in headings when that is the common name:
  `DAT`, `FRM`, `PRO`, `MAP`, `MSG`, `INT`, `SSL`.
- Use backticks in Markdown and `<tt>` in existing HTML for filenames, keys,
  offsets, constants, and short code-like values.
- Keep examples small and focused.
- Avoid presenting guesses as facts. Mark uncertain behavior as unverified or
  needing confirmation.

## Source References

Pages that describe engine behavior should include references when possible.
Good references include:

- Fallout 2 Community Edition source files
- sfall source, artifacts, or documentation
- original Interplay/BIS tools or scripts
- established community format notes
- direct observations from shipped game data

When behavior is runtime-specific, say so. For example:

- "Fallout 2 CE does..."
- "sfall adds..."
- "Original format notes describe..."
- "Shipped Fallout 2 data uses..."

If older notes conflict with source-backed behavior, keep the older note only if
it helps readers understand the history, and clearly identify the newer verified
behavior.

## HTML Conventions

The site is plain HTML. When editing existing pages, prefer local consistency
over large formatting churn.

- Use `<!DOCTYPE html>` for new pages.
- Link the shared stylesheet with `style.css`.
- Prefer semantic headings (`h1`, `h2`, `h3`) over visual-only markup.
- Keep tables readable: include header rows, stable column order, and concise
  cell text.
- Use `<pre>` for code, binary layout sketches, and multi-line examples.
- Use relative links for pages inside this repository.
- Avoid adding new inline CSS when a shared class in `style.css` would work.
- Do not reformat generated reference tables unless the content actually needs
  to change.

## Markdown Sources

Documentation pages live as Markdown sources under `content/pages`. Pages should
preserve their public URLs with frontmatter:

```markdown
---
title: DAT File Format
output: dat.html
description: Short page summary for generated metadata.
---
```

Source guidelines:

- Keep the original page title and heading order unless there is a clear reason
  to improve them.
- Convert simple HTML tables to Markdown tables.
- Convert `<tt>value</tt>` to Markdown backticks.
- Keep internal links relative, such as `[MSG](msg.html)`.
- Use fenced code blocks with a language hint when obvious, such as `text`,
  `c`, or `powershell`.
- Use a fenced `toc` block for curated floating page navigation. The block
  accepts normal Markdown list links and renders as the shared floating menu.
- Percent-encode literal parentheses in Markdown link URLs, for example
  `%28en%29`, because plain `)` terminates links in the current lightweight
  renderer.
- Leave complex generated or interactive pages as legacy HTML until the
  generator has a better source model for them.
- Review the generated `_site/<page>.html` before publishing changes.

Currently migrated pages are `aaf.html`, `acm.html`, `ai_txt.html`, `anim_names.html`, `bio.html`,
`cfg.html`, `criticals.html`, `dat.html`, `elevators.html`, `fallout2_re.html`,
`fo1in2.html`,
`fo_colors.html`, `fon.html`, `frm.html`,
`gam.html`, `gcd.html`, `hrp.html`, `index.html`, `int.html`, `lip.html`, `lst.html`,
`map.html`, `mods.html`, `msk.html`, `msg.html`, `mve.html`, `pal.html`, `party_txt.html`,
`pipboy_txt.html`, `pro.html`, `rix.html`, `savegame.html`,
`scripts_lst.html`, `sfall_refs.html`, `ssl.html`, `structs.html`,
`symbols.html`, `sve.html`, `tools.html`,
`worldmap_config.html`, `worldmap_dat.html`, and the generator note page
`docs-generator.html`.

## Data and Compatibility Notes

Many Fallout formats are index-sensitive. When documenting edits, call out
compatibility risks such as:

- list reordering changing ids or FIDs
- `scripts.lst` line numbers changing script references
- MAP, PRO, SAVE, MSG, and SSL dependencies
- save-game compatibility
- case sensitivity on non-Windows platforms
- differences between packed archive paths and loose file paths

This context is often more useful than the raw field table alone.

## Lightweight Checks

Before submitting a change:

1. Open the edited page in a browser.
2. Follow nearby internal links.
3. Check tables for malformed rows or unclosed links.
4. Search for accidental placeholder text such as `TODO` or `FIXME`.

Useful local commands:

```powershell
rg -n "TODO|FIXME|unknown|Unknown"
rg -n -g "*.html" '<a href="[^"]*$'
```

When changing the generator, templates, or migrated Markdown pages, run:

```powershell
dotnet build tools/FoDocs.Generator/FoDocs.Generator.csproj
dotnet run --project tools/FoDocs.Generator -- --clean
```

For migrated pages, also compare the original and generated heading outlines:

```powershell
Select-String -Path pal.html -Pattern '<h1|<h2|<h3' | ForEach-Object { $_.Line -replace '<[^>]+>','' }
Select-String -Path _site\pal.html -Pattern '<h1|<h2|<h3' | ForEach-Object { $_.Line -replace '<[^>]+>','' }
```

If you have an HTML validator or link checker available, run it on the edited
pages. Fixing invalid markup is especially helpful because many pages contain
large tables where small HTML mistakes are easy to miss.

## Changelog and Dates

Some pages keep local history sections and some do not. Preserve existing page
style unless you are intentionally standardizing it.

When adding a dated note:

- use `YYYY-MM-DD`
- describe the documentation change, not just "updated"
- include attribution only when it adds useful context

## Scope

Keep changes focused. A good pull request usually does one thing:

- documents one format or behavior
- fixes a set of related links
- standardizes one page pattern
- updates one tool/reference list
- repairs markup in a small group of files

Large mechanical cleanups are easier to review when they are separate from
content changes.
