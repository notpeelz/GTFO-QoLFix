using BepInEx;
using MTFO.Core;

namespace QoL.BetterWeaponSwap
{
    [BepInPlugin(PluginGUID, PluginDisplayName, PluginVersion)]
    [BepInDependency("mtfo.core", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("dev.peelz.qol.common", BepInDependency.DependencyFlags.HardDependency)]
    internal class Plugin : MTFOPlugin
    {
        internal const string PluginGUID = "dev.peelz.qol.betterweaponswap";

        internal const string PluginDisplayName = "QoL - Better Weapon Swap";

        internal const string PluginVersion = VersionInfo.SemVer;

        protected override void OnLoad()
        {
            this.RegisterPatch<BetterWeaponSwapPatch>();
        }
    }
}
