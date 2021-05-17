using AK;
using BepInEx.Configuration;
using MTFO.Core;
using HarmonyLib;
using MTFO.Core.Scheduling;

namespace QoL.BugFixes
{
    public class SoundMuffleBugFixPatch : MTFOPatch
    {
        private const string PatchName = nameof(SoundMuffleBugFixPatch);
        private const float RESET_DELAY = 15f;

        private static ConfigEntry<bool> ConfigEnabled = default!;

        public static SoundMuffleBugFixPatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Fixes several bugs related to sound distortion (e.g. scout muffle not resetting)."));
        }

        private static ScheduledAction AudioTimer = default!;

        protected override void Apply()
        {
            base.Apply();
            AudioTimer = ActionScheduler.Repeat(() =>
            {
                Instance!.LogDebug("Resetting");
                CellSound.SetGlobalRTPCValue(GAME_PARAMETERS.SCOUT_SCREAM_DUCKING, 0);
                AudioTimer.Pause();
            }, RESET_DELAY);
            AudioTimer.Pause();
        }

        [HarmonyPatch(typeof(EnemyVoice))]
        [HarmonyPatch(nameof(EnemyVoice.PlayVoiceEvent))]
        [HarmonyPostfix]
        private static void EnemyVoice__PlayVoiceEvent__Postfix(uint ID)
        {
            if (ID != EVENTS.SCOUT_DETECT_SCREAM) return;
            Instance!.LogDebug("Scheduling audio reset");
            AudioTimer.Resume();
        }

        [HarmonyPatch(typeof(GameStateManager))]
        [HarmonyPatch(nameof(GameStateManager.ChangeState))]
        [HarmonyPostfix]
        private static void GameStateManager__ChangeState__Postfix(eGameStateName nextState)
        {
            switch (nextState)
            {
                case eGameStateName.NoLobby:
                case eGameStateName.Lobby:
                case eGameStateName.Generating:
                    CellSound.SetGlobalRTPCValue(GAME_PARAMETERS.SCOUT_SCREAM_DUCKING, 0);
                    CellSound.SetGlobalRTPCValue(GAME_PARAMETERS.GAME_MENU_MUFFLE, 0);
                    CellSound.SetGlobalRTPCValue(GAME_PARAMETERS.MINIMAP_MUFFLE, 0);
                    break;
            }
        }
    }
}
