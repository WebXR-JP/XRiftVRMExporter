// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-License-Identifier: MPL-2.0

#nullable enable

using UniGLTF;
using UniVRM10;
using UnityEngine;

namespace XRift.VrmExporter.Core
{
    /// <summary>
    /// XRift VRM Exporter用のカスタムマテリアルエクスポーター
    /// lilToon等の非標準シェーダーをMToon10に変換してエクスポート
    /// </summary>
    internal sealed class XRiftMaterialExporter : IMaterialExporter
    {
        private readonly BuiltInVrm10MaterialExporter _baseExporter;
        private readonly XRiftLilToonToMToonConverter _lilToonConverter;

        public XRiftMaterialExporter()
        {
            _baseExporter = new BuiltInVrm10MaterialExporter();
            _lilToonConverter = new XRiftLilToonToMToonConverter();
        }

        public glTFMaterial ExportMaterial(Material m, ITextureExporter textureExporter, GltfExportSettings settings)
        {
            // lilToonマテリアルの場合は変換してエクスポート
            if (_lilToonConverter.TryExportMaterial(m, textureExporter, out var lilToonResult))
            {
                return lilToonResult;
            }

            // それ以外は標準のVRM10エクスポーターに任せる
            return _baseExporter.ExportMaterial(m, textureExporter, settings);
        }
    }

    /// <summary>
    /// lilToonマテリアルをMToon10形式に変換するコンバーター
    /// </summary>
    internal sealed class XRiftLilToonToMToonConverter
    {
        private const string LilToonShaderName = "lilToon";

        public bool TryExportMaterial(Material src, ITextureExporter textureExporter, out glTFMaterial dst)
        {
            // lilToonシェーダーでない場合は変換しない
            if (src.shader == null || !src.shader.name.Contains(LilToonShaderName))
            {
                dst = null!;
                return false;
            }

            // TODO: lilToon → MToon10 プロパティマッピング実装
            // 現時点では基本的なPBRマテリアルとしてエクスポート
            dst = CreateBasicMaterial(src, textureExporter);
            return true;
        }

        private glTFMaterial CreateBasicMaterial(Material src, ITextureExporter textureExporter)
        {
            var dst = glTF_KHR_materials_unlit.CreateDefault();
            dst.name = src.name;

            // 基本カラー
            dst.pbrMetallicRoughness = new glTFPbrMetallicRoughness();

            // lilToonのメインカラーを取得
            if (src.HasProperty("_Color"))
            {
                var color = src.GetColor("_Color");
                dst.pbrMetallicRoughness.baseColorFactor = new[] { color.r, color.g, color.b, color.a };
            }

            // lilToonのメインテクスチャを取得
            if (src.HasProperty("_MainTex"))
            {
                var mainTex = src.GetTexture("_MainTex");
                if (mainTex != null)
                {
                    var textureIndex = textureExporter.RegisterExportingAsSRgb(mainTex, needsAlpha: true);
                    if (textureIndex != -1)
                    {
                        dst.pbrMetallicRoughness.baseColorTexture = new glTFMaterialBaseColorTextureInfo
                        {
                            index = textureIndex,
                        };
                    }
                }
            }

            // アルファモード（lilToonの透明モードに応じて設定）
            if (src.HasProperty("_TransparentMode"))
            {
                var transparentMode = src.GetFloat("_TransparentMode");
                if (transparentMode > 0)
                {
                    dst.alphaMode = "BLEND";
                }
            }

            return dst;
        }
    }
}
