// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-FileCopyrightText: 2024-present hkrn (original ndmf-vrm-exporter)
// SPDX-License-Identifier: MPL-2.0
//
// This file contains code derived from ndmf-vrm-exporter
// https://github.com/hkrn/ndmf-vrm-exporter

#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XRift.VrmExporter.Baking;
using XRift.VrmExporter.Core;
using VrmMToon = XRift.VrmExporter.VRM.MToon;
using GltfMaterial = XRift.VrmExporter.VRM.Gltf.Material;

namespace XRift.VrmExporter.Converters
{
    /// <summary>
    /// MToonテクスチャ情報
    /// </summary>
    internal sealed class MToonTexture
    {
        public Texture? MainTexture { get; set; }
        public GltfMaterial.TextureInfo? MainTextureInfo { get; set; }
    }

    /// <summary>
    /// MToon変換設定
    /// </summary>
    internal sealed class MToonConvertSettings
    {
        public bool EnableRimLight { get; set; } = true;
        public bool EnableMatCap { get; set; } = true;
        public bool EnableOutline { get; set; } = true;
    }

    /// <summary>
    /// lilToon → MToon変換
    /// </summary>
    internal sealed class MToonConverter
    {
        private readonly GameObject _gameObject;
        private readonly IAssetSaver _assetSaver;
        private readonly MToonConvertSettings _settings;

        public MToonConverter(
            GameObject gameObject,
            IAssetSaver assetSaver,
            MToonConvertSettings? settings = null)
        {
            _gameObject = gameObject;
            _assetSaver = assetSaver;
            _settings = settings ?? new MToonConvertSettings();
        }

