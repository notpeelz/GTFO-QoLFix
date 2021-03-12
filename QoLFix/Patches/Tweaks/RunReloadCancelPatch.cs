using BepInEx.Configuration;
using GameData;
using HarmonyLib;
using Player;

namespace QoLFix.Patches.Tweaks
{
    public partial class RunReloadCancelPatch : IPatch
    {
        private const string PatchName = nameof(RunReloadCancelPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Lets you cancel weapon reloading by sprinting."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<PLOC_Stand>(nameof(PLOC_Stand.Update), PatchType.Prefix, prefixMethodName: nameof(PLOC__Update__Prefix));
            this.PatchMethod<PLOC_Crouch>(nameof(PLOC_Crouch.Update), PatchType.Prefix, prefixMethodName: nameof(PLOC__Update__Prefix));
            this.PatchMethod<ItemEquippable>(nameof(ItemEquippable.DoTriggerWeaponAnimSequence), PatchType.Postfix);
            //this.PatchMethod<ItemEquippable._DoTriggerWeaponAnimSequence_d__97>(nameof(ItemEquippable._DoTriggerWeaponAnimSequence_d__97.MoveNext), PatchType.Postfix);
        }

        //private static void _DoTriggerWeaponAnimSequence_d__97__MoveNext__Postfix() => Instance.LogDebug("Entering coroutine");

        private static Il2CppSystem.Collections.IEnumerator LastWeaponAnimationSequence;

        private static void ItemEquippable__DoTriggerWeaponAnimSequence__Postfix(
            ItemEquippable __instance,
            ref Il2CppSystem.Collections.IEnumerator __result)
        {
            // Even though this shouldn't happen, check just in case.
            if (!__instance.Owner.IsLocallyOwned) return;

            if (!__instance.m_isWielded)
            {
                Instance.LogWarning($"{nameof(ItemEquippable.DoTriggerWeaponAnimSequence)} was called on an item that isn't being wielded?");
                return;
            }

            Instance.LogDebug("Starting animation coroutine");
            LastWeaponAnimationSequence = __result;
        }

        private static void PLOC__Update__Prefix(PLOC_Stand __instance)
        {
            var player = __instance.m_owner;

            var toggleRun = PlayerLocomotion.RunToggleLock;
            try
            {
                if (!PlayerLocomotion.RunInput(player, false)) return;
            }
            finally
            {
                // PlayerLocomotion.PlayRunInput is responsible for toggling
                // sprint (when toggle sprint is enabled), so we need to
                // restore it here.
                PlayerLocomotion.RunToggleLock = toggleRun;
            }

            if (!player.Locomotion.InputIsForwardEnoughForRun()) return;
            var itemHolder = player.FPItemHolder;

            // If the item isn't busy, we have nothing to patch
            if (itemHolder?.ItemIsBusy != true) return;

            var wieldedItem = itemHolder.WieldedItem;

            // If we're reloading, cancel it
            if (wieldedItem?.IsReloading == true) return;
            wieldedItem.IsReloading = false;

            if (LastWeaponAnimationSequence != null)
            {
                Instance.LogDebug("Aborting animation coroutine");
                wieldedItem.StopCoroutine(LastWeaponAnimationSequence);
                LastWeaponAnimationSequence = null;
            }

            var gearPartHolder = wieldedItem.GearPartHolder;

            gearPartHolder.FrontPartAnimator?.Rebind();
            gearPartHolder.ReceiverPartAnimator?.Rebind();
            gearPartHolder.StockPartAnimator?.Rebind();
            player.AnimatorArms?.Rebind();
        }
    }
}
