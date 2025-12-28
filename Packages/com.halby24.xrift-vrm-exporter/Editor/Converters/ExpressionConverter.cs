// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-FileCopyrightText: 2024-present hkrn (original ndmf-vrm-exporter)
// SPDX-License-Identifier: MPL-2.0
//
// This file contains code derived from ndmf-vrm-exporter
// https://github.com/hkrn/ndmf-vrm-exporter

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEditor;
using UnityEngine;
using XRift.VrmExporter.Components;
using XRift.VrmExporter.VRM.Gltf;
using VrmCore = XRift.VrmExporter.VRM.Core;

#if XRIFT_HAS_VRCHAT_SDK
using VRC.SDK3.Avatars.Components;
using VRC_AvatarDescriptor = VRC.SDKBase.VRC_AvatarDescriptor;
#endif

namespace XRift.VrmExporter.Converters
{
    /// <summary>
    /// VRM表情（Expression）変換
    /// </summary>
    internal sealed class ExpressionConverter
    {
        private readonly GameObject _gameObject;
        private readonly ImmutableDictionary<Transform, ObjectID> _transformNodeIDs;
        private readonly ImmutableDictionary<string, (ObjectID, int)> _allMorphTargets;

        public ExpressionConverter(
            GameObject gameObject,
            IDictionary<Transform, ObjectID> transformNodeIDs,
            IDictionary<string, (ObjectID, int)> allMorphTargets)
        {
            _gameObject = gameObject;
            _transformNodeIDs = ImmutableDictionary.CreateRange(transformNodeIDs);
            _allMorphTargets = ImmutableDictionary.CreateRange(allMorphTargets);
        }

        /// <summary>
        /// 表情をエクスポート
        /// </summary>
        public void ExportExpression(ref VrmCore.Core core, IExpressionSettings? settings = null)
        {
#if XRIFT_HAS_VRCHAT_SDK
            var avatarDescriptor = _gameObject.GetComponent<VRCAvatarDescriptor>();
            if (avatarDescriptor != null)
            {
                core.Expressions = new VrmCore.Expressions
                {
                    Preset =
                    {
                        Aa = ExportExpressionViseme(avatarDescriptor, VRC_AvatarDescriptor.Viseme.aa),
                        Ih = ExportExpressionViseme(avatarDescriptor, VRC_AvatarDescriptor.Viseme.ih),
                        Ou = ExportExpressionViseme(avatarDescriptor, VRC_AvatarDescriptor.Viseme.ou),
                        Ee = ExportExpressionViseme(avatarDescriptor, VRC_AvatarDescriptor.Viseme.E),
                        Oh = ExportExpressionViseme(avatarDescriptor, VRC_AvatarDescriptor.Viseme.oh),
                        Blink = ExportExpressionEyelids(avatarDescriptor, 0),
                        LookUp = ExportExpressionEyelids(avatarDescriptor, 1),
                        LookDown = ExportExpressionEyelids(avatarDescriptor, 2),
                    }
                };
            }
            else
            {
                core.Expressions = new VrmCore.Expressions();
            }
#else
            core.Expressions = new VrmCore.Expressions();
#endif

            // 設定からの表情エクスポート
            if (settings != null)
            {
                ExportFromSettings(ref core, settings);
            }
        }

