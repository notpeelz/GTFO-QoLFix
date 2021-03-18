using AK;
using LevelGeneration;
using Player;
using QoLFix.Patches.Misc;
using UnityEngine;

namespace QoLFix.Patches.Tweaks
{
    public partial class BetterInteractionsPatch : Patch
    {
        // Used to keep track of whether to replace the mine interaction
        // with a world interaction (when looked directly at).
        private static class WorldInteractionOverride
        {
            public static bool state;
            public static bool oldState;
        }

        private static bool CanPlaceMine;
        private static bool UseWorldInteraction;
        private static bool IsMineCooldownActive;

        private void PatchMineDeployer()
        {
            QoLFixPlugin.RegisterPatch<WorldInteractionBlockerPatch>();
            this.PatchMethod<MineDeployerFirstPerson>(nameof(MineDeployerFirstPerson.Update), PatchType.Both);
            this.PatchMethod<MineDeployerFirstPerson>(nameof(MineDeployerFirstPerson.OnUnWield), PatchType.Both);
            this.PatchMethod<MineDeployerFirstPerson>(nameof(MineDeployerFirstPerson.OnWield), PatchType.Postfix);
            this.PatchMethod<MineDeployerFirstPerson>(nameof(MineDeployerFirstPerson.CheckCanPlace), PatchType.Prefix);
            this.PatchMethod<MineDeployerFirstPerson>(nameof(MineDeployerFirstPerson.ShowPlacementIndicator), PatchType.Prefix);
            this.PatchMethod<MineDeployerFirstPerson>(nameof(MineDeployerFirstPerson.OnStickyMineSpawned), PatchType.Postfix);
            this.PatchMethod<MineDeployerFirstPerson>(nameof(MineDeployerFirstPerson.ShowItem), PatchType.Prefix);

            LevelCleanupPatch.OnExitLevel += () =>
            {
                CanPlaceMine = false;
                UseWorldInteraction = false;
                IsMineCooldownActive = false;
            };
        }

        private static void MineDeployerFirstPerson__OnStickyMineSpawned__Postfix(MineDeployerFirstPerson __instance, PlayerAgent sourceAgent)
        {
            if (!sourceAgent.IsLocallyOwned) return;

            IsMineCooldownActive = true;

            __instance.m_lastCanPlace = false;
            __instance.m_lastShowIndicator = false;
            // FIXME: for some reason the placement indicator is incredibly
            // stubborn and won't disappear...
            __instance.m_placementIndicator?.SetVisible(false);
            __instance.m_placementIndicator?.SetPlacementEnabled(false);

            if (__instance.FPItemHolder == null) return;

            __instance.FPItemHolder.ItemHiddenTrigger = false;

            // Don't trigger the cooldown for consumables
            if (__instance.m_isConsumable)
            {
                IsMineCooldownActive = false;
                Instance.LogDebug("Exiting OnStickyMineSpawned early");
                return;
            }

            // Put FPItemHolder in a down state instead of hidden state
            __instance.FPItemHolder.ItemDownTrigger = true;
            Instance.LogDebug("Disabling mine deployer");
        }

        private static bool MineDeployerFirstPerson__ShowItem__Prefix(MineDeployerFirstPerson __instance)
        {
            // Don't trigger the cooldown for consumables
            if (__instance.m_isConsumable)
            {
                Instance.LogDebug("Cancelling ShowItem callback");
                return HarmonyControlFlow.DontExecute;
            }

            Instance.LogDebug("Enabling mine deployer");

            IsMineCooldownActive = false;
            if (__instance.FPItemHolder == null) return HarmonyControlFlow.Execute;

            __instance.FPItemHolder.ItemDownTrigger = false;

            // Set this to true so that the game doesn't fudge the counter
            // when attempting to get out of the Hidden state.
            __instance.FPItemHolder.ItemHiddenTrigger = true;

            return HarmonyControlFlow.Execute;
        }

        private static bool MineDeployerFirstPerson__ShowPlacementIndicator__Prefix(ref bool __result)
        {
            if (WorldInteractionOverride.state || IsMineCooldownActive)
            {
                __result = false;
                return HarmonyControlFlow.DontExecute;
            }

            return HarmonyControlFlow.Execute;
        }

        private static bool MineDeployerFirstPerson__CheckCanPlace__Prefix(ref bool __result)
        {
            if (WorldInteractionOverride.state || IsMineCooldownActive)
            {
                __result = false;
                return HarmonyControlFlow.DontExecute;
            }

            return HarmonyControlFlow.Execute;
        }

        private static void MineDeployerFirstPerson__OnUnWield__Postfix()
        {
            Instance.LogDebug("Unwielding mine deployer");
            CanPlaceMine = false;
        }

        private static void MineDeployerFirstPerson__OnUnWield__Prefix(MineDeployerFirstPerson __instance)
        {
            // We have to undo the Down effect since swapping weapons will
            // cause the execution of ShowItem() to pause.
            if (IsMineCooldownActive)
            {
                Instance.LogDebug("Removing ItemDownTrigger due to paused mine deployer cooldown");
                __instance.FPItemHolder.ItemDownTrigger = false;
            }
        }

        private static void MineDeployerFirstPerson__OnWield__Postfix(MineDeployerFirstPerson __instance)
        {
            // We have to resume the Down effect here since it was paused
            // when we unwielded.
            if (IsMineCooldownActive)
            {
                Instance.LogDebug("Reapplying ItemDownTrigger due to paused mine deployer cooldown");
                __instance.FPItemHolder.ItemDownTrigger = true;
            }
        }

        private static bool MineDeployerFirstPerson__Update__Prefix(MineDeployerFirstPerson __instance)
        {
            // This is so that HasWorldInteraction can return false (to
            // ignore world interactions) unless the player is looking
            // directly at a mine. HasWorldInteraction is used the vanilla
            // CheckCanPlace method, which causes world interactions to
            // suppress the mine deployer interaction.
            if (!UseWorldInteraction)
            {
                WorldInteractionBlockerPatch.IgnoreWorldInteractions++;
            }

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
            if (!UseWorldInteraction)
            {
                WorldInteractionBlockerPatch.IgnoreWorldInteractions--;
            }
        }

        private static bool? PlayerInteraction__UpdateWorldInteractions__MineDeployer(PlayerInteraction __instance)
        {
            var obj = __instance.m_owner.FPSCamera?.CameraRayObject;
            if (obj == null) return null;

            UseWorldInteraction = false;

            // Players probably don't want to put mines down while running,
            // so let them use the world interactions.
            if (__instance.m_owner.Locomotion.m_currentStateEnum == PlayerLocomotion.PLOC_State.Run)
            {
                UseWorldInteraction = true;
                return HarmonyControlFlow.Execute;
            }

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
                    UseWorldInteraction = true;
                    return HarmonyControlFlow.Execute;
                }
            }

            //Instance.LogDebug($"CanPlaceMine: {CanPlaceMine}; IsLadderInteraction: {IsLadderInteraction}");
            if (CanPlaceMine && !WorldInteractionOverride.state) return HarmonyControlFlow.DontExecute;
            return null;
        }
    }
}
