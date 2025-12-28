#nullable enable

using System.Collections.Generic;
using XRift.VrmExporter.Utils;

#if XRIFT_HAS_NDMF_PLATFORM
using nadena.dev.ndmf.platform;
#endif

namespace XRift.VrmExporter.Core
{
    /// <summary>
    /// XRift VRM ビルド状態
    /// ビルドプロセス中に共有されるデータを保持する
    /// </summary>
    internal class XRiftBuildState
    {
#if XRIFT_HAS_NDMF_PLATFORM
        /// <summary>
        /// 共通アバター情報（ビューポイント、表情など）
        /// </summary>
        public CommonAvatarInfo? CommonAvatarInfo { get; set; }
#endif

        /// <summary>
        /// VRM 出力先パス
        /// </summary>
        public string? OutputPath { get; set; }

        /// <summary>
        /// エクスポートが有効かどうか
        /// </summary>
        public bool ExportEnabled { get; set; } = true;

        /// <summary>
        /// Material Variants（衣装切り替え等）
        /// </summary>
        public List<MaterialVariant> MaterialVariants { get; } = new();

        /// <summary>
        /// エクスポートされたVRMデータ
        /// </summary>
        public byte[]? ExportedVrmData { get; set; }
    }
}
