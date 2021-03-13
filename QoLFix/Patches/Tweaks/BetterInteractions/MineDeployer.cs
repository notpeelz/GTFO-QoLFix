using AK;

namespace QoLFix.Patches.Tweaks
{
    public partial class BetterInteractionsPatch : IPatch
    {
        private static bool CanPlaceMine;
        private static bool IgnoreWorldInteractions;
        private static bool IsLookingAtMine;

        private void PatchMineDeployer()
        {
            this.PatchMethod<MineDeployerFirstPerson>(nameof(MineDeployerFirstPerson.Update), PatchType.Both);
            this.PatchMethod<MineDeployerFirstPerson>(nameof(MineDeployerFirstPerson.OnUnWield), PatchType.Postfix);
            this.PatchMethod<PlayerInteraction>($"get_{nameof(PlayerInteraction.HasWorldInteraction)}", PatchType.Prefix);
        }

        private static bool PlayerInteraction__get_HasWorldInteraction__Prefix(PlayerInteraction __instance, ref bool __result)
        {
            if (!IgnoreWorldInteractions) return HarmonyControlFlow.Execute;
            __result = false;
            return HarmonyControlFlow.DontExecute;
        }

        private static void MineDeployerFirstPerson__OnUnWield__Postfix()
        {
            CanPlaceMine = false;
        }

        private static void MineDeployerFirstPerson__Update__Prefix(MineDeployerFirstPerson __instance)
        {
            IgnoreWorldInteractions = !IsLookingAtMine;
        }

        private static void MineDeployerFirstPerson__Update__Postfix(MineDeployerFirstPerson __instance)
        {
            if (!__instance.CanWield) return;
            CanPlaceMine = __instance.CheckCanPlace();
            IgnoreWorldInteractions = false;
        }

        private static bool? PlayerInteraction__UpdateWorldInteractions__MineDeployer(PlayerInteraction __instance)
        {
            if (__instance.m_owner.FPSCamera?.CameraRayObject == null) return null;
            var interacts = __instance.m_owner.FPSCamera.CameraRayObject.GetComponents<Interact_Timed>();

            IsLookingAtMine = false;
            foreach (var interact in interacts)
            {
                // XXX: this is the most efficient way I could think of
                // to detect when the player is looking at a mine.
                // The alternatives would be either convoluted or inefficient.
                // This is dirty but it works.
                var isMine = interact.SFXInteractStart == EVENTS.INTERACT_TOOL_START
                    && interact.SFXInteractCancel == EVENTS.INTERACT_TOOL_CANCEL
                    && interact.SFXInteractEnd == EVENTS.INTERACT_TOOL_FINISHED;

                if (isMine)
                {
                    IsLookingAtMine = true;
                    return HarmonyControlFlow.Execute;
                }
            }

            if (CanPlaceMine) return HarmonyControlFlow.DontExecute;
            return null;
        }
    }
}
