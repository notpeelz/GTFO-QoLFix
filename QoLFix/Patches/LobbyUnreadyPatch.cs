using AK;
using BepInEx.Configuration;
using CellMenu;
using HarmonyLib;
using SNetwork;

namespace QoLFix.Patches
{
    public class LobbyUnreadyPatch : IPatch
    {
        private static readonly string PatchName = nameof(LobbyUnreadyPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Lets you unready after readying up."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<CM_PageLoadout>(nameof(CM_PageLoadout.UpdateReadyButton), PatchType.Prefix);
        }

        private static bool CM_PageLoadout__UpdateReadyButton__Prefix(CM_PageLoadout __instance)
        {
            if (PlayfabMatchmakingManager.Current.IsMatchmakeInProgress) return true;
            if (SNet.IsMaster || !GameStateManager.IsReady) return true;

            __instance.m_readyButton.SetText("UNREADY");
            __instance.m_readyButton.SOUND_CLICK_HOLD_START = EVENTS.MENU_READY_PRESS_AND_HOLD;
            __instance.m_readyButton.SOUND_CLICK_HOLD_CANCEL = EVENTS.MENU_READY_PRESS_AND_HOLD_CANCEL;
            __instance.m_readyButton.SetButtonEnabled(true);
            return false;
        }
    }
}
