// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-License-Identifier: MPL-2.0

#nullable enable

using System.IO;
using nadena.dev.ndmf;
using UnityEngine;
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
            // ビルドフェーズの設定
            // Optimizing フェーズでVRMエクスポート処理を実行
            InPhase(BuildPhase.Optimizing)
                .Run(XRiftVrmExportPass.Instance);
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
}
