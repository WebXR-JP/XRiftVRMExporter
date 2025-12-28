// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-FileCopyrightText: 2024-present hkrn (original ndmf-vrm-exporter)
// SPDX-License-Identifier: MPL-2.0
//
// This file contains code derived from ndmf-vrm-exporter
// https://github.com/hkrn/ndmf-vrm-exporter

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using XRift.VrmExporter.Utils;
using XRift.VrmExporter.VRM;
using XRift.VrmExporter.VRM.Gltf;
using GltfMaterial = XRift.VrmExporter.VRM.Gltf.Material;
using GltfExporter = XRift.VrmExporter.VRM.Gltf.Exporter;

namespace XRift.VrmExporter.Converters
{
    /// <summary>
    /// glTFマテリアルエクスポーター
    /// </summary>
    internal sealed class GltfMaterialExporter
    {
        /// <summary>
        /// エクスポート時のオーバーライド設定
        /// </summary>
        public sealed class ExportOverrides
        {
            public Texture? MainTexture { get; set; }
            public GltfMaterial.AlphaMode? AlphaMode { get; set; }
            public int? CullMode { get; set; }
            public float? EmissiveStrength { get; set; }
            public bool EnableNormalMap { get; set; } = true;
        }

        /// <summary>
        /// テクスチャメタデータ
        /// </summary>
        public sealed class TextureItemMetadata
        {
            public TextureFormat TextureFormat { get; init; }
            public ColorSpace ColorSpace { get; init; }
            public int Width { get; init; }
            public int Height { get; init; }

            public string? KtxImageDataFormat
            {
                get
                {
                    if (Width > 0 && Height > 0 && Width % 4 == 0 && Height % 4 == 0)
                    {
                        return (TextureFormat, ColorSpace) switch
                        {
                            (TextureFormat.RGB24, ColorSpace.Gamma) => "R8G8B8_SRGB",
                            (TextureFormat.RGB24, ColorSpace.Linear) => "R8G8B8_UNORM",
                            (TextureFormat.ARGB32, ColorSpace.Gamma) => "R8G8B8A8_SRGB",
                            (TextureFormat.ARGB32, ColorSpace.Linear) => "R8G8B8A8_UNORM",
                            _ => null,
                        };
                    }

                    return null;
                }
            }
        }

        private static readonly int PropertyCullMode = Shader.PropertyToID("_CullMode");
        private static readonly int PropertyColor = Shader.PropertyToID("_Color");
        private static readonly int PropertyMainTex = Shader.PropertyToID("_MainTex");
        private static readonly int PropertyEmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int PropertyEmissionMap = Shader.PropertyToID("_EmissionMap");
        private static readonly int PropertyBumpScale = Shader.PropertyToID("_BumpScale");
        private static readonly int PropertyBumpMap = Shader.PropertyToID("_BumpMap");
        private static readonly int PropertyMetallic = Shader.PropertyToID("_Metallic");
        private static readonly int PropertyGlossiness = Shader.PropertyToID("_Glossiness");
        private static readonly int PropertyMetallicGlossMap = Shader.PropertyToID("_MetallicGlossMap");
        private static readonly int PropertyOcclusionStrength = Shader.PropertyToID("_OcclusionStrength");
        private static readonly int PropertyOcclusionMap = Shader.PropertyToID("_OcclusionMap");
        private static readonly int PropertyCutoff = Shader.PropertyToID("_Cutoff");

        public IDictionary<ObjectID, TextureItemMetadata> TextureMetadata { get; }

        private readonly Root _root;
        private readonly GltfExporter.Exporter _exporter;
        private readonly IDictionary<Texture, ObjectID> _textureIDs;
        private readonly ISet<string> _extensionsUsed;
        private readonly UnityEngine.Material _metalGlossChannelSwapMaterial;
        private readonly UnityEngine.Material _metalGlossOcclusionChannelSwapMaterial;
        private readonly UnityEngine.Material _normalChannelMaterial;

