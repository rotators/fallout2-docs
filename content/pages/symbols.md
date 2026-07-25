---
title: Fallout2.exe Symbols
output: symbols.html
description: Fallout 2 executable metadata, preferred virtual addresses, function symbols, and variable symbols.
width: full
---

# Fallout2.exe Symbols

The exe in question is a modified version (for HR mod) of Fallout 2 US version 1.02d.

## Metadata

Filename: `fallout2.exe`  
MD5: `3347f6d10bb3d7c02d3614bcf406a912`  
SHA-1: `f6eb5e7dd988699f510677e4ac3b03bfbc878f17`  
SHA-256: `048684f802b05859e2b160fa358be1b99075bf31b49a312a536645581aa16b5f`  
Compilation Timestamp: `1998-12-12 00:56:02`

## Address Basis

The table contains preferred 32-bit virtual addresses from the Fallout 2 executable address map, not relative virtual addresses or file offsets. This address map assumes the normal executable image base `0x00400000`.

To convert a table value to an RVA, subtract the image base:

```text
RVA = preferred virtual address - 0x00400000
0x00410010 -> RVA 0x00010010
```

A file offset cannot be obtained by subtracting the image base alone. Convert the RVA through the PE section table using the section's virtual address and raw-data offset.

These addresses are specific to the executable identified by the hashes above. Other regional releases, patches, or modified executables can place the same function at different addresses. Confirm the executable hash before using an address for patching, debugging, or runtime calls.

```symbol-table
source: symbols.tsv
```
