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
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using XRift.VrmExporter.VRM;
using XRift.VrmExporter.VRM.Gltf;
using VrmSpringBone = XRift.VrmExporter.VRM.SpringBone;

#if XRIFT_HAS_VRCHAT_SDK
using VRC.SDK3.Dynamics.PhysBone.Components;
#endif

namespace XRift.VrmExporter.Converters
{
    /// <summary>
    /// VRChat PhysBone → VRM SpringBone 変換
    /// </summary>
    internal sealed class SpringBoneConverter
    {
        private const string VrmcSpringBoneExtendedCollider = "VRMC_springBone_extended_collider";
        private const float FarDistance = 10000000.0f;

        private readonly GameObject _gameObject;
        private readonly ImmutableDictionary<Transform, ObjectID> _transformNodeIDs;
        private readonly ISet<string> _extensionUsed;
        private readonly ISet<Transform> _excludedSpringBoneTransforms;
        private readonly ISet<Transform> _excludedSpringBoneColliderTransforms;

        public SpringBoneConverter(
            GameObject gameObject,
            IDictionary<Transform, ObjectID> transformNodeIDs,
            ISet<string> extensionUsed,
            ISet<Transform>? excludedSpringBoneTransforms = null,
            ISet<Transform>? excludedSpringBoneColliderTransforms = null)
        {
            _gameObject = gameObject;
            _transformNodeIDs = ImmutableDictionary.CreateRange(transformNodeIDs);
            _extensionUsed = extensionUsed;
            _excludedSpringBoneTransforms = excludedSpringBoneTransforms ?? new HashSet<Transform>();
            _excludedSpringBoneColliderTransforms = excludedSpringBoneColliderTransforms ?? new HashSet<Transform>();
        }

