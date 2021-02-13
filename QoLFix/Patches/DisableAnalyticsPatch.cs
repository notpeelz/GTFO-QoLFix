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

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            QoLFixPlugin.Instance.Log.LogWarning($"<{PatchName}> {WarningMessage}");
            this.PatchMethod<AnalyticsManager>(nameof(AnalyticsManager.TryPostEvent), PatchType.Prefix);
            this.PatchMethod<AnalyticsManager>(nameof(AnalyticsManager.OnGameEvent), PatchType.Prefix);
        }

        private static bool AnalyticsManager__OnGameEvent__Prefix() => false;

        private static bool AnalyticsManager__TryPostEvent__Prefix() => false;
    }
}
