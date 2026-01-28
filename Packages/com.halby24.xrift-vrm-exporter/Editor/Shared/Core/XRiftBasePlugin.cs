// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-License-Identifier: MPL-2.0

#nullable enable

using nadena.dev.ndmf;

namespace XRift.VrmExporter.Shared.Core
{
    /// <summary>
    /// XRift プラグイン基底クラス
    /// プラグイン登録の共通パターンを提供する
    /// </summary>
    /// <typeparam name="T">プラグイン実装クラス</typeparam>
    public abstract class XRiftBasePlugin<T> : Plugin<T> where T : Plugin<T>, new()
    {
        // 共通のプラグイン機能をここに実装可能
    }
}
