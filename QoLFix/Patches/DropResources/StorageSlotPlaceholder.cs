using System;
using System.Collections.Generic;
using LevelGeneration;
using UnityEngine;

namespace QoLFix.Patches
{
    public partial class DropResourcesPatch
    {
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
    }
}
