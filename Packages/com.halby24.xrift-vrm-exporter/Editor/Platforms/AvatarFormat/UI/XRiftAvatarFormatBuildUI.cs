#nullable enable
#if XRIFT_HAS_NDMF_PLATFORM
using System.IO;
using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;
using UnityEngine;
using XRift.VrmExporter.Shared.Core;
using XRift.VrmExporter.Platforms.AvatarFormat.Core;

namespace XRift.VrmExporter.Platforms.AvatarFormat.UI
{
    internal class XRiftAvatarFormatBuildUI : XRiftBaseBuildUI
    {
        protected override string GetFileExtension() => "xrift";
        protected override string GetFileDialogTitle() => "XRift Avatar Format保存先を選択";
        protected override string GetUITitle() => "XRift Avatar Format Exporter";
        protected override string GetBuildButtonText() => "Build XRIFT";

        protected override void OnBuildClicked()
        {
            if (_avatarRoot == null) return;

            var outputPath = _outputPathField?.value;
            if (string.IsNullOrEmpty(outputPath)) return;

            Debug.Log($"[XRift Avatar Format] Building: {_avatarRoot.name}");

            GameObject? clone = null;
            try
            {
                clone = Object.Instantiate(_avatarRoot);
                clone.name = _avatarRoot.name;

                using var scope = new AmbientPlatform.Scope(XRiftAvatarFormatPlatform.Instance);
                var context = AvatarProcessor.ProcessAvatar(clone, XRiftAvatarFormatPlatform.Instance);

                var state = context.GetState<XRiftBuildState>();
                if (state.ExportedData != null)
                {
                    File.WriteAllBytes(outputPath, state.ExportedData);
                    Debug.Log($"[XRift Avatar Format] Saved: {outputPath}");
                    _statusLabel!.text = $"保存完了: {Path.GetFileName(outputPath)}";
                }
                else
                {
                    _statusLabel!.text = "ビルド完了（データなし）";
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                _statusLabel!.text = $"エラー: {e.Message}";
            }
            finally
            {
                if (clone != null) Object.DestroyImmediate(clone);
            }
        }

        protected override void OnAvatarRootChanged()
        {
            // TODO: UIの更新処理
        }
    }
}
#endif
