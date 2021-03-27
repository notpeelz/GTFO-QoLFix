#if !DEBUG
using System.Linq;
#endif

namespace QoLFix.Patches.Misc
{
    public class DisableAnalyticsPatch : Patch
    {
        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
        }

        public override string Name { get; } = nameof(DisableAnalyticsPatch);

        public override void Execute()
        {
            this.PatchMethod<GS_Startup>(nameof(GS_Startup.Enter), PatchType.Postfix);
            this.PatchMethod<AnalyticsManager>(nameof(AnalyticsManager.TryPostEvent), PatchType.Prefix);
            this.PatchMethod<AnalyticsManager>(nameof(AnalyticsManager.OnGameEvent), PatchType.Prefix);
        }

        private static bool NoAnalytics;

        private static void GS_Startup__Enter__Postfix()
        {
            var args = Il2CppSystem.Environment.GetCommandLineArgs();
#if RELEASE
            NoAnalytics = args.Skip(1).Contains("-noanalytics");
#else
            NoAnalytics = true;
#endif

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
