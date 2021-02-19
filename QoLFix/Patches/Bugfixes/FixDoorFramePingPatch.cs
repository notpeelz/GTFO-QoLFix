using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using LevelGeneration;
using UnityEngine;

namespace QoLFix.Patches.Bugfixes
{
    public class FixDoorFramePingPatch : IPatch
    {
        private static readonly string PatchName = nameof(FixDoorFramePingPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Makes the frame of the doors from the Tech tileset pingable (useful for pinging closed doors)."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<LG_BuildGateJob>(nameof(LG_BuildGateJob.SetupDoor), PatchType.Postfix);
        }

        private static readonly string[] CollisionNames = new[] { "c_weakDoor_8x4_tech", "c_weakDoor_4x4_tech" };

        private static void LG_BuildGateJob__SetupDoor__Postfix(LG_BuildGateJob __instance, ref iLG_Door_Core __result)
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
