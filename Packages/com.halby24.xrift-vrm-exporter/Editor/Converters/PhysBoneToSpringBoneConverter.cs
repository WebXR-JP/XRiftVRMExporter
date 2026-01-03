// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-FileCopyrightText: 2024-present hkrn (original ndmf-vrm-exporter)
// SPDX-License-Identifier: MPL-2.0
//
// PhysBone → VRM SpringBone 変換処理
// NDMF VRM Exporter (https://github.com/hkrn/ndmf-vrm-exporter) の変換ロジックを参考に実装

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if XRIFT_HAS_VRCHAT_SDK
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;
#endif

using UniVRM10;
using XRift.VrmExporter.Components;

namespace XRift.VrmExporter.Converters
{
    /// <summary>
    /// VRCPhysBone を VRM 1.0 SpringBone コンポーネントに変換する
    /// </summary>
    internal static class PhysBoneToSpringBoneConverter
    {
#if XRIFT_HAS_VRCHAT_SDK
        /// <summary>
        /// PhysBone → SpringBone 変換を実行
        /// </summary>
        /// <param name="root">アバタールートGameObject</param>
        /// <param name="excludedColliderTransforms">除外するコライダーのTransform</param>
        /// <param name="excludedBoneTransforms">除外するSpringBoneのTransform</param>
        public static void Convert(
            GameObject root,
            HashSet<Transform> excludedColliderTransforms,
            HashSet<Transform> excludedBoneTransforms)
        {
            // Vrm10Instance を取得または作成
            var vrm10Instance = root.GetComponent<Vrm10Instance>();
            if (vrm10Instance == null)
            {
                vrm10Instance = root.AddComponent<Vrm10Instance>();
            }

            // コライダー変換（PhysBoneCollider → VRM10SpringBoneCollider）
            var colliderMapping = new Dictionary<VRCPhysBoneColliderBase, VRM10SpringBoneCollider>();
            ConvertAllColliders(root, excludedColliderTransforms, colliderMapping);

            // コライダーグループを作成（PhysBone毎にグループ化）
            var colliderGroups = new List<VRM10SpringBoneColliderGroup>();

            // SpringBone変換（PhysBone → VRM10SpringBoneJoint チェーン）
            var physBones = root.GetComponentsInChildren<VRCPhysBone>(true);
            foreach (var pb in physBones)
            {
                if (!pb.gameObject.activeInHierarchy)
                    continue;

                var pbTransform = pb.GetRootTransform();
                if (excludedBoneTransforms.Contains(pbTransform))
                    continue;

                // このPhysBoneに関連するコライダーグループを作成
                var colliderGroup = CreateColliderGroup(pb, colliderMapping, vrm10Instance);
                if (colliderGroup != null)
                {
                    colliderGroups.Add(colliderGroup);
                }

                // SpringBone チェーンを変換
                ConvertSpringBone(pb, vrm10Instance, colliderGroup != null ? new List<VRM10SpringBoneColliderGroup> { colliderGroup } : null);
            }

            // 元のPhysBoneコンポーネントを削除（クローン上なので問題なし）
            RemovePhysBoneComponents(root);

            Debug.Log($"[XRift VRM Exporter] Converted {physBones.Length} PhysBones, {colliderMapping.Count} Colliders to VRM SpringBone");
        }

        /// <summary>
        /// すべてのPhysBoneColliderをVRM10SpringBoneColliderに変換
        /// </summary>
        private static void ConvertAllColliders(
            GameObject root,
            HashSet<Transform> excludedTransforms,
            Dictionary<VRCPhysBoneColliderBase, VRM10SpringBoneCollider> mapping)
        {
            var colliders = root.GetComponentsInChildren<VRCPhysBoneCollider>(true);
            foreach (var collider in colliders)
            {
                if (!collider.gameObject.activeInHierarchy)
                    continue;

                var rootTransform = collider.GetRootTransform();
                if (excludedTransforms.Contains(rootTransform))
                    continue;

                var vrmCollider = ConvertCollider(collider);
                if (vrmCollider != null)
                {
                    mapping[collider] = vrmCollider;
                }
            }
        }

