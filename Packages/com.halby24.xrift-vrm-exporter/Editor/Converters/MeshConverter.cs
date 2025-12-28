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
using UnityEngine;
using XRift.VrmExporter.Utils;
using XRift.VrmExporter.VRM.Gltf;

namespace XRift.VrmExporter.Converters
{
    /// <summary>
    /// Mesh変換コンテキスト
    /// </summary>
    internal class MeshConvertContext
    {
        public Root Root { get; init; } = null!;
        public exporter.Exporter Exporter { get; init; } = null!;
        public IDictionary<Transform, ObjectID> TransformNodeIDs { get; init; } = null!;
        public IDictionary<Material, ObjectID> MaterialIDs { get; init; } = null!;
        public Func<Material, ObjectID> ConvertMaterial { get; init; } = null!;
        public bool EnableVertexColorOutput { get; set; } = true;
        public bool DisableVertexColorOnLiltoon { get; set; } = false;
    }

    /// <summary>
    /// UnityのMesh/SkinnedMeshRendererをglTFメッシュに変換するクラス
    /// </summary>
    internal static class MeshConverter
    {
        /// <summary>
        /// 全てのMeshRendererを変換する
        /// </summary>
        public static void RetrieveAllMeshRenderers(Transform parent, MeshConvertContext ctx)
        {
            foreach (Transform child in parent)
            {
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var nodeID = ctx.TransformNodeIDs[child];
                var node = ctx.Root.Nodes![(int)nodeID.ID];

                if (child.gameObject.TryGetComponent<SkinnedMeshRenderer>(out var smr) && smr.sharedMesh)
                {
                    ConvertMesh(smr.sharedMesh, smr.sharedMaterials, smr.sharedMaterial, ctx, child, smr, ref node);
                }
                else if (child.gameObject.TryGetComponent<MeshRenderer>(out var mr) &&
                         mr.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh)
                {
                    ConvertMesh(filter.sharedMesh, mr.sharedMaterials, mr.sharedMaterial, ctx, child, null, ref node);
                }

                RetrieveAllMeshRenderers(child, ctx);
            }
        }

