// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-License-Identifier: MPL-2.0

#nullable enable

using System;
using System.IO;
using UnityEngine;

namespace XRift.VrmExporter.Shared.Core
{
    /// <summary>
    /// XRift エクスポーター基底クラス
    /// </summary>
    public abstract class XRiftBaseExporter : IDisposable
    {
        protected readonly GameObject _gameObject;
        protected readonly IAssetSaver _assetSaver;

        protected XRiftBaseExporter(GameObject gameObject, IAssetSaver assetSaver)
        {
            _gameObject = gameObject ?? throw new ArgumentNullException(nameof(gameObject));
            _assetSaver = assetSaver ?? throw new ArgumentNullException(nameof(assetSaver));
        }

        /// <summary>
        /// エクスポートを実行
        /// </summary>
        /// <param name="stream">出力先ストリーム</param>
        public abstract void Export(Stream stream);

        public virtual void Dispose()
        {
            // 派生クラスで必要に応じてリソースを解放
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
}
