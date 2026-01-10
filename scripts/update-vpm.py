#!/usr/bin/env python3
"""
VPM Repository Update Script

Updates vpm.json with package version information.
Supports multiple packages: main package + UniVRM dependencies.

Usage:
  python update-vpm.py --main-version 0.1.0
  python update-vpm.py --main-version 0.1.0 --univrm-version 0.131.0 --univrm-path /path/to/UniVRM/Packages

Legacy usage (for backward compatibility):
  python update-vpm.py 0.1.0
"""

import argparse
import json
import sys
from pathlib import Path

# Constants
REPO_URL = "https://webxr-jp.github.io/XRiftVRMExporter/vpm.json"
GITHUB_RELEASES_BASE = "https://github.com/WebXR-JP/XRiftVRMExporter/releases/download"

# Package configuration
PACKAGE_CONFIG = {
    "main": {
        "package_name": "com.halby24.xrift-vrm-exporter",
        "local_path": "Packages/com.halby24.xrift-vrm-exporter",
        "zip_name_template": "com.halby24.xrift-vrm-exporter-{version}.zip"
    },
    "unigltf": {
        "package_name": "com.vrmc.gltf",
        "folder_name": "UniGLTF",
        "zip_name_template": "com.vrmc.gltf-{version}.zip"
    },
    "vrm10": {
        "package_name": "com.vrmc.vrm",
        "folder_name": "VRM10",
        "zip_name_template": "com.vrmc.vrm-{version}.zip"
    }
}


def parse_args():
    """Parse command line arguments with backward compatibility"""
    parser = argparse.ArgumentParser(description="Update VPM repository")
    parser.add_argument("legacy_version", nargs="?", help="Legacy: main package version (positional)")
    parser.add_argument("--main-version", help="Main package version")
    parser.add_argument("--univrm-version", help="UniVRM packages version")
    parser.add_argument("--univrm-path", help="Path to UniVRM Packages folder")
    parser.add_argument("--skip-univrm", action="store_true", help="Skip UniVRM packages update")

    args = parser.parse_args()

    # Handle legacy usage: python update-vpm.py 0.1.0
    if args.legacy_version and not args.main_version:
        args.main_version = args.legacy_version

    if not args.main_version:
        parser.error("Main version is required (--main-version or positional argument)")

    return args


def load_package_json(path: Path) -> dict:
    """Load and return package.json content"""
    with open(path, 'r', encoding='utf-8') as f:
        return json.load(f)


def create_vpm_entry(package_info: dict, version: str, download_url: str,
                     repo_url: str, extra_fields: dict = None) -> dict:
    """Create a VPM version entry from package.json"""
    entry = package_info.copy()
    entry["version"] = version
    entry["url"] = download_url
    entry["repo"] = repo_url

    if extra_fields:
        entry.update(extra_fields)

    return entry


def add_package_to_vpm(vpm_data: dict, package_name: str,
                       version: str, entry: dict) -> bool:
    """Add a package version entry to vpm.json data"""
    if package_name not in vpm_data["packages"]:
        vpm_data["packages"][package_name] = {"versions": {}}
        print(f"  Created new package entry: {package_name}")

    # Check if version already exists
    if version in vpm_data["packages"][package_name]["versions"]:
        print(f"  Version {version} already exists for {package_name}, skipping")
        return False

    vpm_data["packages"][package_name]["versions"][version] = entry
    print(f"  Added version {version} to {package_name}")
    return True


def process_main_package(vpm_data: dict, version: str, root_dir: Path):
    """Process the main xrift-vrm-exporter package"""
    print(f"\nProcessing main package version {version}...")

    config = PACKAGE_CONFIG["main"]
    package_json_path = root_dir / config["local_path"] / "package.json"

    if not package_json_path.exists():
        print(f"  Error: package.json not found at {package_json_path}")
        sys.exit(1)

    package_info = load_package_json(package_json_path)

    download_url = f"{GITHUB_RELEASES_BASE}/v{version}/{config['zip_name_template'].format(version=version)}"

    entry = create_vpm_entry(package_info, version, download_url, REPO_URL)
    add_package_to_vpm(vpm_data, config["package_name"], version, entry)