        /// <summary>
        /// SpringBone拡張をエクスポート
        /// </summary>
        public VrmSpringBone.SpringBone Export()
        {
            IList<VrmSpringBone.Collider> colliders = new List<VrmSpringBone.Collider>();
            IList<VrmSpringBone.ColliderGroup> colliderGroups = new List<VrmSpringBone.ColliderGroup>();
            IList<VrmSpringBone.Spring> springs = new List<VrmSpringBone.Spring>();

#if XRIFT_HAS_VRCHAT_SDK
            IList<VRCPhysBoneColliderBase> pbColliders = new List<VRCPhysBoneColliderBase>();

            // コライダーの変換
            foreach (var (transform, _) in _transformNodeIDs)
            {
                if (!transform.gameObject.activeInHierarchy ||
                    _excludedSpringBoneColliderTransforms.Contains(transform) ||
                    !transform.TryGetComponent<VRCPhysBoneCollider>(out _))
                {
                    continue;
                }

                var innerColliders = transform.GetComponents<VRCPhysBoneCollider>();
                foreach (var innerCollider in innerColliders!)
                {
                    ConvertBoneCollider(innerCollider, ref pbColliders, ref colliders);
                }
            }

            var immutablePbColliders = ImmutableList.CreateRange(pbColliders);

            // コライダーグループの変換
            foreach (var (transform, _) in _transformNodeIDs)
            {
                if (!transform.gameObject.activeInHierarchy ||
                    _excludedSpringBoneTransforms.Contains(transform) ||
                    !transform.TryGetComponent<VRCPhysBone>(out _))
                {
                    continue;
                }

                var bones = transform.GetComponents<VRCPhysBone>();
                foreach (var bone in bones!)
                {
                    ConvertColliderGroup(bone, immutablePbColliders, ref colliderGroups);
                }
            }

            var immutableColliderGroups = ImmutableList.CreateRange(colliderGroups);

            // SpringBoneの変換
            foreach (var (transform, _) in _transformNodeIDs)
            {
                if (!transform.gameObject.activeInHierarchy ||
                    _excludedSpringBoneTransforms.Contains(transform) ||
                    !transform.TryGetComponent<VRCPhysBone>(out _))
                {
                    continue;
                }

                var bones = transform.GetComponents<VRCPhysBone>();
                foreach (var bone in bones!)
                {
                    ConvertSpringBone(bone, immutablePbColliders, immutableColliderGroups, ref springs);
                }
            }
#endif

            return new VrmSpringBone.SpringBone
            {
                Colliders = colliders.Count > 0 ? colliders : null,
                ColliderGroups = colliderGroups.Count > 0 ? colliderGroups : null,
                Springs = springs.Count > 0 ? springs : null,
            };
        }

#if XRIFT_HAS_VRCHAT_SDK
        private void ConvertBoneCollider(
            VRCPhysBoneCollider collider,
            ref IList<VRCPhysBoneColliderBase> pbColliders,
            ref IList<VrmSpringBone.Collider> colliders)
        {
            var rootTransform = collider.GetRootTransform();
            var nodeID = FindTransformNodeID(rootTransform);
            if (!nodeID.HasValue)
            {
                Debug.LogWarning($"Collider root transform {rootTransform} not found due to inactive");
                return;
            }

            switch (collider.shapeType)
            {
                case VRCPhysBoneColliderBase.ShapeType.Capsule:
                    ConvertCapsuleCollider(collider, nodeID.Value, ref colliders);
                    break;
                case VRCPhysBoneColliderBase.ShapeType.Plane:
                    ConvertPlaneCollider(collider, nodeID.Value, ref colliders);
                    break;
                case VRCPhysBoneColliderBase.ShapeType.Sphere:
                    ConvertSphereCollider(collider, nodeID.Value, ref colliders);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            pbColliders.Add(collider);
        }

        private void ConvertCapsuleCollider(
            VRCPhysBoneCollider collider,
            ObjectID nodeID,
            ref IList<VrmSpringBone.Collider> colliders)
        {
            var position = collider.position;
            var radius = collider.radius;
            var height = (collider.height - radius * 2.0f) * 0.5f;
            var offset = position + collider.rotation * new Vector3(0.0f, -height, 0.0f);
            var tail = position + collider.rotation * new Vector3(0.0f, height, 0.0f);

            var capsuleCollider = new VrmSpringBone.Collider
            {
                Node = nodeID,
                Shape = new VrmSpringBone.Shape
                {
                    Capsule = new VrmSpringBone.Capsule
                    {
                        Offset = offset.ToVector3WithCoordinateSpace(),
                        Radius = radius,
                        Tail = tail.ToVector3WithCoordinateSpace(),
                    }
                }
            };

            if (collider.insideBounds)
            {
                var extendedCollider = new VrmSpringBone.ExtendedCollider
                {
                    Spec = "1.0",
                    Shape = new VrmSpringBone.ExtendedShape
                    {
                        Capsule = new VrmSpringBone.ShapeCapsule
                        {
                            Offset = offset.ToVector3WithCoordinateSpace(),
                            Radius = radius,
                            Tail = tail.ToVector3WithCoordinateSpace(),
                            Inside = true,
                        }
                    }
                };
                // フォールバック用のダミー値
                capsuleCollider.Shape.Capsule.Offset = new System.Numerics.Vector3(-FarDistance);
                capsuleCollider.Shape.Capsule.Tail = new System.Numerics.Vector3(-FarDistance);
                capsuleCollider.Shape.Capsule.Radius = 0.0f;
                capsuleCollider.Extensions ??= new Dictionary<string, JToken>();
                capsuleCollider.Extensions.Add(VrmcSpringBoneExtendedCollider,
                    Document.SaveAsNode(extendedCollider));
                _extensionUsed.Add(VrmcSpringBoneExtendedCollider);
            }

            colliders.Add(capsuleCollider);
        }

        private void ConvertPlaneCollider(
            VRCPhysBoneCollider collider,
            ObjectID nodeID,
            ref IList<VrmSpringBone.Collider> colliders)
        {
            var offset = collider.position;
            var normal = collider.axis;

            var extendedCollider = new VrmSpringBone.ExtendedCollider
            {
                Spec = "1.0",
                Shape = new VrmSpringBone.ExtendedShape
                {
                    Plane = new VrmSpringBone.ShapePlane
                    {
                        Offset = offset.ToVector3WithCoordinateSpace(),
                        Normal = normal.ToVector3WithCoordinateSpace(),
                    }
                }
            };

            // Planeは拡張として実装し、フォールバックとして巨大なSphereを使用
            var planeCollider = new VrmSpringBone.Collider
            {
                Node = nodeID,
                Shape = new VrmSpringBone.Shape
                {
                    Sphere = new VrmSpringBone.Sphere
                    {
                        Offset = offset.ToVector3WithCoordinateSpace() -
                                 (normal * FarDistance).ToVector3WithCoordinateSpace(),
                        Radius = FarDistance,
                    }
                },
                Extensions = new Dictionary<string, JToken>(),
            };
            planeCollider.Extensions.Add(VrmcSpringBoneExtendedCollider,
                Document.SaveAsNode(extendedCollider));
            colliders.Add(planeCollider);
            _extensionUsed.Add(VrmcSpringBoneExtendedCollider);
        }

        private void ConvertSphereCollider(
            VRCPhysBoneCollider collider,
            ObjectID nodeID,
            ref IList<VrmSpringBone.Collider> colliders)
        {
            var offset = collider.position;
            var radius = collider.radius;

            var sphereCollider = new VrmSpringBone.Collider
            {
                Node = nodeID,
                Shape = new VrmSpringBone.Shape
                {
                    Sphere = new VrmSpringBone.Sphere
                    {
                        Offset = offset.ToVector3WithCoordinateSpace(),
                        Radius = radius,
                    }
                }
            };

            if (collider.insideBounds)
            {
                var extendedCollider = new VrmSpringBone.ExtendedCollider
                {
                    Spec = "1.0",
                    Shape = new VrmSpringBone.ExtendedShape
                    {
                        Sphere = new VrmSpringBone.ShapeSphere
                        {
                            Offset = offset.ToVector3WithCoordinateSpace(),
                            Radius = radius,
                            Inside = true,
                        }
                    }
                };
                // フォールバック用のダミー値
                sphereCollider.Shape.Sphere.Offset = new System.Numerics.Vector3(-FarDistance);
                sphereCollider.Shape.Sphere.Radius = 0.0f;
                sphereCollider.Extensions ??= new Dictionary<string, JToken>();
                sphereCollider.Extensions.Add(VrmcSpringBoneExtendedCollider,
                    Document.SaveAsNode(extendedCollider));
                _extensionUsed.Add(VrmcSpringBoneExtendedCollider);
            }

            colliders.Add(sphereCollider);
        }

        private static void ConvertColliderGroup(
            VRCPhysBone pb,
            IImmutableList<VRCPhysBoneColliderBase> pbColliders,
            ref IList<VrmSpringBone.ColliderGroup> colliderGroups)
        {
            var colliders = (from collider in pb.colliders
                select pbColliders.IndexOf(collider)
                into index
                where index != -1
                select new ObjectID((uint)index)).ToList();

            if (colliders.Count <= 0)
                return;

            var colliderGroup = new VrmSpringBone.ColliderGroup
            {
                Name = new UnicodeString(pb.name),
                Colliders = colliders,
            };
            colliderGroups.Add(colliderGroup);
        }

        private static bool RetrieveSpringBoneChainTransforms(Transform transform, ref List<List<Transform>> chains)
        {
            var numChildren = 0;
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (!child || child is null)
                {
                    continue;
                }

                chains.Last().Add(child);
                if (RetrieveSpringBoneChainTransforms(child, ref chains))
                {
                    chains.Add(new List<Transform>());
                }

                numChildren++;
            }

            return numChildren > 0;
        }

