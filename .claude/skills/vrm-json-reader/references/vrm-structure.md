# VRM JSON Structure Reference

## File Format

VRM files use glTF 2.0 binary format (.glb):

```
[12-byte header]
  magic: "glTF" (4 bytes)
  version: 2 (uint32)
  length: total file size (uint32)

[JSON chunk]
  chunkLength: size (uint32)
  chunkType: "JSON" (4 bytes)
  chunkData: UTF-8 JSON string

[BIN chunk]
  chunkLength: size (uint32)
  chunkType: "BIN\0" (4 bytes)
  chunkData: binary buffer
```

## VRM 0.x Structure

```json
{
  "asset": {
    "generator": "UniVRM-x.xx",
    "version": "2.0"
  },
  "extensionsUsed": ["VRM"],
  "extensions": {
    "VRM": {
      "exporterVersion": "UniVRM-x.xx",
      "specVersion": "0.0",
      "meta": {
        "title": "Avatar Name",
        "version": "1.0",
        "author": "Author Name",
        "contactInformation": "contact@example.com",
        "reference": "https://example.com",
        "texture": 0,
        "allowedUserName": "Everyone",
        "violentUssageName": "Disallow",
        "sexualUssageName": "Disallow",
        "commercialUssageName": "Disallow",
        "otherPermissionUrl": "",
        "licenseName": "CC_BY_NC",
        "otherLicenseUrl": ""
      },
      "humanoid": {
        "humanBones": [
          {"bone": "hips", "node": 0, "useDefaultValues": true},
          {"bone": "spine", "node": 1, "useDefaultValues": true},
          {"bone": "chest", "node": 2, "useDefaultValues": true}
        ],
        "armStretch": 0.05,
        "legStretch": 0.05,
        "upperArmTwist": 0.5,
        "lowerArmTwist": 0.5,
        "upperLegTwist": 0.5,
        "lowerLegTwist": 0.5,
        "feetSpacing": 0,
        "hasTranslationDoF": false
      },
      "blendShapeMaster": {
        "blendShapeGroups": [
          {
            "name": "Blink",
            "presetName": "blink",
            "binds": [
              {"mesh": 0, "index": 0, "weight": 100}
            ],
            "materialValues": [],
            "isBinary": false
          }
        ]
      },
      "firstPerson": {
        "firstPersonBone": 10,
        "firstPersonBoneOffset": {"x": 0, "y": 0.06, "z": 0},
        "meshAnnotations": [
          {"mesh": 0, "firstPersonFlag": "Auto"}
        ],
        "lookAtTypeName": "Bone",
        "lookAtHorizontalInner": {
          "curve": [0, 0, 0, 1, 1, 1, 1, 0],
          "xRange": 90,
          "yRange": 10
        },
        "lookAtHorizontalOuter": {
          "curve": [0, 0, 0, 1, 1, 1, 1, 0],
          "xRange": 90,
          "yRange": 10
        },
        "lookAtVerticalDown": {
          "curve": [0, 0, 0, 1, 1, 1, 1, 0],
          "xRange": 90,
          "yRange": 10
        },
        "lookAtVerticalUp": {
          "curve": [0, 0, 0, 1, 1, 1, 1, 0],
          "xRange": 90,
          "yRange": 10
        }
      },
      "secondaryAnimation": {
        "boneGroups": [
          {
            "comment": "Hair",
            "stiffiness": 1.0,
            "gravityPower": 0,
            "gravityDir": {"x": 0, "y": -1, "z": 0},
            "dragForce": 0.4,
            "center": -1,
            "hitRadius": 0.02,
            "bones": [5, 6, 7],
            "colliderGroups": [0]
          }
        ],
        "colliderGroups": [
          {
            "node": 10,
            "colliders": [
              {"offset": {"x": 0, "y": 0, "z": 0}, "radius": 0.1}
            ]
          }
        ]
      },
      "materialProperties": [
        {
          "name": "Material",
          "shader": "VRM/MToon",
          "renderQueue": 2000,
          "floatProperties": {},
          "vectorProperties": {},
          "textureProperties": {},
          "keywordMap": {},
          "tagMap": {}
        }
      ]
    }
  },
  "nodes": [...],
  "meshes": [...],
  "materials": [...],
  "textures": [...],
  "images": [...],
  "skins": [...],
  "buffers": [...],
  "bufferViews": [...],
  "accessors": [...]
}
```

## VRM 1.0 Structure

