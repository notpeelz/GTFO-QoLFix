using BepInEx.Configuration;
using CellMenu;
using HarmonyLib;
using MTFO.Core;

namespace QoL.IntroSkip
{
    public class IntroSkipPatch : MTFOPatch
    {
        private const string PatchName = nameof(IntroSkipPatch);

        private static ConfigEntry<bool> ConfigEnabled = default!;
        private static ConfigEntry<bool> ConfigSkipRundownInfo = default!;
        private static ConfigEntry<bool> ConfigSkipRundownConnect = default!;

        public static IntroSkipPatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Skips the intro on startup."));
            ConfigSkipRundownInfo = this.Plugin.Config.Bind(new(PatchName, "SkipRundownInfo"), false,
                new ConfigDescription("Skips the rundown info screen."));
            ConfigSkipRundownConnect = this.Plugin.Config.Bind(new(PatchName, "SkipRundownConnect"), true,
                new ConfigDescription("Skips the rundown connect and reveal animation."));
        }

        [HarmonyPatch(typeof(CM_PageRundown_New))]
        [HarmonyPatch(nameof(CM_PageRundown_New.Setup))]
        [HarmonyPrefix]
        private static void CM_PageRundown_New__Setup__Prefix(CM_PageRundown_New __instance)
        {
            __instance.m_cortexIntroIsDone = true;

            // Setting this to true will cause the Intro_RevealRundown
            // coroutine to get skipped. This causes the tier requirements to
            // never get enabled (they're hidden by default).
            // The CM_PageRundown_New::PlaceRundown postfix fixes this.
            __instance.m_rundownIntroIsDone = ConfigSkipRundownConnect.Value;
        }

        [HarmonyPatch(typeof(CM_PageRundown_New))]
        [HarmonyPatch(nameof(CM_PageRundown_New.PlaceRundown))]
        [HarmonyPostfix]
        private static void CM_PageRundown_New__PlaceRundown__Postfix(CM_PageRundown_New __instance)
        {
            if (!ConfigSkipRundownConnect.Value) return;

            __instance.m_tierMarker1?.SetVisible(true);
            __instance.m_tierMarker2?.SetVisible(true);
            __instance.m_tierMarker3?.SetVisible(true);
            __instance.m_tierMarker4?.SetVisible(true);
            __instance.m_tierMarker5?.SetVisible(true);
        }

        [HarmonyPatch(typeof(CM_PageIntro))]
        [HarmonyPatch(nameof(CM_PageIntro.StartInitializing))]
        [HarmonyPrefix]
        private static bool CM_PageIntro__StartInitializing__Prefix(CM_PageIntro __instance)
        {
            SkipIntro(__instance);
            return HarmonyControlFlow.DontExecute;
        }

        private static CM_IntroStep? previousStep;

        [HarmonyPatch(typeof(CM_PageIntro))]
        [HarmonyPatch(nameof(CM_PageIntro.Update))]
        [HarmonyPostfix]
        private static void CM_PageIntro__Update__Postfix(CM_PageIntro __instance)
        {
            if (previousStep != __instance.m_step)
            {
                Instance!.LogDebug($"New {nameof(CM_PageIntro)} step: {__instance.m_step}");
                previousStep = __instance.m_step;
            }

            if (!ConfigSkipRundownInfo.Value) return;

            if (PlayFabManager.LoggedIn && __instance.m_step == CM_IntroStep.StartupScreenWaitingForPlayfab)
            {
                SkipIntro(__instance);
            }
        }

        private static void SkipIntro(CM_PageIntro intro)
        {
            intro.m_startupScreen.SetVisible(false);
            intro.m_cursor.SetVisible(false);
            intro.m_step = CM_IntroStep.IntroDone;
            intro.OnInjectDone();
        }
    }
}
