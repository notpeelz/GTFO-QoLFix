using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Configuration;
using Newtonsoft.Json.Linq;
using QoLFix.UI;

namespace QoLFix
{
    public static class UpdateManager
    {
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(nameof(UpdateManager), "Enabled");
        private static readonly ConfigDefinition ConfigNotifyPrerelease = new ConfigDefinition(nameof(UpdateManager), "NotifyPrerelease");

        private static JArray Releases;

        public static Version CurrentVersion { get; } = new Version(VersionInfo.Version);

        public static ReleaseInfo LatestRelease { get; private set; }

        public static string LatestReleaseUrl { get; private set; }

        public static void Initialize()
        {

            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Enables the update notification system."));
            QoLFixPlugin.Instance.Config.Bind(ConfigNotifyPrerelease, false, new ConfigDescription("Displays update notifications for pre-release versions."));

            if (!QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value) return;

            new Thread(async () => await CheckForUpdate()).Start();
        }

        public static async Task CheckForUpdate(bool force = false)
        {
            LogInfo("Checking for updates");
            LatestRelease = await GetLatestRelease(force);
            if (LatestRelease == null) return;

            if (LatestRelease.Version > CurrentVersion)
            {
                LogInfo($"New version available: {LatestRelease.Version}");
                UpdateNotifier.SetNotificationVisibility(true);
            }
        }

        public static async Task<ReleaseInfo> GetLatestRelease(bool force = false)
        {
            var tag = default(string);
            try
            {
                if (!force && Releases != null || await UpdateReleaseObject())
                {
                    var allowPrerelease = QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigNotifyPrerelease).Value;
                    var release = Releases.Children<JObject>().FirstOrDefault(release => allowPrerelease ^ (bool)release["prerelease"]);

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
                LogError($"Failed to parse version ({tag}) : {ex}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to fetch the latest release version: {ex}");
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
                LogError($"Failed to fetch release info: " + ex);
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
