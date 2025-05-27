#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;

namespace TagDebugSystem
{
    public class EnhancedDebugTagConfigWindow : EditorWindow
    {
        private DebugTagSettingsAsset asset;
        private Vector2 scrollPos;
        private string searchQuery = "";
        private bool showByCategory = true;

        private int selectedPresetIdx = 0;
        private string newPresetName = "";
        private Dictionary<string, bool> categoryFoldouts = new Dictionary<string, bool>();

        [MenuItem("Tools/Enhanced Tag Debug Config")]
        public static void Open()
        {
            GetWindow<EnhancedDebugTagConfigWindow>("Enhanced Tag Debug")
                .minSize = new Vector2(450, 500);
        }

        private void OnEnable()
        {
            // Load runtime cache & categories
            DebugTagConfig.Initialize();

            // Grab or error out if missing
            asset = DebugTagSettingsAsset.Instance;
            if (asset == null) return;

            // Auto-seed any tags you’ve added to the project
            bool dirty = false;
            foreach (var t in InternalEditorUtility.tags)
            {
                if (!asset.settings.Any(s => s.tagName == t))
                {
                    asset.settings.Add(new DebugTagSetting()
                    {
                        tagName = t,
                        color = Color.white,
                        isActive = true,
                        isEssential = false
                    });
                    dirty = true;
                }
            }
            if (dirty)
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            GUILayout.Space(6);
            DrawPresetControls();
            GUILayout.Space(6);
            showByCategory = EditorGUILayout.Toggle("Group by Category", showByCategory);
            GUILayout.Space(6);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            var tags = InternalEditorUtility.tags
                        .Where(t => string.IsNullOrEmpty(searchQuery) || t.ToLower().Contains(searchQuery.ToLower()))
                        .OrderBy(t => t);

            if (showByCategory)
                DrawTagsByCategory(tags);
            else
                foreach (var tag in tags) DrawTagEntry(tag);

            EditorGUILayout.EndScrollView();

            // If anything changed, mark the SO dirty
            if (GUI.changed)
            {
                EditorUtility.SetDirty(asset);
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("All ON", EditorStyles.toolbarButton))
                ApplyToAll(s => s.isActive = true);
            if (GUILayout.Button("All OFF", EditorStyles.toolbarButton))
                ApplyToAll(s => s.isActive = false);
            if (GUILayout.Button("Essentials Only", EditorStyles.toolbarButton))
                ApplyToAll(s => s.isActive = s.isEssential);
            if (GUILayout.Button("Random Colors", EditorStyles.toolbarButton))
                ApplyToAll(s => s.color = new Color(Random.value, Random.value, Random.value));

            GUILayout.FlexibleSpace();
            GUILayout.Label("Search:", EditorStyles.toolbarButton, GUILayout.Width(50));
            searchQuery = GUILayout.TextField(searchQuery, EditorStyles.toolbarTextField, GUILayout.Width(150));
            if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
                searchQuery = "";
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPresetControls()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Presets", EditorStyles.boldLabel);

            var names = asset.presets.Select(p => p.presetName).ToList();
            if (names.Count == 0)
                EditorGUILayout.HelpBox("No presets saved.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = names.Count > 0;
            selectedPresetIdx = EditorGUILayout.Popup(selectedPresetIdx, names.ToArray(), GUILayout.Width(150));
            if (GUILayout.Button("Load", GUILayout.Width(50))) LoadPreset(names[selectedPresetIdx]);
            if (GUILayout.Button("Delete", GUILayout.Width(50))) DeletePreset(names[selectedPresetIdx]);
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            newPresetName = GUILayout.TextField(newPresetName, GUILayout.Width(150));
            GUI.enabled = !string.IsNullOrWhiteSpace(newPresetName) && !names.Contains(newPresetName);
            if (GUILayout.Button("Save As", GUILayout.Width(60)))
            {
                SavePreset(newPresetName);
                newPresetName = "";
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawTagsByCategory(IEnumerable<string> tags)
        {
            var grouped = tags.GroupBy(t => Logger.GetCategoryForTag(t));
            foreach (var grp in grouped.OrderBy(g => g.Key))
            {
                if (!categoryFoldouts.ContainsKey(grp.Key))
                    categoryFoldouts[grp.Key] = true;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                categoryFoldouts[grp.Key] = EditorGUILayout.Foldout(categoryFoldouts[grp.Key], grp.Key, true);

                GUI.enabled = categoryFoldouts[grp.Key];
                if (GUILayout.Button("All ON", GUILayout.Width(60))) SetCategory(grp.Key, true);
                if (GUILayout.Button("All OFF", GUILayout.Width(60))) SetCategory(grp.Key, false);
                if (GUILayout.Button("Random", GUILayout.Width(60))) RandomizeCategory(grp.Key);
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
                if (categoryFoldouts[grp.Key])
                {
                    EditorGUI.indentLevel++;
                    foreach (var tag in grp.OrderBy(t => t))
                        DrawTagEntry(tag);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawTagEntry(string tag)
        {
            var e = asset.settings.First(s => s.tagName == tag);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(tag, GUILayout.Width(120));
            if (!showByCategory)
                EditorGUILayout.LabelField(Logger.GetCategoryForTag(tag), GUILayout.Width(80));

            // Color
            EditorGUI.BeginChangeCheck();
            e.color = EditorGUILayout.ColorField(e.color, GUILayout.Width(40));
            if (EditorGUI.EndChangeCheck()) GUI.changed = true;

            // Active
            EditorGUI.BeginChangeCheck();
            e.isActive = EditorGUILayout.ToggleLeft("Active", e.isActive, GUILayout.Width(60));
            if (EditorGUI.EndChangeCheck()) GUI.changed = true;

            // Essential
            EditorGUI.BeginChangeCheck();
            e.isEssential = EditorGUILayout.ToggleLeft("Essential", e.isEssential, GUILayout.Width(80));
            if (EditorGUI.EndChangeCheck()) GUI.changed = true;

            // “Only This” & “Reset”
            if (GUILayout.Button("Only This", GUILayout.Width(70)))
            {
                ApplyToAll(x => x.isActive = false);
                e.isActive = true;
                GUI.changed = true;
            }
            if (GUILayout.Button("Reset", GUILayout.Width(50)))
            {
                e.color = Color.white;
                e.isActive = true;
                e.isEssential = false;
                GUI.changed = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ApplyToAll(System.Action<DebugTagSetting> act)
        {
            foreach (var s in asset.settings) act(s);
            GUI.changed = true;
        }

        private void SetCategory(string category, bool on)
        {
            foreach (var s in asset.settings)
                if (Logger.GetCategoryForTag(s.tagName) == category)
                    s.isActive = on;
            GUI.changed = true;
        }

        private void RandomizeCategory(string category)
        {
            foreach (var s in asset.settings)
                if (Logger.GetCategoryForTag(s.tagName) == category)
                    s.color = new Color(Random.value, Random.value, Random.value);
            GUI.changed = true;
        }

        private void SavePreset(string name)
        {
            var preset = asset.presets.FirstOrDefault(p => p.presetName == name);
            if (preset == null)
            {
                preset = new DebugTagPreset() { presetName = name };
                asset.presets.Add(preset);
            }
            preset.entries.Clear();
            foreach (var s in asset.settings)
            {
                preset.entries.Add(new TagPresetEntry()
                {
                    tagName = s.tagName,
                    color = s.color,
                    isActive = s.isActive,
                    isEssential = s.isEssential
                });
            }
            GUI.changed = true;
        }

        private void LoadPreset(string name)
        {
            var preset = asset.presets.FirstOrDefault(p => p.presetName == name);
            if (preset == null) return;
            foreach (var e in preset.entries)
            {
                var s = asset.settings.First(x => x.tagName == e.tagName);
                s.color = e.color;
                s.isActive = e.isActive;
                s.isEssential = e.isEssential;
            }
            GUI.changed = true;
        }

        private void DeletePreset(string name)
        {
            var p = asset.presets.FirstOrDefault(x => x.presetName == name);
            if (p != null) asset.presets.Remove(p);
            selectedPresetIdx = 0;
            GUI.changed = true;
        }
    }
}
#endif
