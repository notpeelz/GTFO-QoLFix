using System;
using System.Collections.Generic;
using LevelGeneration;
using UnityEngine;

namespace QoLFix.Patches.Tweaks
{
    public partial class DropResourcesPatch
    {
        public class StorageSlotPlaceholder : MonoBehaviour
        {
            // This workaround is necessary because Unhollower doesn't expose fields/properties to IL2CPP
            private static readonly Dictionary<int, StorageSlot> StorageSlots = new();

            public StorageSlotPlaceholder(IntPtr value)
                : base(value) { }

            public StorageSlot Slot
            {
                get => StorageSlots[this.GetInstanceID()];
                set => StorageSlots[this.GetInstanceID()] = value;
            }
        }
    }
}
