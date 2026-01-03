---
name: vrm-json-reader
description: Extract and analyze JSON metadata from VRM avatar files. Use when working with VRM files (.vrm) to read meta information, humanoid bone mappings, materials, blendshapes/expressions, springbone physics, or any embedded JSON data. Supports both VRM 0.x and VRM 1.0 formats.
---

# VRM JSON Reader

Extract and analyze JSON sections from VRM avatar files.

## Quick Reference

VRM files are glTF 2.0 binary containers with embedded JSON. The JSON chunk contains:

| Section | VRM 0.x Path | VRM 1.0 Path |
|---------|--------------|--------------|
| Meta | `extensions.VRM.meta` | `extensions.VRMC_vrm.meta` |
| Humanoid | `extensions.VRM.humanoid` | `extensions.VRMC_vrm.humanoid` |
| BlendShape | `extensions.VRM.blendShapeMaster` | `extensions.VRMC_vrm.expressions` |
| SpringBone | `extensions.VRM.secondaryAnimation` | `extensions.VRMC_springBone` |
| FirstPerson | `extensions.VRM.firstPerson` | `extensions.VRMC_vrm.firstPerson` |
| LookAt | `extensions.VRM.lookAt` | `extensions.VRMC_vrm.lookAt` |
| Materials | `materials[]` | `materials[]` + `extensions.VRMC_materials_mtoon` |
| MToon | `extensions.VRM.materialProperties` | `extensions.VRMC_materials_mtoon` |

## Using the Extraction Script

Extract specific sections:

```bash
python scripts/extract_vrm_json.py avatar.vrm --section meta
python scripts/extract_vrm_json.py avatar.vrm --section humanoid
python scripts/extract_vrm_json.py avatar.vrm --section springbone
```

Get summary analysis:

```bash
python scripts/extract_vrm_json.py avatar.vrm --summary
```

Extract entire JSON:

```bash
python scripts/extract_vrm_json.py avatar.vrm --all
```

Save to file:

```bash
python scripts/extract_vrm_json.py avatar.vrm --summary -o analysis.json
```

## Available Sections

- `meta` - Title, author, license, version
- `humanoid` - Bone mappings for humanoid skeleton
- `materials` - glTF material definitions
- `mtoon` - MToon shader properties (shade color, outline, rim light, etc.)
- `nodes` - Scene graph nodes
- `meshes` - Mesh geometry references
- `skins` - Skinning data
- `textures` - Texture references
- `images` - Image data references
- `blendshape` - VRM 0.x blend shape groups
- `expressions` - VRM 1.0 expressions
- `springbone` - Physics bone chains
- `lookat` - Eye/head tracking settings
- `firstperson` - First-person view settings
- `vrm` - Full VRM extension data

## VRM Structure Details

See [references/vrm-structure.md](references/vrm-structure.md) for complete VRM JSON structure documentation.

## Common Analysis Tasks

### Check VRM version and basic info
```bash
python scripts/extract_vrm_json.py avatar.vrm --summary
```

### List all humanoid bones
```bash
python scripts/extract_vrm_json.py avatar.vrm --section humanoid
```

### Get expression/blendshape names
```bash
python scripts/extract_vrm_json.py avatar.vrm --section blendshape  # VRM 0.x
python scripts/extract_vrm_json.py avatar.vrm --section expressions  # VRM 1.0
```

### Check springbone configuration
```bash
python scripts/extract_vrm_json.py avatar.vrm --section springbone
```

### Get MToon material settings
```bash
python scripts/extract_vrm_json.py avatar.vrm --section mtoon
```
