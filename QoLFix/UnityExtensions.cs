using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QoLFix
{
    public static class UnityExtensions
    {
        public static string GetPath(this GameObject obj)
        {
            return string.Join("/", obj.GetComponentsInParent<Transform>(true).Select(t => t.name).Reverse().ToArray());
        }

        public static IEnumerable<GameObject> GetChildren(this GameObject obj)
        {
            for (var i = 0; i < obj.transform.childCount; i++)
            {
                yield return obj.transform.GetChild(i).gameObject;
            }
        }

        public static bool ContainsPoint(this BoxCollider collider, Vector3 point)
        {
            var bounds = new Bounds(collider.center, collider.size);
            var localPos = collider.transform.InverseTransformPoint(point);
            return bounds.Contains(localPos);
        }

        public static void SetLayerRecursively(this GameObject obj, int layer)
        {
            obj.layer = layer;
            var t = obj.transform;
            for (var i = 0; i < t.childCount; i++)
            {
                SetLayerRecursively(t.GetChild(i).gameObject, layer);
            }
        }
    }
}
