// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-License-Identifier: MPL-2.0

#nullable enable

#if XRIFT_HAS_LILTOON
using UnityEngine;
using UnityEditor;
using lilToon;

namespace XRift.VrmExporter.Platforms.VRM.Core
{
    /// <summary>
    /// lilToonマテリアルのテクスチャをベイクするユーティリティ
    /// lilToon公式のベイク処理を参考に、メモリ上で完結する実装
    /// </summary>
    internal sealed class LilToonTextureBaker
    {
        private static readonly Vector4 DefaultHSVG = new(0f, 1f, 1f, 1f);

        private readonly Shader? _bakerShader;

        public LilToonTextureBaker()
        {
            _bakerShader = lilShaderManager.ltsbaker;
        }

        public bool IsAvailable => _bakerShader != null;

        /// <summary>
        /// メインテクスチャをベイク（カラー、HSVG、2nd/3rdレイヤー合成）
        /// </summary>
        public Texture2D? BakeMainTexture(Material material)
        {
            if (!IsAvailable) return null;

            var mainTex = material.GetTexture("_MainTex") as Texture2D;
            if (mainTex == null) return null;

            // ベイクが不要かチェック
            var mainColor = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            var mainTexHSVG = material.HasProperty("_MainTexHSVG") ? material.GetVector("_MainTexHSVG") : DefaultHSVG;
            var gradationStrength = material.HasProperty("_MainGradationStrength") ? material.GetFloat("_MainGradationStrength") : 0f;
            var useMain2ndTex = material.HasProperty("_UseMain2ndTex") && material.GetFloat("_UseMain2ndTex") != 0f;
            var useMain3rdTex = material.HasProperty("_UseMain3rdTex") && material.GetFloat("_UseMain3rdTex") != 0f;

            bool needsBake = mainColor != Color.white ||
                             mainTexHSVG != DefaultHSVG ||
                             gradationStrength > 0f ||
                             useMain2ndTex ||
                             useMain3rdTex;

            if (!needsBake) return null;

            var bakerMaterial = new Material(_bakerShader!);
            try
            {
                // 基本パラメータ
                bakerMaterial.SetColor("_Color", Color.white); // カラーは別途適用されるため白で
                bakerMaterial.SetVector("_MainTexHSVG", mainTexHSVG);
                bakerMaterial.SetFloat("_MainGradationStrength", gradationStrength);

                if (material.HasProperty("_MainGradationTex"))
                    bakerMaterial.SetTexture("_MainGradationTex", material.GetTexture("_MainGradationTex"));
                if (material.HasProperty("_MainColorAdjustMask"))
                    bakerMaterial.SetTexture("_MainColorAdjustMask", material.GetTexture("_MainColorAdjustMask"));

                bakerMaterial.SetTexture("_MainTex", mainTex);

                // 2ndレイヤー
                if (useMain2ndTex)
                {
                    SetupLayerParams(bakerMaterial, material, "2nd");
                }

                // 3rdレイヤー
                if (useMain3rdTex)
                {
                    SetupLayerParams(bakerMaterial, material, "3rd");
                }

                return RunBake(mainTex, bakerMaterial);
            }
            finally
            {
                Object.DestroyImmediate(bakerMaterial);
            }
        }

