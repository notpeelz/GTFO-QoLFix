using BepInEx;
using MTFO.Core;

namespace QoL.BugFixes
{
    [BepInPlugin(PluginGUID, PluginDisplayName, PluginVersion)]
    [BepInDependency("mtfo.core", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("dev.peelz.qol.common", BepInDependency.DependencyFlags.HardDependency)]
    internal class Plugin : MTFOPlugin
    {
        internal const string PluginGUID = "dev.peelz.qol.bugfixes";

        internal const string PluginDisplayName = "QoL - Bug Fixes";

        internal const string PluginVersion = VersionInfo.SemVer;

        protected override void OnLoad()
        {
            this.RegisterPatch<SoundMuffleBugFixPatch>();
            this.RegisterPatch<VelocityBugFixPatch>();
            this.RegisterPatch<MeleeChargeBugFixPatch>();
            this.RegisterPatch<WeaponAnimationBugFixPatch>();
        }
    }
}
