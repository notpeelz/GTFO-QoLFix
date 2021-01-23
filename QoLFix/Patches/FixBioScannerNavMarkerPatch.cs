using BepInEx.Configuration;
using Enemies;
using HarmonyLib;

namespace QoLFix.Patches
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
        private static readonly string PatchName = nameof(FixBioScannerNavMarkerPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public void Initialize()
        {
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Fixes the bug where bio scanner tags would remain on the screen after multiple scans."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public void Patch(Harmony harmony)
        {
            var methodInfo = typeof(EnemyAgent).GetMethod(nameof(EnemyAgent.SyncPlaceNavMarkerTag));
            harmony.Patch(methodInfo, prefix: new HarmonyMethod(AccessTools.Method(typeof(FixBioScannerNavMarkerPatch), nameof(EnemyAgent__SyncPlaceNavMarkerTag))));
        }

        private static void EnemyAgent__SyncPlaceNavMarkerTag(EnemyAgent __instance) => __instance.RemoveNavMarker();
    }
}
