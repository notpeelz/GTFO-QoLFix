using System;
using System.Linq;
using System.Threading;
using BepInEx.IL2CPP;
using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using QoLFix.UI;
using QoLFix.Patches.Misc;
using QoLFix.Patches.Annoyances;
using QoLFix.Patches.Tweaks;
using QoLFix.Patches.Bugfixes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using QoLFix.Patches.Common.Cursor;
using QoLFix.Updater;
using QoLFix.Updater.UI;
using UnityEngine.CrashReportHandler;

namespace QoLFix
{
    [BepInPlugin(GUID, ModName, VersionInfo.SemVer)]
    [BepInProcess("GTFO.exe")]
    public class QoLFixPlugin : BasePlugin
    {
        internal const string ModName = "QoL Fix";
#if RELEASE
        internal const string GUID = "dev.peelz.qolfix";
#else
        internal const string GUID = "dev.peelz.qolfix.dev";
#endif
        internal const string RepoName = "notpeelz/GTFO-QoLFix";

        internal static readonly bool IsReleaseBuild = VersionInfo.GitBranch == "master";

        public const int SupportedGameRevision = 21989;

        private const string SectionMain = "Config";
        private static readonly ConfigDefinition ConfigVersion = new(SectionMain, "Version");
        private static readonly ConfigDefinition ConfigGameVersion = new(SectionMain, "GameVersion");

        private static Harmony HarmonyInstance;
        private static readonly Dictionary<Type, Patch> RegisteredPatches = new();

        public static QoLFixPlugin Instance { get; private set; }

        public QoLFixPlugin()
        {
            this.Config.SaveOnConfigSet = false;
        }

        public override void Load()
        {
            Instance = this;

#if RELEASE_STANDALONE
            LogMessage($"Initializing plugin [{VersionInfo.SemVer}] (Standalone build)");
#elif RELEASE_THUNDERSTORE
            LogMessage($"Initializing plugin [{VersionInfo.SemVer}] (Thunderstore build)");
#else
            LogMessage($"Initializing plugin [{VersionInfo.SemVer}] (Dev build)");
#endif

            if (!this.CheckConfigVersion()) return;

            UpdateManager.Initialize();
            this.CheckGameVersion();
            this.CheckUnityLibs();

            this.Config.SaveOnConfigSet = true;

            // Common
            RegisterPatch<DisableAnalyticsPatch>();
            RegisterPatch<DisableCrashReporterPatch>();
            RegisterPatch<UnityCursorPatch>();

            // Misc
            RegisterPatch<DisableSteamRichPresencePatch>();
            RegisterPatch<RecentlyPlayedWithPatch>();
            RegisterPatch<FramerateLimiterPatch>();
            RegisterPatch<ModInfoWatermarkPatch>();
            RegisterPatch<LevelCleanupPatch>();

            // Annoyances
            RegisterPatch<IntroSkipPatch>();
            RegisterPatch<ElevatorIntroSkipPatch>();
            RegisterPatch<ElevatorVolumePatch>();

            // Tweaks
            RegisterPatch<LobbyUnreadyPatch>();
            RegisterPatch<SteamProfileLinkPatch>();
            RegisterPatch<NoiseRemovalPatch>();
            RegisterPatch<TerminalPingableSwapsPatch>();
            RegisterPatch<HideCrosshairPatch>();
            RegisterPatch<DropResourcesPatch>();
            RegisterPatch<BetterWeaponSwapPatch>();
            RegisterPatch<LatencyHUDPatch>();
            RegisterPatch<ResourceAudioCuePatch>();
            RegisterPatch<RecentlyPlayedWithPatch>();
            RegisterPatch<ScreenLiquidRemovalPatch>();
            RegisterPatch<BetterInteractionsPatch>();
            RegisterPatch<RunReloadCancelPatch>();
            RegisterPatch<BetterMovementPatch>();
            RegisterPatch<PlayerAmbientLightPatch>();

            // Bug fixes
            RegisterPatch<FixToolRefillBioScannerPatch>();
            RegisterPatch<FixDoorCollisionPatch>();
            RegisterPatch<FixDoorFramePingPatch>();
            RegisterPatch<FixTerminalDisplayPingPatch>();
            RegisterPatch<FixBioScannerNavMarkerPatch>();
            RegisterPatch<FixLockerPingPatch>();
            RegisterPatch<FixSoundMufflePatch>();
            RegisterPatch<FixFlashlightStatePatch>();
            RegisterPatch<FixWeaponAnimationsPatch>();

            // XXX: needs to execute after everything else
            RegisterPatch<ReparentPickupPatch>();

            this.Config.Save();

            UIManager.Initialized += () =>
            {
                UpdateNotifier.Initialize();

                if (!UpdateManager.Enabled) return;
                new Thread(async () =>
                {
                    try
                    {
                        if (await UpdateManager.CheckForUpdate())
                        {
                            UpdateNotifier.SetNotificationVisibility(true);
                        }
                    }
                    catch (NotImplementedException)
                    {
                        LogWarning($"{nameof(UpdateManager)}.{nameof(UpdateManager.CheckForUpdate)} is not implemented");
                    }
                    catch (Exception ex)
                    {
                        LogError($"Failed checking for update: {ex}");
                    }
                }).Start();
            };
            UIManager.Initialize();
        }

