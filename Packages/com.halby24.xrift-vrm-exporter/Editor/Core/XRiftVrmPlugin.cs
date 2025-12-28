#nullable enable

using nadena.dev.ndmf;
using XRift.VrmExporter.Core;

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
            // TODO: VRM エクスポート処理を実装
            // 現時点では何もしない（スケルトン実装）
        }
    }
}
