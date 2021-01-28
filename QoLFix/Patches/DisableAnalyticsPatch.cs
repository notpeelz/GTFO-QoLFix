using BepInEx.Configuration;
using HarmonyLib;

namespace QoLFix.Patches
{
    public class DisableAnalyticsPatch : IPatch
    {
        private static readonly string PatchName = nameof(DisableAnalyticsPatch);
        private static readonly string WarningMessage = "This patch is mostly useful to prevent spamming the developers with useless analytics data when debugging/testing. Please don't use this during actual gameplay!";
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, false, new ConfigDescription(WarningMessage));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public void Patch(Harmony harmony)
        {
            QoLFixPlugin.Instance.Log.LogWarning($"<{PatchName}> {WarningMessage}");
            {
                var methodInfo = typeof(AnalyticsManager).GetMethod(nameof(AnalyticsManager.TryPostEvent));
                harmony.Patch(methodInfo, prefix: new HarmonyMethod(AccessTools.Method(typeof(DisableAnalyticsPatch), nameof(AnalyticsManager__TryPostEvent))));
            }
            {
                var methodInfo = typeof(AnalyticsManager).GetMethod(nameof(AnalyticsManager.OnGameEvent));
                harmony.Patch(methodInfo, prefix: new HarmonyMethod(AccessTools.Method(typeof(DisableAnalyticsPatch), nameof(AnalyticsManager__OnGameEvent))));
            }
        }

        private static bool AnalyticsManager__OnGameEvent() => false;

        private static bool AnalyticsManager__TryPostEvent() => false;
    }
}
