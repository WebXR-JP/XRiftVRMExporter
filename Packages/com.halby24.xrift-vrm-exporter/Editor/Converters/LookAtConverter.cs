// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-FileCopyrightText: 2024-present hkrn (original ndmf-vrm-exporter)
// SPDX-License-Identifier: MPL-2.0
//
// This file contains code derived from ndmf-vrm-exporter
// https://github.com/hkrn/ndmf-vrm-exporter

#nullable enable

using System;
using UnityEngine;
using VrmCore = XRift.VrmExporter.VRM.Core;

#if XRIFT_HAS_VRCHAT_SDK
using VRC.SDK3.Avatars.Components;
#endif

namespace XRift.VrmExporter.Converters
{
    /// <summary>
    /// VRM LookAt変換
    /// </summary>
    internal static class LookAtConverter
    {
#if XRIFT_HAS_VRCHAT_SDK
        /// <summary>
        /// VRCAvatarDescriptorからLookAtをエクスポート
        /// </summary>
        public static void ExportLookAt(GameObject gameObject, ref VrmCore.Core core)
        {
            var descriptor = gameObject.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null)
                return;

            var animator = gameObject.GetComponent<Animator>();
            if (animator == null)
                return;

            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head == null)
                return;

            var headPosition = head.position - gameObject.transform.position;
            var offsetFromHeadBone = descriptor.ViewPosition - headPosition;

            core.LookAt = new VrmCore.LookAt
            {
                Type = VrmCore.LookAtType.Bone,
                OffsetFromHeadBone = offsetFromHeadBone.ToVector3WithCoordinateSpace(),
            };

            // 下方向
            var down = descriptor.customEyeLookSettings.eyesLookingDown;
            if (down != null)
            {
                var leftAngles = down.left.eulerAngles;
                var rightAngles = down.right.eulerAngles;
                core.LookAt.RangeMapVerticalDown = new VrmCore.RangeMap
                {
                    InputMaxValue = Math.Min(leftAngles.x, rightAngles.x),
                    OutputScale = 1.0f,
                };
            }

            // 上方向
            var up = descriptor.customEyeLookSettings.eyesLookingUp;
            if (up != null)
            {
                var leftAngles = up.left.eulerAngles;
                var rightAngles = up.right.eulerAngles;
                core.LookAt.RangeMapVerticalUp = new VrmCore.RangeMap
                {
                    InputMaxValue = Math.Min(leftAngles.x, rightAngles.x),
                    OutputScale = 1.0f,
                };
            }

            // 左右方向
            var left = descriptor.customEyeLookSettings.eyesLookingLeft;
            var right = descriptor.customEyeLookSettings.eyesLookingRight;
            if (left == null || right == null)
            {
                return;
            }

            var leftLeftAngles = left.left.eulerAngles;
            var leftRightAngles = left.right.eulerAngles;
            var rightLeftAngles = right.left.eulerAngles;
            var rightRightAngles = right.right.eulerAngles;

            core.LookAt.RangeMapHorizontalInner = new VrmCore.RangeMap
            {
                InputMaxValue = Math.Min(leftLeftAngles.y, leftRightAngles.y),
                OutputScale = 1.0f,
            };
            core.LookAt.RangeMapHorizontalOuter = new VrmCore.RangeMap
            {
                InputMaxValue = Math.Min(rightLeftAngles.y, rightRightAngles.y),
                OutputScale = 1.0f,
            };
        }
#else
        /// <summary>
        /// LookAtエクスポート（VRChat SDKなし）
        /// </summary>
        public static void ExportLookAt(GameObject gameObject, ref VrmCore.Core core)
        {
            var animator = gameObject.GetComponent<Animator>();
            if (animator == null)
                return;

            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head == null)
                return;

            // VRChat SDKがない場合はデフォルト値を設定
            core.LookAt = new VrmCore.LookAt
            {
                Type = VrmCore.LookAtType.Bone,
                OffsetFromHeadBone = new System.Numerics.Vector3(0, 0.06f, 0),
            };
        }
#endif
    }
}
