// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-FileCopyrightText: 2024-present hkrn (original ndmf-vrm-exporter)
// SPDX-License-Identifier: MPL-2.0
//
// This file contains code derived from ndmf-vrm-exporter
// https://github.com/hkrn/ndmf-vrm-exporter

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Animations;
using XRift.VrmExporter.Converters;
using VrmCore = XRift.VrmExporter.VRM.Core;

namespace XRift.VrmExporter.Components
{
    /// <summary>
    /// VRM使用許可
    /// </summary>
    public enum VrmUsagePermission
    {
        Allow,
        Disallow,
    }

    /// <summary>
    /// XRift VRM Exporter 設定コンポーネント
    /// </summary>
    [AddComponentMenu("XRift VRM Exporter/VRM Export Description")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/halby24/xrift-vrm-exporter")]
    public sealed class XRiftVrmDescriptor : MonoBehaviour, INDMFEditorOnly, IExpressionSettings
    {
        // === メタデータ ===
        [NotKeyable] [SerializeField] internal bool metadataFoldout = true;
        [NotKeyable] [SerializeField] internal List<string> authors = new();
        [NotKeyable] [SerializeField] internal string? version;
        [NotKeyable] [SerializeField] internal string? copyrightInformation;
        [NotKeyable] [SerializeField] internal string? contactInformation;
        [NotKeyable] [SerializeField] internal List<string> references = new();
        [NotKeyable] [SerializeField] internal bool enableContactInformationOnVRChatAutofill = true;
        [NotKeyable] [SerializeField] internal string licenseUrl = VrmCore.Meta.DefaultLicenseUrl;
        [NotKeyable] [SerializeField] internal string? thirdPartyLicenses;
        [NotKeyable] [SerializeField] internal string? otherLicenseUrl;
        [NotKeyable] [SerializeField] internal VrmCore.AvatarPermission avatarPermission;
        [NotKeyable] [SerializeField] internal VrmCore.CommercialUsage commercialUsage;
        [NotKeyable] [SerializeField] internal VrmCore.CreditNotation creditNotation;
        [NotKeyable] [SerializeField] internal VrmCore.Modification modification;

        // === メタデータ許可設定 ===
        [NotKeyable] [SerializeField] internal bool metadataAllowFoldout;
        [NotKeyable] [SerializeField] internal VrmUsagePermission allowExcessivelyViolentUsage;
        [NotKeyable] [SerializeField] internal VrmUsagePermission allowExcessivelySexualUsage;
        [NotKeyable] [SerializeField] internal VrmUsagePermission allowPoliticalOrReligiousUsage;
        [NotKeyable] [SerializeField] internal VrmUsagePermission allowAntisocialOrHateUsage;
        [NotKeyable] [SerializeField] internal VrmUsagePermission allowRedistribution;
        [NotKeyable] [SerializeField] internal Texture2D? thumbnail;

        // === 表情 ===
        [NotKeyable] [SerializeField] internal bool expressionFoldout = true;
        [NotKeyable] [SerializeField] internal VrmExpressionProperty expressionPresetHappyBlendShape = VrmExpressionProperty.Happy;
        [NotKeyable] [SerializeField] internal VrmExpressionProperty expressionPresetAngryBlendShape = VrmExpressionProperty.Angry;
        [NotKeyable] [SerializeField] internal VrmExpressionProperty expressionPresetSadBlendShape = VrmExpressionProperty.Sad;
        [NotKeyable] [SerializeField] internal VrmExpressionProperty expressionPresetRelaxedBlendShape = VrmExpressionProperty.Relaxed;
        [NotKeyable] [SerializeField] internal VrmExpressionProperty expressionPresetSurprisedBlendShape = VrmExpressionProperty.Surprised;
        [NotKeyable] [SerializeField] internal bool expressionCustomBlendShapeNameFoldout;
        [NotKeyable] [SerializeField] internal List<VrmExpressionProperty> expressionCustomBlendShapes = new();

        // === SpringBone ===
        [NotKeyable] [SerializeField] internal bool springBoneFoldout;
        [NotKeyable] [SerializeField] internal List<Transform> excludedSpringBoneColliderTransforms = new();
        [NotKeyable] [SerializeField] internal List<Transform> excludedSpringBoneTransforms = new();

        // === Constraint ===
        [NotKeyable] [SerializeField] internal bool constraintFoldout;
        [NotKeyable] [SerializeField] internal List<Transform> excludedConstraintTransforms = new();

        // === MToon ===
        [NotKeyable] [SerializeField] internal bool mtoonFoldout;
        [NotKeyable] [SerializeField] internal bool enableMToonRimLight;
        [NotKeyable] [SerializeField] internal bool enableMToonMatCap;
        [NotKeyable] [SerializeField] internal bool enableMToonOutline = true;
        [NotKeyable] [SerializeField] internal bool enableBakingAlphaMaskTexture = true;

        // === デバッグ ===
        [NotKeyable] [SerializeField] internal bool debugFoldout;
        [NotKeyable] [SerializeField] internal bool makeAllNodeNamesUnique = true;
        [NotKeyable] [SerializeField] internal bool enableVertexColorOutput = true;
        [NotKeyable] [SerializeField] internal bool disableVertexColorOnLiltoon = true;
        [NotKeyable] [SerializeField] internal bool enableGenerateJsonFile;
        [NotKeyable] [SerializeField] internal bool deleteTemporaryObjects = true;
        [NotKeyable] [SerializeField] internal string? ktxToolPath;

        // === UI状態 ===
        [NotKeyable] [SerializeField] internal int metadataModeSelection;
        [NotKeyable] [SerializeField] internal int expressionModeSelection;

        // === 拡張機能 ===
        [NotKeyable] [SerializeField] internal bool extensionFoldout;
        [NotKeyable] [SerializeField] internal bool enableKhrMaterialsVariants = true;

        // === プロパティ ===
        public bool HasAuthor => authors.Count > 0 && !string.IsNullOrWhiteSpace(authors.First());

        public bool HasLicenseUrl =>
            !string.IsNullOrWhiteSpace(licenseUrl) && Uri.TryCreate(licenseUrl, UriKind.Absolute, out _);

        public bool HasAvatarRoot => RuntimeUtil.IsAvatarRoot(gameObject.transform);

        // === IExpressionSettings 実装 ===
        VrmExpressionProperty? IExpressionSettings.HappyExpression => expressionPresetHappyBlendShape;
        VrmExpressionProperty? IExpressionSettings.AngryExpression => expressionPresetAngryBlendShape;
        VrmExpressionProperty? IExpressionSettings.SadExpression => expressionPresetSadBlendShape;
        VrmExpressionProperty? IExpressionSettings.RelaxedExpression => expressionPresetRelaxedBlendShape;
        VrmExpressionProperty? IExpressionSettings.SurprisedExpression => expressionPresetSurprisedBlendShape;
        IEnumerable<VrmExpressionProperty> IExpressionSettings.CustomExpressions => expressionCustomBlendShapes;

        /// <summary>
        /// MToon変換設定を取得
        /// </summary>
        public MToonConvertSettings GetMToonConvertSettings()
        {
            return new MToonConvertSettings
            {
                EnableRimLight = enableMToonRimLight,
                EnableMatCap = enableMToonMatCap,
                EnableOutline = enableMToonOutline,
            };
        }

        /// <summary>
        /// 除外するSpringBoneコライダーTransformのセット
        /// </summary>
        public HashSet<Transform> GetExcludedSpringBoneColliderTransforms()
        {
            return new HashSet<Transform>(excludedSpringBoneColliderTransforms.Where(t => t != null));
        }

        /// <summary>
        /// 除外するSpringBone Transformのセット
        /// </summary>
        public HashSet<Transform> GetExcludedSpringBoneTransforms()
        {
            return new HashSet<Transform>(excludedSpringBoneTransforms.Where(t => t != null));
        }

        /// <summary>
        /// 除外するConstraint Transformのセット
        /// </summary>
        public HashSet<Transform> GetExcludedConstraintTransforms()
        {
            return new HashSet<Transform>(excludedConstraintTransforms.Where(t => t != null));
        }

        // ReSharper disable once Unity.RedundantEventFunction
        private void Start()
        {
            // 何もしないがチェックボックス表示のために必要
        }
    }
}
