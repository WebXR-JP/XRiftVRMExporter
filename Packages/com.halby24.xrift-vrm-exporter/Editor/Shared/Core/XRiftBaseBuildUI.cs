// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-License-Identifier: MPL-2.0

#nullable enable

#if XRIFT_HAS_NDMF_PLATFORM
using System.IO;
using nadena.dev.ndmf.platform;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace XRift.VrmExporter.Shared.Core
{
    /// <summary>
    /// XRift ビルドUI基底クラス
    /// NDMF コンソールに表示されるビルドコントロールの共通機能を提供
    /// </summary>
    public abstract class XRiftBaseBuildUI : BuildUIElement
    {
        protected GameObject? _avatarRoot;
        protected TextField? _outputPathField;
        protected Button? _browseButton;
        protected Button? _buildButton;
        protected Label? _statusLabel;

        public override GameObject AvatarRoot
        {
            get => _avatarRoot!;
            set
            {
                _avatarRoot = value;
                OnAvatarRootChanged();
                UpdateButtonState();
            }
        }

        protected XRiftBaseBuildUI()
        {
            BuildUI();
        }

        /// <summary>
        /// ファイル拡張子を取得（例: "vrm", "xrift"）
        /// </summary>
        protected abstract string GetFileExtension();

        /// <summary>
        /// ファイル選択ダイアログのタイトルを取得
        /// </summary>
        protected virtual string GetFileDialogTitle() => $"{GetFileExtension().ToUpper()}保存先を選択";

        /// <summary>
        /// ビルドボタンがクリックされたときの処理
        /// </summary>
        protected abstract void OnBuildClicked();

        /// <summary>
        /// アバタールートが変更されたときの処理
        /// </summary>
        protected abstract void OnAvatarRootChanged();

        private void BuildUI()
        {
            // ルートコンテナ
            var container = new VisualElement();
            container.style.paddingTop = 8;
            container.style.paddingBottom = 8;
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;

            // タイトル
            var title = new Label(GetUITitle());
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
                text = GetBuildButtonText()
            };
            _buildButton.SetEnabled(false);
            container.Add(_buildButton);

            Add(container);
        }

        /// <summary>
        /// UIタイトルを取得（派生クラスでカスタマイズ可能）
        /// </summary>
        protected virtual string GetUITitle() => "XRift Exporter";

        /// <summary>
        /// ビルドボタンのテキストを取得（派生クラスでカスタマイズ可能）
        /// </summary>
        protected virtual string GetBuildButtonText() => $"Build {GetFileExtension().ToUpper()}";

        protected void UpdateButtonState()
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
            var extension = GetFileExtension();
            var defaultName = _avatarRoot != null ? $"{_avatarRoot.name}.{extension}" : $"avatar.{extension}";
            var path = EditorUtility.SaveFilePanel(
                GetFileDialogTitle(),
                "",
                defaultName,
                extension);

            if (!string.IsNullOrEmpty(path))
            {
                _outputPathField!.value = path;
                SaveOutputPathToDescriptor(path);
                UpdateButtonState();
            }
        }

        /// <summary>
        /// 出力パスをDescriptorに保存（派生クラスで実装）
        /// </summary>
        protected virtual void SaveOutputPathToDescriptor(string path)
        {
            // 派生クラスで実装
        }
    }
}
#endif
