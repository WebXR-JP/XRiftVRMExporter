#nullable enable
using nadena.dev.ndmf;
using UnityEngine;
using XRift.VrmExporter.Shared.Core;

[assembly: ExportsPlugin(typeof(XRift.VrmExporter.Platforms.AvatarFormat.Core.XRiftAvatarFormatPlugin))]

namespace XRift.VrmExporter.Platforms.AvatarFormat.Core
{
#if XRIFT_HAS_NDMF_PLATFORM
    [RunsOnPlatforms(XRiftAvatarFormatPlatform.PlatformName)]
#endif
    internal class XRiftAvatarFormatPlugin : Plugin<XRiftAvatarFormatPlugin>
    {
        public override string QualifiedName => "com.halby24.xrift-vrm-exporter.avatarformat";
        public override string DisplayName => "XRift Avatar Format Exporter";

        protected override void Configure()
        {
            // スタブ実装: 最小限のエクスポートパス
            InPhase(BuildPhase.Optimizing)
                .Run(XRiftAvatarFormatExportPass.Instance);
        }
    }

    internal class XRiftAvatarFormatExportPass : Pass<XRiftAvatarFormatExportPass>
    {
        public override string QualifiedName => "com.halby24.xrift-vrm-exporter.avatarformat.export";
        public override string DisplayName => "XRift Avatar Format Export";

        protected override void Execute(BuildContext context)
        {
            var state = context.GetState<XRiftBuildState>();
            if (!state.ExportEnabled) return;

            // TODO: 実装予定
            Debug.Log("[XRift Avatar Format] Export pass (stub)");
        }
    }
}
