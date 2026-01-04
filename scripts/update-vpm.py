#!/usr/bin/env python3
"""
VPM Repository Update Script

Updates vpm.json with new package version information.
Usage: python update-vpm.py <version>
Example: python update-vpm.py 0.1.0
"""

import json
import sys
import os
from pathlib import Path

def main():
    if len(sys.argv) < 2:
        print("Error: Version argument required")
        print("Usage: python update-vpm.py <version>")
        sys.exit(1)

    version = sys.argv[1]

    # Paths
    script_dir = Path(__file__).parent
    root_dir = script_dir.parent
    package_json_path = root_dir / "Packages" / "com.halby24.xrift-vrm-exporter" / "package.json"
    vpm_repo_dir = root_dir / "vpm-repo"
    vpm_json_path = vpm_repo_dir / "vpm.json"

    # Repository configuration
    repo_url = "https://webxr-jp.github.io/XRiftVRMExporter/vpm.json"
    release_url = f"https://github.com/WebXR-JP/XRiftVRMExporter/releases/download/v{version}/com.halby24.xrift-vrm-exporter-{version}.zip"

    print(f"Updating VPM repository for version {version}...")

    # Read package.json
    if not package_json_path.exists():
        print(f"Error: package.json not found at {package_json_path}")
        sys.exit(1)

    with open(package_json_path, 'r', encoding='utf-8') as f:
        package_info = json.load(f)

    # Update version-specific URLs in package info
    package_info["version"] = version
    package_info["url"] = release_url
    package_info["repo"] = repo_url

    # Load or create vpm.json
    if vpm_json_path.exists():
        with open(vpm_json_path, 'r', encoding='utf-8') as f:
            vpm_data = json.load(f)
        print(f"Loaded existing vpm.json")
    else:
        vpm_data = {
            "name": "XRift VRM Exporter VPM Repository",
            "author": "halby24",
            "url": repo_url,
            "id": "com.halby24.vpm-repos",
            "packages": {}
        }
        print("Created new vpm.json structure")

    # Add or update package
    package_name = package_info["name"]

    if package_name not in vpm_data["packages"]:
        vpm_data["packages"][package_name] = {"versions": {}}
        print(f"Added new package: {package_name}")

    # Add version entry
    vpm_data["packages"][package_name]["versions"][version] = package_info
    print(f"Added version {version} to package {package_name}")

    # Create directory if needed
    vpm_repo_dir.mkdir(parents=True, exist_ok=True)

    # Write vpm.json with pretty formatting
    with open(vpm_json_path, 'w', encoding='utf-8') as f:
        json.dump(vpm_data, f, indent=2, ensure_ascii=False)

    print(f"Successfully updated {vpm_json_path}")
    print(f"Package URL: {release_url}")
    print(f"Repository URL: {repo_url}")

    # List all versions
    versions = list(vpm_data["packages"][package_name]["versions"].keys())
    versions.sort(key=lambda v: [int(x) for x in v.split('.')])
    print(f"\nAvailable versions: {', '.join(versions)}")

if __name__ == "__main__":
    main()
