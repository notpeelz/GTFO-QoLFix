using System.Linq;
using Gear;
using LevelGeneration;
using UnityEngine;

namespace QoLFix
{
    public static class GTFOUtils
    {
        public static LG_WeakResourceContainer GetParentResourceContainer(Vector3 position)
        {
            var resourceContainers = Physics.OverlapSphere(position, float.Epsilon, LayerManager.MASK_PING_TARGET)
                .Select(x => x.GetComponentInParent<LG_WeakResourceContainer>())
                .Where(x => x != null)
                .ToArray();

            if (!resourceContainers.Any())
            {
                QoLFixPlugin.Instance.Log.LogError($"{nameof(ResourcePackPickup)} isn't located inside of a resource container!?");
                return null;
            }

            if (resourceContainers.Count() > 1)
            {
                QoLFixPlugin.Instance.Log.LogError($"{nameof(ResourcePackPickup)} is inside of multiple resource containers!?");
            }

            return resourceContainers.First();
        }
    }
}
