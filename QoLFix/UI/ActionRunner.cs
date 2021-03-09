using System;
using UnityEngine;

namespace QoLFix.UI
{
    public static partial class UIManager
    {
        private class ActionRunner : MonoBehaviour
        {
            public ActionRunner(IntPtr value)
                : base(value) { }

            internal void LateUpdate() => ActionScheduler.Update();
        }
    }
}
