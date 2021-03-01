using BepInEx.Configuration;
using HarmonyLib;
using SNetwork;

namespace QoLFix.Patches.Misc
{
    public class DisableSteamRichPresencePatch : IPatch
    {
        private const string PatchName = nameof(DisableSteamRichPresencePatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, false, new ConfigDescription("Disables Steam Rich Presence updates; also prevents Steam friends from seeing your lobby from the rundown screen."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<SNet_Core_STEAM>(nameof(SNet_Core_STEAM.SetFriendsData), new[] { typeof(string), typeof(string) }, PatchType.Prefix);
        }

        private static bool SNet_Core_STEAM__SetFriendsData__Prefix() => false;
    }
}
