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

namespace XRift.VrmExporter.Platforms.VRM.Core
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

#if XRIFT_HAS_LILTOON
        private readonly LilToonTextureBaker _textureBaker = new();
#endif

        // ベイクされたテクスチャを一時保持（エクスポート完了後に破棄）
        private readonly System.Collections.Generic.List<Texture2D> _bakedTextures = new();

        /// <summary>
        /// 一時的にベイクしたテクスチャをすべて破棄
        /// </summary>
        public void Cleanup()
        {
            foreach (var tex in _bakedTextures)
            {
                if (tex != null)
                {
                    Object.DestroyImmediate(tex);
                }
            }
            _bakedTextures.Clear();
        }

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

            // === 乗算MatCapの色を取得（BaseColor/ShadeColorに適用するため先に計算） ===
            var (matcapBaseColor, matcapShadeColor) = GetMatCapMultiplyColors(src);

            // === ベースカラー ===
            // ベイクされたメインテクスチャを取得（シャドウのベイクに使用）
            ExportBaseColor(src, textureExporter, dst, mtoon, matcapBaseColor, out var bakedMainTex);

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
            ExportShadow(src, textureExporter, dst, mtoon, bakedMainTex, matcapShadeColor);

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

            // === TransparentWithZWrite ===
            // Transparent + Outlineの場合、ZWriteを有効にして描画順の問題を回避
            if (dst.alphaMode == "BLEND" && mtoon.OutlineWidthMode != MToonExtension.OutlineWidthMode.none)
            {
                mtoon.TransparentWithZWrite = true;
            }

            // === GI ===
            mtoon.GiEqualizationFactor = 0.9f;

            // MToon拡張をシリアライズ
            MToonExtension.GltfSerializer.SerializeTo(ref dst.extensions, mtoon);

            return dst;
        }

        /// <summary>
        /// ベースカラーとテクスチャをエクスポート
        /// lilToonのテクスチャベイク処理で2nd/3rdレイヤー、HSVG調整等を合成
        /// </summary>
        private void ExportBaseColor(Material src, ITextureExporter textureExporter,
            glTFMaterial dst, MToonExtension.VRMC_materials_mtoon mtoon, Color matcapMultiplyColor, out Texture? bakedMainTex)
        {
            bakedMainTex = null;

            // ベースカラー
            if (src.HasProperty("_Color"))
            {
                var color = src.GetColor("_Color");
                // 乗算MatCapの平均色を適用
                color.r *= matcapMultiplyColor.r;
                color.g *= matcapMultiplyColor.g;
                color.b *= matcapMultiplyColor.b;
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
                    Texture textureToExport = mainTex;

#if XRIFT_HAS_LILTOON
                    // ベイクが必要な場合はベイクしたテクスチャを使用
                    if (_textureBaker.IsAvailable)
                    {
                        var bakedTex = _textureBaker.BakeMainTexture(src);
                        if (bakedTex != null)
                        {
                            _bakedTextures.Add(bakedTex);
                            textureToExport = bakedTex;
                            bakedMainTex = bakedTex;
                        }
                    }
#endif

                    var needsAlpha = GetAlphaMode(src) != "OPAQUE";
                    var textureIndex = textureExporter.RegisterExportingAsSRgb(textureToExport, needsAlpha);
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
            glTFMaterial dst, MToonExtension.VRMC_materials_mtoon mtoon, Texture? bakedMainTex, Color matcapShadeColor)
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

                // シャドウテクスチャ
                Texture? shadowTexToExport = null;
                var shadowBaked = false;

#if XRIFT_HAS_LILTOON
                // ベイクが必要な場合はベイクしたテクスチャを使用
                if (_textureBaker.IsAvailable)
                {
                    var bakedShadowTex = _textureBaker.BakeShadowTexture(src, bakedMainTex);
                    if (bakedShadowTex != null)
                    {
                        _bakedTextures.Add(bakedShadowTex);
                        shadowTexToExport = bakedShadowTex;
                        shadowBaked = true;
                    }
                }
#endif

                // ベイクされなかった場合は元のシャドウテクスチャを使用
                if (shadowTexToExport == null && src.HasProperty("_ShadowColorTex"))
                {
                    shadowTexToExport = src.GetTexture("_ShadowColorTex");
                }

                // シャドウカラー
                // ベイク済みの場合はカラーがテクスチャに含まれるので白（ただしmatcapShadeColorは適用）
                Color shadeColor;
                if (shadowBaked)
                {
                    shadeColor = Color.white;
                }
                else if (src.HasProperty("_ShadowColor"))
                {
                    var shadowColor = src.GetColor("_ShadowColor");
                    var shadowStrength = src.HasProperty("_ShadowStrength")
                        ? src.GetFloat("_ShadowStrength")
                        : 1.0f;

                    // 影の強さを適用した色を計算
                    shadeColor = new Color(
                        1.0f - (1.0f - shadowColor.r) * shadowStrength,
                        1.0f - (1.0f - shadowColor.g) * shadowStrength,
                        1.0f - (1.0f - shadowColor.b) * shadowStrength,
                        1.0f
                    );
                }
                else
                {
                    // デフォルトのシェードカラー（やや暗めの白）
                    shadeColor = new Color(0.8f, 0.8f, 0.8f, 1.0f);
                }

                // 乗算MatCapのシェードカラーを適用
                shadeColor.r *= matcapShadeColor.r;
                shadeColor.g *= matcapShadeColor.g;
                shadeColor.b *= matcapShadeColor.b;

                mtoon.ShadeColorFactor = new[]
                {
                    Mathf.GammaToLinearSpace(shadeColor.r),
                    Mathf.GammaToLinearSpace(shadeColor.g),
                    Mathf.GammaToLinearSpace(shadeColor.b)
                };

                // シャドウテクスチャをアサイン
                // シャドウテクスチャが空の場合はメインテクスチャと同じものを使用
                if (shadowTexToExport != null)
                {
                    var textureIndex = textureExporter.RegisterExportingAsSRgb(shadowTexToExport, needsAlpha: false);
                    if (textureIndex != -1)
                    {
                        mtoon.ShadeMultiplyTexture = new MToonExtension.TextureInfo
                        {
                            Index = textureIndex
                        };
                    }
                }
                else if (dst.pbrMetallicRoughness?.baseColorTexture != null)
                {
                    // シャドウテクスチャが無い場合、メインテクスチャと同じインデックスを使用
                    mtoon.ShadeMultiplyTexture = new MToonExtension.TextureInfo
                    {
                        Index = dst.pbrMetallicRoughness.baseColorTexture.index
                    };
                }
            }
            else
            {
                // シャドウ無効時でもメインテクスチャをシャドウテクスチャとしてアサイン
                // 乗算MatCapのシェードカラーを適用
                mtoon.ShadeColorFactor = new[]
                {
                    Mathf.GammaToLinearSpace(matcapShadeColor.r),
                    Mathf.GammaToLinearSpace(matcapShadeColor.g),
                    Mathf.GammaToLinearSpace(matcapShadeColor.b)
                };
                mtoon.ShadingShiftFactor = 0.0f;
                mtoon.ShadingToonyFactor = 0.9f;

                if (dst.pbrMetallicRoughness?.baseColorTexture != null)
                {
                    mtoon.ShadeMultiplyTexture = new MToonExtension.TextureInfo
                    {
                        Index = dst.pbrMetallicRoughness.baseColorTexture.index
                    };
                }
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
        /// lilToonのMatCapカラーをテクスチャにベイクして出力
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

            // MToonにはMatcapMask機能がないため、lilToonでマスクが設定されている場合はMatcap自体をスキップ
            if (src.HasProperty("_MatCapBlendMask"))
            {
                var matcapMask = src.GetTexture("_MatCapBlendMask");
                if (matcapMask != null)
                {
                    mtoon.MatcapFactor = new[] { 0f, 0f, 0f };
                    return;
                }
            }

            // lilToonのMatCapブレンドモード: 0=Normal, 1=Add, 2=Screen, 3=Multiply
            // 乗算モードはBaseColorに平均色を適用済みなので、MatCapとしてはスキップ
            var blendMode = src.HasProperty("_MatCapBlendMode") ? src.GetFloat("_MatCapBlendMode") : 0f;
            if (Mathf.Approximately(blendMode, 3.0f))
            {
                mtoon.MatcapFactor = new[] { 0f, 0f, 0f };
                return;
            }

            // 通常モード（Normal, Add, Screen）: 加算として処理
            ExportMatCapAdditiveMode(src, textureExporter, mtoon);
        }

        /// <summary>
        /// MatCap加算モード（Normal, Add, Screen）のエクスポート
        /// </summary>
        private void ExportMatCapAdditiveMode(Material src, ITextureExporter textureExporter,
            MToonExtension.VRMC_materials_mtoon mtoon)
        {
            if (!src.HasProperty("_MatCapTex")) return;

            var matcapTex = src.GetTexture("_MatCapTex");
            if (matcapTex == null) return;

            Texture textureToExport = matcapTex;

#if XRIFT_HAS_LILTOON
            // MatCapカラーをテクスチャにベイク
            if (_textureBaker.IsAvailable)
            {
                var bakedMatcapTex = _textureBaker.BakeMatCapTexture(src);
                if (bakedMatcapTex != null)
                {
                    _bakedTextures.Add(bakedMatcapTex);
                    textureToExport = bakedMatcapTex;
                }
            }
#endif

            var textureIndex = textureExporter.RegisterExportingAsSRgb(textureToExport, needsAlpha: false);
            if (textureIndex != -1)
            {
                mtoon.MatcapTexture = new MToonExtension.TextureInfo
                {
                    Index = textureIndex
                };
            }

            // MatCapカラーのアルファ値を取得（加算の強度として使用）
            var matcapAlpha = 1f;
            if (src.HasProperty("_MatCapColor"))
            {
                matcapAlpha = src.GetColor("_MatCapColor").a;
            }

#if XRIFT_HAS_LILTOON
            // ベイク時はRGBカラーがテクスチャに含まれるので、アルファのみファクターに反映
            mtoon.MatcapFactor = new[] { matcapAlpha, matcapAlpha, matcapAlpha };
#else
            if (src.HasProperty("_MatCapColor"))
            {
                var matcapColor = src.GetColor("_MatCapColor");
                // RGB × アルファで加算強度を調整
                mtoon.MatcapFactor = new[]
                {
                    Mathf.GammaToLinearSpace(matcapColor.r) * matcapAlpha,
                    Mathf.GammaToLinearSpace(matcapColor.g) * matcapAlpha,
                    Mathf.GammaToLinearSpace(matcapColor.b) * matcapAlpha
                };
            }
            else
            {
                mtoon.MatcapFactor = new[] { 1f, 1f, 1f };
            }
#endif
        }

        /// <summary>
        /// 乗算MatCapの色を取得（BaseColor/ShadeColorに適用するため）
        /// 乗算モードでない場合は白（効果なし）を返す
        /// </summary>
        private (Color baseColor, Color shadeColor) GetMatCapMultiplyColors(Material src)
        {
            var white = Color.white;
            var useMatCap = src.HasProperty("_UseMatCap") && src.GetFloat("_UseMatCap") == 1.0f;
            if (!useMatCap) return (white, white);

            // 乗算モードかチェック
            var blendMode = src.HasProperty("_MatCapBlendMode") ? src.GetFloat("_MatCapBlendMode") : 0f;
            if (!Mathf.Approximately(blendMode, 3.0f)) return (white, white);

            // マスクが設定されている場合はスキップ
            if (src.HasProperty("_MatCapBlendMask"))
            {
                var matcapMask = src.GetTexture("_MatCapBlendMask");
                if (matcapMask != null) return (white, white);
            }

#if XRIFT_HAS_LILTOON
            if (_textureBaker.IsAvailable)
            {
                return _textureBaker.GetMatCapMultiplyColors(src);
            }
#endif
            return (white, white);
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
                    // lilToonの_OutlineWidthはシェーダー内で0.01を掛けてメートルに変換している
                    // MToon10のOutlineWidthFactorはメートル単位なので、同じ変換を適用
                    mtoon.OutlineWidthMode = MToonExtension.OutlineWidthMode.worldCoordinates;
                    mtoon.OutlineWidthFactor = outlineWidth * 0.01f;

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
#if XRIFT_HAS_LILTOON
            // シェーダー名から判定（より確実）
            var shaderName = src.shader.name;
            if (lilShaderUtils.IsCutoutShaderName(shaderName))
            {
                return "MASK";
            }
            if (lilShaderUtils.IsTransparentShaderName(shaderName))
            {
                return "BLEND";
            }
#endif

            // プロパティからのフォールバック判定
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
