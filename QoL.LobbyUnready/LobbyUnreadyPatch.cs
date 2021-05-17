using AK;
using BepInEx.Configuration;
using CellMenu;
using HarmonyLib;
using MTFO.Core;
using SNetwork;

namespace QoL.LobbyUnready
{
    public class LobbyUnreadyPatch : MTFOPatch
    {
        private const string PatchName = nameof(LobbyUnreadyPatch);

        private static ConfigEntry<bool> ConfigEnabled = default!;

        public static LobbyUnreadyPatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Lets you unready after readying up."));
        }

        [HarmonyPatch(typeof(CM_PageLoadout))]
        [HarmonyPatch(nameof(CM_PageLoadout.UpdateReadyButton))]
        [HarmonyPrefix]
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
