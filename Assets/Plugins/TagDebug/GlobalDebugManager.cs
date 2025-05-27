using UnityEngine;
using System.Collections.Generic;

namespace TagDebugSystem
{
    public enum LogLevel
    {
        Verbose,  // Detailed information for debugging
        Info,     // General information
        Warning,  // Warnings that don't stop execution
        Error,    // Errors that might affect functionality
        Fatal     // Critical errors that stop execution
    }

    public class GlobalDebugManager : MonoBehaviour
    {
        public static GlobalDebugManager Instance { get; private set; }

        [SerializeField] private bool _enableLoggingInBuild = false;
        [SerializeField] private LogLevel _minimumLogLevel = LogLevel.Info;
        [SerializeField] private bool _logToFile = false;
        [SerializeField] private string _logFilePath = "TagDebug.log";

        private System.IO.StreamWriter _logWriter;

        // Tags organized by category
        private static Dictionary<string, string> _categoryMapping = new Dictionary<string, string>();

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // Initialize file logging if needed
                if (_logToFile)
                {
                    try
                    {
                        _logWriter = new System.IO.StreamWriter(_logFilePath, true);
                        _logWriter.AutoFlush = true;
                        Log("GlobalDebugManager", "Logging initialized", LogLevel.Info, this);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to initialize log file: {e.Message}");
                        _logToFile = false;
                    }
                }

                // Initialize default tag categories
                InitializeDefaultCategories();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_logWriter != null)
            {
                _logWriter.Close();
                _logWriter.Dispose();
            }
        }

        private void InitializeDefaultCategories()
        {
            // Define standard categories and their associated tags
            _categoryMapping.Clear();

            // System-related tags
            RegisterTagCategory("System", new string[] {
                "System", "Initialization", "Configuration", "Performance"
            });

            // UI-related tags
            RegisterTagCategory("UI", new string[] {
                "UI", "Menu", "HUD", "Dialog", "Button"
            });

            // Gameplay-related tags
            RegisterTagCategory("Gameplay", new string[] {
                "Player", "Enemy", "NPC", "Combat", "Inventory", "Quest"
            });

            // Network-related tags
            RegisterTagCategory("Network", new string[] {
                "Network", "Server", "Client", "Connection", "Sync"
            });

            // Audio-related tags
            RegisterTagCategory("Audio", new string[] {
                "Audio", "Music", "SFX", "Voice"
            });

            // Core tags from your existing FlowManager
            RegisterTagCategory("Flow", new string[] {
                "FlowManager", "Authentication", "Login", "World", "Character"
            });
        }

        public static void RegisterTagCategory(string category, string[] tags)
        {
            foreach (string tag in tags)
            {
                _categoryMapping[tag] = category;

                // compute default color for this category
                Color defaultColor = GetColorForCategory(category);

                // seed into the SO only if we haven't already
                var asset = DebugTagSettingsAsset.Instance;
                if (asset != null && asset.settings.Find(s => s.tagName == tag) == null)
                {
                    DebugTagConfig.SetTagConfig(tag, defaultColor, true);
                }
            }
        }


        private static Color GetColorForCategory(string category)
        {
            switch (category.ToLower())
            {
                case "system": return new Color(0.5f, 0.5f, 1.0f); // Light blue
                case "ui": return new Color(1.0f, 0.5f, 0.8f);     // Pink
                case "gameplay": return new Color(0.5f, 1.0f, 0.5f); // Light green
                case "network": return new Color(1.0f, 0.8f, 0.2f); // Gold
                case "audio": return new Color(0.7f, 0.4f, 1.0f);   // Purple
                case "flow": return new Color(0.0f, 0.8f, 1.0f);    // Cyan
                default: return Color.white;
            }
        }

        public static string GetCategoryForTag(string tag)
        {
            if (_categoryMapping.TryGetValue(tag, out string category))
            {
                return category;
            }
            return "Uncategorized";
        }

        public static void Log(string tag, string message, LogLevel level, MonoBehaviour context)
        {
            // Early out if logging is disabled in builds
            if (!Debug.isDebugBuild && !Instance._enableLoggingInBuild)
                return;

            // Skip if message level is below minimum
            if (level < Instance._minimumLogLevel)
                return;

            // Format the message with level prefix
            string formattedMessage = $"[{level}] {message}";

            // Log to Unity console (explicit tag)
            TagDebug.Log(tag, formattedMessage, context);

            // Log to file if enabled
            if (Instance._logToFile && Instance._logWriter != null)
            {
                string timeStamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string category = GetCategoryForTag(tag);
                Instance._logWriter.WriteLine($"{timeStamp} [{level}] [{category}:{tag}] {message}");
            }
        }

        // Alternative method for non-MonoBehaviour contexts
        public static void Log(string tag, string message, LogLevel level, GameObject context)
        {
            // Early out if logging is disabled in builds
            if (!Debug.isDebugBuild && !Instance._enableLoggingInBuild)
                return;

            // Skip if message level is below minimum
            if (level < Instance._minimumLogLevel)
                return;

            // Format the message with level prefix
            string formattedMessage = $"[{level}] {message}";

            // Log to Unity console (explicit tag)
            TagDebug.Log(tag, formattedMessage, context);

            // Log to file if enabled
            if (Instance._logToFile && Instance._logWriter != null)
            {
                string timeStamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string category = GetCategoryForTag(tag);
                Instance._logWriter.WriteLine($"{timeStamp} [{level}] [{category}:{tag}] {message}");
            }
        }

        // Static shorthand methods for each log level
        public static void Verbose(string tag, string message, MonoBehaviour context)
        {
            Log(tag, message, LogLevel.Verbose, context);
        }

        public static void Info(string tag, string message, MonoBehaviour context)
        {
            Log(tag, message, LogLevel.Info, context);
        }

        public static void Warning(string tag, string message, MonoBehaviour context)
        {
            Log(tag, message, LogLevel.Warning, context);
        }

        public static void Error(string tag, string message, MonoBehaviour context)
        {
            Log(tag, message, LogLevel.Error, context);
        }

        public static void Fatal(string tag, string message, MonoBehaviour context)
        {
            Log(tag, message, LogLevel.Fatal, context);
        }

        // Additional overloads for GameObject contexts
        public static void Verbose(string tag, string message, GameObject context)
        {
            Log(tag, message, LogLevel.Verbose, context);
        }

        public static void Info(string tag, string message, GameObject context)
        {
            Log(tag, message, LogLevel.Info, context);
        }

        public static void Warning(string tag, string message, GameObject context)
        {
            Log(tag, message, LogLevel.Warning, context);
        }

        public static void Error(string tag, string message, GameObject context)
        {
            Log(tag, message, LogLevel.Error, context);
        }

        public static void Fatal(string tag, string message, GameObject context)
        {
            Log(tag, message, LogLevel.Fatal, context);
        }
    }
}