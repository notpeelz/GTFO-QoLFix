using BepInEx.Configuration;
using Gear;
using HarmonyLib;
using LevelGeneration;
using Player;

namespace QoLFix.Patches.Tweaks
{
    /// <summary>
    /// This patch lets the player look up swapped out resources on the terminal.
    /// Known bugs:
    ///   - The ping doesn't show up unless the player is hosting
    /// </summary>
    public class TerminalPingableSwapsPatch : IPatch
    {
        private const string PatchName = nameof(TerminalPingableSwapsPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Relists swapped out items on terminals. This lets you list/ping/query items after moving them."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<ResourcePackPickup>(nameof(ResourcePackPickup.Setup), PatchType.Postfix);
        }

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
                return;

            LG_LevelInteractionManager.Current.m_terminalItems.Add(terminalItem.TerminalItemId, terminalItem);
            LG_LevelInteractionManager.Current.m_terminalItemsByKeyString.Add(terminalItem.TerminalItemKey, terminalItem);

            var resourceContainer = GTFOUtils.GetParentResourceContainer(__instance.transform.position);
            var spawnNode = resourceContainer.SpawnNode;
            var spawnZone = spawnNode.m_zone;
            terminalItem.SpawnNode = spawnNode;
            terminalItem.FloorItemLocation = spawnZone.NavInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore);
        }
    }
}