        /// <summary>
        /// シャドウテクスチャをベイク（メインテクスチャ × シャドウカラー × マスク）
        /// </summary>
        public Texture2D? BakeShadowTexture(Material material, Texture? bakedMainTex = null)
        {
            if (!IsAvailable) return null;

            var useShadow = material.HasProperty("_UseShadow") && material.GetFloat("_UseShadow") != 0f;
            if (!useShadow) return null;

            var shadowStrengthMask = material.HasProperty("_ShadowStrengthMask")
                ? material.GetTexture("_ShadowStrengthMask")
                : null;
            var shadowMainStrength = material.HasProperty("_ShadowMainStrength")
                ? material.GetFloat("_ShadowMainStrength")
                : 0f;

            // シャドウマスクやメインテクスチャとの乗算が必要な場合のみベイク
            if (shadowStrengthMask == null && shadowMainStrength == 0f) return null;

            var mainTex = (bakedMainTex ?? material.GetTexture("_MainTex")) as Texture2D;
            if (mainTex == null) return null;

            var bakerMaterial = new Material(_bakerShader!);
            try
            {
                var shadowColor = material.HasProperty("_ShadowColor") ? material.GetColor("_ShadowColor") : Color.white;
                var shadowStrength = material.HasProperty("_ShadowStrength") ? material.GetFloat("_ShadowStrength") : 1f;
                var mainColor = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;

                // コントラスト（_ShadowMainStrength）を影色に事前適用
                // lilToonの計算式: lerp(shadowColor, shadowColor * mainColor, contrast)
                var adjustedShadowColor = Color.Lerp(
                    shadowColor,
                    new Color(
                        shadowColor.r * mainColor.r,
                        shadowColor.g * mainColor.g,
                        shadowColor.b * mainColor.b,
                        shadowColor.a
                    ),
                    shadowMainStrength
                );

                bakerMaterial.SetColor("_Color", Color.white);
                bakerMaterial.SetVector("_MainTexHSVG", DefaultHSVG);
                bakerMaterial.SetFloat("_UseMain2ndTex", 1f);
                bakerMaterial.SetFloat("_UseMain3rdTex", 0f); // 3rdレイヤー無効化

                // シャドウカラー適用（コントラスト適用済み）
                bakerMaterial.SetColor("_Color2nd", new Color(
                    adjustedShadowColor.r, adjustedShadowColor.g, adjustedShadowColor.b, shadowStrength));
                bakerMaterial.SetFloat("_Main2ndTexBlendMode", 0f);
                bakerMaterial.SetFloat("_Main2ndTexAlphaMode", 0f);

                bakerMaterial.SetTexture("_MainTex", mainTex);

                // シャドウテクスチャ
                var shadowColorTex = material.HasProperty("_ShadowColorTex")
                    ? material.GetTexture("_ShadowColorTex")
                    : null;
                bakerMaterial.SetTexture("_Main2ndTex", shadowColorTex ?? mainTex);

                // マスク
                if (shadowStrengthMask != null)
                {
                    bakerMaterial.SetTexture("_Main2ndBlendMask", shadowStrengthMask);
                }

                return RunBake(mainTex, bakerMaterial);
            }
            finally
            {
                Object.DestroyImmediate(bakerMaterial);
            }
        }

        /// <summary>
        /// MatCapテクスチャをベイク（MatCapテクスチャ × MatCapカラー）
        /// </summary>
        public Texture2D? BakeMatCapTexture(Material material)
        {
            if (!IsAvailable) return null;

            var useMatCap = material.HasProperty("_UseMatCap") && material.GetFloat("_UseMatCap") != 0f;
            if (!useMatCap) return null;

            var matcapTex = material.HasProperty("_MatCapTex") ? material.GetTexture("_MatCapTex") as Texture2D : null;
            if (matcapTex == null) return null;

            var matcapColor = material.HasProperty("_MatCapColor") ? material.GetColor("_MatCapColor") : Color.white;
            if (matcapColor == Color.white) return null; // ベイク不要

            var bakerMaterial = new Material(_bakerShader!);
            try
            {
                bakerMaterial.SetColor("_Color", matcapColor);
                bakerMaterial.SetVector("_MainTexHSVG", DefaultHSVG);
                bakerMaterial.SetTexture("_MainTex", matcapTex);

                return RunBake(matcapTex, bakerMaterial);
            }
            finally
            {
                Object.DestroyImmediate(bakerMaterial);
            }
        }