        /// <summary>
        /// 単一のPhysBoneColliderをVRM10SpringBoneColliderに変換
        /// </summary>
        private static VRM10SpringBoneCollider? ConvertCollider(VRCPhysBoneCollider collider)
        {
            var rootTransform = collider.GetRootTransform();

            // コライダーを配置するGameObjectにコンポーネントを追加
            var vrmCollider = rootTransform.gameObject.AddComponent<VRM10SpringBoneCollider>();

            switch (collider.shapeType)
            {
                case VRCPhysBoneColliderBase.ShapeType.Sphere:
                    vrmCollider.ColliderType = collider.insideBounds
                        ? VRM10SpringBoneColliderTypes.SphereInside
                        : VRM10SpringBoneColliderTypes.Sphere;
                    vrmCollider.Offset = collider.position;
                    vrmCollider.Radius = collider.radius;
                    break;

                case VRCPhysBoneColliderBase.ShapeType.Capsule:
                    vrmCollider.ColliderType = collider.insideBounds
                        ? VRM10SpringBoneColliderTypes.CapsuleInside
                        : VRM10SpringBoneColliderTypes.Capsule;
                    var height = (collider.height - collider.radius * 2.0f) * 0.5f;
                    vrmCollider.Offset = collider.position + collider.rotation * new Vector3(0.0f, -height, 0.0f);
                    vrmCollider.Tail = collider.position + collider.rotation * new Vector3(0.0f, height, 0.0f);
                    vrmCollider.Radius = collider.radius;
                    break;

                case VRCPhysBoneColliderBase.ShapeType.Plane:
                    vrmCollider.ColliderType = VRM10SpringBoneColliderTypes.Plane;
                    vrmCollider.Offset = collider.position;
                    vrmCollider.Normal = collider.rotation * Vector3.up;
                    break;

                default:
                    Debug.LogWarning($"[XRift VRM Exporter] Unknown collider shape type: {collider.shapeType}");
                    UnityEngine.Object.DestroyImmediate(vrmCollider);
                    return null;
            }

            return vrmCollider;
        }

        /// <summary>
        /// PhysBone用のコライダーグループを作成
        /// </summary>
        private static VRM10SpringBoneColliderGroup? CreateColliderGroup(
            VRCPhysBone pb,
            Dictionary<VRCPhysBoneColliderBase, VRM10SpringBoneCollider> colliderMapping,
            Vrm10Instance vrm10Instance)
        {
            if (pb.colliders == null || pb.colliders.Count == 0)
                return null;

            var vrmColliders = new List<VRM10SpringBoneCollider>();
            foreach (var pbCollider in pb.colliders)
            {
                if (pbCollider != null && colliderMapping.TryGetValue(pbCollider, out var vrmCollider))
                {
                    vrmColliders.Add(vrmCollider);
                }
            }

            if (vrmColliders.Count == 0)
                return null;

            // コライダーグループをPhysBoneのルートに作成
            var rootTransform = pb.GetRootTransform();
            var colliderGroup = rootTransform.gameObject.AddComponent<VRM10SpringBoneColliderGroup>();
            colliderGroup.Name = pb.name;
            colliderGroup.Colliders = vrmColliders;

            // Vrm10Instanceに登録
            vrm10Instance.SpringBone.ColliderGroups.Add(colliderGroup);

            return colliderGroup;
        }

        /// <summary>
        /// PhysBoneをSpringBoneチェーンに変換
        /// </summary>
        private static void ConvertSpringBone(
            VRCPhysBone pb,
            Vrm10Instance vrm10Instance,
            List<VRM10SpringBoneColliderGroup>? colliderGroups)
        {
            var rootTransform = pb.GetRootTransform();

            // ボーンチェーンを取得
            var chains = new List<List<Transform>> { new() { rootTransform } };
            RetrieveSpringBoneChainTransforms(rootTransform, ref chains);
            var validChains = chains.Where(chain => chain.Count > 0).ToList();

            var hasMultipleChains = validChains.Count > 1;
            var chainIndex = 1;

            foreach (var chain in validChains)
            {
                // MultiChildType処理
                IReadOnlyList<Transform> targetChain = chain;
                if (hasMultipleChains && pb.multiChildType == VRCPhysBoneBase.MultiChildType.Ignore && chainIndex > 1)
                {
                    // Ignoreモードで最初のチェーン以外はスキップ
                    chainIndex++;
                    continue;
                }

                if (hasMultipleChains && chainIndex == 1)
                {
                    // 最初のチェーンはルートをスキップ
                    targetChain = chain.Skip(1).ToList();
                }

                var springName = hasMultipleChains ? $"{pb.name}.{chainIndex}" : pb.name;
                ConvertSpringBoneChain(pb, springName, targetChain, vrm10Instance, colliderGroups);

                chainIndex++;
            }
        }

