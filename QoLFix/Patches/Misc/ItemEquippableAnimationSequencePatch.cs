using HarmonyLib;

namespace QoLFix.Patches.Misc
{
    public class ItemEquippableAnimationSequencePatch : IPatch
    {
        public static IPatch Instance { get; private set; }

        public string Name { get; } = nameof(ItemEquippableAnimationSequencePatch);

        public Harmony Harmony { get; set; }

        public void Initialize()
        {
            Instance = this;
        }

        public void Patch()
        {
            this.PatchMethod<ItemEquippable>(nameof(ItemEquippable.DoTriggerWeaponAnimSequence), PatchType.Postfix);
        }

        private static ItemEquippable ActiveItem;
        private static Il2CppSystem.Collections.IEnumerator AnimationSequence;

        public static void StopAnimation()
        {
            if (ActiveItem == null) return;
            Instance.LogDebug("Aborting animation coroutine");
            ActiveItem.StopCoroutine(AnimationSequence);
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
