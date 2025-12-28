// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-FileCopyrightText: 2024-present hkrn (original ndmf-vrm-exporter)
// SPDX-License-Identifier: MPL-2.0
//
// This file contains code derived from ndmf-vrm-exporter
// https://github.com/hkrn/ndmf-vrm-exporter

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using XRift.VrmExporter.VRM.Gltf;
using VrmConstraint = XRift.VrmExporter.VRM.Constraint;

#if XRIFT_HAS_VRCHAT_SDK
using VRC.SDK3.Dynamics.Constraint.Components;
#endif

namespace XRift.VrmExporter.Converters
{
    /// <summary>
    /// Unity/VRChat Constraint → VRM NodeConstraint 変換
    /// </summary>
    internal sealed class ConstraintConverter
    {
        private readonly ImmutableDictionary<Transform, ObjectID> _transformNodeIDs;

        public ConstraintConverter(IDictionary<Transform, ObjectID> transformNodeIDs)
        {
            _transformNodeIDs = ImmutableDictionary.CreateRange(transformNodeIDs);
        }

        /// <summary>
        /// NodeConstraintをエクスポート
        /// </summary>
        public VrmConstraint.NodeConstraint? ExportNodeConstraint(Transform node, ObjectID immobileNodeID)
        {
            VrmConstraint.NodeConstraint? vrmNodeConstraint = null;
            var sourceID = ObjectID.Null;
            float weight;

#if XRIFT_HAS_VRCHAT_SDK
            if (node.TryGetComponent<VRCAimConstraint>(out var vrcAimConstraint))
            {
                vrmNodeConstraint = TryExportVrcAimConstraint(vrcAimConstraint, immobileNodeID, node.name);
                if (vrmNodeConstraint != null)
                    return vrmNodeConstraint;
            }
#endif

            if (node.TryGetComponent<AimConstraint>(out var aimConstraint) && aimConstraint.constraintActive)
            {
                vrmNodeConstraint = TryExportUnityAimConstraint(aimConstraint, immobileNodeID, node.name);
                if (vrmNodeConstraint != null)
                    return vrmNodeConstraint;
            }

#if XRIFT_HAS_VRCHAT_SDK
            if (node.TryGetComponent<VRCRotationConstraint>(out var vrcRotationConstraint))
            {
                vrmNodeConstraint = TryExportVrcRotationConstraint(vrcRotationConstraint, node.name);
                if (vrmNodeConstraint != null)
                    return vrmNodeConstraint;
            }
#endif

            if (node.TryGetComponent<RotationConstraint>(out var rotationConstraint) &&
                rotationConstraint.constraintActive)
            {
                vrmNodeConstraint = TryExportUnityRotationConstraint(rotationConstraint, node.name);
                if (vrmNodeConstraint != null)
                    return vrmNodeConstraint;
            }

#if XRIFT_HAS_VRCHAT_SDK
            // ソースなしのParentConstraintをRotationConstraint扱い
            if ((node.TryGetComponent<VRCParentConstraint>(out var vrcParentConstraint) &&
                 vrcParentConstraint.Sources.Count == 0) ||
                (node.TryGetComponent<ParentConstraint>(out var parentConstraint) &&
                 parentConstraint.sourceCount == 0))
            {
                return new VrmConstraint.NodeConstraint
                {
                    Constraint = new VrmConstraint.Constraint
                    {
                        Rotation = new VrmConstraint.RotationConstraint
                        {
                            Source = immobileNodeID,
                        }
                    }
                };
            }

            if (node.TryGetComponent<VRCConstraintBase>(out _))
            {
                Debug.LogWarning($"VRC constraint is not supported in {node.name}");
            }
#endif

            if (node.TryGetComponent<IConstraint>(out _))
            {
                Debug.LogWarning($"Constraint is not supported in {node.name}");
            }

            return vrmNodeConstraint;
        }

#if XRIFT_HAS_VRCHAT_SDK
        private VrmConstraint.NodeConstraint? TryExportVrcAimConstraint(
            VRCAimConstraint vrcAimConstraint,
            ObjectID immobileNodeID,
            string nodeName)
        {
            if (!vrcAimConstraint.IsActive)
            {
                Debug.Log($"VRCAimConstraint {nodeName} is not active");
                return null;
            }

            ObjectID sourceID;
            float weight;

            var numSources = vrcAimConstraint.Sources.Count;
            if (numSources >= 1)
            {
                if (numSources > 1)
                {
                    Debug.LogWarning($"Constraint with multiple sources is not supported in {nodeName}");
                }

                var constraintSource = vrcAimConstraint.Sources.First();
                var nodeID = FindTransformNodeID(constraintSource.SourceTransform);
                if (nodeID.HasValue)
                {
                    sourceID = nodeID.Value;
                }
                else
                {
                    Debug.LogWarning(
                        $"Constraint source {constraintSource.SourceTransform} not found due to inactive");
                    return null;
                }

                weight = vrcAimConstraint.GlobalWeight * constraintSource.Weight;
            }
            else
            {
                sourceID = immobileNodeID;
                weight = vrcAimConstraint.GlobalWeight;
            }

            if (sourceID.IsNull)
            {
                return null;
            }

            if (TryParseAimAxis(vrcAimConstraint.AimAxis, out var aimAxis))
            {
                return new VrmConstraint.NodeConstraint
                {
                    Constraint = new VrmConstraint.Constraint
                    {
                        Aim = new VrmConstraint.AimConstraint
                        {
                            AimAxis = aimAxis,
                            Source = sourceID,
                            Weight = weight,
                        }
                    }
                };
            }

            Debug.LogWarning($"Aim axis cannot be exported due to unsupported axis: {nodeName}");
            return null;
        }
#endif

