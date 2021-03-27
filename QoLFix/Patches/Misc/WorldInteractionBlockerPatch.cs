namespace QoLFix.Patches.Misc
{
    public class WorldInteractionBlockerPatch : Patch
    {
        public static Patch Instance { get; private set; }

        public override string Name { get; } = nameof(WorldInteractionBlockerPatch);

        public override void Initialize()
        {
            Instance = this;
        }

        public override void Execute()
        {
            this.PatchMethod<PlayerInteraction>($"get_{nameof(PlayerInteraction.HasWorldInteraction)}", PatchType.Prefix);

            LevelCleanupPatch.OnExitLevel += () =>
            {
                IgnoreWorldInteractions = 0;
            };
        }

        private static int ignoreWorldInteractions;
        public static int IgnoreWorldInteractions
        {
            get => ignoreWorldInteractions;
            set
            {
                if (value < 0) Instance.LogWarning($"{nameof(IgnoreWorldInteractions)} is negative");
                else if (value > 0 && ignoreWorldInteractions <= 0) Instance.LogDebug("IgnoreWorldInteractions = true");
                else if (ignoreWorldInteractions != 0) Instance.LogDebug("IgnoreWorldInteractions = false");
                ignoreWorldInteractions = value;
            }
        }

        private static bool PlayerInteraction__get_HasWorldInteraction__Prefix(ref bool __result)
        {
            if (IgnoreWorldInteractions <= 0) return HarmonyControlFlow.Execute;
            __result = false;
            return HarmonyControlFlow.DontExecute;
        }
    }
}
