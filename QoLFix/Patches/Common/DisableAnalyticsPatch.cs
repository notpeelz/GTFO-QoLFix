using System.Linq;
using HarmonyLib;

namespace QoLFix.Patches.Common
{
    public class DisableAnalyticsPatch : IPatch
    {
        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
        }

        public string Name { get; } = nameof(DisableAnalyticsPatch);

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<GS_Startup>(nameof(GS_Startup.Enter), PatchType.Postfix);
            this.PatchMethod<AnalyticsManager>(nameof(AnalyticsManager.TryPostEvent), PatchType.Prefix);
            this.PatchMethod<AnalyticsManager>(nameof(AnalyticsManager.OnGameEvent), PatchType.Prefix);
        }

        private static bool NoAnalytics;

        private static void GS_Startup__Enter__Postfix()
        {
            var args = Il2CppSystem.Environment.GetCommandLineArgs();
            NoAnalytics = args.Skip(1).Contains("-noanalytics");

            if (NoAnalytics)
            {
                Instance.LogWarning("ANALYTICS DISABLED!");
            }
        }

        private static bool AnalyticsManager__OnGameEvent__Prefix() =>
            NoAnalytics
                ? HarmonyControlFlow.DontExecute
                : HarmonyControlFlow.Execute;

        private static bool AnalyticsManager__TryPostEvent__Prefix() =>
            NoAnalytics
                ? HarmonyControlFlow.DontExecute
                : HarmonyControlFlow.Execute;
    }
}