        public GltfMaterialExporter(Root root, GltfExporter.Exporter exporter, ISet<string> extensionsUsed)
        {
            var metalGlossChannelSwapShader = Resources.Load("MetalGlossChannelSwap", typeof(Shader)) as Shader;
            var metalGlossOcclusionChannelSwapShader =
                Resources.Load("MetalGlossOcclusionChannelSwap", typeof(Shader)) as Shader;
            var normalChannelShader = Resources.Load("NormalChannel", typeof(Shader)) as Shader;
            _root = root;
            _exporter = exporter;
            _textureIDs = new Dictionary<Texture, ObjectID>();
            TextureMetadata = new Dictionary<ObjectID, TextureItemMetadata>();
            _extensionsUsed = extensionsUsed;
            _metalGlossChannelSwapMaterial = new UnityEngine.Material(metalGlossChannelSwapShader);
            _metalGlossOcclusionChannelSwapMaterial = new UnityEngine.Material(metalGlossOcclusionChannelSwapShader);
            _normalChannelMaterial = new UnityEngine.Material(normalChannelShader);
        }

        /// <summary>
        /// マテリアルをエクスポート
        /// </summary>
        public GltfMaterial.Material Export(UnityEngine.Material source, ExportOverrides overrides)
        {
            var alphaMode = overrides.AlphaMode;
            if (alphaMode == null)
            {
                var renderType = source.GetTag("RenderType", true);
                alphaMode = renderType switch
                {
                    "Transparent" => GltfMaterial.AlphaMode.Blend,
                    "TransparentCutout" => GltfMaterial.AlphaMode.Mask,
                    _ => GltfMaterial.AlphaMode.Opaque,
                };
            }

            var material = new GltfMaterial.Material
            {
                Name = new UnicodeString(AssetPathUtils.TrimCloneSuffix(source.name)),
                PbrMetallicRoughness = new GltfMaterial.PbrMetallicRoughness(),
                AlphaMode = alphaMode,
            };

            // BaseColor
            if (source.HasProperty(PropertyColor))
            {
                material.PbrMetallicRoughness.BaseColorFactor =
                    source.GetColor(PropertyColor).ToVector4(ColorSpace.Gamma, ColorSpace.Linear);
            }

            // MainTexture
            if (overrides.MainTexture)
            {
                material.PbrMetallicRoughness.BaseColorTexture =
                    ExportTextureInfo(source, overrides.MainTexture, ColorSpace.Gamma, blitMaterial: null,
                        needsBlit: false);
            }
            else if (source.HasProperty(PropertyMainTex))
            {
                var texture = source.GetTexture(PropertyMainTex);
                material.PbrMetallicRoughness.BaseColorTexture =
                    ExportTextureInfo(source, texture, ColorSpace.Gamma, blitMaterial: null, needsBlit: true);
                DecorateTextureTransform(source, PropertyMainTex, material.PbrMetallicRoughness.BaseColorTexture);
            }

            // Emissive
            if (overrides.EmissiveStrength.HasValue)
            {
                var emissiveStrength = overrides.EmissiveStrength.Value;
                if (emissiveStrength > 0.0f)
                {
                    material.EmissiveFactor = System.Numerics.Vector3.Clamp(GetEmissionColor(source),
                        System.Numerics.Vector3.Zero, System.Numerics.Vector3.One);
                    if (emissiveStrength < 1.0f)
                    {
                        material.EmissiveFactor *= emissiveStrength;
                    }
                }

                ExportEmissionTexture(source, material);
            }
            else if (source.IsKeywordEnabled("_EMISSION"))
            {
                var emissiveFactor = GetEmissionColor(source);
                material.EmissiveFactor = System.Numerics.Vector3.Clamp(emissiveFactor,
                    System.Numerics.Vector3.Zero, System.Numerics.Vector3.One);
                var emissiveStrength = Mathf.Max(emissiveFactor.X, emissiveFactor.Y, emissiveFactor.Z);
                if (emissiveStrength > 1.0f)
                {
                    AddEmissiveStrengthExtension(emissiveStrength, material);
                }

                ExportEmissionTexture(source, material);
            }

            // NormalMap
            if (overrides.EnableNormalMap && source.HasProperty(PropertyBumpMap))
            {
                var texture = source.GetTexture(PropertyBumpMap);
                if (texture)
                {
                    var info = ExportTextureInfo(source, texture, ColorSpace.Linear, _normalChannelMaterial,
                        needsBlit: true);
                    material.NormalTexture = new GltfMaterial.NormalTextureInfo
                    {
                        Index = info!.Index,
                        TexCoord = info.TexCoord,
                    };
                    if (source.HasProperty(PropertyBumpScale))
                    {
                        material.NormalTexture.Scale = source.GetFloat(PropertyBumpScale);
                    }

                    material.NormalTexture.Extensions ??= new Dictionary<string, JToken>();
                    DecorateTextureTransform(source, PropertyBumpMap, material.NormalTexture.Extensions);
                }
            }

            // Occlusion
            if (source.HasProperty(PropertyOcclusionMap))
            {
                var texture = source.GetTexture(PropertyOcclusionMap);
                if (texture)
                {
                    var info = ExportTextureInfo(source, texture, ColorSpace.Linear,
                        _metalGlossOcclusionChannelSwapMaterial, needsBlit: true);
                    material.OcclusionTexture = new GltfMaterial.OcclusionTextureInfo
                    {
                        Index = info!.Index,
                        TexCoord = info.TexCoord,
                    };
                    if (source.HasProperty(PropertyOcclusionStrength))
                    {
                        material.OcclusionTexture.Strength =
                            Mathf.Clamp01(source.GetFloat(PropertyOcclusionStrength));
                    }

                    material.OcclusionTexture.Extensions ??= new Dictionary<string, JToken>();
                    DecorateTextureTransform(source, PropertyOcclusionMap, material.OcclusionTexture.Extensions);
                }
            }

            // MetallicRoughness
            if (source.HasProperty(PropertyMetallicGlossMap))
            {
                var texture = source.GetTexture(PropertyMetallicGlossMap);
                if (texture)
                {
                    material.PbrMetallicRoughness.MetallicRoughnessTexture =
                        ExportTextureInfo(source, texture, ColorSpace.Linear, _metalGlossChannelSwapMaterial,
                            needsBlit: true);
                    material.PbrMetallicRoughness.MetallicFactor = 1.0f;
                    material.PbrMetallicRoughness.RoughnessFactor = 1.0f;
                    DecorateTextureTransform(source, PropertyMetallicGlossMap,
                        material.PbrMetallicRoughness.MetallicRoughnessTexture);
                }
            }
            else
            {
                if (source.HasProperty(PropertyMetallic))
                {
                    material.PbrMetallicRoughness.MetallicFactor = Mathf.Clamp01(source.GetFloat(PropertyMetallic));
                }

                if (source.HasProperty(PropertyGlossiness))
                {
                    material.PbrMetallicRoughness.RoughnessFactor =
                        Mathf.Clamp01(source.GetFloat(PropertyGlossiness));
                }
            }

            // AlphaCutoff
            if (material.AlphaMode == GltfMaterial.AlphaMode.Mask && source.HasProperty(PropertyCutoff))
            {
                material.AlphaCutoff = Mathf.Max(source.GetFloat(PropertyCutoff), 0.0f);
            }

            // DoubleSided
            if (overrides.CullMode != null)
            {
                material.DoubleSided = overrides.CullMode == 0;
            }
            else if (source.HasProperty(PropertyCullMode))
            {
                material.DoubleSided = source.GetInt(PropertyCullMode) == 0;
            }

            return material;
        }

