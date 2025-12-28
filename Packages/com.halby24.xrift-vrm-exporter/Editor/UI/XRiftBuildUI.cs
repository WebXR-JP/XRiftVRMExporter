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
        private Button? _buildButton;
        private Button? _saveAsButton;
        private Label? _statusLabel;
        private byte[]? _lastExportedVrmData;

        public override GameObject AvatarRoot
        {
            get => _avatarRoot!;
            set
            {
                _avatarRoot = value;
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

            // ボタンコンテナ
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;

            // ビルドボタン
            _buildButton = new Button(OnBuildClicked)
            {
                text = "Build VRM"
            };
            _buildButton.style.flexGrow = 1;
            _buildButton.SetEnabled(false);
            buttonContainer.Add(_buildButton);

            // 名前を付けて保存ボタン
            _saveAsButton = new Button(OnSaveAsClicked)
            {
                text = "Save As..."
            };
            _saveAsButton.style.marginLeft = 4;
            _saveAsButton.SetEnabled(false);
            buttonContainer.Add(_saveAsButton);

            container.Add(buttonContainer);
            Add(container);
        }

        private void UpdateButtonState()
        {
            var hasAvatar = _avatarRoot != null;
            _buildButton?.SetEnabled(hasAvatar);

            if (_statusLabel != null)
            {
                _statusLabel.text = hasAvatar
                    ? $"選択中: {_avatarRoot!.name}"
                    : "アバターを選択してください";
            }

            // VRMデータがない場合はSave Asを無効化
            if (_lastExportedVrmData == null)
            {
                _saveAsButton?.SetEnabled(false);
            }
        }

        private void OnBuildClicked()
        {
            if (_avatarRoot == null) return;

            Debug.Log($"[XRift VRM] Building VRM for: {_avatarRoot.name}");
            _statusLabel!.text = "ビルド中...";
            _lastExportedVrmData = null;
            _saveAsButton?.SetEnabled(false);

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
                    _lastExportedVrmData = state.ExportedVrmData;
                    _statusLabel.text = $"ビルド完了 ({_lastExportedVrmData.Length / 1024} KB)";
                    _saveAsButton?.SetEnabled(true);
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

        private void OnSaveAsClicked()
        {
            if (_lastExportedVrmData == null || _lastExportedVrmData.Length == 0)
            {
                _statusLabel!.text = "保存するVRMデータがありません";
                return;
            }

            var path = EditorUtility.SaveFilePanel(
                "Save VRM",
                "",
                _avatarRoot?.name + ".vrm",
                "vrm");

            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    File.WriteAllBytes(path, _lastExportedVrmData);
                    Debug.Log($"[XRift VRM] Saved VRM to: {path}");
                    _statusLabel!.text = $"保存完了: {Path.GetFileName(path)}";
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    _statusLabel!.text = $"保存エラー: {e.Message}";
                }
            }
        }
    }
}
#endif
