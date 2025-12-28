// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-FileCopyrightText: 2024-present hkrn (original ndmf-vrm-exporter)
// SPDX-License-Identifier: MPL-2.0
//
// This file contains code derived from ndmf-vrm-exporter
// https://github.com/hkrn/ndmf-vrm-exporter

#nullable enable

using System.Collections.Generic;
using UnityEngine;
using XRift.VrmExporter.VRM.Gltf;
using VrmCore = XRift.VrmExporter.VRM.Core;

namespace XRift.VrmExporter.Converters
{
    /// <summary>
    /// UnityのAnimator/HumanoidをVRM Humanoidボーンに変換するクラス
    /// </summary>
    internal static class HumanoidConverter
    {
        /// <summary>
        /// Humanoidボーンをエクスポート
        /// </summary>
        public static void ExportHumanoidBone(
            GameObject gameObject,
            IDictionary<Transform, ObjectID> transformNodeIDs,
            ref VrmCore.Core core)
        {
            var animator = gameObject.GetComponent<Animator>();
            if (animator == null || !animator.isHuman)
            {
                Debug.LogWarning("[XRift VRM] Animator is not set or not humanoid");
                return;
            }

            var hb = core.Humanoid.HumanBones;

            // 必須ボーン
            hb.Hips.Node = GetRequiredHumanBoneNodeID(animator, transformNodeIDs, HumanBodyBones.Hips);
            hb.Spine.Node = GetRequiredHumanBoneNodeID(animator, transformNodeIDs, HumanBodyBones.Spine);
            hb.Head.Node = GetRequiredHumanBoneNodeID(animator, transformNodeIDs, HumanBodyBones.Head);
            hb.LeftUpperLeg.Node = GetRequiredHumanBoneNodeID(animator, transformNodeIDs, HumanBodyBones.LeftUpperLeg);
            hb.LeftLowerLeg.Node = GetRequiredHumanBoneNodeID(animator, transformNodeIDs, HumanBodyBones.LeftLowerLeg);
            hb.LeftFoot.Node = GetRequiredHumanBoneNodeID(animator, transformNodeIDs, HumanBodyBones.LeftFoot);
            hb.RightUpperLeg.Node = GetRequiredHumanBoneNodeID(animator, transformNodeIDs, HumanBodyBones.RightUpperLeg);
            hb.RightLowerLeg.Node = GetRequiredHumanBoneNodeID(animator, transformNodeIDs, HumanBodyBones.RightLowerLeg);
            hb.RightFoot.Node = GetRequiredHumanBoneNodeID(animator, transformNodeIDs, HumanBodyBones.RightFoot);
            hb.LeftUpperArm.Node = GetRequiredHumanBoneNodeID(animator, transformNodeIDs, HumanBodyBones.LeftUpperArm);
            hb.LeftLowerArm.Node = GetRequiredHumanBoneNodeID(animator, transformNodeIDs, HumanBodyBones.LeftLowerArm);
            hb.LeftHand.Node = GetRequiredHumanBoneNodeID(animator, transformNodeIDs, HumanBodyBones.LeftHand);
            hb.RightUpperArm.Node = GetRequiredHumanBoneNodeID(animator, transformNodeIDs, HumanBodyBones.RightUpperArm);
            hb.RightLowerArm.Node = GetRequiredHumanBoneNodeID(animator, transformNodeIDs, HumanBodyBones.RightLowerArm);
            hb.RightHand.Node = GetRequiredHumanBoneNodeID(animator, transformNodeIDs, HumanBodyBones.RightHand);

            // オプションボーン - Torso
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.Chest, ref hb.Chest);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.UpperChest, ref hb.UpperChest);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.Neck, ref hb.Neck);

            // オプションボーン - Head
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftEye, ref hb.LeftEye);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightEye, ref hb.RightEye);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.Jaw, ref hb.Jaw);

            // オプションボーン - Leg
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftToes, ref hb.LeftToes);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightToes, ref hb.RightToes);

            // オプションボーン - Arm
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftShoulder, ref hb.LeftShoulder);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightShoulder, ref hb.RightShoulder);

            // オプションボーン - 左手指（VRM 1.0 では Thumb の命名が異なる）
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftThumbProximal, ref hb.LeftThumbMetacarpal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftThumbIntermediate, ref hb.LeftThumbProximal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftThumbDistal, ref hb.LeftThumbDistal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftIndexProximal, ref hb.LeftIndexProximal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftIndexIntermediate, ref hb.LeftIndexIntermediate);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftIndexDistal, ref hb.LeftIndexDistal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftMiddleProximal, ref hb.LeftMiddleProximal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftMiddleIntermediate, ref hb.LeftMiddleIntermediate);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftMiddleDistal, ref hb.LeftMiddleDistal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftRingProximal, ref hb.LeftRingProximal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftRingIntermediate, ref hb.LeftRingIntermediate);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftRingDistal, ref hb.LeftRingDistal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftLittleProximal, ref hb.LeftLittleProximal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftLittleIntermediate, ref hb.LeftLittleIntermediate);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.LeftLittleDistal, ref hb.LeftLittleDistal);

            // オプションボーン - 右手指
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightThumbProximal, ref hb.RightThumbMetacarpal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightThumbIntermediate, ref hb.RightThumbProximal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightThumbDistal, ref hb.RightThumbDistal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightIndexProximal, ref hb.RightIndexProximal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightIndexIntermediate, ref hb.RightIndexIntermediate);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightIndexDistal, ref hb.RightIndexDistal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightMiddleProximal, ref hb.RightMiddleProximal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightMiddleIntermediate, ref hb.RightMiddleIntermediate);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightMiddleDistal, ref hb.RightMiddleDistal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightRingProximal, ref hb.RightRingProximal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightRingIntermediate, ref hb.RightRingIntermediate);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightRingDistal, ref hb.RightRingDistal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightLittleProximal, ref hb.RightLittleProximal);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightLittleIntermediate, ref hb.RightLittleIntermediate);
            SetOptionalBone(animator, transformNodeIDs, HumanBodyBones.RightLittleDistal, ref hb.RightLittleDistal);
        }

        private static ObjectID GetRequiredHumanBoneNodeID(
            Animator animator,
            IDictionary<Transform, ObjectID> transformNodeIDs,
            HumanBodyBones bone)
        {
            var boneTransform = animator.GetBoneTransform(bone);
            if (boneTransform == null)
            {
                Debug.LogError($"[XRift VRM] Required bone {bone} not found");
                return ObjectID.Null;
            }

            if (!transformNodeIDs.TryGetValue(boneTransform, out var nodeID))
            {
                Debug.LogError($"[XRift VRM] Required bone {bone} not in node map");
                return ObjectID.Null;
            }

            return nodeID;
        }

        private static ObjectID? GetOptionalHumanBoneNodeID(
            Animator animator,
            IDictionary<Transform, ObjectID> transformNodeIDs,
            HumanBodyBones bone)
        {
            var boneTransform = animator.GetBoneTransform(bone);
            if (boneTransform == null)
            {
                return null;
            }

            if (!transformNodeIDs.TryGetValue(boneTransform, out var nodeID))
            {
                return null;
            }

            return nodeID;
        }

        private static void SetOptionalBone(
            Animator animator,
            IDictionary<Transform, ObjectID> transformNodeIDs,
            HumanBodyBones bone,
            ref VrmCore.HumanBone? target)
        {
            var nodeID = GetOptionalHumanBoneNodeID(animator, transformNodeIDs, bone);
            if (nodeID.HasValue)
            {
                target = new VrmCore.HumanBone { Node = nodeID.Value };
            }
        }
    }
}