        private static bool CalcTransformDepth(Transform? transform, bool incrementDepth, ref int depth)
        {
            var numChildren = 0;
            var hasChildren = false;
            for (var i = 0; i < transform?.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (!child || child is null)
                {
                    continue;
                }

                hasChildren |= CalcTransformDepth(child, !hasChildren, ref depth);
                numChildren++;
            }

            if (hasChildren && incrementDepth)
            {
                depth++;
            }

            return numChildren > 0;
        }

        private static (int, int) FindTransformDepth(Transform? transform, Transform? root)
        {
            var upperDepth = 0;
            var upperTransform = transform;
            while (upperTransform && upperTransform != root)
            {
                upperDepth++;
                upperTransform = upperTransform?.parent;
            }

            var lowerDepth = 0;
            if (CalcTransformDepth(transform, true, ref lowerDepth))
            {
                lowerDepth++;
            }

            return (upperDepth, lowerDepth);
        }

        private void ConvertSpringBone(
            VRCPhysBone pb,
            IImmutableList<VRCPhysBoneColliderBase> pbColliders,
            IImmutableList<VrmSpringBone.ColliderGroup> colliderGroups,
            ref IList<VrmSpringBone.Spring> springs)
        {
            var rootTransform = pb.GetRootTransform();
            var chains = new List<List<Transform>>
            {
                new() { rootTransform }
            };
            RetrieveSpringBoneChainTransforms(rootTransform, ref chains);
            var newChains = chains.Where(chain => chain.Count != 0).Select(ImmutableList.CreateRange)
                .ToImmutableList();
            var hasChainBranch = newChains.Count > 1;
            var index = 1;

            foreach (var transforms in newChains)
            {
                switch (hasChainBranch)
                {
                    case true when pb.multiChildType == VRCPhysBoneBase.MultiChildType.Ignore && index == 1:
                    {
                        var name = $"{pb.name}.{index}";
                        ConvertSpringBoneInner(pb, name, transforms.Skip(1).ToImmutableList(), pbColliders,
                            colliderGroups, ref springs);
                        break;
                    }
                    case true:
                    {
                        var name = $"{pb.name}.{index}";
                        ConvertSpringBoneInner(pb, name, transforms, pbColliders, colliderGroups, ref springs);
                        break;
                    }
                    default:
                    {
                        var name = pb.name;
                        ConvertSpringBoneInner(pb, name, transforms, pbColliders, colliderGroups, ref springs);
                        break;
                    }
                }

                index++;
            }
        }

