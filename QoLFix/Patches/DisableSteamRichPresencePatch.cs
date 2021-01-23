using BepInEx.Configuration;
using HarmonyLib;
using SNetwork;

namespace QoLFix.Patches
{
    public class DisableSteamRichPresencePatch : IPatch
    {
        private static readonly string PatchName = nameof(DisableSteamRichPresencePatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public void Initialize()
        {
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, false, new ConfigDescription("Disables Steam Rich Presence updates; also prevents Steam friends from seeing your lobby from the rundown screen."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public void Patch(Harmony harmony)
        {
            var methodInfo = typeof(SNet_Core_STEAM).GetMethod(nameof(SNet_Core_STEAM.SetFriendsData), new[] { typeof(string), typeof(string) });
            harmony.Patch(methodInfo, prefix: new HarmonyMethod(AccessTools.Method(typeof(DisableSteamRichPresencePatch), nameof(SNet_Core_STEAM__SetFriendsData))));
        }

        private static bool SNet_Core_STEAM__SetFriendsData() => false;
    }
}