def process_univrm_packages(vpm_data: dict, univrm_version: str, univrm_path: Path, main_version: str):
    """Process UniVRM packages (com.vrmc.gltf and com.vrmc.vrm)"""
    print(f"\nProcessing UniVRM packages version {univrm_version}...")

    for key in ["unigltf", "vrm10"]:
        config = PACKAGE_CONFIG[key]
        package_json_path = univrm_path / config["folder_name"] / "package.json"

        if not package_json_path.exists():
            print(f"  Warning: {package_json_path} not found, skipping {config['package_name']}")
            continue

        package_info = load_package_json(package_json_path)

        # UniVRM packages are included in the main version's release
        download_url = f"{GITHUB_RELEASES_BASE}/v{main_version}/{config['zip_name_template'].format(version=univrm_version)}"

        # Additional fields for license attribution
        extra_fields = {
            "licensesUrl": "https://github.com/vrm-c/UniVRM/blob/master/LICENSE.txt"
        }

        entry = create_vpm_entry(package_info, univrm_version, download_url, REPO_URL, extra_fields)
        add_package_to_vpm(vpm_data, config["package_name"], univrm_version, entry)


def load_or_create_vpm_data(vpm_json_path: Path) -> dict:
    """Load existing vpm.json or create new structure"""
    if vpm_json_path.exists():
        with open(vpm_json_path, 'r', encoding='utf-8') as f:
            print(f"Loaded existing vpm.json from {vpm_json_path}")
            return json.load(f)
    else:
        print("Creating new vpm.json structure")
        return {
            "name": "XRift VRM Exporter VPM Repository",
            "author": "halby24",
            "url": REPO_URL,
            "id": "com.halby24.vpm-repos",
            "packages": {}
        }


def save_vpm_data(vpm_data: dict, vpm_json_path: Path):
    """Save vpm.json with pretty formatting"""
    vpm_json_path.parent.mkdir(parents=True, exist_ok=True)
    with open(vpm_json_path, 'w', encoding='utf-8') as f:
        json.dump(vpm_data, f, indent=2, ensure_ascii=False)
    print(f"\nSuccessfully saved {vpm_json_path}")


def print_summary(vpm_data: dict):
    """Print summary of all packages and versions"""
    print("\n=== VPM Repository Summary ===")
    for package_name, package_data in vpm_data["packages"].items():
        versions = list(package_data["versions"].keys())
        # Sort versions semantically
        try:
            versions.sort(key=lambda v: [int(x) for x in v.split('.')])
        except ValueError:
            versions.sort()
        print(f"  {package_name}: {', '.join(versions)}")


def main():
    args = parse_args()

    # Paths
    script_dir = Path(__file__).parent
    root_dir = script_dir.parent
    vpm_json_path = root_dir / "vpm-repo" / "vpm.json"

    print(f"VPM Repository Update")
    print(f"=====================")
    print(f"Main version: {args.main_version}")
    if args.univrm_version:
        print(f"UniVRM version: {args.univrm_version}")
    if args.univrm_path:
        print(f"UniVRM path: {args.univrm_path}")

    # Load or create vpm.json
    vpm_data = load_or_create_vpm_data(vpm_json_path)

    # Process main package
    process_main_package(vpm_data, args.main_version, root_dir)

    # Process UniVRM packages if specified and not skipped
    if args.univrm_version and args.univrm_path and not args.skip_univrm:
        univrm_path = Path(args.univrm_path)
        if not univrm_path.exists():
            print(f"\nError: UniVRM path does not exist: {univrm_path}")
            sys.exit(1)
        process_univrm_packages(vpm_data, args.univrm_version, univrm_path, args.main_version)
    elif args.skip_univrm:
        print("\nSkipping UniVRM packages (--skip-univrm flag)")

    # Save vpm.json
    save_vpm_data(vpm_data, vpm_json_path)

    # Print summary
    print_summary(vpm_data)

    print(f"\nRepository URL: {REPO_URL}")


if __name__ == "__main__":
    main()
