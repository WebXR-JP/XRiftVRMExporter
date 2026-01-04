# XRift VRM Exporter

![Version](https://img.shields.io/github/v/release/WebXR-JP/XRiftVRMExporter)
![Unity](https://img.shields.io/badge/Unity-2022.3%2B-blue)
![License](https://img.shields.io/badge/license-MPL--2.0-blue)

VRM 1.0 exporter for XRift platform with NDMF integration.

## Features

- **VRChat to VRM 1.0 Export**: Export VRChat avatars to VRM 1.0 format
- **PhysBone to SpringBone Conversion**: Automatically converts VRChat PhysBones to VRM SpringBones
- **lilToon to MToon Material Conversion**: Converts lilToon materials to MToon with texture baking
- **NDMF Platform Integration**: Seamlessly integrates with the Non-Destructive Modular Framework
- **Non-Destructive Build Process**: Preserves your original avatar during export
- **Expression Support**: Converts VRChat expression parameters to VRM expressions

## Installation

### Via VCC (VRChat Creator Companion)

1. Open VCC
2. Go to **Settings** > **Packages** > **Add Repository**
3. Add this URL: `https://webxr-jp.github.io/XRiftVRMExporter/vpm.json`
4. Search for **XRift VRM Exporter** in your project
5. Click **Install**

### Manual Installation

1. Download the latest `.zip` from [Releases](https://github.com/WebXR-JP/XRiftVRMExporter/releases)
2. Extract the package to your project's `Packages` folder
3. Unity will automatically import the package

## Requirements

- **Unity**: 2022.3 or later
- **NDMF**: 1.8.0 or later
- **UniVRM**: 0.131.0 or later
- **VRChat SDK**: 3.5.0 or later (for VRChat avatar export)

## Usage

### Basic Export

1. **Add XRiftVrmDescriptor Component**
   - Select your avatar root GameObject
   - Add Component > XRift > XRiftVrmDescriptor

2. **Configure VRM Metadata**
   - Fill in required fields:
     - Avatar Name
     - Author Name
     - Version
     - License Information
   - Set thumbnail image (optional)

3. **Configure Expression Mapping**
   - Map VRChat expression parameters to VRM expressions
   - Supported expressions: Happy, Angry, Sad, Relaxed, Surprised

4. **Export via NDMF**
   - Open **Tools** > **NDMF** > **Manual Bake Avatar**
   - Select your avatar
   - Choose export location
   - Click **Build**

### Advanced Configuration

#### Material Conversion

The exporter automatically handles material conversion:
- **lilToon**: Converts to MToon with texture baking for color/shade adjustments
- **Standard**: Converts to MToon with basic properties
- **Unsupported Shaders**: Exports as-is (may need manual adjustment)

#### PhysBone Conversion

PhysBone parameters are automatically mapped to SpringBone:
- Pull/Spring/Stiffness → Stiffness
- Gravity → Gravity Direction and Power
- Colliders → SpringBone Colliders

## Documentation

For detailed integration information and development notes, see:
- [UniVRM Integration Plan](./Documentation~/UniVRM-Integration-Plan.md)
- [Development Guide](../../CLAUDE.md)

## Troubleshooting

### Common Issues

**Build fails with "NDMF not found"**
- Install NDMF 1.8.0 or later via VCC

**Materials appear incorrect**
- Ensure lilToon is installed if using lilToon shaders
- Check material conversion settings in XRiftVrmDescriptor

**PhysBones not converting**
- Verify PhysBone components are on child objects
- Check console for conversion warnings

## License

This project is licensed under the MPL-2.0 License - see the [LICENSE](LICENSE) file for details.

## Credits

- Based on [ndmf-vrm-exporter](https://github.com/hkrn/ndmf-vrm-exporter) by hkrn
- Uses [UniVRM](https://github.com/vrm-c/UniVRM) for VRM 1.0 export
- Integrates with [NDMF](https://github.com/bdunderscore/ndmf) framework

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## Support

- **Issues**: [GitHub Issues](https://github.com/WebXR-JP/XRiftVRMExporter/issues)
- **Repository**: [GitHub](https://github.com/WebXR-JP/XRiftVRMExporter)
