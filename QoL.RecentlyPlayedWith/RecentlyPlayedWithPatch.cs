using BepInEx.Configuration;
using MTFO.Core;
using SNetwork;
using Steamworks;

namespace QoL.RecentlyPlayedWith
{
    public class RecentlyPlayedWithPatch : MTFOPatch
    {
        private const string PatchName = nameof(RecentlyPlayedWithPatch);

        private static ConfigEntry<bool> ConfigEnabled = default!;

        public static RecentlyPlayedWithPatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Updates the Steam recent players list."));
        }

        protected override void Apply()
        {
            base.Apply();
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