        private VrmConstraint.NodeConstraint? TryExportUnityAimConstraint(
            AimConstraint aimConstraint,
            ObjectID immobileNodeID,
            string nodeName)
        {
            if (!aimConstraint.constraintActive)
            {
                Debug.Log($"AimConstraint {nodeName} is not active");
                return null;
            }

            ObjectID sourceID;
            float weight;

            var numSources = aimConstraint.sourceCount;
            if (numSources >= 1)
            {
                if (numSources > 1)
                {
                    Debug.LogWarning($"Constraint with multiple sources is not supported in {nodeName}");
                }

                var constraintSource = aimConstraint.GetSource(0);
                var nodeID = FindTransformNodeID(constraintSource.sourceTransform);
                if (nodeID.HasValue)
                {
                    sourceID = nodeID.Value;
                }
                else
                {
                    Debug.LogWarning(
                        $"Constraint source {constraintSource.sourceTransform} not found due to inactive");
                    return null;
                }

                weight = aimConstraint.weight * constraintSource.weight;
            }
            else
            {
                sourceID = immobileNodeID;
                weight = aimConstraint.weight;
            }

            if (sourceID.IsNull)
            {
                return null;
            }

            if (TryParseAimAxis(aimConstraint.aimVector, out var aimAxis))
            {
                return new VrmConstraint.NodeConstraint
                {
                    Constraint = new VrmConstraint.Constraint
                    {
                        Aim = new VrmConstraint.AimConstraint
                        {
                            AimAxis = aimAxis,
                            Source = sourceID,
                            Weight = weight,
                        }
                    }
                };
            }

            Debug.LogWarning($"Aim axis cannot be exported due to unsupported axis: {nodeName}");
            return null;
        }

#if XRIFT_HAS_VRCHAT_SDK
        private VrmConstraint.NodeConstraint? TryExportVrcRotationConstraint(
            VRCRotationConstraint vrcRotationConstraint,
            string nodeName)
        {
            if (!vrcRotationConstraint.IsActive)
            {
                Debug.Log($"VRCRotationConstraint {nodeName} is not active");
                return null;
            }

            ObjectID sourceID = ObjectID.Null;
            float weight;

            var numSources = vrcRotationConstraint.Sources.Count;
            if (numSources >= 1)
            {
                if (numSources > 1)
                {
                    Debug.LogWarning($"Constraint with multiple sources is not supported in {nodeName}");
                }

                var constraintSource = vrcRotationConstraint.Sources.First();
                var nodeID = FindTransformNodeID(constraintSource.SourceTransform);
                if (nodeID.HasValue)
                {
                    sourceID = nodeID.Value;
                }
                else
                {
                    Debug.LogWarning(
                        $"Constraint source {constraintSource.SourceTransform} not found due to inactive");
                    return null;
                }

                weight = vrcRotationConstraint.GlobalWeight * constraintSource.Weight;
            }
            else
            {
                weight = vrcRotationConstraint.GlobalWeight;
            }

            if (sourceID.IsNull)
            {
                Debug.LogWarning($"VRCRotationConstraint {nodeName} has no source");
                return null;
            }

            VrmConstraint.Constraint constraint;
            switch (vrcRotationConstraint.AffectsRotationX, vrcRotationConstraint.AffectsRotationY,
                vrcRotationConstraint.AffectsRotationZ)
            {
                case (true, true, true):
                {
                    constraint = new VrmConstraint.Constraint
                    {
                        Rotation = new VrmConstraint.RotationConstraint
                        {
                            Source = sourceID,
                            Weight = weight,
                        }
                    };
                    break;
                }
                case (true, false, false):
                case (false, true, false):
                case (false, false, true):
                {
                    var rollAxis = (vrcRotationConstraint.AffectsRotationX,
                            vrcRotationConstraint.AffectsRotationY,
                            vrcRotationConstraint.AffectsRotationZ) switch
                        {
                            (true, false, false) => "X",
                            (false, true, false) => "Y",
                            (false, false, true) => "Z",
                            _ => throw new System.ArgumentOutOfRangeException(),
                        };
                    constraint = new VrmConstraint.Constraint
                    {
                        Roll = new VrmConstraint.RollConstraint
                        {
                            RollAxis = rollAxis,
                            Source = sourceID,
                            Weight = weight,
                        }
                    };
                    break;
                }
                default:
                    Debug.LogWarning(
                        $"VRCRotationConstraint {nodeName} is not converted due to unsupported freeze axes pattern");
                    return null;
            }

            return new VrmConstraint.NodeConstraint
            {
                Constraint = constraint,
            };
        }
#endif

