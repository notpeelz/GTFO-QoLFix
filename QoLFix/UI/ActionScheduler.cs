using System;
using System.Collections.Generic;
using UnityEngine;

namespace QoLFix.UI
{
    public static class ActionScheduler
    {
        private class ActionEntry
        {
            public ActionEntry(Action action, float delay)
            {
                this.action = action;
                this.delay = delay;
                this.active = false;
            }

            public Action action;
            public float delay;
            public bool active;
        }

        private static readonly List<ActionEntry> Actions = new(64);

        public static void Schedule(Action action, float delay = 0) =>
            Actions.Add(new(action, delay));

        internal static void Update()
        {
            var deltaTime = Time.deltaTime;
            for (var i = Actions.Count - 1; i >= 0; i--)
            {
                var entry = Actions[i];
                if (!entry.active)
                {
                    entry.active = true;
                    continue;
                }

                entry.delay -= deltaTime;
                if (entry.delay <= 0)
                {
                    entry.action();
                    Actions.RemoveAt(i);
                }
            }
        }
    }
}
