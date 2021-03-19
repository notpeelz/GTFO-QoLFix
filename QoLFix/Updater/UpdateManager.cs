using System.Threading.Tasks;
using BepInEx.Configuration;
using UnityEngine;

namespace QoLFix.Updater
{
    public static partial class UpdateManager
    {
        private static readonly ConfigDefinition ConfigEnabled = new(nameof(UpdateManager), "Enabled");
#if RELEASE_STANDALONE
        private static readonly ConfigDefinition ConfigNotifyPrerelease = new(nameof(UpdateManager), "NotifyPrerelease");
#endif

        private static bool ConfigInitialized;

        public static SemVer.Version CurrentVersion { get; } = SemVer.Version.Parse(VersionInfo.Version);

        public static ReleaseInfo LatestRelease { get; private set; }

        // TODO: add Thunderstore support once they fix their API
#if RELEASE_THUNDERSTORE
        public static bool Enabled => false;
#else
        public static bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;
#endif

        public static void Initialize()
        {
            if (ConfigInitialized) return;
            ConfigInitialized = true;

            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Enables the update notification system."));
#if RELEASE_STANDALONE
            QoLFixPlugin.Instance.Config.Bind(ConfigNotifyPrerelease, false, new ConfigDescription("Displays update notifications for pre-release versions."));
#endif
        }

        public static void OpenReleasePage()
        {
            Application.OpenURL(LatestRelease?.DownloadUrl ?? $"https://github.com/{QoLFixPlugin.RepoName}");
        }

        public static string GetLatestReleaseName()
        {
            var versionName = $"v{LatestRelease?.Version?.ToString()}";
            if (LatestRelease?.PreRelease == true)
            {
                versionName += " (pre-release)";
            }

            return versionName;
        }

#pragma warning disable CS1998
#pragma warning disable CS0162
        public static async Task<bool> CheckForUpdate(bool includePrerelease = false, bool force = false)
        {
            LogInfo("Checking for updates");

#if RELEASE_THUNDERSTORE
            LogWarning("Update checking for Thunderstore is not yet supported");
            return false;
#elif RELEASE_STANDALONE
            LatestRelease = await GitHubAPI.GetLatestRelease(includePrerelease, force);
#else
            throw new System.NotImplementedException();
#endif

            if (LatestRelease == null) throw new FailedUpdateException("Failed fetching the latest release");

            if (LatestRelease.Version > CurrentVersion)
            {
                LogMessage($"New version available: {LatestRelease.Version}");
                return true;
            }

            return false;
        }
#pragma warning restore CS0162
#pragma warning restore CS1998

        private static void LogDebug(object data) => QoLFixPlugin.LogDebug($"<{nameof(UpdateManager)}> " + data);

        private static void LogError(object data) => QoLFixPlugin.LogError($"<{nameof(UpdateManager)}> " + data);

        private static void LogFatal(object data) => QoLFixPlugin.LogFatal($"<{nameof(UpdateManager)}> " + data);

        private static void LogInfo(object data) => QoLFixPlugin.LogInfo($"<{nameof(UpdateManager)}> " + data);

        private static void LogMessage(object data) => QoLFixPlugin.LogMessage($"<{nameof(UpdateManager)}> " + data);

        private static void LogWarning(object data) => QoLFixPlugin.LogWarning($"<{nameof(UpdateManager)}> " + data);
    }
}
