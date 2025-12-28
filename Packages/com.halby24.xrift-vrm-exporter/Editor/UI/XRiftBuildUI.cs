#nullable enable

#if XRIFT_HAS_NDMF_PLATFORM
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
        }

        private void OnBuildClicked()
        {
            if (_avatarRoot == null) return;

            // TODO: VRM ビルド処理を実装
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

                // TODO: VRM エクスポート処理
                // var state = buildContext.GetState<XRiftBuildState>();

                _statusLabel.text = "ビルド完了";
                _saveAsButton?.SetEnabled(true);
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
            // TODO: VRM ファイルを保存
            var path = EditorUtility.SaveFilePanel(
                "Save VRM",
                "",
                _avatarRoot?.name + ".vrm",
                "vrm");

            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log($"[XRift VRM] Saving to: {path}");
                // TODO: ファイル保存処理
            }
        }
    }
}
#endif
