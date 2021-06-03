using AK;
using BepInEx.Configuration;
using HarmonyLib;
using MTFO.Core;

namespace QoL.ElevatorEarplugs
{
    public class ElevatorEarplugsPatch : MTFOPatch
    {
        private const string PatchName = nameof(ElevatorEarplugsPatch);

        private static ConfigEntry<bool> ConfigEnabled = default!;
        private static ConfigEntry<float> ConfigVolume = default!;

        public static ElevatorEarplugsPatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Adjusts the SFX volume during the elevator sequence."));
            ConfigVolume = this.Plugin.Config.Bind(new(PatchName, "Volume"), 0.05f,
                new ConfigDescription("The new volume value to use during the scene (1 = 100%, 0.5 = 50%, etc.)"));
        }

        private static bool Focused = true;

        [HarmonyPatch(typeof(CellSettingsManager))]
        [HarmonyPatch(nameof(CellSettingsManager.OnApplicationFocus))]
        [HarmonyPostfix]
        private static void CellSettingsManager__OnApplicationFocus__Postfix(bool focus)
        {
            Focused = focus;
            UpdateAudio();
        }

        [HarmonyPatch(typeof(GameStateManager))]
        [HarmonyPatch(nameof(GameStateManager.ChangeState))]
        [HarmonyPostfix]
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
                    CellSound.SetGlobalRTPCValue(GAME_PARAMETERS.VOLUME_SETTING_SFX, ConfigVolume.Value * 100f);
                    return true;
                default:
                    if (!Focused) return false;
                    CellSettingsManager.SettingsData.Audio.ApplyAllValues();
                    return false;
            }
        }
    }
}
