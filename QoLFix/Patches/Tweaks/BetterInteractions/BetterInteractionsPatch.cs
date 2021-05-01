using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Gear;
using Player;
using QoLFix.Patches.Common;
using QoLFix.Patches.Misc;
using QoLFix.UI;
using UnityEngine;

namespace QoLFix.Patches.Tweaks
{
    public partial class BetterInteractionsPatch : Patch
    {
        private const string PatchName = nameof(BetterInteractionsPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");
        private static readonly ConfigDefinition ConfigPatchInteractDistance = new(PatchName, "PatchInteractDistance");
        private static readonly ConfigDefinition ConfigPatchMineDeployer = new(PatchName, "PatchMineDeployer");
        private static readonly ConfigDefinition ConfigPatchRevive = new(PatchName, "PatchRevive");
        private static readonly ConfigDefinition ConfigPatchHackingTool = new(PatchName, "PatchHackingTool");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Fixes several quirks of the interaction system."));
            QoLFixPlugin.Instance.Config.Bind(ConfigPatchInteractDistance, true, new ConfigDescription("Fixes interactions cancelling when moving too far away (sentries, mines on ceiling, etc.)"));
            QoLFixPlugin.Instance.Config.Bind(ConfigPatchMineDeployer, true, new ConfigDescription("Fixes the mine deployer prioritizing doors over placing mines."));
            QoLFixPlugin.Instance.Config.Bind(ConfigPatchRevive, true, new ConfigDescription("Gives you full control over your camera while reviving. NOTE: for balance reasons, this also prevents you from firing/ADSing while reviving."));
            QoLFixPlugin.Instance.Config.Bind(ConfigPatchHackingTool, true, new ConfigDescription("Prevents the hacking tool minigame from getting cancelled if you swapped weapons/moved too early."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => ConfigEnabled.GetConfigEntry<bool>().Value;

        public override void Execute()
        {
            this.PatchMethod<PlayerInteraction>(nameof(PlayerInteraction.UpdateWorldInteractions), PatchType.Prefix);
            this.PatchMethod<PlayerInteraction>(nameof(PlayerInteraction.Setup), PatchType.Postfix);
            this.PatchMethod<PlayerInteraction>($"get_{nameof(PlayerInteraction.HasWorldInteraction)}", PatchType.Prefix);
            this.PatchMethod<Interact_Timed>(nameof(Interact_Timed.PlayerCheckInput), PatchType.Postfix);
            ActionScheduler.Repeat(UpdateWorldInteractions);

            if (ConfigPatchInteractDistance.GetConfigEntry<bool>().Value)
            {
                this.PatchInteractDistance();
            }

            if (ConfigPatchRevive.GetConfigEntry<bool>().Value)
            {
                this.PatchRevive();
            }

            if (ConfigPatchHackingTool.GetConfigEntry<bool>().Value)
            {
                this.PatchHackingTool();
            }
        }

        public static SharedResource InteractionBlocker { get; } = new();

        public static SharedResource SphereCheckBlocker { get; } = new();

        public static SharedResource CameraRayCheckBlocker { get; } = new();

        public static bool CameraRayInteractionEnabled => PlayerInteraction.CameraRayInteractionEnabled && !CameraRayCheckBlocker.InUse;

        public static bool SphereInteractionEnabled => PlayerInteraction.CameraRayInteractionEnabled && !CameraRayCheckBlocker.InUse;

        public static bool InteractionEnabled => PlayerInteraction.InteractionEnabled && !CameraRayCheckBlocker.InUse;

        private static void UpdateWorldInteractions()
        {
            var @this = PlayerInteractionInstance;
            if (@this == null) return;

            var player = @this.m_owner;
            if (!player.Alive) return;

            //foreach (var prefix in PrefixList)
            //{
            //    if (prefix(@this) == HarmonyControlFlow.DontExecute)
            //    {
            //        return HarmonyControlFlow.DontExecute;
            //    }
            //}

            if (InteractionBlocker.InUse)
            {
                if (@this.m_bestSelectedInteract != null) @this.UnSelectCurrentBestInteraction();
                return;
            }

            if (IsTimerActive)
            {
                Instance.LogWarning("Timer is active 2");
                // This is reset before next execution of
                // Interact_Timed::PlayerCheckInput
                // IMPORTANT: we rely on our method being executed on LateUpdate(),
                // meaning that Interact_Timed::PlayerCheckInput will always
                // run before we do.
                IsTimerActive = false;
                DoInteract();
                return;
            }

            if (!PlayerInteraction.InteractionEnabled
                || player.Inventory.WieldedItem?.AllowPlayerInteraction == false
                || (player.IsNSpace && FocusStateManager.CurrentState != eFocusState.FPSNSpace)
                || (!player.IsNSpace && FocusStateManager.CurrentState != eFocusState.FPS))
            {
                @this.UnSelectCurrentBestInteraction();
                return;
            }

            var camPos = player.CamPos;
            if (CameraRayInteractionEnabled
                && player.FPSCamera.CameraRayObject != null
                && player.FPSCamera.CameraRayDist < (@this.m_searchRadius + Mathf.Min(Mathf.Abs(player.TargetLookDir.y), 0.5f) * 1f)
                && player.FPSCamera.CameraRayObject.layer == LayerManager.LAYER_INTERACTION)
            {
                var components = player.FPSCamera.CameraRayObject.GetComponents<Interact_Base>();
                for (var i = 0; i < components.Length; i++)
                {
                    if (components[i] != null
                        && components[i].IsActive
                        && !components[i].ManualTriggeringOnly
                        && components[i].PlayerCanInteract(player)
                        && (!PlayerBackpackManager.HasItem_Local(InventorySlot.InLevelCarry) || components[i].AllowTriggerWithCarryItem))
                    {
                        @this.m_bestInteractInCurrentSearch = components[i];
                        @this.m_currentBestInteractIsFromCamera = true;
                        break;
                    }
                }
            }

            if (SphereInteractionEnabled && @this.m_bestInteractInCurrentSearch == null)
            {
                @this.m_currentBestInteractIsFromCamera = false;
                for (var i = 0; i < PlayerInteraction.s_collCount; ++i)
                {
                    if (PlayerInteraction.s_collidersClose[i] != null)
                    {
                        foreach (var component in PlayerInteraction.s_collidersClose[i].GetComponents<Interact_Base>())
                        {
                            if (component != null
                                && component.IsActive
                                && !component.ManualTriggeringOnly
                                && !component.OnlyActiveWhenLookingStraightAt
                                && (!PlayerBackpackManager.HasItem_Local(InventorySlot.InLevelCarry) || component.AllowTriggerWithCarryItem))
                            {
                                var position = component.transform.position;
                                var v = position - camPos;
                                if (v.y < 2.0) v.y = 0.0f;
                                if (v.sqrMagnitude <= @this.m_proximityRadius * @this.m_proximityRadius)
                                {
                                    @this.AddToProximity(component);
                                }
                                else
                                {
                                    @this.RemoveFromProximity(component);
                                }
                                var screenPoint = @this.m_playerCam.WorldToScreenPoint(position);
                                if (screenPoint.z > 0.0 && GuiManager.IsOnScreen(screenPoint))
                                {
                                    PlayerInteraction.s_tempDis = (GuiManager.ScreenCenter - (Vector2)screenPoint).sqrMagnitude;
                                    if (component.PlayerCanInteract(player))
                                    {
                                        bool flag = false;
                                        if (component.RequireCollisionCheck())
                                        {
                                            flag = Physics.Raycast(camPos, v.normalized, out var hitInfo, v.magnitude, LayerManager.MASK_PLAYER_INTERACT_BLOCKERS)
                                                && hitInfo.collider.gameObject != component.gameObject;
                                        }
                                        if (!flag && (PlayerInteraction.s_tempShortDis < 0.0
                                            || PlayerInteraction.s_tempDis < PlayerInteraction.s_tempShortDis))
                                        {
                                            @this.m_bestInteractInCurrentSearch = component;
                                            PlayerInteraction.s_tempShortDis = PlayerInteraction.s_tempDis;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (@this.m_bestInteractInCurrentSearch != null
                // Don't switch to a different interaction while nearby a
                // ladder, unless looking directly at an interaction.
                && (!@this.m_enterLadderVisible || @this.m_currentBestInteractIsFromCamera))
            {
                if (@this.m_bestSelectedInteract?.Cast<Interact_Base>() != @this.m_bestInteractInCurrentSearch)
                {

                    if (@this.m_bestSelectedInteract != null)
                    {
                        Instance.LogWarning("Deselecting previous interact");
                        @this.m_bestSelectedInteract.PlayerSetSelected(false, player);
                    }

                    Instance.LogWarning("Selecting m_bestSelectedInteract");
                    @this.m_bestSelectedInteract = @this.m_bestInteractInCurrentSearch.Cast<IInteractable>();

                    Instance.LogWarning("Selecting new interact");

                    @this.WantToEnterLadder = false;
                    @this.m_enterLadderVisible = false;
                    GuiManager.InteractionLayer.InteractPromptVisible = true;
                    @this.m_bestSelectedInteract.PlayerSetSelected(true, player);
                }
            }
            else
            {
                @this.UnSelectCurrentBestInteraction();
                if (PlayerInteraction.LadderInteractionEnabled && @this.WantToEnterLadder)
                {
                    if (!@this.m_enterLadderVisible || !GuiManager.InteractionLayer.InteractPromptVisible)
                    {
                        GuiManager.InteractionLayer.SetInteractPrompt("Climb Ladder", $"Press '{InputMapper.GetBindingName(InputAction.Use)}'", ePUIMessageStyle.Default);
                        GuiManager.InteractionLayer.InteractPromptVisible = true;
                        @this.m_enterLadderVisible = true;
                    }
                    if (InputMapper.GetButtonDown.Invoke(InputAction.Use, player.InputFilter))
                    {
                        @this.WantToEnterLadder = false;
                        PlayerFPSUtils.DoEnterLadder(player, @this.m_ladderToEnter, @this.m_ladderAllowTransition, @this.m_ladderEntryPos, @this.m_ladderTransDuration);
                    }
                }
                else if (!@this.WantToEnterLadder && @this.m_enterLadderVisible)
                {
                    Instance.LogWarning("Hiding ladder interaction");
                    GuiManager.InteractionLayer.InteractPromptVisible = false;
                    @this.m_enterLadderVisible = false;
                }
            }

            DoInteract();

            void DoInteract()
            {
                if (@this.m_bestSelectedInteract == null || !@this.m_bestSelectedInteract.PlayerCheckInput(player))
                {
                    return;
                }
                @this.m_bestSelectedInteract.PlayerDoInteract(player);
            }
        }

        private static bool PlayerInteraction__UpdateWorldInteractions__Prefix() => HarmonyControlFlow.DontExecute;

        private static bool PlayerInteraction__get_HasWorldInteraction__Prefix(ref bool __result)
        {
            if (InteractionBlocker.InUse)
            {
                __result = true;
                return HarmonyControlFlow.DontExecute;
            }
            return HarmonyControlFlow.Execute;
        }

        private static PlayerInteraction PlayerInteractionInstance;

        private static void PlayerInteraction__Setup__Postfix(PlayerInteraction __instance)
        {
            PlayerInteractionInstance = __instance;
        }

        private static bool IsTimerActive;

        private static void Interact_Timed__PlayerCheckInput__Postfix(Interact_Timed __instance)
        {
            IsTimerActive |= __instance.TimerIsActive;
        }
    }
}
