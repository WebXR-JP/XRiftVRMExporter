// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-License-Identifier: MPL-2.0

#nullable enable

using UniGLTF;
using UniVRM10;
using MToonExtension = UniGLTF.Extensions.VRMC_materials_mtoon;
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

            dst = CreateMToonMaterial(src, textureExporter);
            return true;
        }

        private glTFMaterial CreateMToonMaterial(Material src, ITextureExporter textureExporter)
        {
            // ベースマテリアル作成
            var dst = glTF_KHR_materials_unlit.CreateDefault();
            dst.name = src.name;

            // VRMC_materials_mtoon拡張を作成
            var mtoon = new MToonExtension.VRMC_materials_mtoon
            {
                SpecVersion = Vrm10Exporter.MTOON_SPEC_VERSION
            };

            // PBR設定
            dst.pbrMetallicRoughness = new glTFPbrMetallicRoughness();

            // ベースカラー
            if (src.HasProperty("_Color"))
            {
                var color = src.GetColor("_Color");
                // sRGB → Linear変換
                dst.pbrMetallicRoughness.baseColorFactor = new[]
                {
                    Mathf.GammaToLinearSpace(color.r),
                    Mathf.GammaToLinearSpace(color.g),
                    Mathf.GammaToLinearSpace(color.b),
                    color.a
                };
            }

            // メインテクスチャ
            if (src.HasProperty("_MainTex"))
            {
                var mainTex = src.GetTexture("_MainTex");
                if (mainTex != null)
                {
                    var needsAlpha = GetAlphaMode(src) != "OPAQUE";
                    var textureIndex = textureExporter.RegisterExportingAsSRgb(mainTex, needsAlpha);
                    if (textureIndex != -1)
                    {
                        dst.pbrMetallicRoughness.baseColorTexture = new glTFMaterialBaseColorTextureInfo
                        {
                            index = textureIndex,
                        };
                    }
                }
            }

            // アルファモード
            dst.alphaMode = GetAlphaMode(src);

            // シェードカラー（MToon10のデフォルト値を設定）
            // TODO: lilToonの2ndカラーからマッピング
            mtoon.ShadeColorFactor = new[] { 0.8f, 0.8f, 0.8f };

            // シェーディングパラメータ（MToon10のデフォルト値）
            mtoon.ShadingShiftFactor = 0.0f;
            mtoon.ShadingToonyFactor = 0.9f;

            // GI
            mtoon.GiEqualizationFactor = 0.9f;

            // アウトライン無効
            mtoon.OutlineWidthMode = MToonExtension.OutlineWidthMode.none;

            // MToon拡張をシリアライズ
            MToonExtension.GltfSerializer.SerializeTo(ref dst.extensions, mtoon);

            return dst;
        }

        private string GetAlphaMode(Material src)
        {
            if (!src.HasProperty("_TransparentMode"))
            {
                return "OPAQUE";
            }

            var transparentMode = (int)src.GetFloat("_TransparentMode");
            return transparentMode switch
            {
                0 => "OPAQUE",  // Opaque
                1 => "MASK",    // Cutout
                2 => "BLEND",   // Transparent
                _ => "OPAQUE"
            };
        }
    }
}
