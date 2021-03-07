using BepInEx.Configuration;
using HarmonyLib;
using Player;

namespace QoLFix.Patches.Tweaks
{
    public class HideCrosshairPatch : IPatch
    {
        private const string PatchName = nameof(HideCrosshairPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");
        private static readonly ConfigDefinition ConfigShowForMelee = new(PatchName, "ShowForMelee");
        private static readonly ConfigDefinition ConfigShowForConsumables = new(PatchName, "ShowForConsumables");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, false, new ConfigDescription("Hides the in-game crosshair."));
            QoLFixPlugin.Instance.Config.Bind(ConfigShowForMelee, true, new ConfigDescription("Prevents hiding the crosshair when a melee weapon is wielded."));
            QoLFixPlugin.Instance.Config.Bind(ConfigShowForConsumables, true, new ConfigDescription("Prevents hiding the crosshair when a consumable is wielded."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<CrosshairGuiLayer>(nameof(CrosshairGuiLayer.ShowSpreadCircle), PatchType.Prefix);
        }

        private static bool CrosshairGuiLayer__ShowSpreadCircle__Prefix(CrosshairGuiLayer __instance)
        {
            var playerAgent = PlayerManager.GetLocalPlayerAgent();
            if (playerAgent == null) return true;

            switch (playerAgent.Inventory.WieldedSlot)
            {
                case InventorySlot.GearMelee:
                    return QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigShowForMelee).Value
                        ? HarmonyControlFlow.Execute
                        : HarmonyControlFlow.DontExecute;
                case InventorySlot.Consumable:
                    return QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigShowForConsumables).Value
                        ? HarmonyControlFlow.Execute
                        : HarmonyControlFlow.DontExecute;
                default:
                    __instance.HideAllCrosshairs();
                    return HarmonyControlFlow.DontExecute;
            }
        }
    }
}
