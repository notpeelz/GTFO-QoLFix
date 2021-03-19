using BepInEx.Configuration;

namespace QoLFix.Patches.Annoyances
{
    public class ElevatorIntroSkipPatch : Patch
    {
        private const string PatchName = nameof(ElevatorIntroSkipPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Skips the intro that plays when dropping into a level."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => ConfigEnabled.GetConfigEntry<bool>().Value;

        public override void Execute()
        {
            this.PatchMethod<ElevatorRide>(nameof(ElevatorRide.StartPreReleaseSequence), PatchType.Postfix);
        }

        private static void ElevatorRide__StartPreReleaseSequence__Postfix() => ElevatorRide.SkipPreReleaseSequence();
    }
}
