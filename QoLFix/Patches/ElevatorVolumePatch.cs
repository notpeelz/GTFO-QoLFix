using AK;
using BepInEx.Configuration;
using HarmonyLib;

namespace QoLFix.Patches
{
    public class ElevatorVolumePatch : IPatch
    {
        private static readonly string PatchName = nameof(ElevatorVolumePatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");
        private static readonly ConfigDefinition ConfigVolume = new ConfigDefinition(PatchName, "Volume");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Adjusts the SFX volume during the elevator scene."));
            QoLFixPlugin.Instance.Config.Bind(ConfigVolume, 0.05f, new ConfigDescription("The new volume value to use during the scene (1 = 100%, 0.5 = 50%, etc.)"));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<GameStateManager>(nameof(GameStateManager.ChangeState), PatchType.Postfix);
            this.PatchMethod<CellSettingsManager>(nameof(CellSettingsManager.OnApplicationFocus), PatchType.Postfix);
        }

        private static bool Focused = true;

        private static void CellSettingsManager__OnApplicationFocus__Postfix(bool focus)
        {
            Focused = focus;
            UpdateAudio(GameStateManager.CurrentStateName);
        }

        private static void GameStateManager__ChangeState__Postfix(eGameStateName nextState) => UpdateAudio(nextState);

        private static void UpdateAudio(eGameStateName state)
        {
            if (!Focused) return;

            switch (state)
            {
                case eGameStateName.Generating:
                case eGameStateName.ReadyToStopElevatorRide:
                case eGameStateName.StopElevatorRide:
                    CellSound.SetGlobalRTPCValue(GAME_PARAMETERS.VOLUME_SETTING_SFX, QoLFixPlugin.Instance.Config.GetConfigEntry<float>(ConfigVolume).Value * 100f);
                    break;
                case eGameStateName.InLevel:
                    CellSettingsManager.SettingsData.Audio.ApplyAllValues();
                    break;
            }
        }
    }
}
