using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TagDebugSystem
{
    [System.Serializable]
    public class DebugTagSetting
    {
        public string tagName;
        public Color color = Color.white;
        public bool isActive = true;
        public bool isEssential = false;
    }

    [System.Serializable]
    public class TagPresetEntry
    {
        public string tagName;
        public Color color = Color.white;
        public bool isActive = true;
        public bool isEssential = false;
    }

    [System.Serializable]
    public class DebugTagPreset
    {
        public string presetName;
        public List<TagPresetEntry> entries = new List<TagPresetEntry>();
    }

    [CreateAssetMenu(menuName = "Debug/Debug Tag Settings", fileName = "DebugTagSettings")]
    public class DebugTagSettingsAsset : ScriptableObject
    {
        public List<DebugTagSetting> settings = new List<DebugTagSetting>();
        public List<DebugTagPreset> presets = new List<DebugTagPreset>();

        // Used at runtime & in editor to fetch the asset from Resources/
        private static DebugTagSettingsAsset _instance;
        public static DebugTagSettingsAsset Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<DebugTagSettingsAsset>("DebugTagSettings");
                    if (_instance == null)
                        Debug.LogError(
                            "🔧 DebugTagSettings.asset not found in Resources!\n" +
                            "Use Debug → Create Debug Tag Settings Asset to generate it."
                        );
                }
                return _instance;
            }
        }

#if UNITY_EDITOR
        private const string k_AssetPath = "Assets/Resources/DebugTagSettings.asset";

        [MenuItem("Debug/Create Debug Tag Settings Asset")]
        public static void CreateAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DebugTagSettingsAsset>(k_AssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DebugTagSettingsAsset>();
                AssetDatabase.CreateAsset(asset, k_AssetPath);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Debug Tag Settings",
                    $"Asset already exists at {k_AssetPath}",
                    "OK"
                );
            }
        }
#endif
    }
}
