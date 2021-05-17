using BepInEx.Configuration;
using Gear;
using HarmonyLib;
using LevelGeneration;
using MTFO.Core;
using Player;
using QoL.Common;

namespace QoL.TerminalPingableSwaps
{
    // FIXME: the ping icon doesn't show up unless the player is hosting
    public class TerminalPingableSwapsPatch : MTFOPatch
    {
        private const string PatchName = nameof(TerminalPingableSwapsPatch);

        public static TerminalPingableSwapsPatch? Instance { get; private set; }

        private static ConfigEntry<bool> ConfigEnabled = default!;

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Relists swapped out items on terminals. This lets you list/ping/query items after moving them."));
        }

        [HarmonyPatch(typeof(ResourcePackPickup))]
        [HarmonyPatch(nameof(ResourcePackPickup.Setup))]
        [HarmonyPostfix]
        private static void ResourcePackPickup__Setup__Postfix(ResourcePackPickup __instance)
        {
            __instance.m_sync.add_OnSyncStateChange(
                (Il2CppSystem.Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>)(
                    (status, _, _, _) => ResourcePackPickup__OnSyncStateChange(__instance, status)
                )
            );
        }

        private static void ResourcePackPickup__OnSyncStateChange(ResourcePackPickup __instance, ePickupItemStatus status)
        {
            if (status != ePickupItemStatus.PlacedInLevel) return;

            var terminalItem = __instance.m_terminalItem;
            if (string.IsNullOrEmpty(terminalItem?.TerminalItemKey)) return;

            if (LG_LevelInteractionManager.Current.m_terminalItemsByKeyString.ContainsKey(terminalItem.TerminalItemKey))
            {
                return;
            }

            LG_LevelInteractionManager.Current.m_terminalItems.Add(terminalItem.TerminalItemId, terminalItem);
            LG_LevelInteractionManager.Current.m_terminalItemsByKeyString.Add(terminalItem.TerminalItemKey, terminalItem);

            var resourceContainer = GTFOUtils.GetParentResourceContainer(__instance.transform.position);
            if (resourceContainer == null)
            {
                Instance!.LogDebug("ResourcePackPickup synced outside of a resource container?");
                return;
            }

            var spawnNode = resourceContainer.SpawnNode;
            var spawnZone = spawnNode.m_zone;
            terminalItem.SpawnNode = spawnNode;
            terminalItem.FloorItemLocation = spawnZone.NavInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore);
        }
    }
}
