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

        public void Patch(Harmony harmony)
        {
            var methodInfo = typeof(ElevatorRide).GetMethod(nameof(ElevatorRide.StartPreReleaseSequence));
            harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(ElevatorIntroSkipPatch), nameof(ElevatorRide__StartPreReleaseSequence))));
        }

        private static void ElevatorRide__StartPreReleaseSequence() => ElevatorRide.SkipPreReleaseSequence();
    }
}