        /// <summary>
        /// 単一メッシュを変換
        /// </summary>
        public static void ConvertMesh(
            Mesh mesh,
            Material[] materials,
            Material fallbackMaterial,
            MeshConvertContext ctx,
            Transform parentTransform,
            SkinnedMeshRenderer? smr,
            ref node.Node node)
        {
            System.Numerics.Vector3[] positions, normals;
            exporter.JointUnit[] jointUnits;
            System.Numerics.Vector4[] weights;

            if (smr)
            {
                var resolver = new BoneResolver(parentTransform, smr!);
                var boneWeights = smr!.sharedMesh.boneWeights;
                var index = 0;
                jointUnits = new exporter.JointUnit[boneWeights.Length];
                weights = new System.Numerics.Vector4[boneWeights.Length];

                foreach (var item in boneWeights)
                {
                    var (b0, w0) = resolver.Resolve(item.boneIndex0, item.weight0);
                    var (b1, w1) = resolver.Resolve(item.boneIndex1, item.weight1);
                    var (b2, w2) = resolver.Resolve(item.boneIndex2, item.weight2);
                    var (b3, w3) = resolver.Resolve(item.boneIndex3, item.weight3);
                    var jointUnit = new exporter.JointUnit
                    {
                        X = b0,
                        Y = b1,
                        Z = b2,
                        W = b3,
                    };
                    jointUnits[index] = jointUnit;
                    weights[index] = new System.Numerics.Vector4(w0, w1, w2, w3);
                    index++;
                }

                positions = new System.Numerics.Vector3[boneWeights.Length];
                normals = new System.Numerics.Vector3[boneWeights.Length];
                index = 0;
                foreach (var item in boneWeights)
                {
                    positions[index] = resolver.ConvertPosition(index, item);
                    normals[index] = resolver.ConvertNormal(index, item);
                    index++;
                }

                // Skinを作成
                var skinID = new ObjectID((uint)ctx.Root.Skins!.Count);
                var joints = resolver.UniqueTransforms.Select(bone => ctx.TransformNodeIDs[bone]).ToList();
                var inverseBindMatricesAccessor =
                    ctx.Exporter.CreateMatrix4Accessor(ctx.Root, $"{smr.name}_IBM", resolver.InverseBindMatrices.ToArray());
                ctx.Root.Skins.Add(new node.Skin
                {
                    InverseBindMatrices = inverseBindMatricesAccessor,
                    Joints = joints,
                });
                node.Skin = skinID;
            }
            else
            {
                positions = mesh.vertices.Select(item => item.ToVector3WithCoordinateSpace()).ToArray();
                normals = mesh.normals.Select(item => item.normalized.ToVector3WithCoordinateSpace()).ToArray();
                jointUnits = Array.Empty<exporter.JointUnit>();
                weights = Array.Empty<System.Numerics.Vector4>();
            }

            var meshUnit = new exporter.MeshUnit
            {
                Name = new UnicodeString(AssetPathUtils.TrimCloneSuffix(mesh.name)),
                Positions = positions,
                Normals = normals,
                Colors = ctx.EnableVertexColorOutput
                    ? mesh.colors32.Select(item => ((Color)item).ToVector4(ColorSpace.Gamma, ColorSpace.Linear))
                        .ToArray()
                    : Array.Empty<System.Numerics.Vector4>(),
                TexCoords0 = mesh.uv.Select(item => item.ToVector2WithCoordinateSpace()).ToArray(),
                TexCoords1 = mesh.uv2.Select(item => item.ToVector2WithCoordinateSpace()).ToArray(),
                Joints = jointUnits,
                Weights = weights,
                Tangents = mesh.tangents.Select(item => item.ToVector4WithTangentSpace()).ToArray(),
            };

            var numMaterials = materials.Length;
            for (int i = 0, numSubMeshes = mesh.subMeshCount; i < numSubMeshes; i++)
            {
                var subMesh = mesh.GetSubMesh(i);
                var indices = mesh.GetIndices(i);
                IList<uint> newIndices;

                if (subMesh.topology == MeshTopology.Triangles)
                {
                    newIndices = new List<uint>();
                    var numIndices = indices.Length;
                    // 三角形の巻き方向を反転（Unity→glTF座標系変換）
                    for (var j = 0; j < numIndices; j += 3)
                    {
                        newIndices.Add((uint)indices[j + 2]);
                        newIndices.Add((uint)indices[j + 1]);
                        newIndices.Add((uint)indices[j + 0]);
                    }
                }
                else
                {
                    newIndices = indices.Select(index => (uint)index).ToList();
                }

                var primitiveMode = subMesh.topology switch
                {
                    MeshTopology.Lines => mesh.PrimitiveMode.Lines,
                    MeshTopology.LineStrip => mesh.PrimitiveMode.LineStrip,
                    MeshTopology.Points => mesh.PrimitiveMode.Point,
                    MeshTopology.Triangles => mesh.PrimitiveMode.Triangles,
                    _ => throw new ArgumentOutOfRangeException(),
                };

                var subMeshMaterial = i < numMaterials ? materials[i] : fallbackMaterial;
                if (!subMeshMaterial)
                {
                    continue;
                }

                var materialID = ctx.ConvertMaterial(subMeshMaterial);
                var isShaderLiltoon = subMeshMaterial.shader.name == "lilToon" ||
                                      subMeshMaterial.shader.name.StartsWith("Hidden/lilToon", StringComparison.Ordinal);

                if (isShaderLiltoon && ctx.DisableVertexColorOnLiltoon)
                {
                    var numColors = (uint)meshUnit.Colors.Length;
                    foreach (var index in newIndices)
                    {
                        if (index < numColors)
                        {
                            meshUnit.Colors[index] = System.Numerics.Vector4.One;
                        }
                    }
                }

                var primitiveUnit = new exporter.PrimitiveUnit
                {
                    Indices = newIndices.ToArray(),
                    Material = materialID,
                    PrimitiveMode = primitiveMode
                };
                meshUnit.Primitives.Add(primitiveUnit);
            }

            // ブレンドシェイプの変換
            var numBlendShapes = mesh.blendShapeCount;
            var blendShapeVertices = new Vector3[mesh.vertexCount];
            var blendShapeNormals = new Vector3[mesh.vertexCount];
            for (var i = 0; i < numBlendShapes; i++)
            {
                var name = mesh.GetBlendShapeName(i);
                mesh.GetBlendShapeFrameVertices(i, 0, blendShapeVertices, blendShapeNormals, null);
                meshUnit.MorphTargets.Add(new exporter.MorphTarget
                {
                    Name = name,
                    Positions = blendShapeVertices.Select(item => item.ToVector3WithCoordinateSpace()).ToArray(),
                    Normals = blendShapeNormals.Select(item => item.ToVector3WithCoordinateSpace()).ToArray(),
                });
            }

            if (meshUnit.Primitives.Count > 0)
            {
                node.Mesh = ctx.Exporter.CreateMesh(ctx.Root, meshUnit);
            }
        }
    }
}
