using UnityEngine;
using System.Collections.Generic;

namespace TagDebugSystem
{
    // Your existing LogLevel enum remains in scope

    /// <summary>
    /// Core Logger class that works with your existing TagDebug system.
    /// </summary>
    public static class Logger
    {
        // Default minimum log level
        public static LogLevel MinimumLogLevel = LogLevel.Verbose;

        // Enable/disable logging in builds
        public static bool EnableLoggingInBuilds = false;

        // Category to tag mapping
        private static Dictionary<string, string> _tagToCategory = new Dictionary<string, string>();

        // Colors for each log level
        private static readonly Color[] _logLevelColors = new Color[] {
            new Color(0.7f, 0.7f, 0.7f),  // Verbose - Light Gray
            Color.white,                  // Info    - White
            Color.yellow,                 // Warning - Yellow
            new Color(1.0f, 0.5f, 0.3f),  // Error   - Orange
            Color.red                     // Fatal   - Red
        };

        /// <summary>
        /// Register a category with associated tags, seeding default tag colors.
        /// </summary>
        public static void RegisterCategory(string category, string[] tags)
        {
            foreach (string tag in tags)
            {
                _tagToCategory[tag] = category;

                // Seed default color if still white
                var cfg = DebugTagConfig.GetTagConfig(tag);
                if (cfg.color == Color.white)
                {
                    var categoryColor = GetColorForCategory(category);
                    DebugTagConfig.SetTagConfig(tag, categoryColor, true);
                }
            }
        }

        /// <summary>
        /// Default colors per category.
        /// </summary>
        private static Color GetColorForCategory(string category)
        {
            switch (category.ToLower())
            {
                case "system": return new Color(0.5f, 0.5f, 1.0f);
                case "ui": return new Color(1.0f, 0.5f, 0.8f);
                case "gameplay": return new Color(0.5f, 1.0f, 0.5f);
                case "network": return new Color(1.0f, 0.8f, 0.2f);
                case "audio": return new Color(0.7f, 0.4f, 1.0f);
                case "flow": return new Color(0.0f, 0.8f, 1.0f);
                case "ai": return new Color(1.0f, 0.4f, 0.4f);
                case "physics": return new Color(0.4f, 0.7f, 0.1f);
                case "input": return new Color(0.2f, 0.6f, 0.8f);
                case "vfx": return new Color(1.0f, 0.6f, 0.1f);
                default: return Color.white;
            }
        }

        /// <summary>
        /// Get the category for a tag.
        /// </summary>
        public static string GetCategoryForTag(string tag)
        {
            return _tagToCategory.TryGetValue(tag, out var cat) ? cat : "Uncategorized";
        }

        /// <summary>
        /// Seed all your default categories & tags.
        /// </summary>
        public static void InitializeDefaultCategories()
        {
            RegisterCategory("System", new[] { "System", "Initialization", "Configuration", "Settings" });
            RegisterCategory("UI", new[] { "UI", "Menu", "HUD", "Button", "Dialog" });
            RegisterCategory("Gameplay", new[] { "Player", "Enemy", "NPC", "Combat", "Inventory", "Quest" });
            RegisterCategory("Flow", new[] { "FlowManager", "Scene", "Login", "Authentication", "World", "Character" });
            RegisterCategory("Network", new[] { "Network", "Server", "Client", "Connection", "Sync" });
        }

