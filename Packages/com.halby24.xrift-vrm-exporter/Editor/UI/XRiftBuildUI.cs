// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-License-Identifier: MPL-2.0

#nullable enable

#if XRIFT_HAS_NDMF_PLATFORM
using System.IO;
using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using XRift.VrmExporter.Components;
using XRift.VrmExporter.Core;

namespace XRift.VrmExporter.UI
{
    /// <summary>
    /// XRift VRM ビルドUI
    /// NDMF コンソールに表示されるビルドコントロール
    /// </summary>
    internal class XRiftBuildUI : BuildUIElement
    {
        private GameObject? _avatarRoot;
        private TextField? _outputPathField;
        private Button? _browseButton;
        private Button? _buildButton;
        private Label? _statusLabel;

        public override GameObject AvatarRoot
        {
            get => _avatarRoot!;
            set
            {
                _avatarRoot = value;
                LoadOutputPathFromDescriptor();
                UpdateButtonState();
            }
        }

        public XRiftBuildUI()
        {
            // UIを構築
            BuildUI();
        }

        private void BuildUI()
        {
            // ルートコンテナ
            var container = new VisualElement();
            container.style.paddingTop = 8;
            container.style.paddingBottom = 8;
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;

            // タイトル
            var title = new Label("XRift VRM Exporter");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 8;
            container.Add(title);

            // ステータスラベル
            _statusLabel = new Label("アバターを選択してください");
            _statusLabel.style.marginBottom = 8;
            container.Add(_statusLabel);

            // 出力パスコンテナ
            var pathContainer = new VisualElement();
            pathContainer.style.flexDirection = FlexDirection.Row;
            pathContainer.style.marginBottom = 8;

            // 出力パステキストフィールド
            _outputPathField = new TextField("出力先");
            _outputPathField.style.flexGrow = 1;
            _outputPathField.value = "";
            pathContainer.Add(_outputPathField);

            // 参照ボタン
            _browseButton = new Button(OnBrowseClicked)
            {
                text = "..."
            };
            _browseButton.style.width = 30;
            _browseButton.style.marginLeft = 4;
            pathContainer.Add(_browseButton);

            container.Add(pathContainer);

            // ビルドボタン
            _buildButton = new Button(OnBuildClicked)
            {
                text = "Build VRM"
            };
            _buildButton.SetEnabled(false);
            container.Add(_buildButton);

            Add(container);
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

        private void SaveOutputPathToDescriptor(string path)
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

        private void UpdateButtonState()
        {
            var hasAvatar = _avatarRoot != null;
            var hasPath = !string.IsNullOrEmpty(_outputPathField?.value);
            _buildButton?.SetEnabled(hasAvatar && hasPath);

            if (_statusLabel != null)
            {
                _statusLabel.text = hasAvatar
                    ? $"選択中: {_avatarRoot!.name}"
                    : "アバターを選択してください";
            }
        }

        private void OnBrowseClicked()
        {
            var defaultName = _avatarRoot != null ? _avatarRoot.name + ".vrm" : "avatar.vrm";
            var path = EditorUtility.SaveFilePanel(
                "VRM保存先を選択",
                "",
                defaultName,
                "vrm");

            if (!string.IsNullOrEmpty(path))
            {
                _outputPathField!.value = path;
                SaveOutputPathToDescriptor(path);
                UpdateButtonState();
            }
        }

        private void OnBuildClicked()
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
                if (state.ExportedVrmData != null && state.ExportedVrmData.Length > 0)
                {
                    File.WriteAllBytes(outputPath, state.ExportedVrmData);
                    Debug.Log($"[XRift VRM] Saved VRM to: {outputPath}");
                    _statusLabel.text = $"保存完了: {Path.GetFileName(outputPath)} ({state.ExportedVrmData.Length / 1024} KB)";

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
