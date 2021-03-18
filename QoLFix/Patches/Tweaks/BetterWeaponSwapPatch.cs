using BepInEx.Configuration;
using Player;

namespace QoLFix.Patches.Tweaks
{
    public class BetterWeaponSwapPatch : Patch
    {
        private const string PatchName = nameof(BetterWeaponSwapPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");
        private static readonly ConfigDefinition ConfigSwapModeDefault = new(PatchName, "SwapModeDefault");
        private static readonly ConfigDefinition ConfigSwapModeCombat = new(PatchName, "SwapModeCombat");
        private static readonly ConfigDefinition ConfigSwapModeStealth = new(PatchName, "SwapModeStealth");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Changes the weapon fallback priority dynamically based on the drama state of the game (stealth, combat, etc.)"));
            QoLFixPlugin.Instance.Config.Bind(ConfigSwapModeDefault, SwapMode.Melee, new ConfigDescription("Controls the default behavior."));
            QoLFixPlugin.Instance.Config.Bind(ConfigSwapModeCombat, SwapMode.Melee, new ConfigDescription("Controls the behavior during combat."));
            QoLFixPlugin.Instance.Config.Bind(ConfigSwapModeStealth, SwapMode.HackingTool, new ConfigDescription("Controls the behavior during stealth."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public override void Execute()
        {
            this.PatchMethod<PlayerBackpackManager>(nameof(PlayerBackpackManager.WieldFirstLocalGear), PatchType.Prefix);
        }

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
                Instance.LogDebug($"Switching to last wanted slot: {lastWanted}");
                playerAgent.Sync.WantsToWieldSlot(lastWanted);
                return HarmonyControlFlow.DontExecute;
            }

            var slot = GetInventorySlotByDrama();
            Instance.LogDebug($"Switching to slot: {slot}");
            playerAgent.Sync.WantsToWieldSlot(slot);
            return HarmonyControlFlow.DontExecute;
        }

        private static InventorySlot GetInventorySlotByDrama()
        {
            var swapModeCombat = QoLFixPlugin.Instance.Config.GetConfigEntry<SwapMode>(ConfigSwapModeCombat).Value;
            var swapModeStealth = QoLFixPlugin.Instance.Config.GetConfigEntry<SwapMode>(ConfigSwapModeStealth).Value;
            var swapModeDefault = QoLFixPlugin.Instance.Config.GetConfigEntry<SwapMode>(ConfigSwapModeDefault).Value;

            switch (DramaManager.CurrentStateEnum)
            {
                case DRAMA_State.Alert:
                case DRAMA_State.Encounter:
                case DRAMA_State.Combat:
                    return GetPreferredInventorySlot(swapModeCombat);
                case DRAMA_State.Sneaking:
                    return GetPreferredInventorySlot(swapModeStealth);
                case DRAMA_State.ElevatorIdle:
                case DRAMA_State.ElevatorGoingDown:
                case DRAMA_State.Exploration:
                default:
                    return GetPreferredInventorySlot(swapModeDefault);
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
