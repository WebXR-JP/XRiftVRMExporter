#nullable enable
using UnityEngine;

namespace XRift.VrmExporter.Platforms.AvatarFormat.Components
{
    /// <summary>
    /// XRift Avatar Format エクスポート設定
    /// エディター専用コンポーネント（Runtimeには含まれません）
    /// </summary>
    [AddComponentMenu("XRift VRM Exporter/Avatar Format Export Description")]
    [DisallowMultipleComponent]
    public sealed class XRiftAvatarFormatDescriptor : MonoBehaviour
    {
        [SerializeField] internal string outputPath = "";

        public string OutputPath
        {
            get => outputPath;
            set => outputPath = value;
        }

        // TODO: Avatar Format固有の設定を追加
    }
}