        /// <summary>
        /// テクスチャを解決
        /// </summary>
        internal Texture? ResolveTexture(GltfMaterial.TextureInfo? info)
        {
            return info == null
                ? null
                : (from item in _textureIDs where item.Value.Equals(info.Index) select item.Key)
                .FirstOrDefault();
        }

        /// <summary>
        /// MToon用テクスチャ情報をエクスポート
        /// </summary>
        internal GltfMaterial.TextureInfo? ExportTextureInfoMToon(UnityEngine.Material material, Texture? texture,
            ColorSpace cs, bool needsBlit)
        {
            return ExportTextureInfoInner(material, texture, cs, blitMaterial: null, needsBlit, mtoon: true);
        }

        private GltfMaterial.TextureInfo? ExportTextureInfo(UnityEngine.Material material, Texture? texture,
            ColorSpace cs, UnityEngine.Material? blitMaterial, bool needsBlit)
        {
            return ExportTextureInfoInner(material, texture, cs, blitMaterial, needsBlit, mtoon: false);
        }

        private GltfMaterial.TextureInfo? ExportTextureInfoInner(UnityEngine.Material material, Texture? texture,
            ColorSpace cs, UnityEngine.Material? blitMaterial, bool needsBlit, bool mtoon)
        {
            if (!texture || texture is null)
            {
                return null;
            }

            if (!_textureIDs.TryGetValue(texture, out var textureID))
            {
                const TextureFormat textureFormat = TextureFormat.RGBA32;
                var name = $"{AssetPathUtils.TrimCloneSuffix(material.name)}_{texture.name}_{textureFormat}";
                if (mtoon)
                {
                    name = $"VRM_MToon_{name}";
                }

                var textureUnit = ExportTextureUnit(texture, name, textureFormat, cs, blitMaterial, needsBlit);
                textureID = _exporter.CreateSampledTexture(_root, textureUnit);
                _textureIDs.Add(texture, textureID);
                TextureMetadata.Add(textureID, new TextureItemMetadata
                {
                    TextureFormat = textureFormat,
                    ColorSpace = cs,
                    Width = texture.width,
                    Height = texture.height,
                });
            }

            return new GltfMaterial.TextureInfo
            {
                Index = textureID,
            };
        }

