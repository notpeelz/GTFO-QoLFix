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
            // This workaround is necessary to prevent the StorageSlot from getting
            // garbage-collected in the IL2CPP domain.
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
