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
    }
}
