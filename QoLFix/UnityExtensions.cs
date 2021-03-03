using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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

        public static Vector2 GetSize(this RectTransform t)
        {
            var size = new Vector2(t.rect.width, t.rect.height);

            if (size == Vector2.zero)
            {
                size.x = LayoutUtility.GetPreferredWidth(t);
                size.y = LayoutUtility.GetPreferredHeight(t);
            }

            if (size == Vector2.zero)
            {
                var layoutGroup = t.GetComponent<LayoutGroup>();

                if (layoutGroup != null)
                {
                    size.x = layoutGroup.preferredWidth;
                    size.y = layoutGroup.preferredHeight;
                }
            }

            return size;
        }
    }
}
