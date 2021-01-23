using BepInEx.Configuration;
using HarmonyLib;
using Player;
using System.Collections.Generic;

namespace QoLFix.Patches
{
    public class FixWeaponSwapPatch : IPatch
    {
        private static readonly string PatchName = nameof(FixWeaponSwapPatch);
        private static readonly string SectionSwapModeDefault = $"{PatchName}_SwapModeDefault";
        private static readonly string SectionSwapModeLoud = $"{PatchName}_SwapModeLoud";
        private static readonly string SectionSwapModeStealth = $"{PatchName}_SwapModeStealth";
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");
        private static readonly Dictionary<string, SwapModeSection> ConfigSwapModes = new Dictionary<string, SwapModeSection>
        {
            { SectionSwapModeDefault, new SwapModeSection(SectionSwapModeDefault, DRAMA_State.Exploration, SwapMode.Melee) },
            { SectionSwapModeLoud, new SwapModeSection(SectionSwapModeLoud, DRAMA_State.Combat, SwapMode.Melee) },
            { SectionSwapModeStealth, new SwapModeSection(SectionSwapModeStealth, DRAMA_State.Sneaking, SwapMode.HackingTool) },
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

        public enum SwapMode
        {
            Melee,
            HackingTool,
            Primary,
            Secondary,
        }

        public string Name => PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public void Initialize()
        {
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Changes the weapon swap order dynamically based on the drama state of the game (stealth, loud, etc.)"));
            foreach (var kv in ConfigSwapModes)
            {
                kv.Value.Bind();
            }
        }

        public void Patch(Harmony harmony)
        {
            var methodInfo = typeof(PlayerBackpackManager).GetMethod(nameof(PlayerBackpackManager.WieldFirstLocalGear));
            harmony.Patch(methodInfo, prefix: new HarmonyMethod(AccessTools.Method(typeof(FixWeaponSwapPatch), nameof(PlayerBackpackManager__WieldFirstLocalGear))));
        }

        private static bool PlayerBackpackManager__WieldFirstLocalGear()
        {
            var playerAgent = PlayerManager.GetLocalPlayerAgent();
            if (playerAgent == null) return false;

            if (playerAgent.Locomotion.m_lastStateEnum == PlayerLocomotion.PLOC_State.ClimbLadder) return true;

            var wieldedSlot = playerAgent.Inventory.WieldedSlot;
            if (wieldedSlot == InventorySlot.ConsumableHeavy || wieldedSlot == InventorySlot.InLevelCarry) return true;

            var slot = GetInventorySlotByDrama();
            playerAgent.Sync.WantsToWieldSlot(slot);

            return false;
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

        private static InventorySlot GetPreferredInventorySlot(SwapMode swapMode)
        {
            switch (swapMode)
            {
                case SwapMode.Melee:
                    return InventorySlot.GearMelee;
                case SwapMode.Primary:
                    return InventorySlot.GearStandard;
                case SwapMode.Secondary:
                    return InventorySlot.GearSpecial;
                default:
                case SwapMode.HackingTool:
                    return InventorySlot.HackingTool;
            }
        }
    }
}