        private void ExportFromSettings(ref VrmCore.Core core, IExpressionSettings settings)
        {
            if (settings.HappyExpression?.IsValid == true)
            {
                core.Expressions.Preset.Happy = ExportExpressionItem(settings.HappyExpression);
            }
            else
            {
                Debug.LogWarning("Preset Happy will be skipped due to expression is not set properly");
            }

            if (settings.AngryExpression?.IsValid == true)
            {
                core.Expressions.Preset.Angry = ExportExpressionItem(settings.AngryExpression);
            }
            else
            {
                Debug.LogWarning("Preset Angry will be skipped due to expression is not set properly");
            }

            if (settings.SadExpression?.IsValid == true)
            {
                core.Expressions.Preset.Sad = ExportExpressionItem(settings.SadExpression);
            }
            else
            {
                Debug.LogWarning("Preset Sad will be skipped due to expression is not set properly");
            }

            if (settings.RelaxedExpression?.IsValid == true)
            {
                core.Expressions.Preset.Relaxed = ExportExpressionItem(settings.RelaxedExpression);
            }
            else
            {
                Debug.LogWarning("Preset Relaxed will be skipped due to expression is not set properly");
            }

            if (settings.SurprisedExpression?.IsValid == true)
            {
                core.Expressions.Preset.Surprised = ExportExpressionItem(settings.SurprisedExpression);
            }
            else
            {
                Debug.LogWarning("Preset Surprised will be skipped due to expression is not set properly");
            }

            // カスタム表情
            var offset = 0;
            foreach (var property in settings.CustomExpressions)
            {
                var index = offset++;
                if (!property.IsValid)
                {
                    Debug.LogWarning(
                        $"Custom expression offset with {index} will be skipped due to expression is not set properly");
                    continue;
                }

                var item = ExportExpressionItem(property);
                if (item == null)
                    continue;
                core.Expressions.Custom ??= new Dictionary<UnicodeString, VrmCore.ExpressionItem>();
                core.Expressions.Custom.Add(new UnicodeString(property.CanonicalExpressionName), item);
            }
        }

#if XRIFT_HAS_VRCHAT_SDK
        private VrmCore.ExpressionItem? ExportExpressionViseme(
            VRCAvatarDescriptor descriptor,
            VRC_AvatarDescriptor.Viseme viseme)
        {
            if (descriptor.lipSync != VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
            {
                return null;
            }

            var nodeID = FindTransformNodeID(descriptor.VisemeSkinnedMesh.transform);
            if (!nodeID.HasValue)
            {
                Debug.LogWarning($"Viseme skinned mesh {descriptor.VisemeSkinnedMesh} not found due to inactive");
                return null;
            }

            var blendShapeName = descriptor.VisemeBlendShapes[(int)viseme];
            var offset = descriptor.VisemeSkinnedMesh.sharedMesh.GetBlendShapeIndex(blendShapeName);
            return new VrmCore.ExpressionItem
            {
                MorphTargetBinds = new List<VrmCore.MorphTargetBind>
                {
                    new()
                    {
                        Node = nodeID.Value,
                        Index = new ObjectID((uint)offset),
                        Weight = 1.0f,
                    }
                }
            };
        }

        private VrmCore.ExpressionItem? ExportExpressionEyelids(VRCAvatarDescriptor descriptor, int offset)
        {
            if (!descriptor.enableEyeLook)
            {
                return null;
            }

            var settings = descriptor.customEyeLookSettings;
            if (settings.eyelidType != VRCAvatarDescriptor.EyelidType.Blendshapes)
            {
                return null;
            }

            var nodeID = FindTransformNodeID(descriptor.VisemeSkinnedMesh.transform);
            if (!nodeID.HasValue)
            {
                Debug.LogWarning($"Viseme skinned mesh {descriptor.VisemeSkinnedMesh} not found due to inactive");
                return null;
            }

            var blendShapeIndex = settings.eyelidsBlendshapes[offset];
            if (blendShapeIndex == -1)
                return null;

            return new VrmCore.ExpressionItem
            {
                MorphTargetBinds = new List<VrmCore.MorphTargetBind>
                {
                    new()
                    {
                        Node = nodeID.Value,
                        Index = new ObjectID((uint)blendShapeIndex),
                        Weight = 1.0f,
                    }
                }
            };
        }
#endif

        private VrmCore.ExpressionItem? ExportExpressionItem(VrmExpressionProperty property)
        {
            switch (property.baseType)
            {
                case VrmExpressionProperty.BaseType.BlendShape:
                {
                    if (_allMorphTargets.TryGetValue(property.blendShapeName!, out var value))
                        return new VrmCore.ExpressionItem
                        {
                            OverrideBlink = property.overrideBlink,
                            OverrideLookAt = property.overrideLookAt,
                            OverrideMouth = property.overrideMouth,
                            IsBinary = property.isBinary,
                            MorphTargetBinds = new List<VrmCore.MorphTargetBind>
                            {
                                new()
                                {
                                    Node = value.Item1,
                                    Index = new ObjectID((uint)value.Item2),
                                    Weight = 1.0f,
                                }
                            }
                        };
                    Debug.LogWarning($"BlendShape {property.blendShapeName} is not found");
                    break;
                }
                case VrmExpressionProperty.BaseType.AnimationClip:
                {
                    var morphTargetBinds = new List<VrmCore.MorphTargetBind>();
                    foreach (var binding in AnimationUtility.GetCurveBindings(property.blendShapeAnimationClip))
                    {
                        if (!binding.propertyName.StartsWith(VrmExpressionProperty.BlendShapeNamePrefix,
                                StringComparison.Ordinal))
                            continue;
                        var blendShapeName =
                            binding.propertyName[VrmExpressionProperty.BlendShapeNamePrefix.Length..];
                        var curve = AnimationUtility.GetEditorCurve(property.blendShapeAnimationClip, binding);
                        foreach (var keyframe in curve.keys)
                        {
                            if (keyframe.time > 0.0f || Mathf.Approximately(keyframe.value, 0.0f))
                                continue;
                            if (!_allMorphTargets.TryGetValue(blendShapeName, out var value))
                            {
                                Debug.LogWarning($"BlendShape {blendShapeName} is not found");
                                continue;
                            }

                            morphTargetBinds.Add(new VrmCore.MorphTargetBind
                            {
                                Node = value.Item1,
                                Index = new ObjectID((uint)value.Item2),
                                Weight = keyframe.value * 0.01f,
                            });
                        }
                    }

                    if (morphTargetBinds.Count > 0)
                    {
                        return new VrmCore.ExpressionItem
                        {
                            OverrideBlink = property.overrideBlink,
                            OverrideLookAt = property.overrideLookAt,
                            OverrideMouth = property.overrideMouth,
                            IsBinary = property.isBinary,
                            MorphTargetBinds = morphTargetBinds
                        };
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return null;
        }

        private ObjectID? FindTransformNodeID(Transform transform)
        {
            return transform && _transformNodeIDs.TryGetValue(transform, out var nodeID) ? nodeID : null;
        }
    }

    /// <summary>
    /// 表情設定インターフェース
    /// </summary>
    public interface IExpressionSettings
    {
        VrmExpressionProperty? HappyExpression { get; }
        VrmExpressionProperty? AngryExpression { get; }
        VrmExpressionProperty? SadExpression { get; }
        VrmExpressionProperty? RelaxedExpression { get; }
        VrmExpressionProperty? SurprisedExpression { get; }
        IEnumerable<VrmExpressionProperty> CustomExpressions { get; }
    }
}
