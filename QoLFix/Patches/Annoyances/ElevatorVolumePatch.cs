using AK;
using BepInEx.Configuration;

namespace QoLFix.Patches.Annoyances
{
    public class ElevatorVolumePatch : Patch
    {
        private const string PatchName = nameof(ElevatorVolumePatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");
        private static readonly ConfigDefinition ConfigVolume = new(PatchName, "Volume");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Adjusts the SFX volume during the elevator scene."));
            QoLFixPlugin.Instance.Config.Bind(ConfigVolume, 0.05f, new ConfigDescription("The new volume value to use during the scene (1 = 100%, 0.5 = 50%, etc.)"));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => ConfigEnabled.GetConfigEntry<bool>().Value;

        public override void Execute()
        {
            this.PatchMethod<GameStateManager>(nameof(GameStateManager.ChangeState), PatchType.Postfix);
            this.PatchMethod<CellSettingsManager>(nameof(CellSettingsManager.OnApplicationFocus), PatchType.Postfix);
        }

        private static bool Focused = true;

        private static void CellSettingsManager__OnApplicationFocus__Postfix(bool focus)
        {
            Focused = focus;
            UpdateAudio();
        }

        private static void GameStateManager__ChangeState__Postfix(eGameStateName nextState) => UpdateAudio(nextState);

        private static bool UpdateAudio(eGameStateName? state = null)
        {
            if (state == null) state = GameStateManager.CurrentStateName;

            switch (state)
            {
                case eGameStateName.Generating:
                case eGameStateName.ReadyToStopElevatorRide:
                case eGameStateName.StopElevatorRide:
                case eGameStateName.ReadyToStartLevel:
                    if (!Focused) return true;
                    CellSound.SetGlobalRTPCValue(GAME_PARAMETERS.VOLUME_SETTING_SFX, ConfigVolume.GetConfigEntry<float>().Value * 100f);
                    return true;
                default:
                    if (!Focused) return false;
                    CellSettingsManager.SettingsData.Audio.ApplyAllValues();
                    return false;
            }
        }
    }
}
