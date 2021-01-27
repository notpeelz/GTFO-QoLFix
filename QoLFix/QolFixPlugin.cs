using QoLFix.Patches;
using BepInEx.IL2CPP;
using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using System;

namespace QoLFix
{
    [BepInPlugin(GUID, ModName, Version)]
    [BepInProcess("GTFO.exe")]
    public class QoLFixPlugin : BasePlugin
    {
        public const string ModName = "QoL Fix";
        public const string GUID = "dev.peelz.qolfix";
        public const string Version = "0.1.0";

        private const string SectionMain = "Config";
        private static readonly ConfigDefinition ConfigVersion = new ConfigDefinition(SectionMain, "Version");

        private Harmony harmony;

        public static QoLFixPlugin Instance { get; private set; }

        public QoLFixPlugin()
        {
            this.Config.SaveOnConfigSet = false;
        }

        public override void Load()
        {
            Instance = this;

            this.Config.Bind(ConfigVersion, Version, new ConfigDescription("Used internally for config upgrades; don't touch!"));

            var versionEntry = this.Config.GetConfigEntry<string>(ConfigVersion);
            var configVersion = new Version(versionEntry.Value);
            var currentVersion = new Version(Version);
            if (configVersion < currentVersion)
            {
                this.Log.LogInfo($"Upgrading config to {currentVersion}");
                versionEntry.Value = Version;
                this.Config.Save();
            }
            else if (configVersion > currentVersion)
            {
                this.Log.LogError($"The current config is from a newer version of the plugin. If you're trying to downgrade, you should delete the config file and let it regenerate.");
                this.Unload();
                return;
            }

            this.Config.SaveOnConfigSet = true;

            // Misc
            this.RegisterPatch<DisableAnalyticsPatch>();
            this.RegisterPatch<DisableSteamRichPresencePatch>();

            // Annoyances
            this.RegisterPatch<IntroSkipPatch>();
            this.RegisterPatch<ElevatorIntroSkipPatch>();
            this.RegisterPatch<ElevatorVolumePatch>();

            // Tweaks
            this.RegisterPatch<LobbyUnreadyPatch>();
            this.RegisterPatch<SteamProfileLinkPatch>();
            this.RegisterPatch<NoiseRemovalPatch>();

            // Bug fixes
            this.RegisterPatch<FixWeaponSwapPatch>();
            this.RegisterPatch<FixToolRefillBioScannerPatch>();
            this.RegisterPatch<FixDoorCollisionPatch>();
            this.RegisterPatch<FixBioScannerNavMarkerPatch>();
            this.RegisterPatch<FixLockerPingPatch>();
        }

        public void RegisterPatch<T>() where T : IPatch, new()
        {
            if (this.harmony == null)
            {
                this.harmony = new Harmony(GUID);
            }

            var patch = new T();
            patch.Initialize();
            if (patch.Enabled)
            {
                this.Log.LogInfo($"Applying patch: {patch.Name}");
                patch.Patch(this.harmony);
            }
        }

    }
}
