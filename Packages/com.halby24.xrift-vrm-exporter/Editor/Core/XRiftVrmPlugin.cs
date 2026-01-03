// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-License-Identifier: MPL-2.0

#nullable enable

using System.Collections.Generic;
using System.IO;
using nadena.dev.ndmf;
using UniVRM10;
using UnityEngine;
using XRift.VrmExporter.Components;
using XRift.VrmExporter.Converters;
using XRift.VrmExporter.Core;
using XRift.VrmExporter.Utils;

[assembly: ExportsPlugin(typeof(XRiftVrmPlugin))]

namespace XRift.VrmExporter.Core
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
        public override string QualifiedName => "com.halby24.xrift-vrm-exporter";

        protected override void Configure()
        {
            // Transforming フェーズで PhysBone → SpringBone 変換を実行
            InPhase(BuildPhase.Transforming)
                .Run(XRiftPhysBoneConvertPass.Instance);

            // Optimizing フェーズでVRMエクスポート処理を実行
            // その後、ランタイムプレビュー用のVRMロード処理を実行
            InPhase(BuildPhase.Optimizing)
                .Run(XRiftVrmExportPass.Instance)
                .Then.Run(XRiftVrmRuntimePreviewPass.Instance);
        }
    }

    /// <summary>
    /// PhysBone → SpringBone 変換を実行するパス
    /// </summary>
    internal class XRiftPhysBoneConvertPass : Pass<XRiftPhysBoneConvertPass>
    {
        public override string QualifiedName => "com.halby24.xrift-vrm-exporter.physbone-convert";
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
        public override string QualifiedName => "com.halby24.xrift-vrm-exporter.export";
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
            state.ExportedVrmData = memoryStream.ToArray();

            Debug.Log($"[XRift VRM Exporter] Exported VRM: {state.ExportedVrmData.Length} bytes");
        }
    }

    /// <summary>
    /// Playモード時にVRMをロードして元アバターを置き換えるパス
    /// </summary>
    internal class XRiftVrmRuntimePreviewPass : Pass<XRiftVrmRuntimePreviewPass>
    {
        public override string QualifiedName => "com.halby24.xrift-vrm-exporter.runtime-preview";
        public override string DisplayName => "XRift VRM Runtime Preview";

        protected override void Execute(BuildContext context)
        {
            // Playモード時のみ実行
            if (!Application.isPlaying)
            {
                return;
            }

            // XRiftVrmRuntimePreviewコンポーネントがなければスキップ
            var preview = context.AvatarRootObject.GetComponent<XRiftVrmRuntimePreview>();
            if (preview == null)
            {
                return;
            }

            // VRMデータ取得
            var state = context.GetState<XRiftBuildState>();
            if (state.ExportedVrmData == null)
            {
                Debug.LogWarning("[XRift VRM Exporter] Runtime Preview: No VRM data available");
                return;
            }

            // VRMロード＆置換を開始
            LoadAndReplaceAvatar(context.AvatarRootObject, state.ExportedVrmData);
        }

        private static async void LoadAndReplaceAvatar(GameObject original, byte[] vrmData)
        {
            try
            {
                // VRMをロード
                var instance = await Vrm10.LoadBytesAsync(vrmData, showMeshes: true);
                if (instance == null)
                {
                    Debug.LogError("[XRift VRM Exporter] Runtime Preview: Failed to load VRM");
                    return;
                }

                // 元アバターが既に破棄されている場合はスキップ
                if (original == null)
                {
                    Object.Destroy(instance.gameObject);
                    return;
                }

                // 位置を合わせる
                var vrmRoot = instance.gameObject;
                vrmRoot.transform.SetParent(original.transform.parent, false);
                vrmRoot.transform.SetPositionAndRotation(
                    original.transform.position,
                    original.transform.rotation);
                vrmRoot.transform.localScale = original.transform.localScale;

                // 元アバターを非表示
                foreach (var renderer in original.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = false;
                }

                var animator = original.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                }

                Debug.Log($"[XRift VRM Exporter] Runtime Preview: Loaded VRM '{vrmRoot.name}'");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[XRift VRM Exporter] Runtime Preview: Error loading VRM - {ex.Message}");
            }
        }
    }
}
