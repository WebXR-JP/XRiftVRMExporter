// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-FileCopyrightText: 2024-present hkrn (original ndmf-vrm-exporter)
// SPDX-License-Identifier: MPL-2.0
//
// This file contains code derived from ndmf-vrm-exporter
// https://github.com/hkrn/ndmf-vrm-exporter

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEngine;
using XRift.VrmExporter.Core;
using XRift.VrmExporter.VRM.Gltf;
using VrmCore = XRift.VrmExporter.VRM.Core;

namespace XRift.VrmExporter.Converters
{
    /// <summary>
    /// VRM Core拡張（VRMC_vrm）のエクスポート処理
    /// </summary>
    internal sealed class VrmRootExporter
    {
        private readonly GameObject _gameObject;
        private readonly IAssetSaver _assetSaver;
        private readonly ImmutableDictionary<Transform, ObjectID> _transformNodeIDs;
        private readonly ImmutableDictionary<string, (ObjectID, int)> _allMorphTargets;
        private readonly ISet<string> _extensionUsed;

        public VrmRootExporter(
            GameObject gameObject,
            IAssetSaver assetSaver,
            IDictionary<Transform, ObjectID> transformNodeIDs,
            IDictionary<string, (ObjectID, int)> allMorphTargets,
            ISet<string> extensionUsed)
        {
            _gameObject = gameObject;
            _assetSaver = assetSaver;
            _allMorphTargets = ImmutableDictionary.CreateRange(allMorphTargets);
            _transformNodeIDs = ImmutableDictionary.CreateRange(transformNodeIDs);
            _extensionUsed = extensionUsed;
        }

        /// <summary>
        /// VRM Coreをエクスポート
        /// </summary>
        public VrmCore.Core ExportCore(ObjectID thumbnailImage)
        {
            var vrmRoot = new VrmCore.Core();
            HumanoidConverter.ExportHumanoidBone(_gameObject, _transformNodeIDs, ref vrmRoot);
            ExportMeta(thumbnailImage, ref vrmRoot);
            // TODO: ExportExpression, ExportLookAt
            return vrmRoot;
        }

        /// <summary>
        /// メタデータをエクスポート
        /// </summary>
        private void ExportMeta(ObjectID thumbnailImage, ref VrmCore.Core core)
        {
            var meta = core.Meta;

            // デフォルト値の設定
            meta.Name = _gameObject.name;
            meta.Authors = new List<string> { "Unknown" };
            meta.LicenseUrl = VrmCore.Meta.DefaultLicenseUrl;
            meta.AvatarPermission = VrmCore.AvatarPermission.OnlyAuthor;
            meta.CommercialUsage = VrmCore.CommercialUsage.PersonalNonProfit;
            meta.Modification = VrmCore.Modification.Prohibited;
            meta.CreditNotation = VrmCore.CreditNotation.Required;
            meta.AllowRedistribution = false;
            meta.AllowExcessivelyViolentUsage = false;
            meta.AllowExcessivelySexualUsage = false;
            meta.AllowPoliticalOrReligiousUsage = false;
            meta.AllowAntisocialOrHateUsage = false;

            if (!thumbnailImage.IsNull)
            {
                meta.ThumbnailImage = thumbnailImage;
            }

            // TODO: コンポーネントからメタデータを読み込む
        }
    }
}
