using System;
using System.Linq;
using LevelGeneration;
using MTFO.Core;
using UnityEngine;

namespace QoL.Common
{
    public static class GTFOUtils
    {
        public static T[] GetComponentsInParentAtPosition<T>(Vector3 position, int layerMask)
            where T : UnityEngine.Object =>
            GetComponentsAtPosition<T>(position, layerMask, x => x.GetComponentsInParent<T>());

        public static T[] GetComponentsInChildrenAtPosition<T>(Vector3 position, int layerMask)
            where T : UnityEngine.Object =>
            GetComponentsAtPosition<T>(position, layerMask, x => x.GetComponentsInChildren<T>());

        public static T[] GetComponentsAtPosition<T>(Vector3 position, int layerMask, Func<Component, T[]>? getCompsFunc = null)
            where T : UnityEngine.Object
        {
            return Physics.OverlapSphere(position, float.Epsilon, layerMask)
                .Distinct(UnhollowerExtensions.InstanceIDComparer)
                .Cast<Component>()
                .SelectMany(x => getCompsFunc != null ? getCompsFunc(x) : x.GetComponents<T>())
                .Where(x => x != null)
                .ToArray();
        }

        public static LG_WeakResourceContainer? GetParentResourceContainer(Vector3 position)
        {
            var resourceContainers = GetComponentsInParentAtPosition<LG_WeakResourceContainer>(position, LayerManager.MASK_PING_TARGET)
                .Distinct(UnhollowerExtensions.InstanceIDComparer)
                .Cast<LG_WeakResourceContainer>()
                .ToArray();

            if (resourceContainers.Length == 0) return null;

            if (resourceContainers.Length > 1)
            {
                Plugin.LogError($"{nameof(ItemInLevel)} is inside of multiple resource containers!?");
            }

            return resourceContainers[0];
        }
    }
}