        private bool CheckConfigVersion()
        {
            this.Config.Bind(ConfigVersion, VersionInfo.Version, new ConfigDescription("Used internally for config upgrades; don't touch!"));

            var versionEntry = this.Config.GetConfigEntry<string>(ConfigVersion);
            if (!SemVer.Version.TryParse(versionEntry.Value, true, out var configVersion))
            {
                LogError($"Failed parsing semver: {versionEntry.Value}");
                return false;
            }

            if (configVersion < UpdateManager.CurrentVersion)
            {
                LogMessage($"Upgrading config to {UpdateManager.CurrentVersion}");
                versionEntry.Value = VersionInfo.Version;
                this.Config.Save();
            }
            else if (configVersion > UpdateManager.CurrentVersion)
            {
                LogError("The current config is from a newer version of the plugin."
                    + " If you're trying to downgrade, you should delete the config file and let it regenerate.");
                return false;
            }

            return true;
        }

#pragma warning disable CS0162
        private void CheckGameVersion()
        {
            var currentGameVersion = CellBuildData.GetRevision();
            this.Config.Bind(ConfigGameVersion, currentGameVersion, new ConfigDescription("Last known game version; don't touch!"));
            var knownGameVersionEntry = this.Config.GetConfigEntry<int>(ConfigGameVersion);

            // Up to date
            if (currentGameVersion == SupportedGameRevision) return;

            // Check if we're on a downgraded game version
            if (currentGameVersion < SupportedGameRevision)
            {
                if (currentGameVersion == knownGameVersionEntry.Value) return;
                knownGameVersionEntry.Value = currentGameVersion;

                NativeMethods.MessageBox(
                    hWnd: IntPtr.Zero,
                    text: $"You are attempting to run {ModName} {VersionInfo.Version} on an outdated version of the game.\n" +
                        $"Your current version of {ModName} was built for: {SupportedGameRevision}\n" +
                        $"The current game version is: {currentGameVersion}\n\n" +
                        "This may result in stability problems or crashes.\n" +
                        "This warning will NOT be shown again.",
                    caption: $"{ModName} - Outdated game revision",
                    options: (int)(NativeMethods.MB_OK | NativeMethods.MB_ICONWARNING | NativeMethods.MB_SYSTEMMODAL));

                return;
            }

            // We're running on a newer game version
            var btn = NativeMethods.MessageBox(
                hWnd: IntPtr.Zero,
                caption: $"{ModName} - Outdated mod version",
                text: $"You are attempting to run {ModName} {VersionInfo.Version} on a newer version of the game.\n" +
                    $"Your current version of {ModName} was built for: {SupportedGameRevision}\n" +
                    $"The current game version is: {currentGameVersion}\n\n" +
                    "This may result in stability problems or crashes." +
#if RELEASE_STANDALONE
                    "\nWould you like to check if there's a new update available?",
                options: (int)(NativeMethods.MB_YESNO | NativeMethods.MB_ICONWARNING | NativeMethods.MB_SYSTEMMODAL));
#else
#if RELEASE_THUNDERSTORE
                    "\nCheck your mod manager for updates.",
#else
                    "",
#endif
                options: (int)(NativeMethods.MB_OK | NativeMethods.MB_ICONWARNING | NativeMethods.MB_SYSTEMMODAL));

            return;
#endif

            if (btn != NativeMethods.IDYES) return;

