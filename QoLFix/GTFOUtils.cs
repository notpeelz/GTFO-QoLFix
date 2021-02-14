using System;
using System.Collections.Generic;
using System.Linq;
using LevelGeneration;
using Player;
using QoLFix.Patches;
using UnityEngine;

namespace QoLFix
{
    public static class GTFOUtils
    {
        private static readonly IEqualityComparer<UnityEngine.Object> InstanceIDComparer = ProjectionEqualityComparer.Create<UnityEngine.Object, int>(x => x.GetInstanceID());

        public static T[] GetComponentsInParentAtPosition<T>(Vector3 position, int? layerMask = null)
            where T : UnityEngine.Object =>
            GetComponentsAtPosition<T>(position, layerMask, x => x.GetComponentsInParent<T>());

        public static T[] GetComponentsInChildrenAtPosition<T>(Vector3 position, int? layerMask = null)
            where T : UnityEngine.Object =>
            GetComponentsAtPosition<T>(position, layerMask, x => x.GetComponentsInChildren<T>());

        public static T[] GetComponentsAtPosition<T>(Vector3 position, int? layerMask = null, Func<Component, T[]> getCompsFunc = null)
            where T : UnityEngine.Object
        {
            return Physics.OverlapSphere(position, float.Epsilon, layerMask ?? LayerManager.MASK_PING_TARGET)
                .Distinct(InstanceIDComparer)
                .Cast<Component>()
                .SelectMany(x => getCompsFunc != null ? getCompsFunc(x) : x.GetComponents<T>())
                .Where(x => x != null)
                .ToArray();
        }

        public static bool GetComponentInSight<T>(
            PlayerAgent playerAgent,
            out T comp,
            out Vector3 hitPos,
            float maxDistance,
            int layerMask,
            Func<T, bool> predicate = null,
            bool debug = false) =>
            GetComponentInSight(playerAgent.CamPos, playerAgent.FPSCamera.Forward, out comp, out hitPos, maxDistance, layerMask, predicate, debug);

        public static bool GetComponentInSight<T>(
            Vector3 origin,
            Vector3 direction,
            out T comp,
            out Vector3 hitPos,
            float maxDistance,
            int layerMask,
            Func<T, bool> predicate = null,
            bool debug = false)
        {
            if (!Physics.Raycast(origin, direction, out var hitInfo, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
            {
                comp = default;
                hitPos = default;
                return false;
            }

            comp = hitInfo.collider.gameObject.GetComponentInChildren<T>();
            hitPos = hitInfo.point;

            var resourceContainer = hitInfo.collider.GetComponentInParent<LG_WeakResourceContainer>();

            // Ignore placeholder hits unless we're specifically looking for
            // a placeholder.
            if (typeof(DropResourcesPatch.StorageSlotPlaceholder) != typeof(T))
            {
                var placeholder = hitInfo.collider.gameObject.GetComponent<DropResourcesPatch.StorageSlotPlaceholder>();
                if (resourceContainer != null && placeholder != null)
                {
                    comp = resourceContainer.m_graphics.Cast<Component>().GetComponentInChildren<T>();
                }
            }

            if (resourceContainer != null && !resourceContainer.m_intOpen.enabled)
            {
                var storageChildren = resourceContainer.m_storageComp.gameObject
                   .GetChildren()
                   .Where(x => x.GetComponentInChildren<T>() != null)
                   .ToHashSet();

                if (debug)
                {
                    foreach (var child in storageChildren)
                    {
                        QoLFixPlugin.LogDebug($"<{nameof(GetComponentInSight)}> StorageChild: {child.name}");
                    }
                }

                var hits = Physics.RaycastAll(origin, direction, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
                T childComp = default;
                foreach (var hit in hits)
                {
                    if (!storageChildren.Contains(hit.collider.gameObject, InstanceIDComparer)) continue;
                    childComp = hit.collider.gameObject.GetComponentInChildren<T>();
                    if (childComp == null) continue;
                    if (predicate != null && !predicate(childComp)) continue;

                    if (debug)
                    {
                        QoLFixPlugin.LogDebug($"<{nameof(GetComponentInSight)}> Selecting storage child as target");
                    }

                    hitPos = hit.point;
                    break;
                }

                if (childComp != null)
                {
                    comp = childComp;
                }
            }

            if (comp == null) return false;

            return true;
        }

        public static LG_WeakResourceContainer GetParentResourceContainer(Vector3 position)
        {
            var resourceContainers = GetComponentsInParentAtPosition<LG_WeakResourceContainer>(position)
                .Distinct(InstanceIDComparer)
                .Cast<LG_WeakResourceContainer>()
                .ToArray();

            if (!resourceContainers.Any())
            {
                QoLFixPlugin.LogError($"{nameof(ItemInLevel)} isn't located inside of a resource container!?");
                return null;
            }

            if (resourceContainers.Count() > 1)
            {
                QoLFixPlugin.LogError($"{nameof(ItemInLevel)} is inside of multiple resource containers!?");
            }

            return resourceContainers.First();
        }
    }
}
