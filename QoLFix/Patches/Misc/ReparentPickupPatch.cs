using Gear;
using LevelGeneration;
using Player;

namespace QoLFix.Patches.Misc
{
    /// <summary>
    /// This is used to reparent an item pickup to its container
    /// so that GetComponentInChildren() can find pickups that
    /// were swapped.
    /// </summary>
    public class ReparentPickupPatch : Patch
    {
        public static Patch Instance { get; private set; }

        public override string Name { get; } = nameof(ReparentPickupPatch);

        public override void Initialize()
        {
            Instance = this;
        }

        public override void Execute()
        {
            // We can't patch ItemInLevel.Setup directly because it gets
            // executed before the ones from derived classes.
            // This is a problem because we need to add a callback to
            // OnSyncStateChange after everything else.
            this.PatchMethod<ConsumablePickup_Core>(
                methodName: nameof(ConsumablePickup_Core.Setup),
                patchType: PatchType.Postfix,
                postfixMethodName: nameof(ItemInLevel__Setup__Postfix));
            this.PatchMethod<GenericSmallPickupItem_Core>(
                methodName: nameof(GenericSmallPickupItem_Core.Setup),
                patchType: PatchType.Postfix,
                postfixMethodName: nameof(ItemInLevel__Setup__Postfix));
            this.PatchMethod<ResourcePackPickup>(
                methodName: nameof(ResourcePackPickup.Setup),
                patchType: PatchType.Postfix,
                postfixMethodName: nameof(ItemInLevel__Setup__Postfix));
        }

        private static void ItemInLevel__Setup__Postfix(ItemInLevel __instance)
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