            bool updateAvailable;
            try
            {
                // Yes, this is blocking... however it doesn't matter because
                // the game hasn't booted yet :)
                var updateCheck = UpdateManager.CheckForUpdate(includePrerelease: true);
                updateCheck.Wait();
                updateAvailable = updateCheck.Result;
            }
            catch (AggregateException aggregateException)
            {
                var exceptions = aggregateException.InnerExceptions
                    .Where(x => x is not NotImplementedException)
                    .ToArray();

                if (exceptions.Length > 0)
                {
                    for (var i = 0; i < exceptions.Length; i++)
                    {
                        LogError($"Failed checking for update (ex[{i}]): {exceptions[i]}");
                    }
                    NativeMethods.MessageBox(
                        hWnd: IntPtr.Zero,
                        text: "Failed to check for updates; check your BepInEx logs.",
                        caption: $"{ModName} - Failed to check for updates",
                        options: (int)(NativeMethods.MB_OK | NativeMethods.MB_ICONERROR | NativeMethods.MB_SYSTEMMODAL));
                }
                else if (aggregateException.InnerExceptions.Count > 0)
                {
                    NativeMethods.MessageBox(
                        hWnd: IntPtr.Zero,
                        text: "Failed to check for updates (not implemented)",
                        caption: $"{ModName} - Failed to check for updates",
                        options: (int)(NativeMethods.MB_OK | NativeMethods.MB_ICONERROR | NativeMethods.MB_SYSTEMMODAL));
                }
                else
                {
                    NativeMethods.MessageBox(
                        hWnd: IntPtr.Zero,
                        text: "Failed to check for updates (unknown reason)",
                        caption: $"{ModName} - Failed to check for updates",
                        options: (int)(NativeMethods.MB_OK | NativeMethods.MB_ICONERROR | NativeMethods.MB_SYSTEMMODAL));
                }

                return;
            }

            if (!updateAvailable)
            {
                NativeMethods.MessageBox(
                    hWnd: IntPtr.Zero,
                    text: "No update available yet; check again later.",
                    caption: $"{ModName} - No update available",
                    options: (int)(NativeMethods.MB_OK | NativeMethods.MB_ICONINFORMATION | NativeMethods.MB_SYSTEMMODAL));
                return;
            }

            NativeMethods.MessageBox(
                hWnd: IntPtr.Zero,
                text: $"A new update is available: {UpdateManager.GetLatestReleaseName()}\n" +
                    "Press 'OK' to open the download page.",
                caption: $"{ModName} - Update available",
                options: (int)(NativeMethods.MB_OK | NativeMethods.MB_ICONINFORMATION | NativeMethods.MB_SYSTEMMODAL));

            UpdateManager.OpenReleasePage();
            Application.Quit();
        }
#pragma warning restore CS0162

        private bool CheckUnityLibs()
        {
            var unstripped = true;
            try
            {
                var sceneManager = typeof(SceneManager).GetMethod(nameof(SceneManager.add_sceneLoaded));
                unstripped &= sceneManager != null;
                var goActive = typeof(GameObject).GetMethod($"get_{nameof(GameObject.active)}");
                unstripped &= goActive != null;
                var colorAlpha = typeof(Color).GetMethod(nameof(Color.AlphaMultiplied));
                unstripped &= colorAlpha != null;
                var enableCaptureExceptions = typeof(CrashReportHandler).GetMethod($"set_{nameof(CrashReportHandler.enableCaptureExceptions)}");
                unstripped &= enableCaptureExceptions != null;
            }
            catch
            {
                unstripped = false;
            }

            if (!unstripped)
            {
                var btn = NativeMethods.MessageBox(
                    hWnd: IntPtr.Zero,
                    text: $"Looks like you don't have the Unity libraries installed. {ModName} requires unstripped assemblies in order to work properly.\n" +
                        "Would you like to open the installation instructions?\n" +
                        $"Pressing 'No' will launch your game without {ModName}.",
                    caption: $"{ModName} - Missing Unity libraries",
                    options: (int)(NativeMethods.MB_YESNO | NativeMethods.MB_ICONWARNING | NativeMethods.MB_SYSTEMMODAL));

                if (btn == NativeMethods.IDYES)
                {
                    Application.OpenURL($"https://github.com/{RepoName}/blob/master/README.md");
                    Application.Quit();
                }

                return false;
            }

            return true;
        }

        public static void RegisterPatch<T>() where T : Patch, new()
        {
            if (HarmonyInstance == null)
            {
                HarmonyInstance = new Harmony(GUID);
            }

            if (RegisteredPatches.ContainsKey(typeof(T)))
            {
                LogDebug($"Ignoring duplicate patch: {typeof(T).Name}");
                return;
            }

            var patch = new T
            {
                Harmony = HarmonyInstance,
            };

            patch.Initialize();

            if (patch.Enabled)
            {
                LogInfo($"Applying patch: {patch.Name}");
                patch.Execute();
            }

            RegisteredPatches[typeof(T)] = patch;
        }

        public static void LogDebug(object data) => Instance.Log.LogDebug(data);

        public static void LogError(object data) => Instance.Log.LogError(data);

        public static void LogFatal(object data) => Instance.Log.LogFatal(data);

        public static void LogInfo(object data) => Instance.Log.LogInfo(data);

        public static void LogMessage(object data) => Instance.Log.LogMessage(data);

        public static void LogWarning(object data) => Instance.Log.LogWarning(data);
    }
}