        private void DecorateTextureTransform(UnityEngine.Material material, int propertyID,
            GltfMaterial.TextureInfo? info)
        {
            if (info == null)
            {
                return;
            }

            info.Extensions ??= new Dictionary<string, JToken>();
            DecorateTextureTransform(material, propertyID, info.Extensions);
        }

        private static System.Numerics.Vector3 GetEmissionColor(UnityEngine.Material source)
        {
            if (!source.HasProperty(PropertyEmissionColor))
                return System.Numerics.Vector3.Zero;
            return source.GetColor(PropertyEmissionColor)
                .ToVector3(ColorSpace.Gamma, ColorSpace.Linear);
        }

        private void ExportEmissionTexture(UnityEngine.Material source, GltfMaterial.Material material)
        {
            if (!source.HasProperty(PropertyEmissionMap))
            {
                return;
            }

            var texture = source.GetTexture(PropertyEmissionMap);
            material.EmissiveTexture =
                ExportTextureInfo(source, texture, ColorSpace.Gamma, blitMaterial: null, needsBlit: true);
            DecorateTextureTransform(source, PropertyEmissionMap, material.EmissiveTexture);
        }

        private void AddEmissiveStrengthExtension(float emissiveStrength, GltfMaterial.Material material)
        {
            material.Extensions ??= new Dictionary<string, JToken>();
            material.Extensions.Add(Extensions.KhrMaterialsEmissiveStrength.Name,
                Document.SaveAsNode(
                    new Extensions.KhrMaterialsEmissiveStrength
                    {
                        EmissiveStrength = emissiveStrength,
                    }));
            _extensionsUsed.Add(Extensions.KhrMaterialsEmissiveStrength.Name);
        }

        private void DecorateTextureTransform(UnityEngine.Material material, int propertyID,
            IDictionary<string, JToken> extensions)
        {
            Extensions.KhrTextureTransform? transform = null;
            var offset = material.GetTextureOffset(propertyID);
            if (offset != Vector2.zero)
            {
                transform ??= new Extensions.KhrTextureTransform();
                transform.Offset = offset.ToVector2WithCoordinateSpace();
            }

            var scale = material.GetTextureScale(PropertyMainTex);
            if (scale != Vector2.one)
            {
                transform ??= new Extensions.KhrTextureTransform();
                transform.Scale = offset.ToVector2();
            }

            if (transform == null)
            {
                return;
            }

            extensions.Add(Extensions.KhrTextureTransform.Name,
                Document.SaveAsNode(transform));
            _extensionsUsed.Add(Extensions.KhrTextureTransform.Name);
        }

        /// <summary>
        /// テクスチャユニットをエクスポート
        /// </summary>
        public static GltfExporter.SampledTextureUnit ExportTextureUnit(Texture texture, string name,
            TextureFormat textureFormat, ColorSpace cs, UnityEngine.Material? blitMaterial, bool needsBlit)
        {
            byte[] bytes;
            if (needsBlit)
            {
                var destTexture = texture.Blit(textureFormat, cs, blitMaterial);
                bytes = destTexture.EncodeToPNG();
                Object.DestroyImmediate(destTexture);
            }
            else if (texture.isReadable && texture is Texture2D tex)
            {
                bytes = tex.EncodeToPNG();
            }
            else
            {
                bytes = System.Array.Empty<byte>();
            }

            return new GltfExporter.SampledTextureUnit
            {
                Name = new UnicodeString(name),
                MimeType = "image/png",
                Data = bytes,
                MagFilter = texture.filterMode.ToTextureFilterMode(),
                MinFilter = texture.filterMode.ToTextureFilterMode(),
                WrapS = texture.wrapModeU.ToTextureWrapMode(),
                WrapT = texture.wrapModeV.ToTextureWrapMode(),
            };
        }
    }
}
