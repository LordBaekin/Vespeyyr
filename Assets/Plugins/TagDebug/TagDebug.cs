using UnityEngine;

namespace TagDebugSystem
{
    public static class TagDebug
    {
        private static void _Log(string tag, string message, GameObject go)
        {
            // Get the tag configuration by the explicit tag, not go.tag
            var config = DebugTagConfig.GetTagConfig(tag);

            // Skip if disabled (unless it�s an error/fatal; you can extend that logic)
            if (!config.isActive) return;

            // Wrap the message in the configured color
            string logMessage = $"<color=#{ColorUtility.ToHtmlStringRGB(config.color)}>{message}</color>";

            // Send to Unity console, preserving context
            Debug.Log(logMessage, go);
        }

        // Called from Logger.Log
        public static void Log(string tag, string message, GameObject gameObject)
        {
            if (gameObject != null)
            {
                _Log(tag, message, gameObject);
            }
            else
            {
                Debug.Log(message);
            }
        }

        // Overload for MonoBehaviour contexts
        public static void Log(string tag, string message, MonoBehaviour monoBehaviour)
        {
            if (monoBehaviour != null)
            {
                _Log(tag, message, monoBehaviour.gameObject);
            }
            else
            {
                Debug.Log(message);
            }
        }
    }
}
