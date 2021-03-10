using System;
using System.Linq;
using BepInEx.Configuration;
using GameData;
using HarmonyLib;
using LevelGeneration;
using Player;
using SNetwork;
using UnhollowerRuntimeLib;
using UnityEngine;

#if DEBUG_PLACEHOLDERS
using System.Collections.Generic;
using QoLFix.Debugging;
#endif

namespace QoLFix.Patches.Tweaks
{
    public partial class DropResourcesPatch : IPatch
    {
        private const string PatchName = nameof(DropResourcesPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Lets you put resources/consumables back in lockers/boxes."));

            ClassInjector.RegisterTypeInIl2Cpp<PlaceholderInteractionMonitor>();
            ClassInjector.RegisterTypeInIl2Cpp<StorageSlotPlaceholder>();
#if DEBUG_PLACEHOLDERS
            ClassInjector.RegisterTypeInIl2Cpp<RectangleWireframe>();
#endif
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<LocalPlayerAgentSettings>(nameof(LocalPlayerAgentSettings.OnLocalPlayerAgentEnable), PatchType.Postfix);
            this.PatchMethod<LG_ResourceContainer_Storage>(nameof(LG_ResourceContainer_Storage.EnablePickupInteractions), PatchType.Postfix);
            this.PatchMethod<LG_ResourceContainer_Storage>(nameof(LG_ResourceContainer_Storage.DisablePickupInteractions), PatchType.Postfix);
            this.PatchMethod<LG_PickupItem_Sync>(nameof(LG_PickupItem_Sync.OnStateChange), PatchType.Postfix);
            this.PatchMethod<LG_ResourceContainerBuilder>(nameof(LG_ResourceContainerBuilder.SetupFunctionGO), PatchType.Postfix);
            this.PatchMethod<PlayerInteraction>(nameof(PlayerInteraction.UpdateWorldInteractions), PatchType.Prefix);
        }

        private static void LocalPlayerAgentSettings__OnLocalPlayerAgentEnable__Postfix()
        {
            var playerAgent = SNet.LocalPlayer.PlayerAgent?.Cast<PlayerAgent>();
            if (playerAgent == null) return;
            playerAgent.gameObject.AddComponent<PlaceholderInteractionMonitor>();
        }

        // If interactions are disabled, skip the original function to
        // prevent it from interfering with our placeholder interaction.
        private static bool PlayerInteraction__UpdateWorldInteractions__Prefix() =>
            PlaceholderInteractionMonitor.DisableInteractions
                ? HarmonyControlFlow.DontExecute
                : HarmonyControlFlow.Execute;

        private static void LG_PickupItem_Sync__OnStateChange__Postfix(LG_PickupItem_Sync __instance)
        {
            var container = __instance.GetComponentInParent<LG_WeakResourceContainer>();
            if (container == null)
            {
                Instance.LogDebug("Item was placed outside of a container");
                return;
            }

            // We make sure that the container is open when an item gets
            // synced. This is because LG_PickupItem_Sync::OnStateChange
            // gets called when a player drops in mid-game.

            // XXX: hopefully the replicators are in the correct state at
            // this point.
            var replicator = container.m_sync.Cast<LG_ResourceContainer_Sync>().m_stateReplicator;
            if (replicator.State.status != eResourceContainerStatus.Open) return;

            // If an item is picked up or dropped in a container, update all
            // the placeholders.
            UpdatePlaceholders(container);
        }

        private static void LG_ResourceContainer_Storage__EnablePickupInteractions__Postfix(LG_ResourceContainer_Storage __instance)
        {
            var container = __instance.m_core.TryCast<LG_WeakResourceContainer>();
            if (container == null)
            {
                Instance.LogError($"Container isn't a {nameof(LG_WeakResourceContainer)}!?");
                return;
            }

            UpdatePlaceholders(container);
        }

        private static void LG_ResourceContainer_Storage__DisablePickupInteractions__Postfix(LG_ResourceContainer_Storage __instance)
        {
            var container = __instance.m_core.TryCast<LG_WeakResourceContainer>();
            if (container == null)
            {
                Instance.LogError($"Container isn't a {nameof(LG_WeakResourceContainer)}!?");
                return;
            }

            var placeholders = container.GetComponentsInChildren<StorageSlotPlaceholder>();
            foreach (var placeholder in placeholders)
            {
                placeholder.enabled = false;
            }
        }

