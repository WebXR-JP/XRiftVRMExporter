// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-FileCopyrightText: 2024-present hkrn (original ndmf-vrm-exporter)
// SPDX-License-Identifier: MPL-2.0
//
// This file contains code derived from ndmf-vrm-exporter
// https://github.com/hkrn/ndmf-vrm-exporter

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEditor;
using UnityEngine;
using XRift.VrmExporter.Utils;
using XRift.VrmExporter.VRM.Gltf;
using Debug = UnityEngine.Debug;

namespace XRift.VrmExporter.Core
{
    /// <summary>
    /// VRM 1.0エクスポーターのメインクラス
    /// </summary>
    internal sealed class XRiftVrmExporter : IDisposable
    {
        private static readonly string VrmcVrm = "VRMC_vrm";
        private static readonly string VrmcSpringBone = "VRMC_springBone";
        private static readonly string VrmcSpringBoneExtendedCollider = "VRMC_springBone_extended_collider";
        private static readonly string VrmcNodeConstraint = "VRMC_node_constraint";
        private static readonly string VrmcMaterialsMtoon = "VRMC_materials_mtoon";

        public sealed class PackageJson
        {
            public const string Name = "com.halby24.xrift-vrm-exporter";
            public string DisplayName { get; set; } = null!;
            public string Version { get; set; } = null!;