        /// <summary>
        /// 乗算モードMatCapの明暗を分離して取得
        /// BaseColor用（明るい方）とShadeColor用（暗い方）の2色を返す
        /// </summary>
        /// <param name="lerpFactor">明暗を近づける度合い（0.0=そのまま、1.0=同じ色になる）</param>
        public (Color baseColor, Color shadeColor) GetMatCapMultiplyColors(Material material, float lerpFactor = 0.3f)
        {
            var white = Color.white;
            var useMatCap = material.HasProperty("_UseMatCap") && material.GetFloat("_UseMatCap") != 0f;
            if (!useMatCap) return (white, white);

            var matcapTex = material.HasProperty("_MatCapTex") ? material.GetTexture("_MatCapTex") as Texture2D : null;
            if (matcapTex == null) return (white, white);

            var matcapColor = material.HasProperty("_MatCapColor") ? material.GetColor("_MatCapColor") : Color.white;
            var matcapBlend = material.HasProperty("_MatCapBlend") ? material.GetFloat("_MatCapBlend") : 1f;

            // MatCapテクスチャから最大・最小輝度の色を取得
            var (maxColor, minColor) = GetMinMaxLuminanceColors(matcapTex);

            // MatCapColorを適用
            maxColor = new Color(
                maxColor.r * matcapColor.r,
                maxColor.g * matcapColor.g,
                maxColor.b * matcapColor.b,
                1f
            );
            minColor = new Color(
                minColor.r * matcapColor.r,
                minColor.g * matcapColor.g,
                minColor.b * matcapColor.b,
                1f
            );

            // 明暗を少し近づける（極端な差を緩和）
            var lerpedMax = Color.Lerp(maxColor, minColor, lerpFactor);
            var lerpedMin = Color.Lerp(minColor, maxColor, lerpFactor);

            // _MatCapBlendと_MatCapColor.aで強度を調整
            var strength = matcapBlend * matcapColor.a;
            var baseResult = Color.Lerp(white, lerpedMax, strength);
            var shadeResult = Color.Lerp(white, lerpedMin, strength);

            return (baseResult, shadeResult);
        }

        /// <summary>
        /// テクスチャから最大輝度と最小輝度の色を取得
        /// </summary>
        private (Color maxColor, Color minColor) GetMinMaxLuminanceColors(Texture2D srcTexture)
        {
            var readableTex = GetReadableTexture(srcTexture);
            var pixels = readableTex.GetPixels();

            Color maxColor = Color.black;
            Color minColor = Color.white;
            float maxLum = 0f;
            float minLum = 1f;

            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                // 輝度計算（Rec.709）
                float lum = p.r * 0.2126f + p.g * 0.7152f + p.b * 0.0722f;

                if (lum > maxLum)
                {
                    maxLum = lum;
                    maxColor = p;
                }
                if (lum < minLum)
                {
                    minLum = lum;
                    minColor = p;
                }
            }

            if (readableTex != srcTexture)
            {
                Object.DestroyImmediate(readableTex);
            }

            return (maxColor, minColor);
        }

        /// <summary>
        /// テクスチャを読み取り可能な状態で取得
        /// </summary>
        private static Texture2D GetReadableTexture(Texture2D srcTexture)
        {
            if (srcTexture.isReadable)
            {
                return srcTexture;
            }

            // RenderTextureを使って読み取り可能なコピーを作成
            var tmpRT = RenderTexture.GetTemporary(srcTexture.width, srcTexture.height, 0, RenderTextureFormat.ARGB32);
            var prevRT = RenderTexture.active;

            Graphics.Blit(srcTexture, tmpRT);
            RenderTexture.active = tmpRT;

            var readableTex = new Texture2D(srcTexture.width, srcTexture.height, TextureFormat.RGBA32, false);
            readableTex.ReadPixels(new Rect(0, 0, srcTexture.width, srcTexture.height), 0, 0);
            readableTex.Apply();

            RenderTexture.active = prevRT;
            RenderTexture.ReleaseTemporary(tmpRT);

            return readableTex;
        }

