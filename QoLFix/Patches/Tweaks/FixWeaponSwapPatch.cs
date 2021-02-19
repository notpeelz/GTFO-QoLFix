using BepInEx.Configuration;
using HarmonyLib;
using Player;
using System.Collections.Generic;

namespace QoLFix.Patches.Tweaks
{
    public class FixWeaponSwapPatch : IPatch
    {
        private static readonly string PatchName = nameof(FixWeaponSwapPatch);
        private static readonly string SectionSwapModeDefault = $"{PatchName}_SwapModeDefault";
        private static readonly string SectionSwapModeLoud = $"{PatchName}_SwapModeLoud";
        private static readonly string SectionSwapModeStealth = $"{PatchName}_SwapModeStealth";
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");
        private static readonly Dictionary<string, SwapModeSection> ConfigSwapModes = new()
        {
            { SectionSwapModeDefault, new SwapModeSection(SectionSwapModeDefault, DRAMA_State.Exploration, SwapMode.Melee) },
            { SectionSwapModeLoud, new SwapModeSection(SectionSwapModeLoud, DRAMA_State.Combat, SwapMode.Melee) },
            { SectionSwapModeStealth, new SwapModeSection(SectionSwapModeStealth, DRAMA_State.Sneaking, SwapMode.HackingTool) },
        };

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Changes the weapon swap order dynamically based on the drama state of the game (stealth, loud, etc.)"));
            foreach (var kv in ConfigSwapModes)
            {
                kv.Value.Bind();
            }
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<PlayerBackpackManager>(nameof(PlayerBackpackManager.WieldFirstLocalGear), PatchType.Prefix);
        }

        private static bool PlayerBackpackManager__WieldFirstLocalGear__Prefix()
        {
            var playerAgent = PlayerManager.GetLocalPlayerAgent();
            if (playerAgent == null) return true;

            if (playerAgent.Locomotion.m_lastStateEnum == PlayerLocomotion.PLOC_State.ClimbLadder) return true;

            var wieldedSlot = playerAgent.Inventory.WieldedSlot;
            switch (wieldedSlot)
            {
                case InventorySlot.None:
                case InventorySlot.ConsumableHeavy:
                case InventorySlot.InLevelCarry:
                    return true;
                default:
                    var slot = GetInventorySlotByDrama();
                    playerAgent.Sync.WantsToWieldSlot(slot);
                    return false;
            }
        }

        private static InventorySlot GetInventorySlotByDrama()
        {
            var swapModeLoud = QoLFixPlugin.Instance.Config.GetConfigEntry<SwapMode>(ConfigSwapModes[SectionSwapModeLoud].SwapMode).Value;
            var swapModeStealth = QoLFixPlugin.Instance.Config.GetConfigEntry<SwapMode>(ConfigSwapModes[SectionSwapModeStealth].SwapMode).Value;
            var swapModeDefault = QoLFixPlugin.Instance.Config.GetConfigEntry<SwapMode>(ConfigSwapModes[SectionSwapModeDefault].SwapMode).Value;

            switch (DramaManager.CurrentStateEnum)
            {
                case DRAMA_State.Alert:
                case DRAMA_State.Encounter:
                case DRAMA_State.Combat:
                    return GetPreferredInventorySlot(swapModeLoud);
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

        private class SwapModeSection
        {
            private readonly SwapMode defaultSwapMode;
            private readonly DRAMA_State state;

            public SwapModeSection(string section, DRAMA_State state, SwapMode defaultSwapMode)
            {
                this.state = state;
                this.SwapMode = new ConfigDefinition(section, "SwapMode");
                this.defaultSwapMode = defaultSwapMode;
            }

            public void Bind()
            {
                QoLFixPlugin.Instance.Config.Bind(this.SwapMode, this.defaultSwapMode, new ConfigDescription($"Controls the swap behavior when the game is in the {this.state} drama state."));
            }

            public ConfigDefinition SwapMode { get; }
        }

        private enum SwapMode
        {
            Melee,
            HackingTool,
            Primary,
            Secondary,
        }
    }
}
