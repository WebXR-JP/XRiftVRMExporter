// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-FileCopyrightText: 2024-present hkrn (original ndmf-vrm-exporter)
// SPDX-License-Identifier: MPL-2.0
//
// This file contains code derived from ndmf-vrm-exporter
// https://github.com/hkrn/ndmf-vrm-exporter

#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XRift.VrmExporter.Utils;

namespace XRift.VrmExporter.Converters
{
    /// <summary>
    /// SkinnedMeshRendererのボーンウェイトを解決するクラス
    /// Unity座標系からglTF座標系への変換を行う
    /// </summary>
    internal sealed class BoneResolver
    {
        /// <summary>
        /// 使用されているボーンのリスト（重複なし）
        /// </summary>
        public IList<Transform> UniqueTransforms { get; } = new List<Transform>();

        /// <summary>
        /// 逆バインド行列のリスト
        /// </summary>
        public IList<System.Numerics.Matrix4x4> InverseBindMatrices { get; } =
            new List<System.Numerics.Matrix4x4>();

        private readonly Dictionary<Transform, int> _transformMap = new();
        private readonly Matrix4x4 _inverseParentTransformMatrix;
        private readonly Transform[] _boneTransforms;
        private readonly Vector3[] _originPositions;
        private readonly Vector3[] _originNormals;
        private readonly Vector3[] _deltaPositions;
        private readonly Vector3[] _deltaNormals;
        private readonly Matrix4x4[] _boneMatrices;
        private readonly Matrix4x4[] _bindPoseMatrices;

        public BoneResolver(Transform parentTransform, SkinnedMeshRenderer skinnedMeshRenderer)
        {
            var mesh = skinnedMeshRenderer.sharedMesh;
            var bones = skinnedMeshRenderer.bones;
            var numBlendShapes = mesh.blendShapeCount;
            var numPositions = mesh.vertexCount;
            var blendShapeVertices = new Vector3[numPositions];
            var blendShapeNormals = new Vector3[numPositions];
            _originPositions = mesh.vertices.ToArray();
            _originNormals = mesh.normals.ToArray();
            _boneMatrices = bones.Select(bone => bone ? bone.localToWorldMatrix : Matrix4x4.zero).ToArray();
            _bindPoseMatrices = mesh.bindposes.ToArray();
            _deltaPositions = new Vector3[numPositions];
            _deltaNormals = new Vector3[numPositions];
            _boneTransforms = bones;
            _inverseParentTransformMatrix = parentTransform.worldToLocalMatrix;

            // ブレンドシェイプのデルタ値を計算
            for (var blendShapeIndex = 0; blendShapeIndex < numBlendShapes; blendShapeIndex++)
            {
                var weight = skinnedMeshRenderer.GetBlendShapeWeight(blendShapeIndex) * 0.01f;
                if (!(weight > 0.0))
                    continue;
                mesh.GetBlendShapeFrameVertices(blendShapeIndex, 0, blendShapeVertices, blendShapeNormals, null);
                for (var i = 0; i < numPositions; i++)
                {
                    _deltaPositions[i] += blendShapeVertices[i] * weight;
                }

                for (var i = 0; i < numPositions; i++)
                {
                    _deltaNormals[i] += blendShapeNormals[i] * weight;
                }
            }

            // 一意のボーン変換を収集し、逆バインド行列を計算
            foreach (var transform in bones)
            {
                if (!transform || !transform.gameObject.activeInHierarchy ||
                    _transformMap.TryGetValue(transform, out var offset))
                {
                    continue;
                }

                offset = UniqueTransforms.Count;
                UniqueTransforms.Add(transform);
                _transformMap.Add(transform, offset);
                var inverseBindMatrix = transform.worldToLocalMatrix * parentTransform.localToWorldMatrix;
                InverseBindMatrices.Add(inverseBindMatrix.ToNormalizedMatrix());
            }
        }

        /// <summary>
        /// ボーンインデックスとウェイトを解決する
        /// </summary>
        public (ushort, float) Resolve(int index, float weight)
        {
            if (index < 0 || weight == 0.0)
                return (0, 0);
            var transform = _boneTransforms[index];
            if (!transform)
                return (0, 0);
            var offset = _transformMap[transform];
            return ((ushort)offset, weight);
        }

        /// <summary>
        /// 頂点位置を変換する
        /// </summary>
        public System.Numerics.Vector3 ConvertPosition(int index, BoneWeight item)
        {
            var originPosition = _originPositions[index] + _deltaPositions[index];
            var newPosition = Vector3.zero;
            foreach (var (sourceBoneIndex, weight) in new List<(int, float)>
                     {
                         (item.boneIndex0, item.weight0),
                         (item.boneIndex1, item.weight1),
                         (item.boneIndex2, item.weight2),
                         (item.boneIndex3, item.weight3)
                     })
            {
                if (weight == 0)
                    continue;
                var sourceMatrix = GetSourceMatrix(sourceBoneIndex);
                newPosition += sourceMatrix.MultiplyPoint(originPosition) * weight;
            }

            return newPosition.ToVector3WithCoordinateSpace();
        }

        /// <summary>
        /// 法線を変換する
        /// </summary>
        public System.Numerics.Vector3 ConvertNormal(int index, BoneWeight item)
        {
            var originNormal = _originNormals[index] + _deltaNormals[index];
            var newNormal = Vector3.zero;
            foreach (var (sourceBoneIndex, weight) in new List<(int, float)>
                     {
                         (item.boneIndex0, item.weight0),
                         (item.boneIndex1, item.weight1),
                         (item.boneIndex2, item.weight2),
                         (item.boneIndex3, item.weight3)
                     })
            {
                if (weight == 0)
                    continue;
                var sourceMatrix = GetSourceMatrix(sourceBoneIndex);
                newNormal += sourceMatrix.MultiplyVector(originNormal) * weight;
            }

            return newNormal.normalized.ToVector3WithCoordinateSpace();
        }

        private Matrix4x4 GetSourceMatrix(int sourceBoneIndex)
        {
            var sourceMatrix = _inverseParentTransformMatrix * _boneMatrices[sourceBoneIndex] *
                               _bindPoseMatrices[sourceBoneIndex];
            return sourceMatrix;
        }
    }
}
