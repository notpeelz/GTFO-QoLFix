using BepInEx;
using MTFO.Core;

namespace QoL.RecentlyPlayedWith
{
    [BepInPlugin(PluginGUID, PluginDisplayName, PluginVersion)]
    [BepInDependency("mtfo.core", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("dev.peelz.qol.common", BepInDependency.DependencyFlags.HardDependency)]
    internal class Plugin : MTFOPlugin
    {
        internal const string PluginGUID = "dev.peelz.qol.recentlyplayedwith";

        internal const string PluginDisplayName = "QoL - Recently Played With";

        internal const string PluginVersion = VersionInfo.SemVer;

        protected override void OnLoad()
        {
            this.RegisterPatch<RecentlyPlayedWithPatch>();
        }
    }
}
