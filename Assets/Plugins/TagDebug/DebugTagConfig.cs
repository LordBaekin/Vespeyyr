using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TagDebugSystem
{
    public static class DebugTagConfig
    {
        public class RuntimeSetting
        {
            public Color color;
            public bool isActive;
            public bool isEssential;
            public bool isDynamicallyCreated;

            public RuntimeSetting(Color color = default, bool isActive = true, bool isEssential = false, bool isDynamicallyCreated = false)
            {
                this.color = color == default ? GetRandomTagColor() : color;
                this.isActive = isActive;
                this.isEssential = isEssential;
                this.isDynamicallyCreated = isDynamicallyCreated;
            }
        }

        private static Dictionary<string, RuntimeSetting> _cache;
        private static bool _isInitialized = false;
        private static bool _enableDebugLogging = true;
        private static readonly object _lock = new object();

        // Predefined colors for dynamically created tags
        private static readonly Color[] _tagColors = new Color[]
        {
            Color.cyan,
            Color.yellow,
            Color.magenta,
            Color.green,
            new Color(1f, 0.5f, 0f), // orange
            new Color(0.5f, 0f, 1f), // purple
            new Color(0f, 1f, 0.5f), // teal
            new Color(1f, 0f, 0.5f), // pink
            new Color(0.5f, 1f, 0f), // lime
            new Color(0f, 0.5f, 1f), // light blue
        };
        private static int _colorIndex = 0;

        /// <summary>
        /// Enable or disable debug logging for the DebugTagConfig system
        /// </summary>
        public static void SetDebugLogging(bool enabled)
        {
            _enableDebugLogging = enabled;
            DebugLog($"DebugTagConfig logging {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Internal debug logging that can be toggled
        /// </summary>
        private static void DebugLog(string message)
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[DebugTagConfig] {message}");
            }
        }

        /// <summary>
        /// Internal warning logging
        /// </summary>
        private static void DebugWarning(string message)
        {
            if (_enableDebugLogging)
            {
                Debug.LogWarning($"[DebugTagConfig] {message}");
            }
        }

        /// <summary>
        /// Get a random color for dynamically created tags
        /// </summary>
        private static Color GetRandomTagColor()
        {
            Color color = _tagColors[_colorIndex % _tagColors.Length];
            _colorIndex++;
            return color;
        }

        /// <summary>
        /// Load all tag settings from the SO into memory with extensive error handling.
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_isInitialized)
                {
                    DebugLog("Already initialized, skipping...");
                    return;
                }

                DebugLog("Initializing DebugTagConfig...");

                _cache = new Dictionary<string, RuntimeSetting>();

                try
                {
                    var asset = DebugTagSettingsAsset.Instance;
                    if (asset != null)
                    {
                        DebugLog($"Found DebugTagSettingsAsset with {asset.settings?.Count ?? 0} settings");

                        if (asset.settings != null)
                        {
                            foreach (var s in asset.settings)
                            {
                                if (s != null && !string.IsNullOrEmpty(s.tagName))
                                {
                                    _cache[s.tagName] = new RuntimeSetting(s.color, s.isActive, s.isEssential, false);
                                    DebugLog($"Loaded config for tag '{s.tagName}': Color={s.color}, Active={s.isActive}");
                                }
                                else
                                {
                                    DebugWarning("Found null or invalid setting in asset, skipping...");
                                }
                            }
                        }
                        else
                        {
                            DebugWarning("DebugTagSettingsAsset.settings is null, initializing empty list");
                            asset.settings = new List<DebugTagSetting>();
                        }
                    }
                    else
                    {
                        DebugWarning("DebugTagSettingsAsset.Instance is null - will create configs dynamically as needed");
                    }
                }
                catch (System.Exception ex)
                {
                    DebugWarning($"Exception during initialization: {ex.Message}. Continuing with empty cache.");
                    _cache = new Dictionary<string, RuntimeSetting>();
                }

                _isInitialized = true;
                DebugLog($"DebugTagConfig initialization complete. Loaded {_cache.Count} tag configurations.");
            }
        }

        /// <summary>
        /// Ensure the system is initialized before use
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Create a new tag configuration dynamically
        /// </summary>
        private static RuntimeSetting CreateDynamicTagConfig(string tag)
        {
            DebugLog($"Creating dynamic config for new tag '{tag}'");

            var newConfig = new RuntimeSetting(
                color: GetRandomTagColor(),
                isActive: true,
                isEssential: false,
                isDynamicallyCreated: true
            );

            // Try to add it to the ScriptableObject asset if we're in the editor
            TryAddToAsset(tag, newConfig);

            return newConfig;
        }

        /// <summary>
        /// Try to add a dynamically created tag to the ScriptableObject asset
        /// </summary>
        private static void TryAddToAsset(string tag, RuntimeSetting config)
        {
#if UNITY_EDITOR
            try
            {
                var asset = DebugTagSettingsAsset.Instance;
                if (asset != null)
                {
                    if (asset.settings == null)
                    {
                        asset.settings = new List<DebugTagSetting>();
                    }

                    // Check if it already exists
                    var existing = asset.settings.Find(s => s.tagName == tag);
                    if (existing == null)
                    {
                        var newSetting = new DebugTagSetting()
                        {
                            tagName = tag,
                            color = config.color,
                            isActive = config.isActive,
                            isEssential = config.isEssential
                        };

                        asset.settings.Add(newSetting);
                        EditorUtility.SetDirty(asset);
                        AssetDatabase.SaveAssets();

                        DebugLog($"Added new tag '{tag}' to DebugTagSettingsAsset");
                    }
                }
            }
            catch (System.Exception ex)
            {
                DebugWarning($"Failed to add tag '{tag}' to asset: {ex.Message}");
            }
#endif
        }

        /// <summary>
        /// Fetch the runtime config for a tag with comprehensive fallback handling.
        /// Never throws errors - always returns a valid configuration.
        /// </summary>
        public static RuntimeSetting GetTagConfig(string tag)
        {
            // Input validation
            if (string.IsNullOrEmpty(tag))
            {
                DebugWarning("GetTagConfig called with null or empty tag name, using default config");
                return new RuntimeSetting();
            }

            EnsureInitialized();

            try
            {
                // Check if we already have this tag
                if (_cache.TryGetValue(tag, out var existingConfig))
                {
                    return existingConfig;
                }

                // Tag not found - create it dynamically
                DebugLog($"Tag '{tag}' not found in cache, creating dynamic configuration");

                var newConfig = CreateDynamicTagConfig(tag);
                _cache[tag] = newConfig;

                return newConfig;
            }
            catch (System.Exception ex)
            {
                DebugWarning($"Exception in GetTagConfig for tag '{tag}': {ex.Message}. Returning fallback config.");

                // Ultimate fallback - create a basic config and try to cache it
                var fallbackConfig = new RuntimeSetting(Color.white, true, false, true);

                try
                {
                    _cache[tag] = fallbackConfig;
                }
                catch
                {
                    // Even caching failed - just return the config without caching
                    DebugWarning($"Failed to cache fallback config for tag '{tag}'");
                }

                return fallbackConfig;
            }
        }

        /// <summary>
        /// Update a tag's color & active flag with comprehensive error handling.
        /// Mirrors into the ScriptableObject in the Editor.
        /// </summary>
        public static void SetTagConfig(string tag, Color color, bool isActive, bool isEssential = false)
        {
            if (string.IsNullOrEmpty(tag))
            {
                DebugWarning("SetTagConfig called with null or empty tag name, ignoring");
                return;
            }

            DebugLog($"Setting config for tag '{tag}': Color={color}, Active={isActive}, Essential={isEssential}");

            EnsureInitialized();

            try
            {
                // Update runtime cache
                if (!_cache.ContainsKey(tag))
                {
                    _cache[tag] = new RuntimeSetting();
                }

                _cache[tag].color = color;
                _cache[tag].isActive = isActive;
                _cache[tag].isEssential = isEssential;

#if UNITY_EDITOR
                // Mirror into the SO asset
                try
                {
                    var asset = DebugTagSettingsAsset.Instance;
                    if (asset != null)
                    {
                        if (asset.settings == null)
                        {
                            asset.settings = new List<DebugTagSetting>();
                        }

                        var setting = asset.settings.Find(s => s.tagName == tag);
                        if (setting == null)
                        {
                            setting = new DebugTagSetting() { tagName = tag };
                            asset.settings.Add(setting);
                            DebugLog($"Created new setting entry for tag '{tag}' in asset");
                        }

                        setting.color = color;
                        setting.isActive = isActive;
                        setting.isEssential = isEssential;

                        EditorUtility.SetDirty(asset);
                        AssetDatabase.SaveAssets();

                        DebugLog($"Successfully updated asset for tag '{tag}'");
                    }
                    else
                    {
                        DebugWarning("DebugTagSettingsAsset.Instance is null, cannot update asset");
                    }
                }
                catch (System.Exception ex)
                {
                    DebugWarning($"Failed to update asset for tag '{tag}': {ex.Message}");
                }
#endif
            }
            catch (System.Exception ex)
            {
                DebugWarning($"Exception in SetTagConfig for tag '{tag}': {ex.Message}");
            }
        }

        /// <summary>
        /// Get all currently registered tags
        /// </summary>
        public static string[] GetAllTags()
        {
            EnsureInitialized();

            try
            {
                var tags = new string[_cache.Keys.Count];
                _cache.Keys.CopyTo(tags, 0);
                return tags;
            }
            catch (System.Exception ex)
            {
                DebugWarning($"Exception in GetAllTags: {ex.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// Check if a tag is currently active
        /// </summary>
        public static bool IsTagActive(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return false;

            var config = GetTagConfig(tag);
            return config.isActive;
        }

        /// <summary>
        /// Reset the configuration system (useful for testing)
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                DebugLog("Resetting DebugTagConfig system");
                _cache?.Clear();
                _isInitialized = false;
                _colorIndex = 0;
            }
        }

        /// <summary>
        /// Get diagnostic information about the system
        /// </summary>
        public static string GetDiagnosticInfo()
        {
            EnsureInitialized();

            try
            {
                int totalTags = _cache.Count;
                int activeTags = 0;
                int dynamicTags = 0;

                foreach (var kvp in _cache)
                {
                    if (kvp.Value.isActive) activeTags++;
                    if (kvp.Value.isDynamicallyCreated) dynamicTags++;
                }

                return $"DebugTagConfig Status: {totalTags} total tags, {activeTags} active, {dynamicTags} dynamically created";
            }
            catch (System.Exception ex)
            {
                return $"DebugTagConfig Status: Error getting diagnostics - {ex.Message}";
            }
        }
    }
}