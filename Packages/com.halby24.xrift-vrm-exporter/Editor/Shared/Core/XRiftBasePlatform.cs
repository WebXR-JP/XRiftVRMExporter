// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-License-Identifier: MPL-2.0

#nullable enable

#if XRIFT_HAS_NDMF_PLATFORM
using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;
using UnityEngine;

namespace XRift.VrmExporter.Shared.Core
{
    /// <summary>
    /// XRift プラットフォーム基底クラス
    /// NDMF のプラットフォーム機能を使用してビルドUIを提供する
    /// </summary>
    public abstract class XRiftBasePlatform : INDMFPlatformProvider
    {
        /// <summary>
        /// プラットフォーム名（他のプラグインから参照される識別子）
        /// </summary>
        public abstract string QualifiedName { get; }

        /// <summary>
        /// プラットフォームの表示名
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// プラットフォームアイコン
        /// </summary>
        public virtual Texture2D? Icon => null;

        /// <summary>
        /// ビルドUIを作成する
        /// </summary>
        public abstract BuildUIElement? CreateBuildUI();

        /// <summary>
        /// 共通アバター情報からビルドを初期化する
        /// </summary>
        public virtual void InitBuildFromCommonAvatarInfo(BuildContext context, CommonAvatarInfo info)
        {
            var state = context.GetState<XRiftBuildState>();
            state.CommonAvatarInfo = info;
        }
    }
}
#endif
