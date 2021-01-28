using System.Linq;
using BepInEx.Configuration;
using Gear;
using HarmonyLib;
using LevelGeneration;
using Player;
using UnityEngine;

namespace QoLFix.Patches
{
    /// <summary>
    /// Resources inside resource containers aren't pingable because the
    /// BoxCollider of the container encompasses the colliders inside of it.
    /// This patch overrides the ping selection target if the camera is aimed
    /// at a resource inside the container.
    /// </summary>
    public class FixLockerPingPatch : IPatch 
    {
        private static readonly string PatchName = nameof(FixLockerPingPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Fixes the bug where resources inside of lockers/boxes aren't individually pingable."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public void Patch(Harmony harmony)
        {
            {
                var methodInfo = typeof(PlayerAgent).GetMethod(nameof(PlayerAgent.UpdateGlobalInput));
                harmony.Patch(methodInfo, prefix: new HarmonyMethod(AccessTools.Method(typeof(FixLockerPingPatch), nameof(PlayerAgent__UpdateGlobalInput))));
            }
            {
                var methodInfo = typeof(ResourcePackPickup).GetMethod(nameof(ResourcePackPickup.Setup));
                harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(FixLockerPingPatch), nameof(ResourcePackPickup__Setup))));
            }
        }

        private static void ResourcePackPickup__Setup(ResourcePackPickup __instance)
        {
            __instance.m_sync.add_OnSyncStateChange(
                (Il2CppSystem.Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>)(
                    (status, placement, _, _) => ResourcePackPickup__OnSyncStateChange(__instance, status, placement)
                )
            );
        }

        /// <summary>
        /// This is used to reparent a resource pickup to its container
        /// so that PlayerAgent__UpdateGlobalInput can detect pickups that
        /// were swapped.
        /// </summary>
        private static void ResourcePackPickup__OnSyncStateChange(ResourcePackPickup __instance, ePickupItemStatus status, pPickupPlacement placement)
        {
            if (status != ePickupItemStatus.PlacedInLevel) return;

            var resourceContainers = Physics.OverlapSphere(placement.position, float.Epsilon, LayerManager.MASK_PING_TARGET)
                .Select(x => x.GetComponentInParent<LG_WeakResourceContainer>())
                .Where(x => x != null)
                .ToArray();

            if (!resourceContainers.Any())
            {
                Instance.LogError($"{nameof(ResourcePackPickup)} isn't located inside of a resource container!?");
                return;
            }

            if (resourceContainers.Count() > 1)
            {
                Instance.LogError($"{nameof(ResourcePackPickup)} is inside of multiple resource containers!?");
                return;
            }

            var resourceContainer = resourceContainers.Single();
            __instance.gameObject.transform.SetParent(resourceContainer.gameObject.transform);
            __instance.gameObject.transform.SetPositionAndRotation(placement.position, placement.rotation);
        }

        private static bool PlayerAgent__UpdateGlobalInput(PlayerAgent __instance)
        {
            if (Input.GetKey(__instance.m_ffKey1) && Input.GetKeyDown(__instance.m_ffKey2)) return true;
            if (!InputMapper.GetButtonDown.Invoke(InputAction.NavMarkerPing, __instance.InputFilter)) return true;

            if (GuiManager.PlayerMarkerIsVisibleAndInFocus(__instance))
            {
                GuiManager.AttemptSetPlayerPingStatus(__instance, false);
                return false;
            }

            if (!Physics.Raycast(__instance.CamPos, __instance.FPSCamera.Forward, out var hitInfo, 40f, LayerManager.MASK_PING_TARGET, QueryTriggerInteraction.Ignore))
            {
                GuiManager.CrosshairLayer.PopAngryPingIndicator();
                return false;
            }

            var pingTarget = hitInfo.collider.GetComponentInChildren<iPlayerPingTarget>();
            var resourceContainer = hitInfo.collider.GetComponentInParent<LG_WeakResourceContainer>();

            if (resourceContainer != null)
            {
                var storageChildren = resourceContainer.m_storageComp.gameObject
                   .GetChildren()
                   .Where(x => x.GetComponentInChildren<iPlayerPingTarget>() != null)
                   .ToHashSet();

                foreach (var child in storageChildren)
                {
                    Instance.LogDebug("StorageChild :" + child.name);
                }

                var hits = Physics.RaycastAll(__instance.CamPos, __instance.FPSCamera.Forward, 40f, LayerManager.MASK_PING_TARGET, QueryTriggerInteraction.Ignore);
                foreach (var hit in hits)
                {
                    if (!storageChildren.Any(x => x.GetInstanceID() == hit.collider.gameObject.GetInstanceID())) continue;
                    Instance.LogDebug("Selecting storage child as ping target");
                    pingTarget = hit.collider.gameObject.GetComponentInChildren<iPlayerPingTarget>();
                    break;
                }
            }

            __instance.m_pingTarget = pingTarget;
            __instance.m_pingPos = hitInfo.point;
            if (__instance.m_pingTarget != null && (__instance.m_pingTarget != __instance.m_lastPingedTarget || Clock.Time > __instance.m_pingAgainTimer))
            {
                __instance.TriggerMarkerPing(__instance.m_pingTarget, __instance.m_pingPos);
                __instance.m_pingAgainTimer = Clock.Time + 2f;
                __instance.m_lastPingedTarget = __instance.m_pingTarget;
            }

            return false;
        }
    }
}
