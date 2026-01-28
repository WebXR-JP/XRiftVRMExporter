#nullable enable
using System.IO;
using UnityEngine;
using XRift.VrmExporter.Shared.Core;

namespace XRift.VrmExporter.Platforms.AvatarFormat.Core
{
    internal sealed class XRiftAvatarFormatExporter : XRiftBaseExporter
    {
        public XRiftAvatarFormatExporter(GameObject gameObject, IAssetSaver assetSaver)
            : base(gameObject, assetSaver)
        {
        }

        public override void Export(Stream stream)
        {
            // スタブ実装: JSONプレースホルダー
            var json = System.Text.Encoding.UTF8.GetBytes(
                $"{{\"format\":\"xrift-avatar\",\"name\":\"{_gameObject.name}\",\"version\":\"0.0.1\"}}"
            );
            stream.Write(json, 0, json.Length);
            Debug.Log($"[XRift Avatar Format] Exported stub for: {_gameObject.name}");
        }
    }
}