        private VrmConstraint.NodeConstraint? TryExportUnityRotationConstraint(
            RotationConstraint rotationConstraint,
            string nodeName)
        {
            if (!rotationConstraint.constraintActive)
            {
                Debug.Log($"RotationConstraint {nodeName} is not active");
                return null;
            }

            ObjectID sourceID = ObjectID.Null;
            float weight;

            var numSources = rotationConstraint.sourceCount;
            if (numSources >= 1)
            {
                if (numSources > 1)
                {
                    Debug.LogWarning($"Constraint with multiple sources is not supported in {nodeName}");
                }

                var constraintSource = rotationConstraint.GetSource(0);
                var nodeID = FindTransformNodeID(constraintSource.sourceTransform);
                if (nodeID.HasValue)
                {
                    sourceID = nodeID.Value;
                }
                else
                {
                    Debug.LogWarning(
                        $"Constraint source {constraintSource.sourceTransform} not found due to inactive");
                    return null;
                }

                weight = rotationConstraint.weight * constraintSource.weight;
            }
            else
            {
                weight = rotationConstraint.weight;
            }

            if (sourceID.IsNull)
            {
                Debug.LogWarning($"RotationConstraint {nodeName} has no source");
                return null;
            }

            VrmConstraint.Constraint constraint;
            switch (rotationConstraint.rotationAxis)
            {
                case Axis.X | Axis.Y | Axis.Z:
                {
                    constraint = new VrmConstraint.Constraint
                    {
                        Rotation = new VrmConstraint.RotationConstraint
                        {
                            Source = sourceID,
                            Weight = weight,
                        }
                    };
                    break;
                }
                case Axis.X:
                case Axis.Y:
                case Axis.Z:
                {
                    var rollAxis = rotationConstraint.rotationAxis switch
                    {
                        Axis.X => "X",
                        Axis.Y => "Y",
                        Axis.Z => "Z",
                        _ => throw new System.ArgumentOutOfRangeException(),
                    };
                    constraint = new VrmConstraint.Constraint
                    {
                        Roll = new VrmConstraint.RollConstraint
                        {
                            RollAxis = rollAxis,
                            Source = sourceID,
                            Weight = weight,
                        }
                    };
                    break;
                }
                case Axis.None:
                default:
                    Debug.LogWarning(
                        $"RotationConstraint {nodeName} is not converted due to unsupported freeze axes pattern");
                    return null;
            }

            return new VrmConstraint.NodeConstraint
            {
                Constraint = constraint,
            };
        }

        private static bool TryParseAimAxis(Vector3 axis, out string value)
        {
            if (axis == Vector3.left)
            {
                value = "PositiveX";
            }
            else if (axis == Vector3.right)
            {
                value = "NegativeX";
            }
            else if (axis == Vector3.up)
            {
                value = "PositiveY";
            }
            else if (axis == Vector3.down)
            {
                value = "NegativeY";
            }
            else if (axis == Vector3.forward)
            {
                value = "PositiveZ";
            }
            else if (axis == Vector3.back)
            {
                value = "NegativeZ";
            }
            else
            {
                value = "";
            }

            return !string.IsNullOrEmpty(value);
        }

        private ObjectID? FindTransformNodeID(Transform transform)
        {
            return transform && _transformNodeIDs.TryGetValue(transform, out var nodeID) ? nodeID : null;
        }
    }
}
