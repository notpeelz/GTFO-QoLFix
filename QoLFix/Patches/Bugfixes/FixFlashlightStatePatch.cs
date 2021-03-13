using BepInEx.Configuration;
using HarmonyLib;
using Player;
using SNetwork;

namespace QoLFix.Patches.Bugfixes
{
    public class FixFlashlightStatePatch : IPatch
    {
        private const string PatchName = nameof(FixFlashlightStatePatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Fixes the bug where your flashlight would turn off after dropping an item."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<PlayerBackpackManager>(
                methodName: nameof(PlayerBackpackManager.RemoveItem),
                parameters: new[]
                {
                    typeof(SNet_Player),
                    typeof(pItemData)
                },
                patchType: PatchType.Both);
            this.PatchMethod<PlayerBackpack>(nameof(PlayerBackpack.SetDeployed), PatchType.Both);
        }

        private static bool? FlashlightEnabled;

        private static void PlayerBackpackManager__RemoveItem__Prefix(SNet_Player fromPlayer) =>
            UpdateFlashlight__Prefix(fromPlayer);

        private static void PlayerBackpackManager__RemoveItem__Postfix(SNet_Player fromPlayer) =>
            UpdateFlashlight__Postfix(fromPlayer);

        private static void PlayerBackpack__SetDeployed__Prefix(PlayerBackpack __instance) =>
            UpdateFlashlight__Prefix(__instance.Owner);

        private static void PlayerBackpack__SetDeployed__Postfix(PlayerBackpack __instance) =>
            UpdateFlashlight__Postfix(__instance.Owner);

        private static void UpdateFlashlight__Prefix(SNet_Player player)
        {
            FlashlightEnabled = null;
            if (!player.IsLocal) return;
            if (!player.HasPlayerAgent) return;

            var playerAgent = player.PlayerAgent.Cast<PlayerAgent>();
            FlashlightEnabled = playerAgent.Inventory.FlashlightEnabled;
        }

        private static void UpdateFlashlight__Postfix(SNet_Player player)
        {
            if (!player.IsLocal) return;
            if (!player.HasPlayerAgent) return;
            if (FlashlightEnabled == null) return;

            var playerAgent = player.PlayerAgent.Cast<PlayerAgent>();
            playerAgent.Inventory.ReceiveSetFlashlightStatus((bool)FlashlightEnabled, true);
        }
    }
}