```json
{
  "asset": {
    "generator": "UniVRM-x.xx",
    "version": "2.0"
  },
  "extensionsUsed": [
    "VRMC_vrm",
    "VRMC_springBone",
    "VRMC_materials_mtoon",
    "VRMC_node_constraint"
  ],
  "extensions": {
    "VRMC_vrm": {
      "specVersion": "1.0",
      "meta": {
        "name": "Avatar Name",
        "version": "1.0",
        "authors": ["Author Name"],
        "copyrightInformation": "Copyright info",
        "contactInformation": "contact@example.com",
        "references": ["https://example.com"],
        "thirdPartyLicenses": "",
        "thumbnailImage": 0,
        "licenseUrl": "https://vrm.dev/licenses/1.0/",
        "avatarPermission": "everyone",
        "allowExcessivelyViolentUsage": false,
        "allowExcessivelySexualUsage": false,
        "commercialUsage": "personalNonProfit",
        "allowPoliticalOrReligiousUsage": false,
        "allowAntisocialOrHateUsage": false,
        "creditNotation": "required",
        "allowRedistribution": false,
        "modification": "prohibited",
        "otherLicenseUrl": ""
      },
      "humanoid": {
        "humanBones": {
          "hips": {"node": 0},
          "spine": {"node": 1},
          "chest": {"node": 2},
          "upperChest": {"node": 3},
          "neck": {"node": 4},
          "head": {"node": 5},
          "leftEye": {"node": 6},
          "rightEye": {"node": 7},
          "leftUpperArm": {"node": 10},
          "leftLowerArm": {"node": 11},
          "leftHand": {"node": 12},
          "rightUpperArm": {"node": 20},
          "rightLowerArm": {"node": 21},
          "rightHand": {"node": 22},
          "leftUpperLeg": {"node": 30},
          "leftLowerLeg": {"node": 31},
          "leftFoot": {"node": 32},
          "rightUpperLeg": {"node": 40},
          "rightLowerLeg": {"node": 41},
          "rightFoot": {"node": 42}
        }
      },
      "expressions": {
        "preset": {
          "happy": {
            "morphTargetBinds": [
              {"node": 0, "index": 0, "weight": 1.0}
            ],
            "materialColorBinds": [],
            "textureTransformBinds": [],
            "isBinary": false,
            "overrideBlink": "none",
            "overrideLookAt": "none",
            "overrideMouth": "none"
          },
          "angry": {...},
          "sad": {...},
          "relaxed": {...},
          "surprised": {...},
          "blink": {...},
          "blinkLeft": {...},
          "blinkRight": {...},
          "lookUp": {...},
          "lookDown": {...},
          "lookLeft": {...},
          "lookRight": {...},
          "neutral": {...},
          "aa": {...},
          "ih": {...},
          "ou": {...},
          "ee": {...},
          "oh": {...}
        },
        "custom": {
          "customExpression": {...}
        }
      },
      "firstPerson": {
        "meshAnnotations": [
          {"node": 0, "type": "auto"}
        ]
      },
      "lookAt": {
        "offsetFromHeadBone": [0, 0.06, 0],
        "type": "bone",
        "rangeMapHorizontalInner": {
          "inputMaxValue": 90,
          "outputScale": 10
        },
        "rangeMapHorizontalOuter": {
          "inputMaxValue": 90,
          "outputScale": 10
        },
        "rangeMapVerticalDown": {
          "inputMaxValue": 90,
          "outputScale": 10
        },
        "rangeMapVerticalUp": {
          "inputMaxValue": 90,
          "outputScale": 10
        }
      }
    },
    "VRMC_springBone": {
      "specVersion": "1.0",
      "colliders": [
        {
          "node": 5,
          "shape": {
            "sphere": {
              "offset": [0, 0, 0],
              "radius": 0.1
            }
          }
        }
      ],
      "colliderGroups": [
        {
          "name": "HeadColliders",
          "colliders": [0]
        }
      ],
      "springs": [
        {
          "name": "Hair",
          "joints": [
            {
              "node": 10,
              "hitRadius": 0.02,
              "stiffness": 1.0,
              "gravityPower": 0,
              "gravityDir": [0, -1, 0],
              "dragForce": 0.4
            }
          ],
          "colliderGroups": [0]
        }
      ]
    },
    "VRMC_materials_mtoon": {
      "specVersion": "1.0",
      "materials": [
        {
          "shadeColorFactor": [0.9, 0.9, 0.9],
          "shadeMultiplyTexture": {"index": 0},
          "shadingShiftFactor": 0,
          "shadingToonyFactor": 0.9,
          "giEqualizationFactor": 0.9,
          "matcapFactor": [1, 1, 1],
          "parametricRimColorFactor": [0, 0, 0],
          "rimMultiplyTexture": {"index": 0},
          "rimLightingMixFactor": 1,
          "parametricRimFresnelPowerFactor": 5,
          "parametricRimLiftFactor": 0,
          "outlineWidthMode": "worldCoordinates",
          "outlineWidthFactor": 0.001,
          "outlineColorFactor": [0, 0, 0],
          "outlineLightingMixFactor": 1,
          "uvAnimationMaskTexture": {"index": 0},
          "uvAnimationScrollXSpeedFactor": 0,
          "uvAnimationScrollYSpeedFactor": 0,
          "uvAnimationRotationSpeedFactor": 0
        }
      ]
    }
  },
  "nodes": [...],
  "meshes": [...],
  "materials": [...],
  "textures": [...],
  "images": [...],
  "skins": [...],
  "buffers": [...],
  "bufferViews": [...],
  "accessors": [...]
}
```

## Humanoid Bone Names

### Required Bones
- hips, spine, head
- leftUpperArm, leftLowerArm, leftHand
- rightUpperArm, rightLowerArm, rightHand
- leftUpperLeg, leftLowerLeg, leftFoot
- rightUpperLeg, rightLowerLeg, rightFoot

### Optional Bones
- chest, upperChest, neck
- leftEye, rightEye, jaw
- leftShoulder, rightShoulder
- leftToes, rightToes
- leftThumbMetacarpal, leftThumbProximal, leftThumbDistal
- leftIndexProximal, leftIndexIntermediate, leftIndexDistal
- leftMiddleProximal, leftMiddleIntermediate, leftMiddleDistal
- leftRingProximal, leftRingIntermediate, leftRingDistal
- leftLittleProximal, leftLittleIntermediate, leftLittleDistal
- (same for right hand)

## Standard Expression Presets (VRM 1.0)

### Emotion
- happy, angry, sad, relaxed, surprised

### Blink
- blink, blinkLeft, blinkRight

### LookAt
- lookUp, lookDown, lookLeft, lookRight

### Mouth (Viseme)
- aa, ih, ou, ee, oh

### Other
- neutral
