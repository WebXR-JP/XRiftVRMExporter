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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UniGLTF;
using UniVRM10;
using UnityEditor;
using UnityEngine;
using XRift.VrmExporter.Components;
using XRift.VrmExporter.Utils;
using Debug = UnityEngine.Debug;

namespace XRift.VrmExporter.Core
{
    /// <summary>
    /// VRM 1.0エクスポーターのメインクラス（UniVRM統合版）
    /// </summary>
    internal sealed class XRiftVrmExporter : IDisposable
    {

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

        private readonly GameObject _gameObject;
        private readonly IAssetSaver _assetSaver;
        private readonly IReadOnlyList<MaterialVariant> _materialVariants;

        public XRiftVrmExporter(GameObject gameObject, IAssetSaver assetSaver,
            IReadOnlyList<MaterialVariant> materialVariants)
        {
            _gameObject = gameObject ?? throw new ArgumentNullException(nameof(gameObject));
            _assetSaver = assetSaver ?? throw new ArgumentNullException(nameof(assetSaver));
            _materialVariants = materialVariants ?? throw new ArgumentNullException(nameof(materialVariants));
        }

        public void Dispose()
        {
            // UniVRM統合により、Disposeするリソースは不要
        }

        /// <summary>
        /// VRM 1.0形式でエクスポートを実行（UniVRM統合版）
        /// </summary>
        /// <param name="stream">出力先ストリーム</param>
        /// <returns>glTF JSONデータ（デバッグ用）</returns>
        public string Export(Stream stream)
        {
            using (var _ = new ScopedProfile("Export"))
            {
                // エクスポート設定を作成
                var settings = CreateExportSettings();

                // VRMメタ情報を取得
                var meta = GetOrCreateVrmMeta();

                byte[] bytes;
                using (var arrayManager = new NativeArrayManager())
                {
                    using (var __ = new ScopedProfile("ModelExporter.Export"))
                    {
                        // UnityヒエラルキーをVrmLib.Modelに変換
                        var converter = new ModelExporter();
                        var model = converter.Export(settings, arrayManager, _gameObject);

                        using (var ___ = new ScopedProfile("ConvertCoordinate"))
                        {
                            // 座標系変換（Unity左手系 → VRM右手系）
                            model.ConvertCoordinate(VrmLib.Coordinates.Vrm1, ignoreVrm: false);
                        }

                        using (var ___ = new ScopedProfile("Vrm10Exporter.Export"))
                        {
                            // Material Exporterを作成
                            var materialExporter = CreateMaterialExporter();
                            var textureSerializer = new RuntimeTextureSerializer();

                            // VRM 1.0エクスポート
                            using (var exporter = new Vrm10Exporter(settings, materialExporter, textureSerializer))
                            {
                                var option = new VrmLib.ExportArgs { sparse = true };
                                exporter.Export(_gameObject, model, converter, option, meta);

                                // バイナリ出力
                                bytes = exporter.Storage.ToGlbBytes();
                            }
                        }
                    }
                }

                // ストリームに書き込み
                stream.Write(bytes, 0, bytes.Length);

                // デバッグ用にJSON文字列を返す（必要に応じて）
                return "{}"; // TODO: 必要に応じてJSONを返す
            }
        }

        /// <summary>
        /// エクスポート設定を作成
        /// </summary>
        private GltfExportSettings CreateExportSettings()
        {
            var packageJsonFile = File.ReadAllText($"Packages/{PackageJson.Name}/package.json");
            var packageJson = PackageJson.LoadFromString(packageJsonFile);

            return new GltfExportSettings
            {
                InverseAxis = Axes.Z,  // VRM 1.0必須
                UseSparseAccessorForMorphTarget = true,  // ファイルサイズ削減
                ExportVertexColor = true,  // 頂点カラーを保持
                // TODO: 他の設定が必要な場合はXRiftVrmDescriptorから取得
            };
        }

        /// <summary>
        /// Material Exporterを作成
        /// </summary>
        private IMaterialExporter CreateMaterialExporter()
        {
            // Material Variantがある場合はカスタム実装を使用
            // TODO: Material Variant対応の実装
            // if (_materialVariants.Count > 0)
            // {
            //     return new XRiftMaterialExporter(_materialVariants);
            // }

            // 標準はUniVRM10のデフォルトMToon Exporter
            return Vrm10MaterialExporterUtility.GetValidVrm10MaterialExporter();
        }

        /// <summary>
        /// VRMメタ情報を取得または作成
        /// </summary>
        private VRM10ObjectMeta GetOrCreateVrmMeta()
        {
            // XRiftVrmDescriptorから取得
            var descriptor = _gameObject.GetComponent<XRiftVrmDescriptor>();
            if (descriptor != null)
            {
                return descriptor.ToVrm10Meta();
            }

            // デフォルトのメタ情報を作成
            // VRM10ObjectMetaはScriptableObjectではないため、newで作成
            var meta = new VRM10ObjectMeta
            {
                Name = _gameObject.name,
                Version = "0.0.0",
                Authors = new List<string> { System.Environment.UserName },
                AvatarPermission = UniGLTF.Extensions.VRMC_vrm.AvatarPermissionType.onlyAuthor,
                CommercialUsage = UniGLTF.Extensions.VRMC_vrm.CommercialUsageType.personalNonProfit,
                CreditNotation = UniGLTF.Extensions.VRMC_vrm.CreditNotationType.required,
                Modification = UniGLTF.Extensions.VRMC_vrm.ModificationType.prohibited,
                Redistribution = false,
            };
            return meta;
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
