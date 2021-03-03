using System;
using UnityEngine;

namespace QoLFix.UI
{
    public static partial class UIManager
    {
        public static event Action OnNextFrame;

        public class ActionRunner : MonoBehaviour
        {
            private static event Action OnNextFrame;

            public ActionRunner(IntPtr value)
                : base(value) { }

            internal void LateUpdate()
            {
                OnNextFrame = UIManager.OnNextFrame;
                UIManager.OnNextFrame = null;
            }

            internal void Update()
            {
                OnNextFrame?.Invoke();
                OnNextFrame = null;
            }
        }
    }
}
