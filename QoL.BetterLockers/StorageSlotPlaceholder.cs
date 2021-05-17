using System;
using System.Collections.Generic;
using LevelGeneration;
using UnhollowerBaseLib.Attributes;
using UnityEngine;

namespace QoL.BetterLockers
{
    partial class DropResourcesPatch
    {
        private class StorageSlotPlaceholder : MonoBehaviour
        {
            private class Instance
            {
                public StorageSlot? StorageSlot;
                public PlayerPingTarget? PingTarget;
                public StorageType StorageType;
                public bool Enabled;
            }

            // This workaround is necessary to prevent the StorageSlot from getting
            // garbage-collected in the IL2CPP domain.
            // XXX: for some reason unhollower doesn't restore instance
            // properties, so we have to store them elsewhere in mono land.
            private static readonly Dictionary<int, Instance> Instances = new();

            private Instance? instance;

            [HideFromIl2Cpp]
            private Instance GetInstance()
            {
                if (this.instance != null) return this.instance;
                var instanceId = this.GetInstanceID();
                if (!Instances.TryGetValue(instanceId, out var instance))
                {
                    instance = Instances[instanceId] = new();
                }
                this.instance = instance;
                return this.instance;
            }

            public StorageSlotPlaceholder(IntPtr pointer)
                : base(pointer) { }

            [HideFromIl2Cpp]
            public bool Enabled
            {
                get => ConfigDropResourcesEnabled.Value && this.GetInstance().Enabled;
                set => this.GetInstance().Enabled = value;
            }

            [HideFromIl2Cpp]
            public StorageSlot? StorageSlot
            {
                get => this.GetInstance().StorageSlot;
                set => this.GetInstance().StorageSlot = value;
            }

            [HideFromIl2Cpp]
            public StorageType StorageType
            {
                get => this.GetInstance().StorageType;
                set => this.GetInstance().StorageType = value;
            }

            [HideFromIl2Cpp]
            public PlayerPingTarget? PingTarget
            {
                get => this.GetInstance().PingTarget;
                set => this.GetInstance().PingTarget = value;
            }

            [HideFromIl2Cpp]
            public void SetLockerPingIcon()
            {
                if (this.PingTarget == null) return;
                switch (this.StorageType)
                {
                    case StorageType.Box:
                        this.PingTarget.m_pingTargetStyle = eNavMarkerStyle.PlayerPingResourceBox;
                        break;
                    case StorageType.Locker:
                        this.PingTarget.m_pingTargetStyle = eNavMarkerStyle.PlayerPingResourceLocker;
                        break;
                }
            }

            [HideFromIl2Cpp]
            public void SetPingIcon(eNavMarkerStyle icon)
            {
                if (this.PingTarget == null) return;
                if (!ConfigPingBugFixEnabled.Value)
                {
                    this.SetLockerPingIcon();
                    return;
                }

                this.PingTarget.m_pingTargetStyle = icon;
            }

            [HideFromIl2Cpp]
            public void SetPingIcon(uint itemId)
            {
                if (this.PingTarget == null) return;
                if (!ConfigPingBugFixEnabled.Value)
                {
                    this.SetLockerPingIcon();
                    return;
                }

                switch (itemId)
                {
                    // Health
                    case 102:
                        this.PingTarget.m_pingTargetStyle = eNavMarkerStyle.PlayerPingHealth;
                        break;
                    // Ammo
                    case 101:
                        this.PingTarget.m_pingTargetStyle = eNavMarkerStyle.PlayerPingAmmo;
                        break;
                    // Tool refill
                    case 127:
                        this.PingTarget.m_pingTargetStyle = eNavMarkerStyle.PlayerPingAmmo;
                        break;
                    // Disinfection
                    case 132:
                        this.PingTarget.m_pingTargetStyle = eNavMarkerStyle.PlayerPingDisinfection;
                        break;
                }
            }
        }
    }
}
