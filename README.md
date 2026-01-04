# XRift VRM Exporter

![Version](https://img.shields.io/github/v/release/WebXR-JP/XRiftVRMExporter)
![Unity](https://img.shields.io/badge/Unity-2022.3%2B-blue)
![License](https://img.shields.io/badge/license-MPL--2.0-blue)

VRM 1.0 exporter for XRift platform with NDMF integration.

## Overview

XRift VRM Exporter is a Unity package that enables exporting VRChat avatars to VRM 1.0 format for the XRift platform. It integrates with the Non-Destructive Modular Framework (NDMF) to provide a seamless, non-destructive build process.

### Key Features

- **VRChat to VRM 1.0 Export**: Full support for exporting VRChat avatars to VRM 1.0
- **PhysBone Conversion**: Automatic conversion of VRChat PhysBones to VRM SpringBones
- **Material Conversion**: lilToon to MToon conversion with texture baking
- **NDMF Integration**: Non-destructive workflow preserving your original avatar
- **Expression Support**: VRChat expression parameter mapping to VRM expressions

## Installation

### Via VCC (Recommended)

1. Open VRChat Creator Companion (VCC)
2. Navigate to **Settings** > **Packages** > **Add Repository**
3. Add this repository URL:
   ```
   https://webxr-jp.github.io/XRiftVRMExporter/vpm.json
   ```
4. Open your Unity project in VCC
5. Find **XRift VRM Exporter** and click **Install**

### Manual Installation

1. Download the latest release from [Releases](https://github.com/WebXR-JP/XRiftVRMExporter/releases)
2. Extract the ZIP file
3. Copy the `com.halby24.xrift-vrm-exporter` folder to your project's `Packages` directory

## Requirements

- **Unity**: 2022.3 or later
- **NDMF**: 1.8.0 or later
- **UniVRM**: 0.131.0 or later
- **VRChat SDK**: 3.5.0 or later (for VRChat avatar export)

## Quick Start

1. Add the `XRiftVrmDescriptor` component to your avatar root GameObject
2. Fill in the VRM metadata (name, author, license, etc.)
3. Configure expression mappings (optional)
4. Use NDMF's Manual Bake Avatar to export

For detailed usage instructions, see the [package documentation](./Packages/com.halby24.xrift-vrm-exporter/README.md).

## Documentation

- [Package README](./Packages/com.halby24.xrift-vrm-exporter/README.md) - User documentation
- [CHANGELOG](./Packages/com.halby24.xrift-vrm-exporter/CHANGELOG.md) - Version history
- [UniVRM Integration Plan](./Packages/com.halby24.xrift-vrm-exporter/Documentation~/UniVRM-Integration-Plan.md) - Technical details
- [Development Guide](./CLAUDE.md) - Development documentation

## Development

This project is developed with assistance from Claude AI. The development context and guidelines are maintained in [CLAUDE.md](./CLAUDE.md).

### Building from Source

1. Clone this repository:
   ```bash
   git clone https://github.com/WebXR-JP/XRiftVRMExporter.git
   ```

2. The package is located in `Packages/com.halby24.xrift-vrm-exporter`

3. You can symlink this folder to your Unity project's `Packages` directory for development

### Release Process

Releases are automated via GitHub Actions:

1. Update version in `package.json`
2. Update `CHANGELOG.md` with new version
3. Commit changes
4. Create and push a version tag:
   ```bash
   git tag v0.1.0
   git push origin v0.1.0
   ```
5. GitHub Actions will automatically:
   - Create a release with ZIP artifact
   - Update the VPM repository
   - Deploy to GitHub Pages

## License

This project is licensed under the MPL-2.0 License - see the [LICENSE](LICENSE) file for details.

## Credits

- Based on [ndmf-vrm-exporter](https://github.com/hkrn/ndmf-vrm-exporter) by hkrn
- Uses [UniVRM](https://github.com/vrm-c/UniVRM) for VRM 1.0 export
- Integrates with [NDMF](https://github.com/bdunderscore/ndmf) framework
- lilToon material conversion support

## Contributing

Contributions are welcome! Please feel free to:

- Report bugs via [Issues](https://github.com/WebXR-JP/XRiftVRMExporter/issues)
- Submit feature requests
- Create pull requests

## Support

- **GitHub Issues**: [Report bugs or request features](https://github.com/WebXR-JP/XRiftVRMExporter/issues)
- **Repository**: [WebXR-JP/XRiftVRMExporter](https://github.com/WebXR-JP/XRiftVRMExporter)

## Related Projects

- [XRift Platform](https://xrift.example.com) - WebXR platform for VRM avatars
- [UniVRM](https://github.com/vrm-c/UniVRM) - VRM implementation for Unity
- [NDMF](https://github.com/bdunderscore/ndmf) - Non-Destructive Modular Framework