        /// <summary>
        /// lilToonマテリアルをMToonにエクスポート
        /// </summary>
        public VrmMToon.MToon ExportMToon(
            Material material,
            MToonTexture mToonTexture,
            GltfMaterialExporter exporter)
        {
            var mtoon = new VrmMToon.MToon
            {
                GIEqualizationFactor = 1.0f,
                MatcapFactor = System.Numerics.Vector3.Zero
            };

#if XRIFT_HAS_LILTOON
            var so = new SerializedObject(material);
            so.Update();
            var props = so.FindProperty("m_SavedProperties");
            var textures = MaterialBaker.GetProps(props.FindPropertyRelative("m_TexEnvs"));
            var floats = MaterialBaker.GetProps(props.FindPropertyRelative("m_Floats"))
                .ToDictionary(kv => kv.Key, kv => kv.Value.floatValue);
            var colors = MaterialBaker.GetProps(props.FindPropertyRelative("m_Colors"))
                .ToDictionary(kv => kv.Key, kv => kv.Value.colorValue);
            var scrollRotate = colors.GetValueOrDefault("_MainTex_ScrollRotate", Color.clear);

            Texture2D? LocalRetrieveTexture2D(string name)
            {
                var prop = textures!.GetValueOrDefault(name, null);
                return prop?.FindPropertyRelative("m_Texture").objectReferenceValue as Texture2D;
            }

            // シャドウ
            if (Mathf.Approximately(floats.GetValueOrDefault("_UseShadow", 0.0f), 1.0f))
            {
                ExportShadow(material, mToonTexture, exporter, mtoon, floats, colors, LocalRetrieveTexture2D);
            }
            else
            {
                mtoon.ShadeColorFactor = System.Numerics.Vector3.One;
                mtoon.ShadeMultiplyTexture = mToonTexture.MainTextureInfo;
            }

            // リムライト
            if (_settings.EnableRimLight &&
                Mathf.Approximately(floats.GetValueOrDefault("_UseRim", 0.0f), 1.0f))
            {
                ExportRimLight(material, exporter, mtoon, floats, colors, LocalRetrieveTexture2D);
            }
            else
            {
                mtoon.RimLightingMixFactor = 0.0f;
            }

            // MatCap
            if (_settings.EnableMatCap &&
                Mathf.Approximately(floats.GetValueOrDefault("_UseMatCap", 0.0f), 1.0f) &&
                !Mathf.Approximately(floats.GetValueOrDefault("_MatCapBlendMode", 1.0f), 3.0f))
            {
                ExportMatCap(material, exporter, mtoon, floats, LocalRetrieveTexture2D);
            }

            // アウトライン
            var shaderName = material.shader.name;
            var isOutline = shaderName.Contains("Outline");
            if (_settings.EnableOutline && isOutline)
            {
                ExportOutline(material, exporter, mtoon, floats, colors, LocalRetrieveTexture2D);
            }

            // 透過設定
            var isCutout = shaderName.Contains("Cutout");
            var isTransparent = shaderName.Contains("Transparent") || shaderName.Contains("Overlay");
            mtoon.TransparentWithZWrite =
                isCutout || (isTransparent &&
                             !Mathf.Approximately(floats.GetValueOrDefault("_ZWrite", 1.0f), 0.0f));

            // UVアニメーション
            mtoon.UVAnimationScrollXSpeedFactor = scrollRotate.r;
            mtoon.UVAnimationScrollYSpeedFactor = scrollRotate.g;
            mtoon.UVAnimationRotationSpeedFactor = scrollRotate.a / Mathf.PI * 0.5f;
#endif

            return mtoon;
        }

#if XRIFT_HAS_LILTOON
        private void ExportShadow(
            Material material,
            MToonTexture mToonTexture,
            GltfMaterialExporter exporter,
            VrmMToon.MToon mtoon,
            Dictionary<string, float> floats,
            Dictionary<string, Color> colors,
            System.Func<string, Texture2D?> retrieveTexture)
        {
            var shadowBorder = floats.GetValueOrDefault("_ShadowBorder", 0.5f);
            var shadowBlur = floats.GetValueOrDefault("_ShadowBlur", 0.1f);
            var shadeShift = Mathf.Clamp01(shadowBorder - (shadowBlur * 0.5f)) * 2.0f - 1.0f;
            var shadeToony = Mathf.Approximately(shadeShift, 1.0f)
                ? 1.0f
                : (2.0f - Mathf.Clamp01(shadowBorder + shadowBlur * 0.5f) * 2.0f) /
                  (1.0f - shadeShift);

            if (retrieveTexture("_ShadowStrengthMask") != null ||
                !Mathf.Approximately(floats.GetValueOrDefault("_ShadowMainStrength", 0.0f), 0.0f))
            {
                var bakedShadowTex =
                    MaterialBaker.AutoBakeShadowTexture(_assetSaver, material, mToonTexture.MainTexture);
                mtoon.ShadeColorFactor = Color.white.ToVector3();
                mtoon.ShadeMultiplyTexture = bakedShadowTex != null
                    ? exporter.ExportTextureInfoMToon(material, bakedShadowTex, ColorSpace.Gamma, needsBlit: false)
                    : mToonTexture.MainTextureInfo;
            }
            else
            {
                var shadowColor = colors.GetValueOrDefault("_ShadowColor", new Color(0.82f, 0.76f, 0.85f));
                var shadowStrength = floats.GetValueOrDefault("_ShadowStrength", 1.0f);
                var shadeColorStrength = new Color(
                    1.0f - (1.0f - shadowColor.r) * shadowStrength,
                    1.0f - (1.0f - shadowColor.g) * shadowStrength,
                    1.0f - (1.0f - shadowColor.b) * shadowStrength,
                    1.0f
                );
                mtoon.ShadeColorFactor = shadeColorStrength.ToVector3();
                var shadowColorTex = retrieveTexture("_ShadowColorTex");
                mtoon.ShadeMultiplyTexture = shadowColorTex != null
                    ? exporter.ExportTextureInfoMToon(material, shadowColorTex, ColorSpace.Gamma, needsBlit: true)
                    : mToonTexture.MainTextureInfo;
            }

            var texture = retrieveTexture("_ShadowBorderTex");
            var info = exporter.ExportTextureInfoMToon(material, texture, ColorSpace.Gamma, needsBlit: true);
            if (info != null)
            {
                mtoon.ShadingShiftTexture = new VrmMToon.ShadingShiftTexture
                {
                    Index = info.Index,
                    Scale = 1.0f,
                    TexCoord = info.TexCoord,
                };
            }

            var rangeMin = shadeShift;
            var rangeMax = Mathf.Lerp(1.0f, shadeShift, shadeToony);
            mtoon.ShadingShiftFactor = Mathf.Clamp((rangeMin + rangeMax) * -0.5f, -1.0f, 1.0f);
            mtoon.ShadingToonyFactor = Mathf.Clamp01((2.0f - (rangeMax - rangeMin)) * 0.5f);
        }

