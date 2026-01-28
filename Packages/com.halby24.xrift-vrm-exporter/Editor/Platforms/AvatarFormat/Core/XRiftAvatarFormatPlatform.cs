#nullable enable
#if XRIFT_HAS_NDMF_PLATFORM
using nadena.dev.ndmf.platform;
using UnityEngine;
using XRift.VrmExporter.Shared.Core;
using XRift.VrmExporter.Platforms.AvatarFormat.UI;

namespace XRift.VrmExporter.Platforms.AvatarFormat.Core
{
    [NDMFPlatformProvider]
    internal class XRiftAvatarFormatPlatform : XRiftBasePlatform
    {
        public const string PlatformName = "com.halby24.xrift-vrm-exporter.avatarformat";
        public static readonly INDMFPlatformProvider Instance = new XRiftAvatarFormatPlatform();

        public override string QualifiedName => PlatformName;
        public override string DisplayName => "XRift Avatar Format";

        public override BuildUIElement? CreateBuildUI()
        {
            return new XRiftAvatarFormatBuildUI();
        }
    }
}
#endif
