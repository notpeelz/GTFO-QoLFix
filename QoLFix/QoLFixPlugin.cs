using BepInEx.IL2CPP;
using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using System;
using QoLFix.Patches;
using QoLFix.Patches.Common;
using QoLFix.UI;

namespace QoLFix
{
    [BepInPlugin(GUID, ModName, VersionInfo.Version)]
    [BepInProcess("GTFO.exe")]
    public class QoLFixPlugin : BasePlugin
    {
        internal const string ModName = "QoL Fix";
        internal const string GUID = "dev.peelz.qolfix";
        internal const string RepoName = "louistakepillz/QoLFix";

        public const int SupportedGameRevision = 21989;

        private const string SectionMain = "Config";
        private static readonly ConfigDefinition ConfigVersion = new ConfigDefinition(SectionMain, "Version");
        private static readonly ConfigDefinition ConfigGameVersion = new ConfigDefinition(SectionMain, "GameVersion");

        private Harmony harmony;

        public static QoLFixPlugin Instance { get; private set; }

        public QoLFixPlugin()
        {
            this.Config.SaveOnConfigSet = false;
        }

        public override void Load()
        {
            Instance = this;

            if (!this.CheckConfigVersion()) return;

            this.CheckGameVersion();

            this.Config.SaveOnConfigSet = true;

            // Common
            this.RegisterPatch<DisableAnalyticsPatch>();
            this.RegisterPatch<CursorUnlockPatch>();

            // Misc
            this.RegisterPatch<DisableSteamRichPresencePatch>();

            // Annoyances
            this.RegisterPatch<IntroSkipPatch>();
            this.RegisterPatch<ElevatorIntroSkipPatch>();
            this.RegisterPatch<ElevatorVolumePatch>();

            // Tweaks
            this.RegisterPatch<LobbyUnreadyPatch>();
            this.RegisterPatch<SteamProfileLinkPatch>();
            this.RegisterPatch<NoiseRemovalPatch>();
            this.RegisterPatch<PingableSwapsPatch>();
            this.RegisterPatch<HideCrosshairPatch>();
            this.RegisterPatch<DropResourcesPatch>();

            // Bug fixes
            this.RegisterPatch<FixWeaponSwapPatch>();
            this.RegisterPatch<FixToolRefillBioScannerPatch>();
            this.RegisterPatch<FixDoorCollisionPatch>();
            this.RegisterPatch<FixDoorFramePingPatch>();
            this.RegisterPatch<FixTerminalDisplayPingPatch>();
            this.RegisterPatch<FixBioScannerNavMarkerPatch>();
            this.RegisterPatch<FixLockerPingPatch>();

            // XXX: needs to execute after everything else
            this.RegisterPatch<ReparentPickupPatch>();

            this.Config.Save();

            UIManager.Initialize();
            UIManager.Initialized += () =>
            {
                UpdateNotifier.Initialize();
                UpdateManager.Initialize();
            };
        }

        public override bool Unload()
        {
            LogInfo("Unloading");
            return base.Unload();
        }

        private bool CheckConfigVersion()
        {
            this.Config.Bind(ConfigVersion, VersionInfo.Version, new ConfigDescription("Used internally for config upgrades; don't touch!"));

            var versionEntry = this.Config.GetConfigEntry<string>(ConfigVersion);
            var configVersion = new Version(versionEntry.Value);
            if (configVersion < UpdateManager.CurrentVersion)
            {
                LogInfo($"Upgrading config to {UpdateManager.CurrentVersion}");
                versionEntry.Value = VersionInfo.Version;
                this.Config.Save();
            }
            else if (configVersion > UpdateManager.CurrentVersion)
            {
                LogError($"The current config is from a newer version of the plugin. If you're trying to downgrade, you should delete the config file and let it regenerate.");
                this.Unload();
                return false;
            }

            return true;
        }

        private void CheckGameVersion()
        {
            var currentGameVersion = CellBuildData.GetRevision();
            this.Config.Bind(ConfigGameVersion, currentGameVersion, new ConfigDescription("Last known game version"));
            var knownGameVersionEntry = this.Config.GetConfigEntry<int>(ConfigGameVersion);
            if (currentGameVersion == knownGameVersionEntry.Value) return;

            try
            {
                if (currentGameVersion < SupportedGameRevision)
                {
                    NativeMethods.MessageBox(
                        hWnd: IntPtr.Zero,
                        text: $"You are attempting to run {ModName} {VersionInfo.Version} on an outdated version of the game.\n" +
                              $"Your current version of {ModName} was built for rev {SupportedGameRevision}.\n" +
                              $"The current game version is: {currentGameVersion}\n\n" +
                              $"This may result in stability problems or even crashes.\n" +
                              $"This warning will NOT be shown again.",
                        caption: "Outdated game revision",
                        options: (int)(NativeMethods.MB_OK | NativeMethods.MB_ICONWARNING | NativeMethods.MB_SYSTEMMODAL));
                }
                else if (currentGameVersion > SupportedGameRevision)
                {
                    NativeMethods.MessageBox(
                        hWnd: IntPtr.Zero,
                        text: $"You are attempting to run {ModName} {VersionInfo.Version} on a newer version of the game.\n" +
                              $"Your current version of {ModName} was built for rev {SupportedGameRevision}.\n" +
                              $"The current game version is: {currentGameVersion}\n\n" +
                              $"This may result in stability problems or even crashes.\n" +
                              $"This warning will NOT be shown again.",
                        caption: "Outdated mod version",
                        options: (int)(NativeMethods.MB_OK | NativeMethods.MB_ICONWARNING | NativeMethods.MB_SYSTEMMODAL));
                }
            }
            finally
            {
                knownGameVersionEntry.Value = currentGameVersion;
            }
        }

        public void RegisterPatch<T>() where T : IPatch, new()
        {
            if (this.harmony == null)
            {
                this.harmony = new Harmony(GUID);
            }

            var patch = new T
            {
                Harmony = this.harmony
            };

            patch.Initialize();

            if (patch.Enabled)
            {
                LogInfo($"Applying patch: {patch.Name}");
                patch.Patch();
            }
        }

        public static void LogDebug(object data) => Instance.Log.LogDebug(data);

        public static void LogError(object data) => Instance.Log.LogError(data);

        public static void LogFatal(object data) => Instance.Log.LogFatal(data);

        public static void LogInfo(object data) => Instance.Log.LogInfo(data);

        public static void LogMessage(object data) => Instance.Log.LogMessage(data);

        public static void LogWarning(object data) => Instance.Log.LogWarning(data);
    }
}
