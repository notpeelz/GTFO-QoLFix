using BepInEx;
using MTFO.Core;

namespace QoL.LatencyInfo
{
    [BepInPlugin(PluginGUID, PluginDisplayName, PluginVersion)]
    [BepInDependency("mtfo.core", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("dev.peelz.qol.common", BepInDependency.DependencyFlags.HardDependency)]
    internal class Plugin : MTFOPlugin
    {
        internal const string PluginGUID = "dev.peelz.qol.latencyinfo";

        internal const string PluginDisplayName = "QoL - Latency Info";

        internal const string PluginVersion = VersionInfo.SemVer;

        protected override void OnLoad()
        {
            this.RegisterPatch<LatencyInfoPatch>();
        }
    }
}
