#!/usr/bin/env python3
"""Extract and analyze JSON data from VRM files.

VRM files are glTF 2.0 binary containers with embedded JSON metadata.
This script extracts specific sections and provides analysis.

Usage:
    python extract_vrm_json.py <vrm_file> [options]

Options:
    --section <name>    Extract specific section (meta, materials, nodes, etc.)
    --summary           Show summary analysis instead of raw JSON
    --output <file>     Save output to file instead of stdout
    --all               Extract all sections
"""

import argparse
import json
import struct
import sys
from pathlib import Path
from typing import Any, Optional


def read_vrm_json(vrm_path: str) -> dict:
    """Extract JSON chunk from VRM file."""
    with open(vrm_path, 'rb') as f:
        # Read glTF header
        magic = f.read(4)
        if magic != b'glTF':
            raise ValueError(f"Not a valid glTF/VRM file: magic={magic}")

        version = struct.unpack('<I', f.read(4))[0]
        total_length = struct.unpack('<I', f.read(4))[0]

        # Read first chunk (JSON)
        chunk_length = struct.unpack('<I', f.read(4))[0]
        chunk_type = f.read(4)

        if chunk_type != b'JSON':
            raise ValueError(f"First chunk is not JSON: type={chunk_type}")

        json_data = f.read(chunk_length).decode('utf-8')
        return json.loads(json_data)


def get_vrm_version(data: dict) -> str:
    """Detect VRM version (0.x or 1.0)."""
    if 'extensions' in data:
        if 'VRM' in data['extensions']:
            return '0.x'
        elif 'VRMC_vrm' in data['extensions']:
            return '1.0'
    return 'unknown'


def extract_section(data: dict, section: str) -> Optional[dict]:
    """Extract a specific section from VRM data."""
    vrm_version = get_vrm_version(data)

    # Section mappings for different VRM versions
    section_mappings = {
        '0.x': {
            'meta': lambda d: d.get('extensions', {}).get('VRM', {}).get('meta'),
            'humanoid': lambda d: d.get('extensions', {}).get('VRM', {}).get('humanoid'),
            'materials': lambda d: d.get('materials'),
            'mtoon': lambda d: d.get('extensions', {}).get('VRM', {}).get('materialProperties'),
            'nodes': lambda d: d.get('nodes'),
            'meshes': lambda d: d.get('meshes'),
            'skins': lambda d: d.get('skins'),
            'textures': lambda d: d.get('textures'),
            'images': lambda d: d.get('images'),
            'blendshape': lambda d: d.get('extensions', {}).get('VRM', {}).get('blendShapeMaster'),
            'firstperson': lambda d: d.get('extensions', {}).get('VRM', {}).get('firstPerson'),
            'lookat': lambda d: d.get('extensions', {}).get('VRM', {}).get('lookAt'),
            'springbone': lambda d: d.get('extensions', {}).get('VRM', {}).get('secondaryAnimation'),
            'vrm': lambda d: d.get('extensions', {}).get('VRM'),
        },
        '1.0': {
            'meta': lambda d: d.get('extensions', {}).get('VRMC_vrm', {}).get('meta'),
            'humanoid': lambda d: d.get('extensions', {}).get('VRMC_vrm', {}).get('humanoid'),
            'materials': lambda d: d.get('materials'),
            'mtoon': lambda d: [
                {'name': m.get('name'), **m.get('extensions', {}).get('VRMC_materials_mtoon', {})}
                for m in d.get('materials', [])
                if m.get('extensions', {}).get('VRMC_materials_mtoon')
            ] or None,
            'nodes': lambda d: d.get('nodes'),
            'meshes': lambda d: d.get('meshes'),
            'skins': lambda d: d.get('skins'),
            'textures': lambda d: d.get('textures'),
            'images': lambda d: d.get('images'),
            'expressions': lambda d: d.get('extensions', {}).get('VRMC_vrm', {}).get('expressions'),
            'lookat': lambda d: d.get('extensions', {}).get('VRMC_vrm', {}).get('lookAt'),
            'springbone': lambda d: d.get('extensions', {}).get('VRMC_springBone'),
            'vrm': lambda d: d.get('extensions', {}).get('VRMC_vrm'),
        }
    }

    mappings = section_mappings.get(vrm_version, section_mappings['0.x'])
    extractor = mappings.get(section.lower())

    if extractor:
        return extractor(data)
    return None