        private void SetupLayerParams(Material bakerMaterial, Material srcMaterial, string suffix)
        {
            var useProperty = $"_UseMain{suffix}Tex";
            var colorProperty = $"_Color{suffix}";
            var texProperty = $"_Main{suffix}Tex";
            var angleProperty = $"_Main{suffix}TexAngle";
            var blendMaskProperty = $"_Main{suffix}BlendMask";
            var blendModeProperty = $"_Main{suffix}TexBlendMode";
            var alphaModeProperty = $"_Main{suffix}TexAlphaMode";

            bakerMaterial.SetFloat(useProperty, srcMaterial.GetFloat(useProperty));

            if (srcMaterial.HasProperty(colorProperty))
                bakerMaterial.SetColor(colorProperty, srcMaterial.GetColor(colorProperty));
            if (srcMaterial.HasProperty(texProperty))
                bakerMaterial.SetTexture(texProperty, srcMaterial.GetTexture(texProperty));
            if (srcMaterial.HasProperty(angleProperty))
                bakerMaterial.SetFloat(angleProperty, srcMaterial.GetFloat(angleProperty));
            if (srcMaterial.HasProperty(blendMaskProperty))
                bakerMaterial.SetTexture(blendMaskProperty, srcMaterial.GetTexture(blendMaskProperty));
            if (srcMaterial.HasProperty(blendModeProperty))
                bakerMaterial.SetFloat(blendModeProperty, srcMaterial.GetFloat(blendModeProperty));
            if (srcMaterial.HasProperty(alphaModeProperty))
                bakerMaterial.SetFloat(alphaModeProperty, srcMaterial.GetFloat(alphaModeProperty));

            // デカール関連
            var decalAnimProperty = $"_Main{suffix}TexDecalAnimation";
            var decalSubProperty = $"_Main{suffix}TexDecalSubParam";
            var isDecalProperty = $"_Main{suffix}TexIsDecal";

            if (srcMaterial.HasProperty(decalAnimProperty))
                bakerMaterial.SetVector(decalAnimProperty, srcMaterial.GetVector(decalAnimProperty));
            if (srcMaterial.HasProperty(decalSubProperty))
                bakerMaterial.SetVector(decalSubProperty, srcMaterial.GetVector(decalSubProperty));
            if (srcMaterial.HasProperty(isDecalProperty))
                bakerMaterial.SetFloat(isDecalProperty, srcMaterial.GetFloat(isDecalProperty));

            // UV設定
            if (srcMaterial.HasProperty(texProperty))
            {
                bakerMaterial.SetTextureOffset(texProperty, srcMaterial.GetTextureOffset(texProperty));
                bakerMaterial.SetTextureScale(texProperty, srcMaterial.GetTextureScale(texProperty));
            }
            if (srcMaterial.HasProperty(blendMaskProperty))
            {
                bakerMaterial.SetTextureOffset(blendMaskProperty, srcMaterial.GetTextureOffset(blendMaskProperty));
                bakerMaterial.SetTextureScale(blendMaskProperty, srcMaterial.GetTextureScale(blendMaskProperty));
            }
        }

        /// <summary>
        /// GPUでテクスチャをベイク
        /// </summary>
        private static Texture2D RunBake(Texture2D srcTexture, Material bakerMaterial)
        {
            int width = srcTexture.width;
            int height = srcTexture.height;

            var outTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            var bufRT = RenderTexture.active;
            var dstTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);

            try
            {
                Graphics.Blit(srcTexture, dstTexture, bakerMaterial);
                RenderTexture.active = dstTexture;
                outTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                outTexture.Apply();
            }
            finally
            {
                RenderTexture.active = bufRT;
                RenderTexture.ReleaseTemporary(dstTexture);
            }

            return outTexture;
        }
    }
}
#endif
