using BepInEx;
using MTFO.Core;

namespace QoL.ResourceAudioCue
{
    [BepInPlugin(PluginGUID, PluginDisplayName, PluginVersion)]
    [BepInDependency("mtfo.core", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("dev.peelz.qol.common", BepInDependency.DependencyFlags.HardDependency)]
    internal class Plugin : MTFOPlugin
    {
        internal const string PluginGUID = "dev.peelz.qol.resourceaudiocue";

        internal const string PluginDisplayName = "QoL - Resource Audio Cue";

        internal const string PluginVersion = VersionInfo.SemVer;

        protected override void OnLoad()
        {
            this.RegisterPatch<ResourceAudioCuePatch>();
        }
    }
}
