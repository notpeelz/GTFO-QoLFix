using BepInEx.Configuration;
using Gear;
using HarmonyLib;
using Player;
using UnityEngine;

namespace QoLFix.Patches.Bugfixes
{
    /// <summary>
    /// Resources inside resource containers aren't pingable because the
    /// BoxCollider of the container encompasses the colliders inside of it.
    /// This patch overrides the ping selection target if the camera is aimed
    /// at a resource inside the container.
    /// </summary>
    public class FixLockerPingPatch : IPatch
    {
        private const string PatchName = nameof(FixLockerPingPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Fixes the bug where resources inside of lockers/boxes aren't individually pingable."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<PlayerAgent>(nameof(PlayerAgent.UpdateGlobalInput), PatchType.Prefix);
            this.PatchMethod<ResourcePackPickup>(nameof(ResourcePackPickup.Setup), PatchType.Postfix);
        }

        private static void ResourcePackPickup__Setup__Postfix(ResourcePackPickup __instance)
        {
            var pingTarget = __instance.GetComponentInChildren<PlayerPingTarget>();
            if (pingTarget == null) return;
            switch (__instance.m_packType)
            {
                // Fix disinfection pack pings showing up as ammo packs
                case eResourceContainerSpawnType.Disinfection:
                    pingTarget.m_pingTargetStyle = eNavMarkerStyle.PlayerPingDisinfection;
                    break;
                // Tool refills show up as ammo but there's no appropriate
                // icon for them...
                //case eResourceContainerSpawnType.AmmoTool:
                //    pingTarget.m_pingTargetStyle = eNavMarkerStyle.PlayerPingLoot;
                //    break;
            }
        }

        private static bool PlayerAgent__UpdateGlobalInput__Prefix(PlayerAgent __instance)
        {
            if (Input.GetKey(__instance.m_ffKey1) && Input.GetKeyDown(__instance.m_ffKey2)) return true;
            if (!InputMapper.GetButtonDown.Invoke(InputAction.NavMarkerPing, __instance.InputFilter)) return true;

            if (GuiManager.PlayerMarkerIsVisibleAndInFocus(__instance))
            {
                GuiManager.AttemptSetPlayerPingStatus(__instance, false);
                return HarmonyControlFlow.DontExecute;
            }

            if (!GTFOUtils.GetComponentInSight<iPlayerPingTarget>(__instance, out var pingTarget, out var pingPos, 40f, LayerManager.MASK_PING_TARGET))
            {
                GuiManager.CrosshairLayer.PopAngryPingIndicator();
                return HarmonyControlFlow.DontExecute;
            }

            __instance.m_pingTarget = pingTarget;
            __instance.m_pingPos = pingPos;
            if (__instance.m_pingTarget != null && (__instance.m_pingTarget != __instance.m_lastPingedTarget || Clock.Time > __instance.m_pingAgainTimer))
            {
                __instance.TriggerMarkerPing(__instance.m_pingTarget, __instance.m_pingPos);
                __instance.m_pingAgainTimer = Clock.Time + 2f;
                __instance.m_lastPingedTarget = __instance.m_pingTarget;
            }

            return HarmonyControlFlow.DontExecute;
        }
    }
}
