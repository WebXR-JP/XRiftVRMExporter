// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-FileCopyrightText: 2024-present hkrn (original ndmf-vrm-exporter)
// SPDX-License-Identifier: MPL-2.0
//
// This file contains code derived from ndmf-vrm-exporter
// https://github.com/hkrn/ndmf-vrm-exporter

#nullable enable

using UnityEngine;

namespace XRift.VrmExporter.Shared.Utils
{
    /// <summary>
    /// Renderer と Material の対応関係を保持するクラス
    /// </summary>
    public sealed class MaterialVariantMapping
    {
        public Renderer Renderer { get; set; } = null!;
        public Material[] Materials { get; set; } = { };
    }

    /// <summary>
    /// Material Variant（衣装切り替え等）を表すクラス
    /// </summary>
    public sealed class MaterialVariant
    {
        public string? Name { get; set; }
        public MaterialVariantMapping[] Mappings { get; set; } = { };
    }
}
