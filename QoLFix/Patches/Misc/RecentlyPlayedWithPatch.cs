using BepInEx.Configuration;
using HarmonyLib;
using SNetwork;
using Steamworks;

namespace QoLFix.Patches.Misc
{
    public class RecentlyPlayedWithPatch : IPatch
    {
        private const string PatchName = nameof(RecentlyPlayedWithPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Updates the Steam recent players list."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<SNet_Lobby_STEAM>(
                methodName: nameof(SNet_Lobby_STEAM.PlayerJoined),
                parameters: new[] { typeof(SNet_Player), typeof(CSteamID) },
                patchType: PatchType.Postfix,
                postfixMethodName: nameof(UpdatePlayedWith));
            this.PatchMethod<SNet_Lobby_STEAM>(
                methodName: nameof(SNet_Lobby_STEAM.PlayerLeft),
                parameters: new[] { typeof(SNet_Player), typeof(CSteamID) },
                patchType: PatchType.Postfix,
                postfixMethodName: nameof(UpdatePlayedWith));
        }

        private static void UpdatePlayedWith(CSteamID steamID)
        {
            if (steamID.m_SteamID == SNet.LocalPlayer.Lookup) return;
            SteamFriends.SetPlayedWith(steamID);
        }
    }
}
