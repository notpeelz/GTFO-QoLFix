using BepInEx.Configuration;
using HarmonyLib;
using MTFO.Core;

namespace QoL.ElevatorIntroSkip
{
    public class ElevatorIntroSkipPatch : MTFOPatch
    {
        private const string PatchName = nameof(ElevatorIntroSkipPatch);

        private static ConfigEntry<bool> ConfigEnabled = default!;

        public static ElevatorIntroSkipPatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Skips the intro that plays when dropping into a level."));
        }

        [HarmonyPatch(typeof(ElevatorRide))]
        [HarmonyPatch(nameof(ElevatorRide.StartPreReleaseSequence))]
        [HarmonyPostfix]
        private static void ElevatorRide__StartPreReleaseSequence__Postfix() => ElevatorRide.SkipPreReleaseSequence();
    }
}