        /// <summary>
        /// 単一のSpringBoneチェーンを変換
        /// </summary>
        private static void ConvertSpringBoneChain(
            VRCPhysBone pb,
            string springName,
            IReadOnlyList<Transform> transforms,
            Vrm10Instance vrm10Instance,
            List<VRM10SpringBoneColliderGroup>? colliderGroups)
        {
            if (transforms.Count == 0)
                return;

            var rootTransform = pb.GetRootTransform();
            var joints = new List<VRM10SpringBoneJoint>();

            foreach (var transform in transforms)
            {
                // 各ボーンにJointコンポーネントを追加
                var joint = transform.gameObject.AddComponent<VRM10SpringBoneJoint>();

                // Depth比率を計算（カーブ評価用）
                var (upperDepth, lowerDepth) = FindTransformDepth(transform, rootTransform);
                var totalDepth = upperDepth + lowerDepth;
                var depthRatio = totalDepth != 0 ? upperDepth / (float)totalDepth : 0f;

                // パラメータ変換
                var gravity = EvaluateCurve(pb.gravity, pb.gravityCurve, depthRatio);
                var stiffness = EvaluateCurve(pb.stiffness, pb.stiffnessCurve, depthRatio);
                var hitRadius = EvaluateCurve(pb.radius, pb.radiusCurve, depthRatio);
                var pull = EvaluateCurve(pb.pull, pb.pullCurve, depthRatio);
                var immobile = EvaluateCurve(pb.immobile, pb.immobileCurve, depthRatio) * 0.5f;

                // LimitTypeに基づくファクター計算
                float stiffnessFactor, pullFactor;
                if (pb.limitType != VRCPhysBoneBase.LimitType.None)
                {
                    var maxAngleX = EvaluateCurve(pb.maxAngleX, pb.maxAngleXCurve, depthRatio);
                    stiffnessFactor = maxAngleX > 0f ? 1.0f / Mathf.Clamp01(maxAngleX / 180.0f) : 0f;
                    pullFactor = stiffnessFactor * 0.5f;
                }
                else
                {
                    stiffnessFactor = 1.0f;
                    pullFactor = 1.0f;
                }

                // VRM10SpringBoneJoint にパラメータ設定
                joint.m_gravityPower = gravity;
                joint.m_gravityDir = new Vector3(0, -1, 0);
                joint.m_stiffnessForce = immobile + stiffness * stiffnessFactor;
                joint.m_dragForce = Mathf.Clamp01(immobile + pull * pullFactor);
                joint.m_jointRadius = hitRadius;

                joints.Add(joint);
            }

            // Springを作成してVrm10Instanceに登録
            var spring = new Vrm10InstanceSpringBone.Spring(springName)
            {
                Joints = joints,
                ColliderGroups = colliderGroups ?? new List<VRM10SpringBoneColliderGroup>()
            };
            vrm10Instance.SpringBone.Springs.Add(spring);
        }

        /// <summary>
        /// アニメーションカーブを評価
        /// </summary>
        private static float EvaluateCurve(float value, AnimationCurve? curve, float t)
        {
            if (curve != null && curve.length > 0)
            {
                return curve.Evaluate(t) * value;
            }
            return value;
        }

        /// <summary>
        /// ボーンチェーンを再帰的に取得
        /// </summary>
        private static bool RetrieveSpringBoneChainTransforms(Transform transform, ref List<List<Transform>> chains)
        {
            var numChildren = 0;
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child == null)
                    continue;

                chains.Last().Add(child);
                if (RetrieveSpringBoneChainTransforms(child, ref chains))
                {
                    chains.Add(new List<Transform>());
                }
                numChildren++;
            }
            return numChildren > 0;
        }

        /// <summary>
        /// Transform の深さを計算（上方向と下方向）
        /// </summary>
        private static (int upperDepth, int lowerDepth) FindTransformDepth(Transform? transform, Transform? root)
        {
            var upperDepth = 0;
            var upperTransform = transform;
            while (upperTransform != null && upperTransform != root)
            {
                upperDepth++;
                upperTransform = upperTransform.parent;
            }

            var lowerDepth = 0;
            if (CalcTransformDepth(transform, true, ref lowerDepth))
            {
                lowerDepth++;
            }

            return (upperDepth, lowerDepth);
        }

        /// <summary>
        /// Transform の下方向深さを再帰的に計算
        /// </summary>
        private static bool CalcTransformDepth(Transform? transform, bool incrementDepth, ref int depth)
        {
            var numChildren = 0;
            var hasChildren = false;
            for (var i = 0; i < (transform?.childCount ?? 0); i++)
            {
                var child = transform!.GetChild(i);
                if (child == null)
                    continue;

                hasChildren |= CalcTransformDepth(child, !hasChildren, ref depth);
                numChildren++;
            }

            if (hasChildren && incrementDepth)
            {
                depth++;
            }

            return numChildren > 0;
        }

        /// <summary>
        /// 元のPhysBoneコンポーネントを削除
        /// </summary>
        private static void RemovePhysBoneComponents(GameObject root)
        {
            // PhysBone削除
            var physBones = root.GetComponentsInChildren<VRCPhysBone>(true);
            foreach (var pb in physBones)
            {
                UnityEngine.Object.DestroyImmediate(pb);
            }

            // PhysBoneCollider削除
            var colliders = root.GetComponentsInChildren<VRCPhysBoneCollider>(true);
            foreach (var collider in colliders)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }
#else
        /// <summary>
        /// VRChat SDK がない場合のスタブ
        /// </summary>
        public static void Convert(
            GameObject root,
            HashSet<Transform> excludedColliderTransforms,
            HashSet<Transform> excludedBoneTransforms)
        {
            Debug.Log("[XRift VRM Exporter] VRChat SDK not available, skipping PhysBone conversion");
        }
#endif
    }
}