        private void ConvertSpringBoneInner(
            VRCPhysBone pb,
            string name,
            IImmutableList<Transform> transforms,
            IImmutableList<VRCPhysBoneColliderBase> pbColliders,
            IImmutableList<VrmSpringBone.ColliderGroup> colliderGroups,
            ref IList<VrmSpringBone.Spring> springs)
        {
            var rootTransform = pb.GetRootTransform();
            var joints = transforms.Select(transform =>
                {
                    var nodeID = FindTransformNodeID(transform);
                    if (!nodeID.HasValue)
                    {
                        Debug.LogWarning($"Joint transform {transform} not found due to inactive");
                        return null;
                    }

                    var (upperDepth, lowerDepth) = FindTransformDepth(transform, rootTransform);
                    var totalDepth = upperDepth + lowerDepth;
                    var depthRatio = totalDepth != 0 ? upperDepth / (float)totalDepth : 0;

                    var evaluate = new Func<float, AnimationCurve, float>((value, curve) =>
                        curve is { length: > 0 }
                            ? curve.Evaluate(depthRatio) * value
                            : value);

                    var gravity = evaluate(pb.gravity, pb.gravityCurve);
                    var stiffness = evaluate(pb.stiffness, pb.stiffnessCurve);
                    var hitRadius = evaluate(pb.radius, pb.radiusCurve);
                    var pull = evaluate(pb.pull, pb.pullCurve);
                    var immobile = evaluate(pb.immobile, pb.immobileCurve) * 0.5f;

                    float stiffnessFactor, pullFactor;
                    if (pb.limitType != VRCPhysBoneBase.LimitType.None)
                    {
                        var maxAngleX = evaluate(pb.maxAngleX, pb.maxAngleXCurve);
                        stiffnessFactor = maxAngleX > 0.0f ? 1.0f / Mathf.Clamp01(maxAngleX / 180.0f) : 0.0f;
                        pullFactor = stiffnessFactor * 0.5f;
                    }
                    else
                    {
                        stiffnessFactor = 1.0f;
                        pullFactor = 1.0f;
                    }

                    return new VrmSpringBone.Joint
                    {
                        Node = nodeID.Value,
                        HitRadius = hitRadius,
                        Stiffness = immobile + stiffness * stiffnessFactor,
                        GravityPower = gravity,
                        GravityDir = -System.Numerics.Vector3.UnitY,
                        DragForce = Mathf.Clamp01(immobile + pull * pullFactor),
                    };
                })
                .ToList();

            var colliders = (from pbCollider in pb.colliders
                select pbColliders.IndexOf(pbCollider)
                into index
                where index != -1
                select new ObjectID((uint)index)).ToList();

            var groupID = 0;
            var newColliderGroups = new HashSet<ObjectID>();
            foreach (var group in colliderGroups)
            {
                if (colliders.Any(id => group.Colliders.IndexOf(id) != -1))
                {
                    newColliderGroups.Add(new ObjectID((uint)groupID));
                }

                groupID++;
            }

            var spring = new VrmSpringBone.Spring
            {
                Name = new UnicodeString(name),
                Center = null,
                ColliderGroups = newColliderGroups.Count > 0 ? newColliderGroups.ToList() : null,
                Joints = joints.Where(joint => joint != null).Select(joint => joint!).ToList(),
            };
            springs.Add(spring);
        }
#endif

        private ObjectID? FindTransformNodeID(Transform transform)
        {
            return transform && _transformNodeIDs.TryGetValue(transform, out var nodeID) ? nodeID : null;
        }
    }
}
