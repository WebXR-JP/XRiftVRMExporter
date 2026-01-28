// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-License-Identifier: MPL-2.0

#nullable enable

using System.Collections.Generic;
using System.IO;
using nadena.dev.ndmf;
using UniGLTF.MeshUtility;
using UnityEngine;
using XRift.VrmExporter.Shared.Core;
using XRift.VrmExporter.Shared.Utils;
using XRift.VrmExporter.Platforms.VRM.Components;
using XRift.VrmExporter.Platforms.VRM.Converters;

[assembly: ExportsPlugin(typeof(XRift.VrmExporter.Platforms.VRM.Core.XRiftVrmPlugin))]

namespace XRift.VrmExporter.Platforms.VRM.Core
{
    /// <summary>
    /// XRift VRM Exporter の NDMF プラグイン
    /// VRM 1.0 エクスポートのためのビルドパスを登録する
    /// </summary>
#if XRIFT_HAS_NDMF_PLATFORM
    [RunsOnPlatforms(XRiftVrmPlatform.PlatformName)]
#endif
    internal class XRiftVrmPlugin : Plugin<XRiftVrmPlugin>
    {
        public override string DisplayName => "XRift VRM Exporter";
        public override string QualifiedName => "com.halby24.xrift-vrm-exporter.vrm";

        protected override void Configure()
        {
            // Transforming フェーズでArmature回転ベイクを実行
            InPhase(BuildPhase.Transforming)
                .Run(XRiftArmatureRotationBakePass.Instance);

            // Optimizing フェーズでPhysBone変換→VRMエクスポート処理を実行
            // 他の最適化プラグイン（AAO, MA）の後に実行
            // ※ Vrm10InstanceをAAOより前に追加するとNullReferenceExceptionが発生するため
            InPhase(BuildPhase.Optimizing)
                .AfterPlugin("com.anatawa12.avatar-optimizer")
                .AfterPlugin("nadena.dev.modular-avatar")
                .Run(XRiftPhysBoneConvertPass.Instance)
                .Then.Run(XRiftVrmExportPass.Instance);
        }
    }

    /// <summary>
    /// Armature回転をメッシュにベイクするパス
    /// </summary>
    internal class XRiftArmatureRotationBakePass : Pass<XRiftArmatureRotationBakePass>
    {
        public override string QualifiedName => "com.halby24.xrift-vrm-exporter.vrm.armature-rotation-bake";
        public override string DisplayName => "XRift Armature Rotation Bake";

        protected override void Execute(BuildContext context)
        {
            var state = context.GetState<XRiftBuildState>();
            if (!state.ExportEnabled) return;

            var root = context.AvatarRootObject;

            // ヒエラルキー内に回転を持つTransformがあるかチェック
            bool hasRotation = false;
            foreach (var transform in root.GetComponentsInChildren<Transform>())
            {
                if (transform.localRotation != Quaternion.identity)
                {
                    hasRotation = true;
                    break;
                }
            }

            if (!hasRotation) return;

            // UniGLTFのBoneNormalizerを使用してメッシュに回転をベイク
            var meshMap = BoneNormalizer.NormalizeHierarchyFreezeMesh(root, bakeCurrentBlendShape: false);
            BoneNormalizer.Replace(root, meshMap, KeepRotation: false);
        }
    }

    /// <summary>
    /// PhysBone → SpringBone 変換を実行するパス
    /// </summary>
    internal class XRiftPhysBoneConvertPass : Pass<XRiftPhysBoneConvertPass>
    {
        public override string QualifiedName => "com.halby24.xrift-vrm-exporter.vrm.physbone-convert";
        public override string DisplayName => "XRift PhysBone → SpringBone Convert";

        protected override void Execute(BuildContext context)
        {
            var state = context.GetState<XRiftBuildState>();
            if (!state.ExportEnabled)
            {
                return;
            }

            var gameObject = context.AvatarRootObject;

            // 除外設定を取得
            var descriptor = gameObject.GetComponent<XRiftVrmDescriptor>();
            var excludedColliders = descriptor?.GetExcludedSpringBoneColliderTransforms()
                ?? new HashSet<Transform>();
            var excludedBones = descriptor?.GetExcludedSpringBoneTransforms()
                ?? new HashSet<Transform>();

            // PhysBone → SpringBone 変換を実行
            PhysBoneToSpringBoneConverter.Convert(gameObject, excludedColliders, excludedBones);
        }
    }

    /// <summary>
    /// VRM エクスポートを実行するパス
    /// </summary>
    internal class XRiftVrmExportPass : Pass<XRiftVrmExportPass>
    {
        public override string QualifiedName => "com.halby24.xrift-vrm-exporter.vrm.export";
        public override string DisplayName => "XRift VRM Export";

        protected override void Execute(BuildContext context)
        {
            var state = context.GetState<XRiftBuildState>();
            if (!state.ExportEnabled)
            {
                return;
            }

            var gameObject = context.AvatarRootObject;
            var basePath = AssetPathUtils.GetTempPath(gameObject);
            var assetSaver = new TempAssetSaver(basePath);

            using var exporter = new XRiftVrmExporter(gameObject, assetSaver, state.MaterialVariants);
            using var memoryStream = new MemoryStream();

            exporter.Export(memoryStream);
            state.ExportedData = memoryStream.ToArray();

            Debug.Log($"[XRift VRM Exporter] Exported VRM: {state.ExportedData.Length} bytes");
        }
    }

    /// <summary>
    /// 一時ディレクトリへのアセット保存実装
    /// </summary>
    internal class TempAssetSaver : IAssetSaver
    {
        private readonly string _basePath;
        private readonly Dictionary<string, UnityEngine.Object> _assets = new();

        public TempAssetSaver(string basePath)
        {
            _basePath = basePath;
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public void SaveAsset(UnityEngine.Object asset, string name)
        {
            var path = Path.Combine(_basePath, name);
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            _assets[name] = asset;
        }

        public T? GetSavedAsset<T>(string name) where T : UnityEngine.Object
        {
            return _assets.TryGetValue(name, out var asset) ? asset as T : null;
        }
    }
}
