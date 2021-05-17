using BepInEx;
using MTFO.Core;

namespace QoL.TerminalPingableSwaps
{
    [BepInPlugin(PluginGUID, PluginDisplayName, PluginVersion)]
    [BepInDependency("mtfo.core", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("dev.peelz.qol.common", BepInDependency.DependencyFlags.HardDependency)]
    internal class Plugin : MTFOPlugin
    {
        internal const string PluginGUID = "dev.peelz.qol.terminalpingableswaps";

        internal const string PluginDisplayName = "QoL - Terminal-pingable swaps";

        internal const string PluginVersion = VersionInfo.SemVer;

        protected override void OnLoad()
        {
            this.RegisterPatch<TerminalPingableSwapsPatch>();
        }
    }
}
