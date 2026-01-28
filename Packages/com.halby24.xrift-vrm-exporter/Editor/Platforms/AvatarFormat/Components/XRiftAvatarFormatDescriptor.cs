#nullable enable
using nadena.dev.ndmf;
using UnityEngine;

namespace XRift.VrmExporter.Platforms.AvatarFormat.Components
{
    [AddComponentMenu("XRift VRM Exporter/Avatar Format Export Description")]
    [DisallowMultipleComponent]
    public sealed class XRiftAvatarFormatDescriptor : MonoBehaviour, INDMFEditorOnly
    {
        [NotKeyable] [SerializeField] internal string outputPath = "";

        public string OutputPath
        {
            get => outputPath;
            set => outputPath = value;
        }

        // TODO: Avatar Format固有の設定を追加
    }
}
