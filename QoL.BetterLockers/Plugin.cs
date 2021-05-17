using BepInEx;
using MTFO.Core;

namespace QoL.BetterLockers
{
    [BepInPlugin(PluginGUID, PluginDisplayName, PluginVersion)]
    [BepInDependency("mtfo.core", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("dev.peelz.qol.common", BepInDependency.DependencyFlags.HardDependency)]
    internal class Plugin : MTFOPlugin
    {
        internal const string PluginGUID = "dev.peelz.qol.betterlockers";

        internal const string PluginDisplayName = "QoL - Better Lockers";

        internal const string PluginVersion = VersionInfo.SemVer;

        protected override void OnLoad()
        {
            this.RegisterPatch<DropResourcesPatch>();
        }
    }
}
