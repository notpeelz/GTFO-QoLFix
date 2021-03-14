using BepInEx.Configuration;
using Enemies;

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
    public class FixBioScannerNavMarkerPatch : Patch
    {
        private const string PatchName = nameof(FixBioScannerNavMarkerPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Fixes the bug where bio scanner tags would remain on the screen after multiple scans."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public override void Execute()
        {
            this.PatchMethod<EnemyAgent>(nameof(EnemyAgent.SyncPlaceNavMarkerTag), PatchType.Prefix);
        }

        private static void EnemyAgent__SyncPlaceNavMarkerTag__Prefix(EnemyAgent __instance) =>
            __instance.RemoveNavMarker();
    }
}
