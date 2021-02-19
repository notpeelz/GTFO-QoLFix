using BepInEx.Configuration;
using HarmonyLib;
using Player;

namespace QoLFix.Patches.Bugfixes
{
    public class FixToolRefillBioScannerPatch : IPatch
    {
        private static readonly string PatchName = nameof(FixToolRefillBioScannerPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Prevents you from accidentally giving a tool refill to a bio tracker."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
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
                    return false;
                }
            }

            return true;
        }
    }
}
