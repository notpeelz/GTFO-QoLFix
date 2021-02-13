using BepInEx.Configuration;
using CellMenu;
using HarmonyLib;

namespace QoLFix.Patches
{
    public class IntroSkipPatch : IPatch
    {
        private static readonly string PatchName = nameof(IntroSkipPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");
        private static readonly ConfigDefinition ConfigSkipRundownInfo = new ConfigDefinition(PatchName, "SkipRundownInfo");
        private static readonly ConfigDefinition ConfigSkipRundownConnect = new ConfigDefinition(PatchName, "SkipRundownConnect");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Skips the intro on startup."));
            QoLFixPlugin.Instance.Config.Bind(ConfigSkipRundownInfo, false, new ConfigDescription("Skips the rundown info screen."));
            QoLFixPlugin.Instance.Config.Bind(ConfigSkipRundownConnect, true, new ConfigDescription("Skips the rundown connect and reveal animation"));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<CM_PageIntro>(nameof(CM_PageIntro.Update), PatchType.Postfix);
            this.PatchMethod<CM_PageIntro>(nameof(CM_PageIntro.StartInitializing), PatchType.Prefix);
            this.PatchMethod<CM_PageRundown_New>(nameof(CM_PageRundown_New.Setup), PatchType.Prefix);
        }

        private static void CM_PageRundown_New__Setup__Prefix(CM_PageRundown_New __instance)
        {
            __instance.m_cortexIntroIsDone = true;
            __instance.m_rundownIntroIsDone = QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigSkipRundownConnect).Value;
        }

        private static void CM_PageRundown_New__TryPlaceRundown(CM_PageRundown_New __instance)
        {
            if (!QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigSkipRundownConnect).Value) return;

            __instance.m_buttonConnect.SetVisible(false);
            __instance.SetRundownFullyRevealed();
        }

        private static bool CM_PageIntro__StartInitializing__Prefix(CM_PageIntro __instance)
        {
            SkipIntro(__instance);
            return false;
        }

        private static CM_IntroStep? previousStep;

        private static void CM_PageIntro__Update__Postfix(CM_PageIntro __instance)
        {
            if (previousStep != __instance.m_step)
            {
                Instance.LogDebug($"New {nameof(CM_PageIntro)} step: {__instance.m_step}");
                previousStep = __instance.m_step;
            }

            var skipRundownInfo = QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigSkipRundownInfo).Value;
            if (!skipRundownInfo) return;

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