def analyze_vrm(data: dict) -> dict:
    """Generate summary analysis of VRM file."""
    vrm_version = get_vrm_version(data)

    analysis = {
        'vrm_version': vrm_version,
        'gltf_version': data.get('asset', {}).get('version'),
        'generator': data.get('asset', {}).get('generator'),
    }

    # Extract meta info
    meta = extract_section(data, 'meta')
    if meta:
        analysis['meta'] = {
            'title': meta.get('title') or meta.get('name'),
            'author': meta.get('author') or meta.get('authors', []),
            'version': meta.get('version'),
            'license': meta.get('licenseName') or meta.get('licenseUrl'),
        }

    # Count resources
    analysis['counts'] = {
        'nodes': len(data.get('nodes', [])),
        'meshes': len(data.get('meshes', [])),
        'materials': len(data.get('materials', [])),
        'textures': len(data.get('textures', [])),
        'images': len(data.get('images', [])),
        'skins': len(data.get('skins', [])),
        'animations': len(data.get('animations', [])),
    }

    # Humanoid bone info
    humanoid = extract_section(data, 'humanoid')
    if humanoid:
        if vrm_version == '0.x':
            bones = humanoid.get('humanBones', [])
            analysis['humanoid'] = {
                'bone_count': len(bones),
                'bones': [b.get('bone') for b in bones]
            }
        else:
            bones = humanoid.get('humanBones', {})
            analysis['humanoid'] = {
                'bone_count': len(bones),
                'bones': list(bones.keys())
            }

    # BlendShape/Expression info
    if vrm_version == '0.x':
        blendshape = extract_section(data, 'blendshape')
        if blendshape:
            groups = blendshape.get('blendShapeGroups', [])
            analysis['blendshapes'] = {
                'count': len(groups),
                'names': [g.get('name') for g in groups]
            }
    else:
        expressions = extract_section(data, 'expressions')
        if expressions:
            preset = expressions.get('preset', {})
            custom = expressions.get('custom', {})
            analysis['expressions'] = {
                'preset_count': len(preset),
                'custom_count': len(custom),
                'presets': list(preset.keys()),
                'customs': list(custom.keys())
            }

    # SpringBone info
    springbone = extract_section(data, 'springbone')
    if springbone:
        if vrm_version == '0.x':
            bone_groups = springbone.get('boneGroups', [])
            colliders = springbone.get('colliderGroups', [])
            analysis['springbone'] = {
                'bone_groups': len(bone_groups),
                'collider_groups': len(colliders)
            }
        else:
            springs = springbone.get('springs', [])
            colliders = springbone.get('colliders', [])
            analysis['springbone'] = {
                'springs': len(springs),
                'colliders': len(colliders)
            }

    # MToon material info
    mtoon = extract_section(data, 'mtoon')
    if mtoon:
        if vrm_version == '0.x':
            shaders = [m.get('shader', 'Unknown') for m in mtoon]
            analysis['mtoon'] = {
                'count': len(mtoon),
                'names': [m.get('name') for m in mtoon],
                'shaders': list(set(shaders))
            }
        else:
            # VRM 1.0 stores mtoon per-material in extensions
            spec_versions = list(set(m.get('specVersion') for m in mtoon if m.get('specVersion')))
            analysis['mtoon'] = {
                'count': len(mtoon),
                'names': [m.get('name') for m in mtoon],
                'spec_version': spec_versions[0] if spec_versions else None
            }

    return analysis


def main():
    parser = argparse.ArgumentParser(
        description='Extract and analyze VRM JSON data',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__
    )
    parser.add_argument('vrm_file', help='Path to VRM file')
    parser.add_argument('--section', '-s',
                        choices=['meta', 'humanoid', 'materials', 'mtoon', 'nodes', 'meshes',
                                'skins', 'textures', 'images', 'blendshape', 'expressions',
                                'firstperson', 'lookat', 'springbone', 'vrm'],
                        help='Extract specific section')
    parser.add_argument('--summary', action='store_true',
                        help='Show summary analysis')
    parser.add_argument('--output', '-o', help='Output file path')
    parser.add_argument('--all', action='store_true',
                        help='Extract entire JSON')
    parser.add_argument('--indent', type=int, default=2,
                        help='JSON indentation (default: 2)')

    args = parser.parse_args()

    try:
        data = read_vrm_json(args.vrm_file)

        if args.summary:
            result = analyze_vrm(data)
        elif args.section:
            result = extract_section(data, args.section)
            if result is None:
                print(f"Section '{args.section}' not found", file=sys.stderr)
                sys.exit(1)
        else:
            result = data

        output = json.dumps(result, ensure_ascii=False, indent=args.indent)

        if args.output:
            Path(args.output).write_text(output, encoding='utf-8')
            print(f"Output saved to: {args.output}")
        else:
            print(output)

    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)


if __name__ == '__main__':
    main()