        private void ExportRimLight(
            Material material,
            GltfMaterialExporter exporter,
            VrmMToon.MToon mtoon,
            Dictionary<string, float> floats,
            Dictionary<string, Color> colors,
            System.Func<string, Texture2D?> retrieveTexture)
        {
            var rimColorTexture = retrieveTexture("_RimColorTex");
            var rimBorder = floats.GetValueOrDefault("_RimBorder", 0.5f);
            var rimBlur = floats.GetValueOrDefault("_RimBlur", 0.65f);
            var rimFresnelPower = floats.GetValueOrDefault("_RimFresnelPower", 3.5f);
            var rimFp = rimFresnelPower / Mathf.Max(0.001f, rimBlur);
            var rimLift = Mathf.Pow(1.0f - rimBorder, rimFresnelPower) * (1.0f - rimBlur);

            mtoon.RimLightingMixFactor = 1.0f;
            mtoon.ParametricRimColorFactor =
                colors.GetValueOrDefault("_RimColor", new Color(0.66f, 0.5f, 0.48f)).ToVector3();
            mtoon.ParametricRimLiftFactor = rimLift;
            mtoon.ParametricRimFresnelPowerFactor = rimFp;

            if (Mathf.Approximately(floats.GetValueOrDefault("_RimBlendMode", 1.0f), 3.0f))
            {
                mtoon.RimMultiplyTexture =
                    exporter.ExportTextureInfoMToon(material, rimColorTexture, ColorSpace.Gamma, needsBlit: true);
            }
        }

        private void ExportMatCap(
            Material material,
            GltfMaterialExporter exporter,
            VrmMToon.MToon mtoon,
            Dictionary<string, float> floats,
            System.Func<string, Texture2D?> retrieveTexture)
        {
            var matcapTexture = retrieveTexture("_MatCapTex");
            if (matcapTexture != null)
            {
                var bakedMatCap = MaterialBaker.AutoBakeMatCap(_assetSaver, material);
                mtoon.MatcapTexture =
                    exporter.ExportTextureInfoMToon(material, bakedMatCap, ColorSpace.Gamma, needsBlit: true);
                mtoon.MatcapFactor = System.Numerics.Vector3.One;
            }

            if (!_settings.EnableRimLight)
            {
                var matcapBlendMaskTexture = retrieveTexture("_MatCapBlendMask");
                mtoon.RimMultiplyTexture = exporter.ExportTextureInfoMToon(material, matcapBlendMaskTexture,
                    ColorSpace.Linear, needsBlit: true);
                mtoon.RimLightingMixFactor = 1.0f;
            }
        }

        private void ExportOutline(
            Material material,
            GltfMaterialExporter exporter,
            VrmMToon.MToon mtoon,
            Dictionary<string, float> floats,
            Dictionary<string, Color> colors,
            System.Func<string, Texture2D?> retrieveTexture)
        {
            var outlineWidthTexture = retrieveTexture("_OutlineWidthMask");
            mtoon.OutlineWidthMode = VrmMToon.OutlineWidthMode.WorldCoordinates;
            mtoon.OutlineLightingMixFactor = 1.0f;
            mtoon.OutlineWidthFactor = floats.GetValueOrDefault("_OutlineWidth", 0.08f) * 0.01f;
            mtoon.OutlineColorFactor = colors.GetValueOrDefault("_OutlineColor", new Color(0.6f, 0.56f, 0.73f))
                .ToVector3();
            mtoon.OutlineWidthMultiplyTexture =
                exporter.ExportTextureInfoMToon(material, outlineWidthTexture, ColorSpace.Gamma, needsBlit: true);
        }
#endif
    }
}
