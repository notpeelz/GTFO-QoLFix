using System;
using System.Collections.Generic;
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
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnhollowerBaseLib;
using QoLFix.Debug;
#endif

namespace QoLFix.Patches
{
    public class DropResourcesPatch : IPatch
    {
        private static readonly string PatchName = nameof(DropResourcesPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Lets you put resources/consumables back in lockers/boxes."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public void Patch(Harmony harmony)
        {
            ClassInjector.RegisterTypeInIl2Cpp<PlaceholderInteractionMonitor>();
            ClassInjector.RegisterTypeInIl2Cpp<StorageSlotPlaceholder>();
#if DEBUG_PLACEHOLDERS
            ClassInjector.RegisterTypeInIl2Cpp<RectangleWireframe>();
#endif

            {
                var methodInfo = typeof(LocalPlayerAgentSettings).GetMethod(nameof(LocalPlayerAgentSettings.OnLocalPlayerAgentEnable));
                harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(DropResourcesPatch), nameof(LocalPlayerAgentSettings__OnLocalPlayerAgentEnable))));
            }
            {
                var methodInfo = typeof(LG_ResourceContainer_Storage).GetMethod(nameof(LG_ResourceContainer_Storage.EnablePickupInteractions));
                harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(DropResourcesPatch), nameof(LG_ResourceContainer_Storage__EnablePickupInteractions))));
            }
            {
                var methodInfo = typeof(LG_ResourceContainer_Storage).GetMethod(nameof(LG_ResourceContainer_Storage.DisablePickupInteractions));
                harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(DropResourcesPatch), nameof(LG_ResourceContainer_Storage__DisablePickupInteractions))));
            }
            {
                var methodInfo = typeof(LG_PickupItem_Sync).GetMethod(nameof(LG_PickupItem_Sync.OnStateChange));
                harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(DropResourcesPatch), nameof(LG_PickupItem_Sync__OnStateChange))));
            }
            {
                var methodInfo = typeof(LG_ResourceContainerBuilder).GetMethod(nameof(LG_ResourceContainerBuilder.SetupFunctionGO));
                harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(DropResourcesPatch), nameof(LG_ResourceContainerBuilder__SetupFunctionGO))));
            }
            {
                var methodInfo = typeof(PlayerInteraction).GetMethod(nameof(PlayerInteraction.UpdateWorldInteractions));
                harmony.Patch(methodInfo, prefix: new HarmonyMethod(AccessTools.Method(typeof(DropResourcesPatch), nameof(PlayerInteraction__UpdateWorldInteractions))));
            }
        }

        private static void LocalPlayerAgentSettings__OnLocalPlayerAgentEnable()
        {
            var playerAgent = SNet.LocalPlayer.PlayerAgent?.Cast<PlayerAgent>();
            if (playerAgent == null) return;
            playerAgent.gameObject.AddComponent<PlaceholderInteractionMonitor>();
        }

        private static bool PlayerInteraction__UpdateWorldInteractions(PlayerInteraction __instance)
        {
            // If interactions are disabled, skip the normal function to
            // prevent it from interfering with our placeholder interaction.
            if (PlaceholderInteractionMonitor.DisableInteractions) return false;
            return true;
        }

        private static void LG_PickupItem_Sync__OnStateChange(LG_PickupItem_Sync __instance, pPickupItemState newState)
        {
            var container = __instance.GetComponentInParent<LG_WeakResourceContainer>();
            if (container == null)
            {
                Instance.LogDebug("Item was placed outside of a container");
                return;
            }

            // If an item is picked up or dropped in a container, update all
            // the placeholders.
            UpdatePlaceholders(container);
        }

        private static void LG_ResourceContainer_Storage__EnablePickupInteractions(LG_ResourceContainer_Storage __instance)
        {
            var container = __instance.m_core.TryCast<LG_WeakResourceContainer>();
            if (container == null)
            {
                Instance.LogError($"Container isn't a {nameof(LG_WeakResourceContainer)}!?");
                return;
            }

            UpdatePlaceholders(container);
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

        private static void LG_ResourceContainer_Storage__DisablePickupInteractions(LG_ResourceContainer_Storage __instance)
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

        private static void LG_ResourceContainerBuilder__SetupFunctionGO(LG_ResourceContainerBuilder __instance, GameObject GO)
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

                var slotGO = new GameObject($"storageSlot{i}");
                slotGO.transform.SetParent(comp.transform, false);
                slotGO.transform.localPosition = (slotSettings?.Offset - size / 2f) ?? defaultPos;
                slotGO.transform.localRotation = slotSettings?.Rotation ?? Quaternion.identity;
                slotGO.layer = LayerManager.LAYER_INTERACTION;

                var collider = slotGO.AddComponent<BoxCollider>();
                collider.center = Vector3.zero;
                collider.size = size;

                var placeholder = slotGO.AddComponent<StorageSlotPlaceholder>();
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

        public static class StorageSlotManager
        {
            public class SlotSettings
            {
                public Vector3 Size { get; set; }

                public Vector3? Offset { get; set; }

                public Quaternion? Rotation { get; set; }
            }

#if DEBUG_PLACEHOLDERS
            private const string CONFIG_FILE = QoLFixPlugin.GUID + ".wireframes.json";

            private static bool SettingsLoaded;

            private static bool AttemptedLoad;

            private static volatile bool ShouldUpdateSettings;

            private static FileSystemWatcher Watcher;

            public static List<RectangleWireframe[]> BoxWireframes { get; } = new List<RectangleWireframe[]>();

            public static List<RectangleWireframe[]> LockerWireframes { get; } = new List<RectangleWireframe[]>();

            private static SlotSettings[] BoxSlots;

            private static SlotSettings[] LockerSlots;

            internal static void Update()
            {
                if (!ShouldUpdateSettings) return;
                ShouldUpdateSettings = false;
                if (LoadSettings())
                {
                    Instance.LogInfo("Updating box wireframe info!");
                    UpdateWireframes(BoxWireframes, GetBoxSlotSettings);
                    Instance.LogInfo("Updating locker wireframe info!");
                    UpdateWireframes(LockerWireframes, GetLockerSlotSettings);
                    Instance.LogInfo("Done!");
                }
            }

            private static void EnsureSettingsLoaded()
            {
                if (SettingsLoaded) return;
                if (AttemptedLoad) return;
                LoadSettings();
            }

            private static void EnsureWatchInitialized()
            {
                if (Watcher != null) return;
                Watcher = new FileSystemWatcher(BepInEx.Paths.ConfigPath);
                Watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
                Watcher.Changed += OnWatcherEvent;
                Watcher.Created += OnWatcherEvent;
                Watcher.EnableRaisingEvents = true;
            }

            private static void OnWatcherEvent(object sender, FileSystemEventArgs e)
            {
                QoLFixPlugin.Instance.Log.LogDebug($"WATCH EVENT: {e.ChangeType} - {e.Name}");
                if (e.ChangeType != WatcherChangeTypes.Changed && e.ChangeType != WatcherChangeTypes.Created) return;
                if (Path.GetFileName(e.Name) != CONFIG_FILE) return;
                ShouldUpdateSettings = true;
            }

            private static void UpdateWireframes(List<RectangleWireframe[]> wireframes, Func<int, SlotSettings> getSlotSettingsFunc)
            {
                for (var i = wireframes.Count - 1; i >= 0; i--)
                {
                    var container = wireframes[i];
                    try
                    {
                        for (var j = 0; j < container.Length; j++)
                        {
                            var wireframe = container[j];
                            if (wireframe.Pointer == IntPtr.Zero) continue; // this should be enough to trigger ObjectCollectedException
                            var settings = getSlotSettingsFunc(j);

                            var collider = wireframe.GetComponent<BoxCollider>();
                            collider.size = settings?.Size ?? new Vector3(0.1f, 0.1f, 0.1f);

                            wireframe.Size = collider.size;
                            wireframe.transform.localPosition = (settings?.Offset - collider.size / 2f) ?? wireframe.DefaultPosition;
                            wireframe.transform.localRotation = settings?.Rotation ?? Quaternion.identity;
                        }
                    }
                    catch (ObjectCollectedException)
                    {
                        QoLFixPlugin.Instance.Log.LogWarning($"Container {i} was garbage-collected");
                        wireframes.RemoveAt(i);
                    }
                }
            }

            private static bool LoadSettings()
            {
                AttemptedLoad = true;
                EnsureWatchInitialized();
                try
                {
                    var jsonPath = Path.Combine(BepInEx.Paths.ConfigPath, CONFIG_FILE);
                    var serializer = new JsonSerializer
                    {
                        NullValueHandling = NullValueHandling.Include,
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                    };
                    serializer.Converters.Add(new Vector3JsonConverter());
                    serializer.Converters.Add(new QuaternionJsonConverter());
                    var obj = JObject.Parse(File.ReadAllText(jsonPath));
                    BoxSlots = obj["box"]?.ToObject<SlotSettings[]>(serializer);
                    LockerSlots = obj["locker"]?.ToObject<SlotSettings[]>(serializer);
                    SettingsLoaded = true;
                    return true;
                }
                catch (Exception ex)
                {
                    QoLFixPlugin.Instance.Log.LogError("Failed to deserialize wireframes config file: " + ex);
                    return false;
                }
            }
            public static SlotSettings GetBoxSlotSettings(int index)
            {
                EnsureSettingsLoaded();
                try
                {
                    return BoxSlots[index];
                }
                catch
                {
                    return null;
                }
            }

            public static SlotSettings GetLockerSlotSettings(int index)
            {
                EnsureSettingsLoaded();
                try
                {
                    return LockerSlots[index];
                }
                catch
                {
                    return null;
                }
            }
#else

            private static readonly SlotSettings[] BoxSettings = new[]
            {

                new SlotSettings
                {
                    Size = new Vector3(0.33f, 0.5f, 0.4f),
                    Offset = new Vector3(0.165f, 0.25f, 0.4f),
                    Rotation = new Quaternion(0, 0, 0, 1),
                },
                new SlotSettings
                {
                    Size = new Vector3(0.33f, 0.5f, 0.4f),
                    Offset = new Vector3(-0.165f, 0.25f, 0.4f),
                    Rotation = new Quaternion(0, 0, 0, 1),
                },
                new SlotSettings
                {
                    Size = new Vector3(0.33f, 0.5f, 0.4f),
                    Offset = new Vector3(0.495f, 0.25f, 0.4f),
                    Rotation = new Quaternion(0, 0, 0, 1),
                },
            };

            private static readonly SlotSettings[] LockerSettings = new[]
            {
                new SlotSettings
                {
                    Size = new Vector3(0.54f, 0.5f, 0.39f),
                    Offset = new Vector3(-0.45f, 0, 2f),
                    Rotation = new Quaternion(0, 0, 0, 1),
                },
                new SlotSettings
                {
                    Size = new Vector3(0.59f, 0.5f, 0.26f),
                    Offset = new Vector3(-0.4f, 0, 1.61f),
                    Rotation = new Quaternion(0, 0, 0, 1),
                },
                new SlotSettings
                {
                    Size = new Vector3(0.59f, 0.5f, 0.29f),
                    Offset = new Vector3(-0.4f, 0, 1.35f),
                    Rotation = new Quaternion(0, 0, 0, 1),
                },
                new SlotSettings
                {
                    Size = new Vector3(0.59f, 0.5f, 0.32f),
                    Offset = new Vector3(-0.4f, 0, 1.06f),
                    Rotation = new Quaternion(0, 0, 0, 1),
                },
                new SlotSettings
                {
                    Size = new Vector3(0.59f, 0.5f, 0.73f),
                    Offset = new Vector3(-0.4f, 0, 0.74f),
                    Rotation = new Quaternion(0, 0, 0, 1),
                },
                new SlotSettings
                {
                    Size = new Vector3(0.45f, 0.5f, 0.39f),
                    Offset = new Vector3(0, 0, 2f),
                    Rotation = new Quaternion(0, 0, 0, 1),
                }
            };

            public static SlotSettings GetBoxSlotSettings(int index) => BoxSettings.ElementAtOrDefault(index);

            public static SlotSettings GetLockerSlotSettings(int index) => LockerSettings.ElementAtOrDefault(index);
#endif
        }

        public class StorageSlotPlaceholder : MonoBehaviour
        {
            // This workaround is necessary because Unhollower doesn't expose fields/properties to IL2CPP
            private static readonly Dictionary<int, StorageSlot> StorageSlots = new Dictionary<int, StorageSlot>();

            public StorageSlotPlaceholder(IntPtr value)
                : base(value) { }

            public StorageSlot Slot
            {
                get => StorageSlots[this.GetInstanceID()];
                set => StorageSlots[this.GetInstanceID()] = value;
            }
        }

        public class PlaceholderInteractionMonitor : MonoBehaviour
        {
            public PlaceholderInteractionMonitor(IntPtr value)
                : base(value) { }

            private PlayerAgent playerAgent;
            private Interact_ManualTimedWithCallback interact;
            private StorageSlotPlaceholder currentPlaceholder;
            private ItemEquippable currentWieldedItem;

            public static bool DisableInteractions { get; private set; }

            private void Awake()
            {
                Instance.LogDebug($"{nameof(PlaceholderInteractionMonitor)}.{nameof(Awake)}()");
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
                Instance.LogDebug($"{nameof(PlaceholderInteractionMonitor)}.{nameof(OnAbort)}()");
            }

            private void OnTrigger()
            {
                Instance.LogDebug($"{nameof(PlaceholderInteractionMonitor)}.{nameof(OnTrigger)}()");

                switch (playerAgent.Inventory?.WieldedSlot)
                {
                    case InventorySlot.ResourcePack:
                    case InventorySlot.Consumable:
                        break;
                    case var slotType:
                        Instance.LogError($"Player is holding an invalid item: {slotType}");
                        return;
                }

                var wieldedItem = playerAgent.Inventory.WieldedItem;
                if (wieldedItem == null)
                {
                    Instance.LogError($"Wielded item is null?");
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

                var inventorySlot = item.Get_pItemData().slot;
                Transform transform = null;
                switch (inventorySlot)
                {
                    case InventorySlot.ResourcePack:
                        transform = this.currentPlaceholder.Slot.ResourcePack;
                        break;
                    case InventorySlot.Consumable:
                        transform = this.currentPlaceholder.Slot.Consumable;
                        break;
                    case var slotType:
                        //Instance.LogWarning($"InventorySlot: {slotType}");
                        break;
                }

                currentPlaceholder.enabled = false;

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

            private static T GetLevelItemFromItemData<T>(pItemData itemData) where T : ItemInLevel
            {
                if (!PlayerBackpackManager.TryGetItemInLevelFromItemData(itemData, out var levelItem)) return null;

                if (levelItem.TryCast<ItemInLevel>() == null)
                {
                    Instance.LogError($"{nameof(PlayerBackpackManager.TryGetItemInLevelFromItemData)} returned an invalid item type!?");
                    return null;
                }

                return levelItem.TryCast<T>();
            }

            private void UpdateInteractionMessage(string message) =>
                this.interact.SetAction(message, InputAction.Use);

            private void Update()
            {
                if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;

#if DEBUG_PLACEHOLDERS
                StorageSlotManager.Update();
#endif

                var raycast = GTFOUtils.GetComponentInSight<StorageSlotPlaceholder>(
                    playerAgent: this.playerAgent,
                    comp: out var placeholder,
                    hitPos: out var hitPos,
                    maxDistance: 1.5f,
                    layerMask: LayerManager.MASK_PLAYER_INTERACT_SPHERE);

                var isDroppable = this.playerAgent.Inventory?.WieldedSlot switch
                {
                    InventorySlot.ResourcePack => true,
                    InventorySlot.Consumable => true,
                    _ => false,
                };

                if (!raycast || placeholder == null || !placeholder.enabled || !isDroppable)
                {
                    if (this.currentPlaceholder != null)
                    {
                        DisableInteractions = false;
                        // Clear the placeholder interaction
                        this.interact.PlayerSetSelected(false, this.playerAgent);
                        this.UpdateInteractionMessage("");
                        this.currentPlaceholder = null;
                    }
                    return;
                }

                var placeholderChanged = false;

                if (this.currentPlaceholder?.GetInstanceID() != placeholder.GetInstanceID())
                {
                    Instance.LogDebug("Changing placeholder");
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
                    this.playerAgent.Interaction.m_bestSelectedInteract = this.interact.Cast<IInteractable>();
                    this.playerAgent.Interaction.m_bestInteractInCurrentSearch = this.interact;
                }

                var wieldedItem = this.playerAgent.Inventory?.WieldedItem;
                var wieldedItemChanged = false;

                if (this.currentWieldedItem?.GetInstanceID() != wieldedItem?.GetInstanceID())
                {
                    this.currentWieldedItem = wieldedItem;
                    wieldedItemChanged = true;
                }

                if (placeholderChanged || wieldedItemChanged)
                {
                    this.UpdateInteractionMessage($"Drop {wieldedItem?.ItemDataBlock?.publicName ?? "item"}");
                }

                this.interact.PlayerSetSelected(true, this.playerAgent);
                this.interact.ManualUpdateWithCondition(true, this.playerAgent, true);
            }
        }
    }
}
