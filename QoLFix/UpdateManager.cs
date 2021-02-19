using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QoLFix.UI;

namespace QoLFix
{
    public static class UpdateManager
    {
        private static JObject LatestRelease;

        public static Version CurrentVersion { get; } = new Version(VersionInfo.Version);

        public static Version LatestVersion { get; private set; }

        public static string LatestReleaseUrl { get; private set; }

        public static void Initialize()
        {
            new Thread(async () => await CheckForUpdate()).Start();
        }

        public static async Task CheckForUpdate(bool force = false)
        {
            LogInfo("Checking for updates");
            LatestVersion = await GetLatestVersion(force);
            if (LatestVersion > CurrentVersion)
            {
                LogInfo($"New version available: {LatestVersion}");
                UpdateNotifier.SetNotificationVisibility(true);
            }
        }

        public static async Task<Version> GetLatestVersion(bool force = false)
        {
            try
            {
                if (!force && LatestRelease != null || await UpdateReleaseObject())
                {
                    var tag = (string)LatestRelease["tag_name"];
                    if (tag.StartsWith("v")) tag = tag[1..];
                    return new Version(tag);
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to parse version: " + ex);
            }

            return null;
        }

        private static async Task<bool> UpdateReleaseObject()
        {
            try
            {
                using var client = new HttpClient()
                {
                    BaseAddress = new Uri($"https://api.github.com/repos/{QoLFixPlugin.RepoName}/releases/latest"),
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
                var obj = JObject.Parse(str);
                LatestRelease = obj;
                LatestReleaseUrl = (string)obj["html_url"];
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
    }
}
