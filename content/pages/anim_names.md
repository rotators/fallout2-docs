---
title: Animation Naming conventions
output: anim_names.html
description: Critter animation filename suffixes for Fallout and Fallout 2 FRM art, including basic animations, knockdown/death animations, position changes, and single-frame death art.
---

# Animation Naming conventions

### Technicalities

A critter consists of many [FRM](frm.html) files. The engine does not store most critter art as direct paths in PRO or MAP data; it builds paths from an art [FID](lst.html), the base name in `art\critters\critters.lst`, the animation code, the weapon animation code, and sometimes a directional suffix.

For example, a base name such as `hmjmps` can become `hmjmpsaa.frm` for stand/no-weapon art. Directional knockdown/death art can become `hmjmpsba.fr0`, `hmjmpsba.fr1`, and so on. See [LST File Format and FID Resolution](lst.html) for the packed FID fields, critter alias metadata, and exact filename construction rules.

Basically, there are 2 different types of this format, each of them with unique extension:

- `*.FRM` extension - a single `*.frm` file containing up to 6 directions of a sprite.
- `*.FR0`-`*.FR5` extension - `*.frm` animation divided to 6 different frm files, usually used for 6 sprite directions of death animations.

Each of the frm/fr0-fr5 files represents a kind of animation. Name of the frm files designate the type of animation, as shown below.

Note: in Fallout 2 CE, most non-death critter animations are forced to the normal `.frm` path. Directional `.fr0`-`.fr5` lookup is mainly used for the knockdown/death animation range, with fallback behavior if the requested direction is missing.

### Basic Animations

| Animation | Filename suffix |
|---|---|
| `stand` | `*aa.frm` |
| `walk` | `*ab.frm` |
| `magic_hands_ground` | `*ak.frm` |
| `magic_hands_middle` | `*al.frm` |
| `dodge_anim` | `*an.frm` |
| `hit_from_front` | `*ao.frm` |
| `hit_from_back` | `*ap.frm` |
| `throw_punch` | `*aq.frm` |
| `kick_leg` | `*ar.frm` |
| `throw_anim` | `*as.frm` |
| `running` | `*at.frm` |

### Knockdown and Death

| Animation | Filename suffix |
|---|---|
| `fall_back` | `*ba.frm` |
| `fall_front` | `*bb.frm` |
| `big_hole` | `*bd.frm` |
| `chunks_of_flesh` | `*bf.frm` |
| `dancing_autofire` | `*bg.frm` |
| `electrify` | `*bh.frm` |
| `sliced_in_half` | `*bi.frm` |
| `burned_to_nothing` | `*bj.frm` |
| `electrified_to_nothing` | `*bk.frm` |
| `exploded_to_nothing` | `*bl.frm` |
| `melted_to_nothing` | `*bm.frm` |
| `fire_dance` | `*bn.frm` |
| `fall_back_blood` | `*bo.frm` |
| `fall_front_blood` | `*bp.frm` |

### Change Positions

| Animation | Filename suffix |
|---|---|
| `prone_to_standing` | `*ch.frm` |
| `back_to_standing` | `*cj.frm` |

### Single-Frame Death Animations

The last frame of knockdown and death animations.

| Animation | Filename suffix |
|---|---|
| `fall_back_sf` | `*ra.frm` |
| `fall_front_sf` | `*rb.frm` |
| `big_hole_sf` | `*rd.frm` |
| `chunks_of_flesh_sf` | `*rf.frm` |
| `dancing_autofire_sf` | `*rg.frm` |
| `electrify_sf` | `*rh.frm` |
| `sliced_in_half_sf` | `*ri.frm` |
| `burned_to_nothing_sf` | `*rj.frm` |
| `electrified_to_nothing_sf` | `*rk.frm` |
| `exploded_to_nothing_sf` | `*rl.frm` |
| `melted_to_nothing_sf` | `*rm.frm` |
| `fire_dance_sf` | `*rn.frm` |
| `fall_back_blood_sf` | `*ro.frm` |
| `fall_front_blood_sf` | `*rp.frm` |
| `targeting picture` | `*na.frm` |

## History

2020-01-16 - Updated information and moved from readme.txt to html.

2007-07-22 - Written by Lisac2k.
