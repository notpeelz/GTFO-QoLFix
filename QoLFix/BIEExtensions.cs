using BepInEx.Configuration;
using BepInEx.Logging;
using System;

namespace QoLFix
{
    public static class BIEExtensions
    {
        public static ConfigEntry<T> GetConfigEntry<T>(this ConfigFile configFile, ConfigDefinition definition)
        {
            if (!configFile.TryGetEntry<T>(definition, out var entry))
                throw new InvalidOperationException("Config entry has not been added yet.");
            return entry;
        }

        public static void Log(this IPatch patch, LogLevel level, object data) =>
            QoLFixPlugin.Instance.Log.Log(level, $"<{patch.Name}> {data}");

        public static void LogDebug(this IPatch patch, object data) =>
            QoLFixPlugin.Instance.Log.LogDebug($"<{patch.Name}> {data}");

        public static void LogError(this IPatch patch, object data) =>
            QoLFixPlugin.Instance.Log.LogError($"<{patch.Name}> {data}");

        public static void LogFatal(this IPatch patch, object data) =>
            QoLFixPlugin.Instance.Log.LogFatal($"<{patch.Name}> {data}");

        public static void LogInfo(this IPatch patch, object data) =>
            QoLFixPlugin.Instance.Log.LogInfo($"<{patch.Name}> {data}");

        public static void LogMessage(this IPatch patch, object data) =>
            QoLFixPlugin.Instance.Log.LogMessage($"<{patch.Name}> {data}");

        public static void LogWarning(this IPatch patch, object data) =>
            QoLFixPlugin.Instance.Log.LogWarning($"<{patch.Name}> {data}");
    }
}
