// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-License-Identifier: MPL-2.0

#nullable enable

using UniGLTF;
using UniVRM10;
using MToonExtension = UniGLTF.Extensions.VRMC_materials_mtoon;
using UnityEngine;
#if XRIFT_HAS_LILTOON
using lilToon;
#endif

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
    /// lilToon公式のCreateMToonMaterialをベースに、MToon10（VRM1.0）向けに変換
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

            // === ベースカラー ===
            ExportBaseColor(src, textureExporter, dst, mtoon);

            // === アルファモード ===
            dst.alphaMode = GetAlphaMode(src);
            if (dst.alphaMode == "MASK" && src.HasProperty("_Cutoff"))
            {
                dst.alphaCutoff = src.GetFloat("_Cutoff");
            }

            // === 両面描画 ===
            // lilToonの_Cullプロパティ: 0=Off(両面), 1=Front, 2=Back
            if (src.HasProperty("_Cull"))
            {
                dst.doubleSided = src.GetFloat("_Cull") == 0;
            }

            // === シャドウ（影） ===
            ExportShadow(src, textureExporter, dst, mtoon);

            // === 法線マップ ===
            ExportNormalMap(src, textureExporter, dst);

            // === エミッション ===
            ExportEmission(src, textureExporter, dst);

            // === リムライト ===
            ExportRimLight(src, mtoon);

            // === MatCap ===
            ExportMatCap(src, textureExporter, mtoon);

            // === アウトライン ===
            ExportOutline(src, textureExporter, mtoon);

            // === GI ===
            mtoon.GiEqualizationFactor = 0.9f;

            // MToon拡張をシリアライズ
            MToonExtension.GltfSerializer.SerializeTo(ref dst.extensions, mtoon);

            return dst;
        }

        /// <summary>
        /// ベースカラーとテクスチャをエクスポート
        /// </summary>
        private void ExportBaseColor(Material src, ITextureExporter textureExporter,
            glTFMaterial dst, MToonExtension.VRMC_materials_mtoon mtoon)
        {
            // ベースカラー
            if (src.HasProperty("_Color"))
            {
                var color = src.GetColor("_Color");
                // sRGB → Linear変換
                dst.pbrMetallicRoughness.baseColorFactor = new[]
                {
                    Mathf.GammaToLinearSpace(Mathf.Clamp01(color.r)),
                    Mathf.GammaToLinearSpace(Mathf.Clamp01(color.g)),
                    Mathf.GammaToLinearSpace(Mathf.Clamp01(color.b)),
                    Mathf.Clamp01(color.a)
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
        }

        /// <summary>
        /// シャドウ（影色）をエクスポート
        /// lilToon公式の変換ロジックをベースに実装
        /// </summary>
        private void ExportShadow(Material src, ITextureExporter textureExporter,
            glTFMaterial dst, MToonExtension.VRMC_materials_mtoon mtoon)
        {
            // _UseShadow が有効かチェック
            var useShadow = src.HasProperty("_UseShadow") && src.GetFloat("_UseShadow") == 1.0f;

            if (useShadow)
            {
                // シャドウボーダーとブラーからMToon10のパラメータを計算
                // lilToon公式の変換式:
                // shadeShift = (Clamp01(shadowBorder - shadowBlur*0.5) * 2.0) - 1.0
                // shadeToony = shadeShift == 1 ? 1 : (2 - Clamp01(shadowBorder + shadowBlur*0.5)*2) / (1 - shadeShift)
                var shadowBorder = src.HasProperty("_ShadowBorder") ? src.GetFloat("_ShadowBorder") : 0.5f;
                var shadowBlur = src.HasProperty("_ShadowBlur") ? src.GetFloat("_ShadowBlur") : 0.1f;

                var shadeShift = (Mathf.Clamp01(shadowBorder - shadowBlur * 0.5f) * 2.0f) - 1.0f;
                var shadeToony = Mathf.Approximately(shadeShift, 1.0f)
                    ? 1.0f
                    : (2.0f - Mathf.Clamp01(shadowBorder + shadowBlur * 0.5f) * 2.0f) / (1.0f - shadeShift);

                mtoon.ShadingShiftFactor = shadeShift;
                mtoon.ShadingToonyFactor = Mathf.Clamp01(shadeToony);

                // シャドウカラー
                if (src.HasProperty("_ShadowColor"))
                {
                    var shadowColor = src.GetColor("_ShadowColor");
                    var shadowStrength = src.HasProperty("_ShadowStrength")
                        ? src.GetFloat("_ShadowStrength")
                        : 1.0f;

                    // 影の強さを適用した色を計算
                    var shadeColor = new Color(
                        1.0f - (1.0f - shadowColor.r) * shadowStrength,
                        1.0f - (1.0f - shadowColor.g) * shadowStrength,
                        1.0f - (1.0f - shadowColor.b) * shadowStrength,
                        1.0f
                    );

                    mtoon.ShadeColorFactor = new[]
                    {
                        Mathf.GammaToLinearSpace(shadeColor.r),
                        Mathf.GammaToLinearSpace(shadeColor.g),
                        Mathf.GammaToLinearSpace(shadeColor.b)
                    };
                }
                else
                {
                    // デフォルトのシェードカラー（やや暗めの白）
                    mtoon.ShadeColorFactor = new[] { 0.8f, 0.8f, 0.8f };
                }

                // シャドウテクスチャ
                if (src.HasProperty("_ShadowColorTex"))
                {
                    var shadowTex = src.GetTexture("_ShadowColorTex");
                    if (shadowTex != null)
                    {
                        var textureIndex = textureExporter.RegisterExportingAsSRgb(shadowTex, needsAlpha: false);
                        if (textureIndex != -1)
                        {
                            mtoon.ShadeMultiplyTexture = new MToonExtension.TextureInfo
                            {
                                Index = textureIndex
                            };
                        }
                    }
                }
            }
            else
            {
                // シャドウ無効時はデフォルト値
                mtoon.ShadeColorFactor = new[] { 1.0f, 1.0f, 1.0f };
                mtoon.ShadingShiftFactor = 0.0f;
                mtoon.ShadingToonyFactor = 0.9f;
            }
        }

        /// <summary>
        /// 法線マップをエクスポート
        /// </summary>
        private void ExportNormalMap(Material src, ITextureExporter textureExporter, glTFMaterial dst)
        {
            var useBumpMap = src.HasProperty("_UseBumpMap") && src.GetFloat("_UseBumpMap") == 1.0f;
            if (!useBumpMap) return;

            if (src.HasProperty("_BumpMap"))
            {
                var bumpMap = src.GetTexture("_BumpMap");
                if (bumpMap != null)
                {
                    var textureIndex = textureExporter.RegisterExportingAsNormal(bumpMap);
                    if (textureIndex != -1)
                    {
                        dst.normalTexture = new glTFMaterialNormalTextureInfo
                        {
                            index = textureIndex,
                            scale = src.HasProperty("_BumpScale") ? src.GetFloat("_BumpScale") : 1.0f
                        };
                    }
                }
            }
        }

        /// <summary>
        /// エミッションをエクスポート
        /// </summary>
        private void ExportEmission(Material src, ITextureExporter textureExporter, glTFMaterial dst)
        {
            var useEmission = src.HasProperty("_UseEmission") && src.GetFloat("_UseEmission") == 1.0f;
            if (!useEmission) return;

            if (src.HasProperty("_EmissionColor"))
            {
                var emissionColor = src.GetColor("_EmissionColor");
                dst.emissiveFactor = new[]
                {
                    Mathf.GammaToLinearSpace(emissionColor.r),
                    Mathf.GammaToLinearSpace(emissionColor.g),
                    Mathf.GammaToLinearSpace(emissionColor.b)
                };
            }

            if (src.HasProperty("_EmissionMap"))
            {
                var emissionMap = src.GetTexture("_EmissionMap");
                if (emissionMap != null)
                {
                    var textureIndex = textureExporter.RegisterExportingAsSRgb(emissionMap, needsAlpha: false);
                    if (textureIndex != -1)
                    {
                        dst.emissiveTexture = new glTFMaterialEmissiveTextureInfo
                        {
                            index = textureIndex
                        };
                    }
                }
            }
        }

        /// <summary>
        /// リムライトをエクスポート
        /// </summary>
        private void ExportRimLight(Material src, MToonExtension.VRMC_materials_mtoon mtoon)
        {
            var useRim = src.HasProperty("_UseRim") && src.GetFloat("_UseRim") == 1.0f;
            if (!useRim)
            {
                // リム無効時は黒
                mtoon.ParametricRimColorFactor = new[] { 0f, 0f, 0f };
                return;
            }

            if (src.HasProperty("_RimColor"))
            {
                var rimColor = src.GetColor("_RimColor");
                mtoon.ParametricRimColorFactor = new[]
                {
                    Mathf.GammaToLinearSpace(rimColor.r),
                    Mathf.GammaToLinearSpace(rimColor.g),
                    Mathf.GammaToLinearSpace(rimColor.b)
                };
            }

            // lilToon公式の変換式:
            // rimFP = rimFresnelPower / max(0.001, rimBlur)
            // rimLift = pow(1 - rimBorder, rimFresnelPower) * (1 - rimBlur)
            var rimFresnelPower = src.HasProperty("_RimFresnelPower") ? src.GetFloat("_RimFresnelPower") : 3.0f;
            var rimBlur = src.HasProperty("_RimBlur") ? src.GetFloat("_RimBlur") : 0.1f;
            var rimBorder = src.HasProperty("_RimBorder") ? src.GetFloat("_RimBorder") : 0.5f;

            mtoon.ParametricRimFresnelPowerFactor = rimFresnelPower / Mathf.Max(0.001f, rimBlur);
            mtoon.ParametricRimLiftFactor = Mathf.Pow(1.0f - rimBorder, rimFresnelPower) * (1.0f - rimBlur);

            // リムライトのライティングミックス
            if (src.HasProperty("_RimEnableLighting"))
            {
                mtoon.RimLightingMixFactor = src.GetFloat("_RimEnableLighting");
            }
        }

        /// <summary>
        /// MatCapをエクスポート
        /// </summary>
        private void ExportMatCap(Material src, ITextureExporter textureExporter,
            MToonExtension.VRMC_materials_mtoon mtoon)
        {
            var useMatCap = src.HasProperty("_UseMatCap") && src.GetFloat("_UseMatCap") == 1.0f;
            if (!useMatCap)
            {
                mtoon.MatcapFactor = new[] { 0f, 0f, 0f };
                return;
            }

            // MatCapブレンドモードが乗算(3)の場合はMToon10では表現できないためスキップ
            if (src.HasProperty("_MatCapBlendMode") && src.GetFloat("_MatCapBlendMode") == 3.0f)
            {
                mtoon.MatcapFactor = new[] { 0f, 0f, 0f };
                return;
            }

            // MatCapテクスチャ
            if (src.HasProperty("_MatCapTex"))
            {
                var matcapTex = src.GetTexture("_MatCapTex");
                if (matcapTex != null)
                {
                    var textureIndex = textureExporter.RegisterExportingAsSRgb(matcapTex, needsAlpha: false);
                    if (textureIndex != -1)
                    {
                        mtoon.MatcapTexture = new MToonExtension.TextureInfo
                        {
                            Index = textureIndex
                        };
                    }
                }
            }

            // MatCapカラー
            if (src.HasProperty("_MatCapColor"))
            {
                var matcapColor = src.GetColor("_MatCapColor");
                mtoon.MatcapFactor = new[]
                {
                    Mathf.GammaToLinearSpace(matcapColor.r),
                    Mathf.GammaToLinearSpace(matcapColor.g),
                    Mathf.GammaToLinearSpace(matcapColor.b)
                };
            }
            else
            {
                mtoon.MatcapFactor = new[] { 1f, 1f, 1f };
            }
        }

        /// <summary>
        /// アウトラインをエクスポート
        /// </summary>
        private void ExportOutline(Material src, ITextureExporter textureExporter,
            MToonExtension.VRMC_materials_mtoon mtoon)
        {
            // lilToonのユーティリティでアウトラインシェーダーか判定
#if XRIFT_HAS_LILTOON
            var isOutline = lilShaderUtils.IsOutlineShaderName(src.shader.name);
#else
            var isOutline = src.shader.name.Contains("Outline");
#endif

            if (!isOutline)
            {
                mtoon.OutlineWidthMode = MToonExtension.OutlineWidthMode.none;
                return;
            }

            // アウトライン幅
            if (src.HasProperty("_OutlineWidth"))
            {
                var outlineWidth = src.GetFloat("_OutlineWidth");
                if (outlineWidth > 0)
                {
                    // ワールド座標モードで出力
                    mtoon.OutlineWidthMode = MToonExtension.OutlineWidthMode.worldCoordinates;
                    mtoon.OutlineWidthFactor = outlineWidth;

                    // アウトラインカラー
                    if (src.HasProperty("_OutlineColor"))
                    {
                        var outlineColor = src.GetColor("_OutlineColor");
                        mtoon.OutlineColorFactor = new[]
                        {
                            Mathf.GammaToLinearSpace(outlineColor.r),
                            Mathf.GammaToLinearSpace(outlineColor.g),
                            Mathf.GammaToLinearSpace(outlineColor.b)
                        };
                    }

                    // アウトライン幅マスク
                    if (src.HasProperty("_OutlineWidthMask"))
                    {
                        var outlineWidthMask = src.GetTexture("_OutlineWidthMask");
                        if (outlineWidthMask != null)
                        {
                            var textureIndex = textureExporter.RegisterExportingAsLinear(
                                outlineWidthMask, needsAlpha: false);
                            if (textureIndex != -1)
                            {
                                mtoon.OutlineWidthMultiplyTexture = new MToonExtension.TextureInfo
                                {
                                    Index = textureIndex
                                };
                            }
                        }
                    }

                    // ライティングミックス（lilToonはデフォルト1.0）
                    mtoon.OutlineLightingMixFactor = 1.0f;
                }
                else
                {
                    mtoon.OutlineWidthMode = MToonExtension.OutlineWidthMode.none;
                }
            }
            else
            {
                mtoon.OutlineWidthMode = MToonExtension.OutlineWidthMode.none;
            }
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
