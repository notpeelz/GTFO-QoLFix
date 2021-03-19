using BepInEx.Configuration;
using CellMenu;

namespace QoLFix.Patches.Annoyances
{
    public class IntroSkipPatch : Patch
    {
        private const string PatchName = nameof(IntroSkipPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");
        private static readonly ConfigDefinition ConfigSkipRundownInfo = new(PatchName, "SkipRundownInfo");
        private static readonly ConfigDefinition ConfigSkipRundownConnect = new(PatchName, "SkipRundownConnect");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Skips the intro on startup."));
            QoLFixPlugin.Instance.Config.Bind(ConfigSkipRundownInfo, false, new ConfigDescription("Skips the rundown info screen."));
            QoLFixPlugin.Instance.Config.Bind(ConfigSkipRundownConnect, true, new ConfigDescription("Skips the rundown connect and reveal animation"));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => ConfigEnabled.GetConfigEntry<bool>().Value;

        public override void Execute()
        {
            this.PatchMethod<CM_PageIntro>(nameof(CM_PageIntro.Update), PatchType.Postfix);
            this.PatchMethod<CM_PageIntro>(nameof(CM_PageIntro.StartInitializing), PatchType.Prefix);
            this.PatchMethod<CM_PageRundown_New>(nameof(CM_PageRundown_New.Setup), PatchType.Prefix);
        }

        private static void CM_PageRundown_New__Setup__Prefix(CM_PageRundown_New __instance)
        {
            __instance.m_cortexIntroIsDone = true;
            __instance.m_rundownIntroIsDone = ConfigSkipRundownConnect.GetConfigEntry<bool>().Value;
        }

        private static void CM_PageRundown_New__TryPlaceRundown(CM_PageRundown_New __instance)
        {
            if (!ConfigSkipRundownConnect.GetConfigEntry<bool>().Value) return;

            __instance.m_buttonConnect.SetVisible(false);
            __instance.SetRundownFullyRevealed();
        }

        private static bool CM_PageIntro__StartInitializing__Prefix(CM_PageIntro __instance)
        {
            SkipIntro(__instance);
            return HarmonyControlFlow.DontExecute;
        }

        private static CM_IntroStep? previousStep;

        private static void CM_PageIntro__Update__Postfix(CM_PageIntro __instance)
        {
            if (previousStep != __instance.m_step)
            {
                Instance.LogDebug($"New {nameof(CM_PageIntro)} step: {__instance.m_step}");
                previousStep = __instance.m_step;
            }

            var skipRundownInfo = ConfigSkipRundownInfo.GetConfigEntry<bool>().Value;
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
