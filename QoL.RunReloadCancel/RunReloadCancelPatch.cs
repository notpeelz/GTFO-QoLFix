using BepInEx.Configuration;
using HarmonyLib;
using MTFO.Core;
using Player;
using QoL.Common.Patches;

namespace QoL.RunReloadCancel
{
    public class RunReloadCancelPatch : MTFOPatch
    {
        private const string PatchName = nameof(RunReloadCancelPatch);

        private static ConfigEntry<bool> ConfigEnabled = default!;

        public static RunReloadCancelPatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Lets you cancel weapon reloading by sprinting."));
        }

        [HarmonyPatch(typeof(PLOC_Stand))]
        [HarmonyPatch(nameof(PLOC_Stand.Update))]
        [HarmonyPrefix]
        private static void PLOC_Stand__Update__Prefix(PLOC_Stand __instance) =>
            PLOC__Update__Prefix(__instance.m_owner, false);

        [HarmonyPatch(typeof(PLOC_Crouch))]
        [HarmonyPatch(nameof(PLOC_Crouch.Update))]
        [HarmonyPrefix]
        private static void PLOC_Crouch__Update__Prefix(PLOC_Crouch __instance) =>
            PLOC__Update__Prefix(__instance.m_owner, true);


        [HarmonyPatch(typeof(PLOC_Jump))]
        [HarmonyPatch(nameof(PLOC_Jump.Update))]
        [HarmonyPrefix]
        private static void PLOC_Jump__Update__Prefix(PLOC_Jump __instance) =>
            PLOC__Update__Prefix(__instance.m_owner, false);

        private static void PLOC__Update__Prefix(PlayerAgent player, bool isCrouching)
        {
            if (!player.IsLocallyOwned) return;

            if (!RunInput(player)) return;

            if (!player.Locomotion.InputIsForwardEnoughForRun()) return;
            if (isCrouching && !player.PlayerCharacterController.ColliderCanStand()) return;

            var itemHolder = player.FPItemHolder;

            // If the item isn't busy, we have nothing to patch
            if (itemHolder?.ItemIsBusy != true) return;

            var wieldedItem = itemHolder.WieldedItem;

            // If we're reloading, cancel it
            if (wieldedItem?.IsReloading != true) return;
            wieldedItem.IsReloading = false;

            ItemEquippableAnimationSequencePatch.StopAnimation();
        }

        private static bool RunInput(PlayerAgent player)
        {
            var toggleRun = PlayerLocomotion.RunToggleLock;
            try
            {
                return PlayerLocomotion.RunInput(player, true);
            }
            finally
            {
                // PlayerLocomotion.PlayRunInput is responsible for toggling
                // sprint (when toggle sprint is enabled), so we need to
                // restore it here.
                PlayerLocomotion.RunToggleLock = toggleRun;
            }
        }
    }
}
