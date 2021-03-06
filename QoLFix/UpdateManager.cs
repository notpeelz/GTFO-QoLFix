using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BepInEx.Configuration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace QoLFix
{
    public static class UpdateManager
    {
        private static readonly ConfigDefinition ConfigEnabled = new(nameof(UpdateManager), "Enabled");
        private static readonly ConfigDefinition ConfigNotifyPrerelease = new(nameof(UpdateManager), "NotifyPrerelease");

        private static bool ConfigInitialized;

        private static JArray Releases;

        public static Version CurrentVersion { get; } = new Version(VersionInfo.Version);

        public static ReleaseInfo LatestRelease { get; private set; }

        public static bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public static void Initialize()
        {
            if (ConfigInitialized) return;
            ConfigInitialized = true;

            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Enables the update notification system."));
            QoLFixPlugin.Instance.Config.Bind(ConfigNotifyPrerelease, false, new ConfigDescription("Displays update notifications for pre-release versions."));
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

        public static async Task<bool> CheckForUpdate(bool includePrerelease = false, bool force = false)
        {
            LogInfo("Checking for updates");
            LatestRelease = await GetLatestRelease(includePrerelease, force);

            if (LatestRelease == null) throw new FailedUpdateException("Failed fetching the latest release");

            if (LatestRelease.Version > CurrentVersion)
            {
                LogInfo($"New version available: {LatestRelease.Version}");
                return true;
            }

            return false;
        }

        public static async Task<ReleaseInfo> GetLatestRelease(bool includePrerelease = false, bool force = false)
        {
            var tag = default(string);
            try
            {
                if (!force && Releases != null || await UpdateReleaseObject())
                {
                    var allowPrerelease = includePrerelease || QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigNotifyPrerelease).Value;
                    var release = Releases.Children<JObject>().FirstOrDefault(release => !(bool)release["prerelease"] || allowPrerelease);

                    if (release == null) return null;

                    tag = (string)release["tag_name"];
                    if (tag.StartsWith("v")) tag = tag[1..];

                    return new ReleaseInfo
                    {
                        Version = new Version(tag),
                        DownloadUrl = (string)release["html_url"],
                        PreRelease = (bool)release["prerelease"],
                    };
                }
            }
            catch (FormatException ex)
            {
                throw new FailedUpdateException($"Failed to parse version ({tag})", ex);
            }
            catch (Exception ex)
            {
                throw new FailedUpdateException("Failed to fetch the latest release version", ex);
            }

            return null;
        }

        private static async Task<bool> UpdateReleaseObject()
        {
            try
            {
                using var client = new HttpClient()
                {
                    BaseAddress = new Uri($"https://api.github.com/repos/{QoLFixPlugin.RepoName}/releases"),
                };
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.UserAgent.TryParseAdd(QoLFixPlugin.GUID);

                var res = await client.GetAsync("");
                if (!res.IsSuccessStatusCode)
                {
                    LogError($"Failed to fetch release info: {nameof(HttpStatusCode)}.{res.StatusCode}");
                    return false;
                }

                var str = await res.Content.ReadAsStringAsync();
                var arr = JArray.Parse(str);
                Releases = arr;
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to fetch release info: {ex}");
            }

            return false;
        }

        private static void LogDebug(object data) => QoLFixPlugin.LogDebug($"<{nameof(UpdateManager)}> " + data);

        private static void LogError(object data) => QoLFixPlugin.LogError($"<{nameof(UpdateManager)}> " + data);

        private static void LogFatal(object data) => QoLFixPlugin.LogFatal($"<{nameof(UpdateManager)}> " + data);

        private static void LogInfo(object data) => QoLFixPlugin.LogInfo($"<{nameof(UpdateManager)}> " + data);

        private static void LogMessage(object data) => QoLFixPlugin.LogMessage($"<{nameof(UpdateManager)}> " + data);

        private static void LogWarning(object data) => QoLFixPlugin.LogWarning($"<{nameof(UpdateManager)}> " + data);

        public class ReleaseInfo
        {
            public ReleaseInfo() { }

            public string DownloadUrl { get; set; }

            public Version Version { get; set; }

            public bool PreRelease { get; set; }
        }
    }
}
