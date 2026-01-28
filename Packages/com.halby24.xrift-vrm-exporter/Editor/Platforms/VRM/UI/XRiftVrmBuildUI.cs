// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-License-Identifier: MPL-2.0

#nullable enable

#if XRIFT_HAS_NDMF_PLATFORM
using System.IO;
using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using XRift.VrmExporter.Shared.Core;
using XRift.VrmExporter.Platforms.VRM.Core;
using XRift.VrmExporter.Platforms.VRM.Components;

namespace XRift.VrmExporter.Platforms.VRM.UI
{
    /// <summary>
    /// XRift VRM ビルドUI
    /// NDMF コンソールに表示されるビルドコントロール
    /// </summary>
    internal class XRiftVrmBuildUI : XRiftBaseBuildUI
    {
        protected override string GetFileExtension() => "vrm";
        protected override string GetUITitle() => "XRift VRM Exporter";
        protected override string GetBuildButtonText() => "Build VRM";

        protected override void OnAvatarRootChanged()
        {
            LoadOutputPathFromDescriptor();
        }

        private void LoadOutputPathFromDescriptor()
        {
            if (_avatarRoot == null || _outputPathField == null) return;

            var descriptor = _avatarRoot.GetComponent<XRiftVrmDescriptor>();
            if (descriptor != null && !string.IsNullOrEmpty(descriptor.OutputPath))
            {
                _outputPathField.value = descriptor.OutputPath;
            }
        }

        protected override void SaveOutputPathToDescriptor(string path)
        {
            if (_avatarRoot == null) return;

            var descriptor = _avatarRoot.GetComponent<XRiftVrmDescriptor>();
            if (descriptor == null)
            {
                descriptor = Undo.AddComponent<XRiftVrmDescriptor>(_avatarRoot);
            }

            Undo.RecordObject(descriptor, "Set VRM Output Path");
            descriptor.OutputPath = path;
            EditorUtility.SetDirty(descriptor);
            EditorSceneManager.MarkSceneDirty(_avatarRoot.scene);
        }

        protected override void OnBuildClicked()
        {
            if (_avatarRoot == null) return;

            var outputPath = _outputPathField?.value;
            if (string.IsNullOrEmpty(outputPath))
            {
                _statusLabel!.text = "出力先を指定してください";
                return;
            }

            Debug.Log($"[XRift VRM] Building VRM for: {_avatarRoot.name}");
            _statusLabel!.text = "ビルド中...";

            // クローンを作成してビルド
            GameObject? clone = null;
            try
            {
                clone = Object.Instantiate(_avatarRoot);
                clone.name = _avatarRoot.name;

                using var scope = new AmbientPlatform.Scope(XRiftVrmPlatform.Instance);
                var buildContext = AvatarProcessor.ProcessAvatar(clone, XRiftVrmPlatform.Instance);

                // エクスポート結果を取得
                var state = buildContext.GetState<XRiftBuildState>();
                if (state.ExportedData != null && state.ExportedData.Length > 0)
                {
                    File.WriteAllBytes(outputPath, state.ExportedData);
                    Debug.Log($"[XRift VRM] Saved VRM to: {outputPath}");
                    _statusLabel.text = $"保存完了: {Path.GetFileName(outputPath)} ({state.ExportedData.Length / 1024} KB)";

                    // Assetsフォルダ内の場合はReimport
                    var normalizedPath = outputPath!.Replace("\\", "/");
                    if (normalizedPath.Contains("/Assets/"))
                    {
                        var relativePath = "Assets" + normalizedPath.Split(new[] { "/Assets" }, System.StringSplitOptions.None)[1];
                        AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
                    }
                }
                else
                {
                    _statusLabel.text = "ビルド完了（データなし）";
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                _statusLabel.text = $"エラー: {e.Message}";
            }
            finally
            {
                if (clone != null)
                {
                    Object.DestroyImmediate(clone);
                }
            }
        }
    }
}
#endif
