using AK;
using BepInEx.Configuration;
using QoLFix.UI;
using UnhollowerRuntimeLib;

namespace QoLFix.Patches.Bugfixes
{
    public partial class FixSoundMufflePatch : Patch
    {
        private const string PatchName = nameof(FixSoundMufflePatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Fixes several bugs related to sound distortion."));
            ClassInjector.RegisterTypeInIl2Cpp<AudioResetTimer>();
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        private static AudioResetTimer ResetTimer;

        public override void Execute()
        {
            this.PatchMethod<GameStateManager>(nameof(GameStateManager.ChangeState), PatchType.Postfix);

            UIManager.Initialized += () =>
            {
                GOFactory.CreateObject("ResetTimer", UIManager.CanvasRoot.transform, out ResetTimer);
            };

            this.PatchMethod<EnemyVoice>(nameof(EnemyVoice.PlayVoiceEvent), PatchType.Postfix);
        }

        private static void EnemyVoice__PlayVoiceEvent__Postfix(uint ID)
        {
            if (ID != EVENTS.SCOUT_DETECT_SCREAM) return;
            ResetTimer.ScheduleReset();
        }

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