        /// <summary>
        /// Core logging method (MonoBehaviour context).
        /// </summary>
        public static void Log(string tag, string message, LogLevel level, MonoBehaviour context)
        {
            // Build-mode filter
            if (!Debug.isDebugBuild && !EnableLoggingInBuilds) return;
            // Level filter
            if (level < MinimumLogLevel) return;

            // Tag config (color + isActive)
            var cfg = DebugTagConfig.GetTagConfig(tag);
            if (!cfg.isActive && level < LogLevel.Error) return; // always allow errors

            // Level prefix and coloring
            string levelPrefix = $"[{level}] ";
            var lvlColorHex = ColorUtility.ToHtmlStringRGB(_logLevelColors[(int)level]);
            string lvlWrapped = $"<color=#{lvlColorHex}>{levelPrefix}</color> {message}";

            // Tag coloring
            var tagColorHex = ColorUtility.ToHtmlStringRGB(cfg.color);
            string finalMsg = $"<color=#{tagColorHex}>{lvlWrapped}</color>";

            // Dispatch with correct LogType
            if (context != null)
            {
                switch (level)
                {
                    case LogLevel.Warning:
                        Debug.LogWarning(finalMsg, context);
                        break;
                    case LogLevel.Error:
                    case LogLevel.Fatal:
                        Debug.LogError(finalMsg, context);
                        break;
                    default:
                        Debug.Log(finalMsg, context);
                        break;
                }
            }
            else
            {
                // fallback if no context provided
                switch (level)
                {
                    case LogLevel.Warning:
                        Debug.LogWarning($"[{tag}] {finalMsg}");
                        break;
                    case LogLevel.Error:
                    case LogLevel.Fatal:
                        Debug.LogError($"[{tag}] {finalMsg}");
                        break;
                    default:
                        Debug.Log($"[{tag}] {finalMsg}");
                        break;
                }
            }
        }

        /// <summary>
        /// Core logging method (GameObject context).
        /// </summary>
        public static void Log(string tag, string message, LogLevel level, GameObject context)
        {
            // Build-mode filter
            if (!Debug.isDebugBuild && !EnableLoggingInBuilds) return;
            // Level filter
            if (level < MinimumLogLevel) return;

            // Tag config
            var cfg = DebugTagConfig.GetTagConfig(tag);
            if (!cfg.isActive && level < LogLevel.Error) return;

            // Level coloring
            string levelPrefix = $"[{level}] ";
            var lvlColorHex = ColorUtility.ToHtmlStringRGB(_logLevelColors[(int)level]);
            string lvlWrapped = $"<color=#{lvlColorHex}>{levelPrefix}</color> {message}";

            // Tag coloring
            var tagColorHex = ColorUtility.ToHtmlStringRGB(cfg.color);
            string finalMsg = $"<color=#{tagColorHex}>{lvlWrapped}</color>";

            // Dispatch with correct LogType
            if (context != null)
            {
                switch (level)
                {
                    case LogLevel.Warning:
                        Debug.LogWarning(finalMsg, context);
                        break;
                    case LogLevel.Error:
                    case LogLevel.Fatal:
                        Debug.LogError(finalMsg, context);
                        break;
                    default:
                        Debug.Log(finalMsg, context);
                        break;
                }
            }
            else
            {
                switch (level)
                {
                    case LogLevel.Warning:
                        Debug.LogWarning($"[{tag}] {finalMsg}");
                        break;
                    case LogLevel.Error:
                    case LogLevel.Fatal:
                        Debug.LogError($"[{tag}] {finalMsg}");
                        break;
                    default:
                        Debug.Log($"[{tag}] {finalMsg}");
                        break;
                }
            }
        }

        /// <summary>
        /// Convenience—no context.
        /// </summary>
        public static void Log(string tag, string message, LogLevel level)
            => Log(tag, message, level, (MonoBehaviour)null);

        // Shorthand methods
        public static void Verbose(string tag, string message, MonoBehaviour ctx = null) => Log(tag, message, LogLevel.Verbose, ctx);
        public static void Info(string tag, string message, MonoBehaviour ctx = null) => Log(tag, message, LogLevel.Info, ctx);
        public static void Warning(string tag, string message, MonoBehaviour ctx = null) => Log(tag, message, LogLevel.Warning, ctx);
        public static void Error(string tag, string message, MonoBehaviour ctx = null) => Log(tag, message, LogLevel.Error, ctx);
        public static void Fatal(string tag, string message, MonoBehaviour ctx = null) => Log(tag, message, LogLevel.Fatal, ctx);

        // GameObject overloads
        public static void Verbose(string tag, string msg, GameObject ctx) => Log(tag, msg, LogLevel.Verbose, ctx);
        public static void Info(string tag, string msg, GameObject ctx) => Log(tag, msg, LogLevel.Info, ctx);
        public static void Warning(string tag, string msg, GameObject ctx) => Log(tag, msg, LogLevel.Warning, ctx);
        public static void Error(string tag, string msg, GameObject ctx) => Log(tag, msg, LogLevel.Error, ctx);
        public static void Fatal(string tag, string msg, GameObject ctx) => Log(tag, msg, LogLevel.Fatal, ctx);
    }
}