        private static void LG_ResourceContainerBuilder__SetupFunctionGO__Postfix(LG_ResourceContainerBuilder __instance, GameObject GO)
        {
            if (__instance.m_function != ExpeditionFunction.ResourceContainerWeak) return;
            var comp = GO.GetComponentInChildren<LG_WeakResourceContainer>();
            if (comp == null) return;

            var storage = comp.m_storage?.TryCast<LG_ResourceContainer_Storage>();
            if (storage == null)
            {
                if (comp.m_storage == null)
                {
                    Instance.LogError($"{nameof(LG_WeakResourceContainer)} storage is null!?");
                }
                else
                {
                    Instance.LogError($"Unknown {nameof(LG_WeakResourceContainer)} storage type: {comp.m_storage.GetType()}");
                }
                return;
            }

#if DEBUG_PLACEHOLDERS
            var wireframes = new RectangleWireframe[storage.m_storageSlots.Length];
            List<RectangleWireframe[]> wireframeList = null;
#endif

            Func<int, StorageSlotManager.SlotSettings> getSlotSettings = null;

            switch (comp.gameObject.name)
            {
                case var s when s.StartsWith("BoxWeakLock"):
#if DEBUG_PLACEHOLDERS
                    wireframeList = StorageSlotManager.BoxWireframes;
#endif
                    getSlotSettings = (index) => StorageSlotManager.GetBoxSlotSettings(index);
                    break;
                case var s when s.StartsWith("LockerWeakLock"):
#if DEBUG_PLACEHOLDERS
                    wireframeList = StorageSlotManager.LockerWireframes;
#endif
                    getSlotSettings = (index) => StorageSlotManager.GetLockerSlotSettings(index);
                    break;
                case var s:
                    Instance.LogError($"Unknown ${nameof(LG_WeakResourceContainer)} type: {s}");
                    return;
            }

            for (var i = 0; i < storage.m_storageSlots.Count; i++)
            {
                var slot = storage.m_storageSlots[i];
                var slotSettings = getSlotSettings(i);

                var defaultPos = slot.Consumable.parent.localPosition + slot.Consumable.localPosition;
                var size = slotSettings?.Size ?? new Vector3(0.1f, 0.1f, 0.1f);
                var slotGO = GOFactory.CreateObject($"storageSlot{i}", comp.transform,
                    out BoxCollider collider,
                    out StorageSlotPlaceholder placeholder);

                slotGO.transform.localPosition = (slotSettings?.Offset - (size / 2f)) ?? defaultPos;
                slotGO.transform.localRotation = slotSettings?.Rotation ?? Quaternion.identity;
                slotGO.layer = LayerManager.LAYER_INTERACTION;

                collider.center = Vector3.zero;
                collider.size = size;

                // Placeholders get enabled when the container is opened (in EnablePickupInteractions)
                placeholder.enabled = false;
                placeholder.Slot = slot;

#if DEBUG_PLACEHOLDERS
                var wireframe = slotGO.AddComponent<RectangleWireframe>();
                wireframe.DefaultPosition = defaultPos;
                wireframe.Center = collider.center;
                wireframe.Size = collider.size;
                wireframes[i] = wireframe;
#endif
            }

#if DEBUG_PLACEHOLDERS
            wireframeList.Add(wireframes);
#endif
        }

        private static void UpdatePlaceholders(LG_WeakResourceContainer container)
        {
            var interacts = container.gameObject.GetComponentsInChildren<Interact_Pickup_PickupItem>();
            var placeholders = container.GetComponentsInChildren<StorageSlotPlaceholder>();

            for (var i = 0; i < placeholders.Count; i++)
            {
                var placeholder = placeholders[i];
                var placeholderCollider = placeholder.GetComponent<BoxCollider>();

                var items = interacts
                    .Where(x => placeholderCollider.ContainsPoint(x.transform.position))
                    .ToArray();
                var match = items
                    .Select(x =>
                    {
                        var sync = x.GetComponentInParent<LG_PickupItem_Sync>();
                        var root = x.GetComponentInParent<LG_PickupItem>()?.Cast<Component>() ?? sync;
                        return new
                        {
                            Root = root,
                            Sync = sync,
                        };
                    })
                    // When picking up an item, the "ItemInLevel" stays inside
                    // of the container but gets disabled.
                    .FirstOrDefault(x => x.Root?.gameObject.active == true && x.Sync?.gameObject.active == true);

                Instance.LogDebug($"PickupItem Sync: {match?.Sync.name}");
                Instance.LogDebug($"PickupItem Root: {match?.Root.name}");

                if (items.Length > 1)
                {
                    Instance.LogWarning($"Multiple items were hit in {placeholder.name}");
                }

                if (match != null)
                {
                    Instance.LogDebug($"Disabling placeholder[{i}]");
                    placeholder.enabled = false;

                    Transform transform = null;
                    switch (match.Sync.item.Get_pItemData().slot)
                    {
                        case InventorySlot.ResourcePack:
                            transform = placeholder.Slot.ResourcePack;
                            break;
                        case InventorySlot.Consumable:
                            transform = placeholder.Slot.Consumable;
                            break;
                        case var slotType:
                            //Instance.LogWarning($"InventorySlot: {slotType}");
                            break;
                    }

                    if (transform != null)
                    {
                        match.Root.transform.SetParent(placeholder.transform);
                        match.Root.transform.SetPositionAndRotation(transform.position, transform.rotation);
                        match.Root.gameObject.layer = LayerManager.LAYER_INTERACTION;
                    }
                }
                else
                {
                    Instance.LogDebug($"Enabling placeholder[{i}]");
                    placeholder.enabled = true;
                }
            }
        }
    }
}
