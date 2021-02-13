using System.Linq;
using UnityEngine;

#if DEBUG_PLACEHOLDERS
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnhollowerBaseLib;
using QoLFix.Debugging;
#endif

namespace QoLFix.Patches
{
    public partial class DropResourcesPatch
    {
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
    }
}
