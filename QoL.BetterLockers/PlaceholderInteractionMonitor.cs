using System;
using System.Linq;
using LevelGeneration;
using Player;
using SNetwork;
using UnhollowerBaseLib.Attributes;
using UnityEngine;

namespace QoL.BetterLockers
{
    partial class DropResourcesPatch
    {
        public class PlaceholderInteractionMonitor : MonoBehaviour
        {
            public PlaceholderInteractionMonitor(IntPtr pointer)
                : base(pointer) { }

            private PlayerAgent playerAgent = default!;
            private Interact_ManualTimedWithCallback interact = default!;
            private GameObject? ghostGO;
            private StorageSlotPlaceholder? currentPlaceholder;
            private ItemEquippable? currentWieldedItem;

            public static bool DisableInteractions { get; private set; }

            internal void Awake()
            {
                Instance!.LogDebug($"{nameof(PlaceholderInteractionMonitor)}.{nameof(Awake)}()");
                this.playerAgent = this.gameObject.GetComponent<PlayerAgent>();
                this.interact = this.playerAgent.gameObject.AddComponent<Interact_ManualTimedWithCallback>();
                this.interact.OnTrigger = (Il2CppSystem.Action)this.OnTrigger;
                this.interact.OnInteractionEvaluationAbort = (Il2CppSystem.Action)this.OnAbort;
                this.interact.AbortOnDotOrDistanceDiff = true;
                this.interact.OnlyActiveWhenLookingStraightAt = true;
                this.interact.InteractDuration = 0.6f;
                this.UpdateInteractionMessage("Blah");
                this.interact.SetActive(true);
            }

            private void OnAbort()
            {
                Instance!.LogDebug($"{nameof(PlaceholderInteractionMonitor)}.{nameof(OnAbort)}()");
            }

            private void OnTrigger()
            {
                Instance!.LogDebug($"{nameof(PlaceholderInteractionMonitor)}.{nameof(OnTrigger)}()");

                if (this.currentPlaceholder == null)
                {
                    Instance.LogError("currentPlaceholder is null?");
                    return;
                }

                switch (this.playerAgent!.Inventory?.WieldedSlot)
                {
                    case InventorySlot.ResourcePack:
                    case InventorySlot.Consumable:
                        break;
                    case var slotType:
                        Instance.LogError($"Player is holding an invalid item: {slotType}");
                        return;
                }

                var wieldedItem = this.playerAgent.Inventory.WieldedItem;
                if (wieldedItem == null)
                {
                    Instance.LogError("Wielded item is null?");
                    return;
                }

                var item = GetLevelItemFromItemData<ItemInLevel>(wieldedItem.Get_pItemData());
                if (item == null)
                {
                    Instance.LogError($"Failed to resolve {nameof(ItemInLevel)} from {nameof(ItemEquippable)}");
                    return;
                }

                var sync = item.GetSyncComponent();
                if (sync == null)
                {
                    Instance.LogError("PickupItem sync component is null?");
                    return;
                }

                if (this.currentPlaceholder.StorageSlot == null)
                {
                    Instance!.LogError($"Placeholder.StorageSlot is null");
                    return;
                }

                var inventorySlot = item.Get_pItemData().slot;
                Transform? transform = null;
                switch (inventorySlot)
                {
                    case InventorySlot.ResourcePack:
                        transform = this.currentPlaceholder.StorageSlot.ResourcePack;
                        break;
                    case InventorySlot.Consumable:
                        transform = this.currentPlaceholder.StorageSlot.Consumable;
                        break;
                    case var slotType:
                        //Instance.LogWarning($"InventorySlot: {slotType}");
                        return;
                }

                this.currentPlaceholder.Enabled = false;

                var backpack = PlayerBackpackManager.GetLocalOrSyncBackpack();
                var slotAmmo = backpack.AmmoStorage.GetInventorySlotAmmo(inventorySlot);

                var customData = wieldedItem.GetCustomData();
                customData.ammo = slotAmmo.AmmoInPack;

                var courseNode = this.playerAgent.CourseNode;

                sync.AttemptPickupInteraction(
                    type: ePickupItemInteractionType.Place,
                    player: SNet.LocalPlayer,
                    custom: customData,
                    position: transform.position,
                    rotation: transform.rotation,
                    node: courseNode,
                    droppedOnFloor: true);
            }

            private static T? GetLevelItemFromItemData<T>(pItemData itemData) where T : ItemInLevel
            {
                if (!PlayerBackpackManager.TryGetItemInLevelFromItemData(itemData, out var levelItem)) return null;

                if (levelItem.TryCast<ItemInLevel>() == null)
                {
                    Instance!.LogError($"{nameof(PlayerBackpackManager.TryGetItemInLevelFromItemData)} returned an invalid item type!?");
                    return null;
                }

                return levelItem.TryCast<T>();
            }

            [HideFromIl2Cpp]
            private void UpdateInteractionMessage(string message) =>
                this.interact!.SetAction(message, InputAction.Use);

