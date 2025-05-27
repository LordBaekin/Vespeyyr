using UnityEngine;

namespace TagDebugSystem
{
    /// <summary>
    /// TD - Tag Debug facade class that provides short and convenient methods for logging.
    /// This class serves as a simple interface to the full Logger system.
    /// </summary>
    public static class TD
    {
        // Shorthand method for logging with the GameObject's tag
        public static void Log(string message, MonoBehaviour context, LogLevel level = LogLevel.Info)
        {
            if (context != null && context.gameObject != null)
            {
                Logger.Log(context.gameObject.tag, message, level, context);
            }
        }

        // Shorthand method with explicit tag
        public static void Log(string tag, string message, MonoBehaviour context, LogLevel level = LogLevel.Info)
        {
            Logger.Log(tag, message, level, context);
        }

        // Log level shorthands with GameObject's tag
        public static void Verbose(string message, MonoBehaviour context)
        {
            Log(message, context, LogLevel.Verbose);
        }

        public static void Info(string message, MonoBehaviour context)
        {
            Log(message, context, LogLevel.Info);
        }

        public static void Warning(string message, MonoBehaviour context)
        {
            Log(message, context, LogLevel.Warning);
        }

        public static void Error(string message, MonoBehaviour context)
        {
            Log(message, context, LogLevel.Error);
        }

        public static void Fatal(string message, MonoBehaviour context)
        {
            Log(message, context, LogLevel.Fatal);
        }

        // Log level shorthands with explicit tag
        public static void Verbose(string tag, string message, MonoBehaviour context)
        {
            Logger.Verbose(tag, message, context);
        }

        public static void Info(string tag, string message, MonoBehaviour context)
        {
            Logger.Info(tag, message, context);
        }

        public static void Warning(string tag, string message, MonoBehaviour context)
        {
            Logger.Warning(tag, message, context);
        }

        public static void Error(string tag, string message, MonoBehaviour context)
        {
            Logger.Error(tag, message, context);
        }

        public static void Fatal(string tag, string message, MonoBehaviour context)
        {
            Logger.Fatal(tag, message, context);
        }

        // Static context variants
        public static void Verbose(string tag, string message)
        {
            Logger.Verbose(tag, message);
        }

        public static void Info(string tag, string message)
        {
            Logger.Info(tag, message);
        }

        public static void Warning(string tag, string message)
        {
            Logger.Warning(tag, message);
        }

        public static void Error(string tag, string message)
        {
            Logger.Error(tag, message);
        }

        public static void Fatal(string tag, string message)
        {
            Logger.Fatal(tag, message);
        }

        // GameObject context variants
        public static void Log(string message, GameObject context, LogLevel level = LogLevel.Info)
        {
            if (context != null)
            {
                Logger.Log(context.tag, message, level, context);
            }
        }

        public static void Verbose(string message, GameObject context)
        {
            if (context != null)
            {
                Logger.Verbose(context.tag, message, context);
            }
        }

        public static void Info(string message, GameObject context)
        {
            if (context != null)
            {
                Logger.Info(context.tag, message, context);
            }
        }

        public static void Warning(string message, GameObject context)
        {
            if (context != null)
            {
                Logger.Warning(context.tag, message, context);
            }
        }

        public static void Error(string message, GameObject context)
        {
            if (context != null)
            {
                Logger.Error(context.tag, message, context);
            }
        }

        public static void Fatal(string message, GameObject context)
        {
            if (context != null)
            {
                Logger.Fatal(context.tag, message, context);
            }
        }
    }
}