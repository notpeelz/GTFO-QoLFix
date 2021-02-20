using BepInEx.Configuration;
using HarmonyLib;
using SNetwork;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace QoLFix.Patches.Tweaks
{
    /// <summary>
    /// This patch displays a network latency information on your HUD.
    /// It displays your latency to the host on the bottom right corner.
    /// Additionaly, the latency to every player is displayed on the map menu.
    ///
    /// Known bugs:
    /// The latency doesn't update correctly throughout the game for some
    /// unknown reason. This might be because the pPingLocation struct doesn't
    /// get replicated after joining the game.
    /// Another thing that could be responsible for this bug is that
    /// pPingLocation is a network replication wrapper for the
    /// "SteamNetworkPingLocation_t" SteamAPI struct.
    /// According to the Steam API documentation, this struct is not supposed
    /// to be serialized or sent over network:
    ///     NOTE: This object should only be used in the same process!
    ///     Do not serialize it, send it over the wire, or persist it in a file
    ///     or database!
    ///     If you need to do that, convert it to a string representation using
    ///     the methods in ISteamNetworkingUtils
    /// See: https://partner.steamgames.com/doc/api/steamnetworkingtypes#SteamNetworkPingLocation_t
    /// </summary>
    public partial class LatencyHUDPatch : IPatch
    {
        private static readonly string PatchName = nameof(LatencyHUDPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Displays network latency on your HUD.\nNOTE: unfortunately, due to a bug with the way GTFO estimates network latency, the ping is only updated once upon joining a game."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            ClassInjector.RegisterTypeInIl2Cpp<LatencyHUDElement>();
            this.PatchMethod<WatermarkGuiLayer>(nameof(WatermarkGuiLayer.Setup), new[] { typeof(Transform), typeof(string) }, PatchType.Postfix);
            //this.PatchMethod<SNet_Core_STEAM>(nameof(SNet_Core_STEAM.UpdateConnectionStatus), PatchType.Postfix);
        }

        private static void SNet_Core_STEAM__UpdateConnectionStatus__Postfix(SNet_Player player)
        {
            Instance.LogDebug("UpdateConnectionStatus: " + player?.NickName + " " + player?.Ping);
        }

        private static void WatermarkGuiLayer__Setup__Postfix(WatermarkGuiLayer __instance) =>
            LatencyHUDElement.Patch(__instance.m_watermark);
    }
}
