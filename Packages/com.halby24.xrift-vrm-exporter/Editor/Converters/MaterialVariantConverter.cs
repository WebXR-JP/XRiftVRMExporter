// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-FileCopyrightText: 2024-present hkrn (original ndmf-vrm-exporter)
// SPDX-License-Identifier: MPL-2.0
//
// This file contains code derived from ndmf-vrm-exporter
// https://github.com/hkrn/ndmf-vrm-exporter

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using XRift.VrmExporter.Utils;
using XRift.VrmExporter.VRM;
using XRift.VrmExporter.VRM.Gltf;

namespace XRift.VrmExporter.Converters
{
    /// <summary>
    /// KHR_materials_variants 変換
    /// </summary>
    internal sealed class MaterialVariantConverter
    {
        private readonly Root _root;
        private readonly IDictionary<Transform, ObjectID> _transformNodeIDs;
        private readonly IDictionary<Material, ObjectID> _materialIDs;
        private readonly IReadOnlyList<MaterialVariant> _materialVariants;
        private readonly ISet<string> _extensionsUsed;
        private readonly Func<Material, bool, ObjectID> _convertMaterial;

        public MaterialVariantConverter(
            Root root,
            IDictionary<Transform, ObjectID> transformNodeIDs,
            IDictionary<Material, ObjectID> materialIDs,
            IReadOnlyList<MaterialVariant> materialVariants,
            ISet<string> extensionsUsed,
            Func<Material, bool, ObjectID> convertMaterial)
        {
            _root = root;
            _transformNodeIDs = transformNodeIDs;
            _materialIDs = materialIDs;
            _materialVariants = materialVariants;
            _extensionsUsed = extensionsUsed;
            _convertMaterial = convertMaterial;
        }

        /// <summary>
        /// 全てのMaterial Variantsを変換
        /// </summary>
        public void ConvertAllMaterialVariants(bool enableBakingAlphaMaskTexture = false)
        {
            if (_materialVariants.Count == 0)
            {
                return;
            }

            var materialVariants = new Extensions.KhrMaterialsVariants();
            var variantIndex = 0u;

            // バリアント名を登録
            foreach (var variant in _materialVariants)
            {
                materialVariants.Variants.Add(new Extensions.KhrMaterialsVariantsItem
                {
                    Name = new UnicodeString(variant.Name ?? $"Variant{variantIndex}")
                });
            }

            var allMeshMaterialMappings =
                new Dictionary<(ObjectID, int), Extensions.KhrMaterialsVariantsPrimitive>();
            variantIndex = 0u;

            foreach (var variant in _materialVariants)
            {
                var mappingIndex = 0u;
                foreach (var mapping in variant.Mappings)
                {
                    ProcessMapping(mapping, variant.Name, mappingIndex, variantIndex,
                        enableBakingAlphaMaskTexture, allMeshMaterialMappings);
                    mappingIndex++;
                }

                variantIndex++;
            }

            if (allMeshMaterialMappings.Count <= 0)
            {
                return;
            }

            // 拡張をルートに追加
            _root.Extensions!.Add(Extensions.KhrMaterialsVariants.Name,
                Document.SaveAsNode(materialVariants));
            _extensionsUsed.Add(Extensions.KhrMaterialsVariants.Name);

            // 各プリミティブに拡張を追加
            foreach (var ((meshID, primitiveIndex), variantPrimitive) in allMeshMaterialMappings)
            {
                var mesh = _root.Meshes![(int)meshID.ID];
                var primitive = mesh.Primitives![primitiveIndex];
                primitive.Extensions ??= new Dictionary<string, JToken>();
                primitive.Extensions.Add(Extensions.KhrMaterialsVariants.Name,
                    Document.SaveAsNode(variantPrimitive));
            }
        }

        private void ProcessMapping(
            MaterialVariantMapping mapping,
            string? variantName,
            uint mappingIndex,
            uint variantIndex,
            bool enableBakingAlphaMaskTexture,
            Dictionary<(ObjectID, int), Extensions.KhrMaterialsVariantsPrimitive> allMeshMaterialMappings)
        {
            var renderer = mapping.Renderer;
            if (!renderer)
            {
                Debug.LogWarning(
                    $"Cannot convert material variant {variantName}:{mappingIndex} due to renderer is null");
                return;
            }

            if (!_transformNodeIDs.TryGetValue(renderer.transform, out var nodeID))
            {
                Debug.LogWarning(
                    $"Cannot convert material variant {variantName}:{mappingIndex} due to transform not found");
                return;
            }

            var node = _root.Nodes![(int)nodeID.ID];
            if (!node.Mesh.HasValue)
            {
                Debug.LogWarning(
                    $"Cannot convert material variant {variantName}:{mappingIndex} due to mesh is none");
                return;
            }

            var meshID = node.Mesh!.Value;
            var mesh = _root.Meshes![(int)meshID.ID];
            var primitives = mesh.Primitives!;
            var primitiveIndex = 0;

            foreach (var material in mapping.Materials)
            {
                if (primitiveIndex >= primitives.Count)
                {
                    break;
                }

                ObjectID materialID;
                if (material)
                {
                    if (!_materialIDs.TryGetValue(material, out materialID))
                    {
                        materialID = _convertMaterial(material, enableBakingAlphaMaskTexture);
                    }
                }
                else
                {
                    materialID = primitives[primitiveIndex].Material ?? ObjectID.Null;
                }

                AddMaterialMapping(meshID, primitiveIndex, materialID, variantIndex, allMeshMaterialMappings);
                primitiveIndex++;
            }
        }

        private static void AddMaterialMapping(
            ObjectID meshID,
            int primitiveIndex,
            ObjectID materialID,
            uint variantIndex,
            Dictionary<(ObjectID, int), Extensions.KhrMaterialsVariantsPrimitive> allMeshMaterialMappings)
        {
            if (allMeshMaterialMappings.TryGetValue((meshID, primitiveIndex), out var materialMappings))
            {
                var foundMaterialMapping =
                    materialMappings.Mappings.FirstOrDefault(item => item.Material.Equals(materialID));
                if (foundMaterialMapping != null)
                {
                    foundMaterialMapping.Variants.Add(new ObjectID(variantIndex));
                }
                else
                {
                    materialMappings.Mappings.Add(new Extensions.KhrMaterialsVariantsPrimitiveMapping
                    {
                        Material = materialID,
                        Variants = new List<ObjectID> { new(variantIndex) },
                    });
                }
            }
            else
            {
                var materialMapping = new Extensions.KhrMaterialsVariantsPrimitiveMapping
                {
                    Material = materialID,
                    Variants = new List<ObjectID> { new(variantIndex) },
                };
                allMeshMaterialMappings.Add((meshID, primitiveIndex),
                    new Extensions.KhrMaterialsVariantsPrimitive
                    {
                        Mappings = new List<Extensions.KhrMaterialsVariantsPrimitiveMapping>
                            { materialMapping }
                    });
            }
        }
    }
}
