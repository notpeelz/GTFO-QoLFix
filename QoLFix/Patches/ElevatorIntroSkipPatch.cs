using BepInEx.Configuration;
using HarmonyLib;

namespace QoLFix.Patches
{
    public class ElevatorIntroSkipPatch : IPatch
    {
        private static readonly string PatchName = nameof(ElevatorIntroSkipPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Skips the intro that plays when dropping into a level."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<ElevatorRide>(nameof(ElevatorRide.StartPreReleaseSequence), PatchType.Postfix);
        }

        private static void ElevatorRide__StartPreReleaseSequence__Postfix() => ElevatorRide.SkipPreReleaseSequence();
    }
}
