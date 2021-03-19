using BepInEx.Configuration;
using Player;

namespace QoLFix.Patches.Bugfixes
{
    public class FixToolRefillBioScannerPatch : Patch
    {
        private const string PatchName = nameof(FixToolRefillBioScannerPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Prevents you from accidentally giving a tool refill to a bio tracker."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => ConfigEnabled.GetConfigEntry<bool>().Value;

        public override void Execute()
        {
            this.PatchMethod<PlayerAgent>(nameof(PlayerAgent.NeedToolAmmo), PatchType.Prefix);
        }

        private static bool PlayerAgent__NeedToolAmmo__Prefix(PlayerAgent __instance, ref bool __result)
        {
            if (!PlayerBackpackManager.TryGetBackpack(__instance.Owner, out var bp)) return true;
            if (bp.TryGetBackpackItem(InventorySlot.GearClass, out var item))
            {
                if (item.Name == "Enemy Scanner")
                {
                    __result = false;
                    return HarmonyControlFlow.DontExecute;
                }
            }

            return HarmonyControlFlow.Execute;
        }
    }
}
