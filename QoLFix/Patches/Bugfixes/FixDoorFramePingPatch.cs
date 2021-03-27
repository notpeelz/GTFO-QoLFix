using System.Linq;
using BepInEx.Configuration;
using LevelGeneration;
using UnityEngine;

namespace QoLFix.Patches.Bugfixes
{
    public class FixDoorFramePingPatch : Patch
    {
        private const string PatchName = nameof(FixDoorFramePingPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Makes the door frames of the tech tileset pingable (useful for pinging closed doors)."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => ConfigEnabled.GetConfigEntry<bool>().Value;

        public override void Execute()
        {
            this.PatchMethod<LG_BuildGateJob>(nameof(LG_BuildGateJob.SetupDoor), PatchType.Postfix);
        }

        private static readonly string[] CollisionNames = new[] { "c_weakDoor_8x4_tech", "c_weakDoor_4x4_tech" };

        private static void LG_BuildGateJob__SetupDoor__Postfix(LG_BuildGateJob __instance)
        {
            var colliders = __instance.m_gate.gameObject.GetComponentsInChildren<MeshCollider>();

            var count = 0;
            foreach (var collider in colliders)
            {
                if (!CollisionNames.Contains(collider.gameObject.name)) continue;
                if (collider.gameObject.GetComponent<PlayerPingTarget>() != null) continue;
                var pingTarget = collider.gameObject.AddComponent<PlayerPingTarget>();
                // For some reason the propery setter is a noop; so we use the
                // field instead.
                pingTarget.m_pingTargetStyle = eNavMarkerStyle.PlayerPingDoor;
                count++;
            }

            if (count > 0)
            {
                Instance.LogDebug($"Patched gate {__instance.m_gate.name}");
            }

            if (count > 1)
            {
                Instance.LogWarning($"Added more than 1 {nameof(PlayerPingTarget)} to gate {__instance.m_gate.name}");
            }
        }
    }
}
