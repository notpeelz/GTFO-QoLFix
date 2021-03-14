using BepInEx.Configuration;
using SNetwork;
using Steamworks;

namespace QoLFix.Patches.Misc
{
    public class RecentlyPlayedWithPatch : Patch
    {
        private const string PatchName = nameof(RecentlyPlayedWithPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Updates the Steam recent players list."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public override void Execute()
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
