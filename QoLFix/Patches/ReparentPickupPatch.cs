using Gear;
using HarmonyLib;
using LevelGeneration;
using Player;
using UnityEngine;

namespace QoLFix.Patches
{
    /// <summary>
    /// This is used to reparent an item pickup to its container
    /// so that GetComponentInChildren() can find pickups that
    /// were swapped.
    /// </summary>
    public class ReparentPickupPatch : IPatch
    {
        public static IPatch Instance { get; private set; }

        public string Name => nameof(ReparentPickupPatch);

        public bool Enabled => true;

        public void Initialize()
        {
            Instance = this;
        }

        public void Patch(Harmony harmony)
        {
            // We can't patch ItemInLevel.Setup directly because it gets
            // executed before the ones from derived classes.
            // This is a problem because we need to add a callback to
            // OnSyncStateChange after everything else.
            {
                var methodInfo = typeof(ConsumablePickup_Core).GetMethod(nameof(ConsumablePickup_Core.Setup));
                harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(ReparentPickupPatch), nameof(ItemInLevel__Setup))));
            }
            {
                var methodInfo = typeof(GenericSmallPickupItem_Core).GetMethod(nameof(GenericSmallPickupItem_Core.Setup));
                harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(ReparentPickupPatch), nameof(ItemInLevel__Setup))));
            }
            {
                var methodInfo = typeof(ResourcePackPickup).GetMethod(nameof(ResourcePackPickup.Setup));
                harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(ReparentPickupPatch), nameof(ItemInLevel__Setup))));
            }
        }

        private static void ItemInLevel__Setup(ItemInLevel __instance)
        {
            __instance.GetSyncComponent().add_OnSyncStateChange(
                (Il2CppSystem.Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>)(
                    (status, placement, _, _) => ItemInLevel__OnSyncStateChange(__instance, status, placement)
                )
            );
        }

        private static void ItemInLevel__OnSyncStateChange(ItemInLevel __instance, ePickupItemStatus status, pPickupPlacement placement)
        {
            if (status != ePickupItemStatus.PlacedInLevel) return;

            var resourceContainer = GTFOUtils.GetParentResourceContainer(placement.position);
            if (resourceContainer == null) return;

            // Abort if the item was already reparented
            if (__instance.GetComponentInParent<LG_WeakResourceContainer>() != null) return;

            Instance.LogDebug($"Reparenting {__instance.name} {__instance.PublicName}");
            __instance.gameObject.transform.SetParent(resourceContainer.gameObject.transform);
            __instance.gameObject.transform.SetPositionAndRotation(placement.position, placement.rotation);
        }
    }
}
