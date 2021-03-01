using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;

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

        public static void PatchConstructor<TClass>(
            this IPatch patch,
            PatchType patchType,
            string prefixMethodName = default,
            string postfixMethodName = default)
            where TClass : class =>
            PatchConstructor<TClass>(patch, null, patchType, prefixMethodName, postfixMethodName);

        public static void PatchConstructor<TClass>(
            this IPatch patch,
            Type[] parameters,
            PatchType patchType,
            string prefixMethodName = default,
            string postfixMethodName = default)
            where TClass : class
        {
            var m = AccessTools.Constructor(typeof(TClass), parameters);
            PatchMethod<TClass>(patch, m, patchType, prefixMethodName, postfixMethodName);
        }

        public static void PatchMethod<TClass>(
            this IPatch patch,
            string methodName,
            PatchType patchType,
            Type[] generics = null,
            string prefixMethodName = default,
            string postfixMethodName = default)
            where TClass : class =>
            PatchMethod<TClass>(patch, methodName, null, patchType, generics, prefixMethodName, postfixMethodName);

        public static void PatchMethod<TClass>(
            this IPatch patch,
            string methodName,
            Type[] parameters,
            PatchType patchType,
            Type[] generics = null,
            string prefixMethodName = default,
            string postfixMethodName = default)
            where TClass : class
        {
            var m = AccessTools.Method(typeof(TClass), methodName, parameters, generics);
            PatchMethod<TClass>(patch, m, patchType, prefixMethodName, postfixMethodName);
        }

        public static void PatchMethod<TClass>(
            this IPatch patch,
            MethodBase methodBase,
            PatchType patchType,
            string prefixMethodName = default,
            string postfixMethodName = default)
            where TClass : class
        {
            var className = typeof(TClass).Name.Replace("`", "__");
            var formattedMethodName = methodBase.ToString();
            var methodName = methodBase.IsConstructor ? "ctor" : methodBase.Name;

            MethodInfo postfix = null, prefix = null;

            if ((patchType & PatchType.Prefix) != 0)
            {
                try
                {
                    prefix = AccessTools.Method(patch.GetType(), prefixMethodName ?? $"{className}__{methodName}__Prefix");
                }
                catch (Exception ex)
                {
                    patch.LogFatal($"Failed to obtain the prefix patch method for {formattedMethodName}): {ex}");
                }
            }

            if ((patchType & PatchType.Postfix) != 0)
            {
                try
                {
                    postfix = AccessTools.Method(patch.GetType(), postfixMethodName ?? $"{className}__{methodName}__Postfix");
                }
                catch (Exception ex)
                {
                    patch.LogFatal($"Failed to obtain the postfix patch method for {formattedMethodName}): {ex}");
                }
            }

            try
            {
                if (prefix != null && postfix != null)
                {
                    patch.Harmony.Patch(methodBase, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));
                    return;
                }

                if (prefix != null)
                {
                    patch.Harmony.Patch(methodBase, prefix: new HarmonyMethod(prefix));
                    return;
                }

                if (postfix != null)
                {
                    patch.Harmony.Patch(methodBase, postfix: new HarmonyMethod(postfix));
                    return;
                }
            }
            catch (Exception ex)
            {
                patch.LogError($"Failed to patch method {formattedMethodName}: {ex}");
            }
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
