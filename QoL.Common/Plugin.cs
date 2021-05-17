using BepInEx;
using MTFO.Core;
using QoL.Common.Patches;

namespace QoL.Common
{
    [BepInPlugin(PluginGUID, PluginDisplayName, PluginVersion)]
    [BepInDependency("mtfo.core", BepInDependency.DependencyFlags.HardDependency)]
    internal class Plugin : MTFOPlugin
    {
        internal const string PluginGUID = "dev.peelz.qol.common";

        internal const string PluginDisplayName = "QoL - Common";

        internal const string PluginVersion = VersionInfo.SemVer;

        public static Plugin? Instance { get; private set; }

        protected override void OnLoad()
        {
            Instance = this;
            this.RegisterPatch<ItemEquippableAnimationSequencePatch>();
            this.RegisterPatch<ReparentPickupPatch>();
            this.RegisterPatch<PlayerNameExtPatch>();
        }

        public static void LogDebug(object data) => Instance!.Log.LogDebug(data);

        public static void LogError(object data) => Instance!.Log.LogError(data);

        public static void LogFatal(object data) => Instance!.Log.LogFatal(data);

        public static void LogInfo(object data) => Instance!.Log.LogInfo(data);

        public static void LogMessage(object data) => Instance!.Log.LogMessage(data);

        public static void LogWarning(object data) => Instance!.Log.LogWarning(data);
    }
}
