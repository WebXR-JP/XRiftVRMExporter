# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Removed
- Runtime preview feature (incomplete/WIP)
  - Removed XRiftVrmRuntimePreview component
  - Removed runtime preview NDMF plugin and passes

## [0.1.0] - 2026-01-04

### Added
- Initial release of XRift VRM Exporter
- VRM 1.0 export functionality with UniVRM integration
- XRiftVrmDescriptor component for avatar metadata configuration
- PhysBone to SpringBone automatic conversion
  - Pull/Spring/Stiffness parameter mapping
  - Gravity direction and power mapping
  - Collider conversion support
- lilToon to MToon material conversion
  - Texture baking for color and shade adjustments
  - Shadow color baking improvements
  - Material property preservation
- NDMF platform integration
  - Non-destructive build process
  - XRiftVrmPlugin for build hooks
  - XRiftVrmPlatform for NDMF platform support
- Expression mapping support
  - VRChat expression parameters to VRM expressions
  - Supported expressions: Happy, Angry, Sad, Relaxed, Surprised
- Build UI with export path persistence
  - XRiftBuildUI for manual export
  - Export path saved to EditorPrefs

### Dependencies
- nadena.dev.ndmf >= 1.8.0
- com.vrmc.vrm 0.131.0
- com.vrmc.gltf 0.131.0
- com.unity.nuget.newtonsoft-json 3.2.1

### Technical Details
- Unity 2022.3+ support
- Assembly definitions for Editor and Runtime
- Material baking system with texture generation
- Asset path utilities for safe file operations

[Unreleased]: https://github.com/WebXR-JP/XRiftVRMExporter/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/WebXR-JP/XRiftVRMExporter/releases/tag/v0.1.0
