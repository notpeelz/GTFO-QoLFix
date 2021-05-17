using BepInEx.Configuration;
using HarmonyLib;
using MTFO.Core;
using Player;

namespace QoL.BetterWeaponSwap
{
    public class BetterWeaponSwapPatch : MTFOPatch
    {
        private const string PatchName = nameof(BetterWeaponSwapPatch);

        private static ConfigEntry<bool> ConfigEnabled = default!;
        private static ConfigEntry<SwapMode> ConfigSwapModeDefault = default!;
        private static ConfigEntry<SwapMode> ConfigSwapModeCombat = default!;
        private static ConfigEntry<SwapMode> ConfigSwapModeStealth = default!;

        public static BetterWeaponSwapPatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Changes the weapon fallback priority dynamically based on the drama state of the game (stealth, combat, etc.)"));
            ConfigSwapModeDefault = this.Plugin.Config.Bind(new(PatchName, "SwapModeDefault"), SwapMode.Melee,
                new ConfigDescription("Controls the default behavior."));
            ConfigSwapModeCombat = this.Plugin.Config.Bind(new(PatchName, "SwapModeCombat"), SwapMode.Melee,
                new ConfigDescription("Controls the behavior during combat."));
            ConfigSwapModeStealth = this.Plugin.Config.Bind(new(PatchName, "SwapModeStealth"), SwapMode.HackingTool,
                new ConfigDescription("Controls the behavior during stealth."));
        }

        [HarmonyPatch(typeof(PlayerBackpackManager))]
        [HarmonyPatch(nameof(PlayerBackpackManager.WieldFirstLocalGear))]
        [HarmonyPrefix]
        private static bool PlayerBackpackManager__WieldFirstLocalGear__Prefix()
        {
            var playerAgent = PlayerManager.GetLocalPlayerAgent();
            if (playerAgent == null) return HarmonyControlFlow.Execute;

            var lastWanted = playerAgent.Sync.LastWantedSlot;
            var bp = PlayerBackpackManager.LocalBackpack;
            if (lastWanted != InventorySlot.None
                && bp.TryGetBackpackItem(lastWanted, out var lastWantedItem)
                && lastWantedItem.Status == eInventoryItemStatus.InBackpack)
            {
                Instance!.LogDebug($"Switching to last wanted slot: {lastWanted}");
                playerAgent.Sync.WantsToWieldSlot(lastWanted);
                return HarmonyControlFlow.DontExecute;
            }

            var slot = GetInventorySlotByDrama();
            Instance!.LogDebug($"Switching to slot: {slot}");
            playerAgent.Sync.WantsToWieldSlot(slot);
            return HarmonyControlFlow.DontExecute;
        }

        private static InventorySlot GetInventorySlotByDrama()
        {
            switch (DramaManager.CurrentStateEnum)
            {
                case DRAMA_State.Alert:
                case DRAMA_State.Encounter:
                case DRAMA_State.Combat:
                    return GetPreferredInventorySlot(ConfigSwapModeCombat.Value);
                case DRAMA_State.Sneaking:
                    return GetPreferredInventorySlot(ConfigSwapModeStealth.Value);
                case DRAMA_State.ElevatorIdle:
                case DRAMA_State.ElevatorGoingDown:
                case DRAMA_State.Exploration:
                default:
                    return GetPreferredInventorySlot(ConfigSwapModeDefault.Value);
            }
        }

        private static InventorySlot GetPreferredInventorySlot(SwapMode swapMode) => swapMode switch
        {
            SwapMode.Melee => InventorySlot.GearMelee,
            SwapMode.Primary => InventorySlot.GearStandard,
            SwapMode.Secondary => InventorySlot.GearSpecial,
            _ => InventorySlot.HackingTool,
        };

        private enum SwapMode
        {
            Melee,
            HackingTool,
            Primary,
            Secondary,
        }
    }
}
