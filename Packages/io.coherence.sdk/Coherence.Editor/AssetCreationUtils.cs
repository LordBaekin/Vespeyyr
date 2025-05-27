// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEngine;
    using System.IO;
    using UnityEditor;

    internal static class AssetCreationUtils
    {
        private static string GetCurrentFolder()
        {
            string filePath;
            if (Selection.assetGUIDs.Length == 0)
            {
                filePath = "Assets";
            }
            else
            {
                filePath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);

                string fileExtension = Path.GetExtension(filePath);
                if (fileExtension != string.Empty)
                {
                    filePath = Path.GetDirectoryName(filePath);
                }
            }

            return filePath;
        }

        [MenuItem("Assets/Create/coherence/Schema", false, 1000)]
        public static void CreateSchemaFile()
        {
            Analytics.Capture(Analytics.Events.MenuItem, ("menu", "create"), ("item", "schema"));

            string folder = GetCurrentFolder();
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/NewSchema." + Constants.schemaExtension);

            SchemaAsset temp = ScriptableObject.CreateInstance<SchemaAsset>();
            string tempPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(temp));
            Texture2D icon = AssetDatabase.GetCachedIcon(tempPath) as Texture2D;
            Object.DestroyImmediate(temp);

            ProjectWindowUtil.CreateAssetWithContent(path, string.Empty, icon);
        }
    }
}