            private void Update()
            {
                //if (WorldInteractionBlockerPatch.IgnoreWorldInteractions > 0) return;
                if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;

                if (this.playerAgent == null)
                {
                    Instance!.LogError("playerAgent is null?");
                    return;
                }

#if DEBUG_PLACEHOLDERS
                StorageSlotManager.Update();
#endif

                var raycast = this.GetPlaceholderInSight(
                    playerAgent: this.playerAgent,
                    comp: out var placeholder,
                    hitPos: out var hitPos,
                    maxDistance: 1.5f,
                    layerMask: LayerManager.MASK_PLAYER_INTERACT_SPHERE);

                var wieldedItem = this.playerAgent.Inventory?.WieldedItem;
                var wieldedSlot = this.playerAgent.Inventory?.WieldedSlot;
                var isDroppable = wieldedSlot switch
                {
                    InventorySlot.ResourcePack => true,
                    InventorySlot.Consumable => true,
                    _ => false,
                };

                if (!raycast || placeholder == null || !placeholder.Enabled || !isDroppable)
                {
                    if (this.currentPlaceholder != null)
                    {
                        DisableInteractions = false;
                        if (this.ghostGO != null)
                        {
                            Destroy(this.ghostGO);
                            this.ghostGO = null;
                        }
                        // Clear the placeholder interaction
                        this.interact!.PlayerSetSelected(false, this.playerAgent);
                        this.UpdateInteractionMessage("");
                        this.currentPlaceholder = null;
                    }
                    return;
                }

                var placeholderChanged = false;

                if (this.currentPlaceholder?.GetInstanceID() != placeholder.GetInstanceID())
                {
                    Instance!.LogDebug("Changing placeholder");
#if DEBUG_PLACEHOLDERS
                    var wireframe = placeholder.GetComponent<RectangleWireframe>();
                    if (wireframe != null)
                    {
                        Instance.LogDebug($"Default placeholder position: {wireframe.DefaultPosition.ToString()}");
                    }
#endif
                    placeholderChanged = true;
                    this.currentPlaceholder = placeholder;
                    DisableInteractions = true;
                    // Disable the previous interaction
                    this.playerAgent.Interaction.UnSelectCurrentBestInteraction();
                    // This causes PlayerInteraction.HasWorldInteraction to
                    // evaluate to true. This is used to trick other
                    // interactions into thinking that the user already has an
                    // interaction prompt.
                    this.playerAgent.Interaction.m_bestSelectedInteract = this.interact!.Cast<IInteractable>();
                    this.playerAgent.Interaction.m_bestInteractInCurrentSearch = this.interact;
                }

                var wieldedItemChanged = false;

                if (this.currentWieldedItem?.GetInstanceID() != wieldedItem?.GetInstanceID())
                {
                    this.currentWieldedItem = wieldedItem;
                    wieldedItemChanged = true;
                }

                if (placeholderChanged || wieldedItemChanged)
                {
                    if (this.ghostGO != null)
                    {
                        Destroy(this.ghostGO);
                        this.ghostGO = null;
                    }

                    if (wieldedItem == null)
                    {
                        Instance!.LogError("wieldedItem is null?");
                        return;
                    }

                    if (this.currentPlaceholder == null)
                    {
                        Instance!.LogError("currentPlaceholder is null?");
                        return;
                    }

                    if (this.currentPlaceholder.StorageSlot == null)
                    {
                        Instance!.LogError("Placeholder.StorageSlot is null?");
                        return;
                    }

                    this.UpdateInteractionMessage($"Drop {wieldedItem.ItemDataBlock.publicName ?? "item"}");

                    var prefabParts = ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Pickup][wieldedItem.ItemDataBlock.persistentID];
                    var transform = wieldedSlot switch
                    {
                        InventorySlot.ResourcePack => this.currentPlaceholder.StorageSlot.ResourcePack,
                        InventorySlot.Consumable => this.currentPlaceholder.StorageSlot.Consumable,
                        _ => this.currentPlaceholder!.StorageSlot.Consumable,
                    };

                    this.ghostGO = Instantiate(prefabParts[0], transform.position, transform.rotation);
                    foreach (var renderer in this.ghostGO.GetComponentsInChildren<Renderer>())
                    {
                        foreach (var material in renderer.materials)
                        {
                            material.shader = Shader.Find("Transparent/Diffuse");
                            material.color = Color.black.AlphaMultiplied(0.25f);
                        }
                    }
                }

                this.interact.PlayerSetSelected(true, this.playerAgent);
                this.interact.ManualUpdateWithCondition(true, this.playerAgent, true);
            }

            [HideFromIl2Cpp]
            private bool GetPlaceholderInSight(PlayerAgent playerAgent, out StorageSlotPlaceholder? comp, out Vector3 hitPos, float maxDistance, int layerMask)
            {
                var origin = playerAgent.CamPos;
                var direction = playerAgent.FPSCamera.Forward;

                comp = null;
                hitPos = default;

                var hits = Physics.RaycastAll(origin, direction, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
                var hit = hits
                    .Select(x => new
                    {
                        RaycastHit = x,
                        Placeholder = x.collider.gameObject.GetComponent<StorageSlotPlaceholder>(),
                    })
                    .FirstOrDefault(x => x.Placeholder != null);

                if (hit == null) return false;
                hitPos = hit.RaycastHit.point;
                comp = hit.Placeholder;

                return true;
            }
        }
    }
}
