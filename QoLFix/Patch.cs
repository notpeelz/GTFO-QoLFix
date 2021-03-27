using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace QoLFix
{
    public abstract class Patch
    {
        public virtual void Initialize() { }

        protected internal Harmony Harmony { get; set; }

        public virtual string Name { get; }

        public virtual bool Enabled => true;

        public abstract void Execute();

        public void PatchConstructor<TClass>(
            PatchType patchType,
            string prefixMethodName = default,
            string postfixMethodName = default)
            where TClass : class =>
            this.PatchConstructor<TClass>(null, patchType, prefixMethodName, postfixMethodName);

        public void PatchConstructor<TClass>(
            Type[] parameters,
            PatchType patchType,
            string prefixMethodName = default,
            string postfixMethodName = default)
            where TClass : class
        {
            var m = AccessTools.Constructor(typeof(TClass), parameters);
            this.PatchMethod<TClass>(m, patchType, prefixMethodName, postfixMethodName);
        }

        #region Generic PatchMethod overloads
        public void PatchMethod<TClass>(
            string methodName,
            PatchType patchType,
            Type[] generics = null,
            string prefixMethodName = default,
            string postfixMethodName = default)
            where TClass : class =>
            this.PatchMethod<TClass>(methodName, null, patchType, generics, prefixMethodName, postfixMethodName);

        public void PatchMethod<TClass>(
            string methodName,
            Type[] parameters,
            PatchType patchType,
            Type[] generics = null,
            string prefixMethodName = default,
            string postfixMethodName = default)
            where TClass : class
        {
            var m = AccessTools.Method(typeof(TClass), methodName, parameters, generics);
            this.PatchMethod<TClass>(m, patchType, prefixMethodName, postfixMethodName);
        }

        public void PatchMethod<TClass>(
            MethodBase methodBase,
            PatchType patchType,
            string prefixMethodName = default,
            string postfixMethodName = default)
            where TClass : class =>
            this.PatchMethod(typeof(TClass), methodBase, patchType, prefixMethodName, postfixMethodName);
        #endregion

        #region Non-generic PatchMethod overloads
        public void PatchMethod(
            Type classType,
            string methodName,
            PatchType patchType,
            Type[] generics = null,
            string prefixMethodName = default,
            string postfixMethodName = default) =>
            this.PatchMethod(classType, methodName, null, patchType, generics, prefixMethodName, postfixMethodName);

        public void PatchMethod(
            Type classType,
            string methodName,
            Type[] parameters,
            PatchType patchType,
            Type[] generics = null,
            string prefixMethodName = default,
            string postfixMethodName = default)
        {
            var m = AccessTools.Method(classType, methodName, parameters, generics);
            this.PatchMethod(classType, m, patchType, prefixMethodName, postfixMethodName);
        }
        #endregion

        public void PatchMethod(
            Type classType,
            MethodBase methodBase,
            PatchType patchType,
            string prefixMethodName = default,
            string postfixMethodName = default)
        {
            var className = classType.Name.Replace("`", "__");
            var formattedMethodName = methodBase.ToString();
            var methodName = methodBase.IsConstructor ? "ctor" : methodBase.Name;

            MethodInfo postfix = null, prefix = null;

            if ((patchType & PatchType.Prefix) != 0)
            {
                try
                {
                    prefix = AccessTools.Method(this.GetType(), prefixMethodName ?? $"{className}__{methodName}__Prefix");
                }
                catch (Exception ex)
                {
                    this.LogFatal($"Failed to obtain the prefix patch method for {formattedMethodName}): {ex}");
                }
            }

            if ((patchType & PatchType.Postfix) != 0)
            {
                try
                {
                    postfix = AccessTools.Method(this.GetType(), postfixMethodName ?? $"{className}__{methodName}__Postfix");
                }
                catch (Exception ex)
                {
                    this.LogFatal($"Failed to obtain the postfix patch method for {formattedMethodName}): {ex}");
                }
            }

            try
            {
                if (prefix != null && postfix != null)
                {
                    this.Harmony.Patch(methodBase, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));
                    return;
                }

                if (prefix != null)
                {
                    this.Harmony.Patch(methodBase, prefix: new HarmonyMethod(prefix));
                    return;
                }

                if (postfix != null)
                {
                    this.Harmony.Patch(methodBase, postfix: new HarmonyMethod(postfix));
                    return;
                }
            }
            catch (Exception ex)
            {
                this.LogError($"Failed to patch method {formattedMethodName}: {ex}");
            }
        }

        public void Log(LogLevel level, object data) =>
            QoLFixPlugin.Instance.Log.Log(level, $"<{this.Name}> {data}");

        public void LogDebug(object data) =>
            QoLFixPlugin.Instance.Log.LogDebug($"<{this.Name}> {data}");

        public void LogError(object data) =>
            QoLFixPlugin.Instance.Log.LogError($"<{this.Name}> {data}");

        public void LogFatal(object data) =>
            QoLFixPlugin.Instance.Log.LogFatal($"<{this.Name}> {data}");

        public void LogInfo(object data) =>
            QoLFixPlugin.Instance.Log.LogInfo($"<{this.Name}> {data}");

        public void LogMessage(object data) =>
            QoLFixPlugin.Instance.Log.LogMessage($"<{this.Name}> {data}");

        public void LogWarning(object data) =>
            QoLFixPlugin.Instance.Log.LogWarning($"<{this.Name}> {data}");
    }
}
