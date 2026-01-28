#nullable enable

#if XRIFT_HAS_NDMF_PLATFORM
using nadena.dev.ndmf.platform;
using UnityEngine;
using XRift.VrmExporter.Shared.Core;
using XRift.VrmExporter.Platforms.VRM.UI;

namespace XRift.VrmExporter.Platforms.VRM.Core
{
    /// <summary>
    /// XRift VRM プラットフォームプロバイダー
    /// NDMF のプラットフォーム機能を使用してビルドUIを提供する
    /// </summary>
    [NDMFPlatformProvider]
    internal class XRiftVrmPlatform : XRiftBasePlatform
    {
        /// <summary>
        /// プラットフォーム名（他のプラグインから参照される識別子）
        /// </summary>
        public const string PlatformName = "com.halby24.xrift-vrm-exporter.vrm";

        public static readonly INDMFPlatformProvider Instance = new XRiftVrmPlatform();

        public override string QualifiedName => PlatformName;
        public override string DisplayName => "XRift VRM";

        /// <summary>
        /// ビルドUIを作成する
        /// </summary>
        public override BuildUIElement? CreateBuildUI()
        {
            return new XRiftVrmBuildUI();
        }
    }
}
#endif
