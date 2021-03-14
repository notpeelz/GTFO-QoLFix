using AK;
using BepInEx.Configuration;
using CellMenu;
using SNetwork;

namespace QoLFix.Patches.Tweaks
{
    public class LobbyUnreadyPatch : Patch
    {
        private const string PatchName = nameof(LobbyUnreadyPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Lets you unready after readying up."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public override void Execute()
        {
            this.PatchMethod<CM_PageLoadout>(nameof(CM_PageLoadout.UpdateReadyButton), PatchType.Prefix);
        }

        private static bool CM_PageLoadout__UpdateReadyButton__Prefix(CM_PageLoadout __instance)
        {
            if (PlayfabMatchmakingManager.Current.IsMatchmakeInProgress) return HarmonyControlFlow.Execute;
            if (SNet.IsMaster || !GameStateManager.IsReady) return HarmonyControlFlow.Execute;

            __instance.m_readyButton.SetText("UNREADY");
            __instance.m_readyButton.SOUND_CLICK_HOLD_START = EVENTS.MENU_READY_PRESS_AND_HOLD;
            __instance.m_readyButton.SOUND_CLICK_HOLD_CANCEL = EVENTS.MENU_READY_PRESS_AND_HOLD_CANCEL;
            __instance.m_readyButton.SetButtonEnabled(true);
            return HarmonyControlFlow.DontExecute;
        }
    }
}
