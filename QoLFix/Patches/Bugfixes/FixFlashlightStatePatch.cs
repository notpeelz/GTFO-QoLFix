using BepInEx.Configuration;
using HarmonyLib;
using Player;
using SNetwork;

namespace QoLFix.Patches.Bugfixes
{
    public class FixFlashlightStatePatch : IPatch
    {
        private const string PatchName = nameof(FixFlashlightStatePatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

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
        }

        private static bool? FlashlightEnabled;

        private static void PlayerBackpackManager__RemoveItem__Prefix(SNet_Player fromPlayer)
        {
            FlashlightEnabled = null;
            if (!fromPlayer.IsLocal) return;
            if (!fromPlayer.HasPlayerAgent) return;

            var playerAgent = fromPlayer.PlayerAgent.Cast<PlayerAgent>();
            FlashlightEnabled = playerAgent.Inventory.FlashlightEnabled;
        }

        private static void PlayerBackpackManager__RemoveItem__Postfix(SNet_Player fromPlayer)
        {
            if (!fromPlayer.IsLocal) return;
            if (!fromPlayer.HasPlayerAgent) return;
            if (FlashlightEnabled == null) return;

            var playerAgent = fromPlayer.PlayerAgent.Cast<PlayerAgent>();
            playerAgent.Inventory.ReceiveSetFlashlightStatus((bool)FlashlightEnabled, true);
        }
    }
}
