using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using GameData;
using Gear;
using HarmonyLib;
using LevelGeneration;
using MTFO.Core;
using Player;
using QoL.Common;
using SNetwork;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace QoL.BetterLockers
{
    public partial class DropResourcesPatch : MTFOPatch
    {
        private const string PatchName = nameof(DropResourcesPatch);

        private static ConfigEntry<bool> ConfigEnabled = default!;
        private static ConfigEntry<bool> ConfigDropResourcesEnabled = default!;
        private static ConfigEntry<bool> ConfigPingBugFixEnabled = default!;

        public static DropResourcesPatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Controls whether to enable this patch."));
            ConfigPingBugFixEnabled = this.Plugin.Config.Bind(new(PatchName, "PingBugFix"), true,
                new ConfigDescription("Fixes resources not being pingable in containers."));
            ConfigDropResourcesEnabled = this.Plugin.Config.Bind(new(PatchName, "DropResources"), true,
                new ConfigDescription("Lets you put resources back in containers."));

            ClassInjector.RegisterTypeInIl2Cpp<PlaceholderInteractionMonitor>();
            ClassInjector.RegisterTypeInIl2Cpp<StorageSlotPlaceholder>();
#if DEBUG_PLACEHOLDERS
            ClassInjector.RegisterTypeInIl2Cpp<RectangleWireframe>();
#endif
        }

        [HarmonyPatch(typeof(LocalPlayerAgentSettings))]
        [HarmonyPatch(nameof(LocalPlayerAgentSettings.OnLocalPlayerAgentEnable))]
        [HarmonyPostfix]
        private static void LocalPlayerAgentSettings__OnLocalPlayerAgentEnable__Postfix()
        {
            var playerAgent = SNet.LocalPlayer.PlayerAgent?.Cast<PlayerAgent>();
            if (playerAgent == null) return;
            playerAgent.gameObject.AddComponent<PlaceholderInteractionMonitor>();
        }

        // If interactions are disabled, skip the original function to
        // prevent it from interfering with our placeholder interaction.
        [HarmonyPatch(typeof(PlayerInteraction))]
        [HarmonyPatch(nameof(PlayerInteraction.UpdateWorldInteractions))]
        [HarmonyPrefix]
        private static bool PlayerInteraction__UpdateWorldInteractions__Prefix() =>
            PlaceholderInteractionMonitor.DisableInteractions
                ? HarmonyControlFlow.DontExecute
                : HarmonyControlFlow.Execute;

        [HarmonyPatch(typeof(LG_PickupItem_Sync))]
        [HarmonyPatch(nameof(LG_PickupItem_Sync.OnStateChange))]
        [HarmonyPostfix]
        private static void LG_PickupItem_Sync__OnStateChange__Postfix(LG_PickupItem_Sync __instance)
        {
            var container = __instance.GetComponentInParent<LG_WeakResourceContainer>();
            if (container == null)
            {
                Instance!.LogDebug("Item was placed outside of a container");
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

        [HarmonyPatch(typeof(LG_ResourceContainer_Storage))]
        [HarmonyPatch(nameof(LG_ResourceContainer_Storage.EnablePickupInteractions))]
        [HarmonyPostfix]
        private static void LG_ResourceContainer_Storage__EnablePickupInteractions__Postfix(LG_ResourceContainer_Storage __instance)
        {
            var container = __instance.m_core.TryCast<LG_WeakResourceContainer>();
            if (container == null)
            {
                Instance!.LogError($"Container isn't a {nameof(LG_WeakResourceContainer)}!?");
                return;
            }

            UpdatePlaceholders(container);
        }

        [HarmonyPatch(typeof(LG_ResourceContainer_Storage))]
        [HarmonyPatch(nameof(LG_ResourceContainer_Storage.DisablePickupInteractions))]
        [HarmonyPostfix]
        private static void LG_ResourceContainer_Storage__DisablePickupInteractions__Postfix(LG_ResourceContainer_Storage __instance)
        {
            var container = __instance.m_core.TryCast<LG_WeakResourceContainer>();
            if (container == null)
            {
                Instance!.LogError($"Container isn't a {nameof(LG_WeakResourceContainer)}!?");
                return;
            }

            var placeholders = container.GetComponentsInChildren<StorageSlotPlaceholder>();
            foreach (var placeholder in placeholders)
            {
                placeholder.Enabled = false;
                placeholder.SetLockerPingIcon();
            }
        }

        [HarmonyPatch(typeof(LG_ResourceContainerBuilder))]
        [HarmonyPatch(nameof(LG_ResourceContainerBuilder.SetupFunctionGO))]
        [HarmonyPostfix]
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
                    Instance!.LogError($"{nameof(LG_WeakResourceContainer)} storage is null!?");
                }
                else
                {
                    Instance!.LogError($"Unknown {nameof(LG_WeakResourceContainer)} storage type: {comp.m_storage.GetType()}");
                }
                return;
            }

#if DEBUG_PLACEHOLDERS
            var wireframes = new RectangleWireframe[storage.m_storageSlots.Length];
            List<RectangleWireframe[]>? wireframeList = null;
#endif

            Func<int, StorageSlotManager.SlotSettings?> getSlotSettings;
            StorageType storageType;

            switch (comp.gameObject.name)
            {
                case var s when s.StartsWith("BoxWeakLock"):
#if DEBUG_PLACEHOLDERS
                    wireframeList = StorageSlotManager.BoxWireframes;
#endif
                    getSlotSettings = (index) => StorageSlotManager.GetBoxSlotSettings(index);
                    storageType = StorageType.Box;
                    break;
                case var s when s.StartsWith("LockerWeakLock"):
#if DEBUG_PLACEHOLDERS
                    wireframeList = StorageSlotManager.LockerWireframes;
#endif
                    getSlotSettings = (index) => StorageSlotManager.GetLockerSlotSettings(index);
                    storageType = StorageType.Locker;
                    break;
                case var s:
                    Instance!.LogError($"Unknown ${nameof(LG_WeakResourceContainer)} type: {s}");
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
                    out StorageSlotPlaceholder placeholder,
                    out PlayerPingTarget pingTarget);

                slotGO.transform.localPosition = (slotSettings?.Offset - (size / 2f)) ?? defaultPos;
                slotGO.transform.localRotation = slotSettings?.Rotation ?? Quaternion.identity;
                slotGO.layer = LayerManager.LAYER_INTERACTION;

                collider.center = Vector3.zero;
                collider.size = size;

                placeholder.StorageSlot = slot;
                placeholder.StorageType = storageType;
                placeholder.PingTarget = pingTarget;
                placeholder.SetLockerPingIcon();
                // Placeholders get enabled when the container is opened (in EnablePickupInteractions)
                placeholder.Enabled = false;

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

        private static readonly IEqualityComparer<PlaceholderCandidate> PlaceholderComparer =
            ProjectionEqualityComparer.Create<PlaceholderCandidate, int>(x => x.Interact.GetInstanceID());

        private class PlaceholderCandidate
        {
            public PlaceholderCandidate(Interact_Pickup_PickupItem interact, StorageSlotPlaceholder placeholder)
            {
                this.Interact = interact;
                this.Sync = interact.GetComponentInParent<LG_PickupItem_Sync>();
                this.Distance = Vector3.Distance(this.Sync.transform.position, placeholder.transform.position);
            }

            public Interact_Pickup_PickupItem Interact { get; set; }

            public float Distance { get; set; }

            public bool Claimed { get; set; }

            public LG_PickupItem_Sync Sync { get; internal set; }
        }

        private static void UpdatePlaceholders(LG_WeakResourceContainer container)
        {
            var interacts = container.gameObject.GetComponentsInChildren<Interact_Pickup_PickupItem>();

            var placeholders = container.GetComponentsInChildren<StorageSlotPlaceholder>()
                .Select(x => (
                    Collider: x.GetComponent<BoxCollider>(),
                    Placeholder: x
                ))
                // Generate all combinations of interactions with placeholders
                .Select(p =>
                {
                    var candidates = interacts
                        .Where(interact => p.Collider.ContainsPoint(interact.transform.position))
                        .Select(interact => new PlaceholderCandidate(interact, p.Placeholder))
                        .ToArray();

                    PlaceholderCandidate? getBestCandidate() => candidates
                        .Where(x => !x.Claimed)
                        .OrderBy(x => x.Distance)
                        .FirstOrDefault();

                    return (
                        Placeholder: p.Placeholder,
                        Candidates: candidates,
                        GetBestCandidate: (Func<PlaceholderCandidate?>)getBestCandidate
                    );
                })
                .OrderBy(x => x.GetBestCandidate()?.Distance ?? float.MaxValue)
                .ToArray();

            for (var i = 0; i < placeholders.Length; i++)
            {
                var p = placeholders[i];
                var c = p.GetBestCandidate();

                var root = c?.Interact.GetComponentInParent<LG_PickupItem>()?.Cast<Component>() ?? c?.Sync;

                if (root?.gameObject.active == true && c?.Sync?.gameObject.active == true)
                {
                    Instance!.LogDebug($"PickupItem Sync: {c.Sync.name}");
                    Instance!.LogDebug($"PickupItem Root: {root.name}");
                    c!.Claimed = true;

                    Instance.LogDebug($"Disabling placeholder[{i}]");
                    p.Placeholder.Enabled = false;
                    p.Placeholder.SetLockerPingIcon();

                    if (p.Placeholder.StorageSlot == null)
                    {
                        Instance!.LogError($"Placeholder[{i}].StorageSlot is null?");
                        continue;
                    }

                    Transform? transform = null;
                    var item = c.Sync.item;
                    if (item.Is<ResourcePackPickup>())
                    {
                        transform = p.Placeholder.StorageSlot.ResourcePack;
                        var itemData = item.Get_pItemData();
                        p.Placeholder.SetPingIcon(itemData.itemID_gearCRC);
                    }
                    else if (item.Is<ConsumablePickup_Core>())
                    {
                        transform = p.Placeholder.StorageSlot.Consumable;
                    }
                    else if (item.Is<ArtifactPickup_Core>())
                    {
                        transform = p.Placeholder.StorageSlot.Consumable;
                        p.Placeholder.SetPingIcon(eNavMarkerStyle.PlayerPingLoot);
                    }
                    // Objective items
                    // else if (item.Is<GenericSmallPickupItem_Core>()) { }
                    // Door keys
                    // else if (item.Is<KeyItemPickup_Core>()) { }
                    // Carry items (e.g. cryo cases, fog turbines, cells, etc)
                    else if (item.Is<CarryItemPickup_Core>())
                    {
                        Instance.LogWarning("Carry item spawned in a resource container!?");
                    }

                    if (transform != null)
                    {
                        root.transform.SetParent(p.Placeholder.transform);
                        root.transform.SetPositionAndRotation(transform.position, transform.rotation);
                        root.gameObject.layer = LayerManager.LAYER_INTERACTION;
                    }
                }
                else
                {
                    Instance!.LogDebug($"Enabling placeholder[{i}]");
                    p.Placeholder.Enabled = true;
                    p.Placeholder.SetLockerPingIcon();
                }
            }
        }
    }
}
