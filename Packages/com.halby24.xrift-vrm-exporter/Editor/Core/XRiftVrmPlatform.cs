#nullable enable

#if XRIFT_HAS_NDMF_PLATFORM
using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;
using UnityEngine;
using XRift.VrmExporter.UI;

namespace XRift.VrmExporter.Core
{
    /// <summary>
    /// XRift VRM プラットフォームプロバイダー
    /// NDMF のプラットフォーム機能を使用してビルドUIを提供する
    /// </summary>
    [NDMFPlatformProvider]
    internal class XRiftVrmPlatform : INDMFPlatformProvider
    {
        /// <summary>
        /// プラットフォーム名（他のプラグインから参照される識別子）
        /// </summary>
        public const string PlatformName = "com.halby24.xrift-vrm-exporter";

        public static readonly INDMFPlatformProvider Instance = new XRiftVrmPlatform();

        public string QualifiedName => PlatformName;
        public string DisplayName => "XRift VRM";
        public Texture2D? Icon => null; // TODO: アイコンを追加

        /// <summary>
        /// ビルドUIを作成する
        /// </summary>
        public BuildUIElement? CreateBuildUI()
        {
            return new XRiftBuildUI();
        }

        /// <summary>
        /// 共通アバター情報からビルドを初期化する
        /// </summary>
        public void InitBuildFromCommonAvatarInfo(BuildContext context, CommonAvatarInfo info)
        {
            var state = context.GetState<XRiftBuildState>();
            state.CommonAvatarInfo = info;
        }
    }
}
#endif