            public static PackageJson LoadFromString(string json)
            {
                return JsonConvert.DeserializeObject<PackageJson>(json, new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    },
                    DefaultValueHandling = DefaultValueHandling.Include,
                    NullValueHandling = NullValueHandling.Ignore,
                })!;
            }
        }

        private sealed class ScopedProfile : IDisposable
        {
            public ScopedProfile(string name)
            {
                _name = $"XRiftVrmExporter.{name}";
                _sw = new Stopwatch();
                _sw.Start();
            }

            public void Dispose()
            {
                _sw.Stop();
                WriteResult();
            }

            [Conditional("DEBUG")]
            private void WriteResult()
            {
                Debug.Log($"{_name}: {_sw.ElapsedMilliseconds}ms");
            }

            private readonly Stopwatch _sw;
            private readonly string _name;
        }

        private sealed class MToonTexture
        {
            public Texture? MainTexture { get; set; }
            public material.TextureInfo? MainTextureInfo { get; set; }
        }

        private readonly GameObject _gameObject;
        private readonly IAssetSaver _assetSaver;
        private readonly IReadOnlyList<MaterialVariant> _materialVariants;
        private readonly exporter.Exporter _exporter;
        private readonly Root _root;
        private readonly SortedSet<string> _extensionsUsed;
        private readonly IDictionary<Material, ObjectID> _materialIDs;
        private readonly IDictionary<Material, MToonTexture> _materialMToonTextures;
        private readonly IDictionary<Transform, ObjectID> _transformNodeIDs;
        private readonly ISet<string> _transformNodeNames;

        public XRiftVrmExporter(GameObject gameObject, IAssetSaver assetSaver,
            IReadOnlyList<MaterialVariant> materialVariants)
        {
            var packageJsonFile = File.ReadAllText($"Packages/{PackageJson.Name}/package.json");
            var packageJson = PackageJson.LoadFromString(packageJsonFile);
            _gameObject = gameObject;
            _assetSaver = assetSaver;
            _materialIDs = new Dictionary<Material, ObjectID>();
            _materialMToonTextures = new Dictionary<Material, MToonTexture>();
            _transformNodeIDs = new Dictionary<Transform, ObjectID>();
            _transformNodeNames = new HashSet<string>();
            _materialVariants = materialVariants;
            _exporter = new exporter.Exporter();
            _root = new Root
            {
                Accessors = new List<accessor.Accessor>(),
                Asset = new asset.Asset
                {
                    Version = "2.0",
                    Generator = $"{packageJson.DisplayName} {packageJson.Version}",
                },
                Buffers = new List<buffer.Buffer>(),
                BufferViews = new List<buffer.BufferView>(),
                Extensions = new Dictionary<string, JToken>(),
                ExtensionsUsed = new List<string>(),
                Images = new List<buffer.Image>(),
                Materials = new List<material.Material>(),
                Meshes = new List<mesh.Mesh>(),
                Nodes = new List<node.Node>(),
                Samplers = new List<material.Sampler>(),
                Scenes = new List<scene.Scene>(),
                Scene = new ObjectID(0),
                Skins = new List<node.Skin>(),
                Textures = new List<material.Texture>(),
            };
            _root.Scenes.Add(new scene.Scene()
            {
                Nodes = new List<ObjectID> { new(0) },
            });
            _extensionsUsed = new SortedSet<string>();
        }

        public void Dispose()
        {
            _exporter.Dispose();
        }

        /// <summary>
        /// VRM 1.0形式でエクスポートを実行
        /// </summary>
        /// <param name="stream">出力先ストリーム</param>
        /// <returns>glTF JSONデータ</returns>
        public string Export(Stream stream)
        {
            var rootTransform = _gameObject.transform;
            var translation = System.Numerics.Vector3.Zero;
            var rotation = System.Numerics.Quaternion.Identity;
            var scale = rootTransform.localScale.ToVector3();
            var rootNode = new node.Node
            {
                Name = new UnicodeString(AssetPathUtils.TrimCloneSuffix(rootTransform.name)),
                Children = new List<ObjectID>(),
                Translation = translation,
                Rotation = rotation,
                Scale = scale,
            };
            var nodes = _root.Nodes!;
            var nodeID = new ObjectID((uint)nodes.Count);
            _transformNodeIDs.Add(rootTransform, nodeID);
            _transformNodeNames.Add(rootTransform.name);
            nodes.Add(rootNode);

            // TODO: コンポーネントからの設定読み込み
            bool makeAllNodeNamesUnique = true;

            using (var _ = new ScopedProfile(nameof(RetrieveAllTransforms)))
            {
                RetrieveAllTransforms(rootTransform, makeAllNodeNamesUnique);
            }

            using (var _ = new ScopedProfile(nameof(RetrieveAllNodes)))
            {
                RetrieveAllNodes(rootTransform);
            }

            // TODO: RetrieveAllMeshRenderers, ConvertAllMaterialVariants, ExportAllVrmExtensions

            _root.Buffers!.Add(new buffer.Buffer
            {
                ByteLength = _exporter.Length,
            });
            _root.ExtensionsUsed = _extensionsUsed.ToList();
            _root.Normalize();
            var json = Document.SaveAsString(_root);
            _exporter.Export(json, stream);
            return json;
        }

        private void RetrieveAllTransforms(Transform parent, bool uniqueNodeName)
        {
            foreach (Transform child in parent)
            {
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var nodeName = AssetPathUtils.TrimCloneSuffix(child.name);
                if (uniqueNodeName && _transformNodeNames.Contains(nodeName))
                {
                    var renamedNodeName = nodeName;
                    var i = 1;
                    while (_transformNodeNames.Contains(renamedNodeName))
                    {
                        renamedNodeName = $"{nodeName}_{i}";
                        i++;
                    }

                    nodeName = renamedNodeName;
                }

                var translation = child.localPosition.ToVector3WithCoordinateSpace();
                var rotation = child.localRotation.ToQuaternionWithCoordinateSpace();
                var scale = child.localScale.ToVector3();
                var node = new node.Node
                {
                    Name = new UnicodeString(nodeName),
                    Children = new List<ObjectID>(),
                    Translation = translation,
                    Rotation = rotation,
                    Scale = scale,
                };
                var nodes = _root.Nodes!;
                var nodeID = new ObjectID((uint)nodes.Count);
                nodes.Add(node);
                _transformNodeIDs.Add(child, nodeID);
                _transformNodeNames.Add(nodeName);
                RetrieveAllTransforms(child, uniqueNodeName);
            }
        }

        private void RetrieveAllNodes(Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (_transformNodeIDs.TryGetValue(parent, out var parentNodeID) &&
                    _transformNodeIDs.TryGetValue(child, out var childNodeID))
                {
                    var parentNode = _root.Nodes![(int)parentNodeID.ID];
                    parentNode.Children!.Add(childNodeID);
                }

                RetrieveAllNodes(child);
            }
        }
    }

    /// <summary>
    /// アセット保存インターフェース
    /// </summary>
    public interface IAssetSaver
    {
        void SaveAsset(UnityEngine.Object asset, string name);
        T? GetSavedAsset<T>(string name) where T : UnityEngine.Object;
    }

    /// <summary>
    /// 一時ディレクトリへのアセット保存実装
    /// </summary>
    internal class TempAssetSaver : IAssetSaver
    {
        private readonly string _basePath;
        private readonly Dictionary<string, UnityEngine.Object> _assets = new();

        public TempAssetSaver(string basePath)
        {
            _basePath = basePath;
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public void SaveAsset(UnityEngine.Object asset, string name)
        {
            var path = Path.Combine(_basePath, name);
            AssetDatabase.CreateAsset(asset, path);
            _assets[name] = asset;
        }

        public T? GetSavedAsset<T>(string name) where T : UnityEngine.Object
        {
            return _assets.TryGetValue(name, out var asset) ? asset as T : null;
        }
    }
}
