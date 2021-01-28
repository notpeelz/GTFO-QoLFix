using BepInEx.Configuration;
using HarmonyLib;
using Player;

namespace QoLFix.Patches
{
    public class HideCrosshairPatch : IPatch
    {
        private static readonly string PatchName = nameof(HideCrosshairPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");
        private static readonly ConfigDefinition ConfigShowForMelee = new ConfigDefinition(PatchName, "ShowForMelee");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, false, new ConfigDescription("Hides the in-game crosshair."));
            QoLFixPlugin.Instance.Config.Bind(ConfigShowForMelee, true, new ConfigDescription("Prevents hiding the crosshair when a melee weapon is out."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public void Patch(Harmony harmony)
        {
            var methodInfo = typeof(CrosshairGuiLayer).GetMethod(nameof(CrosshairGuiLayer.ShowSpreadCircle));
            harmony.Patch(methodInfo, prefix: new HarmonyMethod(AccessTools.Method(typeof(HideCrosshairPatch), nameof(CrosshairGuiLayer__ShowSpreadCircle))));
        }

        private static bool CrosshairGuiLayer__ShowSpreadCircle(CrosshairGuiLayer __instance)
        {
            var playerAgent = PlayerManager.GetLocalPlayerAgent();
            if (playerAgent == null) return true;

            if (playerAgent.Inventory.WieldedSlot == InventorySlot.GearMelee) return true;

            __instance.HideAllCrosshairs();
            return false;
        }
    }
}
