namespace QoLFix.Patches.Misc
{
    public class ItemEquippableAnimationSequencePatch : Patch
    {
        public static Patch Instance { get; private set; }

        public override string Name { get; } = nameof(ItemEquippableAnimationSequencePatch);

        public override void Initialize()
        {
            Instance = this;
        }

        public override void Execute()
        {
            this.PatchMethod<ItemEquippable>(nameof(ItemEquippable.DoTriggerWeaponAnimSequence), PatchType.Postfix);
        }

        private static ItemEquippable ActiveItem;
        private static Il2CppSystem.Collections.IEnumerator AnimationSequence;

        public static void StopAnimation(ItemEquippable item = null)
        {
            if (ActiveItem?.Owner?.IsLocallyOwned != true) return;

            Instance.LogDebug("Aborting animation coroutine");
            if (item != null && item.Owner != ActiveItem.Owner)
            {
                Instance.LogError("The item is not owned by the local player.");
                return;
            }

            ActiveItem.StopCoroutine(AnimationSequence);

            if (item != null)
            {
                var gearPartHolder = item.GearPartHolder;
                gearPartHolder.FrontPartAnimator?.Rebind();
                gearPartHolder.ReceiverPartAnimator?.Rebind();
                gearPartHolder.StockPartAnimator?.Rebind();
                ActiveItem.Owner.AnimatorArms.Rebind();

                // This resets the animation weights, which fixes the
                // funky fingers created by rebinding AnimatorArms.
                ActiveItem.Owner.Inventory.PlayAnimationsForWieldedItem();
            }

            ActiveItem = null;
            AnimationSequence = null;
        }

        private static void ItemEquippable__DoTriggerWeaponAnimSequence__Postfix(
            ItemEquippable __instance,
            ref Il2CppSystem.Collections.IEnumerator __result)
        {
            // Even though this shouldn't happen, check just in case.
            if (!__instance.Owner.IsLocallyOwned) return;

            if (__instance != __instance.Owner.Inventory.WieldedItem)
            {
                Instance.LogWarning($"{nameof(ItemEquippable.DoTriggerWeaponAnimSequence)} was called on an item that isn't being wielded?");
                return;
            }

            Instance.LogDebug("Starting animation coroutine");
            ActiveItem = __instance;
            AnimationSequence = __result;
        }
    }
}
