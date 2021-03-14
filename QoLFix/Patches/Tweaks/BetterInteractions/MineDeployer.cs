using AK;
using LevelGeneration;
using UnityEngine;

namespace QoLFix.Patches.Tweaks
{
    public partial class BetterInteractionsPatch : IPatch
    {
        // Used to keep track of whether to replace the mine interaction
        // with a world interaction (when looked directly at).
        private static class WorldInteractionOverride
        {
            public static bool state;
            public static bool oldState;
        }

        private static bool CanPlaceMine;
        private static bool IgnoreWorldInteractions;
        private static bool IsLookingAtMine;

        private void PatchMineDeployer()
        {
            this.PatchMethod<MineDeployerFirstPerson>(nameof(MineDeployerFirstPerson.Update), PatchType.Both);
            this.PatchMethod<MineDeployerFirstPerson>(nameof(MineDeployerFirstPerson.OnUnWield), PatchType.Postfix);
            this.PatchMethod<MineDeployerFirstPerson>(nameof(MineDeployerFirstPerson.CheckCanPlace), PatchType.Prefix);
            this.PatchMethod<MineDeployerFirstPerson>(nameof(MineDeployerFirstPerson.ShowPlacementIndicator), PatchType.Prefix);
            this.PatchMethod<PlayerInteraction>($"get_{nameof(PlayerInteraction.HasWorldInteraction)}", PatchType.Prefix);
        }

        private static bool MineDeployerFirstPerson__ShowPlacementIndicator__Prefix(ref bool __result)
        {
            if (WorldInteractionOverride.state)
            {
                __result = false;
                return HarmonyControlFlow.DontExecute;
            }

            return HarmonyControlFlow.Execute;
        }

        private static bool MineDeployerFirstPerson__CheckCanPlace__Prefix(ref bool __result)
        {
            if (WorldInteractionOverride.state)
            {
                __result = false;
                return HarmonyControlFlow.DontExecute;
            }

            return HarmonyControlFlow.Execute;
        }

        private static bool PlayerInteraction__get_HasWorldInteraction__Prefix(ref bool __result)
        {
            if (!IgnoreWorldInteractions) return HarmonyControlFlow.Execute;
            __result = false;
            return HarmonyControlFlow.DontExecute;
        }

        private static void MineDeployerFirstPerson__OnUnWield__Postfix()
        {
            CanPlaceMine = false;
        }

        private static bool MineDeployerFirstPerson__Update__Prefix(MineDeployerFirstPerson __instance)
        {
            // This is so that HasWorldInteraction can return false (to
            // ignore world interactions) unless the player is looking
            // directly at a mine. HasWorldInteraction is used the vanilla
            // CheckCanPlace method, which causes world interactions to
            // suppress the mine deployer interaction.
            IgnoreWorldInteractions = !IsLookingAtMine;

            // Disables the placement indicator if we're looking at a ladder
            var playerInteraction = __instance.Owner.Interaction;
            WorldInteractionOverride.oldState = WorldInteractionOverride.state;
            WorldInteractionOverride.state = playerInteraction.m_enterLadderVisible
                || PlayerInteraction.LadderInteractionEnabled && playerInteraction.WantToEnterLadder
                // Prioritize interacting with certain things if looked at directly
                || GTFOUtils.GetComponentInSight<Component>(
                    playerAgent: __instance.Owner,
                    comp: out var comp,
                    hitPos: out var _,
                    maxDistance: 2.5f,
                    layerMask: LayerManager.MASK_PLAYER_INTERACT_SPHERE,
                    predicate: comp =>
                    {
                        if (comp.Is<iLG_WeakLockHolder>()) return true;
                        if (comp.Is<LG_BulkheadDoorController_Core>()) return true;
                        if (comp.Is<iWardenObjectiveItem>()) return true;
                        if (comp.Is<iPickupItemSync>()) return true;
                        if (comp.Is<DropResourcesPatch.StorageSlotPlaceholder>()) return true;
                        return false;
                    })
                && comp != null;

            // For some reason, patching CheckCanPlace and
            // ShowPlacementIndicator isn't sufficient to hide the indicator.
            var stateChanged = WorldInteractionOverride.oldState != WorldInteractionOverride.state;
            if (stateChanged && WorldInteractionOverride.state)
            {
                Instance.LogDebug("Disabling placement indicator");
                __instance.m_lastCanPlace = false;
                __instance.m_lastShowIndicator = false;
                __instance.m_placementIndicator?.SetVisible(false);
                __instance.m_placementIndicator?.SetPlacementEnabled(false);
                playerInteraction.UnSelectCurrentBestInteraction();
                return HarmonyControlFlow.DontExecute;
            }

            return HarmonyControlFlow.Execute;
        }

        private static void MineDeployerFirstPerson__Update__Postfix(MineDeployerFirstPerson __instance)
        {
            CanPlaceMine = __instance.CanWield && __instance.CheckCanPlace();
            //Instance.LogDebug($"CanPlaceMine: {CanPlaceMine}; ShowPlacementIndicator: {__instance.ShowPlacementIndicator()}");
            IgnoreWorldInteractions = false;
        }

        private static bool? PlayerInteraction__UpdateWorldInteractions__MineDeployer(PlayerInteraction __instance)
        {
            var obj = __instance.m_owner.FPSCamera?.CameraRayObject;
            if (obj == null) return null;

            IsLookingAtMine = false;
            var interacts = obj.GetComponents<Interact_Timed>();
            foreach (var interact in interacts)
            {
                // XXX: this is the most efficient way I could think of
                // to detect when the player is looking at a mine.
                // The alternatives would be either convoluted or inefficient.
                // This is dirty but it works.
                var isMine = interact.SFXInteractStart == EVENTS.INTERACT_TOOL_START
                    && interact.SFXInteractCancel == EVENTS.INTERACT_TOOL_CANCEL
                    && interact.SFXInteractEnd == EVENTS.INTERACT_TOOL_FINISHED;

                if (isMine && interact.enabled)
                {
                    IsLookingAtMine = true;
                    return HarmonyControlFlow.Execute;
                }
            }

            //Instance.LogDebug($"CanPlaceMine: {CanPlaceMine}; IsLadderInteraction: {IsLadderInteraction}");
            if (CanPlaceMine && !WorldInteractionOverride.state) return HarmonyControlFlow.DontExecute;
            return null;
        }
    }
}
