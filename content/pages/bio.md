---
title: BIO File Format
output: bio.html
description: Fallout BIO premade-character biography text files, display behavior, localization, custom premade support, and parser notes.
---

# BIO File Format

BIO files are plain text biographies shown on the premade character selection screen. They live in the `premade` directory and are paired with [GCD files](gcd.html) that use the same base name.

In an unmodified Fallout 2 installation the three selectable premade characters are loaded from these base paths:

| Character role | BIO file | GCD file | Face FID |
|---|---|---|---|
| Combat | `premade\combat.bio` | `premade\combat.gcd` | `201` |
| Stealth | `premade\stealth.bio` | `premade\stealth.gcd` | `202` |
| Diplomat | `premade\diplomat.bio` | `premade\diplomat.gcd` | `203` |

The BIO file contains only the displayed story text. The character's statistics and traits come from the matching GCD file, and the face art is selected by the character selector's premade-character table or by a modded configuration.

## Display behavior

The game opens the selected `.bio` file in text mode and reads it line by line into a 256 byte buffer. Each line is drawn at the biography panel position using font `101` and color table entry `992`.

| Property | Value |
|---|---|
| Starting position | `x = 438`, `y = 40` |
| Drawing width | `202` pixels (`640 - 438`) |
| Vertical limit | Lines are drawn while `y < 260` |
| Line buffer | `256` bytes per line, including terminator |
| Missing file | No biography text is drawn; the selector still opens normally |

There is no word wrapping and no scrolling. A long line is not reflowed onto the next line, so authors need to insert line breaks manually. The old Team-X recommendation of keeping lines to about 26 characters is a useful practical rule, but the real limit is the pixel width of the rendered text. Narrow letters can fit more characters than wide letters.

The number of visible lines depends on font `101`'s line height. The original guidance to keep the biography to roughly 22 content lines is still sensible because the renderer stops once the next line would start at or below `y = 260`.

## Formatting recommendations

- Use plain text and manually wrap every line.
- Keep the heading short and uppercase, for example `NARG'S STORY:`.
- Leave a blank line after the heading, matching the vanilla BIO files.
- Avoid curly quotes and other Unicode punctuation unless the target game build and font encoding are known to support them.
- Test the BIO in game or in a renderer that uses the same font metrics; character count alone is only an approximation.

## Localization

When the active language is not English, Fallout 2 CE checks for a localized premade path before falling back to the base file. For example, when the selector asks for `premade\combat.bio`, the localized lookup can use `premade\<language>\combat.bio` if that file exists. The same lookup is used for the matching GCD file.

## Custom premade characters

Fallout 2 CE includes sfall-compatible support for custom premade characters. Mods can provide a comma-separated list of premade base names and a matching list of face FIDs. The selector then builds `premade\<name>.bio` and `premade\<name>.gcd` from each base name.

In this implementation, custom base names longer than 11 characters are rejected before the `premade\` prefix is added. Keeping short DOS-style names is therefore the safest choice for custom premade sets.

## Example

Taken from Narg's BIO file, `combat.bio`, exactly as it is shown in Fallout 2:

```text
NARG'S STORY:

Narg's exceptional
physique has made him
one of the best hunters
in the tribe. Narg's 
first, and usually only,
impulse is to crush 
anything that he can't
figure out. Narg has
become quite adept at
crushing, and slicing,
and dicing. Narg would
like to prove his
worthiness to lead the
tribe and he'll let
nothing stand in his
way.
```

## Parser notes

- BIO is not a structured binary format; any text editor can open it.
- Blank lines are real lines and consume vertical space.
- Text mode reading means line endings are handled by the platform file layer.
- The file extension is added by the selector; the premade base path is stored without `.bio` or `.gcd`.

## Source References

- [Fallout 2 CE - character_selector.cc](https://github.com/alexbatalov/fallout2-ce/blob/main/src/character_selector.cc)

## History

2019-12-14 - Ported from [Vault-Tec Labs BIO File Format](https://falloutmods.fandom.com/wiki/BIO_File_Format) by [ghost](https://github.com/ghost2238)

2007 - [Translated by Kanhef](https://www.nma-fallout.com/threads/teamx-documents-russian-to-english-translation.179561/)

? - Written by Team-X (bio_format.shtml)
