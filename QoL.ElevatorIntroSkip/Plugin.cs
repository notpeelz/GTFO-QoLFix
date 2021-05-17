using BepInEx;
using MTFO.Core;

namespace QoL.ElevatorIntroSkip
{
    [BepInPlugin(PluginGUID, PluginDisplayName, PluginVersion)]
    [BepInDependency("mtfo.core", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("dev.peelz.qol.common", BepInDependency.DependencyFlags.HardDependency)]
    internal class Plugin : MTFOPlugin
    {
        internal const string PluginGUID = "dev.peelz.qol.elevatorintroskip";

        internal const string PluginDisplayName = "QoL - Elevator Intro Skip";

        internal const string PluginVersion = VersionInfo.SemVer;

        protected override void OnLoad()
        {
            this.RegisterPatch<ElevatorIntroSkipPatch>();
        }
    }
}
