using BepInEx.Configuration;
using Enemies;
using HarmonyLib;

namespace QoLFix.Patches.Bugfixes
{
    /// <summary>
    /// EnemyAgent.m_tagMarker gets reassigned in
    /// EnemyAgent.SyncPlaceNavMarkerTag() whenever a new scan happens.
    /// However, the previous NavMarker instance doesn't get destroyed,
    /// which leads to the EnemyAgent not being able dispose of the marker
    /// on death.
    /// This patch forces the EnemyAgent to dispose of the previous marker.
    /// </summary>
    public class FixBioScannerNavMarkerPatch : IPatch
    {
        private const string PatchName = nameof(FixBioScannerNavMarkerPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Fixes the bug where bio scanner tags would remain on the screen after multiple scans."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<EnemyAgent>(nameof(EnemyAgent.SyncPlaceNavMarkerTag), PatchType.Prefix);
        }

        private static void EnemyAgent__SyncPlaceNavMarkerTag__Prefix(EnemyAgent __instance) =>
            __instance.RemoveNavMarker();
    }
}
