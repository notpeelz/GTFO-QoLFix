using BepInEx.Configuration;
using Player;
using QoLFix.Patches.Misc;

namespace QoLFix.Patches.Tweaks
{
    public partial class RunReloadCancelPatch : Patch
    {
        private const string PatchName = nameof(RunReloadCancelPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Lets you cancel weapon reloading by sprinting."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => ConfigEnabled.GetConfigEntry<bool>().Value;

        public override void Execute()
        {
            QoLFixPlugin.RegisterPatch<ItemEquippableAnimationSequencePatch>();
            this.PatchMethod<PLOC_Stand>(nameof(PLOC_Stand.Update), PatchType.Prefix);
            this.PatchMethod<PLOC_Crouch>(nameof(PLOC_Crouch.Update), PatchType.Prefix);
            //this.PatchMethod<ItemEquippable._DoTriggerWeaponAnimSequence_d__97>(nameof(ItemEquippable._DoTriggerWeaponAnimSequence_d__97.MoveNext), PatchType.Postfix);
        }

        //private static void _DoTriggerWeaponAnimSequence_d__97__MoveNext__Postfix() => Instance.LogDebug("Entering coroutine");

        private static void PLOC_Stand__Update__Prefix(PLOC_Stand __instance) =>
            PLOC__Update__Prefix(__instance, false);

        private static void PLOC_Crouch__Update__Prefix(PLOC_Stand __instance) =>
            PLOC__Update__Prefix(__instance, true);

        private static void PLOC__Update__Prefix(PLOC_Stand __instance, bool isCrouching)
        {
            var player = __instance.m_owner;
            if (!player.IsLocallyOwned) return;

            var toggleRun = PlayerLocomotion.RunToggleLock;
            try
            {
                if (!PlayerLocomotion.RunInput(player, isCrouching)) return;
            }
            finally
            {
                // PlayerLocomotion.PlayRunInput is responsible for toggling
                // sprint (when toggle sprint is enabled), so we need to
                // restore it here.
                PlayerLocomotion.RunToggleLock = toggleRun;
            }

            if (!player.Locomotion.InputIsForwardEnoughForRun()) return;
            if (isCrouching && !player.PlayerCharacterController.ColliderCanStand()) return;

            var itemHolder = player.FPItemHolder;

            // If the item isn't busy, we have nothing to patch
            if (itemHolder?.ItemIsBusy != true) return;

            var wieldedItem = itemHolder.WieldedItem;

            // If we're reloading, cancel it
            if (wieldedItem?.IsReloading != true) return;
            wieldedItem.IsReloading = false;

            ItemEquippableAnimationSequencePatch.StopAnimation(wieldedItem);
        }
    }
}
