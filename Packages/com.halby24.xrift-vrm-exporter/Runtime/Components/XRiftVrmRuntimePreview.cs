// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-License-Identifier: MPL-2.0

#nullable enable

using nadena.dev.ndmf;
using UnityEngine;

namespace XRift.VrmExporter.Components
{
    /// <summary>
    /// VRM ランタイムプレビュー マーカーコンポーネント
    /// このコンポーネントがついたアバターは、Playモード時にVRM変換→ロード→置換が行われる
    /// </summary>
    [AddComponentMenu("XRift VRM Exporter/VRM Runtime Preview")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/halby24/xrift-vrm-exporter")]
    public sealed class XRiftVrmRuntimePreview : MonoBehaviour, INDMFEditorOnly
    {
        // マーカーコンポーネントのため、フィールドは不要

        // ReSharper disable once Unity.RedundantEventFunction
        private void Start()
        {
            // 何もしないがチェックボックス表示のために必要
        }
    }
}
