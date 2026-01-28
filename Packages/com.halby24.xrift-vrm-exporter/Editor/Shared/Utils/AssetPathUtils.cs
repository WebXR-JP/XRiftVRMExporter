// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-FileCopyrightText: 2024-present hkrn (original ndmf-vrm-exporter)
// SPDX-License-Identifier: MPL-2.0
//
// This file contains code derived from ndmf-vrm-exporter
// https://github.com/hkrn/ndmf-vrm-exporter

#nullable enable

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace XRift.VrmExporter.Shared.Utils
{
    /// <summary>
    /// アセットパス関連のユーティリティ
    /// </summary>
    internal static class AssetPathUtils
    {
        private const string CloneSuffix = "(Clone)";

        public static string BasePath => "Assets/XRift VRM Exporter";

        public static string GetOutputPath(GameObject gameObject)
        {
            return GetBasePath(gameObject, BasePath);
        }

        public static string GetTempPath(GameObject gameObject)
        {
            return GetBasePath(gameObject, FileUtil.GetUniqueTempPathInProject());
        }

        public static string TrimCloneSuffix(string name)
        {
            while (name.EndsWith(CloneSuffix, StringComparison.Ordinal))
            {
                name = name[..^CloneSuffix.Length];
            }

            return name;
        }

        private static string GetBasePath(GameObject gameObject, string basePath)
        {
            var sceneName = !string.IsNullOrEmpty(gameObject.scene.name)
                ? StripInvalidFileNameCharacters(gameObject.scene.name)
                : "Untitled";
            var gameObjectName = TrimCloneSuffix(StripInvalidFileNameCharacters(gameObject.name));
            return $"{basePath}/{sceneName}/{gameObjectName}";
        }

        private static string StripInvalidFileNameCharacters(string name)
        {
            return string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
