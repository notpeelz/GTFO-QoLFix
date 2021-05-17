using HarmonyLib;
using MTFO.Core;
using MTFO.Core.Scheduling;
using Player;

namespace QoL.Common.Patches
{
    public class ItemEquippableAnimationSequencePatch : MTFOPatch
    {
        public static ItemEquippableAnimationSequencePatch? Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
        }

        protected override void Apply()
        {
            base.Apply();

            var nestedTypes = typeof(ItemEquippable).GetNestedTypes();
            var patched = false;
            foreach (var t in nestedTypes)
            {
                if (!t.Name.Contains("DoTriggerWeaponAnimSequence")) continue;
                var moveNext = AccessTools.Method(t, "MoveNext");
                if (moveNext == null) continue;

                this.LogDebug($"Patching coroutine IEnumerable: {t.Name}");
                this.PatchMethod(
                    classType: t,
                    methodBase: moveNext,
                    patchType: PatchType.Prefix,
                    prefixMethodName: nameof(DoTriggerWeaponAnimSequence__MoveNext__Prefix));
                patched = true;
                break;
            }

            if (!patched)
            {
                Instance!.LogError("Failed to find a coroutine IEnumerator to patch.");
                return;
            }

            ActionScheduler.Repeat(() =>
            {
                AbortAnimations = false;
            });
        }

        private static bool AbortAnimations;

        private static bool DoTriggerWeaponAnimSequence__MoveNext__Prefix(ref bool __result)
        {
            if (AbortAnimations)
            {
                // Stop the enumerator
                __result = false;
                return HarmonyControlFlow.DontExecute;
            }
            return HarmonyControlFlow.Execute;
        }

        public static void StopAnimation()
        {
            var playerAgent = PlayerManager.GetLocalPlayerAgent();
            if (playerAgent?.Inventory?.WieldedItem == null) return;
            var item = playerAgent.Inventory.WieldedItem;

            var gearPartHolder = item.GearPartHolder;
            if (gearPartHolder != null)
            {
                gearPartHolder.FrontPartAnimator?.Rebind();
                gearPartHolder.ReceiverPartAnimator?.Rebind();
                gearPartHolder.StockPartAnimator?.Rebind();
            }
            playerAgent.AnimatorArms.Rebind();

            // This resets the animation weights, which fixes the
            // funky fingers created by rebinding AnimatorArms.
            playerAgent.Inventory.PlayAnimationsForWieldedItem();

            AbortAnimations = true;
        }
    }
}
